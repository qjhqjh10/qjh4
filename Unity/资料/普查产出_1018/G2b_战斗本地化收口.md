# G2b_战斗本地化收口.md — 战斗侧本地化（第十三轮 · 2026-10-18）

> 写手代理 `G2b_战斗本地化收口`。**只动白名单**；⛔ 没跑 Unity（自检由主对话在同步点跑）；
> ✅ 秒级类型检查跑到 **0 / 0**（多轮，末次附在 §1）。
> ⚠️ **白名单里没列 `Core/Loc.cs`，但我改了它**（甲/乙 都要求「新增词条 + 同批核 `HasEntry`」，
> 权威表只能在那儿）⇒ **见 §7·A，请主对话复核这条越界**。

---

## 1. 一句话结论 + 类型检查

**做了**：① **乙 —— 两条路径统一**：`END TURN` / `YOUR TURN` / `ENEMY TURN` 三条从
`CardText.Phrases`（键 = 英文原文）**搬到 `Loc` 表**（键 = 原版 `mTerm`），`Phrases` 降级成兜底；
`N available` 从我们自拼的 `可选目标 N` 改成**原版拼法** `<词条> 里换 {0}`。
② **甲 —— 又接了两条**：`Battle/Mulligan/Undo`（原来我们只有一档「换」，原版是**同一颗钮两档**）、
`Battle/HUD/TargetsAvailable`。③ **丙7**：`Net/NetBattle.cs` 的标注补全（文案一字未改）。
④ **顺手修一处同族缺陷**：`CardText.TurnLabel` 拿「字体闸」当「语言闸」⇒ 英文档下那半行仍是中文。
⑤ **把 W6 §7·C 那份清单逐条判完**（§4 的表：**接 6 条 · 不接 22 条 · 另有 65 条给判据与待办**）。

**没做**（每条都有判据，见 §4/§6）：`Battle/Cemetery/*` 19 条 · `Battle/Effect/*` 25 条 ·
`Battle/Tips/*` 24 条 —— **都登记成待办**（不是「不做」，理由写在表里）。

类型检查（末次，改完即跑）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

---

## 2. 逐条交件表（本批**新接**的 6 条）

| 键 | 原字面量 / 原路径 | 落点（文件:行） | 载体 | 中文来源 | 断言 | 今天可见影响 |
|---|---|---|---|---|---|---|
| `Battle/HUD/EndTurn` | `CardText.Phrase("END TURN")` | `Core/Loc.cs`（表）· `Core/CardText.cs:374`（`PhraseTerms` 转发）· `Battle/BattleDriver.cs:10075`（建）· `:11236`（每次 `UpdateHud` 重取） | **代码字面量** `0x4288678`：`ClockManager.SetEndTurnText`（`ClockManager__SetEndTurnText.c`）= `GetTranslation(词条)` → **`String.ToUpper`** → 写 TMP | `zh_CN.csv:34`（`END TURN,~,结束回合`，**精确**） | §5 ①–③ | 英文档下这颗钮**从中文变英文**；换语言**当场变**（原来只在建 HUD 时写一次） |
| `Battle/HUD/YourTurn` | `Phrase("YOUR TURN")` | `Core/Loc.cs` · `Core/CardText.cs:375` · `BattleDriver.cs:11114`（回合行） | **代码字面量** `0x4288a48`：`BattleManager.NextTurn` → `BattleTipController.ShowHeadsUpMessage(GetTranslation(词条))`（`…_d__395__MoveNext.c:480-483`） | `zh_CN.csv:450`（`Your turn,~,你的回合`，**值同、键大小写不同 ⇒ 近邻**） | §5 ①–③ | 同上（回合行右半段） |
| `Battle/HUD/EnemyTurn` | `Phrase("ENEMY TURN")` | 同上 | **代码字面量** `0x4288768`：同 `ClockManager.SetEndTurnText` 的**另一支** | EN/ZH **都自拟**（`zh_CN.csv:446` 是近邻 `Enemy turn...,~,敌方回合……`） | §5 ①–③ | 同上（对手回合那一刻） |
| `Battle/HUD/TargetsAvailable` | `CardText.TargetsAvailable(n)` = `可选目标 n` / `n available` | `Core/Loc.cs` · `Core/CardText.cs:597` | **代码字面量** `0x4288950`：`SupportMethods.GetTargetsAvailableText(n)` = `GetTranslation(词条)` → **`Replace("{0}", n)`**（占位符字面量 `0x4265d10` = `"{0}"`，本批实读） | `zh_CN.csv:49`（`0 available,~,可用 0` ⇒ 模式 `可用 {0}`） | §5 ④–⑦ | 技能卡面板那行从「可选目标 3」**改成原版拼法**「可用 3」（英文档 `3 available`） |
| `Battle/Mulligan/Undo` | 无（**原来缺这一档**） | `Core/Loc.cs` · `Battle/MulliganPanel.cs:113`（`UndoTerm`）· `:116`（`CardBtnWord` **唯一判据**）· `:367` · `:448` | **代码字面量** `0x4288f08`：`MulliganFrame.{ChangeCardButtonOnClick,SetupMulligan,UpdateButtonText}` 三处逐字相同的 `term = (card+0x228==0xe) ? Undo : Replace; Localize.set_Term(那颗钮, term)` | EN/ZH **都自拟**（值在远端；`zh_CN.csv` 里 `Undo` 这个英文串**不存在**） | §5 ⑧–⑫ | 标记要换的那张牌，钮上**从「换」变成「撤销」**（原来两档都印「换」= 复刻缺漏） |
| （`Battle/Mulligan/Replace` 等 5 条） | —— | —— | —— | —— | §5 ⑧（HasEntry 列表 5 → **6** 条） | 无（只是把新的第 6 条纳入收口扫描） |

