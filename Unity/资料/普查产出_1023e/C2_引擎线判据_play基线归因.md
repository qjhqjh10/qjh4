# C2 · 引擎线判据 —— `play` 基线归因（现读）

> 只读普查代理 C2（2026-10-23 23:51–23:59 现跑）。**没有改任何代码 / 数据 / 正本 / 任务总表；没有跑 Unity；没有动 git。**
> 量具（纯 dotnet）：`bash d:/4/Unity/工具/ruleprobe.sh check` · `… play check` · `… play <id>` · `… play all pf`。
> 判据源：反编译 `d:/2/tools/decomp_full/` + 卡面/官方本地化 + `cards_engine.json`（只读）。
> 行格式：`编号 | 查的是什么 | 判据结论 | 出处 | 状态 | 照着做时怎么落`

---

## 〇、现读数（本轮现跑，**与台账上的旧数字不同**）

| 量具 | 读数 |
| --- | --- |
| `ruleprobe.sh check`（解析层） | **rc=0** · 全池 **1,121 张** · 解析差异 **0 行** · 改名/新增/消失 0 · **哨兵合计：失败 0 条** |
| `ruleprobe.sh play check`（结算层） | **rc=1** · 基线 `out_play_baseline.txt`（主档 1,127 + 电池 586）· **结算指纹 现算 `ca6ce44bef0b45a748c5216b3dd2619e` ≠ 基线 `5d81171ae96e5a07df9cdc1a2fac51cf`** |
| ①-a 主档差异 | 🔴 **6 张卡变了**（台账旧记「3 主档」）——`AM43` · `BL64` · `DA28` · `GOF42` · **`SW53`** · **`TAU58`** |
| ①-b 电池差异 | 🔴 **463 张卡变了**（台账旧记 467 / 476） |
| 确定性 | `play all pf` **连跑两次 md5 相同** = `feef906b6849deac2e6bfccadf5cee40`（基线与代码都在动的那批之后的新值；⛔ 别拿 1023c 的 `12720980…` 当现状） |

> ⚠️ 与台账/签字文档的数字差异（3→6 主档、476→463 电池）都是**代码在这之后又改过**造成的，不是本轮工具问题。

---

## 一、总表

