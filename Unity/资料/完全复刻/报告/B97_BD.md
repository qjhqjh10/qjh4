# B97_BD — `CardPresentation/Battle/BattleDriver.cs`（+表现层）9 条施工单

执行代理 `B97_BD`（第九十七会话 · 2026-10-29）· 施工单 → `判据/B97_战斗剩余_按文件施工单.md` **§1·5**（9 条）。

**一句话结论**：**落地 2 条**（`:255` ① 选牌上限改读卡数据 · `:292` 督军入场 `×warlordStartSizeFactor`），
**其余 7 条停手写反证** —— 它们的真正落点**不在本件白名单内**（`ActionQueue.cs` / `VfxMap.cs` / `ProfileData.cs` / `RuleCore.cs` / `ActionPump.cs`），
或**判据未定**（`:1081`）、或**必须真机验收**（`:1121`）。
`BattleDriver.cs` **行数 +55**（**全部追加在类尾**，既有 **715 处 `BattleDriver.cs:行` 落点零漂移**）· `typecheck` **0 / 0**。

---

## 一 九条逐条结论

| 账行 | 原版方法 | 结论 | 判据 / 证据 |
|---|---|---|---|
| `:240` | `CancelBattleActionFromQueue` | ⛔ **停手（跨文件 · 白名单外）** | 残 ② 的两半都不在 `BattleDriver.cs`：引擎侧 = `RuleEngine/Core/ActionQueue.cs:487` 的 `case 0x17`（`RuleEngine` 里没有 `GameObject`），表现层 = 订阅回调后 `SetActive(false)`。今天 `0x17` **无生产者** ⇒ 接上也零可观测。**给调度台的补丁**见 §三·1 |
| `:255` | `ChoiceOfCardPlayer` | ✅ **① 已落地**（② 仍缺，跨文件） | ① `BattleDriver.ChooseShowLimit`（`:10692`）改读 `CardDef.ChoicesMaxTargets`；手抄 21 张 `ChooseShowByCardId`（`:10663`）降级为「数据回 `-1` 时」的兜底快照。判据 `decomp_full/BattleManager__GetChoiceOptions.c:40-45`（`iVar1 = min(pool.Count, choicesDefinition+0x40)`；`maxTargets == 0` ⇒ 空表）。② 引擎随机流（与 `:254` 同一张施工单）= `EffectResolver.cs` / `BattleContext.cs` |
| `:292` | `DeviceVariables` | ✅ **已落地**（`warlordStartSizeFactor` 这条唯一的缺项） | 新增 `HeroLandScaleIn`（`:38109`）/ `HeroLandScaleOut`（`:38120`）/ `HeroLandYOffset`（`:38131`）（类尾）。判据 `CardScript._HeroLandIntoField_d__318__MoveNext.c:42/52/59`（state 0：`DefaultScale` → **当前 scale × `[VarsDevice+0x30]` 三分量 `mulss`** → `set_localScale`；`waitMult != 1` ⇒ `y -= 0.2`）与 `:342-400`（state 1：`y += 0.2`、`DOScale(GetUnitSizeInPlay, 0.1)` + `Ease.OutElastic`）。**`+0x30` 偏移独立坐实**：`VarsDevice__GetCardInHandYOffsetTouchInput.c` 自己写 `this+0x1c`/`this+0x20` = `cardInHandShownYOffset`/`…Min` ⇒ `+0x18`=`cardInHandShownScale`、指针 `+0x28` ⇒ **`+0x30` = `warlordStartSizeFactor`**（桌面 1.3 / 移动 1.5） |
| `:483` | `RemoveSelfBuffsFromQueues` | ⛔ **无需改（表现层那半已完整）** | `BattleDriver.RemoveSelfBuffsFromQueues:34846` → `CancelPendingAnimsOf`（只撤 `cueVfx` = `playAnim`）已对上 `CancelAnimFromUnitFromQueue.c:25-81`。残 = `shouldIgnoreDestroy:1` 的引擎语义（`RuleEngine/Core/ActionPump.cs:2084`，**红线段**） |
| `:804` | `MinionPlayedIntoField` | ⛔ **停手（数据未重生成 + 消费端在白名单外）** | 数据侧已就位（`工具/gen_card_vfx_by_card.py:2507-2518` 打可选键 `so`），但**盘上产物过期**：`Resources/card_vfx_by_card.json` 现读 `generatedAt 2026-10-24` / `cardCount 1098` / **`so` 键 0 条** ⇒ 消费端接上也零可观测。补丁见 §三·2 |
| `:1004` | `DamageDeltToEnemyUnit.OnDamageDelt` | ⛔ **停手（三处残差全在本件之外）** | ① 消费侧**不是** `AwardAchievements`（它只转发 `Ctx.Tally`）—— 真读点 = `CardPresentation/Shell/ProfileData.cs:666 tally.DamageOf(...)`，应改 `DamageRawOf`；② 归属写点在 `RuleCore.cs`（红线段）；③ `DamageKind` 打标在 6 个 `RuleCore` 调用点 |
| `:1037` | `EntityScript.GetThisCardPlayedDamage` | ⛔ **停手（判据缺一层 · 不猜）** | 算式那半**已由 `B96_CB` 接上**（`BattleBoardPreview` 的 `info.OtherIsTactic` 支 → `RuleCore.ThisCardPlayedDamageEntries`）。残 ①「`other` 可以是任意被打出的牌」缺的判据 = 一张**手牌里的单位卡**落在原版 `:53-81` 战斗伤害支时**怎么算**（`EnemyReturnsAttack` / `GetCombatDamageWithType` 对手牌态卡没有锚定）⇒ 铁律 3 不猜。残 ② 在引擎（`RuleCorePartial_B96CB.cs`） |
| `:1081` | `MatchData.SetMatchRecording` | ⛔ **停手（判据未定）** | 施工单 §二 明写「先出字段对账表再决定要不要改」；`+0xa8` 活动修饰依旧无对应物（P1 活动线 + 服务端） |
| `:1121` | `MinionManager.GetNextSlotPos` | ⛔ **停手（自相矛盾 ⇒ 写反证）** | 施工单同一行「还差什么」= **需要真机看动画锚点**，而引擎半 `RuleEngine/Core/BoardSlots.cs:702-772`（方法在 `:726`）自己的头注也写「⛔ 没看真机前别接线」；接线 = 推翻 `EnsureMinionSpotAnchor` 的自设计且**改动画锚点位置**（两族语义不同：`NextSlotPos` 是「右/左交替取第 N 个空格」）⇒ 本代理**无法验收** |

