# R1a · A 段 / F 段 / 表外 —— **只读现核**（2026-10-15）

> 只读现核代理 R1a。**没跑 Unity、没动 git、没改任何 `.cs` / `.md` / 资产**；本文件是唯一产出。
> 口径：**一切回代码/资产现读**；`清单_A表全量.md`（1014 13:33 快照、写在收口之前）只当摘要。行号会漂 ⇒ 每条都配锚点。
> ⚠️ **本机文件系统时间戳与项目内记日不同**（`ls` 里昨天那一批活全显 `Oct 6`，项目内记 `2026-10-14`）⇒ 下文只写「现读」，时间戳按原样引。

---

## 结论摘要

1. **39 条里 16 条 ✅ 已收**（A348 · A380 · A418 · A469 · A492 · A512 · A285/A385 · A494 · A543 · A582 · A629 · A532 · A539 · A571 · A702 · A738）。
2. **12 条 🔴 还开着**：A262 · A268 · A338 · A383 · A394 · A451 · A464（只剩 B2b）· A179 · A231③ · A538 · A546 · A752。
3. **5 条 🟡 部分收**：A425（①已做、②③换了落点、④声音未做）· A345-T-b（13/13 已跑、判据字面未达）· A591 · A569（S2–S5 未做）· + A285/A385 的**判据今天回退**（见顺手发现①）。
4. **4 条 ⛔ 挂起/已裁**：A163 · A186+A376 · A367（挂起）· A434 / A550 / A556（已裁，均非待办）。**1 条 ❌ 记录有误**：A628（约束的适用期已过）。
5. 🔴 **最该先看两条**：**A285/A385 的判据 `nPOTScale: 1` 今天 = 2（不是账上记的 0）**——素材腿跑在导入设置那一趟**之后**；**A345-T-b 的判据字面「没对上 0 条」今天实得每场 1 条**（= A591 那笔，但没有任何落痕）。

---

## 状态表

