# B149 · 「自设计」逐条判定 —— Core / Shell / Deck / Hand 四目录

> 只读判定。输入 = `判据/自设计清单.md` §2 明细里位置落在 `CardPresentation/{Core,Shell,Deck,Hand}/` 的 **170 条**（Hand/ 下 **0 条**）。
> 判据源：`d:/2/tools/decomp_full/`（`.c` 优先）· `cpp2il/out_cpp2il_dev_ilspy` · `assetripper_out/ExportedProject` · `assets_full` · 本地官方本地化表 `数据/游戏数据/官方本地化_13lang.json`（10,862 键，13 语言）。
> 日期 2026-11-05。用户口径：「所有与原版不一样的地方都需要改成原版」，唯一例外 = 判据在已关服远端且本地永远拿不到（B 档必须给反证）。

## 一、汇总（170 条）

| 档 | 条数 | 含义 |
|---|---:|---|
| **A 能改成原版** | **28** | 原版值/行为已拿到（prefab 字段 / `.c` / 官方词条 / 反编译常量），给出落点即可改 |
| **B 判据在远端或原版没有** | **62** | 已给反证：服务端字段本地 0 命中 · 原版方法体空 · 官方 10,862 键 0 命中 · 全库字节 0 命中 |
| **C 架构级** | **8** | 渲染队列梯子 / 承载方式（uGUI 兄弟序 vs 世界空间 quad）不同，改它要动整层 |
| **D 误标（已是复刻）** | **72** | 清单生成后代码已改回原版，或本来就是原版读数（逐条给判据） |

**分布**：Core 54（A 5 / B 10 / C 1 / D 38）· Shell 111（A 22 / B 51 / C 6 / D 32）· Deck 5（A 1 / B 1 / C 1 / D 2）· Hand 0。
**新发现（清单外）**：① 官方 I2 全表 2026-10-18 已全量落本地（10,862 键 / 13 语言）⇒ 清单里多条「词条在远端 CCD、本地没有」**已过期**，可 A 化（#83/#90/#97/#154）；② `DailyStreakWindow` + `Daily Streak Popup.prefab` **原版存在**（清单多条写「原版没有这一页」，不成立）⇒ #103/#105 可照原版，#102/#104 属承载方式差异。

## 二、A 档最该先改的 10 条（按「玩家看得见 + 改动小 + 判据硬」排序）

1. **#38 `Core/CardView.cs` 数值命中半径** —— 原版是四个数值容器的 rect（`Health/Range/Melee/Cost Container`，各 `0.4×0.4` 卡单位，`assets_full/bundle_staticgeneralassets_assets_all/GameObject/`）；我们自选 `StatHitR≈0.2+`。改成按容器 rect 判（半宽 0.2）。
2. **#80/#81/#147/#148 奖励物品格 `200×300`** —— 四处同一口径；原版尺寸在 `ItemDrawer` 取的抽屉 prefab 里（本地已 dump）。量出来改常量。
3. **#39 护甲数值色** —— 原版数值色 = 纯白；我们自选冷白。改 `(1,1,1)`。
4. **#90 禁言提示** —— 官方键 `MainMenu/Chat/ChatBanned` = `Account muted due to reports…` / `由于遭到违反《行为准则》的投诉，账户已被禁言`。换键，删自造句。
5. **#48 新建卡组默认名** —— 官方键 `MenuDeck/HUD/NewDeck` = `New deck` / `新卡组`（中文零变化）。两处（`DeckEditorState.cs` + `DeckRuntime.SetDeckName`）换键。
6. **#231 类型标签 `Stratagem` → `Stratagems`** —— 原版 TMP 原文是复数（`MenuDeck/HUD/DeckDescription/Spells`），我们存的单数是旧近似（`Core/FilterPanelModel.cs:424-427`）。
7. **#105 每日连胜文字色** —— 原版 `Daily Streak Popup.prefab` 的 TMP `m_fontColor` 全 `(1,1,1,1)`；我们自选金/灰。改白。
8. **#146 推荐码输入上限 64** —— 原版 `m_CharacterLimit = 0`（不限长）。改 0。
9. **#74 开包动画采样步长 `1/30`** —— 原 clip 是 60 Hz 关键帧（本地有 7 份 `.anim`）。改 `1/60`。
10. **#117 / #144 / #265 CSF 宽 0 的三处框** —— 原版这些节点宽 0、靠 `ContentSizeFitter(PreferredSize)`；我们用自选定宽。改成「量字宽」（本仓已有原版等价实现：`SkirmishEventWindow.PreferredWidthPx`）。

## 三、逐条判定

