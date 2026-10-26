# B9 · 特效动画域 40 条 C 行 · 派活包（按目标文件切片）

> 判据代理 **J4FX**（第八十四会话「完全复刻」）· 2026-10-26 · **只读核**
> ⛔ 本轮**未改**任何 `.cs` / 资产 / 主账表 / 正本；⛔ 未跑 Unity（含 `AnimFXCheck.Run` / `BlendProbe` / `EffectCompare`）；⛔ 未动 git。
> 配套：逐条施工单 = `判据/B9_特效C_施工单.tsv`（12 列 × 40 行）· 总表与判读 = `报告/B9_J4FX_特效C施工单.md`
> 判据输入（未重做，只补证据）：`判据/B9_特效触发链.tsv` · `判据/B9_特效资源覆盖.tsv` · `判据/B9_特效参数对账.tsv` · `判据/B9_shader近似映射_对账.tsv` · `报告/B8_AS_特效侧施工单.md` · `报告/B8_AB_特效动画C档.md` · `资料/ANIMFX_实现与接线.md`

---

## §0 用法（四句话）

1. **一个包 = 一个写手独占的一组文件**；包内按「建议等级 P1 → P2 → 不派活」顺序做，做完一条就在施工单里勾掉。
2. 🔴 **同一时刻一个文件只能一个写手**。本轮 40 条里有 **6 条共用件/瓶颈**（见 §5），跨包撞车一律**串行**，别并行。
3. 每包做完先跑 **`bash 工具/typecheck.sh`**（改过 `.cs` 就必须跑，几秒级），再按「本包验收口」读数；⛔ 写手**不跑 Unity**、⛔ **不跑全量自检**（主对话在同步点统一跑）。
4. ⛔ **禁用的读数口**：`EffectCompare`（原版 vs 我们**并排渲图**）、任何截图并排比、任何像素结论。用户 2026-10-26 口径：**特效「像不像」由用户自己看**，我们只做代码层（属性表 / pass 状态 / 字节码 / 序列化字段 / 反编译方法体）。本文件的「怎么验」全部是可执行**读数**或**断言**。

### 0·1 「现等级 / 建议等级」两列的口径

- **现等级** = 主账 `账/特效动画.tsv` 的 `等级` 列**现值**（`P1` / `P2` / **空 = 写「未定级」**）。它是**优先级**，不是 A/B/C/D 状态。
- **建议等级** = 本代理建议的**优先级**（P0–P3），外加一个特殊值 **`D`** = 「判据显示**原版本身不触发**，建议改判 D（待调度台裁）」。
- 🔴 **状态（A/B/C/D）的改判建议**写在施工单的「施工步骤」列首的方括号标签里：
  `【可派】` · `【可派·前置】` · `【已对齐·0 行…】` · `【判据待读】` · `【建议改判 D（待调度台裁）】`。
  ⚠️ 本代理**不改主表**；所有 `C→B` / `C→D` 的建议要由调度台按补丁流程落账。

### 0·2 本轮最重要的结论（先看这个：四类）

| 类 | 条数 | 账行 |
|---|---|---|
| **要改代码 / 资产**（可派 / 可派·前置） | **25** | 84·87（J-01）· 130（J-09）· 177·178·179·180·181·182·184·185（J-02）· 208（J-11）· 362·367（J-08）· 403·425·430·505·507·508（J-04/J-05）· 455（J-12）· 462·463·464·465（J-03） |
| **同包 0 行**（语义依据 / 等价物已在，随包落地即完成） | **5** | 86·88（J-01，`SetMaterial` / 结构体那一跳只作语义依据）· 432·433·438（J-04，三个回调的等价物已在，只等载体 uv0.zw） |
| **已对齐（建议改判 B，只补断言/注释）** | **8** | 150·154·155（J-07）· 175（J-05 内 0 行）· 187·188·189·190（J-10） |
| **不派活** | **2** | 498（判据待读）· 510（0 调用点 + 0 序列化引用 ⇒ 建议 **D**） |

> 合计 **25 + 5 + 8 + 2 = 40**（与主账 `状态==C` 的 40 条一一对应，已脚本核对 missing 0 / extra 0 / 顺序一致）。

---

## §1 包总表

