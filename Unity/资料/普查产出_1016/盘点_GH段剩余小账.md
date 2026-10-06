# 盘点 · G/H 段剩余小账（只读现核 · 2026-10-16）

> 只读普查（子代理 C 交回，**主对话代落盘** —— Explore 代理无写文件权限）。
> 口径：**每行都回工程现核**，写「账上写 X / 实测 Y」。行号会漂，锚点按「文件 + 句子 / 节点名」。
> 🔴 **本轮最值钱的元发现**：G/H 两段的「状态列」至少 **9 处过期** —— A191 / A192③ / A544 / §三第2条的 `ArmyRowH` 与卡背行序 / A821 / A822 / A826(部分) / 两处悬空指针 **实测都已收**。**别照抄状态列。**

---

## ① 主表（按编号排序 · 5 格）

| 编号 | ①定义 | ②现状（现核） | ③要碰的文件（全） | ④规模 / 前置 | ⑤能否立刻做 |
|---|---|---|---|---|---|
| **A191** | 战场补 4 个缺的宿主对象（改 `ArenaBuilder`） | ✅ **已做完**（账上写「仍开着」= 过期）。`ArenaBuilder.cs:2494/2503-2512`（节头 + 4 个宿主的清单）· `:2558 ApplyGroupNodes` · `:2640` 日志行；`Editor/BattleScene.cs` 命中 `A191` **14 处**（含 4 条 `★ A191`）；**硬判据** `d:/4/Unity/数据/游戏数据/env_blendables.json` 的 `_missingTargets` = **0 条**（`工具/gen_arena_groups.py:691-692` 明写「A191 把 4 个宿主建出来之后…实测 0 条」）；`历史/A表已收口_1011.md:134` 记「2026-10-11 W11 做完并实测通过」 | 无（销账时改 3 行正本：`项目任务.md:423` · `战场场景线_交接.md:650/703`） | 0 改动 | **小件·今天能完**（销账） |
| **A192③** | `particleSystemAreaSpawners[]` 嵌套旁挂 | ✅ **判据已被推翻 + 已收口**。`待办判据_1007.md:58` ③：「真字段名是 `particleSystemAreaSpawners`（旧记录少了 `Area`）⇒ 全仓早有命中、生成器早在 A137 就收了…已由 W11 收口到 `target_fields` 一处、产物逐字节不变」⇒ 账上（`战场场景线_交接.md:650`）「仍没做」= 过期 | 无（销账 1 行） | 0 改动 | **小件·今天能完** |
| **A345-T-b** | 判据「没对上 **0** 条 ×13」 | 🔴 **还开着**。`d:/4/_tmp_view/arenaprefabs_1014.log` = **13 条**「没对上 **1** 条 `target BoardCamera`（旁挂 pos `(100.000,2.222,-13.572)`）」· `arenabuild.log` 同 13 条。**无任何断言读 `LastGroupNodeMissed`** | `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`（`ApplyGroupNodes`/`FindBuilt`/`ResolveGroupParent`）· 13 份 `Assets/WarpforgeArena1/arenas/<场>/<场>_groups.json` · `工具/gen_arena_groups.py` · 若要落断言 → `Assets/CardPresentation/Editor/BattleScene.cs` | 1 处根因待裁（BoardCamera 那条 target 的父链口径 / 位置口径，`A345` 已知盲区）＋**跑 Unity**（重建 13 场 → 必跟一次 `BattleScene.BuildAndSaveScene`） | **中件 / 要等条件**（口径待裁） |
| **A544** | 商品格图标（黑石/票券/金币三张） | ✅ **两条腿都跑完了**（账上写「只差 Unity 腿」= 过期）。盘上：`Resources/Art/ui_menu/{40K_general_icon_currency_blackstone,40K_icon_ticket_bundle,40k_general_icon_currency_gold}.png` **都在**（mtime 10-06 18:26，同批共 **284** 张 = 一次 `--only-menu` 批量导入）· `Shell/ShopData.cs:213-217` 三 id 已改 · `Editor/ShopScene.cs:1693-1733` 六条断言在位 · **`d:/4/_tmp_view/shop.log`** 头 `Date: 2026-10-06T10:52:52Z`（= 本地 18:52，**晚于**导入腿 18:26）· `-executeMethod ShopScene.Run` ⇒ **`[Shop] === 合计：2164 通过 / 0 失败 ===`**，全文件 `✗` 命中 = **0**，`★②/★③` 均 ✓（`:19578` / `:19588`） | 无（销账） | 0 改动 | **小件·今天能完** |
| **A562** | 仓库里 `wf_shaders_extra.bundle` 是旧的，「要不要重打」口径未定 | 🔴 **还开着**。盘上现读 `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle` = **1338640 B / md5 `ecebd2f811a8389bab405940ecfe6e07` / mtime Sep 11 12:29** —— 与账上一字不差（工具跑出来的 = 699808 B / `645fe8370277`）。⚠️ 该路径被 `.gitignore:33` 排除（不进仓库，干净克隆上不存在） | `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle` | 1 次重打；**前置 = 先只读查清「有没有消费方依赖 1.3MB 版」**（本轮没查） | **中件**（先只读查消费方） |
| **A591** | `ArenaBuilder.ApplyGroupNodes` 出声措辞收口 | ✅ **已收口**（W9）。`ArenaBuilder.cs:2667-2676 GroupNodeMissHelp()` 现读 = 「🔴 **既有的、预期的 —— 不是新缺陷**」+ 实际签名 `target BoardCamera`/pos + 「判据 A345-T-b「没对上 0 条×13」**尚未达成**」+ **反向判据**（名字/pos 不符 ⇒ 那是新的）。⚠️ **仍一条断言都没有** | 无（销账） | 0 改动 | **小件·今天能完** |
| **A678** | `Shell/ItemDrawer.cs` 的 `align: 2` 没查证原版 | 🟡 **还开着（判据不齐）**。`Shell/ItemDrawer.cs:1233` 现读 `ClippedText(node, qr, "x" + qty, …, st, 2)`（最后一实参 = align 2 = 右）· `:1254` 注释自陈「2 = 右（原版 TMP 的 `m_HorizontalAlignment`）」。**原版 `ItemDrawerComponents.Quantity` 的 `对齐=` 全库从未读过**（判据只在 `d:/2` 解包） | `Shell/ItemDrawer.cs`（1 处，改不改取决于判据） | 1 处；**前置 = 读 `d:/2` 的解包 prefab**（本轮⛔ 没动 `d:/2`） | **要等条件**（判据在远端） |
| **A679** | `GameWindow.Text` 的 align **全表**要核 | 🟡 **还开着（判据齐、量大）**。口在 `Shell/WindowsManager.cs:375`（`…, int align = 0, float autoMaxPx = 0f, float autoBasePx = 0f`，`:395-396` 两档实装）。账上已核 **6 处真传 align**（A635 两处已改 · `CampaignRewardWindow` 的 `Warning`（现读 `:712-720` 用命名实参）· `RewardWindow:1075/1104/1111` · `InboxWindow`）。**「没传 align」那批的准确条数没查清** | 全部 `Shell/*.cs` 的 `GameWindow.Text` 调用点 | ⚠️ 准确调用点数**没查清**（`Shell/*.cs` 里 `\.Text(` 命中 31 行，但混了 `MenuDraw.Text` 一族，未按接收者类型筛） | **中件** |
| **A680** | `普查产出_1013/WD3_领奖弹窗补建.md` 仍写「只建 6 块」、实测 2 块 | 🔴 **还开着（纯文档）**。`普查产出_1013/WD3_领奖弹窗补建.md:339` 现读仍写「…中段宽 0 ⇒ **只建 6 块**」；判据 `Battle/ImageQuad.cs:481-493` —— `if (w <= 0f \|\| h <= 0f) continue;` 逐行 ⇒ 只剩 `(0,1)/(2,1)` = **2 块**（`wl+wr = 730 > 595.30` ⇒ 中段宽 0） | `资料/普查产出_1013/WD3_领奖弹窗补建.md`（:339 一行 + 留订正痕） | 1 行 | **小件·今天能完**（零前置、零 Unity） |
| **A799** | 给 R2 查出的 **15 处新裁点**补断言/注释 | 🟡 **还开着**。R2 全量表 199/199 已跑完（`普查产出_1015/R2_A799全量表.md` §一；会新裁 **15** = 生产 9 + `Editor/` 夹具 6）。**现核**：`grep -rn "A799" Assets/**.cs` = **仅 2 命中**，全在 `Shell/SocialWindow.cs:345-346`（那是 A822 落的注释）⇒ **15 处一处都还没补**。<br>🔴 **2026-10-16 就地订正（铁律 5 · W6 查出的）**：本行下面「夹具 6 = 6 个文件各 1」**是错的** —— R2 自己的表（`:16-20` / `:60-65`）写着 **Editor 侧那 6 处全在 `Editor/MainMenuScene.cs`**（`:8712/:8713/:8715/:8770/:8777/:8782`，锚 `a781Node`），**没有** CollectionScene / ShellScene / ShopScene 行；而且**那 6 处 W3 已于 2026-10-16 做完**（`W3_MainMenuScene四笔.md:17,74-80` → `MainMenuScene.cs:8992-9002`）。**错因** = 我把 R2 的「6 处」摊成了「6 文件各 1」（同文件 §④-6 自己就写着那 6 处行号「没抠出来」）。⇒ **本件真开的只剩生产 9 处**（W8 在做）。 | 生产 9：`Shell/PurchasePremiumWindow.cs`(×2) · `Shell/RankedRewardEventWindow.cs`(×1) · `Shell/LeaderboardRow.cs`(×4) · `Shell/MatchLogRow.cs`(×1) · `Shell/SocialWindow.cs`(×1) · 夹具 6 在 `Editor/{BattleScene,MainMenuScene,ShellScene,CollectionScene,RewardsScene,ShopScene}.cs` | 15 处**纯注释/断言**（⛔ 不改行为 · 新裁是目的）；夹具 6 处宿主热点 | **小件（注释半今天能完）/ 断言半需宿主窗口** |
| **A821** | 同一颗字裁两刀 ⇒ 诊断计数虚高 | ✅ **已做**（W24）。`Battle/Label.cs:551-583`（`TakeClipFailMark(bool)` 两档凭据）· `Shell/MenuDraw.cs:1042/1212`（两处计数点改走它）· `:1056/1081` 口径注释。残留两笔（W24 §4·2/§4·3 自陈）：① 口径**没有独立牙口**；② 三处旧注释未订正（`资料/已知的坑.md:4308` · `Editor/MainMenuScene.cs:2065` · `Shell/SocialWindow.cs:351`） | `资料/已知的坑.md` · `Editor/MainMenuScene.cs` · `Shell/SocialWindow.cs`（各 1 句） | 3 句注释 | **小件·今天能完**（挑③） |
| **A822** | `SocialPage.Text`/`Hit` 全文件零 `ClipText`，三个使用者都有 `ViewportClip` | ✅ **已做完**（2026-10-16 W19 注释 + W20 断言）。`Shell/SocialWindow.cs:343-356`（`Text` 段：明写「本口**不**自己调 `ClipText` 是有意的，⛔ 别补」，理由 + 缺哪条）· `:402-406`（`Hit` 同族）· 断言在 `Editor/MainMenuScene.cs:5989-6067`（新增局部量法 `A822SpanX` @`:6010` + 跨右沿探针 + 3 条，`:6058/6065`） | 无（销账） | 0 改动 | **小件·今天能完** |
| **A823** | 卡侧 5 个 `AnimFXModuleCollisions.collisionEvent` 有没有订阅者 | ✅ **问题已答**（并已并入 A828）。现核 `grep -c collisionEvent d:/4/Unity/数据/游戏数据/animfx_modules.json` = **0** ⇒ 那 5 颗是**空事件**（后半已答）。代码侧只剩 `Battle/AnimFXController.cs:111` 一句注释 | 无（销账） | 0 改动 | **小件·今天能完** |
| **A824** | `animfx_modules.json` 的 `Scenario` 效果进不了运行时 | 🟡 **记录型（口径已定，⛔ 不是待办）**。现核 `Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 里 `name: Scenario` = **0**（`Scenario` 字面 9 次是别的东西，未逐条读）；`animfx_modules.json` 里 `Scenario` 19 次 | 无 | —— | **销账 / 标「记录·别动」** |
| **A825** | `CheckAbsorbRule` 5 份副本收口 | 🟡 **还开着（4/5）**。已收 4 个宿主转调 `Shell/MenuDraw.cs` 的 `CheckAbsorbRule`（`Editor/CollectionScene.cs:54-66` 包装、`:2186` 等）· 🔴 **`Editor/MainMenuScene.cs` 那一份没动**：`:73` 仍是自己那份实现、`:190` 仍是自己那份 `EdgeInset`、**13 个调用点走老路**。`Shell/MenuDraw.cs:2536-2541` 自陈「**还没收口，仍读自己那份 `EdgeInset`** —— 下次收那一份时把它一并删掉」 | `Editor/MainMenuScene.cs`（`:73-187` 换包装 · `:190` 删 · `:2059-2069` 注释改「唯一标签数」口径） | 1 文件 3 处；⚠️ 该文件是**热点**（A799/A822/A821 都盯它） | **小件但需排活**（A 表清空前该文件只一个写手） |
| **A826** | H3 §C 那 6 条断言从未接线 | ✅ **已接上**（W19）。`Editor/RewardsScene.cs:8246+` 新段 + **38 条**（①排期 2 · ②排期值 7 · ③④+§D 20 · ⑤音高 7 · ⑥取不到素材出声 2）；§C③ 的 `dt` 就地订正（0.05→0.10→0.75 三步）。**`d:/4/_tmp_view/rewards.log` = 1917 通过 / 0 失败** ⇒ 看起来已跑过且绿 | 无（销账） | 0 改动 | **小件·今天能完** |
| **A827** | A496⑨ 的文案过期（写「今天素材未导 ⇒ `Ready == false`」） | 🔴 **还开着（纯文案）**。`Shell/RewardWindow.cs:399` 现读仍写「⚠️ **素材还没进工程**…」；`:410-411` 仍写「⚠️ **它不在 `Resources/animfx_sounds.json` 那张表里**（那张表只收 AnimFX 的 `sounds[]`，**408 条**里没有它）」—— 实测该表**已含**它（`Assets/CardPresentation/Resources/animfx_sounds.json` 里 `Reward open item by item` 命中 **1**）；`:539` 那句「素材还没进工程」同族 | `Shell/RewardWindow.cs`（`:399-401` · `:410-411` · `:537-542`） | 2–3 处注释 | **小件·今天能完** |
| **悬空指针**（2 处） | `AllianceMemberTab.cs:1362` / `FriendsTab.cs:258` 写「先例 → `SocialPage.SetClip` 那段注释」而那段已被 A753 删 | ✅ **已改指**（W14）。现读两处都写：「⚠️ **2026-10-15 就地订正（W14 · 铁律 5）**：…**那段注释已随 A753 = A744 的『全删』整体删掉** ⇒ 指针悬空。改指**还在的那一份**：`资料/已知的坑.md` 的『`internal` 在自检里用不了』那一节…」 | 无（销账） | 0 改动 | **小件·今天能完** |
| **§三 第2条** | 卡组编辑 5 件残件 | **逐件现核**：<br>· `ArmyRowH` 内容高 **550** = ✅ **已做**（`Core/FilterPanelModel.cs:57-61` `ArmyContentTop(50) + rows×ArmyCell(100)` · `:204-205` · 两扇窗共用 `:76`/`:601 CosmoArmyRowH` ⇒ 13 格 5 行 = **550**）<br>· `FltCell`「九字段对拷」= 🟡 **还开着**（`Shell/CollectionWindow.cs:216-260` 的 struct 现读 **17 字段**，两处逐字段对拷 `:1884` / `:1905`；账上「九字段」是旧数）<br>· `Cosmetic Drag Controller` = 🟡 **还开着（有意不建）**（`Deck/DeckRuntime.cs:1325` 明写「100×100 + `scl 0.6` 的 250×405 预览）⇒ **先不建**」）<br>· `String.Contains` 第三实参（3/5）枚举映射 = 🟡 **判据不齐**（出处 `资料/卡组编辑界面_查证_0920.md:438`：BCL 未 dump ⇒ 只能推断）<br>· 卡背那扇窗行序（Army 在前）= ✅ **已做**（`Core/FilterPanelModel.cs:595` + `:632-634` · `Shell/CollectionWindow.cs:992/1898`） | `Core/FilterPanelModel.cs`（模型）· `Shell/CollectionWindow.cs`（FltCell 去重）· `Deck/DeckRuntime.cs`（若做卡背拖拽预览；正本 `资料/卡组编辑界面_查证_0920.md`） | FltCell 彻底去重 = 把画图那段也搬进模型（**另开一轮**）；`Cosmetic Drag Controller` = 新组件；`String.Contains` = 需 BCL dump | **中件**（FltCell）· **要等条件**（`Contains`）· **大件**（拖拽预览） |
| **§三 第5条** | 阵营资源：2D 回退路径没断言 | ✅ **2026-10-16 已做完**（W19：5 条断言落 `Editor/BattleScene.cs:6001-6050`）。🔴 **本行原文的触发条件写错了**（W19 现核 · 铁律 5 就地订正）：**不是 `use3DBoard == false`，是 `_body3D == null`** —— `BuildBody3D` 在 `CardView.cs:1429`/`:640` **无条件调**（`use3DBoard` 只管 `PoseFor` 走哪套坐标 `:6972`、挂不挂 `ArenaLayer` `:7099`）⇒ **关掉 3D 战场照样建 3D 卡体**、那一支不会因此现形；批处理里网格/shader 永远都在 ⇒ 只能加**诊断开关** `CardView.DebugNoBody3D`（`:1922-1936` + `:2654` 那道门，产品恒 false）造出那一态。`CardView.cs:754-761` 的错注释**已就地改掉**。**原记（留作历史）**：🟡 还开着 · `Core/CardView.cs:749-763 SetRemnantBody` 的 else 支现读仍在（`:759` 注释自陈「`use3DBoard == false` 那种配置下才现形 ⇒ 一次都没走到」）；开关落点 `Battle/BattleDriver.cs:44/6972/7099` · `Editor/BattleScene.cs:13665` | `Core/CardView.cs`（被验）· `Editor/BattleScene.cs`（诊断开关 + 断言宿主） | 诊断开关 `CardView.DebugNoBody3D`（产品恒 false）+ 5 条断言（已落） | ✅ **今天能完 → 已完成** |
| **§三 第6b条** | `Company Master` 的 `it` 指代时序（广播早于记账） | 🟡 **还开着（真 Play 项）**。出处 `普查产出_0916/静默桩家族_0916.md:157`：「**按代码推定**，未跑实局」。代码侧无 `Company Master` 命中（只在 `资料/卡牌数据表/*` 里，卡 284，Dark Angels 步兵 rare，`When you draw a card, it costs 1 less this turn`） | 无（要跑实局） | 1 局真 Play | **要等条件** |
| **§三 第9条 · 3 件** | `X N.mat` 副本 · `ChatButton` 两组件 · `TransformScalerBySmallScreenUI` 横向安全区 | · `WarpforgeVFX/Materials/` 的 `X N.mat` = ✅ **2026-10-16 已根治（W20 查证 + W21 落刀）**：🔴 **本行原文两处错**（铁律 5 就地订正）：① **产地不是 `工具/import_original_art.py`**（那脚本亲数 `Material`/`.mat`/`ImportClip` 全 0，它导的是卡图/卡背/立绘）—— 真产地 = **`Assets/WarpforgeArena1/Editor/EffectExporter.cs` 的 `ImportMaterial`**（病灶 `GenerateUniqueAssetPath`+`CreateAsset`），**已在 `:1551-1586` 改成确定路径 + `CopySerialized`**；**同一个病还在 `ImportMesh`**（`Meshes/` 有 164 个 `X N.asset`），**也已在 `:1922-1952` 一并改掉**；② **判据「>0 就没清干净」作废** —— 原版**本来就有**以数字结尾的材质名（亲扫 1092 个 Material JSON 证实 **10 个**：`Lens Flare 1`/`Glow Sphere 01`/`Flare 3`…）⇒ **新判据 = 「形如 `X N.mat` 且同目录存在 `X.mat`」**，今天 = Materials **730 文件 / 190 基名** · Meshes **102**。⚠️ **另两条如实标**：(a) **撞名是真代价** —— `X N.mat` 里 **107 文件 / 18 名字内容确实不同**（`Embers` 24 份/3 种…），确定路径 = 「后到赢」，今天只做到**出声不静默**（`GuardMatName`/`GuardMeshName`），**要真复刻需给每个不同源各自的确定路径（判据未定）**；(b) **网格那条路还欠实跑**（`CopySerialized` 落到已有网格资产上没验过；跑完导出器要**回读** `_typelessdata` 查 NaN/Inf）。**原记（留作历史）**：🔴 还开着 · 现读 718 个 · 判据「>0 就是还没清干净」；「`Resume=false` 全量重导能清干净」已被实测推翻<br>· `ChatButton` 那对象上两个组件并存 = 🟡 **真 Play 项**（`待办判据_战场与战斗视图.md:655`）<br>· `TransformScalerBySmallScreenUI`(menuScale 1.3) 与 `UISafeAreaManager` 横向安全区 = 🟡 **真 Play 项**（同上 `:656`） | `工具/import_original_art.py`（改 `ImportMaterial` 为「确定路径 + `CopySerialized` 原地覆盖」，照 `ImportClip` 写法）· 其余两件无文件 | 1 处生成器改法（⛔ **别手删**，没逐个核过引用）· 2 件真 Play | **中件**（mat 副本）/ **要等条件**（两件真 Play） |
| **§三 第26条 · 3 条** | ④ 发动效果时长 · ⑥ 逐卡 VFX 两处 · ⑨ 两处推导值 | · **④** = 🟡 **还开着，但没有独立可做的部分**：`Core/EventTiming.cs:108` 现读 `case EvtKind.Ability: return 1.0f;`，`:105-107` 自陈「**1.0 仍是临时中值**…属 ⑥ 那一批」。100% 长在 ⑥<br>· **⑥** = 🟡 **还开着两处**：① `playOnEnable = 0` 的 **84/138** 个实例没人调 `PlayAnim()`；② 造数据那半卡在**远端 CCD**<br>· **⑨** = 🟡 **真 Play 项**（`真Play待验清单.md` **D23**）：`Core/CardFeel.cs:121 HitRotLightDeg = 3f // (-3,-3,0) 的模长`（模长实为 **4.243** ⇒ 「取模长」站不住，3 更像最大分量）· `:236 ShakeWorldAmplitude = 0.04f` | ④⑥：`Core/EventTiming.cs` · `Core/UnitTweenTable.cs` · `Core/UnitTweenRuntime.cs` · `Resources/UnitTweens.json` · 需原版 `AnimInfo`（远端）；⑨：`Core/CardFeel.cs` | ⑥① = 建动画事件层；⑥② = 远端数据；⑨ = 真 Play | **要等条件**（三条都是） |

**小结（现核后的真相）**：23 行里 **9 行已收（只欠销账）** · **4 行纯文案/文档**（A680 · A827 · A825 · A821 残留）· **真还开着的技术账 = A345-T-b · A562 · A679 · A799 · §三第2条(FltCell/Cosmetic/Contains) · §三第5条 · §三第9条(mat 副本) · §三第26条**。

---

## ② 「文件 → 被哪几条盯」

| 文件 | 盯它的条目 |
|---|---|
| `Editor/MainMenuScene.cs` | **A825**（:73/:190/13 调用点 · 唯一没收的那份）· **A799**（夹具 6 处之一）· **A821**（:2065 注释口径）· **A822**（:5989-6067 已收） |
| `Editor/BattleScene.cs` | **A345-T-b**（若要落断言）· **§三第5条**（`use3DBoard==false` 夹具）· A191（已收） |
| `Editor/RewardsScene.cs` | **A826**（:8246+ 已收）· **A821**（计数实证处） |
| `Editor/ShopScene.cs` | **A544**（:1693-1733 已收） |
| `Shell/SocialWindow.cs` | **A822**（:343-406 已收）· **A799**（:367 一处新裁）· **A821**（:351 注释） |
| `Shell/MenuDraw.cs` | **A825**（:2536-2541 `AbsorbEdgeInset`）· **A821**（:1042/:1212 已收） |
| `Shell/RewardWindow.cs` | **A827**（:399-401/:410-411/:537-542） |
| `Shell/{LeaderboardRow,MatchLogRow,PurchasePremiumWindow,RankedRewardEventWindow}.cs` | 全是 **A799** 的「会新裁」生产落点（⛔ 别改行为，只补注释/断言） |
| `Shell/ItemDrawer.cs` | **A678**（:1233 align:2） |
| `Shell/WindowsManager.cs` | **A679**（:375 `Text(…, int align = 0, …)`） |
| `Core/FilterPanelModel.cs` · `Shell/CollectionWindow.cs` | **§三第2条**（ArmyRowH 已收 · FltCell 对拷还开 · 卡背行序已收） |
| `Core/CardView.cs` | **§三第5条**（`SetRemnantBody` 2D 回退支） |
| `Core/CardFeel.cs` · `Core/EventTiming.cs` | **§三第26条 ⑨ / ④** |
| `Battle/ImageQuad.cs` | **A680**（判据源 :481-493，只读不改） |
| `Battle/Label.cs` | **A821**（`TakeClipFailMark` 已收） |
| `Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | **A591**（已收）· **A345-T-b** |
| `Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle` | **A562** |
| `Assets/WarpforgeVFX/Materials/*.mat` | **§三第9条**（718 个 `X N.mat`） |
| `工具/gen_arena_groups.py` · `gen_env_blendables.py` · `gen_animfx_modules.py` · `dump_animfx.py` · `import_original_art.py` | A345-T-b · A192③（已收）· A828（用户已裁「做」）· A544（已收）· §三第9条(mat 副本) |
| `资料/普查产出_1013/WD3_领奖弹窗补建.md` | **A680** |

---

## ③ 「能立刻做的小件」候选（按 改动小 + 无前置 排序）

| # | 条目 | 改动 | 前置 | 备注 |
|---|---|---|---|---|
| 1 | **A680** | 1 行（`WD3:339` 「6 块」→「2 块」+ 订正痕） | 无 | 零 Unity、零解析 |
| 2 | **A827** | 2–3 处注释（`Shell/RewardWindow.cs`） | 无（先 grep 一眼 `animfx_sounds.json`） | 纯文案 |
| 3 | **A544** | 0（销账） | 无 —— 证据已在 `shop.log` | **2164/0 已证** |
| 4 | **A191 / A192③ / A822 / A591 / A826 / A823 / 两处悬空指针** | 0（各销账 1 行） | 无 | 正本共约 **6 处过期行** |
| 5 | **A821 残留③** | 3 句注释（`已知的坑.md:4308` · `MainMenuScene.cs:2065` · `SocialWindow.cs:351`） | 无 | 前两处在热点文件 |
| 6 | **A825 第 5 份** | 1 文件 3 处（`Editor/MainMenuScene.cs`） | 无，但**该文件是热点 ⇒ 要排活** | 换包装 + 删 `:190 EdgeInset` + 改口径注释 |
| 7 | **A799 的「注释」半** | 15 处纯注释 | 无 | 夹具 6 处宿主热点 |
| 8 | **A679 的只读普查** | 0（先出清单） | 无 | 适合派**只读代理**，零独占文件 |
| 9 | **A678 的只读查证** | 0（先出判据） | ⚠️ **要读 `d:/2` 的解包 prefab** | 与 A650 同族「要动 d:/2 ⇒ 留专门一趟」 |

---

## ④ 没查清的（如实）

1. **A679**：「没传 `align`」那一批调用点的**准确条数**没查清（`Shell/*.cs` 里 `\.Text(` 命中 31 行，混了 `MenuDraw.Text` 一族，未按接收者类型逐个筛）。
2. **A678**：原版 `ItemDrawerComponents.Quantity` 的 `对齐=` 到底是多少 —— **没查清**（判据在 `d:/2` 解包资源里，本轮⛔ 不动 `d:/2`）。
3. **A345-T-b**：那 1 条 `target BoardCamera` 为何找不到 —— **根因未定**（`FindBuilt` 比真世界位置、旁挂 `pos` 是无缩放世界链 = `A345` 已知盲区；`WA345Ta` §八·3 给了三条待裁口径，⛔ 别照抄）。
4. **A562**：「有没有消费方依赖那 1.3 MB 版」**没查**（只核到文件本体与 md5）。
5. **A824**：`WarpforgeEffectLibrary.asset` 里那 9 次 `Scenario` 字面命中的**具体对象没收**。
6. **A799**：`Editor/` 侧那 **6 个**新裁点的**逐处行号**没逐条抠出来。
7. **`d:/4/_tmp_view/*.log`（18:52 那批）的覆盖面**：现读 `menu 2313/0` · `rewards 1917/0` · `shop 2164/0` · `collection/settings/net` 无「合计」行 · `shell 899/**6 失败**` 但 `shell_retry 905/0` · `battle 1692/0` ⇒ **「11 条全绿」的当次证据没逐条收齐**（只确认这 6 条）。

---

## 附 · 本次现核用到的**唯一新增**盘上事实（不是抄账）

- **A544 已完成**：`d:/4/_tmp_view/shop.log` 头 `Date: 2026-10-06T10:52:52Z` + `-executeMethod ShopScene.Run` + `合计：2164 通过 / 0 失败` + `✗` = 0；且 mtime 18:53 **晚于** 导入腿 18:26。
- **A191 已完成**：`d:/4/Unity/数据/游戏数据/env_blendables.json` 的 `_missingTargets` = **0 条**。
- **A562 未动**：md5 `ecebd2f811a8389bab405940ecfe6e07` 逐位同账。
- **§三第9条 mat 副本**：`718` 个。
- **A544/A826 的宿主**：`Editor/ShopScene.cs:1618/1693/1708/1732` · `Editor/RewardsScene.cs:8246/8269/8279/8304/8353/8369/8387`。
