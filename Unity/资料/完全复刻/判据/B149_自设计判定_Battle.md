# B149 · 自设计逐条判定 —— `CardPresentation/Battle/`（60 条）

> 输入 = `判据/自设计清单.md` §2 明细里**位置在 `CardPresentation/Battle/` 下的 60 条**（census 2026-10-25 生成；本轮 2026-11-05 现读，行号已漂移的按**现读行号**给）。
> 口径（用户 2026-11-05）：「**所有与原版不一样的地方都需要改成原版**」，唯一例外 = **判据在已关服远端、本地永远拿不到**（§18·0），且必须给反证。
> 判据源：`D:/2/tools/decomp_full/`（`.c` 为准）· `cpp2il/out_cpp2il_dev_ilspy`（交叉）· `assets_full` · 官方本地化表。
> ⛔ 本文件只做判定，不改代码/账/表。

## 汇总

| 档 | 条数 | 含义 |
|---|---:|---|
| **A 能改成原版** | **11** | 判据在本地，给出改法与落点即可动手 |
| **B 判据在远端 / 原版本身没有** | **2** | 附反证（远端 RemoteConfig / 服务端字段） |
| **C 架构级** | **8** | 改它要动整层（世界空间↔uGUI 双栈、坐标桥、相机生命周期） |
| **D 误标（其实已是复刻 / 只是注释）** | **39** | 不需要动 |

> ⚠️ 60 条里 **39 条（65%）是误标**：census 只按关键词命中注释，而 2026-10-25 之后大量条目**已经被改成原版值并留了 `✅ 已复刻`**。⇒ **别按清单批量动手**。
> ⚠️ `#3` 与 `#10` 是**同一个常量**（`TutWarlordBeatSeconds`），实际只剩 **10 个不同的 A 落点**。

### A 档里最该先改的 10 条（按「产品可见度 ÷ 改动成本」排）

1. **#180** `BattleDriver.cs:9103` —— 联机换牌等待文案我们多加了 `"…"`，官方词条是 `Waiting for enemy`（无省略号）。删一个字符串拼接。
2. **#6** `BattleDriver.cs:7154/7273` —— 原版 `CanChat(isPlayer=false)` **没有**开场独白闸，我们自加了一条 ⇒ 删。
3. **#183** `BattleDriver.cs:16522` —— 原版 `ShowHeadsUpMessage` 只有入队/到点出队、**没有「撤下」**，我们的 `SetHint("")=ClearTips()` 是自造语义（4 个调用点在用）。
4. **#13** `CardChoicePanel.cs:1229` —— 「点卡本身也能选」是自加输入路径（原版只有卡上那颗按钮）。
5. **#2** `BattleDriver.cs:2604-2613` —— 手牌张数底板摆位「算不出绝对位置」**不成立**：同族父链上一轮已算出 `enemyHand (960,50)`，照同一手法可标定。
6. **#3/#10** `BattleDriver.cs:16993` —— 督军两拍 `0.6s` 是自挑；原版 `WaitForSeconds` 实参在 xmm0（`.c` 没落），VA 反汇编可取真值。
7. **#194** `EnvironmentApplier.cs:255-257` —— 环境淡出多一道 `blendTime+1s` 兜底超时，原版只按 `OnComplete` 计数。
8. **#1** `AttackSelector.cs:1102` —— 技能钮 tooltip 落点 70px 自定；原版 = `设置向量 × tooltipVerticalOffset(−100)`，只差「那个静态设置向量」没标定（属**还没查清**，不是原版没有）。
9. **#184** `BattleDriver.cs:17658` —— 聊天「谁说的」在 `unitType` 认不出时按动作档**猜一侧**，原版只认卡片的 `+0x40`。
10. **#185** `BattleDriver.cs:27798` —— 单位无 desc 那档标「近似」，其实本仓已有原版节点常量 `CardView.UnitNameNoDescW = 1.6`。

## 明细（60 条）