| 编号 | 查的是什么 | 判据结论 | 出处 | 状态 | 照着做时怎么落 |
| --- | --- | --- | --- | --- | --- |
| **P-01** | 主档 `AM43 Recon Operation` 变了 | **改对了**：新行为 = 打出后**抽 2 张**（`+filler27` / `+filler28`，牌库 −2）；旧基线 = 不抽。卡面「If **no enemy is affected**, draw 2 cards」——`count` 结构化求值（`CountEval`，2026-10-23 `新账-97 ④`）判「没有敌人被影响」为真 ⇒ 抽 2。 | 卡面（`cards_engine.json` 的 `desc`）；`CardScript` 侧判据 → `FilterMethods__CheckIfMeetsCriteria.c`（`CountEval` 已按其重写）；本批报告 `普查产出_1023d/引擎批量_A1690_97_95_执行报告.md` §1·3 场景 B。 | **已闭合** | 签字即可（是改对的那一张）。 |
| **P-02** | 主档 `BL64 Murderous Desires` 变了（`playRc 0 → 18`） | **照原版正确、夹具盲**：`conditionsFilter=341`（= 还没用过主动能力）+ `excludeCondition=1` ⇒ 夹具里**没有合法目标**，引擎返回 `ErrNoTargetAvailable(18)` 是**对的**。签字报告已按这个口径签过。 | `普查产出_1023c/play基线归因签字.md`；`B-16` 报告（`普查产出_1023b/引擎线_B16_执行报告.md`）。 | **已闭合** | 签字。**建议**（工具线）：电池里先给一个友军把 Duty 用掉，夹具就不再盲。 |
| **P-03** | 主档 `DA28 Company Master` 变了 | **方向对**：差别只在「手牌总价 6→0 / 7→1」这一列，即那张牌的手牌费用修正（`When you draw a card, it costs 1 less this turn`）被算进手牌总价。签字报告已判「方向对」。 | `普查产出_1023c/play基线归因签字.md`。 | **已闭合**（旧签字） | 签字。 |
| **P-04** | 主档 `GOF42 Da Irongob` 变了 | **方向对**：新行为少了 `FIX_A 2/0/0/2 → …/4`、`FIX_B …/3` 那类**夹具内部**改动，保留 `WARLORD …/30 {concussion1}` = 卡面「Your Warlord gains Concussive until your next turn and heals 5」的真实效果。`B-17` 已落（`GOF42` 是 B-17 真生效的那一张）。 | `普查产出_1023c/play基线归因签字.md` + `T06` 行的 B-17 记录（引擎线任务总表 §一 `T06`）。 | **已闭合**（旧签字 + B-17） | 签字。 |
| **P-05** | 🔴 **主档 `SW53 Embers of Prospero` 变了（新出现）** | **改对了**。卡面：「Give Hunt Mark to an enemy troop. If it **already had** Hunt Mark, destroy it instead」。夹具里的目标 `FIX_E1` 是**本局刚下的新兵**（`play SW53` 的日志逐行可见：回合 1 部署，此后没被给过任何标记）⇒ 正确分支 = **给 Hunt Mark**。新行为正是给 `{huntmark1}`；**旧基线把它打死了 = 走了「already had ⇒ destroy」那一支（错）**。 | 卡面 `cards_engine.json` `SW53.desc`；现跑 `bash 工具/ruleprobe.sh play SW53`（日志末：`战术卡：「Give Hunt Mark to an enemy troop」给了 1 个目标`）；差异列见 `play check` 的 `☠ 离场 P1[3] FIX_E1 … → .`（旧） vs `✎ 改动 P1[3] FIX_E1 … {huntmark1}`（新）。 | **已闭合** | 签字（改对）。 |
| **P-06** | 🔴 **主档 `TAU58 Coordinated Engagement` 变了（新出现）** | **改对了**，与 P-05 同型：卡面「Give Markerlight 2 to an enemy troop. If it **already had** Markerlight, destroy it instead」。旧基线把新兵 `FIX_E1` 打死了；新行为给 `{markerlight2}`（值 = 卡面那个 2）。 | 卡面 `TAU58.desc`；`play check` 差异列（旧 `☠ 离场 P1[3] FIX_E1 … → .` vs 新 `✎ 改动 P1[3] FIX_E1 … {markerlight2}`）。 | **已闭合** | 签字（改对）。 |
| **P-07** | 电池 **6 行未查清**（`GOF67/GOF87/SW8/SW9/SW11/TL16`） | **6 → 3**：本轮现跑，`SW8` / `SW9` / `SW11` 三行**已经与基线逐字节相同**（不再出现在差异里）。剩三行见 P-08/P-09/P-10。 | 逐行比对：`Unity/工具/ruleprobe/基线/out_play_baseline.txt` 里以 `E` 开头的电池行 vs 本轮 `play all pf` 产物。 | **已闭合**（这 3 行已回基线） | 从「未查清」清单里划掉这 3 张。 |
| **P-08** | 电池 `GOF67 Boss Nob` 变了 | **改对了**：卡面「Mob: Give +1 Health to adjacent units」。旧基线把同一个相邻单位**连加两次**（`FIX_A 2/0/0/2→3` 与 `2/0/0/3→4` 两条），还把 `GOF67` 变形成 `FIX_WAY`（夹具链）；新行为**只加一次**（`FIX_A 2/0/0/2→3`）。⇒ 触发次数从「多响」回到「一次」。 | 卡面 `GOF67.desc`；差异列见 P 表下方「原文」。根因族 = 触发入队闸（`abilityContext`，`W63` 那批：`RawCardScript__TriggerAbilities.c:108,207` 的闸在**入队前**）。 | **已闭合**（方向对） | 签字（改对）。 |
| **P-09** | 电池 `GOF87 Zodgrod Wortsnagga` 变了 | **改对了**：卡面「Mob: Deploy a Grot or a Snakebite Grot at random」。旧基线里 **Mob 这一支一次都没跑**（`ctrl 0 / net 0`）；新行为真的部署了 `GOF78`（`✚ 落场/生成 … GOF78 1/2/0/2 {tide1}`、`cite 弃牌 P0 +「GOF78」`、并带出 `FIX_E3` 的死亡链）。同一族（Mob 触发）。 | 卡面 `GOF87.desc`；差异列（旧 `net 7 / ctrl 0` → 新 `net 13 / ctrl 6`）。 | **已闭合**（方向对） | 签字（改对）。 |
| **P-10** | 电池 `TL16 Mawloc` 变了 | 🔴 **查不到（还差什么见下）**。差异只有**两条 3 点伤害给 P1**（旧 `☠ 伤害累计 P1 10 → 13` 与 `13 → 16`，新**没有**了；`net 18 → 16`）。卡自己的正文「Strike: Return this card to your deck and reduce its cost by 3」**两侧完全一样**（`☠ 离场 P0[2] TL16 … → .` + `🂠 牌库 P0 +「TL16」` + `✎ 改动 P0[3] FIX_A 2/0/0/-997` 都在）。这 6 点伤害**不对应 TL16 的卡面**（它没有伤害效果），只可能是**夹具链下游**（`TL16` 的 `Blast 3` 是**攻击时**对目标相邻的溅射 —— `RuleCore.cs:6152-6178`；而电池是 `tactic/troop2/kill` 那一串合成步骤）。 | 卡面 `TL16.desc`；`play TL16` 的电池读数（`电池局自己的变化条数 = 41 · 对照局 = 49 ⇒ 净差 16`）；`play check` 差异列。 | **查不到**（还差：① 电池里 `TL16` 到底有没有「打中一个相邻单位 ≥2 个的敌人」那一步 —— 读 `RunEventPass` 的电池脚本；② 原版 `Blast` 溅射与 `Strike(580)` 触发（`CardScript__ResolveUnitAttacked.c:100`，由 `BroadcastUnitAttacked` @`_ResolveAttack:988` 调）的**相对先后**，以及「返回牌库」那条动作是同步还是入队 —— 这两点半读） | 先按**「电池链副作用、方向未证」**挂账；⛔ 别拿它重钉基线。要闭合就按右栏两点补读（都在反编译里，不跑 Unity）。 |

