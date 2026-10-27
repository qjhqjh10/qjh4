# B9_W13CDW —— K-15 四条（战斗界面 HUD 展示位与加载态描述）

账行：`HUD:750` · `HUD:755` · `HUD:756` · `HUD:758`
白名单：`CardPresentation/Battle/CardDisplayWindow.cs` · `RuleEngine/Core/GameplayVariables.cs`
执行代理：`B9_W13CDW` · 2026-10-27

---

## 一、结论

1. **四条现等级都还是 `C`**（派活包的「现等级」这一栏**没过期**），但**它写的「前置」过期了**：
   派活包说「**数值未读**：6 个展示偏移要先从解包实例取」—— **不成立**。见 §二·1。

2. **`HUD:750` / `756` / `758` 的载体不是放大窗**。原版这三个方法移动的是**棋盘/手牌上那张
   `CardScript` 本体**（`DOMove(transform, …)`、`DOScale(transform, …)`），而 `CardDisplayWindow` 是
   「卡牌放大窗」那棵树（`Card Display Window` / 5 个卡槽 / `m_LocalScale = 250`）——
   与账里 `B2AU`（2026-10-27）就地订正的那一条**一致**。⇒ `CardDisplayWindow.cs` **本笔零改动**
   （账原话：「⛔ 别再往放大窗里加第二套偏移」）。

3. 这三条真正的落点 = `CardPresentation/Battle/BattleDriver.cs` 的 reveal 链 / `DisplayEnemyCardInCenter`
   —— **别人的文件（W10）**。所以本笔交付的是**判据闭合 + 唯一一份公式 + 值表**
   （落在本代理独占的 `GameplayVariables.cs` 尾部的 `RuleEngine.VarsGlobal` 类里），**不动接线**。

4. **`HUD:755` 查出两处真判据错**（本笔只取证、不改代码 —— `SkillPanel.cs` 不在白名单）：
   ① 键前缀我们写的是 `Card_Description/`，**原版拼的是 `Card_Abilities/`**；
   ② 账上原记「`RemovingSiege` 那一支的后缀**没解出来**」—— **不成立**：ILSpy 那份把常量**内联**出来了，
      完整判据已拿到（键 = `Card_Abilities/<id>`，后缀为空串，再按 `[[reload]]` 闸剥 4 个变体）。

5. **代码只改一处**：`RuleEngine/Core/GameplayVariables.cs` **尾部**新增 `public static class VarsGlobal`
   （8 个原版实读常量 + 1 条公式 + 1 条 px 桥）。**零既有行为改动**：没动任何既有字段、没改任何方法体、
   没动任何已有行 ⇒ 该文件里既有的 `文件:行号` 落点**一个都不漂**（新增全在文件末、类外）。
   ⇒ 按简报「若动了 `GameplayVariables.cs` 的行为」这一条：**没动行为**，故**未跑** `ruleprobe.sh check` / `play check`
   （引擎侧此刻有别的代理在改 `EffectResolver` / `UnitState`，跑出来的红分不清是谁的）。

---

## 二、证据

### 二·1 「6 个展示偏移」其实是**引擎常量**，不是解包数据

两处反编译读的是 **同一个静态块** `DAT_1842da2a8`：

| 读法 | 偏移 | 含义 |
|---|---|---|
| `CardScript__GetEnemyCardPlayedDisplayPosition.c:66-70` | `+0x3c / +0x40 / +0x44` | 公式里的 `right` |
| 同上 `:48-62` / 另一处 `:77-81` | `+0x18 / +0x1c / +0x20` | 公式里的 `up` |

而 `DAT_1842da2a8` **就是 `UnityEngine.Vector3` 的 Il2CppClass**：

- 静态字段表 `d:/2/tools/il2cpp_out/dump.cs:537034-537039`（逐行现读：`zero 0x0 / one 0xC / up 0x18 / down 0x24 /`
  `left 0x30 / right 0x3C`）：`zero 0x0 / one 0xC / **up 0x18** / **right 0x3C**`
  ⇒ `+0x18` = `(0,1,0)`、`+0x3c` = `(1,0,0)`，**两个都是常量，不需要从任何实例读**。
