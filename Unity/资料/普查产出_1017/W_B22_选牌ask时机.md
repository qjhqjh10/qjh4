# W_B22 · 选牌 ask 的【时机 + 归属】（A904 / A905）—— 写手代理报告（2026-10-17）

> 账目 = `资料/普查产出_1017/W_B14_选牌一族.md` §③（触发链）· §⑥（手牌那一支）。
> 白名单 = `Battle/BattleDriver.cs` · `Editor/BattleScene.cs`（**只补断言**）· 本文件。
> 🔴 **报告先给一条订正**：B14 说这族是 **4 张**（`Farseer` / `Farseer Skyrunner` / `Master Zacharial` /
> `Suppressor`）—— 全池**实测 6 张**，**漏了 `Azrael` 与 `Watcher in the Dark`**（两张都是 `Agenda` 那一档）。
> 连 `Rally:` 那 6 张算，受影响的是 **12 张**，不是 10 张。

**结论**：两件**都落地**。A904 = ask 点按触发者**分档** + **清队/归属**；A905 = 删掉 `ShowAsk` 里那条短路。
验证手段 = B14 留下的**离线探针**（`net8` + UnityEngine 桩，把整个 `RuleEngine/Core/` 编成控制台程序，
跑**真解析器 + 全池 1126 张普查**，不占 Unity 实例）。探针副本在 `D:/tmp/wf_b22_probe/`（临时目录，没进仓库）。

## ① 原版时序（逐跳 `文件:行号`）

**一句话：原版是「结算到那一步才问」——没有任何「出牌前一次问完 + 答案排队」的机制。**

| # | 事实 | 证据 |
|---|---|---|
| 1 | `ChooseCardMethod` 是**迭代器**（协程），不是「问完返回答案」的普通方法 | `d:/2/il2cpp_out/dump.cs:31768` `private IEnumerator ChooseCardMethod(BattleAction battleAction, Action<int> callback)` |
| 2 | **全工程只有 4 个调用点，且全在「效果结算到那一步」** | `decomp_full/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:607`（非指向性出牌）· `:827`（小人落地之后）· `:1248`（指向性法术动画之后）· `BattleManager._ResolvePlayActiveAbility_d__479__MoveNext.c:367`（**主动技能**） |
| 3 | 每个调用点后面紧跟 `StartCoroutine(...)` + **`return 1`** ⇒ **结算协程在此挂起** | `_d__447:827-832`（`StartCoroutine` → `*(param_1+0x10)=0xb` → `return 1`）· `_d__479:371` · `_d__447:611` · `:1252` |
| 4 | 池子在**那一刻**才算（不是出牌时缓存） | `_ChooseCardMethod_d__449__MoveNext.c:17` `GetChoiceFullPool(...)`；`BattleManager__GetChoiceFullPool.c:27-33` 读 `actingCard+0x2A0`（= `RawCardScript.choicesDefinition`）现算 |
| 5 | **玩家**：开面板 → `UIstate = choosingResolutionCard(14)` → 协程轮询 `yield return bm.minDelay` 直到 `UIstate != 14` | `_d__449__MoveNext.c:28-29`（`isPlayer` 分支）· `BattleManager__ChoiceOfCardPlayer.c:18-20`（`UIstate = 0xe`）· `:50` `ChooseCardMenu__Setup(...)`；`_d__449__MoveNext.c:44-48`（yield 边界）· `:41-53`（醒来） |
| 6 | 答案是**单槽字段**，不是队列：`ChooseCardMenu.selectedCard` → `BattleManager.cardChosenInSelection` | 写：`ChooseCardMenu__SelectCard.c:26`（`+0x60`）→ `ChooseCardMenu__ProcessChooseDone.c:142` → `BattleManager__ClickChosenCardDone.c:22-25`（写 `+0x2E8` 并置 `UIstate=1`，**那一下就是通知协程可以醒了**）；读：`_d__449__MoveNext.c:53` `GetChoiceIndexOfCard(...)`。字段声明 `dump.cs:37544` / `dump.cs:30844` |
| 7 | 答案回灌结算流 = `Action<int>` 闭包写 `battleAction.actionValueTens` | `__c__DisplayClass447_0___ResolvePlayCardFromHand_b__0.c:6`（`*(param_1+0x54)=param_2`；`0x10+0x44` = `battleAction+actionValueTens`） |
| 8 | **灵魂石能力那道闸**：付得起才走到「从池里选」 | `_d__447:713` `NeedToChooseMinionTarget` 假 → `:723` `CanUseSpiritStone / HasDefaultTrait(10)` → `:728` `NeedsToChooseFromPool` 真 → `:831` 起协程 |
| 9 | **对手/AI 那一侧不弹面板、不 yield**（同步算完立刻回调；打 AI 时等价随机挑） | `_d__449__MoveNext.c:28 / :35 / :55`（敌方分支不 yield）；`BattleManager__ChoiceOfCardEnemy.c`（**完全不碰 `ChooseCardMenu`**，`:47` 直写 `+0x2E8`） |
| 10 | **超时**：面板内没有计时器，用的是**回合时钟**；`counter < 3` 秒时自动随机选 + 自动确认 | `ClockManager__Update.c:75 / :130` → `BattleManager__CountdownTrigger.c:5-10`（只在 `UIstate==14` 时转发）→ `ChooseCardMenu__CountdownTrigger.c:22-51`。面板 Done 钮**先禁用**、选中一张才可点：`ChooseCardMenu__Setup.c:58` · `__SelectCard.c:29` |

