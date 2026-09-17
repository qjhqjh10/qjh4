// WFModuleDoDestroyAnimation.cs — 原版 `AnimFXModuleDoDestroyAnimation` 的对应物
//   **1 实例**（`Pray_Idle`：`endAnimationName="Pray Exit"` / `destroyOnAnimationEnd=1` / `timeToDestroy=1.0`）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§3**（2 个方法体：`.ctor` / `DoDestroy`）
//   · `.ctor` 字段默认值：`destroyOnAnimationEnd = true`、`timeToDestroy = 1.0f`（`0x3f800000` 读出来的）
//   · `DoDestroy()`：
//       ① `if (myAnimation != null) myAnimation.Play(endAnimationName)`（null 就跳过，**不抛**）
//       ② `if (destroyOnAnimationEnd) Object.Destroy(gameObject, timeToDestroy)`
//   🔴 **名字骗人**：`destroyOnAnimationEnd` 只表示「要不要销毁」，方法体里**没有任何「等动画播完」
//      的机制** —— 不查 clip 时长、不挂事件，销毁时刻 = `now + timeToDestroy`（块2 §3 明写）。
//   · **只重写 `DoDestroy`** —— 没有 `Initialize` / `Exit` / `Update`（块1 §四也提到这点）。
//
// 照方法体还原了什么：
//   · 上面那两句 + 两个默认值；`DoDestroy` 之外的时机什么都不做
//   · 解析 `myAnimation` 用的是 `@node:Animation:…`（组件就是 legacy `UnityEngine.Animation`）
//     —— 2026-09-18 核过：`WarpforgeVFX/Prefabs/Pray_Idle.prefab:95` 那个 Animation 组件
//       确实挂在 `Pray VFX mesh` 这个节点上（路径与数据一致），所以解析得出来
//
// 我们定的（显式标出）：
//   · 计时自己按 dt 推进（理由同 `WFModuleDestroyInTime`：批处理下 `Object.Destroy(go, t)` 不会到点），
//     play 模式下**额外**照原版调一次 `Object.Destroy(go, t)` —— 两条路都指「t 秒后销毁」，谁先到都一样。
//
// 🔴 未还原（两条，都打一次性警告）：
//   ① **`timeToDestroy` 的延时在我们这边实际上没有效果**：`WarpforgeEffectPlayer.DestroyNow:318-337`
//      是「先广播所有模块的 `DoDestroy()`，**紧接着无条件 `Destroy(gameObject)`**」，
//      而原版 `AnimFXController.DoDestroy` 是「**有模块就不销毁**、把销毁权交给模块」（块1 §「DoDestroy」那行）。
//      ⇒ 对象当场就没了，退场动画与那 1 秒延时都看不到。要真正还原得改播放器（本次不许动）。
//   ② **退场动画本体**：这个 clip 在我们工程里**没有**（全工程 0 个 `.anim`；prefab 里
//      `m_Animations` 的两条引用 guid 全 0 ⇒ 挂不到资产）。`endAnimationName` 找不到就只报不播。
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleDoDestroyAnimation")]
    public class WFModuleDoDestroyAnimation : WFEffectModule
    {
        [Tooltip("原版 `endAnimationName`：退场动画的状态名（`Pray_Idle` 是 `Pray Exit`）")]
        public string endAnimationName = "";

        [Tooltip("原版 `myAnimation` 的引用（`@node:Animation:…`）")]
        public string myAnimationRef = "";

        [Tooltip("原版 `destroyOnAnimationEnd`：**只表示「要不要销毁」**，不表示「等动画播完」")]
        public bool destroyOnAnimationEnd = true;

        [Tooltip("原版 `timeToDestroy`（默认 1.0）：`DoDestroy` 之后多少秒销毁")]
        public float timeToDestroy = 1f;

        [Tooltip("运行时解析出来的 legacy Animation 组件")]
        public Animation myAnimation;

        /// <summary>退场动画没播成的次数（自检用 —— 见文件头「未还原 ②」）。</summary>
        public static int PlayFailures;
        public static void ResetDiagnostics() { PlayFailures = 0; }

        float _t = -1f;
        bool _fired, _warnedNoClip, _warnedPlayerDestroys;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);
            if (def == null) return;
            endAnimationName       = def.GetString("endAnimationName", endAnimationName);
            myAnimationRef         = def.GetString("myAnimation", myAnimationRef);
            destroyOnAnimationEnd  = def.GetBool("destroyOnAnimationEnd", destroyOnAnimationEnd);
            timeToDestroy          = def.GetFloat("timeToDestroy", timeToDestroy);
        }

        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            if (string.IsNullOrEmpty(myAnimationRef)) return;
            myAnimation = ResolveNode<Animation>(transform, myAnimationRef, "AnimFXModuleDoDestroyAnimation");
        }

        /// <summary>原版 `DoDestroy()`：播退场动画 + （按开关）定时销毁整个 GameObject。</summary>
        public override void DoDestroy()
        {
            // ① 播退场动画（原版有 null 检查：null 就跳过，不抛）
            if (myAnimation != null)
            {
                if (myAnimation.GetClip(endAnimationName) == null)
                {
                    PlayFailures++;
                    if (!_warnedNoClip)
                    {
                        _warnedNoClip = true;
                        Debug.LogWarning($"🔴 未还原：《{EffectName}》要播的退场动画 `{endAnimationName}` " +
                                         $"在 {myAnimation.name} 上找不到 —— 全工程 0 个 `.anim`，" +
                                         "prefab 里那两条 clip 引用的 guid 全是 0（导出侧没搬 AnimationClip）。");
                    }
                }
                else myAnimation.Play(endAnimationName);
            }

            // ② 要不要销毁（**注意：不是"等动画播完"**，见文件头）
            if (destroyOnAnimationEnd)
            {
                // play 模式：照原版这一句走 Unity 自己的延迟销毁
                if (Application.isPlaying) Destroy(gameObject, timeToDestroy);
                // 批处理 / 编辑器（没有帧末，`Destroy(go, t)` 永远不会到点）：改由 ModuleTick 按 dt 数
                else _t = 0f;
                if (!_warnedPlayerDestroys)
                {
                    _warnedPlayerDestroys = true;
                    Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleDoDestroyAnimation 想「播完 " +
                                     $"{timeToDestroy}s 再销毁」，但**我们的播放器在广播 DoDestroy 之后立刻销毁**" +
                                     "（原版是「有模块就不销毁、交给模块」）⇒ 这 1 秒延时与退场动画在我们这边看不到。" +
                                     "要还原得改 WarpforgeEffectPlayer（`DestroyNow`）。");
                }
            }
        }

        /// <summary>批处理 / 无帧循环下推进 `timeToDestroy`（play 模式由 Unity 的延迟销毁负责）。</summary>
        public override void ModuleTick(float dt)
        {
            if (_t < 0f || _fired) return;
            _t += dt;
            if (_t < timeToDestroy) return;
            _fired = true;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);
        }
    }
}
