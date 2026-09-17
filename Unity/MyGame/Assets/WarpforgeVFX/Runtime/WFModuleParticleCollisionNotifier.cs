// WFModuleParticleCollisionNotifier.cs — 原版 `AnimFXParticleCollisionNotifier`
//   **数据里 0 实例**：它不是 prefab 里作者挂的组件，而是 `WFModuleCollisions` 在运行时
//   `ps.gameObject.AddComponent<…>()` + `Register(this)` 造出来的（`资料/AnimFX_18类成表.md` §四）。
//
// 原版语义出处（**三个方法体全读了**）：
//   · 方法体原文：`d:/2/Warpforge_tools/data/decomp_il2cpp_0827/decomp_out_ai/`
//       `AnimFXParticleCollisionNotifier__.ctor.c` · `...__Register.c` · `...__OnParticleCollision.c`
//   · 方法体读解：`资料/AnimFX_18类方法体_块2.md` §8 · 字段/签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXParticleCollisionNotifier.cs`
//
// ---- 照方法体还原的 ----
//   ① `.ctor` → `collisionsModules = new List<…>()`；
//   ② `Register(def)` → **就是 `List.Add`**（原版走的是 `List.Add` 的扩容路径，**没有去重、没有过滤**）；
//   ③ `OnParticleCollision(other)` → `foreach (def in collisionsModules) if (def.receiveCollisionMessage)
//      def.collisionEvent.Invoke()`；
//   ④ 🔴 **`other` 参数完全没用**（原版如此）：**不筛粒子系统、不筛撞到的是谁** ——
//      任何粒子撞任何东西，都会把列表里所有开了开关的事件全点一遍。签名保留 `other` 只为收 Unity 的消息。
//
// ---- 我们自己定的（逐条在下面代码里也标着）----
//   A. **刻意不继承 `WFEffectModule`**：原版它也不是 `AnimFXModuleBase` 的子类（没有 `actionStart`、
//      不由 `AnimFXController` 广播、数据里 0 实例）⇒ 这里就是个普通 `MonoBehaviour`。
//      ⇒ 也**不该**贴 `[WFModuleKind]`（`WFModuleFactory` 只登记 `WFEffectModule` 的子类，贴了不生效）。
//   B. **广播转发给 `def.OnParticleCollision()`**：原版 notifier 是直接 `def.collisionEvent.Invoke()`，
//      而 `ParticleCollisionDefinition.OnParticleCollision()` 里是**逐字相同**的那两句
//      ⇒ 只写一份（CLAUDE.md 三·5：两处写同一条规则 = 迟早不一致）。
//   C. **`Broadcast()` 这个公开入口是我们加的**（原版只有私有的 Unity 消息回调）：批处理 / 无帧循环下
//      `OnParticleCollision` 根本不会来，要验就得能手动触发（同 `WarpforgeEffectPlayer.Tick(dt)` 的思路）。
//   D. **诊断计数 `Registered` / `Broadcasts` / `SkippedNulls`**：**纯记账、不改行为**。
//      原版「事件没有订阅者就什么都不发生」是**静默**的（`UnityEvent.Invoke()` 对空监听列表没反应）——
//      **原版此处即静默**，所以我们不改行为，只把「广播了几次」记下来，好让下一个会话说得清。
//
// ⚠️ 两条环境事实（这条线踩过 / 一定会踩）：
//   · 要让这条消息来，得同时满足：粒子系统开了 `collision.sendCollisionMessages`（**本组件就是
//     `Collisions` 模块开它的时候顺手挂上来的**）+ 应用里有碰撞体（我们再核）。
//   · **批处理 / 无帧循环下这条消息不会来**（粒子要手动 `Simulate`，见 CLAUDE.md 三）⇒ 用 `Broadcast()`。
using System.Collections.Generic;
using UnityEngine;

namespace WarpforgeVFX
{
    [DisallowMultipleComponent]
    public class WFModuleParticleCollisionNotifier : MonoBehaviour
    {
        /// <summary>原版 `collisionsModules`（字段名照搬）。原版是 private，我们公开只为自检 / 手查。</summary>
        public List<WFModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition> collisionsModules =
            new List<WFModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition>();

        // ---- 诊断计数（文件头 D：纯记账，不改行为）----
        /// <summary>`Register` 成功的次数（**含重复注册** —— 原版不去重）。</summary>
        public static int Registered;
        /// <summary>广播过的 def 数（每次 Invoke 记一次）。</summary>
        public static int Broadcasts;
        /// <summary>遇到空 def 的次数（注册时传 null / 列表里有 null）——**原版这两处都会 NRE**。</summary>
        public static int SkippedNulls;

        public static void ResetDiagnostics() { Registered = 0; Broadcasts = 0; SkippedNulls = 0; }

        /// <summary>原版 `Register(ParticleCollisionDefinition)` = `collisionsModules.Add(def)`。
        /// ⚠️ **不去重**（原版如此）：同一个 def 被 Initialize 两次，列表里就有两条、事件被点两次。</summary>
        public void Register(WFModuleCollisions.CollisionAndParticles.ParticleCollisionDefinition collisionsModule)
        {
            if (collisionsModule == null)
            {
                // 🔴 原版这里会 NullReferenceException（反编译里是空检查抛异常那一支）⇒ 我们报一下再丢
                SkippedNulls++;
                Debug.LogWarning("[WarpforgeVFX] CollisionNotifier.Register(null)：原版这里会 NRE，我们忽略。" +
                                 "调用方见 WFModuleCollisions.ParticleCollisionDefinition。");
                return;
            }
            collisionsModules.Add(collisionsModule);
            Registered++;
        }

        /// <summary>Unity 的粒子碰撞消息。**`other` 按原版不参与任何判断**（见文件头 ④）。</summary>
        void OnParticleCollision(GameObject other)
        {
            Broadcast();
        }

        /// <summary>原版 `OnParticleCollision` 的循环体（**我们加的公开入口**，见文件头 C）。
        /// 逐个 def：开关关着就跳过；开着就点它的事件（没有订阅者时什么都不发生 —— **原版即静默**）。</summary>
        public void Broadcast()
        {
            for (int i = 0; i < collisionsModules.Count; i++)
            {
                var d = collisionsModules[i];
                if (d == null)
                {
                    SkippedNulls++;                              // 原版这里会 NRE；我们跳过并记账
                    continue;
                }
                if (!d.receiveCollisionMessage) continue;
                d.OnParticleCollision();                         // 见文件头 B
                Broadcasts++;
            }
        }
    }
}