| 包名 | 目标文件（写手独占） | 条数 | 账行 | 前置依赖 | 本包验收口（摘要） |
|---|---|---|---|---|---|
| **J-01** | `Shell/BoosterPackOpenWindow.cs` + `Shell/BoosterPackAnim.cs` + `Shell/RankedDivisionChangeWindow.cs` | 4 | 84·86·87·88 | **J-06**（3 条 clip 由调度台导入） | `InstancedMaterial.GetFloat(...)` 逐关键帧 == 原版曲线 |
| **J-02** | **新建** `Shell/CardLevelUpAnim.cs` + `Shell/CardDetailPopup.cs` | 8 | 177–182·184·185 | **J-06**（3 条 clip）+ `WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs`（只调用、不改） | 输入锁「锁过」+ 背景 α 1→0 且收尾关物体 + 三图 α 归零、RGB 不变 |
| **J-03** | `Deck/DeckRuntime.cs` + `Shell/DraftModeWindow.cs`（`BuildPackCard` 那一段） | 4 | 462·463·464·465 | 无 | `CurrentFade` 0↔1 + `activeSelf` 收尾正确 + **α 绝对值**（非乘性） |
| **J-04** | `Battle/ImageQuad.cs` + `Shell/ForgeTab.cs` + `WarpforgeVFX/Runtime/UIViewportUvWriter.cs` | 5 | 425·430·432·433·438 | 无（但与 J-08 共享 `ImageQuad`） | `mesh.GetUVs(0)` 的 `zw == WorldToViewportPoint(pos).xy` |
| **J-05** | `Battle/BattleDriver.cs` | 7 | 175·403·505·507·508·498·510 | **J-09**（130 的 `BattleDriver` 消费一行）与 J-05 **同文件 ⇒ 串行** | 逐条断言（见下）+ `ruleprobe play check` 指纹不变 |
| **J-07** | `Battle/CombatAutoZoom.cs` + `Battle/BattleCameraSreenSize.cs` | 3 | 150·154·155 | 无 | 「已对齐」只补注释（0 行数变化）+ 现有 A421/A444 断言 |
| **J-08** | `Battle/ScenarioMaterialFader.cs` | 2 | 362·367 | 无 | `DoFade` 翻转 + `Advance` 不推进（灭自证） |
| **J-09** | `RuleEngine/Core/EffectResolver.cs` + `RuleEngine/Core/RuleCore.cs`（+ `BattleDriver` 1 行消费） | 1 | 130 | 无；但**必须与 J-05 串行**（共用 `BattleDriver.cs`） | `play check` 逐字节不变 + `TraitRemoved` 两跳各一次 |
| **J-10** | `RuleEngine/Core/Enchantment.cs` + `RuleEngine/Core/EffectResolver.cs`（只加断言/注释） | 4 | 187·188·189·190 | 无；与 **J-09** 共用 `EffectResolver.cs` ⇒ 串行 | 「同一载荷挂两次 ⇒ 1 条」等四条单测 |
| **J-11** | `Core/OffensiveCards.cs` + `Shell/DraftModeWindow.cs` | 1 | 208 | 轮抽线整体（本层未建） | 五档胜场 × 两侧的可用卡数 |
| **J-12** | **新建** `Assets/ThirdParty/CoffeeUIEffect/**` + `Effects/UIEffectTogglerCoffeeEffects.cs` + 7 处 Shell 接线 | 1 | 455 | 无（成本最大，可最后做） | `enabled` 开关 + `UIEffectTweener.PlayCount` |
| **不派活** | — | 0 条新代码 | 510（建议 D）· 498（判据待读） | — | — |

**合计**：11 个可派包 + 1 个「不派活/待读」= 40 条。

---

## §2 各包明细

