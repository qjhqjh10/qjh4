# 断言批 · 规格 A 组（第十三会话 6 份写手报告 → 可照写的断言清单）

> 2026-10-10 第十四会话。**只读普查**：读第十三会话 6 份写手交件，把它们各自写的「该补什么断言（含灭自证那一半）」
> 抽成一张**照着就能在宿主里写代码**的表。⛔ 本件没改任何生产代码 / 文档 / 正本，没跑 Unity，没动 git。
>
> **出处简写**（下表「出处」列用）：`W1` = `资料/普查产出_第十三会话/W1_引擎族六笔.md` ·
> `W2` = `…/W2_A1311代词目标补侧.md` · `W4` = `…/W4_A1335A1336.md` · `W5a` = `…/W5a_A1331修Oath前缀.md` ·
> `W5b` = `…/W5b_A1332收口.md` · `W5c` = `…/W5c_A1330收口.md`。
> **宿主**：`RET` = `Unity/MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs` · `BS` = `CardPresentation/Editor/BattleScene.cs` ·
> `MM` = `CardPresentation/Editor/MainMenuScene.cs` · `CS` = `CardPresentation/Editor/CollectionScene.cs`。

## 一、读了哪 6 份（行数现读 · 有没有「该补什么断言」这一节）

| 报告 | 行数 | 有没有「该补什么断言」一节 | 抽出行数 | 宿主 |
|---|---|---|---|---|
| `W1_引擎族六笔.md` | 281 | ✅ 有（六笔逐笔一节） | 16（14 条断言 + 2 行「不补 / 不重复补」） | `RET` |
| `W2_A1311代词目标补侧.md` | 305 | ✅ 有（断言 1~7，给了夹具与灭自证） | 7 | `RET`（建议新开 `TestA1311PronounTargetSide`） |
| `W4_A1335A1336.md` | 245 | ✅ 有（`A1335` 六组 + `A1336` ①②③ 各一节） | 12（11 条断言 + 1 行「不建议补」） | `RET` |
| `W5a_A1331修Oath前缀.md` | 139 | ❌ **没有这一节**（只有一句「收口那趟带上 `RuleEngineTest.Run`」） | 0 | —— |
| `W5b_A1332收口.md` | 166 | ✅ 有（1~5，含宿主建议与夹具） | 5 | `BS`（④·B 段，既有 4 条在 `:822-855`） |
| `W5c_A1330收口.md` | 468 | ✅ 有（逐处「该补什么断言」+ 共用 A/B/C 三套夹具的完整写法） | 14 | `MM`（`#1`–`#9`）· `CS`（`#10`） |
| 合计 | 1604 | 5 份有 · **1 份没有（`W5a`）** | **54** | —— |

> ⚠️ `W5a` 那 0 条是真的**:那份报告通篇只写「改了什么 / 怎么验 / 还欠什么」，没有断言节**（详见 §三·`W5a` 段，那 4 张卡的运行时行为它如实标着「没实跑验证」）。
> ⚠️ 行号一律**照报告里写的**（报告自己声明是「交件那一刻的现读」）；行号会随后续改动漂 ⇒ 落盘时**按符号名现读**。

## 二、断言表（一行一条；「灭自证那一半」= 结构上不可能与被测实现同时满足的那条）

