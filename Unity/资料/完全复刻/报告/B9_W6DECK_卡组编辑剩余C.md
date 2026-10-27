# B9_W6DECK · 卡组编辑线剩余 C 级（K-05 / K-06 / K-08 / K-12）

> 执行代理：`B9_W6DECK` · 2026-10-27 · 判据来源 = `资料/完全复刻/判据/B9_P0剩余C_派活包.md`（K-05/K-06/K-08/K-12 四节）
> ＋ `资料/完全复刻/判据/B9_P0剩余C_施工单.tsv`（按「原版类 + 方法」在 `账/卡组编辑.tsv` / `账/收藏.tsv` 里定位）

## 0. 结论

**本波 4 个包共 6 条，派活时只有 2 条还是 C。**

| 包 | 账行号 | 派活时状态 | 本轮 |
|---|---|---|---|
| K-05 | 卡组:71 · 卡组:131 | **A**（W1K02 · 2026-10-26 已落地） | **只核验、不重做**（见 §2.1） |
| K-06 | 卡组:8 | **A**（W4K06 · 2026-10-26 已落地） | **只核验、不重做**（见 §2.2） |
| K-12 | 卡组:325 | **A**（W4K06 · 2026-10-26 已落地） | **只核验、不重做**（见 §2.3） |
| K-08 | 卡组:75 | **C** | ✅ **本轮做完**（+ 夹具） |
| K-08 | 收藏:273 | **C**（缺那一声音效） | ✅ **本轮做完**（+ 夹具；资产侧由音频线 `B2AU` 同批落地，本轮只补接线） |

- **`TMPDIR=/tmp/wf_w6 bash 工具/typecheck.sh` ⇒ 运行时 0 / 编辑器 0**（末次现跑）。
- 账补丁：`资料/完全复刻/账/_parts/B9_W6DECK.tsv`（2 行，状态 C→A）。已在**只读副本**上试合并 + 过闸：
  `卡组编辑.tsv` A 253→**254 / 通过 254 / 漂移 0 / 失败 0**；`收藏.tsv` A 229→230 / 失败 0（那 5 条漂移全在 `Shell/Inventory.cs`，属 W7，与本轮无关）。
- ⛔ 没跑任何 Unity（`DeckScene.Run` 留给调度台在同步点跑）；⛔ 没动 git / 正本 / 别人的文件。

## 1. 逐条（账行号 | 原版怎么算 | 我们原来怎么算 | 改成什么 | 怎么验）

| 账行号 | 原版怎么算 | 我们原来怎么算 | 改成什么 | 怎么验 |
|---|---|---|---|---|
| **卡组:75**（`DeckDrawer.HandlePointerClick()`） | `DeckDrawer : ItemDrawer<PrebuiltDeck>` **覆写** `HandlePointerClick`：`new CardDeck(item)` → `new DeckInfoContext(deck, **View(2)**, selectButton = null, checkOwnership = shouldCheckOwnership, editButtonAction = null)` → `WindowsManager.OpenWindow<DeckInfoPopup>`（`DeckDrawer.cs:17-28`；`.c` 逐句 = `DeckInfoContext___ctor(ctx, deck, 2, 0, uVar1, 0, 0)`，`uVar1 = *(param_1+0x70)`） | 抽屉本体已建（`Shell/ItemDrawer.cs:1701` 的 `Deck()` / `:1764` 的 `DeckAndCardback()`），但**点这一格仍走基类的「开物品详情窗」** ⇒ 预组卡组开出来的是物品窗 | ① `ItemDrawerView.HandlePointerClick` 首句加 `if (DeckClick()) return;`；② 追加区 `ItemDrawerView.DeckClick()`（按 `Drawer` 判 `DeckDrawer`/`DeckAndCardbackDrawer`）→ `ItemDrawer.ShowDeckInfo(item, _showOwnership)`（`DeckInfoPopup.Create(mgr, -1, View)` + `OverrideDeck` / `OverrideInfo` / `IsPlayerDeck = false` / `CheckOwnership`）；③ `ItemDrawerView.SetShowOwnership(bool)` = 原版 `DeckDrawer.SetShowOwnership`（只写字段，原版 0 调用点 ⇒ 我们也不接） | `Editor/DeckScene.cs` 的 `TestItemDrawerDeckClick`：造一格 `PrebuiltDeck` 抽屉 ⇒ `showItemInfoOnClick=false` 不开窗**也不响**；`true` ⇒ 开窗且 `State=View` / `!IsPlayerDeck` / `CheckOwnership=true` / **`!SelectButtonProvided`（没有 `Select Deck`）** / 督军 = `deck.heroId`（原始数据，⛔ 不拿 `ToPlayerDeck` 反推）；再 `SetShowOwnership(false)` ⇒ `CheckOwnership=false` |
| **收藏:273**（`ItemDrawer.OnPointerClick` ④） | 末句 `inspectSound.Play2D()`；`inspectSound` 的 PPtr 解开 = AudioCue **`ButtonClick 1`** → clip **`UI Click 1`**，pitch/vol 1.0、节流 0.1s；`Play2D(MixerType mixerType = MixerType.FX)`（`out_cpp2il_dev_ilspy/AudioCue.cs:87`）⇒ **FX 组** | `PlayInspectSound()` **只出声说明**（`ItemDrawer.NoteInspectCueMissing()`），因为当时 `Resources/Art/audio/sfx/` 里没有 `UI Click 1.wav` | ① `PlayInspectSound()` → `ItemDrawer.PlayInspectCue()`；② `PlayInspectCue()` = `WFSoundBank.HasCue(WindowSounds.ButtonClick1Cue)` ⇒ `WindowSounds.PlayFx(WindowSounds.ButtonClick1Cue, …)`，查不到才退回原来那条说明（⛔ 不静默、⛔ 不拿别的 clip 顶）；③ cue 名唯一来源 = `Shell/WindowSounds.cs:243` 的 `ButtonClick1Cue`（⛔ 不在别处再写一遍字面量） | `TestItemDrawerDeckClick` 的四条：cue 在表里 · `cue.clips` 含 `UI Click 1` · `WFSoundBank.Clip("UI Click 1") != null` · 点一次 ⇒ `WindowSounds.LastCue == ButtonClick 1`、而 `showItemInfoOnClick=false` 时连 `LastCue` 都是 null |

