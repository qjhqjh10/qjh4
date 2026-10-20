# W58 · 界面线 `T16`：Toast / 黑场次序 —— 判据（2026-10-20）

> 只读调查代理 **W58-INV-TOAST**。⛔ 未改任何代码 / 资产 / 任务表 / 正本，未跑 Unity、未动 git、未截图、未起 player。
> 判据一律**现读**：`d:/2/tools/decomp_full/`（方法体）· 本仓 `Shell/*.cs` · `d:/2/新解包资源/assets_full/`（prefab/场景字段）。
> 结论中标注 **[判据]** = 原版实读；**[推断]** = 未坐实；**[我们挑的]** = 本仓取舍。

---

## 结论（3 句内）

1. **原版层序已定**：`Toast Notification Controller` 的 Canvas = **排序层 `PopUps`（第 7 层）/ order 900**；`LoadingMenu` 的 Canvas = **排序层 `Blocking`（第 8 层）/ order 0**。**排序层先于 order 比较** ⇒ **加载黑场压在 Toast 之上**（也压在所有窗、教学高亮、错误横幅之上）。这一条**不再是「判据未定」**。
2. **Toast 规格已完整**：`1.0s OutBack 入场 → 3.0s 停留 → 1.0s InBack 退场`，起始 `y = -h`、目标 `y = index*h + 10px`；`index = freeSpot; freeSpot++`，退场回调 `freeSpot = min(index, freeSpot)`；四条模板、右下角向上堆叠、无并发上限；Toast / LevelUp / GetXP 三颗有悬停色偏与点击回调、Mission 没有。
3. **口径修正**：我们的 **toast(4072–4076) < 黑场(4090)** 已经是**对的**；但**其余几层仍与原版反着** —— `Tooltip`(3606) 低于 toast（原版 Blocking 在 PopUps 之上）· 教学高亮(4100) 和 `LoadingController`(4080) 都高于 toast / 黑场（原版都在其下）。逐条见 §五。

---

## 一、原版 Toast 规格（证据）

### 1·1 对象与层

| 项 | 原版值 | 出处 |
|---|---|---|
| 控制器 | `Safe area Only Horizontal ▸ Toast Notification Controller ▸ Holder` | 场景 `RT 1096.m_Father = 1540`（GO 72 = `Safe area Only Horizontal`）· `RT 1100`（Holder） |
| Holder 几何 | anchor=**pivot=(1,0)**、`apos = (-21.4228515625, 0)`、`sd = 634.4879760742188 × 752.614013671875` | `RectTransform_1100.json` |
| 层 | 独立 Canvas（子画布，`m_OverrideSorting = true`）· 排序层 **`PopUps` = 1054366423** · **order = 900** | `Canvas/Canvas_1074.json`（`m_GameObject.m_PathID = 17`）；层名/次序见 `globalgamemanagers/TagManager/TagManager_3.json` |
| 模板映射（**实读**） | `type 0 → prefab 491`（`Toast`）· `1 → 492`（`Get XP`）· `2 → 490`（`Level Up`）· `3 → 488`（`Mission`）；`defaultToastNotification = 491` | `MonoBehaviour_1939.json`（`notificationTypes` / `defaultToastNotification`） |
| 模板根矩形 | `Mission 650.101×139.713` · `LevelUp 536.755×97.9` · `Toast 466.136×**0**`（运行期 CSF/VLG 算高）· `GetXP 536.755×97.9`（运行期 CSF 算高）；四颗全是 `a(1,0)/p(1,0)`、`apos = (0,0)` | `RectTransform_341/342/343/344.json` |
| 子件 | `Toast`：`Icon 89.8613×89.8638` + `Text 362.057×**0**`；`GetXP`：`Icon 86.6998×86.7019` + `Text 435.619×0`；`Toast` 的 VLG = `padding L83 T12 B7`、`m_ChildControlHeight = 0`；`GetXP` = `L83 T0 B0` | `MonoBehaviour_204/205.json` · `RectTransform_355/356/357/358.json` |

> 🔴 **`m_SortingLayerID` 是 int32**：`-2008831743` 按无符号还原 = **2286135553** = `Blocking`；`TagManager_3.json` 的 `m_SortingLayers` 次序 =
> `Default(0) → Background → Elements → OverElements → Hand → HandMoving → OverHand → **PopUps(7)** → **Blocking(8)**`。
> ⇒ `Blocking` 层的 toast/黑场一律画在 `PopUps` 之上。**⛔ 只比 `m_SortingOrder`（900 > 0）会得出反的结论**（本仓已踩过一次：`R_界面线剩余条目现核.md:33` 的 `N21①` 就是这么错的）。

### 1·2 时序（`ToastNotificationController__ShowNotificationInternal.c` 逐句）

