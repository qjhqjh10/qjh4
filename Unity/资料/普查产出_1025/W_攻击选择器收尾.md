# W · 攻击选择器收尾 5 件（2026-10-25）

> 执行代理 `W`。白名单只碰两个文件：`Battle/AttackSelector.cs`（CRLF）· `Editor/BattleScene.cs`（CRLF）。
> 两文件**行尾核过**：`AttackSelector.cs` CRLF 1882 / bareLF 0；`BattleScene.cs` CRLF 45116 / bareLF 0（`git diff --numstat` 350/101 · 1306/134，增删比正常）。
> 类型检查：`TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（跑过 3 次；中途一次唯一报错在 `Assets/RuleEngine/Editor/RuleEngineTest_EngineA3.cs`，是别的代理正在改的文件，后来自己消失）。
> ⛔ 按红线**没跑 Unity**（`-executeMethod`），所以 `BattleScene.Run` 里这 6 条新断言**只过了编译，没跑过**。

---

## W1 · `SetSelected` 的过期 doc（只改注释）

- **结论**：原 doc 那句「全仓 `selector.SetSelected(` **0 命中**、时序差仍然存在」**已过期**，就地改掉。
- **证据**：`BattleDriver.cs:13932` `if (selector != null) { selector.SetSelected(kind); selector.Hide(); }`
  （在 `CommitCommand` 里，选定时就写 `LastAttackType`）；引擎侧结算那次写仍在
  `RuleCore.cs:5834/10157/10345/10556`（两处不冲突：表现层即时反馈 vs 规则层落账）。
- **改动**：`AttackSelector.cs:1239-1247`（`SetSelected` 的 doc 换成订正后的三句）。

---

## W2 · `ApplyOptionIcons` 补「按军队查不到 ⇒ 整颗钮关掉」（AU1-B 残差）

- **判据（第一权威 = 反编译）**：
  - `d:/2/tools/decomp_full/UnitOnBoardAttackTypeSelector__Toggle.c`（`LAB_18064ad32` 那一支）：
    `lVar6 = ArmyUtilities.GetActiveAbilityIconForArmy(self, *(int*)(card.rawCard + 0x2c), 0)` ——
    **只按阵营值查**，随后 `uVar5 = CardTraitCollection.GetTraitForActiveAbilityRawCard(...)` 只当角标数值。
  - `…/AttackTypesButtonsController__ToggleAttackTypesSelector.c:54-56`：
    `CardDisplayAttackTypeButton__Toggle(btn, bVar4 & param_2)`，`bVar4 = (iconSprite != null)` ⇒
    **取不到图 ⇒ 整颗技能钮不显示**（⛔ 不是「给占位图」）。
  - 5 条真值只在 `Core/ArmyIcons.cs`（`ActiveAbilityIconsByArmySO`：80 Pray / 100 Duty / 110 Agenda / 130 Ferocity / 10 Oath）。
- **改法**：**`Army > 0`**（= 合法阵营值）的技能钮**只认军队那条**；`ArmyIcons.ActiveAbilityIconName`
  取不到 ⇒ `Hidden = true; Enabled = false` + `Debug.LogWarning` + `continue`（不再落回 `IconName` 占位图）。
  - 🔴 **`Army <= 0` 一律保持旧口径（`IconName` / 占位图）、不关钮、不报警**：
    `0` = 调用方没给；**`-1` = `ArmyIcons.Id` 认不出阵营**（自研 `Ember` / `Tide`）——
    这是**初版我写错的地方**（写成 `Army != 0`），已在同日修掉，见文末 **W7（热修）**。
  - 顺带把顺序改成「军队优先于 `IconName`」：原版那一处**只有军队**一条判据（5 个阵营上两者 1:1 吻合，不一致时以原版为准）。
- **证据 / 改动**：
  - 门：`AttackSelector.cs:1281` 起（`ApplyOptionIcons`），闸 = **`Army > 0`**（`:1299`），取不到 ⇒ 关 + 出声 + `continue`（`:1310` 一带）。
  - 计数器：`AttackSelector.cs:1078-1082` `public static int ArmyIconMissingHides { get; private set; }`（每次 `Show` 重刷 ⇒ 断言比增量）。
  - 断言：`BattleScene.cs:8182-8248`（★W2① 正例 `Army=10` ⇒ 钮在、告警不动；★W2② `Army=20`（合法值但不在那 5 条）⇒ 钮不在 + 告警 +1；★W2③ **闸的判别式** `Army=-1` ⇒ 钮在（落回占位图）+ 告警不涨）。
- **安全性核查（重要，别省）**：关钮会不会把玩家卡死（技能钮没了、又没有近战/远程）？
  查了三条：① `HasAbility = (Card.Ability != null)`（`RuleEngine/Core/CardDef.cs:5399`）；
  ② `CardDef.cs:137` 自己写着「池内 `desc` 含 `Ability:` 的卡 **0 张**」；
  ③ 现读卡池 `cards_engine.json`：`hasActiveAbility == 1` 只出现在那 5 个阵营（23+13+24+9+18 = **87 张**），
  另 7 个阵营的 `activeAbilityLogic` 非空、但 `hasActiveAbility` 全不是 1（那是触发逻辑，不是玩家主动技能）。
  ⇒ **原版卡池**里技能钮只可能出现在那 5 个阵营。
  🔴 **但这条核查漏了一半（2026-10-25 热修时补正）**：**自研卡**（`StarterCards.Ember()` / `Tide()`，
  走 `CardDef` 的 `!FromOriginalPool` 那一支 `EffectSpec.Parse`）**能带 `Ability:`**
  （`RuleEngine/Data/StarterCards.cs:65/93/107` 三张）而阵营是 `Ember` / `Tide` ⇒ `ArmyIcons.Id` → **-1**。
  那一档**必须落回占位图**（`BattleDriver.cs:13815-13817` 明写），⛔ 不能按「查不到图标就把钮关掉」处理 —— 见 **W7**。
- **⚠️ 我把一条自己的错判改回来了（留痕）**：中途我以为「`ArmyIcons.ActiveAbilityIconName` 返回原版带空格名、
  `CardArt.Ui` 不做规范化 ⇒ 这条从来没生效」——**错**：`ArmyIcons.cs:222-233` 有 `ResourceNameOf`
  （空格→下划线），`ActiveAbilityIconName` 返回的**已经是我们 `Resources/Art/ui/` 的文件名**。
  我加的 `ArmyIconFile` 包装已**全部删掉**，相关注释也改成正确口径（`AttackSelector.cs:1269-1271`、`:1288-1289`）。

---

## W3 · `HighlightAttackTypes` 那一族（AU3 报「未复刻」）—— **判据已闭，本侧无需改；缺的那部分不在本白名单**

### 判据（逐条现读 `d:/2/tools/decomp_full/`）

| 方法 | 它是谁的 | 什么时候跑 | 做了什么 |
|---|---|---|---|
| `AttackTypesButtonsController__HighlightSelectedAttackTypeButtons.c` | **选择器**（`attackTypeButtons` 那 3 颗） | ① `UnitOnBoardAttackTypeSelector__Toggle.c` **打开选择器**那一支（`HighlightSelectedAttackTypeButtons(unit+0x120)`）；② `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` **选定时**（`ChangeAttackType` 之后同一串） | 逐颗 `SetActive(highlight(+0x38), param_2 == attackType(+0x20))`（**只有已选那格亮圈**）；命中那格 `DOScale(原始 scale × scaleMultiplierWhenSelected(+0x40), DAT_1834b2dc8)` + `SetEase(0x1b)`（= 27 = **OutBack**）；没命中那格 `DOScale(原始 scale)` + `Tooltip.Hide()`；`attackType == 4` 且 `+0xb0` 空才 `Tooltip.Show(+0x68/+0xa8)` |
| `CardDisplayAttackTypeButton__HighlightIfNeeded.c` | **单颗钮**（同一件事的单件版） | `AttackTypesButtonsController__ToggleAttackTypesSelectorJustOne.c` · `UnitOnBoardAttackTypeSelector__ToggleJustOne.c`（= 只显一颗那条路） | 与上一条同形（`SetActive` + `DOScale` + tooltip） |
| `CardHighlight__HighlightAttackTypes.c` → `HighlightAttackType__HighlightIfNeeded.c` | 🔴 **卡面上的「攻击类型图标」那一族**（`CardHighlight+0x48` 列表 / `BasicCardUI+0xC8` 列表），**不是选择器** | ① `CardScript__ChangeAttackType.c`（单位 `attackType` 变了）→ `BasicCardUI__AttackTypeChanged`；② `CardHighlight__ToggleCombatPreviewHighlight.c`（战斗预览伤害/致死那一支，末尾 `HighlightAttackTypes(damage)`） | 逐颗：`SetActive(highlightObject(+0x30), param_2 == 本颗 attackType(+0x20))`；命中那颗 `DOScale(origin × scaleModifierOnHighlight(+0x38 = `0x3f99999a` = **1.2**))`，其余回原尺寸并关 `highlightObject` |

### 结论

1. **选择器里没有「额外整组高亮」这回事** —— 除了「已选打法那一格」之外，原版就是**不亮**（`SetActive(false)`），
   没有任何「三格全亮 / 压暗非选」的档。⇒ 本侧 `RefreshPicked`（`AttackSelector.cs:1639`）已经是这个口径：
   圈只 `IsPickedSlot` 亮、选中 `HighlightScale 1.3 / PickTweenSeconds 0.3 / Ease.OutBack`、其余 `defaultEaseType`，
   tooltip 只在 `attackType == 4` 且一颗钮一次（`RefreshAbilityTooltip` `:1712-1741`）。
   **`DAT_1834b2dc8 = 0.3f`** 与我们的 `PickTweenSeconds` 一致（常量出处另有记录：`资料/待办判据_战场与战斗视图.md:1237`、`资料/战斗规则与数值_出处.md:242`）。
   ⇒ **本白名单内没有要改的**（新增 0 行）。
2. AU3 第 34 条把它标成「未实现这一族」——**只对了一半**：那一族**不在选择器**，而在卡面，
   而且本仓**已经有一半**：`Core/CardView.cs:276-286 / 7829` 就是 `HighlightAttackType.highlightObject`
   指向的那两个节点（`Highlight` / `Highlight ranged`，`SetTargetGem` 点亮底光）。
   **真正缺的是另一半**：`HighlightIfNeeded` 的「命中那颗 ×1.2 缩放 + `highlightObject` 开关」由
   `CardScript.ChangeAttackType → BasicCardUI.AttackTypeChanged` 驱动；我们这侧自己承认没做 ——
   `Core/CardView.cs:11738-11739`：「基准 = 攻击类型高亮态用 `HighlightAttackType.TargetScale`……（我们这四个数值层没有那套高亮，基准恒 `Vector3.one`）」。
- **还差什么**（给主对话派活用，⛔ 不在本白名单）：
  ① 落点 = `Core/CardView.cs`（攻击类型数值层 + `highlightObject` 两个 glow 的显隐）+ `Battle/BattleDriver.cs`
  （把「单位 `LastAttackType` 变了」这条广播给视图 —— 对应原版 `CardScript.ChangeAttackType → BasicCardUI.AttackTypeChanged`）；
  ② 待补判据：`HighlightAttackType` 的补间时长 `DAT_1834b3178` **没读**（本次没去解 PE 常量池），
  以及 `originalScales(+0x3c)` 初始化的确切时机（`Start` 还是 `TargetScale` 首次那条）。
  ⇒ 这两条不补，做出来就是猜；建议先派查判据代理把这两个常量读出来（方法同 `资料/战斗规则与数值_出处.md` §三）。

---

## W4 · 自造副标题 `_note` —— **已删**（标题 `_title` 同日也删，见文末 **W6（追加）**）

- **复核原版 subtree（13 场全树同构）**：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:233-241`：
  ```
  - Drag Attack Selector [-311,1019 622x122]
    - Select Attack Background (inactive)
    - Select Melee Button -> Highlight
    - Select Active Skill Button -> Highlight, ValueText
    - Select Range Button -> Highlight
  ```
  ⇒ **没有标题、没有副标题、没有第二行说明字**（只有技能那格的 `ValueText`，= 我们的 `Badge_*`）。
  另有两份旧普查同结论：`资料/普查产出_1017/UI/R_A1024_字体按用途普查.md:1003`、`W_A1024_对账.md:169`。
- **`_note` 删掉（可见件）**：两条建件路径（`BuildOnCanvas` / `BuildInWorld`）、`RefreshLayout` 的摆位、
  `Hide` 的收尾、`ClearPieces` 的销毁 + 字段，全部移除（`AttackSelector.cs:856-862` · `:949-951` · `:1418-1419` · `:1129-1131` · `:749-767`）。
- **那串文字没丢**：`_noteText` 保留，但**只当技能钮 tooltip 的正文兜底**（`RefreshAbilityTooltip` `:1726`）——
  这正是原版那一格的数据来源（`AttackTypesButtonsController__ToggleAttackTypesSelector.c` 末段
  `RawCardScript.GetLocalizedAbilityDesc` → `CardDisplayAttackTypeButton +0xa8`）⇒ 数据来源不变，只是不再是可见标签。
  字段 doc 写明这条 + 「⛔ 别再加回一行 `Label`」（`AttackSelector.cs:622-629`）；`Show(..., note:)` 的形参 doc 同步改成「不再显示、保留形参是为了不动 `BattleDriver.cs:13756` 那个调用点」（`:1137-1140`）。
- **断言（删可见件要配「该件不存在」）**：`BattleScene.cs:8165-8176`
  `selRoot.Find("SelectorNote") == null`（`selRoot = FrameRect ?? transform`，两档都覆盖）。
- **`_title`：本件里先留着，**同一天按主对话追加口径（照原版、查实没有就删）**已经删掉** ——
  见文末「**W6（追加）**」。留这条只为说明当时的判断链：它同样是自造的
  （`CardText.Phrase("CHOOSE ACTION")` 这条键**不在官方本地化表**里、只在我们手写的 `Core/Loc.cs:2771`）。

---

## W5 · 悬停断言打错了受体（5 处 → 改打钮本体，留 1 条圈的结构断言）

- **为什么错**：生产里**未选中**的两格，圈是 `SetActive(false)` 的（`IsPickedSlot`，判据
  `CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句）⇒ 真鼠标压不到圈；断言却直接调圈上
  `EventTrigger.OnPointerEnter`，**绕过 `activeSelf`** ⇒ 恒绿（假绿）。生产受体是钮本体 `Image`
  （118.372×118.806，`m_RaycastTarget = 1`）；选中那格才是「钮 ∪ 圈」。
- **改动（`Editor/BattleScene.cs`）**：

| # | 位置 | 原 | 现 |
|---|---|---|---|
| 1 | `:8138-8163`（W20·W10② `pindown`） | `HighlightTriggerForTest(0)` | `IconTriggerForTest(0)`；**另留** `HighlightTriggerForTest(0) != null` 的**结构断言**（`：8146-8150`，说明「已选那格走圈」） |
| 2 | `:8260-8271`（W20·W10⑤ 技能选中音） | `HighlightTriggerForTest(1)` | `IconTriggerForTest(1)` |
| 3 | `:8383-8399`（5b「悬停不改缩放」） | `HighlightTriggerForTest(2)` | `IconTriggerForTest(2)` |
| 4 | `:31346-31356`（A462 安全窗） | `HighlightTriggerForTest(2)` | `IconTriggerForTest(2)` |
| 5 | `:35135-35149`（A964/模型③ 三选一） | `HighlightTriggerForTest(OrdIdx964(k))` | `IconTriggerForTest(…))`；`hits964` 那行的「受体」列一起改成「钮本体 EventTrigger」 |
- 另**顺手补了一条**（W5 的配套，原来没人断）：`BattleScene.cs:8199-8203`
  `selectedType = 4 ⇒ HighlightActiveForTest(1) && !HighlightActiveForTest(0)`
  （= 「圈只在已选那格亮」；🧨 退回「三格全亮、非选压暗」⇒ 近战那格变 True ⇒ 红）。
  全文 `HighlightActiveForTest` 之前**零调用**，这条是它的第一个用户。

---

## 没查清的部分

1. `HighlightAttackType` 的补间时长 `DAT_1834b3178` 与 `originalScales(+0x3c)` 的赋值时机（W3 后半，卡面线）——**没读**。
2. ~~`_title` 到底删不删~~ —— ✅ **已定并已做**（主对话追加口径：照原版查实没有就删）⇒ 见 **W6（追加）**。
3. 新增的 **6 条**断言（W2① / W2② / 圈只在已选那格亮 / `SelectorNote` 不存在 / `SelectorTitle` 不存在 / 圈的 `EventTrigger` 结构断言）**只过了编译，没跑**（红线禁 Unity）⇒ 需要主对话在同步点跑 `BattleScene.Run` 验收。

## 顺手发现（都要立账，不在本白名单里改）

- ✅ **已就地订正（主对话随后把这两份放进允许范围）**：`资料/普查产出_1017/UI/W_A1024_对账.md:169` 与
  `R_A1024_字体按用途普查.md`（表后加 `> ⚠️ 2026-10-25 订正` 块 + 汇总行内联）现在都写明
  「`SelectorTitle` / `SelectorNote` **已删**」并带上两级判据。详见 W6「顺手」一节。
  ⚠️ 仍有一份没改：`资料/普查产出_1023e/A23_战斗布局批.md:64`（不在允许范围）。
- **AU3 第 34 条口径偏了半个身位**：`HighlightAttackTypes` 是**卡面**那一族（`CardHighlight`/`BasicCardUI`），
  不是选择器；本仓 `CardView` 已经有 `highlightObject` 那半，缺的是「×1.2 + 开关」那半（详见 W3 的「还差什么」）。→ 应按「卡面线」立账，别记在选择器名下。
- `AttackSelector` 的**世界档**（`_frame == null`，uGUI 宿主没登记时的兜底）**不建任何 `EventTrigger`**
  ⇒ 那两档里所有「悬停送到」的断言都恒为「没挂上」。本轮没动它（判据：原版只有 uGUI 那一套），
  但值得确认「世界档在真机上是否还会出现」——若会出现，世界档的悬停是走 `UpdatePointer` 的矩形判定（`:1590-1630`），本身自洽，只是断言不能拿 `IconTriggerForTest` 去断。

---

## W6（追加）· 自造标题 `_title` —— **口径已定：照原版，查实没有 ⇒ 已删**

### 判据（两条都查不到）

1. **原版 prefab / 资产子树（一级证据，逐颗现读）**
   - `d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/GameObject/Drag Attack Selector.json`
     （`m_Name = "Drag Attack Selector"`，`m_Component[0].component.m_PathID = 2973`）
   - `RectTransform/RectTransform_2973.json` → **`m_Children` 恰好 4 个**，逐个解出名与 `m_IsActive`：
     | 子件 pathID | GameObject 名 | active |
     |---|---|---|
     | 3061 | `Select Attack Background` | **false** |
     | 3368 | `Select Melee Button` | true |
     | 2626 | `Select Active Skill Button` | true |
     | 2804 | `Select Range Button` | true |
   - 再往下一层（`RectTransform_{3061,3368,2626,2804}.m_Children`）：
     `3061 → []` · `3368 → [Highlight]` · `2626 → [Highlight, ValueText]` · `2804 → [Highlight]`。
   - ⇒ 整棵子树里**没有任何标题/文字节点**（除了技能那颗的 `ValueText`，= 我们的 `Badge_*`）。
     第二来源（同一结论）：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:233-241`（13 场全树同构）。
