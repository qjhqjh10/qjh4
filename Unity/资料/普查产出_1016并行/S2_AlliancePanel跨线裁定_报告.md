# S2 · `Alliance Panel` 跨线裁定 · 判据报告（只读侦察 · 2026-10-16）

> ⚠️ **落盘说明（如实记）**：这份报告的**正文由只读侦察子代理产出**（它**没有写文件权限**，本角色明令禁止创建/修改任何文件）。
> ⇒ **主对话原样代录**（内容未改一字，仅补本横幅与文末的「主对话裁定」一节）。
> 🔴🔴 **2026-10-16（第四十二会话）就地订正（铁律 5）—— 本报告有【两处】结论已被后续现核推翻，⛔ 别照它们办事**：
> ① **§1.2 的「绝对矩形 x[853.0, 1668.0]」—— x 列算错了，实为 `x[-107.0, 708.0]`**。
>    错因 = **给 x 加了 960（屏半宽）而给 y 没加 540**，混了两种原点约定。三条独立证据：
>    (a) **本报告自己引的探针原样输出**就是 `x[-107.000, 708.044]`；(b) `AlliancePanelWindow` 文件里 `RootR` 复算 **14/14 吻合**；
>    (c) `资料/说明书/…/2D层_battlearena1全树.md:759` 原文 `Alliance Panel (inactive) [-107,1022 815x475]`。
>    ⇒ **本扇窗在屏幕【左上】**（左缘出屏 107 px，与「敌方名牌在左上」自洽），**⛔ 不是右上**。
>    ⚠️ **连带**：§6.3 的「相交 ≈483×311 px」⇒ 按绝对帧重算 = **≈124×311 px**（**「两窗同开可见」这个方向不变**）。
>    ⚠️ **`y` 那一列的两种读法仍不一致** ⇒ 按本线坑 13「两处打架先别照任一处动手」**留待现核**。
> ② **§⑧·1 的「`Mulligan`/`ChooseCardMenu` 子件断链」不成立** —— 那是 **`MulliganAnchor` / `ChooseCardMenuAnchor` 两个纯 `Transform` 的锚标记件**
>    （在 `Transform/` 目录、**本来就没有 `m_AnchorMin` 可读**）⇒ **不是包缺数据**（判据 = 只读侦察 `S3` 现核）。
> 📌 **本报告唯一【未被推翻】的主干结论**：**路 B（就地翻 uGUI、不动壳侧窗口系统）** —— 见 §⑨，且**已执行**（`A9` 落地，`typecheck` 0/0）。

> **侦察代理**：`Explore` 型 · 代号 `S2` · 日期 **2026-10-16**（第四十二会话）。
> **任务书**：给主对话提供**裁断「`Alliance Panel` 走哪条路」所需的全部判据**；结论由主对话下，代理只交证据。
> ⚠️ **全部结论只给证据，不下结论**（除 §⑦ 末尾那一条「哪条看起来代价小 + 依据」，按要求给）。
> 行号一律**现核**；与任务书给的旧行号不一致处已标 🔴。

---

## ① 原版那一格

### 1.1 `T09c` 表里的那一行（逐字段原文）

出处 = `资料/普查产出_1016战斗判据/T09c_面板到原版宿主映射.md:39`（表头 = `:20`，列序 = 我们的类 | 我们的文件:行 | 原版 GO @ 宿主 | rm | overrideSorting | 排序层 | order | 兄弟序 | 出处）：

| 字段 | 值 |
|---|---|
| 我们的类 | `AlliancePanelWindow`（= 原版 Alliance Panel，**我们有**） |
| 我们的文件:行 | `our/Shell/AlliancePanelWindow.cs:81`（建点写 `BattleDriver.cs:15060`；入口写 `BattleDriver.HandleAlliancePanel:4049` —— 🔴 **两处行号都过期**，见 §③） |
| 原版 GO @ 宿主 | **`Alliance Panel`** @ `…/Safe area FrontCanvas` |
| rm | **继承 `FrontCanvas`(1)** |
| ovr | **(父 true)** |
| 排序层 | **`OverHand`** |
| order | **0** |
| 兄弟序 | **13/14** |
| 出处 | `RT_3052` · `MB_4314`=`BattleAlliancePanel` · GO 子件 = `Close Background`/`Background`/`EnemyInfo`/`NotInaAllianceText` |

同一条被 `T09_整层换uGUI.md:666`（§八 开头的原版 14 子件序）与 `:671`（§八·2 那段判据原文）引用。

### 1.2 实读 `bundle_scenes_scenes_battlearena2`（取值只用 arena2 ✅）

**兄弟序自证**：`Safe area FrontCanvas`(RT 3055) 的 `m_Children` **恰好 14 个**，`[13] = Alliance Panel (RT 3052)` ⇒ 13/14 与 arena1 同构（`RT_3055` = `T09c` 表里多行共用的同一个祖先 pid ✓）。

**根组件表**（`GameObject/Alliance Panel.json`，`m_Component` 5 项，`m_FileID` 全 0）：`3052`(RectTransform) · `2041` · `3776` · `5469` · **`4314`(= `BattleAlliancePanel`)**。

**根 `RectTransform_3052`**：`aMin=(0,1)` · `aMax=(0,1)` · `pivot=(0.13,0.825)` · `pos=(-1.04443359375, -25.271728515625)` · `sd=(815.0440063476562, 475.469970703125)` · `scale=0.9999718070030212`。

**探针现核**（本线专用口，原样命令与输出）：
```
python -I -X utf8 d:/4/Unity/工具/_probe_node_rect.py bundle_scenes_scenes_battlearena2 "Alliance Panel" "FrontCanvas"
→ 链（自下而上）：Alliance Panel(3052) → Safe area FrontCanvas(3055) → FrontCanvas(3093)
  （后两级都是 aMin(0,0) aMax(1,1) pos(0,0) sd(0,0) scale 1 ⇒ stretch 到 1920×1080）
→ 目标中心（相对 FrontCanvas 中心，y 向下）= (300.522, 179.799)；尺寸 = 815.044 × 475.470
→ 矩形（y 向上）：x [-107.000, 708.044] · y [-417.534, 57.936]
```
⇒ **绝对矩形（1920×1080，左上原点）= x[853.0, 1668.0] · y[-57.9, 417.5]**（**顶出屏 57.9 px，原版就长这样**）。与 `AlliancePanelWindow.cs:95` 的 `RootR = (-107.00, -57.93, 708.02, 417.52)`（= 相对画布中心、y 向下）**逐值吻合** ✓。

### 1.3 子树（battlearena2 实读；缩进 = 层级；值 = 序列化 `sizeDelta` / `anchoredPosition` / `pivot` / `anchorMin-Max` / `localScale`）

