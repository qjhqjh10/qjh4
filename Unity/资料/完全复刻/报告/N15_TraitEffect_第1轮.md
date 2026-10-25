# N15 · `TraitEffectController` 钩子层 —— 第 1 轮（执行：只建 3 个新文件，不接线）

> **产出**：`N15 · 执行` 代理，2026-10-25。
> **判据正本**：`Unity/资料/完全复刻/判据/TraitEffectController_钩子层.md`（下称「判据」）。
> **上一轮报告**：`Unity/资料/完全复刻/报告/N13_TraitEffectController_第1轮.md`。
> **本批红线（全部遵守）**：⛔ 不跑 Unity · ⛔ 不动 git · ⛔ 不碰任何既有文件（`TraitFrames.cs` / `BattleDriver.cs` /
> `Battle/**` / `RuleEngine/**` / `Shell/**` / `Editor/**` / `.tsv` / `CLAUDE.md` / `项目任务.md` 一个字节都没改）·
> ⛔ 不修订判据文件（下面 §⑤·b 只提**订正建议**，交由调度台）。

---

## ① 结论

| 文件（新建） | 类 | 实现了什么 | 哪些是**推断** / **我们的** |
|---|---|---|---|
| `CardPresentation/Core/TraitEffectController.cs` | `TraitEffectController`（abstract，: MonoBehaviour） | 原版 17 行**逐字形状**：`currentValue`（死字段保留）· `protected virtual Start()` · 3 个抽象方法 `Toggle` / `UpdateValue` / `AimingAttack` | 形状 = **实读**（`out_cpp2il_dev_ilspy/TraitEffectController.cs` 全文） |
| 同上（同文件，第 4 个类） | `TraitEffectControllers`（static，**非** MonoBehaviour） | `Attach(GameObject, prefabName)`：**按 prefab 名**（`HuntMark_IdleEffect` / `MarkerlightIdle`）补控制器，幂等 | 判据 §6.2 的方案落地；**没人调它**（本轮不接线） |
| `CardPresentation/Core/HuntMarkController.cs` | `HuntMarkController` | `UpdateValue` = `counter.text = newValue.ToString()` + `DOPunchScale(counter.transform, Vector3.one*0.1f, 0.5s, 10, 1.0f)`；`Toggle` 空体；`AimingAttack` **throw `NotImplementedException`**（照原版）；`Start` 空覆写 + 我们的绑定 | `0.5 / 10 / 1.0 / 0.1` = **`.rdata` 实读**；**`Vector3.one × 0.1` 那个乘法 = 高置信推断**（代码注释与报告都如实标了） |
| `CardPresentation/Core/MarkerlightController.cs` | `MarkerlightController` | 9 个字段（名字逐一同原版）· `Start`（关 Expiring + 两个 `Grid floor` 设 `_RippleScale`）· `UpdateValue`（同 HuntMark；`updateMarkerLightVFX` 死支**不做**）· `Toggle` 空体 · `MarkerLightUsed` 逐字 · `ChangeState(bool)` 逐字（原版死代码，按形状保留）· `AimingAttack` = **明确的「未实现」出口**（`LogError`，前置是还没有 `MaterialFader`） | `fadeTime = 0.5` = **prefab 覆写值实读**（⛔ 不是 ILSpy 的 1f）；`Grid floor` 的绑定方式 = **我们的**（判据那句与实物不符，见 §⑤·b F-A） |

**一句话**：三个类把「输入一个 `int` → 写 prefab 上的 TMP + 弹一下」这条**纯 UI** 通道建好了；
⛔ 不写引擎状态、不发事件、⛔ 没有第二条「trait → prefab」绑定路径。
**本轮不接线**：没有任何既有文件引用这三个类（`Attach` 目前 0 调用点）。

---

## ② 判据与证据（对应判据文件哪一节）

| 实现点 | 判据 § | 一手证据 |
|---|---|---|
| 基类 3 个抽象方法 + `currentValue` + `virtual Start` | 判据 §1.1 / §1.2 / §八·A | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TraitEffectController.cs:3,5,7-9,11,13,15`（**全文读过**） |
| 「不是 new 的，是 prefab 组件；`UpdateValue` 在 `Instantiate` 那一拍就调」 | §1.2 | `BattleManager__CreateAnim.c:62-81`（判据已列；本批未重读） |
| 只有 2 个实现 / 各 1 个 prefab | §1.3 / §八·B | 判据的全库唯一性自证（按 `m_Script` pathID 扫 84 个 bundle，各 1 命中）；本批只复核了 prefab 侧 |
| 输出 = `text` + `DOPunchScale`；四个常量 | §3.2 | `decomp_full/HuntMarkController__UpdateValue.c:13-30` · `MarkerlightController__UpdateValue.c:54-79` · `.rdata`：`DAT_1834b2dc4`=0.1 · `DAT_1834b2bb4`=0.5 · `DAT_1834b2bb8`=1.0 · vibrato `mov 0xa` |
| `AimingAttack` 原版 throw | §1.1 / §6.1② | `out_cpp2il_dev_ilspy/HuntMarkController.cs:31-37`（`throw new NotImplementedException()`，**全文读过**） |
| `MarkerlightController` 的 9 字段 + 5 方法体 | §1.1 / §3.2 / §6.1③ | `out_cpp2il_dev_ilspy/MarkerlightController.cs`（**全文读过**，108 行）＋ `decomp_full/MarkerlightController__Start.c` / `__UpdateValue.c` |
| `fadeTime = 0.5`（不是 1f） | §1.3 / §七 F7 | `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-3235397510437391791.json:32`（判据已核，本批沿用） |
| `updateMarkerLightVFX` 是空引用 ⇒ 死支不做 | §七 F5 | 同上 JSON `:33-36`（判据已核） |
| 「按 prefab 名挂、不按 trait 名挂」 | §6.2 / §6.4 灭自证 2 | 判据 §1.3 的唯一性 + §6.2 的 `Attach` 方案 |
| prefab 侧节点名 / 层级 / 文本组件类型 | §1.3 / §6.2 | **本批亲核我们导入的两个 prefab**（`MyGame/Assets/WarpforgeVFX/Prefabs/`）：见 §③ 末「绑定表」 |

**值语义（下一批传参时必须照这个来）**：输入 = **当前 trait 总值** = 基值 + Σmodifier、**钳 ≥ 0**
（原版 `EntityScript__GetCurrentTraitValueWithModifiers.c:1-75`；我们引擎侧等价物 = `UnitState.KwValue(keyword)`）。
⛔ **不是**卡面上的固定值、⛔ 不是「层数变化量」。

---

## ③ 新增文件与接口说明

### ③·1 `TraitEffectController.cs`（6672 B · LF）

```
namespace CardPresentation
  public abstract class TraitEffectController : MonoBehaviour
      protected int currentValue;                        // 原版 :5 —— 死字段，保留形状
      protected virtual void Start() { }                 // 原版 :7-9
      public abstract void Toggle(bool option);          // 原版 :11 —— 本 build 0 调用点
      public abstract void UpdateValue(int newValue);    // 原版 :13
      public abstract void AimingAttack(bool option);    // 原版 :15
  public static class TraitEffectControllers            // 「我们的」挂载助手（判据 §6.2）
      public static TraitEffectController Attach(GameObject go, string prefabName)