### J-01 · `AnimateUIMaterialProperty` 接线（84/86/87/88）· 4 条
- **组件本体 0 行改动**：`WarpforgeVFX/Runtime/AnimateUIMaterialProperty.cs` 已逐句复刻 8 个方法（含 `OnDisable`/`ResetToOriginalMaterial`/`OnDestroy`），API = `Attach(renderer, Slot(name,value), SlotV4(...))`。
- **序**：① 87 的 3 条 clip（调度台跑 Unity 导入；生成口径见施工单 87）→ ② `BoosterPackOpenWindow` 的 `Booster pack Background`（两个 float 槽）→ ③ `RankedDivisionChangeWindow` 的 `Division Image` / `Rank Icon`（一个 `_Blend` 槽）。
- ⛔ **不要**给 `CardFrame` 再挂一份（原版那件 `m_Enabled=0`；`CardView`/`CardFeel` 已硬编码驱动 `_DissolveAmount`，接了会写两遍）。
- **验收口**：`comp.InstancedMaterial.GetFloat("_Distort_Scale")` / `GetFloat("_Blend")` 逐关键帧 == `_tmp_view/b8ab/animate_ui_clips.json`；`InstancedMaterial.name` 以 `(Instanced)` 结尾。
- **风险**：低（纯接线）。⚠️ 三条 clip 里**只有 `floatProperty1/2` 那几条在本工程能落地**，绑 uGUI `m_Color.a` 的曲线会静默不生效 —— 这不属本包能修的（要 Shell 另做）。

### J-02 · 升级庆祝动画整条（177–182 / 184 / 185）· 8 条
- **落点**：新建 `CardPresentation/Shell/CardLevelUpAnim.cs`（一个 MonoBehaviour 覆盖原版 8 个方法 + 输入锁），挂在 `CardDetailPopup.DoUpgrade()` 的成功分支（`CardProgress.SetLevel` 之后、`RebuildKeepingState()` 之前或之后由接线方按可见顺序定）。
- **序**：① 182 `OnEnable` 三图 α 归零 → ② 179 `DoAnim`（开物体 / 播补间 / 抖 / 背景淡到 0.7 / 2D cue）→ ③ 181 `FadeOutImages` + 180 `FadeOutImage`（三件、无补间）→ ④ 178 `ChangeFrame`（抖 / 换框 / 背景 `DOFade(0,0.7)` → 关物体）→ ⑤ 177/184 `AnimationFinished`（解输入锁）→ ⑥ 185 只是 184 的转发入口。
- **原版真值（照做）**：`DoAnim` = `SetActive(true)` → `myAnimation.Play()` → `impulseSource.GenerateImpulse()` → 背景 `SetActive(true)` → `DOFade(0.7, **0.3**)` → `Play2D(cue)`；`ChangeFrame` = `cameraShakePreset.Play()` → `ChangeFrameOnLevelUp()` → `DOFade(**0**, **0.7**)` → OnComplete 关背景。字面量：`0.7`=`DAT_1834b3188` · `0.3`=`DAT_1834b2dc8`（`工具/read_literal.py` 实读）。
- **等价物（🔶 必须标）**：① 菜单层**没有相机** ⇒ `GenerateImpulse` / `cameraShakePreset.Play()` 的等价物 = **窗容器抖**（先例 `Shell/BoosterPackAnim.cs:309-332`，照它、⛔ 别新造）；② `myAnimation.Play()` 的等价物 = 我们自己补间驱动 `AnimateUIMaterialProperty` 的 `_Reveal` 两槽（J-01 的组件）；③ 原版实例计数 = **Controller 1 个（`Upgrade Panel`）+ Notifier 1 个（`CardUI`）** —— 主账「8 处 / 3 处」与现数不符（B8_AB §七 现数）。
- **验收口**：① **灭自证**：`DoAnim` 期间 `InputBlocked==true`（断「锁过」），`AnimationFinished` 后 false；② 背景 α 0.3 s 内到 0.7、收尾 `activeSelf==false`；③ 三图 α 全 0 且 **RGB 逐值不变**；④ `_Reveal` 走 −0.5→1，`_Noise_Strength_X_Speed_Y_Scale_Z == (0.02,−0.1,8.17,0)`。
- **风险**：中高。`CardDetailPopup.cs` 是 Shell 大件 ⇒ **新成员一律追加类尾**（插中间会让账里 A 落点全漂）；3 条 clip 只有调度台能导；`Level Up Effect`/`…Down` 的 shader 名未解 ⇒ 只能验「浮点写对」验不到「画对」。

