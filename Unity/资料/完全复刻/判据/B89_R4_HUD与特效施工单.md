# B89 第 1 波 · R4 —— `战斗界面_HUD` + `特效动画` 施工单

> **生成**：2026-10-28 · B89 第 1 波查判据代理 `R4`（只读；本会话**一个 `.cs` 都没改**、没跑 Unity、没跑 ruleprobe、没动 git / `d:/2` / 账主表）
> **消费者**：下一波执行代理（按「文件所有权」切块派活）
> **取数口**：`python -I -X utf8 d:/4/Unity/工具/ledger_slice.py --domain <域> --status <B|C>`

## 0. 现读数字（2026-10-28 现跑，⛔ 别抄死）

| 域 | B | C | 合计 | 本单一共处理 |
|---|---|---|---|---|
| `战斗界面_HUD` | **5** | **19** | **24** | 24（其中 **4** 条建议升 A 无需施工 · 1 条建议复判 D + 2 条含部分 D 格 · 1 条建议转线） |
| `特效动画` | **2** | **17** | **19** | 19（其中 **4** 条建议升 A 无需施工、1 条建议复判 D） |

⚠️ **行号全部按本轮现读**（已发现多处漂移：`ShowSkillPanel` 15587→**15705** · `ShouldShowAbilityTargetsCounter` 35031→**35291** · `OnCardTapped` 13149→**13248** · `BattleUiIsNormalBattle` 33952→**34198**）。执行代理**按符号名找，⛔ 别直接信账里的行号**（`CLAUDE.md`：账行号会漂）。

---

## §一 `战斗界面_HUD` 可立即做（按文件分组）

### §一·0 先看：不需要施工、请调度台改级的 4 条

| 账行 | 原版方法 | 现等级 | 建议 | 证据（本轮现读） |
|---|---|---|---|---|
| **693** | `CanDisplayHandCard` | C | **升 A（铁律 5 订正）** | 账里写它是「可开卡面窗」的闸 —— **不是**。原版唯一调用点 = `CardScript.OnTouchEnter` → `ShowCardInHand`（ILSpy `CardScript.cs:10057`）；开的窗走另一条 `BattleManager.DisplayCard`。我们的等价物**早就在**，而且是**两条路各一份**：① **抬牌（`CanDisplayHandCard` 的真语义）** = `Hand/CardInteraction.cs:1953 ApplyShowInHand` + `:349 DragBlocked`；② **开窗**（原版另一条 `BattleManager.DisplayCard`，`DisplayCard.c:14-18` 的 `UIstate != 0 && UIstate−7 > 1 && UIstate−11 > 1` ⇒ 只挡 {0,7,8,11,12}）= `:638 BlockedThisFrame` 的 `FullBlocked()`（8 扇模态窗 + 终局）—— 覆盖**比原版更严**（这是照 `:330-350` 那段 doc 的既有裁定，⛔ 别为了「像原版」把闸放松）⇒ **无缺口** |
| **750** | `DisplayOffensiveCardInCenter` | C | **升 A** | 落位已落地（`BattleDriver.RevealEndPx` = (589,495)），残差只剩 `RevealStartPx (960,660)` = 已标注的 `🔶 自己设计`（原版起点是世界零、不在 UI 平面） |
| **756** | `GetEnemyCardPlayedDisplayPosition` | C | **升 A（需先跑 `BattleScene.Run` 验两条断言）** | 已接线 + 两条结构断言（`Editor/BattleScene.cs:15410` + 文件尾 `W67CenterAssert`）**从未跑过**（上一波禁跑 Unity）。跑绿 ⇒ A |
| **757** | `GetEnemyCardPlayedDisplayScale` | C | **升 A（账里「常量零消费者」已过期）** | 现读 `BattleDriver.cs:26050 EnemyCardPlayedDisplayScale = 1.4f` + `:26065 EnemyCardPlayedMobileMultiplier = 1.3f` + `:26071 PlayedCardCenterAbsoluteScaleForTest`（读 `SmallScreenUI.Enabled` 唯一源），并由 `:26139 EnemyCardPlayedDisplayPointWorld` 那条链使用 ⇒ **不是零消费者** |

> 另：**750 / 756 / 757 三条同族**（都是「卡本体居中展示」），改级由调度台**一次裁定**，别拆开。

---

### 组 H-A ｜ 独占文件 = `CardPresentation/Hand/CardInteraction.cs`（★本波最干净的一组，不碰 `BattleDriver`）

**本组独占文件**：`CardPresentation/Hand/CardInteraction.cs`（2163 行）
**同组辅助（只读）**：`CardPresentation/Core/CardHighlight.cs`

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **695** | `BattleManager.CanMoveHandCard()` | ILSpy `BattleManager.cs:46525-46542`：`if (playerHand.animFlag) return false; return UIstate−1 == 0;`。唯一调用点 = `CardScript.cs:10313`（**OnTouchDrag 起拖那一跳**）。🔴 **本节订正账里一条过期话**：账（及 `特效动画:497` 的备注）说「原版 `animFlag` 0 写点」——**不成立**，ILSpy `PlayerHand.cs:858 / 950 / 1302 / 1457` **四处写**（手牌重排协程 MoveNext 的进/出），`IsDoingAnim()`（`:3300`）只是 0 调用者的包装 | `CardInteraction.cs:349 bool DragBlocked()` → 追加 `|| IsDoingAnim`。等价物**已在本类**：`:436 Tween _animGate` / `:436 IsDoingAnim` / `:459` 由 `CardTween.RelayoutDuration`（0.2 s = 原版 `timeToPositionCard`）开窗 ⇒ **同一类里一行就能接** | ① 自检：`Relayout` 期间（`IsDoingAnim == true`）调 `BattleStateBlockedNow` ⇒ 必须为真；② 反证：把 `_animGate` 手动清 null ⇒ 必须为假。③ 真 Play：拖手牌期间重排刚结束的前 0.2 s 内起拖应被吃掉（很难手测，**以断言为准**） | **可立即做 → 做完建议 A**（本组唯一一条，一行改动 + 两条断言） |
| **761** | `CardScript.OnTouchUpAsButton()`（敌方手牌那一档） | `decomp_full/CardScript__OnTouchUpAsButton.c:31-36`：**非我方手牌只有 `rawCardData.spellType == 230 (Sabotage)` 才可点**；`CardHighlight.cs:836 EnemyHandCardClickable(isPlayer, hasRawCard, sabotageKind)` **已建、全树 0 生产调用点** | 接线点 = **敌方手牌那张卡挂的 `CardInteraction`**（同一份脚本也挂在敌方手牌上），在它的**轻点支**里加：`if (!isPlayer && !CardHighlight.EnemyHandCardClickable(false, hasRaw, sabotageKind)) return;`。`sabotageKind` 用现成的 `CreatePool.MatchesKind(card, "sabotage")`（⛔ 别另造判据） | 亮出敌方手牌后轻点：sabotage 卡开窗、其余不开；两条都要有断言（**先写前提断言**：那张卡确实被亮出来了，⛔ 别 `if (前提) Check`） | **可立即做**；⚠️ 本条与 `695` **同文件 ⇒ 必须串行**（同一个写手一次做完） |

