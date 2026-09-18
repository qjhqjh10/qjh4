# 战斗 UI 原版对账表（2026-09-12 建立）

> **用途**：战斗界面里每一个元素的「原版是多少 / 我们是多少 / 补上没有再」，一条一行。
> 改战斗 UI 之前先查这张表，**不要重新去 dump 里挖**。
> ⚠️ **这套不是交接文档**：**接着做什么只看 `项目任务.md` 顶部那张「⏭ 下次接着做」表**；
> 本表只回答「原版的那个数是多少 / 我们摆成什么样 / 还剩什么没对上」。

---

## 一、数据从哪来（**三处独立来源，互相对上才采用**）

| # | 来源 | 路径 | 说明 |
|---|---|---|---|
| ① | **运行时 UI 树** | `资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_drive_0912.tsv` | 2026-09-12 本机重跑 mod 拿的，**1253 行**。每行 = UI 树一个节点：path / name / activeSelf / anchoredPosition / sizeDelta / anchor / pivot / text / fontSize / color / sprite |
| ② | 同一份树的旧版 | `.../data/runtime_ui_dump_Battle_Arena_1.tsv`（2026-08-25） | 比过 14 个关键节点逐字段一致 → 多数是**预制体静态值**。⚠️ **2026-09-17 更正：至少 `Play`/`Pause` 一项不成立** —— 两源正好相反（dump Play=False/Pause=True；`Play=True/Pause=False`），见 §三之〇 |
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
| **任务点** | `UI_Quest_Points` 97.7²：我 x[1817.0,1914.7] **y[595.4,693.1]**（水晶**下**方）、敌 x[1816.1,1913.8] **y[150.2,247.9]**（水晶**上**方） | ③ B 节 `QuestPointsHolder` 的**绝对坐标**；接片 `BackgroundJoin` 另有一条 | `MyQuestX01/Y01`、`FoeQuestX01/Y01` + 两块 `QuestJoin`。⚠️ **2026-09-12 更正**：原来写「玩家在能量上方 37.4 px、敌方在下方 36.4 px」—— 那 37.4/36.4 是 **接片 `BackgroundJoin`** 相对 holder 的偏移，holder 自己的偏移是 +89.59（敌）/ −88.70（我）。照接片摆会让两个图标都贴在水晶**内侧**。<br>🔴 **2026-09-13 更正（张冠李戴，已修）**：这一件**只属于暗黑天使**，不是通用件 —— 原版按督军阵营开关（`cmp [督军+0x2c],0x6e`=110=DarkAngels），`ManaTypeHolder.Toggle` → `SetActive`。**我们原来无条件摆给全部 13 个阵营**（其余 12 个不该有）。出处与机器码见 `BattleDriver.ShowsQuestPoints` |
| **督军名牌** | 实绘 **382.3 × 126.3** px（贴图 442×146，PreserveAspect）。⚠️ **位置**（2026-09-13 更正）：我 rect x[−38.1,397.6] y[951.4,1077.7] / 敌 x[−37.9,397.9] y[15.6,141.9] → **实绘左缘都在 −11.4**（贴屏幕左缘、出血 11 px），文字中心 (237.25, 999.45) / (240.19, 63.9)、**H=居中** | ③:16 + dump `NameBackground 435.7×126.3`；位置出处 `子代理读报_back左区_0827.md:46,55,69,78` | `worldHeight = 126.3/108`。⚠️ **2026-09-13 修正位置**：原来用 `EnemyInfo(157,108)`/`PlayerInfo(32,977)`（那是 `Alliance Panel` 下**另一份** inactive 实例）→ 我方偏右 44 px/偏高 37 px、敌方偏右 ~168 px。见第四节 |
| **回合灯** | **23.4 × 23.4**（anchor 矩形 22.8×34.5，图 60×59 KEEP_ASPECT 缩进去）。⚠️ **位置**（2026-09-13 更正）：我 rect x[1801.5,1825.3] y[967.4,1003.0] → 绝对中心 **(1813.46, 985.2)** = 相对 230² 牌堆中心 **(83.46, −20.2)**；敌 x[1765.5,1786.3] y[−106.5,−75.4] → 相对 200² 牌堆中心 (73.6, −9) | ① `DeckAndEnergyImage/YourTurnImage`（anchorMin/Max (0.779,0.051)-(0.878,0.201) + **`anchoredPosition (7.9,65.8)`**）；绝对 rect `子代理读报_back右区_0827.md:179-180,193-194` | `DeckLightPx = 23.4`、`MyLightDxPx/DyPx`、`FoeLightDxPx/DyPx`。⚠️ **2026-09-13 修正位置**：原来只按**锚点矩形**折算 = (75.6,−86)，**漏了 `anchoredPosition`** → 灯被摆到牌堆右下角（**偏低 66 px / 偏左 8 px**） |
| **牌堆底板** | `PlayerDeck` 230×230 贴**右下角**；`EnemyDeck` 200×200 贴右上角 | ③ 绝对坐标表 | `MyDeckX01/Y01` |
| **卡背** | `Cardback` 2.1739×3.1364（×100 → 217×314 px） | ③ A2 表 | `DeckCardPx = 314` |
| **攻击方式选择器** | 槽 621.7×122.2；底板实绘 514.8×125.4（`780×285 × localScale 0.44 × 1.5`）；按钮 118.8²；黄圈 176.2² | ① `Drag Attack Selector` 子树 | `AttackSelector.cs`（尺寸全对） |
| **结算面板** | `EndBattlePanel` 子树（Background 3840×2160 / Video Image 1920×1080 @(−6,64) / SkullsHolder 648.1×52.4 / skull 64.3×71.2 / Trophy 60.1×60） | ① dump | `EndPanel.cs` |
| **卡牌放大展示窗** | 遮罩 α0.7725；`Card Display` 752×868 居中；放大卡 ≈467×743 px；下方文字条 y≈921~1078 | ③ front弹层报告 §一 + ① | `CardDisplayWindow.cs`（卡高 743 px 有断言） |
| **换牌面板**（原版 `Mulligan`） | 根 `Mulligan` → `MulliganAnchor` / `ButtonsGroup`(`MulliganContinueButton` + `HideMulliganButton`) / `MulliganText`；底条 `40k_bt_underbutton` 577.5×63.8 · 圆钮 `40k_UI_bt_play` 80.5² · 眼睛 `40k_ui_bt_eye` 82.9×79.6 · 提示行 1344×79.4 | ① dump `:437-450` + 反编译 `MulliganManager__*` / `BattleManager__ClickMulliganDone` | `MulliganPanel.cs`。**2026-09-17 照反编译逐条核过四件**：① 键盘 `Space`/`Enter` = 完成换牌 —— **原版本来就有**（`MulliganManager__Update` 读 `0x20`/`0xd` → `ProcessMulliganDone`；我们原来注释写「原版只有按钮」**已更正**）② 「眼睛」是**开关**（读 `activeInHierarchy` 取反），且**连压暗层一起收**（`ShowMulliganElements`→`Shade.SwitchShade`）—— 我们原来只收按钮，**已补**（自检 2 条断言 + `25b_换牌收起.png`）③ **离线局不做换牌倒计时**有据（`MulliganCountdown` 对 `matchType==0x32` 直接 return；另一个 `MulliganFallbackCountdown` 是**联网掉包**兜底，与离线无关）④ ✅ **2026-09-17：对手换牌照原版做了**（`SimpleAI.AiMulliganIndices`：`manaCost>4` 换掉；`AI` 类 41 个方法体已反编译）—— 原来这里写的是「有据地不做」。⚠️ **每张牌的「换」按钮的位置/大小/文案 + 压暗层的颜色透明度是我们挑的**（dump 是静态树、拿不到运行时生成的按钮） |

