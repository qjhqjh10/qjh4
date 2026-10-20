# R · 界面线剩余条目现核（只读）

> 会话：2026-10-17 · 角色：**只读现核代理**（未改任何源码 / 数据 / 文档；未跑 Unity / 未起 player / 未碰 git）。
> 红线：本文件是 **施工输入**，不是验收结论。
> 判据顺序：当前工作树源码现读 → 反编译 `d:/2/tools/decomp_full/` → 解包 `d:/2/新解包资源/assets_full/` → 我们的索引 / 报告。
> 五档：`DONE`（其实已做完，账过期）· `STATIC`（现在就能静态改完）· `UNITY`（要 Unity 批处理）· `PLAYER`（只有真机/player 能验）· `REMOTE/USER`（判据在远端或要用户拍板）。

---

## 〇、结论速览

- **`DONE` 8 条** —— `A1927` · `A1531⑤` · `A1187` · `A1234` · `N49②` · `N49③` · `N45③` · `T27`
- **`STATIC` 14 条** —— `A1024③`（后半）· `A1488`（只差断言，见下）· `N20` · `N21①②③` · `N23` · `A1642` · `A1273` · `N32` · `A1213②` · `A1213③` · `A1213④`（其中 2 条判据空）· `A1235` · `N18` · `N49①` · `N44` · `N45④` · `T28` · `T23`
- **`UNITY` 3 条** —— `A1644` · `A1227` · `A1488` 的「新断言实跑」（宿主 `DeckScene.Run`）
- **`PLAYER` 2 条** —— `A1701` · `T09 2c`
- **`REMOTE/USER` 1 条** —— `A1853②`（14 个键缺事件承载数据）
- **两条被现核直接推翻的旧记录**：
  1. `A1701` 的「探针已插、只欠真机」**不成立** —— `N17`（2026-10-17）已把两处探针删掉（`grep A1701` 全仓只有 `Editor/DeckScene.cs:7975` 一句注释）。
  2. `A1642` 的「页头件是第一个孩子」**不准确** —— 我们**根本没有 `Header` 容器节点**，页头件是 `page` 的直接孩子（`Shell/CollectionWindow.cs:2659` 起 `BuildFilterHeader` 全建在 `page` 上）。

---

## 一、主表

