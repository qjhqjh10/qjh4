# B88_A1 · 战术卡 oath「平 / 誓二选一」—— 施工报告

> **代理**：A1（执行代理 · 第八十八会话第二波）
> **范围**：把「战术卡 `Oath N:` 的二选一」整条做完（引擎 + 面板 + 断言 + 账补丁）
> **判据**：`资料/完全复刻/判据/B88_R3_oath二选一_判据.md`（六节，未重查）
> **没跑 Unity**（由调度台串行调度）· **没动 git** · **没改正本**

---

## 一、做了什么（ask 的形状）

**ask = 一个布尔「付不付誓约」**（判据 §五·0 口径 A）：

| 层 | 落点 | 做什么 |
|---|---|---|
| 判据 | `RuleCore.OathChoiceNeeded(CardDef)` | `ChoicesTargetsAffected == 180` ∧ `ChoicesSpecificCardIds.Count == 2` ∧ `HasOathTrait` ∧ **非单位卡** |
| 注入 | `RuleCore.PlayerChooseOps(ctx, owner, card)` | 上面为真时**追加一条** `Verb = "chooseoath"` 的 ask（⛔ 一参重载一个字没动） |
| 可付性 | `RuleCore.CanPayOathChoice(ctx, p, card, inst)` | `CostOf + OathCost <= Energy`（= 原版 `CanPlayWithOathNowInHand.c:31-36`） |
| 决策 | `RuleCore.DecideOathChoice(ctx, p, card, inst)` | 四档：非二选一卡 ⇒ `true`（行为不变）· 池闸判否（非本方回合 / jam）⇒ `false` · 付得起 ⇒ 取答案 / 没人答则「付得起就付」· 付不起 ⇒ `false` |
| 摘组 | `RuleCore.WithoutOathPaidGroup(ops)` | 平版：把 `CostKindOf == "oath" && Cost > 0` 的整组 op 摘掉（解析层 `StampPaidCost` 给整组每一条都打了同一 `CostKind`） |
| 面板 | `BattleDriver.ShowAsk` 的 `chooseoath` 分支 | 摆 **2 张**合成卡（平版正文 / 整条 desc），插图都用源卡（`MakeChoiceCard` 的 `id = source.Name`）；⛔ **没重建 UI**（复用 `ChoosePanel`） |
| 答案 | `BattleContext.SetOathAnswer / TakeOathAnswer` | **带座位号、一次一取**；表现层只在该问时写，引擎只在池闸成立时读 |
| 扣费时机 | `PlayTacticCore` | 先 `SpendNormalMana(卡费)`（原样），誓约组仍在正文结算时按组收 ⇒ **基础费在选择之前、誓约费在选择之后**（判据 §三） |

**为什么是布尔而不是两个 `CardDef`**：我们池里是**一张合并卡**（oath 效果并进 `desc`，两个变体 id
`choicesSpecificCardIds` 只是元数据、都不在池内）⇒ 照原版「选变体 SO」会动 `cards_engine` / 卡号 / 池口径
（判据 §5·0 口径 B）。已在代码里打 `🔶 自己设计（非原版）`（`BattleContext` 答案格 · `EffectResolver` 决策块）。

---

## 二、改了什么（文件级）

| 文件 | 改动 |
|---|---|
| `RuleEngine/Core/EffectResolver.cs` | +5 处：`PlayerChooseOps`(三参) 注入 · `CanPlayTactic` 加 `bool? oathChosen` + 誓约费闸 · `PlayTacticCore` 先决策再校验 · 正文入队时 `usedOps`（平版摘组）· **类尾**新增 `OathChoiceNeeded` / `CanPayOathChoice` / `DecideOathChoice` / `WithoutOathPaidGroup` / `OathPlainText` + 一个展示用正则 |
| `RuleEngine/Core/BattleContext.cs` | **类尾**新增答案格（4 个成员 + `SetOathAnswer` / `TakeOathAnswer` / `HasOathAnswer` / `ClearOathAnswer`），带座位号 |
| `CardPresentation/Battle/BattleDriver.cs` | `BeginPlay` 每次出牌先清答案 · `ShowAsk` 新增 `chooseoath` 分支 · `OnChooseDone` 把「平/誓」写进新答案格（⛔ 不进 `ChoosePicks`） |
| `RuleEngine/Editor/RuleEngineTest.cs` | 注册 + 新增 `TestTacticOathChoice`（9 组断言） |
| `CardPresentation/Editor/BattleScene.cs` | 新增 21-e-②·b 一节（3 组 UI 断言） |

