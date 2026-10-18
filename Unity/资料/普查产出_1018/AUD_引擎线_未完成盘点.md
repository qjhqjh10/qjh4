# AUD · 引擎线未完成盘点（只读现核 · 2026-10-19）

> 范围 = `资料/任务线/引擎线/任务总表.md` §一 全部未完成项（T01–T11）＋ 两块大账（C 类 11 种效果码 / 88 条实例 · 63 张执行层未归因 / 52 张未核）＋ DA1 Azrael。
> 方法 = 只读：读任务总表 / 五份 T 详情 / 三份 1018 报告 / 历史归档 / 判据件，逐条回**代码现读**核对；未跑 Unity、未跑 typecheck、未跑 ruleprobe、未动 git、未改正本。
> 判据权威序沿用 `任务线/引擎线/准则与做法.md` §二：全量反编译 → 解包 SO/prefab → PnP 卡图 →（粉丝规则书只当旁证）。
> 🔴 **本文件里的行号一律是 2026-10-19 现读**；任务/判据件里的旧行号若已漂，本文件直接给现读值。

---

## §一 结论（先看这 12 条）

### 1·1 计数

| 口径 | 数量 | 说明 |
|---|---|---|
| 任务总表自称 | **11 件 / 55 笔** | 55 = 源表状态分布（36 未做 + 10 部分 + 5 判据未定 + 1 暂缓 + 3 判不出） |
| 本盘点实际覆盖 | **11 件 / 66 个 A 号 + DA1 = 67 行** | ⚠️ **55 无法从 A 号复原**：T 号下名义 A 号就有 66 个，而且互相有「并入 / 同笔双号 / 子号」关系（`A1822`→`A1772`；`A1772≡A1823`；`A2010/2014/2015` 是 T01 子号）⇒ **以后别把 55 当逐条清单用** |

**现核后的处置分布（67 行）**：

| 处置 | 条数 | 含义 |
|---|---|---|
| ✅ **可关账** | **20** | 判据已定 / 代码已落 / 后续会话已做掉，账过期（见 1·2 名单） |
| 🟢 **现在可做** | **37** | 判据齐、文件可定位、不需要裁定（另加 T01 两笔新账 = 39） |
| 🔴 **判据未定 / 等裁定** | **6** | `A1916` · `A1412` · `A1961` · `A1896` · `DA1` · `A1651`（后者是「现状判不出」） |
| ⛔ **被别的件阻塞** | **1** | `A1258`（要先有 `A1567` 附魔层） |
| ↔️ **转线** | **3** | `A2017②`→战斗表现线 · `A828`→特效线 · `A1044`→界面线 |

> 计数口径：`可关账` 按主表「现核状态」列数；其余按「现在可做？」列数。`A1155` 的「一行 doc」计入可关账；`A2000` 的「先钉死是不是真缺陷」计入可做；`A1819` 的 `SecretOrder` 那半折进 `A1567` 的依赖链。

### 1·2 现核发现（会改计划的 12 条）

1. 🔴 **`A1571` 的卡 id 已漂**：`BL68` 现读 = **Zealot Legionary**；`Black Crusade` 现在是 **`BL55`**（`cards_engine.json` 现读 + `card_ids.json.mapping`）。且旧账的「同族 4 张 `TAU27`/`TAU67`/`GSC68`/`BL68`」**今天对不上**：现读这 3 个 id 分别是 Broadside Battlesuit / Kroot Hound / Atalan Jackal，**desc 里都没有 "Deploy … from your deck"**；全池该写法**只剩 `BL55` 一张**。⇒ 先 `play BL55` 复现，⛔ 别再按旧 4 张派活。
2. 🟢 **`A1959` 的前置数据缺口（旧账 `A970`「真值只采到 4.3%」）已经不存在**：2026 导出 `allcards_1350/cards_raw.json` 的 **`full_raw.choicesDefinition.maxTargets` 对池内 1127 张全有值**（分布 **0:979 · 3:127 · 2:12 · 4:9**）。旧 `BattleDriver` 注释里的「977/204/10/1」与现读对不上（旧口径是哪一版快照**没查明**）⇒ `A1959` 现在能直接做，不需要再等真值。
3. 🟢 **`A1958`（extrinsic 来源离场撤回）已经实现**：`RuleCore.cs:7188-7232` 现有 `Extrinsic` + `ActingUnit` + `IsUnitInPlay` 剪枝，并有 jam(0x82) 第二条支线 ⇒ **可关账**（旧账「我们一律不撤」已过期）。
4. 🟢 **`A1155②`（Prayed 时长）已经落地**：`RuleCore.cs:1300-1332` 按 `OnTurnStart.c:100` 在**每次回合开始、不分敌我**清 `Prayed`；`③`（先费用后格位）也已核。⇒ 本号只剩 `RuleCore.cs:2091` 一行 doc 还写「这半条还没逐句核过」。
5. 🟢 **`A1590③` 的「全池 0 张光环给 Stealth」已被复核**：`RuleCore.cs:1215-1219` 记着 2026-10-13 `W1` 重扫结论（1 处是条件句、0 处光环）⇒ 可关账（保留「将来加卡要重核」）。
6. 🟢 **`A1819` 的 `EvtKind.Draw` / `AmbushExit` 已经进了消费端 switch**：`BattleDriver.cs:7299/7314/7326` 现读有 case；`SecretOrder` 有 case 但**无发出点**（依赖附魔层，见下）⇒ 本号只剩 `SecretOrder` 那半，而它属 `A1567`。
7. 🟢 **`A1666`（活字符串里的「规则书 :N」）已清零**：现核 `Core/` 里 `Log(...规则书...)` / `Append(...规则书...)` = **0 处**；剩下的全在注释（属 `A1944` 的边界问题）。
8. 🟢 **`A1569` 三处记录订正已全部落盘**：`CardDef.cs:420-422`（47 词算法）· `:595-597` / `:1162-1165`（Maulerfiend `keywords` 现读 `['Ecstasy 5']`）。
9. 🟢 **`A1826` 的三指纹已随 K06 有意重钉**：`工具/ruleprobe/Program.cs:152`（pool `d134413b…`）· `:174`（baseline `9e98a496…`）· `:225`（parse `a28da8e3…`），且每一条都留了「为什么重钉」的判据 ⇒ 「要不要重钉」这一问已被回答。
10. 🟢 **`A1650` 的读口已经在**：`BattleContext.cs:1327 IsPendingDeath`（E6 落），且 `:1324` 明写「⛔ 别拿 `TakePendingDeaths` 代替」。
11. 🔴 **`A1985` 的「引错对象」有确切更正**：`EntityScript__HasSecretOrders.c` 的访问链是 **`ActiveEnchantment.enchantmentCard(+0x10) → spellType(+0x90) == 0x3c(60)`**（`dump.cs:118926-118930` 的 `enchantmentCard // 0x10`；`dump.cs:23244+` 的 `RawCardScript.spellType // 0x90`），**不是** `CardEffect + 0x90`。
12. 🔴 **`63 张未归因` 名单仍有效但存放位置危险**：名单只在 `_tmp_view/willrun_mechanism.md`（**`.gitignore` 里**），本报告已把 63 张逐张抄进 §四；63 个 id 与卡名在现池**逐一仍对得上**（K06 迁移没有动到它们）。

### 1·3 一句话