- 类身份三条独立反证（不是推断）→ `资料/普查产出_1011/审查_W1.md:113`
  （`decomp_full/CustomTypes__DeserializeVector3.c:18,53` · `__SerializeVector3.c:19` · `NetworkingPeer__SendInstantiate.c:27,45,54`）；
  同族结论也早写进 `BattleDriver.cs` 那段 reveal 头注（`GetEnemyCardPlayedDisplayScale` 基准 = `Vector3.one`）。

⚠️ 派活包把「6 个**标量**」和「两档 y」混成一句「6 个展示偏移要先从解包实例取」——
**6 个 float 是这两个向量**（引擎常量），**真正的偏移字段只有 8 个**，且**早就读出来了**（下面 §二·2）。

### 二·2 偏移取值表（原版字段 → 值 → 我们的落点）

| 原版字段（`VarsGlobal` = 资产 `GlobalVariables`，PathID 451） | 偏移 | 原版值 | 用在哪 | 我们的落点（本笔新增，`RuleEngine.VarsGlobal`） | 换算成屏幕偏移 @1080p（**y 向下为正**） |
|---|---|---|---|---|---|
| `enemyCardDisplayScale` | `+0x48` | **1.4** | 展示缩放的**绝对 localScale** | `EnemyCardDisplayScale` | —（`BattleDriver.cs:25783` 已用同一值） |
| `enemyCardScaleMobileMultiplier` | `+0x4c` | **1.3** | 小屏再乘一次（合 1.82） | `EnemyCardScaleMobileMultiplier` | — |
| `enemyCardDisplayYoffset` | `+0x50` | **24** | 敌方牌 Y（公式里是**减**） | `EnemyCardDisplayYOffset` | **+356.1 px（向下）** |
| `enemyCardDisplayXoffset` | `+0x54` | **−26** | 敌方牌 X（桌面档） | `EnemyCardDisplayXOffset` | **−385.7 px** |
| `enemyCardDisplayMobileXoffset` | `+0x58` | **+25** | 敌方牌 X（小屏档）⚠️ **符号与桌面档相反** | `EnemyCardDisplayMobileXOffset` | +370.9 px |
| `offensiveCardDisplayYoffset` | `+0x5c` | **30** | 进攻牌 Y | `OffensiveCardDisplayYOffset` | **+445.1 px（向下）** |
| `offensiveCardDisplayXoffset` | `+0x60` | **−25** | 进攻牌 X（桌面档） | `OffensiveCardDisplayXOffset` | **−370.9 px** |
| `offensiveCardDisplayMobileXoffset` | `+0x64` | **−25** | 进攻牌 X（小屏档） | `OffensiveCardDisplayMobileXOffset` | −370.9 px |

- **值的来源**：`工具/read_varsglobal.py` 从 `Warpforge_Data/sharedassets0.assets` 的原始字节按声明顺序解出；
  整表 → `资料/VarsGlobal_原版数值.md:35-38`（`enemyCardDisplayScale 1.4` / `…Yoffset/Xoffset/MobileXoffset = 24 / −26 / 25` /
  `offensiveCardDisplayYoffset/Xoffset/MobileXoffset = 30 / −25 / −25`）。
  偏移 ↔ 字段名的对应另有两条**独立锚点**反证（`minionInPlayScale(+0x68) = 0.75` · `cardInHandMovingScale(+0x44) = 1.1`）
  → `资料/普查产出_1024/D6_进攻卡展示缩放.md` §二。
- **px 桥 = 14.835 px / 世界单位 @1080p**：战斗 HUD 那台 `Canvas` 是 **Screen Space - Camera**
  （`m_RenderMode = 1`，⚠️ `Overlay=0 / ScreenSpaceCamera=1 / WorldSpace=2`）、`m_PlaneDistance = 100`、UI 相机 FOV 40
  ⇒ `1080 / (2 × 100 × tan20°) = 14.835`；连带 `lossyScale(HandArea) = 108 × 1/14.835 = 7.28`。
  判据 → `资料/手牌布局_原版算法与参数.md` §四（单位桥）· 记忆 `warpforge-original-3d-size-bridge.md`。
- **为什么偏移按「世界单位」换算**：公式是 `enemyHand.transform.position + right·X − up·Y` —— `.position` 是**世界坐标**，
  加的是**世界方向的标量倍数**，与该对象的 `localScale` 无关（`HandArea` 是裸 `Transform`、`lossyScale 7.28`，
  但那只影响**它的子节点**，不影响 `.position` 那条加法）。
