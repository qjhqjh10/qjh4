# G7 · Ghidra 泛型共享体【全量】导出（第 2 轮）

> 会话：第八十会话（工具/取证线）· 代理 G7 · 2026-10-25
> 任务：把 G6 试导成功的路子推广到**全部 615 条**泛型类方法（G6 §七 的现成命令与名单脚本）。
> 产物：`D:/2/tools/decomp_generic_trial/out/`（G6 的 28 个 + 本轮 612 个 = **640 个 `.c`**）
> 名单：`D:/2/tools/decomp_generic_trial/wanted_generic_all.txt`（615 行）
> 索引：`D:/2/tools/decomp_generic_trial/out/_MANIFEST_generic_all.tsv`（615 行 + 表头）
> 日志：`D:/2/tools/decomp_generic_trial/g7_run.log`
> ⛔ 未改动 `D:/2/tools/decomp_full/` 任何文件（最新文件仍是 2026-09-18）· ⛔ 未删/未改 `ghidra_proj`（全程 `-readOnly`，见 §六·2 的一条 caveat）。

---

## 一、结论

### 1. 全量导出**成功**：`ok=612 / fail=3`，**一趟 14.4 秒**

```text
DECOMP DONE ok=612 fail=3
```

- 请求 **615 条**（落在 **514 个不同地址**）· **612 条出产物** · **3 条无产物**（= **2 个地址**，两个地址各是**超短 tail-call thunk**，Ghidra 工程里根本没有函数，见 §五·2）。
- **耗时 14.4 s 墙钟**（含 JVM 启动 + 打开 2.6 GB 工程 + 反编译 612 次）。G6 外推「≈60–120 s」，实测更快；**不需要拆 2–3 趟**，一趟跑完。
- 产物体量：**25,783 行**（612 个文件）。61 条 ≥100 行，131 条 ≤10 行，最大 `CollectionDisplay<object>.SetCell` **445 行**。
- **可重复性已验**：`ItemDrawer<object>$$Awake` 的 G6 产物与本轮产物**逐字节一致**（除函数名那一行以外无 diff）。

### 2. 名单口径与 G6 完全一致（**G6 §七 的脚本可直接用**）

独立重跑 G6 §七·1 的脚本，逐项复现了 G6 的数字：**615 方法 / 514 地址 / 67 个声明类型 / 39 个泛型类型定义 / 290 条 `<object>` 代表体**。

### 3. 顶行名字：**不是**「612 条都可读、只需看签名」

按 G6 的判读口径统计这 612 条产物的**首行函数名**：

| 类别 | 条数 | 占比 | 含义 |
|---|---|---|---|
| `requested`（首行就是你请求的名字） | **334** | 54.6% | 最好的一档 |
| `ICF_other_name`（首行是别人的名字） | **168** | 27.5% | **体是对的**（地址对、偏移对），名是同一地址上另一个方体的 —— ICF 折叠，**不是失败** |
| `FUN_unnamed`（首行是 `FUN_xxxxxxxx`） | **110** | 18.0% | **体是对的**；名字没建上（根因见 §六·1，**名字里含 `", "`**） |
| 无产物 | **3** | 0.5% | 见 §五·2 |

⇒ **「产物有、但首行不是你请求的名字」合计 278 条（45.4%）**。**这不是导出失败**，两种情况各有明确成因、且**都能判**（判法见 §七「给使用者的说明」）。
⚠️ 这也是本报告与「产物率 100%」之间最容易误读的地方：**产物率 612/615 = 99.5%，但「首行名字命中」只有 54.6%**。⛔ 别拿首行名字当导出成功率的判据。

---

## 二、判据（验收三条逐条对照）

### 判据 1 · 裸字段偏移能与 `dump.cs` 对上 —— ✅（抽样核过，全量不可行，见 §五·5）

**泛型类的 `dump.cs` 字段偏移全是 `// 0x0`**（IL2CppDumper 对开放泛型不落偏移），所以只能按**声明序 + 类型宽度**推：

