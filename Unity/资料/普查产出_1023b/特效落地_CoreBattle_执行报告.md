# 特效落地（`CardPresentation/Core/**` + `CardPresentation/Battle/**`）· 执行报告

> 代理：**「特效落地（Core / Battle 侧）」**（2026-10-23）。
> 判据来源：`普查产出_1023b/特效线_执行报告.md`（§三/§四/§五/§B18）+ `普查产出_1022/P5_四未查清.md` §⑦。
> 红线：**没跑 Unity / 没动 git / 没改正本台账**；只改白名单内的文件（见 §五）。
> 判据优先级：**反编译方法体 > 解包资源字段 > 成品卡图**。

---

## 〇、结论（先看这一节）

| # | 事 | 结果 |
|---|---|---|
| 1 | **Remnant ③**（`remnantIdleParticleSystem`/`card3D`/`remnant`） | ✅ **现核已做完**（2026-10-22 就落了，报告那句是按「文件归属」写的、不是现读）⇒ **核销，未改码**（§一，那一带共 23 条 `Check(`） |
| 2 | **R8** 多 `activeAbility` 按行序拼 | ✅ **原判据被反编译推翻**：原版**就是遍历全部行逐个播** ⇒ 行序拼接 = 忠实；已就地订正注释（§二·1） |
| 3 | **R10** 屏震固定基准 | ✅ **原判据不成立**：`GetModifiedPreset` **没有** `GetUnitSize` ⇒ 固定基准才是原版；魔数换成有出处的两个常量（§二·2） |
| 4 | **R16** `SnapBackDuration` 零出处 | ✅ 查了账上指的方法 —— **那一支不存在**（`OnTouchDrag` 全文 52 行）⇒ 按铁律 3 标成「我们挑的」（§二·3） |
| 5 | **R17** `IsSourced()` 恒 `true`（自证） | ✅ 改成逐档查表（`Death`/`Ability` 两档 false + 带原因）＋断言改口径（§二·4） |
| 6 | **R19** 抽卡敌方 0.15 没接 | ✅ `DurationOf(BattleEvent)` 按座位分档（`IsLocalSeat` 钩子）＋驱动挂钩（§二·5） |
| 7 | **R20** `ManaTransformOf` 没挂 | ✅ 挂 **61/71 灵石**（现成件）；Psychic/Plasma 那 8 档**照原版不造件**（§二·6） |
| 8 | **R26** carrier 叠旋转 | ✅ 朝向那一拍把新实例的局部旋转**归零**（= 原版写 `transform.forward` 的覆盖语义）（§二·7） |
| 9 | **B18 尾巴**（`effectsAnchor` / 停 0.5 s） | ✅ 两项都落：挂 `CardView.EffectAnchor(true)` + `autoDestroySeconds = 0.5`（§二·8） |
| 10 | **R21** ×1.27 / `FrameCutout` | ⚠️ **主体早已有出处**（`bigSizeMultiplier` / `useOverDraw` 真值）；**只剩 `Shaders/FrameCutout.shader` 是零引用孤儿**，该目录**不在本代理白名单** ⇒ 留给主对话（§四·2） |
| 11 | **R9 / R11–R14 / R18** | ⛔ **没做**，各有明确原因（判据缺 / 要 Unity 并排 / 缺一跳数据）⇒ §四 |

**自检**：`TMPDIR=/tmp/wf_fxcore bash 工具/typecheck.sh` → **运行时 0 / 编辑器 0**（每批改动后都跑过；收尾那次也是 0/0）。
⚠️ **中途出现过两次「别人的半成品」假错**，都不是本代理的文件、也都已由各自的主人修好：
① `Editor/MainMenuScene.cs:2884` `CS0103 p0Hl`（另一个代理正在写，随后变 0）；
② `RuleEngine/Core/RuleCore.cs:3944` `CS0103 ActionPump`（同上）。
⇒ **归本代理文件的错误 = 0**。
⚠️ **没有跑**任何 Unity 宿主（红线）。

---

## 一、Remnant ③ —— **已经做完了**（核销）

**现读证据**（`Core/CardView.cs`，全部 2026-10-22 落的）：

