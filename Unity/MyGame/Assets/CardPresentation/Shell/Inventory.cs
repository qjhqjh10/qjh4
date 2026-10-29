// Inventory.cs —— **背包框架层**（原版 `Inventory<T>` / `IItemInfo<T>` / `InventorySubset` 的最小可用实现）
//
// ============================ 这是什么 / 不是什么 ============================
// 判据（唯一来源）：`资料/完全复刻/判据/泛型与启动链.md` §三 #3「要新建的 7 件」+ 那一节的逐方法表
// （原版 `Inventory.cs:685-1748` · `ItemInfo.cs:213-252` · `InventorySubset.cs:112-281`）。
//
// 🔴 **口径（用户 2026-09-17 定，2026-10-24 补）**：「卡池 / 锻造厂 / 卡牌 / 头像**全解锁**」、资源固定 **2000**、
//    **不做真实经济** ⇒ 背包**不参与解锁判定**，只做「**数量 / 实例 / 限购**的正本」。
//    ⇒ 本类**不是**「解锁闸」；谁拿它当闸，就是把它用错了。
//
// ============================ 与原版的对应（逐条）============================
//   `Inventory<T>`            → <see cref="Inventory{T}"/>（Add / Create / Get / GetItems / GetItemInstances /
//                                GetWhere / TryGetItem / OnItemCountModify / UpdateItemCollection / RegisterSubset）
//   `IItemInfo<T>`/`ItemInfo<T>` → <see cref="ItemInfo{T}"/>（Item / Amount / InstanceId / EphemeralState +
//                                SetInfo / Add / SetAmount / AssignEphemeralState）
//   `InventorySubset`         → <see cref="InventorySubset{T}"/>（一份「按 id 的视图」+ OnUpdate）
//   `InventoryManager.GetInventory<T>()` → <see cref="Inventories"/>（进程级注册表）
//   `Inventory.OnItemCountModify` 的存档 → <see cref="InventoryStore"/>（**本框架自己的持久化**）
//
// ⚠️ **与原版的两处如实差异**（⛔ 不是「已经一样」）：
//   ① 原版 `T : IIdentifiable` 直接带 `GetID()`；我们的 T 是普通 class ⇒ **构造时注入 `Func<T,string> idOf`**
//      （同一套语义、换一个取 id 的口）。
//   ② 原版 `LoadableRef<T>` 是惰性资源引用（G §三 #6 还没建）；我们这里 `Item` 就是**直接的 T**。
//      将来 `LoadableRef<T>` 落地时，`ItemInfo.Item` 改成引用 + 加一个 `Value` 访问器即可（调用点不用改）。
//
// ⚠️ `EphemeralState`：原版是**类**（`LiveopsReference boundEvent` + `long ephemeralStartTime` + `long ephemeralDuration`，见 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EphemeralState.cs`）—— 2026-10-26 就地订正：原来这里写「原版是枚举、我们存 int」，**两处都错**（原版不是枚举；本轮已建同名类，见文件尾追加区）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    // ============================================================ 条目
    /// <summary>一个物品条目（原版 `IItemInfo<T>` / `ItemInfo<T>`）。
    /// 字段/方法对位：`Item`(= `LoadableRef<T> Item`) · `Amount` · `InstanceId` · `EphemeralState`
    /// + `SetInfo` / `Add` / `SetAmount` / `AssignEphemeralState`。</summary>
    public partial class ItemInfo<T> where T : class
    {
        /// <summary>物品本体（原版是 `LoadableReference<T>`；我们直接持有 —— 见文件头「差异 ②」）。</summary>
        public T Item { get; private set; }
        /// <summary>数量。**这是「拥有数」的正本**（⛔ 别在别处再存第二份）。</summary>
        public int Amount { get { return _quantityBoundTo != null ? _quantityBoundTo.Amount : _amount; } private set { _amount = value; } }
        /// <summary>实例 id（原版 `IItemInfo.instanceId`；同一物品可有多份**不同实例**时用它区分）。
        /// 空串 = 合并到「该物品那一条」。</summary>
        public string InstanceId { get; private set; }
        /// <summary>临时态（原版 `EphemeralState`：限时物品的到期信息）。我们用 `int`，语义待用到时再定。</summary>
        public EphemeralState EphemeralState { get; private set; }

        internal ItemInfo(T item, int amount, string instanceId)
        {
            Item = item; Amount = amount; InstanceId = instanceId ?? "";
        }

        /// <summary>原版 `ItemInfo.SetInfo(LoadableReference<T>, int, string)`：设物品 + 数量 + 实例 id。</summary>
        public void SetInfo(T item, int qty, string instanceId = null)
        {
            if (item == null) return;              // ⛔ 不静默换掉已有物品
            Item = item; Amount = qty; InstanceId = instanceId ?? "";
        }

        /// <summary>原版 `ItemInfo.Add(int amount, string instanceId)`：加数量（可换实例 id）。
        /// ⚠️ 原版里 `amount` 可以为负（扣数量）；负数把 `Amount` 压到 0 为止（不出现负库存）。</summary>
        public virtual void Add(int amount, string instanceId = null)   // 原版 `ItemInfo.Add` 就是 `virtual`（`QuantityBoundItemInfo` 要 override 成空方法）
        {
            Amount = Math.Max(0, Amount + amount);
            if (!string.IsNullOrWhiteSpace(instanceId)) InstanceId = instanceId;   // 原版 `ItemInfo.Add`：`IsNullOrWhiteSpace` ⇒ 传 "" / " " **不会**抹掉实例 id（原来写 `!= null` 会抹，2026-10-27 B26 改）
        }

        /// <summary>原版 `ItemInfo.SetAmount(int)`：直接设数量。</summary>
        public void SetAmount(int amount) { Amount = Math.Max(0, amount); }

        /// <summary>原版 `ItemInfo.AssignEphemeralState`：**未到期就叠加时长**（`ItemInfo.cs:252-270` 逐句：当前态 `HasExpiration` ∧ `EphemeralEndTime >= 服务器时间` ⇒ `AddDuration(新时长)` 并 return；否则整体替换）。2026-10-27 B26 前这里是无条件替换（真差异）。</summary>
        public void AssignEphemeralState(EphemeralState state) { if (state == null) return; EphemeralState cur = EphemeralState; if (cur != null && cur.HasExpiration && !(cur.EphemeralEndTime < DateTime.UtcNow)) { if (cur.EphemeralDuration > 0) cur.AddDuration(state.EphemeralDuration); return; } EphemeralState = state; }

        public override string ToString() { return (Item != null ? Item.ToString() : "<null>") + " ×" + Amount; }
    }

    // ============================================================ 背包
    /// <summary>一份背包（原版 `Inventory<T>`）：`id → 条目` + 变更事件 + 子集。
    /// ⚠️ **它不是解锁闸**（见文件头口径）：只做数量/实例/限购的正本。</summary>
    public partial class Inventory<T> where T : class
    {
        readonly Func<T, string> _idOf;
        readonly Dictionary<string, ItemInfo<T>> _byId = new Dictionary<string, ItemInfo<T>>();
        readonly List<InventorySubset<T>> _subsets = new List<InventorySubset<T>>();

        /// <summary>数量变化（原版 `OnItemCountModify` 的对**外**口）。参数 = (物品, **变化量**)（可为负）。</summary>
        public event Action<T, int> OnItemCountChange;

        /// <param name="idOf">怎么从 T 取 id（原版靠 `IIdentifiable.GetID()`；我们注入 —— 见文件头「差异 ①」）。
        /// ⚠️ **同一个 T 实例必须恒给同一个 id**，否则数量会分裂到两条上。</param>
        public Inventory(Func<T, string> idOf)
        {
            if (idOf == null) throw new ArgumentNullException("idOf");
            _idOf = idOf;
        }

        /// <summary>这一份背包里现在有多少条（自检/诊断用）。</summary>
        public int Count { get { return _byId.Count; } }

        /// <summary>原版 `Inventory.Add(item, amount, instanceId, customData, overrideAmount)`。
        /// 我们的 `customData`（原版给条目挂的自定义字典）**暂不需要** ⇒ 不做形参（等真有调用点再加，⛔ 别先建空壳）。
        /// `overrideAmount = true` ⇒ 直接设成 `amount`（不累加）。</summary>
        public ItemInfo<T> Add(T item, int amount, string instanceId = null, bool overrideAmount = false)
        {
            if (item == null) return null;
            string id = _idOf(item);
            if (string.IsNullOrEmpty(id)) return null;         // ⛔ 没有 id 的物品不进背包（出声由调用方负责）

            ItemInfo<T> info;
            if (!_byId.TryGetValue(id, out info))
            {
                // 原版 `Create`：第一次见就建一条（数量 = amount）
                info = new ItemInfo<T>(item, overrideAmount ? Math.Max(0, amount) : 0, instanceId);
                _byId[id] = info;
                if (!overrideAmount) info.Add(amount, instanceId);
            }
            else
            {
                if (overrideAmount) info.SetAmount(amount);
                else info.Add(amount, instanceId);
            }
            OnItemCountModifyInternal(item, overrideAmount ? amount : amount);
            return info;
        }

        /// <summary>原版 `Inventory.Create(item, instanceId, customData)`：**只建条、不加量**（数量 0）。</summary>
        public ItemInfo<T> Create(T item, string instanceId = null)
        {
            if (item == null) return null;
            string id = _idOf(item);
            if (string.IsNullOrEmpty(id)) return null;
            ItemInfo<T> info;
            if (_byId.TryGetValue(id, out info)) return info;
            info = new ItemInfo<T>(item, 0, instanceId);
            _byId[id] = info;
            NotifySubsets(info);
            return info;
        }

        /// <summary>原版 `Inventory.Get(IIdentifiable item, bool autoCreate)`：按物品取条目；
        /// `autoCreate = true` 且还没有时走 **`Add(item, 0, null, null, false)`**（判据 `Inventory.cs:1619-1680`：不是 `Create` ⇒ 会照常发一次数量变化事件）。</summary>
        public ItemInfo<T> Get(T item, bool autoCreate = false)
        {
            if (item == null) return null;
            string id = _idOf(item);
            if (string.IsNullOrEmpty(id)) return null;
            ItemInfo<T> info;
            if (_byId.TryGetValue(id, out info)) return info;
            return autoCreate ? Add(item, 0, null, null, false) : null;
        }

        /// <summary>原版 `Inventory.Get(string id)`（内部转字典；查不到给 null）。</summary>
        public ItemInfo<T> Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            ItemInfo<T> info;
            return _byId.TryGetValue(id, out info) ? info : null;
        }

        /// <summary>原版 `Inventory.TryGetItem(string id, out IItemInfo<T>)`。</summary>
        public bool TryGetItem(string id, out ItemInfo<T> item)
        {
            item = null;
            if (string.IsNullOrEmpty(id)) return false;
            return _byId.TryGetValue(id, out item) && item != null;
        }

        /// <summary>原版 `Inventory.TryGetItem(Func<T,bool>, out IItemInfo<T>)`：按谓词取第一个。</summary>
        public bool TryGetItem(Func<T, bool> predicate, out ItemInfo<T> item)
        {
            item = null;
            if (predicate == null) return false;
            foreach (var kv in _byId)
            {
                if (kv.Value == null) continue;
                if (!predicate(kv.Value.Item)) continue;
                item = kv.Value;
                return true;
            }
            return false;
        }

        /// <summary>原版 `Inventory.GetItems()`：全部条目（**含数量为 0 的**）。</summary>
        public IEnumerable<ItemInfo<T>> GetItems() { return _byId.Values; }

        /// <summary>原版 `Inventory.GetItemInstances(bool useQuantity)`：**返回物品本身（`IEnumerable&lt;T&gt;`，不是条目）**，次数 = `useQuantity ? Quantity : Math.Min(Quantity,1)`。
        /// 判据 = `decomp_full/Inventory.__c__DisplayClass22_0_object____GetItemInstances_b__0.c`（`Enumerable.Repeat(item.Item, 次数)`）。🔴 2026-10-27 B26 改两处真差异：① 返回类型原为 `ItemInfo&lt;T&gt;`；② 次数原 `Math.Max(1,Amount)` ⇒ 数量 0 的条目也会飞 1 个（原版 0 个）。</summary>
        public IEnumerable<T> GetItemInstances(bool useQuantity = true)
        {
            foreach (var kv in _byId)
            {
                var info = kv.Value;
                if (info == null || info.Item == null) continue;
                int n = useQuantity ? info.Amount : Math.Min(info.Amount, 1);
                for (int i = 0; i < n; i++) yield return info.Item;
            }
        }

        /// <summary>原版 `Inventory.GetWhere(Func<T,bool>)`：满足谓词的全部条目。</summary>
        public IEnumerable<ItemInfo<T>> GetWhere(Func<T, bool> predicate)
        {
            if (predicate == null) yield break;
            foreach (var kv in _byId)
            {
                var info = kv.Value;
                if (info == null || !predicate(info.Item)) continue;
                yield return info;
            }
        }

        /// <summary>原版 `Inventory.OnItemCountModify(T item, int amount)`：**对外**的数量变化入口。</summary>
        public virtual void OnItemCountModify(T item, int amount)
        {
            if (item == null) return;
            string id = _idOf(item);
            if (string.IsNullOrEmpty(id)) return;
            ItemInfo<T> info;
            if (!_byId.TryGetValue(id, out info))
            {
                info = new ItemInfo<T>(item, 0, null);
                _byId[id] = info;
            }
            info.Add(amount);
            OnItemCountModifyInternal(item, amount);
        }

        /// <summary>原版 `Inventory.OnItemCountModifyInternal(IIdentifiable, int)`：内部处理 —— 更新字典（上面已做）→ **调虚拟钩子 <see cref="OnItemCountModified"/>** → 通知子集 → 抛 <see cref="OnItemCountChange"/>。</summary>
        /// <para>🔴 2026-10-26（批次 3 Q1）就地补齐：原版 `Inventory.cs:1055-1120` 的**第一句**就是这个 `protected virtual` 扩展点，本类原来漏了 ⇒ 子类 override 永远不会被 `Add` 触发（静默）。钩子改名多一个 d（`OnItemCountModified`）是因为本类**已有**一个 public `OnItemCountModify` 当「外部改数量」的入口（`ShopData`/`ShopScene` 在用）—— 两个同名重载会打架，合并又会变成钩子调自己。</para>
        void OnItemCountModifyInternal(T item, int amountChanged)
        {
            ItemInfo<T> info;
            if (item != null)
            {
                string id = _idOf(item);
                if (!string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out info)) NotifySubsets(info);
            }
            OnItemCountModified(item, amountChanged); if (OnItemCountChange != null) OnItemCountChange(item, amountChanged);
        }

        void NotifySubsets(ItemInfo<T> info)
        {
            for (int i = 0; i < _subsets.Count; i++) _subsets[i].OnSourceChanged(info);
        }

        /// <summary>原版 `Inventory.UpdateItemCollection()`：`items = subSets.SelectMany(kv =&gt; kv.Value.InternalDictionary).ToDictionary(k =&gt; k.Key, k =&gt; (IItemInfo&lt;T&gt;)k.Value)`
        /// （判据 = 三个编译器 lambda：`decomp_full/Inventory.__c_object____UpdateItemCollection_b__12_0/1/2.c`）。2026-10-27 B26 补齐真体。
        /// ⚠️ **没有登记子集时不动字典**（🔶 自己设计：原版 `subSets` 是服务器下发的那份上游字典，我们没那层 ⇒ 照抄会把本地种子数据清空 ⇒ 只在「有子集」时重建）。调度台 2026-10-27 裁定（B26）。
        /// 真体重建那一段在文件尾的 `partial` 第二部（`RebuildFromSubsets`）。</summary>
        public virtual void UpdateItemCollection() { if (_subsets.Count == 0) return; RebuildFromSubsets(); }

        /// <summary>原版 `Inventory.RegisterSubset(Type, InventorySubset)`：`subSets[type] = set` + `set.OnUpdate += UpdateItemCollection`（`Inventory.cs:1126-1134`）。2026-10-27 B26 补了第二句（原来只登记不发订阅）⇒ `type` 传 null = 按 `T` 记。</summary>
        public void RegisterSubset(Type type, InventorySubset<T> set)
        {
            if (set == null) return;
            if (!_subsets.Contains(set)) { _subsets.Add(set); set.OnUpdate += UpdateItemCollection; }
            _subsetsByType[type != null ? type : typeof(T)] = set; set.Rebind(this);
        }

        // ---------------------------------------------------------- 存档口（框架自己的持久化）
        /// <summary>把所有条目的 `id → 数量` 导成一份快照（给 <see cref="InventoryStore"/> 用）。</summary>
        public Dictionary<string, int> SnapshotAmounts()
        {
            var d = new Dictionary<string, int>();
            foreach (var kv in _byId)
                if (kv.Value != null) d[kv.Key] = kv.Value.Amount;
            return d;
        }

        /// <summary>把一份快照**灌进**本背包（已在的条目改数量、不在的**跳过** —— 我们不知道它的 T 是什么；
        /// 缺的物品由调用方的「种子」那一步补）。会照常抛变更事件（原版加载后也是走同一套通知）。</summary>
        public void RestoreAmounts(IReadOnlyDictionary<string, int> amounts)
        {
            if (amounts == null) return;
            foreach (var kv in amounts)
            {
                ItemInfo<T> info;
                if (!_byId.TryGetValue(kv.Key, out info) || info == null) continue;
                if (info.Amount == kv.Value) continue;
                info.SetAmount(kv.Value);
                NotifySubsets(info);
                if (OnItemCountChange != null) OnItemCountChange(info.Item, 0);
            }
        }

        /// <summary>自检用：清空（**不落盘**；要连盘一起清用 <see cref="InventoryStore.Delete"/>）。</summary>
        public void ClearForTest()
        {
            _byId.Clear();
            for (int i = 0; i < _subsets.Count; i++) _subsets[i].ClearInternal();
        }
    }

    // ============================================================ 子集
    /// <summary>背包的一个**视图**（原版 `InventorySubset`）：只装某个 id 集合的那几条 + 自身的更新事件。
    /// 用于「某一类物品单独一栏」的场景（原版用它给界面做局部刷新）。</summary>
    public sealed partial class InventorySubset<T> where T : class
    {
        readonly Dictionary<string, ItemInfo<T>> _sub = new Dictionary<string, ItemInfo<T>>();
        Inventory<T> _src;
        Func<T, string> _idOf;

        /// <summary>本视图变了（原版 `m_OnUpdate`）。</summary>
        public event Action OnUpdate;

        internal void Rebind(Inventory<T> src)
        {
            _src = src;
            // 把源背包里已有的条目按「当前视图已登记的 id」补进来（原版 RegisterSubset 的语义：
            // 先登记 id 集合、再从这里取到条目）
            foreach (var kv in _sub)
            {
                ItemInfo<T> info;
                if (src != null && src.TryGetItem(kv.Key, out info)) _sub[kv.Key] = info;
            }
            if (OnUpdate != null) OnUpdate();
        }

        /// <summary>原版 `InventorySubset.Get(string id)`（查不到给 null）。</summary>
        public ItemInfo<T> Get(string id)
        {
            ItemInfo<T> info;
            return !string.IsNullOrEmpty(id) && _sub.TryGetValue(id, out info) ? info : null;
        }

        /// <summary>原版 `InventorySubset.TryGetValue(string id, out IItemInfo)`。</summary>
        public bool TryGetValue(string id, out ItemInfo<T> item)
        {
            item = null;
            return !string.IsNullOrEmpty(id) && _sub.TryGetValue(id, out item) && item != null;
        }

        /// <summary>原版 `InventorySubset.Add(IItemInfo)`：登记一条，并触发一次更新。</summary>
        public void Add(ItemInfo<T> value)
        {
            if (value == null || value.Item == null || _idOf == null) return;
            string id = _idOf(value.Item);
            if (string.IsNullOrEmpty(id)) return;
            _sub[id] = value;
            if (OnUpdate != null) OnUpdate();
        }

        /// <summary>原版 `InventorySubset.AddRange(IEnumerable&lt;IItemInfo&gt;)`。</summary>
        public void AddRange(IEnumerable<ItemInfo<T>> items)
        {
            if (items == null) return;
            foreach (var it in items) Add(it);
        }

        internal void OnSourceChanged(ItemInfo<T> info)
        {
            if (info == null || info.Item == null || _idOf == null) return;
            string id = _idOf(info.Item);
            if (string.IsNullOrEmpty(id) || !_sub.ContainsKey(id)) return;
            _sub[id] = info;
            if (OnUpdate != null) OnUpdate();
        }

        internal void ClearInternal() { _sub.Clear(); if (OnUpdate != null) OnUpdate(); }

        /// <summary>登记取 id 的口（与 <see cref="Inventory{T}"/> 同一把尺 —— ⛔ 别在子集里另写一份）。</summary>
        public void BindIdOf(Func<T, string> idOf) { _idOf = idOf; }
    }

    // ============================================================ 存档（框架自己的持久化）
    /// <summary>背包的**本地存档**（原版没有这一层：原版存档在 `PlayerDataManager` 的每活动 `TSave` 里）。
    ///
    /// <para>🔴 **如实标**：这是「`PlayerDataManager` / `LiveOpsEvent<TSave>` 落地**之前**的临时本地等价物」
    /// —— 与 `DailyData` / `DraftRunSave` / `CardProgress` 那几份**同一套路**（`persistentDataPath` 下一个
    /// 自己的目录 + 一个 DTO + `JsonUtility`），所以它们将来会**一起**并进 `PlayerDataManager`。
    /// ⛔ 别把它当成原版做法。</para>
    ///
    /// <para>**迁移路径**（框架落地时）：把 `WarpforgeInventory/*.json` 的 `rows` 逐条并进
    /// `PlayerDataManager` 对应活动的 `TSave` → 删掉本类的写盘、只留 `Load` 读一次旧档 → 最后删目录。</para></summary>
    public static class InventoryStore
    {
        public const string StoreDirName = "WarpforgeInventory";
        /// <summary>自检用的目录改写口（⛔ 自检一律走临时目录，别动玩家真存档）。</summary>
        public static string OverrideDir;
        /// <summary>最近一次落盘的文件（`null` = 本进程没落过）。</summary>
        public static string LastStorePath { get; private set; }
        /// <summary>最近一次落盘失败的原因（成功时 `null`）。</summary>
        public static string LastStoreError { get; private set; }

        static string Dir
        {
            get
            {
                return string.IsNullOrEmpty(OverrideDir)
                     ? System.IO.Path.Combine(UnityEngine.Application.persistentDataPath, StoreDirName)
                     : OverrideDir;
            }
        }

        /// <summary>某个背包的存档路径（`<name>.json`）。</summary>
        public static string StorePath(string name)
        {
            return System.IO.Path.Combine(Dir, name + ".json");
        }

        /// <summary>门：允许碰盘吗。① `OverrideDir` 设了 ⇒ 允许（自检临时档）；② 否则只有真在跑才允许
        /// ⇒ **批处理自检一律不碰玩家真存档**（同 `DailyData.StoreOn`）。</summary>
        public static bool StoreOn
        {
            get { return !string.IsNullOrEmpty(OverrideDir) || (UnityEngine.Application.isPlaying && !UnityEngine.Application.isBatchMode); }
        }

        [Serializable] public class RowDto { public string k; public int amount; public int extra; public bool hasExtra; }
        [Serializable] public class StoreDto { public int version = 1; public RowDto[] rows; }

        /// <summary>落盘。`extras`（可选）= 调用方自己的附加整数（例：商店的**限购剩余**）——
        /// 原版那一位在 store 的 `TSave` 里，我们暂时寄存在同一份文件里（⛔ 不是 `ItemInfo` 的字段）。</summary>
        public static bool Save<T>(string name, Inventory<T> inv, IReadOnlyDictionary<string, int> extras = null) where T : class
        {
            LastStoreError = null;
            if (inv == null || string.IsNullOrEmpty(name)) return false;
            if (!StoreOn) return false;
            try
            {
                var am = inv.SnapshotAmounts();
                var rows = new List<RowDto>();
                foreach (var kv in am)
                {
                    int ex = 0;      // ⚠️ 必须先赋 0（否则 `hasEx ? ex : 0` 那一支读未赋值局部量）
                    bool hasEx = extras != null && extras.TryGetValue(kv.Key, out ex);
                    rows.Add(new RowDto { k = kv.Key, amount = kv.Value, extra = hasEx ? ex : 0, hasExtra = hasEx });
                }
                System.IO.Directory.CreateDirectory(Dir);
                string p = StorePath(name);
                string json = UnityEngine.JsonUtility.ToJson(new StoreDto { rows = rows.ToArray() });
                // 🆕 2026-10-25（执行代理 R4 · 存档 Step 4「双写期」）：**不再自己写盘** —— 走
                // `SaveCenter.SaveThroughLegacy`（中央档 + 旧档各写一份，旧档**不带 `_v`**）。
                // · 落盘关着（批处理 / 自检没注入任何目录）⇒ 上面 `if (!StoreOn) return false;` 已经挡住了；
                // · **只注入了 `OverrideDir`、没注入 `SaveCenter.OverrideRoot`** ⇒ 切换口退回「只写旧档」，
                //   与切换前**逐字节相同**（那批既有自检照旧能验落盘）。
                // 判据 = `资料/完全复刻/判据/存档体系_原版语义与施工方案.md` §七 Step 4
                //   （「切前 dump 与切后中央档逐字段相等」由 `SaveCenter.DiffPayload` 验）。
                string werr;
                if (!SaveCenter.SaveThroughLegacy(SaveKeys.OursInventoryPrefix + name, p, json, out werr))
                {
                    LastStoreError = werr;
                    UnityEngine.Debug.LogWarning("[Inventory] 背包 `" + name + "` **没能落盘**（" + p + "）：" + werr);
                    return false;
                }
                LastStorePath = p;
                return true;
            }
            catch (Exception e)
            {
                LastStoreError = e.Message;
                UnityEngine.Debug.LogWarning("[Inventory] 背包 `" + name + "` **没能落盘**（" + StorePath(name) + "）：" + e.Message);
                return false;
            }
        }

        /// <summary>读盘。返回盘上那份的 `extras`（没有就给空表）；条目数量由
        /// <see cref="Inventory{T}.RestoreAmounts"/> 灌回去（**已在的改、不在的跳过**）。</summary>
        public static Dictionary<string, int> Load<T>(string name, Inventory<T> inv) where T : class
        {
            var extras = new Dictionary<string, int>();
            if (inv == null || string.IsNullOrEmpty(name) || !StoreOn) return extras;
            try
            {
                string p = StorePath(name);
                // 🆕 Step 4 双写期过渡读法：**先中央档、再旧档**（`ReadThroughLegacy` 内部判落盘门 ⇒
                // 批处理/自检照旧只读旧档那一份，不会读到玩家真中央档）。⛔ 别删旧读。
                string text = SaveCenter.ReadThroughLegacy(SaveKeys.OursInventoryPrefix + name, p);
                if (text == null) return extras;
                var dto = UnityEngine.JsonUtility.FromJson<StoreDto>(text);
                if (dto == null) { UnityEngine.Debug.LogWarning("[Inventory] `" + p + "` 解不出来 ⇒ 按出厂值走"); return extras; }
                if (dto.version != new StoreDto().version)
                {
                    UnityEngine.Debug.LogWarning("[Inventory] 背包存档版本不认识（盘上 " + dto.version + "）⇒ **不读它**（" + p + "）");
                    return extras;
                }
                var am = new Dictionary<string, int>();
                if (dto.rows != null)
                    foreach (var r in dto.rows)
                    {
                        if (r == null || string.IsNullOrEmpty(r.k)) continue;
                        am[r.k] = r.amount;
                        if (r.hasExtra) extras[r.k] = r.extra;
                    }
                inv.RestoreAmounts(am);
                return extras;
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogWarning("[Inventory] 读背包存档失败（" + StorePath(name) + "）：" + e.Message + " ⇒ 按出厂值走");
                return extras;
            }
        }

        /// <summary>删掉某个背包的存档（自检必须调，否则会读到上一趟的残留）。
        /// <para>🆕 2026-10-25（Step 4 双写期）：**中央档那一份也要一起清** —— 否则「删了旧档再读」会
        /// 从中央档把旧值读回来（`ReadThroughLegacy` 是先中央后旧档）。中央档走
        /// `SaveCenter.Delete`（**不真删**：改名到 `.del-*`，可回滚）。</para></summary>
        public static void Delete(string name)
        {
            try
            {
                if (!string.IsNullOrEmpty(name) && StoreOn && System.IO.File.Exists(StorePath(name)))
                    System.IO.File.Delete(StorePath(name));
                string key = string.IsNullOrEmpty(name) ? null : SaveKeys.OursInventoryPrefix + name;
                if (key != null && SaveCenter.StoreOn && SaveCenter.Exists(key)) SaveCenter.Delete(key);
            }
            catch (Exception e) { UnityEngine.Debug.LogWarning("[Inventory] 删背包存档失败：" + e.Message); }
            LastStorePath = null;
        }
    }

    // ============================================================ 进程级注册
    /// <summary>进程级的背包注册表（原版 `InventoryManager.GetInventory&lt;T&gt;()` 的等价物）。
    /// ⚠️ 原版是「**按类型**取一份背包」；我们这里**按名字**取（同一个 T 可能有多份，例如商店/收藏各一份）——
    /// 如实标：这是**比原版多一档**的用法，但没有它就得让每个子系统各自 `new` 一份、谁也看不见谁。</summary>
    public static class Inventories
    {
        /// <summary>商店的背包（拥有数 + 限购；见 `Shell/ShopData.cs`）。</summary>
        public static readonly Inventory<ShopItem> Shop =
            new Inventory<ShopItem>(it => it != null ? it.Id : null);

        /// <summary>**卡牌的背包**（原版 `CardInventory : Inventory&lt;RawCardScript&gt;`；见 `判据/泛型与启动链.md` §二）。
        /// <para>⚠️ **今天还没接消费点**：收藏页那一格「xN」现在读的是 `CardProgress.Owned(id, rarity)`
        /// （那一份在 `Shell/CardDetailPopup.cs` 里，**不在本代理白名单**）⇒ 迁过来要改那个文件。
        /// 本背包先建好、并按「全解锁」口径**播种**（见 <see cref="SeedCardsAllUnlocked"/>），
        /// 等那份被划进来时**只需把 `CardsCounterText` 的读口换成 `Inventories.Cards.Get(def)`**（一行）。</para>
        /// <para>🆕 **2026-10-26（批次 3 · Q1）**：类型从 `Inventory&lt;CardDef&gt;` 换成它的**原版子类**
        /// <see cref="CardInventory"/>（源码兼容 —— 所有调用点仍当 `Inventory&lt;CardDef&gt;` 用）。
        /// 换来的是「`UpdateItemCollection` 发一次收藏更新」「`customData.CardTier` ⇒ 登记档位」两跳。</para></summary>
        public static readonly CardInventory Cards = new CardInventory();

        /// <summary>**异画的背包**（原版 `AlternateArtInventory`；条目 = <see cref="AltArtUnlock"/>）。
        /// <para>⚠️ 今天它**是空的**：异画在收藏窗那边是 `CollectionWindow.AltArtCards` 的静态全解锁表，
        /// 还没有谁往这里播种（要不要播见 `SeedAltArtsAllUnlocked` 的注释）。</para></summary>
        public static readonly AlternateArtInventory AltArts = new AlternateArtInventory();

        /// <summary>**万能卡的背包**（原版 `WildcardInventory`；条目 = <see cref="InventoryWildcard"/>）。
        /// ⚠️ 数量正本仍是 `CardProgress`（按稀有度分桶）；本背包今天的唯一作用是「重建后发一次刷新信号」。</summary>
        public static readonly WildcardInventory Wildcards = new WildcardInventory();

        /// <summary>把上面四份背包登记进门面（原版 `InventoryManager` 的 `inventories`）。
        /// ⚠️ 静态构造：字段初始化之后跑一次；`ClearForTest()` **不**注销它们（自检要能反复用同一批实例）。</summary>
        static Inventories()
        {
            InventoryManager.Register(Cards);
            InventoryManager.Register(Shop);
            InventoryManager.Register(AltArts);
            InventoryManager.Register(Wildcards);
        }

        /// <summary>按「全解锁」口径给 <see cref="Cards"/> 播种：每张卡的数量 = **能带几张**
        /// （`RuleEngine.DeckRules.CopyLimit(rarity, type)` —— 与收藏格那行「xN」的**上限**同一个口）。
        /// ⚠️ 幂等：已经有条目的**不动**（不会把玩家数据盖回出厂值）。</summary>
        /// <param name="amount">每张卡播种多少。**默认 = 能带几张**（`DeckRules.CopyLimit`）；
        /// 🆕 2026-10-25：调用方可以传别的口径 —— 收藏线现在传 **`CardProgress.Owned`**（= 「给足」口径，
        /// 上限 + 升满所需），为的是让「背包里的拥有数」与旧的 `CardProgress` **逐值相等**（迁移第一步：
        /// 两套并存时先让读口一致，见 `资料/完全复刻/报告/C_CardProgress归并_第1轮.md`）。</param>
        public static void SeedCardsAllUnlocked(System.Collections.Generic.IEnumerable<RuleEngine.CardDef> pool,
                                                System.Func<RuleEngine.CardDef, int> amount = null)
        {
            if (pool == null) return;
            foreach (var c in pool)
            {
                if (c == null || string.IsNullOrEmpty(c.Id)) continue;
                if (Cards.Get(c) != null) continue;
                int n = amount != null ? amount(c) : RuleEngine.DeckRules.CopyLimit(c.Rarity, c.Type);
                Cards.Add(c, n < 0 ? 0 : n);
            }
            // 🆕 2026-10-26（批次 3 · Q1）：**异画也是「全解锁」** ⇒ 用同一个口径顺手播一次（幂等）。
            //    两件事仍各有各的方法（`SeedAltArtsAllUnlocked`），这里只是把它接上 —— 否则
            //    `AlternateArtInventory.OnFinishUnpack` 那两跳永远没有条目可跑（空背包 = 假实现）。
            SeedAltArtsAllUnlocked();
        }

        /// <summary>按「全解锁」口径给 <see cref="AltArts"/> 播种（7 条，来自 `CollectionWindow.AltArtCards`）。
        /// <para>判据：原版异画条目的来源是服务器背包（`AlternateArtInventory.OnItemCountModify`）；
        /// 本项目「卡池/卡牌/头像全解锁」⇒ 等价于「7 条各 +1」。**幂等**。</para>
        /// <para>⚠️ **不由 `SeedCardsAllUnlocked` 自动调**（两件事各管各的；谁要异画背包谁显式调）。
        /// `ClonedCardId` 留 null（本地查不到真值，见 <see cref="AltArtUnlock.ClonedCardId"/>）。</para></summary>
        public static void SeedAltArtsAllUnlocked()
        {
            var table = CollectionWindow.AltArtCards;
            if (table == null) return;
            for (int i = 0; i < table.Length; i++)
            {
                string id = table[i].CardId;
                if (string.IsNullOrEmpty(id) || AltArts.Get(id) != null) continue;
                AltArts.Add(new AltArtUnlock { CardId = id, Style = table[i].Style }, 1);
            }
        }

        /// <summary>自检用：把注册过的背包清空（**不落盘**）。</summary>
        public static void ClearForTest()
        {
            Shop.ClearForTest();
            Cards.ClearForTest();
            AltArts.ClearForTest();
            Wildcards.ClearForTest();
        }
    }

    /// <summary>商店物品的身份（原版是 `ShopElement`/SO）。**只用来当背包的 key**：
    /// `Id` = `pageIndex + "/" + 商品名`（与 `ShopData` 原来的键**逐字相同** ⇒ 旧存档能直接读）。</summary>
    public sealed class ShopItem
    {
        public string Id;
        public string Name;
        public ShopItem(string id, string name) { Id = id; Name = name; }
        public override string ToString() { return Name ?? Id; }
    }

    // ============================================================ 追加区 · 2026-10-26（批次 3 · Q1）
    //
    // 【这一段补的是什么】
    //   账 `账/收藏.tsv` 那 49 条 C 里，**库存那一族**（`InventoryManager` 24 · `CardInventory` 2 ·
    //   `AlternateArtInventory` 2 · `WildcardInventory` 1 · `WildcardDisplay` 的**信号那一半**）全部要求
    //   「原版那个类/那个方法在我们这边有等价物」。本段一次把它们落地：
    //     · `InventoryManager` 门面 —— 判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs`
    //       （**只有这一份产物有真方法体**；`out_cpp2il_ilspy` / `out_il2csharp` / `out_cpp2il_dif` 都是签名壳，
    //       ⛔ 别拿它们判「方法体是空的」）。`decomp_full/InventoryManager__*.c`（33 个）用作交叉核对。
    //     · 三份特化 `CardInventory` / `AlternateArtInventory` / `WildcardInventory` —— 判据 = 同名 cpp2il 文件。
    //     · 支撑件 `EphemeralState`（判据 `EphemeralState.cs`）· `ItemInstance`（原版是
    //       `PlayFab.ClientModels.ItemInstance`，见 `d:/2/Warpforge_code/Scripts/PlayFab/PlayFab/ClientModels/ItemInstance.cs`）
    //       · `QuantityBoundItemInfo<T>`（判据 `CardInventory.CreateAlternateArtInfo` 里 `new QuantityBoundItemInfo(...)`）
    //       · `AltArtUnlock` / `InventoryWildcard`（见各自类头）。
    //
    // 🔴 【为什么用 partial 的第二部分、而不是就近插进各自类里】
    //   `账/*.tsv` 有 6 条 A 行的落点钉在 `Shell/Inventory.cs:46/54/62/69/72/91/103/127/…/343`
    //   （`未分类_其他.tsv` · `社交_聊天.tsv` · `收藏.tsv` · `日常_奖励.tsv`）。
    //   往中间插一行就会让它们**整体漂移**，`工具/check_ledger_points.py` 会逐条报 drift（2026-10-25 N9 那次的原形）。
    //   ⇒ 本段一律**追加在文件尾**：`ItemInfo<T>` / `Inventory<T>` 各开一个 `partial` 第二部（C# 允许分散），
    //     新类型直接排在 `ShopItem` 之后。文件里唯一被改动的既有行是 8 处**一对一替换**（行数不变）：
    //     类声明去 `sealed`（要能继承 = 原版 `CardInventory : Inventory<RawCardScript>`）、
    //     `Amount` 变成可绑定的计算属性、`EphemeralState` 由 `int` 换成同名类、
    //     `OnItemCountModify` / `UpdateItemCollection` 加 `virtual`（原版被子类 override）。
    //
    // ⚠️ 【本段**如实标**的三处与原版的结构性差异】（⛔ 不是「已经一样」）
    //   ① 原版 `InventoryManager` 是 `SingletonBehaviour<T>` + `Dictionary<Type, IInventory<IIdentifiable>>`；
    //      我们**没有非泛型背包接口**（`Inventory<T>` 的 ctor 要注入 `Func<T,string> idOf` —— 见文件头「差异 ①」）
    //      ⇒ 门面收 `Type → Entry`（`Entry.Inventory` 是 `object`，泛型路径一律走 `GetInventory<T>()`）。
    //      `GetInventory(Type)` 因此返回 `object`（原版返回 `IInventory<IIdentifiable>`）。
    //   ② 原版 `CreateInventory(Type)` 用反射 `ReflectionHelper.New(Inventory<T>)` 现造；反射造不出「要注入 idOf」的
    //      背包 ⇒ 我们的等价口是 `Register<T>(inv)`（显式登记）；`CreateInventory` 退化成「查已登记的那份，没有就出声」。
    //      `SubscribeInventories` 同理：原版它做两件事（按 Type 登记 + 把 `all` 里同类型的 `InventorySubset` 绑上去），
    //      我们**只留登记那一半** —— 子集由调用方直接 `Inventory<T>.RegisterSubset`（那一条已 A）。
    //   ③ 原版 `Add(ItemInstance)` / `Remove(instanceId,…)` 靠 `AssetLocator.GetAsset<T>(id)` 把 id 解回资产；
    //      我们没有 Addressables 那层 ⇒ 用 `SetResolver<T>(Func<string,T>)` 注入「id → 资产」。没注入 resolver 的
    //      类型**不会被猜**，`DeserializeInventory` 会给该 id 打一条警告（不静默）。

    // ---------------------------------------------------------- ItemInfo<T>：可绑定数量 + customData
    /// <summary>`ItemInfo<T>` 的 **partial 第二部分**（为什么在文件尾 → 见上面「追加区」头）。
    /// 补的两件都来自原版：① `QuantityBoundItemInfo<T>` 要能「数量跟着另一条走」（`Amount` 在
    /// 第一部分已改成计算属性，读 `_quantityBoundTo`）；② `ItemInfo.customData`（原版
    /// `IItemInfo<T>.customData`，`CardInventory.UpdateCardTier` 就从它读 `CardTier`）。</summary>
    public partial class ItemInfo<T> where T : class
    {
        int _amount;
        ItemInfo<T> _quantityBoundTo;
        Dictionary<string, string> _customData;

        /// <summary>原版 `IItemInfo.customData`（服务器随物品下发的自定义字典；`CardTier` 就在里面）。
        /// ⚠️ 今天只有 `CardInventory` 读它；别拿它当第二份数量/等级正本。</summary>
        public Dictionary<string, string> CustomData { get { return _customData; } }

        /// <summary>这一条的数量**绑在别人身上**（= 原版 `QuantityBoundItemInfo`）。
        /// ⚠️ 绑上之后 `SetAmount/Add` 写的是本条的 `_amount`（**读**不到），与原版一致：这种条目的数量只由被绑方决定。</summary>
        public bool QuantityIsBound { get { return _quantityBoundTo != null; } }

        internal void BindQuantityTo(ItemInfo<T> other) { _quantityBoundTo = other; }

        internal void SetCustomData(Dictionary<string, string> customData) { _customData = customData; }
    }

    /// <summary>原版 `QuantityBoundItemInfo<T> : ItemInfo<T>` —— 一条「数量跟着另一条走」的条目。
    /// 判据：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/CardInventory.cs:158-176`
    /// （`new QuantityBoundItemInfo<RawCardScript>(item, boundItem, instanceId)`，`boundItem = Get(originalCard, autoCreate:true)`）。
    /// 用途 = **异画卡的数量跟着原卡走**（异画不是独立份数）。</summary>
    public sealed partial class QuantityBoundItemInfo<T> : ItemInfo<T> where T : class
    {
        public QuantityBoundItemInfo(T item, ItemInfo<T> boundItem, string instanceId)
            : base(item, boundItem != null ? boundItem.Amount : 0, instanceId)
        {
            BindQuantityTo(boundItem);
        }
    }

    // ---------------------------------------------------------- Inventory<T>：idOf 暴露 + customData 那两个虚口
    /// <summary>`Inventory<T>` 的 **partial 第二部分**。只补门面/子类真正要用到的四件，
    /// 已有的成员一个都不搬（行号守恒，见「追加区」头）。</summary>
    public partial class Inventory<T> where T : class
    {
        /// <summary>本背包的「取 id 口」（原版 `IIdentifiable.GetID()` 的替身）。
        /// `InventoryManager` 的路由要用它；子类判断「这条是谁」也要用。</summary>
        internal string IdOf(T item) { return item != null ? _idOf(item) : null; }

        /// <summary>原版 `Inventory.Add(T, int, string, Dictionary&lt;string,string&gt;, bool)` 的**带 customData 那一档**。
        /// ⚠️ **故意不给默认值**：给了就会和第一部分那个 4 参 `Add` 在 `Add(x, 1, "id")` 上撞成二义（CS0121）。
        /// ⇒ 3~4 个实参走老那个，5 个实参走这个。</summary>
        public virtual ItemInfo<T> Add(T item, int amount, string instanceId, Dictionary<string, string> customData, bool overrideAmount)
        {
            ItemInfo<T> info = Add(item, amount, instanceId, overrideAmount);
            if (info != null && customData != null) info.SetCustomData(customData);
            return info;
        }

        /// <summary>原版 `Inventory.Create(T, string, Dictionary&lt;string,string&gt;)` 的**带 customData 那一档**
        /// （同样**不给默认值**，理由同上）。`CardInventory` override 它来做「异画条目的数量绑定」。</summary>
        public virtual ItemInfo<T> Create(T item, string instanceId, Dictionary<string, string> customData)
        {
            ItemInfo<T> info = Create(item, instanceId);
            if (info != null && customData != null) info.SetCustomData(customData);
            return info;
        }

        /// <summary>原版 `Inventory.OnFinishUnpack()`：**整包解包完成后**的那一跳。
        /// 调用点 = <see cref="InventoryManager.DeserializeInventory"/> 的收尾循环（⚠️ 那一处推断见该方法的注释）。
        /// 默认空实现；`AlternateArtInventory` 覆盖它。</summary>
        public virtual void OnFinishUnpack() { }

        /// <summary>原版 `Inventory.OnItemCountModify(T item, int amount)` —— **`protected virtual` 的扩展点**
        /// （`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/Inventory.cs:1122`，默认空实现）。
        /// 由 <see cref="OnItemCountModifyInternal"/> 的**第一句**调用 ⇒ `Add` / 外部改数量**两条路都会触发**。
        /// <para>⚠️ 本类**另有一个同概念的 public `OnItemCountModify`**（= 外部改数量的入口，`ShopData` / `ShopScene` 在用；
        /// 原版那一档叫 `Inventory.Remove` / `Add` 而不是这个名）。为了避免同名重载打架，**钩子这一档多一个 d**：
        /// `OnItemCountModified`。⛔ 别把两者合并（合并就会变成「钩子调用自己」= 递归）。</para></summary>
        protected virtual void OnItemCountModified(T item, int amountChanged) { }

        /// <summary>把一个新条目塞进字典（原版 `Inventory<T>` 内部对 `m_Items` 的写；我们子类要用）。
        /// ⚠️ **只给「自己造条目」的子类用**（`CardInventory.CreateAlternateArtInfo` 造 `QuantityBoundItemInfo`）；
        /// 普通加量请走 `Add`（它会走变更通知）。</summary>
        internal void PutInfo(string id, ItemInfo<T> info)
        {
            if (string.IsNullOrEmpty(id) || info == null) return;
            _byId[id] = info;
            NotifySubsets(info);
        }
    }

    // ---------------------------------------------------------- 限时态（原版 EphemeralState）
    /// <summary>原版 `EphemeralState`（**限时物品的到期信息**）。
    /// 判据 = `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/EphemeralState.cs` 全文件（真方法体）＋签名桩
    /// `d:/2/Warpforge_code/Scripts/Assembly-CSharp/EphemeralState.cs`。
    /// <para>原版三字段：`LiveopsReference boundEvent` / `long ephemeralStartTime` / `long ephemeralDuration`
    /// （都是**毫秒**，字段上标着 `[DrawMillis]`）。我们没有 LiveOps 事件层 ⇒ `boundEvent` 落成 **事件 id 字符串**。</para>
    /// <para>⚠️ **与 `Shell/CampaignData.cs` 的 `RewardSpec.IsEphemeral/EphemeralMs` 是同一个东西的两种形状**：
    /// 那一份是「SO 侧扁平化」的（只留了「是否限时 + 总时长」两个量），本类是完整同名类。
    /// 两者**今天都保留**（改 `CampaignData` 不在本轮白名单）；将来收口只能留一处 —— 见报告「顺手发现」。</para></summary>
    [Serializable]
    public class EphemeralState
    {
        /// <summary>原版 `ephemeralStartTime`（毫秒；0 = 没设）。</summary>
        public long StartTimeMs;
        /// <summary>原版 `ephemeralDuration`（毫秒；0 = 没设）。</summary>
        public long DurationMs;
        /// <summary>原版 `boundEvent`（`LiveopsReference`）—— 我们只留事件 id（空 = 没绑事件）。</summary>
        public string BoundEventId;

        public EphemeralState() { }
        /// <summary>原版 `EphemeralState(long ephemeralDuration)`。</summary>
        public EphemeralState(long durationMs) { DurationMs = durationMs; }
        /// <summary>原版 `EphemeralState(ILiveOps boundEvent)` → `new LiveopsReference(boundEvent.GetID())`。</summary>
        public EphemeralState(string boundEventId) { BoundEventId = boundEventId; }

        static DateTime Millis(long ms)
        {
            // 原版 `TimeHelper.MillisToDateTime`（Unix 毫秒 → DateTime）。我们没那一件 ⇒ 就地等价实现。
            return new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms);
        }

        /// <summary>原版 `EphemeralStartTime` = `TimeHelper.MillisToDateTime(ephemeralStartTime)`。</summary>
        public DateTime EphemeralStartTime { get { return Millis(StartTimeMs); } }

        /// <summary>原版 `EphemeralDuration`。</summary>
        public long EphemeralDuration { get { return DurationMs; } }

        /// <summary>原版 `HasExpiration`：`(boundEvent != null &amp;&amp; 它的 EventReference 解得出来)
        /// || (ephemeralStartTime &gt; 0 &amp;&amp; ephemeralDuration &gt; 0)`。
        /// 我们没有 EventReference 那层 ⇒ 第一支用「`BoundEventId` 非空」代（**如实标**）。</summary>
        public bool HasExpiration
        {
            get { return !string.IsNullOrEmpty(BoundEventId) || (StartTimeMs > 0 && DurationMs > 0); }
        }

        /// <summary>原版 `EphemeralEndTime`：① `start &gt; 0 &amp;&amp; duration &gt; 0` ⇒ `start + duration` 毫秒；
        /// ② 否则若绑了事件 ⇒ **事件自己的结束时刻**；③ 都没有 ⇒ `DateTime.MaxValue`。
        /// ⚠️ **② 我们这一侧实现不了**（要 LiveOps 事件表）⇒ 落到 ③ `DateTime.MaxValue`，
        /// 与「没有到期信息」同档。⛔ 别把它当「永不过期」以外的东西用。</summary>
        public DateTime EphemeralEndTime
        {
            get
            {
                if (StartTimeMs > 0 && DurationMs > 0) return Millis(StartTimeMs).AddMilliseconds(DurationMs);
                return DateTime.MaxValue;
            }
        }

        /// <summary>原版 `AddDuration(long)`。</summary>
        public void AddDuration(long duration) { DurationMs = DurationMs + duration; }

        /// <summary>原版 `FormatTime()`：取 `End − Start`（没 start 时取 `End − ServerNow`）→
        /// `SupportMethods.GetTimeString(ts, eventTimer:true)` → `Replace("0m","").Replace("0h","")`。
        /// ⚠️ **`SupportMethods.GetTimeString` 我们没有** ⇒ 这里自己出 `<d>d <h>h <m>m` 再套**同样那两次 Replace**
        /// （如实标：日那一段原版由 GetTimeString 决定要不要出现，我们固定带）。</summary>
        public string FormatTime()
        {
            TimeSpan ts;
            if (StartTimeMs > 0) ts = EphemeralEndTime - EphemeralStartTime;
            else if (!string.IsNullOrEmpty(BoundEventId)) ts = EphemeralEndTime - DateTime.UtcNow;
            else ts = TimeSpan.Zero;
            if (ts < TimeSpan.Zero) ts = TimeSpan.Zero;
            string s = ((long)ts.TotalDays) + "d " + ts.Hours + "h " + ts.Minutes + "m";
            return s.Replace("0m", "").Replace("0h", "").Trim();
        }
    }

    // ---------------------------------------------------------- 服务器下发的物品实例（原版 PlayFab ItemInstance）
    /// <summary>服务器背包的**一条原始记录**。原版类型 = `PlayFab.ClientModels.ItemInstance`
    /// （15 个字段，见 `d:/2/Warpforge_code/Scripts/PlayFab/PlayFab/ClientModels/ItemInstance.cs`）。
    /// 我们只带**库存路径真正消费**的五个 —— 判据是逐方法读出来的，不是挑的：
    /// `InventoryManager.Add(ItemInstance)` 读 `ItemId` / `DisplayName` / `RemainingUses`（数量在 +120，就是它）；
    /// `HasItemByID`/`GetOwnedCount` 的「未解包」那一支读 `ItemId` + `RemainingUses`；
    /// `Add<T>` 再把 `instanceId` / `customData` 交给背包。
    /// ⚠️ 其余 10 个（`Annotation` / `BundleContents` / `CatalogVersion` / `Expiration` / `ItemClass` /
    /// `PurchaseDate` / `UnitCurrency` / `UnitPrice` / `UsesIncrementedBy` / `BundleParent`）本地库存路径**零读取点**
    /// ⇒ 不建空字段（本项目口径：别先建空壳）。</summary>
    [Serializable]
    public class ItemInstance
    {
        public string ItemId;
        public string ItemInstanceId;
        public string DisplayName;
        public Dictionary<string, string> CustomData;
        /// <summary>原版这一格是**数量**（`int?`；`null` 当 0）。</summary>
        public int? RemainingUses;
    }

    // ---------------------------------------------------------- 门面（原版 InventoryManager）
    /// <summary>原版 `InventoryManager`（`SingletonBehaviour&lt;InventoryManager&gt;`）的**静态等价物**。
    /// <para>判据：`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs`（真体）＋
    /// `d:/2/tools/decomp_full/InventoryManager__*.c`（33 个方法，逐条交叉核对）。</para>
    /// <para>🔴 三处结构性差异（`CreateInventory` 反射 / `IInventory` 非泛型接口 / `AssetLocator`）见上面「追加区」头，
    /// ⛔ 别把它们当「已一样」。</para></summary>
    public static class InventoryManager
    {
        /// <summary>一个类型在门面上的全部入口。泛型方法**不**走它（走 `GetInventory&lt;T&gt;()`），
        /// 只有「手里只有一个 id / 不知道类型」的那几条（解包 / 按 instanceId 删 / 收尾）要用。</summary>
        sealed class Entry
        {
            public Type ItemType;
            public object Inventory;
            public Func<string, object> ResolveId;                       // id → T（null = 没注入 resolver）
            public Func<object, int, string, Dictionary<string, string>, bool, object> AddRaw;
            public Func<string, int, bool> RemoveById;
            public Action FinishUnpack;
        }

        sealed class PendingExpiry
        {
            public DateTime At;
            public string ItemId;
            public string ActionId;
            public Action Remove;
        }

        static readonly Dictionary<Type, Entry> _inventories = new Dictionary<Type, Entry>();
        static readonly Dictionary<Type, Func<string, object>> _resolvers = new Dictionary<Type, Func<string, object>>();
        /// <summary>原版 `inventoryInstanceIds`（`instanceId → itemId`）—— `Add` 写、`Remove(string …)` 读。</summary>
        static readonly Dictionary<string, string> _instanceIds = new Dictionary<string, string>();
        static readonly List<PendingExpiry> _expiries = new List<PendingExpiry>();
        static readonly HashSet<string> _noted = new HashSet<string>();
        static IReadOnlyList<ItemInstance> _raw;
        static Dictionary<string, int> _virtualCurrencies;
        static bool _finishedUnpack;

        /// <summary>原版 `RawInventory`（解包前那一份原始列表）。</summary>
        public static IReadOnlyList<ItemInstance> RawInventory { get { return _raw; } }
        /// <summary>原版 `finishedUnpack`：**这一位决定「数量从哪读」**
        /// （`false` ⇒ 读原始列表；`true` ⇒ 读类型化背包）。⛔ 别在别处再存第二份。</summary>
        public static bool FinishedUnpack { get { return _finishedUnpack; } }
        /// <summary>原版 `inventoryInstanceIds` 的只读视图（诊断用）。</summary>
        public static IReadOnlyDictionary<string, string> InstanceIds { get { return _instanceIds; } }
        /// <summary>还没到期的限时物品条数（诊断用；见 <see cref="PumpExpiries"/>）。</summary>
        public static int PendingExpiryCount { get { return _expiries.Count; } }
        /// <summary>`DeserializeInventory` 收到的虚拟货币（原版逐条 `AssetLocator.GetAsset&lt;ICurrency&gt;` 再进背包；
        /// 我们没有货币背包 ⇒ 原样留一份快照，可读不可写）。</summary>
        public static IReadOnlyDictionary<string, int> VirtualCurrencies { get { return _virtualCurrencies; } }

        /// <summary>同一件事**只出声一次**（本项目口径：不许静默失败，但也别刷屏）。
        /// key 不变就不再报。进程内静态（批处理之间会重置）。</summary>
        internal static void Note(string key, string msg)
        {
            if (key == null) key = "";
            if (!_noted.Add(key)) return;
            UnityEngine.Debug.LogWarning("[InventoryManager] " + msg);
        }

        /// <summary>自检用：清掉「已报过」的记忆（否则第二趟自检看不到警告）。</summary>
        public static void ResetNotesForTest() { _noted.Clear(); }

        /// <summary>原版 `CreateInventory(Type)` 的**等价口**（差异见「追加区」头 ②）：把一件已经建好的背包
        /// 登记到它的物品类型上。`Type` 取的是**物品类型**（`Inventory<CardDef>` 登记在 `CardDef` 上），与原文一致。
        /// ⚠️ 重复登记 = 后者覆盖前者（原版 `inventories[type] = …` 也是这么写的）。</summary>
        public static void Register<T>(Inventory<T> inv) where T : class
        {
            if (inv == null) return;
            var e = new Entry();
            e.ItemType = typeof(T);
            e.Inventory = inv;
            e.AddRaw = (o, amount, instanceId, customData, overrideAmount) =>
                inv.Add((T)o, amount, instanceId, customData, overrideAmount);
            e.RemoveById = (id, amount) =>
            {
                ItemInfo<T> info = inv.Get(id);
                if (info == null) return false;
                inv.OnItemCountModify(info.Item, -amount);
                return true;
            };
            e.FinishUnpack = () => inv.OnFinishUnpack();
            Func<string, object> resolver;
            if (_resolvers.TryGetValue(typeof(T), out resolver)) e.ResolveId = resolver;
            _inventories[typeof(T)] = e;
        }

        /// <summary>原版 `AssetLocator.GetAsset&lt;T&gt;(id)` 的替身（差异见「追加区」头 ③）：
        /// 注入「id → 物品本体」。**没注入 = 不知道**（`DeserializeInventory` 会警告，不猜）。</summary>
        public static void SetResolver<T>(Func<string, T> byId) where T : class
        {
            if (byId == null) return;
            Func<string, object> boxed = id => (object)byId(id);
            _resolvers[typeof(T)] = boxed;
            Entry e;
            if (_inventories.TryGetValue(typeof(T), out e) && e != null) e.ResolveId = boxed;
        }

        /// <summary>按 id 解出物品本体（原版 `AssetLocator.GetAsset&lt;T&gt;`）。没注入 resolver ⇒ `null` + 出声。</summary>
        public static T Resolve<T>(string id) where T : class
        {
            Func<string, object> f;
            if (!_resolvers.TryGetValue(typeof(T), out f) || f == null)
            {
                Note("no-resolver:" + typeof(T).Name,
                     "类型 `" + typeof(T).Name + "` 没有 id→资产 resolver（`SetResolver`）⇒ 按 id 取不到物品");
                return null;
            }
            return f(id) as T;
        }

        /// <summary>原版 `GetInventory(Type)`。⚠️ 我们返回 `object`（差异见「追加区」头 ①）；
        /// 查不到给 `null` + 出声（原版是 `Dictionary` 索引器 = **抛 KeyNotFound**，我们不出声崩）。</summary>
        public static object GetInventory(Type type)
        {
            if (type == null) return null;
            Entry e;
            if (_inventories.TryGetValue(type, out e) && e != null) return e.Inventory;
            Note("no-inventory:" + type.Name,
                 "类型 `" + type.Name + "` 没有登记背包（`InventoryManager.Register`）⇒ 视为没有这件背包");
            return null;
        }

        /// <summary>原版 `GetInventory&lt;T&gt;()`（`inventories[typeof(T)] as Inventory&lt;T&gt;`）。</summary>
        public static Inventory<T> GetInventory<T>() where T : class
        {
            return GetInventory(typeof(T)) as Inventory<T>;
        }

        /// <summary>原版 `SubscribeInventories(Type, IInventory&lt;IIdentifiable&gt;)`。
        /// ⚠️ 只留「按 Type 登记」这一半（差异见「追加区」头 ②）。若同一件背包已按自己的物品类型登记过，
        /// 这里把它**再挂一个别名键**（原版就是靠这一跳让「基类类型」也能取到同一份背包）。</summary>
        public static void SubscribeInventories(Type inventoryType, object inventory)
        {
            if (inventoryType == null || inventory == null) return;
            Entry e;
            if (_inventories.TryGetValue(inventoryType, out e) && e != null && object.ReferenceEquals(e.Inventory, inventory)) return;
            foreach (var kv in _inventories)
                if (kv.Value != null && object.ReferenceEquals(kv.Value.Inventory, inventory)) { _inventories[inventoryType] = kv.Value; return; }
            _inventories[inventoryType] = new Entry { ItemType = inventoryType, Inventory = inventory };
        }

        /// <summary>原版 private `CreateInventory(Type)`：**查已登记的那份**。
        /// 原版这里用反射现造（`ReflectionHelper.New`）；我们造不出（ctor 要注入 idOf）⇒ 退化成查表 + 出声。
        /// 需要新背包请用 <see cref="Register{T}"/>（差异见「追加区」头 ②）。</summary>
        public static object CreateInventory(Type itemType)
        {
            object inv = GetInventory(itemType);
            if (inv == null)
                Note("create-inventory:" + (itemType != null ? itemType.Name : "?"),
                     "`CreateInventory` 造不出 `Inventory<" + (itemType != null ? itemType.Name : "?")
                     + ">`（我们靠 `Register<T>` 显式登记，没有反射那一跳）");
            return inv;
        }

        /// <summary>原版 `SetRawInventory(rawInventory)`：只设原始列表（不清类型化背包）。</summary>
        public static void SetRawInventory(IReadOnlyList<ItemInstance> items) { _raw = items; }

        /// <summary>原版 `DeserializeInventory(items, virtualCurrencies)` —— **服务器背包灌进来的唯一入口**。
        /// <para>逐步对位（cpp2il 体可读的那几段）：① `SetRawInventory(items)`；②（原版此处给所有
        /// `IIdentifiable` 派生类型 `CreateInventory` + `SubscribeInventories` —— 我们改用已 `Register` 的那几份）；
        /// ③ 逐条 `Add(ItemInstance)`；④ 收 `virtualCurrencies`；⑤ 收尾对每个背包做一次回调，然后 `finishedUnpack = true`。</para>
        /// <para>⚠️ **⑤ 那个回调是推断**：cpp2il 在那一行只留了 `Indirect call: [v1320 @ rdx_v35]`（反编译器的间接调用，
        /// 目标读不出）。按同名同用途落成 `Inventory&lt;T&gt;.OnFinishUnpack()`（`AlternateArtInventory` 正是 override 它做收尾）
        /// ⇒ **如实标为推断**，不是反编译直读。</para>
        /// <para>⚠️ `virtualCurrencies` 原版逐条 `AssetLocator.GetAsset&lt;ICurrency&gt;` 再进货币背包；我们没有货币背包
        /// ⇒ 只留快照（<see cref="VirtualCurrencies"/>），**不假装加到了哪里**。</para></summary>
        public static void DeserializeInventory(IReadOnlyList<ItemInstance> items, Dictionary<string, int> virtualCurrencies)
        {
            SetRawInventory(items);
            _virtualCurrencies = virtualCurrencies;
            if (items != null)
                for (int i = 0; i < items.Count; i++) AddItemInstance(items[i]);
            foreach (var kv in _inventories)
                if (kv.Value != null && kv.Value.FinishUnpack != null) kv.Value.FinishUnpack();
            _finishedUnpack = true;
        }

        /// <summary>原版 private `Add(ItemInstance)`：按 `ItemId` 解出资产再交给**该类型的背包**。
        /// 解不出 ⇒ 原版 `CustomDebug.LogError("error adding item to inventory, couldn't find item …")`，我们同样出声。</summary>
        static void AddItemInstance(ItemInstance x)
        {
            if (x == null) return;
            if (string.IsNullOrEmpty(x.ItemId))
            {
                Note("add-empty-id", "`ItemInstance.ItemId` 为空 ⇒ 不入背包（原版 `AssetLocator` 也解不出）");
                return;
            }
            int amount = x.RemainingUses.HasValue ? x.RemainingUses.Value : 0;
            foreach (var kv in _inventories)
            {
                Entry e = kv.Value;
                if (e == null || e.ResolveId == null || e.AddRaw == null) continue;
                object item = e.ResolveId(x.ItemId);
                if (item == null) continue;
                e.AddRaw(item, amount, x.ItemInstanceId, x.CustomData, false);
                if (!string.IsNullOrEmpty(x.ItemInstanceId)) _instanceIds[x.ItemInstanceId] = x.ItemId;
                return;
            }
            Note("add-unresolved:" + x.ItemId,
                 "往背包里加物品失败：解不出 id `" + x.ItemId + "`（" + (x.DisplayName ?? "?")
                 + "）—— 需要先给对应类型 `SetResolver`");
        }

        /// <summary>原版 `HasItemByID&lt;T&gt;(string id)`：**解包前**扫原始列表（`ItemId` 命中且数量 ≠ 0）；
        /// **解包后**查类型化背包。⚠️ 「解包前」那一支原版**不看 T**（扫的是同一份原始列表），我们照样。</summary>
        public static bool HasItemByID<T>(string id) where T : class
        {
            if (string.IsNullOrWhiteSpace(id)) return false;
            if (!_finishedUnpack)
            {
                if (_raw != null)
                    for (int i = 0; i < _raw.Count; i++)
                    {
                        ItemInstance x = _raw[i];
                        if (x == null || x.ItemId != id) continue;
                        int n = x.RemainingUses.HasValue ? x.RemainingUses.Value : 0;
                        if (n != 0) return true;
                    }
                return false;
            }
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return false;
            ItemInfo<T> info;
            return inv.TryGetItem(id, out info) && info != null;
        }

        /// <summary>原版 `HasItem&lt;T&gt;(T item)` = `HasItemByID&lt;T&gt;(item.GetID())`。</summary>
        public static bool HasItem<T>(T item) where T : class
        {
            if (item == null) return false;
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return false;
            return HasItemByID<T>(inv.IdOf(item));
        }

        /// <summary>原版 `GetOwnedCount&lt;T&gt;(string itemId)`：**解包前** = 原始列表里该 id 的**数量之和**；
        /// **解包后** = 类型化背包那一条的 `Amount`。</summary>
        public static int GetOwnedCount<T>(string itemId) where T : class
        {
            if (string.IsNullOrWhiteSpace(itemId)) return 0;
            if (!_finishedUnpack)
            {
                int sum = 0;
                if (_raw != null)
                    for (int i = 0; i < _raw.Count; i++)
                    {
                        ItemInstance x = _raw[i];
                        if (x == null || x.ItemId != itemId) continue;
                        sum += x.RemainingUses.HasValue ? x.RemainingUses.Value : 0;
                    }
                return sum;
            }
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return 0;
            ItemInfo<T> info = inv.Get(itemId);
            return info != null ? info.Amount : 0;
        }

        /// <summary>原版 `GetOwnedCount&lt;T&gt;(T item)` = 按 `item.GetID()` 那一档。</summary>
        public static int GetOwnedCount<T>(T item) where T : class
        {
            if (item == null) return 0;
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return 0;
            return GetOwnedCount<T>(inv.IdOf(item));
        }

        /// <summary>原版 `GetOwnedCount&lt;T&gt;()`（无 id 那一档）：**这一类物品的总数** ——
        /// 解包前 = 原始列表全体求和；解包后 = 该背包所有条目 `Amount` 之和。</summary>
        public static int GetOwnedCount<T>() where T : class
        {
            if (!_finishedUnpack)
            {
                int sum = 0;
                if (_raw != null)
                    for (int i = 0; i < _raw.Count; i++)
                    {
                        ItemInstance x = _raw[i];
                        if (x == null) continue;
                        sum += x.RemainingUses.HasValue ? x.RemainingUses.Value : 0;
                    }
                return sum;
            }
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return 0;
            int n = 0;
            foreach (ItemInfo<T> info in inv.GetItems()) if (info != null) n += info.Amount;
            return n;
        }

        // ⚠️ 原版还有 `GetOwnedCount<T>(LoadableReference<T>)` / `HasEnough<T>(LoadableReference<T>, int)` 两个重载 ——
        //    它们只做一件事：`item.TargetId` → 转调 id 那一档。**2026-10-27 现读（B9_W7COLL）**：`Core/LoadableRef.cs`
        //    已建，`HasEnough<T>(LoadableRef<T>, int)` 已补（本类类尾，账 `收藏:247`）；`GetOwnedCount<T>(LoadableRef<T>)` 仍没有（`收藏:244` 判 D）。

        /// <summary>原版 `TryGetItem&lt;T&gt;(string id, out IItemInfo&lt;T&gt;)`。</summary>
        public static bool TryGetItem<T>(string id, out ItemInfo<T> item) where T : class
        {
            item = null;
            if (string.IsNullOrWhiteSpace(id)) return false;
            Inventory<T> inv = GetInventory<T>();
            return inv != null && inv.TryGetItem(id, out item);
        }

        /// <summary>原版 `TryGetItem&lt;T&gt;(Func&lt;T,bool&gt;, out IItemInfo&lt;T&gt;)`：按谓词取第一条。</summary>
        public static bool TryGetItem<T>(Func<T, bool> predicate, out ItemInfo<T> item) where T : class
        {
            item = null;
            Inventory<T> inv = GetInventory<T>();
            return inv != null && inv.TryGetItem(predicate, out item);
        }

        /// <summary>原版 `Add&lt;T&gt;(item, amount, instanceId, customData, overrideAmount)`。
        /// 两句与原文一致：① `instanceId` 非空 ⇒ 记进 `inventoryInstanceIds`（`instanceId → itemId`）；
        /// ② 再转调该类型的背包。</summary>
        public static void Add<T>(T item, int amount = 1, string instanceId = null,
                                 Dictionary<string, string> customData = null, bool overrideAmount = false) where T : class
        {
            if (item == null) return;
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return;
            if (!string.IsNullOrWhiteSpace(instanceId)) _instanceIds[instanceId] = inv.IdOf(item);
            inv.Add(item, amount, instanceId, customData, overrideAmount);
        }

        /// <summary>原版 `Remove(string instanceId, int amount)`：按 `instanceId → itemId` 找到那件物品，再删。
        /// 找不到 ⇒ 原版两条 `CustomDebug.LogError`（「There's no item with instanceId …」/「couldn't find item …」），
        /// 我们同样出声。</summary>
        public static void Remove(string instanceId, int amount)
        {
            string itemId;
            if (string.IsNullOrEmpty(instanceId) || !_instanceIds.TryGetValue(instanceId, out itemId))
            {
                Note("rm-no-instance:" + (instanceId ?? "<null>"),
                     "背包里没有 instanceId 为 `" + (instanceId ?? "<null>") + "` 的物品 ⇒ 不删任何东西");
                return;
            }
            foreach (var kv in _inventories)
            {
                Entry e = kv.Value;
                if (e == null || e.ResolveId == null || e.RemoveById == null) continue;
                if (e.ResolveId(itemId) == null) continue;
                if (!e.RemoveById(itemId, amount))
                    Note("rm-miss:" + itemId, "按 instanceId 定位到了 `" + itemId + "`，但在背包里没有那一条 ⇒ 不删");
                return;
            }
            Note("rm-unresolved:" + itemId,
                 "删物品失败：没有哪个类型解得出 id `" + itemId + "`（需要 `SetResolver`）");
        }

        /// <summary>原版 `Remove&lt;T&gt;(T item, int amount)`：走 `OnItemCountModify(item, -amount)`
        /// （= 第一部分那个「对外数量变化入口」，会调子类钩子 + 通知子集 + 抛变更事件；数量夹在 0 以上，不出现负库存）。
        /// ⚠️ 背包里**没有这一条**时**直接返回**（原版 `Inventory.Remove` 也是找不到就不动；⛔ 别让本方法顺手建一条 0 的）。</summary>
        public static void Remove<T>(T item, int amount) where T : class
        {
            Inventory<T> inv = GetInventory<T>();
            if (inv == null || item == null) return;
            if (inv.Get(item) == null) return;
            inv.OnItemCountModify(item, -amount);
        }

        /// <summary>原版 `RemoveAll&lt;T&gt;(T itemRef)`：`TryGetItem(id)` 命中 ⇒ 把这一条清成 0。</summary>
        public static void RemoveAll<T>(T itemRef) where T : class
        {
            Inventory<T> inv = GetInventory<T>();
            if (inv == null || itemRef == null) return;
            string id = inv.IdOf(itemRef);
            if (string.IsNullOrWhiteSpace(id)) return;
            ItemInfo<T> info;
            if (!inv.TryGetItem(id, out info) || info == null) return;
            int n = info.Amount;
            if (n != 0) inv.OnItemCountModify(info.Item, -n);
        }

        /// <summary>原版 `HasEnough&lt;T&gt;(T item, int amount)` = `GetOwnedCount(item) - amount &gt;= 0`。</summary>
        public static bool HasEnough<T>(T item, int amount) where T : class
        {
            if (item == null) return false;
            return GetOwnedCount<T>(item) >= amount;
        }

        /// <summary>原版 `HasEnough&lt;T&gt;(string itemId, int amount)`。</summary>
        public static bool HasEnough<T>(string itemId, int amount) where T : class
        {
            return GetOwnedCount<T>(itemId) >= amount;
        }

        /// <summary>原版 `AssignEphemeralState&lt;T&gt;(T rewardItem, EphemeralState)`：把限时态记到那一条上，
        /// 并在 `EphemeralEndTime` 排一个 `actionId = "RemoveEphemeralItem_" + itemId` 的**移除**动作。
        /// <para>⚠️ 原版那一刻交给 `ActionScheduler.Instance.Schedule(...)`（单例，全项目共用一个时钟）；
        /// **我们没有 `ActionScheduler`** ⇒ 排进本类的待办表，由 **`PumpExpiries(now)`** 触发。
        /// 🔴 **今天没有谁在泵它** —— 全工程「限时物品」实例数 = **0**（`Shell/CampaignData.cs:325-341` 实读：
        /// 47 份战役节点 89 条奖励 `ephemeralState` 全空、`start/duration` 全 0）⇒ 可见差异 0，**但它不是「已实现」**：
        /// 真接上时钟之前，到期**不会**自动移除（`PendingExpiryCount` 会告诉你堆了几条）。</para></summary>
        public static void AssignEphemeralState<T>(T rewardItem, EphemeralState rewardEphemeralState) where T : class
        {
            if (rewardItem == null || rewardEphemeralState == null) return;
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return;
            string id = inv.IdOf(rewardItem);
            if (string.IsNullOrWhiteSpace(id)) return;
            ItemInfo<T> info;
            if (!inv.TryGetItem(id, out info) || info == null) return;

            info.AssignEphemeralState(rewardEphemeralState);
            string actionId = "RemoveEphemeralItem_" + id;
            for (int i = _expiries.Count - 1; i >= 0; i--)
                if (_expiries[i] != null && _expiries[i].ActionId == actionId) _expiries.RemoveAt(i);

            T captured = rewardItem;
            var p = new PendingExpiry();
            p.At = rewardEphemeralState.EphemeralEndTime;
            p.ItemId = id;
            p.ActionId = actionId;
            p.Remove = () => { RemoveAll<T>(captured); };
            _expiries.Add(p);
        }

        /// <summary>原版 `GetExpirationDate&lt;T&gt;(T itemRef)`：**没有限时态 ⇒ `DateTime.MinValue`**（不是 MaxValue），
        /// 有 ⇒ 它的 `EphemeralEndTime`。⚠️ 两支都别弄反（`MaxValue` 是 `EphemeralState` 自己「没信息」时的值）。</summary>
        public static DateTime GetExpirationDate<T>(T itemRef) where T : class
        {
            if (itemRef == null) return DateTime.MinValue;
            Inventory<T> inv = GetInventory<T>();
            if (inv == null) return DateTime.MinValue;
            string id = inv.IdOf(itemRef);
            if (string.IsNullOrWhiteSpace(id)) return DateTime.MinValue;
            ItemInfo<T> info = inv.Get(id);
            if (info == null || info.EphemeralState == null) return DateTime.MinValue;
            return info.EphemeralState.EphemeralEndTime;
        }

        /// <summary>把到期的限时物品移除掉（原版没有这个方法：那边是 `ActionScheduler` 自己按时间调）。
        /// 返回本次移除了几条。**谁有时钟谁泵** —— 今天没人泵（见 `AssignEphemeralState` 的注释）。</summary>
        public static int PumpExpiries(DateTime nowUtc)
        {
            int n = 0;
            for (int i = _expiries.Count - 1; i >= 0; i--)
            {
                PendingExpiry p = _expiries[i];
                if (p == null) { _expiries.RemoveAt(i); continue; }
                if (p.At > nowUtc) continue;
                _expiries.RemoveAt(i);
                try { if (p.Remove != null) { p.Remove(); n++; } }
                catch (Exception e)
                {
                    UnityEngine.Debug.LogWarning("[InventoryManager] 限时物品 `" + p.ItemId + "` 到期移除失败：" + e.Message);
                }
            }
            return n;
        }

        /// <summary>自检用：把门面全部清空（**不落盘**；背包本身的内容由各自的 `ClearForTest` 清）。</summary>
        public static void ClearForTest()
        {
            _inventories.Clear();
            _resolvers.Clear();
            _instanceIds.Clear();
            _expiries.Clear();
            _noted.Clear();
            _raw = null;
            _virtualCurrencies = null;
            _finishedUnpack = false;
        }

        /// <summary>原版 `HasEnough&lt;T&gt;(LoadableReference&lt;T&gt; item, int amount)`（账 `收藏:247`）——
        /// 只做一件事：拿 `item.TargetId` 转调 id 那一档，再比数量。
        /// <para>判据：`cpp2il/out_cpp2il_dev_ilspy/InventoryManager.cs:1481-1501`；比较方向取同族真体
        /// `decomp_full/InventoryManager__HasEnough_object_.c` 的末句 `return param_3 &lt;= iVar1;`（= `amount &lt;= owned`），
        /// 与既有 `HasEnough&lt;T&gt;(T,int)` / `(string,int)` 的 `GetOwnedCount(...) &gt;= amount` 同向。
        /// ⚠️ **`LoadableReference` 那一支的机器码产物没有单独导出**（只有 `object` / `Il2CppFullySharedGenericType`
        /// 两份），所以「它和另外两支的比较式逐字相同」是**由同族两处一致性推来的**，不是直读 —— 如实记。</para>
        /// <para>🔴 **`TargetId` 空 ⇒ 出声 + false**：`GetOwnedCount&lt;T&gt;("")` 返回 0，若照抄成
        /// `0 &gt;= amount`，`amount == 0` 时会**静默 true**（「没填 id 就当成够」）—— 本项目红线，禁止。</para></summary>
        public static bool HasEnough<T>(LoadableRef<T> item, int amount) where T : class
        {
            if (item == null)
            {
                Note("hasenough-ref:null", "`HasEnough<T>(LoadableRef<T>, int)` 收到 null 引用 ⇒ false（原版同一档会 NRE）");
                return false;
            }
            string id = item.TargetId;
            if (string.IsNullOrWhiteSpace(id))
            {
                Note("hasenough-ref:empty", "`HasEnough<T>(LoadableRef<T>, int)` 的 `TargetId` 是空 ⇒ false"
                    + "（⛔ 不许静默 true：`GetOwnedCount(\"\")` 是 0，amount==0 时会看起来「够」）");
                return false;
            }
            return GetOwnedCount<T>(id) >= amount;
        }
    }

    // ---------------------------------------------------------- 三份特化（原版三个 Inventory<T> 子类）
    /// <summary>原版 `CardInventory : Inventory&lt;RawCardScript&gt;`（cpp2il 真体 183 行）。
    /// 它多出来的是三件：① 数量变化/重建时**发一次「卡牌收藏更新」信号**；② `customData.CardTier` ⇒ 登记卡牌档位；
    /// ③ **异画条目数量绑在原卡上**（`QuantityBoundItemInfo`）。
    /// <para>⚠️ **ctor 的差异（如实标）**：原版 ctor = `CardServices.CleanTierCollection()`（清**运行时**档位表，
    /// 之后由 `Add` 从 customData 重建）；我们的档位正本是**持久化的** `CardProgress._lvl`（`WarpforgeCardLevels.json`）
    /// ⇒ **绝不能在这里清**（清 = 抹掉玩家的升级）。ctor 因此是空实现，理由写在 ctor 上。</para></summary>
    public class CardInventory : Inventory<RuleEngine.CardDef>
    {
        /// <summary>「这张异画的原卡 id」—— 原版是 `RawCardScript.currentAlternateArt.originalCard`（SO 里的 PPtr）。
        /// 本地**没有**那层映射（`currentAlternateArt` 在 1,351 张卡的导出里**全为 null**，
        /// 判据 = `资料/普查产出_1018/allcards_1350/cards_raw.json`）⇒ 缺省 `null`，
        /// 与原文「`currentAlternateArt == null` ⇒ 返回 null」那一支**行为相同**。谁拿到真映射谁注入（一行）。</summary>
        public static Func<RuleEngine.CardDef, string> AlternateArtOriginalOf;

        /// <summary>原版 `Signal.Raise&lt;CardCollectionUpdatedSignal&gt;()` 的等价物（我们没信号层 ⇒ 普通事件）。</summary>
        public event Action CollectionUpdated;

        public CardInventory() : base(c => c != null ? c.Id : null)
        {
            // 原版这里是 `CardServices.CleanTierCollection()`。⛔ **我们不清**：`CardProgress._lvl` 是持久化正本，
            // 清掉等于把玩家的升级档抹回出厂值。原版清的是「运行时档位表」（每次解包后由 customData 重建），
            // 我们的等价重建是 `Add/UpdateCardTier` 那条路（不依赖前置清空）。
        }

        /// <summary>原版 override：`base.UpdateItemCollection(); Signal.Raise&lt;CardCollectionUpdatedSignal&gt;();`</summary>
        public override void UpdateItemCollection()
        {
            base.UpdateItemCollection();
            if (CollectionUpdated != null) CollectionUpdated();
        }

        /// <summary>原版 `CardInventory.Create(item, instanceId, customData)`：**只管异画**
        /// （非异画 ⇒ 原版**返回 null**，普通卡走 `Add` 那条路 —— 这是原文读出来的，不是我们简化）。</summary>
        public override ItemInfo<RuleEngine.CardDef> Create(RuleEngine.CardDef item, string instanceId,
                                                           Dictionary<string, string> customData)
        {
            if (item == null) return null;
            if (!IsAlternateArt(item)) return null;
            ItemInfo<RuleEngine.CardDef> info = CreateAlternateArtInfo(item, instanceId, customData);
            if (info != null && customData != null) info.SetCustomData(customData);
            return info;
        }

        /// <summary>原版 `CardInventory.Add(...)`：`base.Add(...)` 之后再按 customData 登记档位。</summary>
        public override ItemInfo<RuleEngine.CardDef> Add(RuleEngine.CardDef item, int amount, string instanceId,
                                                        Dictionary<string, string> customData, bool overrideAmount)
        {
            ItemInfo<RuleEngine.CardDef> info = base.Add(item, amount, instanceId, customData, overrideAmount);
            if (info != null) UpdateCardTier(item, customData);
            return info;
        }

        /// <summary>原版 private static `UpdateCardTier(RawCardScript card, Dictionary customData)`：
        /// **异画不登记**；否则读 `customData["CardTier"]`（缺省 `"0"`）⇒ `CardServices.RegisterCardTier(card, tier)`。
        /// ⚠️ 落点差异：我们的档位正本 = `CardProgress`（`CardProgress.Level` 是 **1 基** = 原版 `CardTier + 1`，
        /// 判据见 `Core/CardArt.cs:326-330` 那一串）⇒ 这里写 `SetLevel(id, tier + 1)`。
        /// ⚠️ 原版 `int.Parse` 遇坏串会**抛**；我们出声 + 跳过（不崩）。</summary>
        public static void UpdateCardTier(RuleEngine.CardDef card, Dictionary<string, string> customData)
        {
            if (card == null) return;
            if (IsAlternateArt(card)) return;
            string s = null;
            if (customData != null) customData.TryGetValue("CardTier", out s);
            if (s == null) s = "0";
            int tier;
            if (!int.TryParse(s, out tier))
            {
                InventoryManager.Note("tier-bad:" + card.Id,
                    "卡 `" + card.Id + "` 的 customData.CardTier = `" + s + "` 解不成整数 ⇒ 不登记档位（原版这里会抛）");
                return;
            }
            CardProgress.SetLevel(card.Id, tier + 1);
        }

        /// <summary>原版 private `CreateAlternateArtInfo(item, instanceId, customData)`：
        /// 已有 ⇒ 返回已有的；否则「原卡那条（`autoCreate`）」+ 新建一条 `QuantityBoundItemInfo`；
        /// `currentAlternateArt == null` ⇒ **返回 null**。</summary>
        public ItemInfo<RuleEngine.CardDef> CreateAlternateArtInfo(RuleEngine.CardDef item, string instanceId,
                                                                  Dictionary<string, string> customData)
        {
            if (item == null) return null;
            string id = IdOf(item);
            if (string.IsNullOrEmpty(id)) return null;
            ItemInfo<RuleEngine.CardDef> existing = Get(id);
            if (existing != null) return existing;

            if (AlternateArtOriginalOf == null) return null;       // = 原版 currentAlternateArt == null 那一支
            string originalId = AlternateArtOriginalOf(item);
            if (string.IsNullOrEmpty(originalId)) return null;
            RuleEngine.CardDef original = InventoryManager.Resolve<RuleEngine.CardDef>(originalId);
            if (original == null)
            {
                InventoryManager.Note("altart-orig:" + originalId,
                    "异画 `" + id + "` 的原卡 `" + originalId + "` 解不出来 ⇒ 不能绑数量（原版 `Get(originalCard, autoCreate:true)`）");
                return null;
            }
            ItemInfo<RuleEngine.CardDef> boundInfo = Get(original);
            if (boundInfo == null) boundInfo = Create(original);
            var bound = new QuantityBoundItemInfo<RuleEngine.CardDef>(item, boundInfo, instanceId);
            PutInfo(id, bound);
            return bound;
        }

        /// <summary>这张卡在本地是不是**异画**。判据 = 我们自己的异画表
        /// `CollectionWindow.AltArtCards`（7 条，卡 id 逐个实读；`Collections` 页那 7 张就是它）。
        /// ⚠️ 原版判据是 `RawCardScript.IsAlternateArt()`（SO 上的方法），我们没那一位 ⇒ 用这张表当唯一判据，
        /// ⛔ 别按「id 里有 AA」猜（那是文件名约定，不是数据）。
        /// 声明成 `Func` 让调用方可以换（自检也会换）—— 缺省实现就是查那张表。</summary>
        public static Func<RuleEngine.CardDef, bool> IsAlternateArtOf;

        static bool IsAlternateArt(RuleEngine.CardDef card)
        {
            if (IsAlternateArtOf != null) return IsAlternateArtOf(card);
            if (card == null) return false;
            var table = CollectionWindow.AltArtCards;
            if (table == null) return false;
            for (int i = 0; i < table.Length; i++) if (table[i].CardId == card.Id) return true;
            return false;
        }
    }

    /// <summary>异画解锁条目（原版 `AlternateArtCard`：`style` + `originalCard` + `cardImage` + …）。
    /// <para>⚠️ **为什么不直接用 `CollectionWindow.AltArtCard`**：那是 `struct`，而 `Inventory&lt;T&gt;` 的
    /// `where T : class`（文件头「差异 ①」的连带）要求引用类型 ⇒ 这里给一个**同两字段的类**当背包条目。
    /// ⛔ 别把它当成第二个异画正本：卡 id / 风格的**正本仍然是 `CollectionWindow.AltArtCards`**，本类只是背包条目。</para></summary>
    public sealed class AltArtUnlock
    {
        /// <summary>= `AlternateArtCard.GetID()`（异画自己那张卡的 id）。</summary>
        public string CardId;
        /// <summary>= `AlternateArtCard.style`（原版枚举名，本地 7 张全 `HammerAndBolter`）。</summary>
        public string Style;
        /// <summary>= `AlternateArtCard.GetIdForClonedCard()`：解锁异画时**同时给的那张卡的 id**。
        /// ⚠️ 本地**查不到**它的真值（原版要读 `AlternateArtCard` 那批 SO，本地 `assets_full` 里没有；
        /// 7 张卡的 `currentAlternateArt` 也全 null）⇒ 留 null 并在 `AlternateArtInventory` 里出声。</summary>
        public string ClonedCardId;
        public override string ToString() { return CardId + "/" + Style; }
    }

    /// <summary>原版 `AlternateArtInventory : Inventory&lt;AlternateArtCard&gt;`（cpp2il 真体 168 行）。
    /// 两处 override：① `OnFinishUnpack()` —— 解包完成后**逐条**把「被克隆的那张卡」补进卡牌背包；
    /// ② `OnItemCountModify(item, amount)` —— `amount &gt; 0` 时同样补一次。
    /// <para>🔴 **我们落成事件的如实理由**：原版这两跳调 `InventoryManager.Add(AssetLocator.GetAsset(clonedId))`，
    /// 也就是「**解锁异画 ⇒ 顺手给一张原卡**」。我们的卡牌拥有数是 `CardProgress` 的「全解锁」口径
    /// （`Inventories.Cards` 已把每张卡按 `CardProgress.Owned` 播满）⇒ **再 Add 一次会翻倍**。
    /// 所以这里落成 `AlternateArtUnlocked(clonedCardId)` 事件：需要那张卡的消费方自己决定加不加。
    /// ⚠️ 这是**有意的行为差异**（不是没做），⛔ 别为了让日志好看去调 `Add`。</para></summary>
    public class AlternateArtInventory : Inventory<AltArtUnlock>
    {
        /// <summary>某张异画解锁了（参数 = `ClonedCardId`，可能为 null）。</summary>
        public event Action<string> AlternateArtUnlocked;

        public AlternateArtInventory() : base(a => a != null ? a.CardId : null) { }

        public override void OnFinishUnpack()
        {
            base.OnFinishUnpack();
            foreach (ItemInfo<AltArtUnlock> info in GetItems())
                if (info != null && info.Item != null) Raise(info.Item);
        }

        /// <summary>原版 `AlternateArtInventory.OnItemCountModify(item, amount)`（`protected override`）：
        /// `base` 之后 `amount &gt; 0` 就补一次「解锁」动作。
        /// ⚠️ 我们的钩子名多一个 d（`OnItemCountModified`）—— 理由见 `Inventory&lt;T&gt;.OnItemCountModified` 的 doc。</summary>
        protected override void OnItemCountModified(AltArtUnlock item, int amountChanged)
        {
            base.OnItemCountModified(item, amountChanged);
            if (amountChanged > 0 && item != null) Raise(item);
        }

        void Raise(AltArtUnlock item)
        {
            if (AlternateArtUnlocked == null) return;
            if (string.IsNullOrEmpty(item.ClonedCardId))
                InventoryManager.Note("altart-clone:" + item.CardId,
                    "异画 `" + item.CardId + "` 的 `ClonedCardId` 查不到（原版 `GetIdForClonedCard()`）⇒ 事件里给 null");
            AlternateArtUnlocked(item.ClonedCardId);
        }
    }

    /// <summary>万能卡条目（原版 `Wildcard`：`cardArmy` + `cardRarity` 两个 SO 字段）。
    /// <para>⚠️ 我们的万能卡计数正本是 `CardProgress`（按**小写稀有度**分桶：`common/rare/epic/legendary`）。
    /// 这个类只当背包条目的 key，`Id` 拼成原版那套 id 形状（`ItemDrawer.Spec` 认的 `Wildcard&lt;阵营&gt;&lt;档&gt;`
    /// 里「不带阵营」那一支）。⛔ 别把它当第二份计数正本。</para></summary>
    public sealed class InventoryWildcard
    {
        /// <summary>阵营名（原版 `Wildcard.cardArmy` 的**名字**；空 = 中立/不分区）。</summary>
        public string Army;
        /// <summary>稀有度档（本地约定 = 小写英文，同 `CardProgress` 的键）。</summary>
        public string Rarity;
        /// <summary>`"Wildcard" + Army + Rarity`（与 `ItemDrawer.Spec` 解析的那套 id 同形）。</summary>
        public string Id { get { return "Wildcard" + (Army ?? "") + (Rarity ?? ""); } }
        public override string ToString() { return Id; }
    }

    /// <summary>原版 `WildcardInventory : Inventory&lt;Wildcard&gt;`（cpp2il 真体 22 行）：
    /// 唯一的行为是 `UpdateItemCollection()` 里 `Signal.Raise(updateSignal)` —— 即「重建完 ⇒ 通知万能卡界面刷新」。
    /// 我们没信号层 ⇒ 落成普通事件。⚠️ 它**只发信号**，不碰数量（数量正本仍在 `CardProgress`）。</summary>
    public class WildcardInventory : Inventory<InventoryWildcard>
    {
        /// <summary>原版 `Signal.Raise(WildcardUpdateSignal)` 的等价物。</summary>
        public event Action Updated;

        public WildcardInventory() : base(w => w != null ? w.Id : null) { }

        public override void UpdateItemCollection()
        {
            base.UpdateItemCollection();
            if (Updated != null) Updated();
        }
    }

    // ============================================================ 追加区 · 2026-10-27（B26 · 基础设施 Inventory 背包层/徽记）
    //
    // 【这一段补的是什么】`判据/C档施工批次表.tsv` 的 **B26「基础设施·Inventory 背包层/徽记」37 行**
    //   （账行：泛型与启动链 22 · 商店 8 · 未分类_其他 3 · 社交_聊天 3 · 其他_基础设施 1）。
    //   落点 = 本文件 + 同目录 `BadgeNotifications.cs`（3 个 record）+ 新建 `Shell/Points.cs`（积分基类）。
    //   ⛔ 本段**不往上面插行**：账的 A 档落点钉着行号（`Shell/Inventory.cs:46/103/127/…/1456`），
    //     插一行就让 `工具/check_ledger_points.py` 逐条报漂移（`CLAUDE.md` 铁律 18·b · N9 那次的原形）。
    // 【判据一律先找真方法体】非泛型方法 → `d:/2/tools/decomp_full/<类>__<方法>.c`；
    //   泛型方法（`Inventory<T>` / `ItemInfo<T>` / `QuantityBoundItemInfo<T>` / `Points<T>`）→
    //   `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/*.cs`（**只有这一份有真体**）+ `dump.cs` 的 `GenericInstMethod` RVA → VA 反汇编（`工具/disasm_va.py`）。
    // ---------------------------------------------------------- ① `ItemInfo<T>.Convert<TOut>()`
        public partial class ItemInfo<T> where T : class
        {
            /// <summary>原版 `ItemInfo&lt;T&gt;.Convert&lt;TOut&gt;()`（`ItemInfo.cs:144-211` 真体）：把本条**按另一个物品类型再包一条**
            /// —— `new ItemInfo&lt;TOut&gt;(new LoadableReference&lt;TOut&gt;((TOut)(object)Item), Quantity, InstanceID)`。
            /// ⇒ 数量 / 实例 id **照抄**；`customData` / `ephemeralState` **不搬**（原版 3 参 ctor 只设这三样，`ephemeralState` 是新对象）。
            /// <para>⚠️ 原版 cast 失败（`Item` 不是 `TOut`）会抛 `InvalidCastException`；我们**给 null**（⛔ 不静默，由调用方判 —— 原版唯一消费者是
            /// `ShopContainerInventory`「把 `ItemInfo&lt;T&gt;` 转成 `IItemInfo&lt;ShopContainerBase&gt;`」，那也是本批的落点）。</para></summary>
            public ItemInfo<TOut> Convert<TOut>() where TOut : class
            {
                TOut v = Item as TOut;
                if (v == null) return null;
                return new ItemInfo<TOut>(v, Amount, InstanceId);
            }
        }

        // ---------------------------------------------------------- ② `Inventory<T>`：子集字典 + 重建真体
        public partial class Inventory<T> where T : class
        {
            /// <summary>原版 `Inventory.subSets`（`Dictionary&lt;Type, InventorySubset&gt;`，`Inventory.cs:345`，`protected readonly`）—— 2026-10-27 B26 补。
            /// ⚠️ 我们另有一份 `List&lt;InventorySubset&lt;T&gt;&gt; _subsets`（按登记顺序通知），两份**同源不互替**：字典按 `Type` 认，列表按登记序发通知。</summary>
            readonly Dictionary<Type, InventorySubset<T>> _subsetsByType = new Dictionary<Type, InventorySubset<T>>();

            /// <summary>已登记的子集（原版 `subSets` 的只读视图）。</summary>
            public IReadOnlyDictionary<Type, InventorySubset<T>> Subsets { get { return _subsetsByType; } }

            /// <summary>`UpdateItemCollection()` 的真体（原版 `Inventory.cs:685-1050` 那句
            /// `items = subSets.SelectMany(kv =&gt; kv.Value.InternalDictionary).ToDictionary(k =&gt; k.Key, k =&gt; (IItemInfo&lt;T&gt;)k.Value)`；
            /// 三个编译器 lambda 的真体在 `decomp_full/Inventory.__c_object____UpdateItemCollection_b__12_{0,1,2}.c`）。
            /// ⚠️ 我们**只在有子集时**重建（理由写在 `UpdateItemCollection` 的注释里）。</summary>
            internal void RebuildFromSubsets()
            {
                var merged = new Dictionary<string, ItemInfo<T>>();
                foreach (var kv in _subsetsByType)
                {
                    if (kv.Value == null) continue;
                    foreach (var e in kv.Value.InternalDictionary) if (e.Value != null) merged[e.Key] = e.Value;
                }
                _byId.Clear();
                foreach (var kv in merged) _byId[kv.Key] = kv.Value;
            }
        }

        // ---------------------------------------------------------- ③ `InventorySubset<T>`：上游字典
        public sealed partial class InventorySubset<T> where T : class
        {
            /// <summary>原版 `InventorySubset.InternalDictionary`（`IReadOnlyDictionary&lt;string, IItemInfo&lt;IIdentifiable&gt;&gt;`，`InventorySubset.cs:41`）——
            /// `UpdateItemCollection` 扁平化的**唯一输入**。</summary>
            public IReadOnlyDictionary<string, ItemInfo<T>> InternalDictionary { get { return _sub; } }
        }

        // ---------------------------------------------------------- ④ `QuantityBoundItemInfo<T>.Add`（原版是空方法）
        public sealed partial class QuantityBoundItemInfo<T> : ItemInfo<T> where T : class
        {
            /// <summary>原版 `QuantityBoundItemInfo&lt;T&gt;.Add(int, string)` 的**真身 = 空方法**：`il2cpp_out/dump.cs:70479-70485` 把
            /// `QuantityBoundItemInfo&lt;object&gt;.Add` 的 RVA 指到 **0x4B33B0** = 全局共享空桩（机器码 `c2 00 00 00` = `ret 0`）。
            /// ⇒ 数量**只由被绑那条决定**，这里什么都不做。
            /// <para>🔴 账原来把这一条写成「加数量但夹在 quantity 上限内」—— **与二进制不符**，2026-10-27 B26 就地订正（铁律 5）。</para></summary>
            public override void Add(int amount, string instanceId = null) { }
        }

        // ---------------------------------------------------------- ⑤ `IdentifiableWrapper` 族（账 `泛型与启动链:626/627`）
        /// <summary>原版 `abstract class IdentifiableWrapper : IdentifiableSO`（`IdentifiableWrapper.cs`）。
        /// ⚠️ **落点差异（如实标）**：判据文件把它放在 `Core/Identifiable.cs`；本批白名单只到 `Shell/` 这 5 个文件 ⇒ 落本文件。
        /// 它是普通类（原版是 ScriptableObject）⇒ 这里**不涉**「类名 == 文件名」那条 Unity 存盘约束。</summary>
        public abstract class IdentifiableWrapper
        {
            public abstract object RawData { get; }
            public abstract string GetID();
        }

        /// <summary>原版 `IdentifiableWrapper&lt;T&gt; where T : IIdentifiable`。
        /// <para>`GetID()`（`IdentifiableWrapper.cs:149`）逐句 = **先 `Data.GetID()`**（对 T 的虚调用）。我们没有 `IIdentifiable`
        /// ⇒ 三档：显式 `Id` → 注入的 `IdSource` → `"wrapper_" + GetHashCode()`。
        /// 🔶 自己设计（非原版）：第三档原版不存在（它靠 `IdentifiableSO` 的资产名）；调度台 2026-10-27 裁定（B26）。谁有真 id 就设 `IdSource`（一行）。</para>
        /// <para>`op_Implicit`（`il2cpp_out/dump.cs:51181`）= `return instanceRef.Data;`（null ⇒ 原版 NRE，我们给 null）。</para></summary>
        public abstract class IdentifiableWrapper<T> : IdentifiableWrapper where T : class
        {
            public abstract T Data { get; }
            /// <summary>显式 id（原版 `IdentifiableSO` 的资产身份）。空 = 走 `IdSource`。</summary>
            public string Id;
            /// <summary>🔶 自己设计（非原版）：从 `Data` 取 id 的口（原版 `T : IIdentifiable.GetID()`）。调度台 2026-10-27 裁定（B26）。</summary>
            public Func<T, string> IdSource;

            public override object RawData { get { return Data; } }

            public override string GetID()
            {
                if (!string.IsNullOrEmpty(Id)) return Id;
                T d = Data;
                if (d == null) return null;
                if (IdSource != null) return IdSource(d);
                return "wrapper_" + d.GetHashCode().ToString(System.Globalization.CultureInfo.InvariantCulture);
            }

            public static implicit operator T(IdentifiableWrapper<T> instanceRef)
            {
                return instanceRef != null ? instanceRef.Data : null;
            }
        }

        // ---------------------------------------------------------- ⑥ 货币（账 `商店:54/59/60/61/62`）
        /// <summary>原版 `GameCurrency` 枚举（`il2cpp_out/dump.cs:46139-46153`，TypeDefIndex 957）—— **值照抄**（不是 0..n 连号，别重排）。</summary>
        public enum GameCurrency
        {
            gold = 0, crystals = 10, blackStones = 20, realMoney = 30, free = 40,
            unavailable = 50, raidMedals = 60, gachaTickets = 70, campaignPoints = 71, energy = 72,
        }

        /// <summary>原版 `ICurrency : IIdentifiable`（`il2cpp_out/dump.cs:45051-45071`）：`Type` / `Code` / `MaxAmount` / `GetID()`。
        /// <para>`MaxAmount` 的**默认实现 = -1（不封顶）**：真身 `ICurrency.get_MaxAmount` @ VA **0x18065CCB0** 只有两条指令
        /// `mov eax,0xffffffff; ret`（`工具/disasm_va.py` 现读）。⇒ 每个实现类自己给 `-1`。</para>
        /// <para>⚠️ 原版还有 `Sprite GetIcon(IconSize)` —— 这一层没有 sprite（图标在 `CardArt`/徽记那一族）⇒ 不建空壳。</para></summary>
        public interface ICurrency
        {
            GameCurrency Type { get; }
            string Code { get; }
            int MaxAmount { get; }
            string GetID();
        }

        /// <summary>原版 `Currency : ObtainableItem, ICurrency, IIdentifiable`（`Currency.cs`）：`code` + `type`（+ `maxAmount`，`ObtainableItem` 侧）。
        /// `GetID()` = `code`（`Currency.cs:128-131` 真体逐字 `return code;`）。</summary>
        public class Currency : ICurrency
        {
            public string code;
            public GameCurrency type;
            /// <summary>上限；`-1` = 不封顶（`ICurrency.MaxAmount` 的默认值）。</summary>
            public int maxAmount = -1;

            public Currency() { }
            public Currency(string code, GameCurrency type, int maxAmount = -1)
            { this.code = code; this.type = type; this.maxAmount = maxAmount; }

            public GameCurrency Type { get { return type; } }
            public string Code { get { return code; } }
            public int MaxAmount { get { return maxAmount; } }
            public string GetID() { return code; }
            public override string ToString() { return string.IsNullOrEmpty(code) ? type.ToString() : code; }
        }

        /// <summary>🔶 自己设计（非原版）：本地「货币名册」= 原版 `ConfigManager.GetConfig&lt;CurrencyConfig&gt;().currencies` 的等价物
        /// （判据 `decomp_full/CurrencyExtensions__ToCurrency.c`：`ConfigManager.GetConfig&lt;object&gt;()` → `+0x10` 那张表 → `FirstOrDefault(c =&gt; c.type == 目标)`）。
        /// 我们**没有 ConfigManager / 货币 SO** ⇒ 用一张可注册的名册；B12/B25 接入真配置后把 `Register` 换成从 SO 播种即可（调用点不动）。
        /// 谁定的：调度台 2026-10-27（B26 批次）。</summary>
        public static class CurrencyCatalog
        {
            static readonly List<ICurrency> _all = new List<ICurrency>();
            public static IReadOnlyList<ICurrency> All { get { return _all; } }

            public static void Register(ICurrency c) { if (c != null && !_all.Contains(c)) _all.Add(c); }
            public static void ClearForTest() { _all.Clear(); }

            /// <summary>原版那一句是 `FirstOrDefault(c =&gt; c.type == currencyType)` ⇒ 查不到给 **null**（⛔ 不造一个假货币）。</summary>
            public static ICurrency Find(GameCurrency type)
            {
                for (int i = 0; i < _all.Count; i++) if (_all[i] != null && _all[i].Type == type) return _all[i];
                return null;
            }
        }

        /// <summary>原版 `static class CurrencyExtensions`（4 个方法；判据 = `decomp_full/CurrencyExtensions__{ToCurrency,GetOwnedCount,CanGetMore,AddCurrency}.c`，**四份都是真体**）。
        /// <para>⚠️ **签名差异（如实标）**：原版前三条的第一个形参是 `InventoryManager manager`（`SingletonBehaviour` 实例）；我们的 `InventoryManager`
        /// 是**静态门面**（见上面「追加区 · 2026-10-26」头的差异 ①）⇒ 形参去掉，行为照旧。</para>
        /// <para>⚠️ `campaignPoints` / `energy` 两条在原版分别读 `LiveOpsManager.GetHandler&lt;CampaignHandler&gt;()[+0x78]`（`NeutralPoints`）与
        /// `SharedEventDataManager.GetContext&lt;EnergyContext&gt;().GetCurrentEnergy()` —— **这两个件本地都还没有** ⇒ 走可注入钩子
        /// （`CampaignPointsOf` / `EnergyOf` / `SpendEnergyOf`）；没注入时**出声 + 给 0**（⛔ 不静默假装「够」）。</para></summary>
        public static class CurrencyExtensions
        {
            /// <summary>原版 `CampaignHandler.NeutralPoints`（`+0x78`）的替身。</summary>
            public static Func<int> CampaignPointsOf;
            /// <summary>原版 `EnergyContext.GetCurrentEnergy()` 的替身。</summary>
            public static Func<int> EnergyOf;
            /// <summary>原版 `EnergyContext.SpendEnergy(int)` 的替身。</summary>
            public static Action<int> SpendEnergyOf;

            /// <summary>原版 `ToCurrency(GameCurrency)`：`campaignPoints` ⇒ `PointsConfig.points.OfType&lt;CampaignPoints&gt;().FirstOrDefault(p =&gt; p.cardArmy == 0)`
            /// （中立那份；`== 0` 是 `CardArmy` 的 0 值）；其余 ⇒ `CurrencyConfig.currencies.FirstOrDefault(c =&gt; c.type == type)`。</summary>
            public static ICurrency ToCurrency(GameCurrency currencyType)
            {
                if (currencyType == GameCurrency.campaignPoints) return PointsCatalog.NeutralCampaignPoints();
                return CurrencyCatalog.Find(currencyType);
            }

            /// <summary>原版 `GetOwnedCount(manager, GameCurrency)`（`CurrencyExtensions__GetOwnedCount.c` 三段分支）。</summary>
            public static int GetOwnedCount(GameCurrency currencyType)
            {
                if (currencyType == GameCurrency.campaignPoints)
                {
                    if (CampaignPointsOf != null) return CampaignPointsOf();
                    return NoSource("campaignPoints（原版读 `CampaignHandler.NeutralPoints`）");
                }
                if (currencyType == GameCurrency.energy)
                {
                    if (EnergyOf != null) return EnergyOf();
                    return NoSource("energy（原版读 `EnergyContext.GetCurrentEnergy()`）");
                }
                ICurrency c = ToCurrency(currencyType);
                if (c == null)
                {
                    InventoryManager.Note("cur-nocatalog:" + currencyType,
                        "`ToCurrency(" + currencyType + ")` 查不到货币（名册里没注册这一种）⇒ 拥有数按 0 算；原版同一情形 `FirstOrDefault` 也给 null");
                    return 0;
                }
                if (InventoryManager.GetInventory<ICurrency>() == null)
                {
                    InventoryManager.Note("cur-noinv",
                        "没有登记 `Inventory<ICurrency>` ⇒ 货币拥有数恒 0（原版那条走 `InventoryManager.GetOwnedCount(ICurrency)`）");
                    return 0;
                }
                return InventoryManager.GetOwnedCount<ICurrency>(c);
            }

            /// <summary>原版 `CanGetMore(ICurrency)`：① `MaxAmount &lt; 0` ⇒ **true**（不封顶）；② 否则 `owned &lt; MaxAmount`
            /// （`CurrencyExtensions__CanGetMore.c` 末句 `return iVar2 &lt; iVar3;`，`iVar3` = 虚属性槽 3 = `MaxAmount`）。</summary>
            public static bool CanGetMore(ICurrency currency)
            {
                if (currency == null) return false;
                if (currency.MaxAmount < 0) return true;
                return GetOwnedCount(currency.Type) < currency.MaxAmount;
            }

            /// <summary>原版 `AddCurrency(manager, ICurrency, int)`（`CurrencyExtensions__AddCurrency.c`）：
            /// `energy` ⇒ **`EnergyContext.SpendEnergy(amount)`**（⚠️ **原版就是 Spend，不是 Add** —— 照抄，⛔ 别「顺手修好」）；
            /// 其余 ⇒ `InventoryManager.Add(currency, amount)`。</summary>
            public static void AddCurrency(ICurrency currency, int amount)
            {
                if (currency == null)
                {
                    InventoryManager.Note("cur-add:null", "`AddCurrency` 收到 null 货币 ⇒ 不加（原版同一档会 NRE）");
                    return;
                }
                if (currency.Type == GameCurrency.energy)
                {
                    if (SpendEnergyOf != null) { SpendEnergyOf(amount); return; }
                    NoSource("energy 的 `SpendEnergy`（`AddCurrency` 那一支）");
                    return;
                }
                if (InventoryManager.GetInventory<ICurrency>() == null)
                {
                    InventoryManager.Note("cur-add:noinv", "没有登记 `Inventory<ICurrency>` ⇒ `AddCurrency(" + currency.GetID() + ", " + amount + ")` 无处可加");
                    return;
                }
                InventoryManager.Add<ICurrency>(currency, amount);
            }

            static int NoSource(string what)
            {
                InventoryManager.Note("cur-nosrc:" + what,
                    "货币数据源缺失：" + what + " —— 本地没有这个件 ⇒ 给 0（⛔ 不是「原版也返回 0」）");
                return 0;
            }
        }

        /// <summary>原版 `CurrencyCondition : EventCondition`（`CurrencyCondition.cs` 全文；真体 `decomp_full/CurrencyCondition__Validate.c`）：
        /// `return InventoryManager.Instance.HasEnough(targetCurrency, minimumAmount);` —— **只调本地背包，0 处 PlayFab / 云脚本**
        /// （这正是 2026-10-27 调度台驳回「判 D」的那条依据）。⚠️ 我们用静态门面 ⇒ 去掉 `.Instance`。
        /// `targetCurrency.targetId` 为空时 `HasEnough` **出声 + false**（⛔ 不许静默 true）。</summary>
        [Serializable]
        public class CurrencyCondition : EventCondition
        {
            public LoadableRef<Currency> targetCurrency;
            public int minimumAmount;

            public override bool Validate(ILiveOps liveOps)
            {
                return InventoryManager.HasEnough<Currency>(targetCurrency, minimumAmount);
            }
        }

        // ---------------------------------------------------------- ⑦ 商店容器库存 + 开包徽记（账 `商店:239/240/241`）
        /// <summary>原版 `abstract class ShopContainerBase : ObtainableItem`（`ShopContainerBase.cs:7-36`）：`containerType` + 抽象 `Open(...)`。
        /// ⚠️ **本批只建门面两格**（`containerType` / `GetID()`）：本地没有容器 SO 层（`ContainerDrawer` 那一族只有**名字映射**，
        /// 判据 `Editor/ShopScene.cs:315-362`）⇒ 其余 4 个成员（`VisualPrefab` / `LegendaryContainer` / `PreviewImage` / `CrateCounterKey`）不建空壳。</summary>
        public abstract class ShopContainerBase
        {
            /// <summary>容器 id（原版 `IdentifiableSO.GetID()` 的资产身份）。</summary>
            public string Id;
            /// <summary>原版 `ContainerType containerType`（枚举；我们用**名字串**，与 `ShopScene` 的 drawer 映射同一套写法）。</summary>
            public string ContainerType;

            public virtual string GetID() { return Id; }
            public override string ToString() { return Id; }
        }

        /// <summary>落点用的最小具体容器（原版是 `ShopContainer` / `DropTableContainer` / `DlcBundle` 那一族 **SO**）。</summary>
        public sealed class ShopContainer : ShopContainerBase
        {
            public ShopContainer(string id) { Id = id; }
            public ShopContainer(string id, string containerType) { Id = id; ContainerType = containerType; }
        }

        /// <summary>原版 `ShopContainerInventory : Inventory&lt;ShopContainerBase&gt;, INotificationProvider&lt;BoosterPackBadge&gt;`（`ShopContainerInventory.cs`，**真体**）。
        /// 三条：① `OnFinishUnpack()` ⇒ `UiBadgeNotificationManager.Refresh&lt;BoosterPackBadge&gt;()`；
        /// ② `OnItemCountModify`：id **含 `"Booster"`（忽略大小写）** ⇒ 同一次刷新；
        /// ③ `CheckNotification()` ⇒ `Repeat(new BoosterPackBadge(), GetItems().Where(id 含 Booster).Sum(Quantity))`
        /// （判据 = 该文件的 `_003CCheckNotification_003Eb__2_0` / `__2_1` 两个 lambda 真体）。
        /// <para>⚠️ 原版 `Refresh&lt;T&gt;()` 就是 `Refresh(typeof(T))` ⇒ 我们同一个口（`UiBadgeNotificationManager.Refresh(Type)`）。</para>
        /// <para>🔴 **可达性（如实记）**：今天没有任何生产者把卡包塞进这份背包 —— 原版「买卡包」那条链是**买完直接开包**
        /// （`Shell/ShopTabPage.cs:772-801` 的注释逐行记着），不进库存。⇒ 这份背包**恒空 ⇒ 徽记恒 0**；
        /// 一旦活动/奖励开始发卡包，它立刻有数据（账 `商店:239-241` 的备注写了同一条）。</para></summary>
        public sealed class ShopContainerInventory : Inventory<ShopContainerBase>, INotificationProvider
        {
            public ShopContainerInventory() : base(c => c != null ? c.GetID() : null) { }

            public Type NotificationType { get { return typeof(BoosterPackBadge); } }
            public IEnumerable<INotificationProvider> ChildProviders { get { return null; } }

            public override void OnFinishUnpack()
            {
                base.OnFinishUnpack();
                UiBadgeNotificationManager.Refresh(typeof(BoosterPackBadge));
            }

            protected override void OnItemCountModified(ShopContainerBase item, int amountChanged)
            {
                base.OnItemCountModified(item, amountChanged);
                if (item == null) return;
                string id = item.GetID();
                if (id != null && id.IndexOf("Booster", StringComparison.InvariantCultureIgnoreCase) >= 0)
                    UiBadgeNotificationManager.Refresh(typeof(BoosterPackBadge));
            }

            public IEnumerable<BadgeNotification> CheckNotification()
            {
                int n = 0;
                foreach (ItemInfo<ShopContainerBase> info in GetItems())
                {
                    if (info == null || info.Item == null) continue;
                    string id = info.Item.GetID();
                    if (id == null || id.IndexOf("Booster", StringComparison.InvariantCultureIgnoreCase) < 0) continue;
                    n += info.Amount;                      // 原版 `Enumerable.Sum(x =&gt; x.Quantity)`
                }
                BoosterPackBadge one = new BoosterPackBadge();
                for (int i = 0; i < n; i++) yield return one;  // 原版 `Enumerable.Repeat(element, count)`
            }
        }

        // ---------------------------------------------------------- ⑧ 发奖批次（账 `其他_基础设施:414`）
        /// <summary>原版 `PlayerDataManager.GrantItemsCrate(List&lt;string&gt; itemsGranted)` 的等价物（`PlayerDataManager.cs:10000-10038`，**真体**；
        /// 交叉核对 `decomp_full/PlayerDataManager__GrantItemsCrate.c`）。
        /// <para>🔴 **逐句照抄的三个事实**（都是原文读出来的，⛔ 别按方法名猜）：
        /// ① 形参 `itemsGranted` **在方法体里一次都没被读**（`.c` 里只出现 `this`；ILSpy 那份也没有）—— 原版的「发奖」其实不在这里；
        /// ② 返回值**恒为空 `List`**（`new List&lt;ObtainableItemReward&gt;()` 建完直接 return）；
        /// ③ 真正做的两件事 = 记两个计数（`inventoryOpsThisSession++` / `lastInventoryOperationTime = CurrentMillis()`）
        /// ＋ 在「无待卖卡 ∧ 不在卖 ∧ 无冷却」时起一次 `SellSpareCardsRecursive()`。</para>
        /// <para>⚠️ **我们只落 ①②③ 的计数与「该卖卡了」这一跳**：`SellSpareCardsRecursive` 属收藏/卖卡域（不在本批白名单）⇒ 用
        /// <see cref="SellSpareCardsRequested"/> 事件代替（谁做卖卡谁订阅）。⚠️ 原版那句 `new Dictionary&lt;string,int&gt;()` 一建就是空
        /// ⇒ `Keys.Count == 0` 恒真 ⇒ `CustomDebug.Log("Sell All Spare")` **每次都打**（原版自己的一处死结，照抄成一条 Note）。</para>
        /// <para>⚠️ **落点差异**：原版这是 `PlayerDataManager`（`Shell/ProfileData.cs`，归 B27）的实例方法；我们落在背包层旁，
        /// B27 接手时一行转调即可。</para></summary>
        public static class PlayerInventoryOps
        {
            /// <summary>原版 `inventoryOpsThisSession`（`+0x430`）。</summary>
            public static int InventoryOpsThisSession { get; private set; }
            /// <summary>原版 `lastInventoryOperationTime`（`+0x438`，Unix 毫秒）。</summary>
            public static long LastInventoryOperationTime;
            /// <summary>原版 `Dictionary&lt;string,int&gt; pendingCardsToSell` —— 本方法**只用到 `Keys.Count`** ⇒ 我们只留条数。</summary>
            public static int PendingCardsToSell;
            /// <summary>原版 `isSellingCards`（`+0x378`）。</summary>
            public static bool IsSellingCards;
            /// <summary>原版 `waitingSellCardCooldown`（`+0x440`）。</summary>
            public static bool WaitingSellCardCooldown;

            /// <summary>原版 `SellSpareCardsRecursive()` 的起跑口（原版是 `StartCoroutine`）。</summary>
            public static event Action SellSpareCardsRequested;

            public static List<string> GrantItemsCrate(List<string> itemsGranted)
            {
                var result = new List<string>();                 // 原版 `List<ObtainableItemReward>`，**恒空**
                InventoryOpsThisSession = InventoryOpsThisSession + 1;
                LastInventoryOperationTime = NowMillis();
                InventoryManager.Note("crate-grant",
                    "`GrantItemsCrate` 收到 " + (itemsGranted != null ? itemsGranted.Count : 0) + " 个 id —— **原版体里一个都没读**（照抄：本方法只记计数 + 可能触发卖多余卡）");
                if (PendingCardsToSell == 0 && !IsSellingCards && !WaitingSellCardCooldown)
                {
                    if (SellSpareCardsRequested != null) SellSpareCardsRequested();
                }
                return result;
            }

            public static void ResetForTest()
            {
                InventoryOpsThisSession = 0; LastInventoryOperationTime = 0; PendingCardsToSell = 0;
                IsSellingCards = false; WaitingSellCardCooldown = false; SellSpareCardsRequested = null;
            }

            static long NowMillis()
            {
                return (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            }
        }

    // ============================================================ 追加区 · 2026-10-28（B89-R2b · 日常_奖励 RewardService 原语 · 账 677）
    /// <summary>原版 `RewardService` 的**本地等价物**（第八十九会话 B89-R2b）。只建「有一条明确落点」的那一个原语：
    /// `CollectCurrencyReward`（判据 = `d:/2/tools/decomp_full/RewardService__CollectCurrencyReward.c:22-44` 全文现读）。
    /// <para>⚠️ 它**不是新的生产发奖口** —— 本工程生产发奖口仍是 `Shell/DailyData.cs` 的 `Wallet.Grant`（按菜单图名记账）。
    /// 本类走**类型化货币**（<see cref="ICurrency"/>），供将来把奖励路径从「图名」迁到「货币对象」时接线（账 677 的 Energy 特判就落在这里）。</para>
    /// <para>⛔ `CollectItemReward` / `CollectSingleReward` / `Collect` **没建**：它们要 `RewardInfo` 物品层 + `Collect` 编排层，
    /// 属未建的 reward-event 家族（账 654-668 / U09 / G1），建空壳违反「⛔ 别先建空壳」⇒ 反证写在报告 `B89_R2b_日常奖励执行.md`。</para></summary>
    public static class RewardService
    {
        /// <summary>原版 `SharedEventDataManager.GetContext&lt;EnergyContext&gt;().GrantEnergy(int)` 的可注入替身（能量「授予」那一路；
        /// 与 `CurrencyExtensions.EnergyOf/SpendEnergyOf` 同一套「可注入钩子」口径）。没注入 ⇒ 出声、⛔ 不假装给了能量。</summary>
        public static Action<int> GrantEnergyOf;

        /// <summary>原版 `RewardService.CollectCurrencyReward(Currency currency, int quantity)`（判据 `RewardService__CollectCurrencyReward.c:22-44` 逐句）：
        /// ① `currency.Type == energy && GetContext&lt;EnergyContext&gt;() != null` ⇒ `EnergyContext.GrantEnergy(quantity)`；
        /// ② 否则 ⇒ `InventoryManager.Add&lt;Currency&gt;(currency, quantity)`（兜底走背包）。
        /// <para>🔴 与 `CurrencyExtensions.AddCurrency` 区分：那里 energy 走 **`SpendEnergy`（减）**，这里原版走 **`GrantEnergy`（加）**，⛔ 别混。</para></summary>
        public static void CollectCurrencyReward(ICurrency currency, int quantity)
        {
            if (currency == null)
            {
                InventoryManager.Note("rs-collectcur:null", "`CollectCurrencyReward` 收到 null 货币 ⇒ 不发（原版同一档会 NRE）");
                return;
            }
            if (currency.Type == GameCurrency.energy)
            {
                if (GrantEnergyOf != null) { GrantEnergyOf(quantity); return; }
                // 原版：energy 但 GetContext<EnergyContext>()==null ⇒ 落到下面 InventoryManager.Add 兜底（照抄）
            }
            if (InventoryManager.GetInventory<ICurrency>() == null)
            {
                InventoryManager.Note("rs-collectcur:noinv",
                    "没有登记 `Inventory<ICurrency>` ⇒ `CollectCurrencyReward(" + currency.GetID() + ", " + quantity + ")` 无处可加（原版同一档 InventoryManager.Add 也无处落）");
                return;
            }
            InventoryManager.Add<ICurrency>(currency, quantity);
        }

        // ============================================================ 追加区 · 2026-10-28（B93_D4 · 账 679 / 680 / 681）
        //
        //  原版这三个是「发奖」那一条链的**三个入口**（`RewardService.cs:853-899` + `RewardService__CollectSingleReward.c:2-189`）：
        //    `CollectItemReward(RewardInfo)`      → `CollectItemReward(reward.Item, InstanceID, Quantity)`
        //    `CollectItemReward<T>(T, string, int)` → `InventoryManager.Instance.Add<T>(item, quantity, instanceID)`
        //    `CollectSingleReward(RewardInfo)`    → ①ephemeral 过期闸 ②上报 ③Currency 判型 ④联盟奖杯 ⑤两个 Collect* 收尾
        //  ⚠️ 上一批（B89-R2b）在类注释里写过「这三个**没建**」—— **本条已建**（只建「有一条明确落点」的那几格，
        //     认不出的分支一律出声，⛔ 不假装发过）。

        /// <summary>原版 `RewardService.CollectItemReward&lt;T&gt;(T reference, string instanceID, int quantity)`（`RewardService.cs:890`）：
        /// **一句** `InventoryManager.Instance.Add&lt;T&gt;(reference, quantity, instanceID)` ——
        /// 判据 = `decomp_full/RewardService__CollectItemReward_object_.c`（`InventoryManager__Add&lt;object&gt;(inst, item, qty, instanceID, 0, 0, …)`；
        /// 那两个 `0` = `customData = null` / `overrideAmount = false`，与 <see cref="InventoryManager.Add{T}"/> 的默认值一致）。
        /// <para>⚠️ 原版这一句**没有别的闸** —— `Instance` 为 null 会直接 NRE；我们改成出声 + 返回。</para></summary>
        public static void CollectItemReward<T>(T reference, string instanceID, int quantity) where T : class
        {
            if (reference == null)
            {
                InventoryManager.Note("rs-item:null",
                    "`CollectItemReward` 收到 null 物品 ⇒ 不发（原版同一档在 `SingletonBehaviour.Instance` 上 NRE）");
                return;
            }
            if (InventoryManager.GetInventory<T>() == null)
            {
                InventoryManager.Note("rs-item:noinv",
                    "没有登记 `Inventory<" + typeof(T).Name + ">` ⇒ `CollectItemReward(" + instanceID + ", " + quantity + ")` 无处可加");
                return;
            }
            InventoryManager.Add<T>(reference, quantity, instanceID);
        }

        /// <summary>原版 `RewardService.CollectItemReward(RewardInfo reward)`（`RewardService.cs:853-888`）：
        /// `CollectItemReward(reward.Item, reward.InstanceID, reward.Quantity)`，T = **物品的运行时类型**。
        /// <para>我们的 `RewardInfo` 只有一个 id（见 `Shell/RewardInfo.cs` 的自设计说明）⇒ 按 id 认类型：
        /// 能在 `Inventory&lt;ICurrency&gt;` 里查到 ⇒ 走那一档；认不出 ⇒ **出声、不发**（⛔ 不假装发过）。</para></summary>
        public static void CollectItemReward(RewardInfo reward)
        {
            if (reward == null)
            {
                InventoryManager.Note("rs-item:rnull", "`CollectItemReward(RewardInfo)` 收到 null ⇒ 不发（原版这一档 NRE）");
                return;
            }
            CollectItemRewardById(reward.Id, reward.InstanceId, reward.Quantity);
        }

        /// <summary>`CollectItemReward(RewardInfo)` 的 id → 背包解析口（见上一条的注释）。</summary>
        static void CollectItemRewardById(string id, string instanceId, int quantity)
        {
            if (string.IsNullOrEmpty(id))
            {
                InventoryManager.Note("rs-item:nullid", "`CollectItemReward`：奖励 id 为空 ⇒ 不发（原版 `Item` 为 null 会 NRE）");
                return;
            }
            var currencies = InventoryManager.GetInventory<ICurrency>();
            var held = currencies != null ? currencies.Get(id) : null;
            if (held != null && held.Item != null) { CollectItemReward<ICurrency>(held.Item, instanceId, quantity); return; }

            string type = ItemDrawer.Spec(id, null, null).Type;
            InventoryManager.Note("rs-item:unresolved",
                "`CollectItemReward`：奖励 `" + id + "`（原版类型 " + (string.IsNullOrEmpty(type) ? "判不出" : type)
                + "）在本工程没有对应的背包解析口 ⇒ **不发**（原版走 `InventoryManager.Instance.Add<T>`，T = 物品运行时类型；"
                + "我们只登记了 `Inventory<ICurrency>` 这一份有 id 语义的解析）");
        }

        /// <summary>原版 `RewardService.CollectSingleReward(RewardInfo reward)`（`RewardService__CollectSingleReward.c:2-189`）
        /// —— **单条奖励的分派器**，五段照 `.c` 逐段：
        /// <list type="number">
        /// <item>**ephemeral 过期闸**（`.c:44-56`）：`reward.EphemeralState != null` 且
        ///   `EphemeralEndTime &lt;= ServerDateTime` ⇒ **直接 return**（过期的不发）。</item>
        /// <item>**上报**（`.c:58-60`）：`NetworkingPeer.OnMessage(...)` —— 原版是 PlayFab 上报 ⇒ 我们出声一次（P3）。</item>
        /// <item>**Currency 判型**（`.c:145-190`）：`reward.Item is Currency` ⇒ `CollectCurrencyReward`；
        ///   是 `EnergyContext` 那一档 ⇒ `GrantEnergy(quantity)`（见 <see cref="CollectCurrencyReward"/> 的 energy 支）。</item>
        /// <item>**其余**（`.c:187` 尾句 + 最后那三跳）⇒ `CollectItemReward(reward)`；`.c` 末尾还有
        ///   `reward.Item.&lt;+0x1b8&gt;(quantity)` 一跳 = `ObtainableItem.HandleOnCollect`（**Slot 8**，基类空实现 ⇒ 见 `HandleOnCollectEquivalent`）。</item>
        /// </list>
        /// <para>🔴 原版这一路**还要** `AssignEphemeralState`（限时物品）—— `.c:70-110` 在「非 Currency」那一支里；
        /// 我们的 `InventoryManager.AssignEphemeralState&lt;T&gt;` 已有，这里按 `RewardInfo.IsEphemeral` 调用。</para></summary>
        public static void CollectSingleReward(RewardInfo reward)
        {
            if (reward == null)
            {
                InventoryManager.Note("rs-single:null", "`CollectSingleReward(null)` ⇒ 不发（原版这一档 NRE）");
                return;
            }

            // ① ephemeral 过期闸 —— `RewardInfo.IsEphemeral` = `ephemeralState != null && (duration>0 || 事件引用解得出来)`；
            //    `.c` 的闸是「到期时间 <= 服务器时间 ⇒ return」。我们的 `EphemeralMs` 是**时长**（见 RewardInfo 的注释）⇒
            //    只有「时长 > 0 且已经过了这么久」才判过期；时长 0 = 无期限限制（原版同一档 `duration <= 0` 且事件为空 ⇒ IsEphemeral=false）。
            if (reward.IsEphemeral && reward.EphemeralMs > 0 && ExpiredByDuration(reward.EphemeralMs))
            {
                InventoryManager.Note("rs-single:expired",
                    "`CollectSingleReward`：奖励 `" + reward.Id + "` 的限时态已过期（时长 " + reward.EphemeralMs + " ms）⇒ **不发**");
                return;
            }

            NoteNetPeerOnce();                                              // ② 原版 PlayFab 上报（P3）

            // ③ Currency 判型
            var currencies = InventoryManager.GetInventory<ICurrency>();
            var held = currencies != null ? currencies.Get(reward.Id) : null;
            if (held != null && held.Item != null)
            {
                CollectCurrencyReward(held.Item, reward.Quantity); HandleOnCollectEquivalent(reward.Id);   // ⑥ 原版末尾统一跳（`.c:182`）：对 Currency 是空基类 ⇒ 零行为差异
                return;
            }

            // ④ 联盟奖杯 / 其余物品 —— 原版这两支是**互斥**的（`decomp_full/RewardService__CollectSingleReward.c:110-127`
            //    ＋ ILSpy `RewardService.cs` 的 IL_06ef / IL_0754）：`reward.Item is AllianceTrophyData` ⇒ 在
            //    `AlliancesManager.trophyController.trophies` 里按 id `FirstOrDefault`，命中就写存档三格
            //    （`groupScore=1 · playerScore=1 · tier=quantity`）**且不走** `CollectItemReward`；
            //    其余类型 ⇒ `CollectItemReward(item, instanceID, quantity)`。两支之后都仍落到末尾的 `item.HandleOnCollect(quantity)`（⑥，见下）。
            if (IsAllianceTrophyId(reward.Id)) AllianceTrophyModel.MarkCollected(reward.Id, reward.Quantity);
            else CollectItemRewardById(reward.Id, reward.InstanceId, reward.Quantity);

            // ⑤ 限时物品 ⇒ 挂 ephemeral 状态（原版 `.c:70-110` 的 `AssignEphemeralState`）
            if (reward.IsEphemeral && held != null && held.Item != null && reward.EphemeralMs > 0)
                InventoryManager.AssignEphemeralState<ICurrency>(held.Item, new EphemeralState(reward.EphemeralMs));

            // ⑥ 原版末尾**统一跳** `reward.Item.&lt;+0x1b8&gt;(quantity)`（`.c:182-186`）—— 即 `ObtainableItem.HandleOnCollect`
            //    （Slot 8，基类空实现）⇒ 全汇编唯一 override = `CosmeticItemAvatarBorder`（`if (AutoAssign)` 就装备）。
            //    三支（奖杯 / 其余物品 / 货币）都落到这一跳；本工程只有 id ⇒ 按边框定义表 + `aa` 等价判（见方法体）。
            HandleOnCollectEquivalent(reward.Id);
        }

        /// <summary>限时态到期判定。⚠️ **原版判的是「到某时刻」**（`EphemeralEndTime <= ServerDateTime`），
        /// 而本工程的 `RewardInfo` 只存**时长**（`CampaignData.RewardSpec.EphemeralMs`）⇒ 这里只能按「时长已过」近似。
        /// 判据缺口如实记在账 681 的差异列。</summary>
        static bool ExpiredByDuration(long ms) { return false; }   // 没有起始时刻 ⇒ 恒「未过期」（原版数据今天 89 条全 false，这条支一次都不触发）

        static bool _warnedNetPeer;
        static void NoteNetPeerOnce()
        {
            if (_warnedNetPeer) return; _warnedNetPeer = true;
            InventoryManager.Note("rs-single:peer",
                "原版 `CollectSingleReward` 第 ② 段是 `NetworkingPeer.OnMessage(...)` 上报（PlayFab）—— **P3，无后端** ⇒ 只出声、不上报");
        }

        /// <summary>自检收尾。</summary>
        public static void ResetForTest() { GrantEnergyOf = null; _warnedNetPeer = false; }

        // ============================================================ 追加区 · 2026-10-29（B97_W7 · 账 日常_奖励:680）
        /// <summary>`CollectSingleReward` 的 ④ 支要的「这件奖励是不是联盟奖杯」（原版 `reward.Item is AllianceTrophyData`，
        /// 见 `RewardService__CollectSingleReward.c:110-127` 之前那一跳类型测试 — ILSpy `RewardService.cs` 写作 `typeof(AllianceTrophyData)`）。
        /// <para>⚠️ 我们只有 id、没有物品运行时类型 ⇒ **两条证据合判**：
        /// ① 物品类型实据表 <see cref="ItemDrawer.IdTypes"/>（`Spec` 填出来的 `Type`）；
        /// ② **联盟奖杯定义表** <see cref="AllianceTrophyModel.Find"/> —— 能在这张表里查到 id，就等于「这个 id 的 SO 是 `AllianceTrophyData`」。
        /// 🔴 ② 是**必需的**：`IdTypes` 至今只有 4 条实据（`Booster Pack …` / `DT Ultramarines …`），**没有一条联盟奖杯 id**
        /// ⇒ 只靠 ① 这一支在生产里**恒不命中**（2026-10-29 `B97_W7` 现读 `Shell/ItemDrawer.cs:778-784`）。
        /// 两条都不命中 ⇒ 走 `CollectItemReward`（与原版「其它类型」同档）。</para></summary>
        static bool IsAllianceTrophyId(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            if (ItemDrawer.Spec(id, null, null).Type == "AllianceTrophyData") return true;
            return AllianceTrophyModel.Find(id) != null;
        }

        // ============================================================ 追加区 · 2026-10-29（B97_W40 · 账 日常_奖励:681 的 ⑥ 物品层）
        /// <summary>原版 `reward.Item.HandleOnCollect(quantity)`（`RewardService__CollectSingleReward.c:182-186`；
        /// 虚槽 `+0x1b8` = `ObtainableItem` **Slot 8**）的本地等价物。
        /// <para>🔴 判据（2026-10-29 `B97_W40` 三源现读，与 `B97_R9` 一致）：
        /// ① 基类 `ObtainableItem.HandleOnCollect` = **空实现**（`dump.cs:18791` `RVA 0x4B33B0` = 全局空桩 `ret 0`；
        ///   `decomp_full/ObtainableItem__HandleOnCollect.c` 体只有 `return;` —— ⚠️ 那份**首行是 `NetworkingPeer__OnMessage`**，
        ///   属已记的「文件名与内容不符」，**体照读、名不认**）；
        /// ② **全汇编唯一 override** = `CosmeticItemAvatarBorder.HandleOnCollect`（`dump.cs:18154` `RVA 0x6EDF40`，Slot 8）
        ///   = `base(...)` + `if (AutoAssign) PlayerAvatarDataManager.Instance.SetCurrentItem(this)`
        ///   （`decomp_full/CosmeticItemAvatarBorder__HandleOnCollect.c:12-23`；`AutoAssign` = `PlayerItem.autoAssign` `+0x34`
        ///   的虚 getter，`dump.cs:18833` 只有这一条、无 override）。</para>
        /// <para>⚠️ 我们只有 id、没有物品运行时类型 ⇒ 等价判据 = 「id 命中 <see cref="ProfileData.Borders"/> 且 `aa != 0`」
        /// （`aa` = 原版 `autoAssign`）⇒ 装备它（= `SetCurrentItem`）；其余 id（称号 / 卡背 / 卡 / 牌组 / 货币…）
        /// **什么都不做** = 原版空基类那一档。🔴 原版 `PlayerAvatarDataManager.Instance` 为 null 是 **NRE**；
        /// 我们取不到就不写、不抛（与 ④ 支同一口径，见账 681 的差异列）。</para></summary>
        static void HandleOnCollectEquivalent(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return;
            var list = ProfileData.Borders;
            if (list == null || list.Count == 0) return;      // 清单空 ⇒ `ProfileData.EnsureBorders` 那一处已经出过声（⛔ 不重复刷屏）
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null || b.id != itemId) continue;
                if (b.aa == 0) return;                        // `if (AutoAssign)` 为假 ⇒ 原版也不装备（⛔ 别「反正拿到了就装」）
                ProfileData.SetEquippedBorderById(itemId);    // = `PlayerAvatarDataManager.SetCurrentItem(this)`；清单里没有它会自己出声
                return;
            }
            // 走到这里 = 这件奖励不是头像边框 ⇒ 原版基类空实现 ⇒ 什么都不做（没有第二条 override）
        }
    }
}
