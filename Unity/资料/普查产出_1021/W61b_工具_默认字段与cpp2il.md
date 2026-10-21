# W61b · 工具线：`check_prefab_refs.py` 默认字段补齐 + Cpp2IL `dll_il_recovery` 可行性

> 执行代理 G · 2026-10-21 第六十一会话。**改了 1 个文件**（`工具/check_prefab_refs.py`，白名单内）。
> 没跑 Unity、没动 git、没改任何正本/台账；**没有写 `d:/2/**`**（只读），所有 CPP2IL 产物落 `d:/4/_tmp_view/cpp2il_run/`。

---

## 0. 一句话结论

| 任务 | 结论 |
|---|---|
| **① `check_prefab_refs.py` 默认字段补齐** | ✅ **做完**。默认集 = `m_Script,m_Sprite,m_fontAsset,m_sharedMaterial,m_Mesh,m_Texture`；`--fields` 显式覆盖行为不变；全仓 1,005 个 `.prefab/.unity` 实扫**没有假阳性**，新增命中全是 `guid` 全 0 的真坏引用。 |
| **② cpp2il 方法体恢复可行性** | ✅ **跑通了，而且是「能恢复」**。本机那只 `Cpp2IL 2022.1.0-pre-release.21` 的 `dll_il_recovery` **是个 stub**（源码实证：与 `dll_throw_null` **逐字相同**）⇒ 用它跑 = 0 恢复。改用 **development 源码（b5ad444）自建**后：总耗时 **16.7 s**，`Assembly-CSharp` **21,132 个有体方法里 20,526 条是真 IL（97.1%）**，ILSpy 11.1 能解成可读 C#，**与现有 Ghidra `.c` 语义对得上**（抽 `CurrencyUI::GetCurrencyAmount` 三方对照）。 |

---

## 1. 任务①：默认字段补齐

### 1.1 改了什么

`d:/4/Unity/工具/check_prefab_refs.py`

- `DEFAULT_FIELDS`：`['m_Script','m_Sprite','m_fontAsset','m_sharedMaterial']`
  → `[..., 'm_Mesh','m_Texture']`（`:52-53`）。
- 文件头补两段知识（`:13-19`、`:33-39`）：补的理由与现读数；**内建资源形状**的处置口径。
- 用法示例补一行（`:29`）。
- `--help` 文本是 `%(default)s` 自动展开 ⇒ 已同步（见 §1.4）。
- **`--fields` 显式覆盖、判据、退出码、输出格式全部未动**（diff = +17/−1 行）。

`m_Material` **仍然不在默认集**（交接文档一直显式传它，见 `资料/交接_第六十一会话.md:86`）；
本次按简报只补 `m_Mesh`/`m_Texture`，未擅自扩大（见 §5 顺手发现 3）。

### 1.2 验收：不给 `--fields` 能报出来了

`ShieldTraitEffect_Idle Necron Variant.prefab`（C 报告点名的实样）：

```
# 默认（补后）
🔴 m_Mesh           总数 27   合法空(fileID=0) 13   本地引用 0    **坏引用 1**
     行 15473  fileID 8804033814099603246
CHECK: 🔴 有坏引用 …   RC=1

# 显式 --fields m_Script,m_Sprite,m_fontAsset,m_sharedMaterial（补前的默认集）
CHECK: ✅ 被查字段里没有「guid 全 0 且 fileID≠0」的坏引用   RC=0
```

⇒ **补前假绿、补后报出**，且**显式覆盖仍只查列出来的字段**（同一个文件两种结论）。

### 1.3 全仓复扫（判「会不会炸出成片假阳性」）

范围 = `MyGame/Assets` 下全部 **984 个 `.prefab` + 21 个 `.unity` = 1,005 个文件**，耗时 **30.8 s**（含 `CollectionCheck.unity` 131 MB）。

**新默认集逐字段合计（2026-10-21 23:30 快照）**：

| 字段 | 坏引用 | 件数 |
|---|---|---|
| `m_Script` | 0 | 0 |
| `m_Sprite` | 52 | 2（`Booster Pack Open Window` 40 · `Booster Info Popup` 12） |
| `m_fontAsset` | 2 | 2 |
| `m_sharedMaterial` | 40 | 3（`CardPrefab` 38 + 2） |
| **`m_Mesh`** | **47** | **8** |
| **`m_Texture`** | **0** | 0（补时 2 条，23:18 重导后归零） |

`m_Mesh` 的 47 条逐件、**按所属组件分类**：