| # | 位置（file:line） | 档 | 原版判据 | 我们值 | 改法 | 依据 |
|---:|---|:--:|---|---|---|---|
| 29 | Core/CardFan.cs:44 | D | 原版 1 主卡 + `relatedCard1..4`（`CardUI (0..4)`） | 窗口读 `OriginalSlots`(5)；9 槽表只喂自检 | 无需改；若要清账，删槽 5–8 外推项 | `Battle/CardDisplayWindow.cs:314-315`（2026-11-02 `B117-27`） |
| 30 | Core/CardFeel.cs:174 | B | 原版按**逐卡** UnitTweenSO（`Impact Heavy`/`Impact Target Medium`）分档 | `HeavyHitDamage = 4` 按伤害分档 | 仅当能挖出逐卡映射再改 | `card_vfx_by_card.json` 内 `Impact` **0 命中**（实测） |
| 31 | Core/CardFeel.cs:1816 | D | 原版一定走材质溶解 | 取不到溶解材质时退回淡出并出声 | 不改；shader 已在包内 | `CardView.cs:7183 DissolveSupported`；`B141_W47_Assert.cs:153` 已把「支持溶解」设为前提 |
| 32 | Core/CardFeel.cs:3132 | D | 敌方发牌也是原版一条路 | 敌方走 `DealDurationFoe` + `DealInQuad` | 不改（旧注「缺件」已过期） | `CardFeel.cs:3201` 现读 |
| 33 | Core/CardFeel.cs:3232 | D | DOTween 默认缓动 = OutQuad（原版**没有**改过） | 照 OutQuad 写 | 不改 | `decomp_full/` 内 `defaultEaseType` **0 命中**（无 setter） |
| 34 | Core/CardText.cs:76 | B | 原版 1,198 条 `Card_Name` 里无这 26 张 | 自造 26 张卡的键名自拟 | 属「自造卡」本身，非键名问题 | `RuleEngine/Data/StarterCards.cs:26` |
| 35 | Core/CardText.cs:82 | A | 官方表**无** `Battle/Phrase/` 族；但结算那三句有原版键 | 11 条自拟短语，其中 `YOU WIN`/`YOU LOSE` **正在上屏** | `YOU WIN→Battle/BattleEnd/Victory`、`YOU LOSE→…/Defeat`；其余 9 条零消费点或回 prefab 定 | `BattleDriver.cs:32366`；官方表 `Battle/Phrase` 0 命中 |
| 36 | Core/CardText.cs:84 | B | 官方表**无** `CardEffect/` 族；`Card_Abilities/*`(120) 是逐卡整句、非参数化句式 | 效果小字自拟句式 | 无可照的原版句式 | 官方表 `CardEffect` 0 命中 |
| 37 | Core/CardText.cs:698 | D | —— | 仅注释（判据指向 `Loc.cs`） | 不改 | 同文件注释 |
| 38 | Core/CardView.cs:9580 | A | 每个数值容器挂 `EverguildTooltipTrigger`，rect = 容器 rect（`0.4×0.4` 卡单位） | `StatHitR` 取「0.4 的一半多一点」 | 按容器 rect 判（半宽 0.2） | 同文件 `:9830-9835` 自述判据；`assets_full/…staticgeneralassets/GameObject/` |
| 39 | Core/CardView.cs:12702 | A | 原版数值色 = 纯白 | 护甲用冷白（自选） | 改 `Color.white` | 同处注释「原版数值色是纯白」 |
| 40 | Core/Loc.cs:1440 | A | 原版查不到描述时**只有图标+标题、没有正文**（`EverguildTraitTooltipItem`） | 补一句「没有这个词的条目」（会上屏） | 删该句（或只留空正文） | `资料/tooltip_原版规格与实现.md` §五 |
| 41 | Core/Loc.cs:2242 | D | 原版 `Generic Multi Card Display Combat` 里没有底部提示行 | 已于 2026-10-22 连词条一起删 | 不改 | `Loc.cs:2241-2243` 现读 |
| 42 | Core/Loc.cs:2522 | B | 原版联机走 Photon，无 UPnP 族词条（官方表 0 命中） | P2P/UPnP 九条提示自拟 | 保留（我们自研功能） | 官方表 `Settings/Online/Upnp` 0 命中 |
| 43 | Core/Loc.cs:2596 | B | 官方表无 `Settings/{General,Graphics}/Flash/*` | 12 条状态行自拟（会上屏） | 保留（我们自研联机/画质状态） | 官方表该族 0 命中 |
| 44 | Core/PlayerBoot.cs:46 | B | `TutorialManager.ForceCompleteAll` 有定义、**0 引用** | 只留命令行开关 | 不改 | `decomp_full` 只有定义无调用方 |
| 45 | Core/Tooltip.cs:113 | C | 原版靠 Canvas 兄弟序定前后 | 显式渲染队列 4078 | 不改（队列梯子属架构） | 本仓 `ImageQuad` 世界空间模型 |
| 46 | Core/Tooltip.cs:128 | D | `BasicToolTip.prefab`：根 `m_SizeDelta (500,100)`、正文 `Text` | `PadX 15.55 / MinW 500 / …` | 不改（已按原版字段） | `Tooltip.cs:128` 现读（B141 已订正） |
| 47 | Core/VoiceLines.cs:450 | D | 原版出场只播 `intro`/`default`，不播 `line` | 不再落 `line` | 不改 | 同处注释 + `GetChatSound.c` |
| 48 | Deck/DeckEditorState.cs:264 | A | 官方键 `MenuDeck/HUD/NewDeck` = `New deck`/`新卡组` | 自拟键 `MenuDeck/NewDeckName` | 换门牌到官方键（两处） | 官方表现读；`DeckRuntime.SetDeckName` 同一串 |
| 49 | Deck/DeckRuntime.cs:250 | D | `RowH 55.7` + `RowSpacing 2.0`（prefab `VerticalLayoutGroup` 实读） | `RowPitch = 57.7` | 不改 | 同处注释三条互证；`A1721` 已订正 |
| 50 | Deck/DeckRuntime.cs:1349 | D | `CardDeck.GetEmptyDeck`：名称空 / 战将 null / 防御 null / `deckArmy Neutral` | 同原版（`Name = ""`） | 不改 | `CardDeck__GetEmptyDeck.c:25-31` |
| 51 | Deck/DeckRuntime.cs:3785 | C | 原版靠 Canvas 层级 | `QDragPreview = 3050` | 不改（队列梯子属架构） | 同处注释 |
| 70 | Shell/BaseOfferPopup.cs:142 | D | 出厂文本照 prefab 原样 | 原样用、不自编 | 不改 | 同处注释块（21/21 实读） |
| 71 | Shell/BattleLogTab.cs:142 | B | 原版无打开点（74 处 `OpenWindow` + 91 包搜 GUID **0 命中**） | 我们补一颗入口钮（用户 2026-09-27 拍板） | 不改（用户拍板） | 同处注释 |
| 72 | Shell/BlockingOverlay.cs:46 | C | 原版无此绝对队列 | `QDim 3338 / QSpinner 3339` | 不改（队列梯子） | 同处注释 |
| 73 | Shell/BoosterInfoPopup.cs:171 | D | prefab 出厂文本 | 原样用 | 不改 | 同处注释 |
| 74 | Shell/BoosterPackAnim.cs:97 | A | 原 clip 关键帧 60 Hz（7 份 `.anim` 本地有） | 采样步长 `1/30` | 改 `1/60` | 本文件头「等价物」段 |
| 75 | Shell/BoosterPackAnim.cs:301 | D | 原式 `localScale × (1+4×clamp01(delay))`，原 `localScale = 151` | 换算成本仓 px（1~5） | 不改（量纲等价） | 同处注释 |
| 76 | Shell/BoosterPackAnim.cs:348 | B | DOTween shake preset 的 sustain/attack/decay 本地 0 命中 | 相机抖 0.5s | 保留 | 同处注释 |
| 77 | Shell/BoosterPackOpenWindow.cs:277 | A | 原版翻完 `state` **停在 3** | 我们加 `5 = 翻完了` | 删自加档，按 3 + `CardClicked` 监听 | `CardInBoosterPack__ChangeState.c` |
| 78 | Shell/CampaignData.cs:38 | B | 原版是真实经济（服务端余额） | 战役点固定给足 | 不改（用户 2026-09-17 边界②） | 同处注释 |
| 79 | Shell/CampaignData.cs:140 | B | 进度在服务端存档 | 初值自选 | 保留 | 同处注释 |
| 80 | Shell/CampaignRewardWindow.cs:196 | A | 尺寸在 `ItemDrawer` 取的抽屉 prefab 里（本地已 dump） | `ItemW 200 / ItemH 300` | 量 prefab 后改常量 | 同处注释（A86 已证前提为假） |
| 81 | Shell/CampaignRewardWindow.cs:202 | A | 同上（同一条声明的续注） | 同上 | 同上 | 同上 |
| 82 | Shell/CampaignRewardWindow.cs:219 | D | —— | 图片名映射（空格→下划线） | 不改 | 同处注释 |
| 83 | Shell/CampaignRewardWindow.cs:248 | A | 官方 I2 全表 **10,862 键已在本地**（2026-10-18 并入） | 按 term 末段自填 | 用官方表按 `mTerm` 取值 | `数据/游戏数据/官方本地化_13lang.json` |
| 84 | Shell/CampaignTab.cs:647 | D | 100² 节点上原版也读不出短名 | `NamePx = 0` | 不改 | 同处注释 |
| 85 | Shell/CampaignTab.cs:1427 | B | 三态两个输入来自服务端（`Campaign.IsPremium` + 当日 collected） | 本地三态推导 | 保留 | `CampaignPremiumPointsPanel.Initialize` 反编译 |
| 86 | Shell/CardDetailPopup.cs:114 | B | 升级表原版在服务端 `CardTierConfig` | `{0,0,500,1000,2000}` | 保留（用户 2026-09-22 裁决自拟） | `CLAUDE.md` 铁律 18 表 |
| 87 | Shell/CardDetailPopup.cs:724 | D | 原版 1+4 = 5 槽 | 已于 2026-11-02 改成 `CardFan.OriginalSlots` | 不改 | `CardDetailPopup.cs:788-789` |
| 88 | Shell/CardTierConfig.cs:133 | B | 4 标量原版在服务端配置档 | `Y_Max 100` 等自拟 | 保留 | 同处注释 |
| 89 | Shell/ChatPanel.cs:114 | D | `ChatRoom { Global = 0, Alliance = 1 }` | 同两频道 | 不改 | `Warpforge_code/Scripts/Assembly-CSharp/ChatRoom.cs:1-5` |
| 90 | Shell/ChatPanel.cs:702 | A | 官方 `MainMenu/Chat/ChatBanned` = `Account muted due to reports…`/`由于遭到…禁言` | 自拟英文句 | 换官方键 | 官方表现读 |
| 91 | Shell/CollectionWindow.cs:3711 | C | 原版靠**平移**到 `hiddenPosition (−550,0)` | 我们 `SetActive`（命中区是静态 `PxRect`） | 不改（命中区架构） | 同处注释 |
| 92 | Shell/CollectionWindow.cs:6241 | D | 原版 `UpdateOrder(filteredCollection)` | 只重排本组、其余钉回原位 | 不改（对齐结果） | 同处注释 |
| 93 | Shell/DailyData.cs:127 | B | 周常进度由服务端下发（实拍 25/30 是那个玩家当时的值） | mock 13 | 保留 | 同处注释 |
| 94 | Shell/DailyData.cs:499 | B | 任务内容本来就由服务端下发 | 池内文案/数值自选 | 保留 | 同处注释 |
| 95 | Shell/DailyData.cs:743 | B | 每日奖励内容/里程碑服务端下发 | 自选 | 保留 | 同处注释 |
| 96 | Shell/DailyData.cs:760 | B | 原版 Premium 由商店购买 | 单机恒 `PremiumLocked` | 保留（用户边界②） | 同处注释 |
| 97 | Shell/DailyData.cs:814 | A | 官方表已在本地（10,862 键） | 自拟词条 | 按 term 取官方两列 | 官方表现读 |
| 98 | Shell/DailyData.cs:952 | B | 断签状态由服务端 `currentValue` 给 | 默认不断签 | 保留 | 同处注释 |
| 99 | Shell/DailyData.cs:1051 | B | 原版 `MainMenuMission.FailedValue`（服务端） | `StreakLostValue = 10` | 保留 | 同处注释；prefab 出厂文案 `Streak lost: 10` 与之一致 |
| 100 | Shell/DailyData.cs:1328 | B | 500 金块是服务端奖励值 | 自选 | 保留 | 同处注释 |
| 101 | Shell/DailyData.cs:2080 | B | 随 #93 | 出厂值引用 | 保留 | 同处注释 |
| 102 | Shell/DailyStreakTab.cs:38 | C | 原版是 `Daily Streak Popup` **弹窗**（非侧栏页） | 我们做成 tab（页矩形自选） | 不改（承载方式差异） | `assetripper_out/…/Daily Streak Popup.prefab` 存在 |
| 103 | Shell/DailyStreakTab.cs:48 | D | 原版标题 `m_text = Daily Streak` | `TitleText = "Daily Streak"` | 不改 | `Daily Streak Popup.prefab` TMP `m_text` 现读 |
| 104 | Shell/DailyStreakTab.cs:50 | C | 原版矩形在弹窗 prefab 里 | 三条矩形自选 | 不改（同 #102 承载差异） | 同上 |
| 105 | Shell/DailyStreakTab.cs:58 | A | 原版 TMP `m_fontColor` 全 `(1,1,1,1)` | 金 `#F4E1AC` / 灰自选 | 改纯白 | `Daily Streak Popup.prefab` 现读 |
| 106 | Shell/DeckSelectionPopup.cs:258 | D | prefab 上 `Localize.mTerm` 原文 | 已用该 term | 不改 | 同处注释（`MB_4152179270747796863.json`） |
| 107 | Shell/DraftModeWindow.cs:254 | B | `StagedEventData.maxTries (+0x90)` 服务端、本地 0 命中 | 3 | 保留 | 同处注释 |
| 108 | Shell/DraftModeWindow.cs:261 | B | `StagedEventData.matches (+0x88)` 服务端 | 打满 4 场收尾 | 保留 | 同处注释 |
| 109 | Shell/DraftModeWindow.cs:313 | B | `DraftEventData.reRollCosts (+0xE0)` 服务端 | 空表（免费） | 保留 | 同处注释 |
| 110 | Shell/DraftModeWindow.cs:420 | B | 原版该 `ScrollRect` 被禁、`m_ScrollSensitivity` 读不到 | 一格 120 px | 保留 | 同处注释 |
| 111 | Shell/DraftModeWindow.cs:454 | B | `UICardInfoItem` 模板 prefab 尺寸本地解不出 | `PackCardsArea` 自选 | 保留 | 同处注释 |
| 112 | Shell/DraftModeWindow.cs:2159 | B | 档位由服务端下发 | 用键名反推 | 保留（并出声） | 同处注释 |
| 113 | Shell/DraftModeWindow.cs:2732 | B | Alliance Info 需联盟数据/排行榜（服务器） | 自拟提示 | 保留 | 同处注释 |
| 114 | Shell/DraftPacks.cs:75 | B | `DraftEventData.draftRounds` 服务端 | `RoundsPerDraft = 4` | 保留 | 同处注释 |
| 115 | Shell/DraftPacks.cs:79 | B | `packsPerDraft` 服务端 | 3 | 保留 | 同处注释 |
| 116 | Shell/DraftPacks.cs:84 | B | 随 #114 | `4×3 = 12` | 保留 | 同处注释 |
| 117 | Shell/ExpansionPassEventWindow.cs:275 | A | 原版 `Title` 宽 0、靠 `ContentSizeFitter(MinSize)` | 定宽 1312 | 改「量字宽」（复用 `PreferredWidthPx` 写法） | 同处注释；先例 `SkirmishEventWindow.cs:713` |
| 118 | Shell/ExpansionPassEventWindow.cs:385 | B | 原版取 `LiveOpsAssetUtility.GetLabel(事件)`（远端） | 官方通用词条兜底 | 保留 | 同处注释 |
| 119 | Shell/ExpansionPassEventWindow.cs:656 | B | 同上 | 同 118 | 保留 | 同处注释 |
| 120 | Shell/ForgeData.cs:31 | B | 真实格数在服务端；prefab 只有占位文案 `Level 1/50` | 50 | 保留 | 同处注释 |
| 121 | Shell/ForgeData.cs:70 | B | 奖励表服务端 | 每级轮换自选 | 保留 | 同处注释 |
| 122 | Shell/ForgeData.cs:93 | B | 阵营进度服务端存档 | 初值自选 | 保留 | 同处注释 |
| 123 | Shell/ForgeTab.cs:1183 | A | 抽屉内容按宿主格原尺寸画（`ItemDrawer` prefab 内版式） | 缩放 0.7（给 200×300 的旧格） | 改 1.0 或按 Forge 格 prefab 量 | 同处注释；与 #80/#147 同一族 |
| 124 | Shell/ImportDeckPopup.cs:90 | D | prefab `Localize.mTerm` 原文 | 已用 | 不改 | `MB_6085227748672536722.json` |
| 125 | Shell/InboxWindow.cs:324 | A | 原版 `DOScale(底图).From(localScale×0.5)` | 自选曲线 | 补 `.From(0.5×)` 原版起点 | 同处注释 |
| 126 | Shell/ItemDrawer.cs:312 | B | 原版有真图时不存在占位板 | 中性深灰占位 | 保留 | 同处注释 |
| 127 | Shell/LeaderboardWindow.cs:216 | D | 值 = 原版父 `Content` 序列化框 `248.99,176.01→1671.01,1006.93` | 同值 | 不改 | 同处注释 |
| 128 | Shell/LeaderboardWindow.cs:217 | D | 同上（同一声明续行） | 同上 | 不改 | 同上 |
| 129 | Shell/LiveOpsEventFactory.cs:40 | D | 只决定「解析失败要不要出声」 | `Strict` 额外 `LogError` | 不改（不改返回值） | 同处注释 |
| 130 | Shell/LiveOpsEventWindow.cs:471 | A | 原版模式名 = 官方 `MainMenu/GameMode/{Classic,Skirmish}` | 自造「遭遇 · 12 张」后缀 | 去掉自造后缀，只用官方值 | 官方表现读（该族 3 条） |
| 131 | Shell/LiveOpsEventWindow.cs:492 | A | 同 #130 | 同 #130 | 同 #130 | 同上 |
| 132 | Shell/LiveOpsEventWindow.cs:1074 | D | 格宽 15.96 / 缝 4.3（参考图逐像素 + prefab `Separator` 4 px） | 同值 | 不改 | 同处注释 |
| 133 | Shell/MainMenuRuntime.cs:820 | B | 原版按服务端事件列表**每事件一颗** | 一颗总入口 | 保留 | 同处注释 |
| 134 | Shell/MainMenuRuntime.cs:2371 | B | 哪个模式用 1×1/1×2 是 liveop 服务端数据 | 统一 1×2 | 保留 | 同处注释（参考图 514×830 佐证 1×2 正确） |
| 135 | Shell/MainMenuRuntime.cs:3290 | B | `RankedDivisionV2.TotalSeals` 远端 | 兜底颗数 | 保留 | 同处注释 |
| 136 | Shell/MenuTutorialFlow.cs:57 | B | 两个 stage 无本地 SO ⇒ 无原版 stageID | `#<类名>` 当键 | 保留 | 同处注释 |
| 137 | Shell/MissionRerollPopup.cs:155 | D | prefab 出厂文本 | 原样用 | 不改 | 同处注释 |
| 138 | Shell/MissionsTab.cs:965 | B | `MissionsConfig`（PlayFab Title Data）本地没有 | 格数/币种/数量自选 | 保留 | 同处注释 |
| 139 | Shell/OfferContainer.cs:357 | C | 原版该族无宿主窗口（靠兄弟序） | 显式队列 | 不改（架构） | 同处注释 |
| 140 | Shell/PointerLayer.cs:735 | B | `m_RepeatDelay` 字段名全库 **0 命中**、无 `EventSystem` 导出 | 0.5（UGUI 源码默认） | 保留 | 同处注释 |
| 141 | Shell/PracticeModePopup.cs:222 | D | 原版 `m_CellSize = 180×180` | 180×180 | 不改 | 同处注释（2026-10-05 已订正） |
| 142 | Shell/ProfileTab.cs:247 | B | `DivisionImage` 高度 `0.00`（要运行时 `set_sprite` 后才有 `preferredHeight`） | `DivArtSideRatio 0.72`（实拍反算 0.722/0.688） | 保留 | 同处注释三条判据 |
| 143 | Shell/ProfileTab.cs:773 | B | 当前/历史段位在服务端 | 两卡共用演示段位 | 保留 | 同处注释 |
| 144 | Shell/RaidEventWindow.cs:203 | A | 原版 `Time Remaining` 宽 0（CSF 横向 PreferredSize） | 300 宽右对齐框 | 改「量字宽」 | 同处注释 |
| 145 | Shell/RaidEventWindow.cs:290 | B | 原版取远端 GetLabel | 官方通用词条兜底 | 保留 | 同处注释 |
| 146 | Shell/ReferralPopupWindow.cs:709 | A | 原版 `m_CharacterLimit = 0`（不限长） | 64 | 改 0 | 同处注释 |
| 147 | Shell/RewardWindow.cs:174 | A | 尺寸在抽屉 prefab 里（本地 dump 得出来） | 200×300 | 量 prefab 后改 | 同处注释 |
| 148 | Shell/RewardWindow.cs:178 | A | 同上 | 200×300 | 同上 | 同处注释 |
| 149 | Shell/SaveKeys.cs:303 | B | 原版无本地存档键（玩家数据在服务端） | `_ours_` 前缀 | 保留 | 同处注释 |
| 150 | Shell/SearchingOpponentWindow.cs:99 | B | 原版 `SearchingMatchPopup` 无这条 hint 行 | 700×148 自选框 | 保留 | 同处注释 |
| 151 | Shell/SearchingOpponentWindow.cs:167 | B | `waitLoadTime` 是加载等待，不是展示时长 | 展示 1s | 保留 | 同处注释 |
| 152 | Shell/SettingsWindow.cs:676 | A | 原版该钮 `m_fontSize` 在 prefab（同族两颗 40/38） | 38（沿用近邻） | 读该钮自身 `m_fontSize` 后改 | 同处注释；`assets_full/…/GameObject` |
| 153 | Shell/SettingsWindow.cs:2087 | D | —— | 该行已于 2026-10-24 起不再绘制 | 不改 | 同处注释（F15 ③） |
| 154 | Shell/SettingsWindow.cs:2136 | A | 官方表已在本地（10,862 键） | 自拟中文两列 | 用官方两列（`AutoZoom` 等四条） | 官方表现读 |
| 155 | Shell/ShopData.cs:131 | B | 商品数据服务端；本地唯一 store 属战役高级轨 | 三页商品全自编 | 保留 | 同处注释 |
| 156 | Shell/ShopData.cs:152 | B | 同上（名字/类型/价格/拥有/限购） | 自选 | 保留 | 同处注释 |
| 157 | Shell/ShopTabPage.cs:134 | D | 官方 `MenuCollection/NoDecksFound` EN 是复数 `decks` | 已订正 | 不改 | 官方表现读 |
| 158 | Shell/ShopTabPage.cs:170 | A | 原版商店格走 `ItemDrawer` 内版式（prefab 可量） | 自定主图框/类型带/名字带 | 按抽屉 prefab 内版式改 | 同处注释 + `ItemDrawer.cs` 文件头② |
| 159 | Shell/SkirmishEventWindow.cs:696 | D | 原版 CSF `h:PreferredSize` | 量 TMP `GetPreferredValues().x` | 不改（等价实现） | 同处注释（A1248） |
| 160 | Shell/TitleTab.cs:89 | D | 本页 prefab 无 `mTerm` | 英文原文只作判据、不上屏 | 不改 | 同处注释 |
| 161 | Shell/ToastNotifications.cs:234 | D | 原版 6 份独立 prefab | 一个容器节点 | 不改（结构等价） | 同处注释 |
| 162 | Shell/TransformScalerBySmallScreenUI.cs:58 | D | 原版走玩家存档 | `PlayerPrefs` 键 | 不改 | 同处注释 |
| 163 | Shell/UIBorderGlow.cs:68 | D | 原版只有 `_Reveal` 那条路 | 替代路径**默认关** | 不改 | 同处注释 |
| 164 | Shell/UIBorderGlow.cs:71 | D | 同上 | 替代路径起始 α | 不改（默认关） | 同处注释 |
| 165 | Shell/WindowButton.cs:803 | D | —— | 取不到 shader 保持不变灰（不顶替） | 不改 | 同处注释 |
| 207 | Core/AbilityAnimDurations.cs:114 | B | 表里没有这张卡时原版无数据 | 中位数 1.0 | 保留 | 同处注释 |
| 208 | Core/ArmyIcons.cs:188 | D | `CardArmy → 贴图`（`CardArt.MenuUi` 三级） | 同 | 不改 | 同处注释 |
| 209 | Core/Badges.cs:247 | D | 图集缺图的显式名单（非猜测） | 同 | 不改 | 同处注释 |
| 210 | Core/Badges.cs:684 | D | 兜底阈值 1（比较用） | 同 | 不改 | 同处注释 |
| 211 | Core/CardArt.cs:360 | D | common/空/不认识 兜底（自检已钉） | 同 | 不改 | 同处注释 |
| 212 | Core/CardFeel.cs:61 | D | 原版 `DoPushBack` 调用里硬编码 vibrato 8 | 8 | 不改 | `CardScript__DoPushBack.c` |
| 213 | Core/CardFeel.cs:1940 | D | 与原 clip 逐位同形 | 同 | 不改（⛔ 别改回近似） | 同处注释 |
| 214 | Core/CardFeel.cs:2207 | D | `MeleeAttack` 同一个兜底（按行判） | 同 | 不改 | 同处注释 |
| 215 | Core/CardFeel.cs:3050 | D | 自检诊断字段（哪条路被走） | 同 | 不改 | 同处注释 |
| 216 | Core/CardFeel.cs:3061 | D | 同上 | 同 | 不改 | 同处注释 |
| 217 | Core/CardText.cs:809 | D | 缺键回 `TURN N`（英文） | 同 | 不改 | 同处注释 |
| 218 | Core/CardView.cs:878 | D | 无卡框占位卡面的固定值 | 同 | 不改 | 同处注释 |
| 219 | Core/CardView.cs:1456 | D | 字体资产取不到兜底 | 同 | 不改 | 同处注释 |
| 220 | Core/CardView.cs:5442 | D | `spellTypeKnown` 为真判据，白名单只兜底 | 同 | 不改 | 同处注释 |
| 221 | Core/CardView.cs:6857 | D | 原版正常体 1.12 | 1.12 | 不改 | 同处注释 |
| 222 | Core/CardView.cs:8592 | D | 起点/终点/时长逐一相同 | 同 | 不改 | 同处注释 |
| 223 | Core/CardView.cs:12680 | D | 点阵后端兜底色 | 同 | 不改 | 同处注释 |
| 224 | Core/CardView.cs:12696 | D | 已改用材质原值 `(255,154,88)` | 同 | 不改 | 同处注释 |
| 225 | Core/CardWinBox.cs:87 | D | 原版 `Elements` 下就是 5 个 `EffectElement` | 5 | 不改 | 同处注释 |
| 226 | Core/CardbackFace.cs:99 | D | 退化：贴图自身当 `m_Rect`（padding 0） | 同 | 不改 | 同处注释 |
| 227 | Core/EventTiming.cs:384 | D | `< 0 = 没传` ⇒ 兜底 | 同 | 不改 | 同处注释 |
| 228 | Core/EventTiming.cs:872 | D | 生产路径绝大多数命中阵营档 | 同 | 不改 | 同处注释 |
| 229 | Core/EventTiming.cs:959 | B | 不知道是哪张卡时原版无数据 | 兜底秒数 | 保留 | 同处注释 |
| 230 | Core/EventTiming.cs:1000 | D | 只给旧重载/探针兜底 | 同 | 不改 | 同处注释 |
| 231 | Core/FilterPanelModel.cs:425 | A | 原版 TMP 原文 = **`Stratagems`（复数）**（`MenuDeck/HUD/DeckDescription/Spells`） | `TypeLabels[2] = "Stratagem"` | 改 `"Stratagems"` | 同处注释；官方表键现读 |
| 232 | Core/Loc.cs:1319 | B | 官方表缺键时兜底（Fatigue 两句） | 两句兜底 | 保留 | 同处注释 |
| 233 | Core/Loc.cs:1546 | D | `HasEntry` 兜底写法 | 同 | 不改 | 同处注释 |
| 234 | Core/Loc.cs:2903 | D | 本地兜底切回 `Loc.T` | 同 | 不改 | 同处注释 |
| 235 | Core/Tooltip.cs:1089 | D | 官方表未载入 ⇒ 英文兜底（不猜） | 同 | 不改 | 同处注释 |
| 236 | Core/TraitFrames.cs:745 | D | 出声提示，不是绑定表 | 同 | 不改 | 同处注释 |
| 237 | Core/TraitFrames.cs:767 | D | 值变化兜底 | 同 | 不改 | 同处注释 |
| 238 | Core/TraitFrames.cs:1098 | D | 兜底判定「值没变」 | 同 | 不改 | 同处注释 |
| 239 | Core/TraitFrames.cs:1116 | D | 值变化兜底 | 同 | 不改 | 同处注释 |
| 240 | Core/TraitParticles.cs:92 | D | 反编译两个调用点都硬编码 `FromCode 5` | 5 | 不改 | 同处注释 |
| 241 | Deck/DeckRuntime.cs:8798 | B | 演示卡组（自检/演示用），官方无此键 | `MenuDeck/DemoDeckName` | 保留 | 同处注释；`MenuDeck/HUD/*` 现读无对应键 |
| 242 | Shell/CardDetailPopup.cs:109 | B | 同 #86（服务端 `CardTierConfig`） | 升级表自编 | 保留 | 同 #86 |
| 243 | Shell/DraftModeWindow.cs:2434 | B | `UICardInfoItem` 模板尺寸本地解不出 | 3 张缩略卡布局自选 | 保留 | 同 #111 |
| 244 | Shell/ForgeData.cs:81 | B | 奖励表服务端 | `Palette` 轮换 | 保留 | 同 #121 |
| 245 | Shell/GameWindow.cs:42 | D | 原版 `windowsPlacement` 必填（141 实例全有值） | `UnsetType = -1` 哨兵 | 不改（哨兵不进序列化） | 同处注释 |
| 246 | Shell/LiveOpsManager.cs:67 | B | 原版真值在远端配置里 | 恒 true | 保留 | 同处注释 |
| 252 | Core/Loc.cs:2500 | B | 我们自研 P2P 英文列的括号规则 | `[How to connect]` | 保留 | 同处注释 |
| 257 | Shell/CampaignRewardWindow.cs:189 | D | prefab 字段（`LayoutElement.m_IgnoreLayout = 1`） | `WarnAnchorMin/Max/SizeDX` | 不改（已升级为照原版） | 同处注释（A537） |
| 258 | Shell/CollectionWindow.cs:4679 | D | VA 反汇编：`anchoredPosition = (hiddenPosition.x, originalAnchorPosition.y)` | 照原版 | 不改 | 同处注释（2026-10-05 已升级） |
| 259 | Shell/DraftModeWindow.cs:444 | D | 原版 `Reroll Text` 是 `EverguildTextMeshPro`、无 `Localize`（字面量） | 照原版印英文 | 不改 | 同处注释；官方表 0 命中该串 |
| 260 | Shell/DraftModeWindow.cs:3296 | B | `GameplayVariables` 里没有 ClosedDeck 那一档 | 自选值 | 保留 | 同处注释 |
| 261 | Shell/InboxData.cs:113 | B | 欢迎信文案与奖励在服务端 | 自选 300 点 | 保留 | 同处注释 |
| 262 | Shell/MenuScroll.cs:136 | D | 照抄 UGUI 字段（别自己编） | 同 | 不改 | 同处注释 |
| 263 | Shell/MenuTutorialHighlight.cs:58 | C | 原版靠 Canvas 兄弟序（相对次序 = 判据） | 绝对队列 4040 | 不改（队列梯子） | 同处注释 |
| 264 | Shell/PracticeModePopup.cs:515 | A | 右沿 = 原版 `Deck Name` 的 `DnR`（prefab 可读） | 自选右沿 | 取原版节点 `DnR` | 同处注释 |
| 265 | Shell/PurchasePremiumWindow.cs:138 | A | `Price Display/text` 宽 0（CSF `h:PreferredSize`） | 自定宽 = 框右半 | 改「量字宽」（同 #117） | 同处注释 |
| 266 | Shell/RankedDivisionInfo.cs:161 | D | 官方键 `Ranked/Division/<号>` | 缺键返回空串 + 出声（不编中文） | 不改 | 官方表现读 |

## 四、给动手的人

- **A 档 28 条**可以直接派写手；#80/#81/#147/#148/#123 是同一族（抽屉尺寸），**一次改完**。
- **B 档 62 条不是「不做」**（铁律 11）：只有等原版服务器/远端配置回来才能改；给的反证写在「依据」列，⛔ 别再重新查一遍。
- **C 档 8 条**要动整层（`ImageQuad` 世界空间 quad + 显式渲染队列 vs uGUI Canvas 兄弟序），单独立项。
- **D 档 72 条**：清单生成于 2026-10-26，之后 B141/B117 等波次已把其中多条改回原版；建议**核销这 72 条**，别重复劳动。