| 编号 | 判定 | 证据（`文件:行号` / 资产路径） | 要做什么 |
|---|---|---|---|
| **`A1927`** 卡组费用数字小 14% | **DONE** | `Deck/DeckRuntime.cs:1975` `rowCntLb.SetFace(TmpFont.Face.LatinCardBody)`；注释（`:1962-1974`）写明原版 `m_fontAsset = Pragati-Regular SDF`、行盒 0.9474×50=47.37 ≤ 框 47.88 ⇒ 不缩；自适应四格仍 18/50/32 | 只需真机/断言复核（现行口径下不跑）。**不再是「等 `A1024③`」** |
| **`A1024③` 后半（按用途换面）** | **STATIC**（剩余=逐件换面） | `Core/TmpFont.cs:44-56` 自陈；`:185` `EnsureCjkFallback()` 已做；`:159-165` 三份拉丁面；全仓 `SetFace(TmpFont.Face.*)` **只有 1 个生产调用点**（`DeckRuntime.cs:1975`）；`Face.LatinUi` / `LatinNarrow` **0 消费者** | 见 §二·1 |
| **`A1531⑤` 防御卡第 0 格** | **DONE** | `Shell/DeckInfoPopup.cs:1353`（(甲) 并进待排）+ `:1395`（(乙) 钉第 0 格）+ `:1513` `IsDefensiveRow`；断言 `Editor/CollectionScene.cs:2266` `defPinned` | 无。近似（`Type=="defence"` 代 `spellType==210`）已在 `:1488-1512` 如实登记，等远端 `allcards` |
| **`A1488` 尾巴** | **UNITY**（只差实跑） | 🅐 收尾复位 `Editor/DeckScene.cs:1009-1033`；🅑 `x{n}` `DeckRuntime.cs:2021/4270-4285`（`1<count` 才显）；🅒 图标队列 `DeckRuntime.cs:1512`（`QText-1`）；🅓 `FltHiddenDx` `:871` + `:2588`；🅔 已量化「没被切」（`待办判据_第三十三会话.md` A1488 末段）。➕ 同段的「顺手两条」也已闭：死常量 `Cols/Rows/…` 2026-10-20 已删（`Editor/DeckScene.cs:36-41` 留痕）；费用圆 = **`RowCostS 61.7`**（`Deck/DeckRuntime.cs:254`）= 原版 `Cost Image` 61.70（旧记的 38 vs 46.2 已过期） | 跑 `DeckScene.Run`（会重写 `DeckEditor.unity` ⇒ 跑前留副本） |
| **`A1701` 先滚才能点** | **PLAYER** | `grep -rn "A1701" --include=*.cs` = **1 命中**，且只是 `Editor/DeckScene.cs:7975` 的注释；`DeckRuntime.HandleScroll()`（`:6992`）无任何探针打印；`N17`（`任务总表.md:226`）已删两处探针 | **先重新插探针**（`HandleScroll` 入口 / `HandlePointer` 5 道闸各一处、只打印），再起 player；⚠️ `工具/wf_click.py` **没有滚轮口**（`A1712`）⇒ 滚那一格要另想办法（键盘/脚本注入或手点） |
| **`N20` Toast 四条近似** | **STATIC** | ① 根高见 `N23`；② `Shell/ToastNotifications.cs:899-906`（只从 4 个 `_templateByType` 实例化）；③ `:993-1009` `ApplyFill` 改 `localScale.x`；④ `:191-193` 队列 5000 起 | ③ 改成改 quad 宽度（不缩放）；① 跟 `N23` 一起；②④ 见 §二·4 |
| **`N21` Toast 审查三条** | **STATIC**（① 其实已对） | ① ⚠️ **2026-10-20 更正：原来据 `Canvas m_SortingOrder` 判「原版 Toast 在黑场之上」—— 不成立**。实际还要看**排序层**（排序层先于 order 比较）：Toast = **PopUps(7)/900**、`LoadingMenu` = **Blocking(8)/0** ⇒ **黑场压 Toast**；错因 = **漏读 `m_SortingLayerID`**（`Canvas_1074` 所在层 = PopUps；`Canvas_1081` 的无符号层 id = Blocking）。我们 toast 4072–4076、加载黑场 4090 ⇒ **相对次序已经对**；判据 → `普查产出_1020/W58_界面线_Toast黑场次序判据.md`；② `BuildResourceCounter`（`:742`）只建进 `_templates`、**无任何实例化点**；③ `BuildCollectorTooltip`（`:643`）同样 | ① 只订正文档（旧账「黑场在 toast 之上」不成立）；②③ 删两个死模板函数 + 常量（已核实零消费者） |
| **`N23` Toast 根高两推导打架** | **STATIC**（可静态判，不必真机） | uGUI `LayoutRebuilder.Rebuild` 一轮 = 计算→控制×2（`Library/PackageCache/com.unity.ugui@…/Layout/LayoutRebuilder.cs:76-91`）；`LayoutGroupContentFixer.FixLayout` 对 `layouts` 里每件各跑一次 `ForceRebuildLayoutImmediate`（`decomp_full/LayoutGroupContentFixer__FixLayout.c`），而 Toast 根的 fixer **`layouts = [pid 204]`**（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_509.json`）；原版 `ShowNotificationInternal` 之后**又**跑一次根重建 | 见 §二·5：**静态值 108.86 只对第一轮成立；原版实际是文本驱动的高**，常量本身不成立 |
| **`A1642` 收藏容器层级** | **STATIC** | 原版 dump（`python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Select Deck Tab" --depth 4`）：`Decks Tab`(167.2,70.9→1920,1080) → `Collection Display`(同矩形) → {`Deck Scroll View`(330.9,155.9→1920,1080) → `Empty Collection Warning`(165.88,70.94→1970,1080), `Deck Filters`(0.1,155.9→335.6,1080)}；`Header`(167.2,70.9→1920,155.9) 是 `Select Deck Tab` 的**第 3（最后）个**孩子 | 见 §二·2 |
| **`？A1644` 暗色渐变矩形** | **UNITY** | 复跑 R1 的排除面：`Shell/CollectionWindow.cs` 里 `330.9/331.5/1165` **只有** `DeckViewport`（`:828`）；`MenuDraw` 无渐变族；40 个渐变组件矩形都不匹配（R1） | 唯一办法 = 建完收藏窗后遍历全部 `ImageQuad`，打印 `name / 世界矩形 / renderQueue`，筛 `x1∈[330,333] ∧ x2∈[1164,1167] ∧ y2∈[581,583]` |
| **`A1273` Daily Missions 宽** | **STATIC**（宽度已算完；只剩闸门判据未定） | 四档表 `Shell/MissionsTab.cs:113-135`（n=1 → 965.186 · n=2 → 609.500 · n=3 → 253.814 · n=4 → 0，`surplus/mult` 算式逐步可复算）；当前恒建两张卡（`:288/:296`）⇒ 落 n=2 | 见 §二·3 |
| **`N32` 三条日常的真计数** | **STATIC**（引擎 3 个计数器） | 现成只有 `BattleContext.DamageToEnemy/TroopsPlayed`（`RuleEngine/Core/BattleContext.cs:275-276`）；落点：① 对敌方**督军**伤害 → `RuleCore.cs:6124`（`OwnerOf` 之后那一句，`u.IsWarlord` 可判）；② ③ 攻击次数 / 远程攻击次数 → `RuleCore.cs:4291/4302`（`attacker.AttacksThisTurn++` 与 `attacker.LastAttackType = ranged ? 2 : 1;` 之间） | 见 §二·6 |
| **`A1187` ClippedText / Price Display** | **DONE**（矛盾已解） | 🔴「Army Name 原版关着」= **认错件**。真件 `Wildcard Drawer ▸ Content ▸ Army`：`604,702→1316,810`（712×108）· `字号=75.0 基准=36.0 auto[12.0~75.0] 对齐=Center/Bottom 折行=1`、**未标 `ANC✗`（原版画）**（`menu_dump.py … "Wildcard Drawer" --depth 6` 现读）⇒ `Shell/ItemDrawer.cs:1196-1218` 的注释与接线（`:1217-1218` `ClippedText(..., 0, 12f*k, 75f*k, 36f*k)`）**都对**；`Item Name` 只存在于 `Placeholder()`（`:1258-1266`，**我们自造**、原版无对应件）⇒ 判据空，不接 | 只订正账；② 价签逐站已全部接好（`BoosterInfoPopup.cs:608` 13.46/40/39 · `BaseOfferPopup.cs:979` 12/54/39 · `PurchasePremiumWindow.cs:221-222` · `ShopTabPage.cs:616` · `ItemDrawer.cs:1046` 13.46/65/39 · `DailyRewardPopup.cs:414` · `AllianceSearchTab.cs:638` · `OfferContainer.cs:1297` · `CardDetailPopup.cs:1211` 12/54/39） |
| **`A1213②` PopNumber 未拆** | **STATIC** | 原版两颗 MB 现读（`bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/5890981294211439552` = Damage / `…-7677963611639080000` = Heal）：Damage `auto=1 · fs 5.5 · min .35 · max 5.5 · base 36 · wrap 0 · Center/Middle`；Heal `auto=0 · fs 0.7 · min .35 · max 1.4 · base 36 · wrap 0`。🔴 **且 Parent 链不同尺度**：Damage 自己 `m_LocalScale = 1.54`（`RectTransform_5482325324160211904`），Heal = 1.0；两者父链其余全 1.0 ⇒ 有效比 = `5.5×1.54 : 0.7` = **12.1 : 1**（旧账的 7.86 : 1 漏了 1.54） | 见 §二·7 |
| **`A1213③` 设置面板 7 组** | **STATIC** | 全 7 组四格现读（`menu_dump.py bundle_scenes_scenes_battlearena1 "BattleSettingsPanel" --depth 5 --no-sprite --no-layout`）：见 §二·8 表。我们现状：音量三颗（`SettingsPanel.cs:1747`）· `_muteLabel`(`:1636`) · OFF/ON(`:1615/1622`) **已接**；`Auto zoom`(`:969`) · `Resign`(`:863`) · `Skip tutorial`(`:890`) · `LanguagesDropdown Label`(`:1085`) 只走 `ApplyLangFont`、**没接 `SetAutoFitBox`** | 按 §二·8 表补 4-6 处 `SetAutoFitBox`（+ 折行还原） |
| **`A1213④` 四条读不到** | **STATIC**（2 条判据空） | ① `TurnClock`：原版静态 prefab 无此件、`ClockManager` 34 方法无一 `set_text`（R6 现核）⇒ **例外①**；② `Item Name` = 我们自造 `Placeholder()`（同 `A1187`）⇒ **例外①**；③④ `Loading text` / `Progress text`：`assets_full/level0/GameObject/{Loading text,Progress text}.json` **已导出**（组件 pid `296/298` 与 `295/297`），只有那两个 TMP MB 缺（目录里 295–298 不存在）；原始 `level0` 在 `d:/2/unity_run_ref/Warpforge_Data/level0` | 见 §二·9（手解原始 level0；模板 = 任一已导出 TMP MB，`m_Script` pathID `6627610752530350053`；先例 = `工具/read_gradient2_level0.py`） |
| **`A1227` SetWrapWidth 改 alignment** | **UNITY** | `Battle/Label.cs:427-446`（`SetWrapWidth` 未激活时只记 `_pendWrapW`、**不生成**）；`Core/TmpFont.cs:503-528`（`SetWrapWidthRect` 只写模式+宽；`GenerateLayout` 只 `GetTextInfo`）；全仓写 `_tmp.alignment` **只有 `Label.cs:539`（`SetAlignLeft`）**；全仓 `grep A1227 --include=*.cs` = **0** | 一次极小 repro（未激活父链 → 建 Center 标签 → `SetWrapWidth` → 激活 → 读 `alignment`）。复现不出 ⇒ 按「已被 `A266` 顺带消掉」结案 |
| **`A1235` 中/英字号静默差** | **STATIC** | 全库普查（`SetCapHeight`/`SetGlyphHeight`/`SetScriptHeight` 与 `SetAutoFitBox` 同一 receiver）：**9 站** = `Battle/SettingsPanel.cs` 的 `SliderLabel`(×3) + `_muteLabel` + `_muteOffText` + `_muteOnText`（6）· `Battle/MulliganPanel.cs` 的 `_prompt` / `_turnText` / `_doneText`（3，均经 `SetScriptFont`→`SetScriptHeight`）。其余 `SetAutoFitBox` 站点全走 `SetGlyphHeight`（`MenuDraw.Text/TextBox`、`DeckRuntime`、`CollectionWindow` 各助手）⇒ 不踩 | 见 §二·10 |
| **`N18` 字号对账口径** | **STATIC**（文档） | 口径**已经**在 `资料/已知的坑.md:6504-6505`（「桌面档必须按 TMP 自身 `m_fontSize*` 取」）；抽样复核普查来源：`menu_dump` 打的就是 TMP 自身字段（`Create Deck` = 42/12/auto[10~42]，与 R5 一致；`Booster Info` 价签 = 13.46/40/39/0，与 R2 一致） | 把这条写进 `任务/T07` 的判据段 + 在 R5/R6/RM 三份报告头部标「取数来源 = TMP 自身字段」；**不改代码** |
| **`N49` T07 修正批尾巴** | **① STATIC · ② DONE · ③ DONE** | ① 原版材质 `Asar-Regular Light Grey w outline thin`（`bundle_fonts_assets_all/Material/Material_751232751962157263.json`）：`_OutlineWidth = 0.10` · `_OutlineColor = 黑` · `_FaceDilate = 0.10` · `_OutlineSoftness = 0`；落点 `Battle/TutorialOverlay.cs:737-745`（4 颗教学标注）。② `Badge/Text (TMP)` 的 `m_sharedMaterial` = `Pragati-Regular Atlas Material`（pid `-5050103597772954521`）`_FaceColor = (1,1,1,1)`、`_OutlineWidth = 0`（5 份同名 MB 逐份同值）⇒ **白色是对的**，只补注释。③ 原版抽屉 `Price Display` 节点 = **456.96×93.55**（`Card Drawer` / `Wildcard Drawer` 现读）⇒ 我们 `ItemDrawer.cs:1030` 的 `93.55f * k` **已经对**；旧账「父件 108」是把**我们改前**的 108 当原版 | ① 给 `TmpFont.ApplyOutline` 加 `Thin` 档（0.10 + FaceDilate 0.10）；②③ 订正注释 |
| **`A1853②` 16 个事件键** | **REMOTE/USER**（2 键已接） | 已接 = `ExpansionPass/{Title,GeneralHelp}`（`Shell/SkirmishEventWindow.cs:473-477` + `BaseEventPopup` 的 `ResolveTerm`）；余 14 键的消费面 = `BaseEventPopup.cs:394-403` 那张表。逐类：`DarkZealots/*`（2，过期活动 ⇒ 用户边界不做）· `Expansion/General/*`（3，承载窗 = `T28`）· `Expansion/ORKExp1/EventTitle`（1，`{0}` 参数、现传写死 `"20"`）· `ExpansionPass/TabTitle`（1，活动页签树未建）· `PurchasePremiumOffer/*`（3，报价数据在远端）· `BattlePass/*`（4，EC/SW 活动窗未建、SW/EventTitle 官方表缺） | 见 §二·11 |
| **`N44` N42 收尾三条** | **STATIC** | ① 9 条推不出的规范键（`Core/Tooltip.cs:641-650`）：其中 **3 条可确定性补齐** —— `concussion→concussive`、`darkpact→markOfChaos`、`quest→questPoints`（`trait_tips.json` 的 `bodyEn/bodyZh` 与 `官方本地化_en+zh-CN.json` 的 `CardTraitDescription/<名>` **逐字相同**，见 §二·12）；**6 条原版没有**：`ability/ferocity/huntmark/oath/pack/sentry`（`CardTraitDescription/*` 76 条里零命中、`Card_Trait/*` 77 条里也无对应名，且 `trait_tips` 的 `src` 不是 `official`）⇒ 例外①；②③ 仍是 PLAYER（字体覆盖要看真机、中英逐字节要跑 `SettingsScene.Run`） | ① 给 `LoadOfficialTraitMap` 加第 4 条规则「正文（归一化后）唯一命中」；6 条写「原版没有」；②③ 存档 |
| **`A1234` MissionsTab 裁切次序** | **DONE** | `Shell/MissionsTab.cs:386-393` 自陈 **2026-10-17（A1234 结案）**；机制：`Label.SetWrappingMode`（`Battle/Label.cs:607-630`）→ `ForceRelayout` → `RefreshBounds` **末句 `guard.Reclip()`**（`:1350`）⇒ 任何重排都当场重裁，顺序不再是承重条件；且 `RewardsWindow` 今天无 `ViewportClip` | 无 |
| **`T09 2c` 协程分批 / 预灌字形** | **PLAYER** | `资料/普查产出_1017/UI/PERF_T09_收藏窗性能诊断.md:47`「若 2a 指向立绘加载 ⇒ 分批；若指向字体冷路径 ⇒ 预灌字形」；`:72-73` 明写「先收一次 `[CardView.Build]` 分段，再决定 2c」；`CardView.Build` 的分段计时已加（只在真 Play 出声） | 起一次 player、开收藏窗，收 `[CardView.Build]` 六段读数；**根据读数**再决定 2c 的形态（实现本身是静态的） |
| **`N45③` MenuFocusChangeActions 调用方** | **DONE**（查实无调用方） | 方法体 = `UIBakeScreen.Toggle(!inFocus)`（`decomp_full/MainMenuWindow__MenuFocusChangeActions.c`）；RVA `8434080` = VA `0x18080B1A0`（`d:/2/tools/all_methods.txt:16675`）；全 `decomp_full` grep `FUN_18080b1a0` = **0**；24 个 `MainMenuWindow__*.c` 逐份 grep 方法名 = **0**（除自身）；assets（`level0` / `bundle_menus_assets_all` / `bundle_scenes_scenes_mainmenuwarpforge`）grep `MenuFocusChangeActions` = **0**；而 `MainMenuWindow__ToFocus.c` / `__ToBackground.c` 已各自内联了同一个 `UIBakeScreen__Toggle` | 按例外①「原版没有调用方」留痕；**不接** |
| **`N45④` shader 剥离** | **STATIC** | `ProjectSettings/GraphicsSettings.asset` 的 `m_AlwaysIncludedShaders`（27 条）：`UIBlur` **在**（`:67` guid `7d3c5a91e2b84f60a1c9d7e35b20f486`）· `Card3D` / `FrameCutout` / `TraitDisabled` / `Video Split Alpha` 在；🔴 **`ArtOpaque`（guid `c7d07cdf2dbed6240a1db8f57d872302`）不在**，而它只靠 `Shader.Find("CardPresentation/ArtOpaque")` 取（`Core/CardView.cs:2843`）、且**没有任何 .mat 引用它** | 把 `ArtOpaque` 加进 `m_AlwaysIncludedShaders`（否则 player 里可能被剥 ⇒ 立绘底层整片缺失，**静默**） |
| **`T27` WhereToGetCardsPopup** | **DONE**（建完，待真机验收） | `Shell/WhereToGetCardsPopup.cs:65` `class WhereToGetCardsPopup : GameWindow`；断言节 `Editor/MainMenuScene.cs:12847`（报告 `W_T27_where_to_get_cards.md`） | 只欠真机看；已知偏离 `N51` 五条 |
| **`T28` Parent Event Window** | **STATIC** | **无文件**（`ls Shell/ | grep ParentEvent` = 0）；prefab `bundle_menus_assets_all/GameObject/Parent Event Window.json` 本地有、`tabs` = 2（pid `5874186985141415430` / `-7528133727782064634`）；全树 dump `资料/普查产出_1017/UI/D_ParentEventWindow_dump.txt` | 按 dump 建壳（`TabbedWindowComponents` + `BaseParentEventWindow`）；tab 数据/`CardsSpecialConfig` 运行时数据本地 0 ⇒ 留空 + 出声 |
| **`T23` Import Deck 重复件** | **STATIC** | `Deck/DeckRuntime.cs:3913` `BuildImportPopup()`（树 `imp_*`）；`:3160-3163` 自陈「生产已无入口，只剩自检口 `UiOpenImport()`」；`imp_*`/`UiOpenImport` **只在 `Editor/DeckScene.cs`（26 处）**；外壳那份 `Shell/ImportDeckPopup.cs` 独立（`CollectionWindow.OpenImportPopup` 在用） | 见 §二·13 |