2. **官方本地化表里没有这个键**
   - `Unity/数据/游戏数据/官方本地化_en+zh-CN.json`：10,854 条；`terms/Battle/**` 共 **156** 个键，
     `ChooseAction` / `Choose action` / `CHOOSE ACTION` **0 命中**；`Battle/Phrase/*` 这一类键**一条都没有**。
   - ⚠️ 关于「另核 `I2Languages` 源表」：原始资产 `d:/2/远端缓存_旧profile_1013/导出/I2Languages_typetree.json`（43 MB）
     的负载在 `temp` 里是**逐字节重复密钥 XOR** 的字节数组（`temp: [166,21,25,...]`）⇒ **对它做字面 grep 无意义**
     （实测 `Choose one card` 也 0 命中，可见不是键不存在而是读不到明文）。
     可用的判据就是上面那份**解码产物**（K07 从同一张表并出来的 en+zh 全量）⇒ 无 `ChooseAction`。
3. ⇒ 那句标题的唯一来源是我们手写的兜底短语：`Core/Loc.cs:2771` 的 `Battle/Phrase/ChooseAction`
   （`Core/CardText.cs:712` 把 `"CHOOSE ACTION"` 映射到这个键）⇒ **自造，删**。

### 改动清单（`_title` / `_titleText` 全删，照删 `_note` 那套）

