# DB · 手牌效果「先登记记录、再贴牌」6 红 · 只读诊断

> 只读诊断代理 `DB`。⛔ 没跑 Unity · ⛔ 没跑 typecheck / ruleprobe · ⛔ 没动 git · ⛔ 没改任何工作文件（本文件是唯一产出）。
> 现场 = `d:/4/_tmp_view/ruleengine.log`（4.0 MB，2026-10-07T14:16Z 那次 `RuleEngineTest.Run`）。
> 6 条红 = 56162 / 56176 / 56204 / 56233 / 56306 / 56408（尾部摘要副本 `59002`–`59007`）。

## 1. 一句话结论

**6 条红 = 一个根因：(γ) 同批回归（口径被同批的另一支推翻）+ (δ) 夹具前提不成立 —— 不是引擎缺消费者，也不是本笔「先登记记录、再贴牌」写错。**

`RuleCore.SetupCardInHand` 走 `HandEffectFits`，而 **W5 当天把「筛选条件判不出来 ⇒ 放行单位卡」收窄成「不放行」**（`Core/RuleCore.cs:3736`）；
`RuleEngineTest.TestHandEffectExpiryAndUses` 的 ④⑤⑥ 三处夹具是 **W4（同批、同一天）** 写的，它们**用公开入口的默认 `criteria = null`** 建记录（`Core/EffectResolver.cs:3096`），
却期望「后进手牌的牌吃得上」⇒ 6 条全是 0。
**同一条链上 ⑦（`:4039`，带一条判得出的 criteria）正例/负例都对**，而且 ⑥ 的半途断言（`:3973/:3983` 记录表计数）**都绿** ⇒ 消费者与登记表都是好的。

⇒ **最小改法 = 改 3 处夹具（4 个调用点）各补一个「判得出的 criteria」**，引擎一行不用动；②-1（`:19301`，用 `null` 钉「不放行」）保持绿。

**另外当场查出 1 条独立 (β) 缺陷**：`SetupCardInHand` 那句「退路档要自己说出来」的自报**是死代码**（`RuleCore.cs:3627-3628`，`continue` 排在下标计数之前 ⇒ `loose` 恒 0；全 4 MB 日志里那句话 **0 次命中**）。
**它正是本该在收口时把这条回归直接喊出来的那一条** —— 修它（3 行）比修那 6 条红更值。

## 2. 逐条诊断表

