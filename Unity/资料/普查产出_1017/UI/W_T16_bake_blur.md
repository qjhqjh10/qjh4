# W_T16 · 离屏烘焙 + UI Blur（执行写手报告）

> 2026-10-17 · 界面线 `T16` 的「离屏烘焙 + UI Blur」那一半。
> 判据正本 = `资料/普查产出_1017/UI/D_T16_UIBakeScreen判据.md`（本次逐条照做）。
> ⛔ 按用户口径「先复刻完再验」：**本会话没跑 `*.Run`、没起 player**，只跑了秒级类型检查。

---

## 一、结论

1. `Shell/UIBakeScreen.cs` 已建（类名 == 文件名），行为逐条对原版
   `d:/2/tools/decomp_full/UIBakeScreen__{Start,Toggle,CreateRT,ResolutionChanged,OnDestroy}.c`。
2. `Shell/ShellRuntime.Build()` 在**全部壳 UI 之前**建了
   `Background Render Texture Holder`（原版节点名），出厂不显示、**不登记命中**（子树里没有 `WindowButton`
   = 原版 `RawImage.m_RaycastTarget = 0`）。
3. 开关时机按原版 `MainMenuWindow` 那四跳落进 `GameWindow`（共同基类，闸门 = 是不是 `WindowsManager.BaseMenu`）：
   `ToBackground` / `Hide` / `Close` ⇒ 先烘；`TryOpen` 的 `Closed` / `Background` 两支（= 原版 `ToFocus`）⇒ 关。
4. 新建 `Shaders/UIBlur.shader`：两趟可分离高斯（pass 0 横向 / pass 1 纵向），属性**只有**
   `_MainTex` / `_Sigma` / `_steps`；`Blend Off` 硬编码（不走间接寻址）。
5. `Editor/ShellScene.cs` 加了 **2 段 / 24 条**断言（出厂态 + Toggle/RT/分辨率 + 开关时机），期望值全是原版字面量。
6. `bash d:/4/Unity/工具/typecheck.sh` = **0 / 0**（原文见 §六）。

---

## 二、改了什么（逐文件）

| 文件 | 改动 |
|---|---|
| **新建** `MyGame/Assets/CardPresentation/Shell/UIBakeScreen.cs` | 原版 `UIBakeScreen` 的等价物：`cameraToBake` / `image`（`ImageQuad`）/ `blitBlurMaterial` 三个原版字段；`CreateRT`（`ceil(屏×0.25)` · depth 24 · `image.texture = RT`）；`Toggle(bool)`（没 RT 现建 → `GetTemporary(w,h,0x10)` → 相机渲进 RT → 两趟 `Graphics.Blit`（pass 0/1）→ `Release` → `image` 开关）；`ResolutionChanged()`；`Update()` 里轮询分辨率（代替原版那条信号）；`OnDestroy` 销毁 RT。另加 `static Current`（批处理/编辑态 `ShellRuntime.Instance` 恒 null 时的取件口，形状照 `LoadingMenu.Current`）、只读 `RT` / `Showing` / `BakeCount`。 |
| **新建** `Shell/UIBakeScreen.cs.meta` · `Shaders/UIBlur.shader.meta` | 与仓库既有 `.cs.meta` / `.shader.meta` 同格式（Unity 下次导入不会另起 GUID）。 |
| **新建** `MyGame/Assets/CardPresentation/Shaders/UIBlur.shader` | 两趟可分离模糊；属性只有 `_MainTex` / `_Sigma`(2.25) / `_steps`(10)；`Blend Off` / `ZWrite Off` / `ZTest Always` 硬编码；**没声明**任何原版残留死值属性。 |
| `Shell/ShellRuntime.cs` | ① 新增 `public UIBakeScreen Bake`；② `Build()` 里（相机 `LayoutSpace.Apply` 之后、全部 UI 之前）建 `Background Render Texture Holder` 并显式 `Bake.Build()`（批处理里 `Awake` 不跑）；③ `Dump()` 加一格「模糊快照 显示中/关」。 |
| `Shell/GameWindow.cs` | 新增 `IsBaseMenu` + `SetBake(bool)`（闸门 = `Manager.BaseMenu == this`；找不到件时**只在「壳在、件不在」时出声一次**）；钩子：`ToBackground()` 首句、`Hide()` 守卫之后、`Close()` 守卫之后、`OpenByState` 两支（`Closed` / `Background`，= 原版 `ToFocus`）、`ReopenFromBackground()`。 |
| `Editor/ShellScene.cs` | **只加本件断言**（两段，见 §四）：①·b 出厂态（紧跟在 `Build()` 之后 —— 后面有 `baseMenu` 探针会把烘焙点亮，早断才真）；末尾「`Toggle`/`CreateRT`/分辨率」+「开关时机」。既有断言一个都没动。 |

