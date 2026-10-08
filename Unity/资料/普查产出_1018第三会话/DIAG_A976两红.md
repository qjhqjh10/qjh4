# DIAG —— `RuleEngineTest` 本轮 2 条红（`A976` ②）

- 只读诊断，未改任何代码 / 正本 / git；未跑 Unity。
- 读数来源：`d:/4/_tmp_view/ruleengine.log`（`:60263` ① 绿 · `:60321`/`:60335` 两条红 · `:60437` 汇总 3813/3815）。
- 结论一句话：**`LastCreated.Clear()` 加对了地方、但只加在「两条 `ResolveOps` 中的一条」上**；② 走的是另一条（触发层），
  所以陈旧值照样抢班。② **是 (β) 实现缺陷，红是对的**。

---

## 1 调用链（② 那一跳逐跳）

| # | 位置（`文件:符号名`） | 发生了什么 |
|---|---|---|
| 1 | `RuleEngineTest.TestA976LastCreatedNotCarriedAcrossCards`（`RuleEngineTest.cs:20551`，② 段 :20560） | `ctx.LastCreated.Add(ctrlInst)` 种陈旧值 → `RuleCore.Draw(ctx, 0)` |
| 2 | `RuleCore.Draw`（`RuleCore.cs:1135`） | 只做三件：`MoveDeckTopToHand`（`:1193`，抽出 `A976Big`）→ `using (DrawReferentScope.Seed(...))`（`:1153`）→ `BroadcastWhen(Draw,…)`（`:1154`）→ `DrawnThisTurn=true`。**`RuleCore.Draw` 自己一句 `ResolveOps` 都不调** |
| 3 | `EffectResolver.BroadcastWhen`（`EffectResolver.cs:4879`）棋盘那一跳 | `ResolveOps(ctx, owner, u, ops, "事件触发", subject);`（`EffectResolver.cs:4960`） |
| 4 | ⇒ **`EffectResolver.ResolveOps` 的第二个重载**（`by`/`seed` 版，`EffectResolver.cs:4147`） | **它不是本批改了的那一个**。全程只动 `LastTargets`（种 `:4171` / 还原 `:4187`）+ `LastTarget`，**从不碰 `LastCreated`** |
| 5 | 循环 `ResolveOne(...)`（`EffectResolver.cs:4178`） → `EffectResolver.DoLowerCost`（`EffectResolver.cs:7050`） | `var refs = ctx.LastCreated.Count > 0 ? ctx.LastCreated : ctx.DrawnThisResolve;`（`:7083`）⇒ 陈旧值 `[A976Ctrl]` **抢班** |

**本批改的那一句在哪儿**：`EffectResolver.ResolveOps` 的**第一个重载**（`out unresolved` 版，`EffectResolver.cs:38`）入口，
`ctx.LastCreated.Clear();` 在 `:114`。测试 ①（`RuleEngineTest.cs:20518-20529`）**显式调的就是 #1**
（`RuleCore.ResolveOps(ctx, 0, null, ops, null, out un)`）⇒ **① 绿、② 红**，正是「清了一个入口、漏了另一个」的指纹。

> 两个重载的分工（注释自己写的）：#1（`:38`）= **出牌 / 战术卡**那条路（调用点 `EffectResolver.cs:1096` / `:1178`）；
> #2（`:4147`）= **所有触发层的入口**（它自己的注释原话：「事件触发 / 常驻效果 / 灵魂石 / 誓约 / 手牌陷阱 / 突触邻居 / 死亡结转 …」，
> 见 `EffectResolver.cs:4182-4185`），调用点 8 处：`DoPaidMod:1323` · `ResolveAtTurn:4310` · `ResolveSpiritAbility:4370` ·
> `ResolveOathAbility:4447` · `BroadcastWhen:4960` · `BroadcastPersistentWhen:5065` · `BroadcastHandTrapWhen:5193` · `BroadcastHandWhen:5333`。

## 2 陈旧值可达性

**谁写 / 谁清 / 谁读（逐条符号名）**

