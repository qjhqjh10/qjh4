# AUD3 · 战斗线 §一逐条现核（2026-10-25 · 只读核查代理）

> 口径：**代码里有实现 = 做了**；台账自陈不算。工作树 = HEAD `17cd58fe` ＋未提交改动。
> 范围 = `资料/任务线/战斗线/任务总表.md` **§一 第 11–331 行**里所有**表格行**（`✅` 叙述段不核）。
> ⛔ 未跑 Unity / 未跑 typecheck / 未动 git · 已核过 5 条（`M1` / `W62 D5·D6` / `D3` / `RE1·回放出口` / `R8-b`，见 AUD2）不重复。
> 读数：`BattleScene` 5345/0 · `AnimFXCheck` 209/0 · `RuleEngineTest` 8823/8823（今日实跑）。

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据 | 建议处置 |
|---|---|---|---|---|
| 1 | `:246` W62·真机（3 条） | **已做完（只欠真机）** | D5/D6/D1 已由 AUD2 现核落盘；本行「修复中」措辞过期 | 状态列改 `◐ → ✓ 已修待验收（只欠真机眼看）` |
| 2 | `:247` T02 棋盘几何与落点 | **已做完（只欠真机）** | `Board/BoardLayout.cs` `ProjectionScale` 在；`BattleScene` 5345/0 | 真机暂停，保持 |
| 3 | `:248` T03 命中与可点区域 | **已做完（只欠真机）** | `A2092` 命中口在 `Battle/BattleDriver.cs`·`Battle/CardDisplayWindow.cs` | 同上 |
| 4 | `:249` T04 卡面放大窗残余 | **已做完（只欠真机）** | `CardDisplayWindow.SetFrontVisible` 在位 | 同上 |
| 5 | `:250` T05 逐卡演出接线 | **已做完（只欠真机）** | `OnDeployHandoff` 在 `BattleDriver.cs`·`Board/DeploySequence.cs` | 同上 |
| 6 | `:251` T06 战斗音响 | **已做完（只欠真机听感）** | `BattleDriver.EnsureAudioListener()` `:4658`，`Begin` 调用 `:4740` | 「还差什么」只留真机听感 |
| 7 | `:252` T07 结算与胜负门 | **已做完（只欠真机）** | — 同上族 | 保持 |
| 8 | `:253` T08 每帧开销 | **已做完 → 该核销主件**；A4 尾巴另立 | 台账「只剩跑 `BattleScene.Run`」**已付**（5345/0）；A4「指针不变⇒0」未做（台账已如实写） | 主件搬 §二；A4 拆成单独一行 |
| 9 | `:254` T09 整层换 uGUI（余 A4） | **未完成** | `PollInputEdges` / `_downEdge` / `_upEdge` 仍在（`BattleDriver.cs:13104`·`:13315`） | 按台账：单独一波 + 真机 |
| 10 | `:255` T10 换牌阶段 | **已做完（只欠真机）** | — | 保持 |
| 11 | `:256` T11 教程接线 | **已做完（只欠真机）** | — | 保持 |
| 12 | `:257` T12 演出镜头时序 | **已做完；余「13 场逐场光晕」未跑** | `Battle/CameraIntro.cs`（光晕逐帧相乘实现）在 | 光晕 = 逐场跑/真机，记账即 |
| 13 | `:258` T14 零星真缺/真错 | **做了一半** | `A1316` 只留注释（`CardDisplayWindow.cs:860`）· `A1983` 已闭（`RuleCore.cs:7287`）· **`A1750` 全仓 `.cs` 0 命中 ⇒ 未做** | `A1750` 单独一行（需真跑） |
| 14 | `:259` T21 逐卡专属动画触发面 | **做了一半** | 目标侧+格号通道**已落**：`RuleCore.cs:10669-10673` Emit 带 `targetPlayer/targetSlot/targetCardId` ＋ `E8-1` 四档侧解；**4 张手牌陷阱的手牌锚未见专门落点**（`Battle/AnimAnchors.cs` 有 18/19/150/155 全套锚） | 余项改成「4 张手牌陷阱的发射层+结算层」 |
| 15 | `:260` 场上词条 | **已做完（只欠真机）** | — | 保持 |
| 16 | `:261` T09-残（A4/C/D） | **未完成**（同 T09；且 S2 施工图仍未补写） | `BattleLogPanel.cs:550` 注释仍写「驱动手搓链退场后…」 | 与 `:254` 合成一波 |
| 17 | `:262` T09-新账 | **大部分已落，余真机 + A4 相关断言** | `Log RowClicked` 已接线 `BattleDriver.cs:27160` · `EndPanel.DimColor` = 原版 `(0.0283,0.0283,0.0283,0.753)` `EndPanel.cs:115` · MultiCard 横向滚动 B-25 已落 | 余项逐条留（真机） |
| 18 | `:263` 新账-58（24 条 Tips） | **已做完 → 该核销** | `ShowMuteEnemyChatTip()` `BattleDriver.cs:6915` + 调用点 `:6905` + `BattleChatAssertE7` C 节 | 搬 §二 |
| 19 | `:265` 新账-65（登场/攻击/受伤） | **已做完 → 该核销** | ③ `BulletImpactBurst_crowd` 已删（`VfxMap.cs:352` + `IsOriginalNoGenericEvent`）· ④ `startPosOption/endPosOption` 已消费（`BattleDriver.cs:17978`）· N5 已修（AU1）· N7 已进 `EventTiming.cs:1039/1073` + 断言 `BattleScene.cs:2436` | 搬 §二（只留真机观感） |
| 20 | `:266` W63·场上卡牌布置 | **已做完（只欠真机）** | — | 保持 |
| 21 | `:267` W63·层级审查 | **已做完（只欠真机）** | — | 保持 |
| 22 | `:268` W63·战斗完整审查（余 2 条） | **② 已结案；① 判据已澄清** | `普查产出_1024/C2_战斗线判据总表.md` §3·1/§3·2（② 与我们逐条一致；① 拖拽不点亮=一致，真差异是「出牌后选目标态」我们没建模） | 「余 2 条」改成一条新账：**指向性战术卡「先出牌再选目标」态** |
| 23 | `:269` W63·审查-结束回合UI | **做了一半**（CreateInOutAnim = 判据空/例外①；纯白+SpriteSwap 已落） | `CreateInOutAnim.c:22-24` 入参 null 即 return；13 场字段全 0 | 只留真机观感 |
| 24 | `:270` 动画与特效全面审查（仍开 D8） | **D8 已闭合 ⇒ 差异清零** | `OnCollectSpiritStone(610)` 已放行 + 发射口：`VfxMap.cs:2460`·`:3206`（`MustRouteTriggerNames`） | 可搬 §二（留真机） |
| 25 | `:271` 新账-71 近战观感 | **已做完（只欠肉眼）** | `CardFeel.MeleeAttack` 按深度重算缩放 | 保持 |
| 26 | `:272` W64·兜底审计 | **做了一半** | 余 = `B-23`（判据空，需真机 RGBA）+ 真机项；`D3` 已落（AUD2） | 保持（余项已收敛） |
| 27 | `:273` 拆账·outlineGlow | **已做完（只欠真机并排）** | 专用层删净 + 7 条断言（`CardBaseDemo` 433/0） | 保持 |
| 28 | `:274` 拆账·MenuScroll 半格门槛 | **已做完（只欠真机手感）** | 上下游两处 `==0f` + 4 条断言；`CollectionScene` 2223/2223 | 保持 |
| 29 | `:276` 新账-82 深度缩放三条尾巴 | **已做完 → 该核销** | ② 已改走 `CardView.TweenPose`（`Hand/CardInteraction.cs:620` 注释 + 代码）；①已做 ③⑤判据空 ④已修 | 搬 §二 |
| 30 | `:278` C3 批次总行 | 见 `:279–:284` | — | — |
| 31 | `:279` D1（6 个 trigger 名） | **已做完（只欠真机）** | `VfxMap.cs` 各 `case` + 两条自检 | 保持 |
| 32 | `:281` D4 `trigger` 桶 Start(0)/Trigger(3) 混装 | **做了一半（台账「只有注释」是过期读数）** | 已加名字闸 `TriggerBeatRowsMatching`（`VfxMap.cs:2843`）+ R8-b 全滤 trait 行 ⇒ 错件/多播已挡；**余 = 属于本 trigger 的 trait 件（`MobEffect`/`ArmourEffect`）仍不播**，缺 `keyword/traitTriggerType` 运行期列（= `P7`） | 状态改 `◐ 已修一半`，余项指向 P7 |
| 33 | `:282` D5 | 📌 不是任务 | — | 无需处置 |
| 34 | `:284` D8 `ASH80` 610 双重不可达 | **已做完 → 该核销** | 610 已放行（`VfxMap.cs:2460`/`:3206`）；`ASH80` 的 `trigger` 桶含 `WaystoneTrigger`（`Eldar_TriggerSpiritstone_Unit`）⇒ 第二口存在 | 搬 §二 |
| 35 | `:285` C4 批次总行 | 见 `:286–:290` | — | — |
| 36 | `:286` D6(C4) Cruelty/Stimulation/Ecstasy | **已做完** | `VfxMap.cs` 三个小写 `case` + 正向锁/反向闸 | 保持 |
| 37 | `:287` D7(C4) HuntMark/Faith 映射 | **已做完** | `EffectResolver` 两个 `case` + `NoEmitTriggerNames` 12→10 | 保持 |
| 38 | `:288` D8(C4) `ThisUnitSummoned` | **已做完** | `VfxMap` `case "thisunitsummoned"`；`RuleEngineTest` 8823/8823 | 保持 |
| 39 | `:289` D9(C4) `WhileInPlay` | **已做完** | `VfxMap` `case "whileinplay"` + `RuleCore.EmitWhileInPlayPass` | 保持 |
| 40 | `:290` D10(C4) `EC57` 登场语音 | **已做完（只欠真机听）** | `voice_lines.json` EC57 `deploy` 行 | 保持 |
| 41 | `:291` `CardInteraction:880` 松手回弹 | **已做完（断言未补）** | 自造 0.22 s 序列已删 | 只留「断言未补」 |
| 42 | `:292` 手牌交互两处口径差 | **已做完 → 该核销** | ① `DOLocalRotate` ② 动画闸；**`HandPoseTween` 第二份已删**（`Hand/HandLayout.cs:879`） | 搬 §二（余 = hover/show 碰撞体闸：待真机+拍板） |
| 43 | `:293` C5 批次总行 | 见 `:294–:297` | — | — |
| 44 | `:294–:297` D11 / D12 / D13 / D14 | **全已做完** | `VfxMap` `regiment`/`triggeredsynapse` case 在；`trait_onplay` 桶现读 **137 卡 / 142 行**（与台账一致） | 保持 |
| 45 | `:298` `:299` 两条订正 | 📌 不是任务 | — | 无需处置 |
| 46 | `:300` `SW10` D8 真实形状 | **已做完** | `TriggerNameOfWhenKind(+selfOnly)` 分流 195/190 + 断言 | 保持 |
| 47 | `:301` `mulliganEnabled` 进录像头 | **已做完** | `ReplayStore.cs:226-231` + `BattleDriver.cs:633/788/986` | 保持 |
| 48 | `:302` `B-30` 多卡窗接线 | **已做完（只欠真机窗）** | `BattleDriver.OpenShowCardsWindow` `:10805-10873` | 保持 |
| 49 | `:308` 对手聊天整条链（联机侧） | **已做完 → 该核销** | `Net/NetChat.cs`（新）· `NetBattle.OnLocalChat` `:1053` · `Dispatch case Chat` `:1289` · `NetBattleTest` §4c 17 条 | 搬 §二（余 = 两台机器真连） |
| 50 | `:309` `AttackGiantKiller` 建模 | **已做完** | `RuleEngineTest` 8823/8823；池内 0 张 | 保持 |
| 51 | `:318` RE1·录像头结构修 | **已做完 → 该核销** | 台账「只剩跑 `BattleScene.Run`」**已付**（5345/0） | 搬 §二 |
| 52 | `:320` RE1·列表与文件 | **做了一半** | **① 列表读盘已做**：`Shell/BattleLogData.cs:341-380`（`DisplayRows`/`FallbackRows` + 按页缓存）；**② `finalBreakdown` 全仓 `.cs` 0 命中 = 未做**；③ `R6` 出处仍缺 | ① 核销；只留 ②③ |
| 53 | `:321` RE1·无目标战术卡中央展示 | **已做完（只欠真机）** | — | 保持 |
| 54 | `:322` C6·Q1 根特效层/相机 | **已做完 → 该核销** | `⏳ AnimFXCheck` 已跑 = **209/0**；`CardFeel.RootFxLayerForCard` + `WarpforgeEffectPlayer.Play(layer:)` | 搬 §二（留真机看层） |
| 55 | `:323` C6·Q2 卡根参考系 | **已做完 → 该核销** | `⏳ BattleScene` 已跑 = **5345/0**；`MeleeEndPos(...,tv)` 已接 · `SetRemnantBody` 挂卡底边 | 同上 |
| 56 | `:324` AN1·R3/R12（R5/R8/R11/R8-b 已收） | **R3 未完成 · R12 部分** | **R3**：`Resources/card_anim_order.json` 仍 **62 卡**，而 `ability` 桶 **93 卡** ⇒ 31 卡走单点播（`BattleDriver.cs:18027` `n<=0 ⇒ return false`）。**R12**：时长表 93 卡全覆盖；原 7 卡里 `SOR66/SOR70/SOR34` 已进表，`SW9/ASH74/GOF77/SOR23` 仍退 `AbilityFallbackSeconds=1.0`（`EventTiming.cs:974`），其中 `SW9/SOR30/SOR65` = SO 无动画（判据空） | 余项只留 **R3（1 条）** + R12 的 3 张非例外卡 |
| 57 | `:325` U3·T1 攻击选择五处 | **已做完** | `EnsureAttackTypeInvariant` / `PreviewTargets` / `BadgeFilterAssert` 等 | 保持 |
| 58 | `:326` U3·T1 替代能力周边 | **未完成** | `DutyUsed` 仍硬编码（`RuleCore.cs:11000` 判、`:11065` 置）· `agenda` **无 signal 建模**（只有名字映射 `:11217`）· `oath` 已建模（`CanUseOathAbility`/`OathOps`） | 余项：Duty 改数据驱动 + `agenda` signal |
| 59 | `:327` U3·T2 场上词条多画 | **已做完** | `InPlayIconKeys` 47 条白名单 + `BadgeFilterAssert` 15 条 | 保持 |
| 60 | `:329` 换牌「正修正额外牌」摆位档 | **判据被推翻 + 字段已建模 → 该核销** | 原版 `MoveCardToMulliganPos(hand,card,i,count)` → `CardsHorizontalLayout.GetPosition(i,count)`，**摆位只吃 i/count**；20/10/0 是 `AddCardsToMulligan…:104-117` 写进 `SetupCardInMulligan` 的 `mulliganOption`（消费点只 `==30`）。字段已建模：`CardInstance.MulliganOption`（`RuleCore.cs:371-395` 逐张打档） | 搬 §二，并把「只影响换牌摆位」改成「档值不是坐标、无摆位消费者」 |