---

## 二、原文证据（差异列原样）

### P-05 / P-06（主档）

```text
SW53  旧: ⚙ 能量 P0 20→17; ✎ 改动 P0[4] WARLORD …28→29; ☠ 离场 P1[3] FIX_E1 2/0/0/6 → .;
          ✎ 改动 P1[4] WARLORD …30→29; ✋ -SW53; 🗑 +SW53; 💰 4→1; ☠ 伤害累计 P0 0→1; 🗑 弃牌 P1 +FIX_E1
      新: ⚙ 能量 P0 20→17; ✎ 改动 P1[3] FIX_E1 2/0/0/6 → 2/0/0/6 {huntmark1}; ✋ -SW53; 🗑 +SW53; 💰 4→1
TAU58 旧: ⚙ 20→17; ☠ 离场 P1[3] FIX_E1 …→ .; ✋ -TAU58; 🗑 +TAU58; 💰 4→1; 🗑 弃牌 P1 +FIX_E1
      新: ⚙ 20→17; ✎ 改动 P1[3] FIX_E1 … → … {markerlight2}; ✋ -TAU58; 🗑 +TAU58; 💰 4→1
```

⇒ 旧行为**额外**打死了 P1 的一个单位、并动了双方督军血（连带夹具链）；新行为只做卡面那一件事。

### P-08（电池 `GOF67`）