⇒ **对我们的两条硬结论**：
· **主动技能那一格的 ask 属于「技能结算」**（`_d__479:367`）⇒ 应当**点技能那一刻**才问 —— A904 就是照这条做的；
· **灵魂石那一族的 ask 属于「出牌结算」**（`_d__447:827`，且外面套着 `CanUseSpiritStone`）⇒ 留在**出牌**那一批、**付得起才问**。

## ② 改了什么

### A904 —— ask 点**分档** + **清队/归属**（`BattleDriver.cs`）

| 落点 | 行号（2026-10-17） | 内容 |
|---|---|---|
| `AskOwners` / `NormSrc` / `AskKey` / `IsAskOp` / `AddQuota` / `OwnerLabel` | `:4109-4197` | **归属判定**：`play` / `spirit` / `oath` / `alt:<关键词>`。按 `Verb + 归一化文案`（去掉开头的 `<前缀>:` 再比）比，**不按对象比**（`PlayerChooseOps` 与 `CardDef.Collect*` 各 parse 一次，是两份不同对象）；认领**有名额**（正文里同名 N 次最多认领 N 条） |
| `PlayAsks` | `:4204` | **出牌这一批**：`play` 全收；`spirit` **只在 `SpiritStones >= op.Cost` 时收**；`oath`/`alt` 推迟到技能那一刻。被排除的每一处**都出声** |
| `AbilityAsks` | `:4250` | **技能这一批**：按 `alt → oath → Ability:` 三条来源挑对应那一档 |
| `BeginAsk` / `BeginChoiceBatch` | `:4267` / `:4287` | 一批 ask 的入口 + 续跑回调 `_afterAsks`。🔴 **技能那一批【不清计数】**（`ChooseSites/ChooseAnswered` 进了录像对账哈希 `DeepHash`，而重放那条路 `NetApply.Apply` 从不 `ResetChoices` ⇒ 多一处清零 = 多一处「录/放计数不等 ⇒ 假报分叉」） |
| `BeginPlay` | `:4302` | 改走 `PlayAsks` + 续跑闭包（原来直接 `PlayerChooseOps` + `Ctx.ResetChoices()`） |
| `NextAsk` | `:4516` | 走 `_afterAsks`（原来写死 `DoPlay`）—— 现在**出牌与技能两种动作共用同一条 ask 通道** |
| `ReportUnaskedChoices`（**清队那一半**） | `:5643` | ① 报「引擎替你挑了几处」（**只报新增**，水位 `_settledSites/_settledAnswered`）② 🔴 **把没人取的答案报出来 + 清掉** —— 留着它一定会被**下一个** ask 点（可能是对手/AI 的）吃掉 |
| 清队挂点 | `LocalAct:222` · `AfterAiAction` · `EndTurnAndAdvance` · `EndPlayerTurn` ·（既有）`ApplyLoggedAction:263` | **每一次引擎动作之后**都清，答案不可能活过一次动作 |
| `Resolve` → 拆出 `DoResolve` | `:6485` / `:6515` | 主动技能那一格：有 ask 就先 `BeginAsk`（返回 `OK`）、引擎调用等面板关掉之后由 `DoResolve` 落地 |
| `AskScopesForTest` | `:4020` | 自检口：把归属表**原样问出来**（判据 = `AskOwners`，与面板实际用的是同一份） |

### A905 —— 删掉 `ShowAsk` 里 `ChooseEffectIsHand` 的短路（`:4450`）

`chooseeffect` 分支不再分叉：`hand` 与 `self`/`give` 的差别**只在结算落点**（给手牌 vs 给目标），**开面板这件事一模一样**。
引擎侧 **2026-09-16 就做完了**（`DoChooseEffect` 的 `handScope` → `GrantHandBuff` → `ctx.HandBuffs` → `ApplyHandBuffs`），
原来那条短路让玩家**永远看不到那三项**、引擎按 `ctx.Rng` 替他挑（**静默替玩家做决定**）。

## ③ 断言（`Editor/BattleScene.cs` §21-e，`:10687` 起，纯插入 239 行）

- **归属表**（9 条）：`Farseer` / `Farseer Skyrunner` = `spirit=1` · `Suppressor` = `oath=1` ·
  `Master Zacharial` / `Azrael` / `Watcher in the Dark` = `alt:agenda=1` ·
  `Rapid Deployment` / `Chaplain` / `Infinite Biomorphologies` = `play=1`。
- 🔴 **判别式 (a)**（**核心那条**）：`Farseer` + **0 灵魂石** ⇒ **面板不弹**、牌照样打出去、队列 0、
  日志有「这次不会发动」。**把 ask 点挪回「出牌前一次性问完」⇒ 这条必红。**
