# `TraitEffectController` 钩子层 —— 判据与施工方案

> **产出**：`N13 · 查判据` 代理，2026-10-25。**只读**：⛔ 没改任何 `.cs` / `.tsv` / 正本。
> **对应报告**：`资料/完全复刻/报告/N13_TraitEffectController_第1轮.md`
> **要解决的账**：`交接_第七十八会话.md:50` · `调度台日志.md:364` · `报告/D_未分类_第1轮.md:49,98,125` ·
> `账/_parts/UNC_3.tsv:69`（`HuntMarkController.UpdateValue` = C/P1 · 整件缺失）
> **一句话**：原版这层**只有 2 个实现、只服务 2 个 prefab（各 1 个实例）**，机制是
> 「**实例化后 / 再次显示时，把 `GetCurrentTraitValueWithModifiers(trait)` 塞进 prefab 上的计数器**」。
> 我们**全仓 0 命中**，但**接收端（`TraitFrames`）已经在了** —— 这一层是「往已有实例上补一个组件 + 补两个刷新调用」，
> **不是**重建一套。

> 🔴🔴 **2026-10-25 · 第 1 轮订正（执行代理 `N15` 建文件时实测回灌 4 条 + 我补查 1 条；铁律 5，保留更正痕迹）**
>
> | 编号 | 本文件上一版写的（❌） | 实测/复核后（✅） | 落在哪一节 |
> |---|---|---|---|
> | **F-A** | 「`Counter` / `Normal State` / `Expiring State` / **两个 `Grid floor`** 都是根的直接子件」 | 前三个是；**`Grid floor` 不是** —— 它嵌在 `Normal State/Base VFX/Fixed/` 与 `Expiring State/Base Vfx/Fixed/` 下（4 层深）。照上一版写 `transform.Find("Grid floor")` ⇒ **恒 null（静默）** | §6.2（已改成「子件全扫 + 名字过滤」） |
> | **F-B** | 「`MarkerLightUsed` / `AimingAttack` 都走 `ChangeState`」 | `MarkerLightUsed` 是**直接三句**；`ChangeState` / `FadeFixedElements` **全 build 零调用 = 死代码** | §6.1③ |
> | **F-C** | 骨架里 `public TextMeshPro counter;` | 原版是 **`private`** 且**确实被序列化** ⇒ `[SerializeField] private`；自检走只读观测量 | §6.1② |
> | **F-D** | （没写） | 我们导入的两个 prefab 根上**多一个 `WarpforgeEffectBinder`**（我们导出时加的）—— **不是差异，别改** | §1.3 |
> | **F-E** | （上一版没定） | `Renderer.GetMaterial()` = **`renderer.material`（实例副本）**，⛔ 不是 `sharedMaterial`（`dump.cs:526597-526599` 的 native 绑定坐实） | §3.2·b |
>
> 「上一版为什么错」：**F-A 是外推**（只对前三个真算了 `m_Father` 链，`Grid floor` 没验就顺手带上）；
> F-B 是照 ILSpy 的方法顺序猜了调用关系（`ChangeState` 排在 `MarkerLightUsed` 后面，看着像被它调）；
> F-C 是把 IL2CPP 里被剥掉的 `[SerializeField]` 当成了「public 才能序列化」。
> ⇒ 这三条正好是 CLAUDE.md 铁律 5·c「**一个值 ≠ 全部情况 / 别照形状猜**」的三个变体。

---

## §〇 一页速查

| # | 问题 | 一句话答案 | 置信度 |
|---|---|---|---|
| 1 | 它是什么 | `abstract class TraitEffectController : MonoBehaviour`，**3 个抽象方法**（`Toggle`/`UpdateValue`/`AimingAttack`）+ 1 个守恒字段 `currentValue`。**不是 `new` 出来的**，是**prefab 上的组件**，随 `BattleManager.CreateAnim` 的 `Instantiate` 一起出生，随状态框被 `Destroy` 一起消失。**3 个 `UpdateValue` 调用点**里有一个是 `ResolveTraitAnim` 的内部包装 ⇒ **所有走 `ResolveTraitAnim` 的路径都会调它**。 | 高 |
| 2 | 管哪些 trait | **只有 2 个**：`huntMark`（1260）与 `markerlight`（1170）。**全 build 84 个 bundle 里这两个控制器各只有 1 个实例**，分别挂在 `HuntMark_IdleEffect` / `MarkerlightIdle`（都是这两条 trait 的 **OnPlay(2)** 框）。`UpdateValue` **不是每帧调**，是**事件驱动**（每次「trait 被加上 / 再次显示」与每次「触发显示」）。 | 高 |
| 3 | 输入输出 | 输入 = **`int` 当前 trait 总值**（`EntityScript.GetCurrentTraitValueWithModifiers` = 基值 + Σ modifier，钳 ≥0）；输出 = **只改 prefab 里的那个 TMP 文本 + `DOPunchScale` 弹一下**（`MarkerlightIdle` 另可选播一个 `updateMarkerLightVFX`，**该字段在本 build 是空引用 ⇒ 死支**）。**不写引擎状态、不发事件、不重算规则**。 | 高 |
| 4 | 我们现状 | `RuleEngine/**` + `CardPresentation/**` **0 命中**。**接收端已有**：`CardPresentation/Core/TraitFrames.cs` 已按原版两本字典建好、`huntMark`/`markerlight` 的 OnPlay 框**已经在播**（`trait_effect_bindings.json` 已绑）。**缺的只有**：① 计数器组件 ② 「再次显示时刷新」的调用 ③ 两个 idle 框 4 秒后自杀（见 §七 F1）。 | 高 |
| 5 | 与已有机制的关系 | **互补，不重叠**：`Auras.Recompose` 是**产出值**（引擎侧、连续光环重算）；`ActivateTraitsOnSummonOrEnchantment` 是**首次显示的时机**（我们用它对应的「新建视图 ⇒ `_prev` 为空 ⇒ 全集重演」等价实现）；`WhenEvent`/`EvtKind` 是**「这条 trait 触发了」**（→ `TraitFrames.DisplayTrigger` / `TraitParticles`）。本层是**「这条 trait 的值变了」**。三者**共用同一个实例化者（`TraitFrames`）**，⛔ 不许再造第二个实例化路径。 | 高 |
| 6 | 怎么做 | 见 §六。**4 个文件 + 1 处签名扩展 + 2 个前置修**；**8 条入口各配 1 条断言**（含 2 条灭自证）。 | — |

**施工顺序建议**（详见 §六·7）：`P0` 两件前置 → `P1` 三个新文件 → `P1` 挂到 `TraitFrames` 的 4 个入口 → `P2` markerlight 的两条附加行为。

---

## §一 Q1 —— `TraitEffectController` 是什么

### 1.1 原版行为（逐字）

```csharp
// D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TraitEffectController.cs  （全文 16 行）
public abstract class TraitEffectController : MonoBehaviour   // :3
{
    protected int currentValue;                                // :5
    protected virtual void Start() { }                          // :7-9
    public abstract void Toggle(bool option);                   // :11
    public abstract void UpdateValue(int newValue);             // :13
    public abstract void AimingAttack(bool option);             // :15
}
```

两个实现（全 build 就这两个）：

| 类 | 文件 | 序列化字段 | 覆写了什么 |
|---|---|---|---|
| `HuntMarkController` | `HuntMarkController.cs`（39 行） | **只有 1 个**：`TextMeshPro counter`（`:9`） | `Start` 空覆写（`:11-13`）· `Toggle` **空体**（`:15-17`）· `UpdateValue` **有实现**（`:19-29`）· `AimingAttack` **直接 `throw NotImplementedException`**（`:31-37`） |
| `MarkerlightController` | `MarkerlightController.cs`（108 行） | 8 个：`counter` `normalStateObject` `materialFaderNormalState` `expiringStateObject` `materialFaderExpiringState` `fadeTime` `updateMarkerLightVFX` `floors[]` `alreadyAttacked`（`:7-23`） | `Start` **有实现**（`:25-48`）· `Toggle` **空体**（`:50-52`）· `UpdateValue` **有实现**（`:54-79`）· `AimingAttack` **有实现**（`:81-86`）· 另有 `MarkerLightUsed()`（`:88-93`）`ChangeState`（`:95-101`）`FadeFixedElements`（`:103-106`） |

### 1.2 生命周期：谁创建 / 谁持有 / 什么时候销毁

