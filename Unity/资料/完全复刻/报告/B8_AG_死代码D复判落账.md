# B8_AG · 死代码 D 复判 —— 落账报告

> 执行代理 AG（代号 `B8_AG`）· 2026-10-26
> 输入 = `判据/B8_死代码D复判.tsv`（163 行 · 判据代理 V）＋ `报告/B8_V_死代码D复判.md`
> 产物 = **`账/_parts/B8_AG.tsv`**（163 行 × 11 列 · LF · 只放改过的行）
> ⛔ 未改任何 `.cs` · 未跑 Unity · 未动 git · **未改账主表** · 未改正本 · 未动别人的补丁。

---

## ① 结论（数字）

| 项 | 数 |
|---|---|
| 落账总行数 | **163**（P0 四域 132 + 非 P0 已核 31，与 V 的 TSV 一一对应） |
| **改成 `A`** | **19** |
| **改成 / 记 `B`** | **47** ＝ **27**（V 建议 A、但取不到方法级声明落点 ⇒ **降 B**）＋ **20**（V 建议 B、状态列原为 D ⇒ 按备注订正为 B） |
| **改成 `C`** | **5** |
| **保持 `D`** | **92** |

- **「A 因取不到声明行而降 B」= 27 条**。再拆：
  - **21 条**：V 的「我们落点或工作量」那一格直接写 `—（不需要）`／`—（无独立行为）`／`—（不该实现…）` ⇒ **没有任何声明行可指**；
  - **6 条**：落点只能到**字段 / 数据行**（`SetHealthToZero→Hurt` 那条连行号都是错的）⇒ 按 `方案 §4.1` 记 B。
- **A 的 19 条全部过闸**：`check_ledger_points` 贴到补丁上跑 = **A 19 · 通过 19 · 漂移 0 · 失败 0 · 告警 0**；`verify_annot` 复刻 = **缺注解 0**（19 条都是「异名 + `（等价实现 = …）`」）。
- **7 条「原版出货数据 0 入口」按调度台政策①处理**：保持 `D`，理由**整条改写**成 `D 依据：原版出货数据 0 入口（本 build 池内 0 张可达）；时间限定 = 若数据集变化需重开`（`HasSingleUseMutation` / `SetAsActiveTrap` / `TrapResolved` / `TriggerOtherCardLanding` / `OnTrapDrawn` / `SetEnemySecret` / `AddDrawTrapAction`）。
- **29 条「状态列 D、备注自己说 B」按政策③落成 B**（TSV 里 V 已把它们算进建议 B）：`卡牌面_渲染` 7 · `收藏` 8（另 9 条 `ItemDrawer.ItemDrawer.*` 仍 D，见 ④）· `教程_引导` 3 · `卡组编辑` 2。备注里统一写入「状态列原与备注矛盾，2026-10-26 按备注订正」。

---

## ② 与 V 建议不一致的逐条（27 条，全部是 A→B）

判法（本批统一）：`方案 §4.1` 要求 A 的**双侧都到方法级**，我们侧必须给 `<文件>:<行>` 且那行是**声明行**；给不出 ⇒ 不能记 A。⛔ 我没有为了凑 A 去指一段「并不是它」的代码。

