# B9 · J4FX · 特效动画域 40 条 C 行的施工单（只读判据代理报告）

> 执行代理 **J4FX** · 2026-10-26（第八十四会话「完全复刻」）
> 输入（**未重做**，只补证据）：`账/特效动画.tsv`（`状态==C` = **40 条**，逐条核对 missing 0 / extra 0 / 顺序一致）· `判据/B9_特效触发链.tsv` · `判据/B9_特效资源覆盖.tsv` · `判据/B9_特效参数对账.tsv` · `判据/B9_shader近似映射_对账.tsv` · `报告/B8_AS_特效侧施工单.md` · `报告/B8_AB_特效动画C档.md` · `资料/ANIMFX_实现与接线.md`
> 纪律：⛔ 未改任何 `.cs` / 资产 / 主账表 / 正本；⛔ 未跑 Unity（含 `AnimFXCheck.Run` / `BlendProbe` / `EffectCompare`）；⛔ 未动 git。
> 🔴 **用户口径**：特效「像不像」由用户自己看 ⇒ 本报告**没有**任何截图并排比、没有任何像素结论；全部判据来自**反编译方法体 / 序列化字段 / 字节码 / 属性表**。
> 交付：`判据/B9_特效C_施工单.tsv`（12 列 × 40）· `判据/B9_特效C_派活包.md` · 本报告

---

## ① 结论

### 1·1 一句话

40 条 C 行全部落到了**具体文件 + 具体读数口**；其中 **25 条要改代码/资产**（分成 **11 个可派包**）、**13 条 0 行**（8 条已对齐建议改判 **B** + 5 条只是同包内的语义依据/等价物已在）、**2 条不派活**（1 条建议改判 **D**、1 条判据待读）。

| 类 | 条数 | 账行 |
|---|---|---|
| **要改代码 / 资产** | **25** | 84·87 · 130 · 177·178·179·180·181·182·184·185 · 208 · 362·367 · 403·425·430 · 455 · 462·463·464·465 · 505·507·508 |
| **同包 0 行**（语义依据 / 等价物已在，随包落地即完成） | **5** | 86（`SetMaterial` 只作 84 的语义依据）· 88（结构体那一跳）· 432·433·438（`UIBorderGlow` 三个回调的等价物已在） |
| **已对齐（建议改判 B，只补断言/注释）** | **8** | 150·154·155 · 175 · 187·188·189·190 |
| **不派活** | **2** | 510（建议改判 **D**）· 498（**判据待读**） |

> 合计 **25 + 5 + 8 + 2 = 40**。

### 1·2 「判据查不到」的到底有几条

- **严格「判据查不到」= 0 条**。40 条的原版方法体**全部读到**（`decomp_full/*.c`，必要时用 ILSpy 交叉）。
- **判据缺一层、但读法已明确 = 1 条**：498 `BattleManager.StopTargetedSpellAnim` —— 方法体只有 `Destroy(this+0x370)`，缺的是**前置数据**（逐卡 `targetedSpellAnim` PPtr）。读法已写死：重读 1,351 张卡 SO（`allcards` 2026-10-18 起本地可读）。**全表为空 ⇒ 连同 `StartTargetedSpellAnim` 一起判 D**。
- **判据够但缺一次运行 = 1 条**：362/367 `ScenarioMaterialFader` 的 `doFade = !doFade` —— 机制逐句解出，**先要一次读数**（同一件一次环境变更被调几次）才敢抄。本代理不跑 Unity ⇒ 留给调度台**一次**运行（`BlendCalls` 探针已在）。
- **判据缺但本域外 = 3 处**：`PlayerHand.CurrentSizeMultiplier` 的写点（150/154/155 的 smallScreenUI 档）· `Level Up Effect` 的 shader 名（J-02）· `PlayInvokeAnimation` 绑的 clip 字段（507）。

### 1·3 现读推翻了主账 8 条旧说法（详见 §五）

最重要的三条：**175 的 `boardCenter` 支其实已经实现**（`BattleDriver:23085`）· **187–190 的判重族其实已经实现**（`Enchantment.cs` / `EffectResolver.IsSameHandEffect`）· **508 的 `DOScale(0)` 是错的**（原版是 `Vector3.one × spellPlayedScale`）。

---

## ② 逐条证据

> 下面按「证据族」组织（同一族的方法体形态相同），逐条行号见 `判据/B9_特效C_施工单.tsv` 的「原版出处」列。
> 所有字面量都用 `工具/read_literal.py` 4B 档实读，并附 int 位型（8B 档只在不与 4B 冲突时才用）。