| 环节 | 原版事实 | 出处 |
|---|---|---|
| **谁 `new`** | **没人 `new`**。它是 prefab 上的序列化组件，**随 `Instantiate` 一起出生**。 | `BattleManager__CreateAnim.c:62-63`（`Object.Instantiate(prefab, anchor.position, prefab.localRotation, anchor)`） |
| **实例化在哪** | 卡的 **`effectsAnchor`** 下（`BattleManager.GetAnimTransform` case 0）。缩放**赋值**成 prefab 自己的 `localScale`。 | `BattleManager__CreateAnim.c:65-72` |
| **出生后的第一拍** | ① `Instantiate` → ② `localScale = prefab.localScale` → ③ **invoke 回调**（这一步里就调 `UpdateValue`）→ ④ `AddComponent<CardAnimController>()` + `CardAnimController.Initialize(...)`。 ⚠️ **`UpdateValue` 在 `CardAnimController.Initialize` 之前**。 | `BattleManager__CreateAnim.c:73-81` |
| **`Start()` 何时跑** | Unity 生命周期 —— 下一拍。`MarkerlightController.Start` 就是在这里把 `Expiring State` 关掉、给两个 `Grid floor` 设 `_RippleScale`。 | `MarkerlightController.cs:25-48` |
| **谁持有** | ① GameObject 本身挂在卡的 `effectsAnchor` 下（卡离场 ⇒ 整棵子树一起没）· ② **OnPlay(2) 那一档**还会被写进 `BattleCardUI.TraitFrameEffectsPlay`（`Dictionary<DefinedTrait,GameObject>`，偏移 `+0x2d0`），用来「已有就不重播」。 | `BattleCardUI.cs:631`（字段）· `BattleCardUI__DisplayStatusAnim.c:121-198` |
| **什么时候销毁** | `RemoveStatusAnim` / `CleanStatusAnims`：取该 GO 的 `AnimFXController`——**有 ⇒ `Exit()`；没有 ⇒ 直接 `Destroy`**。**这两个 idle 框没有 `AnimFXController`**（prefab 根只有 `Transform` + 控制器两个组件）⇒ **走 `Destroy`**。 | `BattleCardUI__RemoveStatusAnim.c:47/58/62`（play 表）· `:81/92/96`（trigger 表）· 组件清单见 §1.3 |
| **`currentValue` 字段** | 🔴 **本 build 里没有任何一处读或写它**（两个子类的 `UpdateValue` 都是直接用参数 `newValue`）。⇒ **死字段**，按形状保留即可，⛔ 别拿它当状态。 | `TraitEffectController.cs:5` + `HuntMarkController__UpdateValue.c:13-30`（用 `param_2`，不写 `+0x18`） |
| **`Toggle(bool)`** | 🔴 **本 build 里 0 个调用点**（C# 与 prefab UnityEvent 都没有；两个子类都是**空体**）。⇒ 见 §八·A 的自证。按形状实现成空方法即可。 | §八·A |

### 1.3 prefab 侧实物（这一层唯一的两个挂载点）

| prefab | 根组件 | 序列化字段真值 |
|---|---|---|
| **`HuntMark_IdleEffect`**（`bundle_battleprefabs_vfxandmisc_assets_all/GameObject/HuntMark_IdleEffect.json`，根 `Transform` = `-8415374602843260973`） | `Transform` + **`HuntMarkController`**（`MonoBehaviour_-5880698705900734509.json`）—— **没有 `AnimFXController`** | `counter` → `MonoBehaviour_-2598067149583091757.json`（`TextMeshPro`，`m_text: "1"`、`m_fontSize: 6`、居中、白色、字体 `Pragati-Regular SDF` guid `e257b49ae0e0b27b1eaac5f7f9d95566`） |
| **`MarkerlightIdle`**（`…/GameObject/MarkerlightIdle.json`，根 `Transform` = `4465041863265759825`） | `Transform` + **`MarkerlightController`**（`MonoBehaviour_-3235397510437391791.json`）—— **没有 `AnimFXController`** | `counter`→TMP · `normalStateObject`→`Normal State` · `expiringStateObject`→`Expiring State` · `fadeTime: **0.5**`（⚠️ 不是 ILSpy 默认的 `1f`，**prefab 覆写了**）· `floors[]` = 两个 `ParticleSystemRenderer`（`-7782206327072345519` / `5681904949217997393`，**都是 `Grid floor` 节点**，但**层级很深**，见 §6.2）· `updateMarkerLightVFX` = **空引用 `{0,0}`（死支）** |

> ⚠️ **2026-10-25 补（`N15` 实测回灌，铁律 5）：我们导入的这两个 prefab 根上比原版多一个 `WarpforgeEffectBinder`**
> （guid `d9d66f84463c8544594099fc3457f1f7`，**我们自己导出时加的**：材质重建用）。
> ⇒ **不是差异、不用管**：原版根上是「`Transform` + 控制器」，我们是「`Transform` + `WarpforgeEffectBinder` + 控制器」。
> ⛔ 下个代理别拿它当「我们多做了/原版有而我们没有」去改。

> 🔑 **全库唯一性自证**：`grep -rl` 全部 84 个 bundle 的 `MonoBehaviour/`，按两个脚本的 `m_Script` pathID
> （`-850145008731250946` = `HuntMarkController` · `-3449186136354092259` = `MarkerlightController`）搜 ——
> **各命中 1 个文件**，就是上面那两个。⇒ **这一层在天底下只驱动这 2 个 prefab。**

### 1.4 「3 个调用点」到底是哪 3 个（**这里要澄清一处常见误读**）

| # | 位置 | 性质 |
|---|---|---|
| **①** | `BattleManager.__c__DisplayClass465_0.<ResolveTraitAnim>b__0`（ILSpy `BattleManager.cs:285-297`；反编译 `BattleManager.__c__DisplayClass465_0___ResolveTraitAnim_b__0.c:14-31`） | 🔴 **这不是一个普通调用点，是 `ResolveTraitAnim` 内部的回调包装** —— 它对**每一个**走 `ResolveTraitAnim` 的路径生效（`Start(0)` / `Trigger(3)` / `TriggerOnPlay(6)` / `FromCode(5)` 全都走它）。ILSpy 写出的是 `component.UpdateValue(traitValue)`（`:290`）；反编译是 vtable 槽 `0x198`、参数 = DisplayClass 的 `+0x10`（= `traitValue`）（`.c:22-23`） |
| **②** | `CardScript.PlayTriggerAnim`（ILSpy `CardScript.cs:17159-17200`；反编译 `CardScript__PlayTriggerAnim.c:41-49`） | 它**自己**又写了一遍同样的逻辑（`GetComponent<TraitEffectController>()` → `UpdateValue(traitValue)`），**绕过了** `ResolveTraitAnim`。⚠️ **全 build 只有 1 个调用方**：`PlayerHand.cs:3265` 传的是 `battleAnims.sentinelUnitAnim`（**不是 trait**，`traitValue` 用默认 0）⇒ **实际不产生任何效果** |
| **③** | `BattleCardUI.DisplayStatusAnim` 的 **OnPlay(2) 且「字典里已有」** 那一支（ILSpy `BattleCardUI.cs:1734-1779`，`UpdateValue` 在 `:1765-1778`；反编译 `BattleCardUI__DisplayStatusAnim.c:126-198`，`UpdateValue` 在 `:190-196`） | 🔴 **这才是「刷新」的真正入口**：框**已经存在** ⇒ `localPosition = zero` → `SetActive(true)` → `UpdateValue(GetCurrentTraitValueWithModifiers(trait))`。**不重播、不重建** |

> ⚠️ 顺带一条：`CardScript.ActivateTraitAnim`（`CardScript.cs:16696`，就是 `DisplayStatusAnim` 的薄包装）
> **全 build 0 个调用方**（`grep -rl ActivateTraitAnim decomp_full/` 只命中它自己那个 `.c`）。
> 真正调 `DisplayStatusAnim` 的是 **`CardScript.AddEffect`** 与 **`ActivateTraitsOnSummonOrEnchantment`**（见 §2.3）。

---

## §二 Q2 —— 它管哪些 trait / 多久调一次

### 2.1 管的 trait：**只有 2 条**

| trait | `DefinedTrait` | 哪一条 `TraitEffect` 的 prefab 带控制器 | 触发档 |
|---|---|---|---|
| `huntMark` | **1260**（`DefinedTrait.cs:129`） | `OnPlay(2)` → `HuntMark Idle Anim` → **`HuntMark_IdleEffect`** | `EffectTriggerType.OnPlay` |
| `markerlight` | **1170**（`DefinedTrait.cs:115`） | `OnPlay(2)` → `MarkerlightTraitPlay` → **`MarkerlightIdle`** | `EffectTriggerType.OnPlay` |