| 账（断言处） | 结论 | 证据（文件:行号 + 片段） | 该照哪个判据 | 最小改法 | 置信度 |
|---|---|---|---|---|---|
| **① `:3889`**「后进手牌那份补到 **2** 条」实得 0（日志 56162） | **(δ)+(γ)** 夹具前提不成立 | 夹具 `:3879/:3881` 用 `AttachHandEffectToInstances(ctx,"T_STACK","+1 attack",null,0,new List<CardInstance>{same[0]})` —— **第 7 个形参 `criteria` 走默认 `null`**（`EffectResolver.cs:3093-3096`）⇒ 记录 `Target == null` ⇒ `HandEffectFits` 命中 `if (crit == null && !hasSubtype) { loose = true; return false; }`（`RuleCore.cs:3736`）⇒ `:3627` `continue` ⇒ n=0。**同一份实例上的两条记录本身是好的**（`:3883/:3885` 两条断言**都绿**） | 原版 `PlayerHand__SetupCardInHand.c:58` `FilterMethods.CheckIfMeetsCriteria(word, card, card, handEffect.targetCriteria, 0)`；`FilterMethods__CheckIfMeetsCriteria.c`：`param_4 == 0` ⇒ `CustomDebug.LogError` + **`return 0`**。⇒ **记录带的 criteria 必须判得出**，才谈得上「补给后进手牌的牌」 | `:3879` 与 `:3881` **各加第 7 个实参**（写在块首一次、两处复用）：<br>`var unitCrit = new EffectTargetSpec { Raw="(夹具：手牌里的部队)", Side="own", Kind="unit", Count=0, Auto=true };`<br>`…ToInstances(ctx,"T_STACK","+1 attack",null,0,new List<CardInstance>{same[0]}, unitCrit);` | **高** |
| **② `:3892`**「那 2 条真的落在它自己身上」实得 0（56176） | **(δ)+(γ)** 同根因 | 同 ① —— `AttachHandEffectCopy` 一次都没被调到（`RuleCore.cs:3629`）⇒ `same[1].HandEffects` 空 | 同上 | 同 ①（改完自动为 2） | **高** |
| **③ `:3897`**「条数没变」要 2 实得 0（56204） | **(δ)+(γ)** 同根因（⚠️ 相邻的 `:3895` 是**假绿**，见 §6.2） | 同 ① | 同上 | 同 ①（改完后这条才第一次**真的**在判幂等） | **高** |
| **④ `:3905`**「端到端 2 → **4** 攻」实得 False（56233） | **(δ)+(γ)** 同根因 | 该 ✗ 的 ⟪日志⟫ 尾 = **5 条事件**（`开局 / 回合1开始 / T_STACK ×2 / 部署`，`LogTail` 取末 6 条 ⇒ 全量）—— 里面**没有**任何「进手牌…判不出来」⇒ 与 §3 的推理链互证 | 同上 | 同 ①。`ApplyHandBuffs`（`RuleCore.cs:3380-3390`）遍历的是 `inst.HandEffects`、**不读 `e.Target`** ⇒ 补 criteria **不会**影响兑现：两条 `+1` 各跑一遍 = 2+2 = **4** ✓ | **高** |
| **⑤ `:3942`**「`create … 进手牌` 也走消费者」实得 0（56306） | **(δ)+(γ)** 同根因 | 夹具 `:3932` 同样默认 `criteria = null`；`:3933`（前提，实例级）**绿** ⇒ 记录挂上了、只是后进手牌那一跳被 `HandEffectFits` 拒。`RuleCore.cs:1779` 的 `SetupCardInHand(ctx,p,made)` 那一句**在**（不是漏插） | 同上 + `PlayerHand__AddCardNotDrawnToHand.c:34` → `SetupCardInHand` | `:3932` 末尾加 `unitCrit`（同 ① 那个 spec） | **高** |
| **⑥ `:3993`**「后来进手牌的牌照样吃得上」实得 0（56408） | **(δ)+(γ)** 同根因 | 夹具 `:3971` 默认 `criteria = null`；而 **`:3973`（记录表 = 1）与 `:3983`（载体走了记录还在 = 1）两条断言都绿** ⇒ **记录表这一半是好的**，红只落在「后进手牌的牌吃不吃得上」。抽到的那张 = `filler16`（`Deck()` 垫牌恒 `Unit(...)`，`RuleEngineTest.cs:1486`）⇒ 补 `Kind="unit"` 后命中 | 同上 | `:3971` 末尾加 `unitCrit` | **高** |
| **〔第二条缺陷〕`SetupCardInHand` 的退路自报从不发声** | **(β) 实现缺陷（独立，非本 6 条红的成因，但**掩盖了**它）** | `RuleCore.cs:3626-3629`：<br>`bool isLoose;`<br>`if (!HandEffectFits(inst.Card, e, out isLoose)) continue;`<br>`if (isLoose) loose++;`<br>—— `continue` 排在计数**之前**；而 `HandEffectFits` **只有**在 `{ loose = true; return false; }` 那一支（`:3736`）才置位 ⇒ **`isLoose` 在 `loose++` 处恒 `false`** ⇒ `:3637 if (loose > 0)` **不可达**。<br>证据（决定性）：`grep -c "筛选条件我们"` 与 `grep -c "这一趟一条都没给它"` 在这份 **4.0 MB 日志里都是 0** | W4 自己的「审查问题 9」（`W4_引擎手牌族交件.md:109`「退路档**必须**自己说出来」）+ 工程红线「不许静默失败」 | `RuleCore.cs:3626-3629` 改成：<br>`bool isLoose;`<br>`bool fit = HandEffectFits(inst.Card, e, out isLoose);`<br>`if (isLoose) loose++;`<br>`if (!fit) continue;`<br>`if (AttachHandEffectCopy(ctx, inst, e)) n++;` | **高**（日志反证） |