---

## 二、详细判据（只写要动手的条目）

### 二·1 `A1024③` 后半 —— 按用途换面（`STATIC`）

**做到哪一步（现核）**
1. ① 判据路径已记（`Core/TmpFont.cs:30-43`）。
2. ② 三份拉丁资产在盘上、结构完整（`Resources/Fonts/{Pragati,Asar,RobotoCondensed}-Regular SDF.asset`）。
3. ③ **fallback 那半已做**：`TmpFont.EnsureCjkFallback()`（`:206-224`）在任一份拉丁面第一次被取用时，把 CJK 挂进它的 `fallbackFontAssetTable`（纯内存）。
4. ③ **「按用途选字体」那半仍未做**：`NewText` 五参重载写死 `Face.Cjk`（`:366-369`）；`NewTextUgui` 也没有任何调用点传过非 Cjk 面。

**消费者现状（全仓 grep 现读）**
- `Label.SetFace(TmpFont.Face.LatinCardBody)`：**1 处**（`Deck/DeckRuntime.cs:1975`，卡组行费用数字）。
- `Face.LatinUi`（Asar）：**0**；`Face.LatinNarrow`（Roboto）：**0**。
- `NewText(..., Face.X)` / `NewTextUgui(..., Face.X)` 的显式调用点：**0**（只有内部转发 `Face.Cjk`）。

