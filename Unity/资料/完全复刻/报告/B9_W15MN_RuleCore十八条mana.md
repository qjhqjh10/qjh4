# B9 · W15MN —— `RuleCore.cs` 那 18 条 mana（第八十四会话 · 2026-10-26）

> **身份**：执行型引擎写手 `W15MN`。**独占并只碰**：`RuleEngine/Core/RuleCore.cs` · `RuleEngine/Core/PlayerState.cs`（＋本报告 ＋ `账/_parts/B9_W15MN.tsv`）。
> ⛔ 未碰：`EffectResolver.cs` / 其余 `RuleEngine/Core/*.cs` / `RuleEngine/Editor/**` / `CardPresentation/**` / `Shell/**` / `工具/ruleprobe/**` / 主账表 / 两张正本 / git / Unity。
> **判据源**：`报告/B9_W12MN_基础设施P0_mana族.md` §五（接手指南）＋ 我按该指南**实读**的 `d:/2/tools/decomp_full/PlayerManager__*.c` 与 `BattleManager__*.c`。
> ⚠️ 本代理**没有重查 mana 族判据**（按 brief），只对「要写代码的 4 个写入口」和「两处建议档有疑问的」实读了方法体（见 §三）。

---

## 一 结论（一句话 + 数字）

18 条全部有结论：**A 6 · B 6 · C 3 · D 3**（W12MN 原建议 A2·B12·C1·D3；差的 4 条是把「已有内联实现」**收口成方法级落点**后升的 A，另 2 条是**按判据把建议档从 B 更正为 C**，见 §三·1/§三·2）。

| | 条数 | 方法 |
|---|---|---|
| **A**（方法级落点、可机检） | **6** | `AddFaithMana` · `AddSpiritStoneMana` · `SetSavedMana` · `RefillCustomMana`（本批新增 4 个转发口后成立）· `AddQuestPoints` · `AddSecretPlayed`（原本就有落点，只补账） |
| **B**（已实现、但无方法级落点／形状不同） | **6** | `ActivateHero` · `AddNewMana` · `RefillOneMana` · `ResetMana` · `SetupFullHero` · `UseMana` |
| **C**（纯表现 / 表现层缺口） | **3** | `SetAccumulationMana` · `SetQuestPointsWithDelay` · `ToggleCurrentMana` |
| **D**（不做；均为「原版死代码 / 0 入口 / 0 数据」，均带时间限定或永久） | **3** | `DestroyManaSlot` · `RefillMana` · `UseSpiritStoneEnergy` |

**写了多少代码**：
- `PlayerState.cs` **类尾 +58 行**：4 个写入侧转发口（`AddFaithMana` / `AddSpiritStoneMana` / `SetSavedMana` / `RefillCustomMana`），**旧行一字未动**（7 个既有取值口的落点仍 384/391/399/407/416/425/433）。348→438 是 W12MN 的；本批 **438 → 496**。
- `RuleCore.cs` **行数不变（14,971）**：**6 处单行替换**（把内联赋值改成调上面那 4 个口）＋ **1 处注释就地改写（零行数变化）**。⛔ 特意没插新行 —— 这个文件有 460 条账落点，插一行就整片漂移（铁律 18·b）。
- **一条系统都没新建**（W12MN 的判断成立：mana 族的引擎状态与结算早就在，缺的只是逐方法落点）。

**为什么 4 个转发口是「真收口」而不是死码**（每条都有真调用点）：

| 新口 | 原来的内联写法 → 现在 | 调用点 |
|---|---|---|
| `PlayerState.AddFaithMana(int)` | `ps.Faith += 1` → `ps.AddFaithMana(1)` | `RuleCore.cs:1640`（`GrantPrayEnchantmentFaith`，祈祷附魔） |
| `PlayerState.AddSpiritStoneMana(int)` | `SpiritStones += sp` → `AddSpiritStoneMana(sp)` | `RuleCore.cs:294`（`NewBattle` 开局灵魂石）· `:12312`（`CollectWaystoneCore` 路标石） |
| `PlayerState.SetSavedMana(int)` | `ManaCarry = X` → `SetSavedMana(X)` | `RuleCore.cs:2766`（`EndTurnCore`，= `UpdateManaLeftAccumulation` 的 `min` 那一笔）· `:1776`（`BeginTurnCore`，= 兑现后写 0） |
| `PlayerState.RefillCustomMana(int)` | `Energy += n` → `RefillCustomMana(n)` | `RuleCore.cs:1852`（`BeginTurnCore` 兑现 `addedManaSecond`） |