出处：`Resources/trait_effect_bindings.json` 的这两族行（已在本仓，逐行带 `effectGuid → cardAnim → prefab` 两跳），
例如 `{"trait":"huntMark","triggerType":2,"cardAnim":"HuntMark Idle Anim","prefab":"HuntMark_IdleEffect"}`；
`"markerlight","triggerType":2,"prefab":"MarkerlightIdle"`。

⚠️ **别的 trait 一律不受这层影响** —— 它们的 prefab 上**没有**这两个组件（§1.3 的全库唯一性）。
所以「补了 `HuntMark`/`Markerlight` 的 `UpdateValue` 也不会被调用」这句话是**对的**，但范围**仅限于这 2 条**。

### 2.2 `UpdateValue` 是谁的方法、多久调一次

- **是谁的方法**：`TraitEffectController` 的**抽象方法**（`:13`），两个子类各自实现。
- **调用频率 = 事件驱动，绝不是每帧。** 精确地说，是「**这个 trait 的框被显示 / 被再次显示**」时各一次：

| 语义 | 原版路径 | 次数 |
|---|---|---|
| **首次显示**（新框实例化） | `DisplayStatusAnim` → `ResolveTraitAnim` → `CreateAnim` 里 `Instantiate` 之后**立刻** invoke 回调（回调里 `UpdateValue(traitValue)`） | 1 次 / 每次实例化 |
| **再次显示**（框已在字典里） | `DisplayStatusAnim` 的 `TriggerType==2` 且 `TraitFrameEffectsPlay` 已含该 trait 那一支 | 1 次 / 每次 `DisplayStatusAnim` 调用 |
| **触发显示** | `DisplayTriggerAnim` → `ResolveTraitAnim(… traitValue …)` | 1 次 / 每条 `TriggerType∈{3,6}` 的 effect |
| **FromCode 显示** | `ActivateTraitAnim` / `ActivateTraitParticlesFromCode*` → `ResolveTraitAnim` | 1 次 / 每次（但那 2 条 trait 的 FromCode prefab 不带控制器 ⇒ 空转） |
| `PlayTriggerAnim` | 唯一调用方传的是 `sentinelUnitAnim` | 实测无效 |

### 2.3 什么时候会「再次显示」—— 这是刷新的**真实触发源**

| 触发源 | 原版判据 | 说明 |
|---|---|---|
| **召唤 / 附魔** | `CardScript__ActivateTraitsOnSummonOrEnchantment.c:42-69`：遍历 `card+0x128`（= 该卡的 `List<CardTraitData> CurrentTraits`），**逐个** `DisplayStatusAnim(trait, playSound)` | 一次进场把身上所有 trait 的框各演一遍 |
| **之后每一次「trait 被加上」** | `CardScript__AddEffect.c:645-664`：`if (cardState ∈ {2,0xf,3,0x11})` ∧ `CardTraitCollection.GetTrait(trait) != null` ∧ **`CardTrait._hasInPlayAnim`（`+0x20`）** ⇒ `DisplayStatusAnim(trait, 1)` | 🔑 **`_hasInPlayAnim` 就是那道闸**；猎杀标记叠加后走的正是这里 |
| 具体两条 | `huntMark`（0x4ec）：`AddEffect.c:639-644` 先 `DisplayTriggerAnim(0x4ec)`（播 `HuntMark_StartEffect`）+ `BroadcastHuntMarkResolved`；随后 `:645-664` 再 `DisplayStatusAnim(0x4ec)`（**刷新 Idle 框的计数器**） | 两件事：**演一次「获得」** + **把数字刷成新的总数** |
| | `markerlight`（0x492）：`AddEffect.c:635-638` `BroadcastMarkerlightResolved` | 计数器刷新同样走 `:645-664` |

> ✅ **结论**：`UpdateValue` 的语义 = **「这条 trait 的总值现在是几，写上去」**，
> 触发时机 = **「框刚出生」**、**「框被再次显示（= trait 又被加了一层）」**、**「触发了」**。
> ⛔ **不是**「值一变就刷」的通用观察者 —— 原版没有那种机制。

---

## §三 Q3 —— 输入与输出

### 3.1 输入：一个 `int`，语义是**当前 trait 总值（含 modifier，钳 ≥0）**

```
EntityScript.GetCurrentTraitValueWithModifiers(trait)        // EntityScript__GetCurrentTraitValueWithModifiers.c:1-75
    =  Σ( modifier 条目中 (kind==3 ∧ traitId==trait) 的 +0x40 )
       + List<CardTraitData>(+0x128) 里该 trait 的 baseValue(+0x1c)
    → if (base + mods) < 0 → return 0
```

调用点三处（`DisplayStatusAnim.c:112` / `:190`、`DisplayTriggerAnim.c:98`）用的都是它。
⇒ **`huntMark` 的「几层」= 这个数**；`markerlight` 同理。

### 3.2 输出：**只有 UI**，一行代码看清

```c
// HuntMarkController__UpdateValue.c:13-30
plVar2 = *(longlong **)(param_1 + 0x28);       // counter（TMP）
counter.text = newValue.ToString();            // :17  (0x558 = TMP.set_text)
DOPunchScale(counter.transform,
             /*punch    */ Vector3.one * 0.1f, // :24-28  （Unity 静态 Vector3 表：+0x00=zero 被 DisplayStatusAnim 用作 localPosition；+0x0C=one）
             /*duration */ 0.5f,               // :29  DAT_1834b2bb4
             /*vibrato  */ 10,                 // :29  字面量
             /*elasticity*/ 1.0f);             // :29  DAT_1834b2bb8
```

`MarkerlightController.UpdateValue`（`MarkerlightController__UpdateValue.c:54-79`）与它**前 3 行完全相同**，
之后多一段：`if (updateMarkerLightVFX) { Instantiate(它, 自身 position, identity)… }`
—— ⚠️ **`updateMarkerLightVFX` 在本 build 的 prefab 里是空引用**（`MonoBehaviour_-3235397510437391791.json:33-36`），
⇒ **该分支是死支**（铁律 11 例外①：原版数据本身为空），⛔ 不用做，但要在代码里留注释说明「判据上是空引用」。

**`MarkerlightController.Start()` 的额外输出**（`:25-48`）：

```
alreadyAttacked = false;
normalStateObject.SetActive(true);
expiringStateObject.SetActive(false);
foreach (r in floors) r.GetMaterial().SetFloat("_RippleScale", transform.lossyScale.x);
```

⚠️ **这一步是必需的**：`MarkerlightIdle` prefab 里 **`Normal State` 与 `Expiring State` 两个节点序列化时都是 active=1**
（原版 `GameObject/Normal State.json` / `Expiring State.json` 的 `m_IsActive: True` 实测，我们导入的 prefab 也一致）
⇒ **没有 `Start`，两层会叠着画**（今天我们的 `MarkerlightIdle` 就是这个状态，见 §四·4.3）。

#### 3.2·b 🔴 `Renderer.GetMaterial()` 到底是 `material` 还是 `sharedMaterial`（2026-10-25 补查）

**结论：是 `renderer.material` —— 会为这个 renderer 实例化一份材质副本。⛔ 不是 `sharedMaterial`。**

判据（三个来源互相印证）：

1. **ILSpy 原文**：`MarkerlightController.cs:41` `Material material = array[obj].GetMaterial();`，
   而同一个文件里 `MaterialFader` 那族**同时**出现 `GetSharedMaterial()` 与 `GetMaterial()` 两种写法
   （`MaterialChangerByQuality.cs:33` `obj == null ? GetSharedMaterial() : GetMaterial()`）
   ⇒ 这是**两个不同的 API**，`GetMaterial` 不是 `sharedMaterial` 的别名。
2. **反编译调用符号**：`MarkerlightController__Start.c:41` `lVar3 = UnityEngine_Renderer__GetMaterial(lVar3,0);`
   —— 是 `UnityEngine.Renderer` 上的方法，**不是**某个 helper 的扩展方法（扩展方法在 IL2CPP 里会落成 `<定义类>__GetMaterial`）。
