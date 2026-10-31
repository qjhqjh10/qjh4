# B112_R3B · `StartSpellFrom`（`B-134`）与 `nonTargetedSpellAnim`（`B-135`）

> 只读查判据代理 `B112_R3` 续单 · 2026-11-01 · 第一百一十一会话第 2 波。
> ⛔ 没跑 Unity、没动 git、没改任何 `.cs` / `.py` / 资产 / 账 / 活状态表 / 调度台日志 / 所有权表；**`d:/2` 一字节没改**。
> 解包探针全在 `d:/4/_tmp_view/b112_r3/`，一律 `-I`。产出 = 本文件（只此一件）。

---

## 〇、两条结论（先看这张表）

| 编号 | 一句话结论 |
|---|---|
| **`B-134`** `StartSpellFrom` | **「真缺」** —— 五步里 **③④ 有近似等价**、**⑤ 是真缺**（原版会替玩家点亮合法目标，我们一行 `return 0` 把它关掉了）；另外 ④ 的**配色链**也缺（我们传 `None`，原版传 `Spell` = **金色**，而我们的配色表**根本没有 Spell 档**） |
| **`B-135`** `nonTargetedSpellAnim` | **「结构性恒 null + 0 读者」** —— 三源普查：全树**只有声明**（`RawCardScript.cs:146`）、**没有任何方法读它**；数据 1,351 张**全 null**（与 `+0x130` 同型，四道证据见 §二·2）。我们代码里 **0 处引用** ⇒ 按铁律 11 例外①，**零改动** |

---

## 一、`B-134`：`StartSpellFrom` 到底做什么 + 我们有没有等价

### 1·1 原版方法体（三源，全文只有 5 步）

**源 A（主源，可读 C#）** `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:43834-43863`：

```text
public unsafe void StartSpellFrom(CardScript card, Vector2 pos)
{
    SetCardTracked(card);                                   // ①
    pointerCursorStart = <pos as Vector3>;                  // ②
    targetReticleController.ToggleCrosshair(option: true);   // ③
    targetReticleController.PrepareMovement(<pos>, AttackTypes.Spell);   // ④（内含 SetAttackType(3)）
    if (!card.isPlayer) return;                              // ⑤ 的闸
    foreach (CardScript target in unitsInPlay)               // ⑤
        if (card.targeted && IsValidSpellTarget(card, target))
            target.cardHighlight.ChangeState(CardHighlight.HighlightStates.potentialTargetInBoard);
}
```

**源 B** `d:/2/tools/decomp_full/BattleManager__StartSpellFrom.c:13-47`（逐句同上；`:15` 写 `+800`八字节 + `:16` 写 `+0x328=0` 合起来就是 `pointerCursorStart(+0x320) = Vector3(pos.x, pos.y, 0)` —— `dump.cs:30851` 该字段是 `Vector3`，0x320/0x324/0x328 = x/y/z）。
**源 C（调用点）** 全库**唯一**调用 = `CardScript__MoveIntoFieldFromHand.c:75`（`cardType ∈ {20 Tactic, 40 Whispers}` 且 `targeted`、且 `card.isPlayer`）⇒ **只服务「玩家自己的目标型战术/低语卡」**，不涉及其它卡种。

**入参 `pos` 是什么**：调用点 `:74` `GetHeroPos(bm, card.isPlayer)`；`BattleManager__GetHeroPos.c:10-16` = **`Get2DPosOfCard(PlayerManager.heroCard, useCenter: 1)`** ⇒ **督军卡的 2D 位置**（不是我点的那个位置、也不是卡自己的位置）。

### 1·2 逐步对照（`编号 | 查的是什么 | 判据结论（值/行为 + 文件:行号 + 哪一源）| 我们的落点 / 真缺 | 还差什么`）

