// WFSceneModule.cs — **场景侧**的 AnimFX 模块基类（原版 `AnimFXModuleBase` 的第二个对应物）
//   🆕 2026-10-14（A394）新建。
//
// 🔴 **为什么会有两个基类**（先读这段，别踩）
// ------------------------------------------------------------------
//   原版**一个** `AnimFXModuleBase` 被两条线共用；本仓把它拆成了两个，各有明确判据：
//     · **特效 prefab 那条线**（958 件）的模块基类 = `WFEffectModule`
//       —— 它的 `Initialize` 收的是 **`WarpforgeEffectPlayer`**（那条线唯一的生命周期拥有者）。
//     · **战场场景侧**（本件 A394）= **本类**：`Initialize` 收的是 **`AnimFXController`**
//       —— 场景里那个控制器（`CardPresentation/Battle/AnimFXController.cs`，A196 移植）。
//   ⛔ **别把 `WFEffectModule` 挂到场景侧**，**也别把本类挂到 `WarpforgeVFX/Prefabs/**` 的特效上** ——
//      两条线各有自己的控制器与销毁计时，混用会互相打架（判据见 `AnimFXController.cs` 文件头那段红字）。
//   ⛔ 两条线的**注册表也各一份**（`WFModuleFactory` / `WFSceneModuleFactory`）：靠 `[WFModuleKind]` /
//      `[WFSceneModuleKind]` 特性分别扫，互不认对方登记的类。
//
// 判据（第一权威，逐条对照）
// ------------------------------------------------------------------
//   · 签名桩：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AnimFXModuleBase.cs`
//   · 方法体：`d:/2/tools/decomp_full/AnimFXModuleBase__{Initialize,Exit,DoDestroy,ctor}.c`
//   · 字段偏移（`d:/2/tools/il2cpp_out/dump.cs` 的 `AnimFXModuleBase`，TypeDefIndex 1053，本件亲读）：
//     `actionStart +0x20` · `animFXController +0x28` · `isRetaliation +0x30`
//
//   `Initialize(AnimFXController c)` 原版做**两件**事（`__Initialize.c` 逐句读出来的）：
//     ① `animFXController = c`（+0x28，写屏障后就是那一句）
//     ② 算 `isRetaliation`（+0x30）= `BattleManager.IsPlayerTurn() XOR c.actingCard.isPlayer`
//        —— 那两个 `BattleManager__IsPlayerTurn` 分支读的就是 `param_2 + 0x50`
//        （= `AnimFXController.actingCard`，偏移与 `AnimFXController.cs` 文件头那张表一致）。
//   `Exit()` / `DoDestroy()` 原版**是空实现**：两个 `.c` 的符号名被 IL2CPP 剥成了
//   `NetworkingPeer__OnMessage`、函数体里只有一句 `return` ⇒ 子类要不要做事由子类自己决定
//   （与 `WFEffectModule.cs` 文件头那条「18 类里只有 5 个重写 Exit、1 个重写 DoDestroy」同源）。
//
// ⚠️ **有意的偏离（两处，都有出处，别当缺陷改）**
// ------------------------------------------------------------------
//   ① 🔴 **`isRetaliation` 那一跳我们【不做】**（如实标，⛔ 不是「已覆盖」）：上面 ② 需要
//      「这局轮到谁」+「出招那张卡属于哪一方」，而本类两样都拿不到 ——
//      VFX 层不认识牌局，且我们的 `AnimFXController.actingCard` 是 **`Transform`**
//      （没有 `CardScript.isPlayer`，判据见 `AnimFXController.cs` 的「有意偏离 ④」）。
//      ⇒ 与**卡侧那条线既定的同一口径**（`WarpforgeEffectPlayer.IsRetaliation`，
//        见 `WarpforgeEffectPlayer.cs:93-97`）：**由调用方设**，不设就是 `false`。
//      要真做得另开一条「谁告诉我这局轮到谁」的通道 —— 那件事本件没做。
//   ② `actionStart` 原版是 `[SerializeField] [AllowNesting] protected`；我们由工厂**运行时装配**
//      ⇒ 与这条线其它类同一个口径（`WFEffectModule.actionStart` 也是公开的），字段公开。
//
// 🔴 **谁广播生命周期**：`AnimFXController` 的 `SetData` / `Exit` / `DoDestroy`
//   —— 原版对**所有模块无条件广播**，**它一处都不读 `actionStart`**（要不要响应、什么时候响应，
//   由各个模块**自己读自己那个字段**决定）。⛔ **别把 `actionStart` 做成调度开关**
//   （与 `WFEffectModule.cs` 文件头同一条规矩，出处是 `AnimFXModuleDestroyInTime` /
//   `ChangeMaterial` / `Event.whereToFire` 三个实读）。
//
// 🔴 **场景侧那个控制器上模块今天【会不会真被触发】—— 如实标两句**：
//     · 触发 `Initialize` 的那一跳是 `SetData`；原版 `SetData` 在全反编译里只有 **4 个调用点**，
//       **全在卡侧**（`CardAnimController.Initialize` · `CardScript.PlayTriggerAnim` ·
//       `RemnantBody.DoDestroyByAttack` · `RemnantAeldari.CollectWaystoneEffect`）⇒
//       场景里那唯一一颗带模块的实例（`Railgun BIG (1)`）**在原版很可能一次都不播**。
//     · `AnimEventDoShake` / `TriggerCameraShake` 这两条手动口也没有触发源（详见
//       `WFSceneModuleScreenShake.cs` 文件头那段「触发源」）。
//   ⇒ **本类补的是【机制】，不是画面变化**（本件验收标准就是这个）。
using System;
using UnityEngine;

