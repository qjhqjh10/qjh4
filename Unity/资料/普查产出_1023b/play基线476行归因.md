# `play` 基线 476 行差异 —— 逐行归因（2026-10-23 · 只读归因代理）

> 对象：`_tmp_view/t67_sync/play_new.txt`（741,820 B · 本轮现跑）vs `Unity/工具/ruleprobe/基线/out_play_baseline.txt`（755,372 B · 2026-10-21 20:52）
> 方法：**影子探针**（见 §一）—— 把各提交的 `RuleEngine/` 原样编进临时目录跑 `play all pf`，**不动仓库、不跑 Unity、不动 git**。
> ⛔ 本报告只读归因，**没有重钉基线**、没有改任何源码/测试/台账。

---

## 〇 一句话结论

| 问 | 答 |
|---|---|
| 9 主档全归因了吗 | **是**（9/9）。6 张是**会话 62~66 的旧账**（基线过期 5 个会话），3 张是本轮 —— 其中 `BL64`/`GOF42` **有报告白纸黑字写过**，`EC33` **是真回归**。 |
| 467 电池同源吗 | **不同源**。425 行是**会话 62~66 的旧账**（其中 191 行又被本轮再改一次）；本轮的 234 行里 **227 行是 `B-31` 纯改错误码**（`7→21/22`、`10→23/24`），5 行是 `abilityContext` 的 Mob 卡（与 `引擎线_执行报告.md` §2.2 逐张一致），1 行 `SAU9`（`W64 B-15` 毁灭者改写）、1 行 `EC33`（同下）。 |
| 有真回归吗 | **有 1 张**：`EC33 Alluress` —— Rally 的 `-2/-2` **结算了两次**。根因已定位到 `CardDef.cs` 本轮 `D15`（`have.AddRange(ops)`），**只影响这 1 张**（关掉那一行，全池 476 行里只有 `EC33`/`E\|EC33` 两行回退）。 |
| 能重钉吗 | **不能直接重钉**。先修/裁 `EC33` 那一条；并且要对**会话 62~66 那 425 行旧账**做一次显式签字（本次只做了**归属**，没做**逐条判对错**）。详见 §七 的最后检查清单。 |

> 🔴 **最重要的一条发现（不属于"本轮"但决定重钉这件事）**：**这条基线是 2026-10-21（第六十一会话）钉的，此后跑了 5 个会话**（62/63/64/65/66），
> 每个都大改引擎（62 会话 +3009 行、63 +1025 行、64 +3093 行）。**影子探针实测：`3114b5e3`（= 第六十一会话的提交）跑出来的指纹与基线文件逐字节一致**
> ⇒ 基线 = 那一版引擎的状态，**476 行 = 62→67 五个会话的全部漂移之和**，⛔ 不是"本轮改坏了 476 行"。

---

## 一 方法与自证（影子探针）

```
/tmp/wfshadow   = 工作树工具 + git show HEAD(5e7cc775) 的 RuleEngine/Core + Data/CardDatabase + Resources  ← 影子-HEAD
/tmp/wfshadow2  = 工作树工具 + 工作树 RuleEngine                                        ← 影子-工作树
/tmp/wf_<sha>   = 工作树工具 + git archive <sha> 的 RuleEngine/工具                       ← 逐提交影子
```

**三条自证（都实测过）**：

1. **影子-工作树** 跑 `play all pf` ⇒ 输出与 `play_new.txt` **逐字节相同**（741,820 B）⇒ 影子探针与主对话跑的是同一台尺子。
2. **影子-`3114b5e3`** 跑 `play all pf` ⇒ 输出与**基线文件逐字节相同**（755,372 B）⇒ 基线确实是那一版。
3. 全池 `play all pf` **一次 19 秒**（重编 4 秒）⇒ 二分是廉价的，下面每条归属都是**跑出来的**，不是读代码猜的。

逐提交读数（字节数）：`3114b5e3` 755,372 · `a514fc53` 751,745 · `920d310e` 739,927 · `543a2836` 741,202 · `54abef0e` 741,202 · 工作树 741,820。

---

## 二 476 行的逐会话归属（合计 = 476，行可重叠）