**引擎线的「结算核心」并没有 55 条大缺口**：现核能把 20 条直接关掉、37 条判据已齐可开工；真正卡住的是 **6 条等裁定/判据**（`A1916` 的 buffEffect 收益方 · `A1412` 的 A1346 一格 · `A1961` 的比较函数 · `A1896` 的三选一 · `DA1` 的隐藏能力 vs 数据残留 · `A1651` 的现状判不出）和 **1 条依赖链**（`A1258` ← `A1567`）。

---

## §二 主表（逐 A 号）

> 列义：**现核状态** = 2026-10-19 代码/数据现读；**现在可做？** 里的「能」= 判据齐且不依赖未裁事项（不等于「今天必须做」）。
> 文件路径均相对 `d:/4/Unity/MyGame/Assets/`，另有说明的除外。

### T01 伤害与死亡结算补缺（8 个 A 号 + 2 笔新账）

| A号 | 一句话内容 | 现核状态 | 判据出处（现读） | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1875` | `doDamage` 一族逐项对账 | ✅ **可关账** | 加闸后 16 行、逐行核过 **0 条真缺口**（`任务/T01…md` §二 · `普查产出_1016并行/引擎线T01_报告_第四十二会话.md` §四·2） | 无（留对账件） | 否（已完结） | `A1914` 是它的子集 |
| `A1914` | 30 条 ACT 残留是否该补 | ✅ **可关账** | `CardScript__SetBaseCardTraits.c:37` + `ResolveActiveAbilityPlayed.c:23` + `GetTalentCard.c`（T01 §六·1） | 无 | 否（判据已定 = 不该补） | `A1875` 子账 |
| `A1916` | `Unstable` 的 `conditionsFilter 220` / `addTrait fast` / `buffEffect` 收益方 | 🔴 **？判据未定** | T01 §六·2；**仍缺** `AbilityLogic__PlayAbility.c` 消费 `buffEffect` 那一段（未读） | `Core/RuleCore.cs`（`UnstableBlast` `:8573` / `CleanupDeaths` `:6822`）+ 判据件 | **不能**（差「收益方」一格；且 `UnstableBlast` 无 killer 形参，要从 `CleanupDeaths` 传） | 与 `A1994` 悬案 (a) **同一格** |
| `A2017` | 伤害类型/易伤三条守卫对账 | ◐ **引擎半可关账 · ②转线** | T01 §二 / 报告 §四·3；①③ 与我们等效，**②（易伤高亮）整条缺、属表现层** | ② → `CardPresentation/Battle/BattleDriver.cs`（**转战斗表现线**） | 引擎侧不做（已核）；②转线 | 与「10 条战斗表现偏差」①同族 |
| `A2014` | `doubleRage` 登记 + 机制 | ✅ **可关账** | 机制已落 `RuleCore.cs:6216`（T01 记的 `:6178` 已漂）；`CardDef.DoubleRage` `:3429`；0 张可达 | 无（`CardDef.cs:3403-3413` doc 过期 → 见新账） | 否 | `A1875` 子账 |
| `A2015` | 授甲两种表示同步 | ✅ **可关账** | 已落 `EffectResolver.cs:7029-7031`（`case "armour"` + 关键词副作用） | 无 | 否 | 与 T01 新账 §六·6 同族 |
| `A2010` | 空投舱「池已碎」的 `actual` 陈旧值 | ✅ **可关账** | 已落 `RuleCore.cs:5963-6018`（`podVulnRaw` / `actual = podVuln`） | 无 | 否 | `A1335` / T01 §六·7 同族 |
| `A1335` | `deathType` 映射 / 堡垒吃满算不算受伤 / 真跑 | ✅ **可关账（④ 延后）** | T01 §二/§六·3；唯一发射口 `RuleCore.cs:4207 TryCreditKill`（T01 记的 `:4176` 已漂） | ④ 归 `RuleEngineTest.Run`（收口统一验） | ③ 不需要改；④ 收口跑 | `A1335①` ≡ `A1563`（同笔双号，别数两遍；已销） |
| **新账（T01 §六·6）** | `UnitState` 两处护甲脱节 | ✗ | T01 §六·6；现读 `Core/UnitState.cs:941`（`ApplyGrant case "armour"`）· `:831-837 RemoveKeyword` 反方向 | `Core/UnitState.cs` | **能**（判据同 `A2015`） | 与 `A2015` 同一判据 |
| **新账（T01 §六·5）** | `CardDef` 里 `DoubleRage` 的 doc 过期 | ✗（注释） | T01 §六·5；现读 `Core/CardDef.cs:3403-3413` 仍写着「不进 `Implemented`」 | `Core/CardDef.cs` | **能**（一行） | — |

### T02 条件·替代·落位锚点（5）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1688` | 有条件 `instead` 双结算（5 张过伤） | ✅ **已修待收口验** | `Core/EffectResolver.cs:93 InsteadSkips`（2026-10-18：先解被替换条目的目标当 referent）；测试 `Editor/RuleEngineTest_S23_Resolve.cs` | `Core/EffectResolver.cs`（已改） | 否（已修；收口跑自检） | 与 `A1388` / `A1685` 同族 |
| `A1426` | `Sacrifice:` 的 `+1` 被 `!IsAlive` 丢掉 | ✗ | T02 §二；**现读 `EffectResolver.cs:6842` 仍 `if (t == null \|\| !t.IsAlive) continue;`**（`DoGive` 目标循环） | `Core/EffectResolver.cs`（只改 `DoGive` 一处） | **能**（判别式 = 「在不在场上」替换 `!IsAlive`，⛔ 不许删闸） | 同族 10 处未查 |
| `A1692` | `DeployFree` 有洞棋盘静默覆盖 | ✗ | T02 §二；**现读 `RuleCore.cs:3097` 的 `DeployFree` 仍是 `ps.Board[dst] = unit;`，无占用闸** | `Core/RuleCore.cs` | **能**（注释必须写「原版行为不可比」） | 与 `A1690` 落位同族 |
| `A1690` | `When … adjacent …` 静默锚到「自己」 | ✗ | T02 §二；**现读 `EffectText.cs:1481` 仍是 `t.AdjacentFailed = true;`（⑤ 档），没有走 `Self`** | `Core/EffectText.cs` + `Core/CardDef.cs:2546-2548`（`evHasTarget` 同步）+ 测试 | **能**（需人造夹具；`SW16`/`SW35` 原版 ability 数据本地取不到） | 与 `A1692` 同族 |
| `A1412` | 誓约收费抬到 `ability` 层 | ◐ **等裁定** | T02 §二；`ChargePaidGroups` `EffectResolver.cs:428-452` 已落；**`A1346` 那一格（全池 0 张）没查实收口** | `Core/EffectResolver.cs`（若裁定再收） | **不能**（等一次裁定） | — |