**行号（2026-10-28 现读）**：`EffectResolver.cs:17599` 三参 `PlayerChooseOps` · `:17656` `OathChoiceNeeded` ·
`:17671` `CanPayOathChoice` · `:17689` `DecideOathChoice` · `:17734` `WithoutOathPaidGroup` · `:17750` `OathPlainText` ·
`:2009` 誓约费闸 · `:2524` `PlayTacticCore` 决策 · `:2706` 平版摘组 ·
`BattleContext.cs:2046-2076` 答案格 · `BattleDriver.cs:11087 / 11297 / 11350` ·
`RuleEngineTest.cs:148` 注册 · `:7653` 起 `TestTacticOathChoice` · `BattleScene.cs:29962` 起新节。

⚠️ **行号漂移**：`EffectResolver.cs` 插了新成员 ⇒ **该文件里所有 `:xxxxx` 落点整体后移**（本波 +190 行量级）；
`BattleDriver.cs` / `BattleScene.cs` / `RuleEngineTest.cs` 同理。**调度台在同步点跑 `check_ledger_points --all-A --fix`** 时请一并重定位
（本代理没跑 —— 那条只能在「没有 `.cs` 写手 + 补丁已合并」时跑）。
⚠️ 顺手核过：`EffectResolver.cs` 的 **CRLF 在一次编辑里被翻成 LF**（`git diff` 从 ~200 行变成 35,324 行），
已用**二进制** `b.replace(b'\n', b'\r\n')` 还原（现读 `crlf == lf == 17763`、裸 CR 0）。**同族坑，后面的人注意。**

---

## 三、证据（逐条，文件:行号）

1. **闸只在「原版会问」时问**：`NeedsToChooseFromPool`（`EffectResolver.cs:17488`）判否 ⇒ 不注入；判据 = 原版 `NeedsToChooseFromPool.c:1-153` 五道闸。
2. **可付性判据**：原版 `CardScript__CanPlayWithOathNowInHand.c:31-36` = `CurrentCost + oathValue <= ManaLeft`；
   我们的 `CanPayOathChoice` 用 `RuleCore.CostOf(ctx,p,inst)`（带实例折扣）+ `GetDefaultTraitValue(oath)`。
3. **扣费先后**：判据 §三·2 表 —— `PayCardCost`（`_d__447:377`）在 `NeedsToChooseFromPool`（`:498`）之前；
   誓约费在被选中变体的 ability 结算里收（`AbilityLogic__PlayAbility.c:2031-2033`）。我们的实现与之同形
   （`PlayTacticCore` 的 `SpendNormalMana` 保持原位，誓约组由 `ChargePaidGroups` 在正文里按组收）。
4. **UI 不重建**：`ChoosePanel`（`Battle/ChoosePanel.cs`）已逐项复刻 `ChooseCardMenu`（判据 §二·5）；
   本次只新增一个 `ShowAsk` 分支，标题/钮/排布全部沿用；标题走 `ResolveTitle(uniqueId=card.Id)`（原版同一回落链）。
5. **摘组判据**：`EffectText.StampPaidCost`（`EffectText.cs:2874-2884`）给整段每条 op 打 `Cost/CostKind/CostShared`
   ⇒ `CostKindOf == "oath" && Cost > 0` 一次就能摘干净（含跨句承接的 `CostShared`）。
6. **这 10 张合并卡的 oath 半句都不是「定目标」那一句**（`Adaptive Strategy` / `Angel's Wrath` / `Catechism of Death` /
   `Champions of Humanity` / `Fall Back` / `Indomitus Crusade` / `No Mercy` / `No Respite` / `Shall Know No Fear` /
   `Stormhawk Interception` —— 现读 `cards_engine.json` 10 张，`choicesTargetsAffected` 全 180、`choicesSpecificCardIds` 全 2）
   ⇒ 摘组放在「入队那一刻」不影响 `PickSide` / 目标合法性。

---

## 四、断言清单

