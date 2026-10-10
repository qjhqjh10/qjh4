# R1 · 引擎语义族 6 条现核（`RuleCore.cs`）· 只读代理交件 → 主对话落盘

> 2026-10-11 第十三会话。**只读**：没跑 Unity、没动 git、一个文件都没改。
> 行号一律**现读**（`RuleCore.cs` 现 6756 行；`§29·b` 原文里的行号已漂）。

---

## `A1159` — 判：**(β) 真缺陷待做**（且**账上的字段清单写错了**）

**一句话**：缺口原样还在 —— 三个「第二条命」字段我们一个都没建；而且照账上那句抄会**修错地方**。

**我方现读**：
- `Hurt`：`Core/RuleCore.cs:4446`（签名）· **`:4452`** `if (u == null || !u.IsAlive) return 0;` ·
  **`:4483`** 狂喜闸 `if (u.IsAlive && u.Has(KeywordTable.Ecstasy))` —— 即「`ApplyDamage` 之后看 `u.IsAlive`」，
  与原版 `!EnoughPendingDamageToDie` 只是**近似**。
- `WouldKillByEntries`：**`:4152`** `return u != null && u.IsAlive && u.Health - DamageAfterReductionList(u, entries) <= 0;`
  —— 🔴 **不是同一条代码路**（它不调 `Hurt`/`ApplyDamage`），是**第二处**各自的短路；`:4150` 的 doc 仍写着原版还比那三个字段。
- 三个字段**全仓零实现**：`grep -rn "DropPod|CurrentBastion|CurrentSurvivor|UseSurvivor|Bastion" --include=*.cs`
  ⇒ 只命中 **4 处注释/文档**（`RuleCore.cs:2858/3348/3866/4150`）；`Core/UnitState.cs` 全文 1147 行**一个都没有**。

🔴 **原版侧订正（本笔新查，两个错点）**：
- `CardScript__EnoughPendingDamageToDie.c:42`（**狂喜那道闸用的就是它**）**只读 `CurrentSurvivor`**；
  `CurrentBastion` / `DropPod` 在**该文件里零命中**。
- `CurrentBastion` 只在 `CardScript__EnoughPendingDamageToDieWithDamageValues.c:177/222/241` 里读；
  `CurrentDropPodHealth` ~~在反编译全量里**只有它自己的 getter/setter**（`EntityScript__get_currentDropPodHealth.c`，
  `grep -rln` 仅此 2 个文件）⇒ **没有任何地方读它**~~ ——
  🔴 **2026-10-11 主对话就地订正（铁律 5）：本句【错】。** `EntityScript__get_currentDropPodHealth.c:5-8` 读的正是
  `param_2 + 0xcc / +0xd4 / +0xdc`，而 **`CardScript__EnoughPendingDamageToDieWithDamageValues.c:82-96` 也读这三个偏移**
  （`local_138 = *(undefined4 *)(param_1 + 0xcc)` … `ObscuredInt__op_Implicit(&local_138, 0)`；
  `CardScript__CheckIfDead.c` 同，两文件各 **3 命中**）⇒ **同一格字段、确实会被读**。
  **错因 = grep 找的是【getter 方法名】、没搜【裸偏移】** ⇒ 「某字段没人读」这类**负面结论必须连裸偏移一起搜**
  （由 `W1` 反查纠正，主对话已独立复核）。
- ⇒ §29·b 那句「`!EnoughPendingDamageToDie` 里那三个字段」**本身是错的**。
- 原版两处**不是同一判据、但是包含关系**：`EnoughPendingDamageToDie.c:41-42` 先过 `health > 0 || CurrentSurvivor != 0`，
  **`:47`** 再 `return …WithDamageValues(...)` ⇒ 是**嵌套**、不是「共用同一判据」。
- 闸①「`HasDefaultTrait`（印刷）vs `u.Has(ecstasy)`（当前）」：**今天没处理、也没标**。
  `grep -rn "HasDefaultTrait" Core/*.cs` ⇒ 只有 4 处**注释**（`CardDef.cs:2079` · `RuleCore.cs:4469` · `:6219` · `:6254`），
  实现仍是 `:4483` 的 `u.Has(...)`；`CardDef.cs:2065-2088`（`D26` 订正那段）列了原版四条守卫，
  **但没一句说我们把 `HasDefaultTrait` 换成了 `u.Has`**。
