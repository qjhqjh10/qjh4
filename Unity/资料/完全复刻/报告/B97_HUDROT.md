# `B97_HUDROT` 报告 —— 战斗 HUD 三颗 `Highlight` 的自转（原版 `SimpleRotation`）

> 账行：`账/未分类_其他.tsv:584`（`SimpleRotation.Update()`，状态 **B**）
> 判据来源全部本轮现读（`assets_full` + `decomp_full`），⛔ 没有沿用任何旧结论。
> 一句话结论：**缺口不在战场 prefab，在战斗 HUD** —— 那颗「选中打法的黄圈」我们早就建好了（rect / 贴图 / 显隐都在），
> 缺的是**驱动**。本批补上 `100°/s` 的自转，并把它钉成 12 条断言。

---

## 一、取证表（`SimpleRotation` 全库 42 个实例，逐个现读）

识别方式：按 `m_Script.m_PathID = -6857224376968460714` 扫 `assets_full` 全库
（类名出处 = `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-6857224376968460714.json` → `SimpleRotation`，`m_Namespace` 空、`Assembly-CSharp`）。
`dump.cs` 里该类只有一个字段 `speed`（`+0x20/+0x24/+0x28`）。

### 1·1 竞技场 39 个（13 场 × 3）—— **几何 / 贴图 / speed 全同，无一场例外**

宿主一律是 `Drag Attack Selector` 那三颗钮各自的**子件** `Highlight`（`m_Name` 都是 `Highlight`；
GO 文件在导出里分别落成 `Highlight.json`（第一份）与 `Highlight_<pid>.json`）。