| 编号 | 判定 | 证据（文件:行号 / 资产路径） | 若还开着：要改哪个文件、改什么 | 置信度 |
|---|---|---|---|---|
| **A262** | 🔴 还开着（**一笔都没动**） | 旧名指针**逐字 31 处**仍在：`grep -rn "选牌Choose_数据与设计\|选牌与选效果面板_原版数值\|选牌_受影响卡普查\|灵魂石卡_逐张核\|日常_调用链_{DailyRewardPopup,Inbox,DailyStreak}" MyGame/Assets --include=*.cs \| wc -l` = **31**（与 `庚2_A262②_8组合并.md:154-186` 那张表逐条吻合）；例 `Battle/CardChoicePanel.cs:7,11,126,247` · `Battle/ChoosePanel.cs:6,38,51` · `Shell/DailyRewardPopup.cs:4,18` · `Shell/InboxWindow.cs:4,24` · `RuleEngine/Editor/RuleEngineTest.cs:426,4931,7760,7951,10936,10950,10978,11037` | 15 个 `.cs` 的 31 处注释按表改（⚠️ `RuleEngineTest.cs:10978` 是**断言文案字符串**、不是注释）；M-a 组 1（`工具/check_prebuilt_decks.py:39` 的 `OUT_MD` 让它自己吐指针 + `资料/原版预组牌_核对.md` 标生成物）· 组 6（三份 `查证_裸写触发点_*.md` 合一）· `资料/普查产出_1010/S1_下一批切块普查.md:41` 的「连带 4 处」→ 31 | 高 |
| **A268** | 🔴 还开着（① ⛔ 判据不齐 · ② 无守卫） | ① `Shell/MenuDraw.cs` 的 `ClipTmpMesh` 仍走通用 `materialReferenceIndex`（**无 `i ≥ 1` 硬分支**）；`Editor/CollectionScene.cs:458` 仍有 `if (ch.materialReferenceIndex != 0) continue;`；**全工程无一条断言盯 `mi` 分布**。② `Shell/MenuScroll.cs` 的 `SetOffset` 一族（`OnChanged?.Invoke()`）**仍无「目标视图激不激活」判断** | ① 只能主对话在同步点跑一次自检 + 打印 `mi` 分布（本代理不跑 Unity）；② 先裁「原版对应哪条重建回调」（uGUI `ScrollRect` 继承 `UIBehaviour` ⇒ 未激活不跑），再给守卫 | 高 |
| **A338** | 🔴 还开着（**而且是涨的**） | 按**账上那份脚本的原口径**重跑 `d:/4/_tmp_view/a338_scan.py`（只扫 `CardPresentation/**/*.cs`）⇒ **TOTAL refs = 580**（账上记 **411**） | 全仓「指向我们自己 `.cs` + 行号」的引用改按**符号**引用；⛔ 别拆多写手（要动全部六份自检宿主） | 高 |
| **A348** | ✅ 已收 | `Shell/ChatPanel.cs:44` `const int QBase = 3300;`（**已从 `public` 收窄**，`:41` 留了销账注释）；同文件 `:54/:56` 的 `public` 三条各带引用方注记 | —— | 高 |
| **A367** | ⛔ 挂起（真 Play） | `项目任务.md` §〇 挂起清单仍列 A367；2026-10-14 真 Play 试过、后端卡在 `Waiting for group Data complete Initialization` ⇒ **仍挂起**（如实记） | 无（等真 Play） | 高 |
| **A380** | ✅ 已收（④ 已做） | `资料/阶段二_锻造厂与战役页_原版规格.md:696` =「## 十六、A380④ · 桌面两张实拍逐张对账（2026-10-14）」（含 20 项清单） | —— （带出两条新账：`Premium Panel` 三态没做 · `Timer Text` 无倒计时，已在 `项目任务.md` §〇 记着） | 高 |
| **A383** | 🔴 还开着（判据已齐、**实现未落**） | `RuleEngine/Core/GameplayVariables.cs:25-31` 的 `enum GameMode` **仍只有** `Classic = 0` / `Skirmish = 13`（账上要「补 15 档」）；`BattleDriver` 四处未动 | 五窗 + `GameplayVariables.GameMode` 补 15 档 + `BattleDriver` 四处；判据全文 = `资料/普查产出_1014/RO_战场与窗口判据三件.md` §二 | 高 |
| **A394** | 🔴 还开着（大件） | `CardPresentation/Battle/AnimFXController.cs:115-119` 自陈「**`modules` —— ⚠️ 仍然建不出来**（如实记着）：场景侧没有 `AnimFXModuleBase` 的对应物…要真建得先给场景侧一条模块线（**A394，仍开着**）」；相机震动那半**已在**（`WarpforgeVFX/Runtime/WFModuleScreenShake.cs` · `BattleDriver.cs:1691` · `Core/CardFeel.cs:766`） | 场景侧模块基类 + 触发点（`AnimFXController` 侧）；⚠️ 那唯一一颗模块**很可能一次都不播**（`SetData` 调用点全在卡侧） | 高 |
| **A418** | ✅ 已收（三步都跑了） | `MyGame/Assets/WarpforgeArena1/arenas/battlearena2/battlearena2_manifest.json` 现读 `particles[38] = {go:'Embers', tex:'Default-Particle', texFile:'Default-Particle.png'}` **且 `texFile` 空条目 = 0**（账上「3 条空」已消）；`CardPresentation/Editor/BattleScene.cs:11140-11146` 的 `wantReparen` **已改由旁挂驱动**（`:93-95` 同一次加载取 `reparent`）；`d:/4/_tmp_view/arenaprefabs_1014.log` 里 13 场（含 battlearena2 / battlearenaleviathan）逐场有 A191 行 | —— | 中高（静态判据齐；实跑绿红以那一趟 Unity 腿为准） |
| **A425** | 🟡 **部分收**（①已做 · ②③**换了落点**已做 · ④声音未做） | ① `工具/extract_missing_shaders.py:203/206` 有 A425① 注释与 `"RewardAppearParticle"` 条目；`grep -ao RewardAppearParticle MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_menus_extra.bundle \| wc -l` = **1**（账上记 0）。②③ prefab 已导、库已重建 —— **但落点是 `MyGame/Assets/WarpforgeVFX/Prefabs/RewardAppearParticle.prefab`（guid `9d4947584ac615e49a0b764f2aba470c`）**，`Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 的 `- name: RewardAppearParticle` 条目**正指向这个 guid**；`数据/游戏数据/effect_index.json`（`generated 2026-10-06 14:15`, `count 963`）也有它。④ `Add card to deck` **全工程 0 命中**（`find MyGame/Assets -iname "Add card to deck*"` 空、`Resources/animfx_sounds.json` 无该键） | ④ 声音那半：给 `工具/import_original_sfx.py` 补「窗口级 cue」白名单，或一次性把 `bundle_soundcollection_assets_all/AudioClip/Add card to deck.wav` 按 `Shell/RewardWindow.cs:415` 的落点拷进 `Resources/Art/audio/sfx/`。⛔ **别照账上那句去找 `CardPresentation/Effects/`** | 高 |
| **A451** | 🔴 还开着（**比账上更多**） | 同口径粗扫（`///` 行里裸 `<` / 裸 `&`）⇒ **98 处 / 27 文件**（账上 91 处 / 18 文件）。分块：`Battle/` 68（`ScenarioBlendables.cs` 21 · `BattleDriver.cs` 18 · `AnimFXController.cs` 8 · `MulliganPanel.cs` 5 · `BattleCameraSreenSize.cs` 4 · `Label.cs` 4 · `EnvironmentApplier.cs` 2 · 另 6 文件各 1）· `Shell/` 20（`RewardWindow.cs` 4 · `MenuDraw.cs` 3 · `DailyData.cs`/`InboxWindow.cs`/`MissionsTab.cs`/`WindowsManager.cs` 各 2 · 另 5 个各 1）· `Editor/` 6 · `RuleEngine/Editor/` 4 | 18→**27** 个 `.cs` 的 `///` 行逐处实体化（`&lt;` / `&amp;`）；量尺 `WF_DOC=1 bash 工具/typecheck.sh`（⛔ 不跑 Unity，每路独立 `TMPDIR`） | 高 |
| **A464** | 🔴 还开着（**只剩 B2b**） | `grep -rn "colors32" MyGame/Assets/CardPresentation/Editor/ShellScene.cs` = **0 命中**；`colors32` 全仓只在 `Editor/CollectionScene.cs:5363-5369` · `Editor/MainMenuScene.cs:9689-9694` · `Editor/RewardsScene.cs:411-420`。B2/B2′/B3/B4/B5 在（`Editor/ShellScene.cs` 的 `TmpSpanPx:196` + 各节） | 只改 `Editor/ShellScene.cs`：补 B2b 一条断言 + 一个 alpha 读取器（可照抄 `Editor/RewardsScene.cs:395-420` 的 `TmpVertsAndAlpha`）；⚠️ 那些「计数器 == 0」的断言必须排在该文件 ⑤·d-4 **之前** | 高 |
| **A469** | ✅ 已收（生产接线已落） | `CardPresentation/Editor/RewardsScene.cs:4883-4972` 有「2026-10-14（A469 · 只剩「生产接线」那一小块）」整节：`:4940` 断 `BuildContext` 挂 `OnCollect` · `:4947` `ClickNodeForTest(1)` · `:4966-4972` **真点**窗里那颗 `Unlock Button/Hit`（`PointerLayer` 真路径）⇒ 断「UM1 的**真进度**被领掉」 | —— | 高 |
| **A492** | ✅ 已收 | `Shell/LiveOpsEventWindow.cs`：`title.SetCharSpacing(5f)` 在 `:777`、`title.SetAutoFitBox(...)` 在 `:785`、`MenuDraw.AlignLeft` 在 `:794`（**次序已对调**，并把「口径 / 无牙口 / ⛔ 别自定原版值」逐句留痕） | —— （如实标：这一处**没有断言咬得住**，别为它造期望值） | 高 |
| **A512** | ✅ 已收 | `Editor/CollectionScene.cs:2584` 与 `Editor/RewardsScene.cs:4997` **都改走** `MainMenuScene.CloseModalPopups()`；可见性已提为 `internal static`（`Editor/MainMenuScene.cs:226`） | —— | 高 |
| **A163** | ⛔ 挂起（判据在远端） | `资料/待办判据_1006.md:132` §A163 全文（下半判不出来 = mission 数据的 `EventComponents[Container]` GUID 在 PlayFab Title Data） | 无（等数据 / 真 Play dump） | 高 |
| **A179** | 🔴 还开着（**三块都没做**） | ⑤ `RuleEngine/Core/EffectText.cs:1176` 仍写 `$"战术卡文本解析：完全解析 {Full}/{Cards}"`；①④ `资料/真Play待验清单.md` 里 grep `A111 / A170 / A172` **零命中**（`D28/D29` 是 A113/A120，不是这两笔）；②⑥⑧ 仍待裁 | ⑤ 改一句措辞（`Summary()` 的文案）；①④ 补进 `资料/真Play待验清单.md`；②⑥⑧ 请调度台裁 | 高 |
| **A186 + A376** | ⛔ 挂起（等 `TitleData`） | `资料/待办判据_1007.md:32` §A186 全文（只剩登录卡格数那半；骷髅那半已由 A370 定死） | 无（等数据） | 高 |
| **A231③** | 🔴 还开着（**不许销账**） | 产物在：`d:/2/tools/w230_crosscab/out/final_arena1.bundle`（11,430,604 B / mtime `Oct 6 10:11`）· `synth_after_out.bundle`；但**全仓 grep 不到任何「在 Unity 里 `LoadFromFile` 过」的产物或报告**；`项目任务.md` §三 第 29 条 F 段仍写「⛔ 不许销账」 | Unity 腿：`LoadFromFile(out/final_arena1.bundle)` → `GetAllAssetNames()` 含不含 `Embers (2)` + `LoadAsset<GameObject>` 非 null（一次覆盖 A230② + A231③） | 中高 |
| **A285 / A385** | ✅ 已收 —— 🔴 **但判据今天回退到 2** | 判据 `grep -l '^  nPOTScale: 1' …/ui_menu/*.png.meta \| wc -l` 现读 = **2**（账上 2026-10-14 记 **0**）；命中者 = `40k_OfferBadge.png.meta` 与 `40k_shop_popup_info_bg.png.meta` —— **正是 A629 素材腿新导的那两张**（`.meta` mtime 落在导入设置那一趟之后）；盘上 `ui_menu/*.png` 现读 **287** 张 | 新账：把 `ArtBaker.ApplyImportSettings` 排到**素材腿之后**再跑一次（或让导入器自己调它）；见顺手发现① | 高 |
| **A494** | ✅ 已收（**比账上更全**） | 6 张全在 `MyGame/Assets/CardPresentation/Resources/Art/ui_menu/`：`Laser_Wave_2.png` · `Up_Rays.png` · `Glow.png` · `Shine_trail.png` · `LightningTrail.png` · `Noise_Combined.png`（1014 §一只记了 4 张，`Up_Rays` / `Glow` **也在**） | —— | 高 |
| **A543** | ✅ 已收 | `Resources/Art/ui_menu/` 里 `40k_topmarquee_currency_{crystal,blackstone,ticket,energy}.png` 四张全在 | —— | 高 |
| **A582** | ✅ 已收 | `Resources/Art/ui_menu/` 里 `40k_Crate_Tier{1..5}_{Iron,Copper,Silver,Gold,Warp}_open.png` 五张全在 | —— | 高 |
| **A629** | ✅ 已收 | `Resources/Art/ui_menu/` 里 `40k_shop_popup_info_bg.png`（803,867 B）与 `40k_OfferBadge.png`（54,382 B）都在（与调度台今日实测一致） | —— （⚠️ 这两张带出的导入设置回退 → 见 A285/A385 那行） | 高 |
| **A345（T-b）** | 🟡 部分收（**建场腿已跑；判据字面未达**） | `d:/4/_tmp_view/arenaprefabs_1014.log`（664,551 B）里 **13/13 场**各有 `[Arena] A191 分组节点（<场>）：新建 N · 改挂 M · 补 Animation K · **没对上 1 条**…target BoardCamera`；账上判据写的是「13 场各一行『**没对上 0 条**』」 | 不要修（那 1 条 = A591 那笔既有项）；要做的是**把「这是既有的」落成文字**（见 A591 行）。⚠️ `BattleScene.BuildAndSaveScene` 那一趟已在同批跑过 | 中 |
| **A434** | ⚖️ 已裁（**且已落码**） | `Shell/MenuDraw.cs:194` `public static readonly PxRect NoClip` + `:200 IsNoClip` + `:208/:432` 两处早退；`Shell/ViewportClip.cs:99-104` 的 `ClipState.IsNoClip` / `OptOuts` | 无（不是待办） | 高 |
| **A556** | ⚖️ 已裁（**排活约束，仍成立**） | `Editor/BattleScene.cs` 仍是战斗侧断言的唯一宿主（`_run_8_checks.sh` 里只有 `BattleScene.Run`）；`项目任务.md` §三 第 29 条 F 段那一行仍在 | 无（约束不是活） | 高 |
| **A591** | 🟡 部分收（**现象复现 · 没有落痕**） | 13 场日志逐字复现「每场没对上 **1** 条 `target BoardCamera`（旁挂 pos `(100.000,2.222,-13.572)`）」；但 `MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` 里 grep `BoardCamera` **只命中相机参数三段**（`:97` / `:135` / `:474`）、**没有一条写这是既有的**；`资料/战场场景线_交接.md` grep `没对上` / `A591` / `A345` **零命中**（只 `:135` 有一条 `WA345Ta` 指针） | 在 `ArenaBuilder.cs` 那条出声里写清「`BoardCamera` 回挂是有意为之 / 既有」+ 战场线正本记一笔；否则下个会话会把它当新缺陷 | 中高 |
| **A628** | ❌ **记录有误**（订正：**适用期已过**） | 这条约束的前提是「**A251 那一批**并行时 `Shell/WindowsManager.cs` 注册口 + `Editor/ShopScene.cs` 必须串行」；而 **A251 已全部收口** —— `资料/历史/A表已收口_1014.md:114`（A729 行）「七扇全建完 / 第六扇…**A251 收口**」，`项目任务.md` §三 第 29 条 A 段也写着「本轮已收口的一族 ✅ … **A251**」 | 正本把这一行改写成「**通用规则**：同一文件同一时刻只有一个写手」（约束本体已随 A251 收口失效） | 中 |
| **A532** | ✅ 已收 | `CardPresentation/Editor/BattleScene.cs` 约 `:10486-10496`：「🔴 **2026-10-14（A532）给下面那个 `16` 补出处**」+「🔑 **通则**…⛔ **别写死数**」；实测值 19 也写进去了 | —— | 高 |
| **A538** | 🔴 还开着（半齐） | `Shell/CampaignRewardWindow.cs` 的 `BuildUnlockButton`（`BuildColumn` 那条链）里 `else { pi.SetActive(true); SetText(cost, ctx.PointCost.ToString()); }` **没有 `isBase` 分支** ⇒ **两列都显点数**；同文件 `:757` 的注释自己写着「本战役 47 个节点的 `Cost` 全在 **100…1100**」⇒ **今天恒显点数**（原版高级钮恒「Claim」免费态） | 改 `Shell/CampaignRewardWindow.cs` 的 `BuildUnlockButton`：高级列（`!isBase`）走 `SetAsFreeClaim` 那一档（不画 `pointDrawer` / `cost`） | 中高 |
| **A539** | ✅ 已收 | `CardPresentation/Editor/RewardsScene.cs:5307-5410`：「🆕 **2026-10-13（A539 = A402 + A472 · WC1 出稿）**」整块 —— `:5334` A402① 前提 · `:5339` 断 `Warning` 开着 · `:5369` 断整颗 `Unlock Button` `SetActive(false)` · `:5379/5386` A537③ + A402③ 的宽度断言；`IsPremiumLocked = true` 的**锁定态夹具**就在里面（`:5331`） | —— | 高 |
| **A546** | 🔴 还开着（① ②**都还在**） | ① `Editor/SettingsScene.cs:1441` 仍写「走点阵兜底时 `Align*On` **首句就 return**（空操作）」—— A476 之后它已改成**出声**，措辞过期；② `Battle/Label.cs` 的 `SetAlignLeft` doc 仍写「⚠️ 要在**量尺寸之前**调」，函数体只有 `_tmp == null` 一条守卫、**没有「量过尺寸就出声」的守卫** | ① 改 `Editor/SettingsScene.cs:1441` 那句措辞；② 给 `Label.SetAlignLeft` 补守卫或在 doc 改口径（⚠️ ①② 两笔指向同一批文件，⚠️ `WLabel` 那件「只增不改」） | 高 |
| **A550** | ⚖️ 已裁（不是待办） | `资料/历史/A表已收口_1014.md:28/37`：**用户 2026-10-14 拍板 =「维持现状：只出声」**（同族 A613 · A535 · A566 一起）；落码面只要求 A566 加注释 | 无（裁定已落盘） | 高 |
| **A569** | 🟡 部分收（**S1 现核完 · S2/S3/S4/S5 未做**） | `资料/普查产出_1014/RO_窗口重建三件.md` §A569：扫描已完成（264 条窗口级操作行 / 1900 句柄 / 24 跨窗站点）；S1 **✅ 现读核过 5 处「今天安全」（但无断言守）**；S2 `Shell/BattleLogTab.cs` 的 `OpenPopup` 没读 · S3 `MainMenuScene` 的 `FindChild(nmv,"List View")` 下游没追 · S4 `Editor/ShopScene.cs` 6 个调用点没人工复核 · S5 出声加固没做 | 按 RO 表的派活：**2 只读 + 1 写手**（S1 补出声要独占 `Editor/ShellScene.cs`；⚠️ 与 A571 / A675 撞 ⇒ 排串行队列） | 高 |
| **A571** | ✅ 已收 | 「（前提）重开之后窗真的开着 —— 下面那条才不是空断」这句落在 **4 个宿主共 18 处**：`Editor/CollectionScene.cs` **4** · `Editor/SettingsScene.cs` **7** · `Editor/ShellScene.cs` **4** · `Editor/ShopScene.cs` **3**（+ 1 处助手）；例 `Editor/ShopScene.cs:3333/3496/3607`。与 `Block23_A571.md`「18 处 (b) + 1 处 (a)」逐数吻合 | —— | 高 |
| **A702** | ✅ 已收 | `Resources/Art/ui_menu/` 里 `OctagonUI_Filled_SDF.png` · `UI_HIghlight_Internal.png` · `40k_UI_Banner_BW.png` **三张全在** | —— | 高 |
| **A738** | ✅ 已收（**裁定项已落**） | 裁定要的那份 doc **在**：`Shell/CampaignTab.cs:1007-1051`（`ClearClip` 的 `///`）逐条写了「① 不是缺陷、快照恒 `(null,0,0)` ② 它是 A353 那六条断言**唯一的被测对象** ③ 夹具不在可碰范围」+「**要删它必须先一并改写那段夹具**」（= 谁改夹具谁删它）。`ClearClip()/RestoreClip()` 本体**仍在**（4 对：`:142/150` · `:155/160` · `:192/259` · `:902/997`）—— 那是**裁定接受的**结果 | —— （⚠️ 与 §六·B 那句「⏭ 立账待做」打架，见「不符」§5） | 高 |
| **A752** | 🔴 还开着（纯注释） | `Editor/MainMenuScene.cs:5357` 仍写「**改坏法**：删掉 `BuildTabButton` **末句** `txt.SetWrapping(false)` ⇒ …红」；亲读：那句在 `Shell/MenuWindowBase.cs:532`，**后面还有 `Badge Highlight`（`:536`）+ 点击区（`:543-557`）约 20 行**，`BuildTabButton` 本体是 `:443`–`:558` ⇒ **不是末句** | 改 `Editor/MainMenuScene.cs:5357` 那一句措辞（纯注释，共用件零动 ⇒ 不触发复跑） | 高 |