| # | 断言该断什么（一句话） | 宿主文件 | 被测对象／落点（类.方法 / 文件:行号） | 灭自证那一半 | 出处（报告:行号） | 前置（夹具／临改常量） |
|---|---|---|---|---|---|---|
| 1 | 幸存者救回：目标仍在场上同一格、`survivor` 摘掉、`survivorspent` 挂上、`Health==3`、不进弃牌堆／`ctx.DeadUnits`／`ctx.DiedThisTurn==0`、**无** `EvtKind.Death`、有**一条** `EvtKind.Hit` 且 `Amount == -3` | `RET` | `RuleCore.CleanupDeaths`（幸存者支路）`:4983-4997` · `UnitState.UseSurvivor()` `:350` | 必须断 `ReferenceEquals(原来那个 UnitState, 现在那一格上的 UnitState)` —— 防「翻面成残骸／新建对象」被误读成「救回」 | `W1:36-38` | 夹具：敌方 2 血单位 + `u.AddKeyword("survivor", 3)`；输入＝我方打 5 伤（`DeclareAttack` 或直调 `Hurt`） |
| 2 | 摧毁路绕过幸存者：同一单位走 `Destroy an enemy troop` ⇒ **照旧离场**（弃牌堆／残骸）、`survivor` **一个都没被消耗** | `RET` | `EffectResolver` 摧毁 op（`DoDestroy`）+ `RuleCore.CleanupDeaths` 默认 `maySurvive:false` `:4956` | 把调用点那句 `maySurvive &&` 去掉 ⇒ 第 1 条**必须变红**（两条不可能同时满足） | `W1:39-41` | 同第 1 条夹具，输入换成摧毁 op 那一路 |
| 3 | 狂喜读**印刷**关键词：卡面印 `Ecstasy 2: …` ⇒ 挨一拳打到 ≤2 且 >0 **触发**（攻 +1） | `RET` | `RuleCore.Hurt` 狂喜两闸 `:4748/:4760`（`u.Card.Has(Ecstasy)`） | 格首先断 `u.Has("ecstasy")==true` 前提（否则「触发」可能是关键词压根没挂上） | `W1:42-43` | 夹具 A：卡面印 `Ecstasy X`；输入＝打到 ≤2 且 >0 |
| 4 | 运行时**被授予**的 `ecstasy` **不触发** | `RET` | 同第 3 条 | 先断 `u.Has("ecstasy")==true`（前提成立）—— 否则「不触发」可因没挂上而假绿；与第 3 条并排 | `W1:42-43` | 夹具 B：卡面**不印**、运行时 `AddKeyword("ecstasy", 1)` |
| 5 | 【不补】`A1167` 是**纯注释、零行为改动**；报告明确建议**不要**去断「`IsOver` 时仍跑完再生／中毒／残骸／摘闸门」（那会把我方结构固化成断言） | —— | `RuleCore.cs:1473-1493`（注释） | —— | `W1:64-65` | —— |
| 6 | 【不重复补】`A1218②` 的断言＝**上面第 1、2 条那一组**（别写第二份） | —— | `RuleCore.CleanupDeaths` `:4956` + 调用点 `:4792`／`:5336` | —— | `W1:79` | —— |
| 7 | 【③′a】`Deal 2 damage to an enemy and Stun it` 打**带 `Remnant` 的 2 血单位** ⇒ 晕挂在**棋盘上占位的那一个**（翻面后的残骸）上 | `RET` | `EffectResolver.DoStun` 代词映射段 `:2179-2199` · `RuleCore.BoardOccupantByInstance` `:3069` | 断 `ReferenceEquals(被晕的那个对象, 翻面后的残骸)` **且** 那一格上的对象**不是** `ctx.LastTarget`（只断 `t.IsStunned` 改前改后都假绿）；用 `FindSlot` 反查 | `W1:107-108` | 夹具：战术卡「Deal 2 damage … and Stun it」+ 敌方 2 血 `Remnant` 单位 |
| 8 | 【③″】同一张卡打**不带** `Remnant`／`Waystone` 的单位（真死）⇒ 敌方场上**一个被晕的都没有** | `RET` | `RuleCore.StunStillOnBoard` `:3029-3055`（分支②判据 `IsRemnant`→`IsAlive`） | **本组判别式**：把 `ResolveTargets` 的 `prev` 支直接放开 `IsAlive` ⇒ ③′ 与 ③″ 会**一起变绿**；③″ 在才挡得住「一把梭」 | `W1:109-110` | 同第 7 条夹具，目标换成不带 `Remnant`／`Waystone` |
| 9 | 【③‴】把 `Waystone` 换成 `Remnant` ⇒ **同结果**（原版是一条支路，别只修死灵那一半） | `RET` | 同第 7、8 条（`HasToTransformIntoRemnant = remnant ∨ waystone`） | —— 报告未给 | `W1:111` | 同第 7 条夹具，`Remnant` 换 `Waystone` |
| 10 | 【④】同场挂 `When an enemy receives a Stun, gain +1 Attack` 的观察者 ⇒ 断它 **+1** | `RET` | `EffectResolver.BroadcastKeywordEvent(GetsStun)`（`DoStun` 段） | —— 报告未给（它是「把广播那半钉住」的唯一条，不补则永远无人看管） | `W1:112` | 夹具：再加一个带该观察者正文的单位 |
| 11 | `ApplyDeployTurnState(u)`（默认 `keepStatus=false`）⇒ `u.SummonSickness == true` | `RET` | `RuleCore.ApplyDeployTurnState` `:2037` | —— 见第 12 条 | `W1:128` | 夹具：`new UnitState(card, false)`（`SummonSickness` 默认 `false`） |
| 12 | **先把 `u.SummonSickness` 摆回 `true`**，再 `ApplyDeployTurnState(u, keepStatus: true)` ⇒ **仍是 `true`** | `RET` | 同第 11 条 | **就是「先摆回 `true`」这一句**：不摆的话 `keepStatus:true` 与「写 0」两种实现都得到 `false`，测不出「原版是条件写」 | `W1:129-130` | 同第 11 条，并把 `SummonSickness` 预置为 `true` |
| 13 | `ApplyDeployTurnState(u, keepStatus: false)` ⇒ `SummonSickness == true`（挡「把参数接反」） | `RET` | 同第 11 条 | 与第 12 条并排 | `W1:130` | 同第 11 条 |
| 14 | 毒：**A 方**回合末 ⇒ 该单位被摧毁（进弃牌堆／`ctx.DiedThisTurn` +1／发 `EvtKind.Death`）；**B 方**回合末 ⇒ **不摧毁** | `RET` | `RuleCore.EndTurn` 毒支 `:1330-1405` | **灭自证＝「B 方回合末不摧毁」那一格**：只断「A 方摧毁」的话，删掉 `param_2 == isPlayer` 那条守卫仍全绿；还必须在同格先断 `u.Has("poisoned")==true` | `W1:151-155` | 夹具：A 方场上一个 `AddKeyword("poisoned", 1)` 的单位 |
| 15 | 毒 + `resistant` ⇒ **两个回合末都不摧毁**，且日志里有那条豁免 | `RET` | 同第 14 条（三道守卫里的 `resistant == 0`） | 同第 14 条（先断 `Has("poisoned")` 与 `Has("resistant")` 都为真） | `W1:156` | 同第 14 条夹具再加 `resistant` |
| 16 | 「毒支**不跳**下面的块」：`resistant` + `stun` + `StunnedAtStartOfTurn` 的单位断「**摘闸门照跑**」 | `RET` | `RuleCore.EndTurn`（毒支排在再生之后、`DestroyRemnants` 之前） | ⚠️ 这一格用行为断言**不好钉**（毒支排在摘闸门之前、人先没了）——另一半**只在注释里留判据** | `W1:157-158` | 夹具：同时带 `resistant`／`stun`／`StunnedAtStartOfTurn` 的单位 |
| 17 | 解析层：`Parse("Take control of an enemy troop this turn and give it Fast")` ⇒ `ops.Count==2` · `ops[1].Target.Side=="prev"` · `Kind=="prev"` · **`PrevSide=="enemy"`** · **`ResolvedSide=="enemy"`** | `RET`（建议新开 `TestA1311PronounTargetSide`） | `EffectText.EffectTargetSpec` `:818`／`:832` · `LinkPrevAntecedent` `:1167` | 同时断 `ops[0].Target.Side == "enemy"`（先行词本身）—— 若 `PrevSide` 改成从别处取（如「永远 own」），这一对**不可能同时绿** | `W2:165-171` | 无（纯解析，不需棋盘夹具） |
| 18 | 解析层反面：`Parse("Draw a troop and give it +1 [Attack]")` ⇒ `ops[1].Target.Side=="prev"` **且** `PrevSide == null`（不许编一个侧） | `RET` | 同第 17 条 | 与第 17 条并排 —— 「一切代词都填 `own`」的实现会在这一行红 | `W2:173-178` | 对应真卡 `GOF_Da_Red_Waaagh`（同形还有 `SW13`／`UM23`／`TAU54`／`GOF50`） |
| 19 | 解析层连环顺延：`Parse("Deal 1 damage to a friendly troop and give it Armour 1. 2 : Give it Armour 2 instead")` ⇒ `ops[1].PrevSide=="own"` **且** `ops[2].PrevSide=="own"` | `RET` | `EffectText.LinkPrevAntecedent` `:1167`（① 抄侧 ② 抄种类） | 去掉「顺延」那一句 ⇒ `op[2]` 变 `null` ⇒ 红，而 `op[1]` 仍绿（能分清是哪一半坏了） | `W2:180-184` | 对应真卡 `SOR49 Trial of Suffering` |
| 20 | 解析层**幂等**：`Parse(desc)` 连跑 **3 次**，逐个 op 断 `PrevSide／Side／Kind` 三次一致 | `RET` | `EffectText.LinkPrevAntecedent`（`Parse` 会反复调它） | —— 报告未给（`LinkPrevAntecedent` 被反复调，一红就是**静默错**）；本次实测全池 0 张不幂等 | `W2:186-188` | 无 |
| 21 | 打分层（本账验收）：`ScoreGivingCharge(ctx,0,3,"enemy")==7f` · `ScoreGivingCharge(ctx,0,3,"own")==4f` · `ScoreOps(ctx,ops,3,slotIsTarget:true) − PayloadScore("fast") == 7f` | `RET` | `SimpleAI.ScoreGivingCharge` `:965` · `ScoreOp` `give/gain` 支 `:865` | **「同一格号、两边值不同（7 vs 4）」就是判别式**：任何常量或读错侧的写法不可能同时绿；⛔ 别把两个单位放成同一个 `attack`（会退化成恒真） | `W2:190-200` | 夹具：`RuleCore.NewBattle`，`ctx.Active=0`，**同一格号 3** 放我方 `attack=4`／敌方 `attack=7`，两边都 `Exhausted=true` + `SummonSickness=true`（照 `Place` 那套，别绕过） |
| 22 | 打分层：判不出侧就不给分 —— `ScoreGivingCharge(ctx,0,3,"any")==0f` · `(ctx,0,3,null)==0f`；**反面** `(ctx,0,3,"own")==4f`（不是 0） | `RET` | 同第 21 条 | 反面那条挡「一律返 0」把本条改成恒真的实现 | `W2:202-206` | 同第 21 条夹具 |
| 23 | `Target == null` 的 op 走 **3 参重载（旧口径＝读施放者自己那侧）** ⇒ `ScoreGivingCharge(ctx,0,3)==4f`；并排一条 `Target==null` 的 `give fast` op 经 `ScoreOps` 仍含那一档加分 | `RET` | `SimpleAI.ScoreGivingCharge` 3 参重载 `:984` · `ScoreOp` `if (op.Target == null)` 支 `:865` | **两条并排、结构上不可能同时满足**：`(ctx,0,3)==4` 与 `(ctx,0,3,"any")==0` —— 任何「把两种 null 合成一种」的实现必有一条红；⛔ 别只断 `==4f`（对「一律返 4」也绿） | `W2:208-224` | 夹具：照 `TestA1098SummonSicknessReads` 的 Ⓒ 那格（`ctx.Active=0`，我方 `Board[3]` 疲惫+召唤病、`attack=4`），**对面 `Board[3]` 另放 `attack=7`** |
| 24 | 堡垒吃掉整份伤害：返回 **0**、`t.Health == hp0`（= 打之前那个数）、`KwValue("bastion")==1`、**恰好一条** `EvtKind.Hit` 且 `Amount==0` | `RET` | `RuleCore.ApplyDamage` `:4639` + 堡垒段 `:4751-4777` | 格首记 `int hp0 = t.Health;` 并断 `t.Health == hp0` —— 只断「没死」的话，「扣掉再补回来」也假绿 | `W4:43-47` | 夹具：敌方 **5 血**单位 + `AddKeyword("bastion", 3)`；输入 `ApplyDamage(ctx, t, 2, "自检")` |
| 25 | 堡垒被打穿 ⇒ **只有溢出**打到生命：返回 **2**、`Health==3`、`KwValue("bastion")==0` 且 `Has("bastion")==false`；边界（输入 3、与堡垒打平）⇒ 返回 0、血不变、`Has("bastion")==false` | `RET` | 同第 24 条（`:4751-4777`） | **本笔判别式**：溢出写错（如 `actual = dmg`）时第 24 条仍绿、**本条必红** ⇒ 两格不可能被同一种错实现同时满足 | `W4:49-53` | 血 5 + `bastion 3`；输入 5，再补一格输入 3 |
| 26 | 反击不吃堡垒：`DeclareAttack` 后**攻击者掉 4 血**、`KwValue("bastion")` **仍是 5** | `RET` | `RuleCore.cs:4075` 反击那一跳传 `ignoreBastion: true`（+ `Hurt` 透传 `:4877`） | 把 `ignoreBastion: true` 删掉 ⇒ **本条必红**（攻击者掉 0 血、堡垒变 1）；且它与第 24/25 条走**不同入口**（`DeclareAttack` vs 直调 `ApplyDamage`），两处一起改不会一起绿 | `W4:55-59` | 夹具：我方攻击者（`bastion 5`、近战攻击 1）、敌方目标（3 血、攻击 4、无堡垒） |
| 27 | 空投舱血池三格：① 打 3 ⇒ 返回 0、`Health==5`、`KwValue("droppod")==1`；② 再打 3 ⇒ `Has("droppod")==false` **且血仍 5**、返回 0；③ 再打 2 ⇒ 这次落生命 ⇒ `Health==3`、返回 2 | `RET` | `RuleCore` 空投舱血池段 `:4729-4749` | **灭自证＝第 ② 格**：只断 ①③ 的话，删掉「池 < 1 就摘掉」仍全绿（池变 −2 没人看）⇒ 必须**同时**断「摘掉了」与「血一点没动」 | `W4:61-66` | 夹具：敌方 5 血单位 + `AddKeyword("droppod", 4)` |
| 28 | 空投舱**优先于**堡垒：同单位打 3 ⇒ `KwValue("bastion")==5` **不变**、`Has("droppod")==false`、`Health` 不变 | `RET` | `RuleCore` 两段 `if` 的次序 `:4729` 与 `:4751` | 把这两个 `if` 的**次序对调** ⇒ 本条红而第 24/25/27 条**全绿** | `W4:68-72` | 夹具：同一单位同时 `droppod 2` + `bastion 5`；输入 3 |
| 29 | （建议、非必须）易伤 + 堡垒：`vulnerable 2` + `bastion 3`、5 血、打 3 ⇒ `Health==5`（易伤一份也不该落地） | `RET` | `RuleCore.ApplyDamage`（易伤折进 `DamageAfterReduction`） | —— 报告未给；⚠️ **只能断这一种**（「打穿且血还 > 0」那一档我们会少算，⛔ 别写成期望） | `W4:74-75` | 夹具：`vulnerable 2` + `bastion 3`、5 血、打 3 |
| 30 | `sacrifice`：**我方**回合打 5 伤救回 ⇒ 攻 **+1**、`survivor` 被消耗、留场、`EvtKind.Trigger` 里有一条 `Keyword=="sacrifice"` | `RET` | `RuleCore` sacrifice 那一跳 `:5099-5122`（在 `UseSurvivor` 之前） | 见第 31、32 条（这两格是判别式） | `W4:98-104` | 夹具：敌方 2 血单位 + 卡面印 `Sacrifice: Gain +1 Attack` + `AddKeyword("survivor", 3)`；**每格格首先断 `Has("sacrifice")==true`** |
| 31 | **敌方**回合打 5 伤 ⇒ 救回照旧（血 3、`survivorspent` 为真）但 `sacrifice` **不触发**（攻不变、无 Trigger 事件） | `RET` | 同第 30 条（`ctx.Active == p` 那道闸） | 把 `ctx.Active == p` 删掉 ⇒ **本条必红**（第 30 条仍绿） | `W4:101` | 同第 30 条夹具，把 `ctx.Active` 摆成对面 |
| 32 | **没有** `survivor`（真死）⇒ 照旧离场、`sacrifice` **一次都不触发** | `RET` | 同第 30 条（`ResolveDeadCard`／真死那一支） | 把这一跳挪到「真死」那一支 ⇒ **本条必红**；它是「**位置**」这件事的唯一判别式 | `W4:102-103` | 同第 30 条夹具，去掉 `survivor` |
| 33 | `survivor` 触发 id：同一张卡**再印** `Survivor: Gain +2 Attack` ⇒ 打 5 伤后 `EvtKind.Trigger` 里有一条 `Keyword=="survivor"` **且**攻 **+2**，且它在事件流里**排在 `sacrifice` 那条之后** | `RET` | `RuleCore` survivor 触发那一跳 `:5146`（`UseSurvivor()` 之后） | 把这一跳删掉 ⇒ 本条红、而第 30~32 条**仍绿**（「数值」与「触发」是两个独立判据，别合并）；⛔ **不能只断 `Has("survivor")==false`**（那是 `UseSurvivor` 的效果，与本条无关） | `W4:120-124` | 同第 30 条夹具，卡面再印一条 `Survivor: Gain +2 Attack` |
| 34 | 「**没有正文也要表态**」：卡面**只印 `Survivor 3`、无 `Survivor:` 正文** ⇒ **仍有** `EvtKind.Trigger{survivor}` | `RET` | `RuleCore.FireTriggerAlways` `:6716-6750`（两个调用点用它而**不是** `FireTriggerAt`） | 把这一跳换回 `FireTriggerAt` ⇒ **本条必红**（没正文时不发事件），而第 33 条（有正文）两边都绿 ⇒ 两格合起来才钉得住这个新口 | `W4:125` | 夹具：卡面只印 `Survivor 3`、无 `Survivor:` 正文 |
| 35 | 【不建议补】别为「帧／徽标绑没绑上」补断言 —— 它断的是表现层**既有通用链路**，而 `survivor`／`sacrifice` 我们**没有**可绑的帧 ⇒ 断它只会把「没绑定」固化成期望 | —— | `BattleDriver.PlayTraitPulse` `:10621` · `TraitFrames.TriggerFrames` `:78-91` | —— | `W4:142-144` | —— |
| 36 | 全池不变量（最重要的一条，常驻探针）：对全 1126 张卡每个 `(k,v)` 断 `Badges.CarriesValue(k, v, null) == Badges.CarriesValue(k, v, c.NumericKeywords)`，**并带计数下界** `> 150`（实测 203） | `BS`（④·B 段，既有 4 条在 `:822-855`） | `Badges.CarriesValue` · `CardText.KeywordSegment` `:241` | **判别式**：右边**必须真的传 `c.NumericKeywords`** —— 两边都传 `null` 就退化成同义反复（同实参同函数体恒等，永远绿） | `W5b:101-108` | 夹具用现成的 `RuleEngine.CardDatabase.Load()`（该文件 `:826` 已有用法） |
| 37 | 改动的判别式：`CardText.KeywordSegment(new Dictionary<string,int>{{"flying", 2}}, false, "")` ⇒ `Contains("Flying 2")` | `BS` | `CardText.KeywordSegment` `:241`（实参从 `CarriesValue(key)` 改成 `(key, kv.Value, null)`） | 拿单实参 `CarriesValue(key)`（默认值 1 ⇒ 只走 ③ 表）写就红 —— 本条**只能**靠 `kv.Value` 接进 ② 才过 | `W5b:109-112` | 合成输入（全池 0 张卡是这种形态），测的是判据本身 |
| 38 | 反向闸：`KeywordSegment({{"flying",1}}, false, "")` ⇒ **不含** `"Flying 1"`（且不含任何数字）；`CarriesValue("Flying", 1, null) == false` | `BS` | 同第 37 条 | 挡住「干脆把兜底 1 也印出来」 | `W5b:113-115` | 合成输入 |
| 39 | `numericKeys` 那一档：`CarriesValue("Foo", 1, new[]{"foo"}) == true` **且** `CarriesValue("Foo", 1, null) == false` | `BS` | `Badges.CarriesValue`（`Badges.cs:318-325` doc 段） | 把「① 只在徽标那一档生效」钉成**已知差异**（不是漏） | `W5b:116-117` | 无 |
| 40 | 判据真的把 `value` 读进去了：`Check(CarriesValue("flying", 2, null) != CarriesValue("flying", 1, null))` | `BS` | `Badges.CarriesValue` 的 ② 分支 | 删掉 ② 分支、或把 `CardText` 改回单实参调用 ⇒ **两条里至少红一条**（前者红本条、后者红第 37 条），不会同时变绿；⚠️ 反面教材：**别只写** `CarriesValue("flying",2,null)==true`（它只管 `Badges` 那侧） | `W5b:118-123` | 无 |
| 41 | `A` 套前提夹具：本处量的那个根 `lossyScale.x == 1`（容差 `1e-3`）—— 把「父链单位缩放」从**注释**升成**断言** | `MM`（各根）· `CS`（`#10`） | 10 处的根：`#1` 药丸／`#2` `bltBg`／`#3` `bltBgR`／`#4` `bq`／`#5` `a833Bg`／`#6` `a833BgR`／`#7` `pFill2`／`#8` `mBg`／`#9` `mBgR`／`#10` `bg` | 这一条**本身就是**「读数不变」的前提、不是结论：它红了说明乙式与甲式本来就**不等价**，此时**该按乙式改期望值**（乙式才是建件那条路的逆），⛔ 不是把实现改回甲式 | `W5c:350-359` | 夹具：生产态（自检自己刚建好的那一态） |
| 42 | `B` 套读侧**两态夹具**（`#1`–`#10` 共用）：★1 前提 `CheckNear(q2.x, M*q1.x, 0.02f)`（夹具真带电）· ★2 主判据 `CheckNear(p2.x1, p1.x1, 0.02f)` 与 `CheckNear(p2.y1, p1.y1, 0.02f)`（**读数不随窗根缩放变**） | `MM`（`#1`–`#9`）· `CS`（`#10`） | `MenuDraw.UnionQuadRectPx` `:740-792` · `MenuDraw.QuadRectPx` `:690` | ★1 与 ★2 **结构上不可能同时满足**于甲式实现（甲式读裸 `position` ⇒ `p2 = M·p1`，M=1.07 ⇒ 差 7%）；「断言与实现一起改回甲式」会与 ★1 打架；⚠️ 与既有 `CheckScaleTwo` **方向相反**（那条测**写侧** `p2 == M×p1`，本族测**读侧** `p2 == p1`）⛔ 别合并 | `W5c:361-385` | 夹具：`root` = 本处量的九宫格根，其**父级**挂一颗 `TransformScalerBySmallScreenUI`（M=1.07）+ 底下一颗 `ImageQuad`；态二需 `Set(true)` + `SetScale(M)` + `Tick()` + **重建 root 那棵树**；收尾按 `CheckScaleTwo` 还原 |
| 43 | `C` 套块数守恒（只 `#2`／`#5`／`#8` 三处）：`CheckTrue(live + hidden == root.GetComponentsInChildren<ImageQuad>(true).Length, …)` | `MM` | `#2` `:4539-4563` · `#5` `:6524-6550` · `#8` `:10366-10390`（`MenuDraw.UnionQuadRectPx` 的 `quads` 出参） | 它咬的是收口时**唯一未判**的前提「激活的 quad 一定量得到」：哪天 `QuadRectPx` 对激活块也返 `false`，本条立刻红，而 `hide = 总数 − live` 会把那块**静默**算进关掉的里（不红） | `W5c:387-395` | 无（沿用各处的生产态） |
| 44 | `#1` 新增：`CheckNear(FindChild(rc0.GetChild(i), "Resource Bar Background").lossyScale.x, 1f, 1e-3f, …)` | `MM` | `MainMenuScene.cs:994-1010`（`quadUnion` lambda） | —— 报告未给（它是 `A` 套那句话的具体化） | `W5c:108-109` | 无 |
| 45 | `#1` **已有、不用补**：`pu.z − pu.x == 141.51 ±1.5`（宽）与 `pu.y == 16.467 ±1.5`（**上沿＝本处唯一的位置敏感判据**，宽是位置无关的、量不出读口换没换） | `MM` | `MainMenuScene.cs:1043`／`:1175` 两条调用点的期望值 | 灭自证靠第 42 条的 `B` 套（药丸是 `Nine` 建的、重建成本极低，最容易挂） | `W5c:104-107` | 无 |
| 46 | `#2` 新增：`CheckTrue(bltLive + bltHide == bltBg.GetComponentsInChildren<ImageQuad>(true).Length, …)`（钉「没有哪一块既不算活、也不算关」） | `MM` | `MainMenuScene.cs:4539-4563`（`bltBgTop`／`bltBgBot` + 块数） | 灭自证 = `B` 套（`root = bltBg`） | `W5c:130-131` | 已有且够：`bltLive>0 && bltHide>0` · `bltBgBot == BltVpBot ±0.5` · `bltBgTop < BltVpBot − 20` |
| 47 | `#3` **已有一条**（`bltRBot > 0f && bltRBot < BltVpBot − 20f`）—— 它的价值在**与 `#2` 成对**（「按框裁」vs「一刀切」）；补第 42 条（`root = bltBgR`） | `MM` | `MainMenuScene.cs:4579-4591`（`bltRBot`，只量下沿） | 灭自证 = `B` 套（`root = bltBgR`） | `W5c:140-141` | 无 |
| 48 | `#4` **已有一条**（`nOutB == 0`，报文里印 `bL..bR`）—— 它是**逐块**判据（并集表达不了「每块」）**别删**；补第 42 条（`root = bq`，本处是**唯一四处全量**，两态夹具能同时照出 x／y 两侧） | `MM` | `MainMenuScene.cs:6156-6187`（`PlayerRankingRow` 行底；只收了并集那一半） | ⚠️ `CheckTrue(bTop ≤ bBot && bL ≤ bR)` 意义不大，**真正的补法＝`B` 套** | `W5c:156-158` | 无 |
| 49 | `#5` 新增：`nOn + nOff == 总数`（与 `#2` 同形；⚠️ `#2` 与 `#5` 是**两份副本**，补断言时**两处都要补**） | `MM` | `MainMenuScene.cs:6524-6550`（`LeaderboardRow` 行底 `bgTop`／`bgBot`） | 灭自证 = `B` 套（`root = a833Bg`） | `W5c:164-166` | 已有：`nOn>0 && nOff>0` · `bgBot == VpBot ±0.5` · `bgTop < VpBot − 20` |
| 50 | `#6`：同 `#3`（`root = a833BgR`） | `MM` | `MainMenuScene.cs:6565-6576`（`rBgBot`） | 灭自证 = `B` 套（`root = a833BgR`） | `W5c:170` | 无 |
| 51 | `#7` 新增：`CheckTrue(endNode != null && endNode.GetComponentInChildren<ImageQuad>(true) != null, …)` —— 把「`end` 真的在、且真的会被算进来」钉住 | `MM` | `MainMenuScene.cs:9221-9246`（`TrophyInfoPopup` 的 `Fill` 宽，排除 `end` 子树） | 与「已有」那条 `fx2 − fx1 == 250.5 ±1.0`（**含端帽会是 256.2** ⇒ 差 5.7 远大于容差）合起来，**结构上不可能同时满足**于「忘了排除」的实现；否则「排除 `end`」在 `end` 消失／改名时会退化成空转（照样绿） | `W5c:192-197` | 无 |
| 52 | `#8` 新增：`mLive + mHide == 总数`；灭自证 = `B` 套（`root = mBg`） | `MM` | `MainMenuScene.cs:10366-10390`（`MatchLogRow` 行底 `mTop2`／`mBot2`） | 灭自证 = `B` 套（`root = mBg`） | `W5c:202-203` | 无 |
| 53 | `#9`：同 `#3`（`root = mBgR`） | `MM` | `MainMenuScene.cs:10400-10410`（`mRf` 行底 `rBgBot`） | 灭自证 = `B` 套（`root = mBgR`） | `W5c:205-207` | 无 |
| 54 | `#10` 新增：`CheckNear(bg.lossyScale.x, 1f, 1e-3f, "（前提·父链缩放）…")` | `CS` | `CollectionScene.cs:3346-3361`（`PracticeModePopup` 的 `Army Selector/Background` 四沿） | **灭自证现成**：`CheckScaleTwo`（`CollectionScene.cs:719-757`，4 个调用点）就是两态夹具；⚠️ 但它的期望方向是**写侧** `p2 == M × p1`，本族要的是**读侧** `p2 == p1` —— **别把两者合并成一条** | `W5c:224-230` | 已有且正是判据：四沿 `69.42 / 182.18 / 246.54 / 880.17 ±1.5`（上下沿那两条是刻意的：分得出「照父件画」vs「照 `Viewport` 画」） |

