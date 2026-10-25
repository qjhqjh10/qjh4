# IL2CPP → C# 反编译工具链：调研与实测（2026-10-25）

> 目标（用户 2026-10-25 原话）：「原版是 IL2CPP 编译产物，没有 C# 源码。查查有没有相关工具，看看有没有可以转化的工具。」
> 结论先说：**有，而且能跑通。** 最优路线 = **Cpp2IL（development 分支自建）→ dummy DLL → ilspycmd**，
> Assembly-CSharp 的 **97.7% 方法拿到了可读的真实 C# 方法体**（Ghidra 那套只有 25,096 个方法、且是伪码）。

---

## 〇、一句话结论

| 问题 | 答案 |
|---|---|
| 能不能转成接近 C# 源码？ | **能。** 产出的不是伪码，是 ILSpy 反编译出来的**带方法体的 C#**（有 `out` 参数、三元、枚举名、`switch`、`try/catch`） |
| 用哪个工具？ | **Cpp2IL 2022.1.0 + ilspycmd 11.1.0**（见 §二）；替补 = **il2csharp**（纯 Python，带真实方法体，见 §四） |
| ⚠️ 最大的坑 | **GitHub Release 里的 Cpp2IL 二进制，`dll_il_recovery` 输出格式是空壳**（写死 `ldnull; throw`），必须**从 development 分支源码自建**（见 §三·一）。README 说「`--output-as dll_il_recovery` 就能开分析」，**对官方 release 不成立** |
| 覆盖率 | Ghidra **25,096** → Cpp2IL dev **55,025**（游戏 + 插件层全部方法，55,029 里成功 55,025）→ 若把 UnityEngine/System 的白名单去掉，理论上限 **147,104**（= 有 native code 的方法总数） |
| 装在哪 | `D:/2/tools/cpp2il/`（Cpp2IL 两个版本 + 产物）、`D:/2/tools/ilspycmd/`、`D:/2/tools/il2csharp_src/` |

---

## 一、候选对比表

| 工具 | 版本 / 最后更新 | metadata v31（= 我们这份） | 产物 | 可读性 | 许可证 | 结论 |
|---|---|---|---|---|---|---|
| **Cpp2IL（SamboyCoding）** | **2022.1.0-pre-release.21**（2026-02-22 发布，仓库 2026-09-15 推送） | ✅ **原生支持**（`Metadata version 31.1`，Unity 6000.2.6f2 自动识别） | dummy DLL（签名/字段偏移/属性/事件/特性全）＋ `dll_il_recovery` 的**真实方法体** | ★★★★★（配 ILSpy） | MIT | ✅ **首选**。⚠️ 必须用 development 源码自建，见 §三 |
| **ILSpy / ilspycmd（icsharpcode）** | **11.1.0.9782**（仓库 2026-10-24 推送） | 与 IL2CPP 无关，吃上一步的 DLL | 单类型 / 整工程 `.cs` + `.csproj` | ★★★★★ | MIT | ✅ **必装**，是「吃 Cpp2IL 产物」的那一半 |
| **il2csharp（mynamescrax）** | 源码 HEAD `c3195dc`（2026-10-23），**无 release / 无 tag** | ✅ README 明写 **v24.2–v31**，主测就是 v31 + Windows x64 | 每类型一个 `.cs` + `.csproj` + `script.json` + `stringliteral.json` | ★★★★（比 Ghidra 好得多，比 Cpp2IL+ILSpy 略糙） | MIT（LICENSE 文件是 MIT；GitHub API 报 NOASSERTION，按文件为准） | ✅ **能用的第二方案**，纯 Python（+ `iced-x86`），不依赖 .NET；实测见 §四 |
| **Il2CppDumper（Perfare）** | v6.7.46（2024-07-06） | 部分（本项目已跑通，产物在 `D:/2/tools/il2cpp_out/`） | `dump.cs`（42.7 MB / 114.7 万行 **纯签名**）、`il2cpp.h`、`script.json`（RVA 表） | ★☆☆☆☆（无方法体） | MIT | 用作 **RVA 索引 / 签名对照**，不是反编译器 |
| **Il2CppInspector（djkaty）** | 2021.1（2021-01-31），仓库 2022-05 后停更 | ❌ **不支持 v31**（当年只到 v24/27 一代） | IL2CPP 模型 + dummy DLL | —— | **AGPL-3.0**（传染性，本地研究可以，但滚进别的工具要小心） | ❌ 淘汰（停在 v27 之前） |
| **Il2CppInterop（BepInEx）** | 2026-07-08 推送 | ✅（喂给 BepInEx 的运行时） | **运行期**代理程序集（让 CoreCLR 调 IL2CPP） | —— | NOASSERTION | ❌ 不是反编译器。它解决的是「mod 怎么调游戏」，不是「看源码」。做 mod 时才用 |
| **dnSpyEx（dnSpy 维护分支）** | 2026-10-17 推送 | 与 IL2CPP 无关 | GUI 反编译 / 调试 | ★★★★★ | **GPL-3.0** | 可选 GUI。⚠️ Cpp2IL 作者说「**用 ILSpy，它对残缺 CIL 的容忍度比 dnSpy 好**」——命令行批量首选 ilspycmd |
| **jakzo/Il2CppDecompiler** | 2024-02 停更，60★ | 未知（Ghidra 扩展，靠反汇编） | Ghidra 插件直接吐 C# | ★★☆ | **无 LICENSE 文件** | ❌ 不采用（无许可证 + 停更 + 覆盖不了 v31 的风险） |
| **holly-hacker/WasmLib** | 2024-09，24★ | 只处理 **WebAssembly** 版 IL2CPP | —— | —— | MIT | ❌ 我们是 Windows x64，用不上 |

