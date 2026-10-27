# B88 · 波 1 · 执行代理 `E2`（引擎线 · `EffectResolver.cs`）报告

> 会话：第八十八会话 · 波 1 · 2026-10-27
> 白名单：`RuleEngine/Core/EffectResolver.cs` ＋ 新建 `RuleEngine/Core/EffectResolverPartial_B88E2.cs`（＋ 它的 `.meta`）
> 账：`账/战斗逻辑驱动.tsv` 里落点含 `EffectResolver.cs` 的 **C 行 22 条** ＋ `账/特效动画.tsv` **1 条**（`B9_S0EFFECT` 新记）＝ **23 条**
> 补丁：`账/_parts/B88_E2.tsv`（**23 行** · 11 列表头照抄主表 · LF）
> ⚠️ **跨补丁重复键 3 条**（`check_patch_conflicts.py` 现读，**状态都相容**，谁赢都一样）：
> `BroadcastCardDrawn`（E1=A / E2=A）· `BroadcastCollectFaith`（E1=A / E2=A）· `BroadcastRemoveFromGame`（E2=C / E4=C）。
> ⇒ 调度台合并时按「文件名排序最后一个赢」即可，**不必手工删行**；只是想留我这版的证据文本，就把 `B88_E2` 放在最后合。
> `typecheck`：**运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_s88e2b`，本代理改动全落完之后跑的那一次）。
> ⚠️ 之后两次复跑各撞上**别的代理正在写的文件**（先 `Shell/TutorialModePopup.cs:775` 的 `CS0102`、后 `Battle/CardDisplayWindow.cs:922/926` 的 `CS0103`「`NonEmptyRowsOf` / `IsEmptyRow` 不存在」）—— **都不是本代理的文件**，按纪律没去改；那两处是 E6 那一批的活。

---

## 一 结论（一句话）

**13 条升 A（其中 5 条是把原版三个「写属性」叶子方法逐句补成原语）· 10 条留 C 并写清反证（阻塞点全在本代理白名单之外的两个文件上）**；
`EffectResolver.cs` 共动 **5 处**（净 **+1 行**），其余全部新成员落在 partial 文件里（⛔ 不往 17.5k 行的主文件插成员，避免账落点成片漂移）。

| 状态 | 条数 | 账行（物理行） |
| --- | ---: | --- |
| **A** | **13** | 494 · 501 · 502 · 531 · 627 · 628 · 631 · 656 · 698 · 711 · 712 · 746 · 特效520 |
| **C（反证）** | **10** | 77 · 172 · 521 · 637 · 644 · 690 · 808 · 1122 · 1287 · 特效521 |

⚠️ **`EffectResolver.cs` 净 +1 行**（`DoReturn` 里那句 `SweepHandEffectsOnRemoval` 是**新增语句**，没法塞进已有行）。
⇒ **调度台在同步点跑 `check_ledger_points --all-A --fix`** 即可把 6710 行以下（含 `DoReturn` 之后的全部落点）的漂移一次修齐。

---

## 二 逐条证据

### A · 本批真的落地了的 13 条

1. **494 `ResolveAddHandEffect`** —— 复核闭合，**不需要改代码**：
   - 旧账「单值过期 vs 五时长分桶」**已不成立**：`CardInstance.HandBuffExpiry`（`Core/CardInstance.cs:317-331`）就是原版那五档（`Never/EndOfTurn/OwnerTurnStart/…`，本仓是**互斥**枚举）；
   - 去重走 `IsSameHandEffect`（`EffectResolver.cs:5721`），第 ④ 步 `a.Expire != b.Expire ⇒ 两条` ⇒ 「同来源同载荷、**不同到期档**」不再被压成一条。
   - 判据：`PlayerHand__AddHandEffect.c:89-139`（先登记后贴牌）。
2. **501 `ResolveChangeBaseAttack` / 711 `ChangeBaseAttack`** —— 新原语 `ChangeBaseAttackFidelity`（`Partial:61`）。判据 `CardScript__ChangeBaseAttack.c:17-56`（倒序摘 `!extrinsic ∧ buffType==1 ∧ melee≠0`）+ `:34-38`（写 `currentBaseMeleeAttack(+0x7c)`）+ `BattleManager__ResolveChangeBaseAttack.c:1-22`（日志 + `FinishResolvingAction(0x13)`）。
3. **502 `ResolveChangeMaxHealth` / 712 `ChangeMaxHealth`** —— `ChangeMaxHealthFidelity`（`Partial:114`）。判据 `CardScript__ChangeMaxHealth.c:33-135`（非 extrinsic 的摘/清零、extrinsic 求和）+ `:43-52`（写 `+0xa4`）+ `:56-70`（`min(旧血, 新上限)`）。
4. **746 `ForceChangeMaxHealth`** —— `ForceChangeMaxHealthFidelity`（`Partial:158`）。判据 `CardScript__ForceChangeMaxHealth.c:10/:16-28/:30-42/:44`（三格同写）。**`+0x24c` 那一格我们没有字段，如实标着**。
5. **531 `ResolveRecallToHand`** —— `DoReturn`（`EffectResolver.cs:6537`）的 `case hand / decktop / deck` 之后补 `SweepHandEffectsOnRemoval(ctx)`（判据 `BattleManager._ResolveRecallToHand_d__542__MoveNext.c:80-82` 的 `PlayerHand.UpdateCardEffects`）。**这一条就是净 +1 行的那一处。**
6. **627 `BroadcastCardDrawn`** —— 残余那格「trigger 250 未实现」**已由 `E6ENG` 第三批落地**，本代理复核：`EmitTrapDrawnInsteadOfCardDrawn`（`EffectResolver.cs:16603`，三态 `ArmedTrap`）+ 只在 hop 0 且 `inst == drawnCard` 时取代 260。
7. **628 `BroadcastCardPlayed`** —— 两格都补：
   - ① `BroadcastWhenCore`（`:9824`）快照后加 `if (kind == Play && subject != null) listeners.RemoveAll(...)` —— 原版「**只排除、不预调**」（`BattleManagerSupport.cs:669-686`），⛔ 没走 `preCalled`；
   - ② `Play` 加进 `BoardJumpIncludesPendingDeath`（`:16291`）—— 原版这跳无 `IsInPlay`。
8. **631 `BroadcastCollectFaith`** —— 复核三处口已同形（`:10370` 的 `PreCallBoardListener` ＋ `:12897`/`:12966`/`RuleCore.cs:1636`），无回退。
9. **656 `BroadcastUnitObtainedMark`** —— 两格都补：`preCallSelf` 加 `GetsDarkPact`（`:10943` 那一行）；`GetsDarkPact` 加进 `BoardJumpIncludesPendingDeath`。判据 `BattleManagerSupport.cs:1848-1890`（预调 + 棋盘排除 + 双手牌排除，**无 `IsInPlay`**）。
10. **698 `CanPlayWithOathNowInPlay`** —— 新原语 `CanPlayWithOathNowInPlay`（`Partial:197`）。判据 `CardScript__CanPlayWithOathNowInPlay.c:5-48` ＋ ILSpy `:8241-8308`。🔴 **不减卡费**（与手牌版唯一的差别）。
11. **特效520 `CreateMarkCardEffect`** —— `CreateMarkCardEffect` / `ShownMarkRow`（`Partial:308/327`）。判据 `EntityScript__CreateMarkCardEffect.c:1-21`；🔴 `BuffType = 8` 是 `CardEffectItem.LoadEffect` 的分支键（换值 = 静默不印）。

> 上面 11 条覆盖 **13 行**（501/711 与 502/712 各自两条账行共用一处落点）。

### C（10 条反证，阻塞点都在白名单外的文件上）

| 账行 | 阻塞在哪（文件 = 所有者） | 反证要点 |
| ---: | --- | --- |
| 77 `GetValueFromCriteria` | `EffectText.cs`（解析层） | 三档缺的根源是解析层认不出 `Warlord Melee` / `excess` / `equal to its`（全 0 命中）；`op.AmountRef` 今天只有 faith/spirit/enemyhand。⛔ 没在 `TryApplyAmountRef` 加「无生产者」的三档（死代码）。 |
| 172 `AddTransformCardWithBuff` | `RuleCore.cs`(E1) + `CardDef.cs`(E4) | 换卡核心口 + 变身字段都不在；池内 `transformUnit` 仍 0 可达（复核成立）。 |
| 521 `ResolveJamUnit` | `ActionPump.cs`(E3) + 全量重算口 | 缺的两格 = `CancelEffectsFromDisappearingUnit` 与两侧 `UpdateCardEffects` **全量重算**；本文件那半边已落且无回退。 |
| 637 `BroadcastRemoveFromGame` | `UnitState.cs`(E4) / `PlayerState` | 没有「移出游戏」状态位就必须造半套状态；`state 11` 置位支**无调用点**（死状态），广播那一半经残骸翻面可达。 |
| 644 `BroadcastTurnStarted` | `RuleCore.cs:14747`(E1) | 缺的是**两跳手牌**，而广播器在 `RuleCore.cs`；本文件的 `TurnTriggerNameOf` 是另一件事（玩家卡回合触发名），加了也不补 ⇒ 没做。 |
| 690 `AlreadyContainsEffect` | `CardInstance.cs` / `Enchantment.cs` | 缺的两维（`enchantmentCardId` / `extrinsicVariable`）**今天都是恒等维** ⇒ 可观察差异 0（`Enchantment.cs:46-49` 明写附魔不进 `HandEffects`）。 |
| 808 `NeedsToChooseFromPool` | `BattleDriver.cs`(E5) | 引擎五道闸已落（A10）；剩下是 `PlayAsks` 换三参重载。⛔ **别换 `AbilityAsks`**（`UM97` 那处 ask 在誓约正文里）。 |
| 1122 `GetNextSlotToCard` | `CardDef.cs`(E4) + `RuleCore.DeployFreeCore`(E1) | **算法已落**（`Partial:255`，判据 `MinionManager__GetNextSlotToCard.c:1-110` 逐句），但 `createAtPos` 在我们 DTO 上**没有格子** ⇒ 今天 0 调用点。 |
| 1287 `TriggerEnchantment` | `Enchantment.cs` | 缺「按 trigger **0 / 500** 派发**附魔能力**」的通用层（现只有 `OnCarrierPlayed` 这条 **DA42 专用**硬编码）。 |
| 特效521 `GetCardEffectsToShow` | `UnitState.cs`（S2 `Seq`）+ S0/S3 记录面 | 整条投影要 `Seq` 与各族记录面接齐；只交 mark 分支的数据形状（= 特效520），**没写半条会印错的投影**。 |

---

## 三 改动清单

**`RuleEngine/Core/EffectResolver.cs`（5 处，净 +1 行）**

| 位置 | 改动 |
| --- | --- |
| `:10943` 前后（`BroadcastKeywordEvent` 的 `preCallSelf`） | 加 `\|\| kind == WhenEventKind.GetsDarkPact`（656） |
| `:9875`（`BroadcastWhenCore` 排序前） | 新增语句 `if (kind == Play && subject != null) listeners.RemoveAll(...)`（628） |
| `:12966`-1（`BoardJumpIncludesPendingDeath` 的注释 + return） | `Play` / `GetsDarkPact` 加进放开族（628 / 656），**注释就近合并、行数不变** |
| `:6710` 附近（`DoReturn`） | 新增 `if (moved.Count > 0) SweepHandEffectsOnRemoval(ctx);`（531）＋ 一行注释（**这行的净增 = +1**） |

**`RuleEngine/Core/EffectResolverPartial_B88E2.cs`（新建 · 337 行 · + `.meta`）**
`ChangeBaseAttackFidelity`(61) · `ChangeMaxHealthFidelity`(114) · `ForceChangeMaxHealthFidelity`(158) · `CanPlayWithOathNowInPlay`(197) · `NextSlotToCard`(255) · `CreateMarkCardEffect`(308) · `ShownMarkRow`(327)。

**账**：`账/_parts/B88_E2.tsv`（23 行 · 13 A / 10 C）。

---

## 四 没查清的部分（如实）

1. **501/502/711/712 在现 build 是否真的休眠** —— 我按「ILSpy 全树 + `decomp_full` 全树都搜不到 `changeAttack` / `changeHealth` 的入队点」判「休眠」。⚠️ 但**ILSpy 会丢 `= true` 写点、也会丢常量**（CLAUDE.md 已记两例）⇒ 这条是**两路一致**得出的，**没做 VA 级反汇编确认**。
2. **`ForceChangeMaxHealth` 的 `+0x24c` 读者** —— 没查到（两路反编译只见这两处写点）⇒ 那一格我们没有等价位，**没敢造字段**。
3. **494 的卡级样本** —— `BL49` 的 Dark Pact 段没跑 `ruleprobe play`（子代理不跑 Unity）⇒ 只有结构读数，没有前后差值读数。
4. **628 的棋盘跳自我排除** —— 原版「只排除不预调」是我从 ILSpy 逐句读的（`.c` 里那条 `op_Inequality` 只有 1 个实参，是 Ghidra 掉实参的已知形态）。⚠️ **若 `RuleEngineTest` 里有「单位听自己被打出」的断言，这一改会红** —— 同步点请重点看那一条。
5. **1122 的格号换算** —— 「原版 `IndexOf+2` ⇒ 我们 `SlotOf(side, i+1)`」是推出来的（原版有符号格 = 列表下标+1，与我们 `WarlordSlot + side*(i+1)` 的映射），**没有实机读数**（贴卡三档当前池内几乎不可达）。

---

## 五 顺手发现（⛔ 都没改白名单外的）

1. **`BoardJumpIncludesPendingDeath` 的注释已经写了两轮「逐族」名单，但 `BroadcastUnitObtainedHuntMark` / `…Markerlight` 那两族原版同样没有 `IsInPlay`**（`BattleManagerSupport.cs:1893-1903` 同形）⇒ 它们今天仍被待死闸挡着。**这是同一族的第三、第四格缺口**，建议单列一条新账（本批只按账行 628/656 改了两族，没顺手扩）。
2. **`Enchantments.OnCarrierPlayed` 是 DA42 专用硬编码**（`Enchantment.cs:1190-1214`：写死「+3 任务点 + 摘附魔」）—— 它长得像通用派发、实际只服务一张卡。1287 那条真缺口多半会因为「看着已经有 `OnCarrierPlayed`」被下个会话误判成「已做」⇒ 值得在 `Enchantment.cs` 里加一句「这不是通用派发」的警示（**该文件不在白名单，未动**）。
3. **`EffectResolver.cs` 里 `TryApplyAmountRef`（`:292` 附近）只有三个来源**，而原版 `ChooseValueOptions` 26 档里池内现用 9 档 ⇒ 剩下 6 档（`0/8/80/90/202/204`）是「解析期直接把值写死」的近似。这一层口径今天没有任何一处文档写清「哪 6 档靠解析期、误差可能在哪」。
4. **`RuleCore.cs:4087` 与 `EffectResolver.cs:2616` 是 `BroadcastWhen(Play)` 的两个生产点**，但只有前者传 `subject`。我的 628 改动依赖「`subject` 就是被打出那张」这一前提 —— **如果将来有人给 2616 也传 `subject`（战术卡），会静默把它从棋盘跳剔掉**（无害，但值得一句注释；那句注释我加在了 `BroadcastWhenCore` 里）。

---

# 第二批（2026-10-27 晚 · 调度台追加指令）

> 授权扩到 `RuleEngine/Core/Enchantment.cs` 与 `RuleEngine/Data/EffectText.cs`（两处此刻无别人写）。
> `typecheck`：**运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_s88e2b`）。
> 补丁：`账/_parts/B88_E2b.tsv`（**4 行** · 11 列 · LF）。

