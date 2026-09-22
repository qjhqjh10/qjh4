# 阶段二 · `Shell`（游戏外壳）· 原版规格

> **建立 2026-09-22**。怎么来的：阶段二开工前派子代理只读普查（`assets_full/level0/` + 姊妹场景 + 反编译）。
> 用途：**建 `Shell.unity` 的施工图**。主菜单那一份见 `资料/主菜单_原版规格.md`（同一批产物）。
> 判据来源三处：`level0`（= 原版 `Intro.unity`）的 JSON · **主菜单场景包**（同一批脚本在这里**字段完整**）· 全量反编译。

---

## 〇、三条取证结论（后面全依赖它）

1. **level0 那 73 个失败组件是「对象根本没导出」，不是「字段被抹」。**
   GameObject 的 `m_Component[]` 完整、非 MonoBehaviour 组件（Camera/Canvas/CanvasGroup/VideoPlayer/AudioSource/RectTransform）**字段全在**；
   而 134 个 GO 只对应 **77 个 MonoBehaviour 对象**（437 对象 − 364 导出 = 73）。
2. ✅ **类名 100% 可复原**（参数仍缺）：`assets_full/globalgamemanagers/MonoScript/MonoScript_<pid>.json` 的 `m_Name` 就是类名；
   每个被抹的 `MonoBehaviour_<compPid>.json` 里 **`m_Script.m_PathID` 还在** ⇒ 走 `compPid → m_Script.pid → MonoScript.m_Name`。
   （照这条链复算了 `资料/说明书/08_主程序资源/运行时管理器.md` 那张表，**逐行吻合**。
   ⚠️ 那张表 84 行里 **8 行不在 level0**（`Button Text`×3 / `ButtonLeft` / `ButtonRight` / `Generic UI Button` / `GameObject`），
   level0 实数 = **134 GO / 77 脚本**。）
3. 🔴 **同一批脚本在「独立 bundle」里字段完整** ⇒ **Shell 参数的主来源是姊妹场景**：
   `bundle_scenes_scenes_mainmenuwarpforge`（WindowHolder / UISafeAreaManager / ToastNotificationController /
   EverguildTooltipManager / MenuLoadingController / MainMenuWindow 全有真值）· `bundle_staticgeneralassets_assets_all`
   （MusicManager / SoundManager 预制体）· `bundle_menus_assets_all`（所有菜单窗口的 `GameWindow` 参数）。

---

## 一、常驻件表（只列单机复刻真正需要的）

**运行期层级实证**（`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Intro.tsv`，t45/75/105/135 四点一致）：

```
UI Camera · SteamManager(inactive) · Boot · Intro Videoplayer
Intro UI → { PopupHolder
             Safe area → { Loading text → New Game Object · Progress text(inactive) }
             FadeBackground → { Smooth background fade Left/Right/Bottom/Top } }
```

