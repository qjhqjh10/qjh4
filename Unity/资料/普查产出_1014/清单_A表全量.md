# A 表全量待办清单（只读普查 · 2026-10-14）

> **口径**：本文件是「还开着的 A 表账」的一张**可排活清单**，**不是正本**。
> 正本 = `d:/4/项目任务.md` §三 第 29 条（A/F/G/H 四段）· 判据全文 = `资料/普查产出_1013/批次计划_1013.md` §六·B / `资料/待办判据_*.md`。
> ⛔ **状态列会过期** —— 本表**没有**逐条回代码现核（那是下一轮的事）；只做了「能一眼看出矛盾」的交叉核对，结果全在 §二。
> 🔴 **动手前一律现读**（铁律 5 / 5·b：表上行号会漂、状态列会过期）。
> 已收口的编号集合（⛔ 别当待办）= `资料/历史/A表已收口_{1007,1008,1011,1012,1013}.md`。

---

## 总表

### A 段（§三 第 29 条 · 阶段二 / 界面 15 条）

| A 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **A262** | 只剩三块：M-a（组 1 改方案 + 组 6 合并）· M-b（31 处 `.cs` 注释 / 15 文件）· 订正 S1 报告「4 处」→ 31 | 代码·注释 + 文档 | 15 个 `.cs`（未点名）· `资料/普查产出_1011/S1_下一批切块普查.md:41` | §A262 · `普查产出_1013/A表现核_块5.md` | M-b **排在 `.cs` 写手之后** |
| **A268** | ① `MenuDraw.ClipTmpMesh` 的 `i ≥ 1` 支路走不走得到（**要 Unity 实测钉死**）② `MenuScroll.SetOffset` 页签关着也整页重建 | Unity腿 + 只读 | `Shell/MenuDraw.cs` · `Shell/MenuScroll.cs` · `Editor/CollectionScene.cs` | §A268 · `A表现核_块5.md` | ① 若成立 ⇒ `Editor/CollectionScene.cs` 的 `mi != 0` 过滤会**静默漏图标字形** |
| **A338** | 全仓「指向我们自己 `.cs` + 行号」的引用 **411 处**要更新 | 代码 + 文档 | 全仓（**六份自检宿主全在内**） | §A338 · `待办判据_阶段二与联机.md` | ⛔ **别拆给多写手**；独占窗口、调度上最贵 |
| **A348** | `Shell/ChatPanel.cs:40` 的 `QBase` 仍是 `public`（外部**代码**引用 0） | 代码 | `Shell/ChatPanel.cs` | §A348 · `A表现核_块3.md` | 可并进 A252（A252 **已收口**） |
| **A367** | ⛔ **挂起（真 Play）**：根特效归哪台相机；`CardFeel.cs:960` 喂世界坐标才是要改的一侧 | 挂起 | `Battle/CardFeel.cs` · `Editor/BattleScene.cs`（旧引点 `:5171` 已漂） | §A367 · `A表现核_块3.md` | 静态链已闭合 ⇒ 只能真 Play |
| **A380** | ⏭ 只剩 ④：桌面另两张实拍（`奖励—活动` / `奖励—锻造厂`）逐张对账 | 只读普查 | 桌面实拍 + 奖励窗 | §A380 · `普查产出_1011/R1…` §六 | **先派只读普查、再切写手** |
| **A383** | 排位窗在原版是哪个模式号（「找对手」那半**不成立**）—— 要**按 VA 读指令流** | 只读（反编译） | — | §A383 · `A表现核_块3.md` | ⚠️ 别与 `MatchType`（10/170）混 |
| **A394** | 场景侧 `AnimFXController` 的 **`modules` 层**：模块基类 + 相机震动 + 触发点 | 代码（大件） | 场景侧 AnimFX（震动已有：`WFModuleScreenShake.cs` / `CardFeel.cs:707` / `BattleDriver.cs:1691`） | §A394 · `普查产出_1012/H2_场景侧AnimFX.md` §四 | 大件·独占窗口；那颗模块**很可能一次都不播** |
| **A418** | Unity 腿三步：`gen_unity_arena_manifest.py --arena battlearena2` → `gen_env_blendables.py --check` → `ArenaBuilder.BuildArenaPrefabs` | Unity腿 | `工具/scripts快照/…` · `ArenaBuilder.cs` · 连带断言 | §A418 · `普查产出_1012/H6…` | **必须 `cd` 进仓库那个目录**；与 A345-T-b 同族·串行；另 `battlearenaleviathan` 的 `Embers` 同缺陷 |
| **A425** | 素材三步：`RewardAppearParticle` 重打进 `wf_menus_extra.bundle` · 导 prefab 到 `CardPresentation/Effects/` · 重建效果库 | Unity腿 + 素材 | `工具/extract_missing_shaders.py`（`BOOSTER_GROUPS[0].roots`）· `CardPresentation/Effects/` · `EffectExporter.RunListed` · `EffectLibraryBuilder.Run` | §A425 · `A表现核_块6.md` | ⚠️ **实测：`CardPresentation/Effects/` 里没有 `RewardAppearParticle`**（只有 `WarpforgeVFX/Prefabs/` 一份）⇒ 疑似未做，动手前现核 |
| **A451** | X1（`Battle/` 69 处）+ X2 + X4 —— XML doc 转义（机械活） | 代码（机械） | `Battle/` 等（**撞 `BattleDriver.cs`**） | §A451 · `A表现核_块5.md` | 量尺 = `WF_DOC=1 bash 工具/typecheck.sh`（⛔ 不跑 Unity）；每路独立 `TMPDIR`；X1 **排在输入那批之后** |
| **A464** | ⏭ 只剩 **B2b**（软边削 `colors32.a`）；B1/B2/B2′/B3/B4/B5 已收 | 代码 | 见 §A464（与 A489 **同一行代码**） | §A464 · `A表现核_块4.md` | 迁移时与 A489 一起看 |
| **A469** | ⏭ 只剩「生产接线」：`CampaignTab.BuildContext` → 真数据领到（走 UM1 路子） | 代码 + 断言 | `Editor/RewardsScene.cs` | §A469 · `WC1_战役奖励窗.md` | **须独占 `Editor/RewardsScene.cs`**（该文件「只加不改」） |
| **A492** | `SetAutoFitBox` 排在 `SetCharSpacing` 之前 ⇒ 自适应收敛**不含字距** | 代码 | `Battle/Label.cs`（+ 调用点） | §A492 · `普查产出_1013/WLabel_余下静默口.md` | ⚠️ 判据半齐 ⇒ 改完**可能没有断言咬得住**；与 **A545 / A596 / A536** 同口，要协调 |
| **A512** | 两处「按 `popUpWindow` 清场」要换成 `CloseModalPopups()` | 代码 | `Editor/CollectionScene.cs:2488` · `Editor/RewardsScene.cs:4424` · `Editor/MainMenuScene.cs`（提可见性） | §A512 · `A表现核_块2.md` · `WA505…` | 前置：`CloseModalPopups()` 是 `MainMenuScene` 的 **private static**；⛔ 别抄第二份；风险已下调（一致性、非修红） |

### F 段（各线并入的账 16 条）

