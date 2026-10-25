# AUD3 · 界面线 / 卡牌线 §一 逐条现核（2026-10-25 · 只读核查代理）

> 口径：**代码里有实现 = 做了**；台账自陈不算。判「已做完(该核销)」= 实现已落 **且** 它声称「只剩跑某宿主」的那个宿主**今天实跑绿**。
> 宿主读数取今日最新批 `D:/4/_tmp_view/20261025/b_*.log`（09:05–09:12）：`BattleScene` **5345/0** · `MainMenuScene` **4969/0** · `CardBaseDemo` **433/0** · `CardFaceProbe` **207/0** · `CollectionScene` **2355/2358**（3 红见「不能判的」）· `ShellScene` 1920/0。
> ⚠️ 日志无构建指纹，不能 100% 绑定 HEAD（沿用 AUD2 口径）；本机有批处理在跑，本代理未跑 Unity/typecheck。

## 界面线

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据 | 建议处置 |
|---|---|---|---|---|
| 1 | `:228` W62·真机（界面侧缺陷） | **未完成·仅欠真机**；但行内「待核：头像位➖ / 模式卡无倒计时」两条**已实现** | `Shell/TopBar.cs:109/152`（`QBarLevel` 等级徽章）· `Shell/MainMenuRuntime.cs:2907+`（`TimerDisplay`，本地无活动数据 ⇒ 印 `--` 并出声，原版只有排位卡有这一行） | 进真机批；两条「待核」就地核销 |
| 2 | `:229` T01 卡组编辑窗 | **未完成·仅欠真机** | 静态全闭（本行自陈 `A1701` 探针已重插）；无代码缺口 | 进真机批 |
| 3 | `:230` T02 主菜单与外壳（= `A1520`） | **未完成·仅欠真机**（「壳卡死」仍无判据/无复现）；其中**六条状态不变量那半该核销** | `Editor/ShellScene.cs` I1–I6（`普查产出_1025/SH_A1520状态不变量.md`）· ShellScene 1920/0 | 真机批；I1–I6 核销 |
| 4 | `:231` T03 收藏窗 | **未完成·仅欠真机** | `N52` 断言已落（本行 + 索引报告） | 进真机批 |
| 5 | `:232` T07 字体与文本口径 | **已做完(该核销)**（仅欠并排） | 源 TTF `Assets/…/Fonts/NotoSerifCJK-Regular.ttf` **md5 `395466fcc2c3e40a…`** = `素材/Fonts/…-JP.ttf` 逐字节（= 原版 TTC face 0）· SDF `m_FaceIndex 0` / `m_Scale 0.9` · 资产 10-25 09:15 重烘 · `SettingsScene` 1553/0 | 核销；并排归视觉批 |
| 6 | `:233` T08 本地化与文案 | **未完成·仅欠真机** | 官方表已并入（`官方本地化_en+zh-CN.json`） | 进真机批 |
| 7 | `:234` T12 左栏导航运行期动态 | **未完成·仅欠真机** | `MainMenuRuntime.BuildNavigationPanel` / `TickNavPanelTween` | 进真机批 |
| 8 | `:235` T16 外壳运行层 | **未完成**（`D10` Toast 根高 / `D4` 暗底姿态要原版 player 探针） | 静态项全闭（本行自陈）；`UIBakeScreen.cs:74` 等已落 | 保留；探针归真机批 |
| 9 | `:236` T18 奖励/弹窗族 | **用户拍板**（奖励页 2026-10-22 挂起、不进真机）＋ 8/8 已建 ⇒ **该核销** | `:184-188` 挂起口径；本行「8/8 已建」 | 核销 |
| 10 | `:237` T19 左栏内容页 | **未完成·仅欠真机**（社交/奖励页部分用户挂起，不计验收） | 索引 `U2_左栏六个内容页.md` | 进真机批 |
| 11 | `:238` T23 卡组编辑窗残余 | **未完成·仅欠真机** | TopBar 早退已落（本行自陈） | 进真机批 |
| 12 | `:239` T25 收藏卡背页 | **未完成·仅欠真机** | `CollectionWindow.cs:54/62/79-82`（`Cardback Shadow SDF` 建完、233 张卡背入库） | 进真机批 |
| 13 | `:240` T26 FTUE 选卡组窗 | **用户拍板**——用户 2026-10-24：「选择初始战将及卡组」界面**不需要**（现在全卡）⇒ 该核销 | `:77`（只保留 `-wfftue` 调试口） | 移出 §一 |
| 14 | `:241` T28 Parent Event Window | **未完成·仅欠真机** | ESC 窗参已按基座改（见 #31） | 进真机批 |
| 15 | `:242` T29 全界面底纹 | **未完成·仅欠真机/并排** | `Shell/MainMenuSubmenuWindow.cs:195` 数字三元组已对 | 进真机/视觉批 |
| 16 | `:243` T30 战斗入口·练习窗 | **未完成·仅欠真机**（缺陷已修） | `Shell/PracticeModePopup.cs:735/770/878/930`（判源收成 `CurrentDeck` + `RefreshCostRows`） | 进真机批 |
| 17 | `:244` N117 弹窗压暗层与左栏 | **未完成·仅欠真机** | 方案 C 已落（本行自陈） | 进真机批 |
| 18 | `:245` W63 卡组双模式 | **未完成·仅欠真机** | ①–⑥ 全落（本行自陈）+ `W64_卡组双模式_原版规格.md` | 进真机批 |
| 19 | `:246` 收藏卡池入场动画 | **未完成·仅欠真机** | `Shell/CollectionWindow.cs:1331/1377/1575`（`BuildCardAnimateInTween` / `PlayCardAnimateIn`） | 进真机批 |
| 20 | `:247` `Hand/**` 两处 Sequence+OutQuad | **做了一半** —— ④「拿起手牌」断言**确实没补** | `Editor/HandAssertE8.cs` 无 `PickUpDuration`/`dragScale` 断言；`Hand/CardInteraction.cs:601+` 实现已换 `TweenPose(OutQuad)` | 补 1 条断言 + 真机摸手感 |
| 21 | `:248` `HandLayout.Place` → `TweenPose` | **已做完(该核销)**（仅欠真机手感） | `Hand/HandLayout.cs:882` `c.TweenPose(...,RelayoutDuration,OutQuad)`；E8 `A_LocalRotationAndScaleOwnership` 半程 `BaseScale` 断言实跑 ✓ | 核销 |
| 22 | `:249` Hand 三组断言未补 | **已做完(该核销)** | `Editor/HandAssertE8.cs` 34 条 + `Editor/BattleScene.cs:13546` 挂载；`b_battlescene9.log` 里 `E8 ✓` **34 条**、合计 5345/0 | 核销 |
| 23 | `:252` 收藏窗全控件核查 | **未完成·仅欠视觉/并排**（代码全落） | `Core/CardView.cs:7536-7538` `MenuHoverColor=#0080FF` · 5 处 `Elastic` · 全文件**无活的** `NotifyNotBuilt`（只剩 `:3504` 定义 + 注释）· `OpenCardDetail :5106` | 视觉批；不再派代码 |
| 24 | `:261` S0 轮抽选将页点不到 | **未完成·仅欠真机**（修法已落） | `Shell/DraftModeWindow.cs:1401` 卡体改 `MenuDraw.Absorb`；`:1188` 吸收层；`:251` `WarlordsPerDraft=3` | 进真机批（D58） |
| 25 | `:262` S1 结算屏点击穿透 | **未完成**（真机**未复现**，不能关账） | 需带录屏复现；`End.ExitReady` 闸已加 | 保留，等复现 |
| 26 | `:263` U2·V1 头衔空态 | **做了一半**（已知残留：没开过档案窗直接进战斗 ⇒ HUD 称号仍空） | `Shell/ProfileTab.cs:508/538` `EnsureLocalDefaultTitle`；种子落 `ProfileTab` 而非 `ProfileData` | 保留（用户已知并接受）；如要完全等价需搬种子 |
| 27 | `:264` U2·V2 Top4 空占位 | **已做完(该核销)** | `Shell/RankedTab.cs:106/403`（无数据整格 `SetActive(false)`）· `ProfileAssertU2b` 随 `MainMenuScene` **4969/0** 实跑 | 核销 |
| 28 | `:265` U2·V3 战斗记录钮压边 | **已做完(该核销)** | `Shell/BattleLogTab.cs:167` `PanelVisibleTop = 138f` | 核销 |
| 29 | `:266` U2·V5 头像格多印名字 | **已做完(该核销)** | `Shell/AvatarTab.cs:468` `SetActive(false)`；D7 已删 `:397` 那圈「视口外不建」（`Intersects` 只剩注释）· 日志 `cells==15` 段 ✓ | 核销 |
| 30 | `:267` U2·V6 传奇支多 `Body` | **已做完(该核销)** | `Shell/ProfileTab.cs:1118`（512² 内接 **330.96²**，`preserveAspect`） | 核销 |
| 31 | `:268` 新账-103 `ParamsCloseOnEsc` | **已做完(该核销)** | `Shell/ParentEventWindow.cs:195 = true` / `:552` 写入；`b_mainmenu9.log:205434` 前提断言 ✓、`:205463`「ESC ⇒ 关掉 Parent Event Window」✓ | 核销；真人键盘归真机批 |
| 32 | `:269` A_new 卡组详情窗 ESC | **未完成·仅欠真人键盘**（代码本来就对） | `Shell/CardDetailPopup.cs:623` `win.closeOnEsc = true` | 归真机批（真人键盘） |

