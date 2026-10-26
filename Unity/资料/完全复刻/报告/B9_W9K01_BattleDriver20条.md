# B9 · K-01 `BattleDriver.cs` 20 条 + `E3P13` 接线 + 协调台追加三件

> 写手 **W9K01** · 2026-10-26（第八十四会话）· **独占** `CardPresentation/Battle/BattleDriver.cs`（全仓只有本件改它）
> 派活包 = `判据/B9_P0剩余C_派活包.md` §2「K-01」· 逐条施工单 = `判据/B9_P0剩余C_施工单.tsv`
> 追加件 = `报告/B9_E3P13_视觉链施工.md` §六（接线清单）+ 协调台当轮两条接线请求（`HUD:101` / 预组 bot）+ `W2K03` 的 `HUD:189` 请求
> 账补丁 = `账/_parts/B9_W9K01.tsv`（**11 列 × 29 行**，未合并主表）
> ⛔ 本件**没跑 Unity**、**没动 git**、**没改主表/正本**、**没碰白名单外文件**

---

## §一 结论（先说四句）

1. **开工即验 0/0**：`TMPDIR=/tmp/wf_W9K01 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改完又跑一次，仍 0/0）。文件行尾仍是 CRLF（34,260 / 34,260，裸 CR 0）。
2. **20 条里 5 条做成了 A**（`卡组:48` / `卡组:51` / `HUD:720` / `卡面:21` / `卡面:120`），**10 条落成 B**（口径与件都到位、卡在**别的包**的数据或引擎格上，逐条给出改动请求），**5 条仍是 C**（`卡组:49/50` 的 DTO 在 `DeckRules`；`卡组:194` 的时长来源 `MoveToDeck` 不存在；`HUD:20` 等 generator 补列；`HUD:364/378` 等录像记候选集）。
   另有追加件：`E3P13` 的 4 组接线（`974` 三处做成 A、`StopInPlayAnims` 三族接上；`ShakeUnit` / `ShowCardBeingDrawn` / `804` 卡在数据与引擎）+ 协调台 5 件（3 A / 1 B / 1 A）。补丁合计 **34 行 · A 11 / B 11 / C 12**。
3. **`HUD:744` 的 Codex / Oath 两枚角标不是缺口** —— `trait_collection.json` 里 `codex`/`oath`（含 4 个变体）的 `customObject` **全是空引用** ⇒ 原版 `ShowCodex`/`ShowOath` **第一句就 return**（与账 `HUD:149/151/158/159` 判 D 同一份判据）。这一条本件只补了真正有行为的 ⑥（jam ⇒ glitch）。
4. **协调台追加的五件全部接完**（牌堆 SDF 跟着卡背走 · 预组 bot 过滤链收口到 `PrebuiltDecks.GetRandomDeck` · 换语言广播扩到在册卡视图 · **`ActiveWarlordAbility` 的按下/Tick/松手三处调用点** · **抬牌「可用目标」后缀那一行钩子**），都已进 `typecheck 0/0`。

---

## §二 逐条（判据 → 现状 → 本批做了什么 → 验收口怎么读）

### A 组 · 20 条

#### ① `卡组:48` `DeckAndWarlordData.GetCardTier(RawCardScript card)` → **A**
- **判据（`.c` 亲读）**：`decomp_full/DeckAndWarlordData__GetCardTier.c` 全文 —— `CardTiers` 非空 ⇒ `TryGetValue`；命中返该档、**未命中返 `CardTier.Tier1`（0 基）**；表为 null 原版直接 NRE（不兜）。
- **本批**：`BattleDriver.cs:34106 BuildOpeningDeckData` 在开局装配处建 `_cardTiers`（`cardId → 0 基档`，取值 `CardProgress.Level(id) − 1`），督军按原版 `SetupSerializedData` **步骤 ④ 最后那一句**也单独补进去；`TierOfCard(id)`（`:34015`）是读口，未命中返 0。
- ⚠️ **与视图侧那条是两条链**（⛔ 别合并）：卡面画哪一档框走 `CardView.CardTierFor`（`CardProgress.Level(artId)`，池外/演示卡退回最高档）；本表是**开局数据**那一格，也是 `卡组:54` 反序列化要重建的东西。
- **验收口**：`TierOfCard("UM82")` = 玩家存档等级 − 1。

#### ② `HUD:92 / HUD:93` 战斗侧异画（`CheckToBeAlternate` + `CreateCard` ②）→ **B**
- **判据**：`decomp_full/BattleCardManager__CheckToBeAlternate.c:11-58`（真体：比 `GetOriginalId()` + `IsAlternateArt()`）+ `cpp2il dev BattleCardManager.cs:20-130`（同一段**被内联进活的 `CreateCard`** ⇒ 它**不是死方法**）。原版 clone id 由 `AlternateArtCard.GetIdForClonedCard()` 生成、直接躺在 `shuffledCardLibrary` 里。
- **本批**：视图侧那条链**建出来了**：`CollectAltArtCopies`（`:34051`）扫双方 `PlayerDeck.CardIds`，`#` 前缀 = 「这一份是异画副本」（`#HB_SW1` / `#SW1` 都认，剥出来的 id 找不到卡就**出声**且不猜）；`AltArtOverrideOf`（`:34037`）取 `CardArt.AltArt`，取不到**出声说明是缺图**；两条建视图路都接（手牌 `:26661`、场上 `:27360`）。
- **仍缺（两件都在别的包）**：① `工具/gen_cards_engine.py` 还没产 `card→altArt[]`（原版双向 PPtr 7 张）；② `RuleEngine/Core/DeckRules.cs` 的 `PlayerDeck.CardIds` 没有「异画副本」这一格。**这两件一落地，本件这条链自动生效**（视图侧不用再改）。
- 🔶 `#` 前缀与「先整串找、再取最后一个 `_` 之后那段」这套字符串口径**是我们自己定的**（生成器还没产那一族 id ⇒ 判据不可得），已在注释里标出。

