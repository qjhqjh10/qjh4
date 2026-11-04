# B141-W27 · `4-24` Border Glow + `4-16` 三处视口裁切（第一百二十八会话 · 波次 `W129`）

> 写手：`B141-W27` · 白名单：`Shell/DraftModeWindow.cs` · `Shell/RankedTab.cs` ·
> `Shell/EnergySinglePlayerOnlyEventWindow.cs` · 新建 `Editor/B141_W27_Assert.cs` · 本报告。
> 判据：`判据/B141_W129_对账_UI动画.md` 的 `4-24` / `4-16`；审计：`报告/B138_审计汇总_问题清单.md` §4。
> 原版读数一律 **现读 prefab**（`d:/2/tools/assetripper_out/ExportedProject/Assets/`，2026-11-04）。

---

## 一、四处改动（原版值 / 改前 / 改后）

| # | 站点（我们） | 原版判据（prefab 现读） | 改前 | 改后 | 位置 |
|---|---|---|---|---|---|
| 4-24 | `DraftModeWindow` Ongoing 页 `…/Scaled Content/Glows/{Border Glow Up, Border Glow Down}` | `RectMask2D`：`m_Padding (0,0,0,0)` · `m_Softness (0,174)`；几何见 §二 | **未建**（全文件 0 命中 `Border Glow`） | 建出 `Glows` + 两颗掩码 + 各自 `Glow` 九宫图 | `DraftModeWindow.cs:1693`（调用）· `:4866-4890`（两个方法，追加在文件末尾的 partial 类） |
| 4-16① | `DraftModeWindow` 牌表 `…/Scroll View/Viewport` | `m_Padding (0,0,0,−10)`（Top = −10 ⇒ 顶上外扩 10px）· `m_Softness (0,54)` | `Vector4.zero / Vector2Int.zero` | `new Vector4(0,0,0,−10)` · `new Vector2Int(0,54)` | `DraftModeWindow.cs:1959` |
| 4-16② | `RankedTab` `AllFactions/scroll rect/viewport` | `Player Profile Window.prefab` ▸ `…/Ranking Tab/AllFactions/scroll rect/viewport`：`m_Padding (0,0,0,−10)` · `m_Softness (0,0)` | `Vector4.zero / Vector2Int.zero` | `new Vector4(0,0,0,−10)` · `Vector2Int.zero` | `RankedTab.cs:583` |
| 4-16③ | `EnergySinglePlayerOnlyEventWindow` `Image Mask` | `EnergySinglePlayerOnlyEventWindow.prefab ▸ Image Mask`：`m_Padding (0,0,0,0)` · `m_Softness (375,0)` | `Vector4.zero / Vector2Int.zero` | `Vector4.zero` · `new Vector2Int(375,0)` | `EnergySinglePlayerOnlyEventWindow.cs:386` |

**📍 prefab 出处（`d:/2/tools/assetripper_out/ExportedProject/Assets/`，行号现读）**
* `Draft Mode Menu Demo.prefab`：`Border Glow Up` `:2842`（掩码 `m_Softness` `:23392`）· `Border Glow Down` `:2877`（`:23485`）·
  牌表视口 `m_Padding` `:37666` + `m_Softness` `:37667`。
* `Player Profile Window.prefab`：`Ranking Tab/AllFactions/scroll rect/viewport` 的 `m_Padding` `:64209` + `m_Softness` `:64210`。
* `EnergySinglePlayerOnlyEventWindow.prefab`：`Image Mask` 的 `m_Softness` `:1679`；`Army Selector …/Viewport` `:6898`。

**⚠️ 两处「实例覆盖」**（铁律 5·c：一个值 ≠ 全部情况）：
* `Draft Mode Deck Info Panel.prefab`（**独立那份**）的 `…/Scroll View/Viewport` 是 `(0,0) / (0,0,0,0)`；
  而 `Draft Mode Menu Demo.prefab` 里那**同一个嵌套实例**被覆盖成 `(0,54) / (0,0,0,−10)`。
  我们建的是 `Draft Mode Menu Demo` ⇒ **按实例覆盖那一组**（⛔ 别照独立 prefab 改回去）。
* `Draft Mode Timed Mode Window.prefab` 的同两处与 `Menu Demo` 逐字段同值。

---

## 二、`Border Glow` 的几何（绝对画布 px 的推导）

### 2.1 原版字段（`Draft Mode Menu Demo.prefab` 现读；四份 prefab 的这两颗逐字段同值）

