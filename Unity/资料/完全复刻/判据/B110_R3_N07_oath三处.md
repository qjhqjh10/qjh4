# B110_R3 · N07 oath 布尔答案在 P2P / 录像里的三处断点

> **代理**：`B110_R3`（只读查判据 · 第一百一十会话）· **日期**：2026-10-31
> **范围**：`N07` —— 战术卡 `Oath N:` 的「平 / 誓二选一」答案**进不了 P2P、也进不了本地录像**。
> **不在范围**：oath 机制本身（已实现）· `N10`（誓约闸不少扣卡费）。
> **行号**：全部 **2026-10-31 现读**（文件在动，用之前**现读一次**）。
> ⛔ 本代理没跑 Unity、没动 git、没改任何 `.cs` / 账 / 正本。只新建本文件。

---

## 一句话结论

原版的答案**不是布尔**，是「**选中哪一个变体**」= `choicesDefinition.filterSpecificCards` 的**下标**
（池内 4 张 oath 卡都正好 2 项，所以实际取值就是 `0` / `1`），存在 `BattleManager.cardChosenInSelection(+0x2E8)`，
**跟着动作一起过网/进记录**（`BattleAction.actionValueTens` → `ChoiceOfCardEnemy(…, selectedCard)` → `fullPool[idx]`）。
我们现在这条链**三处全断**：`AiAction` 没字段、`MsgAction` 没字段、`NetApply` 不注入。
⇒ **一处修好不够，三处必须一起动**；而 **`ReplayStore` 本身不用改**（录像存的就是 `MsgAction`），
**回放侧也不要在 `ShowReplayAskForAction` 里 `SetOathAnswer`**（那是 UI 记录，不是引擎答案，理由见 §四·3）。

---

## 二、原版语义（逐句，文件:行）

### 2·1 答案是什么类型、存在哪

| # | 事实 | 出处（现读） |
|---|---|---|
| 1 | 候选池 = `RawCardScript.choicesDefinition(+0x2A0).filterSpecificCards(+0x70)`；`targetsAffected == 0xB4(180)` 时池就是这张表 | `decomp_full/BattleManager__GetChoiceFullPool.c:28-49` · `decomp_full/CardScript__NeedsToChooseFromPool.c:21-24` |
| 2 | 答案 = **池里那一项**（对象引用 `RawCardScript`），存在 **`BattleManager.cardChosenInSelection` = `+0x2E8`** | `out_il2csharp/Assembly-CSharp/BattleManager.cs:126`（字段声明）· `:8963-8965`（`ChoiceOfCardEnemy` 写）· `:9271-9272`（`ClickChosenCardDone` 写） |
| 3 | 🔴 **2026-11-01 就地订正（铁律 5 · `B111_E2` 现核）**：池里两项的顺序 **⛔ 不是** `filterSpecificCards` 顺序 —— `GetChoiceFullPool` 会 **`OrderBy(uniqueId)` 字符串升序**（`out_il2csharp/Assembly-CSharp/BattleManager.cs:9138-9154`，两条 lambda 在 `:27414-27426`）。池内 **10 张** oath 二选一卡**逐卡现算**：誓约变体在池下标 **0** 的 **7 张**、在 **1** 的 **3 张** ⇒ 照旧文「下标 1 = 誓约」写死会**反 7 张**。（~~旧文原写「顺序 = SO 的 `filterSpecificCards` 顺序」+「池内 4 张：`[0]` 不带 `oath`、`[1]` 带」~~ **已作废**） | 生成器 `工具/gen_cards_engine.py:5891-5899`（按 `filterSpecificCards` 顺序 append，0 条解不出）· 数据实测（`cards_truth.json`）：`cd053fb3…`(Fall Back 平) vs `fe6fde3d…`(Fall Back 誓 `oath 2`) · `7b703582…`(No Mercy 平) vs `6ea114bc…`(No Mercy 誓 `oath 4`) |
| 4 | ⇒ **对我们的布尔表示**：`0 = 平版 = 不付誓约`、`1 = 誓约版 = 付誓约`。与我们面板摆位**逐位相同**（`BattleDriver.ShowAsk` 先 `ChooseOath_Plain_` 再 `ChooseOath_Oath_`） | `CardPresentation/Battle/BattleDriver.cs:11350-11351` |

### 2·2 什么时候写

| 场景 | 写点 | 出处 |
|---|---|---|
| **本地玩家**按「Continue」确认 | `ClickChosenCardDone(selectedCard)` → `cardChosenInSelection = selectedCard` | `out_il2csharp/…/BattleManager.cs:9271-9272`（`BattleManager__ClickChosenCardDone.c` 同一处） |
| **对面 / AI**（没有面板） | `ChoiceOfCardEnemy(actingCard, targetCard, selectedCard)` → `fullPool[selectedCard]` → 写 `+0x2E8` | `out_il2csharp/…/BattleManager.cs:8951-8965` · `decomp_full/BattleManager__ChoiceOfCardEnemy.c:1-61` |
| 池为空 | 不写，回调直接 `0` | `out_il2csharp/…/BattleManager.cs:28804-28808`（`_ChooseCardMethod_d__449.MoveNext` 第一段） |

### 2·3 什么时候读

唯读点 = **被选中变体自己那条能力**（`chosenAbility == 0x168` = 360）：
读 `+0x2E8` → 有值且 `HasDefaultTrait(0x4FB)` ⇒ `PayCardPlayedCostOath`（付誓约）+ `OnTriggerAlt(被选变体)`。
判据：`decomp_full/AbilityLogic__PlayAbility.c:2026-2041` · `decomp_full/BattleManager__PayCardPlayedCostOath.c`。