```
Alliance Panel                 RT 3052  sd(815.044,475.470) piv(0.13,0.825) a(0,1)-(0,1) scale 0.99997
├ Close Background             RT 2891  sd(11668.328,8012.037) pos(351.755,41.660) a(0.5,0.5) scale 1  【整屏熄灯层】
├ Background                   RT 3361  sd(722.660,403.130)   pos(371.0,206.0)     a(0,0)     scale 1
│ ├ BG                         RT 3420  sd(557.314,301.346)   pos(28.816,5.615)    a(0.5,0.5) scale 1
│ └ BGFrame                    RT 3534  sd(716.882,399.100)   pos(2.222,-1.572)    a(0.5,0.5) scale 1
│   ├ BGFrame Top              RT 3609  sd(517.687,33.923)    pos(16.372,171.305)
│   ├ BGFrame Bottom           RT 3316  sd(517.687,36.532)    pos(17.109,-153.833)
│   ├ BGFrame Right            RT 3602  sd(83.650,371.064)    pos(317.094,13.590)
│   └ BGFrame Left             RT 2927  sd(117.321,386.982)   pos(-300.237,-5.407)
├ EnemyInfo                    RT 3320  sd(260.0,75.0)        pos(157.0,-107.735)  a(0,1) piv(0,1)
│ ├ Avatar Item Small          RT 3030  sd(212.535,186.389)   pos(-85.016,-50.810) a(0.5,0.5) scale 0.8
│ │ ├ Raycast Target          RT 3288  sd(109.054,122.106)   pos(0.449,7.023)
│ │ ├ Image Container         RT 3247  sd(0.0,-37.368)       pos(0.0,18.684)      a(0,0)-(1,1)
│ │ │ ├ Highlight             RT 3230  sd(3.338,1.855)       pos(1.669,2.600)     scale 1.9
│ │ │ ├ Border                RT 3233  sd(0.0,0.0)           pos(0.0,0.0)         a(0,-0.1)-(1,1) scale 1.25
│ │ │ └ Image                 RT 2909  sd(0.0,0.0)           pos(0.0,2.7)         a(0,0)-(1,1) scale 2.0
│ │ └ Avatar Name             RT 3664  sd(≈0,41.314)         pos(0.0,0.0)         a(0,0)-(1,1) piv(0.5,1)
│ ├ PlayerInfo                RT 3558  sd(376.970,151.229)   pos(188.49,-50.814)  a(0.5,0.5)
│ │ ├ NameHolder              RT 2843  sd(0.0,42.575)  pos(0,0) a(0,0) 【VLG 模板位：0 宽】
│ │ │ ├ PlayerLabel           RT 3400  sd(117.427,51.0)      pos(0.0,-0.051)  scale 0.8
│ │ │ └ Name Text             RT 2972  sd(348.760,51.0)      pos(235.724,-0.051) scale 0.8
│ │ ├ TitleHolder             RT 3454  sd(0.0,38.443)  pos(0,0) a(0,0) 【VLG 模板位】
│ │ │ ├ TitleLabel            RT 3080  sd(118.042,51.0)      pos(-0.005,-0.694) scale 0.8
│ │ │ └ Title Text            RT 3462  sd(348.760,51.320)    pos(235.719,-0.342) scale 0.8
│ │ └ Player Rating Display   RT 3304  sd(0.0,60.539)  pos(0,0) scale 0.8 【VLG 模板位】
│ │     ├ Secondary Icon      RT 3731  sd(61.820,60.539)
│ │     ├ Main Icon           RT 3650  sd(0,0)   ← 尺寸运行期由 RatingDisplay 算
│ │     └ Individual rating value RT 3518 sd(0,0)
│ ├ Alliance                  RT 3690  sd(551.609,135.954)  pos(118.9,-201.263) a(0.5,0.5) 【组内无布局组】
│ │ ├ Alliance Label          RT 2828  sd(118.644,51.0)    pos(160.0,23.377)  scale 0.8
│ │ ├ Alliance Name           RT 3467  sd(321.118,51.0)    pos(384.3,23.377)  scale 0.8
│ │ ├ Alliance Badge Drawer   RT 3698  sd(139.706,131.104) pos(-197.46,8.971)
│ │ │ ├ Frame                 RT 2788  sd(0,0)  a(0,0)-(1,1) piv(0,0)
│ │ │ └ Badge                 RT 3476  sd(0,0)  a(0,0)-(1,1) piv(0,0.5)
│ │ └ Alliance Rating Display RT 3303  sd(184.184,60.539) pos(230.543,40.430) scale 0.8
│ │     ├ Secondary Icon      RT 3729  sd(61.820,60.539)
│ │     ├ Main Icon           RT 3651  sd(0,0)
│ │     └ Individual rating value RT 3517 sd(0,0)
└ NotInaAllianceText          RT 2853  sd(592.657,46.200)   pos(399.83,-95.700)  a(0,0.5) scale 0.8
```
⚠️ 两点与任务书给的子件表**字面一致**（4 个直接子件 ✓）；但**第 5 个直接子件 `NotInaAllianceText` 在任务书那行里被列成了 `EnemyInfo` 的兄弟**——实读确认它是**根的直接子件**（`RT_3052.m_Children` 里第 4 个）✓。

⚠️ **一个未解的小歧义（如实记）**：arena2 里有**两个** GO 名叫 `Safe area FrontCanvas`（RT **3055** 与 RT **2236**）。我用 3055（= `T09c` 表里多行共用的 `RT_3055.m_Children`）✓ 与表一致；2236 那个我没查它挂在谁底下（**没拿它当答案**）。

---

## ② 我们这边现读：`d:/4/Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs`

**真身路径现核** = `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs`（**不是** `Assets/Shell/...`）✓ 与任务书一致。**行数 = 416**。

### 2.1 怎么建件（全部走**世界空间**那一套）

| 口 | 位置 | 用途 |
|---|---|---|
| `MenuDraw.ApplyPxRect(go.transform, parent, RootR)` | `:208` | 根框（世界档：`sizeDelta` 走 `LayoutSpace.Px` = **÷108**） |
| `MenuDraw.Rect(...)` | `:224`（熄灯层）· `:231`（`BG`）· `:233-236`（`BGFrame` 四块）· `:251`（头像框 `Border`，`keepAspect:true`） | 5+ 处，返回 `ImageQuad` |
| `MenuDraw.Node(...)` | `:230` `Background` · `:232` `BGFrame` · `:263` `NameHolder` · `:270` `TitleHolder` · `:280` `Alliance` | 空节点 |
| `MenuDraw.ShadeHit(shade.transform, ShadeR, QShade, QContentMin, Close, "CloseHit")` | `:227` | 熄灯层命中（**建出 `WindowButton`**，`:2211` 内部转 `Hit`） |
| `MenuDraw.Absorb(_root, "BackgroundAbsorb", BackgroundR, QShade, QContentMin)` | `:245` | 窗内吸收层（**也建 `WindowButton`**，另置 `absorbOnly = true`，`:2298`/`:2327`） |
| `MenuDraw.Text(...)`（经私有 `Txt`） | `:375` | 7 颗字 |
| `MenuDraw.AlignLeft` / `MenuDraw.SetVAlign` | `:354-356` `:380` / `:379` | 对齐 |