### 2·1 `AnimateUIMaterialProperty` 四行（84/86/87/88）—— 组件已在，缺**接线**

| 证据 | 内容 |
|---|---|
| 字段序（ILSpy + `.c` 双向） | `image(+0x20)` · `initializeOnEnable(+0x28)` · `floatProperty1(+0x30..0x4f)` · `floatProperty2(+0x50..0x6f)` · `changeMaterialOnAnimation(+0x70)` · **`instancedMaterial(+0x78)`** · `originalMaterial(+0x80)` |
| `OnEnable` | `if(!initializeOnEnable) return; CreateInstanceMaterial(); if(instancedMaterial) image.material = instancedMaterial;`（`.c:8-30`）—— 第 3 句与 `CreateInstanceMaterial` 内部那次**重复**，但 **re-enable 时全靠它** |
| `CreateInstanceMaterial` | `if(instancedMaterial) return; originalMaterial=image.material; 源 = changeMaterialOnAnimation ?: originalMaterial; instancedMaterial = new Material(源); name += "(Instanced)"; **image.material = instancedMaterial**;`（ILSpy `CreateInstanceMaterial`） |
| `SetMaterial(m)` | `CreateInstanceMaterial(); instancedMaterial = m;` —— **只有两句**（`.c:1-10`） |
| `Update()` | 两槽各一遍：把整结构体装箱与 default 比 `ValueType.Equals`（`.c` 里是 `il2cpp_value_box` + `ValueType.Equals`），不相等才 `useVector4 ? SetVector : SetFloat`（`.c:40-95`） |
| 我们的落点 | `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`（335 行，8 方法齐 + `OnDisable`/`ResetToOriginalMaterial`/`OnDestroy`） |
| **缺口** | `grep AddComponent<AnimateUIMaterialProperty>` = **0**（只命中组件自己的 `Attach`）⇒ 0 挂载点；另缺 3 条驱动 clip（`Rank Up` / `Division Up` / `Level Up Card`） |

### 2·2 `CardDisplayLevelUpAnimationController` / `…Notifier` 八行（177–182 / 184 / 185）

- **字段序**（ILSpy `:10-24` + `.c` 偏移互证）：`myAnimation(+0x20)` · `impulseSource(+0x28)` · `levelUpEffect1(+0x30)` · `levelUpEffect2(+0x38)` · `cameraShakePreset(+0x40)` · `upgradePanel(+0x48)` · `fadeBackground(+0x50)` · `sound(+0x58)`；Notifier 的 `levelUpController(+0x20)`。
- **`DoAnim`**（`.c:6-37`）：`SetActive(true)` → `myAnimation.Play()` → `impulseSource.GenerateImpulse()` → `fadeBackground.SetActive(true)` → `DOFade(fadeBackground, 0.7, 0.3)` → `SoundManager.Play2D(sound, MixerType 3)`。
- **`ChangeFrame`**（`.c:8-27`）：`cameraShakePreset.Play()` → `upgradePanel.ChangeFrameOnLevelUp()` → `DOFade(fadeBackground, 0, 0.7)` → `OnComplete(b__12_0 = 关背景)`。
- **`OnEnable` / `FadeOutImages` / `FadeOutImage`**：三张图「保留 RGB、α 置 0」，**无补间**。`.c` 里明写 `local_18._12_4_ = 0` + 前 12 字节原样 ⇒ **ILSpy 的 `image.color = (Color)(&obj)` 是 default 假象**。
- **字面量实读**：`DAT_1834b3188 = 0.7` · `DAT_1834b2dc8 = 0.3`。
- **实例计数**：Controller **1**（`Upgrade Panel`@`scenes_scenes_mainmenuwarpforge`）· Notifier **1**（`CardUI`）—— 主账「8 处 / 3 处」与 B8_AB §七 现数不符。
- **我们的落点**：`Shell/CardDetailPopup.DoUpgrade()`（`:2564`）—— **换框有、抖屏/淡出/2D cue/输入锁全无**。

### 2·3 `CardEffect` 判重四行（187/188/189/190）—— **已在引擎里实现**