---

## 三、还没对上的（**只剩加时**）

> 🔴 **2026-09-17 收口**：这一节原来列的四项（单位语音条 · 回放条 · 等待提示 · 点开牌堆看张数）
> **全部了结**（三件做完、一件查出「没东西可做」）—— 收工记录与「哪些是我们挑的」在 **§三之〇**。
> **现在只剩加时**：规格已查齐 —— 阈值**不再缺数**（用户 2026-09-17 给了判据：**后手方 `MaxEnergy >= 10`**，见 `资料/加时与冲突模式_原版规格.md`）。
> 原先挂在这张表上、已经做完的 15+ 项见 git log（每行都带落点与出处）。
>
> ⚠️ **那 15 行里仍然成立的坑**（行删了，坑留下）：
> · **本回合已出牌数**的 holder 挂在 `LeftArea` 左缘中点，dump 里三枚全 inactive → **看不出它是「玩家侧」
>   还是「当前行动方」**，我们按「我方本回合」做（原版只有三个节点，第 4 张起不显示）。
> · **里程碑骷髅 / 分数**：**权威表 `:195-196` 那两行的 x 是错的** —— 正确值取
>   `子代理读报_back左区_0827.md:56-58`（`RectTransform_2783,3549,3467.json`）。
> · **墓地日志**每行原版画的是**迷你卡**（`CemeteryLogCard : CardScript`）+ `actionImage` 动作图标，
>   我们这版是「小头像 + 一行字」；四条边框**怎么拼没查实**（原版 `Frame` 863×1032.5 与面板 794.1 对不上）。

| 差什么 | 原版实测 | 出处 | 我们的现状 |
|---|---|---|---|
| **加时标记** | 判定 `turnCounter >= overtimeTurn`（每回合开始一次）；表现 淡入 1 s / 停留 1 s / 淡出 1 s + `OvertimeStart` 音效；`OvertimeIndicator` 68.6×71.0（`40k_icon_overtime`） | `OvertimeUi__DisplayOvertime.c` + `MonoBehaviour_4883.json`（`fadeTime:1.0`）；规则书 `:48,:137`「双方能量均达 10 后进入，加时中每回合 +2 能量」 | ⏳ **机制待做**。**标记本身已经摆上了**（`BuildHudExtras`，图 `40k_icon_overtime`、位置 x[1718.9,1787.5] y[341.5,412.5]），但**默认关着** —— ⚠️ **那是原版行为**（`OvertimeUi.Awake` 自己把两个 GO `SetActive(false)` + `alpha=0`），不是我们没做完。<br>⚠️ **2026-09-16 更正**：原来这里写「数值本地确证查不到 ⇒ 要做就二选一」—— **规格其实查齐了，阈值也已由用户判据给定**（后手方 `MaxEnergy >= 10`）：触发 = `turnCounter >= overtimeTurn`（每回合判一次）· 效果 = **经典每回合多抽 1 张**（「+2 能量」是**冲突模式**的）· 表现 = 淡入 1 s / 停留 1 s / 淡出 1 s + `OvertimeStart` 音效（`enteringOvertimeSound` → PathID `6374961739927449780`，**已解出**）。**唯一出处 = `资料/加时与冲突模式_原版规格.md`**（「`overtimeTurn` 取什么」已由用户判据取代） |

