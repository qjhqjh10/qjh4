# P_并行波次 · 文件所有权切片（只读分析 · 2026-10-11 · 第十九会话）

> **只读产出**：本文件之外**未改任何文件**（含两张正本、任何 `.cs`）。未跑 Unity、未动 git、未跑 `typecheck.sh`。
> **判据唯一** = 铁律 13·3「一个文件同一时刻只有一个写手」。
> **输入**：`资料/普查产出_第十九会话/T_待办全量_卡组卡牌战斗.md`（上一轮代理已抽好的表）+ `资料/任务_卡组卡牌战斗.md` 逐行原文。
> **落点定法**：判据里点名的 `.cs`；只给类名的用 Grep 在 `Assets/CardPresentation/`、`Assets/RuleEngine/`、`Unity/工具/` 下定位到**实际文件**。
> **规模**：判为可做 **123 笔** = `✅现在可做` **114** ＋ **9 笔「等写手让路」的（工作区已干净 ⇒ 现在都能做）**。

---

## 〇、先报三条与简报不符的（铁律 5 · 只报不改）

1. 🔴 **简报说的「必须串行那 8 笔」里有 7 笔在表上是 `⛔卡判据或等用户`，不是 `✅现在可做`** —— `A1408` `A1446` `A1452` `A1458` `A1464` `A1465` `A1466`（只有 `A1453` 是 ✅）。
   **但它们的「卡在哪」写的都是「等 X 让路（同一文件一个写手）」** ⇒ 工作区干净后**这 7 笔即解锁**，本表已按可做收进 `L1`。
   同族另查出 2 笔：`A1425`（等 `IconSetup.cs` 的下一个写手）· `A1508`（「三个文件等代理 A 交件」）。
2. 🔴 **`A1053`/`A1066`/`A1024` 三条「判据原文」指针查不到对应小节** —— `资料/待办判据_第六会话.md` 里按 `§A1053` 只定位到 `Shell/InboxWindow.cs` 一处；`第四会话` 那份 §A1024 命中的是**全仓字号换算**（`Core/TmpFont.cs` 等 20+ 个文件）、§A1066 是**全仓命中区普查结果**。**三条都不是「一个落点」** ⇒ 一律进「落点未定」。
3. 🔴 **`A1240` 的 `EverguildTextController` 在工程里【没有同名类】**（`grep "class EverguildTextController"` 0 命中）—— 它只是**原版组件名**，我们对应件未定位；仅在 `Shell/{DailyData,DeckSelectionPopup,GenericOptionsPanel,RewardWindow}.cs` 四处的注释里出现 ⇒ 落点未定。
4. ⚠️ 顺带：**`A1551` 在正本里同时写着「✅已修+已验」和「⛔仍未改」**（`任务_卡组卡牌战斗.md:207`）—— 本表按「**仍开着的那一半**（34 处回调影响面要盘）」处置，归 `L9`。
5. ⚠️ **`A1503` 的「①绿色双箭头」已做完**（正本自述 `1482/1482 全过`）、**④「Ban/空位态」已按铁律 11 例外①判死「不建」** ⇒ 本表只收 **②③⑤⑥** 四笔。

---

## 一、车道表（**车道之间文件零交集 ⇒ 随便挑都不撞车**）

> 「笔数」= A 号个数；「派活单元」= 建议的派工粒度（同文件+同判据的合成一笔）。

### L1 · 引擎 + ruleprobe —— **33 笔 / 31 个派活单元**（最长的一条，必须串行）
**独占文件**：`RuleEngine/Core/{CardDef,EffectText,EffectResolver,RuleCore,Aura,CreatePool,WhenEvent}.cs` · `RuleEngine/Data/CardDatabase.cs` · `RuleEngine/Editor/RuleEngineTest.cs` · `工具/ruleprobe/**`

