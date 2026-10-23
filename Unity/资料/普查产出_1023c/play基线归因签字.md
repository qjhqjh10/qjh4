# `play` 基线 476 行 · 逐行归因签字（2026-10-23 · 执行代理 `T4`）

> 对象：基线 `Unity/工具/ruleprobe/基线/out_play_baseline.txt`（2026-10-21 20:52）vs **当前工作树**现跑。
> 前置：`资料/普查产出_1023b/play基线476行归因.md`（只做到「归属到会话」，明确留了「425 行逐条判对错」这一件）。
> ⛔ 本报告**没有重钉基线**、没有改引擎源码、没有跑 Unity。

---

## 〇 一句话结论

**⛔ 不可重钉。** 476 行**全部**有了落点（8 主档 + 467 电池），其中：

| 分类 | 行数 | 判 |
|---|---|---|
| 反击取哪一档攻击力（会话 63） | **416** | ✅ **改对了**（反编译判据见 §三·1） |
| `B-31` 攻击拒绝原因拆码（本轮） | **43** | ✅ 改对了（原版确有四档不同 Tips） |
| `SAU9` 毁灭者目标规则（`W64 B-15`） | 1 | ✅ 改对了（`IsValidAttackTarget.c:206-264`） |
| `GOF87` Mob `abilityContext`（本轮） | 1 | ✅ 改对了（判定点搬回入队那一刻）。⚠️ 同族的 `GOF9/12/19` **已算在**那 416 行里（它们同时被反击与 Mob 两条机制改过）；`GOF84` 已**回到基线值**、不在当前差异表里 |
| `GOF42`（`B-17` targetsAffected） | 1（主档） | ✅ 改对了（卡面只写 Warlord） |
| `DA28`（手牌总价起点 6→7） | 1（主档） | ✅ 方向对（DA28 当时不在场，不该有抽牌折扣） |
| `BL64`（`ErrNoTargetAvailable`） | 1（主档） | 🟡 **不是缺陷**，但**夹具覆盖不到**（见 §五·2） |
| 🔴 **`If the target dies` 五张主档** | **5（主档）** | 🔴 **真回归**（见 §二 · R1） |
| 🔴 **`SW5` 狂暴正文丢失** | **1** | 🔴 **真回归**（见 §二 · R2） |
| `GOF67 · GOF87 · SW8 · SW9 · SW11 · TL16` | 6 | ⚠️ **未查清**（同族于 R2，需按同一机制复查） |

**合计 476 = 8 主档 + 467 电池。** 每一行的落点见 §一。

**两条真回归的修复不在本代理白名单内（`RuleEngine/**` 禁止触碰），只写报告。**

---

## 一 逐行落点（精确分区，机器可复算）

口径：`play all pf` 的 TSV（主档 19 列 / 电池 14 列），按 `id` 对齐后逐列比。
当前读数：`play check` ⇒ ①-a **8 张卡**、①-b **467 张卡**、rc=1（`EC33` 修好后已从差异表消失）。

### 1·1 电池 467 行

| 机制 | 行数 | 集合怎么来 | 判 |
|---|---|---|---|
| **反击取哪一档攻击力**（`counterAtk = target.Attack` → `FieldAttack(ctx, tgtP, target, ranged)`） | **416** | **实验**：在工作树影子探针里只把这一行改回 `target.Attack`，重跑 `play all pf` ⇒ 与基线差 **416** 行；它与「基线→当前」的 467 行交集**恰好 416** | ✅ |
| **`B-31` 拆码**（`atkM/atkR 7→21` / `friendDies 10→23·24`） | **43** | 归一化 `21→7 / 22→7 / 23..27→10` 后 `steps` 逐字节相同，且**其余 13 列一个字没变** | ✅ |
| **`SAU9 Ophydian Destroyer`** | 1 | `atkR2 0→26`（`ErrCantAttackDestroyer`）；原版 `IsValidAttackTarget.c:206-264` | ✅ |
| **Mob `abilityContext`**（`GOF9/12/19`） | — | 它们的行已经**包含在**上面那 416 里（两条机制同时改过），不再单列 | ✅ |
| **`GOF87` Mob `abilityContext`** | 1 | 与 `引擎线_执行报告.md` §2.2 的 5 张同名；`GOF84` 已回到基线值、当前不再是差异行 | ✅ |
| `GOF67 · GOF87 · SW8 · SW9 · SW11 · TL16` | 6 | 只在会话 62（`a514fc53`）变、且**不被反击实验覆盖** | ⚠️ 未查清 |

