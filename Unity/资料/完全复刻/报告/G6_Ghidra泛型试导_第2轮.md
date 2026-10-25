# G6 · Ghidra 泛型共享体试导（第 2 轮）

> 会话：第八十会话（工具/取证线）· 代理 G6 · 2026-10-25
> 任务：验证 G5 的猜想「**Ghidra 能导出泛型共享体的可读结果，只是当初没导出**」。**只试导一个类**。
> 目标类：**`ItemDrawer<object>`（28 个方法）**。
> 产物：`D:/2/tools/decomp_generic_trial/out/`（新目录，28 个 `.c` · 783 行）
> ⛔ 未改动 `D:/2/tools/decomp_full/` 任何文件；⛔ 未删/未改 `D:/2/tools/ghidra_proj/`（本轮全程 `-readOnly`）。

---

## 一、结论

### 1. 试导**成功**，可以推广

- `DECOMP DONE ok=28 fail=0` —— **28 个方法 28 个 `.c`，没有一条是空壳或签名桩**。
- 可读性：**22 条全可读 / 6 条部分 / 0 条无**（逐条见 §四）。6 条「部分」全部有**明确、已知的成因**（ICF 折叠改名 / 局部返值被反成 void / 1 条语义可疑），**没有一条是「导出失败」**。
- 判据「**裸字段偏移能与 `dump.cs` 对上**」是**逐条命中**的（11 个 `ItemDrawerComponents` 字段偏移 + 6 个 `ItemDrawer<T>` 自身字段偏移，全部与 `dump.cs` 一致，见 §二）。

### 2. 🔴 G5 给的那份「证据」**归错类了**（铁律 5：就地订正）

`D:/2/tools/decomp_full/ItemDrawer__Release.c`（32 行）**不是泛型类的产物**：

- 它对应**非泛型** `ItemDrawer` 的 **`static void Release(ItemDrawer drawer)`**（`dump.cs` 的 `RVA: 0x7BA090`）：内容 = 查 `drawerReferences` 字典（`FUN_181776150`）+ `ComponentReference<object>__Release`，与该签名逐句吻合。
- 而 **`ItemDrawer<T>` 根本没有 `Release` 方法**（`dump.cs:93374-93633` 的方法表里没有；`Release` 只属于非泛型基类 `ItemDrawer`）。
- ⇒ 这条证据证明的是「**非泛型类早有产物**」，**不能**用来证明「泛型共享体能出」。

**但结论本身仍然成立** —— 本轮找到两条更硬的证据（下面 §3）。

### 3. 真正坐实猜想的两条硬证（本轮新找到）

| # | 证据 | 出处 |
|---|---|---|
| ① | **已有产物里已经在按名字调用共享体** ⇒ Ghidra 早就把共享体当成函数并起了名 | `decomp_full/CurrencyDrawer__Draw.c`：`lVar1 = ItemDrawer<object>__get_image(param_1,DAT_184256f80);`　·　`decomp_full/AvatarDisplay__Awake.c:16`：`ItemDrawer<object>__Awake(param_1,DAT_184257a80);` |
| ② | **全量那次的名单里根本没请求它** | `D:/2/tools/wanted_full.txt`（26,282 行）里 `grep -c "ItemDrawer<"` = **0** |

⇒ **「泛型共享体反不出来」不成立**；**「当初没导出」成立**，而且**根因是名单生成，不是 Ghidra 的能力**：
`dump.cs` 对泛型类的**方法本体**写的是 `// RVA: -1 Offset: -1`，真实地址藏在紧随其后的 `/* GenericInstMethod : |-RVA: 0x… */` 子块里。
按「`// RVA:` 为非负」筛方法体的名单生成法会**静默丢掉全部泛型类方法**。

### 4. 成本极低（实测，非估算）

**28 个方法 = 7.7 秒**（含 JVM 启动 + 打开 2.6 GB 工程 + 反编译 28 个函数；连跑三次一致，产物确被重写，见 §三）。

---

## 二、判据（验收三条逐条对照）

