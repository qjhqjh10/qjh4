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

## dump 的列（🆕 `A1368`：op 尾部那四列，2026-10-10）

一条记录 = `id|name|unparsed|partial|op;op;…`，每条 op =
`verb/chooseSrc/chooseWhat/chooseAct/amount/payload` **＋ 尾部四列**：

| 列 | 从哪来 | 形状 |
|---|---|---|
| `cost` | `EffectOp.Cost` | 整数，`0` = 不付费 |
| `shared` | `EffectOp.CostShared` | **`1`/`0`**（`1` = 这份代价是同组**别人**付的，本 op 不再收钱） |
| `condKind` | `EffectOp.ConditionKind` | 条件的**规范名**，空 = 没条件/认不出 |
| `countRef` | `EffectOp.CountRef` | `for each …` 数谁，空 = 不计数 |

🔴 **为什么加**（台账 `A1368`）：这四样原来**一个字都不打** ⇒ 凡改「付费 / 条件 / 计数」三族的活，
`check` 都给不出信号。第十三会话 `W6b` 撞过一次：它写的一个回归让 **28 张**卡的 `cost` 全变 0，
**`check` 全绿**，是它另建的一次性副本才看出来的。
**构造性实测（2026-10-10）**：拿一份**仓库外**的 Core 副本、只把主分支那句 `StampPaidCost` 的费用改成 0
（**只动 `Cost` 一列**）⇒ **旧 dump 逐字节零差异（旧 `check` 完全瞎）**；**新 dump 81 行差异（`check` 红）**。

⚠️ **`countRef` 原样打、不做转义**：`board` 族的取值自带两个 `|`（`enemy|unit|all`）——
全池 **27 个 op** 的 `countRef` 带 `|`（落在 **25 行**上）⇒ 新基线里 `|` 数 >4 的行共 **44 行**（旧基线 **19 行**）。
⛔ **别据此判「格式坏了」**：`chooseone` 的 `chooseWhat` 早就这样（旧基线那 19 行就是它），
而 `check` 只认**前两个** `|`（`KeyOf`/`DefOf`，`Program.cs:196-197`）⇒ 不受影响。
⛔ **也不许把 `|` 换成 `,`** —— 那会让 dump 的值与引擎字段**不同**，下一个人照 dump 抄进断言就抄错了
（正是本笔要治的那一族）。渲染只有一处：`Program.CountRefText`（`Program.cs:142`）。

⚠️ **`CostKind`（货币名 `ck=`）仍然不在 `scan` 列里** —— `seg`/`card` 打 `ck=`，但 `check` 看不见它
⇒ 「只改货币名（`energy`↔`faith`）」这类改动今天**还是**进不了 `check`。**建议同批补上**（一行的事），
本条**只报没做**（见 `资料/普查产出_第十三会话/W9_A1368探针补列.md` 的「顺手发现」）。

## 基线怎么重建（**重要**）

⛔ **别拿「逐字节相同」当判据** —— 卡名是**会变的产物**（B16/B20 两批共改 6 张卡名），
而 `scan` 的第一列含卡名 ⇒ 拿它当基线，**第一天就红、而且红的是噪声**，会把「红了不用管」训练成习惯。
所以摘要行**把两个数分开**：第 1 列（`id|name`）变 = 改名；第 2 列往后变 = 解析差异。

**确认解析器的改动是对的之后**（用 `RuleEngineTest.Run` 或真机验过），才重建基线：

```bash
cd d:/4/Unity/工具/ruleprobe
dotnet bin/Debug/net8.0/wfprobe.dll scan "基线/out_baseline.txt"
```

🔴 **重生成之前先问一句：这个 diff 是「我改了解析器」还是「探针的**卡名索引**掉了」？**（`A1339`）
基线里**只有 1 行**与卡名索引有关 —— `UM_Angels_of_Death`（全池唯一一张「目标靠**卡名**指」且会被
`scan` 打出来的卡）：索引在 ⇒ 第 4 列（`partial`）为空；索引掉 ⇒ 第 4 列变成
`Codex: Give +1 [Ranged] to your Primaris Intercessor`。**这一行就是那条口径的哨兵** ——
见了它别顺手重生成基线，先去看 `Program.LoadPool` 尾部那句 `CreatePool.BuildNameIndex(list)` 还在不在。
（判据：`CreatePool.cs:613` —— 没建索引时 `MatchCardName` **一律返回 null**，探针就会与引擎**不同口径**，
凡「目标靠卡名指」的句子一律报 `target=[]`。这条已经产出过两次**假证据**。）

### 卡名索引：探针必须与引擎同口径（`A1339`，2026-10-10）

引擎侧 `Data/CardDatabase.cs:113` 在读完卡表之后调 `CreatePool.BuildNameIndex(list)`；
探针**原来两处都不建** ⇒ `card`/`seg` 报 `target=[]`、`scan`/`check` 落 `partial` 列。现在：

- `card` / `filter` / `jackal` / `b19test` … —— 由 `Program.LoadPool()` 尾部建（**唯一**卡池入口）。
- `seg`（走 `Dump`）—— 同上，`Dump` 头部调了一次 `LoadPool()`。
- `scan` / `check` —— ⚠️ **这两条【不走 `LoadPool`】**（`Scan.ScanMain` 自己再读一遍 JSON）
  ⇒ 它**自己**调了一次 `Program.LoadPool()`。

🔴 **往这条链上加新入口时，问一句「它过不过 `LoadPool`」** —— 不过的（像 `Scan` 那样自己读 JSON 的）
必须自己补一次，否则就是**只补一处 = 静默偏一半**。

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
