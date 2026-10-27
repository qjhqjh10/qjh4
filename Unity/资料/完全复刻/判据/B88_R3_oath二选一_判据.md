# B88_R3 · 战术卡 oath「二选一」判据

> **代理**：R3（只读查判据 · 第八十八会话）
> **范围**：`UM110 Fall Back` / `UM116 No Mercy` / `Angel's Wrath` / `Catechism of Death`（池内 4 张战术 oath 卡）
> **上游**：`资料/完全复刻/报告/A10_oath付费闸.md`（已查清 `NeedsToChooseFromPool` 五道闸；§四 #5 点名「二选一 UI 没做」）
> **判据源**：🥇 `cpp2il/out_cpp2il_dev_ilspy` / `assetripper ExportedProject` → 🥈 `decomp_full/*.c` → 🥉 `assets_full/`（场景 JSON）
> ⛔ 没跑 Unity · 没动 git · 没改任何 `.cs` / 账 / 正本

---

## 一、变体 SO 的结构（资产路径 + 绑定字段）

### 1·1 结论

**两个 `RawCardScript` 资产（同名、同卡），不是「同一资产的两个 AbilityLogic」。** 一个带默认 trait `oath`（`0x4FB = 1275`），一个不带。

**它们怎么绑到「同一张卡」上**：
> `RawCardScript.choicesDefinition`（类型 **`TargetCriteria`**）→ `TargetCriteria.filterSpecificCards`（**`List<RawCardScript>`**，就是两个变体 SO 的直接 PPtr 列表）。

| 环节 | 字段 | 证据 |
|---|---|---|
| 卡上的选择定义 | `public TargetCriteria choicesDefinition;` | `assetripper.../Assembly-CSharp/RawCardScript.cs:193` |
| 变体列表 | `public List<RawCardScript> filterSpecificCards;` | `.../TargetCriteria.cs:87` |
| 上限 2 | `public int maxTargets;` | `.../TargetCriteria.cs:61` |
| 轴 = 选牌池 | `public TargetsAffected targetsAffected;`（`ChoiceResolution` = `0xB4` = **180**） | `.../TargetCriteria.cs:9` |
| 从选择解析里选 | `public bool selectFromChooseResolution;` | `.../TargetCriteria.cs:91` |

**`.c` 偏移（权威）**：
- `choicesDefinition` = **`RawCardScript + 0x2A0`**（`CardScript__NeedsToChooseFromPool.c:21/24` · `BattleManager__GetChoiceFullPool.c:28/35`）
- `filterSpecificCards` = **`TargetCriteria + 0x70`**（`BattleManager__GetChoiceFullPool.c:37`）—— ⚠️ **是 `TargetCriteria` 的字段，不是 `AbilityLogic` 的**（`AbilityLogic.cs` 里 `grep filterSpecificCards / maxTargets` = 0 命中；`choicesDefinition` 在 `RawCardScript.cs:193` 声明为 `TargetCriteria`）⇒ 回答「两张 SO 还是同一资产的两个 AbilityLogic」：**是两张 SO，绑定在 `TargetCriteria.filterSpecificCards` 上**
- `maxTargets` = **`+ 0x40`**（`BattleManager__GetChoiceOptions.c:42`）
- `targetsAffected` = **`+ 0x10`**（`CardScript__NeedsToChooseFromPool.c:24`）

**池怎么组**（`BattleManager__GetChoiceFullPool.c`）：
1. `:28` `choicesDefinition = card.RawCardScript + 0x2A0`
2. `:33` `AbilityLogic.GetTargets(manager, choicesDefinition, card, card, target, …)`
3. `:47` **若 `choicesDefinition.targetsAffected == 0xB4(180)`** ⇒ 池 = `choicesDefinition.filterSpecificCards`（`:37`）；若它为空 ⇒ 回退 `BattleManager + 0x308`（`:49`）
4. 否则（非 180）⇒ 池 = 第 2 步算出的**场上目标**
⇒ **oath 卡走的正是 180 这一支：池 = `filterSpecificCards` = 两个变体 SO。**

