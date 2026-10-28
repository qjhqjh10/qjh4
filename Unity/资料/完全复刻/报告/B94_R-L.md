# B94 · R-L 账务整理（出处补全 + 体检 + 无落点 五份报告的「账要订正」汇总 · **含 §三 全部裁定**）

> 代理：**R-L（账务整理）** · 第九十四会话 · 波 1.5 · 2026-10-29（第二轮：按 `.c` 逐条裁定 §三）
> 产出：**本报告** + 补丁 **`Unity/资料/完全复刻/账/_parts/B94_RL.tsv`（29 行）**
> ⛔ 未改源码 / 账主表 / 正本 / 所有权表；未跑 Unity；未动 git。
> 输入：`判据/B94_R-A_出处补全.md`（战斗 48）· `判据/B94_R-A2_出处补全_非战斗.md`（95）· `判据/B94_R-A3_出处补全_其余.md`（73）· `判据/B94_R-C_出处体检.md`（381）· `判据/B94_R-B1_无落点_战斗.md`（23）· `判据/B94_R-B2_无落点_其他.md`（25）。
> 第一权威 = `D:/2/tools/decomp_full/`（本报告 §三 的裁定全部**逐句读 `.c`** 得出，泛型类走 `decomp_generic_trial/out{,_b94}/`），二源 = `cpp2il/out_cpp2il_dev_ilspy/`。

## 〇 · 两条必须先读的结论

1. 🔴 **`R-A/R-A2/R-A3` 的「账现值」都是从 `判据/B94_派活.tsv` 那份快照读的，不是账主表现值。** 主表过去一天已被 `B93_*` 与**本轮在飞的 `B94_{F1,F2,F3,F4,F5,RI,S1,S2,S3,S4,T1,V1,W1,X1,X2,Y1,Z1}`** 改过多轮 ⇒ 约 1/3 条目**已在盘上改好**（§二·二），另有一整族报告结论**本身不成立**（§三·8）。
2. 🔴 **合并顺序：本补丁必须【最后】合并。** `check_patch_conflicts.py` 2026-10-29 实测 **17 条**跨补丁重复键，其中 **10 条涉及本补丁**：4 条本补丁赢（F1/F3/F5/RI）、**6 条按文件名排序会输给 `S1/S2/S4`**（明细见 §三·10）。建议顺序 `F1/F3/F5/RI → S1/S2/S4 → … → RL`。

---

## §一 汇总

### 1·1 输入条目 → 处置

| 来源 | 「账要订正」条目 | 处置（逐条见 §二 / §二·二 / §三 / §四） |
|---|---:|---|
| `B94_R-A`（战斗 48）§三 | 5 | **3 条已被 `B94_F5`/`B94_F1` 在盘改好**（1081 / 364 / 292）· 2 条**没查清**（661 的 caster 排除点、94 的 `PlaySoundAsset`）→ §四 |
| `B94_R-A2`（非战斗 95）§三 | 15 | 5 条出补丁（160 · 381 · 681 · 商店:12 · 商店:13）· 6 条已被覆盖 · 4 条**无需改**（现值本就对）· 2 条**不成立**（583、728/772/774）→ §三·6/§三·8 |
| `B94_R-A3`（其余 73）§三 | 13 | 11 条出补丁（56 · 57 · 221 · HUD364 · HUD378 · 273 · 特效195 · 轮抽14 · 轮抽24 · 轮抽67 · 轮抽68 · 菜单427/434/438）· 4 条已被覆盖 · 1 条无需改（菜单:58 极性已在差异列）· 1 条不成立（泛型:8 被 F5 改错 → §三·1 已订正） |
| `B94_R-C`（381 条体检）§二 | 12（8 条错行号 + 4 条其它） | **本代理不出行**：8 条错行号对应账行在 `B94_S1`/`B94_S3` 里已有整行条目（同批），再来一份会造重复键 · 4 条其它中 3 条已被覆盖/无需改、1 条（`SignalEventTrigger.Unregister` 出处写「同上」）→ §四 |
| `B94_R-B1`（战斗无落点 23）§三＋§〇 | 10（7 勘误 + 3 硬结论） | 4 条出补丁（48 · 537 · 306 的 `:1082` · 534 状态）· 2 条已被 `B94_F1` 覆盖 · 2 条属源码注释订正（不在白名单）· 3 条是数据硬结论（不落账） |
| `B94_R-B2`（非战斗无落点 25）§④ | 6 | 6 条出补丁（a→292 由 V1 做、b→教程:3、c→160、d→商店:12、e→菜单:3/:4、f→商店:12/:13 措辞）· X1 已覆盖 `轮抽:9`/`菜单:439`/`其他:779` |
| **合计（含重复）** | **61** | 本补丁 **29 行** |

### 1·2 本补丁按「格」分类（`账/_parts/B94_RL.tsv`，29 行）

| 格 | 行数 | 账行 |
|---|---:|---|
| **判据出处行号/文件订正** | 3 | 战斗逻辑驱动:48 · 泛型:8 · 商店:296 |
| **判据要点订正** | 15 | 战斗界面_HUD:364 · :378 · 其他:160 · 其他:381 · 商店:12 · 商店:13 · 日常:681 · 未分类:273 · 特效:195 · 轮抽:14 · 轮抽:24 · 个人资料:56 · :57 · :221 · 泛型:8 |
| **状态订正** | 3 | 战斗逻辑驱动:534（B→**C**）· 菜单:3（B→**A**）· 菜单:4（B→**A**） |
| **落点订正** | 10 | 战斗逻辑驱动:537 · 商店:12 · 商店:13 · 轮抽:14 · :24 · :67 · :68 · 菜单:427 · :434 · :438 |
| **其它**（差异列行号/措辞 · 备注裁定） | 10 | 战斗界面_HUD:306（`:1082`→`:1092` 3 处）· 商店:13（overclaim 收窄）· 轮抽:68（备注 `:2601`→`:1663`/`:2804`）· 个人资料:221（差异 append）· 教程:3 · 社交:15 · 其他:160 · 战斗逻辑驱动:534（差异 append）· 泛型:8 · 商店:296 |

