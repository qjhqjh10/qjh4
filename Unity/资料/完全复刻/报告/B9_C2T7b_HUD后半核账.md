# B9 / C2T7b —— `战斗界面_HUD.tsv` B 行逐条核（后半 56 条）

> 查判据型子代理（只读 + 落账补丁）。范围 = `账/战斗界面_HUD.tsv` 里 **状态=B 的第 56–111 条**（共 56 条；前半 1–55 归 `R2T3B2`）。
> **边界按方法名钉**：本批起点 = `CardScript.CancelActiveAbility()`，终点 = `SupportMethods.IsCustomAbility(AbilityTrigger trigger)`。
> 产出：`判据/B行逐条核_HUD_后半.tsv`（8 列 · 56 行）· `账/_parts/B9_C2T7b.tsv`（11 列 · 56 行，状态保持原值）· 本报告。

---

## 一、结论速览

- **56 条：建议 A（等价）= 9 条 · 建议 B（保持）= 47 条。**
- **无一条「落点失效」**：47 条有落点的，落点**都是真实方法**（文件存在 + 方法名在场）。⚠️ 但**多条落点的行号已漂移**（文件长大），例如 65/70/71/79 写的 `BattleDriver.cs:25010（PlayAutoPlayCenterDisplay）`，**真身现读在 `:25339`**（合并时请走 `--fix` 按符号名重定位，别按行号）。
- **9 条「无落点（—）」按规则一律 B**：`SetAsDeck` / `GetTraitsFromAbilitiesRawCard` / `CountShowTraits` / `GetOriginalCard` / `HasAlternativeArtStyles` / `HasCustomAbility` / `IsAlternateArt` / `ShowDefaultTrait` / `IsCustomAbility`（我方无对应实现 / 概念不存在）。
- **1 条「判据缺」**：`RemnantBody.DoDestroy(UnitDeathType)` 是 **abstract 无体**（真体在各族子类 `RemnantNecrons/Aeldari`），原版无可读体可对。

### 建议升 A 的 9 条（等价，双侧方法级；供你裁定）

| # | 方法 | 落点 | 等价依据 |
|---|---|---|---|
| 61 | `CardScript.DisplayingActiveAbility()` | `Battle/SkillPanel.cs` SetActing | 面板 acting 白层 + 目标高亮，拆两半都在 |
| 63 | `CardScript.ForceDisplayCard()` | `Battle/CardDisplayWindow.cs` Show | 展示窗 + `_cardWinSource` 防重入（原版 displayingCardFlag） |
| 73 | `CardScript.SendHighlightBloodThirstAction()` | `BattleDriver.cs` PlayTraitPulse + RuleCore.EmitBloodThirst | 走事件模型（不排队），两半都在 |
| 78 | `CardScript.SetupCardLayout(CardsHorizontalLayout)` | `Hand/HandLayout.cs` Refresh | 单一布局者，卡不反存 layout（等价重构） |
| 82 | `CardScript.UnitDeath(bool,bool,UnitDeathType)` | `BattleDriver.cs` PlayDeathFeel + RuleCore.CleanupDeaths | 规则/演出拆两侧，两侧都在 |
| 87 | `CardTraitCollection.GetTraitTextSprite(DefinedTrait)` | `Core/CardIcons.cs` RewriteMarkTraits | 图名规则 `Atlas_trait_icon_{枚举}` 同一条 |
| 89 | `MinionManager.AreMinionsOutOfPos()` | `BattleDriver.cs` SyncBoard | 棋盘连续无洞 ⇒ 落位天然正确，无需「出位」判据 |
| 93 | `MinionManager.ReassembleMinions(CardScript)` | `BattleDriver.cs` SyncBoard | 连续棋盘 ⇒ 无需重组 |
| 97 | `PlayerHand.SetParentOfCard(CardScript)` | `Hand/HandLayout.cs` Refresh | 卡视图建时即挂手牌父节点，无搬父动作 |

---

## 二、47 条保持 B 的成因分类（便于排活）

**A. 真偏离（行为确实缺/多，最该修）**
- 旋转/朝向缺一半：`OrientToTargetingDirection`(69) · `SetCardMovement`(75) · `Update`(83) —— 落点 `TickTargetingLean` **只做位移+倾角、不做转向/附加旋转**（原版核心是转向目标）。
- 状态回退缺：`CancelActiveAbility`(56) —— 落点只做「非法点击取消」，缺「卡状态{在场/攻击/瞄准}→inPlay + 小兵复位」。
- 阵亡两跳缺：`DestroyCard`(58) —— 缺 `SetCardStateInCemetery` / `StopAnimsInPlay`。
- 准星/瞄准期溶卡缺：`MoveIntoFieldFromHand`(67)。
- 守卫未接线：`OnTouchUpAsButton`(68) —— 忠实实现在但 `EnemyHandCardClickable` **0 调用**（敌方手牌 Sabotage 守卫）。
- 缩放/偏移写死：`DisplayEnemyCardInCenter`(60) · `GetEnemyCardPlayedDisplayScale`(65) —— 无 `enemyCardDisplayXoffset` / `VarsGlobal` 读数。
- 多出可见动作：`HighlightBloodThirst`(66) —— 原版该状态框**恒 null（无画面）**，我们额外加徽标脉冲。
- 卡面 id 不刷：`SetInBattleId`(77) —— 只引擎侧有 id，卡面文字层不刷。
- 残骸载体/形参缺：`DoDestroyByAttack`(106 粒子未核) · `Toggle`(108 缺 withAnimation/onComplete) · `BodyVisibilityToggle`(104 载体不同)。

