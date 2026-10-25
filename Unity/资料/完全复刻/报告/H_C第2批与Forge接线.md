# H · 菜单 C 第 2 批 + Forge 组件接线（2026-10-25）

> 代理 H。本轮三件事：① `check_ledger_points.py` 两账清零；② G 审计「建议列」多符号行复核；③ 菜单 C 第二批 + F 组件在 Forge 的两处接线。
> **收尾类型检查：运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_h bash d:/4/Unity/工具/typecheck.sh`）。

---

## §一 账本落点机检：**329 / 329 全过、0 漂移 0 失败**

```
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py 资料/完全复刻/账/锻造.tsv 资料/完全复刻/账/菜单_外壳.tsv
锻造.tsv      A=54   通过=54   漂移=0  失败=0
菜单_外壳.tsv  A=275  通过=275  漂移=0  失败=0
```

- **锻造 24 条失败**：逐条改指真实声明（`ForgeTab` 的 `BuildCell 869 / Refresh 764 / BuildArmyItems 683 / SelectArmy 751 / BuildRewardCells 790 / FocusClaimable 842 / Setup 278 / OnOpen 282 / BindBlink 343`、`GameWindow 714/663`、`ForgeData 118/148/162/87/190`、`BadgeNotifications 431`、`ItemDrawer 840/1480`）。其中 `ForgeHandler.GetMaxLevelForge` 的实现是 `const MaxLevel`（字段行无括号、机检 A 口径不认）⇒ 按新口径**退 B** 并注明。
- **菜单 20 条失败/漂移**：`WindowButton 795`、`DuelPopupWindow 231`、`SearchingMatchPopup 343`、`CardChoicePanel 1003/1054`、`SettingsWindow 2728`、`LoadingMenu 164`、`TopBar 748`、`CollectionWindow 2318`、`ProfileData 263`；并修正 7 条本轮新写 A 的行号。
- ⚠️ **机检坑（已绕开）**：括号里写**单个标识符**时，checker 会去文件里找**第一处通过“像声明”判定的同名词**——而 `SetSoftDisabled` 那种**一行成员里调用 `RefreshGray()`** 也会被判成声明 ⇒ 误报漂移。本轮把这类落点的括号改成**散文**（`（RefreshGray 实现）`），只按行号判声明，结果全绿。

---

## §二 G 建议列复核：**41 条多符号行、改了 6 条**

按调度台口径，只复核「原落点文字里出现多个符号（方法/属性/两个以上 `Xxx`）」且走 G 建议列的那批。

| 结果 | 数量 | 例子 |
|---|---:|---|
| 多符号 A 行 | **41** | `EverguildButton.Awake`（`Collect|ApplyTintInstant`）· `GameWindowWithTabs.Close`（`Close|CurrentTabView`）· `WindowTabBase.*` … |
| 改指真实实现（仍是 A） | **3** | `DuelPopupWindow.OnClassic/OnSkirmishButtonPressed`：G 建议 `DuelButton:231`（**建钮**）→ 改指 **`OnMode:258`**（真正的动作）· `GenericOptionsPanel.SetButtons`：`DefContext:209`（**默认 Context**）→ **`RebuildButtons:456`**（真建钮） |
| 退 B（无单一实现入口） | **3** | `WindowTabBase.Awake`（建议行是 `TryOpenTab`，原版 Awake 空）· `ToggleDisplay.Setup`（拆成 `SetSelected + SetNormalTex + Label` 三跳）· `UrlButton.Initialize`（只存 URL；建议行是 OnClick 的 `AccountOpenUrl`） |
| 复核后**确认是对的** | **35** | `Shade.SwitchShade→FadeTo` · `EverguildButtonHelper.GetGraphicsInChildren→Collect` · `TabButtons.*` · `WindowTabBase.CloseTab/OnClose/TryOpenTab` · `WildcardDrawer.Draw→Draw` … |

> 另：三条「数据表/属性」A 原本会让机检失败，已改指**消费它的方法**：`AlternateArtPanel.Start → CollectionWindow.BuildStylesPage:2318` · `ResourcesBarController.GetCounter/Initialize → TopBar.BuildResourcesBar:748` · `TitleTab.SetTitle → ProfileData.Titles:263`。⛔ 没有留下“指到字段还要算 A”的假精确。

---

## §三 菜单 C 第二批（C 120 → 69）

| 类 | 处理 | 证据 |
|---|---|---|
| `UICulling`（5 条） | **D（菜单域）** | 全库仅 **6 个实例**（全在 `bundle_menus_assets_all`），**6/6 的 `m_OnBecameVisible` → `ExpansionPassRewardItemContainer.DelayedInit`** ⇒ 只被 **Expansion Pass 活动线**用；活动线暂停，只记账 |
| `NestedShadowGroupBase/Group/SpriteRenderer/TextMeshPro/Retarget`（25 条） | **D** | 全解包 **247k 个 MonoBehaviour 0 实例**（按 globalgamemanagers `MonoScript_2311/2582/116/2253/1412` 全扫）+ 全量反编译除自身 **0 调用点** + AssetRipper 工程只有脚本（`link.xml` 引用）⇒ 本 build 未使用 |
| `TitleTab` / `TitleDrawerHorizontal`（8 条） | **D** | 同上：**0 实例、0 外部调用点**；称号列表页没有原版可复刻的运行时入口 |
| `TMProWarpText`（6 条） | **B** | **34 实例全在 `bundle_boosterpacks_assets_all` 的 `Army Text`/`Army Text 2`**；原版 = TMP 逐字顶点沿 `AnimationCurve`（0→0.25→0，端点切线 30/-30）warp + `OnPreRenderText` 订阅。**差**：TMP 顶点级 warp 通用件未做（我们 `Label` 只到 TMP 文本层）；且宿主在卡包开启界面、属装饰 |
| `MenuActions`（7 条） | **B** | 整族**动作队列**（Queue/TryTrigger/Release/Stop/Resume/Routine + `CheckActiveMenus` + 全局 delegate + 协程）；我们目前「点击当场生效/被闸掉」。**差**：队列整族未实现（不是单方法缺口） |

**C 现状：69 条**（原 120）。余下大头：`DragAndDropItem`(10) · `WindowsManager`(9) · `EverguildButton`(8) · `MainMenuWindow`(活动线) · `NestedFadeGroup` 等。下一批建议仍按 `DragAndDropItem`（等拖拽接线）→ `EverguildButton` 事件出口。

---

## §四 F 组件的 Forge 接线（只接线，组件本体用 F 的）

### 4.1 `UITransformTweenController`（`Forge Menu Reward Button/RewardTransform`）
- **实例值**（F 实读）：`animTime 1.5 · animPosition 1 · animScale 1 · targetScale (1.12,1.12,1.12) · targetPosition (0,217.5)`。
- **触发**（`ForgeRewardItemContainer__Initialize.c:33 / :133 / :233`）：`Initialize()` 记原值；**`ToCollect` ⇒ `DoDefaultAnim()`**；**`Collected` ⇒ `BackToDefault()`**。
- **落地**（`Shell/ForgeTab.cs` 的 `BuildCell`）：`RewardTransform` 上 `AddComponent<UITransformTweenController>()` + 上述值；按 `st` 分派 `DoDefaultAnim/BackToDefault`。
- 🔴 **坐标帧适配（如实记）**：原版 `targetPosition` 是 uGUI `anchoredPosition`（设计 px），我们的 `anchoredPosition` 是**世界单位（1 单位 = 108 px）**；且原版初始 `anchoredPosition = (0,207)`、目标是 `(0,217.5)` ⇒ **正解 = 当前 `anchoredPosition` + `LayoutSpace.Px(10.5f)`**（不是直接写 217.5）。

### 4.2 `UIBorderGlow`（`Forge Menu Reward Button/Collect Highlight Effect`）
- **实例值**：`animTime 1 · animDelay 0 · playOnEnable 1`（Forge 是 89 个里**会呼吸的 3 个之一**）。
- **材质**：原版 Material `Border Highlight Appear Purple` → shader `Everguild/UI/UI Border Highlight Appear`（`Shader_6773909295900798026`），`_Reveal 0.723`、`_Noise_R = UI Dirt And Noise skratches`；颜色/浮点逐值抄进 `ForgeGlowMaterial()`（判据 = `Material_794468876670077408.json` 的 `m_SavedProperties`）。
- **几何**：RT stretch `0..1` · pivot `.5` · `anchoredPos (-1.65004,131.7)` · `sizeDelta (-82.8999,-146.2)` ⇒ 实绘 **423.0 × 573.26**；列队 **3030**（在奖励抽屉 3028/3029 之上，不与既有层同队列）。
- **显隐**：出厂 `SetActive(false)`（`ForgeRewardItemContainer__Initialize.c:77` 无条件关）；**领到之后**打开（`__ClickClaim.c:35-37`）。我们在 `ClaimCell` 里 `_claimedGlowLevel=i` → `Refresh()` → 打开新格子的节点；`BuildCell` 也按 `i == _claimedGlowLevel && claimed` 保持它（否则 `ShowCollected` 关闭时的回调 `Refresh` 会立刻把呼吸关掉）。换阵营清空。
- ⚠️ **等价近似（如实记）**：原版 Image 的 sprite PPtr（`1754528483989757341`）本地解不出独立文件 ⇒ 用白图 + **原 shader/材质参数**（可见内容来自 `_ColorMain/_ColorSecondary/_Noise_R`，不是 sprite 像素）。

### 4.3 断言（`Editor/RewardsScene.cs` 锻造节，+13 条）
- `RewardTransform` 有 `UITransformTweenController`、1.5 s、pos+scale 双开、`targetScale=1.12`、**ToCollect 格两条补间计数 ≥1**；
- `Collect Highlight Effect` 有 `UIBorderGlow`、`animTime=1/animDelay=0/playOnEnable=1`、**出厂关**；
- **点 Claim ⇒ 重建后那颗节点 `activeSelf=true`**（紫边呼吸真的播了）。

---

## §五 账的最终状态（两本）

| 账 | A | B | C | D | E |
|---|---:|---:|---:|---:|---:|
| `菜单_外壳.tsv` | 275 | 165 | 69 | 364 | 0 |
| `锻造.tsv` | 54 | 29 | 9 | 26 | 0 |

> 两份均 `check_ledger_points.py` **全过**；`菜单_外壳.tsv` 874 行、纯 LF、11/12 列；`锻造.tsv` 同上无越界。

## §六 还欠 / 下一批
1. `TMProWarpText` / `MenuActions` 两族 B（见 §三）。
2. `DragAndDropItem`(10) 等 `PointerLayer/DeckCell` 拖拽接线（卡面代理收口后）。
3. 其余 C 69 条按 `任务总表` 继续。
4. Forge 紫边/飞入的**真机视觉验收**（批处理只验了装配/字段/状态，动画本身要在 Play 里看）。
