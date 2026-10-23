# C · 界面线（收藏与卡组 / 字体 / 外壳残余）—— 执行代理 C 报告

> 2026-10-23。**未跑 Unity、未动 git、未改正本**（`任务总表` 由主对话写）。
> 判据源：`普查产出_1022/C1_收藏与卡组_代码地图.md` · `D3_DemoDeckInfo成对表.md` · `任务线/界面线/任务总表.md` §一 ·
> 官方表 `数据/游戏数据/官方本地化_en+zh-CN.json` · 全量反编译 `d:/2/tools/decomp_full/` · 原版包 `d:/2/新解包资源/assets_full/`。

---

## ① 结论（逐件）

### 1a. `W65` · 48/78 副练习卡组缺中文名 —— ✅ **完成（根因是旧结论把「产物」当成了「源表」）**

| 项 | 结果 |
|---|---|
| 官方源表里有没有键？ | **有，而且不是 48 条而是 78/78 全有**：`PrebuiltDecks/<deckId>` 在 `官方本地化_en+zh-CN.json` 的 `terms` 里 **78 个 deckId 逐条直中，0 缺**（例 `PrebuiltDecks/ASH_SK_1_2` = `["The Cosmic Serpent","宇宙灵蛇"]`）。 |
| 旧结论「0/78 命中」错在哪 | 它查的是 **`MyGame/Assets/CardPresentation/Resources/official_names.json`（生成物）**，而那个生成器 `resolve_prebuilt_decks` **只扫 103 副池** ⇒ 产物里当然没有池外 78 副。源表一直有键。 |
| 怎么修的 | **只改生成器**（⛔ 没手改 JSON）：`工具/_loc_official/gen_official_names.py` 的 `resolve_prebuilt_decks` 增加第二个来源 `数据/游戏数据/prebuilt_decks_extra.json`（78 副，**只走 deckId 直取、不做英文名兜底** —— 这 78 副里有 8 副的 `name` 与原版 `deckName` 不同，按名兜底会静默挑到另一副），重跑生成器。 |
| 玩家看到什么不同 | **练习窗左列 78 对里，48 副的名字从英文原名变成官方中文名**（例 `AeldariDeck3`「The Cosmic Serpent」→**宇宙灵蛇**、`TauEmpireDeck1`「Target Locked」→**目标已锁定**）。另外 30 副原本显示的是**我们自己译的** `nameZh`，按「中文文案一律走官方词条表」的口径也一并换成官方值（例 `AM_SK_3_2`「合成兵种」→**联合武器**）。⇒ 156 个 deckId **156/156 都有官方词条**，中文档下 0 个回落英文。 |
| 幂等 | 连跑两遍 `--write`，`official_names.json` md5 = **`fd6e7279bf31cab8f3ca55dc05551575`** 两遍相同。 |
| 现有 103 副 0 字节 diff | `MyGame/Assets/RuleEngine/Resources/prebuilt_decks.json` md5 前后都是 **`777b8506789a1e31d40d59390093973d`**（未动）；`prebuilt_decks_extra.json` / `demo_deck_pairs.json` 也都 0 字节变化。 |
| 产物变化 | `official_names.json`：**3162 → 3240 条**，其中 `PrebuiltDecks/*` **101 → 179 条**。与旧产物逐键比：**+78 新增、0 删除、0 改值**（纯增量）。 |

### 1b. `W65` · ④ 起始卡组发放 —— ✅ **已落地（交接为准；总表那句「仍未做（需拍板）」已过期）**