**`RuleEngineTest.Run`（`TestTacticOathChoice`，`RuleEngineTest.cs:7653` 起）**
1. ① 判据：`Adaptive Strategy` / `Fall Back` = 二选一；反例 `The Fang`（ta180 无 oath）= 否；`null` 不炸。
2. ② 纯函数：`WithoutOathPaidGroup` 真摘且摘干净；`OathPlainText` == `Give +1 [Attack] to all friendly troops.`
3. ③ 答**誓约** ⇒ 能量 2→0（卡费 1 + 誓约 1）、友军 +1 攻击 **且 +1 生命**、答案被取走。
4. ④ 答**平版** ⇒ 能量 2→1（只卡费）、+1 攻击、**生命不动**。
5. ⑤ **没人回答** ⇒ 照旧「付得起就付」（2→0、生命 +1）—— 旧行为不变。
6. ⑥ 余额只够卡费 ⇒ `CanPayOathChoice` 假、照样打得出去（平版）、只扣卡费、残答**被作废**。
7. ⑦ `CanPlayTactic`：`oathChosen=true` + 余额不足 ⇒ `ErrCost`；`false` / `null` ⇒ `OK`；够 ⇒ `OK`。
8. ⑧ 注入条件：本方回合 ⇒ 正好 1 条 `chooseoath`；一参版 = 0；非本方回合 ⇒ 0；`The Fang` 仍 1 条 `chooseone`。
9. ⑨ 全池普查：二选一卡 > 0 且 ≤ 「带 oath 的非单位卡」张数（防误扩）。

**`BattleScene.Run`（`BattleScene.cs:29962` 起）**
6. `ChooseOptionCount == 2`（付得起时摆两张）
7. `panel.TitleText == ChoosePanel.DefaultTitle`
8. (a) 点第 2 张（誓约）⇒ 能量 −(卡费+誓约)；`ChoosePicks`/`ChooseCardIds` **空**（⛔ 不进那两格）；
   (b) 点第 1 张（平版）⇒ 只 −卡费；
   (c) 付不起誓约 ⇒ **面板不弹**、只 −卡费、日志出声「付不起誓约」。

**该跑**：`RuleEngineTest.Run` + `BattleScene.Run`（两条都改了对应宿主）。⛔ 本代理没跑 Unity。

**已跑的离线旁证**：
- `bash 工具/typecheck.sh` → **0 / 0**（多次，最后一次在全部改动之后）。
- `bash 工具/ruleprobe.sh play UM109` → rc=0，读数：能量 20→18（卡费 1 + 誓约 1）、`FIX_A 2/0/0/2 → 3/0/0/3`（+1 攻击 +1 生命）。
- `bash 工具/ruleprobe.sh play check` → **rc=0**，主档 1129 / 电池 586 **逐卡 0 差异**、哨兵过
  ⇒ **「没人回答」这条默认路与我们改前逐字节一致**（这条最要紧：AI / 录像回放 / `SimulatePlay` 全走它）。

---

## 五、没查清 / 已知缺口（如实）

1. 🔴 **P2P 与录像回放拿不到这个布尔**：`AiAction`（`RuleEngine/Data/SimpleAI.cs:76`）没有字段能带它，
   而 `LocalAct` 只把 `ChoosePicks` / `ChooseCardIds` 快照进动作（`BattleDriver.cs:297` 的 `CaptureLocalAnswers` / `:809` 的 `RecAct`）。
   ⇒ 若本机玩家选**平版**，远端 / 回放会按「付得起就付」重演 ⇒ **能量分叉、`NetProtocol.Fingerprint` 不等**。
   **修法要动 `AiAction` + `NetProtocol` + `NetApply`（本波白名单外）** ⇒ 请调度台另立账（P1，
   断言：一局 oath 战术局两端指纹相等）。⚠️ 单机 / AI 对局 / `play check` **不受影响**（默认路逐字节不变）。
2. **AI 的「选哪一版」语义未查**：原版有 `BattleManager.ChoiceOfCardEnemy`（账里那行状态 B），我们让 AI 走
   「付得起就付」。这次**没有改变** AI 行为（改前也是自动按组付费），但**离原版 AI 的挑法**仍是未知。
3. **两个变体 SO 的卡面差异未读**（判据 §六 #8）：平版选项的正文是我们用「desc 去掉 `Oath N:` 那句」拼的；
   原版平版 SO 的 `desc` 是否只到前半句，**没逐字核**。
4. **`HideChooseButton` / `ContinueButton` 图标资源名仍未知**（判据 §六 #3）—— 与本波无关（面板照旧）。

---

## 六、顺手发现（调度台请分流）

1. 🔴 **`NeedsToChooseFromPool` 的「法力够不够付誓约」那一闸在我们这条路上少减一个卡费**：
   原版它在**基础费已扣之后**判（`_d__447:377` → `:498`），而 `PlayerChooseOps(ctx,…)` 是在**出牌之前**被
   `BattleDriver.PlayAsks` 调的（`BattleDriver.cs:10974`）⇒ 那一格读的是**扣费前**的能量。
   ⇒ 症状：`Energy == 卡费` 时闸仍判「够」（`Energy >= oathValue`）⇒ 会**摆一个付不起的 oath 选项**。
   本波在**两处**兜住了它（`CanPayOathChoice` 用 `卡费 + 誓约 <= Energy`；`ShowAsk` 在不付得起时不摆选项），
   **没有去改那条闸本身**（改了会影响 `ruleprobe` / 别的调用点）。**建议另立账**：把闸的 mana 那半换成
   「扣费后余额」口径（要么给它 handIdx，要么让调用点先减 `CostOf`），并补一条「Energy == 卡费 ⇒ 闸判否」的断言。