| 落点 | 改法 |
|---|---|
| 字段 `Label _title;` / `string _titleText;` | 删；原位留一段「为什么删 + 两级判据」的红字（`AttackSelector.cs:619-625`） |
| `BuildOnCanvas`（uGUI 档建件） | 删 `HudCanvas.MakeText(..., "SelectorTitle")`；注释扩成两件（标题 + 副标题）一起说明判据（`:858-872`） |
| `BuildInWorld`（世界档建件） | 删 `Label.Create(..., "SelectorTitle")`（`:953-956`） |
| `Hide()` | 删 `if (_title != null) _title.gameObject.SetActive(false);`（`:1134` 一带） |
| `RefreshLayout()` | 删整块「显隐 + `SetText` + `localPosition = center + (0, BarH/2+26px)`」（原位置留一行红字，`:1420-1421`） |
| `ClearPieces()` | 删 `doomed.Add(_title...)` 与 `_title = null;` |
| `Show(...)` | `_titleText = title;` 删掉（`title` 形参**保留但完全不用**）；参数 doc 标「W6 起完全不用」（`:1141-1143`） |
| `Rebuild()` | 跨重建不再带 `title`（`string title = null`，`:738`） |
| 断言 | **翻面**：`BattleScene.cs:8177-8179` 从「（现状）`SelectorTitle` 仍在」⇒ `selRoot.Find("SelectorTitle") == null`，与 `SelectorNote` 那条同形（★W6） |

