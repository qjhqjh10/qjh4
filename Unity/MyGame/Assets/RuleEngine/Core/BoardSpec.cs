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
    }
}