| 动作 | 位置 | 说明 |
|---|---|---|
| 写 | `EffectResolver.DoCreate` `:1860` Clear / `:1958` `AddRange(made)` | 「上一批造出来的」 |
| 写 | `EffectResolver.DoChooseCard` `:2336` Clear / `:2337` `Add(pickedInst)` | 选牌挑中那份 |
| 写 | `EffectResolver.DoDrawRef` `:3814` Clear / `:3815` `AddRange(refs)` | 定向翻找 |
| 写 | `EffectResolver.DoReturn` 支A `:3900` Clear / `:3901` `Add(back)` | 回手那张（一号靶 `Master of Manoeuvre`） |
| 写 | `EffectResolver.DoReturn` 支B `:3981` Clear / `:3982` `AddRange(moved)` | 回手那批 |
| 清 | `EffectResolver.ResolveOps` **#1 入口** `:114`（本批新增） | ⛔ **#2 入口 `:4147` 一处都没有** |
| 读 | `EffectResolver.DoLowerCost` `:7083`（日志 `:7095`） | 就是出问题那一句 |
| 读 | `EffectResolver.DoReturn` 的 pin 段 `:4077-4079` | 同一槽的第二个读点 |

**判定：可达，而且不止一条路。** 链条（真实对局，无需捏造）：

1. 我方打出「**在手里印一张牌**」的卡 → 走 **#1** → `DoCreate` 写 `[X]`；该次 resolve 结束，**#1 不再跑**（不回来清）。
2. 下一个动作只要**不经 #1**，`[X]` 就还在槽里：
   - **回合起始抽牌** —— `RuleCore.BeginTurn`（`RuleCore.cs` 内 `:837` `for (...) Draw(ctx, ctx.Active);`，随后 `:852 ResolveAtTurn("turn_start")`）
     → `RuleCore.Draw`（`:1135`）→ `BroadcastWhen` → **#2**。**这一跳不在任何 `ResolveOps` 里**
     （与 `TestS10CompanyMasterLowerCost` ① 自己那句断言同一事实：`RuleEngineTest.cs:19899` 附近「这次抽牌不在任何 `ResolveOps` 里」）。
   - 我方/对方的**攻击 / 受伤 / 阵亡 / 部署**触发（`RuleCore.cs:2461` / `:2659` / `:2973` / `:3267` / `:4567` 等处的 `BroadcastWhen`）→ 同样落 #2。
3. ⇒ `DoLowerCost` 读到上一张卡留下的 `[X]`。**而且读完之后槽仍不清**（#2 不清、#1 要等下一次出牌）⇒ 同一回合内**反复**误用同一个陈旧值。

「对手的回合会不会替我清掉？」—— 只要对手**没出牌**（只攻击 / 直接结束），`#1` 一次都不跑 ⇒ 陈旧值可以**跨过整个对手回合**活到我方回合起始抽牌。

## 3 原版语义（`d:/2/tools/decomp_full/`，第一权威）

| 方法体 | 读了什么 |
|---|---|
| `AbilityLogic__GetRefCard.c` | `return previousAbilityTargets[0]` —— 原版**只有一个**「指的哪张」槽（`AbilityLogic` 静态表，`DAT_1842c3a28+0xb8`），取法 = **表头第一个** |
| `AbilityLogic__SetPreviousAbilityTargets.c` | `System_Array__Clear(...)` **之后** `AddRange(param_2)` ⇒ **整表覆盖**；写点**唯一** |
| `AbilityLogic__PlayAbility.c:2743-2745` | `uVar18 = *(param_1 + 0x80); SetPreviousAbilityTargets(param_1, uVar18);` —— 传进去的是**这条能力自己的累加表** `param_1+0x80`，写点在**这条能力的末尾** |
| `AbilityLogic__AddDrawCard.c` | 抽到的那批是 `List.AddRange(...)` / `FUN_180002430(list, card)` 进**传入的 `param_7`＝`param_1+0x80`** ⇒ 形态 = **调用方（能力）持有、op 只追加、不清** |
| `grep -rln 'last_created\|LastCreated\|CreatedCard' decomp_full/` | **0 命中** ⇒ 二进制里**没有**第二个「最近造出来的」槽 |

