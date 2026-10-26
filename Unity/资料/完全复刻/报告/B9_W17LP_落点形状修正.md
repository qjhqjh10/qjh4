# B9_W17LP —— A 行「落点形状」修正（13 条）

> 会话：第八十四会话（2026-10-26 ~ 10-27）· 执行型账务写手 `W17LP`
> 范围：**只改账**（`账/_parts/B9_W17LP.tsv`）。⛔ 未碰任何 `.cs` / 资产 / 场景 / git / 主账表。
> 起点：`check_ledger_points.py --all-A` 报 **失败 11 · 漂移 24**；本次处理 = 任务清单里的 **11 条失败 + 2 条漂移**（共 13 行）。
> 所有落点均按**二进制读源码 + 按 `\r\n`/`\n` 切行**复核（⛔ 不用文本模式，避免裸 CR 偏行号）。行尾实读：`RuleCore.cs`/`MainMenuRuntime.cs`/`DraftModeWindow.cs`/`PrebuiltDecks.cs`/`DailyData.cs` 为 LF，`EffectResolver.cs`/`BattleDriver.cs`/`CardInteraction.cs` 为 CRLF；**裸 CR 全部为 0**。

**结论：失败 11 → 0；漂移 24 → 22（本次涉及的 2 条已消掉，余下 22 条是任务书范围外的「歧义 / getter」漂移）。没有一条降档 B。**

---

## 一、逐条：旧落点 → 新落点

格式：`账行` · 原版方法 · 旧落点 → 新落点 · **为什么新的是声明行**

### 1. `卡组编辑.tsv:261` · `DrafModeDeckInfoCardListPanel.OnEnable()`
- 旧：`CardPresentation/Shell/DraftModeWindow.cs:1880-1889（_deckWarlordHit）（等价实现 = _deckWarlordHit：原版行为落在这段代码里）`
  （漂移报「实际在 1889」，而 **1889 本身是赋值语句** `_deckWarlordHit = MenuDraw.Hit(…);`，不是声明 —— 闸也因此打 `NEED-HUMAN`）
- 新：`CardPresentation/Shell/DraftModeWindow.cs:1880（BuildCardListPage）（等价实现 = BuildCardListPage：…）`
- 声明行实读：`1880  void BuildCardListPage(Transform holder)`。`_deckWarlordHit` 的创建/绑定（:1889）就在这个方法体内。
- 语义核对：原版 `DrafModeDeckInfoCardListPanel__OnEnable.c` 体 = 取 `[param_1+0x30]+0x100` 的 `UnityEvent` 再 `UnityEvent__AddListener(UIGenericEventCatcher delegate)` ⇒ 就是「把那颗按钮的点击绑上」。我们建 Hit 时直接绑 `OpenWarlordCard` ⇒ 同一件事，只是不做原版「每次 Enable 挂 / Disable·Destroy 摘」。

### 2. `菜单_外壳.tsv:521` · `OpenWindowButton.OpenWindow()`
- 旧：`CardPresentation/Shell/MainMenuRuntime.cs:1561-1620（OpenByRef）（等价实现 = OpenByRef：…）`
  （漂移报「实际在 1628」；而 `:1561` 当时落在 `NavPanelXForTest` 的 getter 体内）
- 新：`CardPresentation/Shell/MainMenuRuntime.cs:1628（OpenByRef）（等价实现 = OpenByRef：… 方法体只一行转调唯一实现 `WindowsManager.OpenByRef`（Shell/WindowsManager.cs:914）…）`
- 声明行实读：`1628  static T OpenByRef<T>(string prefabRef, System.Func<WindowsManager, T> create, bool closeAll = false)`。
- 语义核对：本文件 8 条开窗入口统一走它；真方法体在 `Shell/WindowsManager.cs:914 public static T OpenByRef<T>(…)`（查 `_openByRef` 缓存 → 命中复用 / 关过新建 → `wm.OpenWindow(win, null, closeAll)`）。已在「等价实现」里点明真正体在哪，⛔ 不再让落点看起来像「这里就是实现」。

### 3. `卡组编辑.tsv:284` · `PrebuiltDeckCollection.AddDeck(PrebuiltDeck deck)`
- 旧：`CardPresentation/Shell/PrebuiltDecks.cs:608（Decks）/ :621（BotDecks）/ :597（MetaOf）（…）`
  （失败：括号里写了 `Decks` 却「找不到这份声明」—— 因为 `608 public static IList<Deck> Decks` 是**属性名单独一行、`{` 在下一行**，闸的候选正则要求名字后面紧跟 `(` `<` `{` `;` `=`，够不着 ⇒ 不可判定）