### 1·2 主档 8 行

| id | 卡 | 变化 | 机制 | 判 |
|---|---|---|---|---|
| `30007162…` | Lord of the Storm | `mech 3→1` · `handCostDelta -2→0` · `changesFull` | `If the target dies` 结构化覆盖 | 🔴 R1 |
| `3f38d2e2…` | Rites of Possession | `other 2→3`，多一条「落场 BL9」 | 同上 | 🔴 R1 |
| `AM39` | Born Soldier | `mech 3→2` · `other 2→4`，多「抽一张」 | 同上 | 🔴 R1 |
| `GSC40` | Backstab | `other 2→4`，多「对手手牌 +1」 | 同上 | 🔴 R1 |
| `UM34` | Point-Blank Shot | `mech 3→2` · `other 2→5`，多「抽一张」 | 同上 | 🔴 R1 |
| `DA28` | Company Master | 只 `changesFull`：手牌总价起点 `6→0` → `7→1`（Δ 仍 -6） | 会话 62 | ✅ 方向对 |
| `GOF42` | Da Irongob | `other 4→1`：只治督军 | `B-17` targetsAffected=120 friendlyHero | ✅ |
| `BL64` | Murderous Desires | `playRc 0→18`（`ErrNoTargetAvailable`）、整行塌成 0 | `B-16` 目标筛（`cond=341 ex=1`） | 🟡 数据上正确·夹具盲 |

---

## 二 🔴 两条真回归

### R1 「`If the target dies`」五张主档 —— 条件被结构化层**无条件判成立**

| 项 | 内容 |
|---|---|
| 现象 | `play AM39`：FIX_E1 `6→5`（**活着**），却抽了牌（`✋ 手牌 P0 +「filler28」；🂠 牌库 P0 -「filler28」`）。`UM34` 同形；`GSC40` 同形（对手手牌 +1）；`3f38d2e2` 同形（落场 BL9）；`30007162` 同形（少了「手牌 -卡」那两条）。**四张都是「目标没死也兑现支线」** |
| 卡面 | `AM39` `Deal 1 damage. **If target dies, draw a card**`；`UM34` 同；`GSC40` `If the target dies, create a random Sabotage in the enemy hand`；`3f38d2e2` `If the target dies, deploy a Possessed Marine`；`30007162` `If the target dies, create a Lord of the Storm in your hand` |
| 根因（文件:行号） | `Core/EffectResolver.cs:10066-10073` 把 `targetdies` 映到 `UnitConditions 220 / exclude=0`；`:10268` `ConditionHolds` **先**走 `TryStructuredCondition`；它查这张卡的 `unitConditions` 行——**这 5 张卡数据里一条都没有**（`cards_engine.json` 的 `AM39` 连 `unitConditions` 键都没有）⇒ 落到 `:10177-10206` 的「原版数据里没有这一格条件 ⇒ **没有条件**」分支，`:10204` **`holds = true`** 直接返回 ⇒ 正文口径 `:10293-10299`（`t == null \|\| !t.IsAlive`，本来是对的）**永远轮不到** |
| 为什么这一档特别 | 同一文件 `:1835` 自己写着：`220 + ex=0` 是**结算后的判定**（卡面「If the target dies」），**不是选目标的筛**，「本仓由 `ConditionHolds` 的 `targetdies` 判」。把 `targetdies` 塞进「目标筛选」映射表，等于用「**没有筛选行**」去证明「**没有条件**」——这两件事不是一回事 |
| 实验（决定性） | 影子探针里把 `{ "targetdies", (220, 0) }` 这一项去掉（其余不动）⇒ `play all pf` 后，**这 5 张主档逐列回到 10-21 基线**（脚本判定 `vs61 equal: True` ×5），**全池其他主档 0 张变** |
| 引入提交 | `543a2836`（第六十四会话「条件结构化」） |
| 建议修法（供调度台裁） | ① `TryStructuredCondition` 对 **post-check 类**（`targetdies` / `targetsurvives`）在「卡上无对应行」时**返回 false（退回正文）**，而不是 `holds=true`；或 ② 把这两项从 `ConditionKindToUnitCondition` 移出，只让 `TargetCondition`（`ResolveTargets`）那条路吃结构化数据 |

