# B121_D3 — `ta=0 (any)` 接线 + `playersAffected` 语义复核

> 执行写手 `B121_D3` · 2026-11-03 · 任务 `B121-03`（P0）· `B121-07`（P1 复核）
> 改动：`RuleEngine/Core/RuleCore.cs`（7 处，**零行数变化**）· 新建 `CardPresentation/Editor/B121_D3_Assert.cs`
> ⚠️ **越出白名单一处**：`CardPresentation/Editor/B104_MT_Assert.cs` 的 **A9-b**（它原本断言「`ta=0` ⇒ `null`」，
> 依据正是被推翻的那条判据；不改就必红）。只改了那一条断言，未动其余。

---

## 一、224 行分类（`abilityTargets[].ta == [0]`，现读 `cards_engine.json`）

**口径**：用 `triggerAbilityIndices[].ca`（`{ab, trigger, lg, ca}`）join 到 `chosenAbility`，
⛔ 不用 `chosenAbility[]` 数组下标（实测 6 张卡的 `ab=1` 越界，下标法会把它误判成「无 chosenAbility」）。

| chosenAbility | 行数 | 判定 |
|---|---|---|
| 100 `createCardInHand` | 117 | 读池 |
| 70 `summonUnit` | 39 | 读池（`PlayAbility.c:733-734` 把 `possibleTargets` 喂给 `GetCardToSummon`） |
| 60 `buffMana` | 16 | 读池 |
| 0 `drawCard` | 14 | 读池（`AddDrawCard(…, possibleTargets, …)`） |
| 50 `buffHand` | 10 | 读池 |
| 10 `doDamage` | 7 | 读池 |
| 40 `buffSelf` | 5 | 读池 |
| 270 `createCardInDeck` | 5 | 读池 |
| 75 `summonBuffedUnit` | 5 | 读池 |
| **3 `doOnlyVfx`** | **4** | **不读池**：`PlayAbility.c:1456-1458` 只 `CustomDebug.Log` 后立刻 `return` |
| 30 `buffOthers` | 2 | 读池 |
| **合计** | **224** | **消费 220 / 不消费 4** |

**「自带目标」的行 = 0 行**。原版唯一**自建** `TargetCriteria` 再调 6 实参 `GetTargets` 的支 =
`chosenAbility 200 exchangeRandomFriendlyMinion`（`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/AbilityLogic.cs:1745-1759`
＝ `.c:466-483`），**这 224 行里没有一行是它**（`210 copyUnitInCemetery` 也仍遍历 `possibleTargets`，不是自建）。

**为什么「全部消费」不奇怪**：`PlayAbility.c:262` 在 chosenAbility 分派**之前**无条件跑一次
`abilityTargets = GetTargets(manager, targetCriteria, abilityData, true)` 并写进 `possibleTargets(+0x78)`
⇒ 每条 ability 的目标表都是先算出来、再由各分支按需读。

另：`activeAbilityLogic` 里 `ta` 含 0 的 = **17 行 / 14 张**（`AM2 AM4 AM11 AM16 AM28 AM73 DA3 DA7 DA65 DA71 DA75 SOR34 SW3 SW23`）。

---

## 二、接了哪些（落点全是 `RuleEngine/Core/RuleCore.cs`）

1. **`:17963` `AbilityTargetListFromCriteria`**（= `GetTargets` 的等价物）—— 白名单门 `:17969` 加 `ta != 0 &&`；
   `:17973` 那一行建池：`if (ta == 0) { AddSide(己方, includeRemnants: true); AddSide(敌方, includeRemnants: true); } else switch (ta)`。
   - 语义 = 判据 `AbilityLogic__GetTargets.c:884-891`：`GetUnitsInPlayList(clear 0, inclHero 1, inclMinions 1,
     inclHeroParts 1, inclWeapons 1, inclRemnants 1, inclPlayer 1, inclEnemy 1, includeDying 0)`。
   - **八个开关是写死的常量**（⛔ 不读 criteria 的 36 格）⇒ `:18045` 那句逐候选筛加 `ta != 0`、
     `:18061` 那句 `maxTargets` 截断加 `ta != 0`。两条都有判据：原版这一支建完池 `goto LAB_180818485`
     （`= return`，返回口 `:1378-1381`），既没到筛、也没到截断；tooltip 原文「'Any' disregards all other filters」（`dump.cs:23826`）。
   - `includeDying 0` 的等价物 = `EffectResolver.AddSide` 的 `!u.IsAlive ⇒ continue`（本引擎无独立「dying」态）。
   - `includeWeapons / includeHeroParts` 在本引擎没有独立实体 ⇒ `AddSide` 遍历 `Board` 已涵盖一切在场单位（注释已写死）。