> ⚠️ `761` 的语义边界（我核过、写清以免做反）：原版 `CanDisplayHandCard` 要求 `isPlayer`，所以**敌方手牌本来就走不到那条悬停路**；本条只决定「轻点要不要开窗」。别把它做成「敌方手牌也允许抬牌」。
> 🔴 **2026-10-28 就地订正（铁律 5 · `B89-E5e` 现读 `.c` 推翻本行前提）**：`BattleCardUI__ShouldShowCardback.c:29-39` 逐句 = `inHand ∧ !isPlayer ⇒ return 1`（**恒卡背**）⇒ 原版敌方手牌**任何状态都不翻面**，它点的是**卡背那一格**、开的是卡面**展示窗**（`DisplayCard(bm, this, 1, 0, 0)`）。⇒ **不该建敌方 CardView、不该有「亮出敌方手牌」这个动作**（那样就是自造旁路）。正确形状 = 卡背可点 + 只有 sabotage 开窗；判定已落 `CardHighlight.FoeHandCardTapOpens`(:1149)，输入路由待接 `BattleDriver`。

---

### 组 H-B ｜ 独占文件 = `CardPresentation/Net/NetRuntime.cs`

**本组独占文件**：`CardPresentation/Net/NetRuntime.cs`
**说明**：805 / 806 是**同一条原版方法的两个切面**（`GetConnectingDots` 供 `OnGUI` 用），必须一次做完。

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **805** | `ShowStatusWhenConnecting.GetConnectingDots()` | `decomp_full/ShowStatusWhenConnecting__GetConnectingDots.c:1-38`（🥇 全文）：`dots = floor(Mathf.Repeat(Time.timeSinceLevelLoad × DAT_1834b2e8c, DAT_1834b2df0))`，逐次 `String.Concat(base, ".", …)`。🔴 **本轮新读出两个常量**（`工具/read_literal.py` 实跑，4B float 档）：`0x1834b2e8c = 3.0f` · `0x1834b2df0 = 4.0f` ⇒ **点数 = `floor(Repeat(t × 3, 4))`**，即 **0/1/2/3 循环、每 1/3 秒加一个点**（⚠️ 账里写的「每秒加一个点」**不准**，本轮订正）。基串 `DAT_1842b80e0` 是字符串对象，**未读出字面量** | 在 `NetRuntime` 里加一条**常驻**「连接中」横幅（不是 `Notice` 队列 —— `:81 Notice` 是「新弹一条」，每秒入队会把队列撑爆，`:132` 的注释已经点名这件事）。点数用上面的式子；⛔ 别复用 `Notice` | ① 断言（可无帧循环跑）：`DotsForTest(0f)=0 / (0.34f)=1 / (0.67f)=2 / (1.01f)=3 / (1.34f)=0`（±1 边界）；② 有连接 ⇒ 横幅常驻、断开 ⇒ 消失 | **可立即做** |
| **806** | `ShowStatusWhenConnecting.OnGUI()` | `decomp_full/ShowStatusWhenConnecting__OnGUI.c:1-80`：`GUILayout.BeginArea(rect, …)`，`rect = ((Screen.width − **400**) × 0.5, (Screen.height − **100**) × 0.5, 400, 100)`；两常量本轮实读 `0x1834b3270 = 400f` · `0x1834b2e08 = 100f` | 同 805 的那条横幅：**居中 400×100** 的盒子。⚠️ 原版是 IMGUI 自绘，我们走自己的窗口/`ImageQuad` 体系**不算偏差**（差的是「常驻」与「节奏」，账里已这么写） | 断言：横幅 rect 中心 = 屏幕中心（±1 px）、尺寸 400×100（走 `LayoutSpace.ToDesignPixel` 读回设计 px，⛔ 别比世界坐标 —— 单位差 7.28 倍） | **可立即做**（与 805 同一笔） |

---

### 组 H-C ｜ 独占文件 = `CardPresentation/Core/CardFeel.cs`

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **802** | `RemnantBody.DoDestroyByAttack()` 的落点 | `cpp2il/out_cpp2il_dev_ilspy/RemnantBody.cs:53-84` + `RemnantNecrons__GetDeathParticlePosition.c` / `RemnantAeldari__GetDeathParticlePosition.c`：`GetDeathParticlePosition` **只填 x/z、y 恒 0**。「四件里三件已对，只差 y」 | `CardPresentation/Core/CardFeel.cs:2921 Vector3 pos = root.transform.position;` 与 `:2927 pos = node.position;` ⇒ 改成 `new Vector3(p.x, 0f, p.z)`。**先量一次再改**（战场是水平面但残骸根是卡底边锚点，y 未必是 0） | 在 `Editor/BattleScene.cs` 加一条「`SpawnRemnantDestroy` 生成件的 `transform.position.y == 0f`（容差 1e-3）」。`SpawnRemnantDestroy` **不看 `animateFeel`**（现读 `:2899-2941` 无该闸）⇒ 批处理档可读 | **可立即做**（若量出 y 本来就是 0 ⇒ 本条直接 A，只补断言） |

---

### 组 H-D ｜ 独占文件 = `RuleEngine/Core/CardDef.cs` + `RuleEngine/Data/CardDatabase.cs`（+ 生成器那一半已 OK）

**本组注意**：这两个文件**不在 `CardPresentation/` 下**，与 E5 的 `BattleDriver.cs` **不相交**，可并行。`工具/gen_cards_truth.py:296-303` **已在 ability 级 emit** `targetsAvailableCounter`（前一路 `E4BDR2` 已订正）⇒ ⛔ 不用改生成器。

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **20**（数据这一半） | `ActiveSkillDisplay.SetupActiveSkill` 的**后缀闸** | `cpp2il/out_cpp2il_dev_ilspy/ActiveSkillDisplay.cs:160`（闸读 **ability 级** `targetsAvailableCounter(+0x40)`）· `:172`（`maxTargets==1` ⇒ ` (目标卡名)`）· `:188`/`:193`（后缀并进 `descText`） | `CardDef` 加 ability 级 `TargetsAvailableCounter { targetsAffected, maxTargets }`；`CardDatabase` 落列。落地后 `BattleDriver.ShouldShowAbilityTargetsCounter`（现读 `:35291`）**自动就真**（它今天恒 false 正是「读不到那一格」） | 现读 `cards_truth.json` 全 1,351 张该格 **0 条** ⇒ 补齐后**仍应恒 false**（原版在这份数据上永远不拼后缀，见账）。⇒ 断言写「补列后仍为 false」，**不是**「补列后变 true」 | **可立即做（数据侧）**；⚠️ 但**断言口径必须先确认**：真值 0 条意味着补列后**行为不变**，这条做完只是把「判据从缺」变「判据为 0」→ **建议做完直接 A，不产生可见变化** |
| **755**（数据这一半） | `CardScript.GetCurrentLocalizedAbilityDescription` 的 `activeAbilityTextReferenceId` 那一半 | `RawCardScript.cs:2374-2446`：id 那一半 = `(activeAbilityTextReferenceId ?? uniqueId)`；`RawCardScript.cs:2384-2405` 还有一支 `id.Contains("((")` ⇒ **整条跳过本地化、把 id 当正文** | `CardDef` 加 `ActiveAbilityTextReferenceId`（原版 `+0x248`），`CardDatabase` 落列；`SkillPanel.AbilityDescription`（`SkillPanel.cs:2133`，三档链已按 `B9_W21SKILL` 修完）读它 | 现读：官方 10,854 键里 120 条 `Card_Abilities/*`，**86 条**能由卡 id 拼出、**34 条拼不出**（30 条 `Effect<名>` + 4 条非卡 id）。补上该列后，`SOR66（Armorium Cherub）` 等应能取到原版那条 | **前置 = 数据列**（来源 1,351 个卡 SO 的 `+0x248`）。数据到位前**动手无意义** ⇒ 见 §三 |