| 编号 | 查的是什么 | 判据结论 | 我们 | 还差什么 |
|---|---|---|---|---|
| **134-①** | `SetCardTracked(card)` | `bm.cardTracked(+0x330) = card`；**旧值≠新值 且 旧 `cardState(+0x228)==8(inHandShowing)` ⇒ `StopShowingCardInHand(true)`**；`card.isPlayer==0 ⇒ CustomDebug.LogError`（源 B `BattleManager__SetCardTracked.c:16-40`；字段 `dump.cs:30852`） | **已有等价（近似）**：无持久字段，改成**每帧派生** `CardHighlight.TouchInput.TrackedCard = HandTacticUnderPointer(WorldPointer()) ?? (已选中的棋盘单位)`（`BattleDriver.cs:34513`；字段 `Core/CardHighlight.cs:412`；消费 `:706/:771`） | 原版那两条**副作用**我们没有对应实现（`StopShowingCardInHand` 的 8 态在我们这侧由手牌层另一套做；`isPlayer==0` 的报错我们这条只有一个来源 ⇒ 不可能触发）。**行为面无差异**，但「未逐句」⇒ 如实记 |
| **134-②** | `pointerCursorStart(+0x320) = Vector3(pos.x,pos.y,0)` | 源 B `.c:15-16`；读者 = `IsValidSpellTarget.c:62`（作为 Vector3 喂玩家卡自己的目标判定，vtable `+0x318`）与 `Update.c:1151`（tooltip 定位） | **已有等价（值等价）**：无持久字段，`UpdateSpellDragReticle` **每帧**从督军视图重算起点（`BattleDriver.cs:36243` `Vector3 from = hero.transform.position`） | ⚠️ **空间口径未核**：原版起点是 `Get2DPosOfCard(hero, useCenter:1)`（要过棋盘→屏→HUD 那一跳），我们直接取 `hero.transform.position`（棋盘世界）。`use3DBoard` 为真时两者**不是同一个点**（`BattleDriver.cs:37220-37234`）。⇒ 若起点确实错位，那是**既有缺陷**，需单独核 |
| **134-③** | `reticle.ToggleCrosshair(true)` | `TargetReticleController__ToggleCrosshair.c:18`（`param_2=1` = 开；`param_3=0` = 走淡入，不瞬开） | **已有等价**：`reticle.Show(...)`（`BattleDriver.cs:36248`，`Show` 里 `_fadeDir=+1` 走淡入） | 时机差（原版在「牌打出去那一刻」，我们在**拖拽中**）；如实记，不改 |
| **134-④** | `reticle.PrepareMovement(pos, AttackTypes.Spell)` ⇒ 内含 **`SetAttackType(3)`** | 源 B `TargetReticleController__PrepareMovement.c:15-38`（存起点 → `set_position` → `CrosshairLineEffect.SetPoints` → **`SetAttackType(param_3)`**）。`SetAttackType` → `CrosshairLineEffect.SetAttackType(3)`：**3 与 4 同走 `+0x30` 曲线**（`CrosshairLineEffect__SetAttackType.c:11-22`），颜色取预置表里 `attackType==3` 那条（我们自己的记录：3 与 4 **逐值相同、都是金** → `TargetReticle.cs:220-225` 的 `Preset Gold` + `MonoBehaviour_5098.json` 实读） | 🔴 **真缺（配色那半）**：我们传的是 `AttackKind.None`（`BattleDriver.cs:36248` 与 `:36251`）⇒ 进 `TryPresetOf` 的 `default`（`TargetReticle.cs:242`）⇒ **一个颜色都不写 = 白色**；而原版是**金色**。更根本：`TryPresetOf`（`TargetReticle.cs:235-243`）**只有 Melee / Ranged / Ability 三档，没有 `case AttackKind.Spell`**（`AttackKind.Spell = 3` 在 `Attack/AttackSelector.cs:99` 确实存在） | 曲线那半是对的：原版 3 走 `+0x30` = 我们 `UpdateArc` 的 `kind == Ranged ? Range : Melee`（`TargetReticle.cs:707`）⇒ **Spell 落 Melee profile，与原版一致**，不用改 |
| **134-⑤** | 玩家侧**点亮所有合法目标** | 源 A `:43848-43862` / 源 B `.c:30-45`：`if (card.isPlayer) foreach (u in unitsInPlay) if (card.targeted && IsValidSpellTarget(card,u)) u.CardHighlight.ChangeState(potentialTargetInBoard)`。⚠️ 这段是 `ShowPotentialTargets(mode=1)` 的**内联副本**（`BattleManager__ShowPotentialTargets.c:34-42` 同构），`ShowPotentialTargets(mode 1)` 的**唯一**调用点是 `_ChooseUnitTarget_d__780__MoveNext.c:62`（**单位卡**那条）⇒ **战术卡的点亮只走 `StartSpellFrom` 这条内联路** | 🔴 **真缺**：`HighlightPlayTargets` 第 3 句就是 `if (!def.IsUnit) return 0;`（`BattleDriver.cs:8244`），直接把战术卡关掉；它上面那段注释（`:8240-8243`）引的是 `Update.c:626-793`「通篇没有点亮动作」——**那句话只对 `Update` 成立，漏了 `StartSpellFrom`** | `RuleCore.IsLegalPlayTarget`（`RuleEngine/Core/EffectResolver.cs:2235-2242`）就是原版 `IsValidSpellTarget` 的等价（`owner != ctx.Active` ↔ `spell.isPlayer == IsPlayerTurn`；`Targeted != 0` ↔ `get_targeted`；池子 ↔ vtable `+0x318`）⇒ **谓词已经现成，只差把闸去掉** |