| 原版字段 | 我们的等价物 | 位置 |
|---|---|---|
| `remnantIdleParticleSystem`（`+0x70`），`Toggle(false)` 那一支 `ParticleSystem.Stop` | `StopRemnantIdleParticles()`（按节点名 `Particles Death Idle` 找，无参 `Stop()`） | `CardView.cs:2216-2237`，调用点 `:1982` |
| `remnant`（`+0x40`）· `card3D`（`+0x68`）各一次 `CardMaterialHelper.ConfigureMaterial` | `ConfigureRemnantMaterials()` → `RemnantMaterialNodesOf()` = 死灵**两颗** / 灵族**一颗**，写 `_CardImage` + `_MatCap` | `CardView.cs:2253-2310`（表在 `:2265-2272`） |
| `Toggle(false)` **保持残骸体可见** + `Animation.Play(reanimateAnimationName)`，演完由动画事件收尾 | `SetRemnantBody(false, …)` 那一支：`_remnantHideAt` 计时 + `TickRemnantHide`（事件 `AnimationResurrectionEnd` @1.8 s）+ 幂等闸 | `CardView.cs:1958-1994` · `:2138-2212` |

**断言已在**：`Editor/BattleScene.cs:12870-13035` 一带共 **23 条 `Check(`**（`RemnantAnimationPlayCalls` / `RemnantIdleStopCalls` /
`RemnantMaterialTexture(...)` 真值回读，含「**灵族撤场一次都不播**」与「死灵必须播」这一对灭自证）。
⇒ 本代理**一行没改**，只核销。

---

## 二、本批落地的 8 笔

### 2·1 R8 —— 多 `activeAbility` 行：**原判据是错的**

- **账上写的**（`普查产出_1022/A1_自造与兜底_特效动画.md:38`）：「原版 `AbilityLogic.PlayAnim` **只播当前那一条**」。
- **反编译现读**（第一权威）`d:/2/tools/decomp_full/RawCardScript__ResolveActiveAbility.c`：
  ```
  lVar6 = hasAlternativeAbility(0x2AA) ? (MeetsCriteria(0x2B0) ? activeAbility(0x260)
                                                             : alternativeAbility(0x2B8))
                                       : activeAbility(0x260);
  lVar6 = *(lVar6 + 0x48);                       // ActiveAbility.activeAbilityLogic : List<AbilityLogic>
  while (i < list.Count) { AbilityLogic__PlayAbility(list[i], mgr, abilityData, 400, 0); i++; }
  ```
  ⇒ 原版**遍历全部 `activeAbilityLogic` 行、按行序逐个 `PlayAbility`**（每行各自 `PlayAnim` 自己那份 `cardAnimations[]`）。
  **行序拼接 = 逐拍等价，不是近似。**
- **改了什么**：`Battle/CardAnimOrder.cs:41-58` 把「唯一还留着的近似」那段整段重写（保留订正痕迹）。
- **真正的缺口（不在本表）**：`hasAlternativeAbility` 时 `activeAbility` / `alternativeAbility` 二选一，
  判据字段 = `normalAbilityBoardCriteria`(**`+0x2B0`**)，**满足时走普通支、不满足才走替代支**。
  我们的事件只带卡名、分不出是哪一份；**而且数据侧也缺**这个字段（`cards_raw.json` 的 `cards[*]` 只有
  `hasAlternativeAbility` / `activeAbility` / `alternativeAbility`）。
  ⇒ 要接得两件事一起：① `BattleEvent` 带一位「哪一份」（`RuleEngine`，不在本代理）② 卡导出补那个字段（工具线）。
- **顺带订正**：`Core/EventTiming.cs:36-48` 与 `Core/AbilityAnimDurations.cs:48-59` 原来都写
  「`MeetsCriteria(**selectCriteria`)** 满足时走替代支」——**字段名与方向都不对**（卡上没有 `selectCriteria`），两处已改。

### 2·2 R10 —— 屏震幅度：**「原版按 `GetUnitSize` 覆盖 2/4/8」不成立**

