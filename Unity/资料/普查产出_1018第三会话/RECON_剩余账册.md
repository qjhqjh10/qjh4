# RECON · 剩余账册（第三会话 · 逐条现核）

> 只读现核 · 2026-10-18 · **只碰本文件**。⛔ 没跑 Unity / 没动 git / 没改任何生产代码。
> 🔴 **行号一律只是读数时的坐标**（本仓每轮都在漂）—— **落点一律按符号认**。
> ⚠️ **重要前提**：本仓**工作区里有未提交的写手改动**（17 个文件 M）。本会话的写手在 `06:55–07:00` 之间落了盘，
> **调度台中间账（06:51）之后又有一批完成了** ⇒ 本报告以**现读工作区**为准，与中间账不一致处**逐条标出**。

---

## 一、✅ 可销账（已做，只是记录过期）

| 编号 | 一句话 | 证据（文件:符号） |
|---|---|---|
| `A947` | ✅ **已做**：`noncombatant`(880) 查询时禁令已补，与 `cantattack`(150) **共用一个口** `AttackBannedByTraits`；断言 `TestA947NoncombatantAttackBan` 在 | `RuleEngine/Core/RuleCore.cs:2064 Noncombatant` · `:2083 AttackBannedByTraits` · `RuleEngine/Editor/RuleEngineTest.cs:472` 段 |
| `A858` | ✅ **已做**：`DiedThisTurn++` 提到反噬分支**之前**（原版「变残骸支也算」）；断言 `TestDiedThisTurnDuringBacklash`；注释已按 R-ENG 判据重写 | `RuleEngine/Core/RuleCore.cs:3169-3187` · `:3225` 注释 |
| `A963③` | ✅ **止血件已落**：新增 `HandTroopCriteriaByName`（按卡名收窄），⛔ 没动共用件 `HandTroopCriteria` | `RuleEngine/Core/EffectResolver.cs:2805` · 断言 `RuleEngineTest.cs` 的 `TestA963HandListenerSelfDoesNotSpread` |
| `A974` | ✅ **已做**：两条路都把 `NotePlayed` 挪到广播**之后**（战术 `EffectResolver` 与单位 `RuleCore`）；含次序扫描断言 | `RuleEngine/Core/EffectResolver.cs:1060-1072` · `RuleEngine/Core/RuleCore.cs` 的 `PlayCard` 支 · `RuleEngineTest.cs:20421 TestA974NotePlayedAfterPlayBroadcast` |
| `A976` | ✅ **已做**：`LastCreated.Clear()` 已加在 `ResolveOps` **入口**（族内共 7 处）；含「跨卡不抢班」判别式 | `RuleEngine/Core/EffectResolver.cs:114` · `RuleEngineTest.cs` 的 `TestA976LastCreatedNotCarriedAcrossCards` |
| `A949` | ✅ **两半都了结**：① `UnitState` 构造句已转调 `HasDeployExemption` ② `EffectResolver` 那处 **F7 已判定「不是同一条判据」**（手写的是「按卡面文本预判即将给的豁免」，合并会永久不成立） | `RuleEngine/Core/UnitState.cs:330` · `RuleEngine/Core/EffectResolver.cs:4578-4584`（订正注释）· `RuleEngine/Core/RuleCore.cs:1387` |
| `A950` | ✅ **夹具已建**：手牌灌水夹具 `FillTo(int)` 已在，**7 张（装得下 ⇒ 一点也不让）与 9 张（压缩态 ⇒ 让位）两态都跑到了** | `Editor/BattleScene.cs:10474 FillTo` · `:10515 FillTo(7)` · `:10535 FillTo(9)` |
| `A951` | ✅ **判据齐、结论「够，不用动」**（`chooseone`/`chooseeffect` 各有端到端测试；`showAsk` 都弹） | `资料/普查产出_1018/A951_chooseone_chooseeffect_判据.md` §〇 · `Battle/BattleDriver.cs:4525 ChooseOptionStableId` doc |
| `A823` | ✅ **已答**（并进 `A828`）：`collisionEvent` 已放宽到 18 层 ⇒ **我们数据里现在真有订阅** | `数据/游戏数据/animfx_modules.json`：`collisionEvent.m_PersistentCalls` 键 **5906** 条 · `…m_Calls[n].m_MethodName` **563** 条（原版 559，同量级） |
| `A826 尾巴` | ✅ **三处过期注释都已就地订正**（「素材还没进工程」×2 · 「不在表里」×1），且都写了「已过期 + 现读事实」 | `Shell/RewardWindow.cs:399` · `:415` · `:545`；另 `:460` 那句是**条件式**（「素材还没进工程时它是唯一能验…的量」），非过期陈述 |
| `A967` | ✅ **已修**：`WingName = "Nine"` 那一行**已删**（落到缺省 `Header Background (1)`），文件头写明「⛔ 别加回来」 | `Shell/LiveOpsEventWindow.cs:805-811`（原行位置的订正注释）· `:783` |
| `A968` | ✅ **已修**：`TitleMode` 从 `SpacingOnly` 改成 **`FitAfterSpacing`**，并补 `TitleFitW/H = H_Title.W/H`（缺省会写字宽 0 ⇒ 竖排） | `Shell/DailyStreakPopup.cs` 的 `BuildHeader`（`TitleMode = WindowHeader.TitleFit.FitAfterSpacing` + `TitleFitW/H`）· `:504-544` 判据注释 |
| `A969` | ✅ **已修**：裸关键词那一支改用**未包 link 的 `spriteTag`**、外面**只包一层** ⇒ 形状 = `<link=X><sprite name="X">词</link>`；**幂等判据同步换成新形状 `bare` 本身** | `Core/CardIcons.cs` 的 `Rewrite`（`bare` 那条支 + 上面两段 2026-10-20 注释） |
| `A980` | ✅ **已改**：`②c/②e/②f/②f2/②f3` 全部重写（`②c` 夹具改成**卡池真文本**、不再自抄）；⚠️ 现多出一条**显式已知红** ⇒ 见 §五·4 | `Editor/IconSetup.cs:261-296`（`②c` 重写注释 + 两条 `Check`）· `:301-354` |
| `A811`（根治那一半） | ✅ **已做**：框的中心改成**宿主写进节点的设计矩形**（`_baseRect`），不读实时 transform；`ApplyPxRect` 尾句穿透写记录；两个可观测计数 | `Shell/ViewportClip.cs:201 _baseRect` · `:216 SetBaseRect` · `:225 CaptureNow` · `:273` 取 `_baseRect.CX/CY` · `Shell/MenuDraw.cs:187 GetComponent<ViewportClip>()` |
| `A678` | ✅ **判据取到了**（原版解包实读）⇒ `align: 2`（右）**对得上**。详见 §四·1 | d:/2 `bundle_menus_assets_all`：`Wildcard Drawer>Content>Label>Quantity` = `对齐=Right/Middle` |
| `A846` / `A847` | ✅ **已做**：`W23` 落了 9 个调用点/11 颗标签（含 `bottom`）；`VOffsetBlockWorld`（多行块高）已在 | `资料/普查产出_1016/W23_A712补漏.md` · `Battle/Label.cs:1494 VOffsetBlockWorld` · `:1573` |
| `A712 · Label.cs 过期记录` | ✅ **已就地订正**（W23 那天改的）：`Label.cs` 那句「只有两处 `anchor.y != 0.5`」已改写 | `Battle/Label.cs:1612-1615`（「🔴 2026-10-16 就地订正（W23 · 铁律 5）」） |
| `A985①` | ✅ **已做**（本会话写手刚落的，中间账之后）：两处 `Describe` 调用点都已包 `Loc.T` | `Deck/DeckEditorState.cs:273 why = Loc.T(DeckRules.Describe(e));` · `Shell/DeckInfoPopup.cs:577 Loc.T(DeckRules.Describe(…))` |
| `A932` | ✅ **已做**：`SearchingOpponentWindow` 已加**自建提示行**（`NetMatchmaking.OnHint` 的消费方，含「这是我们挑的、不是原版的做法」+ 长度钳共用 `NetProtocol.MaxPeerTextChars`） | `Shell/SearchingOpponentWindow.cs:26` · `:60-85`（`OnHint` 消费方 + 只读口） |
| `A962②` | ✅ 已做（调度台已记） | `RuleEngineTest.cs` 的 `TestS10CompanyMasterLowerCost` |
| `A965` | ✅ 已做（调度台已记，本条不重查） | `Editor/CardFaceProbe.cs` 取槽 |
| `A978` | ✅ **无缺陷**（词表已认）；「规范化」属新账 | `RuleEngine/Core/GivePayload.cs:166` 词表 |