`BattleManager__GetChoiceOptions.c`：从池里按 `maxTargets`（`:42`）**随机抽不重复**（`:53 GetRandomInt`），不足再补，最后排序（`:113`，`param_4=0` 时）。
⇒ maxTargets = 2、池 = 2 ⇒ **两张都摆出来**（不是随机抽两张）。

### 1·2 本机数据（我们已有）

| 卡（`cards_engine.json`） | id | 类型/费 | oath 值 | `choicesSpecificCardIds`（平/誓） |
|---|---|---|---|---|
| Fall Back | `UM110` | tactic(20) / 1 | 2 | `cd053fb3…`(plain) · `fe6fde3d…`(oath) |
| No Mercy | `UM116` | tactic(20) / 4 | 4 | `7b703582…`(plain) · `6ea114bc…`(oath) |
| Angel's Wrath | `4de4fcfb…` | tactic(20) / 1 | 2 | `d0516815…`(plain) · `97a4b83a…`(oath) |
| Catechism of Death | `8482077f…` | tactic(20) / 0 | 3 | `74480125…`(plain) · `0d2874f3…`(oath) |

**🔴 关键结构差**：原版是「两张 SO」；我们池里**只有一张合并卡**（`UM110` 等），oath 效果直接并进 `desc`（`"… Oath 2: Lower its cost by 2 and give it Flank."`），**两个变体 id 只是元数据、都不在池内**（`cards_engine.json` 里查这 8 个变体 id，全 `False`）。
- 变体 SO 的真身在 `cards_truth.json`（1,351 个 SO）：四张卡的平/誓两版都在（如 `UM110` oath + `cd053fb3…` plain）。
- SO 资产定位：`RawCardScript.cs` 的 `m_Script` guid = **`545e83c355d1cfe1386cc0149a3530b4`**；两个变体是 `RawCardScript` MonoBehaviour，靠 `filterSpecificCards` 的 **PPtr** 互相绑定（**不是**靠 uniqueId 字符串）。

---

## 二、二选一 UI（节点名 / rect / 字号 / 图标，逐项带出处）

### 2·1 组件 = `ChooseCardMenu`（一个通用「选一张」弹层，不是两个按钮）

- 类：`assetripper.../Assembly-CSharp/ChooseCardMenu.cs`（`MonoBehaviour`）。
- 每个战场场景各放一份：`assets_full/bundle_scenes_scenes_battlearena1/GameObject/ChooseCardMenu.json`（`m_IsActive:false`）+ `ChooseCardMenuAnchor.json`。
- 打开它的调用点：`BattleManager__ChoiceOfCardPlayer.c:50-51` → `ChooseCardMenu__Setup(...)`。
- 字段（`battlearena1/MonoBehaviour/MonoBehaviour_4353.json`）：`chooseMenuAnchor`(CardsHorizontalLayout 4052) · `chooseButtonText`(3994) · `chooseButton`(4897) · `titleTextLocalize`(4871) · `titleTextField`(709) · `buttonsGroup`(560) · `selectCardSound` · `selectCardButtonFrame`。
- 词条：`Battle/ChooseCard/Instructions`（标题，TMP 原文 `Choose one card`）· `Battle/Mulligan/ButtonDone`（确认钮）· `Battle/Prebattle/SelectButton`（每张卡的钮，`Select`）。

### 2·2 层级与 rect（battlearena1 实读；兄弟下标按父 `m_Children` 序）

