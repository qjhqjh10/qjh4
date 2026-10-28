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
//   ✅ **2026-10-28（B89_E9）补的八条**：`Equals(T)` / `Equals(object)` / `GetHashCode` / `WriteJson` /
//      `OnBeforeRead` / `OnBeforeWrite` + `implicit operator T` / `implicit operator string`（类尾，判据 =
//      `D:/2/tools/decomp_generic_trial/out/LoadableReference_object___*.c`，逐句照抄）。⚠️ 原版的
//      `ICustomWrite` / `IBeforeRead` / `IBeforeWrite` 三个接口我们**没有**（无 Newtonsoft）⇒ 只建方法本体、不实现接口。
//   ✅ **2026-10-28（写手 `W6`）补的一对**：`operator ==` / `operator !=`（判据 =
//      `判据/E7_泛型启动链与商店208条判据.md:29`「一对（C# 强制成对）」＋
//      `decomp_generic_trial/out/LoadableReference_object___op_Equality__101c3f0.c` / `…__op_Inequality__101cec0.c`）。
//      🔴 它让本类的 `==` 变成**比 `targetId` 字符串、且 null 安全**（与 `Equals(T)` 传 null 必 NRE 不同）
//      ⇒ 类内凡是要判「引用本身是不是 null」的地方一律改用 `ReferenceEquals`（已逐处改掉，见各处注释）。
//   ⛔ **仍然没做**：`ValidateReferences()`（原版空体）· `GetReferenceObjEditor()` / `SetReferenceObjEditor()`
//      （编辑器 + Addressables 胶水）· `implicit operator LoadableReference<T>(T|string)`
//      两支（原版有、账里没有；我们已有对应 ctor）—— 逐条记在 `报告/B89_E9_LoadableRef12条.md` §3。
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

        /// <summary>原版 `LoadableReference(string id)`（`…___.ctor__101c110.c` 全文）：`targetId = id;` **并且当场查一次资产登记表**（`reference = AssetLocator.GetAsset&lt;T&gt;(id)`）—— 我们没有 `AssetLocator`（无寻址层）⇒ 这一跳落到「首次访问 <see cref="Reference"/> 时出声」上，构造里只记 id。</summary>
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

        /// <summary>原版 `SetReferenceById(string)`（`…__SetReferenceById__101b3d0.c` 全文）：`targetId = id;` 之后**当场查一次资产登记表**（`reference = AssetLocator.GetAsset&lt;T&gt;(id)`；账上「只记 id、清空缓存」是 ILSpy 的残读）—— 我们没有 `AssetLocator` ⇒ 这一跳等价于**清掉已缓存的本体**，留给首次访问 <see cref="Reference"/> 时出声。</summary>
        public void SetReferenceById(string id)
        {
            targetId = id;
            reference = null;
        }

        /// <summary>原版 `SetReferenceByObject(T)`（`…__SetReferenceByObject__101b650.c`）：`reference = obj; targetId = obj == null ? null : obj.GetID();` —— 注意 **`obj` 为空时 `targetId` 是被【清成 null】**（不是「不动」）。</summary>
        public void SetReferenceByObject(T obj)
        {
            reference = obj;
            if (obj == null) { targetId = null; return; }   // 原版 `if (param_2 == 0) uVar1 = 0;` ⇒ 清空 targetId（2026-10-28 B89_E9 就地订正）
            if (_idOf != null)
            {
                string id = _idOf(obj);
                if (!string.IsNullOrEmpty(id)) targetId = id;
            }
        }

        /// <summary>原版 `GetReferenceObjEditor()` / `SetReferenceObjEditor(UnityEngine.Object)` 那一对：getter = `reference == null ? null : (装箱后能赋给 UnityEngine.Object ? reference : null)`（判据 = `decomp_generic_trial/out/LoadableReference___Il2CppFullySharedGenericType___GetReferenceObjEditor__101ab70.c` 全文 —— 🔴 **整个方法体没有一次 `AssetDatabase` 调用**，账 `其他:292` 旧判据要点写的「AssetDatabase」是错的，2026-10-29 `B94_V1` 据 `.c` 订正）；setter = 判型可赋给 `T` ⇒ **写 `reference`（+0x20）**、否则 `LogError`（`…___SetReferenceObjEditor__101b6c0.c`）。⚠️ 两个方法都围绕 `reference`、**不碰** `genericObjectReference`（+0x10；原版那一格只由序列化填，本仓 0 读写者）⇒ ⛔ 别把 getter 改回去（改回去 = set→get 恒 null）。</summary>
        public UnityEngine.Object GenericObjectReference
        {
            get { return (object)reference as UnityEngine.Object; }
            set { reference = value as T; if (value != null && reference == null) UnityEngine.Debug.LogError("[LoadableRef] `GenericObjectReference` 收到的对象不能赋给 `" + typeof(T).Name + "`（原版 `SetReferenceObjEditor` 同一支 `LogError`，照抄出声）"); }
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

        // ============================================================================================
        //  2026-10-28（B89_E9）—— 账 `其他/基础设施:288·289·290·296·297·303·301·304` 补的八条。
        //  判据全部 = 泛型方法体真产物 `D:/2/tools/decomp_generic_trial/out/LoadableReference_object___*.c`
        //  （G6/G7 导出的 Ghidra 伪码，逐句读过）+ 交叉 `cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs`。
        //  ⚠️ 泛型类字段偏移：`genericObjectReference` = +0x10 · `targetId` = +0x18 · `reference` = +0x20。
        // ============================================================================================

        /// <summary>原版 `Equals(LoadableReference&lt;T&gt; other)`（`…___Equals__101a8c0.c` 全文 / ILSpy `:622-651`）：
        /// <code>
        /// if (reference != null) targetId = reference.GetID();          // ⇒ 回填（本类走 TargetId getter 的同一套回填）
        /// if (other.reference != null) other.targetId = other.reference.GetID();
        /// return targetId == other.targetId;
        /// </code>
        /// 🔴 **原版这里【没有】`other == null` 判** —— 两份反编译器都显示「`other` 为空 ⇒ 抛 NullReferenceException」：
        /// Ghidra 的 `/* WARNING: Subroutine does not return */ FUN_1803f47a0();`（该地址在 `Equals(object)` 里也是
        /// `this == null` 的 NRE 帮助函数），ILSpy 直接写 `new NullReferenceException()`（`:638-642`）。
        /// ⇒ **照抄**（铁律：原版自己就这样，⛔ 别加 `if (other == null) return false;` 去「修好」它）；
        /// 施工单 `B88_R10` §1.2 第 5 行写的「if(other==null) return false」是**推的、不是读的**（反证见报告 §1）。</summary>
        protected bool Equals(LoadableRef<T> other)
        {
            string mine = TargetId;         // 原版首句：自己 reference 非空 ⇒ 先回填 targetId
            string theirs = other.TargetId; // 原版第二句：回填 other 的；other 为 null ⇒ 这一记 NRE 就是原版那一记
            return mine == theirs;
        }

        /// <summary>原版 `Equals(object obj)`（`…___Equals__101a5a0.c` 全文 / ILSpy `:653-739`）：
        /// <code>
        /// if (obj == null) return false;
        /// if (ReferenceEquals(this, obj)) return true;
        /// if (obj.GetType() != GetType()) return false;
        /// return Equals((LoadableReference&lt;T&gt;)obj);   // 原版是内联展开 + 显式类型链检查，语义同这一句
        /// </code>
        /// <para>**真调用点（原版侧）**：`AIBotsConfig.cs:272` = `whitelistAvatars.Contains(playerAvatar.uniqueId)`
        /// ⇒ `List&lt;LoadableReference&lt;PlayerAvatar&gt;&gt;.Contains` → `EqualityComparer.Default` → **这一个 object.Equals 虚调用**
        /// （我们这边还没有消费者，但它是「`Contains` 能不能工作」的唯一实现）。</para></summary>
        public override bool Equals(object obj)
        {
            if (obj == null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((LoadableRef<T>)obj);
        }

        /// <summary>原版 `GetHashCode()`（`…___GetHashCode__101aa10.c` / ILSpy `:741-774`）：
        /// <code>if (reference != null) targetId = reference.GetID(); return targetId == null ? 0 : targetId.GetHashCode();</code>
        /// <para>本 build **没有** `Dictionary` / `HashSet&lt;LoadableReference&lt;T&gt;&gt;` ⇒ 当前无消费者；
        /// 但「改了 `Equals` 就必须同时改它」，否则同 id 的两份引用会落进不同的哈希桶。</para></summary>
        public override int GetHashCode()
        {
            string id = TargetId;                       // 原版首句：先回填
            return id == null ? 0 : id.GetHashCode();
        }

        /// <summary>原版 `OnBeforeRead(JToken rawJson)`（`…___OnBeforeRead__101b150.c` / ILSpy `:795-806`）：
        /// `if (rawJson.Type == JTokenType.String) targetId = rawJson.Value&lt;string&gt;();`
        /// —— **只认「是字符串」这一档**；其它 token 类型**原样不动**（⛔ 不是「解析成对象再取 id」）。
        /// <para>🔶 自己设计（非原版）：**token 类型用 `object` 代替 Newtonsoft 的 `JToken`** —— 工程没有 Newtonsoft
        /// （`Shell/LiveOpsHandler.cs:117` 自述，`Packages/manifest.json` 无该包），而我们的最小 JSON 层里
        /// 「一个 JSON 标量」的载荷**本来就是 `object`**（`Shell/LiveOpsSharedData.cs:35`：值只可能是
        /// string / long / double / bool / null / `LiveOpsJson` / `List&lt;object&gt;`），它**没有**「标量 token」这个类型
        /// ⇒ 取 `object` 当最小等价物，语义逐句照抄。调度台 2026-10-28 裁定。</para>
        /// <para>⚠️ 原版首句 `rawJson.Type` 就解引用 ⇒ 传 null 会 NRE；这里**照抄**（⛔ 别改成静默 no-op）。</para></summary>
        public void OnBeforeRead(object rawJson)
        {
            if (rawJson == null)
                throw new NullReferenceException(
                    "[LoadableRef] OnBeforeRead(null)：原版首句 `rawJson.Type` 直接解引用 ⇒ 照抄抛 NRE（B89_E9）");
            if (rawJson is string) targetId = (string)rawJson;
        }

        /// <summary>原版 `OnBeforeWrite()`（`…___OnBeforeWrite__4b33b0.c`）：**空体**。
        /// RVA `0x4B33B0` = 全局共享空方法桩（机器码 `c2 00 00` = `ret 0`），两套反编译都只给 `{ }`。
        /// ⛔ 别照抄 `&lt;__Il2CppFullySharedGenericType&gt;` 那份的间接跳转（`0x101b2a0`，那是 IL2CPP 给**值类型实例**的蹦床；
        /// `T : IIdentifiable` 的实现全是引用类型 ⇒ 运行时一律走这一份空体）。</summary>
        public void OnBeforeWrite() { }

        /// <summary>原版 `WriteJson()`（`…___WriteJson__101bec0.c` / ILSpy `:780-793`）：
        /// `[先回填] return new JValue(targetId);` ⇒ 落盘时写出去的是**裸 id 字符串**，不是对象。
        /// <para>🔶 自己设计（非原版）：返回类型 `object` 代替 `JToken`（同上一条：无 Newtonsoft、最小 JSON 层里标量就是
        /// `object`）。`targetId` 为 null 时返回 null —— 原版 `new JValue((string)null)` 序列化出来同样是 `null`。
        /// 调度台 2026-10-28 裁定。</para></summary>
        public object WriteJson()
        {
            string id = TargetId;   // 原版首句：reference 非空 ⇒ 先回填
            return id;
        }

        /// <summary>原版 `implicit operator T(LoadableReference&lt;T&gt; instanceRef)`（`…___op_Implicit__101c840.c` / ILSpy `:203-319`）：
        /// <code>
        /// if (instanceRef == null) return null;
        /// if (instanceRef.TargetId == null) return null;      // 用 TargetId（getter 先回填），不是裸 targetId
        /// return instanceRef.Reference;                       // 有本体用本体；否则运行期按 id 走 AssetLocator，编辑期直接 null
        /// </code>
        /// <para>⚠️ 原版那一跳 `AssetLocator.GetAsset&lt;T&gt;(TargetId)` 我们没有寻址层 ⇒ 落在 <see cref="Reference"/>
        /// （**只出声 + 返回 null**，见文件头「🔶 自己设计」），等价于原版「编辑期 / 解不出」那一档。</para>
        /// <para>⚠️ 本类 `where T : class`（原版 `where T : IIdentifiable`）⇒ 若有人写 `LoadableRef&lt;string&gt;`，
        /// 这一支与下面 `operator string` 会撞成同一目标类型；本仓当前只有 `LoadableRef&lt;Currency&gt;` 与断言夹具 `LoadableRef&lt;Item&gt;`（`Editor/LoadableRefCheck.cs`）。</para></summary>
        public static implicit operator T(LoadableRef<T> instanceRef)
        {
            // ⚠️ 用 `ReferenceEquals` 而不是 `==`：本类类尾重载了 `==`（比 `targetId` 字符串），
            //    这里要的是「引用本身是不是 null」（原版 `if (instanceRef == null)` 是引用比较）。
            if (ReferenceEquals(instanceRef, null)) return null;
            if (instanceRef.TargetId == null) return null;
            return instanceRef.Reference;
        }

        /// <summary>原版 `implicit operator string(LoadableReference&lt;T&gt; instanceRef)`（`…___op_Implicit__101cb70.c` / ILSpy `:321-...`）：
        /// <code>r == null ? null : (r.TargetId == null ? null : r.targetId)</code>
        /// —— 判空用 `TargetId`（getter 会先回填），**返回的是 `targetId` 字段本身**（原版 `*(r+0x18)`）。</summary>
        public static implicit operator string(LoadableRef<T> instanceRef)
        {
            // ⚠️ 这里**必须**用 `ReferenceEquals`：本类已重载 `==`（见类尾），写 `instanceRef == null`
            //    会去比 `targetId`（空 id 的引用会被判成「等于 null」），语义完全不同。
            if (ReferenceEquals(instanceRef, null)) return null;
            string id = instanceRef.TargetId;      // 原版：先过一遍 TargetId（回填）
            return id == null ? null : instanceRef.targetId;
        }

        /// <summary>原版 `op_Equality(LoadableReference&lt;T&gt;, LoadableReference&lt;T&gt;)`
        /// （`D:/2/tools/decomp_generic_trial/out/LoadableReference_object___op_Equality__101c3f0.c` 全文）：
        /// <code>
        /// uVar1 = null;  if (a != 0) { if (a.reference != null) a.targetId = a.reference.GetID(); uVar1 = a.targetId; }
        /// uVar2 = null;  if (b != 0) { if (b.reference != null) b.targetId = b.reference.GetID(); uVar2 = b.targetId; }
        /// return String.Equals(uVar1, uVar2);
        /// </code>
        /// 🔴 **两点与 `Equals(LoadableRef&lt;T&gt;)` 不同（都是照抄，⛔ 别「统一」掉）**：
        /// ① **null 安全** —— 原版两侧都先判 `!= 0` 再取值 ⇒ `null == null` 是 **true**（`String.Equals(null,null)`），
        ///    而 `Equals(T)` 传 null 会 **NRE**；
        /// ② 比的是 **`targetId` 字符串**，不是引用 ⇒ 两份不同实例、同 id ⇒ 相等（`List&lt;…&gt;.Contains` 靠它）。
        /// ⚠️ 推论（照抄原版、不是 bug）：**`targetId` 为空的非 null 引用与 `null` 相等**。
        /// 判据来源 = 2026-10-28 `判据/E7_泛型启动链与商店208条判据.md:29`「`operator ==` / `operator !=` —— **一对**（C# 强制成对）」。</summary>
        public static bool operator ==(LoadableRef<T> x, LoadableRef<T> y)
        {
            // ⚠️ 本算子体内**必须**用 `ReferenceEquals`：写 `x == null` 会递归回本算子（无限递归 = 栈溢出）。
            string a = ReferenceEquals(x, null) ? null : x.TargetId;   // 原版首句：先从本体回填 targetId，再取值
            string b = ReferenceEquals(y, null) ? null : y.TargetId;
            return string.Equals(a, b);                                // 原版末句 `String.Equals(uVar1, uVar2)`
        }

        /// <summary>原版 `op_Inequality`（`…___op_Inequality__101cec0.c` 末句 `return cVar1 == '\0';`）——
        /// 就是 `!(==)`，⛔ 别单独写一套判据（两处写同一条规则 = 迟早不一致）。</summary>
        public static bool operator !=(LoadableRef<T> x, LoadableRef<T> y) { return !(x == y); }
    }
}
