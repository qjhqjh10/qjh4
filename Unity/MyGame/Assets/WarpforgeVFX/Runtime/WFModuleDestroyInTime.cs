// WFModuleDestroyInTime.cs — 原版 `AnimFXModuleDestroyInTime` 的对应物
//   **1 实例**（`VanguardIdleEffect`：`actionStart=5` / `destroyTime=0.75`）
//
// 语义出处：`资料/AnimFX_18类方法体_块2.md` **§9**（3 个方法体：`.ctor` 空 / `Initialize` / `Exit`）
//   · `Initialize(controller)` → `base.Initialize(...)` 之后
//     `if (actionStart == 0 /*Initialize*/) Object.Destroy(gameObject, destroyTime)`
//   · `Exit()`                 → `if (actionStart == 5 /*Exit*/) Object.Destroy(gameObject, destroyTime)`
//   ⇒ **同一个类按 `actionStart` 当模式开关**（0 = 从 Initialize 起算 / 5 = 从 Exit 起算），
//     两条分支都只有 `Object.Destroy(go, t)` 一句话。**没有 `DoDestroy` 重写、没有手动档**。
//   🔴 与 `WFEffectModule` 文件头那条是同一件事的两面：**控制器不读 `actionStart`、无条件广播**
//      ⇒ 要不要响应由**本模块自己读自己的 `actionStart`**（18 类里只有本类与 `ChangeMaterial`、
//      `Event.whereToFire` 四处这么用，见块2 §9 的结论段与块1 §附F）。
//
// 照方法体还原了什么：
//   · 两种起算点（Initialize / Exit）＋ `destroyTime` 秒后**销毁整个 GameObject**（不是本组件）
//   · `actionStart` 由本模块自己读（不是播放器的调度开关）
//
// 我们定的（逐条标了为什么，都不是"照抄方法体"）：
//   · **计时自己按 dt 推进**（`ModuleTick`），不用 `Object.Destroy(go, t)` 那条 Unity 内部计时：
//     批处理 / 无帧循环下（白板、`-executeMethod`）延时销毁**永远不会到点**，而这条线要求
//     一切都能在批处理里确定性复现（块2 §9 末尾给的正是这条建议）。
//     到点后 play 模式走 `Object.Destroy`、否则 `DestroyImmediate` ——
//     与 `WarpforgeEffectPlayer.DestroyNow:335-336` 同款写法。
//   · `dt` 由播放器给（`WarpforgeEffectPlayer.Tick` 传 `Time.deltaTime`）⇒ 与原版一样**受 timeScale 影响**。
//   · `actionStart` 既不是 0 也不是 5 时**原版没有分支**（= 永远不会自毁）。我们照原样不销毁，
//     但**打一条一次性警告** ——「这个效果永远不会自己消失」是下一个人必须知道的事（不许静默失败）。
//
// 🔴 未还原：无。两条分支都照方法体写了；唯一"不是逐字"的是计时方式（见上）。
using UnityEngine;

namespace WarpforgeVFX
{
    [WFModuleKind("AnimFXModuleDestroyInTime")]
    public class WFModuleDestroyInTime : WFEffectModule
    {
        [Tooltip("原版 `destroyTime`：从起算点起多少秒后销毁整个 GameObject")]
        public float destroyTime = 0f;

        /// <summary>起算点（`Initialize` / `Exit`）—— 诊断用，也是「有没有起算」的判据。</summary>
        public string ArmedFrom { get; private set; }

        float _t;                // 起算之后经过的时间
        bool _armed;             // 已经起算（Exit 可能被调多次，只认第一次）
        bool _fired;             // 已经销毁过
        bool _warnedNoBranch;

        public override void Configure(WFModuleDef def)
        {
            base.Configure(def);                       // actionStart
            if (def != null) destroyTime = def.GetFloat("destroyTime", destroyTime);
        }

        /// <summary>原版 `Initialize`：`actionStart == Initialize(0)` 才起算。</summary>
        public override void Initialize(WarpforgeEffectPlayer controller)
        {
            base.Initialize(controller);
            if (actionStart == WFActionStart.Initialize) Arm("Initialize");
            else if (actionStart != WFActionStart.Exit) WarnNoBranch();
        }

        /// <summary>原版 `Exit`：`actionStart == Exit(5)` 才起算。</summary>
        public override void Exit()
        {
            if (actionStart == WFActionStart.Exit) Arm("Exit");
        }

        /// <summary>推进自毁计时（原版是 Unity 自己的延迟销毁，见文件头）。</summary>
        public override void ModuleTick(float dt)
        {
            if (!_armed || _fired) return;
            _t += dt;
            if (_t < destroyTime) return;
            Fire();
        }

        void Arm(string from)
        {
            if (_armed || _fired) return;
            _armed = true;
            ArmedFrom = from;
            if (destroyTime <= 0f) Fire();     // 原版 `Destroy(go, 0)` = 当帧末就没了
        }

        void Fire()
        {
            if (_fired) return;
            _fired = true;
            if (Application.isPlaying) Destroy(gameObject);
            else DestroyImmediate(gameObject);  // 批处理没有帧末 —— Destroy 不生效
        }

        void WarnNoBranch()
        {
            if (_warnedNoBranch) return;
            _warnedNoBranch = true;
            Debug.LogWarning($"[WarpforgeVFX] 《{EffectName}》的 AnimFXModuleDestroyInTime：`actionStart={actionStart}` " +
                             "在**原版里没有分支**（块2 §9 只有 `==0(Initialize)` 与 `==5(Exit)` 两条）" +
                             "⇒ 本模块**永远不会销毁这个 GameObject**。要它自毁就把 actionStart 改成 0 或 5。");
        }
    }
}