## 结论

| 项 | 结果 |
|---|---|
| ① **808 `NeedsToChooseFromPool`** | **实读确认 → A**：`BattleDriver.cs:10974` = `RuleCore.PlayerChooseOps(Ctx, _me, card)`（三参重载，行尾注释「⛔ 别换 `AbilityAsks`」）；`AbilityAsks`（`:11037`）仍是一参版 ✅。三参版内部就是 `if (NeedsToChooseFromPool(ctx, owner, card, null)) return r;` + 判否时出声。**未改该文件**。 |
| ② **1287 `TriggerEnchantment`** | **A（结构落地）**：新增 `Enchantments.TriggerEnchantment`（`Enchantment.cs:1324`）+ `ParseEnchantOps`（`:1368`）。判据 = `RawCardScript__TriggerEnchantment.c:1-22`（两跳 0 / 500，`.c` 第 8 实参 `isEnchantment = 1`；ILSpy 把它渲染成 `default` = 丢 `= true` 常量的已知形态）。🔴 与 **DA42 专用**的 `OnCarrierPlayed` 在两处注释里互相点名（⛔ 别把前者当通用层）。**数据今天为空**：模板卡 `desc` 空（trigger 0 得 0 条）· 全池 `trigger == 500` **0 行**（trigger 500 恒 null）⇒ 两跳都空时**出声**。原版两个调用点（教程回合末附魔 / `loadingAbilityLogic`）我们都没有 ⇒ **0 调用点**（休眠）。 |
| ③ **77 `GetValueFromCriteria`** | **2/3 落地，仍判 C**：140 `OwnWarlordAttack` 与 101 `BaseRefCardMAttack` 已通（见下），**210 `TargetExcessDamage` 仍缺**。 |
| ④ **690 `AlreadyContainsEffect`** | **A**：两维（`enchantmentCardId` / `extrinsicVariable`）在本仓**结构上取不到两个不同值**（`Enchantment.cs:46-49` 附魔绝不进 `HandEffects`；`extrinsicVariable` 从未产出）⇒ 9 维逐字段比与原版 11 维**等价**。⛔ 不是「影响小所以不比」。 |
| ⑤ **1122 `createAtPos`** | **不硬做**，只出规格（见本节末）。 |

