// BaseEventProgressClaimButtonController.cs —— **活动「领取」钮的四档状态表**（原版同名泛型类）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（唯一来源）：
//   · `资料/完全复刻/判据/B9_日常C_施工单.tsv` 账行 **5 / 6 / 7 / 8**（U04 那一组的 ⑥）
//   · `资料/完全复刻/判据/日常与活动.md` §三（活动线边界）
// 原版真身：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BaseEventProgressClaimButtonController.cs`
//   （**整类都有真方法体**：`StateOption.ApplyState` / `GameObjectElementsStateDefinition.ApplyState` /
//     `Initialize` / `CollectionClick`），字段表 = 同一文件（`stateDefinitions` / `onCollect`）。
//   另有两份 VA 级产物可互证：`d:/2/tools/decomp_generic_trial/out/`
//   `BaseEventProgressClaimButtonController_Int32Enum___Initialize__13bff70.c` 与 `…_CollectionClick__506d00.c`。
//   ⚠️ 后者那份 `.c` 的**首行函数名不是它自己**（写的是 `Missions_<>c__DisplayClass11_0__<CollectChallenge>b__2`）
//   ⇒ 按 `CLAUDE.md` 的「文件名与内容不符」那条，**不以它为准**；
//   `CollectionClick` 的语义取 ILSpy 那份真方法体（`onCollect?.Invoke()`）。
//
// 🔴 **这一族的形状是「一个状态一份定义，每份自己判等」**：
//   `Initialize(currentState, …)` **不是**「按 currentState 选一份」，而是**把每一份都调一次
//   `ApplyState(currentState)`**，由 `GameObjectElementsStateDefinition.ApplyState` 自己判
//   `EqualityComparer&lt;TState&gt;.Default.Equals(this.state, stateToChange)`。
//   ⇒ 改这里时**别**改成「找到匹配的就 break」（原版没有 break，后面每份都要跑一遍判等）。
//
// ⚠️ 与原版的差异（⛔ 不是「已经一样」）：
//   ① 原版是 **MonoBehaviour**、字段都是 `[SerializeField]`。我们这边是普通类（理由同
//      `Shell/EventProgressBarController.cs` 文件头差异 ①）。
//   ② `ProgressRewardLevelObject` / `PriceDisplayButton` 两个组件我们**还没有**对应类
//      ⇒ 各给一个**最小接口**（`ILevelObject` / `IPriceButton`），接口方法名照原版方法名取。
//      接真组件时**只需要让那个类实现接口**，本文件不用改。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `BaseEventProgressClaimButtonController&lt;TState&gt;`（MonoBehaviour → 普通类，差异 ①）。
    /// <para>用途：活动「领取」钮按状态切四档（`Locked` / `Unlocked` / `已有` / `付费档` …）——
    /// 每一档要说「哪些节点亮、要不要高亮、价格显示要不要禁点」。
    /// `TState` 就是那一族状态枚举（扩张通行证用 `ExpansionPassState`，突袭用 `RaidState` 之类）。</para>
    /// <para>🔴 本类**不带任何发奖逻辑**：点击只调 `onCollect`（`CollectionClick` 的全部内容）。
    /// 发奖在回调里（见 `Shell/RewardProgressBarPanel.cs` 的同一条口径）。</para></summary>
    [Serializable]
    public class BaseEventProgressClaimButtonController<TState> where TState : Enum
    {
        // ============================================================ 两个最小组件口（差异 ②）
        /// <summary>原版 `ProgressRewardLevelObject` 里本类唯一用到的那一个方法
        /// （`ApplyState` 末尾 `levelObject.ToggleHighlight(enableLevelHighlight)`）。</summary>
        public interface ILevelObject
        {
            void ToggleHighlight(bool toggle);
        }

        /// <summary>原版 `PriceDisplayButton` 里本类唯一用到的那一个属性
        /// （`ApplyState` 末尾 `priceDisplayButton.Interactable = false`）。</summary>
        public interface IPriceButton
        {
            bool Interactable { set; }
        }

        // ============================================================ 原版嵌套类 `StateOption`
        /// <summary>原版 `BaseEventProgressClaimButtonController.StateOption`（`[Serializable]`，
        /// 两个字段 + 一句方法）。`ApplyState()` 判据（ILSpy 真方法体）：
        /// <code>
        /// if (target)                 // Unity 空判（== null 或 已销毁 都算空）
        ///     target.SetActive(isEnabled);
        /// </code></summary>
        [Serializable]
        public class StateOption
        {
            public GameObject target;
            public bool isEnabled;

            public void ApplyState()
            {
                if (target != null) target.SetActive(isEnabled);
            }
        }

        // ============================================================ 原版嵌套类 `GameObjectElementsStateDefinition`
        /// <summary>原版 `BaseEventProgressClaimButtonController.GameObjectElementsStateDefinition&lt;TState&gt;`
        /// （`[Serializable]`，字段照反编译：`state` / `levelObject` / `enableLevelHighlight` /
        /// `priceDisplayButton` / `forceInteractableState` / `objects`）。
        /// <para>`ApplyState(TState)` 判据（ILSpy 真方法体，**逐条**）：
        /// <code>
        /// if (EqualityComparer&lt;TState&gt;.Default.Equals(this.state, stateToChange)) {
        ///     foreach (var o in objects) o.ApplyState();
        ///     levelObject.ToggleHighlight(enableLevelHighlight);
        ///     if (forceInteractableState &amp;&amp; priceDisplayButton != null)
        ///         priceDisplayButton.Interactable = false;
        /// }
        /// </code>
        /// 🔴 三条容易漏的：**① 整段被 `Equals` 闸住**（不匹配就什么都不做）；
        /// **② `levelObject` 原版没有空判**（解引用 ⇒ 丢高亮那一下会 NRE，我们出声 + 跳过）；
        /// **③ `Interactable` 只会被写成 `false`** —— 原版**从不**在这一跳把它写回 true。</para></summary>
        [Serializable]
        public class GameObjectElementsStateDefinition
        {
            public TState state;
            public MonoBehaviour levelObject;       // 实现 <see cref="ILevelObject"/> 的那个组件
            public bool enableLevelHighlight;
            public MonoBehaviour priceDisplayButton; // 实现 <see cref="IPriceButton"/> 的那个组件
            public bool forceInteractableState;
            public StateOption[] objects;

            [NonSerialized] bool warnedLevel;

            public void ApplyState(TState stateToChange)
            {
                if (!EqualityComparer<TState>.Default.Equals(state, stateToChange)) return;

                if (objects != null)
                    for (int i = 0; i < objects.Length; i++)
                        if (objects[i] != null) objects[i].ApplyState();

                var lv = levelObject as ILevelObject;
                if (lv != null) lv.ToggleHighlight(enableLevelHighlight);
                else if (!warnedLevel)
                {
                    warnedLevel = true;
                    Debug.LogWarning("[ClaimButton] `levelObject` 没接或没实现 `ILevelObject` ⇒ "
                                   + "高亮这一句跳过（原版这一跳无空判，会 NRE）");
                }

                var pb = priceDisplayButton as IPriceButton;
                if (forceInteractableState && pb != null) pb.Interactable = false;
            }
        }

        // ============================================================ 字段（原版两个）
        /// <summary>原版 `private GameObjectElementsStateDefinition&lt;TState&gt;[] stateDefinitions`。
        /// 每一档一份；`Initialize` 会把**每一份**都喂一遍当前状态。</summary>
        public GameObjectElementsStateDefinition[] stateDefinitions;

        /// <summary>原版 `protected Action onCollect`。**唯一的发奖入口**（见 <see cref="CollectionClick"/>）。</summary>
        protected Action onCollect;

        /// <summary>原版 `public void Initialize(TState currentState, CurrencyValue normalPrice, Action onCollect)`。
        /// <para>判据（ILSpy 真方法体 + `…_Initialize__13bff70.c` 互证）：**两句**
        /// ① `this.onCollect = onCollect;`
        /// ② `for (i = 0; i &lt; stateDefinitions.Length; i++) stateDefinitions[i].ApplyState(currentState);`
        /// （每份自己判等，见 `GameObjectElementsStateDefinition.ApplyState`）。</para>
        /// <para>⚠️ **`normalPrice` 在基类方法体里一个字节都没用**（反汇编里只存了第 4 个参数 `onCollect`；
        /// 第 3 个参数只在 `ApplyState` 的泛型调用里被当寄存器透传，没有语义）。
        /// 参数保留是为了**签名与原版一致**（子类/调用点不用改），⛔ 别在这里自己发明「写价格」。</para>
        /// <para>⚠️ `stateDefinitions` 为 null 时原版 NRE（`for` 直接解引用）⇒ 我们出声 + 返回。</para></summary>
        public void Initialize(TState currentState, object normalPrice, Action onCollect)
        {
            this.onCollect = onCollect;
            if (stateDefinitions == null)
            {
                Debug.LogError("[ClaimButton] Initialize：`stateDefinitions` 是 null（原版这一跳 NRE）⇒ 四档显隐一条都没设");
                return;
            }
            for (int i = 0; i < stateDefinitions.Length; i++)
                if (stateDefinitions[i] != null) stateDefinitions[i].ApplyState(currentState);
        }

        /// <summary>原版 `protected virtual void CollectionClick()`（ILSpy 真方法体）：
        /// <code>
        /// onCollect?.Invoke();
        /// </code>
        /// <para>⚠️ 原版是 `protected virtual`（子类可覆盖）；我们这边是普通类 ⇒ 给 `public virtual`
        /// （窗体要能从点击回调里调它），语义不变。</para></summary>
        public virtual void CollectionClick()
        {
            if (onCollect != null) onCollect();
        }
    }
}