> ⚠️ **与 `调度台_中间账.md` 冲突的一批**：中间账（06:51）把 `A967 / A968 / A969 / A980 / A947 / A858 / A963③ / A974 / A976` 记成 ❌未做，
> **现读全部已落盘**（文件 mtime 06:55–06:58 > 中间账 06:51）⇒ **那 9 条可销**。

---

## 二、⏭ 仍开着

### `A971` —— 措辞订正（纯注释）· **判据齐 · 可直接派**
- **现读 = 11 处，不是 10 处**（中间账漏了第 11 处）：
  - `Battle/BattleDriver.cs` **9 处**：`PlayTutorialSound` 段（「聊天那三档不走这里」）· `SpeakTutorialChat` 段 · 日志串 · `ApplyTutorialActionView` 段 · `PlayTutorialSound` 跳过说明 · `②′` 小节标题。
  - `Battle/UnitChatPanel.cs` 1 处（第三颗气泡 `RadioChat` 那条 doc）。
  - 🆕 `Shell/TutorialModePopup.cs` **1 处**（`聊天三档含 RadioChat / 音效 / …`）—— **账上没有**，一并订正。
- 五档实据已是定论（50/55/60/65/90）；⛔ **别动那 6 处「零接线」的历史/断言文案**。
- 落点：纯注释 ⇒ 零条自检（真要跑跟一次 `BattleScene.Run`）。