**改动落点（本件净增，已扣掉 W6 的）**：`Core/Loc.cs` +59 · `Core/CardText.cs` +47/−4 ·
`Battle/BattleDriver.cs` +22 · `Battle/SkillPanel.cs` +5 · `Battle/MulliganPanel.cs` +30/−1 ·
`Net/NetBattle.cs` +26（纯注释）· `Editor/BattleScene.cs` +106。

---

## 3. 两条路径统一（乙）：改了哪几处 · `Phrase` 现在还剩什么 · 字体闸怎么处理

### 3.1 改了哪几处

| # | 改动 | 落点 |
|---|---|---|
| 1 | `PhraseTerms` **转发表**加三条：`END TURN→Battle/HUD/EndTurn` · `YOUR TURN→…/YourTurn` · `ENEMY TURN→…/EnemyTurn` | `Core/CardText.cs:374-377` |
| 2 | `Phrases` 兜底表里那三条**降级**（加注释说明「权威已搬到 `Loc`，这里只在表里没这个键时兜底」） | `Core/CardText.cs:329-340` |
| 3 | `TargetsAvailable(n)` **不再自拼**，改成 `Term("Battle/HUD/TargetsAvailable")` + 换 `{0}` | `Core/CardText.cs:597-606` |
| 4 | `TurnLabel(n)` 补上「当前语档是中文」那一半（它以前只看字体闸） | `Core/CardText.cs:385-395` |
| 5 | `_endTurnLabel` 的**字**从「建 HUD 时写一次」改成 **`UpdateHud` 每帧重取** | `Battle/BattleDriver.cs:11236` |

### 3.2 `Phrase` 现在还剩什么（**照旧全部留着，一条没删**）

`Phrases` 表仍在，`Phrase()` 的两级查找顺序不变（先转发表 → 再兜底表）。表里剩下的键 = 25 条，
**逐条核过它们在 `Battle/` 那 93 条字面量 + 346 条 prefab `mTerm` 里都没有对应词条**：
`GAME OVER` `YOU WIN` `YOU LOSE` `DRAW` `HAND` `DECK` `DISC` `HP` `CHOOSE ACTION` `PICK A TARGET`
`NO LEGAL TARGET` `MELEE` `RANGED` `ABILITY` `THIS UNIT ALREADY ACTED` `STUNNED` `THIS UNIT CANNOT ACT`
（`END TURN`/`YOUR TURN`/`ENEMY TURN` 三条已转正，留在表里只为兜底）。
⇒ **结论：`Phrases` 从此是「没有对应 `mTerm` 的键」的兜底表**，正是主对话定的口径。

### 3.3 那道字体闸的后果 —— **处理了，做法是「把闸搬进新口」，不是删闸**

W6 如实记的后果：本批把战斗侧改成直接 `Loc.T` ⇒ `Phrase()` 里那道
「拿不到中文字体资产就回英文」的闸**对那些件不再生效**（`Loc.T` 不做字体判断）。

本批的处置（**补等价保护**）：

- 新增 `CardText.Term(term)`：**走 `Loc.T`，但 `Zh == false` 时改取英文列**（新开的 `Loc.EnOf(key)`）。
  表里没有这个键时**仍走 `Loc.T`**（⇒ 出声 + 回键名，不静默）。
- 转发表那条路（`Phrase` → `Term`）与 `TargetsAvailable` **都走它** ⇒ 新接的这几件**闸没丢**。
- ⛔ **没有**去改 `Loc.T` 本身（那会波及全工程 15+ 处的既有消费点，跨面）；也**没有**把闸删掉。
- ⚠️ **如实记两条**：① `Loc.EnOf` 是**只为这道闸开的口**（doc 里写死了「⛔ 别当第二条取值路」）；
  ② **W6 那 15 条 + `Shell/`+`Deck/` 的既有 `Loc.T` 消费点仍然没有这道闸**
  （字体资产本身已进仓库 `Assets/CardPresentation/Resources/Fonts/NotoSerifCJK-Regular SDF.asset`，
  `Zh` 为假只在「有人删了它」时发生）⇒ **要不要给全工程补一道是跨面的决定，交主对话**。

---

