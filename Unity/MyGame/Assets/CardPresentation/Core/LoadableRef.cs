// LoadableRef.cs —— 原版 `LoadableReference<T>` 的最小复刻（账 `收藏:247` 的前置件）。
//
// ============================ 这是什么 ============================
// 原版 `LoadableReference<T> where T : IIdentifiable`（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/LoadableReference.cs`
// 是**签名桩**、`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs` 是真体但被 Cpp2IL 打散）：
// 一个「**惰性资源引用**」—— 存三种形态，访问时按需互相回填：
//   ① `genericObjectReference`（`UnityEngine.Object`，编辑器里拖的资产）
//   ② `targetId`（字符串 id —— 存档/网络里带的那一份）
//   ③ `reference`（`T` 本体 —— 解出来的对象，缓存）
//
// ============================ 我们做到哪（如实记）============================
//   ✅ 三格字段 + `TargetId` / `Reference` 两个属性 + 三个构造 + `SetReferenceById` / `SetReferenceByObject`
//   ⛔ **没做**（都没有调用点，做了就是死代码；⛔ 别当成「已经一样」）：
//      · `ValidateReferences()` / `GetReferenceObjEditor()` / `SetReferenceObjEditor()` —— 原版是编辑器 +
//        Addressables 的胶水；我们没有 Addressables（见下一段）。
//      · `ICustomWrite` / `IBeforeRead` / `IBeforeWrite`（`WriteJson` / `OnBeforeRead` / `OnBeforeWrite`）——
//        原版那几个是 Newtonsoft 序列化钩子；我们的存档层不走 Newtonsoft（`Shell/SaveCenter` 自己写）。
//      · `implicit operator` 四支 + `==`/`!=`/`Equals`/`GetHashCode` —— 原版靠它们让 `LoadableReference<T>`
//        能直接当 `T` / `string` 用；我们没有调用点 ⇒ 先不建（建了会有隐式转换歧义风险）。
//   🔶 自己设计（非原版）：**取 id 的口注入** —— 原版 `T : IIdentifiable` 自带 `GetID()`，我们的 T 是普通 class
//      ⇒ 构造时注入 `Func<T,string> idOf`。**与 `Shell/Inventory.cs` 文件头「差异 ①」同一套做法**（同一个理由）。
//      调度台 2026-10-27 裁定（工作项 B9_W7COLL · 账 `收藏:247`）。
//   🔶 自己设计（非原版）：**`Reference` 解不出时只出声不解析** —— 原版那一支走 Addressables / 编辑器资产库；
//      我们没有寻址层 ⇒ 如实出声（⛔ 不静默 return null 让人以为「原版也这样」）。调度台 2026-10-27 裁定。
//
// ============================ 判据出处 ============================
//  · 字段三格 + 属性两支：`LoadableReference.cs`（签名桩，字段/属性名与 `[SerializeField]`/`[JsonIgnore]` 齐全）
//  · `TargetId` getter「reference 非空时先用它回填 targetId，再返回 targetId」：
//    `out_cpp2il_dev_ilspy/LoadableReference.cs` 的 `TargetId` 体（首句 `if (reference != null) … this + 24 …`，
//    +24 = 0x18 = 第三个字段之后那一格 = `targetId`）
//  · 消费方（本类为什么要有 `TargetId`）：账 `收藏:247` —— 原版 `InventoryManager.HasEnough<T>(LoadableReference<T>, int)`
//    只做一件事：拿 `item.TargetId` 转调 `GetOwnedCount<T>(string)`。3 处活调用：
//    `decomp_full/CurrencyCondition__Validate.c:14` · `CardServices__CheckForCardUpgrade.c:88` ·
//    `UpgradePanel__CheckUpgradeRequirements.c:106`。
using System;

namespace CardPresentation
{
    /// <summary>原版 `LoadableReference&lt;T&gt;` 的最小复刻 —— 惰性资源引用（id ⇄ 对象 ⇄ 资产）。
    /// <para>⚠️ 用法边界：**只当「引用 + id」的载体**，别把它当背包条目（条目是 <see cref="ItemInfo{T}"/>）。</para></summary>
    [Serializable]
    public class LoadableRef<T> where T : class
    {
        /// <summary>原版 `genericObjectReference`（`[SerializeField] [JsonIgnore]`）——
        /// 编辑器里直接拖的那份资产。运行时权威是 <see cref="TargetId"/>。</summary>
        [UnityEngine.SerializeField] protected UnityEngine.Object genericObjectReference;