---

## 一、与正本 / 清单不符的地方

1. 🔴 **A285/A385 的判据今天不成立**：账上（`A表已收口_1014.md:14`）记「`nPOTScale: 1` = **0**」；现读 = **2**（`40k_OfferBadge.png.meta` · `40k_shop_popup_info_bg.png.meta`，两张都是 A629 素材腿新导的）。**这不是「记错」，是「收口后回退」**——素材腿跑在 `ArtBaker.ApplyImportSettings` 那一趟之后。见顺手发现①。
2. ⚠️ **A425 的落点写错了**：账上（`A表现核_块6.md:117`）写「② 判据：`CardPresentation/Effects/RewardAppearParticle.prefab` 出现」，但②那条路（`EffectExporter.ListedPrefabs` → `RunListed`）的**真实出口是 `EffectExporter.PrefabDir = Assets/WarpforgeVFX/Prefabs`**，不是 `CardPresentation/Effects/`（那是 `EffectLibraryBuilder.UserPrefabDir`，给「自制特效」用的）。⇒ 只看 `Effects/` 会**误判成「一步没做」**。
3. ⚠️ **A338 的规模又涨了**：账上 411 处（`批次计划_1013.md:134/516`），按**同一份脚本**（`d:/4/_tmp_view/a338_scan.py`，口径 = 只扫 `CardPresentation/**/*.cs`）重跑得 **580**。
4. ⚠️ **A451 的规模也涨了**：账上 91 处 / 18 文件，同口径现读 **98 处 / 27 文件**（新增的 `MissionsTab.cs` / `DailyData.cs` / `BaseOfferPopup.cs` / `ChatPanel.cs` / `CollectionData.cs` / `MenuScroll.cs` / `ReferralPopupWindow.cs` / `TouchInputManager.cs` / `BattleCameraSreenSize.cs` 等是这一批新写的）。
5. 🔴 **A738 一处口径打架**：`批次计划_1013.md` §六·B（A738 行）写「⚖️ 已裁「接受 ①：就留 + doc 写明…」· ⏭ **立账待做**」，而 `Block5_RewardsScene.md:25` 现核判「**已收口**」（doc 已在 `CampaignTab.cs`）。**现读支持后者**：裁定要的三样都写在 doc 里了。另 `Block5` 自己 §4 也留了一条「逐字短语差一点」（裁定要的字面是「**A13 未做**」，doc 里没有这五个字，只有「本轮没删 + 理由」）——建议正本按「已收」收口。
6. ⚠️ **`清单_A表全量.md` 的 A629 / A702 两行已被现读推翻**（那两张清单写在收口之前）：A629 两张图**已在 `Resources/`**、A702 三张**也已在**——两条今天都可销。
7. ⚠️ **A591 那句「当预期红处置」在代码/正本里没有落痕**：13 场建场日志逐字复现，但 `ArenaBuilder.cs` 与 `资料/战场场景线_交接.md` 都**搜不到**这句口径（后者连 `没对上` 三个字都零命中）⇒ 下一个会话会把这条出声当新缺陷。