```

* `Attach` 幂等：`go.GetComponent<TraitEffectController>()` 已经有时**原样返回**（将来重导出把组件带进来时⛔ 别再加一个）。
* 只认两个字符串；别的 prefab 返回 `null`（不挂、**也不出声** —— 这是判据要求：全库只有那两个带控制器）。
* 本轮 **0 个调用点**（⛔ 没接 `TraitFrames`）。

### ③·2 `HuntMarkController.cs`（9913 B · LF）

```
public class HuntMarkController : TraitEffectController
    // 原版数值（public const，便于断言引用；恒定值不参与调参）
    PunchScale=0.1f · PunchDuration=0.5f · PunchVibrato=10 · PunchElasticity=1.0f
    // 原版 :9 的**唯一**序列化字段（原版是 private）
    [SerializeField] TextMeshPro counter;
    // 观测量（判据 §6.5；「我们的」，只读）
    PunchCount · UpdateCalls · LastValue · TextForTest · Bound
    void Awake()                     // 「我们的」：必须在这一拍就绑（原版 UpdateValue 早于 Start）
    protected override void Start()  // 原版空覆写 + 幂等绑定
    public override void Toggle(bool) { }
    public override void UpdateValue(int)
    public override void AimingAttack(bool)   // throw NotImplementedException（照原版语义）
    void BindIfMissing()             // transform.Find("Counter") → GetComponent<TextMeshPro>()
```

⚠️ **`CardTween.Use(...)` 包了一层**（原版是裸 `DOPunchScale`）：只做两件事 ——
① `SetUpdate(CardTween.Mode)`（批处理自检能推进）；② `SetLink(counter.transform.gameObject)`（框被 `Destroy` 时补间一起死）。
**四个参数一个没改**；`Ease.Linear` 对 punch 不生效（punch 用自己的曲线）。注释里写明了这是「我们的封装」。

### ③·3 `MarkerlightController.cs`（17810 B · LF）

```
public class MarkerlightController : TraitEffectController
    const PunchScale/PunchDuration/PunchVibrato/PunchElasticity   // 与 HuntMark 同值（原版前 3 行相同）
    const RippleScaleProperty="_RippleScale" · FloorNodeName="Grid floor" · PrefabFadeTime=0.5f
    [SerializeField] counter · normalStateObject · materialFaderNormalState · expiringStateObject
    [SerializeField] materialFaderExpiringState · fadeTime=0.5f · updateMarkerLightVFX · floors[] · alreadyAttacked
    观测量：PunchCount · UpdateCalls · LastValue · TextForTest · Bound · AlreadyAttackedForTest
            · FloorsBound · FloorsForTest · FadeTimeForTest
    void Awake()                        // 「我们的」：早绑
    protected override void Start()     // 原版 :25-48 逐字（+ 空判/绑定的防御）
    public override void Toggle(bool) { }
    public override void UpdateValue(int)         // 死支注释在，⛔ 未实现
    public override void AimingAttack(bool)       // **明确未实现出口**：LogError（缺 MaterialFader，P2）
    public void MarkerLightUsed()                 // 原版 :88-93 逐字（三句，不走 ChangeState）
    void ChangeState(bool)                        // 原版 :95-101 逐字（原版死代码，按形状保留）
    // FadeFixedElements(bool, MaterialFader)     // 原版 :103-106 —— 参数类型不存在 + 死代码 ⇒ 本轮不复刻（见 §⑤）
    void BindIfMissing() · ChildGo() · ChildComponent<T>()
```

### ③·4 绑定表（本批**亲核**我们导入的 prefab，不是抄判据）

| prefab | 节点 | 实测父链 | 绑定方式 |
|---|---|---|---|
| `HuntMark_IdleEffect.prefab` | `Counter`（`TMPro.TextMeshPro`，3D 版 · 脚本 guid `9541d86e2fd84c1d9990edf0852d74ab` · `m_text: "1"` · `m_fontSize: 6`） | `['Counter','HuntMark_IdleEffect']`（直接子件） | `transform.Find("Counter")` |
| `MarkerlightIdle.prefab` | `Counter`（同上） | `['Counter','MarkerlightIdle']` | `transform.Find` |
| 同上 | `Normal State` / `Expiring State` | 都是根的直接子件 | `transform.Find` |
| 同上 | `Grid floor` ×2（各挂 1 个 `ParticleSystemRenderer`） | 🔴 **不是直接子件**：`['Grid floor','Base Vfx','Fixed','Expiring State','MarkerlightIdle']` 与 `['Grid floor','Base VFX','Fixed','Normal State','MarkerlightIdle']` | `GetComponentsInChildren<ParticleSystemRenderer>(true)` + 按 GameObject 名过滤（含**未激活**，因为 `Expiring State` 在 `Start` 里要被关掉） |
| 两个 prefab 根 | 组件 = `Transform` + **`WarpforgeEffectBinder`**（我们导出时加的，guid `d9d66f84…`）；**没有** `AnimFXController`，也**没有**任何 `TraitEffectController` | — | `Attach` 的 `GetComponent` 检查不会误命中 |

---

## ④ 核验

| 项 | 结果 |
|---|---|
| **秒级类型检查** | `TMPDIR=/tmp/wf_n15 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数: 0** · **编辑器错误数: 0**（**0/0**） |
| 行尾 | 三个 `.cs` 全部 **LF**（CRLF = 0），与 `Core/` 目录惯例一致（`TraitFrames.cs` / `CardFeel.cs` 实测都是 LF） |
| `.meta` | 三个 `.cs.meta` 已建（格式照抄同目录现有 `.cs.meta`：`fileFormatVersion: 2` + 新 guid，59 B、无尾换行） |
| 改动范围 | `git status --porcelain` 只看得到这 **6 个新文件（3 `.cs` + 3 `.meta`）**，全部 `??`（未跟踪）；**没有一个既有文件被我改过** |
| Unity | ⛔ 没跑（本批红线）；⇒ 组件绑定、`_RippleScale`、punch 的**实机效果未验**，要靠批④的断言（A-E1…A-LIFE） |

---

## ⑤ 没查清

| # | 没查清的东西 | 为什么要紧 | 本轮的处置 |
|---|---|---|---|
| 1 | **`Renderer.GetMaterial()` 的定义体在反编译产物里没找到**（`grep 'GetMaterial(this' / 'Material GetMaterial'` 全库 **0 命中**，但调用点很多：`BattleCardUI.cs` / `CardScript.cs` / `MarkerlightController.cs` / `MaterialFader.cs` …） | `Start` 里给 `Grid floor` 设 `_RippleScale` 时，**改的是实例材质还是共用材质**取决于它 | 用 Unity 标准的 `Renderer.material`（为该 renderer 实例化独立材质）；⛔ 不用 `sharedMaterial`（会在运行时改到共用资产、编辑模式下还会把资产弄脏）。代码注释写明「原版那个 helper 到底是哪个 = 未查清」 |
| 2 | `Vector3.one * 0.1f` 的**乘法**这一跳 | 是本次实现里唯一没被逐字读到的一句 | 代码注释标 **高置信推断**，⛔ 没写成「实读」；`0.1` 本身是 `.rdata` 实读 |
| 3 | `AimingAttack` / `FadeFixedElements` 的完整语义 | 依赖**本工程还没有的** `MaterialFader`（判据 §6.6 · P2） | `AimingAttack` 走 `LogError` 的**明确未实现出口**；`FadeFixedElements` 本轮不复刻（参数类型不存在 + 原版死代码）；两个 `materialFader*` 字段类型暂时放宽成 `MonoBehaviour` 保留字段位，P2 建出真类型后改回 |
| 4 | `floors[]` 两个元素在原版 prefab 里的**序列化顺序**（谁先谁后） | 只影响 `Start` 的遍历顺序 | 不影响结果（两个都用**同一个** `transform.lossyScale.x`）；自检按「集合」断，不按顺序断 |
| 5 | 组件在**实机**上的绑定结果（`Counter` / 两态 / 两个 `Grid floor`） | 本批不跑 Unity | 靠 `BindIfMissing()` 的**出声**兜底（绑不上就 `LogError`），靠批④断言复验 |

### ⑤·b 顺手发现（**判据订正建议 4 条**；⛔ 我一条都没改判据文件，请调度台分流）

> 简报里「顺手发现写第 ⑥ 节」与「⑥ = 接线点清单」冲突，这里单列在 ⑤·b，**内容不丢**。