| A 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **A163** | ⛔ **挂起（判据在远端）**：SM 卡的「容器预制体」是哪一份（上半已查实） | 挂起 | — | `待办判据_1006.md` **§A163**（⚠️ `_1008.md:20` 只有引用行） | 远端 |
| **A179** | 拆三块：**⑤ 一句措辞**（写手）· **①④ 进真 Play 清单** · **②⑥⑧ 待裁** | 混合 | 一句措辞（未点名） | `待办判据_1008.md` §A179 · `A表现核_块3.md` | 三块分开处置 |
| **A186 + A376**（并成一条） | ⛔ **挂起（等 `TitleData`）**：只剩登录卡那半（格数）；骷髅那半已由 A370 定死 | 挂起 | `Shell/DailyData.cs:81`（已定死那半） | `待办判据_1007.md` §A186 / §A376 | 同一份 `MissionsConfig` |
| **A231③**（= A522 = A564） | 多 CAB 产物**在 Unity 里真加载过一次 = 没做**（现全为 UnityPy 格式级证据） | Unity腿（验证） | `out/final_arena1.bundle` → `LoadFromFile` → `GetAllAssetNames()` | §A231 · `普查产出_1013/W230_跨CAB与repack.md` | ⛔ **不许销账**；`m_Container` 侧**无先例**（13448 条容器项 `m_FileID` 全 0）⇒ 新形态 |
| **A285 / A385** | 导入设置族（`ArtBaker.ApplyImportSettings`） | Unity腿 | `ArtBaker.cs:126` | §A285/385 · `A表现核_块6.md` | ✅ **实测判据已达成**：`ui_menu/*.png.meta` 里 `nPOTScale: 1` = **0**（判据是 36 → 0）⇒ 可销，见 §二·5 |
| **A494** | `MENU_IMAGES` 6 张（`Laser_Wave_2` / `LightningTrail` / `Noise_Combined` / `Shine_trail` 等） | Unity腿 | `工具/import_original_art.py` | §A285/385 · `A表现核_块6.md` | ✅ **实测四张都在 `Resources/Art/ui_menu/`** ⇒ 可销，见 §二·5 |
| **A543** | 4 张币种小图标（crystal / blackstone / ticket / energy） | Unity腿 | 同上（`MENU_IMAGES`） | §A543 · `W374…` | ✅ **实测四张全在 `ui_menu/`** ⇒ 可销，见 §二·5 |
| **A582** | 5 张箱图 `_open`（+ 纪律：**导入器必须先跑**再跑 `RewardsScene.Run`） | Unity腿 | `工具/import_original_art.py`（W-M1 加的 5 条 `:546-550`） | §A582 · `A表现核_块6.md` | ✅ **实测 5 张 `40k_Crate_Tier{1..5}_*_open` 全在** ⇒ 可销，见 §二·5 |
| **A629** | 🔴 **缺 2 张**：`40k_shop_popup_info_bg` · `40k_OfferBadge` | Unity腿 | `工具/import_original_art.py`（`MENU_IMAGES`） | §A629 · `W-L1` 交件 | ⚠️ **实测两张仍只在 `Assets/CardPresentation/Art/原版/0_mainmenu/`，没进 `Resources/`** ⇒ 仍开着 |
| **A345（T-b）** | 🔴 **硬同步点**：建场侧 + 断言（判据 = 13 场各一行「**没对上 0 条**」） | Unity腿 | `ArenaBuilder.BuildArenaPrefabs` · `Editor/BattleScene.cs` | §A345 · `WA345Ta_战场全树生成侧.md` §八 | ⛔ **在此之前别的线不许插进来跑 `ArenaBuilder`**；跑完**必须跟一次 `BattleScene.BuildAndSaveScene`**；一条断言会「**正确地红**」（A592） |
| **A434** | ⚖️ **已裁**：显式不裁的阶段 1 不动、阶段 2 迁移时若真出现用例再定 | 挂起（已裁） | — | §A434 · `A表现核_块6.md` | 阶段 2（A435）**已做完** ⇒ 建议复核「是否要据此重议」 |
| **A556** | 🔴 排活约束：`Editor/BattleScene.cs` = 战斗侧**所有断言的唯一宿主** ⇒ **必须串行** | 约束 | `Editor/BattleScene.cs` | `批次计划_1013.md` §六·B A556 | 都排在 **A514 段之后** |
| **A591** | T-b 尾巴：13 场重建时每场「没对上 1 条 `BoardCamera`」**是既有的**（9/19 旧日志里就有）⇒ 当预期红处置 | Unity腿 + 记录 | `ArenaBuilder.cs` · 建场日志 | §A591 | 与 A345-T-b 同一行 |
| **A628** | 🔴 排活约束：`Shell/WindowsManager.cs` 注册口 + `Editor/ShopScene.cs` 在 A251 那批里**必须串行** | 约束 | `Shell/WindowsManager.cs` · `Editor/ShopScene.cs` | `批次计划_1013.md` §六·B A628 | 一块一个写手 |

### G 段（§三 第 29 条 · 2026-10-13 第四轮新账 A516–A676 · 表上列出的 76 个）

