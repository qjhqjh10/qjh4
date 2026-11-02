# B120-B · 战斗 HUD 布局参数审计（原版 vs 我们）

> 只读审计（2026-11-02）。⛔ 未改任何 `.cs` / `.tsv` / 资产 / 场景；⛔ 未跑 Unity；⛔ 未动 git。
> 判据优先级：`decomp_full` > `out_il2csharp` > 签名桩 > `assets_full` / `assetripper_out` > 既有判据（**用前现读代码**）。
> ⚠️ 既有判据会过期 —— 下表所有「我们值」的 `文件:行号` 都是**本次现读**；「原版值」若来自既有判据，已注明其原始出处。
> 一句话结论：**画布/适配/按钮四态/名片/详情窗/日志/结算/设置/等待/教程遮罩这些主参数基本已对平**，剩余偏离集中在
> **技能面板两格、教程高亮的 relation 偏移与淡出、结算面板三处、战斗日志行自适应、设置窗小屏缩放、错误横幅的疲劳节拍**。

---

## 参数表

| 参数 | 原版值 | 原版出处（文件:行 / 资产路径+字段） | 我们值 | 我们出处（文件:行） | 判定 | 备注/影响 |
|---|---|---|---|---|---|---|
| ①画布 · `Canvas.renderMode` | `1` = ScreenSpaceCamera | `Battle Arena 1.unity` GO 983 Canvas `&2572` `m_RenderMode: 1` | ScreenSpaceCamera | `Battle/HudCanvas.cs:107` | ✅一致 | — |
| ①画布 · `Canvas.planeDistance` | `100` | 同上（Canvas `&2572`） | **20.3** | `HudCanvas.cs:109` · 常量 `:54` | ✅一致 | **有意差异**：我们 HUD 是正交相机（z=−20）+ 世界空间 quad 靠 z 分层，`planeDistance` 必须 = `HudImageZ + 20`；⛔ 别照原版改 100（会把整块画布甩到 z=80） |
| ①画布 · `Canvas.sortingOrder` | `0` | 同上 | `0` | `HudCanvas.cs:110` | ✅一致 | — |
| ①画布 · `CanvasScaler.uiScaleMode` | `1` = ScaleWithScreenSize | CanvasScaler `&4731` `m_UiScaleMode: 1` | ScaleWithScreenSize | `HudCanvas.cs:113` | ✅一致 | — |
| ①画布 · `referenceResolution` | `1920 × 1080` | 同上 `m_ReferenceResolution` | `1920 × 1080` | `HudCanvas.cs:57` · `:114` | ✅一致 | — |
| ①画布 · `screenMatchMode` / `matchWidthOrHeight` | `1` = **Expand**；`m_MatchWidthOrHeight: 0`（Expand 档**不参与运算**） | 同上 | Expand | `HudCanvas.cs:115` | ✅一致 | 实测口径 `scaleFactor = min(W/1920, H/1080)`；16:9 / 16:10 / 21:9 整幅 |
| ①画布 · `referencePixelsPerUnit` | `100` | 同上 | `100` | `HudCanvas.cs:116` | ✅一致 | — |
| ①画布 · `Canvas.additionalShaderChannels` | **`25`** | 同上 `m_AdditionalShaderChannelsFlag: 25` | **未设**（Unity 默认 0） | `HudCanvas.cs:105-116` 现读**无**该赋值（全树 `grep additionalShaderChannels` = 0 命中） | ❌不一致 | 无观测影响（TMP / UI/Default 都不读 TEXCOORD3/4 与 NORMAL）。改动要在 `Ensure()` 插行 ⇒ 推动 `HudCanvas.cs:90/:445` 两条账落点，须调度台在「没有 `.cs` 写手」时做 |
| ①画布 · 子画布是否带 CanvasScaler | 全场景**只有一个** CanvasScaler（根上） | 场景 CanvasScaler 实例计数 = 1 | 子画布只 `AddComponent<Canvas>()` | `HudCanvas.cs:549` | ✅一致 | — |
| ①适配 · `minAspectRatio` | **`1.3333330154418945`**（场景值；ctor 是 `1.3333334`） | MB `5148`（GO `BattlePrefab` = pid 330） | `1.3333330154418945f`（取**场景**那一档） | `Battle/ScreenAspectRatioController.cs:118` | ✅一致 | — |
| ①适配 · `maxAspectRatio` | **`2.444443941116333`**（场景值；ctor 是 `2.4444447`） | 同上 | `2.444443941116333f` | `ScreenAspectRatioController.cs:115` | ✅一致 | — |
| ①适配 · 黑边两支公式 | 宽过 max：`rect=(f,0,1−2f,1)`，`f=(1−max/aspect)×0.5`；窄过 min：`rect=(0,f,1,1−2f)`，`f=(min/aspect−1)×0.5`；**严格不等** | `ScreenAspectRatioController__AdjustCameraViewport.c` | 逐式相同 | `ScreenAspectRatioController.cs:189-203` | ✅一致 | 16:9 / 16:10 / 21:9 整幅；22.5:9 左右黑边 `x=0.0111112`；5:4 上下黑边 `y=0.0333332` |
| ②锚点 · `PlayerInfo` / `EnemyInfo` 运行时位置 | `ap = (−240,−330)` / `(−240,+330)`（静态被脚本改掉） | `runtime_ui_dump_Battle_Arena_1.tsv:105-133` | 同（按运行期值摆，⛔ 不是 `Alliance Panel` 下那两份 inactive 实例） | `Battle/BattleDriver.cs:28769-28776` 段 | ✅一致 | — |
| ②尺寸 · `NameBackground` 屏框 | **348.56 × 101.04**（= 裸 `m_SizeDelta` 435.7×126.3 × 父链 `m_LocalScale 0.8`）；`PA=1` 实绘 **305.888×101.04** | `menu_dump … --rt 3042`（battlearena2）· `RectTransform` 现读 | `101.04f / 108f` 高 + `HudImage` 锚 `(0,0.5)` 左缘 `27.0559 / 26.7561` | `BattleDriver.cs:28812`（敌）· `:28830`（我） | ✅一致 | 2026-10-19 `A1512/A1254` 修过 |
| ②尺寸 · `PlayerNameText` | 框 `199.38×35.93` · 文字心 `(225.74343, 1002.47582)` | `menu_dump`/`RectTransform_3016` | 同 | `BattleDriver.cs:28838` | ✅一致 | — |
| ②尺寸 · `EnemyNameText` | 框 `290.94×36.84` · 字心 `(228.17147, 66.88264)`（`m_margin (43.2563,0,70.5767,0)×0.8`） | `RectTransform_3568` · `MB_4031` | 同 | `BattleDriver.cs:28825` | ✅一致 | — |
| ②尺寸 · `TitleBackground` | `311×42` @ `ap (30,−35)` ×0.8 ⇒ **248.8×33.6** | `menu_dump` 绝对框 · `MB_3797/3887` | 我 `(79.300195,1025.70515)` / 敌 `(79.6,89.95257)`，`248.8×33.6` | `BattleDriver.cs:30004-30005` | ✅一致 | 无称号 ⇒ **整块关**（`SetProfileTitle.c` `SetActive(!IsNullOrEmpty)`） |
| ②尺寸 · `TitleText` | 框 `187.40×36.84` · fs **30.55** · auto `[2,35]` base36 · H 中 / V Midline · 橙 `(1,0.6306,0.4198)` | `RectTransform_2893/3134` · `MB_3797/3887` | 同（`TitleFontPx = 30.55`） | `BattleDriver.cs:31631` · `:30039` · `:30045` | ✅一致 | — |
| ②尺寸 · `Avatar Item Small` 框 | `155.64×136.49` @ `apos (−121.5,−1.8)`；框 `Border` 视觉 **88.72744×99.12519** | `menu_dump` · `RectTransform` | `_avatarItem` 88.7274449×99.1251923 @ `(37.297268,960.964654)` / `(38.397073,25.452069)` | `BattleDriver.cs:30131` · `:30133` | ✅一致 | — |
| ②尺寸 · HUD 头像立绘 | 视觉 **158.60031 正方**（布局 124.512×79.30 × 自己 2.0，父链 0.8 已含） | `menu_dump` `Image` 行 | `HudAvatarSize = 99.1251923 × 2 × 0.8` | `BattleDriver.cs:29884-29886` | ✅一致 | — |
| ②尺寸 · `ChatButton` | `64.443×61.846` @ `(19.22,110)` · 锚/pivot (0,0) · 图 `40k_UI_bt_voicelines` | `RectTransform_2984` · `MB_5007` | `HudAbs(…, 50.920195, 880.154, 64.443, 61.846)` + `SetNodeRect` | `BattleDriver.cs:30197` · `:30206` | ✅一致 | 画出来是 128² 贴图 `PA=1` 内接的 61.846 正方（与框不同，⛔ 别混） |
| ②尺寸 · `ShowCemeteryBtn` | `64.478×64.17` @ `(34.2,−140)` · `m_RaycastPadding` 各 **−8** · 图 `40k_UI_bt_battlelog` | `RectTransform_2586` · `MB_4020` | 同 + pad 各 −8（命中 `80.478×80.170`） | `BattleDriver.cs:6576-6577` · `:29520` | ✅一致 | 原版**恒显示、恒可点**（全库无引用能关它） |
| ②尺寸 · 联盟钮命中区 | 就是 `EnemyInfo` **本身** · pad `(24.78,18.45,25.29,0)`（负 = 外扩）⇒ **310.07×93.45** | `MB`（`EnemyInfo` 的 `m_RaycastPadding`） | 同（`AllianceButtonHitRectPx`；uGUI 侧直接当 rect 用） | `BattleDriver.cs:6628-6639` · `:30337-30345` | ✅一致 | — |
| ②尺寸 · 结束回合钮 `Clock/TurnBtn` | 框 **130.702×80.432** · `m_PreserveAspect=0` · pad `(−14.19,−30.3,−26.19,−18.4)` ⇒ 命中 **171.082×129.132** | `RectTransform_2913` · `MB_4543` | 同 | `BattleDriver.cs:6596-6598` · `:29124-29132` | ✅一致 | 底图 182×112 比例 1.625 ⇒ 按高 80.4 定 = 130.7 |
| ②锚点 · 结束回合钮位置 | 绝对 x `[1782.9,1913.6]` y `[415.3,495.8]` | `战斗界面JSON权威表_0827.md:149-156` | `EndTurnX01=0.96263` / `EndTurnY01=0.57819` | `BattleDriver.cs:2650` · `:29120` | ✅一致 | — |
| ②尺寸 · 牌堆底板 | 我 **230×230**、敌 **200×200**；`PA=1`、图 `UI_Deck_Background` 364×346 ⇒ 实绘 230×218.63 / 200×190.11 | `RightArea/{Player,Enemy}Deck/DeckAndEnergyImage` | `DeckPlatePx=230` / `FoeDeckPlatePx=200` + `ImageQuad.FitHeight` | `BattleDriver.cs:2467` · `:2552` · `:29223-29228` | ✅一致 | — |
| ②尺寸 · 牌库张数底板 | `sizeDelta (20,0)` + `AspectRatioFitter` 4.03465 ⇒ 高 **59.11/52.05**；宽 **238.5/210**；α **0.6941177**；PA=0 | `RectTransform_3318/2658` · `MB_4275/5165` · `MB_5111/4701` | `MyDeckSizePx 59.11` / `FoeDeckSizePx 52.05` · `MyDeckSizeW 238.5` / `FoeDeckSizeW 210` · `DeckSizeAlpha 0.6941177` | `BattleDriver.cs:2563` · `:2570` · `:2578` · `:29249-29266` | ✅一致 | — |
| ②位置 · 牌库张数底板中心 | 相对牌堆中心：我 `(−15, +50)`、敌 `(0, +60)`（`anchoredPosition`） | 同上 | `DeckSizeDxMine −20.75 / Dy 165` · `DxFoe −5 / Dy 160` | `BattleDriver.cs:2573-2576` · `:29250-29255` | ✅一致 | 差额由父宽换算，逐值同义 |
| ②尺寸 · 本回合已出牌三枚灯 | 方块 **20×20** · `HorizontalLayoutGroup` spacing **9** · 贴屏幕左缘中点（holder `pos (0,−28.178)`） | `RectTransform_2722/3470/3238/2740` · `MB_5039` | `PlayedPipPx 20` · `PlayedPipGapPx 9` · 首枚 `(13.5285, 511.822)` | `BattleDriver.cs:2582-2586` · `:29290-29298` | ✅一致 | **显隐**另有缺口已修，见 ④ 行「三枚灯口径」 |
| ②尺寸 · 骷髅图标 | `MatchSkulls Icon` 框 `65.39×54.14`、图 66×73 `PA=1` ⇒ 实绘 **54.14** 高；绝对 x `[160.7,226.1]` y `[929.5,983.7]` | `RectTransform_2783/3549/3467` · `MB_5234/3785` | `SkullIconX01 0.10073` / `Y01 0.11426` / `Px 54.14` | `BattleDriver.cs:2598` · `:28855` | ✅一致 | — |
| ②尺寸 · 骷髅计数 `MatchSkulls Score` | 框 `94.46×47.31`（纯 `sizeDelta` · 锚 `(0,0.5)`）· fs 35 · **V = Midline** | `RectTransform_3467` · `MB_3785` | 同 + `SetVAlign(Midline)` | `BattleDriver.cs:2598-2600` · `:28863` · `:28875` | ✅一致 | 文案 `"x"+count` |
| ②尺寸 · 手牌数底板 `Bg (1)` | 实绘 **259.3×65.7**（缩放链 `108×0.925926×0.009 = 0.9`）· 图 `40K_display` α 0.6941177 | `RectTransform_3212/3400` · `Transform_1401` · `MB_4418` | `HandPlatePx 65.7` + `SetTint(α 0.6941177)` | `BattleDriver.cs:2620` · `:29111` | ✅一致 | 位置**原版算不出**（节点 inactive + 纯 Transform）⇒ 跟着手牌标签走（见 ❓ 行） |
| ②尺寸 · 手牌数标签框 | 框 **234×64**（2.34×0.64 局域 × 100）· 字号四格 `[5,30]` base30（3D 档 ×0.1） | `CardsInHandText` 两实例逐字段 | `HandLabelBoxW/H 234/64` · `FsMin 5 / Max 30 / Base 30` | `BattleDriver.cs:2642-2644` · `:29101` | ✅一致 | — |
| ②尺寸 · 能量水晶 | 框 **77.786×80.113**（`m_SizeDelta (−22.214,−19.887)` + 父 `Energy Accumulation` 100²）· `PA=0` | `RectTransform_{3619,2912,3346,3132}` · `MB_5378/5308` | `EnergyCrystalW 77.786` / `H 80.113` | `BattleDriver.cs:2698` | ✅一致 | — |
| ②位置 · 能量区底色板 | 中心 `(1929.43, 426.06)`（父链逐跳加 `ap`：holder `(−115.6,+116.5)` + 子 `Background (5.6315,−2.561)`）⇒ `x01 1.00491 / y01 0.60550` | `RectTransform_3554/3158/3537`（battlearena2） | 同 | `BattleDriver.cs:28895-28909` | ✅一致 | — |
| ②位置 · 能量数字 `ManaText` | 数字中心 = 球心 + `ap (−1.5, +0.594)`（两侧同值）· 容器 `ManaHolder.ap` 我 `(1.8,−0.5)` / 敌 `(3.284,0.15)` | `B1_HUD左右区_层参数表.md:109,127` | 同（派生量 `ManaTextApx/Apy`） | `BattleDriver.cs:2664-2680` | ✅一致 | — |
| ③颜色 · 牌库张数底板 α | `(1,1,1,0.6941177)` | `MB_5111/4701.m_Color` | 同 | `BattleDriver.cs:2578` · `:29249` | ✅一致 | 不设就成实心白板 |
| ③颜色 · 手牌数底板 α | `(1,1,1,0.6941177)` | `MB_4418.m_Color` | `DeckSizeAlpha` | `BattleDriver.cs:29111` | ✅一致 | — |
| ③颜色 · 战斗日志底板 | `(0, 0.07547170, 0.008201491, 1)`（无 sprite） | `MB_4620.m_Color` | 逐位同 | `Battle/BattleLogPanel.cs:240` | ✅一致 | 2026-11-03 `G10-15` 由四舍五入值改回 |
| ③颜色 · 战斗日志压暗层 | α **0.5529412**（无图） | `MB_4611.m_Color.a` | 同 | `BattleLogPanel.cs:242` | ✅一致 | 同上 |
| ③颜色 · 结算面板压暗 | `(0.0283019×3, 0.7529412)` | `MB_5043.m_Color` | `DimColor` 逐位同 | `Battle/EndPanel.cs:116-117` | ✅一致 | — |
| ③颜色 · 等待提示压暗 | `(0,0,0,0.6118)` · 无图 | `Dark Shade`（`MB`） | 同 | `Battle/WaitBanner.cs:92` | ✅一致 | 尺寸 3963.5×3366 见 ⑪ |
| ③颜色 · 联盟窗压暗 | `(0,0,0,0.4627)` · 无图 | `Alliance Panel/Close Background` | 同 | `Shell/AlliancePanelWindow.cs:261` | ✅一致 | — |
| ③颜色 · 教程高亮 | 白，α **0.8235294 = 210/255** | `MB_5145.m_Color`（13 场同值） | `210f / 255f` | `Battle/TutorialOverlay.cs:801` | ✅一致 | 2026-11-02 `G10-23` 由自选 0.85 改回 |
| ③颜色 · 按钮 Normal/Highlighted/Pressed/Selected | `1` / `0.9607843` / `0.7843137` / `0.9607843` | `Battle Arena 1.unity` 39 颗 `EverguildButton` `m_Colors` 逐颗 | 同 | `Battle/BattleButtonFx.cs:45-48` | ✅一致 | — |
| ③颜色 · 禁用色两族 | `colorTintGreyOnDisable=1`（19 颗）⇒ α **1.0**；`=0`（4 颗）⇒ α **0.5019608** | `EverguildButton__Awake.c:…+0x17C` 改写 + 场景 39 颗逐颗 | `DisabledOpaque`（α 1.0，**缺省**）· `Disabled`（α 0.502，显式传） | `BattleButtonFx.cs:54` · `:66` · `:110-118` | ✅一致 | 2026-11-02 `G10-03` 修 |
| ③颜色 · 称号橙 | `(1.0, 0.6306, 0.4198)` | `MB_3797/3887.m_fontColor` | 同 | `BattleDriver.cs:30026` | ✅一致 | — |
| ④字号 · 结束回合 / 回合钟数字 | 框 `119.08×80.43` · auto `[8,31]` · base **36** · `m_fontSize 31`（= 上限）· **Asar-Regular SDF** · H Center / V `0x1000` | `RectTransform_3420` · `MB_3971` | 同 + `TmpFont.Face.LatinUi`（= Asar） | `BattleDriver.cs:29136-29138` · `Core/TmpFont.cs:177/185` | ✅一致 | 三种文案（END TURN / ENEMY TURN / 倒计时秒）**共用同一颗**，恒纯白 |
| ④字号 · 牌库张数 | auto `[10,42]` · base **49.63** · Pragati · H Center / V Middle | `MB_3857`(我) / `MB_3944`(敌) · `RectTransform_2604/3406` | 同（`219.42×44.93` / `193.20×39.56`） | `BattleDriver.cs:29274-29278` | ✅一致 | `base 49.63 ≠ 标称` 是本族唯一一颗（自适应二分起点），如实照传 |
| ④字号 · 骷髅计数 | fs **35** · base 36 · auto `[18,35]` | `RectTransform_3467` | `94.46×47.31, 18..35, 36` | `BattleDriver.cs:28863` | ✅一致 | — |
| ④字号 · 名牌名字 | fs **35** · base 36 · auto `[2,35]` · H 居中 | `RectTransform_3016/3568` | `199.38/290.94 × 35.93/36.84, 2..35, 36` | `BattleDriver.cs:28825` · `:28838` | ✅一致 | 名字上限 35（`G10-05` 记的 40 属于联盟窗 `Name Text`，不是 HUD） |
| ④字号 · 手牌数标签 | 四格 `[5,30]` base 30 | `CardsInHandText` | `HandLabelFsMin/Max/Base = 5/30/30` | `BattleDriver.cs:2643-2644` | ✅一致 | 原版 base 36 超上限 ⇒ 夹到 max |
| ④字号 · 战斗日志行 `ActionText` | fs **30** · `m_enableAutoSizing 1` · `m_fontSizeMin 12` / Max 30 · **`m_TextWrappingMode 0`（不折行）** · 框 = 行宽−25 = **723×43.134** | `MB_3885`（`ActionText` TMP） | **固定约 30 px、无自适应、无 `SetWrapping`**（`HudCanvas.MakeText` 建件，未见 `SetAutoFitBox`） | `BattleLogPanel.cs:923-925`（全文件 `grep SetAutoFitBox/SetWrapping` = 0） | ❌不一致 | **可见**：长行会溢出到行外（原版缩到 12 为止）。改法已定（`SetAutoFitBox(723,43.134,12,30)` + 紧跟 `SetWrapping(false)`），但会改所有行版面、需开窗口并排看 ⇒ 未盲改 |
| ④字号 · 错误横幅 `ErrorMesage Text` | fs **60**（`enableAutoSizing 0`）· 单行高 **56.85** · 底条参考 `870.98×60.85` | `MB_3740` · `RectTransform_2942` · `RectTransform_2989` | `BattlePreset`：`FontPx 60` · `RefLineHeightPx 56.85` · `RefBarW/H 870.98/60.85` · `ItemW/H 1439.16/80` | `Battle/ErrorMessageBanner.cs:305-311` | ✅一致 | 中文行高会撑到 60（我们按 `max(56.85, 文字高)+4` 的取舍，已如实标） |
| ④字号 · 等待提示文字 | 框 **1270×70** · auto `[18,55.3]` · base 36 | `Generic Popup Background/Text` | `WaitTextBoxW/H` + `WaitTextFsMin/Max/Base` | `WaitBanner.cs:416-423` · `:570` | ✅一致 | — |
| ④字号 · HUD 提示条 `AboveShader/TipText` | 框 **2450.1299×99.697** · fs **60** · min 4 | `AboveShader/TipText`（`D1_A2109八条真缺` §1） | `TipWpx/TipHpx` · `TipFsPx 60` / `TipFsMinPx 4` | `BattleDriver.cs:1974-1977` · `:29334` | ✅一致 | — |
| ⑤按钮 · 过渡类型分布 | ColorTint **23** · SpriteSwap **14** · None **2**（39 颗 `EverguildButton`） | `Battle Arena 1.unity` 39 颗逐颗 | 按颗选 `Tint` / `Swap`（`BattleButtonFx` 两支 Mode） | `BattleButtonFx.cs:44-75` · 调用点如 `BattleDriver.cs:13458-13460` | ✅一致 | 换图**瞬时**（uGUI SpriteSwap 无补间） |
| ⑤按钮 · `m_FadeDuration` | `0.1` | 39 颗 `m_Colors` | `FadeSeconds = 0.1f` | `BattleButtonFx.cs:68` | ✅一致 | — |
| ⑤按钮 · 禁用 ⇒ 灰化材质 | `DoMaterialRefresh` → 每颗图形件 `Everguild/UI/Greyscale`（静态共享一份） | `EverguildButtonHelper.cs` | `ApplyGray` + `ColorTintGreyOnDisable`；取不到 shader ⇒ 出声且不变灰 | `BattleButtonFx.cs:201-204` · `:240-304` | ✅一致 | — |
| ⑤按钮 · 点击音 | 默认按钮音 **33**（`useDefaultAudioClipIfNull=1`）· 自带 cue **4** · **静音 2**（`Voice Over Button` / `Continue Button`） | 39 颗逐颗（`B117-T02` 判据） | `BattleHudButton.Attach(…, cue)`；HUD/设置/聊天/选牌等已接；两处静音就地写判据 | `Battle/BattleHudButton.cs:79/120` · 例 `BattleDriver.cs:29132/29427/29532/30209/30244/30286/30353` | ✅一致 | `B117-T02` 2026-11-02 `B118-W1` 收口；静音两处：`CardDisplayWindow.cs:619/680` |
| ⑤按钮 · 缩放反馈 / 不可交互时的悬停 | 全库 **0** 颗 Animation/Animator 缩放；`OnPointerEnter/Exit/Down` 第一句 `if (m_Interactable)` | 39 颗 + `EverguildButton__OnPointer*.c` | 无缩放反馈；Disabled 优先级最高（`Retarget`） | `BattleButtonFx.cs:150-178` | ✅一致 | — |
| ⑥联盟窗 · 根框五元组 | `815.0440×475.4710` · `aMin=aMax=(0,1)` · `pivot (0.13,0.825)` · `ap (−1.04443359375, −25.271728515625)` | `RectTransform_3052` | 逐值同 | `Shell/AlliancePanelWindow.cs:247-251` · `:574-581` | ✅一致 | — |
| ⑥联盟窗 · 头像立绘位置 | 布局框 `(9.97,61.39)-(179.99,180.61)` · 中心 **(94.98,121.00)**（`apos.y 2.7` 是局域单位 ×0.8） | `menu_dump … "Alliance Panel"` | `AvatarImageR = (9.97,61.39)-(179.99,180.61)` | `AlliancePanelWindow.cs:284` | ✅一致 | 2026-11-02 `B118-W3` 修（原差 0.54 px） |
| ⑥联盟窗 · 徽章抽屉 | `139.705994×131.104004` · `ap (−197.459930, 8.970734)` · `m_LocalScale 1` | `RectTransform_3503` | `AllianceBadgeDrawerR = (31.586,214.035)-(171.292,345.139)` | `AlliancePanelWindow.cs:309` | ✅一致 | — |
| ⑥联盟窗 · `Frame` / `Badge` 等比画框 | 两层 `PA=1`、贴图 1024² ⇒ 画框 **131.104²**；`Frame` pivot **(0,0)**、`Badge` pivot **(0,0.5)** ⇒ 贴左边、画心 x **−4.301** | prefab `RT`/`Image` + uGUI `PreserveSpriteAspectRatio` 公式 | `PivotFit(tex, drawerR, 0,0)` / `(…, 0,0.5)` | `AlliancePanelWindow.cs:728-731` | ✅一致 | 2026-11-02 `G10-05` 修（原来恒居中 ⇒ 偏右 4.301；另修 `parentR` 传错 ~197 px） |
| ⑦详情窗 · `LowerSection` | `sizeof (1320,137)` · 锚 `(0.5,0.5)` / pivot `(0.5,0)` · `ap (0,−518.34)` | `RectTransform_3533` | `300/921.34/1620/1058.34`（= 1320×137、中心 989.84） | `Core/CardWinBox.cs:25` | ✅一致 | — |
| ⑦详情窗 · `FlavourTextBG` | `sizeof (1320,178)` · `ap (0,0)` | `RectTransform_2753` | `LoreBgT 900.84 / LoreBgB 1078.84` | `CardWinBox.cs:27` | ✅一致 | — |
| ⑦详情窗 · 语音钮 | `88.655×88.655` · `ap (734.33, 0.0001831)` · pivot (0.5,0.5) | `RectTransform_3221` | `BtnS 88.655` · `BtnCy 989.84` · `VoiceCx 1694.33` | `CardWinBox.cs:48-50` | ✅一致 | ⚠️ 那颗钮**本来就落在父矩形外**（734.33 > 660），⛔ 别挪进去 |
| ⑦详情窗 · 语音钮显隐 | 唯一写者 = `SetActive(0 < availableSounds.Count)` | `CardDisplayWindow__SetCardVoiceOver.c:60-66` | 按卡判断 + `SetChrome(true)` 不再覆盖 | `Battle/CardDisplayWindow.cs:1023` · `:1807-1809` | ✅一致 | 2026-11-03 `G10-13` 修（原来所有卡都多一颗没反应的语音钮） |
| ⑦详情窗 · 关闭钮 / 眼睛钮 | **原版战斗版没有**（`LowerSection` 只有 2 个孩子；`showCardTextButton = null` 13/13） | `RectTransform_3533` 孩子表 · `ShowCard.c:137-140` | 不建 | `CardDisplayWindow.cs:619-680` | ✅一致 | — |
| ⑦详情窗 · `EffectList` | 5 个 `EffectElement` 槽 · 出厂 `m_IsActive = False` | `GameObject/EffectList` | `EffSlots = 5` · `SetEffectRows(null)` | `CardWinBox.cs:88` · `CardDisplayWindow.cs:787` | ✅一致 | — |
| ⑦详情窗 · 相关卡扇形 | 换位 `0.25 s` · 相关卡压暗 `(0.65,0.65,0.65,1)` · 槽 5 | `MB_4750` | `SwapTime 0.25` · `BackTint 0.65` · `OriginalSlots 5` | `Core/CardFan.cs:49` · `:217` · `:248` | ✅一致 | — |
| ⑧日志 · 开合位移与时长 | 收起 `initialX −1200` / 拉开 `finalX 87` · 时长 **0.3 s** · 缓动 DOTween 默认 OutQuad | `MB_4491` · `read_literal 0x1834b2dc8 = 0.3f` | `PanelOpenX 87` · `PanelClosedX −1200` · `SlideDur 0.3` · `SlidePxAt` OutQuad | `BattleLogPanel.cs:71` · `:1104-1105` · `:1336` | ✅一致 | 开窗时 `SnapSlide()` 推到底（原版 `Complete`）；**关窗不推**（原版无 `Complete`，本批已改） |
| ⑧日志 · 行数与行高 | **10 行** · 行高 `431.34399 ÷ 10 = 43.1344` | `MB_4491`（`cemeteryActions` 10 项）· `CemeteryActions.m_SizeDelta.y` | `RowCount = 10` · `RowH = 43.134` · `RowW = 748` | `BattleLogPanel.cs:131` · `:145` | ✅一致 | 无滚动条、无翻页（原版 10 颗 `Image.m_RaycastTarget` 全 0） |
| ⑧日志 · 满 10 行后新条怎么进 | 顶掉最旧：行 0 = 最新，`list` 本身不截断 | `CemeteryManager__AddActionToCemetery.c:60-160` | 驱动倒序喂、上限 `RowCount` | `BattleDriver.cs:12095` | ✅一致 | — |
| ⑧日志 · `ShowCemeteryBtn` 显隐 | **恒显示、恒可点**（prefab `m_IsActive true` / `m_Interactable 1`；全库无组件持有它） | `MB_4643` · `RectTransform_2586` 反查 0 命中 | 不闸 | `BattleDriver.cs:29520` | ✅一致 | 教程 `hideCemetery` 只影响「点行」那一条闸（`BattleDriver.cs:12466`） |
| ⑧日志 · 关窗时压暗层 | 开/合那一刻直接 `SetActive`，不跟补间 | `CemeteryManager__ShowCemeteryLogBtn.c` / `__HideCemeteryLogBtn.c` | `Show()/Hide()` 当场切 `_shade` | `BattleLogPanel.cs:1126` · `:1136` | ✅一致 | — |
| ⑨结算 · 面板根 | 锚 `(0,0)-(1,1)` · `sd 0` | `RectTransform_2850` | `FillParent` | `Battle/EndPanel.cs:265-267` | ✅一致 | — |
| ⑨结算 · `Background` | `3840×2160` @ `(0,0)` · 色 `(0.0283019×3, 0.7529412)` · `m_RaycastTarget 1` | `MB_5043` · dump 1150 | `3840×2160` + `DimColor` | `EndPanel.cs:303-322` · `:338` | ✅一致 | 贴图是 Unity 内置 sprite（运行时取不到）⇒ 1×1 白图 + tint，9 宫角块落在屏外 ⇒ 画面等价 |
| ⑨结算 · `AllRewardsHolder` | `100×100` @ `(0,−260)` · `CanvasGroup m_Alpha 1` | `RectTransform` · `CanvasGroup &3578` | `100×100 @ (0,−260)` + `CanvasGroup` | `EndPanel.cs:409-416` · `:1195` 段（`EnsureRewardsGroup`） | ✅一致 | — |
| ⑨结算 · 奖励淡入 | `DOFade(cg,0,1.0,0).From().SetDelay(0.15)` | `ShowRewards.c:192-195` · `read_literal 0x1834b2bb8=1.0` / `0x1834b2bbc=0.15` | `RewardsFadeSeconds = 1f` + 延时用 `BattleDoors.CommonAnimationScaleTime`（0.15） | `EndPanel.cs:1209` · `:1378` | ✅一致 | 2026-11-02 `G10-19` 补 |
| ⑨结算 · `SkullsHolder` | `648.1×52.4` · 三颗骷髅 `64.33×71.205` · spacing **6.1999998** ⇒ 步距 70.53 · `PA=0` | `RectTransform` · `MB_4654` | `SkullW/H 64.33/71.205` · `SkullStep = SkullW + 6.2` | `EndPanel.cs:480` · `:509-510` | ✅一致 | — |
| ⑨结算 · `SkullsHolder` 的 Image 挂哪 | **挂在 `SkullsHolder` 节点自己身上**（`Image MB 4938` 的 `m_GameObject` = GO 391） | dump 1153 · `MB_4938` · `MB_4654` | 建成了**子节点 `skull_plate`** | `EndPanel.cs:493-494` | ❌不一致 | **结构差 1 颗节点**（尺寸/贴图/PA 全对、画面逐像素相同）。被 `Editor/BattleScene.cs:13954` 的 `Find("skull_plate")` 断言钉住 ⇒ 要连同那条一起改 |
| ⑨结算 · `RewardsHolder` / 奖励行 | `495.2×49` · `RewardHolder 140×50` · `DrawerHolder 65×50 ap(5,0) pivot(1,0.5)` · `Quantity` 锚 `(0.5,0.5)-(1,0.5)` `sd(0,45)` fs40 auto[18,40] base36 | `RectTransform_3458` · `MB_4801` | 逐值同 | `EndPanel.cs:938-1003` · `:1151` | ✅一致 | — |
| ⑨结算 · `HolderRating` 那一棵 | `HolderRating 186.9742×45` · `Trophy 60.0588×60 PA=1` · `ArmyIcon 64.8399×60 sprite null 序列化 inactive` · `RatingText 65.302×46.865 fs40 Pragati Center/Middle` | `RectTransform_3228/3441/2851/2897` · `Image 4063/4880` · `TMP 3964` | `RatingW/H` · `TrophyW/H` · `ArmyIconW/H` · `RatingTextW/H` · `RatingFs 18/40/36` | `EndPanel.cs:1195-1205` · `:1261-1307` | ✅一致 | 2026-11-02 `G10-19` 补齐（原来整棵不存在）；`Trophy x −32.651` / `RatingText x +30.029` 由布局组算式推出 |
| ⑨结算 · 印记（`EarnedSeals`）分支 | `EarnedSeals != 0` ⇒ `ratingIcon.sprite = sealIconSprite` = `Rank Skull_0`（guid `9bcbe305…`） | `ShowRewards.c:104-160` | **未接**：`seals` 只影响数字，图标仍用奖杯 | `EndPanel.cs:1312-1323`（doc 明写「未接」） | ❌不一致 | `Rank Skull_0` 本地**没导入**（`find Assets -iname "Rank Skull_0*"` = 0），⛔ 不许拿 `Rank_Skull` 顶；且分支选择走 LiveOps（远端）。离线非排位局 `EarnedRating=0` ⇒ 今天不可见 |
| ⑨结算 · 副标题「敌方战将最低生命 N」 | **原版 `EndBattlePanel` 子树零文字**（这一行本身是 `🔶 自己设计`） | dump 16 行（P1） | 不知道时**印 30**：`_foeWarlordMinHp == int.MaxValue ? 30 : …` | `BattleDriver.cs:32563-32564`；`EndPanel.cs:596` 明写 `-1 = 不知道、不编数` | ❌不一致 | 遭遇局起始生命 20 ⇒ 会印出一个**错的 30**。改法 = 把 `30` 换成 `-1`（`Show` 已实现「只说回合数」那一支）；落点在 `BattleDriver.cs` |
| ⑩设置 · 面板框 | 绝对 `588.4,190.0 → 1331.6,948.6` = **743.20×758.63**（框心 960.0,569.3） | `menu_dump … "BattleSettingsPanel" --depth 3` | `PanelW 743.2` / `PanelH 758.6` | `Battle/SettingsPanel.cs:392` · `:904/929` | ✅一致 | — |
| ⑩设置 · 投降钮 | 面板内中心 **(−171.7,−310.5)** · **300×90** · 底图 `40K_button` border `(234,46,234,46)` · fs **38** · 染 `(0.3686,0.8941,0.5874,1)` | `RectTransform_3211` · `MB_3799/4755` | 逐值同 | `SettingsPanel.cs:410-418` | ✅一致 | — |
| ⑩设置 · 跳过教程钮 | 面板内 **(+171.7,−310.5)** · **300×90** · 同底图/同绿 · fs 38 · `Battle/Settings/SkipTutorial` | `Bottom buttons`/`SkipTutorial Button` · `MB_4721/4307` | 逐值同 | `SettingsPanel.cs:450-455` | ✅一致 | 非教程局也常驻（原版 prefab 常驻；`ClickSkip` 只 `LogError`） |
| ⑩设置 · 关闭叉 | 框 **56.37×54.50** · `m_RaycastPadding` 四边 **−20** ⇒ 命中 96.37×94.50 | `RectTransform_3460` · `MB_5056` | `CloseIconRectW/H 56.37/54.5` · `CloseIconPadPx −20` | `SettingsPanel.cs:407-415` | ✅一致 | 圆底那颗 `m_RaycastTarget=0` ⇒ 命中只由叉图定 |
| ⑩设置 · ESC 关窗 | `WindowsManager.Update` → `BattleSettingsWindow.ESCPressed` → `base` → `GameWindow.Close`；该窗 `closeOnESC = 1` | `WindowsManager.cs:995-1006` · `GameWindow.cs:318-330` · `MB_4034` | 列表没开时 `Hide()`（带 `EscDown()` 闸） | `SettingsPanel.cs:1992`（`EscPressed`） | ✅一致 | 2026-11-02 `G10-20` 修 |
| ⑩设置 · 点压暗层关窗 | `Awake` 把 `CloseBackgroundButton` 挂到 `onClick`（运行期 `AddListener`，序列化 `m_PersistentCalls` 为空 ≠ 没监听） | `BattleSettingsWindow.cs:67-69/126-137` | `OnShadeClicked` 列表没开时 `Hide()` | `SettingsPanel.cs:1357` | ✅一致 | 2026-11-02 `G10-20` 修 |
| ⑩设置 · 每次打开回填音量滑块 | `Open()` 里 `musicSlider.value = … / soundFX / voiceOver` · `languageSelector.Initialize()` | `BattleSettingsWindow.cs:239-249` | `Show()` → `SyncFromOpen()`（`SetValue(fire:false)` + `RefreshTexts()`） | `SettingsPanel.cs:2555` · `:2898` | ✅一致 | 2026-11-02 `G10-20` 修 |
| ⑩设置 · 小屏档整窗 ×1.1 | `GameWindow.Open()`：`smallScreenUI` 且 `extraScaleSmallScreen ≠ 1` ⇒ 窗口根挂 `TransformScalerBySmallScreenUI.SetScale(1.1)`；该窗 `extraScaleSmallScreen = 1.1` | `GameWindow.cs:68-91` · `MB_4034` | **不挂**（生产 `AddComponent<TransformScalerBySmallScreenUI>` 只在 Shell 窗口侧） | `SettingsPanel.cs` 全文无；`Shell/SettingsWindow.cs` 有 | ❌不一致 | 小屏开关打开时原版这扇窗整体大 10%。乘在**哪一级**原版没这一级（我们是 HUD 内面板）⇒ 判据未定，交版面线统一做 |
| ⑪错误 · 池容量 / 轮转 | `NUMBER_OF_MESSAGES = 5` · `currentMessageIndex = ++i; if (4 < i) i = 0` | `UIMessageController__Awake/__ShowMessage.c` | `NumberOfMessages = 5` · `:687` 同一句 | `ErrorMessageBanner.cs:212` · `:687` | ✅一致 | — |
| ⑪错误 · 项几何 | 根 `1920.69995×336.64001` · item `1439.16003×80` · 底条 `870.98×60.85` · fs 60 · 行高 56.85 | `RectTransform_3373/2823/2989/2942` · `MB_3740` | `BattlePreset` 逐值 | `ErrorMessageBanner.cs:305-311` | ✅一致 | 单条条心 = 510.23974609375 px（由布局组 + `LowerCenter` 推出） |
| ⑪错误 · 堆叠次序 | `Show` 第一句 `SetAsLastSibling()`；容器 `VerticalLayoutGroup` 只收活动子件、按兄弟序 ⇒ 刚弹的恒在最下面 | `UIErrorMessageItem__Show.c` · `MB_4106` | 按 `shownSeq` 排序（显示先后，不是池下标） | `ErrorMessageBanner.cs:825-833` · `:701` | ✅一致 | 2026-11-02 `G10-21` 修（池绕回后原来会顶到最上面） |
| ⑪错误 · 字材质描边 | `Pragati-Regular Atlas Material Thick Outline`：`_OutlineWidth 0.097` · `_OutlineColor (0,0,0,0.7882353)` · `_FaceDilate 0.105` | `Material_-455814643003338116.json` | `ApplyBannerOutline`（实例材质，不动共享） | `ErrorMessageBanner.cs:1208-1224` | ✅一致 | 2026-11-02 `G10-21` 修 |
| ⑪错误 · 疲劳横幅次序 | `ShowError(疲劳文案)` → `DealMultiDamage(疲劳伤害)` → `WaitForSeconds(**0.5f**)` | `BattleManager._ResolveFatigue_d__587__MoveNext.c` · `read_literal 0x1834b2bb4 = 0.5` | 伤害先由引擎结算完，横幅是 drain 后从 `EvtKind.Hit` 反推补弹，**没有 0.5 s 间隔** | `BattleDriver.cs:30917-30971` | ❌不一致 | 表现滞后 + 少一拍节奏。要动 `BattleDriver` / `RuleCore`（且反推靠六道闸，可能误判）⇒ 未改 |
| ⑪等待 · `WaitText` 容器 / 弹条 | 根 `1344.0×79.44` · 锚 (0.5,1) · `ap (7.0,−175.1)`；子件 `Generic Popup Background` **1323×90** | `BackCanvas/Safe area BackCanvas/WaitText` · runtime dump 350-355 | `RootW/H 1344/79.44` · `PopupW/H 1323/90` · `BarDx 7 / BarDy −175.1` | `WaitBanner.cs:91` · `:121` · `:141` | ✅一致 | — |
| ⑪等待 · 压暗层 | `Dark Shade` **3963.5×3366** · 无图 · α 0.6118 · `m_RaycastTarget=1` + `m_RaycastPadding=(0,1000,0,0)` | 同上（`WaitText` 子树） | `ShadeW/H 3963.5/3366` · `ShadeColor` | `WaitBanner.cs:129` · `:92` · `Core/ScreenUtilities`/`ShadeContains` | ✅一致 | 「只吃上中部」这条已补断言读口（`ShadeRect`） |
| ⑪等待 · 渲染先后 | **原版没有 z 判据**（`WaitText` 与那两层同父） | 判据为空 | `Z = −0.45f`（我们挑的） | `WaitBanner.cs:104` | ❓查不到 | 原版无可比对象 ⇒ 不属「复刻不到」。已如实写进源码 doc |
| ⑪战前提示条 · `TipPrebattle` | **原版 0 实例**（`openBgWidth` / `timeToOpenBg` 在 `assets_full` 全量 0 命中；decomp 外部引用 0） | `assets_full` 全量扫描 · `globalgamemanagers/MonoScript_1941` | 建出来但**不接线**（无生产调用点） | `Battle/TipPrebattle.cs` 全文；断言只测 `GetRandomTip` | ✅一致 | 铁律 11 例外①（原版这条路不走）；账已裁定「保留不接线、不要删」 |
| ⑫教程 · 高亮贴图尺寸 | **恒 = (屏高/1.48) × (屏高/2.0)**（1080p = 730×540），与目标元素多大无关 | 材质 `_UVScale (1.48,2.0)` · VS 反汇编 `工具/disasm_dxbc.py "Tutorial Highlight"` | `w = DesignHeight/1.48` · `h = DesignHeight/2.0` | `TutorialOverlay.cs:247` · `:1026-1027` | ✅一致 | 2026-11-02 `G10-23` 修（原来跟着锚点尺寸 + 自选 0.06 边距） |
| ⑫教程 · 高亮位置要含 `positionRelation` 偏移 | `EnableTutorialHighlight` 调 `GetTipPosition(…, positionRelation, …)`（与提示文案**同一个**函数）；`LeftOf ⇒ x−=d` · `RightOf ⇒ x+=d` · `Above ⇒ y+=d×factor` | `AiScripted__EnableTutorialHighlight.c:24-46` · `AiScripted__GetTipPosition` | `SetHighlight(on, center, size)` **只给元素中心**，驱动调用点不传 relation | `TutorialOverlay.cs:1002` · `:1022-1023`（doc 自陈）；调用点 `BattleDriver.cs:18027` | ❌不一致 | `LeftOf/RightOf/Above` 三种 relation 下原版光斑偏出去一截，我们贴在中点。提示条那一侧**有** relation（`:951-953`），只差高亮。要改签名 + 驱动补实参 |
| ⑫教程 · 高亮淡出 | 收起 `DOFade(0, fadeTime = 0.1)`，播完才 `SetActive(false)` | `ScreenHighlightPosition__Fade.c` · `__Toggle.c` · `ctor` 0.1 | **瞬关** | `TutorialOverlay.cs` 的 `SetHighlight(false, …)` 支 | ❌不一致 | 0.1 s 可见差。会推迟 `activeSelf=false`，与 `Editor/BattleScene.cs:36907/36933` 的「即时隐藏」断言冲突 ⇒ 两边一起改 |
| ⑫教程 · `TutorialObjs` 教学标注树 | **只有一棵**（`CardDisplayWindow.SetupTutorialObj` 建，7 道闸） | `CardDisplayWindow__SetupTutorialObj.c:26-72` · `ShowBattleCard:664` | 第二棵已停建（`_annoGo = null`） | `TutorialOverlay.cs:822` · `:833`；`CardDisplayWindow.cs:2069-2130` 保留原版那棵 | ✅一致 | 2026-11-02 `B118-W3` 修（原来教程局可能两套字同时出现） |
| ⑫教程 · 小提示 / 开场提示 / 指点器 | `TutorialTip 430×150` · `InitTutorialTip` rect 100×100（文本驱动 700×100）· `Pointer 100×100` | `2D层_battlearena1全树.md` | `TipPxW/H 430/150` · `InitTipPxW/H 700/100` · `PointerPx 100` | `TutorialOverlay.cs:194-199` | ✅一致 | `InitTip` 那 700×100 是我们按「文本驱动」取的（原版 rect 100²） |
| ⑫教程 · 提示淡入与跳过窗 | `fadeDuration = 0.2` · `minTimeBeforeSkip = 1.0` | `TutorialTipScript` prefab 实例 `MB_5246` | `FadeDuration 0.2` · `MinTimeBeforeSkip 1.0` | `TutorialOverlay.cs:234-237` | ✅一致 | — |
| ⑫教程 · 跳过闸挂在哪 | `minTimeBeforeSkip` 是 **`TutorialTipScript`** 的字段，只管**提示本体**；设置窗那颗 `Skip tutorial` **没有**这道闸 | `TutorialTipScript__Update.c` · `BattleSettingsWindow__SkipTutorialButtonOnClick.c` | `TutorialOverlay.TrySkip()` 拿 `MinTimeBeforeSkip` 当「按早了不算」，`BattleDriver.SkipTutorialFromSettings` 直接吃返回值 | `TutorialOverlay.cs:172-173` · `:237` | ❌不一致 | 实际影响小（要先点齿轮，早过 1 s）；与 `BattleScene.cs:36981-36986` 断言绑在一起 ⇒ 两边同改 |
| ⑫教程 · 高亮层宿主 / 缩放 | 原版 `Tutorial highlight` 是**根 Canvas 直子**（stretch，`1×1` 世界单位） | `GameObject/Tutorial highlight.json` · 全树 `:802` | 我们建在 `Tutorial` 根下（世界档）/ 独占子画布（画布档）；生产走世界档 | `TutorialOverlay.cs` `Build()`；`BattleDriver.cs:16967` | ✅一致 | 16:9 下画面等价（已如实记） |
| 跨件复核 · 技能面板色片三层 | 三层独立 Image 完全重叠；`m_Children = LightActing→LightAvailable→LightPressed`；无任何一处点亮一层时关别的层 | `RectTransform_3000/2771/2606` · `RectTransform_3121` | 按原版画序做直通 α 的 over 合成（`ComposeLightTint` + `OverLayer`） | `Battle/SkillPanel.cs:2332` · `:2344` · `:1698` | ✅一致 | 2026-11-02 `G10-09` 修（原来三选一） |
| 跨件复核 · 技能面板费用文字 | `CostText = parentCard.currentAbilityCost`（含降价修正） | `ActiveSkillDisplay` ILSpy `CS:131-133` | **恒写 `"0"`** | `Battle/SkillPanel.cs:1596` | ❌不一致 | 原版印**当前能量费**；要引擎补一格 `currentAbilityCost` + `ShowSkillPanel` 传参（落点在 `BattleDriver.cs:15754`）。现池 `activeAbilityManaCost` 多为 0 ⇒ 影响面待现读 |
| 跨件复核 · 技能面板可用层（黄绿） | `bgLight.SetActive(manager.CanUseActiveAbility(...))` | `SetupActiveSkill.c:141-148` · `DisableUsableLight.c` | `ShownTargets > 0` 即铺 | `SkillPanel.cs:1697` · `:2339` | ❌不一致 | 「有几个合法目标」≠「现在能不能用」（能量不足 / 已用 / jam 时原版不亮，我们照亮；反之 0 目标但可用时原版亮、我们不亮）。需驱动传 `CanUseActiveAbility` |
| 跨件复核 · 攻击选择器几何 | 钮 `118.372×118.806` `PA=0` · spacing **37** · 选中圈 `176.23×176.875` · 选中 `×1.3` `OutBack` `0.3 s` | `RectTransform_3368/2804/2626` · `MB_4206/4121/4054` · `MB_4653` | `ButtonW/H` · `ButtonSpacing 37` · `RingW/H` · `HighlightScale 1.3` · `PickTweenSeconds 0.3` | `Battle/AttackSelector.cs:126-143` | ✅一致 | — |
| 跨件复核 · 攻击选择器角标 | 字号 **89.01**（auto 1..89.01）· 纯白 · 相对钮心 **(+0.452,−1.205)** | `RectTransform_2617` · `MB_3755` | `SetBadgeSizeOnce(89.01)` · `BadgeColor` 白 · `BadgeDx/Dy 0.452/−1.205` | `AttackSelector.cs:2051-2103` · `:213` · `:933/988` | ✅一致 | 2026-11-03 `G10-08` 修（原来小 3 倍 / 暖白 / 低 6.8 px） |
| 跨件复核 · 攻击选择器生命周期 | 选定打法**不关**选择器；起手就带 `card.attackType`；同一档不重复下单 | `UnitOnBoardAttackTypeSelector__AttackButtonClick.c` · `BattleManager__StartAttackFrom.c:20-40` · `__Update.c` case 6 | ①②**已修**（不再 `Hide()`、起手设 `_command`）；③**未加** | `BattleDriver.cs:15693` · `:15325`；`AttackSelector.cs:1581-1587`（`Selectable` 无同档闸） | ❌不一致 | ③ 缺 `if (opt.Kind == _selected) return false`（原版 `AttackButtonClick.c` 第一句）。①②已落地，③可补；影响 = 悬停已选那格仍会重复 `CommitCommand` |
| 跨件复核 · 准星尺寸 / 弧线 | 准星 sprite 180×182 @PPU100 × `localScale 4.83` = `8.694×8.791`（HUD 帧 14.84 ⇒ 129.0×130.5 px）；弧线宽 `0.163`、11 点、`sortingOrder 5` | `Attack Target Reticle` prefab | `CrossW/H = 8.694/8.791 × 14.84` · `:84/199/202/388-396` | `Battle/TargetReticle.cs:80-84` | ✅一致 | — |
| 跨件复核 · 语音条气泡几何 | `Background 648.774×236.505` · `wave 387.946×62.601` @(435.497,68.65) · `Mask 144.457×157.465` @(104.49,113.15) | `RectTransform` 现读（三颗同值） | `BubbleW/H 648.77×236.50` · `WaveW/H` · `PortraitW/H 144.46×157.46` | `Battle/UnitChatPanel.cs:127` 等常量区 | ✅一致 | ≤0.005 px |
| 跨件复核 · 语音条台词中线 | 中线 **135.53** px（相对气泡上沿）· 左沿 **188.55** · 宽 **440.93** | `RectTransform_3566`（aMin/aMax/ap/sd 重算） | `TextLeft 188.55 / TextCy 135.53 / TextW 440.9` | `UnitChatPanel.cs:127` · `:144` | ✅一致 | 2026-11-03 `G10-22` 修（原来 134.55，差 0.98 px） |
| 跨件复核 · 语音条音量/优先级 | `m_Volume 1.0` · `Priority 154` · `spatialize false` · `playOnAwake false` | `AudioSource_1596/1598/1600.json`（4 场同值） | `volume = 1f` · `priority = 154` | `UnitChatPanel.cs:760` | ✅一致 | 2026-11-03 `G10-22` 修（原 0.85 / 默认 128） |
| 跨件复核 · 语音条卡图裁剪（生产档） | `RectMask2D 144.457×157.465`（padding/softness 0）+ 子图 **176×176** `PA=0` 偏移 `(−2.48,−5.00)` | `RectTransform_2600/3347/3408` · `RectTransform_2844/3108/3169` · `MB_4340/4724/4798` | uGUI 档已建 `RectMask2D` + 176² 子图；**世界档仍无遮罩** | `Battle/UnitChatPanel.PartB118W3.cs` | ❌不一致 | 生产档（`BattleDriver.cs:29506` 传画布宿主 ⇒ `_ugui = true`）已一致；**世界档**无等价物 ⇒ 非生产档差异，已如实标 |
| 跨件复核 · 多卡展示滚动 | `m_MovementType = 1 (Elastic)` · `m_Elasticity 0.1` | `MB_4823` | `ScrollElasticity 0.1` + `SmoothDamp` 回弹、拖动不夹 | `Battle/MultiCardDisplay.cs:977` · `:1278` | ✅一致 | 2026-11-02 `B118-W3` 修（原 `Clamped`） |