---

## 二、顺手发现（**只报不改**）

1. 🔴🔴 **导入设置回退（新账 · 与 A285/A385 + A807 + A808 同族）**：`ArtBaker.ApplyImportSettings`（`MyGame/Assets/CardPresentation/Editor/ArtBaker.cs:153/212` 两处设 `ti.npotScale = None`）**没有跟着素材腿跑**。素材腿（`资料/普查产出_1014/素材_导入器登记与A629.md`：盘上 285 → **287**）之后，新进的两张 `.meta` 是 `nPOTScale: 1`。⇒ **判据从 0 回到 2**，且**没有任何自检会红**（`Resources/Art/**` 在 `.gitignore` ⇒ 连 `git status` 也看不见）。
2. ⚠️ **A494 的账记得比实际少**：1014 §一记「4 张」，`A表现核_块6.md` §A494 记的是 **6 张**（含 `Up_Rays.png` / `Glow.png`）。现读 **6/6 全在盘** ⇒ 版本较全的那份是 `A表现核_块6.md`。
3. ⚠️ **`EffectExporter.cs` 的 `ListedPrefabs` 里已经加了 `"RewardAppearParticle"`（`:638`）**，注释写着「判据 → `资料/普查产出_1012/H3_领取粒子与Blink公共件.md`」—— 说明 A425② 那一步**确实执行过**（否则不会留下 prefab + 库条目）；但**账上给的那个落点判据是错的**（见「不符」§2）。
4. 📌 **A268① 的那段注释仍是一句会被误读的判据**：`CardPresentation/Editor/CollectionScene.cs:405-408`（现读 `:458` 是那句 `if (ch.materialReferenceIndex != 0) continue;`）旁边写着「本工程只有一个字体资产 ⇒ 实际恒为 0」—— 它**只证了字体那一半**，没证 `<sprite>` 那一半（`Core/TmpFont.cs:163` 把自带 `m_Material` 的 sprite asset 挂到全工程唯一那道建 TMP 的口上）。
5. 📌 **`ArenaBuilder.ApplyGroupNodes` 那条「没对上」出声的口径偏吓人**：它自己写「**很可能是上游闸门没建**」，而 13/13 场都正是同一颗 `BoardCamera` ⇒ 13 条日志看起来像 13 个新缺陷。建议连同 A591 一起把措辞收口。
6. ⚠️ **`项目任务.md` §三 第 30 条（战场线）仍把「Unity 腿（严格串行）」列在「⏭ 仍开着」**，而那一趟（`gen_unity_arena_manifest.py --arena battlearena2 / battlearenaleviathan` · `gen_env_blendables.py` · 13 场 prefab 重建 · `BattleScene.BuildAndSaveScene`）**已在 2026-10-14 跑完**（`A表已收口_1014.md` §三 + `d:/4/_tmp_view/arenaprefabs_1014.log`）⇒ 那一行的状态列**过期**。

---

## 三、方法 / 边界（如实标注）

- **全部是静态判定**：本轮**没跑 Unity**（红绿不在本代理能力内）。凡「实跑才算数」的（A418 的绿红 · A345-T-b 的终态 · A231③）都已在表里标明。
- 行号只作参考，每条都配了**锚点内容**；`清单_A表全量.md` 的状态列**一律没用**（它是 1014 13:33 的快照、写在收口之前）。
- **搜过哪儿**：`d:/4/Unity/MyGame/Assets/**`（全 `.cs` + `Resources/Art/**` + `StreamingAssets/WarpforgeVFX/**` + `arenas/**`）· `d:/4/Unity/工具/**` · `d:/4/Unity/资料/**`（含 `普查产出_1010..1014`、`历史/`、`待办判据_*.md`）· `d:/4/项目任务.md` · `d:/4/_tmp_view/*.log` · `d:/2/tools/w230_crosscab/out/`。
- **没查到的**（⛔ 不猜）：A231③ 的「Unity 加载过一次」全仓无产物/无报告；`Add card to deck.wav` 全工程 0 命中。