3. 🥇 **`dump.cs` 的 native 绑定（决定性）**：
   ```
   // D:/2/tools/il2cpp_out/dump.cs:526578-526581（Renderer 的属性）
   public Material[] materials { get; set; }
   public Material material { get; set; }        // ← 这一对的 getter 就是下面那个 native
   public Material sharedMaterial { get; set; }
   public Material[] sharedMaterials { get; set; }
   // :526597-526599
   [FreeFunction(Name = "RendererScripting::GetMaterial", HasExplicitThis = True)]
   private Material GetMaterial() { }
   // :526601-526603
   [FreeFunction(Name = "RendererScripting::GetSharedMaterial", HasExplicitThis = True)]
   private Material GetSharedMaterial() { }
   ```
   ⇒ `GetMaterial()` = **`material` 属性的原生 getter**（Unity 的 `material { get => GetMaterial(); set => SetMaterial(v); }`），
   `sharedMaterial` 走的是**另一个** native 入口。
   （`dump.cs` 标 `private` 是因为那对是属性背后的 native 方法；`MarkerlightController` 能调它 = IL2CPP 里那条调用被直接解析到了同一个符号。）

**为什么这条重要（⛔ 别"优化"成 `sharedMaterial`）**：
- 两个 `Grid floor` 用的是**两张不同的材质**（`m_Materials` = `6669896079300163764` / `-2770377422974436246`，`ParticleSystemRenderer_-7782….json` / `…5681….json` 实测），
  但它们仍然是**资产级共享材质** —— 同一 prefab 的多个实例（多个卡各一个 `MarkerlightIdle`）共用同一份
  ⇒ 用 `sharedMaterial` 的话，**后创建的卡会把 `_RippleScale` 写死到所有卡上**（A 卡的缩放决定 B 卡的涟漪），
  而且会污染同材质的其它效果。**用 `material` 才是每个实例独立的、也才是原版的行为。**
- 代价（**原版同形，照抄**）：`renderer.material` 产生的副本**不会**随 GameObject 销毁自动回收
  （Unity 明确说「It is your responsibility to destroy the materials」）。
  ⇒ 每张卡每帧多 2 个材质实例，随卡一起「飘」到 `UnloadUnusedAssets`。**这是原版的行为，⛔ 不要为了"干净"改成 `sharedMaterial`**；
  真要收，只能在 `OnDestroy` 里 `Destroy` 那 2 个 —— 那属于**我们的口径**，必须在注释里写明（铁律 3）。
- ✅ **附带确认**：`_RippleScale` 这个属性名在我们这边是**认得**的 ——
  `数据/游戏数据/original_shader_props.tsv` 与 `WarpforgeVFX/Runtime/OriginalShaderBlendTable.cs` 里都有它
  ⇒ `SetFloat("_RippleScale", …)` 不会静默变成 no-op（⚠️ 但仍要按 §6.4 的 A-ML 断言真读一次值，别信"属性存在"）。

### 3.3 它**不**做什么（防止过度设计）

- ⛔ 不写引擎状态、不改 trait 值、不重算光环；
- ⛔ 不发事件、不触发其他卡的能力；
- ⛔ 不管「框什么时候该消失」（那是 `CleanStatusAnims` 的事）；
- ⛔ 不管「哪张框配哪个 prefab」（那是 `TraitFrames` / `trait_effect_bindings.json` 的事）。

---

## §四 Q4 —— 我们的现状

### 4.1 全仓 0 命中（结论）

```
grep -rn "TraitEffectController"  MyGame/Assets/**/*.cs   → 0
grep -rn "HuntMarkController|MarkerlightController" …      → 0
```
（`CardPresentation/Core/TraitParticles.cs` / `Badges.cs` / `TraitFrames.cs` 里出现 `huntMark` 的地方都是**关键词名**，不是这个类。）

### 4.2 已有的**部分等价物**（逐条判「等价 / 部分 / 不同」）

| 我们的东西 | 位置 | 与原版这一层的关系 | 判定 |
|---|---|---|---|
| **`TraitFrames`（两本字典 + 4 个方法）** | `CardPresentation/Core/TraitFrames.cs`（977 行） | 它**就是**原版 `BattleCardUI.DisplayStatusAnim / DisplayTriggerAnim / RemoveStatusAnim / CleanStatusAnims` + `TraitFrameEffectsPlay/Trigger` 那两本字典的复刻。**它负责「实例化哪张框」**，本层负责「实例化出来之后往计数器上写数」 | **不同的两件事，但共用同一个实例** |
| `TraitFrames.SyncKeywords` 的「新出现」分支 | `TraitFrames.cs:756-771`（`if (_prev.Contains(k)) continue;`） | 对应原版 `ActivateTraitsOnSummonOrEnchantment` → `DisplayStatusAnim`（首次显示） | **等价（首次那一半）** |
| `TraitFrames.DisplayStatus` 的「已在字典 ⇒ SetActive(true)」 | `TraitFrames.cs:799-808` | 对应原版 `DisplayStatusAnim.c:126-198` 那一支 —— 🔴 **但原版在这一支里还要 `UpdateValue`，我们这一支只做了 `SetActive`** | **部分**（缺的正是本层） |
| `TraitFrames.PlayStartRows` / `DisplayTrigger` | `:831-859` / `:883-911` | 对应 `ResolveTraitAnim` 的两个上游 | **部分**（没把 trait 值传下去） |
| `Badges` 的关键词角标数字 | `CardPresentation/Core/Badges.cs`（`CarriesValue` / `InPlayIconKeys`） | 那是**卡面左侧徽标**上的角标。`huntMark` / `markerlight` 的 `hasIcon == false`（`Badges.cs:395` 明写）⇒ **它俩根本不上徽标 ⇒ 没有角标数字** | **不同**（不是这个计数器的替代品） |
| `Auras.Recompose` | `RuleEngine/Core/Aura.cs`（9 个棋盘写入点 + `BeginTurn`） | 引擎侧的**光环重算**，产出的是关键词/值 | **不同的层**（它是「产出值的上游」） |
| `TraitParticles`（FromCode 粒子） | `CardPresentation/Core/TraitParticles.cs` | 原版 `ActivateTraitParticlesFromCode` 那条链，**不带计数器**（`ResolveTraitAnim` 的 callback 传 null） | **不同的一条链**（⛔ 别合并） |

### 4.3 现在**真实可见**的两个缺陷（不装本层就会一直在）

1. **`huntMark` / `markerlight` 没有数字、也不弹** —— 框在播（`TraitFrames` 已绑 OnPlay 框），但 prefab 上那个
   `Counter` 子节点**永远显示 prefab 里写死的 `"1"`**（`m_text: 1`，`HuntMark_IdleEffect.prefab` 里实测），
   叠到 3 层还是 `1`。
2. **`MarkerlightIdle` 的两层状态叠着画** —— 因为 `Start()` 没人做（§3.2）。今天它只活 4 秒就自杀（见 §七 F1），
   但这 4 秒是**错的画面**。

### 4.4 差在哪（一句话）

> **接收端全在，发送端全无**：
> `TraitFrames` 已经把「哪张框、什么时候演」做完了，缺的只是
> **① prefab 上那个组件（`UpdateValue` 的实现）** 与
> **② 4 个时机点把 `KwValue(trait)` 传给它**，
> 外加 **③ 两个前置修**（idle 框不许 4 秒自杀 · 值要能传到 `TraitFrames`）。

---

## §五 Q5 —— 与已有机制的关系（重叠还是互补？）

| 机制 | 它回答的问题 | 与本层的关系 | 会不会打架 |
|---|---|---|---|
| **`Auras.Recompose`**（`RuleEngine/Core/Aura.cs`；9 个棋盘写入点 + `BeginTurn` 各挂一次） | 「**连续光环**现在该给谁加多少」 | **上游**：它改的是 `UnitState.Keywords` 的值 | ⛔ **不会**：它只碰引擎状态，不知道也不该知道 UI。它的结果会在下一次 `SyncBoard` 里被本层读到 |
| **`ActivateTraitsOnSummonOrEnchantment`**（`RuleCore.cs:3483`；我们只做了 `.c:83-99` 那两句**规则**部分，`.c:67` 的**显示**部分没做 —— `Enchantment.cs:183` 已如实标着） | 「进场那一刻，身上每个 trait 都要演一次框」 | **同一时刻的两个动作**：它演框（我们已由「新视图 ⇒ `_prev` 空 ⇒ 全集重演」等价实现），本层写数字 | ⛔ **不会**：两者都在**同一个 `DisplayStatus` 入口**里，顺序 = 先播框、再写数字 |
| **`WhenEvent` / `EvtKind`**（`RuleEngine/Core/WhenEvent.cs`；`EvtKind.Trigger` / `EvtKind.StatusApplied` …） | 「**引擎里发生了什么**」 | **互补的两条通道**：`EvtKind.Trigger` → `TraitFrames.DisplayTrigger` + `TraitParticles`（= 原版 `DisplayTriggerAnim` / FromCode）；**本层要的「trait 又被加了一层」= `EvtKind.StatusApplied`**（`EffectResolver.cs:11468-11471` 已经在发，`hadKw` 告诉我们是不是首次） | ⛔ **不会**：`StatusApplied` 目前**没有**任何显示侧消费者（现读全仓只做 `Tally`/成就），本层是它的第一个消费者 |
| **`TraitFrames` 两本字典** | 「哪张框、什么时候该在、什么时候该撤」 | **同一实例的唯一持有者** | ⚠️ **唯一要守的纪律**：**只许 `TraitFrames` 实例化框**（⛔ 别在 `BattleDriver` 里再 `Play` 一次 idle 框 —— 那会两套框互相抢 `SetActive`） |