2. **`RuleCore.PlayerChooseOps` 的三参重载现在是「闸 + 注入」两个职责**（原来只有「闸」）。若将来还有第二种
   需要注入的 ask，建议把注入抽成 `InjectedAsks(ctx, owner, card)`，别让这个重载继续长。
3. **`EffectResolver.cs` 的 CRLF** 会在某些编辑路径上被整篇翻成 LF（本波踩到一次，`git diff` 一度 35,324 行）。
   `typecheck.sh` 与编译器都不报，只有 `git diff --numstat` / `b.count(b'\r\n')` 看得见 ⇒ 建议把这个判据写进交接。

---

## 七、账补丁

`账/_parts/B88_A1.tsv`（2 行，11 列，LF）：

1. `CardScript.CanPlayWithOathNowInHand()` — **B → A**（引擎侧 predicate 已补：`CanPayOathChoice` + `DecideOathChoice`，
   回合 / jam 两闸仍只走 `NeedsToChooseFromPool`，⛔ 没复制第二份判据）。
2. `CardScript.NeedsToChooseFromPool(CardScript targetCard)` — 状态不变（A），**差异列补「消费端接上了」**
   （这条闸原先「有闸没人问」），并把三个落点行号刷到 2026-10-28 现读。

---

# 第二批（`A1b`）：`ta=50` 敌方手牌计数 + 原版 **80 档**「乘数折进数值」

> 判据：`判据/B88_R2_80档_VA判定.md`（80 档 · VA 级已核）· `判据/B88_R1_90档_逐卡施工单.md`（90 档）
> 白名单追加：`CountEval.cs` · `工具/gen_cards_engine.py` · `BattleContextRandom.cs`

## 一、做了什么（活 1 = 80 档 + ta=50，**同批**）

| 文件 | 改什么 |
|---|---|
| `RuleEngine/Core/CountEval.cs` | **`ta == 50`（手牌）分支落地**（`:643`）：按 `friendlyUnits/enemyUnits` 定侧，数该侧手牌里 `includeMinions`=单位 / `includeSpells`=战术；新增支持面闸 `HandSetSupported`（`:1055`）——其余维度（keyword/trait/四组 stat/`filterSpecificCards`/`cond`/`maxTargets` 截断/自他/英雄·武器·密语·残骸）**一律 `null`+`gap`**（⛔ 不按「大概是」补） |
| `RuleEngine/Core/EffectResolver.cs` | `TryStructuredMultiplier` 里加 **80 档乘数折进数值**：`IsMultiplierFoldedIntoValue`（`:17789`）为真 ⇒ `n = 1` + 出声（`:14059`） |
| `RuleEngine/Editor/RuleEngineTest.cs` | 新增 `TestB88PlayerHandMultiplier`（`:7823`，5 条断言） |

**公式**（判据 §一）：`minValue + maxValue × multiplierFactor`（VA `0x18081b728 imul` / `0x18081b731 lea`），⛔ 不是 `(min+max)×mult`；伤害**只结算 1 次**（`PlayAbility.c:1504`）。

🔴 **两条必须同批的理由（就是本波的核心雷）**：只补 `ta=50` 计数、不改 80 档 ⇒ `GSC35 Benefictus`
的伤害从 `N` 变成 **`N × N`**（夹具 N=3 时 9 点）。本波实测：`play GSC35` **改前 3 / 改后 3**，
日志出「80 档 … 计数 = 3」⇒ 计数真的数了、且**没有**被当循环遍数。

## 二、`IsMultiplierFoldedIntoValue` 为什么这样写（🔶 自己设计，已标在代码里）

原版判据是 `damage.valueOptions == 80`。**我们运行期没有这一位**：
`CardDef.ConditionSpec` 的十种 role（`CardDef.cs:3579-3583`）里没有 `damage`；`ActiveAbilityDamageSpec`
（`CardDef.cs:3143`）只覆盖 `activeAbilityLogic`。补这一位要动 `CardDef.cs` / `CardDatabase.Parse`
（本波白名单外）⇒ 用**等价形状**认：**乘数的池（`ta == 50` 手牌）就是这条 op 数值的来源
（`AmountRef == "enemyhand"`）** ⇒ 那个数只被用一次（折进数值），不会再当遍数乘第二遍。
命中面 = 现池 2 张（`GSC35` / `GSC59`），其它 `mult` 行不会命中。