| # | A号 | 一句话（≤40 字） | 要改的文件 |
|---|---|---|---|
| 1 | `A1466` | `ruleprobe.sh` 拿旧 dll 跑 ⇒ 探针「0 差异」是假绿 | `工具/ruleprobe.sh`（+`工具/ruleprobe/**`） |
| 2 | `A1452`+`A1453` | 信仰 = 阈值不是开销：加收集 + 去 `ps.Faith` 扣除 | `Core/{CardDef,EffectText,EffectResolver,RuleCore}.cs` ＋⚠️`Battle/BattleDriver.cs` |
| 3 | `A1458` | 14 条「无头的持续改费」句没有消费点（12 张卡） | `Core/{Aura,CardDef,RuleCore}.cs` |
| 4 | `A1446` | `TryCostWhenStub` 真缺口在 `AddWhenTrigger` 那半边 | `Core/{EffectText,CardDef}.cs` |
| 5 | `A1465` | `Reanimate a friendly Remnant` 复活全部残骸（≈16 张） | `Core/EffectResolver.cs`（+`EffectText.cs` 动词表） |
| 6 | `A1464` | `ImplementedEffectVerbs` 注释过期（说没登记、其实都在） | `Core/EffectResolver.cs` |
| 7 | `A1408` | `EffectText.cs` 的「那 8 个」是活数字，要改成指针 | `Core/EffectText.cs` |
| 8 | `A1424` | `CreatePool.cs` 记的「英文侧 128 处」复算得 122 | `Core/CreatePool.cs` |
| 9 | `A1470` | `CreatePool.cs` 两处与 `HandledByOtherLayer` 同规则两次落笔 | `Core/CreatePool.cs` |
| 10 | `A1340` | 卡名索引建得太晚 ⇒ `CardDef` 缓存里卡名目标恒 null | `Data/CardDatabase.cs` · `Core/CardDef.cs` · `Editor/RuleEngineTest.cs` |
| 11 | `A1471` | `DA25` 的 `If it survives, heal 3 to it` 永不发生 | `Core/CardDef.cs` |
| 12 | `A1439` | `EffectOp.Source` 在不同采集点不是同一个东西 | `Core/CardDef.cs` |
| 13 | `A1469` | `CollectBareKeywordBody` 门①看不见 `Ecstasy N:` | `Core/CardDef.cs` |
| 14 | `A1438` | `desc`×`keywords` 同头两写、正文不一致 | `Core/CardDef.cs` ＋ 卡表数据（▶见 `L10`） |
| 15 | `A1412` | 收费仍停在逐 op、没抬到 `ability` 层 | `Core/{EffectText,EffectResolver}.cs` |
| 16 | `A1472` | `SOR72` 付费段无冒号 ⇒ 探针结构上看不见 | `Core/EffectText.cs` |
| 17 | `A1428` | 卡面文本的跨句承接解不出来 | `Core/EffectText.cs` |
| 18 | `A1461` | `TAU20` 解析结果错 + 事件层没有 markerlight | `Core/EffectText.cs` · `Core/WhenEvent.cs` |
| 19 | `A1413` | `repeats==0` 收 0 次 / `CostShared` 不查余额 | `Core/EffectResolver.cs` |
| 20 | `A1258` | 「取消攻击」①效果型连空壳都没有 | `Core/RuleCore.cs` |
| 21 | `A1335` | 易伤那一份 + `deathType` 两处子档位 | `Core/RuleCore.cs` |
| 22 | `A1397`+`A1399` | `TriggerOtherCard*` 自排除不一致 / 牺牲那一跳没自排除 | `Core/RuleCore.cs` |
| 23 | `A1279` | `Company Champion` 自链 + 可达面口径要拆两条 | `Editor/RuleEngineTest.cs` · `Core/Aura.cs` |
| 24 | `A1337` | 「没有未实现的词」断言守不住「谎报已实现」 | `Editor/RuleEngineTest.cs` |
| 25 | `A1479` | `RuleEngineTest.cs:12149` 那句「586/586」会误导 | `Editor/RuleEngineTest.cs` |
| 26 | `A1477` | `check` 的 dump 不打 `Target` ⇒ 改目标解析静默过关 | `工具/ruleprobe/Program.cs` |
| 27 | `A1380` | `check` 默认基线路径少两级，不带参即报错 | `工具/ruleprobe/Program.cs` |
| 28 | `A1383` | `filter` 对「没有 `choosecard` op 的卡」零输出 | `工具/ruleprobe/**` |
| 29 | `A1462` | `non <关键词>` 目标筛选疑似整个丢了（未坐实） | `工具/ruleprobe/BattleProbe.cs` → 坐实后回 `Core/` |
| 30 | `A1431` | 全池尺子对「解析过、结算错」结构性失明 | `工具/ruleprobe/{WillRun,Program}.cs` ＋ 工程外 scratch |
| 31 | `A1478` | `ruleprobe.csproj` 注释过期 + 6 文件裸用 `UnityEngine.Debug` | `工具/ruleprobe/ruleprobe.csproj` · `Core/{BattleContext,BoardSlots,CardInstance,GameplayVariables,RuleCore,TutorialScript}.cs` |