```
ChooseCardMenu (RectTransform 3273) anchor(0,0)-(1,1) sizeDelta(0,0) = 整屏 1920×1080
├─ ChooseCardMenuAnchor (xform 1256, 裸 Transform)  = CardsHorizontalLayout(4052)
├─ ButtonsGroup (xform 3448) anchor(0,0)-(1,1)
│   ├─ HideChooseButton (3051) pos(143.4,133.2) size(78.4,78.4)   Image PA=1
│   └─ Continue Button (3196) pos(-396.4,105.0) size(548.0,75.9)  anchor(1,0)
│       ├─ BG (3288)             size(577.5,63.8)
│       ├─ ContinueText (2741)   TMP m_text="Continue" m_fontSize=45
│       └─ ContinueButton (3324) size(80.5,79.6)  Image(箭头图标)
└─ ChooseText (3465) pos(7,-106.5) size(1344,79.4) anchor(0.5,1)  Image disabled(色 1,0.494,0.945)
    └─ Text (2795)               TMP m_text="Choose one card" m_fontSize=55 (base 36) + Localize
```

### 2·3 排布参数（`CardsHorizontalLayout` MB 4052）

| 字段 | 值 | 归属 |
|---|---|---|
| `m_betweenElementsSpacing` | **502.2** | 卡间距（布局单位） |
| `m_maxLayoutSize` / `_SmallScreen` | **0.98 / 0.98** | 最大宽度占比 |
| `m_numberOfCardsForMaxHeight` | **7** | 高度自适应基数 |
| `m_useViewportSize` | **1** | |
| `m_scale` | **183.41** | ⚠️ 与 `CreateCardUI` 里 `+0x78` 是否同字段未证（§六） |
| `m_yAxisCurve` | (0,0)→(1,0) | 无纵向弧 |
| `m_rotationModifierBasedOnTotalCards` | 单点 (0,0) | 无旋转（**不是扇形**） |

摆位算法：`ChooseCardMenu__CreateCardUI.c` → `CardsHorizontalLayout.GetPosition(anchor, index, total, …)` 逐张算位置，卡体从 addressable 实例化（`Card2DController`/`BasicCardUI`）。

### 2·4 每个选项长什么样

- **一张 2D 卡**（`Card2DController`，可点开大卡：`ToggleOpenCardDisplayOnTouch(true)`）
- 卡上叠 **`SelectCardButtonFrame`**（prefab `CardChooseCardButtonFrame`：`CardChooseCardButtonFrame` > `Generic Simplified UI Button_updated` > `Button Text`，词条 `Battle/Prebattle/SelectButton` = `Select`）——见 `ChooseCardMenu._SetupChooseCardsUi_d__22__MoveNext.c`（`SelectCardButtonFrame.Initialize` + `Card2DController.ToggleHighlight`）。

⇒ **是「一排卡（本卡 2 张）+ 每张下方一颗 Select + 顶部标题 + 右下 Continue + 左上 Hide(眼睛)」，不是两个按钮、也不是扇形。**

### 2·5 我们已经有了这一层（不用重建）

`CardPresentation/Battle/ChoosePanel.cs:110`（`ChoosePanel : CardChoicePanel`）已逐项复刻：
- 布局常量 `TitleCx=967 / TitleCy=106.5 / TitleW=1344 / TitleH=79.44 / TitlePx=55`、`CardRowCy=511`、`Confirm(1519,980.26)`、`Eye(143.40,946.83)`、`Bar(1588.80,980.75)`（`:152` 起）
- 词条 `TitleTerm="Battle/ChooseCard/Instructions"`（`:120`）· `ConfirmLabel="Battle/Mulligan/ButtonDone"`（`:124`）· `SelectTerm="Battle/Prebattle/SelectButton"`（`:150`）
- 打开口 `ChoosePanel.Open(IReadOnlyList<CardView>, title, uniqueId)`（`:218`），单选 `PickMode.Single`。

⇒ **缺口只在「引擎侧没发这条 ask」+「扣费时机/可选性」，UI 不动。**

---

## 三、时机（选择与扣费/结算的先后）

### 3·1 结论

**基础卡费在「选择之前」扣；誓约费在「选择之后」扣（结算被选中变体时）。效果也在选择之后结算。**

### 3·2 逐跳（`.c` 行号）