```
① Instantiate(prefab, holder, false) → Initialize(context)
② RectTransform.ForceUpdateRectTransforms(); FixLayout(); ForceRebuildLayoutImmediate()
   → h = rect.height
③ anchoredPosition = (0, -h)                       // 常量 DAT_1834b2c80 = 0x80000000 = 符号位翻转
④ Sequence():
     DOAnchorPosY(index * h + 10, 1.0s).SetEase(0x1b)   // 0x1b = 27 = Ease.OutBack
     AppendInterval(3.0s)
     DOAnchorPosY(-h, 1.0s).SetEase(0x1a)               // 0x1a = 26 = Ease.InBack
     AppendCallback(Destroy(go); freeSpot = min(index, freeSpot))
```
- 常量实读（`工具/read_literal.py GameAssembly.dll`）：`DAT_1834b2bb8 = 1.0f` · `DAT_1834b2da8 = 10.0f` · `DAT_1834b2e8c = 3.0f` · `DAT_1834b2c80 = -0`（符号位）。
- `index = freeSpot; freeSpot++`：**新 toast 取当前最小空闲槽**，互不回流；退场后把 `freeSpot` 降回 `min(index, freeSpot)` ⇒ **槽位复用**。
- **无并发上限**、**没有淡入淡出**（只有位移动画）、没有缩放；每条的 `h` 是**它自己的** `rect.height`（不同文案高不同，槽位步长各按自己算）。
- 第一条的目标 y = `0*h + 10 = +10px`（Holder 原点 = 右下角 ⇒ 屏幕底往上 10px），后续向上堆。

> **缓动**：反编译里 `DOAnchorPosY` 后面**只有 `SetEase`**，没有 `SetUpdate`；DOTween `Ease.OutBack/InBack` 的公式 = `1 + c3·t³ + c1·t²` / `c3·t³ − c1·t²`（`c1 = 1.70158`、`c3 = c1+1`）。本仓 `ToastNotifications.Step` 的手写式与它逐项相同（已核）。

### 1·3 点击 / 射线

| 项 | 原版值 | 出处 |
|---|---|---|
| Toast / LevelUp / GetXP 根 | `Image.m_RaycastTarget = **1**` + `EverguildButton`（`m_Transition = 1 ColorTint`，Normal 白 / Highlighted `0.9608` / Pressed `0.7843` / `m_FadeDuration 0.1`，target = 各自的根 Image） | `MonoBehaviour_401/402/403.json` · `504/508/513.json` |
| Mission 根 | 根无 Image；子件 `Background Darkener` 的 Image `m_RaycastTarget = **1**`（色 `0.2828,0.3011,0.3868`）；`EverguildButton` `m_Transition = **0 (None)**`、`target = 0` ⇒ **没有悬停变化** | `MonoBehaviour_406.json` · `MonoBehaviour_520.json` |
| 点击回调 | `ToastNotification.Initialize`：`button.onClick` 加监听 → `context.OnClick`（`ToastNotification.<Initialize>b__9_0` 取 `ctx + 0x28` 的委托并 Invoke）；`ToastNotificationMission.Initialize` **完全不碰 OnClick** | `ToastNotification__Initialize.c` · `ToastNotificationMission__Initialize.c` |
| 穿透 | **矩形内不穿透**（整块是射线靶），矩形外穿透 | 同上 |

### 1·4 `Initialize` 语义

`context.Text → text.text`；`context.Icon != null` 时才 `Image.sprite = Icon`（**为 null 不清图标**）；最后 `gameObject.SetActive(true)`。
控制器侧还有一层分派（`TryShowNotification`）：`ControllerId` 非空**且**注册过 handler ⇒ 走 handler；否则（含「非空但没注册」）⇒ `ShowNotificationInternal`。

---

## 二、黑场与次序（证据）

### 2·1 `LoadingMenu`（唯一「全屏黑」）

| 项 | 原版值 | 出处 |
|---|---|---|
| 层 | 子画布（`m_OverrideSorting = true`）· 排序层 **`Blocking`（第 8 层）** · **order 0** | `Canvas/Canvas_1081.json`；层序 `TagManager_3.json` |
| 根 active | 主菜单场景 **`m_IsActive = false`**；`bundle_menus` prefab 与 13 个战场 = **true** | `bundle_scenes_scenes_mainmenuwarpforge/GameObject/LoadingMenu.json` · `bundle_menus_assets_all/GameObject/LoadingMenu.json` · `battlearena1/GameObject/LoadingMenu.json` |
| `StartFaded` | 主菜单实例 **0**；`bundle_menus` prefab 与 13 战场实例 **1** | `MonoBehaviour_1942.json` · `MonoBehaviour_-4255785419646353121.json` · `battlearena1/MonoBehaviour_4971.json` |
| 黑图 | `Panel`（= `LoadingImage`）sprite=null、`m_Color=(0,0,0,1)`、**`m_RaycastTarget = 1`**；stretch、`apos.y = 3`、`sd = 12006.41796875 × 10061.134765625` | `MonoBehaviour_1960.json` · `RectTransform_1106.json` |
| `Awake` | `StartFaded == 0` ⇒ **return（不显示）**；否则 `LoadingImage.SetActive(true)` + α = 1.0 | `LoadingMenu__Awake.c` |
| `LoadingFade(show, cb)` | `SetActive(true)`（**只开 LoadingImage**）→ α **立刻**设为 `show ? 0 : 1` → `DOFade(show ? 1 : 0, **1.25s**)` → `OnComplete`：**先跑 cb**，再 `if (!show) LoadingImage.SetActive(false)` | `LoadingMenu__LoadingFade.c` · `…b__0.c`；时长常量 `DAT_1834b2ec0 = 1.25f`（`read_literal.py` 实读） |
| 缓动 | `DOFade` 后**只链 `OnComplete`，无 `SetEase`** ⇒ DOTween 库默认 **OutQuad** | **[推断]**，见 §七·1 |
| 输入 | 黑图是射线靶 ⇒ **黑场 active 期间整屏点击被吃**（含 α=0 的淡入首帧、淡出到 SetActive(false) 之前）；**不挡键盘**（`WindowsManager.Update` 的 ESC 分支无 loading 门槛） | `MonoBehaviour_1960.json:22` · `Monobehaviour_2579`（GraphicRaycaster）· `WindowsManager__Update.c` |

