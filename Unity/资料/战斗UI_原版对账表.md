# 战斗 UI 原版对账表（2026-09-12 建立）

> **用途**：战斗界面里每一个元素的「原版是多少 / 我们是多少 / 补上没有再」，一条一行。
> 改战斗 UI 之前先查这张表，**不要重新去 dump 里挖**。
> 新会话接着做的话：看最下面的「还没对上的」那张表，那就是待办。

---

## 一、数据从哪来（**三处独立来源，互相对上才采用**）

| # | 来源 | 路径 | 说明 |
|---|---|---|---|
| ① | **运行时 UI 树** | `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv` | 2026-09-12 本机重跑 mod 拿的，**1253 行**。每行 = UI 树一个节点：path / name / activeSelf / anchoredPosition / sizeDelta / anchor / pivot / text / fontSize / color / sprite |
| ② | 同一份树的旧版 | `.../data/runtime_ui_dump_Battle_Arena_1.tsv`（2026-08-25） | **和 ① 逐字段一致**（比过 14 个关键节点，0 处不同）→ 证明这些是**预制体静态值**，不随对局/服务器变化 |
| ③ | 静态场景 JSON 的转写 | `资料/战斗规格/战斗重建_0827/战斗界面JSON权威表_0827.md` | 绝对屏幕坐标表（anchor 链算出来的），dump 里没有绝对坐标时用它 |

**⚠️ 关于「原版已关服」**：`drive` 模式进去看到的 **3D 战场是对的**（本地资产），
但**战斗 UI 的"内容"出不来**（要真实对局数据）。所以**本表所有尺寸都没从画面上量** ——
只用 ①②③，三样都跟服务器无关。**能复现**：改 `d:/2/unity_run_ref/UserData/sjs_dump_cfg.txt`
第 3 行为输出目录、第 4 行写 `drive`，启动 `d:/2/unity_run_ref/Warpforge.exe`（先设 `DOTNET_ROOT`）。

---

## 二、对上的（原版有、我们也有，且数值一致）

