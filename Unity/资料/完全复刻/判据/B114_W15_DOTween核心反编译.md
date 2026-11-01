# B-244 · W-15 —— 原版 DOTween 核心方法体反编译（1,848 条）

> 执行代理 `W-15` · 2026-11-01 · 工作目录 `d:/4`
> **本文件只写证据与结论，⛔ 不写「已完成/未完成」**（状态见 `活状态表.md` 的 `B-244`）。
> 产物：**`D:/2/tools/decomp_tween/out/`**（全新目录，⛔ 未覆盖 `decomp_full/`，⛔ 未动 `decomp_generic_trial/`）
> 全程**只读** `GameAssembly.dll`；**没有运行 Unity、没有动 git、没有执行任何解包数据**。

---

## 一、结论速览（三个数）

| 项 | 值 |
|---|---|
| **请求数** | **1,848**（`all_methods.txt` 里 `DG.Tweening.*` **2,182** 条 − `DOTweenModule*` 322 − `CYInstruction*` 12） |
| **出产物数** | **1,848 / 1,848**（Ghidra 自报 `DECOMP DONE ok=1848 fail=0 created=30 contained=0`）⭐ **一条没失败** |
| **产出率** | **100.00%** |
| **落盘 `.c` 文件数** | **1,843**（少 5 个 = 请求清单里有 **5 条完全重复的行**，见 §六·2） |
| **首行名字 = 请求名（SELF）的比例** | **837 / 1,848 = 45.29%** |
| · 首行是 `FUN_xxxx` | **578（31.28%）** —— 🔴 **全部 578 条都含 `", "`**（Ghidra Renamer 拒建符号），**不是失败** |
| · 首行是被 ICF 折叠的「别人」的名字 | **433（23.43%）** —— 体是对的 |
| · **去掉含 `", "` 的名字后**，SELF 比例 | **837 / 1,250 = 66.96%**（比 G7 那批 54.6% 还高） |
| 空壳（非空行 ≤2 的畸形产物） | **0 / 1,843** |
| 0 条可执行语句的纯 `return;` | **27** —— 🔴 **原版就是空实现**（各 `Reset()` / `IDisposable.Dispose`），见 §六·3 |
| 总行数 | **83,587 行**；其中 **325 个文件 ≥50 条语句**（最大 906 条：`PathPlugin.SetOrientation`） |

**一句话**：`B-244` 说的「DOTween 核心实现从没被反编译过」这条**缺口已经补上** ——
`TweenManager` / `DOTween` / `Tweener` / `Sequence` / `EaseManager` / 各 Plugin **全部有真方法体**，
**零失败、零畸形空壳**（27 个纯 `return;` 是原版自己的空实现，不是导出问题）。

---

## 二、复现命令（逐条可跑）

### 2.1 生成名单

```bash
cd /d/2/tools/decomp_tween && python -I -X utf8 extract_wanted.py
```
- 源：`D:/2/tools/all_methods.txt`（格式 `<十进制 RVA>\t<名字>`）
- 过滤：名字含 `DG.Tweening.`，**排除** `DOTweenModule`（322）与 `CYInstruction`（12）
- 输出：`wanted_tween.txt`（`RVA\tName`，`$$` → `.`，与 B95 的 `wanted_b95.txt` 同格式）+ `wanted_report.txt`

### 2.2 导出（Ghidra headless，`-readOnly`）

```bash
bash /d/2/tools/decomp_tween/run_tween.sh
```
脚本内容等价于：
```bash
export JAVA_HOME="D:\2\tools\jdk-21.0.12.1+1"
cd /d/2/tools && cmd //c "D:\2\tools\ghidra_12.1.3_PUBLIC\support\analyzeHeadless.bat \
  D:\2\tools\ghidra_proj Warpforge -process GameAssembly.dll -noanalysis -readOnly \
  -scriptPath D:\2\tools\decomp_tween\scripts \
  -postScript DecompTween.java \
  D:\2\tools\decomp_tween\wanted_tween.txt D:\2\tools\decomp_tween\out"
```
- 导出脚本 = **`DecompG95.java` 的副本**，只改了类名 → `scripts/DecompTween.java`
  （保留它的两条兜底：`createFunction(addr)` / `getFunctionContaining(addr)`；文件名 = `净化名__<十六进制RVA>.c`）