**结论三条：**

1. **窗口**：原版 = **每条能力一个**，而且是**整表覆盖**（不是「清空后各 op 自己填」）⇒
   「上一条能力留下的值参与下一条能力的判定」在原版**不会跨整个回合存活**。　🔴 **2026-10-18 第三会话就地订正（铁律 5）**：本句原写「原版**不存在**」—— **推导链不成立**。现读 `AbilityLogic__PlayAbility.c:2745` 的 `SetPreviousAbilityTargets` **不是「每条能力末尾固定一次」**（那一跳 `LAB_18081cc87` 被**多个 op case** 跳入），而静态槽**在下一次写之前一直留着上一次发布的表** ⇒ **严格说「上一条能力留下的值参与下一条」在原版是【可能发生】的**（程度 = 紧邻的上一次发布，**不是**跨整个对手回合）。✅ **已定的口径不变**（入口清 = **比原版更严**，且实测那条链能跨过整个对手回合 ⇒ 该清）；**只改这句措辞**。**A976 的方向（入口清）是对的**，只是清错了重载。
2. **优先级无判据**：原版**没有**「`LastCreated` 优先 / `DrawnThisResolve` 兜底」这种两级制 —— 那是**我们把一条槽拆成两条**之后的产物
   ⇒ `DoLowerCost` `:7083` 的那个优先级是**自造**（`BattleContext.cs:830-832` 的注释也只援引我们的旧 `.gd`，非原版）。
3. **「抽牌那一跳读哪个」**：原版走的是**该能力自己的累加表**（`AbilityLogic+0x80`）+ 能力末尾整表覆盖，
   形状上等同「**本条能力自己看得见的对象**」，不是「全局最近一次 create」。
   ⚠️ **直证拿不到**（见 §6）：DA31 的 op 载荷在远端 `allcards`，且原版**降费 op 的 handler 我没定位到**
   （`AbilityLogic__PlayAbility.c` 全文 grep `cost` 只有 `BattleManager__PayCardPlayedCostOath` 一处）。

## 4 两条断言逐条分类

| 断言 | 判定 | 判据 |
|---|---|---|
| ① 入口清槽（`RuleEngineTest.cs:20518-20529`，**绿**） | **(δ)-lite：夹具把口径钉窄了**（⛔ 它不红，别动期望值） | 它把 `RuleCore.ResolveOps` **这一个名字**当成「结算入口」这个概念，而那句 `Clear()` 只属于**重载 #1**（`:38`/`:114`）。断言本身不假，但它**在给一个半成品背书**。建议：注释里写清「只覆盖重载 #1（出牌 / 战术卡那条路）」 |
| ② 降费落在刚抽到那张（`:20565` 期望 4 实得 5） | **(β) 实现缺陷** | 见 §1/§2：修的那句在 #1 入口，② 走 #2 ⇒ 未生效 |
| ② 对照：陈旧指代一分钱没降（`:20569` 期望 5 实得 4） | **(β) 实现缺陷**（同一条根因的另一面） | `refs` 取到 `LastCreated=[A976Ctrl]` ⇒ 只降了它、抽到那张连兜底都没走到 |

⚠️ 关于「② 是直接把陈旧值种进槽里」：
- **它模拟的是可达状态**（§2 那条链：上一张卡 `create` → 回合起始抽牌那一跳），所以 **② 判 (β)、不是 (δ)**。
- 它没覆盖两件事：(i) **同一条 resolve 内 create→draw**（写手自陈）；(ii) **反过来「连着出两张牌」** —— 这一种**已经被 #1 的入口清修好了**。
  ⇒ 建议补一条 (ii) 的对照（现在**没有**任何断言钉住「#1 入口清」在真实形状下真的起作用）。

## 5 最小改法候选