---

## 二 改了什么（逐行）

### 2·1 `CardPresentation/Battle/BattleDriver.cs` —— **行数 +55，全部在类尾；既有落点零漂移**

| 位置（现读） | 改动 | 行数 |
|---|---|---|
| `:10631-10638` | `ChooseShowDefault` 那段「仍未搬进管线的那一半」→ 就地改写为「**已搬进管线**」（保留更正痕 `~~…~~`） | **±0**（5→5） |
| `:10651-10661` | `ChooseShowByCardId` 的表头注释：**降级为兜底快照**（删掉「表现层镜像」那一行、加一行 🔴 判定） | **±0**（11→11） |
| `:10691-10697` | `ChooseShowLimit` 体内换写：**数据优先**（`v >= 0` 直接用，`0` 合法），`-1` 才查快照表 / 众数 | **±0**（7→7） |
| `:34748` | 段0 注释：「再 × `AlternateArtCard.CardImage` 的缩放档」→ **「再 × `DeviceVariables().warlordStartSizeFactor` = 1.3」**（那是**符号名撞车**，实参是 `manager`） | **±0** |
| `:34761-34762` | `🔶 ①` 「缩放不写 / 起止值本来就相等」→ **「已撤销」**（state 0 放大 1.3、state 1 补间回正 ⇒ 起止值不等） | **±0**（2→2） |
| `:34774` | `float fade = …` → 同一行加 `dflt = HeroLandScaleIn(v, waitMult)`（局部捕获，不借字段 —— 两尊督军可能同时入场） | **±0** |
| `:34783` | 落地那一拍：同一行加 `HeroLandScaleOut(v, dflt, waitMult);`（= 原版 state 1 的 `DOScale`） | **±0** |
| `:34786` | 那句 `Debug.Log` 尾巴「🔶 缩放/alpha 两半按我们的形状不写」→「**缩放已按 state 0/1 接**（×1.3 → 补间回正）；🔶 alpha 那一半按我们的形状不写」（不改 = 日志与新行为自相矛盾） | **±0** |
| `:38078-38132` | **类尾新增**：两段判据注释 + `HeroLandScaleIn` / `HeroLandScaleOut` / `const HeroLandYOffset = 0.2f` | **+55** |