---

### 组 H-E ｜ ⚠️ **落在 `BattleDriver.cs` ⇒ 等 E5 交回**（本波 E5 独占该文件）

> 本组**一律不派写手**，只把「改哪个**符号**」写清；E5 交回后按符号接。

| 账行 | 原版方法 | 判据 | 改哪个符号（⛔ 用符号名，不用行号） | 验收怎么看 |
|---|---|---|---|---|
| **741**（残差 1 条） | `BattleManager.Update()` 的 B08 段 | `BattleManager__Update.c:97-1413` 的 B08 段：教程「等玩家用主动技」那一支**通篇只有一条 `LogWarning`、零局面影响** | `BattleDriver.Update` 的教程支（对应 `Ctx.Tutorial` 非空那一档）补一条诊断日志 | 无可见行为；断言只能断「日志发过」⇒ **建议调度台判 D（零局面/零画面影响）**，见 §四 |
| **759** | `CardScript.HighlightBloodThirst()` | `decomp_full/BattleCardUI__HighlightBloodThirst.c:1-36`：`+0x268`（`bloodThirstFrame`）为空 ⇒ `op_Equality` 为真 ⇒ **整段跳过**（只留 `DOFade(to 0.3, 0.15).SetLoops(6, Yoyo).SetEase(22)` 那一支不跑）；`CardScript__HighlightBloodThirst.c:1-20` 随后**照样**调 `BattleCardUI.DisplayTriggerAnim(0xdc, 1)` | `BattleDriver.PlayTraitPulse`（现读 `:24845`）：`kw == KeywordTable.BloodThirst` 时**跳过** `v.PulseBadgeByKeyword(kw)`（`:24896`），只走 `TraitFrames.DisplayTrigger`。`DisplayTriggerAnim` 是 trait 动画分派器（`BattleCardUI__DisplayTriggerAnim.c` 全文亲读，**不**调 `HighlightTraitIcon`）⇒ 我们那下徽标脉动**确实多出来** | 触发 bloodthirst ⇒ 卡上徽标**不许**有脉动；状态框照播 |
| **493** | `BattleManager.ResolveBloodThirstAnim` | `BattleManager__ResolveBloodThirstAnim.c:1-132` + `CardScript__ActivateBloodThirst.c:5-23`：闸 = `HasCurrentTrait(0xdc)` ∧ `+0x48==1` ∧ `isPlayer==IsPlayerTurn` ∧ `cardState != 5`；演出 = 上面那条 6 次 yoyo 淡出 **+** `DisplayTriggerAnim(0xdc, 1)` | 与 `759` **同一处**：`PlayTraitPulse` 的 bloodthirst 支。原版那一下在 `bloodThirstFrame` 为空时**什么都不演**（我们两半都没有那个框）⇒ 合并结论 = **759 的「删掉多出的脉动」就是 493 的全部** | 同 759（一条断言覆盖两条账） |
| **713** | `BattleManager.GetUnitSizeRatio(bool)` | 🔴 **本轮重大订正**：`GetUnitSizeRatio` 在**三份反编译源里 0 调用者**（`decomp_full/` 只命中它自己；ILSpy / il2csharp 全树也只有定义）⇒ 它本身是**死方法**。真消费者 = **`BattleManager.GetUnitSizeInPlay(CardTypeOptions, bool)`**（`decomp_full/BattleManager__GetUnitSizeInPlay.c:1-…` 全文亲读）：`Minion` ⇒ `desiredScale`；`Hero` ⇒ `heroScale`；**`else` ⇒ `Vector3.one × minionInPlayScale(+0x68 = 0.75) × (isPlayer ? 1.0 : enemyUnitsScaleRatio(+0x74))`**。资产真值 = `d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/GlobalVariables.asset:38 enemyUnitsScaleRatio: 0.86`（⚠️ C# 默认值是 `0.93`，**以资产为准 = 0.86**） | `CardPresentation/Board/ArenaSlots.cs`：`CardScale/HeroScale`（`:90-92`）只覆盖了前两支；需补第三支 `InPlayScale(bool enemy)` = `0.75f × (enemy ? 0.86f : 1f)`，并在 `BattleDriver.ArenaCardScale`（E5）按 `u.Card.Type` 分派 | 断言：`InPlayScale(false) == 0.75f`、`InPlayScale(true) == 0.75f × 0.86f == 0.645f`（±1e-4）；另断 `0.86 ≠ 0.93`（🧨 用 C# 默认值 ⇒ 红） |
| **208** | `EnviromentalEffectCardsSO.GetEnvCardListDraft` | 规则层已建且逐条同 `.c`（`OffensiveCards.cs:182 ChoicesForDraft`，`B9_E2UDW/W33FX3` 核过）。**剩三条全在 `BattleDriver`**：① AI 路径 `PickOffensiveForAi` 仍走 `OffensiveCards.Choices`；② 防御半边 `offensive:false` 未接；③ 原版两端胜场取 **min**（`BattleManager__GetEnviromentalCardList.c:36-51`），我们只有本机 | `BattleDriver.PickOffensiveForAi`（现读 `:9618`）改用 `OffensiveChoicesOwnedCore` 的同一份分派；`OffensiveChoicesOwnedCore`（`:35729`）补 `offensive:false` 支；`DraftVictoriesLocalForEnvCards`（现读 `:35755`）加对端来源并取 min | 断言：胜场 0/2/3/6/9 五档 × 进攻/防御两态，可用卡数 = `OffensiveCards.DumpDraftAvailabilityForTest()` 那张表（⛔ 别在自检里另写阈值 —— 同一份规则） |

### 组 H-F ｜ 其余 `战斗界面_HUD` 落点（分散，逐条独立）

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **537** | `ManaManager.SetManaSaved` | `cpp2il/out_cpp2il_dev_ilspy/ManaTypeHolder.cs:186-246` + `decomp_full/ManaTypeHolder__SetSavedMana.c` / `__ToggleManaAccumulation.c`（六步状态机） | 桥已接（`BattleDriver.SetSavedMana` / `SyncSavedManaToHud`）。**只剩半格**：`RuleEngine` 的 `EndTurnCore` 在 `SetSavedMana` 之后**立刻**把 `p.Energy = 0`（原版那一刻 `unusedMana` 还留着清零前余额）⇒ 要把「清零前的 `Energy`」作为 `unusedMana` 传过去 | 真 Play（遭遇战 `manaAccumulation = 1`）：回合结束 ⇒ 我方 `40k_battle_energy_full` 亮 + 数字 = `Energy − carry`；下回合开头 ⇒ 灯灭、数字回 `MaxEnergy`。⚠️ 批处理里 `_energyAccum*` 组是关的（经典模式）⇒ **只能走遭遇战夹具** | 落点在 `RuleEngine/Core/RuleCore.cs` 的 `EndTurnCore`（**不在 `BattleDriver`**，可与 E5 并行） |
| **712** | `BattleManager.GetUIState()` | `decomp_full/BattleManager__GetUIState.c:1-6`（全文 = `return *(int*)(this + 0x290);`） | ① **机检修复**：`BattleDriver.BattleUiIsNormalBattle`（现读 `:34198`）是**隐式 private + `{` 拆到下一行** ⇒ `check_ledger_points` 判「不像声明」。补显式 `private` 并把 `{` 挪到同一行（零语义变化）② 可选：补齐缺的 5 档（playingMinion / minionAttacking / choosingUnitTarget / targetedActiveAbility / choosingAbilityTarget） | ⛔ 零语义变化 ⇒ 断言只需「改完 `check_ledger_points` 该行不再报『不像声明』」 | **等 E5**（同一文件）；① 是纯排版、可顺手做 |
| **689** | `BattleManager.BasicBattleEndSequence()` | `BattleManager__BasicBattleEndSequence_d__391__MoveNext.c:35-173`；常量 `0x1834b2bb4 = 0.5f` · `0x1834b2bb8 = 1.0f` | 已实现。残差：① `battleWinner == 10` / `resultType == 0x14` 两道「整侧不摧」分支**没建模**（值来自服务端 `RankedMatchResult`）② 真 Play 未验 | 真 Play：打完一局 ⇒ 两侧单位逐个倒下（hero 先）、约 1 s 后出结算面板 | 见 §三（需真 Play + 服务端语义） |
| **704** | `BattleManager.DestroyUnitsAfterBattleEnd` | `_DestroyUnitsAfterBattleEnd_d__392__MoveNext.c:20-180` | 已实现。残差：`skipDeathSound`（= `matchResult+0x24 == 2 Forfeit`）与 `SetAsUnitDestroyed` 那一跳**无观察量/无等价物** | 真 Play（同 689 一次做完） | 见 §三 |