#### ③ `HUD:364 / HUD:378` 回放遇 choose：**面板 + 2 s 自动确认** → **C（有明确卡点，不是没做）**
- **判据**：`ChooseCardMenu.<AutoProcessDone>d__21.MoveNext`（`WaitForSeconds(2f)`）+ `Setup(:559-660)`（回放自动选 `:628-639`）。
- **为什么做不了**：回放局 `BeginAsk` **根本不会被调**（它只有 `BeginPlay` / `Resolve` 两个调用点），我们直接应用录像动作 ⇒ 面板不弹。要复刻得先有「**候选集**」：录像今天只记了**答案**（`Ctx.ChoosePicks` / `ChooseCardIds`），原版面板要摆的是 `GetChoiceOptions` 的**输出**（随机抽 `maxTargets` 张那一批）⇒ **重建不出来**。
- **改动请求**：录像头加「本次选择候选集」（`CardDef` id 数组）。有了它才能在回放里摆面板 + 2.0 s 自动确认第一张（⛔ 别只把面板画出来却摆错候选）。面板本体那边 ①②③⑤ 已齐（`ShowAsk` `:10937`）。

#### ④ `HUD:537 / HUD:548` 能量结转（`savedMana`）→ **B**
- **判据**：`decomp_full/ManaTypeHolder__SetSavedMana.c` 全文 + `…__ToggleManaAccumulation.c` 全文。
- **现状**：六步状态机**早就落完**（F5，`:33283 SetSavedMana` / `:33312 ToggleManaAccumulation`）。真缺口是**引擎没有 `savedMana` 这一格**（`PlayerState.ManaCarry` 在 `BeginTurn` 立刻兑现并清零）⇒ 生产路径 0 调用。
- **本批**：施工单步骤 ② —— `UpdateHud` 那两颗能量数字改成 `Energy − savedMana`（`SavedManaOf(side)`，`:33996`；未知 ⇒ 0 ⇒ **与改前逐字相同**）；`Begin` 里复位成「未知」（本局的账不跨局）。
- **改动请求**（`RuleEngine/Core/PlayerState.cs` + `RuleCore.BeginTurn/EndTurn`，不在本件所有权）：区分「上一回合结算出的结转」与「本回合已兑现」，回合开始调一次 `SetSavedMana(saved, energy, max, true)`。⚠️ 动 `ManaCarry` 会改**全池能量曲线** ⇒ 指纹要逐卡解释。
- **验收口**：`SetSavedManaForTest(1,5,5,true)` ⇒ ON 亮 / OFF 灭 且 `MyEnergyText == "4/5"`（改后 `UpdateHud` 也写 4，两处同值，不打架）。

#### ⑤ `HUD:702` `BattleManager.CompleteBattleFinished()` → **B**
- **判据**：`decomp_full/BattleManager._CompleteBattleFinished_d__389__MoveNext.c:20-95`。
- **本批**（`ShowEndPanelAndFreezeInput` `:31534`）：① **2.0 s 真时间**闸（`Time.unscaledDeltaTime` 累加；常量 `0x1834b2bbc` 直读 `0x40000000`）② 未到 ⇒ `_endPanelPending = true; return;`，由 `Update` 下帧重试 ③ `_battleStageLeaving`（= `battleStage = 8`）④ `LeaveNetRoom()`（只在真 Play 里发）⑤ 之后才 `_endPanel.Show`。
- ⚠️ **两条如实记**：① 第 ② 跳（`if (uiState(+0x1c))` 再等一格）**没做** —— 常量 `DAT_1834b2e7c` 还没读出来，⛔ 不猜；② **批处理里不等也不离房**（`Application.isPlaying == false` ⇒ 2 s 视为已满、`LeaveNetRoom` 不调），理由 = `BattleScene.Run` 的「打完之后结算面板是显示的」与 `NetBattleTest` 的结算时序断言都排在 `RefreshAll()` 之后，而批处理**没有帧循环**、那 2 s 永远攒不够。真 Play 才走完整行为（本文件已有同款先例：`_hudHidePlayed` 那段）。
- **联机侧差量（要记录）**：对面收到「对手已离开」的时机，从「玩家点出口」提前到「最后一条动作后 2.0 s」——与原版一致，但确实改了可见时序。

#### ⑥ `HUD:720` `BattleManager.ReleaseCardbacks()` → **A**
- **判据**：`BattleManager.cs:16354` + `decomp_full/BattleManager__ReleaseCardbacks.c`（**只放那两张卡背**、不碰共享件）。
- **本批**：`ReleaseCardbacks()`（`:34224`）座位逐个取 `DeckCardBackOf(seat)` → `CardArt.PathOf` 反查缓存路径 → `CardArt.Release(path)`；调用点 = `ShowEndPanelAndFreezeInput` 末尾（`:31688`）。`CardArt.Release` **不销毁贴图**（只摘缓存条目）⇒ 已画上去的照旧显示；租约闸（`Retain`）照旧生效。
- **验收口**：`ReleasedCardbackCount` = 2；再开一局卡背照常显示（= 重新加载成功）。