- **实测耗时**：反编译阶段 **16:57:37 → 16:57:55 = 18 秒**跑完 1,848 条；含 Ghidra 冷启动合计 **< 1 分钟**

### 2.3 出索引与统计

```bash
cd /d/2/tools/decomp_tween && python -I -X utf8 analyze.py        # → out/_MANIFEST_tween.tsv + analysis.txt
cd /d/2/tools/decomp_tween && python -I -X utf8 final_stats.py    # → final_stats.txt（按类汇总 + 体形分布）
cd /d/2/tools/decomp_tween && python -I -X utf8 find_dups.py      # 文件重名检查（5 条）
cd /d/2/tools/decomp_tween && python -I -X utf8 dump_vs_list.py   # dump.cs 交叉核对（42 条 RVA -1）
```

### 2.4 怎么用

索引 = **`D:/2/tools/decomp_tween/out/_MANIFEST_tween.tsv`**（11 列格式同 B95：`类.方法 / RVA / 产物文件 / 行数 / 首行符号 / 名字 SELF|ICF|FUN_|MISSING / 备注`）。

```bash
ls D:/2/tools/decomp_tween/out | grep TweenManager                                   # 按类找
grep -P "^DG\.Tweening\.Core\.TweenManager\.Update\t" \
     D:/2/tools/decomp_tween/out/_MANIFEST_tween.tsv                                 # 按方法名定位文件
```
🔴 **查文件要按 manifest 的「产物文件」列，⛔ 别按 `.c` 的首行名找** —— 有 31% 的首行是 `FUN_`（见 §四·2）。

---

## 三、产物结构

```
D:/2/tools/decomp_tween/
├── wanted_tween.txt              1,848 行 "RVA\tName"      ← 请求清单
├── wanted_report.txt             名单统计（去掉多少、多少地址重复）
├── run_tween.sh / run_tween.log  导出命令与完整日志
├── extract_wanted.py / analyze.py / final_stats.py / find_dups.py / dump_vs_list.py
├── analysis.txt / final_stats.txt
└── out/                          1,843 个 .c + _MANIFEST_tween.tsv
```

- **未触碰**：`D:/2/tools/decomp_full/` · `D:/2/tools/decomp_generic_trial/out*/` · `D:/2/tools/ghidra_proj/` 的工程库
- ⚠️ **`-readOnly` ≠ 零写入**（实测）：工程库没动，但 **2 个索引文件的 mtime 被刷新到 16:57** ——
  `ghidra_proj/Warpforge.rep/idata/~index.dat` · `…/versioned/~index.dat`
  （两份 `~index.bak` 仍停在 10-29 ⇒ 本次没重写；比 B95 文档记的「4 个」少 2 个）

---

## 四、覆盖率与质量

### 4.1 覆盖率 100%，0 失败

Ghidra 日志原文：`DECOMP DONE ok=1848 fail=0 created=30 contained=0` ——
**没有任何一条撞到「无函数 / 超时」**（`DECOMPFAIL` 行数 = 0）。
`created=30` = 那 30 个地址原本不在工程函数表里、被 `createFunction` 补建（见 §六·4）。

### 4.2 🔴 「首行是 `FUN_`」= 名字含 `", "` —— 本次给出**干净的反证**

| 子集 | 条数 | SELF | ICF | FUN_ |
|---|---:|---:|---:|---:|
| 名字**含** `", "`（全是泛型类，如 `TweenerCore<Color, Color, ColorOptions>`） | 598 | 0 | 20 | **578** |
| 名字**不含** `", "` | 1,250 | 837 | 413 | **0** |

⇒ **含 `", "` ⇒ 0 条 SELF；不含 `", "` ⇒ 0 条 `FUN_`** —— 与 G7「名字含 `", "` 时 Ghidra Renamer 拒建符号且吞异常」完全吻合。
那 **20 条**例外全部是 `ABSTweenPlugin<…>..ctor`，它们被 ICF 折叠到 `<>f__AnonymousType31..ctor`（**别人的名字本身不含逗号**），所以首行不是 `FUN_`。

**⇒ 报告口径必须写清**：`FUN_` 是**名字缺失**，不是**体缺失**；本批 578 条 `FUN_` 的体全部可读（见 §五 抽样 3、5）。

### 4.3 体形分布（1,843 个文件）

