// EventBadgeController.cs — 原版 `EventBadgeController : SingletonBehaviour<EventBadgeController>`（给「有活动角标的事件」建角标）。
//
// ============================ 判据（第一权威 = 反编译，本轮现读）============================
//   · `out_cpp2il_dev_ilspy/EventBadgeController.cs`（`Awake` 全文 160 行）
//   · `decomp_full/EventBadgeController.__c___Awake_b__0_0.c` = 谓词
//     `LiveOpsAssetUtility.GetComponentReference(e, 0x19 /*25 = EventComponentReferenceType.EventBadge*/) != null`
//   ⇒ `Awake()` 逐句：
//       `base.Awake();`
//       `foreach (e in LiveOpsManager.GetAllEvents().Where(e => GetComponentReference(e, EventBadge) != null)) {`
//           `ref = GetComponentReference(e, EventBadge); if (string.IsNullOrWhiteSpace(ref.AssetGUID)) continue;`
//           `badge = Instantiate(new ComponentReference<EventBadge>(GUID).Load(), transform);`
//           `badge.Initialize(e); }`
//
// ============================ 我们这边缺的两格（⛔ 别当成「已复刻」）============================
//   ① **GUID → prefab 的装载口**：原版走 Addressables（`ComponentReference<EventBadge>(GUID).Load()`）。
//      本仓没有 Addressables；`assets_full` 按 `notificationHolder` 字段名全量搜 = **0 命中** ⇒
//      那个 prefab **不在离线包里**（它是活动组件 `EventComponents[25]` 的远端引用）。
//      ⇒ 这里建出来的是**没有视觉节点的逻辑壳**（`EventBadge.Bind(...)` 留给将来的建树者），并出声一次。
//   ② **数据侧当前恒 0**：本仓 `LiveOpsLocalCatalog` 的 13 份 SO 里**没有任何一份带 `Components[25]`**
//      （`EventBadge = 25` 只出现在枚举里）⇒ 本地跑起来**一个角标都不会建**（这是如实结果，不是 bug）。
//      判据 = `grep -c EventBadge Shell/LiveOpsLocalCatalog.cs` = 1（只有枚举那一行）。
using UnityEngine;

namespace CardPresentation
{
    /// <summary>原版 `EventBadgeController`（单例）。⚠️ 原版基类是 `SingletonBehaviour&lt;T&gt;`；
    /// 本仓没有那个基类（`SingletonInitializableBehaviour&lt;T&gt;` 是**另一条**链：还要等 `OnInitializationComplete`）
    /// ⇒ 这里用最小静态单例，差异如实记在账 `日常_奖励` 第 12 行。</summary>
    public class EventBadgeController : MonoBehaviour
    {
        static EventBadgeController _instance;

        /// <summary>当前实例（没建过 = null）。</summary>
        public static EventBadgeController InstanceOrNull { get { return _instance; } }

        /// <summary>上一次 `EnsureBadges()` 建出几个（自检/诊断用）。</summary>
        public int LastBuiltForTest { get; private set; }

        void Awake()
        {
            if (_instance != null && _instance != this) { Destroy(this); return; }
            _instance = this;
            LastBuiltForTest = EnsureBadges();
        }

        /// <summary>原版 `Awake()` 的循环体（`base.Awake()` 之后那一段）。返回建了几个。</summary>
        public int EnsureBadges()
        {
            int n = 0;
            foreach (var evt in LiveOpsManager.GetAllEvents(false))
            {
                string guid = BadgeGuid(evt);
                if (string.IsNullOrEmpty(guid)) continue;          // 原版：谓词 `!= null` + `IsNullOrWhiteSpace(GUID) ⇒ continue`
                BuildBadge(evt);
                n++;
            }
            if (n > 0)
                Debug.LogWarning("[EventBadgeController] 建了 " + n + " 个角标的**逻辑壳** —— 🔴 视觉节点没绑："
                               + "EventBadge prefab 不在离线包里（`assets_full` 按 `notificationHolder` 搜 0 命中）⇒ "
                               + "`EventBadge.Bind(...)` 还没人调（账 `日常_奖励` 第 12 行仍为 B）");
            return n;
        }

        /// <summary>原版 `Instantiate(ComponentReference<EventBadge>(GUID).Load(), transform)` + `Initialize(e)`。
        /// ⚠️ 我们**Load 不了 prefab**（见文件头 ①）⇒ 只 `AddComponent` 建壳。</summary>
        void BuildBadge(ILiveOps evt)
        {
            var go = new GameObject("Event Badge (" + (evt.GetID() ?? "?") + ")");
            go.transform.SetParent(transform, false);
            var badge = go.AddComponent<EventBadge>();
            badge.Initialize(evt);                                  // id = evt.GetID()（判据与旁证 → EventBadge 文件头 ②）
        }

        /// <summary>该事件的 `EventComponents[EventBadge = 25]` 的 GUID（没有 ⇒ null）。
        /// 数据源 = `LocalLiveOpsEvent.Local.Components`（离线目录逐份照 SO 抄的 `KeyValuePair&lt;int,string&gt;[]`）。</summary>
        public static string BadgeGuid(ILiveOps evt)
        {
            var local = (evt as LocalLiveOpsEvent)?.Local;
            if (local?.Components == null) return null;
            for (int i = 0; i < local.Components.Length; i++)
                if (local.Components[i].Key == (int)LiveOpsComponentRef.EventBadge)
                    return local.Components[i].Value;
            return null;
        }

#if UNITY_EDITOR
        // ============================================================ 结构性自检钩子（B92_N6b）
        /// <summary>结构性自检（不依赖场景/资产）。返回**问题清单**，空 = 通过。
        /// <para>⚠️ 本机数据下 `BadgeGuid` 对 13 份 SO **恒 null**（如实），所以这里只核「判据链路在」。 </para></summary>
        public static System.Collections.Generic.List<string> SelfCheck()
        {
            var errs = new System.Collections.Generic.List<string>();
            var t = typeof(EventBadgeController);
            if (t.GetMethod("Awake", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public) == null)
                errs.Add("缺 `Awake()`（原版有）");
            if (t.GetMethod("BadgeGuid") == null) errs.Add("缺 `BadgeGuid` 读口");
            if ((int)LiveOpsComponentRef.EventBadge != 25) errs.Add("`LiveOpsComponentRef.EventBadge` ≠ 25（判据：`GetComponentReference(e, 0x19)`）");
            int with25 = 0, total = 0;
            foreach (var e in LiveOpsLocalCatalog.Events)
            {
                total++;
                if (BadgeGuidForLocal(e) != null) with25++;
            }
            if (total == 0) errs.Add("离线活动目录是空的（`LiveOpsLocalCatalog.Events`）");
            // 如实记录：本地 13 份 SO 里带 EventBadge 组件引用的应为 0（若某天 >0，说明数据补上了，那条账要回头改）
            Debug.Log("[EventBadgeController] 自检：离线目录 " + total + " 份活动，其中带 `EventComponents[25]` 的 = " + with25);
            return errs;
        }

        static string BadgeGuidForLocal(LiveOpsLocalEvent e)
        {
            if (e?.Components == null) return null;
            for (int i = 0; i < e.Components.Length; i++)
                if (e.Components[i].Key == (int)LiveOpsComponentRef.EventBadge) return e.Components[i].Value;
            return null;
        }
#endif
    }
}
