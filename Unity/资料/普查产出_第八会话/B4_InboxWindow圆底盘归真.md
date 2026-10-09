# B4 · `Shell/InboxWindow.cs` 关窗钮**圆底盘**归真（矩形 / 缩放 / 挂点）—— 修正报告

> 执行代理 B4（本波**唯一**写手：`Shell/InboxWindow.cs` + `Editor/RewardsScene.cs` 那颗 `A1125Close` 及其紧邻注释）
> 日期 2026-10-18 · ⛔ 未跑 Unity（只跑了秒级类型检查，`TMPDIR=/tmp/wf_b4`）· ⛔ 未动 git 写操作 · ⛔ 未改两张正本。
> 承接：`资料/普查产出_第八会话/B3_InboxWindow节点名错位.md`（**B3 的结论一条都没推翻**，本件在它之上继续）。

---

## §一 第一步：判据**直读**（⛔ 不是推的）

### 1·1 探针

新写的只读探针：**`d:/tmp/wf_b4probe/pa.py`**（复用 `d:/4/Unity/工具/menu_rect.py` 的 `Bundle`，不抄第二份矩形算法）。

```
python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Inbox Menu" 6
```

（`rcunion.py` 只印 `Image[spr RT pad type]`，**印不出** `m_PreserveAspect` / `m_PixelsPerUnitMultiplier`
—— 上一路如实标的那个缺口就是它 ⇒ 本件另写一个探针把这两格**逐字段**读出来。）

### 1·2 三颗 Graphic 的原始字段（**逐字抄自输出**）

| 节点（原版） | `MonoBehaviour/*.json` | `m_Sprite`(pid) → 名 | `m_Type` | **`m_PreserveAspect`** | **`m_PixelsPerUnitMultiplier`** | 矩形 | `m_RaycastTarget` | `m_RaycastPadding` |
|---|---|---|---|---|---|---|---|---|
| **`Generic Close Button Orange`**（**根节点自己**带 Image） | `MonoBehaviour_3213954248950252338.json` | `2381704724431365035` → `UI_Button_Round_background` | `0` Simple | **`1`** | **`1.0`** | **74.39×75.60** | `0` | `(0,0,0,0)` |
| 子件 `Background` | `MonoBehaviour_5609434692533257010.json` | `5693181797853584851` → `40k_general_bt_yellow` | `0` Simple | **`1`** | **`1.0`** | 56.86×58.13 | `1` | `(-20,-20,-20,-20)` |
| 子件 `Icon` | `MonoBehaviour_-3135620231419100366.json` | `-2367583692806092745` → `40k_general_bt_yellow_close` | `0` Simple | **`1`** | **`1.0`** | 56.86×58.13 | `1` | `(-20,-20,-20,-20)` |

⇒ 🔴 **三颗全是 `m_PreserveAspect = 1`、全是 `m_Type = 0`（Simple）**。
上一路靠 `menu_dump` 印的 `preserveAspect` 字样推的三条，**现在全部直读证实**（不是推的了）。

### 1·3 这三张原版 sprite 的 `m_Rect`（`d:/2/新解包资源/assets_full/*/Sprite/*.json`）

| sprite | `m_Rect` | `m_Border` | `m_Offset` | `m_Pivot` |
|---|---|---|---|---|
| `UI_Button_Round_background` | **237×237** | `(0,0,0,0)` | `(0,0)` | `(0.5,0.5)` |
| `40k_general_bt_yellow` | **71×71** | `(0,0,0,0)` | `(0,0)` | `(0.5,0.5)` |
| `40k_general_bt_yellow_close` | **71×71** | `(0,0,0,0)` | `(0,0)` | `(0.5,0.5)` |

两份拷贝（`bundle_duplicateassetisolation_assets_all/` 与 `sharedassets0/`）**逐字段同值**，无歧义。
三张**都是正方**、`m_Border`/`m_Offset` **全 0**（⇒ 无 padding、不挪位）。