| 场（`bundle_scenes_scenes_battlearena…`） | `SimpleRotation` 组件 | 宿主 GO | **父节点（= 那颗钮）** | 父 `RectTransform` | 本件 `RectTransform` | `speed.z` |
|---|---|---|---|---|---|---|
| 1 | `MonoBehaviour_4481` | `Highlight`(GO 112) | `Select Melee Button` | 3368 | 3028 | 100 |
| 1 | `MonoBehaviour_4305` | `Highlight`(GO 396) | `Select Active Skill Button` | 2626 | 2776 | 100 |
| 1 | `MonoBehaviour_4736` | `Highlight`(GO 180) | `Select Range Button` | 2804 | 3512 | 100 |
| 2 | `MonoBehaviour_4681` | `Highlight` | `Select Melee Button` | 3563 | 3221 | 100 |
| 2 | `MonoBehaviour_4502` | `Highlight` | `Select Active Skill Button` | 2819 | 2969 | 100 |
| 2 | `MonoBehaviour_4945` | `Highlight` | `Select Range Button` | 2997 | 3707 | 100 |
| 3 | `MonoBehaviour_4603` | `Highlight` | `Select Melee Button` | 3481 | 3141 | 100 |
| 3 | `MonoBehaviour_4424` | `Highlight` | `Select Active Skill Button` | 2739 | 2889 | 100 |
| 3 | `MonoBehaviour_4859` | `Highlight` | `Select Range Button` | 2917 | 3625 | 100 |
| aeldari | `MonoBehaviour_4453` | `Highlight` | `Select Melee Button` | 3329 | 2989 | 100 |
| aeldari | `MonoBehaviour_4276` | `Highlight` | `Select Active Skill Button` | 2587 | 2737 | 100 |
| aeldari | `MonoBehaviour_4710` | `Highlight` | `Select Range Button` | 2765 | 3473 | 100 |
| astramilitarum | `MonoBehaviour_4651` | `Highlight` | `Select Melee Button` | 3532 | 3192 | 100 |
| astramilitarum | `MonoBehaviour_4476` | `Highlight` | `Select Active Skill Button` | 2790 | 2940 | 100 |
| astramilitarum | `MonoBehaviour_4907` | `Highlight` | `Select Range Button` | 2968 | 3676 | 100 |
| blacklegion | `MonoBehaviour_4556` | `Highlight` | `Select Melee Button` | 3444 | 3104 | 100 |
| blacklegion | `MonoBehaviour_4382` | `Highlight` | `Select Active Skill Button` | 2702 | 2852 | 100 |
| blacklegion | `MonoBehaviour_4811` | `Highlight` | `Select Range Button` | 2880 | 3588 | 100 |
| darkangels | `MonoBehaviour_4529` | `Highlight` | `Select Melee Button` | 3419 | 3079 | 100 |
| darkangels | `MonoBehaviour_4356` | `Highlight` | `Select Active Skill Button` | 2677 | 2827 | 100 |
| darkangels | `MonoBehaviour_4782` | `Highlight` | `Select Range Button` | 2855 | 3563 | 100 |
| emperorschildren | `MonoBehaviour_4527` | `Highlight` | `Select Melee Button` | 3423 | 3083 | 100 |
| emperorschildren | `MonoBehaviour_4354` | `Highlight` | `Select Active Skill Button` | 2681 | 2831 | 100 |
| emperorschildren | `MonoBehaviour_4780` | `Highlight` | `Select Range Button` | 2859 | 3567 | 100 |
| genestealers | `MonoBehaviour_4492` | `Highlight` | `Select Melee Button` | 3385 | 3045 | 100 |
| genestealers | `MonoBehaviour_4317` | `Highlight` | `Select Active Skill Button` | 2643 | 2793 | 100 |
| genestealers | `MonoBehaviour_4745` | `Highlight` | `Select Range Button` | 2821 | 3529 | 100 |
| leviathan | `MonoBehaviour_4648` | `Highlight` | `Select Melee Button` | 3531 | 3191 | 100 |
| leviathan | `MonoBehaviour_4475` | `Highlight` | `Select Active Skill Button` | 2789 | 2939 | 100 |
| leviathan | `MonoBehaviour_4905` | `Highlight` | `Select Range Button` | 2967 | 3675 | 100 |
| sororitas | `MonoBehaviour_4697` | `Highlight` | `Select Melee Button` | 3589 | 3249 | 100 |
| sororitas | `MonoBehaviour_4522` | `Highlight` | `Select Active Skill Button` | 2847 | 2997 | 100 |
| sororitas | `MonoBehaviour_4954` | `Highlight` | `Select Range Button` | 3025 | 3733 | 100 |
| spacewolves | `MonoBehaviour_4494` | `Highlight` | `Select Melee Button` | 3388 | 3048 | 100 |
| spacewolves | `MonoBehaviour_4320` | `Highlight` | `Select Active Skill Button` | 2646 | 2796 | 100 |
| spacewolves | `MonoBehaviour_4749` | `Highlight` | `Select Range Button` | 2824 | 3532 | 100 |
| tauviorla | `MonoBehaviour_5080` | `Highlight` | `Select Melee Button` | 3965 | 3625 | 100 |
| tauviorla | `MonoBehaviour_4902` | `Highlight` | `Select Active Skill Button` | 3223 | 3373 | 100 |
| tauviorla | `MonoBehaviour_5337` | `Highlight` | `Select Range Button` | 3401 | 4109 | 100 |

**这 39 份逐字段现读，值完全相同**（`battlearena1` 的 `Highlight_396` 作样本，其余 38 份同值）：

| 字段 | 值 | 出处 |
|---|---|---|
| `RectTransform.m_AnchorMin` / `m_AnchorMax` | `(0.5, 0.5)` / `(0.5, 0.5)` | `RectTransform/RectTransform_2776.json` |
| `m_AnchoredPosition` | `(0, 0)`（少数是 `-3.814697265625e-05` 的浮点噪声） | 同上 |
| `m_Pivot` | `(0.5, 0.5)` | 同上 |
| `m_LocalScale` | `(1, 1, 1)` | 同上 |
| `m_LocalRotation` | identity ⇒ **起始角 0°** | 同上 |
| **`m_SizeDelta`** | **`(176.22999572753906, 176.875)`** | 同上（`Highlight_180` 的 RT 3512 同值） |
| `m_Children` | `[]`（圈是叶子） | 同上 |
| `Image.m_Sprite` | `Attack type button highlight`（`m_FileID 10` / pathID `-7315808829623560242`） | `MonoBehaviour/MonoBehaviour_4108.json` |
| `m_Color` / `m_Type` / `m_PreserveAspect` / `m_RaycastTarget` | `(1,1,1,1)` / `0`(Simple) / `0` / `1` | 同上 |
| `SimpleRotation.speed` | **`(0, 0, 100)`** | `MonoBehaviour/MonoBehaviour_4305.json` |
| `m_IsActive`（GameObject） | `true`（三份都是；运行时才按「已选打法」开关） | `GameObject/Highlight.json` / `Highlight_180.json` / `Highlight_396.json` |
| 第 5 个组件 | `EverguildButtonMaterialModifier`（`m_Script -9096086197743689236`，**序列化里一个字段值都没有**） | `MonoBehaviour/MonoBehaviour_4693.json` |

