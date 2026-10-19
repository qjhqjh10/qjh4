// WarpforgeVatPoolPicker.cs — 原版 `RemnantNecrons.Toggle(...)` 里「随机挑一条 VAT」的那一半。
//
// 语义出处（**反编译方法体，逐行读的**）：
//   `d:/2/tools/decomp_full/RemnantNecrons__Toggle.c` —— 转成残骸那一支（`param_3 != 0`、`param_2 != 0`）里：
//       lVar5 = this.deathAnimationList;                       // List<VATAnimation>，长度在 +0x18
//       i     = UnityEngine.Random.Range(0, lVar5.Count);      // ⚠️ 不是 System.Random
//       this.vatPlayer._vatAnimation = lVar5[i];               // 写进 VATGPUPlayer 的 +0x20
//       Animation.Play(this.toRemnantAnimationName);           // 随后播 `To Remnant Necrons`
//   ⇒ **五条 `Card Remnant_Remnant Anim {1,3,5,8,9}` 不是五个宿主**，而是**同一个**
//     `RemnantBody3D Necrons/Card Remnant` 上的**随机池**；序列化默认值 = `… Anim 8`
//     （`MonoBehaviour_7929411139690315314.json` 的 `_vatAnimation`），其余四条只活在这张列表里
//     （`MonoBehaviour_9217091644567076402.json` 的 `deathAnimationList`，顺序 1/3/5/8/9）。
//
// 我们这边怎么落：
//   · 原版在 **Toggle 到残骸那一刻** 挑一条；我们的残骸体是**每次死亡新实例化**的
//     （`CardView` 走效果库实例化 `RemnantBody3D <阵营>`）⇒ **`OnEnable` 就是那一刻**，
//     语义等价。⛔ 不要写成「在 Awake 里挑」——`Awake` 早于 `VatDriver` 的字段初始化器那条路，
//     行为虽同、但和原版的「打开时才挑」这句话对不上（下个会话会以为是猜的）。
//   · 只有一条数据时**不挂本组件**（`EffectExporter.AttachOneVat` 的判据），
//     所以 Vanguard 那两件不受影响。
//   · 取不到驱动 / 池是空的 ⇒ **出声**（本项目铁律：不许静默失败）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>一个「VAT 随机池」：启用时从若干份 `WarpforgeVatData` 里随机挑一份交给驱动。
    /// 判据 → 本文件头（`RemnantNecrons.Toggle` 的反编译）。</summary>
    [DisallowMultipleComponent]
    public class WarpforgeVatPoolPicker : MonoBehaviour
    {
        [Tooltip("同一个物件上的 VAT 驱动（原版 `RemnantNecrons.vatPlayer`）")]
        public WarpforgeVatDriver driver;

        [Tooltip("原版 `deathAnimationList` —— **顺序逐字照原资产**（1/3/5/8/9）")]
        public WarpforgeVatData[] animations;

        [Tooltip("原版在「变成残骸那一拍」挑；我们的实例化即那一拍 ⇒ 默认在 OnEnable 挑")]
        public bool pickOnEnable = true;

        void OnEnable()
        {
            if (pickOnEnable) Pick();
        }

        /// <summary>等价于原版 `Toggle` 里那三行（Random.Range → `_vatAnimation = list[i]`）。</summary>
        public void Pick()
        {
            if (driver == null)
            {
                Debug.LogWarning($"[WarpforgeVFX] `{name}` 的 WarpforgeVatPoolPicker 没有绑定 VAT 驱动 —— "
                               + "残骸体会停在第 0 帧（出声，不静默）");
                return;
            }
            if (animations == null || animations.Length == 0)
            {
                Debug.LogWarning($"[WarpforgeVFX] `{name}` 的 WarpforgeVatPoolPicker 池是空的 —— "
                               + "残骸体会停在第 0 帧（出声，不静默）");
                return;
            }
            // ⚠️ `UnityEngine.Random.Range(int,int)` 的上界**不含**（与原版一致：`Random.Range(0, Count)`）
            driver.SetAnimation(animations[Random.Range(0, animations.Length)]);
        }
    }
}