## ③ 77 的两档：解析层 + 结算层都改了，读数如下

**解析层（`EffectText.cs`，+42 行）**
- `ReEqualOwnWarlordMelee`（`:8541`）：`… equal to your Warlord's Melee`（句尾）→ `AmountRef = "ownwarlordmelee"`。
- `ReEqualItsBaseAttack`（`:8552`）：`… equal to its [Attack]` / `… its attack`（句尾）→ `AmountRef = "refcardbaseattack"`。
- 剥句位置 = 原 `equal to your Faith` / `equal to the number of cards in the enemy hand` 那一组的 `else` 分支（`EffectText.cs:3471-3494`）。
- ⚠️ **只认这两个词形**（池内 `equal to its …` 共 4 处，另 2 处 `its Shuriken` / `its Health` **不许**吞进来 —— 现扫 4/4 对上）。

**结算层（`EffectResolver.cs:305-312` + `EffectResolverPartial_B88E2.cs:308`）**
- `ownwarlordmelee` → `ps.Warlord.Attack`（**当前**近战：那张卡自己前半句给 +1 `[Attack]`，这句要吃它）。
- `refcardbaseattack` → `RuleCore.RefCardBaseAttack(ctx)`：`LastCreated` 最后一份（在棋盘上按 `Instance` 反查）→ 退路 `LastTarget` → 都拿不到返回 `-1`（调用方**出声**，⛔ 不当 0）。取 `AttackBase`（名字里的 `Base`；`RefCardAttack = 100` 才是当前值）。

