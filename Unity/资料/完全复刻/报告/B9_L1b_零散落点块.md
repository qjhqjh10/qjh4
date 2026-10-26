# B9_L1b —— 「X-Resolve共享落点」12 条 +「X-字段落点-引擎」26 条（2026-10-26）

## 一、结论

| 项 | 条数 |
|---|---|
| 升 **A** | **12**（12 条共享落点 + 570/571 按调度台裁定另升） |
| 留 **B** | **26** |
| 判 **C** | **0** |
| 小计 | 38 |

补丁：`账/_parts/B9_L1b.tsv` —— **19 行**（= 14 条 A：12 条共享落点 + `ResolveForceAttack` / `ResolveForceAttackFriendly`；**5 条 B 是订正**：334 / 335 / 340 / 348 / 369）。

落点闸（只读）：`check_ledger_points.py 账/_parts/B9_L1b.tsv` ⇒ **A=14 通过=14 漂移=0 告警=0 失败=0**。

## 二、升 A 的 12 条共享落点（A 的例外条款：多个原版方法由同一段代码实现）

| 账行 | 原版方法 | 落点（声明 + 范围） | 这段代码如何分别满足 |
|---|---|---|---|
| L498 | `ResolveAttackDamage` | `RuleCore.cs:6207` DeclareAttackCore · 主伤害段 :6819-6870（`Hurt` :6834） | 算调整后伤害 + 广播；扣血在 `ApplyDamageCore`（与原版「扣血在 ReceiveDamage」分工同） |
| L499 | `ResolveBacklash` | `RuleCore.cs:9280` CleanupDeaths · 残骸支 :9420-9470 · 死亡触发/反噬入队 :9540-9600 | 6 段：残骸转换 / 死讯广播 / 反噬入队 / 重排 / 击杀记账 |
| L500 | `ResolveCardAutoSummoned` | `RuleCore.cs:4715` DeployFreeCore（CardInstance 重载；CardDef 重载 :4917） | 5 跳：落点选择 / 召唤病 / 进场触发 / 光环复制 |
| L507 | `ResolveDestroyUnit` | `RuleCore.cs:9280` CleanupDeaths :9280-9840 + MinionDeathBookkeepingDeferred :9843-9928 | 反噬触发 + 收尾；离场/墓地/重组一并在这里做 |
| L511 | `ResolveDrawCard` | `RuleCore.cs:3042` MoveDeckTopToHand :3042-3090 | 牌库顶→手牌 + 上限/墓地/抽牌触发 |
| L512 | `ResolveEndTurn` | `RuleCore.cs:2276` EndTurnCore :2276-2740 | 狂暴强制(:33-137) / 限时增益撤销 / 标记光留到回合末 / TurnEnd 触发 |
| L520 | `ResolveHeal` | `RuleCore.cs:9035` HealUnit :9035-9077 | IsInPlay 闸 / ≤0 不出血 / corrupt 不治 / clamp 上限 / 实际量>0 才发事件 |
| L525 | `ResolveMultiDamage` | `RuleCore.cs:8250` ApplyDamageCore :8250-8680 | 广播在扣血之前（同 :141→:154） |
| L526 | `ResolveMultiDamageAndDestroySelf` | `RuleCore.cs:8250` ApplyDamageCore + `DoDeal` 的 destroyedSelf 支 | 伤害之后自毁（与原版同序） |
| L544 | `ResolveTraitAdjacentDamage` | `RuleCore.cs:6207` DeclareAttackCore · blast 段 :6877-6895（`Hurt(kind=Blast)` :6884） | 逐相邻格伤害 |
| L545 | `ResolveTraitStompDamage` | `RuleCore.cs:6207` DeclareAttackCore · stomp 段 :6905-6930（`Hurt(excess)` :6921） | 溢出量溅射（`dealt−hpBefore` ⇔ 原版 `abs(currentHealth)`） |
| L547 | `ResolveTransformIntoRemnant` | `RuleCore.cs:9280` CleanupDeaths · 残骸支 :9420-9470 | 新建残骸：血=1 / 不跑反噬·不稳定·死亡监听 / 保留 InstanceId |

> 三条在 `DeclareAttackCore:6207` 上（498 伤害段 / 544 blast 段 / 545 stomp 段）、三条在 `CleanupDeaths:9280` 上（499 / 507 / 547）—— 每一行都在 `判据要点` 里写清自己那一段。

## 三、留 B 的 26 条（逐条理由）