- **机制**：`Shell/FTUESelectDeckWindow.OnContinueClicked`（`:524`）→ `GrantStarter`（`:566`）→ 过 `DeckRules.Validate`（`:590-597`）→ **幂等判定** `IndexOfSameDeck`（`:625`，键 = 督军 + 牌表多重集 + 模式，⛔ 不看名字）→ **`lib.Insert(pd)`**（`:608`；`RuleEngine/Data/DeckLibrary.cs:206-212`，`Insert` 内部 `return Save()` = **落盘**）→ `CollectionData.InvalidateLibrary()`（`:616`）。
- **失败态**：`:542-550` 出声且**不关窗**（= 原版 `OnError`）；`:529-537` 无牌表 ⇒ 什么都没发、出声、关窗（防死窗）。
- **可到达性**：生产入口 = `Shell/MenuTutorialFlow.cs:248`（`TutorialStageSelectArmy` 的 `Execute` 调 `WindowsManager.OpenChooseArmyDeck()`）。⚠️ **但该流程默认不自动启动**：`MenuTutorialFlow.AutoStart = false`（`:135`）/ `MenuTutorialRequested`（`:146`）⇒ 只有 `-wfftue` 才跑。⇒ **全新存档在默认启动下仍然 0 套卡组**（这是用户 2026-10-18「初次进入不自动启动」的口径，不是复刻缺口）。
- **自检已覆盖**：`Editor/MainMenuScene.cs:15496-15600` 的 ⑩ 段（四副逐个发、库 +1、牌表逐张逐序 = 原版 SO、落盘原文、幂等不重复发）—— 这就是「已落地」的独立证据。
- **结论**：**已落地**；总表可直接核销「仍未做」，⛔ 不需要再拍板「发哪一套」（四副 `*_SK_Starter` + 玩家在窗里选）。

### 1c. `W65` 其余尾巴 —— ✅ 静态能定的都做了 / 其余如实列

| 尾巴 | 结果 |
|---|---|
| 「左列格图（预组该用 `buttonImageReference` 每副一张，GUID 未解）」 | 🟢 **判据空（建议核销）**：全量反编译里 **`DemoDeckInfoSO.get_ButtonImage` 零调用点**（`grep -rl get_ButtonImage decomp_full/` 只命中它自己的 getter），`dump.cs:106321` 的 `buttonImageReference` 只在类内被 getter 读；而练习窗格子的主图在 `DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c` 里取自 **`ArmyUtilities.GetArmyIcon(...)`**（= 阵营图标，正是我们现在喂的 `pair.armyIcon`）。⇒ **原版根本不消费 ButtonImage**，不需要解 GUID。 |
| `DeckRuntime.ShareDeckString` 死代码（C1 §二 #18） | 保留（`public`、无生产入口、注释已如实写明原版的家在 `DeckInfoPopup`，那边走的是同一个 `CopyDeckToClipboard`）。⚠️ 但注释里「可自检调」不成立：**全仓 0 个调用点（连自检都没有）** —— 见 ⑤。 |
| 模式页返回链 / 练习窗默认 Skirmish / `Clear filters` 几何 / 模式筛 + 新建继承 | 现读均已在位（`CollectionWindow.cs:3441` 的 Close 钮 → `OnBackClicked:4131` → `ShouldReturnToDeckModePage:4116` → `ShowDeckModePage:4076`）。 |
| 开战取档无模式校验（C1 §三·5 的潜在风险） | 今天不缺（三个入口都先 `CollectionData.Select`）—— 属**未来新增入口的风险**，不是现存缺陷；`BattleDriver` 不在本代理白名单，未动。 |
| `Deck info` 的 `Lore Text`（C1 §二 #17） | 判据空（玩家自建卡组没有 lore 字段；原版 lore 只来自 `DemoDeckInfoSO`，那条已由练习窗显示）。 |

### 2. `T07` 字体与文本口径 —— ✅ **静态部分做完；剩 Unity / 真机**

