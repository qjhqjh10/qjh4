# B146 · A27 CamFeel 两层 + E4 AI 残差（2026-11-05）

> 只读判据代理 `R5`。没跑 Unity / typecheck、没动 git、没改任何源码/账/表。判据一律现读，出处给 `文件:行` 或资产路径 + 字段。
> 「今天可观测」一律以**池内数据**为准（`Resources/ai_scoring.json` 现扫，`full_raw` 已跳过）。

---

## 一、A27 原版两层是什么

**两层 = ① Cinemachine 写相机位姿（base，含开场运镜）· ② ImpulseListener 把 impulse 偏移叠在它上面。两个写手不在同一个 transform 上。**

| 证据 | 现读 |
|---|---|
| 震源 | `CameraShakerManager._DoShake_d__33__MoveNext.c:59-63,99-101`：`ToggleListener(true)` → `CinemachineImpulseSource.GenerateImpulseWithVelocity(velocity)`；**全文件不写任何 `Camera.transform`** |
| 同上 | `CameraShakerManager__DoImpulse.c:9-15`（同一形状）；`:30,34-37` 是 `shakesEnabled(+0x60)` 闸 |
| 监听器 | `assets_full/bundle_scenes_scenes_battlearena1/MonoBehaviour/MonoBehaviour_4546.json`：`m_ApplyAfter: 2`（Noise 阶段之后）· `m_UseCameraSpace: 1` · `m_Gain: 1` |
| 管理器实例 | 同目录 `MonoBehaviour_4544.json`：`useCanvasShake: 0` · `canvas: null` · `impulseListener → 4546` |
| UI 不晃的机制 | 原版有两台相机（BoardCamera / UI Camera，见 `判据/B146_C09_C10_空间与层语义评估.md` §1·1）；impulse 只挂 BoardCamera 那条 Cinemachine 链 |
| Canvas 支路 | 若 `useCanvasShake=1`（主菜单那份），`DoShake` 会把 `Canvas.renderMode` 切到 2(WorldSpace) 跟着晃；13 场战斗全是 0 ⇒ **战斗 UI 不参与** |
| legacy 支路 | `CameraShake__DoDirectCameraShake.c` 全文 = `return;`（空桩）；`CameraShake._Reposition_d__18__MoveNext.c:57-60` 才直写一串 transform 的 `position` —— 那是**旧机制**，战斗走的是 impulse 那条 |

⇒ 原版「两层」是**结构保证**：base 由 Cinemachine Brain 每帧写，offset 由 listener 在 Noise 之后加，**互不覆盖、天然相加**。开场运镜（`cameraAnim.Play()` 播在 `Cinemachine Vcam` 上）就是 base 的一部分。

---

## 二、A27 我们的现状与可达性

**我们：单层。base 与 offset 混在同一个「起播时冻结的 `camHome`」里。**

| 落点 | 现读 |
|---|---|
| `Core/CardFeel.cs:1425-1433` | `camHome = camTr.position`、`camHomeRot = camTr.rotation`（**起播时冻结**） |
| `:1467` / `:1476` | `camTr.position = camHome + d` · `camTr.rotation = camHomeRot * q`（每帧**整份覆写**） |
| `:1503-1513` | `OnKill` 把相机复位到**那份冻结值**；`LastShake` 供自检 |
| `:1423` | 每次起播先 `SettleShake()`（把上一次摇完）—— 处理的是「上一次没收干净」，**不处理 base 在动** |

**相机 transform 的写手只有三处**（全仓 grep，排除注释）：
`Core/LayoutSpace.cs:80-81`（`Apply`：position=(0,0,-20) + identity，Start 时一次）· `Battle/CameraIntro.cs:367`（`SampleAnimation(_cam.gameObject, t)` 每帧写 localPosition/localRotation，`:359-360` 是 `Stop` 复位）· `Core/CardFeel.cs` 上面三行。

