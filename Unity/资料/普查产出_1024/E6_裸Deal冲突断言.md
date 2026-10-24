# E6 · 裸 `Deal N damage` 改回 `ta=30` 之后的冲突断言修复

> 角色：**断言修复代理 E6**（2026-10-24）。只改白名单两份测试分册，⛔ 不跑 Unity / 不动 git / 不改正本 / 不越白名单。
> 判据来源 = **G5 报告**（`资料/普查产出_1024/G5_裸Deal与ta200.md` §1·5 的 4 处）＋ 第一权威反编译
> （`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:192-207`）。
> ✅ 已跑：`TMPDIR=/tmp/wf_e6 bash d:/4/Unity/工具/typecheck.sh` → **运行时 0 / 编辑器 0**（改完两轮都跑过）。
> ✅ 另跑（纯 dotnet，不碰 Unity）：`bash 工具/ruleprobe.sh card "Annihilation Barge"` —— 现读确认
> `SAU73` 卡面解析成**一条** `deal amount=3`、`Side=enemy / Kind=any / Count=1 / Auto=0 / PickMost=-health`。

---

## 一 · 逐处改动（改前 → 改后）

### 件 1 · `RuleEngineTest.cs` ⑦ 段（原 `:11848-11868`，现 `:11856-11895`）

**改前**
- 头注：「裸 `Deal N damage` 打的是**生命最低**的敌人（2026-09-18 用户拍板）……原版是 `BattleManager.GetLowestHealthUnit`」
- 断言：`Board(1,5).Health == 1`（打 2 血的 B）· `Board(1,3).Health == 9`（9 血的 A 没挨）

**改后**
- 头注整段重写并**留更正痕**：裸 Deal = 原版 `ta=30 (target)`，**不做任何筛选**，由施放方（玩家 / AI）给目标；
  「生命最低」是另一档 `ta=240`（只有卡面明写 `with lowest Health` 的卡走）。错因 = 「把 AI 会替玩家挑
  当成了引擎自己挑」。**并如实标出本格走的是「没有人选」的近似**（原版此时给空表，我们落 `ResolveTargets`
  末段的「池序前 N」），这一格与原版**不等价**。
- **新增一条规格断言**：`!Target.Auto && string.IsNullOrEmpty(Target.PickMost)`（🧨 改回 `Auto=true` / `-health` ⇒ 红）
- 期望翻转：`Board(1,3).Health == 8`（池序第一的 A：9 → 8）· `Board(1,5).Health == 2`（2 血的 B 一点没挨）
- 判别力**没减**：夹具仍是「槽号小的那个血更多」，两条口径（池序 vs 生命最低）指向不同人。

### 件 2 · `RuleEngineTest.cs` 平手段（原 `:11870-11888`，现 `:11897-11922`）

**只订正注释与断言文案，期望值一个字没动**（`Board(1,3)==2` / `Board(1,5)==3`）。
- 新增更正痕：这格**仍然绿，但绿的理由变了** —— 裸 Deal 不再走 `-health`，它现在走的是「池序前 N」的近似；
  C 恰好**既是先进场、又占更小的格号** ⇒ 两套口径同一个人、**分不开**。
- 明写：**「并列取 `PlayOrder` 在先者」这条判据的判别力现在只由 `RuleEngineTest_EngineB7.cs` 的 ①②③④ 承担**
  （它们仍用真正的 `PickMost = -health` 规格，且 ② 故意让进场序与格号序相反），⛔ 别拿本格当证据。

### 件 3 · `RuleEngineTest.cs` 主动技能伤害预览（原 `:27204-27205`，现 `:27232-27251`）

**改前**：`ActiveAbilityDamageEntries(...).Count == 0`，理由「裸 Deal 是 `lowestHealth(240)`，只认 `target(30)`」。

**改后**：期望 **1 条**、**值 3**（`oathEntries[0] == 3`）；注释就地订正并留更正痕（原来的 `ta=240` 假设被 C4 证伪：
两张 `ta=240` 都不是裸 Deal；全池 33/33 张裸 Deal = `ta=30`）。
另外把该块的头注从「三个都不是『打到玩家点的那个敌人』的来源」改成「治疗 / `EnemyWarlord` 两档不是
`ta=30` 的来源」，并标注第三条判决已翻转。

### 件 4 · `RuleEngineTest_EngineB7.cs` ⑥（原 `:145-163`，现 `:156-186`）—— 见第二节

---

## 二 · B7 ⑥ 换的样本与判别力说明

**原样本**：裸 `Deal 2 damage`（前提断 `PickMost == "-health"`；端到端断 4 血 `live` 4 → 2、残骸 1 血不挨）。
⇒ G5 改口径后前提与两条端到端**同时失去意义**（裸 Deal 不再带 `PickMost`，也就考不到 `!isRemnant` 那道闸）。

