// ArenaIconRotation.cs —— 原版 `IconRotation` 组件的**运行时等价**（账 未分类:359 / 360）
//
// ============================ 这是什么、判据在哪 ============================
// 判据（本会话现读，2026-10-29）：
//   · `decomp_full/IconRotation__Start.c` **全文 4 行**：`*(float*)(this+0x30) = 0f`（累加器清零）。
//   · `decomp_full/IconRotation__Update.c` 全文：
//     ```
//     accum(+0x30) += Time.deltaTime;
//     if (period(+0x2c) <= accum) { accum = 0; transform.Rotate(rotX(+0x20), rotY(+0x24), rotZ(+0x28)); }
//     ```
//   · 序列化字段名（原版实例现读）：`xSpeed` / `ySpeed` / `zSpeed` / `updateRate`
//     （→ 偏移依次就是 +0x20 / +0x24 / +0x28 / +0x2c；`+0x30` 的累加器**不序列化**）。
//
// ============================ 实例（原版只有 2 个，都在 Black Legion 场） ============================
// 判据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearenablacklegion/MonoBehaviour/`：
//   | GameObject | MonoBehaviour | xSpeed | ySpeed | zSpeed | updateRate |
//   |---|---|---|---|---|---|
//   | `Background_Ring_1`     | `MonoBehaviour_4553.json` | 0 | **−0.02** | 0 | **0** |
//   | `Background_Ring_2.001` | `MonoBehaviour_4120.json` | 0 | **+0.01** | 0 | **0** |
//   (`m_GameObject` 分别 = 731 / 1013；`GameObject/Background_Ring_*.json` 的 `m_Component` 里正是 4553 / 4120 —— 双向对过。)
//
// 🔴 **`updateRate = 0` ⇒ 判据 `period <= accum` 恒真 ⇒ 每帧都转**（原版就这样，**帧率相关**）。
//   ⛔ 别「修成」按秒（那是我们编的行为）；要改成按秒必须先有原版判据。
//
// ⚠️ 原版这两环是**背景装饰环**：`Background_Ring_1` 与 `2.001` 反向（−0.02 / +0.01 ⇒ 转轴上反向 + 角速度 2 倍差）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `IconRotation`（`Assembly-CSharp`，类名 `IconRotation`，无命名空间）的运行时等价。
    /// <para>🔴 **逐句照 `.c`**：`Start` 只清累加器；`Update` 累加 `Time.deltaTime`，`updateRate &lt;= accum` 时
    /// 归零并 `transform.Rotate(x, y, z)`（`Space.Self` = 原版 `Rotate(x,y,z,0)` 那个 0）。</para>
    /// <para>⚠️ 我们放在 `WarpforgeVFX` 命名空间（与本目录其它战场运行时组件同族；`ArenaBuilder` 按全限定名引用）。
    /// 原版是全局命名空间的 `IconRotation` —— 命名空间不同**不影响语义**（只影响引用写法）。</para></summary>
    public class ArenaIconRotation : MonoBehaviour
    {
        /// <summary>原版 `xSpeed`（+0x20）：每次触发绕 X 转多少**度**（不是弧度 —— `Transform.Rotate` 吃角度）。</summary>
        public float xSpeed;
        /// <summary>原版 `ySpeed`（+0x24）。两个实例分别是 −0.02 / +0.01。</summary>
        public float ySpeed;
        /// <summary>原版 `zSpeed`（+0x28）。</summary>
        public float zSpeed;
        /// <summary>原版 `updateRate`（+0x2c）：**累计多少秒转一次**。0 ⇒ 每帧都转（两个实例都是 0）。</summary>
        public float updateRate;

        /// <summary>原版 `+0x30` 的累加器。**不序列化**（原版同样不序列化 ⇒ `Start` 里清零那一下在原版里等于「保证初值」）。</summary>
        [System.NonSerialized] float accumulator;

        void Start()
        {
            accumulator = 0f;                    // ★ 原版 `IconRotation__Start.c` 全文
        }

        void Update()
        {
            TickForTest(Time.deltaTime);         // 产品路径：唯一的真输入就是 `Time.deltaTime`（体在下面那一处，⛔ 不在这里再抄一份）
        }

        /// <summary>🆕 2026-10-29（B94_FX6）**dt 注入缝（只给自检用）**：把 `Update` 那一段逐句跑一次，
        /// 但用调用方给的 <paramref name="dt"/> 充当那一帧的 `Time.deltaTime`。
        /// <para>⚠️ 为什么必须有它：编辑模式批处理里 `Time.deltaTime` **不是可用的正帧长**（实测让累加器变成
        /// 负值 ⇒ 闸门 `updateRate &lt;= accumulator` 永不开 ⇒ 组件一动不动），自检没法拿它驱动这个状态机。
        /// 先例 = `Editor/B93S3Assert.cs:584-608` 的 `SimpleRotation.Update(euler, speed, dt)` / `LoadingController.Tick(dt)`。</para>
        /// <para>⛔ **原版语义一字未改**：仍然是「累加 dt ⇒ 闸门 ⇒ 归零 + `Rotate`」这一条，只是把 `dt` 那个**输入**
        /// 变成参数（`Update` 转发的是真实帧长）。⛔ 别把闸门改成按秒/按帧率无关 —— 那是我们编的行为。</para></summary>
        public void TickForTest(float dt)
        {
            accumulator += dt;                   // ★ 原版 `+0x30 += Time.deltaTime`
            if (updateRate <= accumulator)       // ★ 原版 `if (period(+0x2c) <= accum)`（**不是** `<`）
            {
                accumulator = 0f;                // ★ 归零（不是减去 period —— 原版就是清 0）
                transform.Rotate(xSpeed, ySpeed, zSpeed);   // ★ 原版 `Rotate(x,y,z,0)`，0 = `Space.Self`
            }
        }
    }
}