| 类 | `dump.cs` 字段声明序 | 推算 | 产物里的偏移 | 一致 |
|---|---|---|---|---|
| `LoadableReference<T>`（`dump.cs:55488-55498`） | `Object genericObjectReference` → `string targetId` → `T reference` | 0x10 / 0x18 / 0x20（普通类，头 0x10） | `SetReferenceById` 写 `+0x18`（`param_2` = targetId）+ `+0x20`（`AssetLocator.GetAsset` 结果 = reference）· `GetReference` 读 `+0x20`（缓存命中）→ 否则读 `+0x18` | ✅ 2/2 |
| `MissionEvent<TS,TD>` | 自身只有 `bool UseSmallContainer` | 位于基类 `LiveOpsEvent<TS,TD>` 之后 → `0x28` | `get_UseSmallContainer` = `return *(byte*)(this+0x28)`（**首行名是 `PhotonPlayer__get_isLocal`**，ICF） | ✅ 形状与宽度都对 |

> ⚠️ `MissionEvent` 的 `0x28` 只能证明「对齐在基类之后、且是 1 字节」，**基类尾偏移没有独立判据**——这条属于**形状可核、具体值未坐实**。

### 判据 2 · 调用点有名字（不是 `FUN_xxxx`）—— ✅ 但**不是全部**

- 612 个产物里 **7,896 个"调用样"token，其中 `FUN_` 2,510 个 ⇒ 未命名调用点占 31.8%**。
- 未命名的那批**绝大多数是 IL2CPP 运行时 helper**（`FUN_1803f44f0` 类初始化 / `FUN_1803f47a0` 空引用抛 / `FUN_1803f3730` GC 写屏障 / `FUN_1803f17a0` 接口分派 / `thunk_FUN_18042f4a0` 泛型类型解析），**与全量 `decomp_full` 同形**，不是泛型额外损失。
- 真业务调用点带真名，例：`AssetLocator__GetAsset<object>` · `InventoryManager__HasItem<object>` · `InventoryManager__GetInventory<object>` · `SingletonBehaviour<object>__get_Instance` · `MissionData__get_PremiumItem` · `Everguild_LiveOps_ArmyStatsV3__get_LastPlayedDeck` · `CardDeck__op_Inequality` · `System_Linq_Enumerable__FirstOrDefault<object>` · `System_Runtime_CompilerServices_AsyncTaskMethodBuilder__Start<…>` · `DragAndDropItem__Initialize` · `TouchInputManager__Toggle` · `UnityEngine_Component__GetComponentInParent<object>`。
- 🔴 **调用点的泛型实参不可信**（ICF 孪生）：`System_Collections_Generic_List<AnimatorClipInfo>__get_Item` 其实是我们请求的 `List<T>.get_Item`（名来自同址第一个方体）。**方法形对、类型实参可能是别人的**。

### 判据 3 · 与 `decomp_full` 风格一致、能互相对照 —— ✅

同一套骨架（类初始化 `DAT_` 守卫 → `il2cpp_runtime_class_init` → 字段偏移 → 末尾 `WARNING: Subroutine does not return`），并且**能与 `decomp_full` 的兄弟产物直接对上**（哪怕兄弟不是泛型的）：

- `LoadableReference<object>$$GetReference` ↔ `decomp_full` 里的同名非泛型产物；
- `LiveOpsHandler<object>$$SetupEvents`（async kickoff）↔ **`decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`**（**真逻辑在这里**，见 §六·3）。

---

## 三、命令（可复跑）

### 1) 名单（与 G6 §七·1 完全相同，复跑验证 615/514/67/39/290）