### 2.2 渲染队列阶梯（逐值，现读 `:85-92`）

```
QShade     = 3980   // 熄灯层 Close Background（必须严格低于 QContentMin）
QBg        = 4000   // Background / BG
QFrame     = 4010   // BGFrame 四块
QAvatar    = 4020   // 头像框 Border
QText      = 4030   // 全部文字
QContentMin= QBg = 4000   // 传给 ShadeHit / Absorb 的「本窗内容最低档」
```
全部 ≥3980 ⇒ 都在战斗 HUD（默认档 3000）之上。**分层完全靠 `renderQueue`，⛔ 不是 z**（文件头 `:48-51` 有理由：熄灯层中心在 x≈+650 px、离相机比面板本体近，靠 z 排会把它画到面板上面）。

### 2.3 九宫格 / `Sliced` / 命中 / `raycastTarget`

- **九宫格：没有。** `grep -n "Nine\|Tiled\|Sliced" Shell/AlliancePanelWindow.cs` ⇒ **0 命中**。全部走 `MenuDraw.Rect`（= `ImageQuad`，Simple，逐块显式矩形）。
- **命中件：有 2 颗**，就是 §2.1 的 `ShadeHit`(`:227`) 与 `Absorb`(`:245`) —— 都挂在 `WindowButton` 上。⚠️ 文件 `:326-328` 与 `:414-415` **自陈**：「那扇窗在 `Build` 里挂的 `MenuDraw.ShadeHit` 上是个 `WindowButton`，**只有外壳那套 `PointerLayer` 会派发它** —— 战斗里不走那条路 ⇒ 关窗那一手由调用方（`BattleDriver`）接」。
- **`raycastTarget`：不适用。** 本件走的 `ImageQuad` 是**世界空间 quad**，没有 `raycastTarget` 这个口（同族先例 → `Battle/ChatPopupPanel.cs` 的 `_close`；`BattleDriver.cs:15064-15066` 逐字写着这条）。命中一律「自己算 `Contains(世界坐标)`」。

### 2.4 其它规模事实（写路径 B 要用）

- 文件内**已经全部是实绘 px 的 `PxRect` 常量**（`:94` 明写「画布像素 · 左上原点 · 1920×1080」）：`RootR:95` · `ShadeR:104` · `BackgroundR:108` · `BgR:110` · `FrameR:113` + 四块 `:114-117` · `AvatarBorderR:120` · `NameRowR:123`/`NameLabelR:124`/`NameTextR:125` · `TitleRowR:127`/`TitleLabelR:128`/`TitleTextR:129` · `AllianceGroupR:131`/`AllianceLabelR:132`/`AllianceNameR:133` · `NotInAllianceR:135`
- 色/透明：`ShadeAlpha=0.4627`(`:105`) · `BgTint=(0.349,0.349,0.349,1)`(`:111`) · `NotInAllianceAlpha=0.772549`(`:136`)
- 字号 7 颗（**已乘 `localScale 0.8`**）：`:141-147`；自适应四格：`AutoMinPx 14.4` / `AutoBasePx 28.8` / `AutoMaxPx 28` / `AutoMaxNamePx 32`（`:159-162`）
- 词条 4 条 + 英文兜底：`:167-178`
- 取图三级兜底 `CardArt.Ui → MenuUi → DeckUi`：`:394`
- 建出来的东西 / 公开面：`Visible`(`:186`) · `Root`(`:187`) · `MissingArt`(`:189`) · `NotInAllianceNode`(`:191`) · `AllianceGroupNode`(`:193`) · `TitleHolderNode`(`:195`) · `NameLabel`(`:197`) · `Create(parent, name)`(`:202`) · `Open(name,title,allianceName)`(`:305`) · `Close()`(`:314`) · `HitBody(world)`(`:329`)
- **已知缺口（旧账，与迁 uGUI 无关）**：头像立绘不画 · 两处评级不建 · 徽章 `Frame`/`Badge` 不建 · 出场 `DOFade`+`DOScale` 没做（三常量没读）—— `:53-71`

---

## ③ 它被谁调（逐调用点 + 「只在战斗里用吗」）

`grep -rn "AlliancePanelWindow" d:/4/Unity/MyGame/Assets --include=*.cs` ⇒ **全仓 16 命中 / 7 个文件**，逐条读：

**（a）真调用点 —— 只有 `Battle/BattleDriver.cs` 一个文件：**
| 行 | 内容 |
|---|---|
| `:1197` | `AlliancePanelWindow _alliancePanel;`（字段） |
| `:1196` | 注释：**「不是 `GameWindow`」⇒ 不注册进 `WindowsManager`**，出厂就关着 |
| `:15084` | 🔴 **真正的建点** `_alliancePanel = AlliancePanelWindow.Create(root);`（任务书给的 `:15060` 是**注释行**——「🖱 点它做什么」那一段，⛔ 已过期） |
| `:4051` | 🔴 **`bool HandleAlliancePanel()` 的方法声明行**（任务书给的 `:4049` 是它的 **doc 注释行**）。`:4048` 也是 doc |
| `:4068` | `_alliancePanel.Open(AlliancePanelName, "", "");` |
| `:4075-4098` | 自检口 6 个：`AllianceButtonBuilt:4075` · `AlliancePanelBuilt:4076` · `AlliancePanelVisible:4078` · `AlliancePanelRef:4080` · `AllianceButtonWorldPos:4082` · `AllianceButtonDrawnPx:4088` · `AllianceButtonTint:4096` |
| `:8400` | **唯一调用点**：`if (HandleAlliancePanel()) { UpdateHud(); return; }`（`Update` 那条输入链里，模态） |
| `:15069-15081` | 那颗钮 `AlliancePanelOpenButton` 的建点（`HudImageTex(root, CardArt.Solid(), …, Px(alHit.H), "AlliancePanelOpenButton")`，`SetAspect` 拉成命中矩形 + `SetTint` 全透明） |

**（b）断言侧 —— `Editor/BattleScene.cs`（A985③ 那一族）：** `:21458` `:21470`（`var panel985 = driver.AlliancePanelRef;`）· `:21522` `:21527` `:21532` `:21534` `:21550` `:21556` `:21573` `:21576` `:21612` `:21626` `:21680`。它读的是**节点名**（逐字照 `Build` 的 `MenuDraw.Node(…)` + `Txt(…, name)`，`:21626` 自陈）与 **`Label` 的两态/左缘**。

