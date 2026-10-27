# B9 · D4NT —— `NetBattleTest` 重放终局指纹不一致（只读诊断）

> 诊断代理 `D4NT`，2026-10-27。**只读**：没改任何 `Assets/**/*.cs` / 账 / 正本，没跑 Unity，没动 git。
> 输入：`_tmp_view/netbattle.log`（10-27 07:47，`244 通过 / 3 失败`）、基线 `_tmp_view/fix_NetBattleTest.Run.log`（10-27 00:37，`247 / 0`）。

---

## 〇、一句话结论

**不是「第 3 局 ≠ 第 1 局」的进程级状态。**
是「**live 的终局不是动作流打出来的，而是 NetBattle 层「打岔 → 判负」打出来的**」：
本批 `E1RNG` 把洗牌从 Fisher–Yates(`ctx.Rng`) 换成 `SupportMethods.GetRandomOrder(ctx.GetRandomInt)` ⇒ 开局两副牌的顺序变了 ⇒ 这一局走到**第 9 步**时 AI 枚举到的第一条动作是「**誓约能力 @5**」，而那个单位**付不起誓约费** ⇒ `UseOathAbility` 返 `ErrUnimplemented`；夹具**在自己引擎已经拒了这条动作之后照样广播**（`Editor/NetBattleTest.cs:394`），对端把它读成「两端打岔」⇒ `AbortWithVictory` → `desync` → `Forfeit` ⇒ **live 两端的 `Winner` 被置上 + 多出一批事件**。
而**重放只走 `NetApply.Apply` 那一层**（`Editor/NetBattleTest.cs:106-118`），**没有 NetBattle 那一层** ⇒ 被拒的那条只记一笔「被拒」，**不判负** ⇒ 重建出来的局面 `Winner = 0`、`Events.Count` 也不同 ⇒ 指纹对不上。

⇒ **P-04（`InitRandomNumbers`）/ `ArmedTrap` / `PlayedThirdCardInTurn` / `Aura` 全部排除**（理由见 ②）。

---

## ① 判别式结果（不跑 Unity 也判得出来）

**判别式本来要问的**：把 `Rebuild` 也套到主机上 —— 若重建主机也是 `−934489277`，则是「第 3 局 ≠ 第 1 局」的进程级状态。

**答案：静态就能判死这个假设，两条独立证据。**

1. **基线自证**：00:37 那一跑是**同一个宿主、同一个进程、同样三个 context**（`hostCtx` / `cliCtx` / `Rebuild`），重放**绿**（`fix_NetBattleTest.Run.log:100159` `实际 1409444004，应为 1409444004`）。
   ⇒ 「重建 = 第 3 局」这个**形状昨天就存在**，昨天不出事 ⇒ **不是它**。
2. **代码自证**：`RuleCore.NewBattle` 的全部输入 = (`deckA`, `deckB`, `seed`, `firstSeat`, `vars`, `cardPool`)，六个都是**夹具手里的同一批不可变对象**；
   `BattleContext` 的全部随机源都是**实例字段**（`BattleContext.cs:674-681`：`Rng = new Random(seed)` / `ShowRng` / `AiRng` / `InitRandomNumbers()`），
   `RandomNumbers` / `RandomNumbersIndex` 同样是实例字段（`BattleContextRandom.cs:34,37`）。
   `NewBattle` 唯一会读的进程级状态是 `GameplayVariables.Classic`（`_classic` 静态缓存）—— 但它在 `:176` 就被 `Clone()` 了。
   ⇒ 第 1/2/3 次构造**同输入 ⇒ 同输出**。

**真正发生的事（日志直读，不需要推理）**：