```bash
cd "D:/2/tools/decomp_generic_trial" && python -I -X utf8 - <<'PY'
import json, io, os, os.path
def stub_classes(d):
    return set(os.path.splitext(f)[0] for f in os.listdir(d) if f.endswith('.cs'))
CS = stub_classes(r'd:/2/Warpforge_code/Scripts/Assembly-CSharp') | \
     stub_classes(r'd:/2/Warpforge_code/Scripts/Assembly-CSharp-firstpass')
def cls(n): return n.split('$$')[0]
def strip_generic(c):
    out, dep = [], 0
    for ch in c:
        if ch == '<': dep += 1; continue
        if ch == '>': dep -= 1; continue
        if dep == 0: out.append(ch)
    return ''.join(out)
def isgen(c): return ('<' in c) and ('<>' not in c) and ('.<' not in c)
d = json.load(io.open(r'D:/2/tools/il2cpp_out/script.json', encoding='utf-8'))
done = set()
for l in io.open(r'D:/2/tools/wanted_full.txt', encoding='utf-8'):
    p = l.rstrip('\n').split('\t')
    if len(p) == 2: done.add((int(p[0]), p[1]))
sel = [m for m in d['ScriptMethod']
       if isgen(cls(m['Name']))
       and strip_generic(cls(m['Name'])).split('.')[0] in CS
       and (m['Address'], m['Name']) not in done]
with io.open(r'D:/2/tools/decomp_generic_trial/wanted_generic_all.txt','w',encoding='utf-8',newline='\n') as f:
    for m in sel: f.write('%d\t%s\n' % (m['Address'], m['Name']))
print(len(sel), len({m['Address'] for m in sel}))
PY
# → 615 514
```

### 2) 跑（**本轮实际用的命令**；与 G6 只差：`-scriptPath` 指向本轮目录、`postScript` 用 `DecompG7.java`）

```bash
export JAVA_HOME="D:\\2\\tools\\jdk-21.0.12.1+1"
cd /d/2/tools && time cmd //c "D:\2\tools\ghidra_12.1.3_PUBLIC\support\analyzeHeadless.bat \
  D:\2\tools\ghidra_proj Warpforge -process GameAssembly.dll -noanalysis -readOnly \
  -scriptPath D:\2\tools\decomp_generic_trial \
  -postScript DecompG7.java \
    D:\2\tools\decomp_generic_trial\wanted_generic_all.txt \
    D:\2\tools\decomp_generic_trial\out" > /d/2/tools/decomp_generic_trial/g7_run.log 2>&1
# real 0m14.370s ; DECOMP DONE ok=612 fail=3
```

### 3) 🔴 为什么用 `DecompG7.java` 而不是原版 `Decomp.java`

原版把「名字里非 `[A-Za-z0-9_.-]` 的字符全换 `_`」当文件名 ⇒ **615 条里有 28 组重名会静默互相覆盖**：

| 重名 | 出现次数 |
|---|---|
| `LoadableReference<object>$$op_Implicit` | 4（4 个不同地址） |
| `LoadableReference<__Il2CppFullySharedGenericType>$$op_Implicit` | 4 |
| `ContainerBuilder<object>$$MakeContainer` | 3 |
| `LoadableReference<object>$$.ctor` / `…FullyShared…$$.ctor` | 各 3 |
| 其余 23 组 | 各 2 |

`DecompG7.java`（放在 `D:/2/tools/decomp_generic_trial/DecompG7.java`，**只改一行**：文件名追加 `__<十六进制地址>`）⇒ 产物名形如
`LiveOpsHandler_object___SetupEvents__1019390.c`。
> 建议：把这一行并回正式 `Decomp.java`（`decomp_full` 当初没有 28 组重名，所以没暴露）。

### 4) 自检办法（三条，任一不满足就别信这份产物）

1. 日志末行 `DECOMP DONE ok=612 fail=3`（`fail` 逐条对得上 §五·2 那 3 条 = 已解释）。
2. `ls out/*.c | wc -l` = **640**（= G6 28 + 本轮 612）；`find out -size 0` = **0**。
3. 用 `out/_MANIFEST_generic_all.tsv` 按方法名/地址反查产物文件名与「首行名类别」。

---

## 四、读数

### 1. 六个大户（+ 2 个 `__Il2CppFullySharedGenericType` 孪生体）