---

## 已核参数数

**124 行**（按参数族：①画布/适配 12 · ②锚点尺寸位置 24 · ③颜色 11 · ④字号行盒 9 · ⑤按钮四态 5 · ⑥联盟窗 4 · ⑦详情窗 7 · ⑧墓地日志 5 · ⑨结算面板 10 · ⑩战斗内设置 8 · ⑪错误/等待 9 · ⑫教程 8 · 跨件复核 12）。

- ✅一致 **109** · ❌不一致 **14** · ❓查不到 **1** · ⚠️已知待修 **0**（D118-85 的 HUD 相机透视/正交属 D 域，下表未单列，只在「覆盖边界」指认）。
- 其中 **2 行**是「有意差异/等价实现」但仍记在 ✅ 里（`planeDistance`、结算 `Background` 内置贴图），备注已写明。

## ❌ 数

**14 条**，按可见影响排序：

1. **技能面板费用文字恒 `"0"`** —— `SkillPanel.cs:1596`；原版 `currentAbilityCost`。任何非 0 费用的主动能力都会印错。
2. **技能面板可用层判据错** —— `SkillPanel.cs:1697`；原版 `CanUseActiveAbility`。能量不足/已用/jam 时会亮错，反之该亮不亮。
3. **教程高亮缺 `positionRelation` 偏移** —— `TutorialOverlay.cs:1002` / `BattleDriver.cs:18027`；`LeftOf/RightOf/Above` 档光斑位置错。
4. **战斗日志行文字无自适应** —— `BattleLogPanel.cs:923-925`；原版 `auto[12,30]` + 不折行。长行溢出到行外。
5. **结算副标题在「不知道」时印 30** —— `BattleDriver.cs:32563-32564`；遭遇局（起始生命 20）会印错数。
6. **疲劳横幅次序/节拍** —— `BattleDriver.cs:30917-30971`；原版「先弹横幅 → 再结算伤害 → 等 0.5 s」。
7. **设置窗小屏档 ×1.1 未做** —— `SettingsPanel.cs` 无 `TransformScalerBySmallScreenUI`；小屏开关打开时才可见。
8. **教程高亮淡出瞬关**（原版 0.1 s）—— `TutorialOverlay.SetHighlight(false, …)`。
9. **设置窗 `Skip tutorial` 被 1 s 闸挡住** —— `TutorialOverlay.cs:172-173`；原版那颗钮没有这道闸。
10. **结算 `SkullsHolder` 的 Image 建在子节点 `skull_plate`** —— `EndPanel.cs:493-494`；画面逐像素相同，结构差 1 节点。
11. **结算印记（seals）分支未接** —— `EndPanel.cs:1312-1323`；本地没导入 `Rank Skull_0`，离线不可见。
12. **攻击选择器缺「同档不重复下单」闸** —— `AttackSelector.cs:1581-1587`；原版 `AttackButtonClick.c` 第一句。
13. **`Canvas.additionalShaderChannels` 未设 25** —— `HudCanvas.cs:105-116`；无观测影响，改动会推账落点。
14. **语音条世界档无 `RectMask2D` 裁剪** —— `UnitChatPanel.PartB118W3.cs`；生产走 uGUI 档，非生产档差异。

