// WFModuleChangeVelocity.cs — 原版 `AnimFXModuleChangeVelocity` 的对应物
//   **0 实例 / 0 效果** —— 18 个类里**唯一在出货数据中完全没挂载**的一个（块2 §7）。
//   虽然现在没有任何效果用它，**弹道数学照写**：它是这套模块里唯一含实体物理算法的东西，
//   将来做「抛射物从 A 格飞到 B 格」时就是现成公式（块2 §7 末尾原话）。
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§7**（6 个方法体）
//   · `Initialize(controller)`（`decomp_out_ai/`）：
//     `heroA = BattleManager.GetHero(true)` / `heroB = BattleManager.GetHero(false)`；
//     任一为 null → `LogError("[ERROR] Can't find player or enemy hero")` **并 return**；
//     `d1 = |heroA.pos − heroB.pos|`；`d2 = |actingCard(+0x50).pos − targetCard(+0x58).pos|`；
//     然后 foreach `particleSystems`（元素 16 字节结构：`ParticleSystem` + `bool applyToVelocityModule`）
//     → `ModifyVelocity(this.transform, d1, d2)`
//   · `ModifyVelocity(Transform, d1, d2)`：`applyToVelocityModule == true` →
//     `Debug.LogError("[ERROR] Not implemented yet. Ask Cesar for implementation")`（**原版自己没实现这条分支**，
//     字符串表核过）；否则
//     `H = CalculateMaxHeight(main.startSpeed.constantMax, shape.rotation.x)` →
//     `CalculateSecondProjectile(H, main.startLifetime.constantMax, d2, out v, out ang)` →
//     `shape.rotation.x = sign(旧 rotation.x) * ang` → `main.startSpeed.constantMax = v`
//     ⚠️ **`d1`（督军间距）在这里读了没用** —— 传进来就算了，不参与计算（块2 §7 明写）。
//   · `CalculateMaxHeight(v0, angle)`：
//     `H = (lossyScale.x · v0)² · sin²(angle·Deg2Rad) / (2 · |Physics.gravity.y × main.gravityModifier.constantMax|)`
//   · `CalculateSecondProjectile(H, t, d2, out v, out ang)`：
//     `g' = |gravity.y × gravityModifier.constantMax|`；`w = sqrt(2·g'·H)`；
//     `ang = atan2(w·t, d2) · Rad2Deg`；`v = (d2 / (tan(ang·Deg2Rad) · t)) / lossyScale.x`
//   · `ModifyVelocity(float multiplier, Transform)`：同样「`applyToVelocityModule` 为真就 LogError」，
//     否则 `main.startSpeed.constantMax *= multiplier / transform.lossyScale.x`
//   · **常量全部是从 `GameAssembly.dll` 读出来的**（块2 §7）：`0.0174533`(Deg2Rad) · `57.29578`(Rad2Deg) ·
//     `0x7FFFFFFF`(取绝对值掩码) · `±1.0`(sign)。**没有一条是猜的。**
//   · 上面四个内部方法体是 **2026-09-17 补的反编译**（0827 那批里没有），落点 `d:/2/tools/decomp_cv_0917/`
//     （Ghidra `ok=4 fail=0`；完整命令见块2 文末）。没有这批就只能写「查不到」。
//
// 照方法体还原了什么：
//   · 四条分支的**公式一字不改**（含 `d1` 读了不用这个事实、`sign(旧角度)` 保方向、`/ lossyScale.x`）
//   · `applyToVelocityModule == true` 那条**照样只打那条错误日志**（原版即未实现，不替它编一套）
//   · Initialize 的督军判空 + 那句 `[ERROR] Can't find player or enemy hero`
//
// 我们定的（显式标出）：
//   · **四条公式做成纯函数**（`static`，把 `lossyScale.x` 与 `gravityY × gravityModifier` 显式传参）：
//     原版是从成员上取这两个值的（`ParticleSystemVelocity` 的内嵌方法），我们拆开只为可单测，
//     算式与常量不动。
//   · **`g' == 0` 的保护**：原版那条除法在 `gravityModifier = 0`（或重力为 0）时是 **÷0 → ∞/NaN**，
//     粒子直接飞丢。我们改成「**跳过 + 一次性警告**」（`Mathf.Approximately(gTimes, 0f)`）。
//     这是安全兜底，**不是原版行为** —— 纯函数 `CalculateMaxHeight/CalculateSecondProjectile` 仍照原式除。
//   · **输入从哪来**：原版从 `BattleManager.GetHero` / `controller.actingCard` / `targetCard` 拿位置，
//     我们这边**没有牌局层**（`WarpforgeEffectPlayer` 里没有 actingCard/targetCard，也没有 BattleManager）
//     ⇒ 留三个静态钩子（`GetHeroPosition` / `ActingCardPosition` / `TargetCardPosition`）＋
//     一个公开入口 `ApplyFromContext()`。没人设钩子时**打 LogError 说出来**（照原版那句报错的位置）。
//
// 🔴 未还原（两条）：
//   ① **数据键名无从核对**：出货数据里这个类 **0 实例** ⇒ 扁平化器从没跑过它，
//      `particleSystems[i].particleSystem` / `particleSystems[i].applyToVelocityModule` 这两个键名是
//      **按原版字段名写的**（块2 §7 的结构体成员名），**没有任何实例可验证**。
//      将来原版数据补进来发现名字不同，改 `ResolveSystems()` 里那两行即可。
//   ② **相机/坐标层**：位置钩子给的是世界坐标，原版那边是 `actingCard`/`targetCard` 的
//      `transform.position`（我们**查不到**这两个对象在我们棋盘里的对应物）⇒ 谁装钩子谁负责给对坐标系。
//   ③ 🆕 **2026-09-18：`ModifyVelocity` 少写两个值（未改，留作 TODO）。**
//      全量反编译复核（`AnimFX_实现与接线.md` §十一 的建议②）发现：原版
//      `AnimFXModuleChangeVelocity.ParticleSystemVelocity__ModifyVelocity.c:131-137` 用**同一个比例**
//      `v / 旧 constantMax` **同时**写了 `set_x1(...)` 与 `set_outWeight(...)`，`:146` 才 `set_startSpeed`
//      ⇒ 原版是「**按比例缩放整条 `startSpeed` 曲线的两个端点**」，我们这里只写了 `speed.constantMax`。
//      🔴 **没改的原因**：那**两个 setter 对应 `MinMaxCurve` 的哪两个成员没判定出来**
//      （Ghidra 猜的名字是 `HableCurve.set_x1` / `Keyframe.set_outWeight`，**不可信**）。
//      凭猜改字段名 = 把一个「0 实例、今天零影响」的偏差换成一个**可能真错**的实现。
//      **要做的话**：先把那两条指令的操作数绑到 `MinMaxCurve`/`Keyframe` 的具体成员，再改。
//      ⚠️ 影响面：**该模块出货数据 0 个实例** ⇒ 今天零影响。
using System;
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleChangeVelocity")]
    public class WFModuleChangeVelocity : WFEffectModule
    {
        /// <summary>原版从 DLL 读出来的 `Deg2Rad`（块2 §7）。</summary>
        public const float Deg2Rad = 0.0174533f;
        /// <summary>原版从 DLL 读出来的 `Rad2Deg`（块2 §7）。</summary>
        public const float Rad2Deg = 57.29578f;

        /// <summary>原版内嵌结构 `ParticleSystemVelocity`（16 字节 = `ParticleSystem` + `bool`）。</summary>
        [Serializable]
        public class ParticleSystemVelocity
        {
            [Tooltip("原版 `particleSystem`。数据里是 `@node:ParticleSystem:…`（键名无从核对，见文件头）")]
            public ParticleSystem particleSystem;

            [Tooltip("原版 `applyToVelocityModule`。**为真时原版自己也没实现**（只打一条 LogError）")]
            public bool applyToVelocityModule;
        }

        [Tooltip("原版 `particleSystems`（数组，元素 = 粒子系统 + 那个开关）")]
        public ParticleSystemVelocity[] particleSystems = new ParticleSystemVelocity[0];

        // ---- 我们定的钩子（原版从 BattleManager / AnimFXController 拿，我们没有牌局层）----
        /// <summary>`true`/`false` = 我方/敌方督军的世界坐标（`null` = 不知道）。原版 `BattleManager.GetHero`。</summary>
        public static Func<bool, Vector3?> GetHeroPosition;
        /// <summary>原版 `controller.actingCard.transform.position`。</summary>
        public static Vector3? ActingCardPosition;
        /// <summary>原版 `controller.targetCard.transform.position`。</summary>
        public static Vector3? TargetCardPosition;

        /// <summary>清掉钩子（测试 / 换局用）。</summary>
        public static void ResetContext()
        {
            GetHeroPosition = null;
            ActingCardPosition = null;
            TargetCardPosition = null;
        }

        /// <summary>标定过几次参数 / 因为拿不到上下文而放弃几次（自检用）。</summary>
        public static int AppliedCount;
        public static int MissingContextCount;
        public static void ResetDiagnostics() { AppliedCount = 0; MissingContextCount = 0; }

        WFModuleDef _def;
        bool _warnedZeroGravity, _warnedNoContext;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            _def = def;
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            ResolveSystems();
            if (particleSystems == null || particleSystems.Length == 0) return;   // 原版 foreach 空数组 = 什么都不做
            ApplyFromContext();
        }

        /// <summary>原版 `Initialize` 的后半段：算 d1/d2 然后逐条标定。**可以手动再调**（目标变了就重标一次）。</summary>
        public void ApplyFromContext()
        {
            Vector3? heroA = GetHeroPosition != null ? GetHeroPosition(true) : null;
            Vector3? heroB = GetHeroPosition != null ? GetHeroPosition(false) : null;
            if (heroA == null || heroB == null)
            {
                // 照原版这句报错（原版：任一为 null 就 LogError 并 return）
                Debug.LogError("[ERROR] Can't find player or enemy hero —— 原版这句话的出处在块2 §7；" +
                               "我们这边是 `WFModuleChangeVelocity.GetHeroPosition` 没设（或返回了 null）");
                MissingContextCount++;
                return;
            }
            if (ActingCardPosition == null || TargetCardPosition == null)
            {
                MissingContextCount++;
                if (!_warnedNoContext)
                {
                    _warnedNoContext = true;
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleChangeVelocity 拿不到 " +
                                     "`actingCard` / `targetCard` 的位置（原版从 AnimFXController 上取）⇒ " +
                                     "弹道不标定。要它工作：设 `ActingCardPosition` / `TargetCardPosition`，" +
                                     "再调 `ApplyFromContext()`。");
                }
                return;
            }

            // d1 = 两个督军的间距（**原版读了不用**，见文件头）；d2 = 出手卡与目标卡的间距（真正参与计算）
            float d1 = Vector3.Distance(heroA.Value, heroB.Value);
            float d2 = Vector3.Distance(ActingCardPosition.Value, TargetCardPosition.Value);
            ModifyVelocity(transform, d1, d2);
            AppliedCount++;
        }

        /// <summary>原版 `ModifyVelocity(Transform, float d1, float d2)`：逐条把初速/角度改成能打到 `d2` 的抛物线。
        /// ⚠️ `d1` 只为了对齐签名而存在，**公式里不用它**（照原版）。</summary>
        public void ModifyVelocity(Transform t, float d1, float d2)
        {
            if (particleSystems == null) return;
            var self = t != null ? t : transform;
            float scaleX = self != null ? self.lossyScale.x : 1f;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var e = particleSystems[i];
                if (e == null || e.particleSystem == null) continue;

                if (e.applyToVelocityModule)
                {
                    // 🔴 原版这条分支**自己就没实现**（块2 §7：字符串表核过的原文），照原版只报错
                    Debug.LogError("[ERROR] Not implemented yet. Ask Cesar for implementation" +
                                   "（原版即未实现：`applyToVelocityModule=true` 那条分支，见块2 §7；我们照原版保留）");
                    continue;
                }

                var ps = e.particleSystem;
                var main = ps.main;
                var shape = ps.shape;

                float gTimes = Physics.gravity.y * main.gravityModifier.constantMax;
                if (Mathf.Approximately(gTimes, 0f))
                {
                    // 我们定的兜底：原版这里 ÷0 → ∞/NaN（粒子飞丢），我们跳过并报警
                    if (!_warnedZeroGravity)
                    {
                        _warnedZeroGravity = true;
                        Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleChangeVelocity：" +
                                         $"`gravity.y × gravityModifier` = 0 ⇒ 原版这条会 ÷0（∞/NaN），" +
                                         "我们跳过这个粒子系统（兜底是我们加的，不是原版行为）。");
                    }
                    continue;
                }

                float oldRotX = shape.rotation.x;
                float H = CalculateMaxHeight(main.startSpeed.constantMax, oldRotX, scaleX, gTimes);

                float v, ang;
                CalculateSecondProjectile(H, main.startLifetime.constantMax, d2, scaleX, gTimes, out v, out ang);

                // 角度**保方向**（原版 `sign(旧 rotation.x) * ang`）
                shape.rotation = new Vector3(Mathf.Sign(oldRotX) * ang, shape.rotation.y, shape.rotation.z);

                var speed = main.startSpeed;
                speed.constantMax = v;
                main.startSpeed = speed;
            }
        }

        /// <summary>原版 `ModifyVelocity(float multiplier, Transform)`：整体缩放初速。</summary>
        public void ModifyVelocity(float multiplier, Transform t)
        {
            if (particleSystems == null) return;
            var self = t != null ? t : transform;
            float scaleX = self != null ? self.lossyScale.x : 1f;

            for (int i = 0; i < particleSystems.Length; i++)
            {
                var e = particleSystems[i];
                if (e == null || e.particleSystem == null) continue;

                if (e.applyToVelocityModule)
                {
                    Debug.LogError("[ERROR] Not implemented yet. Ask Cesar for implementation" +
                                   "（原版即未实现：`applyToVelocityModule=true` 那条分支，见块2 §7）");
                    continue;
                }

                var main = e.particleSystem.main;
                var speed = main.startSpeed;
                speed.constantMax *= multiplier / scaleX;
                main.startSpeed = speed;
            }
        }

        // ---- 弹道数学（块2 §7，2026-09-17 补的反编译；公式与常量一字不改）----

        /// <summary>原版 `ParticleSystemVelocity.CalculateMaxHeight(v0, angle)`：抛体**最高点**。
        /// `H = (scale·v0)² · sin²(angle·Deg2Rad) / (2·|g'|)`。
        /// ⚠️ 分母为 0 时原版就是 ∞/NaN —— 保护做在调用方 `ModifyVelocity`（见文件头「我们定的」）。</summary>
        public static float CalculateMaxHeight(float v0, float angleDeg, float scaleX, float gravityYTimesModifier)
        {
            float num = scaleX * v0;
            num = num * num;                                     // (scale · v0)²
            float s = Mathf.Sin(angleDeg * Deg2Rad);
            num = num * (s * s);                                 // · sin²(angle)
            float den = 2f * Mathf.Abs(gravityYTimesModifier);    // 2 · |g'|
            return num / den;
        }

        /// <summary>原版 `ParticleSystemVelocity.CalculateSecondProjectile(H, t, d2, out v, out ang)`：
        /// 由最高点 `H` 与飞行时间 `t` 反解出「能打到 `d2`」的发射角与初速。
        /// `w = sqrt(2·g'·H)`；`ang = atan2(w·t, d2)·Rad2Deg`；`v = (d2/(tan(ang·Deg2Rad)·t))/scale`。
        /// （原版从成员上取 `g'` 与 `lossyScale.x`，我们显式传参 —— 算式没动。）</summary>
        public static void CalculateSecondProjectile(float H, float lifetime, float d2,
                                                     float scaleX, float gravityYTimesModifier,
                                                     out float v, out float angleDeg)
        {
            float g = Mathf.Abs(gravityYTimesModifier);
            float w = Mathf.Sqrt(2f * g * H);
            angleDeg = Mathf.Atan2(w * lifetime, d2) * Rad2Deg;
            float tan = Mathf.Tan(angleDeg * Deg2Rad);
            v = (d2 / (tan * lifetime)) / scaleX;
        }

        /// <summary>把 `particleSystems[i]` 解析成粒子系统。
        /// 🔴 键名**无从核对**（这个类出货数据里 0 实例，见文件头「未还原 ①」）。</summary>
        void ResolveSystems()
        {
            if (_def == null) return;                     // 手挂的：Inspector 里就是真的
            var list = new List<ParticleSystemVelocity>();
            if (particleSystems != null)
                foreach (var e in particleSystems) if (e != null) list.Add(e);

            if (list.Count == 0)
            {
                for (int i = 0; ; i++)
                {
                    string p = "particleSystems[" + i + "]";
                    string refv = _def.GetString(p + ".particleSystem");
                    if (string.IsNullOrEmpty(refv)) refv = _def.GetString(p);   // 兜底：整项就是引用
                    if (string.IsNullOrEmpty(refv)) break;
                    list.Add(new ParticleSystemVelocity
                    {
                        particleSystem = ResolveNode<ParticleSystem>(transform, refv, "AnimFXModuleChangeVelocity"),
                        applyToVelocityModule = _def.GetBool(p + ".applyToVelocityModule"),
                    });
                }
            }

            if (list.Count == 0)
            {
                Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleChangeVelocity 没解析出粒子系统" +
                                 "（数据键 `particleSystems` —— 键名无从核对，见文件头）⇒ 它什么都不会做。");
                return;
            }
            particleSystems = list.ToArray();
        }
    }
}
