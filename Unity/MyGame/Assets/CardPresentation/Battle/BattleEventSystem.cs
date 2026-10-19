// BattleEventSystem.cs — 战斗场景的 uGUI **事件系统**（`T09` 整层换 uGUI · 步骤 A1）
//
// ─────────────────────────────────────────────────────────────────────────────
// 【它是什么】原版战斗 HUD 的点击走 uGUI：`EventSystem` + 输入模块 + 每个 Canvas 上的
//   `GraphicRaycaster` 做射线、`IPointerClickHandler` 收事件。我们此前**一条都没有**
//   （全仓 `raycastTarget = true` = 0 处；判据 → `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §1·1 / §1·4）。
//   本类只负责「层」：建一只 `EventSystem`，挂 **`InputSystemUIInputModule`**。
//
// 🔴 **模块必须选 `InputSystemUIInputModule`**：`ProjectSettings/ProjectSettings.asset:935` 的
//   `activeInputHandler = 1`（New-only）⇒ `StandaloneInputModule`（旧 Input）**一个事件都收不到、
//   而且不报错**（判据 → `T09` 迁移计划 §1·1 那张表最后一行）。本类建完会**自检模块类型**，不对就 `LogError`。
//   依据 = 本机 `Library/PackageCache/com.unity.inputsystem@7a4e1a2a8194/InputSystem/Runtime/Plugins/UI/
//   InputSystemUIInputModule.cs:1639-1656` 的 `OnEnable`：没有绑定动作时自己 `AssignDefaultActions()`
//   ⇒ 运行时 `AddComponent` 即可用，不需要预做 InputActionAsset。
//
// 🔴 **编辑模式的那条坑（自检宿主就跑在编辑模式）**：
//   `资料/已知的坑.md:1125` + `资料/普查产出_1011/W3_子2.md` §2·4③ 已坐实：
//   **`AddComponent` 在批处理下不跑 `Awake/OnEnable`**，而 **`SetActive(true)` 那一刻会跑**
//   （「`OnEnable` 只在激活那一刻跑」）。`EventSystem.OnEnable` 里那句 `m_EventSystems.Add(this)`
//   正是 `EventSystem.current` 的来源 ⇒ 本类建 GameObject 时**先 `SetActive(false)`、加完组件再
//   `SetActive(true)`**（Play 模式同一条路，零副作用）。
//   ⚠️ 同一族还有 `BaseRaycaster.OnEnable → RaycasterManager.AddRaycaster`（`GraphicRaycaster`
//   没带 `[ExecuteAlways]`，见 `BaseRaycaster.cs:79-83`）—— 由 `SyncRaycasters()` 用**公开口**
//   `RaycasterManager.GetRaycasters()` 补登记（见那个方法的 doc）。
//   ⚠️ 万一「激活会派 OnEnable」这条前提哪天不成立，`EnsureCurrentInEditMode()` 会再兜一次
//   （只往 `EventSystem.m_EventSystems` 里登记本实例），**并出声**，⛔ 不静默半死。
//
// 【API】
//   · `Ensure()`       —— **幂等**：场景里已有（或 `EventSystem.current` 已指）就直接用；没有才建。
//                        🔴 **收养既有那只时校验输入模块**（审计 `P3-2`）：必须只有 `InputSystemUIInputModule`；
//                        挂错（典型 `StandaloneInputModule`）⇒ **拆掉 + 补上、并 `LogWarning` 一次**
//                        （见 `EnsureCompatibleModules`；合规时**零动作**）。
//   · `Instance`       —— 本类建/收养的那一只（自检读它，别在别处再 `FindObjectOfType`）。
//   · `InstanceCount`  —— 场景里 `EventSystem` 的总数（**含未激活**；断言「恰好 1 只」用它）。
//   · `Ready`          —— `Instance != null` 且模块是 `InputSystemUIInputModule`、没有别的输入模块。
//   · `ModuleTypeNameForTest` / `ModuleTypeNamesForTest` —— **只读探针（仅编辑器编译）**：`Instance` 上输入模块的类型名
//                        （`Ready` 只答「合不合规」，这两条答「到底是什么」；诊断用，⛔ 不改任何状态）。
//   · `SyncRaycasters()` —— 把场景里所有 `GraphicRaycaster` 补登记进 `RaycasterManager`（编辑模式用）。
//   · `ResetForTest()` —— 自检收尾：停用并销毁本类建的那一只（幂等）。
//
// 【生命周期】原版的 `EventSystem` 在 `level0`（常驻场景，`GameObject/EventSystem.json`）；
//   我们是**每个战斗场景一只**，`BattleScene` 每轮 `NewScene` 会连场景一起销毁。
//   `Ensure()` 先 `FindObjectsByType` 查重 ⇒ **场景里恰好 1 只**这条由断言守（判据 → `T09` §四 R7）。
// ─────────────────────────────────────────────────────────────────────────────