| 子项 | 结果 |
|---|---|
| `N49` / `N54` / `N93` / `N96` | **已全闭**（判据 `普查产出_1021/W61b_界面_T07尾巴.md:16,104,105`；`N96` 里活的 `N55`/`N54②` 已于 10-18/10-19 落地）⇒ 账过期，可核销。 |
| CJK 行盒残差（`m_Scale` 0.9） | **已落地**：字体资产 `Resources/Fonts/NotoSerifCJK-Regular SDF.asset:134` 的 `m_FaceInfo.m_Scale: 0.9`；`Editor/TmpSetup.cs:171-178` 烘焙参数同改（重烘不会回 1）。 |
| **原版源 TTF 是 JP、我们是 SC** | 🟢 **静态这一半做完了**：原版源是 **TTC 4 面**（`assets_full/bundle_fonts_assets_all/Font/NotoSerifCJK-Regular.ttf`，24,810,096 B，`ttcf`）：**face0 = Noto Serif CJK JP / face1 = KR / face2 = SC / face3 = TC**；原版字体资产 `m_FaceInfo.m_FamilyName` = **Noto Serif CJK JP**（`bundle_fonts_assets_all/MonoBehaviour/NotoSerifCJK-Regular SDF.json`）。sc 与 jp 两面的**竖排度量逐值相同**（upem 1000 · hhea 1151/−286/0 ⇒ 1.437 em · OS2 cap 729）⇒ 换面**不会改行盒**，只改字形。已从 TTC 抽出 **face0** 并用 fontTools 校验（`OTTO`/CFF、65,535 字形、cmap 43,029、名字 = `Noto Serif CJK JP`；抽了本批 104 个实际用到的汉字，**0 缺字**）：**`d:/4/_tmp_view/wfC/NotoSerifCJK-Regular-JP.ttf`（23,624,316 B）**。⏭ **重烘要 Unity**（`TmpSetup.SrcTtf` + `TmpSetup.CopyCjkFont`/烘焙入口），不在本代理可跑范围 → **交主对话**。⚠️ 抽出来的是 **CFF 轮廓（`OTTO` 签名）**、扩展名仍是 `.ttf` —— 原版那个 TTC 里装的**也是 CFF**（原版 Unity 能导），所以导入器行为与现状同族；要更保险也可存成 `.otf`，但 `TmpSetup` 的拷贝逻辑按 `.ttf` 写的，改扩展名要同步那一处。 |
| `N44③`（跑一次 `SettingsScene.Run`） | ⛔ 卡铁律 12（要 Unity），本代理不跑 → **交主对话**。 |
| `N44②`（日/韩真机渲染） | 真机验收，非复刻缺口 → 交主对话。 |

### 3. `T16` 外壳运行层残余 + 新账-57 —— ◐ **能静态做的做完；余项全部要真机/Unity**

| 项 | 结果 |
|---|---|
| `N45②`（RT 名是我们挑的） | 🟢 **已落、账过期**：`Shell/UIBakeScreen.cs:74` `RTName = "RT Background bake"`（原版 `stringliteral.json` 实读）。 |
| `D10` Toast 根高 | ⛔ 缺原版真机 `rect.height`（探针方案已写、不猜数）→ 真机。 |
| `D4` 暗底姿态 | ⛔ 等原版 player 探针 → 真机。 |
| 新账-57 · `Army Icon` 70.94/155.94 vs 原版（0.04px） | ✅ **修掉**：把 **`Deck/DeckRuntime.cs:182` 的 `ArmyIconY` 从 `71f` 改成 `70.97f`**。原版真值现读 = `menu_rect.py bundle_menus_assets_all "Deck Editing Menu" --depth 7` 的 `Army Icon 1470.00 70.97 1550.00 155.97 80.00 85.00`（无 LayoutGroup，rect 权威）。⚠️ **收藏窗那颗的 70.94 是对的**（`Collection Menu Variant` 现读 `1470.01 70.94 1550.01 155.94`）—— **两棵 prefab 本来就差 0.03px，别互相"对齐"**（已在常量注释里写死这句）。玩家可见差异：0（亚像素），纯数值对齐。 |
| 新账-57 · `ExpansionPass` 出厂 INACT 的 `Help Icon Player (1)` 未现读 | 🟢 **已现读、账过期**：`Shell/ExpansionPassEventWindow.cs:389-391` 已写明它的 `EverguildTooltipTrigger.text` 实读 = **`Event_Description/Raid/IndividualTooltip`**（跨域原文，⛔ 别"修正"）。 |
| 新账-57 · 参考图「铺场流」当排序夹具先例 | 不是缺陷、是方法建议 → 无需动。 |
| `A1520` 两候选 | 静态那一半（社交残影）已在 `任务/T02_主菜单与外壳.md` §2·8 追完（`T12` 把导航「游玩」接成 `closeAll`）⇒ 只剩「壳卡死」**必须真机**。⛔ 本代理未动。 |

---

## ② 证据（`文件:行号`）

**1a · 数据链**
- 源表：`数据/游戏数据/官方本地化_en+zh-CN.json` 的 `terms`（10,854 条）；78/78 直中（本报告脚本现算，清单见下「附录」）。
- 生成器：`工具/_loc_official/gen_official_names.py`
  - `EXTRA_SRC` / `EXTRA_RES` 常量（文件头下方）
  - `resolve_prebuilt_decks()` 末段：池外循环（只走 deckId 直取 + 数据面/运行期副本漂移检查）
  - `main()`：由 4 元组改 6 元组 + 新增一行池外统计
  - 文件头「A1854」段补了池外来源的说明
