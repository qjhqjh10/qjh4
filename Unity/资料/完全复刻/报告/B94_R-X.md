# B94_R-X — 账务落点修复（第九十四会话 · 同步点）

> 角色：账务修复代理 R-X。**只改 `账/*.tsv` 的「我们的落点」列**，⛔ 不改源码、不动状态列/判据列。

## 一、结论（一句话 + 数字）

`check_ledger_points.py --all-A` 由 **失败 47 · 漂移 11** ⇒ **失败 0 · 漂移 0**。
A 总数 **5445 未变**、通过 **5387 → 5445**（其中告警 524 → 529，+5，可接受）。
改动 **11 个账文件、58 行**，**每一行只改第 7 列「我们的落点」**（列数、行数、行尾全部守恒；CI 用备份逐行比对确认 `非第7列差异 = 0`、`行数差异 = 0`、`裸 CR/CRLF = 0`）。
工具自检 `check_ledger_points_selftest.py` ⇒ **126 PASS / 0 FAIL**。

## 二、逐类修复统计

| 类型 | 条数 | 修法 |
|---|---|---|
| ① 短名落点（`BattleDriver.cs:NNNN`） | 9 | 补成可解析路径 `CardPresentation/Battle/BattleDriver.cs:NNNN` |
| ② 注释行（落在 `///` `//`） | 13 | 改指紧跟其后的真声明（doc 注释所描述的方法） |
| ③ 名字对不上 / 不像声明 / 函数中间 | 20 | 改指真声明；等价实现无同名声明者改指所属方法声明并在括号注明 |
| ④ 落点里没有 `文件:行`（纯散文 / 用 `.` 代替 `:`） | 4 | 补真实 `文件:行`（首个 ref 必须是真声明） |
| ⑤ 次要落点空行 / 文件不存在 | 1 | 重定位到真声明（`RuleCorePartial_B88E1.cs:132`） |
| 漂移（歧义 / 目标非声明） | 11 | 指到「多个同名候选里真正对的那一处」或重定位真声明 |

## 三、逐条改动（`账文件:行` | 原落点 → 新落点 | 类型）

### 个人资料_成就_排行.tsv（3，全为失败）
- `:12` AccountTab.Refresh() | `Shell/SettingsWindow.cs:6513`（局部变量 `k`） → `:2822`（等价实现 = RefreshAccount） | ③名字不对上
- `:270` RankedMenuContainer.ToggleLegendaryRankInfo | `MainMenuRuntime.cs:3583`（语句） → `:3607`（等价实现 = ApplyRankedRatingBranch） | ③不像声明
- `:271` RankedMenuContainer.TogglePositionDisplay | `MainMenuRuntime.cs:4249`（声明的是 LanguageRebuildsForTest） → `:4273`（TogglePositionDisplay 声明） | ③名字不对上

### 其他_基础设施.tsv（6，全为失败）
- `:483` SellSpareCards | `CardDetailPopup.cs:3400`（空行） → `:3405` | ②空行→真声明
- `:484` SellSpareCardsRecursive | `:3429`（只有 `}`） → `:3434` | ③不像声明
- `:546` _003CSellSpareCardsRecursive_003Eb | `:3456`（只有 `}`） → `:3461`（OnSellSpareCardsRecursiveCompleted） | ③不像声明
- `:585` SetAccumulationMana | `BattleDriver.cs:2903`（注释） → `:2900`（RefreshEnergyAccumulation） | ②注释行
- `:586` SetQuestPointsWithDelay | `BattleDriver.cs:32070-32071`（短名+注释区间） → `CardPresentation/Battle/BattleDriver.cs:33749`（TickManaHolderValue） | ①短名
- `:506` ShouldSellCardsFromCrate | `:3391`（注释） → `:3396` | ②注释行

### 战斗界面_HUD.tsv（8 = 4 失败 + 4 漂移）
- `:58` BasicCardUI.SetEmptyRarityStars | `CardDisplayWindow.cs:1870-1894`（`//  <code>`） → `:1902`（ShowBattleCard） | ②注释行
- `:307` CardDisplayWindow.ShowBattleCard | `:1870-1894`（注释） → `:1902` | ②注释行
- `:378` ChooseCardMenu.Setup | `BattleDriver.cs:11189`（短名） → `CardPresentation/Battle/BattleDriver.cs:11189`（ShowAsk） | ①短名
- `:537` ManaManager.SetManaSaved | `BattleDriver.cs:34057`（短名） → `CardPresentation/Battle/BattleDriver.cs:34057`（SetSavedMana） | ①短名
- `:32` BasicCardUI.CardClicked | `:1189`（非 Toggle 声明） → `:1221`（Toggle 重载） | 漂移·歧义
- `:296` InitializeCards | `:1221`（非 Show 声明） → `:1253`（Show 主入口） | 漂移·歧义
- `:308` ShowCard | `:1221` → `:1253` | 漂移·歧义
- `:753` CardScript.ForceDisplayCard | `:1221` → `:1253` | 漂移·歧义