## 4. 甲 —— W6 §7·C 那份清单**逐条判**（接 / 不接都要给判据）

> 清单口径：`Battle/` 前缀的**代码字面量 93 条**（`d:/2/tools/il2cpp_out/stringliteral.json`，
> `RVA = 地址 − 0x180000000`；本批逐条 dump 过，并把每条**反向对到了它的 `.c` 消费点**）
> ＋ **prefab `mTerm` 28 条**（`grep '"mTerm": "Battle/' */MonoBehaviour/*.json | sort -u`）。
> ⚠️ 两处**新事实**（本批读出来的，W6 没记）：**①** 有些 `Battle/` 字面量**不是词条**
> （见下 E 组）；**②** `Battle/` 字面量**一共 93 条**，其中 **`Battle/ChooseCard/Instructions-`** 是
> `键 + "-"` 那个后缀形态、**`Battle/Effect/`** 是拼键用的前缀 —— **这两条不是独立词条**。

### A 组 —— ✅ 本批**接了**（6 条）

`Battle/HUD/{EndTurn,YourTurn,EnemyTurn,TargetsAvailable}` · `Battle/Mulligan/Undo`
（＋ W6 已接的 15 条不重复计）。逐条见 §2。

### B 组 —— ⛔ **不接**，且**判据是空的 / 形状对不上**（22 条）