父链（arena1 现读，其余 12 场同构、只有 pathID 不同）：
`Highlight` → `Select Melee / Active Skill / Range Button`（RT 3368 / 2626 / 2804）→
`Drag Attack Selector`（RT 2973，621.7249755859375 × 122.24700164794922）→
`Safe area FrontCanvas`（2862）→ `FrontCanvas`（2900）→ `Canvas`（2759）→ `BattleHud`（1395）→ `BattlePrefab`（1300）。
⇒ 与 `AttackSelector.Order = { Melee, Ability, Ranged }` **一一对应**。

贴图定义（同名图我们已在用）：
`d:/2/解包整理/03_界面UI/图集/battleatlasui/Sprite/Attack type button highlight_-7315808829623560242.json`
= 227 × 227 · `m_PixelsToUnits 100` · `m_Pivot (0.5, 0.5)` · `textureRect (0,0,227,227)`。
我们那份 = `Assets/CardPresentation/Resources/Art/ui/Attack_type_button_highlight.png`
（读出确实是**非旋转对称**的一圈：白外圈 + 黄内圈 + 两个高光斑 ⇒ 转起来看得见）。

### 1·2 另外 3 个 `Cog`（**不属战斗 HUD**，本批不动）

| `bundle` | `SimpleRotation` 组件 | 宿主 | `speed.z` | 宿主宿主链 / 我们的现状 |
|---|---|---|---|---|
| `menus_assets_all` | `MonoBehaviour_-2329547480181669978` | `Cog`（GO `-3685809886447435866`） | **-20** | 父 `8608300487762018214`（跨 CAB，未解）；贴图 `5609191530438287762`（`40K_icon_searching_cog`）· α 0.8 |
| `menus_assets_all` | `MonoBehaviour_3598788870134810733` | `Cog`（GO `3738674841765267565`） | **-20** | 同上（α 1.0） |
| `scenes_scenes_mainmenuwarpforge` | `MonoBehaviour_2104` | `Cog`（GO 464） | **-20** | ✅ **我们已做**：`Shell/LoadingMenu.cs:376` 的 `LoadingController.Tick` 走 `SimpleRotation.Step` |

`Cog` 的 `RectTransform` 是**拉伸锚点**（`anchorMin (0,0)` / `anchorMax (1,1)` / `sizeDelta (0,0)`）⇒ 尺寸由父框决定，
⛔ 别按 `sizeDelta` 量（本仓踩过：拉伸锚点下 `sizeDelta` 是内缩量）。

---

## 二、建了什么

| 文件 | 动作 | 说明 |
|---|---|---|
| `CardPresentation/Battle/AttackSelector.cs` | **改**（+86 行，全部**追加在类尾** `#endif` 之后；另有两处**同行**改动） | ① 新增 `RingSpinDegPerSec = 100f` · `_ringSpinDeg[3]` · `AdvanceRingRotation(dt)` · `RingSpinDegForTest(i)` · `RingNodeRotationZForTest(k)`；② 在 `Tick(dt)` / `TickForTest(dt, …)` 里把 `TickHover(dt);` 那一行改成 `TickHover(dt); AdvanceRingRotation(dt);` ⇒ **行数零变化**（`git` 侧行数 1960 → 2046，差额 = 新增块） |
| `CardPresentation/Editor/B97_HUDROT_Assert.cs` | **新建**（236 行） | **25 条断言** = A 组 7 条（几何 `176.23 × 176.875` × 3 格 + `A1b/A3b` 的高 + 2 条前提）· B 组 11 条（数学 `B1/B2` + 前提 `B0/B0b` + 起始角 `B3b` + 转速 `B3/B4` + 一致性 `B5` + 方向 `B6` + 「关着不转」`B7/B7b`）· C 组 7 条（显隐 `C1/C2/C2b/C3` + 冻住续上 `C3b` + 收起 `C4/C4b`） |
| `账/_parts/B97_HUDROT.tsv` | **新建** | 1 行更新（`:584` 的 `差异` / `轮次` / `备注`）。`--dry --only B97_HUDROT` 实跑：**更新 1 · 新增 0 · 无变化 0**；`check_patch_conflicts.py` = **无跨补丁重复键（4 份补丁）** |

