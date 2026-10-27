# A10 · oath 付费闸（`CardScript.NeedsToChooseFromPool` / `oath 1275`）

> **代理**：A10（执行代理 · 2026-10-27 · 第八十七会话 · `轮次 = B87-A10`）
> **独占文件**：`RuleEngine/Core/EffectResolver.cs`（`RuleCore` 的那个 partial 也在里面）
> **补丁**：`账/_parts/B87_A10_oath闸.tsv`（1 行 · 11 列 · 逐字照主表表头 · `状态 = C`）
> **⛔ 没跑 Unity · 没动 git · 没改两张正本 / 账主表 · 没重钉任何 `Pinned*Md5`**

---

## ① `trait 0x82 = 130` 是什么 —— **`DefinedTrait.jam`（干扰）**

**结论**：`0x82(130) = DefinedTrait.jam` —— **不是** `SpellType.Mutation 130`（那是另一条轴上的同一个数字）。

**三重证据**：

| # | 证据 | 内容 |
|---|---|---|
| 1 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/DefinedTrait.cs:13` | `jam = 130,`（同族：`oath = 1275` 在 `:132`） |
| 2 | `d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/CardScript.cs:10245` | ILSpy 版把同一个常量**写成了名字**：`if (this.HasCurrentTrait(DefinedTrait.jam))` —— 与 `.c:133` 的裸 `0x82` 是同一句（两份产物互证） |
| 3 | 我们自己的映射 | `KeywordTable.Jam = "jam"`（`CardDef.cs:7692`）；同一 trait 的另两个读点：`EffectResolver` 的 `AddEffect` 闸②（施放者带 jam ∧ extrinsic ⇒ 不结算）· `ConditionEval.cs:501`（带 jam 的单位不能当主动能力目标） |

**语义（它在 `NeedsToChooseFromPool` 里干什么）**：被 **jam** 的卡**不摆选池面板**（它的能力本来就被压住了）。
⇒ 落地时**必须**带上这一支；⛔ 不能「不管它」—— 那会对被干扰的卡多判一次「需要选池」。

---

## ② 改了什么（全在 `RuleEngine/Core/EffectResolver.cs`，**只追加在类尾**）

> 结构性校验：与改前副本 `diff` = **1 个 hunk · 删除 0 行 · 新增 207 行**（纯追加）·
> 行尾仍全 **CRLF**（`CRLF 17561 / LF 17561 / 裸 CR 0`）⇒ 下游的 `文件:行号` 落点**一条都不会漂**。

| 行（现读） | 新增成员 | 干什么 |
|---|---|---|
| `:17406` | `RuleCore.HasOathTrait(CardDef, out string why)` | 原版 `RawCardScript.HasDefaultTrait(oath)`；数据列 = `cards_engine.json` 的 `cardTraits`（`"oath 2"`），没喂 ⇒ `false` **且 `why` 非空**（⛔ 不当「真的没有」） |
| `:17434` | `RuleCore.NeedsToChooseFromPool(BattleContext, int owner, CardDef, UnitState targetCard = null, UnitState thisCard = null)` | **原版那道闸**，五道逐句照抄（见下） |
| `:17545` | `RuleCore.PlayerChooseOps(BattleContext, int owner, CardDef)` | 带 ctx 的重载：把这道闸套成「**这一次开不开选池面板**」。旧的一参版**一字未动** |

### 五道闸（`.c` 行号 = `d:/2/tools/decomp_full/CardScript__NeedsToChooseFromPool.c`）

| 门 | 原版（行号） | 我们的落法 |
|---|---|---|
| ① | `:21` `choicesDefinition == 0` ⇒ 0 · `:24` `targetsAffected == any(0)` ⇒ 0 | `card.ChoicesTargetsAffected <= 0` ⇒ false（`0` = any；**`-1` = 真值快照里没这张卡**，与「null」同判否） |
| ② | `:28` 英雄（`cardType==10`）且 `card+0x221 != 0` ⇒ 0 | ⚠️ **没建模**（`hasChangedActiveAbility` 的写点没查到）⇒ **不减这一条**，记在 §四 |
| ③ | `:68` 循环里 `HasDefaultTrait(oath)` 为真就跳过整段 · `:78` `abilityLogic[j].targetCriteria.targetsAffected == 0x5a(90)` · `:87` `FilterMethods.CheckIfMeetsCriteria(this, target, target, 那条 criteria)` | 给了 `targetCard` 时：扫 `card.AbilityTargets[i].Trigger == 0 (ThisCardPlayed)` 的 `TargetsAffected[j] == 90`，命中就返回 `TargetCriteriaEval.Matches(...)`（三态；判不了 ⇒ **false + 出声**） |
| ④ | `:108-110` `!HasDefaultTrait(0x4fb)` ⇒ **`return 1`** | 没 oath ⇒ **true**（只要有 choicesDefinition 就问） |
| ⑤ | `:111-131` 回合闸 · `:133-134` `HasCurrentTrait(0x82 jam)` ⇒ `return 0` · `:142-147` `GetManaLeft >= GetDefaultTraitValue(oath)` ⇒ 1 / 0 | `ctx.Active != owner` ⇒ false（原版：`isPlayer` 的卡要 `IsPlayerTurn() && allowPlayerActionsFlag`、AI 的卡要 `!IsPlayerTurn()`）· 带 jam ⇒ false · 否则 `ctx.Players[owner].Energy >= card.GetDefaultTraitValue(oath)` |

### 🔴 本轮**新查实**：oath 卡的「池」到底是什么

`filterSpecificCards` 逐条解引用（`cards_raw.json` 的 `path_id` → 卡名）——**这个池就是同一张卡的两个变体 SO**：

| 卡（oath 变体） | `filterSpecificCards` 解出来 |
|---|---|
| `UM110 Fall Back`（`cd053fb3…` / `fe6fde3d…`） | `cd053fb3…` =`Fall Back`（**无 oath trait**）· `fe6fde3d…` =`Fall Back`（**oath**） |
| `UM116 No Mercy`（`6ea114bc…`） | `7b703582…`（无 oath）· `6ea114bc…`（oath） |
| `Angel's Wrath` / `Catechism of Death` | 同形（两个同名 SO，一个带 oath、一个不带；`maxTargets = 2` = 两个都摆） |