### 1·4 按本仓项目口径解释那两个字段

- **`m_PixelsPerUnitMultiplier = 1.0`**：本仓口径是「角块 = `m_Border ÷ ppuMul`」，但**它只对 `Sliced`/`Tiled` 有意义**
  —— 这三颗都是 `m_Type=0` **Simple**，且 `m_Border` 全 0 ⇒ **`ppuMul` 在这三颗上是死值**（读出来只为留证）。
- **`m_PreserveAspect = 1` + `Simple` + sprite `m_Rect` 正方** ⇒ uGUI 实绘 = **正方形内接**：
  **圆底盘实绘 `74.39×74.39`**（框 74.39×75.60 里的高被缩到 74.39）、**黄面/叉实绘 `56.86×56.86`**。
  ⇒ B3 §5·3 那条「按等比规则**推**的 74.39×74.39」**现在有直读判据背书**了。

### 1·5 我们侧那三张贴图的实读尺寸（决定 keepAspect 的净效果）

`CardArt.MenuUi` 的搜索序 = `ui_menu/` → `ui_deck/` → `ui/`（`Core/CardArt.cs:581-592`）：

| 常量 | 贴图名 | 解析到的目录 | **实读 PNG 尺寸** |
|---|---|---|---|
| `ArtCloseBg` | `UI_Button_Round_background` | `Resources/Art/ui_menu/` | **237×237** |
| `ArtCloseIcon` | `40k_general_bt_yellow` | `Resources/Art/ui/` | **71×71** |
| `ArtCloseX` | `40k_general_bt_yellow_close` | `Resources/Art/ui_deck/` | **71×71** |

🔴 **三张的导入尺寸恰好等于原版 `m_Rect`**（237 / 71 / 71）。而 `CardbackFace.Fit` 对**没登记**的贴图
（`Resources/Cardbacks.json` 233 条里**这三张一条都没有**，实查）走兜底 = 「拿贴图自己的宽高当 `m_Rect`、`padding=0`」
⇒ 我们传 `keepAspect:true` 之后**实绘与原版逐像素同值**（不是「接近」）。

---

## §二 第二步：改前 / 改后

### 2·1 挂点：选 `MenuDraw.Rect`（画在根节点上），⛔ **不选** `MenuDraw.Node` + 子件

**依据 = 兄弟窗先例现读**（`grep -rn '"Generic Close Button Orange"' Shell/ Editor/`，逐处看过）：

| 窗 | 圆底盘怎么挂 | 用根矩形 | `keepAspect` |
|---|---|---|---|
| **`Shell/BaseOfferPopup.cs:742-743`** | `Rect(parent, 底图, G.Close, "Generic Close Button Orange", …)` ⇒ **画在根节点上**，`Node` 只作**兜底** | ✅ | ✅ |
| **`Shell/ReferralPopupWindow.cs:386-387`** | 同上（逐字同形） | ✅ | ✅ |
| `Shell/BoosterInfoPopup.cs:342` | 子件 `Base`（自造名） | ❌ 用 `CloseArtR` | ✅ |
| `Shell/LeaderboardWindow.cs:685` / `PlayerProfileWindow.cs:376` | 子件 `Image`（自造名） | ✅（`CloseR`） | ✅ |
| `Shell/ChatPanel.cs:297` / `CollectionWindow.cs:1524` | 子件 `Background Round` | — | ✅ |

🔴 **本件顺带现读出来的（与简报的前提相反，请转述）**：简报说「`BoosterInfoPopup` **也是**传 `keepAspect=true`
且用**根矩形**」——**实测不是**，它用的是子件矩形 `CloseArtR`（56.86×58.13）、挂在一颗自造子件 `Base` 上。
**因此它不是可抄的先例。** 更进一步：

```
python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Booster Info Popup" 6
python -I d:/tmp/wf_b4probe/pa.py bundle_menus_assets_all "Base Offer Popup"   6
```

