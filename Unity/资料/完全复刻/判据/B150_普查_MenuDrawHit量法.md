# B150 · 普查：`MenuDraw.Hit` 命中区「拿节点量位置/尺寸」有多少处会算错（2026-11-06）

> 只读普查代理。**未改任何 `.cs` / 账 / 正本**；未跑 Unity、未跑 typecheck、未动 git。
> 本文件是**证据/口径**，不含「已完成/未完成」状态（状态看 `资料/完全复刻/活状态表.md`）。

## 〇 · 结论一句话

全仓 `Assets/CardPresentation/Editor/**` 里「拿**命中区**（节点或它的 quad）做几何读数」的断言站点共 **19 个代码块**：
**「一定算错」= 0 处**（唯一一处真错 `B141_W3_Assert` 已由 2026-11-05 的 B150 修掉，本次复核确认修法与判据都对）·
**「取决于写法」= 6 处**（今天读数是对的，但只因为「对应建件器把节点摆在了矩形中心」或「读口恰好会下沉到 quad」；
换建件器或换读口就**静默错**）· **「安全」= 13 处**（量 quad / 只量 `sizeDelta` / 走 `PointerLayer` 生产口）。

## 一 · 判据根：命中区节点的两种摆放约定（这是整件事的根）

| 建件器 | 节点 `localPosition` | 真矩形长在哪 | 出处 |
|---|---|---|---|
| `MenuDraw.Hit`（含所有转发：`GameWindow.AddHit` / `CampaignTab.AddHit` / `ForgeTab.AddHit` / `MainMenuSubmenuWindow` 那一族） | **父原点（恒 0）**，只写 `sizeDelta` | 子件那颗 `ImageQuad`（名字也叫 `Hit`）摆在矩形中心 | `Shell/MenuDraw.cs:2247-2255`（`SetPxSize(hit, …)` 后 `MakeHitQuad(hit, hr, q, Local(...))`）· `Shell/GameWindow.cs:326-336` |
| `MenuDraw.ShadeHit` / `MenuDraw.Absorb` | 同上（都转调 `Hit`） | 同上 | `Shell/MenuDraw.cs:2417` / `:2504` |
| `MenuDraw.DeckCell` | **矩形中心**（走 `Node(...)` → `ApplyPxRect`） | quad 摆 0 | `Shell/MenuDraw.cs:3575-3576`（注释逐字写着「节点已在矩形中心 ⇒ quad 摆 0」） |
| `DeckInfoPopup.Hit` | **矩形中心** | quad 摆 0 | `Shell/DeckInfoPopup.cs:2956` |
| `PracticeModePopup.HitOn` | **矩形中心** | quad 摆 0 | `Shell/PracticeModePopup.cs:3458` |
| `ImportDeckPopup.HitOn` | **矩形中心** | quad 摆 0 | `Shell/ImportDeckPopup.cs:566` |
| `CampaignRewardWindow` 手搓的 `Hit` | **父原点**（`SetParent(btn, false)` 后不摆位） | `DrawRect(hit, …, r, …)` | `Shell/CampaignRewardWindow.cs:814-819` |

⇒ **同一个名字 `Hit`，七种建法里只有四种把节点摆在矩形中心**（`DeckCell` / `DeckInfoPopup.Hit` / `PracticeModePopup.HitOn` / `ImportDeckPopup.HitOn`）；其余三档（`MenuDraw.Hit` 及其全部转发、`ShadeHit`/`Absorb`、`CampaignRewardWindow` 手搓那颗）**恒在父原点**。判「量节点行不行」之前，先确认它是哪一档建的。

### 读口的三种中心语义（决定「拿节点」还是「拿 quad」）

- `MenuCheck.DesignPx(t)` / `MainMenuScene.CheckAt` / `CollectionScene.CheckAtPx` / `RewardsScene.CheckAt` / `ShellScene.NodeRectPx` / `ShellScene.GNodeRect` / `MainMenuScene.RenderedRect`：
  中心 = **传入节点自身**的位置 ⇒ 对 `MenuDraw.Hit` 节点**是错的**（拿到父原点）。
  出处：`Editor/MenuCheck.cs:161-163` · `Editor/MainMenuScene.cs:164-184,337-341` · `Editor/CollectionScene.cs:85-114` ·
  `Editor/RewardsScene.cs:63-71` · `Editor/ShellScene.cs:5983-5995` · `Editor/ShellScene.cs:6928-6940`。