### 2·2 谁调它（全量反编译里**只有三处**）

| 调用点 | 方向 / 次序 | 出处 |
|---|---|---|
| `MatchMakerManager.ChangeToBotBattle` | `GeneralMenuController.ShowLoading(…, **1**, …)` ⇒ **先淡到黑**，**然后**才 `MatchData.SetBotOpponent` / `BattleNetworkManager.MoveToBattleRoom`（切场景在回调里） | `Everguild.MatchMakerManager__ChangeToBotBattle.c:48` |
| `MatchMakerManager.OnMatchMakingErrorScreenCloseButtonPress` | `WindowsManager.HidePopUp()` → `ShowLoading(…, **0**, …)` ⇒ **关掉错误弹窗，再淡出黑场** | `…OnMatchMakingErrorScreenCloseButtonPress.c:9-18` |
| `BattleManager.<StartBattleSequence>` | `LoadingMenu.LoadingFade(**false**, null)`，**同帧** `cameraAnim.Play()`（= 2.5s 运镜）→ 教程/起手兵 → 换牌 | `BattleManager._StartBattleSequence_d__331__MoveNext.c:19` |

**⇒ 黑场的完整链路（菜单 → 战斗）**：
`ShowLoading(true)` 1.25s 淡到黑 → 组战斗房间/切场景 → 战斗场景的 `LoadingMenu` **`StartFaded=1` ⇒ Awake 就 α=1（接着黑）** → `StartBattleSequence` `LoadingFade(false)` 1.25s 淡回 + 运镜开始。

**⇒ 窗口开 / 关、奖励、普通界面切换：没有任何 `LoadingFade` 调用**（三处之外为 0）——**原版不为开窗/关窗铺黑场**。

### 2·3 其余「压暗 / 黑」各是谁（别混）

| 件 | 原版层 | 与本任务的关系 |
|---|---|---|
| `LoadingMenu` | **Blocking / 0**（战斗场景 **Blocking / 1000**） | 唯一的「全屏黑」；**在 toast 之上** |
| `LoadingController`（`MenuLoadingController`） | 根/暗底/骷髅/齿轮**都没有自己的 Canvas**（唯一例外 `Cog` 的 Canvas `m_OverrideSorting = false`）⇒ 继承 `Game UI` 的 **Default / 0** | ⇒ **在全部 PopUps（窗/错误横幅/教学/toast）之下**；`Menu Dark Background` α=`0.46666666865348816`、sprite=null |
| `TooltipManager` | **Blocking / 0** | **在 toast 之上**（同层同 order 与 LoadingMenu ⇒ 二者相对次序**不可判**） |
| `Tutorial highlight` | **PopUps / 601** | 在窗(0) 与错误横幅(1) 之上、**toast(900) 之下**、Blocking 之下 |
| `UI Error Message Controller` | **PopUps / 1** | 在窗(0)之上、教学(601)与 toast(900) 之下 |
| `UIBakeScreen`（模糊快照） | `Game UI`（Default）子件、`m_RaycastTarget = 0` | 在地板层；toast 之上无它 |

**窗的层**：`3 - PopUp Holder` = PopUps / 0；`bundle_menus_assets_all` 里 63 个窗 prefab Canvas 的排序档只有 `Default/0(54，override=false ⇒ 继承 PopUps/0)`、`Default 2/10/32767`、`OverElements/0`、`PopUps -3/-2/-1` —— **没有任何一档到 900** ⇒ **toast 压在所有窗之上**。

> ⚠️ **兄弟序 ≠ 跨 Canvas 的层序**：`Safe area Only Horizontal` 的子件序是 `… TooltipManager · Toast Notification Controller · Loading Controller`（toast 在 tooltip **之后**），但 Tooltip 是 **Blocking**、Toast 是 **PopUps** ⇒ **tooltip 反而在上面**。`Core/Tooltip.cs:111-117` 拿兄弟序当判据，**碰巧结论对、理由是错的**（铁律 4/10 的坑）。