namespace WarpforgeVFX
{
    /// <summary>
    /// **场景侧**模块基类（原版 `AnimFXModuleBase`，见文件头）。
    /// **所有场景侧模块都必须是 MonoBehaviour**（原版就是；而且原版那些组件是挂在
    /// `AnimFXController` **同一个 GameObject** 上的 —— 工厂也照这一条挂）。
    /// 覆盖 <see cref="Initialize"/> 时**必须调 `base.Initialize`**（和原版一样，基类要把 controller 记下来）。
    /// </summary>
    public abstract class WFSceneModule : MonoBehaviour
    {
        [Tooltip("原版 `AnimFXModuleBase.actionStart`。⚠️ 它不是调度开关 —— 要不要响应由本模块自己读它。")]
        public WFActionStart actionStart = WFActionStart.Initialize;

        /// <summary>原版 `AnimFXModuleBase.animFXController`（+0x28）。由 `AnimFXController` 广播
        /// `Initialize` 时注入 —— 触发那条链是 `SetData`（见文件头「谁广播生命周期」）。</summary>
        public CardPresentation.AnimFXController Controller { get; private set; }

        /// <summary>原版 `AnimFXModuleBase.isRetaliation`（+0x30）。
        /// 🔴 **有意偏离 ①**（见文件头）：原版在 <see cref="Initialize"/> 里算它
        /// （`BattleManager.IsPlayerTurn() XOR actingCard.isPlayer`），**我们这一跳不做** ——
        /// 本类拿不到牌局、`actingCard` 也没有 `isPlayer` ⇒ 与 `WarpforgeEffectPlayer.IsRetaliation`
        /// 同一口径：**由调用方设**，不设就是 `false`。要真做得多一条「谁告诉我这局轮到谁」的通道。</summary>
        public bool IsRetaliation { get; set; }

        // ---- 生命周期：与控制器一一对应，基类默认都是空实现（原版就是空实现）----

        /// <summary>原版 `AnimFXModuleBase.Initialize(AnimFXController)`：**先记下 controller**。
        /// ⚠️ 子类重写时**必须**先调 `base.Initialize(controller)`，否则 <see cref="Controller"/> 恒 null
        /// （原版那句 `*(this + 0x28) = controller` 就在方法体最前面）。</summary>
        public virtual void Initialize(CardPresentation.AnimFXController controller) { Controller = controller; }

        /// <summary>原版 `AnimFXModuleBase.Exit()` —— **空实现**（判据 = `__Exit.c` 函数体只有 `return`）。
        /// 广播方 = `AnimFXController.Exit()`（**不防重入**，原版如此）。</summary>
        public virtual void Exit() { }

        /// <summary>原版 `AnimFXModuleBase.DoDestroy()` —— **空实现**（判据 = `__DoDestroy.c` 同上）。
        /// 广播方 = `AnimFXController.DoDestroy()`，而且**只有 `modules` 非空时才广播**
        /// （`AnimFXController.cs` 那条：「`modules` 空 ⇒ 销毁自己；否则只广播、自己不动」）。</summary>
        public virtual void DoDestroy() { }

        /// <summary>按数据装配。手挂的模块不走这里（参数在 Inspector 里就是真的）。
        /// 🆕 场景侧多一条**约定**（不是原版的方法）：工厂 `AddComponent` 之后**紧接着**就调它，
        /// 而 `AddComponent` 会**先跑一次 `OnEnable`**（那一刻字段还是默认值）⇒ 各模块要想「一建好就动」，
        /// **时机放这里**、条件照 `OnEnable` 的（`isActiveAndEnabled`）。
        /// 先例 = 卡侧那条线把 `WFModuleScreenShake` 的自动档从 `OnEnable` 挪到了 `Initialize`，同一个理由。</summary>
        public virtual void Configure(WFModuleDef def)
        {
            if (def != null) actionStart = (WFActionStart)def.GetInt("actionStart", 0);
        }
    }
}