| 类 | 请求 | 产物 | 行数 | 首行=请求名 | ICF 他名 | `FUN_` 未命名 | 无产物 |
|---|---|---|---|---|---|---|---|
| **LiveOpsHandler\<object\>** | 36 | 36 | 1,869 | 30 | 6 | 0 | 0 |
| **ItemDrawer\<object\>** | 28 | 28 | 783 | 23 | 5 | 0 | 0 |
| **LoadableReference\<object\>** | 25 | 25 | 790 | 18 | 7 | 0 | 0 |
| LoadableReference\<__Il2CppFullySharedGenericType\> | 25 | 25 | 1,244 | 20 | 5 | 0 | 0 |
| **FastModeBaseEvent\<object, object\>** | 25 | 25 | 1,485 | 0 | 1 | **24** | 0 |
| **MissionEvent\<object, object\>** | 25 | 25 | 1,389 | 0 | 2 | **23** | 0 |
| **CollectionDisplay\<object\>** | 25 | 25 | 989 | 16 | 9 | 0 | 0 |
| CollectionDisplay\<__Il2CppFullySharedGenericType\> | 25 | 25 | 1,915 | 23 | 2 | 0 | 0 |
| **CollectionItem\<object\>** | 24 | 24 | 523 | 14 | 10 | 0 | 0 |
| **Inventory\<object\>** | 22 | 22 | 980 | 19 | 3 | 0 | 0 |
| BiDirectionalDictionary\<object, object\> | 22 | 22 | 676 | 0 | 4 | **18** | 0 |
| BiDirectionalDictionary\<__Il2CppFullySharedGenericType, …\> | 22 | 21 | 1,475 | 0 | 3 | 18 | **1** |

> 🔴 **`FastModeBaseEvent` / `MissionEvent` / `BiDirectionalDictionary` 这三族「首行=请求名」为 0，不是产物有问题** —— 它们的类名里带 `", "`（两个类型参数），Ghidra 拒建符号 ⇒ 首行变成 `FUN_<地址>`（FastMode **24/25**、Mission **23/25**、BiDir **18/22**；余下几条是 ICF 他名，同样 0 条能是自己的名字）。**体与其它大户一样完整**（见 §四·2 的第 3 条抽样、§六·1）。

### 2. 抽样可读率（按域各 3 条，逐条判）

图例同 G6：**✅ 全可读** = 首行名可用 + 逻辑完整 + 偏移/调用点可核 · **◐ 部分** = 有产物但**有明确缺陷**（缺陷写明）· **✗ 无**。

#### 收藏（3 条）

| # | 方法 | 行 | 判读 | 判据 / 缺陷 |
|---|---|---|---|---|
| 1 | `CollectionItem<object>.OnEnable` | 70 | **✅** | 两次 `System_Action<object>__ctor` + `System_Delegate__Combine` + 写屏障回写；两个委托目标取自泛型 rgctx `+0x40` / `+0x48`。语义 = 向事件中继的两个事件各挂一个 Action。调用点全真名 |
| 2 | `Inventory<object>.TryGetItem`（47 行版） | 47 | **✅** | 建 `<>f__AnonymousType31`（闭包 key）→ `this+0x18` 字典判空 → `System_Linq_Enumerable__FirstOrDefault<object>` → 写 `out` 形参并 `return *out != 0`。逻辑完整；匿名类型名来自 ICF 孪生（无害） |
| 3 | `CollectionDisplay<object>.SetCell` | 445 | **✅** | 本轮最大产物。可见 `as` 转换的内联形态（`+0x130` 层级深度 + 接口表 `-8+idx*8`）· `this+0x58` = `List<T>` 取 `param_3` 下标 · `DragAndDropItem__Initialize` / `TouchInputManager__Toggle` / `GetComponentInParent<object>` 全是真名。⚠️ 泛型实参被折叠成 `List<AnimatorClipInfo>`（应为 `List<T>`） |

#### LiveOps（3 条）