| 形状 | 条数 | 说明 |
|---|---:|---|
| 纯 `return;` 空实现 | **27** | 🔴 **是原版的空实现，不是失败** —— 见 §六·3 |
| 薄壳（≤8 语句 + `jmp [reg]` 间接跳转） | 26 | 经泛型 `MethodInfo` 虚分派；**证明调用形状**，本身不含算法 |
| ≥50 语句的大函数 | **325** | 最大 906 条 |
| 其余 | 1,465 | 1–49 条语句，逐条可读 |

**最大的 6 个文件**（说明核心逻辑确实在）：

| 语句数 | 文件 |
|---:|---|
| 906 | `DG.Tweening.Plugins.PathPlugin.SetOrientation__aefa70.c` |
| 549 | `DG.Tweening.Core.Easing.EaseManager.ToEaseFunction__b045c0.c` |
| 378 | `DG.Tweening.Core.TweenManager.FilteredOperation__afadb0.c` |
| 282 | `DG.Tweening.Plugins.PathPlugin.SetChangeValue__aef3c0.c` |
| 279 | `DG.Tweening.Plugins.Core.PathCore.Path.Draw__af1f00.c` |
| 253 | `DG.Tweening.Plugins.Core.PluginsManager.GetDefaultPlugin_*__d62fa0.c`（16 个实例共址） |

### 4.4 按类分布（核心类逐个都有）

| 顶层类型 | 请求 | SELF | ICF | FUN_ |
|---|---:|---:|---:|---:|
| `DG.Tweening.ShortcutExtensions`（含 `<>c__DisplayClass`） | 342 | 182 | 160 | 0 |
| `DG.Tweening.Tweener`（泛型类） | 161 | 1 | 0 | **160** |
| `DG.Tweening.DOTween` | 114 | 87 | 3 | 24 |
| `DG.Tweening.TweenSettingsExtensions` | 82 | 79 | 0 | 3 |
| **`DG.Tweening.Core.TweenManager`** | **64** | **44** | 0 | 20 |
| **`DG.Tweening.Core.Easing.EaseManager`** | **42** | **41** | 1 | 0 |
| `DG.Tweening.TweenExtensions` | 40 | 40 | 0 | 0 |
| `DG.Tweening.Core.DOTweenComponent` | 31 | 24 | 7 | 0 |
| `DG.Tweening.DOVirtual` | 29 | 17 | 12 | 0 |
| `DG.Tweening.TweenParams` | 26 | 26 | 0 | 0 |
| `DG.Tweening.Plugins.Core.PluginsManager` | 24 | 1 | 0 | 23 |
| **`DG.Tweening.Sequence`** | **17** | **17** | 0 | 0 |
| **`DG.Tweening.Tween`** | **18** | **14** | 4 | 0 |
| `TweenerCore<…>` × **20 个实例化** | 各 17（合计 340） | 0 | 0 | 各 17 |

> `B114_R2` 记的「`decomp_full` 里 `TweenManager` 0 命中」——现在 `TweenManager` **64 条全在**，
> 包括 `Update`(124 行) / `FilteredOperation`(378 语句) / `ReorganizeActiveTweens` / `Despawn` 等。

---

## 五、抽样实读（要求 5 份，实际读了 10 份）

### 5.1 随机抽 5 份（`random.seed(20261101)`，从 1,843 个文件里取）

| # | 文件 | 实读结论 |
|---|---|---|
| 1 | `DG.Tweening.Tween.set_active__ae5ac0.c` | `void DG_Tweening_Tween__set_active(longlong p, undefined1 v){ *(undefined1*)(p+0xe8)=v; }` —— **真体**，写 `active` 字段 `+0xE8` ✔ |
| 2 | `DG.Tweening.Core.DOTweenUtils..cctor__b035b0.c` | 静态构造：`Array` 分配 3 元素 + 逐格写 + 写回静态字段，附 `FUN_1803f3730`（写屏障）—— **真体** ✔ |
| 3 | `DG.Tweening.Core.Easing.EaseManager.__c._ToEaseFunction_b__4_17__b06730.c` | 闭包体：`if (t != d) Math.Pow(K, t*a/d) …` 再按位拼回 float —— **真体**（指数缓动 InExpo 形）✔ |
| 4 | `DG.Tweening.DOTween.TotalActiveTweens__ad1670.c` | `return *(int*)(*[TweenManager+0xb8] + 0x18)` —— **真体**（读活跃队列计数）✔ |
| 5 | `DG.Tweening.Core.DOSetter_ulong_.BeginInvoke__168d640.c` | 首行 `System_Action<ulong>__BeginInvoke` —— **ICF 折叠**到 `System.Action<ulong>.BeginInvoke`：装箱 + 转调，**语义一致**，不是空壳 ✔ |