### R2 `SW5 Hunting Wolf` —— 狂暴正文在**卡片已洗回牌库之后**才结算 ⇒ 攻击丢失

| 项 | 内容 |
|---|---|
| 现象 | 电池段：61 版 `FIX_E3 1/0/0/6 → 1/0/0/3` + 两条 `☠ 伤害累计 P1`（净差 3）；**62 起这三条消失**（净差 1，只剩 `🂠 牌库 P0 +「SW5」`），一直到当前工作树 |
| 卡面 | `Ferocity: Attack a random non Flying enemy troop`（该打就得打） |
| 根因 | `Core/RuleCore.cs:10277` `FireTriggerAt(...)` 现在**只入队**；紧接着 `:10306` **同步**执行「洗回牌库」（`BoardSlots.RemoveAt` + `Deck.Add`）⇒ 泵点出队时载体**已不在棋盘上**，攻击那条 op 打不出来（`alt(ferocity)=0` 仍报成功，所以 `steps` 一模一样 —— **静默**） |
| 原版判据 | `CardScript__UsedActiveAbility.c:58-66`：先 `BattleManager__BroadcastUnitFerocity`（监听器 → `PlayAbility` → `AddAutoActionToQueue` 入队），**再** `BattleManager__AddRecallToDeck`；而 `BattleManager__AddRecallToDeck.c:101` 自己也是 `BattleManager__AddAutoActionToQueue` ⇒ **两条都在队列里、正文那条先进 ⇒ 先结算正文，洗回在后**。我们的「同步洗回」把次序**倒过来**了 |
| 实验（决定性） | 影子探针把洗回改成入队（`DelayedInstant` 道，正文在 `Instant` 道先结算）⇒ `play "Hunting Wolf"` 的 `FIX_E3 6→3` 与两条伤害累计**全部回来** |
| 引入提交 | `a514fc53`（第六十二会话「触发入队」接线批）。⚠️ 洗回那段代码本身更早，**是入队把次序反过来的** |
| 建议修法 | 把 `RuleCore.cs:10294-10323` 那段洗回包成一次入队（原版 `AddRecallToDeck` 的形状）；同族的「触发后同步改局面」都要按这条口径复查（⛔ 不建议用 `DelayedInstant` 硬编码道，应按原版 action 类型定道） |
| ⚠️ 提醒 | 该实验在全池改了 **420 行电池读数**（洗回时刻一挪，后面每步的快照都变）。**修法必须由引擎线带断言落地**，不能直接拿这个实验当补丁 |

---

## 三 已查清且判「方向对」的部分（不是自证，判据在反编译）

### 3·1 反击取哪一档攻击力（416 行）

- 原版 `BattleManager._ResolveAttack_d__438__MoveNext.c:1114-1117`：`if (*(char *)(ctx+0x138) != '\0') { GetCombatDamage(*(ctx+0x130), *(ctx+0x58), 0) }`
  —— `+0x130` = 守方（同一函数 `:999` 主伤害打的就是它）、`+0x58` = **本次打法**（`:1119` 用它判 `==2` 走远程动画）。