---

## 三、状态 → 参数表（原版）

| 状态 | Toast | 黑场 / 压暗 | 先后 / 层序（低→高） | 证据 |
|---|---|---|---|---|
| **进游戏（启动）** | 无 | **无**（主菜单 `StartFaded=0` ⇒ `Awake` 直接 return） | — | `MB_1942.json` · `LoadingMenu__Awake.c` |
| **开场 → 主菜单** | 无 | 不是 LoadingMenu：`VideoFinished()` 淡 loginScreenManager 的 canvasGroup | — | `FinishIntro` 那条链（本仓注释）· `LoadingMenu` 无关 |
| **菜单 → 战斗** | 无（原版 toast 控制器属于主菜单场景，切场景即销毁） | **1.25s 淡到黑 → 切场景（黑着）→ 1.25s 淡回** | `LoadingMenu`(Blocking) 盖住一切 | §2·2 三处调用 |
| **关匹配错误窗** | 无 | 先 `HidePopUp()`，再 `LoadingFade(false)` **1.25s 淡回** | 同上 | `…ErrorScreenCloseButtonPress.c` |
| **战斗 → 菜单** | — | **反编译里没有 `LoadingFade` 调用**；走 `EverguildSceneManager.LoadScene` 直切 | （黑场证据为空，见 §七·5） | `BattleManager__LeaveBattle.c:58` |
| **开窗 / 关窗** | 可同时存在，toast 压窗 | **无黑场**；窗自带开关动画 | 窗 ≤ PopUps/0 < 错误横幅 1 < 教学 601 < toast 900 < Blocking(tooltip/黑场) | 三处调用之外为 0 |
| **报错消息（UIMessageController）** | 另一条通道（顶部横幅）：0.15s 入场（0.3×→1×）/ 2.0s 停留 / 0.25s 淡出，5 条一池 | 无黑场 | PopUps/1（**在窗之上、在 toast 之下**） | `MonoBehaviour_2087.json`（`appearScaleFromMultiplier .3` / `appearAnimationTime .15` / `timeToStartFading 2.0` / `timeToFade .25`） |
| **奖励 / 经验 / 成就 / 任务** | 1.0 + 3.0 + 1.0 = **5.0s**；右下角向上堆；第 0 槽 +10px | 无 | PopUps/900 | §1·2 |
| **联盟搜索（转圈件）** | 可同时存在；**toast 在转圈件之上** | 暗底 α0.4667 + 骷髅 α0.7 + 齿轮 −20°/s | 转圈件在 **Default**（最低） | `MenuLoadingController__{ShowLoading,HideLoading}.c` · `Canvas` 清单（无独立 Canvas） |
| **黑场期间请求 toast** | **照常立刻创建并计时**（只是被黑场盖住）；黑场若在 5s 内淡回 ⇒ 剩余时间可见 | — | toast < Blocking | `ShowNotificationInternal` 不看 loading 状态 |

---

## 四、我们的实现（现读，2026-10-20）

| 项 | 现状 | 位置 |
|---|---|---|
| Toast 时序 | `MoveSeconds 1 / HoldSeconds 3 / FirstSlotOffsetPx 10`；`slot*h+10`；`freeSpot = min(slot, freeSpot)` | `Shell/ToastNotifications.cs:229-231` · `:848-852` · `:1141` |
| 缓动 | 手写 OutBack/InBack（**不夹 Clamp01**，RV·H3 已修） | `:1121-1137` · `:1181-1182` |
| 几何 | `HolderRight = ScreenW + HolderAposX = 1898.5771`（RV·H1 已修） | `:238-242` · `:383` |
| 根高 | 按本次文案现量 `padT+iconH+textH+padB`（`ToastRootHeight`），常量只当兜底 | `:990-1010` · `:1019-1060` |
| 队列 | `QShade 4072 / QBg 4073 / QArt 4074 / QText 4075 / QHit 4076`（低于黑场 4090） | `:226` |
| 黑场 | `FadeSeconds 1.25` · `QPanel 4090` · `LoadingFade(bool, Action)` 逐句照原版 | `Shell/LoadingMenu.cs:85,88,164-220` |
| 黑场输入 | `PointerLayer.InputBlocked`：`Panel.activeInHierarchy` ⇒ `HitButton`/`HitScroll` 直接返回空 | `Shell/PointerLayer.cs:1367-1388,1474` |
| 切场景 | `ShellRuntime.LoadSceneWithFade`：淡到黑 → `LoadScene` → `sceneLoaded` 后隔一帧淡回；异常也淡回 | `Shell/ShellRuntime.cs:119-198` |
| 转圈件 | `QDark/QSkull/QCog = 4080/4081/4082`；**生产零调用点** | `Shell/LoadingMenu.cs:244` |
| 其他层 | Tooltip 3605–3607 · 错误横幅 4001 · 教学高亮 4100 · 模糊 2895 · 窗 ≤4030 | `Core/Tooltip.cs:127` · `Battle/ErrorMessageBanner.cs:344` · `Shell/MenuTutorialHighlight.cs:56` · `Shell/UIBakeScreen.cs:69` |

