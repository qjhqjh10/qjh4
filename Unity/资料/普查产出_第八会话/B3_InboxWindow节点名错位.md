# B3 · `Shell/InboxWindow.cs` 关窗钮「节点名 ↔ 内容」错位 —— 修正报告

> 执行代理 B3（本波**唯一**写手：`Shell/InboxWindow.cs` + `Editor/RewardsScene.cs` 那颗断言）
> 日期 2026-10-18 · ⛔ 未跑 Unity（只跑了秒级类型检查，`TMPDIR=/tmp/wf_b3`）· ⛔ 未动 git 写操作 · ⛔ 未改两张正本。

---

## §一 原版实读（**本件亲跑，不是转抄旁证**）

### 1. 三条 Graphic 的「节点名 ↔ 贴图名」对应

命令（只读，不改任何工程文件）：

```
python -I d:/tmp/wf_hit/rcunion.py bundle_menus_assets_all "Inbox Menu" --depth 8
```

相关输出（**逐字抄**）：

```
    Generic Close Button Orange  ACT  (825.54,-506.97,899.92,-431.37) 74.39x75.60
        | Image[spr=UI_Button_Round_background RT=0 pad={'x':0.0,'y':0.0,'z':0.0,'w':0.0} type=Simple]
        ; EverguildButton m_TargetGraphic=pid5609434692533257010 m_Transition=2
          SpriteState(highlighted_pid=556997698038149155) Colors ; EverguildButtonMaterialModifier
      Background  ACT  (833.69,-498.99,890.55,-440.87) 56.86x58.13
        | Image[spr=40k_general_bt_yellow RT=1 pad={'x':-20.0,'y':-20.0,'z':-20.0,'w':-20.0} type=Simple]
      Icon  ACT  (833.69,-498.99,890.55,-440.87) 56.86x58.13
        | Image[spr=40k_general_bt_yellow_close RT=1 pad={'x':-20.0,'y':-20.0,'z':-20.0,'w':-20.0} type=Simple]
```

汇总（**这就是原版的真值**）：

| 谁 | 节点名 | 贴图 | 矩形 | 吃射线 |
|---|---|---|---|---|
| 根（`Content/Generic Close Button Orange`） | `Generic Close Button Orange` | `UI_Button_Round_background`（**圆底盘**） | **74.39×75.60**（= 我们的 `CloseBtn`） | **`RT=0` ⇒ 不吃射线** |
| 子件 1 | **`Background`** | **`40k_general_bt_yellow`（黄面）** | 56.86×58.13（= `CloseBg`） | `RT=1` · pad `(-20)⁴` |
| 子件 2 | **`Icon`** | **`40k_general_bt_yellow_close`（叉）** | 56.86×58.13 | `RT=1` · pad `(-20)⁴` |

第二条独立命令（把 `m_TargetGraphic` 那个 pid 解出来）：

```
python -I d:/tmp/wf_hit/tgt.py bundle_menus_assets_all 5609434692533257010
⇒ 5609434692533257010: GO名='Background' 贴图=40k_general_bt_yellow RT=1 pad={'x':-20.0,'y':-20.0,'z':-20.0,'w':-20.0}
```

⇒ **原版 `m_TargetGraphic`（悬停换图那一层）指的就是叫 `Background` 的那颗 = 黄面。**

`menu_dump` 同向印证（第三条路，`--depth 6 --md`，第 23–25 行）：根 `Simple (1,1,1,1) **preserveAspect**`
· 子件 `Background` = `<未解出 5693181797853584851>` · 子件 `Icon` = `<未解出 -2367583692806092745>`。

### 2. 文件 / 字段坐标