- **可达性（现数卡池 1126）**：`survivor` **0 张** · `bastion` **0 张** · `Drop Pod` 1 张（`SW31 Fenrisian Drop Pod`，
  正文 `Ferocity: Deploy two Grey Hunter`，与 `CurrentDropPodHealth` 无关）⇒ **今天不可达**，但模型缺口照铁律 11 记着。

**最小改法（按原版各自的判据分两处，⛔ 别合并）**：
1. `:4452` + `:4483` 的 `u.IsAlive` 换成「`Health > 0 || CurrentSurvivor > 0`」
   （先给 `UnitState` 加 `Survivor` 字段 + `UseSurvivor` 消耗口，判据 `CheckIfDead.c:111-165`）；
2. `:4152` 的 `WouldKillByEntries` **单独**按 `WithDamageValues` 补 `Survivor` + `Bastion`
   （`:161/177/206/222/241/248`）；
3. 闸①：给 `CardDef` 加一个「印刷关键词」查询（`CardDef.Keywords` 就是印刷的，
   见 `UnitState.cs:513` 从 `card.Keywords` 初始化）⇒ 狂喜闸改读 `u.Card`，并**就地加注释**说明这是有意对齐 `HasDefaultTrait`。

---

## `A1167` — 判：**(β) 只剩「如实标」这一步**（`A1135` 已做完）

- 早退**在**：`RuleCore.cs:1403` `if (ctx.IsOver) return ctx.Winner;` —— 位置在
  `ResolveAtTurn(ctx,"turn_end")`（`:1299`）→ 再生段（`:1311-1330`）→ `DestroyRemnants(ctx, ctx.Active)`（`:1357`）
  → 眩晕/失明闸门摘除（`:1372-1402`）**之后**、`oath` 重挑（`:1405` 起）**之前**。
- **一个字都没标**：`grep -n "早退" RuleCore.cs` ⇒ 只 4 处（`:418/:776/:788` 都不是它；**`:1343`** 那句只把它当**位置锚**）；
  `grep -rn "无法比对|没有可比判据|不可比对" Core/*.cs` ⇒ **0 命中**。
- 原版确无：对 `decomp_full/BattleManager__ResolveEndTurn.c`（870 行）· `BattleManager__NextTurn.c` ·
  `BattleManager._NextTurn_d__395__MoveNext.c` 跑
  `grep -in "gameover|isover|matchover|finishgame|endmatch|victory|defeat"` ⇒ **全部 0 命中** ⇒ 账上那句成立。
- `A1135`：`资料/历史/A表已收口_第九会话.md:21` 记「2026-10-09 已做」；代码侧对应注释在 `RuleCore.cs:1332-1345`
  （逐段对 `CardScript__OnTurnEnd.c` 的 `:72` → `:84-108` → `:117-123` → `:124-134`）。✅ **已做完**。
- **改法**：在 `:1403` **上面**插一段注释（**只加注释、不改逻辑**）：①这道早退是**我们自己加的**
  （原版无对应物，`grep -in` 零命中）②因 `A1135` 把再生挪到它之前，「`turn_end` 触发打死督军」那一格
  我们**会跑一次再生**，而原版**既无早退也无可比判据** ⇒ 如实标「**这条边角无法比对**」，
  ⛔ 不许拿我们的结构当原版语义。

---

## `A1218` — 判：① **(α) 已做完** · ② **(β) 真缺陷待做（今天不可达）** · `+0x65` **已查实**