> 2024–2026 的「新工具」基本只有 **il2csharp** 一个真货；其余同类（`Corvus` / `Reverseport` / `il2cpp-decompiler` / `BetterCpp2IL` / `Zircon-Dumper`）都是 **≤20★、几个月内新建的账号 / 无 release**，不列入推荐。

---

## 二、装在哪 / 怎么跑（可复跑命令）

```
D:/2/tools/cpp2il/
├─ Cpp2IL.exe                 # 官方发行版 2022.1.0-pre-release.21（15,137,811 B）—— 只能出签名，方法体是空壳
├─ bin_dev/                   # ★ 从 development 源码自建（2022.1.0+daa9647，.NET 10）—— 真方法体
├─ _src/Cpp2IL-development/   # 自建用的源码（从 raw.githubusercontent 逐文件拉的，见 §六·坑 3）
├─ _dl/                       # 下载缓存与日志
├─ out_cpp2il/                # 发行版产物（dummy DLL，空体）
├─ out_cpp2il_ilspy/          # ↑ 经 ilspycmd 转出的 C# 工程（2,403 文件，全是 throw null）
├─ out_cpp2il_dev/            # ★ dev 版产物（真实 IL 的 DLL）
├─ out_cpp2il_dev_ilspy/      # ★★ dev 版 → ilspycmd → C# 工程（2,404 文件，27 MB）
├─ out_il2csharp/             # il2csharp 产物（Assembly-CSharp + firstpass，2,785 文件，48 MB）
└─ out_sample/                # 本轮的并排样本（见 §五）

D:/2/tools/ilspycmd/ilspycmd.exe         # ILSpy 11.1.0.9782（dotnet tool 本地装）
D:/2/tools/il2csharp_src/il2csharp-main/ # il2csharp 源码
```

### ① 出「签名级」dummy DLL（快，7 秒）

```bash
cd D:/2/tools/cpp2il
./Cpp2IL.exe --game-path "D:/2/unity_run_ref" \
  --output-as dll_empty --output-to "D:/2/tools/cpp2il/out_cpp2il"
```