2. **`:18440` `TriggerAbilityTargetCriteriaByNumber`**（触发路那条闸）—— `ta` 白名单加 `0`，且 `ta=0` **不吃**
   `MaxTargets >= 1` 那道闸（原版对 `ta=0` 无意义；现池有 **13 行** `ta=0 ∧ maxTargets>=1`）。
   ⚠️ 该路填出的 `AbilityTargetList` 挂在 `EvtKind.Trigger` / `EvtKind.Play` 上，**今天没有消费者**
   （`BattleDriver.ExpandAbilityBeats` 只展开 `EvtKind.Ability`）⇒ 零可观测变化，接它是为了让触发路上的表也是对的。
3. **错注释就地订正**：`:17919-17920` 原来写「其余 `ta`（含 `0 any` —— **池内 0 张**、原版休眠，裁定 ④ 不接线）」
   ⇒ 改成现读数（224 行 / 209 张）＋ 订正痕迹 ＋ 指向本报告。
4. **`ActiveAbilityTargetCriteriaForPlan` 未动**（`:18182` 那条 `ta` 白名单**故意不加 0**）：它是「多行只取一行」那条
   既有裁定的**行选择**策略；把 0 加进 ① 会改掉 `DA65[30,0] / DA71[180,0] / DA75[50,0,0]` 三张的取行结果，
   而那三张**没有任何判据说该取哪一行**（原版是逐行各播各的）⇒ 不发明口径，如实记在这里。
   实际影响面：14 张里 **11 张**仍由 ② 「第一行有 criteria」拿到 ta=0 的 set（AM2/AM4/AM11/AM16/AM28/AM73/DA7/DA3/SOR34/SW23/SW3）。

**断言**（`CardPresentation/Editor/B121_D3_Assert.cs`，7 条）：
A1 双方全场 6 张 · A2 36 格一律不参与（全填 0 仍 6 张）· A3 `includeDying 0`（`Health=0` 的单位不进池，5 张）·
A4 `ta=0 ∧ maxTargets=1` 仍 6 张 · A5 残骸照样进池（criteria 的 `includeRemnants` 填 0 也进，7 张）·
**反证** A6 `ta=10` 仍按 friendly/enemy 建池（4 张）· A7 `ta=65` 仍 `null`。
⚠️ **未接线**：按 `B104_MT_Assert` 的约定，宿主（`RuleEngine/Editor/RuleEngineTest.cs`）由调度台接
（同一物理行末尾追加 `B121_D3_Assert.Run(); _pass += …Pass; _fail += …Fail;`）。

---

## 三、`playersAffected`（`B121-07`）结论：**等价覆盖 ⇒ 不立账**

**判据（逐句现读）**：字段在 `AbilityLogic +0x20`（`dump.cs:19459` 名字还在）。用的源是
`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/AbilityLogic.cs`（**带字段名的真方法体**，比 `.c` 干净）：
`AbilityLogic.playersAffected` 在 **`PlayAbility` 里只被 3 个 chosenAbility 支读**，池内覆盖如下：

| 原版读点 | 语义 | 池内命中 |
|---|---|---|
| `drawCard(0)`（`il2csharp:2323-2460` / `.c:1439-1452`） | `Both\|Self ⇒ 抽给自己`；`Both\|Enemy ⇒ 抽给对面` | **73 行**：Self 70 · Enemy 3（全是 `SW57`，那张的实体效果是 `deploy`，走我们的 `DoDeploy` 双人路）· 未落值 8 |
| `discardWholeHand(165)`（`:1640-1660`） | `IsPlayerAffected` 决定弃哪一边的手牌 | **0 行** |
| `drawUntilEqual(330)`（`:2323-2460`） | 同上双人对称补抽 | **0 行** |
| 其它 chosenAbility | **不读**（`buffOthers/buffSelf/buffHand/summonUnit/…` 里的非零 `pa` 是序列化遗留） | 294 行 |

- `drawCard` 那 70 行 Self ⇒ 与我们的 `EffectResolver.DoDraw`（只给 `owner` 抽）**逐字等价**。
- 那 8 行「未落值」= `pa` 缺省 0 = `Both`，但它们的 `targetCriteria.ta` 全是 **65 (inDeckFiltered)**；
  原版走 `AddDrawCard` 的「点名抽」支（`AbilityLogic__AddDrawCard.c:55-78`），
  循环里只抽 `card.owner == isPlayer` 的候选 ⇒ 给对面的那一次**抽不到任何牌**（池里全是我方牌库）⇒ 实际也是「自己抽」。