**要做什么（落点 + 判据）**
- 逐件把「原版那颗 TMP 的 `m_fontAsset` 是 Asar / Pragati / Roboto」的件改成 `SetFace`/`NewText(face:)`。
- 判据 = 原版 TMP Component 的 **`m_fontAsset` PPtr**（`bundle_fonts_assets_all/MonoBehaviour/* SDF.json`；三份 pid：Pragati `3485036404935369831`、Asar / Roboto 同族）。⚠️ 不要按「卡名/正文」这种语义猜。
- 🔴 换面会**牵动全仓版面**（行盒差：Pragati 0.9474 em ↔ Noto 1.437 em；`A1262`/`A1235` 的换算都以字体为基准）⇒ 仓规「逐件换、逐件验收」；且 `A1226` 有 4 站框高故意不跟、就等这一步。
- 已经安全的先例：`ActualFace`（`Battle/Label.cs:843`）在文本含汉字时强制回 CJK ⇒ 换面不会把汉字交给拉丁资产（不会画方框）。

### 二·2 `A1642` 容器层级（`STATIC`，不跑 Unity）

**原版树（`menu_dump … "Select Deck Tab" --depth 4` 现读）**

| 原版节点 | 矩形 | 父 | 备注 |
|---|---|---|---|
| `Select Deck Tab` | 167.2,70.9→1920,1080 | `Tabs` | 本窗 `PageRoot(0)` |
| └ `Decks Tab` | 167.2,70.9→1920,1080 | Select Deck Tab | 容器 |
| 　 └ `Collection Display` | 同上 | Decks Tab | 容器 |
| 　　 ├ `Deck Scroll View` | 330.9,155.9→1920,1080 | Collection Display | `RecyclableScrollRect` |
| 　　 │　 ├ `Viewport` | 同 | Deck Scroll View | `RectMask2D` |
| 　　 │　 └ `Empty Collection Warning` | 165.88,70.94→1970,1080 | **Deck Scroll View** | ⚠️ 我们挂在 page 上 |
| 　　 └ `Deck Filters` | 0.1,155.9→335.6,1080 | Collection Display | 左抽屉 |
| └ `Select Game Mode Deck Tab` | 330.8,155.9→1920,1080 | Select Deck Tab | 我们没建（本窗不用） |
| └ **`Header`** | 167.2,70.9→1920,155.9 | **Select Deck Tab（最后孩子）** | 内含 `Filter Toggle` / `Control Buttons` / `Separator Line` / `Filters` |

