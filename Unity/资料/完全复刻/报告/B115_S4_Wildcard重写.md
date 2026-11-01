# B115_S4 · `Wildcard Drawer` 重写（`B-293` 第 1 项）

> **执行代理**：`B115_S4`（2026-11-01）· **判据**：`判据/B115_S4_三份Drawer规格.md` §四
> **改动文件**：`Shell/ItemDrawer.cs`（**同物理行替换**）+ 新建 `Shell/ItemDrawerPartial_B115S4.cs`（+ `.meta`）
> ⛔ 没碰：`Editor/RewardsScene.cs`（别的写手在改）· `Shell/ItemDrawerLayout.cs` · `Shell/ForgeTab.cs` · 账 / 正本 / git

---

## 〇、一句话

把 `Wildcard()` 从「我们自己挑的版式」整段换成 **`Wildcard Drawer.prefab` 逐层画**：
`Content`（新）→ `Background`（稀有度底图，铺满 · PA=1）→ `Image`（阵营徽记，**下沿横带**）→
`Army`（阵营名，**灰 · Bottom**）→ `Label`（**名条底板 + 左 Quantity / 右 Name**）。
`WildcardIcon()` **一个字节没动**（它服务的是另一个 prefab：`Icon Wildcard Drawer Variant`）。

---

## 一、改了哪几层（原版 / 改前 / 改后）

| # | 原版（`Wildcard Drawer.prefab`） | 改前（我们的） | 改后 |
|---|---|---|---|
| 帧 | 根 `AspectRatioFitter` `m_AspectMode 2` · `0.66` ⇒ 根宽 = `0.66×高` | 无帧，几何全按 `Square(box, …)` | `FrameOf(box, fit: RootFit.HeightControlsWidth)` |
| ① | `Content` 712×1080（`UIScaleToFit` 缩到根） | **没有这一层** | `DrawerContent(node, fr)`（= x 0.4…712.4） |
| ② | `Content/Background` 铺满 · `m_PreserveAspect 1` · `wildcardBackgrounds[rarity−1]` | 居中方形 `min(W,H)×0.7`、节点名 `Icon`、队列 `QArt` | `DrawerBackground(content, fr, st, WildcardArtName(rarity))` ⇒ 铺满 · PA=1 · 节点名 `Background` · 队列 `QBoard` |
| ③ | `Content/Image` 锚 (0,−0.05)-(1,0.13) ⇒ **y 939.6…1134** · PA=1 · `GetArmyIcon` | 居中方形 `min(W,H)×0.34`、节点名 `Army Icon` | `WildcardEmblemRect(fr)` ⇒ y 939.6…1134 · PA=1 · 节点名 **`Image`** |
| ④ | `Content/Army` 锚 (0,0.25)-(1,0.35) ⇒ **y 702…810** · fs75/base36/auto[12,75] · H=Center · **V=Bottom** · 色 **(0.689,0.689,0.689,0.851)** | 顶部自造条（`NameStrip`）、色 **白**、节点名 `Army Name` | `DrawerArmyName(...)`（色已对）+ `MenuDraw.SetVAlign(..., Bottom, ...)` · 节点名 `Army` |
| ⑤ | `Content/Label` = `40k_main_bt_nametag` 底板（PA=0）+ `HorizontalLayoutGroup` | **整层没有** | `WildcardLabelRow(...)`：底板 `Label` + 两颗 TMP |
| ⑤a | `Label/Quantity` 左半（x 61.0…351.4）· H=**Right** | 钉在框底（`side 10 / top 60 / bottom 15`）、右对齐 | `fr.Map(DrQtyX1, DrLbY1, DrQtyX2, DrLbY2)` · `align: 2` |
| ⑤b | `Label/Name` 右半（x 361.4…651.8）· H=**Left** | 顶部第二条自造条（`NameStripAt(box, …, 1)`） | `fr.Map(DrNameX1, …, DrNameX2, …)` · **`align: 1`** |
| ⑥ | `Converted` / `Ephemeral` / `Collected Badge` / `Premium Highlight` 出厂 `m_IsActive 0`，由调用方开 | 同（不建） | **同**（口径不变，注释里写明） |

**几何一个数字都没新造**：`Content`/`Background`/`Army`/`Label`/`Quantity`/`Name` 六处全用
`ItemDrawerLayout.cs` 里既有的 `DrCx1/DrCx2/DrRootH/DrArmyY1/DrArmyY2/DrLbX1/DrLbX2/DrLbY1/DrLbY2/DrQtyX1/DrQtyX2/DrNameX1/DrNameX2`
（它们本来就是照这一族 prefab 抄的，这次现核**逐位相同**）；新常量只有 `939.6 / 1134`（徽记横带）与图名 `40k_main_bt_nametag`。

---

## 二、新增成员（全在 partial，`ItemDrawer.cs` 零新增行）

`Shell/ItemDrawerPartial_B115S4.cs`：

| 成员 | 作用 |
|---|---|
| `WildcardNametagSprite = "40k_main_bt_nametag"` | `Content/Label` 的底板图（`Resources/Art/ui/` 有） |
| `WildcardEmblemY1 = 939.6f` / `WildcardEmblemY2 = 1134f` | `Content/Image` 横带在根坐标系的上下沿（= 锚 (0,−0.05)-(1,0.13) × 1080） |
| `WildcardEmblemRect(fr)` | 上面那条的 `PxRect` |
| `WildcardLabelRow(content, fr, item, qty, st)` | `Content/Label` 那一行（底板 + `Quantity` + `Name`） |

**为什么 `WildcardLabelRow` 不复用 `DrawerNameQuantity`**：几何**逐位相同**，但原版这一档 `Name` 是
`m_HorizontalAlignment = 1`（**Left**），共用件给的是 Center（按 `Avatar Drawer` 那一族写的）；
共用件不在本批白名单 ⇒ 本档自建，**数字仍照共用常量**（文件头已写，⛔ 别在别处再抄一份几何）。

---

## 三、`ItemDrawer.cs` 的改动形状（硬纪律核对）

| 项 | 读数 |
|---|---|
| 文件行数 | **2497 → 2497**（不变） |
| `git diff --numstat` | **76 / 76**（增删相等 ⇒ 同物理行替换 ✓） |
| 改动范围 | 只落在 **1171…1250**（`Wildcard()` 的 doc + 体）；**1251 行之后逐字节未变**（`difflib` 逐行核过：最后一个变更块在 1250 结束） |
| 账落点 | 该窗口内 6 个锚（`1169 / 1177 / 1257 / 1281 / 1295 / 1297`）**全部原地未动** |
| 行尾 | **LF（`crlf 0 / lf 2497 / cr 0`）** —— ⚠️ 与简报的「CRLF」不符，见 §七-1 |
| `WildcardIcon()` | **未动**（`B114_C_Assert.cs:363-378` 的 F1–F4 前提） |

> ⚠️ 一个必须记下的教训：第一次写盘时我的拼接脚本把整份文件从 LF 写成了 CRLF ⇒
> `git diff --numstat` 立刻变成 **2497/2497**（= 整份重写），当场按二进制 `replace(b'\r\n', b'\n')` 还原后才回到 76/76。
> **判据就是 numstat 那两个数相等**；不相等先查行尾，别先怀疑内容。

---

## 四、三闸读数（都跑过）

| 闸 | 命令 | 读数 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_s4 bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| 账落点 | `python -I -X utf8 工具/check_ledger_points.py`（**没带 `--fix`**） | A 5831 条 · 通过 5831 · **漂移 0 · 失败 0** |
| 聚合行 | `python -I -X utf8 工具/check_aggregate_lines.py <两个文件>` | 聚合行 0 · **违规 0 ⇒ PASS** |

⚠️ `check_aggregate_lines` 对这两个文件报「未发现聚合行（< 20 个匹配）」——
那是**该工具的口径**（它盯的是断言宿主的 `.Run()` 聚合行），不是「我确认过没有代码落在 `//` 之后」。
真正的判据在本节末：**新写的那几行没有一行是追加在既有 `//` 之后的**（整段替换，不是行末追加）。