> ✅ **「阵营资源」（信仰 / 灵魂石 / 任务点）引擎 + HUD 两侧都做完了**，不在待办里；引擎侧 `PlayerState.Faith` / `SpiritStones` / `QuestPoints`，HUD 侧三组在 `BattleDriver.BuildHud` 里（`PlayerQuestPoints` / `PlayerQuestJoin` · `PlayerFaithHolder` · `PlayerSpiritStoneHolder`；任务点数字是**实时值**，完整更正见 §三点五 ③）。
> **显隐判据（唯一一处：`BattleDriver.ShowsQuestPoints` / `ShowsFaith` / `ShowsSpiritStone`）**：原版按**督军阵营 id**（`+0x2c`）查表 —— 灵魂石 `0x1e`(30) / 信仰 `0x50`(80) / 任务点 `0x6e`(110)；链路 `PlayerManager__ResetMana.c` → `ManaManager.Toggle{SpiritStone,Faith,QuestPoints}Mana` → `RawCardScript__Uses*.c`。🔴 **2026-09-18 更正**：原来写「信仰 / 灵魂石按数值 `> 0`」+「原版 `ManaTypeHolder.Toggle` 的调用方没被反编译」—— **两句都不成立**（判据按符号名搜）。
> ⚠️ **唯一真缺口**：`useWaystone` 的**主动「收集」**没做 —— 现在单位一死直接 +1（`Core/RuleCore.cs` 的 `KeywordTable.Waystone` 分支）：**已知简化、不是静默失效**，语义见 `资料/查证_useWaystone_语义.md`。

---

## 三·〇、界面剩余件（待办第 4 行）· **已收工记录**（2026-09-17）

> 四件的规格 + 现状 + 「哪些是我们挑的」都在这一节（**别抄第二份**）。

| 件 | 原版规格 | 现状 |
|---|---|---|
| **点开牌堆看张数** | `DeckManager__ToggleCardbackPreview` 只有一句 `cardbackContainer.SetActive(state)` | ✅ **结论：别做，没东西可做**（三条实据见下）。唯一真缺的是原版那层 `Cardback Shadow SDF`（原版 292×381、`Cardback_*_SDF` sprite 有 699 个；工程里 `back_*.png` 只有 4 个且没有 SDF 版）—— **独立小事，要做单开一条** |
| **等待提示 / 通用弹窗** | `WaitText` 1344×79.4 + `Dark Shade` 3963.5×3366（纯色 α 0.6118）+ `40k_popup` 1323×90 | ✅ **做完** → `Battle/WaitBanner.cs`（底板照原版：九宫格 `40k_popup` + 平铺 `40k_popup_texture`）。⚠️ **触发时机与文案是我们挑的**（原版查不到），见该文件头 |
| **回放条** | `ReplayButtons` 4 枚 79.80×48.57（`40K_replay_bt_*`；容器 293.60×57.41 @ x[410.2,703.8] y[37.3,94.7]；`Play`/`Pause` **同座标互斥**） | ✅ **做完** → `Battle/ReplayBar.cs`。**坐标悬案已复核：坑 38 对、坑 35 错**（`ReplayButtons` 与 `LeftArea` 是**兄弟**，不在 LeftArea 里；`[-550,1117]` 是树生成器对 stretch 父节点的换算缺陷）—— 已就地更正。⚠️ **四个钮接什么是我们挑的**：重开 / 暂停 / 继续 / 单步；**图标的排法也是我们挑的**（在播亮 Pause、停住亮 Play ⇒ 点了总有反应） |
| **单位语音条** | `PlayerChatDisplay` 648.77×236.50 我 x[12.6,661.4] y[643.5,880.0] · `EnemyChatDisplay` 同尺寸 y[173.5,410.0]；`Background` = `40k_voicelines_radio`(766×280) · `wave` = `…wave equalizer`(708×96) · `ChatText` TMP **fs33 白** | ✅ **做完** → `Battle/UnitChatPanel.cs` + `Core/VoiceLines.cs`；音频 **1844 条 / 606 张卡**进了工程（**工程首次有音频**；2026-09-18 从 1787 补全，见 §三之〇 的更正）。细节见下面「语音条三件事」。⚠️ **两处是我们挑的**：① 事件→台词后缀的对应；② 督军那种没有文本的**退回显示卡名** |

**🔴 「点开牌堆看张数」为什么「别做」（三条实据，别再翻）**：
1. **「看张数」早做完了** —— `BattleDriver` 的张数底板 + 文字有断言守着。
2. **牌背常显是对的** —— `UI_Deck_Background` 那张图是「卡形深色框」，牌背 217×314 **比框还高**；原版 `DisplayDeckSize` 里就有「一并把牌背打开」的分支。之前读到的「`Cardback Container` 默认关」是**预制体静态值**（那几份 dump 的路径根都是 `BattlePrefab/BattlePrefab/…`）⇒ **不是运行时结论**。
3. **原版的点击入口查不到** —— `ToggleCardbackPreview` 反编译 **0 调用点**、场景 JSON **0 命中**（可能是死代码）⇒ 任何手势都是我们编的。

**⚠️ 这批「查不到」的东西**（所以我们才要挑 —— 标「我们挑的」时引用这里）：
语音条的 **`wp` 后缀语义**（各族**触发时机**已于 2026-09-18 定案，见 `资料/语音线_原版规格与ASR管道.md` §1.2；只剩 `wp` 本身未证实）·
回放条的**出现模式与功能**（观战？回放？查不到）· `ToggleCardbackPreview` 的**点击入口** ·
`DisplayDeckSize` 的**文案格式**（两个字面量 `StringLiteral_13658`/`_23310` 未解码，我们的 `DECK N` 是自拟）。

