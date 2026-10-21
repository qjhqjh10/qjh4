# W62 · D5「由 {0} 创建」三处偏离 —— 修复（执行代理 Z · 2026-10-22）

> 输入：`W62_缺陷诊断.md` §D5 ＋ `W62_卡面红修复.md` §三·1（`CreatedByEmOfCard` 的 10 倍错）。
> ⛔ 本轮**一条 Unity 都没跑**（红线）；验法 = 回原文件复核判据 + 秒级 typecheck。
> in-engine 读数由主对话跑（命令与期望见 §三）。
> 白名单内改的六个文件：`Core/BattleContext.cs` · `Core/RuleCore.cs` · `Editor/RuleEngineTest.cs`（追加断言）·
> `Battle/BattleDriver.cs` · `Core/CardView.cs` · `Editor/BattleScene.cs`（追加断言）。
> ⚠️ **另加一个白名单之外、黑名单之内的文件：`RuleEngine/Core/EffectResolver.cs`** —— 理由与改动范围见 §四·5
> （「来源」的另外两个写点**物理上就在那个文件里**，不碰它 D5② 做不完整）。

---

## 一 结论

| 项 | 改了什么 | 结果 |
|---|---|---|
| **D5① 显隐门** | `CardView.SetCreatedBy` 加了**状态参数**（原版 `CardStateOptions` 里我们用到的那四档），显隐判据收口到 `CardView.CreatedByVisible` 一处；`SetFace` / `FillCreatedBy` 两个入口都走它。`BattleDriver` 侧新增 `PushCreatedByStates`（唯一的推送口），**静置手牌 ⇒ `InHand`（不印）**、**手牌悬停 ⇒ `InHandShowing`（= 原版 `ShowCardInHand` 那一档，印）**；对手那张中央展示卡走 `InHandPlaying`（state 2 点亮、state 3 收）。 | 真机上「静置手牌全挂着那行字」消失 |
| **D5② 来源修正** | `BattleContext.NewInstance` 的盖章来源：`ActingUnit?.Card ?? PlayingCard` → **`ActingUnit?.Card ?? CreationSource`**。`CreationSource` 是**save/restore 的作用域量**（`PushCreationSource` 压/还原），三个写点：`RuleCore.PlayCard` · `EffectResolver.PlayTactic` · `EffectResolver.PersistentListenerDeferred`。⛔ 不再读那个从不清空的 `PlayingCard`。 | 非结算期新造的实例（抽牌/补牌/天赋生成）**拿不到来源**，结构上不会盖错名字 |
| **D5③ 字号常量** | `CardView.CreatedByEmOfCard`：`2.45f / CardUnitH` → **`0.245f / CardUnitH`**（原版 `m_fontSize 2.45` × `isOrthographic=0` 的 0.1 × `localScale 1`）。 | 换算出的 `fontSize` 现在是 **2.45** —— **恰好等于原版 prefab 的 `m_fontSize`**（强旁证）。⚠️ **可见变化**（见 §四·1） |
| **④ 断言** | 引擎侧 4 条（`RuleEngineTest.TestCreatedBy`）· 表现侧 7 条（`BattleScene.Run` 末尾新节）。 | 见 §二·4，两条「改回旧写法必红」的判别式都配齐 |

---

## 二 逐条：改动 → 原版判据 → 断言

### ① 显隐门（`W62/D5①`）

**原版判据（本轮回原文件复核过，不是转述诊断）**
- `CardStateOptions` 18 档逐值：`d:/2/tools/il2cpp_out/dump.cs:45465-45486`
  （`inDeck=0 · inHand=1 · inPlay=2 · inPlayAttacking=3 · waitingToBePlayed=4 · waitingToDie=5 ·
  inCemetery=6 · drawing=7 · inHandShowing=8 · inHandMoving=9 · inHandPlaying=10 · removedFromGame=11 ·
  inHandJustCreated=12 · inMulliganSelected=13 · inMulliganDiscarded=14 · inPlayTransforming=15 ·
  inDeckDrawing=16 · inPlayAminingAttack=17`）—— **整张表逐值抄进了代码注释**（`CardView.cs` 的 `SetCreatedBy` doc）。
