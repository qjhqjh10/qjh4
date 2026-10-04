// AutoCardRotation.cs — 整卡的**刚性倾摆**（原版 `AutoCardRotation` 的逐行复刻）
//
//  原版在哪：签名桩 `d:/2/Warpforge_code/Scripts/Assembly-CSharp/AutoCardRotation.cs`
//            + 方法体（**第一权威**）`d:/2/tools/decomp_full/AutoCardRotation__{OnEnable,Update,GetRotation}.c`
//            + 两个常量位：`AutoCardRotation__.cctor.c` / `__.ctor.c`
//  挂在哪：  原版是**两层节点** —— 外层 `CardUI Reference`（布局摆位、转扇形角）→ 内层 `2DCard`
//            （`AutoCardRotation` 挂这里，它写的是**它自己那个节点**的 `localRotation`）。
//            出处 `资料/战斗规格/战斗重建_0827/子代理读报_2dcard_0827.md` 的 A0 全树（`CardUI Reference / 2DCard / Front / …`）
//            + A4 组件表（`| 2DCard | 4458 | 2348604941571499468 | **AutoCardRotation** | rotationSpeed=10, maxXAngle=10, maxYAngle=10 |`）。
//            ⇒ 我们照这个结构来：`CardView` 根 = 外层（`SetPose` 在这里写扇形角），
//              `CardView` 建的 `2DCard` 子节点 = 内层（卡面所有层都在它底下，见 `CardView.Build`）。
//            ⚠️ **这两层不能合成一层**：倾摆写的是 `localRotation`（**绝对值**），
//              合在一起就会把布局摆好的**扇形角**一路 `Lerp` 回 identity（手牌 12.75° 那种倾角会自己站直）。
//
//  行为（一句话 × 三条）：
//    · **跟位移、不跟指针**：每帧取 `transform.position` 与上一帧的差；
//      `x` 位移绕 **`Vector3.down`**、`y` 位移绕 **`Vector3.right`**，各自夹到 ±`maxAngle`，
//      两个旋转**相乘** = 目标旋转 ⇒ **往哪边动，前缘就往远离镜头的方向倒**（和拖一张卡的手感一致）。
//    · 目标旋转用 `Quaternion.Lerp(当前, 目标, rotationSpeed * Time.deltaTime)` 追：
//      **动得越快越歪、停住自己回正**（`maxAngle` 是上限）。
//    · 位移停下后再评估 `EVAL_TIME_AFTER_MOVEMENT`（**2 秒**）就停手 —— 这 2 秒足够 `Lerp` 收回 0。
//
//  数值出处（**不是猜的**）：
//    · `EVAL_TIME_AFTER_MOVEMENT = 2f` ← `.cctor`：`static_fields[0] = 0x40000000`
//    · `rotationSpeed / maxXAngle / maxYAngle = 10 / 10 / 10` ← **prefab 序列化值**：
//      全量扫 `d:/2/新解包资源/assets_full` 里含 `maxYAngle` 的 MonoBehaviour = **153 个实例，
//      153/153 全是 (10, 10, 10)**（`bundle_menus_assets_all` 152 + `bundle_battleprefabs_vfxandmisc_assets_all` 1）。
//      ⚠️ `.ctor` 里那三个默认值 **2 / 15 / 15** 一次都用不到（每个实例都序列化了这三个字段）——
//      别拿构造函数的数当"原版参数"。
//    · 转轴 = `Vector3.down` / `Vector3.right` ← 反编译里那两个静态字段的偏移
//      （`DAT_1842da2a8` = `UnityEngine.Vector3`，`static_fields + 0x24` = `downVector`、
//       `static_fields + 0x3c` = `rightVector`；字段表见 `d:/2/tools/il2cpp_out/dump.cs` 的 Vector3 块，
//       `zeroVector 0x0 / oneVector 0xC / upVector 0x18 / downVector 0x24 / leftVector 0x30 / rightVector 0x3C`）。
//      ⚠️ 那个类指针是**读出来的**、不是猜的：`CustomTypes__SerializeVector3.c` 拿它做
//      `obj.klass == Vector3.klass` 的类型校验（`SerializeQuaternion.c` 同法指向 `UnityEngine.Quaternion`）。

