# W4 — `DeckInfoPopup` 第三轮真机验收：底部按钮右对齐 + `Edit Deck` 置灰改判（执行代理 · 2026-10-18）

> 只改了 **`Shell/DeckInfoPopup.cs`**（主体）＋ **`Editor/CollectionScene.cs`**（两条必须同步的断言，最小改动）。
> 没跑 Unity、没动 git、没改正本。typecheck 见 §四。
> 判据 = 用户 2026-10-18 真机截图（`_tmp_view/shots/2026-10-18_卡组选择弹窗_第三轮/98_按钮区对比.png` ·
> `99_side_by_side.png`）＋ `d:/2/tools/decomp_full/` 反编译 ＋ 原版 prefab 实读 ＋ 用户桌面的原版参照图
> `C:\Users\qjh36\Desktop\warpforge参考图\选择卡组出现的界面参考.png`。

---

## 一、按钮整体偏左 —— 根因与修正

### 1·1 根因（**不是**父件宽度/锚点/`padding.right`）

我们这一层**根本没有布局组**：三颗钮是**按固定下标**用绝对 px 硬摆的（`DeckInfoPopup.cs` 第 6) 节）：

```
x1 = BtnL + i * (BtnW + BtnGap)      // 725.2 / 1085.7 / 1446.2，**不管显隐**
```

**原版**那三颗在一个 `HorizontalLayoutGroup` 里（`spacing 36` · `m_ChildAlignment = MiddleRight(5)` ·
容器 `669.3,885.8 → 1770.7,965.9` 宽 **1101.4**），而 **uGUI 的布局组只排 `active` 子件**
（`LayoutGroup` 收集子件时把 inactive 的排除）⇒ 藏掉一颗**不是留空位，而是剩下的整体右移**：

| 可见颗数 | 内容宽 = n×324.5 + (n−1)×36 | 起点 = 669.3 + (1101.4 − 内容宽) | 行区间（设计 px） |
|---|---|---|---|
| 3（`Select Deck` 也露） | 1045.5 | 669.3 + 55.9 = **725.2** | 725.2 … 1770.7（= 我们改前那三格，逐位相同） |
| **2（真机这一档）** | **685** | 669.3 + 416.4 = **1085.7** | **1085.7 … 1770.7** |
| 1 | 324.5 | 669.3 + 777.6 = 1446.2 | 1446.2 … 1770.7 |

真机那扇是 `state = Edit(0)` + `context.SelectButton == null`（`Select Deck` 藏）⇒ 我们只排了**前两格**，
右缘停在 `725.2 + 2×324.5 + 36 = 1410.2` —— 正好是截图里的 **0.73 屏宽**（1410/1920 = 0.734），
比原版左 **360.5 px**。原版两颗则是 `1085.7…1770.7`（`练习` 起 0.565 屏宽、`编辑卡组` 右缘 0.922），
与用户给的 0.57 / 0.92 **同档**。

> 所以错误不是「右对齐到了卡列表」，而是「**按三格硬摆、藏了第三颗也不重排**」——正好把可见的两颗停在中间。
> 父件是 `Buttons` 容器（纯 `GameObject`，无布局语义），它既不是卡列表也不参与计算。

### 1·2 修法

新增 `LayoutButtonRow()`（`public`，`ApplyStateVisibility()` 末尾调一次，建窗末尾那一次就生效）：

* 可见判据**逐字照抄 `ApplyStateVisibility`**（`Practice` = `isPlayerDeck && state∈{0,2}` ·
  `Edit` = `state<2` · `Select` = `SelectButtonProvided`）；
* 按 `n` 颗**右对齐到 1770.70** 重算每颗的 `x1`，与它「原三格」里的基线相减得到位移；
* 位移按**设计 px** 算，写进 `localPosition` 前过 `LayoutSpace.Px()`（1px = 1/108 世界单位）；
* 一颗钮的**底 / 字 / 命中区**是三个兄弟节点、且都以这颗钮为中心建 ⇒ 同一个位移**一起平移**；
* 藏起来的那几颗**复位到基线**（否则下次露出来会停在别人头上）；
* **每次都从基线重算**（不做增量）⇒ `ApplyStateVisibility` 翻转几次都不会漂。

### 1·3 修正后实际值（设计 px）

按新代码的算式：`state 0`（真机那一档，两颗可见）⇒ **`Buttons` 行 = 1085.70 … 1770.70**，
右缘 **1770.70**（原版值，逐位相等）；`练习` 中心 1247.95 · `编辑卡组` 中心 1608.45。

⚠️ 我**没有跑 Unity**（红线），所以这是「新代码必定打印出来的值」而不是实测值 —— 建窗时会打一行日志，
真机验收直接对：

```
[DeckInfo] `Buttons` 行：可见 2 颗（Btn:Practice Deck / Btn:Edit Deck）⇒ 右对齐后 **1085.70 … 1770.70**
（设计 px · 原版容器右缘 1770.70；三条全露 = 725.20…1770.70，两颗 = 1085.70…1770.70 …）
```