---

## 五、原版 vs 我们 · 逐条差异

> 记号：**[P1]** 与 toast/黑场同屏就会看见；**[P2]** 需要特定状态才撞上；**[P3]** 当前零生产调用、潜伏。

### D1 [P1] Tooltip 应在 toast 之上，我们的在下面
- 原版：`TooltipManager` Canvas = **Blocking / 0**、`Toast` = **PopUps / 900** ⇒ **排序层先比 ⇒ tooltip 在 toast 之上**（`Canvas_1080.json` / `Canvas_1074.json` / `TagManager_3.json`）。
- 我们：`QTip = 3606` < `Toast.QShade = 4072`（`Core/Tooltip.cs:127` / `Shell/ToastNotifications.cs:226`）⇒ toast 盖住 tooltip。
- 后果：悬停 toast 附近（或 toast 与 tooltip 重叠）时 tooltip 被压住。

### D2 [P1] 教学高亮应在 toast 与黑场**之下**，我们的在它们之上
- 原版：`Tutorial highlight` = **PopUps / 601** ⇒ 窗(0) < 错误(1) < 高亮(601) < **toast(900)** < **Blocking(tooltip/黑场)**（`Canvas_1073.json`）。
- 我们：`MenuTutorialHighlight.QShade = 4100` > toast 4072、> 黑场 4090（`Shell/MenuTutorialHighlight.cs:56`）⇒ 高亮会压住 toast，**黑场淡入时高亮仍浮在黑屏上**（原版黑场会把它一起盖住）。

### D3 [P2] 错误横幅 vs 高层窗：原版横幅在所有窗之上，我们对 `AlliancePanelWindow` 反而在下
- 原版：错误横幅 = **PopUps / 1** > `3 - PopUp Holder` = PopUps / 0 —— 窗的 prefab Canvas 最高只到 `PopUps/-1` / `Default/32767`（层更低）⇒ **横幅永远在窗之上**。
- 我们：`ErrorMessageBanner.Q = 4001`（`Battle/ErrorMessageBanner.cs:344`）< `AlliancePanelWindow.QFrame/QAvatar/QText = 4010/4020/4030`（`Shell/AlliancePanelWindow.cs:208-211`）⇒ 联盟窗会盖住横幅。
- 相反方向的两条是对的：横幅 < toast(4072) ✓、横幅 < 教学(4100) ✓。

### D4 [P3] 转圈件（`LoadingController`）应在窗/toast 之下，我们的在所有之上
- 原版：`Loading Controller` 子树**没有自己的 Canvas**（只有 `Cog` 一个 `m_OverrideSorting=false` 的子 Canvas）⇒ 继承 `Game UI` 的 **Default / 0** ⇒ 在 PopUps（窗/横幅/教学/toast）与 Blocking（tooltip/黑场）**之下**。
- 我们：`QDark/QSkull/QCog = 4080/4081/4082` > toast 4072 > 窗 4030（`Shell/LoadingMenu.cs:244`）。
- 备注：该件**今天零生产调用点**（`grep LoadingCtl` 只有 `ShellRuntime` 属性与自检）。⚠️ 原版暗底 `m_IsActive=false`、而它是 Holder 唯一子件 ⇒ 原版运行期到底怎么显示本来就没查清（`W_D_ShellLoading.md` §3·3）。

### D5 [P2] Toast 生命周期：原版随主菜单场景销毁，我们挂在 DDOL 壳上
- 原版：`ToastNotificationController` 只存在于 **主菜单场景**（`bundle_scenes_scenes_mainmenuwarpforge/MonoBehaviour_1939.json`；13 个 `battlearena*` 里 **0 处**）⇒ 切场景即整棵销毁。
- 我们：`MainMenuRuntime.Build` 把 toast 建在 **`ShellRuntime.SafeAreaOnlyHorizontal`** 下（`Shell/MainMenuRuntime.cs:524`），而壳根是 `DontDestroyOnLoad`（`Shell/ShellRuntime.cs:295`）⇒ **切到战斗后 toast 节点仍活着**（`LoadSceneWithFade` 不清它）。
- 可见性**未验证**：壳相机在 `LoadMainMenu` 里会先关（`ShellRuntime.cs:784-785`），战斗视角下 toast 会不会被哪台相机画出来**没有实况证据**；但「节点不该跨场景活着」这条与原版是有差别的。

### D6 [P2] 两次点击的 hover 色偏没接（3 颗该有、Mission 不该有）
- 原版：Toast / LevelUp / GetXP 的 `EverguildButton` 全是 `ColorTint`（Normal 白 / Highlighted `0.9608` / Pressed `0.7843` / fade 0.1）⇒ 悬停变暗一档；**Mission = None**。
- 我们：`MenuDraw.Hit(root, "Hit", rootRect, QHit, null)` 不传 `target/art`（`Shell/ToastNotifications.cs:634` 等三处）⇒ hover 打在全透明命中 quad 上，**视觉上什么都不变**（Mission 那一档「不变」反而是对的）。
- 判据：`MonoBehaviour_504/508/513.json`（ColorTint + target=401/402/403）· `MonoBehaviour_520.json`（None）。