### 2·1b ⚠️ **点名：动了一个【非同目录】文件（纯注释 · 行数 7→7）**

`CardPresentation/Core/VarsDevice.cs:38-44` —— 它那句「🔴 **缺的那一项 = `warlordStartSizeFactor`**：全仓 0 命中 ⇒ **整条没做**」
在被本件接上之后**变成假话**（铁律 5：两份说法打架比没有更糟）。就地改写为「✅ 已接 + 落地位置 + 顺序别写反（乘的是**当前 scale**）」，
**零行数变化**（`git diff --numstat` 现读 `7 7`，行尾仍是 LF：`CRLF 0 / 行 125 = HEAD 的 125`）。
⛔ 除这一处注释外**没有**碰任何非同目录文件。

**零漂移的证据**：`BattleDriver.cs` 现读 **38,133** 行（改前 38,078）· 我逐个回源核了 `EnsureMinionSpotAnchor:3929` ·
`ChooseShowByCardId:10663` · `ChooseShowLimit:10692` · `HeroLandInto:34771` · `RemoveSelfBuffsFromQueues:34846` —— **与改前逐一相同**；
账里 **715 处** `BattleDriver.cs:<行号>` 落点中**没有一处 ≥ 38000** ⇒ 追加块不推任何一条。
行尾：`CRLF 38,133 / LF 38,133 / 裸 CR 0`（`io.open(...,'rb')` 现数）。

### 2·2 新建 `CardPresentation/Editor/B97_BD_Assert.cs`

- 入口 `B97_BD_Assert.CheckB97BD(driver)`；`Pass` / `Fail` 属性供宿主累加（同 `B97_UI2_Assert` 的接法）。
- **建议插入点 = `CardPresentation/Editor/BattleScene.cs` 的 `driver.ApplyTutorialVisibilityForTest();`（现读 `:36520`）那一行【之后】、
  `driver.BeginTutorial(0);`（现读 `:36523`，「⑤ 督军两拍」那一段的第一句）之【前】** ——
  即整块 `{ B97_BD_Assert.CheckB97BD(driver); }` 单独插在 `:36521` 那道空行处。
  本件自己 `BeginTutorial(0)` 并由第二拍驱动 `HeroLandIntoField`，宿主紧接着又把同一条开场重置一遍（`BeginTutorial` 会 `_tutWarlordBeat=0/_tutBeatDone=false`，`BattleDriver.cs:4738`）⇒ 对它后面那三条「一藏一显」断言**逐字无害**。
  ⛔ 不传 `driver` 也行（会 `FindFirstObjectByType`），推荐传。
- 断言 13 条（A0–A5 · B0–B2 · C1–C13），每条都带「🧨 什么改动会让它红」（灭自证），要点：
  - **A1**：全池 `ChoicesMaxTargets >= 0` ⇒ 兜底表 / 众数 3 **运行期不可达**（这是本次改动「判据唯一」的**真正保证**）；
  - **A2/A2a/A2b**：`∈{2,4}` 正好 **21 / 12 / 9** 张（与手抄表同规模）；
  - **A5**：**没有一张**「正文带 `Choose ` 却 `maxTargets ≤ 0`」的卡 ⇒ 面板**不会**出现「摆 0 张」死局（实测 60 张 `Choose ` 卡全为 3/2/4）；
  - **C9**：入场那一刻 `BaseScale == 在位缩放 × 1.3`（删掉 `HeroLandScaleIn` ⇒ 红）；**C12**：落地后补间回正（不接 `HeroLandScaleOut` ⇒ 督军永久大 30% ⇒ 红）；
  - **C2**：移动那份是 **1.5**（防「把 1.3 写死在调用点」）；**C10/C13**：`waitMult == 1` 时 y **不动**。