---

## 二、`Edit Deck` 是灰的 —— 反编译判据与最终行为

### 2·1 反编译逐句（`d:/2/tools/decomp_full/`）

`DeckInfoControls__Initialize.c`：

| 行 | 原文（逐句） | 结论 |
|---|---|---|
| `:74-77` | `lVar6 = get_gameObject(param_1 + 0x20); SetActive(lVar6, *(int *)(param_3 + 0x18) < 2, 0);` | `Edit Deck`（+0x20）**只在 `state < 2` 时露** |
| `:78-81` | `if (*(longlong *)(param_1 + 0x60) != 0 && *(longlong *)(lVar5 + 0x10) != 0) { cVar3 = DeckInfoPopup__CanImportDeck(popup, context.Deck);` | 判据就是 `CanImportDeck`，**没有 state 守卫** |
| `:82-83` | `EverguildButton__SoftDisable(*(longlong *)(param_1 + 0x20), cVar3 == '\0', 0);` | `CanImportDeck == 0` ⇒ `SoftDisable(true)`（变灰）；**不改 `interactable`** |
| `:84-98` | `state == 1 ? …DAT_1842d05e0 : …DAT_1842d04e0` → `LocalizationManager.GetTranslation(…)` | `state==1` 换另一条文案键（与本次无关） |

`DeckInfoPopup__CanImportDeck.c`：

| 行 | 原文 | 结论 |
|---|---|---|
| `:35` | `lVar4 = CardDeck__get_CustomGameModeEvent(deck);` | `LiveOpsManager.GetHandler<GameModes>().GetActiveEvent(deck.gameMode)`（`CardDeck__get_CustomGameModeEvent.c:15-21`） |
| `:36-38` | `if (lVar4 == 0) return 0;` | **没有「该模式正在跑的活动」⇒ false**（头一句） |
| `:39-52` | 取 `InventoryManager` 的拥有列表 → `event 的要求件数 + 1 <= 我有的件数` | 拥有度闸 |
| `:48-72` | `Enumerable.All(...)` → `cVar2 == 0 ⇒ return 1`，否则 `return 0` | 全过才 true |

`EditDeck` 里只有 `state == 1` 才用同一判据弹「无法导入卡组」错误（`DeckInfoPopup__EditDeck.c:21-45`）。

`EverguildButton__SoftDisable.c:8-24`：置 `softDisabled` 字段；`param==0` ⇒ `SetToStateActiveOrDisabled(1)`；
`param!=0` 且 `colorTintGreyOnDisable(0x17A)!=0` ⇒ `GetGraphicsInChildren` + `DoMaterialRefresh(list, 0)`
（材质换 `Everguild/UI/Greyscale`）。
**prefab 实读**（`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-8697463422759302744.json`）：
`colorTintGreyOnDisable = 1` · `m_Transition = 2(SpriteSwap)` · `m_Interactable = 1` ⇒ 原版的软禁用**确实会换灰材质**。

### 2·2 结论：**反编译没错，我们那句「等价物」错了**

反编译的字面是「`CustomGameModeEvent == null ⇒ false`」。我们据此写「我们恒 false ⇒ 原版也恒灰」——
**这一步是错的**，实况直接推翻：

* **用户 2026-10-18 的原版参照图**（`选择卡组出现的界面参考.png`，同一扇窗、同一副卡组）：`编辑卡组` 与 `练习`
  **同为橙金**；第三轮对比图 `98_按钮区对比.png` 上半同样是橙金。
* 唯一自洽的解释：原版**实况里 `CustomGameModeEvent != null`**（那款游戏的活动常驻，`GetActiveEvent(deck.gameMode)`
  拿得到），而拥有度那一闸对这名玩家也是过的 ⇒ `CanImportDeck == 1` ⇒ **不灰**。
* **错因**：把「**我们这个工程没有 LiveOps 系统**」当成了「**原版当时没有活动**」。`CustomGameModeEvent`
  读的是**服务端下发的活动配置**；我们离线复刻根本没有这一层。把**运行环境的差异**当成**卡组属性**，
  于是第一道闸在我们的等价物里恒假 ⇒ 整颗钮恒灰。

### 2·3 最终行为（改后）

```csharp
public static bool CanImportDeck(PlayerDeck deck)
{
    if (deck == null) return false;   // 原版 context.Deck 恒非空；我们查不到时保守 false
    return true;                      // 两道闸归位：① 本地没有 LiveOps（≠没有活动）② 全解锁 ⇒ 拥有度必过
}
```

* `Edit Deck` 的 `softDisabled` 现在恒 `false` ⇒ **橙金、可点**（= 原版实况那一档）。
* 调用点**没删**（`ApplyControlStates` 第 ④ 条仍按原版无条件调 `SetSoftDisabled`），只是实参恒为「不灰」；
  将来真接了 LiveOps / 拥有度，只改这一个方法。