using UnityEngine;

namespace CardPresentation
{
    /// <summary>
    /// 整卡刚性倾摆（原版 `AutoCardRotation`，battlearena1 里 153 个实例）。
    /// 挂在**卡面那一层节点**（`2DCard`）上，不是卡根上 —— 见文件头「两层节点」。
    /// </summary>
    [DisallowMultipleComponent]
    public class AutoCardRotation : MonoBehaviour
    {
        /// <summary>`EVAL_TIME_AFTER_MOVEMENT`（`.cctor` 的 `static_fields[0] = 0x40000000` = **2f**）。
        /// 位移停了之后再评估 2 秒，然后停手 —— 这 2 秒是把倾摆**收回 0** 的那一段。</summary>
        public const float EvalTimeAfterMovement = 2f;

        /// <summary>追目标旋转的快慢（`.ctor` 默认 2，**prefab 值是 10**）。
        /// 原版 `Quaternion.Lerp(当前, 目标, rotationSpeed * Time.deltaTime)` —— 按时间收敛。</summary>
        [SerializeField] float rotationSpeed = 10f;

        /// <summary>`x` 位移那一路的上限角（度）。原版 prefab 值 = **10**（153/153 实例）。</summary>
        [SerializeField] float maxXAngle = 10f;

        /// <summary>`y` 位移那一路的上限角（度）。原版 prefab 值 = **10**（153/153 实例）。</summary>
        [SerializeField] float maxYAngle = 10f;

        // 字段顺序 = 原版桩的顺序（`targetRotation` / `lastPosition` / `cacheTransform` /
        // `currentTimeAfterFinishingMoving`；实例偏移 0x2c / 0x3c / 0x48 / 0x50）。
        Quaternion targetRotation;
        Vector3 lastPosition;
        Transform cacheTransform;
        float currentTimeAfterFinishingMoving;

        // ---- 自检要看的东西（只读；不给外部改）----

        /// <summary>目标旋转（原版 `targetRotation`）—— 自检用。</summary>
        public Quaternion TargetRotation { get { return targetRotation; } }
        /// <summary>三个序列化值（自检钉「原版 10/10/10」用）。</summary>
        public float RotationSpeed { get { return rotationSpeed; } }
        /// <summary>见 <see cref="RotationSpeed"/>。</summary>
        public float MaxXAngle { get { return maxXAngle; } }
        /// <summary>见 <see cref="RotationSpeed"/>。</summary>
        public float MaxYAngle { get { return maxYAngle; } }
        /// <summary>「位移停后还剩几秒」的计时器（原版 `currentTimeAfterFinishingMoving`）—— 自检用。</summary>
        public float IdleTimer { get { return currentTimeAfterFinishingMoving; } }

        void OnEnable()
        {
            // 原版：`cacheTransform = transform; lastPosition = cacheTransform.position;`
            ResetBaseline();
        }

        /// <summary>`OnEnable` 干的那一件事。
        /// ⚠️ **自检也得调它** —— 批处理跑在 **edit mode**，`OnEnable` 不会自己来
        /// （`CardBaseDemo.Run` 就是这种环境），不调的话 `cacheTransform` 是空的。
        /// ⚠️ 它同时是「把当前位置记成上一帧」的那个口：**自检要先摆好位姿、再调它**，
        /// 否则第一帧会拿到一整段假位移。</summary>
        public void ResetBaseline()
        {
            cacheTransform = transform;
            lastPosition = cacheTransform.position;
        }

