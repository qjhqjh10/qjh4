# G3 — `AiTargets.Trim` 的 `targetSelectMethod` 逐值口径（引擎线，2026-10-24）

> 判据（⛔ 不重新论证）→ `资料/普查产出_1024/C5_剩余判据普查.md` §2，
> 第一权威 = `d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:1164-1366`。

改动文件（全部在白名单内）：
- `MyGame/Assets/RuleEngine/Data/AiTargets.cs` —— `Trim` 整块重写 + 3 个新私有件 + `AiTargetRef` 加 4 个派生属性
- `MyGame/Assets/RuleEngine/Editor/RuleEngineTest_G3.cs` （新建，含 `.meta`）
- `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` —— **只加** `Section` / `Step` 各一行（`:996-1001` 附近）

---

## 一、逐值规则 vs 我们的改动

原版 `switch(*(undefined4*)(param_2 + 0x44))`（`+0x44` = `targetSelectMethod`），
守卫两句（`:1164-1170`）：`applyMaxTargets == 0 || maxTargets < 1` ⇒ 不截；
`list.Count <= maxTargets` ⇒ 不截（**连排序都不做**）。我们把这两句合并成
`if (c.maxTargets <= 0 || list.Count <= c.maxTargets) return;`（等价）。

| 值 | 原版 | 改前（我们） | 改后（我们） |
|---|---|---|---|
| **0** | 落 `default:` = **无放回随机取 N**（`:1250-1273`） | ❌ 保前 N | ✅ `TrimRandom`：`ctx.AiRng.Next(list.Count)` 取一个删一个，**结果按抽中序**（流的选择见 §六） |
| **1** | `OrderBy(isRemnant).ThenByDescending(max(近战,远程))`（`:1172-1207`） | ⚠️ 只按**近战** `Attack` 降序 | ✅ 主键 `IsRemnant` 升序（非残骸在前）、次键 `HighestAttack = max(Attack, RangedAttack)` 降序 |
| **2** | `OrderByDescending(GetValueInPlay)`（`:1208-1228`） | ✅（但不稳定排序） | ✅ 同键，改成**稳定**排序 |
| **3** | `OrderBy(GetValueInPlay)`（`:1229-1249`） | ✅（同上） | ✅ 同上 |
| **4** | **switch 里没有 case 4** ⇒ 落 `default:` = **随机取 N** | ❌ 「目标排最前」（我们自己发明的） | ✅ 删掉 case 4，并进 `default` |
| **5** | 建新表、**从尾往前**拷 `maxTargets` 个（`:1274-1310`） | 保尾（保持原序） | 同改前（集合一致；**元素次序不同**，见 §四·2） |
| **6** | `OrderBy(CurrentIntrinsicHealth)`（`:1311-1331`） | ⚠️ 裸 `Health` | ✅ `CurrentIntrinsicHealth = Health − Σ(Source==Auras.GrantTag && Attr=="health")` |
| **7** | 建新表、从头拷 `maxTargets` 个（`:1332-1360`） | ✅（走 default 保头） | ✅ 显式 `case 7` 保头 |
| 其它（8…） | 同样落 `default:` = 随机 | ❌ 保前 N | ✅ 随机（与 0/4 同一支） |
| **并列** | LINQ `OrderBy` 稳定 ⇒ 源表序 = `unitsInPlay(+0x470)` 追加序 = 进场序 | ❌ `List.Sort`（**不稳定**）+ 源表是 side→slot | ✅ `StableSortWithPlayOrder`：主键 → `PlayOrder`（`-1` 映射 `int.MaxValue`，排末尾）→ 原下标，三段全序 |

### 1. `case 1` 的键
`CardScript__GetHighestAttackValue.c:6-14` 逐行：`return CurrentMelee < CurrentRange ? 远程 : 近战`
⇒ **`max(近战, 远程)`**（两个都是**运行时当前值**，含 buff）。
我们的对应：`UnitState.Attack` / `UnitState.RangedAttack`（在场），手牌用 `CardDef.Attack/RangedAttack`。
`isRemnant` 主键 = `EntityScript.isRemnant // +0x65`（`EntityScript__get_isRemnant.c`），
升序 ⇒ **非残骸先、残骸后**。