## 卡牌线

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据 | 建议处置 |
|---|---|---|---|---|
| 1 | `:85` W62·卡面（tier4 3 红 / 拉丁字号 / 墨心） | **已做完(该核销)**（仅欠并排 PnP） | ①`Editor/CardFaceProbe.cs:628-631` 基线改 `hole ∪ ring`、`rectMiss` 阈值 → **0.5f** · ②`Core/CardView.cs:1467/4655/4987/5005` `FontSizeForEm` · ③`Editor/CardBaseDemo.cs:2824-2847` 改量**墨心** · `CreatedByEmOfCard = 0.245f`（`CardView.cs:4557`）· `TmpSetup.cs:263-275` ⑧ 口径 10-22 已订正 · `IconSetup.cs:64-75` 注释已换 | 核销；字体可见变化归并排批 |
| 2 | `:86` T01 卡 id 真值链 | **未完成·仅欠真机/运行期** | 静态剩余 0（本行自陈，`W58_卡牌线_剩余收口.md`） | 进真机批 |
| 3 | `:87` T03 中文与官方表 | **未完成·仅欠并排/真机** | `A2047/A2048/A1956` 已闭（本行自陈） | 进真机/视觉批 |
| 4 | `:88` T04 卡面几何与图标 | **未完成·仅欠真机**；12 阵营并排**素材已出** | `cardfaction` 今日 rc=0「13 阵营全部渲出」（`b_faction9.log`） | 真机批 + 并排比 |
| 5 | `:89` T05 卡面层级与场上数值 | **未完成·仅欠真机/并排** | `A1678/A1658/A1661/A1683/A1672` 已落 | 进真机批 |
| 6 | `:90` T06 预组卡组数据 | **已做完(该核销)**（编辑器侧已绿；真机若仍 87/70 = 旧生成物） | `RuleEngine/Resources/prebuilt_decks.json` 实读 `decks` **103** 条；`MainMenuScene` **4969/0** | 核销 |
| 7 | `:92` 新账-66 战术/防御插图与外发光 | **未完成·仅欠 tier3/4 实拍 + 真机并排** | 条带 `FrameRowsOf`/`ArtWindowMesh` + `outlineGlow` 层已落（本行） | 实拍清单 |
| 8 | `:94` 新账-87 阵营/兵种中文 | **做了一半** —— 主体已收口，仍开 **④ 7 个「近邻键」待裁** + `Loc.cs` 文件头复算数过期 | 本行自陈（改键名须先核原版代码字面量） | 保留；判据已写在行内 |
| 9 | `:96` `AlphaMask=null` SoftMask 口径 | **不是待办**（`📌 常驻口径`） | —— | 不动 |
| 10 | `:101` 新账-100 `<sprite>` 面继承 | **未完成·仅欠真机并排** | `CardIcons.PinSpriteFace` + `IconSetup` ⑬ 已绿（本行） | 进真机批 |
| 11 | `:102` trait Tooltip 75×75 两栏 | **未完成·仅欠真机** | 原版两栏已建（`IconSetup` ⑭；`BattleScene` 已绿） | 进真机批 |
| 12 | `:111` M2 整卡面偏低 0.08 | **已做完(该核销)**（仅欠并排 PnP） | `Core/CardView.cs:1970 FrontAposY=0.08f` + `FaceContentRoot`、`:1328/1340` 反向抵消；`CardBaseDemo` **433/0** · `CardFaceProbe` **207/0** | 核销 |
| 13 | `:112` C6·Q7 `_revealCard` 用错族 | **已做完(该核销)** | `b_battlescene9.log:116502`「揭示卡走战斗卡族 1.35 / 参照 1.20」✓；合计 5345/0 | 核销 |
| 14 | `:113` 插图四边近白残余 | **已做完(该核销)**（仅欠渲图肉眼看） | `Core/CardArt.cs:487-503` `ArtUvRect*`（裁块/整页两支）+ `AssertBoardArtNoWhiteEdge` 6 条随 `CardBaseDemo` 433/0；`BattleScene` 5345/0 | 核销；渲图归视觉批 |

