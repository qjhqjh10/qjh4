# B124_W6 —— `Deffkopta` 自挂附魔双发（全套 171 红里唯一一条实锤 β）

写手：W6 · 2026-11-03 · 批 B124 · 直属调度台

## 一、根因链复核（逐层现读，与简报一致，**无需重新分诊**）

| 层 | 事实 | 出处 |
|---|---|---|
| ① 原闸 | 只比**附魔卡自己**的正文：`var eops = e.Card.TriggerOpsByNumber(triggerNumber); if (eops == null \|\| eops.Count == 0) { RunEnchantmentLogicRows(...) }`。模板正文恒空 ⇒ **必然放行** | `EffectResolverPartial_B109E6.cs:121-122`（改前） |
| ② 自挂点 | `buffType 12/15/16` ⇒ `Enchantments.Attach(ctx, carrier, tmpl, …, source: who, …)` | `EffectResolverPartial_B97W2.cs:755`（数据入口 `:491`） |
| ②′ 载体是谁 | `Enchantments.AttachGrantedByPlayedCard`：`carrier = chosen.Instance`、`source = playedInst.Card.Name` ⇒ **单位部署时载体 = 它自己**，`Source == 载体的卡名` | `Enchantment.cs:1654-1657`（闸在 `:1629/:1643`） |
| ③ 数据 | `GOF56` 的 `enchantment` 列 = `{trigger 0, buffType 15, ta 10, token c40a0661…}` → 模板 `28 Goff-Deffkopta_enchantment`，唯一 ability = `trigger 0 / ca 10 / damage {2,2}`（= 卡面「Rally: Other friendly Deffkopta deal 2 damage」的另一种表示） | `cards_engine.json` / `enchantment_templates.json` 现读 |
| ④ 回归 | `RunDoDamageRowOfEnchantment` 2026-11-03 02:59 才落地 ⇒ 15:24 基线时模板行只出声不结算 | `EnchantmentExecutor_B115.cs:860` |

实测日志（`_tmp_view/ruleengine_b123c.log:825690`）：正文路 `eachunitdeal` 打 2 ＋ 模板路 `Deffkopta·Deffkopta·trigger#0` `ca 10` 打 2 = **4**（断言期望 2，`RuleEngineTest.cs:8774`）。

## 二、改了什么（只有一个文件：`RuleEngine/Core/EffectResolverPartial_B109E6.cs`）

**① 闸（`:122`，同物理行内替换，零行数变化）**

- 改前：`if (eops == null || eops.Count == 0) { RunEnchantmentLogicRows(ctx, e, u, owner, slot, triggerNumber, tn, actingCard, targetCard); continue; }`
- 改后：`if (eops == null || eops.Count == 0) { if (CarrierBodyCoversSelfEnchantment(e, u)) continue; RunEnchantmentLogicRows(...); continue; }`

**② 上方注释 `:120`（同一行内追加，零行数变化）** —— 补一句「正文为空时还要先过双发闸第二档」。

**③ 类尾新增** `CarrierBodyCoversSelfEnchantment(ActiveEnchantment e, UnitState u)` ＋ 只喊一次闩 `_b124w6SelfEnchantCovered`
（判法：`e.Effect.Source` **逐字等于** `u.Card.Name`（自挂）**且** `u.Card` 在 `CardDef.RoutableTriggers` 任一触发词上有解析出的 op ⇒ 模板行让路；
让路时 **`UnityEngine.Debug.LogWarning` 出声一次**（⛔ 不用 `ctx.Log`：它进 `ctx.Events`，会改 `Fingerprint` / 让 P2P 两端分叉））。

**为什么「载体的正文」不能按 trigger 逐格判**（关键发现）：样本里正文那条 `eachunitdeal` 挂在 `Rally`（`trigger 81`）、模板那条 `ca 10` 挂在 `trigger 0`
⇒ `GOF56.TriggerOpsByNumber(0)` **为空**（`MapTriggerOpsByNumber` 按 `op.AbilityIndex == row.Ab` 分桶，rally op 落 ab0/trigger81 桶，ab1 无正文）
⇒ 按 `TriggerOpsByNumber(triggerNumber)` 比会**两边都对不上**、这条闸等于没加。故改用「这张卡的正文整体能不能表达效果」这一问（`TriggerOps(kw)` 口径，不依赖 `AbilityIndex`）。

## 三、行数变化（铁律 18·b）

- `:120` 与 `:122` 各为**同一行改写** ⇒ 文件前半段**零行数变化**；`:69`（账 A 档落点 `FireEnchantmentAbilitiesByNumber`）与 `:162`（账里的次处）**行号一个不动**（`git diff -U0` 实测：`@@ -120 +120 @@` / `@@ -122 +122 @@`）。
- 类尾追加：`@@ -200,0 +201,73 @@` ⇒ **202 → 275 行（+73）**，全部在类尾（原类尾已是「本文件此后没有成员」的自检门之后）。
- 行尾：改前改后均 **LF / 0 个裸 CR**（二进制现读）。

## 四、typecheck

