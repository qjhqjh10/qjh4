# B141-W77 · `B141-DEAD-CANDIDATES` 结案报告（`DeadCandidates` 换到墓地那一口）

> 执行写手 `B141-W77` · 批次 B141 · 会话第一百二十八 · 2026-11-04 · 波次 W128
> 输入 = `报告/B141_W128_CHK05_336_77.md` §四（`W65` 顺手查出、⛔ 未改的那条真缺口）。
> 白名单实动文件：`RuleEngine/Core/EffectResolver.cs`（`DeadCandidates` 本体 + 1 处调用点 + 1 处过期注释 + 1 个自检门）· 新建 `RuleEngine/Editor/B141_W77_Assert.cs` · 本报告。

---

## 〇、一句话结论

`EffectResolver.DeadCandidates`（`choose … died this game / this battle / since your last turn` 的候选口）
**已从「手扫 `ctx.DeadUnits`」换到墓地那一口**（`CemeteryDeadUnits` → `ResolveCemeteryTargets` → `RuleCore.GetCardsInCemetery`，
含 `remnantIds` 残骸闸 + 墓地放逐闸 + 卡种 + 侧别四道）—— 被摧毁的残骸**不再**出现在这 5 张卡的候选里（原版同样选不到）。
`typecheck 0/0` · `ruleprobe check` rc=0（解析差异 0 行、哨兵 0 失败）· `play check` rc=0 且**结算指纹未变**
（`a0e9c63d7bfa33735f5dad8230a26edd`）· 新断言 **60 通过 / 0 失败**。

| 项 | 改前 | 改后 |
|---|---|---|
| `DeadCandidates` 候选源 | `ctx.DeadUnits`（本局死亡流水，**零闸**） | `CemeteryDeadUnits`（墓地列表，**四道闸**） |
| 残骸（`isRemnant=true`，已进 `Discard`） | **会被当成「本局死过的部队」让玩家选** | **不在候选里** |
| 池内受影响卡 | —— | **5 张**：`EC39` / `GSC44` / `SAU39` / `SOR42` / `UM61` |
| `EffectResolver.cs` 行数 | 22,095（CRLF 22,095 / 裸 CR 0） | **22,112（+17）**（CRLF 22,112 / 裸 CR 0） |

---

## 一、原版语义（第一权威，逐句）

### 1.1 `ta=60` 那一支只有**一个**候选源

`decomp_full/AbilityLogic__GetTargets.c:949-960`（现读，`param_2 + 0x10 == 0x3c` 那一支）：

```c
if (*(int *)(param_2 + 0x10) == 0x3c) {
    uVar8 = BattleManager__GetCardsInCemetery(param_1,
                 *(undefined1 *)(param_2 + 0x17),   // includeMinions
                 *(undefined1 *)(param_2 + 0x18),   // includeSpells
                 *(undefined1 *)(param_2 + 0x19),   // includeWhispers
                 *(undefined1 *)(param_2 + 0x1a),   // includeWeapons
                 bVar16, bVar17);                   // 两侧（按 `isPlayerTurn` 翻转后的己方/敌方）
```

⇒ **没有第二条路**：`targetsAffected == 60` 的候选**就是**墓地列表。`choose … died …` 与
`Return … died this game` 是**同一个候选源**，我们过去把它们分成了两条路（一条已切、一条没切）。

### 1.2 墓地里那道**无条件**的残骸闸

`decomp_full/BattleManager__GetCardsInCemetery.c`（现读全文 60 行）：

- `:53-60` 的排除链：`cardType == 10`（Hero）**无条件**；`includeMinions/includeSpells/includeWhispers` 各管一档；
  然后 `*(char *)(lVar3 + 0x65) == '\0'` —— **`isRemnant(+0x65) != 0` 无条件排除**（与任何 include 无关）。
- `:63-66` 末段：还要 `*(char *)(lVar3 + 0x24b) != '\0'` —— **墓地放逐闩**（`shouldCheckIfIsInCemetery`），否则不收。
- `CemeteryManager.AddCardToCemetery` 进墓时**不清** `+0x65` ⇒「残骸被摧毁」进墓的那张**带着 `isRemnant=1`**。