| 原版 | 判据（`.c` 实读） | 我们的等价物 |
|---|---|---|
| `IsEnchantment` | `buffType(+0x38) ∈ {12,15,16}`（`.c:9-12`） | `Enchantments.IsEnchantmentBuffType`（`Enchantment.cs:163`），消费点 `Attach` 闸（`:208`） |
| `IsSameEffect` | `buffType` · `enchantingCard(+0x18)` **引用相等** · `heroCheck && cardType==10 && 同卡 ⇒ false` · `+0xa4/+0x30/+0x31/+0x32/+0x33/+0x34/+0x35/+0x36/+0x48/+0x4c/+0x50/+0x54/+0x60/+0x64/+0x68/+0x6c/+0x70` 逐字段（`.c:1-84`） | `EffectResolver.IsSameHandEffect`（`:5663`）：verb / `ActingUnit` 引用 / `Extrinsic` / 五档 until / 五组数值 / `targetCriteria` 值；**明确「不比 RecordId」** |
| `SameIsPlayerEffect` | `this.enchantingCard==null ⇒ true`；`other 的 ==null ⇒ true`；都有 ⇒ 比 `card+0x40`（`.c:1-37`） | 由 `ActingUnit` 引用相等覆盖（更严） |
| `SameTimingAsEffect` | 比 `+0x32/+0x33/+0x34/+0x35/+0x36`，每个「不等 ⇒ false」，且 0x33/0x34/0x35 三处夹「非同侧 ⇒ 要求至少一方该位为 0」（`.c:1-33`） | 由 `Expire`（枚举就是那五档）+ `ExpireTurn`（`Never` 不比）覆盖 |

> ⚠️ `CardDef.IsEnchantment`（`CardDef.cs:5990`，`Type=="enchantment"`）是**卡定义层**的另一件事，⛔ 别与 `CardEffect.IsEnchantment`（buffType 层）混。

### 2·4 `CardAnimController.RotateAnim`（175）—— **两支都已实现**

- **判据**（`.c:31-127`）：`pointTowardsOption(+0x20)==5` 时 → `GetMinionManager(actingCardIsPlayer ^ 1)` → `GetLeftMostPos/GetRightMostPos` → `transform.up = leftMost − self.position` → `angle = Atan2(dy,dx) × 57.2958 − 90` → `DORotate(ToEulerRad(AngleAxis(angle, **Vector3.forward**)) × Rad2Deg → MakePositive, timeMoving)` + `Linear` + `delay=timeAtStartPos`；否则 `transform.forward = 目标 − 自己`。
- **轴 = `Vector3.forward` 的反证链**：静态块 `+0x0c = Vector3.one`（`AnimFXModuleTransformModifier` 的 ILSpy 明确写出 `Vector3.oneVector`）⇒ 布局 `zero@0x0 / one@0xc / up@0x18 / down@0x24 / left@0x30 / right@0x3c / forward@0x48` ⇒ RotateAnim 读的 **`+0x48` = forward**。⚠️ 若照「up」实现会转错平面。
- **字面量**：`DAT_1834b2e98 = 57.2958` · `DAT_1834b2e04 = 90`。
- **我们的落点**：`BattleDriver.cs` 的 `ptIsBoardCenter` 分支 `:23085-23109`（端点用 `foeBoard.DropTargetWorld(0 / SlotCount−1)`，当场设 `Quaternion.Euler(0,0,angle−90)`）。数据侧现读 99 拍里 `pt==5` **0 条** ⇒ 今天不可达。

### 2·5 `CameraVerticalFramer` 三行（150/154/155）—— **已对齐**

- **`CalculateFraming` 全文 12 步**（`.c:1-265`）写在施工单里（含 `+0xb0 lastUsedZoomLevel` · `+0x48 enemyCardAreaSizeHelper` · `+0x50 playerCardAreaSizeHelper` · `+0x58 …Original` · `+0x60 playerHand` · `+0x68/+0x70` 两条 padding 曲线 · `+0x78 maxVerticalSizeInViewPort=7` · `+0x38 cameraSizeXTable` · `+0x40 viewShiftModifier` · `+0x98 originalSensorSize` · `+0xa0 originalLensShift` · `+0xa8/+0xac` 两端基准）。
- **结论**：`CombatAutoZoom.CalculateFraming` + `MeasureCardAreaGapWorld` 的 `try/finally`（**换 `originalSensorSize` 再量、量完还原**）已经覆盖了「换→量→还原」那三跳，并**证了 lensShift 在差分里抵消**。⇒ 主账「真缺口三处」**已过期**。
- **字面量**：`DAT_1834b2ba8 = 1e-10`（缓存为零的阈值）· `DAT_1834b2e08 = 100`（viewport→world 的 z）· `DAT_1834b2e60 = 0x7FFFFFFF`（abs 掩码）· `DAT_1834b2bb4 = 0.5`（中点系数）。

### 2·6 `ScenarioMaterialFader` 两行（362/367）

