# W57 · 自检断言线 · `A1350` 第二批（`MainMenuScene` 3 件）执行报告（SC-9）

> 执行代理 **SC-9** · 2026-10-20 · 第五十七会话
> 判据件：`资料/普查产出_1019/W57_自检线_A1350第一批.md` §8 下一批 + `W57_自检线_A1350读口统一计划.md` §五·3
> 口径：**只做不验** —— ⛔ 没跑 Unity / 没起 player / 没跑任何 `*.Run`；✅ 只跑了秒级 typecheck + 纯 python 只读统计。
> ⛔ 没动 git、没改正本、没删文件。本轮**只写**本报告 + 白名单内 **1 个** `.cs`（`Editor/MainMenuScene.cs`；
> `Editor/MenuCheck.cs` **未动**，基线 numstat `82/5` 改后仍是 `82/5`）。

---

## 〇、三句结论

1. ✅ **3 个一行转发全部落地**：`TmpRenderedRect` → `SelfCheckTextMetrics.RenderedRect` · `LabelRenderedPx`
   → `SelfCheckTextMetrics.RenderedPx` · `RenderedRect` → `MenuCheck.NodeRectPx(t, RectCenterMode.Node, …)`。
   **业务调用点一个没动**（13 / 16 / 3，见 §二）。
2. ✅ **中心按计划 §五·3 从甲式换成乙式**（`MenuDraw.PixelOfDesign(PosInDesignSpace(t))`，与 `MenuDraw.QuadRectPx`
   的中心项是**直系同形副本**）；`Node` 档保住 `A668`「中心恒在传入节点」、**先找 `Label` 再找 `ImageQuad`**
   的旧分支顺序也原样保留（`MenuCheck.NodeRectPx` 就是照旧 `RenderedRect` 逐句实现的）。
3. ✅ **typecheck 运行时 0 / 编辑器 0**；`MainMenuScene.cs` **LF、0 个 CRLF、无 BOM** 未翻；
   `MenuCheck.cs` 未动。**没有发现意外的数值变化**（唯一读数变化 = 计划点名的那一处中心换帧）。

---

## 一、改动清单（3 个转发 + 中心换乙式）

文件：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（改后现读行号）

| # | 函数（现读） | 改前读口 | 改后 | 说明 |
|---|---|---|---|---|
| 1 | `RenderedRect` `:314` | 宽高 = `LabelRenderedPx` / `q.WorldW·108f`；**中心 = 甲式** `LayoutSpace.PxX/PxY(t.position)`（原 `:324`） | `=> MenuCheck.NodeRectPx(t, MenuCheck.RectCenterMode.Node, out …)` | **中心换乙式**（计划 §五·3 唯一一处 sanctioned 读数变化）；`Node` 档 = 中心恒在传入节点 |
| 2 | `TmpRenderedRect` `:362` | 世界帧四角（`localToWorldMatrix` × `textBounds` → `LayoutSpace.PxX/PxY`），12 行 | `=> SelfCheckTextMetrics.RenderedRect(t, out …)` | **纯搬运**：被删体与共享实现归一化后 **611 字符逐字相同**（机核，见 §三·1） |
| 3 | `LabelRenderedPx` `:391` | `TmpRenderedRect` + 退回 `Label.WorldW/H × 108f`，`lb == null ⇒ zero` | `=> SelfCheckTextMetrics.RenderedPx(lb)` | 共享实现 = `TryRenderedPx` 的「只要宽高」版，三条支逐字同契约 |

**中心乙式的来源（乙式来源链）**：
`MenuCheck.NodeRectPx(t, Node, …)` → `MenuCheck.DesignPx(node = t)` → `MenuDraw.PixelOfDesign(MenuDraw.PosInDesignSpace(t))`
→ `LayoutSpace.ToDesignPixel`（`Core/LayoutSpace.cs:299`）。
其中「中心 = `PixelOfDesign(PosInDesignSpace(t))`」正是 **`MenuDraw.QuadRectPx` 的中心项**（`Shell/MenuDraw.cs:737`
`Vector2 cpx = PixelOfDesign(PosInDesignSpace(q.transform));`）—— **直系同形副本**，未新写第三种写法。
`Node` 档（`A668`）保留：`node` 不换成 `lb.transform`；`Label` 优先、`ImageQuad` 兜底的顺序与旧函数一致。

**行为核对（静态逐位；⛔ 没跑 `MainMenuScene.Run`）**：

- **宽高**：label 支改后 = `SelfCheckTextMetrics.RenderedPx` = 旧 `LabelRenderedPx`（本批第 3 件就是转发到它，
  三条支逐字同契约）；quad 支旧 = `q.WorldW * 108f`，新 = `q.WorldW * K`，`K = LayoutSpace.DesignPxH / DesignHeight
  = 1080f / 10f`，两者都是精确可表示值 ⇒ **商精确等于 108f，逐位相同**。
- **文字矩形**：`TmpRenderedRect` 被删的函数体 vs `SelfCheckTextMetrics.RenderedRect` 的现体 —— 归一化空白后
  **同为 611 字符、逐字相等**（机核，见 §三·1）⇒ 纯搬运。
