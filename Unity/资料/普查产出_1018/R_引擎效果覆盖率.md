# R · 引擎效果覆盖率（现读 · 2026-10-18）

> 只读代理产出（无写文件工具 ⇒ 主对话原样落盘）。
> 现读输入：`cards_engine.json`（count=1127）· `allcards_1350/cards_raw.json`（1351）·
> `工具/ruleprobe/基线/out_baseline.txt`（10-18 07:04）· `_tmp_view/willrun_mechanism.md`（10-18 13:17）。

## 1 一句话结论

「解析缺口归零」只在**收漏**这一列成立（0 段），**不等于「所有效果都实现」**：全池 1127 张里主解析器
**1015 张**至少 1 条 op（90.1%）、执行层 `WillRunOps` **970 张**非空（86.1%），**63 张执行层「未归因」**；
SO 对拍确认 **1 条 SO-only 能力无落点（DA1 Azrael）**，另有 **11 种效果码 / 5 种 trigger** 只能靠通用机制近似。
卡面印出来的效果，本轮没有查到「完全没人接」的第二张。

## 2 三个覆盖率数字（分母均 1127）

| 口径 | 有 ≥1 条 | 分母 | 占比 | 出处 |
|---|---|---|---|---|
| 主解析器 `EffectText.Parse` 产出 op | **1015** | 1127 | **90.1%** | `out_baseline.txt` 第 5 列；`EffectText.cs:1144` |
| 执行层 `EffectText.WillRunOps` 非空 | **970** | 1127 | **86.1%** | `willrun_mechanism.md:10-14`（446+456+29+39） |
| 纯白板（执行层「本来没正文」） | **94** | 1127 | **8.3%** | 同上：93 unit + 1 hero |
| SO 真值非 0 效果码（仅 `cardAbilities`） | 1001 | 1127 | 88.8% | `allcards_1350` |
| SO 真值非 0（再加 `activeAbility`/`alternativeAbility`） | 1035 | 1127 | 91.8% | 现算；SO 白板 **92** 张 |

按类型：unit 586（parse 475 / willrun 456 / 空 130 = 93 本来没正文 + 37 未归因）· tactic 446（446/446/0）·
hero 56（55/29/27）· defence 39（39/39/0）。
交叉：SO 白板 92；两边都白约 **84**；SO 有而我们执行层空约 **73**；我们有而 SO 空约 **8**
（误差 ±11，10-14 的 layers dump 缺 11 张新卡 / 3 组重名）。

**Unknown / Partial / ErrUnimplemented（按卡数）**：
- 原始主解析器：**Unknown 51 + Partial 2 = 53 张**（44 unit + 9 hero；tactic/defence 0）。
- 经 `CardDef.HandledByOtherLayer`（`CardDef.cs:2676`）认领后：**0 张**（事件层 / 天赋 / 伴生 / 光环 /
  静态规则 / 开局上手各有 collector；`unit_desc_unparsed.txt` 现读 586/586 · 56/56 · 446/446 · 39/39）。
- `ErrUnimplemented` 是运行期返回码（`RuleCodes.cs:35`），生产站点 4 处，见 §4；卡面 `*` 走 `UnimplementedNotes`
  （`BattleDriver.cs:14287`）。卡面 `*` 现读应为 **0 张**（48 个规范化关键词全在 `KeywordTable.Implemented`）。
  **但 63 张空表卡不进 `MechanismFails` 的循环（`BattleDriver.cs:14349`）⇒ 不打 `*`，这是盲区。**

**EffectText 支持面 vs SO**：我们 **44 个 op 动词** · **71 个已实现关键词** · **25 个可路由触发** ·
**24 个带正文关键词** · **22 种 When 事件**。SO 当前池 **48 种效果码 / 56 种 trigger**（全枚举 99 种）。

两边对不上：
- 效果码 **11 种 / 88 条实例**：`doOnlyVfx`(3,10) · `autoPlayFromHand`(250,13) · `followAfterResolution`(320,24) ·
  `followIfPrev`(325,1) · `createCardInDeck`(270,7) · `setCardDeckToTop`(420,5) · `setReferenceCard`(380,3) ·
  `showCardsWindow`(470,1) · `highlightBloodThirst`(480,1) · `scriptedAttack`(235-238,9) · `triggerEffectInCard`(260,14)。
  其中 3 种/12 条是纯表现/UI；其余靠 `choosecard` action、`envelope`、`forceattack`、`triggerability` 近似。
- Trigger **5 种走通用机制**：`TurnBeforeEnd`(41,1 卡) · `UnitDeathByAbility`(180,4) · `FollowUpResolution`(410,30) ·
  `WhileInPlayVariable`(240,33) · `QuestTriggered`(490,4 卡；DA1 的 270 无落点)。

## 3 引擎执行链（6 行）