⚠️ **第 2 笔要动 `Battle/BattleDriver.cs`（`:7708`/`:8226` 的 `HasFaith` 进门）** ⇒ 那一笔跑的时候 `L2` 必须让路；或把那一小节拆给 `L2` 的队尾。

### L2 · Battle 族（BattleDriver + 战斗断言宿主）—— **13 笔 / 13 个单元**
**独占文件**：`Battle/{BattleDriver,SettingsPanel,WfSlider,WaitBanner,ImageQuad,CombatAutoZoom}.cs` · `Editor/BattleScene.cs` · `Core/{CardFeel,CosmeticPreview}.cs`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1547` | 缺件：按「已打第三张牌」开关的那颗钮没实现 | `Battle/BattleDriver.cs`（判据在 `decomp_full`） |
| 2 | `A1088` | 原版防御池的 `forgeLevel` 闸我们没做 | `Battle/BattleDriver.cs` · `Shell/ForgeData.cs` · 新数据文件 |
| 3 | `A1519` | `ProfileData.AvatarIndex` 是 `public static` 可变字段 | `Shell/ProfileData.cs` · `Battle/BattleDriver.cs` |
| 4 | `A1544` | 2 张头像非正方 ⇒ 立绘宽度不能钉绝对值 | `Battle/ImageQuad.cs` · `Battle/BattleDriver.cs` |
| 5 | `A1171` | 敌方手牌 quad 尺寸只在建视图那一刻算 | `Battle/BattleDriver.cs` |
| 6 | `A1254` | `NameBackground` 抄裸 `m_SizeDelta`、漏乘 0.8 | `Battle/BattleDriver.cs` |
| 7 | `A1303` | 三颗 tooltip 的宿主应是父节点（可射线件并集） | `Battle/BattleDriver.cs` |
| 8 | `A1304` | `WaitText/Dark Shade` 的 pad 语义 + 一条命中路都没接 | `Battle/WaitBanner.cs` · `Battle/BattleDriver.cs` |
| 9 | `A1302` | `HudAbs/HudImage/HudImageTex` 只给高、不做 `PA` 内接 | `Battle/BattleDriver.cs` · `Battle/CombatAutoZoom.cs` · `Editor/BattleScene.cs` |
| 10 | `A1417` | `KeywordSegment.numericKeys` 生产那一行没断言 | `Battle/BattleDriver.cs` · `Editor/BattleScene.cs` |
| 11 | `A1213` | `PopNumber` 一个函数兼两档 + 7 组 TMP 未对 | `Core/CardFeel.cs` · `Battle/SettingsPanel.cs` · `Battle/BattleDriver.cs` |
| 12 | `A1294` | `Skip`/`Resign` 应按 `PA=1` 实绘 300×65.64 | `Battle/SettingsPanel.cs` · `Core/CosmeticPreview.cs` · `Editor/BattleScene.cs` |
| 13 | `A1329` | `WfSlider.HandleHitPx` 是假绿检测器（不过 `ppu`） | `Battle/WfSlider.cs` · `Editor/BattleScene.cs` |

### L3 · Shell 卡组线 + 包装层 + 卡组编辑（最长的一条 UI 车道）—— **22 笔 / 22 个单元**
**独占文件**：`Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` · `Shell/{CollectionWindow,MenuDraw,CollectionData,DeckSelectionPopup,PointerLayer,GenericOptionsPanel,DeckInfoPopup,ShopWindow,ShellRuntime,MissionsTab,ItemDrawer}.cs` · `Battle/SkillPanel.cs` · `Editor/CollectionScene.cs` · `Editor/MainMenuScene.cs`

⚠️ **`Deck/DeckRuntime.cs` 原本会把 `L3` 与「卡组编辑」两条并成一条**（`A1503`/`A1509`/`A1177` 都点它）⇒ 已按「合并」处理，本车道即两者合一。

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1392` | `CollectionScene.cs:3355` 中心走甲式、四沿走乙式 | `Editor/CollectionScene.cs` |
| 2 | `A1132` | `additive`（真「同窗」）没做，判据+做法已在注释 | `Shell/CollectionWindow.cs` |
| 3 | `A1045` | `Share On Chat` 面板要建 + 建成前不许静默 | `Shell/GenericOptionsPanel.cs` · `Shell/DeckInfoPopup.cs` |
| 4 | `A1284` | `SkillPanel.cs:62-64` 逐级折算注释要逐颗回代 | `Battle/SkillPanel.cs` · `Shell/ItemDrawer.cs` |
| 5 | `A1237` | `SkillPanel.Fit` 是死代码 + `Quantity` 缺「抽屉」维 | `Battle/SkillPanel.cs` · `Shell/ItemDrawer.cs` |
| 6 | `A1187` | `ItemDrawer` 两笔：`ClippedText` / `Price Display` 两族 | `Shell/ItemDrawer.cs` |
| 7 | `A1236` | `ShopWindow:741 Available Counter` 被共用闸挡住 | `Shell/ShopWindow.cs` |
| 8 | `A1208` | 两条「该接却接不上」的闸要判（先查 `base>min`） | `Shell/ShopWindow.cs` · `Shell/MissionsTab.cs` |
| 9 | `A1234` | `MissionsTab.Txt/Txt1` 的「先还原折行再裁」次序 | `Shell/MissionsTab.cs` |
| 10 | `A1273` | 两帧不同、宽不可比（`Daily Missions` 有多宽） | `Shell/MissionsTab.cs` |
| 11 | `A1289` | 奖励块偏右 ≈12 设计 px（走 `PreferredWidthPx` 形状） | `Shell/MissionsTab.cs` |
| 12 | `A1207` | 三笔「标称字号与现读不符」+ 三处「不是原版件」 | `Shell/{CollectionWindow,DeckInfoPopup,ShopWindow,ShellRuntime}.cs` |
| 13 | `A1503` | 收藏窗卡组格 ②③⑤⑥（①已做、④判死不建） | `Shell/{MenuDraw,CollectionWindow,CollectionData,DeckSelectionPopup}.cs` · `Deck/DeckRuntime.cs` · `Editor/CollectionScene.cs` |
| 14 | `A1509` | 两窗网格列数不同 / `Empty Collection Warning` 显隐 / 新工具 | `Shell/{CollectionWindow,DeckSelectionPopup}.cs` · `Deck/DeckRuntime.cs` · 新脚本 · 反编译入口文档 |
| 15 | `A1530` | ①`Deck Image` 传了 `keepAspect` ②节点名 `Faction` | `Shell/MenuDraw.cs` · `Editor/MainMenuScene.cs` |
| 16 | `A1536` | `CollectionWindow.Build()` 不可重入 | `Shell/CollectionWindow.cs` · `Shell/PointerLayer.cs` |
| 17 | `A1495` | 补 `UiClickPoolPx` 自检口 + 四条断言 | `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` |
| 18 | `A1532` | `UiQuadActive(key)` 对不存在的 key 恒答 false | `Editor/DeckScene.cs`（+ 同族宿主） |
| 19 | `A1546` | 卡组行底板比原版 `Background` 宽 33.25px | `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` |
| 20 | `A1229` | `hdr_wct` 四格逐值相同、跨文件写了两遍 | `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` |
| 21 | `A1145` | `Editor/DeckScene.cs` 那族「自证结构」没普查干净 | `Editor/DeckScene.cs` |
| 22 | `A1177` | `Img`/`Txt` 的「父件必须在原位」零断言看门 | `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` |

