# `B141-COIN-FLIP` 缺的那一跳 —— **可直接粘贴的补丁**（只读产出）

> 执行写手 `B143-W1` · 2026-11-05 · 上游施工单 = `判据/B143_随机流施工单.md` §2·3（方案 A）
> ⛔ 本文件**不改任何源码**（`RuleCore.cs` 现在归 `R3` 的 `GSC37 setCost`）。落地由调度台转派。
> ✅ 全部行号 = **2026-11-05 现读**；行号会漂 ⇒ **按符号找**（每节都给了 grep 命令）。

---

## 〇 · 一句话

`BattleContextRandom.cs` 的 API 已经齐了（`FirstSeatRandomInt` / `DrawFirstSeatRandomInt`），
**缺的只有「`NewBattle` 去调它」这一跳**（现读：`DrawFirstSeatRandomInt` 全仓 **0 个调用点**）。
补丁 = **5 处**，全部**同物理行改写 / 同行追加**（零行数变化）。

---

## 1 · 现读锚点（按符号找，⛔ 别记行号）

| 符号 | 现读位置 | 现读原文 |
|---|---|---|
| `NewBattle` 签名末行 | `RuleCore.cs:156` | `int botSeat = -1, MatchType matchType = MatchType.Undefined)` |
| `ctx.FirstSeat` 赋值 | `RuleCore.cs:201` | `ctx.FirstSeat = firstSeat == 1 ? 1 : 0;` |
| 建牌库时的洗牌（**必须在掷硬币之后**） | `RuleCore.cs:635`（`if (shuffle) Shuffle(p.Deck, ctx);`，在 `BuildPlayer` 里） | 现读在 `:201` **之后** ✅ |
| 真打路径 `firstSeat` 计算 | `BattleDriver.cs:5503` | `int firstSeat = firstSeatOverride ?? _forceFirstSeatTest ?? byInitiative ?? FirstSeatForSeed(seed);` |
| 教程覆盖（先手照关卡） | `BattleDriver.cs:5510` | `firstSeat = TutorialRules.PlayerStarts(tutorial.Stage) ? 0 : 1;` |
| **唯一生产 `NewBattle` 调用** | `BattleDriver.cs:5524-5526`（**两个物理行**） | `Ctx = RuleCore.NewBattle(myCards, foeCards, seed, shuffle: shuffle, cardPool: pool,` … `matchType: RuleEngine.MatchTypes.For(ResolvePlayMode(playMode)));` |
| `FirstSeatForSeed` | `BattleDriver.cs:13032-13035` | `return new System.Random(seed ^ 0x5F3759DF).Next(2);` |
| 随机源半 | `BattleContextRandom.cs:83` / `:87` | `public int FirstSeatRandomInt;` / `public int DrawFirstSeatRandomInt()` |

> 🔴 **字段名现查结论**：`BattleContextRandom.cs` 是 **`partial class BattleContext`** ⇒ 调用形状是
> **`ctx.DrawFirstSeatRandomInt()` / `ctx.FirstSeatRandomInt`**（直接挂在 `ctx` 上）。
> ⛔ **没有 `ctx.Random` 这个成员** —— `ctx.Rng` / `ctx.ShowRng` / `ctx.AiRng` 是三条 `System.Random`，
> 其中 `Rng` 只用于**烧表**，`ShowRng`/`AiRng` 是另外两条流（判据 → `BattleContextRandom.cs` 文件头）。

grep 复核（落地前先跑一遍，行号对不上就按符号找）：
```bash
grep -n "MatchType matchType = MatchType.Undefined)"     RuleEngine/Core/RuleCore.cs
grep -n "ctx.FirstSeat = firstSeat == 1 ? 1 : 0;"        RuleEngine/Core/RuleCore.cs
grep -n "int firstSeat = firstSeatOverride"              CardPresentation/Battle/BattleDriver.cs
grep -n "Ctx = RuleCore.NewBattle("                      CardPresentation/Battle/BattleDriver.cs
grep -n "return new System.Random(seed ^ 0x5F3759DF)"    CardPresentation/Battle/BattleDriver.cs
grep -rn "DrawFirstSeatRandomInt"                        Assets/    # 今天应只有 BattleContextRandom.cs + B143_R1_Assert.cs
```

