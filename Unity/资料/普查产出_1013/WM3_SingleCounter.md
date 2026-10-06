# WM3 · `Single Counter` 那棵树（A691）

> 写手：WM3（本批次「清空 A 表」第四轮）· 2026-10-13
> 白名单：`Shell/CardDetailPopup.cs` · `Editor/CollectionScene.cs`（只加本段）· 本文件
> ⛔ 没跑 Unity（红线）· 没动 git · 没改两张正本

---

## 一、结论

1. **`spares == 0` 时原版走的确实是【另一棵树】**，而且是**逐字段都不同**的一棵：
   框 **170×50**（center 875,844.50→1045,894.50）· `Counter` 那颗 TMP **fs 35 / auto[25~35] / base 36 /
   `H=2`(Center) / `V=1024`(Bottom)** · **没有** `Slash` / `Duplicates text` / `Duplicate image`。
   （"转引"已被本笔**现读复核**：所有数都对得上，**多出来一件**见 §七·1 的 `V=Bottom`。）
2. 我们原来是**只把 `Duplicate Counter` 改了个名**、几何/字号/结构全部沿用 ⇒ **两棵树被当成一棵**。
   **本笔按原版另建**（`Shell/CardDetailPopup.cs` 的 `if (!dup)` 早返回分支 + 新常量族 `CcSg*`），销掉 WM2 §七·1 记的那笔。
3. **分支判据**原版已经查清、且与我们同构：`spares = 拥有 − min(拥有, 卡组上限) > 0` ⇒ dup 支；
   `== 0` ⇒ single 支（`Initialize.c` 的两条 `SetActive` + 只填 `singleCardCounter`）。**这一半不用改**。
4. 断言 **31 条**，落点 `Editor/CollectionScene.cs:4148-4314`，**两态都走**（先 dup、再把它压成 `spares == 0`、最后还原）。
   ⚠️ **一次都没跑过**（子代理不许跑 Unity）⇒ 归**主对话同步点**：`CollectionScene.Run`。
5. 🔴 **顺手挖出一条真的**：在**当前单机口径**下 `spares == 0` **根本到不了**（`Owned` 恒给足 `cap + 9`，而 `_owned` 只增不减）
   ⇒ 这一支是**建好了、恒不走**。详见 §八·1（本笔**没改**，去留要调度台/用户定）。

---

## 二、原版 `Card Counter/Single Counter` 逐节点表（自己现读 · 每个数带出处）

