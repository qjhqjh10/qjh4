# B123 · 对账 D —— Unity / 真机 **开窗口前**必核

> 代理：只读对账代理 D（批次 `B123`）· 日期 **2026-11-03**
> 口径：**全程没跑 Unity、没跑自检、没动 git、没改任何源码**；唯一写的就是本文件。
> 所有结论均为**现读磁盘**（`io.open(p,'rb')` 二进制读、按 `\r\n` / `\n` 切行）。
> 基线数字来自 `d:/4/_tmp_view/*.log` 的 **2026-11-02** 那一轮（⛔ 不是本批跑出来的）。

---

## 〇 · 三句话结论

1. **23 个断言入口全部在位，且全部落在各自聚合行的第一个 `//` 之前**（逐条按字符偏移核过，最紧的一条离 `//` 还有 75 字符）——接线本身**没有**「代码落进注释」那个坑。
2. **已知会红的旧断言全部还在原处，「会红」的理由是判据变更而不是回归**：`B120-Q04e`（射线 miss 不再回落投影）· `B120-A16`（提交即出牌）· `B120-Q04f/Q04g`（`hits[0]` 没有 z 序）——**三组都能从源码上把红因说死**。
3. **有 3 件事「跑了 Unity 也关不掉」**：`B121-COLL-19` 的四条在断言里就是 `MarkUnverified`（宿主只会印「未验」）；`D118-57` 的断言类 `B118W14_Assert` **全仓 0 宿主**；`B121-COLL-25` 的 `DeckScene` 断的是**我们自己钉的 592.2**，结构上测不出与原版 612.17 的差。

---

## 第一部分 · 逐条静态结论

### §1.A 昨天新接的 23 个断言入口（判据 = `报告/B121_宿主接线清单.md`）

**核对方法**：取该聚合行**整行文本**，算 `firstComma = s.find('//')`，再逐个 token 取 `s.find(名字)`，判 `名字位置 < firstComma`。
同时向回找不到 `RunCore` / `Run` 之间的 `#if` 或方法级 `return;`（三条宿主逐行扫过，**0 命中**）。

**共同结论（全部 23 条）**：

| 项 | 现读 |
|---|---|
| 入口类文件 | **23/23 存在**（`B121_D4` 不是独立类，是 `public static partial class RuleEngineTest` 的用例方法 `B121D4Case()`，在 `CardPresentation/Editor/B121_D4_Assert.cs:34`） |
| 是否在第一个 `//` 之前 | **23/23 是**（详见下表「首个 `//` 偏移」） |
| 宿主 Run 方法可达 | 4 个宿主的 `Run()` 都**无条件**调 `RunCore()`（`BattleScene.cs:467` · `CollectionScene.cs:952` · `DeckScene.cs:777` · `RuleEngineTest.cs` 本体即 `Run`） |
| 方法内早退 | `RunCore` / `Run` 起点到插入行之间 **无 `#if` / 无 `return;`** |

| 断言类 | 宿主 · 行 | 名字偏移 vs 首个 `//` 偏移 | 宿主 Run 方法 | 静态结论 |
|---|---|---|---|---|
| `B121_WA_Assert` | `BattleScene.cs:47035` | 11074 / `//`@11792 ✅ | `RunCore`（`Run`@452→`RunCore`@535） | 类在，`Run()` 无参 |
| `B121_WB_Assert` | 同上 | 11154 / 11792 ✅ | 同上 | 类在（另有嵌套 `Fixture`） |
| `B121_WE_Assert` | 同上 | 11234 / 11792 ✅ | 同上 | 类在；**源码级**断言（读 `Application.dataPath` 下的 `BattleDriver.cs`） |
| `B121_Q4B_Assert` | 同上 | 11314 / 11792 ✅ | 同上 | 类在 |
| `B121_E04B_Assert` | 同上 | 11397 / 11792 ✅ | 同上 | 类在 |
| `B121_E03F_VFX_Assert` | 同上 | 11483 / 11792 ✅ | 同上 | 类在 |
| `B121D2_Assert`（文件 `B121_D2_Assert.cs`） | 同上 | 11581 / 11792 ✅ | 同上 | 类名**无下划线**，以文件里的 `class B121D2_Assert` 为准 |
| `B121AUD_Assert`（文件 `B121_AUD_Assert.cs`） | 同上 | 11658 / 11792 ✅ | 同上 | 同上，类名无下划线 |
| `RuleEngineTest_B121WB` | `BattleScene.cs:45405` | 3248 / 3633 ✅ | `RunCore`（**driver 活着**那条聚合行） | 签名 `Run(BattleDriver driver)`，与调用 `Run(driver)` 一致 |
| `B121_C1_Assert` | `CollectionScene.cs:13286` | 546 / **整行 0 个 `//`** ✅ | `RunCore`（`Run`@939→`RunCore`@969） | 类在；**内含 4 条 `MarkUnverified`**（见 §1.C） |
| `B121_C2_Assert` | 同上 | 638 ✅ | 同上 | 类在 |
| `B121_C3_Assert` | 同上 | 730 ✅ | 同上 | 类在 |
| `B121_COLL16_Assert` | 同上 | 822 ✅ | 同上 | 类在 |
| `B121_COLLFIX_Assert` | 同上 | 926 ✅ | 同上 | 类在 |
| `B121_WC_Assert` | `DeckScene.cs:1400` | 487 / 581 ✅ | `RunCore`（`Run`@764→`RunCore`@794） | 类在 |
| `B121_D1_Assert` | `RuleEngineTest.cs:1400` | 235 / 899 ✅ | `Run`（`Run`@68） | 以 `Guard("B121D1_Assert", Run)` 包一层 |
| `B121_D1B_Assert` | 同上 | 340 ✅ | 同上 | 同上 |
| `B121_D3_Assert` | 同上 | 424 ✅ | 同上 | 直接 `Run()` |
| `B121D4Case`（`Step(...)`） | 同上 | 511 ✅ | 同上 | `partial class RuleEngineTest` 的用例方法 |
| `B121_D5_Assert` | 同上 | 524 ✅ | 同上 | 类在 |
| `B121_D5R_Assert` | 同上 | 606 ✅ | 同上 | 类在 |
| `B121_E03F_DATA_Assert` | 同上 | 691 ✅ | 同上 | 类在 |
| `RuleEngineTest_B121WC` | 同上 | 794 ✅ | 同上 | 类在 |