`TMPDIR=/tmp/wf_b124w6 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。

⚠️ **端到端未验**：`RuleEngineTest.Run`（Unity 宿主）归调度台，本代理⛔ 不跑 Unity / ruleprobe ⇒ 「`:8774` 由 4 变 2」这条只有静态判据（数据 + 调用链），需在同步点复跑该宿主确认。
⚠️ **预期副作用**：真战斗里部署 `Deffkopta` 少打 2 点 ⇒ `ruleprobe play` 的指纹/基线会变（归调度台重钉，与 `B109_E6` 那次同一类）。`ctx.Events` 未被写入（用的是 `Debug.LogWarning`）⇒ P2P 两端不会因此分叉。

## 五、三条满足性自检

1. **① 正文能表达的效果不被模板再算一遍** —— 成立：`GOF56` 走 `Source=="Deffkopta"` ∧ 正文有 `rally` op ⇒ `continue`，模板 `ca 10` 一次都不跑；伤害只剩正文那 2 点。
2. **② 模板独有的行仍由模板跑** —— 成立且有边界：闸的两道条件（`Source == 载体卡名` ∧ 载体正文非空）任一不成立就**照原样跑模板**。
   - 他挂的附魔（战术卡挂给被选中的单位、`source` 是战术卡名）⇒ 条件一不成立；
   - 载体正文解析不出来（0 个 op）⇒ 条件二不成立；
   - 现读**爆半径 = 1 张**：54 张带 `enchantment` 行的卡里只有 `GOF56` 是**单位**且在 `trigger 0` 自挂（SOR 那批单位在 `655/180` 挂，不走 `AttachGrantedByPlayedCard`；`type 20` 全是战术卡）。
   - ⚠️ **残余（记账用，未修）**：若某卡正文只表达它自挂附魔的**一部分**，本闸是「整件让路」而非逐行让路；逐行必须在 `RunEnchantmentLogicRows`（`EnchantmentExecutor_B115.cs`）里判，**那个文件不在本代理白名单**。
3. **③ 不改变 `OtherThanSelf` 等目标筛选语义** —— 成立：本改动只决定「模板行跑不跑」，目标集一律仍由正文路的 `DoEachUnitDeal`（`EffectResolver.cs:3656`）与附魔路的 `ResolveEnchantmentTargets` 各自决定，两处一行未动。

**灭自证（报告要求 4）**：修完后那条断言仍能区分「修对了」与「把 `OtherThanSelf` 删了」——
① 修对 ∧ `OtherThanSelf` 在 ⇒ **2**；
② 修对 ∧ 删 `OtherThanSelf` ⇒ 正文路打**自己 + 别的** = **4**（红）；
③ 不修（双发）⇒ 4（红）；④ 把本方法的 `return true` 改成恒 `false` ⇒ 回到 4（红）。
⇒ 断言现在**只测正文路的目标筛选**，不再被附魔路的第二份 2 点污染，判别力比修前更强（修前「4」既可能是双发、也可能是过滤器坏了，两者无法区分）。

## 六、顺手发现（⛔ 未动手，交调度台分流）

1. **`ActiveCardEffect.Source` 语义不统一**：`AttachGrantedByPlayedCard` 传的是**卡名**，而 `ApplyAbilityBuff` 那条路传的是**日志标签**（`who`/`by`）。⇒ 任何「把 `Source` 与卡名比」的判据**只对「打出即挂」这一条路成立**。本次闸只用在这一条路上（且用 `Ordinal` 全等），但它是个静默陷阱，值得立账。
2. **`AttachGrantedByPlayedCard` 的载体选择可能与原版不符（`ta=10` 行）**：GRANT 行 `ta 10` 我们一律挂到 `chosen`（部署时=自己）；分诊报告 `B124_对账_A_引擎残余.md:90` 的口径是「打出时给**每只别的** Deffkopta 挂一件单次附魔」。若那个口径成立，则真正的修法是**挂载端按 `ta` 选载体**，本次的让路闸只是把症状压掉。`B124_对账_A_引擎残余.md:116` 已记「修 (a) 还是 (b) 需实测」，本代理无实况判据，未动。
3. **`GOF56` 的 `abilityDamage ab1/lg0 = {min 1,max 3,opt 90}` 与卡面「2 点」不符**（`B124_对账_A_引擎残余.md` §九·4 已立账），仍未修 —— 一旦接 `abilityDamage` 的结构化消费，Rally 会变成 1–3。本闸不涉及。
4. **账的次处落点 `:162` 早已偏 3 行**（现读 `:162` 是 `///` 空注释行，`EnchantmentHasAbilityForTrigger` 的**声明在 `:165`**；`git show HEAD` 同样如此）⇒ 与本次改动无关，属既有漂移（SECOND 档，只告警）。
5. **`eops` 那一档仍只按「附魔卡自己」取 `TriggerOpsByNumber`**，而模板行实际来自 `TriggerAbilityRowsOf`；两者口径不同（前者=正文 op，后者=数据行）属设计如此，但措辞容易误导后来者「模板有正文」—— 已在 `:120` 注释里点了一句。