## 三、验证（离线，全部现跑）

| 命令 | 结果 |
|---|---|
| `typecheck.sh` | **0 / 0**（全部改动之后） |
| `ruleprobe play GSC35` | 伤害 **3**（改前 3）；日志「80 档 … 计数 = 3」「按原版 `multiplier` 结构化数据结算 **1** 遍」 |
| `ruleprobe play GSC59` | 伤害 **12**（与改前同；它的 damage op 不走这条 mult 行 ⇒ 无日志） |
| `ruleprobe play check` | **rc=0 · 主档 1129 / 电池 586 逐卡 0 差异** ⇒ **全池无行为变化**（随机流一格没动 ⇒ 级联面 = 0） |
| `ruleprobe check`（解析层） | rc=0（没动解析） |

**该跑 Unity**：`RuleEngineTest.Run`（动了 `RuleEngine/Core` ⇒ 铁律 12 必跑；本波 +2 个 Step = 14 条新断言）。
`BattleScene.Run` 不因本波必跑（只动引擎结算），但第一批改了它 ⇒ 一并跑。

## 四、活 2（`90 RandomizedOnResolution` 逐目标掷）—— **未做，blocker 已定位**

**卡点不在掷骰那一层，在数据面**：`DoDeal` 要按 `op` 的 `valueOptions/damageType` 分流，
而运行期**根本没有这两位**（同上：`CardDef.ConditionSpec` 无 `damage` role，58/65 行在 `abilities[]`）。
判据文件 §三·4 给的「推荐 A」= 扩 `工具/gen_cards_engine.py`（白名单内）**＋** `CardDef.ApplyActiveAbilityLogic`
旁挂 `op.RollPerTarget`（**`CardDef.cs` 白名单外**）⇒ 半步做不了；「只看 `AmountMax > Amount`」的近似被判据
明令禁止（会把 `GOF22` 这类 `(0,5)` 的 `Normal` 区间卡一起改成逐目标 = 静默错）。
⇒ **本波零改动**（随机流一格没动，所以也没有级联），请调度台把 `CardDef.cs` + `RuleEngine/Data/CardDatabase.cs`
放进白名单（并预留 `PinnedParseMd5` / 卡表版本 bump + `PinnedPlayMd5` 重钉 + §四 的 A/B 隔离）。
⚠️ 另：`GetAdjustedDamage` 那一行账里写着「随机区间…已落到 `ApplyDamageCore`」⇒ 真正缺的确实只是
「op 侧那两位」，与上面的判断一致。

## 五、账补丁 `B88_A1b.tsv`（3 行）

1. `AbilityLogic.GetValueFromCriteria(...)`（行 77，A 保持）—— 80 档落地（值的一半 + 不当遍数），差异化写明。
2. `AbilityLogic.GetTargets(...)`（行 76，B 保持）—— `ta=50` 手牌支从「未实现」进到「单一形状已实现」+ 支持面清单。
3. `CardScript.GetAdjustedDamage(...)`（行 748，B 保持）—— 90 档逐目标**未做 + blocker 全文**（谁接手照这条走）。

## 六、没查清（第二批）

1. `ta=50` 里**防御卡算不算 `includeSpells`**（判据 §五·1）—— 本支按「战术」字面**不算**；现池两张卡对手手牌里没防御卡时无影响。
2. `min/max` 与 `multiplierFactor` 的**数据位**仍没进 `CardDef`：值仍走 `AmountRef="enemyhand"`（= `Hand.Count`，含武器/密语/残骸），
   而原版只数「单位+战术」⇒ 若敌方手里有那几类卡，原版少算、我们全算（判据 §四·3，**未在真实手牌里验过**）。
3. 80 档的 `furtherMultiplier`（`+0x38`）这两张卡是 0（不乘）；`IsMultiplierFoldedIntoValue` 也没处理它（同族数据位缺失）。

---

# 第三批（`A1c`）：原版 **90 档** `RandomizedOnResolution` 的「逐目标掷」

> 判据：`判据/B88_R1_90档_逐卡施工单.md`（§二·3 掷点 / §三·2·4 施工面 / §四 级联警告）
> 白名单本波放开：`CardDef.cs` · `CardDatabase.cs` · `工具/gen_cards_engine.py`
> ⛔ 没碰 `RuleCore.cs` / `Aura.cs` / `UnitState.cs` / `BattleDriver.cs` / 任何 `Pinned*` 常量 / 基线

## 一、做了什么（机制 + 数据）