| # | 做什么 | 证据 |
|---|---|---|
| 1 | 战术卡（`cardType 20/40`）**不**需要选小兵目标 ⇒ `NeedToChooseMinionTarget=false` | `BattleManager__NeedToChooseMinionTarget.c:7-16`（只对 `cardType==0` 且 targeted 且非 tutorial 才 true） |
| 2 | **先扣基础卡费** `PayCardCost`（`UseMana(CurrentCost)`） | `BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c:377`（有目标支的重复块在 `:1075`） |
| 3 | **再问 oath 池** `NeedsToChooseFromPool` | `_d__447:498`（无目标） / `:1148`（有目标、选目标动画之后）；主动技 `_d__479:249/258` |
| 4 | 命中 ⇒ 起 `ChooseCardMethod` 协程 | `_d__447:607` / `:1248` · `_d__479:367` |
| 5 | 玩家侧 → 开 `ChooseCardMenu` UI | `BattleManager__ChoiceOfCardPlayer.c:27/28/50-51` |
| 6 | **选择之后**才结算卡面效果 | `CardScript__TargetedSpellPlayed` / `NonTargetSpellPlayed`（`_d__447:1263` / `:633`） |
| 7 | **誓约费** 在结算被选中变体的能力时收 | `AbilityLogic__PlayAbility.c:2031` 判 `HasDefaultTrait(0x4fb)` → `:2033 PayCardPlayedCostOath`；`PayCardPlayedCostOath` = `PlayerManager.UseMana(GetCurrentTraitValue(0x4fb))`（`BattleManager__PayCardPlayedCostOath.c`） |
| 8 | 主动技 oath 同形 | `CardScript__UsedActiveAbility.c:44` → `PayActiveAbilityCostOath` |

### 3·3 可支付闸（决定「摆不摆 oath 选项」）

- 出牌时：`NeedsToChooseFromPool.c:142-147` `GetManaLeft >= GetDefaultTraitValue(0x4fb)` —— **此时基础费已扣** ⇒ 等价「总法力 ≥ 卡费 + 誓约」。
- 手牌预判：`CardScript__CanPlayWithOathNowInHand.c:31-36` `CurrentCost + oathValue <= ManaLeft`。
- 被 jam（`0x82`）⇒ 不问（`:133-134`）。

---

## 四、我们可复用的口（`文件:行号`）

| 能力 | 口 | 位置 |
|---|---|---|
| 这张卡要问哪几处 | `RuleCore.PlayerChooseOps(CardDef)` | `RuleEngine/Core/EffectResolver.cs:5230` |
| 上面 + 原版选池闸 | `RuleCore.PlayerChooseOps(BattleContext,int,CardDef)` | `EffectResolver.cs:17574` |
| 选池闸（五门） | `RuleCore.NeedsToChooseFromPool(...)` | `EffectResolver.cs:17463` |
| 有无 oath trait | `RuleCore.HasOathTrait(CardDef, out why)` | `EffectResolver.cs:17435` |
| 在场付得起誓约 | `CanPlayWithOathNowInPlay(...)` | `EffectResolverPartial_B88E2.cs:197` |
| oath 正文/费用 | `CardDef.OathOps` / `OathCost` / `CollectOathOps` | `Core/CardDef.cs:4605/4608/4610` |
| 主动技 oath 结算（自动付） | `ResolveOathAbility(...)` | `EffectResolver.cs:9095` |
| 按组收费（自动付的根） | `ChargePaidGroups(...)` | `EffectResolver.cs:732` |
| 战术结算入口 | `PlayTactic` / `PlayTacticBodyDeferred` | `EffectResolver.cs:2494 / 2709` |
| 出牌 ask 表 | `BattleDriver.PlayAsks`（`PlayerChooseOps(Ctx,_me,card)`） | `Battle/BattleDriver.cs:~10970`（`:10974`） |
| 主动技 ask 表 | `AbilityAsks` | `BattleDriver.cs:11033` |
| 摆面板 | `ShowAsk` | `BattleDriver.cs:11173` |
| 选牌候选（引擎入队的真卡） | `_askCands` | `BattleDriver.cs:11221` |
| 收面板答案 | `OnChooseDone` → `Ctx.ChooseCardIds.Enqueue` | `BattleDriver.cs:11318 / 11329` |
| 面板本体（复用） | `ChoosePanel` / `CardChoicePanel` | `Battle/ChoosePanel.cs:110` / `CardChoicePanel.cs:131`；`Open` @218 |
| 答案队列 | `ChoosePicks` / `ChooseCardIds` / `ChooseSites` / `ChooseAnswered` / `ResetChoices` | `Core/BattleContext.cs:1457 / 1476 / 1479 / 1481 / 1484` |