| 条数 | 组件 | 文件 |
|---|---|---|
| 19 | `ParticleSystem`（`ShapeModule.m_Mesh`） | `Godspear Warhead Full.prefab` |
| 15 | 同上 | `Vortex Explosion Massive.prefab` |
| 2 | 同上 | `Goff_ProperKilly.prefab` |
| 1 ×4 | 同上 | `EC_Target_user` / `EC_Target_user Lucius` / `Sword_10x_EC` / `Will Of Gork` |
| **7** | **`SkinnedMeshRenderer.m_Mesh`** | **`Explosion Hive Fleet Arrival Explosions only.prefab`** |

**假阳性判定（逐条看过形状）**：
- 47 条全部是 `m_Mesh: {fileID: <非 0>, guid: 00000000000000000000000000000000, type: 0}` —— 与 C 报告里的实样逐字同形，**不是**另一种写法。
- **全仓没有「内建资源形状」的引用**：`grep` 全仓 `.prefab/.unity`，
  `m_Mesh:` / `m_Texture:` 配 `guid: 0000…` + **小 fileID（≤6 位）** 的 **0 条**
  ⇒ 不需要按 fileID 大小做形状排除（按大小切是猜，会把真坏引用也放过）。这条已写进文件头注释。
- `.unity` 场景侧：新增两个字段 **0 命中**（坏引用全在 prefab）。

### 1.4 `--help` / 行为自检

```
--fields FIELDS   要查的序列化字段名，逗号分隔（默认 m_Script,m_Sprite,m_fontAsset,m_shar
                  edMaterial,m_Mesh,m_Texture）
```

默认目标 `CardPrefab.prefab` 跑通：`m_Mesh 总数 5 / 合法空 2 / 坏 0`、`m_Texture 总数 2 / 合法空 2 / 坏 0`，
rc 仍由 `m_sharedMaterial 38` 决定（=1，**与补前一致**，说明新增字段没有改变既有结论）。

---

## 2. 任务②：Cpp2IL `dll_il_recovery` 可行性

### 2.1 先证明「本机那只 unusable」——不是猜，有源码判据

**输入拷贝**（只读 `d:/2`，落 `_tmp_view`，共 ~98 MB）：

```
D:/4/_tmp_view/cpp2il_run/input/
├── Warpforge.exe                     667,648 B
├── GameAssembly.dll               76,765,184 B
└── Warpforge_Data/
    ├── app.info / globalgamemanagers   （版本探测用）
    └── il2cpp_data/Metadata/global-metadata.dat   20,777,944 B
```

**跑本机 `Cpp2IL 2022.1.0-pre-release.21`（`D:/2/unity_run_ref/.../Cpp2IL/Cpp2IL.exe`）**：

```bash
Cpp2IL.exe --game-path D:/4/_tmp_view/cpp2il_run/input --exe-name Warpforge \
           --output-as dll_il_recovery --output-to D:/4/_tmp_view/cpp2il_run/out --verbose
```

- 首次失败：`--exe-name Warpforge.exe` 会被拼成 `Warpforge.exe.exe` ⇒ **`--exe-name` 不能带 `.exe`**（`:34-36`）。
- 改对后 **RC=0 / 6 s**，但用 `System.Reflection.Metadata` 逐方法 census（探针见 §2.5）：

```
methods=21633  withBody=21131  totalIL=42262
body histogram: 147A × 21131     ← 100% 是 `ldnull; throw`
```

**根因（源码实证，非推断）**：从上游取本机这只 exe 内嵌的**同一 commit** `58fc404a`
（版本串自带该哈希）的源码，`Cpp2IL.Core/OutputFormats/AsmResolverDllOutputFormatIlRecovery.cs`
在那一版**只有 24 行**，`FillMethodBody` 与 `AsmResolverDllOutputFormatThrowNull.cs` **逐字相同**：

```csharp
// 58fc404a 版：所谓 "IL Recovery" 就是 throw null 兜底
methodDefinition.CilMethodBody = new();
instructions.Add(CilOpCodes.Ldnull);
instructions.Add(CilOpCodes.Throw);
```

⇒ **与 metadata v31 / Unity 6 **无关** —— 那一版的 IL 恢复功能本身没实现**（上游 commit
`d0e3628e8880` 2026-07-25「Core: Fix dll_il_recovery being completely broken」才修）。

