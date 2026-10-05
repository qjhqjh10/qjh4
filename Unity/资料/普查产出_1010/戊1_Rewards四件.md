# 戊1 · `RewardsScene` 四件（A267 · A281 · A238 · A240+A272）—— 2026-10-11

> 写手：戊1（批次 1）· 白名单 = `Editor/RewardsScene.cs` · `Shell/{ItemDrawer,DailyStreakPopup,CampaignRewardWindow,CampaignTab}.cs`。
> ⛔ 本件**没跑 Unity**（批处理全局串行、只有调度台能跑）；跑的是**秒级类型检查**（读数见 §六）。
> 🔴 本文件按调度台要的格式：① 结论 ② 证据 + 每条断言的改坏法 ③ 改动清单 ④ 没查清 ⑤ 顺手发现（**只报不改**） ⑥ 跑过的检查。

---

## 一、结论（四件各一句）

| # | 件 | 一句话结论 |
|---|---|---|
| 1 | **A267** | 锻造页那一大片断言**原来跑在未激活的页上**（页签要等到「指针层」那段才第一次 `Click(2)`）⇒ 已把切页**提到 §三·b 开头**，并加了「**进本节时页签 = Forge 且 `Forge Tab` 是 `activeInHierarchy`**」两条前提断言 —— **夹具级改动，画面/行为零变化**（推导见 §二·1）。 |
| 2 | **A281** | `Premium Campaign daily bonus` 那颗 TMP 的**折行被 `SetAutoFitBox` 无条件打开**（原版是 `m_TextWrappingMode = 0`）⇒ 已按 A62 那一族补 `SetWrapping(false)`；**连带**把同处那条「左边缘 ≥ 框左边」的断言**换掉**（它只有在我们多折一行时才成立 = 钉的是偏离），换成「折行=0 / 就一行 / 比框宽」三条。 |
| 3 | **A238** | `ItemDrawerStyle` 加了 **`ClipSoftness`** 并**逐层透传**到四条画路（`MenuDraw.Rect` 的 `clipSoftness`）⇒ 抽屉那几层与同一视口里的列底/按钮**同一套 `(200,0)` 渐隐**；战役奖励窗那边给的是本窗那一份状态（`BuildColumn` 期间 = `(200,0)`）。 |
| 4 | **A240+A272** | 连登窗条目**从顶对齐 132.09 改成原版的竖向居中**（上沿 **308.99**、中心 **567.14**）⇒ 放大 1.2 的那一格**不再顶出视口**，A182 收尾那条「渲出来的高之比 = 1.2」**改回并落盘**（新加一条）；A272 = 本窗（战役奖励窗）`Viewport/Content` 改成**原版那个零宽点 960,285→960,935**（⚠️ **`MenuDraw.Node` 只吃矩形中心 ⇒ 这一件【没有可观测行为变化】**，见 §四·1）。 |

---

## 二、证据 + 每条断言的改坏法

### 2·1 件1 · A267（`Editor/RewardsScene.cs:1581`）

**现象（判据出处）**：`资料/普查产出_1008/X_RewardsScene崩溃修.md` §七·2（`资料/普查产出_1010/S1_下一批切块普查.md` §A267 转述）。
`git` 可复核：本件开工时，§三·b（锻造厂）那一片（约 `:1576-2200`）之前**只有** `Click(1)`/`Click(0)`/`Click(3)`（左栏第 4 键不切页），
第一次 `Click(2)` 在「指针层」那一段（本次改前 `:2082`）。

**为什么它是个缺陷**（不是洁癖）：
- 量矩形/命中区不受影响（`Transform.Find` / `GetComponentsInChildren(true)` 找得到关着的子树）—— 所以今天全绿；
- 但**任何依赖「真渲染」的断言在关着的页上必然假绿**：TMP 在未激活对象上 `ForceMeshUpdate` 出不来网格
  （`资料/已知的坑.md`「面板画出来了、字不在」坑①）；`MenuDraw.ClipText` 那条路在未激活的页上只能数进
  `TextClipUploadSkipped`（`Shell/MenuDraw.cs` 的 `ClipTmpMesh` 注释）⇒ 在那一段里验「字被裁/被削 alpha」拿到的是「没生效」。

