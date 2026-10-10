# 断言批 · 规格（C 组）—— 第十三会话 7 份 R 报告 + 1 份 D 报告 抽出的「该补什么断言」

> 2026-10-11 第十四会话。**只读普查**：只写本文件，没跑 Unity、没动 git、没改任何生产代码/文档。
> 用途：调度台照本表即可派活写断言，**不必再回去读那 8 份原报告**。

## 〇、读了哪 8 份 / 各多少行 / 哪几份根本没写断言规格

| 报告 | 行数 | 写了断言规格吗 |
|---|---|---|
| `R1_RuleCore六条现核.md` | 204 | ⚠️ **只 1 条、且内容极薄**（`A1279`「用真卡夹具补断言」，**没写断什么**）；`A1218①` 引的是**已有**断言（`RuleEngineTest.cs:4473/4503/4524`），**不是新增** |
| `R2_A1224现核.md` | 146 | ✅ **写得最全的一份**（`§五·5` 逐条到文案与灭自证） |
| `R3_A1168与A1312查证.md` | 156 | ❌ **根本没写断言规格**（只有判据与改法；两笔都因「今天 0 张可达」收不了口） |
| `R4_A1311与表二四行.md` | 166 | ❌ **没写断言规格**（表一 0 条）；只有 1 处「跑起来看 y 差值」的**可观测点** + 1 处可选旁证 |
| `R5_A1330到A1332现核.md` | 177 | ❌ **没写断言规格**（表一 0 条）；只给了判别式/卡面判据/一条「改完必须复跑」的守卫 |
| `R6_A1344到A1351查证.md` | 168 | ❌ **没写断言规格**（表一 0 条）；给了 3 处可观测点/判据 |
| `R7_A1366到A1369查证.md` | 209 | ✅ **`A1369` 有明确规格**（§③末「该补的断言」+ 反向沙包要求，**但没点名宿主**）；`A1366`/`A1367` 只给判据 |
| `D_A1334诊断.md` | 133 | ✅ 1 条（`§⑤·3` 可选加固，含「⛔ 别拿 X 去断」的灭自证半句） |

合计 **1259 行**。**表一 = 9 条 · 表二 = 17 条**。

---

## 一、表一：明确的断言规格

