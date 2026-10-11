# `E6` · `EffectResolver.cs` 三笔（`A1464` · `A1413` · `A1465`）—— 执行写手交件

> 写手独占文件：`d:/4/Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs`（**只改了这一个文件**）。
> ⛔ 没跑 Unity · 没跑 `typecheck.sh` · 没动 git · 没碰正本 · 没碰白名单外的任何文件。
> 编译（类型检查 / 自检）**由主对话统一跑**：本报告第 3 节的读数**除注明「离线算出」的外，都还没在引擎里跑过**。

---

## 一、改动清单（行号 = 改后现读）

| 行号 | 改了什么 | 服务 |
|---|---|---|
| `:7400-7415` | `ImplementedEffectVerbs` 的 doc：删掉过期的「（现在只剩 `repeat`/`lowercost`/`reanimate`）」括注，按铁律 5 写订正痕 + 现算结论（差集 = **空集**） | `A1464` |
| `:8007` | `DoReanimate` 开头：`bool all = op.Target == null \|\| op.Target.Count == 0;` → **`bool all = ReanimatesAll(ctx, by, op);`** | `A1465` |
| `:8100-8148` | **新增 `ReanimatesAll(ctx, by, op)`** —— 单复数改判**卡面原文**（见 §三·3） | `A1465` |
| `:233-272` | `ResolveOps`：新增 `groupUnpaid[]` 预检（**付钱那条付不起 ⇒ 同组的 `CostShared` 后随 op 一并跳过**）+ 主循环 `if (skip[i] \|\| groupUnpaid[i]) continue;` | `A1413` ② |
| `:392-399` | `ResolveOps`：`repeats <= 0 && pays[i]` ⇒ `ResolveOne(..., payOnly: true)`（**N=0 也照收一次**） | `A1413` ① |
| `:440-470` | 新增 `CostKindKnown(kind)` / `CostPrefixPayable(ctx, owner, op, kind)` 两个小助手（**货币名清单 + 余额比较只此一处**） | `A1413` ② |
| `:505-509` / `:519-521` / `:553` | `ResolveOneCore` 转调上面两个助手（行为等价，见 §二·3） | `A1413` ② |
| `:441-455` / `:487-488` / `:596-609` | `ResolveOne` / `ResolveOneCore` 加可选形参 **`bool payOnly = false`**；付费支**之后**加 `if (payOnly) return op.Cost > 0;` | `A1413` ① |

`git diff --numstat` = **187 / 14**（行尾仍是 **CRLF**，`file` 现读 `with CRLF line terminators`；⛔ 没用过 `sed -i`）。

---

## 二、每笔的判据（含「🧨 改坏法」= 什么改动能让它红）

### 1. `A1464`（注释过期）—— 纯注释，无运行时行为

- **判据原文**：`资料/普查产出_第十四会话/诊断_未归因24张.md:83`（「我没改、也没裁定」）。
- **现核**：`repeat` `:600` · `lowercost` `:599` · `reanimate` `:601` **都在 `EffectDispatch` 里**，而 `ImplementedEffectVerbs` 就是 `new HashSet(EffectDispatch.Keys)` ⇒ 括注**自相矛盾**。
- **现算的正解**：`EffectText.cs` 里 `Verb = "…"` 字面量 **41** 个 − `EffectDispatch.Keys` = **0**（`EffectText`/`CardDef`/本文件里**没有动态拼动词名**的写法，`grep '\.Verb =' 无引号` 命中 0）。
- 🧨 **改坏法**：往 `EffectDispatch` **删**一条而 `EffectText` 仍产它（差集非空）⇒ 新注释那句「现算的是空集」当场过期（所以注释里写的是**算法**而不是数字）。

### 2. `A1413` ①：`repeats == 0` 也照收一次钱

- **判据**：原版收费在 **ability 层** —— 一条 ability = 一个 `manaCost` ＋ 一串 `AbilityLogic`（`ActiveAbility.cs`），`d:/2/tools/decomp_full/BattleManager__PayActiveAbilityCostOath.c:22` 一次激活只调一次 `PlayerManager__UseMana`，**与那条 logic 有没有东西可做无关** ⇒ `for each` 数出 0 也照收。
- **实现**：`payOnly: true` 把 op 送进 `ResolveOneCore`，**只在付费支里走一遍**（查余额 / 扣 / 日志 / 灵魂石广播全部复用**同一份**），条件闸与 handler 都不走。
- 🧨 **改坏法（两条，一红一必须绿）**：
  1. `{ "deal", … }` 里对 `for each` 卡**先扣钱再数个数**，或把 `pays[i]` 判据改成 `op.Cost > 0` ⇒ 组内非首条（`CostShared`）会被**再收一次** ⇒ 双扣，夹具红；
  2. 「`repeats == 0` 时**不该落地效果**」：把 `payOnly` 那一段换成 `ResolveOne(firstOp)`（不带 `payOnly`）⇒ N=0 的 `Deal 1 damage to an enemy for each friendly troop` 会**真打 1 点**，夹具红。
  ⚠️ 另有一条**故意不动的**口径：`ok`/`done` **不因「收了钱」置真** —— 那一刻效果没落地，与改前一致（只多收钱）。