### D7 [P3] `Player Level Text` 的折行模式仍是 `wrap:false`
- 原版 `MB_505`：`m_TextWrappingMode = 1 (Normal)`；我们 `wrap: false`（`Shell/ToastNotifications.cs:574-575`）。单/双位数字看不出差别，字段是反的（RV·L1，仍未改）。

### D8 [P3] 黑场补间用了 unscaled，注释写成「照原版」
- 原版 `DOFade` 没有 `SetUpdate` ⇒ 走 DOTween **默认 update（scaled `Time.deltaTime`）**。
- 我们 `LoadingMenu.Tick(fadeDt, …)` 由 `ShellRuntime.Update` 喂 **`Time.unscaledDeltaTime`**（`Shell/ShellRuntime.cs:309`），`LoadingMenu.cs:188` 注释称「照原版」——**这个理由不成立**（是「[我们挑的]」，`timeScale != 1` 时会与原版不同；菜单/加载期 timeScale 通常为 1，当前无可视差）。
- 判据：`LoadingMenu__LoadingFade.c` 无 `SetUpdate`；DOTween 默认 update = Normal。

### D9 [P2] 壳侧「报错」只有白字横幅，没有红色 `ShowError` 通道
- 原版 `UIMessageController.ShowError` = `ShowMessage(…, errorColor=(0.8,0,0,1))`；`ShowMessage` 用 `messageColor=(1,1,1,1)`。
- 我们：`ErrorMessageBanner.ShowError` 已有（`Battle/ErrorMessageBanner.cs:606-614`），但 `Shell/MessageToast` **只转发 `ShowMessage`**（`Shell/MessageToast.cs:197`）——壳侧想弹红字错误时没有口。
- 原版调用点：`DeckEditingWindow__OnCardClick` / `__CheckCardDrag` / `DeckInfoPopup__EditDeck`。

### D10 [P2] 根高口径仍是「静态量文本」，缺原版真机 `rect.height`
- 原版：`VLG(m_ChildControlHeight=0) + ContentSizeFitter(PreferredSize)` 三轮重建 ⇒ 根高随文案变（`N23` 结论）。
- 我们：`ToastRootHeight` 用量到的 `Label.WorldH` 现算（`Shell/ToastNotifications.cs:990-1010`）。**口径方向已对**，但**没有原版 `rect.height` 的实况读数**可对照（此前两次推导打架的教训）。

### D11 [P3] 自检把「我们起的队列下界」写成了「原版判据」
- `Editor/ShellScene.cs:7272-7277` 的 N21① 断言把 `q < LoadingController.QDark(4080)` 一起写成「原版」理由。
- 实际：原版给得住的只有 **Blocking(toast 黑场) > PopUps(toast)**；`LoadingController` 在 **Default**，本来就在 toast **之下** —— 所以「q < 4080」是**我们这张梯子**的约束，不是原版。断言本身不红，但**口径错**（自检绿 ≠ 口径对）。

### D12（已闭，仅留痕）
- `WF_UI_Generic_FancyNotification_Background` **已在 `Resources/Art/ui_menu/`**（352×116，2026-10-18 导入）⇒ RV 那条「缺图退回同族」今天不会触发；代码仍保留出声兜底。
- RV 的 H1/H2/H3/M1/M2/M3/M4/M5/L1(部分)/L2/L3 多数已在 2026-10-17～18 修掉（本报告 §四 对应行可作复核）。

---

## 六、落地建议（文件 + 符号）

> 顺序按「与 toast/黑场同屏可见度」排。改了 `Core/Tooltip.cs` / `PointerLayer` 这类共用件 ⇒ 按铁律 12 跑受影响的宿主（Tooltip 是 shell 共用件 ⇒ `ShellScene/MainMenuScene/RewardsScene/ShopScene/CollectionScene`；`MenuTutorialHighlight` ⇒ 有教学流程的 `ShellScene/MainMenuScene`）。

1. **D1 Tooltip 抬到 toast 之上、黑场之下**
   - 文件/符号：`Core/Tooltip.cs` 的 `QTipShadow/QTip/QTipText`（`:127`）→ `4077 / 4078 / 4079`。
   - 同批把 `Tooltip.cs:109-124` 那句「兄弟序 ⇒ tooltip 最高」改成「**排序层 Blocking > PopUps** ⇒ 在 toast/窗之上」，并写明与 `LoadingMenu`（同层同 order）相对次序**不可判**；我们取「tooltip 在黑场之下」是**[我们挑的]**。
   - 战斗侧不受影响：`Tooltip.UseHost` 后 `UguiMode=true`，队列写入是空操作（`Core/Tooltip.cs:234` 注释）。