> 这 4 处都是**逐字等价替换**（新方法体就是原来那一句），所以 `play check` 指纹与改动前**逐字节相同**（§四）。

---

## 二 18 条逐条结论

> 「证据」列里的行号都是 **2026-10-26 23:4x 现读**。`EffectResolver.cs` 正被别人改 ⇒ 它的行号合并后请按符号名重定位。

### 2·1 A（6 条）

| 方法 | 我们的落点（现读） | 证据 / 等价性 | 差异（如实） |
|---|---|---|---|
| `AddFaithMana(int)` | `PlayerState.cs:463（AddFaithMana）` | `PlayerManager__AddFaithMana.c` 全文 = `faithMana(+0xB0) += n`（ObscuredInt 解混淆→加→写回）→ `ManaManager.SetFaithMana`；我们 = `Faith += newMana;` | **已收口 1/3**。剩 2 处内联在 `EffectResolver.cs:12704`（`DoFactionResource` 信仰支）/ `:12874`（`FinishActiveAbility`）——**该文件不在白名单**，等它的所有者一次转发即全收口。两处今天都无上限 ⇒ 语义等价 |
| `AddSpiritStoneMana(int)` | `PlayerState.cs:471（AddSpiritStoneMana）` | `PlayerManager__AddSpiritStoneMana.c` 全文 = `spiritStoneMana(+0x9C) += n` | **已收口 2/5**。剩 3 处：`EffectResolver.cs:12705`（gain 支）· `:928`（付费前缀/誓约扣）· `:13166`（`DoSpendSpirit` 全花光） |
| `SetSavedMana(int, bool)` | `PlayerState.cs:482（SetSavedMana）` | `PlayerManager__SetSavedMana.c` = `savedMana(+0xD8) = value` → `ManaManager.SetManaSaved(saved, unused, total, useNumberChange)`；第 3 实参只喂 HUD | **完整收口**（引擎侧两处写点都走这里）。⚠️ 原版**一个字段装两笔**（accumulation 结转 / `_NextTurn:296-334` 的 `addedManaSecond` 补贴），我们**分两处存**（`ManaCarry` / `ctx.Vars.addedManaSecond`）；今天后者 = 0 ⇒ 不可观察（见 §三·4） |
| `RefillCustomMana(int, bool)` | `PlayerState.cs:491（RefillCustomMana）` | `PlayerManager__RefillCustomMana.c:35-41` = `unusedMana(+0x4C) += n`，**无封顶**、`totalMana` 不动 | 等价；值今天 = 0（`addedManaSecond` 两模式都 0、真值是服务器的）⇒ **休眠**。⚠️ `Core/GameplayVariables.cs` 里那句「落点 = `p.Energy += addedManaSecond`」现在该改指本方法（不在白名单，见 §三·5） |
| `AddQuestPoints(int, Action)` | `EffectResolver.cs:13034（QuestPointThreshold）` | `PlayerManager__AddQuestPoints.c` 全文：`questPoints(+0xC4) += n` → 按 `alternativeTraitLogic(+0x4B)` 选阈值（`+0x2c=3` / `+0x30=4`）减一个 → `QuestMilestone++` → `QuestTriggered`（督军 trigger 490） | 原版**延迟 + 协程回调**，我们**同步**（无中间态）⇒ 结果相同、时序不可观察。Alt 档真值在服务器、本地 0 命中 ⇒ 恒按经典 3（已有一次性 `LogWarning`） |
| `AddSecretPlayed()` | `EffectResolver.cs:2462（PlayTacticCore）`（写点 `:2517`） | `PlayerManager__AddSecretPlayed.c` 全文 = `secretsPlayed(+0xEC)++`（无参）；唯一调用点 = 从手牌打出 `spellType==150` | 等价；「生成」一张隐秘不 +=（我们同样只认 `Subtype=="Secret"` 的**打出**路） |