| 键 | 落点（原版）/ 消费面 | 不接的判据（**是不是"原版没有"**都说清） |
|---|---|---|
| `Battle/Cemetery/*` **19 条** | `CemeteryManager.GetActionText` / `GetActionWithParams`（两份 `.c` 亲读）：**动作类型 int → 词条**，参数走 `GetFirstParamText` + `String.Format`（分隔符是 `<b><link="1,` / `"><u>` / `</u></link></b>`） | 🔴 **键在**、**值不在**（远端 I2）＋**形状不同**：原版那一族记的是**"谁做了什么动作"**（draw / secret order / display card in hand / collect spirit stone / exit ambush …），我们 `RefreshBattleLog` 那 10 类记的是**动作的后果**（伤害/阵亡/回手/触发）⇒ **只有 5 类一一对得上**（Play / Attack×2 / Ability×2），其余 5 类原版**没有对应词条**。**接一半 = 中英混排**。⇒ **要做，判据 → `CemeteryManager__GetActionText.c` 的 enum→term 表；先做哪几条 → 先把日志的 10 类**重新按原版分类**，那是表现层重构不是本地化**。已登记待办。 |
| `Battle/Effect/*` **25 条**（＋前缀 `Battle/Effect/`） | `GameStaticData.GetEffectDesc`（`.c` 亲读）= `GetTranslation("Battle/Effect/" + <枚举名>)[.Replace("{[0]}", 值)]` ＋ `SupportMethods.ModifyLocalization` 后处理 | 🔴 **键在**、**值不在**（25 条）＋ **哪一面消费还没钉死**：我读到的模板占位符是 **`{[0]}`（单值）**，而本地唯一那条样例（`EffectText` 节点，`bundle_scenes_scenes_battlearena1/MonoBehaviour_3772.json` 等 3 份）是 **`Get {0} Melee Attack, {1} Ranged Attack and {2} Health`（三值）** ⇒ **两条不是同一个模板**。我们对应的那一处（`CardDisplayWindow.RowsOf` 的「谁给我加的 buff」）读的是引擎 `TempBuff.Name`（只有 `attack/ranged/health/armour` 四个属性名 + 关键词）。⇒ **要做，判据 → 上面那份 `.c` 的 `case` 分派；先做哪几条 → 先钉死"消费面"（拿远端 I2 表或原版实拍）**。已登记待办。 |
| `Battle/Tips/*` **24 条** | 逐条对到了 `.c`（见 §4·E 附表）—— 全部走 `BattleManager.*` / `PlayerHand` / `MulliganManager` 的**拒绝提示**（`BattleTipController.NotifyCantDoAction` / `ShowHeadsUpMessage`） | 🔴 原版那一族是**按因分档**的（`CantAttack*` 8 条 · `AttackBlock*` 4 条 · `DamageFatigue*` 2 条 · 其余 10 条），而**我们产提示的那条链是 `RuleCodes.Describe`（16 条笼统话，在 `RuleEngine/` = 本批黑名单/G3 手上）** ⇒ 一一对应**不成立**，硬接 = 猜。⇒ **要做，判据 → 那 24 条的 `.c` 消费点（已逐条落表）；先做哪几条 → `Battle/Tips/DragToTarget` 与 `…/UnitNotReady`（有独立触发点，见下）**。已登记待办。 |
| `Battle/Tips/DragToTarget`（**单列，因为它有独立缺漏**） | `BattleManager.Update`（`BattleManager__Update.c`，**松手那一支**）：卡回手（`CardScript.MoveBackToHand`）之后 `BattleTipController.NotifyCantDoAction(GetTermTranslation("Battle/Tips/DragToTarget"))` | 🔴 **我们这一档只有 `cantdo` 语音**（`BattleDriver.SpeakCantDo`，`ChatMessage.ICantDoThat` = 枚举 3），**没有这句文字提示** ⇒ 属**复刻缺漏**（判据齐：键 + 触发点都在同一个分支里）。⇒ **要做**，已登记待办。 |
| `Battle/Tips/NoTargetAvailable` ·`NotEnoughMana` ·`NotEnoughRoom` ·`NotYourTurn` ·`PleaseWait` ·`UnitNotReady` ·`InvalidTarget` ·`HandFull` ·`PendingBerzerk` ·`MuteEnemyChat` · 8 条 `CantAttack*` · 4 条 `AttackBlock*` · 2 条 `DamageFatigue*` | 同上一格 | 同上一格（笼统 ↔ 分档，无法一一对应） |
| `Battle/SyncError` | `SearchOpponentManager.CheckStartProp`（`.c`）：`WindowsManager.ShowPopUp(manager, "Battle/SyncError", …)` | 🔴 **它根本不是词条**：`ShowPopUp` 的第 2 实参经 `LoadPopUpAndShow` **只被存进协程字段 `+0x30`**（两份 `.c` 亲读）—— 那是**弹窗的标识/预制体名**，不是 `mTerm`。（对照组：同一族的**消息**词条是 `Battle/EnemySyncError`，在 prefab 上是 `Localize.mTerm`。）⇒ **不是"不接"，是"它不该接"**。 |
| `Battle/Prebattle/CasualString` | `DeckInfoPopup.StartPracticeMatch`（`.c`）：同 `ShowPopUp(manager, "Battle/Prebattle/CasualString", …)` | 同上（**弹窗标识**）＋ 消费面在 `Shell/DeckInfoPopup.cs`（**黑名单**）。 |
| `Battle/BattleEnd/{DamageDone,Tactics,TroopsDead}` | `BattleEndPlayerData.Setup`（`.c`）：三个格子逐格 `GetTranslation(词条) + ": " + 数值`（顺序：TroopsDead → Tactics → DamageDone） | 形状对不上：我们是**一行自加的副标题**（`N 回合 敌方战将最低生命 X`；原版 `EndBattlePanel` 子树**无标题/副标题节点**，W22 已查实）⇒ ⛔ 别硬套。⇒ **要做（属缺漏：我们没建那三个统计格）**，判据 → 上面那份 `.c` 的 3 个 `GetTranslation`。已登记待办。 |
| `Battle/AlliancePanel/NotInAnAlliance`（＋载体①的 `…/TitleLabel` `…/PlayerLabel` `…/AllianceLabel`） | `BattleAlliancePanel.Open`（`.c`）：无联盟时把某个 TMP 设成 `GetTranslation("Battle/AlliancePanel/NotInAnAlliance")` | **我们没建这块面板**（原版 `FrontCanvas/Alliance Panel`；同族节点在我们 `BattleDriver.cs:9885` 的注释里被提到过，且那份实例 `activeInHierarchy=False`）⇒ **没有消费点**。⇒ **要做（缺漏）**，已登记待办。 |
| `Battle/HUD/CreatedBy` | `SupportMethods.GetCreatedByText(name)`（`.c`）= `GetTranslation(词条).Replace("{0}", name)`；消费面 = 攻方卡亮相演出里那句「created by …」（`BattleDriver.cs:3995` 的注释已记这一步） | **我们那一步只有动画、没有那个文字**（全工程 `grep -i "creator\|创建者"` **只命中那一条注释**）⇒ **没有消费点**。⇒ **要做（缺漏）**，判据齐（键 + `{0}` 拼法 + 触发时机都在 `.c` 里）。已登记待办。 |
| `Battle/HUD/Replay` | `ClockManager.Start`（`.c`）：把某颗钮的字设成 `GetTranslation(词条)` 再 `ToUpper` | **我们那颗回放条是纯图标**（`Battle/ReplayBar.cs:147` `Make("Replay", …)` 只给图 `40K_replay_bt_restart`，没有 `Label`）⇒ 没有消费点。⚠️ 顺带：我们只在 `matchType == 160` 才显示那条（同 `ReplayHud.Setup`），**我们从不进回放局** ⇒ 今天零可见影响。 |
| `Battle/Settings/Exit` | `BattleSettingsWindow.Open`（`.c`）：**`matchType == 0xa0(160)` 时**才把某个 TMP 的 term 设成 `Battle/Settings/Exit` | 我们设置窗没有那颗钮、也不进 160 那一档对局 ⇒ 没有消费点（同上一格的处境）。 |
| `Battle/MatchType/{0}` | `BattleLogItem.Initialize`（`.c`）：`String.Format("Battle/MatchType/{0}", 对局类型枚举)` | 消费面 = **战报条目**（我们对应 `Shell/BattleLogData.cs`，**黑名单**）。⇒ 交主对话/下一批。 |
| `Battle/ChooseCard/Instructions` 的 `…-` 后缀形态 | `ChooseCardMenu.{Setup,GetTittleText,SetUpTitleText}`（3 份 `.c`） | **不是独立词条**：`键 + "-" + <actingCardId>` 那种拼法（W6 已记）；前缀那条本批已在表里。 |
| `Battle/BattleEnd/TapToContinue`（载体①，全库 1 颗） | 节点 `Tap To Continue`（1920×95）；`bundle_menus_assets_all/MonoBehaviour_-5883487598702440642.json` | 🔴 **判据不足 + 无消费点**：① 那颗实例的父链**跨 bundle**（父 GO 不在本包，`m_FileID` 指外部 ⇒ 本地走不到根）；② 同名的节点在 `资料/说明书/04_界面UI/菜单全树.md:11876/9561/13728` 挂在**活动/排位到期窗**（`Shell/` 侧），不是战斗结算；③ 战斗结算那屏照原版**零文字零按钮**（W22 查实）⇒ 我们没有那一格。📌 留给将来：`zh_CN.csv:171` 有 `Tap to continue,~,点击继续`。 |

