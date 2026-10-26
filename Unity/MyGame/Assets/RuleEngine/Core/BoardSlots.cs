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
        /// 🔴 **原版 `MinionManager.IsAvailableSlot(mm, n)` 的等价物 —— 「两侧合起来还放得下 n 个单位吗」
        /// 的【唯一判据】**。所有「这里还接得住吗」的问法都走它，⛔ 别在调用点另写「空格 = 没单位」。
        ///
        /// 判据（第一权威，逐句）：`MinionManager__IsAvailableSlot.c:19-29`
        /// ── `return (左容量 − 左人数) + (右容量 − 右人数) >= n;`
        /// （容量 = `left/rightSlotPos.Length`，`+0x20/+0x28`；人数 = `left/rightMinions.Count`，
        ///  `+0x60/+0x68` —— 字段表见 `BoardSlots.cs` 文件头 2026-10-01 那条）
        /// · **两侧相加**（不是「某一侧放得下」）；**只看 `List.Count`**，**不看任何具体格号**
        ///   —— 原版两侧是无洞 `List`，「人数」与「占了哪几格」是同一件事。
        /// · 同族旁证：`MinionManager__IsAvailableSlotLeft.c:19` / `…Right.c:19` 是分侧版
        ///   （`容量 − 人数 >= n`），`MinionManager__AvailableSlotsCount.c` 直接返回那个和式。
        /// · 原版**全部**调用点传的都是 `n = 1`：`BattleManager__CanPlayCard.c:109`（出牌入口 · 棋盘满 ⇒
        ///   `NotEnoughRoom`）· `_ResolveStealMinion_d__551:35`（抢单位入口）·
        ///   `_ResolveSummonUnit_d__510:64` · `_ResolveCardAutoSummoned_d__511:83` ·
        ///   `AbilityLogic__PlayAnim.c:111/:119/:127`。
        ///
        /// ⚠️ 我们的 `CountOnSide` 数的是**非空格数** ⇒ 与 `List.Count` 逐字同义；
        ///    「有洞时到底哪一格能落」**不在**这条判据里（原版根本没有那一档，见
        ///    <see cref="NextFreeWithoutDisplacing"/>）—— 那条只管落点，不管「有没有位」。
        /// </summary>
        public static bool IsAvailableSlot(PlayerState ps, int n)
        {
            return RoomOnSide(ps, Left) + RoomOnSide(ps, Right) >= n;
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
        ///     落到该侧最外那一格。🔴 **2026-10-22（`T02` 同族 · `W63` 点名的那一笔）：这一支的落点
        ///     改走 <see cref="NextFreeWithoutDisplacing"/>**（选侧规则与原来那句 `l &lt; r ? 左 : 右`
        ///     **逐字相同**，多出来的只有「那一格真的空着吗」那道保险 —— 见方法体里的注释）。
        ///   · 🔴 **2026-10-23 补：上面三条算出来的落点如果【已经有个人】（只可能发生在有洞的棋盘上），
        ///     一律改成「该侧最外的空格」并出声** —— 见方法体里那段长注释（`Insert` 的插推循环
        ///     会覆盖那一格的人；连续棋盘上这条闸**一次都不会触发**，行为逐字不变）。
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
                // 退化路（督军格 / 越界）：原版 `AdjustedSlot.c:45-61` —— 左 < 右 ⇒ 左；否则右（平手也走右）。
                //
                // 🔴 **2026-10-22（`T02` 同族 · `W63` 普查点名的那最后一笔）：落点改走
                //    `NextFreeWithoutDisplacing`**（= 与 `DeployFree` / `DoTakeControl` / `EndTurn` /
                //    `SimpleAI.FirstFreeSlot` / `SimpleAI.NextDeploySlot` **同一个**「真的可占」判据）。
                //    为什么：原来那句 `index = CountOnSide(ps, side)` **假设棋盘连续无洞**
                //    （原版两侧是 `List`，人数 = 占了哪几格是同一件事）；我们的 `Board[]` 是定长数组，
                //    **自检夹具**可以直接写出洞（见 `WarnHoley`）⇒ 那时算出的下标**可能已经有人**，
                //    而 `Insert` 在有洞时会落到「插推」路径上 ⇒ **静默抹掉**那一格的人
                //    （与 `A1692` 的原始症状同族）。
                //   ⚠️ **原版行为【不可比】**：原版根本没有「目标格被占」这一档 ——
                //    `GetNextSlotWithoutDisplacing.c:14-30` 全文只比两个 `List.Count`、没有任何占用检查
                //    ⇒ 换格是补**我们表示法**的洞，⛔ 别在注释里写成「照原版」（同 `A1692` 的措辞口径）。
                //   ⚠️ **连续棋盘上行为逐字不变**：那一档的第一支就是原样返回
                //    `NextWithoutDisplacing`（选侧 `l < r ? 左 : 右`、落点 = 该侧最外那一格）。
                //   ⚠️ 有洞且真的换了格要**出声**（本仓红线「不许静默失败」；同 `SimpleAI` 那两处）。
                int l = CountOnSide(ps, Left), r = CountOnSide(ps, Right);
                side = l < r ? Left : Right;                 // 失败时也保持老行为（out 参数）
                int slot = NextFreeWithoutDisplacing(ps, out int blockedSlot);
                if (slot < 0) { index = 0; return false; }   // 两侧都满（= `IsAvailableSlot(ps,1)` 为假）
                if (blockedSlot >= 0)
                {
                    UnityEngine.Debug.LogWarning("[BoardSlots] ⚠️ 棋盘不连续：`Resolve`（督军格退化路）"
                        + "按原版规则算出的 " + blockedSlot + " 号格已被 "
                        + (ps.Board[blockedSlot] != null ? ps.Board[blockedSlot].Name : "?")
                        + " 占着 —— **不覆盖**，改选该侧最外的空格 " + slot
                        + "。引擎的增删路径都维持「每侧从贴督军那格起连续无洞」，出现洞说明"
                        + "**有地方（多半是自检夹具）直接写了 `Board[]`**（`T02` / `A1692` 同族）。");
                }
                side = SideOf(slot);
                index = IndexOf(slot);
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
            }
            else
            {
                index = IndexOf(requestedSlot);
                if (index < 0) index = 0;
                if (index > count) index = count;  // 夹到最外那一格（= 追加）
            }

            // ============================================================
            //  🔴 **2026-10-23（引擎线 · 补「插推族」最后一段）：有洞时落点被占 ⇒ 不覆盖、不推人。**
            //
            //  ⚠️ **这条闸只在【有洞】的棋盘上生效**（判据 = `IsContiguous`）—— 连续棋盘上
            //    「请求的格有人」是**正常的插入**（`Insert` 把它后面的单位整体外移一格，
            //    原版 `List.Insert` 就是这样），**绝不能**在这里换格，否则所有「插在两人之间」
            //    的落点全变。⇒ 先判连续，连续就直接返回原来的 `index`。
            //
            //  有洞时才有的那一档（原版两侧是 `List`、结构上不可能有洞；我们的 `Board[]` 是定长
            //  数组、**自检夹具**可以直接写出洞，见 `WarnHoley`）：
            //    · 洞在内侧（例：左 3 号格空着、2/1/0 号格都有人）⇒ `index == count` 指到**被占的最外格**；
            //    · 洞在中段（例：右 index0/2 有人、index1 空着）⇒ `index == count` 也指到**被占的 index2**；
            //    · 请求侧满、换到对侧时同族（对侧有洞 ⇒ `index = otherCount` 指到被占格）。
            //  ⇒ 交回给 `Insert` 的话，插推循环会把**外侧那个单位覆盖掉**（`A1692` 的原始症状：
            //    循环从 `count-1` 搬到 `count`，而 `count` 那格有人）。
            //  ⇒ 处置 = **改到该侧最外的空格**（判据 = <see cref="OutermostFreeOnSide"/>，
            //    与 `NextFreeWithoutDisplacing` 的保险**同一个方向**）；该侧竟没有空格（不可达）
            //    ⇒ 按「这一侧满了」处理：换对侧最外的空格；仍没有 ⇒ `false`。
            //  ⚠️ 请求格**空着**（有洞）时**不换** —— 那一格正是洞，写进去就把它填上了
            //    （`Insert` 在有洞分支里也是「请求哪格就写哪格」）。
            //
            //  ⚠️ **原版行为【不可比】**：`AdjustedSlot.c` 全文只比两条 `List.Count`、
            //    **没有任何占用检查** ⇒ 换格是补**我们表示法**的洞，⛔ 别写成「照原版」。
            //  ⚠️ **出声**（本仓红线「不许静默失败」）：这一格被改掉是夹具造出来的非法局面。
            // ============================================================
            int landing = SlotOf(side, index);
            if (ps.Board[landing] != null && !IsContiguous(ps, out _))
            {
                int blocked = landing;
                int free = OutermostFreeOnSide(ps, side);
                if (free < 0)
                {
                    // 不可达（`count < SlotsPerSide` ⇒ 该侧必有空格）；留着当结构兜底。
                    int other = -side;
                    free = OutermostFreeOnSide(ps, other);
                    if (free < 0) { index = 0; return false; }
                    side = other;
                }
                index = IndexOf(free);
                UnityEngine.Debug.LogWarning("[BoardSlots] ⚠️ 棋盘不连续：`Resolve`（侧向分支）算出的 "
                    + blocked + " 号格已被 "
                    + (ps.Board[blocked] != null ? ps.Board[blocked].Name : "?")
                    + " 占着 —— **不覆盖、不推人**，改选该侧最外的空格 " + SlotOf(side, index)
                    + "。引擎的增删路径都维持「每侧从贴督军那格起连续无洞」，出现洞说明"
                    + "**有地方（多半是自检夹具）直接写了 `Board[]`**（`T02` / `A1692` 同族）。");
            }
            return true;
        }

        /// <summary>
        /// 这一侧**最外**的那个空格（`-1` = 这一侧满了）。
        /// 判据只此一处 —— <see cref="NextFreeWithoutDisplacing"/> 与 <see cref="Resolve"/>
        /// 的「有洞不覆盖」两支都调它（⛔ 别在调用点另写一遍「从最外往里扫」）。
        /// ⚠️ 原版没有这一档（两侧是 `List`、不会有洞）—— 这是补我们表示法的洞。
        /// </summary>
        static int OutermostFreeOnSide(PlayerState ps, int side)
        {
            for (int i = BoardSpec.SlotsPerSide - 1; i >= 0; i--)
            {
                int s = SlotOf(side, i);
                if (ps.Board[s] == null) return s;
            }
            return -1;
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
        /// <remarks>
        /// 判据 = <see cref="IsAvailableSlot"/>（原版 `BattleManager__CanPlayCard.c:109` 那一跳
        /// `MinionManager.IsAvailableSlot(mm, 1)` 就是它）：**两侧合计**有空位就放得下，
        /// 与「请求哪一格」无关 —— 请求的那一侧满了时 `Resolve` 也会换到对侧最外一格
        /// （`AdjustedSlot.c:64-72 / :104-107`），所以两种问法在布尔上恒等。
        /// ⚠️ `requestedSlot` **保留形参**：调用方语义（`RuleCodes.ErrNotEnoughRoom` 那条）
        /// 仍按「想在 slot 出牌」来读；⛔ 别因为它现在不参与判据就把它删掉。
        /// </remarks>
        public static bool HasRoomFor(PlayerState ps, int requestedSlot)
        {
            return IsAvailableSlot(ps, 1);
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
                // 🔴 **2026-10-22（`T02` 同族 · `W63` 点名的那一笔）：督军格 / 越界请求也【不推人】。**
                //    `Resolve` 的督军支现在给的是「人少那侧最外的**空格**」（判据 =
                //    `NextFreeWithoutDisplacing`），但那个空格的下标**可能小于人数**
                //    （洞在内侧：例如 3 号格空着、2/1/0 号格都有人）—— 这种形状下走下面那段
                //    插推循环会把**最外那一格的人覆盖掉**（循环从 `count-1` 搬到 `count`，
                //    而 `count` 那格有人）：实测 256 种有洞棋盘里有 34 种会这样（见
                //    `RuleEngineTest_T02` 的 `TestT02ResolveWarlordSlotHonoursAvailability`）。
                //    ⇒ 有洞时**一律直写**（本方法在非法局面上的既定契约就是「不推别人」）。
                //    ⚠️ 连续棋盘走不到这里（`IsContiguous` 已挡）⇒ **合法局面的行为一个字不变**。
                //
                // 🔴 **2026-10-23（补「插推族」最后一段）：请求格【被占】时同样不许落到插推循环。**
                //  上面那条只接住了「请求格空着」；请求格被占（侧向格、有洞棋盘）原来会一路落到
                //  下面的插推循环 —— 而插推循环是 `Board[SlotOf(side,i+1)] = Board[SlotOf(side,i)]`
                //  那种整体外移：有洞时它会把**外侧那一格的人覆盖掉**（`A1692` 的原始症状）。
                //  ⇒ 剩下的三种（督军格 / 越界 / **被占的侧向格**）走同一条：`Resolve` 给出的落点
                //    **必定是空格**（侧向分支那段注释），直接写、⛔ 不推人。
                {
                    int hs, hi;
                    if (!Resolve(ps, requestedSlot, out hs, out hi)) return -1;
                    int dst = SlotOf(hs, hi);
                    if (!BoardSpec.IsValid(dst) || ps.Board[dst] != null) return -1;  // 理论上到不了
                    ps.Board[dst] = u;
                    return dst;
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
        /// <remarks>
        /// 🔴 **「满场」那一判只此一处** = <see cref="IsAvailableSlot"/>(ps, 1)
        /// （原版 `MinionManager__IsAvailableSlot.c:19-29`；调用点同族全部传 1）。
        /// 为什么可以拿「两侧合计」替掉原来的「选中那侧的人数 >= 每侧格数」——
        /// 选侧规则是 `l &lt; r ? 左 : 右`（`GetNextSlotWithoutDisplacing.c:14-30`）：
        ///   · 若 `l &lt; r` 且左支 `l >= 每侧格数` ⇒ `r &gt; l >= 每侧格数`，不可能 ⇒ **左支不可能满**；
        ///   · 只剩 `l >= r` 且右支 `r >= 每侧格数` ⇒ `l >= 每侧格数` ⇒ **两侧都满**。
        /// ⇒ 「选中那侧满」⇔「两侧都满」⇔ `IsAvailableSlot(ps, 1) == false`。
        /// （2026-10-22 之前这两句本来就已经等价，本次只是把判据显式收口到**原版那一条**上。）
        /// ⚠️ 原版这里**不做占用检查**（全文只比两条 `List.Count`）—— 目标格被占那一档见
        /// <see cref="NextFreeWithoutDisplacing"/>。
        /// </remarks>
        public static int NextWithoutDisplacing(PlayerState ps)
        {
            if (!IsAvailableSlot(ps, 1)) return -1;       // 两侧都满（原版 `IsAvailableSlot(mm,1)` 那一判）
            int l = CountOnSide(ps, Left), r = CountOnSide(ps, Right);
            int side = l < r ? Left : Right;
            return SlotOf(side, CountOnSide(ps, side));
        }

        /// <summary>
        /// 🔴 **`NextWithoutDisplacing` 的「防空覆盖」版** —— 落点与它**逐字相同**，只是多一层保险：
        /// 「不挤别人」那条算法假设**棋盘连续无洞**（原版两侧是 `List`，结构上不可能有洞），
        /// 而我们的 `Board[]` 是定长数组，**自检夹具**可以直接写出洞
        /// （见 <see cref="WarnHoley"/>）⇒ 那时算出来的那一格**可能已经有人**。
        ///
        /// 处置 = **不覆盖**：改从**同一侧**最外那一格往里找第一个空格。
        ///   · 找不到 ⇒ `-1`（按「这一侧满了」处理，调用方各自的满场分支接管）。
        /// ⚠️ **原版行为【不可比】**：原版根本没有「目标格被占」这一档
        ///    （`MinionManager__GetNextSlotWithoutDisplacing.c:14-30` 全文只比两个 `List.Count`、
        ///    没有任何占用检查）。⛔ **别在注释里写成「照原版」** —— 这是补**我们表示法**的洞。
        /// 🔴 **判据只此一处**：`RuleCore.DeployFree`（免费部署）· `EffectResolver.DoTakeControl`（抢单位）·
        ///    `RuleCore.EndTurn` 的「本回合控制到期、还回去」三处**都调它**
        ///    （以前只有 `DeployFree` 一处内联了这个保险，另外两处会静默盖掉人 —— `T02` 同族）。
        /// </summary>
        /// <param name="blockedSlot">被挡住的那一格（`-1` = 没被挡，落点与 `NextWithoutDisplacing` 相同）。
        /// 调用方用它决定要不要出声，以及「是满场」还是「目标格被占」。</param>
        /// <remarks>
        /// 返回 `-1` 的**两种**含义（调用方据此分支出声）：
        ///   · `blockedSlot &lt; 0` ⇒ **两侧都满**（判据 = <see cref="IsAvailableSlot"/>(ps, 1) == false）；
        ///   · `blockedSlot &gt;= 0` ⇒ 棋盘有洞、目标格被占，**且那一侧一个空格都没有**。
        /// </remarks>
        public static int NextFreeWithoutDisplacing(PlayerState ps, out int blockedSlot)
        {
            blockedSlot = -1;
            int dst = NextWithoutDisplacing(ps);
            if (dst < 0) return -1;                       // 两侧都满（`NextWithoutDisplacing` → `IsAvailableSlot` 的口径）
            if (ps.Board[dst] == null) return dst;        // 连续棋盘：恒走这一支（行为与以前逐字相同）

            blockedSlot = dst;                            // 有洞 ⇒ 那一格已经被占了
            int side = SideOf(dst);
            return OutermostFreeOnSide(ps, side);         // 该侧最外的空格（没有 ⇒ -1）
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

        /// <summary>
        /// 🆕 2026-10-26（批次 3 · 执行代理 `E3`）：**原版 `MinionManager.GetInteractiveUnitsCount()` 的等价物**
        /// = 这一侧「能被交互的单位」个数 = 场上单位数 **−** 带 `summonRift(730)` 的单位数。
        ///
        /// <para>**判据**（第一权威 `d:/2/tools/decomp_full/MinionManager__GetInteractiveUnitsCount.c`，
        /// 56 行逐句读过；🥈 交叉验证 `cpp2il/out_cpp2il_dev_ilspy/MinionManager.cs:3495-3580`）：</para>
        /// <list type="number">
        /// <item>`iVar3 = leftMinions.Count + rightMinions.Count`（`.c:18`，`+0x60`/`+0x68` 两条 `List`）；
        ///   我们的两条 `List` = 棋盘上「非空格数」⇒ <see cref="CountOnSide"/> 逐字同义。</item>
        /// <item>逐单位 `EntityScript__HasCurrentTrait(u, 0x2da, 0)`（`.c:25` / `:38`）为真 ⇒ `iVar3 -= 1`。
        ///   `0x2da = 730 = DefinedTrait.summonRift`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:73`）。</item>
        /// <item>两个列表各扫一遍（先左后右），**两侧都减**；返回减完的数。</item>
        /// </list>
        ///
        /// <para>⚠️ **别把它接到 `CountEval` 上**：`BoardCriteria` 数目标走的是
        /// `FilterMethods.CheckIfMeetsCriteria`，那份**根本不看 `0x2da`**
        /// （`decomp_full/FilterMethods__CheckIfMeetsCriteria.c` 里 `0x2da` **0 命中**，2026-10-26 现读）
        /// ⇒ 原版的「数目标」**不排除**裂隙单位。两者是**两条不同的口径**，混用会静默少数。</para>
        ///
        /// <para>⚠️ **本版没有调用方，是【有意为之】**：原版 `GetInteractiveUnitsCount` 的**唯一**消费者是
        /// `BoardAnalysis.EnemyHasMoreTroops` / `…AfterPlay`（`.c:12,21`），而那两份的**调用方 = 0**
        /// （`decomp_full` + `cpp2il` 全树 0 命中；`d:/2/新解包资源/assets_full` 里按方法名字符串搜也 0 命中
        /// ⇒ 不是反射/SO 调）。⇒ 原版这条链在本 build 里是**死代码**。这里按原语义建好，
        /// 等 AI 打分那条线（`Data/AiScoring.cs`，属 `E4`）接 `EnemyHasMoreTroops`（账行 750/751）时直接用。</para>
        /// </summary>
        public static int InteractiveUnitsCount(PlayerState ps)
        {
            if (ps == null || ps.Board == null) return 0;
            int n = 0;
            for (int s = 0; s < BoardSpec.Size; s++)
            {
                if (s == BoardSpec.WarlordSlot) continue;       // 督军不在原版那两条 List 里（`dump.cs:39326-39329` 只有 minions）
                var u = ps.Board[s];
                if (u == null) continue;
                if (u.Has(KeywordTable.SummonRift)) continue;   // 原版 `.c:25/:38` 的那一下 `iVar3 -= 1`
                n++;
            }
            return n;
        }

        // ------------------------------------------------------------------
        //  🆕 2026-10-26（批次 5 · 执行代理 `G3`）：`MinionManager` 两条**未建**的定位原语
        //  ⚠️ 两条都是**追加在类尾**（不改任何既有行号 —— 本文件的行号被 6 条账行引用）。
        // ------------------------------------------------------------------

        /// <summary>
        /// **原版 `MinionManager.GetIndexOfCard`**（`d:/2/tools/decomp_full/MinionManager__GetIndexOfCard.c`，
        /// 🥇 逐句读过，全文 62 行）的等价物 —— 「这张牌在棋盘的**有符号格号**」。
        ///
        /// <para>**原版行为**：先线性扫 `leftMinions(+0x60)`，第 i 个命中（`Object.op_Equality`）⇒ `return ~i`
        /// （= `-(i+1)`）；再扫 `rightMinions(+0x68)`，第 i 个命中 ⇒ `return i+1`；两侧都没有 ⇒ **`0`**
        /// （督军格的编码 —— 督军本身在 `+0x260`，**不在那两条表里**）。</para>
        ///
        /// <para>**为什么我们需要它**：我们的棋盘是定长 `Board[9]`，格号 ↔ 原版有符号下标只差
        /// `+4`（`ourSlot = 4 + origSlot`，见文件头）。`IndexOf(int slot)` 回答的是「**某一格**
        /// 在本侧排第几」，回答不了「**某张牌**在原版那个编码里是几」—— 后者是 `previousAbilityTargets` /
        /// `targetsAffected=150` / `filterSpecificCards` 那几族要的编码。</para>
        ///
        /// <para>⚠️ **与 `IndexOf(int)` 的分工**：那个是「格 → 侧内下标」，这个是「牌 → 原版有符号格号」。
        /// ⛔ 别把两者互相当替身（`orig = slot - 4` 只对**侧向格**成立，督军格两种口径都落到 0）。</para>
        /// </summary>
        /// <returns>原版有符号格号：`< 0` 左、`> 0` 右、`0` = 不在这两条表里（含督军 / null）。</returns>
        public static int OriginalIndexOfCard(PlayerState ps, UnitState card)
        {
            if (ps == null || ps.Board == null || card == null) return 0;
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                if (ReferenceEquals(ps.Board[SlotOf(Left, i)], card)) return -(i + 1);
            for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                if (ReferenceEquals(ps.Board[SlotOf(Right, i)], card)) return i + 1;
            return 0;
        }

        /// <summary>
        /// **原版 `MinionManager.GetNextSlotCheckingAdjacents`**（`d:/2/tools/decomp_full/MinionManager__GetNextSlotCheckingAdjacents.c`，
        /// 🥇 逐句读过，全文 80 行）的等价物 —— **AI 部署单位时挑「贴着某个『在场所影响相邻』的单位」的落点**。
        /// <para>**原版三条支**：① 该侧还有空位（`slotPos.Length − minions.Count ≥ 1`）⇒ 扫那侧的 minions：
        /// 第 i 个满足 `CardScript.HasWhileInPlayAdjacentEffect()` ⇒ **`return -i-2`**（左）/ **`i+2`**（右）
        /// （= 那一格**外侧**的下标，贴着他落）；② 两侧都不满足 ⇒ **`GetNextSlotWithoutDisplacing`**
        /// （= 我们 <see cref="NextWithoutDisplacing"/> 那条：人少那侧最外一格、平手走右）。</para>
        ///
        /// <para>🔴 **2026-10-26（批次 9 · `E4P08` · 账行 1120）：改成【返回原版的有符号格号】+ 已接线。**
        /// 返回域 = 原版口径：`&lt; 0` 左（`-1` = 贴督军那格）、`&gt; 0` 右，**`|值| − 1` = 该侧下标**；
        /// 绝对格号 = `BoardSpec.WarlordSlot + 返回值`（`ourSlot = 4 + origSlot`，见文件头）。
        /// 原版 `-i-2` / `i+2` 是扫描时直接算好的数，⛔ 别在内部先换绝对号再换回来。
        /// **接线点 = `SimpleAI.NextDeploySlot`**（原版唯一调用点 `AI__ExecuteAction.c:60-63`，AI 部署小兵那一支）。</para>
        ///
        /// <para>🔴 **判据换了（2026-10-26 裁定）：`HasWhileInPlayAdjacentEffect()` 照原版逐字判。**
        /// 我们此前用「`CardDef.AuraSpecs` 里有没有 `Adjacent == true`」当它的替代 —— 那是**另一件事**
        /// （光环作不作用到相邻格 ≠ 原版这个判据），已撤（判据 → `UnitState.HasWhileInPlayAdjacentEffect` 的 doc）。
        /// ⚠️ **今天这一支恒不触发**：池内 `requiem` 0 张 · ability 级 `targetsAffected == 100` 0 行
        /// ⇒ 部署落点**结构上永远走第三支**（这正是「照原版」，⛔ 不是「没做」）。</para>
        ///
        /// <para>⚠️ **返回 `0` = 我们这边「放不下」**（两侧都满）：原版这个函数在满场时返回越界的
        /// `rightCount+1`（调用方靠 `IsAvailableSlot` 事先挡住）；我们给**本函数原版值域里不可达的 0**
        /// （`~count` ≤ −1、`count+1` ≥ 1 ⇒ 原版永远拿不到 0），调用方据此返回自己的哨兵。
        /// ⚠️ 原版两侧是 `List`、**没有洞**；我们的 `Board[]` 可能出现 null（自检夹具）⇒ 本方法
        /// **跳过 null** 继续扫（⛔ 不中断 —— 原版 `get_Item == null` 那一支是「不该发生」的 throw），
        /// 有洞本身由 <see cref="IsContiguous"/> / <see cref="WarnHoley"/> 负责出声。</para>
        /// </summary>
        /// <param name="hasAdjacentEffect">**`null` = 用原版判据 `UnitState.HasWhileInPlayAdjacentEffect()`**
        /// （产品路径就是这条）；非 null 仅供自检/探针注入替身谓词（做鉴别力用）。</param>
        public static int NextSlotCheckingAdjacents(PlayerState ps, System.Func<UnitState, bool> hasAdjacentEffect = null)
        {
            if (ps == null || ps.Board == null) return 0;
            bool Adj(UnitState u)
            {
                if (u == null) return false;
                return hasAdjacentEffect != null ? hasAdjacentEffect(u) : u.HasWhileInPlayAdjacentEffect();
            }
            if (RoomOnSide(ps, Left) > 0)                                   // 第一支：左侧有空位才扫左侧
            {
                for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                    if (Adj(ps.Board[SlotOf(Left, i)])) return -(i + 2);    // 原版 `-i-2`（有符号）
            }
            if (RoomOnSide(ps, Right) > 0)                                  // 第二支
            {
                for (int i = 0; i < BoardSpec.SlotsPerSide; i++)
                    if (Adj(ps.Board[SlotOf(Right, i)])) return i + 2;      // 原版 `i+2`（有符号）
            }
            int blocked;
            int abs = NextFreeWithoutDisplacing(ps, out blocked);            // 第三支（原版 `GetNextSlotWithoutDisplacing`）
            return abs < 0 ? 0 : abs - BoardSpec.WarlordSlot;               // 绝对 → 有符号（`ourSlot = 4 + orig`）
        }
    }
}
