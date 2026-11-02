# B117-WREPLAY · 回放跳过动画另一半 + 宿主断言（执行代理报告）

- 代理：`B117-WREPLAY`（第一百一十七会话）
- 独占文件：`Core/EventTiming.cs` · `Battle/BattleDriver.cs` · `Editor/BattleScene.cs` · `RuleEngine/Core/RuleCore.cs`（仅一条注释）
- 判据正本：`判据/B117-WBD_BattleDriver六条.md` §二 / §五
- 日期：2026-11-03
- 类型：**执行报告**（⛔ 不写「已完成/未完成」；状态见 `活状态表.md`）

---

## 一、四件事逐条（旧 → 新）

### 1️⃣ `EventTiming.cs` · 回放跳过动画（`B117-40` 的另一半，7 处）

> ⚠️ 与判据文档 §二 的两点差异（都是**刻意**，理由写在下面）：
> - 新增的两个成员**没有**插在 `HitIsVulnerable` / `ReplaySkipAnimDivisor` 旁边各占新行，而是**挤在 `:113` 同一物理行**（字段 + 单行方法）。理由 = 本文件被账按行号引用 `:301 / :323 / :328 / :1024 / :1408`，**插行会让它们整体漂移**（CLAUDE.md「新成员追加类尾 / 行数守恒」）。
> - `RetaliationHoldOf`（生产路径）也一起缩了 —— 判据文档 §二⑥ 只列了 `EvtKind` 重载那一格；但生产走的是 `DurationOf(BattleEvent) → RetaliationHoldOf`，**只缩非生产那一格等于没做**。

| # | 落点 | 旧 | 新 |
|---|---|---|---|
| ① | `EventTiming.cs:113`（同一物理行追加） | `public static System.Func<BattleEvent, bool> HitIsVulnerable;` | 追加 `public static System.Func<bool> ReplaySkipAnims;` ＋ 单行 `static float SkipScaled(float seconds) { … / ReplaySkipAnimDivisor … }`（`null` = 未挂钩 ⇒ 原样返回，逐位不变） |
| ② | `EventTiming.cs:145` | `return AttackStepRanged + chain;` | `return SkipScaled(AttackStepRanged) + chain;` |
| ③ | `EventTiming.cs:147` | `return AttackStepRanged + RangedVfxFallback;` | `return SkipScaled(AttackStepRanged) + RangedVfxFallback;` |
| ④ | `EventTiming.cs:327`（doc，同一行改写） | 「等价门 = `BattleDriver._replaySession`」 | 「等价门 = `EventTiming.ReplaySkipAnims`（`Begin` 绑到 `ReplaySkipAnimsNow` = `_replaySession && _replayPaused`）」 |
| ⑤ | `EventTiming.cs:405`（`DelayBetween` 近战剩余那截） | `MeleeDamageLag - AttackStepMelee` | `SkipScaled(MeleeDamageLag) - SkipScaled(AttackStepMelee)`（⇒ 回放档 0.1，与 `DurationOf(Attack)` 0.0333 相加 = 0.1333，与驱动侧 `ReplayScaledAttackStep` 同值） |
| ⑥ | `EventTiming.cs:638`（`DurationOf(EvtKind)`） | `case EvtKind.Attack: return AttackStepMelee;` | `… return SkipScaled(AttackStepMelee);` |
| ⑦ | `EventTiming.cs:707`（`EvtKind.Retaliation` 兜底档） | `AttackStepRanged + RangedVfxFallback` | `SkipScaled(AttackStepRanged) + RangedVfxFallback` |
| ⑧ | `EventTiming.cs:743`（`DurationOf(BattleEvent)`） | `e.Ranged ? AttackStepRanged : AttackStepMelee` | `SkipScaled(e.Ranged ? AttackStepRanged : AttackStepMelee)` |
| ⑨ | `EventTiming.cs:805`（`HitFrameLag`） | `attack.Ranged ? AttackStepRanged : AttackStepMelee` | `SkipScaled(…)` |
| ⑩ | `EventTiming.cs:937-938`（`DeployHoldOf` 两档） | `AttackStepMelee + DeployLandHold` / `AttackStepMelee` | `SkipScaled(AttackStepMelee) + DeployLandHold` / `SkipScaled(AttackStepMelee)` |

- ⚠️ **未动**：`TriggerHold` 的「死值」标注（`WBT` 写的，原样保留）· 登场那三个数（0.95 / 1.20 / 1.30）· `AttackWindUp`（= `attackStepTime * 2.0`，原版那一下读的是**裸字段**、不走 `get_AttackStepTime`，故不缩）。