| # | 方法 | 行 | 判读 | 判据 / 缺陷 |
|---|---|---|---|---|
| 4 | `LiveOpsHandler<object>.SetupEvents` | 56 | **✅** | async kickoff：`AsyncTaskMethodBuilder__Create` → 拷 4 个捕获字段（带写屏障）→ `…Start<LiveOpsHandler_<SetupEvents>d__18<object>>` → `get_Task`。**真逻辑在 `decomp_full/LiveOpsHandler._SetupEvents_d__18_object___MoveNext.c`（已验证存在）** |
| 5 | `LiveOpsHandler<object>.RefreshHandler`（`d__29`） | 50 | **✅** | 同上，状态机 = `<RefreshHandler>d__29<object>`，`MoveNext` 在 `decomp_full` 里已在 |
| 6 | `LiveOpsHandler<object>.ScheduleEventPopups` | 163 | **✅** | `this+0x20` 判空抛 → `List<T>.GetEnumerator` 循环；调用点真名。可读，但需要读完整 163 行才能拟出伪代码 |

#### Addressables（3 条）

| # | 方法 | 行 | 判读 | 判据 / 缺陷 |
|---|---|---|---|---|
| 7 | `LoadableReference<object>.SetReferenceById` | 17 | **✅** | `+0x18 = id`（写屏障）→ `AssetLocator__GetAsset<object>(id, rgctx+0x30)` → `+0x20 = 结果`。**与 `dump.cs` 字段声明序 2/2 对得上**（§二·判据1） |
| 8 | `LoadableReference<object>.GetReference` | 31 | **◐** | 主体完整（先查 `+0x20` 缓存，空则用 `+0x18` 的 id 再取）。**缺陷**：开头一句 `UnityEngine_Application__get_isPlaying(0);` **返回值被丢、没有任何分支用它** —— 像是丢了一段控制流（原版常见 `if (Application.isPlaying)` 守卫）。**未查清**（§五·4） |
| 9 | `LoadableReference<object>.op_Implicit` | 66 | **✅** | `if (this != null) { if (+0x20 != null) +0x18 = FUN_1800021f0(0, type, +0x20)（写屏障）; return +0x18; }`。逻辑闭环 |

#### 任务（3 条）

| # | 方法 | 行 | 判读 | 判据 / 缺陷 |
|---|---|---|---|---|
| 10 | `MissionEvent<object,object>.GetProgressFor` | 37 | **◐** | 结构完整：建闭包 key → `handler+0x20 → +0x48` 取集合 → `FirstOrDefault`。**缺陷两处**：① 首行 `FUN_18102eb40`（名含 `", "`，§六·1）；② 签名被反成 `void`，**`FirstOrDefault` 的返回值被丢弃**（本应 `return` 它） |
| 11 | `MissionEvent<object,object>.get_IsPremium` | 36 | **◐** | 体完整：`MissionData__get_PremiumItem(this+0x18)` → `UnityEngine_Object__op_Implicit` → `InventoryManager__HasItem<object>`。**缺陷**：签名 `void`，最后那次调用（= 真正的返回值）结果被丢 ⇒ 需脑补 `return HasItem(...) != false` |
| 12 | `FastModeBaseEvent<object,object>.GetEventDeck` | 54 | **✅** | 完整且**直接可用**：虚调用 `(*this+0x768)` 取 `ArmyStatsV3` → `get_LastPlayedDeck` → `CardDeck__op_Inequality(null)` 为空则 `InventoryManager.GetInventory<object>` → `FirstOrDefault` 取默认卡组。首行名为 `FUN_`（同 §六·1） |

**合计：✅ 9 · ◐ 3 · ✗ 0 —— 12 条抽样全可读，其中 75% 无缺陷。**

### 3. 全量分布（612 条）

