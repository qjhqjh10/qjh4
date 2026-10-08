# W_CreatedBy — `A985④` 卡面「由谁造出来的」（`Battle/HUD/CreatedBy`）

**本笔改了 3 个文件**：`RuleEngine/Core/BattleContext.cs` · `Core/CardView.cs` · `Battle/BattleDriver.cs`（都在白名单内）。
秒级类型检查 3 次全绿（运行时 0 / 编辑器 0）。行尾：`BattleContext.cs` = **LF**（原样）、`CardView.cs` / `BattleDriver.cs` = **CRLF 全量**（原样）。

---

## 一、原版：显示在哪一层 / 什么时机 / 什么文案 / 什么时候清

**节点** = `CreatedByText`（卡根 `CardUI` 的**第一个子件**，排在 `2DCard` **之前**）。

| 问题 | 答案 | 出处（第一权威 = 方法体/解包字段） |
|---|---|---|
| **哪一层** | 卡视图上那个 TMP 节点 `CreatedByText`；`BasicCardUI.createdByText` | `BasicCardUI__SetupCreatedByText.c`（`t.text = newText`）；节点树 `2D层_battlearena1全树.md:314/344`（13 场每张 `BattleCardUI` 上都有，出厂 `inactive`） |
| **位姿** | `anchoredPosition = (0, **1.65**)` · `sizeDelta = (**2.5, 0.38**)` · 锚点/轴心全 `(0.5,0.5)`；父（卡根）`2.5437×3.3686` | `bundle_menus_assets_all/RectTransform/RectTransform_4803967432083755832.json`；⭐ **运行期 dump 逐位吻合**：`runtime_ui_dump_Battle_Arena_1.tsv:165` = `0.0,1.6` / `2.5,0.4` |
| **文字形态** | 纯白；`m_fontSize = 2.45` + **autosize `[0.3, 3.0]`** + `m_overflowMode = 0`(Overflow)、不折行、居中；材质 `Asar-Regular White w outline`（`_OutlineWidth 0.2` · `_FaceDilate 0.2` · 黑 · 无 underlay） | `MonoBehaviour_4833287377952691000.json` · `Material_6389513473928706012.json` |
| **文案** | `GetTranslation("Battle/HUD/CreatedBy").Replace("{0}", 创建者卡名)`，创建者卡名 = `RawCardScript.GetLocalizedCardName(creator.rawCard)` | `SupportMethods__GetCreatedByText.c`（键字面量在 `il2cpp_out/stringliteral.json`）；预制体占位串 = `Created by someone fancy` ⇒ 模板形态 `Created by {0}` |
| **什么时机** | `BattleCardUI.DisplayCreatedByText(state)`：**先把节点关掉**；只有 `card.createdBy != null` **且** `state ∈ {inHandShowing=8, inHandPlaying=10}` 才点亮（`state==10` 那支还要 `card+0x40`(`isPlayer`) == 0） | `BattleCardUI__DisplayCreatedByText.c:9-50`；**同判据第二处** `BattleCardUI__SetObjectVisibility.c:100-133` |
| **什么时候清** | 演出收尾：`_DisplayOffensiveCardInCenter_d__264` 的 **state 3** 把那个节点 `SetActive(false)`（`…MoveNext.c:79-80`） | 同上 |
| **`createdBy` 记在哪 / 谁写** | `CardScript.createdBy`（`+0x268`，类型 `CardScript`）——**只有三处写点**：`AddNewCardToHand` / `AddNewCardToDeck` / `_ResolveSummonUnit`，**来源都是「正在结算/刚打出的那张卡」** | `CardScript.cs:1518`；`BattleManager__AddNewCardToHand.c`（`lVar15+0x268 = param_3`）· `..._AddNewCardToDeck.c` · `..._ResolveSummonUnit_d__510__MoveNext.c:112`；传参者 `CardScript__ProcessCompanion.c`（传自己）/ `AbilityLogic__PlayAbility.c`（传技能属主） |
| **第三个消费面（未做）** | 坟场/日志那张大卡也印：`CemeteryManager.DisplayCard(rawCard, createdByText)` → `SetupCreatedByText` | `CemeteryManager__DisplayCard.c:52`；文案由 `AddDrawTrapAction.c:68` / `AddExecuteSecretOrderAction.c:72` / `SetupCommonElements.c:340,651` 算好传进来 |