- 那 3 行 Enemy（`SW57` 的反直觉 logic 行）不影响：该卡的实体效果是 `deploy`，已由 `DoDeploy` 的 `EachPlayer` 分支覆盖。

⇒ **今天的可观测行为与原版逐字相同 ⇒ 不立账**（按任务要求的三选一取第一项）。
⚠️ 若将来出现 `discardWholeHand(165)` / `drawUntilEqual(330)` 的卡，这一格要重开 —— 写明在这份报告里，
⛔ 不写进账（账只记真差异）。

---

## 四、验证

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_d3 bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh play check` | **rc=0**；结算指纹 **改前 = 改后 = `a8b7eac3884b94b0df3e8e6ba6d53577`**（基线文件也一致） |
| `python -I -X utf8 工具/check_ledger_points.py` | **失败 0 / 漂移 0**（A 5876 通过 5876，告警 889）；⛔ 未用 `--fix` |
| `python -I -X utf8 工具/check_patch_conflicts.py` | ✅ 无跨补丁重复键（11 份） |

**指纹为什么没变（如实说明，⛔ 别当成「没接上」）**：
- 触发那 224 行填的 `AbilityTargetList` 挂在 `EvtKind.Trigger` / `EvtKind.Play` 上 —— 今天**没有消费者**。
- 主动那 11 张的表会挂到 `ctx.AbilityTargetTable`，但该表只在 `EffectResolver.cs:1925` 的
  `spec.Random && spec.Count < pool.Count` 支被读；**这 11 张的 op 全是 deploy/draw/create/buffMana**，
  没有 `spec.Random` ⇒ 结算不变。
- **可观测变化在表现层**：这 11 张的 `EvtKind.Ability` 目标表由 0/1 变 N（双方全场）⇒
  `BattleDriver.ExpandAbilityBeats` 的「多目标拍」会按 N 播。这是原版形状
  （`PlayAnim.c:171`：位置选项含 `abilityTarget` 时按 `abilityTargets` 逐目标各播一份），
  但**本会话不跑 Unity ⇒ 没亲眼验过**，请在同步点的 `BattleScene.Run` 关注这 11 张的拍数断言。

**行数**：`RuleCore.cs` 改前改后同为 18,486 行（`git diff -U0` 的 7 个 hunk 全是 `-N +N`）。
中途曾误把 `else switch (ta)` 拆到新行（`-17973 +17973,2`）⇒ 立刻合并回一行，漂移归零。

---

## 五、顺手发现（⛔ 未动，交调度台分流）

1. **`HandEffect.playersAffected` 是另一个字段**（`HandEffect` 上，不是 `AbilityLogic`），
   由 `buffHand(50)` 支读（`out_il2csharp/AbilityLogic.cs:726-802`）：`Self ⇒ 只给自己的手牌挂`、
   `Enemy ⇒ 只给对面`、`Both ⇒ 两边`。池内**非零 14 行**（Self 13 / Enemy 1；`ca 50` 10 行、`ca 30` 3 行、`ca 0` 1 行）。
   我们的 `DoBuffHand` 现状**没有读它**（`abilityBuffs[].pa` 一列在 `cards_engine.json` 里 0 行）。
   ⚠️ 不是 `B121-07` 那条（那是 `AbilityLogic.playersAffected`），但同一族 ⇒ 建议另立一条 P1 复核。
2. **`SW57`（`Birth of a Saga`）的 logic 行形状很怪**：`chosenAbility 0 drawCard + summon.chooseCardInPlay + pa=20`，
   与卡面「Each player deploys 3 troops from their deck」对不上（真正干活的是 `ca 250` 那两行）。
   我们的 `DoDeploy` 手写等价物覆盖了卡面行为 —— 建议记一笔「数据噪声行」，⛔ 别按它去改 drawCard。
3. **`B104_MT_Assert.cs` 的 A9-b 那条断言是「错判据钉死人」的活样本**（`B-…` 那类）：它的注释原文写着
   「🧨 顺手把它接上 ⇒ 这条红（那会凭『场上全部单位』造出原版休眠档的行为）」——
   **错的地基会产出看起来很有说服力的自检**。本批已就地改成正向断言并留下订正痕迹。
