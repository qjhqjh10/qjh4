# Dg2 · `CardBaseDemo` 4 红诊断（只读代理产出，主对话落盘）

> 代理 Dg2 为只读（Explore）⇒ 报告由主对话代存。日志：`d:/4/_tmp_view/20261025/cardbase.log`。日期 2026-10-25。

## 0. 一句话结论
**4 条同一根因、同一判类：γ（本批回归，落在断言侧）。生产实现没有缺陷 —— 文字层确实挂在 `Front` 下、`+0.08` 加得完整。是 E16 的「量具」还在用卡根（`2DCard`）坐标系去量一组本来是 `Front` 局部的期望值，整块读数统一偏 `0.08 卡单位 = 0.0240 y01`。**

## 1. 逐条表

| # | 失败行 | 现象 | 根因（`文件:行号`） | 判类 | 一句话修法 |
|---|---|---|---|---|---|
| 1 | `Chaplain Letharius／hero` ② 字墨纵向 `0.6624..0.7608` vs `0.6774..0.7872` | 上沿比下门高 **0.0150** | `Editor/CardBaseDemo.cs:2603`（`card.InverseTransformPoint`，标架 = 卡根）× `Core/CardView.cs:633/660`（`FaceDescBand01` 给的是 **`Front` 局部 y01**） | **γ** | 量具标架换成 `v.FaceContentRoot` |
| 2 | 同卡 ③ 中心 `0.7116` vs `0.7323`，差 `0.0207` | 差 = `0.0240（框架错位） − 0.0033（10-24 就有的字墨心↔行盒心残差）` | 同上 | **γ** | 同上 |
| 3 | `Master of the Fleet／tactic` ② `0.7043..0.8229` vs `0.7240..0.8512` | 上沿越门 **0.0197** | 同上 | **γ** | 同上 |
| 4 | 同卡 ③ 中心 `0.7636` vs `0.7876`，差 `0.0240` | 差 **恰好 = `0.08 / 3.3313 = 0.02401`** | 同上 | **γ** | 同上 |
| 对照 | `Varro Tigurius` ③ `0.7134`（差 0.0189，余量 0.0011）/ ② `0.6771`（余量 **0.0003**） | 仍绿但贴线 | 同上 | — | 修后回到 10-24 的 `0.7374 / 0.7011` |

**关键位置**：调用点 `CardBaseDemo.cs:2519`（传卡根）· 量具 `:2581-2613`（第 `:2603` 行）· 期望值 `:2526/:2528`（`CardView.cs:633-644`）· 坐标系判据 `CardView.cs:527-534` + `:1860-1861` + `X1_三处判据补查.md:120`。
**同文件已修过的同族**：`LayerCenterY`（`:4264-4271`）2026-10-25 已把标架改成 `FaceContentRoot`，注释原文「量在卡根空间会整体多 0.08、全部假红」；`InkBoxCardLocal` 是漏掉的那一份（全仓 2 处：`:2519` / `:2581`）。

## 2. 父链算式（卡高 3.3313；`Front.localPosition.y = +0.08`，`CardView.cs:3974`）

**单位卡 Chaplain Letharius**
```
① 常量（Front 局部 y01）      cen.y = 0.7323
② PlaceAt 写入 Front 局部 y   (0.5 − 0.7323) × 3.3313 = −0.77386   ← 与原版 −0.7737 逐位吻合
③ Front 的 +0.08 只加一次      → 卡根 y = −0.69386
④ 量具（卡根）折成 y01        0.5 − (−0.69386)/3.3313 = 0.70829
⑤ 字墨心↔行盒心残差（10-24 就有）           +0.0033
⑥ 量得                        0.7116（实测 0.7116 ✓）
⇒ 与期望 0.7323 差 = 0.02401 − 0.0033 = 0.0207
```
**战术卡 Master of the Fleet**
```
② (0.5 − 0.7876) × 3.3313 = −0.95808
③ 卡根 y = −0.87808
④ y01 = 0.76359
⑤ 残差 0.0000
⑥ 量得 0.7636 ⇒ 差 = 0.02401，一字不差
```

**假说裁决**
| 假说 | 裁决 | 证据 |
|---|---|---|
| `0.0207 ≈ 0.08 × 0.26` | ❌ 巧合（0.26 在本几何里无出处） | 战术卡差 **0.0240 = 0.08/3.3313** |
| 整层漏加 `+0.08` | ❌ | `CardView.cs:4808` `TmpFont.NewText(FaceContentRoot, …)`；日志 `cardbase.log:103380` M2③ 实测世界位移 `0.08000` |
| 加了被抵消一半 | ❌ | 位移满额 0.08 |
| **期望值坐标系搞错** | ✅ | `CardView.cs:527/633`；`X1:120`；`CardBaseDemo.cs:4273` 同族已订正 |

**旁证**：同父链的兵种行断言（`Editor/CardFaceAssertE24.cs:193/194`，用 `RacePlacedCenterYForTest` = `Front` 局部）本轮全绿。

## 3. 历史读数（本批新红，不是老账）
| 来源 | 总计 | 该两卡 ②③ |
|---|---|---|
| `_tmp_view/20261024d/CardBaseDemo.Run.log:106284` | `426/0` | 单位 ②`0.6864..0.7849` ✓ ③`0.7356` ✓；战术 ②`0.7283..0.8470` ✓ ③`0.7876` ✓ |
| `_tmp_view/20261024d_r3/…` | `426/0` | 同上逐位 |
| `_tmp_view/20261025/cardbase.log:106725` | `429/4` | 全部 `−0.0240` |

## 4. 修法
把 `Editor/CardBaseDemo.cs:2519` 的 `InkBoxCardLocal(v.transform, …)` 改成 `v.FaceContentRoot`（或在 `:2603` 改用 `FaceContentRoot.InverseTransformPoint`），与 `LayerCenterY :4273` 同一规则。**⛔ 不要动 `UnitDescAt` / `TacticDescAt`（`CardView.cs:538/540`）** —— 那是 `Front` 局部真值，改了生产会真低 0.08。
预期：4 条全绿，读数回到 `0.6864..0.7849 / 0.7356` 与 `0.7283..0.8470 / 0.7876`。

## 5. 搜过的文件与词（红线④）
文件：`_tmp_view/20261025/cardbase.log` · `20261024d*` 日志 · `Core/CardView.cs` · `Core/CardText.cs`（未改，仅核状态）· `Editor/CardBaseDemo.cs` · `Editor/CardFaceProbe.cs` · `Editor/CardFaceAssertE24.cs` · `Editor/CardFactionProbe.cs` · `资料/普查产出_1024d/{X1,D5}` · `git status/log/diff`。
词：`FrontAposY` · `FaceContentRoot` · `InkBoxCardLocal` · `LayerCenterY` · `FaceDescBand01` · `DescCenter01` · `UnitDescAt/TacticDescAt` · `PlaceAt/PlaceBottomAt` · `bottomAlign` · `Name and description` · `CBD    ✗` · `M2 ③`。
