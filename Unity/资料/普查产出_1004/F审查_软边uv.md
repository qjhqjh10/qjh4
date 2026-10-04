# F 审查 —— 软边 uv 真缺陷修复 + 一批注释订正

> 审查代理 **R-F**，2026-10-04。审的对象 = 执行代理 **F** 本会话的改动，落在 4 个文件：
> `Shell/MenuDraw.cs` · `Battle/ImageQuad.cs` · `Battle/WaitBanner.cs` · `Shell/BoosterInfoPopup.cs`
> （`git diff` 里这几个文件同时含**上一批**的改动 —— 本报告按「F 自报的 F1/F2/F7/F9/F11/F13 + 那处自报的额外改动」逐条核，
> 凡是判据一样的，**不看是谁写的**）。
> 判据正本 = `资料/普查产出_1004/W6审查_共用件.md`（F1–F7/F9/F11/F13）· `d:/4/_tmp_view/q1_rm2d.txt`（⚠️ **2026-10-05 更正**：原来称它「**全量表**」是错的 —— 它**只含 3 个菜单族包共 156 个 `RectMask2D`**，**全库真值 222**，见 §F3）·
> 本地 UGUI 源码 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/RectMask2D.cs`。
> **只读审查，一行代码都没改**；本文件是我唯一写过的文件。

---

## 〇、🔴 对第 ③-1 条（F 自报「多改了一处」）的明确结论

**结论：成立（有条件，条件已满足）—— ⛔ 不该回退。**

被审的那一句是 `Shell/MenuDraw.cs:310`：

```csharp
if (q.SoftEdgeRebuild != null && !SameRectNear(QuadRectPx(q), vis)) PlaceCell(q, vis, vis, uv0);
```

**(a) 这个条件真能覆盖那两个洞吗 —— 能，两边都覆盖，但覆盖是靠「几何与 uv 同时被写」这条性质。**
- 洞①（子块已被 `ReapplySoftEdges` 的 `SoftEdgeClear` 销毁、这条又不再建 ⇒ 只剩主格、缺的部分静默不画）：
  重切进这条路时宿主的矩形是**上一刀的主格**（`ReapplySoftEdges:384` 的 `cellNew` 就是它），
  而 `vis` 是本趟算出来的并集；只要上一刀切过，`vis ⊋ 主格` 在映射后仍然严格成立
  （`ReapplySoftEdges:387-390` 是「以主格为锚点」的仿射映射，差值按 `sx/sy` 缩放，`sx=0` 只会出现在几何退化那条路上、那条路走 `ClipRect` 失败分支），
  ⇒ `!SameRectNear(...)` 为真 ⇒ `PlaceCell` 把宿主摆回**整个 `vis`** ⇒ 洞①补上。
- 洞②（宿主 uv 停在上一次那份）：`PlaceCell(q, vis, vis, uv0)` 里 `cell == vis` ⇒ 算出来的 `uv` **恒等于 `uv0`**
  （`MenuDraw.cs:485-488`：`uv0.x + uv0.width*(vis.x1-vis.x1)/vis.W = uv0.x`，宽高同理）⇒ 洞②补上。
- 宿主「矩形 ↔ uv」是**同一次 `PlaceCell` 一起写的**（`:475-488` 一函数内），所以「矩形 ≠ vis」当且仅当「uv ≠ uv0」
  ⇒ 拿**矩形**当判据是对的，不是巧合。容差 0.05px 漏判的那一档：缺口最宽 0.05px ⇒ 看不见，可接受。

**(b) F 的论证「首次切时两个条件都不成立 ⇒ 首次路径可证明零行为变化」—— 成立，但**承重的是条件①**。**
- 条件①：`SoftEdgeRebuild` 只由 `ArmSoftRebuild`（`MenuDraw.cs:452-457`）写；全工程 grep 之后，
  `ArmSoftRebuild` 只有 3 个调用点 —— `ApplySoftEdges` 的两个分支（`:312`/`:359`）与 `ReapplySoftEdges` 的裁没分支（`:398`）。
  三个都在**本次 `ApplySoftEdges` 走完之后**才写 ⇒ 一个 quad **第一次**进 `ApplySoftEdges` 时它**一定**是 `null`。
  三个首次入口（`MenuDraw.Rect:725` · `Nine:773` · `Tiled:904`）都成立。
- 条件②：`Rect` 的 `vis` 就是建这个 quad 时那组数（`:713-716` 同一份 `x1..y2`），`Nine`/`Tiled` 的 `vis` 干脆就是
  `QuadRectPx(q2)`（同一个函数）⇒ 首次也为**假**（真）。
- ⚠️ 但**短路靠的是条件①**：就算条件②哪天因浮点判成真，`if` 也不会进。F 括号里那句「且与浮点往返无关」是对的。

**(c) 会不会引入新行为（本来不该切的时候切了）—— 不会。** 两个条件同时成立只有一条路：
先前被 `ArmSoftRebuild` 挂过回调（= 重切路径），或「对同一颗 quad 从外部第二次调 `ApplySoftEdges`」——
后者全工程不存在（只有 3 个调用点，都在建对象时各走一次；`Nine`/`Tiled` 的 `GetComponentsInChildren` 数组在遍历前已取好，
新建的软边子块不会被卷进同一趟循环）。而且这条路**不建子块**（本来就不需要切），只是把宿主摆回去，语义与「整块不切」一致。
递归风险也没有：`PlaceCell → SetAspect/SetWorldHeight → NotifySoftEdgeChanged` 在重切路径上被 `ImageQuad._softBusy` 挡住（`ImageQuad.cs:95-100`）。

**(d) 若判越界、回退它的代价 —— F 那句「不变量要改成只在切开分支跑」方向是对的，但**当前是理论代价**。**
- 保留 F1 的 `uv0` 显式入参、去掉这一句，同时保留 `:313` 的 `CheckSoftEdgeUv(q, uv0, "整块不切")`：
  重切落进「不切」分支时 `want = area(uv0)`（第一次那份）而 `got = area(上一刀的主格 uv)` ⇒ **计数会 +1**
  ⇒ `Editor/ShellScene.cs:624` 的 `Check(MenuDraw.SoftEdgeUvDrifts, 0, …)` 会红 —— 所以「要么留这一句、要么把不变量挪出这个分支」这个耦合是真的。
- ⚠️ 但**今天没有任何自检能走到那一步**：`ShellScene` 那组探针（`:593-626`）的重切**落在切开分支**（算过：
  框 200×200、带 25、`SetWorldHeight(Px(300))` ⇒ 映射后 `visNew=(0,−100,200,300)`、裁回 `(0,0,200,200)` ⇒ 断点 25/175 两个都严格在块内 ⇒ `nx=ny=2`）。
  ⇒ 回退它**不会让今天的自检变红**（只会把那颗雷留在地里）。这同时也说明：**这一句目前零覆盖**（见 R2/R7）。

---

## 一、逐条发现

**R1 · 错 · F7 的「全工程 12 个『压暗层命中区』站点」是**漏数**（实测 ≥ **16**）—— 漏掉的是一整族叫 `BackdropHit` 的站点**
证据：`MenuDraw.cs:1041`（「`grep` 实测 **12 个站点** … **全工程 12 个「压暗层命中区」站点** … 以实测为准」）
vs 我独立 grep（判据 = 每站都满足「整屏压暗层 + 点它关窗/取消」）：
· `Shell/PracticeModePopup.cs:411`（`Menu Dark Background`，`QPr`=3100）+ `:412` `HitOn(root, root, "BackdropHit", …, QPrHit - 1)`（=3102）——
**这就是 `CardDetailPopup.cs:265` 的 `QCdHit − 1` 同一个写法**，按 F 自己的分类它该进「待收口」；
· `Shell/RankedEventWindow.cs:58`（`QBg`=3104）+ `:59` `BackdropHit … QHitBackdrop`（`LiveOpsEventWindow.cs:67` = 3115）；
· `Shell/SkirmishEventWindow.cs:68` + `:70`（同上，同一个常量）；
· `Shell/SearchingMatchPopup.cs:163` + `:165` `BackdropHit … QSrHitBackdrop`（`:32` = 3134，内容档 `QSrHit`=3135）。
⇒ 真实分区应是 **1 已收口 + 2 编码相符 + 13 待收口（9 + 4）= 16**。
另：`Shell/LiveOpsEventWindow.cs:64-66` 那条注释**自己就点名了** `PracticeModePopup` 里 `QPrHit - 1` 这个写法 —— 抄它一眼就能补上，说明这不是「口径不同」而是**漏看**。
该照：铁律 6（数字只留一处且要是对的）+ 铁律 5（就地改）；且 `资料/待办判据_阶段二与联机.md:442/453` 仍写「全工程 **7 处**」、`:466` 写「还剩 **9 处**」
—— **现在 7 / 9 / 12 三个数分处两份文档，比原来更乱**（F 不许改正本、只能回报，这一点 F 做对了，但数字本身要给对）。

**R2 · 错（断言无效，本批新增）· `SoftEdgeUvDrifts` 抓不到它自称要抓的那个缺陷 —— 它是**同义反复**：期望值与被测对象用的是同一个 `uv0`**
证据：`MenuDraw.cs:419-420`（「**它真能红**：把 `uv0` 换回 `q.UvRect`（F1 那个真缺陷）⇒ 重切一次立刻偏离 **每次 × 主格占比**…偏差 25%」）
+ `Editor/ShellScene.cs:618-625`（「把 `uv0` 换回 `q.UvRect` 这里立刻红」）；
实现：`CheckSoftEdgeUv(host, uv0, tag)` 比的是「树里所有 `ImageQuad` 的 uv 面积和」与 `area(uv0)`（`:424-441`），
而**每一块 uv 都是 `PlaceCell` 从同一个 `uv0` 里按 `cell/vis` 切出来的**（`:485-488`）⇒ 逐块面积**望远镜求和恒等于** `area(uv0)`。
逐路径核过：首次切（宿主 uv 本来就是 `uv0`）、切开分支、以及**把 `uv0` 换成 `q.UvRect` 的那一版**（树仍然铺满那个缩过的 `uv0`，`want` 也跟着缩 ⇒ 差值 0、`outside` 也不触发）
—— **四种情形计数都不动**。它**唯一**会响的是「宿主 uv 不是本趟写的」那一档（= 洞②，也就是 F 用 `:310` 补掉的那处）。
⇒ `ShellScene.cs:624` 那条 ★ 断言对本批的 F1 **不成立**（拿掉/改回 `uv0` 它照样绿），属「空转断言」族（同 W6-F10 记的那一类）。
该照：铁律 12（断言要能真红）+ `资料/已知的坑.md`；**能真红的写法（建议，我没改）**：
在**探针**里把「出生时的 uv」单独存一份再比 —— `var uv0AtBirth = softQ.UvRect;`（`Rect` 返回时 = `(0,0,1,1)`），
重切之后断「所有块的 uv 面积和 == `area(uv0AtBirth)`」（而不是拿 `MenuDraw` 自己的 `uv0`）：改回 `q.UvRect` 会得 `0.5625 vs 1.0` ⇒ 立刻红；
或在 `ArmSoftRebuild` 里把**第一次**那份 `uv0` 存到 `ImageQuad` 上、由 `CheckSoftEdgeUv` 拿它当期望值。

**R3 · 错（文档/自检契约没兑现）· `PaddedHitDegenerates` 全工程**无读者**，注释却写「**自检断它 == 0**」**
证据：`MenuDraw.cs:204-206`（声明 + 「自检断它 == 0」）· `:193-194`（只有自增与 `<= 3` 的自用）；
`grep -rn "PaddedHitDegenerates" MyGame/Assets` = **只有 MenuDraw.cs 自己三行**，`Editor/*Scene.cs` 一处都没有；
`Editor/ShellScene.cs:558-584` 的 ⑤·b 段只测了「非零 pad 正常收/放」+ 端到端转发，**没有退化用例**。
⇒ 这一条守卫**没有断言看着它**（把它整段删掉，11 条自检一条都不会红）。
附带事实：`ClipPad` 全工程仍是 **0 处赋值**（`MenuWindowBase.cs:129` 声明、`:280` 转发）⇒ 生产路径上这个守卫根本不可达，只有自检能碰。
该照：铁律 5（别把「打算做」写成「已经做」）+ 铁律 12；补法只有一行：`ShellScene` ⑤·b 里加 `MenuDraw.PaddedHitRect(小矩形, 大 pad)` + `Check(MenuDraw.PaddedHitDegenerates, 0, …)`，
并在用完把它复位（它是 `static` 累加器）。

**R4 · 对 · F9 的退化守卫在**正常矩形上不改行为**（`||` 那一支只在真退化时进）**
证据：`MenuDraw.cs:179`（`pad == Vector4.zero` 早退 ⇒ `hitPad` 全 0 时与改前**同一个对象**返回）+ `:191`（`o.W/o.H ≤ 0.01` 才回退）
+ `Core/UguiRect.cs:31-32`（`W = x2 - x1`、`H = y2 - y1`，**有符号** ⇒ 反向矩形会被判出，不会漏兜）。
正 padding 的正常件：`o.W > 0.01` ⇒ 直接 `return o`（`:201`），与改前逐字节等价。
负 padding（扩大）只会让 `o` 更大 ⇒ 更不可能退化。⚠️ 这一条**只在 `hitPad/clip` 非零时才有意义**，而当前 0 处生产赋值（见 R3）。
该照：CLAUDE.md §三「收口不许顺手改行为」。

**R5 · 错（低，文字）· `MenuDraw.cs:188` 的「（两轴都退化才兜）」与代码相反 —— 代码是 `||`（一轴退化就兜）**
证据：`:188`（注释）vs `:191`（`if (o.W <= 0.01f || o.H <= 0.01f)`）。按注释读会以为单轴退化不兜 ⇒ 下一个人可能「顺手改成 `&&`」把真缺口打开。
该照：铁律 5（就地改一句）。

**R6 · 存疑（低）· 「误差在 1e-5 px 量级」没实测，量级应是 ~1e-4 px（结论不受影响）**
证据：`MenuDraw.cs:92-94`（容差 0.05px 的理由）；`QuadRectPx`（`:255-261`）走 `LayoutSpace.ToPixel(transform.position)`
—— 世界坐标 ~10、float32 相对精度 ~1e-7 ⇒ px 上 ~1e-4；再叠父链几跳。0.05px 仍留了 ~500× 余量 ⇒ 容差选得对，只是**理由里的数字**偏乐观。
该照：铁律 3（别把推断写成实读）。

**R7 · 存疑（覆盖缺口）· `:310` 这一句目前**零覆盖**—— 唯一会走到重切的自检落在切开分支**
证据：`Editor/ShellScene.cs:606-610`（`SetWorldHeight(Px(300))` ⇒ 重切 ⇒ 断「子块数 > 0」）—— 我按 `SoftCuts`/`ReapplySoftEdges` 逐行走了一遍：
映射后的 `visNew = (0,−100,200,300)`、`ClipRect` 裁回 `(0,0,200,200)` ⇒ 断点 25/175 仍严格在块内 ⇒ `nx=ny=2` ⇒ **切开分支**。
`Editor/RewardsScene.cs:1784-1893` 那三段（`ClipSoftness` (0,25)/(42,0)）**全程不改几何** ⇒ 一次重切都不发生；
生产侧也没有「建完之后改软边 quad 几何」的调用点（`MenuScroll` 只挪节点：`Shell/MenuScroll.cs` 里没有任何 `SetWorldHeight/SetAspect`）。
⇒ 「不切」分支里新增的 `PlaceCell` 与 `CheckSoftEdgeUv` 两件事，**当前只能靠人工推演**（本报告 (a) 那一段）。
该照：铁律 12（这一批要不要补一条，请调度台定；补法：探针里先切 3×3，再把框改到「带宽 > 可见宽」逼它落进不切分支）。

**R8 · 对 · F1 的 `uv0` 显式入参：3 个调用点全对、且**编译器兜底**（形参无默认值 ⇒ 漏传编不过）**
证据：签名 `MenuDraw.cs:287`（`…, Vector2 softPx, Rect uv0)` 无默认值）；调用点 `:725`（`Rect`，传 `uv` = `:698-712` 算出来的「整张图 ∩ 裁切框」那一份）
· `:773`（`Nine`，传 `q2.UvRect` = 该子块自己的整张 uv，且 `vis = QuadRectPx(q2)` 与它同源）· `:904`（`Tiled`，同）。
语义与 F 写在 `:279-286` 的注释一致；`ReapplySoftEdges:375-377` 也确实**只**从存下来的那份推导（不再读 `q.UvRect`）。
该照：W6-F1 的「该照」那一行。

**R9 · 对 · F11 删 `SoftEdgeForget`：确无调用点、确无别处靠它清理、「悬挂条目不会出事」也成立**
证据：`git grep -n "SoftEdgeForget" HEAD -- '*.cs'` 与工作区 grep **都只有注释那一行**（`ImageQuad.cs:68`）；
全工程**唯一**销毁软边子块的地方是 `SoftEdgeClear`（`ImageQuad.cs:79-91`，由 `ReapplySoftEdges:382` 调）；
按名字找子块的两处（`Editor/CollectionScene.cs:194` · `Editor/RewardsScene.cs:312`）都是**只读**统计，不删。
「假 null 兜住」成立：`SetTint` 循环（`:173-174`）与 `SoftEdgeClear`（`:84`）都有 `if (k == null)`。
⚠️ 唯一副作用（**现在无路径产生**）：`SoftEdgeKidCount`（`:67`）数的是 `_softKids.Count`，含 null 条目 ⇒ 真有悬挂时 `ShellScene.cs:600/611` 那两条会误红。
该照：W6-F11。

**R10 · 对 · F2 重写的「逐处实读表」我独立重解析那份表核过 —— 值—路径配对 **10/10 族**、计数逐条吻合**（⚠️ **2026-10-05 更正**：本行原写「重解析**全量表**」—— 标签错：那份表只有**菜单族 3 包 = 156**，**全库真值 222**；下面 `:135` 的 `150 + 1 + 5 = 156` **本身是对的、不改**）
证据（我自己跑的解析，不引用 F 的说法）：`d:/4/_tmp_view/q1_rm2d.txt` 三个表头 **150 + 1 + 5 = 156** 个 mask；
非零 **61** 条、**10** 族，逐族与 `MenuDraw.cs:1135-1153` 的清单**逐条相同**：
`(−8,−5,−8,−5)×38`（我另核：**38/38 全部以 `/Text Area` 结尾**，无一是 `Viewport`/`Scroll Rect`）·
`(0,0,−500,0)×6`（进度条族）· `(0,9.69,0,9.69)×5`（全是 `Searching Oponent Popup/Window` 那 5 份）·
`(0,0,0,−10)×3`（Draft Mode ×2 + `Player Profile Window/…/Ranking Tab/AllFactions/scroll rect/viewport`）·
`(10,0,0,0)×3`（`Rewards Base Submenu Variant/…/Forge Tab/…` · `Raid Progress Tab/…` · `Forge Tab/…`）·
`(0,−15,0,0)×2`（两处 `Dynamic Content`）· `(2.49…)×1` · `(−26,0,−26,0)×1` · `(−25,0,0,0)×1` · `(83,0,0,0)×1`。
「本壳那 8 处滚动区全是 `(0,0,0,0)`」也**逐条查过 zero 组**：`…/Packs Scroll View/Viewport`（商店 9 处）、
`…/Select Deck Tab/…/Deck Scroll View/Viewport`、`Chat Tab/Viewport`、`…/Trophies Tab/Scroll/Viewport`、
`…/AllianceMemberVariant/TrophiesWindow/Scroll Rect`、`Reward Window/…`、排行榜三处、`Deck Editing Menu/…`、
`Generic Shop Tab/Packs Scroll View/Viewport` —— **全对**。
该照：铁律 5（原注释确实是「照看着像的填」）；这一条 F 改对了。

**R11 · 存疑（低，口径）· 「本壳真正非零的只有锻造轨道那一族」——对**已建的窗口**成立，但表里还有 3 条主菜单/事件容器的非零项没交代**
证据：`MenuDraw.cs:1151-1154`；表里另有 `Main Menu Offer Container Static Image 1x2` = `(2.49,2.49,2.49,2.49)`、
`Main Menu Offer Container Carousel 1x1/Dynamic Content` 与 `Reward Event Container 1x1/Dynamic Content` = `(0,−15,0,0)`。
我 grep 过：我们**还没建**主菜单 offer 条（`Editor/MainMenuScene.cs` / `Shell/MainMenuRuntime.cs` 里没有 Carousel/offer 条实现；
`Shell/OfferContainer.cs` 是**商店**的 19 个变体），`Shell/InboxWindow.cs:98` 的 `Message Display` 也只是个空节点（没有 `Scroll View/Viewport`）
⇒ 就**当前建出来的东西**而言 F 的判断成立。建议措辞收紧成「**我们已建的那几扇窗里**只有锻造那条非零」，免得下个会话以为表已经穷尽。
该照：铁律 3（把「我们没建」与「原版没有」分开写）。

**R12 · 对 · F13 的理由订正方向正确（原注释「不补就把填充挪到框前面」确实反了）**
证据：`Battle/WaitBanner.cs:33`（`Z = −0.45f`）· `:35`（`U = px/108`）· `:130`（「z 越负越靠前」）· `:146`（`_fillRoot` local z = +0.01 ⇒ 世界 −0.44）
⇒ **不设** `:146` 的 `+0.01` 时 `_fillRoot` 的 local z 是 0 ⇒ 世界 z = **−0.45 = 与框同层**（`MenuDraw.cs:25-26` 的 `Local` 只用在 `fillGo` 那一层，而 `:150` 又把它压回 0）
—— 与 F 写的「不补的话它落在和框同一层 z 上 ⇒ 同队列同距离，谁先画由排序/枚举决定」**一致**；
「0 是更靠后、不是更前面」也对（`MenuDraw.Tiled` 若不覆盖会给 `fillGo` 的 local z = +0.45 ⇒ 世界 0 ⇒ 更靠后）。
⚠️ 同一段里两个「不补」指**两个不同的变量**（① `_fillRoot` 的 `+0.01`；② `fillGo.localPosition = Vector3.zero` 那次覆盖），
第一种落到「同层」、第二种落到 z = 0 —— 两句各自都对，连读容易混，建议各写各的主语。
该照：铁律 5。

**R13 · 对 · `MenuDraw.Tiled` 收口后 `BarFillPieces` 的语义没变（新旧都 = 1，断言不受影响）**
证据：`Battle/WaitBanner.cs:86`（`_fillRoot.transform.childCount`）vs `ImageQuad.cs:385-410`（`CreateTiled` 先建 root、tile 挂 root 下）
—— 新旧写法都是「root 一层 + tile 一层」⇒ 该属性新旧都是 1；`Editor/BattleScene.cs:1815` 断的是 `>= 1` ⇒ 不红。
（`:85` 那句「原版是 11×1」是**旧的**注释口径，与这个属性不等价 —— 不是本批引入的，但顺手值得记一笔。）
该照：铁律 12（断言要量到真东西）。

**R14 · 对（低）· `BoosterInfoPopup.cs:75-78` 的订正内容正确，但引用的行号 `:1068` 实际是 **`:1070`****
证据：`Shell/BoosterInfoPopup.cs:75`（「`Shell/MenuDraw.cs`，本批时在 `:1068`」）vs `MenuDraw.cs:1070`（`public static Transform ShadeHit(`）。
同批写的引用就偏了 2 行（`ShadeRuleOk` 在 `:1089`）。订正的三条实质内容都对：公共件确实已做（`:1070`）、
本件编码确实是 `QShadeHit = QShade`（`BoosterInfoPopup.cs:79`）、有 `qShade >= qContentMin` 告警（`:1073-1078`）+ 自检模板（`:1089`）。
该照：铁律 6（引用行号也会过期，能写名字就别写行号）。

**R15 · 对 · 行尾 / 规模 / 越界**
证据：四个文件**二进制数过**（`b.count(b'\r\n')`）：`MenuDraw.cs`（1333 行）· `ImageQuad.cs`（421）· `WaitBanner.cs`（168）· `BoosterInfoPopup.cs`（507）
**crlf 全 0**（没翻行尾）；`git diff --numstat` = 474/32 · 70/0 · 33/2 · 4/2，**远小于行数** ⇒ 没有整篇重写。
新符号的引用面也没出这 4 个文件：`SameRectNear` / `PaddedHitDegenerates` / `SoftEdgeForget` 在 `Editor/*Scene.cs` 里
只有 `ShadeRuleOk` / `SoftEdgeUvDrifts` / `SoftEdgeRebuilds` / `SoftEdgeKidCount` 这几个**上一批就有的**读者。
（工作区另有 15 个 `.cs` 是别的写手在飞的活 —— 归谁无法从 git 判断，此条只作说明。）
该照：CLAUDE.md §二 的行尾纪律 + 铁律 13·3。

**R16 · 对 · 新加的计数/告警**真的会出声**（除 R3 那一颗外）**
证据：`SoftEdgeRebuilds` 有读者（`Editor/ShellScene.cs:606-609`）· `SoftEdgeUvDrifts` 有读者（`:624`，期望值是**字面量 0**）+ 真 `Debug.LogWarning`（`MenuDraw.cs:443-449`）·
`TextClipReapplied` 有读者（`:652-655`）· `TextClipUnavailable` 有读者（`Editor/RewardsScene.cs:1962`）· `SoftEdgeKidCount` 有读者（`:600/610/611`）。
⇒ 唯一「只加了个字段」的是 `PaddedHitDegenerates`（R3）。
该照：红线「不许静默失败」。

---

## 二、确认干净的（逐条列核过什么）

1. **`:310` 的 `SameRectNear` 容差**：`PxRect` 是有符号差值（`Core/UguiRect.cs:31-32`），比较的是「世界→px 反推」与「调用方给的 px」，
   两处都只能差浮点 ⇒ 0.05px 足够（R6 只纠数字量级）。
2. **`:310` 不会在无回调时误动**：`SoftEdgeRebuild` 的唯一写手是 `ArmSoftRebuild`（`:456`），三个调用点都在 `ApplySoftEdges`/`ReapplySoftEdges` 尾部；
   三个首次入口都在建对象时各走一次 ⇒ 首次条件恒假（(b) 那一节）。
3. **`ApplySoftEdges` 的调用点一个不漏**：`grep -rn "ApplySoftEdges\|ReapplySoftEdges"` 全工程 = `:725`/`:773`/`:904` 三处 + `:402` 递归；
   形参 `uv0` 无默认值 ⇒ 以后漏传会**编不过**（不是运行期静默）。
4. **`ReapplySoftEdges` 的顺序与防重入**：`SoftEdgeClear` → `RestoreCorners`（还原**上斜坡之前**的四角色）→ `ClipRect` → `ApplySoftEdges`；
   `CaptureCorners` 在 `RestoreCorners` **之后**取（`:294` 在递归入口，`:383` 先还原）⇒ alpha **不会**越切越暗；递归由 `ImageQuad._softBusy` 挡（`:95-100`）。
5. **`CheckSoftEdgeUv` 在九宫格/平铺下不会误报**：`Nine`/`Tiled` 的 root 是**裸 `GameObject`**（`ImageQuad.cs:313-315`、`:388-390`），不是 `ImageQuad`
   ⇒ `host.GetComponentsInChildren<ImageQuad>(true)`（`:430`）只会拿到「这一块自己 + 它的软边子块」，不会把兄弟块算进来；
   `PlaceCell` 的 uv 是「按 `cell/vis` 分片」，分片求和恒等于 `uv0` 的面积 ⇒ 合法情形不触发（但见 R2：这条「恒等」也正是它抓不到 F1 的原因）。
6. **`uv0` 在三条路上的语义**：`Rect` = 整张图 ∩ 裁切框（`:698-712`）；`Nine`/`Tiled` = 子块建好那一刻自己的 `UvRect`（与 `vis = QuadRectPx(q2)` 同源）
   —— 与 F 写在 `:279-286`/`:771-772`/`:903` 的口径一致，**没有一处错配**（`Nine` 的 `q2.UvRect` 读在 `ClipNineChildren` 之后，两边同源）。
7. **`RestoreCorners(null)` 的影响面**：把「从没设过顶点色」变成「显式四白」（`:470`）。核过读者：`RebuildMesh` 用 `_cornerColors ?? WhiteVerts`（`ImageQuad.cs:247`）⇒ 值一样；
   `Editor/RewardsScene.cs:1791/1826` 那两条 `CornerColors == null` 断言量的是**没重切过的**quad（那三段探针全程不改几何）⇒ 不受影响。
8. **F9 的符号无关性**：`o.W/o.H <= 0.01` 只看算术结果，不看正负（`||` 一轴即兜）⇒ 换符号约定也仍然成立（R5 只纠「两轴」这个措辞）。
9. **`PaddedHitRect` 的四边对应**：`pad.x→x1`、`pad.w→y1`、`pad.z→x2`、`pad.y→y2`，与 UGUI 的 (X=Left,Y=Bottom,Z=Right,W=Top) + `PxRect` 的 y 向下自洽（`:176-180`）。
10. **F2 的局部结论「那 1 个全正的反例不能吞掉」**：`MenuDraw.cs:1122-1127` 已按 207/208 写；那 1 条正 padding 我独立复算过（`bundle_menus_assets_all` 里 `m_RaycastPadding` 的分布与 W6-F4 同）—— 与本批无关，不重复。
11. **`SoftEdgeForget` 的删除没有留下「只删不摘」的坑**：`SoftEdgeRegister` 有 `_softKids.Contains` 去重（`ImageQuad.cs:63`）、`SoftEdgeClear` 整表清空 ⇒ 登记/注销仍然成对（唯一不成对的是「宿主被 `ClearChildren` 整段删」那条，宿主一起没了）。
12. **WaitBanner 的两处等价**：队列 `3000`（`MenuDraw.Tiled:896` 显式写）= 旧写法「一次都没设」的默认档；`fillGo.localPosition = Vector3.zero`（`:150`）= 旧写法 `center = Vector3.zero`（`ImageQuad.cs:390`）。
13. **`BoosterInfoPopup` 那段订正与 `MenuDraw.ShadeHit` 的实际行为对得上**（告警条件、模板名、本件编码）—— R14 只纠行号。
14. **`grep -rn "MenuDraw.ShadeHit"` 确实只有 `ImportDeckPopup.cs:103` 一个调用点**（其余命中都是注释）⇒ 那句自述属实。
15. **`ClipNineChildren` / `ClipTiledChildren` 的调用点只有 `Nine:763` / `Tiled:898` 两处**，都跑在 `ApplySoftEdges` 之前 ⇒ W6-F15「九宫格/平铺挂的回调不触发」仍然成立。

---

## 三、我没核到的（为什么）

1. **实跑自检**（`ShellScene.Run` / `RewardsScene.Run` / 那 11 条）—— 红线禁止跑 Unity。所有「会红/不会红」都是**逐行读实现推演**出来的（R2/R7 的数值我能手算，但没有实跑数据）。
2. **`ShellScene.cs:624` 那条断言的实跑表现**：我只能证明「把 `uv0` 改回 `q.UvRect` 在**当前的探针路径**上不会让它变红」（探针的两次切都落在切开分支、且 `want` 跟着 `uv0` 一起变），
   **没有实跑**去反证（跑不了）。若调度台要钉死这一条，建议按 R2 的「探针另存出生时 uv」写法补一行再跑一次。
3. **生产侧「建完软边 quad 之后还改它几何」的调用点**：我核了 `MenuScroll`（不写几何）、`ClipNine/TiledChildren`（跑在软边之前）与三处软边探针，
   **没有**把 `MenuWindowBase.Rect/Nine` 的**全部**调用点逐个追一遍（那要跨 6 个正在被别人改的文件）⇒ 「重切在生产上到底带不带电」我只给了**结构性**结论（回调只在 `SetAspect`/`SetWorldHeight` 上触发）。
4. **`ClipPad` 该由谁赋值**（原版锻造轨道那族 `(10,0,0,0)`）：属接线批的活，不在本次审查范围；我只核到「全工程 0 处赋值」这个事实（R3）。
5. **`PaddedHitRect` 的正负号约定**（正 = 缩小 / 负 = 扩大）：与 W6-F4 一样，**引擎侧判据本地拿不到**（`il2cpp_out/dump.cs` 里 `PointInRectangle` 方法体空、`UnityPlayer.dll` 无符号）⇒ 仍是 `[TODO-verify]`，F 也如实标了。
6. **那 4 个 `BackdropHit` 站点各自的档位是否「合规矩」**：我只核到它们**存在且是压暗层命中区**（R1 的计数结论不受影响），没有把每个窗的「压暗档 / 内容命中区最低档」逐个算一遍（那是接线批的逐处表要干的事）。