- `.c:1-120`（`doFade = !doFade` 在 **`:78` / `:110`**；`targetFade=1.0` 在 `:82`）：两分支末尾各一句 `*(bool*)(this+0x40) = !doFade`；`ctor` 只写 `0x2a/0x2b/0x2c` ⇒ **`doFade` 出厂 false**；`Update` 首句 `if(!doFade) return`（连 `currentBlendTime` 都不递减）。
- ⇒ 语义 = **奇数调用才真淡、偶数调用把淡取消**（`OnComplete` 永不触发）。
- 我们的 ①②③④ 已补（`ScenarioMaterialFader.cs:54-101`）；**只差这一句**（+ `Update` 首句闸）。

### 2·7 `UIBorderGlow` 五行（425/430/432/433/438）—— 组件与旁挂件都在，**缺 uv0.zw 的载体**

- `CalculateViewPosition`（`.c:1-57`）：懒取 `Camera.main` 缓存到 `+0x48`；无相机回落 **(0.5,0.5)**（**`.c:31-32`** 两句 `0x3f000000`）。
- `ModifyMesh`（`.c:1-96`，逐顶点循环 `:68-92`）：`if(!isActiveAndEnabled) return`；逐顶点 `PopulateUIVertex` → **只改 `uv0.z/uv0.w`** → `SetUIVertex`（**不重排顶点**；重映射在 PS）。
- `Update`（`.c:1-31`）：`transform.hasChanged ⇒ MyGraphic.SetVerticesDirty(); hasChanged=false`。
- **缺口 = 载体**：`ImageQuad.RebuildMesh` 写 `m.uv = Vector2[]`（`ImageQuad.cs:493-497`）⇒ 原件 shader 采到的 `uv0.zw` 恒 0；唯一挂载点 `Shell/ForgeTab.cs:1043` **没接 writer**。⚠️ 主账「0 挂载点」已过期（现读 1 个）。

### 2·8 `UIEffectTogglerImage` 四行（462–465）

- `Toggle`：`targetStateEnabled=option; ToggleGameObjects(duration>0 ? true : option)`。
- `DoUpdate`（`.c:1-49`）：`to = targetStateEnabled ? 1 : 0`、`from = 1−to`；`n=Clamp01`；逐 Image **`a = (to−from)×n + from`（绝对值）**，RGB 保留。
- `AnimationFinished`：`deactivateOnDisable && !targetStateEnabled ⇒ ToggleGameObjects(false)`。
- `ToggleGameObjects`：逐个 `SetActive(option)`。
- **我们的偏差**：组件逐句对齐，但 ①0 挂载点；②`DoUpdate` 的 α 是**乘性**（`base.a × fade`）而原版是**绝对值**。

### 2·9 `BattleDriver` 侧五条（403/505/507/508/510）

| 行 | 判据要点 |
|---|---|
| 403 `SwarmVFXController.Initialize` | `ConfigureMaterial(mat, card)` → `CardUI.ToggleBody3D(false, 空 nullable)` → 存 `onFinish` → `Play3D(cue, transform.position)` → `StartCoroutine(SafeExit)`。缺 ②③。 |
| 505 `ApplyMovementDisappearAnimRotations` | `useCurve(+0x10)==0 ⇒ identity`；否则 `**LerpUnclamped**(originalRotation, AngleAxis(angle(+0x20), axis), curve(+0x18).Evaluate(t/animTime))`。我们写的是 `SlerpUnclamped`。 |
| 507 `CardToMinionAnim` | `CardUI.PlayInvokeAnimation()` → `rarity = HasCurrentTrait(ambush=1200) ? **1** : cardRarity` → `GetInvokeCue(rarity)` → `Play2D(cue)`。 |
| 508 `DissolveWholeCard` | `.c:20-33`（`one` 在 `:23` · `spellPlayedScale` 在 `:28` · `DOScale` 在 `:32`）：`cardCollider2D.Toggle(cardType==10)` → `DOScale(transform, **Vector3.one × globalVars.spellPlayedScale**, timeToDissolve)` → `StopShowingCodex()` → `StopReflected()` → `card2DController.ToggleHighlight(false,false,false)` → `FadeCardItems(0, timeToDissolve)`。我们前两句已做，后三句没做。 |
| 510 `ForceDisableCanActAnim` | `CardUI.canAttackAnim(+0x140).SetActive(false)` —— **0 调用点 + 0 序列化引用**；且我们的「可攻击浮标」本来就有（`CardView._canAct`）⇒ 建议 D。 |