### L4 · 菜单主窗 —— **6 笔 / 6 个单元**
**独占文件**：`Shell/{TopBar,MainMenuRuntime,PracticeModePopup,RankedEventWindow}.cs` · `Core/Loc.cs`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1553` | 顶栏 `Player Name: Message` 印了两遍 | `Shell/TopBar.cs` · `Shell/MainMenuRuntime.cs` |
| 2 | `A1522` | 剩 ①`Feedback Button` ③底板亮 2 倍 ④高亮恒 PLAY ⑤次序反 | `Shell/TopBar.cs` · `Shell/MainMenuRuntime.cs` · `Editor/DeckScene.cs`（排序键） |
| 3 | `A1526` | 「字段表逐字相同、树里却没挂」的新实例 | `Shell/MainMenuRuntime.cs` |
| 4 | `A1233` | 四处「先 `Text` 再自补 `SetAutoFitBox`」要不要收口 | `Shell/MainMenuRuntime.cs` · `Shell/PracticeModePopup.cs` |
| 5 | `A1535` | 同族两扇窗整族偏离（`Practice` 三处都偏） | `Shell/PracticeModePopup.cs` · `Shell/RankedEventWindow.cs` |
| 6 | `A1501` | 练习窗大面积英文（原版中文） | `Core/Loc.cs` · `Shell/PracticeModePopup.cs` |

### L5 · 设置窗 + 命中区 —— **3 笔 / 3 个单元**
**独占文件**：`Shell/SettingsWindow.cs` · `Editor/SettingsScene.cs`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1326` | 外壳侧 FPS 滑块 `HitBand` 没过 pad | `Shell/SettingsWindow.cs` · `Battle/WfSlider.cs`⚠️ · `Editor/SettingsScene.cs` |
| 2 | `A1251` | `Support` 键的 `Badge Highlight` 整块没建 | `Shell/SettingsWindow.cs` · `Editor/SettingsScene.cs` |
| 3 | `A1252` | `Separators` 画法：我们 Simple、原版 Sliced | `Shell/SettingsWindow.cs`（+ 断言宿主） |

