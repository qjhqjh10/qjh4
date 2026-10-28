# B89 第 1 波 · R5c —— `IsWaitingUseAbility` 转写 + U9 施工指令 + U10 判词

> **消费者**：调度台（转 E10d）· 后续教程/战斗界面写手。
> **本文件回答三件**：① 转写的谓词在哪、怎么来的、缺什么输入 ② U9（HUD 741）可照抄的施工指令 ③ U10 新账判词。
> **红线遵守**：⛔ 没跑 Unity/ruleprobe · 没动 git · 没改主表/正本 · 只改了 `RuleEngine/Core/TutorialScript.cs` 一个文件。

---

## §0 交付物 / 自检

| 项 | 内容 |
|---|---|
| **代码** | `d:/4/Unity/MyGame/Assets/RuleEngine/Core/TutorialScript.cs` —— 在 `public static class **TutorialRules**` **类尾**追加 2 个成员：<br>· `public static bool IsWaitingUseAbility(...)`（`:1981`）<br>· `static string TutChosenIdentity(...)`（`:2015`） |
| **typecheck** | **运行时 0 / 0 · 编辑器 0 / 0**（`TMPDIR=/tmp/wf_B89R5c3 bash d:/4/Unity/工具/typecheck.sh`）。⚠️ 前一版曾报 3 错，全在 `BattleContext.cs`（R1c 的文件、非我的），重跑时已由该线修掉。 |
| **行尾** | **LF**（改前改后都 `CRLF=0 / bareCR=0`）· 文件 `1945 → 2038` 行（+93，**全在类尾** ⇒ 下游账行落点零位移） |
| **补丁** | `d:/4/Unity/资料/完全复刻/账/_parts/B89_R5c.tsv`（11 列表头 · `轮次=B89_R5c` · **1 行 = U10 新账** · LF · 每行 11 列已校验） |

---

## §1 转写说明：`TutorialRules.IsWaitingUseAbility`

### 签名 / 落点
```csharp
public static bool IsWaitingUseAbility(TutorialTurn turn, int actionCounter, BattleContext ctx, string cardSelectedId)
```
落点 = `TutorialRules`（**静态类**，与兄弟 `IsWaitingChangeAttack`(`:1904`) 同处）**类尾**。`TutorialScript.cs` 这个文件里两个类：`TutorialScript`(实例态, `:596`) 与 `TutorialRules`(静态谓词, `:1489`) —— 本谓词属后者。

### 逐句来源（控制流以 Ghidra `.c` 为准）
- 🥇 **`d:/2/tools/decomp_full/AiScripted__CheckIfWaitingToUseAbility.c`** —— **本条控制流权威**。
- 🥈 `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AiScripted.cs:803-…`（可读，交叉核对）。
- ⚠️ **`d:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp/AiScripted.cs:451` 的 `flag1 = true` 收尾是反编译走样** —— ��会把「循环走完」写成恒 true。以 `.c` 的 `if (data.size <= iVar5) return 0;`（循环走完 ⇒ **false**）为准。`TutChosenIdentity` doc 里写了这条。

### 逻辑（= `.c` 逐句）
1. **前闸**：`turn`/`turn.actions` 非空 · `actionCounter ∈ [0, Count)` · 当前条非空 · `cur.playerAction` · `cur.data` 非空 ⇒ 否则 **false**。
2. 遍历 `cur.data[]` 只看 `type == ActiveAbility(40)`（`ScriptedActionType.ActiveAbility = 40`，与 `.c` 的 `+0x10 == 0x28` 吻合）。
3. 命中的那张（`GetChosenCard` 语义）：**== null 或 == 选中的卡** ⇒ **true**；否则看下一条。
4. 全部走完没命中 ⇒ **false**。

### 复用（⛔ 没另造 `GetChosenCard`）
`TutChosenIdentity` **复用本类已有的两条 `GetChosenCard` 等价物**：
- 在场/督军 → `FindBoardUnit(ctx, acting, out side, out slot, out why)`（`:1734`，`ourId` 精确匹配 + 认不出退回 `list[0]`，原版口径）；
- 手牌 → `FindHandIndex(ctx, acting, out why)`（`:~1783`，同口径）；
- 取命中那张的 `CardDef.Id` 作 identity（与 `Matches` 的 `ourId` 同口径）。
⚠️ **这是刻意的**：`FindBoardUnit`/`FindHandIndex` 是 `GetChosenCard` 的**唯一正本**（`BattleDriver.ScriptedChangeAttackMatches` 也在用）⇒ 不在这里再写第三份。