| 段 | 提交 | 共变 | 主档 | 电池 | 主档是哪几张 |
|---|---|---|---|---|---|
| 61→62 | `a514fc53`（第六十二会话） | 27 | **1** | 26 | `DA28` |
| 62→63 | `920d310e`（第六十三会话） | **417** | 0 | **417** | — |
| 63→64 | `543a2836`（第六十四会话） | 8 | **5** | 3 | `30007162b6…` `3f38d2e2…` `AM39` `GSC40` `UM34` |
| 64→65 | `54abef0e`（第六十五会话） | 0 | 0 | 0 | — |
| 65→67 | 工作树（**本轮**） | **237** | **3** | **234** | `BL64` `EC33` `GOF42` |
| **合计（去重）** | 61→67 | **476** | **9** | **467** | |

> 主档 9 张**两段之间零交集**（6 张只在 62~66 段、3 张只在本轮段）⇒ 主档归属是干净的。
> 电池有 191 行**两段都动过**（62~66 改一次、本轮又改一次），所以逐段相加 689 > 去重 476。

---

## 三 9 个主档（逐张：旧/新关键列 → 归因）

列口径见 `PlayProbe.PfCols`（`id/type/warlord/playRc/played/faceOps/selfPlace/mech/other/expectedPay/energyDelta/handCostDelta/dmg/changes/crash/bucket/fixtureFit/injected/changesFull`）。

| # | id | 卡名 | 变化列（旧 → 新） | 归因 | 判 |
|---|---|---|---|---|---|
| 1 | `30007162b68992e41a422f2061705b65` | **Lord of the Storm**（Sautekh · 战术 · "Deal 1 damage… **If the target dies, create a Lord of the Storm in your hand**"） | `mech 3→1` · `handCostDelta -2→0` · `changes 5→3`；`changesFull` 少了 `✋手牌 P0 -「…」×1` 与 `💰手牌总价 3→1（-2）` | **62~66 旧账**（63→64 段） | 预期：`If the target dies` 那一支现在**兑现了**（手里留一份、弃牌堆也 +1 ⇒ 手牌数净 0、总价净 0）。⛔ 不是本轮 |
| 2 | `3f38d2e2c83df4b4c9052ef4a510cfbb` | **Rites of Possession**（BlackLegion · "Deal 2-4 damage. **If the target dies, deploy a Possessed Marine**"） | `other 2→3` · `changes 6→7`；新增 `✚ 落场/生成 P0[2] . → BL9 4/0/0/4 {flank1}` | **62~66 旧账**（63→64 段） | 同上（`If the target dies` 支兑现）。⚠️ **注意**：同一行里 `FIX_E1 2/0/0/6 → 2/0/0/2`（**没死**），却部署了 `BL9` —— 这一支的"目标死没死"判据值得另开一条核（本报告不判对错，只指出疑点） |
| 3 | `AM39` | **Born Soldier**（AstraMilitarum · "Deal 1 damage. **If target dies, draw a card**"） | `mech 3→2` · `other 2→4` · `handCostDelta -1→0` · `changes 6→7`；新增 `✋手牌 P0 +「filler28」;🂠牌库 P0 -「filler28」` | **62~66 旧账**（63→64 段） | 同上（抽牌支兑现） |
| 4 | `GSC40` | **Backstab**（Genestealers · "Deal 2 damage. **If the target dies, create a random Sabotage in the enemy hand**"） | `other 2→4` · `changes 6→8`；新增 `✋手牌 P1 +「7c666a04…」×1;💰手牌总价 P1 3→5（2）` | **62~66 旧账**（63→64 段） | 同上（造对手手牌支兑现，造出来的就是 `Cult Propaganda`，2 费） |
| 5 | `UM34` | **Point-Blank Shot**（Ultramarines · "Deal 2 damage. **If target dies, draw a card**"） | `mech 3→2` · `other 2→5` · `handCostDelta -2→-1` · `changes 6→8`；新增抽牌两行，`💰手牌总价 3→2（-1） ⇒ **除出牌本身外净 +1**` | **62~66 旧账**（63→64 段） | 同上 |
| 6 | `DA28` | **Company Master**（DarkAngels · "When you draw a card, it costs 1 less this turn"） | 只 `changesFull` 里 `💰手牌总价 P0 6→0（-6）` → `7→1（-6）`（**Δ 不变**） | **62~66 旧账**（61→62 段，本段唯一主档） | 预期：起手那份手牌的**基准总价**从 6 变 7 ⇒ 有一张垫牌的费用/张数变了（`DA28` 的抽牌减费那条链）。⚠️ 与 #1~#5 同族（都是"死/抽"支），但归属会话不同 |
| 7 | **`GOF42`** | **Da Irongob**（Goff · "Your Warlord gains Concussive until your next turn **and heals 5**"） | `other 4→1` · `changes 8→5`；`✎改动` 从 4 条（`FIX_A`/`WARLORD`/`FIX_B`/`FIX_C`）缩到 **1 条（只有 `WARLORD 2/0/0/28 → 30`）** | ✅ **本轮 `B-17`**（`targetsAffected=120 friendlyHero`） | **预期且正确**，有文档：`引擎线_尾巴_执行报告.md:16` 与 §3·4「可观测改动 = 1 张卡：`GOF42`，从『治疗 4 个己方单位』变成『治疗督军 1 个』」。卡面只说 Warlord ⇒ 新行为对 |
| 8 | **`BL64`** | **Murderous Desires**（BlackLegion · "Make a damaged friendly unit attack by itself"） | `playRc 0→18`（`ErrNoTargetAvailable`）· `played 1→0` · `mech 3→0` · `other 5→0` · `energyDelta -4→0` · `dmg 2→0` · `changes 9→0` | ✅ **本轮 `B-16`**（条件结构化） | **预期**，有文档：`引擎线_B16_执行报告.md:14`「结算指纹 A/B：1127 张里**只有 `BL64` 一张变**」。⚠️ 但"夹具里明明有受伤友军却报『没有合法目标』"这一点**报告没解释**，见 §六 疑点 B |
| 9 | **`EC33`** | **Alluress**（EmperorsChildren · "**Rally: Give -2 [Attack] and -2 [Ranged] to an enemy troop.** Friendly Daemonette have Flank."） | `mech 3→2` · `other 2→4` · `handCostDelta -2→-1` · `changes 6→8`；`✎改动 P1[3] FIX_E1 2/0/0/6 → 0/-2/0/6` 变成 `→ **-2/-4**/0/6`（**减益应用了两次**） | 🔴 **本轮 —— 真回归**（`CardDef.cs` `D15`） | ⛔ **不是预期**：卡面只印一次 `-2/-2`。根因与最小复现见 §六 |