> 一行可改多格，故分格合计 41 > 行数 29。补丁格式：11 列表头 + LF + UTF-8 无 BOM · `方法` 全为完整签名带 `(...)` · 每行 `备注` 都带 `[B94-R-L]` 标记（形如 `[B94-R-L] 订正：<原值>→<新值>（依据 <报告>:<节/行>）`）。

### 1·3 跨补丁重复键（`check_patch_conflicts.py` 2026-10-29 实测 **17 条**，其中 **10 条涉及本补丁**）

详见 §三·10（含「谁赢 / 会丢什么 / 需要的顺序」）。

---

## §二 逐条改账单（29 行）

> 行号 = **主表 2026-10-29 现读**行号（与 `B94_派活.tsv` 可能不同）。格式：`账行 | 方法 | 改动 | 依据`。

### 2·1 `战斗逻辑驱动.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 1 | 48 | `AIBotsConfig.BotConfigFTUE.GetBotDecks()` | **判据出处**：`decomp_full/AIBotsConfig.BotConfigFTUE__GetBotDecks.c:1-6` → `out_cpp2il_dev_ilspy/AIBotsConfig.cs:46-66`（＋同名 Normal 档 `:13/:29-31`），并写死「原 `.c` 首行是 `<>f__AnonymousType0<object>__get_itemId`（匿名 getter，体 `return *(this+0x10)`），不能当判据」 | R-B1 §〇·2 / §三#4（现核：该行仍指向那份错文件） |
| 2 | 537 | `BattleManager.ResolveRevealOrders(BattleAction)` | **落点**：删掉尾句「⇒ 建议 D」→「出队口四源 0 调用点 **且** 生产端 chosenAbility `280` 池内 0 张 = 双死；按 E4 族9 保持 B、⛔ 不判 D」 | R-B1 §三#7 |
| 3 | 534 | `BattleManager.ResolveRemoveFromCemetery(BattleAction)` | **状态 `B → C`**；**差异** append（放逐语义 + 推翻「可观测为零」）；**备注**记裁定 | §三·5（本代理读 `.c` 三轮闭合） |

### 2·2 `战斗界面_HUD.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 4 | 306 | `CardDisplayWindow.SetupTutorialObj(...)` | **差异列**里 `TutorialOverlay.SetAnnotations` 的 **3 处 `:1082` → `:1092`** | R-B1 §三#5（现读 `TutorialOverlay.cs:1092`，`grep -c SetAnnotations`=1） |
| 5 | 364 | `ChooseCardMenu.AutoProcessDone()` | **判据要点**：① 「Replay ⇒ **选第一张**」→「`SelectCard(_chooseCardUis[target]._cardUI)`，**按 `target` 下标**」；③ 补 `WaitForSeconds(2.0f)`；④ 明确 `ProcessChooseDone()` 无条件 | R-A3 §三·3a |
| 6 | 378 | `ChooseCardMenu.Setup(List<RawCardScript>, string, int, int)` | **判据要点 ④⑤** 补全（`target` 下标；`timerCounter<2` ⇒ `EndSelectionWithDelay` 全句：0.5 s → **未选则随机选一张** → `"0:{timerCounter:00}"`（倒计时文本）→ `<3` 才结束）；补 ⑥ 两个硬编码 id | R-A3 §三·3b — **base = `B94_F3`** |

### 2·3 `其他_基础设施.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 7 | 160 | `Extensions.AlmostEquals(Quaternion,Quaternion,float)` | **判据要点**：删掉「三份反编译 + 解包资源四处全 0 ⇒ 死代码」（**不成立**）→「4 个重载在 ILSpy 都在（`:56/:76/:96/:136`），`decomp_full` 因**同名覆盖**只剩 float 一份」；**状态维持 C**（R-B2 的 A 不采纳，理由见 §三·4） | R-A2 §三·1 / R-B2 §④c — **base = `B94_RI`** |
| 8 | 381 | `PlayerDataManager.FixSkirmishDeck(DeckInventory)` | **判据要点**：从「见 判据/其他与基础设施.md §二·一 #8」→ **五步 + 真体位置**（`…_FixSkirmishDeck_d__466__MoveNext.c:1-390`） | R-A2 §三·3 |

### 2·4 `商店.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 9 | 12 | `BaseOfferPopup.OnDisable()` | **判据要点**：整扇窗描述 → 「`priceDisplayButton(+0xa0)==null` ⇒ **静默 return（本方法无 LogError）**；否则 `m_OnClick.RemoveListener(ProcessPurchase)`」；**落点**：「本仓零 UnityEvent」→「本类的按钮走 `MenuDraw.Hit` 闭包」＋写死「等价 = 建树时挂一次，⛔ 不新建」；**状态维持 B** | R-B2 §5/§④d/§④f · R-A2 §三·15 — §三·4 裁定 |
| 10 | 13 | `BaseOfferPopup.OnEnable()` | **判据要点**：整扇窗描述 → 本方法两句（`null ⇒ LogError("Offer popup missing priceDisplayButton")` / 否则 `AddListener`）；**差异**：overclaim 收窄；**落点**追加「等价 = 建树时挂一次」；**状态维持 B** | R-A2 §三·15/§16 — **base = `B94_S1`** |
| 11 | 296 | `ShopStore.RemoveFinishedOffer()` | **判据出处** → **绝对可解析路径** `D:/2/tools/decomp_generic_trial/out_b94/ShopStore_object__object_.RemoveFinishedOffer__1253e50.c`（＋注明 `decomp_full` 里那份是**另一个类** `LiveOpsEvent`） | §三·4 / §四（原 `_tmp_view` 临时目录风险解除）— **base = `B94_S1`** |

### 2·5 `日常_奖励.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 12 | 681 | `RewardService.CollectSingleReward(RewardInfo)` | **判据要点**末句「实现留待下一棒」→ **已过期**（`Shell/Inventory.cs:2050/2058` 已实现）；状态仍 B（残差 = 两个虚槽名未解 + 上报 P3） | R-A2 §三·12 — **base = `B94_S2`** |