| 元素 | 原版实测 | 出处 | 我们的实现 |
|---|---|---|---|
| **卡本体** | `2.0927 × 3.3313`（卡单位） | ③ 的 `2DCard (GO 1075, RT 2876)`；① 里 `CardUI (4)` = 2.1×3.3 | `CardView.Width/Height` |
| **场卡** | **137.2 × 218.4** px | `2.0927×0.36×182.14` / `3.3313×0.36×182.14`（k=182.14，见 `审查更正清单_0827.md:136`） | `BattleScene.BoardScale`，自检逐条断言 |
| **手牌卡** | **165.0 × 262.6** px | 同上 ×0.73 | `BattleScene.HandScale` |
| **卡框** | `2.2452 × 3.2572 @ y−0.03`（**rect 比卡本体宽**）；金属 bbox 实为 **608×936**（宽高比 0.6496） | ③ A2 表 `CardFrame (GO 83, RT 3385)`；预制体 JSON `RectTransform_-3192049446263720900` | `CardView.FrameMesh`：**按 sprite 自身比例 fit 进那个 rect**（2026-09-12 改）→ 实绘 **2.1159 × 3.2572**。⚠️ 更正两条：① 原话说「两侧塔楼探出去」**不成立**（金属实宽比卡本体窄，rect 左右各约 18% 是透明留白）② **不能把 sprite 拉满 rect** —— 会横向撑宽 6%。fit 出来的宽度正好落在卡本体（2.0927）那条线上，和原版卡面实测「金属顶到卡边」一致 |
| **卡面 7 个位** | 费用 `(0.788,+0.666)` / 护甲 `(0.899,−0.991)` / 近战 `(−0.836,−1.19)` / 远程 `(−0.595,−1.40)` / 生命 `(0.74,−1.36)` / 稀有度 `(0,−1.458)` / 文字块 `1.3×0.68 @(0,−0.7745)` | ③ A2 表（卡单位） | `CardView.CostAt` 等 7 个常量 |
| **棋盘两行 / 手牌行** | 玩家行 708 px、敌行 466 px、手牌中心 950 px | `d:/warpforge/scripts/battle.gd` 的场卡尺寸体系 | `BattleScene.PlayerLineY/EnemyLineY/HandBaselineY` |
| **9 槽跨度 / 中心距** | 1331.6 px / 149.3 px | 同上 | `BattleScene.BoardSpacing` |
| **END TURN 按钮** | 130.7×80.4，中心 **(1848, 456)**（右侧能量区中段） | ① `.../Energy And turn holder/Clock`；③:140 | `BattleDriver.EndTurnX01/Y01` |
| **能量竖排三件套** | EnemyMana 97.7² / Clock 130.7×80.4 / PlayerMana 97.7²，都在 x≈1827~1904 | ① + ③:149-156 | `_energyGem` / `EndTurnX01` |
| **右侧大底板** | `UI_Energy_Holder_big` 302.1×480.8（右侧出血 156 px） | ① `Energy And turn holder` 自己的 rect | `HudImageTex(... HudDecorZ)` |
| **任务点** | `UI_Quest_Points` 97.7²：我 x[1817.0,1914.7] **y[595.4,693.1]**（水晶**下**方）、敌 x[1816.1,1913.8] **y[150.2,247.9]**（水晶**上**方） | ③ B 节 `QuestPointsHolder` 的**绝对坐标**；接片 `BackgroundJoin` 另有一条 | `MyQuestX01/Y01`、`FoeQuestX01/Y01` + 两块 `QuestJoin`。⚠️ **2026-09-12 更正**：原来写「玩家在能量上方 37.4 px、敌方在下方 36.4 px」—— 那 37.4/36.4 是 **接片 `BackgroundJoin`** 相对 holder 的偏移，holder 自己的偏移是 +89.59（敌）/ −88.70（我）。照接片摆会让两个图标都贴在水晶**内侧** |
| **督军名牌** | 实绘 **382.3 × 126.3** px（贴图 442×146，PreserveAspect）。⚠️ **位置**（2026-09-13 更正）：我 rect x[−38.1,397.6] y[951.4,1077.7] / 敌 x[−37.9,397.9] y[15.6,141.9] → **实绘左缘都在 −11.4**（贴屏幕左缘、出血 11 px），文字中心 (237.25, 999.45) / (240.19, 63.9)、**H=居中** | ③:16 + dump `NameBackground 435.7×126.3`；位置出处 `子代理读报_back左区_0827.md:46,55,69,78` | `worldHeight = 126.3/108`。⚠️ **2026-09-13 修正位置**：原来用 `EnemyInfo(157,108)`/`PlayerInfo(32,977)`（那是 `Alliance Panel` 下**另一份** inactive 实例）→ 我方偏右 44 px/偏高 37 px、敌方偏右 ~168 px。见第四节 |
| **回合灯** | **23.4 × 23.4**（anchor 矩形 22.8×34.5，图 60×59 KEEP_ASPECT 缩进去）。⚠️ **位置**（2026-09-13 更正）：我 rect x[1801.5,1825.3] y[967.4,1003.0] → 绝对中心 **(1813.46, 985.2)** = 相对 230² 牌堆中心 **(83.46, −20.2)**；敌 x[1765.5,1786.3] y[−106.5,−75.4] → 相对 200² 牌堆中心 (73.6, −9) | ① `DeckAndEnergyImage/YourTurnImage`（anchorMin/Max (0.779,0.051)-(0.878,0.201) + **`anchoredPosition (7.9,65.8)`**）；绝对 rect `子代理读报_back右区_0827.md:179-180,193-194` | `DeckLightPx = 23.4`、`MyLightDxPx/DyPx`、`FoeLightDxPx/DyPx`。⚠️ **2026-09-13 修正位置**：原来只按**锚点矩形**折算 = (75.6,−86)，**漏了 `anchoredPosition`** → 灯被摆到牌堆右下角（**偏低 66 px / 偏左 8 px**） |
| **牌堆底板** | `PlayerDeck` 230×230 贴**右下角**；`EnemyDeck` 200×200 贴右上角 | ③ 绝对坐标表 | `MyDeckX01/Y01` |
| **卡背** | `Cardback` 2.1739×3.1364（×100 → 217×314 px） | ③ A2 表 | `DeckCardPx = 314` |
| **攻击方式选择器** | 槽 621.7×122.2；底板实绘 514.8×125.4（`780×285 × localScale 0.44 × 1.5`）；按钮 118.8²；黄圈 176.2² | ① `Drag Attack Selector` 子树 | `AttackSelector.cs`（尺寸全对） |
| **结算面板** | `EndBattlePanel` 子树（Background 3840×2160 / Video Image 1920×1080 @(−6,64) / SkullsHolder 648.1×52.4 / skull 64.3×71.2 / Trophy 60.1×60） | ① dump | `EndPanel.cs` |
| **卡牌放大展示窗** | 遮罩 α0.7725；`Card Display` 752×868 居中；放大卡 ≈467×743 px；下方文字条 y≈921~1078 | ③ front弹层报告 §一 + ① | `CardDisplayWindow.cs`（卡高 743 px 有断言） |

---

## 三、还没对上的（**这就是待办**）