- bundle 目录：`d:/2/新解包资源/assets_full/bundle_menus_assets_all/`
- 「黄面」那颗的 **MonoBehaviour 正本**：
  `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_5609434692533257010.json`
  —— 字段：`m_Sprite.m_PathID = 5693181797853584851` → `40k_general_bt_yellow`
  · `m_GameObject.m_PathID = 81156965544985394`（**GO 名 = `Background`**）
  · `m_RaycastTarget = 1` · `m_RaycastPadding = {'x':-20,'y':-20,'z':-20,'w':-20}`。
  ⚠️ 该 GO 的 `GameObject/*.json` **文件名不是 pid**（解包出来的 GameObject json 只有
  `m_Component/m_Layer/m_Name/m_Tag/m_IsActive` 五个键、**没有 `m_PathID`**），所以「pid → 文件名」只能走
  工具里的容器索引 —— 复现请直接用上面两条命令，别按文件名找。

### 3. 与「旁证」的关系（**无冲突**）

`资料/普查产出_第六会话/W_命中区批2.md` §⑧-2（该文件 `:39`）写的三行
`根 Image[UI_Button_Round_background RT=0]` / `Background[40k_general_bt_yellow RT=1 pad(-20)⁴]` /
`Icon[40k_general_bt_yellow_close RT=1 pad(-20)⁴]` —— **与本件实读逐字一致**。
🔴 但**简报里那句概括**（「原版的 `Background` 画黄面 · 我们的是反的」）**少说了一层**：
原版**不是两颗**、是**三颗**（根那颗圆底盘 `Image` + 两颗子件），我们这边同样三颗
（`Background`/`Icon`/`Icon (X)`）—— 错位是**整套名字整体错开一格**，不是「两颗对调」。
⚠️ 本件**以实读为准**，并把这条差异写在这里。

---

## §二 我们侧的现状（**改前**）

`Shell/InboxWindow.cs`（改前 `:333-341`）：

| 节点名 | 画的贴图 | 与原版对照 |
|---|---|---|
| `Background` | `UI_Button_Round_background`（圆底盘） | ❌ 原版这个名字画**黄面** |
| `Icon` | `40k_general_bt_yellow`（黄面） | ❌ 原版这个名字画**叉** |
| `Icon (X)` | `40k_general_bt_yellow_close`（叉） | ❌ **原版根本没有这个节点名**（自造名） |

⇒ 三颗的「名字 → 内容」**整体错开一格**；第三种名字 `Icon (X)` 全仓只此一处。

现有断言侧（`Editor/RewardsScene.cs` 改前 `:8877-8885`）已**如实记过**这处错位，并**只断贴图名**
（`targetNodeName` 传 `null`）—— 注释原话：「断『== `Background`』会对**正确实现**报红」。

---

## §三 选「改名」还是「改内容」

### 影响面（**全仓 `grep` 现读**）

| 查什么 | 结果 |
|---|---|
| 全仓 `"Icon (X)"` | **2 处，都在 `Shell/InboxWindow.cs`**（建它那一句 + 改前那段注释）；`Editor/` **0 命中** ⇒ 没有任何断言/代码碰过它 |
| 全仓 `Find("Background")` / `Find("Icon")` / `FindChild(…, "Background"/"Icon")` | **83 处**，逐处看过：**没有一处**落在**收件箱关窗钮**上（其余是各窗口各自的同名子件、行底板、页签图标 …） |
| `Editor/*.cs` 里 `InboxWindow` 的关窗钮 | **只有 1 处** = `RewardsScene.cs` 那颗 `A1125Close`（本件要改的就是它） |
| `Editor/ShellScene.cs` 的 `InboxWindow` 夹具（`:4390` 起） | 只用它量 `MsgList` / 文字，**不碰关窗钮**（`grep` 该段 0 命中） |
| 运行时代码（`Shell/*` 除本件 · `Core/` · `Board/` · `Battle/`） | 无一处按名字取关窗钮子件 |

⇒ **两份影响面都是 1（只有本件自己那颗断言）**，但两者**效果并不对等**：

### 为什么选「改名」（不是「改内容」，也不停手）