**改动**：`Editor/RewardsScene.cs:1599`（新）`if (forge != null && win.tabButtons != null) win.tabButtons.Click(2);`
+ `:1600-1604` 两条 ★ 前提断言。
**零变化的三条推导**（都可在源码里核）：
① `ForgeTab.Build()` 末尾本来就调过 `Refresh` / `FocusSelectedArmy` / `FocusClaimable`（`Shell/ForgeTab.cs:367-371`）；
② 两条 `FocusOn` 都是**绝对**定位（`MenuScroll.SetOffset`，偏移没变就早退，`Shell/MenuScroll.cs:183-191`）⇒ 偏移逐字相同；
③ 格子的 x 只由内容坐标 + 偏移决定（偏移没变）+ `ForgeData.Selected` 也没变 ⇒ 渲染逐字相同。
下面那几处 `Click(2)`（`:2105` / `:2247` 那一带）从此**幂等**，**照旧调、没改**。
⚠️ 切页那句**带 `forge != null` 的门**：页不存在时 `ChangeTab` 会把三页全关掉，会把后面几十条断言一起带进沟里（红要红在本节上）。

| 断言 | 改坏哪里它会红 |
|---|---|
| `Check(win.CurrentTab, WindowTabType.Forge, "★（夹具前提）进本节时页签真的切到 Forge 了…")` | 删掉/挪后 `:1599` 那句 `Click(2)`（或把 `Click(2)` 改回 `Click(1)`）⇒ 红 |
| `CheckTrue(forge != null && forge.gameObject.activeInHierarchy, "★（夹具前提）Forge Tab 此刻是活的…")` | 同上；或把 `ChangeTab` 里的 `SetActive(true)` 去掉 ⇒ 红 |

### 2·2 件2 · A281（`Shell/CampaignTab.cs:747` + `Editor/RewardsScene.cs:3866-3903`）

**判据（原版同一份 MB 的四个字段）**：`bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_-818462233560502899.json`
（与 `…1918117691617384191.json` 逐位一致）= `m_fontSize 33.3` · `m_enableAutoSizing 1` · `m_fontSizeMin 10` ·
`m_fontSizeMax 40` · **`m_TextWrappingMode 0`**（⛔ 不是我们自己的常量）。

**成因**：`panTitle.SetAutoFitBox(…)` 之后没还原折行 —— `SetAutoFitBox → SetWrapWidth`
（`Core/TmpFont.cs:208` **第一句无条件** `t.textWrappingMode = Normal`）。
**修法**（照 A62 那一族的既有写法，`Shell/AvatarTab.cs:186` · `Shell/CollectionWindow.cs:1623` ·
`Editor/MainMenuScene.cs:1365`）：`panTitle.SetWrapping(false);` —— **放在 `SetAutoFitBox` 之后、`AlignRight` 之前**
（A205：`SetWrapping` 内部 `ForceRelayout` 会挪 TMP 子节点，对齐必须在它之后算）。⛔ 没碰 `Core/TmpFont.cs`。