两扇的**原版结构逐字相同** —— 根 `Generic Close Button Orange`(74.39×75.61) 带 `Image[UI_Button_Round_background]`、
`m_PreserveAspect=1`、`m_RaycastTarget=0`，子件 `Background`/`Icon` 同矩形 56.86×58.13。
⇒ **`BoosterInfoPopup` 那颗子件 `Base` 是它自己偏离原版**，⛔ 不能当先例
（它不在本件白名单内、**本件没动它** ⇒ 已记 §五，请调度台另立一笔）。

**本件照 `BaseOfferPopup` / `ReferralPopupWindow` 的形状**（那是唯一与原版同构的写法）：

```csharp
var closeQ = MenuDraw.Rect(c, Art(ArtCloseBg), CloseBtn, "Generic Close Button Orange", QContent, null, true);
var close = closeQ != null ? closeQ.transform : MenuDraw.Node(c, "Generic Close Button Orange", CloseBtn);
```

- `Rect` 的 6/7 形参 = `tint: null` / **`keepAspect: true`**；`Rect` 在 `tex == null` 时返回 `null` ⇒ `Node` 兜底
  （与两处先例一致、也与改前的「图缺了还能建出节点」行为一致）。
- **自造名 `Base` 随之取消**（圆底盘不再需要子件名 ⇒ 简报 §第二步 3 那句「那个名字可能就不需要了」成立）。
- 位置与子件序**逐位不变**：`MenuDraw.Rect` 与 `MenuDraw.Node` 都用同一份 `Local(parent, …)` 摆矩形中心
  （`MenuDraw.cs:30-31` / `ApplyPxRect:180-183`），`ImageQuad.Create` 也是 `SetParent(parent, false)` 追加
  ⇒ **同一个父 `c`、同一个子件序号**。

### 2·2 矩形 + 缩放

| 谁 | 改前 | 改后 | 原版（直读） |
|---|---|---|---|
| 圆底盘 | `CloseBg` 56.86×58.13 · **不传 `keepAspect`** · 画在子件 `Base` 上 | **`CloseBtn` 74.39×75.60** · **`keepAspect=true`** · **画在根节点自己身上** | 框 **74.39×75.60** · `PA=1` ⇒ 实绘 **74.39×74.39** |
| 黄面 `Background` | `CloseBg` · 不传 `keepAspect` | `CloseBg` · **`keepAspect=true`** | 框 56.86×58.13 · `PA=1` ⇒ 实绘 **56.86×56.86** |
| 叉 `Icon` | `CloseBg` · 不传 `keepAspect` | `CloseBg` · **`keepAspect=true`** | 框 56.86×58.13 · `PA=1` ⇒ 实绘 **56.86×56.86** |

⇒ 净效果：**圆底盘从 56.86×58.13 → 74.39×74.39**（每边 +8.8，改前比原版**小约 24%**）；
**黄面/叉从 56.86×58.13 → 56.86×56.86**（高 −1.27，改前高多 ≈2.2%）。**三颗都归位。**

### 2·3 队列 / 命中区（**有意不动**，写明为什么）

- **队列不变**：圆底盘仍 `QContent`(3010)、黄面/叉仍 `QOverlay`(3014)，与改前逐字相同。
  压暗层那条「`QShade`(3002) 严格低于内容命中区档 `QOverlay`(3014)」的不变量**不受影响**
  （`RewardsScene` 的 `MenuDraw.CheckShadeRule` 那条照旧成立）。
- **命中区不变**：`MenuDraw.Hit(close, "Hit", MenuDraw.PaddedRect(CloseBg, ClosePad), QOverlay, …)` **一个字没动**
  —— 命中区是由 `MenuDraw.Hit` **独立建**的一颗 quad（`PaddedRect(CloseBg, (-20)⁴)` = **96.86×98.13**），
  与那三颗圆盘/黄面/叉的**矩形成像无关**。原版也是「根那颗 `m_RaycastTarget=0`、吃射线的是两颗同矩形子件各按自己的 padding 外扩」
  ⇒ 这一层**本来就是对的**（A1053 那轮做的）。