**读数（`ruleprobe play`，改前 → 改后）**
| 卡 | 改前 | 改后 |
|---|---|---|
| `EC47 Exquisite Swordsmanship` | 「对 1 个目标造成 **0** 点伤害（共 0）」 | 「对 4 个目标造成 **3** 点伤害（共 12）」（战将 2 + 本回合 +1） |
| `UM118 Indomitus Crusade` | 「deal damage to all enemy troops equal to its attack」→ 3 个目标 **0** 点 | 3 个目标各 **5** 点（共 15）= 刚部署的 `1st Company Terminator` 的 `AttackBase` |

**`ruleprobe check`（解析层）**：**解析差异 2 行**（`EC47|Exquisite Swordsmanship` · `UM118|Indomitus Crusade` —— 正是这两张，无其它卡受影响）；改名 0 · 新增 0 · 消失 0；基线 **未重生成**。⛔ 哨兵 ⑦`[输出指纹]` 因此红 1 条（`现算 fd890dc5…` vs `钉住 c9a64798…`）—— **按指令不重钉**，留给调度台确认。
**`ruleprobe play check`（结算层）**：**3 张卡变了** —— `EC47`（`dmg 0→12`）· `UM118`（`dmg 0→15`）**是本笔**；`GOF57 Ork Nob`（`selfPlace 0→1` / `exhausted` 列）**不是本笔**：它的 `desc` 是「When you play a troop, give it Flank」，与我改的两条正则无关，而**同一工作树里 E1/E4 正在改 `RuleCore.cs` / `Aura.cs` / `SimpleAI.cs`** ⇒ 那条差异应归并发写手，**同步点必须等大家落完再重测**。