### ② 出「带真实方法体」的 DLL（★ 关键：必须用 bin_dev，不能用 Cpp2IL.exe）

```bash
cd D:/2/tools/cpp2il
./bin_dev/Cpp2IL.exe --game-path "D:/2/unity_run_ref" \
  --output-as dll_il_recovery --output-to "D:/2/tools/cpp2il/out_cpp2il_dev"
# 实测：16.4 秒（模型 1.6 s + IL 恢复 13.9 s），日志末行
#   [Info] [DllOutput] 100% of methods successfully decompiled (55025 / 55029)
```

### ③ DLL → C#

```bash
# 单个类（快，看某个类时用这个）
"D:/2/tools/ilspycmd/ilspycmd.exe" -t BasicCardUI \
  --referencepath "D:/2/tools/cpp2il/out_cpp2il_dev" \
  "D:/2/tools/cpp2il/out_cpp2il_dev/Assembly-CSharp.dll" > BasicCardUI.cs

# 整工程（一条命令出 .csproj + 2404 个 .cs，约 1～2 分钟）
"D:/2/tools/ilspycmd/ilspycmd.exe" -p -o "D:/2/tools/cpp2il/out_cpp2il_dev_ilspy" \
  --referencepath "D:/2/tools/cpp2il/out_cpp2il_dev" \
  "D:/2/tools/cpp2il/out_cpp2il_dev/Assembly-CSharp.dll"
```

### ④ 重建 bin_dev（只在换 Cpp2IL 版本 / 重装机器时用）

```bash
export DOTNET_CLI_TELEMETRY_OPTOUT=1
cd D:/2/tools/cpp2il/_src/Cpp2IL-development
"D:/2/tools/dotnet10/dotnet.exe" publish Cpp2IL/Cpp2IL.csproj -c Release -f net10.0 \
  -o "D:/2/tools/cpp2il/bin_dev"
# 依赖还原走仓库自带的 nuget.config → https://nuget.samboy.dev/v3/index.json（可达，约 4 秒）
# 目标框架 net10.0；本机 D:/2/tools/dotnet10 是 SDK 10.0.402 / 运行时 10.0.13
```

### ⑤ il2csharp（替补方案）

```bash
python -m pip install --user iced-x86            # 唯一外部依赖（abi3 wheel，Python 3.14 可用）
cd D:/2/tools/il2csharp_src/il2csharp-main
python il2csharp.py "D:/2/unity_run_ref" --only Assembly-CSharp \
  -o "D:/2/tools/cpp2il/out_il2csharp" --workers 0 --json
```

---

## 三、实测结果

### 三·一 🔴 官方 Release 的 `dll_il_recovery` 是空壳（本轮最大的发现）

**现象**：`Cpp2IL-2022.1.0-pre-release.21-Windows.exe` 跑 `--output-as dll_il_recovery`，
**7.15 秒**跑完，日志干净（无报错），但转出来的 C# **每个方法都是 `throw null`**：

```csharp
public bool CanUpgrade()
{
	throw null;                     // ← 全库 21,001 处
}
```

**定位过程（不是猜）**：
1. 日志里 **没有** `Finding key function addresses...` 这一行 —— 而 dev 分支源码里
   `AsmResolverDllOutputFormatIlRecovery.BuildAssemblies()` **第一句**就是打印它 ⇒ 说明这个方法压根没被调用。
2. 于是去读**该 exe 对应的 commit**（版本串里的 `58fc404ac503f4e512055cafc48c03088fc6e224`）的源码：
   `Cpp2IL.Core/OutputFormats/AsmResolverDllOutputFormatIlRecovery.cs` **只有 24 行**，方法体写死：
   ```csharp
   instructions.Add(CilOpCodes.Ldnull);
   instructions.Add(CilOpCodes.Throw);
   ```
