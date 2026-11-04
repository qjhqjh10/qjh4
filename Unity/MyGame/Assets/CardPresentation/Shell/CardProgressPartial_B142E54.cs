// CardProgressPartial_B142E54.cs — `CardProgress` 的**发放**口：领万能卡奖励时把数量记进正本（B142 · 执行代理 E54 · 2026-11-04）
//
// ============================ 为什么要有这个口 ============================
//  原版 `RewardService.CollectSingleReward` 对 `Wildcard` 那一支走的是
//  `CollectItemReward<Wildcard>` ⇒ `InventoryManager.Add<Wildcard>(item, qty, instanceID)`
//  （`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/RewardService.cs:890`；分派处 `:641-700`）。
//  本工程万能卡的**数量正本**是 `CardProgress`（按 `army|rarity` 分桶，见 `Shell/CardDetailPopup.cs` 的
//  `WildKey` / `Wildcards(army, rarity)` / `TrySpendWildcard`）—— `Inventories.Wildcards` 那份背包
//  只当「条目表 + 刷新信号」用（数量读它不准，扣减也从不同步它）⇒ **发放只能写 `CardProgress`**，
//  写第二份就是「两处写同一条规则 = 迟早不一致」。
//
//  ⚠️ 本文件之前 `CardProgress` **只有扣减口**（`TrySpendWildcard`）和自检摆值口
//     （`SetWildcardsForTest`），**没有发放口** ⇒ 万能卡奖励无处可落（`rs-item:unresolved`）。
//
// ============================ 语义照 `TrySpendWildcard` 的成对口径 ============================
//  · 缺键时读到的出厂值 = `WildcardsPerRarity`（99/档，**我们的全解锁口径**，不是原版数值）⇒
//    加量在**读到的那一份**上累加，与扣减同一条算式。
//  · `Save()` 失败 ⇒ **退回内存改动**并出声（`false`），调用方据此不报成功 —— 与 `TrySpendWildcard`
//    2026-10-31（`B104_P3`）那次修复同一条纪律（⛔ 别留下「进程里加了、重启又没了」）。
//  · 加完发一次 `Inventories.Wildcards.UpdateItemCollection()`（= 原版 `WildcardInventory` 那句
//    `Signal.Raise(updateSignal)` 的等价物，与 `Shell/CardDetailPopup.cs` 里合成异画那一处同形）。
//
//  ⚠️ `CardProgress` 在 `Shell/CardDetailPopup.cs:110` 声明成 `partial`（**一字之差、零行数变化**）——
//     那张文件被账按 `文件:行号` 钉着，所以新成员落这里，⛔ 不插进原文件。
namespace CardPresentation
{
    public static partial class CardProgress
    {
        /// <summary>万能卡**发放**（= 原版 `InventoryManager.Add&lt;Wildcard&gt;(item, quantity, instanceID)` 的本地落点）。
        /// <param name="army">阵营名（`CardDef.Faction`；空/null = 「不分阵营」那一桶，与读口同义）。</param>
        /// <param name="rarity">小写英文档位（`common/rare/epic/legendary`，本文件只认
        /// `CardDetailPopup.WildcardRarities` 那四档 —— ⛔ 不在这里再写一份 switch）。</param>
        /// <param name="n">要加几张（≤ 0 = 不动，返回 false）。</param>
        /// <returns>true = 真加上了（或自检档不落盘但内存已改）；false = 参数不合法 / 落盘失败（内存已退回）。</returns>
        public static bool GrantWildcards(string army, string rarity, int n)
        {
            if (n <= 0) return false;

            string r = (rarity ?? "").ToLowerInvariant();
            if (System.Array.IndexOf(CardDetailPopup.WildcardRarities, r) < 0)
            {
                UnityEngine.Debug.LogWarning("[CardProgress] 发放万能卡：档位 `" + rarity + "` 不在 `WildcardRarities` 里 ⇒ 不加（⛔ 不猜一档）");
                return false;
            }

            EnsureLoaded();
            string key = WildKey(army, r);
            int have = Wildcards(army, r);          // 缺键 ⇒ 出厂值 `WildcardsPerRarity`（与扣减口同一条算式）

            int prev; bool hadKey = _wild.TryGetValue(key, out prev);   // 回滚用（缺键 = 用出厂值，回滚要删键）
            _wild[key] = have + n;
            if (!Save())
            {
                if (hadKey) _wild[key] = prev; else _wild.Remove(key);
                UnityEngine.Debug.LogError("[CardProgress] 发放万能卡**没能落盘**（「" + (army ?? "?") + "/" + r
                                         + "」×" + n + "）⇒ 这次发放**已退回**、不算数（⛔ 不假装发过）");
                return false;
            }

            Inventories.Wildcards.UpdateItemCollection();   // = 原版 `WildcardInventory` 的刷新信号
            return true;
        }
    }
}