## 三、报告里提到、但不属断言批的东西（一句话 + 出处；这些要回流到待办正本）

### `W1`（引擎族六笔）
- ⚠️ `A1159` 记的「`Bastion` 结算侧没做」**已被 `W4`/`A1335` 补上**（`RuleCore.cs:4751-4777`），别再当未做项排期。出处 `W1:46-48` + `W4:14,27`。
- ⚠️ `A1159` 记的「`dropPod` 那一支没做」（`…WithDamageValues.c:82-96` 预览侧）**仍然开着**：`W4` 只做了**结算侧**血池，预览侧漏空投舱这一层 `W4` 也如实标着没落。出处 `W1:49` + `W4:233-237`。
- ⚠️ `Hurt` 里 `Penitence`（`:4730`）／`Cruelty`（`:4765`）仍用 `u.IsAlive`，原版那两句**没有对应方法体、判据没读** ⇒ 如实等裁。出处 `W1:50`。
- ⚠️ `A1218②` 记的三处缺口：`sacrifice` 那一支（已由 `W4`/`A1336①` 做）、`survivor` 触发 id（已由 `W4`/`A1336②` 做）、**表现层三跳没接**（`HighlightTraitIcon`／`DisplayTriggerAnim`／音效）。出处 `W1:82-85` + `W4:90-146`。
- ⚠️ 简报建议的「让 `CanAttackNow` 按原版口径读幸存者」**不成立**（全库 `,0x1ae` 命中里没有 `CanAttackNow`）⇒ 别照它改。出处 `W1:85,266`。
- 🔴「`CurrentDropPodHealth` 全库没有任何地方读它」**是错的**（原版读裸偏移 `+0xcc..0xdc`，不走 getter）—— 错因是只 grep 了 getter 方法名。出处 `W1:260-264`。
- 🔴 `survivor`／`survivorSpent` **同时是触发 id**（`OnTrigger(0x1ae)` / `0x1b8`）—— 其中 `survivorSpent` 那半已被 `W4` 订正为「**原版根本没有这条触发**，`0x1b8` 是 `Landing`」。出处 `W1:83,268` + `W4:111-117`。
- ⚠️ `CardScript__OnTurnEnd.c` 的 `.c` 块顺序**不能当地址顺序**（毒支在机器码里在标签之前，真实形状是两条独立 `if`）⇒ 判 `goto` 是不是真跳转**必须回 RVA**。出处 `W1:270-271`。
- ⚠️ `EntityScript__get_*` 系列的返回类型在反编译里会成片丢（渲成 `void`）⇒ 判 read 值只看桩签名 + VA 反汇编。出处 `W1:273`。
- ⚠️ `CardScript._ReceiveDamage_d__381__MoveNext.c` 里 `+0x35` 那个字节是什么字段**没查清**（将来做堡垒／空投舱结算会再撞到）。出处 `W1:275`。
- ⚠️ 「伤害挡下／豁免」那一族有**三处同族判据**（`IsProtectedFromDamageOrSurvivor` 五词表 · `_ReceiveDamage` 开头那串 `if` · `GetAdjustedDamage` 四词表）⛔ 别当三份独立实现。出处 `W1:277` + `W4:243-245`。
- ⚠️ `RET` 那条 `unimplemented.Count == 0`（`:1874`）扫的是**卡池里出现的**关键词 ⇒ 它**并不能**替我们守住「别谎报已实现」（登记了但卡池里没有的词照旧绿）。出处 `W1:279`。
- ⚠️ 行尾：`EffectResolver.cs` 是 **CRLF**（`CardDef`／`RuleCore`／`UnitState` 是 LF）。出处 `W1:281`。

