# B117-FIXG：`NetBattleTest` 誓约兜底 2 红 · `RuleEngineTest` dropPod 1 红

> 执行代理 `B117-FIXG` · 2026-11-02 · 宿主现读 = `_tmp_view/netbattle.log`（Nov 2 14:47）/ `_tmp_view/ruleengine.log`（Nov 2 14:35）
> ⛔ 本文只写判据与结论；**状态见 `活状态表.md`**。

---

## 一、`NetBattleTest` R3-N6 / R3-N6b（实际 19，应为 18）—— **判据过期（断言写死了随机结果）**

### 1.1 现读症状

```
✗ ★ R3-N6：没带答案 ⇒ 按原兜底**付誓约**（= 加这一格之前的行为，逐字不变）（实际 19，应为 18）
✗ ★ R3-N6b：对面同一条兜底 ⇒ 两端一致（实际 19，应为 18）
✓ ★ R3-N6a：没答 ⇒ 线上 `oathRecorded == false`
✓ ★ R3-N6c：两端指纹一致
```
`19 = 20（塞进去的能量）− 1（`Adaptive Strategy`/`UM109` 的卡费）` ⇒ **只扣了卡费、没扣誓约 1**。
同一次的邻居全绿：`R3-N5` 誓约版 20→18、`R3-N5b` 对面也 18 ⇒ **oath 机制本身没坏，坏的只是"没答案"那一支的期望值**。

### 1.2 真因（不是本波引擎改动引入的回归）

| 时间 | `DecideOathChoice` 的"没人回答"支 | 出处 |
|---|---|---|
| 第八十八会话 `B88`（断言写下的那一刻） | **确定性 `return true`**（= 付得起就付） | `git show 75260501:…/EffectResolver.cs:18033-18038`：日志原话「按原版口径**付得起就付**（照旧结算誓约版）」 |
| 第一百一十一会话 `B111_E1B`（2026-11-01） | **改为掷一次**：`var rolledPay = RollOathPay(ctx, card)`，无人作答就用它 | `Core/EffectResolver.cs:18076-18107`（现读）；判据 = `decomp_full/BattleManager__GetChoiceOptions.c:40-70`（bot / 面板**两条支都调**它 ⇒ 玩家手 / AI 手 / 回放落地各消耗正好 1 格预烧随机数） |

⇒ 断言 `R3-N6` 写死了「**恒付誓约**」，而 `B111_E1B` 起这一格**是随机的**（来自 `ctx.GetRandomInt(0,2)`，
`EffectResolverPartial_B111E1B.cs:65-70`）。它此前一直是绿的，靠的是「那一刻预烧随机表的游标恰好给出誓约版」——
**本波 `WMULL`（换牌期不再洗牌、洗牌挪进 `EndMulligan`，`RuleCore.cs:486-537`）改变了随机数的消费位置** ⇒ 游标一变、掷出「平版」 ⇒ 两条当场红。

**判定：断言过期**（写死了一个 50% 的随机结果），**不是实现回归**。反证三条：
1. 正例 `R3-N5/N5b` 同一张卡同一个座位，答案 = 1 时照旧 20→18 ⇒ 誓约扣费链没坏；
2. 两端同一条兜底给出**同一个** 19，且 `R3-N6c` 指纹一致 ⇒ 两端随机表同步、没有分叉；
3. `B111_E1B` 的判据是**原版 `.c`**（`GetChoiceOptions` 两条支都抽数），不是我们自定口径 ⇒ 现行为才是复刻。

### 1.3 改了什么

`Unity/MyGame/Assets/CardPresentation/Editor/NetBattleTest.cs`（**只动断言，不动实现**）：

- `R3-N6`：`eB3 - Cost - oathVal`（写死付）→ `eB3 - Cost - (LastOathPaid(actCtx, a) ? oathVal : 0)`
  ⇒ 钉「**按引擎这次真正用了的那一张结算**」。
- `R3-N6b`：期望值同样改用 `actCtx` 的结论（对面只要偏离这一张就红 ⇒ 两端一致的语义仍在）。
- 出牌前加 `actCtx.ClearOathDecisionRecord(); otherCtx.ClearOathDecisionRecord();`（同 `SimpleAIPartial_B111E2.cs:155`
  的生产口径：残答不许报到下一手）。
- 新增私有辅助 `static bool LastOathPaid(BattleContext, int)`（**追加在类尾**）= `TryPeekLastOathDecision(ctx, out pay) && pay`；
  写点 = `DecideOathChoice` 的 `NoteOathDecision`（`Core/BattleContextPartial_B111E1B.cs:30`）。
- 就地订正那条过期注释（原写「兜底『付得起就付』= 加这一格之前逐字相同」）。

