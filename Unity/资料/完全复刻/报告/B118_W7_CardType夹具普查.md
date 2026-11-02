# B118-W7：`CardDef.CardType` 缺省 `-1` 的夹具普查

任务来源：`B117-R72` · 判据 → `资料/完全复刻/判据/B117-FIXG_誓约兜底与dropPod.md` §二（`SetupDropPodStats` 第一道闸 `CardType != Hero` 静默 return）。
对象：`RuleEngine/Core/CardDef.cs:6610` `int _cardType = GateUnknown;`（`GateUnknown = -1`，`:231`），只有 `CardDatabase.Parse` 或夹具自己 `AttachCardType(...)` 才写它。

## 1 · 普查读数

**现读（2026-11-02，`CardPresentation/Editor` 全目录）**：

| 指标 | 读数 |
|---|---|
| `new CardDef(` 处 / 文件 | **231 / 89**（⚠️ 含本波别的写手正在建的 `B118W2/W5/W6_Assert.cs`；任务简报的「241 / 85」是旧数，已就地订正） |
| 同函数**无** `AttachCardType` | **217 / 87 文件** |
| 同函数无 `AttachCardType` 且该函数触及 6 个消费点之一（普查启发式） | **26 处**（全部 `C3_UnitStateCtor`；其中 2 处同时触及 `C4_低血无敌`） |
| 机器闸 `--check`（本文件范围，按卡种收窄） | **0 `[direct]` + 8 `[same-function]`** |
| 机器闸 `--check`（全 `MyGame/Assets`） | **1 `[direct]` + 12 `[same-function]`** |

**6 个消费点现读**（逐个读过方法体）：
① `RuleCore.cs:3999` `card.CardType == CardTypeTactic`（`CardPlayedMask` 的 Tactic 位）
② `RuleCore.cs:15960-15964` `GetCardsInCemetery` 的四档过滤 + 督军无条件排除（`-1` 时四档全放行）
③ `UnitState.cs:3907` `SetupDropPodStats`（`CardType == CardTypeHero`；**无条件在主构造里调**）
④ `UnitState.cs:3941` `SetupFTUELowLifeInvulnerability`（`CardType == CardTypeHero`；显式调）
⑤ `EffectResolverPartial_B88E2.cs:727` 弃整手牌跳过 `Whispers(40)`
⑥ `Enchantment.cs:515` `FilterByQuestCounter`（`CardType == Whispers`）
另：`CardDef.IsTrapCard`（`Tactic ∧ SpellType==Trap`）→ `CardInstance.ArmedTrap`（`CardInstance.cs:474`）。

**逐档语义（决定「会不会真出错」）**：
- ③④ 只有 **hero** 卡会不同；非 hero（Minion/Tactic/Whispers）在 `SetupDropPodStats` 里与 `-1` **同样 return** ⇒ 无差。
- ①`CardPlayedMask` 只有 **tactic** 位会不同；⑤ 只有 **whispers** 会不同；② 任何卡种都可能被误放行（但见 §4）。
- 本仓**零处** `IsTrapCard` / `ArmedTrap` / `FilterByQuestCounter` / `CardPlayedTag.Tactic` 的断言引用。

## 2 · 已改清单（2 处，均属「会静默出错」）

| 文件:行号 | 改动 | 为什么属于「会静默出错」 |
|---|---|---|
| `CardPresentation/Editor/B97W34_Assert.cs:243` | 在 `wl` 后加 `wl.AttachCardType(CardDef.CardTypeHero);`（新行 `:245`） | B1 的 `wl` 是 `type:"hero"` 的督军，喂进 `ps.Warlord = new UnitState(ctx.NewInstance(wl), true)`（`:248`）⇒ 走 `SetupDropPodStats` 第一道闸。`-1` 与 `10` 的结果**在这条用例里恰好相同**（该卡没有 `dropPod`），但它正是 `B117DROPPOD` 那一类：**夹具的卡型是假的，闸永远在第一步 return**。挂上后与样板 `B97W22_Assert.cs:93` 同形。 |
| `CardPresentation/Editor/B97W34_Assert.cs:269` | 在 `wl` 后加 `wl.AttachCardType(CardDef.CardTypeHero);`（新行 `:271`） | 同上（B3 的 `wl`，`:272` 喂进 `new UnitState`）。 |

