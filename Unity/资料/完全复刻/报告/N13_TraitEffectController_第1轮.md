# N13 · `TraitEffectController` 钩子层 —— 第 1 轮（查判据）

> 代理：`N13 · 查判据`，2026-10-25。**只读**：没改任何 `.cs` / `.tsv` / 正本 / 别人的报告。
> **产出**：`资料/完全复刻/判据/TraitEffectController_钩子层.md`（本报告是它的摘要 + 派单建议）
> **没派子代理**（本轮上下文充裕，全部自己查；见 §⑥「下一步」）。

---

## ① 结论（六问各一句）

1. **它是什么** —— `abstract class TraitEffectController : MonoBehaviour`，3 个抽象方法（`Toggle` / `UpdateValue` / `AimingAttack`）+ 1 个**死字段** `currentValue`。**没人 `new` 它**：它是 prefab 上的序列化组件，随 `BattleManager.CreateAnim` 的 `Instantiate` 出生，随状态框被 `Destroy` 一起消失。所谓「3 个调用点」里，**第一个其实是 `ResolveTraitAnim` 的内部回调包装**（⇒ 所有走 `ResolveTraitAnim` 的路径都会调 `UpdateValue`），第二个（`PlayTriggerAnim`）**实际无效**（唯一调用方传的是非 trait 的 `sentinelUnitAnim`），**第三个才是真正的「刷新」入口**（`DisplayStatusAnim` 的「框已存在」分支）。
2. **它管哪些 trait** —— **只有 2 条**：`huntMark`(1260) / `markerlight`(1170)。**84 个 bundle 里这两个控制器各只有 1 个实例**，分别挂在 `HuntMark_IdleEffect` / `MarkerlightIdle`（两条 trait 的 **OnPlay(2)** 框）。`UpdateValue` **事件驱动、绝不每帧**：框出生时一次 · 框被再次显示时一次 · 触发显示时一次。
3. **输入输出** —— 输入 = **一个 `int`**（`GetCurrentTraitValueWithModifiers` = 基值 + Σ modifier，钳 ≥0）；输出 = **只改 prefab 里那个 TMP 的 `text` + `DOPunchScale(0.1, 0.5s, 10, 1.0)`**。不写引擎状态、不发事件。
4. **我们现状** —— `RuleEngine/**` + `CardPresentation/**` **0 命中**。但**接收端全在**：`TraitFrames` 已按原版两本字典建完、这两条 trait 的 OnPlay 框**已经在播**（`trait_effect_bindings.json` 已绑）。缺的只有 ① 计数器组件 ② 「再次显示时刷新」的调用 ③ 两个 idle 框 **4 秒后自杀**（F1，必须先修）。
5. **与已有机制的关系** —— **互补，不重叠**。`Auras.Recompose` = 产出值的上游（引擎）；`ActivateTraitsOnSummonOrEnchantment` = 首次显示的时机（我们已用「新视图 ⇒ `_prev` 空 ⇒ 全集重演」等价实现）；`WhenEvent`/`EvtKind.Trigger` = 「触发了」（→ `DisplayTrigger`/`TraitParticles`）；**本层 = 「值变了 / 又被加了一层」**。裁定 = **并存，但 `TraitFrames` 是唯一实例化者**，本层只挂在它生出来的实例上。
6. **怎么做** —— 3 个新文件（`TraitEffectController` / `HuntMarkController` / `MarkerlightController`，各一个文件）＋ 把组件挂在 `TraitFrames.Play` 里 ＋ **4 个入口（E1–E4）各传一次值** ＋ **8 条断言**（含 2 条灭自证）。另有两件前置修：**idle 框不许自动销毁** · **把 trait 值传进 `TraitFrames`**。

---

## ② 判据（一眼版）