- 🔴 **符号（踩过一次，写死在这里）**：本仓的像素口径是**原版 UGUI 那一套：左上原点、`y 向下为正`**
  （`Core/LayoutSpace.cs:214` 头注；`FromPixel(x,y)` 直接吃「y 向下」）。而世界里是 `− up·Y`（往**下**）
  ⇒ 屏幕上 **`y` 增大**。所以 `PlayedCardDisplayOffsetPx` **原样返回 `+Y`**，
  敌方牌 = `(X −385.7, Y +356.1)`、进攻牌 = `(X −370.9, Y +445.1)`（@1080p，**y 向下**）。
  ⛔ 别在调用点再翻一次符号 —— 那会和 `RevealStartPx/EndPx`（同样喂给 `FromPixel`）的口径打架。

### 二·3 公式只有一份（两条方法逐行相同）

`decomp_full/CardScript__GetEnemyCardPlayedDisplayPosition.c:60-75` 与
`…/CardScript__GetOffensiveCardPlayedDisplayPosition.c:60-75` **除了读哪三个偏移字段，逐行相同**：

```
pos.x = hand.x + right.x·Xoff − up.x·Yoff      // right.x = 1, up.x = 0
pos.y = hand.y + right.y·Xoff − up.y·Yoff      // right.y = 0, up.y = 1  ⇒ 减 Y
pos.z = hand.z                                  // right/up 的 z 都是 0
```

唯一的差别：

| | X 偏移（桌面 / 小屏） | Y 偏移 |
|---|---|---|
| `HUD:756` 敌方牌 | `+0x54` (−26) / `+0x58` (**+25**) | `+0x50` (24) |
| `HUD:758` 进攻牌 | `+0x60` (−25) / `+0x64` (−25) | `+0x5c` (30) |

⇒ 已收成一处：`VarsGlobal.PlayedCardDisplayPosition(handAnchorWorld, offensive, smallScreen)`
（施工单 K-15 原话「抽一处，⛔ 别写两份」）。

**基线 = 敌方手牌锚**（两条都是）：`BattleManager.__get_globalVars` 里 `enemyHand(+0xD8)` 的 `transform.position`
（字段表 `dump.cs:30739-30741`：`playerHand // 0xD0` / `enemyHand // 0xD8`；
揭示卡本身也是 `battleCards.CreateCard(card, enemyHand.transform, …)` 建的 ⇒ 同一个父）。
⚠️ **进攻牌也用敌方手牌锚** —— 原版代码就是这么写的，不是笔误。

### 二·4 `HUD:750` 的时间轴（原版真体）

`decomp_full/CardScript._DisplayOffensiveCardInCenter_d__264__MoveNext.c`：
`SetCardVisibility(Back)` → `DOMove(GetOffensiveCardPlayedDisplayPosition(), 0.15, ease3)` →
`DORotate(zeroVector, 0.1)` → `WaitForSeconds(DAT_1834b2dc4 = 0.1)` → `DOScale(GetEnemyCardPlayedDisplayScale(), 0.3, ease3)` →
`WaitForSeconds(0.3 × 0.66 = 0.198)` → `DisplayCreatedByText(…)` + `ToggleHighlight(false)` →
`WaitForSeconds(0.3 × 2.6 = 0.78)` → 关「由谁创建」节点。
我们这一侧已按这套时间轴落地（`BattleDriver.cs:9822-9870` 那一段常量 + `RevealInnerState2At` / `RevealInnerState3At`）。

### 二·5 `HUD:755` 原版三档链（含新解出的 RemovingSiege）

`cpp2il/out_cpp2il_dev_ilspy/CardScript.cs:17102-17157`（与 `decomp_full/CardScript__GetCurrentLocalizedAbilityDescription.c` 逐句一致）：

```
if (DoesAbilityRequireLoading()) {
    if (!activeAbilityLoaded(+0x224))  return Desc("-load")  ?? ModifyLocalization(Loc("Card_Abilities/GenericSiegeText"));
    else                               return Desc("-effect") ?? Desc();
} else if (ability != null && ability.needsLoading(+0x22) && HasRemovedTrait(reload 0x1ea)) {
    return DescRemovingSiege();
} else                                 return Desc();
```