- **换图层绑定不变**：`MenuDraw.Hit(…, target: closeFaceQ, art: ArtCloseIcon, hoverArt: "40k_general_bt_yellow_hover")`
  —— `closeFaceQ` 仍是画黄面那颗（节点名 = 原版 `Background`）⇒ ④ 那条断言的两个条件都还成立。
  ⚠️ 悬停换图是 `SetTexture`，会把 `_aspect` 改成新贴图自己的（`40k_general_bt_yellow_hover` 实读 **71×71** 正方）
  ⇒ **与常态图同比例、实绘尺寸不变**。

### 2·4 落地树（改后，自检会看到的）

```
Content/Generic Close Button Orange   [ImageQuad UI_Button_Round_background, CloseBtn, QContent, keepAspect]
  ├─ Background   [ImageQuad 40k_general_bt_yellow,       CloseBg, QOverlay, keepAspect]  ← WindowButton.target
  ├─ Icon         [ImageQuad 40k_general_bt_yellow_close, CloseBg, QOverlay, keepAspect]
  └─ Hit          [hit quad, PaddedRect(CloseBg, ClosePad) = 96.86×98.13, QOverlay]
```

与原版**同构**（三层 Graphic、圆底盘在根节点上、无多余子件）。`FindChild(btn,"Background")` / `"Icon"` / `"Hit"`
**各唯一**，无同名兄弟。

---

## §三 那条断言（`Editor/RewardsScene.cs` · `A1125Close("InboxWindow", …)`）

### 3·1 调用行 —— **逐字未改**（为什么不用改）

```csharp
A1125Close("InboxWindow", inbox.transform, "Generic Close Button Orange", "Hit",
           "Background", "40k_general_bt_yellow", "Background", 96.86f, 98.13f);   // 改前改后同一行
```

四格逐条核过，**没有一格读的是本件改动的量**（⇒ 简报 §第二步 5「若有变 ⇒ 跟着改」这一条**不触发**）：

| 格 | 读的是什么 | 受本件影响吗 |
|---|---|---|
| ① 前提 | `FindChild(winRoot,"Generic Close Button Orange")` / `"Hit"` / `"Background"` 取得到 | ⛔ 不 —— 三个名字都在（圆底盘改名到根节点上，正是原版的名字） |
| ② 命中区尺寸 | 命中 quad 的渲染矩形 = `96.86×98.13` | ⛔ 不 —— `MenuDraw.Hit` 那条链一个字没动 |
| ③ 命中区中心 == 可见面渲染中心 | `Background` 的 quad 中心 | ⛔ 不 —— 见面等比后**仍居中**（`CloseBg` 中心没动、`m_Pivot=(0.5,0.5)`、`padding=0`） |
| ④ 换图层 | `WindowButton.target.gameObject.name == "Background"` 且 `.Texture.name == "40k_general_bt_yellow"` | ⛔ 不 —— `target` 实参仍是 `closeFaceQ` |

**🧨 牙口两半都还在**：① **名字那半** —— 把画黄面那颗的名字从 `"Background"` 改回 `"Icon"` ⇒ 红；
② **贴图那半** —— 把 `ArtCloseIcon` 换成 `UI_Button_Round_background` ⇒ 红。
（③ 的独立锚「命中节点整体搬走 ⇒ 只这条红」也照旧成立。）

### 3·2 注释 —— **改了**（那里原来写着已经不对的话）

改前那句「圆底盘 `UI_Button_Round_background` 落在自建子件 `Base` … 按兄弟窗 `Shell/BoosterInfoPopup.cs:343`
的先例叫 `Base`」**现在既不是事实、也不是好先例** ⇒ 重写成「已画在根节点上 + `Base` 取消」，
并把 🧨 ① 里「改去指圆底盘 `Base`」改成「改去指圆底盘那颗 **`closeQ`**（即根节点 `Generic Close Button Orange` 上那一颗）」。
另加了一段 B4 的**直读判据**（`pa.py` 三行读数 + 那三张 sprite 的 `m_Rect`）。