| 项 | 值 | 出处 |
|---|---|---|
| 基类形状 | `currentValue` / 空 `Start` / 抽象 `Toggle`·`UpdateValue`·`AimingAttack` | `TraitEffectController.cs:5,7-9,11,13,15` |
| 全 build 实例数 | HuntMark ×1 · Markerlight ×1 | 84 bundle 扫 `m_Script` pathID（§八·B） |
| 挂载点 | `HuntMark_IdleEffect`（根 `Transform` `-8415374602843260973`）· `MarkerlightIdle`（`4465041863265759825`） | 两个 `MonoBehaviour_*.json` |
| 出生 | `Instantiate(prefab, anchor.position, prefab.localRotation, anchor)` → 赋值 `localScale` → **invoke 回调（这里调 UpdateValue）** → `AddComponent<CardAnimController>` + `Initialize` | `BattleManager__CreateAnim.c:62-81` |
| 销毁 | `RemoveStatusAnim`/`CleanStatusAnims`：有 `AnimFXController` ⇒ `Exit()`，否则 `Destroy`。**这两个框没有 `AnimFXController`** | `BattleCardUI__RemoveStatusAnim.c:47/58/62,81/92/96` |
| 值 | `GetCurrentTraitValueWithModifiers(trait)` = Σ modifier + base，钳 ≥0 | `EntityScript__GetCurrentTraitValueWithModifiers.c:56-70` |
| 计数 + 弹动 | `counter.text = v.ToString(); DOPunchScale(t, Vector3.one*0.1f, 0.5f, 10, 1.0f)` | `HuntMarkController__UpdateValue.c:17-30`（常量 `.rdata` `0x34B2DC4=0.1` `0x34B2BB4=0.5` `0x34B2BB8=1.0`） |
| Markerlight 初始化 | `alreadyAttacked=false; Normal on; Expiring off; foreach floors: mat.SetFloat("_RippleScale", lossyScale.x)` | `MarkerlightController.cs:25-48` |
| Markerlight 序列化真值 | `fadeTime = **0.5**`（prefab 覆写，**不是** ILSpy 的 1f）· `updateMarkerLightVFX = **null**`（死支）· `floors` = 两个 `Grid floor` | `MonoBehaviour_-3235397510437391791.json:32-46` |
| 触发时机（唯一真实源） | `AddEffect`：`cardState∈{2,0xf,3,0x11}` ∧ `CardTrait._hasInPlayAnim(+0x20)` ⇒ `DisplayStatusAnim(trait)`；`huntMark`(0x4ec) 还先 `DisplayTriggerAnim(0x4ec)`；`markerlight`(0x492) 只 `BroadcastMarkerlightResolved` | `CardScript__AddEffect.c:635-644, 645-664` |
| 首次显示时机 | 召唤/附魔：遍历 `CurrentTraits(+0x128)` 逐个 `DisplayStatusAnim` | `CardScript__ActivateTraitsOnSummonOrEnchantment.c:42-69` |
| `Toggle` | **0 调用点**（C# 无、prefab UnityEvent 无）、两个子类都是空体 ⇒ 判死 | §八·A |
| `currentValue` | **本 build 无读无写** | `HuntMarkController__UpdateValue.c:13-30` |

---

## ③ 证据（关键 `.c` / `.cs` 行号）

**基类与两个实现**
- `out_cpp2il_dev_ilspy/TraitEffectController.cs:3,5,7-9,11,13,15`
- `…/HuntMarkController.cs:7,9,11-13,15-17,19-29,31-37`（`AimingAttack` **throw NotImplementedException**）
- `…/MarkerlightController.cs:5,7-23,25-48,50-52,54-79,81-86,88-93,95-101,103-106`

**三处 `UpdateValue`**
- ① `BattleManager.cs:285-297` ←→ `BattleManager.__c__DisplayClass465_0___ResolveTraitAnim_b__0.c:14-31`（vtable 槽 `0x198`，参数 = DisplayClass `+0x10` = `traitValue`）
- ② `CardScript.cs:17159-17200`（`PlayTriggerAnim`，`GetComponent` 在 `:17179`、调用在 `:17186`）←→ `CardScript__PlayTriggerAnim.c:41-49`
- ③ `BattleCardUI.cs:1652` 起（`DisplayStatusAnim`），刷新那一支在 `:1734-1779` ←→ `BattleCardUI__DisplayStatusAnim.c:126-198`（`UpdateValue` 在 `:190-196`）

**首次 vs 再次**
- Start(0)：`BattleCardUI__DisplayStatusAnim.c:86-119`（`ResolveTraitAnim(..., traitValue, callback=null)` 在 `:112-119`）
- OnPlay(2) 且已存在：`:126-198`（`SetActive(true)` → `UpdateValue`）
- OnPlay(2) 且不存在：`:201-292`（`ResolveTraitAnim(..., traitValue, callback=b__0)`）
- 触发档：`BattleCardUI__DisplayTriggerAnim.c:71`（只认 3/6）·`:88-91`（去重守卫）·`:97-104`（`ResolveTraitAnim` 带 `traitValue`）