| 差什么 | 原版实测 | 出处 | 我们的现状 |
|---|---|---|---|
| **能量底板** | `Card Frame Cost Icon`（sprite 248×244，实绘 91.3×91.3）垫在水晶下 | ① `.../ManaHolder/Energy Player` | ✅ **2026-09-12 做完**：`BattleDriver.cs` 建 HUD 时按权威表 B 节的 `Energy Player` 实绘 94.6×91.3 摆（我 `MyEnergyPlateX01/Y01`、敌 `FoeEnergyPlateX01/Y01`），图取 `CardArt.DeckUi("Card_Frame_Cost_Icon")`。⚠️ 之前那句「本地只有 sprite 元数据、没有导出的 PNG」是错的（已删） |
| ~~敌方能量水晶~~ | `EnemyMana` x[1826.3,1900.8] y[249.8,327.4] + `ManaText` fs40 | ③ B 节 | ✅ **2026-09-12 补上**：原版有这颗（玩家能看到对手剩多少能量），**我们原来一颗都没画**。位置 `FoeEnergyX01/Y01`，两张图 `40k_battle_energy_full/_empty` + `0/0` 数字。自检 7 条断言守着 |
| **阵营资源：信仰 / 灵石** | `FaithHolder` 117.9×149.3（`40k_Battle_Display_Faith`）、`SpiritStoneHolder` 112.1×116.6（`UI_Energy_Eldar`）+ 石 51.0×63.0 | ① `.../ManaHolder` 子树 | **UI 和机制都还没做**。⚠️ 2026-09-12 更正:原来写「引擎侧也没有对应机制」**是错的** —— 机制骨架在解包资源里是齐的（`ManaType.SpiritStone=5`、`PlayerManager.faithMana/spiritStoneMana`、`UseSpiritStoneEnergy`、`AddFaithMana`…），**而且用户给了口径**（信仰=阈值触发不衰减不设上限；灵魂石=无初始值无上限不增长、只由效果扣）。详见 `资料/阵营推进_清单与交接.md` §八 |
| **回合倒计时** | `ClockManager` + `Countdown`：60 s / 缩时 10 s / 超时倒计时 15 s / 催 35 s | `DefaultScenario.json:19-21`（本地解包资产）+ `ClockManager__GetTotalTime.c`、`__Update.c:110-141` | ✅ **2026-09-13 做完**：走完 60 s → 显示 15 s 倒计时 → 走完**自动结束回合**（无惩罚）；≤35 s 数字变色（原版发语音，我们没音频 → 变色是我们挑的）。⚠️ 原版按模式覆盖总时长（EventAI 240 / PracticeOffline 600），我们取 DefaultScenario 的 60，改一行可换 |
| **设置面板**（投降的家） | `SettingsBtn` 63.9²（`UI_Settings_Icon`）x[1808.0,1871.9] y[9.2,73.1]；面板 743.2×758.6 | ③ 权威表:207 + `BattleSettingsWindow.cs:9`（`resignButton`/`closeButton`+三根音量滑块） | ✅ **2026-09-13 做完**：设置按钮（原版坐标）+ 面板（深色实底+压暗+75² 圆形关闭钮）+ **投降按钮**。⚠️ 投降按钮 rect **查不到**（dump 里没这个节点）→ 尺寸位置**我们挑的**；音量滑块没做（没接音频） |
| **加时标记** | 判定 `turnCounter >= overtimeTurn`（每回合开始一次）；表现 淡入 1 s / 停留 1 s / 淡出 1 s + `OvertimeStart` 音效；`OvertimeIndicator` 68.6×71.0（`40k_icon_overtime`） | `OvertimeUi__DisplayOvertime.c` + `MonoBehaviour_4883.json`（`fadeTime:1.0`）；规则书 `:48,:137`「双方能量均达 10 后进入，加时中每回合 +2 能量」 | ⏳ **待做**。⚠️ **数值本地确证查不到**（ⓒ）：`overtimeTurn` 只在 LiveOps 服务器下发的 JSON 里（本地只有 MonoScript 声明）。要做就二选一：照规则书那条（能量都到 10）或自己挑回合数并标明 |
| ~~**牌库张数底板**~~ | `Player Deck Size Container`：**我 238.5×59.11 / 敌 210×52.05**（= 父宽×0.95 + 20，**高由 `AspectRatioFitter`(WidthControlsHeight, 4.0346479415893555) 定** —— 原版 `sizeDelta.y` 是 **0**，只看 sizeDelta 会以为它没高度）；图 `40K_display`，**α = 0.6941177**；文字 fs 42（我）/ **41.15（敌，原版两边就不一样）** | `RectTransform_3318.json` + `MonoBehaviour_4275.json`（我）/ `RectTransform_2658.json` + `MonoBehaviour_5165.json`（敌，整条 `scale=(1,−1)` 竖直镜像）；`子代理读报_back右区_0827.md:181`；dump `runtime_ui_dump_drive_0912.tsv:310,321` | ✅ **2026-09-13 做完**：`BattleDriver.BuildHud` 里画上底板（我/敌两块，半透明 + 原版偏移 −20.75 / −5 px），文字与板**共用同一锚点**。⚠️ 2026-09-13 更正：本格原来写「图只同步到 `Art/原版/`、**还没进 `Resources/`**」—— **已经进了**（`工具/sync_battle_ui_art.py --check` 报 21 张全在）。⚠️ 原版 `PreserveAspect=0`（把 442×112 的图横向拉 2%），我们的 `ImageQuad` 保比例 → 宽度少 2%，注释里标了 |
| ~~**手牌数底板**~~ | `CardsInHandText/Bg (1)`：**实绘 259.3×65.7**（不是 288.2×89.6 —— 那串要乘整条缩放链 `108 × 0.925926 × 0.009 = 0.9`），图**也是** `40K_display`、α 0.6941177、PreserveAspect=1 | `RectTransform_3212.json` + `RectTransform_3400.json` + `Transform_1401.json` + `MonoBehaviour_4418.json` | ✅ **2026-09-13 做完**（`HandPlate`）。⚠️ **位置是我们挑的**：这两个节点在 dump 里 `activeInHierarchy=False`（**没验到实况**），祖先链还是纯 Transform（`HandArea` / `PlayerArea` 都不是 RectTransform）**算不出绝对坐标** → 让它跟着既有手牌标签的中心走（`PlaceHandPlate`）。原文的「288.2×89.6」是漏乘 0.009 的直读 |
| ~~**本回合已出牌数**~~ | `CardsPlayedInTurnHolder`：锚 `LeftArea` (0,0.5)、pos (0,−28.178)、105.057×28.178；三枚 `CardsPlayedInTurn1..3` 各 **20×20**、**间距 9**（`HorizontalLayoutGroup` spacing 9 / LowerCenter），第一枚 x=13.5285、y=−28.178；图 `40k_general_bt_yellow`（71×71，PreserveAspect=0） | `RectTransform_2722,3470,3238,2740.json`、`MonoBehaviour_5039,4406,4381,5015.json`；dump `:101-104` | ✅ **2026-09-13 做完**：`BattleDriver` 记 `_cardsPlayedThisTurn`（真拖出去打出才 +1，走的是 `OnCardDeployed`），出几张亮几枚、**开局全灭**（原版 dump 里也是全 `activeSelf=False`）。⚠️ 第四张起不显示（原版只有三个节点）。⚠️ **这个 holder 挂在 `LeftArea` 左缘中点，从 dump 看不出是「玩家侧」还是「当前行动方」**（三枚全 inactive，没实况）—— 我们按「**我方**本回合」做。⚠️ 我们**没有**缩时（`clockTimeLimitReduced`）那套，所以它目前只是显示 |
| **边角按钮群** | `SettingsBtn` 63.9²、`ChatButton` 64.4×61.8、`ShowCemeteryBtn` 64.5×64.2、`CenterCameraButton` 64.4×61.8、`OffensiveButton` 109×106.9 | ① `.../LeftArea`、`.../RightArea` | ⚠️ 部分做完：`SettingsBtn` ✅ 2026-09-13。其余四个没有 |
| **墓地日志** | `CemeteryLogPanel`：Frame 863×1032.5 / BG 769.5×454.4 + 四条 `40k_battlelog_frame_*` | ① `.../LeftArea`、③ §一 | **整块没有**（图有：`40k_battlelog_frame_{Bottom,Left,Right,TOP}.png` 在切片库里） |
| **换牌 Mulligan** | `Mulligan`：`MulliganText` 1344×79.4 + 继续按钮（`activeSelf=True`） | ① `.../FrontCanvas` | **整块没有**（引擎也没有换牌流程） |
| **选卡菜单** | `ChooseCardMenu/ButtonsGroup`：Continue 548×75.9 + BG 577.5×63.8 + 圆钮 80.5×79.6 | ① `.../FrontCanvas` | 没有（引擎没有「选一张牌」的效果） |
| **单位语音条** | `Unit Chat/PlayerChatDisplay` 648.8×236.5（`40k_voicelines_radio` 766×280）+ 敌侧同尺寸 | ① `.../FrontCanvas` | 没有（原版有语音，我们没接音频） |
| **回放条** | `ReplayButtons` 4 枚 79.8×48.6（`40K_replay_bt_*`） | ① `.../BackCanvas` | 没有（单机没回放） |
| **等待提示 / 通用弹窗** | `WaitText` 1344×79.4 + `Dark Shade` 3963.5×3366 + `40k_popup` 1323×90 | ① `.../BackCanvas` | 没有 |
| **头像块** | `Avatar Item Small` 155.6×136.5 + `Player Profile Border` | ① `.../LeftArea/EnemyInfo` | 没有（原版是玩家头像，单机没数据） |
| ~~**里程碑骷髅**~~ | `MatchSkulls Icon` rect 65.39×54.14（图 `40k_battle_Win Skull` 66×73，PreserveAspect=1 → 实绘 54.14 高）+ `MatchSkulls Score` **`x3`** fs 35、**H=左 / V=Midline**、白粗描边；绝对坐标 骷髅 x[160.7,226.1] y[929.5,983.7]、分数 x[225.4,319.9] y[936.1,983.4] | `子代理读报_back左区_0827.md:56-58`（**权威表 :195-196 那两行 x 是错的**，见那里「矛盾1」）；`RectTransform_2783,3549,3467.json`、`MonoBehaviour_5234,3785.json`；dump `:130-132` | ✅ **2026-09-13 做完**：画在**我方**名牌上（原版节点在 `PlayerInfo` 下，实况 `activeSelf=True` 确实看得见），用**原版绝对坐标**摆。⚠️ `x N` 的 N 我们按「**已达成数**」算，判据与结算面板**共用一份**（`DeckRules.SkullsFor`，规则书:36）；**原版那个算法证不出来** —— `BattleScoreUiManager.UpdateMilestonesCount` 方法体被剥空，「x3」是「已达成数」还是「总数」无法判定，代码注释里标了。⚠️ 敌方名牌上**没有**这一块（dump 里只有 `PlayerInfo/Milestones`） |
| ~~**稀有度钻石**~~ | `Rarity` 0.4×0.4 @(0,−1.458)，图 `1_40k_cardframe_rarity_common` 等 5 张（浅蓝/绿/紫/金/橙红） | ③ A2 表 | ✅ **2026-09-12 做完**：`CardView.GemMesh` + `CardArt.DeckUi`，按稀有度取那 5 张。⚠️ **它和「卡框分档」是两件事**：卡框分档改的是**框的形制**（tier1 素 → tier4 华丽），而卡框纹理上那颗菱形是**空的暗色凹槽**（四档都一样）；颜色在这颗宝石上。对照图 `资料/留档_排查证据/卡面组装_0912/gems.png` |
| ~~**卡框分稀有度**~~ | 卡框原版按 tier1–4 分四张（`CardFramesSO`：13 阵营 × 4 档 × 4 个槽 = **208 个 sprite**） | `03_界面UI/通用静态资源/MonoBehaviour/CardFramesByArmy_6819405058091919240.json` | ✅ **2026-09-12 做完**：四档全导进来（`frame_<阵营>_tier1..4.png`），`CardArt.Frame(阵营, 稀有度)` 取。**稀有度→tier 的映射是对照原版卡面实测的**（common→1 / rare→2 / epic→3 / legendary→4，对照图 `资料/留档_排查证据/卡面组装_0912/tier_map.png`）。⚠️ `special`（39 张）没实测，先按 tier4 |