| 件 | 干什么 | 参数 / 出处 | 判定 |
|---|---|---|---|
| `UI Camera` | 唯一 UI 摄像机 + 监听器 | **实证 JSON** `level0/Camera/Camera_258.json`：透视 fov **40** · near **0.3** / far **1000** · ClearFlags **2（纯黑）** · depth 0 · cullingMask 全 · HDR + MSAA on · targetDisplay 0；`AudioListener_260`。第 3 个组件（303）导出失败 | **能照原版** |
| `Intro UI` | **壳的画布根**（= 常驻宿主，**不是"开场界面"**） | **实证 JSON** `Canvas_276`：RenderMode **1（ScreenSpaceCamera）** · camera=UI Camera **258** · planeDistance **100** · pixelPerfect false · receivesEvents true · shaderChannels 25。4 个组件导出失败（推断含 CanvasScaler / GraphicRaycaster / 面板脚本） | 画布**能照原版**；4 个脚本**查不到** |
| `PopupHolder` | 弹窗锚点容器 | 自身只导出 RT(278, 100×100) + 1 失败件。**姊妹场景实证**：主菜单同名件 `3 - PopUp Holder` = `WindowHolder{placement:15}` + `CustomRaycaster` | 结构**强推断 = WindowHolder(Popup)** |
| `Safe area` | 刘海/安全区适配 | **实证 JSON**：RT(281)（stretch 0,0→1,1，scale 1）+ `CanvasGroup_288`（alpha 1 / interactable / blocksRaycasts）；**该 GO 没有脚本**。脚本真身 = **`UISafeAreaManager{ m_safeZones:[{rectTransform, applyWidth, applyHeight}] }`**，挂主菜单根 GO `Game UI` 上，指向 `Safe area All`(1,1) 与 `Safe area Only Horizontal`(1,0)（`mainmenuwarpforge/MonoBehaviour/MonoBehaviour_2580.json`） | 结构 + 类**能照原版**；level0 侧宿主**查不到** |
| `FadeBackground` | 四边压暗 | **实证**：只有一个 RT(285) = 空容器；子件 4 条 `Smooth background fade *` = RT + CanvasRenderer + 2 失败件。**尺寸实证（TSV）**：Left/Right **205.8×2585.5 @ x=∓960 常开** · Bottom/Top **4605×146.3 @ y=∓540 inactive** | 尺寸/开关**能照原版**；脚本查不到 |
| `Loading text` | 转圈下方文案 | RT(280) + CR(272) + 2 失败件（推断 TMP + Localize）。**TSV：1920×48，y=70，常开** | 版式**能照原版**；文本组件**只能近似** |
| `Progress text` | 进度百分比 | 同上（RT 279 + CR 271）。**TSV：1920×48，y=21.8，运行期 inactive** | 同上 |
| `WindowsManager` | **窗口管理器单例** | 类 = `WindowsManager : SingletonBehaviour<WindowsManager>`（`il2cpp_out/dump.cs:115369`；⚠️ 全工程**无** `WindowManager` 这个类，**名字差一个 s**）。🔴 **实例序列化值全局查不到**（见 §五） | 行为/字段名**能照原版**（反编译） |
| `MusicManager` | 音乐（双源交叉淡入） | **实证**（`bundle_staticgeneralassets_assets_all` 的同名预制体）：`{_audioSources{_first,_second}, musicVolumeMultiplier:0.2, priority:0, status:0}` + 2×`AudioSourceMixerGroupFixer{mixerType:1}` | **能照原版** |
| `SoundManager` | 音效池 + Mixer | **实证**（同预制体）：`audioSourceTemplate` · `defaultButtonSound` · `defaultCloseSound` · `audioMixer` · `fxGroup`/`musicGroup`/`voicesGroup`/`jinglesGroup` · `sfxOnSnapshot`/`sfxOffSnapshot` · **`amountToPool:20`**。Mixer 名字实证在 `bundle_audiocontrol_assets_all/AudioMixerController/`：**Main Mixer**（组：Master · General(FX) · General(Music) · Voices · Jingles · BattleGroup Low/Medium/High） | **能照原版** |
| `Intro Videoplayer` | 开机动画 | **实证 JSON 全参数**（`level0/VideoPlayer/VideoPlayer_289.json`）：TargetCamera = **UI Camera(258)** · `m_RenderMode:0`(CameraNearPlane) · `m_AspectRatio:3` · `m_AudioOutputMode:1` · `m_DirectAudioVolumes:[1.0]` · `m_PlayOnAwake:1` · `m_SkipOnDrop:1` · `m_Looping:0` · `m_WaitForFirstFrame:1`；`AudioSource_261` 音量 **0.65** | **能照原版** |
| `MusicVolumePlayerPrefsSetting` | 音乐音量挂钩存档 | 类实证（comp 308 → script 897）；`Start()` 里 `volume *= PlayerPrefs.GetFloat("MusicVolume", volume)` | **能照原版** |
| `Boot` | 启动流程宿主 | 自身 = Transform(138) + 2 失败件。候选类：`GameBootstrap{sceneToLoad}` + `GameBootController{videoPlayer, isPlayerDataManagerInScene, …}` | **强推断**；宿主归属**未实证** |
| `Shade` | 半透明压暗面板 | **实证**（战场场景同脚本 13 处）：`Shade{shade, shadeUI, onAlphaLevel:0.8, defaultTimeToSwitch:0.5}` | **能照原版** |
| `BlockingOverlay` + `Spinner` | 全屏遮罩 + 转圈（挡输入） | `BlockingOverlay` 类实证（`Awake/Start/StartSpinning/StopSpinning/AutoStopSpinner`）；**字段与 `Spinner` 的类都查不到** | 行为能照原版；参数查不到 |
| `EventSystem` | 输入 | Transform(141) + 3 失败件 | **只能近似**（Unity 标准件照建） |
| `SteamManager` | Steam | 运行期 TSV 里 **inactive** ⇒ 不做 | — |
| 其余 77 个 provider/管理器（Card Provider / Shops / Missions / Forge / Currency / Points …） | 原版数据源 | 类名与 comp pid 可复算；**参数全丢**（就是"经济数值"那批缺口） | 单机**不需要**（我们自定数据源） |

