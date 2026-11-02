# B117_F3 —— 缺词条 `Armies/Ultramarines_short` + 软边 uv 假警报 + 左栏两处（R12）

> 执行代理 **F3** · 2026-11-02
> 结论一句话：**R2 修好（原版本来没有这条短名 ⇒ 不再出声）· R3 是真机假警报（uv 其实是对的，测量基错）· R12① 不改（屏上的字就是官方值）· R12② 修 3px 并给出「原版本来就重叠」的反证。**

---

## 一、`B117-R2` —— `[Loc] 语言表里**没有**这个词条：Armies/Ultramarines_short`

### 1·1 回源（原版到底有没有这条键）

现读 `d:/4/Unity/数据/游戏数据/官方本地化_13lang.json`（列式：`terms` 字典 + `_langs` 13 列）：

| 事实 | 读数 |
|---|---|
| `Armies/*` 键 | **20 条** |
| 其中带 `_short` 的 | **6 条**：`AstraMilitarum` / `Genestealers` / `Goff` / `Leviathan` / `Sautekh` / `Sororitas` |
| `Armies/Ultramarines_short` | **不存在**（`en` / `zh` 两列都没有这条键） |

`官方本地化_en+zh-CN.json` 同结论（`Armies/` 也是 20 条、`_short` 也是那 6 条）；
运行时那份 `Resources/official_loc_13lang.json`（10,862 键）也同结论。

⇒ **原版本来就没有 `Ultramarines_short`**，不是我们漏抄。
原版 `ArmyUtilities.GetLocalizedArmyName(army, useShortName)` 那一支的语义是「**短名取到空 ⇒ 回落全名**」
（`Core/ArmyIcons.cs` 的 `LocalizedName` doc 里早记着这条，出处 `cpp2il/ArmyUtilities.cs:99-124`）。

### 1·2 错在哪（我们这一侧）

`Core/ArmyIcons.cs` 的 `LocalizedName(string, bool)` 用 `CardText.TermOr(短名键, 全名)`：`TermOr` 在
「三档都没有」时**最后一句会调 `Loc.T(term)` 出声记账** ⇒ 把「原版就没有」报成了「表里少一条」。
真机日志（`%LOCALAPPDATA%/../LocalLow/QJHWarpforge/WarpforgeReplica/Player.log:37947`）那一声就是这么来的；
调用链 = `Shell/LiveOpsEventWindow.cs:452`（模式窗阵营格）→ `ArmyIcons.LocalizedName(faction, useShortName: true)`。

### 1·3 修法（没有编中文、也没有删日志）

`Core/ArmyIcons.cs`：取值前先 `Loc.HasAnyTerm(键)` 判一次，缺就**静默回落全名**（= 原版那一支）。

- 选 `Loc.HasAnyTerm` 而**不是** `Loc.HasEntry`：后者的语义是「**手写 `Table`** 里有没有」（`Loc.cs:3595-3605` 的订正），
  而**手写表里一条 `_short` 都没有** ⇒ 用它会把那 6 条**官方短名**也误判成「没有」而白白回落全名（行为退化）。
  `HasAnyTerm` = 三档一起算（手写 `Table` → bulk → 官方 13 语言全表），正是这个场景该用的口。
- 顺手扫了全前缀 `Armies/*_short`：代码里出现的只有 `Armies/Sautekh_short`（`Shell/BoosterPackOpenWindow.cs:1602`）、
  `Armies/Goff_short`（`Shell/LiveOpsLocalCatalog.cs:345/347`）与这条动态拼的 `<faction>_short`；
  前两条**官方表里有**、第三条由本次的兜底覆盖 ⇒ **没有第二处会再冒这条日志**。

### 1·4 断言（`Editor/ShellScene.cs`，接在既有 `ArmyIcons` 断言后面）

- 前提/结论：`ArmyIcons.LocalizedName("Ultramarines", true)` == 全名（与原版「取到空 ⇒ 回落」同形）；
- ★ **`Loc.MissingCount` 不涨**（把 `HasAnyTerm` 前置判拿掉 ⇒ 这条立刻红，真机那声日志就回来了）；
- ★ **正面照**：`Sororitas` 仍走**短名**（`Loc.T("Armies/Sororitas_short")`，中文「修女」≠ 全名「修女会」）。

---