### 5.2 另加 5 份重点（各代表性类别）

| # | 文件 | 实读结论 |
|---|---|---|
| 6 | `DG.Tweening.Core.TweenManager.Update__afe8a0.c`（124 行，SELF） | 完整主循环：类初始化闸 → `active` 置 1 → 遍历 `+0x74` 计数 → 逐条 `ReorganizeActiveTweens` / 更新 —— **真体** ✔ |
| 7 | `DG.Tweening.Core.Easing.EaseManager.Evaluate__b03760.c`（302 行，SELF） | `switch(param_1)` 覆盖 **1..0x24（36 种缓动）**，跳转表 `switchdataD_180b044c4` —— **真体**，正是 `B114_R2` 想要的「原版缓动曲线」判据 ✔ |
| 8 | `DG.Tweening.Core.TweenerCore_Vector3__Vector3__VectorOptions_.ChangeEndValue__138ea50.c`（123 行，**FUN_**） | 泛型实例体：类初始化 → 参数类型校验 → 写 `+0x178/+0x180` → 调 `Startup` —— **首行 FUN_ 但体完整**（🔴 证明 `FUN_` ≠ 失败）✔ |
| 9 | `DG.Tweening.DOTween.Kill__acea60.c`（34 行，SELF） | `Kill(target, complete)`：`complete ? TweenManager.FilteredOperation(0,1,target,1,…) : 0` + `FilteredOperation(1,1,target,0,…)`，返回两者之和 —— **真体** ✔ |
| 10 | `DG.Tweening.Color2..ctor__acc7a0.c`（ICF） | 首行 `UnityEngine_Rendering_Universal_Fixed2<float4>___ctor`，体 = 两次 16 字节复制 —— `Color2` 正是 4×float，**ICF 但语义正确** ✔ |

另有 2 份 `createFunction` 兜底产物复核（§六·4 那两个地址）：`TweenerCore<…>.Startup` 与 `ValidateChangeValueType` **都是完整可读体**。

---

## 六、没解出来的 / 卡住的（0 条失败，但有三类要写清）

### 6.1 一条都没失败

`fail=0`、`DECOMPFAIL` 日志 0 行、`missing` 0 个文件、空壳 0 个。**没有 RVA 为 -1 导致解不出的情况**
（`all_methods.txt` 的地址来自 `script.json`，泛型实例化**已经在里面**、带真实 RVA —— 这正是「按 `// RVA: -1` 筛名单会丢掉整类泛型方法」那条坑的反面）。

### 6.2 请求 1,848 条但只落盘 1,843 个文件 —— 是 **5 条完全重复的请求行**

| RVA | 名字 | 重复次数 |
|---|---|---|
| `0xAE4C40` | `DG.Tweening.TweenSettingsExtensions.SetOptions` | 2 |
| `0xAE4C20` | 同上 | 2 |
| `0xAE4E70` | 同上 | 2 |
| `0x4B33B0` | `DG.Tweening.Plugins.Vector3ArrayPlugin.SetFrom` | 2 |
| `0x4B33B0` | `DG.Tweening.Plugins.PathPlugin.SetFrom` | 2 |

⇒ `all_methods.txt` 里这 5 组是**名字与地址都相同**的行（`SetOptions` 三组是重载被折叠到同一体；两条 `SetFrom` 是空实现共址）。
**Ghidra 照样反编译了 1,848 次、互相覆盖成同一个文件** ⇒ **内容无损**，只是磁盘上少 5 个文件。

### 6.3 27 个「纯 `return;`」文件是**原版的空实现**，不是失败

其中 **26 个落在已知的全局共享空方法桩 `0x4B33B0`**（首行被 ICF 命名成 `NetworkingPeer__OnMessage`）：

- **16 个 `*Plugin.Reset()`** —— `CirclePlugin` / `ColorPlugin` / `Color2Plugin` / `DoublePlugin` / `FloatPlugin` /
  `IntPlugin` / `LongPlugin` / `QuaternionPlugin` / `RectPlugin` / `UintPlugin` / `UlongPlugin` / `Vector2Plugin` /
  `Vector3Plugin` / `Vector4Plugin` / `PureQuaternionPlugin` / `Plugins.Options.NoOptions.Reset`
