# B88 · F2 · Shell / Menu 两条宿主红修复（执行代理报告）

> 指派：`ShellScene.Run` 直接抛异常中止（1 条）+ `MainMenuScene.Run` 2 条红。
> 触碰文件（白名单内，全部）：`Shell/BackgroundOverDrawController.cs`（**新建件，本波产物**）·
> `Editor/MainMenuScene.cs`（**只动 2-a / 2-b 那两条断言与紧邻注释**）。
> ⛔ 没碰：`Core/CardView.cs` · `Battle/**` · `RuleEngine/**` · `Net/*` · `Shell/Shop*` · `Shell/Campaign*` ·
> `Editor/ShopScene.cs` / `Editor/RewardsScene.cs` · `Shell/LiveOpsState.cs`（**未改，见 §2-b**）· 账主表 · 正本 · `git`。

---

## 一 崩溃：`ShellScene.Run` 抛 `MissingReferenceException`（判定 = **β 实现缺陷**）

### 证据

`_tmp_view/shell.log`（修前）`:196427-196438`：

```
MissingReferenceException: The object of type 'CardPresentation.ImageQuad' has been destroyed but you are still trying to access it.
  at UnityEngine.Behaviour.set_enabled (Boolean value)
  at CardPresentation.BackgroundOverDrawController.Pop (…) BackgroundOverDrawController.cs:64   ← 修前第 64 行 = 「恢复新栈顶」
  at CardPresentation.AllianceMemberOptionsPopup.Close () …:282
  at CardPresentation.WindowsManager.CloseAllWindows () …:671
  at ShellScene.RunCore () …Editor\ShellScene.cs:7614
```

### 真因（不只是「对象被销毁」这一句，链路已核到调用点）

`_tmp_view/shell.log:196329 / 196351 / 196373 / 196395` —— 同一份日志里
`AllianceMemberOptionsPopup.Build()`（`…:414` ← `Open():266`）**被连跑了 4 次**，
而 `Build()` 第一句是 `MenuDraw.ClearChildren(root)`（`Shell/AllianceMemberOptionsPopup.cs:357`）
⇒ **上一次那颗整屏底 `_shade` 就此销毁**；随后 `:365` 建新的、`:370` `Push(新件)`。

修前的 `Push` 只做 `_stack.Remove(backdrop)`（**同一个引用**才删）⇒ **旧的死件留在全局栈里**。
最后一跳在 `CloseAllWindows` → `ShowPreviousWindow` 把同一扇窗**重新开回来**（日志 `:196425` 那一串
`GameWindow.TryOpen → OpenByState → Open → Build`），于是栈变成 `[死件基…, 活件]`；
`Close()` 弹掉活件后，「恢复新栈顶」那一句（旧 `:64`）摸到的正是**已销毁**的 `ImageQuad`
⇒ `Behaviour.set_enabled` 抛 `MissingReferenceException` ⇒ 整个 `executeMethod` 中止。

### 改了什么（`Shell/BackgroundOverDrawController.cs`）

1. 新增 `PurgeDestroyed()`：把**已销毁**（Unity `== null` 重载为真）的件从栈里摘掉。
   `Push` 开头调用它 ⇒ 重建路径不再累积死件（正常路径下栈里从此**只有活件**）。
2. 新增 `IndexOfRef()`：按**引用**找下标。⛔ 不用 `List.IndexOf` —— 它对已销毁的
   `UnityEngine.Object` 走 `Object.Equals`（比 `GetInstanceID`），死件那一路不可靠（同一个崩点）。
3. `Pop`：`ReferenceEquals(null)` 早退（**栈空/没建过整屏底 ⇒ 不出声、不抛**，幂等）；
   `alive = backdrop != null` 只决定「要不要给它 `enabled = true`」；出栈后再 `PurgeDestroyed()`，
   然后才「只有自己曾是栈顶才恢复新栈顶」（**至少还能 `set_enabled` 的那一颗**）。

**语义一个字没改**（对照原版 `BackgroundOverDrawController__OnDisable.c`，见该文件头）：
「只有自己曾是栈顶才恢复新栈顶」· 「自己末尾一律 `enabled = true`」两条都在原位；
⛔ 没有退化成「什么都恢复」。新增的两步只处理「已销毁」这一档 —— 原版那边栈项跟着
`OnDisable` 自己走、**不存在**这一档，我们这边由 `MenuDraw.ClearChildren` 直接销毁才必须兜住。

### 复跑读数