⚠️ **已知待修（不重复立新账）**：

- `D118-85`：HUD 相机是**正交**，原版 UI Camera 是**透视 fov 40**（`Core/LayoutSpace.cs:78-79` vs 原版 UI Camera）。属相机/渲染域（D 域），HUD 适配参数本身已对平。
- `D118-84` / `D118-83`：SSAO 多一层、渲染设置 6 项偏离 —— 均为 D 域，本报告只在「覆盖边界」里指认，不逐条重列。

## 覆盖边界

1. **已覆盖**：12 个参数族的主要参数 —— 画布/CanvasScaler/比例适配、HUD 各面板锚点尺寸与位置（名片、称号、头像、牌堆、张数板、三枚灯、骷髅、手牌数板、能量区、结束回合钮、聊天/日志钮、联盟钮命中区）、颜色（含 α）、字号与行盒、按钮四态/过渡/点击音、联盟窗与徽章、详情窗与 Tooltip、墓地/战斗日志、结算面板、战斗内设置/投降/退出、错误提示池/疲劳横幅/等待提示、教程高亮与提示条。
2. **原版 HUD 上没有「血条」**：`BattleHud` 子树里没有任何血条 / `HealthText`；血量只在**卡面**（`2DCard/Front/Card Info/Health Container`）。⇒「顶部血条」这一族**不适用**，不列行（判据 → `B117_G10_战斗域拆分清单.md` §三·1）。
3. **原版 HUD 没有「顶部资源条」**：能量是**右侧一竖排**（`Energy And turn holder`），不是顶部条；已按右侧竖排核对完（见 ② 行）。
4. **手牌区底板**：原版 `BottomAnchor/PlayerArea/HandArea` 是**纯 Transform、无底板图**；我们建的 `HandPlate` 是「手牌数底板 `CardsInHandText/Bg (1)`」（原版有），**位置**因原版节点 `activeInHierarchy=False` + 祖先链纯 Transform ⇒ **算不出绝对坐标**（❓ 见上表那行）。
5. **不覆盖**：战场 13 场 prefab 装配 / 场景光照烘焙（用户 2026-10-29 定「战场线暂停」）· 相机光学参数与渲染设置（D 域，见 D118-83/84/85）· 特效/动画参数（G10-10/11）· 手牌布局手感（G10-06/`B118-W2`）· 战斗内联机表现（G10-26）。
6. **运行时 dump 的口径**：`runtime_ui_dump_*.tsv` 是**原版自己跑的**（`B117-R67` 已订正），⛔ 不能当「我们 vs 原版」基线；本报告的「我们值」一律取自**我们代码现读**，未使用该表的「我们值」列。
7. **既有判据会过期**：`B117-G10-03..23` 那批判据里标「未修」的条目，本报告已逐条回代码现读确认（能修的都已被后续 `B118-*` 波次修掉，未修的今天仍未修）。