🔴 **连带核出来的一件事：旧那条「左边缘 ≥ 框左边 354.43」不能留**（原文写在 `Editor/RewardsScene.cs` 里）。
它的推论是「原版 `m_enableAutoSizing = 1` ⇒ 装不下就**缩字号**，不是溢出」——
**对纵向上成立、对横向不成立**。判据 = 本地 uGUI/TMP 源码
（`Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TextMeshProUGUI.cs`）：
🔴 **2026-10-11 订正（铁律 5 · 保留更正痕迹）**：原文写「横向那一支（`:3917`）**整块长在** `if (m_TextWrappingMode != NoWrap …)` **那道闸里**（`:3565`）⇒ `NoWrap` 时它一次都不跑；⇒ **原版这条 TMP 就是横向溢出**」—— **这个判据是错的，括号配错了**。
· **真相**：`:3565` 那个 `if` 块**闭合在 `:3888`**，**`:3889` 才是 `else`**，而 `:3917` **正落在 `else` 里** ⇒ **`NoWrap` 反而【直接进】缩字号支**。我们实际用的 `TextMeshPro.cs` 同构：闸 `:3216` 闭合 **`:3539`** · **`else :3540`** · 该支在 **`:3568`**。
· ⇒ **原版画的是一行【贴框宽】、不是溢出**（另：MB 里还有 `m_fontSizeBase = 12`，`TextMeshPro.cs:2149` 会 `Clamp(base, …)` ⇒ 从 12 起二分涨到「一行塞得下」）。
· **判据/逐字配平脚本** → `资料/普查产出_1010/F5_Rewards那条断言.md` §② · 通用教训 → `资料/已知的坑.md` **2026-10-11（D2 · A281）**那一节。
· ⛔ **别再照上面那段原文用**（它对纵向上成立、对横向**反了**）。 —— 正本
`资料/阶段二_锻造厂与战役页_原版规格.md:676`：「`Title` 是 355.79 宽放 33.3 号字 —— **框本身就装不下**……
**别再为此改数值**」。⇒ 旧断言**只有在我们被悄悄打开折行（折成两行）时才绿** = 钉的是偏离。

| 断言（新增 3 条 + 2 条前提） | 改坏哪里它会红 |
|---|---|
| `CheckTrue(cpanTLb != null, "（A281 前提）…Label 取得到")` | 把 `Premium Panel/Title` 建掉 ⇒ 红 |
| `CheckTrue(cpanTLb.WrappingMode >= 0, "（A281 前提）…TMP 后端")` | 机器上没有 TMP 字体资产（走点阵后端）⇒ 如实红、不假装（同 `:694` 那一族的写法） |
| `Check(cpanTLb.WrappingMode, 0, "★ …折行 = 0（原版 m_TextWrappingMode = 0）")` | 删掉 `Shell/CampaignTab.cs:747` 那句 `SetWrapping(false)` ⇒ 回 1 ⇒ 红 |
| `Check(cpanTLb.LineCount, 1, "★ …而且渲出来就一行")` | 同上（折行=1 时这段 28 字的串折两行）⇒ 红 |
| `CheckTrue(TextRightPx-TextLeftPx > 355.79f, "★ …而且它比框宽")` | 同上（折行一生效，渲出宽立刻 ≤ 框宽）⇒ 红 |

保留不动的：`:3863` 那条「**右边缘 ≤ 720.35**」（`AlignRight` 之后仍成立）· `CheckFontWindow(…, 10f, 40f)`。

### 2·3 件3 · A238（`Shell/ItemDrawer.cs:184` + 四条画路 + `Shell/CampaignRewardWindow.cs:624`）

**判据（波 C1 报告 §四·1 / A238 那一行）**：`ItemDrawerStyle` 只有 `Clip`、没有 `ClipSoftness` ⇒ 战役奖励窗里
**列底/按钮/徽标是 `(200,0)` 渐隐**（`DrawNine`/`DrawRect` 都传了 `ClipSoftness`），而**物品抽屉那几层是硬边截** —— 同一条带里两种观感。

**改动**：`ItemDrawerStyle` 加 `Vector2 ClipSoftness`（`Default(...)` 给 `Vector2.zero` = 原行为）,
四条画路逐条透传：`Shell/ItemDrawer.cs:793`（阵营徽记）· `:816`（野牌图标档）· `:831`（通用图标档）· `:843`（占位板）；
`Shell/CampaignRewardWindow.cs:624` 给 `st.ClipSoftness = ClipSoftness;`（与 `st.Clip = RenderClip` 同一时刻的窗口状态、成对）。
⚠️ **只给 `Clip` 的调用方（商店 / 战役节点那几处）保持 `(0,0)` ⇒ 一条字节都没动**；`Shell/CampaignTab.cs:397`
那条走的是锻造轨道视口（原版 `m_Softness = (0,0)`）⇒ 那里**默认值就是对的**，没加、也不该加。