| 账行 | 类.方法 | 理由 |
|---|---|---|
| L321 | `GetAllCardsInHand` | 原版 = `GetCardsInHandList(minions/spells/whispers 全开)` 的包装（双方向）；我们**没有「双方向手牌合并」的口**（调用点各自 `ctx.Players[0].Hand` + `[1].Hand`）⇒ 无方法级实现 |
| L322 | `GetAllCardsInPlay` | 原版重建**双方向** `allCardsInPlay`，且带 `includeDying` 一档；我们只有 `PlayerState.Units()`（单方向·现算·`!IsWarlord`），**没有 includeDying 那一档** |
| L334 | `GetCardsDeckPlayer` | 🔴 **订正**：原版签名是 `private unsafe string` —— 它是**调试/诊断字符串构造器**，不是取牌库列表；原账等价成 `PlayerState.Deck` 是错误等价。我们侧无对应物（已进补丁） |
| L335 | `GetCardsHandPlayer` | 🔴 **订正**：原版 `GetCardsHandPlayer` 同样返回 `string`（拼一段手牌摘要），不是取列表；错误等价已在补丁里改掉 |
| L336 | `GetCardsInCemetery` | 原版 6 档 `incl*`（minions/spells/whispers/weapons/player/enemy）过滤；我们 `Discard` 是裸列表、过滤在读方 ⇒ 访问器本身无方法级实现 |
| L337 | `GetCardsInDeck` | 原版 5 档 `incl*`（含 player/enemy 双方向）过滤；我们 `Deck` 是裸列表 ⇒ 同 336 |
| L338 | `GetCardsInHandList` | 原版 6 档 `incl*` + 双方向过滤；我们 `Hand` 是裸列表 ⇒ 同 336 |
| L339 | `GetCardsInHandRef` | 原版返回**列表引用**（供调用方就地改）；我们 `.Hand` 是 `public readonly List` —— 能力等价但**没有方法**（字段不能当落点） |
| L340 | `GetCardsPlayPlayer` | 🔴 **订正**：原版 `GetCardsPlayPlayer` 是 `private unsafe string`（约 225 行，拼「本回合打出的卡」文本），**不是**场上那一排；原账等价成 `PlayerState.Board` 是错误等价（已进补丁） |
| L347 | `GetDeck` | 原版返回某方牌库 `List<CardScript>`；我们 `PlayerState.Deck` 字段（无方法） |
| L348 | `GetDeckManager` | 原版返回 `DeckManager` 这个**类**（牌库管理器：卡背/位置/翻牌）；我们**没有这一层**（已进补丁） |
| L349 | `GetEnemyHandRef` | 同 339（敌方手牌引用） |
| L350 | `GetEnemyMinionsAndRemnantInPlay` | 原版重建**维护型缓存表** `enemyMinionsInPlay`（side + `cardType != Hero` 过滤）；我们 `Units()` 是现算枚举、且不过滤「将死未清」⇒ 结算中途内容可能不同 |
| L353 | `GetFriendlyHandRef` | 同 339（友方手牌引用） |
| L355 | `GetHero` | 原版返回 `playerManager.heroCard`；我们 `PlayerState.Warlord` 字段（无方法） |
| L366 | `GetOriginalWarlord` | 原版从 `MatchData.playerRawBattleData` 取**开局那份督军 RawCardScript**；我们**不保留开局快照**（`Warlord.Card` 是卡模板，语义不同） |
| L369 | `GetPlayerHand` | 原版返回 `PlayerHand` 这个**类**（手牌管理器：上限/摆位/换牌面板）；我们**没有该类型**（已进补丁） |
| L371 | `GetPlayerMinionsAndRemnantInPlay` | 同 350（我方一侧） |
| L382 | `GetTotalMana` | 原版转发 `PlayerManager.GetTotalMana`；我们 `MaxEnergy` 字段（无方法） |
| L388 | `GetUnitsInPlayRef` | 原版直接返回内部维护表 `unitsInPlay` 的引用；我们**没有这张维护表**（棋盘是 `UnitState[]`） |
| L427 | `IsThereDoubleOathEffect` | 原版计数「某侧带 `oathDouble` 的单位数」；我们只有 `CardDef.OathDouble` 字段 + `Aura.cs:693-727` 的授予段，**没有计数方法** |
| L534 | `ResolveRemoveFromCemetery` | 原版 `ResolveRemoveFromCemetery` 只调 `CardScript.RemoveCardFromCemetery`（`.c` 全文 5 行 = 清 `+0x24b` 重入闸）；我们同步结算没有「取出途中」中间态 ⇒ 无对应实现（同 L167 的结论） |
| L674 | `GetManaLeft` | 原版是**方法**（`BoardAnalysis.GetManaLeft` → `BattleManager.Get*Mana` → `PlayerManager+0x4C`）；我们只有 `PlayerState.Energy` **字段**（维护点 = `RuleCore.cs:1732` / `:2689`）⇒ 字段到不了方法级 |
| L675 | `GetTotalMana` | 同 674：原版方法，我们 `PlayerState.MaxEnergy` 字段（AI 消费端 = `AiScoring.cs:719`） |
| L947 | `TriggerOtherCardAbility` | 🔴 落点疑似挂错（`B8_G` 也这样判）：原版这个方法触发的是**另一张卡的 `ThisCardPlayed`**（`abilityCard.OnTriggerEffectInCard(AbilityTrigger.ThisCardPlayed, …)`），不是 `PlayedThirdCardInTurn(15)`；我们的 `OffensiveCardMap` 表桥的是另一件事 ⇒ 需重定位 |
| L1242 | `GetOriginalId` | 原版 `GetOriginalId` 解决「**异画取本体 id**」；我们**异画不建档**（`CardDef.Id` 天然唯一）⇒ 这件事在本地**无对象**。⚠️ `LegacyCardIds` 是两代 id 换号表，**不是**它的等价物 |

