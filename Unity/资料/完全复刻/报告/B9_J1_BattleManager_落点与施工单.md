# B9_J1 —— BattleManager 落点补齐与施工单（2026-10-26 · 第八十三会话）

## ① 结论

| 项 | 数 |
|---|---|
| 本代理处理范围 | `定位表` 里 `定位结论 = 已实现` 的 **154** 条中，落在本次 493 条「B 且无落点」切片里的 **145** 条（另 9 条不在切片里，未动） |
| **升 A** | **14** |
| **改 C** | **0**（每条都找到了具体的我方实现，见 §二） |
| 保持 B、但补上真实落点 | **131** |
| 施工单 | **319 行** = B8_BG 指定先做的 6 条 + `部分实现 279` + `未实现 34` |
| 施工单缺口分级 | **P0 = 54 · P1 = 176 · P2 = 89** |

**产物**
- 账补丁：`资料/完全复刻/账/_parts/B9_J1.tsv`（145 行，11 列，只写真改过的行）
- 施工单：`资料/完全复刻/判据/B9_BattleManager_施工单.tsv`（319 行 × 9 列）
- 本报告：`资料/完全复刻/报告/B9_J1_BattleManager_落点与施工单.md`

**两条机检都跑过（全绿）**
- `python 工具/check_ledger_points.py 资料/完全复刻/账/_parts/B9_J1.tsv` ⇒ `A=14 通过=14 漂移=0 告警=0 失败=0`
- 自写校验（145 条**全部**，含 B 行）：逐条按二进制读源码，比 `decl_name_at(第 N 行) == 落点括号里的名字` ⇒ `bad 0`（唯一一次错是 `673` 写成了 `BattleDriver.cs:871`，已改成 `CardInteraction.cs:871 PickUp`）
- `merge_ledger_parts.py --dry` ⇒ **更新 145 · 新增 0 · 无变化 0**（键 `(原版类, 方法)` 全部唯一命中）

**升 A 的 14 条**（都能同时给出「判据侧原版语义」与「我方唯一落点」）：

`BattleManager.IsCampaignLike`(2119) · `IsTutorialMatch`(2128) · `SortByCostAndName`(BattleDriver:10214 CompareChoiceOptions) · `CardScript.GetTalentCard`(TalentCardData.cs:78 Resolve) · `MatchData.GetMatchType`(BattleContext:2084 For) · `EntityScript.DamageKillsTarget`(CounterAttack:143) · `GetCombatDamageWithType`(CounterAttack:129 CombatDamage) · `GetActiveAbilityDamage`(RuleCore:8081) · `HasEnchantment(RawCardScript)`(Enchantment:302) · `HasEnchantmentWhileInPlay`(Enchantment:464) · `HasSecretOrder`(Enchantment:547) · `HasSecretOrders`(Enchantment:500) · `IsSpentEnchantment`(Enchantment:357 AllSpent) · `SpendEnchantment`(Enchantment:335)

## ② 逐条证据（只列 C 与疑点）

**C：0 条。** 145 条全部找到具体实现（无一条是「该做没做」）。以下是**判成 B 的理由 / 发现的疑点**（都已写进账的「我们的落点」「差异」「备注」）：

1. **共用落点 ⇒ 只能 B**（同一 `文件:行` 被多个不同原版方法占用），实际命中的组——
   `EffectResolver:3885 DoCreate`(157/158/555/556) · `RuleCore:12882 CheckCodex`(188/272/604) ·
   `RuleCore:7892 RevealSecretOrders`(171/589/1195) · `EffectResolver:6226/6284`(176/177/591/592) ·
   `EffectResolver:3021 DoForceAttack`(150/570/571) · `EffectResolver:7713`(148/569) ·
   `EffectResolver:4451`(277/374/376) · `EffectResolver:4604`(275/276) ·
   `RuleCore:11055 CanUseAbilityTarget`(469/470/475/478) · `BattleDriver:15144`(253/662/683) ·
   `BattleDriver:8202 OnReplayButtonClicked`(283–286) · `PlayerState` 集合字段 23 条 · `EffectResolver:11718`(79/541) ·
   `SimpleAI:786 ValueInPlay`(428，与 AI 域 `AI.GetCardValueInPlay` 抢同一行) · `RuleCore:11256`(690/691) ·
   `RuleCore:10813 CanStartAbility`(66/514) · `RuleCore:10844`(254) · `Enchantment:163`(1184–1196 里落类级的那几条不计，已逐条另指) · `PrebuiltDecks:39`(1201/1207/1215) · `CardDef:1811 TriggerOps`(1126/1128) · `UnitState:942 Has`(1180/1183)