- 产物：`MyGame/Assets/CardPresentation/Resources/official_names.json`（1 行 JSON；`count` 3240；`PrebuiltDecks/*` 179）
- 运行时取用：`Shell/PrebuiltDecks.cs:118-126` `DisplayName`（官方 `Loc.BulkOr("PrebuiltDecks/"+deckId)` 优先）
- 左列用名：`Shell/PracticeModePopup.cs:1784`（`PairDeck`）→ `:1883`（`deck.DisplayName`）、`:2356`（右侧详情名）
- 原版判据：`decomp_full/PrebuiltDeck__get_LocalizedName.c:15`（`"PrebuiltDecks/" + deckId`）· `il2cpp_out/dump.cs:22915-22916`（deckId @ `0x48`）

**1b · 起始卡组**
- `Shell/FTUESelectDeckWindow.cs:524` / `:541` / `:566` / `:590-597` / `:600-607` / `:608` / `:616` / `:542-550`
- `RuleEngine/Data/DeckLibrary.cs:206-212`（`Insert` → `Save()`）
- 生产入口：`Shell/MenuTutorialFlow.cs:248`；默认关：`:135` `AutoStart = false`、`:146`
- 自检：`Editor/MainMenuScene.cs:15496-15600`（⑩ 段）

**1c · buttonImageReference 判据空**
- `d:/2/tools/il2cpp_out/dump.cs:106311-106335`（`DemoDeckInfoSO` 字段表 + 仅 `ButtonImage` 属性）
- `grep -rl get_ButtonImage d:/2/tools/decomp_full/` ⇒ **只有 `DemoDeckInfoSO__get_ButtonImage.c`**
- `decomp_full/DeckSelectorMenuItemDemo__InitializeWithPracticeDeck.c`（主图 `+0x20` ← `ArmyUtilities.GetArmyIcon`）

**2 · T07**
- `Resources/Fonts/NotoSerifCJK-Regular SDF.asset:131`（`m_FamilyName: Noto Serif CJK SC`，**待换 JP**）、`:134`（`m_Scale: 0.9`）
- `Editor/TmpSetup.cs:40`（`SrcTtf` = 解包目录的 **SC** TTF）、`:171-178`（`fi.scale = 0.9f`）
- 原版：`assets_full/bundle_fonts_assets_all/MonoBehaviour/NotoSerifCJK-Regular SDF.json` 的 `m_FaceInfo.m_FamilyName = Noto Serif CJK JP`
- 原版 TTС 四面：`assets_full/bundle_fonts_assets_all/Font/NotoSerifCJK-Regular.ttf`（face0 JP / 1 KR / 2 SC / 3 TC）
- 抽出的单面 JP：`d:/4/_tmp_view/wfC/NotoSerifCJK-Regular-JP.ttf`

**3 · T16 / 新账-57**
- `Shell/UIBakeScreen.cs:70-74`（RT 名）
- `Deck/DeckRuntime.cs:176-182`（`ArmyIconY = 70.97f` + 判据注释）、`:1738`（框注释同步）
- `Shell/ExpansionPassEventWindow.cs:389-391`（`TermRaidIndividualTooltip`）
- 原版 rect：`python -I -X utf8 工具/menu_rect.py bundle_menus_assets_all "Deck Editing Menu" --depth 7`（Deck 编辑）；`"Collection Menu Variant" --depth 8`（收藏）

---

## ③ 没查清的部分（如实，⛔ 不猜）