### 2·6 `未分类_其他.tsv` / `特效动画.tsv` / `教程_引导.tsv` / `社交_聊天.tsv`

| # | 文件:行 | 方法 | 改动 | 依据 |
|---|---|---|---|---|
| 13 | 未分类:273 | `Enchantments.TryGetCard(T, out RawCardScript)` | **判据要点**：一对一反查 → **命中 key 后 `idx = BattleManager.GetRandomInt(0, list.Count)`（本局预烧数组按下标取）** = 一个效果映射多张卡时随机取一张 | R-A3 §三·6 |
| 14 | 特效:195 | `CardEffectItem.LoadEffect(CardEffect, bool)` | **判据要点**：`SetActive(false)`（**那是 `Disable()`**）→ **五条分支的文本/称谓填充器** | R-A3 §三·8 |
| 15 | 教程:3 | `BattleTutorialManager.Initialize()` | **状态维持 B**（**不判 D**）；**备注**记裁定 + 证据 | §三·3 |
| 16 | 社交:15 | `AllianceInvitationEntry.OnDestroy()` | **状态维持 B**；**备注**记裁定 + 证据 | §三·4 |

### 2·7 `轮抽.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 17 | 14 | `DraftEventBase.GetPossibleWarlords()` | **判据要点**：`按数据过滤` → **按 `allowedArmies` 集合全给**（⛔ 不是抽 N 张）；**落点**行号 `:3251`→`:3482` | R-A3 §三·10/§三·11 — **base = `B94_S4`** |
| 18 | 24 | `DraftEventBase.PickWarlord(RawCardScript, Action, Action)` | **判据要点**：「校验→写存档→回调」**不成立** → 只有一句云脚本（校验/写存档在服务器侧）；**落点** `:3044`→`:3247` | R-A3 §三·11 — **base = `B94_S4`** |
| 19 | 67 | `DraftModeMenuDemo.GetAvailableWarlords()` | **落点** `:3251`→`:3482` | R-A3 §三·11 |
| 20 | 68 | `DraftModeMenuDemo.GetMenuWarlordImage()` | **落点** `（:1196）`→`（:1211）`；**备注** `:2601`→`:1663`/`:2804`（`:2601` 现读是面板 punch 补间） | 本代理现读 |

### 2·8 `个人资料_成就_排行.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 21 | 56 | `AvatarBorderController.Initialize()` | **判据要点**：`（装备/过期）` → 「`goldenAvatarBorder.IsUnlocked` **为假直接 return**；为真才画它的 `imageReference`。⚠️ **与「玩家装备了哪条边框」无关**」；状态保持 B | R-A3 §三·1 — **base = `B94_S4`** |
| 22 | 57 | `AvatarBorderController.Initialize(PlayerInfo)` | 同上 + 「与无参版**同一份体**、**`playerInfo` 是死形参**」；状态保持 B | R-A3 §三·1 — **base = `B94_S4`** |
| 23 | 221 | `RankedEventDivisionChangeWindowNewSystem.ChangeParticleEffectAndSpriteRank(Image, ParticleSystem, Sprite)` | **判据要点**：`按段位换粒子与立绘` → **逐句六句**（图和粒子 sprite 都取自 `windowData+0xe0`；第三参**只当空判**）；**差异** append 推翻 `B90-N1` 的错读 | §三·2（`.c` + ILSpy 两源一致）— **base = `B94_S4`** |

### 2·9 `菜单_外壳.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 24 | 3 | `AdaptFlexibleGridLayoutByAspectRatio.OnDisable()` | **状态 `B → A`**；**落点**补 `Core/LayoutSpace.cs:106（VisibleHeight）` | R-B2 §19/§④e ＋ **账自身不一致**（`B88-A3` 备注已写「判 A」而状态列没跟）。⚠️ 覆盖边界：`s = max(1,(16/9)/aspect)` 只在 **≥16:9** 时为 1 |
| 25 | 4 | `AdaptFlexibleGridLayoutByAspectRatio.OnEnable()` | **状态 `B → A`** | R-B2 §20/§④e |
| 26 | 427 | `MainMenuWindow.CleanupContainers(List, List)` | **落点** → 等价实现 = 整片重建 `MainMenuRuntime.cs:2629（RebuildModeCards）→ :2502（_placeAllCards）→ :3999（BuildModeCardForCard）`；缺 holder/list 增量复用 | R-B2 §21 |
| 27 | 434 | `MainMenuWindow.InstantiateLiveOp(LiveopsMainMenuData)` | **落点** → `MainMenuRuntime.cs:2807（BuildModeCard）+ :3999 + :4048（ModeCardOwnerEvent）` | R-B2 §22 |
| 28 | 438 | `MainMenuWindow.RefreshLiveOpContainers(bool)` | **落点** → `RebuildModeCards(:2629)` **只等价 `forceClean == true` 那一支**；缺增量回收复用（数据源 = `LiveOpsLocalCatalog.ShowInMainMenu`） | R-B2 §23 |

### 2·10 `泛型与启动链_补读.tsv`

| # | 行 | 方法 | 改动 | 依据 |
|---|---:|---|---|---|
| 29 | 8 | `LoadableReference<T>.op_Implicit(LoadableReference<T>) → T` | **判据出处** → `dump.cs:55616` ＋ **`decomp_generic_trial/out/LoadableReference_object___op_Implicit__101c840.c:1-66`**（RVA **0x101C840**）；**判据要点**：`= GetReference()` → 五句真语义（对象在则直回、否则 id 走 `AssetLocator.GetAsset<T>`） | §三·1（订正 `B94_F5` 的串行）— **base = `B94_F5`** |

---

## §二·二 已被在飞补丁覆盖 / 已无需改 ⇒ 本代理**不出补丁行**（26 组）