### 1.3 为什么残骸**会**落进这条路的输入里（= 这个缺陷是可观察的）

- 残骸被摧毁 ⇒ 进 `Discard`（= 我们的墓地列表；`RuleCore.cs:9955` `AddToCemetery(ps, u.Instance)`）；
- 同一时刻 `DeadUnits` 里**也**有一条它的阵亡登记（`RuleCore.cs:9956`，`IsRemnant = true`；那一句在 `if (!u.Has("ephemeral")) AddToCemetery(...)` **之外**，无条件记）；
- ⇒ 改前那条「手扫 `ctx.DeadUnits`」**只看得到登记、看不到闸**，于是它进了候选；
- 原版那边它到不了 `GetCardsInCemetery` 的输出（`:60` 挡掉）。

---

## 二、改法

### 2.1 `DeadCandidates` 本体（`EffectResolver.cs:6446`，原地重写）

```csharp
static List<CardDef> DeadCandidates(BattleContext ctx, int owner, string scope, EffectOp op)
{
    var outp = new List<CardDef>();
    if (ctx == null || owner < 0 || owner >= ctx.Players.Length) return outp;
    int mark = ctx.Players[owner].LastTurnStartMark;
    var dead = CemeteryDeadUnits(ctx, owner, null, op);      // ← 换源：墓地那一口（含残骸/放逐/卡种/侧别）
    for (int i = 0; i < dead.Count; i++)
    {
        var d = dead[i];
        if (d == null || d.Card == null) continue;
        if (d.Owner != owner || !d.Card.IsUnit) continue;    // ← 墓地里也可能是战术卡 / 对方那一侧
        if (scope == "since_last_turn" && (d.DeathTurn < 0 || d.DeathTurn < mark)) continue;
        outp.Add(d.Card);
    }
    return outp;
}
```

- **签名只多了一个 `op`**（`ResolveCemeteryTargets` 要它读 `abilityTargetCriteria` 那一行），全仓**唯一调用点**在 `:4835`。
- **不复用 `ReturnFromCemetery` 那一整条**（它自带 `limit` / `Dest` / `TakeFromGraveyard` 的搬运语义），
  只复用**它已经在用的**候选适配器 `CemeteryDeadUnits`（`EffectResolver.cs:20551`）⇒ 规则仍只有一份。
- `IsUnit` 这一条是**照原版 include 格**收的余量：池内 5 张全是 `includeMinions = 1 / includeSpells = 0`
  （见 §四），而 `ResolveCemeteryTargets` 的兜底档（读不到 criteria 行时）会把卡种开成「全收」 ⇒ 这一道把它收回单位。

### 2.2 调用点（`EffectResolver.cs:4835`，同一物理行）

`source = DeadCandidates(ctx, owner, op.ChooseDeadScope);` → `source = DeadCandidates(ctx, owner, op.ChooseDeadScope, op);`

### 2.3 过期注释就地订正（铁律 5，`EffectResolver.cs:20134`）

原文：「`ChooseCardCandidates` 的 `ChooseSrc="dead"`（`DeadCandidates` 仍扫 `DeadUnits` ⇒ 残骸闸**没覆盖到这一支**，2026-11-04 `B141_W65` 记，未改）。」
→ 改为「✅ 2026-11-04 `B141_W77` 也已换到墓地那一口 —— 残骸/放逐闸都覆盖到；见 `DeadCandidates` 的 doc」。
（同一物理行替换，行数零变化。）

### 2.4 自检门（类尾，`EffectResolver.cs:22107`）

`public static List<CardDef> DeadCandidatesForTest(...)` —— 只给 Editor 程序集调（纪律同 `CemeteryDeadUnitsForTest`），
**不改任何行为**。

---

## 三、五张卡的影响（逐张）

池内 `abilityTargets[].ta` 含 `60` 且正文是 `choose … died …` 的 = **5 张**（`cards_engine.json` 现读，逐张核过 `desc` / `descZh`）：