### 2. `default(0)` / `case 4` 的随机形状
原版 `:1250-1273`（逐句照抄在 `AiTargets.TrimRandom` 的 doc 里）：
`GetRandomInt(0, list.Count)` → `Add` 到新表 → `RemoveAt` → 循环 `maxTargets` 次 → 用新表替换源表。
⇒ **无放回**，且**结果次序 = 抽中的次序**（不是原表次序）。我们照抄（`ctx.AiRng.Next(list.Count)`，
上界开区间 = `[0, Count)`，与 `GetRandomInt(0, Count)` 同口径）。
⚠️ 随机源 = **`ctx.AiRng`**（`System.Random(seed ^ 0x0A1A1A1A)`，⛔ 不用 `UnityEngine.Random`）
—— **这是有意偏离原版**（原版单流），理由与后果见 §六。

### 3. `case 6` 的 `CurrentIntrinsicHealth`
判据 = `EntityScript__get_CurrentIntrinsicHealth.c:44-62` 全文：

```
iVar3 = 0;
foreach (eff in activeEffects(+0x108))                       // List<CardEffect>
  if (eff.extrinsic(+0x30) != 0 && eff.buffType(+0x38) == 1 /*addStats*/)
    iVar3 += eff.healthChange(+0x60);
return currentHealth(+0x68) - iVar3;
```
（`BuffType` 枚举 → `dump.cs:45430-45450`；`CardEffect` 字段 → `dump.cs:119060-119120`：
`extrinsic @+0x30` / `buffType @+0x38` / `healthChange @+0x60`。）

**我们的等价物与边界**（已在代码 doc 里逐条写死）：
- 我们**没有** `CardEffect` 对象表（属性 buff 直接折进 `UnitState.Health/MaxHealth`）；
  唯一**带账**的 extrinsic 一族 = **光环层**（`Auras.ApplyAura` → `UnitState.RecordGrant(…, Auras.GrantTag)`，
  `Core/Aura.cs:805`），而原版 `extrinsic = abilityData.whileInPlay || whileInPlayVariable`
  （`dump.cs:19557-19566`），我们的 whileInPlay 能力也确实全落在 `CardDef.AuraSpecs`。
  ⇒ **只减 `Source == Auras.GrantTag && Attr == "health"` 的那几条**。
- ⛔ **不减**黑暗契约那一族（`EffectResolver.cs:10376` 的 grant 来自**永久关键词**，
  原版 `extrinsic` 为假 ⇒ 按原版不减）。
- ⛔ **不采用** `MaxHealth − Card.Health`：那会把**非 extrinsic** 的生命增益（一次性 buff）
  和 `setHealth`（如 `Eternal Servitude` 翻倍）一起减掉，而原版明确只减 `buffType==1 && extrinsic`。
  （⚠️ `AiScoring.ValueOfEnchantments` 用的正是那个代理口径 —— 两者**服务不同的原版函数**，不是矛盾，
  但读的人容易混：见 §五·6。）
- 手牌/牌库候选：原版手里那份 `activeEffects` 为空 ⇒ 直接返回卡面 `Health`（我们的实现同）。

---

## 二、断言逐条（`Editor/RuleEngineTest_G3.cs`，入口 `TestG3AiTargetsTrim`，8 个子用例）

夹具统一：`targetsAffected = 10 (allFiltered)` + `friendlyUnits = true` + `includeHero = false`
+ `includeMinions = true`（残骸那一档额外 `includeRemnants = true`），棋盘 9 格、督军在格 4。