| 账行 | 报告条目 | 覆盖者 | 现值（核对结论） |
|---|---|---|---|
| 战斗逻辑驱动:364 `GetMulliganCard` | R-A §三·2 | `B94_F1` | 出处/要点/差异/状态(**A**) 全改 ⇒ 报告说的「账写『取那张要换的卡』」**已不成立** |
| 战斗逻辑驱动:1081 `SetMatchRecording` | R-A §三·1 | `B94_F5` | 要点已从「bool 开关」改成「由 BattleRecordData 装载 MatchData」 |
| 战斗逻辑驱动:292 `DeviceVariables` | R-A §三·5 | `B94_F5`? /（本就已改） | 要点/差异已写全「`.c` 内容不符 + 序列化真值 + 9 项」 |
| 其他_基础设施:291 `GetReference()` | R-A2 §三·2 | `B94_F5` | 出处补 `…GetReference__101ad20.c`、状态 **A** |
| 其他_基础设施:292 `GetReferenceObjEditor()` | R-A2 §三·2 / R-B2 §①/§2/§④a | **`B94_V1`** | 出处（`…__101ab70.c`）·要点（**纯类型判定、0 次 AssetDatabase**）·状态 **A**·落点（`LoadableRef.cs:125`）**全部已落**，比本代理原计划更完整 |
| 其他_基础设施:293 `GetTargetId()` | R-A2 §三·2 | `B94_F5` | 出处补 `…GetTargetId__101af70.c`、状态 A、落点 `CardArt.PathOf(:1120)` |
| 泛型:9 `op_Implicit → string` | R-A3 §三·7b | `B94_F5` | 已改、要点正确（**本补丁只订正 `→T` 那一行**，见 §三·1） |
| 未分类:195/:196/:197 `ContentSizeFitterMinMax ×3` | R-A3 §三·4 | `B94_F5` | 落点已改 `Core/ContentSizeFitterMinMax.cs:83/:69/:76`、状态 A |
| 未分类:702/:703 `UIPreferedSizeFixer ×2` | R-A3 §三·4 | `B94_F5` | 落点已改 `Core/UIPreferedSizeFixer.cs:71/:75`、状态 A |
| 未分类:204 `Countdown.Activate` | R-A3 §三·5 | `B94_S3` | 格式串**已改 `{0:0}:{1:00}`**；组件本体也已现落（`Shell/TimerDisplay.cs:455/504`） |
| 未分类:496 `ProgressBar.SetProgress` | R-A3 §三·12 | `B94_S3` | 要点已写 `SetProgress(current, max, "{0}/{1}", 1, 0)`（`useWholeNumbers = true`） |
| 商店:203 `PurchasePremiumWindow.TryPurchase` | R-A2 §三·14 | `B94_S1` | 落点/差异已重写（等待框 → 本地购买 → 收等待框） |
| 日常:31 `ChallengeList.GetID` | R-A2 §三·6 | （本就已改） | 主表出处**已是** ILSpy `cpp2il/out_cpp2il_dev_ilspy/ChallengeList.cs:5-15` ⇒ 报告要求已满足 |
| 日常:155 `EndBattleRewardHolder.SetReward` | R-A2 §三·7 | （本就已改） | 主表现读**状态 = C**、落点已写「奖励行仍未建 + 两处硬阻塞」⇒ 报告说的「状态 B」不成立 |
| 日常:272 `OnMatchProcessSuccess` | R-A2 §三·8 | `B94_S2`（备注） | 要点本就逐句正确；「B 偏高 / 建议拆两条」= 结构性建议（§四·5） |
| 日常:474 `MissionEvent.Collect` | R-A2 §三·9 | （本就已改） | 落点**已是** `Shell/MissionsTab.cs:913（CollectThenRebuild）` ⇒ 报告前提不成立（§三·8） |
| 日常:561 `PlayEvent.MakeOpponentInfo` | R-A2 §三·10 | （本就已改） | 出处**已是** ILSpy `PlayEvent.cs:871`；`decomp_b91` 悬空路径已不在 |
| 日常:728/:772/:774 | R-A2 §三·13 | （本就已改） | 落点分别 = `SkirmishEventWindow.cs:936/955`（现读 `HasMilestoneToCollect`/`MilestoneStates`）· `ParentEventWindow.cs:1310（BuildRewardDisplay）`· `:1191（BuildOnGoingPage）` ⇒ **不是「无实现」**（§三·8） |
| 其他:497/:511 | R-A2 §三·4 | `B94_S4`（备注） | 落点写的是 `MenuTutorialFlow.cs:102（MarkStageDone）`/`:384（ResetProgress）` —— **带了等价方法名**（§三·8） |
| 轮抽:9 `DraftEventBase.GetBaseData` | R-B2 §25 | **`B94_X1`** | X1 已落 `Shell/DraftEventBase.cs:352`、状态 **A** |
| 菜单:439 `MainMenuWindow.RefreshSignal` | R-B2 §24 | **`B94_X1`** | X1 已落 `RebuildModeCards(AllTutorialsCompleted())`、状态 **A** |
| 其他:779 `get_CurrentSeasonNumber` | R-A2 §三·14 / R-B2 §4 | **`B94_X1`** | X1 已**实现**（`Shell/GameModesHandler.cs:803`）并判 **A** ⇒ 两份报告的分歧被「已经做了」终结（§三·7） |
| `B94_R-C` §二 的 8 条错行号（商店:42/45/89/91/92/98/108/111） | R-C §二#1–#8 | **`B94_S1`/`B94_S3`（同批整行重写）** | 本代理**未改这 8 行**（会造成重复键）。⚠️ **无法确认那两份是否带上了 R-C 的行号订正** ⇒ §四·9 挂了复核清单 |
| 其他:160 的「A/B」 | R-B2 §1 判 A | 本代理裁 **C** | 见 §三·4（已出补丁订正要点 + 备注裁定） |
| 教程:3 / 社交:15 | R-B2 §6 判 D / §18 判 A | 本代理裁 **B / B** | 见 §三·3 / §三·4（已出补丁备注裁定） |

---

## §三 冲突裁定（**已逐条读 `.c` 定论**，每格 `文件:行` 可复核）