- 新：`CardPresentation/Shell/PrebuiltDecks.cs:597（MetaOf）（等价实现 = MetaOf + 生成期分表：…运行期 `Decks`(:608) 直接取 decks 表、`BotDecks`(:621) 用本方法 `type == 0` 过滤 ⇒ 与原版 `AddDeck` 的四步逐条等价）`
- 声明行实读：`597  public static DeckMeta MetaOf(Deck d)`。
- 三个名字的分工（原落点想表达的）保留在「等价实现」散文里：生成期按原版同一组字段分三表 → `Decks`(:608 属性，实质是取 `_lists.decks`)、`BotDecks`(:621 属性，用 `MetaOf` 的 `type` 过滤)、`MetaOf`(:597 方法，取分类位)。**这是任务书说的「共享落点」写法：单一声明名 + 散文说清各自如何满足。**

### 4. `战斗逻辑驱动.tsv:120` · `BattleManager.AddAttackContinuationAction(BattleAction baseAction)`
- 旧：`RuleEngine/Core/RuleCore.cs:6488（DeclareAttackCore 的 landing 支）（…）` —— **6488 是注释行**（`//     当时判成「取消 = return RuleCodes.OK」…`）
- 新：`RuleEngine/Core/RuleCore.cs:6256（DeclareAttackCore）（等价实现 = DeclareAttackCore 的 landing 支：`:6526 if (openedPod && attacker.Has(KeywordTable.Landing))` ⇒ … `:6533 EnqueueAbility(… "落地续攻:…")` …）`
- 声明行实读：`6256 static int DeclareAttackCore(BattleContext ctx, int p, int atkSlot,`（6256-6259 一条签名）。
- 语义核对：landing 支在 :6526-6564（判断 → `ctx.Log` 报「取消这一记」→ 入队 → `return RuleCodes.OK`），整段都在 `DeclareAttackCore` 体内。

### 5. `战斗逻辑驱动.tsv:139` · `BattleManager.AddFollowResolutionAttackContinuationAction(BattleAction baseAction)`
- 旧：`RuleEngine/Core/RuleCore.cs:6218（DeclareAttackCore 的伏击支）（…）` —— **6218 是 `DeclareAttack` 的 doc 注释行**（`///`），甚至不在 `DeclareAttackCore` 里
- 新：`RuleEngine/Core/RuleCore.cs:6256（DeclareAttackCore）（等价实现 = DeclareAttackCore 的伏击支：`:6371 EnqueueAbility(… "伏击续攻:…")` …）`
- 与第 4 条**共用同一个落点**（原版两个方法 = 我们同一方法的两个支：:6371 伏击 / :6533 landing）—— 按任务书「共享落点」写法，在散文里说清如何分别满足。

### 6. `战斗逻辑驱动.tsv:542` · `BattleManager.ResolveStun(BattleAction battleAction)`
- 旧：`RuleEngine/Core/EffectResolver.cs:4066-4192（DoStun，含 `AddEffect` 闸②/④ 与代词目标的旧对象回映射）（等价实现 = DoStun）` —— **4066 是注释行**（`//   进了墓地的没必要再挂）…`）
- 新：`RuleEngine/Core/EffectResolver.cs:4158（DoStun）（等价实现 = DoStun：`stun` op 的唯一施加口（`:1056 {"stun", … => DoStun(…)}`）…）`
- 声明行实读：`4158 static bool DoStun(BattleContext ctx, int owner, string by, EffectOp op,`。
- 语义核对：判定口 = `RuleCore.StunBlockedByTraits`（:4250，对应 `CardScript__Stun.c` 的 `HasCurrentTrait(0xfa)`）；施加 = `t.AddKeyword(KeywordTable.Stun, 1)`（:4264，对应 `AddTraitSilently(param_1,100,…)`）；广播 = `EmitStatusApplied`（:4268，对应 `DisplayStatusAnim`）。

### 7. `战斗逻辑驱动.tsv:933` · `CardScript.Stun(bool isPlayerTurn, BattleAction battleAction)`
- 旧：`RuleEngine/Core/EffectResolver.cs:4066-4192（DoStun 的闸与施加段；判定口共用 `RuleCore.StunBlockedByTraits`）（等价实现 = DoStun）` —— 同上，**注释行**
- 新：`RuleEngine/Core/EffectResolver.cs:4158（DoStun）（等价实现 = DoStun：原版 `CardScript.Stun(isPlayerTurn, battleAction)` 的闸与施加段就是本方法 …）`
- 与第 6 条**共用同一个落点**（同一条施加口）—— 原版 `CardScript.Stun` 与 `BattleManager.ResolveStun` 都汇到这一处。