验收标准要求「每一个方法都可读逻辑」且满足其中**至少两条**。三条本轮**全部**满足：

### 判据 1 · 裸字段偏移，且能与 `dump.cs` 字段序对上 —— ✅ 17/17 命中

**A. 经 `components` 读的 11 个偏移**（对照 `dump.cs:93635-93660` 的 `ItemDrawerComponents` 字段表）

| 方法 | 产物里的偏移 | `dump.cs` 字段 | 一致 |
|---|---|---|---|
| `get_background` | `+0x20` | `background; // 0x20` | ✅ |
| `get_image` | `+0x28` | `image; // 0x28` | ✅ |
| `get_label` | `+0x30` | `label; // 0x30` | ✅ |
| `get_quantity` | `+0x38` | `quantity; // 0x38` | ✅ |
| `get_premiumHighlight` | `+0x40` | `premiumHighlight; // 0x40` | ✅ |
| `get_premiumBadge` | `+0x48` | `premiumBadge; // 0x48` | ✅ |
| `get_claimedWarning` | `+0x50` | `claimedWarning; // 0x50` | ✅ |
| `SetConvertedItem` | `+0x58` / `+0x60` / `+0x68` | `conversionDisplay` / `convertedItem` / `convertedLabel` | ✅✅✅ |
| `SetEphemeralDisplay` | `+0x70` / `+0x78` | `ephemeralDisplay` / `ephemeralText` | ✅✅ |

**B. 自身与基类字段的 6 个偏移**（`dump.cs` 对 `ItemDrawer<T>` 的字段全写 `// 0x0`，但**基类与声明序**仍可定）

| 产物里的写法 | 语义 | 判据 |
|---|---|---|
| `get_Components` → `+0x30` | `components` | 与 `ItemDrawer<T>` 字段声明序第 1 位 + 基类 `options` 占 `0x28` 自洽 |
| `Draw`/`OnPointerClick` 的 `param_1[7]` = `+0x38` | `item` | 同上，第 2 位 |
| `Draw` 的 `*(undefined4*)(param_1+8)` = `+0x40` | `amount` | int，8 字节对齐后落 `0x40` |
| `.ctor` 的 `*(param_1+0x44)=1`；`ToggleShowInfoOnClick` 写 `+0x44` | `showItemInfoOnClick` | bool，紧跟 int |
| `OnPointerClick` 的 `param_1[9]` = `+0x48` → `AudioCue__Play2D` | `inspectSound` | 类型吻合 |
| `OnPointerClick`/`OverrideOnClickAction` 的 `param_1[10]` = `+0x50` | `onClick` | 且 `OnPointerClick` 里走 `(**(code**)(lVar1+0x18))(...)` 调用委托 |
| `Draw`/`Refresh` 的 `param_1[5]` = `+0x28` | 基类 `options` | `dump.cs:93289`：`protected ItemDrawerOptions options; // 0x28` ✅ |

### 判据 2 · 调用点有名字（不是 `FUN_xxxx`）—— ✅

产物里绝大多数调用点带真名：
`UnityEngine_Object__op_Equality` · `UnityEngine_Component__GetComponent<object>` · `UnityEngine_GameObject__SetActive` ·
`UnityEngine_UI_Image__set_sprite` · `PriceDisplay__Setup` · `SupportMethods__GetTimeString` · `System_String__Replace` ·
`EphemeralState__get_HasExpiration` / `__get_EphemeralStartTime` / `__get_EphemeralEndTime` ·
`I2_Loc_LocalizationManager__GetTranslation` · `System_Int32__ToString` · `UpgradeCardSignal___ctor` ·
`WindowsManager__OpenWindow<object>` · `SingletonBehaviour<object>__get_Instance` · `AudioCue__Play2D` · `CustomDebug__LogError` · `System_Type__GetTypeFromHandle`。