- `MenuCheck.NodeRectPx(t, RectCenterMode.LabelNode, …)`（= `CollectionScene`/`RewardsScene`/`ShopScene` 的 `RectOf`）：
  中心下沉到子树里的 `Label` 或**第一颗激活的 `ImageQuad`** ⇒ 对命中区节点**碰巧对**（拿的是命中 quad）。
  出处：`Editor/MenuCheck.cs:269-317`（`:278` 取 Label、`:308` 取 Quad，`:313` 才是 `DesignPx(node)`）。
- `MenuDraw.QuadRectPx` / `UnionQuadRectPx` / `PointerLayer.HitBoxForTest` / `ButtonAt`：**只认 quad** ⇒ 永远对。
  出处：`Shell/MenuDraw.cs:781-800` · `:831-880`。

## 二 · 逐处表

> 期望值来源一律是「原版 prefab 字段实读 / 几何复算的字面量」——本次普查不评价期望值，只评价**量法**。
> 「分级」：🔴 一定算错 / 🟠 取决于写法 / ✅ 安全。

| # | 文件:行 | 量的是什么 | 用哪个属性 | 为什么可疑 | 分级 | 正确量法 |
|---|---|---|---|---|---|---|
| 1 | `Editor/CollectionScene.cs:2554` 取节点，`:2558/:2560/:2562/:2564` 断言 | 压在视口下边那一格的命中区**中心**（x 与 y 各一条 + 与格心的差） | `MenuCheck.DesignPx(eHit)`（eHit = `FindChild(edge, "Hit")`，`edge` 来自 `win.DeckCells`） | 中心取的是**节点位置**。今天对，只因为 `DeckCell` 把节点摆在矩形中心；若哪天 `DeckCell` 改成 `MenuDraw.Hit` 那种「节点在父原点」写法 ⇒ 读数变「格心」，三条断言同时静默错 | 🟠 | 二选一：① 保留（`DeckCell` 的约定已在 `.cs:3575-3576` 写明）并在断言旁写明「本行依赖 DeckCell 把节点摆在中心」；② 改量 `FindChild(eHit,"Hit")` 那颗 quad 的 `MenuDraw.QuadRectPx` 中心 |
| 2 | `Editor/CollectionScene.cs:2887` | `Deck Options/Opt_*` 命中区**节点**在原版射线区中心 | `CheckAtPx` → `MenuCheck.DesignPx(t)` | 同上（中心取节点）。今天对，只因为 `DeckInfoPopup.Hit` 摆了节点（`:2956`） | 🟠 | 同 1；该文件既有注释已经用「节点没摆（停在容器 (0,0)）⇒ 差几百 px ⇒ 红」当改坏法，可保留，但要写清依赖 |
| 3 | `Editor/CollectionScene.cs:3540` | 关闭钮命中区**节点**中心 | `CheckAtPx` → `DesignPx` | 同 2（同一档建件器）。它有意用 2D（z 不进比较）是对的，但 x/y 仍来自节点位置 | 🟠 | 同 1 |
| 4 | `Editor/B103F6_Assert.cs:190` 取 `Hit`，`:193` 量矩形，`:64-76` 是量法 | `DeckCell` 的 `Hit` 矩形（左/右/下沿 == 格子沿） | `NodeRect`：`rt.sizeDelta × 108` + `MenuCheck.DesignPx(t)` | 中心取节点。今天对，只因为 `DeckCell` 摆位 | 🟠 | 同 1（或换成 `MenuDraw.QuadRectPx`） |
| 5 | `Editor/ShellScene.cs:6154` 取 `Hit`，`:6169` 量、`:6207` 再量（态三） | 练习窗阵营格命中区**左右沿**（宽 82 / 切窄后 41） | 局部 `NodeRectPx`（`ShellScene.cs:5983-5995`，中心 = `DesignPx(t)`、尺寸 = `rt.rect`） | 中心取节点。今天对，只因为 `PracticeModePopup.HitOn` 摆了节点（`:3458`）；且只比 x 两沿，y 不参与 | 🟠 | 同 1；或改用同文件已有的 `GHitRect`（`:6949`，量 quad） |
| 6 | `Editor/ShellScene.cs:6253` 取 `RowHit`，`:6266` 量、`:6310` 再量 | 练习窗卡组行命中区**左右沿** | 同 5 | 同 5 | 🟠 | 同 5 |
| 7 | `Editor/B141_W3_Assert.cs:201/:208` | 阵营格命中区**宽/高**（142 / 132 的判定） | `hrt.sizeDelta.x/y ÷ unit` | 只量尺寸 ⇒ **安全**（`MenuDraw.Hit` 写 `sizeDelta` 就是命中矩形大小，`MenuDraw.cs:2254`）；这条与 B150 那条中心错是两件事 | ✅ | 保持 |
| 8 | `Editor/B141_W3_Assert.cs:263-271`（本次已修） | 命中区**中心 y**、上/下沿 | 先 `bestHitTr.Find("Hit")` 取 quad 再 `MenuCheck.DesignPx(hitQuadTr)` | 修前量的是节点 ⇒ 偏「条目中心 − 命中区中心」；现量 quad ⇒ 对（这正是 B150 报告 §一 Rewards #5/#6）。⚠️ **残留**：`:269` 有 `if (hitQuadTr == null) hitQuadTr = bestHitTr;` —— quad 缺失时会**回落到节点**继续算两条边。好在 `:268` 已先记一条红，不会「静默全绿」，但那两条边会成为二次噪声 | ✅（含 🟠 残留） | 建议把回落改成「直接 return」（前提红已记，不必再算） |
| 9 | `Editor/CollectionScene.cs:6125-6148` | 卡牌页压边格 `CardHit_*` 的**上下沿/宽** | `RectOf(h)` → `MenuCheck.NodeRectPx(…, LabelNode, …)` → 中心 = 命中 quad | `CardHit` 是 `AddHit`（= `MenuDraw.Hit`）建的，节点在父原点；但 `RectOf` 会下沉到 quad ⇒ **今天对** | ✅ | 保持；换读口时别换成 `Rect` 中心语义（`CheckAt`/`RenderedRect`） |
| 10 | `Editor/CollectionScene.cs:11733-11745` | 聊天最后一行头像命中区被视口裁后的左沿/下沿/上沿 | `RectOf(hitLast)` → 同上（下沉到 quad） | 同一族风险：`hitLast` 是 `MenuDraw.Hit` 节点。今天对靠 `LabelNode` 档 | ✅ | 保持（同 9） |
| 11 | `Editor/CollectionScene.cs:10045-10056` | 卡扇点击区四沿 | 先 `GetComponentInChildren<ImageQuad>()` 再 `QuadRectOf` | 直接量 quad ⇒ 安全 | ✅ | 保持 |
| 12 | `Editor/ShopScene.cs:3052-3067` | 卡包详情窗 Tooltip 图标命中区**四边** | `RectOf(hitNodeS)`（`TipHit` 是 `MenuDraw.Hit` 建的） | 靠下沉到 quad；该处注释已写明「⛔ 不是承载它的节点」 | ✅ | 保持 |
| 13 | `Editor/ShopScene.cs:3342-3350` / `:5433-5460` / `:5577-5582` / `:5612-5620` | 开包格命中区比例 / `GenericOptions` 两钮命中区四沿 / hero、handler 命中区 | 先取 `ImageQuad` 或 `RectOf(…)` | `:5437` 那段注释逐字写明「量的是 `Hit` 那颗 quad 自己，⛔ 不是承载它的节点 —— `MenuDraw.Hit` 的节点摆在父原点」，是本仓的**正面样板** | ✅ | 保持 |
| 14 | `Editor/SettingsScene.cs:816` / `:2406` | 关窗钮命中区 / FPS 手柄 `Handle Hit` 的**渲染矩形** | `CheckRectS` / `CheckRectPx` → `RectOf` → `MenuDraw.UnionQuadRectPx` | 走 quad 并集 ⇒ 安全 | ✅ | 保持 |
| 15 | `Editor/RewardsScene.cs:4119` / `:4190` / `:12121` / `:12197` / `:12387` | `ClaimHit` quad 四沿 / 回收站命中区三态 / 收件箱 `RowHit` 四沿 | `QuadRectOf` / `RectOfUnion` / 对 **quad** 调 `CheckAt` | 全部落在 quad 或其并集上 ⇒ 安全 | ✅ | 保持 |
| 16 | `Editor/ShellScene.cs:4479-4480` / `:4499` 等 `A1125Close` 各调用点 | 分页钮/面板命中区中心、关窗钮命中区宽度与中心 | `PxCenterOf(node)`（内部取子树第一颗 quad）· `A1125Close`（`QuadRectPx`） | 走 quad / 并集 ⇒ 安全 | ✅ | 保持 |
| 17 | `Editor/ShellScene.cs:7112-7285`（`GHitRect` 家族，7 处） | 视口四态下命中区被裁后的矩形 | `GHitRect`（`ShellScene.cs:6949`，内部 `GetComponentInChildren<ImageQuad>(true)` + `MenuDraw.QuadRectPx`） | 正确的写法；同文件 `:6921-6940` 的 `GNodeRect` 注释里留着实测代价：拿节点量在「态二」差 `交集.W/2`，**实测 +34.09px** | ✅ | 保持 |
| 18 | `Editor/MainMenuScene.cs:2372-2382`（`mbox`）/ `:4690-4703`（`box`）/ `:4746-4760`（`pbox`）/ `:17915-17917`（`CW/CH`）/ `Editor/RewardsScene.cs:4638-4642` | 命中区节点（或 `MenuDraw.Hit` 探针）的**宽/高**，以及 `localPosition == 0` 的约定守卫 | `rt.rect.width/height` · `sizeDelta` · `localPosition.magnitude` | 只量尺寸 ⇒ 对（`MenuDraw.Hit` 写的就是 `sizeDelta`）；`:4641` 那条 `localPosition == 0` 是**故意把约定钉死**，改实现会立刻红 | ✅ | 保持；⛔ 别删 `RewardsScene:4641` 那条守卫 |
| 19 | 生产指针口家族：`Editor/B102HOVER_Assert.cs:113` · `B106_W8_Assert.cs:76,120` · `B141_W75_Assert.cs:149` · `C4_Assert.cs:146` · `ShopScene.cs:3000-3016` · `ShellScene.cs:1780-1830` | 命中区中心/半宽/四沿、命中赢家、吸收层判定 | `PointerLayer.HitBoxForTest` / `ButtonAt` / `HoverAt` / `ClickAt`（生产同一份口径） | 生产口只认 quad（`HitBoxPx` = quad 世界中心 + `WorldW/H`）⇒ **结构上不碰节点位置** | ✅ | 保持（这是「不用自造量法」的首选口） |