| A 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **A521** | `Shell/DailyData.cs:815` 注释标「⚠️ 我们挑的」不准（原版 prefab 文本就是 `'Current streak:'`） | 代码·注释 | `Shell/DailyData.cs` | §六·B A521 | 无 |
| **A523** | `工具/extract_missing_shaders.py` 里**两个重打包口并存**是债 ⇒ 要定「谁是唯一口」 | 代码（工具） | `工具/extract_missing_shaders.py` | §六·B A523 | 与 A230 / A801 / A802 同族 |
| **A527** | `Shell/ChatPanel.cs:636` 注释写「四个调用点」实为 **3** | 代码·注释 | `Shell/ChatPanel.cs` | §六·B A527 | 无 |
| **A533** | `Editor/DeckScene.cs` 约 `:3073` 一条**弱断言**（三条件命一即可） | 断言 | `Editor/DeckScene.cs` | §六·B A533 | 弱断言族；⚠️ **G 段列了它两次** |
| **A535** | `SaveAndSay()` 成功支的 `HideDeckPopUp()` **失去断言覆盖** ⇒ 要不要加 `UiPressDone()` 直调口 | **待裁** | `Editor/DeckScene.cs` | §六·B A535 | 留调度台裁 |
| **A536** | `Label.SetAutoFitBox` 在 `basePx == nomPx` 时**写不进** `m_fontSizeBase` 的既有洞 | 代码 | `Battle/Label.cs` | §六·B A536（源 A407） | 与 A492 / A545 / A597 同口 |
| **A544** | `Shell/ShopData.cs:188-189` 商品名 `"Blackstone Bundle"` **发的是水晶图标** | 代码 + 查证 | `Shell/ShopData.cs` | §六·B A544 | 要查原版那两个包各自发什么 |
| **A549** | ① `Shell/CollectionWindow.cs:2694-2701` 日志打「新建卡组「」」② `Shell/LiveOpsEventWindow.cs:962-975` 良性 | 代码·注释 | `Shell/CollectionWindow.cs` · `Shell/LiveOpsEventWindow.cs` | §六·B A549 | ②半属「如实标注」 |
| **A565** | Discard 后**不做内存回滚**（修法 2 行）—— 会**推翻 A364⑥⑦ / A399③ 的前提** ⇒ 必须同改夹具 | 代码 + 夹具 | `Deck/DeckRuntime.cs`（`LoadDeck(Library.Current); RefreshAll();`）· `Editor/DeckScene.cs` 夹具 | §六·B A565 | ⛔ 不是「不做」；夹具要同改 |
| **A566** | ⚖️ **待裁**：「写回哪一副」没钉死（原版写回开窗那一副、我们写回 `_current`） | **待裁** | `Deck/DeckRuntime.cs`（`CommitDeck` 守卫）· `DeckLibrary`（白名单外） | §六·B A566 | 请裁是否立账 |
| **A602** | `DeckRuntime.TryImport` 的 `false` 有**三种**成因、`ImportError` 只覆盖两种 | 代码 | `Deck/DeckRuntime.cs` | §六·B A602 | 与 A503 / A547 同族 |
| **A603** | `DeckLibrary.Load()` **每次 `new`、无缓存** ⇒ ⚠️ 谁改成单例会**打脸两条断言** | 记录·⚠️ | `RuleEngine/Data/DeckLibrary.cs:41-52` | §六·B A603 | 记录（别改） |
| **A604** | `DeckStore.SaveAll` 是「写 `.tmp` 再 `Move`」—— **不是缺陷**，⛔ **别去「修」** | 记录·别动 | `Deck/DeckStore.cs` | §六·B A604 | 别动 |
| **A612** | `Shell/LiveOpsEventWindow.cs:967` 的 `idx` **可能是 −1**（`CreateDeck` 失败 ⇒ `IndexOf("")`） | 代码 | `Shell/LiveOpsEventWindow.cs` | §六·B A612 | 归 A600 的 4 个未跟调用点；⛔ 别只抄 `if (!Select(idx))` |
| **A525** | 🔴 `Shell/ChatPanel.cs:698` 的 `Message` **既不左也不右 ⇒ 居中**（注释写着 Left/Top）⇒ 改法一行 | 代码 | `Shell/ChatPanel.cs` | §六·B A525 | 与 A403 同族 |
| **A526** | `ChatPanel` 输入框占位原版 `折行=3`（`PreserveWhitespaceNoWrap`）、我们恒落 `Normal(1)` | 代码 | `Shell/ChatPanel.cs` | §六·B A526 | 只有输入框那一族这一格不对 |
| **A530** | A410 的**联机 / 重连两档措辞本件验不了** ⇒ 另派（宿主 `Editor/NetBattleTest.cs`） | 代码 + 断言 | `Editor/NetBattleTest.cs` | §六·B A530 | ⛔ 不是「不做」（铁律 11） |
| **A596** | 🔴 `Battle/Label.cs` 约 `:524-533` 的 `SetCharSpacing` 是**第 9 个出声口**、走自己的 `Debug.Log` 且**不去重** | 代码 | `Battle/Label.cs` | §六·B A596 | 收编会改消息 + 改刷屏行为 |
| **A597** | `Label.SetAutoFitBox` 内部**两条静默 `return`**（入参不成立）；没查过有没有 `minPx = 0` 的调用点 | 代码 | `Battle/Label.cs` | §六·B A597 | 半齐 |
| **A598** | 出声助手 **`NoteDotAlign` 已是历史名**（管 8 个口、只有 3 个是对齐）；没改名 | 代码 + 文档 | `Battle/Label.cs` · `Editor/SettingsScene.cs:246` | §六·B A598 | 改名要牵动 SettingsScene |
| **A608** | 「`Resources.Load` 失败 Unity 会不会**负缓存**」**没判据** | 只读 / 挂起 | — | §六·B A608 | 半齐·缺判据（`decomp_full` 里没有直接判据） |
| **A613** | ⚖️ **待裁**：**写盘失败要不要拦下来**（现在一律「只出声」） | **待裁** | 一批写盘口（`CollectionData` / `DeckRuntime` / `DeckInfoPopup`…） | §六·B A613 · §九·5 需裁表 | 改玩家可见流程 ⇒ 用户/调度台裁 |
| **A650** | 原版唯一的 `TouchPressed` 消费点（`EnemyInfoTouch` 松手收敌情小窗）**我们没对应件** | 代码 / 待定 | 敌情小窗（未点名） | §六·B A650 | 半齐（**没查我们有没有敌情小窗**） |
| **A531** | 🔴 `A513` 只钉了 `ChatButton` 一件 —— 同族 **0.1px 取整字面量**在 `BuildHudExtras` 里**还有一批** | 只读 → 代码 | `Battle/BattleDriver.cs`（`BuildHudExtras`） | §六·B A531 | 半齐（**要逐件取原版父链**） |
| **A644** | ⚠️ 量法陷阱：**九宫的根没有 `ImageQuad`** ⇒ 量整块只能用 `RectOfUnion` | 断言 / 文档 | 各量法调用点（`Wpx`/`CheckRectPx`/`CheckArt`） | §六·B A644 | 给后面写断言的人；`Editor/*.cs` 里 `!RectOfUnion` 式断言只有 1 处 |
| **A648** | WSmall3 那两条断言**鉴别力下降但仍成立** ⇒ ⛔ 别当回归网 | 记录 | `Editor/CollectionScene.cs` 约 `:5426`/`:5436` | §六·B A648 | 记录 |
| **A666** | `Editor/MainMenuScene.cs` 里 **5 处「手抄同一式」**、不走 `RenderedRect` ⇒ grep 找不到 | 代码 | `Editor/MainMenuScene.cs` | §六·B A666 | ⚠️ **已被 W-E1c 做完**（`A表已收口_1013.md` §四）⇒ **表上还留着 = 过期**，见 §二·4 |
| **A667** | `Editor/MainMenuScene.cs:2000` 期望值**引用了实现常量** `PracticeModePopup.DlContentB` | 代码 | `Editor/MainMenuScene.cs` | §六·B A667 | ⚠️ **已被 W-E1c 做完** ⇒ **表上还留着 = 过期**，见 §二·4 |
| **A534** | 🔴 通则候选：「弹出模态窗的那一节**收尾必须显式收窗**」—— A415/A399 写了、**A330 漏了** | 代码 + 纪律 | 写夹具的各 `Editor/*.cs` | §六·B A534 | 建议与三条必查行并列 |
| **A552** | ⚖️ `AnimFXController` **缺 `[DisallowMultipleComponent]`** ⇒ 补守卫或断言 | **待裁** | `AnimFXController.cs` | §六·B A552 | 待裁 |
| **A570** | `Editor/RewardsScene.cs` 约 `:5133` 的**注释与代码相反**（`_ctx` 被清成 null） | 代码·注释 | `Editor/RewardsScene.cs` | §六·B A570 | ⛔ 别改成无参 `TryOpen()`、也别 `OpenWindow(cw, ctx0)` |
| **A573** | `ReopenFromBackground()` 已成**死方法**（自注「留着当形状存档」） | 记录 | `Shell/WindowsManager.cs` | §六·B A573 | 记录 |
| **A615** | `Editor/RewardsScene.cs` 约 `:8842` 还写「待接线」—— **已接完** | 代码·注释 | `Editor/RewardsScene.cs` | §六·B A615 | 归 `Editor/RewardsScene.cs` 的写手 |
| **A633** | `Shell/WindowsManager.cs` 约 `:1082-1087` 那句「`MainMenuRuntime._openByRef` 第二份**要等调度台合并**」 | 代码·注释 | `Shell/WindowsManager.cs` | §六·B A633 | A251 批的**独占口** |
| **A671** | 🔴🔴 `Editor/RewardsScene.cs` §三·(a) 的夹具**今天就在静默假绿** —— 两条 ★ 断言**一条都不跑** | 断言 / 夹具 | `Editor/RewardsScene.cs`（夹具侧一处 `if` 换两行） | §六·B A671 · `WA569_跨窗重建普查.md` | 判据齐·**但没派出去**；⛔ **别动 `RefreshOpenDailyWindows`**；按**锚点**定位（行号漂过 3 次） |
| **A672** | `WindowType` 的字段默认值 = `Fullscreen`、**没有哨兵**（与 `placement` 的 `UnsetPlacement` 不对称） | 代码 | `Shell/WindowsManager.cs:87` | §六·B A672 | 今天 31 个 `Create()` 全有值，但新窗忘赋 ⇒ **静默**走全屏支、无断言会红 |
| **A673** | 🔴 `Editor/ShellScene.cs` 的 `logBtn`：`lw1.Close()` 之后**直接** `onClick()`、**连 null 守卫都没有** | 断言 / 夹具 | `Editor/ShellScene.cs` | §六·B A673 | 半齐（「旧委托体碰到已销毁对象会怎样」没读） |
| **A675** | ⚖️ **待裁**：「窗内子树重建」那一族（`.Build()`/`.Refresh()`/`.RebuildForTest()`）**要不要单独普查** | **待裁** | — | §六·B A675 | 与「跨窗重建」**不是同一条链** |
| **A554** | A431 的 `wantNodes{3,4,1}` 与 A514 新的**存在性检查耦合** ⇒ 哪天真撞上，期望值要一起改 | 断言 | `Editor/BattleScene.cs` | §六·B A554 | 与 A514 耦合（同 `Embers` 先例） |
| **A561** | 同族**第 3 处「取第一份 `SerializedFile`」**：`工具/extract_mirror_shaders.py:84` + `:112` 的 `next(...)` 无 `default` ⇒ 多 CAB 壳包会抛 `StopIteration` | 代码（工具） | `工具/extract_mirror_shaders.py` | §六·B A561 · §九·4 | 今天安全（壳单 CAB） |
| **A651** | `Battle/BattleDriver.cs` 的 `PointerHeld()` 与 `PointerDown()` **函数体逐字相同**（名字有歧义） | 代码 | `Battle/BattleDriver.cs`（调用点约 `:5164`/`:5172`） | §六·B A651 | 改名牵连面极小 |
| **A652** | `CenterCameraButton` 的 `m_OnClick` 挂着一条**永不触发的野接线**（`m_Target.PathID = 0`） | 记录 | 原版 prefab（**无我们代码**） | §六·B A652 | 不影响行为，如实记 |
| **A659** | 🔴 少了「**指针在屏幕外 ⇒ 归零**」的守卫 ⇒ `WorldPointer()` 会**外推**到界外点 | 代码 | `Core/LayoutSpace.cs:93-100` · `Battle/BattleDriver.cs` | §六·B A659 | 要动 `BattleDriver` / `LayoutSpace` |
| **A660** | `_CloseBattleDoors` 那条「**结算门动画期间冻住输入**」我们没有 | 代码 | `Battle/BattleDriver.cs` | §六·B A660 | 属 `TouchInputManager.Toggle` 语义族 |
| **A630** | ⚖️ **待裁**：`BoosterInfoPopup` 与 `BaseOfferPopup` 是**同一棵树形的两个实例** ⇒ 要不要收口到共用骨架 | **待裁** | `Shell/BoosterInfoPopup.cs` · `Shell/BaseOfferPopup.cs` | §六·B A630 | 待裁 |
| **A631** | ⚠️ `OfferContainer.SlotTypes` 与抽屉槽名**对不上 6 格** | 代码 / 文档 | `Shell/OfferContainer.cs`（本族 prefab） | §六·B A631 | W-L1 已把实读类名写进自己的表 + 配互核断言 |
| **A632** | ⚠️ `MenuDraw.Absorb` 的档 = `QHit − 1`，本窗 **3098** 与底图档 `QBtn` **同档** · 脆 | 代码 / 记录 | `Shell/MenuDraw.cs` | §六·B A632 | 将来谁把钮的命中区放进 3089–3098 就同档（症状：点击**有时**无反应、静默） |
| **A634** | 原版 `BaseOfferPopup.Close()` 的 `Unload(assetGroup 2)` + `RemoveListener` **我们没做** | 代码 | `Shell/BaseOfferPopup.cs` | §六·B A634 | 老账 A123 |
| **A555** | `ArenaBuilder.ApplyGroupNodes` 建的分组节点与 `nodes[]` 的**重叠面没做过全量普查** | 只读 | `ArenaBuilder.cs` | §六·B A555 | 半齐 |
| **A620** | `工具/menu_rect.py` 的 `walk` **两处静默**（`depth` 截断 / `rt is None`） | 代码（工具） | `工具/menu_rect.py` | §六·B A620 | 属「本表不全」另一族 |
| **A621** | `工具/menu_dump.py` 的**次级取名字处**仍走 `go_name`（约 `:2031`）⇒ 撞车包下**统计数字**仍可能取错 | 代码（工具） | `工具/menu_dump.py` | §六·B A621 | 同 A541 / A619 族 |
| **A622** | D3（**类名优先 vs 字段指纹**）**只核了 1 个包**（menus 1470 个布局组），其余 89 包没逐包核 | 只读 | `工具/…` | §六·B A622 | 半齐 |
| **A625** | `工具/menu_rect.py` 的 **Mask 指纹比 `menu_dump.fingerprint` 多一条脆 clause** | 代码（工具） | `工具/menu_rect.py` | §六·B A625 | 实测额外命中 0、今天无害 |
| **A626** | `menu_rect` 同文件**两种口径** + `MR.walk` **按下标取元组** + 另两个脚本也 import 它 | 代码（工具） | `工具/menu_rect.py` · `工具/menu_dump.py` · `工具/menu_dump_w1diff.py` · `工具/_probe_deckinfo.py` | §六·B A626 | 以后往那个元组塞东西**必须加在末尾** |
| **A801** | 🔴 `repack()` 产物**不改内层名**（沿用既有行为）—— ⚠️ 一旦源包与产物**同时加载**，会变成「整包被拒收」的雷 | 代码 / 风险 | `工具/extract_missing_shaders.py` | §六·B A801 · `W230_跨CAB与repack.md` | 已写进代码输出；**未立账部分等真实需求** |
| **A802** | ⚖️ **待裁**：跨 CAB 那一支按 A173 口径**整组留** ⇒ 真例产物 **10.9 MB** / 合成例 +4.7 MB 未压缩；「要不要把别组也裁 / 连它的流一起留」**没定** | **待裁** | `工具/extract_missing_shaders.py` | §六·B A802 · §九·4 | 需真实需求或用户拍板（已写进 `[6] ⓘ 体积：…`） |
| **A584** | `Shell/DailyData.cs:485` 注释「`_skullsCount` …**出厂恒 160**」—— 现读 `:130` 是 `= 0` | 代码·注释 | `Shell/DailyData.cs` | §六·B A584 | 纯注释 |
| **A590** | A 表那句「**965 件粒子**」复现不出 ⇒ 实测 = **578 粒子 + 717 网格** | 文档 | `项目任务.md` §三 第 30 条 / A345 那行 | §六·B A590 | **合并时按此订正那张 A 表** |
| **A594** | 「要补哪几条 = 旁挂自己给的」**已半过期**（旁挂仍是来源之一、不再是主路） | 文档 | `资料/战场场景线_交接.md` 等 | §六·B A594 | 无 |
| **A595** | `工具/gen_env_blendables.py` 两处注释**不再成立** + 清单**11 处重复** + A345 那条盲区**风险面被放大** | 文档 | `工具/gen_env_blendables.py` | §六·B A595 | 与 A345-T-b 相关（T-b 要盯「没对上」） |
| **A619** | 🔴 撞车包那 7 个根下的 **24 个节点名字要重核**（凡引用过它们的文档） | 文档 | 各引用文档（`A表现核_块1.md` / `资料/日常_原版规格.md` 等） | §六·B A619 | W499 已用文件级真值表独立复核 **24/24 吻合** |
| **A623** | `资料/普查产出_1006/乙_工具批.md:150` 记的「`menu_rect.py` = 476 行」**早已过期**（改后 812） | 文档 | `资料/普查产出_1006/乙_工具批.md` | §六·B A623 | 无 |
| **A636** | `资料/阶段二_锻造厂与战役页_原版规格.md` §十四（约 `:107-111`）**还留着旧模型** | 文档 | 同左 | §六·B A636 | A537 之后不再成立 |
| **A637** | `资料/普查产出_1012/E1_逐子件宽与Clip清空.md` §一「实到矩形」表与 §五·3 过期（+ `批次计划_1013.md` 引的行号） | 文档 | 同左 | §六·B A637 | 无 |
| **A638** | 原 A239 那条「注释订正 `2135..2380` → `1955..2200`」**已过期**（新模型按钮在 `1370..1615`） | 文档 | 原 A239 那条 | §六·B A638 | 无 |
| **A572** | 🔴 `资料/普查产出_1013/A表现核_块2.md` §A505 的**判据引用错**（**全库没有 `Shell/ShellScene.cs`**） | 文档 | 同左 | §六·B A572 | 正确出处 = `Editor/ShellScene.cs:2000-2005` + `Editor/MainMenuScene.cs:205-232` |
| **A656** | 🔑 **读原版「钮挂什么」的三级跳**（`GameObject.json` → `m_Script.m_PathID` → `m_ClassName`）· **已记** | 记录（知识） | — | §六·B A656 | ⚠️ 正文写「已记」⇒ 大概率**不是待办**，见 §二·8 |
| **A665** | 🔑 `.c` 里看不出来、照抄会静默错的判据取法（`FUN_…` 认名 / `get_deltaPosition` 等）· **已记** | 记录（知识） | — | §六·B A665 | 同 A656，见 §二·8 |
| **A562** | 🔴 仓库里的 `wf_shaders_extra.bundle` **是过期的**（1.3 MB / md5 `ecebd2f8…`）⇒ **定要不要重打** | Unity腿 | `wf_shaders_extra.bundle` | §六·B A562 · §九·4 | 排进最后那次 Unity 腿 |
| **A577** | `Counters` 的 HLG 位置**只能真 Play 定案**（现有 dump 里整棵树 `activeInHierarchy=False`） | 挂起（真 Play） | — | §六·B A577 | 已挂 `真Play待验清单` |
| **A580** | 🟡 灰化的**倍数**实拍核不了（`0.784` 无法在视频帧上定标） | 记录 | `Shell/MissionsTab.cs` | §六·B A580 | 按原版序列化字段写、**如实标注** |
| **A586** | ⚖️ 原版「跨重启靠服务端」是**反编译推的、非实拍** | 挂起（真 Play） | — | §六·B A586 | 已挂 `真Play待验清单` |
| **A587** | ⚖️ `ShellScene.Play` 在 **GUI 手调**时 `isPlaying && !isBatchMode` ⇒ 落盘门开着 | 记录 | `Editor/ShellScene.cs` | §六·B A587 | 11 条标准自检不走那条路；**留着、如实记** |
| **A606** | 🔴 **A553 的断言【没落地】**（成品代码在 `WSmall2_场景重试闩.md` §五，可直接粘贴） | 断言 | `Editor/BattleScene.cs`（A514 段约 `:10609-10804` 之后） | §六·B A606 | 排在 **A514 段之后 · 整轮收尾之前** |
| **A618** | 🔴 A490 的新量法**是「可能引入新红」的唯一一处** ⇒ 红了**先量实得值**再判 | 验证 / 记录 | `Editor/MainMenuScene.cs` | §六·B A618 | 同步点 `MainMenuScene.Run` 时专看；⛔ 别直接改期望值 |