⇒ **「选池面板」= 「要不要付誓约、走 oath 变体」那个选择**；而 `GetManaLeft >= oath 值` 就是
「**付得起才摆这个选项**」。这一条把整段 oath 分支的语义闭合了（A3/R21 当时只记了「付费闸」三个字，没说是**选什么**）。

---

## ③ 验收读数

### 3·1 秒级类型检查（`TMPDIR=/tmp/wf_a10 bash d:/4/Unity/工具/typecheck.sh`）

```
运行时错误数: 3      ← 全部落在别人的在写文件（ChatTab.cs / MainMenuRuntime.cs）
编辑器错误数: 28     ← 全部落在 Editor/*（DraftAssertD3 / NetBattleTest / TraitEffectAssert …）
grep -c EffectResolver 日志 = 0      ⇒ 我的文件 0 错
```
（按 `CLAUDE.md` 13·3：错误**全部集中在不是我负责的文件**上 ⇒ 不是我的问题；隔一轮应自行消失。）

### 3·2 真卡池读数（仓外一次性壳 `d:/4/_tmp_view/a10_20261027/harness`，`CardDatabase.Parse` 真装载 1129 张）

```
pool = 1129
choicesTargetsAffected > 0 : 84      其中 cardTraits 带 oath : 11
oath 卡（池内）: 29 张 · trait 值 vs 卡面 OathCost : 29/29 完全相等
池内 `trigger==0 且 ta==90` 行 = 35 · 卡 = 24     （门③ 不是死路）
```