| 指标 | 数值 |
|---|---|
| ≥100 行的方法 | 61 条 |
| ≤10 行的方法 | 131 条 |
| 最大 | `CollectionDisplay<object>.SetCell` 445 行 |
| 第 2–6 大 | `MissionEvent.UpdateSaveGame` 318 · `CollectionDisplay.ReorderCollectionByHierarchy` 233 · `LoadableReference.SetReferenceObjEditor` 214 · `FastModeBaseEvent.GetSqlFilter` 205 · `ToggleCollectionFilter.SetupFilter` 201 |
| 未命名调用点占比 | 31.8%（2,510 / 7,896，几乎全是 IL2CPP helper） |
| 疑似「返值被反成 void」 | **78 条**（名字以 `get_`/`Get`/`TryGet`/`Is`/`Has`/`op_` 开头而签名是 `void`；例：`Points<object>.get_Id`、`Inventory<object>.get_TotalCount`、`MissionEvent.get_IsPremium`） |
| async kickoff 产物 | 5 条（`LiveOpsHandler` 的 4 个状态机；**`MoveNext` 全在 `decomp_full` 里已有**） |

---

## 五、没查清

1. 🔴 **G5 说的「276 条」复现不出来**（G6 §五·4 也没复现）。本轮把六种可能口径都算了：

   | 口径 | 条数 |
   |---|---|
   | 全量（本轮基准） | **615** |
   | 名字含 `<object>`（≈G6 说的「`<object>` 代表体」） | **290** |
   | 排除 `__Il2CppFullySharedGenericType` | **399** |
   | 排除 `.ctor`/`op_*`/`Equals`/`GetHashCode` | 389 |
   | 只算方法（排除 ctor 与属性访问器） | 411 |
   | 只取 `Assembly-CSharp`（不含 firstpass） | 615（firstpass 对这 39 个定义**零贡献**） |

   **没有一种等于 276**。⇒ **以 615 为准**；276 的原始口径**没查清**（可能是当时按 `dump.cs` 的 `// RVA: -1` 数、且只覆盖了部分类）。
2. 🔴 **3 条无产物 = 2 个地址，工程里根本没有函数**（`getFunctionAt` 与 `getFunctionContaining` **都为 null**）：

   | 地址 | 挂的名字 | 实况 |
   |---|---|---|
   | `0x1366510` | `ToggleCollectionFilter<object, int>$$<Filter>b__7_2` + `…<object, Int32Enum>$$` 同址 | 28 字节 thunk：`test rdx,rdx / jne / xor al,al / ret`，否则 `mov r8,[rcx]; … jmp rax`（尾调用） |
   | `0x15ca730` | `BiDirectionalDictionary<__Il2CppFullySharedGenericType, …>$$System.Collections.IEnumerable.GetEnumerator` | 22 字节 thunk：`[rdx+0x20]→[+0xc0]→[+0x128]→ jmp rax` |

   字节是**真代码**（`disasm_va.py` 读出来是完整指令流，紧跟 `int3` 填充、下一个函数起点就在 32 字节后），只是 **Ghidra 没把它识别成函数**（没有序言、`-noanalysis` 下也不会补）。⇒ **不是反编译失败**。
   **怎么补**：① 直接读指令流（已读，结论在上表）；② **兄弟实例有产物**：`0x1366530`（`__Il2CppFullySharedGenericType` 版 `b__7_2`，已导出 201 行里的一个）与 `0x15ca750`（`object, object` 版 `IEnumerable.GetEnumerator`，已导出，body 完整可读）。**未尝试**「先 `disassemble` + `createFunction` 再导」——那要写 Ghidra 工程，本轮是 `-readOnly`。
3. `CollectionDisplay<object>.SetCell` 的泛型实参被折成 `List<AnimatorClipInfo>`：**方法形确定**，但**具体 `T` 只能从上下文推**，没有逐条核。
4. `LoadableReference<object>.GetReference` 开头 `Application.get_isPlaying` **返回值被丢** —— 是 Ghidra 丢了控制流、还是原版就调了不用，**没查清**（要 `disasm_va.py` 读该函数头部）。
5. **未逐条核 612 条的字段偏移** —— 泛型类的 `dump.cs` 偏移全是 `// 0x0`，每类都要按声明序手推；本轮只按 §二·判据1 核了 `LoadableReference<T>`（2/2 命中）+ `MissionEvent`（形状与宽度命中）。**其它 37 个类型定义的偏移未坐实**。
6. G6 §五 的 3 条（`ToggleClaimedWarning` 语义 / 5 个 IL2CPP helper 真名 / 虚调用槽命名）**本轮未跟进**，仍是开口。