| # | 账行（域:行号） | 原版方法 | V 建议 | 落账 | 为什么 |
|---|---|---|---|---|---|
| 1 | 战斗逻辑驱动:880 | `CardScript.HasMutation` | A | **B** | V 落点格 = `—（不需要）`；我们**没有 mutation 维度**，无实现可指 |
| 2 | 战斗逻辑驱动:990 | `CardScript.ResolvePsyker` | A | **B** | 同上；原版本体离线不可达，我们也没建灵能资源 |
| 3 | 战斗逻辑驱动:1022 | `CardScript.SetHealthToZero` | A | **B** | V 的落点 `RuleCore.cs:8568（Hurt）` **两处都不对**：8568 不是声明行（`Hurt` 在 **8742**），且 `Hurt` ≠ 裸置血 0；我们没有「裸置血 0」这个口 |
| 4 | 战斗逻辑驱动:1074 | `CardScript.TriggerMutation` | A | **B** | 落点只能到 `CardDef.cs:1937` 的**名字表项**（数据行，非声明） |
| 5 | 战斗逻辑驱动:1140 | `CustomAbilityLogic.CanPlayAbility` | A | **B** | 原版恒 `return true` + 全仓 0 子类 ⇒ 我们无对应基类 |
| 6 | 战斗逻辑驱动:1141 | `CustomAbilityLogic.PlayAbility` | A | **B** | 原版空体 + 0 子类 ⇒ 同上 |
| 7 | 战斗逻辑驱动:1177 | `EntityScript.GetMarksOfChaos` | A | **B** | 落点只能到**字段** `_keywordsPact`（`EffectResolver.cs:12295`，只记最后一份） |
| 8 | 战斗逻辑驱动:1178 | `EntityScript.GetMutations` | A | **B** | 同 #1（无 mutation 维度） |
| 9 | 战斗界面_HUD:92 | `BattleCardManager.CheckToBeAlternate` | A | **B** | V 自己写「—（无独立行为）」；AA 查表内联在 `CreateCard` |
| 10 | 战斗界面_HUD:243 | `BattleScoreUiManager.ScoreUpAnimation` | A | **B** | V 写「—（不该实现这颗动画）」⇒ 无实现可指；口径（B 还是 D）待裁 |
| 11 | 战斗界面_HUD:349 | `CemeteryManager.DisplayCardTraits` | A | **B** | 原版是空方法 ⇒ 无落点 |
| 12 | 战斗界面_HUD:386 | `ClockManager.ChangeMaterialHurryUp` | A | **B** | 0 调用点、我们无该材质切换 ⇒ 无落点 |
| 13 | 战斗界面_HUD:405 | `ClockManager.SetFillPercentage` | A | **B** | 原版空方法（`ClockManager.cs:490-492`）⇒ 无落点 |
| 14 | 战斗界面_HUD:532 | `ManaBuff.DeepCopy` | A | **B** | 0 调用者、我们不需要拷贝口 ⇒ 无落点 |
| 15 | 战斗界面_HUD:533 | `ManaBuff.GetNewInstanceOfThisEffect` | A | **B** | 同上 |
| 16 | 战斗界面_HUD:578 | `PracticeModePopup.IsSavedDeck` | A | **B** | 零调用 + private/bool 挂不上 UnityEvent；我们不复刻这条「只写不读」的口 |
| 17 | 卡组编辑:144 | `DeckGeneralInfoDemo.SetUnlockState` | A | **B** | 教程锁走 `TutorialModePopup` 另一条链 ⇒ 无对应实现 |
| 18 | 卡组编辑:200 | `DeckManager.ToggleCardbackPreview` | A | **B** | 0 调用点 + 消费者侧无行为可对齐（「域外」占位已清掉） |
| 19 | 卡组编辑:255 | `DemoDeckInfoSO.SetTutorialIndex` | A | **B** | 落点只能到**字段** `TutorialModePopup.StageRow.Idx`（:251） |
| 20 | 卡组编辑:279 | `PrebuiltDeck.IsIncompleteLibrary` | A | **B** | 落点只能到**生成期数据字段** `Deck.complete/missing`（`PrebuiltDecks.cs:98/99`） |
| 21 | 卡组编辑:280 | `PrebuiltDeck.RefreshList` | A | **B** | 体只有「置空表」一句、0 调用点；我们无缓存层 |
| 22 | 卡组编辑:281 | `PrebuiltDeck.SetHero` | A | **B** | 落点只能到**字段** `Deck.heroId` |
| 23 | 卡组编辑:292 | `PrebuiltPack.SetCardLibrary` | A | **B** | 0 调用点；我们的预组包生成期装好、无运行期灌库 |
| 24 | 未分类_其他:201 | `CosmeticItem.UnloadReferences` | A | **B** | 我们图本地导入、**没有句柄释放这一层**，无方法声明可指 |
| 25 | 未分类_其他:221 | `CursorByArmySO.ReleaseArmyCursor` | A | **B** | 同上 |
| 26 | 未分类_其他:304 | `FlavourTextSO.ReleaseFlavorBackground` | A | **B** | 同上 |
| 27 | 音频_语音:74 | `SoundAsset_OLD.GetDuration` | A | **B** | 落点只能到 BCL 的 `AudioClip.length`（不是我们的声明）；若要同名封装 = C（1 行） |

**其余 136 行的 `状态` 与 V 的建议一致**（A 19 · B 20 · C 5 · D 92）。差异只在内容层（见下节「顺手改掉的硬错」）。

### 顺手改掉的硬错（都在补丁里落定，铁律 5）