### J-03 · `UIEffectTogglerImage` 接线（462–465）· 4 条
- **两处接线（原版 2 个实例，B8_AS 现读）**：
  | 实例 | 我们的位置 | 写法 |
  |---|---|---|
  | `Done Highlight`（卡组编辑） | `Deck/DeckRuntime.cs` 的 `SetOn(_doneHl, err==DeckError.None)`（原记 `:4869`） | 同节点自动收集 ⇒ `AddComponent<UIEffectTogglerImage>()` + `UIEffectTogglerController.Toggle(on)` |
  | `Prebuilt Pack UI`（轮抽） | `Shell/DraftModeWindow.cs` 的 `BuildPackCard` 里 `MenuDraw.Nine(… "Highlight" …)`（原记 `:2413`） | **`fadeTargets.Add(highlightNode)`**（原版 `images[0]` 是子件，不是同节点）+ `Toggle(on)` |
- **本包唯一语义修正（463）**：`DoUpdate` 里 α 现在是 `base.a × fade`（**乘性**），原版是 **`a = from + (to−from)×Clamp01(n)`（绝对值覆盖）** ⇒ 基线 α≠1 时不同。改成原版口径。
- **还差（判据没读）**：两处 `Toggle(option, duration)` 的**实参与触发时机**没读方法体 ⇒ 接线方按 `PrebuiltPackUI.OpenAnim/SelectAnim/CloseAnim` 与 `UIEffectTogglerController.Update` 再核一次。
- **验收口**：基线 α=0.5 的图在 `n=1,to=1` 时 α==**1.0**（乘性写法给 0.5 ⇒ 这条就是灭自证）；`Toggle(false,0)` 立刻关；`Toggle(false,0.3)` 先开后关。
- **风险**：中。`DraftModeWindow.cs` 与 J-11 同文件 ⇒ 串行。

### J-04 · `ImageQuad` 的 uv0.zw 口 + `UIBorderGlow` 挂载（425/430/432/433/438）· 5 条
- 🔴 **真实缺口 = 载体，不是组件**：`UIBorderGlow` 本体与 `UIViewportUvWriter`（`CalculateViewPosition` + 逐顶点写 uv0.zw + 变化重写）**都已逐句等价**；缺的是
  ① `ImageQuad.RebuildMesh` 写的是 **2 分量** `m.uv = Vector2[]`（`CardPresentation/Battle/ImageQuad.cs:493-497`）⇒ 原件 shader 采到的 `uv0.zw` 恒 0 ⇒ 扫光圆心恒在 (0,0)；
  ② 唯一挂载点 `Shell/ForgeTab.cs:1043` 只 `AddComponent<UIBorderGlow>()`、没接 writer。
  ⚠️ 主账「0 挂载点」**已过期**（现读有 1 个）。
- **做法（二选一，⛔ 别两处都写）**：
  - **A（推荐）** `ImageQuad.RebuildMesh` 改 `m.SetUVs(0, Vector4[])`（xy 语义不变、zw=0）+ 新增 `public void SetUv2(Vector2 zw)`（**谁写谁负责**，⛔ 不要在 ImageQuad 里算视口坐标）；ForgeTab 每帧写 zw。
  - **B（旁挂）** `glowNode.AddComponent<UIViewportUvWriter>()`（`cam` 留空 ⇒ `Camera.main`）。
  ⇒ **A 是前置**：只做 B，writer 写完会被 `RebuildMesh` 的 2 分量覆盖。A 落地后把 B 删掉或定为唯一写手，并写进注释。
- **验收口**：`mesh.GetUVs(0, list)` 的 `zw == Camera.main.WorldToViewportPoint(quad.transform.position).xy`（无相机 == (0.5,0.5)）；`material.GetFloat("_Reveal")` −0.5→1。
- **风险**：🔴 **高**。`ImageQuad` 是**全工程 UI 总件**；`m.uv`(Vector2[]) 与 `SetUVs(0,Vector4[])` **不能混用**，写错会让所有 UI 贴图错位 ⇒ 必须逐档验 6 个 Shell 宿主 + Battle。