---

## §二 `特效动画` 可立即做（按文件分组）

### §二·0 先看：不需要施工、请调度台改级的 5 条

| 账行 | 原版方法 | 现等级 | 建议 | 证据（本轮现读） |
|---|---|---|---|---|
| **86** | `AnimateUIMaterialProperty.SetMaterial` | C | **升 A**（或 D） | 原版**也**全树 0 调用点（ILSpy 除自身外无任何文件提到这个类；`decomp_full` 只命中 `__SetMaterial.c` 自己）⇒ 我们「0 调用者」与原版一致；实现已逐句对齐（3 句：`CreateInstanceMaterial()` + 写词 `+0x78`，**不回头改 `image.material`**） |
| **506** | `CardScript.CanContinueAnim` | C | **升 A** | `Battle/AnimGate.cs` 已建、`BattleDriver` **8 处已接**（`:24430 / :24504 / :24779 / :24882 / :24951 / :25018 / :25028 / :25140`）；12 处三目 `ViewAt` 已判「问/混合」⇒ **不是缺口**（本波⛔ 不许换） |
| **508** | `CardScript.DissolveWholeCard` | C | **升 A** | ③④⑤ 三句已补（`BattleDriver.StopCardUiExtrasForDissolve`）；`StopShowingCodex` / `StopReflected` **我们这侧无对应层**（原版 `customObject` 全空引用 / `reflectedPrefab` 空引用）⇒ 已出声一次、⛔ 不假装做了 |
| **497** | `BattleManager.StopHandAnim(float delay)` | C | **升 A（铁律 5 订正）** | 账里写「本仓**没有**『延时收教程指针』这一跳 ⇒ 记缺」——**不成立**。现读 `BattleDriver.cs` 已**四件全做**：`:16687 PlayPendingBerzerkTriggerAnim()`（= ① `DisplayTriggerAnim(berzerk,1)`）· `:16689 ShowPendingBerzerkPointer()`（= ③ `StartPointer`）· `:35550 PendingBerzerkPointerSeconds = 2.0f` + `:35597 ShowPendingBerzerkPointer` 里 `_berzerkPointerOffAt = _clock + 2.0f` · `:35621 TickPendingBerzerkPointer`（= ④ 到点 `SetPointer(false)`，走 `_clock` 泵，批处理可推）· `:35633 TickPendingBerzerkPointerForTest`。⇒ 真残差只剩**两条已写进代码注释的已知偏离**：`to`（拖拽终点）未做（`SetPointer(bool, Vector3)` 是单点口）· 非教程局不画（原版恒画） |
| **490** | `BattleManager.RemoveVoiceLinesControllerListeners` | C | **建议复判 D**（见 §四） | 原版摘 `VoiceLinesController(+0xb0)` 上的两个 popup 委托；我们**没有那条 popup 订阅链**（语音按需 `Has`/`Play`）⇒ 该副作用在我们的架构里**不存在** |

---

### 组 V-A ｜ 独占文件 = `CardPresentation/Battle/BattleDriver.cs` ⇒ ⚠️ **等 E5**

| 账行 | 原版方法 | 判据 | 改哪个符号（符号名，⛔ 不用行号） | 验收怎么看 |
|---|---|---|---|---|
| **484** | `AbilityLogic.PlayAnim` | `decomp_full/AbilityLogic__PlayAnim.c:1-282`（🥇 逐句读完）· 调用点 `AbilityLogic__PlayAbility.c:262` | `BattleDriver.ExpandAbilityBeats` / `PlayAbilityBeat`：**① 口径订正** —— 原版「`abilityTargets` 为空 ⇒ `LogError` + **整段 return**」，我们**只跳这一拍** ⇒ 改成整段返回 + `Debug.LogError`。② `PlayAnim.c:62-64` 那个虚谓词（`AssetReferenceTyped<T>` vtable 槽）**仍判不了** ⇒ 见 §七。③ 多目标档池内 0 张可达（`Ability:` 全池 0 张）⇒ 只有夹具能证 | ① 夹具：传空目标表 ⇒ 整个能力拍**一条都不排** + 一条 Error 日志（🧨 只跳一拍 ⇒ 红） |
| **492** | `BattleManager.ResolveAnimInfoRangeAttack` | `BattleManager._ResolveAnimInfoRangeAttack_d__468__MoveNext.c:1-149`（真体）；`skipSound` 是**未使用形参**（`d__468` 字段表已证，⛔ 别去接它） | `BattleDriver.ScheduleAttackPresentation`：补 `animInfo+0x80` 的 `UnitTweenSO` 序列那一跳 —— `BuildSequence(acting, target)`，且 `+0x18` 为真时 `WaitForCompletion` **且这段等待要算进链长（队尾）** | 断言：带 `+0x80` 的那一击 ⇒ 队尾时刻 = 前面各段 + tween 长度（🧨 只播不等 ⇒ 队尾偏早、红）。⚠️ **别把「手感补间参数逐条照 clip」当成这段已复刻**（参数对、长度不进队尾） |
| **485** | `AiScripted.ShowPlayerActionAnim`（玩家/教程指针支） | `decomp_full/AiScripted__ShowPlayerActionAnim.c:1-638`（🥇 全文）；`TutorialPointer__StartPointer.c:1-68`；常量 `0x1834b2bbc = 2.0` / `0x1834b2bb8 = 1.0` | `BattleDriver.TutorialPointerAnchorFor` + `ApplyTutorialActionView`。三处差：① 原版取「**被选中的那张**」= `GetChosenCard(对应区列表, cardId)`（`.c:60-110` 五档区），我们只认 `d0.acting`、**同卡多份挑谁没建模**；② 原版还画 `ShowAttackTypeMark`（攻/近/远标记），我们没有（全树只 `AttackSelector.cs:1203` 一句注释）；③ 原版那颗 `TutorialPointer` **恒在**（非教程局也画），我们只在 `Ctx.Tutorial != null` 时建 overlay | ① 同卡两份在同一个区 ⇒ 指针落在**被选中的那张**（夹具）；② `ShowAttackTypeMark` 要新做（见 §七：本地无对应 sprite 判据）；③ 非教程局也建 overlay ⇒ 断言「`Ctx.Tutorial == null` 时 overlay 也非 null」 |
| **208** | 见 §一 组 H-E（同一笔，两域共用一行账） | | | |

