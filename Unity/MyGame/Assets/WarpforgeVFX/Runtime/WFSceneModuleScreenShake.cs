// WFSceneModuleScreenShake.cs — 原版 `AnimFXModuleScreenShake` 在**场景侧**的对应物
//   🆕 2026-10-14（A394）新建。
//
// 🔴 **这不是「第二份屏震」—— 它是一个**生命周期适配器**，屏震的取值与覆盖语义**只有一份**：
//     `WFModuleScreenShake.Resolve(ShakeEntry)`（preset 表 + 6 个 `overwrite*` 开关）
//     与下游钩子 `WFModuleScreenShake.OnShake`（表现层接的是 `Core/CardFeel.ShakeCamera`）。
//     本类**不重算任何参数、不认识相机、不碰噪声资产** —— 一条都不许在这里再写一遍。
//
// 🔴 **为什么场景侧要单独一个类，而不是直接挂 `WFModuleScreenShake`**：
//     那个类的基类是 `WFEffectModule`（`Initialize` 收的是 `WarpforgeEffectPlayer`，卡侧那条线的控制器），
//     而场景里那颗 `AnimFXController` 上挂的模块必须收 `AnimFXController`（`WFSceneModule`）。
//     两条线的控制器与销毁计时不能混（判据 → `AnimFXController.cs` 文件头那段红字）。
//     ⇒ 共用的是**语义**（`Resolve` / `OnShake`），不共用**基类**。
//
// 判据（第一权威，逐条对照）
// ------------------------------------------------------------------
//   · 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXModuleScreenShake.cs`
//     （`cameraShakes // 0x38` · `manualTriggerCameraShakes // 0x40` · 三个方法
//      `OnEnable` / `AnimEventDoShake(int)` / `TriggerCameraShake(int)`）
//   · 方法体：`d:/2/tools/decomp_full/AnimFXModuleScreenShake__{OnEnable,AnimEventDoShake,TriggerCameraShake}.c`
//   · 原版语义（与卡侧那份逐字同源，见 `WFModuleScreenShake.cs` 文件头）：
//       `OnEnable`              → `foreach (cameraShakes)`：preset 非空就播（**自动档**）
//       `AnimEventDoShake(i)`   → 播 `manualTriggerCameraShakes[i]`；越界**静默 return**
//       `TriggerCameraShake(i)` → 播 `manualTriggerCameraShakes[i]`；越界 **LogError**
//       🔴 两个「手动」口读的是**同一个数组**（都是 `+0x40`），**只有 `OnEnable` 读 `+0x38`**。
//
// ⚠️ **一处【有意偏离】**（与卡侧那份**逐字同一个理由**，只是落点不同，别改回去）
// ------------------------------------------------------------------
//     **自动档从 `OnEnable` 挪到 `Configure`。**
//     原版那个 `OnEnable` 跑得起来，是因为字段**是序列化在场景里的**（组件一激活，字段就是真值）；
//     我们是**运行时 `AddComponent` + 事后装配** —— `AddComponent` 会**立刻**触发一次 `OnEnable`，
//     那一刻字段还是默认值（空数组）⇒ 放 `OnEnable` 就是「用空参数播」。
//     ⇒ 落点改成 `Configure`：工厂 `AddComponent` 之后**紧接着**调它（见 `WFSceneModuleFactory.Create`），
//       **触发条件照抄 `OnEnable` 的**（`isActiveAndEnabled` = 组件开着 ∧ 宿主 GO 开着）。
//     ⚠️ 为什么**不**像卡侧那样放 `Initialize`：卡侧 `Initialize` 是「两条路都走」的入口；
//       而**场景侧的 `Initialize` 只在 `SetData` 里发出**，原版 `SetData` 的 4 个调用点**全在卡侧**
//        （见下）⇒ 放那儿等于**永远不播**，比放在 `Configure` 偏离得更远。
//
// 🔴 **触发源（如实标，⛔ 别写成「会触发」）**
// ------------------------------------------------------------------
//   · **自动档**（`cameraShakes`）靠 `SetData` → `Initialize`。原版 `SetData` 只有 4 个调用点、
//     **全在卡侧**；场景里唯一那颗带模块的实例（`battlearenatauviorla` 的 `Railgun BIG (1)`）
//     是**战场物件** ⇒ 原版**很可能一次都不播**（`资料/普查产出_1012/H2_场景侧AnimFX.md` §四）。
//   · **手动档**（`manualTriggerCameraShakes`）本件顺着查完了（A394 那一格原来写着「没查清」）：
//     ① `grep -rln "AnimEventDoShake|TriggerCameraShake" d:/2/tools/decomp_full/` ⇒ **只有它们自己那两个 `.c`**
//        （**没有**静态调用点）；
//     ② 99 个 `AnimationClip`（`d:/2/新解包资源/assets_full` 全量）的事件表里**零命中**
//        —— 连 `Railgun BIG` 那条 clip 自己都是 `m_Events = []`；
//     ③ 但**不是**「没有触发通道」：`AnimFXModuleCollisions.collisionEvent`（一个 **UnityEvent**）
//        在 **5** 个卡侧特效 prefab 里**真的连着** `AnimFXModuleScreenShake.AnimEventDoShake(0)`
//        （实据：`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/` 的
//         `MonoBehaviour_-1804495645565317640.json` / `…_8973829403083807152.json` 等 **5 份**，
//         每份都带 `collisionAndParticles`，`m_MethodName = "AnimEventDoShake"` · `m_IntArgument = 0`）。
//        ⇒ 卡侧那条线的 `WFModuleScreenShake.cs` 文件头写的「触发源是动画挂点」**不准确**
//          （那是**旁证错**，不是行为错；本件**没改**那个文件 —— 白名单外，已写进报告）。
//   ⇒ **归到场景侧这颗实例上：两条档都没有触发源 ⇒ 它一次都不播**（与「机制补好了」不矛盾：
//      A394 的验收标准是**机制复原**，不是画面变化）。
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>原版 `AnimFXModuleScreenShake` —— **场景侧**那一份（取值/覆盖语义与卡侧共用，
    /// 见文件头）。挂在带 `AnimFXController` 的那个 GameObject 上（原版就是这样）。</summary>
    [WFSceneModuleKind("AnimFXModuleScreenShake")]
    public class WFSceneModuleScreenShake : WFSceneModule
    {
        /// <summary>自动档那条轨道（原版 +0x38）。**只有 `Configure` 读它**（= 原版 `OnEnable` 那一跳）。
        /// ✅ **2026-10-14（A394 欠账收口 / W10）：旁挂里有了** —— `EnvBlendables.json` 的
        /// `modules.第 i 个模块.cameraShakes[j].*`（由 `工具/gen_env_blendables.py` 的 `pack_module_fields`
        /// 从原版包里收出来），`Configure` 用 `WFModuleScreenShake.ReadList` 装进来。
        /// ⚠️ 原版这颗实例的 `cameraShakes` **本来就是空数组**（判据 = `animfx_modules.json` 的
        /// `Scenario` 效果 + 原版场景包 `MonoBehaviour_5320.json`）⇒ 装完仍然是 0 条（**数据，不是缺口**）。⚠️ 键里的下标写作中文「第 i 个模块」：裸尖括号在 XML 注释里会被当标签（CS1570）。</summary>
        public WFModuleScreenShake.ShakeEntry[] cameraShakes = new WFModuleScreenShake.ShakeEntry[0];

        /// <summary>手动档那条轨道（原版 +0x40）。**两个手动口读的都是它**（见 `AnimEventDoShake`）。
        /// ✅ 2026-10-14 起由旁挂装配（同 `cameraShakes` 的说明）；原版这颗实例这里是 **1 条**：
        /// `presetSO = @asset:MonoBehaviour:Shake Earthquake`，6 个 `overwrite*` 全 0
        /// （判据 = `animfx_modules.json` 的 `Scenario` 效果）。</summary>
        public WFModuleScreenShake.ShakeEntry[] manualTriggerCameraShakes = new WFModuleScreenShake.ShakeEntry[0];

        /// <summary>自动档：原版在 `OnEnable`，我们在 `Configure`（**有意偏离**，理由见文件头）。
        /// 触发条件照抄 `OnEnable`：`isActiveAndEnabled`（组件开着 ∧ 宿主 GO 开着）——
        /// 我们这条链上真的两档都有（`battlearena3` 那两颗组件是关的、`Big Gun Effect` 宿主 GO 是关的）。</summary>
        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            // 🆕 2026-10-14（A394 欠账收口 / W10）：把旁挂里**模块自己的**字段装进两条轨道。
            //   🔴 读法**只有一份** = 卡侧那份 `WFModuleScreenShake.ReadList`（本件把它从 `private`
            //     放宽到 `internal`，就为了这一处）—— ⛔ 别在这里另抄一份：键名或 `@asset:` 那条
            //     解析抄错一格，结果就是**静默的空轨道**（本仓红线）。
            //   ⚠️ 装配必须排在自动档**之前**（原版那个 `OnEnable` 跑的时候，字段已经是序列化在场景里的真值）。
            //   ⚠️ `def == null`（工厂允许传 null）= 没有字段可装 ⇒ 两条轨道保持空数组，不会误播。
            if (def != null)
            {
                cameraShakes = WFModuleScreenShake.ReadList(def, "cameraShakes");
                manualTriggerCameraShakes = WFModuleScreenShake.ReadList(def, "manualTriggerCameraShakes");
            }
            if (!isActiveAndEnabled) return;                      // 原版 `OnEnable` 跑不起来的那两档
            for (int i = 0; i < cameraShakes.Length; i++) Play(cameraShakes[i], "自动档");
        }

        /// <summary>原版 `Initialize(AnimFXController)`：**只记 controller**。
        /// ⚠️ 自动档**不在这里**（本类把它放在 `Configure`，理由见文件头那段偏离）。
        /// ⚠️ 重写时**必须**调 `base.Initialize`（否则 <see cref="WFSceneModule.Controller"/> 恒 null）。</summary>
        public override void Initialize(CardPresentation.AnimFXController controller)
        {
            base.Initialize(controller);
        }

        /// <summary>给**动画事件 / `UnityEvent`** 用的手动档（原版 `AnimEventDoShake`）。
        /// 越界**静默 return**（原版如此 —— ⛔ 别「顺手修好」，那是改语义）。</summary>
        public void AnimEventDoShake(int i)
        {
            if (i < 0 || i >= manualTriggerCameraShakes.Length) return;
            Play(manualTriggerCameraShakes[i], "手动档");
        }

        /// <summary>原版 `TriggerCameraShake(i)`：**与 `AnimEventDoShake` 读同一个数组**
        /// （`manualTriggerCameraShakes // 0x40`）。越界**报错**（原版如此）。</summary>
        public void TriggerCameraShake(int i)
        {
            if (i < 0 || i >= manualTriggerCameraShakes.Length)
            {
                Debug.LogError($"[WarpforgeVFX] 场景侧屏震下标 {i} 越界（{gameObject.name} 只有 " +
                               $"{manualTriggerCameraShakes.Length} 条手动档）");
                return;
            }
            Play(manualTriggerCameraShakes[i], "TriggerCameraShake");
        }

        /// <summary>🔴 **本类唯一的「播」入口，而且它自己不决定任何数值** ——
        /// 参数一律走 `WFModuleScreenShake.Resolve`（preset 表 + 6 个覆盖开关），
        /// 下游一律走 `WFModuleScreenShake.OnShake`（与卡侧**同一个钩子、同一个计数器**）。
        /// ⛔ 别在这里再抄一份 `Resolve` / 再挂一个 `OnShake` 之外的通道。</summary>
        void Play(WFModuleScreenShake.ShakeEntry e, string from)
        {
            if (e == null || string.IsNullOrEmpty(e.preset)) return;   // 原版：preset 空就跳过
            var req = WFModuleScreenShake.Resolve(e);                  // ← 取值 / 覆盖语义**只此一份**
            if (WFModuleScreenShake.OnShake == null)
            {
                WFModuleScreenShake.DroppedRequests++;                 // ← 与卡侧共用一个计数器
                if (WFModuleScreenShake.DroppedRequests <= 5)
                    Debug.LogWarning($"[WarpforgeVFX] 场景侧屏震 preset `{e.preset}`（{from}）**没有下游接** —— " +
                                     "表现层要挂 `WFModuleScreenShake.OnShake`（我们这边接 CardFeel.ShakeCamera）。" +
                                     "这条警告只报前 5 次（与卡侧共用一个计数）。");
                return;
            }
            WFModuleScreenShake.OnShake(req);
        }
    }
}