---

## 二、开场动画完整链

1. `Boot`（→ `GameBootstrap.Awake()`）：Addressables 初始化 → 加载本场景（`decomp_full/GameBootstrap._Awake_d__1__MoveNext.c`）。
2. `Intro Videoplayer` 的 `VideoPlayer(289)` **`m_PlayOnAwake:true`** ⇒ 立即播 `sharedassets0/VideoClip/Warpforge Intro.mp4`，
   **画面渲到 UI Camera(258) 近平面**，声音走同 GO 的 `AudioSource(261)`（音量 **0.65**，实际再乘 `PlayerPrefs["MusicVolume"]`）。
3. 🔴 **跳过 = 任意键 / 鼠标左键，没有按钮、没有 UI**：`GameBootController.Update()` 里 `Input.anyKeyDown` 或左键
   → `videoPlayer.time = clip.length - ε` → `VideoFinished()`（`GameBootController__Update.c` / `__SkipToVideoToEnd.c`）。
4. `VideoFinished()`：置 `videoEnded` → `DOTween DOFade(loginScreenManager.canvasGroup, …)` 登录界面淡入 + `StartCoroutine(DelayedEnable)`。
5. 登录完成 → `StartGame()`：**若 `FirstTwoTutorialCompleted()==false` ⇒ 直接进教程战**（`TutorialManager.SetupTutorialMatch()` + `MatchMakerManager.StartMatch(…,4,…)`）；
   否则 `EverguildSceneManager.ClearLoadedScenes()` + `SceneChanger.DoChange()` 跳主菜单。
6. **单机复刻**：**跳过第 4/5 步的登录** ⇒ `VideoFinished → 淡入 → 载 MainMenu`。`Warpforge Intro.mp4` 是 mp4，可直接播。
7. 反编译文件：`GameBootController__{Update,SkipToVideoToEnd,VideoFinished,StartGame,DelayedEnable}.c` · `GameBootstrap._Awake_d__1__MoveNext.c` · `LoginScreenManager__SetupScene.c`；
   桩：`Assembly-CSharp/{GameBootController,GameBootstrap,LoginScreenManager,SceneChanger,MusicVolumePlayerPrefsSetting}.cs`。

---

## 三、开窗口调用链（**后面所有菜单页的宿主**）

1. **入口两种**：① `OpenWindowButton{windowToOpenScene, windowToOpenPrefab(ComponentReference<GameWindow>), windowPayload, button, closeOtherMenus, closeOnToggleChangeToFalse}` 的 `ButtonClick→OpenWindow`；
   ② 代码 `WindowsManager.OpenWindow<T>(data, closeAll)` / `OpenWindow(GameWindow, data, closeAll, options)`（**共 7 个重载**）。
2. `WindowsManager : SingletonBehaviour<WindowsManager>` 静态 `anchors: Dictionary<WindowsPlacement,Transform>` + 字段
   `popupWindowOneButton/TwoButtons`(ComponentReference) · `…Prefab` · `temporaryWindowDictionary: List<TypedWindowReference>` ·
   `openWindows: List<GameWindow>` · `currentWindow` · `popUpWindow` · `automaticallyLoadedWindows` · `OnWindowOrTabChanged`
   （桩 `WindowsManager.cs:149-172`）。
3. `OpenWindow(ComponentReference)` → Addressables 取 prefab → `OpenWindowCO(window, data, closeAll, options)`：
   `closeAll` ⇒ `CloseAllWindows()`；`window.type==0(Fullscreen)` ⇒ 关当前主窗；`==1(Popup)` ⇒ 把上一个 `ToBackground()`；
   `set_CurrentWindow` + 压入 `openWindows`；最后 `window.Open(...)` + `GameAnalytics.SendWindowOpen`（`WindowsManager__OpenWindowCO.c`）。
4. `GameWindow.TryOpen(data, options)`：`SetupData` → **`SoundManager.Play2D(openSound, MixerType.FX)`** → `SetActive(true)` → `CurrentState=Open` → `Open()` → 触发 `OnOpen`（`GameWindow__TryOpen.c`）。
5. `GameWindow.Open()`：若**窗口宽度 < 阈值**，用 `TransformScalerBySmallScreenUI.SetScale(extraScaleSmallScreen)` 放大；
   `extraScaleSmallScreen` 实测：**普通窗 1.0 · 商店/活动类 1.2**（`GameWindow__Open.c` + `bundle_menus_assets_all` 18 例）。