### 3. `A1413` ②：付款失败 ⇒ 同组其余 op 一并跳过

- **判据**：`d:/2/tools/decomp_full/BattleManager__CanUseActiveAbility.c` **整条 ability 判一次**余额；付不起 = 整条 ability 不激活（那一串 logic 一条都不跑）。
- **实现**：`ResolveOps` 在 `pays[]` **顺延之后**算一份 `groupUnpaid[]`（只认 `Cost > 0 && CostShared` 这一个分界标记 —— 与顺延**同一个**），主循环跳过。
  · 付钱那条**自己不跳**（照原样由 `ResolveOneCore` 打「付费不够 —— 这一条没生效」，日志口径不变）。
  · **行为等价性**：`ResolveOneCore` 那条闸由 `!waived && have < op.Cost` 改成 `!CostPrefixPayable(...)` —— `waived=true` 时 `CostPrefixPayable` 返回真 ⇒ 落 `else if (!waived)` 假 ⇒ 落最后那个「**免付**」日志支，**三分支逐条对得上**。
- 🧨 **改坏法**：
  1. 把 `groupUnpaid` 的预检**挪到 `pays[]` 顺延之前** ⇒ `instead` 把付款人换到组内另一条时，判的是**旧**付款人 ⇒ 「付得起却整组跳过」/「付不起仍白给」（两个方向都能红）；
  2. 把 `j` 的循环判据从 `Cost > 0 && CostShared` 改成「后面所有 op」⇒ 会**连下一组的 op 一起跳**（跨组误伤），夹具红；
  3. 让付钱那条**自己**也被 `groupUnpaid` ⇒ 「付费不够」那行日志消失（日志断言红）。
- ⚠️ **闸没被放开**：本笔**没碰**那 10 处 `if (t == null || !t.IsAlive) continue;`（铁律 11 的判别式原样有效）。

### 4. `A1465`：`Reanimate a friendly Remnant` 复活【全部】残骸 —— 已修（只动 `DoReanimate`，⛔ 没动动词表）

- **根因（现核）**：`ParseTarget("a friendly remnant")` **与** `ParseTarget("all friendly remnants")` **都返回 `null`** —— `EffectText.cs:7680` 那条「真不认识的词 → **不猜**」的守卫（`remnant` 既不是 kind 词、也不是卡名）⇒ **`Target == null` 分不出单复数**，而 `all = op.Target == null || Count == 0` 就把**单数卡面**当成「全体」。
- **判据 = 卡面原文**（铁律 7）。全池 `desc` 提到 reanimate 的 **16 张**，其中**只有 1 张**写复数：
  · **`SAU52 Resurrection Orb` = `Reanimate all friendly Remnants`** —— 已读成品卡图逐字核对：`d:/2/Warpforge部队卡片/Necron/4计策/Warpforge_52_Resurrection-Orb.png`（卡面逐字就是这个）；
  · 单数样板也核了一张：`…/Necron/4计策/Warpforge_26_Reanimate.png` = `Ephemeral. Reanimate a friendly ⬛ Remnant`（单数）。
- **实现**：新增 `ReanimatesAll(ctx, by, op)` —— `Target != null` 时**照旧**按 `Count == 0`（一字不变）；`Target == null` 时在 `op.Source` 的 **`reanimat… → remnant` 这一截**里找 `\ball\b`（⛔ 不看整句：尾句 `… to all enemies` 里的 `all` 会被误认成目标）。
  **认不出 ⇒ 按单数**（16 张里 15 张是单数；而「解不出就放大成全体」正是本笔要治的病），并**打日志出声**。
- 🧨 **改坏法**：
  1. 把正则放宽成 `op.Source.Contains("all")`（不带 `reanimat…remnant` 的窗口）⇒ 任何尾句带 `all enemies` 的卡会被判成「全体」，夹具红；
  2. 把「认不出」那一支改回 `return true` ⇒ 静默放宽**又回来了**（这是本笔的判别式本体）；
  3. `SAU52` 是**必须仍然全翻**的那一格：若把正则写成 `reanimates?\s+a\b…`（只认单数）⇒ `SAU52` 变成只翻 1 个，夹具红。

---

## 三、实测对照（改前 → 改后）