3. 回头查 pre-release.**19 / .20** 的同名文件 —— **也是 26 行空壳** ⇒ **IL 恢复在官方 release 里已经停了好几版**。
4. `development` 分支 HEAD 的同名文件是**完整实现**（`methodContext.Analyze()` → `IlGenerator.GenerateIl()`），
   **但它没有发版**。

⇒ **结论：README 那句「Analysis is now off by default and can be enabled with `--output-as dll_il_recovery`」
对已发布的二进制不成立。要用 IL 恢复，只能自己编 development。**

### 三·二 自建 development 分支后：IL 恢复真的跑起来了

- 源码 HEAD = `daa964705b6eea683e5d196ae972b268057fa8a9`（版本串 `2022.1.0+daa9647`）
- 构建：`dotnet publish -c Release -f net10.0` **一次通过**（0 error，4 条 trim 警告）
- 跑同一份游戏：

```
[Info] [Library] Using actual IL2CPP Metadata version 31.1
[Info] [Program] Determined game's unity version to be 6000.2.6f2
[Info] [Library] Got Binary codereg: 0x183581FF0, metareg: 0x183CC6710 in 75ms.
[Info] [Program] Finding key function addresses...
[Info] [Program] Key function addresses found in 122.9216ms
[Warn] Skipping RemnantAeldari::Toggle: Decompilation failed: Stack state not settling! (500001 blocks already visited)
[Warn] Method NameGenerator::.cctor is too big (51794 bytes), skipping analysis.
...（共 8 条 Warn，其余全绿）
[Info] [DllOutput] 100% of methods successfully decompiled (55025 / 55029)
[Info] [Program] Done. Total execution time: 16454.0066ms
```

- 日志共 **8 条 Warn**：**4 条「栈状态不收敛」**（`RemnantAeldari::Toggle` / `PriceDisplay::Setup` /
  `UIResourceCounter::Initialize` / `UnityEngine.UI.Extensions.ScrollSnap::OnEndDrag`）
  ＋ **4 条「方法太大跳过」**（`NameGenerator::.cctor` 51,794 B / `I2.Loc.GoogleLanguages::.cctor` 33,484 B /
  `PlayFab.Events.PlayFabEvents::OnProcessingEvent` 33,064 B / `ExitGames...StructWrapperPools::.cctor` 30,538 B）。
  最终计数 **55,025 / 55,029** ⇒ 折合 **4 个方法**没有恢复（部分警告落在被白名单跳过的模块里，不计入分母）。
- **`Assembly-CSharp.dll` 从 1.66 MB（空壳）变成 9.78 MB（有体）**；`Assembly-CSharp-firstpass.dll` 从 0.33 MB 变成 2.21 MB —— 体积差就是方法体。

### 三·三 ilspycmd 吃产物：全工程只崩 1 个方法

```
While decompiling .../Assembly-CSharp.dll:
1 error(s) occurred
  Error decompiling @06000A11 RawCardScript.GetOriginalId: Index was outside the bounds of the array.
EXIT=70
```

**2,404 个 .cs 文件，只有 `RawCardScript.GetOriginalId` 1 个方法反编译失败**（ILSpy 自身数组越界，不是 Cpp2IL 的锅）。

### 三·四 质量量化（`out_cpp2il_ilspy`（空壳）vs `out_cpp2il_dev_ilspy`（真体））

| 指标 | 发行版（空壳） | dev 自建 |
|---|---|---|
| `.cs` 文件数 | 2,403 | 2,404 |
| 总大小 | 7.4 MB | **27 MB** |
| `throw null;`（= 没方法体） | **21,001** | **473** |
| ⇒ 真实方法体占比 | ≈ 0% | **≈ 97.7%** |
| 带 ILSpy 类型推断告警（`//IL_0xxx`）的文件 | —— | 1,555 / 2,404（65%），告警行共 62,418 |