1. 卡表装载 `CardDatabase.Parse`（`CardDatabase.cs:218`）→ `new CardDef(...)`；构造里 `CollectTriggerOps`
   （`CardDef.cs:761`）调 `EffectText.Parse`（`EffectText.cs:1144`），并分流到 When/Spirit/Oath/Faith/Attacked/
   Aura/Static/Companion/Talent/StartWith 各 collector。
2. 产物存 `CardDef.Ops/TriggerOps/WillRunOps`（`EffectText.cs:1872`）；「哪些层会真跑」的唯一判据是 `WillRunOps`。
3. 出牌时机：`RuleCore.PlayCard`（`:2373/2387`）→ `EffectResolver.PlayTactic`（`:1562`）/`ResolveOps`（`:172`）；
   主动技 `UseAbility`（`:7525`）· 誓约 `UseOathAbility`（`:7706`）· 替代行动 `UseAlternative`（`:7902`）。
4. 时机钩子：`BeginTurn`（`:1028`）/`EndTurn`（`:1394`）→ `ResolveAtTurn`（`EffectResolver.cs:5229`）；
   `DeclareAttack`（`:4256`）→ `FireTriggerAt`（`:8260`）。
5. 结算：`ResolveOneCore`（`EffectResolver.cs:624`）→ `EffectDispatch`（`:843`）handler 改 `BattleContext` 并 `ctx.Emit`。
6. 表现：`BattleDriver` 事件消费（`:7259`）→ `VfxMap`（`:12611`）播动画/音效/飘字；
   卡面 `*` 走 `UnimplementedNotes`（`:14287`）→ `WillRunOps` + `OpHasMechanism`（`EffectText.cs:1872/1781`）。

## 4 ErrUnimplemented 全站点（生产代码，共 4 处）

| 站点 | 触发条件 | 属于哪类「没实现」 | 现池命中 |
|---|---|---|---|
| `EffectResolver.cs:1409` | `CanPlayTactic`：`!IsFullyParsed(desc) && !IsHandTrap` | **唯一真正的「卡效果没实现」闸** | tactic/defence 全解析 ⇒ **0 张** |
| `RuleCore.cs:7726` | `UseOathAbility` 返回 false | 「这次激活没生效」，不是未实现 | 运行时按资源 |
| `BattleDriver.cs:9494` | 教程白名单拒绝 | 教程脚本限制 | 教程局内 |
| `NetApply.cs:100` | 未知 `AiActionKind` | 联机动作枚举未接 | 正常协议不命中 |

## 5 10 张对拍（只列不一致/需裁定）

| 卡 | 我们解析 | SO 真值 | 判定 |
|---|---|---|---|
| **DA1 Azrael** | `choosecard/deck/card/decktop`（卡面第一句）+ TalentName | a0 `trigger=490(QuestTriggered) chosen=270(createCardInDeck)`，`unitsToSummon` 5 张 + `createAtPos=140` | **无落点（1 张）**：卡面没这段文字、代码也没有该路径 ⇒ 需裁定是隐藏能力还是数据残留 |
| **TL41 Overwhelming Swarm** | `lowercost/1/infantry troops` | `buffHand{costChange=-1, keywordFilter=5, untilEndOfTurn=1}` 对得上；但同 logic 另有 `buffEffect{healthChange=+3}` | **SO 与卡面冲突**；⛔ 别按 SO 补 +3 血 |

其余 8 张（AM51/DA42/SOR3/UM64/UM86/ASH30/TAU55/GSC67）核对为一致或仅表示不同。
**A1977 的 192 条「真差异」抽查 6/6 是仪器误判**（只比 `buffEffect`，漏了 `choosecard/…/deploy|hand|inccost|todeck`
的 action）⇒ 该表 128 条「我们缺」不能当未实现数用。

## 6 未实现清单（分类 + 数量）

- **A 卡面效果完全无落点：0 张**（53 条 Unknown/Partial 全被 `HandledByOtherLayer` 接手）。
- **B SO-only 能力无落点：1 条 / 1 张**（DA1）。
- **C 效果码无专用动词：11 种 / 88 条实例**（其中 3 种/12 条纯表现/UI）。
- **D Trigger 无专用钩子：5 种**（4 种有通用机制承载，`QuestTriggered` 的 DA1 落空）。
- **E ErrUnimplemented 4 站点**：只有 `CanPlayTactic` 属效果闸，现池 0 命中。

## 7 没查清

1. 63 张「未归因」卡只抽查 11 张（11/11 在主解析器有 op 且落到其它消费点）；其余 52 张未逐张核。
2. A1977 的 192 条真差异需要 action-aware 重跑才能定量；316 条「卡不在池」是旧 id join 失败。
3. `WillRunOps` 空集（157）与 SO 白板（92）的精确交集未算（84 是近似值）。
4. DA1 `trigger=490/chosen=270` 的语义没有本地化文本可核。
5. 未跑 Unity；`TAU28/TAU38` 等伴生修复晚于 07:04 基线，现解析需重跑确认。
6. SO trigger 全枚举 99 种、池内 56 种（`R_卡牌线…评估.md` 的「57 种」是旧口径）。
