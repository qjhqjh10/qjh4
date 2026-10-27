// BaseEventProgressRewardDrawerController.cs —— **活动「一格奖励」的抽屉控制器基类**
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/泛型与启动链.md` §四 `BaseEventProgressRewardDrawerController<TState>`（C 1 条）
//   · 账 `资料/完全复刻/账/泛型与启动链_补读.tsv` 同一类名下那一行（P2 · `OverridePremiumBadge`）
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BaseEventProgressRewardDrawerController.cs`
//   （**整类有真方法体**：`Initialize` / `OverridePremiumBadge` / 抽象 `ChangeState`）
//   ＋ VA 级互证 `d:/2/tools/decomp_generic_trial/out/BaseEventProgressRewardDrawerController_Int32Enum___OverridePremiumBadge__13c0390.c`
//
// 🔴 `OverridePremiumBadge` 的**全部内容**（ILSpy 真方法体）：
//   <code>
//   public void OverridePremiumBadge(Sprite sprite) { if (sprite != null) itemDrawer.OverridePremiumBadge(sprite); }
//   </code>
//   ⇒ **空的图不换**（不判空就会把角标刷成空白）。那两个 `null` 判是**两道**：
//   入口判 `sprite != null`、`ItemDrawerView.OverridePremiumBadge` 里还要判「这一格有没有那个角标节点」。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版是 MonoBehaviour + `[SerializeField] ItemDrawer itemDrawer`。我们这边是普通类（同
//      `Shell/EventProgressBarController.cs` 文件头差异 ①），抽屉句柄改用 `ItemDrawerView`。
//   ② 原版 `Initialize` 里那句 `ItemDrawer.Draw(transform, item, quantity)` 我们**没有等价签名**
//      （我们的 `ItemDrawer.DrawView` 还要 `PxRect box` + `ItemDrawerStyle`，几何属窗体）
//      ⇒ 抽屉由窗体通过 `OnDraw` 钩子建好再交回来；本类只在拿到句柄后做原版那后两跳。
//   ③ `Sprite` → `string`（我们没有 Sprite 句柄，全工程口径同 `ItemDrawerView.OverridePremiumBadge(string)`）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BaseEventProgressRewardDrawerController&lt;TState&gt; : MonoBehaviour`（抽象）。
    /// <para>用途：活动奖励格里「一件奖励 + 它属于哪一档状态」的控制器 —— 画一件、按状态改外观。
    /// 两个抽象/虚口：`Initialize`（虚，可覆盖）与 `ChangeState`（抽象，由每个活动的具体子类实现）。</para></summary>
    [Serializable]
    public abstract class BaseEventProgressRewardDrawerController<TState> where TState : Enum
    {
        /// <summary>原版 `protected ItemDrawer itemDrawer`（`+0x20`）。</summary>
        public ItemDrawerView ItemDrawer;

        /// <summary>原版 `protected IReadOnlyList&lt;RewardInfo&gt; rewards`（`+0x28`）：**整组奖励**，
        /// `Initialize` 只画**第一条**（`Enumerable.First`）。</summary>
        public IReadOnlyList<CampaignData.RewardSpec> Rewards;

        /// <summary>建抽屉的钩子（差异 ②）：`(第一条奖励, 件数) =&gt; 建好的句柄`。
        /// null ⇒ 出声一次（⛔ 不静默：那一格会是空的）。</summary>
        public Func<CampaignData.RewardSpec, int, ItemDrawerView> OnDraw;

        [NonSerialized] bool warned;

        /// <summary>原版 `public virtual void Initialize(IReadOnlyList&lt;RewardInfo&gt; rewards, bool isPremium,
        /// TState currentState)`。
        /// <para>判据（ILSpy 真方法体，**四步**）：
        /// ① `this.rewards = rewards;`
        /// ② `var first = rewards.First();`                        ← 空表会抛 `InvalidOperationException`
        /// ③ `itemDrawer = ItemDrawer.Draw(transform, first.Item, first.quantity);`
        /// ④ `itemDrawer.&lt;虚表 +0x1B8&gt;(isPremium);` 然后 `ChangeState(currentState);`
        /// </para>
        /// <para>🔴 第 ④ 步那个 `+0x1B8` 槽 = **`ItemDrawer.TogglePremiumHighlight(bool)`**
        /// （同一个槽位在 `Shell/ItemDrawer.cs` 的 §⑦ 已经核过：`drawer.&lt;0x1b8&gt;(tier == 10)` 那一处）。
        /// ⇒ 语义 = `TogglePremiumHighlight(isPremium)`。⚠️ 这一跳是**从槽位 + 同族调用点反推**的
        /// （ILSpy 只给出 `Indirect call: [v166 @ rax_v12+1B8]`，没有方法名）—— 按上面那条同槽位实证取它。</para>
        /// <para>⚠️ `rewards` 空表：原版 `First()` 抛异常 ⇒ 我们出声 + 不画（全工程口径）。</para></summary>
        public virtual void Initialize(IReadOnlyList<CampaignData.RewardSpec> rewards, bool isPremium, TState currentState)
        {
            Rewards = rewards;
            if (rewards == null || rewards.Count == 0)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogError("[RewardDrawer] Initialize：rewards 是空表（原版 `Enumerable.First()` 会抛 "
                                 + "InvalidOperationException）⇒ 这一格不画");
                }
                ChangeState(currentState);
                return;
            }
            var first = rewards[0];
            if (OnDraw != null) ItemDrawer = OnDraw(first, first.Quantity > 0 ? first.Quantity : 1);
            else if (!warned)
            {
                warned = true;
                Debug.LogWarning("[RewardDrawer] `OnDraw` 没接 ⇒ 抽屉建不出来（原版这一跳是 `ItemDrawer.Draw(...)`；"
                               + "几何在窗体那一侧，所以由窗体注入）");
            }

            if (ItemDrawer != null) ItemDrawer.TogglePremiumHighlight(isPremium);
            ChangeState(currentState);
        }

        /// <summary>原版 `public void OverridePremiumBadge(Sprite sprite)`（ILSpy 真方法体，**一句 + 一道空判**）：
        /// `if (sprite != null) itemDrawer.OverridePremiumBadge(sprite);`
        /// <para>⚠️ 差异 ③：收 `string artName`（= `ItemDrawerView.OverridePremiumBadge(string)` 的口）。
        /// 🔴 **空值不换**这条照抄：`null` / 空串都当「不换」（原版就是 `sprite != null`）。
        /// ⚠️ `itemDrawer` 为 null 时原版 NRE ⇒ 我们出声（⛔ 不静默吞掉）。</para></summary>
        public void OverridePremiumBadge(string artName)
        {
            if (string.IsNullOrEmpty(artName)) return;      // ← 原版 `if (sprite != null)` 那一道
            if (ItemDrawer == null)
            {
                if (!warned)
                {
                    warned = true;
                    Debug.LogWarning("[RewardDrawer] OverridePremiumBadge：`ItemDrawer` 是 null（原版这一跳 NRE）⇒ 不换角标");
                }
                return;
            }
            ItemDrawer.OverridePremiumBadge(artName);
        }

        /// <summary>原版 `protected abstract void ChangeState(TState currentState)` —— 由每个活动的具体子类实现
        /// （扩张通行证/突袭各自的「这一档长什么样」）。</summary>
        protected abstract void ChangeState(TState currentState);
    }
}