| 编号 | 判据原文 | 实际（本批一手核） | 影响 / 我的处置 |
|---|---|---|---|
| **F-A** 🔴 | 判据 **§6.2**：「`Counter` / `Normal State` / `Expiring State` / **两个 `Grid floor`** 在我们导入的 prefab 里**都是根的直接子件**」 | **前半句对，`Grid floor` 错**。实测父链：`['Grid floor','Base Vfx','Fixed','Expiring State','MarkerlightIdle']` 与 `['Grid floor','Base VFX','Fixed','Normal State','MarkerlightIdle']` | 照判据写 `transform.Find("Grid floor")` **恒 null**（还会静默只出声）⇒ 我改成 `GetComponentsInChildren<ParticleSystemRenderer>(true)` + 名字过滤（实测正好 2 个）。**建议改判据那一行** |
| **F-B** | 判据 **§6.1③**：「`MarkerLightUsed` / `AimingAttack` … **上面两个都走 `ChangeState`**」 | **源码不是这样**（`out_cpp2il_dev_ilspy/MarkerlightController.cs` 全文）：`MarkerLightUsed`(:88-93) 是**直接三句**；`AimingAttack`(:81-86) 走 `MaterialFader.Toggle`；`ChangeState(bool)`(:95-101) 与 `FadeFixedElements`(:103-106) **类内零调用**（`grep` 在该文件里只有定义行）⇒ **死代码** | 我按源码实现（`MarkerLightUsed` 直接三句；`ChangeState` 按形状保留并注明死代码；`FadeFixedElements` 不复刻）。**建议改判据 §6.1③ 的「都走它」与「别写两份」** |
| **F-C** | 判据 §6.1② 的代码骨架把 `counter` 写成 `public TextMeshPro counter;` | 原版是 **`private TextMeshPro counter;`**（`HuntMarkController.cs:9`；`MarkerlightController.cs:7` 同） | 我保留 `[SerializeField]` + private（贴合序列化形状），自检改走只读属性 `TextForTest` / `Bound` 等观测量 |
| **F-D** | 判据 §1.3 说两个 prefab 根「只有 `Transform` + 控制器两个组件」 | **我们导入的** prefab 根多了一个 `WarpforgeEffectBinder`（我们导出时加的，guid `d9d66f84…`）——**原版没有**，但它不影响本层（`Attach` 只查 `TraitEffectController`） | 仅备案；`Attach` 的幂等检查已覆盖 |

---

## ⑥ 接线点清单（**下一批**做；本批一条都没接）

> **共同前提**：`valueOf(key)` 必须是**当前 trait 总值**（基值 + Σmodifier、钳 ≥0），
> 我们引擎侧的等价物 = **`UnitState.KwValue(keyword)`**；⛔ 别用卡面固定值、⛔ 别用「本次增加量」。
> **`ctl` 的取法**：`WarpforgeEffectPlayer` 是 MonoBehaviour ⇒ `p.gameObject.GetComponent<TraitEffectController>()`。

| # | 入口（现读位置） | 原版对应 | 传什么值 | 要做什么 | 下一批谁做 |
|---|---|---|---|---|---|
| **W0** | `TraitFrames.Play(string prefabName)`（`TraitFrames.cs:964-975`，**所有**状态框出生的唯一入口） | `BattleManager.CreateAnim` 的 `Instantiate` + invoke | — | ① `WarpforgeEffectPlayer.Play(...)` 之后 `TraitEffectControllers.Attach(p != null ? p.gameObject : null, prefabName)`；② 顺手做 **P0-1**：持久档（`_play` 里存的那张 OnPlay 框）显式传 `autoDestroySeconds: float.PositiveInfinity`（原版这两件**没有**计时销毁，见判据 §6.0） | 批① / 批③（`TraitFrames` 写手，**必须串行**） |
| **W1**（E1） | `TraitFrames.DisplayStatus` 的「**首次实例化** OnPlay 框」分支（`:819-822`） | `BattleManager.__c__DisplayClass465_0.<ResolveTraitAnim>b__0`（`DisplayClass465_0…b__0.c:14-31`） | `valueOf(key)` | `var p = Play(name); ctl?.UpdateValue(v);` | 批③ |
| **W2**（E2） | `TraitFrames.DisplayStatus` 的「**已在 `_play` 字典**」分支（`:799-808`） | `BattleCardUI__DisplayStatusAnim.c:126-198`（判据 §1.4 的 **③ 号调用点**） | `valueOf(key)`（**当前值**） | `localPosition = Vector3.zero` → `SetActive(true)` → `ctl?.UpdateValue(v)`。⛔ **不重建、不重播**（断言 A-E2 断 `InstanceId` 不变、`PlayCount` 不增） | 批③ |
| **W3**（E3） | `TraitFrames.PlayStartRows`（`:845-857`） | `DisplayStatusAnim.c:86-119`（`ResolveTraitAnim` 带 `traitValue`） | `valueOf(key)` | 每次 `Play` 后 `ctl?.UpdateValue(v)`。⚠️ 这两条 trait 的 Start/FromCode prefab **不带控制器** ⇒ 事实上空转，但**调用必须发生**（A-E3 数 `UpdateCalls`） | 批③ |
| **W4**（E4） | `TraitFrames.DisplayTrigger`（`:902-909`） | `DisplayTriggerAnim.c:97-104` | `valueOf(key)` | 每条 `Play` 后 `ctl?.UpdateValue(v)`；⛔ 不动 `_triggerPlays` 的去重语义 | 批③ |
| **W5**（P0-2） | `TraitFrames.SyncKeywords(IEnumerable<string>)`（`:756`） | `CardScript.ActivateTraitsOnSummonOrEnchantment` + `AddEffect` 的 `DisplayStatusAnim(trait,1)` | — | 加**重载** `SyncKeywords(IEnumerable<string> current, Func<string,int> valueOf)`；旧签名转发 `valueOf = null`（⚠️ 别把值塞进 `CardData`） | 批① |
| **W6**（P0-2） | `BattleDriver.cs:25396`（`SyncKeywords` **唯一**调用点） | 同上 | `k => u.KwValue(k)` | 改传重载 | 批① |
| **W7** | `BattleDriver.PlaySignal` 的 `EvtKind.StatusApplied` 分支（`:21908-21918`，**已在**） | `CardScript__AddEffect.c:645-664`（闸门 = `CardTrait._hasInPlayAnim`） | `u.KwValue(e.CardId)`（只在 `e.Amount > 0` 时） | 现在只调 `statusTf.DisplayStatus(e.CardId)` —— **没传值**。改成 `DisplayStatus(e.CardId, value)` 或新方法 `ReplayAndRefresh(e.CardId, value)`；⛔ **不新建 `TraitFrames`**（`_prev` 为空会把全部 OnPlay 重演一遍） | 批③ |
| **W8** | 值变化兜底 `_prevValue`（判据 §6.3 末段） | 原版 `GetCurrentTraitValueWithModifiers` 里 modifier 变化那条路 | `valueOf(key)` | 只在**值真的变了**才刷；必须与 W7 **同一把帧锁**（同帧只刷一次，否则 `DOPunchScale` 双弹） | 批③ |
| **W9**（P2） | `MarkerlightController.MarkerLightUsed()`（**本批已实现**，0 调用点） | `CardScript.SetRemoveMarkerlight`（`CardScript.cs:11275` / `:11293` —— **两本字典各取一次**：play 表 + trigger 表） | — | 前置：补一条「markerlight 被打掉」的 `EvtKind`（现在没有）→ 对**两本字典**里的实例各调一次 `MarkerLightUsed()`。⚠️ 连带账 **F2**：原版**回合末**才 `RemoveTrait(0x492)`，我们 `RuleCore.cs:6884` 当场 `RemoveAll` | 批⑤ |
| **W10**（P2） | `MarkerlightController.AimingAttack()`（**本批是明确的未实现出口**） | `CardScript.SetTraitsOnPlayAttackingStatus`；调用方 `CardScript._PrepareAttack_d__353__MoveNext.c:41`（=false）/`:65`（=true） | — | 前置①**新写 `Core/MaterialFader.cs`**（`MaterialFader__Toggle.c:11-77` + `Update` + `CacheOriginalMaterials` + `RestoreOriginalMaterials`）；②补「开始攻击 / 攻击结束」两条引擎事件；③把 `materialFader*` 两个字段类型从 `MonoBehaviour` **改回 `MaterialFader`** 并接上 | 批⑤ |