⛔ **没碰**：`Shell/MainMenuWindow.cs`（不在白名单）、`WindowsManager.cs`、`Core/**`、`Battle/**`、`Deck/**`、`项目任务.md`、`CLAUDE.md`、`资料/任务线/**`、`d:/2/**`、git。

---

## 三、判据对照（原版 vs 我们，逐条）

| 原版（出处） | 我们 | 状态 |
|---|---|---|
| 字段 `cameraToBake` / `image` / `blitBlurMaterial`（MB 1976 字段名逐字） | 同名字段；`image` = `ImageQuad`（原版是 uGUI `RawImage`） | ✅ |
| `RENDER_TEXTURE_MULTIPLIER = 0.25f`（`CreateRT.c` 的 `DAT_1834b2e7c`） | `UIBakeScreen.RENDER_TEXTURE_MULTIPLIER = 0.25f` | ✅ |
| `w = ceil(Screen.width * 0.25)`、`h = ceil(Screen.height * 0.25)` | 逐字同式；`Screen` 为 0 时兜到 1 并**出声**（原版没有这一档，已在注释标明） | ✅（+ 一档兜底，已写明） |
| `new RenderTexture(w, h, 0x18, 0)` | `new RenderTexture(w, h, 24, RenderTextureFormat.Default)`（`0` = ARGB32 = Default） | ✅ |
| 设 RT 名字（字符串常量 `DAT_1842a4768` **没读出**） | 用节点名 `Background Render Texture` —— **我们挑的**，注释已写「别当成原版的」 | ⚠️ 见 §五 |
| `RawImage.texture = RT` | `image.SetTexture(RT, keepAspect:true)` + `image.SetAspect(w/h)`（`ImageQuad` 只换图不重建网格 ⇒ 必须显式改比例，注释已写） | ✅（等价） |
| `Toggle(true)`：没 RT ⇒ `CreateRT`；`GetTemporary(w,h,0x10)`；相机渲进 RT；`Blit(RT,tmp,mat)`；`Blit(tmp,RT,mat,1)`；`Release(tmp)`；`image.enabled = on` | 逐条同序；相机那一步 = `cam.targetTexture = RT; cam.Render();`（原版是 URP `SingleCameraRequest`）；`try/finally` 恢复 `targetTexture` | ✅ |
| `Toggle(false)`：只 `image.enabled = false` | `image.gameObject.SetActive(false)`（= 关 Renderer 所在物体；`ImageQuad.enabled` 关不掉 MeshRenderer） | ✅（等价，注释已写） |
| `Start()`：注册分辨率信号 + `image.enabled = false` | `Build()` 里显式 `image.gameObject.SetActive(false)`；分辨率改为 `Update` 里轮询（没有那条信号） | ✅（简化，注释已写） |
| `ResolutionChanged()`：`image.enabled == false` ⇒ `CreateRT`；否则不动 | `if (Showing) return; CreateRT();` | ✅ |
| `OnDestroy()`：`Destroy(renderTexture)` | `Release()` + `Destroy`/`DestroyImmediate`；另把我们 `new` 的材质一起销毁 | ✅（+ 材质，注释已写） |
| 材质 `UI Blur`：真属性只有 `_MainTex` / `_Sigma 2.25` / `_steps 10` | shader 只声明这三样；`Build()` 里 `SetFloat("_Sigma", 2.25)` / `SetFloat("_steps", 10)`；残留死值属性一个都不接 | ✅ |
| 两趟 Blit 的 pass 0 / pass 1 | shader pass 0 = 横向、pass 1 = 纵向 | ✅ |
| `ToBackground()`：`Toggle(true)` → `base.ToBackground()` → `SetActive(false)` | 基类 `ToBackground` **首句** `SetBake(true)`（`MainMenuWindow` 覆写先调 base 再 `SetActive(false)` ⇒ 次序等价） | ✅ |
| `ToFocus()`：`SetActive(true)` → … → `Toggle(false)` | `OpenByState` 两支在 `SetActive(true)` 之后 `SetBake(false)`；`ReopenFromBackground` 同样 | ✅ |
| `Hide()`：`Toggle(true)` → `base.Hide()` | `GameWindow.Hide()` 守卫之后 `SetBake(true)` | ✅ |
| `Close()`：`Toggle(true)` → `base.Close()` | `GameWindow.Close()` 首句守卫之后 `SetBake(true)`（⛔ 不在「本来就没开」时烘一张别的窗的快照 —— 已写理由） | ✅（有意的次序差，已写） |
| 只有 `MainMenuWindow` 带 `screenBaker` | 闸门 = `Manager.BaseMenu == this`（不看类型 —— 独立场景/夹具没有 baseMenu 时不该凭空烘） | ✅ |
| `m_RaycastTarget = 0` | 整屏 quad 子树**没有 `WindowButton`**（不登记命中） | ✅ |
| 渲染队列：原版靠排序层/兄弟序 | 我们挑 **2895**（`UIBakeScreen.DisplayQueue`）：2890（压暗 `FadeQueue`）< 2895 < 2900（主菜单 `QBg` / 全部窗的起点） | ✅（值是我们起的，理由写进常量注释） |
| 先烘、再显示新窗 | `WindowsManager.OpenWindow` 在 `win.TryOpen(...)` **之前**调 `ToBackground()` / `HideAllWindows()` ⇒ 快照里不会带上新窗（代码路径，已写进注释） | ✅ |
| 烘之前把模糊 quad 自己关掉 | `Toggle(true)` 里先 `image.gameObject.SetActive(false)` 再渲 | ✅（原版没有这一句，注释标为**有意差别**、防自反馈） |