| 断言（10 条，全在 `Editor/RewardsScene.cs:3753-3830`，用 `cwWide` 那个「高级列 4 件」加压夹具） | 改坏哪里它会红 |
|---|---|
| `CheckTrue(wPick != null, "（前提）找得到压在渐隐带里的物品格（box 中心 x = 1830）")` | 夹具列位置一改（padL/spacing/ItemW）⇒ 前提红（出声，不静默跳过） |
| `CheckTrue(wIconQ != null, "（前提）那一格的主图（或占位板）画出来了")` | 抽屉不再画主图 ⇒ 红 |
| `CheckTrue(icc != null && icc.Length == 4, "★ 抽屉那一层有顶点色（= 真吃了 (200,0) 软边）")` | **删 `CampaignRewardWindow.cs:624`**（或去掉 `ItemDrawer.cs` 四处的 `st.ClipSoftness` 实参）⇒ `CornerColors == null` ⇒ 红 |
| `CheckNear(icc[0].a, clamp01((1920 − 左沿)/200), 0.01)` | 软边写成 `(0,200)`（或 100/89）⇒ 值不对 ⇒ 红 |
| `CheckNear(icc[1].a, clamp01((1920 − 右沿)/200), 0.01)` | 同上 |
| `CheckTrue(icc[0].a < 0.999f && icc[1].a < 0.999f, "★ …两个角都被削")` | 同第 3 条（硬边 = 全 1） |
| `CheckTrue(icc[0].a > icc[1].a + 0.05f, "…斜坡方向：左边比右边亮")` | 把 softness 写成 `(0,200)`（削上下）⇒ 左右相等 ⇒ 红 |
| `CheckTrue(Mathf.Abs(icc[3].a−icc[0].a) < 0.001 && Mathf.Abs(icc[2].a−icc[1].a) < 0.001, "…y 方向是硬边")` | 写成 `(200,200)` ⇒ 同侧上下不再相等 ⇒ 红 |
| `CheckNear(icc[0].r, 1f, 0.001f, "…只乘 alpha、rgb 不动")` | 把斜坡乘到 rgb 上 ⇒ 红 |

**期望值口径**：`1920`（视口右沿）与 `200`（原版 `m_Softness.x`）都是**原版字面量**，x 是**量出来的角**
（`QuadRectOf`）⛔ 不读 `CampaignRewardWindow.ScrollSoft`（那是被测实现）。
🔴 **为什么必须量顶点色**：那一块**整块落在渐隐带里**（不带几何切）⇒ `ScanSoftCuts` 一条切线都查不到
（`MenuDraw.ApplySoftEdges` 的「整块都在同一个线性段里」那一支）；带内与带外的差别**只在四角 alpha**。

### 2·4 件4 · A240 + A272

**(a) A240 条目纵向摆位**（`Shell/DailyStreakPopup.cs`）
判据 = 原版 `Rewards Content` 的 `HorizontalLayoutGroup`（`align = 3(MiddleLeft)` · `pad = 2,0,58,0`；
⚠️ 形状是 UGUI 的 `(L,R,T,B)` ⇒ **T 58 / B 0**）+ 内容矩形 **y 132.09 → 944.19**：
`EntryTop = (132.09 + 58 + 944.19)/2 − 516.301/2 = 567.14 − 258.15 = **308.99**`。
改动：`Shell/DailyStreakPopup.cs:88`（`ContentPadT/B`）· `:96`（`S_Content`，顺带把 x 从 `0` 改成原版的 **2.0** ——
`MenuDraw.Node` 只吃中心、子件按画布绝对坐标摆 ⇒ **零画面变化**）· `:107`（`EntryTop`）· `:313`（条目 y）。
🔴 **连带**：`Editor/RewardsScene.cs:4013-4040`（新）· 并把 A182 收尾那段「改量渲出来的高也不成立…记在报告里，没动」
的注释**就地订正**（`:4061-4065`），同时**把「渲出来的高之比 = 1.2」落盘**（`:4101-4110`）—— 那一条**只有 A240 修完才成立**。