---

## 五、待接线的断言补丁（`Editor/RewardsScene.cs` §③，**我没动，等写手交回**）

**为什么会红**：那 5 条断的是「奖励格的**主图**被视口裁掉」——它找的是 `ItemDrawer.NodeIcon`
（旧版式里主图叫 `Icon`）。新版式里主图叫 **`Content/Background`** ⇒ `FindChild` 返回 null。
（`MenuCheck.FindChild` 是**深度查找**：`Editor/MenuCheck.cs:141-147` `GetComponentsInChildren` ⇒ `Background` 挂在 `Content` 之下也取得到。）

**替换 1（第 5024–5028 行那 5 行注释，整段换成 6 行；⛔ 也可以只改第 5027–5028 两句，行数就不会变）**

```
                    // ---- ③ 同一格：主图的矩形与 **uv 一起**被截（只截矩形不截 uv 会把图压扁）----
                    // 🔴 **2026-10-17（第四十八会话 · 桶 H1）**：`T21` 已把奖励格改成**抽屉整层画** ——
                    //   根节点名是 `WildcardDrawer`（`ItemDrawer.DrawerWildcard`）。
                    // 🔴 **2026-11-01（`B-293` · `B115_S4`）就地重写**：主图**不再是** `Icon`（`NodeIcon`），
                    //   而是 **`Content/Background`** —— `Wildcard()` 已按 `Wildcard Drawer.prefab` 整版重画
                    //   （底图铺满 `Content` 712×1080 + `m_PreserveAspect 1`）。旧口径
                    //   `min(297.655,384.66)×0.95 = 282.7725`（`Square(box, st.IconFill)`）**已作废**。
```

**替换 2（第 5031 行，一行换一行）**

```
                    var rwEdge = rwDrawer != null ? FindChild(rwDrawer, ItemDrawer.NodeBackground) : null;
```

**替换 3（第 5038 行，一行换一行）**

```
                    var rwIn = rwInDrawer != null ? FindChild(rwInDrawer, ItemDrawer.NodeBackground) : null;
```

其余四行（`rwQ` / `rwInQ` / `fullW` / 三条 `CheckTrue`）**一个字都不用改**。

**红/空转清单（现推，未跑）**：

| 行 | 断言 | 预期 |
|---|---|---|
| 5033 | `rwQ != null && rwQ.Texture != null` | 🔴 **红**（`rwQ` 为 null） |
| 5041 | `fullW > 0f`（前提） | 🔴 **红**（`rwInQ` 为 null） |
| 5044-5045 | `rwQ.WorldW*108 < fullW-1` | ⚪ **空转**（被 `if` 跳过 —— 这正是 `CLAUDE.md` §三 那条「前提不成立时静默跳过」） |
| 5046-5047 | `rwQ.UvRect.width < 0.999` | ⚪ 空转 |
| 5049-5050 | `CheckNear(rwInQ.UvRect.width, 1f, …)` | ⚪ 空转 |

⇒ 打补丁后**这 5 条应当全绿**；「整块宽」的读数会从 **282.77 px** 变成 **≈253.9 px**
（= `712.8 × 384.66/1080`，这是 `HeightControlsWidth` 的必然结果，不是回归）。

---

## 六、要跑哪几条自检 + 进游戏看什么

### 6·1 Unity 自检（⛔ 本代理没跑，交给调度台串行跑）

| 宿主 | 为什么跑 | 预期 |
|---|---|---|
| **`RewardsScene.Run`** | §③ 那 5 条（见 §五）+ 锻造/战役奖励格都在这条宿主里 | **打补丁前**：2 红 3 空转；**打补丁后**：应全绿。若仍红 ⇒ 先看 `Background` 的 uv 是否随视口截（`MenuDraw.Rect` 的 `clip` 解析走 `ViewportClip.Resolve`，`DrawerBackground` 传的是 `st.Clip`＝生产恒 null ⇒ 沿父链找视口节点 ✓） |
| **`ShellScene.Run`** | 战役页 / 奖励窗的外壳链路（`CampaignRewardWindow` 的格子就在这条链上） | 预期绿；若红，先看是不是**别人的在飞改动**（`Shell/ItemDisplayWindow.cs` / `CampaignTab.cs` 当前都有人动过） |
| `ShopScene.Run` | 只有**路由**断言碰 `DrawerWildcard`（`:4200/4209/4331` 断的是 `res.Drawer`，不量几何） | 预期绿（低风险，可不跑） |
| `CollectionScene.Run` / `MainMenuScene.Run` | 若那两页有野牌格（收藏窗的万能卡展示） | 未核是否真画野牌抽屉 —— **跑之前先看结果，别预设** |

⚠️ **改的是共用抽屉 ⇒ 命中的不止一处**：`CampaignRewardWindow`（200×300）、`RewardWindow`（200×300）、
`ForgeTab`（**297.655×384.66**，比例 0.774）。**三处都会变**。

### 6·2 进游戏看什么（★ 必须看，断言管不了观感）

挑一个**带野牌的战役奖励格**（`CampaignData.cs:428-471` 有 13+ 条 `Wildcard<阵营><1..4>`，例：
`WildcardUltramarines1/2/3/4`）。逐条对照：

1. **底图铺满**整格（不再是一个居中方块，四周不再露出背景色）。
2. **阵营徽记在最下沿的那条横带里**（原来是正中一个小方块）—— 现在它**越出卡片下沿 13.9%**，靠父级裁切/画面边缘吃掉，这是原版形状。
3. **名条（`40k_main_bt_nametag`）出现在画面下 15%…25% 那一带**：左边是**数量**（右对齐）、右边是**名字**（左对齐；英文 `Legendary Wildcard`，中文按词条 `MenuShop/RarityWildcard` + `Card_Rarity/*` 取）。
4. **阵营名**（`Armies/*` 词条，如 `极限战士`）在名条**上面**一条，颜色是**灰**（不是白），且**贴着自己那条的下沿**（V=Bottom）。
5. 在锻造格（0.774）里：内容只占**中间 253.9 px**、左右各留 ~22 px 空白 —— **这是原版 ARF 的行为**，不是画错。

---

## 七、遗留 / 顺手发现（⛔ 都没动，交调度台分流）

1. 🔴 **`Shell/ItemDrawer.cs` 是 LF，不是简报说的 CRLF**（现读 `crlf 0 / lf 2497 / cr 0`，`git show HEAD:` 同样是 LF）。
   同族的 `ItemDrawerLayout.cs` 也是 LF。⇒ 后续写手别按 CRLF 去 `wb`，也⛔ 别「顺手统一行尾」（那会把整份文件变成 2497/2497 的 diff）。
2. 🔴 **`Shell/ForgeTab.cs:1203` 的 `st.IconFill = 0.95f`（带 `🔶 自己设计` 标记）现在没有作用了** ——
   它当初写的理由是「原版判据拿不到」，而判据现已取得（prefab 就是铺满）。建议**删掉那一行 + 那条 🔶 标记**；
   ⚠️ 该文件不在本次白名单，**没动**。
3. 🆕 **`Content/Image` 的横带矩形（939.6…1134）现在有两份**：本文件的 `WildcardEmblemRect` 与
   `Shell/ItemDrawerContainer.cs:51` 的内联 `fr.Map(DrCx1, 939.6f, DrCx2, 1134f)`。两者**必须同值**，
   建议下一波收口成 `ItemDrawerLayout.cs` 的共享常量（那份文件不在本次白名单）。
4. 🆕 **`ItemDrawerOptions.cs` 文件头**说「两闸是与关系：`options` ∧ prefab 上有承载件」，
   而 `Wildcard` 的 `showName = false` 却在**泛型基类 `Draw` 里没有消费点**（现读 `decomp_generic_trial/out/ItemDrawer_object___Draw__1ae9cc0.c:80-105`：
   `label` 的显隐只看 `GetItemName` 是否为空）⇒ 我们的 `Name` **照画**是对的。这条已在
   `判据/B115_S4_三份Drawer规格.md` §九-4 记过，这里只提醒别在接线时按 `showName` 把它关掉。