| | 00:37 基线 | 10-27 07:47（本批） |
|---|---|---|
| 脚本对打步数 | **37 步**（`…Run.log:99671` 最后一行为步 37） | **9 步**（`netbattle.log:98892` `脚本对打跑了 9 步（回合 3）`） |
| 权威动作流 | 42 条 | **14 条**（`netbattle.log:98956`） |
| 第 9 步 | `AttackMelee @5 →P2@4` | **`ActiveAbility @5 [oath]` → 该功能本版未实现**（`netbattle.log:98881`） |
| 该条在 live 的后果 | —（没这条） | 对端引擎也拒 ⇒ **打岔判负**（`netbattle.log:98804` / `:98835` / `:98852`） |
| live 终局 | 正常打到 `IsOver` | **靠 `Forfeit` 收场**（`Ctx.IsOver` 为真，循环退出） |
| 客机 live 指纹 | 1409444004 | −934427773（两端一致 ✅） |
| 客机重放后 | 1409444004（✅） | **−934489277（❌，两次重放都是它）** |

⇒ 差异**全部**来自「live 收场多了一个带外动作（判负）+ 它带来的一批事件」，而重放**没有这一层**。

---

## ② 逐条排除：谁「第 N 局会不会不同」

| 嫌疑 | 结论 | 依据 |
|---|---|---|
| **P-04 随机源预烧**（`E1RNG`） | ❌ **排除**（但它是**触发链**的第一环，见下） | `InitRandomNumbers()` 在**构造里**调（`BattleContext.cs:681`），而 `Rng`/`RandomNumbers`/游标全是**实例字段** ⇒ 每局各一份、同 seed 同表。**它不改「第几局」，它改的是「这一局的牌序」** ⇒ 是把**一场不一样的对局**喂给了这个宿主 |
| **`ArmedTrap` / `IsTrapCard`**（`E7ENG`） | ❌ 排除 | `ArmedTrap` **不进指纹**（`NetProtocol.PlayerHash` 逐行读过：没有它）；`TrapArmedOf` 只读 `CardDef`，`_trapDrawnNoted`（`EffectResolver.cs:1015`）**只用来压重复日志**（`:16561-16566`：`if (!_trapDrawnNoted.Add(…)) return;`），不改局面 |
| **`PlayedThirdCardInTurn`** | ❌ 排除 | 本局 9 步里没有触发到能改变终局判定；且它是**逐动作**副作用，live 与重放都经 `NetApply` |
| **`Aura` / `AchievementTally` / `EvtKind.EnchantmentRemoved`** | ❌ 排除 | `Aura.cs` 只 1 行改动；`AchievementTally` 新增的静态只有 `_abilityOverflowWarned`（一次性告警）；都不在 `Fingerprint` 的取值面上 |
| **静态可变状态普查** | ✅ **无一处能影响新局面** | `RuleEngine/Core|Data` 全量扫过：`GameplayVariables._classic/_skirmish/_tutorial/_closedDeck`（缓存的**默认值**，`NewBattle:176` 已 `Clone`）· `EffectResolver._lastMarkCtx/_lastMarkKind` + `_keywordsPact`（**每次 `NewBattle` 开头 `ResetTransientForNewBattle()` 清**，`EffectResolver.cs:12441-12448`）· `CreatePool`/`OffensiveCardData`/`RelatedCardData`/`TalentCardData`/`AiScoringData`/`CardDatabase._cache`/`ForcedAttackRefund._map`（**惰性加载的只读数据表**）· 其余全是 `*WarningsForTest` 计数器与 `_xxxWarned` 一次性告警 |

### 真正的成因链（每一跳都有 `文件:行`）

1. **洗牌换了随机源** —— `RuleCore.cs:1192` `static void Shuffle<T>(IList<T>, BattleContext ctx)` 现在调 `ShuffleByRandomOrder`（`:14989`）；`git diff` 显示改前是 `Shuffle<T>(IList<T>, Random rng)` 的 Fisher–Yates。
   ⇒ 同一条随机流给出的**排列不同** ⇒ 同 seed 下开局两副牌**和昨天不一样**（日志实证：昨天步 2 是 `PlayCard hand#3 @5`，今天是 `PlayCard hand#4 @0`）。