> **开窗口时要看什么**：每个类自己都印一行「合计」（`B121xx ==== 合计：…`）。**按类名 grep 它自己的合计行**，⛔ 不要只看宿主总合计——`B121_C1` 那 4 条「未验」是不进 `_sink` 的。

---

### §1.B 已知会红 / 判据变更的旧断言

| ID | 静态可判结论 | 现读证据 | 开窗口时要看什么 |
|---|---|---|---|
| **B120-Q04e** | **还在原位，且红因可从源码说死**。`HitSlot` 3D 档已改成「原版射线链」，**miss 直接 `-1`、不再回落投影**。所以「把卡心投影点喂进去必中」这一族，只要**射线打不到 3D 挡板**就会红。 | `BattleDriver.cs:33320`（`if (use3DBoard …) { TryHitSlotByBoardRay; return raySlot; }` 注释明写「miss ⇒ -1 ⛔ 不回落」）· `:41133-41153`（`RaycastNonAlloc` → **字面 `hits[0]`** → `GetComponentInParent<CardView>()`）· `:41421` `BoardTargetRayLayerMask = 1 << ArenaSlots.BoardCardLayer`（层 10）· `CardView.cs:2321-2331` `SetLayer` 递归子件 ⇒ 3D 挡板也在层 10 | 三条旧断言的**位置没变**：`B97_UI2_Assert.cs:97-127`（B1/B2/B3/B4/C1/C2/D1/D2/D3）· `B97W37_Assert.cs:100-112`（W37-381-1/2/3）· `BattleScene.cs:38804-38810` + `:39089-39160`。**判红因**：先看 `B97_UI2_Assert:101` 那条「B2（前提）真 collider 命中 ≥1」——**它红 = 射线整条没命中**（判据变更）；**它绿而 B1 红 = 命中了别的卡**（真回归） |
| **B120-A16** | **还在原位**。`CardInteraction.TryCommitWhileHolding`（类尾 `:2360+`，在 `UpdateDrag` 的 `:1084` 同行调用）⇒ **描到落点第一帧就 `Release`**。所以「趁松手前量」的那一段**必然已经清空**。 | `BattleScene.cs:6638` 是 `for (24) { SimulateDrag; Step(1/30); }`（0.8 s）· `:6646/:6650/:6652` 断 `LitSlotCount` · `:41361-41379` 那 24 帧同理 · 实现 `CardInteraction.cs:1084` / `:2360-2406` | 预期红：`BattleScene.cs:6646`（`LitSlotCount == 1`）· `:6650`（战术卡 0 格）· `:6652`（对面 0 格）· `:41369/:41372/:41374/:41377` 四条前提。**改法是「提交前量」，不是放宽期望** |
| **B120-Q04f** | 文案过期：`B2b②` 写完「取 **z 最小**」，而现读判据是原版 `hits[0]`（`RaycastNonAlloc` **不排序**）。 | `BattleScene.cs:39337-39343`（含自注「⚠️ 首次 Unity 跑要确认 Unity 的缓冲序与 z 一致」） | 与下一条同看 |
| **B120-Q04g** | **`hits[0]` 与 z 序的关系本地判不了**（缓冲区顺序是 Unity 运行时行为）。另：`HitSlotHitCountForTest` 已改成「首条命中 0/1」，**不再是命中张数**。 | `BattleDriver.cs:13703`（3D 档 `return c3;`）· `:41135` 注释「第 2 个出参改成**首条**命中（0/1）」 | 看 `BattleScene.cs:39337` 的 `pickA == uSlot964 && pickB == otherSlot`。**若红而 `nHitOverlap == 1` ⇒ 是缓冲序问题（Q04g 实锤），不是实现回归** |
| **B120-F19** | **大概率绿**（静态判据已满足）：`Main Mixer.mixer` 里确有 `Voices` 组；`Music.mixer` 里确有 `BattleGroupLow/Medium/High`；资源在 `CardPresentation/Resources/Audio/`。 | `Main Mixer.mixer:178 m_Name: Voices` · `Music.mixer:181 BattleGroupLow / :148 BattleGroupMedium / :79 BattleGroupHigh` · `WarpforgeAudio.cs:24 MixerResource = "Audio/Main Mixer"`、`:345 Audio/Music` · 断言 `B120_W11_Assert.cs:154-157`（B0 / B0b） | 只有 `B0`/`B0b` 红才说明是 **mixer 资产导入**问题。⚠️ `AudioSetup.Verify()` / `PlayCheck` **在 batchmode 不跑**，真正的执行口是 `B120_W11_Assert`（已接 `BattleScene.cs:47035`） |
| **B120-A11** | **无代码可判**：透视下「向屏幕中心收拢 ≈8%」正交相机模拟不了，只做了像素等价净缩放 1.0137×。 | `报告/B120_W1_执行.md` §没做·1 · `CardInteraction.cs:40/940/986` | 真机拖起一张牌，看「有没有向中心收」的观感 |
| **B120-F17** | **无代码可判**（批处理 mixer 快照不前进）。 | `BattleMusic.cs:216-253`（`snap.TransitionTo(dur)` + `UpdatePauseByVolume`）· 判据 `DAT_1834b3278 = -60 dB` | 真机听 BGM 切档的过渡有没有爆音 / 停顿 |
| **B120-Q07b** | **真机探针**：读运行期 `Application.targetFrameRate` 与 `GameStaticData.FPSLimit`。代码已按原版删了 `fps<=0 ⇒ 60` 的钳制。 | `Card3DAnimationController.cs:414`（原样喂）· `:509-511 FrameSlackExpired` | 真机打一行 `Application.targetFrameRate`。**拿到真值前别改那一档** |
| **B120-W15a** | **静态已承认「未核」**：`SetInDeckPosition` 拷的是 `localPosition`，只有「卡的父 == 参考件的父」时绝对换算才成立。 | `BattleDriver.cs:41508`（原文明写「未核的那一半」）· `CardFeel.cs:3979` | 真机看飞牌**起点**偏不偏。若偏，**先回源核这条**，⛔ 别改常量去凑 |
| **B120_W16_Assert A 组 / X9 段** | 两处前提已改成显式 `3,3`（删自定容差），但**只有跑过才知道**。 | `B120_W16_Assert.cs:171`（`TryResolveSlot(drop, out slot0, 3, 3)`）· `BattleScene.cs:19115-19121` · `HandAssertE8.cs:568-590` | 首跑看 `B120_W16` 的 A1/A2/A3 与 X9-A/B/C1/C2 这五条 |

