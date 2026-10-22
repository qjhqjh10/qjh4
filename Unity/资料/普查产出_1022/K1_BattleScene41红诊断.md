# K1 · `BattleScene.Run` 41 红诊断（只读，2026-10-22）

> 现场：`_tmp_view/w65e_battle.log`（**4144 通过 / 41 失败**）；上一版绿 `_tmp_view/w65_battle2.log`（**4285 / 1**）。
> 期间改动：**H-A** 换牌期 8 条 `BattleHud*` 动画接线（HUD 件搬进 9 个 rig）· **H-B** 残骸体动画/粒子接线。
> **结论：41 条里真·实现缺陷 0 条**（α 40 · γ 1 既存）。

## 结论表

| # | 类 | 条数 | 一句话成因 | 判据（文件:行号） | 建议改法 |
|---|---|---|---|---|---|
| 1 | α | 16 | H-A 把 16 颗 HUD 字搬进 `BattleHudLayer/<rig>/`，`FindHudNode` 只找 RootRect 直子或 `BattleHudLayer/<name>` 直子，**不递归第三级** ⇒ `lb/tmp` 都为 null；字本身在、仍 TMP | `BattleScene.cs:638-644`（`FindHudNode`）· `:685-692`（`CheckHudBox`）· 调用 `:20404-20480` · 搬家 `BattleDriver.cs:24018-24050` | 给 `FindHudNode` 加第三档：在 9 颗 rig 里按名递归找；⛔ 别写 `Find("BattleHudLayer/LeftArea/PlayerInfo/…")`（rig 名里带 `/`） |
| 2 | α | 8 | 同上；`HudPxOf` 找不到 `My/FoeDeckSizePlate` / `My/FoeDeckPlate` / `Player/EnemyPlate` ⇒ 返回 (−1,−1) | `BattleScene.cs:619-622` · 调用 `:2995-3040` / `:4438-4439` | 同 #1 |
| 3 | α | 4 | `OffensiveButton` / `CenterCameraButton` 进了 `LeftArea/Left Anchor` rig ⇒ null ⇒ 0×0 / NaN | `BattleScene.cs:19909-19910` · 搬家 `BattleDriver.cs:24047` | 同 #1 |
| 4 | α | 12 | `AvatarItemSmall_Me/Foe` · `AvatarImage_Me/Foe` · `ChatButton` 进了 PlayerInfo/EnemyInfo rig ⇒ `Rt()` null ⇒「节点不在」/ 尺寸 −1 / z NaN | `BattleScene.cs:21403-21425`（`Rt()`）· 搬家 `BattleDriver.cs:24018-24024` | 同 #1 |
| 5 | γ（既存，非本批） | 1 | 手牌数框高 γ，数值与基线**逐位相同**（164.7544×75.8061） | `BattleScene.cs:27693`；日志 L:85469 = G:86545 | 维持已知项 |

## 三件事的答案

**(a) `HudLabelOnCanvasForTest`**（`BattleDriver.cs:23457-23472`）改成从 `transform.parent` **最多上溯 4 跳**；判据仍是「祖先是 `HudCanvas.RootRect` 本体」。实际链 = `字 → rig → _hudLayer → RootRect`（`_hudLayer` 在 `BattleDriver.cs:20763-20766` SetParent 到 cvRoot），3 跳就中；不会走到另一台相机的画布。日志 `L:8941-9435` **19/19 全绿**。

**(b) 「走 TMP 后端」**不是直接读 `Label._tmp/_mr`，而是 `GetComponent<Label>() != null && GetComponentInChildren<TMP_Text>(true) != null`（`BattleScene.cs:686-687`）。搬家路径是 `SetParent(rig,false)` + `HudCanvas.Add`（`BattleHudAnim.cs:488/498`；`HudCanvas.cs:455-459` 只重排兄弟序），**不销毁/重建组件** ⇒ 后端不可能换。反证：`EndTurnButton` / `EnergyLabel` 的 TMP 字体资产断言照样绿（`L:9487/9500`）。

**(c) 少的 ~100 条不是异常**：16 个 `CheckHudBox` 早退（`BattleScene.cs:692`）× 9 条 = **144 条没跑**；本批新增 **43 条**（H-A §16c 25 条 `L:65572-66208` 全绿；H-B 18 条 `L:31665-31898` 全绿）。4286 − 144 + 43 = **4185**（= 4144 + 41），逐位吻合。日志无异常、无整段提前结束。

> 代理类型：`Explore`（只读，不能写文件）⇒ 本文件由主对话按其 hand-back 原文落盘（铁律 5·b）。
