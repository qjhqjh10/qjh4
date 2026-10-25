# 判据 · HUD 自绘光标（448/449/450）与抽牌终点锚（194）

> 2026-10-25 · 执行代理 D · 结论先行：**光标那条原版是死代码（P0 应撤销）**；**抽牌锚的「y 翻转」假设已被两个反例推翻**，A 当时不猜参数是对的。

---

## 一、`CursorManager`（账 `战斗界面_HUD.tsv` 的 448 / 449 / 450，原判 C/P0）

### 结论：**原版出货里它不运行 ⇒ 我们一直用系统光标是与原版一致的。建议改判 `D`（原版本身没有 / 判据是空的），撤掉 P0。**

### 证据（三条，互相独立）

1. **唯一实例的 GameObject 是关着的，且两张贴图都是空**（`D:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/Intro.unity`）：
   ```yaml
   --- !u!1 &118                     # GameObject "CursorManager"
     m_IsActive: 0                   # ← 关着（Start/Update/OnGUI 一个都不会跑）
   --- !u!114 &420                   # 组件
     m_Enabled: 1
     cursorTextureNormal: {fileID: 0}   # ← null
     cursorTextureClicked: {fileID: 0}  # ← null
   ```
   ⇒ 即使它跑起来，`GUI.DrawTexture(rect, null)` 也**什么都不画**，而 `Cursor.visible = false` 会让**光标彻底消失**。

2. **全库 grep `cursorTextureNormal` = 0 命中**（`D:/2/新解包资源/assets_full`，84 个 bundle / 24.7 万文件，`rg`）⇒ 出货包里**根本没有这份组件数据**（连 Intro 那份都没有）。

3. **系统光标在原版 PC 上是可见的**：全量反编译里只有 4 处动 `Cursor.visible` ——
   - `CursorManager.Start`（= 上面那个死组件）
   - **`PlayerDataManager.Awake`**：`if (SteamUtils.IsSteamRunningOnSteamDeck() && GameStaticData.touchInput) Cursor.visible = false;`
     ⇒ **只有 Steam Deck + 触屏输入**才藏（`PlayerDataManager.cs:7812-7815`）
   - `TargetReticleController.ToggleCrosshair` / `GeneralTab.TouchInputToggle`：由 UI 开关驱动（瞄准/触屏），**不是常驻隐藏**

### 方法体（已读全，留着备查 —— 万一今后要「真做」这个功能）

`d:/2/tools/decomp_full/CursorManager__{Start,Update,OnGUI}.c`（ILSpy 版里那段 `imul` 被吞了，`.c` 是完整的）：

```
Start:  Cursor.visible = false
        cursorSize      = Screen.width / 21      // 0x15
        cursorXposition = Screen.width / 70      // 0x46
        cursorYposition = Screen.height / 40     // 0x28
        currentNormal = cursorTextureNormal ; currentClicked = cursorTextureClicked   // 拷贝引用
Update: mouse = (Input.mousePosition.x, Screen.height - Input.mousePosition.y)        // GUI 是左上原点
        mouseClicked = Input.GetMouseButton(0)
OnGUI:  tex = mouseClicked ? currentClicked : currentNormal
        GUI.DrawTexture(new Rect(mouse.x - cursorXposition, mouse.y - cursorYposition,
                                 cursorSize, cursorSize), tex)
```
⇒ 热点 = `(宽/70, 高/40)`；绘制尺寸 = `宽/21`（正方形）。

### 建议
- 账：448/449/450 由 **C/P0 → D**（断点④：原版唯一实例 `m_IsActive: 0` + 两张贴图为 null + `assets_full` 0 命中）。
- ⛔ **不要新建 `Battle/CursorManager.cs`** —— 那会**凭我们自己的设计**加一条原版没有的行为（铁律 3：不许把猜测写成「原版的做法」；铁律 11 例外①）。
- 若将来确实想要软件光标（例如为了截图/录屏里能看到指针），那是**我们的选择**，应做成 **默认关闭的开关**并在注释里写清「原版没有」。

---

## 二、抽牌终点锚（账 194 `BattleHud.GetCardDrawPosition`）

### 结论：**「树里的 y 一律 = H − y_ours」这个假设不成立** —— 两个反例推翻；A 当时不猜参数是对的。

### 事实
- 13 份战场树都有 `CardDrawPositions/{PlayerDrawnPos, EnemyDrawnPos}`；`2D层_battlearena1全树.md:142-144`：
  `CardDrawPositions [-0,1030 100x100]` · `PlayerDrawnPos [1350,1030 100x100]` · `EnemyDrawnPos [1350,1030 100x100]`（两者同值）。
- 我们 `HudAbs(root, art, x, y, w, h, name, z)`（`BattleDriver.cs:28179`）用的是**同一个绝对原点**的一套数。

### 交叉验算（拿树里另两个节点反算）
| 节点 | 树 `[x,y wxh]` | 我们 `HudAbs(x,y,w,h)` | y_tree + y_ours |
|---|---|---|---|
| `CenterCameraButton` | `[18,1108 64x62]` | `17.929, 568.174, 64.443, 61.846` | **1676.17** |
| `OffensiveButton` | `[0,987 109x107]` | `0, 446.857, 109.008, 106.943` | **1433.86** |