2. **不同的牌序把宿主带到一个新分支**：第 9 步 `PickAction` 取「枚举表里第一条非 `EndTurn`」（`NetBattleTest.cs:1777-1784`），枚举来自 `SimpleAI.EnumerateActions`；誓约那一条在 `SimpleAI.cs:391-392`（**只看 `CanUseOathAbility == OK`**）。
3. **`CanUseOathAbility` 不查钱**（`RuleCore.cs:11655-11667`）—— 这一条**与原版一致**（`d:/2/tools/decomp_full/CardScript__CanUseOathAbility.c` 全文：只查同回合/`oathInAllTurns`/`oathTripleActivation`/已用过/次数上限，**没有 mana**）⇒ **不是偏离**。
4. **付费在结算层**：`ResolveOathAbility` 的 `paid` 判据是「能量有没有减少」（`EffectResolver.cs:9066`），付不起 ⇒ 返 `false` ⇒ `UseOathAbilityCore` 把它翻成 **`RuleCodes.ErrUnimplemented`**（`RuleCore.cs:11717-11724`）。
5. **夹具本机被拒后照样广播**：`NetBattleTest.cs:392-394`
   ```csharp
   int code = NetApply.Apply(ctx, wire, nb.MySeat);
   if (code != RuleCodes.OK) { Debug.LogWarning(...); }   // ← 只记一笔
   nb.OnLocalAction(act);                                 // ← 无条件广播
   ```
   🔴 **生产路径不是这样**：`BattleDriver.cs:308` 是 `if (_net != null && code == RuleCodes.OK) _net.OnLocalAction(act);`
   ⇒ **只有自检会把「自己引擎都拒了的动作」发出去**。
6. **对端把它读成打岔并判负**：`NetBattle.cs:1527-1531`（`code != OK` ⇒ `AbortWithVictory`）→ `NetRemoteResign(Cancelled)`（客机判自己胜）/ 主机 `AcceptPeerDesync` 判自己负（`netbattle.log:98852-98866`）⇒ `RuleCore.Forfeit`（`RuleCore.cs:13706`）置 `Winner` + 发事件。
7. **重放没有第 5~6 那一层**：`BareHost.NetReplayFromNet`（`NetBattleTest.cs:106-118`）逐条 `NetApply.Apply`，只数 `ok/bad` ⇒ 13 落地 / 1 被拒（`netbattle.log:99347`），**终局 `Winner = 0`**。
8. **指纹包含 `Winner` 与 `Events.Count`**（`NetProtocol.cs:561-564`）⇒ 必然对不上。重放两次同一结果，正是因为它**也同样确定性地**重现了「没有判负」。

---

## ③ 分类

| 项 | 判定 |
|---|---|
| 3 条断言翻红 | **(γ) 本批回归**（本批 `E1RNG` 换洗牌源 ⇒ 换了这一局的牌序 ⇒ 换出一条会把「打岔判负」逼出来的路径） |
| 被暴露出来的真缺陷 | **(β) 夹具缺陷**：`NetBattleTest` 广播了**本机引擎自己拒掉的**动作（`NetBattleTest.cs:394`），与生产口径（`BattleDriver.cs:308` 只在 OK 时广播）**不一致** |
| 断言本身 | **(α) 不成立**：断言语义「重放能追平」是对的，**问题在重放模型不完整**（只回放 `NetApply` 那一层，没回放 NetBattle 的判负层）。⚠️ 这是**覆盖面缺口**，⛔ 不要靠放宽断言修 |
| `CanUseOathAbility` 不查钱 | **不是缺陷** —— 与原版逐句一致（`CardScript__CanUseOathAbility.c` 全文） |
| `InitRandomNumbers` 预烧表 | **不是缺陷** —— 原版 `MatchData` 也是每局一份（`BattleContextRandom.cs:11-18` 的判据链），我们落成实例字段同形状 |

---

## ④ 改法（都不动引擎语义）

### 4·1 必须改（修 3 条红的根）

**`Editor/NetBattleTest.cs:392-394`** —— 与本机引擎口径对齐，**拒掉的动作不广播、也不计入步数**：

```csharp
int code = NetApply.Apply(ctx, wire, nb.MySeat);
if (code != RuleCodes.OK)
{
    Debug.LogWarning(P + " 本机这条被拒了（**不广播** —— 与 `BattleDriver.LocalAct` 同口径）："
                     + RuleCodes.Describe(code) + " " + act);
    continue;          // ⛔ 不要 OnLocalAction：生产路径自己也只发 OK 的那条
}
nb.OnLocalAction(act);
```