---

## 三点五、2026-09-13 全面位置核对（用户要求「举一反三，全面检查位置是否正确」）

> 起因：里程碑骷髅压住了名牌文字。**根因不是骷髅，是名牌的位置本身抄错了实例** ——
> 于是把所有可见元素按「1920×1080 绝对矩形」重对了一遍（原版值一律取
> `子代理读报_back左区/back右区/front交互层/front弹层_0827.md` 的 chain_rect，
> 权威表已知有错行、dump 是运行时被改写过的帧——见那份汇总里的「备注1/2」）。

**① 核对后改掉的（都是位置错，不是尺寸错）**

| 元素 | 原来（错） | 改成（原版） | 错因 |
|---|---|---|---|
| **督军名牌（我）** | 左缘 x=32.6、中心 y_top 977.4 | 左缘 **−11.4**、中心 y_top **1014.55** | 抄的是 `FrontCanvas/Alliance Panel` 下**另一份** `PlayerInfo`（inactive） |
| **督军名牌（敌）** | 左缘 x=157.4、中心 y_top 108 | 左缘 **−11.2**、中心 y_top **78.75** | 同上（敌方偏了 ~168 px，一眼可见） |
| **名牌文字** | 左对齐 | **居中**（原版 H=2） | 同上（原版文本框 x[112.6,361.9] 居中） |
| **牌堆回合灯** | 相对牌堆中心 (75.6, −86) | **(83.46, −20.2)**（敌 (73.6,−9)） | 只按**锚点矩形**折算，**漏了 `anchoredPosition (7.9,65.8)`** |