**我们现状（现读）**
- `PageRoot(0)` = `_pages[0]`；`BuildDeckList(page)` 直接 `Node(page, "Deck Scroll View", DeckViewport)`（`Shell/CollectionWindow.cs:3207`）；`BuildDeckFilterDrawer(page)` 直接 `Node(page, "Deck Filters", DeckFltView)`（`:3299`）；`Empty Collection Warning` 也建在 `page`（`:3227`）。
- **没有 `Header` 节点**：`BuildFilterHeader`（`:2657-`）把 `Filters Button` / `Filters Icon` / `Filters Label` / `Separator Line` / `Filters` 组全部建在 `page` 上；`Control Buttons` 是 `Node(page, "Control Buttons", …)`（`:3085`）。
- 直接孩子查找 3 处：`page.Find("Deck Scroll View")`（`:3517` / `:3636` / `:3686`）。

**改法（全部静态）**
1. 在 `BuildDeckList` 开头插入 `var decksTab = Node(page, "Decks Tab", PageRect); var disp = Node(decksTab, "Collection Display", PageRect);`（两者矩形 = 整页 `167.2,70.9→1920,1080`，**像素零变化**）。
2. `Deck Scroll View` / `Deck Filters` 的父改成 `disp`；`Empty Collection Warning` 的父改成 `Deck Scroll View` 的节点（`Node(holder,"Empty Collection Warning",new PxRect(165.88f,70.94f,1970.01f,1080f))`）。
3. `BuildFilterHeader` / `BuildDeckHeader` 建出的页头件改挂到 `var header = Node(page, "Header", new PxRect(167.2f,70.9f,1920f,155.9f));`；并且**让 `Header` 成为最后一个孩子**（在 `CollectionTabPage.Setup()` 里把 `BuildDeckHeader` 挪到 `BuildDeckList` 之后，或建完 `header.SetAsLastSibling()`）。
4. 3 处 `page.Find("Deck Scroll View")` 改成已有的 `FindDeep(page, "Deck Scroll View")`（`:3523` 已存在）。
5. 断言侧：`Editor/CollectionScene.cs` 的 `FindChild` = **递归**（`Editor/MenuCheck.cs:141-147`）⇒ 插容器不会断；`CheckAt(Deck Scroll View, 330.9… )` 仍成立。

⚠️ 渲染队列不变（显式 queue 分层）⇒ 像素差保持 0；本件只动树形。

### 二·3 `A1273` Daily Missions 宽（`STATIC`）

- **宽度不是缺口**：四档已全部算完并交叉验证（`Shell/MissionsTab.cs:113-135`）；n=1 = 965.186、n=2 = 609.500、n=3 = 253.814。`CanvasScaler` 口径 = `ScaleWithScreenSize + 1920×1080 + Expand` ⇒ `scale = min(W/1920,H/1080)`，画布 px = 屏 px ÷ scale（`D_A1281_画布比例.md`）。⇒ **两种卡数下的宽度都能静态算出来，且已经写死在表里**。
- **唯一缺口** = 「实机挂几张卡」。现核：
  - prefab **作者态**：登录卡 `m_IsActive = false` ⇒ n=1（R5 §T04 现读）。
  - 用户实拍：只有骷髅卡、没有登录卡 ⇒ n=1。
  - 我们现在**恒建两张**（`MissionsTab.cs:288` 登录卡 + `:296` 骷髅卡）⇒ n=2 / 609.50。
- **能不能静态定闸门**：`DailyData.CanCollectLogin()` 存在（`Shell/DailyData.cs:1165`），可以「当天已领 ⇒ 不建登录卡」。但「原版领过之后是否隐藏登录卡」**反编译查不到**（搜过 `decomp_full` 的 `MissionsTab__CreateMissions.c` / `InstantiateNormalMissions.c` / `OnOpen.c`：方法体是大量 `FUN_` + `ContainerHolder__TryAdd`，没有可读的 `SetActive`/登录卡闸）。
- **建议**：按 `CanCollectLogin()` 选档（n=1 / n=2 两套常量都已就绪），并在注释里标「闸门是推断，判据缺」；⛔ 不要改宽度常量本身。

### 二·4 `N20` Toast 四条近似（`STATIC`）

| # | 现状 | 改法 |
|---|---|---|
| ① 根高 | 见 §二·5 → **常量不成立** | 改成按本次文案现量（与 `N23` 同一改） |
| ② Mission toast 装配 | 只静态建可见层 | 需要逐行复刻原版运行时装配 ⇒ 先读 `MissionToastNotification` 的 `Initialize`（`decomp_full/`）再定；**判据可静态取** |
| ③ `Fill` 横向缩放 | `ToastNotifications.cs:993-1009` | 改成改 quad 宽（`MenuDraw.SetQuadWidth` 一族），不碰 `localScale` |
| ④ `SortingOrder 900` | 用显式队列 5000 起 | **不是缺口**（相对次序已对，见 `N21①`）；只订正文档 |

### 二·5 `N23` Toast 根高（`STATIC`，推翻「只能真机」）

