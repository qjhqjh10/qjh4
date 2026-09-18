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
// ---- ✅ `ChangeShapeAngle()` 的角度修正（2026-09-18 还原，照 VA 反汇编）----
//   公式（**逐字来自指令流**，三个常量都实测过；出处 `资料/AnimFX_实现与接线.md` §11.6 c-2）：
//     shape.angle = atan2( tan(旧角 × Deg2Rad) × 兵线距 × **目标卡**.localScale.x ,
//                          两卡 3D 距离 ) × Rad2Deg
//   · 反汇编工具：`工具/disasm_va.py`（VA `0x180668E00` 起）+ `工具/resolve_va.py`（地址→方法名）。
//     照出的真名：`Transform.get_position` / `get_localScale` / `ShapeModule.get_angle` / `set_angle`
//     / `CustomDebug.LogError` ⇒ **读写的确实是锥角，单位是度**。
//   · 三个常量：`Deg2Rad 0.0174533`（`0x1834B2DC0`）· `Rad2Deg 57.2958`（`0x1834B2E98`）·
//     abs 掩码 `FF FF FF 7F`（`0x1834B2E60`，用在「兵线距 = |玩家线 − 敌方线|」上 —— **只取 Z 轴**）。
//   · `+0x58 = AnimFXController.targetCard` / `+0x50 = actingCard`（`dump.cs:47987-47988` 字段名坐实）
//     ⇒ 位置与 `localScale` **都取目标卡那侧**。
//   · ⚠️ **原版把 `shape.angle` 读回来再写回去**（同一个对象）⇒ **重复调用会累积**。
//     我们照原版；调用点只有 `Initialize` 一处，且每个效果实例都从 prefab 的原始角开始。
//   · ⚠️ **原版那一支是 `CustomDebug.LogError` + 跳过**，我们是「计数 + 一次性警告」（不静默）。
//   · ⚠️ 跨行目标在原版是 **3D 深度**，我们 2D 只能给**投影距离**（残余风险①，照 d) 一律取投影量）。
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
                 "✅ 角度修正 2026-09-18 已照反汇编还原（公式见文件头）——" +
                 "需要 `MinionLines` 钩子提供两条兵线；没接就退回「保持原角 + 计数 + 一次性警告」。")]
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
        /// <summary>真的按原版公式改过 `shape.angle` 的粒子系统数（✅ 2026-09-18 起不再是 0）。</summary>
        public static int ShapeAngleApplied;
        /// <summary>因为**两条兵线没接上**而没能改角度的粒子系统数（原版这是「缺引用」那一支）。</summary>
        public static int ShapeAngleNotApplied;
        static bool _shapeAngleWarned;

        public static void ResetDiagnostics()
        {
            MissingCardRefs = 0; MissingParticleRefs = 0; ScaleApplied = 0;
            ShapeAngleApplied = 0; ShapeAngleNotApplied = 0; _shapeAngleWarned = false;
        }

        /// <summary>两条**兵线中心**（原版 = `BattleParticleColliderManager` 上 `playerMinionCollider` /
        /// `enemyMinionCollider` 两个 `Transform` 的 `position` —— 锥角**只用这两个**，见文件头 b)。
        /// 返回 false = 没接 / 拿不到 ⇒ 走原版「缺引用」那一支（计数 + 一次性警告，**不静默**）。
        /// 🔑 **我们不另摆空物体**：兵线的定义本来就在 `BoardLayout` 上，`SlotPosition(4)` 就是那个点
        /// （X 对齐督军槽中心）—— 两行中心线相距 **0.2241 归一化 × `LayoutSpace.DesignHeight`(10)
        /// = 2.241 我们世界单位**，与原版屏上那 **242 px** 是同一个距离（出处 §11.6 d)）。</summary>
        public delegate bool MinionLineProvider(out Vector3 playerLine, out Vector3 enemyLine);
        public static MinionLineProvider MinionLines;

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

        /// <summary>原版 `ChangeShapeAngle()`：按「**兵线距** × 目标卡缩放」把 `shape.angle` 换算到实际距离。
        /// 公式、常量、字段归属与三条残余风险见**文件头**与 `资料/AnimFX_实现与接线.md` §11.6 c-2。
        /// ⚠️ 原版是「读回 `shape.angle` 再写回同一个对象」⇒ 重复调用会累积；调用点只有 `Initialize` 一处。</summary>
        void ChangeShapeAngle()
        {
            Vector3 pLine, eLine;
            if (MinionLines == null || !MinionLines(out pLine, out eLine))
            {
                // 原版这一支是「缺引用」：`CustomDebug.LogError` + 跳过。我们照旧不静默 ——
                // 改成**计数 + 一次性警告**（自检读计数，屏幕上不刷屏）。
                for (int i = 0; i < particleSystemsShapeAngle.Length; i++)
                    if (particleSystemsShapeAngle[i] != null) ShapeAngleNotApplied++;

                if (ShapeAngleNotApplied > 0 && !_shapeAngleWarned)
                {
                    _shapeAngleWarned = true;
                    Debug.LogWarning("[WarpforgeVFX] ScaleByTarget.ChangeShapeAngle 拿不到**两条兵线**" +
                                     "（`WFModuleScaleByTarget.MinionLines` 没接，原版这是「缺引用」那一支）" +
                                     "⇒ `shape.angle` 保持 prefab 里的原值，**锥角会和原版不一致**。" +
                                     "本进程累计受影响粒子系统数：" + ShapeAngleNotApplied);
                }
                return;
            }

            // 两个距离**都取世界系**（原版两个量也在同一个世界系里）。兵线距是常量，卡距逐实例变。
            float lineD = Vector3.Distance(pLine, eLine);
            float cardD = Vector3.Distance(targetCard.position, actingCard.position);
            float scaleX = targetCard.localScale.x;                 // 🔴 目标卡那侧，不是出招卡

            for (int i = 0; i < particleSystemsShapeAngle.Length; i++)
            {
                var ps = particleSystemsShapeAngle[i];
                if (ps == null)
                {
                    MissingParticleRefs++;
                    Debug.LogError("[ERROR] Missing particle reference in " + name);
                    continue;                                   // 原版此循环里也是报错后继续
                }

                var shape = ps.shape;
                float slope = Mathf.Tan(shape.angle * Mathf.Deg2Rad);          // 读 prefab 里的标定角（度）
                shape.angle = Mathf.Atan2(slope * lineD * scaleX, cardD) * Mathf.Rad2Deg;
                ShapeAngleApplied++;
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