| 断言（4 条 + 1 条前提） | 改坏哪里它会红 |
|---|---|
| `CheckNear(PxYOf(ds.entries[0].position.y), 567.14f, 0.5f, "★ 奖格的竖向中心 = 567.14")` | `DailyStreakPopup.cs:313` 退回 `S_Content.y1`（顶对齐）⇒ 390.24 ⇒ 红 |
| `CheckNear(ty1, 289.70f, 0.5f, "★ 第 6 格 BG 上沿 = 289.70")`（= 原版「居中 + 放大 1.2 绕格心」的几何后果；顶对齐那一版 = 80.46，会被裁到 159.33） | 同上 ⇒ 红 |
| `CheckTrue(false, "（A240）第 6 格 BG 量得到渲染矩形")`（量不到时出声） | 抽屉/BG 不再画 ⇒ 红（不静默跳过） |
| `CheckNear((by2−by1)/(ny2−ny1), 1.2f, 0.01f, "★ BG 渲出来的高 = 邻格的 1.2 倍")` | 退回顶对齐 ⇒ 两格都被视口顶裁（497.31 / 452.91）⇒ 比值 1.098 ⇒ 红；删 `ScaleAbout` 那一句 ⇒ 1.0 ⇒ 红 |

**(b) A272 `Viewport/Content` 零宽点**（`Shell/CampaignRewardWindow.cs:109` + `:306`）
原版 = **960,285 → 960,935**（CSF `m_HorizontalFit = 2(MinSize)` 撑出来的零宽点）；我们原来建的是「与 `Viewport` 同矩形的容器」。
🔴 **如实说清：这一件在我们这套里【没有可观测行为变化】** —— `MenuDraw.Node` 只写 `localPosition`、**不写 `sizeDelta`**，
而「与 Viewport 同矩形」与「零宽点」的**中心都是 (960,610)** ⇒ 场景里量不出差别（`MenuDraw.cs` 不在白名单，⛔ 没动它）。

| 断言（4 条） | 改坏哪里它会红 |
|---|---|
| `CheckTrue(wInner/wbCol/wpCol 都在)` | 少建那一层 / 改名 ⇒ 红 |
| `CheckNear(PxOf(wInner.position.x), 960f, 0.5f, "★ 那个零宽点的中心 x = 960")` | 内容容器挪走 ⇒ 红（⚠️ 它对 A272 这次改动**本身**恒真 —— 见上） |
| `CheckNear(PxYOf(wInner.position.y), 610f, 0.5f, "…中心 y = 610")` | 同上 |
| `CheckNear(内容容器中心 x, 两列中心的中线, 0.5f, "★ …正落在两条列中心的中线上"（相对断言）)` | 谁把内容容器挪到别人身上 / 两列被挪 ⇒ 红 |

---

## 三、改动清单（白名单内，五个 `.cs` 一个不越界）

| 文件 | 位置 | 改了什么 |
|---|---|---|
| `Editor/RewardsScene.cs` | `:1581-1604` | **A267**：切页提前 + 两条 ★ 前提断言（夹具级） |
| | `:3694-3716` | **A272**：`Viewport/Content` 零宽点 4 条断言 |
| | `:3753-3830` | **A238**：抽屉软边 10 条断言 |
| | `:3866-3903` | **A281**：换掉旧的「左边缘」那条，新增 3 条 + 2 条前提 |
| | `:4013-4040` / `:4061-4065` / `:4101-4110` | **A240**：摆位 2 条 + 前提 1 条；订正 A182 收尾那段注释；**新落盘**「高之比 = 1.2」1 条 |
| `Shell/ItemDrawer.cs` | `:174-184` / `:194` | `ItemDrawerStyle.ClipSoftness` 字段 + `Default(...)` 给 `Vector2.zero` |
| | `:793` / `:816` / `:831` / `:843` | 四条画路逐条透传 `st.ClipSoftness` |
| `Shell/CampaignRewardWindow.cs` | `:101-109` / `:306` | **A272**：`InnerContent` 常量（原版零宽点）+ 建点改用 |
| | `:617-624` | **A238**：`st.ClipSoftness = ClipSoftness;` + 就地订正「抽屉这条路上没有软边」那段 |
| | `:407` | 注释里那句 `ItemDrawerStyle.Clip` → 补 `+ ClipSoftness` |
| `Shell/CampaignTab.cs` | `:713-747` | **A281**：`panTitle.SetWrapping(false);` + 就地订正「原版是【缩字号】不是溢出」那句（附 TMP 源码判据） |
| `Shell/DailyStreakPopup.cs` | `:84-113` / `:313` | **A240**：`ContentPadT/B` · `S_Content`（原版零宽点 + y 区间）· `EntryTop` · 条目 y |