### 战斗逻辑驱动.tsv（22，全为失败）
- `:103` AiScripted.GetTipPosition | `BattleDriver.cs:16250`（短名） → `CardPresentation/Battle/BattleDriver.cs:16250`（ReticleTargetView） | ①
- `:115` AiScripted.SkippableActionWait | `BattleDriver.cs:18413`（短名） → 全路径 `:18413`（StepTutorialScript） | ①
- `:131` AddDisplayFromHand | `EffectResolver.cs:4828`（注释） → `:4794`（DoChooseCard，分支体在 4828） | ②
- `:196` ApplyEnvEffect | `BattleDriver.cs:9760`（短名） → 全路径（ApplyOffensiveEnvOnce） | ①
- `:202` BroadcastActivateMinions | `RuleCore.cs.BroadcastActivateMinions（:16403）`（无 `文件:行`） → `RuleCore.cs:16436`（真声明） | ④
- `:231` BroadcastUnitSynapse | `EffectResolver.cs:2925`（注释） → `:2887`（RepeatTacticOnAdjacent，广播在 2927） | ②
- `:393` GetWarcry | `BattleDriver.cs:34972`（函数中间） → `:34965`（MetaOf 声明） | ③
- `:439` LeaveBattle | `BattleDriver.cs:12778`（短名） → 全路径 | ①
- `:446` NeedToChooseMinionTarget | `BattleDriver.cs:7935`（短名） → 全路径（NeedsUnitTargetAfterDeploy） | ①
- `:455` PayCardPlayedCostOath | `EffectResolver.cs:2585`（注释） → 全体重写：`RuleCore.cs:16960`（真声明）＋调用点 2587 / 987 / 3945 | ②
- `:516` ResolveFatigue | `RuleCore.cs:3095`（语句） → `:3056`（MoveDeckTopToHand 声明） | ③
- `:553` ResolveTriggerMob | `RuleCore.cs:12496`（注释） → `:12540`（FireTriggerAt 声明） | ②
- `:622` UpdateManaLeftAccumulation | `RuleCore.cs.EndTurnCore（…）`（无 `文件:行`） → `RuleCore.cs:2300`（EndTurnCore 声明） | ④
- `:637` BroadcastRemoveFromGame | `RuleCorePartial_B88E1.cs:2026-10-28 …`（该文件不存在） → `RuleEngine/Core/RuleCorePartial_B88E1.cs:132`（真声明）＋ EffectResolver.cs:17093 | ⑤
- `:721` CheckStun | `RuleCore.cs:16623`（声明的是 `r`） → `:16614`（CheckStun 声明） | ③
- `:830` ReactToUnitHealed | `EffectResolver.cs:15917`（注释） → `:15918`（BroadcastUnitHealed 声明） | ②
- `:837` ReactToUnitStunned | 次要落点 `EffectResolver.cs:4055`（空行） → `:4401`（GetsStun 广播） | 次要空行
- `:1079` SetBotOpponent | `BattleDriver.cs:4514`（短名） → 全路径（BeginFromDeckLibrary） | ①
- `:1080` SetBotOpponentDeck | `BattleDriver.cs:4514`（短名） → 全路径 | ①
- `:1102` MinionManager.BroadcastActivateMinion | `RuleCore.cs:16445`（语句） → `:16436`（BroadcastActivateMinions 声明） | ③
- `:1280` ReleaseFullHeroSprite | `CardArt.cs:1341`（注释） → `:1348`（ReleaseFullHeroSprite 声明） | ②
- `:1286` TriggerAbility | 次要落点 `EffectResolver.cs:9084`（空行） → `:9086`（SpiritAbilityDeferred） | 次要空行

### 日常_奖励.tsv（4 = 2 失败 + 2 漂移）
- `:42` ChallengeLogMgr.TroopKilled | `RuleCore.cs:9908`（语句） → `:9534`（CleanupDeaths 声明；语句仍在 9908） | ③
- `:781` TroopKilled | 同上 `:9908` → `:9534` | ③
- `:2` AnnouncementEvent.GetMessage | `InboxData.cs:215`（非 Body 声明） → `:221`（Body 方法） | 漂移·歧义
- `:3` GetTitle | `:207` → `:212`（Title 方法） | 漂移·歧义

### 未分类_其他.tsv（1，失败）
- `:689` UIMessageController.OnDestroy | `ErrorMessageBanner.cs（…）`（无 `文件:行`） → `ErrorMessageBanner.cs:1151`（OnDestroy 声明） | ④