**生产调用点（震屏）**
| 入口 | 触发 |
|---|---|
| `Battle/BattleDriver.cs:22995` → `ShakeCameraForAttack` | 挨打（Hit 演出） |
| `:25040` `PlayDeployFeel` → `ShakeCameraForLand` | 任何单位落地（**含 AI 出牌**，`:25036` 明写） |
| `:35007` `HeroLandInto` → `ShakeCameraForLand` | 督军入场 |
| `:3607` `HookAnimFxShake` → `ShakeCamera` | AnimFX 模块震屏（preset 自带参数） |

编辑器里另有 `Editor/BattleScene.cs:24591/24668/24677`、`Editor/B104_CardFeel_Assert.cs:295/306/372`、`Editor/B120_W4_Assert.cs:187-211` 直接调（自检）。

**可达性判定**
- **批处理永不出现**：开场运镜生产路径在 `Application.isBatchMode` 下 `_batchSuppressed`（`CameraIntro.cs:311-317`：不建播放态、不 tick、不写位姿）⇒ 冲突窗口在自检里结构上不存在，现有断言测不到。
- **真机可叠加**：运镜在 `Begin` 里起播（`BattleDriver.cs:5816`）、**先于** `OpenMulligan`，时长 2.5 s 量级（clip 长度，`CameraIntro` 表）。换牌确认后首回合的 Deploy/Hit 若落在 2.5 s 内就重叠。
  - 人类正常节奏多半 >2.5 s；**教程脚本回合 / `-wfdrive` 自动出牌 / 回放（`Begin` 里同步跑完整份动作表后由时间线按实时播）/ 联机快节奏** 可能 <2.5 s。
  - ⚠️ 只读代理，**没在真机上实测**这条窗口（见 §六）。
- **重叠时的具体故障**：`apply` 每帧把相机写回 `camHome + d` ⇒ 运镜剩余段**整段被吃掉**（相机停在中段）；`OnKill` 复位到冻结位姿，下一帧运镜再写回正确位姿 ⇒ **一次可见跳变**。不重叠时行为完全正确。
- **反向也成立**：运镜若在 shake 进行中把位姿写回 base，那一帧的 offset 会丢（DOTween 的 Update 与 `BattleDriver.AdvanceTimeline` 都是 `Update`，**没有约定的执行顺序**）。

**与 C-09 的联动（结论：不影响 A-27，但决定 hudRoot 那一半该不该做）**
- `HudCanvas` 是 `ScreenSpaceCamera` + `planeDistance 20.3`（`Battle/HudCanvas.cs:54`、`:109-120`），Unity 每帧把它摆到相机前方 ⇒ **推相机对画布件无效**。这恰好等于原版「UI 相机不挂 listener」的结果，**是想要的**。
- 画布是**独立根对象**（`HudCanvas.Ensure` 里 `new GameObject("BattleHud Canvas")`，无父级），**不在 `hudRoot` 下** ⇒ `ShakeCamera` 里对 `hudRoot` 做的刚体反补只对**世界档 HUD**（`CardView.Create(hudRoot,…)`、揭示卡、教程世界画布）有意义，对画布件本来就是空转。
- ⇒ C-09 那条发现**不动摇 A-27 的两层设计**；它只说明「想让画布跟着震」这条路在 `ScreenSpaceCamera` 下走不通（将来也⛔ 别按那个方向改）。base 运镜同样不影响 uGUI，与原版一致。

---

## 三、A27 结论与最小改法

**结论：真缺口（结构差导致的可观测缺陷），不是「结构不同但行为等价」。** 反证只有一条能成立——「2.5 s 内不可能有任何 Deploy/Hit」——而代码里**没有任何闸**保证这一点（`_cameraIntro` 只用于 Tick/Stop），回放/教程/自动出牌三条路都可能是早事件。