**新样本**：**`SAU73 Annihilation Barge`**（卡面 `Artifice: Deal 3 damage to the enemy with lowest Health.`）。
- 为什么是它：全池**只有两张** `ta=240`（`SAU70 Damaged Hexmark` / `SAU73`），而 `SAU70` 的卡面是
  `Attacks a random enemy with lowest Health`（`forceattack`，不是 `deal`）⇒ **`SAU73` 是唯一能走 `deal` 的 `ta=240` 真卡**。
- ⚠️ **就地纠正简报里的一处**：简报举的 `AM5 Shock Trooper` / `ASH30 Fire Prism` **都不是 `lowest Health` 卡** ——
  现读 `cards_engine.json`：`AM5` = `Deal 1 damage`（裸 Deal）、`ASH30` = `Deal 3 damage. If the target has Armour,
  deal 8 damage instead`（裸 Deal + 条件换值）。两张恰好是 G5 §5·3 里「`subjTa = -1` 读不到 30」的那一族。
- 规格**从卡池现读**（`CardDatabase.Load()` + `CardDatabase.Find(pool, "Annihilation Barge")` + `EffectText.Parse(barge.Desc)`），
  ⛔ 不是手抄字符串 —— 卡面被改坏会在这里红。
- 前提断言：**一条** op · `Amount == 3` · `PickMost == "-health"`（🧨 卡面改回裸 Deal / 解析丢 `PickMost` ⇒ 红）。

**判别力（为什么没减）**：残骸（1 血、`IsAlive` 真）故意摆在**池序/格号更前**的槽 3，活得那支在槽 5（6 血）。
- 少了 `if (u.IsRemnant) continue;` ⇒ 最低血候选变成残骸 ⇒ 3 点打到残骸（1 → 0）、`live` 不动 ⇒
  `Check(live.Health, 3)` 实得 6 **红** + `Check(rem.Health, 1)` 实得 0 **红**（两条同时红）。
- 正常时：候选 = `live`(6) + 敌督军(30)，严格小于 + 初始 best = 督军 ⇒ 挑 `live` ⇒ 6 → 3；残骸 1。
- 伤害值从 2 提到 3（跟卡面走），`live` 血量从 4 提到 6 ⇒ 结果 `3 / 1` 两个数**互不相同**，
  读失败时不会出现「两个 1 撞在一起、看起来像对的」。

**同批订正的三处同族过期文案**（都在白名单内）：
`RuleEngineTest_EngineB7.cs` 文件头 ①（原来写「裸 `Deal N damage` 走的正是它（`ta=240`）」）·
`B7Lowest(...)` 的 doc（原来写「与裸 Deal 解析出来的同一个形状」）· `Run()` 里那段注释 + `Section(...)` 文案
（`RuleEngineTest.cs:971-976`）——全部带更正痕。

---

## 三 · 有没有不得不放宽的地方

**没有放宽，反而多钉了一条。**
- 7 处改动里，**只有 1 处把期望值往「更松」的方向动了**：件 2 的平手段——但那是**判据本身改了**（裸 Deal 不再走
  `-health`），期望值**一个字没动**，只是把「谁在守这条规则」的说明指向了 B7 ①②③④。
- 件 1 多了一条规格断言（`!Auto && PickMost 为空`），件 3 多了一条值断言（`oathEntries[0] == 3`），
  B7 ⑥ 多了一条「样本是 `deal 3`、带 `-health`」的前提断言。
- ⚠️ **一处如实标注的「不等价」**：件 1 那格（没有选择方）实证的是**我们的近似**，不是原版语义
  （原版 `ta=30` 在 `targetCard == null` 时给**空表**）。注释里写明了「这一格与原版不等价」，
  与 G5 报告 §四·1 的记法一致 —— 这不叫放宽，叫**别让断言冒充原版**。

---

## 四 · 没查清 / 没做

1. **没跑 Unity**（红线）：`RuleEngineTest.Run` / `RuleEngineTest.B7` 那几条**没有实跑过**，只过了 typecheck。
   `run()` 用例表没有变化（我没有新增/删除 `Step`），所以**不需要**主对话重生成 `_Diag`；
   但那 7 处期望值必须靠一次实跑验收。
2. **B7 ⑥ 的端到端结果是我按代码路径推的，没跑**：
   `ResolveTargets` 的 `-health` 支（`EffectResolver.cs:1706-1776`）里 `IsRemnant` / `IsPendingDeath` /
   `EnoughPendingDamageToDie` 三道闸 + 「初始 best = 该侧督军 + 严格小于」⇒ 挑 `live`（6）。推得较硬，
   但**「推的」≠「跑过的」**，如实标。
