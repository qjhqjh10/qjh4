# B8_AN · 教程脚本 + `RuleEngine/Core/` 零散小文件（B→A）

> 执行代理 **B8_AN** · 2026-10-26 · 白名单 = 18 个 `RuleEngine/Core/*.cs`（`TutorialScript` / `CounterAttack` / `CreatePool` / `BoardSlots` / `CardInstance` / `RelatedCardData` / `PlayedThirdCardInTurn` / `ActionQueue` / `AchievementTally` … ）
> 补丁 = `资料/完全复刻/账/_parts/B8_AN.tsv`（**9 行**：8 A + 1 D）
> 判据口径：`判据/战斗逻辑驱动_B2A_口径.md` 的 G0–G8（**该表开篇明写只适用于 `Add*`/`Resolve*` 一族**）+ 方案 **§4.1**（其余方法）

---

## 一、结论（先看这 7 行）

| 项 | 数 |
|---|---|
| **升 A** | **8 行**（70 · 71 · 476 · 905 · 1299 · 1364 · 1397 · 1488） |
| **判 D**（死代码，三面扫） | **1 行**（210） |
| 改了 `.cs` | **2 个**：`CreatePool.cs` **+19 行**（`Resolve` 内一处）· `PlayedThirdCardInTurn.cs` **+44 行**（draw 分支改写 + 类尾新成员） |
| 零行数变化 | `RelatedCardData.cs`（文件头一句误读**就地订正**，122 → 122） |
| **修掉的真缺陷** | **1 类 3 处**：`CreatePool` 把「不认识的限定词」**静默丢掉** ⇒ 具名卡被降成随机兵种（3 张实证错卡，改后逐张对上原版 SO 真值） |
| typecheck | **运行时 0 / 编辑器 0**（08:03 收尾那一跑；中途一度整仓红是**别人的** `CardDef.cs:8634 CS0708`，他随后自己修好了 —— 见 §五·5） |
| `ruleprobe check` | 解析差异 **1** 行 / 哨兵失败 **1** 条 —— 都是**别人的** `f8260229 Ethereal Supreme`（未重钉），不是本批 |
| `play check` | 主档 **7** 张 / 电池 **6** 张 vs 旧基线；**我的 delta 只有 3 行**（用 before/after 隔离证明，见 §二·1） |

⚠️ **没到 10 条 A**：本批 41 条候选里，25 条卡在「无读数口 / 原语未接线 / 数据缺口 / 需人裁定」四类，
逐条列在 §四（每条都写「卡在哪、回来的条件」）。⛔ 我没有为了凑数把「读到了但接不上线」的行硬判 A。

---

## 二、逐条证据（8 A + 1 D）