- **① 已做完**：判定口 `RuleCore.cs:2907 StunStillOnBoard(...)`（`:2918` 那句
  `occ.IsRemnant && ReferenceEquals(occ.Instance, target.Instance)`）；调用点 `:3955`；
  文档订正在 `:2755` / `:2844` / `:2894-2907`。断言：`Editor/RuleEngineTest.cs:4473`（③′ 会晕）·
  `:4503`（③″ 对照：真死不晕）· `:4524`（③‴ `Waystone` 同支路）。
  原版判据：`CardScript__CheckIfDead.c:122-147`（`HasToTransformIntoRemnant` 真 ⇒ `:143 AddTransformIntoRemnant(...,1,...)` + `:144 return`，**不置 5**）。
- **② 未实现**：`grep -rn "survivor" RuleEngine/Core/*.cs` ⇒ 只有 `RuleCore.cs:2859`（注释）+
  `Data/SimpleAI.cs:18/140/596`（AI 打分**读** `target.Has("survivor")`，不是**产**）⇒ **没有任何地方授予或消耗它**。
- ② 的悬案「有没有效果卡在运行时授予」**今天核掉了**：逐张扫 `cards_engine.json` 1126 张的**整个卡片 JSON**
  （含 `desc`/`descZh`/`keywords`）搜 `survivor|幸存|存活者` ⇒ **0 命中**。
  ⚠️ 残留不确定性：原版的 ability **资产在远端 CCD**、本地读不到 ⇒ 这是**卡面口径**的结论，不是逐资产核过的。
- 🆕 **`+0x65` = `isRemnant`**（可结掉 `R3` 的悬案）：`EntityScript__get_isRemnant.c:5`
  （`return *(undefined1*)(param_1 + 0x65);`）· `EntityScript__set_isRemnant.c:5` ·
  `CardScript__TransformFromRemnant.c`（`*(undefined2 *)(param_1 + 0x65) = 0;`）· `CardScript__CardSetup.c:94`（`= 0`）
  ⇒ 同一八位组被当 `undefined2` 清 0，与 `set` 的 `undefined1` 不矛盾。
  它在 `CheckIfDead.c:123` 的用法 = `HasToTransformIntoRemnant == 0 || card.isRemnant != 0` ⇒ **真死**。
- **改法（②）**：`UnitState` 加 `Survivor`（字段 + `RemoveAll` 清）+ `RuleCore.CleanupDeaths`
  （现读 `:4799-4830` 的残骸支路**之前**）加一条「`Survivor >= 1` ⇒ 消耗 + 留场、不置将死、不进坟场」的支路
  （判据 `CheckIfDead.c:111-112 / :152-165`），并顺手让 `CanAttackNow` / `IsAlive` 一族按原版口径读它。
  **今天可达性 = 0**，但铁律 11 ⇒ 要做。

---

## `A1258` — 判：**(β) 真缺陷待做**（① 通道**连空壳/出声位都没有**）

- **我方**：`grep -rn "ancel" RuleEngine/**/*.cs`（排 `CancellationToken`）⇒ 只有 4 类命中：
  `CardDef.cs:705`（注释，说的就是 ② 那条链）· `EffectResolver.cs:1589`（注释）· `RuleCore.cs:3524/3593`（注释）·
  `RuleCore.cs:67-68`/`:6630`（**匹配取消**，另一码事）· `RuleCore.cs:2824-2825`（`CardScript__CancelAttack.c:35` 的注释引用）。
  **没有任何 `CheckIfAbilityCancelsAttack` / `HasCancelAttackEffect` 的实现、桩、或出声。**
- **原版 ① 的形状（逐行读过）**：
  · 调用点 `BattleManager._ResolveAttack_d__438__MoveNext.c:1310`
    `CheckIfAbilityCancelsAttack(bm, +0x28 /*攻方*/, +0x30 /*原目标*/)`
  · `BattleManager__CheckIfAbilityCancelsAttack.c:7` ⇒ `CardScript__HasCancelAttackEffect(攻方, 攻方, 原目标)`
  · `CardScript__HasCancelAttackEffect.c:18-31`：扫 `card + 0x110`（`List<CardAbility>` 容器），
    对每一项 `RawCardScript.HasAbilityType(raw, 0x118)`；`:50-51` 再
    `CardAbility.CanTriggerAbility(ability, 0x118, bm, thisCard=攻方, param_2=攻方, param_3=原目标)`；命中 ⇒ `return 1`
  · **`0x118 = 280 = AbilityTrigger.AboutToAttack`**（`d:/2/Warpforge_code/Scripts/Assembly-CSharp/AbilityTrigger.cs:44`）
  · 命中后（同文件 `:1314-1368`）：`CardScript__CancelAttack(攻方)` → `ClearPendingDamage(攻方)`（`:1316`）
    + `ClearPendingDamage(原目标)`（`:1318`）→ `RegisterPlayerActionCancelled`（`:1365`）→
    `CardScript__ResolveCancelAttack(攻方, 攻方, 原目标)`（`:1368`）（⇒ `RawCardScript.OnTrigger(0x118,…)`，能力照常演）