| # | 位置（现读） | 档 | 原版判据（文件:行 / 值） | 我们现值 | 改法（落点） | 依据一句话 |
|---:|---|:--:|---|---|---|---|
| 1 | `Battle/AttackSelector.cs:1102-1103` | A | 技能钮 tooltip：`localPosition = 设置对象向量 × tooltipVerticalOffset`（`decomp_full/CardDisplayAttackTypeButton__HighlightIfNeeded.c:96-104`）；`tooltipVerticalOffset = −100`（`MonoBehaviour_4449.json`） | `TooltipDropPx = 70f`（自定落点距离） | 先定位 `DAT_1842da2a8+0xb8` 那个静态设置对象取出向量，按 `× (−100)` 落点；标定前保留 70 并钉 TODO | 原版**有**行为判据，只差一个静态向量未标定 ⇒ 属「还没查清」，不是「原版没有」 |
| 2 | `Battle/BattleDriver.cs:2604-2613` | A | 原版 `CardsInHandText` 两个实例都在包里（`bundle_scenes_scenes_battlearena1/GameObject/CardsInHandText{,(1)}.json`）；绝对位可由父链 `anchoredPosition` 累加（同族先例：`enemyHand` 锚点 = (960,50) px，见 `判据/enemyHand屏幕锚点_判定.md`） | 位置标为「我们挑的」（贴在既有手牌标签上），`HandPlatePx = 65.7` 尺寸已是原版值 | 按父链累加算绝对 px，替换 `HandPlate` 的摆位；尺寸不动 | 「祖先链纯 Transform、算不出绝对位置」这条**不成立**（同类父链上一轮已算出过） |
| 3 | `Battle/BattleDriver.cs:4754-4760`（常量 `:16993`） | A | 原版 `TutorialStartSequence` 两次 `WaitForSeconds`（`decomp_full/BattleManager._TutorialStartSequence_d__592__MoveNext.c:45,89`），实参在 xmm0、Ghidra 未落进 `.c` | `TutWarlordBeatSeconds = 0.6f`（自挑） | 反汇编 `WaitForSeconds___ctor` 前一条 `movss xmm0, …` 取真值，替换常量 | 常量就在机器码里（本仓 `read_literal.py` / RVA 那套是现成手法） |
| 4 | `Battle/BattleDriver.cs:7058` | B | 原版读远端 `AIBotsConfig.EmotesConfig.reply_chance`（消费点 `cpp2il …/VoiceLinesController.cs:1464-1482`；provider = `Everguild.LiveOps.Config.ConfigProvider<T>.Initialize(Dictionary titleData)` 吃 PlayFab titleData） | `AiReplyChance = 0.75f` | 不动（保留本地默认 + 出声） | 反证：`assets_full` 与安卓 1.35.0 远端包逐字搜 `reply_chance`／`emote_chance_after_action` **0 命中**；`AIBotsConfig` 字段无默认值（全靠反序列化） |
| 5 | `Battle/BattleDriver.cs:7062-7064` | D | 原版 `AIResponse` 全文**没有冷却**支 | `AiEmoteCooldown` 已停用，只剩常量给旧断言 | 无（可顺手把常量连同旧断言一起清） | 2026-11-02 `B118_W23 #52` 已按铁律 11 删掉这道自造护栏 |
| 6 | `Battle/BattleDriver.cs:7154-7155`、`:7273` | A | `VoiceLinesController.CanChat(isPlayer=false)` 只看 `IsDuringBattle` + `chatMuted`（`cpp2il …/VoiceLinesController.cs:1344-1369`，**无 popUp/intro 闸**；`isPopUpShown` 只走 isPlayer=true 支） | 多一条「开场独白期间（`_introStage >= 0`）AI 不说话」的本地护栏 | 删掉该条件（`CanAiChat`） | 原版判据明确**没有**这一支 |
| 7 | `Battle/BattleDriver.cs:9963-9966` | C | 原版 reveal 起点 = 世界零（z=0，不在 UI 平面上） | `RevealStartPx = (960,660)`（🔶，调度台 2026-10-27 裁定） | 要真复刻需让 reveal 走「世界空间→屏幕」投影，动 `LayoutSpace` 与坐标桥 | 正交 UI 相机下世界零不可表示 ⇒ 架构差，不是单个数值 |
| 8 | `Battle/BattleDriver.cs:10350-10353` | D | 原版 `ChatManager.IsPlayerFirstIntro` 只看 `entry.talksFirst` | 已按它实现（`IntroFirstSide`） | 无 | 注释是历史订正行，已是复刻 |
| 9 | `Battle/BattleDriver.cs:13167` | D | —（玩家可见文案） | 「别以为打的是自己编的那 30 张」提示句 | 无 | 「自己编」指玩家自建卡组，不是自设计值（纯误命中） |
| 10 | `Battle/BattleDriver.cs:16990-16993` | A | 同 #3 | 同 #3（`0.6f`） | 同 #3 | 与 #3 是**同一个常量**，合并一笔改 |
| 11 | `Battle/BattleDriver.cs:17748-17750` | D | 原版直接吃 `VarsGlobal.tipDuration`，无下限兜底 | `TutTipDefaultLimit = 10f`（数据 ≤0 时兜底） | 无（要洁癖可删） | 61/61 条数据全是 50/120 ⇒ 分支**不可达**，无行为差异 |
| 12 | `Battle/Card3DAnimationController.cs:397-409` | D | 原式 `OneFrameSlack / Application.targetFrameRate + t < 0`，`__Update.c:22-23` 原样喂值；出厂 `FPSLimit=2` ⇒ `targetFrameRate=-1` | 已删 `if (fps <= 0) fps = 60` 的钳制 | 无 | 2026-11-03 `B121-WB` 已按原版改回并留证 |
| 13 | `Battle/CardChoicePanel.cs:1228-1235` | A | 原版选卡只有卡上那颗按钮（`ChooseCardMenu__ClickChooseDone.c:13-18`；本文件 `:1213-1216` 亦引）；卡体点击**无**选择路径 | 额外一条「点卡本身也能选」 | 删 `CardChoicePanel.cs:1229` 那段 hit-test 循环 | 这条输入路径是我们自加的，原版判据在 |
| 14 | `Battle/CardDisplayWindow.cs:187-194` | C | 原版靠 uGUI 真层级（同父件兄弟序）决定前后 | 用 `ZGroup = ZContent` 在 `HudCanvas` 登记兄弟序 | 改需连 `HudCanvas` 的分组/登记方案一起动 | 层序等价物是架构选择，不是单值 |
| 15 | `Battle/CardDisplayWindow.cs:196-202` | C | 原版 `Menu Dark Background` 自带 `BackgroundCloseButton`，与窗同一棵、同一 Canvas | 我们把压暗留在世界空间，画布上只放隐形关闭区（`ZBackdrop = ZContent + 0.5`） | 把压暗层迁进画布 = 动文件头「没迁的两样 ②」整块 | 世界/uGUI 双栈造成的绝对值，属架构 |
| 16 | `Battle/CardDisplayWindow.cs:227-230` | C | 原版这一整棵都在 `Card Display Window` 一级 | 多一层世界壳 `Card Display Window (world)` | 同上（把卡体那族迁进子画布） | 同上 |
| 17 | `Battle/CardDisplayWindow.cs:695` | D | 原版 `SoundManager.Play2D(…, 2)` ⇒ `set_volume(1.0)`（`CardDisplayWindow.__c__DisplayClass62_0___SetCardVoiceOver_b__0.c:63`） | `volume = 1f` | 无 | 2026-11-05 `B144-B2` 已复刻并留判据 |
| 18 | `Battle/ErrorMessageBanner.cs:367-373` | C | 原版只给相对次序：`UI Error Message Controller` = `PopUps` / order 1 | `MenuQueue = 4035` 绝对值 | 无（相对次序已复刻；绝对值是我们队列带的坐标） | 改它要重排整条窗带（4030/4041/4047），属架构 |
| 19 | `Battle/MultiCardDisplay.cs:313-316` | C | 原版靠 `ScrollRect` 自带分派（`MonoBehaviour_5036.json`：`m_DragThreshold` 等原版字段） | `DragSlopPx = 6f` 自定阈值 | 让全屏背景关闭件不吃抬手（改事件分派），阈值自然不需要 | 根因是我们的关闭件实现，属架构 |
| 20 | `Battle/MultiCardDisplay.cs:331-332` | D | 原版子树 `RT_3370.m_Children` = 四件 | 自拟提示 `mcd_hint` **已删** | 无 | 命中行是「已删」的历史注记 |
| 21 | `Battle/ReplayStore.cs:364-366` | B | 原版回放存服务端：`BattleLogMatch.replaysVersion`（`+0x78`，服务端→客户端），客户端只跟 `"-1"` 比（`BattleLogItem__Initialize.c:206`） | 本地库 `MaxKept = 50`（用户 2026-09-27 定） | 不动 | 反证：本地没有任何回放内容生产者，判据在服务端 |
| 22 | `Battle/SkillPanel.cs:383-390` | D | `ActiveSkillDisplay__FadeOut.c:26` × `DAT_1834b2bb4 = 0.5f`（`read_literal.py` 现读） | `FadeTime 0.2` / `FadeOutFactor 0.5` | 无 | 2026-10-24 `E17` 已解出常数并改 |
| 23 | `Battle/TutorialOverlay.cs:190-191` | D | 设计像素→世界单位 = `LayoutSpace.DesignPxH / DesignHeight = 108` | `Px = 108f` | 无 | 值本身就是判据 |
| 24 | `Battle/TutorialOverlay.cs:246-247` | D | 原版材质 `Tutorial Highlight` 的 `_UVScale = (1.48, 2.0)`（`Material_509935442048338405.json`）+ VS 反汇编算式 | `HlUvScaleX/Y = 1.48 / 2.0`；旧 `HighlightPad = 0.06` **已删** | 无 | 2026-11-02 `B117-G10-23` 已按判据删自设计 |
| 25 | `Battle/TutorialOverlay.cs:248-252` | D | 原版 `SkipTutorial Button` 面板内 `(+171.7, −310.5)`、300×90（已标定） | 旧「我们挑的」摆位已删；钮已搬进 `SettingsPanel` | 无 | 「父链偏移没能标定」后来有了确切答案 |
| 26 | `Battle/UnitChatPanel.cs:760` | D | 原版气泡自带 `AudioSource`：`m_Volume = 1.0` · `Priority = 154` · `Spatialize = 0`（4 场逐值同） | `volume = 1f, priority = 154` | 无 | `B117-G10-22`（2026-11-03）已改 |
| 176 | `Battle/AttackSelector.cs:866-877` | D | 原版 `Drag Attack Selector` 子树恰好 4 件；官方表 `ChooseAction` 0 命中 | 两行自造文字（标题+技能小字）**已删** | 无 | 已按判据删除 |
| 177 | `Battle/BattleDriver.cs:2079-2081` | D | 原版词条 `Battle/Overtime/Title`（`Localize.mTerm` + TMP 原文 `OVERTIME!`） | `OvertimeTerm` 同键 | 无 | 「兜底」指词条兜底，不是自设计值 |
| 178 | `Battle/BattleDriver.cs:2910-2918` | D | `addedManaSecond` 是规则书口径（经典 0 / 遭遇 1），不是 LiveOps 下发 | 已按现读订正注释与实现 | 无 | 历史订正行 |
| 179 | `Battle/BattleDriver.cs:6373-6381` | D | 原版 `m_RaycastTarget=1` + `m_RaycastPadding=(−8,−8,−8,−8)` ⇒ 命中 80.443×77.846 | 已按原版矩形 | 无 | 「按文字判」只在美术缺图时走，产品路径不触发 |
| 180 | `Battle/BattleDriver.cs:9099-9103` | A | 官方本地化 `Battle/Mulligan/WaitEnemy` = `Waiting for enemy` / `等待敌人`（`官方本地化_en+zh-CN.json`，**无省略号**）；原版 TMP 直绑 `mTerm` | `Loc.T("Battle/Mulligan/WaitEnemy") + "…"` | 删掉 `+ "…"`（`:9103`） | 原版串里没有省略号，那个 `…` 是自加 |
| 181 | `Battle/BattleDriver.cs:11196-11206` | D | 原版面板按卡取（`GetChoiceOptions` 那一侧） | 优先按实例 `_pendingInst` 取，取不到才回落下标 | 无 | 回落只在实例缺失时，正常路径与原版同源（且 `B101_E2` 已按原版修候选池） |
| 182 | `Battle/BattleDriver.cs:11244` | D | `battle.gd` 的同形做法 | 注释：候选卡名即它要做的事 | 无 | 散文/对照说明，不是值 |
| 183 | `Battle/BattleDriver.cs:16496-16501`、`:16522` | A | 原版 `BattleTipController.ShowHeadsUpMessage`（`decomp_full/BattleTipController__ShowHeadsUpMessage.c`）只有：比队尾去重、上限取 `GlobalVars+200`、到点出队；**没有「撤下」路径** | 空串 = `ClearTips()` **立刻撤下**（4 个产品调用点在用） | 4 个调用点改为不提示（或交给 1.7 s 自然到点），删空串语义 | 原版没有取消路径，这是我们加的行为 |
| 184 | `Battle/BattleDriver.cs:17658` | A | 原版「谁说的」由卡片的 `+0x40`（isPlayer）定（`VoiceLinesController__DisplayChatBox.c:38`） | `unitType` 认不出时按动作档**兜底猜一侧** | 删兜底：取不到卡就出声、不猜侧 | 原版没有「猜一侧」这条 |
| 185 | `Battle/BattleDriver.cs:27798-27800` | A | 原版该档用的是另一颗节点 `UnitNameNoDescW`；本仓已有原版常量 `Core/CardView.cs:5292` = **1.6000** | 那档走 `DescBoxSize` 并标「近似」 | 改用 `CardView.UnitNameNoDescW` / `UiNameBoxWidth` 那条 | 判据就在本仓常量里，不必近似（影响面 = 探针/自检取框口） |
| 186 | `Battle/BattleDriver.cs:32859-32866` | D | offset 原版就是 UI 空间 px；108 px/单位是设计桥（精确） | `TipPx = 108f`，注释写「这一步是近似」 | 只改注释措辞 | 换算是精确口径，不是近似 |
| 187 | `Battle/BattleEventSystem.cs:314-319` | D | 原版跑 Play 模式，`EventSystem.m_EventSystems` 自有登记 | 编辑模式反射兜底（只在 OnEnable 没跑/没登记时动） | 无 | 产品路径不触发，只为批处理自检 |
| 188 | `Battle/BattleLogPanel.cs:132-145` | D | 原版 `CemeteryActions` = 748.006×431.344 · spacing 0 · UpperCenter · **10 个 `CemeterySliderUI`**（`MonoBehaviour_4589.json`） | `RowCount = 10` | 无 | 「硬编码 10」就是原版值 |
| 189 | `Battle/BattleLogPanel.cs:642-646` | D | Overlay 档相机本来为 null；Camera 档才需要 `worldCamera` | 同 | 无 | 原版等价支 |
| 190 | `Battle/CardDisplayWindow.cs:178-180` | C | 原版卡格前后由 `CardFan` 队列/兄弟序定（`ZSlotStep` 只是数值） | `ZSlotStep = 0.02f` 仅兜底 | 无（要动的是世界空间卡叠本身） | 属「卡体没迁」那一块架构 |
| 191 | `Battle/ChatPopupPanel.cs:130-132` | D | 原版 `White Square` 的 `m_Color` = **(0.001554, 0.066038, 0, 1)** | 同值 | 无 | 精确值已抄 |
| 192 | `Battle/ChatPopupPanel.cs:177-183` | D | I2 六条官方词条 2026-10-14 已解开并存进 `Loc` | 英文枚举名只在缺键时兜底 | 无 | 产品路径走官方词条 |
| 193 | `Battle/ChoosePanel.cs:199-209` | D | 原版 `ChooseCardMenu.GetTittleText` 的回落链 = 带后缀 → 无后缀 → 调用方 fallback（`dump.cs:37575`） | 同链 | 无 | 后两级回落是**原版自己的** |
| 194 | `Battle/EnvironmentApplier.cs:254-257` | A | 原版靠每个 blendable 的 `OnComplete` 计数归零才 `Destroy` | 额外一道 `blendTime + 1s` 兜底超时 | 删兜底：只按 `pending == 0` 收；若怕永不回调改成**只出声、不提前 Finish** | 原版无此分支 |
| 195 | `Battle/Label.cs:1287-1293` | D | — | 入参非法时**出声**（原来静默 return） | 无 | 诊断出声，不是自设计值 |
| 196 | `Battle/Label.cs:1325-1329` | D | 同上 | 出声（该格够不到） | 无 | 同上 |
| 197 | `Battle/Label.cs:1337-1339` | D | 同上 | 出声（该格够不到） | 无 | 同上 |
| 198 | `Battle/MulliganPanel.cs:412-422` | D | 原版 `m_SizeDelta.y = 63.84000015258789`（`RectTransform_2659.json`） | `BarH = 63.84f` | 无 | 2026-10-19 `A1301③` 已逐位对齐 |
| 199 | `Battle/MultiCardDisplay.cs:175-181` | D | 原版绝对框 x[1322.7,1900.2] y[945.2,1009] | `BarCx/BarCy` 取原版值（现只兜底支用） | 无 | 值是原版；产品路径走 `BtnBarCx` |
| 200 | `Battle/MultiCardDisplay.cs:240-241` | D | 原版字宽实测（`Label.WorldW`） | `BtnTextCxFallback` 只兜底 | 无 | 2026-10-24 `W64·B-24` 已改实测 |
| 201 | `Battle/MultiCardDisplay.cs:244-247` | D | 同上 | 兜底支 | 无 | 同上 |
| 202 | `Battle/MultiCardDisplay.cs:246-248` | D | 同上（「−60」半字宽近似已被实测取代） | 兜底值 | 无 | 历史注记 |
| 203 | `Battle/PopNumber.cs:203-209` | D | 原版字面量 `0.847 / 0.700` | `EffectiveScaleRatio = DamageEmLocal / HealEmLocal` | 无 | 由原版字面量算，不是写死 |
| 204 | `Battle/ReplayBar.cs:101-104` | D | 原版 `m_AnchoredPosition` 逐值（`Replay −86.0` / `Play −6.9487…` / `StepPlay +72.2512…`） | `SlotApX` 取原版序列化值 | 无 | 明确写「这里取原版序列化值」 |
| 205 | `Battle/SettingsPanel.cs:520-533` | D | 官方表有 `Settings/Graphics/AutoZoom`（`Auto zoom` / `自动缩放`） | 已接键，英文串只兜底 | 无 | 2026-10-14 已接 |
| 248 | `Battle/BattleDriver.cs:26195-26198` | D | 原版 `DORotate(Vector3.up, …)` 当欧拉角 ⇒ 绕 Y **1°** | `new Vector3(0, 1, 0)` | 无 | 注释即「照抄、别当 90°」 |
| 249 | `Battle/CombatAutoZoom.cs:708-717` | C | 原版一局一份场景 ⇒ 没有「换相机就重抓 `originalSensorSize`」这一格 | 额外一条重抓 | 要删需先让相机不跨局复用（动场景/相机生命周期） | 架构（我们的相机生命周期与原版不同） |
| 250 | `Battle/SettingsPanel.cs:462-468` | D | 原版 skip 钮父链 = `SkipTutorial Button → Bottom buttons → BattleSettingsPanel`，面板内 (171.7, −310.5) | 已搬进 `SettingsPanel`、坐标取原版 | 无 | 「摆位我们挑的」是落点错，已改正 |
| 251 | `Battle/TutorialOverlay.cs:263-271` | D | 原版 `TutorialArrows/arrow1·arrow2` 的 `SpriteRenderer.m_Sprite` = `{0,0}`（本地 + 安卓 1.35.0 双证） | 箭头整层已删；两个读口恒 `false` + 出声 | 无 | 2026-10-31 `B105_W1` 已按判据删 |

## 判定方法备注

- **行号**：census 生成于 2026-10-25，本轮按**现读代码**重定位；同一常量若在两处被 census 各记一条（#3/#10），已合并说明。
- **「兜底」不等于自设计**：清单里 12 条「兜底」中 **10 条**是原版行为（`#188`/`#189`/`#193`/`#199`/`#201`/`#202`…）；
  真正自造且**可达**的只有 `#194`（超时）与 `#183`（撤下语义），另有 `#11`/`#196`/`#197` 是不可达防御。
- **`#4` 类（远端）只有 2 条**：判 B 必须能指出「本地 0 命中 + 代码里没有默认值 + 唯一来源是 titleData/服务端字段」三条同时成立，否则一律 A。
- ⛔ 未判 B 的那些「查不到」（`#1` 的静态向量、`#2` 的绝对位、`#3` 的 `WaitForSeconds` 实参）**都属「还没查清」** —— 按用户口径它们是**待办**，不是例外。