**（c）其余 5 个文件的命中** —— **逐条读过，全是注释，零调用**：
- `Core/Loc.cs:1693` · `:1695`（讲 `is is` 笔误与 `LocOr` 消费点）
- `Shell/EnergySinglePlayerOnlyEventWindow.cs:445`（引 `AlliancePanelWindow.cs:373` 的 `SetWrapping(false)` 做先例）
- `Shell/SettingsWindow.cs:2157`（引 `LocOr` 做同形比对）
- `Shell/PopUpGameWindow.cs:171`（引「不需要像 `AlliancePanelWindow` / `DailyRewardPopup` 那样乘刻度」）
- `Editor/BattleScene.cs:21556` `:21573` `:21612` `:21626`（注释，见上）

### ⇒ 判定：**今天它【只在战斗里用】**
- **壳侧（主菜单/`MainMenuScene`/`ShellScene`/`ShellRuntime`/`WindowsManager`）零调用点** —— 上表 (c) 那 5 个文件**一行调用都没有**。
- 它**住在 `Shell/` 目录**只是因为「同一个作者的分类」；类自己 `:78-80` 写着「**它是「面板」不是「窗」**：根组件是普通 `MonoBehaviour`、**不是 `GameWindow`** ⇒ 没有 `type`/`placement`/`closeOnESC`、也⛔ 不注册进 `WindowsManager`」⇒ **壳的窗口系统今天根本没接管它**。
- **⇒ 任务书里担心的「要不要双模式」，今天的前提不成立**（「双模式」是为「跨场景共用件」准备的形状；本件今天**Battle-only**）。⚠️ **但有一条我没查**（见 §⑧）：主菜单那一侧的「看档案」今天是别的窗（如 `Shell/PlayerProfileWindow.cs` / `AllianceMemberTab.cs`），它们与原版 `Alliance Panel` 的关系**我没核**（超本任务范围）。
- ⚠️ 它建在 **`hudRoot`** 下（`BattleDriver.cs:13874` `BuildHud()` → `:14508` `BuildHudExtras(root)` → `:15084` `Create(root)`）；`hudRoot` = **裸 `Transform`**（= 原版 `BattleHud` 那一级，`BattleDriver.cs:13886-13894` 判据自陈）⇒ 世界空间、且**在 uGUI 根 canvas 之外**。

---

## ④ 壳侧窗口系统的接口面

### 4.1 行数（现核）
| 文件 | 行数 |
|---|---|
| `Shell/MenuDraw.cs` | **3145** |
| `Shell/WindowsManager.cs` | **1079** |
| `Shell/PopUpGameWindow.cs` | **513** |
| `Shell/MenuWindowBase.cs` | **289** |
| `Shell/WindowHolder.cs` | **37** |
| （相邻承重件）`Shell/GameWindow.cs` | **756** |
| （相邻承重件）`Shell/PointerLayer.cs` | **1361** |
| （相邻承重件）`Shell/WindowButton.cs` | **852** |
| `Shell/` 目录 `.cs` 总数 | **109** |

### 4.2 对外口
- **`MenuDraw`**：**61 个 `public static`**（`:22` 起）。建件口 10 个：`Node:119` · `SetPxSize:155` · `ApplyPxRect:180` · `Rect:1531` · `Nine:1612` · `Tiled:1764` · `Text:1848` · `TextBox:1915` · `Hit:1994` · `DeckCell:2881,2943`；压暗/吸收 5 个：`ShadeHit:2211` · `Absorb:2298` · `ShadeRuleOk:2346` · `CheckShadeRule:2427` · `CheckAbsorbRule:2597`；其余 46 个是换算（`Local:30` · `PixelOfDesign:605` · `PosInDesignSpace:71`）· 裁切（`ClipRect:221` · `ClipText:1186` · `ClipQuad:1483` · …）· 自检口。
- **`WindowsManager`**：`RegisterAnchor:173` · `UnregisterAnchor:191` · `GetWindowAnchor:205` · `HasAnchor:221` · `ClearAnchorsForTest:223` · `OpenWindow:253` · `TopWindow:326` · `HideAllWindows:352` · `SetBaseMenu:388` · `ShowMainMenu:405` · `AttachToAnchor:417` · `CloseAllWindows:584` · `OpenByRef<T>:741` · `StillOpen:775` · 7 个 `OpenXxx` 便捷口（`:803-920`）· `ShowPopUp:959` · `ShowMessagePopUp:1014` · `HidePopUp:1061`。枚举 `WindowsPlacement:49`（None/Canvas=5/World=10/Popup=15）· `WindowType:52` · `WindowState:64`。
- **`WindowHolder`**（37 行）：唯一对外口 = `RegisterNow()`(`:36`) + `placement` 字段(`:27`)；`[ExecuteAlways]` 在 `:24`。
- **`MenuWindowBase`**（289 行）：对外 = `static class WindowHeader`(`:42`) 的 `WithBackButton(root, Spec):186` + `Spec:119` + `Parts:175` + 三个 enum（`TitleFit:82` / `BackStyle:104`）。⚠️ 它**不含窗口基类**——窗口基类在 `Shell/GameWindow.cs`。
- **`PopUpGameWindow`**：`class PopUpGameWindow : GameWindow`(`:84`)，全部常量都是**世界空间**（`QShade = 3560`(`:93`) · `ArtPopup:101` 九宫格 `(169,160,169,160)` · `Win1R/Mask1R/…:117-142`）。
- **`GameWindow`**：基类 4 个建件口 `DrawRect:233` · `DrawNine:241` · `AddHit:274` · `Text:331` —— **全部是世界档 `MenuDraw` 转调**。

### 4.3 调用点计数（现核，grep 计数）
- `MenuDraw.*`：**3309 处**，分布在 **119 个 `.cs`**。
  分口：`Node` **367** · `Rect` **265** · `Text` **171** · `Nine` **92** · `Hit` **88** · `TextBox` **57** · `ShadeHit` **38** · `Absorb` **35** · `SetPxSize` **32** · `Tiled` **11**。
- `WindowsManager.*`：**579 处**，分布在 88 个文件。
- `GameWindow` 派生类：**42 个**（`grep "class .*: *GameWindow|GameWindowWithTabs|MainMenuSubmenuWindow|LiveOpsEventWindow"` 计数）。

