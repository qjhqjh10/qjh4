# B117-G10-13 · 卡牌详情窗 · Tooltip（战斗域第 13 部分）

> 执行代理 `B117-G10-13` · 2026-11-03 · 判据全部**现读**（`decomp_full` / `assets_full` / 本地资源），⛔ 无一条从我们的常量反推。
> 范围：`Battle/CardDisplayWindow.cs` · `Core/Tooltip.cs` · `Core/RelatedCards.cs` · `Core/CardPreview.cs` · `Effects/CardDisplayLevelUp*`。
> 结论一句话：**这一部分基本已经复刻完成**，本批只查出 **1 处真缺陷**（语音钮可见性被覆盖）并当场修好；其余 15 项逐值对账**全部一致**。

---

## 一、本批改了什么（1 处代码 + 1 处文档订正）

| # | 文件:行号 | 改法 | 判据 |
|---|---|---|---|
| 1 | `Battle/CardDisplayWindow.cs:1023` | `SetChrome(bool on)` 里 `if (_voiceBtn != null) _voiceBtn.gameObject.SetActive(on);` → `if (_voiceBtn != null && !on) _voiceBtn.gameObject.SetActive(false);`（**2 行 → 2 行，行数守恒**） | `decomp_full/CardDisplayWindow__SetCardVoiceOver.c:60-66`：语音钮的唯一写者是 `SetActive(voiceOverButton, 0 < availableSounds.Count)`；`Open.c` / `Close.c` 全篇**没有一处**按「窗开没开」动它 |
| 2 | `Core/Tooltip.cs:23-25` | 文件头 ①「原版走远端 I2、本地只有 key」**就地订正**（3 行 → 3 行），指向文件内已有的 `T17` 订正段 | 官方 I2 表 2026-10-14 已在本地（`Resources/official_loc_13lang.json` 10,862 键）；`TipText` 取的就是官方两列，**只剩 3 条自拟** |

### 缺陷 1 的症状（改前）

`Show()` 的顺序是 `RefreshButtons()`（`:1289`，按卡算「这张有没有语音」→ `CardButtons.HasVoice`）**然后** `SetChrome(true)`（`:1291`）。
`SetChrome` 里那句 `SetActive(on)` 无条件点亮 ⇒ **把上一句的结果整个盖掉**。
⇒ 没有语音的卡（现读语音覆盖：hero 50/53 · unit 549/656 · **tactic 4/484**）开窗后都会**多出一颗按了没反应的语音钮**。
`HideCore` 那一侧（`SetChrome(false)`）行为不变；伏击支 `ShowBattleCard:1929` 的显式关也没受影响。

---

## 二、差异表（元素 | 原版值 + 出处 | 我们 `文件:行号` | 差 | 改法）

### ① `LowerSection` 1320×137 里那几件（位置与显隐）

| 元素 | 原版值 + 出处 | 我们 | 差 | 改法 |
|---|---|---|---|---|
| `LowerSection` 矩形 | `RectTransform_3533.json`：`m_SizeDelta (1320, 137)` · 锚/轴心 `(0.5,0.5)`/`(0.5,0)` · `ap (0, −518.34)` | `CardWinBox.cs:25` `300/921.34/1620/1058.34`（= 1320×137，中心 −449.84） | **无** | — |
| `LowerSection` 的孩子 | **只有 2 个**：`2753`=FlavourTextBG、`3221`=Voice Over Button | `CardDisplayWindow.cs:631-634`（`_lower` 下 `_bg`）+ `:662`（`_voiceBtn`） | **无**（战斗版**没有**关闭钮、**没有** `Show Card Text` 眼睛钮） | — |
| `FlavourTextBG` | `RectTransform_2753.json`：`m_SizeDelta (1320, 178)` · `ap (0,0)`（相对 `LowerSection` 中心） | `CardWinBox.cs:27` `LoreBgT 900.84 / LoreBgB 1078.84`（= 178 高、中心 = LowerSection 中心） | **无** | — |
| 底板贴图 | `FlavourTextSO.GetClanFlavorBackground(clanId)`（**按阵营**取图；`SetCardLore.c:24-27` 无文本 ⇒ 整块 `SetActive(false)`） | `CardDisplayWindow.cs:1720-1735`（按 `faction` 取 `flavourbg_<faction>.png`）+ `RefreshLore` 按「有没有 lore」开关 | **无** | — |
| 语音钮矩形 | `RectTransform_3221.json`：`m_SizeDelta (88.655, 88.655)` · `ap (734.33, 0.0001831)` · `m_Pivot (0.5,0.5)` | `CardWinBox.cs:48/50` `BtnS 88.655` · `BtnCy 989.84` · `VoiceCx 1694.33`（= 960+734.33，y 中心 989.84） | **无** | — |
| 语音钮**显隐** | `SetCardVoiceOver`：`SetActive(0 < sounds.Count)`（+ 两个远端开关位，本地无判据） | `CardDisplayWindow.cs:1806` `CardButtons.HasVoice(_shown)` → `:1809` | 🔴 **改前被 `SetChrome(true)` 覆盖** | ✅ **本批已修**（见 §一） |
| 关闭钮 | **原版没有**（`LowerSection` 只有 2 个孩子；全窗也没有 `Close` 节点） | 不建 | **无** | — |
| 眼睛钮（`Show Card Text`） | 战斗版 `showCardTextButton = {m_PathID:0}`（13/13） | 不建（`CardDisplayWindow.cs:650-651`） | **无** | — |