### 3·3 ⚠️ 如实记一处**断言缺口**（本件没补，理由写清）

🔴 **本文件里没有任何一条断言钉住「圆底盘那颗 quad 的渲染矩形 = 74.39×74.39（= 根矩形 + 等比）」**
⇒ 将来把这处**改回 `CloseBg` 或去掉 `keepAspect`**，**四条断言一条都不会红**（静默）。

**为什么没补**：本件白名单把 `Editor/RewardsScene.cs` 限成「**只许**改那颗 `A1125Close("InboxWindow", …)`
及其**紧邻注释**」⇒ 新增一条 `Check(...)` 已越界。已在 `RewardsScene.cs` 那段注释里**就地写明这个缺口**
（连带写上补它要用的尺子），并在此请调度台裁：**要不要另派一件**给这处补一条（白名单放开一行即可）。

---

## §四 影响面 · 验证读数

### 4·1 影响面（全仓现读）

| 查什么 | 结果 |
|---|---|
| 全仓 `"Icon (X)"` | **1 命中，且只是本件注释里引用的那句话**（B3 改前是 2 处、已清）；`Editor/` **0** |
| 全仓 `Find("Background")` / `FindChild(…,"Background")` 落在**收件箱关窗钮**上的 | **0**（其余 80+ 处是各窗自己的同名子件） |
| `Editor/` 里碰 `InboxWindow` 关窗钮的 | **只有 1 处** = `RewardsScene.cs:8906` 那颗 `A1125Close` |
| `Editor/ShellScene.cs` 的 `InboxWindow` 夹具（`:4390` 起） | 只用它量 `MsgList` / 三颗 TMP 文字，**不碰关窗钮**（`grep "Generic Close"` 在该段 0 命中） |
| `Editor/MainMenuScene.cs:5737` 的 `FindChild(t,"Generic Close Button Orange")` | `t` = **Leaderboard 窗**（`lb`），⛔ 不是收件箱 |
| 运行时代码（`Shell/*` 除本件 · `Core/` · `Board/` · `Battle/`） | 无一处按名字取本窗关窗钮子件 |
| `closeBaseQ` 这个变量名 | 全仓原只有本件一处；已随改名一并消失（`closeQ`），**无外部引用** |
| `Rect` 换 `Node` 对父链的影响 | `MenuDraw.Local` 只读父的**世界位置**（`PosInDesignSpace`）、不读父的 `sizeDelta` ⇒ 子件位置不受「父从 RectTransform 变成裸 Transform」影响；`ViewportClip.Resolve` 走 `FindAbove(parent)` 沿 `Transform` 父链、**不要求 `RectTransform`**（两处先例已证明这条路在生产里跑得通） |

### 4·2 验证读数