### 1·3 总判定

**`B-134` = 「真缺」**（不是「已有等价」也不是「判据为空」）：
- **⑤ 点亮**：真缺，一行闸关掉的。
- **④ 配色**：真缺两处（传 `None` + 配色表缺 `Spell` 档）。
- **①②③**：已有近似等价（①② 无持久字段是设计选择，③ 时机不同但效果等价）。

### 1·4 顺手发现（两条，都会误导下一个会话）

1. 🟡 **`BattleDriver.cs:36245` 那条注释的判据不成立**：「原版这一档 `MoveCrosshair` 的第 4 个实参就是 0 ⇒ 不换色、不 punch」——
   `MoveCrosshair` 的签名是 **`MoveCrosshair(Vector3 originPositionBoardSpace, Vector3 targetPositionUISpace)`，只有两个形参**
   （ILSpy `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TargetReticleController.cs:197`；`.c` 里那个尾随的 `0` 是 `MethodInfo`）。
   它的体只有 `set_position` + `UpdateTrail`，**压根不碰攻击方式**。
   ⇒ **准星的打法色整场由 `PrepareMovement` 定**，不会被后续 `MoveCrosshair` 刷掉 ⇒ 我们那条「所以传 None 也等价」的推论是**基于误读**的。
2. 🟡 「战术卡不点亮」这句话被复制到 **4 处**：`BattleDriver.cs:8050`（`NotifyUnitTargetPickPending` 一带的 doc）、`:8179`、`:8198`（**这是玩家会看到的 `Ctx.Log` 文案**）、`:8243`。
   改的时候**四处一起改**（铁律 5：同一句错话被复制到别处要一起改）。

---

## 二、`B-135`：`nonTargetedSpellAnim(+0x138)` 有没有读者

### 2·1 三源读者普查（口径 + 覆盖度一起给，免得「0 命中」被当成假阴性）