### J-05 · `BattleDriver.cs` 的战斗表现 7 条（175/403/505/507/508/498/510）
- **按建议顺序**：① 505（1 行：`SlerpUnclamped`→`LerpUnclamped`）→ ② 508（补 3 句：`StopShowingCodex`/`StopReflected`/`ToggleHighlight(false)`）→ ③ 403（补 `SetBody3DVisible(false)` + 核材质交接）→ ④ 507（`autoPlayFromHand` 那条路补 `PlayInvokeAnimation` + `ToggleBody3D(true)`；**先判是否复用 `CardFeel.HandToBoard`**）→ ⑤ 175（0 行，只补守护断言）→ ⑥ 498（**判据待读**，别动手）→ ⑦ 510（**建议改判 D**，别动手）。
- **验收口**：见施工单逐条；共同的灭自证口径 = **断「触发过」的计数，不断终态**（例：`Body3DVisible` 合并期 false 且**有** `HideBodyCount+1`）。
- **风险**：中。`BattleDriver.cs` 34,392 行、全项目第二大瓶颈；与 J-09 共用 ⇒ **必须同一写手串行**；`CardView.cs` 若需 getter，只加只读口（共用件）。

### J-07 · 取景三条（150/154/155）· **已对齐，0 行行为改动**
- 现状（现读）：`CombatAutoZoom.CalculateFraming` + `MeasureCardAreaGapWorld`（`try/finally` 里换 `originalSensorSize` 再量、量完还原）+ `FrameSensorSizeX` / `LensShiftY` 逐式在；`BattleCameraSreenSize.Start/ResolutionHasChanged/Initialize` 在。
- 真残余**两件且都不必改**：(a) `playerCardAreaSizeHelper.sizeDelta.y` 那一跳在 `CurrentSizeMultiplier` 恒 1 时**幂等**；(b) `smallScreenUI` 下 M 的真值（`PlayerHand+0x3c` 写点）**没查到**（进 §8 未查清）。
- 只做一件事：把 `CombatAutoZoom` 文件头那句「⛔ 不照抄把缓存值写进相机那一半」与现行代码对齐（**注释级、0 行数变化**）。

### J-08 · `ScenarioMaterialFader` 的 `doFade` 取反（362/367）· 2 条
- 只补原版那两句 + `Update` 首句闸：`_doFade = !_doFade`（两个分支末尾各一句）、`if(!_doFade) return;`。
- 🔴 **抄完立刻读数**：原版「每条环境一次变更 = 1 次 `DoScenarioBlend`」。若我们同一件被调 2 次（`EnvironmentApplier` 的 `AttachBlendables` + `Apply` + 卸载三条路），照抄会把该淡的那次取消 ⇒ **那时要修的是调用方次数**，⛔ 不是把开关撤掉。现有 `BlendCalls/BlendCallsForward` 计数探针就是为这次读数准备的。
- **验收口**：首调后 `DoFade==true`、二调后 `DoFade==false` 且 `Advance` **不推进** α（改前恒推进）。

### J-09 · `DoSimpleShake` 的四跳接线（130）· 1 条
- 6 行改动（逐行见施工单 130）：`EffectResolver.EmitTraitRemoved` 加可选 `effect` → `RuleCore` 两处破舱点各发 `"droppod-open"` / `"droppod-destroyed"` → `BattleDriver` 的 `EvtKind.TraitRemoved`（**今天 0 消费者**）加 1 行消费。
- ⛔ `WarpforgeVFX/**` **一行不用改**（组件、preset、`OnShake` 全已就位）。
- **验收口**：`ruleprobe play check` **逐字节不变**；事件流断言两跳各一次。

### J-10 · 引擎判重族（187–190）· **已实现，0 行行为改动**
- 现读：`Enchantments.IsEnchantmentBuffType`（`{12,15,16}`）· `EffectResolver.IsSameHandEffect`（逐字段、含五档 until、明确「不比 RecordId」）。主账四条的「差异」全是 2026-10 以前的旧说法。
- 只补四条单测（见施工单）。

### J-11 · 轮抽环境卡（208）· 1 条
- `OffensiveCards.ChoicesForDraft(faction, offensive, victories)`（新）+ `DraftModeWindow` 的消费点（**该层今天不存在** ⇒ 需按原版轮抽流程接面板；用户已把进攻/防御面板关掉 ⇒ 接出来也默认关）。
- **验收口**：`victories = 0/2/3/6/9` × 两侧 ⇒ 防御 `1/1/1/2/3`、进攻 `0/0/1/2/3`（`>5` / `>8` 是**严格大于**）。