#### ⑦ `HUD:744` `CardScript.ActivateCard(bool activateY)` → **B**
- **判据**：`decomp_full/CardScript__ActivateCard.c:1-148`（真体，逐句核过）。
- ①③⑦ 早就有（`RefreshHandPlayable` `:27288` 的 `playable` + `SetHighlight`/`SetOutline` + `SyncHand` 的 `ShowEphemeral`）；② 手牌恒 `inHand` ⇒ 原版那档 `cardState` 闸不适用。
- **本批补 ⑥**：`if (HasCurrentTrait(jam=130) && isPlayer) ToggleGlitch(true)` ⇒ `CardView.SetGlitch(card.Has(Jam) || Ctx.IsEphemeral(inst))`（`:27320`）。⚠️ **与「临时卡」共用同一个材质写口**（原版也是两处都写 `ToggleGlitch`）⇒ 取**或**，⛔ 别写成 `SetGlitch(jammed)`（那会把 `ShowEphemeral(true)` 换上的 glitch 又关掉）。
- **④⑤（Codex / Oath 角标）归 D**，**不是缺口**：`trait_collection.json` 现读 `codex`/`oath` 及其 4 个变体的 `customObject` 全是空引用 ⇒ 原版那两条**第一句就 return**（原版零行为）。

#### ⑧ `卡面:21` `Card2DController.ToggleTooltips(bool option)` → **A**
- **本批修的现病**：`SyncHand` 末尾原来有一段 `for (…) _handViews[i].TooltipsEnabled = true`（**整排都开**）—— 那就是「悬停任意手牌都弹 tip」的根（原版 `BattleCardManager.cs:128` 是**建卡关、抬牌才开**）。已删（**行数保持不变**，注释说明为什么），抬起来那一张仍归 `CardInteraction.ApplyShowInHand`（`:1943`）。
- ⚠️ 如实记：原版 6 个调用点里 4 处已接（本处 + `CardDisplayWindow` 两处）；`CardInBoosterPack:284/299`（开包期间整片关）与 `MulliganManager.cs:284` 仍未接 —— 两个文件都不在本件白名单 ⇒ **改动请求**。

#### ⑨ `卡面:27` `CardCollider2D.SetColliderNotInHand(CardStateOptions)` → **B**
- **本批**：`PlayDeathFeel`（`:24752`）在 `_dying.Add(v)` 后对那张视图调 `SetHitEnabled(false)`（`:24798`）⇒ **打死一个单位之后它不再可点**（原来那 0.7 s 阵亡演出里仍然可点）。
- ⚠️ **残骸不在这一次关**（它仍在场、原版那档仍是常态矩形）：上面那条「翻面成残骸」的守卫已经 `return`，走到这里的必然是**真进墓地**那一档。
- **`removedFromGame(11)` 那半无落点**：我们引擎没有这个状态（`RuleCore.cs:5157` 记着「唯一置位点是 `RemoveFromGameCard.c:19`」，全链未建）。

#### ⑩ `卡面:120` `TextTooltipController.GetTooltipPosition(float)` → **A（本来就已就位，本批复核）**
- `:31912 TickTraitTip` → `Tooltip.ShowAtMouse(body, MouseWorld, v.KeywordAnchor.z, 1f/TipPx)`（`:31944`）：z = 关键词那一层的世界 z、10 px 由 `Tooltip.MouseTipDyPx` 换算、指针走 `BattleDriver.MouseWorld`（内含**屏外归零**守卫）。与施工单要求的写法**逐字一致** ⇒ 不动。

#### ⑪ `卡组:51` `OutputLogContents()` → **A**
- 四行转储进了 `BuildOpeningDeckData` 末尾（`:34106`），顺序照抄：`deckName:` / `deckWarlord:`（**本地化名**，走 `CardDatabase.FindById` + `CardText.Name`）/ `cardback:` / 卡数。cardback 为空**照打空**（原版也照打空）。

#### ⑫ `卡组:52` `SetPlayerData(PlayerBattleData)` → **B**
- **本批**：`PlayerBattleMeta` 结构（字段名/顺序照 `.c` 偏移表，11 组）+ `MetaOf`（`:34163`）逐格灌。能拿到的五格：`deckName` / `cardback` / `playerAvatar`（`ProfileData.AvatarArt`）/ `playerAvatarBorder`（`AvatarBorderOn`）/ `playerTitle`（`TitleName`）。
- **服务器那几格离线无源**（`elo` / `allianceName` / `allianceRating` / `allianceBadge` / `warcry` / `currentDeckId`）⇒ **每次开局出声列出缺哪几格**（⛔ 不当漏填、也不静默空串）。
- **验收口**：`MyMeta` / `FoeMeta`（自检读口）。