- 🔴 **两条通道共用同一个 trigger id（280 / `AboutToAttack`）**，区别只在**扫谁**：
  ② 扫 `bm + 0x470`（场上每一张牌，`CheckUnitsCancellingAttack.c:33-66`）找替身；① 只扫**攻方自己**的能力表。
  我们引擎里 `AboutToAttack` / `0x118` 字面**零命中**（`grep -rn "AboutToAttack"` ⇒ 4 处全是注释）。
- **可达性**：逐张扫 1126 张卡的整份 JSON 搜 `cancel` ⇒ **0 命中** ⇒ 账上那句成立。
- **改法**：`RuleCore.DeclareAttack`（`:3532`，在 `:3546 IsValidTarget` 之后、`:3552` 消耗攻击之前，或照原版放在**主伤害之前**
  任一处）加一道 `TryCancelAttackByOwnAboutToAttack(ctx, attacker, target)`：① 若 `attacker.Card` 有一条
  `AboutToAttack`（新关键词，需同时进 `KeywordTable` + `CardDef.BodyKeywords`/`RoutableTriggers` +
  `EffectText` 的触发前缀表）的能力且条件成立 ⇒ **直接 return `RuleCodes.OK` 但不出伤害**。② **今天没有这张卡**，
  所以最小合规做法 = 按 ① 原样落地 + 在 `RuleCode`/日志里出声；
  ⛔ **不建议只留 `ErrUnimplemented` 空壳**（原版那条路是**真取消**，不是未实现，空壳会让人以为「没有这条规则」）。
- **没查清**：`RegisterPlayerActionCancelled`（`:1365`）与 `_ResolveAttack` 里 state `0x1a` 那一步对
  「攻击配额 / 已行动标记」的撤销语义 —— 没逐跳读（`ClearPendingDamage` 之外是否回退 `attacksThisTurn` 未核）。

---

## `A1278` — 判：**(γ) 前提不成立**（风险面不存在 ⇒ **0 行代码，可销**）

- 第 ⑥ 道闸在 **`RuleCore.cs:3492`** `if (kOwner != ctx.Active) return false;`（`TryCreditKill` 签名 `:3483`）。
- `TutorialScript.cs:943` 现读 = `int code = RuleCore.DeclareAttackByCurrentType(ctx, aSide, aSlot, tSide, tSlot);`
  （`aSide` 来自 `:934` 的 `TutorialRules.FindBoardUnit(ctx, d.acting, …)`）。`FindBoardUnit`（同文件 `:1455-1475`）
  **按数据里的 `unitType` 定 side**（`PlayerWarlord`/`PlayerMinion*` ⇒ **0**、`AiWarlord`/`EnemyMinion` ⇒ **1**），
  **与 `ctx.Active` 无关** ⇒ **不保证相等**。
- **现数**（扫 `RuleEngine/Resources/tutorial_stages.json`，6 关 / 79 个 `Attack`+`AttackFreeMode` 数据条目）：
  **78 条 `turnSide == actingSide`，1 条不等** = 第 1 关 `turns[13]`（`turnName = "AI 7"`，`playerStarts=true`
  ⇒ 该关第 14 回合 = AI 的回合，`ctx.Active = 1`）里的 `Attack`，acting = `PlayerMinion`
  「Primaris Intercessor (tutorial)」（**`playerAction = false`，脚本自己执行**）。