### 5.1 「合并还是并存」的裁定

- **并存，但只有一个实例化者。**
  - `TraitFrames` 继续负责 **实例化 / 去重 / 清理**（已有的两本字典 + 4 个方法，⛔ 不推翻）。
  - 本层是**挂在那个实例上的组件**（`AddComponent`），生命周期跟着 `TraitFrames` 走。
  - **不许**新开一条「按 trait 名找 prefab」的路由 —— 绑定表只有 `trait_effect_bindings.json` 一份（铁律：两处写同一条规则 = 迟早不一致）。
- **不合并进 `TraitFrames` 类本身**：原版是**独立组件**（`TraitEffectController`），
  拆出去的收益 = 两个 prefab 各自的行为（尤其是 `Markerlight` 那段 `Start`/`MaterialFader`）不必污染 `TraitFrames`；
  而且我们的 class-文件名同名规则（CLAUDE.md A1506/A1508）要求一个类一个文件，塞进去反而更难维护。

---

## §六 Q6 —— 施工方案

### 6.0 前置：两条**必须先修**的（否则做完也看不见）

| 编号 | 问题 | 判据 | 修法 | 改哪个文件 |
|---|---|---|---|---|
| **P0-1** | 🔴 `HuntMark_IdleEffect` / `MarkerlightIdle` 在我们这边**4 秒后自毁** | 两个 prefab **没有 `AnimFXController`**（§1.3）⇒ 效果库给的是 `destroyTime: -1, natural: 4.3 / 4`（`Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` 实测）；`WarpforgeEffectLibrary.AutoLifetime()`（`:75-81`）在 `destroyTime < 0` 时**回落到 `natural`** ⇒ `_lifetime = 4.3 / 4 s`。原版这两件**没有计时销毁**，只由 `CleanStatusAnims` 的 `Destroy` 收 | `TraitFrames` 里**「要长期持有」的那一档**（`_play` 字典存的那张 OnPlay 框）调 `Play` 时显式传 `autoDestroySeconds: float.PositiveInfinity`。⚠️ Start(0) 与 Trigger(3/6) 那两档**保持** `AutoLifetime`（原版它们本来就该自己消失） | `CardPresentation/Core/TraitFrames.cs`（`Play` / `DisplayStatus`） |
| **P0-2** | `TraitFrames` **看不见 trait 的值**（只有 key 集合） | `SyncKeywords(IEnumerable<string> current)`（`:756`）；唯一调用点 `BattleDriver.cs:25396` 传的是 `u.Keywords.Keys` | 加一个重载 `SyncKeywords(IEnumerable<string> current, System.Func<string,int> valueOf)`；旧签名保留（转发 `valueOf = null`）。⚠️ **别把值塞进 `CardData`** —— 那会和 `CardData` 的「卡面数据」口径混成一锅 | `TraitFrames.cs` + `BattleDriver.cs`（1 行） |

### 6.1 新增文件（3 个 · 每个类一个文件 —— CLAUDE.md A1506/A1508 硬规则）

放在 **`MyGame/Assets/CardPresentation/Core/`**（与 `TraitFrames.cs` 同目录：本层只服务它）。

**① `TraitEffectController.cs`**（形状照抄，含注释标出处）

```csharp
// TraitEffectController.cs — 原版 `TraitEffectController`（d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TraitEffectController.cs）
// 判据 → 资料/完全复刻/判据/TraitEffectController_钩子层.md
//   · 3 个抽象方法 `Toggle` / `UpdateValue` / `AimingAttack`（:11/:13/:15）
//   · `currentValue`（:5）在原版本 build **无读无写**（死字段）⇒ 保留形状、⛔ 不当状态用
//   · `Start()` 是**空**的虚方法（:7-9）—— 子类 `MarkerlightController` 覆写了它
namespace CardPresentation
{
    public abstract class TraitEffectController : MonoBehaviour
    {
        protected int currentValue;                  // 原版 :5（死字段，保留形状）
        protected virtual void Start() { }           // 原版 :7-9
        public abstract void Toggle(bool option);    // 原版 :11 —— 本 build 0 调用点（判据 §八·A）
        public abstract void UpdateValue(int newValue);   // 原版 :13 —— 本层的全部意义
        public abstract void AimingAttack(bool option);   // 原版 :15
    }
}
```

**② `HuntMarkController.cs`**

```csharp
// 原版 HuntMarkController.cs:1-38；方法体判据 HuntMarkController__UpdateValue.c:13-30
//   counter.text = newValue.ToString();
//   DOPunchScale(counter.transform, Vector3.one * 0.1f, 0.5f, 10, 1.0f)
//   ⚠️ 常量：0.1 = DAT_1834b2dc4 · 0.5 = DAT_1834b2bb4 · 1.0 = DAT_1834b2bb8（.rdata 实读）
//   ⚠️ punch 向量 = UnityEngine 静态 Vector3 表的 +0x0C 项 = `Vector3.one`（+0x00 是 zero，
//      DisplayStatusAnim 用它当 localPosition）⇒ 0.1*(1,1,1)
//   ⚠️ `AimingAttack` 原版**直接 throw NotImplementedException**（:31-37）—— 照抄（它不该被调）
//   ⚠️ `Start` 是**空覆写**（:11-13）；`Toggle` 是**空体**（:15-17）
public class HuntMarkController : TraitEffectController
{
    // ⚠️ 2026-10-25 更正（`N15` 实测回灌，铁律 5）：原版这个字段是 **`private`**（`HuntMarkController.cs:9`
    //    `private TextMeshPro counter;`），但它**确实被序列化**（prefab 的 MonoBehaviour JSON 里有 `"counter": {…}`）
    //    ⇒ 正确形状 = `[SerializeField] private`，⛔ **不是 `public`**。
    //    IL2CPP 反编译产物里看不到 `[SerializeField]`（特性被剥），只能靠「字段是 private ∧ prefab 里有它」反推。
    //    ⚠️ 我们是**运行期 AddComponent**，序列化字段恒为空 ⇒ 必须在 `Setup()/Start()` 里按名字绑定
    //      （`transform.Find("Counter")` 能拿到，见 §6.2），**空就必须出声**（⛔ 不许静默）。
    //    ⚠️ 自检**不许直接读私有字段** ⇒ 加只读观测量（`TextForTest` / `PunchCountForTest`）。
    [SerializeField] private TextMeshPro counter;
    // UpdateValue / Toggle / AimingAttack / Start 见上
}
```

**③ `MarkerlightController.cs`**（字段 8 个 + 5 个方法）

```
counter · normalStateObject · materialFaderNormalState · expiringStateObject ·
materialFaderExpiringState · fadeTime(=0.5，prefab 覆写) · updateMarkerLightVFX(=null，死支) ·
floors[]（两个 "Grid floor" 的 Renderer）· alreadyAttacked
Start()          :25-48    ← 必需（关 Expiring、给 floors 设 _RippleScale）
UpdateValue()    :54-79    ← counter + punch（同 HuntMark）
MarkerLightUsed():88-93    ← **直接三句**：alreadyAttacked = true；Normal off；Expiring on
AimingAttack()   :81-86    ← materialFader(alreadyAttacked ? expiring : normal).Toggle(!option, fadeTime)
Toggle()         :50-52    ← 空体
```