- **验证**：`TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0（W6 后又跑一次，绿）。
  行尾仍全 CRLF：`AttackSelector.cs` 1886/0 · `BattleScene.cs` 45120/0；
  `git diff --numstat` = 371/118（AttackSelector）· 1310/134（BattleScene，比上级开工时 +80 行）。
  ⛔ 仍未跑 Unity ⇒ ★W6 那条断言**只过编译、没跑**。

### 顺手（本次允许范围内已做完）

- `资料/普查产出_1017/UI/R_A1024_字体按用途普查.md`：表后加一条 `> ⚠️ 2026-10-25 订正` 说明那四行
  `SelectorTitle`/`SelectorNote` 已过期；另一处汇总行（`:1003`）在该行末列内联补「**已删** + 新判据」。
- `资料/普查产出_1017/UI/W_A1024_对账.md:169`：该行末列内联补「⚠️ 2026-10-25 订正：这两件已删」。
- ⚠️ **`资料/普查产出_1023e/A23_战斗布局批.md:64`** 仍写「标题（`SelectorTitle`）/备注（`SelectorNote`）是我们自加的」——
  句子本身没写错（是在说「自加」），但**它描述的状态已不存在**（两件都删了）。那份**不在**本次允许改的范围内 ⇒ 只在此留痕，建议主对话补一句订正。

---

## W7（热修 · 真缺陷）· `Army != 0` 写反了 —— 已改回 `Army > 0`

- **症状（主对话实测）**：`BattleScene.Run` 连红 **9 条** —— 选择器里没有技能钮 / 进不了选目标 / 定不了打法 /
  技能卡面板不弹 / 技能打不中 / 不花行动 / A964 面板命中框…… 全部源于「主动技能那一格被整颗关掉」。
- **根因**：我把闸写成 `o.Army != 0` ⇒ **`Army == -1`（阵营认不出）也被送进「按军队查」** ⇒
  `ArmyIcons.ActiveAbilityIconName(-1)` → null ⇒ `tex == null` ⇒ `Hidden = true; Enabled = false`。
  而 `-1` 的来源是**自研两阵营 `Ember` / `Tide`**（`RuleEngine/Data/StarterCards.cs:28-29`；
  自检里的探针单位 `CardByName(StarterCards.Tide(), "Ballista")` 等全是这两个阵营）
  ⇒ `HasCommand(Ability)` 变 false ⇒ 战斗里**主动技能整颗钮消失**。
- **规格（HEAD 口径，白纸黑字）**：`BattleDriver.cs:13815-13817`：
  「`Id()` 认不出阵营时回 **-1** ⇒ `ApplyOptionIcons` 的 **`Army > 0`** 那道闸自然落回旧口径（按关键词 / 占位图）」。
- **改法（一行 + 注释/断言配套）**：
  - `AttackSelector.cs:1299` 闸改回 **`o.Army > 0`**；`want = ArmyIcons.ActiveAbilityIconName(o.Army)`（不再需要三元）。
  - `Debug.LogWarning` **保留，但只覆盖「军队 id 合法（>0）却取不到资源名/贴图」那一档** ——
    这正是原版 `iconSprite == null` 要关钮的那一档；`0` / `-1` 两档**不报警**（它们不是缺陷）。
  - 方法 doc 重写（`:1267-1280`）：写清 `<=0` 的两档语义 + 把这次写反的代价留痕（「⛔ 别再把闸放宽到 `!= 0`」）。
  - 断言块重写（`BattleScene.cs:8182-8248`）：★W2② 负例改用 **`Army = 20`（`CardArmy.Goff`，合法值但不在那 5 条里）**；
    另加 ★W2③ **闸的判别式**：`Army = -1` ⇒ 钮**在**、告警**不涨**（🧨 写回 `!= 0` ⇒ 这条红 —— 正是这次的缺陷）。
- **验证**：`TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0。
  行尾仍全 CRLF：`AttackSelector.cs` 1897/0 · `BattleScene.cs` 45265/0。
  ⛔ 按红线**没跑 Unity** ⇒ **需主对话复跑 `BattleScene.Run`** 确认那 9 条红转绿。