剩下的 `FUN_1803f44f0` / `FUN_1803f47a0` / `FUN_1803f3730` / `FUN_1801eaf30` / `FUN_1803f17a0` 是 **IL2CPP 运行时 helper**（类初始化 / 空引用抛 / GC 写屏障 / 接口分派），`resolve_va.py` 反查不到（它们不是托管方法，本就不在 `script.json` 里）——**这是全量 `decomp_full` 里一模一样的形态，不是泛型的额外损失**（同一族在 `ItemDrawer__Release.c` 里也出现）。

### 判据 3 · 与既有 `ItemDrawer__Release.c` 风格一致、能互相对照 —— ✅

同一套骨架：`if (DAT_1845xxxxxx == '\0') { FUN_1803f44f0(&...); … }` 类初始化 → `if (*(int*)(DAT_xxx+0xe4)==0) il2cpp_runtime_class_init();` → 字段偏移读写 → 末尾 `/* WARNING: Subroutine does not return */ FUN_1803f47a0();`。
新增的一条差异（泛型特有、且**可当作「这是共享体」的识别特征**）：**多一个隐藏的尾参 `param_N` = `MethodInfo`**，方法体里用
`*(longlong*)(*(longlong*)(param_N+0x20)+0xc0)+0xNN` 取**泛型实参类型令牌**。这与 `CLAUDE.md` / `全量反编译_入口与用法.md` 里「泛型类的 `r8` 常是隐藏的泛型 `MethodInfo`」完全吻合。

---

## 三、命令（可复跑）

### 1) 生成名单（从 `script.json` 取，**不要从 `dump.cs` 的 `// RVA:` 取**）

```python
# python -I -X utf8
import json, io
d = json.load(io.open(r'D:/2/tools/il2cpp_out/script.json', encoding='utf-8'))
want = [m for m in d['ScriptMethod'] if m['Name'].startswith('ItemDrawer<object>$$')]
with io.open(r'D:/2/tools/decomp_generic_trial/wanted_itemdrawer_object.txt','w',encoding='utf-8',newline='\n') as f:
    for m in want: f.write('%d\t%s\n' % (m['Address'], m['Name']))
print(len(want))   # 28
```

格式 = 「**十进制地址 ⇥ 名字**」（`Decomp.java` 吃这个）；28 条里地址跨度 `0x4c68b0 … 0x1aeab80`。

### 2) 跑（**复用工程 + `-noanalysis` + `-readOnly`**）

```bash
export JAVA_HOME="D:\\2\\tools\\jdk-21.0.12.1+1"     # 不设会报 The 'java' command could not be found
cd /d/2/tools
cmd //c "D:\2\tools\ghidra_12.1.3_PUBLIC\support\analyzeHeadless.bat D:\2\tools\ghidra_proj Warpforge \
  -process GameAssembly.dll -noanalysis -readOnly \
  -scriptPath D:\2\Warpforge_tools\data\decomp_il2cpp_0827\ghidra_scripts \
  -postScript Decomp.java \
    D:\2\tools\decomp_generic_trial\wanted_itemdrawer_object.txt \
    D:\2\tools\decomp_generic_trial\out"
```

- 🔴 **加了 `-readOnly`**（本轮新验）：日志里出现 `REPORT: Processing read-only project file: /GameAssembly.dll` ⇒ **2.6 GB 工程一个字节没被写**。全量那次没加，所以以后重跑建议一律加。
- 🔴 **不需要先跑 `Renamer.java`**：全量那次已经用 `all_methods.txt` 把 294,942 个地址全打过标签/建过函数，泛型共享体**在其中**（所以产物第一行直接就是 `ItemDrawer<object>__Awake` 这种真名）。
- 脚本路径里**没有**「文件级白名单」概念：`Decomp.java` 只吃列表 + 输出目录，不会碰 `decomp_full/`。

### 3) 自检办法

- 日志末行必须是 `Decomp.java> DECOMP DONE ok=N fail=0`。**`fail>0` 才有问题**（那说明 `fm.getFunctionAt()` 为 null = 那个地址不是函数起点）。
- 产物第一行的函数名**不等于**列表里的名字 ⇒ 该地址**与别的方体共用（ICF 折叠）**，不是导出失败（判法见 §四·注 1）。
- 抽样验偏移：与 `dump.cs` 的字段表逐格比（本轮 17/17 命中）。