**怎么读那个 62,418**：ILSpy 在「IL 的类型栈和它推的对不上」的地方会插一行
`//IL_xxxx: Expected I4, but got Obj`。它是**注释**，不挡阅读；出现处通常是
**值类型的按地址传参 / 结构体返回（sret）/ 泛型共享体**，也就是 Cpp2IL 的 x64 ISIL 还没完全吃透的那一段。
**不是错误、也不是空方法** —— 方法体在，只是个别表达式的类型被标成 `object`/`ulong`。

---

## 四、替补方案 il2csharp 实测

- 命令：`python il2csharp.py "D:/2/unity_run_ref" --only Assembly-CSharp -o ... --workers 0 --json`
- 探测（`--probe`）先过：
  ```
  metadata v31 | 22647 types | 163827 methods | 129 images | 26514 string literals
  registrations ok | 147104/163827 methods have native code | 5677 ms
  ```
- 正式跑（**306 秒 / 8 worker**）：
  ```
  {"out":"...out_il2csharp","files":2785,"lifted":23684,"failed":0,"fallbacks":0,"emit_failed":0,"ms":306183}
  ```
  ⇒ **23,684 个方法拿到了真实方法体，0 失败、0 回退**。覆盖 Assembly-CSharp(21,633) + firstpass(4,775) 两个程序集。
- 产物：每类型一个 `.cs`（`CardScript.cs` 17,840 行 / 791 KB、`BattleManager.cs` 41,216 行、`DeckEditingWindow.cs` 1,096 行），
  外加 `script.json`（RVA 索引，Il2CppDumper 兼容格式）+ `stringliteral.json`。
- 优点：**纯 Python、不需要 .NET、不需要编 Cpp2IL**，一条命令；对 metadata v31 + x64 明显是专门调过的。
- 缺点（相对 Cpp2IL+ILSpy）：
  - 有 `goto L_1805e792d` 这类**标签 + goto**（Cpp2IL 那一路会还原成 `if/else`）；
  - **个别语义会退化**（见 §五 `ToggleCardBack` 的并排）；
  - 泛型共享体认不出来时给 `sub_18096b540/*shared body, 2 candidates*/`（**诚实标注**，不是静默错）；
  - **仓库 19★、无 release / 无 tag、2026-09 才建号** ⇒ 供应链上要保守对待（本轮已通读：主包无网络调用、无 `os.system`，仅 `tests/`、`tools/` 用 `subprocess` 调 dotnet）。

---

## 五、并排片段（Ghidra 伪码 / Cpp2IL+ILSpy / il2csharp）

### 片段 1：`BasicCardUI.CanUpgrade()`

**① Ghidra（现在 `D:/2/tools/decomp_full/BasicCardUI__CanUpgrade.c`）**
```c
void BasicCardUI__CanUpgrade(longlong param_1)
{
  undefined8 uVar1;
  undefined8 local_res8 [4];
  if (DAT_18453b837 == '\0') {
    FUN_1803f44f0(&DAT_1842d4cf8);
    DAT_18453b837 = '\x01';
  }
  uVar1 = *(undefined8 *)(param_1 + 0xe0);
  local_res8[0] = 0;
  if (*(int *)(DAT_1842d4cf8 + 0xe4) == 0) {
    il2cpp_runtime_class_init();
  }
  CardServices__CheckForCardUpgrade(uVar1,local_res8,0);
  return;
}
```

**② Cpp2IL(dev) + ILSpy（`out_sample/BasicCardUI.cpp2il_dev.ilspy.cs:567`）**
```csharp
public bool CanUpgrade()
{
	CardTierConfig.CardTierInfo nextTier;
	return CardServices.CheckForCardUpgrade(<rawCardData>k__BackingField, out nextTier);
}

public void ToggleCardBack(bool option)
{
	card2DController.SetCardVisibility(option ? Card2DController.CardVisibility.Back : Card2DController.CardVisibility.Front);
}
```