| 卡 | 卡面 | `ChooseDeadScope` | `abilityTargetCriteria` 那一行 |
|---|---|---|---|
| `EC39` Eternal Servitude | `Choose a friendly troop that died since your last turn and deploy it. Lower its Health to 1` | `since_last_turn` | `includeMinions 1` · `includeSpells 0` · `includeWhispers 0` · `friendlyUnits 1` · `enemyUnits 0` |
| `GSC44` Subterranean Ambush | `Choose a friendly troop that died this battle and deploy it` | `all` | 同上 |
| `SAU39` Reconstitution Protocol | `Choose a friendly troop that died this game and put it in your hand` | `all` | 同上 |
| `SOR42` Rite of Restoration | `Choose a friendly troop that died since your last turn and deploy it` | `since_last_turn` | 同上 |
| `UM61` The Chapter's Due | `Choose a friendly Infantry that died this game and return it to your deck. Draw a card` | `all` | 同上（`keywordFilter 5` = Infantry 那一档，由 `FilterChoose` 消费） |

**行为差异（都在这 5 张上）：**

| 情形 | 改前 | 改后 | 判据 |
|---|---|---|---|
| **残骸被摧毁后**（`IsRemnant = true`，在 `Discard` + `DeadUnits` 都有） | **进候选**（玩家能选到） | **不进候选** | `BattleManager__GetCardsInCemetery.c:60` |
| 已在墓地**放逐**（`ExileFromCemetery`）的单位 | 进候选（若 `DeadUnits` 有登记） | **不进候选** | 同上 `:63`（`+0x24b`） |
| **手牌超上限**被弃掉的单位卡（在 `Discard`、`DeadUnits` 无记录 ⇒ `DeathTurn = -1`） | **不进候选**（根本不在 `DeadUnits`） | `all` 档 **进候选**（`EC39`/`SOR42` 是 `since_last_turn` 档 ⇒ 仍排除） | 原版 `PlayerHand._AddDrawnCardToHand_d__37.c:98` / `_AddCardNotDrawnToHand_d__39.c:64` 都走 `CardScript.SendToCemetery` ⇒ **原版墓地里就有它**，`GetCardsInCemetery` 会返回 |
| 敌方墓地里的单位 | ——（`DeadUnits` 按 `Owner` 挡掉了） | 仍挡住（`d.Owner != owner`） | 同上（`inclPlayer/inclEnemy` 那一对） |
| 墓地里的战术卡 | 进候选（如果它在 `DeadUnits` 里 —— 实际不会有） | **不进** | `includeSpells = 0` |
| **`ephemeral` 单位**阵亡 | 进候选（`DeadUnits` 有登记） | **不进**（它**不进墓地**：`RuleCore.cs:9955` 的 `if (!u.Has("ephemeral")) AddToCemetery(...)`，判据 `CardScript.GoToCemetery` / `HasCurrentTrait(ephemeral)` ⇒ 原版也不进墓地，`GetCardsInCemetery` 自然查不到） | `CardScript__GoToCemetery` 那一支 |

> ⚠️ **上表第 3 行是「换源带来的、可观察的」新增成员**，我按**原版语义**判「应当收」（原版墓地列表里本来就有手牌超上限弃掉的卡）。
> 这与 `W65` 报告 §四 ②「`this game` 无窗口档要不要收它们还没查清」的那一格对应 —— 本轮**查清了**（判据见上一列，两份原版异步体都写 `SendToCemetery`）。

---

## 四、验证（改前 / 改后，**都跑了**）