```text
旧: ✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/0/3; ✎ 改动 P0[3] FIX_A 2/0/0/3 → 2/0/0/4; ✎ 改动 P0[3] GOF67 6/4/0/2 {mob1} → FIX_WAY 0/0/0/2 {waystone1}
新: ✎ 改动 P0[3] FIX_A 2/0/0/2 → 2/0/0/3
```

### P-09（电池 `GOF87`）

```text
旧: … ✚ 落场/生成 P0[7] . → GOF78 …; ✎ 改动 P0[6] FIX_C → GOF78; ☠ 离场 P0[7] GOF78 → .; ✚ P0[5] → GOF78 …
新: … ✎ 改动 P0[2] GOF87 5/3/0/1 {mob1,talent1} → GOF78 …; ☠ 离场 P0[2] GOF78 → .; 🗑 弃牌 P0 +GOF78;
     ☠ 伤害累计 P0 15→16; ✎ 改动 P0[5] FIX_B 3/0/0/3 → 3/0/0/2; ☠ 离场 P1[5] FIX_E3 1/0/0/6 → .; ☠ 伤害累计 P0 16→24; …
```

⇒ 旧版 Mob **没作用在 GOF87 自己身上**（改的是 FIX_C/FIX_A 那几张），新版才把 GOF87 换成 GOF78（= 卡面「部署一只 Grot」）。方向明确。

### P-10（电池 `TL16`）

```text
差: 旧 ✎ 含 `☠ 伤害累计 P1 10 → 13` 与 `☠ 伤害累计 P1 13 → 16`；新没有这两条（其余逐字相同）
```

---

## 三、归因汇总（给「能不能重钉」用）

| 类别 | 张 | 结论 |
| --- | --- | --- |
| 主档改对了 | **4** | `AM43`（CountEval 结构化）· `SW53` / `TAU58`（`already had` 条件，旧版走了「instead」那支）· `BL64`（原版正确、夹具盲） |
| 主档方向对（旧签字） | **2** | `DA28`（手牌费用修正进总价）· `GOF42`（B-17 真生效） |
| 电池：本次已回基线 | **3** | `SW8` / `SW9` / `SW11` |
| 电池：改对了 | **2** | `GOF67`（Mob 只加一次）· `GOF87`（Mob 真的部署了） |
| 电池：**未查清** | **1** | `TL16`（6 点伤害差异，不对应卡面；可能是 Blast/Strike 次序在电池链里的连锁） |
| 电池：其余 | **457** | 台账已签字为「既有漂移」（`play基线归因签字.md`：416 反击取哪档攻击力 / 43 `B-31` 拆码 / 等）；**本轮没有重跑那 457 行的逐行归因**，沿用旧签字 |

⇒ **仍然不建议重钉基线**：① `TL16` 那一行没有判据；② 457 行的逐行签字是**旧代码状态下**做的，
本轮代码又变过（`BL64`/`SW53`/`TAU58`/`GOF67`/`GOF87` 都是新的）⇒ 重钉前要重新走一遍「逐行归因 → 签字」，
否则会把未查清的 `TL16` 一起钉成「预期」。重钉的正确姿势见台账 `play 基线·重钉结论` 那一行（四条前置）。

---

## 四、顺手发现（⛔ 未改）

1. **`SW53` / `TAU58` 这一对是「`already had X ⇒ instead`」这条链的活样本**：它们现在两侧都对，
   说明 `already had` 的求值已经是「看目标身上有没有这个关键词」。建议补一条**电池级断言**
   （先给目标上标记、再打一次，必须走 `instead`）—— 今天只验了「没有标记时给标记」这一半。
2. **`TL16` 的 `Blast 3` 在电池里的表现**值得单列一格：`Blast` 是**攻击时**对目标相邻的溅射
   （`RuleCore.cs:6152-6178`，判据 `_ResolveAttack:1020-1045` + `ResolveTraitAdjacentDamage.c`），
   而电池脚本是 `softAtk/chip2/tactic/troop2/kill` 一串合成步骤 ⇒ 这条差异很可能根本不在 `TL16` 身上。
   建议工具线给电池加一列「这一步是谁造成的」。