| 位置 | 原来 | 现在（现读） |
|---|---|---|
| `AI.CombatAttackWithType` ×2 落点 | `RuleCore.cs:4884（FieldAttack）` | **4939** —— 4884 现读是**注释行**（这就是 V 抄下来的那格；直接拿它升 A 会被闸判失败） |
| `BattleManager.GetPlayerGoesFirst` 落点 | `FirstSeatRules.cs:1-31` | **`FirstSeatRules.cs:77（ByInitiative）`** —— 1-31 是文件头注释块 |
| `BattleCardUI.GetTraitListForCardIcons` 落点 | `Badges.cs:532（For）` | **570** —— 532 现读在 `PlaysInPlayIcon` 体内，`For` 的真声明行是 570 |
| `BasicCardUI.ColliderBigSize` 落点 | `CardView.cs:9319（SyncHitCollider）` | **9324** —— 9319 是调用行（`SyncHitCollider();`）+ 几何 `:9406（HitRect）` |
| `CardScript.SetHealthToZero` 落点 | `RuleCore.cs:8568（Hurt）` | **撤下**（8568 不是 Hurt 的声明行；`Hurt` 在 **8742**，且语义不同） |
| `CursorManager` Start/Update/OnGUI 差异 | 「死代码 + `Cursor.visible=false` 的闸是 `SteamUtils…` + `cursorTextureNormal` 0 命中 ⇒ 贴图 null」 | **整条重写**：唯一实例 `m_IsActive = false`（现读）⇒ 三个回调都不跑；两份反编译的 `Start` 第一句是**无条件** `Cursor.visible = false;`（**没有** SteamUtils 闸）；`cursorTextureNormal` 那半句**无效证据**（`level0` 无 type tree、该组件体未 dump） |
| `UnityServicesManager.InitializeUnityServices` 备注 | 「保持 C，**等用户裁定**」与状态列 D 并存 | 旧口径**作废**、只留 D（原版服务端）+ 时间限定 |
| **24 条** D 行的 `差异` 拿「死代码」当唯一理由 | `BattleDeviceCompatibility×2` · `ResolutionManager×4` · `TipPrebattle×10` · `UISizeManager×2` · `YourTurnUI×3` · `CursorManager×3` | 改成**依据族**（原版 0 实例 / 唯一实例 inactive / 移动端预设不挂）+ 补时间限定 —— ⛔ 不再用「死代码」四个字当唯一理由 |
| **14 条** D 行的 `差异` 是空的或只有占位（`无` / `不做（原版无体可复刻）` / `不适用…`） | `ItemDrawer.ItemDrawer.*`×9 · `ListExtensions.RemoveWhere` · `MilestoneScoreBar` · `UITextGradient` · `WhereToGetCardsTester` · `UnityServicesManager` | 每条补一句**可复核的依据**（V 的「顺手发现 §2」正是这类行：`差异` 为空 ⇒ 任何按 `差异` grep 的审计都会整批漏掉） |

---

## ③ 补丁统计与校验

| 项 | 结果 |
|---|---|
| 文件 | `d:/4/Unity/资料/完全复刻/账/_parts/B8_AG.tsv` · **180,313 B** · **164 行**（表头 + 163）· **LF · CRLF 0 · 裸 CR 0** |
| 列数 | **164/164 行恰好 11 字段**（脚本内逐行 assert，且 `方法` 全为单方法签名） |
| 身份列 | `域/原版类/方法/判据出处/判据要点` **与主表逐字节相同**（补丁只改 状态/落点/差异/等级/轮次/备注） |
| `check_patch_conflicts.py` | **✅ 没有跨补丁重复键**（收尾时 `_parts/` 只剩本补丁；并发的 W/W2/AJ 已被各自的合并归档） |
| `merge_ledger_parts.py --dry --only B8_AG` | **更新 163 · 新增 0 · 无变化 0**，9 份账（战斗逻辑驱动/战斗界面_HUD/卡牌面_渲染/卡组编辑/收藏/教程_引导/未分类_其他/菜单_外壳/音频_语音），无「命中>1」或签名拒绝 |
| `check_ledger_points.py`（**把补丁当账本跑**） | **A 19 · 通过 19 · 漂移 0 · 告警 0 · 失败 0** |
| `verify_annot` 规则复刻（补丁） | 同名 0 · **异名已注解 19** · **缺注解 0** |
| `check_a_vs_diff` 规则复刻（补丁） | **A 行命中 0**（19 条 A 行都在 `差异` 末尾带 `[非缺口]` + 一句「这是等价机制说明 / 原版 0 调用点带来的无可观察量」） |
| 主表上的两条官方自检（我**没**改主表，跑给基线） | `check_a_vs_diff` **强 0 · 弱 29**（= **当前**主表基线；我开工时的基线是 23，中途别的代理又合进了 6 条）· `verify_annot` **缺注解 0**（同名 711 · 异名已注解 ≈2.4k，随并发合并变动） |
| **端到端演练**（把 21 份主表复制到 `_tmp_view/ag/sim/ledger`，真跑一次 merge；数字是**收尾那一刻现读**，主表此刻仍在被并发代理改写） | `verify_annot` 复刻 **A 3102 · 缺注解 0**；`check_a_vs_diff` **强 0 · 弱 29**（= 当前主表基线**同数**，本批未新增；`[非缺口]` 跳过 314 → 333 = **+19**，正是本批的 A 行）；`check_ledger_points` **通过 2910 · 漂移 191 · 失败 1** —— **漂移 191 / 失败 1 与我合并前的主表基线一模一样**（见 ⑤-3），本批 19 条 A **全部通过** |
| 主表是否被我改过 | **没有**。`git status` 里主表 §tsv 的 ` M` 与 `_parts/` 的 ` D` **都是并发代理（W / Y / AJ）合并留下的**，我的写入只有 `_parts/B8_AG.tsv`（`??`） |