> ⚠️ 注意：原版这个字段**不是一次一取、也不清**（每选一次覆盖一次）。我们这边的 `SetOathAnswer` / `TakeOathAnswer`
> 是**带座位 + 一次一取**（`BattleContext.cs:2048-2080`，已在 doc 里标 🔶 自己设计）—— 比原版严格，且是**已裁定**的口径
> （`B88_R3_oath二选一_判据.md` §5·0 口径 A）。**本批不改它**，只是把答案搬进动作。

### 2·4 P2P / 录像里怎么同步（**这是本批的判据核心**）

**答案跟着「动作」走，不是另开一条消息。**

1. `BattleAction.actionValueTens`（`+0x54`）就是那个下标；非玩家路径直接把它传给 `ChoiceOfCardEnemy`：
   `out_il2csharp/…/BattleManager.cs:28810-28816`（`ChoiceOfCardMethod`：`num2 = battleAction.actionValueTens` →
   `ChoiceOfCardPlayer(…, num2)` 预选 / `ChoiceOfCardEnemy(…, battleAction.actionValueTens)`）。
2. 编码侧把它**打进 `PlayerAction.pos(+0x20)` 的十位**：
   `decomp_full/BattleManager__ParsePlayerAction.c:608` = `pos = SpacesInTheBoard + <tens> * 10 + <units>`；
   解码侧 `ParseEnemyAction` 再把它拆回 `actionValueUnits` / `actionValueTens`
   （`out_il2csharp/…/BattleManager.cs:2433-2436` 等三支）。
   ⚠️ **如实**：`ParsePlayerAction.c:608` 里那两个源局部（`iStack_204` / `local_e8`）在本份 `.c` 里**从未被赋值**
   （cpp2il 丢赋值的已知形状），所以「十位 = `actionValueTens`」是**按位置 + 唯一候选**判的，**没逐条钉死**；
   但「答案是一个小整数、**塞在动作载荷里**」这一点由 1 与 608/728 两处**独立坐实**。
3. 玩家自己那一侧：UI 选完 → `GetChoiceIndexOfCard(选中变体)` → 下标（`out_il2csharp/…/BattleManager.cs:8959-8962`）
   ⇒ 同一个「下标」语义。
4. 录像（原版）= 服务端 `BattleRecordData` / `PlayerAction` 流（P3，不复刻）；**我们自己的本地录像**
   存的就是 `MsgAction`（`ReplayStore` 文件头），所以只要答案进了 `MsgAction`，录像**自动**就有了。

---

## 三、三条链现状

### 链 1 · `AiAction`（+ 录制 / 发送的中转）

| 文件:行（现读） | 现在是什么 | 缺什么 | 怎么改 |
|---|---|---|---|
| `RuleEngine/Data/SimpleAI.cs:76` | `public class AiAction`：`Kind/HandIdx/HandInst/Slot/TargetP/TargetSlot/Ranged/AltKeyword(99)/CapturedPicks(105)/CapturedPickIds(106)/Score/ManaCost` | **没有 oath 答案位** | 加两格：`bool OathChoiceRecorded` + `int OathChoice`（见 §四 步骤 1） |
| `CardPresentation/Battle/BattleDriver.cs:284` `LocalAct` | 抓 `picks`/`pickIds`（`:297-301`）→ `apply()` → `ReportUnaskedChoices()` → `_net.OnLocalAction(act)` → `RecAct` | **不抓 oath 答案** ⇒ 当局录像与发出去的那条都没有 | 在 `apply()` **之前** peek 一次写进 `act`（步骤 4） |
| `CardPresentation/Net/NetProtocol.cs:497` `ToWire` | 只拷 `kind/handIdx/handId/slot/targetP/targetSlot/ranged/altKeyword`；`ctx != null` 时顺带抓 `picks/pickIds` | **不拷 oath** | 照 `altKeyword` 同一行加两格拷贝（步骤 2） |
| `CardPresentation/Net/NetProtocol.cs:524` `FromWire` | 自检用的反方向（**必须与 `NetApply` 同语义**） | 同上 | 加两格（步骤 2） |
| `CardPresentation/Battle/BattleDriver.cs:870` `RecMsg` | **逐字段手抄**一条 `MsgAction`（`:873-879`） | 抄漏 ⇒ **对手那一半的答案会丢**（这是本批最容易漏的一处） | 加 `oathRecorded` / `oathChoice`（步骤 5） |

> 🔴 `ToWire(act, ctx:null, …)` 是**生产路径**（`NetBattle.OnLocalAction` 与 `RecAct` 都传 `null`）
> ⇒ **答案只能从 `AiAction` 上取，不能指望 `ToWire` 去读 `ctx`**。这就是链 1 非改不可的理由。

### 链 2 · `NetApply` 的 play 消息

| 文件:行（现读） | 现在是什么 | 缺什么 | 怎么改 |
|---|---|---|---|
| `CardPresentation/Net/NetProtocol.cs:93` `MsgAction` | 有 `handIdx/slot/targetP/targetSlot/ranged/altKeyword/picks/pickIds/marks/envSlot/envSO/defId/preApplied` | **没有 oath 答案** | 加 `bool oathRecorded` + `int oathChoice`（步骤 2） |
| `CardPresentation/Net/NetApply.cs:140-146` `case PlayCard` | 只 `RuleCore.PlayCard(ctx, actorSeat, m.handIdx, m.slot)` | **不 `SetOathAnswer`** ⇒ 对面/回放一律走「付得起就付」 | 落地前清一次 + 有答案就写（步骤 3） |