### `W2`（`A1311` 代词目标补侧）
- 🔴 简报那句「把前一条 op 的 `Target.Side` 抄给代词 op」**照字面做会破结算**（`EffectResolver.cs:5976` 要靠 `Side=="prev" && Kind=="prev"` 认手牌那族）⇒ 已裁：**另开 `PrevSide` 栏**，台账 `A1311` 行已就地订正。出处 `W2:286-291`。
- ⚠️ `SAU9 Flayed One` 的 `gain` op 是 `Target == null`，而同形的 `ASH78`／`GOF15` 拿到的是 `Side=own Kind=unit` ⇒ **两条路（`When …` 正文 vs 裸正文）口径不一致**，**是否影响结算没查**，建议另立一条账。出处 `W2:292-297`。
- ⚠️ `UM_Angels_of_Death` 是全池**唯一**「战术卡 + `Target == null` 的 `give`」⇒ 3 参重载的唯一真卡用户；**没查**它是不是被判成半懂（卡面 `*`）。出处 `W2:298-301`。
- ⚠️ 全池还有 **23 条代词目标判不出侧**（`PrevSide=null`），今天只影响 `fast` 那一档打分、**空跑**；将来别的打分档要按侧取就会撞上它们。出处 `W2:134-147,271-274`。
- ⚠️ `AM77 Armoured Lightning` 的 `op[1]/op[2]` `PrevSide` 仍 `null`（`deploy` 族根本没有目标规格）—— 解析层另一缺口、**不在本账范围**。出处 `W2:275-278`。
- ℹ️ `工具/ruleprobe/` 那一族可以**离线**把 `RuleEngine/Data/SimpleAI.cs` 一起编（临时树摆法与 `csproj` 原文见报告）⇒ 打分档多一条 2 秒内环；⚠️ 别整目录 `*.cs` 加。出处 `W2:232-262,302-305`。

