# ruleprobe —— 引擎离线对拍探针

> 🔴 **它是【快速内环 / 旁证】，正式验收仍然是 `RuleEngineTest.Run`（断言的真宿主）。**
> 措辞照本家族先例 `D:/tmp/wf_b29_probe/Program.cs:2`。
> 长文档（原理 / 桩的边界 / 已知的坑）→ `资料/引擎离线对拍_探针.md`。

## 它解决什么

`RuleEngine/Core/` 按工程**成文规矩**只碰 `UnityEngine.Debug` / `UnityEngine.Random`
（`TutorialScript.cs:344-352` 把它写成判据，Core 各文件头注逐条重申）⇒ 三十行桩就能把**整个 Core**
编成控制台程序，**不跑 Unity、不占 Unity 实例、不用等 1–9 分钟编译**。

实测：**重编 ≈ 1.5 秒**，`check` 全池一趟 **≈ 2 秒**。对比 `_run_8_checks.sh` 的 1–9 分钟 / 条。

## 怎么跑

```bash
bash d:/4/Unity/工具/ruleprobe.sh            # check：与基线对拍，打一行摘要（默认）
bash d:/4/Unity/工具/ruleprobe.sh b19test    # 45 条 B19 断言
```

判据 = **看那行 `=== 全池 … 解析差异 N 行 … ===`**：

| 摘要 | 含义 | 绿红 |
|---|---|---|
| `解析差异 0 行` | 解析器没动 | ✅ 绿 |
| `改名 K 行`（解析差异 0） | 本工程每批都在改卡名 / 重跑产物 —— **通常无害** | 🟡 黄，看一眼 |
| `解析差异 > 0` | 引擎解析器真变了 | 🔴 红，**逐行看**（是修基线还是修解析器） |

```bash
# 手搓一条语句看解析结果
dotnet bin/Debug/net8.0/wfprobe.dll seg "<卡面英文原文>"
# 看某张卡
dotnet bin/Debug/net8.0/wfprobe.dll card "<卡名>"
# 生成一份全池 dump（基线就是这么来的）
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

## 基线怎么重建（**重要**）

⛔ **别拿「逐字节相同」当判据** —— 卡名是**会变的产物**（B16/B20 两批共改 6 张卡名），
而 `scan` 的第一列含卡名 ⇒ 拿它当基线，**第一天就红、而且红的是噪声**，会把「红了不用管」训练成习惯。
所以摘要行**把两个数分开**：第 1 列（`id|name`）变 = 改名；第 2 列往后变 = 解析差异。

**确认解析器的改动是对的之后**（用 `RuleEngineTest.Run` 或真机验过），才重建基线：

```bash
cd d:/4/Unity/工具/ruleprobe
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

## 桩够不够（2026-10-18 实测口径）

**真实可编译的 UnityEngine 面 = 4 个成员**（`Debug.Log` / `LogWarning` / `LogError` + 类本身，11 个调用点）。
`UnityEngine.Random` 有 10 处引用但**全是注释**（代码刻意走 `ctx.Rng`）。

🔴 **2026-10-18 更新（`A941` 之后，这条已闭环）**：Core 里**已经没有任何 Unity 类型**了 ——
原来 `Resources` / `TextAsset` / `JsonUtility` 那三处（在 `TutorialScript.cs` 的 `#if UNITY_5_3_OR_NEWER` 里）
已整段搬进 **`Data/TutorialDatabase.cs`**，换乘点是一个「**半个 `partial` 方法**」：
`Core/TutorialScript.cs` 只写 `static partial void LoadFromUnity();`（**无实现**）⇒
**C# 会把「只有声明的 `partial void`」连同调用点一起抹掉** ⇒ 探针侧自动落回兜底、Unity 侧真装载。
⇒ **两边都能验，而且不再靠 `#if`**。`Editor/RuleEngineTest.cs` 的 `TestCoreLayerPurity` 现在**守着这条边界**。

🔴 **但 `.csproj` 里仍然别加 `<DefineConstants>UNITY_5_3_OR_NEWER</DefineConstants>`** ——
定义它今天已不再有影响，可**探针要的就是「Core 能单独编」这条边界**；定义符号会让这条边界悄悄失效
（下次有人往 Core 里加 Unity 调用时就不会被发现）。

## 三个坑（收编时全部修掉了，别再踩回去）

| 坑 | 症状 | 修法 |
|---|---|---|
| `scan` 的 `unparsed` 列打成类型名 | `.Append(List<string>)` 命中 `Append(object)` ⇒ 每张卡都是 `System.Collections.Generic.List\`1[System.String]`，**「没解析出来的碎片」这列被整个丢掉** | `string.Join(",", unparsed)` |
| 中文输出 GBK | 本机默认编码 ⇒ 所有中文乱码、摘要行读不了 | `Console.OutputEncoding = UTF8` |
| 退出码恒 0 | `Main` 是 `void`、无 `Environment.Exit` ⇒ **FAIL 45 条也退 0** | `Main` 改 `int` + `return` |

## 来源与边界

- **收编自** `D:/tmp/wf_b14_probe/`（2026-10-18；那份住临时目录、**迟早被清**）。同族探针在 `D:/tmp/` 下曾有 **18 个**（每批重造一个）—— 这个就是用来止住那件事的。
- 卡池路径**由仓库根推出来**（`Program.ResolveOutDir()` 从程序集往上找 `MyGame/Assets`），⛔ 不写死盘符。
- `_专题/seg_any_1016.cs.txt` = 更早那份 `_tmp_view/seg_any_probe/`（`GivePayload.ParseInto` 的 `any` 语义影响面），
  ⚠️ **它是【一次性专题】，不是通用盘**；`_tmp_view/` 在 `.gitignore:154` 里被忽略 ⇒ 搬过来**先保命**。