### H 段（§三 第 29 条 · A750–A813 · 表上列出的 29 个）

| A 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **A803** | 🔴🔴 **β1**：`PurchasePremiumWindow.Open()` 只 `Initialize()`、**从不 `Build()`** ⇒ 这扇窗**开出来是空树**（约 **25 条红**跨 `ShellScene`+`ShopScene`） | 代码 | `Shell/PurchasePremiumWindow.cs` | `D1013_诊断_块2/块4` · `批次计划_1013.md` §九·5 β1 | **一行修**：`Build();` 必须在 `Initialize()` **之前**；⚠️ 与 A806 **同文件 ⇒ 一次改完** |
| **A804** | 🔴 **β2**：`Battle/Label.cs:862/872` 的 `AlignLeftOn`/`AlignRightOn` **平移后不重裁** ⇒ 文字裁切框被推走（实测 **+123.16px**） | 代码 | `Battle/Label.cs` | §九·5 β2 | 各补 `ClippingTextGuard?.Reclip()`；**断言要跟着改**；**共用件 ⇒ 全套** |
| **A805** | 🔴 **β3**：`Shell/BaseOfferPopup.cs:804-805/811-813` 的 `MenuDraw.AlignLeft` 排在 `SetAutoFitBox` **之前**（被 `RefreshBounds()` 抹掉） | 代码 | `Shell/BaseOfferPopup.cs` | §九·5 β3 | **两行整行挪到 `SetAutoFitBox` 之后**（同文件 `Descripton` 是正确写法） |
| **A806** | 🔴 **β4**：`PurchasePremiumWindow` 容器**相对档 / 绝对档混用**（A780 推导成立） | 代码 | `Shell/PurchasePremiumWindow.cs` | §九·5 β4 · §六·B A780 | ⛔ **必须改两半**（容器搬绝对档 + `Sub` 基准改 `Abs(ContainerR)`）；⚠️ **先只改 A803 再跑一次、用那条报出的实得坐标反证**；另含 A431 注释预告过期 + 收尾四段「不许依赖 `driver`」注释纪律 |
| **A812** | ⚖️ **待用户拍板**：`Shell/AllianceEventScorePanel.cs:168-179` 的 `SetAllianceName("")` —— **日志说「保持出厂原文」、实现一条都没回写**；两处口径打架 | **待裁** | `Shell/AllianceEventScorePanel.cs` · `Editor/MainMenuScene.cs:8339`（断言） | §九·5 需裁表 · §六·B A812 | 原版语义**本地读不到**（只有远端 group service 有真值）；事实层 1.0 / 该按哪边修 0.6 |
| **A807** | 🔴 素材腿**时序坑**：`Resources/Art/**` 在 `.gitignore`（`:91`）⇒ 素材腿要**排到全部写手交件之前** | 纪律 + 文档 | `.gitignore:91` · 派活次序 | §九·5 顺手发现① | 本轮因此 **8 条「缺图」断言静默过期** |
| **A808** | 🔴 手拷的那 **13 张图在任何导入器里零命中** ⇒ 谁跑一次导入器 / 删目录，那 4 条断言**静默翻回红** | 代码（工具） | `工具/import_original_art.py` · `工具/sync_battle_ui_art.py`（`NAMES_MENU`） | §九·5 顺手发现② | 与 A807 同族 |
| **A809** | 🔴 **A422 rig 真泄漏静态订阅者**：`Editor/BattleScene.cs:10239-10244` 的 `finally` **没摘 `UnregisterResolutionSignal`** | 代码 | `Editor/BattleScene.cs:10239-10244` | §九·5 顺手发现③ | D1 与 D2 **各自独立**查到；冒一条 `[BattleCameraSreenSize] …没接上` 且被闩住 |
| **A810** | 口径收口三件：`ShopScene` 对「缺图」**三套口径** / `MaxQuadBottomPx` vs `GUnion` 两套过滤 / `InboxWindow.cs:339` 把常态图名传成 `null` | 代码 | `Editor/ShopScene.cs` · `Shell/InboxWindow.cs` | §九·5 顺手发现④⑤⑦ | 建议收口成「0 才绿」 |
| **A811** | **卡牌页那 3 条**（未展开抽屉里视口外的 `Label` 不建）= **A781/B1 的老账**、⛔ 不是本批新缺陷 | 断言 | 卡牌页宿主（`Editor/*`） | §九·5 顺手发现⑥ | D2 明确建议**别在收口批里顺手做** |
| **A753** | 🔴 A744 的「全删」补丁 —— **三个文件一份清单**（档 1 推荐：探针挂一颗 `ViewportClip` 节点） | 代码 | `Shell/SocialWindow.cs`（12 处）· `Editor/MainMenuScene.cs`（8 处）· `Editor/ShellScene.cs`（2 处） | `WA435丁_收尾.md` **§四** | **必须一次做完**：先改两个 Editor 文件、再改 `SocialWindow`；⛔ **没硬删** |
| **A776** | ⚠️ **δ 族 5 处**（自持矩形 = **第二状态源**）：调用点自己手里的矩形喂纯矩形函数，节点搬了它不跟 | 代码 | `Shell/AllianceMemberTab.cs:520/1327` · `Shell/AlliancesTab.cs:524` · `Shell/BattleLogPopup.cs:228` · `Shell/ChatPanel.cs:601` · `Shell/FriendsTab.cs:334` | §六·B A776（源 `A766` 普查） | ⛔ **不建议一刀切**；要逐处算 **0.05px 容差**（普查**没算**） |
| **A787** | 📌 `MenuScroll` 的 `MinOffset`/`MaxOffset`/`ClampLo·Hi` 是**滚程数学** ⇒ ⛔ **别顺手迁** | 记录·别动 | `Shell/MenuScroll.cs` | §六·B A787 | 别动（迁移要另判） |
| **A797** | 🔴 两处注释**已被做掉、但没订正**（铁律 5） | 代码·注释 | `Shell/PurchasePremiumWindow.cs:585-589` · `Shell/CampaignRewardWindow.cs:867` | §六·B A797 | 「下一阶段」；与 A803/A806 同文件 ⇒ 可顺路 |
| **A798** | 🔴 `MenuDraw.Text` 仍**不做「整块在框外 ⇒ 不建」**那道闸（与 `MenuWindowBase.Text` / `Rect`/`Nine` 不一致） | 代码 | `Shell/MenuDraw.cs` | §六·B A798 | ⚠️ 改动面**没查清**（会把返回契约改成「可能 null」，200 个调用点） |
| **A799** | 🔴 `clip == null` 时由**父链节点**接管 ⇒ **~145 处父链没解**（只知道 3 处会改行为） | 只读 → 代码 | 各 `Text`/`TextBox` 调用点（候选最可疑 = `Shell/ItemDrawer.cs:999`） | §六·B A799 | **先派只读**逐处解父链；归因用：`WorldW`/`textBounds` 类断言看不见它 |
| **A796** | 🔴 `MenuDraw.CheckShadeRule`（`Shell/MenuDraw.cs:2128`）那 **6 条断言【一条都不问「点了会不会关」】** ⇒ 压暗层点击在 11 条自检里**零站立点** | 断言 | `Shell/MenuDraw.cs` + 各宿主 | §六·B A796 | **要做**：补「点了会关 / 不会关」的行为断言（A788 那一类错能溜过全套自检就是因为这个） |
| **A789** | ⚖️ **已裁「不建」**，但**裁决自身有矛盾**（`Menu Demo [3740]` 不是模式卡、真卡已在「不做」表里） | 挂起（备复核） | — | §六·B A789 | **留挂起清单备用户复核** |
| **A790** | 🔴 A103 另两件**不是窗**、没有注册口 ⇒ 只提供 `Create(parent, x1, y1)` | 代码 / 待接线 | `Shell/AllianceEventScorePanel.cs` · `Shell/AllianceScoreBar.cs` | §六·B A790 | 父件 `Alliance Detail View` **我们也没建** |
| **A793** | ⚠️ 本壳**没有「逐节点 `m_LocalScale`」** ⇒ 面板根容器 rect **大 4.4%**；`InterpolateMilestones` 插值式**没读出来**（用了线性归一） | 文档 / 记录 | `Shell/…`（A103 那扇） | §六·B A793 | 如实标（画面上每个可见件都在原位） |
| **A795（缓存）** | 🔴 `Shell/MenuWindowBase.cs:521-524` 的 A720 订正注释**现在过期**（四窗四条**全是新口**了） | 代码·注释 | `Shell/MenuWindowBase.cs` | `WA750_同族三处.md` **§5·1** | ⚠️ **编号与 Energy 窗那条 A795 撞车**，见 §二·1；共用件 ⇒ **只改注释、代码零动** |
| **A796′** | 🔴 **同族更外层的缓存口仍在**：三份各自的 `RectOf` 的 **label 支** + `TextLeftPx/TextRightPx` + `CollectionScene` 四处 | 只读 → 代码 | `Editor/RewardsScene.cs` · `Editor/ShopScene.cs` · `Editor/CollectionScene.cs` | `WA750_同族三处.md` **§5·2** | ⚠️ **先派只读代理逐条判「同形同义 vs 同形不同义」**；`RectOf` 有**几十个调用点** ⇒ **不是「顺手」的量级** |
| **A797′** | ⚠️ `Editor/RewardsScene.cs:2900` 的 `if (!RectOfUnion(...)) continue;` ⇒ **「量不到」与「没被盖住」同形** = 弱断言 | 断言 | `Editor/RewardsScene.cs` | `WA750_同族三处.md` **§5·3** | 弱断言族（CLAUDE.md §三） |
| **A791** | 🔴 缺 **6 张图**（`40k_topmarquee_currency_energy` + 5 张 `40k_Crate_TierN_*_open`）· **H 表把它写成「两处注释过期」与 §六·B 不符** | Unity腿 / 文档 | `工具/import_original_art.py` | §六·B A791 | ⚠️ **实测那 6 张已在 `Resources/Art/ui_menu/`** ⇒ 缺图那半**疑似已收**；见 §二·3 |
| **A786** | ⚠️ `Editor/CollectionScene.cs:2316-2325` 的「理由」**已过期**（结论仍成立、只是理由变了） | 代码·注释 | `Editor/CollectionScene.cs` | §六·B A786 | 无 |
| **A792** | 📌 `Editor/MainMenuScene.cs:8760-8761` 的**真红法脆性**（节点 softness 若被写成字面量会静默失效） | 记录 | `Editor/MainMenuScene.cs` | §六·B A792 | **已在常量注释里写死「只此一份」** |
| **A782** | 📌 `Editor/MainMenuScene.cs:4693-4792` 的**榜单断言段没跑也没改**（尤其约 `:4762` 的 `ScanSoftCuts`） | 断言 | `Editor/MainMenuScene.cs` | §六·B A782 | 下次 `MainMenuScene.Run` 一起看 |
| **A813** | 🔴 **全仓 `.md` 表格竖线普查**：**767 份里 53 份**存在「同一表块内竖线数不一致」⇒ **剩下 53 份待清** | 文档 | 53 份 `.md` | §六·B A813 · `工具/check_pipes.py` | 工具**已就位**；判据 = 同一连续表块内竖线数恒定（⛔ 排除 GFM 转义的 `\|`） |
| **A338**（H 段重列） | 同 A 段：全仓 **411 处**「指向我们自己 `.cs` + 行号」的引用 | 代码 + 文档 | 全仓（六份自检宿主） | §9·2 大件组 | ⛔ 别拆给多写手（与 A 段同一笔，**别当两条**） |
| **A394**（H 段重列） | 同 A 段：场景侧 `AnimFXController` 的 `modules` 层 | 代码（大件） | 场景侧 AnimFX | §9·2 大件组 | 与 A 段同一笔 |
| **A712 一族** | 🔴🔴 **TMP 垂直对齐**：原版**非 `Middle` 占 1/3**（`Middle 64~67% · Midline 23~26% · Capline 9% · Bottom/Top 少量`）；**那 67% 的 `Middle` 其实也没对准** | Unity腿 + 代码（大件） | `Battle/Label.cs` · `Shell/MenuDraw.cs`（**共用件 ⇒ 全套**） | `WA712_垂直对齐普查.md` · §六·B A712 / A731 / A732 / A733 | 🔴 **前置 = 先在 Unity 里量【字墨】**（`characterInfo[i].topLeft/bottomLeft`）；它决定「只加档位偏移」能不能算完工；实例：**A725**（`Referral Popup` 的 `Descripton` Top）· **A735②**（`Battle/SettingsPanel.cs:71` 三根音量滑条 Bottom）· 改法三选 = **A733（待裁）** |