**最小改法（推荐：位姿拆两个 transform，base 归运镜、offset 归震屏）**
1. 给 BoardCamera 加一个 `BoardCameraRig`（父 = 场景根），相机 reparent 到 rig 下、相机 local = 0；rig.local = 原相机位姿。**建议运行时在 `BattleDriver.Begin` 建**（避开场景重生成与 `!u!115`）。落点：`Battle/BattleDriver.cs`（建 rig 一处）+ `Battle/CameraIntro.cs`（新字段）。
2. `CameraIntro`：`SampleAnimation` 改采到 **rig**（`CameraIntro.cs:367`）、`_homePos/_homeRot` 改读 rig（`:261-262`）、`Stop` 写 rig（`:359-360`）；`Pose*ForTest` 三个口（`:523-541`）同步读 rig。
3. `CardFeel.ShakeCamera`：改成只写**相机相对 rig**的 offset（`camTr.localPosition = d`、`camTr.localRotation = q`），`OnKill` **归零**而不是复位到 `camHome`；删掉 `:1425-1433` 的冻结。`hudRoot` 那一半**原样保留**（它是另一个对象，不为零）。
4. `LayoutSpace.Apply`（`:80-81`）：位置/旋转改写到 rig（或先建 rig 再调，两者顺序必须定死）。

**影响面**：3 个 `.cs` + 1 处建 rig；读 `boardCam.transform.localPosition` 的断言要改成读 rig 或读合成值（`BattleScene.cs` 的 T12 开场运镜那组、`B104_CardFeel_Assert`、`B120_W4_Assert`）。
⛔ **不能只改 `CardFeel` 一处**——「谁写 base」不解决，两个写手仍在同一个 transform 上，只是把覆盖换成「一帧丢 offset」。
⛔ 备选「shake 期间暂停运镜 / 运镜期间吞掉 shake」都与原版不符（原版是叠加），只能当止血。

**建议同时补一条行为断言**（B 报告已要求）：`StartCameraIntroForTest` 播到中段 → 起一次 shake → 推完 → 断言相机回到「运镜在该时刻应有位姿」而非起播位姿。

---

## 四、AiTargets vs CheckIfMeetsCriteria 逐条对表

原版签名：`CheckIfMeetsCriteria(thisCard=param_1, evaluatedCard=param_2, targetCard=param_3, criteria=param_4)`（`FilterMethods__CheckIfMeetsCriteria.c`，515 行）。我们的对照物 = `Data/AiTargets.cs` 的 `Match`（`:591-715`）+ `ApplyFilters`（`:576-584`）。