6. **锚点/父子关系**：`WindowHolder{placement}` 在 `OnEnable` 里 `WindowsManager.RegisterAnchor(placement, transform)`；
   查不到会 **`CustomDebug.LogError`**。`WindowsPlacement` = `None=0 / Canvas=5 / World=10 / Popup=15`。
7. 🔴 **主菜单场景实测 3 个 Holder 缺一不可**：`1 - Below Upper Bar Holder{10}` · `2 - Canvas Holder Above upper bar{5}` · `3 - PopUp Holder{15}`
   （`MainMenuWindow` 自身 `windowsPlacement:0`，不挂 Holder）。**level0 只有 PopupHolder** ⇒ 复刻时三类锚点必须齐。
8. 弹窗：`ShowPopUp(text, localizeTexts, closeOnEsc, …)`（3 个重载）→ `LoadPopUpAndShow` / `LoadPopup(twoButtons)` 协程 →
   `popupWindowOneButton/TwoButtons` → `HidePopUp(forceHide)`；`GameWindowButton{Text, OnPress}`。
9. 带页签：`GameWindowWithTabs : GameWindow`，`tabs: List<WindowTabBase>` + `TabbedWindowComponents{TabButtons, TabHolder, TabPrefab}`，
   `SetupTabs/OpenTabs/ChangeTab/ChangeTabCO/GetTab<T>/ForceCloseTabs`。