| # | 用例 | 断言（期望值全是字面量） | `🧨` 灭自证（哪一处改动 ⇒ 必红） |
|---|---|---|---|
| ① | `G3Case1RemnantAndHighestAttack` | 残骸（近战 9）+ 近战 4 + **远程 5** 三个候选，`maxTargets=2` ⇒ `G3C1Ranged,G3C1Melee` | **去掉 `isRemnant` 主键** ⇒ 残骸排第一 ⇒ 实得 `G3C1Remnant,G3C1Melee`；**键退回只比近战** ⇒ 实得 `G3C1Melee,G3C1Ranged` |
| ② | `G3Case2And3ValueInPlay` | `method=2` ⇒ `G3C2Pricey`（5 费=11）；`method=3` ⇒ `G3C2Cheap`（1 费=3） | 2/3 写反 ⇒ 各红；键换成近战/血量（两张相同）⇒ 变成并列 ⇒ 红 |
| ③ | `G3Case0Random` | 8 候选、`maxTargets=2`、抽样 **30 次**：每次都 2 个 · 同次不重复 · **出现过源表前两个以外的名字** | **退回「`default` 保前 N」** ⇒ 永远只有 `G3C0U0/G3C0U1` ⇒ 红；忘 `RemoveAt` ⇒ 可能重复 ⇒ 红 |
| ④ | `G3Case4RandomNotTargetFirst` | 8 候选（含 `targetUnit`）、`maxTargets=2`、30 次：**出现过第 0 个不是目标** | **退回「目标排最前」** ⇒ 第 0 个永远是目标 ⇒ 红 |
| ⑤ | `G3Case5And7KeepTailHead` | `method=5` ⇒ `G3C5C,G3C5D`（保尾）；`method=7` ⇒ `G3C5A,G3C5B`（保头） | 两档写反 ⇒ 各红 |
| ⑥ | `G3Case6IntrinsicHealth` | 底 1 + **光环 +4**（当前 5/本征 1）· 底 2 + **契约 +3**（当前 5/本征 5）· 底 3（当前 3）⇒ `maxTargets=2` ⇒ `G3C6Aura,G3C6Plain` | **用裸 `Health`** ⇒ `G3C6Plain,G3C6Aura` ⇒ 红；**把所有 health grant 都减**（不分来源）⇒ `G3C6Pact` 变 2 ⇒ `G3C6Aura,G3C6Pact` ⇒ 红 |
| ⑦ | `G3TieBreakByPlayOrder` | 格 7 先摆 / 格 1 后摆，同键 ⇒ 取**格 7**那张（进场序小）；`method=1` 与 `method=6` 各一条 | **去掉 `PlayOrder` 第二键、退回源表下标（= 格号序 1<7）** ⇒ 实得 `G3TieSecondIn` ⇒ 红；某档自己另写不稳定 `Sort` ⇒ 红/间歇红 |
| ⑧ | `G3RandomReproducible` | 三条判别式：① 两个同 seed 独立对局各连调 3 次 ⇒ 结果串一致；② **先把共享 `ctx.Rng` 烧掉 5 个数** ⇒ 结果**仍一致**；③ **先把 `ctx.AiRng` 烧掉 5 个数** ⇒ 结果串**必须不同** | 抽签改用 `ctx.Rng` ⇒ ② 红；改用 `UnityEngine.Random` / 无 seed 的 `new Random()` ⇒ ① 红；改用别的流 ⇒ ③ 红（误判概率 `≈ (1/P(8,3))³ ≈ 3e-8`） |

**为什么 ③④⑧ 不写死「抽中了谁」**：那等于把实现的 RNG 序列抄进断言（自证 + 一改就假红）。
改成断**规则的结构性质**并给概率：
- ③ 旧写法（保前 2）**必然**只给前两个；随机侧误报率 = `(1/C(8,2))^30 = (1/28)^30 ≈ 0`；
- ④ 旧写法（目标排最前）**必然**目标在第 0 位；随机侧误报率 = `(1/8)^30 ≈ 0`；
- ⑧ 断的是「同 seed ⇒ 同结果」，不涉及具体抽签。

另设前提断言（防「夹具没建立 ⇒ 断言在错误的理由下假绿」）：残骸确实 `IsRemnant` 且近战最高 ·
三次 `RecordGrant` 后当前生命确实是 5/5/3 · 格 7 那张 `PlayOrder` 确实更小 · `targetUnit` 确实在盘上。

---

## 三、影响面 / 读数（现读，非估算）

### 1. `ruleprobe` **照不到**（明说）
`工具/ruleprobe/ruleprobe.csproj` 关掉 `EnableDefaultCompileItems`，只显式编
`Core/*.cs` + 少数 `Data/` 文件（`CardDatabase.cs`）—— **`Data/AiTargets.cs` 不在清单里**
⇒ `ruleprobe` 给不出这块的读数；本件的观测口只有 `RuleEngineTest_G3.cs`（Unity 侧）。