- **秒级类型检查**：`TMPDIR=/tmp/wf_b4 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ `--- 运行时程序集 --- 运行时错误数: 0` · `--- 编辑器程序集 --- 编辑器错误数: 0`
  （在**两处编辑全部落盘之后**跑的；**没出现**「错在别人正在写的文件上」那种情况）
- **⛔ 未跑 Unity 自检**（按红线）。
- **行尾**：两文件改完仍**纯 LF**（二进制读 `CRLF=0`）。
  `InboxWindow.cs` 644 → **678** 行 · `RewardsScene.cs` 10695 → **10706** 行。
- **`git diff --numstat`**：
  - `Unity/MyGame/Assets/CardPresentation/Shell/InboxWindow.cs` → **`58 9`**（含 B3 未提交的那一段）
  - `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs` → **`201 0`**（**全是纯插入**，含本波前序写手的未提交新增；
    本件只落在 `@@ -8830,0 +8877,31 @@` 这一个 hunk 里，`git diff` 逐行核过）
- `git diff -U0` 的 hunk 头：`InboxWindow.cs` 两个（`-334,8` = B3+B4 合成 · `-365` = B3 的另一处）、
  `RewardsScene.cs` 三个（`-116` · `-8830`（本件）· `-10279`，后两个是前序写手的）

---

## §五 没查清的部分 · 顺带发现 · 需要哪几条自检

### 5·1 没查清的部分（如实）

1. **没有渲染验收**。本件只做了「直读判据 + 算式对齐」，**改后实际画出来是不是 74.39×74.39**
   ⛔ 本件**没跑 Unity**、没出图 ⇒ 要跑 `RewardsScene.Run` 才能读到 `QuadRectPx` 的真值。
   判据链上唯一的「我们的实现」那一段 = `MenuDraw.Rect` → `CardbackFace.Fit` 的兜底支
   （已逐行读过：贴图 237×237 当 `m_Rect`、`padding=0` ⇒ `fw=fh=74.39`），**但那是读代码、不是实测**。
2. **`m_PixelsPerUnitMultiplier` 在本仓的等价物**：本件只确认它对这三颗是死值（`Simple` + `Border=0`），
   **没有**去核 `MenuDraw.Nine` 那条（角块 = `m_Border ÷ ppuMul`）在本窗有没有使用点 —— 本窗这三颗不走 `Nine`。
3. **原版那颗圆底盘「有 Image 的根节点」的 `RectTransform`**：我们是 `MenuDraw.Rect` 建的**裸 `Transform`** 节点
   （两处先例同样如此）⇒ **少了那个 `sizeDelta = 74.39×75.60`**。这是**本仓共用的有意偏离**
   （`MenuDraw.Rect` 恒 `new GameObject`、`Node` 才给 `RectTransform`），⛔ 不在本件范围，**未动**、**未查它的影响面**。
4. **`BoosterInfoPopup.cs:342` 那颗自造子件 `Base`**（以及 `LeaderboardWindow:685` / `PlayerProfileWindow:376`
   的 `Image`、`ChatPanel:297` / `CollectionWindow:1524` 的 `Background Round`）**同样是这个偏离**，
   但**都在本件白名单外**、**本件一个字没动** ⇒ 请调度台立账（判据就是 §二那两条 `pa.py` 读数）。

### 5·2 顺带发现的（`Shell/` 在改之外的窗，⛔ 本件没动）

- 🔴 **简报里「`BoosterInfoPopup` 用根矩形」这句是错的**（实测用 `CloseArtR`）—— 见 §二 2·1。
  它是本件**唯一**需要推翻的简报前提；因为推翻了它，本件才改照 `BaseOfferPopup` / `ReferralPopupWindow` 的形状。
- `Shell/BoosterInfoPopup.cs:342`：圆底盘挂在**自造子件 `Base`** 上、用**子件矩形** —— 与 `Booster Info Popup`
  的原版结构（圆底盘在根节点上、用根矩形）不符（两处 `pa.py` 读数已附 §二）。
  它**不在**本件白名单 ⇒ 只报不动。

### 5·3 需要哪几条自检覆盖

- **必跑**：**`RewardsScene.Run`**（宿主 = 唯一改了断言注释的文件；那颗 `A1125Close` 就在其中）。
- **建议加跑**：**`ShellScene.Run`** —— `Shell/InboxWindow.cs` 是 `Shell/` 的运行时件，
  且它是 `ShellScene` 里「TMP 文字四态」那段的**真窗夹具**（`Editor/ShellScene.cs:4390` 起）。
  ⚠️ 那段夹具**不碰关窗钮**、且本件**没动文字**，所以它**不是必跑**；加跑只是稳一手。
- 🔴 **本件改的是渲染几何（矩形 + 等比），而 `RewardsScene.Run` 那四条断言里**没有一条量圆底盘的矩形**
  ⇒ **跑绿也不证明「圆底盘变大了」**。真验收要么补 §3·3 那条断言，要么走**并排渲图**（铁律 10 第 6 条那条）——
  两者本件都做不了，请调度台裁。