---

## 四、读数（逐方法）

图例：**✅ 全可读** = 真名 + 完整逻辑 + 偏移可核 · **◐ 部分** = 有产物但**有明确缺陷**（缺陷写在备注） · **✗ 无** = 无产物。

| # | 方法 | 行数 | 判读 | 关键读数 |
|---|---|---|---|---|
| 1 | `get_Components` | 25 | ✅ | `+0x30` 空则 `GetComponent<ItemDrawerComponents>()` 并回写（带写屏障） |
| 2 | `get_background` | 15 | ✅ | `+0x20` |
| 3 | `get_image` | 15 | ✅ | `+0x28` |
| 4 | `get_label` | 15 | ✅ | `+0x30` |
| 5 | `get_quantity` | 15 | ✅ | `+0x38` |
| 6 | `get_premiumHighlight` | 15 | ✅ | `+0x40` |
| 7 | `get_premiumBadge` | 15 | ✅ | `+0x48` |
| 8 | `get_claimedWarning` | 15 | ✅ | `+0x50` |
| 9 | `get_Item` | 7 | ◐ | 体正确（`return +0x38`）；**签名被反成 `PhotonLagSimulationGui__get_Peer`**（注 1） |
| 10 | `get_ObtainableItemTypeToDraw` | 14 | ◐ | 体是 `Type.GetTypeFromHandle(rgctx+0x18)`，但**反成 `void`、返值被丢**（注 2） |
| 11 | `get_Amount` | 7 | ◐ | 体正确（`return []+0x40`）；签名被反成 `AlternateArtCard__get_Style`（注 1） |
| 12 | `Awake` | 15 | ✅ | 两次虚调用（`[klass+0x1d8]` / `[klass+0x1e8]`，实参均为 `0`）+ 空引用保护；Ghidra 报「Could not recover jumptable / Treating indirect jump as call」= 它把 **尾调用 `jmp [rax+0x1e8]`** 当成跳表了，指令本身合法（已用 `disasm_va.py` 现验） |
| 13 | `Refresh` | 13 | ✅ | `vtable[0x188](this, item, amount, options, …)` ⇒ 就是 `Draw(item, amount, options)` 的转发 |
| 14 | `Draw` | 155 | ✅ | 完整：读 `options`→`Transform`、`item` 回写（带写屏障）、`amount`、`SetActive(true)`、`Int32.ToString` 写数量、`IsNullOrEmpty` 选择显隐，末尾 `vtable[0x288]` |
| 15 | `SetItemName` | 23 | ✅ | 经 `components+0x30`（label）写文本 |
| 16 | `SetConvertedItem` | 95 | ✅ | 完整：`conversionDisplay.SetActive` + `PriceDisplay.Setup` + 接口分派查虚表（`+0x12e`/`+0xb0`）取属性 + `I2_Loc…GetTranslation` 写回 `convertedLabel` |
| 17 | `SetEphemeralDisplay` | 69 | ✅ | 完整：`HasExpiration` 分支 → `EndTime-StartTime` → `SupportMethods.GetTimeString` → 两次 `String.Replace` → 写 `ephemeralText` |
| 18 | `ToggleClaimedWarning` | 30 | ◐ | **语义可疑**（注 3）：机器码**不读 toggle**、尾调用 `Object.op_Implicit(claimedWarning)` 后直接返回 |
| 19 | `OverridePremiumBadge` | 42 | ✅ | 判空后 `Image.set_sprite(premiumBadge, sprite)` |
| 20 | `TogglePremiumHighlight` | 42 | ✅ | 判空后 `premiumHighlight.SetActive(toggle)` |
| 21 | `ToggleShowInfoOnClick` | 8 | ✅ | `*(bool*)(this+0x44) = toggle` |
| 22 | `OverrideOnClickAction` | 9 | ◐ | 体正确（写 `+0x50` + 写屏障）；签名被反成 `PlayerHand__set_currentHand`（注 1） |
| 23 | `GetItemName` | 14 | ◐ | 体是 1 次虚调用（`[klass+0x1a8]`）；签名被反成 `MissionsTab.<>c__<RefreshRefillCount>b__9_0`（注 1） |
| 24 | `ToggleHighlight` | 14 | ✅ | 1 次虚调用（`[klass+0x1b8]`） |
| 25 | `ToggleLock` | 14 | ✅ | 1 次虚调用（`[klass+0x1a8]`） |
| 26 | `OnPointerClick` | 38 | ✅ | `+0x44` 开关 → `item` 判空（空则 `CustomDebug.LogError`）→ `onClick` 有则调、无则走 `vtable[0x258]` → `inspectSound.Play2D` |
| 27 | `HandlePointerClick` | 28 | ✅ | `UpgradeCardSignal..ctor(item, amount)` → `SingletonBehaviour<WindowsManager>.Instance.OpenWindow(signal)` |
| 28 | `.ctor` | 16 | ✅ | `showItemInfoOnClick = true` 后调基类 ctor（基类 ctor 名被反成 `PhotonHandler___ctor`，同注 1 现象，不影响判读） |