| # | 账行（战斗逻辑驱动） | 原版方法 | 判据（方法体） | 我们落点（行号范围） | 观测量 · 两侧数值 |
|---|---|---|---|---|---|
| 1 | **70** | `AbilityLogic.GetCardScriptToSummon` | `decomp_full/AbilityLogic__GetCardScriptToSummon.c:1-116`（🥇 全文；旧记「只读到分支入口」**已补完**） | `CreatePool.cs:90-311（Resolve）` | 部署出来的是哪张卡：`Homing Beacon` 原版真值 `unitsToSummon=[TAU11]`（Crisis Battlesuit）· 改前 = **TAU28 Enforcer Battlesuit**（错卡）· 改后 = **TAU11** ✓ |
| 2 | **71** | `AbilityLogic.GetCardToSummon` | `decomp_full/AbilityLogic__GetCardToSummon.c:1-282`（🥇 全文 282 行） | `CreatePool.cs:90-311（Resolve）` | 同上；另 `Monster Hunters` 改前 GOF91+GOF93+GOF79（两张错卡）⇒ 改后 **GOF79 ×3**（卡面「or 3」档）✓ |
| 3 | **476** | `BattleManager.IsValidActiveAbilityTargetTutorial` | `cpp2il/out_cpp2il_dev_ilspy/BattleManager.cs:44374-44388`（🥈 唯一有真方法体那份；调用点 `:44346`） | `TutorialScript.cs:1200-1214（PermitsPlayerAction）` | 逐句对过 4 句（纯谓词 ⇒ 按方案 §4.1，`tutorial g3` 无该块） |
| 4 | **905** | `CardScript.IsTheSameTurnDrawn` | `decomp_full/CardScript__IsTheSameTurnDrawn.c:1-37`（🥇 全文） | `RuleCore.cs:2951-3031（DrawCore）`＋`BeginTurnCore`(1680-1722) | 逐句 2 句 + 三个落点各自逐句；差异=整数回合号 vs 布尔（详见补丁） |
| 5 | **1299** | `PlayerHand.AddCardsToMulligan` | `decomp_full/PlayerHand._AddCardsToMulligan_d__41__MoveNext.c:104-117`（🥇 真体在协程） | `RuleCore.cs:149-395（NewBattle）` | 档位映射逐句核过（`pos`↔`+0x34` / `extra`↔`+0x38`；`+0x3c` 写死 0 ⇒ 30 档不可达）；消费点都只判 `==30` ⇒ 今天零可观测差异 |
| 6 | **1364** | `RawCardScript.ContainsRelatedCards` | `decomp_full/RawCardScript__ContainsRelatedCards.c:1-16`（🥇 全文） | `RelatedCardData.cs:68-74（Get）` | **原版侧**：逐张扫 1,351 张 SO 的 `full_raw.relatedCard1` ⇒ **212 张非空**（其中在池 198）· **我们侧**：`related_cards.json` `count=191 / edges=239 / skipped=14`；差 7 张已点名（见补丁「差异」列） |
| 7 | **1397** | `RawCardScript.GetRelatedCards` | `decomp_full/RawCardScript__GetRelatedCards.c:1-56` ＋ 消费者 `CardDisplayWindow__InitializeRelatedCards.c:26-63`（🥇） | `RelatedCardData.cs:68-74（Get）` | 四槽占用模式全扫：`....`1139 · `X...`176 · `XX..`24 · `XXX.`6 · `XXXX`6 ⇒ **洞 = 0**（所以我们「压掉 null 的紧凑数组」与「4 元素遇 null 停」逐张等价）；边合计 **266** = 我们 239 + 落不进池 27 |
| 8 | **1488** | `SupportMethods.IsPlayerAffected` | `decomp_full/SupportMethods__IsPlayerAffected.c:1-13`（🥇 全文 13 行）＋ 调用点 `AbilityLogic__PlayAbility.c:308/324` | `PlayedThirdCardInTurn.cs:237-245（IsPlayerAffected）` | 参考系查实：`dump.cs:21973` = `<isPlayer>k__BackingField // 0x40` ⇒ 原版读的就是 `isPlayer`、不取反；`Empyric Rift` 的 `drawCard/Enemy` 档行为不变；`play check` 逐卡清单与改前**完全一致** |
| 9 | **210** → **D** | `BattleManager.AutoActionsLeftInQueue` | `decomp_full/BattleManager__AutoActionsLeftInQueue.c:1-19`（🥇 全文） | `ActionQueue.cs:233（CountOf）` | 死代码三面扫：`decomp_full` 0 调用 · `cpp2il dev` 只有声明（`private`，prefab `m_PersistentCalls` 结构上调不到）· `assets_full/**/*.json` 按名 0 命中 |

### 2·1 我的 delta 是怎么隔离出来的（⛔ 别把别人的改动算在本批头上）

`play check` 是对**旧基线**（钉的 `a93ffb8f…`）比的，而基线早已被别的代理推前 ⇒ 直接看它会把我没做的事算进来。
所以本批用的是**自己的 before/after**：把本轮 `CreatePool` 那处改动用 `if (false && …)` 关掉、重跑 `play all pf`，再打开重跑，逐卡 diff：