### 2. AI 评分层里**真会走到 Trim** 的判据 —— 现读 `ai_scoring.json` = **7 条**（不是 C5 说的 1 条）

| 卡 | `targetSelectMethod` | `maxTargets` | 改前 → 改后 |
|---|---|---|---|
| `SOR51 Divine Intervention` | **1** | 1 | 键：近战降序 → **`max(近战,远程)` 降序**；并列：枚举序 → **进场序**。`includeRemnants` 缺省 false ⇒ 残骸已被筛掉 ⇒ **`isRemnant` 主键对它今天是空转** |
| `BL53 Heldrake Strike` | **0** | 3 | **保前 3 → 随机取 3** |
| `SOR48 Imperial Creed` | **0** | 3 | **保前 3 → 随机取 3** |
| `Lord of the Storm` (`30007162…`) | **0** | 1 | **保第 1 → 随机取 1** |
| `Hero of the Empire` (`9f75a6…`) | **0** | 1 | 同上 |
| `GSC56 A Trap Sprung` | **0** | 1 | 同上 |
| `Grisly Feast` (`b3735ef3…`) | **0** | 1 | 同上 |

（7 张**全部在池内**，现读 `cards_engine.json` id/name 双向命中；`count = 1127`。）
⇒ **`SOR51` 的评分预期怎么变**：它的 `scoringOption = 2 (SumValueInPlay)`，`maxTargets = 1`
⇒ 分数里那一项 = **被选中那一个敌方小兵**的 `ValueInPlay`。改后当「敌方**近战**最高的」≠
「敌方 **max(近战,远程)** 最高的」时，选中的目标会换人 ⇒ **评分跟着变**（远程单位从此能被选中）；
并列时按敌方进场序取（改前按 side→slot 枚举序，恰好也是格号序 ⇒ 大部分局面结果相同）。

### 3. 另外 6 条 0 档会**消耗 `ctx.AiRng`**（改前一次都不掷）
AI 评分在**动作评估**阶段就会调 `GetTargets` ⇒ 只要那 6 张里有一张在 AI 手里被评估，
就会从 **`ctx.AiRng`** 取数（主对话 2026-10-24 拍板：**不碰共享的 `ctx.Rng`**，见 §六）。
后果分两层：
- ✅ **回放 / 伤害那一路不受影响**：`ctx.Rng` 一个数都没多花 ⇒ 局内随机目标、伤害范围、
  以及「按种子写死期望值」的既有断言**不会整体漂移**。
- ⚠️ **AI 自己的行为会变**：`AiRng` **同时**被 `SimpleAI.TweakAvailableActions` 的跳动作掷骰用
  （`Data/SimpleAI.cs:1297`，每步 0~4 次）⇒ 评分抽签与掷骰**交错消费同一条派生流**
  ⇒ 同一 seed 下 AI 的「跳不跳这一步」可能与改前不同。仍然**可复现**（同 seed ⇒ 同结果）、
  且**回放不受影响**（回放录的是动作，不重掷）。⇒ `RuleEngineTest.Run` 里若有按 seed 写死
  AI 动作的断言，仍可能因 AI 决策改变而漂移，**建议复跑一次**。

### 4. 零风险的部分
`targetSelectMethod == 2/3/5/7` 在 `ai_scoring` 层 **0 条**；`case 6` 在评分层 **0 条**
⇒ 这三块今天**不可观测**（只为 `abilityLogic` 那一层预备，而那一层我们还没跑）。
`Trim` 的调用点只有 `AiScoring.cs:123,126`（现读全仓 grep，表现层不调）。

---

## 四、没查清 / 没做

1. **没有跑 Unity 自检**（红线：子代理不跑 Unity）⇒ 本件 8 个子用例**尚未实跑**，
   上面的「应该绿」是静态推演。建议主对话复跑 `RuleEngineTest.Run`（3m53s）一次。
2. **`CurrentIntrinsicHealth` 有一处已知缺口**：如果哪天出现「不走 `RecordGrant` 的 extrinsic
   生命增益」，本键会偏高（少减）。今天**没有这种产出者**（全仓 `RecordGrant` 调用点 3 处，
   `health` 只来自光环与黑暗契约），但这是**实现边界**，不是「与原版等价」。