---

## 四 467 个电池行

### 4·1 按"变化的列"分组（全量 476 行口径下 467 行电池）

| 变化的列组合 | 行数 |
|---|---|
| `ctrl+raw+steps` | 108 |
| `ctrl+net+netFull+raw+steps` | 69 |
| `ctrl+raw` | 55 |
| `steps`（只动 steps） | 51 |
| `ctrl+net+netDmg+netFull+raw+steps` | 45 |
| `net+netDmg+netFull+raw+steps` | 25 |
| `net+netFull+steps` | 19 |
| `net+netFull+raw+steps` | 16 |
| `ctrl+netFull+raw+steps` | 16 |
| 其余 23 种组合 | 各 1~13 |

**单列出现次数**：`raw` 385 · `steps` 375 · `ctrl` 331 · `netFull` 243 · `net` 205 · `netDmg` 100。
⚠️ **`bucket` / `cand` / `playRc` / `played` / `ctrlPlayed` / `note` 六列：全池一个字都没变**（467 行里 0 行）。

**`raw`/`ctrl` 的方向**（现算）：`raw` 变了的 385 行里 **346 减 / 39 增**；`ctrl` 变了的 331 行里 **303 减 / 28 增**。
按方向组合看：**171 行 = raw 减 + ctrl 减 + `net` 不变**、**106 行 = raw 减 + ctrl 减 + `net` 也减**、29 行 = raw 减 + ctrl 不变 + net 减。
**多数是 raw 与 ctrl 同向同量** ⇒ 这一大块是**夹具层面**的位移（两局都少同样多条变化），不是"某张卡效果变了"；
但 **39 行 raw 是增的**，那批要单独看（见 §六 D 的 `GOF19`）。

### 4·2 本轮的 234 行（影子-HEAD → 工作树）—— 拆成四类