## 2. 证据（文件:行号）

### 2.1 K-05（卡组:71 · 131）——**已经是 A，核验通过**

- `Core/CostCurveDrawer.cs:245-402`：`RowTween` / `MakeRowTween` / `RegisterTween` / `ApplyFrac` / `Tick` / `Ease.OutQuad` / `Ease.InQuad`。
  - 值补间 **0.2 s**、缓动 = `OutQuad`（= 原版 `DOTweenSettings.defaultEaseType: 6`，**不是我们挑的**）；
  - 立刻写 `ColorWhileUpdating = (1,1,1,1)`（原版 `.ctor` 实读）、色补间 **0.4 s** `InQuad` 回 `FillTint`；
  - `Mathf.Approximately(0, 目标)` ⇒ 整段跳过（原版同一句）；`Tick` 以**长的那条 0.4 s** 收尾；`instant` 档直赋、不闪白。
- `Core/CostCurveDrawer.cs:404-500`：每帧泵（`PlayerLoop.Update` 幂等安装 / `Detach`），批处理不跑 PlayerLoop ⇒ 自检手推 `Tick(dt)`。
- 现读结论：**与施工单要求的「0.2 s 长值 / 0.4 s 渐变 / instant 一帧到位」逐项一致**，本轮**未改一行**。

### 2.2 K-06（卡组:8）——**已经是 A，核验通过**

- `Deck/DeckEditorState.cs:409-458` `CanAddCore` 闸序 = `null` → 督军/防御卡槽（同卡 ⇒ `CopyLimitExceeded`）→ `EffectOnlyCard` → 阵营 → 同名上限(`.c:48`) → 传奇总数(`.c:100-147`) → **整副满(`.c:166`，最后)**。
- 原版交叉核对（`decomp_full/CardDeck__CanAddCard.c:164-176`）：禁卡之后才 `if (0 < EmptySlotsCount) return 1;` 否则给「整副满」那条键 ⇒ **整副满确实在最后**与我们一致。
- 夹具：`Editor/DeckScene.cs` 的 `TestSkirmishMode` 里「满 12/12 + 同名已达上限 ⇒ `CopyLimitExceeded`」那条（W4K06 加的）。

### 2.3 K-12（卡组:325）——**已经是 A，核验通过**

- `Battle/ErrorMessageBanner.cs:1069` `ShowShellError(...)`（无战斗上下文入口，转发 `MessageToast.ShowError`，⛔ 不另建第二颗横幅）。
- `Deck/DeckRuntime.cs:8859` `SayError(bannerText, logPrefix = null)`；两个出口 `:4067`（`BeginCardDragAt` → 带「拖不进去：」前缀**只进日志**）、`:8281`（`TryAddCard` → 屏幕 = 词条本身）。`:8863` 调 `ErrorMessageBanner.ShowShellError`。
- 夹具：`Editor/DeckScene.cs:9995` `TestAddErrorBanner`（满编 +1 条横幅 / 文案 = `DeckErrorText(TooManyCards)` / 合法卡不弹 / 收尾拆件）。