### 2️⃣ `BattleDriver.cs` · 接一行

| 落点 | 旧 | 新 |
|---|---|---|
| `BattleDriver.cs:3877`（同一物理行追加） | `EventTiming.HitIsVulnerable = e => e != null && e.IsVulnerableSecondHit;` | 同一行追加 `EventTiming.ReplaySkipAnims = () => ReplaySkipAnimsNow;` ＋ 判据注释 |

- 位置 = `HookEventTimingCardSource()`（`Begin:5021` 调）—— 与另外三个 `EventTiming` hook 同处，**每次 `Begin` 重挂**；属性本身实时读 `_replaySession && _replayPaused`。
- ⚠️ 之所以**不**另起一行：`BattleDriver.cs` 有大量账按行号引（`git diff --numstat` 也因别人未提交改动不可用）⇒ 同一物理行追加 = **零行数变化**。

### 3️⃣ `Editor/BattleScene.cs` · 宿主断言（`B117-48`）

| 落点 | 旧 | 新 |
|---|---|---|
| `:7203-7205`（3 行 → 3 行） | `Check(drv.LogHoverCardKey == wantKey, …「弹的就是那一行的卡」…)` —— 断的是**小预览卡还弹着** | `Check(drv.CardDisplay != null && drv.CardDisplay.Visible && drv.CardDisplay.ShownTitle == wantKey && string.IsNullOrEmpty(drv.LogHoverCardKey), …)` —— 断**大卡窗显示那张卡**＋**小预览卡当场收掉** |
| `:7169`（注释，同一行改写） | 「…我们认整行矩形」 | 补 2026-11-03 订正：本段 `RowAt` 用例只测**行矩形降级口**；生产判据已由 `B117-49` 收窄到 `LinkRowAt` |
| `:7176`（注释，同一行改写） | 「① 命中区就是行底板那颗 quad 的真实矩形」 | 改成「① **行矩形这一档**（`RowAt`）…（`B117-49` 之后生产命中走 `LinkRowAt`，这里是降级 / 判别口）」 |
| `:7188`（注释，同一行追加说明） | W57 那一条「原来断面板没被关掉」 | 同行补 `B117-48` 说明：点行终点 = 大卡窗，小预览卡是悬停落点、点击当场收掉 |

- 断言 **未删、未改成恒真**：保留「点了就该看到那张卡」的语义（`ShownTitle == wantKey`）。
- `wantKey = log.RowLinkKey(linkRow)`（= 日志行 `<link="…">` 里的 `e.CardId` = 卡英文名）；`ShownTitle` = `CardText.Name(def)`，批处理英文档下二者逐字相同（先例 = `BattleScene.cs:4730` 已有 `ShownTitle == c0.name` 的断言）。

### 4️⃣ `RuleCore.cs:645` 过期注释（同一行改写）

| 旧 | 新 |
|---|---|
| 「走 `GameplayVariables.warlordLifeChange`（经典 0 ⇒ 这一段一次都不走，老行为逐字不变）。」 | 「…（🔴 2026-11-03 就地订正：**经典现读 +10 ⇒ 这一段会走**、冲突 Skirmish 现读 0 ⇒ 不走；见 `GameplayVariables.cs:141/162` 的 `ClassicWarlordLifeChange`）」 |

- 现值判据：`GameplayVariables.cs:141`（`【推断】督军生命增减：经典 +10 · 冲突 0 —— 2026-11-02 改定`）· `:162`（`warlordLifeChange = ClassicWarlordLifeChange`）· `:116`（`SkirmishWarlordLifeChange = 0`）。

---

## 二、新增的「灭自证」

`BattleScene.cs:7203` 那一条同时挂两个**结构上不可能同时满足**的反向判别式（任一把实现退回旧路，它就红）：

1. `drv.CardDisplay.Visible` + `ShownTitle == wantKey`
   ⇒ 🧨 把 `ShowLogCardLarge` 退回 `ShowLogCard`（旧的小预览卡）⇒ **`CardDisplay.Visible == false`、`ShownTitle` 为空 ⇒ 红**。
2. `string.IsNullOrEmpty(drv.LogHoverCardKey)`
   ⇒ 同上退回旧路 ⇒ **小预览卡又挂着、`LogHoverCardKey` 变回卡名 ⇒ 红**。

（原断言 `LogHoverCardKey == wantKey` 正好是这两条的反面；已按判据替换，不是删掉。）