- ⚠️ 本件会推时间线 **0.31 s** + 补间 **0.3 s**（都是表现泵），`animateFeel` / `CardTween.Mode` 用完还原。

### 2·3 账补丁 `资料/完全复刻/账/_parts/B97_BD.tsv`

11 列 + 首行表头、裸 `split("\t")` 可读、LF、9 行（每行 `方法` 列 = **完整签名带 `(...)`**）：
`:240` · `:255`（**状态留 B**，② 仍缺）· `:292`（**状态 B → A**：`warlordStartSizeFactor` 已接、其余 5 项属**本侧重构**不是与原版的差异）· `:483` · `:804` · `:1004` · `:1037` · `:1081` · `:1121`。
⛔ 未动主表（调度台合并）。

---

## 三 ⛔ 停手那 7 条 —— 给调度台的精确补丁

### 3·1 账 `:240`（`createHandCard(0x17)` 的表现层副作用）

`RuleEngine/Core/ActionQueue.cs`（**本件白名单外**，现读 `:487` 的 `case 0x17:`）加一个静态钩子：

```csharp
public static System.Action<UnitState> OnCancelCreateHandCard;   // 表现层订阅（RuleEngine 里没有 GameObject）
// case 0x17 支内、出声之前：
if (OnCancelCreateHandCard != null) OnCancelCreateHandCard(act.Target);
```
`BattleDriver` 侧（类尾）订阅并做：棋盘档 `TryLocateBoardView` 找 `_myUnits/_foeUnits` 里的视图 ⇒ `SetActive(false)`；
**手牌档需要一张 `UnitState → CardView` 的手牌映射，今天不存在**（手牌视图是按 `HandViewAt(i)` 取的，没有按实例反查的口）⇒ 那一半要先补映射。
⚠️ 今天 **`0x17` 无生产者**（全仓 `grep createHandCard` 只有判据注释）⇒ 接上也零可观测。

### 3·2 账 `:804`（`summonAnimDelayed` 两拍）

1. `CardPresentation/Core/VfxMap.cs`（**本件白名单外**）：`CardPick` 加 `public string so;`，再加一个只挑 `so == "summonAnimDelayed"` 的取数口（今天 `JsonUtility` 忽略未知键）。
2. `BattleDriver.FireDeployVfxInArenaSpace(VfxMap.Deploy, …)` 的两处调用（`PlaySignal` 的 Deploy 支 · `PlayAiDeployLandingVfx:25661`）**排除**那一条，改在 `LandOnGround` 之后**第二拍**播，闸 = `shouldTriggerSummon ∧ 非 ephemeral(5) ∧ 非 ambush(0x4b0)`（判据 `_MinionPlayedIntoField_d__314__MoveNext.c:33-257`）。
3. 🔴 **先重生成数据**：`Resources/card_vfx_by_card.json` 盘上产物 `2026-10-24` / 1,098 张 / `so` **0 条**（`工具/gen_card_vfx_by_card.py` 现算 1,100 张）⇒ 不重生成，消费端接上**零行为**。

### 3·3 账 `:1004`（伤害口径）

`CardPresentation/Shell/ProfileData.cs:666`（**本件白名单外**）`tally.DamageOf(me, DamageKindOf(f))` → `tally.DamageRawOf(...)`
（容器 `AchievementTally.AddDamageRaw:354` / `DamageRawOf:364` **早就在**，写点 `RuleCore.cs:8475` 也已接；⛔ 别与 `DamageOf` 同时读 = 双计）。
归属那一格（`dealerSeat = 1 - 受害者座位` vs 原版 `actingCard.isPlayer`）在 `RuleCore` ⇒ 玩家可见数值，必须连 `play check` 一起做。

### 3·4 其余 4 条

`:483` 引擎半在 `ActionPump.cs:2084`（红线段）· `:1037` ① 缺「手牌单位卡走原版战斗伤害支」的判据（⛔ 不猜）· `:1081` 判据未定 · `:1121` 等真机（`BoardSlots.NextSlotPos` 头注自己写着「没看真机前别接线」）。

---

