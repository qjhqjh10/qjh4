# W6 · `Editor/CollectionScene.cs` 换口（A796′ / A799 / A811 现核）

> 写手代理 **W6** · 独占 `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs` **一个文件** · 改动时刻 **2026-10-16**。
> 行尾 **LF**（改前 `\r\n` = **0** / 改后 = **0**；`git diff --numstat` = **+72 / −19** ⇒ 不是整篇重写）。
> 类型检查 `TMPDIR=/tmp/wf_w6 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 · 编辑器 0 错**（改完跑一次）。
> 🔴 **本件没跑 Unity** ⇒ 一切「行为等价」都是**逐句读源码 + 类型检查**得出的，**不是**跑出来的。⛔ 未动 git / 未改正本 / 未越白名单。

---

## ① 结论（三笔）

| # | 账 | 状态 | 改了几处 |
|---|---|---|---|
| 1 | **A796′ 换口** | ✅ **做完** | **13 处 / 14 个读数**（＋1 个新助手 ＋6 段伴注释） |
| 2 | **A799 夹具** | ⛔ **本文件里没有这一处**（判据 §④-1）⇒ **一处未改** | **0** |
| 3 | **A811 尾巴** | ✅ 现核**在位**（修法 与 验收断言 都在）⇒ 只报不改 | **0** |

- **A796′ 换的是什么**：只换「**从哪个口读那个数**」—— 旧口 `Label.WorldW/H`（= 字段缓存 `_tmpW/_tmpH`，只有 `RefreshBounds()` 写 ⇒ **被测实现自己**）
  → 新口 **TMP 自己渲出来那块 `textBounds`**（文件里已有的 `TmpRenderedRect`）。
  ⛔ **没用「TMP 网格顶点」那条口**（判据：`RO_缓存口径与输入三件.md` §一·3 —— 它多一个首字左边距，`Current Streak` 43.00 vs 45.22 = 2.22px）。
- **没动的东西**：中心仍是**节点位置**（`PxOf(node.transform.position.x)`）；`× 54f` 一律写成 `× 0.5f`（同值）；**期望值 / 容差 / 断言语义一个字没动**；`RectOf` 的**图（`ImageQuad`）那一支一字未动**。
- **换口当天为何不动期望值**：两法**同源**（`RefreshBounds` 读的就是 `textBounds`）⇒ 判据称「逐位同值」（RO §一·3 + 本文件 `TmpRenderedRect` 的 doc）。⚠️ 这一条是**判据/源码级**结论，本件**没跑**（见 §④-4）。

---

## ② 逐处 `文件:行号`（**行号 = 改完之后**；本section除署名外都在 `Editor/CollectionScene.cs`）

**新增助手**（本件唯一新增符号）：

| 处 | 行号 | 说明 |
|---|---|---|
| `static Vector2 LabelRenderedPx(Label lb)`（doc `:214-230`，体 `:231-238`） | `:231` | 走 `TmpRenderedRect(lb.transform, …)`；**量不到 ⇒ 退回旧口** `Label.WorldW/H`（点阵后端**本来就没有** `textBounds`，`WorldW` 读的是 `_texW`）⇒ ⛔ 不是「静默吞掉新口」。`lb == null` ⇒ `Vector2.zero`（与旧写的 `null ⇒ 0f` 同值） |

**13 个换口点 / 14 个读数**：

| # | 账上锚点 | 现行号 | 旧 | 新 |
|---|---|---|---|---|
| 1 | `RectOf` **label 支** | `:379-388`（旧 `:352`） | `node = lb.transform; w = lb.WorldW * 108f; h = lb.WorldH * 108f;` | `node` 仍是 `lb.transform`；`w/h` 取 `LabelRenderedPx`。**`:389` 的 quad 支一字未动** |
| 2 | `TitleLeftPx` | `:415` | `float w = lb.WorldW * 108f;` | `float w = LabelRenderedPx(lb).x;` —— **`w>20 && w<2000` 守卫保留**（量不到仍 ⇒ `−9999` ⇒ 必红） |
| 3 | 直读 `lb0` | `:3308` | `lb0 != null ? lb0.WorldW * 108f : 0f` | `LabelRenderedPx(lb0).x`（null ⇒ 0，同值；下面 `lw > 20f` 守卫原样） |
| 4 | 直读 `olb` | `:3384` | 同上式 | 同上 |
| 5 | 直读 `flb` | `:4301-4302` | `float rw = flb.WorldW*108f, rh = flb.WorldH*108f;` | `var flbSz = LabelRenderedPx(flb); float rw = flbSz.x, rh = flbSz.y;` |
| 6 | 直读 `alb` | `:4353` | `float wpx = alb.WorldW * 108f;` | `float wpx = LabelRenderedPx(alb).x;` |
| 7 | 直读 `sgLb` | `:4514-4515` | `sgLbW / sgLbH` | `var sgLbSz = LabelRenderedPx(sgLb); …` |
| 8 | `×54f` `lcf` | `:1101` | `PxOf(…) + lcf.WorldW * 54f` | `PxOf(…) + LabelRenderedPx(lcf).x * 0.5f` |
| 9 | `×54f` `lil` | `:1102` | `− lil.WorldW * 54f` | `− LabelRenderedPx(lil).x * 0.5f` |
| 10 | `×54f` `dnl` | `:1797` | `− dnl.WorldW * 54f` | 同式 |
| 11 | `×54f` `wnl` | `:1802` | `− wnl.WorldW * 54f` | 同式 |
| 12 | `×54f` `lt` | `:3960` | `− lt.WorldW * 54f` | 同式 |
| 13 | `×54f` `lc2` | `:3961` | `+ lc2.WorldW * 54f` | 同式 |

**6 段伴注释**（都是**描述上面这些点**的旧口径语句 ⇒ 按铁律 5 就地订正、**留了换口痕**）：
`RectOf` doc `:366-367` · `TitleLeftPx` doc `:402-408` · 「③ 标签左对齐」`:3376-3380` · 「小标题左沿 4 条」`:3456-3458` · A404 块 `:4256-4258` · A575 块 `:4350-4351`。

**已核、⛔ 故意不改**（同族但**不是 `Label`**）：`:119-120` · `:357/:362`（`Wpx/Hpx`）· `:436`（`QuadRectOf`）· `:1389/1608/1644/1880/1902/1905/1984/2356/2359/2403/2607/3883/4125/4145/4147/4472/6399/6404` —— 逐个查过声明 = **全是 `ImageQuad`**；`:245`（`RectOf / WorldW/H` 那句说的是**图**）仍然成立。

---

## ③ A811 现状（只报不改）

| 半边 | 在哪 | 现状 |
|---|---|---|
| **最小修法** | `Shell/CollectionWindow.cs:512-514`（`ApplyDrawerSlide` 尾「④」） | ✅ **在位、没漂**：条件 `Slide>=1 && SlideTarget>=1 && prevSlide<1 && RowsBuiltOffBase && Scroll!=null`（那句注释自称「三个限定」= 后三条）⇒ `RebuildFilterRows(p)`（说明注释 `:485-511` 完整） |
| **验收断言** | `Editor/CollectionScene.cs:5122-5148` | ✅ **也已落地**：`:5127` 只 `cFltBtn.Click()`（补偿那次 `win.ClearCardFilters()` **已删**）· `:5128` 断「展开到位」· 紧接三条「（前提）…在」（`Input Text` / `Cell_owned/Label` / `Title Army`）· **灭自证** `probeCard` `:5122/:5145` |
| **根治那一半** | 跨 `Shell/MenuDraw.cs` / `Shell/MenuWindowBase.cs` | ⏳ **仍开着**（把「看框」与「被比的矩形」统一到同一帧）—— 不在本件范围 |

⇒ 本件对 A811 **一处未改**。

---

## ④ 没查清的 / 顺手发现（⛔ 一处未越界改）

1. 🔴 **A799 在本文件【没有落点】—— 派单前提不成立，请调度台订正**（三条判据）：
   - **本文件零个 `MenuDraw.Text` / `TextBox` 调用点**：`grep -c "MenuDraw\.\(Text\|TextBox\)(" Editor/CollectionScene.cs` = **0**（它走 `MenuDraw.Node` + `Label`）⇒ 根本不在 R2 的 199 个调用点里。
   - `普查产出_1015/R2_A799全量表.md:16-20`（计数表：会新裁 15 = 生产 9 + Editor 夹具 6）· `:60-65` —— **那 6 条 Editor 夹具行全在 `./Editor/MainMenuScene.cs`**（`:8712/:8713/:8715/:8770/:8777/:8782`，锚 = `a781Node`）。全量表里**没有一行**是 `Editor/CollectionScene.cs`。
   - **真正的落点 W3 已做完**：`普查产出_1016/W3_MainMenuScene四笔.md:17` + `:74-80`（一处注释覆盖那 6 个调用点，落在 `Editor/MainMenuScene.cs:8992-9002`）。
   - **错在哪**：`普查产出_1016/盘点_GH段剩余小账.md:22` 写「夹具 6 在 `Editor/{BattleScene,MainMenuScene,ShellScene,CollectionScene,RewardsScene,ShopScene}.cs`」（每文件 1）—— 与 **R2 自己的表矛盾**；同文件 §④-6 自陈「Editor 侧那 6 个的逐处行号**没抠出来**」⇒ 那份文件清单是**推断**。
   ⇒ 本文件该笔 = **不适用（0 处）**；若调度台另有 A799 之外的「新裁」落点判据，请另给锚点。
2. 🆕 **同族「更外层还在的口」还有一处（RO 清单没列）**：`Shell/CollectionWindow.cs:1305`
   `public float StyleLogoWidthPx { get { return _styleLogo != null ? _styleLogo.WorldW * 108f : 0f; } }` —— 读的**仍是字段缓存那条口**，
   而它是 `Editor/CollectionScene.cs:4895` 那条断言（`StyleLogoWidthPx <= 512 + 1f`）的**唯一尺子**。
   ⚠️ `Shell/` 不在本件白名单 ⇒ **只报不改**；**请调度台决定是否开账**（它比本件的 13 处更外侧：生产侧 getter）。
3. ⚠️ `Editor/CollectionScene.cs:3097`（A773 夹具注释）仍写「「压边」这件事与 `Label.WorldW` 量出来是多少无关」——
   **那句话今天仍成立**（该夹具走 `TextExtentPx`，与本次换口无关）⇒ **未动**（如实记，免得下一个人以为漏了）。
4. 🔴 **验收要跑一次 `CollectionScene.Run`**（本件没跑，按本轮规矩）：
   - 首跑重点看**没有量到 / 退化**那一族：`lw > 20f`（`:3310`）· `owl > 20f`（`:3386`）· `wpx > 5f`（`:4355`）· `sgLbW > 5f`（`:4516`）—— 它们挡的是「量法没生效（0 / 4.29e9）」与「实现真溢出」两档。
   - 期望值 / 容差**一个都不用动**；红了先量实得值判「实现没对齐」还是「量法没生效」，⛔ 别改期望值。