**合计：✅ 22 · ◐ 6 · ✗ 0（产物率 28/28 = 100%，全可读 78.6%）**

**注 1 · 签名名字被换成别的方法** = **IL2CPP 相同代码折叠（ICF）**，`全量反编译_入口与用法.md` §四·3 已记为已知现象。
取证：这 4 条**全部落在「一字段 getter/setter」的折叠簇**上 —— 现读 `all_methods.txt`：
`0x508e70` 一个地址挂着 **277 个**方法（`ItemDrawer<object>$$get_Item` / `ItemDrawerComponents$$get_Quantity` / 一堆 `<>f__AnonymousType…$$get_*`）· `0x519230` 挂着 **123 个**（`PlayerHand$$set_currentHand` / `ItemDrawer<object>$$OverrideOnClickAction` …）· `0x4c68b0` 挂着 **44 个** · `0x6e6e80` 挂着 **8 个**（`CardbackDrawer/PremiumDrawer/ExpansionPassPremiumDrawer$$GetItemName` + `ItemDrawer<object>$$GetItemName`）。
**体是对的**（返回值偏移一一吻合），错的只是「第一行那个名字」。
**注 2** · `dump.cs` 里 `dump.cs:93374` 的 `get_ObtainableItemTypeToDraw` 确实声明为 `Type`；产物写成 `void` 并丢返值 —— 属 `全量反编译_入口与用法.md` 记的「**本该返数值却反成 void**」那一族，**语义仍可从一行代码补全**（`Type.GetTypeFromHandle(rgctx[0x18])`）。
**注 3** · `ToggleClaimedWarning` —— 已用 `disasm_va.py 0x181AEA940` 复核**机器码就是这样**：`mov rbx,[rax+0x50]`（claimedWarning）→ `xor edx,edx` → `jmp 0x182ffb890`（`UnityEngine.Object.op_Implicit` 尾调用），**全程没读 `dl`（= 传入的 toggle）**。同族 `TogglePremiumHighlight`（`0x181AEAA30`）则有 `movzx edi,dl` + `test al,al` + `SetActive(…,param_2)`。⇒ 要么该方法体在原版里真就如此、要么 `script.json` 把地址归到了别的方体上 —— **见 §五「没查清」**。

---

## 五、没查清

