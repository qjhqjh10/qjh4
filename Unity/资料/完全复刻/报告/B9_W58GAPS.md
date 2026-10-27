# B9_W58GAPS —— W48「真缺口 C」逐条落成施工判据（报告）

> 代理：W58GAPS（只读为主）· 2026-10-27
> **产出**：`资料/完全复刻/判据/引擎真缺口_施工判据.md`（808 行 · 37 个施工块 · 覆盖 41 条账行）
> **只读脚本**：`d:/4/_tmp_view/w58/`（`scan.py` / `dumpfam.py` / `dumpcompact.py` / `classify_copy.py` / `valueopts.py` / `vopt2.py`）
> ⛔ 未改任何 `.cs` / `.py` / `.json` / 账主表 / 正本 / 资产；未跑 Unity；未动 git。

---

## 结论

1. **W48 的外推对得上**：逐条核完 **F4=29 · F3=7 · F14=6 块（7 行）**，与 W48 §四「C ≈ 43」同量级。F1 的 7 条**全部判 D**（P3 服务端半，按 2026-10-25 裁定 + 2026-10-26 授权）⇒ **施工 41 条 / 37 块**。
2. **只有 7 块今天可做**（判据齐 + 落点不在 W54/W57 占用的 `RuleCore`/`EffectResolver`/`BattleEvent`/`BattleDriver` 里）：G08–G12 五个 `MatchType` 谓词（`BattleContext.cs`）· G16 `ta=230`（`AiTargets.cs`）· G37 Whispers 闸（`Enchantment.cs`）· G34 `GetThisCardPlayedDamage`（`CardDef.cs`）· G28① `courtesy` 前缀 · G19 队列查询口 · G06① 计数器字段。**建议第一批就做这 7 块**（纯增量、零行为改动、可单测）。
3. **最有价值的三条不是"补机制"，而是"补 bug/接线"**：
   - **G23① `Uprising` 过触发** —— 唯一一条**多触发**型实锤差异（缺「本回合上场」闸，我们在后续回合仍触发）；
   - **G22② 死亡路径不摘外来增益** —— 原语 `UnitState.RemoveExtrinsicEffectsFrom` **已存在**，但 `Die` 不调；
   - **G07 `MissActions` 入队点** —— 动作体**早已实现**（账说"没有计数器"是过期结论），只差在「无合法目标」支接线。
4. **休眠机制（池内 0 张）4 块**（换位 / 变形 / 复制墓地 / 偷牌）判据**已齐到可施工**，按铁律 11 要做、排最后；其中 G03 的「造新卡」助手是 G02 的前置。

## 证据（文件:行号）

- **判决来源**：`资料/完全复刻/判据/战斗逻辑驱动_B行重裁.md` §二 F3/F4/F14 + §三 3·2 + §四。
- **账现读**：`账/战斗逻辑驱动.tsv` 1,376 数据行（A 711 / B 402 / C 45 / D 218），本批 43 行**全是 `状态=B`**。
- **原版逐句**（本批新读，全部落进判据文件）：
  - `decomp_full/BattleManager._ResolveExchangeMinion_d__552__MoveNext.c`（357 行，两段 yield：`:36-70` 三条闸 / `:64-77` 广播+4 次摘手牌 / `:81-152` `ReplaceUnit` / `:154-159` `ChangeOwner` / `:316-347` 收尾五跳）
  - `decomp_full/BattleManager__ResolveTransformUnit.c`（2,251 行，16 个子句按行号排好序）
  - `decomp_full/BattleManager._ResolveCopyUnitInCemetery_d__541__MoveNext.c`（146 行）
  - `decomp_full/BattleManager._ResolveStealFromDeck_d__547__MoveNext.c`（271）/ `…StealFromHand_d__546__MoveNext.c`（244）
  - `decomp_full/BattleManager__IsAgainstAI.c`（43，位测试已逐值还原成 10 个 `MatchType`）/ `__IsAgainstOnlineBot.c`（45）/ `__IsDuringBattle.c`（19，配 `资料/普查产出_1016战斗判据/T09b_uGUI拦路判据.md` §B·1 的 15 档真名）/ `__IsReplayMatch.c`（28）
  - `decomp_full/BattleManager__ReleaseCardbacks.c`（113，= 两方 cardback cosmetic 的 `UnloadReferences`）
  - `decomp_full/BattleManager__AllowResolveAttack.c`（末段 `:439-558`：`isPlayer ∧ !IsAttacking ∧ matchType!=160` ⇒ 写 `+0x528` 后 `return 0`）
  - `decomp_full/BattleManager__CheckForHeroDeckTransform.c` + `BoardAnalysis__DeckMeetsCriteria.c`（三条比较 `==/>=/<=`）
  - `decomp_full/SupportMethods__ShuffleCardList.c`（93）+ `__GetRandomOrder.c`（98，**关键：不是 Fisher–Yates，是"每次从未用集合取第 r 个"**）
  - `decomp_full/CardScript__NonTargetSpellPlayed.c`（31）/ `__ResolveUnitSummoned.c`（Uprising 闸 `:92-96`）/ `__CheckForSkullEarned.c`（69）/ `__UseSurvivor.c`（`:87` 是 `BroadcastUnitHealed` 的唯一生产点）/ `__get_IsAttacking.c`（= `cardState ∈ {3,17}`）
  - `decomp_full/PlayerHand__RemoveEffectsFromCard.c`（47，倒序 + 按**施放者卡**反查）/ `EntityScript__GetThisCardPlayedDamage.c` / `EntityScript__ReachedQuestCounter.c`
  - `out_cpp2il_dev_ilspy/ChooseValueOptions.cs`（**26 个成员**，不是 ">170 档"）