## 三 · 统一判据（以后自查用这一句）

> **命中区的「位置/中心」只能从它的 `ImageQuad` 读（`MenuDraw.QuadRectPx` / `UnionQuadRectPx` / `PointerLayer.HitBoxForTest`）；
> 节点自身的 `localPosition` / `position` / `DesignPx(节点)` 只有在【该命中区由 `DeckCell` / `DeckInfoPopup.Hit` / `PracticeModePopup.HitOn` / `ImportDeckPopup.HitOn` 建】时才是矩形中心，
> 由 `MenuDraw.Hit` / `AddHit` / `ShadeHit` / `Absorb` 建的节点恒在父原点；`sizeDelta`（宽高）两边都可以放心读。**

两条判别式（照抄即可）：

1. **要中心/四沿** ⇒ `var q = hit.GetComponentInChildren<ImageQuad>(true); MenuDraw.QuadRectPx(q)`；
   ⛔ 不写 `MenuCheck.DesignPx(hit)`、`CheckAt(hit, …)`、`CheckAtPx(hit, …)`、`RenderedRect(hit, …)`、
   `ShellScene.NodeRectPx(hit, …)`（这些的中心都是「传入节点自身」）。
2. **要宽高** ⇒ `((RectTransform)hit).sizeDelta`（或 `rect`）**可以**；`MenuDraw.Hit` 已经把命中矩形写进去了。

