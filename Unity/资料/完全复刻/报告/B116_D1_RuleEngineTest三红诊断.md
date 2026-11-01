# B116_D1 · `RuleEngineTest.Run` 三红诊断（只读，2026-11-02）

> 日志：`_tmp_view/ruleengine.log`（94 MB）· 工程：`D:/4/Unity/MyGame`
> 本代理**只读**：未改任何 `.cs` / `.json` / 账 / 活状态表，未跑 Unity / git。

## 〇、先纠一个前提：**「6 条失败」≠「3 类 ×2」**

`RuleEngineTest.cs:1409-1414` 的汇总只回显 `_failures` 列表（= `Check` 自己记的 3 条）；
而 `Guard("B116W27_Assert", …)` 那一路把 `B116W27_Assert.Fail` 直接加进 `_fail`、**不进 `_failures`**
（`RuleEngineTest.cs:1306` 同一物理行）。日志实证：

- `ruleengine.log:1020395` → `[B116W27] === 合计：66 通过 / 3 失败 ===`（C1/D1/D4 三个 `✗`）；
- `ruleengine.log:1063848` → `RE === 结束：13916/13922 通过，**6 条失败** ===`，但下面**只列 3 行**；
- 那 3 行各出现 2 次（`912323`/`1015778`/`1017874` = 现场，`1063849-1063851` = 汇总回显）⇒ 是「现场 + 回显」，不是「×2」。

⇒ **真红 = 3（`RuleEngineTest` 本体）+ 3（`B116W27_Assert`）**。前者见 §一～§三，后者见 §四。

---

## 一、`★ A1574：那个「it」不是留下那张` —— **(α) 断言本身错**

**断言原文**（`Assets/RuleEngine/Editor/RuleEngineTest.cs:35381-35385`）：
`Check(host.Attack, 4, "★ A1574：**那个「it」不是留下那张**（留下那张 = 2 + 2 = 4，**没有**再多 1 点）…")`

**证据（判据链，逐跳现读）**
1. `RuleCore.cs:4828` `FireTriggerAlways(ctx, host, Swarm, p, hostSlot, just)`；`:13091` 广播 `BroadcastWhen(Triggers(swarm), owner, u=host.Card, subject=just, target: u=host)`。
2. 本批 `B-314` 落地的那一行 `EffectResolver.cs:10650`：`ResolveOps(…, seed: (kind==Triggers(Swarm) && target!=null) ? target : subject)`
   ⇒ 监听卡正文的 seed = **target = host**（`git diff` 亲见：`-…subject);` → `+…? target : subject);`）。
3. 卡表判据：`Resources/cards_engine.json` 现读 —— `TL15 Tyranid Prime = abilityTargets[{trigger:620, ta:[30]}]`、`TL75 Termagant Brood = ta:[20]`（池内 620 监听只有这两张）。
4. `AbilityLogic__GetTargets` 的**六参重载**（签名见 `d:/2/tools/il2cpp_out/dump.cs:19513`：
   `GetTargets(manager, criteria, thisCard, actingCard, targetCard, checkMaxTargets)`）
   ⇒ `.c:192-202` 的 `ta==0x1e(30)` 分支读 **`param_5` = `targetCard`**（`:202` 把 `param_5` 入表）；`ta==0x14(20)` 走 `:184-189` 读 `param_3` = `thisCard`（自己）。
5. `CardScript__TriggeredSwarm.c:5-7` → `OnTrigger(0x26c, …, actingCard=param_2(被并掉), targetCard=param_3(留下))`；
   `AbilityData__SetNewData.c:6-11` 把三张卡写进 `+0x10 / +0x18 / +0x20` ⇒ **原版把 TL15 的 `+1/+1` 加在留下的 host 上**（ta=30 → targetCard）。

**根因**：本行期望值写于 2026-10-11，依据是「`it` ↔ `actingCard`」的**词面对照**（同函数 doc `RuleEngineTest.cs:35329-35338`）。
2026-11-02 的 `B-314` 用**卡数据**（`ta:[30]`）推翻了那条推断 —— `it` 落到哪一格由 `abilityTargets.ta` 决定，⛔ 不按英文词猜。
本批自己的断言已按新口径钉住同一读数并通过：`ruleengine.log:1015282`
`✓ ★★ ①-a 留下的 host 拿到 TL15 的 +1 [Melee]（2+2 合并 +1 = **5**）`。