**B. 未逐条核（口径已对、细节未逐句 ⇒ 保守留 B）**
`CreateCardInGame`(57) · `DestroyCardOffensive`(59) · `FinishMulliganUi`(62) · `GetCardImageSprite`(64) · `PlayEnemyCard`(70) · `PlayPlayerCard`(71) · `ResolveRangedAttack`(72) · `SetCardPosAndScaleToBoard`(76) · `ShowCardInCenter`(79) · `ShowWarlordActiveAbilityForTutorial`(80) · `StopDisplayingCardDescription`(81) · `UpdateAssetInPlayPosition`(84) · `UpdateHealthText`(85) · `UpdateMinionInPlayPosition`(86) · `GetMinionConversionTime`(90) · `HideCardShadow`(91) · `MoveMinionToConversionPoint`(92) · `SetCardShadow`(94) · `GetInversedPos`(95) · `GetScale`(96) · `Initialize`残骸(107) · `GetConnectingDots`(109) · `OnGUI`(110)。

**C. 无落点（—）—— 我方无对应/概念不存在**
`SetAsDeck`(74) · `GetTraitsFromAbilitiesRawCard`(88) · `CountShowTraits`(98) · `GetOriginalCard`(99) · `HasAlternativeArtStyles`(100) · `HasCustomAbility`(101) · `IsAlternateArt`(102) · `ShowDefaultTrait`(103) · `IsCustomAbility`(111)。

**D. 判据缺**
`DoDestroy(UnitDeathType)`(105) —— abstract 无体。

---

## 三、方法学 & 证据

1. **落点存在性机械核**：48 条有落点的，逐个解析 `File.cs:LINE（Method）`，`os.path.exists` 全 OK、方法名在文件内 `name_ok`（唯一解析异常 67 号因两文件落点，人工确认在 `CardInteraction.cs:1419` + `BattleDriver.TryBeginPlayTargetPick`）。
2. **行号漂移**：多条 `落点` 行号已过期（`PlayAutoPlayCenterDisplay` 25010→**25339**；`CancelTargetChoosingByInvalidClick` 14405→**14509**；`TickTargetingLean` 15548→**15692** 等）。合并请用 `--fix` 按符号名重定位（⛔ 别按行号）。
3. **读过的原版/我方实体**（逐句比用）：`TickTargetingLean`（BattleDriver:15692）· `CancelTargetChoosingByInvalidClick`（:14509）· `DeploySequence.Play`（DeploySequence:224）· `PlayAutoPlayCenterDisplay`（:25339）· `SyncBoard`（:25812）· `PlayTraitPulse`（:24299）· `SkillPanel.SetActing`（SkillPanel:1602）· `CardIcons.RewriteMarkTraits`（CardIcons:884）· `CardView.SetPose`（:9696）/`SetRemnantBody`（:2537）· `BlobShadow.Create`（:80）· `ArenaSlots.CardScale/TryResolveSlot`（:90/:147）· `NetRuntime.Notice`（:81）· `CardHighlight.OnTouchUpAsButton`（:796）等。
4. **A 的判据**：只对「我读了落点方法体、且与要点行为等价 / 结构性等价（棋盘连续模型、布局单一化）」的升 A；未逐句核的一律留 B（保守，符合落点闸）。

---

## 四、产物与改动

- `判据/B行逐条核_HUD_后半.tsv`：8 列 × 56 行（`域 方法 原等级 建议等级 判据出处 我们的落点(文件:行) 一句结论 还差什么`）。建议等级：A=9 / B=47。
- `账/_parts/B9_C2T7b.tsv`：11 列 × 56 行（表头逐字抄主表；**状态列全部保持原值**；只改 `差异`（A 行改写为「等价（C2T7b 核）：…」、B 行在原差异后追加 `｜[C2T7b 2026-10-27] …`）/ `轮次`=`B9_C2T7b` / `备注` 前缀 `[C2T7b 2026-10-27]`，原备注全文保留）。
  - 校验：`merge_ledger_parts.py --dry --only B9_C2T7b` → **更新 56 · 新增 0 · 无变化 0 · 0 报错**；`check_patch_conflicts.py` → 无跨补丁重复键；纯 LF、无裸 CR、全 11 列。
- 未写任何 `.cs`、未跑 Unity、未动 git、未改主表/正本。

---

## 五、顺手发现

- **行号漂移普遍**（上段第 2 点）：`战斗界面_HUD.tsv` 里本批多条 `落点` 的 `:行号` 已落后于文件真身，合并前务必 `--fix`；否则 `check_ledger_points` 会报漂移。
- **`TickTargetingLean` 一个方法承担了 3 条账的落点（69/75/83），但它只做「位移+倾角」** —— 三条原版方法都要求「转向目标/附加旋转」，这是**同一处旋转缺失**造成的 3 条 B；修一处（加转向）可同时收 3 条，建议合成一个施工单。
- `PlayAutoPlayCenterDisplay` 现读 `BattleDriver:25339` 是 `Tween PlayAutoPlayCenterDisplay(Transform,int,Vector3,Action)`，被 60/65/70/71/79 多条账共用 —— 它的「居中展示」是**多账共享段**，逐条核时注意别把某一条的偏差当成整段的。