### 组 V-B ｜ ⚠️ **落在 `BattleDriver.cs` ⇒ 等 E5**（本组无独立文件）

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **403** ① | `SwarmVFXController.Initialize` 的材质交接 | `decomp_full/SwarmVFXController__Initialize.c:20-21`：`CardMaterialHelper.ConfigureMaterial(renderer.GetMaterial(), card)`；方法体 `CardMaterialHelper__ConfigureMaterial.c:23-58` **本地可读**（🔴 代码注释 `BattleDriver.cs:25043/25049` 写的「**查不到判据，不换**」**不成立** —— 判据就在本地）——把那件 VFX 材质的**卡插图**与**按 `CardTier` 的 MatCap** 两格换成这张卡的 | `BattleDriver.PlaySwarmMergeFeel`（现读 `:24798`；「不换材质」那句警告在 `:25044-25050`）。两格的取法判据已备：`CardArt.cs:162-171` + `CardMaterialHelper__GetCardMatCapByCardTier.c`。⚠️ 还要一条：`WarpforgeEffectPlayer` 今天**没有「按卡改写特效材质」的路**（材质来自效果库原件）⇒ 得先给效果播放器开一个「材质两格写入」口 | 断言：合并演出那件 VFX 材质的 `_CardImage` = 这张卡的插图、`_MatCap` = 该 `CardTier` 那张（走 `sharedMaterial.GetTexture` 比对） | **等 E5**；②③ 见 §三 |

### 组 V-C ｜ `CardPresentation/Battle/TutorialOverlay.cs` + `BattleDriver`

> **497 已做完 ⇒ 本组无施工项**（见 §二·0）。保留下面的判据备查。
> 真残差（`to` 拖拽终点 / 非教程局也画）**是两条新的已知偏离**，⚠️ 若要补，得先扩 `TutorialOverlay.SetPointer` 成双点口 —— **本条不在 497 的账上**，建议另立新账（见 §七 U11）。

| 账行 | 原版方法 | 判据（备查） | 现状 | 状态建议 |
|---|---|---|---|---|
| **497** | `BattleManager.StopHandAnim(float delay)` | `decomp_full/BattleManager._StopHandAnim_d__405__MoveNext.c:1-44`（真体 = `WaitForSeconds(delay)` → `tutorialPointerBattle.OnDisable()`）· 唯一调用点 `out_il2csharp:5333`（`EndTurnClick`，**delay = 2.0f**）· `TutorialPointer.OnDisable` 与 `StopAnim` **同址**（RVA `0x854B20`，ICF 折叠）= `activeAnim=0` + `moveTween.Kill()` + `pointer.SetActive(false)` | **四件全做**（`:16687` / `:16689` / `:35550`+`:35597` / `:35621`）。有 `TickPendingBerzerkPointerForTest` 可断言 | **升 A** |

### 组 V-D ｜ 引擎侧 `RuleEngine/Core/RuleCore.cs`

| 账行 | 原版方法 | 判据 | 改哪个符号 | 验收怎么看 | 状态建议 |
|---|---|---|---|---|---|
| **503** | `CardScript.ActivateTraitParticlesFromCodeInTarget` | `decomp_full/CardScript__ActivateTraitParticlesFromCodeInTarget.c:1-86`：唯一调用方 = `CardScript__TriggerOnMinionDeath.c:77`，trait `0x4ec` = **1260 = huntMark**（`trait_collection.json` 现读）；`.c:47-72` 把 `ResolveTraitAnim(anim, acting, target, caster)` **三个角色都填那张卡** ⇒ 粒子播在**死者**身上 | ① `RuleCore` 的 huntMark 死亡支（现读 `RuleCore.cs:9740-9743` 一带，**不发 `EvtKind.Trigger`**）补发一条带「**死者格**」的事件；② `BattleDriver.PlayTraitParticles`（现读 `:24928`）加一条「按事件里的目标格播」的口（今天 `TraitParticles.Play(t, v.transform)` **恒挂触发者自己**，`:24955`） | 断言：huntMark 击杀 ⇒ 粒子件的父 = **死者**那张卡的视图（🧨 挂触发者 ⇒ 红）。⚠️ 另一支 `…FromCode`（trait `0x47e`/`0x4f1`/`0x4d8`/`0x28a`）acting/target 都是自己，**我们那条路是对的、⛔ 别改** | ① 引擎侧（可并行）；② 等 E5 |
| **486** | `BattleManager.AddCardAnim` | `decomp_full/BattleManager__AddCardAnim.c:1-98`；道序 = `0x368(p2) → 0x358(p1) → 0x360(p3) → 0x350(p0)`（`BattleManager__Update.c:1284-1299`） | **表现侧已切完**（`BattleEvent.Priority` + `BattleDriver` 三处带 `lane = e.Priority` + `AnimLaneRank`）。**剩发侧**：`BattleContext.Emit(...)` 加 `int priority = 0` 形参 + 四处 `ctx.Emit(EvtKind.Ability, …)` 传值 —— **逐条已写成施工单** ⇒ 直接照 `资料/完全复刻/判据/B88_E5_AbilityPriority_发侧施工单.md` 做（`RuleCore.cs:11621` 主动技=1 · `:11789` 誓约=1 · `:12047` 替代行动=按 `CardDef.AbilityTriggerPriority` 否则 1 并出声 · `:15425` 请 E1 先判属哪一支） | 指纹口径：`NetProtocol.Fingerprint` 读 `ctx.Events.Count`，新增字段不改条数 ⇒ 指纹应稳定。另：同帧塞 p0 + p2 ⇒ p2 先播 | **引擎侧可立即做（照现成施工单）**；`ScheduleAttackPresentation` 恒 lane 0 那一格 = 攻击动画族 priority 生产者 ⇒ 判据 `判据/B88_AddCardAnim_priority表.tsv`（12 行），属 `BattleDriver` ⇒ **等 E5** |

---

## §三 等条件 / 等接线 / 需 Unity 差分实验（⛔ 现在别派写手）