**但底层分析是好的**：同一只 exe 的 `--output-as isil` **9 s** 产出 **18,322 个 `.txt` / 447 MB**，
逐方法的原生反汇编 + ISIL 都正确（例：`BattleManager::get_playerData` 的 ISIL 16 条，含 `typeof(...)` / `Call`）。

### 2.2 自建 development 版（可复现步骤 + 所需补丁）

```bash
# 1) 取源码（tarball；api.github.com 120 s 超时只下到 3.96 MB，tar 报 Unexpected EOF，
#    但大部分文件已解出，缺 WasmDisassembler/ 5 个文件 —— 逐个 raw 补齐即可，不用 git）
curl -sSL https://api.github.com/repos/SamboyCoding/Cpp2IL/tarball/development -o dev.tar.gz
tar xzf dev.tar.gz          # → SamboyCoding-Cpp2IL-b5ad444/

# 2) 补缺的 5 个文件（WasmDisassembler/*.cs + .csproj）

# 3) 两处补丁（必须，否则编不过）
#    ① Cpp2IL.Core/Cpp2IL.Core.csproj：Disarm 2022.1.0-master.117 → 2022.1.0-master.99
#       （nuget.org 上最高只有 .99；.117 在作者的私有 feed，未配置凭据）
#    ② Cpp2IL.Core/InstructionSets/NewArmV8InstructionSet.cs：删掉 4 个 Arm64Mnemonic 分支
#       （BTI_C / BTI_J / BTI_JC / UDF —— Disarm .99 的枚举里没有；**纯 ARM64**，x86 路径不受影响）

# 4) 用 .NET 10 SDK 编（dev 分支 TargetFramework = net10.0；本机 SDK 在 D:/2/tools/dotnet10）
D:/2/tools/dotnet10/dotnet.exe build Cpp2IL/Cpp2IL.csproj -c Release -f net10.0
# → RC=0，4 s
```

### 2.3 真跑（完整命令行 + 耗时）

```bash
D:/2/tools/dotnet10/dotnet.exe \
  D:/4/_tmp_view/cpp2il_run/src/SamboyCoding-Cpp2IL-b5ad444/Cpp2IL/bin/Release/net10.0/Cpp2IL.dll \
  --game-path D:/4/_tmp_view/cpp2il_run/input --exe-name Warpforge \
  --output-as dll_il_recovery --output-to D:/4/_tmp_view/cpp2il_run/out_dev
```

- **RC=0 · 16.7 s**（含 binary 解析 0.13 s · 模型 1.54 s · 输出 14.5 s）
- 自报：`100% of methods successfully decompiled (55025 / 55029)`
- 产物：`D:/4/_tmp_view/cpp2il_run/out_dev/` —— **129 个 DLL / 34 MB**，
  `Assembly-CSharp.dll` **9,783,808 B**（对照 stub 版 4,716,544 B）
- 告警 9 条（全部是「栈状态不收敛 / 方法过大」，点名了方法）：
  `RemnantAeldari::Toggle` · `PriceDisplay::Setup` · `UIResourceCounter::Initialize` ·
  `UnityEngine.UI.Extensions.ScrollSnap::OnEndDrag`（`Stack state not settling`，各 500,001 blocks）；
  `NameGenerator::.cctor`(51,794 B) / `I2.Loc.GoogleLanguages::.cctor`(33,484) /
  `PlayFab.Events.PlayFabEvents::OnProcessingEvent`(33,064) / `StructWrapperPools::.cctor`(30,538)（过大跳过）

### 2.4 成功率（`Assembly-CSharp` 全量 census，不止抽样 200）

判据 = 逐方法读 `MethodBodyBlock.GetILBytes()` 分类：
`real` = IL > 4 B 且不是失败兜底；`failure-stub` = 末尾 `throw` + 中间有 `ldstr`/`newobj`；
`trivial` = IL ≤ 4 B。

| 项 | 数 | 占比 |
|---|---|---|
| 方法总数 | 21,634 | — |
| 有方法体（RVA≠0） | 21,132 | — |
| **真 IL（>4 B 且非失败兜底）** | **20,526** | **97.1% 的有体方法** |
| 其中含 call / 字段 / 算术 / ldstr 之一的 | 20,162 | 98.2% 的真 IL |
| `trivial`（≤4 B） | 603 | 2.9% |
| ├ 纯 `ret`（`2A`） | 602 | |
| └ `ldnull; ret`（`142A`） | 1 | |
| **失败兜底** | **3** | 0.014% |
| IL 总字节 | 4,548,742 | 平均 215 B/方法（stub 版 67,538 B / 3.2 B） |