## 3. 根因分析（为什么排除其他候选解释）

**候选 1「登记表没登记 / 写坏了」⇒ 排除。**
⑥ 的 `:3973`（`ctx.Players[0].HandEffectRecords.Count == 1`）与 `:3983`（载体离开手牌后仍为 1）**两条都绿** —— 登记（`EffectResolver.cs:2950-2965`）与「记录 ≠ 手牌」都成立。

**候选 2「消费者一整条链坏了」⇒ 排除。**
同一个 `TestHandEffectExpiryAndUses` 的 **⑦**（`:4012-4054`）是**正例 + 负例成对**的：正例（同兵种 `Beast`）实得 **1**、负例（`Infantry`）实得 **0**，且日志里 `吃上了**还在生效**的手牌效果` 只出现在正例（全日志 2 次命中）。
⇒ `HandEffectRegistry`（`RuleCore.cs:3668-3683`，按 `RecordId` 去重 + 按号升序）与 `AttachHandEffectCopy`（`EffectResolver.cs:3032-3059`）都是好的。

**候选 3「一次挂多张只记一条 / `RecordId` 去重把两条压成一条」⇒ 排除。**
④ 的两条记录来自**两次独立调用**（两个 `RecordId`，`:3885` 已断言不同）；而且「被压成一条」的结果只可能是 **1 或 2**，**不可能是 0**（实得 0）。

**候选 4（真根因）「`HandEffectFits` 把 `Target == null` 的记录一律拒掉」⇒ 成立。**
- 三处夹具与 ⑦ 的**唯一**差别 = 有没有传 `criteria`：⑦ 传 `Kind="unit" + SubtypeFilter="Beast"` 的 spec（`:4034-4041`），④⑤⑥ 用默认 `null`。
- 走的是**同一条**代码路径（`:3620 registry → :3627 HandEffectFits → :3629 copy`），**结果相反** ⇒ 变量只能是 criteria。
- 6 条红**恰好**落在「要求一张**后进手牌**的牌吃一条 `Target == null` 的记录」这三处（④ 的 4 条、⑤ 1 条、⑥ 1 条）；而④⑤⑥ 里**不**经过 `SetupCardInHand` 的断言（`:3883/:3885/:3895/:3973/:3983/:3933` 等）**全绿**。

**为什么「收窄」本身不是缺陷（别去把它改回 `return true`）**：
`FilterMethods__CheckIfMeetsCriteria.c` 的**头两句**就是判据 —— `if (param_4 == 0) { CustomDebug__LogError(...); return 0; }`（**criteria 缺失 ⇒ 拒 + 报错**）、`if (*(int *)(param_4 + 0x10) == 0) return 1;`（**空 criteria ⇒ 全放行**）。
我们这一侧：`e.Target == null`（= **没有规格**）最贴近原版 `param_4 == 0` 那一支 ⇒ **拒**是忠于原版的；`e.Target` 有值但读不出内容（如 `Kind="prev"` 无先行词）在原版**没有对应状态**，按工程红线「宁可少给，也别乱给」拒掉也一致。
⇒ **要动的是夹具，不是这道闸**。

**收口时为什么没被拦住（值得记一笔）**：
W4 写的 ④⑤⑥ 建立在「判不出来 ⇒ 按单位卡放行」这个**当时的行为**上（`W4_引擎手牌族交件.md:109/:142` 明确记着「默认仍是放行单位卡、需裁」）；
同一天的 W5 把退路档收窄（`W5_引擎残账交件.md:30/:58`），**只验了自己新写的 ②-1**（`W5:58` 写着「退路档改回 `return true` ⇒ ②-1 实得 1 ⇒ 红 ✅」），**没有回头跑 W4 那三处依赖旧行为的断言**。
⇒ 两条账在**同一个 commit（`fe239e9`）里互斥**（`git log -S "T_STACK"` / `-S "判不出筛选条件"` / `-S "退路档"` 三个都是 `fe239e9`），只有同步点那次全套才暴露。