### `W4`（`A1335`／`A1336`）
- 🔴 `OtherCardSacrifice`（`AbilityTrigger = 225`）那条**广播整条没做**（对场上所有卡 + 两手牌各发一次 `OnTrigger(0xe1)`），要新开一个 `WhenEventKind`；**全池 0 张卡能碰到** ⇒ 建议新开一笔。出处 `W4:105,221-226`。
- 🔴 `dropPod` 的**「开舱」那半没做**（`Landing` 触发 + 出手后／回合开始两条时机）—— `A1335` 只做了血池 ⇒ 建议新开一笔。出处 `W4:227-232`。
- 🔴 `EnoughPendingDamageToDieWithDamageValues` 里**空投舱也是判死的一层**，我们**预览侧漏了它**，而 `.c` 读不通（`iVar5` 两支同值）**没敢落** —— 要回 RVA 核一遍。出处 `W4:233-237`。
- ⚠️ `EmitBloodThirst`（`RuleCore.cs:6636`）与 `TrySwarmMerge`（`:2613`）也各踩「没正文就不发事件」半边（两个方向各缺一半，注释已写明）。出处 `W4:238-240`。
- ⚠️「**被堡垒整份吃下**算不算『受到伤害』」（`Penitence`／`When … receives damage`）—— 判据**没读**、如实记着（0 张卡可达）。出处 `W4:241-242`。
- ⚠️ `IsBastionHigherThanAttack` 与 `GetAdjustedDamage:52-59` 是**同一条判据的两处**（`param_3` 那个开关谁传 `true` = 没查）。出处 `W4:243-245`。
- ⚠️ **易伤那一份**（原版 `:188-257` 是另一次 `DealMultiDamage`）没做：我们把它**折进** `DamageAfterReduction` 一个数 ⇒ 「堡垒打穿且血还 > 0」那一档**会少算**，判据已齐、如实标着。出处 `W4:78-83`。
- ⚠️ `deathType == combatAttacker(10)` **只映射了反击那一处**（全集查过、今天没有漏的）；将来新增伤害点要照着 `TryCreditKill` 那个口径再判一次。出处 `W4:84`。
- ⚠️ 表现层音效那一跳**按判据停手**：`sacrifice` 那格原版的 cue 本身就是空引用（铁律 11 第一档**不做**）；`survivor` 那格要动 `Resources/Art/**` + 跑导入工具（白名单外）。出处 `W4:134-140`。
- ⚠️ `RuleCore.cs:4028` 那段 Stomp 等价性论证已重写（结论不变、理由换了）—— **谁改了 `actual` 的语义就要重核这一格**。出处 `W4:86`。