**③ il2csharp（`out_sample/BasicCardUI.il2csharp.cs:522`）**
```csharp
public bool CanUpgrade()
{
    CardTierConfig.CardTierInfo cardTierInfo1 = default;
    return CardServices.CheckForCardUpgrade(this.__field_rawCardData, ref cardTierInfo1);
}
public void ToggleCardBack(bool option)
{
    this.card2DController.SetCardVisibility(option); return;   // ← 枚举映射丢了（Cpp2IL 那版是对的）
}
```

### 片段 2：`CardScript.CanAttackNow()`（同一条控制流，三级可读性）

**① Ghidra**（`decomp_full/CardScript__CanAttackNow.c`，节选）
```c
  uVar6 = EntityScript__get_CurrentMeleeAttack(param_1,0);
  if ((int)uVar6 < 1) {
    uVar6 = EntityScript__get_CurrentRangeAttack(param_1,0);
    if ((int)uVar6 < 1) goto LAB_1805e792d;
  }
  if (*(char *)(param_1 + 0x65) == '\0') {
    uVar6 = EntityScript__HasCurrentTrait(param_1,0x96,0);
    ...
    bVar7 = *(char *)(lVar4 + 0x246) == '\0';
```

**② Cpp2IL(dev) + ILSpy**（`out_sample/CardScript.cpp2il_dev.ilspy.cs:6940`，节选）
```csharp
public unsafe bool CanAttackNow()
{
	int currentMeleeAttack = CurrentMeleeAttack;
	if (currentMeleeAttack <= 0)
	{
		int currentRangeAttack = CurrentRangeAttack;
		if (currentRangeAttack <= 0) goto IL_0233;
	}
	if (!base.<isRemnant>k__BackingField && !HasCurrentTrait(DefinedTrait.cantAttack)
	    && !HasCurrentTrait(DefinedTrait.nonCombatant))
	{
		BattleManager battleManager = <BattleMgr>k__BackingField;
		if (!base.<isPlayer>k__BackingField)
			flag = !battleManager.<allowEnemyActionsFlag>k__BackingField;
		...
```

**③ il2csharp**（`out_sample/CardScript.il2csharp.cs:1306`，节选）
```csharp
public bool CanAttackNow()
{
    if (this.CurrentMeleeAttack > 0) {
        L_1805e77b5:
        if (this.__field_isRemnant) { L_1805e792d: return false; }
        if (this.HasCurrentTrait(DefinedTrait.cantAttack)) goto L_1805e792d;
        if (this.HasCurrentTrait(DefinedTrait.nonCombatant)) goto L_1805e792d;
        ...
```

**判读**：三者**语义一致**（Cpp2IL 那版甚至把 `0x96 → DefinedTrait.cantAttack`、`0x370 → DefinedTrait.nonCombatant` 都对上了），
差别在**形式**：Ghidra 要人脑还原控制流，il2csharp 有 goto，Cpp2IL+ILSpy 是**能直接读的 C#**。

> 另有一例能看出**保真度差异**：`BasicCardUI.ColliderBigSize(bool)` ——
> Cpp2IL 版把 `gameObject.GetComponent<BoxCollider2D>()` 提到 `if` **之前**（正确，原版就是这么取的），
> il2csharp 版把它留在 `else` 分支里、`value == true` 时 `boxCollider2D1` **未赋值**就用（该分支实际语义错）。
> ⇒ **两个工具要用，但「以谁为准」= Cpp2IL+ILSpy**；il2csharp 适合当**独立第二意见**。

---

## 六、覆盖率：25,096 → 多少？

| 口径 | 数字 |
|---|---|
| metadata 里的方法总数 | **163,827** |
| 其中**真的有 native code** 的 | **147,104**（89.8%） |
| 现有 Ghidra `decomp_full/` 的方法体 | **25,096**（≈ 总有 native code 的 **17%**） |
| Cpp2IL dev 实际恢复 | **55,025 / 55,029** = 100%（日志原文）；范围 = **Assembly-CSharp + firstpass + 全部游戏插件**（UnityEngine.\*/Unity.\*/System.\*/mscorlib 被 `shouldSkip` 白名单**按设计跳过**，这些本来就有官方源码） |
| Assembly-CSharp + firstpass 里**真拿到方法体**的 | 空壳 21,001 处 `throw null` → dev 只剩 **473** ⇒ **≈ 97.7%** |
| 理论上限（把白名单也去掉） | **147,104** |

