# W_D · 壳侧加载两件（`LoadingMenu` + `Loading Controller`）

> 2026-10-17 · 写手代理 W_D（界面线 T16「外壳运行层 · 加载那一半」）· 白名单见简报。
> 判据 = `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/`（场景逐字段）+
> `d:/2/tools/decomp_full/`（`LoadingMenu__*` / `MenuLoadingController__*` / `GeneralMenuController__ShowLoading` /
> `SimpleRotation__Update`）+ 本仓 `资料/普查产出_1017/UI/U3_弹窗与小界面族.md` §二 D / §五·2 ·
> `资料/普查产出_1017/UI/U1_主菜单外壳与左栏.md` §二「运行层」。
> **音效那一半不在本件**（另一个写手在做）；⛔ 没碰 `Editor/BattleScene.cs` 与战斗侧文件。

## 1. 结论 + 改动清单

**结论**：照原版建出壳侧两件 —— `Shell/LoadingMenu.cs` 里的
① `LoadingMenu`（全屏黑场；`LoadingFade(bool show, Action cb)`，语义与反编译逐条对齐）；
② `LoadingController`（`MenuLoadingController` 的等效件：Holder 1920×1080 + 暗底 `4574.6001×2572.3601 @ (-102,-51)` α=0.46666667 +
骷髅 270.438995² α=0.7 + 齿轮 270.438995² **-20°/s**）。
两件都由 `ShellRuntime.Build()` 挂在 `Safe area Only Horizontal` 下，`ShellRuntime.Update` 每帧推。
`BlockingOverlay` 保留为**降级路径**（现读零运行时调用点），队列抬到全部 shell UI 之上。
typecheck：**运行时 0 错 / 编辑器 0 错**。

| 文件 | 行 | 改了什么 |
| --- | --- | --- |
| `Shell/LoadingMenu.cs`（**新建**，354 行） | 1–354 | 两件本体 + 逐值常量 + 出处/取舍注释（文件头 1–73 行）。`LoadingMenu`：75–205；`LoadingController`：208–354 |
| `Shell/LoadingMenu.cs` 关键口 | 79 · 83 · 86 · 91 | `PanelW/H`（12006.41796875×10061.134765625）· `FadeSeconds=1.25` · `QPanel=4090` · `StartFaded=0` |
| 同上 | 114 · 151 · 169 · 184 | `Build()` · `LoadingFade(bool, Action)` · `Tick(fadeDt, spinDt)` · `TickForTest` |
| 同上 | 212 · 217 · 220 · 223 | `RootX/Y=-419/-145` · `SpinnerPx=270.4389953613281` · `SpinDegPerSecond=-20` · `QDark/QSkull/QCog=4080/4081/4082` |
| 同上 | 243 · 326 · 333 · 340 · 348 | `Build(parent)` · `Show()` · `Hide()` · `Tick(dt)` · `SetAngleForTest` |
| `Shell/ShellRuntime.cs` | 92 · 95 | 新属性 `Loading`（黑场）/ `LoadingCtl`（转圈件） |
| 同上 | 136 | `Update` 里推一步：`Loading.Tick(Time.unscaledDeltaTime, Time.deltaTime)`（黑场用 unscaled、齿轮照 `SimpleRotation` 用 scaled） |
| 同上 | 267–273 | `Build()` 里建两件（挂在 `safeH` 下；`StartFaded=0`；根出厂关） |
| `Shell/BlockingOverlay.cs` | 9–25 | 文件头改写成「**降级路径，不是原版件**」+ 列出零调用点 + `IsBlocking` 无消费者 |
| 同上 | 50 · 57 | 渲染队列 **3060/3061 → 4070/4071**（原 3060 写着「挡住一切」，而壳侧窗已到 4030 ⇒ 作为兜底其实挡不住；本件零运行时调用点 ⇒ 行为面零影响） |

## 2. 两件逐值对照（原版 → 我们 → 出处）