⚠️ 第 1 笔要用 `Battle/WfSlider.cs`（**只读转发的形状**，已在 `L2`）⇒ 若需改编，排到 `L2` 之后。

### L6 · `Label` 共用件 —— **3 笔 / 3 个单元**
**独占文件**：`Battle/Label.cs`（共用件 ⇒ 动它要 `grep` 谁在用 + 跑全部受影响的自检）

| 顺 | A号 | 一句话 |
|---|---|---|
| 1 | `A1538` | `Label` 不实现 TMP 的 `m_margin` |
| 2 | `A1235` | `SetCapHeight` + `SetAutoFitBox`：中/英字号比待查 |
| 3 | `A1227` | `SetWrapWidth` 在未激活时会把 `alignment` 改成 `Left` |

### L7 · 事件窗 + 奖励窗 —— **6 笔 / 5 个单元**
**独占文件**：`Shell/{SkirmishEventWindow,EnergySinglePlayerOnlyEventWindow,DailyData,DailyStreakPopup}.cs` · `Editor/RewardsScene.cs`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1221`+`A1248` | 两扇事件窗对同两颗件处置不一致（`Skirmish` 只画半棵） | `Shell/SkirmishEventWindow.cs` |
| 2 | `A1245` | 同一扇窗的第三颗 CSF = `Total Victories` | `Shell/EnergySinglePlayerOnlyEventWindow.cs` |
| 3 | `A1246` | 那两颗 + `Player victories` 四颗的位置来自零宽模板位 | `Shell/EnergySinglePlayerOnlyEventWindow.cs` |
| 4 | `A1288` | 登录卡卡框也画作者态 + 两张卡落位口径不一致 | `Shell/DailyData.cs` · `Editor/RewardsScene.cs` |
| 5 | `A1280` | `DailyStreakPopup` 整族漏三处祖先 `m_LocalScale` | `Shell/DailyStreakPopup.cs` · `Editor/RewardsScene.cs` |

### L8 · 工具 + 构建 —— **7 笔 / 6 个单元**（**与整个 Unity 工程源码零交集**）
**独占文件**：`工具/{gen_icon_plan,gen_diag_runner,menu_dump}.py` · `Editor/{PlayerBuild,IconSetup,SceneStubAudit}.cs` · `Core/PlayerBoot.cs` · `RuleEngine/Editor/RuleEngineTest_Diag.cs` · `ProjectSettings/ProjectSettings.asset`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1483`+`A1484` | 双击 exe 落在白板 + 产品名/公司名还是模板默认值 | `Editor/PlayerBuild.cs` · `Core/PlayerBoot.cs` · `ProjectSettings.asset` |
| 2 | `A1476` | 真因 = 扫描器 `KEYWORD_SCAN` 要求收尾 `:`/`.` | `工具/gen_icon_plan.py` |
| 3 | `A1394` | `RunSafe` 已修；派生件（按 `Run()` 生成）要普查 | `工具/gen_diag_runner.py` · `Editor/RuleEngineTest_Diag.cs` |
| 4 | `A1548` | `menu_dump.py` + `assets_full` 会静默丢掉整支子树 | `工具/menu_dump.py` ＋ 存量结论盘查（文档） |
| 5 | `A1425` | 对账比的是字符不是字节（`ReadAllText` 吃 BOM） | `Editor/IconSetup.cs` |
| 6 | `A1508` | 拆完 8 个后全仓还剩 35 个 `MonoBehaviour` 没同名 `.cs` | `Editor/SceneStubAudit.cs` ＋ 点名的那几个类 |