### 表外（§六·B 有定义、但 A 表 §29 与已收口文档里**都没有** —— 疑似漏收，共 10 条）

| A 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **A532** | `Editor/BattleScene.cs` 的 A388(a) 段注释写「`Begin` 之后 ≥ 16」，现读实际是 **19**；另附通则：`AttachPostFx` 取不到 `Volume` ⇒「钩子数」**是环境相关的** | 代码·注释 | `Editor/BattleScene.cs` | §六·B A532 | 纯注释 + 一条通则（**别写死数**） |
| **A538** | 🔴 `CampaignUnlockButton__SetUnlockCost` 在**全量反编译里零调用点** ⇒ 原版高级钮**永远是「Claim」免费态、不显点数**；我们两列都走「`PointCost ≥ 1` ⇒ 显点数」 | 代码 + 只读 | `Shell/CampaignRewardWindow.cs`（`BuildColumn` 那一段） | §六·B A538 | **半齐**（覆盖面如实：不能 100% 排除未反编译的方法）；真数据 `PointCost` 恒 ≥100 ⇒ **今天恒显点数** |
| **A539** | A472 + A402 的**断言没落地**（成品块在 `WC1_战役奖励窗.md` §五·2）；附带查出**本窗锁定态在自检里零覆盖** | 断言 | `Editor/RewardsScene.cs`（插在约 `:4632` 的 `}` 与 `:4634` 之间） | §六·B A539 | 疑似已随 A472/A402 收口 ⇒ **动手前现核**（见 §二·7） |
| **A546** | ① `Editor/SettingsScene.cs` 的 A228 前提注释**措辞略旧** ② `SetAlignLeft` 的 doc 说「要在量尺寸之前调」而**代码里没有守卫** | 代码 + 代码·注释 | `Editor/SettingsScene.cs:1523-1526` · `Battle/Label.cs`（`SetAlignLeft`） | §六·B A546 · `WLabel_余下静默口.md:192/206` | ①②**两笔账指向同一个文件**；`WLabel` 那件特意「只增不改」 |
| **A550** | ⚖️ **待裁**：**写盘失败后要不要回滚内存改动**（A398 已定语义「内存已改、落盘失败」） | **待裁** | `Deck/*` · `Shell/CollectionData.cs`（4 条警告那几处） | §六·B A550 | 请主对话或用户裁（与 A613 / A535 / A566 **同形状，第四次问**） |
| **A569** | 🔴 **「跨窗重建」普查**：开一扇全屏窗 ⇒ 别窗置 `Closed` ⇒ 关掉它 ⇒ `ShowPreviousWindow` 把列表尾**带回 ⇒ 重建** ⇒ 旧句柄变**假 null**、点击**静默** | 只读 | 建议 grep「句柄赋值 → 中间有没有开那 8 类窗 / `CloseAllWindows()` → 使用点」 | §六·B A569 | 疑似已由 `WA505_同实例重开普查.md`（A671–A676）覆盖 ⇒ **动手前现核**（见 §二·7）；⚠️ `RewardWindow`(Popup) 与 `RewardsWindow`(Fullscreen) **只差一个 `s`** |
| **A571** | ⚠️ `Editor/ShopScene.cs` 约 `:1039` 的重开隐含前提「`ShopWindow.Open()` **不读 `Data`**」**没有断言守着**（同族 **12 处** `TryOpen(null)` 同样） | 断言 | `Editor/ShopScene.cs` | §六·B A571 | 「可选出声」 |
| **A702** | 🔴 **缺 3 张图**：`OctagonUI Filled SDF`（GOP/AMOP 的 `bg shadow`，工程里一张都没有）· `UI_HIghlight Internal`（PPW 的 `Hightlight`）· `40k_UI_Banner BW`（RRew 的 `Bonus points` 横幅） | Unity腿 | `工具/import_original_art.py`（`MENU_IMAGES`） | §六·B A702 · `A251-L2` 交件 | ⚠️ **实测三张都没进 `Resources/`**（只在 `Assets/CardPresentation/Art/原版/0_mainmenu/`）⇒ **仍开着**；节点已照建、不画 + `LogWarning`，断言钉 `MissingArt` 张数 |
| **A738** | 🔴 **A13（`Shell/CampaignTab.cs` 的 `ClearClip()/RestoreClip()` 整对删除）没做** —— 删了会让 W-E3 的 6 条 A353 断言全红 | 代码 + 夹具 | `Shell/CampaignTab.cs` · `Editor/RewardsScene.cs`（A353 夹具） | §六·B A738 | ⚖️ 已裁「接受 ①：就留 + doc 写明『A13 未做 + 原因 + 谁改夹具谁删它』」· ⏭ **立账待做** |
| **A752** | ⚠️ `Editor/MainMenuScene.cs:5320` 的 C3 块注释写「删掉 `BuildTabButton` **末句** `SetWrapping(false)`」—— 亲读：那句在 `Shell/MenuWindowBase.cs:525`、**后面还有 ~30 行** ⇒ **不是末句** | 代码·注释 | `Editor/MainMenuScene.cs` | §六·B A752 | 纯注释（共用件只改注释 ⇒ 不触发复跑） |