### 4.4 要让它「同时支持 uGUI」，大致要动哪几处（给点，不写代码）
1. `Shell/MenuDraw.cs:1531/1612/1764/1848/1915/119/1994/2211/2298/2881` —— **10 个建件口**，每个都要有 uGUI 档或旁路。
2. `Shell/MenuDraw.cs:155`(`SetPxSize`) / `:180`(`ApplyPxRect`) / `:30`(`Local`) —— **量纲绑死 `LayoutSpace.Px`（÷108）**（🔴 判据 = `T09` §五·补⑩ 一：uGUI 档沿用会把整扇面板缩成 1/108，**画面只是小得看不见、不报错**）。
3. `Shell/GameWindow.cs:233/241/274/331` —— 42 个派生窗全走它。
4. `Shell/WindowButton.cs`（852 行）+ `Shell/PointerLayer.cs`（1361 行）—— 命中派发那一族也全是世界档（`ImageQuad` + `HitBoxPx`）。
5. ⚠️ **还要先给壳侧造一块根 canvas**：`grep -rn "AddComponent<Canvas>\|CanvasScaler" Shell/ Deck/` ⇒ **只有一处注释命中**（`Shell/MainMenuRuntime.cs:1967`）；`HudCanvas.*` 的调用者**全在 `Battle/` + `Core/Tooltip.cs` + `Core/TmpFont.cs` + `Editor/BattleScene.cs`** ⇒ **`HudCanvas` 是战斗侧专用设施，壳侧今天没有任何 canvas 基础设施**（`T09` §五·补⑨ 一 也写着「全工程没有任何一个 `.unity` 带 `CanvasScaler`」）。
6. **本仓已有的先例（⛔ 不是「整层双后端」）**：`Tooltip`（`UseHost(host)`，战斗侧登记 `Blocking`/0 子画布、卡组编辑侧不登记 ⇒ 自动留世界档，`T09` §五 第 3 步）· `ErrorMessageBanner`（`Create(..., uguiHost)`，战斗侧传 `PopUps`/1、`Shell/MessageToast` 不传 ⇒ 留世界档，`T09` §五 第 4 步）⇒ **范式是「一扇窗一层」的选择性双模式，不是把 `MenuDraw` 整体做成双后端**。

---

## ⑤ 战斗侧现成可复用的 uGUI 口（`Battle/HudCanvas.cs`，548 行）

**建层/建件口（逐个签名，`:行号`）：**
| 签名 | 行 |
|---|---|
| `static RectTransform Ensure(Camera cam)`（建根 canvas，幂等；`Root`/`RootRect` 属性在 `:58`/`:60`） | `:88` |
| `static RectTransform MakeImage(Transform parent, Texture2D tex, string name, float x, float y, float w, float h, Color color, bool raycast = false)`（x/y = **屏幕绝对 px** 左上角） | `:124` |
| `static HudPiece MakeImageInParent(Transform parent, Texture2D tex, string name, float xPx, float yPxFromTop, float wPx, float hPx, float z, Color color)`（**相对父中心**） | `:158` |
| `static HudPiece MakeSlicedPiece(Transform parent, Texture2D tex, string name, float wPx, float hPx, Vector4 border, Color color)`（`Image.Type.Sliced`；⚠️ `ppuMul` 恒 1，`:186-187` 自陈原版那字段没读到） | `:188` |
| `static HudPiece MakeImagePiece(Transform parent, Texture2D tex, string name, float x, float y, float w, float h, Color color)` | `:279` |
| `static HudPiece QuadFromTopPx(Transform parent, Texture2D tex, string name, float xPx, float yPxFromTop, float hPx, float z, Color color)`（**按高定、宽由贴图比例来**） | `:252` |
| `static HudPiece MakeAnchored(Transform parent, Texture2D tex, string name, float x01, float y01, Vector2 pivot, float hPx, float wPx, float z, Color color)` | `:520` |
| `static Label MakeText(Transform parent, string text, float px, float py, int scale, Color color, Vector2 anchor, float z, string name = null)`（py **y 向上**、相对父中心、**设计 px**） | `:505` |
| `static RectTransform EnsureSubCanvas(Transform parent, string name, int sortingLayerId, int sortingOrder)`（**按名字幂等**） | `:421` |
| `static Canvas SubCanvas(string name)` | `:264` |
| `static void FillParent(RectTransform rt)` | `:219` |
| `static void SetPxSize(RectTransform rt, float wPx, float hPx)`（`sizeDelta` 吃**真 px**） | `:231` |
| `static Vector3 PosPx(float xPx, float yPxFromTop)` | `:241` |
| `static void Add(RectTransform rt, float z)` / `static void Reorder(RectTransform rt, float newZ)` / `static float OrderZOf(RectTransform rt)` / `static Group GroupOf(RectTransform rt)` | `:369` / `:376` / `:383` / `:359` |
| 自检口 `CenterPx:288` · `SizePx:299` · `RenderModeValue:67` · `PlaneDistanceValue:68` · `SortingOrderValue:69` · `ReferenceResolutionValue:71` · `ScreenMatchModeValue:76` · `HasGraphicRaycaster:80` | — |
| `static class SortingLayer`（9 层常量） | `:395-406`（`Default 0` · `OverHand 2007638115` · `PopUps 1054366423` · `Blocking -2008831743` …） |
| `const float DefaultZ = 0.3f` · `const float PlaneDistance = 20.3f` · `ReferenceResolution 1920×1080` | `:546` / `:52` / `:55` |

**⛔ 它【没有】的**（判据 §⑦ 路 B 的代价）：
- **没有任何「窗口壳」/「窗」级口**（没有 `MakeWindow` / `GameWindow` 那一族）。
- **没有任何命中件建口**：除 `MakeImage(..., raycast:true)` 外，所有口建的 `Image.raycastTarget = false`（`:144` `:176` `:206` `:538`）。
- **`EventSystem` 到 `T09` 第 4 步之前不建**（`:33-35` 明写：① 判据没收 ② 现在没有可交互件），`GraphicRaycaster` 随 canvas 一起建。

**`Battle/PopUpGameWindow`：不存在。** `find . -name "PopUpGameWindow*.cs"` ⇒ 全仓只有 `./CardPresentation/Shell/PopUpGameWindow.cs`。
**`Shell/PopUpGameWindow.cs` 与战斗侧的关系**：它是 `: GameWindow`(`:84`) 的世界空间弹窗；战斗侧唯一相关的是**重连弹窗**，而那是 `NetRuntime`/`WindowsManager.ShowPopUp` 走的 **Shell 那一层**（`Battle/BattleDriver.cs:236` · `:7574` · `:8324` 注释自陈「**并行的另一件事/并行现象**」）—— **不是** `AlliancePanelWindow` 这一族。
**战斗侧 `GameWindow` 派生类 = 0 个**（`Battle/SettingsPanel.cs:1418` 自陈「本件不是 `GameWindow`」；`BattleDriver.cs:1196` 自陈同）。

---

## ⑥ 中间态影响（如实核）

### 6.1 现状：已经迁到 `OverHand` 画布的**有 5 块**（`OverHand` / order 0，逐块带 `文件:行`）
| # | 子画布名 | 出处（现核） |
|---|---|---|
| 1 | `Mulligan Canvas` | `Battle/MulliganPanel.cs:601-602` |
| 2 | `Generic Multi Card Display Combat Canvas` | `Battle/MultiCardDisplay.cs:206-207` |
| 3 | `ChooseCardMenu Canvas` | `Battle/CardChoicePanel.cs:220-221` |
| 4 | `Card Display Window Canvas` | `Battle/CardDisplayWindow.cs:305` |
| 5 | `AboveShader Canvas` | `Battle/BattleDriver.cs:14430`（提示条）· `:15240`（加时 splash） |