### C 组 —— ⚠️ 本批**核查后确认"原版确实没有对应词条"**（升级成否定证据）

| 我们这一处 | 结论 | 否定证据（**搜过哪些载体 / 哪些名字**） |
|---|---|---|
| `CardText.Phrases` 里剩下那 17 条（`GAME OVER` `YOU WIN` `PICK A TARGET` `STUNNED` …） | 原版**没有**对应 key | ① `stringliteral.json` 全表按 `Battle/` 前缀取 **93 条**，逐条比对：`GAME OVER`/`WIN`/`LOSE`/`PICK A TARGET`/`NO LEGAL TARGET`/`CHOOSE ACTION`/`STUNNED`/`HAND`/`DECK`/`DISC`/`HP` **零命中**；② `assets_full` **全部 91 个顶层条目**里 `Battle/` 前缀的 `mTerm` **28 条**，同样零命中。 |
| `Battle/MulliganPanel` 那句「（回车 = 完成）」 | 原版没有（W6 已按原版去掉） | 同上两条载体。 |
| `Battle/WaitBanner` 的**触发时机** | 原版查不到（只有**文案**查到了） | W6 已记；本批复核：`Battle/Mulligan/WaitEnemy` 的消费点 `MulliganManager.SetWaitingForEnemy`（`.c`）里**没有**时机判据。 |
| `CardText.TurnLabel` 的「第 N 回合 / TURN N」 | 原版**没有**这个词条 | **逐条核过** `Battle/HUD/*` 那 8 条（`CardsInHand` `CardsLeft` `CreatedBy` `EndTurn` `EnemyTurn` `Replay` `TargetsAvailable` `YourTurn`）—— 里面**没有**"回合计数"这一条。 |

### D 组 —— 载体①（prefab `mTerm`）28 条的处置

W6 已处理的 24 条 + 本批的 4 条（都记在 §2/§4）＝ **28 条全覆盖**，**无剩余**。
（对照：`Battle/AlliancePanel/{TitleLabel,PlayerLabel,AllianceLabel}` 属**没建的那块面板**，
`Battle/BattleEnd/TapToContinue` 属判据不足，`Battle/HUD/{LostConnectionMsg,WaitOpponentConnectionMsg,
PleaseWaitConnection,MatchAlreadyFinished}` + `Battle/EnemySyncError` 属**重连弹窗那一族**
—— W6 §7·A 已把 9 条 `LocalizedString` 字段查清并**如实记了两条不接的理由**，主对话已裁定接受。）

### E 组 —— 📌 附：93 条字面量的**消费点反查表**（本批新产出，给将来的批次当索引）