        /// <summary>原版 `targetId`（`[SerializeField]`）—— 存档 / 网络里带的那一份 id。</summary>
        [UnityEngine.SerializeField] protected string targetId;

        /// <summary>原版 `reference`（`[SerializeField] [JsonIgnore]`）—— 解出来的本体（缓存）。</summary>
        [UnityEngine.SerializeField] protected T reference;

        /// <summary>🔶 自己设计（非原版）：从 T 取 id 的口（原版 `T : IIdentifiable.GetID()`）。见文件头。</summary>
        readonly Func<T, string> _idOf;

        public LoadableRef() { }

        /// <summary>原版 `LoadableReference(string id)` —— 只有 id，本体等首次访问时解。</summary>
        public LoadableRef(string id) { targetId = id; }

        /// <summary>原版 `LoadableReference(T identifiable)` —— 直接给本体（id 由它派生）。</summary>
        public LoadableRef(T identifiable) : this(identifiable, null) { }

        /// <summary>带取 id 口的那一支（我们特有，见文件头「🔶 自己设计」）。</summary>
        public LoadableRef(T identifiable, Func<T, string> idOf)
        {
            _idOf = idOf;
            SetReferenceByObject(identifiable);
        }

        /// <summary>原版 `TargetId` —— **`reference` 非空时先用它回填 `targetId`**（原版 getter 首句就是这个），
        /// 再返回 `targetId`。⚠️ 回填只在「我们能取到 id」时做（没注入 `idOf` 就保留手写的 id）。</summary>
        public string TargetId
        {
            get
            {
                if (reference != null && _idOf != null)
                {
                    string id = _idOf(reference);
                    if (!string.IsNullOrEmpty(id)) targetId = id;
                }
                return targetId;
            }
        }

        /// <summary>原版 `Reference` —— 已解出的本体（没有就 null）。
        /// 🔶 自己设计（非原版）：原版这一支会去 Addressables / 编辑器资产库**解析**；我们没有寻址层
        /// ⇒ **只出声、不假装解出来了**（⛔ 别静默 null 让人以为原版也这样）。见文件头。</summary>
        public T Reference
        {
            get
            {
                if (reference != null) return reference;
                if (!string.IsNullOrEmpty(targetId)) NoteUnresolved(targetId);
                return null;
            }
        }

        /// <summary>原版 `SetReferenceById(string)`：改 id，**清掉已缓存的本体**（否则会指着旧对象）。</summary>
        public void SetReferenceById(string id)
        {
            targetId = id;
            reference = null;
        }

        /// <summary>原版 `SetReferenceByObject(T)`：给本体，并按它回填 id。</summary>
        public void SetReferenceByObject(T obj)
        {
            reference = obj;
            if (obj == null) return;
            if (_idOf != null)
            {
                string id = _idOf(obj);
                if (!string.IsNullOrEmpty(id)) targetId = id;
            }
        }

        /// <summary>编辑器那份资产（原版 `GetReferenceObjEditor`）。没有就 null。</summary>
        public UnityEngine.Object GenericObjectReference
        {
            get { return genericObjectReference; }
            set { genericObjectReference = value; }
        }

        /// <summary>🔶 自己设计（非原版）：同一份 id 只出声一次（本项目口径：不许静默失败，但也别刷屏）。</summary>
        static readonly System.Collections.Generic.HashSet<string> _noted
            = new System.Collections.Generic.HashSet<string>();

        static void NoteUnresolved(string id)
        {
            if (!_noted.Add(id)) return;
            UnityEngine.Debug.LogWarning("[LoadableRef] 只有 id、没有本体：「" + id
                + "」——原版这一支走 Addressables 解析，我们没有寻址层 ⇒ 返回 null（⛔ 不静默）");
        }

        /// <summary>自检用：清掉「已报过」的记忆（否则第二趟看得到不警告）。</summary>
        public static void ResetNotesForTest() { _noted.Clear(); }

        public override string ToString() { return targetId ?? ""; }
    }
}