## 四 验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_bd bash 工具/typecheck.sh` | **运行时 0 / 编辑器 0** |
| `bash 工具/ruleprobe.sh play all dump _tmp_view/B97_BD_dump.tsv` | **rc=0**；与 `_tmp_view/repin_1029/play_after_remfix.tsv` **逐字节相同**（216,354 B，`a == b`）。改完全部落地后再跑一次（`B97_BD_dump2.tsv`）—— 仍**逐字节相同** |
| `PinnedPlayMd5` | ⛔ **未动** |
| 行尾 / 行数 | `CRLF 38,133 = LF`，裸 CR 0；既有落点**零漂移**（见 §2·1） |
| 断言 | 未跑（子代理不跑 Unity）⇒ 由调度台接线后随宿主跑 |
| `python 工具/check_ledger_points.py`（只读，改动后） | 合计 **漂移 411 / 失败 25** —— 🔴 **这 411/25 一条都不是本件造成的**：本件所有就地改动**逐段 diff 都是 `c` 型（行数不变）**、唯一的追加块在 **≥38078** 而账里**没有一处** `BattleDriver.cs:>38000`。闸报出的 5 条 `BattleDriver.cs` 漂移（`:34664/:34704/:34770/:34818/:34883`）**全是同一个 +1 偏移**，而 **13:22 写成的施工单里就已经是 `:34771 HeroLandInto`**（HEAD 是 34770）⇒ 那 +1 在我开工**之前**就存在（本会话别的写手） |

⚠️ **`play all dump` 逐字节不变是「预期」而不是「有力证据」**：`工具/ruleprobe/` 是独立 dotnet 宿主、只链 `RuleEngine`，
**根本不含 `CardPresentation`**（本件改的全在表现层）⇒ 它**结构上看不见**这两条改动（同 `CLAUDE.md` §三 R11 那条「`play all dump` 看不见 AI」的形状）。
真正的验收 = 新断言 + 真机（督军入场那 0.3 s 的放大/回正）。

---

## 五 顺手发现（如实，⛔ 未改）

1. **`Editor/BattleScene.cs:29645` 的注释已半过期**：它写「判据链（`BattleDriver`）：`ChooseShowByCardId`（`{"GSC65", 4}`）→ `ChooseShowLimit`」—— 现在运行期**先读数据**、表只在 `-1` 兜底。该文件是**红线段（不改）**，留给调度台一并改。
2. **`VarsDevice.cs:41` 那条接法说明写的是 `localScale = defaultScale × factor`**，而 `.c` 现读是**「当前 scale × factor」**（`+0x324/+0x32c` 三个缓存分量）。今天两者相等（入场前 scale 就是 `DefaultScale`），但**顺序别写反** —— 我在 `HeroLandScaleIn` 的注释里写清了。
3. **`BattleDriver.cs:34748` 原来那句「× `AlternateArtCard.CardImage` 的缩放档」是符号名撞车**（`.c` 里 `AlternateArtCard__get_CardImage` 的实参是 `manager(+0x258)`、返回值被当 `VarsDevice` 用）⇒ 已就地订正，并与 `VarsDevice.cs` 的偏移推导互证。
4. **`B97_FXC` 在波 2 的「在飞」清单里，而它上一轮就在本文件类尾追加过东西**（`SwarmMerge` 那段，行 **38053-38076**）。本件的新块追加在**它之后**（38078-38132）；若它还要往类尾加，**必须排在本块之后**（否则顺序无所谓，只是别插到中间）。

## 六 未闭合

- 我要用的 `ChooseShowByCardId` 剩余价值极低（数据全池 `>= 0` ⇒ 运行期不可达）。**要不要把那张 21 行的表删掉**由调度台定：删掉会**减少 24 行** ⇒ 511 处落点整体漂移 -24（`check_ledger_points --fix` 可修，但要在无写手的窗口做）。本件按「优先零行数变化」**留表 + 降级为兜底**。
- `ShowAsk` 里「`showMax <= 0` 时摆 0 张」这一档**没有加防御分支**：原版 `GetChoiceOptions` 对这一档就是回空表（不猜），而我们的数据 A5 保证**走不到**；真走到的话面板会摆 0 张并在日志里说「随机抽出 0 张」（**不静默**）。若调度台希望改成「出声 + 不开面板」，那是**一处新增分支**（会 +N 行）。