- 现读 `d:/2/tools/decomp_full/OverwriteCameraShakePreset__GetModifiedPreset.c` **全文 150 行**：
  `grep GetUnitSize` **零命中**（它只按 `overwrite*` 开关换 amplitude/frequency/sustain/direction/attack/decay）。
  全仓调 `SupportMethods.GetUnitSize` 的只有 `CardScript.ResolveAttackAnimationEffects` / `ReceiveAttackAnim` 等
  **挨打那一档**（我们已走 `CardFeel.ShakeCameraForHit`）。
  ⇒ **AnimFX 这条支路本来就没有尺寸档**，按 `amplitude × |direction|` 线性放大**才是原版**。
- **改了什么**：`Battle/BattleDriver.cs:2538` 把魔数 `0.5657f` 换成
  `CardFeel.ShakePresetAmplitude * CardFeel.ShakePresetDirMag`（推导 `ShakeWorldAmplitude` 时用的同一对常量）；
  注释里写清「⛔ 别改成 `ShakeWorldAmplitudeFor(size)`」（`BattleDriver.cs:2440-2454` 一带）。

### 2·3 R16 —— `SnapBackDuration = 0.22`：判据**确实缺**，按铁律 3 标死

- 账上指「查 `CardScript.OnTouchDrag` 取消出牌那一支」—— **去查了，那一支不存在**：
  `d:/2/tools/decomp_full/CardScript__OnTouchDrag.c` **全文 52 行**，只有一句卡状态闸（`+0x1c != 0xa0`），
  没有任何回弹 / `DOMove` / 补间时长。
- **改了什么**：`Core/CardTween.cs:21-31` 把常量标成「**我们挑的**」，并明写
  「⛔ 不许拿 `VarsGlobal.timeToPositionCard`（0.2，那是**手牌重排**的出处）硬套」。
- **没查清**：原版「拖出牌 → 松手 → 回原位」那条链到底走哪个方法（见 §四·1）。

### 2·4 R17 —— `IsSourced()` 不再自证

- **改了什么**：`Core/EventTiming.cs:556-597`
  - `IsSourced(EvtKind)` 逐档查表；
  - 新增 `IsSourced(EvtKind, out string unsourcedPart)`（`false` 时说明「哪一格没出处」）；
  - 两档 `false`：`Death`（时长 0.2 有出处，但 `DelayBetween` 的 `DeathHold` **0.1 我们挑的**）·
    `Ability`（逐卡真值有出处，但**查不到那张卡**时退回的 `AbilityFallbackSeconds = 1.0` **不是原版值**）。
- **改了什么（断言）**：`Editor/BattleScene.cs:1310-1345` 把原来的 `unsourced == 0` 换成
  「**恰好 2 档**且必须是这两档」＋灭自证（同一次调用里必须同时出现 true/false，且 false 要带非空原因）。

### 2·5 R19 —— 抽卡按座位分档（敌方 0.15 接上）

- **改了什么**：
  - `Core/EventTiming.cs:83` 新增 `public static Func<int,bool> IsLocalSeat;`（语义 = 原版那个 `bool isPlayer` = **本地玩家**，联机局本机可能是座位 1）。
  - `Core/EventTiming.cs:310` `DurationOf(BattleEvent)` 加 `Draw → DrawDealOf(e.Player)`。
  - `Core/EventTiming.cs:525-548` `DrawDealOf(seat)`：两档都走 `CardFeel.DealSeconds(...)`（**判据只此一处**）；
    没挂钩 ⇒ 按我方档 + **出声一次**（不是静默）；`seat < 0` ⇒ 按我方档（与改前一致）。
  - `Battle/BattleDriver.cs:2694` 挂钩 `IsLocalSeat = seat => seat == _me;`；
    `:3071` 加进静态钩子清单（`OnDestroy` 会摘，`StaticHookCount` 的「卸后归零」仍成立）。
- **断言**：`Editor/BattleScene.cs:1346-1372`（夹具**故意让本机 = 座位 1**，写死 `seat == 0` 的实现会现形）。

### 2·6 R20 —— 灵石锚点 61/71 挂上

- **判据**：`AbilityLogic__GetPos.c` 的 `0x3d`(61) / `0x47`(71) 两支 ——
  `61` 取 `GetPlayerManager(actingCard.isPlayer)`、`71` 取 `…(!isPlayer)`（两档互为镜像）。