### `W5a`（`A1331` 修 `Oath` 前缀）—— 🔴 **本份没有「该补什么断言」一节**
- 🔴 **还欠**：图标那一半 —— `card_icon_plan.json` 里这 4 张卡的英文 `desc` 没有条目，改完之后英文卡面会**少了 `oath` 徽记**（卡面原版是 `⟨徽记⟩ Oath 4:`）⇒ 要跑一次 `工具/gen_icon_plan.py`，大概率还要在 `TOKEN_BY_CARD` 补 4 条。出处 `W5a:115-119`。
- 🔴🔴 **同形状、另一枚记号**：修女会 4 张（`SOR72`／`SOR11`／`SOR23`／`SOR24`）英文 `desc` 也缺段首记号，缺的是 `☀`（**信仰**、不是 `Oath`）⇒ 静态判：这 4 张付费激活会**扣能量而不是扣信仰**（未跑验证）；要不要立账请调度台裁。出处 `W5a:125-131`。
- ⚠️ `KeywordSegment` 判据 ① 会多压掉一个该印的关键词：`UM74` 卡面 `Sentry 2.` 印不出来（正文里 `Gain Sentry 2` 含这个词 ⇒ 判「提过了」跳过）；`R5` 的表与此**相反**，要真渲一张才定得了。出处 `W5a:132-133`。
- ⚠️ `UM81` 的誓约能力**跨两句**，第二句不在 `_oathOps` 里 ⇒ 激活后只造成 2-4 点伤害、**不会给 `Shield`**（既有缺口、今天才可观测，未跑验证；全池只此 1 张）。出处 `W5a:134-138`。
- ⚠️ `EffectResolver.cs:4728` 那句注释（「`OathCost == 0` 的卡理论上没有」）补完前缀后**重新成立**，但 `.cs` 不在白名单 ⇒ 没改。出处 `W5a:94,139`。
- ℹ️ `gen_cards_engine.py` 在本机控制台不设 `PYTHONIOENCODING=utf-8` 会**崩在 `--check` 结论之前**（GBK `print`）⇒ 建议写进 `资料/命令速查.md`。出处 `W5a:46,134`。
- 🔴 **运行时那一半没验**：「点一下真能激活、真按 4/3/1/3 扣费」**没有实跑证据**（判据 = 静态链 + 逐字正则 + 卡图）；建议收口那趟带上 `RuleEngineTest.Run`（这 4 张卡的行为路径变了）。出处 `W5a:95`。