### 收口自检带出来的 61 条（46 α + 12 δ + 3 γ）—— 一行级、已逐条诊断

| 编号 | 一句话 | 类型 | 要动的文件 | 判据全文在哪 | 依赖 / 串行约束 |
|---|---|---|---|---|---|
| **（无独立 A 号）** | 90 条红 = 46 (α) 断言错 · 29 (β) 实现缺陷（= **A803–A806**）· 3 (γ) 本批回归 · 12 (δ) 夹具前提 ⇒ **除 β 之外的 61 条全是断言/夹具侧一行级订正** | 断言 / 夹具 | 四份报告的宿主（`Editor/{BattleScene,DeckScene,ShellScene,CollectionScene,MainMenuScene,ShopScene,RewardsScene}.cs`） | **正本 = `资料/普查产出_1013/D1013_诊断_块{1..4}_*.md`**（逐条含证据 + 最小改法 + 置信度）· 汇总 → `批次计划_1013.md` **§九·5** | 各条**最小改法已给**；⛔ 别自己发明口径；改完**只复跑受影响的那几条** |

---

## §一 已设计好的分组（上一轮抄下来的）

### 1·1 `批次计划_1013.md` **§九·2** 的**七组**（原文口径）

| 组 | 条目 |
|---|---|
| **① 大件（独占窗口 / 要连做几轮）** | **A338**（411 处 · 要动全部六份自检宿主 ⇒ 调度上最贵、**别拆给多写手**）· **A394**（场景侧 `AnimFXController` 的 `modules` 层）· **A712 一族**（TMP 垂直对齐 · 🔴 **前置 = 先量字墨**） |
| **② A435 的尾巴** | **A753**（A744 的「全删」补丁——三个文件一份清单）· **A776**（δ 族 5 处）· **A781**（`MenuDraw.Text`/`TextBox` 的文字半边 —— **已交件**）· **A787**（`MenuScroll` 滚程数学、⛔ 别顺手迁） |
| **③ 同族收口** | **A750**（同族第四/五/六处换口 —— **已做**）· **A788**（压暗层点击 —— **已裁「判据不成立」**）· **A786** · **A782** · **A649 余项** |
| **④ 卡在判据 / 远端** | **A163** · **A186 + A376** · **A367** · **A434** · **A577** · **A586** · **A231③ / A564** · **A608** |
| **⑤ 需用户拍板** | **A613**（写盘失败要不要拦）· **A630**（两扇 popup 要不要收口共用骨架）· **A675**（「窗内子树重建」要不要普查）· **A789**（裁决自身的矛盾）· **A812**（`SetAllianceName("")`） |
| **⑥ 纯文档 / 零自检** | **A584** · **A590** · **A594** · **A595** · **A619** · **A623** · **A636** · **A637** · **A638** · **A572** · **A262-M-a/M-b** · **A428 类** |
| **⑦ 「如实标注 = 结论，⛔ 别去修」** | **A378②** · **A548②** · **A549②** · **A568③** · **A580** · **A587** · **A604** · **A661** · **A662** · **A716** · **A731**（U+0435 类）· **A787③** |