| 键 | 消费它的方法（`d:/2/tools/decomp_full/`，2026-10-18 逐条实读） |
|---|---|
| `Battle/AlliancePanel/NotInAnAlliance` | `BattleAlliancePanel__Open.c` |
| `Battle/BattleEnd/{DamageDone,Tactics,TroopsDead}` | `BattleEndPlayerData__Setup.c` |
| `Battle/Cemetery/*`（19） | `CemeteryManager__GetActionText.c`（18 条）· `CemeteryManager__GetActionWithParams.c`（`AmbushedTroop`） |
| `Battle/HUD/{CardsLeft,CardsInHand}` | `DeckManager__DisplayDeckSize.c` · `PlayerHand__ShowHandSize.c` |
| `Battle/HUD/CreatedBy` | `SupportMethods__GetCreatedByText.c` |
| `Battle/HUD/{EndTurn,EnemyTurn}` | `ClockManager__SetEndTurnText.c` |
| `Battle/HUD/Replay` | `ClockManager__Start.c` |
| `Battle/HUD/TargetsAvailable` | `SupportMethods__GetTargetsAvailableText.c` |
| `Battle/HUD/YourTurn` | `BattleManager._NextTurn_d__395__MoveNext.c` |
| `Battle/MatchType/{0}` | `BattleLogItem__Initialize.c` |
| `Battle/Mulligan/Undo` · `Replace` | `MulliganFrame__{ChangeCardButtonOnClick,SetupMulligan,UpdateButtonText}.c` + `MulliganManager__SetupMulliganButton.c` |
| `Battle/Mulligan/{Instructions,secondTurn,GoFirst}` | `MulliganManager__ActivateMulligan.c` |
| `Battle/Mulligan/WaitEnemy` | `MulliganManager__SetWaitingForEnemy.c` |
| `Battle/Prebattle/CasualString` | `DeckInfoPopup__StartPracticeMatch.c` |
| `Battle/Settings/Exit` | `BattleSettingsWindow__Open.c` |
| `Battle/SyncError` | `SearchOpponentManager__CheckStartProp.c` |
| `Battle/Effect/*`（25） | `GameStaticData__GetEffectDesc.c` |
| `Battle/Tips/AttackBlock*` `CantAttack*` `AttackGiantKiller`（13） | `BattleManager__IsValidAttackTarget.c` |
| `Battle/Tips/{NotEnoughMana,NoTargetAvailable,NotEnoughRoom,NotYourTurn,PleaseWait}` | `BattleManager__CanPlayCard.c`（+ `TryUsingActiveAbility*` / `CanUseActiveAbility` / `CanUseWaystone` / `ReadyToUseActiveAbility` / `WaitingForBlockingAction`） |
| `Battle/Tips/UnitNotReady` | `BattleManager__CanUseActiveAbility.c` |
| `Battle/Tips/{DamageFatigue,DamageFatigueEnemy}` | `BattleManager._ResolveFatigue_d__587__MoveNext.c` |
| `Battle/Tips/DragToTarget` | `BattleManager__Update.c` |
| `Battle/Tips/HandFull` | `PlayerHand._Add{DrawnCardToHand,CardNotDrawnToHand,CardNotDrawnToHandAtIndex}_d__*.c` |
| `Battle/Tips/InvalidTarget` | `BattleManager._ResolvePlayActiveAbility_d__479__MoveNext.c` · `BattleManager__FinishedChoosingAbilityTargetByClick.c` |
| `Battle/Tips/MuteEnemyChat` | `VoiceLinesController__DisplayWarlordChatMessage.c` |
| `Battle/Tips/PendingBerzerk` | `BattleManager__EndTurnClick.c` |

---

## 5. 断言清单（逐条 `文件:行`）

**A. 新增**

| # | 位置 | 条数 | 断什么 |
|---|---|---|---|
| ① | `Editor/BattleScene.cs:2932-2980`（`WaitBanner` 那一节之后） | **7** | `END TURN` 钮的字 **与** 回合行的归属段（**期望值一律用字面量**：`结束回合` / `END TURN` / `对手回合` / `ENEMY TURN`）；切 `English` 再断**三处**（钮 = `END TURN`、行首 = `TURN `、归属 = `ENEMY TURN`）；切回后**复原**（那一条同时验「换语言真的会重设 HUD」） |
| ② | `:5311-5341`（技能卡面板那一节） | **4** | `sp.TargetsText` = `可用 N` / `N available`（**字面量**，不用 `CardText` 当期望）＋ `HasEntry` ＋ 英文档再断一次 ＋ 切回复原 |
| ③ | `:10838-10862`（换牌面板那一节） | **5** | `HasEntry(Undo)` · 标记后 = `Loc.T(Undo)`（`:10848`） · 🔴 **判别式**「两档必须不是同一串」 · 未标记的第 2 张仍是 `Replace`（档是**逐张**的） · 取消标记后复原 |
| ④ | `:8370`（§W6 那组收口扫描） | 覆盖面 +5 键 | 键表 **27 → 32**：加 `Battle/HUD/{EndTurn,YourTurn,EnemyTurn,TargetsAvailable}` + `Battle/Mulligan/Undo`（每条都过 `HasEntry` ＋ **中/英两档各取一次、两列都不得为空或等于键名**） |

**B. 改了判据的（原文会红 / 本批跟着改）**

| # | 位置 | 原判据 | 现判据 |
|---|---|---|---|
| ⑤ | `:10878` 的 `HasEntry` 合并条 | 「换牌面板用到的 **5 条键**」 | 「**6 条键**」（加 `UndoTerm`；文案里的数字也改了 —— 不写数字留着会变成假记录） |

**C. 可能需要主对话复核的两条**（⚠️ 我判断是绿的，但**没跑 Unity**）

1. `Editor/BattleScene.cs:10834`（W6 那条 `CardBtnWordAt(0) == Loc.T(ReplaceTerm)`）——
   我改成两档后它**仍应绿**：那一刻面板是刚 `drv.Begin(...)` 开的，而 `_marked` 在
   `MulliganPanel.cs:294` **开面板时清空** ⇒ 第 0 张未标记。
   🧨 真红的话，只可能是「开面板没清 `_marked`」—— 那本身就是缺陷。