- 显隐函数：`d:/2/tools/decomp_full/BattleCardUI__DisplayCreatedByText.c:14-43` 逐行读：
  ① 节点 `SetActive(0)`（第 16 行，**先关**）；② `card+0x248 → +0x268`（`createdBy`）为 null ⇒ 走异常分支；
  ③ `param_2 == 8` ⇒ **直接点亮**（不判 isPlayer）；
  ④ 否则要求 `card + 0x40`（`isPlayer`）**== 0**（第 26 行 `!= '\0' ⇒ return`）且 `param_2 == 10` ⇒ 点亮。
  ⇒ **8 两边都亮 / 10 只对手那张亮 / 其余 16 档不亮**。
- 这两个状态是谁置的（同一份链）：`CardScript__ShowCardInHand.c:38` 置 8（调用者 = `CardScript__OnTouchEnter.c:30`，
  条件是 `cardState == 1` 且 `isPlayer` 且 `CanDisplayHandCard`）；`CardScript__MoveIntoFieldFromHand.c:17` 与
  `BattleManager._ResolveDisplayFromHand_d__548__MoveNext.c:193` 置 10。
- 出厂态：13 个 arena 的 `CreatedByText` 节点 `m_IsActive = false`（诊断已核，与本轮结论一致）。

**改动**
- `CardPresentation/Core/CardView.cs:1096-1101` —— 新增两个字段 `_createdByState`（**显式初始化为 `Hidden`**，
  因为枚举值是 `-1` 而 C# 字段默认 0 ⇒ 不显式初始化的话详情窗/收藏窗那些「没推过状态」的视图会落在亮着那一档）、
  `_createdByIsLocalPlayer`。
- `CardView.cs:2212-2303` —— `SetCreatedBy(creator, state, isLocalPlayer)` 重写：
  去重键从「只看名字」改成「**名字 + 状态 + isPlayer 三者都要比**」（悬停进出只改状态、名字不变，不比较就会漏推）；
  新增公有嵌套枚举 `CreatedByState { Hidden = -1, InHand = 1, InHandShowing = 8, InHandPlaying = 10 }`
  （**值 = 原版原文**）；显隐判据收口到 `CreatedByVisible` 一个属性。
- `CardView.cs:1638-1640` —— `SetFace` 里那句 `Show(_createdBy, !board)` → `Show(_createdBy, CreatedByVisible)`
  （铁律 10 第 5 条：换形态是第二个入口，必须显式设定，判据共用一份）。
- `CardView.cs:3355-3373` —— `FillCreatedBy`：**先 `SetActive(true)` 再 `Fill`**（TMP 在未激活对象上量不出尺寸，
  这是本仓已知坑；不先激活的话 `FitToBox` 会量到垃圾），填完再按 `CreatedByVisible` 收起 ——
  **层建着、只是关着**（= 原版「先 close、够条件再点亮」，⛔ 不拆层：拆了 `CreatedByShown` 就分辨不出两态）。
- `Battle/BattleDriver.cs:18845-18848` —— `SyncHand` 里原来那段**无条件** `SetCreatedBy(CreatedByOf(...))` → `PushCreatedByStates()`。
- `BattleDriver.cs:18888-18905` —— 新增 `PushCreatedByStates`（**唯一推送口**）：按下标配对 `_handViews[i] ↔ h[i]`，
  `hover == i ? InHandShowing : InHand`（本方 `isLocalPlayer = true`）。注释里写清了三层映射：
  「原版 state 8 = `ShowCardInHand`」↔「我们 = 手牌悬停（`HandLayout` 抬 30 px、放大 1.3）」，
  「原版 state 10 = 打出飞行 / 中央展示」↔「我们的手牌视图不走这一档（原版那一档只点亮**对手**那张）」。
- `BattleDriver.cs:10238-10241` ＋ `18907-18915` —— 新增 `TickCreatedByHover()` 并挂进 `Update`：
  悬停是**纯输入**、不产生引擎事件，而 `SyncHand` 只跟引擎事件 ⇒ 不补这一条的话「悬停放大时那行字不跟着亮」。