### L9 · 单件独立（**一条车道内文件互不相干**）—— **7 笔 / 7 个单元**
**独占文件**：`Battle/{MultiCardDisplay,MulliganPanel,CardChoicePanel,ChoosePanel}.cs` · `Shell/MessageToast.cs` · `Core/{CardView,CardText}.cs` · `Shell/{WindowHolder,WindowsManager}.cs`

| 顺 | A号 | 一句话 | 要改的文件 |
|---|---|---|---|
| 1 | `A1054` | 多卡展示窗三条尾巴：字号档/继续位置/小屏缩放档 | `Battle/MultiCardDisplay.cs` |
| 2 | `A1104` | 换牌那两行写死字号，原版是 `autoSizing(18~55)` | `Battle/MulliganPanel.cs` |
| 3 | `A1140` | 那条 toast 通道钉在绝对屏坐标 ⇒ 不走安全区 | `Shell/MessageToast.cs` |
| 4 | `A1141` | 原版另一套 `Toast Notification`（任务/等级/经验）没建 | 新建 Shell 件（`Shell/MessageToast.cs` 旁） |
| 5 | `A1327` | `CardChoicePanel.HitDone` 是同一族第二个过近似 | `Battle/CardChoicePanel.cs` · `Battle/ChoosePanel.cs` |
| 6 | `A1549` | 卡面费用图标两档（`Cost Container` ↔ `Big`）切换条件没查 | `Core/CardView.cs` · `Core/CardText.cs` |
| 7 | `A1551` | `[ExecuteAlways]` 已修；34 处回调影响面要盘 | `Shell/WindowHolder.cs` · `Shell/WindowsManager.cs` |

### L10 · 卡表数据侧 —— **7 笔 / 7 个单元**（同一批 JSON ⇒ 必须串行）
**独占文件**：`Unity/数据/游戏数据/{cards_engine,card_stats}.json` · `数据/卡牌翻译/zh_cards.json` · `i18n/zh_CN.csv` · `工具/gen_cards_engine.py`

| 顺 | A号 | 一句话 |
|---|---|---|
| 1 | `A1445` | `zh_cards.json` 的 `count = 1196` 而 `cards` 实际 1198 |
| 2 | `A1474` | `ASH81` 的 `desc` 整段丢了 `Flying. Sniper.` |
| 3 | `A1403` | `SW36`/`AM41` 的 `desc` 两段正文粘一起、两个前缀都丢 |
| 4 | `A1338` | `DA83` 卡名与 PnP 卡面对不上（多了 `Ravenwing`） |
| 5 | `A1401` | 卡名与 PnP 不符是族账：`GSC64`/`TAU34`/`SW36` |
| 6 | `A1456` | `SOR42` 的 `8 [faith]:` 前缀是我们自己补的 |
| 7 | `A1381` | `ASH65` 的 `payload` 带 `<i>shuriken</i> 2` |

⚠️ **第 2/7 笔要先核 PnP 卡图**（第 7 笔可能顺带要动 `Core/CardText.cs`，那在 `L9` ⇒ 需要就排到 `L9` 队尾）。

### L11 · 徽记 `Badges` —— **3 笔 / 3 个单元**
**独占文件**：`Core/Badges.cs`（只读 `Core/Loc.cs`）

