# 远程收招第二段（新账-73）+ `_baseScale` 变陈族（新账-74）—— 执行报告（P2）

> 2026-10-23 · 战斗线 · 执行代理 P2
> 上游判据：`资料/普查产出_1023c/新账71_近战深度缩放_执行报告.md` §1·1~§1·2（曲线口径）与 §四·2（本件就是它没做的那一条）
> 第一权威：`d:/2/tools/decomp_full/`（逐方法读）＋ `GameAssembly.dll` 常量实读。

---

## 〇 结论（两句话）

**A**：原版远程收招是**两段**（`_ResolveAttackRangedAnim` 0.1 s 回正 → `AfterAttack`：**`SetDelay(0.65)`** 再 0.1 s 归位，
逐帧按战场深度重算 `localScale`，收尾 `FinishAfterAttack` 钉回静止位/朝向/缩放）。我们原来只有第一段，**第二段整条缺** ——
现在在 `CardFeel.AttackRecover` 内部接上了（**没改 `BattleDriver.cs`**），整条序列 = `0.1 + 0.65 + 0.1 = 0.85 s`。

**B**：`AttackRecover` / `DealIn` / `DealInPose` 原来用 `DOScale` 直写 `transform.localScale`（`_baseScale` 变陈）——
现在三条都走新加的 `CardFeel.ScaleTo` → `CardView.SetBaseScale`（**唯一口**），**没有新增任何 `transform.localScale =` 直写**。

---

## 一 判据（本轮读出来的硬东西，全部可复查）

### 1·1 段2 = `CardScript.AfterAttack`（`decomp_full/CardScript__AfterAttack.c`）

```
DOLocalMove(transform, *(param_1 + 0x2c4) /*静止位*/, attackStepTime)
  .SetDelay(DAT_1834b315c, …)                       ← 0.65（见 1·2）
  .OnUpdate(b__370_0)                               ← 深度缩放
  .OnComplete(FinishAfterAttack)
```

- **调用点**：`CardScript._ResolveRangedAttack_d__364__MoveNext.c:39`（state 1：段1 那条协程跑完、`CanContinueAnim()` 为真之后）。
  ⇒ 段1 / 段2 **串联**，不是并行。
- **段1**（我们已有）：`CardScript._ResolveAttackRangedAnim_d__360__MoveNext.c:80-104`
  = 一个 Sequence 三条同时（`DOLocalMove(静止位)` + `DORotateQuaternion(identity)` + `DOScale(DefaultScale)`，时长 `attackStepTime`）
  → `WaitForCompletion` → `WaitForSeconds(attackStepTime)` → 才发 VFX（= `EventTiming.AttackStepRanged` 的 0.2）。
- **`OnUpdate`**（`CardScript___AfterAttack_b__370_0.c`）= `localScale = MinionScaleByPositionSO.GetScale(isPlayer, localPosition, defaultScale)`
  —— 与近战那条 `__c__DisplayClass357_0___AttackMeleeAnim_b__0.c` **逐字同形** ⇒ **直接复用新账-71 那套 `DepthScaleFactor` / `ApplyDepthScale`**。
- **`OnComplete`**（`CardScript__FinishAfterAttack.c`）：`SetSortingLayer` → `set_localPosition(静止位)` →
  `set_rotation(identity)`（**世界**旋转）→ `set_localScale(DefaultScale)` → 之后半段是**卡状态机记账**（见 §四·3）。

### 1·2 `0.65` 是**读出来的**，不是挑的

`CardScript__AfterAttack.c` 那一句是 `TweenSettingsExtensions.SetDelay<object>(tween, DAT_1834b315c, DAT_1842d0b20)`
（`DAT_1842d0b20` 在 `0x1842xxxxx` = 元数据区、`DAT_1834b315c` 在 `.rdata` = 字面量）。按 `CLAUDE.md` §三 那套读法
（ImageBase `0x180000000` → PE 节表换文件偏移）读 `d:/2/unity_run_ref/GameAssembly.dll`：

| 地址 | 4 字节 | float |
|---|---|---|
| `0x1834b315c` | `6666263f` | **0.65** |
| `0x1834b2e8c` | `00004040` | **3.0**（= `ReplaySkipAnims` 时 `attackStepTime` 的除数，段2 同样 `/3`） |

**互证**：同一地址在 `FatigueCard._DrawFatigue_d__7__MoveNext.c:64-70` 被当成 `DOScale` 的目标向量分量
（`local_48 = (0.65, 0.65, …)`）⇒ 它就是一个 0.65 的字面量，不是方法指针。
⇒ 落成 `CardFeel.AfterAttackDelay = 0.65f`（`Src.Field`，已登记进 `CardFeel.Catalog`）。

