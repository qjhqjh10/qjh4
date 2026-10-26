# B8_E · `SimpleAI` / `AiScoring` / `PlayerState` / `BattleContext` 四族 B→A 报告

> 执行代理 **E**（代号 `B8_E`）· 2026-10-26
> 白名单：`Data/SimpleAI.cs` · `Data/AiScoring.cs` · `Core/PlayerState.cs` · `Core/BattleContext.cs`
> 账补丁：`资料/完全复刻/账/_parts/B8_E.tsv`（10 行 = 1 表头 + 9 条；**A 6 条 · B 3 条（重读方法体、状态不变）**）
> 生成器：`d:/4/_tmp_view/b8e_write_patch.py`（可复跑；⛔ 别手工改补丁）

---

## ① 结论

| 项 | 值 |
|---|---|
| **真升 A** | **6 条**（目标 ≥10，**未达标 —— 原因见 §四·A，全部是「读数口」结构性缺，不是没干活**） |
| 保持 B（本轮把方法体读全 / 补「还差什么」） | 3 条 |
| typecheck | **运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_b8e bash 工具/typecheck.sh`） |
| `ruleprobe check` | **rc=0**（全池 1121 张 · 解析差异 0 行 · 哨兵合计失败 0 条） |
| `ruleprobe play check` | **rc=0**（主档 1128 行 + 电池 586 行，逐卡差异 0；基线指纹 `a93ffb8f…` 与钉住一致，**未重钉**） |
| 行号漂移 | **零**（`SimpleAI.cs` 只在类尾追加；`PlayerState.cs` 只改 226 段以后；`AiScoring.cs` 净行数 0 —— `git diff --numstat` = `3/3`） |

### 升 A 的 6 条

| # | 原版方法 | 我们的落点 | 观测量（探针） |
|---|---|---|---|
| 1 | `CardScript.IsInCemetery()` | `PlayerState.cs:310-313`（新增） | `play` 的「弃」清单 |
| 2 | `CardScript.IsInDeck()` | `PlayerState.cs:281-284`（新增） | `play` 的「🂠 牌库」清单 |
| 3 | `CardScript.IsInHand()` | `PlayerState.cs:295-298`（新增） | `play` 的「✋ 手牌」清单 |
| 4 | `BattleManager.GetSlotsLeftInBattlefield(bool)` | `PlayerState.cs:245-249`（转发）＋ `BoardSlots.cs:87`（唯一判据） | `play` 棋盘行的可部署空格数 |
| 5 | `BattleManager.MightAttackBuffedCard(card, buff)` | `SimpleAI.cs:2185-2192`（新增）＋ `AiScoring.cs:547-548`（接入闸） | `ai <卡>` 的 `option=5 … mightAct=真M/假N ⇒ X` |
| 6 | `RawCardScript.UsesQuestPoints()` | `PlayerState.cs:328-332`（新增） | `play` 夹具的督军阵营 ＋ `任务点` 列 |

---

## ② 逐条证据（判据 `文件:行号` ＋ 探针原始输出）

### 2·1 `CardScript.IsInCemetery()` → A
- **判据（G0）**：🥇 `decomp_full/CardScript__IsInCemetery.c:1-7`，全文 7 行、整支只有一句：
  `return *(int *)(param_1 + 0x228) == 6;`
- **落点（G1）**：`PlayerState.cs:310-313`（新增 `IsInCemetery(CardInstance)`），逐句读完。
- **机制差异（G2）**：原版是**单值状态机 int（+0x228）**；我们是**三张区域表之一的成员资格**（`Discard`）。
  `RuleCore.TakeFromGraveyard` 把 `Discard` 与 `DeadUnits` **一起**写（只此一处）⇒ 静息态一一对应。
  原版另有一个 `+0x24b`「正在从墓地取出」的 UI 闸（`CardScript__RemoveCardFromCemetery.c:1-8`），我们这一侧**无对应物** ⇒ 那条另记 B。
- **可观察量（G3/G4/G5）**：
  ```
  play DA49 前：P0 能=20 手=[DA49,filler29] 手牌总价=4 库=29 弃=(空) 伤害=0 出兵=3 …
  play DA49 后：P0 能=17 手=[filler27,filler28,filler29] 手牌总价=3 库=27 弃=[DA49] 伤害=0 出兵=3 …
  ```
  期望（原版）：进墓地后 `+0x228==6` ⇒ 真。实测：`✋ 手牌 P0 -「DA49」×1` + `🗑 弃牌 P0 +「DA49」×1` 同步出现 ⇒ 真。
  **边界①**（对照单位卡）：`play BL4` 前后 `弃=(空)` 两次都空 ⇒ 单位落场不进墓地、`IsInCemetery` 恒假。
  **边界②**：同一份实例同一时刻只落在一张表里（手牌 -1 与弃牌 +1 同时出现）。

### 2·2 `CardScript.IsInDeck()` → A
- **判据**：🥇 `decomp_full/CardScript__IsInDeck.c:1-13`：`iVar1 = +0x228; ==0 ⇒ 真; 否则 return (iVar1 == 0x10)`。
- **落点**：`PlayerState.cs:281-284`。
- **G2**：原版把 **`0x10`（正在从牌库抽出）也算在牌库**；我们抽牌是**一次搬移**（`Deck` → `Hand`），**无该中间态** ⇒
  抽出那一刻两侧短暂不一致，但**同步结算里观察不到**（引擎没有帧间状态）。
- **G3/G4/G5**（`play DA49` = `Draw two cards`）：
  ```
  前：… 库=29 弃=(空) …
  后：… 库=27 弃=[DA49] …
  🂠 牌库 P0 -「filler27」×1
  ✋ 手牌 P0 +「filler27」×1
  ```
  ⇒ 同一份实例 `IsInDeck` 真→假、`IsInHand` 假→真。**边界**：`play BL4` / `play EC39` 前后 `库=29` 两次都 29（打出的那张不回牌库）。

### 2·3 `CardScript.IsInHand()` → A
- **判据**：🥇 `decomp_full/CardScript__IsInHand.c:1-11`：`+0x228 ∈ {1,4,8,9,10,12}` ⇒ 真（六档）。
- **落点**：`PlayerState.cs:295-298`。
- **G2**：原版把 **8/9/10/12（展示中 / 离场回手 / 飞向战场）** 那几档过渡态也算「在手」；我们出牌即从 `Hand` 摘除 ⇒ 过渡期为假。静息态一致。
- **G3/G4/G5**：
  ```
  play EC39  前：手=[EC39,filler29]          → 后：手=[filler29]
  play DA49  前：手=[DA49,filler29]          → 后：手=[filler27,filler28,filler29]   （抽到的两张进来）
  play BL4   前：手=[BL4,filler29]           → 后：手=[filler29]                     （落场即离手）
  ```

### 2·4 `BattleManager.GetSlotsLeftInBattlefield(bool)` → A
- **判据**：🥇 `decomp_full/BattleManager__GetSlotsLeftInBattlefield.c:1-19` —— 全文只做「按方取那个 `MinionManager`
  （`+0xe8`/`+0xe0`）+ 空检 + 转调 `MinionManager.AvailableSlotsCount`」，**自己不算任何一个数**；
  🥇 `decomp_full/MinionManager__AvailableSlotsCount.c:26-27` —— 唯一算式
  `(左容量 − 左人数) + (右容量 − 右人数)`（容量 = `left/rightSlotPos.Length`，人数 = `+0x60/+0x68` 两个 `List.Count`）。
- **落点**：`PlayerState.cs:245-249`（`FreeSlots`，**本轮改成转发**）＋ `BoardSlots.cs:87`（`RoomOnSide`，唯一判据）。
  `HasFreeSlot()`（`:253`）同步转发到 `BoardSlots.IsAvailableSlot(this, 1)`。
- **G2 机制差异**：原版两侧是**无洞 List**、人数 = `List.Count`；我们 `BoardSlots.CountOnSide` 数的正是「非空」
  ⇒ **逐字同义**（不是「连续无洞时才等价」）。🔴 **本轮把重复实现收口**：`PlayerState.FreeSlots` 原来是
  **第二份**同式实现（自己数「可部署格且为空」），与 `BoardSlots.RoomOnSide` 各写一遍 —— 按工程铁律
  「两处写同一条规则 = 迟早不一致」改成转发。
- **G3/G4/G5**：原版期望 `play BL4` 前 = `(4−1)+(4−2) = 5`（左 4 格里 FIX_A 占 1、右 4 格里 FIX_B/FIX_C 占 2）；
  ```
  前：P0 棋盘 [.][.][.][FIX_A 2/0/0/2][WARLORD 2/0/0/28][FIX_B 3/0/0/3][FIX_C 1/0/0/2][.][.]
  ```
  ⇒ 可部署空格 = 0/1/2 + 7/8 = **5** ✓。打出后 BL4 落槽 2：非空 4 ⇒ 原版期望 `(4−2)+(4−2) = 4`，实测空格 **4** ✓。
  **边界**：督军格恒 `WARLORD` 但**不计**占用（容量只算两侧 4+4）。

### 2·5 `BattleManager.MightAttackBuffedCard(CardScript, CardEffect)` → A
- **判据**：🥇 `decomp_full/BattleManager__MightAttackBuffedCard.c:1-33`（全文 33 行，逐段读完）：
  ```
  if (card) { if (card.mightAct) {                                  // .c:10
      v = CurrentMeleeAttack + buff.meleeAttackChange(+0x48);        // .c:12-14
      if (v < 1) { v = CurrentRangeAttack + buff.rangeAttackChange(+0x50);   // .c:17-19
                   if (v < 1) return false; }                        // .c:20
      if (card.state(+0x228) == 2) return true; } }                  // .c:22-24
  return false;
  ```
  调用点：`AI__GetPotentialBuffedAttackActions.c:48`（逐动作一次）+ `AI__ScoreFromBestAttack.c:21`（只调一次**带 buff** 的列表）。
- **落点**：`SimpleAI.cs:2185-2192`（新增 `MightAttackBuffedCard(ctx, t, meleeDelta, rangedDelta)`）；
  闸的接入点 = `AiScoring.cs:547-548`（`BestAttackScore`，原来只判 `u.Exhausted || u.IsStunned`）。
- **G2 机制差异**：原版第三条（`state == 2`）由调用点恒满足（`t.Unit` 来自棋盘枚举）；前两条逐字抄。
  `mightAct` 转发引擎唯一判据 `AiTargets.MightAct` → `ConditionEval.MeetsUnit(…, 20, …)`
  （同源 `CardScript__get_mightAct.c:7-27`）。⚠️ 我们**另外保留**旧口径 `Exhausted || IsStunned`（= 我们侧 `canAct`）
  作为**附加合取项（只收紧、不放宽）** —— 理由见 §四·①。
- **G3**：`bash 工具/ruleprobe.sh ai "Path of the Warrior"`（buff = `meleeAttackChange 1`）：
  ```
  cardTarget[0] option=5 base=0/0 mult=1/-1 targets=1 mightAct=真1/假0 ⇒ 0
  ```
  期望（原版）：这条 buff 不改变可攻击集合 ⇒ `withBuff == noBuff` ⇒ option 5 贡献 **0**。实测 **0** ✓。
- **G4 差分实验（灭自证）**：把 `Exhausted || IsStunned` 这一句**临时去掉**再跑同一行 ⇒ **`⇒ -1.2`**（差 1.2 = `withBuff` 值）
  ⇒ 证明这段闸是**活代码、并非空转**；同时证明「`canAct` 与 `mightAct` 在「已行动但未攻击」的单位上结论相反」。
  反向读数：`ai BL54`（+4 近战）改前/改后都是 `⇒ 0`。
- **G5 边界**：① `ai SOR60`（option 5 但 `targets=0`）⇒ `mightAct=真0/假0`，闸没被触发，`⇒ 无目标=-1 · 有敌方目标=0` 不变；
  ② `ai BL54`（`meleeAttackChange 4`）⇒ 同一行 `mightAct=真1/假0`，值仍 0。

### 2·6 `RawCardScript.UsesQuestPoints()` → A
- **判据**：🥇 `decomp_full/RawCardScript__UsesQuestPoints.c:5-7`（全文 7 行）：`return *(int *)(param_1 + 0x2c) == 0x6e;`
  （`0x6e = 110 = DarkAngels`）。消费者 `PlayerManager__ResetMana.c:135` 据此 `ManaManager.ToggleQuestPoints`。
- **落点**：`PlayerState.cs:328-332`（新增 `UsesQuestPoints()`，读 `Warlord.Card.Faction`）。
- **G2**：原版读 `RawCardScript.army` **枚举（int 110）**，我们读 `CardDef.Faction` **字符串 `"DarkAngels"`**
  （与 `EffectResolver.cs:12899` 同一串，判据互不冲突）；判定对象两侧同为**督军卡**。
  表现层的槽位切换（`ManaTypeHolder`）归 R15 那批，本行只钉「阵营 → 布尔」这一跳。
- **G3/G4/G5**：夹具的 P0 督军阵营 = **被测卡的 faction**（`PlayProbe.cs:453-457` 的注释：`PlayProbe.Deck(p0Fac, …)`
  造 `FIXWarlord_<faction>`）⇒ `印的数值 … faction=X` 那一行就是督军阵营的读数。
  ```
  play DA58：印的数值 … faction=DarkAngels  → 任务点 P0 0 → 2（🔮 任务点 P0 0 → 2）   ⇒ UsesQuestPoints = 真
  play BL4 ：印的数值 … faction=BlackLegion → 任务点 0 / 0（前后都 0）                ⇒ UsesQuestPoints = 假
  play DA49：faction=DarkAngels             → 任务点 0 → 2                          ⇒ 真（同侧同结论）
  ```
  期望（原版）：`cmp [督军+0x2c],0x6e` 命中 ⇒ 真（切任务点槽）、不命中 ⇒ 假。

---

## ③ 改了哪些文件哪些行（新增行数）

| 文件 | 改动 | `git diff --numstat` |
|---|---|---|
| `MyGame/Assets/RuleEngine/Core/PlayerState.cs` | ① `FreeSlots()`/`HasFreeSlot()` 从「第二份同式实现」改成转发到 `BoardSlots.RoomOnSide` / `IsAvailableSlot`（`:245-249` / `:253-256`，判据 doc 写全）② 类尾追加 4 个方法：`IsInDeck`(:281) · `IsInHand`(:295) · `IsInCemetery`(:310) · `UsesQuestPoints`(:328) | **+96 / −9** |
| `MyGame/Assets/RuleEngine/Data/SimpleAI.cs` | 类尾追加 `MightAttackBuffedCard`（`:2185-2192`，含 33 行判据的完整 trans脚本 + 三条机制差异 + 观测量读法） | **+74 / −0** |
| `MyGame/Assets/RuleEngine/Data/AiScoring.cs` | 只改 `BestAttackScore` 的**闸**（`:546-548`）：保留 `Exhausted || IsStunned`，**新增**带 buff 档的 `MightAttackBuffedCard` 合取；另把该 doc 的两行合并成一行以保持净行数 0 | **+3 / −3** |
| `BattleContext.cs` | **未改**（本轮结论：该族没有能过 G3 的候选，见 §四） | 0 |

- 行尾：三份文件均 **纯 LF**（`crlf=0`），无裸 CR；`SimpleAI.cs` 只在类尾追加 ⇒ 账里 `:2105` 等落点**未漂移**；
  `PlayerState.cs` 只动 `:226` 之后 ⇒ `:19/:20/:21/:74/:75/:106/:147/:172/:206-223` **全部未动**。
- `_tmp_view/b8e_write_patch.py` = 补丁生成器（复跑即可重写补丁）。

---

## ④ 没查清的部分

### A）为什么只到 6 条 —— 4 类「结构性卡点」（**都不是没干活，是没有读数口 / 判据在远端**）

| 卡点 | 卡住的行 | 具体缺什么 |
|---|---|---|
| **① AI 动作表没有读数口** | `BattleManager.GetAvailableActions`（本轮把 **370 行读完**，五段逐段对上） | `play all dump` **结构上看不见 AI**；`ai` 模式只打**打分**、不打**动作表**。⇒ 需要 `ruleprobe ai actions <卡id>`（或给 `play` 加一列「枚举出的动作」）才能给 G3 |
| **② `PlayOrder` 不可读** | `MinionManager.GetMinionList` · `BattleManager.SetUnitOrderInPlay` | `PlayOrder` 不在 `B2A 口径 §三` 的可观察量清单里，两个探针都不打印它。⇒ 需要给棋盘/单位加一列 `PlayOrder` |
| **③ 判据侧本身读不全** | `AI.ScoreFromHandBuff`（`.c` 把返回值丢了，两份产物逐字节相同）· `MinionManager.GetMinionList` 的 keySelector（`.c:60-71` 是**缓存委托**，读不出 key 是哪个字段） | 需要 VA 级复读（`RVA 0x93B7A0`）／`out_cpp2il_dev_ilspy/MinionManager.cs` 交叉 |
| **④ 判据在远端（P3）** | `MatchData.GetBotConfigMaxSkips / MinSkips / SkipChance` | 三个值都在 `AIBotsConfig : IConfigData`（`Everguild.LiveOps.Config`）= **远端 Title Data**，本地 84 个 bundle 里 0 个 `BotConfig` 资产（`find -iname "*botconfig*"` 空手）⇒ **原版值本身拿不到**，G3 永远差「原版侧期望值」。按 `铁律 11 例外①`（判据是空的）如实保持 B |

### B）本轮明确「还没核」的几段
- `GetAvailableActions` 的 `+0x244 / +0x246` 两个「本回合手牌闸」字节 —— **写点没核**（我们这侧由 `RuleCore.CanPlayCard` 承担）。
- `SetUnitOrderInPlay` 的 `+0x3a8` 五 dword 里哪几个是 `fakeValue/inited`（只影响反作弊，不影响值）。
- `MinionManager.GetMinionList` 的排序键 == `orderInPlay(+0x44)`：**本份 `.c` 复核不了**（账里上一轮记的结论，本轮无法独立确认）。
- `CardScript.IsInHandState(CardStateOptions)` / `RemoveCardFromCemetery` / `SetAsDeckDrawing`：**没有等价落点**（我们无 `+0x228` 状态机、无 `+0x24b`/`0x10` 中间态）⇒ 仍 B，且不是漏做。

---

## ⑤ 顺手发现（**报上来，我没自己修**）

1. 🔴 **`Exhausted`（= `canAct`）与 `mightAct` 不是同一件事，而且两者在真实局面上结论相反。**
   `CardScript__get_mightAct.c:7-27` 一处都不读 `canAct`；`UnitState.cs:253-256` 的 doc 也写着「不能拿 `Exhausted` 顶替」。
   实测：`ruleprobe ai` 的夹具（直接摆棋盘 ⇒ `Exhausted = !HasDeployExemption` 为真、`SummonSickness` 为假）
   正好落在两者分叉的区间 —— 把 `Exhausted` 那句去掉，`ai "Path of the Warrior"` 的 option 5 从 `0` 变 `-1.2`。
   ⇒ **谁该管 `BestAttackScore` 这一格要引擎线裁一下**；我取的是**保守合取**（两个都过），⛔ 别单方面删 `Exhausted`。
2. 🟡 **`PlayerState.FreeSlots()` 与 `BoardSlots.RoomOnSide()` 是两份同式实现**（本轮已把 `FreeSlots` 收口成转发）；
   但 `FreeSlots` 的**唯一调用点**是 `PlayerState.ToString()`（`BattleDriver.cs:26823` 用的是 `HasFreeSlot`）⇒ 它几乎是死代码。
3. 🟡 **账里 `AI.ScoreFromManaBuff` 的落点 `AiScoring.cs:719` 已经漂了** —— 现读 `ManaBuffValue` 在 **`:728`**；
   而 `HEAD` 版就已经是 728 ⇒ **不是本轮造成的**（`HandBuffValue:620` / `ScoreFromJam:807` / `TraitAiScore:863` 三个都对）。
   那是一条 **C 档**行，不在我的 B→A 范围，没改。
4. 🟡 **`BattleDriver.ShowsQuestPoints(string faction)` 是 `UsesQuestPoints` 的表现层孪生**（同一串 `"DarkAngels"`）。
   本轮把判据收到引擎侧 `PlayerState.UsesQuestPoints()`，表现层应当转发过来（见 §六①）。
5. 🟢 **`BattleManager.GetGameState()`（129 行）是一条纯**调试字符串**组装**（`String.Format` 8 个 warlord/cards-played/hand/deck 字符串）——
   落在 `BattleContext` 那行「对外快照」其实对应不上。它不属于战斗逻辑，建议归「工具·文档」域或记 D（**我没判**，只报）。

---

## ⑥ 给调度台的请求

1. **`BattleDriver.ShowsQuestPoints` 转发到 `PlayerState.UsesQuestPoints()`**（`CardPresentation/Battle/BattleDriver.cs:187`）。
   注：`CardPresentation/**` **不在我白名单**，所以我只写了引擎侧判据，没有接线。
2. **给 `ruleprobe` 加两个读数口**（这是 §四 里 3 条 B→A 的**唯一**卡点）：
   - `ai actions <卡id>`：打印 `SimpleAI.EnumerateActions(ctx)` 的列表（`Kind / HandIdx / Slot / TargetP / TargetSlot / ManaCost`）——
     解锁 `GetAvailableActions`；
   - 棋盘/单位那一行加 `PlayOrder`（或 `play --order`）—— 解锁 `GetMinionList` / `SetUnitOrderInPlay`。
   （`工具/ruleprobe/**` 不在我白名单，⛔ 我没动。）
3. **`AI.ScoreFromHandBuff` 需要一次 VA 级复读**（`RVA 0x93B7A0` 的返回表达式）—— 两份反编译产物
   （`decomp_full/` 与 `decomp_AI/`）**逐字节相同**、`return;` 处的值全丢；`AiScoring.HandBuffValue` 的公式目前只有
   上一轮 `R11` 的 RVA 转述，G0 过不了。
4. **`Exhausted` vs `mightAct` 判定归属请引擎线拍板**（§五·①）。若判定「原版就是只看 `mightAct`」，
   则应删掉 `AiScoring.cs:547` 那句 `Exhausted || IsStunned` 并重跑 `RuleEngineTest` 的 AI 判别式断言。
5. **预期账影响**：本补丁 6 条 A + 3 条 B（改的是 `判据出处`/`判据要点`/`差异`/`备注`，状态不变）。
   合并请带 `--only B8_E_`；合并前先跑 `工具/check_patch_conflicts.py`。
6. **预期漂移**：`PlayerState.cs` 的 `FreeSlots` 落点由 `:226` 变 `:245`（已在补丁里改写）；其余落点**零漂移**。
   `play check` 基线**未重钉**（`a93ffb8f…` 仍一致，⛔ 无需重钉）。