### T03 事件层与时机（8）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1649` | `damaged` 广播排在扣血之后（原版之前） | ✗ | T03 §二；**现读 `RuleCore.cs:6131-6132` 是 `u.Health -= actual; EmitHit(ctx,u,actual);`** | `Core/RuleCore.cs` + **先盘** `EvtKind.Hit` 消费面（`BattleDriver.cs`） | **能，但跨面**（先只读盘清多少处吃「掉血后的值」再改） | 与战斗线「结算领先表现」①同源 |
| `A1958` | 有绑定的 extrinsic 在来源离场时撤回 | ✅ **可关账** | 已落 `RuleCore.cs:7188-7232`（`Extrinsic` + `ActingUnit` + `IsUnitInPlay` + jam 0x82） | 无 | 否（已实现） | 与 `A1819 SecretOrder` / `A1567` 概念相邻 |
| `A1819` | `Draw` / `AmbushExit` 在 `PlaySignal` switch 外；`SecretOrder` 无发出点 | ✅ **可关账（SecretOrder 除外）** | `BattleDriver.cs:7299/7314/7326` 现读**已有 case**；`SecretOrder` 的发出点依赖附魔层（`BattleEvent.cs:180-187` 自陈） | `Core/BattleEvent.cs`（注释；`SecretOrder` 属 `A1567`） | Draw/AmbushExit 已结；`SecretOrder` 不能（依赖 `A1567`） | 与 `A1567` 重复 |
| `A1560` | `EC28` 的 `Slay:` 头把 `Ecstasy 3:` 吞进正文 | ✗ | T03 §二；**现读 `CardDef.AddTriggerOp:2018` 取 `seg.Substring(c+1)` 整段、不切下一个触发头** | `Core/CardDef.cs`（`TriggerTexts` 产出地） | **能**（改一处） | 与 `A1469` 同族 |
| `A1651` | 连出两张时手牌下标被前一次挪位 | 🔴 **？现状判不出** | T03 §二；**现读未定位到「写死第一次 HandIdx」的缓存点**（`BattleScene.HandIdxByName:26296` 每次按名重算） | 待定位的调用点 + 断言 | **不能（先复核）**：需一次全仓 grep/只读复核确认是否已被修 | — |
| `A1650` | 将死队列读口 | ✅ **可关账** | `Core/BattleContext.cs:1327 IsPendingDeath`（E6 已落；`:1324` 明写别用 `TakePendingDeaths`） | 无 | 否 | — |
| `A1670` | 换牌 `mulliganOption != 0x1e` 闸 | ✗ | 判据 `CardScript__ClickMulliganSelected.c:27`；现读全仓 `mulliganOption` **0 命中** | `RuleEngine`/`BattleDriver` 换牌路（落点待定） | **能，但先判可达**（非教程局大概恒不触发；⛔ 铁律 11 不许因「用不上」划掉） | — |
| `A1590` | aura/stealth 相对次序 | ✅ **可关账** | `RuleCore.cs:1205-1219` 现读：`Auras.Recompose` 排在 stealth 到期前，且 2026-10-13 `W1` 已复核「0 处光环给 Stealth」 | 无 | 否（将来加卡时重核） | `A1397②` 已落 |

### T04 关键词语义与附带层（2）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1258` | 「取消攻击」①效果型通道 | ✗ | T04 §二；现读**仍无实现/桩/出声**（`grep ancel` 只有注释） | `Core/RuleCore.cs`（`DeclareAttack`）+ **附魔层** | **不能单独做**（依赖 `A1567`；共用 trigger 280，⛔ 别新造 trigger） | 与 `A1983` 同族 |
| `A1567` | 附魔层整层（两条并列 list + 五个入口） | ✗ | T04 §二；`BattleEvent.cs:180-187` 自陈「无层」；`dump.cs:118926 ActiveEnchantment{enchantmentCard 0x10, enchantmentCardID 0x18}` | `Core/CardInstance.cs` · `Core/BattleEvent.cs` · `Core/RuleCore.cs` · `Core/EffectResolver.cs` | **能**（大件；判据齐；0 张卡可达 ⇒ 断言必须人造夹具） | `A1258`/`A1819`/`A1985` 共用 |

### T05 选择面板的目标与顺序（3）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1959` | 面板「摆几张」按逐卡 `choicesDefinition.maxTargets` | ✗ **但前置缺口已解** | 🆕 **现核 `cards_raw.json:full_raw.choicesDefinition.maxTargets` 池内 1127 张全有值（0:979 · 3:127 · 2:12 · 4:9）**；旧账 `A970` 的 4.3% 已过时；`BattleDriver.cs:6062 ChooseShowByCardId` 仍 6 张手工表 + 众数 3 | `工具/gen_cards_engine.py` + `Resources/cards_engine.json` + `Core/CardDef.cs` + `CardPresentation/Battle/BattleDriver.cs` | **能**（不再等真值） | 旧账 `A970` 的缺口已由 2026 导出补上 |
| `A1961` | 候选面板顺序（原版最后一次 `Sort(CompareChoiceOptions)`） | 🔴 **？判据未定** | T05 §二；现读 `BattleDriver.cs` 里**没有 Sort**；比较函数仍需 **VA 反汇编**（`decomp_full` 同名文件内容不符，见 `A1962`） | 先只读解判据 → `BattleDriver.cs` | **不能**（先解出比较函数，⛔ 不许自己发明排序） | — |
| `A1960` | 黑暗契约一族「开不开面板」 | ✗ | T05 §二；**现核 `BL4/BL13/BL29/BL37/BL38` + `GSC65` 全是 `maxTargets=4` · `targetsAffected=180`**；`EffectText.TryChooseCard` 不发 `choosecard` | `Core/EffectText.cs` + `Core/CreatePool.cs`（`DarkPactContracts`）+ `BattleDriver.cs` | **能** | 与 `A1949` 那张 6 张表**是两件事** |

### T06 AI 与评分表（2）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1797` | AI 选牌/选目标照原版逐卡评分表 | ✗ | 🆕 **现核**：池内 **507/1127** 有逐卡评分（`cardTargetScoringCriteria` 261 · `nonTargetScoringCriteria` 307，有重叠；全 1,351 张口径 = 367/387，即任务总表那组数）；`attackScoringCriteria` 1127 张**全是同一份全 0 模板** | `RuleEngine/Data/SimpleAI.cs`（`ScoreOps:721`）+ `gen_cards_engine.py`/`cards_engine.json` + `Core/CardDef.cs` | **能**，但**先裁一句口径**：620 张没有逐卡评分的卡怎么办（⛔ 别拿模板冒充真值） | — |
| `A1312` | 变身/自动召唤那一档的回合状态（`keepStatus`） | ◐ | `待办判据` §A1312；现读 `ApplyDeployTurnState` 已有 `keepStatus`；剩「`canAct/canAttack` 两位跟 `SummonSickness && !fast && !flank` 走」 | `Core/RuleCore.cs` | **能**（两位重算；0 调用点传 true ⇒ 零行为变化）；`keepEffects==true` 的**实例**仍等远端/二进制数据 | — |