**断言（批④，另写 `Editor/TraitEffectAssert.cs`）**：A-E1 / A-E2 / A-E3 / A-E4 / A-ML / A-LIFE + **灭自证 2 条**
（同帧连跑 3 次 sync ⇒ `PunchCount` 仍 1；`MarkerlightIdle` 与两个非 idle prefab 上 `GetComponent<TraitEffectController>() == null`）。
本轮已把断言要读的量备好：`PunchCount` / `UpdateCalls` / `LastValue` / `TextForTest` / `Bound` /
`AlreadyAttackedForTest` / `FloorsBound` / `FloorsForTest` / `FadeTimeForTest`。
`TraitFrames` 侧那四个计数器（`UpdateCalls` / `AttachedCount` 等，判据 §6.5）**不在本批范围**。

> 🔴 **2026-10-25 就地订正（第二轮 · 铁律 5）**：`Editor/TraitEffectAssert.cs` **已建（8 组）**，但
> **`A-LIFE` 与「同帧连跑 3 次 sync ⇒ `PunchCount` 仍 1」这两条【本轮没写】** —— 它们要断的是
> `TraitFrames` / `P0-1` / `W8` 的行为，相应 API 还不存在（写进来编译不过）。
> 而「非 idle prefab 上 `GetComponent<TraitEffectController>() == null`」那半条**已写**（= 断言 A-E4）。
> 逐条对应、以及「现在跑是什么结果」见 **§⑦**。

---

## ⑦ 断言文件与 8 组断言的对应关系（第二轮 · 2026-10-25）

> **新文件**：`Unity/MyGame/Assets/CardPresentation/Editor/TraitEffectAssert.cs`（+ `.meta`）·
> 入口 `-executeMethod TraitEffectAssert.Run` · 全局命名空间（与 `BadgeFilterAssert` / `CardRootAssert` 同款）。
> **本批只写、没跑 Unity**；`TMPDIR=/tmp/wf_n15 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ⛔ 不接线、不改既有文件；只碰自己造的假物体（`new GameObject` + `hideFlags = DontSave`，跑完 `DestroyImmediate`），
> 不建场景、不碰 `TraitFrames` / `BattleDriver` / 引擎状态。

### ⑦·1 8 组断言 → 对应哪个接线点（W#）

| 断言 | 断的是什么 | 对应 W（接线后真正被覆盖的行为） |
|---|---|---|
| **A-E1** | 首次挂载：`Attach("HuntMark_IdleEffect")` ⇒ `HuntMarkController`；`UpdateValue(2)` ⇒ 文本 `"2"` + 弹一次；并比对四个原版常量（`0.1 / 0.5 s / vibrato 10 / elasticity 1.0`，`.rdata` 实读） | **W0**（`TraitFrames.Play` 里 `Attach`）+ **W1**（E1 首次实例化后 `UpdateValue`） |
| **A-E2** | 再次刷新：`Attach` 幂等；`UpdateValue(3)` ⇒ `"3"` / `PunchCount 2`；**根 / `Counter` / 控制器三个 `InstanceID` 不变**、控制器仍 1 个 | **W2**（E2「已在字典」分支）+ **W7**（`StatusApplied` 触发的重刷） |
| **A-E3** | 绑不上也**照样记账**（`UpdateCalls` 涨、`PunchCount` 不涨、`TextForTest == null`）+ **必须 `LogError` 出声** | **W3 / W4**（`PlayStartRows` / `DisplayTrigger`：即使实例上没有控制器，调用也必须发生）+ `TraitFrames` 侧 `UpdateCalls` 观测量 |
| **A-E4** | **只按 prefab 名挂**：`MarkerlightIdle` ⇒ `MarkerlightController`；`HuntMark_StartEffect` 与 trait 名 `huntmark` **都不挂**；`null` / 空名 / `null` 名 ⇒ 返回 null 不抛 | **W0**（`Attach` 的挂载范围；判据 §6.4 灭自证 2 的「挂载范围」那一半） |
| **A-ML** | `Start` 的两态（Normal on / Expiring off）+ `floors` 恰好绑**两个** `Grid floor`（诱饵 `Ground Effect` 不进）+ `fadeTime == 0.5` + 属性名字面量 `_RippleScale` | **W0**（组件挂上后 `Start` 自己跑）；寿命那半见 A-LIFE |
| **A-ML2** | `MarkerLightUsed()` 切态且**不刷计数**；`HuntMarkController.AimingAttack` **抛 `NotImplementedException`**；`MarkerlightController.AimingAttack` **不抛 + 出声 + 一个状态都不改** | **W9**（`MarkerLightUsed` 的引擎事件）+ **W10**（`AimingAttack` / `MaterialFader`） |
| **灭自证 1** | ① 没人调用 ⇒ 文本仍 `"1"` / `PunchCount 0` / `UpdateCalls 0`；② **同值连调两次 ⇒ 弹两次**（防抖不许塞进控制器） | **W8**（真正的「值没变不刷」闸门 = `TraitFrames` 的帧锁） |
| **灭自证 2** | `UpdateValue` **只改 `text`**：前后状态快照（层级 / 名字 / 兄弟序 / 本地位置·旋转·缩放 / activeSelf / layer / 组件数 / TMP 字号·颜色·对齐·enabled·字体）**逐项不变**，`Counter` 与兄弟节点 `InstanceID` 不变 | **W7 / W8**（接线后这条要升级成「前后**局面哈希**不变」） |

### ⑦·2 🔴 **有一处我【没有照原话做】—— 故意的，理由写在这里**（⛔ 下一批别以为我漏做了）

**原话**（调度台）：「必须包含 2 条灭自证（例：**值没变时计数不弹**；`UpdateValue` 改了 text 但没改引擎状态）」；
**判据 §6.4 灭自证 1 的原话**：「同一帧内把 `SyncBoard`/`SyncKeywords` 连跑 3 次（值不变）⇒ `PunchCount` **必须仍为 1**」。

**我没有在控制器层断「值没变不弹」，理由三条（缺一条都不成立）：**

1. **那道闸门不在这一层。** 原版 `HuntMarkController.UpdateValue`（`HuntMarkController__UpdateValue.c:13-30`）
   里**没有任何「同值就跳过」的判断** —— 每被调一次就写文本 + 弹一次。「值没变不刷」是**上游**保证的
   （`DisplayStatusAnim` 只在「trait 被加上 / 再次显示」时调它，判据 §2.2 / §2.3），对应我们的 **W8 帧锁**。
2. **在本层断它会逼出一个原版没有的实现。** 为让这条变绿，就得在 `UpdateValue` 里加
   `if (newValue == LastValue) return;` —— 那是**改实现去迎合断言**（「断言自证」的镜像错误）。
3. **它会让 A-E2 失去意义**：同一层里「同值不弹」与「再施加一次要弹」互相抵消，
   接线后 `EvtKind.StatusApplied` 的重刷（W7）会静默失效。

**改成的两颗钉子（结构上等价、方向相反，都在断言文件里）：**

* **(a) 没人调用 ⇒ 一个都不动**：文本仍 = prefab 的 `"1"`、`PunchCount 0`、`UpdateCalls 0`
  ⇒ 挡「在 `Awake`/`Start` 里自己刷一次」（这是本层**可能真犯**的错）。
* **(b) 同一个值连调两次 ⇒ 弹两次** ⇒ 挡「把防抖塞进控制器」（这是本层**最像对**的错实现）。

**判据那条原话没被丢掉**：它已写进断言文件末尾「⏳ 待接线」的第 4 条，**等 W8 接好后在 `TraitFrames` 侧补**
（到那时 `SyncKeywords` 的重载 + 帧锁存在，才写得对）。

### ⑦·3 现在跑是什么结果 / 还红在哪 / 分别等哪个 W

**结论：按设计，接线前跑 `TraitEffectAssert.Run` 应该是【8 组全绿】** ——
它只测本轮新建的三个类 + `Attach`，**不碰** `TraitFrames` / `BattleDriver`（那些 API 还不存在，所以断言里没写）。
⚠️ 也就是说：**「8 组绿」只证明「这一层自己没写错」，不证明「接上之后对」** —— 后者要等 W0–W10 接完再看同一批断言。

**两种「看着红/绿其实不对」的情况**（都已写进断言消息，别读错）：

* **假 TMP 造不出来**（`TmpFont.NewText` 返回 null = 字体取不到）⇒ A-E1 / A-E2 / A-ML / A-ML2 / 灭自证 2 会
  **显式报「没验到」**（`✗ (前提 …)`），⛔ **不是**「被测实现错了」；A-E3 不依赖 TMP，仍可绿。
* **`_RippleScale` 的数值**本轮**验不了**（工程里没有带该属性的 shader，`grep -rl _RippleScale *.shader` = **0 命中**）
  ⇒ A-ML 只验「绑到哪两个 renderer」+「属性名字面量」，**没验数值**，⛔ 不把这格当通过。

**还没写、等 W 接线的断言（写进来会编译不过，已列在断言文件末尾注释里）：**

| 待补的断言 | 等哪个 W | 判据 |
|---|---|---|
| **A-E3 的原版形**（实例上**没有控制器**也必须有调用） | **W3 / W4**（`TraitFrames` 侧 `UpdateCalls`） | §6.4 A-E3 |
| **A-E2 的 `InstanceId` / `PlayCount` 部分**（断 `_play` 里那个 `WarpforgeEffectPlayer`） | **W2** | §6.4 A-E2 |
| **A-LIFE**（两个 idle 框实例化 **>6 s 仍 `!= null`**） | **P0-1 + W0**（`autoDestroySeconds = +∞`） | §6.0 / §七 F1 |
| **灭自证 1 的原版原话**（同帧连刷 3 次 ⇒ `PunchCount` 仍 1） | **W8**（帧锁） | §6.4 灭自证 1 |
| 「`UpdateValue` **不改引擎状态**」的强形（前后**局面哈希**相等） | **W7 / W8** | §3.3 |
| `MarkerLightUsed` / `AimingAttack` 的**端到端**形（由引擎事件触发） | **W9 / W10** | §6.6 |

---

## ⑧ `Core/MaterialFader.cs`（第三轮 · 2026-10-25 · 只新建，不接线）

> **新文件**：`Unity/MyGame/Assets/CardPresentation/Core/MaterialFader.cs`（+ `.meta`）· 291 行 / 19.6 KB · LF · 类名 == 文件名。
> `TMPDIR=/tmp/wf_n15 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
> ⚠️ 复跑途中撞到过**别的代理的半成品**（⛔ 不是本文件）：`Shell/ChatPanel.cs` 3 条（`RowBgColor`/`RowPadLeft`/`RowPadRight`）·
> `WarpforgeArena1/Editor/ArenaBuilder.cs` 3 条（`TapColliderEntry.mesh`）· `RuleEngine/Core/EffectResolver.cs` 1 条（`PickPactKind`）；
> 等它们落盘后复跑 = **0/0**（判据：三次报错**一次都没有**指向本批的新文件）。
> ⛔ **没改 `MarkerlightController.cs`**（所以它的 `AimingAttack` **仍然是 `LogError` 的未实现出口**，那是下一批 W10 的事）·
> ⛔ 没碰 `Core/` 下任何其它文件。
> 📌 **§⑥ 的 W10 前置①「新写 `Core/MaterialFader.cs`」= 本轮已完成**；W10 剩下的前置是「引擎事件 + 把 `materialFader*` 字段类型改回 `MaterialFader` + 绑定」。