---

## 六、顺手发现

1. 🔴🔴 **「产物首行是 `FUN_xxxx`」的根因 = 名字里有 `", "`（逗号+空格），Ghidra 拒建符号**（`Renamer.java` 把异常吞掉了）。**精确取证**：
   - 本轮 110 条 `FUN_` 产物 **110/110 名字含 `", "`**；反过来，含 `", "` 的 127 条里 **110 条是 `FUN_`、14 条是 ICF 他名、3 条无产物 —— 「首行 = 它自己的名字」的 0 条**（真正的判据：**名字含空格 ⇒ 一定建不上符号**）。
   - 反证：`wanted_full.txt` 里「**有逗号但没空格**」的名字 3 条（`KeyValuePair<System.String,System.String>` 那类）**3/3 都正常命名** ⇒ **就是空格**，不是逗号。
   - 🔴 **这个坑不止泛型**：`wanted_full.txt` 里名字含 `", "` 的有 **449 条**（其中 444 条在 `decomp_full/` 有产物），随机抽 120 个 → **84 个（70%）首行是 `FUN_`**（另 34 个因为同址另有不含空格的名字被 Ghidra 选中）。⇒ **`decomp_full` 里估计约 314 个文件的「名字」本就是缺的**（这是既有状况，不是本轮引入）。
   - ✅ **不影响可读性**，但**影响「按首行名认方法」的所有做法**：本轮的 `_MANIFEST_generic_all.tsv` 就是为此而写（首行名 + 类别 + 请求名 + 地址四列对齐）。
2. ⚠️ **`-readOnly` 不是「零字节写入」**：程序库（`Warpforge.rep/00/`）确实没动（仍是 9/17），但 **4 个索引文件被重写**：`idata/~index.dat`、`idata/~index.bak`、`versioned/~index.dat`、`versioned/~index.bak`（mtime 本轮运行时刻）。⇒ 说「工程一个字节没被写」**不准确**；准确说法是「**程序数据库没被写，索引文件会重写**」。
3. 🔴 **泛型 async 方法的产品只是「状态机 kickoff」** —— 真逻辑在状态机的 `MoveNext` 里，而 `d__NN` 是**嵌套类（名字含 `<>`）⇒ 不在 615 条里**，但它们**本来就在 `decomp_full`**（因为嵌套类型的方法有真 RVA）。本轮 5 条 kickoff 的 4 个状态机 `d__18/d__26/d__27/d__29`（`<object>` 版）**全部已在 `decomp_full`**。⇒ 读泛型 async 方法要**两处一起看**。
4. **调用点的泛型实参可能来自 ICF 孪生**（`System_Collections_Generic_List<AnimatorClipInfo>__get_Item` 实为 `List<T>.get_Item`）：**方法形可信、类型实参不可信**。
5. **一个地址最多挂 6,789 个方法名**（`0x4b3000`，全是空 `.ctor` / `op_Implicit` 折叠）—— 所以 `.ctor` 类产物的 8 行体可能是别人类别的构造函数。本轮 615 条里 **38 个地址内部重名、涉 139 条**；G6 说的「187 条（30%）共享地址」= **地址在 `all_methods.txt` 里挂 ≥2 个名字的条数**（含与非泛型方体共享），与本轮的 139（**严格在 615 内部**共享）是两个口径，都对。
6. **文件名必须带地址**：28 组重名会被原版 `Decomp.java` 静默覆盖（§三·3）。这条建议并回正式脚本。
7. **`ItemDrawer<object>` 28 条本轮重复导了一次**（在 `out/` 里以 `__<addr>` 结尾并存）：与 G6 的 28 个**内容逐字节一致** ⇒ 既证明可重复，也说明**以后重跑不必担心产物漂移**。