> ⚠️ **「语音钮在父矩形之外」不是我们的错**：734.33 > 1320/2 = 660 ⇒ 原版那颗钮**本来就落在 `LowerSection` 矩形外**（屏幕 x 1650.0–1738.7）。C6 那条断言把它钉住了，⛔ 别「顺手挪进去」。

### ② 相关卡扇形（换位时机与曲线）

| 元素 | 原版值 + 出处 | 我们 | 差 | 改法 |
|---|---|---|---|---|
| 换位时长 | `MonoBehaviour_4750.json` `relatedCardSwapTime = 0.25` | `CardFan.cs:49` `SwapTime = 0.25f` | **无** | — |
| 相关卡压暗 | `MB_4750.json` `cardInBackGroundColorTint = (0.65,0.65,0.65,1)` | `CardFan.cs:217` `BackTint (0.65,0.65,0.65,1)` · 前台 `FrontTint` 纯白 | **无** | — |
| 六条补间 | `ChangeCardPosition.c:203-221`：两张卡各 `DOLocalMove` + `DOLocalRotateQuaternion` + `DOScale`，**同一个时长**；无自定义 ease（DOTween 默认 `OutQuad`） | `CardDisplayWindow.cs:1665/1686` 两条 `TweenPose`（内含位/转/缩放）+ `:1689-1690` 两条 `TweenTint`，`Ease.OutQuad`，同一时长 | **无** | — |
| 兄弟序互换 | `ChangeCardPosition.c:230-240`：两张卡 `GetSiblingIndex` 后互设 | `:1672` `CardFan.ApplyQueues(...)`（我们这套前后由**渲染队列**决定，语义同向） | **无**（机制不同、结果同一，已在账里记过） | — |
| 三道闸 | ① `swappingCards` ② 点的是前台自己 ③ `displayAnimation.isPlaying` | `:1606-1619` 三道齐全（顺序：③→①→②） | **无** | — |
| 换位音 | `ChangeCardPosition.c:68-73` `SoundManager.Play2D(soundOnCardSwap, MixerType.FX)` | `:1631` `WindowSounds.PlayFx(SwapSoundCue)`（cue `CardMove`），**位置**在 `SetImageOverDraw` 与补间之前 | **无** | — |
| 收尾 | `CardSwapFinished`：开闸 · 换前台指针 · 重设 lore/语音 · 清 `showCardTextButton`（战斗侧恒 null） | `:1673-1684`（OnComplete 里） | **无** | — |
| 槽数 | `cardUIs[5]`（`relatedCard1..4` 真值最多 4 + 主卡） | `CardFan.cs:248` `OriginalSlots = 5` · `RelatedCards.Find(def, OriginalSlots-1)` = 4 | **无** | — |

### ③ Tooltip（命中口与文案源）