---

## 2 · 补丁（五处，逐处给「改前原文」→「改后完整代码」）

### 补丁 A · `RuleCore.cs:156` —— 加形参（**同行追加**，零行数变化）

改前：
```csharp
                                              int botSeat = -1, MatchType matchType = MatchType.Undefined)
```
改后：
```csharp
                                              int botSeat = -1, MatchType matchType = MatchType.Undefined, bool drawFirstSeat = false)
```
> 缺省 `false` ⇒ **40+ 个探针 / 自检调用点一个字都不用改**，`play check` 指纹不动（施工单 §2·5）。

### 补丁 B · `RuleCore.cs:201` —— 掷硬币 + 用掷出来的座位（**整行改写**，零行数变化）

改前：
```csharp
            ctx.FirstSeat = firstSeat == 1 ? 1 : 0;
```
改后：
```csharp
            if (drawFirstSeat) ctx.DrawFirstSeatRandomInt(); ctx.FirstSeat = (firstSeat >= 0 ? firstSeat : (ctx.FirstSeatRandomInt == 0 ? 0 : 1)) == 1 ? 1 : 0;   // 🔴 2026-11-05（B143 · B141-COIN-FLIP）：原版 `SearchOpponentManager__StartBattle.c:20-21` 开局**无条件**掷 `GetRandomInt(0,2)` 存 `MatchData+0xe8`，是整局**第 0 格**；平局支 `SupportMethods.cs:3216-3228` 读它（`==0` ⇒ 座位 0 先手）。`firstSeat >= 0` = 调用方**显式指定**（联机/录像/教程/自检钉子）⇒ 值照旧覆盖、但**格子照吃**（原版同形）；`-1` = 没指定 ⇒ 用掷出来的。⛔ 缺省 `drawFirstSeat = false` ⇒ 探针路径流不动
```
**要点（缺一个就是错）**
- 位置必须在 `BuildPlayer`（`:635` 洗牌）**之前** —— 它现在就在，✅ 别挪。
- 教程那一支（`:196` 的 `firstSeat = TutorialRules.PlayerStarts(st) ? 0 : 1;`）在**本行之前**跑 ⇒ 传进来的 `firstSeat` 已是 0/1 ⇒ 走「显式指定」那一档：**格子照吃、值照关卡** = 原版「先无条件掷、再由 `matchType` 分支覆盖取值」✅。
- `ctx.FirstSeatRandomInt` 在不掷时恒 `0` ⇒ 若有人传 `firstSeat: -1` 又不掷，会落到座位 0（与今天默认一致，无害）。

### 补丁 C · `BattleDriver.cs:5503` —— 真打路径改成「没指定就交给掷骰」（**整行改写**，零行数变化）

改前：
```csharp
            int firstSeat = firstSeatOverride ?? _forceFirstSeatTest ?? byInitiative ?? FirstSeatForSeed(seed);
```
改后：
```csharp
            int? seatFixed = firstSeatOverride ?? _forceFirstSeatTest ?? byInitiative; int firstSeat = seatFixed ?? -1;   // 🔴 2026-11-05（B143 · B141-COIN-FLIP）：**没有显式指定 ⇒ 传 -1，由 `NewBattle` 用掷出来的那个**（原版就是 `StartBattle` 无条件掷）；`FirstSeatForSeed` 退回只服务联机/自检（见补丁 E）
```
> `firstSeatOverride`（放录像）/ `_forceFirstSeatTest`（自检钉子）/ `byInitiative`（军种先攻）**仍然赢** —— 它们只决定**值**，不影响**吃格**（补丁 B 的 `>= 0` 分支）。

### 补丁 D · `BattleDriver.cs:5524-5526` —— **唯一生产调用点**补实参（**同行追加**，零行数变化）