### 2·1 `LoadingMenu`（全屏黑场）

| 项 | 原版值（出处） | 我们 | 说明 |
| --- | --- | --- | --- |
| 宿主位置 | `Safe area Only Horizontal/LoadingMenu`（`bundle_scenes_scenes_mainmenuwarpforge/RectTransform_1098.json` 的 `m_Father=1540` = 安全区节点） | `ShellRuntime.Build()` 里 `NewRoot(safeH, "LoadingMenu")`（`ShellRuntime.cs:270`） | 我们的安全区节点是空节点（U1 记的缺口），PC 无刘海时无差异 |
| 根出厂 active | `GameObject/LoadingMenu.json:m_IsActive = false` | 建完 `SetActive(false)`（`ShellRuntime.cs:273`） | `LoadingFade` 第一句显式激活根（原版哪一处开的根**没查到**，见 §3） |
| `Panel`（= `LoadingImage`） | stretch；`m_AnchoredPosition.y = 3`；`m_SizeDelta = 12006.41796875 × 10061.134765625`（`RectTransform_1106.json`） | 高 `10061.134765625/108 = 93.158654` 世界单位；`SetAspect(12006.41796875/10061.134765625)`；局部位置 `y = 3/108`（`LoadingMenu.cs:79,117-121`） | 尺寸远超屏幕，照抄 |
| `Panel` 的图 | sprite = null；`m_Color = (0,0,0,1)`；`m_RaycastTarget = 1`（`MonoBehaviour_1960.json`） | `CardArt.Solid()` + tint 黑 α=1（`LoadingMenu.cs:118-127`） | uGUI null-sprite Image = 白块 × `m_Color`，与「白纹理 × 黑 tint」等价 |
| 出厂面板 | prefab `Panel.m_IsActive = false`（`bundle_menus_assets_all/GameObject/Panel.json`）；主菜单场景实例是 true，但其父 `LoadingMenu` 关 ⇒ **净效果相同** | `Panel.SetActive(StartFaded != 0)` ⇒ **0 ⇒ 关**（`LoadingMenu.cs:131`） | 简报里「Panel 出厂关」按 prefab 这一档实现 |
| `StartFaded` | 主菜单场景实例 **0**（`MonoBehaviour_1942.json`）；`bundle_menus` prefab **1**（`MonoBehaviour_-4255785419646353121.json`）；13 个战场实例 **1**（如 `battlearena1/MonoBehaviour_4971.json`） | `StartFaded = 0`，由 `ShellRuntime` 显式写（`ShellRuntime.cs:271`） | 🔴 **简报写的「StartFaded=1」与场景实读不符** —— 见 §5·1 |
| `Awake` | `if (StartFaded == 0) return;` 否则 `LoadingImage.gameObject.SetActive(true)` + 色 A = 常量（`LoadingMenu__Awake.c`） | `Build()` 里 `Panel.SetActive(StartFaded != 0)`；α 初值 1 | 常量 `DAT_1834b2bb8` 本体读不出；战斗线由同批 `DOFade` 旁证推 = 1.0 |
| `LoadingFade(show, cb)` | `SetActive(true)` → 色 A = `show ? 0 : 1` → `DOFade(show ? 1 : 0, 1.25s)` → 完成时**先** `cb`、再 `if (!show) SetActive(false)`（`LoadingMenu__LoadingFade.c` + `…b__0.c`） | 逐句同（`LoadingMenu.cs:151-167,196-205`） | ⚠️ 方向若写反 = 黑屏卡住（战斗线 `D1` §5 踩过） |
| 时长 | `DOFade` 实参被剥；常量 `DAT_1834b2ec0` = **1.25f**（战斗线 `A2109` 实读，同一份方法体） | `FadeSeconds = 1.25f`（`LoadingMenu.cs:83`） | 见 §3·1 |
| 缓动 | 反编译里 `DOFade` 后**只链了 `OnComplete`，无 `SetEase`** ⇒ DOTween 默认 OutQuad | `1-(1-p)²`（`LoadingMenu.cs:176`） | **推断**（依据 = 没有 SetEase），已注明 |
| 渲染层级 | Canvas `overrideSorting=1` / `sortingOrder 0` / `sortingLayerID -2008831743`（`Canvas_1081.json`） | 渲染色队列 **4090**（现读 shell 最高 4030） | 两套分层机制不同（U1 已记），按「必须盖住一切」定 |