- **改了什么**：`Battle/BattleDriver.cs:2643-2677` 挂 `AnimAnchors.ManaTransformOf`：
  61/71 → `_myStoneIcon` / `_foeStoneIcon`（HUD 现成件，不另摆空物体）；**其余 8 档 return null**
  （Psychic / Plasma 原版这套 HUD 上本来就空 —— `普查产出_1022/A1…` §A-8）。
- **断言**：`Editor/BattleScene.cs:13036-13080`（含「61 与 71 不是同一个 Transform」的灭自证；
  以及 Psychic 仍回 null —— 挡「为了补齐拿灵石顶」）。

### 2·7 R26 —— 技能拍：朝向覆盖 prefab 作者旋转

- **判据**：`CardAnimController__Initialize.c` 的 `RotateAnim` 是 `transform.forward = 锚点 − 自己`
  ⇒ 写的是**实例自己**的 forward，**覆盖** prefab 上作者摆的旋转。我们把朝向落在**载体**上，
  prefab 那层 `localRotation` 会**乘上去**。
- **改了什么**：`Battle/BattleDriver.cs:18084` 新增 `FlattenNewEffectRotation(parent, from, orient)`（public，自检要调）；
  调用点 `:18230`（`CardEffects.Fire` 之后、只有 `orient` 为真才归零 —— `move` 但 `!orient` 时原版**不碰旋转**）。
- **断言**：`Editor/BattleScene.cs:13123-13137`（两档对照：`orient=false` 不动 / `orient=true` 归零 —— 灭自证）。

### 2·8 B18 尾巴 —— 相邻单位：挂 `effectsAnchor` + 停 0.5 s

- **判据**：`AnimFXInstanceParticleAdjacent__ExecuteEffect.c:42-104` → `CreateAnimFromAnimFxController(animInfo, …)`，
  而 `BattleManager__GetAnimTransform.c:64-67` 的 `case 1/2` 取**第二个 CardScript 的 `CardUI`(+0x290) →
  `effectsAnchor`(+0x1A0)** ⇒ 起点 = 终点 = **那个相邻单位的 `effectsAnchor`**；
  `AnimInfo`：`timeAtStartPos = 0.5` / `timeMoving = 0` / `shouldMoveVFX = 0` ⇒ **原地停 0.5 s**。
- **改了什么**：`Battle/BattleDriver.cs:2250-2307`
  - 新增 `AdjacentVfxHoldSeconds = 0.5f`（值来自 `数据/游戏数据/adjacent_vfx_animinfo.json` 的
    `instances[0].animInfo.timeAtStartPos`；表不在 `Resources/` ⇒ 只能落常量，注释写明「第二个实例出现就要改成读表」）；
  - 新增 `PlayAdjacentVfx(prefabName, mine, slot)`：逐相邻单位取 **`CardView.EffectAnchor(true)`**
    （= 原版 `effectsAnchor`；取不到才退回卡根并**出声**），`Play(..., autoDestroySeconds: hold)`；
  - `OnExecute` 那段 lambda 改成调它（原来挂的是 `view.transform`，比 `effectsAnchor` 偏低一个局部位
    `(0, 1.34, −0.128)`）。
- **断言**：`Editor/BattleScene.cs:13082-13122`（造一对相邻单位 → 只在 `sA+1` 有邻居 ⇒ 期望恰好 1 份、
  锚点 == `EffectAnchor(true)`、且 **≠ 卡根**（灭自证））。

---

## 三、判定「不用改」的两条

1. **Remnant ③** —— 见 §一（已做完 + 已有 12 条断言）。
2. **R21 的 ×1.27** —— 现在**有出处**：`CardView.BigArtScale = 1.27` = 原版 `CardImageController.bigSizeMultiplier`
   （`CardView.cs:9646-9650`），且「哪张卡溢出」走**真值字段** `useOverDraw`（`cards_engine.json` 641×1 / 486×0，
   `CardView.cs:9570-9580` 的 2026-10-22 订正）。⇒ 原报告 §五 那条「×1.27 近似」**已过期**。
   ⚠️ **仍差**：`CardPresentation/Shaders/FrameCutout.shader` **零引用孤儿**（`grep FrameCutout` 只命中注释），
   删它**需要改 `Shaders/` 目录**（不在本代理白名单）⇒ 交给主对话。