- 6 个 `DOTweenComponent.<WaitFor*>d__NN.System.IDisposable.Dispose`
- `LinearDecoder.SetWaypointsLengths` · `PathPlugin.SetFrom` · `Vector3ArrayPlugin.SetFrom` · `StringPlugin.SetRelativeEndValue`

第 **27** 个是**独立地址** `0xAEE200` 的 **`IntPlugin.GetSpeedBasedDuration`** —— 它**真的是空实现**，
而兄弟 `FloatPlugin.GetSpeedBasedDuration` 有真体（`duration/speed` + 负号翻转，见
`out/DG.Tweening.Plugins.FloatPlugin.GetSpeedBasedDuration__aeddd0.c`）⇒ **这是原版真实的类间差异，不是导出问题**。

另有一族「形似空实现」但**不在上面 27 条里**：`0x4B3000` 的 **110 条 `..ctor`**（`DOTween..ctor` + 各 `<>c__DisplayClass*..ctor`
+ 20 个 `ABSTweenPlugin<…>..ctor`）体是「调一次空桩 + `return;`」，**等价于空 ctor**。

✅ 以上都是 DOTween 的真实形状，**照抄即可，⛔ 别"修好"**。

### 6.4 `createFunction` 兜底用了 30 次 —— 这 30 条要**多看一眼**

| 方法 | 条数 |
|---|---:|
| `TweenerCore<…>.Startup` | 23 |
| `TweenerCore<…>.ValidateChangeValueType` | 7 |

这 30 个地址在 Ghidra 工程里**原本不是独立函数**（泛型类虚方法），由 `createFunction(addr,null)` 补建 ⇒
**体边界是 Ghidra 推断的**，个别可能被截短。已另抽 2 份实读（§五 末尾那段，`Startup@0x1393F60` / `ValidateChangeValueType@0x138BB20`），都完整；
**但整族若要逐行依赖，建议先核「末句是不是 `return`」**。`contained=0` ⇒ 没有「拿邻居的体凑数」的情况。

### 6.5 `dump.cs` 里 42 条 `// RVA: -1` 的名字 —— 都是**泛型定义 / 抽象成员**，不是漏

交叉核对（`dump_vs_list.py`）：`dump.cs` 有 **77 个 `DG.Tweening*` 类/结构、668 个方法名**（其中 **42 条 `// RVA: -1`**）。
这 42 条**在 `all_methods.txt` 里没有同名条目**，但**每一条都有真实体**，只是挂在具体实现上：

| `// RVA: -1` 的名字 | 真实体在哪（已导出） |
|---|---|
| `TweenerCore<T1,T2,TPlugOptions>.{ChangeEndValue,ChangeStartValue,ChangeValues,ApplyTween,Startup,…}`（10） | **20 个 `TweenerCore<具体类型>` 实例化**各 17 条（共 340 条） |
| `ABSTweenPlugin<T1,T2,TPlugOptions>.{SetFrom,Reset,EvaluateAndApply,…}`（7） | 各 `*Plugin` 具体类（`ColorPlugin`/`Vector3Plugin`/`PathPlugin` …） |
| `DOGetter<T>` / `DOSetter<T>` / `TweenCallback<T>.{Invoke,BeginInvoke,EndInvoke}`（9） | 各具体 `DOGetter<Color>` / `DOSetter<ulong>` … 实例化 |
| `Tween.{ApplyTween,Startup,Validate}`（3） | `Sequence`（`Sequence$$Validate`/`$$Startup` 在）+ `TweenerCore<…>` 覆写 |
| `ABSPathDecoder.{FinalizePath,GetPoint,get_minInputWaypoints}`（3） | `LinearDecoder` / `CatmullRomDecoder` / `CubicBezierDecoder`（各有全套） |
| 其余（`IPlugSetter<T…>` / `IPlugOptions` / `IDOTweenInit.SetCapacity`） | 接口成员，由实现类承担 |

⇒ **「RVA -1」在这里只是「这是泛型/抽象定义」的记号，不是「解不出来」**。

---

## 七、对 `B-244` 的结论

**该核销** —— 判定条件逐条对：