2. `Editor/BattleScene.cs:2936` 那组要求「那一刻是对手回合、且不在换牌、且没结束」——
   上面刚 `driver.SimulateEndTurn()`（`:2907`），我核过中间没有 `SimulateAiTurn` ⇒ `Ctx.Active != _me` 成立。

⚠️ 自检**由主对话在同步点跑**（本件未跑 Unity）。**建议只跑 `BattleScene.Run` 一条**
（本批只动了 `Battle/` + `Core/{CardText,Loc}`，`Editor/BattleScene.cs` 是唯一宿主）。

---

## 6. `git diff --numstat` + 行尾

```
   1251     15   Unity/MyGame/Assets/CardPresentation/Battle/BattleDriver.cs      （W6 1229/15 → 本件 +22/-0）
     84      9   Unity/MyGame/Assets/CardPresentation/Core/CardText.cs            （W6  37/5  → 本件 +47/-4）
    414      5   Unity/MyGame/Assets/CardPresentation/Core/Loc.cs                 （W6 355/5  → 本件 +59/-0）
    113     17   Unity/MyGame/Assets/CardPresentation/Battle/MulliganPanel.cs     （W6  83/16 → 本件 +30/-1）
      5      0   Unity/MyGame/Assets/CardPresentation/Battle/SkillPanel.cs        （W6 未动   → 本件 +5/-0）
    261     15   Unity/MyGame/Assets/CardPresentation/Net/NetBattle.cs            （W6 235/15 → 本件 +26/-0，纯注释）
    972     42   Unity/MyGame/Assets/CardPresentation/Editor/BattleScene.cs       （W6 866/42 → 本件 +106/-0）
```

⚠️ 上表**是 vs `HEAD` 的整份 diff**（W6 的改动还没提交，所以含它那一份）——
**括号里那列才是本件的净增**（= 现值 − W6 交件报告里自己记的数）。
`BattleDriver.cs` / `Editor/BattleScene.cs` / `Net/NetBattle.cs` 会话开头就带 `M`。

**行尾逐文件核过**（`b.count(b'\r\n')` vs `b.count(b'\n')`，**不是** `file` 命令）：

| 文件 | CRLF | LF | 判定 |
|---|---|---|---|
| `Battle/BattleDriver.cs` | 12064 | 12064 | **纯 CRLF（未翻）** ✅ |
| `Editor/BattleScene.cs` | 17255 | 17255 | **纯 CRLF（未翻）** ✅ |
| `Core/CardText.cs` | 626 | 626 | **纯 CRLF（未翻）** ✅ |
| `Core/Loc.cs` | 0 | 908 | **纯 LF（未翻）** ✅ |
| `Battle/MulliganPanel.cs` | 0 | 564 | **纯 LF（未翻）** ✅ |
| `Battle/SkillPanel.cs` | 0 | 447 | **纯 LF（未翻）** ✅ |
| `Net/NetBattle.cs` | 0 | 1129 | **纯 LF（未翻）** ✅ |

（全程用 Edit 工具，⛔ 没用 `sed -i`、⛔ 没用 python 文本写。）

---

## 7. 没做到 / 判据不足 / 顺手发现

### A. 🔴 **越了白名单一步：改了 `Core/Loc.cs`**（请主对话复核）

简报的白名单写的是 `Core/{CardText,Tooltip}.cs`，**没有 `Core/Loc.cs`**。但：
- 甲要求「**每接一条同一步核 `Loc.HasEntry(键)`**」、要求中文列给 `csv:<行>`；
- 乙要求「战斗侧统一走 `Loc.T` + **原版 `mTerm`**」；
- `Loc` 那张表是全工程**唯一**的语言权威（`Loc.cs` 文件头：「全工程唯一一份」），⛔ 两处写同一条规则 = 迟早不一致。
⇒ 不新增词条，这两条**都做不到**。我判断这是白名单的遗漏（W6 那一轮也改了 `Loc.cs`，355/5），
**已改并在 §2 逐条列出**。⛔ 若这属于「撞车风险」，请回退 `Loc.cs` 那两处（`Loc.cs:720-757` 的
4 条 `Battle/HUD/*` + `Loc.cs:652-659` 的 `Battle/Mulligan/Undo`，以及 `Loc.cs:819-827` 的 `EnOf`），
其余改动（`CardText` 的转发、HUD 刷新、`MulliganPanel` 两档、断言）**不依赖删表**、只是会红。

### B. 🔴 **顺手发现（真缺陷，跨面 ⇒ 交主对话）**：`CardText.Zh` 是「字体闸」，不是「语言闸」

`CardText.Zh { get { return TmpFont.Available; } }` —— 它**只看中文字体资产在不在**，
**不看 `Loc.Current`**。而中文字体资产**已经进仓库**（`Assets/CardPresentation/Resources/Fonts/
NotoSerifCJK-Regular SDF.asset`）⇒ **`Zh` 恒真**。后果：