### 缺的输入（如实记，⛔ 没造假输入）
- 🔴 **`cardSelectedId` 是形参，由调用方传**：原版第 3 实参 `cardSelected` = `BattleManager.cardSelected`（当前选中的那张卡）。**我们引擎侧没有「选中的卡」这个状态**（选中态在表现层 `BattleDriver._selectedSlot`(`:2747`)）⇒ 本谓词把它当形参，由 `BattleDriver` 侧传 `Ctx.Players[_me].Board[_selectedSlot].Card.Id`。
- `waitingToResolveActiveAbilityFlag`（原版 `+0x418`）**不属于本谓词**，在调用方（`BattleDriver`）判 —— 见 §2 条件③。

### 🔶 自己设计（非原版）
identity 用 **`CardDef.Id` 字符串**判 `== cardSelected`，而原版 `GetChosenCard` 用 `CardScript` **引用相等**。理由：本仓无「选中卡引用态」，且 `Matches` 已用同一取舍。⚠️ 手牌里两张**同名同 Id** 的卡会被判成同一张（教程局不会出现；本仓既有闸门同此口径）。已在 `TutChosenIdentity` doc 里标 `🔶 自己设计`。

---

## §2 U9（HUD 741）—— 施工指令（给 E10d，可照抄）

> 调度台已裁：**不判 D、做**（原版那条 `LogWarning` 我们没发 = 真差异）。状态 **741 C → B**（请调度台在账上改；本代理不动主表）。

### ① 文案（原文，逐字）
```
"Avoid activeSkillDesc.FadeOut because we are waiting for that action in tutorial"
```

### ② 触发七条件（原版 → 我们字段）
原版：`decomp_full/BattleManager__Update.c` **`case 1`（normalBattle）** 段，LogWarning 在 **`:183-185`**；字面量在 `cpp2il/out_il2csharp/Assembly-CSharp/BattleManager.cs:25693-25697`（教程支）/ `:25707-25712`（战役支，同一句）；谓词 `AiScripted.cs:451`。

| # | 原版条件 | 我们仓的等价 | 备注 |
|---|---|---|---|
| ① | `case 1` = **normalBattle**（非选目标/非移动卡态） | `BattleDriver.Update`(`:14160`) 的**普通战斗选卡段** | —— |
| ② | `targetCard != cardSelected` | 当前**悬停**那张 != `_selectedSlot` 那张 | 悬停派发在 `TickBoardHover`(`:34236`) |
| ③ | `!waitingToResolveActiveAbility`（`+0x418`） | ⚠️ **本仓无同名 flag**（只有 `:14824` 一句注释提到）⇒ **E10d 认领**：候选 = 未处于「主动技选目标/结算」态（如 `_command != AttackKind.Ability`） | 找不到就**如实记「认领失败」**，⛔ 别硬编一个等价 |
| ④ | `IsPlayerTurn()` | `Ctx.Active == _me && !Ctx.IsOver` | 与 `UpdateHud` 同口径（`:14793`） |
| ⑤ | `IsTutorialStageAvailable() \|\| IsCampaignStageAvailable()` | `Ctx.Tutorial != null`（教程）· 战役支 `Ctx.CampaignStage`(`BattleContext.cs:922`) **无写点**（账 709）⇒ 今天恒空 | —— |
| ⑥ | `cardSelected != null` | `_selectedSlot >= 0` | —— |
| ⑦ | `CheckIfWaitingToUseAbility(this, GetCurrentTurnScriptedData(), cardSelected)` | `TutorialRules.IsWaitingUseAbility(tut.CurrentTurn, tut.ActionCounter, Ctx, selectedCardId)`<br>`tut = Ctx.Tutorial`，`selectedCardId = Ctx.Players[_me].Board[_selectedSlot].Card.Id` | 谓词本轮已就绪（`:1981`） |

⇒ **七条全真** ⇒ **跳过**主动技面板的 `Hide()/FadeOut` + `Debug.LogWarning(文案)`。