抽样样例（随机 15 条，含长度与方法名）：`MatchData::SetBotDeckRandomCardLevels` 1044 B ·
`BoostMenu::ShowAllBoosters` 1502 B · `<Initialize>d__14::MoveNext` 178 B ·
`SwapSubmeshesMaterial::SwapSubMeshes` 636 B · `<>c::<get_AllRemoteLabels>b__15_0` 53 B …

**`ret`-only 那 602 条是不是「丢体」？抽了 `NetworkingPeer::OnMessage`**：
ISIL dump 里它的原生反汇编就是 `ret 0`、ISIL 只有 `Return` ⇒ **恢复是忠实的**，
但「原版真的是空方法」还是「IL2CPP 把指针折叠到了共享 `ret`」（与 W60 §2.3 的地址折叠同族）**未证**。

### 2.5 能否喂给现有反编译链

**不是同一条链**：现有正本是 Ghidra 反编译原生代码（`D:/2/tools/decomp_full/*.c`，25,096 个），
而 IL 恢复产物是**托管 DLL**，要另配一个 IL→C# 反编译器。本次现装 **ILSpy 11.1.0** 验证：

```bash
D:/2/tools/dotnet10/dotnet.exe tool install ilspycmd --tool-path D:/4/_tmp_view/cpp2il_run/tools   # 6 s
./tools/ilspycmd.exe -t CurrencyUI out_dev/Assembly-CSharp.dll
```

产出可读 C#（节选）：

```csharp
public int GetCurrencyAmount()
{
    //IL_0041: Expected I4, but got Obj
    CurrencyItem currencyItem = item;
    if ((object)item != null) return currencyItem.amount;
    NullReferenceException ex = new NullReferenceException();
    return (int)ex;                 // ← ILSpy 对「不返回的 throw 路径」的渲染瑕疵
}
```

**三方语义对照（同一方法）**：Ghidra `.c`（`D:/2/tools/decomp_full/CurrencyUI__GetCurrencyAmount.c`）

```c
if (*(longlong *)(param_1 + 0x60) != 0) return *(undefined4 *)(*(longlong *)(param_1 + 0x60) + 0x44);
/* WARNING: Subroutine does not return */ FUN_1803f47a0();
```

⇒ **`item!=null ? item.amount : throw`**，三者一致。
**结论：可喂给「IL 反编译」这条链；它不能直接喂 Ghidra，也不需要**（两条链互补：Ghidra 覆盖 25k 个原生函数，
IL 恢复覆盖 20.5k 个 `Assembly-CSharp` 方法，后者**带真实类型/字段名**，可读性优于 `.c`）。
⚠️ ILSpy 有明显**栈类型推断瑕疵**（`Expected I4, but got Obj`），个别方法会渲染错（如上面 `return (int)ex`），
**当参考用、别当判据**；关键结论仍要与 `decomp_full` 对账。

### 2.6 产物路径

| 路径 | 内容 |
|---|---|
| `D:/4/_tmp_view/cpp2il_run/input/` | 4 个输入文件（拷贝，只读源在 `D:/2`） |
| `D:/4/_tmp_view/cpp2il_run/out/` | 本机 release .21 跑 `dll_il_recovery`（**全 stub，仅作反例**）12 MB |
| `D:/4/_tmp_view/cpp2il_run/out_call/` | 同上 + `callanalyzer`（仍全 stub） |
| `D:/4/_tmp_view/cpp2il_run/out_isil/` | `isil` 输出 447 MB / 18,322 txt（底层分析可用） |
| **`D:/4/_tmp_view/cpp2il_run/out_dev/`** | **真恢复产物，129 DLL / 34 MB** |
| `D:/4/_tmp_view/cpp2il_run/src/SamboyCoding-Cpp2IL-b5ad444/` | development 源码 + 补丁；`Cpp2IL/bin/Release/net10.0/Cpp2IL.dll` = 可用可执行体 |
| `D:/4/_tmp_view/cpp2il_run/probe/` | 方法体 census 探针（`ilstat`，`dotnet run -c Release --no-build -- <dll>`） |
| `D:/4/_tmp_view/cpp2il_run/tools/ilspycmd.exe` | ILSpy 11.1.0（`dotnet tool install`，装进 `_tmp_view`） |

---

## 3. 没查清的部分（如实记）

1. **602 条 `ret`-only 的成因**：抽样 ISIL 显示原生体就是 `ret 0`（忠实恢复），但**没有**证明它们是「原版空方法」
   还是「IL2CPP 共享 `ret` 折叠」；要证需要拿 `all_methods.txt` 的地址去核这些方法是否共用同一地址。