`ShellScene.Run` → **`[Shell] === 合计：2102 通过 / 0 失败 ===`**（`_tmp_view/shell2.log:221883`）
· `grep -c MissingReferenceException shell2.log` = **0** · 另有 `[E15] 合计：通过 59 / 失败 0`。
（修前：抛异常、整轮中止，一条合计都没有。）

---

## 二 `MainMenuScene.Run` 两条红

修前：`[Menu] === 合计：5067 通过 / 2 失败 ===`（`_tmp_view/menu.log:133776` / `:211655`）。

### 2-a 教程「督军那行」（判定 = **α 断言过期**，⛔ 不是 UI 缺陷）

- 出处：`Editor/MainMenuScene.cs:6333`（修前行号）。
- 实测 vs 期望：屏上 `战将：<color=orange>Nemesor Zahndrekh</color>`，
  断言期望**写死的英文** `Warlord: <color=orange>Nemesor Zahndrekh</color>`。
- **为什么不一致（查清才改）**：这不是「`Loc.T` 那一刻返回英文」——
  **同一份日志**里上一条同族断言 `★ 督军那行 = 词条 MainMenu/Tutorial/Warlord …`
  （`_tmp_view/menu.log:133485`）实测与期望**都是** `战将：<color=orange>Uriel Ventris</color>`
  即 `Loc.T("MainMenu/Tutorial/Warlord")` 那一刻返回的**就是中文**（`Loc.Default = Chinese`，
  `Core/Loc.cs` 的 `Default` / `Effective` 那一节）。
  ⇒ 真因 = 2026-10-27 **B88-E7** 把 `TutorialModePopup.WarlordLine`（`Shell/TutorialModePopup.cs:843`）
  从「代码里写死英文格式串」改成「取词条 `MainMenu/Tutorial/Warlord`」（zh = `战将：{0}`），
  而 `:6333` 这条断言**没跟着改**、仍写死英文 ⇒ **断言过期**。
  （它新旧口径下都只在英文档绿；本宿主跑的是默认中文档 ⇒ 红。）
- **改法 = (i)**：期望改成 `string.Format(Loc.T("MainMenu/Tutorial/Warlord"), "Nemesor Zahndrekh")`
  —— 与上一条同族断言**同一条取法**（词条表 + 这里硬写的督军名），⛔ **不**调被测窗的
  `WarlordLine`/常量（那是自证）；督军名仍是**独立**的卡表推导结论（`Sautekh_Deck0_Tutorial3_Zahndrekh → SAU3`）。
- **判别力**：把 UI 改用别的键 / 写死某一语档 / 名字换错关 ⇒ 红；
  把词条值本身改成「督军」⇒ 两侧同步变**不会**红 —— 那是**数据口径变更**（正常，不算回归）。
  严格钉「屏上必须是某一语档」的写法只有写死中文字面量，而那正是本工程禁止的（⛔ 写死语档）。
- 复跑：`_tmp_view/menu2.log:134217` ✓ 实测与期望均为 `战将：…Nemesor Zahndrekh…`。

### 2-b 在期右开边界（判定 = **α 断言过期**，实现是对的，`Shell/LiveOpsState.cs` 未改）

- 出处：`Editor/MainMenuScene.cs:17304`（修前行号）。
- **判据核实（照 ⑤ 要求逐条对）**：
  · `Shell/LiveOpsState.cs:58-73` 的 `LiveOpsStateMachine.Compute`：`FinalDate < now ⇒ Expired` →
    `endDate < now ⇒ Finished` → `startDate < now ⇒ Active`，**全部 `DateTime.op_LessThan`（严格小于）**，
    与 `decomp_full/Everguild.LiveOps.LiveOpsEventData__GetState.c:21-47` 及
    `资料/完全复刻/报告/B88_J3_活动线判据.md` §2·1（`:63`/`:64`「原版仍 Active、我们已出期」）**一致**。
  · 挑选闸：`Shell/LiveOpsHandler.cs:926` `SelectInTable` 用 `e.IsCurrentlyActive(utc)`（= `GetState == Active`），
    **B88_J3 已把旧 `IsInPeriod`（`start ≤ now < end`）换掉**（`Shell/LiveOpsLocalCatalog.cs:190` 注释标「旧口径」）
    ⇒ 这条断言的红**不是本波回归实现的错，是断言还停在旧口径**。
  · `now == endDate`（2025-05-01 09:00 UTC）时：`FinalDate < now` 假 · `endDate < now` 假 ⇒ 落到
    `startDate < now` ⇒ **Active（在期）**。`grep` 实测 `Scoring` 类只有 4 份 SO，覆盖该日的只有 `ExpansionOrks1Scoring`。
  · **`expireDate` 那一档一并核过**：该承载面（`Surface.ScoringHelpTitle` 的 kinds = `{Scoring}`，
    `LiveOpsLocalCatalog.cs:652-654`）落到 `ExpansionOrks1Scoring`（`LiveOpsLocalCatalog.cs:360-361`：
    `expirable = 1` · `expire = 1748768400000` = **2025-06-01 09:00 UTC** · `end = 1746090000000` = 05-01 09:00）
    ⇒ `FinalDate = expirable ? expireDate : endDate` = **06-01 09:00**（≠ endDate，所以 `Expired` 这一档
    在 05-01 09:00 那一刻**还不会**截住它）—— `LiveOpsState.cs:99` 的 `FinalDateMs` 与 `Compute` 的
    `finalMs` **两处一致，无缺陷**。