### J-12 · `Coffee.UIEffect` 导入 + ImageQuad 版（455）· 1 条（成本最大）
- 三步：导 dll → 搬 shader 载体（⚠️ bundle 里的 shader **落不了工程资产**，只能运行时加载）→ 写 `UIEffectTogglerCoffeeEffects` 等价物。
- ⚠️ `UIEffectBase` 依赖 uGUI `Graphic`/`IMeshModifier`/`IMaterialModifier`，而 Shell 是 `ImageQuad` ⇒ **uGUI 版只服务 Battle 那批；Shell 那 6 处必须另写 ImageQuad 版**。估 0.5–1 天 + 2–5 天。
- **还差**：7 个实例的逐实例 `uiEffects[]` 指向与触发时机**未全读** ⇒ 接线方按实例再核。

---

## §3 「不派活 / 只补断言」清单（13 + 2 条）

| 账行 | 方法 | 处置 | 理由 |
|---|---|---|---|
| 150 | `CameraVerticalFramer.CalculateFraming` | **已对齐，0 行** | 两半都在；④ 那一跳在 M 恒 1 时幂等；只剩注释订正 |
| 154 | `…ResolutionHasChanged` | **已对齐，0 行** | `Initialize(true)` 等价物在 |
| 155 | `…Start` | **已对齐，0 行** | 信号总线换静态事件 + 每帧比屏宽高（已如实标为等价物） |
| 175 | `CardAnimController.RotateAnim` | **已对齐，0 行** | `boardCenter` 支已在 `BattleDriver` `:23085-23109`（原版轴 = `Vector3.forward`、`−90°`）；两条近似如实记 |
| 187 | `CardEffect.IsEnchantment` | **已对齐 → 建议 B** | `Enchantments.IsEnchantmentBuffType` 已覆盖 12/15/16 |
| 188 | `CardEffect.IsSameEffect` | **已对齐 → 建议 B** | `EffectResolver.IsSameHandEffect` 逐字段 |
| 189 | `CardEffect.SameIsPlayerEffect` | **已对齐 → 建议 B** | 由 `ActingUnit` 引用相等覆盖 |
| 190 | `CardEffect.SameTimingAsEffect` | **已对齐 → 建议 B** | 由 `Expire`+`ExpireTurn` 覆盖 |
| 432/433/438 | `UIBorderGlow` 三个回调 | **等价物已在** | 每帧比对「网格实例/顶点数/视口值」；落地只等 J-04 的载体 |
| **510** | `CardScript.ForceDisableCanActAnim` | **建议改判 D（待裁）** | `decomp_full`+ILSpy 只有定义、**0 调用点**；`assets_full` 全库 **0 处字符串引用** ⇒ 原版从不触发（铁律 11 例外①）；且我们的「可攻击浮标」本来就有（`CardView._canAct`/`SetCanAct`/`CanActVisible`）⇒ 主账「无该浮标」也已过期 |
| **498** | `BattleManager.StopTargetedSpellAnim` | **判据待读** | 方法体只有 `Destroy(this+0x370)`，但**前置**（逐卡 `targetedSpellAnim` PPtr）没读；读法：重读 1,351 张卡 SO（`allcards` 2026-10-18 起本地可读）。全表为空 ⇒ 连同 `StartTargetedSpellAnim` 一起判 D |

---

## §4 P3 / 远端候选（**只给证据 + 时间限定，⛔ 不判 D**）

> 口径（铁律 11 例外② + §四 P3）：判据在**远端 / 服务端**的，列在这里，**不自行判 D**；时间限定 = **本 build（离线版）**。