**语音条三件事**（**音频数字的唯一出处**）：
1. **音频源**：`D:/2/新解包资源/assets_full/bundle_*cardassets*_assets_all/AudioClip/`（`D:/2/解包整理/01_卡牌/*/AudioClip/` 是同内容第二份）
   —— 实测 **1857 条 / 70.59 MiB**（**ogg 1742 + wav 115**）。
   ⚠️ **别只 copy `*.ogg`** —— `Guilliman` / `Valius Paxor` 两个督军**全套都是 wav**；死灵族 **44 条不带 `VO_` 前缀**（其中 21 条是被引用的）。
2. **归属**：`数据/索引/card_index.json` 的 `cards[].voice`（**602 张卡 / 1778 条引用，实测逐条都找得到**）——
   index 里**没有 id 字段**，按 **(阵营, 卡名)** 并到我们卡表（**606 张**命中）。
3. **落地**：**1844 条**进 `Resources/Art/audio/vo/`（**被 .gitignore 排除**，和美术同一条规矩）+ 映射
   `Resources/voice_lines.json`（**进仓库**）；运行时查表 `Core/VoiceLines.cs`，脚本 `工具/import_original_audio.py`
   （跑完**必须**再跑 `ArtBaker.ApplyAudioImportSettings`）。两张图已同步进 `Resources/Art/ui/`。

> 🔴 **2026-09-18 更正上面那两个数**：这里原来写 **595 张 / 1787 条**，是**导入管道的缺口**造成的，
> **不是原版的量**。实测漏了 **70 条**（含 `Imotekh the Stormlord` / `Orikan the Diviner` **两张督军整条语音线**）
> —— 根因是**卡名**（`SAU1` 当时叫 `stormlord`、`SAU5` 叫 `Diviner`，都是美术文件名尾段）+
> `(阵营, 卡名)` join 太脆 + 孤儿判据太窄。**已修**，现在是 **606 张 / 1844 条**，
> 残差 13 条逐条核过（占位符/池里没有/写法差异），**不硬塞**。
> 全过程和修法见 `资料/语音线_原版规格与ASR管道.md` §1.5.1 f)。

**⚠️ 顺手纠的两处旧记录**：① `40k_UnitChat_Background_*` 那 4 张**不属于语音条**，是 `ChatPopup` 的边框；
② 权威表把玩家/敌方的 `Background` / `wave` **标反了**（已在该文件就地更正）。
**⚠️ 波形那一层**：原版是**频谱等化器**（`AudioWaveEqualizer`，`spectrumModifier=15` / `speedChange=10`）——
shader **在本地**（`11_着色器/shaders/Shader_2656665607827278157.json`）但 **`m_Name` 空、无字节码** ⇒
只读得到**属性表**那 7 个 uniform、**逻辑拿不到**；场景材质上那批 `_WaveScale1/2/3` / `_Parallax` 是
**Standard shader 残留值、当前 shader 不读**（`CLAUDE.md` 那条坑）⇒ 我们画的是**静态图**（自写等于自己发明）。

### 🆕 2026-09-18 新增两件（**规格与出处都写在代码文件头**，这里只留指针）

| 件 | 代码 | 规格出处 |
|---|---|---|
| **放大窗的语音按钮**（原版 `Voices Over Button` / `CardDisplayWindow.voiceOverButton`） | `Battle/CardDisplayWindow.cs`（`HitVoice` / `PlayVoice`） | 🔴 **2026-09-18 更正**：原写「**本文档 §一**那一行」—— §一 讲的是「数据从哪来」三个来源，**没有这一行**（全文 `88.655` 只在本行出现过）。真出处是**卡预制体的解包值**：`88.655²` @ `x[1650.0,1738.7] y[945.5,1034.2]`、贴图 `40k_UI_bt_voicelines`（已导进 `Resources/Art/ui/`）。播哪条 = `VoiceLines.TryPick(ForDeploy)`，**没语音就明说** |
| **「多张一起看」展示窗**（原版 `UIMultiCardDisplay` / `Generic Multi Card Display Combat`） | `Battle/MultiCardDisplay.cs` | `资料/战斗规格/战斗重建_0827/子代理读报_front弹层_0827.md` **§二**（逐字段权威表）：窗口带 `y[131,949]`、标题 `1192.37×63.204` fs38、遮罩 α0.7725、Continue 条 `577.5×63.84` + 圆钮 `80.47`。🔴 **四处「我们挑的」写在那个文件头**（卡间距 / 卡高 / 滚动改成「缩到装得下」/ **入口 = 点我方牌堆**） |

---

## 三点五、2026-09-13 全面位置核对（用户要求「举一反三，全面检查位置是否正确」）

> 起因：里程碑骷髅压住了名牌文字。**根因不是骷髅，是名牌的位置本身抄错了实例** ——
> 于是把所有可见元素按「1920×1080 绝对矩形」重对了一遍（原版值一律取
> `子代理读报_back左区/back右区/front交互层/front弹层_0827.md` 的 chain_rect，
> 权威表已知有错行、dump 是运行时被改写过的帧——见那份汇总里的「备注1/2」）。
>
> 🖼 **对照图**：`资料/留档_排查证据/战斗UI位置核对_0913/对齐核对_原版矩形vs我们.png`
> —— 在我们自己的截图**上画出原版的绝对矩形**，一眼看出对齐没对齐（原版真渲图里没有 HUD，
> 所以尺子只能取字段值；为什么这么做、怎么复现，见那个目录的 `README.md`）。