5. ⚠️ **`Placeholder` 兜底路径现在挂在 `Content` 之下**（原来挂在抽屉根）。
   原因：新版式先建 `Content` 再判图，若还在根部画占位板会多一个空 `Content` 兄弟。
   `FindChild` 是深查找 ⇒ 既有断言不受影响；但**如果哪条断言按「根的直接子件」数节点**，要留意。
6. 📌 `Reserved`：`st.IconFill` / `st.ArmyFill` 对 `WildcardDrawer` **已无作用**，但 `Square()` 仍被
   `Icon` / `Placeholder` / `CampaignPoints` 用着 ⇒ ⛔ 别因为「`Wildcard` 不用了」就把 `Square` 删掉。

---

## 断言接线与死常量清理（✅ 已完成 · 2026-11-01 · 尾巴两项）

### 一、改了什么

| # | 文件 | 位置（**现读定位，不是旧行号**） | 改动 |
|---|---|---|---|
| 1 | `Editor/RewardsScene.cs` | §③ 注释块（`// ---- ③ 同一格…` 之下 4 行） | 4 行 → 4 行：写明主图**不再是 `Icon`** 而是 **`Content/Background`**，旧值 `min(297.655,384.66)×0.95 = 282.7725`（`Square(box, st.IconFill)`）**已作废** |
| 2 | 同上 | `var rwEdge = rwDrawer != null ? FindChild(rwDrawer, ItemDrawer.**NodeIcon**)` | → `NodeBackground`（1 行换 1 行） |
| 3 | 同上 | `var rwIn = rwInDrawer != null ? FindChild(rwInDrawer, ItemDrawer.**NodeIcon**)` | → `NodeBackground`（1 行换 1 行） |
| 4 | 同上 | 「整块宽从这一格现量」那句注释尾 | `写死 282.7725 只在锻造格成立` → `⛔ 别写死任何宽度常量`（1 行换 1 行，清掉已作废的引用） |
| 5 | `Shell/ForgeTab.cs` | `BuildRewardIcon` 里 `st.IconFill = 0.95f;`（带 `🔶 自己设计`） | **删掉这句**，同物理行改写成「为什么删 + 判据在哪」的注释（1 行换 1 行） |

**为什么 `NodeBackground` 找得到**：`MenuCheck.FindChild` 是 `GetComponentsInChildren<Transform>(true)`（`Editor/MenuCheck.cs:141-147`）⇒ **深度查找**，`Content/Background` 挂在 `Content` 之下也命中。
**为什么删 `IconFill = 0.95f` 不改变画面**：`Wildcard()` 重写后四层全按 prefab 锚点算，`st.IconFill` / `st.ArmyFill` **在本抽屉已不参与几何**（`ItemDrawer.cs` 里那段 ⑥ 已写明）；这一格只有野牌分支用它。

### 二、numstat / 行数 / 行尾

| 文件 | 行数 | 行尾 | `git diff --numstat` | 判读 |
|---|---|---|---|---|
| `Editor/RewardsScene.cs` | **14,265 → 14,265**（守恒 ✓） | **LF**（`crlf 0 / cr 0`） | **157 / 7** | ⚠️ 这个数**含 `B115_W13` 的未提交改动** ⇒ ⛔ 不能拿它判我这一笔（我拿不到干净的基线：开始改之前没量过）。我的判据是另外两条：**① 文件行数 `14,265` 前后守恒**（协调者给的值 = 我改完的现读）；**② 四处改动全是等行数替换**（4→4 的注释块 + 三处 1→1）⇒ **净增删 = 0 行** |
| `Shell/ForgeTab.cs` | **1,503 → 1,503**（守恒 ✓） | **LF** | **1 / 1** | 同物理行替换 ✓ |

> 🔴 行尾纪律：两份都**先二进制确认是 LF 再写**（`crlf 0 / cr 0 / bareCR 0`），写完再确认一次 —— 上一轮 `ItemDrawer.cs` 就是被脚本悄悄写成 CRLF、numstat 一度变成 2497/2497。
> ⚠️ `RewardsScene.cs` 的 numstat **不能**用「两端相等」判我这一笔 —— 那文件本来就有别人的在飞改动。**我的判据是「行数守恒 + 我改的那 7 行逐个核过」**。

### 三、三闸读数