### ⑧·1 判据与证据

| 实现点 | 判据 | 一手证据 |
|---|---|---|
| 4 个序列化字段 + 5 个运行时字段（名字/偏移） | 账 237–244 · 判据 §6.6 | `out_cpp2il_dev_ilspy/MaterialFader.cs`（全文）· 反汇编偏移 `+0x20/0x28/0x29/0x2a/0x30/0x38/0x3c/0x40/0x44` |
| `Update` 的逐帧 α 插值与收尾 | 账 244 | `decomp_full/MaterialFader__Update.c:14-68`（`MoveTowards` 用 `dt/blendTime`；`currentBlendTime <= 0` 那一帧收尾；**每帧都减** `currentBlendTime`） |
| `Toggle` 的五步（cache → enabled=true → 两个时间 → targetFade → **`doFade = !doFade`**） | 账 242 | `MaterialFader__Toggle.c:11-77`（`doFade` 是 `== 0` 的**取反**，机器码级确认） |
| `CacheOriginalMaterials` 记 `sharedMaterial` | 账 237 | `MaterialFader__Toggle.c:30-40`（调 `GetSharedMaterial`） |
| `RestoreOriginalMaterials` = `Destroy(GetMaterial())` + `SetMaterial(original[i])` | 账 240 | `Update.c:70-88` |
| 材质取用 = **`renderer.material`（实例）** | 判据 §6.6 末 + N13 的 il2cpp dump | `Update.c:28`（`Renderer__GetMaterial`）+ `Toggle.c`（存档侧才用 `GetSharedMaterial`） |
| `FadeIn` / `FadeOut` / `SetTargetFade` / `ToggleRenderers` = **死代码** | 账 238/239/241/243 | `grep "\.FadeIn()\|\.FadeOut()"` 命中的都是别的类；全库 UnityEvent **0 处** `m_MethodName: FadeIn\|FadeOut\|Toggle`（本批亲核） |

### ⑧·2 🔴 本批一手查出的四件事（三件要订正/拍板）

| 编号 | 发现 | 证据 | 影响 / 本批处置 |
|---|---|---|---|
| **F-E** | 账 244 行写「原版 2 个实例（**都在** `MarkerlightIdle > Expiring State > Fixed`）」—— **错了一半** | 本批按 `Transform.m_Father` 父链解析两个实例的 GameObject：`-4433243589834689967`（normal）→ `Fixed > **Normal State** > MarkerlightIdle`；`-1453127347361795503`（expiring）→ `Fixed > **Expiring State** > MarkerlightIdle` | W10 接线要知道**各挂各的状态节点**。已在 `MaterialFader.cs` 类头写死 |
| **F-F** 🔴 | **`restoreOriginalMaterialsOnFadeOut` 在 prefab 里被覆写成 `0`**（C# 字段默认是 `true`）；`restoreOriginalMaterialsOnFadeIN = 0`、`disableOnFadeOut = 1` | `MonoBehaviour_-4433243589834689967.json` / `MonoBehaviour_-1453127347361795503.json` 实读 | 运行时 `AddComponent` **没有序列化值** ⇒ 字段初值按原版 C# 默认（true/false/true），**W10 必须调 `ApplyPrefabFlags(false, false, true)`**，否则淡出后会多一步「还原材质」（铁律 5·c 的典型） |
| **F-G** 🔴 | 原版 `Toggle` / `Update` 遇到 `renderers[i] == null` **会抛 `NullReferenceException`**（`.c` 里是 `goto` 抛出桩）；而**原版 expiring 实例的 `renderers[2]` 就是 `null`**（`m_PathID: 0`） | `Toggle.c:30-32` / `:73-74` / `Update.c:28` / `:59-60` + 两个实例 JSON 的 `renderers[]` | 照抄 = 切到 expiring 那一支时**崩掉整条攻击链**。我们**改成「跳过该下标 + `LogError` 一次」**（不静默、不崩）。⚠️ **这条差异请调度台拍板**：要严格照抄就改回抛 |
| **F-H** | `MaterialFader` **不是 Markerlight 专用**：全库 **28 个实例**（`vfxandmisc` 17 · `battlearenaleviathan` 4 · `battlesharedresources` 3 · `tauviorla`/`darkangels`/`aeldari`/`arena3` 各 1） | `grep -rl restoreOriginalMaterialsOnFadeOut */MonoBehaviour/ \| wc -l` = 28 | 「组件是死的」这个说法**不成立**；准确说法 = **组件通用且到处有，唯一活驱动是 `MarkerlightController.AimingAttack`**（`FadeIn`/`FadeOut`/`SetTargetFade`/`ToggleRenderers` 才是死代码） |