| 顺 | A号 | 一句话 |
|---|---|---|
| 1 | `A1379` | `Badges.KeyOf("stun")` 返回键名本身，会套 `<link=stun>` |
| 2 | `A1362` | `CardTrait` 两个序列化开关没人查过（getter 已反编译） |
| 3 | `A1430` | 只写在 `keywords` 里的那 8 张 `Sniper` 不在计划表射程 |

### L12 · 文档订正（**零 `.cs`**）—— **2 笔 / 2 个单元**

| 顺 | A号 | 一句话 |
|---|---|---|
| 1 | `A1214` | `R1` 那两列已有 3 处过期，要就地订正 |
| 2 | `A1328` | 命中区普查的范围缺口要写进普查方法 |

---

## 二、可并行的波次编排

**规则**：车道之间**文件零交集**；每条车道**内部串行**（一个写手从头做到尾）。⇒ **每波 = 从还没跑完的车道里挑 ≤5 条派出去**，任挑任配都不撞车。

| 波 | 含哪几条 A号（= 每车道取一笔） | 这一波碰的文件（去重后） | 与别波是否无交集 | 最多几个写手 |
|---|---|---|---|---|
| **波 1** | `L1:A1466` · `L2:A1547` · `L3:A1392` · `L4:A1553` · `L5:A1326` | `工具/ruleprobe.sh`+`ruleprobe/**` · `Battle/BattleDriver.cs` · `Editor/CollectionScene.cs` · `Shell/{TopBar,MainMenuRuntime}.cs` · `Shell/SettingsWindow.cs`+`Battle/WfSlider.cs`+`Editor/SettingsScene.cs` | ✅ 波内零交集 | **5** |
| **波 2** | `L6:A1538` · `L7:A1221+A1248` · `L8:A1483+A1484` · `L9:A1054` · `L10:A1445` | `Battle/Label.cs` · `Shell/SkirmishEventWindow.cs` · `Editor/PlayerBuild.cs`+`Core/PlayerBoot.cs`+`ProjectSettings.asset` · `Battle/MultiCardDisplay.cs` · `数据/卡牌翻译/zh_cards.json` | ✅（`Label.cs` 与波 1 无交集） | **5** |
| **波 3** | `L11:A1379` · `L12:A1214` · `L1:A1452+A1453` · `L2:A1088` · `L3:A1132` | `Core/Badges.cs` · 纯文档 · `RuleEngine/Core/{CardDef,EffectText,EffectResolver,RuleCore}.cs`+`Battle/BattleDriver.cs` · `Battle/BattleDriver.cs`+`Shell/ForgeData.cs` · `Shell/CollectionWindow.cs` | ⚠️ `L1:A1452` 与 `L2:A1088` **都碰 `Battle/BattleDriver.cs`** ⇒ **本波只能派 4 个写手**（把 `L2:A1088` 推到波 4，或把 `L1` 那一小节拆给 `L2` 队尾） | **4** |
| **波 4** | `L2:A1088` · `L3:A1045` · `L4:A1522` · `L5:A1251` · `L6:A1235` | `Battle/BattleDriver.cs`+`Shell/ForgeData.cs` · `Shell/{GenericOptionsPanel,DeckInfoPopup}.cs` · `Shell/{TopBar,MainMenuRuntime}.cs`+`Editor/DeckScene.cs` · `Shell/SettingsWindow.cs`+`Editor/SettingsScene.cs` · `Battle/Label.cs` | ✅ | **5** |
| **波 5+** | 按各车道表**顺序往下取下一笔**，每波 ≤5 条；一条车道跑完就换下一条 | —— | ✅ | **5** |

**效率提示（按车道长度排的耗时预估）**：`L1`（31 单元）与 `L3`（22 单元）**是最长的两条** ⇒ **这两条车道从波 1 就要开跑、中间不换人**；`L5`/`L6`/`L11`/`L12` 是短车道，用来填空位。

---

## 三、落点未定的（**11 笔 · 铁律 2 如实标，⛔ 不编文件名**）