> `globalVars.spellPlayedScale` 的偏移推定：`ScriptableObject` 首字段在 **`+0x18`**（`UnityEngine.Object` 多一个 `m_CachedPtr` 8 字节）⇒ `+0x40 = spellPlayedScale`。与 `BattleDriver.cs:13768` 既有的注释（「+0x40 = spellPlayedScale = 1.0」「+0x44 = 1.1 已知锚点」）互相印证。

### 2·10 其余三条

| 行 | 判据要点 | 我们 |
|---|---|---|
| 130 `DoSimpleShake` | `p = instancedDefaultPreset.preset`；**只覆盖 `p.amplitude = strength`**；`DoShake(...)`。唯一调用点 `RemoveDropPod`：`DoSimpleShake(isDestroyed ? 3.0 : 0.5)`（`DAT_1834b2e8c`=3.0 · `DAT_1834b2bb4`=0.5）。 | 组件 0 行改动；缺 4 跳接线（引擎 3 处 emit + 表现 1 处消费） |
| 208 `GetEnvCardListDraft` | `victories<3 && 进攻 ⇒ 空表`；防御恒给 [0]、`>5` [1]、`>8` [2]；进攻 `>2` [0]、`>5` [1]、`>8` [2] | 轮抽线**完全没有**这一层（`DraftModeWindow`/`DraftPacks` 0 处 env 引用） |
| 455 `UIEffectTogglerCoffeeEffects.Toggle` | `targetStateEnabled=option`；逐 `uiEffects` `enabled=option`；`option && UIEffectTweener ⇒ Play(resetTime:true)` | 第三方包未导入（dll 在 `d:/2/tools/cpp2il/out_cpp2il_dev/Coffee.UIEffect.dll`，329,216 B，有方法体）；shader 仅 1 只 `Hidden/UI/Default (UIEffect)`（`globalgamemanagers.assets` path_id 77） |

---

## ③ 没查清（如实记，⛔ 别当成已对齐）

1. **`PlayerHand.CurrentSizeMultiplier`（`PlayerHand+0x3c`）的写点没查到** ⇒ `smallScreenUI` 那一档的取景（150/154/155）**没复刻**。还差：`PlayerHand__get_CurrentSizeMultiplier.c` 的两分支知道了（`GameStaticData.smallScreenUI(+0x11c)` 假 ⇒ 常量 1.0；真 ⇒ `+0x3c`），但**谁写 `+0x3c`、写多少**没有 ↑。读法：VA 级反汇编扫该偏移的写点。
2. **`Level Up Effect` / `Level Up Effect Down` 的 shader 名没解**：两件共用外部材质（`m_FileID 5 / m_PathID 4`）⇒ 要解 `scenes_scenes_mainmenuwarpforge` 的 externals 第 5 项。后果：J-02 只能验「浮点写对」，验不到「画对」。
3. **`UIEffectTogglerImage` 两处 `Toggle(option, duration)` 的实参与触发时机没读方法体**（B8_AS §3·2 末已记，本轮未推进）⇒ J-03 的 `duration` 实参由接线方再核。
4. **`Coffee.UIEffect` 7 个实例的逐实例 `uiEffects[]` 指向与触发时机未全读**。
5. **`BattleCardUI.PlayInvokeAnimation` 播的 clip 字段没读**（`.c:30-40` 只写了 `Play(clip)` + `speed=1.0`）⇒ 507「是否复用 `CardFeel.HandToBoard` 那支 clip」未定。
6. **508 的 `Vector3.one` 判据是「静态块偏移 + 类身份」两条反证**（`+0x0c` 由 ILSpy `Vector3.oneVector` 反证、`+0x48 = forward` 由布局推）—— 不是逐字节字面量直读 ⇒ 施工单里标为「判据充分但非字面量直读」。
7. **`AnimateUIMaterialProperty` 空槽的逐字节 `Equals(default)` 等价未证**（G12 已记；6 个出厂实例可见结果相同）。
8. **本代理不跑 Unity** ⇒ 施工单里的「怎么验」全部是**可执行步骤**，不是「已跑过」；`AnimFXCheck` / `BlendProbe` / 宿主断言都留给调度台。

---

## ④ 改动清单（本代理动过的文件）

| 文件 | 状态 |
|---|---|
| `资料/完全复刻/判据/B9_特效C_施工单.tsv` | **新增**（12 列 × 40 行 + 表头；LF / 无 BOM；表头逐字照 `判据/B9_剩余C_施工单.tsv`；已脚本核对 40 行与主账 `状态==C` 逐条一一对应） |
| `资料/完全复刻/判据/B9_特效C_派活包.md` | **新增**（11 个可派包 + 不派活/待读 · 文件所有权表 · 波次 · 合法读数口白名单 · P3/远端候选） |
| `资料/完全复刻/报告/B9_J4FX_特效C施工单.md` | 本文件 |
| `_tmp_view/j4fx/gen_tsv.py` | 临时生成脚本（可复跑；数据全在脚本里） |