---

## ④ 没查清的部分（如实写）

1. **27 条 A→B 的最终归属需要调度台口径。** 我的判法只用了「有没有方法级声明落点」这一条机器判据；它们在语义上更接近「原版无可观察量」。⇒ 若调度台认「原版无行为 = 已对齐」，这 27 条可再升 A（但那样 A 就没有「实现」可指，与 §4.1 的收紧口径冲突）；若认「原版没有 ⇒ D」，则应改 D。**我没有替调度台做这个取舍。**
2. **19 条 A 行的 `[非缺口]` 是结论、不是消音器**，它的前提是「原版 0 调用点 ⇒ 无可观察量」。其中 **1 条我建议重点复核**：`CardScript.GetAvailableMarksOfChaos` —— 原版那个「池内全部印记种类 − 已挂的」**差集查询我们确实没有对应物**（只有「最后一份种类」+ 层数）。今天 0 调用点 ⇒ 无观测量，但**数据一旦变化就是真缺口**（约 10 行）。
3. **7 条「数据 0 入口」的 D 全部带时间限定**（本 build / 1,351 张 SO 现读 0 张）；这些是**会随数据集变化翻案**的行，不是永久 D。
4. **非 P0 的 129 行**（V 只做了机械扫描：特效动画 36 · 菜单_外壳 31 · 未分类_其他 21 · 收藏 19 · 战役 8 · 社交 6 · 日常 3 · 音频 2 · 联机 2 · 轮抽 1 · 其他 1）**仍没有逐条人工复核** —— 不在本批范围，我也不重查（避免与 V 重复劳动）。TSV 里那 31 行非 P0 是 V 额外核过的，本批已落账。
5. **VA 级反汇编本批没有重跑**（V 也没跑，3500 条不现实）⇒ 对**独特方法名**置信度高、对通用方法名（`Toggle`/`Start`/`IsValid`…）置信度低。`IsValidActiveAbilityTarget` 那条「0 直调但属接口实现」的反例提醒：接口/委托/反射调用不被任何文本 grep 覆盖。
6. **`ItemDrawer.ItemDrawer.*` 那 9 行**（收藏 263/270/275/277/279/283/285/288/294）我**保持 D**、**没有执行「并入 `ItemDrawer<T>.*` 那条」的账务合并** —— 那是主表结构改动，必须单写手，归调度台（见 ⑤）。
7. `收藏:203 GameModesIconCollection.Release()` 的**二选一仍未裁**（建句柄层 = B / 写域口径 = D）。按 V 的建议我落成 **B** 并在备注里写明「二选一未裁、本行不做取舍」。

---

## ⑤ 给调度台的请求

### 5.1 下一波的 C（按建议顺序，全部已在补丁里带「缺什么 + 等级 + 落点文件 + 工作量」）