- **「改内容」不能真正修好这处缺陷**，它只是把错位**挪个位置**：
  「改内容」= 名字不动、只换贴图 ⇒ 要让 `Background` 画黄面、`Icon` 画叉，那颗**圆底盘**就只能
  落到 **`Icon (X)`** 这个名字上 ⇒ 变成「**一个叫『叉』的节点画着圆底盘**」= 又一处名实不符（只是换了对象）。
  而且我们**只有两个原版名字**（`Background`/`Icon`）却有**三颗** Graphic ⇒ 「改内容」**无论如何都留下第三颗**
  顶着 `Icon (X)` 这个**原版没有、且说反**的名字。
- **「改名」一次到底**：`Background` 画黄面 · `Icon` 画叉（**与原版逐字一致**，也与本仓**同族关窗钮的既有写法**
  一致 —— `Shell/BaseOfferPopup.cs:744` · `Shell/BoosterInfoPopup.cs:344` · `Shell/ChatPanel.cs:298` ·
  `Shell/BattleLogPopup.cs:159` · `Shell/DeckSelectionPopup.cs:492` **全是** `Background` 画黄面、
  `Icon` 画 `40k_general_bt_yellow_close`）；只剩那颗**原版本来就没有独立子件名**的圆底盘要起个自造名。
- **圆底盘那颗给什么名字**：原版它长在**根节点自己**身上（根 GO 名 = `Generic Close Button Orange`，无子件名），
  而我们的 `MenuDraw.Rect` **只会新建子节点**（`ImageQuad.Create` 恒 `new GameObject`，见
  `Battle/ImageQuad.cs:118-119`）⇒ 它只能是个子件。本件按**兄弟窗先例**取名 **`Base`**
  （`Shell/BoosterInfoPopup.cs:343` 的圆底盘子件正叫 `Base`；
  同房 `Shell/BaseOfferPopup.cs:743` 则把圆底盘**画在根节点上**、根节点名就复用 `Generic Close Button Orange`）。
  ⛔ **绝不能留 `Background`** —— 那会和黄面那颗**同名兄弟**，`FindChild` 按名字取会**静默取错一颗**。
- **「改名」的红线检查**：改名会动到「按名字找节点」的地方 ⇒ 上表已逐处核过，**关窗钮这条链外零命中**
  ⇒ **不红任何既有件**（本件自己那颗断言同步改，见 §四）。

---

## §四 改动清单（`git diff --numstat` = `InboxWindow.cs 23/8` · `RewardsScene.cs 190/0`）

> ⚠️ `RewardsScene.cs` 的 `190/0` **不是本件的量** —— 该文件在本会话开始前就是 `M`（本轮前序写手的未提交新增，
> 全为纯插入）。本件在其中**只改了 `:8874` 起那一段**（`git diff` 该 hunk 逐行核过）。
> 两个文件改完**行尾仍是纯 LF**（`CRLF=0`；改前 `InboxWindow.cs 629 行 / RewardsScene.cs 10684 行`，
> 改后 `644 / 10695`）。

### 4·1 `Shell/InboxWindow.cs`

**改前**（`:334-341`）：

```csharp
var close = MenuDraw.Node(c, "Generic Close Button Orange", CloseBtn);
// 🔴 **换图落在「按钮脸」那一层，⛔ 不是圆底盘**；⚠️ **本窗的节点名是错位的** …
var closeBaseQ = MenuDraw.Rect(close, Art(ArtCloseBg), CloseBg, "Background", QContent);
var closeFaceQ = MenuDraw.Rect(close, Art(ArtCloseIcon), CloseBg, "Icon", QOverlay);
MenuDraw.Rect(close, Art(ArtCloseX), CloseBg, "Icon (X)", QOverlay);
```

**改后**（`:353-355`，注释整段重写成「原版实读 + 为什么叫 `Base`」）：

```csharp
var closeBaseQ = MenuDraw.Rect(close, Art(ArtCloseBg), CloseBg, "Base", QContent);          // 圆底盘（自造名，原版在根节点上）
var closeFaceQ = MenuDraw.Rect(close, Art(ArtCloseIcon), CloseBg, "Background", QOverlay);  // 黄面（= 原版节点名）
MenuDraw.Rect(close, Art(ArtCloseX), CloseBg, "Icon", QOverlay);                            // 叉（= 原版节点名）
```