| 元素 | 原版值 + 出处 | 我们 | 差 | 改法 |
|---|---|---|---|---|
| 命中口 | `TextTooltipController__CheckForLinkCollision.c:29`：`TMP_TextUtilities.FindIntersectingLink(TMP, 鼠标, uiCamera)` → 取 `LinkID` → `CheckLinkID` | `CardView.cs:9867` → `TmpFont.LinkAt`（内部就是 `FindIntersectingLink`）→ `BattleDriver.cs:32838` `v.LinkAt(wp, cam)` | **无**（同一 API、同一个「每个 TMP 各跑一次」的形状） | — |
| 前进门 | `TextTooltipController__Update`：`if (+0x48) CheckForLinkCollision()`，`+0x48` 只在 `OnPointerEnter` 置 1 | `BattleDriver.cs:32837` `v.ContainsNear(wp, TipHitMargin)`（等价门，注释已记） | **无** | — |
| 面板版式 | `Trait Tooltip` prefab：`Background 494.98×141.60` · `TraitImage 75² @ (−208.72, 1.74)` · `Title 398.1×34 @ (27.12, 43.10)` fs32 · `Description 397.39×89.71 @ (27.48, −16.60)` fs30 · `Line` inactive | `Core/Tooltip.cs:158-165` 逐值相同，`Line` **不建** | **无** | — |
| 出现/消失 | `EverguildTooltipManager`：`OnPointerEnter` **立刻**、`OnPointerExit` **立刻**，alpha 淡入/淡出各 0.3s（`ANIMATION_TIME`），无缩放无位移 | `Tooltip.cs:120` `FadeTime = 0.3f`；每帧 tick 命中即 `Show`、未命中即 `Hide`（`BattleDriver.cs:32803`） | **无** | — |
| 跟随 | `GetTooltipPosition`：`ScreenToWorldPoint(mouse.x, mouse.y + 10, text.z)` | `Tooltip.ShowAtMouse(body, MouseWorld, …)`，`MouseTipDyPx = 10` | **无** | — |
| 文案源 | `TextTooltipController` → `GetTermTranslation("Tips/…")`（I2 官方表） | `TipText.*` 全部 `Loc.T("Tips/…")`；关键词走 `CardTraitDescription/<规范键>`（官方 56 条，5 条退规则书、1 条 `ability` 无源） | **无**（**只剩 3 条自拟**：`Tips/ArmourTip` + 两条 `Tips/Trait/*` 括注） | ✅ 文件头旧说法本批已就地订正 |
| 数值圈 | `Health/Range Attack/Melee Attack/Cost Container` 四颗触发器；**护甲没有** | `BattleDriver.cs:32872-32875` 四条；`:32868` 显式跳过 `StatArmour` | **无** | — |
| HUD 计数 | 敌我各一条键（`Tips/Hud/{Player,Opponent}*Count`）；`Skulls` **只有一条** | `TipText` 5 + 4 条，`Skulls` 单条（`BattleDriver.cs:32797-32801`） | **无** | — |

### ④ `EffectList`

| 元素 | 原版值 + 出处 | 我们 | 差 | 改法 |
|---|---|---|---|---|
| 出厂状态 | `GameObject/EffectList.json`：`m_IsActive = False` | `CardDisplayWindow.cs:787` `SetEffectRows(null)` | **无** | — |
| 槽数 | `Elements` 下 **5** 个 `EffectElement`（现数：`EffectElement` + `(1)..(4)`，配套 5×`EffectBg`/5×`EffectText`/5×`EnchanterText`） | `CardWinBox.cs:88` `EffSlots = 5` | **无** | — |
| 亮/灭条件 | `DisplayCardEffects.c:127-131`：`SetActive(非空条数 > 0)`；**另有环境效果卡闸** `:88-105`（`spellType ∈ {210,200}` ⇒ 整组强制关，即使有行） | `CardDisplayWindow.cs:2566-2578` 外层重载（`hideForEnvironmental`）+ `:2548` `HidesEffectRowsFor`；`:951` 内层槽位模型 | **无** | — |
| 进槽过滤 | `DisplayCardEffects.c:44-53`：倒序剔除 `buffType 6 / 35 / 30` 与 `(16 且 !HasQuests)` | `:858` `UnitState.IsEffectListEntry(b.BuffType, hasQuests)` | **无** | — |
| 空文本占槽 | `EnableCardEffectSlots.c:46-60`：`IsTextEmpty` ⇒ `Disable()` **但 `cursor += 1`**（留洞、后面不往前补） | `:961` `bool on = i < n && !IsEmptyRow(rows[i])`（占槽不画） | **无** | — |

---

## 三、判据已定但**未做**的（本批不属我的白名单 / 无判据）

1. **`SetCardVoiceOver` 的两个远端开关位**（`UnityRemoteConfigManager.GetRemoteConfigData()` 的 `+0x10` / `+0x11`）—— 原版用它们分别控 `cardType == 0`（Minion）与 `cardType == 10`（Hero）的语音钮。**本地没有远端配置**（`UnityRemoteConfigData` 走 Unity Remote Config 服务）⇒ 这一格属 **P3 服务器面**，我们的等价判据退化成「这张卡有没有语音」，**已在代码注释里如实记**（`Core/CardButtons.cs` 文件头）。
2. **`GetSabotageTooltip`**（`TextTooltipController` 里那条）—— 属 HUD 的**破坏/计谋计数**，归第 4 部分（HUD 状态信息层），不在第 13 部分范围；`TipText` 里目前没有对应成员，**留给第 4 部分核**。

## 四、判据未定的