### 2·2 B（6 条）

| 方法 | 落点 | 为什么是 B（不是 A） |
|---|---|---|
| `ActivateHero(bool)` | `RuleCore.cs:3531（RecomputeActAndAttack）` | 原版 = 督军身上的一次 `ActivateMinion`（重算 `canAct +0x230` / `canAttack +0x23C`，**不是纯表现**）。我们的等价重算有、**但「动作结束后对英雄重算」这一跳没有单独入口**（原版唯一调用点 `FinishResolvingAction` ≡ `ActionPump.ActionScope.Dispose`，不在本批白名单）。**判「无可观察差异」的三条理由**：① 督军不可能有召唤病（`BuildPlayer` 里 `p.Warlord.Exhausted = false`，且它从不走 `PlayCard` / `+0x58` 写点）；② 攻击后 `AttacksThisTurn ≥ ActionQuota` ⇒ 我们 `DeclareAttack` 写的 `Exhausted=true` 与原版重算结果同为「不可动」；③ 「退还攻击」那条路已显式调 `RecomputeActAndAttack`。⚠️ **这是推理 + 逐点核对，未被 `play` 夹具覆盖** |
| `AddNewMana(bool, bool)` | `EffectResolver.cs:12626（DoEnergy）` · `:12653（DoRefill）` | 原版「一个方法 + 两个布尔」，我们**拆两条路**（`createMana` / `refillMana` 各自判定）⇒ 没有单一方法级落点。今天逐项等价（现读 createMana **52** / refillMana **43**）。⚠️ `untilEndOfTurn` 子支现读 **0 处**（138 个 manaBuff）⇒ **该子支 D（时间限定）**，没建 |
| `RefillOneMana()` | `EffectResolver.cs:12653（DoRefill）` | 原版 `if (unused != total) unused++`；我们 `Min(MaxEnergy, Energy+n)` —— 只在 `unused > total`（**不可达**）时不同 |
| `ResetMana(int×4)` | `RuleCore.cs:1754（BeginTurnCore）` · `:289（NewBattle）` | 形状不同：原版**一次性 Reset + 跨回合累加器**，我们**每回合从式子重算** + `ctx.Vars.ManaBonus` 记永久增量 ⇒ 逐项等价、无方法级落点。第 2/3 实参（plasma/psychic）**不建模**（两池全池 0 入账）。末段 `ToggleQuestPoints` 是表现层（引擎判据已收口在 `PlayerState.UsesQuestPoints`） |
| `SetupFullHero(CardScript)` | `RuleCore.cs:631（BuildPlayer）`（`+ :632 / :645`） | 引擎半边等价（`Warlord` 绑定 + `Board[WarlordSlot]` 同一对象 + 清 `Exhausted`）；**表现半边**（reparent / `GetHeroBoardPos` / `SetAsHero`）归表现层 |
| `UseMana(int, ManaType, bool)` | `RuleCore.cs:12987（SpendNormalMana）` · 灵魂石支 = `EffectResolver.cs:928` / `:13166` | 我们**没有 `ManaType` 分派这个形状**（出牌恒 normal；灵魂石花费走另一条链）。灵魂石「不够 ⇒ 清零」给不出同形：`:928` 前面有 `CostPrefixPayable` 闸（不够不进扣费）、`:13166` 是「全花掉」⇒ **不会出现「余额被清零」这一状态** ⇒ 不可观察 |

### 2·3 C（3 条 · 表现层）