### ⚠️ 冲突告警（必须转达）

改写期间 `Editor/BattleScene.cs` **被别的代理同时写**：文件里出现了执行代理 `C`（`:29486`、`:39000`、`:39102`、`:40801`、`:43099` 一带）、
`R`（`:31697`、`:38552`、`:39348-39422` 一带）、审查代理 `AU4`（`:13992`）的 2026-10-25 段落，
文件在一个编辑回合内从 45,120 行涨到 45,265 行（我的改动只占约 +46 行）。
—— **一个文件两个写手 = 互相覆盖且不报错**（铁律 13·3）。本次我的 9 处改动（5 处 `IconTriggerForTest`、
1 条圈的 `EventTrigger` 结构断言、W2①②③、W4/W6 两条 `Find(...) == null`）**逐条复查仍在、无重复**，
typecheck 也是绿，但这属于运气好；**建议主对话把这几个代理的 `BattleScene.cs` 改动改成串行或指定唯一写手**。
（另：因为别人的插入，我报告里 `BattleScene.cs` 的行号会**继续漂**，以 `grep "★ W2"` / `IconTriggerForTest` 定位为准。）

---

## W8（追加 · `BattleScene` 最后 1 红）· 删掉「技能 - 选择目标 (N)」这条自拼提示

- **现场**：`BattleScene.Run` **5289 / 5290**，唯一红是
  `★ HUD 上的字全部来自官方本地化表 / 数据（18 段，越界 1 段）：#13「技能 - 选择目标 (1)」`
  （断言在 `Editor/BattleScene.cs:4200` 那一族，扫 `HudLabelTextForTest`）。
