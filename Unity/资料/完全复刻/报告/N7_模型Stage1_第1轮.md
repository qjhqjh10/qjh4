# N7 · 属性模型重构 Stage 1（只加投影、零行为变化）—— 第 1 轮

> 任务来源：调度台 2026-10-25 裁定「**方案 A · 跟原版做投影**」，分 Stage 1 / 2a / 2b / 2c。
> 本轮**只做 Stage 1**：在 `UnitState` 上加只读投影、在 `RuleCore` 上加 `CurrentCostOf`，
> **旧字段原样保留、没有任何调用者**。
> 判据 = `Unity/资料/完全复刻/判据/战斗逻辑驱动_前置门与现算口径.md` §六（施工清单）/ §6·5（六条口径）
> ＋ 交接 `Unity/资料/完全复刻/报告/Q2_前置门_第1轮.md` §⑨（8 条坑）。
> 原版判据一律现读 `d:/2/tools/decomp_full/EntityScript__get_*.c`（第一权威）。

---

## ① 结论

| 新增 | 落在 | 对应原版方法（判据） | Stage 1 的体 |
|---|---|---|---|
| `CurrentMeleeAttack` | `UnitState` | `EntityScript.get_CurrentMeleeAttack`（`EntityScript__get_CurrentMeleeAttack.c:29-67`） | `return Attack;` |
| `CurrentRangeAttack` | `UnitState` | `get_CurrentRangeAttack.c:24-70` | `return RangedAttack;` |
| `CurrentMaxHealthProcessed` | `UnitState` | `get_currentMaxHealthProcessed.c:22-108` | `return MaxHealth;` |
| `CurrentIntrinsicHealth` | `UnitState` | `get_CurrentIntrinsicHealth.c:26-50` | `return Health;` |
| `CurrentIntrinsicMeleeAttack` | `UnitState` | `get_CurrentIntrinsicMeleeAttack.c:22-46` | `return Attack;` |
| `CurrentCostOf(ctx, owner, inst)` | `RuleCore` | `get_CurrentCost.c:30-92` | `return CostOf(ctx, owner, inst);` |

- **零行为变化**：以上 6 条**没有任何调用者**（本轮只加、不换读点、不换写点，`Attack` / `RangedAttack` /
  `MaxHealth` / `Health` 的读写路径一个字没动）。②「恒等」不靠自觉 —— 见 §④ 的两组实测。
- **`CurrentCost` 为什么不是 `UnitState` 属性**：它要 `ctx`（`CostMods` / 静态条件降费）与 `owner`
  （修正作用在哪一方手牌），只能落在 `RuleCore`（判据 §6·5 末注）。
- ⚠️ **`get_CurrentMeleeAttack` 的「命中即返回」口径已逐字落进注释**：
  `buffType == 70`（`setMeleeAttack`，反编译写作 `0x46`）⇒ `return` 它的 `meleeAttackChange(+0x48)`，
  **连 base 与已累加部分都不算**；因为**正序**遍历 ⇒ 多条时**列表里第一条**胜出。
  可观察后果 = 「一旦被 set 过，之后叠 `addStats(+1 melee)` 不涨」（判据 §6·4① 的验收用例）。
- **Stage 1 故意没做的两条**（都写进了注释，防后面的人「顺手补上」）：
  1. `IsBlind ⇒ CurrentRangeAttack = 0`（`get_CurrentRangeAttack.c:24-28`）**没加** ——
     加了就不恒等（真 Play 给单位上 blind 就会差）。它**归 2c**，届时 §7·2 那 11 组读点一次性全对。
  2. 修饰表 / 效果记录**没建**（判据只要求 Stage 1 加投影）—— 建它就是替 Stage 2/3 定数据结构，越界。
- **⚠️ 给 2c 的硬提醒（已写进代码注释）**：2c 会把 `Attack` / `RangedAttack` / `MaxHealth`
  换成**转发到本段属性**的形式 ⇒ 那时这几条投影**必须同时改写成「读 `*Base` + 查修饰表」**，
  否则 `Attack → CurrentMeleeAttack → Attack` **无限递归（栈溢出）**。

---

## ② 判据与证据

### 2·1 原版方法体（本轮逐句复读，`d:/2/tools/decomp_full/`）