## 二、`B117-R3` —— `[MenuDraw] 软边的 uv **没有铺满整张图**` 三处

### 2·1 结论：**uv 没有错**，是【测量基】错（数到了「正在等 Destroy」的上一刀子块）

日志（`Player.log:37942-37944`）：`切开` · 「Image」· `3 块 1.190476` / `4 块 1.380952` / `5 块 1.571428`，`整张图 1.000000`。

**算术反证**：多出来的面积每块**恒等于 `4/21 = 0.190476`**。

- 这条路的软边 = `Shell/LiveOpsEventWindow.cs:177` 的 `VpSoft = (0, 52)`（只渐变上下），宿主是阵营格
  `Image`（`ArmyCell = 168` px）。
- 视口软边的内沿落在格子内部 **32 px** 处 ⇒ 那一刀切出来的**带**在 uv 上正好是 `32/168 = 4/21 = 0.190476`。
- ⇒ 每一次多出来的那一块**就是上一刀切出来的那条带**：块数 `3→4→5`（每次 +1）、面积每次 +`4/21`，逐值吻合。

**为什么会数到上一刀的块**：`ImageQuad.SoftEdgeClear` 在**运行时**走 `Object.Destroy`
（`CardPresentation/Battle/ImageQuad.cs:90-102`：编辑器档才用 `DestroyImmediate`），而 Unity 的
`Object.Destroy` **延迟到帧末**销毁 ⇒ 同一帧里连切几刀时，旧的子块**还挂在宿主下面**，
`GetComponentsInChildren<ImageQuad>(true)` 照样把它们数进来。
这同时解释了「为什么 14 个自检宿主一直全绿」—— 编辑器/批处理那侧 `DestroyImmediate` 立刻生效，永远数不到残留。

⇒ **这一条不是「软件缺陷」，是那条不变量把「上一刀的块」也算进了「这一刀的块」里**（画面本身是对的：
uid 与子块都是本趟 `PlaceCell` 从同一个 `uv0` 切出来的，望远镜求和恒等于 `uv0` 面积）。

### 2·2 修法（`Shell/MenuDraw.cs`，**行数守恒 +0**）

- `CheckSoftEdgeUv(host, uv0, tag, kids)` 新增第 4 个形参：**只数「宿主 + 本趟建出来的那几块」**
  （`切开` 分支传本趟刚建的 `_uvCheckKids`；`整块不切` 传 `null`）。
  ⛔ 不再用 `host.GetComponentsInChildren<ImageQuad>(true)`。
- 新增静态表 `_uvCheckKids`（复用 ⇒ 建窗路径零分配；`ApplySoftEdges` 不重入，重切由 `ImageQuad._softBusy` 挡）；
  `ApplySoftEdges` 的 `切开` 分支建块时 `Clear()` 一次、每建一块 `Add` 一次（这两句挂在**已有物理行**上，行数不变）。
- 两处 doc 就地订正（铁律 5）：`SoftEdgeUvDrifts` 与 `CheckSoftEdgeUv` 的说明里写清「数的是本趟的块」+ 本轮实测数字。

### 2·3 「这条日志该不该每次出声」—— 判断与判据

**该出，而且保持 `LogWarning`（上限 3 条）不变**。理由：
① 修完之后它只在**真违规**时才会响（宿主 uv 不是本趟那份 / 本趟的块铺不满 `uv0`）；
② 修之前它会**只在真机上**误报（编辑器侧结构上数不到残留）⇒「觉得吵就删」会把**唯一的真机信号**删掉；
③ 上限 3 条已经挡了刷屏。⇒ **不降级、不加闸**，只把「数哪些块」改对。

### 2·4 断言（`Editor/ShellScene.cs` 新增 ⑤·c-3）

夹具：200×200 的框 + 软边 25（切 3×3），**在宿主下面挂一颗与本趟无关的子 quad**（uv = 整张图），再让它切一刀。

- （前提）宿主建出来了 · 诱饵子块挂上了（uv 面积 > 0.5）· `SoftEdgeKidCount == 8`（真切开了）；
- （前提②）把**老数法**原样复现一遍：`GetComponentsInChildren` 在这份夹具上**必须算成 2.0**
  —— 这条断的是「夹具本身有没有牙」；