- 🔴 **但那条打不出去**：`DeclareAttack`（`:3539`）第一句就是
  `int pre = CanAttackNow(ctx, p, atkSlot, ranged); if (pre != OK) return pre;`，
  而 `CanAttackNow`（`:3057`）`:3059` = `if (ctx.IsOver || p != ctx.Active) return RuleCodes.ErrNotTurn;`
  ⇒ `aSide=0`、`ctx.Active=1` 那一记**返回 `ErrNotTurn`**，走不到 `TryCreditKill`（`:4020`）
  ⇒ **⑥ 改不动任何东西**。
- 旁证：`tutorial_decks.json` 全部字串里 `slay`/`kills` **零命中** ⇒ 教程牌里没有 `Slay`/`Kills` 消费者。
- **改法：0 行代码。** 建议在 §29·b 那一行落现核结论 ⇒ **可销**。

> ✅ **主对话已亲自复核这条的关键一步**（`RuleCore.cs:3539` `CanAttackNow` 是 `DeclareAttack` 的第一句；
> `:3059` 确为 `p != ctx.Active ⇒ ErrNotTurn`；`:3492` 确为第⑥道闸）⇒ **采信，A1278 销账**。

---

## `A1279` — 判：**(β) 真缺陷待做**（且账上的口径**要订正**）

- **21 张复现**：现数 `keywords` 含 `slay` **9** + `desc` 含 `Slay:` **19** ⇒ **并集 21**（与 `G1 §5` / `RN §①·④` 逐数吻合）。
- **口径①（伤害/摧毁）复现 = 0**：去掉 `Slay:` 子句后 21 张的余文里只有
  `Trait: …` / `Flying.` / `Armour 2.` / `Create a random …` / `Your Warlord gains "…"` ⇒ 无一条造成伤害/摧毁。
- 🔴 **口径② 另有 1 张**：**`EC28 Flawless Blade Champion`**（`type=unit`，攻 6），正文 =
  `Slay: Give +1 [Might] to all friendly troops.` **＋** `Ecstasy 3: Attack a random enemy.` —— 后半句**在 `Slay:` 之外**。
  它 100% 能打死单位：`EffectText.cs:4817-4827` 的 ④ 号正则 `^attacks?\s+(.+)$` ⇒ `Verb="forceattack"` ⇒
  `EffectResolver.cs:1595-1607 TryAttackOnce` ⇒ `RuleCore.DeclareAttack` ⇒ 尾段 `:4020 TryCreditKill(…)` **就算击杀**
  ⇒ **它自己的 `Slay` 再响**（自链同 `Company Champion`）。
  ⇒ 账上「能走的就是 `Company Champion` 那一张」**低估**。
- **口径③ 没数过**（`G1 §5` 自己写着「没逐张数」）。**今天粗算**（判据 = 能授 `Slay` 的来源 × 能接的载体 ×
  载体自带「造成伤害/摧毁」正文）：
  · 授予源 = 21 张里那 6 张：`BL4 Helspear Assault`(督军) · `EC52 Pledge to the Dark Prince`(**一个友方部队**) ·
    `GSC_Master_Outrider`(督军) · `GOF90 Beastboss on Squigosaur`(**友方野兽**) · `GOF_Ferocious_Rage…`(督军) ·
    `GOF_Uge_Choppa`(督军)；引擎侧落点 = `Core/Aura.cs:749-766 GrantEmbeddedAura`
    （注释自己写「全池 1 张 `Beastboss on Squigosaur`」）。
  · **督军那一支**：全池 **57** 张 `subtype == "Warlord"`，正文含「造成伤害/摧毁」的 **3 张**
    （`ASH74 Jain Zar` · `AM_Hektor_Thenmann` · `EC3 Lucius The Eternal`）—— **都不是 BL/GOF/GSC** ⇒ **无可达对**。
  · **部队那一支**：`EC52` 给**一个友方部队**；EC 阵营自带「造成伤害/摧毁」正文的**部队** 4 张
    （`EC19 Blastmaster Noise Marine` · `EC12 Tormentor` · `EC33 Terminator Champion` · `EC38 Heldrake`）⇒ **可达**。
  · **野兽那一支**：`GOF90` 给友方野兽；`GOF96 Gargantuan Squiggoth`（`subtype=Beast`，同阵营）正文 =
    `Stomp. Rally: … Mob: Deal 1 damage to all enemies` ⇒ **可达**。
  · ⇒ **至少 2 组可达对**（`EC52 × {EC19/EC12/EC33/EC38}`、`GOF90 × GOF96`）。
  ⚠️ **标记**：这是按 `subtype`/`faction` + 正则在**卡面文字**上的粗扫，**没逐张读正文**，
  也没逐跳核「那一下击杀会不会被 `TryCreditKill` 记上」。
