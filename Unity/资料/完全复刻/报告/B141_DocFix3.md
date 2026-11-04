# B141_DocFix3 — 三处过期注释订正（执行写手 `B141-W72`）

- 批次 / 会话：`B141` / 第一百二十八会话（W128）
- 日期：2026-11-04
- 白名单：`CardPresentation/Editor/B121_WC_Assert.cs` · `RuleEngine/Core/B97W5_RowsAndDedupe.cs` ·
  `CardPresentation/Shell/WindowButton.cs` · 本文件
- ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表 / 活状态表 / 所有权表。

---

## 〇、一句话结论

三处（实为 4 行注释 / 4 个物理位置）**全部只改注释、零行为变化、行数零变化、行尾未翻**；
`typecheck` 运行时 0 错 / 编辑器 0 错。其中 `B121_WC_Assert.cs` 按简报的两个坐标（`:131-132` 与
「`ErrorTextKey` 同键」判据）各订正了一条**都确实过期**的注释（详见 §一 的坐标说明）。

---

## 一、`CardPresentation/Editor/B121_WC_Assert.cs`（2 处，均 1↔1 行）

### 1. 文件头「灭自证」块（现 `:13`）

**改前**

```csharp
//  🧨 A3「`InvalidCard` 的人话 ≠ `EffectOnlyCard` 的人话」：把新档并回旧档 ⇒ 两句话变成同一句 ⇒ 红。
```

**改后**

```csharp
//  🧨 A5「`ErrorTextKey(InvalidCard) == ErrorTextKey(EffectOnlyCard)`（两档**同键** = `MenuDeck/Error/InvalidCard`）」：把 `EffectOnlyCard` 改回 `InvalidDeck` 兜底 ⇒ 两边键不同 ⇒ 红。
```

**为什么过期（现读，不是照抄 W38）**：当前 `A5`（`:96-98`）断的是两档**同键**；
`DeckRules.Describe` 的 `EffectOnlyCard` 分支（`DeckRules.cs:596`）返回 `TermPrefix + "InvalidCard"`，
且 `MenuDeck/Error/EffectOnlyCard` 在官方 13 语言表里 0 命中（W38 已核）。
⇒ 旧注释「人话 ≠」与实际实现**正好相反**，且把断言编号写成了 `A3`（A3 现在是词条表查得到）。

### 2. `C_ValidateGate()` 夹具注释（现 `:136-137`）

**改前**

```csharp
// 2026-11-03（B124-W1 · δ）：原来只放 1 张 ⇒ `DeckRules.Validate` 在 ④ 张数档（`DeckRules.cs:486-488`）就回 `TooFewCards`，走不到 ⑥b（`:502` 的 `IsNotDeckable`）。
//   补满到合法张数（经典 30）：⑥b 在 ⑦ 同名上限**之前**返回 ⇒ 30 张同名不会先撞 `CopyLimitExceeded`；要测的那张 `notDeckable` 卡仍在。
```

**改后**

```csharp
// 2026-11-03（B124-W1 · δ）：原来只放 1 张 ⇒ **旧闸序**里 ④ 张数档先回 `TooFewCards`，走不到 ⑥b（`IsNotDeckable`）。
//   🔴 2026-11-04 就地订正（B141 · W38 现核）：④ 张数闸已挪到 ⑧ 之后（`DeckRules.cs:524-526`）；⑥b 仍在 ⑦ 同名上限之前 ⇒ 30 张同名不会先撞 `CopyLimitExceeded`，要测的那张 `notDeckable` 卡仍在。
```

**为什么过期（现读 `DeckRules.cs:452-528`）**：W31 把 ④ 张数闸挪到 `:524-526`（⑧ 之后）；
⑥b `IsNotDeckable` 在 `:495-499`、⑦ 同名上限在 `:501-517`。⇒ 「④ 在 ⑥b 之前 ⇒ `TooFewCards`」是**旧闸序**，
旧行号 `:486-488` / `:502` 也已不对；代码行为（30 张同名在 ⑥b 先返回）**仍成立**。

**坐标说明（如实）**：简报写的 `:131-132` 来自 W38 报告 §四（`B141_W129_FollowAsserts.md:135`），
指的是本条（C 段夹具注释）；因 `D` 段 W30b 新增约 6 行，现位置是 `:136-137`。
简报括号里的判据（`ErrorTextKey(InvalidCard) == ErrorTextKey(EffectOnlyCard)` 同键）指向的是 §一·1 那条
（文件头 `:13`）—— 两处**都确实过期**，故一并订正，均在同一白名单文件内、只改注释。

---

## 二、`RuleEngine/Core/B97W5_RowsAndDedupe.cs`（2 处，均 1↔1 行 / 2↔2 行）

照 `报告/B141_W128_BL37Discard.md` §六·5 的可粘贴补丁订正。

### 1. 文件头「📊 可达性」（`:24-25`，2 行 → 2 行）

**改前**

```csharp
//   · `chosenAbility 160`：池内 **0 张**（`cards_truth.json` 1,351 张里唯一那一行落在 BL37 的
//     **重复 SO** `f3df0cfc…`，池内那张记录是 30）· `165` = **0 张**；
```

**改后**

```csharp
//   · `chosenAbility 160`：池内 **0 张**（`cards_truth.json` 1,351 张里唯一那一行在**教程起手占位卡**
//     `T1 BlackLegion-Abaddons Chosen_startinghand`（`uniqueId f3df0cfc…`；`trigger 30 / ctx 10`：先 `ta=20 self` 弃自己、再 `ca 100` 造真 BL37）—— **不是** BL37 的副本；池内那张记录是 30）· `165` = **0 张**（判据 → `报告/B141_W128_BL37Discard.md`）；
```