### 2·1 语义上的三个决定（都写进了代码注释）

1. **写 `localRotation`**，不是 `rotation`。原版写 `transform.eulerAngles`（世界欧拉角），但三颗圈的父链全是恒等旋转 +
   `localScale 1` ⇒ 两种写法得出的数相同；用局部那份才不会被父级旋转带偏（世界档 `ImageQuad` / uGUI 档 `RectTransform` 都没有别的旋转要保住）。
2. **只推进 `activeSelf == true` 的那一颗**（`AdvanceRingRotation` 里那句 `continue`）。原版未选中的那颗 GameObject 是关的
   ⇒ Unity 不给它跑 `Update` ⇒ **角度冻住**。逐字照这条，所以「切选」那一刻新亮起的那颗从它上次冻住的角度续上。
3. **角度累加器每颗一个**（`float[3]`），⛔ 不是三颗共用一个 —— 共用会让关着那颗也跟着走（原版不走），
   而这条差异**只能在自检里看出来**（B7/B7b/C3b 就是为它写的）。

---

## 三、证据（`文件:行` + 资产路径）

**判据（原版侧）**
- `d:/2/tools/decomp_full/SimpleRotation__Update.c`（全文）：`euler = transform.eulerAngles` → 逐轴 `euler[i] += speed[i] * Time.deltaTime` → `set_eulerAngles`。
- `d:/2/tools/decomp_full/CardDisplayAttackTypeButton__HighlightIfNeeded.c` 第一句 `SetActive(highlightImage(+0x38), param_2 == attackType(+0x20))`（显隐条件）。
- `d:/2/tools/decomp_full/EverguildButtonMaterialModifier__GetModifiedMaterial.c` + `…__get_ToogleGreyScale.c`：只有 `+0x20` 为真时才改材质 ⇒ 本处（字段全默认）**无视觉作用**。
- 资产：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena<…>/GameObject/Highlight{,_<pid>}.json` ·
  `…/RectTransform/RectTransform_{3028,2776,3512,3221,2969,3707,…}.json` · `…/MonoBehaviour/MonoBehaviour_{4305,4481,4736,…}.json` ·
  `bundle_Waprforge_monoscripts/MonoScript/MonoScript_-6857224376968460714.json`（类名）·
  `bundle_Waprforge_monoscripts/MonoScript/MonoScript_350208831926335389.json`（= `UnityEngine.UI.Image`）·
  `MonoScript_-9096086197743689236.json`（= `EverguildButtonMaterialModifier`）。
- 贴图：`d:/2/解包整理/03_界面UI/图集/battleatlasui/Sprite/Attack type button highlight_-7315808829623560242.json`。

**我们这一侧（改动落点）**
- `CardPresentation/Battle/AttackSelector.cs:2007`（`RingSpinDegPerSec = 100f`）· `:2011`（`_ringSpinDeg[3]`）·
  `:2014`（`RingSpinDegForTest`）· `:2021`（`RingNodeRotationZForTest`）· `:2036`（`AdvanceRingRotation`，`:2041` 那一句是 `SimpleRotation.Step`）·
  `:429` / `:439`（`Tick` / `TickForTest` 的调用点，**同行追加**）。
- 已有的几何 / 显隐（本批**没改**，只复用）：`AttackSelector.cs:143`（`RingW/RingH = 176.23 / 176.875`）·
  `:897` / `:980`（建圈，贴图 `Attack_type_button_highlight`）· `:1528`（`SetSlotActive` 按 `IsPickedSlot` 开关）· `:1710`（`RefreshPicked`）。
- 数学那一半（本批**没改**）：`CardPresentation/Shell/LoadingMenu.cs:409`（`SimpleRotation`）· `:412`（`Update` 逐轴）· `:419`（`Step` + `Repeat 360`）。

---

## 四、`typecheck`

```
TMPDIR=/tmp/wf_hudrot bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

