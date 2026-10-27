// UIEffectTogglerPlaySound.cs — 原版 `UIEffectTogglerPlaySound` 的运行时等价物
//
// 判据（逐方法读过 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/UIEffectTogglerPlaySound.cs` 全文 31 行
//       ＋ `d:/2/tools/decomp_full/UIEffectTogglerPlaySound__{DoUpdate,AnimationFinished,ctor}.c`）：
//   `public class UIEffectTogglerPlaySound : UIEffectTogglerBase`
//     · 两个槽：`PlaySoundOnTime onSound` / `PlaySoundOnTime offSound`
//     · `DoUpdate(currentTime, normalizedTime)`：**只推「目标态那一个槽」**
//        —— `((!targetStateEnabled) ? offSound : onSound)?.Update(currentTime, null);`
//        （⚠️ 第二个实参原版传的就是 **`null`** ⇒ `PlaySoundOnTime.Update` 里那条
//          `!is2d && transform != null` 判据会落到 **2D** 分支）
//     · `AnimationFinished()`：两个槽**都** `Reset()`（= `numberOfTimesPlayed = 0`，见
//        `PlaySoundOnTime__Reset.c`「只有一句」）；空槽跳过（原版 `if (x != null) x.Reset()`）
//     · 构造函数只有一句 `deactivateOnDisable = true` —— 我们的基类**默认就是 true**
//        （`UIEffectTogglerBase.deactivateOnDisable = true`）⇒ 语义等价，这里不重复写
//   ⛔ **不覆写 `Toggle`**：原版也没覆写（基类那句 `targetStateEnabled = option;` 就是全部）。
//
// 🔴 **复用而不是复制**：两个槽的类型是 `Battle/AnimFXController.cs` 里那个**已有**的
//    `[Serializable] public class PlaySoundOnTime`（顶层类，`CardPresentation` 命名空间）。
//    ⛔ 别在本文件再抄一份（`CLAUDE.md`「两处写同一条规则」），⛔ 也别去改那个文件
//    （它会牵动 `AnimFXController` 的行号 —— 判据见该文件 `PlaySoundOnTime` 的头部注释）。
//
// ⚠️ **与原版的一处形态差（如实记）**：原版那两个槽是 `private` + 预制体序列化；
//    我们的 UI 件是**代码里建**的（同 `UIEffectTogglerImage.fadeTargets` 的口径）⇒ 这里做成 `public` 字段，
//    由接线方在 `AddComponent` 之后赋值。空槽 = 这一档不出声（原版 `?.` 同义，且**不静默失败**：
//    `Ready` 会答 false，接线方可以据此出声）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>按目标态**播一条定时音效**（原版 `UIEffectTogglerPlaySound`）。
    /// <para>由 <see cref="UIEffectTogglerController"/> 驱动：它每帧调 <see cref="DoUpdate"/>，
    /// 一段动画走完再调 <see cref="AnimationFinished"/>。</para></summary>
    public class UIEffectTogglerPlaySound : UIEffectTogglerBase
    {
        /// <summary>「开」那一档的音效槽（原版 `onSound`）。空 = 开的时候不出声。</summary>
        public PlaySoundOnTime onSound;
        /// <summary>「关」那一档的音效槽（原版 `offSound`）。空 = 关的时候不出声。</summary>
        public PlaySoundOnTime offSound;

        // ---- 自检读口（⚠️ 全部只读；计数器是我们这一侧的等价物，原版没有）----
        /// <summary>两个槽都空？（接线方判空用 —— 空槽 = 这一档不会出声，⛔ 别当成「播了但听不见」）。</summary>
        public bool Ready { get { return onSound != null || offSound != null; } }
        /// <summary>推进过「开」槽几次。</summary>
        public int DrivenOnCount { get; private set; }
        /// <summary>推进过「关」槽几次。</summary>
        public int DrivenOffCount { get; private set; }
        /// <summary><see cref="AnimationFinished"/> 被调过几次（每次都该把两槽复位）。</summary>
        public int ResetCount { get; private set; }
        /// <summary>最近一次 <see cref="DoUpdate"/> 推的是哪个槽（true = `onSound`）。</summary>
        public bool LastDroveOn { get; private set; }

        /// <summary>清计数（自检每段之前调；**不碰两个槽自己的播放计数**）。</summary>
        public void ResetCountersForTest()
        {
            DrivenOnCount = 0;
            DrivenOffCount = 0;
            ResetCount = 0;
            LastDroveOn = false;
        }

        /// <summary>原版 `DoUpdate`：**只推目标态那一个槽**，另一个槽这一帧**一点不动**
        /// （所以「开→关」切换时 `offSound` 的计时从它自己被推的第一帧才起算）。</summary>
        public override void DoUpdate(float currentTime, float normalizedTime)
        {
            bool on = targetStateEnabled;
            var slot = on ? onSound : offSound;
            LastDroveOn = on;
            // 原版是 `?.Update(...)`：空槽**什么都不做**（不是「推一个空槽」）。
            if (slot == null) return;
            slot.Update(currentTime, null);      // ⚠️ 第二实参恒定 null（原版就是这样，见文件头）
            if (on) DrivenOnCount++; else DrivenOffCount++;
        }

        /// <summary>原版 `AnimationFinished`：**两个槽都复位**（`numberOfTimesPlayed = 0`），
        /// 空槽跳过。⚠️ 这是「下一段动画重新从头播」的前提 —— 漏了它，同一句音效第二次就不会响。</summary>
        public override void AnimationFinished()
        {
            ResetCount++;
            if (onSound != null) onSound.Reset();
            if (offSound != null) offSound.Reset();
        }
    }
}