**① 核对后改掉的（都是位置错，不是尺寸错）** —— ✅ 已全部修完（明细与错因见 `资料/规则引擎_进度与交接.md` §〇·二 ⑤）：
督军名牌（我/敌）原来抄的是**另一份 inactive `PlayerInfo`/`EnemyInfo`**（我方偏右 44 px/偏高 37 px、敌方偏右 ~168 px）· 名牌文字改**居中** · 牌堆回合灯原来只按**锚点矩形**折算、**漏了 `anchoredPosition (7.9,65.8)`**（灯被摆到牌堆右下角，偏低 66 px）。
**② 核对后确认「本来就对」的**（都有断言守着，别再动）：牌堆 230² 逐像素相等 · 张数底板/文字 · 能量水晶与底板（四块）· 任务点两个 + 接片 · `END TURN` 130.7×80.4 · `SettingsBtn` 63.9² · 已出牌数三枚 · 里程碑骷髅/分数 · 技能面板 · 卡牌放大窗 752×868。

**③ 核对出来的「原版有、我们没有」**（不是位置错，是缺件）——
✅ **除最后一行外全部摆上了**（`BattleDriver.BuildHudExtras`；断言在 `BattleScene.Run` 第 16 节）：

| 件 | 原版实测 | 状态 |
|---|---|---|
| `TitleBackground` 头衔底条 | 311×42（图**原生 1:1**）我 x[54.2,365.2] y[1028.5,1070.5] / 敌 x[54.5,365.5] y[92.8,134.8] | ✅ 摆上（**在名牌后面** —— 原版它就是名称条的底，z 序有断言） |
| `Avatar Item Small` 头像块 | 容器 155.6×136.5（我 x[−19.7,136] y[948.1,1084.6]，左缘出屏） | ✅ 摆上。⚠️ **三处照 dump 修过**：① 实绘的 `Border` 在 `Image Container` 里（stretch `size(0,-37.4)`）→ 实际 **155.64×99.1**；② 它是 `PlayerName` 的子节点、**画在名牌上面**（z 序有断言）；③ 原版 `Border` 是 stretch **不保比例**，我们 `SetAspect` 照做。原版场景态里 `avatarImage` **m_Enabled=0** → 我们**只摆框不摆立绘** |
| `ChatButton` | 64.4×61.9，我名牌下 x[50.9,115.4] y[880.2,942] | ✅ 摆上（⚠️ 🔴 **2026-09-18 更正：那一个对象上挂了两个组件** —— `Button.m_OnClick → BattleManager.ClickChat`（**开 `ChatPopup` 面板**）**和** `PlayerStateToggle.selectedBool = EnableWarlordVOs`（语音开关）；原来只写「是敌方语音开关」是**半对**。哪个生效**待跑实况**，见 `资料/语音线_原版规格与ASR管道.md` §1.7.0） |
| `ShowCemeteryBtn` | 64.5×64.2，敌 x[52,116.4] y[135.9,200.1] | ✅ 2026-09-13 早先已做（墓地日志入口） |
| `CenterCameraButton` | 64.4×61.9（x[17.9,82.4] y[568.2,630]） | ✅ 摆上 |
| `OffensiveButton` | 109×106.9（x[0,109] y[446.9,553.8]） | ✅ 摆上 |
| 任务点**数字** `QPText '0/3'` | fs40.5 **Bold** 白（我 x[1841.8,1889.9] y[621.4,666.6]） | ✅ 画上了（中心**正好等于任务点 holder 的中心**）。⚠️ **更正（第三十三轮）**：这一格原来写「引擎里没有任务点机制 → 数字恒为 0/3」—— **机制确实接上了**（`PlayerState.QuestPoints` + 判定阈值）。✅ **2026-09-18 二次更正：数字取实时值 —— 上一版是对的**（`BattleDriver.UpdateHud` `:4371` 里 `SetText($"{me.QuestPoints}/3")`；`UpdateHud()` 挂在 `AdvanceTimeline` 上 ⇒ 批处理里也是活值）。⚠️ 同一天早些时候这里被「更正」成「写死 `0/3`」，**那次更正才是错的**：只看了建标签那行的初始文本。自检那条 `QpText == "0/3"` 能过是**因为这一局双方真的一分都没有**，不是判据 |
| `Energy Accumulation`（ON/OFF） | 77.8×80.1（x[1746.7,1824.4]，在能量球**左侧**） | ✅ 显示哪张**判据已查到（2026-09-17）**：**`0 < manaAccumulation`**（`GameplayVariablesData`，反编译 `BattleManager__SetupBoardPhase.c:181/196`）—— 原来这里写「ON 什么时候显示没查到 ⇒ 固定 OFF，是我们挑的」，**已推翻**（见 `资料/自设计清查_0917.md` §一 #8） |
| `OvertimeIndicator` | 68.6×71（x[1718.9,1787.5] y[341.5,412.5]，图 174×180 preserveAspect） | ✅ 图接好了、**默认关着** —— ⚠️ **2026-09-16 更正：那是原版行为**（`OvertimeUi.Awake` 自己 `SetActive(false)`），**不是「我们还没做」**；加时**规格已查齐**（阈值判据由用户给定，见 §三「加时标记」一行） |
| **选卡菜单 / 等待提示 / 回放条 / 单位语音条** | — | ✅ **全部做完**（2026-09-14 / 09-17）：选卡菜单三族共用一个面板（`资料/选牌Choose_数据与设计.md`）· 另外三件见 **§三之〇**（**唯一出处**）。换牌（Mulligan）更早，见 §二 |

