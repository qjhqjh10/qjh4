# DIAG 手牌「边框光芒」分档（绿=可打出 / 紫=临时卡 / 不亮=费用不够）

## 一、结论

1. **用户三条口径逐条成立**，而且判据在原版里**全找齐了**（实读值，不是「看着像」）。
2. 那圈光 = 原版 **SDF 高亮描边**（材质 `Card Highlight And Shadow` 上的 `_Outline`），**不是**我们叫「状态环」的那一层
   （`MinionLight` 上的 `CardHighlight.FrameHighlight` —— 那是「选目标」6 态专用的，里面**根本没有**「可打出绿 / 临时紫」两档）。
3. 三档颜色 = `BattleCardUI` 的 3 个序列化字段：可打出 **绿 #1C7101** · 临时卡 **紫 #5B0F53** · 打不出 **`_Outline` alpha=0（根本不描边）**；补间 **0.2s**。
4. **我们这三分档【不是缺功能】**：`CardHighlight.OutlineOf` + `CardView.SetOutline` 2026-09-19 就接了，颜色逐值照抄原版。
5. 🔴 **真偏离两处，都在 `BattleDriver.RefreshHandPlayable` 那一行**：
   **A** 战术卡（非单位卡）被 `card.IsUnit` 挡住 ⇒ **永远不亮绿 + 永远置灰**（原版 `CanPlayCard` 对战术卡走 `CanPlayTactic`，能打就亮）；
   **B** 临时卡只认卡面 `ephemeral` 关键词 ⇒ **漏掉运行时打上的 `CardInstance.EphemeralMarked`**（引擎 `Ctx.IsEphemeral` 两条路都管，我们只走了一条）。

## 二、原版逐档

链路：`BattleManager.CanPlayCard` → `PlayerHand.LightPlayableHand` → `CardScript.ActivateCard(bool)` → `BattleCardUI.ChangeToHighlightColor` → `Card2DController.ChangeHighlightColor` → `UIImageMaterialColorChanger.DoColorChange(color, 0.2f)` → SDF 材质的 `_Outline`。

| 档 | 触发条件 | 颜色 RGB(A) | 补间 |
|---|---|---|---|
| 可打出（默认档） | 己方回合 + `CanPlayCard` 为真 | `cardPlayableColor` 0.109804, 0.443137, 0.003922, 1 → **绿 #1C7101** | 0.2s |
| **临时卡** | `EntityScript.HasCurrentTrait(card, 5)` —— **5 = `DefinedTrait.ephemeral`**（当前关键词，含运行时加的） | `ephemeralCardPlayableColor` 0.356863, 0.058824, 0.325490, 1 → **紫 #5B0F53** | 0.2s |
| 特技（teleport 且本回合抽到 / oath 的钱够） | `HasCurrentTrait(0x4ce=1230 teleport) && IsTheSameTurnDrawn()` · `HasCurrentTrait(0x4fb=1275 oath) && CanPlayWithOathNowInHand()` | `cardSpecialActionColor` 0.258824, 0.898039, 1.0 → 青 #42E5FF | 0.2s |
| 破坏 | `rawCardScript + 0x90 == 0xe6 (230)`（= `SpellType.Sabotage` 230，沿用本仓既有结论） | `cardSabotageColor` 1.0, 0.132175, 0 → 红 #FF2200 | 0.2s |
| 打不出 | 上面都不成立（`ActivateCard` 的 bool 为假） | —— **`ToggleHighlight(false)`，整层不亮** | —— |

优先级 = **破坏 > 临时 > 特技 > 默认可打出**（if/else 嵌套顺序）。
出处：`decomp_full/BattleCardUI__ChangeToHighlightColor.c:16-24`（默认色）/`:22-27`（破坏）/`:29-42`（临时）/`:32-43`、`:45-55`（特技）/`:65-71`（才真上色）·
`PlayerHand__LightPlayableHand.c:33-38` · `CardScript__ActivateCard.c:44-48`（不亮那支）、`:62-63`（亮那支）·
`Card2DController__ChangeHighlightColor.c:23-24`（时长 0.2）· `Card2DController.cs:54`（`HIGHLIGHT_COLOR_CHANGE_TIME = 0.2f`）·
字段名 `BattleCardUI.cs:238-254` · **颜色值** `assets_full/bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_122102074273340352.json` ·
`DefinedTrait.cs:4`（`ephemeral = 5`）· `BattleManager__CanPlayCard.c:100-129`（单位卡才查格位；战术卡查 `IsSpellType`+目标）。