**行尾**：五个文件改完**现读全是纯 LF、0 个 CRLF**（二进制读，`git diff --numstat` 量级也对得上：只动我碰的那几段）。

---

## 四、没查清的部分（⛔ 不猜）

1. **A272 的「零宽」在本工程里量不出来**（本次最该由调度台知道的一条）：`MenuDraw.Node`（`Shell/MenuDraw.cs:59-65`）
   **只写 `localPosition`、不写 `sizeDelta`** ⇒ 原版那个「宽 0」的点在我们场景里**没有可读的载体**。
   ⇒ A272 这次改的**只有代码/常量的表达**（与「同一个内容矩形在两处各表达一次」那条欠账对齐），**行为等价**（两者的中心都是 960,610）；
   断言只能钉**位置**与**相对关系**（两条列中心的中线）。
   **要做的话**：给 `MenuDraw.Node` 补一句 `sizeDelta = Px(w, h)`（它在白名单外，且会动全工程所有空节点）—— **这是一件独立的活**。
2. **A281 的「原版实际收敛到多少 px」仍然没人查过**（简报已标）⇒ 我**没有**断那个字号；
   新加的三条只断「折行模式 / 行数 / 比框宽」这三件**确定**的事。
   ⚠️ 顺带一条**推断**（不是实证）：按上面那份 TMP 源码，`wrap=0` 时横向越界**不缩字号** ⇒ 原版屏幕上那串
   应该也是**一行、横向溢出**；但**原版客户端那份 TMP 是哪个版本、行为是否一致，本件没查**（要真 Play 或原版截图才坐实）。
3. **A240 的「上沿 ≈ 309」是算出来的**（波 C1 报告 §四·4 给的也是 ≈309，与本件的 308.99 一致）；
   ⚠️ **没有**原版实拍/运行时读数二次确认（条目那一族的 `localPosition` 由 HLG 跑出来，dump 里是模板位）。
4. **A238 只覆盖了「主图/占位板」那一层**：另外三层（阵营徽记 `Army Icon`、阵营名条、数量）走的是同一份
   `ClipSoftness` 透传，但**没有各自的断言**（同一个夹具里它们分别在别的物品格上、位置不落在带里 ⇒ 一条也咬不住）。
   ⇒ 钉的是「这一条路带电」（主图那一层），另三层靠**同一段代码**保证（`ItemDrawer.cs` 四条画路一处一个实参）。
5. ⚠️ **本件改了两处「画面」**，需要调度台决定要不要重新并排比：
   · **`02_战役.png` 会变** —— `Premium Panel/Title` 从「两行挤在框里」变成**一行、横向溢出**（= 原版字段的行为）；
   · **`04_每日连登.png` 会变** —— 连登窗条目整体**下移 176.9px**（这是 A240 的目的）。
   两者都要重新目视（本件跑不了 Unity，出图只能等 `RewardsScene.Run`）。

---

## 五、顺手发现（⛔ 一个都没改）

