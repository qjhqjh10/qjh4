# B131_A5 · 战斗 HUD 版面「数值参数」审计（只读 · 2026-11-03）

> 范围 = 手牌 / 顶栏 / 战场槽位 / 换牌 / 大卡展示 / 战斗日志 / Toast / Loading / HUD 层。
> 攻击·选择器已由 `B129` 审过，本表不重查。行号 2026-11-03 现读；原版侧现读 `bundle_scenes_scenes_battlearena1` 的
> `RectTransform/*.json` / `MonoBehaviour/*.json` 与场景 YAML（`assetripper_out/.../Battle Arena 1.unity`，含 `m_LocalScale`）。
> ⚠️ 我方绝对值只对照「原版字段 + 父链缩放」算出来的设计 px；**⛔ 没有用 `runtime_ui_dump` 的绝对列**（那份表头错位且 Canvas 映射不可靠）。

| # | 这一格是什么 | 我们落点(文件:行号) | 原版判据(资产路径+字段 或 文件:行号) | 原版值 | 我们值 | 判定 |
|---|---|---|---|---|---|---|
| 1 | 手牌我方卡宽 | `Hand/HandLayout.cs:87,96` | `MonoBehaviour_5271.m_scale` = 0.73000001907；`m_scale × 2.0927 × 108` | **164.96 px** | 0.73005 × 2.0927 × 108 = **165.0 px** | ✅一致 |
| 2 | 手牌敌方卡宽 | `Hand/HandLayout.cs:136`（`ConfigureForEnemy:407`） | `MonoBehaviour_4053.m_scale` = 0.54000002146 | **122.05 px** | 0.54 → **122.05 px** | ✅一致 |
| 3 | 手牌中心间距（我/敌） | `HandLayout.cs:172`；`ConfigureForEnemy:410` | `MB_5271.m_betweenElementsSpacing` = 1.45000004768；`MB_4053` = 0.59999996424 | 1.45 / 0.6 | 1.45 / 0.6 | ✅一致 |
| 4 | 弧高上限 / 张数满档（我/敌） | `HandLayout.cs:174,187`；`ConfigureForEnemy` | `MB_5271.m_maxHeight` = 0.69999998808、`m_numberOfCardsForMaxHeight` = 12；`MB_4053` = **−0.77999997139** / 10 | 0.70·12 / **−0.78**·10 | 0.70·12 / −0.78·10 | ✅一致（负值=敌方向下凹） |
| 5 | 扇形 lookTo 距离（我/敌） | `HandLayout.cs:209` 45.33；`ConfigureForEnemy:413` 10.23 | `MB_5271.m_verticalOffsetLookTo` = **−330.0**；`MB_4053` = **+74.5**（×14.835/108） | 45.325 / 10.234 我们的单位 | 45.33 / 10.23 | ✅一致 |
| 6 | 布局宽上限 / 小屏档 | `HandLayout.cs:158,161`；`ConfigureForEnemy:405-406` | `MB_5271.m_maxLayoutSize` 0.59 / `…SmallScreen` 0.51；`MB_4053` 0.51 / 0.51 | 0.59·0.51｜0.51·0.51 | 同 | ✅一致 |
| 7 | 悬停抬起量 | `HandLayout.cs:265,798`（`HoverLiftWorld=30`） | `VarsDesktop.cardInHandShownYOffset` = 30.0；消费者 `CardScript__ShowCardInHand.c:69-79` 加在**世界 y** | 30 原版世界单位 = **445 px** | 30 × 14.835/108 = 4.12 我们的单位 = **445 px** | ✅一致（2026-11-02 已修） |
| 8 | 悬停缩放 | `HandLayout.cs:268,800` | `VarsDevice.cardInHandShownScale` = 1.3；`ShowCardInHand` 写 `Vector3.one × 1.3`（**绝对值**） | **1.3**（静止 0.73 ⇒ 比值 1.7808） | `hoverScale × LayoutSpace.Scale` = 1.3 | ✅一致（2026-11-02 已改绝对值） |
| 9 | 悬停那张的 z 提前量 | `HandLayout.cs:285,805` `frontZ = 0.5` | `CardScript__ShowCardInHand.c` `z − 0.01`（`DAT_1834b2dbc`） | 0.01 原版世界单位 = **0.148 px** | 0.5 我们的单位 = **0.069 原版单位 = 54 px** | 🟠有差异（大 6.9 倍；分层靠渲染队列，观感无碍，仅登记） |
| 10 | 手牌布局基线 y | `HandLayout.cs:132` `DefaultBaselineY` | 原版 `Transform_1455.localPosition.y = 1.1 × 108`（`HandArea` scale 108） | **118.8 px 距底** | **131.35 px 距底**（+12.55 = 卡半高） | 🟠有差异·🔶 自己设计（用户 2026-10-22 拍板「保真档」） |
| 11 | 手牌卡间 z 步长 | `HandLayout.cs:299` | 原版 `GetPosition` 里 **无逐张 z**（靠嵌套 Canvas 排序）；旧记录 0.001 | 0.001（原版无等价物） | 0.08（只管几何纵深） | 🔶 自己设计（已在代码标） |
| 12 | 溢出只压间距不缩卡 | `HandLayout.cs:529-549` | `CardsHorizontalLayout__GetPosition.c:201-220` `spacing = cap ÷ n`，无按张数缩放项 | cap ÷ n | cap ÷ n | ✅一致 |
| 13 | 选中让位量 | `HandLayout.cs:273`（`SelectedExtraWorld`） | `VarsDesktop.extraSpaceOnSelectedCard` = 2.0（世界单位） | 2.0 = **29.7 px** | 2.0 × 14.835/108 = 29.7 px，且只在压缩态让 | ✅一致 |
| 14 | 结束回合钮框 + 中心 | `BattleDriver.cs:6596,2650` | `RectTransform_2913`：sd **130.70199585 × 80.43199921**；chain_rect 中心 (1848.25,455.55) | 0.96263 / 0.57819 | 0.96263 / 0.57819 | ✅一致 |
| 15 | 结束回合钮命中框 | `BattleDriver.cs:6597-6610` | `MB_4543/4895.m_RaycastPadding = (−14.19,−30.30,−26.19,−18.40)`（负=外扩） | 130.702+14.19+26.19 = **171.08 × 129.13** | 同式（`EndTurnBtnHitPx`） | ✅一致 |
| 16 | 能量水晶框 | `BattleDriver.cs:2694` | `Energy Player Accumulation ON/OFF` sd (−22.213993,−19.886978) + 父 `Energy Accumulation` 100×100 | **77.786 × 80.113** | 77.786 × 80.113 | ✅一致 |
| 17 | 能量球/数字的两次偏移 | `BattleDriver.cs:2669-2671` | `MB_4372/4373` 子树：`{Player,Enemy}Mana/ManaHolder.ap` = (1.8,−0.5) / (3.284,0.15)；`ManaText.ap` = (−1.5,0.594) | 同左 | 同左 | ✅一致 |
| 18 | 任务点 / 接片 | `BattleDriver.cs:2713-2718` | `RT_2768/3423` sd **97.708²**；`BackgroundJoin` sd **35.207 × 23.89** | 97.708 / 23.89 | 97.7 / 23.9 | ✅一致（截位 ≤0.01） |
| 19 | 信仰 / 灵魂石 / 宝石 | `BattleDriver.cs:2735-2745` | `RT_2968/2885` **117.943 × 149.271**；`RT_2692/2756` **112.143 × 116.588**；`SpiritStone` **51.028 × 63.015** | 同左 | 117.9·149.3 / 112.1·116.6 / 51.028·63.015 | ✅一致 |
| 20 | 牌堆底板（我/敌） | `BattleDriver.cs:2467,2552` | `RT_3292 PlayerDeck` sd **230²**；`RT_2964 EnemyDeck` sd **200²** | 230 / 200 | 230 / 200 | ✅一致 |
| 21 | 牌库张数底板高 + α | `BattleDriver.cs:2563,2578` | `Player Deck Size Container` sd **(20,0)** + `AspectRatioFitter` 4.03465 ⇒ 59.11/52.05；`m_Color.a` 0.6941177 | 59.11·52.05·α0.69412 | 同 | ✅一致 |
| 22 | 骷髅 / 分数 | `BattleDriver.cs:2596,2600` | `RT_3549 MatchSkulls Icon` **65.39 × 54.1375**；`RT_3467 Score` **94.461 × 47.311** | 高 54.1375 / 94.461×47.311 | 54.14（高，宽由 PA） / 94.46×47.31 | ✅一致 |
| 23 | 名牌底（我/敌） | `BattleDriver.cs:28792-28829`（`HudPlateScale` 29877） | `RT_3293/3331 NameBackground` **435.7 × 126.3**，父 `PlayerName/EnemyName` `m_LocalScale = 0.8` | **348.56 × 101.04** | 348.56 × 101.04（0.8 已计入） | ✅一致（A1512 已修） |
| 24 | 战场槽距/额外距/缩放/深度（我/敌） | `Board/ArenaSlots.cs:56-86` | `MB_4372/4373`：`MinionSeparation` **0.82 / 1.53** · `minionExtraDistanceFromHero` **0.09 / 0.13** · `desiredScale` **0.36 / 0.69** · `heroScale` **0.4 / 0.77** · z **−6.655 / 1.043** · `heroExtraOffset.z` **−0.42 / −0.75** | 同左 | 逐值同 | ✅一致 |
| 25 | 战场行心 y（我/敌） | `Board/BoardLayout.cs:80` `lineY` | 透视实测 我方卡心 **636 px 距顶** / 敌 **425 px**（`3DBody…规格.md` §四之五） | 0.4110 / 0.6063 | 0.4110 / 0.6063 | ✅一致 |
| 26 | 敌方槽位不镜像 | `ArenaSlots.Position`（:108-113） | `MinionManager__FillMinionPositions.c:33-35,55-56` 两侧同一式（左 −x） | 不镜像 | 不镜像 | ✅一致（B8_M 已结案） |
| 27 | 换牌文案框 / 两行字 | `Battle/MulliganPanel.cs:339,352,399` | `RT_2828 MulliganText` **1344 × 79.44**；子 `Text` **1307.06×69.44**、`TurnText` **1307.06×54.17** | 同左 | 1344×79.4 / 1307.064×69.44 / 1307.06×54.17 | ✅一致 |
| 28 | 换牌三颗钮 | `MulliganPanel.cs:422,430,431,432` | `MulliganContinueButton/Button` **577.5×63.84**；`CircleButton` **80.47×79.64**；`HideMulliganButton` **82.95×79.60** | 同左 | 577.5×63.84 / 80.469×79.6 / 82.947×79.603 | ✅一致 |
| 29 | 换牌行心 | `MulliganPanel.cs:617` `RowCx=960, RowCy=511` | `Transform_1292 MulliganAnchor.m_LocalPosition = (0, 29, 0)`，父整屏 pivot 居中 | 屏心 +(0,−29) ⇒ (960,511) | (960,511) | ✅一致 |
| 30 | 换牌卡宽 | `MulliganPanel.cs:633` **292** | `MB_4881.m_scale` = **183.41**（与选牌 `MB_4052` 逐字同）；探针：卡本地单位 ×183.41 → **430×609 px**（`panel_0914b/报告.md:75,77`） | 算法 **≈383.8~430 px** | **292 px**（用户实拍 240 帧px × 1080/887） | 🟠有差异（差 31%~47%；A1749 待真机复测定稿） |
| 31 | 换牌行间距 | `MulliganPanel.cs:645` **502.2** | `MB_4881.m_betweenElementsSpacing` = 502.2；运行期探针 n=2..3 中心距 **502.1 px** | **502.2 px** | 502.2 px（但用户实拍量到 ~400） | 🟠有差异（算法 502.2 vs 实拍 ~400；A1749 待定） |
| 32 | 大卡展示框 | `Battle/CardDisplayWindow.cs:7-10` | 场景：`Card Display` **752×868**；`LowerSection` **1320×137**；`FlavourTextBG` **1320×178**；`Voice Over Button` **88.655²** | 同左 | 同左 | ✅一致 |
| 33 | 战斗日志面板 | `Battle/BattleLogPanel.cs:65,119,131,145,221-224` | 场景：`CemeteryLogPanel` **794.069**；`BG` **769.5439×454.3976**；`CemeteryActions` **748.006×431.344**（10 行 ⇒ 行高 43.134）；四框 707.302×65.056 / 707.302×46.795 / 97.651×571.236 / 63.049×578.654 | 同左 | 794.1 / 769.5×454.4 / 748×43.134 / 逐值同 | ✅一致 |
| 34 | Toast 根 + 寿命 | `Battle/ErrorMessageBanner.cs:240,246,226-231` | `RT_3373` **1920.69995×336.64001**、`ap.y −213.59973`；`timeToStartFading 2.0 + timeToFade 0.25` | 1920.7×336.64 / 2.25 s | 同 | ✅一致 |
| 35 | 进战斗黑场淡出 | `BattleDriver.cs:17212-17213` | `LoadingFade` 常量 `0x1834B2EC0` = **1.25f**；`Awake` α=1.0 | 1.25 s / α1 | 1.25 s / α1 | ✅一致 |
| 36 | HUD 图/字/底板三层 z | `BattleDriver.cs:31603,31613,31619` | 原版用 Canvas 兄弟序（无世界 z 等价物） | 无等价物 | 0.3 / 0.0 / 0.6 | 🔶 自己设计（**代码里没打标记**，见顺手发现 1） |