> **能不能给「全部」方法的 C#？**
> **能给「全部有 native code 且我们能跑的」—— 但「全部」这个词要拆开看**：
> ① **游戏自己的代码**（Assembly-CSharp / firstpass / 插件）→ **97.7% 真体**，剩下的 2.3% 是 4 个超大/不收敛方法 + 纯声明（abstract / interface / extern）；
> ② **Unity 引擎与 .NET 基础库**（约 9.2 万方法）→ Cpp2IL **故意跳过**（它们是 Unity 自己的 DLL，本来就不该反编译）；
> ③ **泛型共享体（shared generic）** → 可能一个 native 体对应多个 C# 方法，Cpp2IL 会尽量按调用点还原，未还原处会标警告（**不会静默给错**）。

---

## 七、建议的落地流程

```
GameAssembly.dll + global-metadata.dat
        │
        ├─(A) Cpp2IL.exe  --output-as dll_empty        → 签名级 dummy DLL（7 秒）★ 日常查签名用这个
        │
        └─(B) bin_dev/Cpp2IL.exe --output-as dll_il_recovery  → 带真实 IL 的 DLL（16 秒）★ 要方法体用这个
                        │
                        └─ ilspycmd -p  →  out_cpp2il_dev_ilspy/  （2,404 个 .cs + .csproj，1～2 分钟）
```

- **(A) 与 `D:/2/tools/decomp_full/`（Ghidra）并存，别删**：
  - Ghidra 那 25,096 份仍是**独立的第二意见**（尤其当 Cpp2IL 的 ILSpy 告警很密时，可以回去看反汇编）；
  - 但**「查原版语义」的第一站应从 `out_cpp2il_dev_ilspy/` 开始** —— 它比伪码快一个数量级。
- **(A) 与 Il2CppDumper 的关系**：`dump.cs`（42.7 MB，1,147,030 行）仍是**最全的签名表**（含字段偏移、
  `TypeDefIndex`、RVA），Cpp2IL 的 dummy DLL 是同一份东西的另一种包装。**查签名查 `dump.cs` 更快（纯文本 grep）**。
- **产物放哪**：`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/`（只读档案区）。⛔ **不要拷进 `D:/4` 工程**。
- **要不要重建**：只有原版游戏换版本/换包时才需要重跑（16 秒 + 2 分钟，成本极低）。

---

## 八、踩到的环境坑（本机 2026-10-25 实测）

1. 🔴 **`github.com:443` 连不上，但 GitHub 的其它域名通**：
   `api.github.com` ✅ / `codeload.github.com` ✅ / `raw.githubusercontent.com` ✅ / `objects.githubusercontent.com` ✅ /
   **`github.com` ❌（`Failed to connect ... after 21s`）**。
   ⇒ **`releases/download/...` 的链接下不了**。绕法（本轮就是这么下的）：
   ```bash
   curl -sSL -H "Accept: application/octet-stream" \
     -o Cpp2IL.exe "https://api.github.com/repos/SamboyCoding/Cpp2IL/releases/assets/360212190"
   ```
   （asset id 从 `https://api.github.com/repos/<owner>/<repo>/releases` 的 `assets[].id` 取）
2. **codeload 拉整仓 tarball 太慢且不支持断点续传**：Cpp2IL 仓库 **321 MB**（其中 **TestFiles/ 占 319 MB**
   的测试用 GameAssembly/PDB）。拉 39 MB 就开始超时，`-C -` 也不行（`HTTP server does not seem to support byte ranges`）。
   ⇒ **只拉源码**：GitHub API 的 `git/trees/development?recursive=1` 拿到全文件清单（**排除 TestFiles/ 后只有 457 个文件 / 1.95 MB**），
   再用 `curl -P 8` 逐文件从 `raw.githubusercontent.com` 拉。脚本留在
   `D:/2/tools/cpp2il/_scripts/fetch_src.py`（⚠️ 它用 Python `urllib` 会**卡在 IPv6 上**，本轮最后是改用 `xargs -P 8 curl` 才拉完）。