**④ 我方**自加**的（原版没有，别拿原版去"修"）**：中上那行 `TurnLabel`（原版表示回合归属**只靠牌堆上的灯**，
全场景 `m_text` 里唯一的 TURN 是 `"END TURN"`）· `HintLabel` 提示行 · `ResultLabel`。

**⑤ 手牌数底板的位置仍是「我们挑的」**：静态 JSON 把它算在屏幕正中（x[816,1104] y[492.6,582.2]），
但它的祖先链是**纯 Transform**（`HandArea`/`PlayerArea`）、节点本身 `activeInHierarchy=False` —— 那串数不可信，
所以没照它摆（🔴 **2026-09-18 更正：原写「见第三节那一行」—— §三 已无那一行**（09-17 收口时删了）；
这一项的去处见 **§三点五 ②** 与 `资料/规则引擎_进度与交接.md` §〇·二 ⑤）。

---

## 三点六、卡牌高亮 / 柔光（2026-09-13 查实）—— **我们做的不是原版那一套**

用户问「这个柔光你是否实现？是否按解包资料的说明实现」。查完的账：

| | 原版 | 我们 |
|---|---|---|
| 软光/影 | `Card Highlight And Shadow`：**4.4281×4.4281**（比卡本体 2.09×3.33 大得多）@(0,−0.0126)，是 `Front` 的**最底层**（在立绘之下）。它的 `Image` **没有 sprite（PathID 0）—— sprite 运行时由 `CardHighlight` 组件生成**（SDF 软光/影）。材质 PathID `-3316280387615011577` | 一圈 **1.09×1.06 的羽化描边**（`SoftRimTexture`，`Sprites/Default` 染色）。**没有那张大软光** |
| 状态染色 | `FrameHighlight` / `FrameHighlightRemnant` 两个 **SpriteRenderer**，按状态染 5 个**序列化颜色**：`ValidTargetColor` / `SelectedColor` / `PlayableColor` / `SelectedTargetColor` / `RegularColor` | **状态集合**是我们自己挑的（`CardHighlightState` 6 态：Normal/Playable/Unplayable/Selected/ValidTarget/Hover）；**颜色 2026-09-18 已换成原版值**（`Core/CardHighlight.cs`） |
| 状态集合 | `regular` / `potentialTargetInHand` / `selected` / `potentialTargetInBoard` / `selectedTargetInBoard` / `displayingActiveAbility` | 语义不同（我们分「可打出/不可打出/悬停」，**没有**「手牌里潜在目标 vs 场上潜在目标」这两档） |
| 动效 | `CardBodyToScale` × `ScaleFactor` 的**缩放补间**（DOTween，时长 `CardHighlightAnimTime`）+ 小兵将死时的 `minionWillDieAnimation` | **没有缩放补间** |

出处：`d:/2/Warpforge_code/Scripts/Assembly-CSharp/CardHighlight.cs:5-60`；场景节点
`07_场景/battlearena1/GameObject/Card Highlight And Shadow_1145.json` + `MonoBehaviour_4309.json`（挂 `CardHighlight`）
+ `MonoBehaviour_4320.json`（那个 `Image`，`m_Sprite` 为空）+ `子代理读报_2dcard_0827.md:35,72,308`。

> 🔴 **2026-09-18 更正：本条原来写「那 5 个状态色的数值本地没有 · `d:/2/解包整理`（44.9 万文件）全库 0 命中
> · 带这些字段的卡预制体没被解出来」—— 那是错误否定。**
> **错因**：**在战场场景里搜的，而它们在卡牌预制体里。**
> **实据**（`08_预制体特效/战斗预制体/MonoBehaviour/MonoBehaviour_-3885077450169410624.json`，逐字段实读）：
> `ValidTargetColor (0,1,0.129)` · `SelectedColor (1,1,1)` · `PlayableColor (1,1,0)` ·
> `SelectedTargetColor (1,0.518,0)` · `RegularColor (1,1,1,0)`；**`CardHighlightAnimTime = 0.1`** ·
> **`ScaleFactor = 1.05`**。态映射（`CardHighlight__ChangeState.c`）：0/1/2/3/4/5 → Regular/Valid/Selected/Valid/SelTarget/Playable。
> **旁证**：这份值与我们 **2026-09-10** 就记在 `Unity/_资源评估_场景特效动画.md:506,617` 的那份**逐值吻合**
> —— 两个独立来源对上 ⇒ **可以定案**（当时那份自述「扫 7 个 UI bundle 得到的 1 个 CardHighlight 原型」）。
> ⇒ ✅ **2026-09-18 已落地**：那 5 个原版值已照着接进 `Core/CardHighlight.cs`（3 个直接照抄；
> **两处故意不照抄**：`RegularColor` 的 alpha=0、我们本来就没有的 `selectedTargetInBoard` —— 理由写在那个文件头）。
>（本条 2026-09-17 曾记「用户点名先按新解包路径再找一次」—— 那次重找仍然没找到，因为找错了目录；
> 2026-09-18 的复核找到了。**别再按「颜色得自己挑」当结论。**）

---

## 三点七、「我说没有、其实本地有」的对账（2026-09-13 用户质疑后逐条核）