| 源 | 覆盖度 | `nonTargetedSpellAnim` | 对照：`targetedSpellAnim` |
|---|---|---|---|
| **A** `D:/2/tools/cpp2il/out_cpp2il_dev_ilspy`（主力，真方法体） | 2,016 个文件；带 `NotImplementedException` 的**只有 15 个**文件，**`BattleManager.cs` 一个都没有** | **全树 1 处 = 声明** `RawCardScript.cs:146` | 2 个文件：声明 `RawCardScript.cs:144` + `BattleManager.cs`（`:14315` 字段 / `:25309` 内联 Destroy / `:44860` Start / `:44874` 读 / `:44905` Instantiate / `:44911` Stop） |
| **B** `D:/2/tools/cpp2il/out_il2csharp/Assembly-CSharp`（逐方法可读，有的比 ILSpy 干净） | 2,013 个文件；227 个文件带 `NotImplementedException`，其中**含 card/battle 字样的只有 2 个**（`EntityObjectsManager.cs`、`WildcardIconDrawer.cs`，都是整方法级的小缺口，不是 `BattleManager`/`CardScript`/`RawCardScript`） | 同样 **1 处 = 声明** | 同样 2 个文件（`BattleManager.cs` + `RawCardScript.cs`） |
| **C** `d:/2/tools/decomp_full`（C 伪码，26k 方法） | 95 个文件出现过 `+0x138`，逐类核过：**全是别的类**（状态机自读 `param_1+0x138`、Photon、CardDisplayWindow、静态字段…） | **0 处** | **唯一读者** = `BattleManager__StartTargetedSpellAnim.c:30/36`（反证：这套搜法**能命中**同族字段） |
| （不用）`D:/2/Warpforge_code/Scripts/Assembly-CSharp` | 空方法体桩 | —— | —— （⛔ 按简报：不作判据） |

**额外的定向搜（针对 C 源的偏移歧义）**：按「RawCardScript 链」搜 `0x28) + 0x138` = **0 个文件**（若真有读者，形状就是这一种：`*(...+0x28)` 取到 `rawCardData` 再 `+0x138`）。

⇒ **`nonTargetedSpellAnim` = 有声明、0 读者**。我们工程侧也 **0 处引用**（`grep -rn nonTargetedSpellAnim --include=*.cs` = 0）。

### 2·2 「结构性恒 null」四道证据（与 `+0x130` 同型）

1. **控制列**（证明不是整张卡都读成默认）：`whileInPlayAnim == 1` **41 / 1,351** · `goSecondCardInHand` 非空 **64** · `startingCardInHand` 非空 **3** · `relatedCard1` 非空 **212** · `cardImageReference` 非空 **1,328** · `initiative ∈ {20:1349, 30:2}`。
2. **邻接对齐**（`dump.cs:23286-23293` 的原生偏移序）：`initiative(+0x12C)` → **`targetedSpellAnim(+0x130)`** → **`nonTargetedSpellAnim(+0x138)`** → `secretIdentity(+0x140)` → `startingCardInHand(+0x148)` → `goSecondCardInHand(+0x150)`。**有值的邻居在两侧都出现**（`initiative` / `startingCardInHand` / `goSecondCardInHand`）⇒ 这 24 字节没有错位。
3. **bundle 结构**：allcards 包 = `MonoBehaviour 1359` + `AssetBundle 1`，**没有任何 GameObject**；内层只有 **1 个 external**（`CAB-17b1b5f8…`，`m_Dependencies=['cab-17b1b5f8…']`）；`PPtr<GameObject>` 若为非空，必然 `m_FileID≠0` 或指向本文件里不存在的对象 —— **实测 1,351 张的 `m_FileID` 全 0、`m_PathID` 全 0**。
4. **老快照交叉验证**（`d:/2/远端缓存_旧profile_1013/导出/cards_1195_2022时代.json`，1,195 张，另一路导出）：`{m_FileID:0, m_PathID:0}` **1,192 张** + **3 张连键都没有**；非空 **0**。（`targetedSpellAnim` 同一条读数完全一致。）

### 2·3 逐条表