- **来源**：`Battle/BattleDriver.cs` 的 `HighlightTargetsCore` 末尾（原 `:14513-14521`）——
  `SetHint(CardText.Phrase(what/*MELEE|RANGED|ABILITY*/) + " - " +
  (n > 0 ? Phrase("PICK A TARGET") + " (" + n + ")" : Phrase("NO LEGAL TARGET")))`。
- **判据（原版到底有没有这条提示）—— 三条都指向「没有」**：
  1. **反编译**：`d:/2/tools/decomp_full/BattleManager__ShowPotentialTargets.c` 整个函数只做一件事 ——
     遍历 `+0x470` 那串卡，逐卡 `BattleManager.IsValidAttackTarget / IsValidSpellTarget /
     IsValidActiveAbilityTarget` 通过就 `CardHighlight.ChangeState()`；**没有任何 TMP/文本写入**
     （`BattleManager__StopShowingPotentialTargets.c` 同理）。原版选完打法那一拍 =
     `AttackButtonClick` → `ChangeAttackType` → `HighlightSelectedAttackTypeButtons` →
     `TargetReticleController.SetAttackType` → `StopShowingPotentialTargets` → `ShowPotentialTargets`，
     **全是视觉态，没有一句提示**。
  2. **官方本地化表**：`terms/Battle/**` 共 156 键，**没有 `Battle/Phrase/*` 这一类**；
     `PickTarget` / `NoLegalTarget` / `Melee` / `Ranged` / `Ability` 这些键只存在于**我们手写的** `Core/Loc.cs:2772-2776`。
  3. **格式**：`" - "` 与 `" (" + n + ")"` 的拼法本身也是自造（原版 `Battle/HUD/TargetsAvailable = "{0} available"`
     是**技能卡面板**里的 `TargetsAvailableText`，不是提示条）。
  ⇒ 按「没有就删干净」：**整段删掉**（不是只保留官方键部分 —— 原版这一拍连提示条都不出现）。