### 1·3 深度缩放的**消费点**清单（新账-71 那张表，本笔收掉第 2 处）

| # | 原版调用点 | 我们 | 状态 |
|---|---|---|---|
| 1 | `AttackMeleeAnim` b__0（近战） | `CardFeel.MeleeAttack` | ✅ 新账-71 已接 |
| 2 | **`AfterAttack` b__370_0（远程收招第二段）** | `CardFeel.AttackRecover` | ✅ **本笔接上** |
| 3 | `ReassembleMinions`（让位/补位） | `CardFeel.Reassemble` | ✅ 新账-71 已接 |
| 4 | `MoveAndDisappearAnim` b__1/b__4（技能动画移动） | `BattleDriver.PlayUnitSkillAnim` | ⛔ 未接（`BattleDriver.cs` 不在本笔白名单） |
| 5 | `AnimFXModuleTransformModifier.Update` | `WFModuleTransformModifier.cs` | ⛔ 未接（不在白名单） |

---

## 二 改了什么（逐文件）

| 文件 | 增/删 | 内容 |
|---|---|---|
| `Core/CardFeel.cs` | **+140 / −12**（本笔部分；文件对 HEAD 合计 247/14，差额是新账-71 已落盘的 107/2） | `AfterAttackDelay` 常量 + `Catalog` 登记；`ScaleTo` / `ToBaseScale` / `_warnedNoCardView`；`HookDepthScale` 加 `activeFrom` / `restingScale` 两个可选参；`AttackRecover` 补段2 + 走 `SetBaseScale`；`DealIn` / `DealInPose` 换成 `ScaleTo` |
| `Editor/CardBaseDemo.cs` | **+135 / −0** | 新节 `AssertRangedRecoverAndBaseScale()`（**8 条编号断言 + 3 条附加 + 4 条夹具**）+ `Run()` 里第 6 节调用 |
| `Core/CardView.cs` · `Editor/CardBaseDemo.cs` 之外的宿主 | 0 | —— |

行尾：`CardFeel.cs` **LF**（3249/0）· `CardBaseDemo.cs` **CRLF**（3512/3512）· 两份都逐个核过，**没翻**（`git diff --numstat` 数字远小于文件行数）。

### 2·1 A 的实现（`CardFeel.AttackRecover`）

```
段1  0.00–0.10  DOLocalMove(home) + DORotateQuaternion(identity) + ScaleTo(默认缩放, OutQuad)
段2  0.10–0.75  AppendInterval(0.65)          ← 原版那记 SetDelay
     0.75–0.85  DOLocalMove(home)              ← 原版 AfterAttack 的位移
                OnUpdate  = HookDepthScale(activeFrom = 0.75, restingScale = 默认缩放)
                OnComplete = 原版 FinishAfterAttack（位置 / 朝向 / 缩放钉回静止态）
```

三条**刻意的设计**（都写进代码注释了）：

1. **`activeFrom` 这道门是必须的**：原版段1 的 `DOScale` 与段2 的逐帧 `GetScale` **在同一根 `transform` 上接力**；
   我们的钩子若挂在整条序列上，会在 0–0.1 s 与段1 的缩放补间**逐帧抢同一个 `_baseScale`**（静默、只在帧序上差一点点）。
   ⇒ 门开在 0.75 s。`Elapsed(false)` 排掉外部 `SetDelay`（否则门会整体后飘）。
2. **`restingScale` 显式传 `toBase`**：段2 的深度系数要乘的是「静止/默认缩放」，⛔ 不能抓钩子那一刻的
   `card.BaseScale`（那时段1 的缩放补间还没跑）。
3. **`scale` 形参 = 「可见缩放」**（调用点传的是 `v.transform.localScale.x`，**含**高亮系数 ⇒ 内部 ÷ `HighlightMul`）。
   ⚠️ 与 `SetPose` 第三参（**基础**缩放）口径不同 —— 因为 `BattleDriver` 的两个调用点不在本笔白名单，改不了传值口径。

### 2·2 B 的实现（缩放唯一口）

- 新 `ScaleTo(card, tr, toBase, dur, ease)`：`DOTween.To(card.BaseScale → toBase, x => card.SetBaseScale(x))`，
  `CardView` 缺席时**不做缩放并出声一次**（⛔ 不退回 `tr.DOScale`，否则这个口就没意义了）。