| 账行 | 域 | 卡在哪 | 解锁条件 |
|---|---|---|---|
| **306** | HUD | `CardDisplayWindow.SetupTutorialObj`：闸的数据源 = LiveOps `TutorialEvent`（`Shell/TutorialModePopup.cs:29` 写明随远端下发 / PlayFab userInfo）⇒ 建出来**恒不可达**，且会与教程脚本的 `SetAnnotations` 形成**双控**。同族子树已在 `Battle/TutorialOverlay.cs:827`（`TutorialObjs` + 四个子件 + `:1074 SetPointer` / `:1091 SetAnnotations`），**父链不同**（我们挂 Tutorial Canvas） | **归教程线**；需先有远端教程事件或用户裁定做本地模拟 |
| **364 / 378** | HUD | `ChooseCardMenu.AutoProcessDone` / `Setup` 的④「回放自动选」：录像里**只记答案**（`Ctx.ChoosePicks` / `ChooseCardIds`）、**没记候选集** ⇒ 面板摆不出来。读侧 `Battle/ReplayStore.cs` 可改，但**生产者 = `BattleDriver.BeginAsk`**（现读 `:11050`，⚠️ 账里写 11015 是漂移；E5） | ① E5 在 `BeginAsk` 写「本次选择候选集」；② 回放路径喂给 `ChoosePanel`；③ 2 s 用 **`Tick` 显式秒数**（批处理无协程）。⛔ 别只画面板不摆候选 |
| **709** | HUD | `GetCurrentTutorialStage` 战役支：`Ctx.CampaignStage`（`BattleContext.cs:922`）**没有任何写点**（`RuleCore.cs:1220/2799` 自承）⇒ 130/190 两支恒 null。教程支已对（`Ctx.Tutorial`） | 战役局入口把 `TutorialStageData` 灌进 `Ctx.CampaignStage` ⇒ **战役线** |
| **684 / 687** | HUD | `PressedBtn` 那颗按压件：账里判「本地取不到（rect/sprite 都取不到）」，证据是 `assets_full` 全树**按名字**搜只命中 `globalmonobehaviour` 的 MonoScript。🔴 **按 `CLAUDE.md`「判资产本地没有不能按名字搜」这条口径，该证据不足** ⇒ 见 §七 | 需**按脚本/组件反查**（`m_Script` pathID → monoscripts 的 `m_ClassName`，或读 `MonoBehaviour/*.json` 的独有字段）后重判 |
| **755** | HUD | 残差 = `activeAbilityTextReferenceId`（原版 `+0x248`）数据列，来源 = 1,351 个卡 SO | 数据列落地（见 §一 组 H-D）；另 `DoesAbilityRequireLoading` 的 `HasRemovedTrait(reload)` 在 `activeEffects` 循环体内的边界（`CardScript__DoesAbilityRequireLoading.c:38-45`，我们无条件 `&&`，今天不可观测） |
| **195** | 特效 | `CardEffectItem.LoadEffect`：三条差异**全部**落在「原版 `CardEffect` 有、`UnitState.TempBuff` 没有」的字段上（`buffType` / `visibleForPlayers`+`hiddenForPlayers` / `effectName`），而 `RuleEngine/Core/UnitState.cs` = **E4 的地盘** | `TempBuff` 扩档三项后，在 `CardDisplayWindow.RowsOf`(`:851`) + `SetEffectRows`(`:916`) 逐句落地。⛔ 别用 `TempBuff.Name` 反推 `buffType` |
| **198**（B） | 特效 | `CardEffectsService.EnableEffects`：① 已复刻（`B88_E6`：`SetEffectRows` + `IsEmptyRow` + `EffectRowCount` = 非空条数，`:916`/`:926`）；② 判 **D**（`root.SetActive(!card.IsEnviromentaleffect())` 在本载体**恒不可达** —— 唯一入口 `BattleDriver.OpenOffensiveCardWindow` 走 `Show(d)` 重载 ⇒ `effects == null` ⇒ 非空条数恒 0）；③ **仍缺（数据侧）**：进槽前**倒序过滤** `buffType ∈ {6, 0x23, 0x1e}` 或 (`0x10` 且 `!HasQuests`)（`DisplayCardEffects.c:44-53`）—— 要 `TempBuff.buffType` + `HasQuests`，两者都没有 | **与 195 同一笔阻塞**（`UnitState.TempBuff` 扩档）⇒ 一起做。⚠️ 连带：`特效动画:191/192/196/197`（A 行）正文里写的 `on = i < n` 现在多了 `&& !IsEmptyRow(...)` 一半 —— 语义未变、措辞待同步 |
| **521** | 特效 | `EntityScript.GetCardEffectsToShow`：整条投影未建。阻塞 = `UnitState` 的 **`Seq`**（`_effectSeq` / `StatMod.Seq` / `TraitEffectRecord.Seq` / `TempBuff.Seq` + 光环重算保戳快照）+ 各族记录面接齐 | `UnitState.cs`（S2）+ S0/S3 记录面（施工细则 `判据/GetCardEffectsToShow_施工细则.md`） |
| **403 ②③** | 特效 | 原版「藏体 1.2 s 再销毁」与 `onFinish`（`DestroyUnitSilent`）需要「**被并掉那张**」的身份/位置带到表现层，而 `BattleEvent` 现在**只带存活那格** | 引擎事件扩档（`RuleCore` 结算处带被并卡的 slot/instanceId） |
| **498** | 特效 | `StopTargetedSpellAnim`：**判据本身没读** —— 要先逐卡读 1,351 张 SO 的 `targetedSpellAnim` PPtr 落一张「卡 id → PPtr/GUID」表。全表为空 ⇒ 本行连同 `StartTargetedSpellAnim` 一起判 **D**（原版本身没有）；确有值 ⇒ 先做 `StartTargetedSpellAnim` 再做本行 | **需一次数据普查**（`allcards` 本地可读）⇒ 建议派一个只读代理先出这张表 |
| **507** | 特效 | `CardToMinionAnim` 的 `PlayInvokeAnimation` 那一半：① 那支 clip 名字面量在 `BattleCardUI` **静态块**（`BattleCardUI__PlayInvokeAnimation.c:20-22`，`**(DAT_1842c3528 + 0xb8)`）—— **没读成字面量**（旁证强但不是字面量）；② 我们的等价片 `CardFeel.HandToBoard` **开头就** `if (card.Face == CardFace.Board) return null;`，而 AI/自动打出那条路的视图**已经是 Board** ⇒ 直接复用**不会播**，要播得先把视图退回手牌形态（牵动 `SyncBoard`/卡面/高亮三条链） | ① VA 反汇编 + `.rdata` 读串（需读 `BattleCardUI` 静态块）；② 「AI 落地视图要不要先退回手牌形态」是**形态语义改动**，须**单独立账 + 用户/调度台裁定**。⚠️ 玩家部署那条路**已经**在 `StartHandToBoardClip`（0.3 s）播过同一支 ⇒ ⛔ 别加两处（会播两遍） |
| **516** | 特效 | `ResolveAttackAnimationEffects`：只剩 **命中粒子两档**（`this+0x1b8` 小 / `this+0x1c0` 大，按 `GetUnitSize`），我们只接 `VfxMap.Hit → AttackHitSmall` 一件。🔴 **「命中音三档」那半已判不是缺口**（三个 cue 指向同一个 pathID `-3640304923993188049`） | 特效线（VFX prefab 装配）：需先确认 `AttackHitBig` prefab 本地有没有（`VfxMap.cs:366` 注释提到原版 `attackHitSmallParticles` / `attackHitBigParticles`） |
| **537 / 689 / 704** | HUD | 均卡在**真 Play**（批处理不跑 `Application.isPlaying` 那条闸）；689/704 另卡服务端结果语义（`battleWinner==10` / `resultType==0x14` / `matchResult+0x24==2 Forfeit` 来自 `RankedMatchResult`） | **主对话在同步点跑真 Play**；服务端语义部分见 §四候选 |
| **741** | HUD | 见 §四 | | |

---

## §四 建议判 D（必须符合铁律 11 例外①，带证据）