> ⚠️ **2026-10-25 更正（`N15` 实测回灌，铁律 5）—— 上一版骨架在这里写错了一条**：
> 原来写「`MarkerLightUsed` / `AimingAttack` **都走 `ChangeState`**」。**不成立**：
> · `MarkerLightUsed` 是**直接三句**，逐句见 `MarkerlightController__MarkerLightUsed.c:5-12`：
>   `*(undefined1 *)(this+0x68) = 1;` → `GameObject.SetActive(normalStateObject(+0x30), 0)` → `GameObject.SetActive(expiringStateObject(+0x40), 1)`；
> · 而 `ChangeState(bool)`（`MarkerlightController.cs:95-101`）与 `FadeFixedElements(bool, MaterialFader)`（`:103-106`）**类内零调用、全 build 零调用**（两个都是 `private`；`grep -rn "ChangeState|FadeFixedElements" d:/2/tools/decomp_full/` 对本类**0 命中**，ILSpy 里也只有定义行 95 / 103）
>   ⇒ **它们是死代码**（铁律 11 例外①：原版本身没调用点）。
> `ChangeState` 那三句与 `MarkerLightUsed` 的语义**重复**（就是「按 alreadyAttacked 切两个 state」），
> 但**原版真正被执行的是 `MarkerLightUsed` 那份** ⇒ 实现时**照 `MarkerLightUsed` 写**，
> `ChangeState` / `FadeFixedElements` **保留成私有方法、留注释标死**（保形状，⛔ 别让 `MarkerLightUsed` 改成转发给它 —— 那就和原版的执行路径不一致了）。

⚠️ **`AimingAttack` / `MarkerLightUsed` 依赖一个我们**没有**的共用件 `MaterialFader`**
（原版 `MaterialFader`：`MaterialFader.Toggle(bool,float)` 缓存原材质 → 全 renderer `enabled=true` → 设目标 alpha 与时长，
`Update` 里自己插值；判据 `MaterialFader__Toggle.c:11-77`）。⇒ **这两条排 `P2`**，见 §6.6。

### 6.2 挂载：**在哪把组件加到实例上**

**唯一入口 = `TraitFrames.Play(prefabName)`**（`:964-975`，所有框都从这里出生）。

```csharp
WarpforgeEffectPlayer Play(string prefabName, bool persistent = false)
{
    ...
    var p = WarpforgeEffectPlayer.Play(prefabName, _anchor, Vector3.zero, 1f,
                                       persistent ? float.PositiveInfinity : -1f);   // ← P0-1
    TraitEffectControllers.Attach(p != null ? p.gameObject : null, prefabName);      // ← 本层
    return p;
}
```

```csharp
static class TraitEffectControllers   // 可与 ③ 同文件（不是 MonoBehaviour，不受「类名=文件名」约束）
{
    // 全 build 只有这 2 个 prefab 带控制器（判据 §1.3：84 bundle 扫 m_Script，各 1 实例）
    public static TraitEffectController Attach(GameObject go, string prefabName)
    {
        if (go == null || string.IsNullOrEmpty(prefabName)) return null;
        var exist = go.GetComponent<TraitEffectController>();
        if (exist != null) return exist;               // 万一将来重导出把组件带进来 ⇒ 别重复加
        switch (prefabName)
        {
            case "HuntMark_IdleEffect":  return go.AddComponent<HuntMarkController>();
            case "MarkerlightIdle":      return go.AddComponent<MarkerlightController>();
        }
        return null;
    }
}
```

**绑定靠名字还是靠路径？** —— 三类节点**不一样**，必须分开说（已按 prefab YAML 的 `m_Father` 链逐级算过）：

| 字段 | 实测层级（子 → 根） | 能不能 `transform.Find` |
|---|---|---|
| `counter`（两个 prefab） | `['Counter', 'HuntMark_IdleEffect']` / `['Counter', 'MarkerlightIdle']` | ✅ **能**（直接子件） |
| `normalStateObject` / `expiringStateObject` | `['Normal State', 'MarkerlightIdle']` / `['Expiring State', 'MarkerlightIdle']` | ✅ **能**（直接子件） |
| 🔴 `floors[]`（两个 `Grid floor`） | `['Grid floor', 'Base Vfx', 'Fixed', 'Expiring State', 'MarkerlightIdle']`<br>`['Grid floor', 'Base VFX', 'Fixed', 'Normal State', 'MarkerlightIdle']` | ❌ **不能** —— **嵌了 4 层深** |

> ⚠️⚠️ **2026-10-25 就地更正（`N15` 建文件时实测报回、我这里复核确认；铁律 5）**：
> 本文件上一版在这里写的是「`Counter` / `Normal State` / `Expiring State` / **两个 `Grid floor`** 都是根的直接子件」
> —— **「两个 `Grid floor` 也是直接子件」这句是错的**，错因 = 我只对前三个真算了 `m_Father` 链，
> `Grid floor` 那一路是**顺手外推**的（没验）。实测（脚本按 prefab YAML 逐级上溯，两个 prefab 各跑一遍）：
> `Grid floor` 分别在 **`Expiring State/Base Vfx/Fixed/`** 与 **`Normal State/Base VFX/Fixed/`** 下。
> ⚠️ 顺带注意到一处**大小写不一致**：Expiring 那支叫 **`Base Vfx`**，Normal 那支叫 **`Base VFX`** ——
> **⛔ 别拿路径字符串去按名字找**（一改大小写就 null）。
>
> ⇒ **`floors` 的正确取法**（`N15` 已采用，本判据采纳）：
> ```csharp
> // 原版 `floors` 是长度 2 的 Renderer[]（`MarkerlightController.cs:21`）——
> //   数组里就是两个 "Grid floor" 的 ParticleSystemRenderer（pathID -7782206327072345519 / 5681904949217997393，
> //   两个 GameObject 分别叫 `Grid floor_8925490275536089681` / `Grid floor_3521225978504491601`）。
> // 我们导入的 prefab 里**恰好 2 个**节点名是 "Grid floor"（全树扫过，没有第 3 个）
> // ⇒ 用「子件全扫 + 名字过滤」，⛔ 绝不用 `transform.Find("Grid floor")`（恒 null，且是**静默**的）。
> var floors = new List<Renderer>();
> foreach (var psr in GetComponentsInChildren<ParticleSystemRenderer>(true))
>     if (psr.gameObject.name == "Grid floor") floors.Add(psr);
> if (floors.Count != 2) Debug.LogWarning($"[MarkerlightController] floors 期望 2 个 'Grid floor'，实得 {floors.Count} —— "
>                                       + "prefab 结构变了？判据见 判据/TraitEffectController_钩子层.md §6.2");
> ```
> 🔴 **为什么这条最危险**：`Find` 拿不到时是 `null`，而 `foreach (null)` 会**直接抛 NRE 或者被静默跳过**；
> 即便跳过不崩，**`_RippleScale` 就永远不设** —— 画面只差一点，**断言里不断数值就完全看不见**（CLAUDE.md §三「不许静默失败」）。
> ⇒ §6.4 的 **A-ML 断言必须真读材质里的 `_RippleScale`**，不能只断「组件挂上了」。
>
> ⚠️ 拿不到就必须**出声**（上例的 `LogWarning`）；`counter` / 两个 state 节点将来重导出若变了层级，
> 同样按上面那张表先查、查不到就报。


### 6.3 四个入口：每个入口都必须显式传值（铁律 10 第 5 条）

| 编号 | 入口（我们的代码） | 原版对应 | 传什么值 | 动作 |
|---|---|---|---|---|
| **E1** | `TraitFrames.DisplayStatus` 的「**首次实例化** OnPlay 框」分支（`:819-822`） | `ResolveTraitAnim` 回调（`DisplayClass465_0.b__0`） | `valueOf(key)` | `Play(...) → ctl?.UpdateValue(v)` |
| **E2** | `TraitFrames.DisplayStatus` 的「**已在字典**」分支（`:799-808`） | `DisplayStatusAnim.c:126-198`（**③ 号调用点**） | `valueOf(key)`（**当前值**） | `localPosition=zero` → `SetActive(true)` → `ctl?.UpdateValue(v)` |
| **E3** | `TraitFrames.PlayStartRows`（`:845-857`） | `DisplayStatusAnim.c:86-119`（`ResolveTraitAnim` 带 `traitValue`） | `valueOf(key)` | 每次 `Play` 后 `ctl?.UpdateValue(v)` |
| **E4** | `TraitFrames.DisplayTrigger`（`:902-909`） | `DisplayTriggerAnim.c:97-104`（`ResolveTraitAnim` 带 `traitValue`） | `valueOf(key)` | 每条 `Play` 后 `ctl?.UpdateValue(v)` |

**E2 的触发源（关键）**：`DisplayStatus` 现在**只在新关键词出现时**被调（`SyncKeywords` 的 `if (_prev.Contains(k)) continue;`）
⇒ **第二次猎杀标记不会走到 E2**。原版的「再次显示」由 `AddEffect` 触发 ⇒ 我们必须补一条：

```
BattleDriver：EvtKind.StatusApplied(关键词, UnitState u)
    → 若 keyword ∈ {huntmark, markerlight} 且 Amount>0
    → 取【已有】的 TraitFrames（⛔ 不要 Attach 新的 —— 新实例 _prev 空，会把全部 OnPlay 重演一遍）
    → tf.ReplayAndRefresh(keyword, u.KwValue(keyword))     // 新方法 = E2 那一支的完整语义
```