| 层 | 落点 | 做什么 |
|---|---|---|
| 数据 | `工具/gen_cards_engine.py` | 新增逐卡列 **`abilityDamage`** = `[{"ab","lg","min","max","opt","type"}]`（源 = `cards_raw.json` 的 `cardAbilities[].abilityLogic[].damageCriteria`）；**卡表 v35 → v36** |
| DTO | `CardDatabase.cs` | `CardDto.abilityDamage` + `AbilityDamageRowDto`（四格 `-1` = 缺键，`0` 合法）+ `MapAbilityDamage`（缺 `ab/lg` / 撞键 ⇒ 出声丢弃）；**构造后挂载** `def.AttachAbilityDamage(...)`（⛔ 不给 `CardDef` 构造加形参 —— 它有 40 处账落点） |
| 载体 | `CardDef.cs` | `AbilityDamageSpec`（类尾）+ `AttachAbilityDamage` / `AbilityDamageAt(ab, lg)` / `HasAbilityDamage` |
| 标志 | `EffectText.cs` | `EffectOp.RollPerTarget`（**类尾**，+12 行） |
| 判定 | `EffectResolver.cs` | `StampRollPerTarget`（`:17824`，**唯一判据**：`opt==90 ∧ type∈{5,10,12}` ∧ `ab/lg>=0` ∧ `AbilityMap != "attached"`）；在 `ResolveOneCore` 起手标（`payOnly` 那趟不标） |
| 结算 | `EffectResolver.cs` `DoDeal` | 掷骰从「循环外一次」挪成：`RollPerTarget && AmountMax>Amount` ⇒ **每个目标各掷一次**；日志加一句「逐目标各掷一次（min-max）」 |
| 断言 | `RuleEngineTest.cs` | `TestB88PerTargetRoll`：`AM53` 打 2 个目标 ⇒ `RandomNumbersIndex` 走 **2** 格 + 合计掉血 6..10 |

**为什么必须走数据位**：`valueOptions/damageType` 在 `abilities[].logic[].damageCriteria` 里，运行期原本**根本没带进 `CardDef`**
（`CardDef.ConditionSpec` 的十种 role 无 `damage`；`ActiveAbilityDamageSpec` 只覆盖 `activeAbilityLogic`）。
⛔ 明确**没有**用「有区间就逐目标」的近似（那会把 `GOF22` 的 `(0,5)` = `Normal` 区间卡改错，原版那里是**共用**）。

## 二、变化面（**如实：本波量化不了**）

| 读数 | 结果 | 能不能当证据 |
|---|---|---|
| `typecheck.sh` | **0 / 0** | ✅ |
| `ruleprobe check`（解析层） | rc=0 · 解析差异 **0** 行 | ✅（新增列不进解析器） |
| `ruleprobe play all dump`（改前 `_tmp_view/a1b_before_play.tsv` / 改后 `_tmp_view/a1c_after_play.tsv`） | **逐字节 0 差异** | 🔴 **不能** —— 见下 |
| `ruleprobe play check` | rc=0 · 主档/电池 0 差异 | 🔴 **盲绿** |

🔴 **为什么是盲绿**：`ruleprobe` 的 `LoadPool` 走 **`System.Text.Json` 手读**（`工具/ruleprobe/Program.cs:1021+`，
注释自己写着「不经过 `Data/CardDatabase.cs`」），它按**列名逐个手映射**进 `CardDef` 构造 ⇒ **看不见新增的 `abilityDamage`**。
所以「dump 前后相同」= 探针根本没进那条分支，**不是**「行为没变」。这正是 R1 §四 提醒的「别拿假绿当结论」。

**静态面（现读卡表 v36，口径写明）**：`abilityDamage` 里 `opt == 90` 的行 = **74 行 / 53 张**（全部 `max > min`）；
按 R1 §一·a，其中**多目标行 16 行 / 14 张**才会产生可观测差（其余是单目标 ⇒ 掷 1 次 = 与改前同分布）。
⚠️ 另外只有「我们 op 上**真的**有区间（`AmountMax > Amount`，来自卡面文字）」的那些才会真的多掷；
文字没印数字的行在本波**退化**成不掷（= 与改前同）。

**要真量化，二选一（请调度台裁）**：
- **(a)** 授权改 `工具/ruleprobe/Program.cs`（≈10 行：DTO 一个字段 + 一处映射 + 构造后 `AttachAbilityDamage`）⇒ 之后 `play all dump` 就能给出逐卡变化表（含随机流整体平移）。
- **(b)** 在 Unity 侧跑 `RuleEngineTest.Run`（本波已加 `TestB88PerTargetRoll`，它就是判别式：共用一掷 ⇒ `used == 1` ⇒ 红）。