同批登记在 `HudCanvas.SubCanvasRank`（`Battle/HudCanvas.cs:470-483`）—— 表里**只有这 5 项**（`OverHand` 段 `:477-482`）；⚠️ **没有 `Alliance Panel Canvas` 这一项**，且 `grep -rn "EnsureSubCanvas"` 全部 30 处调用点里**也没有任何一处为联盟面板建画布**。

### 6.2 机制（为什么会被压住）—— 承重假设，非实测
- `Battle/HudCanvas.cs:17-22` 自陈：「与现有 quad 互相插队 —— 靠 `planeDistance`，⛔ **不是 `sortingOrder`** —— 它会**先于** `renderQueue` 比较 ⇒ 整块 canvas **跳到所有 quad 前/后**」。
- 这 5 块的 `sortingLayerID = OverHand(2007638115)`（`:403`），而 `AlliancePanelWindow` 那 20 颗 `ImageQuad` 是**世界空间件**（隐式落在 `Default` 层）+ `renderQueue 3980/4000/4010/4020/4030`（`:85-89`）⇒ **层比较在前 ⇒ 画布整组压在世界件之上**。
- ⚠️ **这条是承重假设**：`T09` §八·5 自陈「**排序层 vs `renderQueue` 谁先比**，这条我们**没有实机验过**（现行口径不许起 player）」。

### 6.3 会不会画面可见？（几何现核，两处交集）
- `Alliance Panel` 绝对矩形 = **x[853.0, 1668.0] · y[-57.9, 417.5]**（§1.2，半个面板在屏幕上半、偏右）。
- **交集 A —— `Card Display Window` 的 `Card Display`**：探针 `_probe_node_rect.py … "Card Display" "Safe area FrontCanvas"` ⇒ 中心 (0.000, 0.000)（= **正中**）、尺寸 **752.000 × 868.000** ⇒ 绝对 x[584, 1336] · y[106, 974]。**与本面板相交 x[853, 1336] × y[106, 417] ≈ 483 × 311 px** ⇒ 两窗同时开时**可见**。
- **交集 B —— `AboveShader` 的 `TipText`**：探针 ⇒ 中心 (1.060, −91.780)（y-down 相对屏心）、尺寸 **2450.130 × 99.697** ⇒ 绝对 y[398.4, 498.1]。**与本面板相交 y[398.4, 417.5] ≈ 19 px 高、x 全宽**。⚠️ 但 `TipText` / `OvertimeSplashText` **原版出厂 `m_IsActive = false`**（`T09c:42` 自陈「子件 = `TipText`(inactive) + `OvertimeSplashText`(inactive)」）⇒ **平时不画**。
- **三块没核完**：`Mulligan` / `ChooseCardMenu` / `Generic Multi Card Display Combat` 三块原版根都是 **0×0 或 0×818.04**（探针尺寸），它们的**子件矩形我没能全解出来**（断链，见 §⑧）⇒ 「这三块压上去会不会看得见」**没有答案**。
- **出场条件我也没逐条核**：这 5 扇都是「按需开」的模态/阶段窗，**同时开**的概率与判据我没查（`T09` §八·2 只说「会**短暂地**被压住」）。

---

## ⑦ 路 A / 路 B 的改动面与风险对照（⛔ 不下结论，只给代价）

### 路 A：让**壳侧窗口系统**支持 uGUI
| 项 | 代价 |
|---|---|
| 必动文件 | `Shell/MenuDraw.cs`(3145 行 · 61 个 `public static`) 的 10 个建件口 + 3 个量纲口(`:155`/`:180`/`:30`) · `Shell/GameWindow.cs` 的 4 个基类口(`:233`/`:241`/`:274`/`:331`) · `Shell/WindowButton.cs`(852) · `Shell/PointerLayer.cs`(1361) · 外加**壳侧要先有一块根 canvas**（今天**没有**，§4.4-5） |
| 影响半径 | `MenuDraw.*` **3309 处 / 119 个 `.cs`** · `WindowsManager.*` **579 处 / 88 个文件** · `GameWindow` 派生类 **42 个** · `Shell/` `.cs` **109 份** ⇒ **全局** |
| 风险 | ① **静默失败**：量纲错 108 倍「只是小得看不见、不报错」（`T09` §五·补⑩ 一，原话）；`Transform`/`RectTransform`、锚点、`sizeDelta` 三处都在这条线上。② 已有 2 处双模式先例证明「**一扇窗一层**」可行，但**没有**「`MenuDraw` 整体双后端」的先例。③ 壳侧窗口多与滚动/裁切（`ViewportClip` / `ClipRect` / `ClipText`）耦合，那一族在 uGUI 档要靠 `RectMask2D` 语义重做。 |
| ⚠️ 关于「8 份 `阶段二_*` 窗口」 | **现核纠正**：`资料/阶段二_*.md` 是 **10 份规格文档**（`阶段二_Shell_原版规格.md` · `_卡片详情窗_` · `_卡组线_` · `_商店_` · `_多人界面_` · `_战斗入口_` · `_滚动与指针_` · `_锻造厂与战役页_` + `资料/历史/阶段二欠账_已收口_1004.md` · `资料/历史/阶段二第3层_逐件记录.md`）—— **它们是文档、不是窗口源码**；壳侧窗口源码在 `Shell/*.cs`（109 份）。⇒ 「8 份窗口在用它」这个说法**请按这个数重核**（我数到的是 109 份 `.cs` / 42 个 `GameWindow` 派生类）。 |
| 与「本扇窗」的距离 | 路 A 里**没有任何一处是「只为本扇窗」**的 —— 本扇窗今天甚至**不注册进 `WindowsManager`**（§③）⇒ 路 A 的价值是**铺路**（`OverHand` 组剩下的 `Unit Chat`/`ActiveSkillDesc`/`AttackSelector` 等），不是**这扇窗的前置**。 |