## ⑤ 1122 的规格（下一批照这个做）

| 要补什么 | 谁消费 | 判据 |
|---|---|---|
| **`CardDef.CreateAtPos`（int）** —— 原版 `SummonCriteria.createAtPos (+0x20)`；生成器 `gen_cards_engine.py` 带出该列 | `EffectResolver.DoCreate`（`summonUnit(70)` 那一族）→ 传「请求格」 | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/SummonCriteria.cs` 字段表 · `cards_raw.json` 现读 `createAtPos` **71 处** |
| **`RuleCore.DeployFreeCore` 接受「请求格」**（两条重载都要）→ `BoardSlots.Insert(ps, u, requestedSlot)` | 同上 | `decomp_full/MinionManager__GetNextSlotToCard.c:1-110` · `AbilityLogic__PlayAbility.c:755-780`（落点四档：`self→GetNextSlotToCard(thisCard)` / `cardTarget→…(targetCard)` / `friendlyHero→…(该侧英雄)` / **其余→`GetNextSlotWithoutDisplacing` + LogWarning**） |
| **算法那一半已经写好** | `RuleCore.NextSlotToCard(ctx, owner, anchor)`（`EffectResolverPartial_B88E2.cs:255`） | 同上（逐句照抄，含「督军/空卡直接默认落点」与「都不中 ⇒ 出声 + 默认落点」） |

⚠️ 可达性（沿用旧账的时间限定）：`createAtPos ∈ {0 self, 1 cardTarget}` 全池 **0 处**、`3 friendlyHero` **1 处**（GUID 名卡）⇒ 接线后当前卡池几乎不可测，验收只能用**合成夹具**。

## 第二批的改动清单 + 行数影响（⚠️ 同步点要 `--fix`）

| 文件 | 净行数 | 说明 |
|---|---:|---|
| `Core/EffectResolver.cs` | **+8**（累计，本批 +7） | `TryApplyAmountRef` 加两个来源分支 |
| `Core/EffectText.cs` | **+42** | 两条正则 + 剥句分支 + 四处 doc |
| `Core/Enchantment.cs` | **+91** | `TriggerEnchantment` + `ParseEnchantOps`（追加在类尾） |
| `Core/EffectResolverPartial_B88E2.cs` | 391 行（本批 +54） | `RefCardBaseAttack` |

⇒ 这三个老文件都被 `账/*.tsv` 按 `文件:行号` 引用 ⇒ **`check_ledger_points --all-A --fix` 必须跑**（本代理不跑、不改基线）。

## 顺手发现（第二批）

1. **ILSpy 把 `TriggerEnchantment` 的 `isEnchantment = true` 渲染成 `default`**（`RawCardScript.cs:1893/1896`）—— 与 `CLAUDE.md` 已记的「ILSpy 丢 `= true` 常量」同族**第三例**（前两例：`GetCardsInHandList` 六开关全 `default`、`secretBuff`/`questBuff` 写点）。**判据必须是 `.c`**。
2. `Beautiful Death` 那句 `For each excess damage` 今天被通用计数口当成 `own|any|all` **计 4 遍**（`play` 现读）—— 这个数**没有任何语义**，且它掩盖了「210 未实现」这件事（日志看起来像在跑）。建议给 `CountEval` 加一句「认不出的 for-each 短语 ⇒ 出声」。
3. `TryApplyAmountRef` 的**未做 6 档**（`0 / 8 / 80 / 90 / 202 / 204`）今天靠解析期写死数值；`8` 已判定是**数据侧的 editor 残留**（`ValueOptions异常值_判定.md`：38 行全死数据）⇒ 剩下 5 档（`0/80/90/202/204`）值得单列一条账，写清「哪些句子实际靠解析期近似」。
4. `Enchantments.OnCarrierPlayed` 的 doc 里已经写明它是 DA42 专用，但**方法名**（`OnCarrierPlayed`）仍然像通用钩子；我在 `TriggerEnchantment` 的 doc 里反向点名了它，建议将来改名（`OnDA42CarrierPlayed`）—— 改名会动调用点，**本波没动**。

---

# 第三批（2026-10-27 深夜 · 调度台追加指令）

> 授权扩到 `RuleEngine/Core/BattleContext.cs` 及其 partial（我用了 **partial 新文件**，主文件一行未动）。
> `typecheck`：**运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_s88e2c`）。
> 补丁：`账/_parts/B88_E2c.tsv`（**1 行** · 11 列 · LF）。

## 结论

| 活 | 结果 |
|---|---|
| **1️⃣ 假绿** | ✅ **修掉**。`for each` 短语**认不出**时不再静默按「本方全体单位」兜底：`EffectText.ClassifyCount` 的 ④ 支把「纯靠默认值猜」记进 `EffectOp.CountFallback`（`EffectText.cs:807`），`EffectResolver.TryComputeRepeats` 见到就 `ctx.Log` **出声**（`:13877`）。 |
| **2️⃣ 剩余 5 档** | ✅ 逐档判据落成 **`判据/ValueOptions_剩余档_判定.md`**：`0` / `202` / `204` **对齐（不改码）**；**`80`（3 行 / 2 张）与 `90`（87 行 / 63 张）是真差异**，建议调度台按那份**新立 1~2 条账**（`90` = P1 量最大且判据齐 · `80` = P2 判据缺、今天可观察差 0）。⚠️ 本代理不写账主表 ⇒ 表里没有新增行。 |
| **3️⃣ 210 `TargetExcessDamage`** | ✅ **落地**，账 77 **整条判 A**：新 `BattleContext.LastDamageExcess`（partial `BattleContextExcessDamage.cs`）+ 两处伤害点写值 + `CountFor` 的 `excess` 支 + 解析层 `for each excess damage` 一档。 |

## 1️⃣ 假绿：为什么原来「看着在跑」

`Beautiful Death` = `Deal 10 damage to an enemy troop. **For each excess damage**, heal 1 to your Warlord`。
`EffectText.TryForEach` 把 `excess damage` 这个短语交给 `ClassifyCount` ⇒ 它四道支（`one` / 手牌 / 契约 / 抽牌 / 阵亡 / 打出 / **④ 盘面**）都不命中 ⇒ 落进 ④，
而 ④ 里有两条**默认值**（「没写谁 ⇒ 本方」`own=true`、「没写什么 ⇒ any」`kind="any"`）⇒ 得到一个**完全合法**的 `own|any|all`，
`CountFor` 就数出「本方场上 4 个单位」⇒ 日志印「按 own|any|all 计 **4** 遍」—— **看起来像在正常结算，实际那个 4 毫无语义**。

**改法（不改判决、只出声）**：④ 支新增 `guessed = !own && !foe && !damaged && !hasKindWord && !inPlay`
（= 一个信号都没有 ⇒ 纯靠默认值），经 `ClassifyCount` 的四参重载吐给 `TryForEach` 的三个落点，记进 `EffectOp.CountFallback`；
结算层见到它就补一行：

> `（⚠️ 上面那个计数「own|any|all」是**本版认不出**该 for-each 短语之后的**兜底猜测**（按「本方全部单位」数出 4）；原版走的是结构化 multiCountCriteria —— 数错/多数时以卡面为准，别把它当成已验证）`

⛔ **没有**把返回值改成 `false`：那会让今天能跑的二十几张卡一起哑掉（是更大的回归）。⛔ **没有**改任何判决/数值。

**新增出声条数 = 0（今天）**，但机制已就位。复算（`cards_engine.json` 逐卡抽 `for each/every …` 短语、按 ④ 支「零信号」条件判）：
全池只有 **1 条**零信号短语 —— `GSC56` 的 `sabotage in the opponent's hand`，而它被 `ReCountInHand`（手牌那一支，排在 ④ 之前）先接走；
唯一真正走到过兜底猜的 `Beautiful Death`（`excess damage`）已被 3️⃣ 接管 ⇒ **现在没有卡再靠兜底猜**。
⚠️ `play all dump` 的读数**逐卡不变**（本批只多日志、不改数值），与上面的隔离实测一致。
⚠️ 这也意味着：**这条出声是「防将来」的**（下一个人加一张带新 for-each 短语的卡时，日志会立刻指出来，而不是静默多数）。

## 3️⃣ 210：改前 / 改后（含隔离实测）

`play Beautiful Death`：
- **改前**：「按 **own|any|all** 计 4 遍」（那个 4 = 本方棋盘人数：3 个 fixture + 督军）→ 治疗 0（督军满血）
- **改后**：「按 **excess** 计 **4** 遍」（10 点打 6 血 ⇒ 超额 4）→ 治疗 0（督军满血）

⚠️ **两个 4 是巧合**（本 fixture 恰好 4 个单位）⇒ 光看数字分不出真假，所以我做了**隔离实测**：
把写值临时改成 `ctx.LastDamageExcess = 0` ⇒ 同一句立刻变「按 excess 计 **0** 遍」，复原后回 **4**
⇒ **计数源确实是超额量**，不是棋盘人数。

**`LastDamageExcess` 的默认值语义（按你的要求写清）**：
- `0` = **没有超额**，或**本条 ability 还没造成过伤害**（两种情况故意同值 —— 原版没有 `DamageInfo` 时也读不到超额）；
- **每条 ability 起手清零**（两个 `ResolveOps` 入口各调 `ResetDamageExcess()`）⇒ **不会读到上一张卡的残值**（`LastCreated`/`LastTarget` 那一族踩过的坑）；
- 一条 ability 内**逐次覆盖、不累加**。

🔴 **已知近似（如实标）**：超额 = `DamageAfterReduction(目标, 点数) − 目标当前血`（取正部）—— **不含 `ignoreBastion`（堡垒）那一档**；
多目标时取**最后一个目标**。今天唯一消费者是**单目标**卡 ⇒ 两处近似零可观察；换多目标消费者前必须先补判据。

## 第三批的读数（⛔ 基线未重钉）

| 尺子 | 读数 |
|---|---|
| `ruleprobe check` | **解析差异 3 行**（`EC42 Beautiful Death` · `EC47 Exquisite Swordsmanship` · `UM118 Indomitus Crusade`）· 改名 0 · 新增 0 · 消失 0 · **基线未重生成** |
| 哨兵 ⑦`[输出指纹]` | 红 1 条（`现算 fd890dc5…` vs `钉住 c9a64798…`）—— **预期**（我改了结算+解析），按指令**不重钉** |
| `ruleprobe play check` ①-a | **3 张卡变了**：`EC47`（dmg 0→12）· `UM118`（0→15）· **`GOF57`（不是本笔）** |
| `ruleprobe play check` ①-b | **32 张卡变了**，全是 `AM*` 族的 `duty`/vanguard 落场/伤害链 ⇒ **不是本笔** |
| **本批的隔离实测** | 把本批新增的那行出声临时关掉 ⇒ ①-a/①-b **逐项相同（3 / 32）** ⇒ **本批零数值影响**；那 3+32 是波 1/波 2 或**并发写手**（E1 `RuleCore` / E4 `CardDef`+`Aura` 正在改，`git status` 现读都是 `M`）留下的，**同步点必须等大家落完再重测** |

## 第三批改动清单 + 行数影响（⚠️ 同步点要 `--fix`）

| 文件 | 净行数 | 说明 |
|---|---:|---|
| `Core/EffectText.cs` | **+94**（累计，含波 2 的 +42） | `EffectOp.CountFallback` 字段 + `ClassifyCount` 四参重载 + 三处落点 + `excess damage` 一档 |
| `Core/EffectResolver.cs` | **+29**（累计，波 1 +1 / 波 2 +7 / 波 3 +21） | 两处伤害点写超额 · `CountFor` 的 `excess` 支 · `CountFallback` 出声 · 两个 `ResolveOps` 入口清零 |
| **`Core/BattleContextExcessDamage.cs`（新建 partial）** | 43 行 | `LastDamageExcess` + `ResetDamageExcess()` —— **主文件 `BattleContext.cs` 一行未动** |

> 口径 = `git diff --numstat` 现读（`EffectText.cs 102/8` · `EffectResolver.cs` 累计 `+29` · `Enchantment.cs 91/0`）。
> 三个老文件都被账按 `文件:行号` 引用 ⇒ **`check_ledger_points --all-A --fix` 必须跑**（本代理不跑、不改基线）。

## 顺手发现（第三批）

1. **`ValueOptions` 的两个统计口径差得很大**（`判据/ValueOptions异常值_判定.md` §一 的 `80:10 / 90:88` vs 我按 `cards_truth.json` 单份树扫的 `80:3 / 90:87`）—— 前者含**伤害/其它字段的另一种切法**。这正是 `CLAUDE.md` 点过的「三个张数口径别互相校验」同族问题 ⇒ 引用时必须**连口径一起写**。
2. **`GSC35` / `GSC59` 的 `80` 档今天被 `AmountRef="enemyhand"` 覆盖**（那两张是「等于敌方手牌数」的卡）⇒ 数据里那个 `80` 是**死值**。与 `8` / `17` 那批「editor 残留」同族，但**不能按「死数据」归档**（形状是错的，将来一旦 `AmountRef` 不覆盖就会露出来）。
3. `Beautiful Death` 的 `heal` 之所以读数是 0，是**督军满血**（不是缺陷）。真要验「超额 4 ⇒ 治疗 4」，夹具必须先把督军打伤 —— 现有 `play` 夹具不提供这个能力。