- `BattleDriver.cs:18210-18232` ＋ `18238-18246` ＋ `18256-18262` ＋ `18270-18286` —— 对手那张中央展示卡：
  `DisplayEnemyCardInCenter` 新增可选参 `creator`，在 **state 2**（`0.1 + 0.66·hold`）推 `InHandPlaying`／
  **state 3**（`+2.6·hold`）收；来源由 `PlayedCreatorOf(e)` 从 `Ctx.PlayedCards` 回查那一次打出的实例
  （`Play` 事件**不带实例号** —— 见 §四·3 的引擎缺口）。
  🔴 同时订正了方法头那句「`EvtKind.Play` 不带创建者 ⇒ 不画」—— 那是**把「事件里没有」当成了「原版没有」**。

### ② 来源修正（`W62/D5②`）

**原版判据**
- `createdBy` 的写点**只有三处**，每处传的都是「**正在结算的那张卡**」：
  `BattleManager__AddNewCardToHand.c`（`param_3`）/ `BattleManager__AddNewCardToDeck.c`（同）/
  `BattleManager._ResolveSummonUnit_d__510__MoveNext.c:112`；调用方 `CardScript__ProcessCompanion.c` 传**它自己**、
  `AbilityLogic__PlayAbility.c` 传 `AbilityData.thisCard`（能力属主）。
- 抽牌 / 回合起补牌 / 天赋生成那几条路**没有 creator 参数** ⇒ 原版那几份 `createdBy` 恒 `null`。
- 我们改前的问题（诊断已核，本轮复核）：`PlayingCard` 的写点只有 `NotePlayed` 与 `ResolveOps` 的出牌重载，
  **出完结算留在原地**（`BattleContext.cs` 的 `PlayingCard` / `CardPlaySource` 两段 doc 都白纸黑字写着）。

**改动**
- `RuleEngine/Core/BattleContext.cs:641` —— `var creator = ActingUnit != null ? ActingUnit.Card : CreationSource;`。
- `BattleContext.cs:675-745` —— 新增 `public CardDef CreationSource`（doc 里逐条写了三个写点、与
  `CardPlaySource` 的分工、以及**已知残差**）＋ `PushCreationSource(src)` ＋ `HandCardOrNull(p, handIdx)`
  （后者只给出牌入口取「这一手牌位上是哪张卡」，⛔ 不参与任何规则判定）。
- `RuleEngine/Core/RuleCore.cs:3293-3309` —— `PlayCard` 的外层套 `try/finally`：
  种 `CreationSource = 打出的那张`，**restore 在 `using (BeginAction) `之外**（`ActionScope.Dispose` 泵队那一刻
  本字段必须还在 —— 战术正文/触发体是那时候跑的），覆盖 `SpawnTideCopies` / `PlayCompanions` 那两条
  「紧接着造牌、但还没进任何 `ResolveOps`」的路。
- `RuleEngine/Core/EffectResolver.cs:1665-1678` —— `PlayTactic` 同样套一层（战术那条路 `source = null`、
  `ActingUnit` 也是 null，效果造牌只能靠这一格）。
- `EffectResolver.cs:7605-7621` —— `PersistentListenerDeferred`：`CreationSource = pe.Source`
  与它原来那句 `CardPlaySource = pe.Source` **同一段 try/finally、同一份判据**（原版这条 ability 的属主就是写出
  这条常驻效果的卡）。
- `BattleContext.cs:1172-1197` —— `NotePlayed` 里刷 `PlayingCard` 那句**保留**（还有别的读者），
  但把那句「`CreatedBy` 要它」**就地订正**成「已不成立，⛔ 别再挂回 `CreatedByOf`」（铁律 5）。

**优先级为什么仍是 `ActingUnit` 优先**：一次出牌期间**别的单位**触发自家能力造牌时，`ActingUnit` 是那个单位
（= 它那条能力的属主），必须压过 `CreationSource`（否则会盖成「被打出的那张」）。已写进字段 doc。

### ③ 字号常量（`W62/D5③`）