| 事实 | 出处 |
|---|---|
| `CurrentMeleeAttack`：正序扫 `activeEffects(+0x108)`；`buffType(+0x38)==1` 累加 `+0x48`；`==70` **立即 return** `+0x48`；扫完 `clamp0(currentBaseMeleeAttack(+0x7c) + Σ)` | `EntityScript__get_CurrentMeleeAttack.c:29-51` / `:44-47` / `:52-66` |
| `CurrentRangeAttack`：**第一句** `HasCurrentTrait(0x3cf /*blind*/) ⇒ return 0`；`buffType==1` 累加 `+0x50`；`==85`（`0x55`）立即 return；`clamp0(+0x90 + Σ)` | `EntityScript__get_CurrentRangeAttack.c:24-28` / `:41-49` / `:50-64` |
| `currentMaxHealthProcessed`：`base = +0xa4`；**第一趟倒序** 非extrinsic(+0x30==0)∧`buffType==1`∧`healthChange(+0x60)≠0` ⇒ `base +=`；**第二趟倒序** extrinsic 那批 ⇒ 另加到 `ext`，同趟里 `buffType==9`(`setHealth`)∧`≠0` ⇒ **`base` 整项被覆盖**；`return base + ext`，**无 clamp** | `EntityScript__get_currentMaxHealthProcessed.c:22-32` / `:33-59` / `:60-107` / `:65,:107` |
| `CurrentIntrinsicHealth` = `currentHealth(+0x68)` − Σ(extrinsic ∧ `buffType==1` 的 `+0x60`)，正序、无 clamp | `EntityScript__get_CurrentIntrinsicHealth.c:53-55` / `:49-50` |
| `CurrentIntrinsicMeleeAttack` = `CurrentMeleeAttack` − Σ(extrinsic ∧ `buffType==1` 的 `+0x48`)，正序、无 clamp | `EntityScript__get_CurrentIntrinsicMeleeAttack.c:29-39` / `:44-45` |
| `CurrentCost`：正序；`buffType==2`(`changeCost`) 累加 `costChange(+0x68)`；`==95`(`setCost`,`0x5f`) ⇒ **累加值被替换成它的 `costChange` → 整表重扫一遍把所有 `buffType==2` 再加一次 → `clamp0` 直接 return**；没遇到 `setCost` 才 `+ rawCardData.manaCost(+0x28→+0x78)` 再 `clamp0` | `EntityScript__get_CurrentCost.c:49-51` / `:52-82`（`:66-73` 是那个 return）/ `:84-91` |

### 2·2 我们侧（改后行号）

- `Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs` —— 新增块从 `public int MaxHealth;` 之后开始
  （说明块 `:60` 起），五条投影在 `:116`（`CurrentMeleeAttack`）/ `:135`（`CurrentRangeAttack`）/
  `:162`（`CurrentMaxHealthProcessed`）/ `:174`（`CurrentIntrinsicHealth`）/
  `:190`（`CurrentIntrinsicMeleeAttack`）。
- `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs:951-990` —— `CurrentCostOf(ctx, owner, inst)`
  （方法体 `:987-990`），紧挨着它转发的 `CostOf(ctx, owner, CardInstance)`（`:946-949`）。

---

## ③ 改动清单