> ⚠️ **`handId` 是个「只写不读的死字段」**（`MsgAction` doc 自己写着全仓 0 读取点）——
> ⛔ **别拿它顶替答案**，也⛔ **别把答案塞进 `picks` 数组**（`picks` 与结算顺序一一对应，多一格会让后面每一处错位）。

### 链 3 · 回放 / 录像（`ReplayStore` + `PlayReplay`）

| 文件:行（现读） | 现在是什么 | 缺什么 | 怎么改 |
|---|---|---|---|
| `CardPresentation/Battle/ReplayStore.cs:1009` `ReplayRecord.actions` | `List<MsgAction>` —— 录像就是动作流 | —— | **不用改结构**（答案随 `MsgAction` 走，见步骤 2） |
| `CardPresentation/Battle/ReplayStore.cs:833` `ReplayAsk` | 有 `picked`（玩家点了第几张），**只作摆面板用** | —— | **不改**；它继续只当 UI 记录（步骤 4 末尾「为什么不用它」） |
| `BattleDriver.cs:1196` `ApplyReplayActionAt` | 逐条 `ApplyLoggedAction(m)` | —— | **不用改**（`ApplyLoggedAction` → `NetApply.Apply`，第 3 步注入即覆盖回放） |
| `BattleDriver.cs:38397` `ShowReplayAskForAction`（闸在 `:38382` / 批处理早退在 `:38391`） | 摆面板 + `TogglePick(e.picked)`，**不 `SetOathAnswer`** | —— | **⛔ 不要在这里补 `SetOathAnswer`**（理由见下） |

**为什么不把回放答案挂在 `ShowReplayAskForAction`（G5A 简报里的第 ② 项）**：

1. **两个真相源**：`ReplayAsk.picked`（UI 记录）与 `MsgAction.oathChoice`（引擎答案）会各自演化，迟早不一致；
2. **它有闸**：`Application.isBatchMode` ⇒ 整段不摆（`BattleDriver.cs:38388` 一带）；`_replayPaused` ⇒ 不推进；
   候选全查不到（`n == 0`）⇒ 跳过。**引擎答案不能依赖「面板摆没摆出来」**；
3. **活局没有 `ReplayAsk`**，P2P 更没有 ⇒ 那条路对链 2 一点用都没有，还得再写第二份注入逻辑
   （`CLAUDE.md` §三「两处写同一条规则 = 迟早不一致」）。
4. 反过来说：`ReplayAsk.picked` 保留下来正好当**交叉校验**（见 §四 步骤 9 的可选断言 R3-N5）。

---

## 四、施工单（转岗后照这个做）

> **白名单**（转岗时以调度台发的为准；下面是本施工单**必需**的最小集）：
> ✅ `RuleEngine/Data/SimpleAI.cs` · ✅ `RuleEngine/Core/BattleContextPartial_OathPeek.cs`（**新建**）·
> ✅ `CardPresentation/Net/NetProtocol.cs` · ✅ `CardPresentation/Net/NetApply.cs` ·
> ✅ `CardPresentation/Battle/BattleDriver.cs` · ✅ `CardPresentation/Editor/NetBattleTest.cs` ·
> ✅ `CardPresentation/Editor/BattleScene.cs`
> ⛔ `ReplayStore.cs`（本批不需要）· ⛔ `EffectResolver.cs`（引擎语义不动）· ⛔ 任何 `账/` 主表 · ⛔ 两张正本。

### 步骤 1 · `AiAction` 加两格（`RuleEngine/Data/SimpleAI.cs:106` 之后，紧挨 `CapturedPickIds`）

```csharp
        /// <summary>🆕 2026-10-31（`B110_R3` · 账 `N07`）：这一手**有没有**「平 / 誓二选一」的答案。
        /// `false` = **没记**（老录像 / 对面那一手 AI / 玩家没答）—— 必须与「记了、选了平版」**分得开**
        /// （`JsonUtility` 把缺键一律填 0；同族先例 = `MsgDeck.draftPropsVer` / `ReplayVars.varsRecorded`）。
        /// ⚠️ 与 `AltKeyword == "oath"`（**单位主动技**那条路，`RuleCore.UseOathAbility`）**不是一回事**。</summary>
        public bool OathChoiceRecorded;
        /// <summary>`OathChoiceRecorded` 为真时有效：**0 = 平版 · 1 = 誓约版**。
        /// 值 = 原版 `choicesDefinition.filterSpecificCards` 的**下标**（池内 4 张卡逐张现读：[0] 不带 oath trait、[1] 带）
        /// —— 与面板摆位同序（`BattleDriver.ShowAsk` 的 `ChooseOath_Plain_` / `ChooseOath_Oath_`）。</summary>
        public int OathChoice;
```

（可选，低风险）：`ToString()` 里带一句 `+ (OathChoiceRecorded ? " oath=" + OathChoice : "")`，日志好读。

### 步骤 2 · `MsgAction` 加两格 + `ToWire` / `FromWire` 拷贝

**(a) `CardPresentation/Net/NetProtocol.cs:93` `class MsgAction`** —— 建议加在 `altKeyword` 之后、`picks` 之前
（**新字段加在任何位置都不影响 `JsonUtility`**，但放在 `altKeyword` 旁边语义最近）：