### ⑧·3 接口（原版形状 + 我们的绑定/观测）

**原版形状（逐字）**：`Update()` · `FadeOut()` · `FadeIn()` · `Toggle(bool, float)` · `SetTargetFade(float, float)` ·
`CacheOriginalMaterials()` · `RestoreOriginalMaterials()` · `ToggleRenderers(bool)`；字段 4 序列化 + 5 运行时。

**我们的新增（⛔ 都不接线、0 调用点，只为下一批能绑、能断）**：

| 成员 | 为什么有 |
|---|---|
| `BindRenderers(Renderer[])` | 原版靠 prefab 序列化；我们运行时 `AddComponent` ⇒ 必须显式绑。**故意不自动按子件绑**（原版是**精确的 4 条**，自动绑会多绑） |
| `ApplyPrefabFlags(bool, bool, bool)` | 见 F-F：我们没有序列化值可读，只能由接线方按 prefab 实读值设定 |
| `ToggleCalls` / `UpdateCalls` / `RestoreCalls` / `BoundCount` / `OriginalMaterialCount` / `TargetFadeForTest` / `BlendTimeForTest` / `CurrentBlendTimeForTest` / `DoFadeForTest` / `RenderersForTest` | 断言用（与 `TraitEffectController` 的 `*ForTest` 同款口径） |
| 两处「我们的」写法（都写在注释里） | ① `blendTime <= 0` 时**显式**当帧到位（原版 `dt/0 = ±Infinity` 结果相同，但 `dt=0` 时会出 **NaN**）；② 还原材质时的 `Destroy` 分 `Application.isPlaying ? Destroy : DestroyImmediate`（照 `Battle/ScenarioMaterialFader.cs` 的既有惯例） |

### ⑧·4 没查清（如实）

| # | 没查清 | 要补的话 |
|---|---|---|
| 1 | ~~原版 expiring 实例那个 null renderer 到底会不会被走到~~ | ✅ **已查清（第三轮 · 见 §⑨）**：**主路径不可达（已证死）** · **边角路径结构上可达、但未穷尽** ⇒ 按「可达/不可达/判不了」三选一归 **「判不了（只差最后一步：枚举卡效）」**，机制本身已全部读死 |
| 2 | `Renderer.GetMaterial()` 的**定义体**仍未找到（N13 用 il2cpp dump 判了 `= renderer.material`；本批又从 `Update.c:28` 得到同样的间接证据） | 若要 100% 逐字，得解 `GetMaterial` 那个 helper 的方法体 |
| 3 | 其余 **26 个实例**的 `renderers[]` 与三个开关值（本批只读了 `MarkerlightIdle` 那两个） | 逐 prefab 扫；只有将来别的线要用这个组件时才需要 |
| 4 | 我们导入的 prefab 里 **0 个 `MaterialFader` 组件**（`grep MaterialFader *.prefab` 空） | W10 必须**运行时加**两个实例并绑（见下） |

### ⑧·5 W10 接线清单（下一批；本批一条没做）

1. 在 `MarkerlightIdle` 实例的 **`Normal State/Fixed`** 与 **`Expiring State/Fixed`** 两个节点上各 `AddComponent<MaterialFader>()`
   （⚠️ 我们的 prefab 里没有这两个组件；原版实例就挂在这两个 `Fixed` 上，见 F-E）。
2. 各自 `BindRenderers(...)` —— 原版是**精确 4 条**：
   * normal → `Base VFX` / `Ground Effect` / `Ground glow pulse` / `Grid floor`
   * expiring → `Base Vfx` / `Ground Effect` / **`null`** / `Grid floor`（我们的实现允许 null，会跳过并出声）
3. 各自 `ApplyPrefabFlags(false, false, true)`（F-F）。
4. 把 `MarkerlightController` 的两个字段类型从 `MonoBehaviour` **改回 `MaterialFader`** 并赋值；
   `AimingAttack` 里那句 `LogError` 换成原版 `fader.Toggle(!option, fadeTime)`。
5. 断言（可与 A-ML2 合并）：`ToggleCalls` / `TargetFadeForTest`（`AimingAttack(false)` ⇒ 1，`(true)` ⇒ 0）·
   `UpdateCalls` 随 `CardTween`/`Time` 推进 · `BoundCount == 4`（null 也算一条）· 淡出后 `disableOnFadeOut`
   生效 ⇒ 4 个 renderer 里非 null 的 `enabled == false`。⚠️ 批处理里 `Update` 不自动跑 ⇒ 用反射显式调 `Update` 或直接断字段。

---

## ⑨ 追加调查：那个 `null` renderer 到底会不会被走到（第三轮 · **只读**，未改任何代码）

> 目的：判 `MarkerlightController` 的 **expiring fader**（`renderers[2] == null`）会不会被 `Toggle` 碰到
> ⇒ 决定「原版这里会抛 NRE」是**死分支**还是**真缺陷**。方法：`cpp2il/out_cpp2il_dev_ilspy`（方法体全）为主 +
> `decomp_full`（调用序列 / 字段偏移）交叉，两份都读过再下结论。

### ⑨·1 证据链（六条，逐条带出处）

| # | 事实 | 出处 |
|---|---|---|
| **1** | `AimingAttack` 的**唯一上游** = `SetTraitsOnPlayAttackingStatus(hideStatus)`；后者的**唯一调用者** = `PrepareAttack` 协程的两个分支：`toPrepareAttack == false` ⇒ `hideStatus: false`；`toPrepareAttack == true` ⇒ `hideStatus: true` | `CardScript.cs:11298-11333`（方法体）· `CardScript.cs:3637 / :3649`（两个分支）· 交叉 `CardScript._PrepareAttack_d__353__MoveNext.c:41 / :65` |
| **2** | `PrepareAttack(true)` = **攻击开始**（拖拽起手 `CardScript.cs:10391`、`SetupAttack :12416`）；`PrepareAttack(false)` = **结束/取消攻击姿态**（`CancelAttack :12512`、`FinishActiveAbility :12529`） | 同上 ILSpy 四处 `StartCoroutine(PrepareAttack(...))` |
| **3** | `MarkerLightUsed` 的**唯一上游** = `SetRemoveMarkerlight()`（`CardScript.cs:11260-11296`）；而 `SetRemoveMarkerlight` 全库**唯一调用点** = `_ResolveAttack` 的 `case 0x1a`。它作用的对象 = 协程字段 `+0x130`，该字段在 `case 0` 里由 `+0x30` 拷贝而来，而 `+0x130` 在同一协程里就是**吃伤害的那张卡**（`ReceiveDamage(+0x130, …)`）⇒ **= 被攻击方（目标）**，不是攻击者 | `grep "CardScript__SetRemoveMarkerlight("` → 仅 `BattleManager._ResolveAttack_d__438__MoveNext.c:1633` · 同文件 `:110`（拷贝）· `:999`（`ReceiveDamage`） |
| **4** | ⇒ `alreadyAttacked = true` 只发生在**被打的那张卡**身上；而 `AimingAttack` 只在该卡**自己发起攻击**时被调 | #1 + #3 |
| **5** | 标记何时消失：`OnTurnEnd` 在 `removeMarkerlightAtEndOfTurn && CurrentMarkerlight > 0` 时 `RemoveActiveEffect(0x492)` + `RemoveTrait(0x492)`；而 **`RemoveTrait` 内部立刻** `CardUI.RemoveStatusAnim(trait)` + `CleanStatusAnims()` ⇒ **框在摘 trait 的同一拍被 `Destroy`**（`MarkerlightIdle` 无 `AnimFXController` ⇒ 走 `Destroy`） | `CardScript__OnTurnEnd.c:135-141` · `CardScript.cs:19394-19403`（`RemoveTrait` 体）· 判据 §1.2（无 `AnimFXController` ⇒ `Destroy`） |
| **6** | `OnTurnEnd` 的广播范围 = **全场面 + 双方手牌**（先遍历场上卡，再 `GetCardsInHandRef(IsPlayerTurn())` 与 `!IsPlayerTurn()` 两副手牌）⇒ **标记在「施加它的那一回合」结束就被摘掉** | `BattleManagerSupport__BroadcastTurnEnd.c:20-63` |