```
pf md5  关掉 = 32260c0e0a2c465a2958ffffd3620af1
pf md5  打开 = cad21349b3fe30c8c3b69aa468bc8444
逐卡变了 = 3 行：1688c614(Homing Beacon) · GOF105(Monster Hunters) · E|GOF96(Snakebite Battlewagon 电池)
```
三行**全部**变成原版 SO 真值：`TAU11` / `GOF79×3` / `GOF79`。
`play check` 里另外那几张（`GOF97` `SOR60` `TAU64` `TL27` `f82602` `ASH16` `GOF67` `UM105`）**不是本批**的。

---

## 三、改了哪些文件哪些行（新增行数 / 行号影响）

| 文件 | 行尾 | 行数 | 内容 |
|---|---|---|---|
| `RuleEngine/Core/CreatePool.cs` | LF | 1738 → **1757（+19）** | `Resolve` 里 `MatchKindWord` 之后新增一段（**183-193**）：带**未知限定词**（`kindPhrase != what`）且无阵营词时，先按**整段**当卡名查（`FindByNameLoose`）；命中就用具名卡（并 `unitsOnly` / `IsEnchantment` 两道门），否则照旧落兵种池。判据 = 原版 SO 真值的 `summon.unitsToSummon`（3 张实证卡）。 |
| `RuleEngine/Core/PlayedThirdCardInTurn.cs` | LF | 201 → **245（+44）** | ① draw 分支（**120-141**）改走统一判据（原来是 `if/else` 兜底「两侧都抽」）；② 类尾（**225-245**）新增 `RuleCore.IsPlayerAffected`（真值表 + 参考系出处）。 |
| `RuleEngine/Core/RelatedCardData.cs` | LF | **122 → 122（零）** | 文件头把「`iVar4` 从 1 起」那句**就地订正**成「枚举从第 0 个元素起、`iVar4` 只是显示槽号」（铁律 5）。 |

**行号影响（给 `--fix` 用）**：
- 账里引 `CreatePool.cs` 的只有**行 70/71**（本补丁已重写）；插入点在 **183 行之后**，`Resolve` 声明仍在 `:90` ⇒ **不产生新漂移**。
- 账里引 `PlayedThirdCardInTurn.cs` 的 3 条：**1407（A）落点 `:55` 未动**（我的插入都在 120 之后）⇒ 不漂；1076（B）本来就挂错（见 §五·1）；1488 已重写。
- `RelatedCardData.cs` 零行数变化 ⇒ 不漂。
- ⚠️ 我重写了 6 条 `RuleCore.cs` / `BattleDriver.cs` 的**引用行号**（不是修改那些文件）；那几个文件天天被别人改，**重定位一律按名字**（`NewBattle` / `DrawCore` / `BeginTurnCore` / `TutorialPermits` / `TutAttemptTarget`），⛔ 别再按我写的行号硬跑。

---

## 四、没查清 / 接不上线的（⛔ 所以那些行保持原状）