| 编号 | 查的是什么 | 判据结论 | 我们的落点 | 还差什么 |
|---|---|---|---|---|
| **135-①** | 有没有读者 | **0 读者**（三源；A 源 2,016 文件只 15 个有缺口且都与卡无关） | 我们 0 引用 | 残余风险**低但非 0**：B 源有 227 个方法体缺失（其中与卡相关 2 个文件都不是 Battle/CardScript/RawCardScript）⇒ 将来若补全那些方法体，理论上要复跑一次 |
| **135-②** | 数据真值 | **0 / 1,351**（池内 1,129 张同样 0） | `cards_engine.json` 没有这一列，也不需要 | —— |
| **135-③** | 结构上能不能有值 | **不能**（包内 0 个 GameObject；唯一的 external 也没被任何卡用过） | —— | —— |
| **135-④** | 判定 | **死字段（结构性恒 null）** ⇒ **铁律 11 例外①**（原版本身就没有） | **零改动** | 若要记账：把「三源 0 读者 + 数据全 null」写进账的「判据要点」列 |

---

## 三、如果现在就要动手，施工单是什么

> 两件都在 **`BattleDriver.cs`**（非 `partial`，现读 **40,229 行 / 纯 CRLF / 裸 CR=0** ⇒ **必须唯一写手整文件写**，落地后由调度台在「无其它 `.cs` 写手」时跑一次 `check_ledger_points --all-A --fix`），
> 外加 `TargetReticle.cs` **一处 3 行**。**`B-135` 零改动**。

### 3·1 施工单 A：把「战术卡点亮合法目标」接上（`B-134` ⑤）

**改哪里**：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:8244`
**怎么改**：
1. **删掉** `if (!def.IsUnit) return 0;` 这一行（连同它上面 `:8240-8243` 那段「非单位卡一个都不点亮」的注释整段换成下面的新判据）。
2. 函数其余部分**一行都别动** —— 它已经是通用形状（遍历双方棋盘 → `RuleCore.IsLegalPlayTarget(Ctx, _me, def, u)` 过滤 → `v.SetHighlight(CardHighlightState.ValidTarget)` → 记进 `_awaitPlayTargetLit`），对两种卡都成立。
3. 新注释照抄这段（判据+出处，别写「我们挑的」）：

```text
// 判据 = `BattleManager__StartSpellFrom.c:30-45`（= `ShowPotentialTargets(mode=1)` 的内联副本）：
//   `card.targeted && IsValidSpellTarget(card, u)` ⇒ `u.CardHighlight.ChangeState(potentialTargetInBoard)`。
//   ⚠️ 2026-11-01 就地订正（铁律 5）：原来这里写「战术/防御卡那一支 Update.c:626-793 通篇没有点亮动作
//   ⇒ 非单位卡一个都不点亮」—— **那句只对 `Update` 成立**；战术卡的点亮走的是 **`MoveIntoFieldFromHand.c:75`
//   → `StartSpellFrom` 这条内联路**（`ShowPotentialTargets(mode=1)` 的唯一调用点是单位卡那条
//   `_ChooseUnitTarget…:62`，战术卡不经它）。⇒ 两类卡都点亮，谓词同一条。
```

4. **同刻改 4 处错注释**（同一句错话被复制了 4 次）：`:8050`、`:8179`、`:8198`（**这处是玩家可见的 `Ctx.Log` 文案**，要改成「原版这一支**会**点亮合法目标」）、`:8243`。

**验收（断言写在哪 + 怎么写）**：宿主 = `CardPresentation/Editor/BattleScene.cs`（追加在既有断言段，⛔ 别往别人那行行尾 `//` 之后追加）。
现成 API：`driver.PlayTargetPoolForTest(def)`（`BattleDriver.cs:8403`）、`driver.SimulatePlayTargetPickForTest(owner, slot)`（`:8420`）、`driver.ViewAt(owner, slot)`（`:25129`）。
- **①** 夹具打一张 `Targeted == 1 && cardType == 20` 的战术卡 ⇒ 进 `AwaitingPlayTarget` 后，**目标池里每一格都必须是 `CardHighlightState.ValidTarget`**，且「点亮的格数 == 池内格子数」。
- **②（🧨 判别式）** 把 `if (!def.IsUnit) return 0;` 加回去 ⇒ ① 必须红（挡「两边一起改回去也绿」）。
- **③** 取消（`SimulatePlayTargetPickForTest(-1, -1)`）之后，所有卡回 `Normal`（走既有 `ClearPlayTargetHighlight`，顺带证明账没漏）。