**我们说「没有」的时候搜过什么** —— 见判据文件 §八（含两条「反证搜法是对的」的对照）。

---

## ④ 没查清（如实）

| 项 | 状态 | 还差什么 |
|---|---|---|
| **原版实况（跑游戏看这两个框）** | **没跑到** | 原版已关服；这两个框只在**战斗内**出现，战斗内需要远端卡数据（`inj FAIL: empty Data`）⇒ 进不去。静态判据已完整（组件字段 + 方法体 + prefab 值三项齐），但**「弹动的观感」没有实拍对照** |
| `Vector3.one * 0.1f` 里那个 `one` | **推断（高）** | 依据：`DAT_1842da2a8` 是 `UnityEngine.Vector3` 类型静态块，`+0x00` = `zero`（`DisplayStatusAnim.c:149-159` 用它当 `localPosition`，显然为零）、`+0x0C` = 下一个静态 = **`one`**。**没去 DLL 里读那 12 字节**（本轮不值得为一个振幅开一条 .rdata 取证） |
| `MarkerlightController` 的 `MaterialFader` 全语义（`Update` 插值、材质数组顺序） | **未展开** | 属 `P2`；六个方法体已列出路径，下一批再读 |
| `_RippleScale` 在 shader 里的含义 | **未查** | 只影响观感；`工具/dump_shader.py` 可查 |
| `Grid floor` ×2 是否就是 `floors` 字段的两个元素 | **已坐实**（不是推断） | `ParticleSystemRenderer_-7782206327072345519.json` / `…5681904949217997393.json` 的 `m_GameObject` → 两个 `GameObject/Grid floor_*.json` |

---

## ⑤ 顺手发现（7 条，请调度台分流；⛔ 我一条都没动手）

| 编号 | 发现 | 判据 | 建议 |
|---|---|---|---|
| **F1** 🔴 | 两个 idle 框在我们这边 **4.3 s / 4 s 后自毁** —— 计数器会凭空消失 | prefab 无 `AnimFXController` ⇒ 效果库 `destroyTime:-1, natural:4.3/4` ⇒ `WarpforgeEffectLibrary.AutoLifetime():78-79` 回落 `natural`；原版只由 `Destroy` 收（`BattleCardUI__RemoveStatusAnim.c:58`） | **列为本件 P0-1**（`TraitFrames` 持久的 OnPlay 框显式传 `autoDestroySeconds = +∞`） |
| **F2** 🔴 | **markerlight 摘除时机**与原版不同 | 原版：攻击后只置 `+0x57`，**回合末**才 `RemoveTrait(0x492)`（`CardScript__OnTurnEnd.c:135-141`）；我们：`RuleCore.cs:6884` 当场 `RemoveAll("markerlight")` | **要重新核规则书**（可能是有意简化，但账上没写）。不是本件范围 |
| **F3** | `CardScript.ActivateTraitAnim`（`CardScript.cs:16696`）**0 调用方** | `grep -rl ActivateTraitAnim decomp_full/` 只命中它自己 | 记账；⛔ 别照它接调用点 |
| **F4** | `TraitEffectController.currentValue` **死字段** · `Toggle(bool)` **0 调用点** | 判据文件 §1.2 / §八·A | 实现成形状（空方法），⛔ 别赋予语义 |
| **F5** | `MarkerlightController.updateMarkerLightVFX` 在本 build 是**空引用** | `MonoBehaviour_-3235397510437391791.json:33-36` | `UpdateValue` 里那段 `Instantiate` 是死支（铁律 11 例外①），⛔ 别猜一个 VFX 塞进去 |
| **F6** | `markerlight`=**1170/0x492**、`huntMark`=**1260/0x4ec** 在反编译里大量以**字面量**出现 | `CardScript.cs:11264…11330`（`(Int32Enum)1170`）· `AddEffect.c:635,639` | 找代码路径时⛔ 别只搜名字，要搜数字 |
| **F7** | `MarkerlightIdle.fadeTime` = **0.5**（prefab 覆写），ILSpy 字段默认值 = **1f** | `MonoBehaviour_-3235397510437391791.json:32` vs `MarkerlightController.cs:17` | 照 ILSpy 默认值写会慢一倍（铁律 5·c） |
| **F8** | `Renderer.GetMaterial()` = **`renderer.material`（实例副本）**，**不是** `sharedMaterial` | `d:/2/tools/il2cpp_out/dump.cs:526597-526599`（`[FreeFunction(Name="RendererScripting::GetMaterial")] private Material GetMaterial()`）与 `:526579` 的 `material` 属性成对；`sharedMaterial` 走 `:526601-526603` | ⛔ 别改成 `sharedMaterial`（多张卡会互相写死 `_RippleScale`）；副本材质不自动回收，**原版同形** |
| **F9** | 我们导入的两个 prefab 根上**多一个 `WarpforgeEffectBinder`**（我们自己导出时加的） | `WarpforgeVFX/Prefabs/*.prefab` 的 `m_Script` | **不是差异**，⛔ 别当「我们多做了」删掉 |

