# AUD3 · 引擎线 §一（13–193 行）逐条现核（2026-10-25 · 只读）

> 口径：**代码里有实现 = 做了**；台账自陈不算。⛔ 未跑 Unity / typecheck / git 写操作。
> 证据路径相对 `D:/4/Unity/MyGame/Assets/`（`.cs`）或 `D:/4/Unity/`（`资料/`、`工具/`）。

| # | 台账条目（行号 + 一句话） | 现核结论 | 证据（file:line / 报告） | 建议处置 |
|---|---|---|---|---|
| 1 | `:159` **T02** 余项 `SW18` 变身分配口径（判据未定） | **已做完（该核销）** — 变身路已按「逐张各掷一次」实现（先掷后判同名），并有用例 | `RuleEngine/Core/EffectResolver.cs:3062-3100`（`:3074` 掷骰点）· `Editor/RuleEngineTest_EngineB3.cs` ③（`0x41a` 同批） | 核销（若「分配口径」另指别的，未见单独判据，请主对话裁） |
| 2 | `:159` T02「还差什么」列的 `A1412`/同族 `or`/`Sacrifice:`/落位锚点 | **已做完** — 37/38 已建模、`Sacrifice:` 全池 0 张、落位收口 `BoardSlots.NextFreeWithoutDisplacing` | `任务/T02_…md` 行内 · 本表 `:159` 自述 | 核销 |
| 3 | `:160` **T03** 余项 `A1670` 是否已收 | **未完成（判据未定）** — 任务文件明写「没验 / 先判可达性」 | `资料/任务线/引擎线/任务/T03_事件层与时机.md:23,57,72` | 派查判据（教程局可达性） |
| 4 | `:160` T03 余项「电池 3 张真机探针」 | **未完成（真机项）** — 代码侧无对应夹具，属真机 | 同上 `:72` | 留真机清单（现暂停） |
| 5 | `:160` T03「还差什么」列仍写 `P3-C` 半闭合（联机/回放半） | **台账自相矛盾** — 状态列写「⑤ `P3-C` 全闭（C2 D-06）」 | 同格两处对读 | 订正「还差什么」列 |
| 6 | `:161` **T03·b** 余项：换牌「正修正额外牌」摆位档 20/10/0 | **已做完（该核销）** — `MulliganOption` 按区间打档并已接换牌/`SimpleAI` | `RuleEngine/Core/RuleCore.cs:392` · `Core/CardInstance.cs:91-101` · `Data/SimpleAI.cs:1704` | 核销 |
| 7 | `:162` **T03·c** 四跳接线 + `D10`/`D11` 两条口径 | **已做完（该核销）** — `D11` 结算序已成「棋盘→当前方手牌→常驻」；`D10` 判「现读不可区分、不改」 | `Core/EffectResolver.cs:7935,8070,8093` · `Core/ActionQueue.cs`（四道 FIFO） | 核销 |
| 8 | `:163` **T03·d** 余项（`P2-8` berzerk 选靶 / `P3-C` / `GetsShield↔510`） | **已做完（该核销）** — `P2-8` 由 N1 补候选口径；`P3-C` 由 C2 D-06 闭合；`GetsShield` 判「映射不成立 ⇒ 不改」 | 本表 `:163` 自述 + `普查产出_1024c/A2_…` D-05/D-06 | 核销 |
| 9 | `:164` **codex 能力触发** — 只欠真机看 Codex 那一拍 | **做了一半** — 代码已做（`case "Codex"` 放行），`AnimFXCheck` 209/0；真机项未验 | `CardPresentation/Core/VfxMap.cs`（`case "Codex"`）· `资料/交接_第七十五会话.md:25` | 核销代码侧，留真机 |
| 10 | `:165` **T06** 余项 play 基线归因 / `PinnedPlayMd5` 重钉 | **已做完（该核销）** — 基线已重钉 `fc591fb3…`、`play check` rc=0（1127+586 零差异） | `资料/交接_第七十五会话.md:35,37` · `普查产出_1025/E6_十三行接闸.md` §六·b | 核销 |
| 11 | `:166` **T07** 余项 `PinnedPlayMd5` 未重钉 | **已做完（该核销）** — 同上 | 同上 | 核销 |
| 12 | `:168` **play 基线·重钉结论**（`rc=1` / ⛔ 不重钉） | **已做完（该核销）** — 已重钉、现 rc=0 | 同上 | 整行搬 §二 |
| 13 | `:170` **新账-88** 回放终局指纹 ≠ 录制 | **未完成** — 判别器已建并接线，但真机观测到的分叉点仍未定位 | `Core/ReplayFingerprintTrace.cs`（`FirstDivergentIndex`）· 本表 `:80` 读数 −1912646073 ≠ −1762953645 | 派只读诊断（逐动作 `StateHash`） |
| 14 | `:171` **新账-89** 三条次要 + 覆盖盲区 | **已做完（该核销）** — ①`HandMaxFor` 已接 ③`ResetTransientForNewBattle` 已清；② 由 D-01 判「原版无 `>0` 闸 ⇒ 不改」 | `RuleEngine/Data/AiScoring.cs:262,375` · `Core/EffectResolver.cs:11250` | 核销 |
| 15 | `:173` **新账-95** 数据侧死列与口径（8 个 role 未接） | **已做完（该核销）** — `mult/multi/avail/choice/nontarget/targetscore` 全处置，数据键已在 `CardDatabase` 落 | `RuleEngine/Data/CardDatabase.cs:2385-2388` · `Core/EffectResolver.cs:12618-12675` | 核销 |
| 16 | `:175` **新账-97** 余「判据不足三条」 | **做了一半** — `terror` 的 `currentCombatAttack` **已读**（本条可销）；`TargetsAffected` 23 档仍只部分（`ta==100` 明写未实现） | `Core/CounterAttack.cs:73,87,94-101` · `Core/TargetCriteriaEval.cs:22-57`（`:57` 未实现） | 销 `terror` 一条，留 `TargetsAffected` |
| 17 | `:177` `questTimes` 消费点 | **做了一半（当前不可达）** — 消费点已接；`FireTriggerAlways` 的「无正文」支未过闸；池内 0 张可达 | `Core/Enchantment.cs:420` · `Core/RuleCore.cs:11478`（闸）· `:11781-11802`（无闸支） | 留账，注明「0 张可达」 |
| 18 | `:178` **`BroadcastTurnEnd` 棋盘趟第三支**「只差一条断言」 | **已做完（该核销）** — 三靶用例已写并注册 | `Editor/RuleEngineTest_EngineB3.cs:212-238` · `Editor/RuleEngineTest.cs:970` | 核销 |
| 19 | `:179` **换牌阶段投降的录像终局指纹** `✗ 未做` | **已做完（该核销）** — 专用入口 + 驱动器接线 + 断言齐 | `Core/RuleCore.cs:1565-1587` · `CardPresentation/Battle/BattleDriver.cs:1219-1221` · `Editor/RuleEngineTest_EngineA1.cs:351-358` | 核销 |
| 20 | `:190` **C5·Q1 ③**「`regeneration` + 另 8 个同形状 trait 记进不可达清单」 | **未完成** — 全仓无该清单，只有一处注释点名 | `CardPresentation/Core/VfxMap.cs:2592`（仅注释；`grep 不可达清单` 0 命中） | 派执行（建表 + 断言） |
| 21 | `:191` **C5·Q4 ②**「候选是卡的入口 + 手牌池消费端」 | **未完成** — 求值器只吃 `UnitState`，无卡候选入口 | `Core/TargetCriteriaEval.cs:205-334`（`candidate.Card` 只取属性） | 派执行 |
| 22 | `:191` **C5·Q4 ③**「`CountEval` 与 `TargetCriteriaEval` 收口」 | **未完成** — `CountEval` 仍把四组 stat/若干布尔筛当未实现 | `Core/CountEval.cs:321-333` | 派执行 |
| 23 | `:60,70,78,191` **E2 未接的 63 行**（其中 13 行已由 E6 接上） | **做了一半** — 13/13 已接；余 48 行（J5：`trigger 0`×40 · `410`×5 · `580`×2 · `620`×1） | `普查产出_1025/E6_十三行接闸.md` §〇 · `资料/交接_第七十五会话.md:61` · `Core/ActionPump.cs:313-330`（闸只接 `Attack`/`Die`） | 先裁歧义 32 行/23 张，再批量接 |
| 24 | `:35④` **`ruleprobe` 的 `scan`/`check` 不覆盖 `ext=` 块** | **未完成** — `ExtraFields` 只挂在 `card` 那条 dump 上，`scan` 自述不打 | `工具/ruleprobe/Program.cs:1466-1468`（唯一调用点）· `:1488-1492`（自述 `scan` 不打本块） | 派工具线 |
| 25 | `:34` **B6 夹具（自检线 T03）未做** | **已做完** — 已改走生产同口（R3 三步：候选来自生产口 / 前提成立 / 逐卡桶判别式） | `CardPresentation/Editor/BattleScene.cs:40239-40320` · `普查产出_1025/R_…md` §R3 | 核销 |
| 26 | `:49,55` ability 级 criteria 数据管线（v26 补列） | **已做完** — v26/v27/v28 三列已落并接最小消费点 | `普查产出_1025/E2/E3/E4_…md` · 本表 `:55,63,67` | 核销（余口见 #21/#23） |
| 27 | `:42,76` **`referencedCards` 列**（「48 键里没有 ⇒ 数据管线」） | **不是待办（口径错）** — 现读 43 键；它是运行期字段、加列无人写 | `普查产出_1025/AUD2_台账矛盾核对.md` §7 · 本表 `:76`（E5-4） | 按 `:76` 口径改写 `:42`（⛔ 别列成待做） |
| 28 | `:70` **E5-2 四道现池 0 张的闸**（ward/wardWb/summonRift/tainted） | **已做完（该核销）** — 四闸已建 + 断言 | `Core/CardDef.cs:7512-7537`（登记）· `Core/RuleCore.cs:10416+`（`CanUseAbilityTarget`）· `普查产出_1025/E7_四道闸.md` | 核销 |
| 29 | `:102` **24 行 stat 过滤今天大概率不可达** | **未完成** — 求值器已实现但无「候选是卡」入口（= 与 #21 同根） | `Core/TargetCriteriaEval.cs`（只吃 `UnitState`） | 与 #21 合并派 |
| 30 | `:103` **`excludeTargetted(+0x20)` 一次都不读** | **未完成** — 字段有定义、无消费点 | `Core/CardDef.cs:2233-2234`（仅定义/可判性检查） | 派执行（判据在同批 J5） |
| 31 | `:128,135④` 触发层 `PerCount` 增量 | **已做完** — 抽 `IsPerCountIncrement`/`TryFoldPerCount` | 本表 `:106` · `普查产出_1024c/E1_PerCount与desc闸.md` | 核销 |
| 32 | `:129,135` **`ta=200/201` 的载体装不下手牌卡** | **未完成，且台账两处相反** — `410` 只有注释、无入队点；`:129` 判「要做」、`:76` 判「不是待办」 | `Core/EffectResolver.cs:2459,1219-1244`（`SetReferenceCards` 无 410 调用） | 主对话先裁口径，再决定立账/关账 |
| 33 | `:130` **`AM22` 的 `chooseone` 要拆** | **未完成** — 仍列 `gap:kind-ambiguous` | `Editor/RuleEngineTest_G12.cs:428-435` | 待判据（与 48 行同批） |
| 34 | `:131` **`SOR13` 二级匹配可做** | **未完成** — 同上（`gap:kind-ambiguous`） | 同上 | 待判据 |
| 35 | `:135` 裸 `Deal` 无人选时原版空表 / 我们落池序前 N | **未完成（明写近似）** — 仍在，已如实记录 | `Core/EffectText.cs:6844-6852` | 要做（引入「问玩家」通道或按原版空表） |
| 36 | `交接§二` **`FollowUpSchedulerOf` 漏 6 张池内卡** | **未完成** — 硬编码表里无 `EC40/EC42/GOF31/GOF93/TL3/UM64` | `Core/EffectResolver.cs:7065-7098` · `普查产出_1025/E9_ActionPump三偏差.md` §四（现成补丁） | 直接照补丁做 |
| 37 | `交接§二` **`ward` 的另一半**（战术卡/通用目标） | **未完成** — 代码自述「那一半还没接」 | `Core/CardDef.cs:7545-7549` | 派执行（池内 0 张，仍要做） |
| 38 | `交接§二` **`activeAbility` 友方目标（驱动/AI 侧）** | **做了一半** — 引擎侧已通（`FriendlyUnit` + `CanUseAbility(targetOwner)`）；驱动层仍退回 `1-_me`，AI 只扫 `foeBoard` | `Core/RuleCore.cs:10211-10228,12484` · `CardPresentation/Battle/BattleDriver.cs:14084-14105` · `RuleEngine/Data/SimpleAI.cs:425-431` | 派执行（先表现层、再 AI） |
| 39 | `交接§二` **`EmitListenerTrigger` 另三处未传 gates** | **未完成** — 只有 `FireBoardWhenListener` 一路传 | `Core/EffectResolver.cs:9504`（已传）· `:9719,9812,9890,10010`（未传） | 留账（今天无偏差） |
| 40 | `:65` **`DA22 Techmarine` 歧义** | **未完成（判据不足）** — 仍「歧义 ⇒ 不触发 + 出声」 | 本表 `:65` · `RuleEngine/Editor/RuleEngineTest_G12.cs:428` | 待判据（J5/判据代理） |
| 41 | `:87` 4 个新 `.cs` 需 Unity 生成 `.meta` | **已做完** — `.meta` 全在 | `Core/TargetCriteriaEval.cs.meta` · `Core/ActionQueue.cs.meta` · `Core/ReplayFingerprintTrace.cs.meta` | 无 |
| 42 | `:45①,45③,50,35⑤` A2/A3 顺手发现（ctor 陷阱 / at-turn 12 张 / acting+cardtarget / warning 文案） | **已做完** — `A3-3` 出声治陷阱 · at-turn `deal/give` 6 张贴+5 张写清理由 · `acting`→v27 · `cardtarget`→A1 | 本表 `:39,48,51,63` · `普查产出_1025/A3_…md` | 核销 |