### T07 逐卡对账与数据侧补齐（9 个 A 号 + DA1）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1759` | 用原版效果数据库逐张对账 | ◐ | 产物 `普查产出_第二十九会话/A1937_对账_改后.tsv`（1822 行）+ `第二十八会话/A1759_逐条对账.md`；`R_引擎效果覆盖率.md` §5：`A1977` 的 192 条「真差异」抽查 6/6 是仪器误判 | 报告 + 仪器 | **能**（先修仪器 `A1986`/`A1977`/`A1991`，再 action-aware 重跑） | 与 `A1772` 同件 |
| `A1772`（≡`A1823`） | `A1759` 续：`abilityLogic[]` 逐张对账 | ◐ | `A1937_对账_改后.tsv`；⚠️ **同笔双号，别数两遍** | 同 `A1759` | **能**（同上） | 与 `A1759`/`A1823` 同一份 |
| `A1822` | `SummonCriteria` 135 条 | ✅ **可关账** | 2026-10-14 订正：病根是 `Talent:` 正文没进解析（已修 `EffectText.TryTalent`）⇒ **并入 `A1772`，不是独立第二份账** | 无 | 否 | 并入 `A1772` |
| `A1571` | `BL68 Black Crusade` 真打零效果 | ✗ | 🆕 **现核：id 已漂** —— `BL68` = Zealot Legionary；Black Crusade = **`BL55`**；全池「Deploy … from your deck」只剩 `BL55` 一张；`BL55` 的 SO 是 `autoPlayFromHand(250)` ×4（见 §三） | `Core/CreatePool.cs` / `Core/EffectResolver.cs`（先诊断） | **能**（先按 `BL55` 跑一次 `play`；旧的 4 张同族表已失效，⛔ 别照派） | 与 C 类 `autoPlayFromHand` 同源 |
| `A2009` | `DoChooseCard` 「造了新实例却不写指代槽」的其余分支 | ◐ | `交接_第三十一会话.md` §六·十·二 `N11`；**现读 `EffectResolver.cs:3164-3170` 注释 + 6 个分支都 `?? pickedInst`**（deploy/todeck/shuffle/decktop/return/enemyhand） | `Core/EffectResolver.cs`（若真打发现缺口） | **能**（补「逐支真打」验证；代码形状已统一） | 与 C 类 `setReferenceCard` 共用指代槽 |
| `A1896` | 逐张摘除调用点比原版多一处 | 🔴 **？等裁定** | `普查产出_第二十八会话/W10_引擎线.md`；原版唯一调用点 = `DeckEditingWindow__TryOpen.c:47` | `CardPresentation/Deck/DeckEditorState.cs` + `Battle/DeckBuilder.cs` | **不能**（三选一未裁：保留 / 只在开编辑器跑 / 两处都跑） | — |
| `A2054` | 黑暗契约记号族判据三份 | ◐ | `F1_RuleEngineTest修红.md`；现读 `CreatePool.MentionsDarkPact:1118-1125` 仍只认字面 `黑暗契约`（另有自检交叉核对） | `Core/CreatePool.cs` + `Editor/RuleEngineTest.cs` + `Editor/IconSetup.cs` | **能**（收成一处判据；铁律 6） | `A2045` |
| `A2055` | `CreatePool` 两处过期注释 | ◐ | 现读 `CreatePool.cs:267-269` 仍举 `Sergeant Taaman↔Naaman`（卡名已改）；`:836-848` 的 `128/67` 已就地订正 | `Core/CreatePool.cs` | **能**（一行注释） | — |
| `A1881` | 回填 `BuffType` 15 个取值的说明 | ✗ | `abilityLogic_枚举词典.tsv` 仍是旧快照（`BuffType.cs` 0-4 等，无 12/15 说明） | `资料/普查产出_第二十七会话/abilityLogic_枚举词典.tsv` | **能**（文档） | — |
| `DA1` | Azrael 的 SO-only 能力（`QuestTriggered → createCardInDeck`） | 🔴 **？等裁定** | `R_引擎效果覆盖率.md` §5；卡面没这段、代码无路径；`trigger=490` / `chosen=270` 无本地化文本可核 | （若裁定要做）`Core/CardDef.cs` + `Core/RuleCore.cs` + `Core/EffectResolver.cs` | **不能**（裁定：隐藏能力 vs 数据残留） | 与 `createCardInDeck` C 类相邻 |

### T08 探针与量具口径（10）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1965` | 全池对拍还差「档 B」事件电池 | ◐ | `普查产出_第三十会话/A1965_电池步设计.md`（现状 6 步 / 154 张 / 6 个新步设计） | `工具/ruleprobe/PlayProbe.cs` | **能**（施工图已有） | 与 `A1979` 是两条不同线（动态 vs 静态） |
| `A1941` | `desc` 为空的 6 张卡盲区 | ◐ | `工具/ruleprobe/Scan.cs:138` 现读仍是 `if (string.IsNullOrEmpty(desc)) continue;`；`GOF8` 仍未查 | `工具/ruleprobe/Scan.cs` + 判据件 | **能**（小） | — |
| `A1598` | 探针两笔尾巴 | ✗ | `历史/A表已收口_第二十会话.md` §九·2 | `工具/ruleprobe/Program.cs` / `WillRun.cs` | **能** | — |
| `A1568` | `Source` / `Tail` 在探针里看不见 | ◐ | ① **已修**：`Program.cs:1091-1145` 有 `/[SRC:…]` 渲染 + 哨兵⑧；② `Tail`/`ChooseDeadScope` 仍只在 `Dump`、`Scan` 不打 | `工具/ruleprobe/Program.cs` / `Scan.cs` | **能**（只剩 ②） | 与 `A1561①` 同族 |
| `A1561` | 引擎/探针杂项四条 | ✗ | `待办判据` §A1561（`EffectOp` 50 字段 dump 只盖 15） | `工具/ruleprobe/*` + `Core/EffectText.cs` doc | **能** | 与 `A1568` 同族 |
| `A1573` | `play` 的已知误报/噪声族 | ✅ **可关账** | 已收进 `准则与做法.md` §6·1·5（四条噪声族） | 无 | 否（口径记录） | — |
| `A1952` | `PlayProbe.Blank` 丢两列 | ✗ | 现读 `PlayProbe.cs:716 Blank` **未传** `fromOriginalPool` / `goesSecondId`（`CardDef.cs:240-241` 有这两个形参） | `工具/ruleprobe/PlayProbe.cs` | **能**（小） | 与 `A1942` 同族 |
| `A1681` | `FaithProbe` 的尺子 Q 与注释脱节 | ✗ | 现读 `FaithProbe.cs:79` / `:407` 仍用 `WillRun.IsPaidHead` | `工具/ruleprobe/FaithProbe.cs` | **能** | — |
| `A1594` | `RuleEngineTest` 里的 `WillRun` 判据副本 | ✗ | 现读 `Editor/RuleEngineTest.cs:9983-9985` 自有 `WRSegPaid` 等常量与判定 | `Editor/RuleEngineTest.cs` | **能**（收成转调探针那唯一一份） | 与 `A1598` 同族 |
| `A1826` | 三指纹要不要重钉 | ✅ **可关账** | 现读 `Program.cs:152/174/216` 三常量已随 K06（2026-10-18）有意重钉，并各留判据 | 无 | 否（已在 K06 做了） | — |