## 二、引擎侧：creator 加在哪

**形态**：`BattleContext` 上的一张表 `Dictionary<int /*CardInstance.Id*/, CardDef>` + `CreatedByOf/HasCreatedBy/CreatedByCount`；
**盖章点 = `NewInstance` 一处**（全对局唯一的「新造一份」出口）。

- **为什么是表不是 `CardInstance` 上的一位**：本笔白名单只开到 `BattleContext.cs`/`UnitState.cs`（`CardInstance.cs`
  2026-10-18 刚被 `A885/A886` 动过）。键 = `CardInstance.Id`（每局从 1 起、同种子同序号）⇒ 不引入新的不确定性。
  ⛔ 将来搬上 `CardInstance` 时**连同那段注释一起搬**，别两处各存一份。
- **来源 = `ActingUnit?.Card ?? PlayingCard`** —— 次序是**照本仓既有口径**（`EffectResolver.cs:4117` / `:7234` 就是同一个写法）。
- 🔴 **多补了一处**（必须记）：`PlayingCard` 原来只在 `ResolveOps` 入口写，而**打出单位卡时 `PlayCard` 里紧接着就造东西**
  （`SpawnTideCopies` 造潮涌复制品 `RuleCore.cs:1750`、`PlayCompanions` 造伴生卡 `:1929`）——**那一刻还没进任何 `ResolveOps`**
  ⇒ 只靠入口那句会把来源记成**上一张**结算的卡（静默写错一个名字）。⇒ 在 `NotePlayed` 里**顺手把 `PlayingCard` 刷成刚打出的那张**
  （原版那三处调用点传的正是「刚打出/正在结算的那张卡」）。**不改变任何既有读者**：读点全在 op 里，而 `ResolveOps` 入口每次都覆写它。
- **两个记账口**：本笔**没有新增动作路径**（只是给既有的「造牌」动作加了一条**派生注解**）⇒ 不需要走
  `LocalAct` / `SimpleAI.ExecuteAction`。
- **对录像 / 哈希 / 老局的影响（判了）**：
  · `NetProtocol.Fingerprint` / `StateHash` / `PlayerHash` **只哈希** Turn/Active/Winner/Events.Count + 手牌/牌库/弃牌/场上单位
    ⇒ **这张表不进哈希**，联机与录像对账**一个字都不变**；
  · 表是**确定性地重算**出来的（不是录进文件的东西）⇒ 录像格式未变、**老录像/老联机局不受影响**（回放时会自然多出这行字）；
  · 开局那几处 `NewInstance`（初始牌库/督军/起手/防御卡）跑在任何 `ResolveOps`/`NotePlayed` **之前** ⇒ 来源 `null` ⇒ 不盖章（与原版一致）。

## 三、表现层：改了哪

| 改哪 | 内容 |
|---|---|
| `CardView.cs` | ① 层 `_createdBy` + `_createdByName`（必须存：`SetData` 每刷新一次都重跑 `BuildTextLayers`）；② 常量组 `CreatedByAt01`(=`ToAt01(0, 1.65)`) / `CreatedByBoxW 2.5` / `CreatedByBoxH 0.38` / `CreatedByEmOfCard 2.45` / `CreatedByInk` 纯白；③ `CreatedByLine`（模板）+ `ApplyCreatedByOutline`（0.2/0.2 黑描边）；④ `FillCreatedBy()`，由 `BuildTextLayers` ⑥ 调（**原地刷**，没来源时 `Fill(空)` 把层拆掉 = 原版 `SetActive(false)`）；⑤ `SetCreatedBy(CardDef)` / `CreatedByShown`；⑥ **`SetFace` 里补一句 `Show(_createdBy, !board)`**（铁律 10⑤：换形态入口只有这一处 —— 不补就是「打出去的兵在场上还挂着这行字」）；⑦ `ApplyTint` 补一句颜色跟随（它不在 `_layers` 里，那个 foreach 刷不到它）。 |
| `BattleDriver.cs` | `SyncHand` 里紧挨 `ShowEphemeral` 那一行之后逐张推 `SetCreatedBy(Ctx.CreatedByOf(h[i]))` —— **每轮重推**（视图是复用来的，同 `ShowEphemeral` 那条注释的理由）。 |