---

## 三、闸

| 闸 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_wreplay bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0**（最终一次；中间被别的写手打断，过程见下） |
| `python -I -X utf8 工具/check_aggregate_lines.py`（四份文件） | 聚合行 1 · 调用 55 · **违规 0 ⇒ PASS** |
| 行数守恒 | `EventTiming.cs` **1503 → 1503**（`:113` 同一物理行挤两个成员）· `BattleScene.cs` **51301 → 51301**（3 处各 1 行改 / 断言 3 行换 3 行）· `BattleDriver.cs` **40718 → 40718**（`:3877` 同行追加）· `RuleCore.cs` 1 行换 1 行 |
| 账行落点 | `EventTiming.cs:328` 仍是 `ReplaySkipAnimDivisor`、`:1024` 仍是 `DrawDealOf` ⇒ 无漂移 |
| 行尾 | 四份均 **LF/CRLF 未翻**（`EventTiming`/`RuleCore` = 纯 LF、`BattleDriver`/`BattleScene` = CRLF 与行数相等、**裸 CR 0**） |

### ⚠️ 过程中的环境噪声（都不是本代理的文件，最终已自行恢复）

- 第一次跑：运行时 12 条**全部**指向自己的 `EventTiming.cs`（成员第一版落进了同命名空间的 `AnimTrace` 类）⇒ 改成挤进 `EventTiming` 同一行后 **运行时 0**。
- 随后几次重跑断断续续报：`WarpforgeArena1/Editor/ArenaBuilder.cs(4634,155)` CS1002/CS1513 · `Battle/EnvironmentApplier.cs` 两条 CS0103 · `Battle/BattleDriver.cs(15533,10)` CS1513 —— 三者 mtime 都在这段时间里被**别的写手**改过（`BattleDriver.cs` 从 3,376,212 B 涨到 3,377,035 B，我的 `:3877` 那一行始终在位）。按纪律**记录、未去动**；等到 12:55 那一次重跑，**运行时 0 / 编辑器 0**，本代理的四份改动均在位。

---

## 四、没做成 / 如实记

1. **同一族的残差（判据文档未列，未做）**：
   - `EventTiming.AttackWindUp`（`:773`）**没有**一起缩 —— 它是 `attackStepTime * 2.0`，原版那一下读的是**裸字段**（`WaitForSeconds(attackStepTime * 2.0)`），**不走** `get_AttackStepTime` ⇒ 按判据不缩。若后续查实原版读的是 getter，需补。
   - `EventTiming.RangedDamageLag`（`:821`）/ 驱动 `SkillAnimLagOf`（`BattleDriver.cs:20358`）里的 `MeleeDamageLag` 那一截**没有**缩：判据文档 §二⑥ 未列，且原版 `MeleeDamageLag = 0.4` 是硬编码段（0.1+0.1+0.1+0.2）之和，是否走 getter 未逐字核 ⇒ 留给调度台裁定。
2. **`EventTiming` 的两个新成员与 `HitIsVulnerable` 挤在同一物理行**（可读性差）—— 是为了**零行数漂移**；将来若做一次全账 `--fix` 重钉基线，可以顺手拆成独立行。
3. **未跑 Unity 批处理**（红线）⇒ `BattleScene.cs:7203` 那条断言**尚未实跑验证**，需要在同步点的 `BattleScene.Run` 里确认（尤其 `drv.CardDisplay` 在批处理下非 null、且 `ShownTitle` 与 `wantKey` 同语档）。
4. **`ArenaBuilder.cs:4634` 的编辑期语法错**仍在（别人的文件），它会挡住编辑器程序集的整体绿灯 —— 需要那个文件的写手或调度台处理。

---

## 五、顺手发现

- `EventTiming.cs` 的**类边界**：`EventTiming`（`:53-1263`）与 `AnimTrace`（`:1311-1523`）是**两个并列的顶层 static 类**。「追加到类尾」在这份文件上要看清关的是哪个类 —— 我第一版就追加到了 `AnimTrace` 里（`typecheck` 立刻 12 条 `CS0103`）。建议以后往这份文件加成员时**优先用同一物理行追加**。
- `EventTiming.ReplaySkipAnims` 是**静态钩子**，闭包捕获的是某个 `BattleDriver` 实例；实例销毁后闭包仍可读其字段。当前无风险（`_replayPaused` 在 `SetReplayPaused(false)` 后为假、`_replaySession` 由 `Begin` 清零），但与其它三个钩子同族的「跨实例泄漏」值得记一笔。