- `AttackRecover`：`DOScale(Vector3.one*scale)` → `ScaleTo(toBase = scale ÷ HighlightMul, OutQuad)`。
- `DealIn`：`DOScale(one)` → `ScaleTo(1f, OutCubic)`（目标 1 = **基础**缩放，手牌档静止值）。
- `DealInPose`：`DOScale(one*scale)` → `ScaleTo(scale, OutCubic)`（这里的 `scale` 与 `SetPose` 第三参**同口径** = 基础缩放）。
- `grep DOScale CardFeel.cs` 复核：剩下的都是**非卡根**的（卡上徽标 / 治疗字 / 残骸体）或**死亡/墓地**那两条
  （`Dissolve` 的 0.85 收缩、`GoToCemetery` 那条 0.3 s），⛔ 没有新增任何 `transform.localScale =`。

---

## 三 断言清单（`CardBaseDemo.Run` 第 6 节 = 8 条编号断言 + 3 条附加 + 4 条夹具 = 15 条 `Check`）

夹具 = 一张**挂在 `ArenaSlots.ArenaLayer`**、**摆在敌方行 z** 上、按 `isPlayer: true` 算的卡
⇒ 深度系数 = `0.70/0.36 = 1.9444`（判别力最大的一档）。

| # | 断什么 | 🧨 改坏法（能红） |
|---|---|---|
| 夹具 | 曲线两端比 = 1.9444（**测试里独立重写两键插值**，⛔ 不调 `CardFeel.DepthScaleCurve`） | 曲线端点写错 ⇒ 红 |
| 夹具 | `AttackRecover` 建出来了 + 起始基础缩放 0.36 | —— |
| ① | **整条时长 = 0.1 + 0.65 + 0.1 = 0.85 s** | 删掉段2 ⇒ 0.100 红；删掉那记 `SetDelay` ⇒ 0.200 红 |
| ② | **段1 跑完 `CardView.BaseScale` = 0.30**（+ `localScale = BaseScale × 高亮` 结构不变量） | 退回 `tr.DOScale` ⇒ `localScale` 对、`BaseScale` 仍 0.36 ⇒ 红 |
| ③ | **那 0.65 s 里深度缩放不参与**（t=0.50 实测 0.30，不是 0.583） | `activeFrom` 传 0（钩子挂整条序列）⇒ 红 |
| ④ | **段2 在按战场深度重算**（t=0.80 实测 = `0.30 × 1.9444`） | 段2 只位移、不挂 `HookDepthScale` ⇒ 停在 0.30 ⇒ 红 |
| ⑤ | 收尾按 `FinishAfterAttack` 复位（缩放回 0.30、位置在静止位、朝向 identity） | 去掉 `OnKill` 复位 ⇒ 停在 0.583 ⇒ 红 |
| ⑥ | **反档：HUD 层（layer 0）的卡在段2 半路仍是 0.30** | 摘掉 `IsArenaCard` 那道门 ⇒ 只有本条红 |
| ⑦ | `DealIn` 收尾 `BaseScale = 1`（起始 0.5）+ 结构不变量 | 退回 `tr.DOScale(one)` ⇒ `BaseScale` 仍 0.5 ⇒ 红 |
| ⑧ | `DealInPose` 收尾 `BaseScale = 0.65` | 退回 `tr.DOScale(one*scale)` ⇒ `BaseScale` 停在 1.000 ⇒ 红 |

**成对（灭自证）**：④ ⟷ ③（「钩子挂整条」过 ④ 不过 ③；「忘了挂钩子」过 ③ 不过 ④）· ② ⟷ 老写法（只有 ② 红）· ⑥（门）。

⚠️ **① 之前先 `Step(0.001f)`**：DOTween 的 `Sequence` 是**首次推进时**才 Validate 总时长，之前读 `Duration()` 是 0
（不先推一下会让 ① 假红）—— 已写进断言代码注释。

---

## 四 没做 / 没查清（如实标，⛔ 都是本笔白名单外或判据外的）

1. **`BattleDriver.PlayAttackFeel` 应显式传 `e.Player == _me`**（`BattleDriver.cs:19053` 附近）：
   `MeleeAttack(...)` 与新的 `AttackRecover(..., isPlayer)` 现在都**靠静止位离哪一行近**推断（`IsPlayerRow`）。
   两行 z 差 7.698、督军偏移 0.42/0.75 只占 6% ⇒ 结构上不会误判，但**能传真值就该传真值**。
   ⇒ **本该改、没改**（`BattleDriver.cs` 被另一个代理独占）。
2. **`FinishAfterAttack` 的后半段（卡状态机记账）未做**：`+0x228 = 2` / `HasCurrentTrait(0xdc)` → `SendHighlightBloodThirstAction`
   —— 那是**卡状态位 + 嗜血高亮**那一层，我们的 presenter 没有等价状态机（状态在 `RuleEngine` / `BattleDriver`）。
   ⇒ 本笔只补「位姿那三句」，**这半段没做**，如实记。