3. **`case 5` 的元素次序没对齐**：见 §五·2（集合对、次序不对），未改（不在本批四偏差范围内）。
4. **`case 2/3` 对手牌/牌库候选仍一律 0**（C5 顺手发现 3）——未改，仍用 `SimpleAI.ValueInPlay(Unit)`
   而不是 `AiScoring.ValueInPlay(AiTargetRef)`。当前数据 0 条，不可观测。
5. **`SOR51` 评分变化的具体幅度没量**（需要真打或跑评分 dump，本批无 Unity）。
6. **未核对** `GetTargets` 里 `param_6`（`applyMaxTargets`）在**所有**调用点是否都传 true
   —— AI 那一路（`AI__ScoreFromCriteria.c:83,93`）已由 C5 核过；别的调用点本轮没查。

---

## 五、顺手发现（超出了本批四偏差，但下个会话要 know）

1. 🔴 **C5 §2「其余 236 条都是 0（且其中 0 条 `maxTargets>0`）」不成立** ——
   现读 `ai_scoring.json`：`targetSelectMethod != 0 或 maxTargets > 0` 的判据共 **7 条**，
   其中 **6 条是 method 0 且 maxTargets>0**（见 §三·2 表）。C5 那个「0 条」大概是漏看了
   `maxTargets` 一旦 >0 就会触发 `default:` 那条随机支（method 0 也是随机，不是「不截」）。
   ⇒ 影响面比 C5 说的大：这 6 张卡的 AI 评分从**确定性**变成**随机**。
2. 🟡 **`case 5`（lastAdded）原版结果的元素次序是【反的】**：`:1274-1310` 是**从尾往前**拷进新表
   ⇒ `[a,b,c,d,e]`、`maxTargets=2` 得 **`[e,d]`**；我们的 `RemoveRange(0, …)` 得 **`[d,e]`**。
   C5 写「✅ 等价的『保尾部』」—— **集合等价、次序不等价**。今天消费点（计数 / 求和）不看次序 ⇒
   不可观测；但若将来有地方取 `targets[0]` 当「最后进场的那一个」，会错。**未改**（不在本批范围）。
3. 🟡 **`RuleEngineTest_Diag.cs` 现在过期了**（我加了 `Step(TestG3AiTargetsTrim)`）：
   现跑 `PYTHONIOENCODING=utf-8 python -I -X utf8 工具/gen_diag_runner.py --check` 输出
   「第 91 行起不一样 …… 盘上缺 `TestG3AiTargetsTrim`」。**该文件不在我的白名单** ⇒
   请主对话跑一次 `PYTHONIOENCODING=utf-8 python D:\4\Unity\工具\gen_diag_runner.py`
   （纯工具、一条命令、不跑 Unity）。不跑的话每次 Unity 起来 `DiagDriftAlarm()` 都会 `LogError`，
   且 `RunSafe()` 的 `VerifyDiagCoverage` 会对不上。
4. 🟢 **`AiTargetRef` 新加的 4 个派生属性是纯增量**（`RangedAttack` / `HighestAttack` / `IsRemnant` / `PlayOrder`），
   不碰结构体布局语义（都是只读属性），既有调用点零影响（typecheck 双程序集 0 错）。
5. 🔴 **`ctx.Rng` vs `ctx.AiRng` —— 已拍板，改走 `AiRng`**（主对话 2026-10-24）：
   本仓既有决定是「AI 这一侧的随机不挪动共享流」（`BattleContext.cs:371-377`），
   而原版是单流 `BattleManager.GetRandomInt`。⇒ **契约优先**（回放不重掷），采用 `AiRng`，
   并在 `AiTargets.cs` 的块注释与 `RuleEngineTest_G3.cs` 的 ⑧ 里写明**这是有意偏离、不是等价**。
   完整说明见 §六。