**落点与时机怎么对的**：位姿逐位照 `RectTransform` 字段（`PlaceAt` 算出来的根局部 y = `(0.5−0.004701)×3.3313` = **+1.6496** = 原版 `+1.65`）；
文案照 `GetCreatedByText` 的两跳（本地化卡名 → 模板替换），卡名走 `CardText.Name` = 原版 `GetLocalizedCardName`；
显隐照「有来源才亮、场上一律不亮」。

## 四、断言 / 并排比 / 没查清

- ⛔ **断言：本批没写**（宿主不在白名单）。需要：① `Editor/CardBaseDemo.cs`（卡面两态：`SetCreatedBy(A)` ⇒ `CreatedByShown`
  = 名字；`SetCreatedBy(null)` ⇒ `null`；`SetFace(Board)` ⇒ `null`；🧨 改坏法 = 删掉 `SetFace` 里那句 `Show`；
  灭自证 = **不信 `_createdBy != null`，信 `activeSelf`** —— `CreatedByShown` 已经按 `activeSelf` 取了）；
  ② `RuleEngine/Editor/RuleEngineTest.cs`（引擎侧：造一张牌的 op 之后 `CreatedByOf(新那份) == 源卡`、
  开局那些份 `== null`；🧨 改坏法 = 把 `NewInstance` 里那句盖章删掉）。
- 🔴 **要配一次并排比**（铁律 10⑥ —— 这一改**动了卡面输出**）：抽一张**被造出来的**卡与 PnP 成品图并排渲。
  ⚠️ **PnP 是印刷品、不含这行**（原版这行是运行期点亮的）⇒ 这次并排比**只能验「像不像/有没有压住卡名与立绘」**，
  **不能拿它当参数来源**；参数已全部取自解包字段（见上表）。
- **如实标注的「我们挑的」两条**（都写进了 `CardView` 的常量注释，别当原版）：
  ① **压谁**：原版 `m_Children` 里它在 `2DCard` **之前**（按 uGUI 次序应画在卡面**底下**），可它的中心正好压在卡顶沿上
     ⇒ 画在底下只看得见露在卡外那一条。我们放在**卡面之前**（可见优先）—— **没跑到实况**，待真 Play 核；
  ② **没照搬** `m_fontSizeMin = 0.3` 那条下限 + Overflow：照搬的话长卡名（英文 12+ 字符即触发）会横向溢出到 ~2 倍卡宽。
- **没查清 / 需要另开一笔**：
  1. **文案键没进语言表**：`Core/Loc.cs` 不在白名单 ⇒ 现在的中文「由 {0} 创建」**是我们写的**（英文那半有旁白证据）。
     收口 = 往 `Loc` 里加 `Battle/HUD/CreatedBy` 后把 `CreatedByLine` 换成 `Loc.T(...)`（⛔ 别留两套）。
  2. **第三个消费面（坟场/日志那张大卡）没做** —— 判据已在 §一表末行；它还依赖日志侧存着「这条动作的造牌者」。
  3. 原版那两档状态判据（8/10 + `isPlayer`）在我们这边**没有对应物**（卡视图没有 `CardOptions` 状态机）⇒
     现在是「有来源就亮」；**近似**如实写在 `SetCreatedBy` 的注释里。
- ⚠️ **撞车提示**：本笔进行中 `BattleDriver.cs` 另有人写了约 **+53/−2** 行（`A985⑦`/`A991`/`A964`/`A940` 那些标记）；
  本笔只加了 `SyncHand` 那 11 行，**没有回滚任何东西**。