1. **`A1520`「壳卡死」** —— 无真机，静态判不了（`任务/T02` §2·8 已这么写）。两候选里「社交残影」那半静态追到 `T12` 应已消，但**未真机复核**。
2. **`D10` Toast 根高** —— 缺原版 player 的 `rect.height`；**`D4` 暗底姿态** —— 缺原版 player 探针。都只能真机。
3. **`N44③`** —— 要跑 `SettingsScene.Run`（本代理禁跑 Unity）。
4. **JP 字体重烘后是否真像素级更像原版** —— 需重烘 + 并排验收；本代理只证明「源面确实该是 JP、度量不变、字形覆盖够」。
5. **「你的卡组」首格缺失** —— 全资料只有台账那一句、**没有判据文档**；且 `MenuDeck/HUD/YourDeck`（"你的卡组"官方键）是**战斗 HUD** 的（`普查产出_1022/W62_战斗非原版元素清理.md:167`）⇒ 怀疑这条**归错线**，但**没查清**（未追代码）。
6. **8 副官方英文名与 SO `deckName` 不同**（`AstraMilitarumDeck6` / `DarkAngelsDeck6` / `GenestealersDeck1` / `GenestealersDeck3` / `SpaceWolvesDeck2/4/6` / `SW_SK_2_2`）—— 其中数条看上去是**另一副的名字**（与总表里池内那 4 副是同一族现象）。按既定口径（官方键 = 原版 `LocalizedName`，SO 的 `deckName` 是死字段）**以官方表为准**；但原版已关服、**无实拍可验**，如实记账待实拍。
7. **`official_names.json` 不含 4 副 `*_SK_Starter` 的键**（源表有：舰队之主 / Da红色Waaagh / 地狱之矛突击 / 无情的行军）—— FTUE 发放走的是 `Loc.Lang13Of`（`FTUESelectDeckWindow.cs:575-579`）能取到，**今天无可见影响**；但 `Loc.BulkOr` 这条路取不到 ⇒ 若将来有 UI 用 `PrebuiltDecks.ById()` 查起始副会回落英文。**没改**（不在本批口径里，避免给生成器开第三个来源）。

---

## ④ 改动清单（逐文件 + 一句话）

