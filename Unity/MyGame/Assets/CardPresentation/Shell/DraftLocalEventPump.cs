// DraftLocalEventPump.cs —— 🔶 轮抽「活动到期」的本地时钟泵（原版没有这一层：到期时刻由服务端下发）
//
// ============================ 它是谁、为什么需要 ============================
// 原版：服务端到点时 `LiveOpsHandler.ExpireEvent` 被调到 → `ILiveOps.Expire`（本工程 = `DraftEventBase.Expire`）。
// 本地：服务器已关服 ⇒ **没有任何东西会来敲这一下**（账 `轮抽:6` 的 B 就是这么来的：方法体在位、触发源缺失）。
// 本类 = 那个触发源的本地等价物：玩家启动时装一个小泵，**每秒**问一次
// `DraftLocalEvent.Tick()`（它读的判据与 `LiveOpsHandler.ScheduleEventPopups` 的过期支同一句：
// `data.GetState(NowUtc) == LiveOpState.Finished`）。
//
// ============================ 判据 / 为什么这样装 ============================
// · 安装点 = `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]`（同族先例：`Shell/LiveOpsHandler.cs:1510`
//   `LiveOpsLocalBoot` · `Shell/LocalPlayerData.cs:418` · `Core/PlayerBoot.cs:67`）—— 只在**启动那一次**跑，
//   编辑器批处理（`-executeMethod`）**不跑**（不进 play 模式 ⇒ 断言不会被动装一个泵）。
// · 🔴 泵**不参与**任何玩法判定：它只调 `DraftLocalEvent.Tick()`，那个方法在「没到期」时什么都不做。
// · 关掉模拟（`DraftLocalEvent.SimulationEnabled = false`）⇒ 连 GameObject 都不建（见 `Install`）。
//
// ⚠️ 本类**不是** MonoBehaviour 的嵌套类 —— Unity 拒 `AddComponent<T>()` 的条件之一是「T 没有自己的
//    脚本资产」（只有嵌套类才会没资产），所以这里保持**顶层类 + 类名==文件名**。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>🔶 本地模拟 draft 活动的时钟泵：每秒 `<see cref="DraftLocalEvent.Tick"/>` 一次。
    /// <para>原版**没有对应类**（这件事由服务端 + `ActionScheduler` 那条链完成）—— 这是标准的「服务器相关 ⇒ 允许自设计」门类。</para></summary>
    public sealed class DraftLocalEventPump : MonoBehaviour
    {
        /// <summary>泵的 GameObject 名（场景里认得出、日志里也认得出它是模拟件）。</summary>
        public const string RootName = "Draft Local Event Pump (🔶 本地模拟)";

        /// <summary>已经装过（`AfterSceneLoad` 每次启动只跑一次，但重进 play 模式会再来一遍 ⇒ 幂等）。</summary>
        public static bool Installed { get; private set; }

        /// <summary>tick 间隔（秒）。1 秒足够：原版到期判据的最细粒度也是「排期刷新」那一档，不是逐帧。</summary>
        public const float TickSeconds = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Install()
        {
            if (!DraftLocalEvent.SimulationEnabled)
            {
                Debug.Log("[Draft] 🔶 本地模拟 draft 活动：`SimulationEnabled = false` ⇒ **不装**到期泵"
                        + "（到期链不会跑，`TimedDraftLocal` 保持默认关）");
                return;
            }
            if (Installed) return;                  // 幂等（热重载 / 重进 play 模式）
            var go = new GameObject(RootName);
            Object.DontDestroyOnLoad(go);
            go.AddComponent<DraftLocalEventPump>();
            Installed = true;
            DraftLocalEvent.EnsureLoaded();         // 启动时打那条「本地模拟活动，非原版下发」的日志（出声）
            Debug.Log("[Draft] 🔶 到期泵已装：每 " + TickSeconds + "s 查一次活动状态"
                    + "（原版 = 服务端下发到期事件；本泵只是那个时刻的本地等价物）");
        }

        float _nextAt;

        void Update()
        {
            if (Time.unscaledTime < _nextAt) return;
            _nextAt = Time.unscaledTime + TickSeconds;
            DraftLocalEvent.Tick();
        }

        /// <summary>自检口：清掉「已装过」标记（断言不会用它 —— 它只给重复进 play 模式的编辑器用）。</summary>
        public static void ResetForTest() { Installed = false; }
    }
}