### `A338 卡组子集 5 处 + A895 5 处` —— 纯注释 · **判据齐 · 可直接派**
- 落点（按符号认，⛔ 不抄行号）：卡组子集 5 处 = `Shell/PointerLayer.cs` · `Shell/MenuScroll.cs` · `Shell/CollectionWindow.cs` · `Editor/DeckScene.cs` · `Editor/BattleScene.cs`；
  `A895` 5 处 = `Editor/BattleScene.cs`。
- ⛔ **「≈44 处」那批不能派**（R-ENG 已判那笔账本身不成立）。
- ⚠️ 改动面跨 3 个自检宿主 ⇒ 按覆盖面跑 `DeckScene.Run` + `BattleScene.Run` + `CollectionScene.Run`。

### `A451` —— **残余只在 `Shell/` 外？不，`Shell/` 里还有 7 个文件**
- ✅ **X2 那 13 件/27 处已做完**（W7：白名单 18 件 CS1570 归零；全程序集 100→6，剩 6 条全在 `Assets/WarpforgeVFX/Runtime/` 的 3 个文件，**白名单外**）。
- 🆕 **静态扫（本会话跑，⛔ 不是编译器）：`Shell/` 里 `///` doc 块含裸 `<` 的 = 9 块 / 7 文件 / 36 处**：
  `AllianceMemberOptionsPopup.cs` · `AlliancesTab.cs`(2) · `FriendsTab.cs` · `SettingsWindow.cs`(3) · `SocialWindow.cs`(2) · `TrophyInfoPopup.cs`(3) · `TutorialModePopup.cs`。
- **可复现命令**（只读，无副作用）：
  ```bash
  python -c "import io,os,re;root=r'd:/4/Unity/MyGame/Assets/CardPresentation/Shell';
  bad=lambda s,c:sum(1 for m in re.finditer(c,s) if not re.match(r'<[A-Za-z/!?]' if c=='<' else r'&(#\d+|#x[0-9A-Fa-f]+|[A-Za-z][A-Za-z0-9]*);', s[m.start():]));
  [print(f,l) for f in sorted(os.listdir(root)) if f.endswith('.cs') for l,ln in enumerate(io.open(os.path.join(root,f),encoding='utf-8',errors='replace').read().split('\n'),1) if ln.lstrip().startswith('///') and (bad(ln,'<')+bad(ln,'&'))]"
  ```
- ⚠️ **判据仍未跑过**（全仓无带 `XML doc 警告数` 读数的日志）⇒ **这 36 处到底是真 CS1570 还是白扫，只有一次 `WF_DOC=1 bash 工具/typecheck.sh` 能答**。
- 🔴 **建议**：这一条**别再当成纯文档活** —— 它现在有**可派的部分**（一次实跑 + 按读数机械转义），但**必须等所有写手停手**。