| # | 候选 | 证据 | 时间限定 / 影响面 | 处置建议 |
|---|---|---|---|---|
| R1 | **J-01 段位那两处（`Division Image` / `Rank Icon`）的触发数据** | 动画本体（`_Blend` 补间 + `Rank Up`/`Division Up` clip）**全在本地**；但「什么时候升段」的数据 = `RankedDivisions`（服务端）——`Shell/RankedDivisionChangeWindow.cs` 文件头已写「段位名/评分/印玺数全在服务端」 | 本 build 无服务端 ⇒ 用本地替身（`RankedData`）驱动；**动画照做** | 动画照复刻，触发写「数据是替身 + 出声」，⛔ 不判 D |
| R2 | **`UIBorderGlow` 89 个实例里落在 P2 社交/排行榜/联盟窗口的那批** | 实例计数 = 89（B8_AB §七）；本轮只定位到 **1 个真实挂载点**（Forge 的 `Collect Highlight Effect`）。哪些其余窗口引用它**未逐窗口核** | 本 build：那批窗口多数靠服务端数据（P2/P3 边界） | **本轮不派**；按窗口逐条开账后再做（不判 D） |
| R3 | **轮抽环境卡的「卡名/卡引用」** | `OffensiveCards.cs` 文件头：卡引用在**远端 CCD**、本地解不出真名（`cardName 0/39`）⇒ 表里是「SO 名→贴图名」反推的**候选名** | 本 build：档位逻辑可写、**卡名不可得** | 档位逻辑照做（J-11）；卡名沿用候选并写明「名字是候选、不是卡面名」，⛔ 不判 D |
| R4 | **`PlayScaleIncomingBox` 类开包/奖励箱的 `.sound`/远端数据**（本域外，仅提示） | 本域 40 条里**没有**这一项 | — | —— |

> ✅ 本轮 40 条**没有一条**因远端而被判 D。

---

## §5 共享瓶颈与「文件所有权」表（派活前必须照这张表排）

| 文件 | 谁在用（本轮） | 独占规则 |
|---|---|---|
| `CardPresentation/Battle/BattleDriver.cs` | **J-05**（7 条）+ **J-09**（1 行消费） | 🔴 同一时刻**只能一个写手**；J-09 要么并进 J-05 的同一写手，要么排在 J-05 之后 |
| `CardPresentation/Battle/ImageQuad.cs` | **J-04** | 🔴 **全工程 UI 总件**；只允许 J-04 改，且必须逐档验 6 Shell 宿主 + Battle |
| `CardPresentation/Core/CardView.cs` | J-05（只加只读 getter）· J-02（只调用 `SetBody3DVisible`） | 🔴 共用件；**只加不改行为**（新增成员追加类尾） |
| `CardPresentation/Shell/DraftModeWindow.cs` | **J-03**（`BuildPackCard`）+ **J-11**（消费点） | 🔴 两包**串行**（同文件） |
| `RuleEngine/Core/EffectResolver.cs` | **J-09**（`EmitTraitRemoved`）+ **J-10**（只加断言） | 🔴 串行（且都是引擎核心） |
| `RuleEngine/Core/RuleCore.cs` | **J-09** | 🔴 独占（18 条账在那个文件里，改动要极小） |
| `WarpforgeVFX/Runtime/*` | 本轮 **0 行改动**（组件都已就位） | 🔴 别动；`UIViewportUvWriter.cs` 只在 J-04 的「二选一」里被删/留 |
| `CardPresentation/Shell/CardDetailPopup.cs` | **J-02** | 独占；新成员追加类尾（否则账里 A 落点全漂） |
| `CardPresentation/Shell/BoosterPackOpenWindow.cs` / `BoosterPackAnim.cs` / `RankedDivisionChangeWindow.cs` | **J-01** | 独占（三文件同一写手） |
| `CardPresentation/Battle/ScenarioMaterialFader.cs` | **J-08** | 独占（小件） |
| `CardPresentation/Battle/CombatAutoZoom.cs` / `BattleCameraSreenSize.cs` | **J-07** | 独占（只改注释，0 行数变化） |

---

## §6 并发波次建议（并发上限 10，别贴满；本轮实际可并行 7 路）

| 波次 | 可同时开工 | 说明 |
|---|---|---|
| **第 0 波（先放 1 路，最便宜）** | **J-08**（2 条，1 个小文件）+ **J-09 的 6 行** 可同批 | 都不依赖任何人 |
| **第 1 波（6 路，文件互不相交）** | **J-01** · **J-02** · **J-03** · **J-04** · **J-07** · **J-10** | 六路文件两两不交；J-02/J-01 都要 3 条 clip ⇒ 先让调度台把 clip 导入排进这一波的起手动作 |
| **第 2 波（1 路）** | **J-05**（7 条 · BattleDriver 独占） | 必须等 J-09 的 `BattleDriver` 那 1 行落完（或同一写手串行） |
| **第 3 波（2 路）** | **J-11**（等 J-03 放掉 `DraftModeWindow.cs`）· **J-12**（成本最大，独立目录） | J-12 可随时起，但会占一路很久 |
| **不派** | 510（建议 D）· 498（判据待读） | — |