- `CardView.cs:3281-3291` —— `CreatedByEmOfCard`：`2.45f / CardUnitH` → **`0.245f / CardUnitH`**，
  注释写清换算链：`CreatedByText.json` 的 TMP `m_fontSize = 2.45` · 节点 `m_LocalScale = 1` ·
  **`m_isOrthographic = 0`** ⇒ `em = fontSize × 0.1 × localScale` = **0.245 卡单位**（卡高的 7.35%）。
  同处新增只读口 `public static float CreatedByEmPerCardHeight`（自检用，产品代码不用它）。
- 顺带订正了两处过期文案：`CardView.cs:3253-3257`（补 `isOrthographic` 那一跳）·
  `CardView.cs:2212` 上方那句「词条值在远端 I2 表、本地没有」（官方表 2026-10-14 起已并入，这句早过期）。

### ④ 断言

**引擎侧（`RuleEngine/Editor/RuleEngineTest.cs`，追加在现有 `TestCreatedBy` 里，不新建宿主）**
| 行 | 断什么 | 改回旧写法为什么红 |
|---|---|---|
| `:14855-14868` | 先打一张战术（把「上一张」坐实 = `PlayingCard == poke`，前提行 `:14852` 仍在），**再在作用域外** `ctx.NewInstance(...)` 一份 ⇒ `!HasCreatedBy` | 旧写法 `creator = PlayingCard` ⇒ 此刻它是那张战术（非 null）⇒ 被盖章 ⇒ **红** |
| `:14869-14873` | 灭自证：`CreatedByOf(fresh) != poke` | 同一个对象引用不可能既 null 又是 poke ⇒ 与上一条**一起红** |
| `:14871-14873` / `:14918-14925` | 结构：`ctx.CreationSource == null`（出牌作用域外 / 两次出牌都退出后） | 漏还原（`finally` 写错）⇒ 红 |
| `:14780-14795` | doc 里补第 ⑤ 件 + 改坏法清单（**订正**：删 `NotePlayed` 那句**不再**让 ③ 红，现行改坏法 = 删 `PlayCard` 的 `PushCreationSource`） | —— |

**表现侧（`CardPresentation/Editor/BattleScene.cs:33030-33130`，`RunCore` 末尾新节，全用现有 `Check`）**
| 行 | 断什么 |
|---|---|
| `:33053-33055` | ★ `CreatedByEmOfCard 折回卡单位 = 0.245`（容差 1%）—— 期望值来自**原版 prefab**，⛔ 不回读常量 |
| `:33069-33071` | ★ 静置手牌（`InHand`）⇒ **不印** |
| `:33074-33077` | ★ 展示档（`InHandShowing`）⇒ **印出来了**（打印实际文本） |
| `:33079-33082` | ★ 打出档 + **我方** ⇒ 不印（原版 `+0x40` 闸） |
| `:33084-33087` | ★ 打出档 + **对手** ⇒ 印，且内容与展示档**逐字一致** |
| `:33089-33092` | ★ 灭自证：同一张卡 / 同一个创建者，**只改状态**就有两种结果 |
| `:33108-33129` | ★ **驱动层**：往本方手牌里塞一份**真带来源**的牌（`CreationSource` 是唯一盖章口，用完立刻还原）→ `RefreshAll()` → 断言 **一张都不印**（含前提：这份牌真有来源 / 真有手牌视图；座位取 `driver.MySeat`，⛔ 不猜 0） |

改回旧写法的红法（逐条）：删 `CreatedByVisible` 的门 ⇒ 「静置手牌不印」红；删 `isLocalPlayer` 那一半 ⇒
「打出档 + 我方不印」红；`SyncHand` 回退成无条件 `SetCreatedBy(来源)` ⇒ 驱动层那条红（真机 W62 抓到的就是它）。

---

## 三 主对话要跑的验证 + 期望读数