---

## 七、给使用者的说明（下一个会话怎么用这份产物）

### 1. 去哪找

| 想找什么 | 去哪 |
|---|---|
| 某个泛型类方法的 body | `D:/2/tools/decomp_generic_trial/out/<净化名>__<十六进制地址>.c`，例：`LiveOpsHandler_object___SetupEvents__1019390.c` |
| **不知道文件名**（最常见） | 查索引 **`out/_MANIFEST_generic_all.tsv`**：`addr ⇥ requested_name ⇥ file ⇥ topline_name ⇥ name_class`，按 `requested_name` grep 一行即可 |
| 非泛型方法 | 仍是 `D:/2/tools/decomp_full/<净化名>.c`（25,096 个） |
| 泛型 async 的真逻辑 | `D:/2/tools/decomp_full/<Type>._<Method>_d__NN_<实参>___MoveNext.c` |
| 这个地址还挂着哪些名字（ICF 取证） | `D:/2/tools/all_methods.txt` 里 grep 该地址的十进制值 |
| 某条指令流（产物丢控制流时） | `"D:/2/Warpforge_tools/py312/python.exe" d:/4/Unity/工具/disasm_va.py D:/2/unity_run_ref/GameAssembly.dll <VA> [条数]`。⚠️ **VA = `0x180000000` + RVA**，例：RVA `0x1366510` ⇒ VA **`0x181366510`**（九个十六进制位）。写成 `0x1801366510`（多打一个 `0`）会报 `NOT MAPPED` —— 本轮踩过 |

### 2. 怎么确认这份产物可信（按 `name_class` 五步）

| `name_class` | 可信度 | 怎么用 |
|---|---|---|
| **`requested`**（334 条） | 最高 | 直接用。仍建议抽一眼调用点里有没有夸张的 `FUN_` |
| **`ICF_other_name`**（168 条） | **体可信，名不可信** | ① 确认**地址**是你要的（索引里就在该行）；② `all_methods.txt` 里该地址**确实挂着你的名字**；③ 优先读 **`__Il2CppFullySharedGenericType` ↔ `<object>` 孪生**（两者代码通常完全一样）；④ 一字段 getter 那类（名像 `PhotonPlayer__get_isLocal`）**几乎必然是折叠**，别被名字带跑 |
| **`FUN_unnamed`**（110 条） | **体可信，名字压根没建** | 名字含 `", "`（§六·1）。**以文件名为准**（文件名里的名字是你请求的、地址也是），别因为首行 `FUN_` 就判「没导出来」 |
| **`MISSING`**（3 条） | 无产物 | 先读 §五·2 那张表；再读**同族的兄弟实例**（`0x1366530` / `0x15ca750`）；都不行再用 `disasm_va.py` |
| 任何一条 | —— | **验字段**：泛型类的 `dump.cs` 偏移是 `// 0x0`，**必须按字段声明序 + 宽度自己推**（`LoadableReference` 的 0x18/0x20 是本轮核过的样板）。**验调用点**：方法名可信、**泛型实参不可信**（ICF） |

### 3. 三条「别踩」的口径

1. ⛔ **别用「首行名 == 请求名」当导出成功率**（真实值 54.6%，而产物率 99.5%）。**判据永远是 `fail=`**。
2. ⛔ **别把「ICF 名是别人」当缺陷去改** —— 那是 IL2CPP 的相同代码折叠，**改不了**（也不该改）。
3. ⛔ **别把 `void` 签名当「这个方法没返回值」** —— 本轮 78 条是「最后一步调用的返值被丢」，把它们当 void 读会**丢掉整个方法的语义**（`MissionEvent.get_IsPremium` 就是典型：真正的返回值是最后那句 `InventoryManager.HasItem`）。