```
Glows            anchor (0,0)-(1,1) · sizeDelta (1820,980) · apos (−20.999878, 9.547968) · localScale (1.1,1,1)
Border Glow Up   apos (28, 109.5)  · sizeDelta (700, 67.08) · RectMask2D: pad (0,0,0,0) · soft (0,174)
Border Glow Down apos (28,−118.3)  · sizeDelta (700, 67.08) · RectMask2D: pad (0,0,0,0) · soft (0,174)
  └ Glow (Up)    apos (0, 26.89)   · sizeDelta (700, 53.78)
  └ Glow (Down)  apos (0,−27.5)    · sizeDelta (700, 53.78)
       Image: m_Sprite = `40k_Separator Fade Sides Horizontal`(128×4) · m_Type Sliced · m_Border (63,0,63,0)
              m_PixelsPerUnitMultiplier 0.34 · m_Color (0.7019608, 0.46274513, 0.17254902, 1) · m_FillCenter 1
```

### 2.2 折算（本仓按**绝对屏幕 px** 摆件 ⇒ 必须把两层缩放折进去）

* `MainContent.localScale = 1.06`、`Glows.localScale.x = 1.1`；
* 同一棵树上的校准点（已验证）：`Win Marks` 的 `apos (1.0000458, 9.547987) · sizeDelta (729.9315, 316.065)`
  ⇔ 我们现用的视觉值 `(697.90,373.80)-(1471.60,708.80)`（773.73 × 335.03 = ×1.06）⇒
  **`x = 中心 + ax×1.06` · `y_down = 中心 − ay×1.06`**（`PxRect` 是左上原点、y 向下）。

`Scaled Content` 中心（我们建的那一格）= **`(1083.70, 551.40)`** ⇒

| 节点 | 绝对矩形（画布 px） |
|---|---|
| `Glows` | 中心 `(1061.44, 541.28)` · 尺寸 `1920×1080 ×1.06 = 2035.2×1144.8` ⇒ **(43.84, −31.12)-(2079.04, 1113.68)** |
| `Border Glow Up` | 中心 `(1094.09, 425.21)` · 尺寸 `(700×1.1×1.06, 67.08×1.06) = 816.2×71.10` ⇒ **(685.99, 389.66)-(1502.19, 460.76)** |
| `Border Glow Down` | 中心 `(1094.09, 666.68)` · 同尺寸 ⇒ **(685.99, 631.13)-(1502.19, 702.23)** |
| `Glow`（Up 子件） | 中心 `(1094.09, 396.71)` · 尺寸 `816.2×57.01` ⇒ (685.99, 368.21)-(1502.19, 425.21) |
| `Glow`（Down 子件） | 中心 `(1094.09, 695.83)` · 尺寸 `816.2×57.01` ⇒ (685.99, 667.33)-(1502.19, 724.33) |

> 为什么两条带子落在「胜场面板」的上/下沿：`Win Marks` = `(697.9,373.8)-(1471.6,708.8)`；
> Up 带顶 `389.66` 在面板顶下 ~16px、Down 带底 `702.23` 在面板底上 ~6.6px；两条各 71px 高、
> 掩码 `softness.y = 174` **大于整条带高** ⇒ 整条带都是一段线性渐隐（这正是原版的样子）。
> 图片宽度 816.2 比面板 773.7 略宽，来自 `Glows.localScale.x = 1.1`。

### 2.3 实现要点

* 通道：`ViewportClip.Hang(parent, name, maskR, Vector4.zero, new Vector2Int(0,174))`
  + `MenuDraw.Nine(vc.transform, tex, imgR, (63,0,63,0), 128, 4, q, tint, true, "Glow", (63/0.34,0,63/0.34,0))`。
* 九宫角块：`m_PixelsPerUnitMultiplier 0.34` ⇒ 画出来的角块 = `63 ÷ 0.34 = 185.29 px`
  （与同文件分隔线那条 `63 ÷ 2.89` 同一口径）。
* 渲染队列取 **`QText`(4165)**：原版 sibling 序里 `Glows` 排在 `Win Marks` 之后 ⇒ 压在胜场格
  （`QArt` 4160 / `QArt1` 4161）**之上**；它与任何文字都不重叠，这一档只为「高于格子、低于命中区 `QHit` 4170」。
  （若将来它要与文字抢层级，再单开一档常量。）
* `Glows` 容器本身**没有** `localScale`（那一层 1.1 已经折进子件矩形）—— 本仓不做「父级缩放传播」。

---

## 三、顺手发现（**不在白名单，只记不修** ⇒ 需调度台另派）

### 3.1 同一颗 `Army Selector` 视口的 softness 是**逐窗不同的四组值**

