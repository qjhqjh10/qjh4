// UIEffectTogglerCoffeeEffects.cs — 原版 `UIEffectTogglerCoffeeEffects` 的**外壳**等价物（账 455 / J-12）
//
// ============================ 判据（全是本机实读，见 判据/J-12_Coffee前置判据.md） ============================
// 原版类：`public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase`（`d:/2/tools/il2cpp_out/dump.cs:114051`），
//   **字段只有一个**：`[SerializeField] private UIEffect[] uiEffects; // 0x28`（`UIEffect` = **Coffee.UIEffect.dll** 里的
//   uGUI 材质滤镜组件，是一个挂在 `Graphic` 上的 `IMaterialModifier`/`IMeshModifier`）。
// 四个成员（`.c` + `dump.cs` 的 RVA 双证）：
//   · `Toggle(bool option, float duration = 0.3)` —— RVA `0x86F2E0`，体见 `decomp_full/UIEffectTogglerCoffeeEffects__Toggle.c:20-45`：
//       `targetStateEnabled(+0x21) = option`；
//       遍历 `uiEffects(+0x28)`：`if (uiEffects[i] != null) { Behaviour.enabled = option;
//                                                          if (option) { var tw = uiEffects[i].GetComponent<UIEffectTweener>();
//                                                                        if (tw != null) tw.Play(); } }`
//       ⚠️ **`duration` 一个字节都没用**（这一类没有补间）—— ⛔ 别把它接成"补间时长"。
//   · `DoUpdate(float, float)` —— RVA **`0x4B33B0`**（= 全局共享空方法桩，机器码 `c2 00 00` = `ret 0`）⇒ **空方法**；
//   · `AnimationFinished()`    —— RVA 同样是 **`0x4B33B0`** ⇒ **空方法**；
//   · `.ctor` —— 只写 `+0x20 = 1`（= 基类 `deactivateOnDisable` 的出厂值）+ `Behaviour..ctor()`。
//
// ============================ 🔴 我们这一侧的两处形态差（如实记，⛔ 不冒充） ============================
// ① **装不上 Coffee**：本工程**零** Coffee 资产（无 `Assets/ThirdParty/`、`find -iname "*Coffee*"` 无命中），
//    而 Coffee 本体是三个第三方 dll（`Coffee.UIEffect` / `Coffee.UIParticle` / `Coffee.UIEffect.R`）；本机那两个
//    `MelonLoader` 下的 dll 是**从 il2cpp 重建的产物、不是发行版**，且**两处目录都没有 LICENSE** ⇒
//    「搬 dll」那条**等用户拍板**（判据文 §③/④）。⛔ 本类**不引用**任何 Coffee 类型。
// ② **载体不是 `Graphic`**：我们的 UI 是 `ImageQuad`（自定义 MonoBehaviour），不是 uGUI `Graphic`
//    ⇒ 就算有 dll，`UIEffect` 也挂不上去。所以这里把「`uiEffects[i]`」换成**我们自己的件**、
//    把「`uiEffects[i].GetComponent<UIEffectTweener>().Play()`」换成**调用方给的钩子**（`playOnEnable[i]`）。
//    ⇒ **原版的视觉本体（滤镜 shader）本类【没有】复刻**，缺的时候**出声**（见 `WarnMissingVisualBody`）。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `UIEffectTogglerCoffeeEffects` 的**外壳**（开关 + 开时播一次）。语义逐条照 `Toggle`，
    /// `DoUpdate` / `AnimationFinished` 与原版一样**留空**（原版那两个是 `RVA 0x4B33B0` 的空桩）。
    /// <para>⛔ **别给这两个回调加补间** —— 原版这一档不参与 `UIEffectTogglerController` 的补间（那是
    /// `UIEffectTogglerImage` / `UIEffectCanvasGroupToggler` 那一族的事）。</para>
    /// <para>⚠️ **视觉本体缺失**：原版靠 Coffee 的 `UIEffect`（uGUI 材质滤镜）出效果；我们这侧没有它、也装不上，
    /// 缺的时候由 <see cref="WarnMissingVisualBody"/> **一次性出声**（⛔ 不静默）。</para>
    /// <para>判据全文 → `判据/J-12_Coffee前置判据.md`；派活包 → `判据/B9_特效C_派活包.md` §2 J-12。</para></summary>
    public class UIEffectTogglerCoffeeEffects : UIEffectTogglerBase
    {
        /// <summary>= 原版 `UIEffect[] uiEffects(+0x28)`（**只装本节点/本层的那几件**，不递归子件 ——
        /// 原版 `images[]`/`uiEffects[]` 都是这个口径）。</summary>
        public List<ImageQuad> uiEffects = new List<ImageQuad>();

        /// <summary>与 <see cref="uiEffects"/> **同下标**的「开时播一次」钩子（原版 = 同节点上
        /// `GetComponent<UIEffectTweener>().Play()`）。空元素 = 这一件没有 tweener（原版也是 `if (tw != null)`）。</summary>
        public List<Action> playOnEnable = new List<Action>();

        /// <summary>已经出过「视觉本体缺失」声明的件（按 `name` 去重 ⇒ 一次性，不刷屏）。</summary>
        static readonly HashSet<string> _warned = new HashSet<string>();

        public override void Toggle(bool option, float duration = 0.3f)
        {
            // ⚠️ `duration` 原版**没用**（`…__Toggle.c` 里根本没有它的读取点）⇒ 这里也不接成补间。
            targetStateEnabled = option;

            if (uiEffects == null || uiEffects.Count == 0)
            {
                WarnMissingVisualBody("（`uiEffects` 是空的）");
                return;
            }
            for (int i = 0; i < uiEffects.Count; i++)
            {
                var q = uiEffects[i];
                if (q == null) continue;
                q.enabled = option;                       // 原版：`Behaviour.set_enabled(uiEffects[i], option)`
                if (!option) continue;
                if (playOnEnable != null && i < playOnEnable.Count && playOnEnable[i] != null)
                    playOnEnable[i]();                    // 原版：`UIEffectTweener.Play()`
            }
            if (option) WarnMissingVisualBody(null);
        }

        /// <summary>原版 = 空桩（`RVA 0x4B33B0`）⇒ 这里也留空。⛔ 别往这里加补间。</summary>
        public override void DoUpdate(float currentTime, float normalizedTime) { }

        /// <summary>原版 = 空桩（`RVA 0x4B33B0`）⇒ 这里也留空。</summary>
        public override void AnimationFinished() { }

        /// <summary>「视觉本体没复刻」**一次性出声**（按 `gameObject.name` 去重；`null` = 用本对象的 `name`）。
        /// <para>🔴 这是**如实报告缺口**，不是普通日志：原版这一档的可见效果 100% 来自 Coffee 的 `UIEffect`，
        /// 我们这侧没有 ⇒ 只剩「开关 + 播一次」的外壳。</para></summary>
        void WarnMissingVisualBody(string extra)
        {
            string key = (name ?? "?") + (extra ?? "");
            if (!_warned.Add(key)) return;
            Debug.LogWarning("[UIEffectTogglerCoffeeEffects] `" + (name ?? "?") + "` 只复刻了**外壳**"
                           + "（开关 + 开时播一次），**原版那个 Coffee `UIEffect` 材质滤镜视觉本体没有**"
                           + "（本工程零 Coffee 资产；dll 是 il2cpp 重建产物、无 LICENSE ⇒ 等拍板）。"
                           + (extra ?? ""));
        }
    }
}