## 三、没做的一档（记录，未近似）

**主动能力那 7 行（`activeAbilityLogic`）里的多目标 3 行**（`AM12 Flamer Trooper` · `AM30 Manticore` · `SW36 Bjorn`）**没做**：
它们的 `(ab, lg)` 是 `activeAbilityLogic` 的下标空间（`op.AbilityMap == "attached"`），与 `cardAbilities` 是**两套下标**
⇒ 本波 `StampRollPerTarget` **显式跳过** `"attached"`（⛔ 不混用、不猜映射）。数据其实已经在表里
（`activeAbilityLogic[].dmg` 早就带 `{min,max,opt,type}`，见 `CardDef.ActiveAbilityDamageSpec`）⇒ 下一批只需
在 `CardDef.ApplyActiveAbilityLogic` 挂 op 的那一处顺手把 `RollPerTarget` 按同一判据标上（1 处，判据同一份）。

## 四、没查清（第三批）

1. **级联面**：随机流是「一局一条、一个游标」⇒ 多目标 90 改成逐目标之后，**同一局后续所有掷骰整体平移**。
   本波**没能**用探针量化（见上），Unity 侧的 `play all dump` 类读数也没有（⛔ 我不跑 Unity）。
2. **`_ResolveMultiDamageVariable`（`PlayAbility.c:1561-1623`，`case 0xc`）没有 `!= 0x5a` 闸**（R1 §五·3）——
   那一支里 `damageType==5` 但 `opt != 90` 会不会也逐目标掷**未定**；本波按「只认 90」实现，与 R1 §三·4 的硬约束一致。
3. **`AM23` / `GOF95` 那两处「两次伤害 / 条件替换」**（R1 §五·7）掷数次数未逐句核；本波没改它们的取值路径。
4. 生成器加列后**卡表 v36**：`CardDatabase` 没有为 v36 加「旧表出声」的版本守卫（靠「缺列 ⇒ DTO null ⇒ 整列未知」兜住）。
   若要与其他列同形，下批补一条 `< 36` 的 `LogWarning`（1 行）。

## 五、账补丁 `B88_A1c.tsv`（1 行）

`CardScript.GetAdjustedDamage(...)`（行 748，状态 B 保持）—— 差异列补：逐目标掷的**分流口 + 数据面 + 静态面 +
盲绿警告 + 未做的主动能力 3 行 + 两条量化路径**。⛔ 没动 `Pinned*`、没重钉基线。

---

# 第四批（`A1d`）：探针补 `abilityDamage`（去掉盲绿）+ 主动能力那 3 行

> 授权：调度台 2026-10-28 批 **(a)** —— 改 `工具/ruleprobe/Program.cs`；同批做掉 `AM12`/`AM30`/`SW36`。
> ⛔ 没动 `Pinned*`、**没重钉基线**。

## 一、改了什么

| 文件 | 改什么 |
|---|---|
| `工具/ruleprobe/Program.cs` | `LoadPool` 手读 DTO 新增 **`abilityDamage`** 解析（口径与 `CardDatabase.MapAbilityDamage` 逐字相同：缺列 ⇒ `null`、缺 `ab/lg`/撞键 ⇒ 出声丢弃），并把 `new CardDef(...)` 收成 `var __def = ...; __def.AttachAbilityDamage(__adRows); list.Add(__def);` |
| `CardDef.cs` | 新增 `StampActiveRollPerTarget(grp, ops)`：**主动能力**那一行走 `ActiveAbilityLogicSpec.Damage[0]`，判据与 `RuleCore.StampRollPerTarget` **同一条**（`opt==90 ∧ type∈{5,10,12}`）；只在「1 对象 ∧ 1 行」时标（多行判不出 ⇒ ⛔ 不猜）；只标 `Verb == "deal"` 的 op。挂在 `ApplyActiveAbilityLogic` 的**三个** `attached` 出口前 |
| `EffectResolver.cs` | `DoDeal` 的汇总日志改成「`3-5` 点（**逐目标各掷一次**；共 N）」—— 原写法会印出那个已无意义的共享 `dmg` |

## 二、探针「确实看见了」的证据（约束 2）

`play AM53`（`opt==90 · type 5 · min3/max5 · 两目标`）：

| | 改前（盲） | 补探针后 |
|---|---|---|
| 伤害累计 | 6（一掷 ×2） | **8** |
| 日志 | `对 2 个目标造成 3 点伤害（共 6）` | `对 2 个目标造成 3-5 点（逐目标各掷一次；共 8）` |