`ShowEventArmiesPanel.cs:110` 现在写死 `Vector4.zero, Vector2Int.zero`。原版现读：

| prefab | 节点 | `m_Softness` | `m_Padding` |
|---|---|---|---|
| `EnergySinglePlayerOnlyEventWindow` | `Army Selector Panel/Army Selector/Viewport` | **(25,0)** | (0,0,0,0) |
| `SkirmishModeEventWindow` | `Ranked Army Selector/Army Selector/Viewport` | **(0,52)** | (0,0,0,0) |
| `RankedEventWindowV2` | `Ranked Army Selector/Army Selector/Viewport` | **(0,52)** | (0,0,0,0) |
| `Campaign Army Selector` / `Campaign Tab` | `…/Viewport` | (0,0) | (0,0,0,0) |
| `RankedClassicLeaderboardPopup Variant` / `RankedSkirmishLeaderboardPopup` | `Ranking Display/Content/Army Selector/Viewport` | **(42,0)** | (0,0,0,0) |

⇒ **不能给它写一个常量**，得把 softness 变成 `ShowEventArmiesPanel.Build(...)` 的形参、由各宿主逐窗传
（铁律 5·c：「一个值 ≠ 全部情况」；这与 `4-16` 那条「视口参数逐处不同」是同一个病）。
⚠️ 本批白名单只含三个窗文件 ⇒ 本写手**未动**它。

### 3.2 `ProfilePage.cs` 那段说明只写了 `m_Softness`，没写 `m_Padding`

`Shell/ProfilePage.cs`（六页公共底座的文件头注释）写「`Trophies Tab/Scroll/Viewport` 与
`Ranking Tab/AllFactions/scroll rect/viewport` = `(0,0)`（硬边）」——**只针对 `m_Softness`，是正确的**；
但同一颗掩码上还有 `m_Padding (0,0,0,−10)`（顶上外扩 10px），那段没提。建议补一句（不在本批白名单）。

---

## 四、派活单里两处口径要订正（铁律 5：判据本身错要就地写清）

1. **「`RankedTab` 原版 `soft 42`」不成立** —— `RankedTab.cs:583` 对应的是
   `Player Profile Window ▸ … ▸ Ranking Tab/AllFactions/scroll rect/viewport`，实读
   **`m_Softness (0,0)`（硬边）+ `m_Padding (0,0,0,−10)`**。
   `soft (42,0)` 是 **`Daily Reward Popup ▸ Army Selector ▸ Viewport`**（`4-16` 的第一处、`B141-W6` 的活）。
   ⇒ 本处按 prefab 实读的 **pad −10** 复刻。
2. **「`EnergySinglePlayerOnlyEventWindow.cs:386` 原版 `pad 25`」不成立** ——
   `:386` 是 `Image Mask`，实读 **`m_Padding (0,0,0,0)` + `m_Softness (375,0)`**；
   那个「25」是本 prefab 的**第二颗** `RectMask2D`（`Army Selector Panel/Army Selector/Viewport` 的
   `m_Softness (25,0)`），由共享件 `ShowEventArmiesPanel.cs` 建、**不在本批白名单**（见 §3.1）。
   ⇒ 本处按 **soft 375、pad 0** 复刻；`25` 那一半留作 §3.1 的后续。

---

## 五、验证

* **秒级类型检查**：`cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w27 bash 工具/typecheck.sh`
  ⇒ **运行时 0 错 · 编辑器 0 错**（改动前后各跑一次，均为 0/0）。
* **行尾**：三个被改文件 + 新断言文件都是**纯 LF、无裸 CR**（二进制计数核对过，`crlf=0 / bareCR=0`）。
* **断言**：`Editor/B141_W27_Assert.cs`（新），`B141W27_Assert.Run()`，**86 个调用点 / 实际展开 ≈112 条**：
  * `A` 段（一扇 `DraftModeWindow`）：两条 `Border Glow` 各 22 条 —— 存在 / `ViewportClip` /
    `softness (0,174)` / `padding 0` / `BaseRect` 四个沿 / `RenderClip` / 子件 `Glow` / 图名 /
    **软边真的落到顶点色（最小角 alpha < 1）**；
  * `B` 段：牌表视口 —— `soft (0,54)` / `pad (0,0,0,−10)` / `BaseRect` 四个沿 /
    **`RenderClip` 相对 `BaseRect` 顶上多 10px**（结构性判别式）；
  * `C` 段（裸 `RankedTab`）：`soft (0,0)` / `pad (0,0,0,−10)` / 四个沿 / **顶上多 10px**；
  * `D` 段（`Energy` 窗）：`Image Mask` `soft (375,0)` / `pad 0` / `RenderClip` 非空；
  * `E` 段：源码非注释扫描（`BuildOngoingGlows(scaled)` 恰 1 · 三处调用的字面量还在且没有退回 `…zero`）。