## 应核销清单（已做完但台账还挂着）

| 行号 | 条目 | 一句话 |
|---|---|---|
| `:253` | T08 主件 | 「只剩跑 `BattleScene.Run`」已付（5345/0）；A4 尾巴拆出去 |
| `:263` | 新账-58 | `MuteEnemyChat` SmallTip 已实现 + 断言 |
| `:265` | 新账-65 | ③④ 已落、N5/N7 已闭 |
| `:270` | 动画与特效全面审查 | 唯一余项 D8 已随 `:284` 闭合 |
| `:276` | 新账-82 | ② 已改走 `TweenPose`；①③④⑤ 全闭 |
| `:284` | D8 `ASH80` | 610 发射口已补 + 已放行 |
| `:292` | 手牌交互两处 | ①② 已落、`HandPoseTween` 已删 |
| `:308` | 对手聊天（联机侧） | `NetChat.cs` + `NetBattle` 收发 + 17 条断言 |
| `:318` | RE1·录像头结构修 | 只剩跑自检，已 5345/0 |
| `:320` | RE1·列表与文件 ① | `BattleLogData` 列表读盘已做 |
| `:322` `:323` | C6·Q1 / Q2 | 两处 `⏳` 的宿主都已实跑绿 |
| `:329` | 换牌摆位档 | 判据驳回（档值不是坐标）+ 字段已建模 |

## 不能判的

1. **所有「只欠真机」的观感正确性**（约 15 行）：用户 2026-10-25 已明确真机暂停，无实拍 ⇒ 判不了「看到的效果对不对」。
2. **T14·`A1750` 12.5 s 冻帧**：全仓 `.cs` 0 命中，但「该不该有代码」取决于实跑复现，静态判不了。
3. **T21 的 4 张手牌陷阱**：未逐张读那 4 张的卡 SO（`sp=0 self` 在手里时的锚点数据），只核到 `AnimAnchors` 有 18/19/150/155 全套手牌锚。
4. **R12 的 `SW9/SOR30/SOR65`**：SO 的 `activeAbility` 一条非空 CardAnim 都没有 ⇒ 原版「等 0 还是等一拍」判据不足（台账口径，未独立复核 SO 字节）。
5. **U3 替代周边的 `agenda`/`oath` signal**：只核了我们侧实现（`agenda` 无 signal、`oath` 有 `OathOps`），未读反编译确认原版 signal 的具体形状。
6. **读数与 HEAD 的绑定**：`battlescene8.log`（07:42）与 `RuleEngineTest`/`AnimFXCheck` 读数取自今日实跑，但日志无 commit 指纹（同 AUD2 的疑点）。