**风险（照实写进补丁说明）**：这一改会让池内 **196 张** `targeted` 战术卡**第一次**出现绿环 —— 那是**原版行为**，但用户会第一次看到它 ⇒ 交付时要在汇报里点一句。

### 3·2 施工单 B：把法术准星的配色接上（`B-134` ④）

**改哪里（两处，共 3 行）**：
1. `d:/4/Unity/MyGame/Assets/CardPresentation/Battle/TargetReticle.cs:239-242` —— 在 `case AttackKind.Ability: p = Gold; return true;` **之前**加一行：
   ```csharp
   case AttackKind.Spell:  p = Gold;   return true;   // 原版 attackType 3 与 4 预置逐值相同（金）
   ```
2. `BattleDriver.cs:36248` 与 `:36251` —— 两处 `AttackKind.None` 改成 `AttackKind.Spell`（`Show` 与 `Aim` **必须一起改**：`Aim` 里 `if (kind != _lastPunchKind) ApplyAttackType(kind)`，只改一处的话第一帧后会被刷回无色）。
3. 顺手把 `:36245-36248` 那条注释删掉/改写 —— 它引的「`MoveCrosshair` 第 4 个实参是 0」**不成立**（见 §1·4）。

**为什么弧线形状不用改**：原版 `CrosshairLineEffect.SetAttackType` 对 `3/4` 走 `+0x30` 曲线（`CrosshairLineEffect__SetAttackType.c:11-22`），而我们 `UpdateArc` 的 `kind == Ranged ? Range : Melee`（`TargetReticle.cs:707`）把 Spell 落进 **Melee profile** —— **正好一致**。

**验收**：断言断 `BattleDriver` 暴露的准星读数（`reticle` 的 `CrossColor`/`PunchCount` 或既有 `ReticleVisible` 那一族）—— 进法术选目标态后 **`CrossColor` == 金色预设**（`(0.9137256, 0.7764707, 0.2039216)`）；🧨 判别式：把 kind 改回 `None` ⇒ 本条红。

### 3·3 施工单 C：`B-135` —— **零代码改动**

只做一件事：把下面这三句写进账（`判据要点` / `差异` 列）——
「`RawCardScript.nonTargetedSpellAnim(+0x138)`：**三源 0 读者**（`out_cpp2il_dev_ilspy` 全树 1 处 = 声明；`out_il2csharp` 同；`decomp_full` 按偏移与按 `rawCardData` 链两种搜法均 0 命中）；**数据 1,351 张全 null**（控制列/邻接/bundle 结构/2022 快照四道证据）⇒ **铁律 11 例外①，不建**。」

---

## 四、还没查清的（⛔ 一个都没有拿猜测填）

1. **`134-②` 的空间口径**：原版起点 = `Get2DPosOfCard(hero, useCenter:1)`（棋盘 → 屏 → HUD 世界），我们 = `hero.transform.position`（棋盘世界）。`use3DBoard` 为真时两者不同 —— **是否错位没核**（要核得看 `UpdateArc` 的 `transform.TransformPoint` 语义与准星所在 canvas 的帧，属于准星线自己的账）。
2. **`134-①` 的两条副作用**（`StopShowingCardInHand(true)` / `isPlayer==0` 报错）在我们这侧有没有等价，**没逐句核**（行为面看不出差别，但没证据就不写「已实现」）。
3. **`IsValidSpellTarget` 的 vtable `+0x318`** 那一跳（玩家卡自己的目标判定）我只核到「委托给 `rawCardData` 上的虚方法、并且把 `pointerCursorStart` 一起传进去」，**没有展开它的实现**；我们 `PlayTargetPool` 是否逐条对齐它，**未核**（单元卡那条路已经用它、且在跑，风险低）。