### 1·2 `资料/可并行任务清单.md:529` 的**下一批七组**（更新的一版，与 1·1 有差）

| 组 | 条目 |
|---|---|
| **① 大件** | **A338** · **A394** · **A712 一族**（前置先量字墨） |
| **② A435 的尾巴** | **A753** · **A776** δ 族 · **A787** |
| **③ 文字半边** | **A797** · **A798** · **A799** |
| **④ 压暗层 / 窗口行为** | **A796** · **A789**（待用户复核）· **A790** · **A793** |
| **⑤ 缓存口径族** | **A795** · **A796′** · **A797′** |
| **⑥ 纯文档订正** | **A786** · **A792** · **A782** |
| **⑦ 卡在判据 / 远端** | **A163** · **A186 + A376** · **A367** · **A434** · **A577** · **A586** · **A231③ / A564** · **A608** |

> ⚠️ **两版差异**：1·2 把 A796–A799（文字半边 / 压暗层）、A795/796′/797′（缓存口径）**单独立组**；1·1 的「同族收口」里含 **A750 / A788（两条都已收）** 与 **A649 余项**；1·1 的「如实标注」组（⑦）在 1·2 里**没有对应组**（别漏）。

### 1·3 顺序（唯一正本 = `项目任务.md` §〇）

1. 🔴🔴 **先清「收口那次自检的 90 条红」** —— **四个 β 根因 = A803 / A804 / A805 / A806**（**第一优先级**）+ 其余 61 条一行级订正（D1013 四份报告）。
   ⚠️ 改完**只复跑受影响的那几条**（β1 跨 `ShellScene` + `ShopScene` ⇒ 两条一起跑；`BattleScene` / `DeckScene` 只动宿主 ⇒ 各一条），**全套留到下次跨面**。
2. 之后才是 **G/H 两组（本轮新账）** 与 **A/F 两表（原表余项）** —— 排法与切块 → 上面那七组。
3. **挂起清单（判据在远端 / 等真 Play / 已裁）**：A163 · A186 + A376 · A38④ · A367 · A434 · A577 · A586 · A231③ / A564（+ 本表的 A608）。
4. **本轮执行纪律**：A 表清零前**不中途跑 Unity 自检**；过程中查出的问题**当场解决**；`_run_8_checks.sh` 只在**账清零 / 跨面 / 准备提交**时跑（**必须串行**）。秒级类型检查**改完 `.cs` 立刻跑**（带独立 `TMPDIR`）。

---

## §二 存疑 / 矛盾处（状态列可能过期的）

1. 🔴 **A795 一个编号两笔账（真撞车）**：
   - **A795 = Energy 窗压暗层回归**（`Shell/EnergySinglePlayerOnlyEventWindow.cs:272` 只有 `Absorb`、零 `ShadeHit`）—— **已由写手 WA795 修完**（报告 `资料/普查产出_1013/WA795_能量窗压暗层修复.md`，改动已落盘、含订正痕；`已知的坑.md:4167` 也按这一笔记的）。
   - **A795 = `Shell/MenuWindowBase.cs:521-524` 的 A720 订正注释过期**（来源 = `WA750_同族三处.md` §5·1）—— **仍开着**，且被 `可并行任务清单.md:529` 当成「⑤ 缓存口径族」的第一条。
   ⇒ **两处定义都在 2026-10-13 同一天产生**。建议给「缓存口径」那笔**改号**，否则下一个会话必然改错文件。
2. ⚠️ **A796′ / A797′ 用撇号编号**（与 A796 = `CheckShadeRule` 6 条断言、A797 = 两处注释订正**同号不同事**）。建议改成连号。
3. ⚠️ **A791 在 H 表被写成「两处注释过期，见上」**，而 `§六·B` 的 A791 = **缺 6 张图**（`40k_topmarquee_currency_energy` + 5 张 `40k_Crate_TierN_*_open`）—— 两者**不是同一件事**（「两处注释过期」实为 **A797**）。
   🔎 **实测**：那 6 张**都已进 `MyGame/Assets/CardPresentation/Resources/Art/ui_menu/`**（`40k_Crate_Tier{1..5}_*_open.png` + `40k_topmarquee_currency_energy.png`）⇒ **缺图那半疑似已收**，动手前现核后销账 / 改号。
4. ⚠️ **A666 / A667 仍列在 G 段**，但 `资料/历史/A表已收口_1013.md` **§四**已记 **W-E1c 做完**（它们也在 `项目任务.md:427` 的「当天开、当天做」名单里）⇒ **表上这两行过期**。
5. ⚠️ **F 段那一行「A285 / A385 / A494 / A543 / A582 / A629 = 主对话 Unity 腿（一次跑完）」整组一起挂着**，但本轮实测**只有 A629 还没做**：
   - **A285 / A385**：`ui_menu/*.png.meta` 里 `nPOTScale: 1` = **0**（判据正是「36 → 0」）⇒ **已达成**。
   - **A494**：`Laser_Wave_2.png` · `LightningTrail.png` · `Noise_Combined.png` · `Shine_trail.png` **全在 `ui_menu/`** ⇒ **已达成**。
   - **A543**：4 张币种图（crystal / blackstone / ticket / energy）**全在 `ui_menu/`** ⇒ **已达成**。
   - **A582**：5 张 `40k_Crate_Tier{1..5}_*_open.png` **全在 `ui_menu/`** ⇒ **已达成**（导入器那一步已先跑）。
   - **A629**：`40k_shop_popup_info_bg` 与 `40k_OfferBadge` **只在 `Assets/CardPresentation/Art/原版/0_mainmenu/`，没进 `Resources/`** ⇒ **仍开着**。
