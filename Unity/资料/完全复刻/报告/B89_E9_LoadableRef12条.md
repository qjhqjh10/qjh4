# B89 · E9 —— R10「批次 B1：`LoadableReference<T>` 12 条」（账 `其他/基础设施` L288 / L289 / L290 / L294 / L295 / L296 / L297 / L298 / L299 / L301 / L303 / L304）

执行代理：`B89_E9`（2026-10-28）· 白名单文件：`CardPresentation/Core/LoadableRef.cs`（只追加 + 就地改注释/一行语义）·
新建 `CardPresentation/Editor/LoadableRefCheck.cs`（只放断言）· 账补丁 `账/_parts/B89_E9.tsv`

> **判据主源**：`D:/2/tools/decomp_generic_trial/out/LoadableReference_object___*.c`（640 个泛型方法体真产物，2026-10-25 就位）
> ＋交叉 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/LoadableReference.cs`。
> 泛型类字段偏移：`genericObjectReference` = +0x10 · `targetId` = +0x18 · `reference` = +0x20。
> 🔴 **本批没跑 Unity**（红线）⇒ §2 的断言**写好了但没人跑过**。

---

## §1 逐条结论

⚠️ **先说行号口径**：施工单 `B88_R10` §1.2 的「账行」列与 `.tsv` 的**物理行号错位**（它把 L288 写成 `.ctor(T instance)`，而物理 L288 是
`Equals(LoadableReference<T> other)`）。**12 行的集合是对的**（288/289/290/294/295/296/297/298/299/301/303/304），下表按**方法名**对齐（见 §3·①）。

| 账行 | 原版方法（判据 = `.c` 行号） | 我们改了什么（符号名 + **现读**行号） | 状态建议 | 说明 |
|---:|---|---|---|---|
| **288** | `Equals(LoadableReference<T> other)`<br>`…___Equals__101a8c0.c:1-34` · ILSpy `:622-651` | 新增 `protected bool Equals(LoadableRef<T> other)` —— `LoadableRef.cs:160` | **A** | 两边各自「`reference` 非空 ⇒ `targetId = reference.GetID()`」回填后比 `targetId`（回填走 `TargetId` getter = 原版首句同一套）。<br>🔴 **原版【没有】`other == null` 判** ⇒ 传 null 抛 NRE（Ghidra `FUN_1803f47a0()`，ILSpy `:638-642` 直接 `new NullReferenceException()`）⇒ **照抄**，⛔ 没按施工单加 `return false`（反证见下表 §1·a）。 |
| **289** | `Equals(object obj)`<br>`…___Equals__101a5a0.c:1-62` · ILSpy `:653-739` | 新增 `public override bool Equals(object obj)` —— `:177` | **A** | `null ⇒ false` / `ReferenceEquals(this,obj) ⇒ true` / `GetType()` 不同 ⇒ `false` / 否则转 `Equals((LoadableRef<T>)obj)`。原版中间把 `Equals(T)` 内联展开 + 显式类型链检查，语义同。 |
| **290** | `GetHashCode()`<br>`…___GetHashCode__101aa10.c:1-39` · ILSpy `:741-774` | 新增 `public override int GetHashCode()` —— `:189` | **A** | `[回填] return targetId == null ? 0 : targetId.GetHashCode();` |
| **294** | `LoadableReference<T>(T instance)`<br>`…___.ctor__101c040.c:1-24` · ILSpy `:101-124` | 既有 `public LoadableRef(T identifiable)` —— `:63`（**本轮只补注释/核账**） | **A** | `reference = 入参` · `targetId = 入参 == null ? null : 入参.GetID()`；`if (param_2 == 0) uVar1 = 0;` ⇒ null 入参 targetId 也是 null（我方走 `SetReferenceByObject`，见 L299 已对齐）。取 id 走注入的 `Func<T,string>`（🔶 调度台 2026-10-27 裁定，⛔ 没改）。 |
| **295** | `LoadableReference<T>(string id)`<br>`…___.ctor__101c110.c:1-19` · ILSpy `:79-100` | 既有 `public LoadableRef(string id)` —— `:60`（本轮补注释写清那一跳） | **A**（差一跳，见右） | 原版构造时**立刻** `reference = AssetLocator.GetAsset<T>(id)`；我们没有 `AssetLocator`（无寻址层）⇒ 只记 id，解析落在首次访问 `Reference`（出声 + null）⇒ **可观察时机不同、值相同**。 |
| **296** | `OnBeforeRead(JToken rawJson)`<br>`…___OnBeforeRead__101b150.c:1-24` · ILSpy `:795-806` | 新增 `public void OnBeforeRead(object rawJson)` —— `:204` | **B** | 语义照抄：`if (rawJson.Type == String) targetId = rawJson.Value<string>();`；其它 token 原样不动；null ⇒ NRE 照抄。<br>🔶 **token 类型用 `object` 代替 Newtonsoft `JToken`**（工程无 Newtonsoft → 见 §1·b）。**还差接线**：原版唯一消费者 `SerializationHelper` 我们整个没有 ⇒ 0 调用点。 |
| **297** | `OnBeforeWrite()`<br>`…___OnBeforeWrite__4b33b0.c:1-7`（RVA `0x4B33B0` = 全局共享空桩 `ret 0`）· ILSpy `:776-778` | 新增 `public void OnBeforeWrite() { }` —— `:216` | **B** | **空体**已建（逐字等价）；⛔ 没照抄 `<__Il2CppFullySharedGenericType>` 那份的间接跳转（`0x101b2a0` = IL2CPP 给**值类型实例**的蹦床；`T : IIdentifiable` 的实现全是引用类型 ⇒ 运行时走 `<object>` 这一份空体）。同上：0 调用点。 |
| **298** | `SetReferenceById(string id)`<br>`…___SetReferenceById__101b3d0.c:1-17` · ILSpy `:125-136` | 既有 `public void SetReferenceById(string id)` —— `:101`（行为未改，补注释） | **A** | 原版是「记 id **+ 当场查登记表**」；账上「只记 id、清空缓存」是 ILSpy 残读。我们没有登记表 ⇒ `reference = null` 等价于「查不出来」那一档。 |
| **299** | `SetReferenceByObject(T reference)`<br>`…___SetReferenceByObject__101b650.c:1-23` · ILSpy `:137-160` | **就地订正** `public void SetReferenceByObject(T obj)` —— `:108` | **A** | 🔴 **原版 `if (param_2 == 0) uVar1 = 0;` ⇒ 传 null 时 `targetId` 被【清成 null】**；我们 2026-10-28 之前是 `if (obj == null) return;`（= 不动）⇒ **已改成 `targetId = null; return;`**（**同一行内改，零行数变化**）。 |
| **301** | `implicit operator T(LoadableReference<T>)`<br>`…___op_Implicit__101c840.c:1-66` · ILSpy `:203-319` | 新增 `public static implicit operator T(LoadableRef<T>)` —— `:239` | **A** | `r==null ⇒ null` · `r.TargetId==null ⇒ null` · 否则 `r.Reference`（原版 `Reference` getter：有本体用本体，否则运行期走 `AssetLocator`、编辑期 null —— `…__get_Reference__101c220.c`）。我方 AssetLocator 那一跳落在我方 `Reference`（出声 + null）。 |
| **303** | `WriteJson()`<br>`…___WriteJson__101bec0.c:1-26` · ILSpy `:780-793` | 新增 `public object WriteJson()` —— `:223` | **B** | `[回填] return new JValue(targetId);` ⇒ 写出**裸 id 字符串**。🔶 返回类型 `object`（同 L296 理由）；`targetId == null ⇒ null`（原版 `new JValue(null)` 序列化出来同为 `null`）。0 调用点。 |
| **304** | `implicit operator string(LoadableReference<T>)`<br>`…___op_Implicit__101cb70.c:1-54` · ILSpy `:321-401` | 新增 `public static implicit operator string(LoadableRef<T>)` —— `:249` | **A** | `r==null ? null : (r.TargetId==null ? null : r.targetId)` —— 判空过 `TargetId`（先回填），**返回 `targetId` 字段本身**（原版 `*(r+0x18)`）。 |

**合计：A 9 · B 3**（3 个 B 全是「本体已照抄、缺序列化层接线」，接线属存档线，不属本批白名单）。

### §1·a 🔴 一处「判据与 `.c` 冲突」——按 `.c` 实现，并给反证（铁律 11 的处理手法）

施工单 `B88_R10` §1.2 第 5 行写：`if(reference!=null) targetId=reference.GetID(); **if(other==null) return false;** if(other.reference!=null) …`。

**`other == null ⇒ return false` 这一句不成立**。三份独立证据：

1. Ghidra `LoadableReference_object___Equals__101a8c0.c:18-33`：
   `if (param_2 != 0) { …; System_String__Equals(…); return; }` + `/* WARNING: Subroutine does not return */ FUN_1803f47a0();`（`:32`）
2. 同一个 `FUN_1803f47a0` 在 `…___Equals__101a5a0.c:15-18` 里承担的是 `this == null` 的**空引用异常帮助函数**
   （`uVar3 = System_Object__GetType(param_1,0);` 之前那一记判空）⇒ 它就是 `il2cpp_codegen_raise_null_reference_exception` 一类。
3. ILSpy `LoadableReference.cs:638-642` 把这一支反编译成 `NullReferenceException ex = new NullReferenceException(); return (byte)(int)ex != 0;`
   —— 「`new NullReferenceException()` + 返回值是垃圾」正是 ILSpy 对**抛异常路径**的固定渲染（`return false` 会被写成 `return false`）。

⇒ 实现取「`other` 为 null ⇒ NRE」（`other.TargetId` 自然解引用），并在 `LoadableRefCheck.cs` 里**钉死**这一条
（`Equals(T) 传 null ⇒ NullReferenceException`），防止后来者「好心修好」。
⚠️ 我没有拒做——本条只影响 null 这一档，方法其余部分与施工单逐字一致。

### §1·b 为什么 token 类型是 `object` 而不是 `LiveOpsJson`

- 调度台裁定方案 ②「建在我们自己的最小 token 类型上」，并点名 `Shell/LiveOpsSharedData.cs` 的 `LiveOpsJson`；同时给了逃生口：
  「若需要能力它没有，在 `LoadableRef.cs` 里就地做最小包装并把缺口写进报告」。
- **缺口是真的**：`LiveOpsJson`（`:37`）是**对象容器**（`Dictionary<string,object>` + 保序表），**没有「标量 token」这个类型**；
  它的值域里「一个标量」的载荷**本来就是 `object`**（`:35` 注释原文：值只可能是 `string`/`long`/`double`/`bool`/`null`/`LiveOpsJson`/`List<object>`）。
  而原版这两个钩子要的正是「一个裸字符串 token」。
- ⇒ 用 **`object`** 当最小等价物（不新造类型）：`OnBeforeRead(object rawJson)` 里 `if (rawJson is string) targetId = (string)rawJson;`、
  `WriteJson()` 返回 `targetId`（= 原版 `new JValue(targetId)` 序列化出来的那个裸字符串）。两处都打了
  `🔶 自己设计（替代 Newtonsoft JToken）` 标记 + 「调度台 2026-10-28 裁定」。
- `Shell/LiveOpsSharedData.cs` **一个字节没动**（不归本代理改）。

---

## §2 验证

### §2·a typecheck 读数

`TMPDIR=/tmp/wf_B89E9 bash d:/4/Unity/工具/typecheck.sh`：

| 第几次 | 运行时 | 编辑器 | 说明 |
|---|---:|---:|---|
| 1 | **1** | (级联 1) | 错误全在 `Assets/RuleEngine/Core/UnitState.cs(1548,47)`：`RecomputeActAfterAuraKeyword` 不存在 —— **不在我的白名单**（RuleEngine 归 E1–E7），当时正是别人写到一半 |
| 2 | **2** | (级联 1) | 错误全在 `Assets/CardPresentation/Battle/BattleDriver.cs(331,39)` `RegisterPlayerAction` / `(14515,13)` `ResetTurnActionCounter` —— **E5 的文件**，同因 |
| 3 | **0** | **0** | ✅ 全仓干净（含我的 `LoadableRef.cs` 与新建的 `Editor/LoadableRefCheck.cs`） |

> 判据照铁律 13·3：错误**全部集中在我白名单之外**的文件上 ⇒ 那是别人的活，**我没去改**。
> 第 3 次 0/0 说明：我的两个文件编得过，且那一刻全仓没有别的半成品。

### §2·b 拟好的断言（`CardPresentation/Editor/LoadableRefCheck.cs`，**36 条 Check**，⚠️ **没跑过**）

入口 = `LoadableRefCheck.Run()`（`public static void Run()`，全局命名空间，`-executeMethod` 可直接调）。
日志前缀 `LOADREF`，末行 `LOADREF === 合计：N 通过 / M 失败 ===`，批处理下 `Application.Quit(_fail == 0 ? 0 : 1)`。

| 组 | 断言（要点） |
|---|---|
| 前提 | `protected bool Equals(LoadableRef<T>)` 存在且签名是**泛型那一支**（不是 object 重载）—— 没有它，下面所有 Equals 断言都是**空转**（`if(x!=null) Check(...)` 那种写法本仓吃过 3 次亏，这里改成「先硬检查 + 早退」） |
| 构造 | `(T)` ⇒ TargetId 由对象回填 · Reference 就是传进来那一份 · `(T=null)` ⇒ TargetId 为 null · `(string)` ⇒ 只记 id、Reference 为 null（**会打一条预期警告**） |
| setter | `SetReferenceById` ⇒ id 改 + 旧本体清 · `SetReferenceByObject(obj)` ⇒ 回填 id · **`SetReferenceByObject(null)` ⇒ TargetId 被清成 null**（照抄原版的订正项） |
| `Equals(T)` | 同 id 不同实例 ⇒ true（**同时挡「改成比引用」**）· id 不同 ⇒ false · 一方只带 id、一方只带本体 ⇒ true（回填生效）· **传 null ⇒ NullReferenceException** |
| `Equals(object)` | null ⇒ false · 同实例 ⇒ true · 同 id ⇒ true · id 不同 ⇒ false · 类型不同（string）⇒ false · 泛型实参不同（`LoadableRef<object>`）⇒ false |
| `GetHashCode` | 同 id 同哈希 · id 为 null ⇒ 0 · 等于 `"id".GetHashCode()` |
| `implicit string` | 引用 null ⇒ null · TargetId null ⇒ null · 有 id ⇒ id |
| `implicit T` | 引用 null ⇒ null · TargetId null ⇒ null · 有本体 ⇒ `ReferenceEquals` 那一份 |
| 序列化钩子 | `WriteJson` ⇒ 裸 id 字符串（含「只带 id、没有本体」那一档）· `OnBeforeRead(string)` ⇒ 取该串 · `OnBeforeRead(long)` ⇒ **原样不动** · `OnBeforeRead(null)` ⇒ NRE · `OnBeforeWrite` ⇒ 无副作用 |

---

## §3 顺手发现（⛔ 我没有改）

1. **施工单 §1.2 的「账行」列与 `.tsv` 物理行号错位**：它把 L288 记成 `.ctor(T instance)`，实际物理 L288 = `Equals(LoadableReference<T> other)`
   （`python -I -X utf8` 现读 `账/其他_基础设施.tsv` 第 288–304 行逐条对过）。**12 行的集合没变**，我按**方法名**落账。
   建议调度台在施工单上加一句行号口径说明，免得下一个代理照行号抄错行。
2. **主表两条 ctor 的「判据出处」行号是错的**：`LoadableReference.cs:402` 其实是 `implicit operator LoadableReference<T>(T instance)`、
   `:468` 是 `implicit operator LoadableReference<T>(string id)`；**真 ctor 在 ILSpy `:101` / `:79`**。账补丁里我写的是正确行号。
3. **原版还有两支 `implicit operator LoadableReference<T>`**（`…___op_Implicit__101c9d0.c` = 由 `T` 造、`…__101c680.c` = 由 `string` 造），
   **账里没有对应行** ⇒ 本批没建（我们已有等价的 ctor `LoadableRef(T)` / `LoadableRef(string)`）。建议账里补两行或注明「不算缺口」。
4. **主表 L298 的「我们的落点」写 `Core/CardArt.cs（按名取图，等价的单向路）`，与 B87-A7 的结论（「本笔不动、归 B32」）并存** ——
   同一条账里两个落点口径。本批按「统一类型这一层」落在 `LoadableRef` 本体，`CardArt` 那一层**没碰**；合并时请调度台统一这一列。
5. **`Shell` 侧还有两处「原版实现 `IBeforeRead`、我们没搬」**：`Shell/MissionEvent.cs:112`（`MissionPersistence`）、
   `Shell/PlayEvent.cs:145`（`LastOpponentInfo`），理由都写「没有 Newtonsoft」。现在 `LoadableRef` 已经用 `object` 当 token 了
   ⇒ 那两处**可以按同一套口径补上**（`Shell/**` 不归本批，⛔ 没动）。
6. **`Equals(object)` 的原版真调用点**在**原版侧**：`AIBotsConfig.cs:272` = `whitelistAvatars.Contains(playerAvatar.uniqueId)`
   （`List<LoadableReference<PlayerAvatar>>.Contains` → `EqualityComparer` → `object.Equals` 虚调用）。我们仓当前 **0 调用点** ⇒ 这条属「配套正确性」。
7. **`LoadableRef.cs:42`（类声明行）没漂移** —— `账/收藏.tsv:247` 的落点 `CardPresentation/Core/LoadableRef.cs:42` **仍然成立**
   （本轮所有新增都追加在类尾，文件头改动是**同层数就地替换**）。可复核：`b.count(b'\n')` 142 → 256，但第 42 行内容未变。

---

## §4 没查清（⛔ 不是「查不到」，是「这一轮没查到」）

1. **`implicit operator T` 里 `Application.isPlaying` 那一跳的确切位置**：Ghidra（`…__op_Implicit__101c840.c:49`）与 ILSpy（`:291`）
   **都把 `get_isPlaying` 的返回值丢掉了**（反编译噪声）。我按「`…__get_Reference__101c220.c:17` 里同一句 `Application__get_isPlaying`
   紧挨着 `if (lVar3 == 0) { if (targetId != 0) AssetLocator.GetAsset(…) }`（`:18-26`）」推：**它在 `Reference` getter 内**（`!isPlaying ⇒ 编辑期不加载`），
   `op_Implicit` 只是 `return instanceRef.Reference`。**这是推的，不是直读** —— 影响面：只是「编辑期」那一档，运行期两支等价。
2. **`_MANIFEST_generic_all.tsv` 的 ICF 行没逐条核**：本批用到的 12 个 `.c` 里，`OnBeforeWrite__4b33b0.c` 的首行是 `NetworkingPeer__OnMessage`、
   `ValidateReferences__4b33b0.c` 同址 —— 按 R10 §1.1 的说明那是「链接器把相同机器码折叠」，**体是空体（对的）**。
   我没去核「`0x4B33B0` 一共被多少个方法共用」这件事（R10 §六·7 已把它查清成「全局空桩 `ret 0`」，我采信）。
3. **同一实例自比的边缘**：`Equals(other)` 里我先取 `mine = TargetId` 再取 `theirs = other.TargetId`；若 `this`/`other` 是**同一个对象**
   且注入的 `_idOf` 每次返回不同值（非确定性取 id），两者可能不等。原版的 `GetID()` 是稳定属性 ⇒ 该风险**只存在于我们自己注入的口**，
   当前注入方（`Inventory` 线）都是字段直取。**没查清**：将来若有「每次都变」的 `_idOf`，这里要加一条断言。

---

## §5 交接

1. 🔴 **调度台要加一条 `-executeMethod` 入口**：`LoadableRefCheck.Run`
   （文件 `MyGame/Assets/CardPresentation/Editor/LoadableRefCheck.cs`，全局命名空间静态类，签名 `public static void Run()`）。
   建议在 `d:/4/Unity/工具/_run_8_checks.sh` 里加（**串行**，同其它宿主）：
   ```
   unset ELECTRON_RUN_AS_NODE && "$UNITY" -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
     -executeMethod LoadableRefCheck.Run -logFile -
   ```
   判绿红看 `LOADREF === 合计：N 通过 / M 失败 ===`，**不看退出码**（同仓约定）。
   ⚠️ **它一次都没跑过**（本批红线禁止跑 Unity）⇒ 第一次跑若红，先看是不是我写的期望值错了（尤其 §1·a 的 null ⇒ NRE 那一条）。
2. **账补丁**：`d:/4/Unity/资料/完全复刻/账/_parts/B89_E9.tsv`（**12 行 · 11 列 · LF**，已过 `check_patch_conflicts.py`：
   「✅ 没有跨补丁重复键（4 份补丁）」）。合并时 `--only B89_E9`，合并完请把它移到 `账/_parts/_merged_B89_E9/`（防旧内容回灌）。
   状态：**A 9 / B 3**；3 个 B 的备注里都写清了「还差接线 = 本工程没有 `SerializationHelper`」。
3. **两处语义改动要转告**（都在白名单内、都零行数变化）：
   - `SetReferenceByObject(null)` 现在会**清 `targetId`**（原版口径，之前是「不动」）—— 若哪天有人靠「传 null 不清 id」的行为，会看到变化（本仓今天 0 调用点）。
   - `LoadableRef.cs` 文件头「没做」那段已就地改写成「2026-10-28 补了八条 / 仍然没做的四类」，**同层数**，`:42` 未漂。
4. **还没做、且不在本批**：`ValidateReferences()` / `GetReferenceObjEditor()` / `SetReferenceObjEditor()`（账里已判 D）· `==`/`!=`
   · 两支 `implicit operator LoadableReference<T>`（账里没有行，见 §3·③）。
