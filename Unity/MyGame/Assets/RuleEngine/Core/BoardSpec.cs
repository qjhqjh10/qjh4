// BoardSpec.cs — 战场几何（**9 格，督军居中**）
//
// 这是本次定下来的硬决定，权威来源三处一致：
//   · 规则书 :30 ——「玩家棋盘 9 个空格，督军永远占据中央格，两侧各 4 格共可部署 8 张部队卡」
//   · `rule_core.gd:44` —— `BOARD_SIZE := 9` / `WARLORD_SLOT := 4`
//   · battlearena1 资源 —— `MinionArea` 的 `slotsPerSide=4` = 每侧 4 个部署位
//
// ⚠️ 卡牌基座原来是 **7 格**（`SlotCount=7` / `WarlordSlot=3`）——
//    那是被推翻过的误读，`rule_core.gd:42` 的注释写得很明白：
//    「2026-08-23 修正：此前注释误读 slotsPerSide=3 小兵+督军=7 格 → 7 格违反说明书」。
//
// 槽位布局（从 0 到 8，左 → 右）：
//     0  1  2  3  [4=督军]  5  6  7  8
using System.Collections.Generic;

namespace RuleEngine
{
    public static class BoardSpec
    {
        public const int Size = 9;
        public const int WarlordSlot = 4;

        /// <summary>督军两侧各有几个可部署格</summary>
        public const int SlotsPerSide = 4;

        /// <summary>能往这一格部署吗（督军格不能放）</summary>
        public static bool IsDeployable(int slot)
        {
            return slot >= 0 && slot < Size && slot != WarlordSlot;
        }

        public static bool IsValid(int slot) { return slot >= 0 && slot < Size; }

        /// <summary>槽位号 → 该格在「督军左侧第几格」的表示（左 1..4 = -1..-4，右 1..4 = +1..+4，督军 = 0）</summary>
        public static int OffsetFromWarlord(int slot) { return slot - WarlordSlot; }

        /// <summary>
        /// 一格位的**左右紧邻格**（不含自己），写到 <paramref name="into"/>（先清空）。
        ///
        /// 🔴 **「谁算相邻」的判据只此一处**（2026-09-13 抽取）。原来 `RuleCore` 里**内联写了两遍**
        /// （践踏 `~:940` 与爆裂 `~:969` 各一份 `slot ± 1` + `IsValid`），做「相邻」效果时再抄第三份
        /// 就是工程铁律说的「两处写同一条规则 = 迟早不一致」。现在三处都读这一个。
        ///
        /// 边界语义照原版：`BattleManager.GetAdjacentUnits`（反编译
        /// `decomp_out/BattleManager__GetAdjacentUnits.c`）按单位的**所属方**取那一方的
        /// `MinionManager.GetAdjacentUnits`，**只管同一方棋盘行内**的左右格；
        /// 越界的那一侧直接没有（我们这里用 <see cref="IsValid"/> 表达）。
        ///
        /// ⚠️ **不排除督军格** —— 督军就在 4 号格，它的左右（3 / 5）当然算相邻。
        ///    要不要把督军从**结果**里去掉由调用方决定（爆裂就排除了督军，践踏没有）。
        /// </summary>
        public static void AdjacentSlots(int slot, List<int> into)
        {
            into.Clear();
            if (!IsValid(slot)) return;
            for (int off = -1; off <= 1; off += 2)
            {
                int adj = slot + off;
                if (IsValid(adj)) into.Add(adj);
            }
        }
    }
}