---

## 五、施工面（文件 → 做什么）+ 断言清单

### 5·0 关键设计前提（**必须先定**）

我们池里是**一张合并卡**（`UM110`，oath 效果在 `desc` 里）+ 两个「变体 id」元数据（不在池内）。
⇒ **不能照原版「选哪个 CardDef」直接落地**（plain 变体不是可打出的 `CardDef`）。两条路：

- **A（推荐，不动数据）**：保留合并卡，加一个**布尔 ask「付不付誓约」**；选 plain ⇒ 跳过 `CostKind=="oath"` 的 op 组，选 oath ⇒ 照旧。
- **B（不推荐）**：把两个变体 SO 塞进池、按两个 `CardDef` 选 —— 会动 `cards_engine` / 池口径 / 卡号，风险大、面广。

以下按 **A** 写施工面。

### 5·1 文件 → 做什么

| 文件（独占写手） | 做什么 |
|---|---|
| `RuleEngine/Core/EffectResolver.cs` | ① 新增 `RuleCore.OathChoiceNeeded(CardDef)`（判据 = `choicesTargetsAffected==180` ∧ `HasOathTrait` ∧ `choicesSpecificCardIds.Count==2`）② `PlayerChooseOps(ctx,…)` 在其为真时**注入一条新 ask op**（新 Verb，如 `"chooseoath"`；`PlayerChooseOps(CardDef)` 一参版**不动**，避免影响 `AskScopesForTest`）③ `PlayTacticBodyDeferred` 新增入参 `bool oathChosen`，`!oathChosen` 时过滤掉 `CostKind=="oath"` 的 op 及其 `CostShared` 组 ④ `CanPlayTactic`：oathChosen 时按 `CostOf + OathCost` 判余额（对齐 `CanPlayWithOathNowInHand`） |
| `RuleEngine/Core/BattleContext.cs` | 新增 `Queue<bool> ChooseOath`（与 `ChoosePicks`/`ChooseCardIds` 同序同位；`ResetChoices` 里清）—— 或复用一个专用 `ChooseCardIds` 槽存 `"oath"/"plain"`（若复用要写清语义，⛔ 别混用） |
| `CardPresentation/Battle/BattleDriver.cs` | ① `ShowAsk` 识别新 ask：用现有 `MakeChoiceCard` 合成两张（plain / oath，标题如 `Fall Back` / `Fall Back（Oath 2）`），`_choosePanel.Open(views, "Choose one card", card.Id)` ② `OnChooseDone` 把「oath/plain」排进 `Ctx` 的新队列 ③ `DoPlay`/`DoResolve` 把它传给 `PlayTactic(..., oathChosen)` ④ `PlayAsks` 只让战术卡走这条；主动技那一档另说（见 §六） |
| `RuleEngine/Editor/RuleEngineTest.cs` | 引擎断言（宿主 `RuleEngineTest.Run`） |
| `CardPresentation/Editor/BattleScene.cs` | UI 断言（宿主 `BattleScene.Run`） |

⚠️ **一行一个写手**；`EffectResolver.cs` 与 `BattleDriver.cs` 是**两个写手**（互不相交）。

### 5·2 断言清单（宿主）