* **灭自证**：
  * 把任一处 `softness` / `padding` 改回 0 ⇒ A2/B2/C2/D2 当场红；
  * 把节点矩形改错（漏乘 1.06 / 1.1、或 y 方向取反）⇒ A6/B4/C4 红；
  * 把 `softness` 改回 0 ⇒ 渐隐带消失 ⇒ A8（顶点色 alpha）红（与 A2 **不共因**）；
  * 把 pad 改回 0 ⇒ B3/C3（外扩量 10px）红（与 B2/C2 **不共因**）；
  * 把 `BuildOngoingGlows(scaled)` 删掉 ⇒ E1 红、A1 前提红。

### 怎么接（⚠️ 本写手不动宿主）
宿主 = `CardPresentation/Editor/MainMenuScene.cs`。在该宿主**合计行之前、同一物理行**（该行第一个 `//` **之前**）追加：

```
B141W27_Assert.Run(); _sink.Pass += B141W27_Assert.Pass; _sink.Fail += B141W27_Assert.Fail;
```

⚠️ 追加必须落在第一个 `//` 之前（本仓已 6 次踩「代码落进行尾注释 ⇒ 编译过、账绿、断言一条没跑」）。
⚠️ 本断言**会建两扇窗再销毁**（`DraftModeWindow` / `EnergySinglePlayerOnlyEventWindow`）+ 一格裸 `RankedTab`；
三个 `finally` 里都 `PointerLayer.UnregisterOwnedBy` + `DestroyImmediate`，并清 `DraftModeWindow` 的静态一局
（`ClearRunForTest`）与 `WindowsManager.ClearReuseCacheForTest()`。它**不需要场景里已经建过这些窗**。

---

## 六、行数变化与账影响

| 文件 | 行数 | 说明 |
|---|---|---|
| `Shell/DraftModeWindow.cs` | 4849 → **4898**（+49） | `BuildOngoingPage` 里 **+2 行**（`Glows` 注释 + 调用）· 文件末尾 partial 类里 **+47 行**（两个新方法）；`1959` 那一行是**同物理行改写**（零行差） |
| `Shell/RankedTab.cs` | 853 → **853**（0） | 三行注释 + 调用行**同物理行改写** |
| `Shell/EnergySinglePlayerOnlyEventWindow.cs` | 1747 → **1747**（0） | 两行注释 + 调用行**同物理行改写** |
| `Editor/B141_W27_Assert.cs` | 新增 472 行 | 纯断言 |

🔴 **账影响**：`DraftModeWindow.cs` 在 `:1693` 之后**整体 +49 行** ⇒ 该文件的 A 行落点会漂
（现读账内引用该文件的条目 ≈175 行，跨 `轮抽.tsv` / `卡牌面_渲染.tsv` / `联机.tsv` 等）。
按纪律 **不在写手这侧改账** ⇒ 同步点由调度台跑 `check_ledger_points --all-A --fix`（且必须在
「没有 `.cs` 写手 + 补丁已合并完」时做）。另两个文件零行差，其落点不受影响。

---

## 七、❓ 未做 / 待确认

1. **运行时断言未实测**：红线不许跑 Unity ⇒ 上面那 112 条只过了**编译**。
   接入宿主后请留意两处**可能的前提不成立**（若红，按提示改断言，不是改产品）：
   * `DraftModeWindow.Create` 在纯编辑器宿主下应能建出整棵树（`DraftWindowAssertD2` 同款路径）；
   * `RankedTab.Setup()` 在 `Win == null` 时走的是「图取不到就记 `MissingArt` / 不画」，不应抛异常。
2. **`Army Selector` 视口的 softness（§3.1）未修** —— 需给 `ShowEventArmiesPanel.Build` 加参数，
   四/六处宿主逐窗传值；不在本批白名单。
3. **`Border Glow` 的观感**（渐隐带是不是「一条淡琥珀色横带」）只能真机肉眼确认 ——
   断言只能钉住「参数对、软边真的上了」，钉不了「像不像」。真机入口：主菜单 → 轮抽 → 走到 Ongoing 页。
4. `Glows` 的 `localScale.x = 1.1` 是我**手工折进子件矩形**的（本仓不做父级缩放传播）；
   若将来有人把 `MenuDraw` 改成「父级缩放传递」，这里的四个矩形要**改回设计值**（⛔ 别两边都乘）。