| 笔 | 改前 | 改后 | 读数来源 |
|---|---|---|---|
| `A1464` | 注释说「只剩 `repeat`/`lowercost`/`reanimate` 没登记」—— 三个**都在表里** | 差集 = **空集** | **离线算出**：`EffectText.cs` 的 `Verb="…"` 字面量集合 − `EffectDispatch.Keys`（`grep` + python 现算，命中 0） |
| `A1413` ① | `repeats == 0` ⇒ 能量 **Δ=0**（一分不收） | 应收 **Δ=−4**（例：`4 Energy: … for each friendly troop`，场上 0 个部队） | ⚠️ **需主对话跑**（离线驱动 / `RuleEngineTest.Run`）。改前那个 Δ=0 是 `W_A1369付费三洞.md:19` 记的**已有读数** |
| `A1413` ② | 组首「付费不够」后，后半句**照常结算**（`UM93 Fall Back` / `SOR6` 今天就是这个形状） | 组首付不起 ⇒ 后半句**一并跳过**（日志如实说） | ⚠️ **需主对话跑**（夹具：`Oath 2: Lower its cost by 2 and give it Flank`，能量不足那一格） |
| `A1465` | 8 张单数卡各判成「全体」⇒ 场上有 N 个残骸就翻 N 个 | 8 张单数卡各翻 **1 个**（`ctx.Rng` 可复现）；`SAU52` **仍翻全部** | 分支判定 = **离线逐张算出**（见下表）；引擎内读数（场上摆 3 个残骸：3 → 1）⚠️ **需主对话跑** |

**`A1465` 逐张（离线，用真卡的 `desc` 跑本笔那条正则）** —— 9 张卡的 9 条 reanimate 句：

| 卡 | 卡面片段 | 本笔判 | 改前 |
|---|---|---|---|
| `SAU52 Resurrection Orb` | `Reanimate all friendly Remnants` | **all = true** | 全部（**本来就对**） |
| `SAU26 Reanimate` · `SAU46 Reanimation Protocol` · `SAU67 Resurrection Vault` · `SAU25 Canoptek Reanimator` · `SAU49 Protection Protocol` | `(Re)animate a friendly Remnant` | all = **false** ⇒ 1 个 | **全部**（错） |
| `SAU16 Technomancer` · `SAU27 Chronomancer` | `reanimate a random friendly Remnant` | all = **false** ⇒ 1 个随机 | **全部**（错） |
| `SAU5 Orikan the Diviner` | `When you reanimate a Remnant, …`（**触发从句**，不是 op） | — | — |

> ⚠️ 「影响面 ≈ **16 张**」是「`desc`/`descZh` 里提到 reanimate 的卡」；**真会走到 `DoReanimate` 的只有 9 张**（其余 7 张是 `When Reanimated, …` 监听卡 —— `SAU9/10/29/72` 与 `Talent: Reanimate`、`create a Reanimation Protocol` 那几张）。这个差**不是本笔改出来的**，是账目里那句「≈16」的口径宽。

---

## 四、没查清 / 没做 / 顺手发现

1. ⚠️ **`A1465` 的「正解」本该在解析层**：`ParseTarget` 认不出 `remnant`（那条「不猜」守卫是对的），于是**按卡面原文**在结算层补判。**这是绕道**：更干净的做法是给 `EffectText` 的 kind 词表加 `Remnant`（+ `Count` 单复数），但那在 `EffectText.cs`（**别人的文件、此刻有写手**）⇒ 按简报**停手没动**，如实记在这里。**今天的行为是对的，位置不理想。**
2. ⚠️ **`A1413` ①② 今天在全池仍然 `0 张可达`**（`W_A1369付费三洞.md` §附二 的扫描口径：付费前缀落在 `for each` 上的卡 = 0；`repeats == 0` 那格要「那张卡真的打出来且计数为 0」）。⇒ 本笔是**堵形状 + 补可达性**，真读数只有夹具能给。
3. ⚠️ **`A1413` ① 的 `done` 口径没动**（收钱不算「结算了」）—— 若主对话希望「付了钱但 N=0」也算一条已结算，那是**另一笔口径**（会改 `ResolveOps` 的返回值语义），本笔**故意没扩大**。
4. 📌 **顺手发现（一条，归本行记录、不另立账）**：`DoReanimate` 里那句旧注释「卡面**全写** `Reanimate a friendly Remnant`（那 16 张里 15 张是这个写法）」**现核成立**（逐张扫过：16 张里 15 张单数）—— 顺带确认了 `SAU52` 是**唯一**例外，本笔的判据就架在这条上。
5. 📌 **顺手发现（没动、也没查透）**：`ResolveOps` 里「组」的分界完全靠 `Cost > 0 && CostShared` **连续**判定。若解析层将来把**付费前缀盖到不连续的 op 上**（中间夹一条 `Cost = 0` 的 op），组首付不起时**后半段仍会白给**。本笔**照既有分界标记做、⛔ 没有另立第二份「哪几条算一组」的判据**（那会与本仓铁律打架）⇒ 如实留在这里：这条边界是**结构性的**，要治得先在解析层保证连续性。
6. ⚠️ **本笔没跑任何自检 / 类型检查**（简报明令）⇒ 上面三笔的**编译可用性未验**；若主对话跑出红，第一嫌疑顺序 = ① `payOnly` 形参（`ResolveOne`/`ResolveOneCore` 两处签名 + 一处转调）；② `CostPrefixPayable` 的实参个数（4 个）；③ `ReanimatesAll` 的 `System.Text.RegularExpressions.*` 全限定名（本文件只有 `using System.Collections.Generic;`，全限定名是本文件既有写法）。
