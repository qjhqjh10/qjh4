// WildcardTutorial.cs — 万能牌（卡池筛选）教程：原版 `WildcardTutorialManager` 的本地等价
//
// ============================ 判据（方法体/场景资产实读）============================
// · 枚举（`WildcardTutorialManager.cs` 现读）：`WildcardTutorialState { NoState=0, Filter=1, Owned=2, End=3 }`；
//   按钮（私有枚举）：`FilterToggle=0, OwnedToggle=1, FixedPosition=2`。
// · `StartWildcardTutorial(filterToggle, ownedToggle)`（`:271` 起）：
//      `state != End` ⇒ 存两个开关 → `state = Filter` → `gameObject.SetActive(true)` → 销毁旧指针 →
//      `AddPointer(FilterToggle)` → 给两个开关挂 `onValueChanged`；
//      已是 `End` ⇒ 只销毁指针 + 摘监听（**不重启**）。
// · `ToggleFilters(bool on)`（`:331`）：`on && state == Filter` ⇒ 销毁指针 →
//      `AddPointer(OwnedToggle, 词条 Tips/WildcardTutorial1)` → `state = Owned`。
// · `ToggleOwned(bool owned)`（`:351`）：`!owned && state == Owned` ⇒ 销毁指针 →
//      **`filterToggle.isOn = false`** → `AddPointer(FixedPosition, 词条 Tips/WildcardTutorial2, showPointer:false)`
//      → `state = End` → 摘两个监听。
// · `CardDisplayOpened()`（`:545`）：销毁指针（**不改 state**）。
// · `AddPointer` → `AddPointerWithDelay`（协程）→ 目标按名字取 `buttonObjNames`：
//      `FilterToggle → "Filter Toggle"` · `OwnedToggle → "Owned Toggle"` · `FixedPosition → "CardsTab"`（`.ctor` 字典实读）。
// · 指针偏移 = **三个序列化字段**（`.ctor` 默认值只是 0；真值在 **`Intro.unity`** 里，现读）：
//      `tutorialPointerOffsetFilterToggle = (0, -10)` · `tutorialPointerOffsetOwnedToggle = (-20, 0)` ·
//      `tutorialPointerOffsetFixed = (-270, 690)`
//      （`assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity:7694-7696`）。
//    ⚠️ 原版偏移是**目标本地像素、y 向上**；我们的设计像素 **y 向下** ⇒ 用时 y 取反（同 `MenuTutorialFlow` 那条）。
// · 指针 prefab = `tutorialPointerMenu`（`AssetReferenceTyped<GameObject>`，远端）⇒ 我们用自己的
//   `TutorialPointerUi`（`Tier.Wildcards`，同一份 `TutorialPointerWildcards` 规格）。
//
// ============================ 接线（⛔ 本轮不能改 CollectionWindow）============================
// 收藏窗卡池筛选面板的持有者只需三步：
//   ```csharp
//   var b = new WildcardTutorialCollectionBindings(...);   // 实现 IWildcardTutorialBindings（3 个节点 + 两个开关状态）
//   WildcardTutorial.TryStart(transform /* 全屏根 */, b);  // 已做过会自己返回 null
//   // 玩家点开卡详情时：WildcardTutorial.NotifyCardDisplayOpened();
//   ```
// `CollectionWindow` 归别的代理 ⇒ 本文件只提供 API + 一个**纯状态机**（可脱离场景自检）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `WildcardTutorialState`（值照抄：0/1/2/3）。</summary>
    public enum WildcardTutorialStep
    {
        NoState = 0,
        Filter = 1,
        Owned = 2,
        End = 3,
    }

    /// <summary>**纯状态机**（无场景依赖，自检直接跑）：逐条对应原版三个回调的守卫与迁移。</summary>
    public sealed class WildcardTutorialLogic
    {
        public WildcardTutorialStep State { get; private set; } = WildcardTutorialStep.NoState;

        /// <summary>= `StartWildcardTutorial` 的状态那一半：`!= End` ⇒ 进 `Filter` 并返回 true；`End` ⇒ false（不重启）。</summary>
        public bool Start()
        {
            if (State == WildcardTutorialStep.End) return false;
            State = WildcardTutorialStep.Filter;
            return true;
        }

        /// <summary>= `ToggleFilters(on)`：**只有** `on && State == Filter` 才迁到 `Owned`。</summary>
        public bool OnFilterToggled(bool on)
        {
            if (!on || State != WildcardTutorialStep.Filter) return false;
            State = WildcardTutorialStep.Owned;
            return true;
        }

        /// <summary>= `ToggleOwned(owned)`：**只有** `!owned && State == Owned` 才迁到 `End`。</summary>
        public bool OnOwnedToggled(bool owned)
        {
            if (owned || State != WildcardTutorialStep.Owned) return false;
            State = WildcardTutorialStep.End;
            return true;
        }

        public void Reset() { State = WildcardTutorialStep.NoState; }
    }

    /// <summary>宿主（收藏窗卡池面板）要提供的三样：两个开关的节点与状态、卡池页签节点。</summary>
    public interface IWildcardTutorialBindings
    {
        Transform FilterToggleNode { get; }
        Transform OwnedToggleNode { get; }
        Transform CardsTabNode { get; }
        bool FilterOn { get; }
        bool OwnedOn { get; }
        /// <summary>= 原版 `filterToggle.isOn = false`（第三步之前要把筛选关掉）。</summary>
        void SetFilterOn(bool on);
    }

    /// <summary>万能牌教程（组件）。`Update` 轮询两个开关状态 —— 我们这套 UI 没有 `EverguildToggle.onValueChanged`，
    /// 用轮询做**等价订阅**（只在值变化的那一拍触发迁移，不会重复弹指针）。</summary>
    public class WildcardTutorial : MonoBehaviour
    {
        public const string PrefDone = "WfWildcardTutorial.Done";

        // 词条（原版 `LocalizationManager.GetTermTranslation` 的两个 key）
        public const string TermStep1 = "Tips/WildcardTutorial1";
        public const string TermStep2 = "Tips/WildcardTutorial2";

        // 节点名（原版 `.ctor` 的 `buttonObjNames` 字典实读）
        public const string NodeFilterToggle = "Filter Toggle";
        public const string NodeOwnedToggle = "Owned Toggle";
        public const string NodeCardsTab = "CardsTab";

        // 指针偏移（Intro.unity:7694-7696 实读；**y 向上**，用时取反）
        public static readonly Vector2 OffsetFilterToggle = new Vector2(0f, -10f);
        public static readonly Vector2 OffsetOwnedToggle = new Vector2(-20f, 0f);
        public static readonly Vector2 OffsetFixed = new Vector2(-270f, 690f);

        static WildcardTutorial _instance;

        readonly WildcardTutorialLogic _logic = new WildcardTutorialLogic();
        IWildcardTutorialBindings _bindings;
        TutorialPointerUi _pointer;
        Transform _root;
        bool _hadFilter, _hadOwned, _primed;
        bool _pointerShown;

        /// <summary>本机有没有做过（= 原版 `state == End` 的持久化等价）。</summary>
        public static bool IsDone { get { return PlayerPrefs.GetInt(PrefDone, 0) == 1; } }
        public static void ResetProgressForTest() { PlayerPrefs.DeleteKey(PrefDone); }

        /// <summary>起一次（已经做过 ⇒ 返回 null，**不重启** = 原版 `state == End` 那一支）。</summary>
        public static WildcardTutorial TryStart(Transform root, IWildcardTutorialBindings bindings)
        {
            if (IsDone)
            {
                Debug.Log("[WildcardTutorial] 已做过 ⇒ 不重启（= 原版 `currentState == End` 那一支）");
                return null;
            }
            return ForceStart(root, bindings);
        }

        /// <summary>无视存档强起（自检 / 调试）。</summary>
        public static WildcardTutorial ForceStart(Transform root, IWildcardTutorialBindings bindings)
        {
            if (root == null || bindings == null)
            {
                Debug.LogWarning("[WildcardTutorial] 起不来：root/bindings 为空（⛔ 不静默）");
                return null;
            }
            if (_instance != null) Destroy(_instance.gameObject);
            var go = new GameObject("WildcardTutorial");
            go.transform.SetParent(root, false);
            var w = go.AddComponent<WildcardTutorial>();
            w._root = root;
            w._bindings = bindings;
            w._pointer = TutorialPointerUi.Create(root, TutorialPointerUi.Tier.Wildcards);
            w._pointer.Hide();
            if (!w._logic.Start())
            {
                Debug.Log("[WildcardTutorial] 状态机拒绝启动（已是 End）");
                return null;
            }
            w._hadFilter = bindings.FilterOn;
            w._hadOwned = bindings.OwnedOn;
            w._primed = true;
            w.ShowPointerFor(WildcardTutorialStep.Filter);
            _instance = w;
            Debug.Log("[WildcardTutorial] 起：state = Filter（= 原版 `StartWildcardTutorial`）");
            return w;
        }

        /// <summary>玩家点开了卡详情（原版 `CardDisplayOpened`：**只收指针、不改 state**）。</summary>
        public static void NotifyCardDisplayOpened()
        {
            if (_instance == null) return;
            _instance.HidePointer();
            Debug.Log("[WildcardTutorial] 卡详情打开 ⇒ 收指针（原版 `CardDisplayOpened`，state 不变）");
        }

        public WildcardTutorialStep State { get { return _logic.State; } }
        public bool PointerVisible { get { return _pointerShown; } }
        public TutorialPointerUi Pointer { get { return _pointer; } }

        void Update()
        {
            if (_bindings == null || _pointer == null) return;
            if (!_primed) return;

            bool f = _bindings.FilterOn, o = _bindings.OwnedOn;
            bool fChanged = f != _hadFilter, oChanged = o != _hadOwned;
            _hadFilter = f; _hadOwned = o;

            // ① 原版 `ToggleFilters`：开筛选 ⇒ 换指 Owned + 第一段文案 + state=Owned
            if (fChanged && _logic.OnFilterToggled(f))
            {
                HidePointer();
                ShowPointerFor(WildcardTutorialStep.Owned);
                Debug.Log("[WildcardTutorial] 筛选已开 ⇒ state = Owned（文案 `" + TermStep1 + "`）");
                return;
            }
            // ② 原版 `ToggleOwned`：关掉 Owned ⇒ `filterToggle.isOn = false` → 固定位文字 → state=End + 记存档
            if (oChanged && _logic.OnOwnedToggled(o))
            {
                HidePointer();
                _bindings.SetFilterOn(false);      // 原版 `filterToggle.isOn = false`
                ShowPointerFor(WildcardTutorialStep.End);
                PlayerPrefs.SetInt(PrefDone, 1);
                PlayerPrefs.Save();
                Debug.Log("[WildcardTutorial] Owned 已关 ⇒ state = End（固定位文案 `" + TermStep2 + "`）；已记完成");
            }
        }

        void ShowPointerFor(WildcardTutorialStep step)
        {
            if (_pointer == null) return;
            Transform target = null;
            Vector2 off = Vector2.zero;
            string text = null;
            bool showPointer = true;
            switch (step)
            {
                case WildcardTutorialStep.Filter:
                    target = _bindings != null ? _bindings.FilterToggleNode : null;
                    off = OffsetFilterToggle;
                    break;
                case WildcardTutorialStep.Owned:
                    target = _bindings != null ? _bindings.OwnedToggleNode : null;
                    off = OffsetOwnedToggle;
                    text = Loc.T(TermStep1);
                    break;
                case WildcardTutorialStep.End:
                    target = _bindings != null ? _bindings.CardsTabNode : null;
                    off = OffsetFixed;
                    text = Loc.T(TermStep2);
                    showPointer = false;               // 原版 `showPointer: false`
                    break;
            }
            if (target == null)
            {
                // 目标还没建（页签没出来）⇒ 下一帧再试；⛔ 不静默当成完成
                _pointerShown = false;
                return;
            }
            Vector2 p = MenuTutorialFlow.CenterPx(target) + new Vector2(off.x, -off.y);   // y 向上 → 向下
            _pointer.Show(p, text, showPointer);
            _pointerShown = true;
        }

        void HidePointer()
        {
            if (_pointer != null) _pointer.Hide();
            _pointerShown = false;
        }

        void OnDestroy()
        {
            if (_instance == this) _instance = null;
            if (_pointer != null) Destroy(_pointer.gameObject);
        }

        // ============================================================ 自检
        /// <summary>纯状态机自检（不建场景）。**含灭自证**：
        /// ① 乱序事件必须被守卫挡住（`Owned(false)` 在 Filter 态不迁移、`Filter(true)` 在 Owned 态不重复迁移）；
        /// ② 只能经 `Owned(false)` 到 End —— 若有人把两条迁移写成「恒前进」，上面两条守卫生效时这条就到不了 End。</summary>
        public static bool SelfCheck(out string why)
        {
            why = null;
            var m = new WildcardTutorialLogic();
            if (m.State != WildcardTutorialStep.NoState) { why = "初始应为 NoState"; return false; }
            if (m.OnOwnedToggled(false)) { why = "NoState 下 Owned 不该迁移"; return false; }
            if (!m.Start() || m.State != WildcardTutorialStep.Filter) { why = "Start 应进 Filter"; return false; }
            if (m.OnFilterToggled(false)) { why = "关筛选不该迁移"; return false; }
            if (m.OnOwnedToggled(false)) { why = "Filter 态下 Owned 不该迁移（乱序守卫）"; return false; }
            if (!m.OnFilterToggled(true) || m.State != WildcardTutorialStep.Owned)
            { why = "开筛选应进 Owned"; return false; }
            if (m.OnFilterToggled(true)) { why = "Owned 态下重复开筛选不该再迁移"; return false; }
            if (m.OnOwnedToggled(true)) { why = "打开 Owned 不该迁移"; return false; }
            if (!m.OnOwnedToggled(false) || m.State != WildcardTutorialStep.End)
            { why = "关 Owned 应到 End"; return false; }
            if (m.Start()) { why = "End 后 Start 不该重启"; return false; }
            return true;
        }
    }
}
