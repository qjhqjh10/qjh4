# 执行代理 D · 界面线（D1–D7）执行报告

会话日期：2026-10-25 · 不跑 Unity、不动 git、不改两张正本与 `任务总表.md`
typecheck：`TMPDIR=/tmp/wf_d1 bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（末次）

---

## D1 `新账-103`：`ParamsCloseOnEsc` 照基座改 1 + 断言

**结论：已落。** 活动承载窗按 ESC 现在会真关窗（并因 `updateNavPanel = 1` 把左竖导航灯拨回 `Main`）。

**判据（第一手）**：基座 `Parent Event Window` 的 MB `6349037533193994564`
（GO `-3290546486736551612`）实读 `closeOnESC = 1` · `updateNavPanel = 1`；
Two Sides 那份 MB `-1838579764277826042` 是 `closeOnESC = 0` · `updateNavPanel = 0`。
这一件是「基座外壳 + Two Sides 两页」的组合 ⇒ **窗参取外壳那一份**（与 2026-10-23 对 `updateNavPanel` 的裁定同一条口径）。

**改动清单**
- `Shell/ParentEventWindow.cs:195` `ParamsCloseOnEsc = false → true`（含 doc 订正；文件头 §① 与窗参注释同步）
- `Shell/ParentEventWindow.cs:546` 写入注释同步（`closeOnESC = 1` 基座那份）
- `Editor/MainMenuScene.cs:17039-17044`：T28 窗参断言**翻面** → `closeOnEsc == true`（附 🧨 反证说明）
- `Editor/MainMenuScene.cs:17537-17540`（ESC 行为条）：**撤掉「夹具」`evt.closeOnEsc = true`**，
  改成断言「窗自己的真值 = true」（原来是靠夹具把第二道门槛借过来）
- `Editor/MainMenuScene.cs:17560-17562`（灭自证）：撤夹具，改为断言新那扇 `closeOnEsc` 也是真值

**需要主对话跑**：`MainMenuScene.Run`（T28 段 + 第 6 颗事件钮那段）。

**没查清**：无。真机上「ESC 到底走没走进这扇窗」仍需真人键盘复核一次（本环境键盘通道可疑，见战斗线同日记录）。

---

## D2 轮抽剩余 5 小件

### ① 去领奖步 / 按口径收尾 —— **已落**
- **收尾条件**（`Shell/DraftModeWindow.cs:2111-2130`）：`joinState == Finished(2)` **或**
  `matchesLost >= MaxTries(3)` **或** `matchesWon + matchesLost >= MatchesToFinish(4)`（新常量 `:264`）
  ⇒ **一律 `Finished(20)`**；删掉了 `rewardsCollected ? 22 : 20` 那一跳。
- **`Reward Info Panel` 整块不建**（`BuildOngoingPage`，`:1784-1789`）：宝箱图 / 领奖钮 / 奖励档位全不做
  （用户 2026-10-24「轮抽不做奖励」；铁律 11 例外②）。`BuildRewardPanel` 保留但**不调用**（doc 写明「恢复奖励时接回来」）。
- **`CollectReward()` 改成兼容口**（`:2745+`）：只出声 + 清掉这一局 + 关窗（= 原版 `ResetCollectedDraft` 的那一半）。
- 🔴 **有意偏离（已写进代码注释）**：原版 `Finished` 把 `Reset Event Button` 也藏起来、唯一出口是领奖钮；
  本版没有领奖步 ⇒ **收尾页保留 Reset** 当出口，否则玩家会卡在「已结束、没有任何按钮」的页上
  （再开窗还是 `Finished`）。`GoToState` 那句 `SetOn(_resetNode, true)` 有 🧨 说明，⛔ 别改回 `!finished`。

**断言已同步（`Editor/DraftAssertD3.cs`，本批白名单内）**
- ⑤ `RewardPanelNode == null`（原来断「显示」）+ 新增「`ResetNode` 仍显示」
- ⑥ 整组重写：3 胜 0 负 ⇒ 仍 `OngoingRun`；第 4 场 ⇒ `Finished`（钉「打满 4 场结束」，取代旧的「不设胜场上限」）
- ② `rewardsCollected = true` 也只落 `Finished`（钉「`Collected` 那一跳已不存在」）
- ③ `CollectReward()` ⇒ 清档 + 关窗

### ② 重掷 —— **已在本批之前做完（`W7`），本次仅核对**
`ReRollPacks()`（`Shell/DraftModeWindow.cs:2500+`）· `ReRollCost` 照 `GetReRollCosts` · 表空 ⇒ 恒免费 · 节点/几何在 `:401-404`。

### ③ 动画 —— **已落（真 Play 才播）**
五组常量（`Shell/DraftModeWindow.cs:270-300`，判据 = 任务总表 §一「④ 动画」那一行）：
开包 `0.6→1 / 0.4s` · 步进 `0.175s` · 选包 `1→1.2(0.3s)→0.6(0.4s)` · 关包 `1→0.6 / 0.4s` · 右栏 Punch `0.05×base / 0.4s / vibrato5 / elastic1`。
实现：`BuildPackOpenTween` / `BuildPackSelectTween` / `BuildPackCloseTween` / `BuildPanelPunchTween`（都可单独用
`CardTween.Mode = Manual` + `CardTween.Advance(dt)` 推进）；生产入口 `PlayPackOpenAnim` / `PlayPickPackAnims`。
- 批处理无帧循环 ⇒ 一律过 `Application.isPlaying` 闸（照 `Shell/CollectionWindow.PlayCardAnimateIn`）；批处理下 `localScale` 显式写回 1。
- 选包流程：真 Play 下「选中那张 SelectAnim + 其余 CloseAnim + 右栏 Punch」播完 **0.7 s** 再 `UpdateState`
  （协程 `PickPackThenAdvance`；原版也是 `serverConfirmedPack && selectPackAnimFinish` 才推进）。
  **批处理/自检仍同步推进**（`isPlaying == false` ⇒ `PlayPickPackAnims` 返回 false）。
- ⚠️ **没做**：`CloseAnim` 的**淡出**（S3 记「缩放+淡出」，但 alpha 常量没读到 ⇒ 只做缩放，注释如实标）；
  红底闪 `Generic Window Red Background Small` 的**时长常量没读到** ⇒ 仍不播（Punch 已做）。

### ④ 一局存档落盘 —— **已落**
`Shell/DraftModeWindow.cs:648-780`：`persistentDataPath/WarpforgeDraft/run.json`，形状逐条照 `Shell/BattleLogData.cs`
（`StoreOn` 门 = `OverrideDir` 或 `isPlaying && !isBatchMode` · 版本号 · 出声 · `JsonUtility`）。
- 读：`Open()` 第一句 `EnsureRunLoaded()`（原版这一局在服务端 `DraftEventSaveGame`，重启也在）。
- 写：`SyncRun`（选将/选包/重掷）· `BeginRun`（新局）· `IngestBattleResults`（战绩）· `UpdateState` 收尾 · `Resign`/`CollectReward`（清档 = 删文件）。
- 自检口：`OverrideDir` / `SaveRunNowForTest` / `ReloadRunForTest` / `DeleteRunForTest`（**绝不碰玩家真档**）。
- 断言（`Editor/DraftAssertD3.cs:535` `RunSaveGroup`）：写盘 → 重读逐字段 → `_run == null` ⇒ 删档。

### ⑤ 预览静音 —— **本批之前已满足，本次仅核对**
`ShowPackPreview` 里一次 `Debug.Log` 说明「`previewPackSound` 的 clip 名查不到 ⇒ 静音」（`:1677-1679`），**没有任何播放调用**。

**需要主对话跑**：`MainMenuScene.Run`（它挂了 `DraftWindowAssertD2.Run()` 与 `DraftAssertD3.Run()`）。
**没查清**：选包红底闪的时长常量（未读到）；`CloseAnim` 的淡出 alpha（未读到）。两条都在代码里如实标「没做」。

---

## D3 锻造按钮完整链路

**结论：链路本身在 HEAD 里已经完整（不是空实现）** —— `OnCraftClicked`（`Shell/CardDetailPopup.cs:1968`）=
传奇且 `owned == 1` 先弹确认窗（`:2016`），否则 `CraftNow`（`:1982`）= `TryCraft`（`:2085`，扣 1 张同稀有度通配符 +
`owned +1`）→ `PlayCraftVfx`（`:2118`，`WarpforgeUiEffect.PlayWildcardCraft(rarity, onButton)` 两件）。
8 件 `Wildcard_Use_*` 表由 `Editor/CardFaceAssertE5.cs` C1/C1b 逐件钉住。
⇒ **本次只做「断言改走真实入口」**（工单的第二半）。

**改动清单（`Editor/CardFaceAssertE5.cs`）**
- C3：删掉 `else pop.OnCraftClicked()` 兜底，**只走 `CraftHit` 的 `WindowButton.Click()`**，并断言 `wb != null`
  （兜底会把「钮这条路断了」也变绿 = 断言自证）。
- C7（传奇闸）：原来直调 `pop3.OnCraftClicked()` ⇒ 改走真实入口，并新增前提
  `wb3 != null && wb3.Interactable`（传奇 + 有通配符 ⇒ 原版 `CheckButtons` 给可交互）。
- **C6 保留直调**（通配符 0 ⇒ 钮本来就灰）—— 它模拟的正是「有人绕过灰钮」那条负例（工单要求保留）。

**需要主对话跑**：`CardFaceAssertE5.Run`（**独立宿主**，`-executeMethod CardFaceAssertE5.Run`；不在任何 `*Scene.Run` 里）。

**没查清**：无（`WindowButton.Click()` 在 `Interactable == false` 时直接 return，行为与真实派发同源，见
`Shell/WindowButton.cs:869-888`）。

---

## D4 等级 → 图案（`DivisionArt` 号 → 图）：从「前缀推断」升级为**字节证据**

**结论：找到第一手 SO 实例，7+6 条映射逐条对上 GUID。**

🔴 **就地订正一条旧结论（铁律 5）**：`Shell/RankedDivisionInfo.cs` 原注释写「`RankedDivisions` 全盘一个实例都没有」
—— **不成立**。SO 在 **`d:/2/tools/assetripper_out/ExportedProject/Assets/MonoBehaviour/RankedDivisionsSO.asset`**
（`S4` 当时只扫 `assets_full` 那 84 个 bundle，没扫这份 AssetRipper 导出）。

**证据链（可逐字节复算）**
1. `RankedDivisionsSO.asset`：`divisions` = 7 条 `{ divisionNumber: 1..7, image.m_AssetGUID }`；
   `ranks` = 6 条 `{ divisionNumber: 1..6, image.m_AssetGUID }`。
2. 每张 Sprite 的 YAML（`ExportedProject/Assets/<名>.asset`）里 `m_RenderDataKey: { <guid>: 21300000 }`
   —— 那个 guid **就是它自己的源资产 GUID**，与 1 的 `m_AssetGUID` **逐条相等**；
   同一文件里 `m_Name` 就是资产名。
3. 反编译口径：`RankedDivisions__GetDivisionData.c` = `Math.Max(n,1)` + `FirstOrDefault(divisionNumber == n)`
   （查不到退回 `Last()`）；`DivisionImageDisplay__Initialize.c` 用返回项 `+0x18` 的 `AssetReferenceTyped` → `Load` → `Image.sprite`。

**逐号对应**
| 段位号 | SO `m_AssetGUID` | 资产名（`Resources/Art/ui_menu/`） |
|---|---|---|
| 1 | `541a2e07625804b1482d2e9522581ef6` | `01-Rook` |
| 2 | `cfab7727194ea472e831a769576cc6dc` | `02-Veteran` |
| 3 | `80afab2fba6a24751815c6f864b8bfd6` | `03-Commander` |
| 4 | `c8be32f4f4efa4b2c9289c48aa489201` | `04-Admiral` |
| 5 | `225606b26794d4c83ab1ea0b584774e4` | `05-Conqueror` |
| 6 | `52405d3262cb443c0b7401b90b025a3b` | `06-Galactic Threat`（导入后 `06-Galactic_Threat`） |
| 7 | `b075dd3b7593c408692ddfd17a74cc3f` | `07-Legend` |

| 名次号 | GUID | 资产名 |
|---|---|---|
| 1 | `46c79894a8b3441a796387317c280306` | `Roman I`（导入后 `Roman_I`） |
| 2 | `f4304ddc1ffdf474ebc30bfba3c7a914` | `Roman II` |
| 3 | `ee72f98c981bd4bcbb45c6ff2fa07502` | `Roman III` |
| 4 | `bbe7723dd016d41eb88cd80d19b01178` | `Roman IV` |
| 5 | `06f63a9c019a4464283bdad006d9b87d` | `Roman V` |
| 6 | `49724dc857ad546ccbe73558f10709ea` | `Roman VI` |

**旁证（与上表互不依赖，仍然成立）**：`A1` 的像素比对 —— 4 = `04-Admiral`（实拍「金 II」NCC 0.707）·
7 = `07-Legend`（实拍「传奇」NCC 0.939）。⇒ 两条独立信号同指。

**改动清单**
- `Shell/RankedDivisionInfo.cs`：`DivisionArt` 与 `RankArt` 两张表的 doc **整段重写**成上面这条证据链
  （每条写清 GUid + 资产路径 + 字段名 `m_RenderDataKey` / `m_AssetGUID`）；表的**内容没动**（本来就对）。
- 代码表仍是唯一落点（`DivisionArtName` / `RankArtName`）；越界 ⇒ null、调用方出声不画（不变）。

**需要主对话跑**：`MainMenuScene.Run`（其中挂了 `RankedArtAssert.Run()`）。
**顺手发现（不在本批白名单）**：`Editor/RankedArtAssert.cs:122-129` 的 A·1 仍在断「所有名字都带序号前缀」——
那正是「前缀推断」的自证式断言。建议改成**逐项精确表**（7 个名字 + 6 个名字），本代理**未改**（文件不在白名单）。

---

## D5 轮抽 Card List 的 `Scroll View`

**结论：已照原版挂上竖滚 + 几何归真 + 新断言。**

**原版判据（协调方给定，已抄进代码注释）**
`m_ScrollSensitivity = 50` · `m_Horizontal = 0` · `m_Vertical = 1` · `m_MovementType = 1`(Elastic) ·
`m_Elasticity = 0.1` · `m_Inertia = 1` · `m_DecelerationRate = 0.135`
（`MonoBehaviour_-1440327974928745787.json:22` 等）；Content VLG `m_Spacing = 2.5`；行高 45 ⇒ 步距 47.5；
视口 **330.97×588.88**；运行期落点 `Scroll View (1530,270)`、`Cards in deck panel (1490,141) 415×786`
（`资料/说明书/04_界面UI/菜单全树.md:4059-4064`）。

**改动清单（`Shell/DraftModeWindow.cs`）**
- 常量：`DeckListR 1542..1858×330..800 → 1490..1905×141..927`（原版运行期实框；旧注释自陈「我们挑的」已删）·
  新增 `DeckScrollR (1530,270)-(1860.97,858.88)` · `DeckRowH 38 → 45` + `DeckRowGap 2.5` ·
  `DeckScrollNotchPx = 50` · 头部三件精确矩形（`DeckListNameR/DeckListCountR/DeckListSepR`）。
- 结构：新增 `BuildCardListPage`（`:1801`）——头部三件 + `Scroll View` → `Viewport`(`ViewportClip.Hang`) → `Content`，
  **建一次常驻**；`MenuScroll.TopAligned` + `Elastic = true` + **`ClipNode` 显式赋值** + `Owner = gameObject` +
  `OnChanged = RebuildDeckListRows` + `SocialPage.RegisterScroll`。
- `RefreshDeckPanel` 的 Card List 段**不再 `Destroy` 重建**（改成开关常驻件 + 只重排 `Content` 行）——
  否则每重建一次就要重登记一遍滚动区（「登记表只增不减」那颗雷）。
- `RebuildDeckListRows`（`:2595`）：行高 45 + 间距 2.5；内容高 = `n×45 + (n−1)×2.5`；行位置走 `MenuScroll.Shift`。
- 读口：`DeckListScrollForTest` / `DeckListContentForTest` / `DeckScrollRectForTest`（`:3076-3082`）。

**断言（`Editor/DraftAssertD3.cs:965-1010` ⑤b）**
- 参数三连（Vertical / Elastic / Inertia）· 视口 330.97×588.88 · `NotchPx == 50` ·
  `Content` 行数 == 卡数 · 内容高 == `n×45+(n−1)×2.5` ·
  **滚一格 = 50 px**（`Wheel(-1)` ⇒ `delta × m_ScrollSensitivity`；12 张本来不满一屏 ⇒ 临时撑高内容并摘
  `OnChanged` 再量，量完还原）。

**没查清 / 仍差**：原版**行模板**是 `Deck Selector Card Info button`（331×45，含底板 / 稀有度渐变 / 边框 /
费用图标 / 名称 / `xN` 张数），我们这页**仍只画纯文字行** ⇒ 行内视觉仍是缺口（协调方这次只要求滚动 + 行高 + 视口，
已如实记在这里，建议另开一条账）。

---

## D6 Alliance Info 页：补 1 条断言

**结论：只补断言，产品提示已存在（`Shell/DraftModeWindow.cs:2583-2590`）。**

**改动清单**
- `Shell/DraftModeWindow.cs:1461` 新增读口 `AllianceHintLogCountForTest`（静态计数）；
  出声那边加了「**每次进入第 3 页只出声一次**」的闸（`_deckAllianceSaid`；离开第 3 页重置）。
- `Editor/DraftAssertD3.cs:1016-1027` ⑦：`SetDeckTab(2)` ⇒ `Alliance Hint` 节点在且显示；
  **同页再刷一次** ⇒ 计数只 +1（钉「出声恰好一次」）。
- ⛔ 不编造排行榜/联盟数据（页面仍只有自拟提示）。

---

## D7 `Avatar Name` 视口外也建节点（照原版逐格建）

**判据**：`d:/2/tools/decomp_full/AvatarTab__InitializeGameObjects.c:140-160` —— 对**过滤后的全表**逐个
`Instantiate`（`:152`），**没有任何视口判据**。

**改动清单**
- `Shell/AvatarTab.cs:395-411`：**删掉** `if (!_scroll.Intersects(r)) continue;`（旧注释自陈「一屏只建看得见的那些」，
  是自加口径）；doc 改成「照原版逐格建」，并如实记代价（469 格全建，真机首开略慢）。
  裁切照旧由 `ViewportClip`（原版 `RectMask2D` 的等价物）负责。
- `Editor/ProfileAssertU2b.cs`：V5 的期望从「视口内才建」改成
  **`cells == 15`（全表全建）+ `nameNodes == cells`**；把原来那句「按既定规矩本来就不建（A184/A435）」**整段删掉**
  （那是把我们的偏离钉成正确 = 自证），改成「照原版逐格建」的文案 + 🧨 反证（把 `Intersects` 早退加回来就红）。

**需要主对话跑**：`MainMenuScene.Run`（V5 在 `ProfileAssertU2b.Run()` 里，`MainMenuScene.cs:8288` 挂载）。
**没查清**：`cells == 15` 依赖 `ProfileData.Avatars` 的条数（数据驱动）——若数据表变了这条会红，
届时按实际条数改断言、**别改回「视口内才建」**。

---

## 需要主对话跑的宿主（汇总）

| 宿主 | 覆盖 |
|---|---|
| **`MainMenuScene.Run`** | D1（T28 窗参 + ESC 行为/灭自证）· D2（`DraftWindowAssertD2` + `DraftAssertD3`：收尾/存档/滚动/Alliance）· D4（`RankedArtAssert`）· D7（`ProfileAssertU2b` V5） |
| **`CardFaceAssertE5.Run`**（独立入口） | D3（C3/C6/C7 真实入口） |

（本批没有动 `DeckScene` / `BattleScene` / 引擎；`ShellScene.Run` 与本批无直接交集，按主对话口径决定是否搭车。）

---

## 顺手发现（只报不改）

1. 🔴 `Editor/RankedArtAssert.cs:122-129`（**不在本批白名单**）：A·1 仍断「名字都带序号前缀」= 前缀推断自证式断言，
   建议换成逐项精确表（D4 已给出 7+6 条）。
2. `资料/普查产出_1024b/S4_模式窗细节与徽章.md` §① 「`RankedDivisions` SO 全盘没有实例」**已过期**（SO 在 AssetRipper 导出里）。
   `资料/普查产出_1024b/S3_轮抽窗原版规格.md` 里「滚动区没有」之类与本次 D5 相关的旧结论也应一并订正（主对话统一落正本）。
3. `Shell/DraftModeWindow.cs` 的 `BuildRewardPanel` 现在是**死方法**（保留作将来恢复奖励的入口，doc 已写明）——
   若决定彻底删，连 `RewardPanelR/CollectR/CollectTxR/CrateR/...` 那组常量一起清。
4. 轮抽 Card List 的行模板仍是纯文字（见 D5「没查清 / 仍差」）。
5. `DraftModeWindow` 新落盘层是**第一次让轮抽状态跨进程**：`ProfileData` / `BattleLogData` 那两套的
   `OverrideDir` 约定已照抄，但**没有**做「多进程同时写」的锁（同其它本地存档，风险等级相同，如实记）。

---

# 追加批（D8 + 两条顺手发现）

## D8 菜单侧换位音 `CardMove`

**结论：已落。**

**判据（一手）**：原版 `CardDisplayWindow__ChangeCardPosition.c:68-73` —— 换位状态置真（`+0x150 = 1`）**之后**、
`SetImageOverDraw` 与两条换位补间**之前**，一声 `SoundManager.Play2D(<soundOnCardSwap>, MixerType 0 = FX)`。
cue = **`CardMove`**（pid 4812904303840730115 · 4 clip）；菜单侧 `CardDetailPopup` 的根 MB 与战斗那扇
`CardDisplayWindow` **同一个 pid（2033）** ⇒ 同一个常量 `CardDisplayWindow.SwapSoundCue`（`Battle/CardDisplayWindow.cs:249`）。
数据侧 `Resources/animfx_sounds.json:721` 已有 `CardMove`（`CardMove` / `_1` / `_2` / `_3`）。

**改动清单**
- `Shell/CardDetailPopup.cs:1062-1073`：`_swapping = true;` 之后、`b.ArtOverdraw = true;` 之前加
  `WindowSounds.PlayFx(CardDisplayWindow.SwapSoundCue, "[CardDetail] 换位音（原版 `soundOnCardSwap` → `CardMove`）");`
  —— 参数形制逐字抄 `Battle/CardDisplayWindow.cs:1592`（只传 cue + 日志上下文，⛔ 不自己发明音量/pitch；
  随机参数全由 cue 表给：pitch 0.8–1.1 / vol 1 / ttp 0.1）。
- `Editor/CardFaceAssertE5.cs` 新增 **D 组**（`RunSwapSound` + `CheckSwapSoundCallSite`）：
  - **D1 负例**：只开窗 / 重建（`ShowCard`）⇒ `WindowSounds.Played == 0` 且 `LastCue != "CardMove"`；
  - **D3 正例**：`SwapToFront(1)` ⇒ 该 cue **恰好 1 次**（`Played == 1 && LastCue == "CardMove"`；
    自检期间 `WFSoundPlayer.Enabled = false`，不建音源、不出声）；
  - **D4a/D4b 顺序（读源码）**：`_swapping = true;` < `PlayFx(CardDisplayWindow.SwapSoundCue…` <
    `b.ArtOverdraw = true;`，且该调用**恰好一处**（批处理补间不推进 ⇒ 纯运行期断言证明不了顺序）；
    ⚠️ 读不到源码 = 报红（「没验到」≠「验过没问题」）。
  - 样本选取：遍历卡池找第一张**带相关卡**的卡（`RelatedCards.Find`，扇形要有第 1 格才能换位）。

**需要主对话跑**：`CardFaceAssertE5.Run`（独立入口）。
**没查清**：无。已核：菜单侧 `SwapToFront` 在其它宿主断言里**零调用点**（BattleScene 那几处是**战斗窗**
`cdw.SwapToFront`，不同类），所以这一声不会打乱既有「Played 计数」类断言。

## 顺手发现 ①：`RankedArtAssert` 换成**逐项精确表 + GUID 出处**

**改动**（`Editor/RankedArtAssert.cs`，本次由协调方授权扩白名单）
- 文件头 A·1 的描述从「两位前缀序号 == 位置」改成「逐项等于 `RankedDivisionsSO` 的 `divisionNumber → image` 查表结果」，
  并写清证据链（SO `m_AssetGUID` ↔ Sprite `m_RenderDataKey.guid`）。
- `CheckDivisionTable`：新增 `guidHead[]`（7 条源资产 GUID 前 8 位），每条断言文案打印
  「SO `divisionNumber = N` → `m_AssetGUID …` → Sprite `m_Name`」；变量 `allPrefix` → `boundsOk`（不再叫「前缀」）。
- `CheckRankTable`：同样新增 6 条 `guidHead[]` 并打进文案。
- 「期望值从哪来」那一节改成「第一手查表」，删掉「解包资源里的原始文件名」那句**唯一依据**式表述
  （文件名仍成立，但现在只是**交叉旁证**）。

## 顺手发现 ④：`BuildRewardPanel` 死方法 —— 保留 + 断言「没人调」

**改动**
- `Shell/DraftModeWindow.cs`（方法头，上一批已加）：明确写它是「**恢复奖励的唯一入口**」，⛔ 别另写第二份面板。
- `Shell/DraftModeWindow.cs` 的 `Build()` 复位块新增 `_rewardPanelRoot = null;`（重建时不留旧引用）。
- `Editor/DraftAssertD3.cs` 新增 **(E) 组** `RewardPanelNotCalledGroup`（读源码）：
  ① `BuildRewardPanel` **只有一处定义**；② `BuildRewardPanel(` **零非注释调用点**（接回去就红 = 领奖步静默复活）；
  ③ 源码里确有显式 `_rewardPanelRoot = null`（「整块不建」的落点）。

**typecheck**：以上全部改完 → **运行时 0 / 编辑器 0**。
**需要主对话跑**：`MainMenuScene.Run`（其中挂 `RankedArtAssert.Run()` 与 `DraftAssertD3.Run()`）+ `CardFaceAssertE5.Run`。

---

# 第三批：`CardFaceAssertE5.Run` 的 C3 那一红 —— **α（断言写错），已修**

**日志**（`d:/4/_tmp_view/20261025/e5.log`，整轮 49 ✓ / 1 ✗）：
```
E5 ✗ C3 点一下（走真实入口 CraftHit 的 WindowButton.Click()）= 真发请求：通配符 3→2 · 拥有数 11→12
```
🔑 **关键读法**：文案里的 `3→2` / `11→12` 就是实测值 —— **两个数都动了**（请求确实发出去了），
而 C4/C5（同一拍播了 2 件特效、按稀有度选件）也**全绿** ⇒ 实现没坏。

**根因（α · 断言写错）**：C3 的判据写成了 `Check(wb != null && 通配符-1 && owned+1, …)`，
把 `wb != null` 放在**点击之后**求值。而 `DoCraft()` 末尾是 `RebuildKeepingState()` → `Build()`
（`Shell/CardDetailPopup.cs:2189` / `:760-765` 的 `MenuDraw.ClearChildren(transform)`）—— 它把**整棵子树销毁重建**
⇒ 点完之后那颗 `WindowButton` 是**已销毁组件**，Unity 重载的 `!= null` 对它返回 **false**
⇒ 数明明动了也判红。同族先例 = `Editor/MainMenuScene.cs` 的「首格点击后节点被重建 ⇒ 再查恒 null」。

**改了什么**（`Editor/CardFaceAssertE5.cs`）
- C3：新增 `bool hadBtn = wb != null;`（**点击之前**取），判据改 `hadBtn && 通配符-1 && owned+1`；
  文案补上「判空必须在点击之前」的 🧨 说明。
- C7（同族隐患，顺手一起收）：同样改成**点击前**取 `hadBtn3 = wb3 != null && wb3.Interactable`，
  并**删掉** `else pop3.OnCraftClicked()` 那条兜底（兜底会让「钮这条路断了」也变绿）；
  顺手订正一处错字（「传奇 with owned==1」→「传奇且 owned==1」）。
- **W 负例保留**：C6（通配符 0 ⇒ 钮本来就灰）仍是**直调 `OnCraftClicked()`**，模拟「有人绕过灰钮」。

**实现侧一行未改**（`Shell/CardDetailPopup.cs` 本轮无改动）。

**需要重跑**：`CardFaceAssertE5.Run`（独立入口）。预期 50/50。