**`RuleEngineTest.Run`（引擎）**
1. `UM110`，能量 = 卡费+誓约，选 **oath** ⇒ 能量 −(卡费+誓约) 且 oath 效果生效（Flank / 降费）。
2. 同上，选 **plain** ⇒ 能量只 −卡费，oath 效果**不生效**。
3. `UM110`，能量 = 卡费（不够誓约）⇒ `NeedsToChooseFromPool` = false ⇒ **只给 plain**（oath 选项不出现）。
4. `CanPlayTactic`：`oathChosen=true` 且余额 < 卡费+誓约 ⇒ `ErrCost`。
5. 非 oath 的 `choicesDefinition` 卡（`UM97 Suppressor` 等）⇒ 行为不变（回归，`AskScopesForTest` 不变）。

**`BattleScene.Run`（UI）**
6. oath 战术牌打出时 `ChooseOptionCount == 2`。
7. 面板标题 == `ChoosePanel.DefaultTitle`（`Choose one card`）。
8. 点第 2 张（oath）⇒ 引擎按 oath 结算；点第 1 张 ⇒ 不扣誓约。

⇒ **约 8 条（2 个宿主：`RuleEngineTest.Run` 5 条 · `BattleScene.Run` 3 条）。**

---

## 六、没查清的部分（如实）

| # | 没查清的 | 现状 |
|---|---|---|
| 1 | `AbilityLogic.PlayAbility` 里 `iVar8==0x168`（=360）究竟是哪条 ability code、为什么 oath 变体的能力码是它 | 只读到偏移/常量（`AbilityLogic__PlayAbility.c:1994`），未解枚举语义；需要 VA/`DefinedAbility` 枚举确认 |
| 2 | `BattleManager + 0x308`（`filterSpecificCards` 空时的回退池）语义 | `GetChoiceFullPool.c:49` 读到偏移，未解；对 oath 卡不走这条（影响 0） |
| 3 | `HideChooseButton` / `ContinueButton` 两张图标的**资源名** | 外链 `m_FileID=8`（场景 externals 未解映射）；只有 pathID（`-7255197835733746773` / `9056225148749506697`） |
| 4 | `CardsHorizontalLayout.m_scale=183.41` 与 `CreateCardUI` 里读的 `+0x78` 是否同一字段 | 未逐条核 MonoBehaviour 偏移↔序列化字段 |
| 5 | 原版卡面**是否显示 Oath 徽标/角标** | `BattleCardUI__ShowOath.c` 存在但未读；卡面有没有 "Oath N" 字样未核 |
| 6 | 四张合并卡里「哪些 op 属于 oath 组」的边界 | 依赖 `EffectText.CostKindOf=="oath"` 解析，未逐张核（`desc` 里 `Oath N:` 后半句的 op 分组） |
| 7 | 主动技（场上 `Oath N:` 点按钮）是否也要走「二选一」 | 原版 `ChoiceOfCardPlayer` 也服务主动技（`_d__479`），但主动技的 oath 是「点技能时付」，不是「二选一」—— 需确认是否同一套 UI 或另一条 |
| 8 | 原版两个变体 SO 的卡面文案差异（plain 版 `desc` 是否只有前半句） | `cards_truth.json` 只存 trait，未读两版 `desc` 逐字 |

---

## 附 · 复现命令

```bash
# 结构（变体绑定）
grep -n "filterSpecificCards\|maxTargets\|targetsAffected\|choicesDefinition" \
  "d:/2/tools/assetripper_out/ExportedProject/Assets/Scripts/Assembly-CSharp/TargetCriteria.cs" \
  "d:/2/tools/assetripper_out/ExportedProject/Assets/Scripts/Assembly-CSharp/RawCardScript.cs"

# 时机（先扣费 → 后问 → 再付誓约）
grep -n "PayCardCost\|NeedsToChooseFromPool\|ChooseCardMethod" \
  "d:/2/tools/decomp_full/BattleManager._ResolvePlayCardFromHand_d__447__MoveNext.c"
grep -n "0x4fb\|PayCardPlayedCostOath" "d:/2/tools/decomp_full/AbilityLogic__PlayAbility.c"

# UI（battlearena1 场景）
python -I -X utf8 -c "import json,io;print(json.dumps(json.load(io.open('d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4353.json',encoding='utf-8')),ensure_ascii=False,indent=1))"
```