| # | 断言该断什么（一句话） | 宿主文件 | 被测对象/落点（类.方法 / 文件:行号） | 灭自证那一半（没有就写「报告未给」） | 出处（报告文件:行号） | 前置（要不要夹具/临改某个常量） |
|---|---|---|---|---|---|---|
| 1 | 给 `A1279` 那 21 张 `Slay` 消费者补**真卡夹具**断言（⚠️ 报告只说「用真卡夹具补断言」，**没写断什么**，断文案待调度台定） | `RuleEngine/Editor/RuleEngineTest.cs` | `Slay` 结算尾段 `RuleCore.cs:4020 TryCreditKill` | 报告未给 | `R1:178-181` | 要夹具：可达对 = `EC52`×{`EC19`/`EC12`/`EC33`/`EC38`}、`GOF90`×`GOF96`（`R1:175`）；⚠️ `RuleEngineTest.cs` 里那 8 张「造成伤害并眩晕」**零引用**（`R1:180`） |
| 2 | 夹一把 `Deal 2 damage to an enemy and Stun it` 打**带 `Remnant` 的 2 血单位** ⇒ **残骸被晕上** | `RuleEngine/Editor/RuleEngineTest.cs` | 代词路 `EffectResolver.cs:628`（`them`）/`:632`（`it`）；姊妹口 `RuleCore.cs:2907-2920 StunStillOnBoard` | ✅ 有：**旧 `UnitState` 上没有晕**（挂在旧对象上 = `UnitState._keywords` 各一份 ⇒ 静默丢掉） | `R2:94-98` | 要夹具（临时战术卡）；照 `A1176` 三连形状（`RuleEngineTest.cs:4473`） |
| 3 | 同一张卡打**不带 `Remnant`/`Waystone` 的单位（真死）** ⇒ **敌方场上一个被晕的都没有** | `RuleEngine/Editor/RuleEngineTest.cs` | 同 #2 | ✅ 有：这条本身**就是**挡「顺手改宽」的反向沙包 | `R2:99` | 要夹具 |
| 4 | `Waystone` 换 `Remnant` ⇒ **同结果**（⛔ 只修死灵那半 = 静默偏一半） | `RuleEngine/Editor/RuleEngineTest.cs` | 原版 `HasToTransformIntoRemnant = remnant ∨ waystone` 是**一条**支路（`CardScript__CheckIfDead.c:122-147`） | 报告未给 | `R2:100-101` | 要夹具（`Waystone` 那一档） |
| 5 | 同时挂一个 `When an enemy receives a Stun, gain +1 Attack` 的观察者 ⇒ **广播那半也被钉住** | `RuleEngine/Editor/RuleEngineTest.cs` | `EffectResolver.cs:2188` `BroadcastKeywordEvent(ctx, WhenEventKind.GetsStun, t)` | 报告未给 | `R2:87-90`、`R2:102` | 要夹具；⚠️ 报告写「**必须补一条断言钉它**，否则这半永远无人看管」 |
| 6 | 洞 1（`for each` 重复付费）：付费组**货币恰好扣一次** × 效果**都发生**，且**同时**断「遍数 N」与「`Energy` 只掉一次」 | ⚠️ **报告未点名宿主**（引擎侧 ⇒ 应为 `RuleEngine/Editor/RuleEngineTest.cs`，待调度台定） | `EffectResolver.cs:261-262`（重复循环）· `EffectResolver.cs:293-326`（逐 op 收费）· `EffectText.cs:124` `CostShared` | ✅ 有：「**反向沙包**」= 把修法退回旧写法时**必须变红** | `R7:182-183` | 要夹具；**今天 0 张可达**（`R7:143`）⇒ 夹具得手造 |
| 7 | 洞 2（`, plus` 前半句白给）：同上公式 | 同上 | `EffectText.cs:2089` / `:2123` 两支（`StampPaidCost` 只贴后半批） | ✅ 同上（反向沙包） | `R7:144`、`R7:151-153`、`R7:182-183` | 要夹具；今天 0 张可达（池里带 `, plus` 的 4 张**全都没付费前缀**，`R7:148`） |
| 8 | 洞 3（`instead` 跳过付钱那条）：同上公式 | 同上 | `EffectResolver.cs:155-186`（决定 `skip[]`）· `:163` | ✅ 同上（反向沙包） | `R7:145`、`R7:154-160`、`R7:182-183` | 要夹具；今天 0 张可达（`SOR49` 只 1 条 op、`SOR6` 跳的是**本来就不付钱**那条，`R7:155-159`） |
| 9 | 单位卡 `When <事件>, gain X` 的**执行层** op **必须** `Target.Subjectless == true` | `RuleEngine/Editor/RuleEngineTest.cs` | 判据取 `u.Card.FireWhen(...)` 的返回（或 `WhenTriggers[i].Ops[0].Target`）；现成断言在 `RuleEngineTest.cs:2379-2425` ⑤ 旁边 | ✅ 有：**⛔ 别拿 `EffectText.Parse(整条 desc)` 的结果去断**（那正是今天这条分叉） | `D:89-92` | 可选加固（钉现状、防回归）；⚠️ 动 `§⑤·1` 主修前先看 `RuleEngineTest` 里有没有钉住那 10 张的断言语料（`D:85`） |

---

## 二、表二：可观测点 / 判据（报告里给了「怎么判它成立」，但没写成断言规格）