**判据源**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/`
- `MonoBehaviour/MonoBehaviour_2006.json` = `CardCounterDisplay`（按「同时含 `cardCounter` 与 `singleCardCounter` 两个键」扫全目录命中，**唯一一份**）。
  它的五个字段引用：`duplicateCounterContent → 149` · `cardCounter → 1892` · `duplicateCardCounter → 1823` ·
  `singleCounterContent → 361` · `singleCardCounter → 1893`。
- 另有独立复核：本笔亲跑 `python 工具/menu_dump.py bundle_scenes_scenes_mainmenuwarpforge "Card Counter" --depth 5 --relative --no-ancestor-scale`
  （⚠️ **不带 `--no-ancestor-scale` 时整棵子树被压成 0×0** —— 原版 prefab 里父链存着 `m_LocalScale = 0,0` 的**出厂姿态**，
  那不是「这件没有尺寸」，`menu_dump` 自己也在表尾这么标注）。

**层级 = 按 `RectTransform.m_Father/m_Children` 走**（不是按名字猜）：

| 节点 | RT pid | GO（`m_Name` / `m_IsActive`） | anchors · pos · sizeDelta · pivot · scale · rot | 组件 / 文字 |
|---|---|---|---|---|
| `Card Counter` | `1596` | `Card Counter` / **T** | (.5,.5)-(.5,.5) · pos **(0,−329.5)** · sd **(269.857, 79.37)** · pv(.5,.5) · scl 1 · rot 0 | `CardCounterDisplay`（MB `2006`） |
| ├ `Duplicate Counter` | `1420` | `Duplicate Counter` / T | (.5,.5) · pos (0,0) · sd **(239.991, 70.586)** | ——（对照支） |
| │ ├ `Background` | `1140` | `Background` / T | (0,0)-(1,1) · pos (0,0) · sd (0,0) · **rot z 180°** | Img spr PathID `3969383734418133180` · `m_Type=0`(Simple) · **`m_PreserveAspect=1`** · col (1,1,1,1) |
| │ ├ `Counters` | `1215` | `Counters` / T | (0,0)-(1,1) · pos **(−3.30002, −6.9)** · sd (0,0) | 纯容器 |
| │ │ ├ `Counter` | `1351` | `Counter` / T | (.5,.5) · pos **(−50.652, −5.5)** · sd (73.283, 70.586) | TMP `1892` 'x2' fs **52.6** base **36** auto[**25~52.6**] on=1 **H=4** V=512 wrap 0 raycast 1 |
| │ │ └ `Slash` | `1224` | `Slash` / T | (.5,.5) · pos **(0, −5.5)** · sd (28.02, 70.586) | TMP `1848` `'/ '` fs **57** base **36** auto[25~57] **H=2** V=512 |
| │ │    └ `Duplicates text` | `1293` | T | (1,.5)-(1,.5) · pos (0, 3.6e-05) · sd (44.09, **50**) · pv(0,.5) | TMP `1823` '88' fs **52.6** base **35** auto[**18**~52.6] **H=1** V=512 |
| │ │       └ `Duplicate image` | `1336` | T | (1,.5)-(1,.5) · pos (0, 3.9) · sd (41.936, 50) · pv(0,.5) | Img spr PathID `−8653826876429069342` · Type 0 · PA 1 |
| └ **`Single Counter`** | **`1531`** | **`Single Counter` / `m_IsActive = False`**（出厂关着） | (.5,.5) · pos **(0, 0)** · sd **(170, 50)** · pv(.5,.5) · scl 1 · rot 0 | **只有一个 `RectTransform`**（无 Image 无 MB） |
|   ├ **`Background`** | `1513` | `Background` / T | (0,0)-(1,1) · pos (0,0) · sd (0,0) · **rot z 180°** | Img spr PathID **`3969383734418133180`（和 dup 支同一张）** · Type 0 · **PA=1** · col (1,1,1,1) · `LayoutElement` |
|   └ **`Counter`** | `1151` | `Counter` / T | (0,0)-(1,1) · pos (0,0) · sd (0,0)（**stretch**） | TMP `1893` 'x2' · fs **35.0** · base **36.0** · auto[**25.0~35.0**] · `m_enableAutoSizing=1` · **H=2(Center)** · **V=1024(Bottom)** · `m_TextWrappingMode=0` · `m_overflowMode=0` · raycast 1 · margin 全 0 |

**换算成绝对矩形**（父 = `Card Options Panel` = 全屏 0,0→1920,1080；用 `Core/UguiRect.cs` 那份唯一锚点算法复算）：

| 节点 | 绝对矩形 | 尺寸 |
|---|---|---|
| `Card Counter` | 825.07, 829.81 → 1094.93, 909.19 | 269.857 × 79.37 |
| `Single Counter` | **875, 844.50 → 1045, 894.50**（两支**同心**，心 =(960, 869.5)） | **170 × 50** |
| `Single Counter/Background` | 同上（stretch sd 0,0） | 170 × 50 |
| `Single Counter/Counter` | 同上（stretch sd 0,0）⇒ **TMP 容器宽 = 170** | 170 × 50 |

> 行内 `menu_dump` 的复核值：`Single Counter` 在 `Card Counter` 相对帧里 = `49.9, 14.7 → 219.9, 64.7`（= 825.07+49.9 = **874.97** ✓ 与 875 同）；
> `Counter` 那一行的 `对齐=Center/Bottom` ✓ 与 H=2 / V=0x400 吻合。

---

## 三、两支的切换判据（原版 `文件:行` ⇒ 我们 `文件:行`）

**原版**（`d:/2/tools/decomp_full/`，本笔亲读）：

| 文件 | 关键句 |
|---|---|
| `CardCounterDisplay__Initialize.c` | `iVar1 = InventoryManager__GetOwnedCount(卡)`；`iVar2 = …GetMaxCopiesInDeck(卡,…)`；`if (iVar1 < iVar2) iVar2 = iVar1;`（= `min(拥有,上限)`）；**`if (0 < iVar1 - iVar2)`** ⇒ `SetActive(0x38 /*singleCounterContent*/, 0)` + `SetActive(0x20 /*duplicateCounterContent*/, 1)` + `Format(DAT_18425ce10, iVar2)` → `0x28 /*cardCounter*/` + `Format(DAT_184265d10, iVar1−iVar2)` → `0x30 /*duplicateCardCounter*/`；**否则** ⇒ `SetActive(0x38, 1)` + `SetActive(0x20, 0)` + `Format(DAT_18425ce10, iVar2)` → **`0x40 /*singleCardCounter*/`** |
| `CardCounterDisplay__SetSingleCounter.c` | 与上面那个 else 支**逐句同构**（`0x38→1` · `0x20→0` · `0x40 = Format(ce10, n)`） |
| `CardCounterDisplay__SetDuplicateCounter.c` | 与 if 支同构（`0x38→0` · `0x20→1` · `0x28 = Format(ce10, min)` · `0x30 = Format(65d10, spares)`） |

**字段偏移 → 名字**的对应（拿 MB `2006` 的五个引用回填，逐一自洽）：
`0x20 duplicateCounterContent`(149) · `0x28 cardCounter`(1892) · `0x30 duplicateCardCounter`(1823) ·
`0x38 singleCounterContent`(361) · `0x40 singleCardCounter`(1893)。

**两个格式串**（本笔按内存里的字面量表核过，RVA = 地址 − ImageBase `0x180000000`）：

- `DAT_18425ce10` → RVA `0x425CE10` = **`'x{0}'`** —— **`cardCounter` 与 `singleCardCounter` 用的是同一个符号**（所以那一支印的是 `x{min(拥有,上限)}`，不是多余副本数）。
- `DAT_184265d10` → RVA `0x4265D10` = **`'{0}'`** —— 只喂 `duplicateCardCounter`。
  （出处：`d:/2/tools/il2cpp_out/stringliteral.json` 的两条 `{value,address}` 记录。）

**我们**：

| 处 | 位置 | 内容 |
|---|---|---|
| 判据 | `Shell/CardDetailPopup.cs:78-82` | `CardProgress.Spares = 拥有 − min(拥有, 卡组上限)`（**与原版同式**，不用改） |
| 分支 | `Shell/CardDetailPopup.cs:703` | `bool dup = CardProgress.Spares(...) > 0`（同原版 `0 < iVar1 − iVar2`） |
| 两支 | `Shell/CardDetailPopup.cs:707`（框）· `:745-767`（single 支，早 `return`）· `:768-792`（dup 支） | 见 §四 |

---

## 四、改动清单

**A. `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs`**（纯 LF · `git diff --numstat` = **116 / 10**）

| 行 | 动作 |
|---|---|
| `145-148` | `CcWrapX1` 的注释**补一句**：「容器 = `m_SizeDelta.x`」**只对点锚点的件成立**，stretch 的件容器 = **解析后的父宽**（TMP 真读 `rect.width`）。⛔ 不改它那三个值 |
| `156-165` | **新增** `CcSgL/CcSgT/CcSgR/CcSgB = 875 / 844.5 / 1045 / 894.5`（逐字段出处写在注释里） |
| `166-174` | **新增** `CcSgPx=35 · CcSgMin=25 · CcSgMax=35 · CcSgBase=36 · CcSgWrapX=170`（出处 + 「三颗 dup 的值一个都不能套过来」） |
| `698-707` | `BuildCounter` 头：注释改成「两支是两棵不同的树（含 `Initialize.c` 的字段偏移）」+ `var rBox = dup ? dup 那套 : single 那一套`；`box`/`Background` 都用 `rBox` |
| `734-743` | WM2 那条 **「⚠️ 如实标注（顺手发现，本笔没做）」→ 「✅ 2026-10-13（A691）已另建」**（铁律 5：就地改掉过期记录） |
| `745-767` | **新增 single 支**：`if (!dup) { MenuDraw.Text(box, rBox, "x"+inDeck, …, CcSgPx, QCdText, CcSgWrapX, CcSgMin, CcSgMax, CcSgBase); SetWrapping(false); return; }` + 「本支没有那三件」+ **V=Bottom 的如实标注** |
| `775` | 原来那句 `if (dup) {` → 裸块 `{`（+ 一行注释说明「不是 `if` 体」）—— 这样 dup 那 12 行**一行都不用重排**，A574/A575 的 diff 不被冲掉 |

**B. `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/CollectionScene.cs`**（纯 LF · `git diff --numstat` = **828 / 0**，**零删除** ⇒ 没碰任何人已有的行）

| 行 | 动作 |
|---|---|
| `4148-4314` | **新增** A691 断言段（**31 条**）。⚠️ 落点在 **W-E2（4031-4090）/ WM2（4091-4147）之后**、`// 三块面板的动作`（现在是 `4315`）之前 —— 那五段**一行没碰** |

---

## 五、状态 → 参数 表

| 参数 | `spares > 0`（`Duplicate Counter`） | `spares == 0`（`Single Counter`） | 出处 |
|---|---|---|---|
| 根框 `sd` | **239.991 × 70.586** | **170 × 50** | RT `1420` / RT `1531` |
| 根 `anchoredPosition` | (0, 0) | (0, 0) | 同上（**两支同心** ⇒ 靠尺寸分辨） |
| 底图 | `40K_main_deck_card_counter` · PA=1 · rot z 180° | **同一张** · PA=1 · rot z 180° | MB `2233` / `2336`（`m_Sprite.m_PathID` 同值 `3969383734418133180`） |
| 中间容器 | `Counters`（pos −3.30002, −6.9） | —— **没有** | RT `1215` |
| 数字 TMP 名（原版） | `Counters/Counter`（MB `1892`） | **`Counter`**（MB `1893`） | `m_Father` 链 |
| 数字字号 / auto / base | **52.6** · [25, 52.6] · **36** | **35** · [**25, 35**] · **36** | MB `1892` / `1893` |
| 数字水平对齐 `H` | **4 (Right)** | **2 (Center)** ＝ 我们的出厂档 | MB `1892` / `1893` |
| 数字垂直对齐 `V` | 512 (Middle) | 🔴 **1024 (Bottom)** | MB `1893`（**我们没建模**，见 §七·1） |
| 数字文案 | `x{min(拥有,上限)}` | **`x{min(拥有,上限)}`**（同一个格式串 `ce10`） | `Initialize.c` |
| 冗余数 TMP | `Duplicates text`：44.09×**50** · fs 52.6 · [**18**, 52.6] · `H=1`(Left) · 文案 `{拥有−该数}` | **没有** | MB `1823` |
| 斜杠 TMP | `Slash`：28.02×70.586 · fs 57 · [25,57] · `H=2` | **没有** | MB `1848` |
| 图标 | `Duplicate image` 41.936×50（`40k_general_icon_card_amount`） | **没有** | RT `1336` |
| 出厂 `m_IsActive` | T | 🔴 **False**（原版 prefab 里关着，靠运行时 `SetActive(1)`） | GO `Single Counter` |
| TMP 容器宽 | 73.283 / 28.02 / 44.09（各自 `sd.x`，**点锚点**） | **170**（stretch，**解析后的父宽**） | RT `1151` |

---

## 六、断言清单（31 条 · `Editor/CollectionScene.cs:4148-4314`）

**怎么分辨两支**（本段的骨架）：
① **结构**（`Duplicate Counter` 在不在 / `Single Counter` 在不在 / 那三件在不在）× ② **尺寸**（239.991×70.586 vs 170×50）
× ③ **字号档**（52.6 那套 vs 35 那套）。**几何中心两条不是判别式**（两支 `pos` 都是 (0,0)），代码注释里已如实写明。

| # | 断什么 | 期望值来源 | 改坏法（⇒ 红） |
|---|---|---|---|
| 1 | `Card Counter` 在 | 两支出自同一父 | —— |
| 2 | dup 态 `Duplicate Counter` 在 | `Initialize.c` 的 `SetActive(0x20,1)` | 分支反了 |
| 3-4 | dup 框 **239.991 × 70.586** | RT `1420` 的 `sd`（经 `UguiRect.Child` 现算） | ① 两支共用一套框 |
| 5 | dup 态 **`Single Counter` 不在** | `SetActive(0x38,0)` | ② 两支都建（去掉 `if (!dup)` 早返回） |
| 6 | （前提）`CardProgress.Spares == 0` | 原版换支条件 | 夹具假设不成立 ⇒ 后面会连锁红（**不静默**） |
| 7 | single 态 `Single Counter` 在 | `SetActive(0x38,1)` | 分支反了 |
| 8 | single 态 **`Duplicate Counter` 不在** | `SetActive(0x20,0)` | ② 两支都建 |
| 9-10 | **★ 框 `170 × 50`** | RT `1531` 的 `sd` | **①**（这就是「只改名」那种写法的判别式） |
| 11 | 框心落在 (960, 869.5) | `UguiRect.Child` 的矩形心 | 节点摆到别处（⚠️ **不是分支判别式**，注释里已标） |
| 12-14 | `Slash` / `Duplicates text` / `Duplicate image` **都不在** | `Single Counter` 子树只有 2 个子件 | ①④ 沿用它那三件 |
| 15 | 底图还是 `40K_main_deck_card_counter` | MB `2336` 的 `m_Sprite`（与 dup 支同一个 PathID） | 换成别的图 / 没建 |
| 16-17 | 底色**渲出来 ≤ 170×50**（+「真量得到」前提条） | `m_PreserveAspect=1` + 框 | ① 误用 dup 那套框 ⇒ 渲成 227.4×70.6 ⇒ 越界 |
| 18 | `Single Counter/Counter` 在 | 原版子件名 | 没建 |
| 19 | 文案 = **`x{卡组上限}`** | `x{0}` 格式串 + `min(拥有,上限)` | 印成多余副本数 / 留空 |
| 20 | 那上面有 `Label` | —— | 建了个空节点 |
| 21 | **★ `AutoSizing == true`** | MB `1893` 的 `m_enableAutoSizing=1` | ⑤ `wrapPx` 传 `0f` ⇒ 自适应整段不执行 |
| 22 | 自适应上限 = **35px** | `m_fontSizeMax` | 抄 dup 的 52.6 |
| 23 | 自适应下限 = **25px** | `m_fontSizeMin` | 抄 `Duplicates text` 的 18 |
| 24 | 自适应 base = **36px** | `m_fontSizeBase` | 不传第 10 参 ⇒ base 落到 35（铁律 5·c） |
| 25 | 折行档 = **0**（`NoWrap`） | `m_TextWrappingMode=0` | ③ 删掉那句 `SetWrapping(false)` |
| 26 | TMP 容器宽 = **170px** | stretch ⇒ 解析后的父宽 | ⑤ `wrapPx=0f`；或抄 dup 的 73.283/44.09 |
| 27-28 | 渲染块**≤ 170×50**（+ 前提条） | `AutoFitBox` 教训（只比字号会漏「字号对而溢出」） | 字号/框配错 |
| 29 | **★ 块心 = 960**（`H=2 Center` = 出厂档 ⇒ **不调 `Align*`**） | MB `1893` 的 `m_HorizontalAlignment=2` | ④ 把 dup 那颗的 `AlignRight` 抄过来 |
| 30 | 拥有数还原 | —— | 给后面留个脏状态 |
| 31 | 还原后**又切回 `Duplicate Counter`** | 两支来回都走得通 | 只搭了个壳（单向往） |

**判别力（如实）**：
- **①（只改名）** 咬在 **9-10 / 12-14 / 16-17 / 22-23 / 26**（尺寸 + 结构 + 字号档 + 容器宽）—— **一次至少红五条**。
- **②（两支都建）** 咬在 **5 / 8**。
- **⑤（`wrapPx=0`）** 咬在 **21-24 / 26**（A404 那一族的同款判别式）。
- ⚠️ **3-4（dup 框）** 对「①」**不一定红** —— 如果只改了 single 那一支的框、dup 支原样，那两条仍然绿（它们守的是 dup 支本身）。
- ⚠️ 1 / 11 两条**不是判别式**（节点在不在 / 摆得对不对），别把它们算进「分辨两支」。
- ⚠️ **本段一条都没跑过** —— 上面的红/绿是**按代码路径推的**，最终以同步点那次 `CollectionScene.Run` 为准。

---

## 七、没查清 / 没做的（⛔ 不许猜）

1. 🔴 **原版 `Single Counter/Counter` 的 `m_VerticalAlignment = 1024 (Bottom)` —— 我们没建模**（真偏离，**要做**）。
   - 现状：`Label.RefreshBounds`（`Battle/Label.cs:799-809`）把文字块**居中**摆在锚点上 ⇒ **全工程一档 `Middle`**；
     本笔那颗因此按 **`Middle`** 画（原版是 `Bottom`）。**差值约四分之一行高**（同族先例：`Shell/BoosterPackOpenWindow.cs:66`
     对 `Capline` 也是标成「垂直对齐近似（全工程口径）」，`Deck/DeckRuntime.cs:2844` 同）。
   - **判据已齐**（MB `1893` 逐字段现读，对照：dup 那三颗都是 `512 (Middle)`；`Card Counter` 子树里**只有它**是 Bottom）。
   - **本笔做不了的原因 = 白名单**：要真做得在 `Battle/Label.cs` 加一个 `AlignBottomOn`（照 `AlignLeftOn/RightAlignOn` 那一族，
     `:862-880`），或在 `Shell/MenuDraw.cs` 加个助手 —— **两个文件都不在白名单里**。
   - ⇒ 已在 `CardDetailPopup.cs` 的 single 支注释里**就地标注**（并写了「不是判据为空、也不是用户拍板不做」），
     **请调度台另派一笔**（或并进「垂直对齐档」那一族）。
2. **没跑到实况**：这一支在**原版游戏里进不去** —— 判据吃的是 `InventoryManager.GetOwnedCount`（**服务端数据**），
   而后端已不可达（`MEMORY` 记的「开了 Steam 也连不上 PlayFab」）⇒ **本件零实拍证据**，只有 prefab 字段。
3. **渲染类的数（161.11×50 / 227.44×70.586 / 块宽）是按算式推的，没在实机上量过**：
   `40K_main_deck_card_counter.png` 实测 **116×36**（三份导出都是，本笔读 PNG 头核过），
   `MenuDraw.Rect` 的 `keepAspect` 走「按高内接」⇒ 170×50 的框里画 **161.11×50**、240×70.586 的框里画 **227.44×70.586**。
   ⚠️ 断言里**没有**把这两个数写死（只断「渲出来 ≤ 框」+「前提：量得到」），所以**不依赖**上面这段推算对错 ——
   但 §六 里「16-17 的改坏法」那句「会渲成 227.4×70.6」是推算值。
4. **自适应收敛到几号没量**：`fs 35` + 框高 50 ⇒ 是否会触发收缩、收到多少，**本笔没跑**。
   （旁证：A404 那三条对 `Duplicates text`（fs 52.6 / 框高 50）已经在跑、且当时是绿的 ⇒ 同一条机制对 single 这颗也该成立；
   `min=25` 这个下限够不够**没验**。）
5. **`CheckAt`（第 11 条）对「分辨两支」零判别力** —— 两支 `anchoredPosition` 都是 (0,0)、矩形心逐位同值。已在代码里写明，别把它当判别式。
6. 本笔**没碰** `Slash`（MB `1848`，`H=2`/`V=512`）与 dup 那三颗的 base/对齐 —— 那是 A574/A575/WM2 的账，**一行没动**。

---

## 八、顺手发现（⛔ 本件一条都没改）

1. 🔴 **`spares == 0` 在当前单机口径下【到不了】—— 新建的这一支是「建好了、恒不走」。**
   - `CardProgress.Owned`（`Shell/CardDetailPopup.cs:62-69`）**恒给足** = `卡组上限 + 9`（用户 2026-09-22 的「给足」口径）
     ⇒ `Spares` 恒 = **9 > 0**；
   - `_owned` 的**唯一**写入点是 `AddOwned`（`:70-72`，**只加**）与 `ResetForTest`（`:90`，清空重建）⇒ **拥有数只增不减**
     ⇒ **`spares` 永远 ≥ 9** ⇒ dup 支恒走。（唯一能减的是 `AddOwned(…, 负数)` —— 全仓**没有生产调用点**，
     本笔的自检就是**故意**用它把状态压到 `spares == 0`。）
   - 形态同先例：`Shell/BoosterPackOpenWindow.cs:71-73`「`New Card Badge` 恒不显示：层照建、如实说，**不是忘了做**」。
   - ⛔ **本笔没改**（要改得先定「单机怎么消耗卡」这个口径 —— 那是用户/调度台的事）。
2. **我们 dup 支的框常量与原版差 0.009 / 0.006px**：`CcBgL/T/R/B` = 840 / 834.21 / 1080 / 904.79 ⇒ 240 × 70.58，
   原版 RT `1420` 的 `sd` = **239.991 × 70.586**。**不用动**（断言容差 1.5px），只是记一笔「我们存的是四舍五入后的矩形」。
3. **原版 `Single Counter` 根 GO 出厂 `m_IsActive = False`**（该窗自己的根也是出厂 inactive）——
   我们是**按 `!dup` 直接建 / 不建**，没有那套 `SetActive` 开关。**行为等价**（原版那两条 `SetActive` 的净效果就是「只有一支活着」），
   如实记：**我们没建那个「两支都在、只切显隐」的结构**。
4. **`MenuDraw.Text` 的自适应整段写在 `if (wrapPx > 0f)` 里**（A404 已经记过一次）——
   对**任何**以后要建「原版带 `auto` 的 TMP」的写手仍然是个坑（只传 `autoMinPx` = 传死实参、字段对画面没有）。
   本笔再记一次：**stretch 的件要把「解析后的宽」传进 `wrapPx`**，⚠️ TMP 真读的是 `rect.width`，不是 `sizeDelta.x`。

---

## 九、类型检查结果

```
TMPDIR=/tmp/wf_wm3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
- 跑了 **5 次**（每次改完立刻跑）：改 `CardDetailPopup.cs` 常量族后 · 改 `BuildCounter` 后 · 加完断言段后 · 补注释后 —— 前四次都是 **0 / 0**。
- 🔴 **第 5 次（11:00:07）编辑器程序集报过 1 条 —— 不是本笔的**：
  `Assets\CardPresentation\Editor\MainMenuScene.cs(8418,13): error CS0103: …HAlignUndecidable`。
  按铁律 13·3 的判据处置：**错误集中在【不是我的文件】上** ⇒ ⛔ 没去改它，**隔 45 秒重跑** ⇒
  11:01:02 那次 **0 / 0**（`MainMenuScene.cs` 的 mtime = **11:00:37** ⇒ 是邻居写手当时正写到一半、我的类型检查把半成品一起编了进去）。
  **本笔两个文件从头到尾没有编不过的记录。**
- **没有**任何（属于本笔的）报错。`git diff --numstat`（收工时）：
  `Shell/CardDetailPopup.cs` = **119 / 10** · `Editor/CollectionScene.cs` = **828 / 0**
  （⚠️ 后者**零删除** ⇒ WM2/W-E2/W-E4/WSmall3/WSmall4 那几段**一行都没被碰**；
  ⚠️ 期间 `CollectionScene.cs` 的 mtime 只有我自己那两次 —— **本笔落笔时没人在动它**）。
- 行尾：两个文件**都是纯 LF**（写完复核 `CRLF=0`）—— 全程用 Edit 工具，⛔ 没用 `sed -i`、⛔ 没用 python 整篇写。
- ⛔ **没跑 Unity**（红线）⇒ **`CollectionScene.Run` 一条断言都没执行过**，归同步点。