⚠️ **但只改这一句会卡住**：被拒的动作不推进局面 ⇒ 下一轮 `PickAction` 会**再挑同一条**（`NetBattleTest.cs:1777-1784` 恒取第一条非 `EndTurn`），一直空转到 `guard < 400` 用完。
所以**同时**要把「取动作」改成 `NetBattleTest.cs:413` 之后那句的方式（它已经示范过：先问 `CanPlayCard`，`can == OK` 才动手）—— 建议：

```csharp
// 从枚举表里挑**第一条这条引擎真的会接受**的动作；全都接受不了才 EndTurn
static AiAction PickAction(BattleContext ctx)
{
    var list = SimpleAI.EnumerateActions(ctx);
    if (list != null)
        foreach (var a in list)
        {
            if (a.Kind == AiActionKind.EndTurn) continue;
            if (WouldApply(ctx, a) == RuleCodes.OK) return a;   // ← 只多这一道
        }
    return new AiAction { Kind = AiActionKind.EndTurn };
}
```
`WouldApply` 可以直接用「`NetProtocol.ToWire` → 试算」实现不了（会改状态）⇒ 更省的做法是**在循环里**改成「取一条 → 应用；`code != OK` 就换列表里的下一条」。两者选一，**都要向调度台报一声**（这会动到自检的动作挑选口径）。

### 4·2 建议顺手记一笔（⛔ 不是本任务要改）

- **`RuleCodes.ErrUnimplemented` 被拿去当「效果没生效 / 付不起」**（`RuleCore.cs:11723`）⇒ 日志印成「**该功能本版未实现**」，把「钱不够」误导成「我们没做这个功能」。
  这就是这次诊断最容易走偏的地方（第一眼会以为誓约没实现）。建议加一个 `ErrNoEffect` / 复用 `ErrNotPayable` 之类的码（改 `RuleCodes.cs` 是**跨面**改动，要单独评估）。
- **`SimpleAI.EnumerateActions` 会把付不起的誓约列进候选**（`SimpleAI.cs:391`）—— 与原版 gate 一致，但**AI 打分侧的 `AiScoring.ActiveAbilityManaCost` 默认 `null`**（`AiScoring.cs:1120`，注释自陈「主动技 manaCost 列未落 ⇒ ⑥⑦ 恒 0」）⇒ 真实 AI 也可能反复挑一条打不出去的誓约。**要不要查清原版 AI 怎么过滤 → 另立账**（本次没查，见 ⑤）。
- **本宿主的有效覆盖面缩水**：00:37 是 37 步真对打，本批变成 9 步且**以打岔判负收场** ⇒ 后面那三条断言虽然「绿/红」都还在，但验的是**一局被掐断的棋**。修完 4·1 之后**要确认步数回到 20+**，别让宿主长期停在 9 步。

---

## ⑤ 查不清 / 还没查的

1. **`NetProtocol.ToWire` 是否把 `altKeyword` 带进 wire** —— 没有逐行核。不影响结论（两支最终都返 `ErrUnimplemented`），但**决定 live 对端走的是 `UseOathAbility` 还是 `UseAbility`**；修 4·1 之前不必查。
2. **原版 AI 会不会枚举「付不起的誓约」** —— 本次没查（`AI.GetActions` / `GetActionScore` 那两支）。只确认了 **gate 层（`CanUseOathAbility`）确实不查钱**，所以**我们枚举得比原版宽是否算偏离，仍未知**。
3. **`ResolveOathAbility` 里那句「见上面那行原因」** —— 具体失败原因为什么没印进 Unity 日志（走的是 `ctx.Log`，进 `ctx.Log` 缓冲而不是 Console）。若要让下一个会话一眼看出「是钱不够」，要么在夹具里把 `ctx.Log` 尾巴 dump 出来，要么按 4·2 第一条换码。
4. **`RuleCodes.ErrUnimplemented` 的全部调用点**有没有别的「语义错码」—— 没普查。