| 账行 | 域 | 建议 | 证据（判据为空 / 原版本身没有） | ⚠️ 边界 |
|---|---|---|---|---|
| **490** | 特效 | **D** | `BattleManager.cs:19514 RemoveVoiceLinesControllerListeners` = 从 `VoiceLinesController(+0xb0)` `remove_OnPopUpShow` / `remove_OnPopUpHide`。我们**没有 popup 订阅链**（语音按需 `Has`/`Play`，无 popup 事件）⇒ 该反注册副作用在我们的架构里**不存在**，属**结构差异、无对应物**（同 `712 GetUIState` 那一类的先例） | ⚠️ 账里已被调度台从 B 降到 C，本条是**再降 D**。⛔ 别拿 `VoiceLines.Has` 当落点（那是读取口，原备注已判错） |
| **741** | HUD | **D（建议，可争议）** | `BattleManager__Update.c:97-1413` 的 B08 段（教程「等玩家用主动技」）**通篇只有一条 `LogWarning`、零局面/零画面影响**。我们已有 `Ctx.Tutorial` 教程支 ⇒ 缺的只是一条诊断日志 | ⚠️ **严格说不符**例外①（原版确实有这段代码）；但「零可观测行为」与 `712` 的先例同构。⇒ **这一条我不自行定 D，请调度台裁定**（若要保留，则归 `BattleDriver`、等 E5，工作量 = 一条日志） |
| **689① / 704** | HUD | **部分 D 候选**（不是整行） | 689 的 `battleWinner == 10` / `resultType == 0x14`，704 的 `matchResult+0x24 == 2 Forfeit` —— 三个值都来自**服务端 `RankedMatchResult`**，属 P3 数据（原版调用链断在这儿） | 整行**不能判 D**（时间表那 4 段已实现且可验）；只这三格可判 D + 写清「值来自服务端、本机拿不到同一语义」 |

> ⛔ 本单**不擅自把任何行改成 D**（我是只读代理）；以上全是**建议**，由调度台裁定并写进账。

---

## §五 并行方案（文件所有权 = 唯一硬约束）

### 可同时派（文件两两不相交）

| 组 | 文件（写手独占） | 条数 | 内容 |
|---|---|---|---|
| **P1** | `CardPresentation/Hand/CardInteraction.cs` | 2 | 695（`DragBlocked` 加 `IsDoingAnim`）+ 761（敌手 Sabotage 接线）。**同文件 ⇒ 串行**，但整组只占一个写手 |
| **P2** | `CardPresentation/Net/NetRuntime.cs` | 2 | 805 + 806（常驻连接横幅 + `floor(Repeat(t×3,4))` 点数 + 居中 400×100） |
| **P3** | `CardPresentation/Core/CardFeel.cs` | 1 | 802（残骸粒子 y→0，落点 `:2921 Vector3 pos = root.transform.position;` 与 `:2927 pos = node.position;`） |
| **P4** | `RuleEngine/Core/CardDef.cs` + `RuleEngine/Data/CardDatabase.cs` | 2 | 20（ability 级 `targetsAvailableCounter`）+ 755（`ActiveAbilityTextReferenceId`）。**同一批文件 ⇒ 串行**；⚠️ 两条做完**都没有可见变化**（该格真值 0 条 / 数据源未到）⇒ 只补判据 |
| **P5** | `RuleEngine/Core/RuleCore.cs` | 3 | 486 发侧（照 `B88_E5_AbilityPriority_发侧施工单.md`）+ 503①（huntMark 死者事件）+ 537（`EndTurnCore` 清零前的 `Energy`） |
| **P6** | `CardPresentation/Board/ArenaSlots.cs` | 1 | 713（`InPlayScale(enemy)` = `0.75 × (enemy?0.86:1)`） |

**P1–P6 = 6 个写手可全并行**（文件互不相交，且都不碰 `BattleDriver.cs`），共 **11 个账行**（HUD 5 + 特效 6）：
P1 = 695 / 761 · P2 = 805 / 806 · P3 = 802 · P4 = 20 / 755 · P5 = 486 发侧 / 503① / 537 · P6 = 713。
> ⚠️ 其中 **P4 两条、P5 的 486 发侧** 落地后**行为不变**（数据真值 0 条 / 缺生产者）⇒ 它们的价值是「把判据从缺变有」，验收只能靠**算式与指纹断言**，别指望画面变化。

### 必须串行的

| 原因 | 组 |
|---|---|
| **`BattleDriver.cs` 本波归 E5** | H-E 全组（741 / 759+493 / 713 的接线半 / 208 / 712① / 486 的 `ScheduleAttackPresentation` 半 / 403 的 `PlaySwarmMergeFeel` 半 / 503②）+ 特效 484 / 492 / 485 |
| **同文件多写手** | P1（2 条）· P4（2 条） |
| **同一笔改动横跨两个组** | 759+493 必须**一次**改 `PlayTraitPulse`（拆开会导致「删了脉动但没接 yoyo」的中间态） |

### 依赖链（不能并行）

```
E5 交回 BattleDriver.cs
   ├─→ 503②（PlayTraitParticles 按目标格播）  ← 需 P5 的 503① 先发事件
   ├─→ 208（PickOffensiveForAi 改走 OffensiveChoicesOwnedCore）
   ├─→ 486 的 ScheduleAttackPresentation lane（需 P5 的 486 发侧）
   └─→ 759+493（PlayTraitPulse）
P5（503①）──→ E5（503②）
P5（486 发侧）──→ E5（486 lane）
364/378（录像候选集）：E5（BeginAsk 生产者）──→ 回放读侧（ReplayStore，谁都能改但没生产者时是死件）
```

---

## §六 顺手发现（⛔ 我没改任何东西，只记录，由调度台分流）