| 账行 | 方法 | 卡在哪 | 回来的条件 |
|---|---|---|---|
| 101 | `AiScripted.ExecuteAction` | 未接 3 档：`ActiveAbility`（**桩**）/ `ShowManaAura` / `ResolveCard`。**6 关教程数据里这三档 0 条**（`ScriptedActionType` 直方图已现读）⇒ 接了也没有行为、没有观测 | 教程关数据里出现这三档，或另给离线夹具 |
| 104 | `AiScripted.GetActingCard` | 读数已 11/11 一致，卡在**共享落点**的另一半「认不出退回 `list[0]`」= 行 106 | 先定 106 |
| **106（C）** | `AiScripted.GetChosenCard` | 原版**找不到就取 `list[0]`**；我们「⛔ 不猜、出声、不执行」（840/1091 两条红线）。**需裁定**：照抄原版降级 vs 保持「宁可认不出」 | 调度台裁一句即可（两行改动） |
| 1532 | `TurnScriptedData.UpdateName` | `cpp2il dev` 体里 `num6 = default`（回合数字串**真丢了**）、`obj != null` 那半句是装箱残留 ⇒ G0 读不全；且它只影响**可读名**、不进结算 | 用 VA 反汇编复核 `0x1809xxxx`，或维持 B |
| 566 | `BattleManager.ResolveExchangeMinion` | **我们整条换位没实现**（全仓只有注释/枚举命中）；落点不在本批白名单 | `EffectResolver`（`AbilityLogic.PlayAbility` 那条路）补换位 |
| 1025 | `CardScript.SetLeavingRemnant` | 原版一行 `+0x66=1`，唯一写点 = `AddTransformFromRemnant` —— 我们的等价入口是 `EffectResolver.DoReanimate` 残骸支（**不在白名单**）⇒ 该位今天恒 false | 那一支接上写点（并确认同步完成 ⇒ 无窗口） |
| 1166 | `EntityScript.EnemyReturnsAttack` | 方法已建、`AiScoring.cs:582` 已接（`B8_P`），但 `ruleprobe ai` 的分解走 `SimpleAI.ScoreFromAttack`（未接线那条）⇒ **没有读数口** | 给 `ai` 探针抬 option 5 的读数，或 Unity 侧断言 |
| 1253 / 1268 | `MinionManager.GetIndexOfCard` / `GetOwnPositionInBoard` | 原语 `BoardSlots.OriginalIndexOfCard` 已建且正确，**全仓无调用方**（原版的两处消费点我们模型里不存在）⇒ 无法声称「行为一致」 | 谁接 `targetsAffected=150` / `filterSpecificCards` 时顺手接上 |
| 1254 | `MinionManager.GetInteractiveUnitsCount` | 唯一消费者 `BoardAnalysis.EnemyHasMoreTroops*` 三面 0 调用（我也复核了：`cpp2il` 里 `BoardAnalysis` 其它方法都在用、就这两条没人调）⇒ 看着像死链，**但我没判 D** —— 怕「棋盘比较」这条判据在别处另有入口 | 调度台裁 D（我给的证据在补丁外的本条备注） |
| 1317 | `PlayerHand.GetMulliganOption` | 原版**死代码**（全树 0 调用点），真值在换牌协程里 | 单开一行判 D 由调度台定 |
| 1521/1522/1523 | `TriggerAbility.*` | `MissionChallenge` 定义在 **PlayFab Title Data**（84 个 bundle 0 资产） | 铁律 11 例外② ⇒ 等远端 |
| 917 | `CardScript.NeedsToChooseFromPool` | 1275 的**法力闸未接**；落点 `EffectResolver.cs:4371-4400`（不在白名单） | 那个文件的写手补 |
| 1076 | `CardScript.TriggerOtherCardAbility` | 落点**挂错**（`B8_G` 已提）：原版触发的是**另一张卡的 `ThisCardPlayed`**，不是 `PlayedThirdCardInTurn(15)` | 重判这一行；我给的方向见 §五·1 |

---

## 五、顺手发现（⛔ 除 §五·5 外都没自己改）

1. 🔴 **`CreatePool` 这个「未知限定词被静默丢掉」是一整类，本轮只补了「整段是卡名」那一档**：
   如果整段**不是**卡名（例：句子里插了 `random`/形容词），仍会静默落到兵种池随机挑。建议补一句「有未知限定词且整段不是卡名 ⇒ `Detail` 里出声」——我**没加**（怕动 `Detail` 串影响断言）。
