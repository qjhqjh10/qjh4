# F · UI 两组件实现（UIBorderGlow / UITransformTweenController）

> 两个**新文件**（不碰别人的）：
> `CardPresentation/Shell/UIBorderGlow.cs` · `CardPresentation/Shell/UITransformTweenController.cs`
> （各带 `.meta`；**LF 行尾**，与 `Shell/*.cs` 一致）
> ✅ **秒级类型检查：运行时 0 错 · 编辑器 0 错**（`bash Unity/工具/typecheck.sh`）。
> ⛔ 不接线（H 的活，见 §3）；⛔ 没改账、没动 git、没跑 Unity。

---

## ① `UIBorderGlow`

### 1.1 先订正一条**我自己上一轮说错的**

我上一轮在《UI 特效三组件调查》里写「**α 区间未解**（`DoAnimation`/`DealyedAnim` 里有 `Unmanaged memory load`）」。
本轮把方法体读完，**这条要拆成两半**：

| 项 | 真相 | 出处 |
|---|---|---|
| **材质浮点区间** | ✅ **解出来了：`_Reveal` 从 `-0.5` → `1.0`** | `DoAnimation`：`instancedMaterial.SetFloat(_Reveal, -0.5f)` → `DOFloat(instancedMaterial, 1f, _Reveal, animTime)` |
| 属性名 | ✅ **`"_Reveal"`** | ctor `:477` `Shader.PropertyToID("_Reveal")` |
| 缓动 | ✅ **`Ease.Linear`** | `DoAnimation` `SetEase(Ease.Linear)` |
| graphic 自身 α | ✅ **播放瞬间置 `0`**，且**永不还原** | `DoAnimation` 末尾 `ChangeGraphicAlpha(0f)`；`graphicOriginalColor` 全文件只在 `Awake` 记过、**没有一处用它还原** |
| 那一行 `Unmanaged memory load` | ❌ **仍未解** —— 它是读**某个对象字段**（偏移 +68），不知道干什么 | `:203` `NoteDecompilerIssue("Unmanaged memory load: [this @ rcx (UIBorderGlow)+68]")`；本组件**不复刻这一行、也不猜** |

⇒ 所以「α 区间做成可注入字段」这句，**对材质浮点（`_Reveal`）不需要**（已直读）；
**对「我们自己那套没有 `_Reveal` 的材质」才需要** —— 见 1.2 的 ② 路。

### 1.2 本组件的两条路（**关键：不猜**）

| 路 | 条件 | 行为 |
|---|---|---|
| ① **原版口径** | 材质**有** `_Reveal` | `_Reveal` **−0.5 → 1**，延迟 `animDelay`、时长 `animTime`、**线性**；播的瞬间把 graphic α 置 `alphaOnPlay`（=0） |
| ② **替代路径**（**默认关闭**） | 材质**没有** `_Reveal`（我们的 `ImageQuad` 用 `Sprites/Default`，就是这种） | **`Debug.LogWarning` 出声**；**只有 `alphaBreathing=true` 时**才退化成 α 单向渐变（`alphaFrom → alphaTo`） |

🔴 **`alphaFrom/alphaTo` 的默认值不是原版值**（原版没有这条），注释里写死了这句。
`alphaBreathing` 默认 **false** ⇒ **没接线就什么都不动**（⛔ 不静默编一个区间）。

### 1.3 字段（每个都标了出处）

| 字段 | 值 | 出处 |
|---|---|---|
| `animTime` | **1f** | ctor `:476` |
| `animDelay` | 0 | 原版字段无初始化器；**89 个实例实测全 0** |
| `playOnEnable` | false | 同上；**实测 86/89 = 0**（只有 3 处奖励高亮 = 1） |
| `shaderRevealProperty` | **`_Reveal`** | ctor `:477` |
| `revealFrom` / `revealTo` | **−0.5 / 1** | `DoAnimation` |
| `alphaOnPlay` | **0** | `DoAnimation` 末尾 `ChangeGraphicAlpha(0f)` |
| `alphaBreathing` / `alphaFrom` / `alphaTo` | false / 1 / 0 | ⛔ **非原版**：替代路径的开关与区间 |

### 1.4 API（照原版）
`Play()`（:155）· `Stop()`（:219，**只停补间、不还原**，照原版）· `ChangeGraphicAlpha(float)`（:262）·
内部还有原版的两步：`CacheMaterial()`（:164，克隆渲染件材质；**拿不到就等一帧再试** → `DelayedAnim`）。

### 1.5 断言（2 条，含灭自证）
| # | 断言 |
|---|---|
| 1 | `SelfCheckWired()`：这个 GameObject 上有可着色件（`ImageQuad`/`Graphic`/`Renderer`），没有就 **`Debug.LogError`** |
| 2 | `SelfCheckProgression()`：**起播 ≈ `from`** · **0.5×T 严格落在 (from,to) 之间且离终点 >2%**（🔴 **灭自证**：谁把动画改成「直接置终值」，这条立刻红，而「动画跑完了」那条照样绿）· **T+容差 == `to`** |

---

## ② `UITransformTweenController`

### 2.1 判据（逐方法读的，230 行）

| 原版方法 | 语义（本类照做） |
|---|---|
| `Awake` :32 | **仅当 `initializeOnAwake`** 才记 `originalPosition/originalScale` |
| `OnEnable` :48 | **仅当 `playOnEnable`** ⇒ 记原值 + 按 `animPosition/animScale` 分派 |
| `Initialize` :70 | 只记原值（`initialized=true`） |
| `DoDefaultAnim` :79 | 按开关分派（不重记原值） |
| `BackToDefault()` / `(float)` :110/:127 | 补回 `originalPosition/originalScale` |
| `DoAnim` :146 | 位置 + 缩放各起一条 |
| **`DoAnimPosition` :154** | 🔴 **原版守卫：当前 `anchoredPosition` == 目标 ⇒ 直接 return，不起补间**；不等才 Kill 旧 + `DOAnchorPos`。**没有 `SetEase`** ⇒ DOTween 默认 **OutQuad** |
| **`DoAnimScale` :180** | 同一套守卫 + `DOScale` |