#### ⑬ `卡组:54` `SetupSerializedData(...)` → **B**
- ③（按 id 还原卡列表）由既有 `ResolveDeck` 承担（卡 id 已是真 id，⛔ 不走名字匹配）；④（`CardTiers` + 督军那一格）见 ①；⑤（元数据）见 ⑫。
- ①②⑤ 的**反序列化那一半**是 DTO，属 `RuleEngine/Core/DeckRules.cs` ⇒ 改动请求。

#### ⑭ `卡组:49` / `卡组:50` → **C**
- 同一份 `SerializedPlayerDeckData` DTO 的两半（序列化出口 + 11 组字段清单），**落点在 `DeckRules.cs`（别的包）**。驱动侧那一半（`PlayerBattleMeta`）已按同序建好。⛔ 别照 ILSpy 那份碎的（`NoteDecompilerIssue` 一大片）——按 `.c` 偏移表写。**只做客户端侧**，⛔ 不接网络发送（P3 边界）。

#### ⑮ `卡组:194` `AddCardNotDrawnToDeck(CardScript card)` → **C**
- 原版两跳：`yield return WaitForSeconds(CardScript.MoveToDeck(1, null) 的返回值)` → `SetupInDeck()`。**时长来自 `MoveToDeck` 的返回值**（⛔ 不是硬编码 0.4）。
- 我们**没有**「卡还没离牌库就回牌库」这条路（`EventTiming` 明写没有这一档），而且时长来源 `CardScript.MoveToDeck` 在我们这边**不存在**（`CardFeel` 那一族里没有这条补间）⇒ 无落点。开局会**出声一次**（`WarnMissingPresentationData`）。

#### ⑯ `HUD:20` `ActiveSkillDisplay.SetupActiveSkill(CardScript)` → **C**
- 面板侧三件与闸都齐（`ShowSkillPanel` `:15319`，`targetsAny` 经 `CardFaceRules.ShouldShowTargetsAvailable`）。缺的**全是数据**：`工具/gen_cards_truth.py` 的 `scan_ability` 不 emit **ability 级** `targetsAvailableCounter(+0x40)`（今天读的是**卡级** `avail` 行 = 原版 `+0x298`）；池内 17 张该拼。

---

### B 组 · `E3P13` 遗下的接线

| 件 | 本批做了什么 | 状态 |
|---|---|---|
| `CardView.StopInPlayAnims()` 剩 3 族 | **全部接上**：① **残骸翻面**（`SyncBoard` `:25753`，`SetRemnantBody` **之前**停，`:25972`）② **jam**（`PlaySignal` 的 `EvtKind.StatusApplied` 支，`:22377`；关键词在 `e.CardId`、⛔ 不是 `e.Keyword`）③ **重置**（`SyncHand` 按**卡名**捞回旧视图时，`!ReferenceEquals(match.Inst, inst)` ⇒ 停；`:26615`/`:26644`）。加上 E2 已接的 `PlayDeathFeel` = 原版 10 个调用点里 **4 个**（另有 `ReturnToHand` 里那个）。余下 4 个的前提本仓都还没有（`MoveToDeck` 补间 / `MoveBackToHand` 触发源 / `removedFromGame` / 两种 `DestroyCard`） | 完成 |
| `CardFeel.ShakeUnit` ← `unitsShake` | **只做了「出声」**：`Resources/card_anim_order.json` 没有 `unitsShake` 字段（全量数据有 110 条）**且**引擎侧没有 `ResolveUnitShake`（action 472，`RuleEngine` 全库 0 处 `shake`）⇒ **留不下有意义的调用点**（留一个 = 死代码）。开局出声一次（`WarnMissingPresentationData`） | 卡在数据+引擎 |
| `CardFeel.ShowCardBeingDrawn` ← 弃牌 / 偷牌 | **做不了**：`EvtKind`（20 档）里没有 Discard / Steal，引擎那两条结算路径不发任何表现信号（`discard`/`steal` 只 `ctx.Log`）。⛔ 没为了让数字对上新增 `EvtKind` 成员。stepTime 两档（弃 0.2 / 偷 0.4）已记在 `CardFeel` 里，接线时 ⛔ 别统一 | 卡在引擎事件 |
| **974 的三处** | **全部接上**：① 敌方自动定档 `AutoAttackTypeOf`（`:34200`）= `(melee<range)+1`，在 `ApplyOwnAttackType`（`:15512`）里**只在引擎没给打法时**兜底 ② `+0x120` 缓存 = `_ownAtkTypeCache`（按 `CardView` 实例，**值没变就不调** —— `SetOwnAttackType` 按原版不短路）③ `CardData.base*` 两条建卡路都填（`:27373` 场上 / `:27460` 手牌，`statBasesKnown = true`）⇒ 数值颜色从「变一下涂一下」换成原版 **current-vs-base 持续色** | 完成（**会红一条自检，见 §五**） |
| **804 的两处** | **只记判据**：解码了三段（state 2/3/4）—— 「召唤延迟动画」= 逐卡 `AssetReference` 的 prefab（我们既没那条数据也没那条实例化链）；`m_OnDisplaySummonMessage` = 展示消息出口（无对应件）。时序四段本来就已对上（`DeploySequence`） | 卡在数据+件 |

### C 组 · E2 仍缺的两件（本批复核判词，同意保留 C）