| 条件 | 原版 `.c` | 我们 | 判定 |
|---|---|---|---|
| `ta==0` | `:57-59` return 1 | `Match:599` return true；`GetTargets:144` 另一支 | ✅ |
| `ta==20` self | `:60-74` evaluated==thisCard | `:609` `isSelf`（有实例按实例） | ✅ |
| `ta==30` target | `:76-90` evaluated==targetCard | `:610-611` | ✅ |
| `ta==40` attacker | `:185-192`（0x28）evaluated==thisCard，早返 | `:612` isSelf | ✅ |
| `ta==70` abilityOwner | `:185-192`（0x46）同上 | ❌ 落常规档 | 池内 **0 行** |
| `ta==80` actingCard | `:168`（0x50）**return 1** | `:612` isSelf ⇒ 会淘汰 | 池内 **0 行**，属多筛 |
| `ta==90` | `:193-201` 身份不符 return 0，相符**继续常规档** | `:615` 同 | ✅ |
| `ta==100` | 常规档 + 尾段 `:407-425` 相邻判定 | 常规档 + 建池侧筛相邻 | 池内 0 行 |
| `ta==140` | `:171` 跳过常规档、走尾部 `List.Contains` | 落常规档 | 池内 **0 行** |
| `ta==150` | `:172-183` 前表 Contains 后再落常规档 | 缺前表闸 | 池内 0 行 |
| `ta==200/201` | `:92-124` thisCard+0x278 列表 Contains（201 还要 IsInPlay） | 落常规档 | 池内 0 行 |
| `ta==235` | `:126-157` 天赋卡名相等 | 落常规档 | 池内 0 行 |
| `ta==220` | **`.c` 里没有这一档** ⇒ 常规档 | `:610` 当成 target 身份（从 `GetTargets` 那侧搬来的） | **语义错**；池内 1 行（BL49），今天分值 0（§五） |
| 其余 ta（10/50/51/52/55/60/65/110/113/…） | 落常规档 | 落常规档 | ✅ |
| `excludeSimilarCards` | `:203-215` 比 **rawCard 引用** | `:619` 用 `isSelf`（有实例按实例） | 池内 0 行；语义略窄 |
| `friendlyUnits`/`enemyUnits` | `:216-228` 比 `isPlayer(+0x40)` | `:621-629` `t.Side == thisSide` | ✅ |
| `includeSelf`/`excludeNotSelf` | `:232-243` 比 CardScript 实例 | `:632-633` | ✅ |
| `excludeFriendlyHero`/`EnemyHero` | `:244-272` `IsWarlordEquivalent(候选)`+isPlayer | `:636-638` `IsHeroCard`（`Unit.IsWarlord` / cardType 10） | ✅ |
| `includeRemnants` | `:273-278` 读候选 `+0x65` | `:641` `Unit.IsRemnant`（手牌候选恒 false） | ✅ |
| 卡种白名单 | `:280-293` 只读 Hero/Minions/Spells/Whispers；**+0x1a includesWeapons / +0x1c includeHeroParts 从不读** | `:643-648` 同样只读四格 | ✅（`headParts/weapons` 都属「不读」） |
| `spellType` | `:294-297` 候选 `rawCard+0x90` | `:651-655` `AiScoringData.spellType`（同源快照） | ✅（数据面 1 行 230） |
| `conditionsFilter` | `:298-303` `MeetsConditionCriteria(候选, cond, ex, thisCard)` | `:658` → `ConditionEval`（唯一判据） | ✅ |
| `keywordFilter` | `:304-313` `RawCardScript.IsKeyword` = **候选 `keyword(+0x98) == filter`**（`RawCardScript__IsKeyword.c` 全文一行） | `:661-666` `AiScoringData.keyword == filter` | ✅（同一字段；21 行 / 4 个值） |
| `traitsFilter` | `:314-317` `MeetsTraitCriteria` 读候选 trait 列表 `+0x128`（另有一条 second-list 分支） | `:670` `CardDef.Has(name)` 读 `_keywords`，名字由 `TraitName` 映射 | ⚠️ **映射缺 260 slay / 1230 teleport**（§五） |
| 攻组 | `:326-328` (候选 `CurrentMeleeAttack`, 载体 `CurrentMeleeAttack`, alt=候选 `CurrentRangeAttack`) | `:680` (t.Attack, `thisCard.Attack`, alt=t.RangedAttack) | 候选✅ / 载体侧 = **卡面基础值**（只 option 4/5/7 用，0 行） |
| 血组 | `:349-352` (候选 health, 载体 `CurrentMeleeAttack`, alt=候选 bastion) | `:681` (t.Health, thisAtk, thisBastion) | 同上（线上 4 行 = option 2，不读这两参数） |
| 费组 | `:354-360` (候选 **`CurrentCost`**, 载体 `CurrentMeleeAttack`, alt=0) | `:682` (t.Cost = **`CardDef.Cost` 基础费**, thisAtk, 0) | 候选侧也不同；`manaCostFilterOption` 全 0 ⇒ 0 行 |
| 稀有度 | `:367-377` 候选 `rawCard+0x28`，**无条件调** | `:684-689` 仅 option!=0 时调、判不了 `false` | 池内 0 行 ⇒ 等价 |
| `filterSpecificCards` | `:465-483` 按卡名相等 | `:692-700` `SameCardId` 按 **id**（生成器已把 PPtr 解成 id，如 `SAU52 → "SAU1"`） | ✅ 1 行 |
| `excludeEnchantments` | `:430-463` 比**候选身上的附魔列表 `+0x110`** | `:703-707` 比**候选卡本身** | **语义错**；池内 0 行 |
| `armyFilter` | `:396-406` `MeetsArmyCriteria` | `:710` `MeetsArmy` 只实现 0 档（其余恒 true） | 池内 0 行；**未覆盖** |
| `excludeTargetted` / `includeWeapons` / `includeHeroParts` / `selectFromChooseResolution` | `.c` **从不读**（grep `param_4 + 0x20/0x1a/0x1c` = 0 命中） | 也不读 | ✅ |