- **改动**：
  - `Battle/BattleDriver.cs`（`HighlightTargetsCore` 末尾）：删那段 `if (!isPreview) { … SetHint(…) }`，
    原位留 9 行红字写清三条判据 + 「⛔ 别再把它加回来」（现 `:14513-14523`）。
  - `Battle/BattleDriver.cs` 新增自检读口 **`TipTextsForTest`**（`_tipQueue` 的拷贝，下标 0 = 正在显示那条，
    没用过 = 空数组不返 null；`:14987-14999`）。为什么必须有它：`HudLabelTextForTest` 扫的是**屏幕上正显示的**文字，
    提示条 1.7 s 就撤下了 ⇒ 要判「有没有自拟文案混进提示条」只能读真队列。
  - `Editor/BattleScene.cs` 新增 **★W8** 两条断言（`:8853-8881`，紧跟「选完打法」那一拍）：
    ① **判别式（灭自证）**：同一套判据必须抓得到被删的原串 `"技能 - 选择目标 (1)"`（🧨 判据恒 false ⇒ 红）；
    ② 断言那一刻 `driver.TipTextsForTest` 里**没有任何**自拟文案（判据 = 含 `" - "` / 选择目标 / PICK A TARGET /
       没有合法目标 / NO LEGAL TARGET）（🧨 把 `SetHint` 加回来 ⇒ 立刻出现「… - 选择目标 (N)」⇒ 红）。