| 文件 | 改动 |
|---|---|
| `Unity/工具/_loc_official/gen_official_names.py` | `PrebuiltDecks/*` 的生成期 join 增加**池外 78 副**来源（`数据/游戏数据/prebuilt_decks_extra.json`，只走 deckId 直取）；新增数据面/运行期副本漂移检查；`main()` 多打一行池外统计；文件头补 2026-10-23 说明。 |
| `Unity/MyGame/Assets/CardPresentation/Resources/official_names.json` | 重跑生成器：3162 → **3240** 条（`PrebuiltDecks/*` 101 → **179**），**纯增量**（旧 3162 条逐键 0 改）。 |
| `Unity/工具/gen_practice_decks_extra.py` | 订正末段那条**过期注释/打印**（原文「官方名表也没有」是错的；现读 78/78 有）；逻辑未动，`--check` 仍通过（三份产物 0 字节变化）。 |
| `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | 练习窗段新增 **2 条断言**（+49/−0）：① 78 对 × 两侧 156 个 deckId **全部**取得到官方词条；② 中文档下这 156 个名字**全部含汉字**。🧨 改坏法写在注释里（删池外 join ⇒ 48 条红）。 |
| `Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs` | `ArmyIconY` **71 → 70.97**（原版 prefab 真值，`+10/−2` 行，含判据注释 + 同步 `:1738` 的框注释）。 |
| `d:/4/_tmp_view/wfC/NotoSerifCJK-Regular-JP.ttf`（**未入工程**） | 从原版 TTC 抽出的 **face0 = Noto Serif CJK JP**（23.6 MB，fontTools 校验通过）—— 供主对话改 `TmpSetup.SrcTtf` + 重烘。 |
| `Unity/资料/普查产出_1023/C_界面线收藏与卡组.md` | 本报告。 |

**验过的硬约束**
- `prebuilt_decks.json` md5 前后 = `777b8506789a1e31d40d59390093973d`（**0 字节 diff**）。
- `prebuilt_decks_extra.json` / `demo_deck_pairs.json` md5 与基线一致（`6a3749f2…` / `25390a7c…`）。
- `official_names.json` 两遍 `--write` md5 相同（`fd6e7279…`）。
- 行尾：`DeckRuntime.cs` 仍全 CRLF（8623/8623）；`MainMenuScene.cs` 仍全 LF；两个 `.py` 全 LF。
- typecheck：改完 `MainMenuScene.cs` 那次 **0/0**；之后两次运行出现 **别人的文件** 的错（`Core/VfxMap.cs` 4 条运行时 + `WarpforgeArena1/Editor/ArenaAnimsProbe.cs` 10 条编辑器）——**全部集中在不是我负责的文件**（`VfxMap.cs` 是白名单明令禁碰的），按简报口径**不是我需要改的**，如实记录。我这两个文件没有出现在任何一条错误里。

---

## ⑤ 顺手发现（只报不改）

1. **`Core/Loc.cs:3044-3047` 的条数注释过期**：仍写「`Resources/official_names.json` = **3,156 条 / 9 族**」与 `Event_Description 144 · Card_Trait 27` 的分项；现读 **3,240 条**（`PrebuiltDecks/*` 101→179）。`Core/CardIcons.cs:698` 同样写「九个族、共 3156 条」。两个文件都不在本代理白名单 ⇒ **请主对话订正**。
2. **`Shell/MenuTutorialFlow.cs:486` 注释过期**：「我们这一版 `FTUESelectDeckWindow` 的 `Continue` **不发卡**（只出声 + 关窗，见该窗文件头）」—— 现在 `Continue` **会发卡并落盘**（`FTUESelectDeckWindow.cs:566-621`），且 `SelectArmy` 阶段的完成判据正是靠「窗被关掉」。注释该改。
3. **`Shell/WindowsManager.cs:776-779` 文档过期**（`PrefabRefChooseArmyDeck` 那条 doc）：「⚠️ **本地没有生产入口**：原版是 FTUE onboarding 的一环，而我们**没有菜单版教程状态机** ⇒ 这里只提供怎么开」—— **不成立**：`Shell/MenuTutorialFlow.cs:248` 就在开它（`TutorialStageSelectArmy`），而且那条流程默认关（`AutoStart=false`）；`Shell/FTUESelectDeckWindow.cs:88` 自己也已就地订正过这一句，但 `WindowsManager` 这份没跟着改。
4. **`DeckRuntime.ShareDeckString()` 的 doc 里「（`public`，可自检调）」不成立**：全仓 **0 个调用点**（`grep -rn ShareDeckString CardPresentation/` 只有定义与注释）。方法本身保留没问题（原版的家在 `DeckInfoPopup`，那边走同一个 `CopyDeckToClipboard`），但「可自检调」这句该删。
5. **`DemoDeckInfoSO.buttonImageReference` 在原版是死数据**（见 ①·1c）：`get_ButtonImage` 全量反编译**零调用点**。总表 §一 顶部「仍未做」那条「左列格图（预组该用 `buttonImageReference` 每副一张，GUID 未解）」建议**直接核销为「判据空」**，别再派人解 GUID。
6. **`*_SK_Starter` 四副不在 `official_names.json`**：源表有官方名（舰队之主 / Da红色Waaagh / 地狱之矛突击 / 无情的行军），但生成器的两个来源（103 池 + 78 池外）都不含它们；FTUE 走 `Loc.Lang13Of` 能取到 ⇒ 今天无可见影响。要收口的话，给生成器加「FTUE 四副」来源即可（本代理未改，避免开第三个 join 口）。
7. **`PrebuiltDecks.Pairs` 里那 8 副官方英文名与 `deckName` 不同**（见 ③·6）：与总表已记的池内 4 副同一族（`DarkAngelsDeck3` 等），建议实拍核对时**并把这一批一起看**。
8. **`official_names.json` 是单行无尾换行**（生成器 `newline="\n"` + `separators=(",",":")`）—— 这次按原样写，未引入格式 diff（`git diff --numstat` = `1/1`）。

---

## 附录 · 48 副（实为 78 副）官方键抽样（全部 78 条以脚本现算，节选）

```
AeldariDeck3          The Cosmic Serpent      → 宇宙灵蛇
ASH_SK_1_2            The Cosmic Serpent      → 宇宙灵蛇
AstraMilitarumDeck2   Armoured Regiment       → 装甲团
AM_SK_2_2             Wall of Muscle          → 肌肉墙        （classic 侧官方作「肌肉壁垒」）
SautekhDeck3          Implacable Conqueror    → 不屈的征服者  （skirmish 侧官方作「坚不可摧的征服者」）
UMDeck8               Master Tactician        → 战术大师
TauEmpireDeck1        Target Locked           → 目标已锁定
OrksDeck3             Goff is Da Best         → 戈夫是最棒的
```
⚠️ 同一对的两侧**官方译文可能不同**（上面 `AM_SK_2_2` / `SAU_SK_1_2` 两例）—— 那是**原版两个 deckId 各自的 `LocalizedName`**，⛔ 不是抄错。
