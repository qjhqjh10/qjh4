// RewardInfo.cs — 原版 `RewardInfo`（奖励条目：物品 + 数量 + 档位 + 实例）的本地等价物
// 🆕 2026-10-28 · 执行代理 `B91_W7` · 账 `日常_奖励` 668（G1 奖励事件层，裁定 1：按原版类型名独立成文件，类名 == 文件名）
//
// ── 判据（逐条出处）────────────────────────────────────────────────────────────
//   · 类形态 / 字段：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/RewardInfo.cs`
//     （`RewardInfo : ItemInfo<ObtainableItem>`；字段 `rewardTier` / `isExtraReward` / `convertedInto` /
//      `extraRewardProvider` / `grantedXp`，五个属性 `RewardTier` / `IsExtra` / `ConvertedInto` /
//      `ExtraRewardProvider` / `GrantedXp` 都是纯读 `[JsonIgnore]`）。
//   · `CanCollect()`：`decomp_full/RewardInfo__CanCollect.c` —— **全部内容只有一句**
//     `return Item.CanBeCollected();`（虚槽 `+0x1d8`；`Item` 是 `+0x208` 那个 getter = `base.Item`）。
//     ⇒ 这一层**没有任何自己的逻辑**，语义全在物品侧：`Currency` / `CampaignPoints` 按「上限 / 持有量」返 false。
//   · `IsEphemeral`：`decomp_full/RewardInfo__get_IsEphemeral.c`
//     = `ephemeralState != null && (ephemeralDuration > 0 || (boundEvent != null && 它的引用解得出来))`。
//   · `FromItemInfo(...)`：`RewardInfo__FromItemInfo.c`（静态工厂；此账 669 已判 A —— 原版它**零调用点**）。
//
// ── 与我们的数据形态的对接（🔴 必读）──────────────────────────────────────────
//   本工程**没有** `ObtainableItem` 类层次（物品 = `Shell/Inventory.cs` 的 `ICurrency` / `CardDef` 等），
//   奖励条目在本工程的正本是 `Shell/CampaignData.cs` 的 **`CampaignData.RewardSpec`**（生成器从
//   `Campaign Node Data*.json` 抄出来的 struct：`Id` / `Quantity` / `Tier` / `IsEphemeral` / `EphemeralMs` /
//   `ConvertedInto`）⇒ 本类与它是**双向无损转换**（`FromSpec` / `ToSpec`），⛔ 不并存两份数据。
//
//   🔶 自己设计（非原版）：`Id` 用 string 代替原版的 `LoadableReference<ObtainableItem>`；
//     `Item` 用「按 id 去背包里查」代替原版的对象引用。
//     为什么必须自己定：本机没有 `ObtainableItem`/`LoadableReference` 那一层（奖励条目在 SO 里只有
//     `item.targetId` 一个字符串）。调度台 2026-10-28（裁定 1）定。
using System;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `RewardInfo : ItemInfo&lt;ObtainableItem&gt;` 的本地等价物（一条奖励）。
    /// <para>⚠️ 它是**数据 + 一个谓词**，不含发放逻辑 —— 发放走 <see cref="RewardService"/>（`Shell/Inventory.cs`）。</para></summary>
    [Serializable]
    public class RewardInfo
    {
        /// <summary>原版 `Item`（`LoadableReference&lt;ObtainableItem&gt;` ⇒ 落到 `item.targetId`）。
        /// 本工程 = 背包里的物品 id（货币 code / 卡 id / …）。</summary>
        public string Id;
        /// <summary>原版 `Quantity`（`ItemInfo.quantity`）。</summary>
        public int Quantity;
        /// <summary>原版 `rewardTier`（`RewardTier`：0 = `Basic`；`10` = 高级档，见 `RewardsScene` 的 `IsPremiumLocked` 判据）。</summary>
        public int Tier;
        /// <summary>原版 `instanceId`（同 id 多实例时区分；空 = 不区分）。</summary>
        public string InstanceId;
        /// <summary>原版 `isExtraReward`（`[JsonIgnore]`，不落盘）：这条是不是 `RewardEventHandler` 追加出来的。</summary>
        public bool IsExtra;
        /// <summary>原版 `convertedInto`（`[JsonIgnoreIfEmpty]`）：这条奖励被兑换成的那一条（没有 = null）。</summary>
        public RewardInfo ConvertedInto;
        /// <summary>原版 `extraRewardProvider`（`[JsonIgnore]`）：谁追加的（handler key）。</summary>
        public string ExtraRewardProvider;
        /// <summary>原版 `grantedXp`（`[JsonIgnoreIfEmpty]`）。</summary>
        public int GrantedXp;
        /// <summary>原版 `IsEphemeral` 的两半（见文件头判据）：时长（毫秒）与世界事件绑定。
        /// ⚠️ 本工程 89 条战役奖励这两格**全是 0/null** ⇒ 今天一次都不触发（⛔ 不是没实现）。</summary>
        public bool IsEphemeral;
        public long EphemeralMs;

        public RewardInfo() { }

        public RewardInfo(string id, int quantity, int tier = 0, string instanceId = null,
                          string extraRewardProvider = null, bool isExtra = false)
        {
            Id = id;
            Quantity = quantity;
            Tier = tier;
            InstanceId = instanceId;
            ExtraRewardProvider = extraRewardProvider;
            IsExtra = isExtra;
        }

        // ============================================================ 与原版 1:1 的五个只读面
        public int RewardTier { get { return Tier; } }
        public bool IsExtraReward { get { return IsExtra; } }
        public RewardInfo GetConvertedInto { get { return ConvertedInto; } }
        public string GetExtraRewardProvider { get { return ExtraRewardProvider; } }
        public int GetGrantedXp { get { return GrantedXp; } }

        /// <summary>原版 `bool CanCollect()`（`RewardInfo__CanCollect.c` = **一句** `return Item.CanBeCollected();`）。
        /// <para>本工程物品侧的等价判据：**是货币且已经顶到 `MaxAmount`** ⇒ 不能领（`ICurrency.MaxAmount`，
        /// 见 `Shell/Inventory.cs:1663`）；其余物品（卡 / 异画 / 卡组…）本机没有「上限」这一档 ⇒ 可领。</para>
        /// <para>⚠️ 与账 675-680 同一条口径：**本方法今天零生产调用点**（领取链 `RewardService.Collect`
        /// 还没接线）⇒ 它只服务将来「按条判可领」的那种 UI，⛔ 别拿它当已接线。</para></summary>
        public bool CanCollect()
        {
            if (string.IsNullOrEmpty(Id))
            {
                Debug.LogWarning("[RewardInfo] CanCollect：Id 为空 ⇒ 领不了（原版这一档 `Item` 为 null 会 NRE）");
                return false;
            }
            var currencies = InventoryManager.GetInventory<ICurrency>();
            if (currencies != null)
            {
                ItemInfo<ICurrency> held = currencies.Get(Id);
                if (held != null && held.Item != null)
                {
                    int max = held.Item.MaxAmount;
                    if (max > 0 && held.Amount >= max) return false;   // 已满上限 ⇒ 再发也进不去
                }
            }
            return true;
        }

        // ============================================================ 与 `CampaignData.RewardSpec` 的桥
        /// <summary>把生成器产出的 <see cref="CampaignData.RewardSpec"/> 读成一条奖励（⛔ 不改原 struct）。</summary>
        public static RewardInfo FromSpec(CampaignData.RewardSpec spec, string extraRewardProvider = null, bool isExtra = false)
        {
            return new RewardInfo(spec.Id, spec.Quantity, spec.Tier, null, extraRewardProvider, isExtra)
            {
                IsEphemeral = spec.IsEphemeral,
                EphemeralMs = spec.EphemeralMs,
            };
        }

        /// <summary>写回 <see cref="CampaignData.RewardSpec"/>（发放链要的就是这个形态）。</summary>
        public CampaignData.RewardSpec ToSpec()
        {
            var s = new CampaignData.RewardSpec();
            s.Id = Id;
            s.Quantity = Quantity;
            s.Tier = Tier;
            s.IsEphemeral = IsEphemeral;
            s.EphemeralMs = EphemeralMs;
            return s;
        }

        /// <summary>原版 `static RewardInfo FromItemInfo(IItemInfo&lt;ObtainableItem&gt;, RewardTier, string instanceId)`。
        /// <para>⚠️ 原版这个方法**零调用点**（账 669 已判 A，等价实现 = `CampaignData.RewardsOf`）⇒ 这里只留
        /// 「从背包条目抄一条奖励」的本地形态，供将来那条链用，**今天没有消费者**。</para></summary>
        public static RewardInfo FromItemInfo(ItemInfo<ICurrency> itemInfo, int tier = 0, string instanceId = null)
        {
            if (itemInfo == null || itemInfo.Item == null)
            {
                Debug.LogWarning("[RewardInfo] FromItemInfo：条目/物品为 null ⇒ 返 null（原版同一档 NRE）");
                return null;
            }
            return new RewardInfo(itemInfo.Item.GetID(), itemInfo.Amount, tier, instanceId);
        }

        public override string ToString()
        {
            return (Id ?? "<null>") + " ×" + Quantity + "（tier " + Tier + (IsExtra ? " · extra" : "") + "）";
        }
    }
}