⚠️ **中途两次不是我的红**（如实记，防下一个人误判）：
① 第一次 `SkirmishEventWindow.cs(519,13) CS0103 WireScoreBar`；
② 第二次同一文件 `CS0234 MilestoneLocalSimulation`（错误内容在两次之间**变了**）——
那是 `B97_MINI2` 名下那个文件**正在写**（`WireScoreBar`/`_scoreLines`/`MilestoneLocalSimulation` 都是它那一批的新成员）。
等它落地后重跑 = **0 / 0**。按铁律 13·3，这类「全集中在一个不属于自己的文件上」的错不算自己的。

---

## 五、账补丁

`账/_parts/B97_HUDROT.tsv`（11 列 + 首行表头，`方法` = `Update()`）：
只动 `:584` 的 `差异`（追加本批落地与残余）· `轮次`（追加 `B97_HUDROT`）· `备注`（追加断言清单与落点）。
**状态仍留 `B`**，理由写在 `差异` 列：竞技场 39 个实例已复刻并带断言，但**同一行的 42 个实例里还差 2 个 `Cog`**（见下节），
那 2 个不在战斗 HUD 线上 ⇒ 整行还没到 A。

---

## 六、待接线（本代理**没有**改宿主）

**断言入口**：`CardPresentation/Editor/BattleScene.cs:10073`（那一行是
`                      "★ 这个单位**还没打过** ⇒ 各格都不放大（原版那圈挂的是 `unit.attackType`，0 = 不亮）");`），
**在同一行末尾追加**（⇒ 行数零变化，不让账落点漂移）：

```csharp
B97HUDROTAssert.Run(); pass += B97HUDROTAssert.Pass; fail += B97HUDROTAssert.Fail;
```

⚠️ `BattleScene.Run` 用的是**局部** `pass` / `fail`（`:633`）＋ 局部 `Check`（`:637`），**没有 `_sink`** ——
别照抄 `_sink.Pass += …`（那在 `BattleScene` 里编译不过；`_sink` 是 `ShellScene` 那一族的写法）。
本文件**自带夹具**（自己 `new GameObject` + `AddComponent<AttackSelector>()` + `Show(...)`），
临时物体在 `finally` 里 `DestroyImmediate` —— 因为 `BattleScene.Run` 收尾会 `SaveScene()`，⛔ 不能留进 `Battle.unity`。

**另外两条不是我的、但要记账的东西**：
1. `Shell/SettingsWindow.cs:3134` 建了那颗 `Cog`（`40K_icon_searching_cog`，`Animated Loading Image` 的子件），
   但**全文没有一处旋转驱动**（`grep localRotation|Rotate` = 0 命中）⇒ 原版那 2 个 `Cog` 里的这一个还是静态的（菜单/外壳线）。
2. `ChooseNameWindow` 的 `Cog` —— 本仓**没建那个窗**（`grep ChooseName` 只命中 `Loc.cs` / `NetProtocol.cs` / 两个 Editor 宿主，没有窗实现）。

两条都已写进账 `:584` 的 `差异` 列（铁律 5·b）。

---

## 七、没查清 / 没法查清的

1. **`menus_assets_all` 那 2 颗 `Cog` 的父链名**：父 `RectTransform` 的 pathID（`8608300487762018214` / `-3672936344398346131`）
   是大整数、落在别的内层 CAB ⇒ 按 `m_FileID` 跨 CAB 才能解出 prefab 名。本批只需要战斗 HUD 那 39 个，**没有继续挖**。
   （`mainmenuwarpforge` 那颗已确证是加载齿轮，`Shell/LoadingMenu.cs:21-23` 的老注释里已记死。）
2. **`EverguildButtonMaterialModifier` 在别的宿主上有没有非默认值**：本处 3 份（`MB_4693` / `MB_5155` / …）
   序列化里**一个字段值都没有** ⇒ 默认 `false` ⇒ 无作用。但「全库有没有哪个实例把它设成 `true`」**没有统计**
   （不属本件；若以后要复刻按钮的灰化，得按那一族单独盘）。
3. **`m_IsActive = true` 与「运行时被 `HighlightIfNeeded` 关掉」之间的时序**：本批沿用本仓既有实现
   （`SetSlotActive` 在 `Show`/`RefreshPicked` 里开关），**没有**去追原版「场景加载后第一帧谁先 SetActive」——
   观察不到差别（选择器本身就是被 `Toggle(true)` 弹出来的）。
