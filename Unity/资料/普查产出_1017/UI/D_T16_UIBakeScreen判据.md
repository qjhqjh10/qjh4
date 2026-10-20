# T16 · 离屏烘焙 + UI Blur —— 判据（2026-10-17 · 主对话现核）

> 这份是**复刻的输入**（「原版怎么做」），不是验收报告。
> 判据全部现场读过：prefab JSON + 反编译方法体。⛔ 没读过的地方写「没查清」。

---

## 一、原版对象（已查实）

| 项 | 值 | 出处 |
|---|---|---|
| 节点 | `Game UI` → `Main Canvas` → **`Background Render Texture Holder`**（GO 42 / RT 1118） | `bundle_scenes_scenes_mainmenuwarpforge/` |
| 组件 | `CanvasRenderer` + `UIBakeScreen`（MB 1976）+ `RawImage`（MB 1977） | `MonoBehaviour/MonoBehaviour_1976.json` / `1977` |
| RawImage | `m_Texture = null` · `m_Color` 白 · **`m_RaycastTarget = 0`** · 锚 (0,0)-(1,1)、`sizeDelta = 0` ⇒ 整屏拉伸 | `MB_1977` + `RectTransform_1118` |
| `UIBakeScreen` 字段 | **`cameraToBake`**（= 相机 602 = UI Camera）· **`image`**（= 1977 RawImage）· **`blitBlurMaterial`**（= 本包 `Material/Material_3.json` = **`UI Blur`**） | `MonoBehaviour/MonoBehaviour_1976.json`（字段名逐字） |
| 持窗者 | `MainMenuWindow.screenBaker` · `MainMenuDemoVersion.screenBaker` | `d:/2/Warpforge_code/Scripts/Assembly-CSharp/MainMenuWindow.cs:79` |

## 二、`UIBakeScreen` 的行为（反编译方法体逐句读的）

出处：`d:/2/tools/decomp_full/UIBakeScreen__{Start,Toggle,CreateRT,ResolutionChanged,OnDestroy}.c`
（类签名桩 → `d:/2/Warpforge_code/Scripts/Assembly-CSharp/UIBakeScreen.cs`）

| 方法 | 做什么 |
|---|---|
| `Start` | ① 把 `ResolutionChanged` 注册到「屏幕分辨率变了」的信号上；② **`image.enabled = false`**（出厂不显示）。 |
| `CreateRT` | ① 已有 RT ⇒ `DestroyImmediate`；② `w = ceil(Screen.width * 0.25)`、`h = ceil(Screen.height * 0.25)`（常量 **`RENDER_TEXTURE_MULTIPLIER = 0.25f`**）；③ `new RenderTexture(w, h, depth: 24, format: 0)`；④ 设名字；⑤ **`RawImage.texture = RT`**。 |
| `Toggle(bool on)` | **`on == true`**：① 没有 RT ⇒ `CreateRT`；② 按 **RT 的宽高** `RenderTexture.GetTemporary(w, h, …)`；③ 建 `UniversalRenderPipeline.SingleCameraRequest`，**destination = 这块 RT**，`RenderPipeline.SubmitRenderRequest(pipeline, req)` ⇒ **把相机这一帧渲进 RT**；④ **`Graphics.Blit(RT, temp, blurMat)`**；⑤ **`Graphics.Blit(temp, RT, blurMat, pass: 1)`**（两趟可分离模糊）；⑥ `Release(temp)`；⑦ **`image.enabled = on`**。 |
| `ResolutionChanged` | `image.enabled == false`（= 没在用）⇒ 重建 RT；否则不动。 |
| `OnDestroy` | `Destroy(renderTexture)`。 |

**材质 `UI Blur`**（`bundle_scenes_scenes_mainmenuwarpforge/Material/Material_3.json`）：
- **shader 真正只认 3 个属性**：`_MainTex` · **`_Sigma = 2.25`** · **`_steps = 10`**（`Shader/Shader_339.json` 的 `m_ParsedForm.m_PropInfo.m_Props` 逐条读的）。
- ⚠️ 材质里还烘着 `_BlurRadius` / `_GridSize` / `_Iterations` / `_Offset` / `_Sigma` / `_Spread` / `_xBlur` / `_yBlur` / `_steps` / `_QueueControl` / `_QueueOffset` —— **其中除 `_Sigma` / `_steps` 外全是别的 shader 代的残留死值**（工程已知坑：原版材质带旧属性残留，⛔ 别当参数用）。
- shader 字节码在 `bundle_scenes_scenes_mainmenuwarpforge` 的 `Shader_339`（`compressedBlob` 3556 B）—— 要精确复算模糊核可解，但**属性语义 = 高斯 σ 与采样步数**已经够写等价实现。

## 三、什么时候开 / 关（`MainMenuWindow` 反编译）