**仍然有效的四条**：
- ⚠️ **墓地日志的动作图标（9 种）= 唯一一条「真没有」** —— 3207 个 sprite 名里 `cemetery` 0 命中，`actionImage` 那个预制体不在任何已导 bundle。那 9 种对应的是**本地化文本 key**（`Battle/Cemetery/ActionAttackMelee` …），不是图。
- ✅ **单位语音音频 —— 2026-09-17 进工程、2026-09-18 补全**（**1844 条 / 606 张卡**，`Resources/Art/audio/vo/`；规模真值与「别只 copy ogg」那些坑见 **§三之〇**，那里是**唯一出处**）。
- ⚠️ **`ChooseCardMenu` / `Mulligan`：结构与图都在**（原版是「场景根 + 代码生成卡片」：`SetupChooseCardsUi` / `CreateDisplayCard`，卡面复用 `BasicCardUI`）⇒ 要做不用重新挖。换牌**已经做完了**（`规则引擎_进度与交接.md` §〇·二 ④）。
- ⚠️ **原版卡面描边**：「组件里没有、在材质里」**说法成立**（`BasicCardUI`/`CardScript` grep outline/shadow = 0；描边在 TMP 材质 `_FaceDilate`/`_OutlineWidth`）。

**教训**：说「没有」之前要按 `解包资源使用地图` → `ui_extract` → 全盘搜 这三步走完（CLAUDE.md 铁律 5 早就写着）。

---

## 四、悬案（**两边证据冲突，别照猜着改**）

| 事项 | 证据 A | 证据 B | 结论 |
|---|---|---|---|
| **攻击方式选择器三钮的位置** | ① dump：近战在槽**左下角**、Active 与 Range **叠在槽中心** —— 但整条 `Drag Attack Selector` 的 `activeSelf=False`，这是**预制体静态值**，运行时很可能由代码重排 | `AttackSelector.cs` 文件头写着「原版三钮同位叠加是错的、以横排为准」，我们做成了横排 | **没定论**。要定得拿到真实战斗里的实况（需要对局数据，现在拿不到）。改动前先看 `资料/规则引擎_进度与交接.md` **§「攻击方式选择器」** 那一节 |
| **牌堆底板位置** | ③ 绝对坐标表：`PlayerDeck` 贴屏幕右下角 | ① dump 里 `PlayerDeck` 是 stretch 容器（size −0.0,−0.1），**读不出绝对位置** | 按 ③ 做了。**单一来源**，看着不对就说 |

---

## 五、这次量出来的中间值（做别的效果时可能用得上）

| 量什么 | 结果 | 怎么量的 |
|---|---|---|
| **卡框的透空窗口** | Ultramarines 卡框（1024²）不透明 bbox `x[201,808] y[56,991]`；窗口 `x[283,741] y[122,915]` → 占 bbox 的 `u[0.1349,0.8882]`、`v(从顶)[0.0705,0.9177]`；换成卡单位是 `x∈[−0.8197,0.8716] y∈[−1.3905,1.3690]` | 对卡框图 alpha 做「**先膨胀 2 px 封住抗锯齿的缝**、再从图外 flood fill」，剩下的闭合透明区就是窗口。⚠️ 不膨胀会从 1 px 的缝漏进去，把整张图判成窗口（第一次就栽在这）<br>⚠️ **2026-09-12：这个「窗口」不该被当成裁剪区用**（做法见 §五点五）—— 原版是「立绘按自己的矩形铺出去、由卡框 alpha 遮罩」（`2dcard` 规格 :310）。窗口是**拱形**，拿它当矩形裁剪框会让立绘的直角顶出拱形 |
| **卡牌插图 sprite 的真实矩形**（🆕 2026-09-12） | 纹理 **1024×1024**，sprite `textureRect = x176.5, y0, **670.5 × 1024**`（宽高比 **0.6548**） | `d:/2/新解包资源/assets_full/bundle_spacemarinesultramarinescardassets_assets_all/Sprite/SM_UM_inf_Aggressor Sergeant.json` 的 `m_RD.textureRect`。⚠️ 这个字段**旧解包（`解包整理`）里没有** —— 旧的直接把图裁成 660×1024 给我们，宽度都不对。卡本体宽高比是 0.628，**插图就是照着「铺满卡片」设计的** |

⚠️ 另外两条（**卡框上宝石的实心中心** / **原版插图库的匹配办法**）已经并进卡面线。
🔴 **2026-09-18 更正**：原写「插图匹配现在的账是 **1111 张配上 / 19 张配不上**」—— 那个数**与 `规则引擎_进度与交接.md` 的「18 张」对不上，两边又都没有互证判据** ⇒ **本地数字删掉**，
**名单与裁定只看 `资料/卡表核对_卡图提取/裁定_*.md`**（本表不再抄第二份）。
⚠️ 另注（仍成立）：立绘文件名按**卡名**生成 ⇒ **跨阵营同名卡会互相覆盖**（`art_aggressor.png` 是太空野狼那张，暗黑天使 `DA12 Aggressor` 挂着别人的画）。

---

## 五点五、卡面装配（`2DCard`）—— 2026-09-12 对着**原版成品卡图**重对了一遍

**尺子**：`d:/2/Warpforge部队卡片/<阵营>/<1督军·2天赋·3部队·4计策·5防御卡>/*.png`
—— 原版拼好的 **PnP 成品卡图**（900×1200）。以前从没拿它比过，是我们卡面一直「差点意思」的根因。
渲染我们自己的卡对照：`CardFaceProbe.Run`（单卡渲染探针，输出 `_tmp_view/cardface/*.png`）。