- `EntityScript__GetCombatDamage.c:19-24`：`param_2 == 2 ⇒ get_CurrentRangeAttack()`，否则 `get_CurrentMeleeAttack()`。
- ⇒ 远程攻击引发反击时，取**守方的远程攻击力**。我们原来写死 `target.Attack`（当前近战）⇒ 夹具守方 `FIX_E* 2/0/0/6`（远程 0）本该**反击 0**，旧版却按近战 2 打 ⇒ 被测单位多掉血、少活一回合 ⇒ 后面 `atkM2` / `kill=@2` / 血量数字全跟着变。**这 416 行就是这么来的，方向对。**

### 3·2 `B-31` 拆码（43 行）

`BattleManager__IsValidAttackTarget.c` 里**四个不同的拒绝理由**（`:134` 隐身 / `:149-174` 护卫 / `:177-181` 近战打飞行 / `:206-264` 毁灭者）分别引用四条不同的 Tips 字面量（`DAT_1842896a8` / `DAT_1842894c0` / `DAT_1842895b8` / `DAT_184289988`，见该文件 `:37-46` 的初始化与 `:173/:255/:316/:329` 的使用点）。旧实现把它们塌成 `7/10` 两个码 ⇒ 拆开是补，不是改结算（归一化后 13 列逐字节相同）。

### 3·3 `SAU9` / `GOF42` / `Mob` / `DA28`

- `SAU9`：毁灭者「另有这一下打得死的目标 ⇒ 不许打当前目标」，`IsValidAttackTarget.c:206-264`；`W64 B-15` 照抄。
- `GOF42`：卡面 `Your **Warlord** gains Concussive … and heals 5` ⇒ 只治督军（`28→30`，上限 30）；旧版顺手治了 `FIX_A/B/C` 是**打宽了**。
- `Mob` 5 张：原版 `RawCardScript__TriggerAbilities.c:108,207` 同步调 `CardAbility__CanTriggerAbility`→`IsRightContext`（`CardAbility__IsRightContext.c:20-30`：`Play(0)` 要求 `IsInPlay`）⇒ **闸在入队之前**；我们现在的判定点与它一致，Mob 正文该响。
- `DA28`：主档只差「手牌总价起点」6→7（Δ 不变）。手牌 = [DA28(6), filler29(1)]；61 版给 filler29 打了 -1 折扣（打完剩 0），现版不打（剩 1）。DA28 第 2 回合才打出、此前不在场 ⇒ **现版不给折扣是对的**（⚠️ 只核到卡面语义，没核到反编译的折扣施加点）。

---

## 四 重钉前置条件（按顺序）

1. **R1 修掉**（5 张主档必须回到「目标活着就不兑现」）。
   - 验收判据：`dotnet <probe> play AM39` ⇒ 打出去之后**手牌不含 filler28**、`牌库` 不变；`play 3f38d2e2…` ⇒ 无 `✚ 落场 BL9`。
2. **R2 修掉**（狂暴正文要在洗回之前结算）。
   - 验收判据：`play "Hunting Wolf"` 电池段出现 `FIX_E3 …→…/3` 与两条伤害累计。
3. **6 行未查清**（`GOF67/GOF87/SW8/SW9/SW11/TL16`）按 R2 同族复查 —— 它们全是「会话 62 触发入队」时变的，且方向不一（多了／少了都有）。
4. 修完重跑两次 `play all pf` 比字节（老规矩），再重钉 + 同步 `PlayProbe.PinnedPlayMd5`。
   ⚠️ **`PinnedPoolMd5` 是另一颗钉子，别顺手改**。

---

## 五 附：两个容易误判的点（都实测过，别再翻案）

### 5·1 `BL64` 的「没有合法目标」**不是缺陷**

