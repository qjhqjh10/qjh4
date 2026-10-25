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
// ⚠️ `EphemeralState`：原版是枚举（限时物品的到期态）；我们存 `int`（判据只需「这一格存在且能读写」）。
using System;
using System.Collections.Generic;

namespace CardPresentation
{
    // ============================================================ 条目
    /// <summary>一个物品条目（原版 `IItemInfo<T>` / `ItemInfo<T>`）。
    /// 字段/方法对位：`Item`(= `LoadableRef<T> Item`) · `Amount` · `InstanceId` · `EphemeralState`
    /// + `SetInfo` / `Add` / `SetAmount` / `AssignEphemeralState`。</summary>
    public sealed class ItemInfo<T> where T : class
    {
        /// <summary>物品本体（原版是 `LoadableReference<T>`；我们直接持有 —— 见文件头「差异 ②」）。</summary>
        public T Item { get; private set; }
        /// <summary>数量。**这是「拥有数」的正本**（⛔ 别在别处再存第二份）。</summary>
        public int Amount { get; private set; }
        /// <summary>实例 id（原版 `IItemInfo.instanceId`；同一物品可有多份**不同实例**时用它区分）。
        /// 空串 = 合并到「该物品那一条」。</summary>
        public string InstanceId { get; private set; }
        /// <summary>临时态（原版 `EphemeralState`：限时物品的到期信息）。我们用 `int`，语义待用到时再定。</summary>
        public int EphemeralState { get; private set; }

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
        public void Add(int amount, string instanceId = null)
        {
            Amount = Math.Max(0, Amount + amount);
            if (instanceId != null) InstanceId = instanceId;
        }

        /// <summary>原版 `ItemInfo.SetAmount(int)`：直接设数量。</summary>
        public void SetAmount(int amount) { Amount = Math.Max(0, amount); }

        /// <summary>原版 `ItemInfo.AssignEphemeralState(EphemeralState)`：标注临时态。</summary>
        public void AssignEphemeralState(int state) { EphemeralState = state; }

        public override string ToString() { return (Item != null ? Item.ToString() : "<null>") + " ×" + Amount; }
    }

    // ============================================================ 背包
    /// <summary>一份背包（原版 `Inventory<T>`）：`id → 条目` + 变更事件 + 子集。
    /// ⚠️ **它不是解锁闸**（见文件头口径）：只做数量/实例/限购的正本。</summary>
    public sealed class Inventory<T> where T : class
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
        /// `autoCreate = true` 且还没有时**建一条数量 0 的**（原版就是这样）。</summary>
        public ItemInfo<T> Get(T item, bool autoCreate = false)
        {
            if (item == null) return null;
            string id = _idOf(item);
            if (string.IsNullOrEmpty(id)) return null;
            ItemInfo<T> info;
            if (_byId.TryGetValue(id, out info)) return info;
            return autoCreate ? Create(item) : null;
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

        /// <summary>原版 `Inventory.GetItemInstances(bool useQuantity = true)`：展开成「实例序列」
        /// —— `useQuantity = true` 时数量 &gt; 1 的那一条**重复出现 N 次**（原版就这么用，例如奖励逐一飞出）。</summary>
        public IEnumerable<ItemInfo<T>> GetItemInstances(bool useQuantity = true)
        {
            foreach (var kv in _byId)
            {
                var info = kv.Value;
                if (info == null) continue;
                int n = useQuantity ? Math.Max(1, info.Amount) : 1;
                for (int i = 0; i < n; i++) yield return info;
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
        public void OnItemCountModify(T item, int amount)
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

        /// <summary>原版 `Inventory.OnItemCountModifyInternal(IIdentifiable, int)`：内部处理
        /// —— 更新字典（上面已做）→ 通知子集 → 抛 <see cref="OnItemCountChange"/>。</summary>
        void OnItemCountModifyInternal(T item, int amountChanged)
        {
            ItemInfo<T> info;
            if (item != null)
            {
                string id = _idOf(item);
                if (!string.IsNullOrEmpty(id) && _byId.TryGetValue(id, out info)) NotifySubsets(info);
            }
            if (OnItemCountChange != null) OnItemCountChange(item, amountChanged);
        }

        void NotifySubsets(ItemInfo<T> info)
        {
            for (int i = 0; i < _subsets.Count; i++) _subsets[i].OnSourceChanged(info);
        }

        /// <summary>原版 `Inventory.UpdateItemCollection()`：把「实例列表」重建成 `id → 条目` 字典。
        /// ⚠️ 我们是**字典即正本**（没有另一份实例列表）⇒ 本方法**没有活可干**；
        /// 留着它是为了让「原版的调用点」有一个同名落点（⛔ 别把调用点改成空语句）。
        /// 📌 将来若真的引入「实例列表 + 重建」那一套（原版那样），**改这一个方法**即可，调用点不动。</summary>
        public void UpdateItemCollection() { }

        /// <summary>原版 `Inventory.RegisterSubset(Type, InventorySubset)`：登记一个子集视图。</summary>
        public void RegisterSubset(InventorySubset<T> set)
        {
            if (set == null || _subsets.Contains(set)) return;
            _subsets.Add(set);
            set.Rebind(this);
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
    public sealed class InventorySubset<T> where T : class
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

        /// <summary>**卡牌的背包**（原版 `Inventory&lt;RawCardScript&gt;`；见 `判据/泛型与启动链.md` §二）。
        /// <para>⚠️ **今天还没接消费点**：收藏页那一格「xN」现在读的是 `CardProgress.Owned(id, rarity)`
        /// （那一份在 `Shell/CardDetailPopup.cs` 里，**不在本代理白名单**）⇒ 迁过来要改那个文件。
        /// 本背包先建好、并按「全解锁」口径**播种**（见 <see cref="SeedCardsAllUnlocked"/>），
        /// 等那份被划进来时**只需把 `CardsCounterText` 的读口换成 `Inventories.Cards.Get(def)`**（一行）。</para></summary>
        public static readonly Inventory<RuleEngine.CardDef> Cards =
            new Inventory<RuleEngine.CardDef>(c => c != null ? c.Id : null);

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
        }

        /// <summary>自检用：把注册过的背包清空（**不落盘**）。</summary>
        public static void ClearForTest()
        {
            Shop.ClearForTest();
            Cards.ClearForTest();
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
}