⇒ 两个节点的「和」**不同**（1676 vs 1434）⇒ 树里的 y **不是**「相对同一屏幕高度的翻转」，而是**各自父节点的 `m_AnchoredPosition`**（不同父/不同 anchor ⇒ 换算是**逐节点**的，需要 anchor/pivot + 父链）。

### 差什么（要核出 194 必须先补的判据）
1. `CardDrawPositions` / `PlayerDrawnPos` / `EnemyDrawnPos` 各自的 **`m_AnchorMin/Max` · `m_Pivot` · 父链的 rect 与 anchor**（树 dump 只给了 `[x,y wxh]`，没有 anchor/pivot）；
2. 或者直接从**场景 JSON 原文件**（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/RectTransform/*.json`）读这三个节点的 `m_AnchoredPosition / m_AnchorMin / m_AnchorMax / m_Pivot`，再按 Unity 的 rect 公式换算到我们 `HudAbs` 的原点。

⚠️ 在那之前**别把 `(1350, 646.2)` 之类的数写进代码** —— 那是拿上面那个已被推翻的假设算出来的。

---

## 二·订正（2026-10-25 下半场：**换算方法已核出**，抽牌锚的数已给出）

> 上面那节的「差什么」已补齐。**方法**已落成脚本 **`Unity/工具/scene_rect_to_hud.py`**（只读，可复跑）。

### 2·1 换算公式（从场景 `RectTransform` 到 `HudAbs`）

数据源：`D:/2/新解包资源/assets_full/bundle_scenes_scenes_<场>/RectTransform/*.json`（有 `m_AnchorMin/Max` / `m_Pivot` / `m_AnchoredPosition` / `m_SizeDelta` / `m_Father`，**树 dump 里没有这些**）。

1. **画布局部矩形**（原点 = 画布**左下角**、y 向上；画布 = **1920×1080**），从根往下逐级：
   ```
   size    = sizeDelta + (anchorMax − anchorMin) * parentSize
   rectMin = parentRectMin + anchorMin*parentSize + anchoredPosition − pivot*size
   ```
2. **换成 `HudAbs` 口径**：
   ```
   x = rectMin.x
   y = 540 − (rectMin.y + size.y)        // 540 = 画布半高；等价于「从画布中线往下量到矩形顶边」
   w / h = size
   ```
   ⚠️ **右锚件**（本来就在画布右外侧的那些，如 `OvertimeIndicator`）我们代码里写的是 `1920 + rectMin.x`（从右边量的）———— 这是**我们自己的不一致**，不是公式的分支。

### 2·2 验证（三个节点，公式 vs 我们 `HudAbs` 里写死的数）

| 节点 | 公式算出来 | 代码里写死的 | 结果 |
|---|---|---|---|
| `CenterCameraButton` | `17.929, 568.174, 64.443, 61.846` | `17.929, 568.174, 64.443, 61.846` | **完全相等** |
| `OffensiveButton` | `0.000, 446.857, 109.008, 106.943` | `0, 446.857, 109.008, 106.943` | **完全相等** |
| `OvertimeIndicator` | `−201.118, 341.504, 68.625, 70.992` | `1718.882, 341.504, 68.625, 70.991` | y/w/h 相等；x = `1920 + (−201.118)` ⇒ **同一件东西，我们是从右量的** |

⇒ 公式 **三位小数命中**，可以直接拿来算别的锚点。

### 2·3 🔴 抽牌终点锚（账 194）的答案

`battlearena1` 实算（`python -I -X utf8 Unity/工具/scene_rect_to_hud.py battlearena1 CardDrawPositions PlayerDrawnPos EnemyDrawnPos`）：

| 节点 | rectMin | size | **`HudAbs` 口径 (x, y, w, h)** |
|---|---|---|---|
| `CardDrawPositions` | `(−0.000, −50.000)` | `100×100` | **`(0, 490, 100, 100)`** |
| `PlayerDrawnPos` | `(1350.000, −50.000)` | `100×100` | **`(1350, 490, 100, 100)`** |
| `EnemyDrawnPos` | `(1350.000, −50.000)` | `100×100` | **`(1350, 490, 100, 100)`** |

⚠️ **两个抽牌锚同值**（树 dump 里也都是 `[1350,1030 100x100]`）—— 这不是抄错：原版这两个 Anchor 确实是同一个点
（抽到牌先落到同一点，再进各自的手牌布局）。**接的时候别自作聪明给对手挪一格。**

📌 **13 场全部实算过（2026-10-25）—— 数值完全一致**：
每场 `PlayerDrawnPos` 与 `EnemyDrawnPos` 都是 **`(1350, 490, 100, 100)`**，`CardDrawPositions` 都是 **`(0, 490, 100, 100)`**。
（复跑：`python -I -X utf8 Unity/工具/scene_rect_to_hud.py <场名> PlayerDrawnPos EnemyDrawnPos`；13 个场名 = `battlearena1..3` + `battlearena{aeldari,astramilitarum,blacklegion,darkangels,emperorschildren,genestealers,leviathan,sororitas,spacewolves,tauviorla}`。）
⇒ A 接的时候**一份常量就够**（不必按场分支），但⛔ 别把它当成「本来就是常量」的猜测 —— 这是逐场算出来的。