| 项 | 改前 | 改后 |
|---|---|---|
| `TMPDIR=/tmp/wf_b141_w77 bash 工具/typecheck.sh` | 运行时 0 / 编辑器 0 | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh check` | rc=0 · 哨兵失败 0 · 全池 1134 张 · 解析差异 0 行 | **rc=0 · 哨兵失败 0 · 解析差异 0 行** |
| `bash 工具/ruleprobe.sh play check` | rc=0 · 指纹 `a0e9c63d7bfa33735f5dad8230a26edd` | **rc=0 · 同一指纹（未变）** · ①-a / ①-b 逐卡差异 **0** |
| `play EC39` / `GSC44` / `SAU39` / `SOR42` / `UM61` 五张单卡读数 | —— | **逐行与改前一致**（夹具的弃牌堆是空的 ⇒ 这条路的输入本来就是空的；差异只在「墓地里有什么」时才现形） |
| 本件断言（离线跑法见下） | —— | **60 通过 / 0 失败** |
| `EffectResolver.cs` 行数 | 22,095（CRLF 22,095 / 裸 CR 0） | **22,112（+17）**（CRLF 22,112 / 裸 CR 0） |

### 4.1 断言（`RuleEngine/Editor/B141_W77_Assert.cs`，新建 · 60 条）

- **A 组（残骸闸，11 条）**：同一份夹具（`Discard` 与 `DeadUnits` 逐字相同、**只差 `IsRemnant` 这一位**）——
  `IsRemnant = true` ⇒ 候选 **0 张**、`GetCardsInCemetery` **0 张**；
  `IsRemnant = false` ⇒ 候选 **1 张**且是那张（按 `CardDef` 引用判）。
- **A 组★灭自证（4 条）**：本文件里复刻了一份**改前的形状**（`LegacyDeadCandidates` = 手扫 `ctx.DeadUnits`），
  在同一份**残骸**夹具上它给出 **1 张**、而且正是那张残骸
  ⇒ A 组的 0 **不是**「夹具里本来就空」，而是真的被闸挡掉了。再加上「它确实在 `Discard` 里 / 确实有阵亡登记 / 确实是单位卡」三条前提。
- **B 组（另外三道闸，6 条）**：墓地放逐（放逐前 1 → 放逐后 0）· 侧别（敌方墓地里那张对我不进、对它自己是 1）· 卡种（墓地里的战术卡不进）。
- **C 组（窗口档 / `DeathTurn = -1`，6 条）**：`all` 档两张都在、`since_last_turn` 只留 `DeathTurn >= mark` 那张；
  `DeathTurn = -1`（手牌超上限弃掉）在 `all` 档收、窗口档排除。
- **D 组（池内 5 张卡可达，5 张 × 7 条 = 33 条）**：逐张 —— 在池内 / 正文解析出 `choosecard` + `ChooseSrc = dead` /
  窗口档与卡面一致 / criteria 那三格（`includeMinions 1`·`includeSpells 0`·`friendlyUnits 1`）/
  **真卡跑一遍**（墓地里摆「残骸 + 普通单位」各一张 ⇒ 候选**恰好 1 张**且是普通那张）。

### 4.2 断言怎么离线跑通的（本代理⛔不跑 Unity）

把 `工具/ruleprobe` 拷到系统临时目录（`%TEMP%/wf_b141_w77_probe`，**跑完已删**）：csproj 加一条
`Assets/RuleEngine/Editor/B141_W77_Assert.cs`、`Stub.cs` 的 `Debug` 改成写 Console、`Program.Main` 加一个 `w77` 分支
⇒ `dotnet run -- w77` → **`=== W77 pass=60 fail=0 ===`**。
⚠️ 这只是**取证手段**，不是新工具；**仓库里没有留任何临时文件**（csproj / Program / Stub 一个都没改）。

---

## 五、行数 / 账漂移提示（给调度台）

`EffectResolver.cs` **净 +17 行**，其中：

- **`:6432-6453` 那段原地重写**（旧 22 行 → 新 32 行 ⇒ **+10**）⇒ **旧行号 ≥ 6454 的账落点全部 +10**
  （已核：`ResolveCemeteryTargets` 20136→**20146** · `CemeteryDeadUnits` 20541→**20551**）。
- **类尾新增自检门**（`:22104-22110` 附近 ⇒ **+7**）⇒ 它下面**没有**账落点（那就是类尾）。

⇒ 请在有权限时跑一次 `check_ledger_points --all-A --fix`（**⛔ 本代理不动账主表**）。

---

## 六、还没做 / 需要别人做

1. 🔴 **宿主接线**（本批白名单不含宿主）：`Assets/RuleEngine/Editor/RuleEngineTest.cs` 的 `Run()` 里**同一物理行追加**
   （落在该行第一个 `//` 之前）：
   `Guard("B141W77_Assert", B141_W77_Assert.Run); _pass += B141_W77_Assert.Pass; _fail += B141_W77_Assert.Fail;`
   ⚠️ **未接线之前本文件一条都不跑**（如实标）。