**② 核对后确认「本来就对」的**（都有断言守着，别再动）：
牌堆 `PlayerDeck` x[1615,1845] y[850,1080] **逐像素相等** · 张数底板/文字 · 能量水晶与底板（四块）·
任务点两个 + 接片（holder 自身的 ±89 px，不是接片的 ±37）· `END TURN` 130.7×80.4 ·
`SettingsBtn` 63.9² · 已出牌数三枚 x[13.5,91.5] y[548.2,568.2] · 里程碑骷髅/分数 ·
技能面板 `ActiveSkillDesc` x[671.8,1248.2]（y 从下 56.8–381.6 = 从上 698.4–1023.2）· 卡牌放大窗 `Card Display` 752×868。

**③ 核对出来的「原版有、我们没有」**（不是位置错，是缺件 —— 要做时照这张表）：
`TitleBackground` 头衔底条 311×42（我 x[54.2,365.2] y[1028.5,1070.5] / 敌 x[54.5,365.5] y[92.8,134.8]）·
`Avatar Item Small` 155.6×136.5（我 x[−19.7,136] y[948.1,1084.6]）·
`ChatButton` 64.4×61.9（我名牌下 x[50.9,115.4] y[880.2,942]）· `ShowCemeteryBtn` 64.5×64.2（敌 x[52,116.4] y[135.9,200.1]）·
`CenterCameraButton` 64.4×61.9（x[17.9,82.4] y[568.2,630]）· `OffensiveButton` 109×106.9（x[0,109] y[446.9,553.8]）·
任务点**数字** `QPText '0/3'` fs40.5（我 x[1841.8,1889.9] y[621.4,666.6]）· `Energy Accumulation ON/OFF` 77.8×80.1（x[1746.7,1824.4]）·
`OvertimeIndicator` 68.6×71 · 换牌/选卡/等待提示/回放条那几套。

**④ 我方**自加**的（原版没有，别拿原版去"修"）**：中上那行 `TurnLabel`（原版表示回合归属**只靠牌堆上的灯**，
全场景 `m_text` 里唯一的 TURN 是 `"END TURN"`）· `HintLabel` 提示行 · `ResultLabel`。

**⑤ 手牌数底板的位置仍是「我们挑的」**：静态 JSON 把它算在屏幕正中（x[816,1104] y[492.6,582.2]），
但它的祖先链是**纯 Transform**（`HandArea`/`PlayerArea`）、节点本身 `activeInHierarchy=False` —— 那串数不可信，
所以没照它摆（见第三节那一行）。

