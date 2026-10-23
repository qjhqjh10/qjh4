# A27 · `VfxMap` 第三批白名单（`C4` D6）+ `TriggerNameOfWhenKind` 补丁方案（D7）

> 执行代理 **A27** · 2026-10-24 · 有写权限。
> **实际改动只有一个文件**：`Unity/MyGame/Assets/CardPresentation/Core/VfxMap.cs`
> （`git diff --numstat` = **459 / 67**，其中 **A21 遗留 380 / 67**，本批 **+79 行、0 行删除**）。
> **没动**：`RuleEngine/**`（A24 在写）· `Battle/**`（A23）· `Hand/**`（A26）· `Shell/**` · `Editor/**` ·
> `工具/**` · `WarpforgeVFX/**` · `Core/` 其余文件 · 正本 · 任务总表 · git。
> **没跑 Unity**；`TMPDIR=/tmp/wf_a27 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（改完跑了两次，都是 0/0）。
> 判据来源：`A21_VfxMap白名单.md`（做法 + 自检结构，⛔ 没重造）· `C4_动画特效审查_索引.md` §二 / §四 D6(C4) / D7(C4)；
> 发射口全部**回 `RuleEngine/Core/` 现读**，候选规模全部**回 `cards_engine.json` 现算**。

---

## 一 · 结论（第 1 件：**已做完**）

`IsRoutedTriggerName` 放行 3 个**引擎已经在发、只差白名单**的名字；
反向闸 `NoEmitTriggerNames` 同时把 4 个「还不许进」的名字锁死。

| # | 名字 | 枚举 | 条目 / 卡（现读） | 引擎发射口（逐处现核） | 放行前 | 本批 |
|---|---|---|---|---|---|---|
| D6-a | `Cruelty` | 720 | **10 行 / 8 张**（全 EC） | `RuleCore.cs:8076` `FireTriggerOnSide(ctx, ctx.Active, KeywordTable.Cruelty)` | 整桶播 `CrueltyEffect`（错件）/ 无 `trigger` 桶的卡**什么都不播** | ✅ 进白名单 |
| D6-b | `Stimulation` | 710 | **10 行 / 10 张**（全 EC） | `EffectResolver.cs:2080` `FireTriggerAt(ctx, st, KeywordTable.Stimulation, sp, sslot)` | 同上（`RallyEffect` / `TalentEffect` / 空） | ✅ 进白名单 |
| D6-c | `Ecstasy` | 715 | **7 行 / 7 张**（全 EC） | `RuleCore.cs:8065` `FireTriggerOnBoard(ctx, u, KeywordTable.Ecstasy)` | 整桶播 `EcstasyEffect` / `ArmourEffect`（错件） | ✅ 进白名单 |

**合计 27 行 / 24 张**（`EC22 Veldras the Sublime` 同时带 `Cruelty` + `Stimulation`，所以 8+10+7 行对 24 张卡）。
三家全在 **EmperorsChildren**（与 C4 的分布一致，现读复核）。

**反向闸新增 4 个（`NoEmitTriggerNames` 8 → 12）** —— 这 4 个名字**本批一个都没进白名单**，且现在有断言盯着：

| 名字 | 枚举 | 条目 / 卡 | 为什么还不许进（现读） |
|---|---|---|---|
| `UnitReceivesHuntMark` | 520 | 5 / 2（`SW20`/`SW35`） | **监听事件有**（`WhenEventKind.GetsHuntMark`，`EffectResolver.cs:9247` 广播），但 `TriggerNameOfWhenKind`（`:6183-6214`）**没有这个分支** ⇒ `EmitListenerTrigger`（`:6281-6289`）直接 return ⇒ **`EvtKind.Trigger` 从不发** |
| `OnCollectFaith` | 665 | 2 / 1（`SOR38`） | 同上（`WhenEventKind.GainFaith` 有广播：`:9921`/`:9963`，映射没有） |
| `ThisUnitSummoned` | 195 | 1 / 1（`SW10`） | 引擎部署那一拍发 **`rally`**（`RuleCore.cs:3658`/`:4300`），听者侧映射 **`OtherUnitSummoned`**（`EffectResolver.cs:6147`）⇒ 没有任何一处发这个名字 |
| `WhileInPlay` | 20 | 2 / 1（`SW18`） | 常驻能力，本仓走 `Aura.Recompose`、**不发 `EvtKind.Trigger`** ⇒ 今天没有「那一拍」 |

---

## 二 · 证据（全部现读，不是转述）

### 2·1 三个发射口逐句

| 项 | 现读 |
|---|---|
| `Cruelty` | `Core/RuleCore.cs:8076`：`FireTriggerOnSide(ctx, ctx.Active, KeywordTable.Cruelty);` —— 在 `HurtCore` 尾部，条件是 `dealt > 0 && u.IsAlive` 且 `hurtOwner >= 0 && hurtOwner != ctx.Active`（**你的回合、敌方单位受伤未死 ⇒ 己方**那一排各响一次） |
| `Stimulation` | `Core/EffectResolver.cs:2075-2080`：`if (chosen != null && chosen.Has(KeywordTable.Stimulation)) { … FireTriggerAt(ctx, st, KeywordTable.Stimulation, sp, sslot); }` —— 在 `PlayTactic` 里，**结算之前** |
| `Ecstasy` | `Core/RuleCore.cs:8059-8065`：`if (!EnoughPendingDamageToDie(u) && u.Card != null && u.Card.Has(KeywordTable.Ecstasy)) { … FireTriggerOnBoard(ctx, u, KeywordTable.Ecstasy); }` |
| keyword 常量 | `CardDef.cs:5241` `Ecstasy = "ecstasy"` · `:5321` `Cruelty = "cruelty"` · `:5428` `Stimulation = "stimulation"`（**全小写**） |
| 信号出口 | `FireTriggerAt` 的 `emitSignal`（`RuleCore.cs:10740-10742`）：`ctx.Emit(EvtKind.Trigger, owner, slot, u.Name, keyword: keyword, …)` —— **keyword 原样写入，不大小写化** |
| 生产路径传 keyword | `Battle/BattleDriver.cs:18411-18422`：`evt = VfxMap.Trigger; perCardOnly = true;` + `string triggerKeyword = e.Kind == EvtKind.Trigger ? e.Keyword : null;` ⇒ 白名单命中后走 `ResolveAllTimed(..., keyword)` |
| 一道附带的广播 | `FireTriggerOnSide`（`RuleCore.cs:11370-11399`）先 `t.HasFx(keyword)` 过滤、再按 `PlayOrder` 逐单位 `FireTriggerAt` —— **发射条件 = 卡自己那条正文/效果解出来了**（见 §五·1 的「没查清」） |
| 旁证 | `Core/ActionPump.cs:389-391` 早已把 `stimulation`/`ecstasy`/`cruelty` 映射成原版 trigger 名（`TriggerLane` 用）⇒ 引擎一侧本来就把它们当一等触发 |

### 2·2 候选规模 + 质量（`cards_engine.json` 现算，判据 = `animTimes[].tn`）

| 名字 | 卡 / 行 | 行无 prefab | 「一个 CardAnim 挂两个 trigger」（`AmbiguousTriggerLabel` 会跳过的） | 阵营 |
|---|---|---|---|---|
| `Cruelty` | **8 / 10**（`EC23`/`EC26` 各 2 行） | **0** | **0** | 全 EC |
| `Stimulation` | **10 / 10** | **0** | **0** | 全 EC |
| `Ecstasy` | **7 / 7** | **0** | **0** | 全 EC |

逐卡（现读）：
- `Cruelty`：`EC8` · `EC20` · `EC22` · `EC23`(2) · `EC24` · `EC26`(2) · `EC28` · `EC35`
- `Stimulation`：`EC3` · `EC6` · `EC10` · `EC13` · `EC14` · `EC18` · `EC21` · `EC22` · `EC55` · `EC56`
- `Ecstasy`：`EC5` · `EC7` · `EC9` · `EC25` · `EC30` · `EC31` · `EC57`

### 2·3 放行前后逐卡对照（现读运行期表 `Resources/card_vfx_by_card.json`）

「放行前」= `target == null` ⇒ `TryResolveTriggerPicks` 走 ④「原样回 `trigger` 粗桶」；
「放行后」= 只留标到该 trigger 名的候选（`trigger` + `ability` 两张桶一起筛）。

| 卡 | trigger 名 | 放行前 `trigger` 桶（播的） | 放行后（该播的） |
|---|---|---|---|
| `EC35` | Cruelty | `FlankEffect` + `CrueltyEffect` | `Emperors Children Generic Buff Record` |
| `EC24` | Cruelty | `CrueltyEffect` | `Emperors Children Generic Buff Record` |
| `EC8` | Cruelty | `CrueltyEffect` | `Buff EC Pink Twirl` |
| `EC23` | Cruelty | `CrueltyEffect` | `Shadow_Projectile` + `Buff EC Pink Twirl` |
| `EC26` | Cruelty | `ArmourEffect` + `CrueltyEffect` | `Sword_Slash_EC_User` + `Cut EC` |
| `EC20` | Cruelty | `CrueltyEffect` | `MarkOfSlaanesh_enchantment` |
| `EC28` | Cruelty | `ArmourEffect` + `CrueltyEffect` | `Emperors Children Summon` |
| `EC22` | Cruelty | `CrueltyEffect` | `Buff EC Pink Twirl` |
| `EC22` | Stimulation | （同一个粗桶） | `EC Target` |
| `EC3` | Stimulation | **无 `trigger` 桶 ⇒ 什么都不播** | `Debuff EC` |
| `EC18` | Stimulation | 无 ⇒ 什么都不播 | `Buff EC Pink Twirl` |
| `EC13` | Stimulation | 无 ⇒ 什么都不播 | `Buff EC Pink Twirl` |
| `EC14` | Stimulation | 无 ⇒ 什么都不播 | `Debuff EC` |
| `EC6` | Stimulation | 无 ⇒ 什么都不播 | `Buff EC Pink Twirl` |
| `EC10` | Stimulation | `RallyEffect` | `Buff EC Stimulate` |
| `EC21` | Stimulation | `ArmourEffect` + `EcstasyEffect` | `Buff EC Pink Twirl` |
| `EC55` | Stimulation | `TalentEffect` | `Whip Attack EC Lucius` |
| `EC56` | Stimulation | `TalentEffect` | `Buff EC Stimulate` |
| `EC9` | Ecstasy | `EcstasyEffect` | `MarkOfSlaanesh_enchantment` |
| `EC25` | Ecstasy | `ArmourEffect` + `EcstasyEffect` | `Buff EC Pink Twirl` |
| `EC7` | Ecstasy | `EcstasyEffect` | `Buff EC Pink Twirl` |
| `EC5` | Ecstasy | `EcstasyEffect` | `BulletImpact_3shot_trail_EC` |
| `EC31` | Ecstasy | `EcstasyEffect` | `Emperors Children Summon` |
| `EC30` | Ecstasy | `EcstasyEffect` | `Buff_HideousMutation_EC` |
| `EC57` | Ecstasy | `EcstasyEffect` + `TalentEffect` | `Buff EC Pink Twirl Intense` |

> ⇒ **5 张卡从「什么都不播」变成播自己的技能动画**（`EC3/6/13/14/18`），其余 19 张从「播 trait 件（错件）」变成播对。
> 件级自检用 Python 按 `SelfCheckRoutedTriggers` 的**同一套逻辑**预演过：**三个名字全部 0 违规**（不多播、不漏播）。

### 2·4 大小写 / 撞名（复核 A21 的前提，本批没有新增风险）

- 引擎发**小写**、候选是 PascalCase ⇒ 白名单靠方法头的 `ToLowerInvariant()`、候选靠 `NameIs` 的 `OrdinalIgnoreCase`。
- 三个新名字与已有 case **无撞名**（`cruelty`/`stimulation`/`ecstasy` 与任何已放行名都不同）。
- 原版 `AbilityTrigger` 枚举（`CardDef.cs:1585-1620` 那张表）里**没有只差大小写的两个名字**（A21 已逐条核；本批复核这三个不构成反例）。

---

## 三 · 改动清单（`VfxMap.cs` 现行号）

| 位置 | 改了什么 |
|---|---|
| `:1903-1904` | `IsRoutedTriggerName` 的头部 doc 追加一行：A27 再放行三个（指向下面那段 `case`） |
| `:2029-2072` | **新增整段注释 + 三个小写 `case`**（`:2068-2071`）：每个的发射口、keyword 常量、候选读数、不放行的后果、发射闸提醒、⛔ 四个不许加的名字 |
| `:2262-2270` | `NoEmitTriggerNames` 的 `<para>`：判据改成「**两种成因**」——① 事件根本不存在；② 事件有、但名字到不了 `EvtKind.Trigger`（映射断 / 名字不对）。不改措辞的话，新加的四条会被误读成「连事件都没有」 |
| `:2274-2297` | `NoEmitTriggerNames` 的逐条 `<item>` 新增 4 条（各带判据与「补完要删本行」的说明） |
| `:2298-2303` | 数组本体 **8 → 12**：`"UnitReceivesHuntMark", "OnCollectFaith", "ThisUnitSummoned", "WhileInPlay"` |
| `:2310-2314` | `MustRouteTriggerNames` **5 → 8**：加 `"Cruelty", "Stimulation", "Ecstasy"` |
| `:2321-2325` | `MustRouteTriggerKeywords` **同长同序**：加 `KeywordTable.Cruelty, KeywordTable.Stimulation, KeywordTable.Ecstasy` |
| `:2380-2384` | `SelfCheckRoutedTriggers` 的 doc 现读口径补三个名字的行数 |

**没有新增 API、没有改 `private` 可见性**（`IsRoutedTriggerName` 仍是 private，宿主只经 `SelfCheckRoutedTriggerNames()` 间接验 —— 与 A21 §1·5 的口径一致）。
**行尾**：`CRLF 0 / LF 2624`（改前改后同值）—— 没翻行尾。

---

## 四 · 断言（**怎么才会红**）

### A. 常驻（已经在跑，不需要自检宿主）

`EnsureLoaded()`（`VfxMap.cs:825-831`）载表时跑一次 `SelfCheckRoutedTriggerNames()`，违规 → `Debug.LogError("[Vfx][ASSERT] …")`：

- 🧨 把 `NoEmitTriggerNames` 里**任一**名字（含新加的 `UnitReceivesHuntMark` / `OnCollectFaith` / `ThisUnitSummoned` / `WhileInPlay`）加进 `IsRoutedTriggerName` ⇒ **LogError**。
  （这正是本批新增 4 条的意义：D7 修完之后，如果只补映射、忘了从反向闸里删，而有人顺手把名字加进白名单 ⇒ 红。）
- 🧨 把 8 个正向名字里任一个删掉 ⇒ **LogError**（防误删）。
- 🧨 把 `switch (triggerName.ToLowerInvariant())` 改回 `switch (triggerName)` ⇒ `"codex"` / `"cruelty"` / `"stimulation"` / `"ecstasy"` 那几态挂 ⇒ **LogError**（D6 复发闸）。
- 🔑 **大小写三态**自动覆盖：`SelfCheckRoutedTriggerNames` 的第二个循环对 `MustRouteTriggerNames` 的**每个**名字都验 `n` / `ToLowerInvariant()` / `ToUpperInvariant()` 三态 ⇒ 三个新名字**不需要另写断言**（⛔ 别重复加一份）。
- 🧨 新加的三条 `case` 若写成 PascalCase（`case "Cruelty"`）⇒ 上面那条 `lo` 断言先红（与 D6 同一个 bug）。

### B. 自检宿主（⛔ 本批不能改 `Editor/**`；下一批把这段贴进 `BattleScene.Run` 的 `W57/T21` 那一段旁边）

**不用改代码** —— A21 已经写好那两条，本批只是让它们**自动多覆盖三个名字**（循环走 `MustRouteTriggerNames`）：

```csharp
var a21Names = VfxMap.SelfCheckRoutedTriggerNames();
Check(a21Names.Length == 0,
      "★ A27/D6（名字级）：`Mob`/`Teleport`/`Codex`/`Uprising`/`Artifice`/"
    + "`Cruelty`/`Stimulation`/`Ecstasy` 都在白名单里（且 PascalCase / 小写 / 全大写三态都命中）；"
    + "`UnitReceivesHuntMark`/`OnCollectFaith`/`ThisUnitSummoned`/`WhileInPlay` 不在"
    + (a21Names.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", a21Names))
    + " —— 🧨 把 switch 改回大小写敏感、删掉任一正向 case、或把那四个名字加进去 ⇒ 本条红");

var a21Bad = VfxMap.SelfCheckRoutedTriggers();   // 件级：会载卡池
Check(a21Bad.Length == 0,
      "★ A27/D6（件级）：八个 trigger 的逐卡归口**不多不少**"
    + "（新增 Cruelty 8/10 · Stimulation 10/10 · Ecstasy 7/7）"
    + (a21Bad.Length == 0 ? "" : "\n  ✗ " + string.Join("\n  ✗ ", a21Bad))
    + " —— 🧨 把 `TryResolveTriggerPicks` 的 ③ 改回「只查 trigger 桶」⇒ 每张卡都报「漏播」");
```

### C. 件级预演（本机 Python，按 `SelfCheckRoutedTriggers` 的同一逻辑跑）

对 `ByCard` 里每张有 `TriggerName == <名字>` 行的卡，按生产路径逐参（`perCardOnly: true` + 引擎小写 keyword）合并
`trigger` + `ability` 两桶，逐件比对：
**`Cruelty` 8 张/10 行 · `Stimulation` 10 张/10 行 · `Ecstasy` 7 张/7 行 —— 三个名字全部 0 违规**
（多播 0 · 漏播 0 · 歧义 0 · 无件 0）。

---

## 五 · 没查清的部分（如实记，⛔ 没有猜着写）

1. 🔴 **正文解析器本批没跑**（不跑 Unity）。`FireTriggerAt` 有一道「**没正文就不发信号**」的闸
   （`RuleCore.cs:10748-10750`：`ops == null && spec == null && skipDraw == null && 附魔空` ⇒ `return false`），
   而 `FireTriggerOnSide` 还先按 `t.HasFx(keyword)` 过滤（`:11383`）。
   **静态判据（现读成立）**：三个词都在 `CardDef.RoutableTriggers`（`:1220-1246`）与 `CardDef.BodyKeywords`（`:1397-1403`）里；
   裸写正文那几张（`desc` 无 `Xxx:` 前缀）靠 `CollectBareKeywordBody` 的「**带正文关键词唯一才敢认**」那道闸 ——
   现读它们的其余关键词（`Flank` / `Shield` / `Armour` / `Talent` / `Rally`）**都不在 `BodyKeywords`**
   ⇒ 闸过得去。**但 `EffectText.Parse` 是否对每张卡都解出 op，没有跑过**。
   受影响最可疑的 5 张（裸写）：`EC35`（kw=`Flank,Cruelty`）· `EC20`（`Shield,Cruelty`）· `EC23`（`Cruelty`）·
   `EC3`（`Stimulation`）· `EC31`（`Ecstasy 5`）。
   **建议**：真机放一次 / `RuleEngineTest` 里逐卡看日志有没有 `触发 CRUELTY` / `触发 STIMULATION` / `触发 ECSTASY` 那一行。
2. **`Stimulation` 的发射条件用「当前关键词」**（`EffectResolver.cs:2075` `chosen.Has(...)`）——
   运行时**被授予**的 `stimulation` 也会触发。原版是否只认印刷关键词：**本批没核**（与 C4 §七·5 同一族问题）。
3. **放行后的取舍（⛔ 别当回归去「修」）**：`trigger` 桶里的 `CrueltyEffect` / `EcstasyEffect` / `ArmourEffect` /
   `TalentEffect` / `RallyEffect` 都是 trait 件，在 `CardDef.AnimTimes` 里**没有行** ⇒ `TriggerNameOf` 回 null ⇒
   被「未标名的一律丢」丢掉 ⇒ 这一拍**只播卡自己那条技能动画**。原版是两件都播。
   补齐属于 **D4 / `P7`**（与 A21 §1·6 同一取舍）。
4. 以上全部是**读码 / 数据结论，没跑 Unity** —— 坐实最快的办法：真机让 `EC9` 掉血越过狂喜阈值、`EC35` 触发残忍，
   看播的是 `MarkOfSlaanesh_enchantment` / `Emperors Children Generic Buff Record` 还是 trait 件。

---

## 六 · 第 2 件：`TriggerNameOfWhenKind` 补两个分支 —— **完整补丁方案（本批未动 `RuleEngine/**`）**

### 6·1 改哪个方法（🔴 行号就地订正）

- 文件：`Unity/MyGame/Assets/RuleEngine/Core/EffectResolver.cs`
- 方法：`static string TriggerNameOfWhenKind(string kind)` —— **`:6183-6214`**
  （⚠️ C4 与 A21 都引 `:6141-6172`，**已漂移**；`EmitListenerTrigger` 现读 **`:6281-6289`**，不是 `:6239-6243`）。

### 6·2 加什么分支（逐行照抄即可）

**① 常量区**（`:6160-6168` 那一组 `TN_*`，与 `TN_UnitReceivesMark = "UnitReceivesMarkOfChaos"; // 510` 并排）：

```csharp
const string TN_UnitReceivesHuntMark = "UnitReceivesHuntMark";  // 520
const string TN_OnCollectFaith       = "OnCollectFaith";        // 665
```

**② `switch (kind)` 里**（与 `case WhenEventKind.GetsDarkPact: return TN_UnitReceivesMark;` 同一层）：

```csharp
case WhenEventKind.GetsHuntMark:  return TN_UnitReceivesHuntMark;
case WhenEventKind.GainFaith:     return TN_OnCollectFaith;
```

**③ 对应哪个常量（⚠️ 这里最容易找错）**

| 要的东西 | 真值 | 出处 |
|---|---|---|
| `kind` 的键（case 标签） | `WhenEventKind.GetsHuntMark` = `"gethuntmark"` · `WhenEventKind.GainFaith` = `"gainfaith"` | `WhenEvent.cs:190` / `:441`；`:107` / `:433` |
| 返回的字符串 | 原版 `AbilityTrigger` 枚举名 `UnitReceivesHuntMark` = **520** · `OnCollectFaith` = **665** | `CardDef.cs:1605` / `:1612`（那张 `{名字, 数值}` 表） |
| 候选侧的同名键 | `cards_engine.json` 的 `animTimes[].tn`（SW20 2 行 · SW35 3 行 · SOR38 2 行，现读） | — |
| **`KeywordTable` 常量** | **不存在、也不该新造** —— 这两个是 `AbilityTrigger` 名，**不是**引擎触发关键词（`KeywordTable` 里没有 `UnitReceivesHuntMark` / `OnCollectFaith`，现 grep 0 命中） | ⛔ 别去找、别新造一个 |

**④ 映射之外还要动的两处（必须同批）**

1. `VfxMap.NoEmitTriggerNames`：**删掉** `"UnitReceivesHuntMark"` 与 `"OnCollectFaith"` 两行
   （删这一行 = 「我核过发射口了」的签名；不删 ⇒ 常驻闸 LogError）。
2. `VfxMap.IsRoutedTriggerName`：加 `case "unitreceiveshuntmark":` / `case "oncollectfaith":`（**小写**），
   并放进 `MustRouteTriggerNames` / `MustRouteTriggerKeywords`（**同长同序**）。
   ⚠️ **顺序错了会很难查**：只补映射不补白名单 ⇒ 事件发出了，但 `target` 恒 null ⇒ 走 ④ 整桶播（错件）；
   只补白名单不补映射 ⇒ 事件根本不发（白名单空转）。**三处必须同批上**。

### 6·3 补上之后哪些环节会跟着变（要一起验，别只看 VfxMap）

| 环节 | 现读 | 补完后 | 风险 |
|---|---|---|---|
| `EmitCardTrigger` 的闸 | `HasTriggerSource`（`:6267-6278`）：① 有动画行 **或** ② 真值 `abilityTriggers` 说它有这条 ability | SW20/SW35/SOR38 走 ① 就过（`UnitReceivesHuntMark` 行：SW20 2 行 · SW35 3 行；`OnCollectFaith` 行：SOR38 2 行，**都有 prefab**）；② 也过：`abilityTriggers` 现读 SW20 `[0,520]` · SW35 `[520]` · SOR38 `[605,665]`（`AbilityTriggerPresence("UnitReceivesHuntMark")` 走 `CardDef` 名字表 → 520，现核） | 无 |
| `ListenerLane`（`ActionPump.cs:644`） | `triggerName == null` ⇒ 默认 `1 Instant` | 查数据 `abilityTriggerPriority`（v18）；这三张卡**现读没有该列** ⇒ 仍回默认 1 | 无（次序不变） |
| `AbilityContextOfAbilityV14`（`EffectResolver.cs:8090`） | `tn == null` ⇒ `-1` **未知** ⇒ 调用方回退旧口径 | v21 表 + 缺行 = `Play(0)` ⇒ **context 闸开始对这两族生效**（要求载体在场上、且不是待死） | ⚠️ **可能比原版严**：这三张卡现读**没有 `abilityContexts` 列** ⇒ 会被判成 `Play(0)`。要验「监听者被打成待死时还响不响」 |
| `PendingDeathAllowsListener`（`:8137`） | 只对 `WhenEventKind.Attack` 读 context | **不受影响**（这两族不是 Attack） | 无 |
| 事件源 | `BroadcastKeywordEvent(GetsHuntMark)`（`:9247`，`RuleCore` 加 huntmark 时调）· `BroadcastWhen(GainFaith)`（`:9921`/`:9963`） | 不变 | 无 |

> **安全边界（现读全池）**：`abilityTriggers` 含 **520** 的卡**只有** `SW20`/`SW35`；含 **665** 的**只有** `SOR38`；
> 含 **195** 的只有 `SW10` ⇒ 补这两条映射**不会**给别的卡凭空发事件（与 C4 §四 D7 的判断一致）。

### 6·4 断言怎么写（`Editor/RuleEngineTest.cs`；⛔ 不要为了「直接调」把 `TriggerNameOfWhenKind` 改成 public）

**行为断言（推荐，端到端，不新增口径）** —— 四条：

1. **`UnitReceivesHuntMark` 发得出来**：摆 `SW20` + 一个敌方单位 → 给那个敌方单位加 `huntmark`
   （走现成的 `ApplyOneGain` / `Give` 路径，⛔ 别手写 `Board[i].AddKeyword` 绕过 `BroadcastKeywordEvent`）→ 泵队；
   断言 `ctx.Signals` 里存在 `Kind == EvtKind.Trigger && Keyword == "UnitReceivesHuntMark" && CardId == "Thunderwolf Cavalry Pack Leader"`。
   🧨 把 `case WhenEventKind.GetsHuntMark` 删掉 ⇒ **红**（今天就是这一态）。
   🧨 把 `case` 写进别的 kind（例如 `Deploy`）⇒ 红。
2. **`SW35` 同一族**：同一个事件，`CardId == "Venerable Dreadnought"` 的那条触发也在（它 3 行动画，**事件只该 1 条** —— 每行是动画不是事件）。
3. **`OnCollectFaith` 发得出来**：用现成的「获得信仰」夹具（`RuleEngineTest.cs:18448` 已经在断 `WhenEventKind.GainFaith`，
   把它当起点）→ 断言 `Keyword == "OnCollectFaith" && CardId == "Paragon Warsuit"`。
   🧨 删 `case WhenEventKind.GainFaith` ⇒ **红**。
4. **反向（灭自证）**：`SW10` 部署完之后，断言 `Signals` 里 **没有** `Keyword == "ThisUnitSummoned"`；
   同时 `SW18` 在光环重算之后**也没有** `Keyword == "WhileInPlay"`。
   🧨 有人把 `Deploy` 也映射成 `ThisUnitSummoned`（D8 的粗暴修法）、或给 `WhileInPlay` 接一条每帧发射 ⇒ **红**。

**白名单闸（挂在同一宿主）**：`Check(VfxMap.SelfCheckRoutedTriggerNames().Length == 0, …)`
—— 补完映射+白名单、**但忘了从 `NoEmitTriggerNames` 删那两行** ⇒ 这一条**红**（这是好事：它逼着三处同批改）。

**表现层件级（`Editor/BattleScene.cs`，属 A19 的文件，另案）**：
- `SW20` 那一拍应播 `Buff_Blue_SW_Wolf` + `Buff_Red`；
- `SW35` 那一拍应播 `Axe_Slash_User_SW` + `Cut Axe SW` **×2**（同名两行 —— `SelfCheckRoutedTriggers` 用 `left.Remove` **逐次消费**，
  所以「出现 2 次」才算对）；
- `SOR38`：`BulletImpact_Sororitas_MultiMelta` 必须播；`Faith_trigger_unit` **会因「一个 `CardAnim` 挂两个 trigger」被判 ambiguous 跳过**
  （它同时挂在 `OnCollectFaith` 与 `UseFaith` 下，见 §七·4）—— 这是**已知代价**，不是新缺陷。

### 6·5 ⛔ 这个方案里**不要**顺手做的事

- ⛔ **别把 `Deploy` 支改成 `ThisUnitSummoned`（D8）**：`WhenEvent.SelfOnly` 的判别在签名里**没有参数**
  （`TriggerNameOfWhenKind(string kind)`），要改就得加形参 / 另开方法，而且会影响 `OtherUnitSummoned(190)` 那一整族。
  另案，先核「自触发 vs 别的单位部署」在 `FireBoardListener` 里怎么分流。
- ⛔ **别给 `WhileInPlay` 接「每次重算都发」**（D9）：先定频率（原版缓存只比己方单位数），⛔ 别每帧发；
  而且 `SW18` 的正文是裸写、`TriggerOps("whileinplay")` 今天大概也是空的，`FireTriggerAt` 那道「没正文不发」的闸还会挡它。
- ⛔ 别把 `EmitListenerTrigger` 的 `tn == null` 改成「猜一个名字」—— 那是拿别的 trigger 的动画行来播。

---

## 七 · 顺手发现（不是本批任务，按铁律 13·4⑧ 报上来）

1. 🔴 **行号漂移（就地订正）**：C4 §二 / §四 D7 与 A21 §四 都引 `TriggerNameOfWhenKind :6141-6172`、`EmitListenerTrigger :6239-6243`；
   **现读是 `:6183-6214` 与 `:6281-6289`**。本批在 `VfxMap` 的注释里已按现读写，其余文档没权改。
2. ⚠️ **C4 §九 最后那条「交付前现读」现在过期了**：它写「`case "cruelty"/"stimulation"/"ecstasy"` 逐个 grep 都是 0 命中」——
   **本批已落地**（`VfxMap.cs:2068-2071`）。建议协调方就地更新 C4 索引（⛔ 我无权改别人的报告）。
3. ✅ **`ActionPump.cs:389-391` 早就有这三个 keyword → 原版名的映射**（`TriggerLane` 用）——
   可作为「引擎把它们当一等触发」的旁证；也说明本次白名单不是「新造语义」。
4. 🟠 **`SOR38 Paragon Warsuit` 的 `Faith_trigger_unit` 同时挂在 `OnCollectFaith` 与 `UseFaith` 下**
   （现读 `animTimes`：`OnCollectFaith` → `Faith_trigger_unit`、`UseFaith` → `Faith_trigger_unit`）⇒
   走 `TriggerNameOf`（只取第一条）时**分不出来**，与 A21 §四·8 的 `UM37`/`UM84` **同一族问题**。
   D7 补完后件级自检会把它算 1 行 ambiguous 并**如实跳过**；要收口同样得让分类键带上 `Order`。
5. ℹ️ **`WhileInPlay`(20) 的 `abilityTriggers` 现读有 13 张卡**（`AM72`/`BL27`/`SAU66`/`GOF91`/`GOF29`/`SW18`/`SW36`/`SW38`/`TAU32`/`TAU29`/`TAU24`/`TAU20`/…），
   但**只有 `SW18` 有 `tn == "WhileInPlay"` 的动画行**。⇒ D9 的「动画面」确实只有 1 张，
   但一旦有人给 `WhileInPlay` 补发射口，**能力结算**会波及 13 张 —— 排期时要注意这个面比 C4 记的「1 张」大。
6. ℹ️ `SW35 Venerable Dreadnought` 的 3 条 `UnitReceivesHuntMark` 行里 `Cut Axe SW` **出现两次**（不同 `o` 序位）
   ⇒ 件级自检要「出现 2 次」才算对；⛔ 别按名字去重（`SelfCheckRoutedTriggers` 用 `left.Remove` 逐次消费，天然支持重复名）。
7. ℹ️ 本批放行后，EC 这 24 张卡的 `trigger` 桶里那些 trait 件（`CrueltyEffect` / `EcstasyEffect` / `ArmourEffect` /
   `TalentEffect` / `RallyEffect`）**不再播** —— 与 A21 §1·6 是同一取舍，属 **D4 / `P7`**，⛔ 别当回归。

---

## 八 · 自检状态（本批）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_a27 bash 工具/typecheck.sh` | **运行时 0 · 编辑器 0**（两次都是） |
| `git diff --numstat`（`VfxMap.cs`） | **459 / 67**（A21 基线 380 / 67 ⇒ 本批 **+79 / +0**） |
| 行尾 | `CRLF 0 · LF 2624`（改前改后同值）⇒ 没翻 |
| 件级预演（Python，同 `SelfCheckRoutedTriggers` 逻辑） | 三个名字 **0 违规**（8/10 · 10/10 · 7/7） |
| Unity 批处理 | ⛔ 未跑（本批规则） |
| `RuleEngine/**` | ⛔ 一个字节没动（第 2 件只写方案） |
