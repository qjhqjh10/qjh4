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
| **任务点** | `UI_Quest_Points` 97.7²，玩家在能量上方 37.4 px、敌方在下方 36.4 px | ① `.../ManaHolder/QuestPointsHolder` | 两个 `ImageQuad` |
| **督军名牌** | 实绘 **382.3 × 126.3** px（贴图 442×146，PreserveAspect） | ③:16 + dump `NameBackground 435.7×126.3` | `worldHeight = 126.3/108` |
| **回合灯** | **23.4 × 23.4**（anchor 矩形 22.8×34.5，图 60×59 KEEP_ASPECT 缩进去） | ① `DeckAndEnergyImage/YourTurnImage` | `DeckLightPx = 23.4` |
| **牌堆底板** | `PlayerDeck` 230×230 贴**右下角**；`EnemyDeck` 200×200 贴右上角 | ③ 绝对坐标表 | `MyDeckX01/Y01` |
| **卡背** | `Cardback` 2.1739×3.1364（×100 → 217×314 px） | ③ A2 表 | `DeckCardPx = 314` |
| **攻击方式选择器** | 槽 621.7×122.2；底板实绘 514.8×125.4（`780×285 × localScale 0.44 × 1.5`）；按钮 118.8²；黄圈 176.2² | ① `Drag Attack Selector` 子树 | `AttackSelector.cs`（尺寸全对） |
| **结算面板** | `EndBattlePanel` 子树（Background 3840×2160 / Video Image 1920×1080 @(−6,64) / SkullsHolder 648.1×52.4 / skull 64.3×71.2 / Trophy 60.1×60） | ① dump | `EndPanel.cs` |
| **卡牌放大展示窗** | 遮罩 α0.7725；`Card Display` 752×868 居中；放大卡 ≈467×743 px；下方文字条 y≈921~1078 | ③ front弹层报告 §一 + ① | `CardDisplayWindow.cs`（卡高 743 px 有断言） |

---

## 三、还没对上的（**这就是待办**）

| 差什么 | 原版实测 | 出处 | 我们的现状 |
|---|---|---|---|
| **能量底板** | `Card Frame Cost Icon`（sprite 248×244，实绘 91.3×91.3）垫在水晶下 | ① `.../ManaHolder/Energy Player` | ⚠️ **2026-09-12 更正：原来那句「本地只有 sprite 元数据、没有导出的 PNG」是错的。** 图在 → `Resources/Art/ui_deck/Card_Frame_Cost_Icon.png`（248×244，已带 .meta，**运行时 `CardArt.DeckUi("Card_Frame_Cost_Icon")` 直接可取**）。错因：只在 `素材/Warpforge原版/UI图集/去重资源/Sprite/` 那个镜像目录里看到 `.json` 就下了结论，**没查我们自己的索引**（`资料/索引与盘点/解包资源使用地图.md:117` 白纸黑字写着它 2026-08-17 就切好了、状态 ✅ 在用）。**只差把它画上去** |
| **阵营资源：信仰 / 灵石** | `FaithHolder` 117.9×149.3（`40k_Battle_Display_Faith`）、`SpiritStoneHolder` 112.1×116.6（`UI_Energy_Eldar`）+ 石 51.0×63.0 | ① `.../ManaHolder` 子树 | 没有。**引擎侧也没有对应机制**（原版是部分阵营的资源） |
| **加时标记** | `OvertimeIndicator` 68.6×71.0（`40k_icon_overtime`） | ① `.../Energy And turn holder` | 没有（引擎没有加时机制） |
| **牌库张数底板** | `Player Deck Size Container` 20×59.1（`40K_display`），文字 fs 42 | ① `.../PlayerDeck` | ⚠️ **2026-09-12 更正：图也在**（`40K_display.png` 442×112，索引 `:117` 同样列为 ✅ 在用，切好的在 `d:/2/Warpforge_tools/data/ui_extract/…/Sprite/`）。目前只同步到 `Assets/CardPresentation/Art/原版/去重资源/`，**还没进 `Resources/`**，要用得先拷一份过去 |
| **手牌数底板** | `CardsInHandText/Bg (1)` 288.2×89.6 | ① `.../BottomAnchor/PlayerArea` | 只有纯文字 |
| **本回合已出牌数** | `CardsPlayedInTurnHolder` 3×20×20（inactive） | ① `.../LeftArea` | 没有（引擎没记这个数） |
| **边角按钮群** | `SettingsBtn` 63.9²、`ChatButton` 64.4×61.8、`ShowCemeteryBtn` 64.5×64.2、`CenterCameraButton` 64.4×61.8、`OffensiveButton` 109×106.9 | ① `.../LeftArea`、`.../RightArea` | 都没有 |
| **墓地日志** | `CemeteryLogPanel`：Frame 863×1032.5 / BG 769.5×454.4 + 四条 `40k_battlelog_frame_*` | ① `.../LeftArea`、③ §一 | **整块没有**（图有：`40k_battlelog_frame_{Bottom,Left,Right,TOP}.png` 在切片库里） |
| **换牌 Mulligan** | `Mulligan`：`MulliganText` 1344×79.4 + 继续按钮（`activeSelf=True`） | ① `.../FrontCanvas` | **整块没有**（引擎也没有换牌流程） |
| **选卡菜单** | `ChooseCardMenu/ButtonsGroup`：Continue 548×75.9 + BG 577.5×63.8 + 圆钮 80.5×79.6 | ① `.../FrontCanvas` | 没有（引擎没有「选一张牌」的效果） |
| **单位语音条** | `Unit Chat/PlayerChatDisplay` 648.8×236.5（`40k_voicelines_radio` 766×280）+ 敌侧同尺寸 | ① `.../FrontCanvas` | 没有（原版有语音，我们没接音频） |
| **回放条** | `ReplayButtons` 4 枚 79.8×48.6（`40K_replay_bt_*`） | ① `.../BackCanvas` | 没有（单机没回放） |
| **等待提示 / 通用弹窗** | `WaitText` 1344×79.4 + `Dark Shade` 3963.5×3366 + `40k_popup` 1323×90 | ① `.../BackCanvas` | 没有 |
| **头像块** | `Avatar Item Small` 155.6×136.5 + `Player Profile Border` | ① `.../LeftArea/EnemyInfo` | 没有（原版是玩家头像，单机没数据） |
| **里程碑骷髅** | 骷髅 65.4×54.1（`40k_battle_Win Skull`）+ 分数 `x3` fs 35 | ① `.../LeftArea/PlayerInfo/Milestones` | 结算面板里有骷髅，姓名牌上没有这一块 |
| ~~**稀有度钻石**~~ | `Rarity` 0.4×0.4 @(0,−1.458)，图 `1_40k_cardframe_rarity_common` 等 5 张（浅蓝/绿/紫/金/橙红） | ③ A2 表 | ✅ **2026-09-12 做完**：`CardView.GemMesh` + `CardArt.DeckUi`，按稀有度取那 5 张。⚠️ **它和「卡框分档」是两件事**：卡框分档改的是**框的形制**（tier1 素 → tier4 华丽），而卡框纹理上那颗菱形是**空的暗色凹槽**（四档都一样）；颜色在这颗宝石上。对照图 `资料/留档_排查证据/卡面组装_0912/gems.png` |
| ~~**卡框分稀有度**~~ | 卡框原版按 tier1–4 分四张（`CardFramesSO`：13 阵营 × 4 档 × 4 个槽 = **208 个 sprite**） | `03_界面UI/通用静态资源/MonoBehaviour/CardFramesByArmy_6819405058091919240.json` | ✅ **2026-09-12 做完**：四档全导进来（`frame_<阵营>_tier1..4.png`），`CardArt.Frame(阵营, 稀有度)` 取。**稀有度→tier 的映射是对照原版卡面实测的**（common→1 / rare→2 / epic→3 / legendary→4，对照图 `资料/留档_排查证据/卡面组装_0912/tier_map.png`）。⚠️ `special`（39 张）没实测，先按 tier4 |