> ⚠️ **为什么不用「每次 `SyncBoard` 都比对值」**：`UpdateValue` 里有 `DOPunchScale` ——
> 每帧刷会让计数器**一直抖**。原版是「trait 被加上了」才刷，**不是**「值变了就刷」。
> `EvtKind.StatusApplied` 正是我们已有的「trait 被加上了」事件（`EffectResolver.cs:11468-11471`，带 `hadKw`）。
>
> ⚠️ **同时**保留一个**值变化兜底**（`_prevValue` 字典，只在值真的变了时才刷）——
> 因为原版还有「非 AddEffect 路径导致的数值变化」（例：`GetCurrentTraitValueWithModifiers` 里的 modifier 变了）。
> 兜底闸必须与 `StatusApplied` **同一把锁**（同一帧内只刷一次），否则会双弹。

### 6.4 断言（每个入口 1 条 + 2 条灭自证）

| # | 断言 | 放在哪 |
|---|---|---|
| **A-E1** | 场上新建一张带 `huntMark 2` 的卡 ⇒ `_play["huntmark"]` 上的 `HuntMarkController.TextForTest == "2"` 且 `PunchCount == 1` | `BattleScene`（真机脚本侧）/ `TraitFrames` 自检 |
| **A-E2** | 同一张卡再 +1 ⇒ 文本 `"3"`、`PunchCount == 2`、**`InstanceId` 不变**（没重建框）、`PlayCount` **不增** | 同上 |
| **A-E3** | `StartPlayCount` 增 1 的同时 `UpdateCalls` 增 1（即使该 prefab 上没有控制器，**调用必须发生**） | 同上 |
| **A-E4** | `DisplayTrigger("huntmark")` 后 `UpdateCalls` 增 1，且 `_triggerPlays` 行为不变 | 同上 |
| **A-ML** | `MarkerlightIdle` 实例化后**下一帧**：`Expiring State.activeSelf == false` ∧ `Normal State.activeSelf == true` ∧ **两个 `Grid floor` 的 `renderer.material.GetFloat("_RippleScale") == 根 lossyScale.x`（真读数值，不是只断"组件挂上了"；取值方式见 §3.2·b + §6.2 的 `floors` 取法） | 同上 |
| **A-LIFE** | idle 框实例化 **>6 秒**后仍 `!= null`（挡 `AutoLifetime` 的 4.3 s 自杀） | 同上（可用 `WarpforgeEffectPlayer.ActivePlayers` 或直接持有引用） |
| 🔴 **灭自证 1** | **同一帧内把 `SyncBoard`/`SyncKeywords` 连跑 3 次（值不变）⇒ `PunchCount` 必须仍为 1**。这条挡「每次 sync 都无脑刷新」那个**看着对、实际每帧抖**的实现 | 同上 |
| 🔴 **灭自证 2** | **`HuntMarkController` 只能出现在 `HuntMark_IdleEffect` 上**：对 `MarkerlightIdle` 与 `HuntMark_StartEffect` / `MarkerlightProcEffect` 各断 `GetComponent<TraitEffectController>() == null`。这条挡「按 trait 名而不是按 prefab 名挂」的错实现 | 同上 |

> 断言口径警告（CLAUDE.md §三）：**盯原版参数，不盯我们的常量**。
> 上面每条的期望值都能回溯到 §一/§三 的 `文件:行号`，⛔ 不许写成「等于我们代码里那个常量」。

### 6.5 观测量（不跑 Unity 也能查的）

给 `TraitFrames` 加四个计数器（只读，自检用）：

```
UpdateCalls        // 累计调用 UpdateValue 的次数（含「实例上没控制器」的那些 ⇒ 证「调用发生了」）
PunchCount         // 实际弹了几次（HuntMarkController 内自增）
LastValue          // 最近一次写进去的数（HuntMarkController / MarkerlightController 各一个）
AttachedCount      // 挂上了几个控制器（应恒等于「播过的 idle 框实例数」）
```

### 6.6 `P2`：`markerlight` 的两条**附加行为**（同一组件、另一批判据）

| 行为 | 原版判据 | 我们的落点 | 额外前置 |
|---|---|---|---|
| **`MarkerLightUsed()`**（Normal→Expiring） | `CardScript.cs:11260-11296`（`SetRemoveMarkerlight`）：置 `removeMarkerlightAtEndOfTurn = true`（`+0x57`，`EntityScript__get/set_removeMarkerlightAtEndOfTurn.c`）+ 对两本字典各取一次 `GetComponent<MarkerlightController>().MarkerLightUsed()`。调用方 = `BattleManager._ResolveAttack_d__438__MoveNext.c:1633`（标记光伤害那一段跑完之后） | 引擎侧**已经等价**（`RuleCore.cs:6877-6888` 打伤害后 `RemoveAll("markerlight")`）。表现侧要接：**在 `EvtKind` 上补一条「markerlight 被打掉」的事件**（现在没有）→ `tf.MarkerLightUsed("markerlight")` → 组件切 Expiring（= **直接三句**，见 §6.1③） | 无（`MarkerLightUsed` 只 `SetActive`，不需要 `MaterialFader`） |
| **`AimingAttack()`**（攻击前后淡入/淡出） | `CardScript.cs:11298-11333`（`SetTraitsOnPlayAttackingStatus`）；调用方 `CardScript._PrepareAttack_d__353__MoveNext.c:41`（=0）`:65`（=1） | 同上（需要「这张卡开始攻击 / 攻击结束」两条事件） | 🔴 **要新写 `MaterialFader`**（我们没有；`MaterialFader__Toggle.c:11-77` / `Update` / `CacheOriginalMaterials` / `RestoreOriginalMaterials`）。**这是新共用件，属于另一件活** |

🔴 **顺手发现 F2（引擎侧）**：原版 `SetRemoveMarkerlight` **只置标志、不摘 trait**，
真正 `RemoveTrait(0x492)` 在 **`CardScript__OnTurnEnd.c:135-141`**（`if (+0x57 ∧ CurrentMarkerlight>0) → RemoveActiveEffect + RemoveTrait`）。
**我们 `RuleCore.cs:6884` 是当场 `RemoveAll("markerlight")`** ⇒ 行为差异：
原版**标记光一直留到回合末**（计数器也一直在），我们**打完就没了**。
⇒ 这条要比对规则书/卡面再定（**不在本件范围**，但必须先记账，见报告 §⑤）。

### 6.7 建议的派单（顺序 · 文件所有权互不重叠）

| 批 | 件 | 改哪些文件 | 依赖 |
|---|---|---|---|
| **①** | `P0-1` + `P0-2`（前置修） | `CardPresentation/Core/TraitFrames.cs` · `CardPresentation/Battle/BattleDriver.cs`（1 行） | 无 |
| **②** | 三个新文件（`TraitEffectController` / `HuntMarkController` / `MarkerlightController`，后者先只做 `Start` + `UpdateValue`） | 新增 3 个 `.cs`（+ `.meta`） | ① |
| **③** | `Attach` + 4 个入口（E1–E4）+ `ReplayAndRefresh` + `EvtKind.StatusApplied` 接线 | `TraitFrames.cs` · `BattleDriver.cs` | ② |
| **④** | 断言（A-E1…A-LIFE + 2 条灭自证） | `Editor/BattleScene.cs`（或独立 `Editor/TraitEffectAssert.cs`） | ③ |
| **⑤**（P2，另开） | `MaterialFader` 共用件 + `MarkerLightUsed` / `AimingAttack` 接线 | 新 `Core/MaterialFader.cs` + `TraitFrames.cs` + 引擎事件 | ③ |

⚠️ ①③ 都动 `TraitFrames.cs` ⇒ **必须串行**（一个文件同一时刻一个写手）。
② 的三个新文件与 ①③ **互不相交**，可并行。

---

## §七 顺手发现（都已落进报告 §⑤，供调度台分流）