- ★（结论）`MenuDraw.SoftEdgeUvDrifts` **不动**（新数法只数宿主 + 本趟的块）。
  ⇒ 改回 `GetComponentsInChildren` ⇒ 诱饵把它顶到 2.0 ⇒ 计数 +1 ⇒ **这条红**。

---

## 三、`B117-R12①` —— 左栏 PLAY 的译名：「玩」还是「游戏」

### 3·1 屏上实际印的是什么

真机截图（`d:/4/_tmp_view/shots/20261103_s117/06_collection.png`）左栏第一格：**「玩」**（不是「做」）。

### 3·2 官方表怎么写的

| 键 | 用在哪 | 官方 `en` | 官方 `zh` |
|---|---|---|---|
| `MainMenu/NavigationPanel/HomeButton` | **左竖导航第 1 颗**（`Shell/MainMenuRuntime.cs:1111` 传的就是这个键） | `PLAY` | **玩** |
| `MainMenu/MainButtons/ButtonLabel/Play` | 主菜单那颗按钮 | `Play` | **玩** |

（现读 `数据/游戏数据/官方本地化_13lang.json`；13 语言整列：`Play/Jugar/Играть/Jouer/Spielen/Jogar/Gioca/플레이/玩/プレイ/Graj/Hrát/Oyna`）

### 3·3 原版实拍怎么写的

`C:/Users/qjh36/Desktop/warpforge参考图/收藏—卡组的参考图.png`（左栏第一格，被 200% 放大看过）：
图标（交叉链锯剑）下面印的就是 **「玩」**。

### 3·4 裁定：**不改**（⛔ 没有编中文）

官方值 = 我们屏上的值 = 原版实拍的值 = **「玩」**。用户说的「应该译成『游戏』」**在三个来源里都拿不到支持**
（这条键的官方中文就是「玩」）。按 `CLAUDE.md` §18·0「需要拍板的地方**默认复刻原版**」+「⛔ 别自己编中文」，
本代理**不动**这一格；若用户坚持要「游戏」，那是**明确偏离原版**的裁定，需要用户自己拍板（本报告给足证据即可）。

⚠️ 用户复述的「做」与截图上的「玩」不一致 —— 可能看的是另一帧/另一处（例如某个按钮的「做」），
本代理只核了左栏第一格这一处。

---

## 四、`B117-R12②` —— 「图标」与「下方文字」重叠

### 4·1 原版 prefab 的真实数字（`Collection Menu Variant.prefab` 现读）

| 件 | 原版字段 | 换算（相对**钮顶**，钮 165×180） |
|---|---|---|
| `Icon` | `anchorMin/Max (0,0)-(1,1)` · `sizeDelta (0,0)` · **`pivot (0.5, 0.7)`** · `Image.preserveAspect = 1` · 精灵 `126×126` 方形 | **4.5 … 169.5**（画 165×165） |
| `Label` | `anchor (.5,.5)` · `pivot (.5,0)` · `anchoredPosition.y = −72.162` · `sizeDelta 155 × 37.856` | **124.3 … 162.2** |
| `TabButtonLabel`（文字本身） | 对齐 `Center/Midline` · 字号 36 · `auto[5~36]` · 字距 −3.5 | 与 `Label` 同一框 |

四个子菜单 prefab（`Base Submenu` · `Rewards Base Submenu Variant` · `Social Submenu Variant` · `Collection Menu Variant`）
的 `Icon` 节点**逐字段相同**（pivot `(0.5,0.7)`、stretch、sizeDelta 0）。

### 4·2 🔴 反证：**「文字上沿 ≥ 图标下沿」这条判据与原版冲突**

原版：文字框顶 **124.3**、图标框底 **169.5** ⇒ **文字框本来就压进图标框 45.2 px**。
原版实拍（同一张参考图）也是这样：文字压在插图下沿的 nametag 条上。
⇒ 本代理**没有**写那条断言（写了必然立刻红，而且是把「非原版」的口径钉进自检里）；
改成钉**原版那两组数**（见 4·4）。

### 4·3 真缺陷只有一个：图标**低 3 px**（已修）

我们原来按「居中」画图标（`cy`），而 uGUI 的 `preserveAspect` **不是居中**：
本机 `Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Image.cs:810-827`
`PreserveSpriteAspectRatio` 最后一步是 `rect.y += (oldHeight - rect.height) * rectTransform.pivot.y;`。
⇒ `(180 − 165) × 0.7 = 10.5`（自钮底）⇒ 图标 = 钮顶 **4.5 … 169.5**；我们是 7.5 … 172.5 ⇒ **整块低 3 px**。