### 8. `战斗逻辑驱动.tsv:890` · `CardScript.SetAsUnitInPlay(bool setVisibility)`
- 旧：`CardPresentation/Battle/BattleDriver.cs:13279-13293（等价实现 = EnterBoardBody）；视图集合那一半 = :25302-25629（SyncBoard）· 新建就用场上形态 = :25428-25432`
  —— **13279-13293 是 `HitUnitSlotForTest` 的 doc 注释**；而且 `:25302-25629` / `:25428-25432` 两处都**与本题无关**（现读是 `ShowCardInCenter` 的注释与 `PlayedCardCenterHoldMul` 一类常量）⇒ 这两个引用是**过期/串行**的，已删。
- 新：`CardPresentation/Battle/BattleDriver.cs:13564（EnterBoardBody）（等价实现 = EnterBoardBody：…飞行交接 `OnDeployHandoff`(:13548) 与程序化出牌 `DoPlay`(:13683 → 调用于 :13834) 都走它。视图集合那一半 = `SyncBoard`(:25812)…）`
- 声明行实读：`13564  void EnterBoardBody(CardView card)`；两处调用点 :13551（`OnDeployHandoff` 内）与 :13834（`DoPlay` 内）。

### 9. `战斗逻辑驱动.tsv:891` · `CardScript.SetAsUnitTransforming()`
- 旧：`CardPresentation/Battle/BattleDriver.cs:25302-25629（等价实现 = SyncBoard，残骸体那一支 = :25515-25517）；不消散那一支 = :24341-24346（PlayDeathFeel 的残骸守卫）；视图侧读点 = CardPresentation/Core/CardView.cs:2664（RemnantBodyVisible）`
  —— **25302-25629 与残骸体无关**（现读整段是 `ShowCardInCenter` 的注释 + 一堆 `AutoPlayCenter*` / `PlayedCardCenter*` 常量），`SyncBoard` 实际声明在 **25812** ⇒ 旧落点**指错了整段**。
- 新：`CardPresentation/Battle/BattleDriver.cs:25812（SyncBoard）（等价实现 = SyncBoard 的残骸体支：`:26025 RemnantPrefabOf(u)` → `:26031 StopInPlayAnims()` → `:26032 SetRemnantBody(true, remPrefab)` / `:26033` 撤 … 视图侧落地 = `Core/CardView.cs` 的 `SetRemnantBody`(:2537)…）`
- 声明行实读：`25812  void SyncBoard(int owner, Dictionary<int, CardView> views, bool mine)`；残骸体那一支在 :26020-26033。
- 视图侧改指 `CardView.SetRemnantBody`（`:2537 public void SetRemnantBody(bool on, string prefabName)`）而不是 `RemnantBodyVisible`（:2664 是**一行式属性 getter、且注释写明「自检用它」**）—— getter / 自检口不该当「实现」的证据。

### 10. `战斗逻辑驱动.tsv:1165` · `PlayerHand.GetMulliganCardsToDiscard()`
- 旧：`RuleEngine/Core/RuleCore.cs:440-459（等价实现 = Mulligan：弃回牌库 → 重洗 → 补抽）· 同文件:13810-13836（MulliganCollectIndices）`
  —— **440 是 doc 注释**；且 `MulliganCollectIndices` 的旧行号 **:13810-13836 已过期**（现读声明在 **:14131**）。
- 新：`RuleEngine/Core/RuleCore.cs:444（Mulligan）（等价实现 = Mulligan：… 「要弃哪几张」的整批校验在 `MulliganCollectIndices`(:14131)…）`
- 声明行实读：`444 public static int Mulligan(BattleContext ctx, int player, IList<int> handIndices)`；`14131 static List<int> MulliganCollectIndices(PlayerState ps, IList<int> handIndices, out List<string> bad)`。

### 11. `战斗逻辑驱动.tsv:1183` · `PlayerHand.RemoveCard(CardScript card)`
- 旧：`CardPresentation/Hand/CardInteraction.cs:1331-1331（等价实现 = Release）` —— **1331 是 `ResolveDrop` 的 doc 注释**
- 新：`CardPresentation/Hand/CardInteraction.cs:1355（Release）（等价实现 = Release：松手合法时 `_cards.Remove(card)`(:1410) + `_placed[land] = card`(:1408) …）`
- 声明行实读：`1355  void Release(Vector3 world)`；「从手牌列表里摘掉」那两句在方法体内（:1408 / :1410），被拒时走回弹、不摘。