| 编号 | 发现 | 判据 | 影响 |
|---|---|---|---|
| **F1** | 🔴 两个 idle 框在我们这边 **4.3 s / 4 s 后自毁** | prefab 无 `AnimFXController`（§1.3）⇒ 库 `destroyTime:-1, natural:4.3/4` ⇒ `WarpforgeEffectLibrary.AutoLifetime():78-79` 回落到 `natural`；原版只由 `Destroy` 收（`RemoveStatusAnim.c:58`） | 计数器/状态框**会在 4 秒后凭空消失**；`TraitFrames._play` 里留下死引用 ⇒ 下次 `DisplayStatus` 会**重新实例化**（看起来像「闪一下」）。**必须先修**（已列 P0-1） |
| **F2** | 🔴 markerlight **摘除时机**与原版不同 | 原版：攻击后只置 `+0x57`，回合末 `CardScript__OnTurnEnd.c:135-141` 才 `RemoveTrait(0x492)`；我们：`RuleCore.cs:6884` 当场 `RemoveAll` | 数值/表现都会差一整个回合；**需重新核规则书**（可能是我们有意的简化，但账上没写） |
| **F3** | `CardScript.ActivateTraitAnim`（`CardScript.cs:16696`）**0 调用方** | `grep -rl ActivateTraitAnim d:/2/tools/decomp_full/` 只命中它自己 | 又一个「壳方法」。⛔ 别照它去接调用点 |
| **F4** | `TraitEffectController.currentValue` **死字段**；`Toggle(bool)` **0 调用点** | §1.2 / §八·A | 实现时按形状留空，⛔ 别赋予语义 |
| **F5** | `MarkerlightController.updateMarkerLightVFX` 在本 build 是**空引用** | `MonoBehaviour_-3235397510437391791.json:33-36` | `UpdateValue` 里那段 `Instantiate` 是死支（铁律 11 例外①），做了也看不到 ⇒ ⛔ 别猜一个 VFX 塞进去 |
| **F6** | id **1170 = markerlight**、**1260 = huntMark** 直接以**字面量**出现在 `CardScript.cs:11264…11330`（`(System.Int32Enum)1170`） | `DefinedTrait.cs:115,129` | 找 hunter/markerlight 的代码路径时，⛔ 别只搜名字 —— 要搜 `1170` / `0x492` / `1260` / `0x4ec` |
| **F7** | `MarkerlightIdle` 的 `fadeTime` 是 **0.5**（prefab 覆写），ILSpy 的字段默认值是 **1f** | `MonoBehaviour_-3235397510437391791.json:32` vs `MarkerlightController.cs:17` | 照 ILSpy 默认值写 = 慢一倍（铁律 5·c「一个值 ≠ 全部情况」的典型） |
| **F8** | `Renderer.GetMaterial()` = **`renderer.material`（实例副本）**，**不是** `sharedMaterial` | `dump.cs:526597-526599`（`[FreeFunction(Name="RendererScripting::GetMaterial")] private Material GetMaterial()`）与 `:526579` 的 `material` 属性成对；`sharedMaterial` 走 `:526601-526603` 另一个入口 | ⛔ 别「优化」成 `sharedMaterial`（多张卡会互相写死 `_RippleScale`）。副本材质**不自动回收**，**原版同形** ⇒ 照抄；要收只能在 `OnDestroy` 里 `Destroy`（= 我们的口径，须注释） |
| **F9** | 我们导入的两个 prefab 根上**多一个 `WarpforgeEffectBinder`**（guid `d9d66f84…`，我们自己导出时加的） | `WarpforgeVFX/Prefabs/*.prefab` 的 `m_Script` 实测 | **不是差异**，⛔ 别当「我们多做了」去删；原版根 = `Transform`+控制器 |

---

## §八 附录 —— 「查不到 / 没有」的自证

### A. `Toggle(bool)` 在本 build 无调用点

| 搜法 | 结果 |
|---|---|
| `grep -rn "TraitEffectController" out_cpp2il_dev_ilspy/` | **6 行**：3 个 `GetComponent<TraitEffectController>`（= §1.4 三处）+ 基类声明 + 2 个子类声明 |
| 三处的后续调用 | 全部是 vtable 槽 **`0x198`**（= `UpdateValue`）；**没有一处**调 `Toggle` |
| 反编译里对 `TraitEffectController` 类型变量的其它槽位 | `grep -rn "+ 0x190)" decomp_full/` → **0 命中** |
| prefab 上的 UnityEvent | `grep -n "m_MethodName"` 于两个 prefab → **0 行**（没有 `m_OnCullStateChanged` 之外的持久调用） |
| 两个子类的 `Toggle` 方法体 | **都是空体**（`HuntMarkController.cs:15-17` / `MarkerlightController.cs:50-52`） |
⇒ **判死**（铁律 11 例外①：原版本身没有调用点）。实现成空方法即可。

### B. 「全 build 只有 2 个实例」

```
cd D:/2/新解包资源/assets_full
grep -rl -- "-850145008731250946" */MonoBehaviour/    → 1  （HuntMarkController）
grep -rl -- "-3449186136354092259" */MonoBehaviour/   → 1  （MarkerlightController）
```
（`-850145008731250946` / `-3449186136354092259` 是这两个脚本的 `m_Script` pathID，
取自上面那两个 MonoBehaviour JSON 的 `m_Script.m_PathID`。）

### C. 本轮**没**覆盖到的（如实写）

| 没做的 | 为什么 | 要补的话 |
|---|---|---|
| **没跑到原版实况** | 原版已关服；这两个框只在**战斗内**出现，而战斗内需要远端卡数据（`inj FAIL: empty Data`，见 CLAUDE.md 铁律 4 那张表）⇒ **进不去** | —— （静态判据已足够：组件字段、方法体、prefab 值全部拿到） |
| `MaterialFader` 的完整语义（`Update` 插值曲线、`CacheOriginalMaterials` 的材质数组顺序） | 属 `P2`，且是**新共用件**，本件只登记不施工 | 派下一批时按 `MaterialFader__*.c` 六个方法逐个读 |
| `Grid floor` 的 `_RippleScale` 在 shader 里的**含义** | 只影响观感参数，不影响钩子层 | 查 `bundle_*` 的 shader 属性表（`工具/dump_shader.py`） |

---

## §九 文件索引（施工时会碰到的）

| 类/资产 | 路径 |
|---|---|
| `TraitFrames`（**主战场**） | `MyGame/Assets/CardPresentation/Core/TraitFrames.cs` |
| `BattleDriver`（`SyncKeywords` 唯一调用点 `:25396`；`EvtKind` 消费者） | `MyGame/Assets/CardPresentation/Battle/BattleDriver.cs` |
| `WarpforgeEffectPlayer`（实例化 + 生命周期） | `MyGame/Assets/WarpforgeVFX/Runtime/WarpforgeEffectPlayer.cs`（`Play` `:267-341`） |
| 效果库（`destroyTime/-1` 的来源） | `MyGame/Assets/Resources/WarpforgeVFX/WarpforgeEffectLibrary.asset` · `Runtime/WarpforgeEffectLibrary.cs:75-81` |
| 绑定表（trait → prefab，**唯一来源**） | `MyGame/Assets/CardPresentation/Resources/trait_effect_bindings.json` |
| 两个 prefab | `MyGame/Assets/WarpforgeVFX/Prefabs/HuntMark_IdleEffect.prefab` · `MarkerlightIdle.prefab` |
| 原版判据（ILSpy） | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/{TraitEffectController,TraitEffect,HuntMarkController,MarkerlightController,BattleManager,BattleCardUI,CardScript}.cs` |
| 原版判据（Ghidra） | `d:/2/tools/decomp_full/`：`TraitEffectController__Start.c` · `HuntMarkController__*.c` · `MarkerlightController__*.c` · `BattleManager__CreateAnim.c` · `BattleManager._ResolveTraitAnim_d__465__MoveNext.c` · `BattleManager.__c__DisplayClass465_0___ResolveTraitAnim_b__0.c` · `BattleCardUI__DisplayStatusAnim.c` · `BattleCardUI__DisplayTriggerAnim.c` · `BattleCardUI__RemoveStatusAnim.c` · `CardScript__PlayTriggerAnim.c` · `CardScript__AddEffect.c` · `CardScript__ActivateTraitsOnSummonOrEnchantment.c` · `CardScript__SetRemoveMarkerlight.c` · `CardScript__SetTraitsOnPlayAttackingStatus.c` · `CardScript__OnTurnEnd.c` · `EntityScript__GetCurrentTraitValueWithModifiers.c` · `MaterialFader__Toggle.c` |
| 原版 prefab 侧 | `d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/`：`GameObject/HuntMark_IdleEffect.json`、`GameObject/MarkerlightIdle.json`、`MonoBehaviour/MonoBehaviour_-5880698705900734509.json`、`MonoBehaviour/MonoBehaviour_-3235397510437391791.json`、`MonoBehaviour/MonoBehaviour_-2598067149583091757.json`、`ParticleSystemRenderer/ParticleSystemRenderer_-7782206327072345519.json` |