## 四、疑点 / 新发现

1. 🔴 **`GetCardsPlayPlayer` / `GetCardsHandPlayer` / `GetCardsDeckPlayer` 都是 `private unsafe string`（调试文本构造器）**，不是取牌/牌的列表 —— 原账把它们等价成 `PlayerState.Board` / `Hand` / `Deck` 是**错误等价**，已在补丁里订正（状态仍 B）。判据：`BattleManager.cs:20075 / 20301 / 20399` 签名 + 体内 `text = ""`（:20166）→ `text = text8`（:20293）。
2. 🔴 **`GetDeckManager` 返回 `DeckManager`、`GetPlayerHand` 返回 `PlayerHand`** —— 都是**类的实例**（管理器组件），我们结构上没有这两个类型（牌库=List、手牌=List+表现层 HandLayout）⇒ 不是「落点没写对」，是**返回值层面不对位**。
3. ⚠️ **`GetAllCardsInPlay(includeDying)` 的 `includeDying` 一档我们没有**（`Units()` 不过滤、也不区分「将死」）；`Get{Player,Enemy}MinionsAndRemnantInPlay` 是**维护型缓存表**（`playerMinionsInPLay` / `enemyMinionsInPlay`，side + `cardType != Hero`），我们的 `Units()` 是**现算枚举** ⇒ 结算中途内容可能不同。
4. ⚠️ **`GetCardsInHandList/Deck/Cemetery` 的 `incl*` 标志里有一维是 `player`/`enemy`（= 双方向）** —— 我们所有集合访问都是**单方向**（`ctx.Players[i].X`），没有任何「双方向合并」的口。

### 4·b `CheckIfCodexTriggered`（L272，调度台点名要写清）

- **原版判据（`.c`，`BattleManager__CheckIfCodexTriggered.c`）**：签名 = `(card param_2, manaLeft param_3, targetCard param_4, shouldCheckCost param_5)`；
  · **闸① `.c:30`** `if (param_3 == 0)` —— **manaLeft（= 打出前的剩余能量）必须为 0**；
  · **闸② `.c:31-36`** `if (shouldCheckCost) { if (get_CurrentCost(card) < 1) return; }` —— **这张打出的卡代价必须 ≥ 1**（0 费卡不触发）；
  · 过闸后 `.c:43-45` 打 `Trigger codex from playing card <name>`，`.c:97` `AddAutoActionToQueue(…, priority = 1)` 排一条 `triggerPlayerCodex`。
- **我们（`RuleCore.cs:12923 CheckCodex`）**：只有闸①（`Energy != 0 ⇒ return`），**没有闸②** ⇒ 今天「0 费卡 + 能量为 0」这一格**我们会触发、原版不触发**。
- **差什么**：`CheckCodex` 的签名没有「刚打出的那张卡」这个形参 ⇒ 要补闸②，得先找到它的调用点（`PlayCardCore` 打出后那一处）把卡传进去；另外原版是**入队**、我们是**同步**（这一层不阻塞，只要闸对）。
- ⚠️ `.cs`（`out_cpp2il_dev_ilspy/BattleManager.cs:45428-45470`）对这两格闸有 `NoteDecompilerIssue`，读起来是乱码形状；**`.c:30-36` 才是干净的判据**。

## 五、没查清的

- 共享落点那 12 条里，`ResolveEndTurn` 原版 `:200-871`（收局 / 动画 / 网络广播，约 400 行）**没逐句核**；`ResolveBacklash` 的 `TriggerUnitDeathActions` 80/81/90 三档细分**没核**。两条都写进了 `备注`。
- `GetAllCardsInPlay` 的 `includeDying` 在原版具体过滤的是哪个状态位（`waitingToDie`?）本轮没读。