## 四 · 扫过什么 / 没扫到 / 判不准

**扫过的范围**

- `Assets/CardPresentation/Editor/*.cs` 全部（现读 **655** 个 `.cs`，其中 **566** 个是 `*_Assert.cs`；含 `Hit` 字样的 **119** 个 ⇒ 这 119 个逐文件过了一遍数据流，其余按全量正则扫过）。
- `Assets/RuleEngine/Editor` · `Assets/WarpforgeVFX/Editor` · `Assets/WarpforgeArena1/Editor` · `Assets/Plugins/**/Editor`：`MenuDraw.Hit` / `AddHit` / `MenuCheck.DesignPx` / `PointerLayer.ButtonAt` **0 命中** ⇒ 与本题无关。
- 用到的关键词（全量 grep）：`Hit` · `HitBox` · `HitRect` · `HitNode` · `ShadeHitNode` · `CloseHitNode` · `MessageHitNode` · `TipHit` · `SlotHits` · `absorbOnly` · `m_RaycastPadding` · `RaycastPadding` · `WasAbsorb` · `AbsorbTierWarned` · `HitBoxForTest` · `PaddedRect` · `PaddedHitRect` · `DesignPx` · `PointerFramePx` · `PointerFramePxFromDesign` · `RenderedRect` · `NodeRectPx` · `RectOf` · `RectOfUnion` · `QuadRectOf` · `QuadRectPx` · `CheckAt` · `CheckAtWorld` · `CheckAtPx` · `CheckAtS` · `CheckNearPx` · `CheckRectPx` · `CheckRectS` · `Wpx/Hpx` · `CW/CH` · `sizeDelta` · `.rect` · `localPosition` · `.position` · `anchoredPosition` · `PixelOfDesign` · `ToDesignPixel` · `LayoutSpace.ToPixel`。
- 数据流扫法：先把「本文件里从 `"Hit"` / `Hit(` / `…HitNode` 赋出来的变量名」收全，再找这些名字出现在任一几何读口里的行；另做一遍反向（读口参数里带 hit 词根）与一遍同名单行配对，互为交叉。