### 12. `日常_奖励.tsv:430` · `MainMenuMission.Initialize()`
- 旧：`Shell/MainMenuRuntime.cs:657（Build 末尾）（等价实现 = …本体 = DailyData.cs:2395）` —— **657 是 `Build()` 里的语句行** `DailyData.TrySchedulePopup();`
- 新：`CardPresentation/Shell/DailyData.cs:2395（TrySchedulePopup）（等价实现 = TrySchedulePopup：… 主菜单那一拍由 `MainMenuRuntime.Build()`(:401) 末尾 :657 调它触发）`
- 声明行实读：`2395 public static bool TrySchedulePopup()`；`MainMenuRuntime.cs:401 public void Build()`（401-约 690，657 在体内）。
- 语义核对：原版 `MainMenuMission__Initialize.c` 的判据体只有「`base.Initialize()` + 尾调 `TrySchedulePopup()`」⇒ 行为本体就是 `TrySchedulePopup`。

### 13. `日常_奖励.tsv:432` · `MainMenuMission.UpdateSaveGame(LiveOpsEventPersistence save)`
- 旧：`Shell/DailyData.cs:2064（Persist 写成功之后）（…）` —— **2064 是方法体中段的语句行** `TrySchedulePopup();`
- 新：`CardPresentation/Shell/DailyData.cs:2034（Persist）（等价实现 = Persist：… 写成功之后 :2064 调 `TrySchedulePopup()` 重排 ⇒ 等价；`StoreOn == false`（自检常态）早退 …）`
- 声明行实读：`2034  static void Persist(string why)`；尾调在方法体内 :2064。

---

## 二、改判 B 的条目

**无。** 13 条全都找到了**方法级声明行**（方法 / 表达式体方法），没有一条只能落到字段或注释 ⇒ 按任务书 §3 不需要降档。
唯一两条「属性名单独一行」的（`PrebuiltDecks.Decks` :608 / `BotDecks` :621）也换成了同族的方法级声明 `MetaOf`(:597)，并在散文里保留三个名字的分工说明。

---

## 三、三项自检（全部在**主表副本**上做，⛔ 未碰主表、未合并）

做法：把 `账/*.tsv` 全量复制到 `d:/4/_tmp_view/w17lp_b9/ledger/`，用**真** `merge_ledger_parts.py`（`--only B9_W17LP --parts … --ledger …`）把补丁合进副本（**更新 13 / 新增 0 / 无变化 0**，四份主表行尾仍 LF），再在副本上跑闸。

| 自检 | 结果 |
|---|---|
| ① `check_ledger_points.py`（对 21 份副本账全跑） | **失败 0**（原 11）· 漂移 **22**（原 24；本次 2 条已消）· A 3595 · 通过 3573（告警 114，原 124）|
| ② `verify_annot.py`（同逻辑副本，`D` 指向副本目录） | **缺注解 0**（同名 838 · 异名已注解 2757）|
| ③ `check_patch_conflicts.py`（**不带** `--all`） | **✅ 没有跨补丁重复键（1 份补丁）** |

补丁本身：`账/_parts/B9_W17LP.tsv` · 14 行（1 表头 + 13 行）· 11 列 · UTF-8 **无 BOM** · **LF** · `方法` 列全部是**完整签名带 `(...)`**。

---

## 四、留给调度台的提示

1. 合并命令（**同步点、无 `.cs` 写手时**）：
   `python -I -X utf8 d:/4/Unity/工具/merge_ledger_parts.py --only B9_W17LP`
   合并后**立刻**把补丁移进 `账/_parts/_merged_B9_W17LP/`（否则是「旧内容定时炸弹」）。
2. 合并后 **不需要** 再跑 `check_ledger_points --fix`：这 13 行的名字都能被 `find_decl_candidates` 唯一命中且与账内行号一致（逐条实测过）。
3. 余下 **22 条漂移不在本次范围**：其中 1 条是真相漂移（`MissionEvent.GetNextRefreshTime` 的 getter `NextResetAt` 1644→1663），21 条是「同名多声明 ⇒ 歧义」（`SetHint` / `SetVis` / `FieldAttack` / `FireTriggerOnSide` / `DeployFree` / `PlayCard` / `CanUseAbility` / `ViewAt` / `LiveOps` 那族 / `GetHandler`），都要人判，`--fix` 不碰。
4. 顺带发现（未改，交调度台分流）：`卡组编辑.tsv:259/260`（`OnDestroy` / `OnDisable`）双双落在 `RebuildDeckListRows`(:2777) —— 形状上过关，但那是「重建行」的方法，与「销毁 / 失活」语义存疑，建议下一轮复核。