**证明链（全部本地可读）**
1. `LayoutRebuilder.Rebuild` 一轮 = `CalculateHorizontal → SetLayoutHorizontal → CalculateVertical → SetLayoutVertical`（`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/LayoutRebuilder.cs:76-91`）。
2. `SetLayoutVertical` 是**父→子**（同文件 `:105-118`：先 `ILayoutSelfController`、再布局组、然后递归子件）。
3. 所以**第一轮**里：根 CSF 读 VLG 首选高时，`Text` 的 `rect.height` 还是 0（它的 CSF 还没跑）⇒ 根高 = `12 + 89.8638 + 0 + 7 = 108.8638`；随后 `Text` 的 CSF 才把 `Text.height` 写成首选高。
4. 原版 `ShowNotificationInternal`（`decomp_full/ToastNotificationController__ShowNotificationInternal.c`）在 ③ 里跑 `layoutGroupContentFixer.FixLayout()`，而 Toast 根那颗 fixer 的 `layouts = [pid 204]`（= 根自己的 VLG，`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour/MonoBehaviour_509.json`）⇒ **又多跑一轮根重建**；紧接着 ③ 自己还有一句 `ForceRebuildLayoutImmediate(rectTransform)` = **第三轮**。
5. 后面的轮次里 `Text.rect.height` 已经是**首选文本高**，而 `VLG.m_ChildControlHeight = 0` ⇒ VLG 的首选高直接取子件 `rect.height` ⇒ **根高 = 12 + 89.86 + 文本高 + 7**，**随文案变**。

**结论**：`ToastNotifications.cs:570` 的常量 `108.8638`（和 `:608` 的 `86.7019`）**只对第一轮成立**，不是原版运行值。`Toast` 的 `Text` 节点 `折行=1`（`menu_dump` 现读 `Toast ▸ Text` = `'Serghar Targorst Serghar '` fs24 auto[1~24] 折行 1）⇒ 高度**必然**随文案/宽度变。
**改法**：`Show` 时先按当前文案量 `Text` 的首选高（我们自己的 `Label`/TMP 可静态量），再按 `padT + iconH + textH + padB` 算根高；⛔ 不再用常量；断言断「同一文案两种长度 ⇒ 根高不同」+「单行 == padT+iconH+lineH+padB」。

### 二·6 `N32` 三条日常计数（`STATIC`，最小改法）

现状：`Shell/DailyData.cs:1370-1383` 的 `OnBattleEnd(win, damageToEnemy, troopsPlayed, skulls, playMode)` 只推进 `damageToEnemy` / `troopsPlayed` / `win`；`Battle/BattleDriver.cs:17378` 从 `Ctx.DamageToEnemy[_me]` / `Ctx.TroopsPlayed[_me]` 取。

**最小改法（3 个计数器，两处引擎落点）**
1. `RuleEngine/Core/BattleContext.cs`（紧挨 `:275-276`）加：
   - `public readonly int[] DamageToEnemyWarlord = new int[2];`
   - `public readonly int[] Attacks = new int[2];`
   - `public readonly int[] RangedAttacks = new int[2];`
2. `RuleEngine/Core/RuleCore.cs:6124`（`ctx.DamageToEnemy[1 - _own] += actual;` 同一处）加：
   `if (u.IsWarlord) ctx.DamageToEnemyWarlord[1 - _own] += actual;`
   （`u` 就是被伤害的 `UnitState`；督军也在棋盘上、`IsWarlord = true` ⇒ 这条判据可靠。）
3. `RuleCore.cs` `DeclareAttack`：在 `attacker.AttacksThisTurn++`（`:4291`）与 `attacker.LastAttackType = ranged ? 2 : 1;`（`:4302`）之间加：
   `ctx.Attacks[p]++; if (ranged) ctx.RangedAttacks[p]++;`
   （**放在消耗配额之后、伤害结算之前**：这样「被取消的攻击」不计 —— 与上面 `TryCancelAttackByOwnAboutToAttack` 的早退次序天然一致。）
4. `BattleDriver.cs:17378` 把两个新值一起喂给 `DailyData.OnBattleEnd`（签名加参）；`DailyData.Advance(0/1/2, …)` 三条 mission 各自取对。
- ⚠️ `BattleContext` 是引擎面 ⇒ 改完要跑 `RuleEngineTest.Run`（本报告不动手）。

### 二·7 `A1213②` PopNumber 拆两档（`STATIC`）

- 落点 `Core/CardFeel.cs:1512-1537`：`PopNumber(..., bool heal)` 一颗函数兼两档（字号 `SetCapHeight(0.30f)` 共用）。
- 原版四格（§主表）：Damage `auto=1 fs5.5 [0.35,5.5] base36 wrap0`；Heal `auto=0 fs0.7 [0.35,1.4] base36 wrap0`。
- 🔴 **父链现核（旧账没查的那一条）**：两者同父（`-4334370980763821120`，scale 1.0），但 **Damage 自己的 RT `m_LocalScale = 1.54`**（`RectTransform_5482325324160211904`），Heal = 1.0 ⇒ 有效高度比 = `5.5×1.54 : 0.7` = **12.1 : 1**，不是 7.86 : 1。
- 改法：拆成 `PopDamage` / `PopHeal`；Damage 保留 `SetCapHeight(0.30f)`（我们的基准），Heal 用 `0.30f × 0.7/(5.5×1.54) = 0.02476f`；Damage 开 `SetAutoFitBox`（按原版 `[0.35,5.5]/base36` 折成我们的单位），Heal **不开**。补一条「两档高度比 = 12.1」的灭自证断言。

### 二·8 `A1213③` 设置面板 7 组四格（`STATIC`，现读值）

| 组 | 原版节点 | 字号 / base / auto / 折行 / 对齐 | 我们现状 |
|---|---|---|---|
| Auto zoom | `Auto Zoom Toggle/Label` | 42 / 36 / [29~42] / 1 / Left-Middle | `SettingsPanel.cs:969` 只 `ApplyLangFont` ⇒ **补 `SetAutoFitBox`** |
| 静音行标题 | `Volume Sliders/ChatToggle/Label` | 42 / 36 / [18~42] / 1 / Left-Middle | `:1636` **已接** |
| OFF | `…/OffText` | 30 / 36 / [1~30] / 0 / Center-Middle | `:1615` **已接** |
| ON | `…/OnText` | 同上 | `:1622` **已接** |
| 三根音量 | `…/{Music,FX,Voiceover} Container/Text` | 42 / 36 / [10~42] / 1 / Left-**Bottom** | `:1747` **已接**（含 VAlign Bottom） |
| Resign | `Bottom buttons/Resign Button/Button Text` | 38 / 12 / [12~38] / 0 / Center-**Midline** | `:863` 只 `ApplyLangFont` ⇒ **补四格 + `SetWrapping(false)`** |
| Skip tutorial | `…/SkipTutorial Button/Button Text` | 同上（逐字段同形） | `:890` 同上 |
| Language | `Language Selector/LanguagesDropdown/Label` | 18 / 14 / [18~40] / 0 / Left-Middle | `:1085` 只 `ApplyLangFont`；⚠️ `min == fs` ⇒ `MenuDraw` 那道闸会挡（同 `SettingsWindow` 的登记），要么直调 `Label.SetAutoFitBox`、要么如实标 |
| Match Skulls | `Match Skulls/Match Skulls Text` | 31.59 / 31.59 / **无 auto** / 1 / Center-Middle | 不需要接（原版没有自适应） |
| Debug 三颗 | `Debug Buttons/…/Button Text` | 38 / 12 / [12~38] / 0 / Center-Midline | 调试件（可后置） |