---

## 四、悬案（**两边证据冲突，别照猜着改**）

| 事项 | 证据 A | 证据 B | 结论 |
|---|---|---|---|
| **攻击方式选择器三钮的位置** | ① dump：近战在槽**左下角**、Active 与 Range **叠在槽中心** —— 但整条 `Drag Attack Selector` 的 `activeSelf=False`，这是**预制体静态值**，运行时很可能由代码重排 | `AttackSelector.cs` 文件头写着「原版三钮同位叠加是错的、以横排为准」，我们做成了横排 | **没定论**。要定得拿到真实战斗里的实况（需要对局数据，现在拿不到）。改动前先看 `资料/规则引擎_进度与交接.md` 里这条 |
| **牌堆底板位置** | ③ 绝对坐标表：`PlayerDeck` 贴屏幕右下角 | ① dump 里 `PlayerDeck` 是 stretch 容器（size −0.0,−0.1），**读不出绝对位置** | 按 ③ 做了。**单一来源**，看着不对就说 |
| ~~**名牌（督军信息条）的位置**~~ | `子代理读报_back左区_0827.md:46,69`：`NameBackground` **以 `PlayerInfo`/`EnemyInfo` 为中心**摆（`pos(0,0)`、anchor(0.5,0.5)），`PreserveAspect=1` → 实绘 382.3×126.3；我方 rect x[−38.1,397.6] y[951.4,1077.7]、敌方 x[−37.9,397.9] y[15.6,141.9] → **两边实绘左缘都在 −11.4**（贴屏幕左缘、出血 11 px）。`PlayerNameText` 文本中心 (237.25, 999.45)、`EnemyNameText` 有效中心 (240.19, 63.9)，**H=居中** | 我们 `BattleDriver` 原来把名牌按 `PlayerInfo` 的**左缘**（x=32）+ 中心 y=102.6 摆，且文字**左对齐** —— 出处 `arena_hud_layout.py` 那句「PlayerInfo (32,977) 260×75」，那是 `FrontCanvas/Alliance Panel` 底下**另一份** inactive 实例的坐标 | ✅ **2026-09-13 修好**：两块名牌都改成原版绝对位置（左缘 −0.005938 / −0.005833，中心 y01 0.060602 / 0.927083），文字改成**居中**摆。错出来的后果是**我方偏右 44 px / 偏高 37 px、敌方偏右 ~168 px**（敌方那个一眼就能看出来）。⚠️ **顺带治好了「骷髅压住名字」** —— 名牌下移 37 px 之后骷髅正好落在文字上方（和原版一样**翘在名牌上沿之外**）。断言钉了三条：两块名牌的位置 + 「骷髅下沿 > 文字中心 0.005」 |

---

## 五、这次量出来的中间值（做别的效果时可能用得上）

| 量什么 | 结果 | 怎么量的 |
|---|---|---|
| **卡框的透空窗口** | Ultramarines 卡框（1024²）不透明 bbox `x[201,808] y[56,991]`；窗口 `x[283,741] y[122,915]` → 占 bbox 的 `u[0.1349,0.8882]`、`v(从顶)[0.0705,0.9177]`；换成卡单位是 `x∈[−0.8197,0.8716] y∈[−1.3905,1.3690]` | 对卡框图 alpha 做「**先膨胀 2 px 封住抗锯齿的缝**、再从图外 flood fill」，剩下的闭合透明区就是窗口。⚠️ 不膨胀会从 1 px 的缝漏进去，把整张图判成窗口（第一次就栽在这）<br>⚠️ **2026-09-12：这个「窗口」不该被当成裁剪区用** —— 原版是「立绘按自己的矩形铺出去、由卡框 alpha 遮罩」（`2dcard` 规格 :310）。窗口是**拱形**，拿它当矩形裁剪框会让立绘的直角顶出拱形 |
| **卡牌插图 sprite 的真实矩形**（🆕 2026-09-12） | 纹理 **1024×1024**，sprite `textureRect = x176.5, y0, **670.5 × 1024**`（宽高比 **0.6548**） | `d:/2/新解包资源/assets_full/bundle_spacemarinesultramarinescardassets_assets_all/Sprite/SM_UM_inf_Aggressor Sergeant.json` 的 `m_RD.textureRect`。⚠️ 这个字段**旧解包（`解包整理`）里没有** —— 旧的直接把图裁成 660×1024 给我们，宽度都不对。卡本体宽高比是 0.628，**插图就是照着「铺满卡片」设计的** |
| **卡框上宝石的实心中心** | 红(近战) `(266,879)`、紫(远程) `(331,931)`、绿(生命) `(718,905)` px | 按颜色阈值取质心。换成卡面坐标后，和 JSON 那三个 container 的位置**差 ~0.022 卡宽**（≈3.6 px @ 手牌尺寸）—— 在质心法的误差量级内，**以 JSON 为准**，这条只是备查 |
| **原版插图库的匹配办法** | 1113/1131 张配上 | `工具/import_original_art.py` 的 `portrait_jobs()`：按卡名归一化子串匹配 + `difflib` ≥0.86 兜底。配不上的 18 张会打出来 |
| **配不上的 18 张** | `Exemplary Warrior` / `Predator Annihilator`(图库拼成 anihilator) / `Mega Blasta Deffkopta` / `Hellfire Torch` / `Hellfire Pit` / `Lord Commander` / `Master of Repentance` / `Dark Pact of Blood|Excess|Resilience` / `Lord Kaphrael` / `Veldras the Sublime` / `Armoury of Excess` / `Decadent Throne` / `Undying Legions` / `Moment of Grace` / `Rusted Vent` / `Awakened Obelisk` / `Protective Bio-structure` | 要么图库里没有，要么命名差得多。要补就得人工对 |