| 方法 | 判据结论 | 我们的现状 |
|---|---|---|
| `SetAccumulationMana(bool)` | 🔴 **就地更正 W12MN 建议档 B→C**：`PlayerManager` 那层只有一句转调 `ManaManager.SetAccumulationMana(+0x30, active)`；而 **`ManaManager__SetAccumulationMana.c` 全文只有 `GameObject.SetActive` + 一次布局重排** ⇒ 纯表现，**不动任何引擎状态**。引擎侧真值 = `GameplayVariables.manaAccumulation(+0x40)`（由 `UpdateManaLeftAccumulation` 直接读） | 模式真值已在 `Core/GameplayVariables.cs`（经典 0 / 遭遇 1）；**HUD 有没有那个「存能量」提示件待核**（`CardPresentation/**` 不在白名单）⇒ 归表现层批次（P2） |
| `SetQuestPointsWithDelay(float, Action)` | 🔴 **就地更正 W12MN 建议档 B→C**：状态机全文（`PlayerManager._SetQuestPointsWithDelay_d__43__MoveNext.c`）= `WaitForSeconds(delay)` → 取阈值 → `ManaManager.SetQuestPoints(questPoints, 阈值, animate:1)` → 调 `finishCallback`。**全程没有一次字段写**（唯一的调用是 `ManaManager__SetQuestPoints` 与回调）⇒ 纯时序 + 表现 | 我们同步走完（`EffectResolver.cs:13034` 里「先写余数再发 trigger」，无中间态）。若要做「涨满→回落」动画，订阅 `EvtKind.GainQuest` |
| `ToggleCurrentMana(bool)` | `PlayerManager__ToggleCurrentMana.c` 全文 = 一句 `ManaManager.ToggleCurrentMana(active, unused, total)`；唯一调用点 = `CardScript__SetAmbush` ⇒ 埋伏时灰化「当前能量」那组 HUD | 引擎侧无对应物；我们未见同款 HUD 行为（P2） |

### 2·4 D（3 条 · 不做，全部写清理由 + 时间限定）

| 方法 | 为什么 D | 时间限定 / 边界 |
|---|---|---|
| `DestroyManaSlot()` | 唯一入口 = `ProcessManaBuff` 尾循环 `while (i > manaBuff.manaAmount)`，`i` 初值 0 ⇒ **只有 `manaAmount < 0` 才进**；现读全池分布 `{1:121, 2:5, 3:5, 5:4, 100:3}` ⇒ **负值 0 张** ⇒ **0 入口** | **时间限定**：卡数据若出现负 `manaAmount` 需重跑。⛔ 别为它写「摧毁法力槽」的机制 |
| `RefillMana()` | 三源 0 调用点（decomp / ILSpy / assets_full）。方法体 = `unusedMana = totalMana`（直接拷） | **永久**（死代码）。同式已在 `RuleCore.cs:1777` 的 `p.Energy = p.MaxEnergy`（TurnStart 的活路径） |
| `UseSpiritStoneEnergy(int)` | **0 调用点** —— `UseMana` 的 `ManaType.SpiritStone(5)` 支把它**整段内联**了 | **永久**（死代码）。语义已由 `UseMana` 那行（`SpendNormalMana` + `EffectResolver:928/:13166`）承载 |

---

## 三 判据新发现 / 就地更正（只报，改由调度台分流）

### 3·1 `SetAccumulationMana` 建议档 **B → C**（已改进账）
理由见 §2·3。判据 = `ManaManager__SetAccumulationMana.c` 全文（只有 `GameObject.SetActive` + 布局）。W12MN 说的「开局按模式设一次」设的是 **HUD 提示件的显隐**，不是引擎参数。**建议保持 C**：要补就由表现层批次补那个提示件。

### 3·2 `SetQuestPointsWithDelay` 建议档 **B → C**（已改进账）
判据 = 状态机 `MoveNext` 全文：只读 `questPoints` / 阈值 → 转 HUD → 调回调，**零字段写**。