1. **`showCardTextButton` 的显隐闸 `cardType == 0 || == 10`**（`ShowCard.c:137-140` / `CardSwapFinished.c:20-45`）—— 战斗版那颗钮 13/13 为 `null` ⇒ 这条链**在战斗窗恒不执行**，无法用实况验；`CardButtons.HasTextButton` 已按同一判据实现（菜单版用），但**没有战斗侧的观测点**。判据本身清楚，只是**不可观测**，如实记。

---

## 五、顺手发现（⛔ 只报，本批未动）

1. 🔴 **活状态表引用的两份报告在本机和 git 里都不存在**：
   `活状态表.md` 的 `B117-R20` 行写着「报告 `报告/B117_G3_两扇详情窗.md` · `报告/B117_G1_战斗详情窗.md`」，但
   `ls Unity/资料/完全复刻/报告/ | grep B117_G` **零命中**，`git log --all -- <这两个路径>` **也零命中**（从未提交过）。
   ⇒ 那两条的**结论本身**已经完整写进活状态表那一行的「状态」列（G3 的两套摆放结论就在里面），知识没丢；但**指针是死的**。
   建议：调度台把那两个路径改成「结论就地见本行」或替换成本文件。
2. ⚠️ `Core/CardPreview.cs` 只被 `Deck/DeckRuntime.cs:3954`（卡组编辑的拖拽残影）使用，**与详情窗无关**；本批核了一遍，无差异。
3. ⚠️ `Effects/CardDisplayLevelUp.cs` 战斗窗**不建**（`B-195` 已定：13 场 `Level Up Effect` / `Lighting Rays` 全 0 命中），唯一合法建点是 `Shell/CardDetailPopup.PlayUpgradeAnimation` ⇒ 本部分只需要确认「战斗窗没把它接回来」，**已确认**（`CardDisplayWindow.cs:1508` 的注释 + 全类零 `AddComponent<CardDisplayLevelUp>`）。
4. 📌 **新增断言文件尚未接线**：`Editor/B117_G10_13_Assert.cs` 按简报要求「⛔ 不改既有宿主」，所以**今天没有宿主会跑它**。
   接线 = 在任意宿主里加一行 `B117_G10_13_Assert.Run();`（建议放 `BattleScene.Run` 的 UI 段末尾，那一段已有 `CardDisplayWindow.Create` 的探针）。

---

## 六、断言

**文件**：`Assets/CardPresentation/Editor/B117_G10_13_Assert.cs`（新增 · 静态类 `B117_G10_13_Assert` · `Run()` · 无宿主改动）
**条数**：断言调用点 **42 条**（`Check` 32 + `CheckNear` 10；含**前提 11 条**）。分组：
§A 源码落点锁 6 · §B/行为 10 · §C 下缘几何 8 · §D 相关卡/换位 12 · §E 效果清单 6。

**灭自证 4 组（结构上不可能同时满足）**：

| # | 内容 | 为什么是灭自证 |
|---|---|---|
| ① | **B3 ⇄ B5**：没语音 ⇒ 钮不亮 **且** 有语音 ⇒ 钮亮 | 只断一侧时，「一律关掉」和「一律点亮」（改前那版）分别都能满足；两条一起只有「按卡决定」能过 |
| ② | **D5 ⇄ D6**：`Find(TAU3,4).Count == 4` **且** `Find(TAU3,3).Count == 3` | 只断 4 的话「把 `max` 参数丢掉」也满足 |
| ③ | **C6**：语音钮偏移 734.33 **>** `LowerSection` 半宽 660 | 谁觉得「不合理」把钮挪进父矩形，C5/C6 一起红 |
| ④ | **E2**：`spellType 210/200` ⇒ 真 **且** `0/1` ⇒ 假 | 只断一侧时「恒真」「恒假」都能过 |

另有 **D7**（截断从尾部切）与 **D8**（同卡两次 `Find` 顺序稳定）两条顺序锁、**B1/B1'/B2** 三条源码级落点锁（防「改回来」）。

---

## 七、验收

- ✅ `TMPDIR=/tmp/wf_g1013 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完两轮各跑一次，均 0/0）。
- ⚠️ **本批没跑 Unity 宿主**（红线：子代理不许 `-executeMethod`）⇒ `B117_G10_13_Assert.Run()` 至今**一次都没执行过**。主对话接线后请跑一次 `BattleScene.Run`。
- ✅ 行数守恒：`git diff --numstat` = `CardDisplayWindow.cs 2/2` · `Tooltip.cs 3/3`；账里指向本文件的 52 条 `文件:行号` **一条不漂**（`SetChrome 1019` / `Hide 1342` / `BuildFan 1434` / `SwapToFront 1604` 全部原位）。