| 元素 | 原版成品卡 | 我们（改完之后） | 出处 |
|---|---|---|---|
| 卡名 | **白**，居中，名字正中偏下 | ✅ 白（原来是**橙**） | 成品卡 + A3 表（⚠️ A3 把 name/army 两行的颜色**记反了**） |
| 阵营行 | **橙**，紧贴名字下方 | ⚠️ **只该在「放大窗」里印**（**用户 2026-09-17 口径**：手牌与场上**不印**；场上连卡框与效果文本都没有）—— 我们现在是**三处都印**，待改（第 12 行） | `ArmyTextUnit`(0,+0.31) → y01 0.6394；`ArmyTextTactc`(0,+0.13) → 0.6935 |
| 效果文字 | 米白多行，居中 | ✅ 米白；**下沿**贴版面框底边、按可用高度自动缩字号 | `DescTextUnit`/`DescTextTactic`；框底 0.8466 / 0.9061 |
| 兵种行 | **橙**（`Infantry` 这种），排在效果文字**下面** | ✅ 橙（**原来也没画**） | `RaceText`(0,−0.452) → y01 0.8682；**战术卡没有这一行** |
| 立绘 | 只在卡框的**拱窗**里，卡框外是空的 | ✅ 立绘板 = 卡框 quad（2.07–2.12 宽），UV 取「原版 2.7484² 里落进板内的那一块」 | `CardImage` 2.7484²@(0,−0.044) + 卡框 alpha 遮罩 |
| 数值 | 左下红/紫、右下一绿、右侧一盾（**只单位卡有**） | ✅ 战术卡不再画数值和盾 | 成品卡 + A2 表 |
| 费用 | 右上蓝宝石 + 白数字 | ✅ 真图 `Card Frame Cost Icon` | A2 表 |
| 稀有度 | 底部中央小菱形 | ✅ 0.258×0.2335 | A2 表 |

**立绘装配 / 立体感（3DLit）/ 卡框 `Read/Write` 三个坑 —— 已经写在 `资料/规则引擎_进度与交接.md` §〇·二 ⑧⑫ 与 `资料/卡牌基座_进度与交接.md` §四·五**，本表**不再抄第二份**。要点三条：
① 立绘板 = 卡框 quad（2.07–2.12 宽），UV 取「原版 `CardImage 2.7484²`@(0,−0.044) 里落进板内的那一块」，**再裁到卡本体矩形**（原版卡根有 RectMask2D：卡图比卡大、溢出先被卡矩形裁、再被卡框 alpha 掩）。三条踩过的坏法：立绘板取「卡框 quad」→ UV 出 [0,1] → Clamp 拉出**一圈白边**；fit 到方形不裁 → **画到卡框外面**；居中放不限制高度 → 效果文字一多就**往上长盖住阵营行**。
② 立绘贴图的 alpha **就是角色的抠图轮廓、是真数据**（单位卡 91–97% 全透明、战术卡 0%）—— 正确做法是**软 alpha 原样保留** + 角色外一圈 **8 px 渗色**；早先「**二值化（>127→255）**」和「**导入时统一写 alpha 255**」**两条都被推翻**（前者会给角色轮廓外造出一圈亮边）。我们在 PnP 的合成顺序上**故意**把立绘盖在卡框上面（用户要的就是 PnP 那个观感）。⚠️ **没做**：`AutoCardRotation` 整卡倾摆 / SDF 光影层 / 战场 3D 网格；`Shaders/FrameCutout.shader` 那条思路**没调通**（遮罩采样错、插图碎成马赛克）。
③ **战术卡框贴图没开 `Read/Write`** → `FrameUv` 量不出不透明 bbox、退回整张图 → 卡框被画成一块 2.2452² 的方片（**所有战术卡**都错）⇒ **加完美术要跑一次 `ArtBaker.ApplyImportSettings`**（判据在 `FrameQuad` 上）。另：`Unplayable` 态的手牌**不描边**（描边用羽化贴图），不然会在卡的透明角**露出一圈灰白硬边**。

---

## 六、**我们加的**（原版没有这一件 —— 别当成复刻，也别拿原版去「修」它）

| 元素 | 我们是什么 | 为什么加 |
|---|---|---|
| **提示行上的「本局用哪副牌」** | 提示行（两行棋盘中间那条缝，`_hintLabel` @ 0.5 / 0.507）的**休息态文字**，三种：<br>· `本局用你编的「X」· N 张（效果本版解析不了的战术卡）没上场`<br>· `你的卡组「X」不合法（原因）—— 本局退回自动凑的一副`<br>· `卡组存档读不出来（原因）—— 本局自动凑了一副`<br>游戏过程中的临时提示（选目标 / 不能行动…）盖在它上面，用完自动落回来（`BattleDriver.SetHint`） | **为什么加**：引擎只收**能解析的**卡，而这件事原来只写在 `Debug.LogWarning` 里 —— 打包后玩家一个字都看不到，画面上**牌却确实少了**（看起来像卡组没生效）。⚠️ **战术卡 2026-09-12 / 防御卡 2026-09-13 都已打通** ⇒ 现在会丢的只剩「效果本版解析不了的战术卡」。⚠️ 结算时提示行会**真清空**（不然从结算面板底下透出来）；卡组名**截断 14 字**（`Label` 是 NoWrap 的）。⛔ **原版没有也不需要这一行**（它全卡种都能上场）—— 明细见 `资料/规则书_实现指南对账.md` §一·④ |