**最小修复（只改测试，一行期望 + 文案）**
- `RuleEngineTest.cs:35381`：`4` → **`5`**；
- `35382-35385` 文案改成：「★ A1574：那个「it」= 原版 `targetCard` = **留下的 host**（2+2+1 = **5**）」；
- 自证标记换掉（旧的那句「不传 `subject` ⇒ 5」现在两边都 5，**已失去鉴别力**）：
  「🧨 把 `EffectResolver.cs:10650` 的 seed 改回 `subject` ⇒ 本行实得 **4**」；
- 顺手订正 doc `35329-35338`：「「it」↔ `actingCard`」→「620 的 `it` 由 `abilityTargets.ta=30` 定 = `targetCard`（留下那张）」。
  （`subject = just` 那一半**仍然成立**，⛔ 别一起改。）

**是否本批回归**：**否**。实现侧是本批 `B-314` 的**正确**落地（有本批 ①-a 互证）；红的是过期断言。

---

## 二、`★ A7b 回落时出声点名…不在可玩卡池里` —— **(α) 断言本身错（探针措辞错）**

**断言原文**（`Assets/RuleEngine/Editor/RuleEngineTest_B116W6.cs:177`）：
`CheckTrue(HasLogB116W6(ctx, "不在可玩卡池里"), "★ A7b 回落时**出声**…")`

**证据**：
- 生产侧**有出声**，但措辞是 `EffectResolver.cs:21652-21654`：`「…」第 N 项的原版**选项卡 SO**（id=…）**可玩池与选项 SO 旁表都没有它** ⇒ 按卡面文本结算这一项`。
- 全仓 grep `不在可玩卡池里`：只命中 `EffectResolver.cs:21621`（**注释**）与 `RuleEngineTest_B116W6.cs:177`（断言本身）
  ⇒ 这句话**从来没进过 `ctx.Log`**（`TryRunStructuredOptionOrText` 是本次新增的整块，见 `git diff`）。
- 行为本身正确：A7（手牌 +2）与 A6b（日志含「选项卡 SO」）都 ✓ —— 说明确实走了回落且确实打了日志。

**最小修复**：`B116W6.cs:177` 的 needle 改成 **`"都没有它"`**（或全句 `"可玩池与选项 SO 旁表都没有它"`）；
⚠️ 别反过来去改生产措辞 —— 现在这句同时覆盖两张表，比旧措辞准。

**是否本批回归**：**否**（新测试与 W32 的新措辞没对齐；生产零缺陷）。

---

## 三、`前提：AbilitiesForTrigger(580) 非空且只有 1 条` —— **(δ) 夹具前提不成立**

**断言原文**（`Assets/RuleEngine/Editor/RuleEngineTest_B116W19.cs:189-190`）：
`var before = card.AbilitiesForTrigger(580); CheckTrue(before != null && before.Count == 1, …)`

**证据**：
- `AbilitiesForTrigger` 只数 `_abilityTargets` 里 `s != null && s.Trigger == 580` 的行
  （`Core/CardDefPartial_B94B16.cs:181-191`）。
- 而夹具 `B94Z2Fixture`（`Editor/RuleEngineTest_EngineB94Z2.cs:261`）造的是 **`new AbilityTargetSpec[triggers.Length]` = 全 null 占位**（`:270` 「null 占位也留着」是有意的：`[i]` 要与 `cardAbilities[i]` 对齐）⇒ `Count == 0`；
  `AbilityTargetsVersioned` = `abilityTriggersVersioned` = true（`CardDef.cs:1120`）⇒ 返回的是**空表**（不是 null）。
- 真卡池无此问题：现读 `cards_engine.json` 1,129 张 / 1,273 行 `abilityTargets`，**null 行 = 0** ⇒ 纯夹具问题。
- 连带后果：`C3`（`B116W19.cs:201-204` 比 `before.Count` 与 `after.Count`）在 0 对 0 下**恒绿**——本条「灭自证」现在是**空断言**。