**「多」的只有一处**：`ta==220` 的 target 身份特判（`:610`）。它是从 `AbilityLogic__GetTargets.c:192-206`（`0x1e`/`0xdc` 同支、建池）搬到**逐候选筛**上的，而 `CheckIfMeetsCriteria.c` 里没有 220 这一档。⇒ 收口时必须把「建池」与「逐候选筛」两个原版函数分开，⛔ 别拿 `GetTargets` 的 ta 特例去补 `Match`。

---

## 五、收口方案与影响面

**今天可观测性（现扫 `ai_scoring.json`：1,351 卡 / 882 条 criteria）**

| 字段 | 现读 |
|---|---|
| `attackFilterOption` / `manaCostFilterOption` / `rarityFilterOption` | **882/882 = 0** |
| `currentHealthFilterOption` | 878 行 0 · **4 行 2**（EqualOrHigher，不读 thisValue/alt） |
| `traitsFilter` | 16 行 / 11 个值：70 1225 330 260 1230 70 390 920 1200 1260 1170 1000 |
| `conditionsFilter` | 27 行 / 6 值（10/40/221/243/340/341，`ConditionEval.KindOf` 全 Ok） |
| `keywordFilter` | 21 行 / 4 值（5/10/30/35） |
| `filterSpecificCards` 1 行 · `includeSelf` 1 行 · `armyFilter` 0 行 · `exclude*` 全 0 · `spellType` 1 行 | — |

⇒ **E4 的判断成立**：费组/攻组的两份实参（`t.Cost` vs `RuleCore.CurrentCostOf`、`thisCard.Attack` vs `CurrentMeleeAttack`）**今天 0 可观测**——option 4/5/7 池内 0 行，option 0 整组跳过，唯一非零的 option 2（血组）不读这两个参数。

⇒ **但有两处已经能影响分值**：
1. 🔴 **`TraitName` 缺 260 / 1230**（`AiTargets.cs:1053-1100` 的 switch）⇒ `MeetsTrait` 恒 false。池内 16 张卡带 `slay`、7 张带 `teleport`（`cards_truth.json` 的 `cardTraits` 现数），而 **`DA20 Master Lazarus` 的 activeAbility 两条 cardTargetScoringCriteria 正是 traitsFilter 260 / 1230（ta=90）** ⇒ 这两条 AI 评分恒 0（原版能命中 slay/teleport 目标）。**这是一条真缺口，两行即可修。**
2. ⚠️ `traitsFilter 330`(markOfChaos)/`1260`(huntMark) 映射有名字、但池内没有默认带这两个 trait 的卡 ⇒ 今天等价（运行时是否可能被临时授予未查，见 §六）。
3. ⚠️ `BL49` 那条 `ta=220` 语义错，但 `baseValue=0` 且 `multiplyByEnemy=0`、目标为敌方 ⇒ `ScoreFromCriteria` 回 0 ⇒ **今天 0 可观测**（latent）。