## 4. 原版在这几处应该是什么行为（带出处）

| 处 | 原版行为 | 出处（逐句读的方法体） |
|---|---|---|
| 牌进手牌时按什么筛 | 遍历 `PlayerHand.activeEffects // +0x48`（**记录表**），每条过 `FilterMethods.CheckIfMeetsCriteria(word, card, card, handEffect.targetCriteria /*+0x20*/, 0)` | `PlayerHand__SetupCardInHand.c:53-59`（亲读：`param_1 + 0x48` = 记录表、`lVar2 + 0x20` = 该记录的 `targetCriteria`） |
| criteria **缺失**（null）时会怎样 | **拒**：`CustomDebug__LogError(...)` + **`return 0`** | `FilterMethods__CheckIfMeetsCriteria.c` 头两句 |
| criteria **存在但为空**（`kind` 字段 `+0x10` == 0） | **全放行**：`return 1` | 同上，下一句 |
| 命中之后 | `CardScript.AlreadyContainsEffect`（按**记录**认，`:65`）为假 ⇒ `CardScript.AddEffect(card, handEffect.cardEffect)`（`:71`）⇒ 效果**挂在那张牌自己身上** | `PlayerHand__SetupCardInHand.c:65 / :71` |
| 记录**先于**贴牌 | `PlayerHand.AddHandEffect`：`:89` 打 `doNotStack` → **`:90-110` 先把记录收进 `activeEffects`** → **`:112-139` 才遍历 `currentHand` 逐张贴**（那段循环里**没有**「一张都没贴就把记录撤掉」） | `PlayerHand__AddHandEffect.c:88-139`（本笔「先登记记录、再贴牌」的判据，**成立、不必撤**） |
| 「同一次挂载」的身份 | 一次 `AddHandEffect` 发给 N 张牌 = **同一个 `HandEffect` 记录 / 同一个 `CardEffect` 对象** ⇒ 按**记录**去重（不是「来源 + 载荷」字符串） | `PlayerHand__AddHandEffect.c:112-139` + `CardScript__AlreadyContainsEffect`（本笔 `RecordId` 的判据，**成立**） |
| ⚠️ 我们这侧的**结构性差异**（如实标注，不是新发现） | 原版记录表在 `PlayerHand` 上、**效果对象**挂 `CardScript +0x108`；我们两张表都有、且是**同一批 `HandEffect` 引用**（`PlayerState.HandEffectRecords` + `CardInstance.HandEffects`）⇒ **不是第二份状态**（`PlayerState.cs:30-51`） | 无需改，`W4`/`G3` 已核 |

## 5. 没查清的部分（含 D1 遗留的两张卡）

1. 🔴 **原版有没有「手牌效果的 `targetCriteria` 为空」的真例**（即 `return 1` 那一支在**手牌效果**这条路上到底可不可达）。
   **还差**：在解包资源里找到 `HandEffect` 那族 ScriptableObject 资产表（`PlayerHand.activeEffects` 的**生产者**）。
   与 `W4_引擎手牌族交件.md:76` 的「没查清：`GetCardsInHandRef` 那条路上 criteria 到底从哪来」**同一条**。
   ⇒ 在它查清之前，「`criteria == null` 该放行还是该拒」**两个方向都不算错**（原版的 null 支 = 拒；空对象支 = 放行），**我们两种输入都能表示**（`Target == null` / `Target` 非空但判不出）。今天可判的只有：**这条输入在真对局里不可达**（见下）。
2. ✅ **`criteria: null` 在真对局里不可达（可判、已判清）**：四个真生产者**全部**传 spec ——
   `GrantHandBuff`（`EffectResolver.cs:3157` `HandTroopCriteria`）· `GrantHandBuffForTargets`（`:3227` `spec`）· `AttachEffectToHandInstances`（`:3483` `op.Target`，且它只在 `spec.Side=="prev" && spec.Kind=="prev"` 时被调，`:5519-5523`，故 `Target` **非空**）· `BroadcastHandWhen` 自指那一条（`:5168-5169` `HandTroopCriteria`）。
   ⇒ **`Target == null` 只可能来自自检直接调公开入口的默认值**。这也是为什么改夹具**零玩法影响**。
   ⚠️ **因此 §5.1 的答案不改变结论**：无论将来裁向哪边，都必须**同时**动 ④⑤⑥ 与 ②-1 里的同一侧（它们钉的是同一个不可达输入的两端）。