### `A828` 尾巴（4 条，逐条现核）
1. **「订阅者真被叫起来」没有运行时断言** —— 仍开着（`AnimFXModuleCollisions` 那条 UnityEvent 链无断言）。
2. **`m_CallState` 语义仍是推断** —— 仍开着（全仓 `.cs` 无 `m_CallState` 读点；判据只能从 `dump.cs`）。
3. **`工具/import_original_sfx.py` 取材面** —— ℹ️ **现读已是三来源**（`sounds[]` / `exitSounds[]` / `collisionEvent` 的 `PlaySound`，见文件头 `:80+`）⇒ **这条其实已做**，只是账没销。
4. **35 个 def 会走 `[ERROR] particle system not assigned`（没跑过）** —— 仍开着（没有实跑记录）。

### `A985` 各分项（逐条）
| 分项 | 现状 | 证据 |
|---|---|---|
| ① `Describe` 两处没包 `Loc.T` | ✅ **已做**（写手在中间账之后落的，见 §一） | `Deck/DeckEditorState.cs:273` · `Shell/DeckInfoPopup.cs:577` |
| ③ `AlliancePanel` 整扇窗 | ⏭ **仍开着**（只有注释，没有窗） | `Battle/BattleDriver.cs:10809-10813`（判据注释）· 全仓无 `BattleAlliancePanel` 实现 |
| ④ `HUD/CreatedBy` | ⏭ **仍开着（大）**：消费面钉死，**引擎里没有 `creator` 字段** | `Battle/BattleDriver.cs:4105-4109`（判据注释）· `RuleEngine/` 全仓 `creator` 0 命中 |
| ⑤ `BattleEndPlayerData` 三格 | ⛔ **挂起**：判据齐、**宿主 prefab 本地取不到** | `Battle/EndPanel.cs:327-334`（判据注释） |
| ⑥ `Battle/Cemetery/*` 19 条 | ⏭ **仍开着**：前置 = **引擎加 `Draw`/`AmbushExit`/`SecretOrder` 3 种事件** + 值在远端 | `Battle/BattleDriver.cs:753-755` · `:3549-3587` · §29·b G5 条 |
| ⑦ `Battle/Tips/HandFull` | ⏭ **仍开着**（`DragToTarget`/`UnitNotReady` 已落，`HandFull` 全仓只有注释引用） | `Battle/BattleDriver.cs:3375 DragToTargetTerm` · `:3377 UnitNotReadyTerm`；`Core/Tooltip.cs:35` 是**清单注释**不是实现 |
| ⑧ `RuleCodes.Describe` 改造 | ⏭ **仍开着**（1:1 三处只落了 `ErrCost` 的断言侧） | `Editor/BattleScene.cs:2996 Check(code == RuleCodes.ErrCost, …)` · `Core/Tooltip.cs:35-36`（`NotEnoughMana`/`NotYourTurn` 只在注释清单里） |
| ⑨ `CardEffectItem` 每场 5 颗 | ⏭ **仍开着（未判准）**：我们 `EffSlots = 5`；原版槽数**只找到循环体、没数出常量** | `Core/CardWinBox.cs:71 EffSlots = 5` · `Battle/CardDisplayWindow.cs:274/433` · 反编译 `CardEffectsService__EnableCardEffectSlots.c` + `CardDisplayWindow__DisplayCardEffects.c` |
| ⑩ `BattleVictory`(1) 裁定「做」 | ⏭ **仍开着**：枚举值在，**一个调用点都没有**（`CheckWinner` 仍不带码） | `RuleEngine/Core/RuleCore.cs:57 BattleVictory = 1` · `:4949` 只有 `Describe` 的一格 |
| ⑪ `WinButton`(4) 裁定不做 | ✅ 结论：不做（已裁） | —— |
| ⑫ `Cancelled`(5) | ⏭ **仍开着**：要先改「`ctx` 何时出生」 | §29·b G10 条 |
| ⑬ 录像重放理由码 | ⏭ **仍开着**：需单独设计 | §29·b G10 条 |