**方案 A（最小、单文件、今天可做）**：① 补 `TraitName` 的 260/1230；② 费组改 `RuleCore.CurrentCostOf(ctx, t.Side, t.Inst)`（board 单位传 `t.Unit.Instance`）；③ `Match` 删掉 `ta==220` 的 target 身份特判（`GetTargets` 那侧的 30/220 保留，那是对的）；④ `excludeEnchantments` 改成比候选 `ActiveEnchantments`；⑤ `MeetsArmy` 按 `MeetsArmyCriteria.c` 补齐其余档。不动签名、不跨文件，风险最低。

**方案 B（结构收口：让 AiTargets 转调唯一求值器）** —— 可行，但**不是薄包装**：
- `TargetCriteriaEval.Matches`（`:93-387`）只吃 **`UnitState` 候选**，而 AI 有 **手牌/牌库/弃牌候选**（`GetTargets` 的 ta 50/51/52/60/65 池）⇒ 必须先抽「候选视图」（`CardDef + CardInstance + UnitState + Side + IsRemnant + IsWarlord`），并给 `Matches` 加这个视图形参。
- 调用点：`Matches` 现有 13 个调用点（`EffectResolver.ResolveTargets`/`PlayTargetPool` 一族、`CountEval` 等，A-18 已记「6 个在白名单外」）。
- 两个**不能抹掉**的差异位：① 判不了时 AI 侧取 `false`（丢候选），`Matches` 返回 `null`（留候选）；② 原版本来就是**两个函数**（`GetTargets` 建池 vs `CheckIfMeetsCriteria` 逐候选），ta 120/130/160/161/162/170/190/220/230/240/245 在两条路上答案不同 ⇒ 单一求值器必须带「哪条路」的标志位，否则会把 §四表里那些「池内 0 行」变成新的静默差异。
- 建议与 A-18（`Matches` 第 9 形参 `actingCard`）**合并做一次**，都动同一个签名。

**方案 C（低成本保真）**：保留双实现，补一条机检——同一批 ta/字段在两份实现上跑同一夹具，差异必须落进白名单文件。把「两处写同一条规则」从不可见变成可观测。

**推荐**：先 A（判据已定、两行修掉真缺口），B 等 A-18 一起做，C 作为长期闸。

---

## 六、没查清的

1. **A-27 真机窗口未实测**：只读代理不能起 player。需要一条日志/真机：`Begin` 后 2.5 s 内是否出现 `EvtKind.Deploy` / `EvtKind.Hit`（教程关、`-wfdrive`、回放三条路各一次）。
2. **回放首事件落在 intro 窗口内的概率未量**：`BattleDriver` 的回放分支（`:1166-1173`）在 `Begin` 里同步跑完整份动作表，但演出的时间线偏移没有累计实测。
3. **Cinemachine ImpulseListener 的叠加数学**只用了三条间接判据（`m_ApplyAfter=2` + `m_UseCameraSpace=1` + `DoShake` 不写 transform）；本机没有 Cinemachine 源码（只有 dll 与签名桩），没读 `ImpulseListener` 的实现体。
4. **legacy `CameraShake` 是否在战斗里被调用**：只判定 `DoDirectCameraShake` 是空桩、`DoTimedCameraShake→DoLegacyShake` 是旧路；没有逐场景核 `CameraShake` 组件的挂载与调用者。
5. **E4 · traits 330/1260 的运行时来源**：池内无默认卡；运行时是否可能被临时授予（若会，`MeetsTrait` 的 `CardDef.Has`（读 `_keywords`）能否命中要单独核）。
6. **E4 · `MeetsTraitCriteria` 的第二条 list 分支**（`.c:37-57`，命中即恒 false）语义未完全解出；池内 traitsFilter 无一落进该族，所以今天不判。
7. **E4 · 手牌卡 `CurrentMeleeAttack` 没有唯一投影口**：`new UnitState(inst,false)` 只读卡面值（`EffectResolver.cs:2214-2217`）⇒ 将来 option 4/5/7 真出现数据时，要先定「手牌当前近战」的求值器，否则会变成第二份口径。