- **我们现状（三源）**：`grep Assets` 逐条落进判据文件（`exchangeMinion`/`transformUnit`/`copyUnitInCemetery`/`steal*`/`ReleaseCardback`/`IsAgainstAI` 等**全部只有注释/枚举**）；ILSpy `AddTransformCard*` 是 decompiler-issue 壳（不可用，已回 `.c`）；`assets_full` 侧用卡表可达性代替（`desc` 含 swap/exchange/steal/cemetery 各 **0 张**）。

## 没查清的部分（判据文件 §四 有完整版，这里只列七条硬缺口）

1. **`transformUnit` 的 `keepEffects`（`+0xEC`）两个分支** —— `.c` 里按局部拷贝读、偏移被摊平，**直读不出**。
2. **`AllowResolveAttack` 末段的 `IsAttacking` 何时置位/清位** —— 需读 `_PrepareAttack*` 的 MoveNext 或 VA；**在这之前落实现会把玩家攻击全拒掉**（判据文件明确"不许硬做"）。
3. **`copyUnitInCemetery` 的 `param+0x2c` / `vars+0xa8`、`CheckForHeroDeckTransform` 的 `rawBattleData+0x18/+0x28`** —— 字段名与我们 deck 数据**未对上**。
4. **`IsNetworkedGame` 在引擎层的权威位** —— 今天只有 `BattleDriver._net != null`（表现层）。
5. **`*ValueOptions` 的两个异常值**：`8`（38 处，`ChooseValueOptions` 无此值，`StatFilterOptions.RandomChance = 8`，但字段声明是 `ChooseValueOptions`）· `17`（8 处，**两个枚举都没有**，全在 `GOF30`–`GOF34` 的 `buff.healthValueOptions`）—— **必须先查清再实现分派**（否则静默按 0）。
6. **三个 trigger 常量语义**：`Combo` 590 vs 1130 是两回事、`IsTheSameTurnPlayed()` 体未读、`CheckForSkullEarned.c:21` 的 `iVar2 != 10` 是哪个枚举。
7. **`ReleaseCardbacks` 的等价位**：原版是 Addressables `UnloadReferences`，我们美术常驻 `Resources/` ⇒ 只能落 `🔶 自己设计`，**需调度台/用户确认这个等价物算不算数**。

## 该怎么改（逐条施工判据）

**全部在 `资料/完全复刻/判据/引擎真缺口_施工判据.md`**：§一 45 行清单表（编号 G01–G38 / 账行 / 缺口 / 落点文件.方法 / 可做性）· §二 37 个施工块（每块六段：原版逐句 · 我们现状三源 · 落点 · 施工步骤 · 验收口 · 依赖）· §三 四个批次 · §四 未闭合判据 · §五 过期记载清单。

**批次摘要**：
- **第一批（今天）**：G08–G12 · G16 · G37 · G34 · G28① · G19 · G06①（`BattleContext.cs` / `AiTargets.cs` / `Enchantment.cs` / `CardDef.cs` / `ActionQueue.cs`；**其中 `CardDef.cs` 两件必须同写手串行**）
- **第二批（等 W57）**：G13 联机兜底 · G19 消费端 · G14 面板侧
- **第三批（等 W54 · 按"有真实后果"排）**：G23① → G22② → G07 → G20②/G24① → G32/G33 → G25/G35 → G29 → G31（只对算法形状）→ G36 → G30
- **第四批（休眠机制）**：G03 → G04/G05 → G02 → G01
- **不入批**：G18（缺 VA）· G21（缺手牌 abilityTriggers 执行口）· G26/G27 的实现（只做对照表）· G15（字段名）· G38（P3 已判 D）

## 顺手发现

1. 🔴 **账 524 过期**：「没有 `MissActions` 计数器」**不成立** —— `RuleCore.MissActions` 已按 `.c:1-33` 逐句实现；真缺的是入队点。
2. 🔴 **`AiTargets` 注释过期**：「230 完整选法未读完」—— `.c` 62 行早读完，只是没实现。
3. 🔴 **账 1052 前提过期**：「`CardDef` 里没有 Whispers」—— `CardDef.cs:6602` 有 `CardTypeWhispers = 40`。
4. 🔴 **账 205/206/207 自相矛盾**：同格先说「✅ 已收窄 ⇒ 只跑棋盘跳」，后说「🔴 我们走四跳会叫醒手牌」。现读两条 GainFaith 发射点都传了 `boardAndPersistentOnly: true` ⇒ **后一句是旧结论残留**。
5. 🔴 **`CardDef.cs:7663` 与账 558 打架**（都说 `ResolveTriggerRequiem` 的接线状态相反）—— 落地前核一处即可定。
6. ⚠️ **W48 的「`ChooseValueOptions` 40+ 档 / >170 档」是误读**：枚举 **26 个成员**、最大 210（170 是成员值）。
7. ⚠️ **账 136 备注建议"判 D + 时间限定"**，但它自己写"零可达" —— 按铁律 11 与 `Auras` 那条先例（仅 0 调用点不足以判 D）⇒ 本文件按 **C（G01）** 落，未判 D。
8. 🆕 **新数据：`*ValueOptions` 的真实可达面**（`cards_truth.json` 全扫）—— 池内只用到 **16 个不同值**，其中 **13 个是合法档**（`10/30/40/41/50/80/101/140/201/202/203/204/210`）+ `0/90` 常见，另 **`8`（38 处）/ `17`（8 处）是异常**。这比"40+ 档大 switch"更能指导施工。
9. ⚠️ **本文件一个行号落点都没沿**（W48 给的三处行号今天已漂）—— 落点一律写 `文件.方法`。
