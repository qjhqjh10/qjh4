// WFModuleScaleByTarget.cs — 原版 `AnimFXModuleScaleByTarget` 的对应物
//   **314 实例 / 314 效果**（覆盖面第二大；成表见 `资料/AnimFX_18类成表.md` §二）
//
// 原版语义出处（**逐方法体**，不是照名字猜的）：
//   · 方法体读解：`资料/AnimFX_18类方法体_块1.md` §4
//   · 方法体原文：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//       `AnimFXModuleScaleByTarget__Initialize.c` · `AnimFXModuleScaleByTarget__ChangeShapeAngle.c`
//   · 字段名 / Tooltip：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXModuleScaleByTarget.cs`
//   · 报错原文：`d:/2/tools/all_strings.txt`（`[ERROR] Can't find target or acting card in an AnimFXModule of prefab `
//       · `[ERROR] Missing particle reference in `）
//
// ---- 照方法体还原的（`Initialize`）----
//   ① `base.Initialize(controller)` → **`actingCard` 与 `targetCard` 必须都非空**，否则
//      `LogError("[ERROR] Can't find target or acting card in an AnimFXModule of prefab " + name)` 后 **return**；
//   ② `ratio = targetCard.localScale / actingCard.localScale`（**逐分量**；原版用「× 目标的 scale × (1/出招卡的 scale)」，
//      **除法没有 0 保护**，出招卡某轴 scaled 0 会出 Inf —— 我们照原版不保护）；
//   ③ `foreach (ps in particleSystems)`：**引用为空 → `LogError("[ERROR] Missing particle reference in " + name)` 后跳过这一个**
//      （是 continue，不是中断）；非空则 `ps.transform.localScale *= ratio`；
//   ④ `doParentRelation` 为真 → 原版 `ps.gameObject.AddComponent<ParentConstraint>()` +
//      `Init(targetCard.GetEffectAnchor())` + `ToggleParentingOptions(position: true, rotation: true, scale: false)`
//      （反编译实据：`ToggleParentingOptions(1,1,0,<MethodInfo>)` —— **缩放继承是关的**）；
//   ⑤ 收尾判据 **`particleSystemsShapeAngle != null && Length >= 1` 才调 `ChangeShapeAngle()`**
//      （数组为 null 或长度 0 就不调 —— 出货数据里 217/314 是这种）。
//
// ---- 我们自己定的（原版有、我们这边没有对应物；逐条在下面代码里也标着）----
//   A. **两张卡从哪来**：原版从 `AnimFXController.actingCard/targetCard`（`CardScript`）拿；
//      我们的 `WarpforgeEffectPlayer` **不持有卡片**（只有 `IsRetaliation`）⇒ 本类开两个 `Transform` 字段
//      （手挂 / 由下游钩子 `CardResolver` 填）。**填不出来就照原版那一支 LogError 并 return**（不静默）。
//   B. **`ParentConstraint` 我们没有** ⇒ 用 `SetParent(锚点, worldPositionStays: true)` 替代：
//      位置/旋转跟着锚点走（= 原版的开关组合），`worldPositionStays: true` 保住世界缩放（≈ 原版「不继承缩放」）。
//      ⚠️ **差异**：原版那份是「约束」（把锚点的位置/旋转**贴过来**），父子关系则连**层级**一起变（后面谁在这个
//      节点下找东西，看到的树不一样），而且父级销毁时子物体会跟着没。语义上够用，但**不是逐字段同构**。
//   C. **锚点**：原版是 `targetCard.GetEffectAnchor()`（卡片预制体上一个专用锚点）；我们没有这个概念 ⇒
//      直接用 `targetCard` 本身。
//   D. **`Configure` 里只记引用字符串、不解析**：原版没有 `Configure` 这一步（字段是 Inspector 里连的），
//      我们的数据装配在 `Configure` → `Initialize` 之间注入 `Controller`，而 `@node:` 路径要**从 prefab 根起算**
//      ⇒ 解析一律放到 `Initialize`（那时 `Controller` 才是非 null）。
//
// ---- 🔴 没还原的（**故意留白，不是漏了**）----
//   `ChangeShapeAngle()` 的**角度修正没做**。方法体读了，但**精确操作数配对没能逐位还原**：
//   `FUN_18048a0d0` = `tanf`、`FUN_180486da0` = `atan2f`，两者之间的参数是
//   「`tan(shape.angle × Deg2Rad)`」× 「`targetCard.localScale`」× 「`|target.pos − acting.pos|`」×
//   「`BattleParticleColliderManager` 的 `playerMinionCollider` / `enemyMinionCollider` 两个位置」
//   这四个量的某种组合（Ghidra 把寄存器里的实参丢了，见 `资料/AnimFX_18类方法体_块1.md` §4 与源 .c）。
//   分不清谁配谁 ⇒ 按「**不许编语义**」：这里**不改角度**（`shape.angle` 保持 prefab 里的原值），
//   只做原版循环的**前半段**（解析引用 + 空引用 `LogError`），并打一条警告 + 计数。
//   受影响的规模：**97/314 实例**（`particleSystemsShapeAngle` 非空的那些）的锥角会和原版不一致。
//   💡 想补的人从这条线索下手 —— 那个字段的原版 Tooltip 说明了作者假设：
//      "The original particle must be authorized with the cardprefab at the minion lines distance,
//       one in front of the other" ⇒ **两个 minion collider 之间的距离就是标定时的参考距离**，
//      所以公式多半是「把在参考距离下标定好的 `tan(angle)` 换算到实际距离/缩放」。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleScaleByTarget")]
    public class WFModuleScaleByTarget : WFEffectModule
    {
        [Tooltip("原版 Tooltip：Particle effects that will get the target scale")]
        public ParticleSystem[] particleSystems = new ParticleSystem[0];

        [Tooltip("原版 Tooltip：This will do a parent relation without changing the hierarchy。" +
                 "⚠️ 我们用的是父子挂接（见文件头 B），层级会变。")]
        public bool doParentRelation;

        [Header("Change shape angle by distance and scale")]
        [Tooltip("原版 Tooltip：…The original particle must be authorized with the cardprefab at the minion lines distance, " +
                 "one in front of the other。\n" +
                 "🔴 本类**没实现**角度修正（原版操作数配对没读出来，见文件头「没还原的」）——" +
                 "这里只做引用解析与空引用报错，角度不动。")]
        public ParticleSystem[] particleSystemsShapeAngle = new ParticleSystem[0];

        // ---- 卡片上下文：原版从 controller 拿，我们拿不到（文件头 A）----
        [Tooltip("出招卡（原版 `AnimFXController.actingCard`）。手挂时在 Inspector 里拖；数据装配时由 CardResolver 填。")]
        public Transform actingCard;

        [Tooltip("目标卡（原版 `AnimFXController.targetCard`）")]
        public Transform targetCard;

        /// <summary>下游钩子（可选）：把这次特效的 `actingCard` / `targetCard` 填进上面两个字段。
        /// **不接 = 每实例走原版「找不到卡」那一支**（LogError + return，不会静默）。</summary>
        public static Action<WFModuleScaleByTarget> CardResolver;

        // ---- 诊断计数（自检用；数字不对是事故，要看得见）----
        /// <summary>走进原版「找不到 target / acting card」那一支的次数。</summary>
        public static int MissingCardRefs;
        /// <summary>空粒子引用（原版 LogError 那一支）的次数 —— 按**每一个引用**计。</summary>
        public static int MissingParticleRefs;
        /// <summary>真正做过 `localScale *= ratio` 的次数。</summary>
        public static int ScaleApplied;
        /// <summary>因「角度修正没还原」而**保持原角度**的粒子系统数。</summary>
        public static int ShapeAngleNotApplied;
        static bool _shapeAngleWarned;

        public static void ResetDiagnostics()
        {
            MissingCardRefs = 0; MissingParticleRefs = 0; ScaleApplied = 0;
            ShapeAngleNotApplied = 0; _shapeAngleWarned = false;
        }

        // 数据装配路径的中间态（文件头 D：这里只存字符串，解析在 Initialize）
        string[] _refs = new string[0];
        string[] _angleRefs = new string[0];

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            _refs = ReadRefStrings(def, "particleSystems");
            _angleRefs = ReadRefStrings(def, "particleSystemsShapeAngle");
            doParentRelation = def.GetBool("doParentRelation");
            // ⚠️ 引用**不在 Configure 里解析**（见文件头 D）：这时 `Controller` 还没注入，
            //    而 `@node:` 路径要从 prefab 根起算。
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);

            // 解析 `@node:` 引用。根 = 播放器所在的节点（= prefab 根）；
            // 拿不到播放器（手挂且没走装配）就退到本组件所在节点。
            var root = Controller != null ? Controller.transform : transform;
            if (_refs.Length > 0) particleSystems = ResolveRefs<ParticleSystem>(_refs, root, "ScaleByTarget");
            if (_angleRefs.Length > 0) particleSystemsShapeAngle = ResolveRefs<ParticleSystem>(_angleRefs, root, "ScaleByTarget");

            // ---- ① 两张卡必须都在（原版那一支：LogError + return）----
            if (CardResolver != null) CardResolver(this);          // 我们自己定的（文件头 A）
            if (actingCard == null || targetCard == null)
            {
                MissingCardRefs++;
                Debug.LogError("[ERROR] Can't find target or acting card in an AnimFXModule of prefab " + name);
                return;
            }

            // ---- ② 逐分量比值（**原版不做 0 保护**，我们也不做 —— 见文件头 ②）----
            Vector3 ts = targetCard.localScale;
            Vector3 acts = actingCard.localScale;
            Vector3 ratio = new Vector3(ts.x / acts.x, ts.y / acts.y, ts.z / acts.z);

            // ---- ③ 逐个粒子系统 ----
            for (int i = 0; i < particleSystems.Length; i++)
            {
                var ps = particleSystems[i];
                if (ps == null)
                {
                    MissingParticleRefs++;
                    Debug.LogError("[ERROR] Missing particle reference in " + name);
                    continue;                                   // 原版是 continue（跳过这一个）
                }

                var pt = ps.transform;
                Vector3 s = pt.localScale;
                pt.localScale = new Vector3(s.x * ratio.x, s.y * ratio.y, s.z * ratio.z);
                ScaleApplied++;

                if (doParentRelation)
                {
                    // 原版：AddComponent<ParentConstraint>() + Init(targetCard.GetEffectAnchor())
                    //       + ToggleParentingOptions(position:true, rotation:true, scale:false)
                    // 我们：SetParent(锚点, worldPositionStays:true) —— 见文件头 B / C
                    pt.SetParent(targetCard, true);
                }
            }

            // ---- ⑤ 收尾判据（数组为 null 或长度 0 就不调；我们读出来的数组不会是 null，只在长度上等价）----
            if (particleSystemsShapeAngle.Length >= 1) ChangeShapeAngle();
        }

        /// <summary>原版 `ChangeShapeAngle()`：按「目标距离 + 目标缩放」修正 `shape.angle`。
        /// 🔴 **本类没还原角度修正**（操作数配对没读出来，见文件头「没还原的」）——
        /// 这里只做原版循环的**前半段**：解析引用 + 空引用 `LogError`（原文案）+ 计数 + 一条一次性警告。
        /// **角度原样不动**。</summary>
        void ChangeShapeAngle()
        {
            for (int i = 0; i < particleSystemsShapeAngle.Length; i++)
            {
                if (particleSystemsShapeAngle[i] == null)
                {
                    MissingParticleRefs++;
                    Debug.LogError("[ERROR] Missing particle reference in " + name);
                    continue;                                   // 原版此循环里也是报错后继续
                }
                ShapeAngleNotApplied++;
            }

            if (ShapeAngleNotApplied > 0 && !_shapeAngleWarned)
            {
                _shapeAngleWarned = true;
                Debug.LogWarning("[WarpforgeVFX] ScaleByTarget.ChangeShapeAngle **未还原**" +
                                 "（原版精确操作数配对没读出来，见 WFModuleScaleByTarget.cs 文件头「没还原的」）：" +
                                 "`shape.angle` 保持 prefab 里的原值 —— 这些粒子系统的**锥角会和原版不一致**。" +
                                 "本进程累计受影响粒子系统数：" + ShapeAngleNotApplied);
            }
        }

        // ---- 数据形状工具（**公开**：`WFModuleCollisions` 共用这一份）----
        //
        // ⚠️ 这两只在**这个类**里，是因为本轮 `WFEffectModule.cs` 是**只读**的（别人在改）。
        //    下次谁动基类，把它们挪到 `WFEffectModule` 上去 —— **别在 `WFModuleCollisions` 里抄第二份**
        //    （CLAUDE.md 三·5：两处写同一条规则 = 迟早不一致）。

        /// <summary>扫 `prefix[0]probe`、`prefix[1]probe`… 的**下标个数**（洞也计入，中间空号不截断扫描）。
        /// 两种形状都用它：
        ///   · `prefix="particleSystems", probe=""` → 探 `particleSystems[i]`（元素自己有值）；
        ///   · `prefix="collisionAndParticles", probe=".collisionPlane"` → 元素**自己没有值**、只有元素里的
        ///     字段有值（`Collisions` 就是这种，见 `WFModuleCollisions.Configure`）。
        /// 🔴 **洞要留着**：dump 里解析不出的引用**整个不写**（实测 `NecronGauss_Damaged Hexmark` 少了
        /// `particleSystems[2]`）⇒ 扫到最大下标，洞由调用方当 null 处理
        /// （正好对上原版 `Missing particle reference` 那一支）。</summary>
        public static int CountIndexed(WFModuleDef def, string prefix, string probe)
        {
            if (def == null) return 0;
            int last = -1;
            for (int i = 0; i < 1024; i++)
            {
                if (def.Has(prefix + "[" + i + "]" + probe)) last = i;
                else if (i > last + 1) break;                   // 连着两个空号 = 到头了
            }
            return last + 1;
        }

        /// <summary>按 `key[0]`、`key[1]`… 取引用字符串（洞留空串）。内部就是 `CountIndexed(def, key, "")`。</summary>
        public static string[] ReadRefStrings(WFModuleDef def, string key)
        {
            int n = CountIndexed(def, key, "");
            var arr = new string[n];
            for (int i = 0; i < n; i++) arr[i] = def.GetString(key + "[" + i + "]");
            return arr;
        }

        /// <summary>把引用字符串数组落成组件数组。空串（洞）→ null（**不打警告**，由调用方按语义报）。</summary>
        public static T[] ResolveRefs<T>(string[] refs, Transform root, string who) where T : Component
        {
            var arr = new T[refs != null ? refs.Length : 0];
            for (int i = 0; i < arr.Length; i++)
            {
                if (string.IsNullOrEmpty(refs[i])) continue;
                arr[i] = WFEffectModule.ResolveNode<T>(root, refs[i], who);
            }
            return arr;
        }
    }
}