**四态读数（11 张 oath ∧ choicesDefinition>0 的卡）**：

| id | 卡 | `Energy=20 / Active=自己` | `Energy=0 / Active=自己` | `Energy=20 / Active=对手` |
|---|---|---|---|---|
| UM97 | Suppressor（oath 1, ta 60） | **true** | **false** | **false** |
| UM109 | Adaptive Strategy（oath 1, ta 180） | **true** | **false** | **false** |
| UM110 | Fall Back（oath 2） | **true** | **false** | **false** |
| UM111 | Shall Know No Fear（oath 8） | **true** | **false** | **false** |
| UM112 | Champions of Humanity（oath 4） | **true** | **false** | **false** |
| UM115 | Stormhawk Interception（oath 3） | **true** | **false** | **false** |
| UM116 | No Mercy（oath 4） | **true** | **false** | **false** |
| UM117 | No Respite（oath 3） | **true** | **false** | **false** |
| UM118 | Indomitus Crusade（oath 6） | **true** | **false** | **false** |
| 4de4fcfb… | Angel's Wrath（oath 2） | **true** | **false** | **false** |
| 8482077f… | Catechism of Death（oath 3） | **true** | **false** | **false** |

其余 **73 张**非 oath 的 `choicesDefinition` 卡在三种状态下**恒 true**（门④）—— 与 `.c:108-110` 一致。

### 3·3 `ruleprobe play` 改前 / 改后（`UM116` · `UM110`）

```
bash d:/4/Unity/工具/ruleprobe.sh play UM116     rc=0
  变化逐条（7 条）：能量 P0 20 → 12（-8）· 离场 P1[3] FIX_E1 · 手牌 -UM116 · 弃牌 +UM116
                   · 手牌总价 5→1 · 伤害 4 · 弃牌 P1 +FIX_E1
bash d:/4/Unity/工具/ruleprobe.sh play UM110     rc=0
  变化逐条（6 条）：能量 P0 20 → 17（-3）· 离场 P0[3] FIX_A · 手牌 +FIX_A / -UM110
                   · 弃牌 +UM110 · 手牌总价 2→1
```

🔴 **改前与改后逐字节相同**（两卡各跑两次、数值逐项一致；探针构建成功、无 `error CS`）。
**这不是「改动没生效」，是结构使然**：`ruleprobe play` 走的是**结算层**，从不调 `PlayerChooseOps`
（那是**表现层开不开面板**的 ask 表）。这道闸**天生不改结算数字** —— 它改的是「问不问玩家」。

### 3·4 唯一一张「ask 数会变」的卡（证明闸真的咬合）

壳里对全池跑了 `PlayerChooseOps(card)` vs `PlayerChooseOps(ctx, owner, card)` 的差集：

```
UM97 Suppressor    一参=1  三参=0   choicesTA=60
（其余 1128 张：两者相同）
```
`Suppressor` 那唯一一处 ask 在 `Oath 1:` 正文里 ⇒ 这正是 §四 那条「⛔ 别把闸接到 `AbilityAsks`」的实证。

---

## ④ 没做的 / 为什么