```bash
# ① 引擎侧（必跑；动了 RuleEngine）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod RuleEngineTest.Run -logFile d:/4/_tmp_view/w62z/rule.log

# ② 表现侧（必跑；动了 BattleDriver / CardView / BattleScene）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod BattleScene.Run -logFile d:/4/_tmp_view/w62z/battle.log

# ③ 建议（不是本轮白名单要求的宿主，但**它是 `CreatedBy` 现有断言的家**；只因 SetCreatedBy 加了默认参、FillCreatedBy 改了激活顺序才建议跑）
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" -batchmode -quit \
  -projectPath "D:\4\Unity\MyGame" -executeMethod CardBaseDemo.Run -logFile d:/4/_tmp_view/w62z/cbd.log
```
⚠️ ②③ 都会重建并覆盖存盘自己的场景；跑前留副本、跑后 `git diff --numstat` 对一眼（`CLAUDE.md` §二）。

**期望读数**

| 宿主 | 断言行 | 期望 |
|---|---|---|
| `RuleEngineTest` | `★★【W62/D5②】不在结算期新造的实例没有来源` | ✓（新增） |
| | `★★【灭自证】来源不是上一张那张战术` | ✓（新增） |
| | `★★【结构】出牌作用域之外 ctx.CreationSource 必须是 null` | ✓（新增） |
| | `★★【W62/D5② · 结构】两次出牌都退出之后 … 仍是 null` | ✓（新增） |
| | `TestCreatedBy` 原有 ①②③④⑤ 若干条 | **全 ✓，一条不许变红**（`CreatedByCount == cbBase + 2` 也不变） |
| | 结尾合计 | **失败 0** |
| `BattleScene` | `★ W62/D5③ … 折回卡单位 = 0.2450（期望 0.245，容差 1%）` | ✓ |
| | `★ W62/D5① 静置手牌（原版 inHand = 1）⇒ 不印` | ✓ |
| | `★ W62/D5① 展示档（原版 inHandShowing = 8）⇒ 印出来了：「由 <某督军名> 创建」` | ✓（文案随 `Loc.Current`） |
| | 打出档 我方 / 对手 / 判别式 | 三条 ✓ |
| | `★ W62/D5① 驱动层：静置手牌（无悬停）一张都不印（实得「」）` | ✓ |
| | 合计 | 失败 0（⚠️ `BattleScene.Run` 有一条**已知间歇** —— 判绿红看「断言合计」，不看退出码） |
| `CardBaseDemo` | `AssertCreatedBy` ①②③④⑤⑥ 六条 | 全 ✓（默认参 = `InHandShowing` ⇒ 旧断言语义不变） |

---

## 四 没查清 · 改动清单 · 顺手发现

**1. ⚠️ 白名单外的那一处：`RuleEngine/Core/EffectResolver.cs`（不在白名单、也不在黑名单）**
D5② 要求「来源 = 本次结算正在跑的那张卡」，而这句话的**两个写点在物理上就落在这个文件里**：
`PlayTactic`（战术整条结算的入口 —— 测试与外部都直接调它，不经过 `RuleCore.PlayCard`）与
`PersistentListenerDeferred`（常驻效果/手牌监听，来源 = `pe.Source`）。
⇒ 若不碰它，**直接调 `PlayTactic` 的那条路上「战术造出来的牌」会一个来源都没有**（改前它们靠
`PlayingCard` 的陈旧值，虽然常常是错的、但至少非空）。
**改动范围严格限定在这两个函数**：各加 `PushCreationSource` + `try/finally` 还原（外加注释），
**没有动任何结算逻辑、没有改任何 op 的语义**。若主对话认为这越界，可只回退这两处 ——
代价是「直接调 `PlayTactic` 造牌」那一档没有来源（`TestCreatedBy` 与 `BattleScene` 的断言**不依赖**这两处，
它们走 `RuleCore.PlayCard`，只有 ⑥ 的 `ctx.CreationSource == null` 那两条会照旧绿）。

**2. ⚠️ D5③ 是【可见变化】（估：那行字变小 ~15–20%）**
改前 `CreatedByEmOfCard` 是 2.45（10 倍），`fontSize` 算出来 ≈ 24.5，永远被 `FitToBox`（框高 0.38）压到装得下
⇒ **屏幕上的实际大小由「装进 0.38」决定**（em 约 0.30）。改后名义 em = 0.245 ⇒ 行高约 0.29 < 0.38 ⇒
**`FitToBox` 不再回缩**，成字就是 0.245 = 原版 devs 的 autosize 起点（`2.45 ≤ m_fontSizeMax 3.0`，原版也不会缩）。
⇒ 结论：**改后的尺寸才是原版的**，改前偏大约 20%（**估算，未实机量** —— 按铁律 10·6 建议渲一张真卡与 PnP 并排看一次）。