---

### §1.C 收藏 / 卡组编辑窗待验

| ID | 静态可判结论 | 现读证据 | 开窗口时要看什么 |
|---|---|---|---|
| **B121-COLL-19** | **四条代码全在，但断言自己是 `MarkUnverified`** ⇒ **跑 `CollectionScene.Run` 也只会印「未验」，关不掉这条账**。逐条静态判定：① 点格后不再改当前卡组 **✅ 静态成立**（`SelectDeck` 方法体只剩 `OpenDeckInfo(i)`）；② 选阵营后 `_hdrClearQ[0].activeSelf` **✅ 接线成立**（`RefreshClearFilter(0)` → `DeckFiltersActive` ← `_deckFacFilters`，而点阵营会 `_deckFacFilters.Add/Remove`）；③ 灰度 **✅ 接线成立**（`ApplyCardGrey` → `v.SetGrey(grey?1:0, 0)`，`grey = !owned 或 isBanned`）；④ 114 闸 **✅ 接线成立**（`RefreshDeckImportGate` 里 `DeckCount() < 114`）。 | `CollectionWindow.cs:5342`（`SelectDeck` → 只 `OpenDeckInfo`）· `:3740` + `:7018`（`DeckFiltersActive`）· `:4876`（`_deckFacFilters` 增删）· `:7033-7038`（`ApplyCardGrey`）· `:7055-7063`（114 闸）· `B121_C1_Assert.cs:59-63`（**4 条 `MarkUnverified` 原文**） | **要真关掉必须「建真窗 + 真存档」**：起 Collection player → ①点一格卡组看当前套没变 ②点阵营看 Clear 钮出现 ③找一张未拥有/被禁的卡看灰度 ④造 114 套看 Import 灰掉。⚠️ ③④ 常态**不可达**（本机全解锁、无 LiveOps），④ 还得先塞 114 套——**如实判「不可达」也算结论**，⛔ 别伪造 |
| **B121-COLL-25** | **两说并存，且现有断言测不出差异**：`FilterPanelModel.HdrClearX(0) = HdrGrpL(592.17) + 20 = 612.17`（原版 CSF 跑过那一档）；`DeckRuntime.HdrClearX = 592.2`（建时静态位）；`RefreshHeaderChips` 在 `cells.Count == 0` 时**用 592.2**。`DeckScene.cs:3286` 断的正是 592.2 ⇒ **绿是自证，不能证明对**。 | `FilterPanelModel.cs:1195 HdrGrpL = 592.17` / `:1212-1216 HdrClearX(n)` / `:1203 HdrChipMax = 5` · `DeckRuntime.cs:108 HdrClearX = 592.2` / `:10343`（三元：`cells.Count > 0 ? FilterPanelModel.HdrClearX(n) : HdrClearX`）· `DeckScene.cs:3286 CheckRect("hdr_clear", 592.2f, …)` | 真窗口量 `hdr_clear` 的实际 `anchoredPosition.x`。**若原版确为 612.17，则 592.2 是待改的偏差**；改时要同时改 `DeckScene.cs:3286` 的期望，⛔ 别只改一边（会变红） |
| **B121-COLL-28** | **静态确认**：`RefreshHeaderChips()` 每拍 `DestroySafe` 旧 chip → 重建（上限 `HdrChipMax = 5`），且 `RefreshHeader` 每拍都调。功能正确、开销可控。 | `DeckRuntime.cs:10308-10345`（① 拆 ② 建 ③ 挪 Clear）· `FilterPanelModel.cs:1202 HdrChipMax = 5` | 真机连点筛选/切页看有没有掉帧或 chip 残影 |

---

### §1.D 战场（`B121-BF-03` + 13 prefab import）