```csharp
        /// <summary>🆕 2026-10-31（`B110_R3` · 账 `N07`）：**这一手带不带「平 / 誓二选一」的答案**。
        /// `false` = 没记（老报文 / 老录像 / AI 那一手 / 玩家没答）—— 与「记了、选了平版」**必须分得开**
        /// （`JsonUtility` 缺键填 0 ⇒ 只用一个 int 的话两种情形在线上同形；同族先例 = `MsgDeck.draftPropsVer`）。
        /// 🔴 原来是「**原版把下标塞在动作载荷里**」：`BattleAction.actionValueTens(+0x54)` →
        /// `ChoiceOfCardEnemy(…, selectedCard)` → `fullPool[idx]` → `cardChosenInSelection(+0x2E8)`
        /// （`out_il2csharp/Assembly-CSharp/BattleManager.cs:28810-28816 / :8949-8965`；
        /// 编码侧 `decomp_full/BattleManager__ParsePlayerAction.c:608` 把它打进 `PlayerAction.pos` 的十位）。
        /// ⇒ 我们同形：答案**随动作走**，⛔ 不另开消息。</summary>
        public bool oathRecorded;
        /// <summary><see cref="oathRecorded"/> 为真时有效：**0 = 平版 · 1 = 誓约版**（= 原版 `filterSpecificCards` 下标）。
        /// ⚠️ 与 <see cref="altKeyword"/> == "oath"（单位主动技）不是一回事。</summary>
        public int oathChoice;
```

**(b) `ToWire`（`NetProtocol.cs:497`，`altKeyword = a.AltKeyword,` 那一行后面）**

```csharp
                altKeyword = a.AltKeyword,
                oathRecorded = a.OathChoiceRecorded,
                oathChoice = a.OathChoice,
```

**(c) `FromWire`（`NetProtocol.cs:524`，`AltKeyword = m.altKeyword,` 后面）**

```csharp
                AltKeyword = m.altKeyword,
                OathChoiceRecorded = m.oathRecorded,
                OathChoice = m.oathChoice,
```

### 步骤 3 · `NetApply` 落地前注入（`CardPresentation/Net/NetApply.cs:140-146`）

把 `case AiActionKind.PlayCard:` 那一支改成：

```csharp
                case AiActionKind.PlayCard:
                    // ⚠️ **用手牌下标、不用实例 id**：…（原注释保留，一字不改）…
                    //
                    // 🆕 2026-10-31（`B110_R3` · 账 `N07`）：**誓约二选一的答案随动作过来** ——
                    //   原版把它当**一个下标**塞在动作里（`BattleAction.actionValueTens` →
                    //   `ChoiceOfCardEnemy(…, selectedCard)` → `fullPool[idx]` → `cardChosenInSelection(+0x2E8)`）。
                    //   我们同形：落地前摆进 ctx（`DecideOathChoice` 只认同座位、**一次一取**）。
                    //   🔴 `oathRecorded == false`（老录像 / 老报文 / AI 那一手 / 玩家没答）⇒ **一格都不写**
                    //      ⇒ 引擎走「付得起就付」兜底 = 加这一格之前**逐字相同**。
                    //   🔴 先清一次：`TakeOathAnswer` 只在出牌里消费，被引擎拒掉的那一手会把残答留到下一手。
                    ctx.ClearOathAnswer();
                    if (m.oathRecorded)
                    {
                        if (m.oathChoice == 0 || m.oathChoice == 1)
                            ctx.SetOathAnswer(actorSeat, m.oathChoice == 1);
                        else if (log != null)
                            log($"⚠️ 誓约答案的下标 `{m.oathChoice}` 不在 {{0,1}} 里 ⇒ 这一手按「没记」处理（付得起就付）");
                    }
                    code = RuleCore.PlayCard(ctx, actorSeat, m.handIdx, m.slot);
                    break;
```

> ⚠️ `ctx.ClearOathAnswer()` 在这里是**安全**的：活局里本机自己的动作**不走 `NetApply`**（走 `LocalAct` 直调引擎），
> 而「本机有未取的答案」与「对面/回放的动作落地」在单线程里不可能交错（`OnChooseDone → NextAsk → _afterAsks → LocalAct` 全同步）。

### 步骤 4 · `LocalAct` 抓答案（`CardPresentation/Battle/BattleDriver.cs:297` 之前插一段）

```csharp
            // 🆕 2026-10-31（`B110_R3` · 账 `N07`）：**誓约二选一的布尔答案也要在 `apply()` 之前抓**
            //   —— 与 `picks` 同一个道理（`DecideOathChoice` 会在结算里 `TakeOathAnswer` 取走它），
            //   而且它必须**跟着动作走**（`AiAction` → `ToWire` → 线上 / 录像）。
            //   不抓 ⇒ 对面与回放一律按「付得起就付」兜底 ⇒ 玩家选了平版的那些局**两端分叉、回放演成另一局**。
            //   ⚠️ 只对**出牌**抓（`DecideOathChoice` 只在 `PlayCard` 的战术支里读；攻击/技能/收集/结束回合没有这一格）。
            //   ⚠️ 没答（`TryPeek…` 为假）⇒ 两个字段保持「没记」⇒ 兜底路径不变。
            if (Ctx != null && act != null && act.Kind == AiActionKind.PlayCard)
            {
                bool oathPay;
                if (Ctx.TryPeekOathAnswer(_me, out oathPay))
                {
                    act.OathChoiceRecorded = true;
                    act.OathChoice = oathPay ? 1 : 0;
                }
            }
```

**为什么用 peek 而不是 `TakeOathAnswer`**：引擎自己还要取（`DecideOathChoice`），抢走它会变成「没人回答 ⇒ 付得起就付」。
**为什么不用 `ReplayAsk.picked`**：见 §三 链 3 那四条（UI 记录 / 有闸 / 活局没有它）。

