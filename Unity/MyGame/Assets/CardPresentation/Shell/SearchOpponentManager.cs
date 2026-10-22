// SearchOpponentManager.cs — **常驻**的「找对手」推进器（类名照原版 `SearchOpponentManager`）
//
// ============================ 为什么要有这一件（出处逐条）============================
// 🔴 **原版判据（2026-10-22 现读 `d:/2/tools/decomp_full/`）**：
//   · `SearchingOpponentWindow__*.c` 只有 ctor / CancelButtonClick / CancelMatchMatchmaking / Close /
//     OnEnable / OnDisable / Open / OpponentFound —— **没有 `Update`** ⇒ 那扇窗只负责显示。
//   · 等待与超时在**常驻 manager 的协程**上：`SearchOpponentManager.__c__DisplayClass21_0___SearchOpponent_b__0.c:114/146`
//     起 `WaitForHumanOpponent`；`SearchOpponentManager._WaitForHumanOpponent_d__35__MoveNext.c:159`
//     （`WaitForSeconds` 循环）→ `:219 ChangeToBotBattle`；取消 = `SearchOpponentManager__CancelBattleSearch.c:12`
//     `StopCoroutine(manager)`。
//   ⇒ **窗 active 与否都不影响等待**。
//
// 🔴 我们原来的形状是反的：推进挂在**窗**上（`LiveOpsEventWindow.Update → TickSearch`）⇒
//   2026-10-22 真机缺陷：排位窗点 `Battle!` → 开全屏 `SearchingOpponentWindow` →
//   `WindowsManager.OpenWindow` 的全屏支 `HideAllWindows()`（`Shell/WindowsManager.cs` 的 `OpenWindow`）
//   → `GameWindow.Hide()` 把排位窗整棵 `SetActive(false)` ⇒ Unity **不再给它派 `Update`**
//   ⇒ `SecondsLeft` 冻在 12，等多久都不开打（诊断全文 → `资料/普查产出_1022/W64_排位匹配卡死诊断.md`）。
//
// ⇒ 本类 = 原版那个 manager 的等价物：**每一帧推一次「当前正在搜索」的那一份 `SearchingMatchPopup`**，
//   与任何窗口的 active 状态无关。**窗只负责显示**。
//
// ============================ 单一驱动（⛔ 不许一帧推两次）============================
//   · **注册/注销**只有一处判据 = `SearchingMatchPopup.SyncTicker()`：`_waiting` 为真且
//     `HostDrivesItself == false` ⇒ 注册（`Begin`），否则注销（`End`）。
//   · `LiveOpsEventWindow` **不再有自己的 `Update`**（原来那句 `Update → TickSearch` 已删）⇒
//     排位 / 遭遇战两扇的 12 秒只由这里推。`LiveOpsEventWindow.TickSearch` **留给自检直调**。
//   · 练习窗（`PracticeModePopup`）**仍然自己 `Update → TickSearch`**（它的宿主不会被全屏窗藏起来），
//     它的 `SearchingMatchPopup` 因此**不注册**到这里（`Attach(..., hostDrivesItself: true)`）——
//     否则一帧推两次，12 秒会变成 6 秒。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>常驻的「找对手」推进器。**不挂在任何窗口上** —— 窗口关掉/藏起来它照常每帧推。</summary>
    public class SearchOpponentManager : MonoBehaviour
    {
        static SearchOpponentManager _inst;

        /// <summary>当前正在搜索的那一份（= 原版 manager 手里那一局）。`null` = 没有在搜。</summary>
        SearchingMatchPopup _active;

        /// <summary>常驻宿主那一台（自检读；**不要**拿它开窗）。</summary>
        public static SearchOpponentManager Instance { get { return _inst; } }

        /// <summary>现在手里有没有一份（自检用 —— 「取消之后没有残留」就是断它）。</summary>
        public static bool HasActive { get { return _inst != null && _inst._active != null; } }

        /// <summary>这一份现在是不是由本 ticker 在推（自检用；断「注册生效」与「注销干净」两向）。</summary>
        public static bool IsDriving(SearchingMatchPopup popup)
        {
            return popup != null && _inst != null && _inst._active == popup;
        }

        /// <summary>幂等建宿主（**没建就现建**）。挂在 <paramref name="parent"/> 下；传 null 时建在场景根。
        /// 🔴 **编辑模式（自检）不跑 `Awake`** ⇒ 这里必须**显式登记 `_inst`**（同 `WindowsManager.EnsureHost` 的先例：
        /// 不显式登记的话，再调一次会又建一台，而且 `Instance` 一直是 null）。</summary>
        public static SearchOpponentManager Ensure(Transform parent = null)
        {
            if (_inst != null) return _inst;
            var go = new GameObject("SearchOpponentManager");
            if (parent != null) go.transform.SetParent(parent, false);
            var m = go.AddComponent<SearchOpponentManager>();
            _inst = m;
            Debug.Log("[Searching] 现建常驻推进器 `SearchOpponentManager`（原版同名类：等待/超时在常驻 manager 上，窗只显示）"
                    + (parent != null ? "，挂在 `" + parent.name + "` 下" : "（场上没有 `WindowsManager` ⇒ 建在场景根）"));
            return m;
        }

        /// <summary>**注册**：从这一刻起由本 ticker 每帧推它 —— 原版 `StartCoroutine(WaitForHumanOpponent)` 的等价物。
        /// 只由 `SearchingMatchPopup.SyncTicker()` 调（`BeginSearch` / `BeginNetWait` 那条路）。</summary>
        public static bool Begin(SearchingMatchPopup popup)
        {
            if (popup == null) return false;
            var m = Ensure(WindowsManager.Instance != null ? WindowsManager.Instance.transform : null);
            if (m == null) return false;                 // `AddComponent` 失败只可能是引擎级故障；不静默（下面那台会一直不推）
            var prev = m._active;
            if (prev != null && prev != popup)
            {
                // 原版同一时刻只有一局。两向都如实说出来 —— 静默顶替会让「12 秒不推进」无从分辨。
                if (prev.IsWaiting)
                    Debug.LogWarning("[Searching] 又有一份搜索要注册，而上一份**还在等**（`" + prev.name + "`）—— "
                                   + "原版同一时刻只有一局；照「后来者顶替」处理（上一份从此不再被推）");
                else
                    Debug.Log("[Searching] 上一份搜索已经不在等（`" + prev.name + "`）⇒ 换成新的一份 `" + popup.name + "`");
            }
            m._active = popup;
            return true;
        }

        /// <summary>**注销**：不再推这一份（原版 `CancelBattleSearch → StopCoroutine` 的等价物）。
        /// ⚠️ 只摘注册，**不动** `Searching`/`SecondsLeft` —— 要清状态请走 `SearchingMatchPopup.AbortSearch()`。</summary>
        public static void End(SearchingMatchPopup popup)
        {
            if (_inst == null) return;
            if (popup == null) { _inst._active = null; return; }
            if (_inst._active != popup) return;          // 不是它（可能是更新的那一份）⇒ 不许顺手清掉别人
            _inst._active = null;
        }

        /// <summary>推一帧（**静态口**）：自检用 —— 批处理下没有帧循环。生产那一跳是 `Update → Tick`。
        /// ⚠️ 这是**生产同一条**推进路径（`Tick` 就是 `Update` 每帧调的同一个方法），不是另开一条。</summary>
        public static void Advance(float dt)
        {
            if (_inst != null) _inst.Tick(dt);
        }

        /// <summary>推一帧。**生产**由 `Update` 每帧调一次；自检走静态口 <see cref="Advance"/>。</summary>
        public void Tick(float dt)
        {
            var p = _active;
            if (p == null) { _active = null; return; }   // 已销毁（Unity 假 null）⇒ 当场摘掉，别每帧空转
            p.Tick(dt);
            // `Tick` 里可能把自己注销掉（等满 ⇒ `Hide` ⇒ `End`；取消 ⇒ `AbortSearch` ⇒ `End`）⇒ 这里不再碰 `_active`。
        }

        void Update() { Tick(Time.deltaTime); }

        void Awake()
        {
            // 与 `WindowsManager.Awake` 同形：重了就让后来者自毁（正常只有一台）
            if (_inst != null && _inst != this) { Destroy(gameObject); return; }
            _inst = this;
        }

        void OnDestroy() { if (_inst == this) _inst = null; }
    }
}