2. **落点是属性 getter ⇒ 按规则不升 A**：`245`/`252` → `BattleDriver.cs:11864 CemeteryRowClickAllowed`（原版 `CanShowCemetery` 本身就是个 getter 形状，但我们的规矩是 getter 不当 A 落点）。附带记录：`245 CanDisplayCemeteryInTutorial` 的 `+0x25c` 那道闸我们**没有对应物**。
3. **落点是 const 常量 ⇒ 不升 A**：`417 GetTargettingTime` → `CardFeel.cs:542 EnemyTargetingAnimTime`（值 0.5 s 已实读）。
4. **我方没有同名谓词**：`452 IsClosedDeckGame` → 只能落到 `MatchType.ClosedDeck`（2059）枚举档位本身；调用点是直接比枚举，**没有** `IsClosedDeck(...)` 这个函数。
5. **结构不同（不算「已实现」的等价）**：`144 AddEffect`（原版任意 CardEffect，我们只有附魔味 → `Enchantments.Attach`）· `260 CancelBattleActionFromQueue`（我们无动作队列，只有泵环硬闸的 `Clear`）· `399 GetMulliganCard`（原版取卡，我们收下标）· `1126/1128`（原版**全局 trait 表**，我们**每张卡自带 op 表**）· `1184`（原版按 `AbilityTrigger` 档，我们按**关键词**）· `1195 RevealedSecret`（我们没建「已揭示」这一位，只在揭示时顺手清）· `1201/1207/1215`（原版 `botDecks` 配置表，我们只有预组表）· `649 SetupInitialEnchantments`（`Enchantments.SetupInitial` **恒 false**：无战役关卡数据）
6. **一处旧文本与新落点不符，已改**：`1165 DamageKillsTarget` 原「差异」写的是 RuleCore 预测管线（`DamageAfterReduction`，带易伤/堡垒），而真正的 1:1 实现在 `CounterAttack.cs:143`（`Max(1, dmg − Armor) >= Health`，⛔ 明写不许用 `DamageAfterReduction`）⇒ 差异列已改写为「无（同式）＋两条管线别混」。
7. **A 行的判据要点全部重写过**（原来是模板「（原版 X，判据见 BattleManager.cs 方法体）」），现在写的是「原版做了什么 + 我们在哪几行做同样的事 + 判据文件行号」。

## ③ 没查清的部分

- **判据侧没逐条核是 A 的主要闸门**：BattleManager 那一大批（`判据要点` 只有「（原版 X，判据见 BattleManager.cs 方法体）」这一句模板，约 90 条）→ 我**没有**替它们编「等价」，一律保持 B、只把落点补实。要升 A 得先按 `decomp_full/BattleManager__<方法>.c` 逐条读体。
- **493 条无落点 B 里还有 348 条没碰**：本代理的范围只是「定位表 = 已实现」的 145 条；`部分 279` / `未实现 34` 已进施工单（不修）；`属表现层 131` / `无处可落 12` 未处理（不在本轮切片定义里）。
- **施工单的 P0/P1/P2 是启发式**（按方法名前缀 + 判据要点 + 目标文件；P2 关键词 = 战役/教程/回放/动画/平台/调试…）。**不是逐条人工裁定**，P0 与 P1 的边界会漂；`ReceivePlayerAction` / `AddPlasmaMana` / 两条 `RandomSeedProvider` 是我手工覆写的。
- **全局 trait 表**：`1126/1128` 判「数据模型不同」只读到 `CardDef.TriggerOps` 这一处；原版 `CardTraitCollection` 的全局表在我们这边到底有没有等价数据（`cardTraits` 列 vs 每卡自带 op）**没查全池**。
- **`417 GetTargettingTime`** 是否还有别的消费点（`BattleDriver.cs:18003/22724` 出现过同名注释）没逐一坐实。

## ④ 交接给下一个代理

**保持的规律（下一批直接用）**
1. **落点写法**：`路径:行（裸声明名）（等价实现 = 我们的名字：一句理由）`。第一个括号只放**那一行真的声明名**（机检会按 `decl_name_at` 严格比）；**getter / const / `*ForTest` 都不升 A**。
2. **同一 `文件:行` 被两个不同原版方法占用 ⇒ 两个都只能 B**（§二·1 那张列表就是现成的冲突表）。
3. **A 的三件套缺一不可**：判据出处能给到该方法的具体行 + 判据要点写得出原版语义 + 我方落点唯一且是实现行。缺哪条就写 B，并在「差异」写清缺哪条。
4. **落点别照 `定位表` 的行号硬抄**：它给的起始行 **101/145 不是声明行**（在方法体中间）。正确做法 = 用括号里给的**我们的名字**去 `find_decl_candidates` 找声明，再取**离区间最近**的那个。

**施工单怎么用**
- 前 6 行是 B8_BG 指定的开工顺序（`ResolveRevealOrders` → `ResolveFatigue` → `ResolveBacklash` → `ResolveHeal` → `ResolveDiscardFromHand` → `ResolveCreateDeckCard`），**都不大，先做**。
- ⛔ 别把 `ResolveAttack`（1653 行）/ `ResolvePlayCardFromHand`（1549 行）排进第一批。
- **写手分布（瓶颈）**：`RuleCore.cs 74` · `BattleDriver.cs 68` · `EffectResolver.cs 54` · `NetBattle.cs 29` · `TutorialScript.cs 28` · `BattleContext.cs 13` · `CardDef.cs 9`。**P0 里 RuleCore 29 / EffectResolver 21** ⇒ 派活时 RuleCore 与 EffectResolver 各只能有**一个**写手，其余按文件切。
- `我们的落点` 有 **26 条是空的**（原版方法我们全仓 0 命中，全是 `未实现`）——这些要**先读判据**再决定落哪个文件，⛔ 不是「照单加一个空壳」。

**合并纪律（别踩）**
- 合并前先 `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --dry`；真合并**必须带 `--only B9_J1`**；合完把 `B9_J1.tsv` 移进 `账/_parts/_merged_B9_J1/`（否则下次整目录合并会把后来的手工修订盖回旧内容）。
- 合并/`--fix` 只能在「**没有 `.cs` 写手 + 没有其它账写手**」时串行做（铁律 16·8）。

**本代理没做的事**：没跑 Unity、没动 git、没改主表/正本、没改任何 `.cs`、没碰别的代理的 `_parts/B9_E*`。