| 文件 | 改动 | 行数 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/UnitState.cs` | **只加**：一段「Stage 1 说明 + 五条只读投影」的注释与属性块（插在 `MaxHealth` 字段与 `Exhausted` 字段之间）。**未改任何既有成员** | +133 / −0 |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **只加**：`public static int CurrentCostOf(BattleContext, int, CardInstance)` + 它的 doc（插在两个 `CostOf` 重载与私有 `CostOf` 核心之间）。**未改 `CostOf` 任何一行** | +41 / −0 |
| `d:/4/_tmp_view/n7/probe/{n7probe.csproj,Probe.cs,Main.cs}` | 临时恒等验收探针（**不进工程、不进 git**；只编 `RuleEngine/Core/*.cs` + `Data/CardDatabase.cs` + ruleprobe 的 `Stub.cs`） | 新增 |
| `d:/4/_tmp_view/n7/*.log|*.tsv|*.txt` | 改前/改后基线、探针读数 | 新增 |

行尾：两个 `.cs` 都是 **LF**（改前 `CRLF 0 / LF 1510`、`CRLF 0 / LF 13488`，二进制数的），
本轮用 Edit 就地插入，`git diff --numstat` 的删除数为 **0 ⇒ 没有整篇翻行尾**。

---

## ④ 核验

### 4·1 秒级类型检查

```bash
TMPDIR=/tmp/wf_n7 bash d:/4/Unity/工具/typecheck.sh
# --- 运行时程序集 --- 运行时错误数: 0
# --- 编辑器程序集 --- 编辑器错误数: 0
```
（**改前**也先跑了一次同样 0/0，作为对照。）

### 4·2 恒等验收 · 逐单位（新投影 == 旧字段）

命令：
```bash
cd /d/4/_tmp_view/n7/probe && dotnet build -v:q --nologo
dotnet "D:/4/_tmp_view/n7/probe/bin/Debug/net8.0/n7probe.dll"
```
输入局面 = **全池 1128 张卡逐张 × 6 个局面**（`cards_engine.json` 现读张数 = 1128）：
`s0` 刚建出来 · `s1` 模拟永久 `addStats`（`+3 近战 / +2 远程 / +2 生命上限 / +2 生命`，**直接写字段**）·
`s2` 受伤 3 · `s3` 上 `blind`（🔴 Stage 1 故意不把远程归零 ⇒ 仍须恒等）·
`s4` 上 `armour`/`bastion` 关键词并加 `Armor` · `s5` 上限压到 1（原版 processed 无 clamp）。

比对 5 项：`Attack==CurrentMeleeAttack` · `RangedAttack==CurrentRangeAttack` ·
`MaxHealth==CurrentMaxHealthProcessed` · `Health==CurrentIntrinsicHealth` · `Attack==CurrentIntrinsicMeleeAttack`。

读数（`_tmp_view/n7/identity.log`）：
```
卡池 1128 张
单位 1128 × 局面 6768 ⇒ 逐项比对 33840 次 · 差异 0 条
费用 1128 张 × 4 种 ctx ⇒ 逐值比对 4512 次 · 差异 0 条
样例 Autarch：Attack=9 CurrentMeleeAttack=9 / RangedAttack=6 CurrentRangeAttack=6 / MaxHealth=6 CurrentMaxHealthProcessed=6
=== 恒等验收：全绿（差异 0）===      (rc=0)
```
费用那 4 种 ctx = `CostMods` 空 / 一条 `Player=0,Key="*",Delta=-1` / 一条 `Player=1,Key="*",Delta=+2` / `ctx=null`。

### 4·3 恒等验收 · 端到端（全池真打，逐字节）

改前先留基线，改后同一条命令再跑一次：

```bash
bash d:/4/Unity/工具/ruleprobe.sh check
bash d:/4/Unity/工具/ruleprobe.sh play all dump <out.tsv> pf <out.txt>
```

| 项 | 改前 | 改后 | 结论 |
|---|---|---|---|
| `ruleprobe check` | `解析差异 0 行 · 哨兵合计：失败 0 条`（rc=0） | 同（rc=0） | 解析层零变化 |
| `play all` 结算指纹 `pf` | `a2a94183251db16af36d0a00cd1625ed` | **同** | **逐字节相同** |
| `play all dump` 全池读数 | `351c95043ba194dd7c6026de94ac14f6` | **同** | **逐字节相同** |

⇒ Stage 1「零行为变化」有**两层证据**：逐单位（33840 次比对）＋ 全池真打（1121 张的结算指纹逐字节相同）。

---

## ⑤ 没查清（如实）

1. **原版 `activeEffects` 为 `null` 时**，上面这 6 个 getter 全都走 `FUN_1803f47a0()`（`/* Subroutine does not return */`，
   看形状是 il2cpp 的**空引用抛出**）。⇒ 原版「列表恒非 null（至少是空表）」，但**谁保证的**（构造函数？）本轮**没去追**。
   对我们**无影响**（我们的场单位不靠这张表），只是 Stage 3 建表时别用 `null` 表达「空」。
2. **`CurrentIntrinsicRangeAttack` 没加** —— 同族方法体本轮**没读**（判据 §3·5② 已记「未证」），
   ⛔ 不按「melee 对称」猜着写。要用它得先读那个 `.c`。
3. **`setHealth(9)` 那条记录到底由谁生成**：原版 `currentMaxHealthProcessed` 里 `buffType==9` 是「覆盖 base」，
   而池内 `setHealth` 记录 = 0（判据 §3·1 已记）。⇒ Stage 1 这条分支**永远是空转**，本轮没再挖它的生成端。
4. **`buffType` 数字 ↔ 名字**：本轮只用到 `1/2/9/70/85/95`，与判据 §3·1 一致；但 `9`/`70`/`85`/`95`
   的反编译写法分别是 `9`/`0x46`/`0x55`/`0x5f`（十进制 = 70/85/95），已逐条对上，**没再核枚举原文**。

---

## ⑥ 顺手发现（只报，**没有自己顺手改**）

### ⑥·1 🔴 判据文件 §3·3·5 / §6·5 的 `setCost` 公式**推导不成立**（反编译 + 数据双向证伪）

- **判据原文**（`战斗逻辑驱动_前置门与现算口径.md:280` 与 `:593`）写的是
  `CurrentCost = clamp0(rawManaCost + setCost.costChange + Σ(全部 changeCost))`，并说
  「`costChange` 按 `N − manaCost` 生成（**推导，未二次核**）」。
- **反编译实际控制流**（`EntityScript__get_CurrentCost.c`）：`setCost` 那一支在 `:66-73` **就 `return` 了**，
  而 `+ rawCardData.manaCost`（`+0x28 → +0x78`）在 `:84-91`，**根本走不到**
  ⇒ 有 `setCost` 时 = **`clamp0(setCost.costChange + Σ 全部 changeCost)`，不含 base**。
- **数据侧现证「`costChange` 存的就是目标的绝对费用 N」**（`Unity/数据/游戏数据/cards_truth.json`，现读）：

  | 卡 | 自己的 `manaCost` | `buffType==95` 的 `costChange` | 卡面英文 |
  |---|---|---|---|
  | `GSC39 Lying in Wait` | 2 | **1** | `… reduce its cost to 1` |
  | `TAU68 Trail Shaper` | 3 | **1** | `Lower its cost to 1` |
  | `TL69 Neurotyrant` | 6 | **1** | （复制品费用 1） |

  三条都等于目标绝对值 1、且与各自的 `manaCost` 无关 ⇒「`N − manaCost`」的推导**是错的**；
  反过来，正因为 `costChange = N` 且那一支不加 base，`CurrentCost` 才恰好等于卡面写的 N。
- **影响面**：我们今天的实现（`EffectResolver.DoLowerCost` 快照 delta `d = N − 当时 CostOf`）**在这三张卡上结果正确**；
  会分叉的场合仍是「同一条链里还有别的 `changeCost`」——但**分叉原因与判据写的不同**（不是「N−base 折进去」，而是
  「原版 = N + Σ 全部 changeCost vs 我们 = N」）。
- **建议**：Stage 3 落地前把 §3·3·5 / §6·5 那两处公式就地订正（铁律 5），并补一条「`costChange` 是绝对值」的判据注。
  本轮**只在自己的代码注释里写了订正与证据**，**没动判据文件**（不在白名单）。

### ⑥·2 现读口径提醒（不是缺陷）

- `cards_engine.json` 当前 **1128** 张（判据/记忆里写的是 2026-10-15 的 1127）—— 引用张数照旧现读，别写死。
- `ruleprobe` 的 `check` 口径是 **1121** 张（那是「解析基线覆盖到的池」，与 `cards_engine.json` 的 1128 不是同一口径）。

### ⑥·3 一条给 Stage 2 的提醒（本轮没做，只是看见）

`UnitState` 里 `MaxHealth` 目前同时承担「原始上限 + 已处理上限」两格（`EffectResolver.ApplyOneGain` 的
`health` 支直接 `MaxHealth += val`），而 `RevertBuffs` / `RemoveBuffsFromCard`（`UnitState.cs` 的 `health` 支）
**是 `Health -= / MaxHealth -=`**。这与判据 §7·4 记的「原版摘 `addStats.health` 是
`currentHealth = min(currentHealth, 新上限)`、**不减**」是同一个差异 —— 归 Stage 3，**本轮没碰**。