### `A845` mat/mesh 两笔尾巴
- (a) **网格路要实跑验证 `CopySerialized`** → ⏭ **仍开着**，代码自己写着「**这条路没实跑过**」（`WarpforgeArena1/Editor/EffectExporter.cs:1933-1934`）。
- (b) **撞名真复刻判据未定** → ⏭ **仍开着**（守卫已落 = 出声不静默，`:156-164`；但「撞名时正版怎么选」无判据）。

### `A878` 字体缺字语料
- ✅ **代码侧全做完**（语料已并：`Core/CardText.cs:642` 转调 `Core/Loc.cs:999 AllChinese()`；断言在 `Editor/BattleScene.cs:8677-8689` 含改坏法）。
- ⏭ **只剩 Unity 腿**：跑 `-executeMethod TmpSetup.BuildCjkFontAsset` 后销账。**不属 12 条自检** ⇒ 单独排。

### `A964` 命中区覆盖普查 —— **判据基本齐，但有前置**
- 判据正文（含**分档裁定**）齐：`资料/待办判据_1018.md` §A964 + §A964 续；侦察 = `资料/普查产出_1018/P-HIT_命中区探针侦察.md`。
- **可派两块**（互不依赖）：**战斗侧**落 `Editor/BattleScene.cs` 新段；**外壳侧**落 `Editor/ShellScene.cs` 新段（⛔ 不是各新建 `Editor/*.cs`）。
- 🔴 **前置（缺则探针 = 自证）**：① `Shell/PointerLayer.cs` 要一个 `internal` 只读口交出 `HitBoxPx` 算式 ② `BattleDriver.SetHudButtonActiveForTest` 补 `"cameraReset"` 档 + `HudButtonWorldPosForTest`。
- 🔴 **还欠一条只读活**：`d:/2` 原版侧的 `m_RaycastPadding` / `m_Padding` **逐窗实读值一条都没核**（白名单需要）。

### `A942` —— F1 那条护栏
- 现读：`TestDeathAccountsAfterBacklash` 在 **`RuleEngine/Editor/RuleEngineTest.cs:17594`**（⚠️ **不在 `Editor/BattleScene.cs`**，账上那句落点写错了）。
- ⏭ **仍开着**：要判 ① `ConditionHolds("deaths")` 的 `MatchesKind(d.Card,"troop")` 与夹具 `type=="unit"` 能否过 ② 正文一改，两条断言的期望值要重定。判据齐、**可直接派**。

### `A712 一族`（残余）
- ✅ `A846`（撞车 2 文件 4 处）· ✅ `A847`（块高）· ✅ `Label.cs` 过期记录 —— **都已做**。
- ⏭ **仍开着 = `A848` 的 `Core/CardView.cs` 卡面那一族**（占非 `Middle` 的 42%、自有布局不走 `RefreshBounds`）；
  `CardView.cs` 目前 `SetVAlign` 调用点 **0**，只有一条 2026-10-18 的注释（`Core/CardView.cs:1694`）⇒ **未逐处落、也未判「要不要落」**。
- ⚠️ `W16` §六 自陈「本批一条断言都没写」⇒ 这一族的**牙口**仍空。

### `A799` —— 15 处的收尾
- ✅ **生产 9 处已补注释**（W8，`Shell/{PurchasePremiumWindow,RankedRewardEventWindow,LeaderboardRow,MatchLogRow,SocialWindow}.cs`）。
- ✅ **夹具 6 处**在 `Editor/MainMenuScene.cs`（W3 已做）。⚠️ 中间账把夹具 2 处记在 `ShellScene`/`ShopScene` —— **那个前提不成立**（W8 现读推翻：两处都判「不会新裁」）。
- ⏭ **仍开着**：`LeaderboardRow`/`MatchLogRow` 那 5 处**「部分越界被夹到视口沿」没有任何断言**；W8 建议宿主 = `Editor/ShellScene.cs`（白名单内可行）⇒ **要裁**。

### `A562` —— `wf_shaders_extra.bundle`
- 现读：`MyGame/Assets/StreamingAssets/WarpforgeVFX/wf_shaders_extra.bundle`，**1.34 MB · mtime 09-11**（相对工程日期 ≈ 2026-09-21）；**git 里没有这个文件**（未被跟踪）。
- ⏭ **仍开着**：「要不要重打」的口径仍未定；全仓无 `A562` 记录（`资料/*.md` 零命中）。