## ❓查不到的搜索记录

- **换牌卡的运行时绝对尺寸**：搜过 `panel_0914b/mulligan_tree_*.tsv`（`MulliganAnchor.childCount=0`，原版这一阶段不建卡）、`battle_hand_*.tsv`（7 份全只有表头，手牌注入一直失败）、`MB_4881` 静态字段 —— **拿不到「原版换牌卡实际多宽」的直接读数**；只有「算法 183.41 → 430」与「实拍 → 292」两个互斥来源（见 #30/#31）。
- **HUD 绝对屏幕坐标的独立复核**：`runtime_ui_dump_Battle_Arena_1.tsv` 的 Canvas 运行时 `ap=−343 / sd=1920×1080 / anchor(0,0)`，按该节重建出的 `Right Anchor` 会落到 x=2396（屏外）⇒ **这套 Canvas 映射不可用于绝对 px**；改用「原版字段 + 父链缩放 + `子代理读报_back右区_0827.md` 的 chain_rect」互证。搜索目录：`资料/原版参照图/Unity参照管线_0825/data/`、`资料/战斗规格/战斗重建_0827/`。

## ⚠️ 顺手发现

1. **`HudImageZ=0.3` / `HudLabelZ=0` / `HudDecorZ=0.6`（`BattleDriver.cs:31603/31613/31619`）是「机制不同但视觉等价」的自设计层序，却没打 `🔶 自己设计`**（同族的 `HandLayout.zOrderStep` 已打）。按 CLAUDE 铁律 18 应补标。
2. `MulliganPanel.RowSpacingPx=502.2` 与 `CardWidthPx=292` **互相不自洽**：502.2 的间距配 292 的卡会留 210 px 空隙，而算法（同一 `MB_4881`）给出的卡宽/间距是自洽的（430 / 502）。这两条应**同批**定稿，⛔ 别只改一条。
3. `BattleDriver.cs:3555` 附近 `ApplySmallScreenHandScale` 会按屏宽重写手牌字段，但 `HoverLiftPxForDevice`（移动端 `cardInHandShownScale=1.7` / 触屏 y 偏移）**仍未接线**（`HandLayout.cs:265` 自陈）⇒ 桌面档一致，**移动档没接**。