---

## 四、没查清 / 没做的（**⛔ 不许当结论**）

1. **R16 的原版出处**：`CardScript.OnTouchDrag` 里没有这一支（52 行）；「拖出牌松手回原位」原版到底走
   `PlayerHand._MoveCardsInHandToPosition`（`timeToPositionCard` 0.2）还是别的，**没定位到**。
   ⇒ 现在 0.22 明确标成「我们挑的」，**没有**硬套 0.2。
2. **R9 `rawSignal` 的采样算式**：`Cinemachine.dll` 在本地**是托管 DLL**
   （`d:/2/tools/assetripper_out/AuxiliaryFiles/GameAssemblies/Cinemachine.dll` 等 4 份），
   但本机**没有 ILSpy / dnSpy / ilspycmd**（`d:/2/tools/` 只有 Ghidra + Ruri（shader 用）+ dotnet10），
   装一个要联网 ⇒ **判据缺**（⛔ 没按 Beautify 惯例猜）。
   ⚠️ 另外落地还要改 `工具/gen_shake_presets.py` 与 `Resources/…/shake_presets.json`（**都不在本代理白名单**）。
3. **R18 能力时长没算 tween 段**：判据本身清楚（`_ResolveAnim` 在 `tweenAnim != null && waitAnimation != 0`
   时先 `WaitForCompletion`），**缺的是数据那一跳**：`ability_anim_durations.json` 的 `activeAnims[*].tweenAnim`
   只有 **`m_PathID`**（20 条），而 `Resources/UnitTweens.json` 只按**名字**建键（74 条），
   **没有 PathID → 名字**这张表。补它要改生成器（工具线）。
4. **R11 / R12 / R13 / R14（`CardFeel` 那一族）**：没动。
   - R11 先说清：现在的「相机 + `hudRoot` **同向等量**平移」= **HUD 在屏幕上不动**，与原版
     `useCanvasShake: 0`（只震主相机）**行为等价**；开着的只是「机制是我们的」（`CardFeel.cs:934-1000` 已文档化）。
     要判「等价到什么程度」得**真机并排**。
   - R12/R13/R14 是「三条粒子曲线 / 回手走溶解 / 阵亡爆炸退路」—— 都要**并排渲图**才能判，且改动面在
     `CardFeel` 的动画主体上（本批不抢这块，避免和战斗线同时在飞）。
5. **B14（GrabPass 501）** —— 按简报 **不做**（要 Unity A/B），留给主对话。

---

## 五、改动清单（**只动白名单**）

| # | 文件 | 一句话 |
|---|---|---|
| 1 | `MyGame/Assets/CardPresentation/Core/EventTiming.cs` | R19（`IsLocalSeat` + `DrawDealOf` + `DurationOf(BattleEvent)` 分档）· R17（`IsSourced` 逐档查表 + 带原因重载）· `SourceOf(Draw)` 更新 · SW21 判据订正（字段名/方向） |
| 2 | `MyGame/Assets/CardPresentation/Battle/CardAnimOrder.cs` | R8：把「行序拼接是近似」整段订正为「= 原版 `ResolveActiveAbility` 的遍历」+ 真正的缺口写清 |
| 3 | `MyGame/Assets/CardPresentation/Core/AbilityAnimDurations.cs` | 同一处 SW21 判据订正（与 EventTiming 同步，⛔ 不留两份说法） |
| 4 | `MyGame/Assets/CardPresentation/Core/CardTween.cs` | R16：`SnapBackDuration` 按铁律 3 标成「我们挑的」+ 查证痕迹 |
| 5 | `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` | R19 挂钩 · R20 `ManaTransformOf` · R26 `FlattenNewEffectRotation` · B18 `AdjacentVfxHoldSeconds` + `PlayAdjacentVfx` · R10 基准常量 · `EventTiming.IsLocalSeat` 加进静态钩子清单 |
| 6 | `MyGame/Assets/CardPresentation/Editor/BattleScene.cs` | **只加断言**：R17/R19 改口径（原 `unsourced==0` 那条）· R20/B18/R26 新块 · R10 常量三条（**没动别人刚加的那两段**） |
| 7 | `Unity/资料/任务线/特效线/任务总表.md` | 三行状态更新（B18 尾巴已落 / Remnant ③ 核销 / A1 的 R8·R10·R16·R17·R19·R20·R26 结果） |
| 8 | `Unity/资料/普查产出_1023b/特效落地_CoreBattle_执行报告.md` | 本报告 |