### 路 B：给**战斗侧单独重建一份** `Alliance Panel`
| 项 | 代价 |
|---|---|
| 要动文件 | ① 新件（照配方落在 `Battle/` 下）② `Battle/BattleDriver.cs:15084` 换建点 ③ 调用面保持同名即可：`:4051` `HandleAlliancePanel` / `:4068` `Open` / `:4075-4098` 自检口 / `:8400` 唯一调用点 ④ `Editor/BattleScene.cs:21458-21680` 那族断言（读**节点名**与 **`Label`**，`:21626` 自陈逐字照 `MenuDraw.Node(…)` + `Txt(…, name)`）⑤ `Battle/HudCanvas.cs:470-483` 的 `SubCanvasRank` 要**加第 14 项**（今天只有 9 项、`OverHand` 段只有 5 项） |
| 新件放哪 | 照 `T09` §五·补⑩ 六 的配方：子画布 = `Alliance Panel Canvas`，`OverHand` / order **0**，`EnsureSubCanvas`。⚠️ 原版这一格**自身没有 `Canvas` 组件**（`T09c:39` 的 rm 列写「继承 `FrontCanvas`(1)」）⇒ 做法与 `ChooseCardMenu` / `Multi Card Display` **完全同构**（那两扇的注释 `CardChoicePanel.cs:36-39` / `MultiCardDisplay.cs:23-26` 逐字给了同一条理由）。⚠️ `AlliancePanelWindow` 现在**不是**建在画布下、而是建在 `hudRoot`（裸 `Transform`）下（§③）⇒ 换父链。 |
| 要抄多少几何 | **20 个 `PxRect` + 7 颗字号 + 4 格自适应 + 3 个色/alpha + 4 张图名 + 4 条词条**，全部**已在 `Shell/AlliancePanelWindow.cs` 里**（§2.4 逐条给了行号）。 |
| **判据够不够抄？** | **够，而且不用换算。** 理由：那些常量**本来就是设计 px / 左上原点**（文件头 `:94` 明写「画布像素 · 左上原点 · 1920×1080」），而 `HudCanvas` 的单位**就是设计 px**（`HudCanvas.cs:8-13`）⇒ 逐值照搬。**唯一要换的是「矩形 → 节点」那一句**：`MenuDraw.ApplyPxRect`(`:208`) → `FillParent`/`SetPxSize`/`PosPx`；`MenuDraw.Text`(`:375`) → `MakeText`。 |
| **缺什么** | ① **命中**：现在那 2 颗（`ShadeHit`/`Absorb`）挂 `WindowButton`、**只有壳侧 `PointerLayer` 会派发**（`:326-328`），战斗里靠 `BattleDriver.HandleAlliancePanel` 用 `HitBody` 接 —— uGUI 档若走 `Image.raycastTarget = true`，要等 `T09` 第 4 步的 `EventSystem`（`HudCanvas.cs:33-35` 明写这轮不建）⇒ **两条路在这一点上一样，都要由 `BattleDriver` 接**。② **两份实现的风险**：旧件在 `Shell/`、新件在 `Battle/` ⇒ 要防「同一扇窗两份」（CLAUDE.md §三）；**好消息**：旧件今天**只有战斗在用**（§③ 现核）⇒ 理论上可以整体移走/删掉，不欠壳侧。③ 出场 `DOFade`/`DOScale` 三常量仍缺（旧件 `:64-66` 自陈）—— **与迁 uGUI 无关的老缺口**。④ 若走「就地改 `Shell/AlliancePanelWindow.cs`」（而不是新建一份），那它是 `Shell/` 下的**世界空间**件 ⇒ 与 `T09` §八·2 判据原文的禁令冲突（「⛔ 不能把它挂到战斗子画布下」）。 |
| 规模印象 | 416 行里：几何/文案常量 ≈ 150 行**可逐值照搬**；`Build()`(`:214-293`) ≈ 80 行**要逐句换口**；`Apply`/`Open`/`Close`/`HitBody`/`Txt`/`LocOr`/`Art`(`:295-402`) ≈ 110 行**基本不动**。 |
| 风险 | 相对小：落点有 **5 个完全同构的先例**（§6.1）；代价是**「每扇各写一份」的起点**。⚠️ 还有一条**共享风险**：迁上去之后它就在 `OverHand` 画布里，而 §6.2 那条「排序层 vs `renderQueue` 谁先比」**没实机验过** —— 但 `T09` §八 的迁移方向就是**按 13→1 从高到低**，`Alliance Panel`(13) **正好是这一组的第一格** ✓。 |

**「哪条看起来代价小 + 依据」（按要求给一句，⛔ 不替主对话拍板）**：
**路 B 看起来小** —— 依据三条：① **本扇窗今天只在战斗里用**（§③ 现核：全仓唯一生产调用点是 `BattleDriver.cs:15084`，壳侧 5 个命中全是注释）⇒ 壳侧 uGUI 化**不是**它的前置；② 路 B 的落点已有 **5 个同构先例**（`Mulligan` / `ChooseCardMenu` / `Card Display Window` / `Multi Card Display` / `AboveShader`，§6.1）且几何常量**现成、零换算**（§⑦ 路 B 行）；③ 路 A 要碰 **3309 处调用点 / 42 个派生窗 / 109 份 `Shell/*.cs`**，而其中**与本扇窗有关的只有这一扇**。
⚠️ **反向的那一格也如实给出**：路 A 做完之后，`OverHand` 组剩下的那几扇（`Unit Chat` / `ActiveSkillDesc` / `AttackSelector` / `Tutorial`）**都不用再各写一份**；路 B 是「逐扇各写一份」的起点。**这一权衡留给主对话。**

---

## ⑧ 没查清的（⛔ 不许猜；写清搜过哪些词 / 哪些文件）

1. **`Mulligan` / `ChooseCardMenu` / `Generic Multi Card Display Combat` 三块已迁画布上具体哪些子件压在 Alliance Panel 上 —— 没解出来。**
   - 已拿到的：三者的原版根矩形（`Mulligan` **0×0** · `ChooseCardMenu` **0×0** · `Generic Multi Card Display Combat` **0×818.040**，探针输出）。
   - 断在哪：自写 python 逐层解子件矩形时，在 arena2 的 `RectTransform/` 索引上**断链** —— `KeyError: '1518'`（`Card Display Window` 子树）与 `KeyError: '1341'`（`Mulligan` RT 3539 的第 1 个子件）⇒ 那些 `m_Children` 指向的 RT **不在本包的 `RectTransform/` 目录**（另在别的 CAB）。
   - 搜过：`python -I -X utf8 工具/_probe_node_rect.py bundle_scenes_scenes_battlearena2 <名> "Safe area FrontCanvas"` 逐名跑 5 次（`Alliance Panel` / `Card Display Window` / `Mulligan` / `ChooseCardMenu` / `Generic Multi Card Display Combat` / `Card Display` / `TipText`）· arena2 `GameObject/` + `RectTransform/` 全目录建索引 · 自写逐层递归解。