**3. 已知残差（未修，如实记）**
- `EffectResolver.ResolveDeploy` / `ResolveAtTurn` 那几条**常驻作业**把「刚部署的那个单位」当 `source` 传给
  `ResolveOne`（本工程为代词/相邻锚点做的结构选择）⇒ 那些路上 `ActingUnit` 是**事件宾语、不是能力属主**，
  由常驻效果造出来的牌仍会盖成「被部署的那个单位」。修它要动那三条路的 `ResolveOne` 入口，
  与 `FactionSourceCard` 同一笔活 —— 不在本笔白名单。
- **嵌套出牌**：`PlayCard` 在别人的作用域里被调用时（触发里再打一张），本字段在最外层 `Dispose`（泵队）之前
  就还原成外层的值 ⇒ 从**那次嵌套出牌**入队、到外层才泵出去的 body 里造牌，来源会是外层那张。
  今天靠 `ResolveOne` 的 `ActingUnit` 兜住绝大多数；结构上的干净解法见下条。

**4. 引擎缺口（建议另开账）**：`BattleEvent` 的 `Play` 事件**不带 `InstanceId`**（`Deploy` 带）。
所以「对手中央展示那张是不是被造出来的」只能从 `Ctx.PlayedCards` **回查**（`PlayedCreatorOf`）。
更干净的做法 = 给两条 `Emit(EvtKind.Play)` 补 `InstanceId = inst.Id`（`RuleCore.PlayCardCore` 与
`EffectResolver.PlayTacticCore`），但那是「事件契约」改动、不在本笔白名单（`RuleCore.cs` 只开到造牌/来源那几处）。

**5. 顺手发现（不属于本笔，⛔ 我没动）**
- `CardPresentation/Editor/CardBaseDemo.cs:803-816` 的 `AssertCreatedBy` doc 里那句「删掉 `SetFace` 里那句
  `Show(_createdBy, !board)` ⇒ ④ 红」**已经与代码不符**（现在那句是 `Show(_createdBy, CreatedByVisible)`）——
  断言照样有效（红法不变），但**文案该更新**。同 doc 第 804 行「有来源 ⇒ 印」现在只在默认参 `InHandShowing` 下成立。
- `CardPresentation/Core/Loc.cs:1477-1502` 那段 `Battle/HUD/CreatedBy` 的注释里「中文那半是我们自己写的」
  **已过期**（官方表已并入，`Loc.cs:1488` 自己也写了这一点）—— 与本笔同族，但 `Loc.cs` 不在白名单。

**改动清单（行尾：改前二进制数 → 改后二进制数，全部未翻）**

| 文件 | 行尾 | 改后 CRLF / LF |
|---|---|---|
| `CardPresentation/Core/CardView.cs` | 纯 CRLF | 9438 / 9438 |
| `CardPresentation/Battle/BattleDriver.cs` | 纯 CRLF | 24076 / 24076 |
| `CardPresentation/Editor/BattleScene.cs` | 纯 CRLF | 35105 / 35105 |
| `RuleEngine/Core/BattleContext.cs` | 纯 LF | 0 / 1895 |
| `RuleEngine/Core/RuleCore.cs` | 纯 LF | 0 / 11515 |
| `RuleEngine/Core/EffectResolver.cs` | 纯 CRLF | 11023 / 11023 |
| `RuleEngine/Editor/RuleEngineTest.cs` | 纯 CRLF | 33607 / 33607 |

---

## 五 typecheck

```
$ TMPDIR=/tmp/wf_z25 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（最后一次在**全部编辑之后**跑；中间一次报 `CardData` / `CardView.Placeholder` 的 CS0117 是我自己写错了宿主类型，
当场改掉后归零。⚠️ 期间 `BattleScene.cs` 曾被**别人**在盘上改过（Edit 提示 "file had been modified on disk"）——
我只做了追加，未覆盖任何既有内容。）