⛔ **没碰**：`RuleEngine/**` · `WarpforgeVFX/**` · `Shell/**` · `Deck/**` · `Effects/**` · `CardPresentation/Shaders/**` ·
`CLAUDE.md` · `项目任务.md` · 其他线台账 · `数据/游戏数据/**` · `Resources/**` · `d:/2`（只读）· git。

---

## 六、断言清单（全在 `Editor/BattleScene.cs`）

| # | 位置 | 盯什么 | 灭自证 |
|---|---|---|---|
| 1 | `:1310-1345` | R17：「**恰好 2 档**没出处」且必须是 `Death` / `Ability` | 同一次调用必须同时出现 true/false，false 必须带非空原因 —— 改回 `{ return true; }` 两条同时红 |
| 2 | `:1346-1372` | R19：本机（**座位 1**）= 0.3 / 对面（座位 0）= 0.15 | 两档必然不同 —— 收回常数 0.3 则两条同时红 |
| 3 | `:13036-13080` | R20：`ManaTransformOf` 非空；61/71 取对侧；Psychic 仍 null | 61 ≠ 71（同一行动方）—— 两档写成同一个就红 |
| 4 | `:13082-13122` | B18：`PlayAdjacentVfx` 恰好 1 份、锚点 == `EffectAnchor(true)` | 锚点 **≠ 卡根**（回退成 `v.transform` 就红） |
| 5 | `:13123-13137` | R26：`orient=false` 不动作者朝向 / `orient=true` 归零 | 两档对照（只写一半就红） |
| 6 | `:19638-19663` | R10：基准 = `ShakePresetAmplitude × ShakePresetDirMag`；`OnShake` 挂着 | 基准是**乘积**、不等于任一因子 |

---

## 七、顺手发现（**只报不改**）

1. ⚠️ `EventTiming.SourceOf(Death)` 仍写着「`VarsGlobal.deathTimeMinionDuration`=0.2」而 `DurationOf(Death)` 走的是
   `CardFeel.DeathDissolveMinion` —— 同源但**不是同一个符号**（今天值相等）。要不要改成引用常量，留给下一个人判。
2. ⚠️ `CardEffects.Fire` **不返回**实例（`void`），所以「新建了哪几个子节点」只能用 `childCount` 差来推
   （`CardEffects.StampNewChildren` 也是这么做的）。R26 沿用了这个口径；若将来要更精确，给 `Fire` 加一个
   `out`/返回 `List<Transform>` 更稳（改 `CardEffects` = 本代理可改，但这批没动它，避免和卡牌线的 `F4` 改动撞车）。
3. 🔴 `Core/CardView.cs` 与 `Core/CardEffects.cs` 在**本批开始时就已经是修改态**（别的代理在改）——
   我**没有**在这两个文件里落任何改动（Remnant ③ 只是核销）。
4. ⚠️ `数据/游戏数据/adjacent_vfx_animinfo.json` 仍**不在 `Resources/`** ⇒ `BattleDriver` 里那个 0.5 是
   **常量**。第二个实例出现时要么把表搬进 `Resources/`，要么把值烘进模块 prefab（工具线）。
5. ⚠️ `OverwriteCameraShakePreset__GetModifiedPreset.c` 里有一段池扩容逻辑（`0x1e` = 30 上限 + `IncreaseCameraShakePresetPoolSize`）——
   我们的 `CardFeel.ShakeCamera` **没有**这个上限/扩容语义（同时 30 次以上震动的场合才会现形）。今天不会撞到，
   记在这里免得以後当成已复刻。