⇒ `RollPerTarget` 在探针侧**真为真**，不再是「探针没走那条路」的假绿。

## 三、变化面量化表（约束 3，**如实、未调小**）

基准 = `_tmp_view/a1c_after_play.tsv`（探针仍盲时的全池 dump，1129 行 × 25 列）
对照 = `_tmp_view/a1d_after_play.tsv`（探针看见之后，同一份引擎代码）

| 面 | 读数 |
|---|---|
| 主档（打出去那一拍） | **2 张变**、且**只有 `dmg` 一列** |
| `AM53 Artillery Strike` | `dmg 6 → 8`（2 目标 × 逐目标掷） |
| `GOF41 Unbridled Carnage` | `dmg 5 → 6`（同上；同一张卡里的目标选择也被额外一掷挪过：`FIX_E1` → `FIX_WARLORD`，**同卡内的随机流平移**） |
| 事件电池（586 行，`type=unit`） | **0 差异** |
| 其它 1127 张 | **0 差异** |

**为什么不是「16 行 / 14 张」全都变**（如实记，别误读）：
① 探针**逐卡独立夹具** ⇒ 随机流**不跨卡**共享 ⇒ R1 §四 担心的「整局后续整体平移」在探针里**结构上不出现**
   （真机/联机的同一局里才会）。所以这张表只能证明「哪些卡自己会变」，**不能**外推「一局里会影响多少张」。
② 只有「我们 op 上真带区间（`AmountMax > Amount`，来自卡面文字）」且**夹具里真打到 ≥2 个目标**的行才会变；
   其余多目标行在探针夹具里只命中 1 个目标（或正文没印数字 ⇒ 不掷），因此读数不变。

**`play check` 因此 红（rc=1），差异正是上面那 2 张**——基线是第 87 会话用**盲探针**钉的，重钉归调度台。
`ruleprobe check`（解析层）**rc=0 · 解析差异 0 行**。

## 四、主动能力那 3 行（`AM12` / `AM30` / `SW36`）

- **做了**：`CardDef.StampActiveRollPerTarget`（同一条判据，来源 = `activeAbilityLogic[].dmg`，那是**另一套下标空间**）
  ⇒ `op.RollPerTarget` 在挂载时就已经为真，`EffectResolver` 那条 `AbilityMap != "attached"` 的闸保持不变（⛔ 混用下标）。
- **验证到哪一层（如实）**：`play all dump` 与上一版**逐字节相同**（`diff` 0 行）——
  因为探针的 `play` **只部署、不发动主动能力** ⇒ 那 3 张的主动能力根本没跑过 ⇒ 只有**编译级 + 代码级**覆盖。
  真正的验法是 Unity 侧点那三张的主动技（本波的 `TestB88PerTargetRoll` 只覆盖 `cardAbilities` 那一路）。

## 五、该跑哪条 / 期望什么（调度台照单跑）

| 宿主 | 为什么 | 期望 |
|---|---|---|
| **`RuleEngineTest.Run`** | 动了 `RuleEngine/Core`（`EffectResolver` / `CardDef` / `EffectText`）+ 卡表 v36 | 我加的 3 个 Step 全绿：`TestTacticOathChoice`(9) · `TestB88PlayerHandMultiplier`(5) · `TestB88PerTargetRoll`(3)。⚠️ 若 `TestB88PerTargetRoll` 红在 `used == 1` ⇒ 逐目标掷没生效 |
| **`BattleScene.Run`** | 第一批改了 `BattleDriver`/`BattleScene` | 21-e-②·b 那一节 3 组全绿 |
| `ruleprobe check` | 解析层 | **rc=0 · 解析差异 0 行**（已跑） |
| `ruleprobe play check` | 结算层 | **rc=1**，差异**只有** `AM53`/`GOF41` 的 `dmg` —— 重钉基线后应变绿（重钉后请再跑一次确认只剩这 2 张） |
| ⛔ 不用跑 | `Net*` / `Shell*` / 其它 | 本波没碰 `Net/*` 与外壳 |

## 六、没查清（第四批）

1. **真机/联机的级联面仍未知**：探针逐卡独立夹具，量不出一局内「同一条随机流」的平移面。要量化必须在 Unity 里跑一局含多张 90 卡的完整对局并比终局哈希（或等 `NetBattleTest` 那类）。
2. **主动能力 3 行的运行时行为未验证**（见 §四）。
3. `GOF41` 的目标选择被额外一掷挪过 ⇒ 若它的 `maxTargets` 真是 1（R1 §一·a 表的推断），那是**我们既有打宽**的问题（与 90 档无关），本波没改；记着别把它当本波回归。