- **`973 UndissolveToDeck`**：五步（换回卡图 → 淡入 → 停召唤病动画 → SetActive(true) → 碰撞体开）的前提是「**同一张卡停在半溶态**」；我们离场即销毁、再抽到是新建视图（一上来就全不透明）⇒ **无落点**。⛔ 别为它新建一条「把视图停成半溶再复原」的路径（那会是原版没有的形状）。
- **`936 ToggleCombatState`**：两个消费者都无落点（① 「会打死它」图标我们只由悬停准星那条路写 ② 我们**故意**不在清选择时熄攻击型底光 = AU1 口径）⇒ 只加一个 `bool` = 没有发射口/消费口的死代码。归 `B8_AW` 那条线一起做。

### D 组 · 协调台当轮追加的三件

| 件 | 做了什么 | 位置 |
|---|---|---|
| **`HUD:101` 牌堆 SDF = 卡背派生**（`W1K02` 的被调方） | `MakeDeckSdf` 签名加 `string cardbackId`，改调 `CardArt.DeckCardbackSdf(cardbackId, faction)`；两个调用点分别传 `DeckCardbackIdOf(_me)` / `DeckCardbackIdOf(1 - _me)`（与相邻 `DeckCardBackOf(...)` **同一格口径**，⛔ 不另推座位映射） | `:30805` / 调用 `:28332-28333` |
| **预组 bot 牌组接线**（`W5K09` 的新表） | `PickBotDeck` 改成 `PrebuiltDecks.GetRandomDeck(new System.Random(seed), playMode: gameMode, faction: faction)`；**删掉本地那一份过滤**（`PickFrom`）与「池外 78 副」兜底 ⇒ 过滤链**只剩一处** | `:33776` |
| **`HUD:189` 换语言广播**（`W2K03` 的被调方） | `HookHudLanguageChanged` 加一个订户 `RefreshLocalizedCardTexts`（`:33181` 挂 / `DetachStaticHooks` 摘），逐张**在册**卡视图调 `CardView.RefreshLocalizedText()`（`:33192`）；只遍历手牌 / 双方场上 / 两档淡出 / 阵亡 / 回手，⛔ 不 `FindObjectsOfType`；幂等（先摘再挂） | `:33181` / `:33192` |
| **`HUD:682/683/684/686` `ActiveWarlordAbility` 三处调用点**（`W3K04` 的状态机） | ① **按下那一沿**：`BoardCardTouchDown` 里 `wa.IsPlayer = (side == _me)` + `wa.OnTouchDown()` ② **每帧 Tick**：`TickBoardHold()` 插在 `TickBoardHover` / `BoardPress` **之前**（`:14599`），含「指针出框 ⇒ `OnTouchExit()`」（真 collider `CardView.HitByCollider`）③ **松手**：`BoardPress` 的 `!held` 支读 `HoldDisplayedThisPress` 挡重复 toggle、没拖没弹 ⇒ 先走 `OnTouchUpAsButton()`（返回假才回老的 `TapBoardCard`）；`dragged` 支也 `OnTouchExit()` ④ 三个钩子每次按下重挂（`WarlordAbilityOf`）：`ForceDisplayCard` / `SetupActiveSkill` / `TryUseActiveAbility` | `:33556` / `:14700` / `:34324` / `:34370` |
| **`HUD:148` 抬牌「可用目标」后缀**（`W3K04` 的收尾） | `Begin` 里填宿主钩子：`interaction.TargetSuffixFor = v => CardInteraction.AvailableTargetsSuffix(Ctx, _me, DefOf(v));` | `:4867` |

---

## §三 没查清 / 拿不到的（⛔ 没有猜）

| # | 是什么 | 边界 |
|---|---|---|
| 1 | `HUD:702` 第 ② 跳的常量 `DAT_1834b2e7c` | 那个 `WaitForSecondsRealtime` 的时长**还没从 `.rdata` 读出来** ⇒ 那一段**没做**（只做 2.0 s 那一跳）。下一步 = `工具/read_literal.py` 读它。 |
| 2 | `804` 的 `BattleCardUI +0xd8` 到底是不是「召唤延迟动画」那颗 GO | `.c` 里只看到 `*(battleCardUI+0xd8)` → `GetComponent` → `gameObject.SetActive(false)`；组件名在 `dump.cs` 里没锚定到字段（E3P13 §三 表也记了同一条）。**按「疑似」记，不当判据用**。 |
| 3 | 异画 clone id 的**字面格式**（`#HB_SW1` 还是 `AA_HB_SW1`） | 生成器还没产 ⇒ 本件按「`#` 前缀 + 先整串后取最后一段 `_`」的宽松口径收，已标 🔶 自己设计。 |
| 4 | `卡组:49/50` 的 DTO 是否要跟「卡组串导出」共用一份 | 属 `DeckRules` 的口径，本件只提供了字段清单（照 `.c`），没替它选实现位置。 |

---

## §四 改动清单（2 文件 · `typecheck` 0/0）

> 主要改动在 `CardPresentation/Battle/BattleDriver.cs`（我的独占文件）；另有**一处**在 `Editor/BattleScene.cs`（R16② 期望，**调度台 2026-10-26 授权**，见 §五·1）。
> **两个文件行尾都仍是 CRLF**（`BattleDriver.cs` 34,421 / 34,421、`BattleScene.cs` 48,107 / 48,107，裸 CR 各 0）；`typecheck` 改完复跑 = 运行时 0 / 编辑器 0。