| 类 | 行数 | 证据 |
|---|---|---|
| **`B-31` 纯改错误码** | **227** | 把 steps 里的 `=21→=7 / =22→=7 / =23→=10 / =24→=10 / =25..27→=10` 归一化后，`steps` 与其余 12 列**逐字节相同**。方向：`atkM/atkR 7→21`（191 行）、`friendDies 10→23`（67 行）、`friendDies 10→24`（`TAU10`，隐身） |
| **`abilityContext` 的 5 张 Mob** | 5 | `GOF9` `GOF12` `GOF19` `GOF84` `GOF87` —— 与 `引擎线_执行报告.md` §2.2 的名单**逐张一致**（"主档 0 张变、电池 5 张变，全部是 Mob 卡"） |
| **`W64 B-15` 毁灭者改写（真行为变化）** | 1 | `SAU9 Ophydian Destroyer`：`atkR2 0 → 26`（`ErrCantAttackDestroyer`），`raw/ctrl 52→46` |
| 🔴 **`EC33` 双结算** | 1 | 见 §六 |

> 补充：`RuleCodes` 的 7 个新码（21..27）在 `Terms` 里 7/7 有词条、有中文名 ⇒ 不会吐"未知错误码"。

### 4·3 62~66 的 425 行 —— **性质已量，来源未逐条归**

- **206 行**：`netFull` 只差**数字**（改动的结构一模一样，只有血量数字不同）。
- **219 行**：结构也不同（多/少一条 `✎ 改动`、`kill`/`friendDies`/`collect` 的目标格变了）。
- `raw`/`ctrl` 方向（现算 425 行）：**294 行两者同减**、25 行同增、53 行只 raw 减、33 行两者都不变；
  另有 8 行 raw 增 + ctrl 不变、4 行 raw 增 + ctrl 减 —— 那 12 行是"净差被拉大"的，要单独看。
  典型同减例：`AM13 45→44`、`AM10 49→45`、`BL11 45→42`。
- 已能看到的典型形态：**夹具的 `selfChip`/`chip2` 削血在 62~63 之间"没生效"了** ——
  例 `AM10`：`selfChip=2`（rc 相同）之后，61 版血 = 1、63 版血 = 3（卡池里 `AM10` 的 `health` 五个提交都是 5 ⇒ **不是卡表漂移**）。
- 🔴 **这 425 行本报告只做了"归属到会话"，没有做"逐条判对错"** —— 这是一件独立的活（见 §七 检查清单第 3 条）。

---

## 五 抽样全文（10 张，逐张读）

> 标签 `(本轮)` = 该列值在 影子-HEAD → 工作树 之间又变过；`(62~66 遗留)` = 只在基线 → HEAD 之间变。

**① `E|AM13`（bucket B）—— 纯改号**
```
[steps] (本轮)
  10-21: …enemyTactic=0@3,friendDies=10@1/atk@3,end1=0,collect=17@1/live+way,…atkM=0,atkR2=0,kill=0@5M/gone@tgt5
  NOW  : …enemyTactic=0@3,friendDies=23@1/atk@3,end1=0,collect=17@1/live+way,…atkM=0,atkR2=0,kill=0@5M/gone@tgt5
[raw] 45→44  [ctrl] 45→44      ← 62~66 遗留
```
`friendDies 10→23` = `ErrTarget` → `ErrAttackBlock`（`B-31`）。逐字只差这两个数字。

**② `E|TAU10`（bucket C · cand=1 · Stealth Drone）—— 纯改号（我第一版归一化漏了 24）**
```
[steps] (本轮)  friendDies=10@1/atk@3 → friendDies=24@1/atk@3        ← 10 → 24 = ErrAttackBlockStealth
[raw] 42→41                                                          ← 62~66 遗留
```

**③ `E|SAU9`（Ophydian Destroyer · 带 `Destroyer` 关键词）—— 真行为变化（本轮 `W64 B-15`）**
```
[steps] (本轮)  …atkM=0,atkR2=0,kill=0@5M/gone@tgt5   →   …atkM=0,atkR2=26,kill=0@2M/gone@tgt5
[raw] 52→46  [ctrl] 52→46
```
第二发攻击从 `0`（放行）变成 `26`（`ErrCantAttackDestroyer`）。判据 = `RuleCore.IsValidTarget` 的毁灭者那一支本轮按
`BattleManager__IsValidAttackTarget.c:207-250` **重写**（`RuleCore.cs` `isCheckingDestroyerTargets`），语义 = "打不死当前目标、且另有一个**这一下能打死**的合法目标 ⇒ 不合法"。
⚠️ 这是**按反编译判据改对了**，但它同时是**结算变化**，`B-31` 的报告口径（"只改错误码，不改结算"）**没覆盖到它** —— 见 §六 疑点 C。