### 本地化.tsv（1，失败）
- `:5` LanguageSelector.Initialize | `SettingsWindow.cs:3551`（lambda 语句） → `:3494`（BuildLangListSubtree ＝ 原版 ResetLanguagesDropdown） | ③

### 社交_聊天.tsv（7 = 3 失败 + 4 漂移）
- `:178` BaseInboxEvent.GetMessageContent | `InboxData.cs:555`（只有 `}`） → `:560`（GetMessageContent 声明） | ③
- `:459` BoosterPackBadge record | `BadgeNotifications.cs:528`（注释） → `:536`（class 声明） | ②
- `:461` ChallengeMatch record | `:552`（语句） → `:560`（class 声明） | ③
- `:177` GetMessage | `InboxData.cs:215` → `:221`（Body 方法） | 漂移·歧义
- `:179` GetRewards | `:223` → `:228`（HasReward 方法） | 漂移·歧义
- `:180` GetTitle | `:207` → `:212`（Title 方法） | 漂移·歧义
- `:460` OfferBadge | `:540` → `:542`（class OfferBadge 声明） | 漂移·歧义

### 菜单_外壳.tsv（4 = 3 失败 + 1 漂移）
- `:379` GraphicsTab.ConfigureSuperSamplingVisibility | 次要落点 `SettingsWindow.cs:6329`（空行） → `:6360`（SuperSampling.RowVisible） | 次要空行
- `:411` LiveopMenuContainer.OnInitialize | `MainMenuRuntime.cs:4010`（只有 `}`） → `:4022`（BuildModeCardForCard；数据半边等价） | ③
- `:415` LiveopMenuContainer.SetText | `MainMenuRuntime.cs:2807`（注释） → `:2822`（BuildModeCard 声明） | ②
- `:521` OpenWindowButton.OpenWindow | `MainMenuRuntime.cs:1632`（非 OpenByRef 声明，实为 const） → `:1647`（`static T OpenByRef<T>` 声明） | 漂移·目标非声明

### 轮抽.tsv（1，失败）
- `:32` DraftEventBase.Validate | `DraftModeWindow.cs:4382`（注释） → `:4431`（DraftRunRepair.Validate 声明） | ②

### 锻造.tsv（1，失败）
- `:52` CraftingPanel.TryCrafting | `CardDetailPopup.cs:2271-2279`（空行） → `:2276`（NeedsLegendaryCraftConfirm 声明） | ②

## 四、仍修不了的

**无。** 47 失败 + 11 漂移全部修到 0，没有用「散文假落点」凑数 —— 每一条的第一个 `文件:行` 现在都指向**真声明**（或真数据行），括号里的说明保留原语义。

## 五、给工具的建议

1. **首个落点的「短名唯一兜底」**（本次 9 条短名，占失败 19%）：`resolve_ref_file` 已有「同名文件唯一 ⇒ 兜底」逻辑，但**只用于次要落点**（`check_extra_refs`），首个落点仍走 `find_file` 精确匹配。建议首个落点也加同一套兜底（命中唯一同名文件 ⇒ 通过但报 `WARN_SHORTNAME` 告警）。这样账里写 `BattleDriver.cs:NNNN` 不再假红，而「文件在别处/不存在」仍会被兜住。
2. **歧义漂移的人工入口**：`歧义（X 有 N 处声明）` 目前只能人工确认。建议 `--warn-getters`/`--warn-b` 里对这一档额外打印**每处候选的名字+签名首行**，免得人工还要回去逐个 sed。
3. **`.cs.` 写成 `.` 当分隔符**（`RuleCore.cs.BroadcastActivateMinions（:16403）`）被当「无 `文件:行`」。这是个常见手误，建议在失败原因里点名「你可能想写 `:`」。

## 六、验证命令与结果

```
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py --all-A
  ⇒ 合计：A 5445 条 · 通过 5445（其中告警 529 条）· 漂移 0 · 失败 0      [exit 0]

python -I -X utf8 d:/4/Unity/工具/check_ledger_points_selftest.py
  ⇒ PASS 126 / FAIL 0
```

**改动范围自证**（备份 vs 现行逐行比对）：`非第7列差异 = 0` · `行数差异 = 0` · `CRLF/裸CR = 0`（全 LF 保持）· `改动行 = 58`（= 47 失败 + 11 漂移）。

> ⚠️ `git diff --numstat 账/` 对着 **HEAD** 会多报 9 个**我没碰过**的文件（卡牌面_渲染/商店/战役/收藏/教程_引导/泛型与启动链_补读/特效动画/联机/音频_语音）——
> 那些是**本会话之前就已存在的未提交账改动**（我只改了 11 个文件）。我自己的改动量见上表与备份比对（58 行 / 11 文件）。