| # | 位置 | 改了什么 |
|---|---|---|
| 1 | `SyncHand` | **删**「整排开 tooltip」那 4 行（等价替换成注释，行数不变）— `卡面:21` |
| 2 | `SyncHand` 按卡名捞回旧视图处 | 换实例 ⇒ `StopInPlayAnims()` — `StopAnimsInPlay` 第 3 族 |
| 3 | `SyncHand` 建卡 | `CardData.artOverride = AltArtOverrideOf(card)` — `HUD:93` |
| 4 | `PlayDeathFeel` | `v.SetHitEnabled(false)` — `卡面:27` |
| 5 | `PlaySignal`（`StatusApplied`） | jam ⇒ `StopInPlayAnims()` — `StopAnimsInPlay` 第 2 族 |
| 6 | `SyncBoard`（残骸体） | `SetRemnantBody` 之前 `StopInPlayAnims()` — 第 1 族 |
| 7 | `ApplyOwnAttackType` | 敌方自动定档 + `+0x120` 缓存 — `974` ①② |
| 8 | `ToCardData(UnitState…)` / `ToCardData(CardDef…)` | `statBasesKnown` + 五个 base + `artOverride` — `974` ③ / `HUD:93` |
| 9 | `RefreshHandPlayable` | `SetGlitch(jam \|\| ephemeral)` — `HUD:744` ⑥ |
| 10 | `UpdateHud` 两颗能量数字 | `Energy − SavedManaOf(side)` — `HUD:537` ② |
| 11 | `ShowEndPanelAndFreezeInput` | 2.0 s 真时间闸 + `_battleStageLeaving` + `LeaveNetRoom` + `ReleaseCardbacks()` — `HUD:702` / `HUD:720` |
| 12 | `Begin`（每局复位段） | `_savedMana*` / `_endPanelWait*` / `_battleStageLeaving` / `_ownAtkTypeCache` / `ReleasedCardbackCount` 复位 |
| 13 | `Begin`（`_deckNotice` 之后） | 调 `BuildOpeningDeckData` + `WarnMissingPresentationData` — `卡组:48/51/52/54` |
| 14 | `MakeDeckSdf` + 两个调用点 | 加 `cardbackId` 形参、改调 `CardArt.DeckCardbackSdf` — `HUD:101` 接线 |
| 15 | `PickBotDeck`（+ 删 `PickFrom`） | 收口到 `PrebuiltDecks.GetRandomDeck` — 预组 bot 接线 |
| 16 | `HookHudLanguageChanged` / `DetachStaticHooks` | 加/摘 `RefreshLocalizedCardTexts` 订户 — `HUD:189` |
| 17 | `BoardCardTouchDown` | `wa.IsPlayer = (side == _me)` + `wa.OnTouchDown()` — `HUD:682` |
| 18 | `DrivePlayerTurn`（`TickBoardHover` 之前） | 插 `TickBoardHold();` — `HUD:686` |
| 19 | `BoardPress` 的 `!held` 支 | `OnTouchExit()` + `HoldDisplayedThisPress` 互斥 + 没拖没弹 ⇒ 先 `OnTouchUpAsButton()` — `HUD:683/684/686` |
| 20 | `BoardPress` 的 `side == _me && dragged` 支 | `OnTouchExit()` — `HUD:683` |
| 21 | `Begin`（`HookHudLanguageChanged` 之后） | `interaction.TargetSuffixFor = …` — `HUD:148` |
| 22 | `ResolvePlayTargetByUnit`（取消出牌那一支） | 按 `animateFeel` 分成两条原版路：真 ⇒ `CardFeel.ReturnToHand`（反向 clip，重排排在 `onDone`）；假 ⇒ `CardFeel.MoveBackToHand`（α，无动画）— `MoveBackToHand` 分叉（调度台裁定） |
| 23 | **`Editor/BattleScene.cs`（调度台授权改的第二文件）** | R16② 期望改成新口径：`OwnAttackType == (两围自动定档)` + 该档数值 ×倍率 + **另一档回基准**（灭自证）— 配合 `974` |
| 24 | **类尾追加** | `SavedManaOf` · `_cardTiers`/`TierOfCard`/`BuildOpeningDeckData` · `PlayerBattleMeta`/`MetaOf` · `_altArtCopyIds`/`AltArtOverrideOf`/`CollectAltArtCopies` · `_ownAtkTypeCache`/`AutoAttackTypeOf` · `_endPanelWaitLeft`/`ReleaseCardbacks`/`WarnMissingPresentationData` · `RefreshLocalizedCardTexts` · `_waPressedBtn`/`WarlordAbilityOf`/`WarlordSetupActiveSkill`/`WarlordTryUseActiveAbility`/`TickBoardHold` |