**没动**：任何 `MyGame/Assets/**`（`.cs` / `.asset` / `.prefab` / `.anim`）· `账/*.tsv` 主表 · `账/_parts/**` · 任何正本（`CLAUDE.md` / `项目任务.md` / `资料/ANIMFX_实现与接线.md` / `资料/特效还原_进度与交接.md`）· git。

---

## ⑤ 🔴 顺手发现（只报不改；全部有出处）

### 5·1 主账 8 处「差异/判据要点」已被现读推翻（⛔ 别再照着开工）

| 账行 | 主账说法 | 现读结论 | 证据 |
|---|---|---|---|
| **86** | 「`CreateInstanceMaterial()` 后把 **changeMaterialOnAnimation** 存到 +0x78」 | +0x78 = **`instancedMaterial`**（+0x70 才是 `changeMaterialOnAnimation`） | ILSpy 字段序 `:20-40` + `OnEnable` 把 +0x78 赋给 `image.material` |
| **175** | 「真缺口：`boardCenter` 那一支**没找到对应实现**」 | **已实现**（含正确的 `Vector3.forward` 轴与 −90°） | `BattleDriver.cs:23085-23109`（2026-10-19 `W` · 偏差 6） |
| **177/184** | 「原版 **8 处 / 3 处**实例」 | Controller **1** / Notifier **1** | B8_AB §七 全 84 bundle 现数 |
| **187** | 「15/16 两个 buffType 没有对应判定」 | `Enchantments.IsEnchantmentBuffType` 已覆盖 `{12,15,16}` | `Enchantment.cs:163` + `:208` |
| **188/189/190** | 「我们按记录身份判 ⇒ 同一载荷挂两次会叠两层」「没有『两条是否同时机』的判定」 | `EffectResolver.IsSameHandEffect` 逐字段比、**明确不比 RecordId** | `EffectResolver.cs:5663` + `AttachHandEffectCopy :5755` |
| **425/430/432/433/438** | 「仍 C = 组件 **0 挂载点**」 | 现读 **1** 个（`Shell/ForgeTab.cs:1043` 的 `Collect Highlight Effect`） | `grep AddComponent<UIBorderGlow>` |
| **507** | 「按稀有度（**ambush 强制 4**）取 invokeCUE」 | **`? 1 : cardRarity`** | ILSpy `:8394` + `.c:15-20` 双读 |
| **508** | 「`DOScale(0)`」 | `DOScale(`**`Vector3.one × spellPlayedScale`**`, time)`（实例 1.0 ⇒ 缩到 1，**不是缩到 0**） | `.c:20-33`（静态块 +0xc=one 在 `:23`）+ `VarsGlobal` 首字段 +0x18 ⇒ +0x40=spellPlayedScale；与 `BattleDriver.cs:13768` 既有注释一致 |
| **510** | 「缺：**无该浮标**」 | 浮标**有**（`CardView._canAct` / `SetCanAct` / `CanActVisible`），缺的只是「强制关」入口；且该方法 **0 调用点 + 0 序列化引用** | `grep canAttackAnim` 只命中注释；`decomp_full`/ILSpy 只有定义；`assets_full` 全树 `grep -rl` **rc=1（0 命中，18 s 扫完）**，正控 `CardHandToBoardAnimationFinished` 命中 ⇒ 搜法有效 |

> ⚠️ 本代理**不改主表**。建议调度台按补丁流程把这 9 行的「差异/判据要点」订正（每条带上面的出处），并把 175/187/188/189/190 建议改判 **B**、510 建议改判 **D**。

### 5·2 方法论级（可复用，建议进 `资料/工作准则_操作经验.md`）