3. **全池还有没有别的测试在依赖「裸 Deal = 生命最低」**：我在两份白名单文件里按
   `.Auto` / `PickMost` / `lowest` / `Parse("Deal N` / `ParseSegment("deal N` 逐一扫过，
   除 G5 列的 4 处外还扫出 **1 处**（见第五节），已修。
   ⛔ **黑名单里的分册没有扫**（`_G5` / `_G6` / `_EngineB6` / `_EngineA1` / `_EngineB3` / `_EngineF8` / `_G1b` /
   `_G2` / `_G3` / `_N1` / `_Priority` / `_EntryPump` / `_Diag`）——若它们也有依赖，等它们那一侧的执行代理或主对话收口。
4. **`Data/SimpleAI.cs:902` 的过期注释**（G5 §5·3 已记）**仍未改** —— `Data/**` 不在我的白名单。

---

## 五 · 顺手发现（只报，已在白名单内顺手修 1 处）

1. ✅ **已修（G5 报告没列到的第 5 处冲突断言）**：`RuleEngineTest.cs:7673-7685`
   `EffectText.ParseSegment("deal 5 damage and gain 1 Armour")` 那组，原来断
   `Target.Auto == true` **且** `Target.PickMost == "-health"`（注释写「那一支现在**真的**按生命最低挑了」）。
   G5 改口径后**两条都会红**（`Auto` 变 false、`PickMost` 变空）。已按同一判据翻转成
   `!Target.Auto` + `PickMost` 为空，并留更正痕。
   🔴 **这条值得记账**：G5 的 §1·5 只列了 4 处，说明它的普查口径（大概是只看了 `RuleEngineTest` 里
   带 `118xx` / `272xx` 编号的那几段）漏了这一处 —— 换一个执行代理时同样的漏检还会发生。

2. 🟡 **`RuleEngineTest.cs:3602` 的一句旧话**：注释说 `Deal 99 damage to a friendly unit`「是自动选目标的」。
   那句写的是**明写了目标**的卡（`to a friendly unit`），走的不是裸 Deal 那一支 ⇒ 这句话今天**仍然成立**，
   我没有动。但同一个词「自动选目标」在本仓现在有两种含义（`Auto=true` 的旧口径 vs 裸 Deal 的新口径），
   以后容易再混 —— 建议主对话在收口时统一措辞。

3. 🟡 **`EffectText.PickTarget` 现在会认裸 Deal 为「要玩家选目标」**（G5 §5·2 的连带效应）：
   真机上「打裸 Deal 的战术卡」现在要**点一个目标**才打得出去。这条**不是缺陷**（与原版 `ta=30` 同向），
   但它会改变 UI 交互 ⇒ `资料/真Play待验清单.md` 值得补一条「打一张裸 Deal 战术卡 / 用一手裸 Deal 主动技能」。

4. 🟡 **`AM5 Shock Trooper` / `ASH30 Fire Prism` 不是 `lowest Health` 卡**（见第二节）。
   简报把它们当样本是**按名字猜的**，实测卡面证伪。以后再要 `ta=240` 的样本，**只认 `SAU70` / `SAU73` 两张**，
   且 `SAU70` 只走 `forceattack`。

---

## 六 · 改动清单（文件 → 行段）

| 文件 | 行段（改后现读） | 内容 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` | `:971-977` | `Run()` 注释 + `Section(...)` 文案（B7 段落标题，`Section` 在 `:977`） |
| 同上 | `:7670-7685` | ✅ **第 5 处**（G5 没列）：`ParseSegment("deal 5 damage …")` 的 `Auto` / `PickMost`（断言在 `:7674-7685`） |
| 同上 | `:11856-11895` | ⑦ 段：头注重写 + 新增规格断言 + 期望翻转（A 9→8 / B 不挨） |
| 同上 | `:11897-11922` | 平手段：只订正注释/文案（期望值不动） |
| 同上 | `:27217-27251` | 伤害预览：头注 + 期望 0 → **1 条 / 值 3** |
| `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_EngineB7.cs` | `:9-18` | 文件头 ① 判据（`ta=240` 只服务明写 `lowest Health` 的卡） |
| 同上 | `:34` | `Section(...)` 文案 |
| 同上 | `:39-43` | `B7Lowest(...)` 的 doc（不再说「与裸 Deal 同形」） |
| 同上 | `:54-56` | ① 段头注 |
| 同上 | `:156-186` | ⑥ 换样本 `SAU73`（前提 + 端到端 6→3 / 残骸不挨） |

**行尾**：`RuleEngineTest.cs` 改后 **CRLF 36811 / 裸 LF 0**（原样）；`RuleEngineTest_EngineB7.cs` **纯 LF 285**（原样）。
**typecheck**：改完两轮均 `运行时 0 / 编辑器 0`。