### 3·3 `EffectResolver.cs:12620` 的「49 / 40」是过期数字（⛔ 我不在它的白名单）
现读 `cards_truth.json`（单份、无 `full_raw`）：`manaBuff` **138** 个实例 · `createMana` **52** · `refillMana` **43** · 两者都有的 ⇒ `DoEnergy` + `DoRefill` 各跑一次。**请 `EffectResolver.cs` 的所有者把那一句改成 52/43**（铁律 5）。

### 3·4 原版「savedMana 一个字段装两笔」在我们这里是两处（已写进账）
`SetSavedMana` 的写点 = `UpdateManaLeftAccumulation.c:51-57`（`min(manaAccumulation, GetUnusedMana)`）**与** `_NextTurn:296-334`（后手一次性补贴 `addedManaSecond`）。原版共用 `+0xD8`（**最后写者生效**）；我们分存 `ManaCarry` / `ctx.Vars.addedManaSecond`。
- 今天**不可观察**（`addedManaSecond` 两模式都 0，真值在服务器）。
- ⚠️ 若将来拿到非 0 真值、且与 `manaAccumulation` 同时非 0，要按「谁最后写谁生效」重核一次（同步点记一笔即可，**不必现在改代码**）。

### 3·5 `Core/GameplayVariables.cs` 两处 doc 该跟着改（不在本批白名单）
① `addedManaSecond` 的 doc 写着「落点 = `p.Energy += addedManaSecond`」—— 现在**已改走 `PlayerState.RefillCustomMana`**；
② `manaAccumulation` 的 doc 还以「【规则书:60，**非官方**】」为首要依据 —— 真判据是 `BattleManager__UpdateManaLeftAccumulation.c:51-57`（我只改了自己有权改的那一处：`PlayerState.ManaCarry` 的 doc + `RuleCore.EndTurnCore` 的注释，**零行数变化**）。

### 3·6 主账表里 `GetCurrentFaith`（行 564）的「差异」列现在不准
它写「我们现有 3 处内联 `ps.Faith += n`（`EffectResolver.cs:12676` / `:12846` / `RuleCore.cs:1631`）」——**行号已过期**（现读 `12704` / `12874` / `1640`），且 `RuleCore.cs` 那处**已转发到 `PlayerState.AddFaithMana`**（`AddSpiritStoneMana` 同理，行 562 的对应描述也要更新）。**主表只有调度台能写**，请合并时顺手改这一句。

### 3·7 `ActivateHero` 若要 100% 贴原版，落点在别人的文件
原版唯一调用点 = `FinishResolvingAction`，我们的等价物 = `ActionPump.ActionScope.Dispose`。若 `ActionPump` 的所有者愿意，在 scope 退出时对 `ctx.Active` 的督军调一次 `RuleCore.RecomputeActAndAttack` —— **行为不变**（见 §2·2 的三条理由），但能把「动作后重算英雄」这一跳也接上。⛔ 本批没碰那个文件。

---

## 四 读数（本批实测）

| 检查 | 命令 | 结果 |
|---|---|---|
| **秒级类型检查** | `TMPDIR=/tmp/wf_W15MN bash d:/4/Unity/工具/typecheck.sh` | ✅ **运行时 0 / 编辑器 0**（改完代码后跑了两次，都是 0/0） |
| **账落点机检** | `python -I -X utf8 工具/check_ledger_points.py 账/_parts/B9_W15MN.tsv --all-A --warn-getters` | ✅ **A 6 · 通过 6 · 漂移 0 · 告警 0 · 失败 0** |
| **注解齐备** | `python -I -X utf8 工具/verify_annot.py 账/_parts/B9_W15MN.tsv` | ✅ **缺注解 0** |
| **`ruleprobe check`（解析指纹）** | `bash 工具/ruleprobe.sh check` | ⚠️ rc=1：现算 `c9a647982fad19bb65896011c281c93e` ≠ 钉住 `18439d96…`；解析差异 **1 行 = BL47｜Disgustingly Resilient** ⇒ **与 W12MN 那次读数逐字相同**，归 `E10TB`（解析链我一行没动）。⛔ 未改 `PinnedParseMd5` |
| **`play check`（结算指纹）** | `bash 工具/ruleprobe.sh play check` | ⚠️ rc=1：现算 **`1b40f42ab010e623e908fcd4a236205f`** · 差异 = ①-a **BL47** · ①-b **BL18 / SW25 / UM18** ⇒ **与 W12MN 改后那次逐字节相同**（同指纹、同 4 张卡）。⛔ 未改 `PinnedPlayMd5` |
| 行尾 | 二进制核 | `RuleCore.cs` **CRLF 0 / LF 14971 / 裸 CR 0**（行数与改前一致）· `PlayerState.cs` **CRLF 0 / LF 496 / 裸 CR 0** |