2. **「排序层 vs `renderQueue` 谁先比」没有实机验证** —— `T09` §八·5 自陈「没实机验过」，现行口径不许起 player ⇒ §6.2 那条「画布压在世界件之上」是**承重假设**。
3. **`AboveShader` / `TipText` 在我们这边今天建出来了没有** —— 我只核到它**有子画布**（`BattleDriver.cs:14430`）；`T09` §六·4 自陈「未建件与本次迁移的先后 —— 没查」。搜过：`grep -n "AboveShader" Battle/BattleDriver.cs` · `T09` §六。
4. **主菜单那一侧「看档案」是哪个窗、与原版 `Alliance Panel` 什么关系** —— 没查（超范围）。文件里存在的近名件：`Shell/PlayerProfileWindow.cs` · `Shell/AllianceMemberTab.cs` · `Shell/AllianceEventScorePanel.cs` · `Shell/AllianceMemberOptionsPopup.cs`（**只列名，未读正文**）。搜过：`grep -rn "AlliancePanelWindow" --include=*.cs .`。
5. **壳侧那 10 份 `阶段二_*` 规格文档与「哪些窗口会受路 A 影响」的对应关系** —— 没查（只落实了「它们是文档不是源码」这一条，§⑦ 路 A 行）。搜过：`find . -iname "*阶段二*"` · `grep -rln "阶段二" --include=*.md .`。
6. **arena2 里第二个 `Safe area FrontCanvas`（RT 2236）挂在谁底下** —— 没查（我全程用 3055，与 `T09c` 表一致）。搜过：arena2 `GameObject/` 全目录按 `m_PathID` 反查名字。
7. **`AlliancePanelWindow` 出场动画那三个 DOTween 常量** —— 旧件 `:64-66` 自陈「没读，要在 `GameAssembly.dll` 里取值」⇒ **仍然是账**（与迁 uGUI 无关）。

---

## ⑨ 🔴 主对话的裁定（2026-10-16 · **本节由主对话写，不是侦察代理的产出**）

### 9·1 裁定：**走【路 B】—— 给战斗侧一份 uGUI 的 `Alliance Panel`**

**依据（三条，全部来自本报告已核实的证据）**：
1. **本扇窗今天【只在战斗里用】**（§③ 现核：全仓唯一生产调用点是 `BattleDriver.cs:15084`；壳侧 5 处命中**全是注释**）⇒ **壳侧 uGUI 化不是它的前置**，「壳侧的窗」这个前提**不成立**（`T09` §八·2 那句据此**就地订正**，见下）。
2. **路 B 的落点有 5 个完全同构的先例**（§6.1）：`Mulligan Canvas` / `Generic Multi Card Display Combat Canvas` / `ChooseCardMenu Canvas` / `Card Display Window Canvas` / `AboveShader Canvas` —— 全是 `OverHand`/order 0 的子画布。
3. **几何常量现成、零换算**（§2.4 + §⑦ 路 B 行）：文件里的 20 个 `PxRect` **本来就是设计 px / 左上原点 1920×1080**（`:94` 自陈），而 `HudCanvas` 的单位就是设计 px ⇒ 逐值照搬，只换「矩形→节点」那一句。
4. **反方那一格也记着**：路 A 做完之后 `OverHand` 组剩下几扇都不用各写一份 —— 但代价是 **3309 处调用点 / 42 个派生窗 / 109 份 `Shell/*.cs`**，而其中**与本扇窗有关的只有这一扇**。⇒ **路 A 不作为本件的路**；它**不是**本线当前任何一件任务，⛔ **不要凭这一条自己开新账**。

### 9·2 🔴 与「一份实现」的关系（⛔ 铁律：不许两份）

- 判决 = **路 B 但【不是】新建第二份实现**：
  旧件 `Shell/AlliancePanelWindow.cs` 今天**只有战斗在用**（§③）⇒ 做法是**把它整体翻成 uGUI 档**（含「宿主由调用方给」的双模式：宿主为 null 时保留今天的世界档、逐位不变），
  ⛔ **不许**在 `Battle/` 下另写一份同名面板、把旧的留在原地 —— 那就是「同一扇窗两份」（`CLAUDE.md` §三）。
- 文件**留在原路径**（`Assets/CardPresentation/Shell/AlliancePanelWindow.cs`）：本工程只有一个程序集，目录只是分类，⛔ **别为迁层去搬文件**（搬文件要连 `.meta` 一起动，得不偿失）。

### 9·3 🔴 就地订正（铁律 5）：`T09` §八·2 那句「它是壳侧的窗」**说错了**

- **原文**：「…它是**壳侧的窗**：`Shell/AlliancePanelWindow.cs` 整套走壳的窗口系统…⛔ 不能把它挂到战斗子画布下（世界空间 quad 挂画布下 = 本仓明令禁止的半迁移态）。⇒ 要么让壳侧窗口系统也支持 uGUI、要么给战斗侧单独重建一份 —— 属跨线（界面线）的大活」
- **实际**：它**住在 `Shell/` 目录**、用**壳侧的 `MenuDraw` 助手**，但**类自己 `:78-80` 明写「它是『面板』不是『窗』」**（根组件是普通 `MonoBehaviour`、**不是 `GameWindow`**、**不注册进 `WindowsManager`**）· 而**壳的窗口系统今天根本没接管它**（壳侧零调用点）。
- **错因**：**把「文件所在目录 + 用了哪套助手」当成了「归哪个系统管」** —— 没去 `grep` 谁在调它。
- ⇒ **对迁移的结论不变**（它仍要迁进 `OverHand` 子画布），**变的只是走法**：**不需要动壳侧窗口系统**。

### 9·4 附带订正两条（同步、同一批核出来的）

1. 🔴 **`T09c` §一 那一行的两处行号已过期**（本报告 §③ 现核）：建点写 `BattleDriver.cs:15060` → **实为 `:15084`**（`:15060` 是**注释行**）；入口写 `HandleAlliancePanel:4049` → **实为 `:4051` 的方法声明行**（`:4048`/`:4049` 是 doc 注释）。⚠️ 这正是本线已知的坑「旧文档引 `BattleDriver.cs` 行号整体偏移」的又一例 ⇒ **引行号前一律现核**。
2. 🔴 **「8 份 `阶段二_*` 窗口在用它」这个说法不成立**（本报告 §⑦ 路 A 行 现核）：`资料/阶段二_*.md` 是 **10 份规格文档**、**不是窗口源码**；壳侧窗口源码是 `Shell/*.cs`（**109 份**）+ **42 个 `GameWindow` 派生类**。⇒ 今后引「路 A 的影响半径」用后两个数。

### 9·5 本裁定**没有**解决 / ⛔ 不是「不做」的（铁律 11）

- **`T09` §八·2 里「中间态影响」那半仍然成立**：迁之前，`Alliance Panel` 会被**已经迁上去的 5 块画布压住**。
  🔴 **本报告 §6.3 把它从「很窄」改成了「有据可查的可见」**：与 `Card Display Window` 的 `Card Display` 相交 **≈483×311 px**（两窗同开时**可见**）⇒ 这一件**优先度上升**（它是 `OverHand` 组里**兄弟序最高**的一格，按「13→1 从高到低」正是**第一件**）。
- **§⑧ 那 7 条「没查清的」原样挂着**（尤其第 2 条「排序层 vs `renderQueue` 谁先比」**没实机验过** —— 它是本组全部迁移的**承重假设**，统一验收时**必须专门看**）。
- **壳侧窗口系统 uGUI 化（路 A）**：**今天不是任何一件任务**，⛔ **别自己开新账**；若将来壳侧真需要 uGUI，再由用户/主对话决定。