修：`Shell/MainMenuSubmenuWindow.cs` 的 `BuildTabButton` 里
`float iconCy = cy - (TabBtnH - side) * (0.7f - 0.5f);`（= `cy − 3`），并把原来那句
「UGUI 的 preserveAspect 是**不看 pivot**」的**错注释就地订正**（铁律 5）。

### 4·4 断言（`Editor/CollectionScene.cs`，接在左栏四键的文案断言之后）

- （前提）`CollectionTabButton_0/Icon` 的 `ImageQuad` 取得到、矩形量得出；文案节点设计坐标量得出；
- ★ 图标高 = **165**（±1.5）· 宽 = **165**（±1.5）—— 原版 165×180 框 + 方形精灵 + preserveAspect；
- ★ **文案中心比图标中心低 56.25 px**（原版 `143.25 − 87`，±1.5）——把 `iconCy` 退回 `cy` ⇒ 53.25 ⇒ **红**。

---

## 五、闸

| 闸 | 读数 |
|---|---|
| `bash 工具/typecheck.sh` | **0 / 0** |
| `python 工具/check_ledger_points.py` | 漂移 **28** · 失败 **2** —— **全部落在 `RuleEngine/Core/DeckRules.cs`**（**不是本代理的文件**，是并发的另一个写手：`git status` 里它也在 M 列，且实测它与基线差 +3 行）。**本代理碰过的 4 个文件（`MenuDraw.cs` / `ArmyIcons.cs` / `MainMenuSubmenuWindow.cs` / `CollectionScene.cs`）逐条 grep：0 漂移 0 失败** |
| `bash 工具/ruleprobe.sh check` | rc=0 · 输出指纹 **`ebdac779…` 未变** · 基线指纹一致 |
| 行数守恒 | `MenuDraw.cs` **14/14（净 0）** · `ArmyIcons.cs` **4/4（净 0）** —— 这两个文件有账的 A 档落点，所以按「同一物理行改写」的纪律做 |

### 动过的文件

| 文件 | 改了什么 |
|---|---|
| `CardPresentation/Core/ArmyIcons.cs` | R2：短名键取值前 `Loc.HasAnyTerm` 前置判 + doc |
| `CardPresentation/Shell/MenuDraw.cs` | R3：`CheckSoftEdgeUv` 只数本趟的块（+`_uvCheckKids`）+ 两处 doc 订正 |
| `CardPresentation/Shell/MainMenuSubmenuWindow.cs` | R12②：页签图标 `cy − 3`（原版 pivot 0.7）+ 错注释订正 |
| `CardPresentation/Editor/ShellScene.cs` | R2 / R3 的断言（⑤·c-3 与新块，均在 `ShellScene.Run` 内） |
| `CardPresentation/Editor/CollectionScene.cs` | R12② 的断言（左栏四键文案断言之后） |

---

## 六、没查清 / 边界（如实）

1. **R12② 的「像不像」没做并排对比的可信结论**：参考图是**视频帧**（左右有黑边、底部有字幕条），
   画布→帧的缩放/偏移只能靠地标反推，我试了三组标定都对不齐，故**放弃**用它对参数（铁律 3：
   参数只认解包/prefab，截图只用于验收）。⇒ 本报告的图标/文字数字**全部来自 prefab + uGUI 源码**，
   不是从图上量的。
2. **真机上文字是否还有「压得太狠」的观感**：prefab 上原版就是重叠 45.2 px，我们修完 3px 后与 prefab 逐值一致；
   若用户仍觉得难看，那属于「原版就这样」，需要用户另行拍板（不是复刻缺陷）。
3. **R12①（玩 → 游戏）**：本代理**没有改**，理由见 §三（官方表 + 原版实拍都是「玩」）。这一条需要用户确认
   ——若用户仍要「游戏」，那是一次**明确的偏离原版**的裁定。
4. **没跑 Unity**：三条断言都是编辑器宿主（`ShellScene.Run` / `CollectionScene.Run`）里的新块，
   **本代理没有执行它们**（红线：不跑 Unity 批处理）⇒ 首次跑批时要盯这三条（R2 两条 + R3 一条 + R12② 三条）。