2. 🔴 **`RuleCore.cs:15932` 还有一句过期注释**（**不在我的白名单** —— 本件授权是「仅抽共用窄口必需时」，这句不是）：
   「⛔ 别在别处再造第二份 incl* 过滤；`choose … died …` 那一路（`DeadCandidates`）**尚未切**，见其上方注。」
   → 建议就地改成「✅ 2026-11-04 `B141_W77` 已切」。**同一物理行替换、行数零变化**。
3. 🟠 **账主表**：`报告/B141_W128_CHK05_336_77.md` §六 第 3 条那条「请立账」的 `DeadCandidates` 缺口 = **本件已做完**，
   请核销（⛔ 我没改主表）。
4. ❓ **`since_last_turn` 窗口档本身与原版不完全一致（本笔**未改**，只记录）**：
   原版 `BattleManager__GetCardsInCemetery.c` 全文**没有**「死亡回合」这一维（只有 include / 卡种 / 残骸 / 放逐四类闸），
   而 `AbilityLogic__GetTargets.c:949-960` 这一支也**不再筛**别的 ⇒ **原版这两张卡（`EC39` / `SOR42`）的候选其实是整座墓地**，
   「since your last turn」很可能只是**卡面文字**、数字版没有对应的运行期闸。
   我们这一档来自解析层（`EffectText.cs:6577` 的 `deadScope = "since_last_turn"`）+ `DeadCandidates` 的窗口判断。
   **要不要收掉这一档**需要单独裁（会改 `EC39`/`SOR42` 的候选面）—— 本轮**原样保留**，理由：本件的范围是「换源 + 残骸闸」，
   窗口档不在缺陷描述里，且没有反证能证明原版某一处真的按回合筛（`CardScript__HasDiedThisTurn` 是**条件**层
   `FilterMethods__MeetsConditionCriteria.c:260`，不是候选源那一支）。
5. ❓ **`choosecard` op 上读不到 `abilityTargetCriteria` 那一行**（离线实测：5 张卡**全部**落进
   `ResolveCemeteryTargets` 的「行读不到 ⇒ 按目标短语推」兜底，日志有 `ta=60` 的 `Debug.LogWarning`）。
   原因：`op.AbilityIndex / LogicIndex` 在这条正文路（`EffectText` 的 `choosecard`）上没有归到那行（`-1`）。
   **本件不影响正确性**（5 张的 include 格都是 `minions-only + friendly`，与兜底档 `all kinds + self` 在
   「只要单位 + 只我方」这两条上等价 —— 差额由新加的 `IsUnit` / `Owner` 两道筛补上），但**这是个待收敛的口径差**，
   建议另立一条（把 `choosecard` 的 op 也归到 abilities 行，或让 `ResolveCemeteryTargets` 在 `spec == null` 时
   从 `op.Source` 反查卡而不是只认 `ActingUnit/CreationSource`）。

---

## 七、出处（`文件:行号`）

- 判据：`d:/2/tools/decomp_full/AbilityLogic__GetTargets.c:949-960` · `BattleManager__GetCardsInCemetery.c:53-66` ·
  `PlayerHand._AddDrawnCardToHand_d__37__MoveNext.c:98` · `PlayerHand._AddCardNotDrawnToHand_d__39__MoveNext.c:64`
- 落点：`MyGame/Assets/RuleEngine/Core/EffectResolver.cs:4835` / `:6446` / `:20134` / `:20551` / `:22107`
- 闸：`MyGame/Assets/RuleEngine/Core/RuleCore.cs:15934`（`GetCardsInCemetery`）· `:15964`（`remnantIds`）
- 残骸灌值：`MyGame/Assets/RuleEngine/Core/RuleCore.cs:9955`（进墓）· `:9956`（`IsRemnant = true`）
- 断言：`MyGame/Assets/RuleEngine/Editor/B141_W77_Assert.cs`