---

## 五点五、卡面装配（`2DCard`）—— 2026-09-12 对着**原版成品卡图**重对了一遍

**尺子**：`d:/2/Warpforge部队卡片/<阵营>/<1督军·2天赋·3部队·4计策·5防御卡>/*.png`
—— 原版拼好的 **PnP 成品卡图**（900×1200）。以前从没拿它比过，是我们卡面一直「差点意思」的根因。
渲染我们自己的卡对照：`CardFaceProbe.Run`（单卡渲染探针，输出 `_tmp_view/cardface/*.png`）。

| 元素 | 原版成品卡 | 我们（改完之后） | 出处 |
|---|---|---|---|
| 卡名 | **白**，居中，名字正中偏下 | ✅ 白（原来是**橙**） | 成品卡 + A3 表（⚠️ A3 把 name/army 两行的颜色**记反了**） |
| 阵营行 | **橙**，紧贴名字下方 | ✅ 橙（**原来根本没画这一行**） | `ArmyTextUnit`(0,+0.31) → y01 0.6394；`ArmyTextTactc`(0,+0.13) → 0.6935 |
| 效果文字 | 米白多行，居中 | ✅ 米白；**下沿**贴版面框底边、按可用高度自动缩字号 | `DescTextUnit`/`DescTextTactic`；框底 0.8466 / 0.9061 |
| 兵种行 | **橙**（`Infantry` 这种），排在效果文字**下面** | ✅ 橙（**原来也没画**） | `RaceText`(0,−0.452) → y01 0.8682；**战术卡没有这一行** |
| 立绘 | 只在卡框的**拱窗**里，卡框外是空的 | ✅ 立绘板 = 卡框 quad（2.07–2.12 宽），UV 取「原版 2.7484² 里落进板内的那一块」 | `CardImage` 2.7484²@(0,−0.044) + 卡框 alpha 遮罩 |
| 数值 | 左下红/紫、右下一绿、右侧一盾（**只单位卡有**） | ✅ 战术卡不再画数值和盾 | 成品卡 + A2 表 |
| 费用 | 右上蓝宝石 + 白数字 | ✅ 真图 `Card Frame Cost Icon` | A2 表 |
| 稀有度 | 底部中央小菱形 | ✅ 0.258×0.2335 | A2 表 |

**立绘到底怎么装（这一轮绕了三次，结论记在这儿，别再重来）**：
卡框 quad = 金属 bbox（2.07–2.12 × 3.257，居中 y −0.03）；立绘 = 原版那块
`CardImage 2.7484²@(0,−0.044)` 按自身比例 fit（单位卡 1.87×2.86），**再裁到卡本体矩形**
（原版卡根上有 RectMask2D：卡图比卡大、溢出先被卡矩形裁、再被卡框 alpha 掩）。
⇒ 三条都被实际踩过，各有各的坏法：
  ① 立绘板取「卡框 quad」→ 立绘比板小 → UV 出 [0,1] → 贴图 Clamp 把最外圈像素拉到卡边 → **一圈白边**
  ② 立绘 fit 到方形但不裁 → 方形立绘（战术卡）比卡还宽 → **画到卡框外面**
  ③ 立绘居中放、不限制高度 → 效果文字行数一多就**往上长、盖住阵营行**

**卡面立体感（用户叫「3DLit」，2026-09-13 查）** —— 机制查清了，做出来一半：