2. **D2 教学高亮降到 toast 与黑场之下**
   - 文件/符号：`Shell/MenuTutorialHighlight.cs` 的 `QShade`（`:56`）→ 建议 **4040**（窗 ≤4030 之上、toast 4072 之下），并改那一行「压在所有窗之上、指针之下」的注释为「**原版 PopUps/601：窗之上、toast 与 Blocking 之下**」。

3. **D3 错误横幅在窗之上（按档分开）**
   - 文件/符号：`Battle/ErrorMessageBanner.cs` 的 `Q`（`:344`）+ `LayoutPreset`（`MenuPreset`）。**⛔ 别直接改战斗档**；给菜单档加一个 `MenuQueue = 4035`（窗 4030 之上、教学 4040 / toast 4072 之下），调用点 `Shell/MessageToast.cs:170` 的 `ErrorMessageBanner.Create(..., MenuPreset)` 走它。

4. **D4 转圈件层级（潜伏件）**
   - 文件/符号：`Shell/LoadingMenu.cs` 的 `QDark/QSkull/QCog`（`:244`）。
   - 原版 = Default 层（在全部 PopUps 之下）。若将来接线到「主菜单内容区的搜索」（原版 `JoinAllianceMenu` 那种），应放到 `2999–3001`（壳内容之上、窗之下）；⚠️ **但我们的联盟搜索今天在窗里** ⇒ 放低会看不见。**先记录为设计差异，等界面线决定「联盟搜索要不要改成主菜单页」再一起动**（铁律 11：要做，只是先后）。

5. **D5 切场景前清 toast**
   - 文件/符号：`Shell/ShellRuntime.cs` 的 `LoadSceneWithFade`（`:119`）；在 `loading.LoadingFade(true, () => { … SceneManager.LoadScene … })` 之前（或 `sceneLoaded` 回调里）调 `ToastNotifications.DisposeForTest()`。
   - 若要保留控制器、只清活动条，建议在 `Shell/ToastNotifications.cs` 加 `public static void ClearLive()`（销毁 `_live` 里每条、`_live.Clear()`、`_freeSpot=0`、`_scheduled.Clear()`），再在 `LoadSceneWithFade` 调它 —— 更贴近原版「切场景 = toast 全没」。

6. **D6 hover 色偏**
   - 文件/符号：`Shell/ToastNotifications.cs` 的三处 `MenuDraw.Hit(root, "Hit", rootRect, QHit, null)`（`BuildToast` / `BuildLevelUp` / `BuildGetXP`）→ 传 `target: <根 Background 的 ImageQuad>`、`art: null`，由 `WindowButton` 的 ColorTint 打色。
   - ⚠️ **Mission 那一颗保持不传**（原版 `m_Transition = 0`），⛔ 别四颗一起补。
   - 参数：Normal `(1,1,1,1)` / Highlighted `(0.9608…)` / Pressed `(0.7843…)`，`m_FadeDuration 0.1`。

7. **D7 `Player Level Text` 折行**：`Shell/ToastNotifications.cs:575-576` 的 `wrap: false` → `true`（`MB_505` = Normal）。

8. **D8 黑场 dt**：`Shell/ShellRuntime.cs:151` 的 `Loading.Tick(Time.unscaledDeltaTime, Time.deltaTime)` —— 若要严格照 DOTween 默认，应喂 `Time.deltaTime`；若保留 unscaled，**改 `LoadingMenu.cs:188-189` 的「照原版」措辞为「[我们挑的]」**（加载期 timeScale=1，当前零可视差）。

9. **D9 壳侧红字错误**：`Shell/MessageToast.cs` 加 `public static bool ShowError(string text, bool localize=false, string fallback=null)` → `_banner.ShowError(...)`（判据 `Battle/ErrorMessageBanner.cs:606-614` / 原版 `UIMessageController.ShowError`）。

10. **D11 自检口径**：`Editor/ShellScene.cs:7267-7277` 把断言拆成两句 —— ①「原版判据：toast < 加载黑场」只引 `TagManager_3.json` + `Canvas_1074/1081`；②「我们的梯子：BlockingOverlay 4071 < toast < LoadingController.QDark 4080」标成 **[我们挑的]**（并说明原版 `LoadingController` 在 Default、本就在 toast 之下）。

11. **文档过期（铁律 5，需主对话改；本代理只读）**
    - `资料/普查产出_1017/UI/R_界面线剩余条目现核.md:33,127` 的 `N21①`：「Toast 在黑场之上 / 已经同序」**不成立**（只比了 order、漏了 `m_SortingLayerID`）⇒ 应改为「加载黑场在 toast 之上」。
    - `资料/普查产出_1017/UI/D_T16_UIBakeScreen判据.md:70` 的「**N21 仍未定**」⇒ 已定，指向本报告。
    - `资料/任务线/界面线/任务总表.md:61,73` 的「Toast/黑场次序（判据未定）」⇒ 可改为「判据已定（W58 报告）；余 D1–D11 落地」。

---

## 七、没查清的部分（逐条写「查了什么 / 还差什么」）

