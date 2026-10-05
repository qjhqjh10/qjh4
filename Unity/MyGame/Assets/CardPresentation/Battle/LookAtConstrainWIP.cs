// LookAtConstrainWIP.cs — 原版 `LookAtConstrainWIP`（**2026-10-11 · A196 移植**）
//
// 为什么要有它：原版 `TauCannonAnimationStopper.lookAtConstrains[]`（炮塔场 `tauviorla` 的
// `Railgun Turret Base.00N` / `Cylinder.00N` 两个节点各挂一个）就是**这个类**。
// 我们工程里原来**没有它**⇒ 旁挂那两个目标恒解析不到、`Toggle(false)` 第 ① 步不生效并出声。
//
// 判据（第一权威，逐字对照）
// ------------------------------------------------------------------
//   · 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LookAtConstrainWIP.cs`
//     （`public Transform target` · `bool lockXAxis/lockYAxis/lockZAxis` · `Vector3 upVector/rotationOffset`）
//   · 方法体：`d:/2/tools/decomp_full/LookAtConstrainWIP__Update.c`（`__.ctor.c` 是空壳 ——
//     那份反编译把符号名打错了，实际只调 `MonoBehaviour..ctor`，**没有字段默认值**）
//
// `Update()` 逐句对回（`.c` 里的浮点运算是内联展开的四元数乘法，已按乘数配对还原出**调用顺序**）：
//   ```
//   if (target == null) { Debug.LogWarning(…); return; }        // op_Inequality → LogWarning → return
//   dir  = target.position - transform.position;                 // 逐轴相减（x/y/z 三句分开写的）
//   if (lockXAxis) dir.x = 0;  if (lockYAxis) dir.y = 0;  if (lockZAxis) dir.z = 0;
//   Debug.DrawRay(transform.position, dir);                      // 两参重载（第 3 个实参是 MethodInfo*）
//   rot = Quaternion.LookRotation(dir, upVector);
//   rot = rot * AngleAxis(rotationOffset.x, Vector3.right)
//             * AngleAxis(rotationOffset.y, Vector3.up)
//             * AngleAxis(rotationOffset.z, Vector3.forward);
//   transform.rotation = rot;
//   ```
//   🔴 **三条容易抄错的**（都从 `.c` 里逐算子核过，别凭「常识」改）：
//     ① 三个 `AngleAxis` 的**轴**取自 `UnityEngine.Vector3` 的静态字段块（`+0x3c`/`+0x18`/`+0x48`），
//        按字段布局依次是 **right / up / forward** —— 与 `rotationOffset` 的 **x / y / z** 一一对应；
//     ② 三个 `AngleAxis` 与 `LookRotation` 的**乘序**是 `look * rotX * rotY * rotZ`（左乘、不是右乘、
//        也不是 ZYX）—— 四元数乘法不满足交换律，抄错一个顺序画面就偏；
//     ③ 锁轴是**置零那一个分量**（`dir.x = 0` 这种），不是「忽略」、也不是「投影到平面再归一化」。
//
// 资产侧实测（`bundle_scenes_scenes_battlearenatauviorla/MonoBehaviour/MonoBehaviour_{5050,5261,5556,5639}.json`，
// 4 个实例 · 2 个 `TauCannonAnimationStopper` 各 2 个）：
//   | 宿主 GameObject          | `target`                      | lockX | lockY | lockZ | upVector   | rotationOffset  |
//   |--------------------------|-------------------------------|-------|-------|-------|------------|-----------------|
//   | `Railgun Turret Base.001`| `Railgun Turret 1 Target `    | 0     | **1** | 0     | (0,1,0)    | (-90, 0, 90)    |
//   | `Cylinder.001`           | `Railgun Turret 1 Target `    | 0     | 0     | 0     | (0,1,0)    | (0, 90, 0)      |
//   | `Railgun Turret Base.002`| `Railgun Turret 2 Target`     | 0     | **1** | 0     | (0,1,0)    | (-90, 0, 90)    |
//   | `Cylinder.003`           | `Railgun Turret 2 Target`     | 0     | 0     | 0     | (0,1,0)    | (0, 90, 0)      |
//   （`Railgun Turret 1 Target ` 那个名字**原版就带一个尾随空格** —— 照抄，别「顺手修好」。）
//   这些值由 `工具/gen_env_blendables.py` 的 `TARGET_FIELDS['LookAtConstrainWIP']` 收进旁挂，
//   运行时由 `ScenarioBlendables` 的工厂**就地建组件**并逐字段填（见那里的 `MakeLookAtConstrains`）。
//
// ⚠️ 原版 `Update()` 里那句 `Debug.LogWarning` 的**文案没读出来**（如实记着，别当成已知）：
//   `.c` 里实参是 `DAT_184282b70`，其 8 字节内容 = `0x00000000A000791D` —— 这是 il2cpp 的
//   **元数据 usage 编码**（高位 `>>29` = 5 = StringLiteral），解出的字面量下标 **31005**
//   **超出了字符串字面量表**（`global-metadata.dat` v31：`stringLiteral` 256/212112 ⇒ **26514 条**）
//   ⇒ **这个解码法不对，原文未知**。按铁律 2/3：**不猜原文**，这里写我们自己的措辞。
//   走到这句只可能是**有人手挂了组件而没给 target**（工厂那条路解析不到 target 时**根本不建**）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `LookAtConstrainWIP`（`Assembly-CSharp`，全局命名空间）—— 「让本对象一直看着 `target`」。
    /// 类名里的 `WIP` 是原版自己带的（work-in-progress），**不是我们加的**。</summary>
    public class LookAtConstrainWIP : MonoBehaviour
    {
        /// <summary>原版 `target`（Transform）。为空 ⇒ `Update()` 每次 `LogWarning` 后直接 return（照抄）。</summary>
        public Transform target;

        /// <summary>锁 X 轴：把 `dir.x` 置 0（三个轴各自独立，可同时锁）。</summary>
        public bool lockXAxis;
        /// <summary>锁 Y 轴：把 `dir.y` 置 0（4 个实例里 2 个开了它）。</summary>
        public bool lockYAxis;
        /// <summary>锁 Z 轴：把 `dir.z` 置 0。</summary>
        public bool lockZAxis;

        /// <summary>`Quaternion.LookRotation(dir, upVector)` 的第 2 个实参。4 个实例**全是 (0,1,0)**。</summary>
        public Vector3 upVector;

        /// <summary>LookRotation 之后再按 **x→right / y→up / z→forward** 各转一次（**左乘**，见文件头 ②）。</summary>
        public Vector3 rotationOffset;

        /// <summary>判据 = `LookAtConstrainWIP__Update.c`（逐句见文件头）。</summary>
        void Update() { Aim(); }

        /// <summary>🆕 **我们加的入口**（原版只有 `Update()`，没有这个成员 —— 如实记着）：
        /// **批处理下没有帧循环**（CLAUDE.md §三）⇒ 自检要能**手动推一帧**才验得了「转到位了没有」。
        /// 实时那条路走 `Update()` → 这里，**同一份实现**（别写第二份）。</summary>
        public void Aim()
        {
            if (target == null)
            {
                // ⚠️ 原版文案**没读出来**（见文件头那条）；这里用我们自己的措辞，语义与原版一致：
                //    「没 target 就出声，然后这一帧什么都不做」。
                Debug.LogWarning($"[EnvBlend] `LookAtConstrainWIP`({name})：`target` 是空的 —— "
                               + "原版这一句就是 `Debug.LogWarning` 后直接 return（这一帧不转向）。"
                               + "⚠️ 原版那句文案**没能从元数据里读出来**（解码下标越界），这是我们的措辞");
                return;
            }

            Vector3 dir = target.position - transform.position;
            if (lockXAxis) dir.x = 0f;
            if (lockYAxis) dir.y = 0f;
            if (lockZAxis) dir.z = 0f;

            // 原版那两参重载（`.c` 里第 3 个实参是 MethodInfo*，不是颜色）
            Debug.DrawRay(transform.position, dir);

            Quaternion rot = Quaternion.LookRotation(dir, upVector)
                           * Quaternion.AngleAxis(rotationOffset.x, Vector3.right)
                           * Quaternion.AngleAxis(rotationOffset.y, Vector3.up)
                           * Quaternion.AngleAxis(rotationOffset.z, Vector3.forward);
            transform.rotation = rot;
        }
    }
}