**A/B 隔离（本笔）**：本批 6 处都是「把一句赋值换成调一个**方法体就是那句赋值**的方法」+ 一处**注释**改写 ⇒ 语义恒等。
**实测支撑**：`play check` 的指纹（`1b40f42a…`）与差异集（BL47/BL18/SW25/UM18）**与 W12MN 改完后那次完全一致** ⇒ ① 本批零观察差异；② 那 4 张卡百分百是别人的活（BL47 = `E10TB`、BL18/UM18 = `P-06`、SW25 = `P-05`）。
⚠️ 本批**没跑** Unity 宿主（`RuleEngineTest.Run` 是调度台/自检的活，见 §五·2）。

---

## 五 留给下一路 / 待调度台

### 5·1 没做、留给下一路（点名）

| 事项 | 归谁 | 说明 |
|---|---|---|
| `EffectResolver.cs` 里剩余的 **5 处内联**（信仰 ×2：`:12704` / `:12874`；灵魂石 ×3：`:12705` / `:928` / `:13166`） | `EffectResolver.cs` 的所有者 | 各改成 `ps.AddFaithMana(n)` / `ps.AddSpiritStoneMana(n)`（方法已就位）⇒ 那两条的账可写「完整收口」。⚠️ `:928` 是**扣**（负数入参）——原版 `UseMana` 的灵魂石支也是「减」，用同一个口即可；`:13166` 是「全花光」，建议写成 `AddSpiritStoneMana(-n)` 或保留现状（判定同上：不可观察） |
| `EffectResolver.cs:12620` 的 49/40 → **52/43** | 同上 | 铁律 5（就地订正过期注释） |
| `Core/GameplayVariables.cs` 两处 doc（§3·5） | 该文件所有者 或 同步点 | 一个词级别改动 |
| 主账表行 564 / 562 的「差异」列 | **调度台** | §3·6 |
| `ActionPump.ActionScope` 退出时重算督军（可选） | `ActionPump` 所有者 | §3·7；不做也无可观察差异 |

### 5·2 该配的断言（`RuleEngineTest` —— ⛔ 本代理不跑，交调度台）
> 照项目口径：**先写前提断言、再写被测断言**；⛔ 别用 `if (x != null) Check(...)`（前提不成立会静默跳过）。