### 步骤 5 · `RecMsg` 拷贝新格（`BattleDriver.cs:878`，`defId = m.defId,` 后面）

```csharp
                envSlot = m.envSlot, envSO = m.envSO, defId = m.defId,
                oathRecorded = m.oathRecorded, oathChoice = m.oathChoice,
```

> 🔴 **这一处最容易漏**：`RecMsg` 是**权威流**（对手那一半 + 换牌定序）唯一的录像口，
> 它是**逐字段手抄**的（不是整体拷贝）⇒ 少抄两格 = 联机录像里对手的誓约答案丢光，而单机录像看不出来。

### 步骤 6 · 新建 partial：peek 口（**零行数漂移**）

新建 `RuleEngine/Core/BattleContextPartial_OathPeek.cs`（先例 = `BattleContextPartial_B88E14.cs` /
`BattleContextRandom.cs` / `BattleContextTurnCounter.cs`；`BattleContext.cs` 有 40+ 处账按行号引用 ⇒ **主文件一行不动**）：

```csharp
// BattleContextPartial_OathPeek.cs — `BattleContext.TryPeekOathAnswer(...)`（`partial`，与 `BattleContext.cs` 同一个类）
//
// 🆕 2026-10-31（`B110_R3` · 账 `N07`）：给「表现层/录像侧」一个**只读**口，
//   而不必 `TakeOathAnswer`（那个取完即清，引擎自己还要取）。
// 判据：主文件 `BattleContext.cs:2048-2080` 那四格（`_oathAnswerSeat/_oathAnswerPay/_oathAnswerHas`）。
// ⛔ 不改主文件一行（它被账按 `文件:行号` 引用 40+ 处）。
namespace RuleEngine
{
    public partial class BattleContext
    {
        /// <summary>看一眼那一格答案（**不取、不清**）。`false` = 没有 / 座位不对。</summary>
        public bool TryPeekOathAnswer(int seat, out bool pay)
        {
            pay = false;
            if (!_oathAnswerHas || _oathAnswerSeat != seat) return false;
            pay = _oathAnswerPay;
            return true;
        }
    }
}
```

### 步骤 7 · 自检口（`BattleDriver.cs` **类尾**追加）

```csharp
        /// <summary>🆕 2026-10-31（`B110_R3` · 账 `N07`）自检口：录像里**最后一条**动作。
        /// `null` = 没在录像 / 还没动作。⚠️ 只给断言读，⛔ 别在别处拿来判业务。</summary>
        public MsgAction LastRecordedActionForTest
        {
            get
            {
                return (_rec != null && _rec.actions != null && _rec.actions.Count > 0)
                     ? _rec.actions[_rec.actions.Count - 1] : null;
            }
        }
```

（两段 × 每段 2 行 ÷ 1──见下断言里那句 `★ B110_R3-4`。）

### 步骤 8 · 断言

#### 8·1 `NetSelfTest.Run`（宿主 15s）—— 链路往返（改 `TestSeatAndWire`，`NetSelfTest.cs:624` 之后）

```csharp
        // 🆕 2026-10-31（B110_R3 · 账 N07）：誓约答案**随动作走**（`ToWire` 常被 `ctx:null` 调 ⇒ 不能靠 ctx）
        var ao = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Slot = -1,
                                OathChoiceRecorded = true, OathChoice = 1 };
        var wo = NetProtocol.ToWire(ao, null, 0);
        Ok(wo.oathRecorded && wo.oathChoice == 1,
           "★ B110_R3-I⑩：答案随动作上线（`ToWire(act, ctx:null, …)` 也带 —— `OnLocalAction` 就是这么调的）"
         + "｜🧨 只在 `ToWire` 里 `ctx != null` 才拷 ⇒ 本条红");
        var bo = NetProtocol.FromWire(wo);
        Ok(bo.OathChoiceRecorded && bo.OathChoice == 1, "★ B110_R3-I⑪：`FromWire` 反方向同语义");
        var wd = NetProtocol.ToWire(new AiAction { Kind = AiActionKind.PlayCard, HandIdx = 0, Slot = -1 }, null, 0);
        Ok(!wd.oathRecorded, "★ B110_R3-I⑫：没答 / 老档 ⇒ `oathRecorded == false`（⛔ 不能默认成『平版』）");
```

#### 8·2 `NetBattleTest.Run`（宿主 15s）—— 两端真落地（加在那段「选择点·另开一局」`hs2/cs2` 里，`NetBattleTest.cs:600` 之后）

> 模板就是同文件 `:555-598` 那段「两边同序塞一张卡 → 本机落地 → `nb.OnLocalAction` → `PumpBoth`」。