| 问题 | 答案 | 证据 |
|---|---|---|
| 立绘贴图的 **alpha 通道**是什么？ | **就是角色的抠图轮廓**：单位卡 91–97% 是全透明、亮的形状正是角色（光环/肩甲/武器/旗帜）；**战术卡 0% 透明**（整幅矩形插画，所以战术卡没有越界效果）。⚠️ 我们以前的导入脚本把它**当成「解码残渣」写成 255 了**，所以这个效果一直没做出来 | `d:/2/新解包资源/…bundle_*cardassets*/Texture2D/*.png` 直接读；13 个 bundle 抽查：单位 91–97%、战术 0% |
| 角色「越出卡框」怎么做的？ | **同一张立绘用两次**：底层忽略 alpha（完整插图，拱窗里的背景靠它）+ 前景层用 alpha（角色，盖在卡框上）。⚠️ 卡框的拱窗**自己也是全透明**的（实测 alpha=0），所以拱窗里必须有底层补背景 | 卡框 PNG 采样：窗心/窗上/窗下 = (0,0,0,0)，两翼金属 = alpha 255 |
| ⚠️ **游戏内**结构是这样吗？ | **不是**。游戏内 `Front` 只有**一层**立绘 + 一层卡框，且**卡框渲染在立绘之上**；立绘每卡只有 1 张 1024² 贴图，**没有**抠像/前层/3D 变体。上面那套「立绘压卡框」是**官方 PnP 印刷卡**（`D:/2/Warpforge部队卡片/`，900×1200）的合成顺序 —— 但**用户要的就是 PnP 那个观感**，我们照它做 | 原始 JSON `RectTransform_2610.json` 的 m_Children 顺序 + 运行时 dump `runtime_ui_dump_drive.tsv:461-466`；PnP 像素级定位：Titus 链锯剑越左上、Guilliman 金肩甲压内缘竖棱 |
| 游戏里的「3D 感」还来自哪？ | ① 整卡**刚性 3D 倾摆** `AutoCardRotation`（±10°，**跟位移**、不跟指针）② SDF 光影层 `Card Highlight And Shadow`（4.4281²）③ 战场单位是**真 3D 网格** + MatCap（`Card 3D WH40k.obj`、材质 `Card 3d Stealth`）| `MonoBehaviour_4458.json`（挂在 2DCard 上）、`AutoCardRotation.cs`、`BattleCardUI__SetCardImageTo3DBase.c` |
| 「1240×1240 画布」 | 本地**不存在**这个尺寸（只有 1024²/2048²/4096²/8192²） | 全 `assets_full` 的 Texture2D 尺寸扫描 |

**我们的实现（2026-09-13）**：`import_original_art.py` 保留 alpha **并二值化**（>127→255；不二值化的话那片半透明渐变会把卡面糊成马赛克）、
`Shaders/ArtOpaque.shader`（底层只取 RGB）、`CardView` 画两层（前景层只在 `card_cutouts.json` 里有的卡上画，665 张）、
清单由导入脚本生成。⚠️ **没做**：`AutoCardRotation` 的整卡倾摆、SDF 光影层、战场 3D 网格。
⚠️ **没调通**：另一条思路 `Shaders/FrameCutout.shader`（卡框按遮罩挖洞、立绘只画一次，没有重影）——
遮罩采样是错的，插图会碎成马赛克，留在代码里（`CardView.UseFrontLayer = false` 可切过去接着调）。

**踩过的两个大坑（都会让整批卡面错，但截图不容易看出来）**：
1. **战术卡框贴图没开 `Read/Write`** → `FrameUv` 量不出不透明包围盒、退回整张图
   → 卡框被画成一块 2.2452² 的方片（金属只占 60%），**所有战术卡**都错。
   修法：重跑 `Tools/CardPresentation/重设美术导入设置`（`ArtBaker.ApplyImportSettings` 里
   `isReadable = path.Contains("/cards/")`）。判据在 `FrameQuad` 上，探针里打印 `卡框实绘` 一看就知道。
2. **效果文字居中放** → 行数一多就往上长、把阵营行盖住。改成**下沿对齐 + 按可用高度缩字号**。
3. **状态描边（`_rim`）是块比方大的实心方片** —— 手牌里付不起的卡是 `Unplayable` 态，
   会在卡的透明角/透明边上**露出一圈灰白硬边**（用户 2026-09-12 报的「手牌有白边」就是这个）。
   修法两条：`Unplayable` **不描边**（置灰靠 `SetTint` 就够）；其余态用**羽化**贴图
   （`SoftRimTexture`，羽化宽度只能占「卡外那一圈」≈4.5%，给大了就等于没描边）。
   原版对应的是 `Card Highlight And Shadow`（4.4281² 的 SDF 软光/影），本来就不是硬方框。

---

## 六、**我们加的**（原版没有这一件 —— 别当成复刻，也别拿原版去「修」它）

| 元素 | 我们是什么 | 为什么加 |
|---|---|---|
| **提示行上的「本局用哪副牌」** | 提示行（两行棋盘中间那条缝，`_hintLabel` @ 0.5 / 0.507）的**休息态文字**，三种：<br>· `本局用你编的「X」· N 张战术/防御卡引擎还不支持，没上场`<br>· `你的卡组「X」不合法（原因）—— 本局退回自动凑的一副`<br>· `卡组存档读不出来（原因）—— 本局自动凑了一副`<br>游戏过程中的临时提示（选目标 / 不能行动…）盖在它上面，用完自动落回来（`BattleDriver.SetHint`） | 引擎**只收单位卡**：全卡池 587 单位 / **448 战术** / 39 防御 / 57 督军，战术卡是 `ErrUnimplemented`、防御卡引擎压根不认识 —— **一副合法的 30 张卡组，实际能上场的是里面的单位卡**。这件事原来只写在 `Debug.LogWarning` 里，打包后玩家一个字都看不到，而画面上**牌确实少了**（看起来像卡组没生效）。⚠️ 结算时会**真清空**（不然会从结算面板底下透出来）；卡组名**截断 14 字**（`Label` 是 NoWrap 的）。⛔ 原版全卡种都能上场，**没有也不需要这一行** |