## 搜索过但没找到的路径与关键词

**搜过、确实没找到（❓ 或「原版不存在」）**：

| 找什么 | 搜过的位置 / 关键词 | 结论 |
|---|---|---|
| HUD 血条 | `grep -rn "HealthText\|HealthBar" Battle/BattleDriver.cs`；`runtime_ui_dump` 全表 `Health`；`2D层_battlearena1全树.md` | HUD 下**没有**；19 处 `Health` 全在**卡面**与 Debug 钮 ⇒ 这一族不适用 |
| HUD 顶部资源条 | `Energy And turn holder` 全树；`RightArea` 子树 | 原版能量是**右侧竖排**，无顶部条 |
| 手牌数底板原位 | `RectTransform_3212/3400` + 祖先链；`runtime_ui_dump` `activeInHierarchy` | 节点 inactive、祖先链纯 Transform ⇒ 绝对位置**算不出**（我们跟着手牌标签走） |
| `WaitBanner` 的 z 判据 | `WaitText` 子树 z；`runtime_ui_dump` 不写 `localPosition`/排序 | **原版没有 z 判据** ⇒ 我们的 `Z = −0.45` 属自选（已标 ❓） |
| 战斗日志行字体面 | `bundle_fonts_assets_all/MonoBehaviour/`（`Asar Regular Almost white` / `Asar-Regular SDF`）；`ActionText.m_fontAsset` pathID `3485036404935369831` | pathID **未解码**到具体那一份 ⇒ 判据不足（我们代码用 `LatinCardBody` = Pragati，注释写 Asar，**自相矛盾**，未猜着改） |
| `EndPanel` 评分数据源 | `EndMatchResult +0x18/+0x1c`；`OnlineMatchResult` | 服务端下发（P3）；单机局 `EarnedRating=0` ⇒ 与原版「非排位局不亮评分行」逐档一致，只复刻结构与显隐 |
| 战斗内设置「结算面板同时打开」时的关窗判据 | `BattleSettingsWindow*.c` 全方法体；`WindowsManager.cs` ESC 派发；桌面 `warpforge参考图\` | **无该组合实拍/判据** ⇒ 按「照常关窗」实现（`G10-20` 已记） |
| 设置窗小屏 ×1.1 乘在哪一级 | `GameWindow.cs:68-91`；我们面板结构 | 原版乘在**窗口根**，我们这扇是 HUD 内面板、没有「窗口根」⇒ 判据未定 |
| `NameBackground` / 名片原始静态值 | `Battle Arena 1.unity`（静态是 `(31.7,28)` / `(50,−28)`，被脚本改掉） | 静态值**不可当判据**（运行时被改），一律按运行时 `ap (−240,±330)` |