`RawCardScript.GetLocalizedAbilityDesc(suffix)`（`RawCardScript.cs:2374-2406`）：

- 键 = **`"Card_Abilities/" + (activeAbilityTextReferenceId ?? uniqueId) + suffix`** ← 🔴 **前缀是 `Card_Abilities/`**；
- 特例：`activeAbilityTextReferenceId` 含 `"(("` ⇒ **直接返回该字段原文**（不过本地化表，`:2384-2389`）；
- 缺键（`IsNullOrEmpty`）⇒ 调用方往下落。

`RawCardScript.GetLocalizedAbilityDescRemovingSiege()`（`RawCardScript.cs:2408-2446`）—— **原记「后缀没解出来」不成立**：

- 键 = `"Card_Abilities/" + (activeAbilityTextReferenceId ?? uniqueId)`（**后缀为空串**）；
- 过 `[[reload]]` 闸（`Contains`）后依次剥 4 个变体，**替换值都是空串**：
  `[[reload]]: ` → `[[reload]]:` → `[[reload]] : ` → `[[reload]] :`。

现 build 的官方本地化表（`Resources/official_loc_13lang.json`，10,862 键）实读：

- `Card_Description/*-load` / `*-effect`：**0 条**；`Card_Abilities/GenericSiegeText`：**0 条**
  ⇒ 三档今天全落**裸描述**（与账上「行为与改前逐字相同」一致）；
- `Card_Abilities/*` 共 **120 条**：90 条 id 式（其中 **86 条就是卡 id**，4 条不是：
  `SaviorProtocolsEffect` / `ExperimentalDronesEffect` / `SOR25_new` / `SOR30_new`）＋ **30 条 `Effect<名>` 式**
  ⇒ **至少 34 个键无法由卡 id 拼出**，只能来自 `activeAbilityTextReferenceId`（见 §三·2）。

我们这一侧的落地（`CardPresentation/Battle/SkillPanel.cs`）：

- 三档形状 + 缺键落法**已实现**：`AbilityDescription`（`:2133-2176`），入参由
  `BattleDriver.ShowSkillPanel`（`BattleDriver.cs:15584-15611`）喂 —— `requiresLoading` 的算法和原版
  `DoesAbilityRequireLoading` 对齐（除 §三·4 那个边界）。
- 🔴 **两处判据错**：① 键前缀 `Card_Description/`（`SkillPanel.cs:2142` / `:2156`，连同头注 `:2124` 那条也写着
  同一个错前缀）应改 **`Card_Abilities/`**；
  ② `RemovingSiege` 支没实现（`:2161-2172` 出声后退回裸描述）—— **现在有判据了，可以落地**。
- `CardText.EffectSentence`（`Core/CardText.cs:960-985`）拼的是 `CardEffect/Sentence/*` 模板，
  **不查 `Card_Abilities/<id>`** ⇒ 裸键那一档与原版不是同一条路（见 §三·2）。

---

## 三、没查清的部分

1. **`enemyHand` 的屏幕锚点**（这是 §二·2 那张表唯一还缺的一格，也是 `RevealStartPx/EndPx` 至今是 🔶 的原因）。
   - 运行时 dump（`资料/原版参照图/Unity参照管线_0825/data/panel_0914/runtime_ui_dump_drive.tsv:335,343`）里
     `UpperAnchor/EnemyArea/HandArea` 与 `HandAnchor` 都是**裸 `Transform`（`TF`）**，`rect` / `pos` 两列**空**
     ⇒ **拿不到**。
   - 还差什么：① 沿 `Safe area BackCanvas → UpperAnchor → EnemyArea` 的 `RectTransform` 父链把 `anchoredPosition`
     一路累加（这些在 `bundle_scenes_scenes_battlearena1/` 与 arena prefab 里有），再乘 Canvas 缩放
     `1/14.835`；或 ② **进原版跑一次** dump（本机可跑，见 `资料/命令速查.md`）直接读那台 `Transform` 的
     `position`。**本代理不跑 Unity / 不动 `d:/2`** ⇒ 停在这里，如实记。
   - 一旦拿到，绝对值 = `handAnchor_px + 上面那张表的 px 偏移`；`BattleManager__DisplayRevealedCard.c:61-63`
     那边的**起点 = `Vector3.zero`**（世界零），也要同样的换算（若 HUD 相机在 `(0,0,−planeDistance)` 看向原点，
     世界零 = 屏幕正中，但**这一点没核**，⛔ 不当结论用）。