using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;              // `GraphicRaycaster`（`UnityEngine.UI`，⛔ 不在 `UnityEngine.EventSystems`）

namespace CardPresentation
{
    /// <summary>战斗场景的 uGUI 事件系统（`EventSystem` + `InputSystemUIInputModule`）。
    /// **判据只此一处** —— 调用点别自己 `new GameObject("EventSystem")`。</summary>
    public static class BattleEventSystem
    {
        /// <summary>本类建（或收养）的那一只。未建 ⇒ null。</summary>
        public static EventSystem Instance { get; private set; }

        /// <summary>层是活的吗：有实例 **且** 输入模块是 `InputSystemUIInputModule` **且** 没有别的模块。
        /// （⛔ 别只判 `Instance != null` —— New-only 下挂错模块 = **一个事件都收不到且不报错**。）
        /// <para>⚠️ 判据与 `EnsureCompatibleModules` **共用** `ModulesAreCompatible` 这一个口（两处写同一条规则 = 迟早不一致）。</para></summary>
        public static bool Ready
        {
            get
            {
                var es = Instance;
                return es != null && ModulesAreCompatible(es);
            }
        }

#if UNITY_EDITOR
        /// <summary>**只读探针（仅编辑器编译；自检 / 审计用）**：`Instance` 上**按组件序第一只**输入模块的类型名；
        /// 没有实例或一只模块都没有 ⇒ `"none"`。**纯读**（`Ready` 只答「合不合规」，这条答「到底是什么」）。</summary>
        public static string ModuleTypeNameForTest
        {
            get
            {
                var names = ModuleTypeNames(Instance);
                return names.Count == 0 ? "none" : names[0];
            }
        }

        /// <summary>**只读探针（仅编辑器编译）**：`Instance` 上**全部**输入模块的类型名（逗号分隔、按组件序）；
        /// 没有 ⇒ `"none"`。用来分辨「合规」`"InputSystemUIInputModule"` 与「混了旧模块」
        /// `"StandaloneInputModule,InputSystemUIInputModule"` 这两种情况（`Ready` 只说后者不行）。</summary>
        public static string ModuleTypeNamesForTest
        {
            get
            {
                var names = ModuleTypeNames(Instance);
                return names.Count == 0 ? "none" : string.Join(",", names);
            }
        }
#endif

        /// <summary>场景里 `EventSystem` 的总数（**含未激活对象** —— uGUI 约定恰好 1 只）。
        /// 自检断言「恰 1 个」用它（⛔ 别 `FindObjectOfType` —— 那个只给第一个、查不出重复）。</summary>
        public static int InstanceCount
        {
            get
            {
                var all = UnityEngine.Object.FindObjectsByType<EventSystem>(
                    FindObjectsInactive.Include, FindObjectsSortMode.None);
                int n = 0;
                for (int i = 0; i < all.Length; i++) if (all[i] != null) n++;
                return n;
            }
        }