| 验收要求 | 实测 | 结论 |
|---|---|---|
| 产物落到全新目录、不覆盖 `decomp_full/`、不动 `decomp_generic_trial/out/` | `D:/2/tools/decomp_tween/out/`，另两个目录 mtime 未变 | ✅ |
| 报三个数 + 首行自己名的比例 | 1,848 / 1,848 / 100.00%；SELF 45.29%（去逗号后 66.96%） | ✅ |
| 随机抽 5 份确认是真方法体 | 随机 5 + 重点 5，**10/10 都是真体**，0 空壳 | ✅ |
| 写清哪些没解出来、卡在哪 | §六：**0 失败**；三类「看起来像没解」的（重复行 / 空实现 / 泛型定义 RVA -1）已逐条说明 | ✅ |

**还差什么（留给下一个消费者，不是本活的前置）**：
- 本活只**补覆盖面**，⛔ 没有、也不该改我们工程里的 `DOTween.dll`（`B-96` 已结案：不替换）。
- 「原版与 1.3.030 有没有行为差异」还需要有人**按本产物逐条比对** —— 现在**判据才第一次存在**。
  最值得先比的三个：`EaseManager.Evaluate`（36 种缓动的常量表）· `TweenManager.Update`/`FilteredOperation`（调度与安全模式）· 各 `Plugin.EvaluateAndApply`。

---

## 八、顺手发现（⛔ 本代理未动手改任何东西）

1. 🔴 **`B-244` 原话的「1,847」少算了 1 条 —— 实为 1,848。**
   `B114_R2` 的算式是「剔掉 `Module` / `CYInstruction`」，而用**子串 `Module`** 去剔会连带剔掉
   **`DG.Tweening.Core.DOTweenSettings.ModulesSetup..ctor`**（`Module`=323 vs `DOTweenModule`=322）。
   那是 **DOTween 核心的设置结构**（`DOTweenSettings` 这个 ScriptableObject 里选择启用哪些 tween 模块），**不是 `DOTweenModule*`**。
   本活按「`DOTweenModule` / `CYInstruction` 子串」精确排除 ⇒ **1,848**，并**已经把它解出来了**
   （`out/DG.Tweening.Core.DOTweenSettings.ModulesSetup..ctor__aeec60.c`）。
   ⇒ 建议 `活状态表 B-244` 与 `文件所有权_当前` 里那个「1,847」改成 **1,848**（铁律 5：就地改）。
   ⚠️ 实质影响为 0（本活取的是**超集**，那一条已在产物里），只是数字要对。

2. **`all_methods.txt` 自身有 5 组「同名同址」的完全重复行**（§六·2）。
   ⇒ 以后任何「按 `all_methods.txt` 数方法条数」的统计都会**多算 5 条**（本批：1,848 行 / 1,843 个不同方法）。

3. **`0x4B3000` 是一个此前没记过的共享桩**（除已知的 `0x4B33B0` 空方法桩之外）：
   首行是 `<>f__AnonymousType31..ctor`，体 = 调一次 `NetworkingPeer__OnMessage(…)`，
   被 DOTween 的 **110 条 `..ctor`** 共用（`DOTween..ctor` + 各 `<>c__DisplayClass*..ctor`，其中 20 条是 `ABSTweenPlugin<…>..ctor`）。
   ⇒ **判「空实现 / 折叠」时要把 `0x4B3000` 一起列进已知共享桩清单**。

4. **`DecompG95.java` 的 `createFunction` 兜底是必需的，不是可选的**：本批 **30 条**（全是 `TweenerCore<…>` 的
   `Startup` / `ValidateChangeValueType`）在原 `Decomp.java` 口径下会**直接判失败**（`fm.getFunctionAt()==null` ⇒ skip）。
   ⇒ 呼应「G95 取代 G7」那条：**凡泛型类实例的方法，一律走 G95 那条路**。

5. **`dump.cs` 的 `DG.Tweening` 只有 668 个方法名 / 77 个类型，而 `script.json` 派生的 `all_methods.txt` 有 2,182 条**（含各泛型实例化）——
   两者口径差 3 倍。⇒ **统计 DOTween 覆盖面一律以 `all_methods.txt`（实例化口径）为准**，
   ⛔ 别拿 `dump.cs` 的类成员数当分母（会得出「只覆盖了 30%」的假结论）。