### 3·1 `op_Implicit` 两行串行 —— **`B94_F5` 错，`B94_R-A3` 对**

- **裁定**：`→ T` 的 RVA = **`0x101C840`**，`→ string` = **`0x101CB70`**。
- **证据（本代理现读全文）**：
  - `decomp_generic_trial/out/LoadableReference_object___op_Implicit__101c840.c:1-66`：对象非空时回填 `+0x18` 后 **把对象本身返回**，对象为空且 `+0x18` 非空时走 `AssetLocator__GetAsset<object>(…)+return` ⇒ **返回的是对象（T）**。
  - `…__op_Implicit__101cb70.c:45-52`：末句 **`return *(undefined8 *)(param_1 + 0x18)`** = 缓存串 ⇒ **返回 string**。
  - `dump.cs:55614-55624` 的 `GenericInstMethod` 给 `op_Implicit → T` 写的就是 **RVA `0x101C840`**；`:55627-55636` 给 `→ string` 的是 `0x101CB70`。
- ⇒ `B94_F5` 的 `→T` 行「出处写 `101cb70` + 要点写 `= GetReference()`」**两处都错**。本补丁已出第 29 行改成 101c840 + 真语义。
- 🔴 **合并后必须只剩 2 行**：`B94_RI` 里已有两条**地址正确**的行（`原版类=LoadableReference`、`方法=T(...)` / `string(...)`），而 `泛型与启动链_补读` 的两行是同一对方法的**另一套键文本** ⇒ 两套并存 = **4 行覆盖 2 个方法**，而 `check_patch_conflicts.py` **抓不到**（键文本不同）⇒ 请调度台合并后**人工删掉一对**。

### 3·2 `个人资料:221` `ChangeParticleEffectAndSpriteRank` —— **`B94_R-A3` 对，账差异 `B90-N1` 错**

- **裁定**：图和粒子 sprite **都取自 `windowData(+0x98)+0xe0`**；第三个入参 `rankSprite` **只当空判**（`Object.op_Implicit` → 给 `SetActive` 与 `renderer.enabled`）。
- **证据（两源一致）**：
  - `.c:14-42`：`Image.set_sprite(param_2, *(*(param_1+0x98)+0xe0))`；`uVar2 = Object__op_Implicit(param_4)`；`GameObject__SetActive(lVar1, uVar2)`；`TextureSheetAnimationModule__SetSprite(&module, 0, *(*(param_1+0x98)+0xe0))`；`Renderer__set_enabled(lVar1, uVar2)`；其余路径 `FUN_1803f47a0()` = 抛。
  - ILSpy `RankedEventDivisionChangeWindowNewSystem.cs:516-530`：`targetImage.sprite = rankIcon.m_Sprite;` / `bool active = rankSprite;` / `gameObject.SetActive(active);` / `textureSheetAnimationModule.SetSprite(0, image2.m_Sprite);` / `component.enabled = flag;`
- ⇒ `B90-N1` 的「第一件事 `if (rankSprite != 0) Image.set_sprite(rankSprite)`」是**把空判读成了图源**（ILSpy 也把 `rankIcon` 那跳印成 `Method not found`，只有 `.c` 有实参）。本补丁已重写判据要点并在差异列推翻该句。

### 3·3 `教程:3` `BattleTutorialManager.Initialize` —— **B（R-B2 的 D 不成立、账备注的 A 也不成立）**

- **裁定**：**状态 B**（架构性不适用）。
- **证据**：
  - `.c:1-28` 全文 = 只两条字段写（`+0x38 = BattleManager.Instance`、`+0x40 = PlayerDataManager.Instance`）+ 两次 `FUN_1803f3730`（il2cpp 写屏障），**没有别的动作** —— 这一点 R-B2 读对了。
  - 🔴 **但它不是「零可观察行为」**：这两个字段被 `DetectClickToSkip()` 读 —— ILSpy `BattleTutorialManager.cs:47-49`：`if (!canSkip || manager.IsReplayMatch() || !manager.IsTutorialMatch()) return;` ⇒ 没有这一跳「点屏跳过教程」根本走不到 ⇒ **D 的定义不满足**（D 要求「空桩 / 纯日志 / 只写私有字段」，这里写的字段**有读点**）。
  - 我们侧等价物 = `BattleDriver` 本身就是 manager、`TutorialOverlay` 直接读 `Ctx` ⇒ 行为等价但**无同名方法** ⇒ 按「A 需双侧方法级」+ 2026-10-28「架构性不适用 ⇒ B」收 **B**。
- ⛔ 不新建任何 overlay（`EnsureTutorialOverlay` 是 `B92_V1` 已推翻的错落点）。

### 3·4 「架构性不适用」四行 + `商店:296` 出处 —— **`商店:12`=B · `商店:13`=B · `社交:15`=B · `其他:160`=C**