1. 🔴 **`UnityEngine.Vector3` 的静态块布局可以反推，而且必须反推**：`AnimFXModuleTransformModifier` 的 ILSpy 明写 `Vector3.oneVector` 且那条路径的 `.c` 读 **`+0xc`** ⇒ `zero@0x0 / one@0xc / up@0x18 / down@0x24 / left@0x30 / right@0x3c / forward@0x48 / back@0x54`。**代号 `DAT_xxx+0xb8` + 偏移**这种读法在 VFX 侧到处出现（本域至少 5 处），**不反推就会把 `forward` 当 `up`**（`RotateAnim` 就会转错平面）。
2. 🔴 **`ScriptableObject` 派生类的首字段在 `+0x18` 不是 `+0x10`**（`UnityEngine.Object` 多一个 `m_CachedPtr`）⇒ `VarsGlobal+0x40 = spellPlayedScale`（按 +0x10 推会错位到 `enemyCardDisplayScale`）。判据：用一条**已知锚点**（本例 `+0x44 = 1.1 = cardInHandMovingScale`）反证。
3. 🔴 **`decomp_full` 里「首行名不是自己」有两种，必须先分清**：`CardDisplayLevelUpAnimationController__FadeOutImages.c` 首行是 `OnEnable` —— 但 ILSpy 两方法体**逐字相同** ⇒ **同码折叠（ICF）**，**不是**内容错位。判法：**先读体再下结论**（与 CLAUDE.md 的「ICF 良性反例」同族）。
4. 🔴 **ILSpy 的「一排 `default` / `(Color)(&obj)`」= 实参常量丢失**，本例 `FadeOutImage`/`FadeOutImages`/`OnEnable` 三处的 `image.color` 都被读成 `default` ⇒ 回 `.c` 一看是「保留 RGB、只清 α」。**本轮 3 处都靠这一条救回来**。

### 5·3 代码侧（只报不改）

1. 🔴 **`UIEffectTogglerImage.DoUpdate` 的 α 是乘性**（`base.a × fade`），原版是**绝对值覆盖** ⇒ 基线 α≠1 的件会偏暗。修法见 J-03（1 行）。出处：`CardPresentation/Effects/UIEffectTogglerImage.cs` 的 `DoUpdate` vs `decomp_full/UIEffectTogglerImage__DoUpdate.c:43`。
2. 🟡 **`BattleDriver.MinionAxisRot` 用 `SlerpUnclamped`，原版 `LerpUnclamped`**（505）；它的注释写「必须 SlerpUnclamped」——真实要求是「必须 **Unclamped**」，`Slerp`/`Lerp` 的钳制版才会抹掉曲线中键 −0.952。
3. 🟡 **`Shell/ForgeTab.cs:1043` 挂了 `UIBorderGlow` 但没接 `UIViewportUvWriter`** ⇒ 那颗扫光的圆心恒在 (0,0)（shader 读 uv0.zw）。这是 J-04 的入口。
4. 🟡 **`Shell/BoosterPackOpenWindow.cs` 文件头那句「原版 shader 的逻辑本地没有 ⇒ 我们没有等价物可依」是错的**（B8_AS §⑤·1 已记：`Shader Graphs/Nebula` 就在本地）—— 本轮确认它仍是**注释态**、未改；J-01 接线时应顺手订正（铁律 5）。
5. 🟡 **`CardFeel.cs` 的 `PlayInvokeAnimation` 注释已把判据读全**（`.c:30-40`：`Animation.Play(clip)` + `speed=1.0` + `ToggleBody3D(true)`）—— 507 的施工可以照它，⛔ 不需要再读一遍。
6. 🟡 **`OffensiveCards.ChoicesOwnedAt` 用的是「锻造等级」语义**（`need <= level`），那是 `GetEnvCardList` 的；`GetEnvCardListDraft` 用的是「胜场」⇒ ⛔ **别复用同一个函数**（208 里已写死这条禁令）。
7. 🟡 **`Swarm Card Merge VFX` 这条链的 `ToggleBody3D(false)` 缺口**（403）与 A31 §六·③⑤ 是**同一条账**，别开第二份。
8. 🟡 **`ScenarioMaterialFader.cs` 文件头与主账 362 的备注都把 `doFade = !doFade` 的行号写成 `.c:96 / :124`** —— 实际是 **`.c:78` / `.c:110`**（该文件只有 120 行，`:124` 不可能存在）。改 `doFade` 的人会照着错行号去找 ⇒ 建议订正那两处引用（本代理不改 `.cs`、不改主账）。

---

## ⑥ 给调度台的请求（三件）

1. **落账**：本代理**没有**写 `账/_parts/` 补丁（只读纪律）。建议按 §5·1 那 9 行 + §1·1 的改判建议（175/187/188/189/190 → B、510 → D）由调度台出补丁；每条证据已写在上表。⚠️ 合并前跑 `python -I -X utf8 工具/check_patch_conflicts.py`（多代理同批在飞）。
2. **一次 Unity 批处理（只有调度台能跑）**：J-01/J-02 要的 **3 条 clip**（`Rank Up` / `Division Up` / `Level Up Card`）+ `AnimFXCheck.Run` / `BlendProbe`（J-04 的 uv0.zw 读数、J-02 的浮点读数）。⛔ **不要**跑 `EffectCompare`（用户口径禁截图并排比）。
3. **一次读数（362/367 的前置）**：换一张进攻卡（环境卡），读每件 blendable 的 `ScenarioMaterialFader.BlendCalls` 增量 —— **同一件若正好 2 次**，说明我们的调用次数与原版不同，J-08 要先修调用方（`EnvironmentApplier`）再抄 `doFade`。