1. **DOTween 库默认缓动是不是 OutQuad（`LoadingFade` 的 1.25s 曲线）—— 仍是[推断]。**
   本轮试过：① 全量反编译 `grep defaultEaseType` = **0 命中**（游戏代码没有覆盖默认缓动）；② `LoadingFade` 的 `DOFade` 后面**没有 `SetEase` / `SetUpdate`**；③ 读 `d:/2/unity_run_ref/MelonLoader/Dependencies/Il2CppAssemblyGenerator/Cpp2IL/cpp2il_out/DOTween.dll` —— 它的 `.cctor` IL 只有 `ret`（**Cpp2IL 空壳**，不是真方法体）；④ `all_methods.txt:266202` 的 `DG.Tweening.DOTween$$.cctor` RVA `11344144` 反汇编出来**不是 cctor**（是带 4 个参数的初始化壳），方法→RVA 映射不可靠。
   ⇒ **要坐实**：用 Ghidra 在 `GameAssembly.dll` 里定位 `DG.Tweening.DOTween` 的静态字段初始化（找 `defaultEaseOvershootOrAmplitude = 1.70158f` 附近）或起原版 player 打一条 `DOTween.defaultEaseType` 探针。⛔ 在此之前别把「OutQuad」写成原版实读。
2. **主菜单里谁把 `LoadingMenu` 根打开**：场景 `m_IsActive=false`，`LoadingFade` 只开 `LoadingImage`；`GeneralMenuController.ActivateLoadingFade` 在本语料**零调用点**（⚠️ `decomp_full` 有已知符号错配，见 `已知的坑.md:6497`）⇒ **运行期开根点未查到**。我们显式开根（`LoadingMenu.cs:169`）是[我们挑的]。
3. **`MenuLoadingController` 的运行期姿态 / 暗底到底显不显示**：场景里 `Menu Dark Background` `m_IsActive=false` 且它是 Holder 的唯一子件（⇒ 骷髅/齿轮一起不显示），而 `ShowLoading` 只开 Holder ⇒ 序列化值与运行时矛盾（`W_D_ShellLoading.md` §3·3 已记）。没跑实况，**不猜**。
4. **Toast 根高（`Toast` / `Get XP`）的原版真机 `rect.height`**：只有静态推导（uGUI 三轮重建）。要一锤定音得进原版/player 打一次 `rect.height`（`Toast ▸ Text` 的一行 vs 多行两种文案）。
5. **战斗 → 菜单有没有黑场**：三处 `LoadingFade` 调用点之外没找到；`EverguildSceneManager.LoadScene` / `SceneChanger`（`WaitForSeconds` 后 `LoadScene`）都不带淡入淡出；`OpenCloseTransition`（`OutAnimation/LoadingAnimation/InAnimation`，DORotate `Ease 0x16` + `WaitForSeconds`）在本语料里**只有自身三个方法**、没有调用方（符号错配风险同上）⇒ 那条「场景开合转场」的接线**未查清**。
6. **TooltipManager 与 LoadingMenu 的相对次序（主菜单）**：两者都是 `Blocking / 0`、都是 override 子画布 —— 同层同 order，Unity 的回退次序（创建/注册顺序）本地不可判。**战斗场景可判**：`LoadingMenu` = Blocking **order 1000**（`battlearena1/Canvas_2576.json`）> Tooltip Blocking 0。我们取「tooltip < 黑场」是[我们挑的]。
7. **Toast 跨场景的可见性**：节点挂在 DDOL 壳上（D5），但战斗场景里由哪台相机、画不画得出来没有实况证据。只断言「生命周期与原版不同」，不断言「一定看得见」。
8. **`BlockingOverlay`（原版）的层**：`level0/GameObject/BlockingOverlay.json` 只有 `Transform + MonoBehaviour`，`level0/MonoBehaviour` 没有对应 typed 导出（无 type tree 字段），其引用的可视件（字段 `0x28 / 0x40`）**解不出来** ⇒ 我们的 4070/4071 是[我们挑的]。

---

## 附 · 一页速查

```
排序层（TagManager_3.json）：
  Default(0) … OverHand(6) < PopUps(7) < Blocking(8)

原版主菜单各 Canvas（全部挂在 Game UI / ScreenSpaceCamera 下）：
  UIBakeScreen / LoadingController  → Game UI   Default  0   （最低）
  3 - PopUp Holder                  → PopUps    0
  UI Error Message Controller       → PopUps    1
  Tutorial highlight                → PopUps  601
  Toast Notification Controller     → PopUps  900   （PopUps 最高）
  TooltipManager                    → Blocking  0
  LoadingMenu                       → Blocking  0     （战斗场景 = Blocking 1000）

原版 Toast：
  1.0s OutBack → 3.0s → 1.0s InBack；起始 -h，目标 index*h+10px；freeSpot 复用；无上限
  矩形内吃点击（Image.raycastTarget=1 + Button）；Toast/LevelUp/GetXP 有 hover 色偏，Mission 没有
```

（本报告 = `d:/4/Unity/资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md`）