```csharp
                // ---- 🆕 2026-10-31（B110_R3 · 账 N07）：**誓约二选一的答案过网** ----
                CardDef oathCard = null;
                foreach (var c in pool) if (c != null && c.Name == "Adaptive Strategy") { oathCard = c; break; }
                Ok(oathCard != null, "（R3 前提）卡池里有带 `Oath` 的战术卡 `Adaptive Strategy`（UM109，1 费 + Oath 1）");
                if (oathCard != null && !ctxA.IsOver && !ctxB.IsOver && ctxA.Active == ctxB.Active)
                {
                    int a = ctxA.Active;
                    int ea = ctxA.Players[a].Energy, eb = ctxB.Players[a].Energy;
                    ctxA.Players[a].Hand.Add(ctxA.NewInstance(oathCard));
                    ctxB.Players[a].Hand.Add(ctxB.NewInstance(oathCard));
                    int hIdx = ctxA.Players[a].Hand.Count - 1;
                    int slot = SimpleAI.FirstFreeSlot(ctxA.Players[a]);
                    // 0 = 平版（不付誓约）
                    var actP = new AiAction { Kind = AiActionKind.PlayCard, HandIdx = hIdx, Slot = slot,
                                              OathChoiceRecorded = true, OathChoice = 0 };
                    int canP = RuleCore.CanPlayCard(ctxA, a, hIdx, slot);
                    Ok(canP == RuleCodes.OK, $"（R3 前提）那张 oath 卡打得出去（{RuleCodes.Describe(canP)}）"
                                           + "｜⚠️ 不 OK 就换 `UM110 Fall Back` / `UM116 No Mercy`，或先摆一个单位");
                    if (canP == RuleCodes.OK)
                    {
                        var nbP = a == 0 ? na : nc;
                        var wP = NetProtocol.ToWire(actP, null, 0); wP.actor = nbP.MySeat;
                        int rcP = NetApply.Apply(ctxA, wP, nbP.MySeat);
                        nbP.OnLocalAction(actP);
                        PumpBoth(hs2, cs2, na, nc, 12);
                        Eq(rcP, RuleCodes.OK, "（R3 前提）平版那一手落地成功");
                        Eq(ctxA.Players[a].Energy, ea - oathCard.Cost,
                           $"★ B110_R3-N2：**平版 ⇒ 本机只扣卡费**（{ea} → {ctxA.Players[a].Energy}，卡费 {oathCard.Cost}）");
                        Eq(ctxB.Players[a].Energy, eb - oathCard.Cost,
                           $"★ B110_R3-N3：**对面也只扣卡费**（{eb} → {ctxB.Players[a].Energy}）—— 答案真的过了网"
                         + "｜🧨 删掉 `NetApply` 里那两行注入 ⇒ 对面按「付得起就付」多扣誓约 ⇒ 本条红");
                        Eq(NetProtocol.Fingerprint(ctxB), NetProtocol.Fingerprint(ctxA),
                           "★ B110_R3-N4：两端指纹一致"
                         + "｜🧨 让 `RecMsg`/`ToWire` 少拷一格 ⇒ 本条在接线那一侧红（但本条只覆盖 `ToWire`）");
                    }
                }
```

再来一条**负例**（证明「没记」不等于「平版」）—— 用同一张卡、`OathChoiceRecorded = false`，
断言两端都**多扣了誓约**（= 今天的兜底行为，逐字不变）：

```csharp
                        // ★ B110_R3-N6：没记（老报文 / AI 那一手）⇒ 兜底「付得起就付」= 加这一格之前的行为
                        Ok(ctxA.Players[a].Energy == ea2 - oathCard.Cost - oathCard.GetDefaultTraitValue(KeywordTable.Oath, out _),
                           "★ B110_R3-N6：没带答案 ⇒ 按原兜底**付誓约**（⛔ 不能被当成『平版』）");
```

#### 8·3 `BattleScene.Run`（宿主 7m56s）—— **录制侧真的写了**（改 §21-e-②·b：誓约版在 `BattleScene.cs:30399`、平版在 `:30428`，各自的 `SimulateChooseDone()` 之后）

```csharp
                // 誓约版那一手（CheckPick(driver, 1, …) + SimulateChooseDone 之后）
                var lrOath = driver.LastRecordedActionForTest;
                Check(lrOath != null, "★ B110_R3-4a（前提）：录像里记下了这一手（`LastRecordedActionForTest` 非 null）");
                Check(lrOath != null && lrOath.oathRecorded && lrOath.oathChoice == 1,
                      "★ B110_R3-4：点**誓约版** ⇒ 录像里 `oathRecorded=true / oathChoice=1`"
                    + $"（实得 recorded={lrOath?.oathRecorded} choice={lrOath?.oathChoice}）"
                    + "｜🧨 删掉 `LocalAct` 里那几行 peek ⇒ 本条红");

                // 平版那一手（同上）
                var lrPlain = driver.LastRecordedActionForTest;
                Check(lrPlain != null && lrPlain.oathRecorded && lrPlain.oathChoice == 0,
                      "★ B110_R3-5：点**平版** ⇒ 录像里 `oathRecorded=true / oathChoice=0`"
                    + "｜🧨 把 `OathChoice` 写成恒 1（或恒 0 且不置 `Recorded`）⇒ 本条红");
```

**可选交叉校验（R3-N5，需再加一个 `LastRecordedAskForTest`）**：`ReplayAsk.picked`（UI 记录）与
`MsgAction.oathChoice`（引擎答案）**由两条不同的路产生**（前者 `ReplayAskNotePickedIndex`，后者 `TryPeekOathAnswer`）
⇒ 断言两者相等 = 灭自证（只改其中一条路不会一起变绿）。

### 步骤 9 · 汇总表