**只动了 `name` 三个实参**（贴图实参、矩形实参、队列实参、`MenuDraw.Hit` 的 `target`/`art`/`hoverArt`
**一个字没动** ⇒ 渲染结果与改前**逐像素相同**，改的只是节点名）。
另把下面 A1058 那段注释里「本窗里它的节点名叫 `Icon`」一句订正为「**起它的节点名就是原版的 `Background`**」
（留了改前名字，作订正痕）。

### 4·2 `Editor/RewardsScene.cs`（那一段 = 关窗钮的断言）

**改前**（`：8884-8885`）：

```csharp
A1125Close("InboxWindow", inbox.transform, "Generic Close Button Orange", "Hit",
           "Icon", "40k_general_bt_yellow", null, 96.86f, 98.13f);
```

（`faceName = "Icon"`；`targetNodeName = null` ⇒ ④ 只断贴图名）

**改后**：

```csharp
A1125Close("InboxWindow", inbox.transform, "Generic Close Button Orange", "Hit",
           "Background", "40k_general_bt_yellow", "Background", 96.86f, 98.13f);
```

⇒ ④ 补齐成**节点名 + 贴图名两个条件都断**：`wb.target.gameObject.name == "Background"`
**且** `wb.target.Texture.name == "40k_general_bt_yellow"`；同时 ②（命中区尺寸 `96.86×98.13`）·
③（命中区中心 == 可见面中心）**一个字没动**。上方那段注释同步重写成「已改正 + 原版期望值 + 改坏法」。

**🧨 改坏法（两半各一条，任一改回必红）**：

1. **名字那半** —— 把 `Shell/InboxWindow.cs` 里画黄面那颗 quad 的节点名 `"Background"` 改回 `"Icon"`
   （或把 `MenuDraw.Hit` 的 `target` 实参改去指圆底盘 `closeBaseQ`）⇒ `wb.target.gameObject.name` 变 `Icon`/`Base` ⇒ 红；
   ⚠️ 改回 `"Icon"` 会**同时**红掉「可见面子件 `Background` 拿得到」那条前提（那颗节点不存在了）。
2. **贴图那半** —— 把 `ArtCloseIcon`（`40k_general_bt_yellow`）换成 `UI_Button_Round_background` ⇒ 贴图名不符 ⇒ 红。

（三条断言各自独立源：② 量命中 quad 的渲染矩形、③ 比命中区中心 vs 可见面渲染中心、④ 读 `WindowButton.target`
⇒ 改坏一处只红一条，不互相掩盖。）

---

## §五 验证读数 · 未查清的部分

### 5·1 验证读数

- **秒级类型检查**：`TMPDIR=/tmp/wf_b3 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ `--- 运行时程序集 --- 运行时错误数: 0` · `--- 编辑器程序集 --- 编辑器错误数: 0`
  （最后一次跑是在**三处编辑全部落盘之后**；本次 0 错，未出现「错在别人在写的文件上」那种情况）。
- **⛔ 未跑 Unity 自检**（按红线；宿主建议见 5·4）。
- **行尾**：两文件改完仍纯 LF（二进制读出 `CRLF=0`）。
- **`git diff --numstat`**：`Shell/InboxWindow.cs 23 8` · `Editor/RewardsScene.cs 190 0`（后者含前序未提交插入，见 §四说明）。

### 5·2 改名后的实际树（自检会看到的）

```
Content/Generic Close Button Orange           (node, CloseBtn 74.38×75.60, 无 quad)
  ├─ Base          [ImageQuad UI_Button_Round_background, CloseBg, QContent]   ← 圆底盘
  ├─ Background    [ImageQuad 40k_general_bt_yellow,     CloseBg, QOverlay]    ← 黄面 = WindowButton.target
  ├─ Icon          [ImageQuad 40k_general_bt_yellow_close, CloseBg, QOverlay]  ← 叉
  └─ Hit           [hit quad, PaddedRect(CloseBg, ClosePad) = 96.86×98.13, QOverlay]  ← 命中区
