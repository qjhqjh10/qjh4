// TutorialPositioning.cs — 教程/脚本动作「落点在参照小兵左边吗」的原版判据（`MinionManager.PositionWillBeAtLeft`）
//
// 🔴 为什么单开一个文件（铁律 18·b）：`TutorialScript.cs` 有大量账落点按【行号】引用（`:908` / `:1189` /
//    `:1428` / `:1454` / `:1705` / `:1928` …）⇒ 在 `struct TutorialAttempt` 中间插字段会让它们整体漂移。
//    做法 = 把 `TutorialAttempt` 声明成 `partial`（那一行**零行数变化**），新增字段放进本文件 ⇒ 行号不动。
//
// 本文件两样东西：
//   ① `TutorialAttempt.ActingSlot`（`int?`）—— 原版 `PositionWillBeAtLeft` 的**参照小兵**（= `AiScripted.GetActingCard`
//      取到的 acting card）所在的**格号**。`null` = 未接（回退旧近似，见 `Matches` 那一行的注释）。
//   ② `TutorialPositioning.PositionWillBeAtLeft(...)` —— 原版判据的**纯函数等价物**。
//
// ── 判据（第一权威 = `d:/2/tools/decomp_full/MinionManager__PositionWillBeAtLeft.c`，逐句现读 2026-10-29）──
//   签名 `bool PositionWillBeAtLeft(MinionManager mm, uint slot, CardScript refMinion)`，返回「**在 refMinion
//   左边那一格插入**时，这条 `slot` 是不是那一格」。三支：
//     ① `refMinion == Hero(该侧)`（`:31` `op_Equality`）⇒ `return slot == 0xffffffff`（= **-1**，贴督军左边那一格）；
//     ② refMinion 在**左表**（`+0x60`）第 i 个（`:44` 命中 → `uVar4 = ~i` → `uVar4 -= 1` = `-(i+2)`）⇒ `return slot == -(i+2)`；
//        而左表第 i 个的有符号格号 = `-(i+1)`（`BoardSlots.OriginalIndexOfCard`）⇒ **`slot == refSigned - 1`**；
//     ③ refMinion 在**右表**（`+0x68`）第 j 个（`:74` 命中时 `uVar4` 已 `+1`，不再 `-1`）⇒ `return slot == j+1`；
//        右表第 j 个的有符号格号 = `j+1` ⇒ **`slot == refSigned`**（在右表同一格插入会把 ref 顶出去，新卡占它原位 ⇒ 在新卡视角是「ref 左边」）。
//   ⇒ 归并成四档：`refIsHero ⇒ slot==-1` · `refSigned<0 ⇒ slot==refSigned-1` · `refSigned>0 ⇒ slot==refSigned` ·
//      `refSigned==0 且非督军（= 找不到 refMinion）⇒ slot==-1`（账 1138 判据要点「都找不到 ⇒ == -1」）。
//     格号口径 = 原版**有符号格号**（`<0` 左 / `>0` 右 / 0 = 督军），不是我们 0..8 的绝对格号
//     （换算：`signed = ourAbsSlot - BoardSpec.WarlordSlot`，判据 = `BoardSlots.SlotOf` 与 `OriginalIndexOfCard`）。
//
// ⚠️ **为什么 `ActingSlot` 是 `int?` 而不是 `int`**：我们的绝对格号 `0` 是合法格（最左那格）⇒ 用它当「未接」哨兵会
//    把「ref 在 0 号格」误判成「未接」。`int?` 默认 `null`，语义无歧义。
// ⚠️ **今天 `ActingSlot` 没有生产者**（全仓 `new TutorialAttempt` = 0 处，`TutorialAttempt` 只在 `TutorialScript.cs`
//    内部出现）⇒ `Matches` 仍走回退旧近似 ⇒ **行为零变化**；接上后（`PermitsPlayerAction` 的调用方填 `ActingSlot`）
//    自动切到原版判据。⚠️ **未做 board 级端到端实测**（无 attempt 构造点）⇒ 格的换算是按 `BoardSlots` 的公式推的，
//    接上时**必须**拿一个真局面核一次（左表/右表各一）。

namespace RuleEngine
{
    public partial struct TutorialAttempt
    {
        /// <summary>
        /// 参照小兵（原版 `PositionWillBeAtLeft` 的 `refMinion` = `AiScripted.GetActingCard` 取到的 acting card）
        /// 的**绝对格号**（我们 0..8 的口径）。
        /// <para>`null` = 未接（`Matches` 回退旧近似 `< BoardSpec.WarlordSlot`，**已知偏宽** —— 账 1138）。</para>
        /// <para>⚠️ 只对 `PlayCard` 且 `target.unitType ∈ {PlayerMinionLeft(31), PlayerMinionNotLeft(32)}` 有意义。
        /// 判据 / 换算 → 本文件头部。</para>
        /// </summary>
        public int? ActingSlot;
    }

    /// <summary>
    /// 原版 `MinionManager.PositionWillBeAtLeft(MinionManager, uint slot, CardScript refMinion)` 的**纯函数等价物**。
    /// 判据 / 推导 → 本文件头部。⛔ 别在别处再写一份「左边吗」的判断。
    /// </summary>
    public static class TutorialPositioning
    {
        /// <summary>
        /// 在 <paramref name="refSigned"/> 处那张参照单位**左边一格**插入时，<paramref name="targetSigned"/>
        /// 是不是那一格。
        /// </summary>
        /// <param name="targetSigned">目标格（**原版有符号格号**：`ourAbsSlot - BoardSpec.WarlordSlot`）。</param>
        /// <param name="refSigned">参照单位所在格（同上；督军 = 0）。</param>
        /// <param name="refIsHero">参照单位是不是督军（原版 Hero 那一支）。</param>
        /// <returns>`true` = 目标格正是「ref 左边那一格」。</returns>
        public static bool PositionWillBeAtLeft(int targetSigned, int refSigned, bool refIsHero)
        {
            if (refIsHero) return targetSigned == -1;      // ① `:31` `slot == 0xffffffff`（督军）
            if (refSigned < 0) return targetSigned == refSigned - 1;   // ② 左表 `slot == -(i+2)`
            if (refSigned > 0) return targetSigned == refSigned;       // ③ 右表 `slot == j+1`
            return targetSigned == -1;                     // refSigned==0 且非督军 ⇒ **找不到** ⇒ 同 `:28` `== 0xffffffff`
        }
    }
}