- **中心（唯一变化）**：甲式 → 乙式。y 那一半两条口**同式**（`ToDesignPixel` 的 y 就是 `PxY`）；x 那一半
  16:9 + 父链单位缩放下 ≤ 2.5e-4 px（`PxPerWorldX = 1920 / VisibleWidth ≈ 107.99999`），非 16:9 / 父链带缩放 M
  才见真章 —— 这是计划 §一·2 / §五·3 写明的**预期变化**，不是意外。**未实测**（口径：只做不验）。
- **契约**：`t == null` / 无 `Label` 且无 `ImageQuad` ⇒ `false` + 四个 `out` 全 0（`NodeRectPx` 与旧 `RenderedRect`
  逐字同）；取组件 `GetComponentInChildren<Label>()` / `<ImageQuad>()` **均不带 `true`**（含 inactive 口径未变）。

**文档同步（铁律 5）**：三个函数各补一段「2026-10-20（`A1350` 第二批）」说明（读口 / 契约 / 唯一读数变化）；
`RenderedRect` 旧句「中心仍是传进来那个节点的位置（`t.position`）」已就地改成「传进来那个节点（`t`；读设计帧）」；
`TmpRenderedRect` 头里「节点**世界** x ± 宽÷2」也已补「🆕 2026-10-20 起读的是设计帧」。

---

## 二、调用点计数（注释剥离后按「标识符 + `(`」机数；行号 = 改后现读）

| 符号 | 改前 | 改后 | 业务调用点（应不变） |
|---|---|---|---|
| `RenderedRect(`（排除 `TmpRenderedRect`） | 14 = 1 定义 + **13 调用** | 14 = 1 定义 + **13 调用** | **13 → 13 ✅** |
| `TmpRenderedRect(` | 18 = 1 定义 + **1 包装内**（`LabelRenderedPx` 体内）+ **16 业务** | 17 = 1 定义 + **16 业务** | **16 → 16 ✅**（少的 1 处随第 3 件搬进 `SelfCheckTextMetrics`） |
| `LabelRenderedPx(` | 5 = 1 定义 + **1 包装内**（`RenderedRect` 体内）+ **3 业务** | 4 = 1 定义 + **3 业务** | **3 → 3 ✅**（少的 1 处随第 1 件改走 `NodeRectPx → RenderedPx`） |

- `RenderedRect` 的 13 个调用点（现读）：`:212` · `:3301` · `:3302` · `:3307` · `:6187` · `:6200` · `:7525` ·
  `:7559` · `:9149` · `:9247` · `:10667` · `:12180` · `:12181`。
  > ⚠️ 文本搜索会多出一处 `:363` —— 那是转发体里的 **`SelfCheckTextMetrics.RenderedRect(`**（限定名），
  > **不是**本文件局部函数的调用，别把它算进去。
- `TmpRenderedRect` 的 16 个业务调用点（现读）：`:247` · `:3072` · `:4793` · `:4951` · `:5573` · `:5588` ·
  `:6528` · `:7472` · `:8646` · `:8865` · `:10421` · `:10423` · `:10632` · `:10979` · `:14754` · `:15257`。
- `LabelRenderedPx` 的 3 个业务调用点（现读）：`:3245` · `:3820` · `:4396`。
- 计数口径与 `SC-8` 报告 §一·1 一致：**业务调用点不变**；包装函数体内那次调用随收口搬进共享实现。

---

## 三、没查清的部分（⛔ 没拿默认值顶替）

1. **没有实跑 `MainMenuScene.Run`**（口径 = 只做不验）⇒「13 个调用点读数逐位不变」只在**静态**层面成立
   （宽高逐位同式 + 文字矩形逐字体搬运）；中心乙式与甲式的差按计划 §一·2 是 ≤ 2.5e-4 px（16:9），**未实测**。
   验收时若某条 1px 容差断言在 x 方向上处于临界，先看它是不是 16:9 主档之外的档位。
2. **尺寸帧缺陷（计划 §二·3）仍在**：`NodeRectPx` 的尺寸项「字走 `RenderedPx`（TMP 路径 = 世界帧）、图走
   `WorldW/H × 108`（设计长度）」在父链带缩放 M 时仍会分家。本批**未修**（不在两件里；账在 `RenderedRectDesign`），
   `MenuCheck.NodeRectPx` 的 doc 已写明。
3. **`RenderedRect` 的中心语义**按 `A668` 保持 `Node`（本批 ⛔ 没动）；「该量卡格还是卡名」仍是待裁项（计划 `R12`）。
4. **`PosInDesignSpace` 遇到「`t` 活着、`parent` 已销毁」这一档**没有逐位核对：Unity 的 fake-null 会让
   `above == null` 走 `t.position` 支，理论上与旧口径等价；**未实跑**。
5. **`MainMenuScene.cs` 改前已有未提交改动**（基线 `git diff --numstat` = `364/87`，别的批次留下，含 `A1835` 等），
   ⇒ 只能按**前后差**归因本批（`391/142`，即 **+27 / −55**），不能单看总量。`MenuCheck.cs` 基线/改后均 `82/5`（未动）。
   行尾前后均 **LF（CRLF = 0）· 无 BOM**；行数 15856 → **15828**（−28）。

---

## 四、typecheck 结果

```
$ TMPDIR=/tmp/wf_sc9 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

> ⚠️ 只证明「编得过」，不证明断言行为 —— 行为验收要等 `MainMenuScene.Run`（按铁律 12 攒到复刻收口后统一跑）。