| 步骤 | 文件（⛔ 白名单外不许碰） | 改哪一行（现读） | 断言 | 风险 |
|---|---|---|---|---|
| 1 | `RuleEngine/Data/SimpleAI.cs` | `:106` 之后加两格 | I⑩~I⑫ | 极低（纯加字段） |
| 2 | `CardPresentation/Net/NetProtocol.cs` | `:93` 类内加两格 · `:509` 后 `ToWire` 拷贝 · `:536` 后 `FromWire` 拷贝 | I⑩~I⑫ | 低（`JsonUtility` 加字段不影响旧报文） |
| 3 | `CardPresentation/Net/NetApply.cs` | `:140-146` `case PlayCard` | N2/N3/N4/N6 | **中**：`ClearOathAnswer()` 位置错了会吃掉活局答案（只有本机不走这条路 ⇒ 实际安全） |
| 4 | `CardPresentation/Battle/BattleDriver.cs` | `:297` 之前插 9 行 | 4/5 | 低（只对 `PlayCard` 生效） |
| 5 | `CardPresentation/Battle/BattleDriver.cs` | `:878` 加两格 | 联机录像 | **中**：漏了 = 对手那一半丢答案，单机自检**看不出来** |
| 6 | `RuleEngine/Core/BattleContextPartial_OathPeek.cs`（**新建**） | —— | 编译过 = 到位 | 零（partial，主文件零行数变化） |
| 7 | `CardPresentation/Battle/BattleDriver.cs` | **类尾** | 4/5 的前提断言 | 零（类尾追加，无账落点） |
| 8 | `Editor/NetSelfTest.cs` · `Editor/NetBattleTest.cs` · `Editor/BattleScene.cs` | 见上 | 见上 | 低 |

**跑哪几条自检**：改了 `Net/**` + `BattleDriver` ⇒ `NetSelfTest.Run` · `NetBattleTest.Run` · `BattleScene.Run`
（铁律 12 的覆盖面判据；`RuleEngineTest.Run` 只在**引擎语义**变了才必跑 —— 本批没动引擎语义，但 `SimpleAI.cs` 归引擎线，
建议顺手跑一条 `RuleEngineTest.Run` 以防字段加进错类）。

---

## 五、协议兼容 / 旧档怎么办

| 问题 | 结论 | 依据 |
|---|---|---|
| `NetProtocol.Version` 要不要 +1 | **不要**（照仓里现行口径） | `NetProtocol.cs:825`「这只新增字段、没改任何既有字段的形状或语义 ⇒ 不 +`Version`」（同族先例 = `MsgChat` / `MsgDesync` / `MsgModeMismatch` / `draftPropsVer`） |
| 旧端（旧构建）收到新字段 | `JsonUtility` 忽略未知键 ⇒ 不炸；但它**没有答案** ⇒ 按兜底「付得起就付」⇒ **能量/局面与对端分叉** ⇒ 回合末指纹 `Abort`（**出声**，不静默） | `NetProtocol.Fingerprint` / `NetBattle` 的 desync 链 |
| ⇒ 若将来真要**两版并行** | 那时把 `Version` 3 → 4（`NetSession.cs:606` 会直接拒握手），比「打到一半 desync」诚实 | `NetSession.CheckProofKey` |
| 旧录像（`ReplayRecord.version == 4`） | **不用 +1**（纯新增字段，哨兵 `oathRecorded=false`）⇒ 老档回放的**行为与今天逐字相同**（一律付誓约） | 先例 = `ReplayAsk`（`ReplayStore.cs:830-832`）、`ReplayVars.varsRecorded`（`:896-897`） |
| ⚠️ 老录像的已知缺陷 | 老档里玩家当时选了平版的那些局，**今天就已经放不对**（引擎只好付誓约）——本批修的是**新录的**；老档无从补救（答案从来没被记过） | 事实陈述，不是回归 |
| 重连重放（`NetBattle.Log`） | `Log` 是 `List<MsgAction>` 原对象（`NetBattle.cs:217`）⇒ 新字段**自动**跟着走，不用改 | 现读 |

---

## 六、账补丁格式

> ⚠️ 合并器按 **`(原版类, 方法)` 整行替换**（`工具/merge_ledger_parts.py` 文件头「定位规则」）
> ⇒ 更新既有行时，**其余 9 列必须逐字复制主表现读的那一行**，否则会把别人的列冲掉。

本批**不需要新行**：`N07` 对应的原版方法是 `BattleManager.ChoiceOfCardEnemy`，
它已经在 `账/战斗逻辑驱动.tsv:254`（**现读状态 = A**）。**只往 `备注` 尾巴追加**，`轮次` 加 `·B110_R3`。

补丁文件（**新文件**，名字按调度台给的批次）：`d:/4/Unity/资料/完全复刻/账/_parts/B110_R3.tsv`

```
域	原版类	方法	判据出处	判据要点	状态	我们的落点	差异	等级	轮次	备注
```

**唯一一行**（第 6~9 列**必须**与主表 `战斗逻辑驱动.tsv:254` 现读值**逐字一致**；下面只用占位符标出「照抄」）：