## 应核销清单

**整条核销（实现已落 + 宿主已绿 / 用户拍板）**：界面线 **T07 · T18 · T26 · Hand 三组断言(`:249`) · `HandLayout.Place`(`:248`) · U2·V2 · U2·V3 · U2·V5 · U2·V6 · 新账-103**；卡牌线 **W62·卡面(`:85`) · T06 · M2 · C6·Q7 · 插图四边(`:113`)**。
**半条核销（该行保留，但其中一格已闭）**：`T02` 的 `A1520` 六条状态不变量 · `W62·真机` 的「头像位➖ / 模式卡倒计时」两个待核 · `T07` 的 `N44③`（SettingsScene 1553/0）。
**明确不是待办**：卡牌线 `:96` `📌 常驻口径`；界面线 D5 两半（`Row Bg` 层 + `MenuDeck/Error/NoWarlord` 自造键，用户 2026-10-25「暂时不动」）；97 条成就计数器（已另派判据代理 → `资料/待办判据_成就计数器_1025b.md`）。

## 不能判的

1. **一切「仅欠真机」的行**（界面线 1–4、6、7、10–12、14–19、24、31–32；卡牌线 2–5、7、10–11）：本机 **Unity 批处理在跑**，本代理**不跑 Unity、不点 player**，只能判「代码在不在」。真机实测归 `资料/真Play待验清单.md` 的批量验收。
2. **`S1` 结算穿透**：真机未复现、也无录屏 ⇒ 既不能判缺陷也不能判已修（台账自己写了「别据未复现就关账」，同意）。
3. **`T16 D10/D4`** 要原版 player 探针（`rect.height` / 暗底姿态），静态判不了。
4. **`CollectionScene` 那 3 红**（`b_collection9.log:161579/161594/161609`，第 4 行文字网格 0 顶点 / 渐隐带 alpha 未削）= 视口裁切软边族，**不属这两条线 §一 的任何一行**，本报告不裁。
5. **「像不像」类**（T29 底纹 / 收藏窗悬停光带 sRGB / W62 字体小 14.7% / 插图白边渲图 / M2 并排 PnP / 卡片详情 0.1px 偏差）：只能并排比，不能靠断言。
6. **`A_new` ESC 与 `新账-103` 的键盘那一半**：本环境键盘通道可疑（战斗线同日记录），须真人键盘。
7. **日志与 HEAD 的绑定**：`b_*.log` 无构建指纹（沿用 AUD2 §不能判的 4）。