6. ⚠️ **A425 疑似未做**：`CardPresentation/Effects/` 目录里**没有** `RewardAppearParticle.prefab`（全工程只有 `Assets/WarpforgeVFX/Prefabs/RewardAppearParticle.prefab` 一份、目录里也没有 `RewardAppear` 命名的件）⇒ 与 A425 第 ② 步（「导 prefab 到 `CardPresentation/Effects/`」）不符。动手前现核。
7. ⚠️ **A533 在 G 段出现两次**（「牌库写盘族尾巴」与「同族按并集/自证当尺子」两组）—— **是同一笔账**（`Editor/DeckScene.cs:~3073` 的弱断言），别当两条排。
8. ⚠️ **A656 / A665 挂在「文档订正」组，但正文写「已记」** —— 它们读起来是**知识记录**（可复用的读法/取法）、不像待办。建议销账或明确降为参考（同 A659/A664 那种「✅ 判据闭合」的写法）。
9. ⚠️ **A539 / A569 疑似已随别笔收口**（A539 随 A472/A402；A569 随 `WA505_同实例重开普查` 的 A671–A676）—— 两者都**没有** ✅ 标记，但工作已发生 ⇒ **列入前先现核**。
10. ⚠️ **G 段「Unity 腿 / 真 Play」那一格自己写着**「✅ 主对话 2026-10-13 已跑完那一趟（素材 18 张全补 · 导入设置 36 → 0 = A285/A385 收 …）」—— **同一格里既有「已收」也有 F 段那行「仍开着」**，两处口径不一致（见第 5 条）。
11. 📌 `批次计划_1013.md` **§八**自陈的 A 表自身错处：**A404 行文件写错**（应 `Shell/CardDetailPopup.cs`）· **A472 行文件写错**（应 `Shell/CampaignRewardWindow.cs:676`）· **A465 行「别改成两轴」已过期** · **A513 行归属写反**。⚠️ 这四条**都已收口**（`A表已收口_1013.md` §二），所以现在**只在历史里成立**；但同一段的另两条仍有效：**A 表 A404/A472 那种「文件写错」是同一种病** ⇒ 动手前现读文件名。

---

## §三 我数出的总数（按类型分）

**总表一共 145 行 / 145 个编号**（每行一个编号；`A186 + A376`、`A285 / A385`、`A338`（A/H 两段重列）、`A394`（同）、`A712 一族` 已按「一笔账一行」合并计数）：

| 来源 | 行数 | 说明 |
|---|---|---|
| **A 段**（阶段二 / 界面） | **15** | A262 · A268 · A338 · A348 · A367 · A380 · A383 · A394 · A418 · A425 · A451 · A464 · A469 · A492 · A512 |
| **F 段**（各线并入的账） | **14** | 含 `A186 + A376`、`A285 / A385` 两条合并行（合起来 16 个编号）· **A591 也在这段**（G 段那行不再重列） |
| **G 段**（A516–A676） | **75** | = A 表 G 段那 76 个编号去掉已在 F 段列的 A591 |
| **H 段**（A750–A813） | **31** | 其中 A338 / A394 是同 A 段那两笔的重列；**净新增 29 个**（含 `A712 一族`） |
| **表外**（§六·B 有定义、A 表与已收口文档都没收） | **10** | A532 · A538 · A539 · A546 · A550 · A569 · A571 · A702 · A738 · A752 |
| **另**（无独立 A 号的一笔） | —— | 收口自检的 **61 条 α/δ/γ**（一行级订正，逐条改法在 `D1013_诊断_块{1..4}.md`） |

**按类型分布（互斥口径，合计 = 145）**：

| 类型 | 条数 | 备注 |
|---|---|---|
| **代码（含小工具 `.py`）** | **43** | 真正要改 `.cs` / `工具/*.py` 的 |
| **文档 / 注释（零自检）** | **29** | 铁律 5 类订正；**纯文档 ⇒ 一条自检都不用跑**；其中约一半落点是 `.cs` 里的注释 |
| **断言 / 夹具** | **16** | 只改自检侧（弱断言、静默假绿、量法换口、断言没落地） |
| **Unity腿（含素材腿 / 重建 / 验证）** | **15** | 其中 **A285/A385 · A494 · A543 · A582 四条实测已达判据**（见 §二·5）⇒ 真正还欠的是 **A418 · A425 · A629 · A702 · A791 · A562 · A231③/A564 · A345-T-b(A591) · A268①** |
| **记录 / 别动（「如实标注 = 结论」）** | **14** | ⛔ 这些**不是待办**（A580 · A587 · A603 · A604 · A632 · A648 · A652 · A656 · A665 · A787 · A792 · A793 …）—— 排活时**应剔除** |
| **只读普查（判据未齐，先派只读）** | **9** | A380 · A383 · A539 · A555 · A569 · A571 · A622 · A650 · A796′ |
| **待裁（用户 / 调度台）** | **9** | A535 · A550 · A552 · A566 · A613 · A630 · A675 · A802 · A812 |
| **挂起（判据在远端 / 等真 Play / 已裁）** | **8** | A163 · A186+A376 · A367 · A434 · A577 · A586 · A608 · A789 |
| **约束（不是活，是排活规则）** | **2** | A556 · A628 |

> 🔴 **真正「要动手」的 ≈ 145 − 14（记录/别动）− 8（挂起）− 9（待裁） = 114 笔**；其中**纯文档/注释 29 笔零自检**，**只读普查 9 笔要先派只读**。
> 🔴 **最该先动的 4 条**：**A803**（一行修、约 25 条红跨两宿主）· **A804**（共用件、2 条）· **A805**（两行、3 条）· **A806**（与 A803 同文件 ⇒ **必须一次改完**）—— 这四条之后是 **A338 / A394 / A712 一族**三件大件。
> ⚠️ **分类有交叉**（例如 A712 一族既是大件代码、又有 Unity 前置），上表按「主要形态」归类、**加总恰好 = 145** 是归类的巧合、不代表互斥。

---

## §四 顺手发现（本轮只读普查额外查到的）

0. 🔴 **本轮开工时工作区里已有【在飞的 β 修法】（未提交）** —— `git status` 不再是干净的：
   `Unity/MyGame/Assets/CardPresentation/Shell/PurchasePremiumWindow.cs`（+25/−4，**已插入 `Build();` 并带 A803 的订正注释**）·
   `Battle/Label.cs`（+18/−2，疑 A804）· `Shell/BaseOfferPopup.cs`（+15/−4，疑 A805）· `Shell/BoosterPopup…`=`Shell/BoosterInfoPopup.cs`（+6/−3）。
   ⇒ **A803 / A804 / A805 / A806 这四条不要重复派** —— 先 `git status` + `git diff` 现核，再决定是接着改还是等它交件（铁律 13·3：一个文件同一时刻只有一个写手）。
1. 🔴 **`§六·B` 里有 10 个编号从没进过 A 表**（A532 / A538 / A539 / A546 / A550 / A569 / A571 / A702 / A738 / A752）—— 其中 **A538 · A550 · A571 · A546 · A702 · A738** 在 `§六·B` 里**既没有 ✅、也没有「已裁」标记** ⇒ **按铁律 11 它们是「要做」的**，但**正本里看不见**（下个会话会漏）。已全部收进本表（见上）。
   - 其中 **A550 是「写盘失败要不要回滚内存改动」= 与 A535/A566/A613 同一形状的第 4 次问**，A 表只列了后三条。
   - **A538 是一处真行为差异**（原版高级钮永远 Claim 免费态、我们恒显点数），**半齐**、被 A537 的同批改动掩盖着。
   - **A702 三张缺图实测仍未进 `Resources/`**（`OctagonUI Filled SDF` / `UI_HIghlight Internal` / `40k_UI_Banner BW`）。
2. 🔴 **A791 的两个定义打架**（H 表「两处注释过期」vs §六·B「缺 6 张图」），且**缺图那半实测已补**（见 §二·3）。
3. ⚠️ **A345 那条 A 表写的「965 件粒子」是错的**（A590）—— 正本自己已经记着要改，但**还没改**。
4. ⚠️ `批次计划_1013.md` **§八**还留着 6 条**主对话收尾要做的文档订正**（`普查产出_*` 目录 21 vs 19/18 · `待办判据_1006.md` 没进总入口 · `已知的坑.md` 没有「切块与撞车约束」节 · `可并行任务清单.md` §一·B 6 处「？」 · `主菜单_原版规格.md:602` 的「5 个资源格」是错数 · A163 指针绕一层）—— **这些不在 A 表任何一段里**，是**无编号的待办**。
5. 📌 **`项目任务.md` §三 第 30 条（战场场景线）**还开着 **A191（补 4 个缺的宿主对象 ⇒ 改 `ArenaBuilder`）· A192③（嵌套旁挂）** 与那条 **Unity 腿顺序（逐场对旧表 → 2 → 5 → 7 → 9 → 最后 1）** —— **不在 §29 的 A/F/G/H 四段里**，但**是 A 编号的待办**，排活时别漏（明细 → `资料/战场场景线_交接.md` §五）。