### 🆕 无编号的 6 行笔（逐行）
| 行 | 现读 |
|---|---|
| B28：`CollectionCheck.unity` + `voice_lines.json` 旧名 | ⏭ 仍开着（`CardPresentation/Scenes/CollectionCheck.unity` **在盘**；等重跑产物 ⇒ 收口时跑 `CollectionScene.Run`） |
| F3 两条悬案（四条字栏差值的 x/y 分解 · `AlignLeft` 之外是否有第二因） | ⏭ 仍开着（要 Unity 量） |
| 批 3 附带：原版「别的单位监听这张卡死亡」在反噬 action 之内、我方排在之前 | ⏭ 仍开着（已在 `RuleEngine/Core/RuleCore.cs:3236-3257` 落成注释表；**起点仍读不到**） |
| `Battle/BattleDriver.cs` 过期注释「`OnClosed`/`OnPeerLost` 全仓零接线」 | ⏭ **仍开着**（现读 `:5858` / `:5860` 两句**原样还在**；⚠️ 行号已漂到 `:5857-5861`；纯注释、零自检） |
| `A914` 一个时序品种（最后 1 秒重连） | ⛔ 真 Play |
| `A860 的续`：菜单版那颗眼睛钮 | ⛔ 真 Play |

---

## 三、⛔ 挂起（判据在远端 / 等真 Play）

| 编号 | 为什么 |
|---|---|
| `A985⑤ BattleEndPlayerData` | 判据齐，**宿主 prefab 本地取不到**（91 包 24.7 万文件按类名 + 5 个字段名全盘搜过：零实例） |
| `A985⑥ Cemetery 19 条` | 文案值**本地一条都没有**（远端 I2） |
| `A897` / `A970` | 都要**远端 `allcards_assets_all.bundle`**（`V-PACK` 采集面只到 4.3%） |
| `A163` / `A186+A376` | 判据在远端（`TitleData`） |
| `A914` / `A860 的续` | 等真 Play（`资料/真Play待验清单.md`） |
| `A650` | 两条路都要动 `d:/2` ⇒ 留专门一趟 |

---

## 四、⛔ 判据不足 / 需先裁

1. 🔴 **`A678` —— 判据已取到（可销），但同时挖出两条新缺口**（建议 A 表就地订正）：
   - 原版 `ItemDrawerComponents.Quantity` 那颗 TMP = **`Right`（H）/`Middle`（V）** · `字号 75.0` · `基准 36.0` · `auto[12~75]` · **`折行=0`** · 色白
     （实读：`Wildcard Drawer>Content>Label>Quantity`；**66 个 `Quantity` 实例里 65 个是 `Right`**，唯一例外 = `Rewards Base Submenu Variant` 的 `Left`）。
     ⇒ 我们的 `align: 2`（右）**对得上**；**V 档 / 字号 / 自适应区间仍是我们的**（`Shell/ItemDrawer.cs:1223-1233` 自己写着「这一套位置仍是我们的」）。
     **可复现**：`python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Wildcard Drawer" --depth 6`。
   - **试过哪些包/哪些词**：`bundle_menus_assets_all`（命中）；`bundle_menusharedresources_assets_all` / `bundle_mainmenualwaysloaded_assets_all` 无。词 = `ItemDrawer` / `Quantity` / `Item Drawer`。
2. 🔴 **`A962③` 主序（别的卡监听死亡 vs 反噬）—— 判据仍不齐**：现读 `FlushDeathWatches`(`RuleCore.cs:3272`) 仍**排在** `FireTriggerAt(…Backlash)`(`:3294`) **之前**；调度台裁定「判据读不出确凿次序 ⇒ 一行不许改、如实标」⇒ 需先复核 `_ResolveBacklash_d__451__MoveNext.c` 逐跳。✅ `Emit(EvtKind.Death)`(`:3233`) 那条**裁定②的注释已落**。
3. `A964` 的原版侧白名单（`m_RaycastPadding`/`m_Padding` 逐窗实读）+ 两个测试口 —— 见 §二。
4. `A848 CardView 78 处` —— 「要不要落」本身没判（自有布局、不走 `RefreshBounds`）。
5. `A799` 那 5 处断言的宿主 —— 要裁（`ShellScene.cs` 可行）。
6. `A562` —— 重打口径未定。
7. **`A338/A895` 的「真身」也在漂** —— 连「订正后的行号」都不能信（R-ENG §6 已判）⇒ 一律按符号认。