出处：`d:/2/tools/decomp_full/MainMenuWindow__{MenuFocusChangeActions,ToBackground,ToFocus,Hide,Close}.c`

| 原版方法 | 对 bake 做了什么 | 语义 |
|---|---|---|
| `MenuFocusChangeActions(bool focused)` | `Toggle(!focused)` | **失去焦点 ⇒ 烘 + 显示**；拿回焦点 ⇒ 关 |
| `ToBackground()` | `Toggle(true)` → `GameWindow.ToBackground()` → **`gameObject.SetActive(false)`** | 被别的窗压到背景：**先烘再把自己藏掉** |
| `ToFocus()` | `SetActive(true)` → 导航栏放回 → **`Toggle(false)`** | 回前台：**关掉模糊底** |
| `Hide()` | `Toggle(true)` → `GameWindow.Hide()` | 隐藏时**留一张模糊底** |
| `Close()` | `Toggle(true)` → `GameWindow.Close()` | 关闭时**留一张模糊底** |

⇒ **一句话：主菜单不在前台（被窗压住 / 隐藏 / 关闭）时，显示「上一帧 UI 的模糊快照」；回前台就关。**

## 四、我们这边的落点（设计）

1. 新文件 `Shell/UIBakeScreen.cs`（类名 == 文件名 —— MonoBehaviour 类必须同名文件，否则存盘烘 `!u!115` 存根、player 崩）。
   - 字段照原版：`cameraToBake` · `image`（我们用一个 `ImageQuad`，不是 uGUI `RawImage`）· `blitBlurMaterial`。
   - `CreateRT`：`ceil(Screen.width * 0.25) × ceil(Screen.height * 0.25)`、depth 24。
   - `Toggle(bool)`：`camera.targetTexture = rt; camera.Render();` → 两趟 `Graphics.Blit`（自建等价 blur shader，`_Sigma`/`_steps` 两个属性）→ `image.enabled = on`。
2. **建在哪**：`ShellRuntime.Build()` 里、**在全部壳 UI 之前**建一块整屏 quad（我们走 `ImageQuad` + 显式渲染队列）。
   - 队列：**低过所有窗、高过主菜单/壳背景**（参考现有梯子：壳 UI 2900–2999；压暗 `FadeQueue = 2890`）。
   - **`raycastTarget` 等价物 = 不登记命中区**（原版 `m_RaycastTarget = 0`）。
3. **何时 Toggle**：把原版那五处映射到我们的 `GameWindow` / `MainMenuRuntime` 焦点转移：
   - 主菜单被窗压住 ⇒ `Toggle(true)`（**在**新窗显示**之前**烘，否则快照里会带上新窗）；
   - 主菜单回前台 / 全部窗关 ⇒ `Toggle(false)`；
   - 主菜单 hide/close（例如进全屏窗）⇒ `Toggle(true)`。
4. **烘的是哪台相机**：原版 `cameraToBake = 602`（UI Camera，只渲 layer 5）。我们是 `Camera.main`（正交、世界空间 quad）—— 烘 `Camera.main` 即可；⚠️ 烘之前必须让「模糊底 quad 自己」不可见，否则会把上一帧的它烘进去。

## 五、没查清 / 待验

1. **blur shader 的精确核**：`_Sigma` 与 `_steps` 的语义（高斯 σ / 采样步数）是从属性名 + 两趟 Blit 推的，**没有逐条读 DXBC**。要 1:1 复算得解 `Shader_339` 的字节码（工具 `工具/dump_shader_blob.py`）。
2. **RT 的 `format` 实参**：反编译里 `new RenderTexture(w, h, 0x18, 0)` —— `0x18` 出现在「depth」形参位（24 位深度）；format 用默认。⚠️ 没跑起来核过。
3. **`SingleCameraRequest` 的具体字段**：反编译里 `*(req + 0x10) = renderTexture`，按 URP API 推断是 `destination`；我们不用 URP API，直接 `camera.targetTexture` 等价。
4. **Toast 与黑场谁在上**：✅ **2026-10-20 已定 —— 黑场压 Toast**（原版 `LoadingMenu` = Blocking(8)/0 > Toast = PopUps(7)/900；**排序层先于 order**）。⚠️ 原 `N21` 只读 `m_SortingOrder`、漏了 `m_SortingLayerID`，据此得出的「Toast 在上」**不成立**（就地订正见 `R_界面线剩余条目现核.md`）。判据 → `普查产出_1020/W58_界面线_Toast黑场次序判据.md`。

## 六、与其它账的关系

- 本件 = 界面线 `T16` 的「离屏烘焙 + UI Blur」那一半（安全区那半 2026-10-17 已落地）。
- 原版判据的**归属**：`主菜单_原版规格.md` §三①（它已把「3D 背景 → RT」那条旧说法推翻，见该节四条反证）。