**硬约束复述**：`BattleDriver.cs` / `ImageQuad.cs` / `DraftModeWindow.cs` / `EffectResolver.cs` / `RuleCore.cs` / `CardView.cs` 六个文件**同一时刻各只能一个写手**。

---

## §7 本域「怎么验」的合法读数口（白名单）

| 口 | 用途 | 跑的人 |
|---|---|---|
| `bash 工具/ruleprobe.sh play all dump` / `play check` / `play <id> preset …` | **引擎侧**结算层对拍 / 单卡夹具（J-09·J-10） | 写手可跑（不跑 Unity） |
| `bash 工具/typecheck.sh` | 改完任一 `.cs` 必跑（秒级） | 写手 |
| `AnimFXCheck.Run` | AnimFX 模块层断言（4 类） | **只有调度台**（Unity 批处理） |
| `BlendProbe` / `MatBlendProbe` / `LutBlendProbe` | 材质/blend 参数读数 | **只有调度台** |
| 宿主断言（`BattleScene.Run` / `ShellScene.Run` / `RewardsScene.Run` / `RuleEngineTest.Run`） | 表现层断言 | **只有调度台**（同步点） |
| 代码层读数（`material.GetFloat` / `mesh.GetUVs` / 计数属性） | 属性与几何 | 写手/自检 |
| **真机**（`-wfscene Shell` / `wf_click.py`） | 「像不像」 | **用户自己看** |
| ⛔ **禁用** | `EffectCompare` / 任何截图并排比 / 像素结论 | — |

---

## §8 没查清的部分（如实记，⛔ 别当成已对齐）

1. **`PlayerHand.CurrentSizeMultiplier`（`PlayerHand+0x3c`）的写点没查到** ⇒ `smallScreenUI` 那一档的取景（150/154/155）没复刻。**还差**：读 `PlayerHand` 的 `CurrentSizeMultiplier` 属性体 + 谁写 `+0x3c`（VA 级反汇编）。
2. **`Level Up Effect` / `Level Up Effect Down` 的 shader 名没解**（共用外部材质 `m_FileID 5 / m_PathID 4`，要解 `scenes_scenes_mainmenuwarpforge` 的 externals 第 5 项）⇒ J-02 的 `_Reveal`/噪声只能验「浮点写对」，验不到「画对」。
3. **`UIEffectTogglerImage` 两处 `Toggle` 的实参与触发时机没读方法体**（见 B8_AS §3·2 末，本轮未推进）。
4. **`Coffee.UIEffect` 7 个实例的逐实例 `uiEffects[]` 指向与触发时机未全读**。
5. **`BattleCardUI.PlayInvokeAnimation` 播的 clip 字段没读**（`BattleCardUI__PlayInvokeAnimation.c:30-40` 只写了 `Play(clip)+speed 1.0`）⇒ 507 的「是否复用 `CardFeel.HandToBoard` 那支 clip」未定。
6. **`CardScript.DissolveWholeCard` 的 `Vector3.one` 判据是「静态块偏移 + 类身份」两条反证**（`+0x0c = one` 由 ILSpy `Vector3.oneVector` 反证；`+0x48 = forward` 由布局推）—— 不是逐字节读出来的 ⇒ 508 的 ② 标为「判据充分但非字面量直读」。
7. **`AnimateUIMaterialProperty` 空槽的逐字节 `Equals(default)` 等价未证**（G12 已记；6 个出厂实例可见结果相同）。
8. **本代理不跑 Unity** ⇒ 上述「验收口」全部是**README 级别的可执行步骤**，不是「已跑过」。

---

## §9 与本文件一起交付的东西

| 文件 | 内容 |
|---|---|
| `判据/B9_特效C_施工单.tsv` | 12 列 × 40 行（表头逐字照 `判据/B9_剩余C_施工单.tsv`）· LF · 无 BOM · 40 行与主账 `状态==C` 的 40 条**逐条一一对应**（已用脚本核对：missing 0 / extra 0 / 顺序一致） |
| `判据/B9_特效C_派活包.md` | 本文件 |
| `报告/B9_J4FX_特效C施工单.md` | 结论 + 逐条证据 + 没查清 + 改动清单 + 顺手发现（只报不改） |