1. **`ToggleClaimedWarning` 的语义**（上注 3）：机器码层面**确凿**（不读 toggle、没有 `SetActive`），但**解释不通**（`void ToggleClaimedWarning(bool)` 不可能不带 toggle 用）。**没查清**是「方法体本来如此」还是「`script.json` 地址归属错」。下一步可查：`all_methods.txt` 里 `0x1AEA940` 是否还挂着别的名字；以及谁调用它。
2. **5 个 IL2CPP 运行时 helper 的真名**：`FUN_1801eaf30`（疑似 `castclass/isinst`，第 1 参 = 对象、第 2 参 = 类型令牌）、`FUN_1803f44f0`（类初始化）、`FUN_1803f47a0`（空引用抛）、`FUN_1803f3730`（GC 写屏障）、`FUN_1803f17a0`（接口分派，与文档「钥匙 1」对应）。`resolve_va.py` 全部「没有方法从这个地址开始」—— 它们**不是托管方法**，所以 `script.json` 里没有。**本轮没有从别处坐实它们**（可从 `il2cpp` 库符号或 IL2CPP 源码比对）。
3. **虚调用槽没有逐条命名**：`0x188 / 0x1a8 / 0x1b8 / 0x1d8 / 0x1e8 / 0x278 / 0x288`。按 `全量反编译_入口与用法.md`「钥匙 2」（`(0xNNN − 0x138)/0x10`）能换算成槽号，但**本类的 vtable 基址未核实**，所以本轮**没有**把它们写成方法名（避免猜）。这是**读法问题、不是产物缺陷**。
4. **「276 条」与我量到的数对不上**：我按「声明类型是泛型 且 泛型类型定义在 `Assembly-CSharp`(+firstpass) 的桩目录里有同名类」量到 **615 条 / 67 个声明类型 / 39 个泛型类型定义**，其中 **`<object>` 代表体 290 条**。可能是口径差（276 ≈ 只算某个域，或只算 `<object>` 且剔除若干类）。**没查清 276 的原始口径**，见 §七 用我量的口径。

---

## 六、顺手发现

1. 🔴 **G5 的证据归错类**（`ItemDrawer__Release.c` 是非泛型静态方法）—— 已在 §一·2 写死，建议同步进调度台日志，避免下一轮继续引用它当「泛型能出」的证据。
2. 🔴 **「泛型类没产物」的根因是名单生成，不是 Ghidra**：`dump.cs` 对泛型类方法体写 `// RVA: -1`，真地址只在 `GenericInstMethod` 子块。**任何按 `// RVA:` 非负来筛的名单法都会静默丢掉全部泛型类方法** —— 这与 2026-09-18 那次「只提顶层类名、漏掉 29% 嵌套类型」是**同一类错误**（名单口径 bug），建议并进 `资料/全量反编译_入口与用法.md` §四·坑列表。
3. **缺口实测（可直接当下一批的输入）**：游戏侧泛型类方法 **615 条**，`ItemDrawer<object>` 只占 28 条；大户 = `LiveOpsHandler<object>` 36 · `ItemDrawer<object>` 28 · `LoadableReference<object>` 25 · `FastModeBaseEvent<object,object>` 25 · `MissionEvent<object,object>` 25 · `CollectionDisplay<object>` 25 · `CollectionItem<object>` 24 · `Inventory<object>` 22 · `BiDirectionalDictionary<…>` 22。
   615 条落在 **514 个不同地址**上 ⇒ **187 条（30%）与其他方法共用地址**，那部分**必然**出现注 1 的「签名名字是别人」。**这不是失败，是 ICF**。
4. **`ItemDrawer<T>` 的隐藏泛型参数字符串**：产物里 `param_N`（最后一个形参）就是 `MethodInfo`，方法体用 `…+0xc0)+0xNN` 取泛型实参类型令牌 —— 与 `CLAUDE.md`「泛型类的 `r8` 常是隐藏的泛型 `MethodInfo`」一致，**又一处印证**。
5. **`-readOnly` 可用**：本轮三次运行全部加 `-readOnly`，Ghidra 日志明确 `Processing read-only project file`，**2.6 GB 工程零写入**。以后任何「只导出、不改工程」的活都该加它。
6. **`Refresh` 的语义**：`vtable[0x188](this, item, amount, options, …)` ⇒ `Refresh()` 就是**按当前 `item/amount/options` 重画**。这对复刻「收藏/奖励格子刷新」是个直接可用的判据。