| 行 | 裁定 | 证据 |
|---|---|---|
| `商店:12` `BaseOfferPopup.OnDisable` | **B**（R-B2 的 A 不采纳） | `.c:14-27`：`(+0xa0)==0 ⇒ 静默 return`（**无 LogError**）→ `m_OnClick.RemoveListener(ProcessPurchase)`；我们无方法级等价物（按钮走 `MenuDraw.Hit` 闭包、随节点销毁） |
| `商店:13` `BaseOfferPopup.OnEnable` | **B** | `.c:16-35`：`op_Equality(priceDisplayButton,null)` ⇒ **LogError(`DAT_1842692b0`) + return**；否则 `m_OnClick.AddListener(ProcessPurchase)`。⇒ 与 `:12` 成对 = 「Enable 挂 / Disable 摘」，两行**同口径**；能对位的两件（点价签=ProcessPurchase、缺节点=同一条 LogError）在我们这侧都已落 |
| `社交:15` `AllianceInvitationEntry.OnDestroy` | **B**（R-B2 的 A 不采纳） | `.c:7-15`：**只有两跳 `UnityEventBase.RemoveAllListeners()`**（`+0x50`/`+0x58` 的 `+0x100`；任一为 null ⇒ `FUN_1803f47a0` 抛）；不销毁、不置空。本仓按钮是闭包、无 `UnityEvent` 可摘 ⇒ 架构性不适用 |
| `其他:160` `Extensions.AlmostEquals(Quaternion…)` | **C**（R-B2 的 A 不采纳） | ① `decomp_full/Extensions__AlmostEquals.c` 全文 = **float 重载**（`|a−b| < c`，掩码 `DAT_1834b2e60`）—— 同名覆盖只剩这一份；② 全 26,282 个 `.c` 里 `grep -rl AlmostEquals` 只命中 `Extensions__AlmostEquals.c` + `NetworkingPeer__{OnSerializeWrite,DeltaCompressionWrite,AlmostEquals}.c`（**后者是它自己的 private 重载**）⇒ **原版 0 调用点成立**；③ A 不成立：本仓无同名方法，且 `Quaternion.Angle(a,b) > 0.01f` 语义不同（原版**不取 Abs**、比较方向 `maxAngle > angle`），且该封装原版 0 调用点 ⇒ 任何内联用法都不是「该方法的可观察行为」 |
| `商店:296` 出处 | **改绝对路径** | `find /d/2/tools -name '*RemoveFinishedOffer*'` ⇒ 真身 = `D:/2/tools/decomp_generic_trial/out_b94/ShopStore_object__object_.RemoveFinishedOffer__1253e50.c`（与本仓 `_tmp_view/decomp_b91/` 那份 **`diff` 逐字节相同**）；`decomp_full/Everguild.LiveOps.LiveOpsEvent__RemoveFinishedOffer.c` 是**另一个类**，⛔ 别混 |

### 3·5 `战斗逻辑驱动:534` `ResolveRemoveFromCemetery` —— **状态 C（R-B1 对；「可观测为零 / D 候选」不成立）**

- **裁定**：**C**（休眠、判据齐、落点已定）；⛔ **不判 D**。
- **证据链（三轮现读）**：
  - `CardScript__Awake.c:40`：`*(param_1 + 0x24b) = 1;`
  - `CardScript__RemoveCardFromCemetery.c:5`（**全文一句**）：`*(param_1 + 0x24b) = 0;`
  - `CardScript__set_shouldCheckIfIsInCemetery.c:5` 是属性 setter，**全树 0 调用点**（ILSpy 只有 `CardScript.cs:6062-6070` 的定义）⇒ **一旦清 0 就永久 0**。
  - `BattleManager__GetCardsInCemetery.c:54`：`if (*(char *)(lVar3 + 0x24b) != '\0') { … FUN_180002430(lVar2, uVar4, …); }`（ILSpy `BattleManager.cs:41964` 的 `if (cardScript2.shouldCheckIfIsInCemetery__BackingField)`）⇒ **只有该位为真才 `List.Add`** ⇒ **放逐后的牌永久不出现在任何墓地查询里**（消费方 = `AbilityLogic__GetTargets.c` 的 `ta=60` 墓地取牌/复活）。
- ⇒ `+0x24b` = **「已从墓地放逐」永久闩**，不是「取出途中」的重入闸；「可观测效果为零」被推翻。休眠原因是**生产端** chosenAbility `350` 池内 **0 张**（数据一变即可达）⇒ 按 R-B2 口径「真没做 + 已能指名落点 = **C**」。
- 附带：账 `备注` 那句「`AddRemoveFromCemetery` 有 4 个产出点」→ `decomp_full` 现读**只有 1 个**（`AbilityLogic__PlayAbility.c:1986`）+ 动作 `0x3c` 的出队口体内联在 `ResolveAction.c:4913`。

### 3·6 `日常:583` `PracticeEvent.Initialize` —— **`B94_R-A2` §三·11 的两句都不成立**（不改）

- **裁定**：账的判据要点**正确**，`R-A2` 的「需重写」与「落点无实现」**都错**。
- **证据**：
  - `decomp_full/Everguild.LiveOps.PracticeEvent__Initialize.c:24-58`：`FUN_1811272c0(param_1, …)`（= `base.Initialize()`）→ **`if (param_1[4] == 0)` … `if (*(char *)(param_1[4] + 0x44) == '\0') return;`**（= `if (!save.matchStarted) return;`）→ `RankedMatchResult___ctor(lVar1,0,0,0)` → **`*(undefined4 *)(lVar1 + 0x28) = 0x14;`**（= `battleWinner = Enemy(20)`）→ `(**(code **)(*param_1 + 0x658))(param_1, lVar1, 0, …)`（= `MatchFinished(result, null)`）→ `MenuActions__QueueAction(…)`。ILSpy `PracticeEvent.cs:34-63` 逐句同形，含 `WindowsManager.ShowPopUp("Demo/MainMenu/OngoingMatch", …)`。
  - 落点：`Shell/GameModesHandler.cs:768 InitializePracticeForMenuStart()`，其 doc（`:761`）就写着「原版 `PracticeEvent.Initialize()` 的本地落地（账 583）」⇒ `R-A2` 的「0 处提 `PracticeEvent`」是搜类名/方法名的必然 0 命中。

### 3·7 `其他:779` `get_CurrentSeasonNumber` —— **两份报告（D / C）都过期，实际已由 `B94_X1` 做成 A**

`B94_X1` 已建 `IRankedEvent` + `Shell/GameModesHandler.cs:803 CurrentSeasonNumber`（`null ⇒ 出声 + 0`，对齐原版 NRE 口径）并判 **A** ⇒ 本代理不出行，只登记。

### 3·8 「报告说无实现、其实等价物换了名字」一整族 —— **报告不成立，⛔ 未改主表**