| 列 | 值 |
|---|---|
| 域 | `战斗逻辑驱动`（合并器会按 `(原版类,方法)` 归一） |
| 原版类 | `BattleManager` |
| 方法 | `ChoiceOfCardEnemy(CardScript actingCard, CardScript targetCard, int selectedCard)` |
| 判据出处 / 判据要点 / 状态 / 我们的落点 / 差异 / 等级 | **照抄主表第 254 行**（现读 A 档那一行；⛔ 别重打，会丢字） |
| 轮次 | 照抄 + `·B110_R3` |
| 备注 | 照抄 + `｜[B110_R3 2026-10-31] **答案的传输链已补**：`AiAction.OathChoiceRecorded/OathChoice`（`SimpleAI.cs`）→ `MsgAction.oathRecorded/oathChoice` + `ToWire`/`FromWire`（`NetProtocol.cs`）→ `LocalAct` peek（`BattleDriver.cs`）→ `RecMsg` 拷贝（`BattleDriver.cs`）→ `NetApply` 落地前 `SetOathAnswer`（回放/重连/对端同一条路）。哨兵 `oathRecorded=false` = 没记（AI/老档/没答）⇒ 兜底「付得起就付」逐字不变。`NetProtocol.Version` **不 +1**（只新增字段；同族 `draftPropsVer`）。断言 = `NetSelfTest` I⑩~I⑫ · `NetBattleTest` N2/N3/N4/N6 · `BattleScene` 4/4a/5。`ReplayStore` 结构与 `version` 均不动。`｜🔶 仍自设计：布尔表示（原版是变体 SO 下标，我们池里只有一张合并卡，见 `B88_R3` §5·0 口径 A）` |

> 若调度台要求「新开一行」而不是改 254：那就把 `方法` 写成**一个真实方法签名**（合并器会拒收总结行），
> 且必须与 254 的 `(原版类,方法)` **不同**，否则命中 >1 处会被跳过。

---

## 七、没查清的部分（如实）

| # | 没查清 | 现状 / 还差什么 |
|---|---|---|
| 1 | `PlayerAction.pos` 的**十位**到底是不是 `actionValueTens` | `decomp_full/BattleManager__ParsePlayerAction.c:608` 的算式在（`pos = SpacesInTheBoard + <tens>*10 + <units>`），但两个源局部 `iStack_204` / `local_e8` **在本份 `.c` 里从未被赋值**（cpp2il 丢赋值的已知形状）⇒ 按位置 + 唯一候选判的。要钉死需 **VA 级反汇编** `ParsePlayerAction` 那一段。**对本批无影响**（我们用独立字段，不照抄位打包）。 |
| 2 | `BattleActionType` 5（`playCardFromHand`）那一支的 `ParseEnemyAction` 解码**看着不对**（`actionValueUnits = playerAction.tget` / `isPlayer = playerAction.tgtd`） | 疑似反编译丢字段 / 错位。同一函数里 `typ == 30`（`playActiveAbility`）那一支**是对的**（`actionValueUnits` + `actionValueTens` 都齐）。⇒ 我**只用 typ30 那一支**做证据。要弄清需读 `out_il2csharp` 的原始 `MoveNext`（ILSpy 那半边）。**对本批无影响**。 |
| 3 | 原版录像（服务端 `BattleRecordData` / `PlayerAction`）里誓约答案怎么落盘 | **P3**（服务器已关）⇒ 不复刻；我们走自己的本地录像，判据只到「答案随 `MsgAction` 走」为止。 |
| 4 | 原版 `cardChosenInSelection` **不清**（常驻到下一次选择）是否有别的消费者 | 现读只有 `AbilityLogic.PlayAbility` 的 `0x168` 一支 + `GetChoiceIndexOfCard`。**我们保持一次性 + 带座位**（已裁定口径，不改）。 |
| 5 | `ChoiceOfCardPlayer` 的预选下标（`actionValueTens`）在**本地**出牌时是哪来的 | 本地那一手理论上是 0（默认）⇒ 面板预选第 1 张。没跑实况核这一步（面板只在真机看得见）。**对本批无影响**（我们面板不预选）。 |

---

## 八、顺手发现（交给调度台分流）

1. 🔴 **原版 AI 对 oath 是「随机二选一」，我们是「付得起就付」** —— 这是一条**真行为差异**，不是传输问题。
   判据：`ChoiceOfCardEnemy` 在「非联网 + 敌方卡 + 非 replay」时 `flag2 = true`
   ⇒ `num1 = fullPool.IndexOf(options[0])`（`out_il2csharp/…/BattleManager.cs:8951-8965`），
   而 `GetChoiceOptions(..., isBotAndNotReplay: true)` **不排序**（`:9061` 的 `if (!(isBotAndNotReplay)) Sort(...)` 被跳过）
   ⇒ `options[0]` = **随机抽出来的第一张** ⇒ 平/誓**等概率**（并且消耗 `GetRandomInt`）。
   我们 `DecideOathChoice` 是「付得起就付」（`EffectResolver.cs:18020-18032`）⇒ **确定性**。
   ⛔ 本批**没动**（简报明确排除机制）；建议单开一条（顺带要处理 RNG 消耗形状 —— 原版那两抽进的是预烧随机表，
   见账 `战斗逻辑驱动.tsv:254` 的 `B94_Z3` 注）。**注意**：本条一旦要做，**正好用得上本批新加的两个字段**
   （AI 挑出来的那个值也必须录进 / 发出去）。
2. 🟠 **`MsgAction.handId` 是死字段**（全仓 0 读取点，类 doc 自己写着）。本批差点被当成「能塞答案的地方」——
   建议在 `N07` 收口时顺手记一条「要么删、要么写明为什么留」（同族：`MsgStart.myDeckSeat` 也是零消费方）。
3. 🟢 `ReplayAsk.picked` 的缺键默认是 `0`（`JsonUtility`）而 `-1` 才是「没选」⇒ 老档的 ask 会被**预选第 0 张**（纯表现）。
   今天无害（`picked` 不驱动引擎）；⚠️ 若将来有人把它接成引擎答案，这就是个静默错 —— 本批**正是**为了避免这件事才走 `MsgAction`。
4. 🟢 `TestSeatAndWire` 现在只断 `FromWire` 与 `NetApply` 同语义、**没有任何「字段级」往返断言**
   （加 `picks` 时也没加）⇒ 本批的 I⑩~I⑫ 是这一族的第一批；后续再加 `MsgAction` 字段时照这个形状补一条。