---

## 四、断言（`Editor/ShellScene.cs`，只加本件）

**①·b（紧跟 `Build()`，出厂态）**

1. 壳里建了 `Background Render Texture Holder`（原版节点名）。
2. 它下面有整屏图。
3. 整屏图可见高 = 10 世界单位（= 1080px 设计高）。
4. 出厂**不显示**（原版 `Start: image.enabled = false`）。
5. 出厂**还没有 RT**（原版 `CreateRT` 是懒建）。
6. **不登记命中** —— 子树里 `WindowButton` 数 = 0（原版 `m_RaycastTarget = 0`）。
7. 渲染队列满足 `2890 < q < 2900`（**梯子约束**，两侧是别的件的号）。
8. 模糊材质在、shader 名 = `CardPresentation/UIBlur`。
9. `_Sigma = 2.25`（原版 `Material_3.json` 实读）。
10. `_steps = 10`（同上）。
11. 原版那些**残留死值属性**（`_BlurRadius`/`_GridSize`/`_Iterations`/`_xBlur`/`_yBlur`/`_Spread`）**一个都没接**。

**末尾：`Toggle` / `CreateRT` / 分辨率变化**

12. （归零）`Toggle(false)` 之后不显示。
13. `Toggle(true)` 之后 RT 在位。
14. RT 宽 = `ceil(Screen.width × 0.25)`（原版字面量 0.25）。
15. RT 高 = `ceil(Screen.height × 0.25)`。
16. RT 深度 = **24**（原版 `0x18`）。
17. `Toggle(true)` 之后**显示**。
18. `Toggle(false)` 之后**关掉**。
19. `ResolutionChanged()` 在**不显示**时重建 RT（对象身份变了）。
20. **显示中** `ResolutionChanged()` 不动 RT（对象身份不变 —— 与 19 成对，灭自证）。