| # | 判什么 | 落点（文件:行号） | 出处（报告文件:行号） | 备注 |
|---|---|---|---|---|
| 1 | `IsAlive` 一族应改成「`Health > 0 \|\| CurrentSurvivor > 0`」；`WouldKillByEntries` **单独**按 `WithDamageValues` 补 `Survivor`+`Bastion` | `Core/RuleCore.cs:4452`/`:4483` · `:4152` | `R1:43-47` | 三个字段**全仓零实现**；可达性 0（`survivor` 0 张 · `bastion` 0 张）⇒ 今天不可观测 |
| 2 | `Survivor >= 1` ⇒ **消耗 + 留场、不置将死、不进坟场** | `RuleCore.CleanupDeaths`（`RuleCore.cs:4799-4830` 残骸支路**之前**）· `UnitState.Survivor` | `R1:89-92` | 判据 `CardScript__CheckIfDead.c:111-112 / :152-165`；今天可达 = 0 |
| 3 | 攻方 `AboutToAttack`（`0x118` = 280）能力成立 ⇒ **`return RuleCodes.OK` 但不出伤害** | `RuleCore.DeclareAttack`（`RuleCore.cs:3532`，`:3546` 之后 / `:3552` 之前） | `R1:117-122` | ⛔ **不建议只留 `ErrUnimplemented` 空壳**（原版那条路是**真取消**）；⛔ 别新造第二个 trigger id |
| 4 | 毒单位在**它自己那方**回合末被销毁（`deathType = poison(40)`），`resistant` 是豁免 | `RuleCore.EndTurn` 摘闸门**之前**；`KeywordTable` 补 `poisoned`/`resistant` | `R3:41-42`、`R3:48-51` | 判据 `CardScript__OnTurnEnd.c:229-262`；池里 **0 张**带 `poisoned` ⇒ 不可观测，但铁律 11 要做 |
| 5 | 部署入口的 `SummonSickness` 不该**无条件**置真 —— 原版第 5 处 = `BattleAction.keepEffects` | `Core/RuleCore.cs:1929-1935 ApplyDeployTurnState`（`:1932`） | `R3:88-91` | 4 处常量 false 是对的、第 5 处被吃掉；⚠️ `SummonCriteria.keepEffects == true` 的实例**本地判不了** |
| 6 | 跑一次 `MainMenuScene.Run`，看 4 条 `CheckLeftAlignedAtWorld` 打印的 **y 那一半实得差值**：≤ 容差 ⇒ **无第二因**（可销）；超容差 ⇒ 第二因存在（方向 = 字号/行高） | `Editor/MainMenuScene.cs:3034-3037`（×4 处调用） | `R4:44-50` | ⚠️ 第四会话日志已有「y 差 0.00px」但**跑在改断言之前** ⇒ **只能当旁证、⛔ 别拿它销账** |
| 7 | 同一份实例**不会**同时在 `Hand` 与 `Board`（可选运行期旁证；结构性排除已足够） | `EffectResolver.cs:5543 BroadcastHandWhen` · `RuleCore.cs:2031/:2573/:2465` | `R4:93-94` | 「夹具 + 数笔数的口」**不再是判缺陷的必要条件**；要做就落 `RuleEngineTest.Run` |
| 8 | **手写 min/max 并集循环**应收成 `UnionQuadRectPx` —— 判别式 = ①`GetComponentsInChildren<ImageQuad>(searchInactive)` ②激活闸 `None/Self/InHierarchy` ③`MenuDraw.QuadRectPx` ④`MaxValue/MinValue` 初值 ⑤单侧也可 | 参照实现 `Shell/MenuDraw.cs:740`/`:745-768`；**17 处手写点**里 **#1–#10 未被任何账登记** | `R5:8-40` | 「不收」的三处真清单在 `ShellScene.cs:229-235`（`RewardsScene.TmpVertPx` 族 · `MainMenuScene.CountSoftFadedTextVerts` · `IconSizeProbe`/`Round1015Probe`），⚠️ 台账点名点错了 |
| 9 | 那 4 张卡的卡面是 `Oath N: …`（不是裸 `N:`） | `数据/游戏数据/cardface_fixes.json` 的 `desc` 列（4 条） | `R5:70-81` | 卡图逐张亲读：`UM80`→`Oath 4` · `UM81`→`Oath 3` · `UM_Scout_Sniper`→`Oath 1` · `UM74`→`Oath 3` |
| 10 | 改 `Core/CardText.cs:222` 走同一入口后，**必须复跑 `BattleScene.Run`**（现有 4 条断言盯着 `KeywordSegment` 输出） | `Core/CardText.cs:222` · 守卫在 `Editor/BattleScene.cs:822-855` | `R5:149` | 现状 = `CardText` 只走 ③、`Badges` 走 ①②③；今天**逐键等价**（`R5:131-139`）⇒ 是**潜伏的缝** |
| 11 | 4 张卡的 `OathCost` 应为 4/3/1/3（今天 = 0 ⇒ **誓约能力激活不出来**） | `Core/EffectText.cs:2955 ReOathPaid` → `Core/CardDef.cs:985 CollectOathOps` | `R5:100-110` | 与卡面症状**同一根因**；旁证 = `EffectResolver.cs:4728` 注释「`OathCost == 0` 的卡（**理论上没有**）」 |
| 12 | 补 `☀` 后 `CostKindOf("☀")` 应回 **`faith`**、**扣的是 `ps.Faith` 而不是 `ps.Energy`** | `Core/EffectText.cs:6120` · `Core/EffectResolver.cs:296-297`/`:310-312`/`:320-325` | `R6:10-30`、`R6:50` | 现在日志打「付了 N 点**能量**激活」；⚠️ **未跑 Unity** ⇒ 只有静态链 |
| 13 | **一次激活只付一次**（承接后 `_oathOps` 2 条各带 `Cost=3` ⇒ 会扣 6 而非 3） | `Core/EffectResolver.cs:293-326`（逐 op 收）· `:4730`/`:4762 ResolveOathAbility` | `R6:79-83` | 原版 = 一次 activation 一次 `UseMana`（`BattleManager__PayActiveAbilityCostOath.c:22`）；⚠️ 本报告没跑，静态推的 |
| 14 | 甲式（裸 `PxX/PxY` + `WorldW*108f`）vs 乙式（`MenuDraw.QuadRectPx`）的两处差：**(a) 父链缩放 (b) x 斜率**；今天 `k==1`+16:9 ⇒ **逐位相同**（潜伏） | `Shell/MenuDraw.cs:690-696`（乙式）· `:638-645`/`:647-659`/`:682-684` | `R6:99-106` | 不是「有断言挡着」，是**巧合相同** ⇒ 一旦非 16:9 或父件带 `localScale` 就裂 |
| 15 | `MenuDraw.QuadRectPx` 对**自带 `localScale` 的父件**取矩形 ⇒ 得 **`_w/w` 倍的宽度**（值 5/10 时**整整宽一倍**），中心项又多除一级 | `Shell/MenuDraw.cs:644`（待落成规范性一句）· `Battle/WfSlider.cs:349-352` `_fillRoot` | `R6:136-146` | 建议 `WfSlider` 加一个 **`FillRenderedPx()`**（照 `TrackWorldH`/`WorldPos`/`HasArt` 那族「自检用」访问器）；⚠️ 没实测量过，算式推的 |
| 16 | 给 `Stun` 加裸词条目后的**误伤面 = 0**：全池 `\bStun\b` 命中 30(en)/30(zh)、两集合**完全相同**、每题**恰 1 处**、无更长词包含（`Stunned` 0 处） | `工具/gen_icon_plan.py:415 BARE_TOKEN_BY_CARD` · 运行时 `CardIcons.cs:283-321` | `R7:40-42`、`R7:56-61` | ⛔ 别动 `KEYWORD_SCAN`（放开「句首+冒号」会动全池）；⛔ 别塞进 `KEYWORD_PREFIX`（成死条目） |
| 17 | 关键词段里**带正文的关键词**应印 `X:`（卡面）而我们印 `X.` —— 走 `CollectBareKeywordBody` 的卡**都差这个冒号** | `CardPresentation/Core/CardText.cs:263`（按 `CardDef.BodyKeywords` 分流） | `R7:111-121` | 影响面 **~200 张卡面**；⚠️ 本轮**只核了 3 张**（`DA39`/`SW11` 正例、`ASH20` 反例）⇒ 未逐张核 |

