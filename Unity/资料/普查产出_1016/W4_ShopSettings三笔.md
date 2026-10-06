# W4 · `Editor/{ShopScene,SettingsScene}.cs` —— A672（13 处裸探针）/ A796′（缓存口径换口）/ A796（开包窗那 1 处）

> 「清空 A 表」第六轮 · 2026-10-15 · **执行写手 W4**。**独占**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShopScene.cs` + `Editor/SettingsScene.cs`
> **⛔ 没跑 Unity**（本轮规矩）· 没动 git · 没改正本 · 没碰白名单外任何文件（`Shell/*` 与另外五个 `Editor/*Scene.cs` 一行未动）。
> **秒级类型检查**：`TMPDIR=/tmp/wf_w4 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（一趟过，无跨文件假错）。
> **行尾**：两个文件**改前改后都是纯 LF**（`ShopScene` CRLF 0 / LF 4713 → 4741；`SettingsScene` CRLF 0 / LF 2625 → 2645）。
> **`git diff --numstat`**：`ShopScene.cs` **+30 / −2** · `SettingsScene.cs` **+20 / −0**（那 2 行删除 = `RectOf` 的 doc 一行 + label 支那一行，**其余全是纯插入**）。

---

## ① 结论（三行）

1. **A672 夹具裸探针：实做 11 处**（`SettingsScene` **8** + `ShopScene` **3**），全部显式钉成 **`WindowType.Fullscreen`**（= A672 之前那个默认值 ⇒ 与改前**逐位同义**）。
   🔴 **不是账上的 13 / 不是派单里的「Settings 10」** —— 现读 13 处裸探针里，**2 处必须【不赋】**（`SettingsScene.cs` 的 `tWin` / `t3Win`：A672 哨兵断言**自己的探针**，断言就要求 `!HasType`；与 `Editor/ShellScene.cs:1040` 同一类），**1 处**（`t2Win`）本来就赋了 `Popup`。逐条 → §二-①。
2. **A796′ 换口：`ShopScene.RectOf` 的 label 支已换**（宽/高从**字段缓存** `Label.WorldW/H` → TMP 自己的 **`textBounds`**，走本文件既有的 `TmpRenderedRect` 那条口）。
   ⚠️ **改动是「一处定义、21 个调用点同时生效」**，不是 21 处逐点改（`RectOf` 是共用量尺）—— **图那一支一个字节没动**。✅ **没换成「TMP 网格顶点」那条口**（硬约束 ④·1）。
3. **A796 那 1 处（开包窗）：判为【不接】**，理由见 §三（核心 = 接上去**必然**改被测状态：`TryOpen()` → `Open()` → `Build()` 会把那一包的**一次性揭示状态整个重置**）。

---

## ② 证据 / 改动清单（逐笔逐处 · 行号 = **改后现读**）

### ① A672 夹具裸探针（11 处赋值；口径全文写在每个文件的**第一处**）

| # | 文件:行（赋值行） | 探针 | 同处的裸 `AddComponent` 行 |
|---|---|---|---|
| 1 | `Editor/SettingsScene.cs:393` | `probeWin`（A166 的 `placement` 哨兵探针） | `:387` |
| 2 | `Editor/SettingsScene.cs:1266` | `w1`（A165(二) ① 开关关） | `:1258` |
| 3 | `Editor/SettingsScene.cs:1277` | `w2`（A165(二) ② 开关开） | `:1276` |
| 4 | `Editor/SettingsScene.cs:1297` | `w3`（A165(二) ③ 烤 1.35） | `:1296` |
| 5 | `Editor/SettingsScene.cs:1314` | `w4`（A165(二) ④ 开关关+烤值） | `:1313` |
| 6 | `Editor/SettingsScene.cs:1411` | `a228w2`（A228 态二） | `:1410` |
| 7 | `Editor/SettingsScene.cs:1736` | `a167w1`（A167 态一） | `:1735` |
| 8 | `Editor/SettingsScene.cs:1770` | `a167w2`（A167 态二） | `:1769` |
| 9 | `Editor/ShopScene.cs:3365` | `a294w2`（A294 态二） | `:3358` |
| 10 | `Editor/ShopScene.cs:3529` | `a297gw2`（A297 态二） | `:3528` |
| 11 | `Editor/ShopScene.cs:3641` | `a298gw2`（A298 态二） | `:3640` |

**数字对账（现读，⛔ 不信旧账）**：

- 两个文件 `AddComponent<GameWindow>()` 现读 **14 处**（`SettingsScene` **11** + `ShopScene` **3**）。`SettingsScene` 那 11 处 = 上表 1~8 的 8 处 + **`tWin`(`:423`) · `t2Win`(`:439`) · `t3Win`(`:451`)**。
- **`t2Win`（`:439`）**：下一行 `:441` 就是 `t2Win.type = WindowType.Popup;` ⇒ **已赋**，不在本笔范围内。
- 🔴 **`tWin`（`:423`）与 `t3Win`（`:451`）⛔ 不能赋**：它们**就是** A672 那两条哨兵断言自己的探针 ——
  `:426` 断 `!tWin.HasType`、`:429` 断「忘了赋 `type` ⇒ 出声 且 `tErrs[0]` 是 `type` 的文案」、`:453` 断 `t3Win` 那两条错都出声（`errs[0]` = `placement`、`errs[1]` = `type`）。
  给它们钉一个明确值 = **当场把 `SettingsScene.Run` 的 A672 那几条断言改红**（也正是派单里点名要保住的 `Editor/ShellScene.cs:1040` 那种情形）。
  ⇒ **账上「Settings 10 = 11 − 1」是把这两处算进去了**（那 2 处只能保持哨兵）。
- **为什么钉 `Fullscreen` 而不是 `Popup`**：A672 之前 `GameWindow.type` 的默认值**就是** `Fullscreen`（`Shell/WindowsManager.cs:102` 现读 = `public WindowType type = UnsetType;`，哨兵 `UnsetType` 在 `:100`）
  ⇒ 钉 `Fullscreen` = **「把旧默认值写成显式的」**，两层守卫（`:908`/`:1032`）不命中、行为与改前逐位一致；⛔ 这不是「这些探针该是全屏窗」的判断（它们没有原版对应物）。
- **对既有断言的影响 = 0（逐条核过）**：
  · 这 11 个探针里 **10 个**只 `TryOpen(null)` / 直接 `new`（**不调 `AttachToAnchor` / `OpenWindow`**）；唯一调 `AttachToAnchor` 的是 A166 那只 `probeWin`，而那两个 `AttachToAnchor` 只读 `placement`（`type` 那两处守卫在赋过值之后不命中）⇒ `type` 的**读者**（`OpenWindow:915` / `ShowPreviousWindow`）**一处都碰不到**，值换了也无人读。
  · 唯一有耦合的是 **A166 那只 `probeWin`**：赋 `type` 之后，第一拍 `CaptureErrors(AttachToAnchor)` **只剩 `placement` 一条**（原来两条）
    ⇒ `:397` `errs.Count > 0` ✓、`:400` `errs[0].Contains("没有显式赋值")` ✓（**`errs[0]` 仍然是 placement 那条**）、第二拍 `:406` `errs2.Count == 0` ✓ —— **三条判据一个字没改也照样成立**（本件把 W8 §三-⑤ 那张表里「第 1 拍写回 `type`」那条**前提**也顺手解掉了：不再依赖写回）。

### ② A796′ 换口（`Editor/ShopScene.cs`）

| 锚点 | 改前 | 改后 |
|---|---|---|
| `RectOf` 的 doc（`:959-961`） | 「图走 `ImageQuad.WorldW/H`、字走 `Label.WorldW/H`（都是 TMP/材质的真测量）」 | 「图走 `ImageQuad.WorldW/H`、字走 **TMP 自己渲出来那块 `textBounds`**（🆕 A796′ 换口）」 |
| `RectOf` 的 label 支（定义 `:966` · 分支 `:973-991`） | 一行：`if (lb != null) { node = lb.transform; w = lb.WorldW * 108f; h = lb.WorldH * 108f; }` | `node = lb.transform;` + `if (TmpRenderedRect(node, out rx1…ry2)) { w = rx2 - rx1; h = ry2 - ry1; } else { 退回旧口 }`（+ 12 行注释写清口径/边界/⛔ 不许换成网格顶点那条口） |
| 图那一支（`:992`） | `q.WorldW * 108f` | **一字未动**（`ImageQuad` 不是缓存口） |

- **覆盖**：`RectOf` 现读 **21 个调用点**（`:1413/1414/1415/1435/1448/1482/1892/1903/1933/2007/2075/2077/2133/2333/2529/3198/3220/3871/3879/3892/3969`）+ 1 个定义 —— 一处改动全部生效。
  ⚠️ **和 RO 记的「22 处」对不上**（我 21 + 定义 = 22 是最接近的解释，**没查实**；RO 那份的行号在本文件上整体偏 ≈ +69，见 §四）。
- **只换「从哪个口读那个数」**：中心仍是**节点位置**（`AlignLeft/Right` 把节点挪走的口径没动）。
  🔴 **为什么不连「那块矩形的四角」一起换成 `textBounds` 的**：那样矩形会整体挪**两项** —— TMP 子节点上的 `_vOffset`（A712 字墨校正，`RefreshBounds` 里那句）与 `(0.5 − anchor)` 那一项 —— 两项今天**都不保证为 0**
  ⇒ 期望值会集体漂（那就不是「换口」而是「改量法」了，与 RO §一·3「期望值一位都不用动」冲突）。
- **为什么这是灭自证**：`WorldW/H` 那份缓存**只有 `RefreshBounds()` 写**（`Battle/Label.cs` 现读 `:1013-1018`），而 `SetFontSize`（`:601`）/`SetCharSpacing`（`:631`）**只重排 mesh、不刷缓存** ⇒ 谁在末次刷缓存之后重排一次，旧口照旧报旧值。⚠️ **`SetAutoFitBox` 不属这一族**（它末句就是 `RefreshBounds()` —— A721 已订正过这个口径）。

### ③ A796（见 §三，结论 = 不接，**本文件零改动**）

---

## ③ A796 那 1 处（`Editor/ShopScene.cs` 开包窗）—— **判断：不接**

**先决条件（派单明写）＝「接上去会不会改被测状态」⇒ 会，而且不是「无害地改」**：

1. `MenuDraw.CheckShadeClickRule`（`Shell/MenuDraw.cs:2301`）的③步硬要求**点之前** `state() == Open`（`:2329`，不满足就**报红并早退**）⇒ 必须在 `bp` **已关**（`:2551` 那句「点整屏 ⇒ 关窗」之后）**重开**。
2. 重开这条路**只有一条**：无参 `TryOpen()`（`Shell/WindowsManager.cs:496-499`；它**不调** `SetupData` ⇒ `Data`/`Page`/`Index` 原样保留）→ `OpenByState` 的 `Closed` 支（同文件 `:509-517`）→ **`Open()`** → `BoosterPackOpenWindow.Open()` **无条件调 `Build()`**（`Shell/BoosterPackOpenWindow.cs:289-293`）。
3. 而 `Build()`（`:304-327`）**正是**重置本段刚刚建立并断完的那一整套状态：`Cards = RollPack(Army, Page*1000+Index)`（**重新发牌**）、`CardsLeft = 5`、`Opened[i] = false`、`SlotHits/i 全 null`、`MenuDraw.ClearChildren(transform)`（整棵树重建）。
   ⇒ 本节赖以存在的「**5 张全翻开 ⇒ `Tap to close` 亮**」那一档**当场没了**；而**揭示链是一次性的**（`:2470` / `:2487` 那两处 `SlotHits[i].ClickForTest()` 翻转 + 闪烁时钟 `bBlk.Clock`）—— 即 W7 §四·4 记的那条理由，**现读复核成立**。
4. 重开之后压暗层那颗命中区还**变成 inactive**：`CloseNode`（`Tap to close`）出厂是关的、**只有 `CardsLeft <= 0` 才 `SetActive(true)`**（`Shell/BoosterPackOpenWindow.cs:575`），而 `Collider` 是**它的子件**（`:374` `MenuDraw.ShadeHit(CloseNode, …, "Collider")`）
   ⇒ 那一口会点在**这一态下根本不可能被点到的节点**上（`WindowButton.Click()` 不判激活态，所以**会绿**）—— 那是「绿得不诚实」，不是覆盖。
5. 想让第 4 条诚实，就得重开后再**把 5 张重新翻开一遍**（重跑一次性揭示 + 重播粒子 + 重启闪烁）—— **改的面比要断的那一句大得多**，而且**必须有 Unity 跑一次才能证伪**（本件禁跑）。
6. **本窗【已有】站立点，且不比本口弱**：`Shell/BoosterPackOpenWindow.cs:374` 那颗命中区**就是** `MenuDraw.ShadeHit` 建的 ⇒ `:2523` 的 `MenuDraw.CheckShadeRule(...)` 已经断了「是公共件建的 + 档位与视觉压暗层一致 + 低于内容档」（含 `WasShadeHit`，`Shell/MenuDraw.cs:2235`），
   紧接着 `:2550-2551` 又用**真命中区**点击 + 断 `Closed`。本口的**增量**只剩「点之前必须是 `Open`」与「不是吸收层」两条。
   ⚠️ 因此账上那句「（开包窗）只配了几何断言」**在现读下不成立**（W7 §三·2 已复核过同一条）。
7. **⛔ 也不许「挪到既有那句之前」**：本口会把窗真的关掉 ⇒ 后面那句 `Check(bp.CurrentState, Closed, "点整屏 ⇒ 关窗")` 会**恒真**（正是本仓反复点名要避免的形状，`Shell/MenuDraw.cs:2282-2289`）。

⇒ **结论：不硬接。** 要真接，唯一诚实的形状 = **另起一块**：走生产入口新开一扇 ⇒ 翻完 5 张 ⇒ 再跑本口（那是**新账**，且**要先跑一次 Unity** 才知道 `Build()` 之后重开会不打扰后面的段）。

---

## ④ 没查清的 / 还欠什么

1. **🔴 `RectOf` 换口「当天逐位同值」是【静态推理】，不是跑出来的**（本件禁跑 Unity）。推得的两个**可能分叉点**（都如实标，没跑就分不出）：
   - **父链缩放**：`TmpRenderedRect` 走 `localToWorldMatrix` ⇒ 量到的是**含父链缩放**的宽高；旧口 `Label.WorldW` 是**纯局部值**。本文件今天相关处都是 scale 1（`:3844-3845` 那条「窗根 `lossyScale.x` = 1 ⇒ `RectOf` 的 px 与设计 px 同量纲」的前提断言就是钉它的），**但这一条不是全局保证** —— 谁将来在 `SmallScreenUI` 开着时量 `RectOf`，新口给的是**渲出来的真值**、旧口给局部值（差 M 倍）。**没查清**：本 run 里有没有哪一段会在缩放态下走 `RectOf`（我只核到 A294/A297/A298 的探针用的是**各自的根**、且每块收尾都 `Set(false)`）。
   - **A266 的「待办兑现」副作用**：`Label.WorldW` 的 getter 会经 `EnsureMeasured()` **当场兑现**「未激活时欠下的折行待办」（生成一整个版面再量）；新口**不做这一下** ⇒ 对一个「在未激活链里被 `SetWrapWidth` 过」的标签，新口读到的是**上一版版面**。**没查清**：本文件 `RectOf` 量的那些标签里有没有这种（`SetWrapWidth` 的调用点全在 `Shell/*`，本文件零处直调）。
   - **空串标签**：`TmpRenderedRect` 对空串 TMP 会给哨兵 `4.29e9`（它自己的 doc 已记）—— 与旧口是否同值**没逐处核**。
2. **`ShellScene.cs:1040`（A672 第二层守卫自己的探针）⛔ 没碰**（不在白名单）；与之同类的 `SettingsScene.cs` 的 `tWin`/`t3Win` 见 §二-①（**建议**把「这两处也是故意不赋」写进账，免得下一轮当成漏改又补一遍）。
3. **同族但不在本件定义里、我【没改】的地方**（⛔ 不越界，列给调度台分流）：
   - `Editor/ShopScene.cs:841` `CheckLeftAt` 的体：`t.position.x - lb.WorldW * 0.5f`（**1 个调用点**，`:4243`）—— 与 `RewardsScene.TextLeftPx/TextRightPx` **同形同义**（RO §一 判「该收」那一族），但 RO 的逐文件表**只把这个文件的 `RectOf` 划进来了** ⇒ 留着。
   - `Editor/SettingsScene.cs:221`（`leftPx`）· `:890`（`tlb` 左沿）· `:2169`（`ipLb` 左沿）是**同族直读**；本文件**根本不在 A796′ 的清单里**（RO/盘点两处都没有它）⇒ 一行未动。
4. **数字对不上（如实记，别当结论）**：RO §一 给本文件的**行号整体偏 ≈ +69**（它写 `RectOf` label 支在 `:1034/:1041`，现读在 `:966/:973`；它写的 `:1034` 今天是 `TmpRenderedRect` 的**函数体**）；「**22 处**」我现读是 **21 个调用点 + 1 个定义**。**没查清**差在哪（不排除它把 `RectOfUnion` 那条转发也算了，本文件里 `RectOfUnion` **不**转发 `RectOf`）。
5. **`Editor/BattleScene.cs` 这一轮没再出现半成品错误**（上一批出现过 169 条全集中在那一个文件）—— 本件一趟类型检查 0 错。如实记，⛔ 没去碰它。