### 2.4 K-08（本轮新做的两条）

- 原版判据：
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/DeckDrawer.cs:15`（`bool shouldCheckOwnership = true`）· `:17-28`（`HandlePointerClick`）· `:30-33`（`SetShowOwnership`）
  - `d:/2/tools/decomp_full/DeckDrawer__HandlePointerClick.c`（`DeckInfoContext___ctor(ctx, deck, 2, 0, uVar1, 0, 0)`；`uVar1 = *(param_1+0x70)`）
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/ItemDrawer.cs:917-951`（`OnPointerClick` 四步，末句 `inspectSound.Play2D()`）
  - `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AudioCue.cs:87`（`Play2D(MixerType mixerType = MixerType.FX)` ⇒ **FX 组**）
- 我们的落点：
  - `Shell/ItemDrawer.cs:1607`（`HandlePointerClick` 首句 `if (DeckClick()) return;`，**同行内替换、行数不变**）
  - `Shell/ItemDrawer.cs:2034` `ItemDrawer.ShowDeckInfo(...)` · `:2079` `ItemDrawerView.DeckClick()` · `:2075` `SetShowOwnership` · `:2011` `LastDeckInfo`（自检读口）· `:2018` `PlayInspectCue`
  - `Shell/ItemDrawer.cs:1971` `PlayInspectSound()` → `PlayInspectCue()`
  - `Shell/WindowSounds.cs:243` `ButtonClick1Cue = "ButtonClick 1"`（cue 名唯一来源）· `:173` `PlayFx`
  - `工具/import_original_sfx.py:254` `"ButtonClick 1"`（在 `EXTRA_CUES` 里）
- 资产（**由音频线 `B2AU` 同批落地，本轮只核**）：
  - `Resources/Art/audio/sfx/UI Click 1.wav` 存在；`Resources/animfx_sounds.json` 里 `ButtonClick 1 → clips: ["UI Click 1"]`、pitch/vol 1.0、`timeToPlayAgain 0.1`；
  - `python -I -X utf8 工具/import_original_sfx.py --check` ⇒ **解析成功 512/512 · 反向查漏 0 候选**（`--check` 不写盘）。
- 夹具：`Editor/DeckScene.cs:10097` `TestItemDrawerDeckClick()`，在 `TestLayout` 尾部 `:9716` 调（本节最后跑，收尾会拆 `WindowsManager` 宿主）。

## 3. 没查清的部分

1. **卡组:75 的真实观感验不了（时间限定 = 本 build）**：`PrebuiltDeck` 实例 225 个里只有 2 个被 Demo `DeckInfo` SO、1 个被 `Intro.unity:7792 starterDeck` 引用，**没有任何商店/奖励 SO 以 `ObtainableItem` 形式发放它**（原版走远端目录）⇒ 玩家在游戏里看不到这一格，**只能靠夹具**。远端目录回来后代码不用改就会可见（前提：远端那批 `PrebuiltDeck` 仍走 `ItemDrawerConfig` 的 `PrebuiltDeck → DeckDrawer` 映射，那条**已取证**＝C）。
2. **`DeckScene.Run` 没跑**（红线：子代理不许跑 Unity）⇒ 夹具是否真绿、`DeckInfoPopup` 在编辑模式下 `Build` 是否顺利，**要等调度台在同步点跑**。风险点已尽量压低：夹具用 `WindowsManager.Get()`（内部 `EnsureHost` 幂等）、收尾走既有 `DestroyPopupScaffold()`、并把 `WFSoundPlayer.Enabled` 临时关掉（批处理不真发声，同 `AnimFXCheck.cs:342` 先例）以免音源池污染。
3. **`DeckDrawer.HandlePointerClick` 里那两个 `0` 的含义**：`.c` 只给位置实参，含义是按 `DeckInfoContext` 构造签名（`deck, state, selectButton, checkOwnership, editButtonAction`）反推的；`out_cpp2il_dev_ilspy` 版那句被 `NoteDecompilerIssue` 打了折（`new CardDeck((PrebuiltDeck)0)`、`checkOwnership = default`）⇒ 我用 `.c` 的**硬编码 2** 与 `*(param_1+0x70)` 为准，与 cpp2il 的 `DeckInfoStates.View` / 字段名互为印证。**若将来拿到未剥体的 `DeckInfoContext` ctor，值得再核一次第 6 个实参**（`0` 现在被我按「没有 editButtonAction」解释）。
4. **`PlayInspectCue` 走 `WFSoundBank.HasCue` + `WindowSounds.PlayFx` 是**壳侧**播放口**；原版 `inspectSound` 是**prefab 上的 AudioCue 组件**。两者净效果等价（2D / FX 组 / 同一 cue），但**「每格一个 AudioCue 实例」这件事我们没有**（全仓只有一张表）——这是既有架构口径（`WindowSounds` 文件头已写），不是本轮引入。