* **连带如实记**：`state 1(Import)` 那条「无法导入卡组」提示在本地**不再可达**（拥有度恒过）。
  我们本来也没有 `state == 1` 的调用点；`EditDeck` 里那道闸**保留原样**（判据链没动）。
* 日志出声：`ApplyControlStates` 末尾那行现在打 `softDisabled = False（… **不灰**：2026-10-18 按原版实况改判…）`。

---

## 三、改动清单

### `Unity/MyGame/Assets/CardPresentation/Shell/DeckInfoPopup.cs`

| # | 位置 | 改了什么 |
|---|---|---|
| 1 | 字段区（`_wbTargetQ` 之后） | 新增 `readonly Dictionary<string, Vector3[]> _btnBasePos`（三颗钮组成节点的基线局部坐标）+ 一段 doc（为什么需要、原版 HLG 算式） |
| 2 | `Build()` 开头 | `_btnBasePos.Clear();`（与 `_parts`/`_wbs`/`_wbTargetQ` 同生共死） |
| 3 | 第 6) 节三颗钮的建树循环末尾 | 登记每颗钮组成节点的基线（顺序与 `_parts` 一致） |
| 4 | `ApplyStateVisibility()` 末尾 | 调 `LayoutButtonRow()`（显隐改了 ⇒ 重新右对齐） |
| 5 | 新增 `LayoutButtonRow()` / `MoveButton()` | 按可见集合右对齐到 1770.70；打印实际右缘（设计 px）；藏起来的复位到基线 |
| 6 | `CanImportDeck(...)` | 由恒 `false` 改为 `true`（`deck == null` 仍 `false`）；整段 doc 换成「逐句反编译 + 2026-10-18 实况订正 + 错因 + 两道闸归位」 |
| 7 | 文件头（`:93-103`）与 `ApplyControlStates` 第 ④ 条 | 就地订正原来那两句「我们恒 false ⇒ 这颗钮一直是灰的 / ⛔ 别为了亮着改成 true」（铁律 5，保留更正痕） |
| 8 | `ApplyControlStates` 末尾日志 | 「灰着也点得动」改成两态措辞（true ⇒ **不灰**，false ⇒ 灰着点得动） |

### `Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`（最小同步，2 处）

| # | 位置 | 改了什么 |
|---|---|---|
| 1 | `:1931-1948` | 「`Edit Deck` 的可见底图 = `Everguild/UI/Greyscale`」→ **反过来**：必须是 `Sprites/Default`（不是灰化材质）。注释同步订正「`CanImportDeck` 恒 false ⇒ 应当一直灰」那句 |
| 2 | `:2025-2030` | `Practice Deck` 中心 `887.45 → **1247.95**`（两颗可见、右对齐后的真值）；`Select Deck`（藏着）`1608.45` 保留，注释改成「复位在基线、不是布局排出来的」 |

> 同步理由：这两条断言直接钉着本轮改的两个行为（灰化 / 三格固定位）。其余断言不受影响
> （`Buttons` 三个节点仍然都建、`Opt:*` 五颗圆钮未动、队列不变量未动）。

---

## 四、没查清 / 判不准（⛔ 别当已复刻）

1. **原版当时 `CustomGameModeEvent` 到底是什么** —— 只能从「截图里不灰」反推它非空。
   LiveOps 的配置表在远端 CCD、本地没有 ⇒ **读不到那条活动数据**，本报告不假装读过。
2. **真机实测值**：新代码算出来是 `1085.70 … 1770.70`，但我**没跑 Unity**（红线）。
   必须以建窗那行日志 / 下一轮截图为准；若实测不是这个数，先看 `LayoutSpace.Px` 的量纲与 `_btnBasePos` 的记录时机。
3. **`Buttons` 容器本身的矩形**：我们那个 `holder` 仍是「内容盒」（725.2…1770.7，三条全露时才等于原版内容盒），
   而且它是**纯 `GameObject`（没有 rect 语义）**。原版容器是 `669.3…1770.7`（含左右各 27.95 的空隙）。
   子件全部绝对定位 ⇒ **画面无差**；但将来若要给它挂布局组 / 给它 rect 语义，得先按 `669.3…1770.7` 建。
4. **三条全露（`Select Deck` 也露）那一档没真机核过**：算式上它 = 改前的 725.2/1085.7/1446.2（逐位不变），
   但「选卡组弹窗点一行」那条链本轮没进游戏看过。
5. **`state 1(Import)` 的文案**仍是老账（本地无 I2 键），与本次无关，`NoteUnimplementedLayers()` 照旧每次出声。

---

## 五、typecheck 原文（改完、全部落盘之后）

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

命令：`TMPDIR=/tmp/wf_f9 bash d:/4/Unity/工具/typecheck.sh`