---

### 四、报告里提到、但不属断言批的东西

- `R1:187-193` 顺手发现：`§29·b` 的 `A1159` **字段清单错**（照「三个字段」抄会白写两个）；`A1224` 的注释 `EffectResolver.cs:2148-2152` 与正本**打架**，修的时候要一起改掉。
- `R1:194` 行号漂移：`A1159` 行引的 `RuleCore.cs:3624` 现读 `:4150`（`A1278` 行引的 `TutorialScript.cs:943` 未漂）。
- `R1:120-122` `A1258` 的两条通道**共用同一个 trigger id**（280 `AboutToAttack`），将来补①时**别新造第二个 trigger**。
- `R4:26-35` `A1311` 改法二选一：正解 = 在 `EffectText.cs:1115 LinkPrevAntecedent` 那趟给代词 op 抄**前一条 op 的 `Target.Side`**（形参扩成 `(ctx, p, slot, side)`）；止血档 = `SimpleAI.cs:848` 加 `&& op.Target.Side == "own"`（AI 评分变化，需在汇报里出声）。
- `R4:98-117` 表二·3 `CS1573` 一族 = **337 条**（运行时 320 + 编辑器 17）/ **39 个文件**，可机械补；另 `CS1570` 实读 **110 条**（台账「10 条」是 `head -20` 随机样本）。⛔ 与 `CS1570` 是两族、别一起清。
- `R5:92-98` `A1331` 数据侧改法：`cardface_fixes.json` 的 `desc` 列补 4 个字符串；⚠️ `cards_engine.json` 是生成物**别手改**、走 `工具/gen_cards_engine.py`。
- `R5:165` `A1331` 顺手发现：`OathCost = 0` ⇒ **4 张卡誓约能力打不出来**（更重的引擎侧缺陷、同一根因，⛔ 别当两笔修）；建议请调度台裁是否跑 `RuleEngineTest.Run` 复核。
- `R6:32-51` `A1344` 数据侧改法：`cardface_fixes.json` 的 `desc` 列 4 条补 `☀`（⚠️ `Adelaide` 卡面**没有冒号**、是 `6 ☀ Gain…`）；生成链要带 `PYTHONIOENCODING=utf-8`。
- `R6:108-121` `A1350` 普查表 + 「要收」三步：① `MainMenuScene.QuadPxRect` 一行转调 → ② `RectOf`×3 中心整句换设计帧读口 → ③ `RenderedRect` **先裁 `Label` 支**（35 个调用点在等裁断）。
- `R6:143-145` `A1351` 该做的两件：① `MenuDraw.cs:644` 那句落成**规范性说明**（量纲 = 建件写进去的设计矩形，⛔ 共用件不代偿 `localScale`）；② `WfSlider._fillRoot` 加注释/改名。
- `R6:159-164` `A1346` 顺手发现：付费前缀 + 尾句 ⇒ 同一次激活**可能重复收费**（候选 5 张 `UM93`/`SOR6`/`ASH_Farseer`/`ASH_Farseer_Skyrunner`/`UM_Suppressor`，**没验**）。
- `R6:152-158` `A1344` 顺手发现：`SOR72 Adelaide` 的 `desc` 把【信仰】写成【能量】**两处**（`descZh` 是对的）—— ⚠️ **不是零代码改动**，要数据 + 解析侧补 `faithcheck` 两处一起。
- `R6:165-167` 顺手发现：`WfSlider` 用「整体横向缩放」而非原版的「改 `anchorMax`」（端帽应**恒 30px**）—— **要做**（铁律 11），修它顺带从根上消掉 `A1351`。
- `R7:53-61` `A1366` 落点 = `gen_icon_plan.py:415` 加裸词条目（en 30 + zh 30）；资产齐备**不用重建**（`R7:202-203`）。
- `R7:118` `A1367` 建议：`A1367` 本身**不动**，把「带正文关键词该以 `:` 结尾」**另立一笔卡面族账**。
- `R7:63-82` `A1366` 的另一面（`A1362`）：盘上 78 张 traits PNG vs 计划表真出现 61 个，差集 17 = A 类 8 个（池里没这词，不是缺口）+ B 类 9 个候选缺口（`stun`/`sniper` 已亲读证实；另 7 个未核）。
- `D:79-85` `A1334` 主修：`EffectText.cs:1849-1861` 之后补 `When <事件>, …` 前缀分派（判据**转调 `CardDef.TryParseWhenSentence`**，⛔ 别写第二份）；影响面 = baseline 里**只有 11 行**会变、会连带改 `Coverage` 两个计数。
- `D:86-88` 已立 `A1339`：`工具/ruleprobe/Program.cs:89` 尾部补 `CreatePool.BuildNameIndex(list)`（今天 `card`/`scan`/`check` 与引擎**不同口径**）。
- `D:125-130` 已立 `A1340`：`RuleEngine/Data/CardDatabase.cs:113` 的 `BuildNameIndex` 排在**所有 `CardDef` 构造之后** ⇒ 缓存里的「卡名目标」永远是 null（今天 0 张受害，但要记成待办）。