### 2. `RunDiscardFromHandRow` 的 `<para>`（`:78`，1 行 → 1 行）

**改前**

```csharp
/// <para>📊 池内 `chosenAbility == 160` = **0 张**（1,351 张里唯一那一行在 BL37 的重复 SO）⇒ 休眠件。</para>
```

**改后**

```csharp
/// <para>📊 池内 `chosenAbility == 160` = **0 张**（1,351 张里唯一那一行在**教程起手占位卡** `T1 BlackLegion-Abaddons Chosen_startinghand`，**不是** BL37 的副本；判据 → `报告/B141_W128_BL37Discard.md`）⇒ 休眠件。</para>
```

**为什么（判据在 W58 报告 §一·4/§一·5，本次只按补丁转写，未重查）**：唯一带 `160` 的 SO 是
教程起手占位卡（`trigger 30 / ctx 10`：先弃自己、再 `ca 100` 造真 BL37），池内 BL37 本体是
`chosenAbility 30` ⇒ 旧措辞「BL37 的重复 SO」会让人以为「BL37 另一个副本」，语义误导。

---

## 三、`CardPresentation/Shell/WindowButton.cs:1248`（1↔1 行，实际改 `:1247-1248` 两行文字）

**改前**

```csharp
// 🔴 我们的 `Click()` 原来**先播音、再执行 onClick**（见下面那一句次序）⇒ 即使 onClick 里自己
//   写了「同一键再点就 return」（例如 `AchievementsMenu.Select`），**声音已经响过了**。
```

**改后**

```csharp
// 🔴 我们的 `Click()` 原来**先播音、再执行 onClick**（见下面那一句次序）⇒ 那一下本该被「已选中」吃掉时
//   （真实用法 = `allowClickWhenAlreadySelected = false` 的左栏页签，如 `PlayerProfileWindow:420`），**声音已经响过了**。
```

**为什么过期**：`B141-W66`（`报告/B141_W129_AllowClick.md` §三·1）删掉了 `AchievementsMenu.Select()` 的
同值早退（原版回调 `…<Awake>b__0` 没有那一段），并把成就页 5 颗页签的 `allowClickWhenAlreadySelected`
改成 **`true`** ⇒ 「`AchievementsMenu.Select` 里有同一键再点就 return」**已不成立**。

**新例子（现读真实用法，道理保留）**：
- `Shell/PlayerProfileWindow.cs:420` `_btnWb[i].allowClickWhenAlreadySelected = false;`（左栏 6 颗 prefab = 0，W66 报告 §二 复核；运行期建 5 颗）
- 同族仍在用：`Shell/SettingsWindow.cs:2015`、`Shell/MainMenuSubmenuWindow.cs:908`（均为左栏页签 `= false`）
- 反例（明确**不**走这条）：`Shell/PracticeModePopup.cs:2628` 的 `if (ArmyIndex == i) return;` —— 那是**普通 `EverguildButton`** 的 onClick 早退，第二次点击**仍应响默认按钮音**（该处 `:2625-2627` 已写死「⛔ 别改用 `allowClickWhenAlreadySelected = false`」）。

> 简报原文写「换成仍成立的例子（现读找一处真实的 `allowClickWhenAlreadySelected` 用法）」⇒
> 这里按该口径取了 `PlayerProfileWindow:420`，不再引用已失效的 `AchievementsMenu.Select`。

---

## 四、验证

| 项 | 结果 |
|---|---|
| `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w72 bash 工具/typecheck.sh` | **运行时错误数 0 / 编辑器错误数 0** |
| `B121_WC_Assert.cs` 行数 | 改前 **251** → 改后 **251**（净 0） |
| `B97W5_RowsAndDedupe.cs` 行数 | 改前 **249** → 改后 **249**（净 0） |
| `WindowButton.cs` 行数 | 改前 **1315** → 改后 **1315**（净 0） |
| 行尾 | 三文件均 **纯 LF**（`\r\n` = 0，裸 CR = 0），改前改后一致 |
| 代码语义 | **零改动**（只改 `//` 与 `///` 注释文本；断言、字段、`Click()` 守卫一字未动） |

---

## 五、没做 / 说明（如实）

1. **没跑 Unity** ⇒ 断言未实际执行；本笔不改行为，`typecheck` 是唯一验证。
2. **`WindowButton.cs` 有并发写手的痕迹**：我首次读取之后、本次编辑之前，该文件在盘上被别的写手改过
   （Edit 工具给出的提示）。我只对 `:1247-1248` 两行做定点替换、改后现读确认目标行正确、
   行数与行尾未变 ⇒ **没有覆盖他人改动**。⚠️ 调度台合并时请留意该文件当前所有权。
3. **`B121_WC_Assert.cs` 的坐标漂移**：简报 `:131-132` 是 W38 报告里的旧坐标，现为 `:136-137`（见 §一·2）。
4. 本笔**未新增可断言行为**；按铁律 20 不在代码里新增 `✅ 已复刻` 标记（注释订正不是复刻），仅在此报告记录：

`✅ 已复刻（2026-11-04 · B141 · 文档订正：B121_WC_Assert 同键判据/闸序 · B97W5_BL37Discard 补丁 · WindowButton 举例）`