3. ✅ **`Rubric Marine`（`BL15`，`When you draw a card, gain a Dark Pact of Fate`）—— 落进 `HandListenerSelfOp`，结论：是。**
   三条闸逐条判（`EffectResolver.cs:5215-5225`）：
   - `verb ∈ {give,gain,lose}` ⇒ **`gain`**。出处：离线探针基线 `d:/4/Unity/工具/ruleprobe/基线/out_baseline.txt:198`
     `BL15|Rubric Marine||When you draw a card, gain a Dark Pact of Fate|gain/pool///0/a dark pact of fate;`
     —— dump 格式 = `verb/ChooseSrc/ChooseWhat/ChooseAct/Amount/Payload`（`工具/ruleprobe/Scan.cs:41-43`），
     `ChooseSrc = "pool"` 是**默认值**（`Core/EffectText.cs:246`），**不是**特判支。
   - `Payload` 非空 ⇒ `a dark pact of fate`（同上）；源侧 `op.Payload = what`（`EffectText.cs:6694-6697`，无时长可摘）。
   - `Target.Subjectless` ⇒ **真**：`ReGain`（`EffectText.cs:6762-6763`）第二条备选 `^(?:gains?|gets?|becomes?)\s+(.+)$` 命中时 **group 1 不参与** ⇒ `subj = ""` ⇒ `:6751` `if (subj.Length == 0 || IsSuspendedSubject(subj))` ⇒ `:6752-6756` 那条 `Subjectless = true` 的 spec。
   ⇒ **它确实被挂到「那一份手牌实例」上**（`EffectResolver.cs:5166-5168`，`criteria = HandTroopCriteria` = `Kind:"unit"` ⇒ **判得出**）。
   🔴 **顺带两条必须落盘的后果**（都**不在**这 6 条红的修法里）：
   - **(a) 这是一条【本批新引入】的行为改动**：`git show ff6a612:Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs | grep HandListenerSelfOp` = **零命中** ⇒ 整支「没写主语 ⇒ 落在它自己那份手牌实例上、**打出时才兑现**」是 `fe239e9` 新加的。
     对 `Rubric Marine` 意味着：`When you draw a card, gain a Dark Pact of Fate` 从「**抽牌那一刻结算**」变成「**挂到那张手牌实例、打出时才兑现**（`RuleCore.ApplyHandBuffs`）」。**真卡受影响**（铁律 11 ⇒ 该做/该裁，不是「观察一下」）。
   - **(b) 那条记录会被发给【后进来的任何部队】**：记录带 `criteria = HandTroopCriteria`（`Kind:"unit"` ⇒ `CreatePool.KindWords` 第 `unit` 行 = `type == "unit"`，`CreatePool.cs:878`）⇒ 按 `SetupCardInHand` 的语义，**之后进手牌的每一张部队卡**都会吃上它、并在打出时拿到一个 `darkpact(fate)`。
     即：`Rubric Marine` 的效果**不止落在它自己身上**。这是**否**为原版行为，**我没查到判据**（差的是：原版这条 `When you draw` 是走 `PlayerHand.AddHandEffect` 那条带 `targetCriteria` 的路，还是只挂 `CardScript` 自己那份）。**≠ 猜测，如实挂着。**
   - ⚠️ **我的 `Subjectless` 判定是静态读出来的**（没跑 `EffectText.Parse`，本代理不许跑）：基线 dump 那几列**不含 `Target`** ⇒ 前两闸有 dump 直证、第三闸是代码路径直读。要一次性钉死：跑一次 `EffectText.Parse` 或 `ruleprobe` 的专题。