- **验证**：`TMPDIR=/tmp/wf_w1 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
  行尾：`BattleDriver.cs` **CRLF 31147 / bareLF 0**（开工时 31131）、`BattleScene.cs` CRLF 46242 / 0；
  `git diff --numstat` = 1276/104（BattleDriver）· 2451/153（BattleScene）。
  ⛔ 未跑 Unity ⇒ **需主对话复跑 `BattleScene.Run`** 确认 5290/5290 全绿。
- **没动的地方（如实记）**：`Core/Loc.cs:2772-2776` 那 5 条 `Battle/Phrase/*` 现在**没有调用点**了，
  但 `Core/**` 不在我的允许范围 ⇒ 只留痕；建议主对话顺手删（它们本来就是自造键，留着容易被下一个人当「原版权威」用）。
  ⚠️ 也没动 `SetHint` 的「空串 = 清队列」那条**我们加的行为**（它有自己的 doc 与调用点，与本条无关）。

### ⚠️ 并发写手仍在（第二次告警）

这一轮 `Editor/BattleScene.cs` 又从 **45,265 → 46,242 行**（我只加了 ≈30 行），仍能看到
执行代理 `C`（`:41692` / `:45656`）、`R`（`:43990`）、`M1`（`:45384-45418`）的 2026-10-25 段落。
我的改动逐条复查仍在、无重复（`★ W8` 2 条 / `★ W2` 3 条 / `★ W4`·`★ W6` 2 条 / 5 处 `IconTriggerForTest`），
typecheck 绿；但**一个文件多写手迟早互相覆盖且不报错**（铁律 13·3）—— 请主对话收口时指定唯一写手或串行。