**最小修复（夹具加一个可选形参，默认行为不变）**
1. `RuleEngineTest_EngineB94Z2.cs:261` 末尾加 `CardDef.AbilityTargetSpec[] abilityTargets = null`，
   `:270` 改 `var ta = abilityTargets ?? new CardDef.AbilityTargetSpec[triggers != null ? triggers.Length : 0];`
   （现读所有调用点都用命名实参 ≤ `contextRow`，加尾部可选参零影响）。
2. `RuleEngineTest_B116W19.cs:188` 的夹具改成
   `B94Z2Fixture("W19T3", 0, new[]{580}, new string[0], body: "Strike: Draw 1 card", abilityTargets: new[]{ new CardDef.AbilityTargetSpec { Trigger = 580 } })`。
   这样 `before.Count == 1`、`C3` 变回真断言，且 `AssignAbilityTargets` 那侧是惰性的
   （`TargetsAffected` 为 null ⇒ `EffectText.cs:2104/2124` 两条分支都不进）。
3. ⛔ **别改 `AbilitiesForTrigger` 把「全 null 占位」判成 null** —— 真卡池 0 行，改了只是给手搓夹具加口径。

**是否本批回归**：**否**（新文件，前提从未成立；同一段的 C1/C2/D1-D 全绿）。

---

## 四、§四（顺手发现，不在这 3 条内）：`B116W27_Assert` 的 C1 / D1 / D4 也是红

- `B116W27_Assert.cs:257` `Eq(n,1)` 实得 2 · `:298` 实得 2 · `:314` `Eq(n2,0)` 实得 1。
- 根因（读码，未跑）：`DispatchCardAbilityRows` 在**战术路**上除了 `ca 380` 还会派 **`ca 320`**
  （`Core/CardAbilityDispatch_B116.cs:80` 的 B5 + `:156-157` 的 380/320 两跳，执行体 `:236` 返回 1）。
  GOF104 的 `trigger 0` 三行 = `ca380 / ca260 / ca320`、AM54 = `ca0 / ca380 / ca320`（`cards_engine.json` 现读）⇒ 派 2 行**符合本层自己 doc**，断言的「只有这 1 行归本层」写错了；
  D4 同理：`380` 因目标表空不结算（D5 已绿），但 `320` 那一拍照发 ⇒ 1 而不是 0。
- 最小修复：C1 → `Eq(n,2)` 并注明「380 + 320（B4/B5）」；D1 → 2；D4 → 1（文案改成「380 不结算、320 照发」）。
- 该结论是**读码推断**（本代理不跑 Unity），落地时要复跑宿主确认。

---

## 汇总

| # | 断言 | 四档 | 本批回归 | 能立刻修？ |
|---|---|---|---|---|
| 1 | `A1574` host 期望 4 | **(α) 断言本身错**（期望值过期；原版 `ta=30` ⇒ `targetCard` ⇒ 5） | 否 | ✅ 改期望 4→5 + 改文案 + 换自证标记（判据已齐） |
| 2 | `A7b` 日志 needle | **(α) 断言本身错**（needle 与生产措辞不符；生产有出声） | 否 | ✅ 一行：`"不在可玩卡池里"` → `"都没有它"` |
| 3 | `AbilitiesForTrigger(580)` 前提 | **(δ) 夹具前提不成立**（`abilityTargets` 是全 null 占位 ⇒ 空表；C3 被架空） | 否 | ✅ 夹具加可选参 + 断言夹具喂真 spec |
| 附 | `B116W27` C1/D1/D4 | **(α) 断言本身错**（漏数 `ca 320` 那一行） | 否 | ✅ 三个期望值（需复跑宿主确认） |

**最该先改哪条：`A1574`。** 它与本批新断言 `B116W12` ①-a（host = 5）**正面矛盾**，留着会让下个会话把正确的 `B-314` 实现判成回归、甚至把 `EffectResolver.cs:10650` 改回去（那样 TL15 的 `+1/+1` 会继续落在已离场卡上 = 整半句消失）。
**先改判据口径（doc 35329-35338）、再改期望值**；三条都不需要动生产代码。