### ③ 落点
`CardPresentation/Battle/BattleDriver.cs` 的 `Update`(`:14160`) 普通战斗段里、**「因选择/悬停变化而要收起主动技描述面板（`skillPanel.Hide()` / `CancelAbilityTargeting()`(:14837)）那一处之前**加闸。
- **推荐照抄现有样板**：`TutorialFrameGuard()`(`:16732`) —— 它正是「每帧按脚本闸门收状态」的同类（当前在 `tut.FinishedScriptedActionsInTurn` / `_selectedSlot<0` 处早退；本条把它的早退条件换成上面七条、命中时**只跳过 FadeOut + 打日志**，不 `ClearSelection`）。
- ⚠️ 别把日志塞到 `skillPanel.Hide()` 本身（那是状态机切档 `:31674/:31713`，会每档都刷）—— 要落在**选卡/悬停那一帧的判定处**。

### ④ 判据出处（写进代码注释）
`BattleManager__Update.c:183-185` · `out_il2csharp/Assembly-CSharp/BattleManager.cs:25693-25697`/`:25707-25712` · 谓词 `AiScripted__CheckIfWaitingToUseAbility.c` / `AiScripted.cs:451` · 兄弟 `TutorialScript.cs:1904 IsWaitingChangeAttack` · 谓词本体 `TutorialScript.cs:1981 IsWaitingUseAbility`。

---

## §3 观测口（谓词自己不会跑）

`IsWaitingUseAbility` 是**纯函数**（无副作用、确定性，只读 `turn`/`ctx`），今天**全仓零调用**（消费者 = E10d 的 U9 日志支）。观测口 = 用下面 **5 条用例矩阵**（fixture `TutorialTurn` + `BattleContext`）：

| # | turn / ctx / cardSelectedId | 期望 |
|---|---|---|
| 1 | `turn == null`（或 `actions==null` / counter 越界） | **false** |
| 2 | 当前条 `playerAction==false` 或 `data==null` | **false** |
| 3 | 当前条 `data` 里**没有** `ActiveAbility(40)` | **false** |
| 4 | 有 `ActiveAbility`，其 `acting` 解析出的卡 `CardDef.Id == cardSelectedId` | **true** |
| 5 | 有 `ActiveAbility`，`acting` 解析出的卡 `!= cardSelectedId`（可解析） | **false**；若解析不出（空列表/督军缺失/坏 unitType）⇒ **true** |

⚠️ **不自动化**（`Editor/*Check.cs` 归 E6c，⛔ 我不碰）：等 E10d 接上消费者后，这 5 条随 `BattleScene.Run` 一起验即可。

---

## §4 U10（507②）—— 判词 + 新账（已写补丁）

**判词（调度台 2026-10-28 裁）**：原版 AI 出的那一刻是 **2D Hand 卡**，播完 `Card Hand To Board` 才换 3D（`MinionManager.cs:1186-1222` · `CardScript.cs:8387` · `BattleCardUI.cs:1435`）；我们 `SyncBoard` 直接建 Board ⇒ **结构性差异，需单独立项**。**状态 = C**，⛔ 别塞进账 507 那一行。

**已落补丁**：`账/_parts/B89_R5c.tsv`（域=`特效动画` · 原版类=`MinionManager` · 方法=`MoveMinionToConversionPoint(...)` · 状态=`C` · 轮次=`B89_R5c`）。判据全文见 `判据/B89_R5b_APK挖PressedBtn.md` §三。

---

## §5 没查清 / 注意

1. **①「`ActiveWarlordAbility` 是死代码还是运行时 `AddComponent`」—— 保持「没查清」，⛔ 不判 D**（调度台 2026-10-28 明确）。四级源（ILSpy/.c · `assets_full` · AssetRipper · 安卓 APK）都**无实例**，但「无实例 ≠ 原版没有」；全树未见 `AddComponent<ActiveWarlordAbility>`，`Start()` 只 `GetComponentInParent`。
2. **U9 条件③（`waitingToResolveActiveAbility`）本仓无同名 flag** —— 由 E10d 认领；认领不到就如实记，别硬编。
3. **U9 战役支**（`IsCampaignStageAvailable`）今天恒空（`Ctx.CampaignStage` 无写点，账 709）⇒ 施工只覆盖教程支；战役支等战役线。
4. **741 账状态 C→B 由调度台改**（本代理不动主表；只交报告 + `_parts` 补丁）。