**④ `E|GOF12`（Mob）—— 本轮 `abilityContext`**
```
[raw] 46→44   [ctrl] 47→45（62~66 遗留）
[net] 0→3  [netDmg] 0→2  [netFull] "" → "✎ 改动 P0[5] FIX_B 3/0/0/3 → FIX_B 5/0/0/4;✎ 改动 P0[5] FIX_B 5/0/0/4 → FIX_B 5/0/0/1;☠ 伤害累计 P0 13 → 23"
```
= Mob 能力在"入队时在场、出队前离场"时**不再被取消** ⇒ 第一次读出了净差。与 `引擎线_执行报告.md` §2.2 一致。

**⑤ `E|GOF19` —— 同上，但方向是"造了两张"**
```
[netFull] "✋手牌 P0 +「GOF4」×1;💰3→5（2）"  →  同一段再重复一遍 ⇒ "…再加一张 GOF4、💰5→7（2）"
```
⚠️ 同一张卡**造出两张 `GOF4`**（第二次），这不是"不再被取消"能解释的 —— 见 §六 疑点 D。

**⑥ `E|GOF84` —— 同上（造牌支）**
```
[net] 0→2  [netFull] "" → "✋ 手牌 P0 +「GOF88」×1;💰 手牌总价 P0 3 → 8（5）"
```

**⑦ `E|AM10`（bucket C）—— 62~66 遗留（夹具削血没生效）**
```
[steps] (本轮) friendDies=10@1 → 23@1        ← 改号
[netFull] (62~66) "…AM10 3/3/0/1 {duty1,rally1} → AM10 3/5/0/1…"  →  "…AM10 3/3/0/3 → AM10 3/5/0/3…"
[raw] 49→45  [ctrl] 47→45  [net] 14→13
```
`selfChip=2` 之后血量：61 版 1、现版 3 ⇒ 那一步**没扣血**（卡池 `health` 没变）。

**⑧ `E|BL11` —— 62~66 遗留，净差不变、只是两边都少了 3 条**
```
[steps] (本轮) friendDies=10@1 → 23@1
[raw] 45→42  [ctrl] 45→42      ← 两局同量同向 ⇒ 夹具位移，不是卡的问题
```

**⑨ `E|SW22` —— 62~66 遗留（`huntmark` 那条链）**
```
[netFull] (62~66)
  10-21: "SW22 3/5/0/3 → SW22 3/5/0/1 {…huntmark1…}；(略)…☠ 离场…"
  NOW  : "SW22 3/5/0/3 → SW22 3/5/0/3 {…huntmark1…}；…"（血量不再掉）
[steps] (本轮) `atkR=0` 后面**多出一格** `atkM2=0` —— 旧版那一格**根本不存在**（第一发之后 `Board[slot]` 已经空了 ⇒ 第二发那一段整段跳过），
新版单位**存活** ⇒ 第二发照跑。同族现象见 ①（`AM18/AM19` 那批 `atkM2 <ABSENT>→0`，共 96 行）
```

**⑩ `E|UM101` —— 62~66 遗留（净差整段位移）**
```
[raw] 52→49  [ctrl] 53→50  [net] 29→26  [netDmg] -20→-23
[netFull] 结构相同，只有血量数字不同（`FIX_E1 2/0/0/6 → 2/0/0/3 → 2/0/0/1` 那一段）
```

**抽样结论**：10 张 = **2 张纯改号**（①②）· **3 张 `abilityContext` 的 Mob**（④⑤⑥）· **1 张毁灭者改写**（③）·
**2 张「改号 + 旧账」混排**（⑦⑧）· **2 张纯旧账**（⑨⑩）。**没有一张是「只变数字、来源不明」**。

---

## 六 真回归候选与疑点

### 🔴 A【真回归 · 已定位根因】`EC33 Alluress` —— Rally 的 `-2/-2` 结算了两次