## 4. 改动清单（全部在授权白名单内）

| 文件 | 改动 |
|---|---|
| `Assets/CardPresentation/Shell/ItemDrawer.cs` | ① `HandlePointerClick` 首句加 `if (DeckClick()) return;`（**同行内替换，行数不变**）② `PlayInspectSound()` → `ItemDrawer.PlayInspectCue()`（同行内）③ 文件尾追加区：`ItemDrawer.LastDeckInfo` / `PlayInspectCue()` / `ShowDeckInfo(...)` + `ItemDrawerView._showOwnership` / `ShowOwnership` / `SetShowOwnership` / `DeckClick()` ④ 两处**过期注释**就地订正（「工程没有 `UI Click 1.wav`」→ 已有；`NoteInspectCueMissing` 改成「表里查不到时」的口径） |
| `Assets/CardPresentation/Editor/DeckScene.cs` | ① `TestLayout` 尾部加一节 + 一行 `TestItemDrawerDeckClick();` ② 新增 `TestItemDrawerDeckClick()`（在类尾） |
| `资料/完全复刻/账/_parts/B9_W6DECK.tsv` | 新建：2 行（卡组:75 / 收藏:273，C→A + 落点/差异/备注） |
| `资料/完全复刻/报告/B9_W6DECK_卡组编辑剩余C.md` | 本文件 |
| （未改）`工具/import_original_sfx.py` · `Core/CostCurveDrawer.cs` · `Deck/DeckEditorState.cs` · `Battle/ErrorMessageBanner.cs` · `Deck/DeckRuntime.cs` · `Assets/RuleEngine/Core/DeckRules.cs` | 判据已满足 / 前序会话已落地，本轮零改动 |

**行号守恒核对（账里钉住的落点）**：`Shell/ItemDrawer.cs` 的 `:840 / 1588 / 1592 / 1605 / 1701 / 1871 / 1889 / 1957 / 1968 / 1971` 全部**原位**；
`Editor/DeckScene.cs` 的 `:2712`（卡组:8 的 A 落点）也在原位 —— 现跑 `check_ledger_points`：`卡组编辑.tsv` **漂移 0 / 失败 0**。

## 5. 顺手发现（该谁管由调度台分流）

1. 🔴 **`收藏:273` 的「缺 wav」这一半已被音频线 `B2AU` 同批解决，但账没跟上**：`EXTRA_CUES` 收了 `ButtonClick 1`、`UI Click 1.wav` 已拷进 `Resources`（`.wav` mtime 8-23 = `shutil.copy2` 保留的原包时间戳，`.meta` 是今天）⇒ 本行缺口只剩**接线**，本轮已补并在补丁里写清（含 `--check` 反向查漏 0 候选这条证据）。**建议调度台核一下**：`B2AU` 的包与本包在 `收藏:273` 上有**重叠**（他们只做 key 注册，接线归我）。
2. ⚠️ **跨代理的软依赖**：`ItemDrawer.PlayInspectCue` 引用 `WindowSounds.ButtonClick1Cue`（`Shell/WindowSounds.cs:243`，`B2AU` 今天新加的常量；该文件不在我的白名单里）。若那条常量被改名/移动，我这一行会编不过 —— 现跑 typecheck 0/0，说明当前一致。
3. ⚠️ `账/收藏.tsv` 有 **5 条既有漂移**，全落在 `Shell/Inventory.cs`（`AlternateArtInventory.OnFinishUnpack` / `OnItemCountModified` 歧义、`CardInventory.CreateAlternateArtInfo` 1380→1406、`UpdateCardTier` 1360→1386、`WildcardInventory.UpdateItemCollection` 歧义）—— **属 W7 的文件**，我没动；建议同步点后跑一次 `--fix`（要等 `.cs` 写手停了再跑）。
4. ℹ️ 过程中两次 typecheck 报别人的半成品：先是 `Core/CardView.cs`（17 条 CS0116，W4 正在写）、后是 `Core/CardFeel.cs`（2 条，W2）。按口径**没去碰**，末次已回到 0/0。
5. ℹ️ `ItemDrawer.cs` 追加区头注里「`HandlePointerClick` 那一跳 = 开物品详情窗（`ItemDisplayWindowContext(item, 0)`）」那段**现在只覆盖非卡组抽屉**了 —— 新追加区的头注已写明卡组那一支；⚠️ 若下个会话要改这里，请一并读**文件尾**那段追加区（两处口径必须合起来看）。