| 项 | 静态可判结论 | 现读证据 | 开窗口时要看什么 |
|---|---|---|---|
| **盘上 prefab 版本** | **确认还停在 11-01**（全部 13 件）。 | `MyGame/Assets/Resources/ArenaPrefabs/*.prefab` mtime：`battlearena1` 11-01 22:49 · `battlearena2` 22:53 · `battlearena3` 22:57 · `aeldari` 23:00 · `astramilitarum` 23:07 · `blacklegion` 23:13 · `darkangels` 23:17 · `emperorschildren` 23:23 · `genestealers` 23:27 · **`leviathan` 23:29** · `sororitas` 23:35 · `spacewolves` 23:39 · `tauviorla` 23:42 | — |
| **Timeline 层有没有烘进 prefab** | **没有**：`battlearenaleviathan.prefab` 里 `PlayableDirector` **0 次命中**（`battlearena1` 同样 0）。 | `grep -c "PlayableDirector" battlearenaleviathan.prefab` = 0 | 重建后再 grep，应 ≥ 1 |
| **代码 / 生成器在位** | **都在**：`WarpforgeVFX/Runtime/ArenaTimelinePlayer.cs`（含 `.meta`，guid `66b2472af48a403583320b4becd75440`）· `Unity/工具/gen_arena_timeline.py`（`--check` 现跑 **rc=0**，两份 json 逐字节一致，`1 场 / 1 director / 5 track / 7 control clip`）。 | `gen_arena_timeline.py --check` 输出「✅ 一致：数据/游戏数据/arena_timelines.json」「✅ 一致：MyGame/Assets/CardPresentation/Resources/arena_timelines.json」· `ArenaTimelinePlayer.cs:114 ResourcePath = "arena_timelines"` / `:128-132 Resources.Load<TextAsset>` | 重建后看日志里 `[Arena] Timeline：…` 一行 |
| **调用链** | **通**：`BuildPrefabFromCLI`(`ArenaBuilder.cs:630`) → `BuildOneArenaPrefab`(`:741`) → `BuildContent(null, mf)`(`:749`) → `ApplyGroupNodes(mf, root); ApplyArenaTimelines(mf, root);`(`:3141`，**在第一个 `//` 之前**，`//` 偏移 68，调用在偏移 35)。 | 同上；`ApplyArenaTimelines` 定义在 `:6465` 类尾 | 只重建 **leviathan** 一场即可（数据里只有这 1 场有 director） |

> **13 prefab import 的真实含义**：`Resources/ArenaPrefabs/` 只重建 1 件；其余 12 件**不需要**重打。Unity 打开工程会自动重导入新增的 `.cs` / `.json`（`.meta` 都在）。⛔ 不要为这个跑 `BuildArenaPrefabs()` 13 场全量（用户 2026-10-11 明令「一场一场做」）。

---

### §1.E 真机清单（只核「有没有可执行步骤」）

| ID | 静态可判结论 | 现读证据 | 怎么验（一步） |
|---|---|---|---|
| **B117-R7** | **静态支持「按钮被禁用」这一解释**：`WindowButton` 在 `!_interactable` 时**直接返回、不派发点击**，症状与「点不中」完全同形。判据链：`SkirmishEventWindow.RefreshBannedLine:792` → `SetBattleInteractable(valid)` → `WindowButton.Interactable`。 | `LiveOpsEventWindow.cs:2375 BattleButtonWb` / `:2389 BattleInteractable` / `:2398-2409 SetBattleInteractable`（**值变化时打一行 `[Event] Battle! interactable = …`**）· `WindowButton.cs:545/829`（`if (!_interactable) return;`）· `SkirmishEventWindow.cs:792` | 起 player → 进「遭遇战」→ **换一次卡组**（触发 valid 变化）→ 在 `Player.log` 里 grep `[Event] Battle! interactable`。**先读这个值，再谈坐标**（`LiveOpsEventWindow.BattleInteractable` 也可直接读） |
| **B117-R8** | **自动那半边已在跑**：UI-21 的三族断言已接进 `CollectionScene.cs:13285`（`B117F14_Assert` / `B117_WCARD_Assert`）。 | `CollectionScene.cs:13285` 整行（`B117F14_Assert.Run(); … B117_WCARD_Assert.Run();`，接在 `B121_C1` 前一行）· `CardView.cs:5000/5082` 的 `bandMax` 八档 | 起 player → 收藏窗卡池 → **截一张 1080p**，与 `_tmp_view/shots/20261103_s117/cmp_ui21.png` 的右半边比「卡名/阵营/效果文字大小」 |
| **B117-R25** | **没有自动宿主**（纯静态数值判据 + 要渲图）。 | `报告/B120_REC_对账.md:17` · `判据/B121_REC_旧任务开行对账.md:19` | 起 player → 触发 G2 那一段 → 在 `t≈0.31` 与 `t≈0.72` 各截一帧，只判「有没有播、播的是不是该播的那个」 |
| **B117-R43** | **最小探针的三个读口都在**：`FaceCostText`（null = TMP 没建）· `FaceCostVisible`（容器没开）· `.text`（值）。`BuildFaceCostText` 四个早退：已建过 / `!TmpFont.Available` / `!ShowsCost(Data)` / `_faceMode == CardFace.Board`。 | `CardView.cs:13493-13545`（`BuildFaceCostText`）· `:13550-13560 SyncFaceCostText` · `:13579 FaceCostText` · `:13581 FaceCostVisible` | 起 player → 收藏窗卡池 → **停 3 秒再截**；对第 3 行任一格读 `cv.FaceCostText`（null ⇒ TMP 没建）/ `cv.FaceCostVisible`（false ⇒ 容器没开）/ `.text`（空 ⇒ 值没刷）。⛔ 先别猜是三者之一，**先读值** |
| **B117-36** | **确认包是旧的**：`Assembly-CSharp.dll` mtime **2026-11-02 07:52**，早于 11-03 的两处手感修复（`RotateEase=OutQuad` @ `Board/DeploySequence.cs:334`、删二次 `SetArtLocalY` @ `Core/CardFeel.cs:2090`）。 | `d:/4/_tmp_view/player/WarpforgePlayer_Data/Managed/Assembly-CSharp.dll` = Nov 2 07:52（`WarpforgePlayer.exe` 本身 Sep 16 21:44，壳不重建） | **真机验收前必须先重出 player 包**，否则验的是旧代码 |