改前（两个物理行，第 2 行结尾是 `);`）：
```csharp
            Ctx = RuleCore.NewBattle(myCards, foeCards, seed, shuffle: shuffle, cardPool: pool,
                                     openMulligan: mulliganEnabled, vars: _vars, firstSeat: firstSeat,
                                     tutorial: tutorial, botSeat: botSeatThisGame, matchType: RuleEngine.MatchTypes.For(ResolvePlayMode(playMode)));
```
改后（只动**最后一行**，把 `, drawFirstSeat: true` 追加在 `)` 之前）：
```csharp
                                     tutorial: tutorial, botSeat: botSeatThisGame, matchType: RuleEngine.MatchTypes.For(ResolvePlayMode(playMode)), drawFirstSeat: true);
```
> ⚠️ 这一行本身**已经带着一段很长的 `//` 注释**（`B116_W37` 那条 `matchType` 说明）⇒ 追加要落在**代码里**、
> 不是注释后面（本仓踩过 6 次的老坑）。若那行结构变了，按符号 `matchType: RuleEngine.MatchTypes.For(ResolvePlayMode(playMode))` 定位。
> ✅ 联机 / 放录像 / 重连**也走 `Begin`** ⇒ 同样掷、同 seed ⇒ 两端/两遍游标一致（施工单 §2·3 第 5 条）。

### 补丁 E · `BattleDriver.cs:13032-13035` —— `FirstSeatForSeed` 改成**与表第 0 格同源**（整行改写）

改前：
```csharp
            return new System.Random(seed ^ 0x5F3759DF).Next(2);
```
改后：
```csharp
            return new System.Random(seed).Next(0, 1000) % 2 == 0 ? 0 : 1;   // 🔴 2026-11-05（B143 · B141-COIN-FLIP）：**与预烧表第 0 格同源** —— `Rng = new Random(seed)`（`BattleContext.cs:676`）烧表第一格 = `Rng.Next(0,1000)`，掷硬币 = `RandomNumbers[0] % 2`（`GetRandomInt(0,2)` = `0 + v % 2`）⇒ 联机/自检算出来的座位必须与真打一致，否则两边先手不同
```
> ⚠️ **改动波及**：`NetMatchmaking.cs:856`（联机先手）与一批「用旧公式找种子」的自检（`Editor/BattleScene.cs` 的
> `SeedForFirstSeat` 会自己重算 ⇒ **能自适应**；但**写死期望值**的断言要现读更新 —— 施工单 §2·3 第 6 条已点名）。

---

## 3 · 断言（补两条，写进现成的 `CardPresentation/Editor/B143_R1_Assert.cs` F 组旁边）

F 组现在只断 **API 自身**（`new BattleContext(seed)` 直调）⇒ **全绿也不代表接线了**（今天就是 0 调用点）。补：

### 3·1 ★★ 结构断言（**唯一能抓「又没人调」的那条**）