**由 1–6 直接推出（主路径）**：被标记单位**下一次正常攻击**时，旧框早已销毁；若是新标记，新框的 `Start()`
把 `alreadyAttacked` 重置为 `false` ⇒ 走 **normal fader** ⇒ `null` 那个条目**不会被 `Toggle` 碰到**。
（另外：两处 `_ResolveAttack` 里的 `CancelAttack` 都在 :172 / :299，**早于** `case 0x1a`(:1633)，只是中止路径。）

### ⑨·2 边角路径（结构上可达，**未穷尽**）

| 路径 | 为什么能碰上 expiring fader | 出处 |
|---|---|---|
| **同回合内被卡效强制再攻击** | `SetRemoveMarkerlight`（= 置 `alreadyAttacked=true`）之后、该回合结束之前，若该单位被 `AddScriptedAttack` / `AddRedirectedAttack` / `AddAttackAction` 拉去攻击 ⇒ 它们都先 `SetupAttack(...)` ⇒ `PrepareAttack(true)` ⇒ `AimingAttack(true)` ⇒ **expiring fader** ⇒ `Toggle` ⇒ 撞上 `renderers[2] == null` | `BattleManager.cs:40151`（`AddScriptedAttack`）· `:40243`（`AddRedirectedAttack`）· `:36480`（`AddAttackAction`）；三处内部都调 `SetupAttack(playSound:false)` |
| **同回合内的 `CancelAttack`** | `OnTurnEnd` 的 `CancelAttack()` 在 **`RemoveTrait(0x492)` 之前**（`.c:45` vs `:137`）⇒ 若被标记单位在**回合结束时仍处于攻击姿态**，那一刻框还在、`alreadyAttacked==true` ⇒ `AimingAttack(false)` ⇒ expiring；`Stun` 同理 | `CardScript__OnTurnEnd.c:45 / :137` · `CardScript.cs:20213-20220`（`Stun`） |

⚠️ 这三处 API **我证明了存在**，但**没有逐张卡枚举**「有没有卡能在当回合让**被标记单位**再攻击 / 在它身上触发 `CancelAttack`」
（那是查卡效数据，不是查反编译）。**这就是「差最后一步」的全部内容。**

#### ⑨·2·b 第三轮追加：强制攻击是**通用 API**（把「还剩什么」缩到最小）

| 事实 | 出处 |
|---|---|
| 真正的**入口**不是 `AddScriptedAttack`，而是 **`AddForceAttack(CardScript targetCard, …)`** —— 🔴 **第一个参数就是「被强制攻击的那张卡」**，而且它会先 `targetCard.RemoveAttackThisTurn()`（把这回合的攻击次数退掉） | `BattleManager.cs:39952-39983` |
| 另一入口 `AddForceAttackFriendlyUnits(CardScript targetCard, …)`（友方单位） | `BattleManager.cs:40079` |
| 两者分别经 `BattleActionType.forceAttack` / `forceAttackFriendly` 入队 → `ResolveForceAttack`（`BattleManager.cs:26531`）/ `ResolveForceAttackFriendly`（`:26629`）→ 内部 `AddScriptedAttack(...)` | `BattleActionType.cs:45 / :69` · `BattleManager.cs:7483 / :7687` |
| ⇒ **语义上「让任意一张卡当回合再攻击一次」是存在的**；差的只是「**哪张卡**有这样的效果、能不能选到**敌方**那张被标记的卡」 | —— |
| ⚠️ 五个入口（`AddForceAttack` / `AddForceAttackFriendlyUnits` / 三个 `Resolve*` 协程）在 ILSpy 与 `decomp_full` 里**都是 0 处文本调用点** ⇒ 走**间接派发**（方法表/委托）⇒ **只能从卡效表侧查**，反编译这边已经查到底了 | `grep "AddForceAttack("` 只命中定义与它自己的日志字符串 |

### ⑨·3 结论（三选一，明确到字）

> **主路径：不可达（已证死）· 边角路径：可达（结构上）但未穷尽 · 整体裁定：判不了（只差枚举卡效）**
>
> **⛔ 不是「大概」**：机制侧（谁调谁、谁被摘、框何时销毁）已经**逐条读死**（⑨·1 六条）；
> 唯一没做的是一件**不需要再读反编译**的事 —— 枚举 **`AddForceAttack`（`BattleManager.cs:39952`）/
> `AddForceAttackFriendlyUnits`（`:40079`）** 的**卡效调用方**（判「有没有卡能让**敌方**那张被标记的卡当回合再攻击」）。
> ⚠️ 反编译这边已经**查到底**：这五个入口全是**间接派发**（0 处文本调用点，见 ⑨·2·b），再往深处读也不会有新信息。
>
> ⚠️ **附带一处没定位到（不影响本条结论）**：`BattleManager.ResolveAttack` 的**调用点**在 `decomp_full`
> 与 ILSpy **都是 0 处**（`grep "BattleManager__ResolveAttack("` / `ResolveAttack(` 都只命中定义），
> ⇒ 它只能被**委托/间接**调用。所以「**正常攻击收尾到底走 `CancelAttack` 还是 `FinishActiveAbility`**」我没定位到。
> 但这不改结论：主路径的排除靠的是「**标记只作用在被打方**（⑨·1 #3）+ **摘 trait 立刻销毁框**（#5）」，与收尾走哪条无关。

### ⑨·4 两种结果的处置（按调度台裁定预先写死）

* **若枚举结果 = 没有任何卡能这样** ⇒ 这条记为「**死分支**」：账里写「**原版会抛 NRE，但我们不照抄崩溃**」；
  我们 `MaterialFader` 里的「跳过 null + `LogError` 一次」这处偏离**无害**。
* **若枚举结果 = 有卡能这样** ⇒ **原版此处本身就是坏的**：`NullReferenceException` 会把那次 `PrepareAttack` 协程的**剩余步骤全部中断**
  （`MinionInitiatingAttack` 音效 / `DOLocalMove`·`DORotate` / `WaitForSeconds` 都不再执行；Unity 只在 Console 记一条异常）⇒
  账里按「**原版行为 = 崩溃，我们不复刻崩溃**」写清，并作为「**原版有缺陷**」案例上报；**⛔ 不许照抄**。
  （⚠️「协程里抛异常会中断该协程」是 Unity 的既定行为，本条**未实机验**。）