- 🔴 **判别式 (b)**：`Farseer` + **2 灵魂石** ⇒ 面板**弹**、选一张 ⇒ **那张真从牌库抽上手**。
  与 (a) **一起把「什么时候问」两头夹住**（也挡住「干脆把灵魂石那一档也搬到技能那儿去」）。
- 🔴 **判别式 (c)**：出牌 `Watcher in the Dark` ⇒ 面板**不弹** + `ChooseCardIds.Count == 0`
  —— **旧写法这里是 1**（那 1 格就是会被下一处选择吃掉的那一格）。
- 🔴 **判别式 (d)**：把它的行动解开、点那格技能 ⇒ **面板这时才弹**、候选 ≥1、选一张 ⇒ 进手牌、队列 0。
- A905 判别式：「给手牌」那一支**面板照常弹**、三项文字对、`ChooseAnswered` 涨（= 引擎用的是**面板给的**那一项，
  不是 `ctx.Rng`）。**旧那行短路 ⇒ 必红。**
- ⛔ **没跑**（按简报：选牌是交互，断言写好不跑）。跑法照工程既有做法（`SimulatePlayViaPanel` +
  `SimulateOpenCommand` + `SimulateCommand` + `SimulateChoosePick/Done`，全是**真实点击同一条路**）。

## ④ 没查清 / 需要白名单外的文件（请调度台分流）

1. **出牌那一档仍「早半拍」**（如实标着，**不是做完了**）：原版是**小人落地之后**才 `ChooseCardMethod`
   （`_d__447:827`），我们是在 `PlayCard` **之前**弹面板。要做到同拍，**引擎必须能被挂起**
   （`ResolveOps` 一口气跑完 ⇒ 得改成协程/续延，动 `RuleEngine/Core/**`）⇒ **白名单外，停手**。
   影响 = **6 张 `Rally:` 卡**（只差半拍、答案一定被用掉）。
2. **技能那一格「付费失败」时仍会白问一次**：`Oath` 的付费在 `ResolveOathAbility` **里面**
   （`CanUseOathAbility` 不查余额）⇒ 付不起时面板照样开、答案作废。**已出声**（剩余检测会报），
   但要真同拍得动引擎（`CanOath` 那次判定）。受影响 1 张（`Suppressor`）。⚠️ 它同时还有另一层：
   它的候选域（`Choose a non-Legendary Stratagem you played this game`）**当前返 0**（B14 的 §三-1 那张记账表还没做）
   ⇒ 面板开不起来、只出声 —— 那是**另一条账**，不在本次。
3. **原版那 4 个调用点的「谁启动这两个协程」读不到**：全目录 grep `ResolvePlayCardFromHand` /
   `ResolvePlayActiveAbility` 只命中工厂方法与闭包，`BattleManager__ResolveAction.c` /
   `__ResolveActionFromQueue.c` 里都没有 ⇒ 「动作队列 → 这两个协程」那一层**没反编译出来**。
4. **原版「同一张牌两个选择点」没有正面案例**：`ChooseCardMethod` 只有 4 个调用点，且 case 5/7 里
   目标询问与池子询问是 **if/else 互斥** ⇒ 推不出「一次结算连问两次」的路径，但也**没有反例**。
   （我们的算法对这种情况有兜底：剩余检测 + 「混装」名额制；实测**全池 0 张混装**。）
5. **超时的具体秒数读不出来**：`DAT_1834b2bb4 / bb c / f94` 都是数据段符号，`decomp_full` 与
   `il2cpp_out` 里都没有浮点值；只能确定 `< 3` 这个整数阈值。（**没做**：面板超时自动确认 —— 原版有，我们还没有。）
6. 🔴 **三处过期注释/记录（全在我白名单外，请核销）**：
   · `RuleEngine/Core/EffectResolver.cs` 的 `ChooseEffectIsHand` 注释 + `DoChooseEffect` 的 ③ 段
     —— 仍写「**没做的只剩面板那一侧**（`BattleDriver.ShowAsk` 里 `ChooseEffectIsHand` 那一支）」；
   · `RuleEngine/Core/EffectText.cs` 的 `TryChooseEffect` 注释 —— 同句；
   · `资料/选牌_数据与规格.md:268 / :424 / :487-488` 与
     `资料/普查产出_1017/盘点_战斗部分文档.md:102` —— 「**面板侧没做**」已不成立。
   **A905 已把它们全部做掉** ⇒ 请主对话按铁律 5 就地订正。
7. **正本两行**（`项目任务.md` §三 `A904` / `A905`）**不在我白名单** ⇒ 请主对话核销。

## ⑤ 类型检查（原样贴）

```
$ TMPDIR=/tmp/wf_b22 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（末次重跑亦 0/0。）**行尾已核**：`BattleDriver.cs` CRLF 10542 / LF 10542 ·
`Editor/BattleScene.cs` CRLF 15660 / LF 15660 —— **未翻**。⚠️ 这两个文件**在我开工前就是 `M`**
（工作区里带着别人的未提交改动），`git diff --numstat` 是 551/58 与 664/7，**不全是我的**；
我那一处插入是 `@@ -10337,0 +10687,239 @@`（纯插入）。