### 二·9 `A1213④` `Loading text` / `Progress text`（`STATIC`，手解原始 level0）

- 已导出：`assets_full/level0/GameObject/{Loading text,Progress text}.json` ⇒ `m_Component` 里 TMP 的 pid = **296/298**（Loading）与 **295/297**（Progress）；但 `level0/MonoBehaviour/` 目录里 **295–298 全缺**（类型树缺失 ⇒ 导出器跳过）。
- 原始文件：`d:/2/unity_run_ref/Warpforge_Data/level0`（另有 `d:/2/10.13游戏/…`、`d:/2/Warhammer 40k Warpforge/…` 两份同源）。
- 现成先例：`工具/read_gradient2_level0.py` —— 用 UnityPy `get_raw_data()` 取原始字节，再用**任一已导出的同类型 MB JSON**当字段顺序模板切开（`m_Script` pathID：`TextMeshProUGUI = 6627610752530350053`，现读自 `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_5890981294211439552.json`）。
- 判什么：`m_fontSize / m_fontSizeMin / m_fontSizeMax / m_fontSizeBase / m_TextWrappingMode / m_HorizontalAlignment / m_VerticalAlignment / m_fontColor` ⇒ 供 `Shell/ShellRuntime.cs:477-479` 的 `_loadingText` / `_progressText`（现在走 `TextBand` 的猜值 `SetCapHeight(TextBandH/108*0.6)`，`:549`）。

### 二·10 `A1235` 中/英字号差（`STATIC`，一处共用件）

- **普查结果（9 站）**：见主表。实现路径全部是 `SetScriptHeight(text, px, 1)`（`Battle/Label.cs:386-392`）：含汉字 → `SetGlyphHeight(px)`；否则 → `SetCapHeight(px * 0.72f)`。
- 🔴 **缺陷点 = 硬编码 0.72**：同一个文件里 `GlyphHeightWorld`（`:351-359`）自己写着「**别写死 0.72**」、并用实测 `TmpFont.WorldCapPerFontSize / WorldGlyphPerFontSize`；`NorminalPx()`（`:1127-1132`）也按 `ActualFace` 反算。⇒ 两条 `Set*` 路的 px 口径**不是靠 0.72 对齐的**，而是靠 `Wcap/Wglyph`。
- ⚠️ **仓里有两个互相打架的实测值**（都要一起订正）：
  - `Core/TmpFont.cs:232`：`0.0683 ÷ 0.72 ≈ 0.0948`（⇒ Wcap = 0.0683 ⇒ 比值 **0.7205**）
  - `Battle/Label.cs:1111` · `Battle/UnitChatPanel.cs:612` · `资料/已知的坑.md:999`：`0.0779 / 0.0948 = 0.822`
  - 字体资产 `m_FaceInfo` 现读：Noto `capLine 36 / pointSize 50 = 0.72`；Pragati `60/95 = 0.6316`；Asar `61/94 = 0.649` ⇒ **0.72 是 em 口径、0.822 是相对汉字墨高口径**，两者不是同一个分母，被混用了。
- **改法（只改一处 + 一条断言）**：把 `SetScriptHeight` 的 `px * 0.72f` 换成 `px * (TmpFont.WorldCapPerFontSize / TmpFont.WorldGlyphPerFontSize)`（**运行时实测值，单一来源**）；`SetScriptHeight` 的 doc 同时订正「0.72 em」那句。补一条**中/英字号比**断言（同一个 `px` 下，`FontSizeToPxNow` 两档相等）。
- ⚠️ 9 站里 `SliderLabel` / `_muteLabel` / `_muteOffText` / `_muteOnText` 都排在 `ApplyLangFont` **之后** `SetAutoFitBox`（顺序已对，`SettingsPanel.cs:1736-1747`）。

### 二·11 `A1853②` 14 个事件键（`REMOTE/USER`）

- 已接 2：`ExpansionPass/Title`（0 与 10）· `ExpansionPass/GeneralHelp`（12）。
- 余 14 逐条归类（判据表 `Shell/BaseEventPopup.cs:394-403`）：
  - **用户拍板不做** 2：`LiveOps/DarkZealots/{Title,Description}`（过期活动，用户边界）。
  - **缺承载窗（`T28`）** 3：`Expansion/General/{JoinTheExpansionEvent,IntroEventDescription,EndEventDescription}` —— `Parent Event Window` 未建；建完才谈得上接。
  - **缺参数通道** 1：`Expansion/ORKExp1/EventTitle` —— 现在 `SkirmishEventWindow.cs:493` 的 `EventHelpDescriptionArgs` 写死 `"20"`；这条键是 `{0}` 的实参来源，可由 `Loc.BulkOr` 取。
  - **缺页面** 5：`ExpansionPass/TabTitle`（活动页签树）· `BattlePass/{Title,GeneralHelp,EC/EventTitle,SW/EventTitle}`（BattlePass 活动窗未建；`BattlePass/SW/EventTitle` 官方快照**整条缺失**）。
  - **缺远端报价** 3：`ExpansionPass/PurchasePremiumOffer/{Title,Subtitle,Description}`。
- **能静态做的只有一条**：把 `Expansion/ORKExp1/EventTitle` 取出来填进 `EventHelpDescriptionArgs`（前提是 `{0}` 的语义确为「活动名」，需回那个 `GeneralHelp` 的实参类型确认）。其余等远端/用户。

### 二·12 `N44①` 关键词映射补齐（`STATIC`，3 条可补 / 6 条原版没有）