6. 🟢 **两个「intrinsic / addStats 代理」口径不同，别互相引用**：
   `AiScoring.ValueOfEnchantments` 用 `MaxHealth − Card.Health`（把 **extrinsic 也算进去**，
   因为它服务的原版函数是「**跳过** extrinsic」的那一个）；本件 `CurrentIntrinsicHealth`
   只减 **extrinsic**（它服务的原版函数正是「只减 extrinsic」）。两者各自对得上自己的原版函数，
   ⛔ 不要为了「统一」把其中一个改成另一个的写法。
7. 🟢 `case 1` 的 `isRemnant` 主键在**今天的 AI 数据里是空转**（SOR51 的 `includeRemnants` 缺省 false
   ⇒ 残骸在 `ApplyFilters` 就被筛掉，根本到不了 `Trim`）。它只在 `abilityLogic` 那一层
   （`includeRemnants=true` 的 criteria）才有意义 —— 所以断言里必须显式打开 `includeRemnants`，
   否则这条规则**永远测不到**（我已在夹具里写明）。


---

## 六、Rng 口径与偏离说明（主对话 2026-10-24 拍板）

**结论：`AiTargets.Trim` 的随机抽签走 `ctx.AiRng`，⛔ 不走共享的 `ctx.Rng`。**

### 1. 原版是什么
`AbilityLogic.GetTargets` 的 `default:` 支用 `BattleManager.GetRandomInt(manager, 0, count)`
（`:1250-1273`），而 `BattleManager` 只有**一条**随机流 —— AI 评分、伤害范围、随机目标
**全部共用**它。⇒ 逐位复刻的话，评分抽签会挪动整局的随机序列。

### 2. 我们为什么偏离
本仓既有决定（`Core/BattleContext.cs:367-377`，`新账-88` 那批定的）：
**AI 这一侧的随机走独立派生流 `AiRng`（`seed ^ 0x0A1A1A1A`），不挪动共享流。**
理由 = **回放契约**：回放录的是「起始条件 + 动作流」，**不重掷**（`Battle/ReplayStore.cs` 文件头）；
共享流一动，同一局后续动作的随机结果整体错位，录下来的动作序列就对不上。
同族先例 = `ShowRng`（表现层抽候选，`BattleContext.cs:355-365`，理由逐字相同）。

### 3. 偏离的**具体后果**（不粉饰）
- ✅ **共享 `ctx.Rng` 一个数都不多花** ⇒ 伤害/随机目标/洗牌… 以及大量「按种子写死期望值」的
  既有断言**不会**因此漂移。断言 ⑧② 直接把这一条钉死（烧 `Rng` 5 个数，结果必须不变）。
- ⚠️ **`AiRng` 与 `SimpleAI.TweakAvailableActions` 的跳动作掷骰（`Data/SimpleAI.cs:1297`，
  每步 0~4 次）是同一条流** ⇒ 评分抽签与掷骰**交错消费** ⇒ 同一 seed 下 AI 的决策序列
  可能与改前不同（仍然可复现、回放仍然对得上）。⇒ 若 `RuleEngineTest.Run` 里有按 seed
  写死 AI 动作的断言，**可能因 AI 决策改变而漂移**，请复跑确认。
- ⚠️ **我们与原版的同种子局不再逐位相同** —— 这是**契约优先**的取舍，**不是**「等价」。

### 4. 断言怎么钉住这件事（`RuleEngineTest_G3.G3RandomReproducible`）
| 断言 | 期望 | `🧨` |
|---|---|---|
| ① 两个同 seed 独立对局，各连调 3 次 | 结果串一致 | 无 seed 的 `new Random()` / `UnityEngine.Random` ⇒ 红 |
| ② 先烧共享 `ctx.Rng` 5 个数再调 | 结果串**仍一致** | 抽签用 `ctx.Rng` ⇒ 必不同 ⇒ 红 |
| ③ 先烧 `ctx.AiRng` 5 个数再调 | 结果串**必须不同** | 抽签用别的流 ⇒ 与 ① 相同 ⇒ 红（误判概率 `≈ (1/P(8,3))³ ≈ 3e-8`） |

代码侧的同一句话写在 `Data/AiTargets.cs` 的 `Trim` 块注释与 `TrimRandom` 的 doc 里
（「**有意偏离原版**：原版单流 vs 我们 `AiRng`，理由是回放不重掷」），
⛔ 别让后人把它当成「原版就是这样」。