10. **典型窗口参数实证**（`bundle_menus_assets_all`）：`RatePopup`(type1/Popup, place15, closeOnESC 0) · `RewardWindow`(1/15/1) ·
    `DailyRewardPopup`(**type0**/15/1) · `ChooseNameWindow`(1/**0**/1) · `SkirmishModeEventWindow`(1/**5**/1) ·
    `FTUESelectDeckWindow`(0/15/0) · `TutorialMessageWindowPopUp`(1/15/**0**)。

**反编译点名**：`WindowsManager__{OpenWindow,OpenWindow_object_,OpenWindowCO,Init,RegisterAnchor,GetWindowAnchor,CloseWindow,ShowPopUp,LoadPopUpAndShow,LoadPopup,HidePopUp,SetBaseMenu,ShowMainMenu}.c` ·
`GameWindow__{TryOpen,Open}.c` · `WindowHolder__OnEnable.c`；
桩：`Assembly-CSharp/{WindowsManager,GameWindow,GameWindowWithTabs,WindowHolder,GameWindowOptions,GameWindowWithTabsOptions,OpenWindowButton,GameWindowButton,PopUpGameWindow,MainMenuWindow}.cs`。

---

## 四、音频（菜单音乐）

- **播放入口**：`MainMenuMusicController{mainThemeMusic, idleThemeMusic}`（`AssetReferenceTyped<AudioClip>`）→ `Initialize()` 首次
  `MusicManager.Instance.PlayMusic(mainTheme, MusicTransitionType 2)` 再 `PlayMusicQueued(idleTheme)`；重复进入时 `Release(mainTheme)` 后直接 `PlayMusic(idleTheme, 2)`
  （`decomp_full/MainMenuMusicController__Initialize.c`）。GUID：main `bcd5d12cda7834f788d81601be698860` · idle `953f619289b0c467d9cbc88205b3bbf0`。
- **两个 clip** = `bundle_menumusic_assets_all/AudioClip/` 的 **`Main Theme.ogg` + `Menu Idle Theme.ogg`**。
- **音量链**：`MusicManager.musicVolumeMultiplier = 0.2` → `AudioSourcesController` 双源交叉淡入 → 输出组由 `AudioSourceMixerGroupFixer{mixerType:1}` 指定；
  全局 `SoundManager.SetMixerVolume(MixerType, float)`，Mixer = **Main Mixer**；音效池 `amountToPool:20`。
- 落盘只有 `PlayerPrefs["MusicVolume"]`（音效/语音**不存**）—— 与我们已做的三滑块 AudioMixer 直接对得上。

---

## 五🔴、明确「查不到」的（别当已解决）

1. level0 **73 个组件的类名与参数**（类名多数可复原，**参数拿不到**）：Safe area 侧宿主 / WindowsManager / Spinner / TouchManager /
   Loading・Progress text 的 TMP 组件 / 4 条 fade 的脚本 / Intro UI 的 4 件 / Boot 的 2 件 / UI Camera 的第 3 件 …
2. `WindowsManager` 实例的**序列化值**：全 `assets_full` grep `popupWindowOneButton` / `temporaryWindowDictionary` **零命中**
   ⇒ **弹窗 prefab 与「类 → prefab」字典得我们自建**。
3. `_stats.json` 只到文件级（`fail:73`），**没有失败对象的类名清单**。
4. `Spinner` / `TouchManager` 等类在 `dump.cs` 里**不存在同名类**，无法按名反推。

---

## 六、⏭ 我们要建的 `Shell.unity`（落地清单）

> 原则照旧：**能照原版的照原版**（表里标了"能照原版"的那几件）· **查不到的自己建并标明**。
> 建法照 `Editor/DeckScene.cs` + `DeckRuntime` 那套：**Editor 只开场景/自检/存场景，绘制全在 runtime 的 `Build()` 一处**。
>
> ### ✅ 2026-09-22 已建（自检 **42 通过 / 0 失败**，`ShellScene.Run`）
> 代码：`CardPresentation/Shell/{WindowsManager,ShellRuntime,ShellParts,PopUpGameWindow}.cs` + `Editor/ShellScene.cs`。
> 命令与素材导入 ⇒ `资料/命令速查.md`；截图 ⇒ `d:/4/_tmp_view/shell/`。
>
> | # | 件 | 状态 |
> |---|---|---|
> | 1 | `UI Camera` | ✅ 正交（`LayoutSpace`，可见高度 10 单位）+ 原版参数记在注释里（fov40/near0.3/far1000/清屏黑/HDR+MSAA） |
> | 2 | 画布 | ⚠️ **不开 UGUI Canvas** —— 本工程全线是世界空间 mesh + TMP 世界空间文字（`ImageQuad`/`Label`），对平面 UI 画面等价 |
> | 3 | **三个 Holder** | ✅ `10 / 5 / 15` 全建、全注册（`WindowHolder` 加了 `[ExecuteAlways]` + 显式 `RegisterNow()`） |
> | 4 | `UISafeArea` | ✅ 两个安全区（All 1,1 / Only Horizontal 1,0） |
> | 5 | `FadeBackground` 四边 | ✅ 尺寸照实证；左右开、上下关 |
> | 6 | `Shade` / `BlockingOverlay` | ✅ Shade 字段照实证（0.8 / 0.5s）；遮罩按行为等价 |
> | 7 | `Loading text` / `Progress text` | ✅ 版式照实证（y=70 开 / y=21.8 关）；**文案留空**（本地化词条本地没有） |
> | 8 | 音频 | ✅ 菜单音乐两首（源音量 0.2）+ 走 mixer 的 `Music` 组（`WarpforgeAudio` 新补的 `MusicGroup`） |
> | 9 | 开场动画 | ✅ `VideoPlayer` + 全屏 quad；**任意键/左键跳过**；播完淡入主菜单（不做登录） |
> | 10 | `WindowsManager` + `GameWindow` | ✅ 自建（原版实例值本地没有）：全屏/弹窗/锚点/压背景/关闭链全通；`PopUpGameWindow` 是**自建版式**（待 `GenericPromptWindow` 表替换） |
>
> ### ⏭ 还没做 / 已知缺口（**别当已解决**）
> - **开场动画的「真 Play 目视」还没做过** —— 自检验的是「加载得到 + 时长 + 参数 + 跳过链」，**没有真解码出帧**。
>   要照 `Editor/VideoProbe.cs` 那套（`EditorApplication.update` + `QueuePlayerLoopUpdate` 状态机）写一个 `ShellScene.Play`。
> - **转圈图没导**（`40K_menu_loading` 不在 `Resources/Art/ui_deck/`）⇒ `Spinner` 现在**只挡输入、不画圈**（有日志）。
> - **`GenericPromptWindow [1139]`**（「暂无服务器」的宿主，在 `bundle_menus_assets_all`、参数齐全）**还没读** ⇒ 读到之后 `PopUpGameWindow` 的版面要换成它。
> - `Tooltip` / `Toast` / `UI Error Message Controller` / `LoadingMenu` 这些**壳还没建**（原版出厂也是空壳 + 运行时装）。