3. **NuGet**：`api.nuget.org` 会 302 到 `nuget.azure.cn`（本机镜像），`-L` 跟随即可；
   Cpp2IL 仓库自带 `nuget.config` 指向 `https://nuget.samboy.dev/v3/index.json`（**可达，4 秒还原完**）——
   **它同时代理了 nuget.org 的包**（CommandLineParser / Pastel / PolySharp / AsmResolver / AssetRipper.CIL 全下到了）。
4. **Cpp2IL 需要 .NET**：官方 exe 是自包含单文件（15 MB，直接跑）；**自建版**要 .NET 10 SDK + 运行时，
   本机用 `D:/2/tools/dotnet10/`（SDK 10.0.402 / 运行时 10.0.13）✅。
5. **ilspycmd 需要 .NET 运行时**：本地装 `dotnet tool install ilspycmd --tool-path D:/2/tools/ilspycmd`
   （版本 11.1.0.9782），跑起来没报错（用它自己的 apphost 找到运行时即可）。
6. ⚠️ **Cpp2IL 的日志带 ANSI 颜色码**，重定向到文件后 grep 不友好 ⇒ 先过一遍
   `sed -e 's/\x1b\[[0-9;]*m//g'`。

---

## 九、还没做 / 建议下一步

1. **把 `out_cpp2il_dev_ilspy/` 定为「查原版语义」的第一站**，并在 `资料/命令速查.md` 里留了命令（见 §二）。
2. **可以考虑把 Cpp2IL 的 `shouldSkip` 白名单也去掉**（`AsmResolverDllOutputFormatIlRecovery.FillMethodBody` 里那 5 行），
   把 UnityEngine/System 的 9.2 万方法也恢复出来 —— **判据上有价值（比如确认真的是不是原版 UGUI 行为），
   但体积会涨很多，本轮没做**。
3. **il2csharp 与 Cpp2IL 的结论互相校验**：两者独立实现，凡两边都给出同一结论的方法可信度最高；
   凡两边不一致（如 §五 `ToggleCardBack`）⇒ **回去看卡图/实机，别挑一个信**。
4. **没有验证的部分**：`RawCardScript.GetOriginalId`（ILSpy 自己崩）—— 需要时用 il2csharp 那版交叉看，
   或对这一个方法用 Ghidra 看反汇编。

---

## 附：本轮落盘的样本文��

| 文件 | 内容 |
|---|---|
| `D:/2/tools/cpp2il/out_sample/BasicCardUI.ilspy.cs` | 发行版（空壳）→ ILSpy：`throw null` 的对照证据 |
| `D:/2/tools/cpp2il/out_sample/BasicCardUI.cpp2il_dev.ilspy.cs` | ★ dev → ILSpy：823 行，真实方法体 |
| `D:/2/tools/cpp2il/out_sample/CardScript.cpp2il_dev.ilspy.cs` | ★ dev → ILSpy：21,313 行 |
| `D:/2/tools/cpp2il/out_sample/DeckEditingWindow.cpp2il_dev.ilspy.cs` | ★ dev → ILSpy：1,965 行（卡组编辑窗） |
| `D:/2/tools/cpp2il/out_sample/BasicCardUI.il2csharp.cs` | il2csharp 同一类型，做并排 |
| `D:/2/tools/cpp2il/out_sample/CardScript.il2csharp.cs` | il2csharp：17,840 行 |
| `D:/2/tools/cpp2il/out_sample/DeckEditingWindow.il2csharp.cs` | il2csharp：1,096 行 |