4. ✅ **`Company Master`（`DA31`，`When you draw a card, it costs 1 less this turn`）—— 不受影响，结论与 D1 的「大概率」一致，现在能坐实。**
   基线 `out_baseline.txt:250`：`DA31|Company Master|||lowercost/pool///1/when you draw a card, it;` ⇒ verb = **`lowercost`**，而 `HandListenerSelfOp` 只收 `give/gain/lose` ⇒ **不进自指那一支**，照旧走 `ResolveOps`（触发时结算），**不经过**「挂手牌实例」这条新路。
   （⚠️ 顺手记：那行 dump 的 `Payload` = `when you draw a card, it` —— 看着像**触发句没被切掉**。我**没查**它是不是真缺陷（也可能是**基线过期**：`W5` 动过 `EffectText`）⇒ 单列 §6.4，别混进这 6 条红。）
5. `ruleprobe` 基线是否与 `fe239e9` 同步 —— **没验**（验它要 `dotnet build`，会往 `工具/ruleprobe/bin|obj` 写东西，本代理按只读办）。
   ⇒ §5.3 里引用的两行 dump 属**旁证**；判据以源码为准（我已逐条给出源码行号）。

## 6. 顺手发现（都没改）

1. 🔴 **§2 最后一行那条 (β) 死代码**（`RuleCore.cs:3627-3628`）—— **它是本轮最该先修的一行**：修完之后，任何一处「criteria 判不出来 ⇒ 少给」都会**当场出声**，⑨ 号那类回归不会再静默通过收口。建议同时按本工程惯例补一条**「出声」断言**（同族先例 = `RuleEngineTest.cs:3847-3850`「摘的时候**出声**」）：例如在 ②-1 末尾断言 `ctx.Events` 里含「判不出来」（今天 ②-1 **只有** 0/0 两条，没有任何「出声」断言）。
2. **`:3895` 是「假绿」**（断言 `SetupCardInHand(...) == 0`，而当下**任何**输入都返回 0 ⇒ 它同时容得下「幂等」和「整条链不工作」）。
   ⚠️ **同族还有 ②-1（`:19305-19307`）**：它是**故意**期望 0 的那一条 —— 形式上永远分不出「闸拒了」与「什么都没发生」（`W5` 的 🧨 是**手动变异验证**的，`W5_引擎残账交件.md:58`；不是断言自带的判别力）。⇒ 若裁向 §5.1 的任一边，**两处要一起改**，别只改一边（否者就是把「假绿」换成「假红」）。
3. **`criteria: null` 的语义在三处注释里是【旧的】**（`W5` 收窄后没跟着改，铁律 5）：
   - `Core/EffectResolver.cs:2851-2852`（`AttachHandEffect` 的 `criteria` 形参）：「`null` = 没有可判的规格（**那时 `RuleCore.SetupCardInHand` 退回「单位卡」这一档**，见那儿）」—— **那个退路已经不存在了**（`RuleCore.cs:3736`）。
   - `Core/EffectResolver.cs:3090-3091`（公开入口 `AttachHandEffectToInstances` 的同一个形参）：同上。
   - `Core/CardInstance.cs:158-163`（`HandEffect.Target`）：「`null` = 这一条没有可判的规格（见 `RuleCore.SetupCardInHand` 的如实说明）」—— 没写清**后果**（= 这份记录**不补给**后进手牌的牌）。
   **建议一句话统一**：「`null` ⇒ 这条记录**不补给**后进手牌的牌（`HandEffectFits` 判不出来就不放行）；真对局里四个生产者都传 spec ⇒ 本值只由自检默认值产生」。
4. **`Company Master` 那行基线 dump 的载荷看着不对**（§5.4）—— 单列待查，**我判不了**（要跑 `ruleprobe check` 看差异，或直接读 `EffectText` 的 when 切分）。
5. **同批一条「顺带被否掉」的候选**（写下来免得下一轮重查）：④ 的 ✗ 里那句「实得 3」是**作者对旧写法的预期**，**从没在本版被实测到** —— 本版任何写法在这条上都只能得 0（闸在产品之前）⇒ 该断言的注释里那句判别力说明**今天不成立**，改夹具后**会**成立（那时「压成一条」的写法才真的得 2 攻而不是 4）。