2. **`activeAbilityTextReferenceId`（原版 `RawCardScript +0x248`）我们根本没有这个字段**
   （`RuleEngine/Resources/cards_engine.json` 的键表里没有，全表 1,128 张 §二·5 已核）。
   ⇒ 技能描述键的「id 那一半」在原版是 `(textReferenceId ?? uniqueId)`，我们只有后者。
   影响面**已量化**：120 个 `Card_Abilities/*` 键里 **≥34 个**拼不出来（30 个 `Effect<名>` + 4 个非卡 id）。
   要补：**数据侧**加这一列（来源 = 1,351 个卡 SO 的 `activeAbilityTextReferenceId`），不是表现层能猜的。

3. **`((` 短路分支的语义没查清**：`activeAbilityTextReferenceId.Contains("((")` 时原版**原样返回该字段**
   （`RawCardScript.cs:2384-2389`，`decomp_full` 同：`System_String__Contains(*(+0x248), DAT_18426a500)` →
   `goto LAB_1809478fe` = 直接 `ModifyLocalization(那个串)`）。那串是「内嵌富文本」还是「指向别的卡的引用」，
   没有第二个证据 ⇒ **写「没查清」**。今天不影响（我们没有该字段）。

4. **`DoesAbilityRequireLoading` 的边界**：原版把 `HasRemovedTrait(reload)` 放在
   **`activeEffects(+0x108)` 列表的循环体里**（`CardScript__DoesAbilityRequireLoading.c:38-45`）⇒
   **列表为空时一次都不查**，直接返回 `ability.needsLoading`。我们是**无条件** `&& !reloadRemoved`
   （`BattleDriver.cs:15599-15601`）⇒ 「`activeEffects` 为空 ∧ 已移除 reload」这一格两者不同。
   今天**不可观测**（`ActiveAbilityNeedsLoading` 现读 1,351 张**全 0** ⇒ `requiresLoading` 恒假），
   但**照抄原版应该把那道闸挪进循环等价位置**（= 仅当 `activeEffects.Count > 0` 时才看 reload）。

---

## 四、改动清单

| 文件 | 改动 | 行数影响 |
|---|---|---|
| `Unity/MyGame/Assets/RuleEngine/Core/GameplayVariables.cs` | **文件尾**新增 `public static class RuleEngine.VarsGlobal`：8 个原版实读常量 + `PlayedCardDisplayPosition(...)`（唯一一份公式）+ `PlayedCardDisplayOffsetPx(...)`（14.835 px/单位桥） | 全在文件末、`GameplayVariables` 类**闭合之后** ⇒ 既有 `文件:行号` 落点**零漂移** |
| `Unity/资料/完全复刻/账/_parts/B9_W13CDW.tsv` | 4 行补丁（11 列 · 带表头 · `方法` 列是完整签名） | — |
| `Unity/资料/完全复刻/账/_parts/_gen_B9_W13CDW.py` | 生成上面那份补丁的脚本（可复跑、按 `(原版类, 方法)` 从主表取行；放 `_parts/` 与既有 `_gen_B9_*.py` 同一约定） | — |
| `Unity/资料/完全复刻/报告/B9_W13CDW_展示位与加载态.md` | 本报告 | — |

**⛔ 明确没动**：

- `CardPresentation/Battle/CardDisplayWindow.cs` —— 账里 `B2AU`（2026-10-27）明写「这不是它的载体、
  ⛔ 别再往放大窗里加第二套偏移」；本笔核过放大窗那棵树（`CardDisplayWindow.cs:1-46` 的头注：5 个卡槽
  `m_LocalScale = 250`、`Card Display` 固定居中 752×868）⇒ **确认无关**，**一个字节都没改**。
- `Battle/BattleDriver.cs`（W10）· `Battle/SkillPanel.cs`（不在白名单）· 任何 `*.unity` 场景 · 账主表 · 两张正本。

**账补丁自检**：`python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry --only B9_W13CDW`
⇒ `合计：更新 4 · 新增 0 · 无变化 0（dry-run，未写盘）`。