- **改法**：断言改为「`endDate` 那一刻**仍在期**」，并**补一条配对断言**「`endDate` 之后 1 分钟（09:01 UTC）
  不在期」，同时把上面那句「右开区间」的注换成判据出处 + 灭自证说明。
  ⇒ 三条一组（08:59 在期 / 09:00 在期 / 09:01 不在期）**结构性**挡住把 `<` 改成 `≤`（第一条红）
  或把边界方向写反（第三条红）。
- 复跑：`_tmp_view/menu2.log:212081 / 212096 / 212111` 三条全 ✓。

### 复跑读数（`MainMenuScene.Run`）

**`[Menu] === 合计：5070 通过 / 0 失败 ===`**（`_tmp_view/menu2.log:219058`）
（修前 5067 / 2；总数 5069 → **5070 = +1**，正是 2-b 新补的配对断言）。
其它宿主内小计亦全绿：`[E18] 38/0` · `[E19] 47/0` · `[A1] 46/0` · `[E10] 50/0`。

---

## 三 验收与卫生

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s88f2 bash 工具/typecheck.sh` | 运行时 **0** / 编辑器 **0** |
| `ShellScene.Run` | **2102 通过 / 0 失败**，无异常 |
| `MainMenuScene.Run` | **5070 通过 / 0 失败** |
| 两个 `.cs` 行尾（二进制读） | `BackgroundOverDrawController.cs` LF 104 / CRLF 0 / 裸 CR 0；`MainMenuScene.cs` LF 20230 / CRLF 0 / 裸 CR 0（**没翻行尾**） |
| 场景被重写吗 | `Scenes/MainMenu.unity` mtime 仍 13:10、`Shell.unity` 13:01（**两次跑都没重写场景**）；`CardBase.unity` 00:15 / `DeckEditor.unity` 00:14 是**本会话之前**留下的改动，不是这两跑。备份仍在 `_tmp_view/scene_bak_20261028/` |

---

## 四 顺手发现（⛔ 我没动，交调度台分流）

1. **整屏底那条全局栈【零自检覆盖】**：全仓 `grep BackgroundOverDrawController.` 只有
   `AllianceMemberOptionsPopup` 的 `Push`/`Pop` 两个调用点（**没有任何断言**读 `Depth` / `Top` / `Contains`）。
   ⇒ 这次崩的那条链（重建 → 死件留栈 → 恢复死件）**只能靠跑崩发现**。
   建议补 3 条：① `Build` 两次后 `Depth == 1`（钉住「重建不留死件」）② `Pop` 空栈不抛 ③
   「只有曾是栈顶才恢复新栈顶」的正/负例。落点建议 `Editor/ShellScene.cs`（不在本代理白名单）。
2. **`AllianceMemberOptionsPopup.Build()` 每次 `Open()` 都整棵重建**（`MenuDraw.ClearChildren` +
   重新 Push），而 `Close()` 只 `Pop` 一次 —— 若哪天出现「Build 了但没 Close」的路径，栈仍会留死件
   （现在由 `PurgeDestroyed` 兜住，属**兜底**不是根治）。是否该在 `Build()` 里先 `Pop` 旧的，
   属语义决策（= 原版「先 `OnDisable` 再 `OnEnable`」），⛔ 我没自行改。
3. `Push` 里 `_stack.Remove(backdrop)` 仍是 `List.Remove`（走 `Object.Equals`）；对**活件**没问题，
   但与本轮新加的 `IndexOfRef` 口径不统一，建议后续统一成引用比较（不影响今天任何行为）。