---

### §1.F `D118-38` / `D118-57` / `D118-85`

| ID | 静态可判结论 | 现读证据 | 开窗口时要看什么 |
|---|---|---|---|
| **D118-38** | **代码三处全在，且断言已接宿主** ⇒ 静态侧已满足，只剩跑。 | `LiveOpsHandler.cs:838`（`LocalLiveOpsEvent : LiveOpsEvent<PlayEventSaveGame, LiveOpsEventData>`）· `:926`（`PlayEventSaveGame fs;` + 注释）· `:973`（`LiveOpsSaveStore.Load(id, typeof(PlayEventSaveGame))`）· 断言 `B118W5Assert.Run()` 已接 **`ShellScene.cs:11214`**（类名**无下划线**，文件 `B118W5_Assert.cs`） | 跑 `ShellScene.Run` + `MainMenuScene.Run`。看 `B118W5` 合计行（① 类型形状 ② Save→Load 往返 ③ 三处源码接线） |
| **D118-57** | **Shader 修复在**（`c.a = i.color.a`，只丢贴图 alpha、保材质/顶点 alpha）⇒ 三条淡入路径「真会淡」。但 **`B118W14_Assert` 全仓 0 宿主**（只在定义文件里出现）⇒ **自动跑永远跑不到**。另：同族潜伏口 `CardFeel.UnDissolve` **确认全仓 0 外部调用点**（只有定义 + 自注）。 | `CardPresentation/Shaders/ArtOpaque.shader:70 c.a = i.color.a` · `B118W14_Assert.cs:263/477`（E6 断言本体）· `grep -rn B118W14_Assert` 除定义文件外 **0 命中** · `CardFeel.cs:3596 UnDissolve`（调用点只在同文件内） | 只能真机：发牌淡入 / 阵亡消散 / 相关卡压暗**三条路径逐个看「淡得对不对」**。⚠️ 这条**没有自动兜底**（断言没接线） |
| **D118-85** | **偏差仍在，且是「整台相机」级别**：`LayoutSpace.Apply` 强制 `orthographic = true` + `orthographicSize = 5`，**没有**原版 UI Camera 的透视 fov 40 那一档。震屏包络那一半已改（t² / 三次 Hermite）。 | `Core/LayoutSpace.cs:76-85`（`Apply`：`cam.orthographic = true; cam.orthographicSize = DesignHeight*0.5 = 5`）· 报告 `B120_W4_执行.md` | 真机 A/B：同一 HUD 元素在屏幕**边角**的相对大小（透视会有远近差，正交没有）。这是 `D-1`（HUD 相机整台换透视）的前置，**改它必须真机对照** |

---

## 第二部分 · Unity 施工单（按执行顺序）

> **硬规矩**：同一工程同一时刻只能一个 Unity 实例 ⇒ **全串行**；子代理不许跑 `-executeMethod`；跑了 `DeckScene.Run` 会**重写** `DeckEditor.unity`。
> 全套一键：`bash d:/4/Unity/工具/_run_8_checks.sh`（现读 **24** 条 `run`，约 20+ 分钟）。
> **绿红判据 = 日志里的「合计」行**，⛔ 不是退出码（`BattleScene.Run` 的 **139** 是已知间歇段错误）。

### 第 0 步 · 前置（必须做，约 1 分钟）

```bash
cd d:/4
git status --short                                   # 期望：空（现读 clean）
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py   # 期望：漂移 0 · 失败 0 · A 5899
TMPDIR=/tmp/wf_pre bash d:/4/Unity/工具/typecheck.sh       # 期望：运行时 0 / 编辑器 0
mkdir -p d:/4/_tmp_view/scene_bak
cp "d:/4/Unity/MyGame/Assets/CardPresentation/Scenes/DeckEditor.unity" \
   "d:/4/_tmp_view/scene_bak/DeckEditor.unity.bak_20261103"
```

- **只有 `DeckScene.Run` 会重写受版本管理的场景**（`Scenes/DeckEditor.unity`，`DeckScene.cs:1161`）。
- `CollectionScene.Run` 存的是 `Scenes/CollectionCheck.unity`（**已 gitignore**，`CollectionScene.cs:13419`），不必备份。
- `BattleScene.Run` / `ShellScene.Run` / `MainMenuScene.Run` 的 `Run` 入口**不调 `SaveScene`**（`SaveScene` 只在 `BuildAndSaveScene` / `ShellScene.Play` 里）⇒ 不用备份。
- 跑完对一眼：`git diff --numstat` 应只有 `DeckEditor.unity` 一个文件。