| 走 `CardText.Zh` 的入口 | 英文档下的实际表现 |
|---|---|
| `CardText.Name(id)` / `Name(id, nameZh)` | **印中文卡名** |
| `CardText.Faction(key)` | 印中文阵营名 |
| `CardText.Keyword` / `KeywordSegment` / `Effect` / `EffectSentence` | 印中文 |
| `CardText.Phrase(...)` **兜底表**（`PICK A TARGET` `STUNNED` `ABILITY` … 17 条） | **印中文**（本批转正的那三条**已修**） |
| `CardText.TurnLabel` | 本批**已修**（`+ Loc.Current == Chinese`） |

⇒ **选「English」之后，卡面/阵营/关键词/效果小字/HUD 兜底短语这一大片仍然是中文** ——
只有走 `Loc.T` 的那些件（窗口、本批接的这几条）会变。
⚠️ **我没有改 `Zh` 本身**：它一改，卡面（`Core/CardView.cs`）、`Shell/`、`Deck/` 的**输出全体**跟着变，
那是**跨面的口径决定**（简报也把 `Core/CardView*.cs` / `Shell/` / `Deck/` 列成黑名单）。
⇒ **这是本批最大的一条"没做到"**，判据齐（一行代码），**请主对话裁定**。

### C. 顺手发现的其它四条

1. **`Battle/HUD/CardsInHand` 的英文列可疑**：我们表里两条的 EN 都是 `Cards left`
   （W6 按那两颗节点的静态 TMP 定的）。本批按 `zh_CN.csv` 复核：`csv:69/70` 只有
   `Cards left: %d` 与 `Cards left: %d/%d`（**带数字**），**没有**裸 `Cards left` 这个源串
   ⇒ 那两条的 EN 是**近邻推的**，不是精确命中。⚠️ **未改**（改动会动 W6 已交件的值，且要主对话先定口径）。
2. **`Battle/Tips/GoFirst` 的中文列**：W6 记「EN/ZH 都自拟」。本批核到
   `zh_CN.csv` 里**没有** `You go first` 这个源串 ⇒ 维持自拟，**结论不变**（这条只是复核）。
3. **`Battle/Tips/DragToTarget` 有独立缺漏**（见 §4·B 单列那格）：我们「拖了没落到有效目标就松手」
   只出 `cantdo` 语音、**没有那句文字提示** ⇒ **要做**，已登记。
4. **`Battle/HUD/CreatedBy` 有独立缺漏**：原版攻方卡亮相时会显示「created by <玩家名>」
   （拼法 `GetTranslation(词条).Replace("{0}", name)`，方法体亲读），我们那一步**只有动画** ⇒ **要做**，已登记。
5. 🔴 **字体语料（`AllChinese()`）里【没有】`Loc` 表的中文列** ——
   `Editor/TmpSetup.cs:500` 只 `foreach (CardText.AllChinese())`，而 `AllChinese()` 收的是
   本文件那几张表 + `Phrases.Values`（`CardText.cs:611-620`），**⛔ 不含 `Loc.Table` 的 `Zh`**。
   后果：**W6 那一批新词条（`手牌剩余`/`牌库剩余`/`结束回合`…）与本批的（`可用 {0}`/`撤销`…）
   都不在"要烘哪些字"的语料里**；今天**不会真缺字**（字体资产是 TMP **Dynamic**，运行期按需补字形），
   但 `AllChinese()` 那条自检**覆盖不到它们**（= 「改了文案忘了重烘」那一族）。
   ⇒ **要做**，判据 → `TmpSetup.cs:489-500` 的注释与 `CardText.cs:611`；
   ⛔ 本批**没动**（改语料要重跑 `TmpSetup.BuildCjkFontAsset`，而 `Editor/TmpSetup.cs` 也不在白名单里）。

### D. 没查清的部分（如实记，**没有拿猜测填空**）

1. **`ClockManager.SetEndTurnText` 那个 `bool` 的语义**：方法体是 `param_2 != 0 ⇒ EndTurn`、
   `== 0 ⇒ EnemyTurn`。**调用点没找到**（`grep -rl` 全 25,096 个方法体，只在 cctor 里出现）⇒
   我**只取了"这两条键各自对应哪个词"这一层**，⛔ **没有**搬那个二选一的行为（我们那颗钮恒印 `END TURN`）。
   ⚠️ **这条要记一笔**：若原版真是「不是你的回合时钮上写 `ENEMY TURN`」，那我们**缺一个行为**。
2. **`Battle/Effect/*` 的消费面**没钉死（见 §4·B 那一格：两套占位符 `{[0]}` vs `{0}` 打架）。
3. **`Battle/Cemetery/*` 的 19 条值**（中英）本地零命中 ⇒ 只能靠远端 I2 表。
4. **`Battle/BattleEnd/{Defeat,Draw}`** 的英文（W6 遗留）仍未解决。
5. **13 个战场是否逐场一致**（W6 遗留的保留项，本批没动）。

---

（本文件由写手代理 `G2b` 产出；所有判据均给了 `文件:行` / 资产路径 / 方法体；
⛔ 没跑 Unity、⛔ 没动 git、⛔ 没改正本；类型检查 **0/0**。）