### ⑨·5 一条必须同账的关联（否则会误判）

我们引擎侧 **F2**（`RuleCore.cs:6884` 打完伤害就 `RemoveAll("markerlight")`）与原版「**留到回合末**」不同
⇒ **在我们当前实现下，Expiring 状态几乎不存在**，边角路径也基本碰不到；**但一旦把 F2 改成原版口径
（标记留到回合末），边角路径就重新暴露**。所以 ⑨·4 的裁定必须和 F2 写在同一处，⛔ 别分开记。
（✅ 已落账：`账/战斗逻辑驱动.tsv` 的 **`CardScript.SetRemoveMarkerlight()` 那一行备注**写了完整绑定条款，
`CardScript.OnTurnEnd(bool playerTurn)` 那一行只留一句指针 —— 铁律 6，只留一处。）

---

## ⑩ 交接（N15 收尾 · 给下一批/给调度台并入会话交接）

> ⚠️ 我**没有**新建/覆盖 `资料/交接_第N会话.md` —— 那份是主对话的写手（铁律 13·3「一个文件一个写手」/ 13·8「正本只有主对话能写」）。
> 下面这段是自包含的，调度台可以**整段并入**会话交接，或直接让下一批看本报告。

### ⑩·1 本轮产出（全部是「新增」，⛔ 零接线、⛔ 零改动既有文件）

| 文件 | 状态 | 入口/关键成员 |
|---|---|---|
| `MyGame/Assets/CardPresentation/Core/TraitEffectController.cs` | 新增 | `abstract TraitEffectController`（`Toggle` / `UpdateValue` / `AimingAttack` + `currentValue` 死字段）· **`TraitEffectControllers.Attach(GameObject, prefabName)`**（按 prefab 名、幂等，**0 调用点**） |
| `…/Core/HuntMarkController.cs` | 新增 | `counter`（`[SerializeField]` + 按名绑）· `UpdateValue` = 写文本 + `DOPunchScale(one*0.1, 0.5, 10, 1.0)` · `AimingAttack` **throw** · 观测量 `PunchCount/UpdateCalls/LastValue/TextForTest/Bound` |
| `…/Core/MarkerlightController.cs` | 新增 | 9 字段 + `Start`（两态 + `_RippleScale`）· `UpdateValue` · `MarkerLightUsed` · `ChangeState`（原版死代码）· `AimingAttack` = **出声的未实现出口** · 观测量 `FloorsBound/FloorsForTest/FadeTimeForTest/AlreadyAttackedForTest` |
| `…/Core/MaterialFader.cs` | 新增 | 4 序列化 + 5 运行时字段 · `Update/Toggle/CacheOriginalMaterials/RestoreOriginalMaterials/FadeIn/FadeOut/SetTargetFade/ToggleRenderers` · **`BindRenderers` / `ApplyPrefabFlags`** · 观测量 `ToggleCalls/UpdateCalls/RestoreCalls/BoundCount/OriginalMaterialCount/TargetFadeForTest/DoFadeForTest/CurrentBlendTimeForTest/RenderersForTest` |
| `…/Editor/TraitEffectAssert.cs` | 新增 | `-executeMethod TraitEffectAssert.Run` · 8 组 / 36 个 `Check`，全用**假 GO + 假 TMP**，不依赖真 prefab |
| `资料/完全复刻/账/战斗逻辑驱动.tsv` | 改 **2 行备注** | `CardScript.SetRemoveMarkerlight()`（F2 绑定条款全文）· `CardScript.OnTurnEnd(bool playerTurn)`（指针） |

### ⑩·2 🔑 下一批简报的「已知什么」—— 直接抄这 8 条（别再重新查）

1. **只有 2 个 prefab 带控制器**：`HuntMark_IdleEffect` / `MarkerlightIdle`；**按 prefab 名挂，⛔ 不按 trait 名**。
2. **`MarkerlightIdle` 的两个 `MaterialFader` 节点** = `Normal State/Fixed` 与 `Expiring State/Fixed`（⛔ 不是都在 Expiring State；账原来那句错一半）。
3. **两个 fader 的 `renderers[]` 各 4 条**：normal = `Base VFX`/`Ground Effect`/`Ground glow pulse`/`Grid floor`；expiring = `Base Vfx`/`Ground Effect`/**`null`**/`Grid floor`。
4. 🔴 **`ApplyPrefabFlags(false, false, true)` 必须调**（prefab 把 `restoreOriginalMaterialsOnFadeOut` 覆写成 0，而 C# 默认 true）。
5. 🔴 **`renderers[i] == null` 是原版会抛 NRE 的分支**（expiring 实例真有 null）⇒ 我们的处置 = **跳过 + `LogError` 一次**（有意的偏离，见 §⑧·2 F-G / §⑨）。
6. **`Grid floor` 不是根的直接子件**（`…/Fixed/Grid floor`）⇒ 绑定用 `GetComponentsInChildren<ParticleSystemRenderer>(true)` + 名字过滤；`Counter` / `Normal State` / `Expiring State` **是**直接子件。
7. **`MarkerlightIdle` 的 `fadeTime` = 0.5**（prefab 覆写），**⛔ 不是 ILSpy 的 1f**。
8. **`UpdateValue` 不是每帧刷**：原版每次调用就弹一次，**防抖在 `TraitFrames` 的帧锁（W8）**，⛔ 别塞进控制器（否则 A-E2 / W7 会互相抵消）。

### ⑩·3 下一批的活（依赖已标；详细表见 §⑥ 与 §⑧·5）

| 批 | 活 | 文件所有权 | 依赖 |
|---|---|---|---|
| ① | `P0-1`（持久框 `autoDestroySeconds=+∞`）+ `P0-2`（`SyncKeywords(…, Func<string,int>)` 重载 + `BattleDriver:25396` 1 行） | `Core/TraitFrames.cs` · `Battle/BattleDriver.cs` | — |
| ③ | `Attach` + 四个入口 E1–E4 + `ReplayAndRefresh` + `EvtKind.StatusApplied`（**只差传值**）+ 帧锁 | 同上两个文件 | ① ② |
| ④ | 8 条断言接线（A-E3 原版形 / A-E2 的 `InstanceId`·`PlayCount` / **A-LIFE** / 灭自证 1 原版形 / 局面哈希） | 新建或扩 `Editor/TraitEffectAssert.cs` | ③ |
| ⑤ | `MaterialFader` 接线（`AddForceAttack` 那套与它无关）：两个实例 AddComponent + `BindRenderers` + `ApplyPrefabFlags` + `MarkerlightController` 字段类型改回 + `AimingAttack` 真实现；`MarkerLightUsed` 的引擎事件 | `Core/MarkerlightController.cs` 等 | ③ |
| ⏳ | **F2 口径**（markerlight 留到回合末）——**动它之前**先做账里那条绑定条款要求的一次**卡效枚举** | `RuleEngine/Core/RuleCore.cs` | 用户/调度台拍板 |

### ⑩·4 现状与红线

* **typecheck**：本批最后一次 `TMPDIR=/tmp/wf_n15 bash d:/4/Unity/工具/typecheck.sh` = **运行时 0 / 编辑器 0**
  （复跑途中撞到过别的代理的半成品：`Shell/ChatPanel.cs` → `WarpforgeArena1/Editor/ArenaBuilder.cs` → `RuleEngine/Core/EffectResolver.cs`，⛔ 都不是本批文件）。
* ⛔ **本轮没跑 Unity**（所以组件绑定 / `_RippleScale` / punch 的实机效果**未验**）· ⛔ 没动 git · ⛔ 没改正本（`CLAUDE.md` / `项目任务.md`）· ⛔ 没改任何既有 `.cs`。
* **断言现在跑应该是 8 组全绿，但那**只**证明「这一层自己没写错」**（见 §⑦·3）—— ⛔ 别当成「接上之后对」。