### 第 1 步 · `RuleEngineTest.Run`（约 4 分钟）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile "d:/4/_tmp_view/ruleengine.log"
```

- **期望读数**：`RE === 结束：N/N 全过 ✅`。**基线（2026-11-02）= 14072/14072**；本批新增 8 个断言类，**N 会变大**，看的是 **失败 = 0**。
- **逐类读新增行的合计**（grep `B121D1` / `B121D1B` / `B121D3` / `B121D5` / `B121D5R` / `B121E03F_DATA` / `B121WC`）。
- **已知会红（不是回归）**：`EvtKind` 成员计数（已提前改成 **27**）· 卡池 `1142` / `G12` 的 `1010` / `EngineD4` 的 `v49`（都已按新读数改）。若这几处仍红，说明**改的地方没生效**，不是判据问题。

### 第 2 步 · `BattleScene.Run`（约 8 分钟）

```bash
… -executeMethod BattleScene.Run -logFile "d:/4/_tmp_view/battle.log"
```

- **期望读数**：`BT === 合计：N 通过 / M 失败`。**基线 = 10132 通过 / 0 失败**；本批新增 9 条入口（47035 的 8 个 + 45405 的 `RuleEngineTest_B121WB`）。
- **已知会红（判据变更，不是回归）——按这张表认**：

| 红在哪 | 怎么认 | 处置 |
|---|---|---|
| `B97_UI2_Assert` B1/B3 等 | 先看同段 `B2（前提）真 collider 命中 ≥1` | **B2 也红 ⇒ 射线整条没命中（Q04e 判据变更）**；B2 绿而 B1 红 ⇒ 命中了别的卡（真回归） |
| `B97W37_Assert` W37-381-2 | 同上看 W37-381-1 前提 | 同上 |
| `BattleScene.cs:6646 / 6650 / 6652` | `LitSlotCount` 全变 0 | **A16：第一帧已提交、落点指示已清**。改断言（提交前量），⛔ 别改实现 |
| `BattleScene.cs:41369/41372/41374/41377` | 前提「飞行中引擎还没出牌」不成立 | 同上 |
| `BattleScene.cs:39337`（B2b②） | 红且 `nHitOverlap == 1` | **Q04g：`hits[0]` 缓冲序 ≠ z 序**。要按原版重判（⛔ 别把「取 z 最小」加回去） |
| `B120_W16_Assert` A 组 / `BattleScene:19115-19121` / `HandAssertE8:568-590` | X9-B / X9-A 一类 | 首跑确认；前提已改成显式 `3,3` |
| 退出码 **139** | 日志里「合计」已印出来 | **已知间歇段错误，不算失败**（但也别当没看见） |

### 第 3 步 · `DeckScene.Run`（约 2 分钟）

```bash
… -executeMethod DeckScene.Run -logFile "d:/4/_tmp_view/deck.log"
```

- **期望**：`DK === 结束：N/N 全过 ✅`。**基线 = 2372/2372**；新增 `B121_WC_Assert`。
- **注意**：本步会**重写 `DeckEditor.unity`**。跑完 `git diff --numstat` 只该看到它一个。

### 第 4 步 · `CollectionScene.Run`（约 4 分钟）

```bash
… -executeMethod CollectionScene.Run -logFile "d:/4/_tmp_view/collection.log"
```

- **期望**：`[Collection] === 结束：N/N 全过 ✅`。**基线 = 3287/3287**；新增 5 个 `B121_*` 类。
- **⚠️ 重点**：`B121C1 … 合计：通过 X · 失败 Y · **未验 4**` —— 那 4 条是 `COLL-02/03/08/09`，**本条跑绿也不会关掉 `B121-COLL-19`**。
- UI-21 的三族（`B117F14` / `B117_WCARD`）也在这一条里跑。

### 第 5～7 步 · 外壳三件（各 10～30 秒）

```bash
… -executeMethod ShellScene.Run     -logFile "d:/4/_tmp_view/shell.log"      # 基线 6123 通过
… -executeMethod MainMenuScene.Run  -logFile "d:/4/_tmp_view/menu.log"       # 基线 6695 通过
… -executeMethod ShopScene.Run      -logFile "d:/4/_tmp_view/shop.log"       # 基线 3399 通过
… -executeMethod RewardsScene.Run   -logFile "d:/4/_tmp_view/rewards.log"    # 基线 3153 通过
… -executeMethod SettingsScene.Run  -logFile "d:/4/_tmp_view/settings.log"
```

- 为什么要跑：`MenuDraw`（Ban 档）· `PointerLayer`（Ctrl+V）· `BaseEventPopup`（AUD）· `WFSoundPlayer`/`WarpforgeAudio`（WA）都是**共用件**。
- `ShellScene.Run` 里含 `B118W5Assert`（`D118-38` 的三条判据）。

### 第 8～11 步 · 卡面 / 音频 / 数据宿主

```bash
… -executeMethod CardBaseDemo.Run          -logFile "d:/4/_tmp_view/cardbase.log"   # 基线 596 通过
… -executeMethod AnimFXCheck.Run           -logFile "d:/4/_tmp_view/animfx.log"
… -executeMethod CardFaceAssertE5.Run      -logFile "d:/4/_tmp_view/cardface_e5.log"
… -executeMethod AudioSnapshotCheck.Run    -logFile "d:/4/_tmp_view/audiosnap.log"
… -executeMethod VoiceWaveProbe.Run        -logFile "d:/4/_tmp_view/waveprobe.log"
… -executeMethod EnchantmentTemplateCheck.Run -logFile "d:/4/_tmp_view/enchant_tpl.log"   # ⚠️ 预期会红见下
… -executeMethod GameStaticDataCheck.Run   -logFile "d:/4/_tmp_view/gsd.log"              # ⚠️ 预期会红见下
… -executeMethod GameModesCheck.Run        -logFile "d:/4/_tmp_view/gamemodes.log"
… -executeMethod LoadableRefCheck.Run      -logFile "d:/4/_tmp_view/loadable_ref.log"
… -executeMethod PlayerDataMathCheck.Run   -logFile "d:/4/_tmp_view/pdmath.log"
… -executeMethod MatKeyCheck.Run           -logFile "d:/4/_tmp_view/matkey.log"           # 只诊断，无红绿
… -executeMethod NetSelfTest.Run           -logFile "d:/4/_tmp_view/net.log"
… -executeMethod NetBattleTest.Run         -logFile "d:/4/_tmp_view/netbattle.log"
```

- **`EnchantmentTemplateCheck` 预期会红**：模板 106→257 KB、138 行 9 键、卡池版本 49 —— 旧的「条数 / 版本号 / 9 键」断言过期。**按现读数改期望**，⛔ 别放宽。
- **`GameStaticDataCheck`「新卡判定」预期会红**：卡池 1129→1142。同上。
- `MatKeyCheck` **没有 pass/fail**，读法是 `grep "^MK " matkey.log`。

### 第 12 步 · 战场（独立，不与上面的 `Run` 混跑）

```bash
# ① 只重建 leviathan 一场（数据里只有它有 Timeline）
unset ELECTRON_RUN_AS_NODE && WF_ARENA=battlearenaleviathan \
  "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod ArenaBuilder.BuildPrefabFromCLI \
  -logFile "d:/4/_tmp_view/arena_leviathan.log"