- 卡面 `Make a damaged friendly unit attack by itself`；夹具确实有受伤友军（`FIX_A 2/0/0/2`，上限 4）。
- 但原版数据 `cards_engine.json` 的 `BL64.unitConditions` 有两行：`{cond:40 damaged, role:cardtarget}` 与 **`{cond:341 canUseDuty, ex:1, role:target}`**；原始 SO 同形（`abilityLogic[0].targetCriteria = {targetsAffected:30, conditionsFilter:341, excludeCondition:1}`）。
- `341` 的原版实现 `FilterMethods__MeetsConditionCriteria.c:170-174` = `card+0x4c == 0`；`CardScript__ResolveActiveAbilityPlayed.c:30` 把它置 1 ⇒ **`341` 真 = 「还没用过主动能力」**（`ex=1` ⇒ 只要**已用过**的）。全池另外两张用 `341 ex=1` 的是 **`Cadian Honour` / `Press the Attack`**，卡面都写「**reload their Duty abilities**」——完全自洽。我们的 `ConditionEval.cs:454-456` `return !u.DutyUsed` 与之一致。
- ⇒ 夹具的友军**从没用过 Duty** ⇒ 照原版数据**本来就该没有合法目标**。旧读数（能打）是没筛目标。
- ⚠️ **代价**：夹具从此刻起**看不见这张卡的正文**（`playRc=18`）。建议在电池里补一步「先让一个友军用掉 Duty」把覆盖补回来 —— 那是工具线的活，不是引擎缺陷。

### 5·2 `3f38d2e2` 的「目标没死却落场 BL9」**是 R1 的表现**，不是另一条缺陷

上一份归因文档 §六 D 记的疑点（「同一行 `FIX_E1 6→2` 没死，却部署了 BL9」）就是 R1：条件恒真。R1 修完它自然消失，**不需要单独立账**。

---

## 六 顺手发现（不在本任务范围，交调度台分流）

1. **`RuleCore.cs:10294-10323` 的「同步洗回」不是孤例**：凡「触发 → 入队」之后紧跟**同步**改局面/移牌/结算的写法，都会重现 R2 的次序倒置。建议引擎线 `grep` 一遍 `FireTriggerAt` 之后紧接着动 `Board`/`Deck` 的调用点。
2. **`EffectResolver.cs:10287`** 还有一处 `t == null ⇒ holds = true`（注释写着「同 `targetdies`：没有目标 ⇒ 当『死了』」）—— 同一族「用缺失当成立」，R1 修的时候一并看。
3. **`SW22`**：`(可查) 3/5/0/3 → 3/5/0/3 {huntmark2}` —— 电池里同一单位**叠了两层猎杀标记**（`huntmark2`）。是否符合原版（猎杀标记是否可叠）**没查**，只记现象。
4. `play check` 的 **①-c**（电池覆盖不到的那一档）当前仍有 77 张「净差 = 0」，R2 修完后其中一部分可能会转正 —— 建议修完顺带看 `①-c` 的名单变化，那是这次修复的免费验证。

---

## 附 复现命令（都在临时目录，不碰仓库）

```bash
# 当前读数
bash d:/4/Unity/工具/ruleprobe.sh play check            # rc=1（8 主档 + 467 电池）
bash d:/4/Unity/工具/ruleprobe.sh check                 # rc=0（解析层，与本题无关）

# 影子探针（工作树引擎 + 工作树工具，改一个变量就能隔离一条机制）
cp -r d:/4/Unity/MyGame/Assets/RuleEngine /tmp/t4/wtshadow/Unity/MyGame/Assets/
cp d:/4/Unity/工具/ruleprobe/*.cs d:/4/Unity/工具/ruleprobe/ruleprobe.csproj /tmp/t4/wtshadow/Unity/工具/ruleprobe/
cd /tmp/t4/wtshadow/Unity/工具/ruleprobe && dotnet build -c Release && \
  dotnet bin/Release/net8.0/wfprobe.dll play all pf /tmp/t4/pf_wt.txt

# 已跑过的三组实验
#  ① 反击：RuleCore.cs 的 `int counterAtk = FieldAttack(...)` → `target.Attack`  ⇒ 416 行
#  ② targetdies：EffectResolver.cs 映射表去掉 `{ "targetdies", (220, 0) }` ⇒ 5 主档逐列回到基线
#  ③ 狂暴洗回：RuleCore.cs:10306 那段改成 EnqueueAbility(DelayedInstant, …) ⇒ SW5 伤害回来
```