        void Update()
        {
            if (cacheTransform == null) ResetBaseline();          // 防御（原版直接解引用）
            // ---- 原版那两段把关 ----
            //  transform 动过 ⇒ 计时器重置成 2 秒；没动且计时器已归零 ⇒ **这一帧什么也不做**
            if (cacheTransform.hasChanged) currentTimeAfterFinishingMoving = EvalTimeAfterMovement;
            else if (currentTimeAfterFinishingMoving <= 0f) return;

            Step(Time.deltaTime);

            // 原版显式清一次（`Transform__set_hasChanged(t, false)`）
            cacheTransform.hasChanged = false;
        }

        /// <summary>
        /// **一帧的推演** = 原版 `Update` 的正文（计时器扣减 → 读位移 → 目标旋转 → `Lerp` 回写 → 记位置）。
        ///
        /// ⚠️ 那两处把关（`hasChanged` 与 2 秒计时器）**留在 `Update` 里、不在这一步里** ——
        /// 自检直接调这个函数，喂进去的位移就一定被算，不必去赌批处理下 `hasChanged` 可不可信。
        /// ⚠️ 回写的是 **`cacheTransform.localRotation`（这个节点自己的）** ——
        /// 卡根的扇形角由布局写、两层互不干扰（见文件头）。
        /// </summary>
        public void Step(float dt)
        {
            currentTimeAfterFinishingMoving -= dt;

            Vector3 position = cacheTransform.position;
            // 原版是**两次 `GetRotation` 相乘**（不是欧拉角相加，顺序也不能反）：
            //   `GetRotation(dx, maxXAngle, Vector3.down) * GetRotation(dy, maxYAngle, Vector3.right)`
            targetRotation = TargetRotationFor(position.x - lastPosition.x, position.y - lastPosition.y);
            cacheTransform.localRotation = Quaternion.Lerp(cacheTransform.localRotation, targetRotation,
                                                          rotationSpeed * dt);
            lastPosition = position;
        }

        /// <summary>本组件自己的两个上限角（自检/调试用）。</summary>
        public Quaternion TargetRotationFor(float dx, float dy)
        {
            return TargetRotationFor(dx, dy, maxXAngle, maxYAngle);
        }

        /// <summary>
        /// 位移 → 目标旋转。**纯函数**（自检直接调它）。
        /// 原版 `Update` 里那两行 `GetRotation` 的组合：**先 x 后 y**，中间**不相乘反**。
        /// </summary>
        public static Quaternion TargetRotationFor(float dx, float dy, float maxXAngle, float maxYAngle)
        {
            // 轴不能换：x 位移绕 down、y 位移绕 right（见文件头的偏移证据）。
            // 「往右动 ⇒ 前缘（右边）往后倒、往左动 ⇒ 左边往后倒」——`down` 那个负号就是这件事。
            return GetRotation(dx, maxXAngle, Vector3.down)
                 * GetRotation(dy, maxYAngle, Vector3.right);
        }

        /// <summary>
        /// 单轴：`Quaternion.AngleAxis(Mathf.Clamp(translation * maxAngle, -maxAngle, maxAngle), axis)
        /// * Quaternion.identity`（原版 `AutoCardRotation.GetRotation` 的逐行复刻）。
        ///
        /// ⚠️ 末尾那次乘 `Quaternion.identity` 是**原版真有的一步**（不是笔误也不是多余的化简）：
        ///   反编译里它是 `static_fields[0]` of `UnityEngine.Quaternion` = `identityQuaternion`
        ///   （`dump.cs`：`private static readonly Quaternion identityQuaternion; // 0x0`，值 = (0,0,0,1)）。
        ///   与 identity 相乘**是恒等变换**（逐分量精确相等）⇒ 结果不受影响，写出来只为「照原样」。
        /// </summary>
        public static Quaternion GetRotation(float translation, float maxAngle, Vector3 rotationAxis)
        {
            float angle = Mathf.Clamp(translation * maxAngle, -maxAngle, maxAngle);
            return Quaternion.AngleAxis(angle, rotationAxis) * Quaternion.identity;
        }
    }
}