### T09 判据链自证与记录订正（6）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A1666` | C# 活字符串里 6 处「（规则书 :N）」 | ✅ **可关账** | 现核 `Core/` 里 `Log(...规则书...)` / `Append(...规则书...)` = **0 处** | 无 | 否 | `A1660` 已做 |
| `A1944` | C# 里约 37 行「字符串里的规则书 :N」 | ✗ | 现核（非注释字符串）：`Editor/RuleEngineTest.cs` **32** · `Editor/DeckRulesTest.cs` **15** · `Editor/BattleScene.cs` **10** | 测试/编辑器文件（多数是断言标签） | **能**，但**先判边界**：断言标签 vs 真判据（⛔ 别把「断言文案里提到规则书」当判据依赖） | `A1666` 的续 |
| `A1955` | 4 处判据只引粉丝规则书 | ◐ | 现读多数已标「旁证、非官方」：`EffectResolver.cs:920` · `BattleContext.cs:10` · `EffectText.cs:3816/3918/7547` | `Core/EffectResolver.cs` · `Core/BattleContext.cs` · `Core/EffectText.cs` | **能**（逐处换「原版方法体 / 字段」或明标「我们挑的」） | — |
| `A1569` | 三处记录订正 | ✅ **可关账** | `CardDef.cs:420-422` / `:595-597` / `:1162-1165` 现读均已订正 | 无 | 否 | — |
| `A1577` | 五条小账（doc 挂错 / `ICardProvider` 注释 / `keywords` 默认值 / F1 两处 / 临时目录） | ✗ | `待办判据` §A1577；**现读 `Core/ICardProvider.cs:15` 仍写「现在用卡名」**（与 1127 张 id 全非卡名的事实不符） | `Core/ICardProvider.cs` · `Core/EffectText.cs`（6 处双 summary） · `Core/CardDef.cs:1928` | **能**（便宜） | — |
| `A1155` | 三条判据不足 | ✅ **可关账（一行 doc）** | ① 已落（`RuleCore.cs:8303` 附近有订正痕）；② 已落 `RuleCore.cs:1300-1332`；③ 已核；**只剩 `RuleCore.cs:2091` 仍写「这半条还没逐句核过」** | `Core/RuleCore.cs`（一行） | **能**（一行） | — |

### T10 其余零星与口径（2）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A179⑤` | ⑤ 本身已收口；本行只留 ⑥/②⑦⑧ | ✅ **可关账** | `待办判据` §A179；⑥/②⑦⑧ 按 §四·17【暂不裁】；①④ 已进真 Play 清单 | 无 | 否（本号已收口） | ⑥/②⑦⑧ 属同一行其它子项 |
| `A1664` | 「选择防御卡」面板与原版不符 | ✅ **可关账** | 现读 `BattleDriver.cs:5127 defensiveCardPanelEnabled = false`（默认关，照原版）；发卡走 `RuleCore.EndMulligan → GrantDefenceCard` | 无（真机验收延后） | 否 | — |

### T11 【迁入批】§29·b 的账（11）

| A号 | 一句话内容 | 现核状态 | 判据出处 | 要改的文件 | 现在可做？ | 与谁重复 |
|---|---|---|---|---|---|---|
| `A828` | 粒子订阅者无运行时断言 / 35 个 def 报 `[ERROR] particle system not assigned` | ⏸ | `待办判据_第二十五会话.md` §A828 | `CardPresentation` 特效侧 | **不能（本线不属 + 要 Unity 跑）** | ↔️ **转特效线** |
| `A1044` | `A1003` 收口留下的三件（第 8 份内联副本等） | ⏸ | `待办判据_第四会话.md` §A1044 | `Editor/ShellScene.cs:1186-1202` · `Editor/MainMenuScene.cs` · `Editor/DeckScene.cs` | **不能（本线不属）** | ↔️ **转界面线** |
| `A1975` | 教程起始单位少 trait 激活 + 四个钩子 | ◐ | `待办判据` §A1975；**现读 `TutorialScript.cs:1269` 已补 `Auras.Recompose`（调用点）**；`PlaceTroops` 里 **trait / ResolveDeploy / ResolveFaithAbility / RecomputeCurrentAttackType+ForceAttackTypeOnDeploy 仍未补** | `Core/TutorialScript.cs` | **能**（⛔ 别加 `summonSickness`/`Exhausted`，原版起始单位不吃召唤病） | 与 `A1990` 同一件 |
| `A1983` | 取消攻击路上的前置状态闸 | ✗ | `CardScript__CancelAttack.c:35`（只有 `cardState ∈ {3,0x11}` 才取消得掉）；现读 `RuleCore.cs:3285` 已引判据、**未落** | `Core/RuleCore.cs` | **能**（按「已声明攻击、未结算」的等价窗口落，⛔ 别照抄 `cardState` 字段） | 与 `A1258` 同族 |
| `A1985` | `Core/BattleEvent.cs:181` 注释引错对象/偏移 | ✗ | 🆕 **现核**：`EntityScript__HasSecretOrders.c` 读 **`ActiveEnchantment.enchantmentCard(+0x10) → spellType(+0x90) == 0x3c`**，不是 `CardEffect+0x90` | `Core/BattleEvent.cs:181` | **能**（一行） | 与 `A1567` 同批 |
| `A1986` | 对账仪器缺 `choosecard ↔ create/recall/…` 桥 | ✗ | 现读 `工具/_a1759_reconcile.py:158`（`'create': {'create'}`）· `:194`（`'choosecard': 'choose'`），**无 `selectFromChooseResolution` 桥** | `工具/_a1759_reconcile.py` | **能**（补桥 + 跨全表复核） | `A1977` / `A1991` |
| `A1990` | 「新单位落地入口 = 三个」口径本身错了 | ✗ | 现读 `Core/RuleCore.cs:2137-2140` **仍写「三个 / F9 已全树 grep 确认没有第四个」**；第四个 = `TutorialScript.PlaceTroops` | `Core/RuleCore.cs`（口径 + 断言） | **能**（与 `A1975` 同批） | 与 `A1975` 同一件 |
| `A1994` | `E2` 之后仍留红的几支要人工判 | ✅ **可关账** | `交接_第三十一会话.md` §六·四：**35 行 = 34 行误报 + 0 行确认缺口 + 2 悬案**；(a) `Unstable` 的 `addTrait fast` = **`A1916` 同一格**；(b) 4 行快照过期 | 无（(a) 并入 `A1916`） | 否 | `A1916` |
| `A2000` | `TL16` 疑似真缺陷（回牌库没生效） | 🔴 **？待钉死** | `A1965_电池步设计.md:127`：两发 `rc=0`、`raw == ctrl == 26` ⇒「事件到了却没做」 | 先 `工具/ruleprobe` 诊断 → 可能 `Core/RuleCore.cs` | **能**（先钉死是不是真缺陷） | 与 `A2001` 同族（「够到了却没响」） |
| `A1979` | 「逐卡真打」39 块没有施工图 | ✗ | `待办判据` §A1979；`资料/可并行任务清单.md` 无对应条目 | `资料/可并行任务清单.md` + 一份施工图 | **能**（文档；与 `A1965` 是两条不同判据的线） | 与 `A1965` 分工不同 |
| `A1978` | 对账口径两处未验前提 | ◐ | ① **已由 `A1914` 坐实**（`SetBaseCardTraits.c:37` 资产↔运行期忠实同步）⇒ 可销；② `Talent:` 的 `create` op vs 原版 `chosenAbility 100 + unitsToSummon` **仍未验** | `工具/_a1759_report.py:26-28`（`aa.get()` 的 None） + 核对 | **能**（②） | `A1914` / `A1915` |

---

## §三 C 类 · 11 种效果码 / 88 条实例

> **计数口径（已复算，与 `R_引擎效果覆盖率.md` 逐项吻合）**：只数 **`cardAbilities[].abilityLogic[].chosenAbility`**、分母 = 池内 1127 张。
> ⚠️ 若把 `activeAbility` / `alternativeAbility` 也算进来，总数会变成 95 条（`followAfterResolution` +2 · `followIfPrev` +2 · `setCardDeckToTop` +2 · `scriptedAttack` +5）—— 引用时可注明口径。
> 🆕 **真正要复刻的是 65 张卡**（88 条实例里有 23 条是同卡多份：`SW57` ×6 · `BL55` ×4 · `AM58` ×2 等）。