| 账行 | 报告的说法 | 现读证据 |
|---|---|---|
| 日常:474 `MissionEvent.Collect` | 落点 `MissionEvent.cs` 无 `Collect` ⇒ 无实现 | 落点 = `Shell/MissionsTab.cs:913（CollectThenRebuild）`，差异列明写「没有同名方法」 |
| 日常:497/:511 | `grep SetTutorialState/TestResetTutorial` = 0 ⇒ 无落点 | 落点 = `MenuTutorialFlow.cs:102（MarkStageDone）` / `:384（ResetProgress）`（**带等价方法名**） |
| 日常:583 | 同 §三·6 | `.c` + `GameModesHandler.cs:768` |
| 日常:728 | `SkirmishEventWindow` 无 `RefreshScore` | `:936 HasMilestoneToCollect` / `:955 MilestoneStates` / `:574` 接线 |
| 日常:772 / :774 | `ParentEventWindow` 无 `InitializeMilestones` / `OnOpen` | `:1310 BuildRewardDisplay` / `:1191 BuildOnGoingPage` |
| 商店:203 | `ShopTabPage.cs` 0 处提 `PurchasePremium` | 等价链 = `ShopTabPage.cs:764 DoBuy` + `ShopData.cs:472 Buy`（S1 已重写落点/差异） |
| 日常:155 | 状态 B、落点 `EndPanel` 无 `SetReward` | 状态**已是 C**；`EndPanel.cs:274 Build` 明写「奖励行仍未建 + 两处硬阻塞」 |

- **共同错因**：**按原版方法名 grep 判「有没有」**（`CLAUDE.md` §三 已记的那族错误；本轮第 4 次复现）。凡「等价物换了名字」，grep 必然是 0。

### 3·9 `UnityEvent` 措辞 overclaim —— **已收窄（`商店:12`/`商店:13`），`社交:15` 无需改**

- 现读：`Shell/` 里**确有 2 处** uGUI `Button.onClick` —— `Shell/WindowHeaderWithBackButton.cs:90`（`backButton.onClick += BackButtonPressed`）、`Shell/EventBadge.cs:113`（`buttonOnClick.AddListener(...)`）。
- 结论不变（`BaseOfferPopup` / `AllianceSearchTab` 那两处的按钮确实走 `MenuDraw.Hit` 闭包），但措辞必须从「**全仓/本仓**零 `UnityEvent`」收窄成「**本类的**按钮」。`社交:15` 的落点本来写的是「本仓按钮走闭包」，不含「零」字 ⇒ 无需改。

### 3·10 合并顺序（`check_patch_conflicts.py` 2026-10-29 实测 17 条，10 条涉及本补丁）

| 键 | 谁赢（文件名排序） | 若 RL 先合并会丢什么 | 需要的顺序 |
|---|---|---|---|
| `BattleManager.ResolveRemoveFromCemetery` | **RL** | — | F1 先、RL 后 ✅ |
| `ChooseCardMenu.Setup(...)` | **RL** | — | F3 先、RL 后 ✅ |
| `LoadableReference<T>.op_Implicit(...) → T` | **RL** | — | F5 先、RL 后 ✅ |
| `Extensions.AlmostEquals(Quaternion…)` | **RL** | — | RI 先、RL 后 ✅ |
| `BaseOfferPopup.OnEnable()` | **`B94_S1`** | ❌ 判据要点 / 差异措辞 / 落点追加 | **RL 必须在 S1 之后** |
| `ShopStore.RemoveFinishedOffer()` | **`B94_S1`** | ❌ 出处的绝对路径 | 同上 |
| `RewardService.CollectSingleReward(...)` | **`B94_S2`** | ❌ 判据要点（过期句） | RL 在 S2 之后 |
| `DraftEventBase.GetPossibleWarlords()` | **`B94_S4`** | ❌ 判据要点 + 落点行号 | RL 在 S4 之后 |
| `DraftEventBase.PickWarlord(...)` | **`B94_S4`** | ❌ 判据要点 + 落点行号 | 同上 |
| `AvatarBorderController.Initialize()` ×2 | **`B94_S4`** | ❌ 判据要点 ×2 | 同上 |
| `RankedEventDivisionChangeWindowNewSystem.ChangeParticleEffectAndSpriteRank(...)` | **`B94_S4`** | ❌ 判据要点（第三参只空判）+ 差异推翻 `B90-N1` | 同上 |

- **一句话**：**`F1/F3/F5/RI` 随意先后 → `S1/S2/S4` → **最后** `RL`**。
- 与 RL 无关、供调度台一并处理的 5 条：`ChallengeLogMgr.TroopKilled`（F1↔Y1，Y1 赢）· `BattleManager.PayCardPlayedCostOath`（F1↔F2，F2 赢）· `BattleManager.GetTargetInPos`（F5↔X2，X2 赢）· `FeatureChecker.OnEnable`（RI↔X1，X1 赢）· `PracticeEvent.Initialize`（S4↔W1，W1 赢）。
- ⚠️ 另有一处**机检抓不到**的重复：§三·1（`op_Implicit` 的两套键）。

---

## §四 没查清的（如实列，⛔ 未用猜测填空）