---

## ⑥ 建议怎么派下一批

**几件、顺序、各改哪个文件**（文件所有权已互斥）：

| 批 | 件 | 文件所有权 | 依赖 | 预估 |
|---|---|---|---|---|
| **①**（1 个执行代理） | 两件前置修：F1（idle 框不自杀）+ 把 trait 值传进 `TraitFrames`（重载 `SyncKeywords(..., Func<string,int>)`） | `CardPresentation/Core/TraitFrames.cs` · `CardPresentation/Battle/BattleDriver.cs`（1 行） | — | 小 |
| **②**（1 个执行代理，与①并行） | 三个新文件：`TraitEffectController.cs` / `HuntMarkController.cs` / `MarkerlightController.cs`（后者**只做 `Start` + `UpdateValue`**，`MarkerLightUsed`/`AimingAttack` 留到 ⑤） | 新增 3 个 `.cs`（+ `.meta`），**不碰任何既有文件** | 无 | 中 |
| **③**（1 个执行代理，**必须等 ①+②**） | `Attach` + 4 个入口（E1–E4）+ `ReplayAndRefresh` + `EvtKind.StatusApplied` 接线 | `CardPresentation/Core/TraitFrames.cs` · `CardPresentation/Battle/BattleDriver.cs` | ① ② | 中 |
| **④**（1 个执行代理，可与③同批但**同一文件除外**） | 8 条断言（A-E1…A-LIFE + 2 条灭自证） | 新 `CardPresentation/Editor/TraitEffectAssert.cs`（**新建**，避免与 `BattleScene.cs` 抢写手） | ③ | 小 |
| **⑤**（P2，**另开一轮**） | `MaterialFader` 共用件 + `MarkerLightUsed` / `AimingAttack` 接线 | 新 `Core/MaterialFader.cs` + `TraitFrames.cs` + 引擎事件 | ③ | 大 |

**口径提醒（写给执行代理，简报里请原样带上）**
- ⛔ **不许新开第二条「trait → prefab」绑定** —— 唯一来源 = `Resources/trait_effect_bindings.json`。
- ⛔ **不许在 `BattleDriver` 里直接 `WarpforgeEffectPlayer.Play` 这两个 idle 框** —— 实例化者只能是 `TraitFrames`。
- ⛔ **不许把 `UpdateValue` 挂成「每帧/每次 sync 都刷」** —— `DOPunchScale` 会一直抖；灭自证 1 就是挡这个。
- ⛔ **不许按 trait 名挂组件**，要按 **prefab 名**（`HuntMark_IdleEffect` / `MarkerlightIdle`）—— 灭自证 2 挡这个。
- ⛔ **不许改这两个 prefab**（它们由 `WarpforgeArena1/Editor/EffectExporter.cs` 生成，手改会被下次导出覆盖）。
- ✅ 三个新类**必须一个类一个文件**（CLAUDE.md A1506/A1508：类名 ≠ 文件名 ⇒ 存盘会烘成 `!u!115` 存根 ⇒ player 崩）。

---

## ⑦ 第 1 轮订正（执行代理 `N15` 实测回灌 4 条 + 我补查 1 条；铁律 5）

`N15` 建文件时实测到判据有 4 处与源码不符（**它没改我的文件，只报**）；我逐条复核**全部成立**，已就地订正并留痕迹（判据文件头部有一张订正表）。第 5 条（`GetMaterial`）是本轮补查的。

