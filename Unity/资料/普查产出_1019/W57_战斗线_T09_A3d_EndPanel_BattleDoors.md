# W57 · 战斗线 `T09` 步骤 A3-d —— `EndPanel` + `BattleDoors`（结算按钮 / 门）

> **日期** 2026-10-20（第五十七会话）· **代理** `T09-A3d`（执行）
> **判据入口** `资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §2 步骤 A3-d（`:180`）· A1 报告
> `W57_战斗线_T09_A1.md`（`EventSystem` / `BattleHudButton` 已就绪）
> **白名单** = `Unity/MyGame/Assets/CardPresentation/Battle/EndPanel.cs` · `…/Battle/BattleDoors.cs`
> （**只有这两个文件被改**；`git diff --numstat`：`EndPanel.cs +60/−0` · `BattleDoors.cs +35/−1`）
> **本波口径 = 只做不验**：未跑 Unity / 未起 player / 未跑 `*.Run` / 未跑 `_run_8_checks.sh`；只跑了秒级 typecheck。
> 未动 git、未改正本、未删文件；**未改** `Editor/**`（T09-A2 在改）· `BattleDriver` / `HudCanvas` / `BattleLogPanel` / `WfSlider`。

---

## 〇、一句话

**原版这一屏一个按钮都没有**（2026-10-20 现读两个战场包的 `EndBattlePanel` 整棵子树：只有
`Background` / `Video Image` / `AllRewardsHolder` 三支，**零 `m_OnClick`、零 `EventTrigger` / `PointerDown`**）
⇒ A3-d 的「结算按钮」= **无按钮可迁**；本步真正落地的是**模态闸**：
压暗层 `_dim` 与视频层 `_screen` 的 `raycastTarget` 按原版 `m_RaycastTarget = 1` 置 `true`
（**只吃射线、不挂任何点击处理器** —— 「点任意处离开」是原版 `Input.GetMouseButtonDown(0)` 裸输入，
不经过 uGUI）。两个文件本来就已在 2026-10-16 批 2 迁到 uGUI，本步**没有**任何世界空间件要搬。

---

## 一、改前 / 改后对照

| # | 件 | 改前（现读） | 改后 | 出处（改后行号） |
|---|---|---|---|---|
| 1 | `EndPanel._dim`（结算压暗层 / 全屏图） | `HudCanvas.MakeImagePiece` 建出来是**装饰件** ⇒ `Image.raycastTarget = false`（`HudCanvas.MakeImage:146` 的默认）。uGUI 下点面板空白会**穿透到背后的 HUD 钮** | `HudPiece.SetRaycastTarget(true)`；失败 `LogError`（⛔ 不静默）。`raycastPadding` 不显式写（默认 `Vector4.zero` = 原版 `(0,0,0,0)`，判据 → 本机 uGUI `Graphic.cs:188`） | `EndPanel.cs:220-236`（`Build()` 的 `_dim` 块） |
| 2 | `EndPanel` 的**按钮** | 无（本件本来就没有按钮） | **仍然没有** —— 原版该子树零按钮（判据见 §二），⛔ 不造一个「原版没有的」 | — |
| 3 | `BattleDoors._screen`（`Video Image` 那颗 `RawImage`） | `raycastTarget = false`（注释写着「装饰件不吃射线」） | `raycastTarget = true`（原版 `m_RaycastTarget = 1`）。对 `Playing` / `_finished` / `Advance` / `DropFrame` **零影响**（这四个都不读它） | `BattleDoors.cs:336-345` |
| 4 | 自检探针（Editor-only 只读） | 无 | `EndPanel`：`DimPieceForTest` / `DimGraphicForTest` / `DimRaycastTargetForTest` / `DimRaycastPaddingForTest`；`BattleDoors`：`ScreenGraphicForTest` / `ScreenRaycastTargetForTest` / `ScreenRaycastPaddingForTest`。**全在 `#if UNITY_EDITOR` 里**（播放器构建不存在） | `EndPanel.cs:584-625` · `BattleDoors.cs:505-528` |
| 5 | 开门/关门时序 · 回调 | `SetupDoor` → `Play` → `Advance` → `Pause`/`_finished`；`DropFrame`；`Stop` | **逐字未动** | `BattleDoors.cs:285-465`（本波零改动） |
| 6 | 对外 API | — | **零删除 / 零改签名 / 零改可见性**；新增只有 `#if UNITY_EDITOR` 的只读探针 | 见 §三 |

**没有改的（有意）**：`_title` / `_sub` / `_skullPlate` / `_skulls[i]` 这些**装饰件继续 `raycastTarget = false`**
（计划 §2 A3 的口径是「**全屏件**置 true」；原版这些图是 uGUI 默认的 1，但**整棵子树没有任何点击处理器**，
且它们都被压暗层盖在下面 ⇒ 两种取值**无可观察差异**。如实记在这里，不冒充「原版就是 false」）。
`Background` 的**外观**（原版是 3840×2160 的 sprite `Background` + `rgba(0.0283,0.0283,0.0283,0.753)`；
我们是纯黑 `alpha 0.97` 的替代品）**不在本步范围**，见 §五·1。

---

## 二、原版判据（现读 prefab，⛔ 不是猜）

**读法**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena{1,2}/`
—— `EndBattlePanel` 的 `RectTransform_2832`(a2) / `_2639`(a1) 往下**整棵递归**，逐件读 `m_Component`。

### 2·1 子树形状（两个包同构，`a2` 的 pid）

```
EndBattlePanel           RT_2832 + CanvasRenderer_2075                 ← 无任何 MonoBehaviour
├─ Background            RT_2830 + CR_2127 + Image_5043                ← 全屏压暗图
├─ Video Image           RT_3596 + CR_2344 + RawImage_5177 + VideoPlayer_3788
└─ AllRewardsHolder      RT_3047 + CR_5118 + GridLayoutGroup_5118 + CanvasGroup_3773
   ├─ SkullsHolder …（skull1/2/3）
   └─ RewardsHolder …（数量 / 奖杯 / 评分）
```

### 2·2 两条与本步直接相关的字段

| 节点 | 组件 | `m_RaycastTarget` | `m_RaycastPadding` | `Button` / `EventTrigger` | 判据（两包同值） |
|---|---|---|---|---|---|
| `Background` | `Image` | **1** | **`(0,0,0,0)`** | **无** | a2 = `MonoBehaviour_5043`；a1 = `MonoBehaviour_4830` |
| `Video Image` | `RawImage` | **1** | **`(0,0,0,0)`** | **无** | a2 = `MonoBehaviour_5177`；a1 = `MonoBehaviour_4961` |

- 其余图（`SkullsHolder` 底板 / 三个骷髅 / `RewardsHolder` / `Trophy` / `ArmyIcon` / 两个 TMP 文字）
  也都是 `m_RaycastTarget = 1`（uGUI 新建 Graphic 的默认值），**同样零处理器**。
- **整棵子树零 `m_OnClick`**：把 18 个 MonoBehaviour 逐个 dump 过键名 —— 只有 Image / RawImage /
  GridLayoutGroup / TMP / `RewardHolder`（`drawerHolder`+`quantityText`）四类键，**没有任何 `m_OnClick`**。
- 运行时 dump 独立复核：`资料/原版参照图/Unity参照管线_0825/data/runtime_ui_dump_Battle_Arena_1.tsv:1147-1164`
  —— 16 行，`EndBattlePanel` 子树只有 `Background`(IMG `Background`) / `Video Image` / `AllRewardsHolder` 三支，
  **没有任何按钮节点**。

### 2·3 「点任意处离开」为什么**不能**做成 uGUI 点击

反编译 `BattleManager__Update.c:116-120`（`d:/2/tools/decomp_full/`）：

```c
if ((*(char *)(param_1 + 0x510) != '\0') &&
   ((cVar13 = UnityEngine_Input__GetMouseButtonDown(0,0), cVar13 != '\0' ||
    (cVar13 = UnityEngine_Input__GetKeyDownInt(0x1b), cVar13 != '\0')))) {
  CustomDebug__Log(DAT_184235ad8,0);
  BattleManager__LeaveBattle(param_1,0);
```

= **闸门（`+0x510`）开了之后读裸输入**（左键按下沿 / ESC），**不经过 EventSystem**
⇒ 我们的 `BattleDriver.HandleEndBattleExit`（`BattleDriver.cs:8241`，`ClickedThisFrame()`）**保持原样**，
压暗层**不该挂 `IPointerClickHandler`**（挂了反而会多出一条原版没有的路径）。

---

## 三、API / 数值行为不变的证据

1. **对外 API 一个字没改**：`git diff` 里**没有**任何既有成员的删除 / 签名 / 可见性改动；
   新增的只有 `#if UNITY_EDITOR` 段里的 7 个**只读**成员（属性，无 setter、无副作用）。
2. **`BattleDoors` 的时序链逐字未动**：`SetupDoor` / `Play` / `Advance` / `DropFrame` / `Stop` /
   `Finished` / `ExitReady` 的代码**一行没改**；本波只改了 `SetupDoor` 里 `_screen.raycastTarget` 的**一个布尔值**。
   `EndPanel.Show` / `Advance` / `Hide` / `SetVisible` 同样**零改动**。
3. **`_dim` 的矩形 / 层级 / 颜色**零改动：仍是 `FillParent` + `Reorder(…, ZDim)` + `new Color(0,0,0,0.97f)`。
4. **`raycastPadding` 的等价性**：原版 `(0,0,0,0)` ≡ `Graphic` 的字段初值
   （本机 `…/com.unity.ugui/Runtime/UGUI/UI/Core/Graphic.cs:188` `private Vector4 m_RaycastPadding = new Vector4();`）
   ⇒ 不写就是原版值；探针 `DimRaycastPaddingForTest` / `ScreenRaycastPaddingForTest` 供断言现读。
5. **`SetRaycastTarget` 的失败会出声**：`HudPiece.SetRaycastTarget` 本身对「世界空间件 / 没有 Image」
   两支已 `LogError` 并返回 `false`，`EndPanel` 调用点**再补一条**（⛔ 不静默半迁移）。

---

## 四、没查清的部分（⛔ 不猜，给「还差什么」）

1. **批处理编辑模式下 `Graphic` 是否登记进 `GraphicRegistry`** —— 本波不跑 Unity，**没实测**。
   `HudCanvas.MakeImage` 是「活动 GO + `AddComponent<Image>()`」，而本仓已知「编辑模式下 `AddComponent`
   不一定派 `OnEnable`」。若没登记，未来 `EventSystem.RaycastAll` 打不到压暗层 ——
   那属于**编辑模式补偿缺口**（A1 的 `BattleEventSystem.SyncRaycasters()` 只补 `GraphicRaycaster`，
   **不补 `GraphicRegistry`**），不是本件逻辑错。**第一次真跑 `BattleScene.Run` 时先看这一条。**
2. **`EndPanel` 出厂是 `Hide()` 状态**（`Create` 末尾 `p.Hide()`），子件全 `SetActive(false)`。
   「打开后压暗层能不能吃到射线」依赖 `SetActive(true)` 派 `OnEnable` 把 Graphic 登记回去 ——
   与上一条同一个前提，**未实跑**。
3. **`RawImage.raycastTarget = true` 在真机上的实际观感**：原版那层 `Video Image` 是 uGUI 默认值没关，
   我们改成 true 之后「指针在视频上」与「在压暗上」都会报 `IsPointerOverGameObject`——
   本仓**生产代码没有任何一处读它**（`CombatCameraZoom.PointerOverUi` 是空委托、没人接线；
   `BattleDriver` 那几处只是注释）⇒ 预期零影响，**未实跑**。
4. **原版 `Background` 的 sprite 名 = `Background`**（运行时 dump 的 IMG 列），但**那张图我们没做**
   （见 §五·1）。它跨包引用（`m_FileID = 9` / `pid 1660267235368898380`），**本地没定位到对应的
   `Sprite_*.json`**（`assets_full` 两层内 find 零命中）⇒ 「那张图长什么样 / 有没有在本地」**没查清**。
5. **`EndBattlePanel` 与原版父子颠倒**（原版 `BattleDoors/Canvas → EndBattlePanel`；我们
   `EndPanel → BattleDoors`）**本波没动** —— 那是计划 §七·1 的另一笔账（下游 = 「小 6.65% 只修一半」）。

---

## 五、顺手发现（记账，⛔ 本波不改白名单外的东西）

1. **原版压暗层的真值（外观）**：`Background` 那颗 `Image` 是
   **`m_Color = (0.0283, 0.0283, 0.0283, 0.753)` · `m_Type = 1`(Sliced) · `m_PreserveAspect = 0` ·
   sprite 名 `Background` · 节点 3840×2160**。我们今天是**纯黑 `alpha 0.97` 的实心替代**
   （`EndPanel.cs:213` 那段注释自陈「这是替代，不是原版」）。
   ⇒ 按铁律 11 这是**与原版不符**、应做；但它是「缺那张 sprite」的替代件，**不是本步（按钮/门）的范围**，
   **本波没动**。**建议的下一步**：先定位那张 sprite（§四·4 没查清），拿到之后再换成原版贴图 + 原版色。
2. **`_screen` 的 `raycastTarget` 原来写死 `false`、注释写「装饰件不吃射线（第 4 步的可点件才吃）」**
   —— 那条口径对**钮**成立，对**原版本来就是射线靶的全屏层**不成立（本波已就地订正）。
   同族的 `MulliganPanel._shade` / `BattleLogPanel._shade` / `SettingsPanel` 空白区在 A3-c/A3-e 那两笔里，
   本波**没碰**（别人的文件）。
3. **`EndPanel` / `BattleDoors` 没有任何 `PointerDown` 类事件的需求** —— 原版子树里
   `EventTrigger` 的敌人只有 `BattleLogPanel` 那块 `shade`（A3-c 的判据），别把那条口径套到结算屏上。

---

## 六、typecheck 结果

```bash
cd d:/4/Unity/MyGame
TMPDIR=/tmp/wf_t09a3d bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 改前基线同样 **0 / 0**（先跑过一次再动手）；改后连跑两次（第二次在订正注释之后）都是 **0 / 0**。
- `UNITY_EDITOR` 在 `/tmp/wf_t09a3d/wf_csc.rsp` 里**已定义**（现核）⇒ `#if UNITY_EDITOR` 探针**确实被编到了**。
- 本波期间**没有**撞上其它代理在飞文件的半成品错误。

行尾核对（二进制计数；两个文件改前都是 CRLF）：

| 文件 | 改前 | 改后 | 结论 |
|---|---|---|---|
| `Battle/EndPanel.cs` | CRLF 567 / LF 567（567 行） | **CRLF 627 / LF 627** | ✅ 未整篇翻（+60 行） |
| `Battle/BattleDoors.cs` | CRLF 496 / LF 496（496 行） | **CRLF 530 / LF 530** | ✅ 未整篇翻（+34 行） |

```
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Battle/{EndPanel,BattleDoors}.cs
60      0       Unity/MyGame/Assets/CardPresentation/Battle/EndPanel.cs
35      1       Unity/MyGame/Assets/CardPresentation/Battle/BattleDoors.cs
```