| # | 条目 | 差什么 |
|---|---|---|
| 1 | `战斗逻辑驱动:94 AiScripted.ExecuteAction` 的「末尾 `PlaySoundAsset`」 | R-A §三·4 说 `grep -i sound` 在 `.c` 里 **0 命中**；要区分「账记错」还是「在状态机/无名分支里」需 **VA 复核** ⇒ 未改 |
| 2 | `战斗逻辑驱动:661 BroadcastUnitRegiment` 的 `!= caster` 排除点 | R-A §三·3：`.c` 体里只有 `IsInPlay ∧ card!=null`，**未见 caster 比较**；第 3 形参被 Ghidra 吞（ILSpy 有 3 参）⇒ 需 `out_il2csharp`/VA 确认 + 定位排除层 ⇒ 未改 |
| 3 | 日常:272「拆两条」（主档写回已做 / boost 表未接） | 拆行 = 新增账行，超出「改一格」范围 ⇒ 请调度台拍板 |
| 4 | 个人资料:197/198 ＋ 菜单:427/434/438/439 的「6 条 → 2 个阻塞点」指针化（R-A3 §三·13） | 只补了 427/434/438 的等价落点；**跨 6 行互指的指针化**留给调度台 |
| 5 | 未分类:322 `GenericArmyItem.GetItem(CardArmy)` 带参版体 | `decomp_full/GenericArmyItem__` 只有 8 个产物、**无带参版** ⇒ 现只能引 ILSpy `:221-276`；差什么：按 `dump.cs` 的 `GenericInstMethod` 重导 |
| 6 | 轮抽:14 `GetPossibleWarlords` 的**方法体**仍缺 | 泛型类 ⇒ `dump.cs` 写 `RVA: -1`；`decomp_generic_trial/out` 与 `out_b94` 均**不含 `DraftEventBase`** ⇒ 本补丁的要点取自 3 个 lambda（`b__20_0/1/2`）+ ILSpy = **旁证级** |
| 7 | `B94_R-C` §二 的 8 条错行号 | 对应账行在 `B94_S1`/`B94_S3` 里已有整行条目，**无法确认那两份是否带上了行号订正**；合并后请按这 8 个真值复核：`CarouselOfferPopupDrawer.cs:309/109`（`DoReset`/`Initialize`）· `ExpansionPassPurchasePremiumPanel.cs:216/436/441` · `GachaCratePanel.cs:226`（`OpenCrateButtonClick` 是同步 void，不是协程）· `GachaEventTab.cs:144/216` |
| 8 | `商店:296` 的 `decomp_generic_trial/out_b94` 是否**长期**保留 | 该目录是 2026-10-29 新出现的产物目录（211 个文件）；若后续被清理，本行出处会再次悬空 ⇒ 建议调度台把它连同 `DecompG7.java` 名单一起登记 |
| 9 | `B94_RI` / `泛型与启动链_补读` 两套 `op_Implicit` 键的去留 | §三·1：合并后必须只剩 2 行，**删哪一对**由调度台定（本代理只把「`→T` 必须指 `101c840`」订正到位） |

---

## §五 取证清单（可复跑）

```bash
# 1) 主表现读（按 (原版类, 方法) 定位，⛔ 别用报告里的行号）
python -I -X utf8 -c "
import io
b=io.open('D:/4/Unity/资料/完全复刻/账/战斗界面_HUD.tsv','rb').read().decode('utf-8-sig').replace('\r\n','\n')
for i,l in enumerate(b.split('\n'),1):
    r=l.split('\t')
    if len(r)==11 and r[1]=='ChooseCardMenu': print(i,r[2][:50],'|',r[4][:80])
"
# 2) §三 各裁定的第一权威（全部逐句读过）
sed -n '1,45p'  D:/2/tools/decomp_full/RankedEventDivisionChangeWindowNewSystem__ChangeParticleEffectAndSpriteRank.c
cat            D:/2/tools/decomp_full/BattleTutorialManager__Initialize.c          # 28 行
cat            D:/2/tools/decomp_full/BaseOfferPopup__OnDisable.c                  # 无 LogError
cat            D:/2/tools/decomp_full/BaseOfferPopup__OnEnable.c                   # LogError 在这里
cat            D:/2/tools/decomp_full/AllianceInvitationEntry__OnDestroy.c         # 只有两跳 RemoveAllListeners
cat            D:/2/tools/decomp_full/Extensions__AlmostEquals.c                   # float 重载
grep -rl AlmostEquals D:/2/tools/decomp_full | head                               # 只 Extensions + NetworkingPeer
cat            D:/2/tools/decomp_full/Everguild.LiveOps.PracticeEvent__Initialize.c # :24-58 有 matchStarted 闸
sed -n '45,52p' D:/2/tools/decomp_generic_trial/out/LoadableReference_object___op_Implicit__101c840.c   # 返回对象
sed -n '45,52p' D:/2/tools/decomp_generic_trial/out/LoadableReference_object___op_Implicit__101cb70.c   # 返回缓存串
# 3) 534 的放逐链
grep -n "0x24b" D:/2/tools/decomp_full/CardScript__Awake.c D:/2/tools/decomp_full/CardScript__RemoveCardFromCemetery.c
sed -n '50,60p' D:/2/tools/decomp_full/BattleManager__GetCardsInCemetery.c
grep -n "shouldCheckIfIsInCemetery" D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/{CardScript,BattleManager}.cs
# 4) 名字类错误复现（本轮 8 例「按方法名 grep 判无实现」）
grep -n "HasMilestoneToCollect\|MilestoneStates"  D:/4/Unity/MyGame/Assets/CardPresentation/Shell/SkirmishEventWindow.cs
grep -n "BuildOnGoingPage\|BuildRewardDisplay"    D:/4/Unity/MyGame/Assets/CardPresentation/Shell/ParentEventWindow.cs
grep -n "InitializePracticeForMenuStart"          D:/4/Unity/MyGame/Assets/CardPresentation/Shell/GameModesHandler.cs
grep -n "SetAnnotations"                          D:/4/Unity/MyGame/Assets/CardPresentation/Battle/TutorialOverlay.cs   # 1 处 = :1092
grep -n "backButton.onClick\|buttonOnClick.AddListener" D:/4/Unity/MyGame/Assets/CardPresentation/Shell/{WindowHeaderWithBackButton,EventBadge}.cs
# 5) 跨补丁重复键
python -I -X utf8 d:/4/Unity/工具/check_patch_conflicts.py
```

### 红线自查
- 本代理产出 = **本报告 + `账/_parts/B94_RL.tsv`**，共两件；未改任何 `.cs` / 数据 / 账主表 / 正本 / 所有权表。
- 未跑 Unity、未动 git、未改 `d:/2`（只读 `decomp_full` / `decomp_generic_trial/out{,_b94}` / `out_cpp2il_dev_ilspy`）。
- 补丁逐行校验：**30 行（1 表头 + 29 数据）· 全 11 列 · 无 BOM · 全 LF · 无裸 CR · `方法` 列全带 `(...)` · 每行 `备注` 带 `[B94-R-L]`**。
- §三 每一条都给了 **`.c` 行号或 ILSpy 行号**；真查不出的写进 §四，**没有替任何一方猜**。