```csharp
// 读源码文本（本仓已有先例：B142_E36_Assert 的 C 组就是切方法体文本比）
var src = System.IO.File.ReadAllText("Assets/CardPresentation/Battle/BattleDriver.cs");
int at = src.IndexOf("RuleCore.NewBattle(", System.StringComparison.Ordinal);
Check(at >= 0, "G0 前提：`BattleDriver.cs` 里找得到 `RuleCore.NewBattle(`");
string call = src.Substring(at, System.Math.Min(900, src.Length - at));
Check(call.Contains("drawFirstSeat: true"),
      "G1 ★★ 真打路径（`BattleDriver.Begin` 的唯一 `NewBattle` 调用）**必须**传 `drawFirstSeat: true`"
    + " —— 今天这个参数存在但 **0 个调用点**，删掉/漏掉它 = 开局硬币又静默不掷（本仓反复踩的「断言全绿 ≠ 接线」）");
```
> 判别力：把补丁 D 的 `, drawFirstSeat: true` 删掉 ⇒ **G1 立刻红**；只加 API 不接线（= 今天的样子）⇒ 也红。

### 3·2 ★ 行为断言（四档，全部走 `NewBattle`）

```csharp
Check(RuleCore.NewBattle(D(), D(), seed: 0, shuffle: false).RandomNumbersIndex == 0,
      "G2 缺省（`drawFirstSeat` 不传）⇒ 游标**不动**（探针 40+ 个调用点与 `play check` 指纹的前提）");

var cd = RuleCore.NewBattle(D(), D(), seed: 0, shuffle: false, drawFirstSeat: true);
Check(cd.RandomNumbersIndex == 1, "G3 ★ 掷了 ⇒ **恰好吃 1 格**（0 → 1）");
Check(cd.FirstSeat == (cd.RandomNumbers[0] % 2 == 0 ? 0 : 1),
      "G4 ★ 掷出来的值决定座位（`RandomNumbers[0] % 2`；奇偶方向不能反）");

var cf = RuleCore.NewBattle(D(), D(), seed: 0, shuffle: false, drawFirstSeat: true, firstSeat: 1);
Check(cf.RandomNumbersIndex == 1 && cf.FirstSeat == 1,
      "G5 ★★ 显式指定 `firstSeat: 1` ⇒ **值照旧、格子照吃**（原版 `StartBattle` 无条件掷 + 分支覆盖取值，两者不是二选一）");

var cs = RuleCore.NewBattle(D(), D(), seed: 0, shuffle: false, drawFirstSeat: true, firstSeat: -1);
Check(cs.FirstSeat == (cs.RandomNumbers[0] % 2 == 0 ? 0 : 1), "G6 `-1` 哨兵 ⇒ 用掷出来的那个");
```
> ⚠️ `D()` = 一副最小合法牌（现成夹具：`B143_R1_Assert` 里已有 `Deck()`/`Hero()` 一族，直接用）。
> ⚠️ 断言里**别钉具体种子烧出什么值**（跨运行时可能变）—— 只断「`FirstSeat` 与 `RandomNumbers[0] % 2` 一致」这种**关系式**。

---

## 4 · 落地顺序与三条订正

1. **必须先补 A、B（`RuleCore.cs`）× 再补 C、D（`BattleDriver.cs`）** —— 反过来会有一段 `firstSeat = -1` 但
   `NewBattle` 不认 `-1` 的中间态：`ctx.FirstSeat = (-1 == 1 ? 1 : 0) = 0` ⇒ **所有局都变成座位 0 先手**（静默）。
2. **E 可以最后做**（它只影响联机/自检算座位，不影响对局内的流）。
3. 🔴 **`BattleDriver.cs:5505` 那句注释会过期**：现读写「⇒ ⛔ **这里不掷硬币**（掷了会和关卡打架）」——
   方案 A 之后**掷是会掷的**（在 `NewBattle` 里），只是**值被关卡覆盖**。那一句必须同批改成
   「掷照吃、值照关卡（原版同形）」，否则下一个人会按注释把补丁 B 删掉。
4. ⚠️ **旧录像不再兼容**（施工单 §2·5）：旧档录制时没掷硬币 ⇒ 回放按新代码会掷 ⇒ 流 +1 ⇒ 从第一次抽卡起分叉。
   与 `B143-REPLAY-RANDOM-INDEX` 是**两个方向**的事：游标列能救「AI 吃格」，**救不了**「开局少掷一格」——
   后者只能靠 `version +1` 拒旧档（施工单 §3·4 已写）。**两条要一起决**。
5. ⚠️ **`firstSeat` 的缺省仍是 `0`（不是 `-1`）**：这是**有意保留**（改缺省会波及 40+ 探针调用点，施工单 §2·6）。
   所以「没指定 ⇒ 掷出来的」这条**只在 `BattleDriver` 那条真打路上成立**（补丁 C 传 `-1`），别指望 `NewBattle` 自己会。

---

## 5 · 顺手发现（⛔ 都没改）

1. `BattleDriver.cs:5488` 的注释把先手四档写成「①… ②… **③ 掷硬币（`FirstSeatForSeed`，真 Play 那条路）**」——
   方案 A 之后「掷硬币」变成**开局共享表那一格**，不再是 `FirstSeatForSeed`；这句同批要改，否则与补丁 E 打架。
2. `FirstSeatForSeed` 的 doc（`:13028`）写着「**只此一处**：`Begin` 用它；自检找种子时也用它」——
   补丁 C 之后 `Begin` **不再用它** ⇒ doc 的「只此一处」要改成「联机/自检那条路用它」。
3. 现读 `DrawFirstSeatRandomInt` 只有 doc 里提到 `RuleCore.NewBattle` 会调它（`BattleContextRandom.cs:86`）——
   这条 doc 是**写给未来**的（今天不成立）⇒ 补丁落地后才变成事实。若调度台决定**不做**这一跳，这条 doc 得改成「暂未接线」。