# ② 判产物（不开 Unity 就能查）
grep -c "PlayableDirector" "d:/4/Unity/MyGame/Assets/Resources/ArenaPrefabs/battlearenaleviathan.prefab"   # 期望 ≥ 1（现读 0）
grep "\[Arena\] §27 \*\*单场\*\* prefab" "d:/4/_tmp_view/arena_leviathan.log"                              # 期望 "✅ 建好"
grep "\[Arena\] Timeline：" "d:/4/_tmp_view/arena_leviathan.log"                                          # 不该有 "找不到" 警告

# ③ 再跑两条战场探针（它们实例化 13 颗 prefab，比纯数据宿主重）
… -executeMethod ArenaAnimsProbe.Run                  -logFile "d:/4/_tmp_view/arena_anims.log"
… -executeMethod ArenaOriginalShaderRebindProbe.Run   -logFile "d:/4/_tmp_view/arena_shader_rebind.log"
```

- **判读**：`grep '^AAP '`（取到/没取到计数）· `grep '^ASR '`（`ASR ✓` / `ASR ✗`）。
- ⛔ **不要**为这一步跑 `BuildArenaPrefabs()`（13 场全量）——用户 2026-10-11 明令「一场一场做」。

### 第 13 步 · 收口

```bash
git diff --numstat                    # 应只有 DeckEditor.unity（可能还有 Battle.unity 之外的意外，逐条核）
git checkout -- "Unity/MyGame/Assets/CardPresentation/Scenes/DeckEditor.unity"   # 不需要留这次重写时
```

---

## 第三部分 · 真机施工单

> ⚠️ **前置**：先重出 player 包（`B117-36`：现包 `Assembly-CSharp.dll` = 2026-11-02 07:52，早于 11-03 的两处手感修复）。
> 起 player 用 `-wfscene Shell`；点击用 `工具/wf_click.py`（**关沙箱**）；截图放 `_tmp_view/shots/<日期_会话>/`；**验完及时关游戏**。

| 序 | 条目 | 一步操作 | 判据 |
|---|---|---|---|
| 1 | **`B117-R7` Battle 钮点不中** | 进「遭遇战」→ **换一次卡组** → `grep "[Event] Battle! interactable"` Player.log | 值为 `False` ⇒ **按钮被禁用**（不是命中区错）；为 `True` 且点不中 ⇒ 才是命中区/坐标问题，再上 `menu_rect` |
| 2 | **`B121-COLL-19` 四条** | 收藏窗：①点一格卡组（看当前套不变）②点一个阵营（看 Clear 钮出现）③找未拥有/被禁卡（看灰度）④（不可达则记「不可达」） | 逐条截图 + 记数值。⚠️ 宿主断言**不会**帮你判这四条 |
| 3 | **`B121-COLL-25`** | 卡组编辑窗 → 不选任何筛选 → 量 `hdr_clear` 节点 `anchoredPosition.x` | 现实现 = **592.2**；原版 CSF 档 = **612.17**。量到哪个记哪个 |
| 4 | **`B121-COLL-28`** | 收藏窗/卡组窗连点筛选与切页 20 次 | 无掉帧、无 chip 残影 |
| 5 | **`B117-R43` 第 3 行费用数字** | 收藏窗卡池 → **停 3 秒** → 截第 3 行；再读 `FaceCostText` / `FaceCostVisible` / `.text` | `FaceCostText == null` ⇒ TMP 没建（查 `TmpFont.Available`）；容器关 ⇒ `_faceMode/ShowsCost`；三者都在而画面没有 ⇒ **构建时序/重盖**（`_enableRestampPending`） |
| 6 | **`B117-R8` UI-21** | 截 1080p 卡池图 | 与 `cmp_ui21.png` 右半边并排：卡名/阵营/效果文字**不该再明显大于原版** |
| 7 | **`B117-R25` G2 两帧** | 触发 G2 → `t≈0.31` 与 `t≈0.72` 各截一帧 | 只判「有没有播、播的是不是该播的那个」 |
| 8 | **`B121-BF-03` 地堡门** | 进 leviathan 战场 | 地堡门**自动循环动**（原版 `initialTime 15.0` / Loop / 7 clips） |
| 9 | **`D118-57` 三条淡入** | 依次看 发牌淡入 / 阵亡消散 / 相关卡压暗 | 「淡得对不对」（以前被 `c.a = 1` 吞掉） |
| 10 | **`B120-A11` 拖起收拢** | 拖起一张手牌 | 有没有向屏幕中心收的观感（正交模拟不了透视） |
| 11 | **`B120-F17` BGM 过渡** | 打一局，听 BGM 切档 | 无爆音 / 无停顿 |
| 12 | **`B120-Q07b` 帧率** | 打一行 `Application.targetFrameRate` | 拿到真值再定 `fps<=0` 那一档 |
| 13 | **`B120-W15a` 飞牌起点** | 抽牌看起点 | 若偏，回源核 `SetInDeckPosition`（`BattleDriver.cs:41508`），⛔ 别改常量凑 |
| 14 | **`D118-85` HUD 透视** | 看屏幕边角 HUD 元素相对大小 | 正交 vs 透视的差别（`D-1` 前置） |

---

## 第四部分 · 顺手发现

1. 🔴 **`_run_8_checks.sh` 现读是 24 条，不是 27 条** —— `grep -cE '^run [A-Za-z]'` = **24**（含函数定义 `run () {` 时是 25）。而 `报告/B121_宿主接线清单.md:3` 写「现读 **27** 条入口（不是 24）」，与磁盘不符。**照 27 去找会找空**。（我没有改那份报告——不在白名单。）

2. 🔴 **`B118W14_Assert`（`D118-57` 的 ArtOpaque 断言）全仓 0 宿主** —— 除定义文件外 `grep` **0 命中**。它与 `B118W5Assert`（已接 `ShellScene.cs:11214`）不同：**这条跑全套也永远不会执行**。要兜住得先接宿主（建议 `CardBaseDemo.Run` 或 `BattleScene.cs:47035`，都零前提）。

3. 🔴 **`B121-COLL-19` 的四条在断言里就是 `MarkUnverified`**（`B121_C1_Assert.cs:59-63`）—— 这**不是**「跑 Unity 就能关掉」，而是「**要建真窗才可能关掉**」。施工单里已单列。同类：`B121_COLL16_Assert.cs:64` 也有一条 `MarkUnverified`。

4. ⚠️ **`DeckScene.cs:3286` 那条 `CheckRect("hdr_clear", 592.2f, …)` 是自证** —— 它断的是**我们自己选的常量**，所以「绿」不能证明与原版（612.17）一致。这就是 `B121-COLL-25` 必须靠真机定案的原因。

5. ⚠️ **`BattleScene.cs:39337` 的文案会误导**：变量 `nHitOverlap` 名为「重叠命中数」，但 `HitSlotHitCountForTest` 在 `B120-W15` 之后 3D 档返回的是**首条命中 0/1**（`BattleDriver.cs:13703`）。所以那条日志会**恒印「命中 1 张」**。不造成假红，但会让下一个会话以为「只命中一张」。建议顺手改文案（或改读口名）。

6. ⚠️ **`B121_D4_Assert.cs:24` 的宿主路径写错了**：文里写「那一行在 `RuleEngine/RuleEditor/RuleEngineTest.cs`」，实际是 **`RuleEngine/Editor/RuleEngineTest.cs`**（没有 `RuleEditor` 这一级）。

7. ℹ️ **`B120-Q04e / A16 / Q04f / Q04g` 的旧断言行号全部没有漂移**（`B97_UI2_Assert:97-127` · `B97W37_Assert:100-112` · `BattleScene:38804 / 39089 / 6640-6675 / 41365-41379` 逐条现读仍在原处）⇒ 可以直接按行号改，不需要先重定位。

8. ℹ️ **`B120-F19` 的「大概率绿」有实据**：两份 mixer 的组名与 `WarpforgeAudio` 的查找名**逐字一致**（`Voices` / `BattleGroupLow|Medium|High`），资源在 `Resources/Audio/` 下且有 `.meta`。若 Unity 里仍红，**那不是判据问题、是导入问题**。

9. ℹ️ **战场 Timeline 的离线面已经闭合**：`gen_arena_timeline.py --check` 现在跑 **rc=0**（两份 json 逐字节一致），所以第 12 步只剩「重建 prefab + 判产物」两件事，不需要重新生成数据。

10. ℹ️ **`文件所有权_当前.md` 现读「2026-11-03 04:00 · 全部写手已交回，当前无人在飞」** ⇒ 起 Unity 窗口时**没有文件写手在飞**，不需要额外等。

---

## 附 · 本文件的证据来源（全部现读）

- 断言接线：`MyGame/Assets/CardPresentation/Editor/BattleScene.cs` · `CollectionScene.cs` · `DeckScene.cs` · `MyGame/Assets/RuleEngine/Editor/RuleEngineTest.cs`
- 射线链：`MyGame/Assets/CardPresentation/Battle/BattleDriver.cs:33291-33331 / 41107-41160 / 41420-41421 / 13696-13710`
- 提交即出牌：`MyGame/Assets/CardPresentation/Hand/CardInteraction.cs:1084 / 2360-2406`
- 收藏窗：`MyGame/Assets/CardPresentation/Shell/CollectionWindow.cs:3740 / 5342 / 7018 / 7033 / 7055`
- 卡组编辑窗：`MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs:108 / 10308-10345` · `Core/FilterPanelModel.cs:1195 / 1212`
- 战场：`MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs:630 / 741 / 2177 / 3141 / 6465` · `WarpforgeVFX/Runtime/ArenaTimelinePlayer.cs:114 / 128`
- 音频：`CardPresentation/Core/WarpforgeAudio.cs:24 / 345 / 373` · `Resources/Audio/Main Mixer.mixer` · `Resources/Audio/Music.mixer`
- 包与场景：`d:/4/_tmp_view/player/WarpforgePlayer_Data/Managed/Assembly-CSharp.dll` · `d:/4/_tmp_view/*.log`（2026-11-02 基线）