| 闸 | 命令 | 读数 |
|---|---|---|
| 类型检查 | `TMPDIR=/tmp/wf_s4b bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| 账落点 | `python -I -X utf8 工具/check_ledger_points.py`（**没带 `--fix`**） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| 聚合行 | `python -I -X utf8 工具/check_aggregate_lines.py Editor/RewardsScene.cs Shell/ForgeTab.cs` | 聚合行 0 · **违规 0 ⇒ PASS** |

### 四、跑 `RewardsScene.Run` / `ShellScene.Run` 的预期

**`RewardsScene.Run`**
- §③ 那 5 条**现在应当全绿**（打补丁前是 2 红 + 3 空转）：
  `:5033` 有图 / `:5041` 整块宽前提 / `:5044` 被截 / `:5046` uv 跟着截 / `:5049` 对照 uv = 1。
- 🔴 **唯一会「变数字」的读数**：「整块宽」`fullW` 从旧的 **282.77 px**（`Square(box, 0.95)`）变成
  **≈253.9 px**（= `712.8 × 384.66 / 1080`，`RootFit.HeightControlsWidth` 的必然结果）。
  **这不是回归** —— 原版根宽就是 `0.66 × 高`，格子 297.655 比它宽 ⇒ 左右各留 ~21.9 px 空白。
  三条比较式（`<` / `< 0.999`）都是**相对**比较，不写死绝对值 ⇒ 不受影响。
- 若仍红，按这个顺序查：① `Background` 的 `ImageQuad.Texture` 是否为 null（图没进 `Resources/Art/ui_deck/`）；
  ② `st.Clip` 那一跳是否被解析成视口（`DrawerBackground` 传的是 `st.Clip`，生产恒 `null` ⇒ 走父链 `ViewportClip`）；
  ③ 那一格是不是压根没走 `WildcardDrawer`（`FindChild(cellEdge, "Reward")` 兜底分支）。

**`ShellScene.Run`**
- 预期**全绿**：本批没碰外壳的任何节点名/尺寸，只改了抽屉内部。
- 若红：**先排除别人的在飞改动**（`Shell/ItemDisplayWindow.cs` / `CampaignTab.cs` / `MainMenuRuntime.cs` /
  `LiveOpsHandler.cs` 当前工作树里都有人动过，见 `git status`），别当成这次引入的。

**其余宿主**：`ShopScene.Run` 只断路由（`res.Drawer`）⇒ 预期绿；`MainMenuScene.Run` / `CollectionScene.Run` 若画到野牌格才会受影响（未核是否真画）。

### 五、🆕 下一波待办（⛔ 本波**没做**，收口会动 `ItemDrawerContainer.cs`，不在本次白名单）

> **`Content/Image` 那条「下沿横带」矩形现在有两份实现，必须收口成一份**

| # | 位置 | 写法 |
|---|---|---|
| A | `Shell/ItemDrawerPartial_B115S4.cs` 的 `WildcardEmblemRect(fr)` | `fr.Map(DrCx1, WildcardEmblemY1 /*939.6*/, DrCx2, WildcardEmblemY2 /*1134*/)` |
| B | `Shell/ItemDrawerContainer.cs:51`（`Container()` 里 `Army Icon` 那颗） | 内联 `fr.Map(DrCx1, 939.6f, DrCx2, 1134f)` |

**建议做法**：把 `939.6f / 1134f` 提升成 `Shell/ItemDrawerLayout.cs` 的共享常量（`DrEmblemY1/DrEmblemY2`），
A 与 B 都改引它 —— 这样「两处写同一条规则 = 迟早不一致」那条（`CLAUDE.md` §三）才真正闭合。
⚠️ 改 B 会让 `Editor/B114_W43_Assert` / `B114_W16_Assert` 里那几条 `Container` 的几何断言**仍然绿**（值不变），
但**必须复跑 `CollectionScene.Run` / `ShellScene.Run`**（`Container` 是商店/收藏两侧都在用的档）。
📌 建议由调度台进 `活状态表` 新开一行（本代理无权改那张表）。

---

## Deck Drawer 重写（✅ 已完成 · 2026-11-01 · `B-293` 第 2 项）

> 判据 → `判据/B115_S4_三份Drawer规格.md` §五（`Deck Drawer.prefab` 逐节点现读）。
> ⚠️ **判据那一份 prefab = `-3438027711880944002`**（`DeckDrawer` 类、`gameModeIcon` **非** null、
> `background`/`quantity` 槽 **null**）；全库另两个同名 `Deck Drawer` 是 `DeckAndCardbackDrawer` 且 `gameModeIcon` null
> ⇒ **按字段列表认，⛔ 别按类名**（`B109_E11` 已订正过这一点，这次再核一遍仍成立）。

### 一、对照表（原版锚点 / 槽位 vs 改后）

| 层 | 原版（`Deck Drawer.prefab`） | 改前 | 改后 |
|---|---|---|---|
| 帧 | 根 `ARF 2 · 0.66` ⇒ **根宽 = 0.66 × 根高**、左右居中；`Collection Deck`(328×496.97) 由 `UIScaleToFit` **缩满整根**（×2.173166） | `DeckAnchor(box, …)` —— 乘 **`box`**（框比例 ≠ 0.66 就横向拉伸） | `RootRectOf(box, fit: HeightControlsWidth)` 当根，锚点乘**根矩形** |
| `Content` | 锚 (0,0)-(1,1) 拉伸 + `sizeDelta 0` ⇒ **= 整根** | **没有这一层**（所有层直接挂抽屉根） | `MenuDraw.Node(node, NodeContent, root)` |
| `Deck Image(mask)` | (0.044,0.18)-(0.956,0.93547) + **`RectMask2D`** | 只把它当主图的框 | 当 **mask**：主图画在**过扫**矩形里、裁到这个框 |
| `Deck Image`（主图） | 相对 mask **过扫** (−0.265,−0.067)-(1.26,1.065)、`m_PreserveAspect 0` | 在 **mask 框**里整张压扁（`keepAspect: false`） | 画在**过扫**矩形（353.09×328.96 @ 297.655 格）+ 裁到 mask（231.54×290.60） |
| `Frame` | (0,.04935)-(1,.958) · `sizeDelta.x −4.0127` · PA=0 | 同（乘 box） | 同（乘 root） |
| `Faction Icon` | (−.042,.11253)-(.296,.32425) · PA=1 | 同（乘 box） | 同（乘 root） |
| `Game Mode Icon` | (.682,.11253)-(1.02,.32425) · PA=1 | 同（乘 box） | 同（乘 root） |
| `Deck Name` | (.08,.052)-(.92,.15) · fs **32** · base **30** · auto **[8,32]** · **V = Capline** | fs 32×k（对）、**无自适应 / 无竖直档**、`wrapPx = box.W` | fs/base/auto 三格全传 + `SetVAlign(Capline)` + `wrapPx = 名框宽` |
| `quantity` 槽 | **null ⇒ 原版不画数量** | `if (QuantityPx>0 && Stackable(item)) Quantity(...)`（`PrebuiltDeck.stackable=1` ⇒ **会画**） | **整句删掉** |
| `background` 槽 | **null ⇒ 无底图** | 同（本来就没画） | 同 |

**新增成员（全在 partial）**：`DeckMaskClip(parent, maskR, st)` —— 返回 `maskR ∩ 外层视口`。
🔴 **为什么不直接传 `maskR`**：`MenuDraw.Rect` 的 `clip` 是**显式覆盖**（`ViewportClip.Resolve` 第 ① 条），
直接传会把**外层滚动视口的裁切丢掉** ⇒ 先 `Resolve` 父链那一份、与 `maskR` 求交；无外层裁切时结果就是 `maskR`。

### 二、`ItemDrawer.cs` 的改动形状

| 项 | 读数 |
|---|---|
| 改动区域 | `Deck()` 的 doc + 体（**1764…1821**，共 58 行）—— **58 行 → 58 行** |
| 文件行数 | **2497 → 2497** |
| `git diff --numstat` | **100 / 100**（增删相等 ✓；含上一轮 `Wildcard` 的 76/76 累计） |
| 行尾 | **LF**（`crlf 0 / cr 0`），写前写后各确认一次 |
| 账锚 | `Deck` 的签名行**必须钉在 1768**（`账/卡组编辑.tsv` 有 3 条 `DeckDrawer.*` 落点在这一行）。第一次写盘时 doc 少了一行、签名掉到 **1767**，闸立刻报 **漂移 3**（判成「`Deck` 有 2 处声明 1740/1767 ⇒ 歧义」）；补一行 doc、从体内删掉一个空行 ⇒ **回到 1768、漂移归零** |

### 三、三闸读数（都在最后一笔 `.cs` 之后）

| 闸 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s4c bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `check_ledger_points.py`（**没带 `--fix`**） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| `check_aggregate_lines.py <两个文件>` | 聚合行 0 · **违规 0 ⇒ PASS** |

### 四、待接断言（**可粘贴**，宿主选 `ShellScene`）

**为什么选 `ShellScene`**：① **最便宜的宿主**（实测 **9 s**，`CollectionScene` 要 4 m 20 s）；
② 抽屉属于「壳 / menu 的物品格」这一族，`ShellScene` 就是这条链的宿主；③ 整块**自带夹具**（自己建 `GameObject`、自己销毁），
**贴到任何宿主都能跑** —— 只有 `CheckTrue` / `Section` 两个断言口是宿主本地的（`ShellScene` 现成就有：
`static void CheckTrue(bool, string)` / `static void Section(string)`）。
📌 **备选（零新夹具）**：`Editor/DeckScene.cs:10287-10299` **已经有**一份一模一样的 `PrebuiltDeck` 夹具，
把那 4 条 `CheckTrue` 贴到 `view` 建出来之后即可。

```csharp
        // ══ B-293 · `Deck Drawer`：按 `Deck Drawer.prefab`（`PrebuiltDeck` 那份 = `-3438027711880944002`）逐层 ══
        Section("B-293 `Deck Drawer`：根帧 0.66 / Content / 主图过扫+裁切 / 不画数量");
        {
            var probe = new GameObject("B293_DeckDrawer_probe");
            try
            {
                var all = PrebuiltDecks.All;
                CheckTrue(all != null && all.Count > 0, "（前提）`PrebuiltDecks.All` 里有预组卡组");
                PrebuiltDecks.Deck deck = null;
                if (all != null) for (int i = 0; i < all.Count; i++) if (all[i] != null) { deck = all[i]; break; }
                CheckTrue(deck != null, "（前提）取到一副非空预组");
                if (deck != null)
                {
                    // 🔴 夹具框取**锻造格** 297.655×384.66（比例 0.774 ≠ 根的 0.66）—— 旧写法乘 box 时这里才露馅；
                    //    ⛔ 别换成 200×300（比例 0.667 ≈ 0.66，帧模型对不对看不出来）。
                    var cell = new PxRect(0f, 0f, 297.655f, 384.66f);
                    var spec = new ItemSpec
                    {
                        Id = deck.deckId, Kind = ItemKind.Generic, Type = "PrebuiltDeck",
                        Deck = deck, WithCardback = false, Label = deck.DisplayName,
                    };
                    var res = ItemDrawer.Draw(probe.transform, cell, spec, 1, DrawerOverride.Default,
                                              ItemDrawerStyle.Default(10, 11, 12));
                    CheckTrue(res.Node != null && res.Drawer == ItemDrawer.DrawerDeck, "（前提）走到 `DeckDrawer`");
                    if (res.Node != null)
                    {
                        // ① 帧：根宽 = 0.66 × 高 ⇒ `Content` 宽 253.876（框是 297.655）
                        var content = MenuCheck.FindChild(res.Node, ItemDrawer.NodeContent);
                        CheckTrue(content != null, "① `Content` 建出来了（旧写法整层没有）"
                                                 + "｜🧨 删掉 `MenuDraw.Node(node, NodeContent, root)` ⇒ 红");
                        if (content != null)
                        {
                            float cw = ((RectTransform)content).sizeDelta.x;
                            CheckTrue(Mathf.Abs(cw - 253.876f) < 0.5f,
                                      $"① 根帧 = 0.66 × 384.66 ⇒ `Content` 宽 **{cw:F2} ≈ 253.88**（框 297.655）"
                                    + "｜🧨 仍乘 `box` ⇒ 得 297.655 ⇒ 红");
                        }
                        // ② 不画数量（判据那份 prefab 的 `quantity` 槽是 null）
                        CheckTrue(MenuCheck.FindChild(res.Node, ItemDrawer.NodeQuantity) == null,
                                  "② 不建 `Quantity`（`quantity` 槽 null ⇒ 原版不画）"
                                + "｜🧨 把那句 `Quantity(...)` 加回来 ⇒ 红");
                        // ③ 主图 = **过扫**矩形（353.09 × 328.96），不是 mask 框（231.54 × 290.60）
                        var heroNode = MenuCheck.FindChild(res.Node, ItemDrawer.NodeDeckImage);
                        var hq = heroNode != null ? heroNode.GetComponentInChildren<ImageQuad>() : null;
                        CheckTrue(hq != null && hq.Texture != null, "③（前提）主图建出来了且有图");
                        if (hq != null)
                            CheckTrue(Mathf.Abs(hq.WorldW * 108f - 353.09f) < 2f,
                                      $"③ 主图宽 **{hq.WorldW * 108f:F2} ≈ 353.09**（= 1.525 × mask 231.54，即**过扫**）"
                                    + "｜🧨 画在 mask 框里 ⇒ 231.54 ⇒ 红（原版是「拉伸到过扫框再裁」）");
                    }
                }
            }
            finally { UnityEngine.Object.DestroyImmediate(probe); }
        }