- 机制：`Core/Tooltip.cs:641-696` 的 `LoadOfficialTraitMap` 有三条规则（直取 / 归一化同名 / 显示名相同），推不出就落 `bodyEn`。
- **可补 3 条** —— 判据 = `trait_tips.json` 的 `bodyEn/bodyZh` 与官方 `CardTraitDescription/<枚举名>` **逐字相同**（现读，都是本地数据）：

| 我们的规范键 | `trait_tips` body（en/zh） | 官方键 | `CardTraitDescription` |
|---|---|---|---|
| `concussion` | `When this unit deals attack damage to another unit, [[stun]] it` / 同 | `concussive` | 逐字相同 |
| `darkpact` | `Give a blessing from one of the four Chaos Gods` / 同 | `markOfChaos` | 逐字相同 |
| `quest` | `Add a Secret to your deck for each 3 Quest Progress earned.` / 同 | `questPoints` | 逐字相同 |

- **原版没有 6 条**（`CardTraitDescription/*` 76 条、`Card_Trait/*` 77 条里都零命中；`trait_tips` 的 `src` 也不是 `official`）：`ability`（空正文）· `ferocity` · `huntmark` · `oath` · `pack` · `sentry` ⇒ 按例外①保留英文兜底 + 出声。
- 实现建议：在 `LoadOfficialTraitMap` 加第 4 条规则「body 归一化后唯一命中」（用 `Loc.Lang13Of("CardTraitDescription/"+name, …)`），跑一遍 `Tooltip.TraitSource` 自检。

### 二·13 `T23` 删除 `DeckRuntime` 内的重复 Import 树（`STATIC`）

- **重复关系**：
  - `Deck/DeckRuntime.cs`：`BuildImportPopup()`（`:3913-`）+ 字段 `_imp*` + `UiOpenImport()` / `UiSetImportText()` / `RefreshImportText()` / `_importOpen` 分支（`HandlePointer` / `HandleButtons` / `HandleTyping` kind 3 / `HandleEscape`）。
  - `Shell/ImportDeckPopup.cs`：外壳那份（`CollectionWindow.OpenImportPopup()` 在用，且 `CollectionScene` / `ShellScene` 均有断言）。
- **生产入口**：`DeckRuntime` 那份**没有**——`:3160-3163` 自陈两颗入口钮 2026-10-17 已删，只剩自检口 `UiOpenImport()`。
- **删它要动的断言位置（全在 `Editor/DeckScene.cs`，共 26 处引用）**：`:660`（`FindDeep` 注释）· `:724-727`（`CheckA1212` 四颗）· `:4506-4633`（导入弹窗一节：命中/正向控制/`imp_ok_t`/hover/关闭）· `:4667`（件数）· `:5825` · `:6725/6733` · `:6976`（A223 ESC 反例）· `:7283/7308` · `:7638-7677`。
- **跑哪几条自检**：`imp_*` / `UiOpenImport` **只在 `Editor/DeckScene.cs`**（grep 全 Editor 只有它）⇒ 删完 **只跑 `DeckScene.Run`** 即可。`DeckRuntime` 虽被壳侧复用，但壳侧只用到静态助手（`FactionIcon` / `CopyDeckToClipboard` / `DeckErrorText`），不碰 `imp_*`。
- ⚠️ `DeckScene.Run` 会**重写 `DeckEditor.unity`** ⇒ 跑前留副本、跑后 `git diff --numstat`。

---

## 三、没查到 / 搜过什么（逐条）

1. **`A1644` 的 quad 身份** —— 搜过 `Shell/CollectionWindow.cs` 全文（`330.9` / `331.5` / `1165` 只有 `DeckViewport`）· `MenuDraw` 的 `Rect/Nine/Tiled/Text`（无渐变族）· R1 已排除的 40 个 `Gradient2/UIGradient/UICornersGradient` 组件（`bundle_menus_assets_all`）· `CollectionWindow` 的 4 处 `40k_main_tab_shadow`。**结论：静态定位不了**，须 Unity dump `ImageQuad`。
2. **`A1227` 的复现** —— 全仓 `grep A1227 --include=*.cs` = 0；`SetWrapWidth` / `SetWrapWidthRect` / `GenerateLayout` / `TryApplyPendingWrap` / 所有 `_tmp.alignment =` 现读。**静态只能证明「当前代码没有这条路径」**，复现须 Unity。
3. **`N32` 的「攻击次数」口径** —— 本报告按「`DeclareAttack` 成功一次记一次」给落点；原版是否有「同一单位多次攻击（bloodthirst）各记一次」的细则**没查到**（搜过 `RuleCore.DeclareAttack` / `AttacksThisTurn` / `LastAttackType`，无原版计数件）。若原版计数在 `MissionChallenge` 侧，改法相同、只是口径要再核。
4. **`A1273` 登录卡显隐闸门** —— 搜过 `decomp_full/MissionsTab__CreateMissions.c` · `__InstantiateNormalMissions.c` · `__OnOpen.c`（方法体为大量 `FUN_` + `ContainerHolder__TryAdd`，无可读 `SetActive` 判据）；`MissionsTab` 24 个方法全列出、无 `LoginBonus` 闸。**判据缺**。
5. **`A1853②` 的事件→键映射** —— 需要 PlayFab Title Data 的 `EventComponents[key=0].m_AssetGUID`（`LiveOpsAssetUtility.LoadComponent`）；本地 13 个活动 SO 只有标签表本身，没有「哪个事件实例配哪组标签」的运行时数据。
6. **`N23` 的文本首选高** —— 静态能判「常量不成立」，但**具体数值依赖文案**（`Toast ▸ Text` 折行 = 1），本报告不给数值、只给公式与改法。

---

## 四、五档计数

| 档 | 条数 | 编号 |
|---|---|---|
| `DONE` | 8 | `A1927` · `A1531⑤` · `A1187` · `A1234` · `N49②` · `N49③` · `N45③` · `T27` |
| `STATIC` | 17 项（含拆分项） | `A1024③后半` · `N20` · `N21` · `N23` · `A1642` · `A1273` · `N32` · `A1213②` · `A1213③` · `A1213④` · `A1235` · `N18` · `N49①` · `N44` · `N45④` · `T28` · `T23` |
| `UNITY` | 3 | `A1644` · `A1227` · `A1488`（只差 `DeckScene.Run` 实跑） |
| `PLAYER` | 2 | `A1701` · `T09 2c` |
| `REMOTE/USER` | 1 | `A1853②`（其中「过期活动 2 键」= 用户边界不做） |

> 合计 31 项（`A1024③` 同时挂在 `A1927` 上游，算一项；`N49` 三项拆开计）。