### 1.4 为什么 `LastOathPaid` 不可能是"读不到所以恒 false"

同一张卡 + 同一座位在这个自检里**三连打**：`R3-N5`（答案 1）已经证明 `OathChoiceNeeded` / `NeedsToChooseFromPool` /
`CanPayOathChoice` 三道闸全过、`DecideOathChoice` 走到了 `NoteOathDecision`。负例只少一个答案 ⇒ 必进 `CanPayOathChoice`
那一支 ⇒ 一定写结论。⇒ 期望值来自**真掷出来的那一张**，不是兜底的 false。

---

## 二、`RuleEngineTest` D3⁗（督军构造时 dropPod 血量 0，期望 2）—— **夹具没挂原版 `cardType`**

### 2.1 三条猜想的逐条排除

| 猜想 | 结论 | 证据 |
|---|---|---|
| ① `SetupDropPodStats()` **没被调用** | ❌ 排除 | 它在 `UnitState` 的**主构造**里：`Core/UnitState.cs:931`（`UnitState(CardInstance, bool)`，`:881` 起，**无条件**执行），`new UnitState(card,false)` 走 `:940` 转调它 |
| ② 夹具的 `dropPod` 不在 `keywords` 也不在 `cardTraits` | ❌ 排除 | `D0`/`D0′` 同一批夹具里 `HasCurrentTrait(DropPod)` = 真/假，**全绿**（`_tmp_view/ruleengine.log:1009205/1009220`）；`HasCurrentTrait` 先读 `_keywords`（`UnitState.cs:4386`），构造时已在 `:893-897` 填好 |
| ③ `HasCurrentTrait` 在"刚构造、还没 `AttachCardTraits`"时读不到 | ❌ 排除 | 同上：`_keywords` 在构造最前面就填了，`HasCurrentTrait` 的第一步就是 `Has()`；`D0` 就在构造后立刻读，绿 |

### 2.2 真因（第四条，也是唯一剩下的那条）

`SetupDropPodStats()` **第一道闸**是 `Card.CardType != CardDef.CardTypeHero` ⇒ `return;`（`UnitState.cs:3907`），
而 **`CardDef.CardType` 缺省 = `GateUnknown(-1)`**（`CardDef.cs:6613/6617`，只有 `CardDatabase.Parse`
或自检自己 `AttachCardType(...)` 才写它）。测试夹具 `Hero(...)` 只把 `subtype: "Warlord"` 传进了 `CardDef` 构造，
**没有** `AttachCardType(CardDef.CardTypeHero)` ⇒ `CardType == -1 ≠ 10` ⇒ **在 cardType 那道闸上就返回了**，
`HasCurrentTrait(DropPod)` 根本轮不到。

> ⇒ 2026-11-02 那次 `Has` → `HasCurrentTrait` 的改动**方向是对的**，但**不是这一条的根因**。
> 同族现成样板：`CardPresentation/Editor/B97W22_Assert.cs:93` 的 `Hero()` 夹具就是 `c.AttachCardType(CardDef.CardTypeHero)`。

### 2.3 改了什么

`Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest_B117DROPPOD.cs` 的夹具工厂（**只动夹具，不动实现**）：

```csharp
static UnitState Make(CardDef card) { if (card.CardType < 0) card.AttachCardType(card.Subtype == "Warlord" ? CardDef.CardTypeHero : CardDef.CardTypeMinion); return new UnitState(card, false); }
```

- 督军夹具（`subtype == "Warlord"`）⇒ `CardTypeHero(10)` ⇒ `SetupDropPodStats` 走完 ⇒ 构造时 2/0。
- 普通单位夹具 ⇒ `CardTypeMinion(0)`（原来是 `-1`）⇒ `D3/D3′`（"非督军构造后仍是 0"）现在**真正在测 `cardType==10` 那道闸**，
  而不是"因为 -1 所以也返回了"。
- ⚠️ `SetupDropPodStats` 本身**一个字没改**（`UnitState.cs:3905-3916`，含上一手 `HasCurrentTrait` 的修正）。

---

## 三、验证

- `TMPDIR=/tmp/wf_fixg bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ⛔ 没有跑 Unity 批处理（调度台统一跑）。预期转绿：
  - `NetBattleTest`：`R3-N6`、`R3-N6b`（另 `R3-N6a/N6c` 本来就是绿的）。
  - `RuleEngineTest`：`B117DROPPOD D3⁗`（`D3⁵`、`D3/D3′/D3″/D3‴`、`W1-W4″` 不受影响）。
- 仍红：**未知**（本次没跑宿主）；若 `NetBattleTest` 还有别的红，与本改无关（本次只碰了 R3-N6 两行 + 一个类尾辅助）。