### 2.2 参数（**Forge 实例真值**，另 14 个在暂停线）

| 字段 | 值 | 说明 |
|---|---|---|
| `animTime` | **1.5**（类默认 **1f**，ctor） | |
| `animPosition` / `animScale` | **1 / 1** | 两条都开 |
| `targetPosition` | **(0, 217.5)** | `anchoredPosition`（uGUI 设计 px） |
| `targetScale` | **(1.12, 1.12, 1.12)** | 类默认 `Vector3.one`（ctor） |
| `initializeOnAwake` | **0** | 所以 `BackToDefault` 前必须显式 `Initialize()`（原版也这样） |

### 2.3 API（照原版）
`Initialize()` · `DoDefaultAnim()` · `DoAnim(pos,scale,time)` · `DoAnimPosition(pos,time)` · `DoAnimScale(scale,time)` ·
`BackToDefault()` / `BackToDefault(time)`。另加一个原版**没有**的 `Stop()`（只在注释里标明「原版没有，本类补的停止口」）。

### 2.4 断言（3 条，含**两条**灭自证）
| # | 断言 |
|---|---|
| 1·**守卫** | `SelfCheckGuards()`：把目标设成**当前值**各调一次 ⇒ **两条补间计数都不许增加**。🔴 **灭自证**：删掉守卫就会红，而「动画能跑」照样绿 |
| 2·**渐进** | `SelfCheckProgressionPosition()`：**0.5×T 位置必须严格在起终点之间**（挡「一帧到位」）· **T+容差到终点** |
| 3·**回位** | `SelfCheckBackToDefault()`：`Initialize()` → 动 → `BackToDefault()` 必须回到**捕获那一刻**的值 |

---

## ③ 给 H 的接线说明（三行，每个组件各一段）

### 3.1 `UIBorderGlow`
```csharp
// ① 挂在「该发光的那块」上（我们这边 = 九宫格 ImageQuad 那个节点）
var glow = node.AddComponent<CardPresentation.UIBorderGlow>();
glow.playOnEnable = false; glow.animTime = 1f; glow.animDelay = 0f;   // 原版 3 个字段
// ② 原版口径（材质有 _Reveal）→ 什么都不用配，`glow.Play()` 即可；
//    我们的 ImageQuad 是 Sprites/Default（无 _Reveal）⇒ 要么换带 reveal 的材质，要么显式开替代路径：
glow.alphaBreathing = true; glow.alphaFrom = 1f; glow.alphaTo = 0f;   // ⛔ 这两个值不是原版，按观感自己给
// ③ 播：`glow.Play()`；停：`glow.Stop()`（原版 Stop 不还原 α，要还原自己 ChangeGraphicAlpha(1f)）
```
**原版谁会播**（现读）：只有 3 处「奖励可领取」——`Expansion Pass / Forge / Raid` 的
`Collect Highlight Effect`（`playOnEnable=1`，或由 `ExpansionPassRewardItemContainer.cs:316` /
`RaidEventRewardItemContainer.cs:284` 显式 `Play()`）。**其余 86 个实例都是静帧**，不用接。

### 3.2 `UITransformTweenController`
```csharp
// ① 挂在奖励条目的 `RewardTransform`（RectTransform）上
var tw = rewardTransform.gameObject.AddComponent<CardPresentation.UITransformTweenController>();
tw.animTime = 1.5f; tw.animPosition = true; tw.animScale = true;
tw.targetPosition = new Vector2(0f, 217.5f); tw.targetScale = new Vector3(1.12f, 1.12f, 1.12f); // Forge 实例真值
// ② `initializeOnAwake=0`（原版也是 0）⇒ 播之前先 `tw.Initialize()` 记原值
tw.Initialize(); tw.DoDefaultAnim();          // 或只播一条：tw.DoAnimPosition(...) / tw.DoAnimScale(...)
// ③ 要回到原位：`tw.BackToDefault()`（它会重新捕获原值 —— 原版同一条）
```
⚠️ 另一处别忘：`animTime/targetPosition/targetScale` 每个实例都不同（15 个实例可逐个现读），
**别把 Forge 这组值抄给 Daily / ExpansionPass / Raid** —— 那 14 个都在暂停线，恢复时现读。

---

## ④ 没查清 / 边界

1. **`UIBorderGlow.DoAnimation` 里那一行 `Unmanaged memory load [this+68]`**（:203）—— 仍未解，
   本组件**不复刻它、也不猜它做什么**。若将来发现它才是「呼吸」的来源，本组件的 ① 路要补一条。
2. **`_Reveal` 的视觉含义**：本组件只保证「材质的 `_Reveal` 从 −0.5 走到 1」，**它长什么样取决于 shader**
   （原版 shader 已剥，我们也没这个属性）⇒ **观感无法离线验证**。
3. **`alphaBreathing` 这条路**：原版**没有**，纯属给「材质没有 `_Reveal`」的兜底；默认关。
4. **`UITransformTweenController` 只逐读了 1 个实例的字段值**（Forge）。其余 14 个未抄（都在暂停线）。
5. 两个组件都**没在 Unity 里跑过**（用户 2026-10-25 暂停自检）——断言写了但未执行。