---

## 五、顺手发现（⛔ 未改，交调度台分流）

1. 🆕 **`A971` 的处数是 11 不是 10** —— 中间账漏了 `Shell/TutorialModePopup.cs:57`（同一句「聊天三档」措辞）。
2. 🆕 **`A451` 的残余比账上宽** —— 账上说「X2（Shell/ 13 件/27 处）仍开着」，实读那 13 件**已归零**，
   而**另外 7 个 `Shell/` 文件 / 9 个 doc 块**里有裸 `<`（36 处）。**两说打架**（W7 的编译器读数 vs 本会话静态扫）⇒ 需一次实跑定案。
3. 🆕 **`gen_icon_doc.py` 硬编码「`IconSetup.Verify` 报 270/270」与实际打架**（实跑 1606；`资料/卡面图标_现状与缺口.md:40` 写 1307）—— 三个数，只有实跑那个真。
4. 🆕 **`IconSetup.Verify` 现在有一条【显式已知红】**（`②c【已知缺口 A980-c】`）：
   `DA44 Ancient Reliquary` 卡面是**两枚**图标（拳 + 枪），我们只画拳 —— `desc` 那个位置是**裸词 `Ranged`**、计划表无对应项
   ⇒ **缺口在 `工具/gen_icon_plan.py`，不在 `IconSetup.cs`**（`Editor/IconSetup.cs:291-296` 自己写着「这一条现在就是红的」）。
5. 🆕 **`A949` 的 `EffectResolver` 那一处【不该并】**（F7 已用离线探针实测：`GSC64` 那条路 `Has(fast)=False`）⇒ **A 表那句「三词表还欠两处」应订正成「欠一处」**。
6. 🆕 **`A942` 的落点账上写错**：护栏在 `RuleEngine/Editor/RuleEngineTest.cs:17594 TestDeathAccountsAfterBacklash`，**不在 `Editor/BattleScene.cs`**。
7. 🆕 **`A823` 可销**（我们数据里 `collisionEvent` 真有订阅：5906 个键 / 563 个 `m_MethodName`）；`import_original_sfx.py` 三来源也在 ⇒ `A828` 尾巴里那两条**其实已做**。
8. ℹ️ 本会话把 `A967/A968/A969/A980/A947/A858/A963③/A974/A976/A811根治/A949/A950/A951/A823/A826尾巴/A985①/A932` 逐条读成**已落盘** —— 与中间账的 ❌ 不一致，**原因是写手在中间账之后落了盘**（mtime 06:55–07:00 及更晚）。
9. 🆕 **写手仍在动**：本会话末次 `git status` 比会话开头**多了 6 个 M**（`Deck/DeckEditorState.cs` · `Deck/DeckRuntime.cs` · `Editor/DeckScene.cs` · `Editor/NetSelfTest.cs` · `Shell/DeckInfoPopup.cs` · `Shell/SearchingOpponentWindow.cs`）
   ⇒ **本报告的「仍开着」在收口时可能又变了**，复跑前的最后一步应当重新 `git status` + 重读本报告点名的落点。

---

## 六、⛔ 没查清的（如实标，别当结论）

1. `A451` 那 36 处**是不是真 CS1570** —— **没跑编译器**（红线：不许跑 Unity；且 `typecheck.sh` 的固定路径会被并发写手污染）。**只给了静态估计 + 可复现命令**。
2. `A828` 的 `m_CallState` 语义 —— 只确认「全仓 `.cs` 无读点」，**没读 `dump.cs`**。
3. `A985⑨` 原版 `CardEffectItem` 的**槽数常量** —— 只找到循环体（`CardEffectsService__EnableCardEffectSlots.c`），**没数出来**。
4. `A845(b)` 撞名时正版的选法 —— **判据没找**（只确认我们这边是「出声不静默」）。
5. `A562` 的重打口径 —— 全仓无记录，**没查到出处**。
6. `A678` 唯一那个 `Left` 例外（`Rewards Base Submenu Variant`）—— **没逐链核它是不是同一类抽屉**。