**类型检查**（`TMPDIR=/tmp/wf_w13 bash d:/4/Unity/工具/typecheck.sh`，收工前最后一跑）：**运行时 0 错 / 编辑器 1 错**；
那 1 条在 `CardPresentation/Editor/BattleScene.cs:24176`（`W11ShakeAssert` 找不到 —— **W11 正在写那个文件**）。
本代理的两个文件（`GameplayVariables.cs` / `CardDisplayWindow.cs`）**全程 0 条错误**。
（中途几跑时全仓曾有 **运行时 11 / 编辑器 62** 条，全部落在别人的在飞文件：`Shell/DraftModeWindow.cs`(W8) ·
`Editor/BattleVfxAssertE3.cs` · `RuleEngine/Editor/RuleEngineTest_*.cs` · `Editor/TraitEffectAssert.cs` —— 数字每轮都在变。）
按 `CLAUDE.md` §13·3 的口径：那些不是本代理的活，⛔ 没去改别人的文件。

---

## 五、顺手发现

1. **派活包「现等级」栏这次没过期，但「前置」栏过期**（⇒ 建议调度台更新 K-15 的 §2 前置那句）：
   「6 个展示偏移要先从解包实例取」**错**（引擎常量）；真正的 8 个偏移值 2026-09-17 就已经在
   `资料/VarsGlobal_原版数值.md:35-38` 了。**这条会让后续代理白跑一趟读实例**。

2. **同一件事在三处口径不一**（建议以本报告 §二·1 为准）：
   - 派活包 §2 K-15：把 `+0x18/+0x1c/+0x20/+0x3c/+0x40/+0x44` 当「要先从解包实例读的 6 个标量」；
   - 账 `HUD:756` 差异列：写对了「`right/up` 取自静态块 `+0x3c / +0x18`」，但没点明那就是 `Vector3`；
   - `BattleDriver.cs` 的 `GetEnemyCardPlayedDisplayScale` 头注：早就写对了（`DAT_1842da2a8` = `Vector3`）——
     **新结论只需从那里引用，不必再查**。

3. **账 `HUD:755` 备注里那句「`Card_Description/*` 那一族在官方全表里 ⇒ 拿 `HasEntry` 会恒 false」只对裸键成立**：
   `Card_Description/*` 有 1,261 条，但 **`-load` / `-effect` 后缀键 0 条**，而且原版**整个族的名字是 `Card_Abilities/`**
   ⇒ 那句注释建议连同键前缀一起订正（本笔已在补丁的「差异/备注」里写清，代码留给 `SkillPanel` 的写手）。

4. `CardDisplayWindow` 的注释里有一处**容易误导后来人**的地方（本笔**没改**，只记）：
   `CardDisplayWindow.Show(..., faceDown)` 的 doc（`:1213-1221`）说那个参数是「把战斗里那条
   `CardScript._DisplayEnemyCardInCenter` 的翻面搬到我们的载体上」—— 那句话容易被读成
   「放大窗 = 中央展示位」，而实际上 `DisplayEnemyCardInCenter` 用的载体是
   `BattleDriver` 里 `CardView.Create(hudRoot, …)` 建的**临时卡**（`BattleDriver.cs:25850-25870`）。
   ⇒ 建议 `CardDisplayWindow` 的写手在那段 doc 里补一句「本窗**不是**展示位公式的载体」。

---

## 六、交接给下一个代理（若需要继续）

- **下一步能做的两件**（都**不在本代理白名单**）：
  ① `BattleDriver`（W10）：把 `DisplayEnemyCardInCenter` 的终点从「固定屏幕正中」改成
     `RuleEngine.VarsGlobal.PlayedCardDisplayOffsetPx(offensive: false, smallScreen: false, screenHeightPx)` 加到
     **敌方手牌锚的屏幕点**上（锚点见 §三·1）；reveal 链同法用 `offensive: true`。
  ② `SkillPanel`（其写手）：键前缀 → `Card_Abilities/`；`RemovingSiege` 支按 §二·5 的 4 个 `Replace` 实现；
     裸键档改成「先查 `Card_Abilities/<id>`，缺键再回 `CardText.EffectSentence`」。
- **不要做的**：把偏移加进 `CardDisplayWindow`；把 `Vector3.up/right` 当数据去解包资源里找；
  给 `VarsGlobal` 的常量改名（账里已经引用它们了）。