---

## ⑦ 追加：账补丁 `_parts/B9_J4FX.tsv`（调度台 2026-10-26 授权写 `_parts`）

> 授权范围：**只写 `账/_parts/`**；⛔ 未改主表、未合并、未跑 Unity。生成脚本 = `_tmp_view/j4fx/gen_patch.py`（可复跑）。

**规模**：**21 行 = 1 行表头 + 20 行**（11 列 · LF · 无 BOM · 表头逐字照主表；`方法` 列全部是**完整签名带 `(...)`**；20 个 `(原版类, 方法)` 键**互不重复**）。

| 处置 | 行数 | 账行 |
|---|---|---|
| 订正（状态保持 C，仅改 `差异`/`备注`） | 12 | 86 · 177 · 184 · 362 · 425 · 430 · 432 · 433 · 438 · 507 · 508（+ 175 与改判 B 同行合并） |
| 改判 **B**（`等级` 已清空） | 8 | 150 · 154 · 155 · 175 · 187 · 188 · 189 · 190 |
| 改判 **D**（时间限定 = 本 build） | 1 | 510 |
| **不动** | — | 498（判据待读，保持 C） |

**只改了允许的列**（已做列级 diff：原表 vs 合并后副本，523 行不变）：`状态` / `差异` / `备注` / （8 条 B 行的）`等级`；**`判据出处` / `判据要点` / `我们的落点` / `轮次` 一个字节没动**。
⚠️ 因此「`判据要点` / `判据出处` 里的旧错误」（86 的 `+0x78`、507 的「ambush 强制 4」、508 的 `DOScale(0)`、362 的 `.c:70-135`、425 的 `.c:44-45`、430 的 `.c:1-110`）**写进了 `备注` 并注明「建议下轮改判据要点/判据出处」**，本补丁**未直接改那两列**（按授权约束）。

### 三项自检（全部实跑）

| # | 检查 | 结果 |
|---|---|---|
| 1 | **每个键在主表唯一命中**（按 `merge_ledger_parts.py` 的定位规则，扫 `账/*.tsv` 全部 21 份） | ✅ **20/20 都是「恰好 1 处命中」**（0 处、>1 处都是 0） |
| 2 | **`verify_annot.py` 的「缺注解」合并前后** | ⚠️ **基线 = 1（不是 0）**，合并后副本 = **1（不变）**；唯一那条 **不在本补丁范围**：`账/战斗逻辑驱动.tsv:780 CardScript.HasWhileInPlayAdjacentEffect()` 的落点写 `RuleEngine/Core/UnitState.cs:2429`，而该方法现在在 **`:2465` / `:2478`**（+36 行漂移，别的代理的 `.cs` 改动所致）。⇒ 建议同步点跑 `check_ledger_points.py --all-A --fix`（⛔ 需在「无 `.cs` 写手 + 补丁已合并」时）。 |
| 3 | **`check_patch_conflicts.py`** | ✅「没有跨补丁重复键（4 份补丁）」—— 4 份 = `B9_E11RC` / `B9_J4FX` / `B9_W9K01b` / `B9_W9K01c`（已确认本补丁被计入）。⚠️ 加 `--all` 时另有 5 条冲突，全部是 `_merged_b3/*`（已归档）与 `B9_W9K01c` 之间的，**与本补丁无关**。 |

**模拟合并（在主表副本上跑，未碰主表）**：`_tmp_view/j4fx/sim/`（21 份主表副本 + 本补丁）
`merge_ledger_parts.py --dry --parts sim/parts --ledger sim/ledger` ⇒ **更新 20 · 新增 0 · 无变化 0**；
真合并到副本 ⇒ 同数字，`✅ 写盘 特效动画.tsv（行尾 LF）`；副本上跑 `verify_annot` 逻辑 ⇒ 缺注解 **1（不变）**。

**给调度台的一句**：合并时**必须带 `--only B9_J4FX`**（`_parts/` 里还有 `B9_E11RC` / `B9_W9K01b` / `B9_W9K01c` 三份在飞）。