```

**期望值怎么来的**（`K = 384.66/1080 = 0.356167`；根 = 253.876 × 384.66，左右各留 21.89）：
`mask = (0.044,0.18)-(0.956,0.93547)` ⇒ **231.535 × 290.599**；`hero = mask` 再按 `(−0.265,−0.067)-(1.26,1.065)` 过扫 ⇒ **353.09 × 328.958**；
`name` 框 = (42.20, 326.96)-(255.46, 364.66)；`Frame` 宽 = 253.876 − 4.0127 = **249.863**。

⚠️ 三条断言各**灭一种回退**（`CLAUDE.md` §三「灭自证」）：① 帧模型 · ② 数量槽 · ③ 过扫 + 裁切。**改坏任一处，对应那条单独红**。

### 五、宿主与预期

| 宿主 | 为什么 | 预期 |
|---|---|---|
| **`ShellScene.Run`** | 贴上 §四 那块之后的落点（9 s，最便宜） | 新增 6 条全绿（4 条真断言 + 2 条前提） |
| `DeckScene.Run` | **现成就有**同一份 `PrebuiltDeck` 夹具（`:10287-10299`），在 `view` 之后加同样 4 条即可 | 加完应全绿；不加则**不受影响**（它只断路由与点击闸，不量几何） |
| `CollectionScene.Run` / `ShopScene.Run` | 都不画 `PrebuiltDeck`（数据里 0 条发放物） | 与本笔无关，预期不变 |

🔴 **今天 0 个 `PrebuiltDeck` 实例**（`数据/游戏数据/*.json` 里 0 条发放物）⇒ **画面上根本看不到这个抽屉**，
只能靠 §四 的断言验收 ⇒ 那 4 条是**唯一**的验收口，⛔ 别省。

### 六、遗留

1. 🆕 节点名仍是旧的（`NodeIcon` = `Icon`、`NodeDeckFrame` = `Deck Frame`、`NodeDeckFaction` = `Deck Faction Icon`、
   `NodeDeckGameMode` = `Deck Game Mode Icon`、`NodeItemName` = `ItemName`），而 prefab 里分别叫
   `Deck Image` / `Frame` / `Faction Icon` / `Game Mode Icon` / `Deck Name`。
   **本波没改名**（不在"三处小改"里，且 `NodeItemName` / `NodeIcon` 是**全库共用**常量，改它们会波及其它抽屉）
   ⇒ 要按 prefab 逐字得**另开一笔**（加 deck 专属常量，⛔ 别动那两个共用常量）。
2. 📌 `Content/Image` 横带那两份（`WildcardEmblemRect` 与 `ItemDrawerContainer.cs:51`）**仍未收口** —— 按调度台要求留给 **`B-315`**，本波没动 `ItemDrawerContainer.cs`。
3. ⚠️ `DeckMaskClip` 依赖 `ViewportClip.Resolve` 的**父链解析**；若将来有人给 `Rect(...)` 传 `MenuDraw.NoClip` 哨兵，
   这条求交会得到「无裁切」而不是 mask —— 当前没有调用点这么传（`Deck()` 传的是我们自己算的交集）。

---

## 节点名对齐与 Base 结论（✅ 2026-11-01 · `B-316` + `B-293` 收尾）

### 一、活 1 · `Deck Drawer` 节点名按 prefab 逐字

| 层 | 旧名 | 新名（= prefab 逐字） | 落在哪 |
|---|---|---|---|
| 主图 | `NodeIcon` = `Icon`（**全库共用**） | **`NodeDeckImage` = `Deck Image`** | **新增**在 `ItemDrawerPartial_B115S4.cs` |
| 名字 | `NodeItemName` = `ItemName`（**全库共用**） | **`NodeDeckName` = `Deck Name`** | **新增**在 `ItemDrawerPartial_B115S4.cs` |
| 卡框 | `NodeDeckFrame` = `Deck Frame` | `NodeDeckFrame` = **`Frame`** | 原地改**值**（`ItemDrawer.cs:1854`） |
| 阵营徽记 | `NodeDeckFaction` = `Deck Faction Icon` | `NodeDeckFaction` = **`Faction Icon`** | 原地改**值**（`:1855`） |
| 模式图标 | `NodeDeckGameMode` = `Deck Game Mode Icon` | `NodeDeckGameMode` = **`Game Mode Icon`** | 原地改**值**（`:1856`） |

⛔ **`NodeIcon` / `NodeItemName` 一个字都没动**（它们是全库共用的常量，`Placeholder` 等还在用 —— `:1320` 那处仍是 `NodeItemName`）。

**为什么有三个是「原地改值」而不是「在 partial 里新开常量」**：这三个常量**只服务 deck 这一档**（外部零引用，见下），
再在 partial 里开一份同名新常量 = **两处写同一条规则**（`CLAUDE.md` §三），而且旧的那三个会变成**死常量**。
⇒ 只把**真正需要新名字**的两个（那两个共用常量不能改）放进 partial，其余三个原地改值。

🔴 **改之前按调度台的命令查过旧名引用**：
```
grep -rn "Deck Frame\|Deck Faction Icon\|Deck Game Mode Icon" --include=*.cs --include=*.json D:/4/Unity/MyGame/Assets
```
⇒ **命中只有那 3 行声明本身**（`Shell/ItemDrawer.cs:1854-1856`），**零外部引用** ⇒ 改名不需同步改任何断言。

⚠️ **顺带查到的、要留个印象的一条**：`"Deck Image"` 与 `"Deck Name"` 这两个名字**在本工程别处已经存在** ——
`CollectionScene` 的卡组格子树（`FindChild(cell, "Deck Name")` / `"Deck Image"`，`:1710/2028/7842/7860` …）
与 `B103F6_Assert.cs:169`。那是 **`CollectionDeck` prefab 自己的节点**，与抽屉无关；
它们都在**各自的子树**里 `FindChild`，不会串到 `ItemDrawer` 的 drawer 子树 ⇒ 无碰撞。
📌 但以后若有人写「全树深搜 `"Deck Name"`」，要记得这两处是**不同 prefab 的同名节点**。

### 二、活 2 · `Base Drawer` 正式结论已写进判据文件

`判据/B115_S4_三份Drawer规格.md` 末尾新增 **`## Base Drawer 结论（正式）`** 一节，含：
- **一句话结论**：prefab 模板、运行时无路径会画它 ⇒ ⛔ 不写渲染器（写了就是死代码）；
- **三条可复跑复核**：① `assets_full` 只有它自己 **1 份** `GameObject`；② `ExportedProject` 全量 `.prefab/.unity` 搜它的 guid `00198b7704fdefb48a5d6b695d93501f` = **0 命中**；③ `dump.cs` 无 `BaseDrawer` 类；
- **口径**：`B-293` 的 Base 项 = 不产出代码；`Placeholder` **不是**它在画；要「像基类」是 `Placeholder` 重构、另立账；
- **作废条件**：若将来新 build 里②搜得出命中，该节作废、按 §三 的规格做；
- ⚠️ 节首写明「**以后谁想实现 `Base Drawer` 渲染器，先看这一节**」。

### 三、三闸读数 + 形状

| 项 | 读数 |
|---|---|
| `ItemDrawer.cs` 行数 / 行尾 | **2497 → 2497** · **LF**（`crlf 0 / cr 0`） |
| `git diff --numstat`（`ItemDrawer.cs`） | **104 / 104**（增删相等 ✓） |
| **`Deck()` 签名行实测** | `sed -n '1768p'` = `        static void Deck(Transform node, PxRect box, ItemSpec item, int qty, ItemDrawerStyle st, ref ItemDrawResult res)` ✓ **仍在 `:1768`**（账钉着的那 3 条 `DeckDrawer.*` 落点不动） |
| 类型检查 `TMPDIR=/tmp/wf_s4d` | 运行时 **0** · 编辑器 **0** |
| `check_ledger_points.py`（无 `--fix`） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| `check_aggregate_lines.py` | 聚合行 0 · **违规 0 ⇒ PASS** |
| `ItemDrawerPartial_B115S4.cs` | 现 **119 行**（新增 `NodeDeckImage` / `NodeDeckName` 两个常量 + 各自的 doc） |

### 四、对宿主的影响

- **零影响**：这次只改节点名（字符串），不改任何几何/层数/顺序。
- §四 那块**待接线断言**里用到的 `ItemDrawer.NodeQuantity`（没变）与 `ItemDrawer.NodeContent`（没变）**不受影响**；
  但如果那条断言将来改成按主图节点名找，要用 **`NodeDeckImage`**（不是 `NodeIcon`）。
  📌 §四 的断言块**已经**用 `ItemDrawer.NodeDeckImage` 找主图（本次改名后同步改的）—— ⛔ 别再写回 `NodeIcon`，
  那个名字在 `DeckDrawer` 的子树里**已经不存在**了（它是其它抽屉主图的名字）。
- `ShellScene.Run` / `DeckScene.Run` 的既有断言：**都不按这三个旧名找节点**（已核，零引用）⇒ 预期不变。

---

## B-315 横带矩形收口（✅ 2026-11-01）

> 起因 = 我上一轮自己查出来的「`Content/Image` 那条下沿横带**有两份实现**」。
> 判据 → `判据/B115_S4_三份Drawer规格.md` §四·2 / §五·2（`Wildcard Drawer` 的 `Content/Image` 与
> `Container Drawer` 的 `Content/Army Icon` **是同一条矩形**：锚 `(0,−0.05)-(1,0.13)` × 1080 = `939.6…1134`）。

### 一、三处改动

| # | 文件 | 改动 |
|---|---|---|
| 1 | `Shell/ItemDrawerLayout.cs`（+6 行，**只加**） | 新增 **`const float DrEmblemY1 = 939.6f, DrEmblemY2 = 1134f;`** + 3 行 doc（记「锚 (0,−0.05)-(1,0.13) × 1080 ⇒ 939.6 / 1134、越过 `Content` 下沿 54 px、两档同一条」）。**⛔ 既有常量一个值都没动**（`DrImgY1/DrImgY2` / `DrArmyY1/Y2` / `DrLb*` / `DrQty*` / `DrName*` / `DrCx1/Cx2` 全部原样）。 |
| 2 | `Shell/ItemDrawerPartial_B115S4.cs`（119 → 117 行） | `WildcardEmblemRect` 改用 `DrEmblemY1/Y2`；**删掉**本文件里那份 `WildcardEmblemY1/Y2`（否则就是第三份） |
| 3 | `Shell/ItemDrawerContainer.cs:51`（**行数不变** 64 → 64） | `fr.Map(DrCx1, 939.6f, DrCx2, 1134f)` → `fr.Map(DrCx1, DrEmblemY1, DrCx2, DrEmblemY2)`（同物理行，行尾追加一句来源注释） |

**全仓 `939.6f` / `1134f` 现在的分布（现读）**：
`ItemDrawerLayout.cs:137`（**唯一来源**）· `ItemDrawerPartial_B115S4.cs:31`（doc 里的引用，不是字面量）·
外加两处 **`1134f`** —— `ItemDrawerPartial_B111R3W.cs:216` 与 `ItemDrawerRandomCard.cs:77`，它们的算式是
`fr.Map(DrCx1, **0f**, DrCx2, 1134f)`（`Random Image` 从 **y=0** 起），**与这条横带不是同一个矩形**
（只是右边界数字碰巧相同）⇒ **没动**，也⛔ 不该改成 `DrEmblemY2`（语义会错）。

### 二、数值零变化的证明（四边界**逐位**相同）

`DrawerFrame.Map(x1,y1,x2,y2) = PxRect(OX + x1·K, OY + y1·K, OX + x2·K, OY + y2·K)`，全程 `float32`。
新常量与旧字面量是**同一批 IEEE-754 单精度位型**：

```
旧字面量 (0.4f, 939.6f, 712.4f, 1134f) 位型 = 3ecccccd 446ae666 4432199a 448dc000
新常量   (DrCx1, DrEmblemY1, DrCx2, DrEmblemY2) 位型 = 3ecccccd 446ae666 4432199a 448dc000
```

三组真实输入下，改前/改后算出的矩形四个边界**二进制位型完全相同**：

| 用例 | `K` / `OX` / `OY` | 改前矩形 (x1,y1,x2,y2) | 改后矩形 | 位型 | 四边界逐位相同 |
|---|---|---|---|---|---|
| A 根 712.8×1080 @ 原点 | `1` / `0` / `0` | 0.4000000059604645 · 939.5999755859375 · 712.4000244140625 · 1134.0 | 同左 | `3ecccccd 446ae666 4432199a 448dc000` | ✅ |
| B 锻造 / Deck 格 297.655×384.66 | `0.356167` / `21.8896…` / `0` | 22.032167434692383 · 334.6541748046875 · 275.6228332519531 · 403.89300537109375 | 同左 | `41b041e1 43a753bc 4389cfb9 43c9f24e` | ✅ |
| C 战役奖励格 200×300 | `0.277778` / `0.555…` / `0` | 1.1111111640930176 · 261.0 · 198.88890075683594 · 315.0 | 同左 | `3f8e38e4 43828000 4346e38f 439d8000` | ✅ |

（`939.6f` 的真值就是 `939.5999755859375`（float32 不能精确表示 0.6 的小数），改前改后同值 —— **不是** 939.6 本身。）

⇒ **像素零变化**：两处调用点拿到的 `PxRect` 与改前**逐位相同**，`MenuDraw.Rect` 的输入不变。

### 三、三闸读数

| 闸 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s4e bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `check_ledger_points.py`（无 `--fix`） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| `check_aggregate_lines.py`（三个文件） | 聚合行 0 · **违规 0 ⇒ PASS** |

⚠️ **一次假红，记一下**：第一次跑 typecheck 时报了 4 条运行时报错，**全部落在 `Assets/RuleEngine/Core/EnchantmentExecutor_B115.cs`**
（另一个代理的在飞文件，`ChosenBuffHand` / `RunAbilityBuffRowOfEnchantment` 未定义）+ 编辑器那半边 `CS0006 找不到 WFCheck.dll`
（= 运行时编不过的**级联**）。**我三个文件一条错都没有**；隔一次重跑就 **0 / 0**。
⇒ 正是 `CLAUDE.md` 里那条「错误全部集中在不是你负责的文件上 ⇒ 那不是你的问题，隔一会儿重跑」。

**账落点前置核查**（调度台要求）：`grep -oh "ItemDrawerContainer\.cs:[0-9]*" 账/*.tsv | wc -l` = **0**，
`ItemDrawerLayout.cs` 同样 = **0** ⇒ 两个文件都没有 A 档落点，改它们不会撞账（`ItemDrawerContainer.cs` 仍是**行数守恒** 64→64）。

### 四、要跑的宿主与预期

**画面零变化 ⇒ 预期全绿，且不该有任何数值断言变红。**

| 宿主 | 为什么 |
|---|---|
| **`CollectionScene.Run`** | `Container Drawer` 在收藏/商店两侧都出现，是这次**唯一**被改到生产路径的档（`ItemDrawerContainer.cs`） |
| **`ShellScene.Run`** | 商店格 / 菜单物品格同样走 `Container`（9 s，最便宜的一道） |

📌 `Wildcard` 那半边（`WildcardEmblemRect`）**只换了常量来源**，矩形不变 ⇒ `RewardsScene.Run` 的 T21 那 5 条**行为不变**
（它们现在找的是 `Content/Background`，本来就不碰这条横带）。
⚠️ 如果 `CollectionScene.Run` / `ShellScene.Run` 有**像素级**的差（不该有），按 §二 的三组位型先对一遍再怀疑代码。

---

## 空引用修复（✅ 2026-11-01 · `Wildcard` 重写引入的崩溃）

> 现象：`RewardsScene.Run` **654 通过 / 1 失败**，`RunCore` 中途抛 `NullReferenceException` ⇒ 其后断言段没跑（读数不完整）。
> 栈：`ForgeTab.BuildRewardIcon`（`ForgeTab.cs:1206`）→ `ItemDrawer.Draw` → `Dispatch` → `Wildcard`（`ItemDrawer.cs:1243`）
> → **`WildcardLabelRow`（`ItemDrawerPartial_B115S4.cs:54`）**。日志 → `_tmp_view/w115_rewards.log:1211320`。

### 一、根因（**不是**「content 为 null」，是 `MenuDraw.Rect` **按契约返 null**）

`MenuDraw.Rect`（`MenuDraw.cs:1624`）有**三条**返 null 的路，本次命中的是第 ② 条：

| # | 位置 | 条件 | 本次 |
|---|---|---|---|
| ① | `if (tex == null) return null;` | 贴图取不到 | ❌ 不成立（本函数进门先 `Tex(...)` 判过 `tag != null`） |
| ② | `if (!ClipRect(new PxRect(x1,y1,x2,y2), clip, out cr)) return null;` | **整块落在裁切区外 / 退化 / 薄交集** | ✅ **就是它** |
| ③ | `if (quad == null) return null;` | `ImageQuad.Create` 建不出（退化尺寸） | ❌ 本格 `labelR` = 640.8K × 108K ≈ 228×38.5，不是退化 |

`ClipRect`（`MenuDraw.cs:269-289`）返 **false** 的三个条件：`r.W/H ≤ 0.01` · `!Visible(r, clip)`（**完全无交集**）· 薄交集 < 0.01 px。
`clip` 从哪来：`st.Clip` 是 `null`（`ItemDrawerStyle.Default`）⇒ `ViewportClip.Resolve(parent, ...)` 沿父链找到锻造页滚动区的 `ViewportClip` 节点。

**为什么会在「刷新一整页」时命中**：`ForgeTab.SelectArmy("AstraMilitarum")` → `Refresh()` → `BuildRewardCells()` → **对每一格都调 `BuildCell(i)`**
（`ForgeTab.cs:868/1024`），**包括滚出视口的那几格** ⇒ 那些格子的 `labelR` 与视口**没有交集** ⇒ `ClipRect` 返 false ⇒ `Rect` 返 null。

**为什么之前不崩**：同一条路上其它画法**都不解引用 `Rect` 的返回值** ——
`DrawerBackground`（`ItemDrawerLayout.cs:258`）只 `MenuDraw.Rect(...)` 当语句；`DrawerNameQuantity`（`:295`）用的是 **`MenuDraw.Node`**（恒非 null）；
旧版 `WildcardIcon` 里那句也是语句。**只有我新写的这一行把返回值当成了「恒非 null」**（`.transform` 硬解）⇒ 全仓唯一一处这么写的。

### 二、改法（**不是**「加个 null 检查把异常吞掉」）

```csharp
var labelQuad = MenuDraw.Rect(content, tag, labelR, "Label", st.QBoard, null, false, st.Clip, st.ClipSoftness);
if (labelQuad == null) return;      // 这一行三层（底板/数量/名字）共用同一个 `labelR` ⇒ 一起不建
row = labelQuad.transform;
```

判据理由：
1. `Rect` 返 null **不是异常态**，是 `ClipRect` 判「整块在裁切区外」的**正常契约**（滚动区里天天发生）⇒ 正确处理是**「连节点一起不建」**，
   这与 `Rect` / `Nine` / `Tiled` / `Hit` / `Text` **五条画路**的既有口径一致（`MenuDraw.Text` 那段注释原话：
   「整块在视口外 ⇒ 连节点一起不建」）。
2. 三层的矩形**同一个** `labelR` ⇒ 底板不可见时，`Quantity` / `Name` 也必然不可见（`ClippedText` 里的 `VisibleAbove` 会给出同一判断）
   ⇒ 早退**不会漏画**，也不会多建一个空 `Label`。
3. **不建空节点**比「建一个空容器」更贴近原版（原版 `RectMask2D` 也是整块不渲）。
4. 底板图**取不到**（`tag == null`）那一支**保持原样**：仍然建 `Label` 容器 + 照画文字 + 出声（那是「缺图」，与「在视口外」是两件事）。

**代码位置**：`ItemDrawerPartial_B115S4.cs` 的 `WildcardLabelRow`（`ItemDrawer.cs` 这一波**一个字节没动**，`Deck()` 签名仍在 `:1768`）。

### 三、哪些调用点会走到这条支

`ItemKind.Wildcard` → `PickDrawer` 返 `DrawerWildcard` → `Dispatch` → `Wildcard()` 的三条生产路径**全部**会走到 `WildcardLabelRow`：

| 调用点 | 场景 | 会不会命中 null |
|---|---|---|
| **`Shell/ForgeTab.cs:1206`**（`BuildRewardIcon`） | 锻造页奖励格（**夹具框 297.655×384.66**，滚动列表） | ✅ **本次崩的就是它**（滚出视口的格） |
| `Shell/CampaignRewardWindow.cs:969` | 战役奖励窗的格子（200×300，也在滚动区里） | ✅ 同样会（只要格子滚出视口） |
| `Shell/RewardWindow.cs:1435` | 战斗奖励窗的格子 | ✅ 同样会 |

⇒ 这一处修好 = **三条路径一起修好**（不是只修锻造页）。另外三条路径走的是**同一个** `Wildcard()`，没有第二份实现。

### 四、自查：还有没有同类的「解引用 `Rect` 返回值」

`grep -rn "MenuDraw.Rect(.*)\.\(transform\|GetComponent\)" Assets/` ⇒ **0 命中**（修完这一处之后）；
`Wildcard()` 里画徽记那句、`Deck()` 里五句画图**都是语句形式**（不解引用）⇒ 无同类风险。
`MenuTutorialHighlight.cs:93` 是 `h._quad = MenuDraw.Rect(...)` 先存字段、用之前另有判空 —— 不是同类。

### 五、三闸读数

| 闸 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s4f bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `check_ledger_points.py`（无 `--fix`） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| `check_aggregate_lines.py` | 聚合行 0 · **违规 0 ⇒ PASS** |
| `ItemDrawer.cs` | **本轮未改**（`numstat` 仍 104/104 · 2497 行 · `:1768` = `static void Deck(...)` ✓） |
| `ItemDrawerPartial_B115S4.cs` | 127 行 |

### 六、`RewardsScene.Run` 预期

- **异常不再抛** ⇒ `RunCore` 能跑完 ⇒ 断言总数应回到 **3,088+** 量级（不再是截断的 654）。
- T21 §③ 那 5 条（已接 `NodeBackground`）应全绿。
- 若仍有红：**先看是不是别的档**（`Container` / `CampaignPoints` …），别默认是这次引入的 —— 本轮只改了 `WildcardLabelRow` 的一个早退。

---

## A302 探针改瞄准（✅ 2026-11-01）

> 失败的断言：`Editor/RewardsScene.cs` 的 `A302` 探针 ——「★ …而且**确实被夹在 1920 上**（实测 1878.1）」。
> 前置：`RewardsScene.Run` 已从截断的 654 恢复到 **3,109 通过 / 1 失败**，只剩这 1 条。

### 一、为什么它会红（版式是对的，探针的落点假设过期了）

新版式照 prefab：`Content/Label` 里 **左半 `Quantity`（右对齐）/ 右半 `Name`（左对齐）**。
于是 `Quantity` 的右对齐落点 = `OX + DrQtyX2·K`（`DrQtyX2 = 351.4`），**不再是** `box.x2 − 10`。

旧盒子 `(1780,400,1980,700)`（200×300）下：`K = 300/1080 = 0.277778`、`rootW = 712.8K = 198`、`OX = 1880 − 99 = 1781`
⇒ `Quantity` 右沿 = `1781 + 351.4×0.277778 = **1878.6**` —— 与探针实测的 **1878.1** 只差 0.5 px（= 字形右 bearing，`TmpVertPx` 量的是字墨不是框边）。
**1878.6 < 1920** ⇒ 这条旧盒子下**永远够不到**带口 ⇒「必须被夹在 1920 上」的前提不成立。

### 二、改法：**改盒子，不改版式**（⛔ 版式是照 prefab 的，一个像素都没动）

| 项 | 改前 | 改后 |
|---|---|---|
| 探针盒子 | `PxRect(1780, 400, 1980, 700)` | **`PxRect(1871, 400, 2071, 700)`** |
| 数量 | `3` | **`123456`**（6 位） |
| 目标层 | `Quantity`（不变） | `Quantity`（不变） |
| 两条断言 | `pmx ≤ 1920.5` ∧ `pmx ≥ 1919.5` | **一字未改**（⛔ 没放宽、没删） |

新盒子下：`OX = 1971 − 99 = 1872` ⇒ `Quantity` 右沿 = `1872 + 97.611 = **1969.6**`（跨过 1920 共 **49.6 px**）。

### 三、🔴 为什么新目标**仍然是 `Quantity`**，而不是 `Name`（调度台建议的那颗）

我核过 `Name`（右半，`x1 ≈ 1881 / x2 ≈ 1962`，确实跨 1920）**也能跨**，但它**不能当回归探针**：

| | `Name`（右半） | `Quantity`（左半） |
|---|---|---|
| 对齐 | **左对齐**（`m_HorizontalAlignment 1`） | **右对齐**（`4`） |
| 右沿落点 | = `x1 + 文字宽` ⇒ **取决于文字宽度** | = **框右沿，与文字长短无关** |
| 文字宽由谁定 | `ClippedText(wrapPx = 名框宽)` + **自适应 + 折行** ⇒ 会被压进框内 ⇒ 实际右沿**不确定**（且中英文串长不同） | 自适应只改字号、**不改右对齐锚点** |
| 文本可控性 | `WildcardName()` 生成（跟随稀有度/词条），探针改不了 | 由探针的 `qty` 直接控制 |

⇒ **左对齐 + 自适应/折行 = 右沿不可预测**，做成断言的期望值就是「拿一次实测当基准」，下一轮换语言/换字号就会飘。
**右对齐的 `Quantity` 右沿是几何恒等式**（`OX + DrQtyX2·K`），这是可写进断言的确定量。
所以：**保留 `Quantity`，改盒子** —— 改的是 fixture，不是被测语义。

**关于旧注释那句「⛔ 别把 box 右移去凑 1920」**：那条警告针对的是「移到文字**刚好擦边**」（未裁切时只差 1 px，容易误判）。
本次是**按 prefab 算式重算落点**、让文字跨过 1920 共 **49.6 px** ⇒ 区分度**更大**而不是更小（见下面的鉴别力表），所以那条禁令在这里不适用；
相应的解释已写在代码注释里（原注释已就地改写成当前口径，行数不变）。

### 四、两条断言的鉴别力自证

| 情形 | `pmx` | `≤ 1920.5` | `≥ 1919.5` | 判定 |
|---|---|---|---|---|
| **正常（裁切生效）** | **1920.0**（`ClipText` 把顶点夹到带口） | ✅ | ✅ | 绿 ✔ |
| 🧨 抽屉文字**没接裁切**（本探针要防的那个 bug） | **1969.6** | ❌ | ✅ | **红** ✔ |
| 🧨 文字太短/字号被压到够不着边（上面那条会**恒真**） | `≈1889+字宽` | ✅ | ❌ | **红** ✔ |
| 🧨 有人把 `Quantity` 改回居中/左对齐 | 落点左移 | ✅ | ❌ | **红** ✔ |

⇒ 两条**成对**才有意义：单留第一条，对一段「本来就没越界」的短文字恒真；单留第二条，对「裁切没接但文字够长」恒真。两条都留 = **只有「真被截」才同时成立**。

### 五、对照组 `pr2` 仍然成立（顺手核过）

`pr2` 盒子 `(2000,400,2200,700)` ⇒ `OX = 2100 − 99 = 2001` ⇒ `Content` 整块在 `x ≥ 2001 > 1920` ⇒ **与带口零交集**：
- `MenuDraw.Rect` 的 `ClipRect` 三条 false 全命中 ⇒ **一个 `ImageQuad` 都不建**（quad 是在裁切判定**之后**才 `Create` 的）；
- `WildcardLabelRow` 走的是「`labelQuad == null ⇒ return`」那条早退 ⇒ **`Quantity` / `Name` 节点都不建** ⇒ `QuantityLabelOf(pr2) == null` ✓；
- `Army` 那颗走 `ClippedText → VisibleAbove` ⇒ 也不建。
⇒ 两条对照断言（文字不建 / 图为 0）**行为不变**。

### 六、三闸读数 + 形状

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_s4g bash 工具/typecheck.sh` | 运行时 **0** · 编辑器 **0** |
| `check_ledger_points.py`（无 `--fix`） | A 5831 · 通过 5831 · **漂移 0 · 失败 0** |
| `check_aggregate_lines.py` | 聚合行 0 · **违规 0 ⇒ PASS** |
| `RewardsScene.cs` 行数 / 行尾 | **14,265 → 14,265**（守恒 ✓）· **LF**（`crlf 0 / cr 0`） |
| 我这四处的 diff 形状 | 全 **1:1**（或 8:8 / 2:2）—— 我这一笔**净行数 = 0**（`numstat` 171/20 里那 **+80 / +70** 是 `B115_W13` 的未提交新增，不是我的） |

⚠️ 中间踩了一次：第一版改写让注释块 9 → 10 行 ⇒ 文件变 14,266、`{9717, 13605}` 那两个账锚整体下移。
**当场按 `git diff -U0` 的 hunk 头 `-7881,8 +7961,9` 定位到多出的那一行、合并回 9 行**，行数回到 14,265、`漂移 0`。
（教训：`RewardsScene.cs` 有 **5 个** A 档落点，其中两个在我编辑区**下面** ⇒ 这个文件也是「行数守恒」的硬约束，不只 `ItemDrawer.cs`。）

### 七、`RewardsScene.Run` 预期

- 这一条应**转绿** ⇒ 1 失败 → **0 失败**；通过数维持 **3,109** 量级。
- 若仍红：先看实测 `pmx` 落在哪 ——
  - `≈1969` ⇒ 裁切没生效（文字那一半没走 `ClipText`）；
  - `< 1919.5` 且 `≈1889..1960` ⇒ 自适应把字压小了（把 `qty` 再加长即可，别动断言阈值）。