### `W5b`（`A1332` 收口）
- 🔴 **① 那一档仍没接**（`CardText` 传 `numericKeys = null`）—— 这是本次收口**明确的残留缺口**，要闭合得动两处（给 `KeywordSegment` 加可选参数 + 在生产调用点 `BattleDriver.FaceTextFull` 传 `c.NumericKeywords`）⇒ 建议**单开一笔**（跨 `CardPresentation/{Core,Battle}`）。出处 `W5b:129-135`。
- ⚠️ `BattleScene.Run` **没跑**（红线下）：那 4 条盯 `KeywordSegment` 的断言（`BS:822-855`）与 `CardBaseDemo.cs:989-995` 两条**逐条读过、预期 0 翻**，但**这是读出来的、不是跑出来的**。出处 `W5b:88-92,136-137`。
- ⚠️ **没查/没实跑**：`CarriesValue` 的「值 ≥ 2」那一档在运行时（喂单位**当前**关键词表那段，`BattleDriver.cs:11587-11591`）会不会因光环抬到 ≥2 而产生新不一致 —— 判断**不会**（`KeywordSegment` 入参是 `CardDef`），但没实跑。出处 `W5b:138-143`。
- ℹ️ 2 张卡共 3 个关键词词被 `KeywordTable.Normalize` 判不出 ⇒ 被 `Parse` 丢弃（`BL1` 的 `'Chosen of the Four'`、`SOR12` 的 `'Talent'` + `'Hymn of Battle'`）—— 后两个**没在任何账上见过**，请分流。出处 `W5b:151-157`。
- ⚠️ `FirstNumber` 找的是**整串**里第一个数字（含 `:` 之后的正文）⇒ 全池今天 5 个项取到兜底 1（无害），但**形是危险的**：将来 `Rally: Deal 2 damage` 会印出 `Rally 2`。出处 `W5b:158-163`。
- ℹ️ `CardText.KeywordSegment` 的**生产调用点全仓只有 1 处**（`BattleDriver.FaceTextFull`），其余 5 处都在自检里。出处 `W5b:164-166`。