```

`FindChild(btn, "Background")` ⇒ 唯一命中黄面那颗；`FindChild(btn, "Hit")` ⇒ 命中区那颗。均无歧义。

### 5·3 🔴 本件**顺手查出、但超出本简报范围、未改**的一处偏离（请裁定后落 `项目任务.md` §三）

**同一颗关窗钮的圆底盘，我们的矩形 + 缩放都对不上原版**：

| 项 | 原版实读 | 我们（`InboxWindow.cs:353`） | 差 |
|---|---|---|---|
| 圆底盘那颗 quad 的矩形 | 根节点 `Generic Close Button Orange` = **`CloseBtn` 74.39×75.60** | **`CloseBg` 56.86×58.13** | **宽小 17.53 / 高小 17.47**（≈ 每边 8.8） |
| `preserveAspect` | `menu_dump` 印 **`preserveAspect`**（三颗都有） | **没传 `keepAspect`**（`MenuDraw.Rect` 第 7 形参） ⇒ 拉伸 | 贴图 `UI_Button_Round_background` 是 **237×237 正方** ⇒ 原版实绘 **74.39×74.39**、我们 **56.86×58.13**（≈ 小 24%） |

- 判据出处：§一那两条命令 + `menu_dump … "Inbox Menu" --depth 6 --md` 第 23 行
  （`Generic Close Button Orange … 74.39×75.60 … Simple (1,1,1,1) preserveAspect`）。
- **兄弟窗先例**：`Shell/BaseOfferPopup.cs:743` 画圆底盘传 `keepAspect = true` 且用**根矩形**；
  `Shell/BoosterInfoPopup.cs:343` 同。⇒ 我们这一处**既偏离原版、也偏离本仓同族写法**。
- ⚠️ 黄面/叉那两颗也有同族的小差（框 56.86×58.13、正方贴图、原版 `preserveAspect` ⇒ 原版实绘 56.86×56.86
  而我们 56.86×58.13，高多 1.27 ≈ 2.2%）；本件**未动**。
- ⛔ **本件没改它**：简报只授权修「节点名与内容错位」，且 §13·4⑧ 明写「顺手发现的东西也要报、**不许自己顺手改**」。
  ⇒ 记录在此，请主对话裁后立项（在 `Shell/InboxWindow.cs` 白名单内，落点清楚）。

### 5·4 需要哪几条自检覆盖

- **宿主 = `RewardsScene.Run`**（唯一改了断言的文件；`InboxWindow` 只在 `Editor/RewardsScene.cs`
  与 `Editor/ShellScene.cs` 的夹具里出现，后者不碰关窗钮）。
- `Shell/InboxWindow.cs` 是**运行时**（`Shell/`）代码，被 `ShellScene.Run` 间接用到（它的 `InboxWindow` 夹具）
  ⇒ 若同步点要稳一点，可**加跑 `ShellScene.Run`**；但本件改的只有节点名，`ShellScene` 那条链不读关窗钮子件名。
- ⛔ 本件**没跑**任何 Unity 自检。

### 5·5 没查清的部分（如实）

- 原版根那颗圆底盘 `Image` 的 **`m_PreserveAspect` 字段本身**没直读（只凭 `menu_dump` 印的
  `preserveAspect` 字样）；§5·3 的「实绘 74.39×74.39」是**按 `Image.GetDrawingDimensions` 的等比规则推的**
  （框 74.39×75.60、贴图 237×237 正方）—— 若要落成待办，建议顺手把 `m_PreserveAspect` 与
  `m_PixelsPerUnitMultiplier` 也直读一次。
- 那颗圆底盘在原版**是长在根节点自己身上**（无独立子件名）—— 我们**没有**照 `BaseOfferPopup` 的做法
  把它画到根节点上（那要改 `MenuDraw.Node` → `MenuDraw.Rect` 的结构，属**结构改动**、超出本简报）。
  本件只给了它一个自造名 `Base`；**结构是否也要归真**（像 `BaseOfferPopup` 那样把它挂到根节点、
  顺便把矩形改成 `CloseBtn`）**留给主对话裁**。