| 顺序 | 账行 | 方法 | 等级 | 工作量 / 落点 | 为什么排这个位置 |
|---|---|---|---|---|---|
| **1** | 卡组编辑:290 | `PrebuiltPack.IsValid()` | **P1** | ≈10 行；轮抽取包那条（`CardPreview/Shell` 预组装载侧） | **本批唯一「0 调用点」被证伪**的行（活调用点 `DraftEventBase.cs:378`）；最小、最便宜，且顺手修掉一条假 D |
| **2** | 菜单_外壳:612 | `ScrollViewFocusFunctions.FocusOnItemCoroutine(...)` | **P1** | ≈40 行；锻造奖励选取（`ForgeRewardSelector` 一族） | 有 **2 处活调用点**（`:416` / `:438`）⇒ 玩家能看见「奖励列表不滚动」；锻造线本来就在 P1 |
| **3** | 战斗逻辑驱动:12 | `AI.GetDefensiveAttackValue(...)` | **P1** | ≈20 行；`RuleEngine/Data/AiScoring.cs` | AI 出招偏好；**接进 `ScoreFromAttack` 要单独裁定**（会引入「督军不互砍」） |
| **4** | 战斗逻辑驱动:39 | `AI.SuicideHeroAttack(...)` | **P1** | ≈15 行；`RuleEngine/Data/AiScoring.cs` | 同一族的另一半，可与 #3 同批做（同一文件） |
| **5** | 战斗逻辑驱动:38 | `AI.ShouldPlayComboCard(...)` | **P2** | ≈30 行；`AiScoring` + 池内 combo 语义 | 池内 combo 卡 **0 张** ⇒ 今天不可观测，放最后；数据变了要重开 |

（#3 + #4 同文件，建议合成一个代理；#1 与 #2 分属不同文件，可并行。）

### 5.2 需要调度台拍三句（我不代裁）

1. **「原版无行为（0 调用点 / 空体 / 无可观察量）」到底记 A 还是 D？** ⇒ 决定 ④-1 那 27 条的归属（现在是 B）。
2. **`收藏:203`**：建句柄层（B）还是写域口径改 D？—— 挂了 1 轮多，仍悬着。
3. **`ItemDrawer.ItemDrawer.*` 那 9 行的账务合并**（并入 `ItemDrawer<T>.*`）：需要一次**独占 `收藏.tsv`** 的写手。

### 5.3 合并提示

- 本补丁**与 `_parts/` 下其它补丁无重复键**（收尾时只剩它一份）。合并：
  `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AG`
- 合并后**必做**：`check_ledger_points.py --all-A`（本批落进了 19 条 A，其中 2 条是**由注释行/调用行修正来的**，别的代理若再改 `RuleCore.cs` / `CardView.cs` / `Badges.cs`，这 19 条会先出现「漂移」而不是「失败」）。
- ⚠️ **并发提示（本批实测）**：我读主表期间（约 07:35–07:40）有 **W / Y / AJ 三个代理各自把补丁合进了主表**（`_merged_B8_W/X/Y/AJ` 四份归档目录）。我收尾时重跑了一遍「身份列 vs 当前主表」= **163/163 命中且逐字节一致**，所以补丁**仍然可直接合并**。但这说明「主表同一时刻一个写手」在实际执行里被打破了 —— 若下一批还要并发，建议把合并全部收到调度台一处。
- ⚠️ **落点会随别人的 `.cs` 改动漂**（本批实测 2 次）：`RuleCore.FieldAttack` 我现读 4939、`EffectResolver.GrantDarkPact` 在 20 分钟内从 12039 漂到 **12050**（有代理正在改 `EffectResolver.cs`）。补丁用的是 `路径:行（声明名）` 形态 ⇒ **`check_ledger_points --all-A --fix` 能按名字自愈**；但**合并后必须立刻跑一次 `--all-A`**，别把「漂移」当成「失败」。

### 5.4 顺手报两条（不属于本批，但闸上看得见）

1. 🔴 **主表里有 1 条 A 行**过不了闸（**不是我改的**，是并发代理合进去的）：
   `战斗逻辑驱动.tsv | BattleManager.ProcessManaBuff(bool isPlayer, ManaBuff …)` —— 落点 `RuleEngine/Core/EffectResolver.cs:12488-12505` 是**注释行**（`/// · PlayerManager__AddNewMana.c…`）⇒ `check_ledger_points` 判**失败**。**这一条要改成 `RuleEngine/Core/EffectResolver.cs:12499（DoEnergy）`**（2026-10-26 现读：`DoEnergy` 的真声明行 = **12499**；把 12488 那个注释行起点甩掉，写成 `:12499（DoEnergy）` 即可过闸）。
2. `check_a_vs_diff` 当前 **强 0 · 弱 29**（我开工时是 23，涨的 6 条都来自别的代理新合的 A 行）。弱信号按工具口径是「等价替换的正当解释」，**需要人逐条扫一遍**，别当失败看。