---

## 四、悬案（**两边证据冲突，别照猜着改**）

| 事项 | 证据 A | 证据 B | 结论 |
|---|---|---|---|
| **攻击方式选择器三钮的位置** | ① dump：近战在槽**左下角**、Active 与 Range **叠在槽中心** —— 但整条 `Drag Attack Selector` 的 `activeSelf=False`，这是**预制体静态值**，运行时很可能由代码重排 | `AttackSelector.cs` 文件头写着「原版三钮同位叠加是错的、以横排为准」，我们做成了横排 | **没定论**。要定得拿到真实战斗里的实况（需要对局数据，现在拿不到）。改动前先看 `资料/规则引擎_进度与交接.md` 里这条 |
| **牌堆底板位置** | ③ 绝对坐标表：`PlayerDeck` 贴屏幕右下角 | ① dump 里 `PlayerDeck` 是 stretch 容器（size −0.0,−0.1），**读不出绝对位置** | 按 ③ 做了。**单一来源**，看着不对就说 |

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

## 六、**我们加的**（原版没有这一件 —— 别当成复刻，也别拿原版去「修」它）

| 元素 | 我们是什么 | 为什么加 |
|---|---|---|
| **提示行上的「本局用哪副牌」** | 提示行（两行棋盘中间那条缝，`_hintLabel` @ 0.5 / 0.507）的**休息态文字**，三种：<br>· `本局用你编的「X」· N 张战术/防御卡引擎还不支持，没上场`<br>· `你的卡组「X」不合法（原因）—— 本局退回自动凑的一副`<br>· `卡组存档读不出来（原因）—— 本局自动凑了一副`<br>游戏过程中的临时提示（选目标 / 不能行动…）盖在它上面，用完自动落回来（`BattleDriver.SetHint`） | 引擎**只收单位卡**：全卡池 587 单位 / **448 战术** / 39 防御 / 57 督军，战术卡是 `ErrUnimplemented`、防御卡引擎压根不认识 —— **一副合法的 30 张卡组，实际能上场的是里面的单位卡**。这件事原来只写在 `Debug.LogWarning` 里，打包后玩家一个字都看不到，而画面上**牌确实少了**（看起来像卡组没生效）。⚠️ 结算时会**真清空**（不然会从结算面板底下透出来）；卡组名**截断 14 字**（`Label` 是 NoWrap 的）。⛔ 原版全卡种都能上场，**没有也不需要这一行** |