        /// <summary>建（**幂等**）。
        /// <para>顺序：① `EventSystem.current` 已指一只 ⇒ 直接收养；② 场景里已有 ⇒ 收养第一只
        /// （多于一只时**出声**，本类不替你销毁别人的）；③ 都没有 ⇒ 现建。
        /// 现建时 **先 `SetActive(false)` → `AddComponent<EventSystem>()` → `AddComponent&lt;InputSystemUIInputModule&gt;()`
        /// → `SetActive(true)`** —— 编辑模式只有「激活那一刻」派 `OnEnable`（见文件头那条坑）。</para>
        /// <para>🔴 **收养（①②）时校验并修好输入模块**（审计 `P3-2`）：见 `EnsureCompatibleModules` ——
        /// 合规零动作；挂错（`StandaloneInputModule` 之类）⇒ 拆掉 + 补上 + `LogWarning` 一次。</para>
        /// <para>建/收养完都会 `SyncRaycasters()`（把已存在的 `GraphicRaycaster` 补登记）。</para></summary>
        public static EventSystem Ensure()
        {
            if (Instance != null) return Instance;

            // 编辑器不派 `OnDisable` 的那一档会留下「已销毁」的死件 —— 先清掉再读 `current`
            //（否则 `current` 会返回一个 Unity-null 的残件，`current != null` 为假 ⇒ 误判「没有」）。
            PruneDeadRegistry();

            // ① 已有 current（Play 模式正常路径 / 上一次自检留下且还活着）
            var cur = EventSystem.current;
            if (cur != null)
            {
                Instance = cur;
                EnsureCompatibleModules(cur);        // 🔴 审计 `P3-2`：收养前先查模块（New-only 下挂错 = 静默全废）
                SyncRaycasters();
                return Instance;
            }

            // ② 场景里已经有一只（但没登记成 current —— 例如它是「建的时候就是活的」那一路）
            var existing = UnityEngine.Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            if (existing.Length > 1)
                Debug.LogError($"[BattleEventSystem] 场景里有 **{existing.Length}** 只 `EventSystem`"
                             + "（uGUI 约定恰好 1 只）—— 本类**不替你销毁别的**，先查是谁建的"
                             + "（判据 → `T09` 迁移计划 §四 R7）");
            if (existing.Length > 0)
            {
                EventSystem pick = null;
                for (int i = 0; i < existing.Length; i++)
                    if (existing[i] != null) { pick = existing[i]; break; }
                if (pick != null)
                {
                    Instance = pick;
                    EnsureCompatibleModules(pick);   // 🔴 同上：这条收养路一样要查模块
                    EnsureCurrentInEditMode(Instance);   // 编辑器里它可能没进 `m_EventSystems`
                    SyncRaycasters();
                    return Instance;
                }
            }

            // ③ 现建
            var go = new GameObject("EventSystem");
            go.SetActive(false);                 // 见文件头：编辑模式只有「激活那一刻」派 OnEnable
            var es = go.AddComponent<EventSystem>();
            var mod = go.AddComponent<InputSystemUIInputModule>();
            go.SetActive(true);
            Instance = es;

            if (!(mod is InputSystemUIInputModule))
                Debug.LogError("[BattleEventSystem] 输入模块挂**错**了（不是 `InputSystemUIInputModule`）"
                             + " —— `activeInputHandler = 1`（New-only）下**一个事件都收不到**（判据 → `T09` §1·1）");

            // 激活那一刻 `OnEnable` 应该已经把本实例登记进 `m_EventSystems`；没登记就兜一次（并出声）。
            if (EventSystem.current != es) EnsureCurrentInEditMode(es);

            SyncRaycasters();
            return es;
        }

        // ── 输入模块校验 / 修复（审计 `P3-2`）────────────────────────────────────────