### 2·2 `Loading Controller`（转圈）

| 项 | 原版值（出处） | 我们 | 说明 |
| --- | --- | --- | --- |
| 根 `Loading Controller` | `m_IsActive = true`；RT stretch、`m_AnchoredPosition = (-419,-145)`（`RectTransform_1083.json`） | 根位置 `(-419/108, -145/108)` 世界单位（`LoadingMenu.cs:212,247-251`） | 序列化值照抄；运行期姿态**未查清**（见 §3·2） |
| `Holder` | 锚/轴 (1,0)、`1920×1080`、pos (0,0) ⇒ 与根矩形重合（`RectTransform_1117.json`） | 同尺寸、局部 (0,0)（`LoadingMenu.cs:253-255`） | Holder 只当分组/开关用（原版 `loadingIndicator` = Holder） |
| 出厂显示 | `MenuLoadingController.Awake` → `loadingIndicator.gameObject.SetActive(false)` | `Build()` 末尾 `_holder.SetActive(false)`（`LoadingMenu.cs:316`） | 逐句同 |
| `Menu Dark Background` | `4574.60009765625 × 2572.360107421875`、中心偏移 `(-102,-51)`（`RectTransform_1535.json`）；`m_Color = (0,0,0,0.46666666865348816)`、sprite=null（`MonoBehaviour_2499.json`）；场景里 `m_IsActive = false` | 同尺寸/偏移，黑 α=`0.46666666865348816`（`LoadingMenu.cs:214-216,257-268`） | 🔴 **场景里它是关的**；照抄则骷髅/齿轮（它的子件）永不显示 ⇒ 按「加载件可见」实现，见 §3·3 |
| `Animated Loading Image` | 中心锚、`270.4389953613281 × 270.4389953613281`（`RectTransform_1273.json`）；sprite `7621650145332779859` = **`40K_icon_searching_skull`**（`MonoBehaviour_2446.json`）；同 GO 的 CanvasGroup `m_Alpha = 0.699999988079071`（`CanvasGroup_1620.json`） | 270.438995²，骷髅图 α=0.7（`LoadingMenu.cs:217,274-291`） | |
| `Cog` | 与父同框（`RectTransform_1183.json`）；sprite `5609191530438287762` = **`40K_icon_searching_cog`**（`MonoBehaviour_2501.json`）；`SimpleRotation.speed = (0,0,-20)`（`MonoBehaviour_2104.json`） | 270.438995²，齿轮图；**-20°/s**（`LoadingMenu.cs:220,293-313,340-345`） | 🔴 **原版转速是查得到的**（简报里「转速我们挑的」说的是旧 `BlockingOverlay`） |
| α 传播 | CanvasGroup 在 `Animated Loading Image` 上 ⇒ **子件 `Cog` 一起乘 0.7** | 骷髅与齿轮**都** tint α=0.7（`LoadingMenu.cs:283,310`） | 只给骷髅乘会静默差一档 |
| 显示/隐藏 | `MenuLoadingController.ShowLoading/HideLoading` → 同一个 `loadingIndicator` 开/关 | `Show()` / `Hide()`（`LoadingMenu.cs:326-337`） | 本地唯一原版调用点 = `JoinAllianceMenu`（公会搜索） |
| 渲染层级 | Canvas/CanvasGroup/排序层（原版） | 队列 4080/4081/4082（暗底 < 骷髅 < 齿轮） | 世界空间 quad 的一条硬规矩：透明件按到相机距离排序 ⇒ 必须显式队列 |