**行号漂移（同步点请 `--fix`）**：`BattleDriver.cs` 净增 **598 行**（33,823 → **34,421**），`Editor/BattleScene.cs` 也动了（→ 48,107）。`check_ledger_points` 里指到 `BattleDriver.cs` 的 **225 条报「漂移」**（漂移总数 597 → 631；**漂移 ≠ 失败**，按 `16·8` 的口径不影响内容正确性）。
🔴 **我自己捅的一个坑（已修，铁律 5 如实记）**：第一批 34 行补丁是脚本重建的，那个脚本把落点写成了**二次前缀** `CardPresentation/Battle/CardPresentation/Battle/BattleDriver.cs`（原串已有前缀、`sub` 又补了一遍）⇒ 合并后 **18 行**变成「文件不存在」的失败，另 1 行（`OutputLogContents`）落在注释行。
　⇒ 已用 **`账/_parts/B9_W9K01c.tsv`（19 行）** 修正：路径折回 `CardPresentation/Battle/…`、行号按当时文件重读、备注留痕；**合并它之后那 6 条失败应当消失**。
　另：`账/_parts/B9_W9K01b.tsv`（1 行）= `MoveBackToHand` 由 C 推 B（分叉落地的记录）。
⇒ 同步点按 `16·8` 的口径跑 `check_ledger_points --all-A --fix`（**必须在没有 `.cs` 写手时**），之后复跑一次。

---

## §五 顺手发现 / 改动请求（⚠️ 其中一条会让自检变红）

1. ✅ **`Editor/BattleScene.cs` 的 R16② 已按授权改好**（调度台 2026-10-26 同意改这一处）：原断言断的是「敌方从没设过打法 ⇒ `OwnAttackType == None`、近战数值回基准」，**那是 `974` 未做时的行为**；现改成**新口径** + 注释写清「这条断言原来的口径是 974 未做时的行为」：
   - ① `OwnAttackType == (两围推出的那一档)` 且该档底光亮（判据 `decomp_full/CardScript__UpdateAttackText.c:21-67`，严格小于 ⇒ 相等落 `Melee`）
   - ② **该档**数值 = 基准 × 倍率
   - ③ **另一档**数值回基准（**灭自证**：「两边一起放大」过不了这一条）
   `BattleScene.cs` 现在也是本件改过的第二个文件（`typecheck` 编辑器程序集 0/0 已过）；⛔ 我**没跑** `BattleScene.Run`。
2. **`HUD:107` 的落点可以收紧**：`BattleCardUI.ChangedLanguage()`（现 A，落点写 `BuildTextLayers`）→ 建议改成 `Core/CardView.cs:14706（RefreshLocalizedText）` —— 那是 `W2K03` 新加的**可调用**等价口（本代理不改主表）。
3. ✅ **`MoveBackToHand` 分叉已按裁定落地**（调度台 2026-10-26：「按 `animateFeel` 分叉，⛔ 别只留一条再注释了事」）：取消出牌那一支（`ResolvePlayTargetByUnit`）现在 —— `animateFeel` **真** ⇒ `CardFeel.ReturnToHand`（= 原版 `_TransformUnitInPlayToCard` 的反向 clip，0.75 s；形态换档 / 挂父件 / `SyncHand` 重排排在 `onDone`）；**假** ⇒ `CardFeel.MoveBackToHand`（= `MoveBackToHand.c:12-30`：`SetSortingLayer(Hand)` + `StopAnimsInPlay` + α→1 + 关 `body3D`，**无动画**）。账 `MoveBackToHand` 已由 C 推 **B**（仍欠的原版入口 = `OutFromPlayingAreaAnim`，我们**没有「出牌区」** ⇒ 无落点）。
   ⚠️ **真机边界（已写进账）**：`animateFeel` 真时那 0.75 s 里若 `RefreshAll` 触发 `SyncHand`，会把这张视图提前捞回手牌形态。
4. **④ 那条 grep 的结果**（调度台要的确认）：全仓 `TooltipsEnabled = true` 只有 **3 处**，**没有一处是「遍历整排」**——
   - `CardView.cs:13341` = **字段默认值**（`public bool TooltipsEnabled = true;`）
   - `CardInteraction.cs:1943` = 抬起来**那一张**（`ApplyShowInHand`）
   - `CardDisplayWindow.cs:1614` = 详情窗里的**主卡**（相关卡 `:1613` 关）
   关的那些：`BattleDriver.cs:26726`（手牌建卡）· `CardInteraction.cs:1938/1958`（放回 / 换列表）· `CardDisplayWindow.cs:1446`（`i == 0`）⇒ **手牌那一族已经收口**。
   🟡 **顺手发现（未改，需裁定）**：`CardView` 的**字段默认值是 `true`**，而棋盘视图的建件路**没有**任何地方把它关掉 ⇒ 悬停**场上单位**也会弹关键词 tip。原版 `BattleCardManager.CreateCard` 是「建卡时关、抬牌才开」，而抬牌只发生在手牌 ⇒ 照原版口径场上单位**不该**有 tip。改法有两种（改 `CardView` 默认值 / 在棋盘建件处显式关），都会碰到 `CardView` 或影响别的窗口，**不在本件白名单** ⇒ 只记不猜。