**那圈「状态环」与本题无关**（别拿它去实现用户说的规则）：`CardHighlight` / `MinionLight.FrameHighlight` 只有
`RegularColor`(alpha 0) / `ValidTargetColor`(#00FF21 绿) / `SelectedColor`(白) / `PlayableColor`(1,1,0 黄，**= state 5 `displayingActiveAbility`**) / `SelectedTargetColor`(#FF8400) 五色。
出处：`MonoBehaviour_-3885077450169410624.json`（d:/2/解包整理/08_预制体特效/战斗预制体/MonoBehaviour/）· 字段 `CardHighlight.cs:44-64` · 状态 `CardHighlight.cs:6-14` · 逐态 `CardHighlight__ChangeState.c`。

## 三、我们逐档

| 项 | 我们的实现 | 出处 |
|---|---|---|
| 描边层 | `_shadowLayer`，材质 = `SdfMaterial()` 原版 shader+原版值，`AddLayer` 内 `new Material(mat)` **每卡一份**（⇒ 不会串色） | `CardView.cs:1885-1888` · `:2653-2689` · `:2562` |
| 可打出绿 | `OutlinePlayable` 0.10980392,0.44313726,0.00392157 → #1C7101（**逐位 = 原版**） | `CardHighlight.cs:180` |
| 临时卡紫 | `OutlineEphemeral` 0.35686275,0.05882353,0.32549021 → #5B0F53（逐位 = 原版）；**判据 = `card.Has("ephemeral")`（只看卡面关键词）** | `:181` · **`:212`** |
| 特技青 / 破坏红 | `OutlineSpecial` #42E5FF · `OutlineSabotage` #FF2200 | `:182-183` · `:211-214` |
| 打不出 | `OutlineOf` 第一行 `if (!canBePlayed) return (0,0,0,0)` | `:198` |
| 补间 / 时长 | `OutlineAnimTime = 0.2f`；「目标没变就早退」 | `CardHighlight.cs:72` · `CardView.cs:4497-4519` |
| 谁调用 | `BattleDriver.RefreshHandPlayable()`（`SyncHand` 每轮调） | `BattleDriver.cs:11578` · `:11786-11806` |
| **playable 怎么算** | **表现层自己算**：`myTurn && card.IsUnit && card.Cost <= p.Energy && hasSlot` | **`BattleDriver.cs:11797`** |
| ephemeral 怎么算 | 在 `OutlineOf` 里**自己算**（第二处判据） | **`CardHighlight.cs:212`** |
| 状态环 `_rim` | `FrameColorOf(Playable)` = alpha 0 ⇒ **与原版一致，不是错**；`:3668`/`:3672` 那条注释是对的 | `CardView.cs:3664-3686` · `CardHighlight.cs:117-129` |
| 场上卡 | 不描边（`_faceMode != CardFace.Board` 才写 `_Outline`） | `CardView.cs:4505` |

## 四、三列差异表

| 档 | 用户口径 | 原版 | 我们 | 判定 |
|---|---|---|---|---|
| 可打出 → 绿 | ✅ | `cardPlayableColor` #1C7101 | `OutlinePlayable` 同值 | ✅ 颜色/时长一致 |
| 临时卡 → 紫 | ✅ | `HasCurrentTrait(ephemeral)`，**含运行时加上的** | 只认 `card.Has("ephemeral")` | ⚠️ **缺一半**（复制出来的临时卡不紫） |
| 费用不够 → 不亮 | ✅ | `_Outline` alpha 0 | 返回 alpha 0 | ✅ 一致 |
| 战术卡（非单位卡） | 未提 | `CanPlayCard` 分流 `CanPlayTactic`，能打就绿 | `card.IsUnit` 挡住 ⇒ **永不亮 + 永远置灰** | 🔴 **真偏离 A** |
| 「可打出」的判据 | 未提 | 引擎 `CanPlayCard`（费用+格位+目标+教程闸门） | 手搓一式（少 plasma、少战术、多格位） | 🔴 **真偏离 A 的根** |
| 破坏红 / 特技青 | 未提 | 有 | 有 | ✅ |
| 状态环（`MinionLight`） | 用户说的那圈光 | 只给「选目标」6 态，无「可打出绿」 | 同 | ✅ **⛔ 别去给它加绿** |

## 五、改动清单（落在共用件 `CardView` / 引擎口上）

**改点 1（真偏离 A，主修）** — `BattleDriver.cs:11797`
把那句手搓的 `playable` 换成引擎**唯一**那个口：`RuleCore.CanPlayCard(Ctx, _me, i, slot) == RuleCodes.OK`（`RuleEngine/Core/RuleCore.cs:1922`）。
· 同文件 **`:3100` 已经这么用了**（落点预览那个委托）—— 同一份判据写两遍迟早不一致（CLAUDE.md §三）。
· `slot`：走 `BoardSlots.Resolve` / `interaction` 那一份（`CanPlayCard` 只在单位卡上用第 4 参）。
· 风险：核一遍 `CanPlayCard` 是**纯查询**（不许有写 ctx 的副作用）—— 教程闸门在它外面（`:3099`），不在里面。

**改点 2（真偏离 B）** — `CardHighlight.cs:212`
**⛔ 不要在 `OutlineOf` 里再加第二个 `if`**（那就成第三处规则）。把判据换成引擎那一个口 `BattleContext.IsEphemeral(inst)`（`BattleContext.cs:1171`，已是「关键词 ‖ 标记」）。
最小改法：`OutlineOf` 的入参由 `CardDef` 换成 `CardInstance`，或直接由 `RefreshHandPlayable` 传 `Ctx.IsEphemeral(inst)`。
🔑 **本 bug 的错因就在眼前**：同一个循环里 `BattleDriver.cs:11564`（`ShowEphemeral`）传的是对的 `Ctx.IsEphemeral`，
而 `:11805`（`SetOutline`）传进去的是错的 `OutlineOf(card, …)` ⇒ **一处对一处漏**。

**改点 3（顺带，同族）** — `CardHighlight.cs:117-129`
`FrameColorOf` 缺原版 state 5 `displayingActiveAbility`（→ `PlayableColor (1,1,0)` 黄、**瞬切**，`CardHighlight__ChangeState.c:63-70`）。我们枚举里没有这一态。

**改动风险（共用件）**：
· `CardView` 是**所有卡视图**共用 —— ⛔ 别动 `SetOutline` 的补间结构（`:4499` 那条早退是 2026-09-19 实测踩出来的：少了它补间永远推不动、**描边一直不亮**）；
· ⛔ 别动 `StampQueue(_rim)` 那套队列（A1665）；⛔ 别把 `AddLayer` 的 `new Material(mat)` 改成共享材质（会让所有手牌同色）。
· 覆盖面：动了 `CardView`（共用件）⇒ 收口时跑 `BattleScene.Run` + `CardBaseDemo.Run`（四档分辨率）。

## 六、没查清的部分（不猜）

1. `ChangeToHighlightColor.c:22` 的 `rawCardScript + 0x90 == 0xe6` —— 我**只沿用**了本仓既有结论「= `SpellType.Sabotage 230`」（`CardHighlight.cs:203`），**没重新核** `RawCardScript` 偏移 `0x90` 到底是哪个字段。
2. `Card2DController` 里 `cardHighlightMaterialID`(+0x84) / `cardShadowMaterialID`(+0x80) 的**赋值点没找到**（`Awake` 只看到取 `image.material` 存 +0x88）⇒ 「属性名 = `_Outline`」是按本仓既有结论读的，**没取到字面名**。要钉死得读 `.cctor` 或 `Shader.PropertyToID` 调用点。
3. `PlayerHand__LightPlayableHand.c:25,29` 跳过的那两个 `cardType`（4 / 10）**没查到是哪两种**。
4. **本轮没跑 Unity、没进真机**（只读诊断）⇒ 「改完到底亮不亮」**没有实机证据**，本报告只到代码级判定。