**末尾：开关时机（`MainMenuWindow` 那四跳）**

21. 探针常驻主菜单开起来 + 回前台 ⇒ 烘焙**关**。
22. 弹窗压背景 ⇒ 主菜单 `Background` 且物体不活、烘焙**开**、`BakeCount > 0`（**真的烘过**，不是只翻状态）。
23. 关弹窗 ⇒ 主菜单回前台、烘焙**关**（=`ToFocus` 那一跳）。
24. 全屏窗 ⇒ `HideAllWindows` 把主菜单藏成 `Closed`、烘焙**开**；关掉全屏窗 ⇒ 主菜单回 `Open`、烘焙**关**。
25. 闸门：普通窗 `ToBackground()` **不碰**快照（原版只有 `MainMenuWindow` 带 `screenBaker`）。
26. 主菜单自己 `Close()` ⇒ 留一张模糊底且自己藏掉（原版 `MainMenuWindow.Close`）。
27. （收尾）再开一次 ⇒ 快照关掉（与 26 成对，防「关了就再也关不掉」）。
28. 收尾还原 `baseMenu` + 销毁两个探针。

---

## 五、没查清 / 已知取舍（如实记）

1. **blur shader 的精确核没做**：`_Sigma` / `_steps` 的语义（高斯 σ / 采样步数）是从属性名 + 两趟 Blit 推的，
   **没有逐条读 `Shader_339` 的 DXBC**。本实现 = `w(k)=exp(-k²/(2σ²))`，k=1..steps，±k texel 归一化
   （σ=2.25、steps=10 ⇒ 21 抽）。shader 文件头已如实写「等价实现、未逐条核字节码」。
2. **RT 的名字没读出**：原版 `set_name` 的字符串常量（`DAT_1842a4768`）没解；我们用节点名，已在常量注释写明**是我们挑的**。
3. **`MenuFocusChangeActions(focused) → Toggle(!focused)` 没接**：本地反编译里**找不到它的调用方**
   （`decomp_full` 只有它自己的方法体；类桩 `MainMenuWindow.cs:175` 是空函数）—— 不知道是「应用窗口失焦」还是
   「导航焦点」驱动的，⛔ 没凭空发明信号。已在 `GameWindow.cs` 的注释里如实记。
4. **独立场景档没有烘焙件**：`MainMenu.unity` 直起 / `MainMenuScene` 自检里没有壳 ⇒ 没有 `UIBakeScreen`。
   原版主菜单 prefab 里 `screenBaker` 是烤好的引用，我们这一档如实缺失（`SetBake` 静默跳过，因为「没有壳」
   不是缺陷）；被窗托管的生产路径（壳 + 主菜单场景）**有**。
5. **RT format**：反编译形参位是 `0`，按 `RenderTextureFormat.ARGB32`（= `Default`）实现；⚠️ 没跑起来核过（本会话不起 player）。
6. **批处理/无图形环境**：`cam.Render()` / `Graphics.Blit` 若抛异常会被 catch 并 `LogWarning`（`LastBakeFailed = true`），
   状态机照常推进 —— 这样自检不会因为环境问题假红；真机上应当 `BakeCount` 递增、`LastBakeFailed == false`。
7. **shader 进 player 的剥离问题**：`Shader.Find("CardPresentation/UIBlur")` 与仓库既有自建 shader
   （`ArtOpaque` / `TraitDisabled`）同一条路；本件**没改** `ProjectSettings/GraphicsSettings.asset`（不在白名单）。
   若将来出 player 后模糊不生效，第一处要看的就是「这个 shader 有没有被打进包」。

---

## 六、typecheck 原文

```
$ bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

（按用户口径：**没跑任何 `*.Run`、没起 player、没打截图**。）