- **改法**：不是改代码，是**先把这一行的口径改对**（① 加 `EC28` ② 把「0 张」拆成两个口径
  ③ 把授予那一支的可达对写上去），然后用**真卡夹具**补断言
  （`RuleEngineTest` 现有夹具照不到：`RN §②·④` 已记那 8 张「造成伤害并眩晕」在 `RuleEngineTest.cs` 里零引用）。
  ⚠️ 账上那句「收口跑 `RuleEngineTest.Run` 时要留意有没有变慢」**判不了**（红线禁跑 Unity）。

---

## 顺手发现（⛔ 只报不改）

1. 🔴 **§29·b 的 `A1159` 字段清单错了**：~~`CurrentDropPodHealth` 在 `EnoughPendingDamageToDie*` 两处**都没被读**；~~
    🔴 **2026-10-11 主对话就地订正（铁律 5）：这半【错】** —— `…WithDamageValues.c:82-96` **确实读** `+0xcc/+0xd4/+0xdc`
    （就是 `get_currentDropPodHealth` 那三个偏移）⇒ **只有「狂喜那道闸只读 `CurrentSurvivor`」与「`CurrentBastion` 只在 `WithDamageValues`」这两半成立**。
   `CurrentBastion` **只在 `WithDamageValues`**；狂喜用的 `EnoughPendingDamageToDie` **只读 `CurrentSurvivor`**。
   照「三个字段」抄会白写两个。
2. 🔴 **`A1224` 的代码注释与它打架**：`Core/EffectResolver.cs:2148-2152` 仍写「…**已判等价、⛔ 不另加判据**」，
   与 `RN_Slay与DoStun查证.md` §② 的结论**正面冲突**。（与 `R2` 的顺手发现 ① 同一处。）
3. **`A1258` 的两条通道共用同一个 trigger id（280 `AboutToAttack`）** —— 将来补 ① 时**别新造第二个 trigger**。
4. **行号漂移**：`A1159` 行引的 `Core/RuleCore.cs:3624` **现读是 `:4150`**；
   `A1278` 行引的 `TutorialScript.cs:943` **现读仍是 `:943`**（未漂）。
5. ~~`CardDef.cs:701-705` 的 `Bodyguard` 文档**读点过期**~~ —— 🔴 **2026-10-11 主对话就地订正（铁律 5）：本条【不成立】。**
   现读 `CardDef.cs:701` 写的**已经是**「**读点**：`RuleCore.TryRedirectAttackToBodyguard`（由 `RuleCore.DeclareAttack` 调…）」
   ⇒ **文档早就改过了**，是本条照了旧报告（`G1 §8·2`）的结论、**没有现读就写「现读仍成立」**。
   🔑 教训：**「某条旧报告报过」≠「现读仍成立」** —— 负面 / 过期类结论**必须现读那几行**才可落。
6. `RuleCore.cs:3866` 一句注释「（我们没做 `Bastion` 之类会在中间改血的东西）」—— 与 `A1159` **同源**，别当两句。

**没查清**：① `A1258` 里「取消后攻击配额/已行动标记要不要回退」（`RegisterPlayerActionCancelled` 与 state `0x1a`）**没逐跳读**；
② `A1279` 授予那一支只做了**卡面正则粗扫**；③ 全部 6 条**都没有实况验证**（红线：只读、不跑 Unity）。