- 语义安全：`AttachCardType` 只写 `_cardType`，不影响 B1/B2/B3 所测的 `startingCardInHand`（逐字未变）。
- 行尾：文件前后均 LF（`CRLF 0 / LF 313 / 裸 CR 0`）；`git diff --numstat` = **+2 / -0**。
- 验证：`TMPDIR=/tmp/wf_w7 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；`check_aggregate_lines.py B97W34_Assert.cs` ⇒ **PASS**（无聚合行）。

## 3 · 机器闸 `check_fixture_cardtype.py`

**位置**：`D:/4/Unity/工具/check_fixture_cardtype.py`（⛔ 不在 `Assets/`；**裸跑只打用法、`rc=2`、零写盘**）。

**用法**：
```bash
python -I -X utf8 d:/4/Unity/工具/check_fixture_cardtype.py --check                          # 全 Assets
python -I -X utf8 d:/4/Unity/工具/check_fixture_cardtype.py --check --root MyGame/Assets/CardPresentation/Editor
python -I -X utf8 d:/4/Unity/工具/check_fixture_cardtype.py --check --verbose                 # 连 [same-function] 候选一起列
```
退出码：`0` = 无 `[direct]`；`1` = 有 `[direct]`；`2` = 没给动作参数 / 用法错误。

**规则（按卡种收窄，避免「非督军走 `SetupDropPodStats` 也报」的噪声）**：同一函数里 `new CardDef(` 且**无** `AttachCardType`，且该变量被喂进——
`GetCardsInCemetery`（全卡种）· `IsTrapCard`/`CardPlayedMask`（只报 tactic）· `dropPod`/`低血无敌`/`new UnitState`（只报 hero）· `FilterByQuestCounter`（只报 whispers）。
`[direct]` = 变量出现在触发调用的实参里（硬违规）；`[same-function]` = 同函数有触发调用、间接流（只打印）。

**本次读数**：
- 本文件范围（`CardPresentation/Editor`）：`0 [direct]` / `8 [same-function]` ⇒ **rc=0**。
- 全 `MyGame/Assets`：`1 [direct]` / `12 [same-function]` ⇒ **rc=1**，唯一那条 **不在本任务写权限内**：
  `RuleEngine/Editor/RuleEngineTest.cs:39435 | hero | fn@39434 | UnitStateCtor`（实测该督军无 `dropPod`，今天行为无差；建议引擎线补一行 `hero.AttachCardType(CardDef.CardTypeHero)`）。

## 4 · 未改的那批 + 为什么

1. **`[same-function]` 8 处**（`B102C506:56` · `B102EXEC:52` · `B103R5:213/216` · `B103W5:54` · `B104_D:477` · `B104_MT:52` · `B111_E1:237`）：全是**表达体 Hero 工厂**（`static CardDef Hero(...) => new CardDef(..., "hero", ...)`），卡被喂进 `NewBattle` 的卡组、由引擎内部建 `UnitState`。今天**无 `dropPod` / FTUE 断言** ⇒ 行为无差。按简报「别为了整齐全改」保留；闸里作 warning，未来给它们加 `dropPod` 时应就地补。
2. **其余 ~200 处非 hero 夹具**：`Unit`/`Tactic`/`Enchant` 工厂。逐档核对：③④ 对非 hero 与 `-1` 同结果；⑤ 夹具手牌里无 whispers；① 只在 tactic 卡上不同，而唯一测 `CardPlayedCount`（`attackingType` 闸）的 `B103W1_Assert.cs` **早已挂对**（`:86`）。
3. **② 高风险那条在本仓不成立**：全仓只有 3 个文件调 `GetCardsInCemetery`——`B98G2:264`（全开）· `B98K4:137`（`true,true,true,true,true,false`，前四档全开）· `B97_W3:64/86/96`（全开）。**没有任何一处传 `false` 的 `inclMinions/Spells/Whispers`** ⇒ 简报说的「四条 `incl*` 开关全不过滤 ⇒ 断言空转」今天没有实例。
4. **`B97W22_Assert.cs:220` 的 `troop`（F3 负对照）**：`-1` 与 `Minion(0)` 在 `SetupFTUELowLifeInvulnerability` 里都得出「假」⇒ 结果相同，未改。样板 `Hero()`（`:93`）本来已挂。
5. **已挂对的 4 个样板**：`B97W22:93` · `B98G2:60` · `B103W1:86` · `BattleScene:12299`（后者只读、⛔ 未改）。

## 5 · 账补丁

`账/_parts/B118_W7.tsv`：**只有 11 列表头、无数据行**。
理由：本任务只改**夹具**（`_Assert.cs`），不改任何原版方法的复刻状态；账记录的是「原版类 · 方法」的 A/B/C/D 状态，夹具修正不产生新行。相关方法（`CardScript.CardSetup`）的账行 = `战斗逻辑驱动.tsv:707`，本次未动它（行为未变）。

## 6 · 顺手发现

1. **`RuleEngine/Editor/RuleEngineTest.cs:39435`**：`CheckB93F1UnitPoisonedBroadcast` 的 `hero` 夹具缺 `AttachCardType`（本闸唯一 `[direct]`，闸因此 rc=1）。不在本任务写权限内，交调度台派引擎线。今天该卡无 `dropPod` ⇒ 行为无差。
2. **`Enchantment.cs:515` 的 `CardTypeWhispers(40)` 与数据口径存疑**：`B115_W14` 报告说「tactic / defence / enchantment 真值这三个都是 `cardType=20`」，而 `FilterByQuestCounter` 要求 `== 40` ⇒ 若真值 20，则该闸对真实任务附魔**永不命中**。当前 `spellType==120` 池内 **0 张** ⇒ 不可达、不影响今天；但这是引擎/数据侧的疑点，记于此（⛔ 非夹具问题）。
3. `AiTargets.cs:807` 与 `CampaignScriptRunner.cs:394/1045` 对 `CardType < 0` 有**自愈兜底**（`ai_scoring` → `Type` 映射 / `Type` 映射）⇒ 不算静默出错；`AiTargets` 还会 `LogWarning` 一次。
4. **普查工具**：一次性脚本 `D:/4/Unity/工具/_tmp_b118w7_census.py`（只读、仅打表）。它按「整文件」兜底表达体方法 ⇒ 会把同文件的触发调用算进来（26 行）；正式闸 `check_fixture_cardtype.py` 用卡种收窄后为 8 行。两者差异是启发式，不是矛盾。
5. 本波 `B118W2/W5/W6` 正在建新 `_Assert.cs`（我在普查途中看到它们出现）⇒ 复核读数含这三份；它们归各自写手，我未碰。