**判不准 / 没查清的**

1. **`ItemDrawer.HitNode` 子树里有两颗 quad**（`MenuDraw.Hit` 建的那颗 + `ItemDrawerPartial_C4.cs:70` 追加的 `NodeIconRaycast`）。
   今天没有断言用 `RectOf`/`UnionQuadRectPx` 量这个节点（C4 那批走 `PointerLayer.HitBoxForTest`，是生产并集口）；
   但**将来**谁若对这个节点写 `RectOf`（取第一颗 quad）或 `RectOfUnion`（取并集），读数会变成「命中区」或「命中区 ∪ 问号靶」二选一，
   而**两者都不一定是他想要的**。判不准「C4 那颗靶该不该算进命中区」——那属于命中区语义，不是本次量法问题。
2. **运行时才生效的摆放**：`MenuDraw.ApplyPxRect` / `ApplySoftEdges` 会在裁剪时**移 quad 不动节点**（这是全仓既有约定）。
   本次只静态读代码，**没有实跑**去验证「某一帧下节点与 quad 中心实际差多少」；B150 报告 §三 那组静态推导（+129.75 / +0.25）与本文件判据一致。
3. **`MenuDraw.Hit` 被裁剪时**：`SetPxSize(hit, hr.W, hr.H)` 写的是**裁后**宽高（`MenuDraw.cs:2235` 先 `ClipRect` 再 `SetPxSize`），
   ⇒ 对这类节点量 `sizeDelta` 得到的就是「露出来那块」的尺寸，**与 quad 一致**。这一点我按代码读确认成立，未实跑。
4. 只覆盖 `Editor/` 与 `*_Assert.cs`（任务边界）。**生产代码里的自检口**（如 `MenuDraw.CheckShadeRule`、`PointerLayer.ForTest`）
   只按「它们怎么量」读过（结论：都不量命中区节点的位置，只量 quad 档/尺寸/绑定），未逐行展开。

**没有发现的**

- 全仓**没有**任何一处在 `MenuDraw.Hit`/`AddHit` 节点上用 `DesignPx` / `CheckAt` / `CheckAtPx` / `RenderedRect` / `NodeRectPx(Node 档)` 量**中心或四沿**（`B141_W3` 是唯一一处，已修）。
- 全仓**没有**用 `.anchoredPosition` 量命中区的断言（该属性在 `Editor/` 里有 **72** 处代码用例，全是抽屉/画心/布局件，**没有一处作用在命中区节点上**）。
- 全仓**没有**对 `ShadeHitNode`/`AbsorbHit` 量位置的断言（只断 `WasAbsorb` / 档 / `RenderQueue` / `WindowButton`）。