3. **深度缩放消费点还剩两处**（§1·3 的 4 / 5）：`BattleDriver.PlayUnitSkillAnim` 与 `WFModuleTransformModifier`，
   两个文件都不在本笔白名单；曲线口（`DepthScaleFactor` / `ApplyDepthScale` / `HookDepthScale` 的新两个参数）已经现成。
4. **外部 `SetDelay` 与 `activeFrom` 的耦合**：门用 `Elapsed(false)` 排掉了外部延迟；两个调用点 `delay` 恒 0 ⇒
   今天零影响。若将来有调用点给 `AttackRecover` 传 `delay`，**段与段的相对时序不变**（整体后移），语义正确。
5. **`ReplaySkipAnims` 那一半没做**：原版 `AfterAttack` 在 `ReplaySkipAnims` 时把 `attackStepTime / 3`。我们整套时序
   都没有这个开关（同 `get_AttackStepTime` 其它消费点）⇒ 不是本笔新增的缺口。

### 4·1 残余风险（本笔引入、但与原版同形，主对话可留意）

- **段2 的 `DOLocalMove(home)` / `OnComplete` 会写死「排期时那个静止位」**：若这 0.85 s 内棋盘变化让这张卡
  `Reassemble` 去了新格位，段2 会把它**拉回旧 home**。**原版一模一样**（`AfterAttack` 也补到 `+0x2c4` 那个存下来的位）。
  我们的时间线（`EventTiming` / `ExtendTimelineTail`）**不包含**这 0.85 s，原版也不等它（`_ResolveRangedAttack`
  state 1 调完就返回）⇒ 结构一致，未改。
- **`scale` 形参口径（可见 vs 基础）**：见 §2·1 第 3 条。今天两个调用点都传 `localScale.x`、高亮关着时两者相等。

---

## 五 验证

| 检查 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_p2 bash d:/4/Unity/工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `git diff --numstat`（本笔两文件） | `CardFeel.cs 247/14`（含新账-71 的 107/2 ⇒ **本笔 +140/−12**）· `CardBaseDemo.cs 135/0` |
| 行尾（改完复核） | `CardFeel.cs` LF 3249/0 · `CardBaseDemo.cs` CRLF **3518/3518**（一行都没翻） |
| Unity 批处理 / `-executeMethod` / player | **没跑**（红线） |
| git 写操作 | **没有**（只读 `git diff`） |
| `BattleDriver.cs` | **一个字节没动**（`git diff` 里它那 31/7 是**别的代理**的） |

**⇒ 待主对话跑**：`CardBaseDemo.Run`（本笔断言唯一宿主；顺带验 `DealIn`/`DealInPose` 那两条没回归）。
⚠️ `BattleScene.Run` 里没有 `AttackRecover` 的断言，但**远程攻击的演出**会经过它 ⇒ 建议同批一起跑（`BattleScene.cs` 里
`EventTiming.AttackStepRanged` 那几条只盯 `EventTiming`，本笔没动那个常量 ⇒ 不该红）。

---

## 六 顺手发现（⛔ 未改，本笔白名单外或不该改）

1. **`BattleDriver.PlayAttackFeel` / `ScheduleRetaliationPresentation` 不传 `isPlayer`** —— 见 §四·1（主对话可一行改两处）。
2. **`Hand/CardInteraction.cs:869` 仍然是 `card.transform.DOScale(...)`**（拿起/回弹那一下）—— 与 B 同一族，
   今天**零可见差**（它写的目标值恰好等于 `SetPose` 写进 `_baseScale` 的那个值），但它是**第 4 个直写口**。
   `CardInteraction.cs` 不在白名单 ⇒ 未改。**建议单开一条账**（`CardFeel` 那三条已收，这条是漏网的）。
3. **`CardFeel.Dissolve`（`:2909`）与 `GoToCemetery`（`:2390`）也直写 `localScale`** —— 都是**死亡/墓地**路径，
   卡马上就没了 ⇒ `_baseScale` 变陈无后果。⛔ 不建议改（改了要多记一档「死亡时的静止缩放」）。
4. **`AttackRecover` 段1 的缓动**：原版那三条**没有 `SetEase`**（默认 `OutQuad`，本工程
   `DOTweenSettings.asset:31` = 6）。我们把新加的缩放补间显式写成 `OutQuad`；但外层 `CardTween.Use(seq, Ease.Linear)`
   给整条序列套了 Linear —— **这层交互是原来就有的**，本笔没动（要改得先确认序列缓动到底覆不覆盖子补间）。
5. **`AfterAttack` 里 `SetDelay` 的第三参**（`asIndependent`）读不出真假（那是指针落在元数据区）；
   它对「延迟是不是跟着 timeScale 走」有影响。今天两个调用点 `delay = 0` 之外无 `timeScale` 用法 ⇒ 零影响。