        /// <summary>输入模块**合规**的唯一判据（`Ready` 与 `EnsureCompatibleModules` **共用这一口** ——
        /// 两处写同一条规则 = 迟早不一致）：
        /// 至少一只 `InputSystemUIInputModule`、且**没有**任何别的 `BaseInputModule`。</summary>
        static bool ModulesAreCompatible(EventSystem es)
        {
            if (es == null) return false;
            var mods = es.GetComponents<BaseInputModule>();
            bool hasNew = false;
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i] == null) continue;
                if (mods[i] is InputSystemUIInputModule) hasNew = true;
                else return false;                       // 混进任何别的模块（典型 `StandaloneInputModule`）⇒ 不合规
            }
            return hasNew;
        }

        /// <summary>`es` 上全部输入模块的类型名（按组件序）。**只读**（只有编辑器探针用；空表 ⇒ 一只都没有）。</summary>
        static List<string> ModuleTypeNames(EventSystem es)
        {
            var names = new List<string>();
            if (es == null) return names;
            var mods = es.GetComponents<BaseInputModule>();
            for (int i = 0; i < mods.Length; i++)
                if (mods[i] != null) names.Add(mods[i].GetType().Name);
            return names;
        }

        /// <summary>收养既有 `EventSystem` 后**校验并修好**输入模块（审计 `P3-2` 收口）。
        /// <para>🔴 为什么：`activeInputHandler = 1`（New-only）下 `StandaloneInputModule`（旧 Input）
        /// **一个事件都收不到、而且不报错** —— 收养一只挂错模块的 `EventSystem` = 整个 HUD **静默全废**
        /// （判据 → `T09` 迁移计划 §1·1）。</para>
        /// <para>合规（见 `ModulesAreCompatible`）⇒ **零动作、不出声**。
        /// 不合规 ⇒ **拆掉**所有非 `InputSystemUIInputModule` 的模块、**缺了就补一只**，并 `LogWarning` **一次**。</para>
        /// <para>⚠️ 补的那只直接在**当时的激活状态**上 `AddComponent`：Play 模式那一刻就派 `OnEnable`
        /// ⇒ `HasNoActions() ⇒ AssignDefaultActions()`（`InputSystemUIInputModule.cs:1639-1656`）立刻可用；
        /// 编辑模式不派（见文件头那条坑）也**不需要** —— 自检只直接调 `EventSystem.RaycastAll`（不经过模块），
        /// 而 Play 模式进场时 Unity 会重新派一遍。
        /// ⛔ **不要**为此把 GameObject 停用再激活 —— 那会改 `EventSystem.current` 的登记次序
        /// （`OnDisable` 从 `m_EventSystems` 摘掉、`OnEnable` 追加到**表尾**），还可能顺手激活别人关着的那只。</para></summary>
        static void EnsureCompatibleModules(EventSystem es)
        {
            if (es == null) return;
            if (ModulesAreCompatible(es)) return;        // 合规 ⇒ 零动作（不出声）

            var mods = es.GetComponents<BaseInputModule>();
            var names = new List<string>();
            bool hasNew = false;
            for (int i = 0; i < mods.Length; i++)
            {
                if (mods[i] == null) continue;
                names.Add(mods[i].GetType().Name);
                if (mods[i] is InputSystemUIInputModule) hasNew = true;
            }

            // 出声（**一次**）：把拿到的东西原样写出来 —— 定位「是谁先建了一只挂错模块的 EventSystem」要靠它
            Debug.LogWarning("[BattleEventSystem] 收养的 `EventSystem`「" + es.gameObject.name + "」输入模块**不合规**："
                           + (names.Count == 0 ? "（一只模块都没有）" : string.Join(" + ", names))
                           + " —— `activeInputHandler = 1`（New-only）下旧输入模块**一个事件都收不到且不报错**，"
                           + "整个 HUD 会静默全废；已移除所有非 `InputSystemUIInputModule` 的模块"
                           + (hasNew ? "" : "、并补上一只 `InputSystemUIInputModule`")
                           + "（判据 → `T09` 迁移计划 §1·1 / 审计 `P3-2`）");

            for (int i = 0; i < mods.Length; i++)
            {
                var m = mods[i];
                if (m == null || m is InputSystemUIInputModule) continue;
                // Play：先停用 ⇒ 当场退出 `EventSystem.Update` 的模块挑选（它的 `OnDisable` 会 `UpdateModules()` 重算表），
                // 再 `Destroy`（与 `ResetForTest` 同一条口径）。编辑模式**不碰 `enabled`** —— 那里 `OnEnable` 没跑过、
                // 模块自己的 `m_EventSystem` 还是 null，白挨一条 `OnDisable` 里的 NRE 没有意义。
                if (Application.isPlaying) { m.enabled = false; UnityEngine.Object.Destroy(m); }
                else UnityEngine.Object.DestroyImmediate(m);
            }

            if (!hasNew && es.gameObject.AddComponent<InputSystemUIInputModule>() == null)
                Debug.LogError("[BattleEventSystem] 补 `InputSystemUIInputModule` 失败（`AddComponent` 返回 null）"
                             + " —— 整个 HUD 会收不到事件");
        }

        /// <summary>把场景里**所有** `GraphicRaycaster` 补登记进 `RaycasterManager`（返回本次新增几个）。
        /// <para>🔴 **为什么需要**：`BaseRaycaster.OnEnable` 里那句 `RaycasterManager.AddRaycaster(this)`
        /// 与 `EventSystem.OnEnable` 同一族 —— 编辑模式下 `AddComponent` 不派 `OnEnable`
        /// （`HudCanvas.Ensure/EnsureSubCanvas` 正是「建 GameObject → AddComponent」，判据见文件头那条坑）
        /// ⇒ **自检宿主里射线表可能是空的**，`EventSystem.RaycastAll` 会一条都不返回。
        /// `RaycasterManager.GetRaycasters()` 是**公开口**、返回的就是内部那张表 ⇒ 直接补（`Contains` 去重）。</para>
        /// <para>⚠️ Play 模式它们早已登记，本函数是一次**空操作**（只清已销毁的死件）。</para></summary>
        public static int SyncRaycasters()
        {
            var list = RaycasterManager.GetRaycasters();
            if (list == null)
            {
                Debug.LogError("[BattleEventSystem] `RaycasterManager.GetRaycasters()` 返回 null ⇒ 射线表不可用"
                             + "（`EventSystem.RaycastAll` 将一条都不返回）");
                return 0;
            }
            for (int i = list.Count - 1; i >= 0; i--) if (list[i] == null) list.RemoveAt(i);

            int added = 0;
            var all = UnityEngine.Object.FindObjectsByType<GraphicRaycaster>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                var rc = all[i];
                if (rc == null || list.Contains(rc)) continue;
                list.Add(rc);
                added++;
            }
            return added;
        }

        /// <summary>自检收尾：停用并销毁本类建/收养的那一只（幂等）。**不碰**别人的 `EventSystem`
        /// （本类从没有过第二只；`Instance` 为空时什么都不做）。</summary>
        public static void ResetForTest()
        {
            var es = Instance;
            Instance = null;
            if (es != null)
            {
                var go = es.gameObject;
                if (go != null)
                {
                    go.SetActive(false);     // 停用那一刻派 `OnDisable`（与 OnEnable 对称）
                    if (Application.isPlaying) UnityEngine.Object.Destroy(go);
                    else UnityEngine.Object.DestroyImmediate(go);
                }
            }
            PruneDeadRegistry();
            var list = RaycasterManager.GetRaycasters();
            if (list != null)
                for (int i = list.Count - 1; i >= 0; i--) if (list[i] == null) list.RemoveAt(i);
        }

        // ── 编辑模式兜底（只在 `OnEnable` 没跑 / 没登记时动）────────────────────────────
        //  `EventSystem.m_EventSystems` 是 `private static List<EventSystem>`（uGUI 2.x，
        //  本机源码 `.../EventSystem/EventSystem.cs:27`），**没有公开的登记口**
        //  （`current` 的 setter 对「未知实例」直接 `LogError` 拒绝，`:34-47`）。
        //  ⇒ 只有这一种情况用反射：**非 Play 模式**，且激活后 `current` 仍不是本实例。
        //  ⚠️ 字段名变了 ⇒ **出声**、不静默（那时 `RaycastAll` 仍可用：调用点拿 `Instance` 直接调）。

        static List<EventSystem> EventSystemRegistry()
        {
            var f = typeof(EventSystem).GetField("m_EventSystems",
                BindingFlags.Static | BindingFlags.NonPublic);
            if (f == null)
            {
                Debug.LogError("[BattleEventSystem] 找不到 `EventSystem.m_EventSystems`（uGUI 版本变了？）"
                             + " ⇒ 编辑模式下 `EventSystem.current` 会是 null；`RaycastAll` 请用 `Instance` 调");
                return null;
            }
            return f.GetValue(null) as List<EventSystem>;
        }

        static void PruneDeadRegistry()
        {
            var list = EventSystemRegistry();
            if (list == null) return;
            for (int i = list.Count - 1; i >= 0; i--) if (list[i] == null) list.RemoveAt(i);
        }

        static void EnsureCurrentInEditMode(EventSystem es)
        {
            if (Application.isPlaying) return;          // Play 模式 `OnEnable` 一定跑过
            if (es == null) return;
            var list = EventSystemRegistry();
            if (list == null) return;
            PruneDeadRegistry();
            if (list.Count > 0 && list[0] == es) return;
            int i = list.IndexOf(es);
            if (i >= 0) list.RemoveAt(i);
            list.Insert(0, es);                          // `current` 读的是 `list[0]`
            Debug.Log("[BattleEventSystem] 编辑模式：激活那一刻没把本实例登记成 `EventSystem.current`"
                    + " ⇒ 已手工补登记（`m_EventSystems`，仅编辑模式）");
        }
    }
}