## 应核销清单（台账写着没做/半做，代码里已完成）

1. `:159` **T02**（余 `SW18`）· 2. `:159` T02 还差什么列同族项 · 3. `:161` **T03·b** · 4. `:162` **T03·c** · 5. `:163` **T03·d** · 6. `:165` **T06** · 7. `:166` **T07** · 8. `:168` **play 基线·重钉结论**（整行搬 §二）· 9. `:171` **新账-89** · 10. `:173` **新账-95** · 11. `:178` **BroadcastTurnEnd 第三支** · 12. `:179` **换牌阶段投降录像**（`✗`→`✓`）· 13. `:175` 里 **`terror` 的 `get_currentCombatAttack`** 一条 · 14. `:70` **E5-2 四道 0 张闸** · 15. `:34` **B6 夹具** · 16. `:42` **`referencedCards` 不是待办**（改写口径，不是核销）· 17. `:87` **`.meta`**。

## 不能判的

1. **`A1670`「是否已收」**：任务文件只写「先判可达性、没验」，代码里找不到与之对应的实现也无从反证 ⇒ 判据未定（#3）。
2. **`C5` Q3「建议落点 2」的 doc 订正**（`:86①`）：`W2` 报告 §168 要求订正，但 `普查产出_1024c/C5_引擎战斗剩余判据.md` 里已无「建议落点 2」原句 ⇒ 无法判断是订正过还是本来就指别处。
3. **`GSC75` 的 `repeat`/`lowercost` 两条 op 是否原版口径**：`repeat` 计数已修（`Core/EffectText.cs:5065+`）、`lowercost` 是既有 op（`Core/EffectResolver.cs:1012`），但「这张卡这两条 op 的口径」没有单独判据 ⇒ 判不了。
4. **`#1` T02 的「`SW18` 变身分配口径」是否就是已实现的「逐张各掷一次」**：语义高度重合，但台账没写这一项的判据原文 ⇒ 若另有所指，需要主对话裁。