| A号 | 为什么定不出 |
|---|---|
| `A1024` | 字体口径 = **全仓字号换算**（`Core/TmpFont.cs` 及 20+ 处调用），判据文档命中的是「口径」不是「落点」 |
| `A1053` | 命中区通用族（**「父件整矩形 vs `m_TargetGraphic`」**），判据 §A1053 只点到 `Shell/InboxWindow.cs` 一处，其余站未列 |
| `A1066` | 全仓命中区普查**结果**表，未给逐处落点 |
| `A1212` | 剩的一半 = **66 处的断言**，`RA1212_切块表.md` 是 **13 块 / 30+ 文件** ⇒ 本身要先再切块 |
| `A1226` | 66 处对应**原版件 `m_SizeDelta`**，逐处表在 `RM_原版TMP框尺寸.md`（44 吻合 / 14 差 ≥10px / 8 不可比）⇒ 跨多文件 |
| `A1240` | `EverguildTextController` **工程里没有同名类**（只出现在 4 个 Shell 文件的注释里）⇒ 我们对应件未定位 |
| `A1243` | 「凡 `wrapPx` = 整格钮宽」的**全库普查还没做**（判据要求先 `grep` 再逐处判）⇒ 先出普查表 |
| `A1272` | `count` 的**内容相关宽**；本条只写了「原版取 `preferredWidth`」，**没写在哪扇窗的哪一段**（候选 `Shell/MissionsTab.cs`，未坐实） |
| `A1422` | 徽记「两侧都没画」≈40 个词名的**盲区**，要逐词亲读卡图后再按词立账 ⇒ 落点取决于逐词结论 |
| `A1437` | 先查证「原版哪些卡真有 ability」⇒ **改哪里取决于结论** |
| `A1455` | 先核「`Sister Novitiate` 是没解出来还是名字对不上」⇒ 数据侧 / 解析侧二选一 |

---

## 四、本批**不建议派**进并行波次的

| A号 | 理由 |
|---|---|
| `A1230` · `A1232` · `A1239` · `A1282` · `A1228` · `A1312` · `A1155` · `A985⑥` · `A179⑤` · `A1382` · `A1395` · `A1426` · `A1468` · `A1406` · `A1475` · `A1350` · `A1480` · `A1482` · `A1511` · `A1537` · `A1523` · `A1539` · `A1545` | **表上就是 `⛔卡判据或等用户`**，且卡在哪写的**不是「等写手」**（判据在远端 / 要用户拍板 / 要先查证）⇒ 不构成本任务的分片对象 |
| `A1489` · `A1491` · `A1492` · `A1493` · `A1494` · `A1498` · `A1500` · `A1502` · `A1506` · `A1513` · `A1520` · `A1525` · `A1529` · `A1534`? · `A1540` · `A1541` · `A1542` · `A1543` · `A1544`? · `A1550` · `A1552` · `A1217` · `A1262` · `A1281` · `A1244` · `A1247` · `A1291` · `A1377` · `A1345` · `A1481` · `A1485` · `A1487` · `A1549`? | **表上标 `⚠️要真机`（共 31 条）** ⇒ 按简报要求**不排进并行波次**（需真机实拍/实点，批处理里验不了） |
| `A1212` · `A1226` | 「可做」但**是跨 13 块 / 30+ 文件的一整族**，本身要先再切块 ⇒ 建议**先把切块表落实成一张派工表**，再按块派 |
| `A1483`+`A1484` 之外，任何**与「已跑过 L1/L3 的车道」同文件**的笔 | 同文件同期只能一个写手 ⇒ 等那条车道当前那一笔交件 |

> ⚠️ 上表两处带 `?` 的（`A1534` · `A1544` · `A1549`）是**我在表里查不到对应行号**的编号，**未核**（本任务红线只读这两份文件）；`A1544`/`A1549` 已按可做收进 `L2`/`L9`。

---

## 五、四个数

- **判为可做的条数**：**123**（`✅现在可做` 114 ＋ 9 笔「等写手让路」现已解锁）
- **真正定出落点的条数**：**112**（= 123 − 11 落点未定）
- **波次总数**：**12 条车道**；**可行波次 ≥ 23 波**（按最长车道 `L1` 的 31 个派活单元计上限，每波 ≤5 条车道）
- **热点队列长度**：**最长一条 = `L1` 引擎+ruleprobe 31 个派活单元 / 33 笔**；`L3` 次之 22 笔；其余 10 条合计 35 笔（其中 `L2` 13 笔）
