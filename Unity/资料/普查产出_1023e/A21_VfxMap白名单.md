# A21 · `VfxMap` 白名单（D1 + D6 + D7）· D3 施工方案 · D5 常驻说明

> 执行代理 **A21** · 2026-10-24 · 有写权限。
> **实际改动只有一个文件**：`Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs`（**+380 / −67 行**，`git diff --numstat`）。
> **没动**：`Battle/**` · `Editor/**` · `RuleEngine/**` · `Core/` 其余文件 · `工具/**` · 正本 · git。
> **没跑 Unity**；`TMPDIR=/tmp/wf_a21 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（最后一次）。
> 判据来源：`资料/普查产出_1023e/C3_动画特效审查_索引.md` §四 D1/D3/D5/D6/D7 + 三份分阵营报告；
> 复核用 `d:/2/tools/decomp_full/` + `cards_engine.json` + `Resources/card_vfx_by_card.json` 现读。
> 🆕 **第二版（协调方追加四条）**：除了 D1 的 `Mob`/`Teleport`，本批还做了
> **D6（`Codex` 大小写 bug）** 与 **D7（`Uprising` / `Artifice` 进白名单）**，并把四个**没有发射口**的名字
> 放进反向闸。**这一版同时推翻了第一版的一个隐患**：第一版加的 `case "Mob"/"Teleport"` 是 PascalCase，
> 对引擎发的 `"mob"/"teleport"` **同样恒 false**（与 D6 同一个 bug）—— 已一起修好。

---

## 一 · 白名单四条修正（**已做完**）

### 1·0 一句话

`IsRoutedTriggerName` 现在**先 `ToLowerInvariant()` 再比**，并放行 5 个**引擎真的会发**的名字：
`Mob` / `Teleport` / `Codex` / `Uprising` / `Artifice`；反向闸挡 8 个**没有发射口**的名字。

### 1·1 结论

| # | 名字 | 枚举 | 条目 / 卡（现读） | 引擎发射口 | 放行前 | 本批 |
|---|---|---|---|---|---|---|
| D1-a | `Mob` | 300 | **15 行 / 11 张**（Goff） | `RuleCore.cs:6008`（keyword `"mob"`） | 整桶播（`MobEffect`/`TideEffect`/`RallyEffect`/`TalentEffect`） | ✅ 进白名单 |
| D1-b | `Teleport` | 690 | **9 行 / 6 张**（DA） | `RuleCore.cs:3674`（keyword `"teleport"`） | 整桶播（6 张桶里**全是** `ArmourEffect`= 错件） | ✅ 进白名单 |
| **D6** | `Codex` | 145 | **33 行 / 23 张**（UM；**32 行有件**） | `RuleCore.cs:11233`（keyword **`"codex"`**） | 🔴 **白名单里那条 `case "Codex"` 对 `"codex"` 恒 false** ⇒ 等于没写 ⇒ 整桶播 | ✅ **修好大小写** |
| D7-a | `Uprising` | 680 | **13 行 / 8 张**（GSC） | `RuleCore.cs:3604`（keyword `"uprising"`） | 整桶播 | ✅ 进白名单 |
| D7-b | `Artifice` | 560 | **14 行 / 11 张**（`BL32` + `GSC24/31` + **Sautekh 8 张**） | `EffectResolver.cs:2187`（keyword `"artifice"`） | 整桶播 | ✅ 进白名单 |

⛔ **反向闸挡住（引擎没有发射口，加了必然整桶播错件）** —— `NoEmitTriggerNames` 现读 8 个：
`Unstable`(81) · `OnSecretCreated`(700) · `OnCollectQuestPoints`(695) · `TriggeredCodex`(650)
（第一批 D1-c/D1-d）＋ **`OnCollectSpiritStone`(610) · `UnitLoseStealth`(647) · `UnitStunned`(291) ·
`OnSabotageCreated`(701)**（第二批 D7-c，协调方点名）。

### 1·2 证据（全部现读，不是转述）

| 项 | 判据 | 读数 |
|---|---|---|
| `Mob` 发射口 | `RuleCore.cs:6008` `FireTriggerAt(ctx, attacker, KeywordTable.Mob, p, atkSlot)`（近战攻击结算尾部） | keyword = `KeywordTable.Mob` = **`"mob"`**（`CardDef.cs:5070`） |
| `Teleport` 发射口 | `RuleCore.cs:3674` `FireTriggerAt(ctx, unit, KeywordTable.Teleport, p, slot)`（本回合从牌库抽到即打出） | keyword = **`"teleport"`**（`CardDef.cs:5314`） |
| `Codex` 发射口 | `RuleCore.cs:11233`（`CheckCodex` 内）`FireTriggerAt(ctx, u, KeywordTable.Codex, p, s)` | keyword = **`"codex"`**（`CardDef.cs:5412`）；`emitSignal` **原样**写进 `BattleEvent.Keyword`（`RuleCore.cs:10706`），**没有大写化** |
| `Uprising` 发射口 | `RuleCore.cs:3604` `FireTriggerOnSide(ctx, p, KeywordTable.Uprising, unit)` → 内部 `FireTriggerAt`（`:11394`） | keyword = **`"uprising"`**（`CardDef.cs:5424`） |
| `Artifice` 发射口 | `EffectResolver.cs:2187` `FireTriggerOnSide(ctx, p, KeywordTable.Artifice)` | keyword = **`"artifice"`**（`CardDef.cs:5361`） |
| 候选侧名字 | `cards_engine.json` 的 `animTimes[].tn` | 全部是 **PascalCase**（`Mob`/`Teleport`/`Codex`/`Uprising`/`Artifice`） |
| 每行都有件 | 逐行比 `animTimes[].n` 与运行期表 `es[]` | `Mob` 15/15 · `Teleport` 9/9 · `Uprising` 13/13 · `Artifice` 14/14 · `Codex` **32/33**（差的 1 行 = `UM28` 的 `Wait_1s`，`n` 为空，= D5 纯计时拍） |
| 大小写不撞名 | 原版 `AbilityTrigger` 枚举（`dump.cs:20235-20335`）逐个核 | **没有任何两个名字只差大小写** ⇒ 小写化不产生歧义 |

### 1·3 🔴 D6 的修法（**整段归一，不是只补一个 case**）

**根因**：C# 的 `switch (string)` 是 **Ordinal（大小写敏感）**；引擎发 `KeywordTable.*` 的**小写常量**、
原版枚举/候选是 PascalCase ⇒ `case "Codex"` 对 `"codex"` **恒 false**。
**同一个 bug 也吃掉了第一版的 `case "Mob"/"Teleport"`**（协调方追加时发现的）—— 所以修法不能只补一个 `case "codex"`。

**实际改法**（`VfxMap.cs:1906-2030`）：
```csharp
switch (triggerName.ToLowerInvariant())
{
    case "codex":  …  case "mob": …  case "uprising": …     // 全部 case 标签一并小写
}
```
- 用 `ToLowerInvariant()`，⛔ 不用 `ToLower()`（后者按 culture：土耳其语下 `I` → `ı`）。
- 前面两条 `Ambush` / `TriggerEnchantment` 仍用 `OrdinalIgnoreCase`（本来就是对的，**没动**）。
- `CanonicalTriggerName`（`penitence → Rage`）仍在**外面**先跑，顺序不变。
- 候选匹配那段 `NameIs` 本来就是 `OrdinalIgnoreCase` ⇒ 不用动。

**`IsRoutedTriggerName("CODEX")` 的行为（协调方点名要写清楚）：** **true**。
理由：归一在**入口处**做（`ToLowerInvariant`），所以 `"CODEX"` / `"codex"` / `"Codex"` 三态**等价**。
这是**刻意**的：① 枚举里没有任何两个名字只差大小写（上表已核）⇒ 不会误配；
② 引擎/数据两侧的写法不统一（引擎小写常量、候选 PascalCase），只有入口归一能一次兜住。
⚠️ **不许**把归一改回「按大小写敏感匹配 + 逐个补 case」—— 那正是 D6 的形状。

### 1·4 改动清单（`VfxMap.cs` 现行号）

| 位置 | 改了什么 |
|---|---|
| `:141-164` | 新增 **D3** 的整段判据注释（见 §二） |
| `:825-831` | `EnsureLoaded()` 载表时跑一次 `SelfCheckRoutedTriggerNames()`，违规 → `Debug.LogError("[Vfx][ASSERT] …")` |
| `:1865-1879` | 新增 **D5 常驻说明**（见 §三） |
| `:1901-2030` | `IsRoutedTriggerName`：**整段大小写归一**（`ToLowerInvariant`）+ 新 case（`codex` / `mob` / `teleport` / `uprising` / `artifice`），注释写清**每个发射口**、**候选读数**、**不放行的后果**、**放行后的取舍**、**⛔ 八个名字不许加** |
| `:2226-2234` | `NoEmitTriggerNames`（**8 个**反向清单）的整段判据注释 + 数组 |
| `:2236-2243` | `MustRouteTriggerNames`（5 个正向锁） |
| `:2245-2252` | `MustRouteTriggerKeywords`（与上表**同长同序**的引擎小写 keyword） |
| `:2254-2303` | `public static string[] SelfCheckRoutedTriggerNames()`（名字级 + **大小写三态**；不需要卡池） |
| `:2306-2380` | `public static string[] SelfCheckRoutedTriggers()`（名字级 + **件级**：真实卡表上跑 `ResolveAllTimed`） |
| `:2374-2400` | `AmbiguousTriggerLabel`（件级自检的「分不出来」跳过判据，见 §四·8） |

**件级自检做什么**：对**每一张**有 `TriggerName == <白名单名>` 行的卡，按**生产路径逐参**
（`perCardOnly: true` + 引擎关键词常量 —— 与 `BattleDriver.PlaySignal` 的 Trigger 支同参）调
`ResolveAllTimed`，逐件比：**多一件**（不属于这个 trigger 的候选）= 整桶播了；**少一件** = 归口丢件。
自检覆盖面（现读）：`Mob` 11 张/15 行 · `Teleport` 6 张/9 行 · `Codex` **22 张/30 行**（另 2 行是
「一个 CardAnim 挂两个 trigger」分不出来，见 §四·8）· `Uprising` 8 张/13 行 · `Artifice` 11 张/14 行。
**本机用 Python 按同一逻辑预演过一遍：5 个名字全部 0 违规。**

### 1·5 断言（怎么才会红）

**A. 常驻（已经在跑，不需要自检宿主）** —— `EnsureLoaded()` 里那一句：
- 🧨 把 `NoEmitTriggerNames` 里**任一**名字加进 `IsRoutedTriggerName` ⇒ **LogError**。
- 🧨 把 5 个正向名字里任一个删掉 ⇒ **LogError**。
- 🧨 把 `switch (triggerName.ToLowerInvariant())` 改回 `switch (triggerName)` ⇒ `"codex"` 那一态挂 ⇒ **LogError**（**D6 复发闸**）。

**B. 自检宿主（⛔ 本批不能改 `Editor/**`，下一批把这段贴进 `BattleScene.Run` 的
`W57/T21` 那一段旁边即可；`RuleEngineTest` 也行）**：

```csharp
// ---- 🆕 2026-10-24（A21 · C3 D1/D6/D7）：trigger 白名单 ----
var a21Names = VfxMap.SelfCheckRoutedTriggerNames();
Check(a21Names.Length == 0,
      "★ A21/D1+D6+D7（名字级）：`Mob`/`Teleport`/`Codex`/`Uprising`/`Artifice` 在白名单里"
    + "（且 `codex` 小写 / `CODEX` 大写两态都命中）；八个没有发射口的名字不在"
    + (a21Names.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", a21Names))
    + " —— 🧨 把 `switch` 改回大小写敏感（D6 复发）、删掉任一正向 case、或把 `Unstable`/"
    + "`OnCollectSpiritStone` 之类加进去 ⇒ 本条红");

var a21Bad = VfxMap.SelfCheckRoutedTriggers();   // 件级：会载卡池（战斗宿主里本来就载过）
Check(a21Bad.Length == 0,
      "★ A21/D1+D6+D7（件级）：五个 trigger 的逐卡归口**不多不少**"
    + "（Mob 11/15 · Teleport 6/9 · Codex 22/30 · Uprising 8/13 · Artifice 11/14）"
    + (a21Bad.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", a21Bad))
    + " —— 🧨 把 `TryResolveTriggerPicks` 的③合并改回「只查 `trigger` 桶」⇒ 每张卡都报「漏播」");

// D6 的**直接靶子**（上面两条已覆盖，但这条失败时最一眼看出来）
Check(VfxMap.__A21CaseProbe(), "…");   // ⛔ 不新增探针 API —— 用上面两条即可
```

> ⚠️ 上面最后一行是**占位说明**：`IsRoutedTriggerName` 是 private，宿主通过
> `SelfCheckRoutedTriggerNames()` 间接验（它内部就调 `IsRoutedTriggerName("codex")` / `("CODEX")`）。
> ⛔ 别为了「直接调」把它改成 public —— 那会多开一个口径。
> `SelfCheckRoutedTriggers` **返回空数组才是通过**；卡池没载到时它**单列一条「没验」**（⛔ 不是静默通过）。

### 1·6 放行后的**取舍**（如实记进代码注释了，⛔ 别当回归去「修」）

`MobEffect` / `ArmourEffect` 这类 **trait 件在 `CardDef.AnimTimes` 里没有行** ⇒ `TriggerNameOf` 回 null
⇒ 被「未标名的一律丢」那条既有规则丢掉。所以放行后这一拍**只播卡自己那条技能动画**，原版是两件都播
（trait 的 `DisplayTriggerAnim` + `PlayAbility` 各一趟）。

- 收益：`Teleport` 那 6 张不再播 `ArmourEffect`（Start(0)，纯错件）；`Mob` 那 11 张不再把
  `TideEffect`/`RallyEffect`/`TalentEffect` 一起播（错件 + 多播）；`Codex` 那 23 张同理。
- 代价：`MobEffect`（`TraitEffects[Trigger]` 那件真件）暂时不播。
- **补齐属于 D4 / `P7`**：要先把 `keyword` / `traitTriggerType` 落进运行期表（现在只有 `n/c/a`），
  `TryResolveTriggerPicks` 才能把「属于这个 trigger 的 trait 件」与 Start(0) 那批分开。
  ⛔ **别为了让 `MobEffect` 回来就放宽「未标名一律丢」** —— 那会把 Tide/Rally/Talent 一起放回来。

### 1·7 没查清 / 如实记

1. **`Codex` 有 2 行结构上分不出来**（`UM37` 的 `BulletTrail_Stormraven_SingleTarget` ·
   `UM84` 的 `Buff_Purple_target`：同一个 `CardAnim` 同时挂在 `ThisCardPlayed` 与 `Codex` 下，
   而运行期分类键 `TriggerNameOf` 只取第一条）⇒ 这两行的 **Codex 那一拍今天播不出来**
   （`UM84` 仍有自己的 `CodexEffect` 顶住；**`UM37` 那一拍变成不播** —— 详见 §四·8 的量化）。
   自检 `AmbiguousTriggerLabel` **跳过并记账**，⛔ **没有**假装通过。
   要收口得让分类键带上 `Order`（引擎/数据侧另案）。
2. **`Mob` 的近战限定**：`RuleCore.cs:6005` 的 `if (!ranged && …)` 照原版 `param_4 == Melee` ⇒ 远程攻击不发 `mob`。**已核**。
3. **`Teleport` 的卡关键词**：6 张卡的 `cards_engine.json` 都有 `Teleport` 关键词（`unit.Has(...)` 那道闸过得去）。**已核，0 例外**。
4. **`Artifice` 的 Sautekh 8 张**：引擎 `FireTriggerOnSide(..., Artifice)` 只对**带 `HasFx("artifice")`** 的单位发
   ⇒ 那 8 张是否真的带这个词，**没逐张核**（件级自检只验归口、不验发射条件）。**如实记**。
5. 以上全部是**读码/数据结论，没跑 Unity**（D6 尤其）—— 坐实最快的办法：真机放一次 Codex（`UM13` 之类），
   看播的是 `CodexEffect` 还是它 `trigger` 桶里那件。

---

## 二 · D3：`UnitAttack(50)` 技能动画的归口 —— **完整补丁方案（本批未动手）**

### 2·1 先订正 C3 的一处前提（铁律 5）

C3 写「`BattleDriver` 只在 `EvtKind.Attack && !e.Ranged` 时查这个桶（`:17404`）⇒ **远程攻击时不出**，
`UM70` **100% 永不播**」。**这一半已经过期**：

- `ScheduleAttackPresentation` 的 `if (!e.Ranged) { …命中帧… }` 在 **`BattleDriver.cs:17889-17900`**，
  而 ③「攻方自己的 `UnitAttack(50)` 技能动画」在 **`:17906-17921`**，**在 `if` 之外**，
  `atkEnd` 按 `e.Ranged` 分叉（近战 `MeleeDamageLag` / 远程 `RangedDamageLag`）。
- ⇒ **攻方自己那条动画今天近战、远程都播**（2026-10-23 `W63` 收口时改的；C3 引的 `:17404` 是改前的行）。
- ⇒ D3 真正剩下的两件是：
  1. **「替别人攻击时播」不成立**：6 张监听型卡（见 2·3）在**别人**攻击时一条都不播；
  2. **反过来多播**：③ 对**监听型卡自己攻击时也无条件播**，而原版动画属于 ability 的一步，
     ability 没过 criteria（`CardAbility__CanTriggerAbility.c:126-140`：
     `IsRightContext` + `FilterMethods.CheckIfMeetsCriteria` ×2 + `BoardAnalysis.MeetsCriteria`）
     就**不该有动画**（例：`DA22 Techmarine` 自己（Infantry）攻击时，「我方载具攻击」不成立 ⇒ 原版不播，
     我们现在会播 `Trait_GainQuest` + `Healing_Projectile`）。

**原版判据（本批逐句重读，不是转述）**：
- `BattleManager._ResolveAttack_d__438__MoveNext.c:988` → `BroadcastUnitAttacked(manager, attacker, target, attackType)`，
  在共享尾标号 `LAB_1809aed47` 上（近战/远程两支都汇到这里）。
- `BattleManagerSupport__BroadcastUnitAttacked.c:22 / :29 / :48-58 / :60+`（本批现读）：
  **先** `ResolveUnitAttacked(target, attacker, target, …)`、**再** `ResolveUnitAttacked(attacker, attacker, target, …)`、
  **然后**遍历场上（`CardScript__IsInPlay` 为真）**每一张卡**、**最后**手牌。
- `CardScript__ResolveUnitAttacked.c:28-30` → `RawCardScript__OnTrigger(0x32 = 50, thisCard, actingCard, targetCard)`，**无条件**。
- **但**动画是 ability 的一步：`RawCardScript__TriggerAbilities` → `CardAbility__CanTriggerAbility`
  （trigger 50 走 `:126` 的 `IsRightContext` + `:131-140` 三条 criteria）→ `PlayAbility` → `PlayAnim`
  ⇒ **「OnTrigger 无条件」≠「动画无条件」**。这一点 C3 没写，是这次补上的关键判据。

### 2·2 我方现状（为什么「监听型卡」今天一条都不播）

引擎**已经把这条链修好了一半**（不是缺触发）：

| 环节 | 现状 | 出处 |
|---|---|---|
| 攻击广播 | **有**：先预调被打者、再预调攻方、再广播给其余棋盘（快照排除这两个），次序照原版 | `RuleCore.cs:6101-6110`；`EffectResolver.BroadcastWhen` + `SnapshotBoardListeners` |
| 监听者发 `EvtKind.Trigger` | **有**：`FireBoardWhenListener` → `EmitListenerTrigger`，keyword = `TriggerNameOfWhenKind(Attack)` = **`"UnitAttack"`**，闸 = `HasTriggerSource`（有动画行 或 有 ability） | `EffectResolver.cs:6239-6246` / `:7880` / `:6149` |
| 表现层取件 | **断在这里**：`TryResolveTriggerPicks` 只合并 **`trigger` + `ability`** 两个桶；而 `UnitAttack(50)` 的候选在 **`unit_attack_anim`** 桶（`W63` 于 `EnsureLoaded` 把它们从 `attack_melee` **搬**过去，`VfxMap.cs:909-928`）⇒ 合并结果 **空** ⇒ `perCardOnly:true` ⇒ 返回空数组 | `VfxMap.cs:2016-2070` |
| 攻方自己 | 走 ③（与 Trigger 事件无关）⇒ 近战/远程都播，但**不判 ability 是否真的结算** | `BattleDriver.cs:17906-17921` |

**另一条相关事实**：`EC1/EC2/TL11` 这一族（卡面「被本单位攻击的…」）的正文不在 `WhenTriggers` 里，
而是 `CardDef.AttackedOps`，引擎在攻击段发的是
`ctx.Emit(EvtKind.Trigger, …, keyword: "attacked", …)`（`RuleCore.cs:5959`）——
**`attacked` 既不在白名单里、也不是任何 `CardAnimTime.TriggerName`**。

### 2·3 全池 11 张卡（现读 `cards_engine.json`，判据 = `animTimes[].tn == "UnitAttack"`）

| # | id | 卡名 | 阵营 | 攻/远 | 那几行（prefab） | 卡面语义 | 归类 | 今天 |
|---|---|---|---|---|---|---|---|---|
| 1 | `DA22` | Techmarine | DarkAngels | 4/4 | `Trait_GainQuest`(t=0) · `Healing_Projectile`(t=0.7) | 当我方载具攻击时… | **监听型** | 自己攻击时才播（且不判条件）；别人提车攻击时**不播** |
| 2 | `EC1` | Sonic Blaster Noise Marine | EmperorsChildren | 1/4 | `Debuff EC` | 被本单位攻击的敌方部队眩晕 -1/-1 | 攻方自身（`AttackedOps`） | 自己攻击时播（③） |
| 3 | `EC2` | Blastmaster Noise Marine | EmperorsChildren | 2/6 | `EC_Sonic Stomp` | 摧毁被此单位攻击的有护甲军队 | 攻方自身（`AttackedOps`） | 同上 |
| 4 | `GSC12` | Neophyte Iconbearer | Genestealers | 2/2 | `CreateCard GSC` | 当一个友方单位攻击时… | **监听型** | 别人攻击时**不播** |
| 5 | `SAU31` | Doomstalker | Sautekh | 4/8 | `NecronGauss_Large` · (`Wait_1s` 纯计时，无件) | 当敌人攻击时… | **监听型** | 别人攻击时**不播** |
| 6 | `SW16` | Long Fang | SpaceWolves | 2/5 | `BulletImpact_Bolter_Wide Area` | **当此单位攻击**带 huntMark 的敌人时… | 攻方自身（self 监听） | 自己攻击时播；**别人攻击时不该播**（现状靠 ③ 也不播，OK） |
| 7 | `SW26` | Arjac Rockfist | SpaceWolves | 6/4 | `Hammer_Slam_SW` | 摧毁带 huntMark 的敌军 + rally | 待核（全场被动 + rally） | 自己攻击时播 |
| 8 | `TL11` | Venomthrope | Leviathan | 2/2 | `ToxicMiasma_Venomthrope` | 摧毁被本单位攻击的部队 | 攻方自身（`AttackedOps`） | 自己攻击时播 |
| 9 | `TL24` | Harpy | Leviathan | 4/7 | `Tyranid Spawn` | 当一个敌方单位攻击时，部署 TL37 | **监听型** | 别人攻击时**不播** |
| 10 | `UM70` | Invictor Warsuit | Ultramarines | 3/5 | `BulletImpact_2shot_autocannon` | 当一个敌方单位攻击时，造成 2 伤 | **监听型** | 别人攻击时**不播**（C3 的「永不播」只对「作为听众」成立） |
| 11 | `UM83` | Valtus | Ultramarines | 9/9 | `BulletImpact_gatling_1s` | 当友方单位攻击时，对目标造成 3 伤 | **监听型** | 别人攻击时**不播** |

> `ranged > attack`（判据取远程、自己**永远不近战**）的有 **6 张**：`EC1 1/4` · `EC2 2/6` · `SW16 2/5` ·
> `TL24 4/7` · `SAU31 4/8` · `UM70 3/5`。
> ⚠️ **C3 §四 D3 写「5 张」但列了 6 个 id**（`EC1 EC2 SW16 TL24 SAU31 UM70`）—— 现读是 **6 张**，这里就地订正。
> ⚠️ 这 6 张里 `EC1/EC2/SW16` 的动画在 ③ 下**本来就会播**（因为 ③ 不看远近）；只有 3 张监听型
> （`TL24`/`SAU31`/`UM70`）是「自己远程/近战攻击时也播，但语义上那条 ability 不成立」。

### 2·4 补丁方案（逐步骤）

> **总原则**：动画的唯一来源 = **引擎真的把这条 ability 跑了一次**那一拍（原版 `PlayAbility → PlayAnim`）。
> ③ 那种「攻方无条件播」是**临时口径**，要退掉。

**第 0 步（决策闸 · 必须先在 Unity 里跑一次探针，不改代码）**
在 `RuleEngineTest` / `BattleScene` 里插一个统计口：对 11 张卡各摆一局，
分别做「它自己攻击」与「队友/敌人攻击」两种，把 `ctx.Signals` 里 `Kind == Trigger` 的
`(CardId, Keyword)` 全打出来。要确认三件：
① 6 张监听型卡在**别人**攻击时确实收到 `keyword == "UnitAttack"`（预期：是，`EmitListenerTrigger`）；
② `EC1/EC2/TL11` 在自己攻击时收到的是 **`"attacked"`**（预期：是，`RuleCore.cs:5959`）；
③ `SW26` 属于哪一族（卡面「摧毁任何带 huntMark 的敌军」是全场被动；它是否走 `attacked` / 还是完全没事件）。
⇒ **探针结果决定第 3 步的写法**（尤其 `SW26`）。⛔ 别跳过这一步直接删 ③。

**第 1 步 · `Core/VfxMap.cs`（本代理的文件；下一批做）**
1. `CanonicalTriggerName`（现 `:2017-2023` 附近）新增别名：**`"attacked"` → `"UnitAttack"`**
   （判据 = `RuleCore.cs:5959` + `CardDef.AttackedOps` 的 doc：那一族的卡面原文就是「被本单位攻击的…」，
   原版 trigger 名 = `UnitAttack(50)`）。⚠️ 别名只写这一处（判据收口，⛔ 别在别处再写一份）。
2. `TryResolveTriggerPicks` 的「③ 两张桶一起」：
   ```csharp
   List<CardPick> trig, abil, unitAtk, unitDmg;
   m.TryGetValue(Trigger, out trig);
   m.TryGetValue(Ability, out abil);
   m.TryGetValue(UnitAttack, out unitAtk);      // 🆕 W63 把 50 那批搬到这个桶了
   m.TryGetValue(UnitDamaged, out unitDmg);     // 🆕 60 那批同理
   ```
   然后把 `unitAtk` / `unitDmg` 也按 `NameIs(target, "UnitAttack"/"UnitDamaged")` 各并一遍
   （`TriggerNameOf(times, p)` 对这两桶的候选**本来就解得出来** —— `A` = `CardAnimTime.Anim`）。
   ⚠️ **只并「目标名字对上」的那一个桶**，别无条件并（否则 `UnitAttack` 那拍会把 `UnitDamaged` 的件也播）。
3. 现有的「② 没有逐候选时间 ⇒ 原样回 trigger 桶」那一段要**加一句**：`target` 已知是
   `UnitAttack`/`UnitDamaged` 时，**不许**回粗桶（那两桶没有粗桶可言；回粗桶 = 播 `trigger` 桶里的 trait 件 = 错件）。

**第 2 步 · `Core/VfxMap.cs` 的断言（同批）**
扩 `SelfCheckRoutedTriggers`：对 `UnitAttack` 也做一遍件级检查（现读 11 张 / 12 条有件行；
`SAU31` 的第二行 `Wait_1s` 无件）。**但这条只有在第 3 步删掉 ③ 之后才该绿** ——
在那之前它会被 `SportOnly` 之类的东西干扰，所以**第 2 步与第 3 步必须同批上**。

**第 3 步 · `Battle/BattleDriver.cs`（A19 的文件，下一批执行）**
删除 ③ 那三行（`:17906-17921` 的 `float atkEnd = …` / `PlayUnitSkillAnim(e, VfxMap.UnitAttack, atkEnd);` 与整段注释），
改为**只在 Trigger 事件上播**。两个写法，按第 0 步的结果二选一：

- **写法 A（推荐，语义最正）**：攻方在 `DeclareAttack` 里发出的那条 `attacked` / 监听者那条 `UnitAttack`
  事件，经第 1 步的别名/合并，自然走 `ResolveAllTimed(Trigger, …, perCardOnly:true, keyword)`。
  **时刻**：Trigger 信号与攻击事件在**同一批** `Signals` 里（引擎同步发），表现层按 `EventTiming` 排 ⇒
  落在攻击动画之后的那一拍。**要验**：与改前 `atkEnd`（近战 0.4 / 远程 `RangedDamageLag`）的差 ≤ 一档事件间隔；
  超了就在 `EventTiming` 里给 `Trigger(UnitAttack)` 补一条与 `MeleeDamageLag`/`RangedDamageLag` 对齐的档
  （⛔ 别在 `ScheduleAttackPresentation` 里再补一次播放）。
- **写法 B（保时刻的回退档）**：保留 ③ 的**排期位置**，但**只在该卡这一击真的收到
  `UnitAttack`/`attacked` 信号时**才排（在 `PlaySignal` 处理 Trigger 时记一个
  `(cardId → 本批已触发)` 的小集合，`ScheduleAttackPresentation` 读它）。
  ⚠️ 顺序问题：`ScheduleAttackPresentation` 在 `EvtKind.Attack` 那一拍调用，**早于**同批的 Trigger 信号被弹
  ⇒ 集合此时还是空 ⇒ 写法 B **只能在信号**全部入队之后**重排**（或把这段挪到批尾），复杂度高。
  ⇒ **除非第 0 步证明「Trigger 事件的呈现时刻差得离谱」，否则走写法 A。**

**第 4 步 · 断言（`Editor/BattleScene.cs`，A19 的文件）**
1. **件级归口**：`Check(VfxMap.SelfCheckRoutedTriggers().Length == 0, …)`（见 §1·4 B 的代码，第 2 步扩完之后覆盖 UnitAttack）。
2. **监听型 6 张在别人攻击时播**：摆 `DA22` + 一个载具 → 载具攻击 → 断「这一击里 `DA22` 的
   `Trait_GainQuest`/`Healing_Projectile` 各有 1 拍」（走 `UnitSkillAnimBeats` 这种计数器或 `PlayUnitSkillAnimForTest` 的口）。
   🧨 把 `CanonicalTriggerName`/合并那一段改回去 ⇒ 红。
3. **不多播（灭自证）**：`DA22` **自己**攻击（自己不是载具）⇒ 断 `UnitSkillAnimBeats` **+0**。
   🧨 把 ③ 加回来 ⇒ 红。**这一条是本方案的核心**（它把「无条件播」和「跟着 ability 跑」区分开）。
4. **远程也算**：`UM70`（3/5，判据取远程）+ 一个敌方单位攻击 ⇒ 断播 1 拍 `BulletImpact_2shot_autocannon`。
5. **且只播一次**：`SW16`（self 监听 + 自己攻击）⇒ 断 `UnitSkillAnimBeats == 1`（不是 2）。
   🧨 ③ 与 Trigger 路同时活 ⇒ 红（这条专门挡「两边都播」）。

### 2·5 没查清（下一批开工前要补的）

1. **`SW26 Arjac Rockfist`** 属于哪一族（§2·4 第 0 步 ③）。
2. **`EC2/SW26/TL11` 的动画时刻**：`attacked` 那一跳在 `DeclareAttack` 里排在 `UnitAttack` 广播**之后**，
   原版 `BroadcastUnitAttacked` 的预调/循环次序是「被打者 → 攻方 → 其余」⇒ 这几张（属于「攻方」那一下）
   在队列里的位置是否与改前 `atkEnd` 等价，**没在运行时核过**。
3. `UM83` 的 `abilityContext` 是 `Play(0)`（不是 40）⇒ 它**待死时**原版不响，我们 `requireActorInPlay` 那边
   已经按数据取（`ActorMustBeInPlayForTrigger`），但**表现层**会不会播还没验（第 4 步的断言要挑一张非待死的样本）。

---

## 三 · D5：`Wait_*` 折叠 —— **常驻说明已落进 `VfxMap`**（`:1865-1879`）

写在「trigger 归口」那一节的头部，原文要旨：

- `Wait_1s` / `Wait_1,5s` / `Wait_300ms` / `Wait_2s` 是**纯计时拍**：`animAdressable` 是**空 GUID**
  （`数据/索引/anim_address_map.json` 的 `unresolved.cardAnimBlankGuid` 逐条列着）⇒ **原版本来就没有 prefab**。
- 运行期表把它们剔掉**不丢延迟** —— 延迟烙在 `cards_engine.json` 的 `animTimes[].t`（组偏移）里；
  逐卡四例的 `t` 对照见 `C3_动画特效审查_索引.md` §三·4（`GOF30` 0.0→0.3 · `GOF3` 0.0→1.0 · …）。
- ⛔ **别当缺陷去修**：补一条 `Wait_*` 占位 = 延迟**再算一遍**（特效晚到）；把 `t` 清零 = 特效**提前到**。

---

## 四 · 顺手发现（不是本批任务，按铁律 13·4⑧ 报上来）

1. 🔴 **C3 §四 D3 的「远程不播 / UM70 100% 永不播」已过期**（见 §2·1）—— 攻方自己那条动画
   自 2026-10-23 `W63` 起**近战/远程都播**（`BattleDriver.cs:17889-17900` 的 `if` 在 ③ **之前就闭合**）。
   ⇒ 建议把 C3 那张表里 D3 的「本批影响」改成「监听型 6 张不播 + 监听型卡自己攻击时多播」。
2. 🔴 **`CardAbility__CanTriggerAbility` 对 trigger 50 有 criteria 闸**（`CardAbility__CanTriggerAbility.c:126-140`），
   ⇒ 「`OnTrigger(0x32)` 无条件」不等于「动画无条件」。这条判据 C3 没写，直接影响 D3 的修法（见 §2·1）。
3. ⚠️ **C3 D3 写「5 张」但列了 6 个 id**；现读 `ranged > attack` 的是 **6 张**（§2·3 订正）。
4. 🆕 **`attacked` 关键词目前不在白名单**（`RuleCore.cs:5959`，`CardDef.AttackedOps` 那一族：
   `EC1/EC2/TL11`）。它不是原版 trigger 名、也没有 `CardAnimTime.TriggerName` 与它对应
   ⇒ 这条**不能**直接进 `IsRoutedTriggerName`（会永远等不到），**只能**当 §2·4 第 1 步那个**别名**处理。
   （已写进报告；代码没动 —— 因为动它会立刻与 ③ 双播。）
5. ⚠️ **C3 引的 `RuleCore.cs:8807-8822`（`Unstable` 没有发射口）行号偏了**：代码在
   **`:8829-8844`**（`:8807-8822` 是那段注释的起点）。已在 `NoEmitTriggerNames` 的注释里就地订正。
6. ✅ **五个白名单名字的逐卡行全都在运行期表里**（`Mob` 15/15 · `Teleport` 9/9 · `Uprising` 13/13 ·
   `Artifice` 14/14 · `Codex` 32/33，差的 1 行 = `UM28` 的 `Wait_1s` 纯计时拍）—— 下一批不必再查。
7. ℹ️ `SAU31 Doomstalker` 的第二行 `Wait_1s`（`t=0.75`）在 `UnitAttack` 桶里**没有件**
   —— 与 D5 同源（纯计时拍），⛔ 别把它当「`UnitAttack` 桶少一条」的缺陷。
8. 🔴🆕 **「一个 `CardAnim` 挂两个 trigger」的行，运行期分类键分不出来**（新发现，本批顺手量化）：
   `TriggerNameOf`（`VfxMap.cs`，先 `A` 后 `N`、**只取第一条**）对同一个 `CardAnim` 出现两次的行
   只能判成**先出现**的那个 trigger。现读命中 **2 行**：
   - `UM37 Death from Above` 的 `BulletTrail_Stormraven_SingleTarget`（`ThisCardPlayed` 0.0.2 ↔ `Codex` 1.0.0）
   - `UM84 Devastator Doctrine` 的 `Buff_Purple_target`（`ThisCardPlayed` 0.0.1 ↔ `Codex` 1.0.1）
   ⇒ 这两行的 **Codex 那一拍结构上播不出来**（视觉上由 `play` 桶同 prefab 的行盖住，**不是黑屏**）。
   **量化（23 张 Codex 卡逐张算过）**：放行 `Codex` 之后，**22 张**的 Codex 那一拍取到 1~2 件自己的件；
   **只有 `UM37` 一张**变成「什么都不播」—— 因为它唯一的 Codex 行就是那条分不出来的
   `BulletTrail_Stormraven_SingleTarget`（同时在 `play` 桶里，**同一件已经在出场那一拍播过**），
   而它 `trigger` 桶里原来播的 `CodexEffect`（Codex trait 的 Trigger(3) 件）会按「未标名一律丢」被丢掉。
   ⛔ 这 1 张是**已知代价**，不是新缺陷；要收口同样得让分类键带上 `Order`。
   自检用 `AmbiguousTriggerLabel` **跳过并记账**（⛔ 没有假装通过）。**这条建议进 `§29·b` 立账。**
9. ⚠️ **C3 D7 的 `Artifice` 记「6 条 / 3 张」只覆盖新三阵营**；全池现读 **14 行 / 11 张**
   （多出 **Sautekh 8 张**：`SAU70/69/25/26/6` 等）。⛔ 断言里别写死 3 —— 已在
   `MustRouteTriggerNames` 的注释里写明。
10. 🔴🆕 **第一版的 `case "Mob"/"Teleport"` 也是 PascalCase ⇒ 对 `"mob"/"teleport"` 恒 false**
    （与 D6 同一个 bug，协调方追加时才看出来）⇒ 已随 D6 的整段归一一起修好。
    ⚠️ **这条的教训**：以后加任何 trigger 名，**必须同时验「引擎实际发的小写形态」**，
    自检里那两条 `lo` / `up` 断言就是为此加的。

---

## 五 · 自检状态（本批）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_a21 bash 工具/typecheck.sh` | 最后一次：**运行时错误数 0 · 编辑器错误数 0** |
| 中途出现过的红 | `CardDatabase.cs(1580,24) CS0426` 与 `RuleEngineTest.cs(26463,12) CS7036` —— **都在别人正在写的文件里**（A20），本代理**没有**改；等它们收工后重跑即绿（最后一次已经 0） |
| 件级自检预演 | 按 `SelfCheckRoutedTriggers` 的**同一套逻辑**用 Python 在 `cards_engine.json` + `card_vfx_by_card.json` 上跑了一遍：**5 个名字全部 0 违规**（`Codex` 跳过的 2 行见 §四·8） |
| Unity 批处理 | ⛔ 未跑（本批规则） |
| 行尾 | `git diff --numstat` = **380 / 67**；删除行全部是被替换的旧注释（大段 `case` 标签小写化 + 上面那几段注释重写）；`:83-400` 那批 hunks 是**本代理开工前**工作区里已有的 ambush 段改动（与本批无关）⇒ **没有翻行尾** |