### `W5c`（`A1330` 收口）
- ⚠️ **同步点该跑的自检**（动了共用件 `MenuDraw.UnionQuadRectPx`，全仓 6 个宿主）：`DeckScene.Run`／`MainMenuScene.Run`／`RewardsScene.Run`／`SettingsScene.Run`／`ShopScene.Run`／`CollectionScene.Run`；优先看 `MainMenuScene.Run`、`CollectionScene.Run`、**`ShopScene.Run`**（唯一带 `quads` 计数的调用点）。亚像素抖动正常；**几十 px 偏移**说明「父链单位缩放」前提不成立、**不是转换错了**（那时是原来那 10 处的读数本来就错），别回滚。出处 `W5c:314-330`。
- ⚠️ 同一族**另一条腿没收口**：`RenderedRect`（`MM:244`，**带 `Label` 支**）· `QuadPxRect`（`MM:12215`，直系同形副本）· `PxOf/PxYOf`（`CS:272-273`）· `QuadsOutside`（`MM:12134` 区）仍是**甲式** ⇒ 建议另立一笔「`Editor/*` 里甲式量法还剩几处」。出处 `W5c:420-432`。
- ⚠️ `WfSlider._fillRoot` 的 `localScale` 那一档 —— `MenuDraw.cs:644` doc 自己写着「**留待调度台裁**」，本件没碰；新增风险：它是九宫格根，谁对它取并集 ⇒ **并集 ≠ 渲染框**。出处 `W5c:434-444`。
- 🔴 **台账 `项目任务.md` §29·b 的 `A1330` 行有两处点名错还没落盘**：`SpanOfTmp` 在 `ShellScene`（不在 `RewardsScene`）、`CollectionScene` 不在那份「有意不收」清单里 ⇒ 建议主对话按铁律 5 就地改，并把 `A1330` 行更新成「`#1`–`#10` 已收口（2026-10-10）；余 7 处归 `A810②`／`A1044①`／形状不同」。出处 `W5c:446-453`。
- ℹ️ `#7` 那处**不是** `WfSlider`（`Fill Area` 这个名字**两个族都在用**，`TrophyInfoPopup` 的 `Fill` 与 `WfSlider` 的 `slider_fill` 名字能分开）。出处 `W5c:455-461`。
- ℹ️ 两个宿主里**剩下的** 11 处 `GetComponentsInChildren<ImageQuad>` 逐个判过，**一条都不是「手写 min/max 并集」** ⇒ 无漏网。出处 `W5c:463-468`。
- ⚠️「新写法在非 16:9 下更好」**没有实测**：本仓 `A990①`（尺寸项 `WorldW × K` 在非 16:9 下不跟位置缩放）还没落 ⇒ 那时**位置对了、尺寸仍不对**，并集框**仍不是**渲染框。出处 `W5c:409-411`。
- ⚠️ `#1` 的 lambda 两调用点（`MM:1040`／`:1169`）期望值**没动**（`±1.5` 容差、位置敏感）；若同步点报这两条红，先看是不是别人动了 `MainMenuRuntime`／`Nine`。出处 `W5c:414`。
- ℹ️ `#7` 的空集判据**已改**（从 `fx1 < float.MaxValue` 改成 `CheckTrue(fGot, …)`，因为空集时四沿归零会让旧判据**恒真**、变假绿）—— ⛔ 别改回去。出处 `W5c:186-188`。