| 编号 | 上一版（❌） | 复核结果（✅） | 判据怎么写的 |
|---|---|---|---|
| **F-A**（最危险） | 「`Counter` / `Normal State` / `Expiring State` / **两个 `Grid floor`** 都是根的直接子件」 | 前三个是；**`Grid floor` 不是** —— 嵌在 `Normal State/Base VFX/Fixed/` 与 `Expiring State/Base Vfx/Fixed/` 下（**4 层深**）⇒ `transform.Find("Grid floor")` **恒 null（静默）**、`_RippleScale` 永远不设。⚠️ 还有一处大小写不一致：`Base Vfx` vs `Base VFX` | §6.2 改成 `GetComponentsInChildren<ParticleSystemRenderer>(true)` + `name == "Grid floor"` 过滤（实测全树**恰好 2 个**，`!= 2` 必须出声）；A-ML 断言必须**真读材质里的 `_RippleScale`** |
| **F-B** | 「`MarkerLightUsed` / `AimingAttack` 都走 `ChangeState`」 | `MarkerLightUsed` = **直接三句**（`MarkerlightController__MarkerLightUsed.c:5-12`）；`ChangeState` / `FadeFixedElements` **全 build 零调用 = 死代码**（ILSpy 只有定义行 `:95` / `:103`，两个都是 `private`） | §6.1③ 改成照 `MarkerLightUsed` 写；`ChangeState`/`FadeFixedElements` 保留成私有方法 + 注释标死 |
| **F-C** | 骨架写 `public TextMeshPro counter;` | 原版是 **`private`**（`HuntMarkController.cs:9`）**且确实被序列化**（prefab JSON 里有 `"counter"`）⇒ `[SerializeField] private` | §6.1② 改正；⚠️ 我们是**运行期 `AddComponent`**，序列化字段恒空 ⇒ 必须 `Setup/Start` 里按名字绑；自检**走只读观测量**（`TextForTest` / `PunchCountForTest`），⛔ 不直读私有字段 |
| **F-D** | （上一版没写） | 我们导入的两个 prefab 根上**多一个 `WarpforgeEffectBinder`**（guid `d9d66f84…`，我们自己导出时加的）—— **不影响本层** | §1.3 补注：**不是差异**，⛔ 下个代理别当「我们多做了」去改 |
| **F-E**（本轮补查） | 上一版只说「`MaterialFader` 没有」，没说 `GetMaterial()` 的语义 | **`Renderer.GetMaterial()` = `renderer.material`（实例副本）**，⛔ **不是 `sharedMaterial`** | 新增 §3.2·b。**决定性判据** = `d:/2/tools/il2cpp_out/dump.cs:526597-526599`：`[FreeFunction(Name = "RendererScripting::GetMaterial", HasExplicitThis = True)] private Material GetMaterial()`，与 `:526579` 的 `public Material material { get; set; }` 成对；`sharedMaterial` 走另一个 native 入口 `RendererScripting::GetSharedMaterial`（`:526601-526603`）。旁证：同文件 `MarkerlightController__Start.c:41` 调的就是 `UnityEngine_Renderer__GetMaterial`；`MaterialChangerByQuality.cs:33` 里 `GetSharedMaterial()` 与 `GetMaterial()` **并列出现** ⇒ 两个 API。**影响**：用 `sharedMaterial` 会让多张卡互相写死 `_RippleScale`（A 卡的缩放决定 B 卡的涟漪）；用 `material` 的代价是每实例 2 份材质副本**不自动回收** —— **原版同形，照抄**，要收只能在 `OnDestroy` 里 `Destroy`（= 我们的口径，须注释）。另：`_RippleScale` 这个名字我们认得（`数据/游戏数据/original_shader_props.tsv` + `OriginalShaderBlendTable.cs`），不会静默 no-op。 |

**「上一版为什么错」**（写下来给下个代理）：F-A 是**外推**（只对前三个真算了 `m_Father` 链，`Grid floor` 没验就顺手带上）；F-B 是照 ILSpy 的**方法顺序**猜调用关系（`ChangeState` 排在 `MarkerLightUsed` 后面，看着像被它调）；F-C 是把被 IL2CPP 剥掉的 `[SerializeField]` 当成了「只有 public 才序列化」。⇒ 三条都是 CLAUDE.md 铁律 5·c「一个值 ≠ 全部情况 / 别照形状猜」的变体。

**对下一批的影响**：`P0/P1` 的排序**不变**；`②`（三个新文件）的简报里要把上表 **F-A/F-B/F-C** 三条**原样贴进去**（否则 N15 之外的代理会照旧判据写坏）。