5. **`jam` 全池 0 张可达**（`CardDef.KeywordTable.Jam` 现读 0 张带它）⇒ 本批那条 glitch 支是**结构补齐**、今天看不见任何变化。别把它当「已经验过」。
6. **`WatchHandFull` 是旁证式观测**（`RuleCore.EnforceHandLimit` 只 `ctx.Log`、不发事件）—— 它**不能**拿来冒充弃牌 / 偷牌那两条揭示链的触发源（见 §二 B 组）。
7. 🔴 **棋盘按压这一族有可见行为变化（是原版行为，但玩家能看见）**：① 按住同一张卡 0.3 s 弹出的大卡窗，**松手不再关掉**（改前会「弹→立刻关」闪一下）；② **点我方有主动技的单位**现在走「填技能面板 + 试放主动技」，**能当场放掉的技能点一下就放了**（不再开大卡窗）—— 没主动技的单位照旧开窗（🔶 这条分派口径是**我们自己定的**，原版那颗组件在卡 prefab 的子节点上、命中矩形本地拿不到）。真机请按这两条核手感，若与用户预期不符要回来改分派。
8. `wa.IsPlayer` **只在按下那一沿**写（`BoardCardTouchDown`）⇒ 不经过 touch-down 的自检口 `SimulateTapUnit` 行为**逐字不变**（`OnTouchUpAsButton` 第一句就被 `isPlayer` 挡掉）。⚠️ 将来谁在自检里「先按一下再轻点」，会走进主动技那条链 —— 那时要显式把 `IsPlayer` 复位。

---

## §六 要真机核的 / 等数据重烘的

### 要真机核（本批**批处理里看不见**的）

| # | 验什么 | 怎么验 |
|---|---|---|
| 1 | **结算 2.0 s 自动离房 + 亮面板**（`HUD:702`） | 打完最后一击停手 ⇒ 约 2.0 s 后结算面板自动出；联机局对面在同一刻收到「对手已离开」 |
| 2 | **悬停手牌只有那一张弹 tip**（`卡面:21`） | 鼠标扫过整排手牌 ⇒ 只有**抬起来/悬停**那一张出关键词 tip（改前整排都出） |
| 3 | **打死单位后不可点**（`卡面:27`） | 打死一个单位，在 0.7 s 消散演出里点它 ⇒ 不应再选中/开窗；**残骸**仍可点 |
| 4 | **敌方攻击型底光**（`974` ①） | 进一局看敌方单位：melee>ranged 的近战底光亮、反之远程亮（改前两颗都不亮） |
| 5 | **数值持续色**（`974` ③） | 给一个单位加/减攻击或伤害它 ⇒ 那一格**持续**绿/红，回到基准才白（改前是「变一下闪一下」） |
| 6 | **牌堆 SDF 跟卡背**（`HUD:101`） | 换一个非默认卡背进战斗 ⇒ 牌堆底下那圈「厚度/投影」跟着换（改前恒阵营默认） |
| 7 | **换语言不重启**（`HUD:189`） | 局内切语言 ⇒ 手牌 / 场上卡的卡名·描述·兵种·阵营逐张变，数值不变 |
| 8 | **卡背释放**（`HUD:720`） | 打完一局再开一局 ⇒ 卡背照常显示（`ReleasedCardbackCount` 那两条是自检口） |
| 9 | **按住棋盘单位 0.3 s**（`HUD:686`） | 按住不动约 0.3 s ⇒ 弹大卡窗；**松手它不再关掉**（改前会闪一下） |
| 10 | **点我方有主动技的单位**（`HUD:684`） | 轻点 ⇒ 技能面板出来；技能不需要选目标 ⇒ **当场放掉**（不是进选目标）；没有主动技的单位 ⇒ 照旧开大卡窗 |
| 11 | **抬牌「可用目标」后缀**（`HUD:148`） | 鼠标抬起一张手牌 ⇒ 描述尾部出现 `（目标卡名）` 或 `N 个可用目标`；不该印的卡**什么都不印**（⛔ 不是印空串） |

### 等数据重烘 / 等别的包（本件已把落点与判据备好）

| # | 等什么 | 谁 |
|---|---|---|
| 1 | `card→altArt[]`（7 张）+ `PlayerDeck.CardIds` 的「异画副本」格 | `工具/gen_cards_engine.py` · `RuleEngine/Core/DeckRules.cs`（`HUD:92/93` 才会真生效） |
| 2 | ability 级 `targetsAvailableCounter(+0x40)` 列 | `工具/gen_cards_truth.py` · `CardDef` · `CardDatabase`（`HUD:20`） |
| 3 | `unitsShake` 110 条并进 `Resources/card_anim_order.json` **且**引擎补 action 472 | 数据重烘 + `RuleEngine`（`732`） |
| 4 | 弃牌 / 偷牌一条表现信号（`EvtKind` 里不能凭空加） | `RuleEngine/Core/EffectResolver.cs`（`921`） |
| 5 | 引擎 `savedMana` 一格 | `RuleEngine/Core/PlayerState.cs` + `RuleCore.BeginTurn/EndTurn`（`HUD:537/548`） |
| 6 | `SerializedPlayerDeckData` DTO（序列化 + 反序列化） | `RuleEngine/Core/DeckRules.cs`（`卡组:49/50/54`） |
| 7 | 录像头记「本次选择候选集」 | 录像格式（`HUD:364/378`） |
| 8 | 开包窗 / 换牌面板的 tooltip 整片关 | `Shell/DraftModeWindow.cs` · `Battle/MulliganPanel.cs`（`卡面:21` 余下两处） |
| 9 | ~~`BattleScene.cs` 的 R16② 期望~~ ✅ **已改**（调度台授权，见 §五·1） | —— |
| 10 | `PressedBtn` 那颗按钮的**层**（0.4 s 按压反馈的可见部分） | 节点名 / rect / sprite 全库零实例（`CardPrefab` 那 4 个子 GO 未导出）⇒ 钩子 `PressedBtnSetActive` 不接时状态机**出声一次**；建层要到那份 prefab 导出之后 |