| 码 | 名称 | 实例 | 独立卡 | 我们有没有专用动词 | 判据 | 落点文件 | 可做？ |
|---|---|---|---|---|---|---|---|
| 3 | `doOnlyVfx` | 10 | 10 | ✗ | `AbilityEffect.cs:4`；2026 池 10 张（`SAU46`/`SAU47`/`SW49`/`GOF90`/`SAU55`…） | `CardPresentation/Battle/BattleDriver.cs`（VFX 表） | ↔️ **纯表现 → 特效线** |
| 250 | `autoPlayFromHand` | 13 | **4**（`SW57`×6 · `BL55`×4 · `AM58`×2 · `GOF93`） | ✗（最接近 `deploy` / `RuleCore.PlayCard`） | `abilityLogic_枚举词典.tsv:46`；`AbilityEffect.cs:50` | `Core/EffectResolver.cs`（新动词）+ `Core/RuleCore.cs PlayCard` | 🟢 能（先读 `PlayAbility.c` 的 0xFA 支，判「免费打出 vs 只部署」） |
| 320 | `followAfterResolution` | 24 | 24 | ✗ | `AbilityEffect.cs:58`；实例 24 张（UM64/GOF104/AM21…） | `Core/EffectResolver.cs`（结算队列/收尾） | 🟢 能（先读 `PlayAbility` 的 follow 段；与 `followIfPrev` 同一机制） |
| 325 | `followIfPrev` | 1 | 1（`ASH73`） | ✗ | `AbilityEffect.cs:59` | 同上 | 🟢 能（与 320 一起做） |
| 270 | `createCardInDeck` | 7 | 7（`DA1/DA2/DA3/DA44/DA72/DA75/GSC55`） | ◐ `create` 支持 `hand/enemyhand/decktop`，**不支持「洗牌进牌库」** | `AbilityEffect.cs:52` | `Core/EffectResolver.cs DoCreate:2649-2703` | 🟢 能（加 `dest="deck"`；与 A1986 的桥同批） |
| 420 | `setCardDeckToTop` | 5 | 5（`DA11/DA16/DA40/DA72`·Master Chronomancer） | ◐ `DoChooseCard` 有 `decktop` 动作；**非选区路径没有动词** | `AbilityEffect.cs:72` | `Core/EffectResolver.cs` | 🟢 能（提一个共用动词） |
| 380 | `setReferenceCard` | 3 | 3（`GOF104/AM54/GOF93`） | ✗ | `AbilityEffect.cs:67` | `Core/EffectResolver.cs` + `Core/BattleContext` 指代槽 | 🟢 能（与 `A2009` 同一指代槽，同批做） |
| 470 | `showCardsWindow` | 1 | 1（`GSC55`） | ✗ | `AbilityEffect.cs:83` | `CardPresentation/Battle/BattleDriver.cs`（面板） | ↔️ **纯 UI → 界面/战斗表现线** |
| 480 | `highlightBloodThirst` | 1 | 1（`SW34 Murderfang`） | ✗ | 2026 新增枚举值 | `BattleDriver` 的 VFX/高亮 | ↔️ **纯表现 → 特效线** |
| 235-238 | `scriptedSelfAttack` / `scriptedActingCardAttack` / `scriptedTargetCardAttackDontCount` / `scriptedSelfCardAttackDontCount` | 9 | 8（`GOF93/GOF86/SAU33/EC46/SW9/EC12/EC22/SAU70`） | ◐ 只有 `forceattack` 近似（不区分「不记攻击次数/打谁」） | `AbilityEffect.cs:45-48` | `Core/EffectResolver.cs`（4 个 scripted 变体） | 🟢 能（先读原版 `scriptedAttack` 支，补齐 4 种语义） |
| 260 | `triggerEffectInCard` | 14 | 14（`GOF104/GOF56/GOF31/TL49/BL49/ASH46/UM82/TL50/SW56/GOF41/GOF90/ASH43/DA39/SAU52`） | ◐ 只有 `triggerability` 近似 | `AbilityEffect.cs:51` | `Core/EffectResolver.cs DoTriggerAbility` | 🟢 能（先分辨「触发卡内效果」vs「触发关键词能力」） |

**小结**：88 条 = **3 种 / 12 条纯表现**（3·470·480）+ **8 种 / 76 条引擎侧**，落在 **65 张卡**上；其中 `followAfterResolution`（24 张）与 `triggerEffectInCard`（14 张）两家就占 **38 张**，是第一优先。

---

## §四 63 张「执行层未归因」

### 4·1 现状与判据

- **来源**：`_tmp_view/willrun_mechanism.md`（2026-09-16；**`.gitignore` 里、随时会丢** —— 下表是本报告抄存的完整名单）。
- **判据**：`EffectText.WillRunOps` 为空、且**不是**「本来没正文」那一档 ⇒ 63 张（unit 37 + hero 26）。它们**不进 `MechanismFails`**（`BattleDriver.cs:14349`）⇒ 卡面不打 `*`，是静默盲区。
- **已抽查 11 张**：结论 = **11/11 在主解析器有 op、且都落到其它消费点**（`R_引擎效果覆盖率.md` §7·1）。⚠️ **那 11 张的名单没有落盘**（搜过 `R_引擎效果覆盖率.md`、`交接_第五十三会话.md`、`普查产出_第三十三会话/R4`/`R6`、`普查产出_第二十六~三十会话`，都没有逐张名单）—— 只剩这条结论可复用。
- **现池对齐**：63 个 id 与卡名在 `cards_engine.json` 现读**逐一仍对得上**（K06 的 id 迁移没有动到它们）。

### 4·2 可批量脚本化的分类（先跑这一步，再人工）

| 类别 | 张数 | 判据/消费层（引擎公开面） | 怎么做 |
|---|---|---|---|
| `Talent`（只有 `Talent: X`） | **24** | `CardDef.TalentName` + `RuleCore.SpawnTalents` | 批量断言：63 张里凡 `desc` 含 `Talent:` 的，`TalentName` 必须非空 |
| `Talent+kw`（Talent + 关键词起头） | **16** | 同上 + 关键词标记 | 同上；kw 只影响标记层、不应进 `WillRunOps` |
| `AtTurn`（`At the start/end of … turn`） | **7** | `EffectText.AtTurnClauses` + `RuleCore.ResolveAtTurn` | 批量断言 `AtTurnClauses` 非空 |
| `AtTurn+kw` | **7** | 同上 | 同上 |
| `Talent+AtTurn` | **2**（`BL67`/`SAU36`） | 两条都要有 | 两条断言分别跑 |
| `Talent+AtTurn+kw` | **1**（`SAU20`） | 同上 | 同上 |
| `Talent+costless` | **1**（`AM15`） | `CardDef.CostWhens` | 断言 `CostWhens` 非空 |
| `kw+costless` | **1**（`BL27`） | `CostWhens`/Aura | 需人工确认是「你的战术卡减费」哪一层 |
| `Talent+kw+costless` | **1**（`TAU32`） | 两条都要有 | 分别断言 |
| `costless` | **1**（`TAU20 Piranha`） | `CardDef.CostWhens` | 断言 |
| `kw` | **1**（`TL70 Norn Emissary`：每次 Synapse 触发降费 2） | `CostWhens` / Synapse 触发 | 需人工（⚠️ 可能是「每次触发都降」的累积实现） |
| `Talent+static` | **1**（`UM100`：誓约能力额外生效一次） | `StaticBattleRules` | 断言 |

