// WFExtraDestroyByTime.cs — 原版 `DestroyByTime` 的等价实现（9 挂载 / 7 件 prefab）
//
// 🔴 **2026-10-26（第八十三会话 · 代理 X1）从 `WarpforgeVFX/Runtime/WFEffectExtras.cs` 拆出**：
//    Unity 只能为「类名 == 文件名」的类建出脚本资产；异名文件里的 MonoBehaviour 建不出资产
//    ⇒ 场景存盘会被烘成内联 `MonoScript`（`!u!115`）存根 ⇒ player 加载即崩（编辑器里一切正常）。
//    普查 = `工具/scene_class_filename_census.py`。
//
// ⚠️ 本块**逐字搬**（只把缩进重新对齐到 namespace 里）。表 `effect_extras.json` 的读取与
//    `AttachAll` 装配仍在 `WFEffectExtras.cs`；`IWFEffectExtraTick` / `WFEffectExtra` /
//    `WFSoundSlot` 也在那里（同一 `WarpforgeVFX` 命名空间）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `DestroyByTime`（9 挂载 / 7 件 prefab）：`Start` 里 `Destroy(gameObject, lifeTime)`；
    /// **lifeTime == 0 时 warning + 退回 2 秒**（`DestroyByTime.cs:11-30`）。
    /// ⚠️ 原版是 `Start`（Awake 之后），我们在 `Apply()`（= 播放器 `Init`）里排 —— 同一拍，且批处理也走得到。
    ///
    /// <para>✅ 已复刻（2026-11-04 · B139-G · decomp_full/DestroyByTime__Start.c:23-38）—— 条目 13-01 就地订正
    /// （铁律 5 保留更正痕迹）：本类原来把定时销毁**交给播放器**（`owner.DestroyAfter(t)` ⇒ 销毁**整件特效**）。
    /// **那是错的** —— 原版逐句是 `Object.Destroy(get_gameObject(this), lifeTime)`，**销毁的是它自己那个节点**。
    /// 后果（最硬的一条）：`Blast_Big_Ork_Missiles` 的根 `destroyTime = 4.0 s`，而挂在
    /// `Bullet FX Ork/Impact #4/Blast_Big_Ork` 那个**子节点**上的 `DestroyByTime(1.5 s)` 把**整件**提前收掉
    /// ⇒ 爆炸只活 1.5 s（原版的 37.5%）。9 个挂载里 4 个挂在子节点上，全部吃这一条。
    /// ⇒ 现在照原版**只销毁自己这个 GameObject**：子节点到点自己消失，根该活多久活多久。
    /// （根上那几个本来就等于「收整件」，语义不变。）</para>
    ///
    /// <para>⚠️ **批处理（没有帧循环）里的两条路**：`Object.Destroy` 在批处理里不生效，所以
    /// ① **子节点** ⇒ 走 `IWFEffectExtraTick` 自己数秒，到点 `DestroyImmediate` 自己这个节点；
    /// ② **节点就是特效根**（= 收整件，与原版等价）⇒ 交回播放器的 `DestroyAfter`（它会在 `Tick` 的
    /// 正确一拍收，不会在 `Init` 中途把正在构造自己的对象删掉 —— 那样 `Play` 会返回一个已销毁的对象）。</para></summary>
    public class WFExtraDestroyByTime : MonoBehaviour, IWFEffectExtraTick
    {
        public float lifeTime;

        /// <summary>累计排过几次「到点销毁自己这个节点」（断言读数；运行期是**延迟**销毁，所以这里计的是「排上」）。</summary>
        public static int Scheduled { get; private set; }
        /// <summary>`lifeTime == 0` 走原版兜底 2 秒的次数。</summary>
        public static int ZeroFallbacks { get; private set; }
        /// <summary>批处理里由**自己**（`ExtraTick`）收掉的**子节点**数。</summary>
        public static int SelfDestroyed { get; private set; }
        public static void ResetDiagnostics() { Scheduled = 0; ZeroFallbacks = 0; SelfDestroyed = 0; }

        // ---- 批处理那条自我计时的路（见类注释 ⚠️）----
        float _at = -1f;          // <0 = 不走这条路
        float _t;
        WarpforgeEffectPlayer _owner;

        /// <param name="owner">播放器。**只在「本节点就是特效根」那一档用到**（见类注释 ⚠️②）。</param>
        public void Apply(WarpforgeEffectPlayer owner)
        {
            float t = lifeTime;
            if (t == 0f)
            {
                Debug.LogWarning("Animation lifeTime in DestroyByTime set to 0");
                t = 2f;                                  // 原版兜底（逐字）
                ZeroFallbacks++;
            }
            Scheduled++;
            _owner = owner;

            // 原版 = `UnityEngine.Object.Destroy(this.gameObject, lifeTime)`（**只收自己这个节点**）。
            if (Application.isPlaying) { Destroy(gameObject, t); return; }

            bool isRoot = owner != null && owner.gameObject == gameObject;
            if (isRoot) { if (owner != null) owner.DestroyAfter(t); else DestroyImmediate(gameObject); return; }
            _at = t; _t = 0f;                            // 子节点：自己数秒（见类注释 ⚠️①）
        }

        public void ExtraTick(float dt)
        {
            if (_at < 0f) return;
            _t += dt;
            if (_t < _at) return;
            _at = -1f;
            SelfDestroyed++;
            // 只收**自己这个节点**（原版范围）；批处理没有帧循环 ⇒ 立刻收。
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