| # | 没做的 | 为什么 | 谁来做 / 判据 |
|---|---|---|---|
| 1 | **接进 `BattleDriver`**（让闸真的影响面板） | `BattleDriver.cs` 不在我的白名单（别的代理在用）。要做的是**一行**：`PlayAsks` 里 `RuleCore.PlayerChooseOps(card)` → `RuleCore.PlayerChooseOps(Ctx, _me, card)` | 下一个有 `BattleDriver.cs` 写权的代理。⚠️ **⛔ 别改 `AbilityAsks`**（见 3·4） |
| 2 | 门② 英雄那格 `hasChangedActiveAbility(+0x221)` | **判据没查到**（`+0x221` 的写点没找到，我们也没有对应字段）⇒ 不减这一条（原版为真时返回 false，我们此刻会继续往下判） | 需要 VA 级反汇编去找 `+0x221` 的写点 |
| 3 | 门⑤ 的 `allowPlayerActionsFlag(+0x244)` | 我们**没有等价位**（那是「等对手/等弹窗时不许操作」的 UI 态）⇒ 用「是不是这一侧的回合」代替，已写进代码注释 | 属表现层闸，接 UI 那一侧时再定 |
| 4 | 门③ 的 `CheckIfMeetsCriteria` 全 36 格 | 走的是现成的 `TargetCriteriaEval.Matches`（三态）；**判不了时返回 false + 出声**（宁可不问，也不凭空多问一次） | 判据缺口在 `TargetCriteriaEval` 自己那边（它的 doc 已记） |
| 5 | **「玩家选不选 oath」的 UI** | 原版用**同一个池面板**让玩家在 plain / oath 两个变体里挑；我们今天的战术卡 oath 是**自动付**（`play UM110` 实测 −3 = 1 卡费 + 2 oath）。要做真选择得先有「变体选择」的 ask 类型 + 面板，跨 `BattleDriver` / UI 两条线 | 新账，建议由调度台立项 |

---

## ⑤ 顺手发现（只报不改）

1. 🔴 **oath 的「池」= 同卡的两个变体 SO**（§二末）。这解释了三件事：`maxTargets` 为什么恒 = 2、`targetsAffected` 为什么是 `choiceResolution(180)`、以及 `oath` 为什么被塞在 `NeedsToChooseFromPool` 里。**A3/R21 都没说到这一层**，建议后续凡是「选池」都先看这条。
2. 🔴 **我们战术卡的 oath 是自动付的**（不是玩家选的）：`play UM116` 把 4+4=8 能全扣了。与原版「先问、付得起才给选项」不一致 ⇒ 已作为 §四 #5 记。
3. ⚠️ **旧账的「oath 36 张」是 1,351 张 SO 的口径**；**池内（`cards_engine.json`）带 oath 默认 trait 的 = 29 张**，且与 `desc` 提到 oath 的 29 张**逐张同一批**（零偏差）。其中 `choicesTargetsAffected>0` 的 **11 张**才是这道闸的作用面。
4. ⚠️ **`oath` trait 值与卡面 `OathCost` 29/29 完全相等** —— 这是一条很好的自校验（`cardTraits` 列与正文解析两条独立来源一致），建议以后加进自检断言（本轮**没加**：`RuleEngineTest.cs` 不在我的白名单）。
5. 🔴 **A3 报告的 `+0x2A0 = activeAbility` 是错的**（应为 `choicesDefinition`）。已在代码注释与账补丁里就地订正；`资料/完全复刻/报告/A3_RuleCore四件.md` 本身**我没改**（它不是我这一批的文件），请调度台合并时一并订正那一行。

---

## 附 · 复现命令

```bash
# 类型检查（独立 TMPDIR，避开别的写手）
TMPDIR=/tmp/wf_a10 bash d:/4/Unity/工具/typecheck.sh

# 结算层读数（改前/改后应逐字节相同 —— 天然如此，见 3·3）
bash d:/4/Unity/工具/ruleprobe.sh play UM116
bash d:/4/Unity/工具/ruleprobe.sh play UM110

# 判据读数（仓外一次性壳，可直接重跑）
cd d:/4/_tmp_view/a10_20261027/harness && TMPDIR=/tmp/wf_a10 dotnet run -v:q

# 账补丁机检（只读）
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py d:/4/Unity/资料/完全复刻/账/_parts/B87_A10_oath闸.tsv
python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py
python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B87_A10_
#  ⇒ A 0 · 通过 0 · 漂移 0 · 失败 0（本行状态 C，机检只查 A 行）
#  ⇒ 无跨补丁重复键（2 份补丁）· dry 合并：更新 1 · 新增 0 · 无变化 0
```