| 方法 | 建议断言 |
|---|---|
| `PlayerState.AddFaithMana(2)` | 前提：`GetCurrentFaith()` 记下；`AddFaithMana(2)` ⇒ `Faith +2`；再 `AddFaithMana(3)` ⇒ **无上限**（99+3=102 那种白盒可复用） |
| `PlayerState.AddSpiritStoneMana(3)` | `SpiritStones +3`；`GetCurrentSpiritStone()` 同步（无初始值/上限） |
| `AddNewMana(filled:0)` | `MaxEnergy +1`、`Energy` **不变**（DA61 `Plasma Generator` 那一档） |
| `AddNewMana(filled:1)` | `MaxEnergy +1` **且** `Energy +1`；`untilEndOfTurn` 在本 build **0 入口** ⇒ 写一条「无人调用」的证据断言 |
| `AddQuestPoints(3)`（阈值 3） | 余数回 0 + `QuestMilestone +1` + 往牌库加 1 张隐秘；补一条 **Alt 档 3/4** 的 `alternativeTraitLogic` 支 |
| `AddSecretPlayed` | 打出 `subtype=="Secret"` ⇒ `SecretsPlayed +1`；**生成**一张隐秘 ⇒ **不 +1** |
| `RefillOneMana` | `Energy < MaxEnergy` ⇒ +1；`Energy == MaxEnergy` ⇒ 不变 |
| `PlayerState.RefillCustomMana(2)` | `Energy +2`、`MaxEnergy` **不变**（不封顶） |
| `ResetMana` 的等价（`BeginTurnCore`） | 四池归位：`MaxEnergy == Energy == 1+TurnCount`、开局灵魂石来自第 4 实参 |
| `UseMana(3, Normal)` | `Energy -3`；扣到 0 ⇒ `SpentAllMana` 双方督军各一次 |
| `UseMana(3, SpiritStone)` | `SpiritStones -3`；**不触发** `SpentAllMana` |
| `PlayerState.SetSavedMana(2)` | 写 `ManaCarry == 2`；下回合 `BeginTurn` 兑现后 `ManaCarry == 0` |
| `SetupFullHero` | `PlayerState.Warlord` 指向该卡、`Board[WarlordSlot]` 是**同一个对象** |

### 5·3 交接给下一个代理（若本批之后还有同族活）

- **做到哪**：18 条全部定案并落账；`PlayerState.cs` 4 个写入口已就位并接上 6 个真实调用点；`RuleCore.cs` 6 处单行替换 + 1 处注释改写（行数不变）。
- **剩什么**：§5·1 那五格（全在别人的文件或调度台的主表）。
- **文件动到哪**：`PlayerState.cs` 438→496（类尾 +58）；`RuleCore.cs` **14,971 不变**；`账/_parts/B9_W15MN.tsv` 19 行（表头 + 18）。
- **下一步命令**：`TMPDIR=/tmp/wf_W15MN bash d:/4/Unity/工具/typecheck.sh`（应 0/0）· `bash 工具/ruleprobe.sh play check`（应仍为 `1b40f42a…`、4 张差异）。
- **踩过的坑 / 两点纪律**：
  1. **别在 `RuleCore.cs` 中间插行** —— 我先插了 2 行注释，立刻意识到会把 >2766 的账落点整体 +2，已改成「就地改写一行现有注释」（零行数变化）。
  2. `ManaManager__SetAccumulationMana` / `__SetQuestPoints` 这类 **`ManaManager.*` 尾跳**要读**它自己**的方法体再判档 —— 两处都是纯 `SetActive`/刷数字，照 `PlayerManager` 那一层的名字猜会把 C 判成 B。

---

## 六 白名单与「我动过什么」（可对账）

| 文件 | 性质 | 行数变化 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/PlayerState.cs` | 类尾追加 4 个写入口（+58 行）+ `ManaCarry` 的 doc 就地订正（**零行数变化**）；旧行一字未改、行尾仍 **LF** | 438 → **496** |
| `Unity/MyGame/Assets/RuleEngine/Core/RuleCore.cs` | **6 处单行替换**（294 / 1640 / 1776 / 1852 / 2766 / 12312）+ **1 处注释就地改写**（2762）；行尾仍 **LF** | **14,971 → 14,971（不变）** |
| `Unity/资料/完全复刻/账/_parts/B9_W15MN.tsv` | 新建（11 列表头 + 18 行） | 19 行 |
| `资料/完全复刻/报告/B9_W15MN_RuleCore十八条mana.md` | 本文件 | — |

**没动**：`EffectResolver.cs` / 其余 `RuleEngine/Core/*.cs` / `RuleEngine/Editor/**` · `CardPresentation/**` · `Shell/**` · `工具/ruleprobe/**` · 主账表 · `CLAUDE.md` / `项目任务.md` · `PinnedParseMd5` / `PinnedPlayMd5` · git · Unity 宿主。
⚠️ `git diff --numstat` 在多代理并行时对 `RuleCore.cs` 不可用（工作树里有别人大量未提交改动）；本批的改动以本报告 §六 那 7 个行号为准，行尾用二进制核过。