> 先把上表的**批量断言**跑一遍（离线、只读 `CardDef` 构造 + `RuleProbe` 口径）：凡「分类说的消费层为空」的卡 → 才是真正要人工判的残数。**预期残数远小于 63**（含 `Talent:` 的一族共 **46 张**，很可能一次全销）。

### 4·3 逐张名单（63）

| id | 卡名 | 类型 | 自动分类 | 可能的消费层 |
|---|---|---|---|---|
| `ASH19` | Witchblade Warlock | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `ASH29` | Wave Serpent | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `ASH1` | Anvirr Keltoc | hero | Talent | TalentName/SpawnTalents |
| `ASH2` | Eliac Zephyrblade | hero | Talent | TalentName/SpawnTalents |
| `ASH3` | Medreyal Ghaelyn | hero | Talent | TalentName/SpawnTalents |
| `ASH76` | Lhykhis | hero | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `AM15` | Minka Lesk | unit | Talent+costless | TalentName/SpawnTalents / CostWhens |
| `AM18` | Master of Ordnance | unit | AtTurn | ResolveAtTurn |
| `AM27` | Nork Deddog | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `AM67` | Aquilon Servo-Sentry | unit | AtTurn | ResolveAtTurn |
| `BL34` | Abaddon the Despoiler | hero | Talent | TalentName/SpawnTalents |
| `BL67` | Ghallaron's Champion | unit | Talent+AtTurn | TalentName/SpawnTalents / ResolveAtTurn |
| `BL22` | Master of Possession | unit | Talent | TalentName/SpawnTalents |
| `BL23` | Sorcerer | unit | Talent | TalentName/SpawnTalents |
| `BL27` | Venomcrawler | unit | kw+costless | CostWhens / 单元关键词 |
| `BL36` | Sylar Hexscorn | hero | Talent | TalentName/SpawnTalents |
| `DA31` | Ezekiel | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `EC17` | Konstrictus Tormentor | unit | AtTurn | ResolveAtTurn |
| `GSC3` | Iconward Malak Vorenth | hero | Talent | TalentName/SpawnTalents |
| `GSC66` | Lhaska Szenari | hero | Talent | TalentName/SpawnTalents |
| `GSC2` | Magus Uthrel Naas | hero | Talent | TalentName/SpawnTalents |
| `GSC1` | Primus Saffa Rhiannor | hero | Talent | TalentName/SpawnTalents |
| `SAU20` | Chronomancer | unit | Talent+AtTurn+kw | 三条都要 |
| `SAU29` | Necron Lord | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `SAU24` | Command Barge | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `SAU35` | Imotekh the Stormlord | hero | Talent | TalentName/SpawnTalents |
| `SAU36` | Nemesor Zahndrekh | hero | Talent+AtTurn | TalentName/SpawnTalents / ResolveAtTurn |
| `GOF28` | Ghazghkull Thraka | hero | Talent | TalentName/SpawnTalents |
| `GOF90` | Wurrboy | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `GOF76` | Mozrog Skragbad | hero | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `GOF82` | Beast Snagga Nob | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `GOF86` | Unleashed Tramplasquig | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `GOF92` | Weirdboy | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `GOF23` | 'Ardshell Gurk | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `GOF27` | Boss Zagstruk | hero | Talent | TalentName/SpawnTalents |
| `GOF75` | Warboss Gordrang | hero | Talent | TalentName/SpawnTalents |
| `SOR70` | Sororitas Rhino | unit | AtTurn | ResolveAtTurn |
| `SW3` | Ragnar Blackmane | hero | Talent | TalentName/SpawnTalents |
| `TAU36` | Riptide Battlesuit | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TAU21` | Stealth Battlesuit | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TAU38` | Stormsurge | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TAU6` | Gun Drone | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `TAU7` | Marker Drone | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `TAU10` | Stealth Drone | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `TAU32` | Aun'Shi | unit | Talent+kw+costless | 三条都要 |
| `TAU13` | DS8 Support Turret | unit | AtTurn | ResolveAtTurn |
| `TAU20` | Piranha | unit | costless | CostWhens |
| `TAU2` | Aun'Va | hero | Talent | TalentName/SpawnTalents |
| `TAU1` | Shadowsun | hero | Talent | TalentName/SpawnTalents |
| `TL6` | Biovore | unit | AtTurn | ResolveAtTurn |
| `TL8` | Genestealer | unit | AtTurn+kw | ResolveAtTurn / 单元关键词 |
| `TL37` | Spore Mine | unit | AtTurn | ResolveAtTurn |
| `TL25` | Hive Tyrant | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TL70` | Norn Emissary | unit | kw | CostWhens（Synapse 累积降费） |
| `TL23` | Tyrannocyte | unit | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TL36` | Neurothrope | hero | Talent+kw | TalentName/SpawnTalents / 单元关键词 |
| `TL35` | Swarmlord | hero | Talent | TalentName/SpawnTalents |
| `TL34` | Tervigon | hero | Talent | TalentName/SpawnTalents |
| `UM76` | Lieutenant Titus | hero | Talent | TalentName/SpawnTalents |
| `UM100` | Chaplain Cassius | unit | Talent+static | TalentName/SpawnTalents / StaticBattleRules |
| `UM46` | Marneus Calgar | hero | Talent | TalentName/SpawnTalents |
| `UM47` | Uriel Ventris | hero | Talent | TalentName/SpawnTalents |
| `UM48` | Varro Tigurius | hero | Talent | TalentName/SpawnTalents |

---

## §五 建议分波（按文件所有权 + 依赖）

> 原则（`CLAUDE.md` 铁律 13·3）：**一个文件同一时刻只有一个写手**；同文件的账排在一起；判据未定的不派。

### 5·0 跨线重复 / 归属（先看这张）

| 账 | 归属判断 | 依据 |
|---|---|---|
| `A2017②`（易伤高亮） | ↔️ **战斗表现线** | T01 明写「属表现层」；与 `R_战斗表现链路对拍.md` 的偏差①（状态可见时刻）同源 |
| `A828`（粒子订阅者断言 / 35 def 报错） | ↔️ **特效线** | 落点在 AnimFX / 粒子，不在 `RuleEngine` |
| `A1044`（`A1003` 收口三件） | ↔️ **界面线** | 全是 `Editor/ShellScene` / `MainMenuScene` / `DeckScene` 的 quad 并集副本 |
| `A1649`（damaged 广播次序） | ✅ **引擎线**（与战斗线同源但不同件） | 事件发出次序在 `RuleCore.Hurt`；战斗线的「结算领先表现」是消费侧，**改引擎这条会同时动表现** |
| `A1896`（逐张摘除多一处） | ✅ **引擎线**（调用点相邻卡组线） | 判据是 `DeckEditingWindow__TryOpen.c:47`；落点在 `DeckEditorState`/`DeckBuilder` |
| `A1959` / `A1960` / `A1961` | ✅ **引擎线**（面板**版式**属界面线） | 本线只管「开不开 / 摆几张 / 什么顺序」；「面板长什么样」在界面线 |
| `A1986` / `A1979` / `A1978` / `A2000` | ✅ **引擎线**（工具/仪器件） | 判据是引擎效果/对账口径；改动只在 `工具/**` 与报告，**可与结算核心并行** |

### 波 0 · 关账/文档（0 行为改动，可与其他波并行）
- **关账**：`A1875` `A1914` `A2014` `A2015` `A2010` `A1335` `A1688` `A1958` `A1819`(Draw/AmbushExit 半) `A1650` `A1590` `A1822` `A1573` `A1826` `A1666` `A1569` `A1155` `A179⑤` `A1664` `A1994`。
- **文档/一行注释**（同属 `Core/CardDef.cs` / `Core/RuleCore.cs`，可并进波 1）：T01 的 `DoubleRage` doc · `A1155③` 的 `RuleCore.cs:2091` · `A1985` 的 `BattleEvent.cs:181` · `A2055` 的 `CreatePool.cs:267-269` · `A1577` 的 `ICardProvider.cs:15`。
- **文档-only**（与代码零冲突）：`A1881`（枚举词典）· `A1979`（39 块施工图）· `A1944`/`A1955`（判据链订正，先判边界）。

### 波 1 · `RuleEngine/Core` 小改（一个写手，按文件排）
同一个写手顺序做（都在 `RuleCore.cs` / `EffectResolver.cs` / `EffectText.cs` / `UnitState.cs`）：
1. `A1426`（`EffectResolver.DoGive`）
2. `A1692`（`RuleCore.DeployFree`）
3. T01 §六·6（`UnitState` 两处护甲）
4. `A1560`（`CardDef.AddTriggerOp`）
5. `A1312`（`RuleCore.ApplyDeployTurnState` 两位重算）
6. `A1690`（`EffectText.FillAdjacentAnchors` + `CardDef.evHasTarget` + 夹具）
7. `A1649`（`RuleCore.Hurt` 次序；**先只读盘 `EvtKind.Hit` 消费面**，⚠️ 跨面）
8. `A1571`（先 `play BL55` 诊断；若真是 `autoPlayFromHand` 语义，落 `EffectResolver` 新动词）

### 波 2 · T05 选择面板（生成器 + 数据 + 表现）
- `A1959`：`工具/gen_cards_engine.py` → `cards_engine.json` → `Core/CardDef.cs` → `BattleDriver.ChooseShowByCardId`（把 6 张手工表换成逐卡数据）。
- `A1960`：`Core/EffectText.cs` + `Core/CreatePool.cs` + `BattleDriver`（开面板）。
- `A1961`：**先只读 VA 解 `CompareChoiceOptions`**（解不出就不动，⛔ 不发明排序）。
- ⚠️ 三个都碰 `BattleDriver.cs` ⇒ **串行一个写手**；`A1959` 与 `A1960` 可在同一批。

### 波 3 · 附魔层（大件，前置链）
- `A1567` 建层（`CardInstance` / `BattleEvent` / `RuleCore` / `EffectResolver`，五入口）→ 然后 `A1258`（①通道）→ `A1983`（状态闸）→ `A1819 SecretOrder`（发出点）。**这四笔有严格依赖，不能拆开并行。**

### 波 4 · 探针/量具（`工具/ruleprobe/**`，与波 1-3 零文件冲突，可并行）
`A1965`（电池新增 6 步）· `A1941` · `A1598` · `A1568②` · `A1561` · `A1952` · `A1681` · `A1594`。

### 波 5 · 对账仪器与逐卡裁定（工具 + 文档）
`A1986`（补桥）→ `A1978②` → `A1759`/`A1772`（action-aware 重跑）→ `A2009`（逐支真打）→ `A1571`（若波 1 未做）→ `DA1`（等裁定）。

### 波 6 · AI（等一句口径）
`A1797`：先裁「620 张没有逐卡评分的卡怎么办」；`A1312` 的 `keepEffects` 实例等远端/二进制数据（其余半可做）。

### 波 7 · 教程入口（同文件，一起做）
`A1975` + `A1990`（`Core/TutorialScript.cs` + `RuleCore.cs` 的入口口径与断言）。

### 波 8 · 转线
- `A2017②` → 战斗表现线（与「10 条表现偏差」同批）。
- `A828` → 特效线。
- `A1044` → 界面线。

---

## §六 没查清 / 搜过什么（⛔ 不猜）

1. **`A1571` 没有跑 `ruleprobe`**（本轮红线：不跑构建/Unity）。静态核到：`CreatePool.MatchKindWord`/`KindWords` 把 `troop → type=unit`，`DeployFrom="deck"` 也有解析（`EffectText.cs:7399-7402`）⇒ 逻辑上应能筛到；「一张都没筛到」**可能是探针夹具牌库为空**，也可能是 `autoPlayFromHand` 语义差异。**必须 `play BL55` 复现后才能定性。**
2. **`A1916`**：`AbilityLogic__PlayAbility.c` 消费 `buffEffect` 的那一段**没读**（本轮只读了任务文件给的枚举语义）。
3. **`A1961`**：`CompareChoiceOptions` 的比较函数**没解**（需要 VA 反汇编；本轮没做）。
4. **`A1896`**：`DeckEditingWindow__TryOpen.c` 的逐张摘除段**没亲读**，只用了账上的结论。
5. **`A1412` / `A1896` / `DA1`**：三笔都要人拍板（`A1346` 一格 / 两个调用点 / 隐藏能力 vs 数据残留）。
6. **11 张已抽查的「未归因」卡名单**：`R_引擎效果覆盖率.md` 只记了「11/11 落到其它消费点」，**没有落名单**；搜过该报告、`交接_第五十三会话.md`、`R4_资料目录盘点与任务结构.md`、`R6_任务逐条分类搬迁表.md`、第二十六~三十会话目录，均无逐张记录。
7. **`A1651`**：全仓 `HandIdx` 现读只命中 `TutorialScript` 与 `BattleDriver` 的教程动作；**没有找到「写死第一次算的 HandIdx」的缓存点**，所以「现状判不出」。
8. **`A828` / `A1044`**：只读了账文与落点文件名，**没读对应代码**（两笔都不属本线，建议转线时由接收线现核）。
9. **63 张名单的时效**：名单来自 2026-09-16 的探针读数，本轮**没有重跑 `willrun`**（红线）；只核了「63 个 id 与卡名在现池仍一一对应」。
10. **C 类 88 条的 `activeAbility`/`alternativeAbility` 口径**：本轮复算时发现另有 7 条落在 `activeAbility`/`alternativeAbility`（总数 95）。`R_引擎效果覆盖率.md` 的 88 用的是 `cardAbilities` 口径；**引用时要说清**。
11. **`A1975`/`A1990`**：本轮只核到「`Auras.Recompose` 已补在调用点（`TutorialScript.cs:1269`）」，另外四个钩子（trait / `ResolveDeploy` / `ResolveFaithAbility` / 攻型两位）**未补**；`RuleCore.cs:2137-2140` 的「三个入口」口径**仍在**。
12. **`A2000`**：只在 `A1965_电池步设计.md` 里读到「疑似真缺陷」；**没有跑 `play TL16`**，未定性。
13. **未跑任何自检/类型检查/探针**（本轮是只读盘点）；所有「已落」结论都来自代码现读，最终仍需收口时由 `RuleEngineTest.Run` 统一验收（现行口径）。
