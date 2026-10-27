# B9_W26ANCHOR —— enemyHand 屏幕锚点判定（报告）

判定文件：`资料/完全复刻/判据/enemyHand屏幕锚点_判定.md`

## 结论
- **`enemyHand` = `UpperAnchor/EnemyArea/HandArea`（GO 318 / `Transform_1295`，`isPlayer=0`）的屏幕锚点 = `(960, 50) px` @1080p**（x 从左、y 从上、向下为正）。
  - 父链：`Canvas(2759) → BackCanvas(2684) → Safe area BackCanvas(3498) → UpperAnchor(2762) → EnemyArea(2798) → HandArea(1295, localPos 0)`。
  - `EnemyArea` pivot 距顶 50 = `UpperAnchor` 中心（顶下 50）；`HandArea` localPos 0 ⇒ 同点。
- Canvas = ScreenSpaceCamera / plane 100 / CanvasScaler 1920×1080 按宽匹配 ⇒ **1080p 下 1 画布单位 = 1 px**；px↔世界单位 = **14.8364**（=14.835）。
- **reveal 落点**（原版 `GetOffensiveCardPlayedDisplayPosition` = `enemyHand.position + (−25,−30,0)` 世界）：
  - **`RevealEndPx = (589, 495)`** —— 可撤 `🔶`（现 `BattleDriver.cs:9860` = `(960,520)`）。
  - `RevealStartPx` = 世界零，在 UI 平面外（z=0 vs 画布 plane 100）⇒ 投到屏外约 `(28440,540)`，**我们的正交布局无法表示** ⇒ **保留 `🔶`**。

## 证据
- 场景 JSON：`d:/2/解包整理/07_场景/battlearena1/`（RectTransform 2762/2798/3498/2684/2759、Transform 1295/1257、GameObject 317/318、MonoBehaviour 4350 `isPlayer=0`）。
- Canvas：`Canvas_2572.json`（renderMode=1、plane 100、cam 1462）+ `MonoBehaviour_4731.json`（scaler 1920×1080、match=0）；相机 `Camera_1462.json` fov40。
- 反编译：`decomp_full/CardScript__GetOffensiveCardPlayedDisplayPosition.c`（`+0xd8`=enemyHand、`+0x60`=−25、`+0x5c`=30、`y = pos.y − up.y×30`）、`BattleManager__DisplayRevealedCard.c`（parent=enemyHand、起点 Vector3.zero）。
- 我们现状：`Hand/HandLayout.cs:154` `EnemyBaselineY = 1−0.32/1080`（= **HandAnchor 0.32px**，非 reveal 用的 HandArea 50px）。

## 该怎么用
- 把 `RevealEndPx` 改成 `(589,495)` 并删 `🔶`；`RevealStartPx` 留 `🔶`，注释改成「世界零不在 UI 平面、正交布局不可表示，起手 0.01 缩放观感无关」。

## 没查清 / 残余
- reveal 卡由 UI Camera 画是**逻辑反证**（enemyHand 在 UI 空间，BoardCamera 会把它投到屏外极左）；`CreateCard` 的 layer 实参未逐字节解出（不影响数值）。
- `RevealStartPx` 属**结构性不可表示**（非「没查到」）。
- 仅 1080p 基；非 16:9 下画布单位≠px（`A2098` 另一件事）。

## 顺手发现（未改白名单外文件）
1. `C4:112` 终点 y 符号疑似写反（+30 应为 −30；照 +30 会算到屏外顶部）。
2. `C4 §2·2` / `D6 §五·1` / `W_卡牌飞行族普查.md:118-119` 的「enemyHand 落点未解」现已补齐，可改为「已解 → 本判据文件」。
3. `BattleDriver.cs:9805-9807/9860` 头注「三处都解不出」对 EndPx 已过期、对 StartPx 仍成立，订正时别一刀切。
4. `runtime_ui_dump` 表头列名错位（rect/pos/pivot 实为 anchoredPosition/sizeDelta/anchorMin/anchorMax/pivot，判据 `SceneJumpShot.cs:41-49`）。

## 文件所有权
只写本报告 + 判据文件；未动任何 `.cs`/`.tsv`/正本/资产，未跑 Unity，未动 git，未碰 `d:/2`（只读）。