---

## 七、推广到「276 条」怎么做

> 口径说明：你说的 276 我没能复现（§五·4）。下面按**我量到的口径**写成可调参数 —— 把 `PREFIXES` 换成你要的集合即可，命令不变。

### 1) 名单（一次生成，按「泛型类型定义 × 实参」全收）

```python
# python -I -X utf8  →  落 D:/2/tools/decomp_generic_trial/wanted_generic_all.txt
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
    if len(p) == 2: done.add((int(p[0]), p[1]))          # 全量那批已导出的
sel = [m for m in d['ScriptMethod']
       if isgen(cls(m['Name']))
       and strip_generic(cls(m['Name'])).split('.')[0] in CS
       and (m['Address'], m['Name']) not in done]
with io.open(r'D:/2/tools/decomp_generic_trial/wanted_generic_all.txt','w',encoding='utf-8',newline='\n') as f:
    for m in sel: f.write('%d\t%s\n' % (m['Address'], m['Name']))
print(len(sel), 'methods ·', len({m['Address'] for m in sel}), 'distinct addrs')
# → 615 methods · 514 distinct addrs
```

**只要 `<object>` 代表体**（≈290 条，最贴近「G5 那 276」的口径）：把条件改成 `and '<object>' in cls(m['Name'])`。

### 2) 一条命令跑完（与 §三 完全相同，只换两个路径）

```bash
export JAVA_HOME="D:\\2\\tools\\jdk-21.0.12.1+1"
cmd //c "D:\2\tools\ghidra_12.1.3_PUBLIC\support\analyzeHeadless.bat D:\2\tools\ghidra_proj Warpforge \
  -process GameAssembly.dll -noanalysis -readOnly \
  -scriptPath D:\2\Warpforge_tools\data\decomp_il2cpp_0827\ghidra_scripts \
  -postScript Decomp.java \
    D:\2\tools\decomp_generic_trial\wanted_generic_all.txt \
    D:\2\tools\decomp_generic_trial\out_generic_all"
```

### 3) 预计耗时（实测外推，不是拍脑袋）

| 项 | 实测 / 外推 |
|---|---|
| 28 条（本轮，三次） | **7.7 s** 墙钟（含 JVM + 打开 2.6 GB 工程） |
| 开工固定成本 | ≈ 6 s（Ghidra 日志自报 `Headless startup complete (1993 ms)` + 打开工程） |
| **615 条（一趟）** | **≈ 60–120 s**（按 0.1–0.2 s/方法外推） |
| **只跑 `<object>` 290 条** | **≈ 35–60 s** |
| 冷启动（Ghidra 首次解压类型库 / 页缓存空） | 首次那趟明显更慢，**留 5 分钟余量** |

⚠️ 正式批量时**建议拆 2–3 趟**（每个泛型类型定义一趟），这样单趟失败不用整体重跑；总耗时同一个量级。

### 4) 风险（4 条，都能预判）

1. **ICF 折叠改名（必然发生，约 30%）**：187/615 与其他方法共用地址 ⇒ 产物第一行**不是你请求的名字**。**判法**：`all_methods.txt` 里查同一地址挂着哪些名字；**用法**：体仍然可用，按请求的名字归属。
2. **返值被反成 `void`**（已知族）：`get_*` 这类「一行 getter」最容易丢返值。**判法**：`dump.cs` 的声明类型 vs 产物签名。
3. **文件名撞车**：`Decomp.java` 把名字里非 `[A-Za-z0-9_.-]` 的字符全换成 `_` 再当文件名 ⇒ `Foo<object>$$X` 与 `Foo_object_$$X` 理论上会撞、后者覆盖前者。615 条里**没实测到**，但批量后应核对「产物数 vs `ok=`」的差值。
4. **单函数 60 s 超时**：`Decomp.java` 里写死 `decompileFunction(fn, 60, monitor)`，超大方法会 `fail++`（全量那次 fail=7）。批量时**看 `fail=` 而不是看文件数**。