| 项 | 内容 |
|---|---|
| 现象 | `play EC33` 的日志里 `RALLY：「Give -2 Attack and -2 Ranged to an enemy troop」给了 1 个目标` **出现两次**；`FIX_E1` 从 `2/0/0/6` 变成 `-2/-4/0/6`（应为 `0/-2/0/6`） |
| 卡面 | `Rally: Give -2 [Attack] and -2 [Ranged] to an enemy troop. Friendly Daemonette have Flank.` ⇒ **只许一次** |
| 根因 | **本轮 `D15`**：`Core/CardDef.cs` 的 `AddTriggerOp` 从「**先到先得**（`if (_triggerOps.ContainsKey(key)) return;`）」改成「**同文去重 + 异文追加**（`have.AddRange(ops);`，`CardDef.cs:3287`） |
| 为什么偏偏是它 | 该卡 `keywords = ['Rally: Give -2 [Attack] and -2 [Ranged] to an enemy troop']`（**只有第一句**），而 `desc` 的同一段是**两句**（多了 `Friendly Daemonette have Flank.`）⇒ 两条正文**字符串不相等** ⇒ 去重没挡住 ⇒ ops 追加 ⇒ 播两次 |
| 三个方向的实测证据 | ① **清空该卡的 `keywords`**（`WFPROBE_POOL` 指向临时副本）⇒ 只播一次；② **把 `have.AddRange(ops)` 改成 `have.AddRange(new List<EffectOp>())`** ⇒ 只播一次；③ 全池 `play all pf` 在②下与当前指纹**只差 `EC33` / `E\|EC33` 两行**（其余 474 行一个字不动） |
| 不是哪一层 | 解析层无辜：`ruleprobe card Alluress` 的 `parse` 只有 **1 条 op** ⇒ 是**注册层合并**重复，不是解析重复 |
| 影响面 | 全池 **1 张**。但**机制是通用的**：全池有 **26 张**"`keywords` 里带触发前缀、且同段正文也出现在 `desc` 里"的卡，只是其余 25 张两份正文**逐字相同**、被同文去重挡住了 |
| 建议修法（供调度台裁） | 二选一：① **数据侧**把 `EC33` 的 `keywords` 那条多余触发正文删掉（它本来就是 `desc` 第一句的副本）；② **代码侧**把"同一触发词的多条正文"去重键从"整段相等"改成"**该触发词的头 + 归一化正文**"，或按"这条 op 是否已在同一 trigger 的 op 表里出现过"去重。⛔ **不要回退 `D15`** —— 诊断文档 `诊断_RuleEngine8红.md:87` 已按 `RawCardScript__TriggerAbilities.c:112-215` 判过「同一 trigger 挂两条就是两条都触发」，回退会把一条正确的修复推翻 |

### ⚠️ B【疑点 · 需另开一条】`BL64` 报"没有合法目标"

夹具对**我方 3 张部队各扣 2 血**（`FixtureChip=2`），BL64 的正文是 `Make a **damaged** friendly unit attack by itself` ⇒ 应该**有**目标。
新行为却是 `playRc=18`（`ErrNoTargetAvailable`）。`引擎线_B16_执行报告.md:14` 只写了"只有 `BL64` 一张变"，**没写清"变成什么、为什么"**。
⇒ 这不一定是回归（也可能原来的"打了自己人 `FIX_A` 并让它死"才是错的），但**报告没交代**，重钉前应有一句话交代清楚。

### ⚠️ C【口径缺口】`SAU9` 是「改结算」，不是「只改错误码」

`atkR2` 从 `0`（放行）变成 `26`（拒绝）。`B-31` 的报告口径写的是"只改错误码，不改结算" —— 但毁灭者那一支是 `W64 B-15` **重写了形状**，
它**确实改了结算**。判据（反编译 `:207-250`）站得住，**但归类写错了**，重钉记录里别把它记成"纯改号"。

### ⚠️ D【疑点 · 需另开一条】`GOF19` 造出**两张** `GOF4`

`62~66` 之外本轮又改了它（`net 2→4`），方向是"同一条造牌支跑了两遍"。`引擎线_执行报告.md` §2.2 只说"变化方向 = Mob 能力不再被取消"，
解释不了"第二次又造一张"。⚠️ 与 §六 A 同族（都是"同一段正文/同一支被跑两遍"），**建议 A 修完先复跑一次，看 D 会不会一起消失**。