## 3. 没做的 / 没查清的（逐条：为什么 + 搜过什么）

1. **`LoadingFade` 的时长/缓动「实参」本身在反编译里没有**。`LoadingMenu__LoadingFade.c` 里 `DOFade` 只传了目标对象，
   时长在 `xmm2`（寄存器）里 —— 本方法体读不到。**搜过**：`d:/2/tools/decomp_full/LoadingMenu__*.c`（4 个文件全读）。
   ⇒ 时长用战斗线**同一份方法体**的常量实读（`DAT_1834b2ec0 = 1.25f`，`A2109` 落盘，见 `Battle/BattleDriver.cs` 的
   `LoadingFadeSeconds` 头注）；缓动按「没有 `SetEase` ⇒ DOTween 默认 OutQuad」**推断**。
   ⚠️ 若原版在全局设过 `DOTween.defaultEaseType`，这条推断会偏 —— 那项设置**本地没有**（不是没找，是没有那份资源）。
2. **`Loading Controller` 的运行期姿态**（U3 §五·2 同一条）：场景里父链 `Game UI` 的 `m_LocalScale = 0`，
   整棵是**未缩放帧**；`270.438995²` 是设计值。原版运行时**先设 scale 再显示**的赋值点**没查到**。
   **搜过**：`decomp_full` 里 `MenuLoadingController__*.c`（4 个文件全读，只有 `Awake/ShowLoading/HideLoading`）、
   全库 `grep "MenuLoadingController"`（只命中 `JoinAllianceMenu` 3 处）、13 个战场包 + `bundle_menus` 的
   `MenuLoadingController` 脚本 id `-6522733442835287676`（只在主菜单场景 1 处）。
   ⇒ 本件**显式给 270.438995²**（⛔ 不按 0 缩放），偏移 `(-419,-145)`、`(-102,-51)` 照序列化值抄。**判据未清**。
3. **`Menu Dark Background` 在场景里 `m_IsActive = false`**。照抄 ⇒ 它的子件（骷髅/齿轮）整棵永不显示，
   而 `MenuLoadingController.ShowLoading` 只开 `Holder`。**搜过**：`MenuLoadingController__ShowLoading.c`（只两件事：
   取单例 + `loadingIndicator.SetActive(true)`）、该 GO 上的 5 个组件（`BackgroundCloseButton` 等，没有一个负责开它）。
   ⇒ 本件让暗底随 `Holder` 一起开关，如实标**判据未清的本仓取舍**（不猜「原版运行时谁开的」）。
4. **主菜单 `LoadingMenu` 根由谁激活**：场景里根 `m_IsActive = false`，而 `GeneralMenuController.ShowLoading`
   只调 `LoadingFade`（内部只 `SetActive(LoadingImage)`）⇒ 根必须先被别的东西打开，**那一点本地没查到**。
   **搜过**：`GeneralMenuController__ShowLoading.c` / `__ActivateLoadingFade.c`（后者全库零调用点）、
   `MainMenu` 的 `loadingMenu=1942`（MB2578 的序列化字段只证明「它握住这根引用」）。
   ⇒ 本件 `LoadingFade` 第一句显式激活根（显式 > 猜）。
5. **没有接线到「进战斗」**：`PracticeModePopup` / `LiveOpsEventWindow` 的 `LoadScene` 都在**禁改名单**里。
   本件只提供 `ShellRuntime.Loading.LoadingFade(true/false, cb)` 这个口；接线应由下一批做（见 §6 建议）。
6. **没有做输入拦截**：原版黑图 `m_RaycastTarget = 1`；我们这条输入链的命中只认 `WindowButton`
   （`PointerLayer.HitButton` 挑「带按钮的最高队列」），一张无按钮的全屏 quad **不挡点击**；
   而 `BlockingOverlay.IsBlocking` **零消费者**（见 §5·2）。改输入得动 `PointerLayer` / `MenuScroll`（不在白名单）⇒ 没做、已记账。