2. 🔴 **全池召唤对拍可以一次做完**（本轮只抽验了 3 张）：335 条 `summon.unitsToSummon`（244 张卡）已经在 `cards_truth.json` 里；我们对侧只要逐张 `ruleprobe play <id>` 抓「✚ 落场/生成」那几行即可对拍（实测 ≈0.4 秒/张 ⇒ 220 张 ≈ 1.5 分钟）。这是**能一次量出「文本口径 vs 结构化真值」全表差异**的活，建议派出去。
3. ⚠️ **7 张帝皇之子卡（`EC9/EC20/EC36/EC38/EC41/EC60` + `2621d428…`）的相关卡指向隐藏附魔模板** `Mark of Slaanesh (enchantment)`（`ca169f2a…`）—— 该模板按 `X20` 决定不进卡池 ⇒ 生成器「不猜」丢掉 ⇒ 这 7 张的详情窗**退替身**（会出声）。要不要为「相关卡显示」单独把附魔模板放进池子，是**产品裁定**（它会让 98 张战术卡的造牌口径受影响吗？只影响详情窗的话风险低）。
4. ⚠️ 原版 `InitializeRelatedCards` 的 `iVar4` 从 1 起**只是显示槽号**，不是跳过 `relatedCard1` —— 旧记误读，已在 `RelatedCardData.cs` 文件头与账 1397 两处订正。
5. ✅ **一度出现的编译红不是我的**（已由对方修掉，现 0/0）：`MyGame/Assets/RuleEngine/Core/CardDef.cs(8634,21) error CS0708` —— `TraitReachability.IsWarlordEquivalent` 在**静态类里声明了实例成员**。它当时堵着所有人的 typecheck，错误行**没有一处**落在我改的三个文件上。
6. ⚠️ **`offensive_card_abilities.json` 里 `Empyric Rift` 的 `drawCard` 是 `playersAffected: "Enemy"`** ⇒ 我们 `Draw(ctx, 1-seat)`（给**对手**抽）。卡面文案在远端拿不到 ⇒ 无法从卡面核「给对手抽牌」是不是原意，如实挂着。
7. ⚠️ `ruleprobe check` 的**解析差异 1 行** = `f8260229254a09746af6d9ff7a9b0b54 | Ethereal Supreme`，**与本批无关**（我只改了 `CreatePool.Resolve` 与进攻卡的 draw 支，两者都不在 `EffectText.Parse` 路径上）；⌛ 它属于别的代理未重钉的改动。

---

## 六、给调度台的请求

1. **合并**（⚠️ 合并后**立刻归档**）：
   `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B8_AN_`
   然后把 `B8_AN.tsv` 移进 `_parts/_merged_B8_AN/`（合并过的补丁不能留在原地）。
   `check_patch_conflicts.py` 已跑：**没有跨补丁重复键**。
2. **新指纹**（⛔ 我不重钉，请在同步点统一）：
   · 不含本批改动：`play all pf` md5 = `32260c0e0a2c465a2958ffffd3620af1`
   · **含本批改动**：`cad21349b3fe30c8c3b69aa468bc8444`
   · 基线文件里钉着的 `a93ffb8f…` 早已过期；`play check` 现在的主档 7 张 / 电池 6 张 = 旧基线 → 现状的差，其中**只有 3 行是本批**。
3. **行号重定位**：本批**预期不产生新漂移**（理由见 §三「行号影响」）。
   唯一要重判的是 **1076**（落点本来就挂错，`B8_G` 也提过）—— 方向：原版真体 = `abilityCard.OnTriggerEffectInCard(AbilityTrigger.ThisCardPlayed, mgr, this, null, null)`，与我们 `OffensiveCardData`/`PlayedThirdCardInTurn` 那条**不是同一族**。
4. **转派**（按优先级）：
   ① **全池召唤对拍**（§五·2，判据已齐、只差跑）——这是把「文本 vs 结构化」的全表差异一次量出来的活；
   ② **106 的一句话裁定**（「认不出退回 `list[0]`」照抄 vs 保持不猜）——它同时解开 104；
   ③ `EffectResolver` 补 `AddTransformFromRemnant` 等价入口的写点（1025）、补 1275 法力闸（917）；
   ④ `CardDef.cs:8634` 的编译红（§五·5）——现在**堵着所有人的 typecheck**。
