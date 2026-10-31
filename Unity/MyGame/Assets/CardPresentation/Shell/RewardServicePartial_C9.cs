// RewardServicePartial_C9.cs — 第 ② 处断点：**卡 id 落到「没有解析口 ⇒ 不发」**（第一百零九会话 · 执行代理 `C9`）
//
// ============================ 这条补的是什么 ============================
//  原版 `RewardService.CollectItemReward<T>(T reference, string instanceID, int quantity)` = **一句**
//  `InventoryManager.Instance.Add<T>(reference, quantity, instanceID)`（判据 =
//  `decomp_full/RewardService__CollectItemReward_object_.c`；类型 `T` = **物品的运行时类型**）。
//  拆到卡上就是 `T = RawCardScript` ⇒ 走到 `CardInventory.Add`（`CardInventory.cs:89,141`）
//  ⇒ 收藏读口 `InventoryManager.GetInventory<RawCardScript>().GetItemInstances(false)` 才看得见（`CardCollectionTab.cs:196,51`）。
//
//  我们这一侧 `CollectItemRewardById`（`Shell/Inventory.cs:2027`）原来**只查 `Inventory<ICurrency>`**
//  ⇒ 卡 id 一路落到 `Note("rs-item:unresolved")` 然后 return —— **一条都发不出去**，
//     而卡面已经按 `RawCardScript` 画对了（`Shell/ItemDrawerPartial_B109E26.cs`）
//  ⇒ 玩家看到「画对了卡面，但领了进不了收藏」。**这就是本文件补的那一跳。**
//
// ============================ 判据：为什么「id 在卡池里认得出」= 卡 ============================
//  本工程**没有** `ObtainableItem` 的运行时类型层（奖励条目只有 `item.targetId` 一个字符串，
//  见 `Shell/RewardInfo.cs` 的文件头）⇒ 判据只能换成「id 在卡池里认得出」。
//  这不是新发明：`ItemDrawer.Spec(...)` 的卡类型补位（`Shell/ItemDrawerPartial_B109E26.cs:59 FillCardType`）
//  与 `CardDrawerItem`（`Shell/ItemDrawerCard.cs:112`）**用的是同一个取卡口 `CollectionData.Card(id)`**
//  ⇒ 「这一格画成卡面」与「这一格发进卡背包」两处**永远一致**（⛔ 不按 `UM` 前缀推 —— `UM_SK_Starter` 是预组不是卡）。
//
//  ⛔ **异画包装 id（`#HB_…`）不进这一支**：原版那种物品的运行时类型是 `AlternateArtCard`，
//     不是 `RawCardScript`（它绑在原卡的数量上，`QuantityBoundItemInfo`）
//     ⇒ 判据 `def.Id != id` ⇒ **不认**（留给出声，铁律 2「查不到就说查不到」）。
//
// ============================ ✅ 原来那段「必须带着的规避」【已过期 · 2026-11-01（`B111_S2`）就地订正】============
//  ⚠️ 原来这里写「必须传 `CardTier = Level−1`，否则 `SetLevel(id, 1)` 会把升到 3 级的卡打回 1 级」——
//     **那个前提已经不存在了**：`B110_F1`（2026-10-31）把 `CardInventory.UpdateCardTier` 改成
//     「`customData` 里**没有** `CardTier` ⇒ **什么都不写**」（`Shell/Inventory.cs:1392` 的 `if (s == null) return;`），
//     因为原版那一跳写的是**运行时** `CardServices.TierCollection`（对玩家可见状态零影响），
//     而我们的落点是**持久**等级（`CardProgress`）—— 两者不同义 ⇒ 缺档位时必须不写。
//  ⇒ 下面那句 `customData` 现在是**同值写**（写进去的 `tier + 1` == 现值）：**代码保留**，作用是回归覆盖
//     （`Editor/B110_F1_Assert.cs` 的 B/C 组要靠它验「带档位的 `Add` 仍然只写现值」）。
//     ⛔ **别删 `customData` 那一句代码**，也⛔ 别把注释改回「必须带着」。
//
// ============================ 落在哪 / 怎么接 ============================
//  `Shell/Inventory.cs:1950` 的 `public static class RewardService` 改成 `partial`（**零行数变化**），
//  `CollectItemRewardById` 那一行（`:2036`，货币支之后）**同一物理行**追加一句
//  `if (CollectItemRewardCard(id, instanceId, quantity)) return;` —— 同样**零行数变化**
//  （铁律 18·b：`Shell/Inventory.cs` 被账按 `文件:行号` 钉着，`:2058` / `:2092` / `:2152` 三条就在它下游）。
//
//  判据 / 报告：`资料/完全复刻/判据/B109_R22_卡奖励收藏链.md` §3·1 · `资料/完全复刻/报告/C9.md`
using System.Collections.Generic;

namespace CardPresentation
{
    public static partial class RewardService
    {
        /// <summary>`CollectItemReward(RewardInfo)` 的**卡**那一支（见文件头）。
        /// <para>返回 `true` = 「**这个 id 是卡，这一条我处理了**」（发或不发都算处理完 —— 别让调用方再落进
        /// `rs-item:unresolved` 那条「判不出类型」的出路）；`false` = 「不是卡这一档」，调用方继续按原样往下走。</para>
        /// <para>⚠️ 与 <see cref="CollectItemReward{T}"/> 的分工：那一支管「物品 → 背包」这一句，
        /// 本方法管「**id → 是哪种物品**」那一跳（原版靠运行时类型，我们靠卡池）。⛔ 别把两件事合到一处。</para></summary>
        internal static bool CollectItemRewardCard(string id, string instanceId, int quantity)
        {
            if (string.IsNullOrEmpty(id)) return false;

            RuleEngine.CardDef def = CollectionData.Card(id);
            if (def == null || def.Id != id) return false;      // 卡池认不出 / 异画包装 `#HB_…`（见文件头）

            // 🔴 先摸一下 `Inventories`：四份背包是在**那个类的静态构造**里 `InventoryManager.Register` 的，
            //    不先触发就 `GetInventory<CardDef>()` 会拿到 null（`Shell/LiveOpsState.cs:393` 同款写法）。
            var bag = Inventories.Cards;
            if (bag == null || InventoryManager.GetInventory<RuleEngine.CardDef>() == null)
            {
                InventoryManager.Note("rs-item:noinv-card",
                    "卡奖励 `" + id + "` 认得出是卡，但 `Inventory<RuleEngine.CardDef>` 没登记 ⇒ 无处可加（这条**不发**）");
                return true;
            }

            // 带上**当前档位**（= 同值写，理由见文件头「已过期那条] —— `UpdateCardTier` 自 `B110_F1` 起在缺档位时**什么都不写**，
            // 这一份 `customData` 现在只是**回归覆盖**：写进去的 `tier + 1` 恰好等于现值 ⇒ 零可见变化。⛔ 别删。
            int tier = CardProgress.Level(def.Id) - 1;
            var cd = new Dictionary<string, string> { { "CardTier", tier.ToString() } };

            // = 原版 `InventoryManager.Instance.Add<RawCardScript>(reference, quantity, instanceID)`
            //   （末尾两个实参 = 原版 `.c` 里的 `customData` / `overrideAmount`；原版是 `null / false`，
            //    我们这里多给一份 customData，理由见文件头）。
            InventoryManager.Add<RuleEngine.CardDef>(def, quantity, instanceId, cd);
            return true;
        }
    }
}