1. **🔴 `enemyUnitsScaleRatio` 有真消费者，账里 713 的框架是错的**（本单 §一 组 H-E 已展开）：`GetUnitSizeRatio` 三源 0 调用者 = **死方法**；真消费者是 `GetUnitSizeInPlay` 的 **else 支**（非 Minion/非 Hero 在场卡）= `0.75 × (enemy ? 0.86 : 1)`。资产真值 0.86（`GlobalVariables.asset:38`），**C# 默认 0.93 是死值**。
2. **🔴 `animFlag` 不是 0 写点**（`特效动画:497` 备注里的断言）—— ILSpy `PlayerHand.cs:858/950/1302/1457` 四处写、`BattleManager.cs:46532` 读（就在 `CanMoveHandCard` 里）。⇒ `695` 是**真缺口**，且等价物 `CardInteraction.IsDoingAnim` 已在同一类里。
3. **🔴 账行号普遍漂移**（本轮现读）：`ShowSkillPanel` 15587→**15705** · `ShouldShowAbilityTargetsCounter` 35031→**35291** · `OnCardTapped` 13149→**13248** · `BattleUiIsNormalBattle` 33952→**34198** · `RevealEndPx` 9860 一带仍在。⇒ 施工单**一律用符号名**。
4. **🔴 `read_literal` 的默认 stdout 编码是 GBK**：直接跑会 `UnicodeEncodeError`（`✅` 打不出来），**必须 `export PYTHONIOENCODING=utf-8`** 再跑。这条以前没记过，会反复踩。
5. **`BattleCardUI.DisplayTriggerAnim` 不调 `HighlightTraitIcon`**（全文亲读 `decomp_full/BattleCardUI__DisplayTriggerAnim.c`）⇒ 血渴那一支原版**只有**「框淡出（框恒 null ⇒ 不跑）+ trait 动画」，**没有徽标脉动** ⇒ 759 的「多出一下」判据成立。
6. **`ShowStatusWhenConnecting` 的点数节奏不是「每秒一个」**：`floor(Repeat(t × 3, 4))` ⇒ 每 **1/3 秒**加一个点、4 个循环。
7. **`ChooseCardMenu.Setup` 的 `actingCardId == "offensive"/"defensive"` 还会 `chooseMenuAnchor.SetActive(false)`**（记账 378 现读已补），我们**没有 `ChooseCardMenuAnchor` 这个节点**（卡片行直接挂面板下）⇒ 该句**无对应物**。
8. **`GetUnitSizeInPlay` 的 `else` 支在我们的棋盘上今天没有观测面**：`PlayerState.Board` 是 `UnitState[]`（`PlayerState.cs:69`），而卡池类型分布 = unit 586 / tactic 446 / hero 56 / **defence 39** / enchantment 2 ⇒ 非部队/非督军的在场卡（defence / enchantment）**尚未进 Board** ⇒ 补了 `ArenaSlots.InPlayScale` 也没画面。⚠️ 但**仍要补**（铁律 11：都做、只分先后），只是验收只能靠**算式断言**。
9. **`493` 与 `759` 是同一处代码**（`BattleDriver.PlayTraitPulse`），两条账**必须一次改完**，否则中间态是「删了多出的脉动、但没接 6 次 yoyo」。
10. **`755` 的 `34 条拼不出来` 里 30 条是 `Effect<名>`**：它们**不是键**，是原版 `activeAbilityTextReferenceId.Contains("((")` 那一支「整条跳过本地化、把 id 当正文」的正文（`RawCardScript.cs:2384-2405`）。⇒ 补数据列时必须**连那一支一起补**，否则那 30 条仍会走去查键、显示模板句。
11. **🔴 `497 StopHandAnim` 其实是【已做完】的**：账里那句「本仓**没有**『延时收教程指针』这一跳 ⇒ 记缺」**不成立**。现读 `BattleDriver.cs` 四件全在 —— `:16687 PlayPendingBerzerkTriggerAnim()` / `:16689 ShowPendingBerzerkPointer()` / `:35550 PendingBerzerkPointerSeconds = 2.0f` + `:35597` 里 `_berzerkPointerOffAt = _clock + 2.0f` / `:35621 TickPendingBerzerkPointer`（`_clock` 泵，批处理可推）/ `:35633 TickPendingBerzerkPointerForTest`。⇒ 建议升 A。**同族提醒**：它旁边那段注释（`:16520-16535`）说明「`Loc.T` 缺键返回键名本身」是**已修**的缺陷 —— 别把 `:16527` 当成 pending-berzerk 的落点（那里是 `Loc.HasEntry` 的说明）。
12. **`OffensiveCards.DumpDraftAvailabilityForTest()` 在 `OffensiveCards.cs:226`**（现读），`208` 的验收可以直接用它（⛔ 别在自检里另写阈值）。

---

## §七 没查清（⛔ 不许拿猜测填空）

| 编号 | 没查清什么 | 还差什么 |
|---|---|---|
| U1 | **`PressedBtn` 节点到底在不在本地**（账 684/687 / `ActiveWarlordAbility.OnTouchUpAsButton` 的 ④） | 账的证据是 `assets_full` **按名字**搜。按 `CLAUDE.md`「判资产本地没有不能按名字搜」，要改走 **`m_Script` pathID → monoscripts `m_ClassName`** 反查，或读 `MonoBehaviour/*.json` 的独有字段。**没重查之前 ⛔ 不判 D** |
| U2 | **`AbilityLogic.PlayAnim.c:62-64` 那个虚谓词**（`AssetReferenceTyped<T>` 的 vtable 槽）⇒ 「这一拍要不要播」仍判不了 | 需要 VA 级反汇编那个 vtable 槽的调用点（前一路 `B9_W10BDR` §三·1 未解） |
| U3 | **`BattleCardUI` 静态块里 `PlayInvokeAnimation` 的 clip 名字面量**（`**(DAT_1842c3528 + 0xb8)`） | VA 反汇编 + `.rdata` 读串。旁证（同方法末尾用 `GetHandToBoardAnimEventTime`）很强，但**旁证 ≠ 字面量** |
| U4 | **`ShowStatusWhenConnecting` 的基串** `DAT_1842b80e0`（横幅正文） | 是字符串对象，`read_literal` 只能读数值字面量 ⇒ 要读托管字符串的字符块（或从官方本地化表反查 key） |
| U5 | **`targetedSpellAnim` 全池有几张非空**（498 的前置） | 逐卡读 1,351 个 SO 的 PPtr ⇒ 建议先派只读代理出表 |
| U6 | **`VfxMap` 的 `AttackHitBig` prefab 本地有没有**（516 的 ②） | 按组件/sprite 反查（⛔ 别按名字搜就下结论）；`VfxMap.cs:366` 只说原版有 `attackHitBigParticles`，没说我们有没有 |
| U7 | **`689/704` 三个服务端值的本地替代语义**（`battleWinner==10` / `resultType==0x14` / `matchResult+0x24==2`） | 需确认我们的 `Ctx` 有没有等价结果字段；没有 ⇒ 写清「本机拿不到同一语义」并按 §四 判 D |
| U8 | **`GetEnemyCardPlayedDisplayPosition` 的非 1080p 档像素落点** | `screenHeightPx` 传死 1080（与 `RevealEndPx` 同口径）⇒ 别的分辨率下会偏；判据未算 |
| U9 | **`741` 到底算不算缺口**（零局面/零画面的诊断日志） | 需调度台按铁律 11 例外① 裁定；本代理**不自行定 D** |
| U10 | **`LanDeployFlight` / AI 落地视图退回手牌形态**（507 的第 ② 条） | 属形态语义改动，牵动 `SyncBoard` / 卡面 / 高亮三条链 ⇒ 得单独立账 |
| U11 | **`497` 归位后剩下的两条真偏离**：① `to`（拖拽终点）—— `TutorialOverlay.SetPointer(bool, Vector3)` 是**单点口**，原版是双点（`StartPointer(clickAndDrag, from, to, 1)`）；② 非教程局不画（原版 `TutorialPointer` **恒在**、这一档所有对局都亮） | ① 需把 `SetPointer` 扩成双点口（+ 拖拽终点判据：`Get2DPosOfCard(目标)`）；② 需决定「非教程局也建 overlay」= `EnsureTutorialOverlay()` 的建层闸要不要放宽（会与 `485③` 同源）。**两条都不在 497 的账上**，建议另立新账 |

---

### 附：本单与既有施工单的关系（⛔ 别重复造）

| 既有文件 | 已覆盖本单的行 |
|---|---|
| `判据/B9_特效C_施工单.tsv` | 特效 86 / 208 / 403 / 498 / 507 / 508（逐条步骤已在，直接照做） |
| `判据/B9_剩余C_施工单.tsv` | 特效 195 · HUD 687 / 805 / 806（⚠️ 805/806 的「每秒一个点」口径本单已订正为 1/3 秒） |
| `判据/B9_基础设施与未分类C_施工单.tsv` | 特效 486 / 490 / 506 / 507 / 516 |
| `判据/B88_E5_AbilityPriority_发侧施工单.md` | 特效 486 的发侧四处（本单直接引用） |
| `判据/B88_AddCardAnim_priority表.tsv` | 特效 486 的 12 个调用点（含 `ScheduleAttackPresentation` 恒 lane 0 那一格） |
| `判据/GetCardEffectsToShow_施工细则.md` | 特效 521 的 S0–S7 全套 |
| `判据/BattleManager_Update_13分支_判定.md` | HUD 741 的 18 段总表 |
| `判据/enemyHand屏幕锚点_判定.md` | HUD 750 / 756 的 (960,50) 锚点 |