**候选 A（最小、直给）**：在**重载 #2 入口**（`EffectResolver.cs:4147`，`prime` 那段 `:4170-4176` **之前**）加一句 `ctx.LastCreated.Clear();`。
- 为什么：与 #1 的 `:114` 对称，覆盖「所有触发层」这一整族（#2 自己的注释就这么叫它，`:4182`）。
- ⛔ **只清 `LastCreated`，别顺手把 `DrawnThisResolve` 也清了** —— 后者在 `DrawReferentScope`（`EffectResolver.cs:4793`/`4807`）里被**临时种成「刚抽到那一份」**，
  #2 清它会把 `S10` ①（`Company Master` 真生效那条绿）**改红**。
- 风险：`DoPaidMod:1323`（按新时长重结算 `baseOps`）· `ResolveAtTurn:4310` · `ResolveSpiritAbility:4370` · `ResolveOathAbility:4447` 也是 #2 的入口 ⇒
  若某条能力「先 create，之后经另一次 #2 再结算 lowercost」，会被打断。**这 8 个调用点没逐条核**（挂 §6）。

**候选 B（最贴原版窗口）**：在**每一条监听器（能力）结算之前**清一次，而不是在整段广播之前。
- 落点建议收成**一处** helper，由 `EffectResolver.BroadcastWhen`（`:4879`）的四条跳共同调用
  （`BroadcastPersistentWhen:5065` · `BroadcastHandTrapWhen:5193` · `BroadcastHandWhen:5333` + 棋盘跳 `:4960`）——
  铁律：「两处写同一条规则 = 迟早不一致」。
- 风险：清在「整段广播之前」会打断「同一次广播里前一条监听器 create → 后一条监听器 lowercost」；严格按 §3-1 那本来也不该指到，但要**显式记下来**。

**候选 C（结构，最彻底）**：把 `LastCreated` 从 `BattleContext` 长存槽（`BattleContext.cs:814`）改成**随结算帧走的局部表**（照 `AbilityLogic+0x80` 的形态：调用方持有、op 只追加）。
- 优点：唯一能从**结构上排除**「跨能力存活」的做法，也最好写铁律里那条「灭自证」断言。
- 风险：改动面 = 5 个写点（`DoCreate`/`DoChooseCard`/`DoDrawRef`/`DoReturn`×2）+ 2 个读点（`DoLowerCost`/`DoReturn`）+ 字段本身。

**候选 D（⛔ 不建议单独用）**：把 `DoLowerCost` `:7083` 的优先级反过来（`DrawnThisResolve` 优先）。
- 只治 DA31 这一条症状、**无判据**（§3-2）；且一条正文同时 create + draw 时仍会挑错。要用必须标「这是我们挑的」。

## 6 没查清的部分

- 原版**降费 op 的 handler** 没定位（`AbilityLogic__PlayAbility.c` grep `cost` 只有 `PayCardPlayedCostOath`）⇒ §3-3 的直证仍缺。
- DA31 的 op 载荷在远端 `allcards` bundle（本地无）⇒ 无法逐字证明「原版抽牌那一跳读刚抽到那张」。
- 重载 #2 的 **8 个调用点没有逐条核**「入口清会不会打断同一条能力内的两段结算」。**选候选 A 之前应把这 8 处过一遍。**
- **顺手发现（未查证、未动手）**：重载 #1 清的**另外四个槽** —— `DrawnThisResolve`（`:54`）· `LastHandTargets`/`LastHandTarget`（`:63-64`）·
  `LastTargets`（`:83`）· `LastChosenCard`（`:89`）—— 在**重载 #2 里也一处都不清**（#2 只碰 `LastTargets`：`:4171` 种 / `:4187` 还原）
  ⇒ **`A888` 那批（`LastHandTargets`，`BattleContext.cs:860` 自己写着「什么时候清：`RuleCore.ResolveOps` 的入口」）与 `LastChosenCard`
  大概率有同一族缺陷**，只是还没有一张卡把它踩出来。建议派一条只读普查（判据：每个槽的「#1 有清 / #2 无清」逐条列）。
- 另：② 的两条断言**没挂 `LogTail(ctx)`**（`RuleEngineTest.cs:1429` 那个 helper），所以红的时候看不到引擎自己那句日志
  （例如「要降费，但前面没有可指代的卡」）—— 这次定型靠的是推理链而不是现场日志。建议补上。