### ⚠️ E【旧账 · 决定重钉的那一条】62~66 的 425 电池行

本次只做了归属（§二），**没有逐条判对错**。其中 206 行"只差数字"、219 行"结构也不同"，最集中的形态是**夹具 `selfChip`/`chip2` 削血在 63 之后不再生效**（`AM10` 是最小复现）。
⇒ 重钉 = 把这一批一起烘进基线。**要么先归因，要么由调度台显式签字"接受"**，⛔ 不能因为"不是本轮造成的"就默认放行。

---

## 七 能否重钉 + 重钉前的最后检查清单

### 结论

**⛔ 现在不能重钉。** 必须先有 §六 A 的处理决定（修 / 或明确接受），并对 §六 E 的 425 行旧账做一次显式签字。

### 重钉前最后检查清单（按顺序做）

1. **`EC33` 有结论** —— 修掉（数据侧删多余 `keywords` 正文 或 代码侧改去重键），或调度台明确写下"接受 `-2/-2` 两遍"。
   - 验收判据：`dotnet <probe> play EC33 | grep -c "给了 1 个目标"` **= 1**。
   - 修法的候选与反例都在 §六 A，⛔ 别回退 `D15`。
2. **`GOF19`（§六 D）复跑一次** —— A 修完之后先看它会不会跟着消失；不消失就单独立账。
3. **425 行旧账签字** —— 至少在台账里写一句："会话 62~66 造成的 425 电池行 + 6 主档，**已接受/待归因**"，
   并记下本次的分段数（27 / 417 / 8 / 0 / 237）。⛔ 不要用"基线过期"四个字糊过去。
4. **`BL64` 一句话交代**（§六 B）：新读数是 `playRc=18 ErrNoTargetAvailable`，为什么被判成"没有合法目标"。
5. **重跑两次比字节** —— `play all pf` 连跑两遍，两份**逐字节相同**（这是全仓对 `play` 指纹的老规矩）。
6. **重钉之后 `play check` 必须 rc=0**：
   - ①-a / ①-b 差异 **0 行**；
   - ② 哨兵（基线文件 md5 == `PlayProbe.PinnedPlayMd5`）要**同步改成新 md5**。
     ⚠️ 现在钉的是 `5d81171ae96e5a07df9cdc1a2fac51cf`（= 现行基线文件的 md5，本次现算**确认一致**）；
     新指纹的 md5 本次现算 = `ccad519f7d2fc3dfd4f17c82994a4e1d`（⛔ 修完 `EC33` 之后**会再变**，所以**别抄这个值**，重跑后现读）。
7. **行数核对** —— 新文件仍应是 **1713 行**（主档 1127 ＋ 电池 586），本次 `play_new.txt` 实测 **1713**（新增 0 / 消失 0）✅。
8. **别顺手改 `PinnedPoolMd5`** —— 《尾巴》报告已单独重钉过它（`3f4002d0…`，卡池 v17）；`play` 基线是**另一颗钉子**，两件事别混。

---

## 附录 · 本次用到的可复现命令（都在 `/tmp` 影子目录里，不碰仓库）

```bash
# 影子-HEAD
mkdir -p /tmp/wfshadow/Unity/工具 /tmp/wfshadow/Unity/MyGame/Assets/RuleEngine
cp d:/4/Unity/工具/ruleprobe/*.cs d:/4/Unity/工具/ruleprobe/ruleprobe.csproj /tmp/wfshadow/Unity/工具/ruleprobe/
cd d:/4 && git archive HEAD -- Unity/MyGame/Assets/RuleEngine | tar -x -C /tmp/wfshadow
git show HEAD:Unity/工具/ruleprobe/Program.cs > /tmp/wfshadow/Unity/工具/ruleprobe/Program.cs
cd /tmp/wfshadow/Unity/工具/ruleprobe && dotnet build -c Release && dotnet bin/Release/net8.0/wfprobe.dll play all pf /tmp/pf_head.txt

# 逐提交（同理，换成 git archive <sha>）
# 单卡：dotnet bin/Release/net8.0/wfprobe.dll play EC33
# 解析层：dotnet bin/Release/net8.0/wfprobe.dll card Alluress
```