## 4. typecheck

```text
TMPDIR=/tmp/wf_d_loading bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ 第一次跑时出错 6 条（+ 编辑器 CS0006），**全部集中在 `Shell/SettingsWindow.cs`**（`_ddQuality` / `_ddTextInHand`
未定义等）—— 是**别的写手正在写的半成品**；按简报隔 90 秒重跑，对方已写完 ⇒ **0/0**，未改别人文件一分一毫。

## 5. 顺手发现（⛔ 未改）

1. 🔴 **U3 §二 D 那一行的 `StartFaded=1` 与场景实读不符**：主菜单实例是 **0**（`MonoBehaviour_1942.json`）。
   `=1` 的是 `bundle_menus` 的 prefab 与 13 个战场实例。**建议**把 U3 那格改成「主菜单 0 / prefab·战场 1」（我未改文档正本）。
2. 🔴 **`BlockingOverlay.IsBlocking` 全仓零消费者**：`grep -rn "IsBlocking" Unity/MyGame/Assets --include=*.cs` 只命中
   本文件与 `Editor/ShellScene.cs` 的三条自检 ⇒ 「挡输入」这条行为**从来没有接线**，`StartSpinning()` 除了自检没人调。
   （`BlockingOverlay` 本身也零运行时调用点；大厅断线走 `NetRuntime.Notice` + 弹窗，不是它。）
3. 🟡 **`BlockingOverlay` 的渲染队列原来低于窗口**：3060/3061 时期，壳侧弹窗已到 3100–3150、面板到 4030 ⇒
   它自己注释写着「挡住一切」其实盖不住。**这一条本批已改**（4070/4071，见 §1 改动清单；在白名单内）。
4. 🟡 **`bundle_menus_assets_all` 的 `LoadingMenu` prefab 面板高 = 10394.9189453125**（场景实例 = 10061.134765625，
   宽相同）= 两份不同的值。本件按**场景实读**（简报指定的唯一来源）。若将来要给「窗口内加载」用这份 prefab，高度别抄错。
5. 🟡 `Menu Loading Controller` 的本地唯一调用点 = `JoinAllianceMenu`；我们的公会/联盟搜索（`AllianceSearchTab` 等）
   目前没有「搜索中转圈」。是否接 `LoadingController` 由界面线后续定（本件只提供组件）。
6. 🟢 两张图本地都在：`Resources/Art/ui_menu/40K_icon_searching_skull.png`（256²）·
   `40K_icon_searching_cog.png`（256²）—— 与 `Animated Loading Image` / `Cog` 的 270.438995² 方框等比吻合
   （`m_PreserveAspect = 0`，原版直接拉伸到 270.438995²）。

## 6. 下一步建议

1. 把**进战斗/切场景**两处 `LoadScene`（`LiveOpsEventWindow.StartBotBattle`、`PracticeModePopup`）前面接
   `ShellRuntime.Instance.Loading.LoadingFade(true, () => SceneManager.LoadScene(...))`；回到主菜单时
   `LoadingFade(false)`（原版调用点 = `MatchMakerManager.ChangeToBotBattle` / `…ErrorScreenCloseButtonPress`）。
   战斗场景自己已有 `BattleDriver.BuildLoadingCover`（同原版件、1.25s 淡出）⇒ 两端能接上。
2. 联盟搜索接 `LoadingController.Show()/Hide()`（原版对应 `JoinAllianceMenu.Initialize` / `FillOpenAlliances`）。
3. 输入拦截那条（§3·6）：如果要做，判据是 `LoadingImage.m_RaycastTarget = 1` + 原版整屏 uGUI 射线；
   实现上要么在 `PointerLayer` 里加「最高队列的全屏遮挡件吃掉命中」，要么把 `IsBlocking` 真正接进菜单入口。