2. **3 条失败兜底**的具体方法名未取（只在 census 里计数；探针能打名字，但没加 `--failure` 输出）。
3. **恢复 IL 的语义精度未做大规模对账**：只做了 1 个方法的三方对照（`CurrencyUI::GetCurrencyAmount`）。
   要真当判据用，应按类抽 ≥30 个方法与 `decomp_full` 比。
4. **其他 128 个 DLL 的方法体统计没做**（只量了 `Assembly-CSharp`，也就是游戏逻辑主体）。
5. **自建版的可复现性依赖两个非官方补丁**（§2.2 ①②）——补丁只落在 `_tmp_view`，**没有**进仓库；
   若以后要长期用，得把这两个补丁的判据记进工具文档（当前 `d:/2` 只读，无法放 `d:/2/tools/`）。
6. **`isil` 输出 447 MB** 留在 `_tmp_view` 里，属临时产物，可随时删。

---

## 4. 改动清单

| 文件 | 改动 |
|---|---|
| `d:/4/Unity/工具/check_prefab_refs.py` | `DEFAULT_FIELDS` +`m_Mesh`,`m_Texture`（`:52-53`）；文件头补 2 段知识（`:13-19` 现读数与 `m_Material` 说明 · `:33-39` 内建资源形状的处置口径）；用法示例 +1 行（`:29`）。**判据/输出/退出码/`--fields` 语义未动** |
| `d:/4/Unity/资料/普查产出_1021/W61b_工具_默认字段与cpp2il.md` | 本报告（新建） |
| `d:/4/_tmp_view/cpp2il_run/**` | 输入拷贝 / 4 次运行产物 / 自建源码与补丁 / census 探针 / ilspycmd（全部临时） |

未动 git；未动 `MyGame/Assets/**`、`CLAUDE.md`、`项目任务.md`、`任务线/**`；未跑 Unity。

---

## 5. 顺手发现（只报不改）

1. 🔴 **`Explosion Hive Fleet Arrival Explosions only.prefab` 有 7 条 `SkinnedMeshRenderer.m_Mesh` 坏引用**
   （`guid` 全 0、`fileID` 非 0；行 15050 / 149311 / 310755 / 330718 / 395217 / 405488 / 450226；文件 mtime 10-10 16:07）。
   C 的 `W61b_特效_ShapeModule导出.md` §2.1 写的是 **`ShapeModule` 口径 45 条 / 12 件**，**不含这 7 条**；
   `EffectExporter` 这次补的 `ShapeModule` 分支**也不会碰 `SkinnedMeshRenderer`** ⇒ 这是一笔**新账**（特效线）。
2. **`m_Material` 仍不在默认集**：`资料/交接_第六十一会话.md:86`、`:23` 与 `任务线/工具文档线/任务总表.md:24`
   都写「默认字段集看不到 `m_Material`」，而 `A1325` 的 `Image.m_Material` 那一跳（5 条）就靠它验。
   本次按简报只补 mesh/texture，**建议下一轮把 `m_Material` 也并入默认集**（该字段全仓现读只在 `CardPrefab` 有事，
   不会炸量）。
3. **C 报告 §2.1 的「45 条 / 12 件」是补前快照**：现在 `ShapeModule` 侧是 **40 条 / 7 件**
   （`ShieldTraitEffect_Idle` 5 个变体已在 23:18 的重导里归零），报告本身没错，是**时间差**。
4. ⚠️ **本轮全仓扫描期间，仓库正被主对话重导覆盖**：`ShieldTraitEffect_Idle*.prefab` mtime = **23:18:49**、
   `Buff_Sororitas_quick_fire*.prefab` = **23:18:51**（我在 23:19 与 23:30 两次扫的 `m_Mesh` 数字
   从 52→47、`m_Texture` 从 2→0 就是这个原因）。**任何「现读数」都带时刻**，报告里已标时间戳。
5. **`Cpp2IL` 的 `--exe-name` 不能带 `.exe`**（会拼成 `.exe.exe`）—— 2022 版 help 没写。
6. **`Cpp2IL` 的 `dll_il_recovery` 在 release .21 上是死功能**，而本机 MelonLoader 正是随包带的这一只；
   谁再拿 `cpp2il_out/` 当「方法体恢复失败」的证据，会容易归错因（真正原因在源码：那两个 OutputFormat 类是同一份代码）。