1. 🔴 **`01_日常_Missions.png` 其实是【锻造厂页】的截图** —— **硬证据**：它与 `03_锻造厂.png`
   **字节完全相同**（`d:\4\_tmp_view\rewards\` 下 `md5sum` 都为 `34630922ab930d4a3ded67326e74241c`、大小都是 1505160）。
   成因：`Editor/RewardsScene.cs:3854` 那句 `Shoot("01_日常_Missions.png")` 之前，**最后一次会切换页签的调用**是
   `Click(2)`（指针层那段，本次改前 `:2082`、`:2248`），而 `Click(1)` 在**下一行**（`:3855`）⇒ 拍到的页是 Forge。
   `Shoot` 只查「平均亮度 > 3」（`:594` 起的 `Shoot`，那句在 `:616`），两个页面都是亮的 ⇒ **没有任何断言会红**。
   **建议**：在 `:3854` 之前补一句 `win.tabButtons.Click(0);`（一行）。⛔ **本件没做** —— 它会**改一张 PNG**（
   与「A267 只动夹具、零画面变化」的口径相冲），请调度台裁。
   ⚠️ 顺带：`03b_锻造厂_不可领.png` / `02b_锻造厂.png`（9-23 的旧文件）与 `02b_战役奖励窗.png` 的命名也对不太上，**没细查**。
2. ⚠️ **§三·c 战役页那一节（`Editor/RewardsScene.cs:3162` 起）跑在【未激活】的 `Campaign Tab` 上** ——
   **与 A267 完全同一族**：它之前最后一次切页也是 `Click(2)`（Forge），`Click(1)` 要等到 `:3855`。
   今天那一节只量矩形/位置/字符串（不吃渲染）⇒ **没有假绿**，但**下一批谁往那一段里加「渲染类」断言就会中同一个坑**。
   （本件的 A281 那三条渲染断言在 `:3855` 之后，天然安全。）**建议**：照 A267 的写法给那一节也补「切过去 + 前提断言」。
3. ⚠️ **`ItemDrawer` 里那三层【文字】完全不吃裁切**（`Shell/ItemDrawer.cs:799` 阵营名 · `:846` 短名 · `:879` 数量）：
   `MenuDraw.Text` **没有裁切形参**，而本库**也没走** `MenuDraw.ClipText` ⇒ 物品压在视口边上时，
   它的**图被裁、字照画**。⚠️ A182 那段注释（`Shell/CampaignRewardWindow.cs` 的 `BuildItem`）写的是
   「抽屉里那四层**凡是整块落在视口外的**不建、压在边上的**截**」—— **对文字那三层不成立**（本件已在那段注释里写明）。
   今天没有断言/夹具咬到（文字块都落在视口内）⇒ 不红，但**是欠账**（铁律 11：要做的活，判据 = `MenuDraw.ClipText`）。
4. ℹ️ **`Shell/CampaignTab.cs:622-633` 的 `RefreshNodes()` 设了 `Clip` / `ClipPad`，但【没设 `ClipSoftness`】** ——
   同一条纪律（「谁设 `Clip` 谁顺手把它设对」）在 A48 那批只补到了 `ClipPad`。今天没有泄漏（各写入方都会还原），
   但**下一个不还原的写入方**会把 `ClipSoftness` 漏进战役轨道。建议补一句 `_win.ClipSoftness = Vector2.zero;`
   （原版这一处实读 `(0,0)`）。⛔ 本件没动（不属四件，且要配断言）。
5. ℹ️ **A62 主表 #16 / #17 那两处仍在**（本件只做 A281 = 主表 #18）：`Shell/CampaignTab.cs:183`（`_title`）·
   `:193`（`_points`）同样在 `SetAutoFitBox` 之后没还原折行（两处原版都是 `折行=0`）。
   **本件没动**（不在 A281 那一行里，且这两处要各自配断言）⇒ 留在 A62 那一族。
6. ℹ️ `Shell/DailyStreakPopup.cs:391` 的 `Text(e, O(E_CollectText), …, 52.85f * 0.7f * k, …)` 用 **0.7 字面量**
   （同文件别处是 `* k`）—— 那是 Collect 组自己的 `scl=0.7`，看着是有意的，**没改、只是记一笔**。

---

## 六、跑过的检查

- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_e1 bash d:/4/Unity/工具/typecheck.sh`
  → **运行时程序集 错误数 0 · 编辑器程序集 错误数 0**（跑了两遍：第一次落 A267/A281/A238/A272 之后、第二次在 A240 与
  `S_Content` 的 x 改动之后；两次都 0 错，**没有别人半成品文件混进来的报错**）。
- ⛔ **没跑任何 Unity 自检**（红线）—— `RewardsScene.Run` 由调度台在同步点跑（本件覆盖的那几条断言全在那一条自检里）。
- ⛔ **没动 git**；⛔ **没改两张正本**、没改任何 `资料/待办判据_*.md` / `资料/已知的坑.md`。
