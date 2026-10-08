# W_AlliancePanel — A985③「`AlliancePanel` 整扇窗要建」（写手交件 · 2026-10-18）

## 原版那扇窗：节点树 / rect / 文字 / 显隐条件（逐层给字段出处）

**包**：`d:/2/新解包资源/assets_full/bundle_scenes_scenes_battlearena1/`（13 个战场各一份；**arena1 vs arena2
逐字段比过**：根框五元组 / 子件 rect / 贴图 pid **逐位相同** ⇒ 13 场共用同一份 prefab）。

**根**：`GameObject/Alliance Panel.json`（GO 1130，`m_IsActive = 0`）+ `RectTransform_2859.json`
`anchorMin=anchorMax=(0,1)` · `pivot=(0.13, 0.825)` · `ap=(-1.044435, -25.271729)` ·
`sizeDelta=(815.044006, 475.469971)` · `localScale=0.9999718070030212`。
父链 `Canvas(RT 2759) → FrontCanvas(2900) → Safe area FrontCanvas(2862)`（两条 stretch、零偏移）
⇒ **绝对 x[-107.00, 708.02] y[-57.93, 417.52]**（顶出屏 57.9 px）。⚠️ 父链三级 `sizeDelta` 都是 0（stretch
到 1920×1080）；`Canvas` 自己 `localScale=0` 是**序列化残留**（ScreenSpaceOverlay 由引擎驱动）⇒ 按 1 解。
**独立旁证**：`资料/说明书/01_战斗_对战/2D层_battlearena1全树.md:756-779` 那棵树给的是 `[-107,1022 815x475]`
—— 它的 x = 屏幕左缘、y = 屏幕顶 + 1080（我按 4 个节点反推出来的换算），**与本件算出的值逐位吻合**。

**12 个字段**（`MonoBehaviour_4117.json`，顺序 = 偏移序）：`0x20 avatarDisplay` · `0x28 playerName` ·
`0x30 playerTitle` · `0x38 playerTitleHolder` · `0x40 playerScore` · `0x48 allianceGO` ·
`0x50 playerAllianceName` · `0x58 allianceRatingDisplay` · `0x60 backgroundCloseButton` ·
`0x68 canvasGroup` · `0x70 allianceBadgeDrawer` · `0x78 notInAnAllianceMessage`。

**显隐条件**（`BattleAlliancePanel__Open.c` 逐句；`BattleAlliancePanel__Awake.c` = `SetActive(false)` +
给 `backgroundCloseButton` 挂 `CloseButtonClick`；`…__CloseButtonClick.c` = `SetActive(false)`）：

| 件 | 条件 | 赋值 |
| --- | --- | --- |
| `playerTitleHolder`(0x38) | **称号非空 ⇒ 开** | — |
| `allianceGO`(0x48) | **联盟名非空 ⇒ 开** | — |
| `notInAnAllianceMessage`(0x78) | **联盟名为空 ⇒ 开** | 文案 = `GetTranslation("Battle/AlliancePanel/NotInAnAlliance")` |
| `playerAllianceName`(0x50) | — | = 联盟名 |
| `playerName`(0x28) / `playerTitle`(0x30) | — | = 名 / 称号 |
| `avatarDisplay`(0x20) / `playerScore`(0x40)·(0x58) / `allianceBadgeDrawer`(0x70) | — | `Initialize(...)` / `Draw(...)`（运行期灌） |

**入口**：`BattleHud__AlliancePanelOpenButton.c` → 取 `BattleManager.matchData` 的
`+0x10`名 · `+0x50`称号 · `+0x38`评级 · `+0x40`/`+0x48`头像 id · `+0x68`**联盟名** · `+0x7c`联盟评级 ·
`+0x80`徽章 id → `Open(...)`（**分支判的是联盟名**）。出场 = `alpha=0 → DOFade` + 「先缩 `×DAT_1834b2e84`
再 `DOScale` 弹回」。

**逐件实绘 rect**（画布 px · 左上原点 1920×1080；**已乘 `localScale`**）：熄灭层
`x[-5181.741,6486.258] y[-3867.769,4144.042]`（11668×8012，纯黑 α0.4627）· `Background`
`x[-97.327,625.312] y[9.970,413.089]` · `BG`（图 `40K_shop_offer_bg_Sororitas_0`，染 (0.349,0.349,0.349)）
`x[14.158,571.457] y[55.246,356.583]` · `BGFrame` `x[-92.217,13.557→624.645,412.646]` + 四块
（Left `-92.673,25.022→24.645,411.994` · Right `541.475,13.985→625.123,385.038` ·
Top `23.750,24.840→541.422,58.762` · Bottom `24.487,348.664→542.159,385.195`）· 头像框 `Border`
`x[-11.283,201.246] y[60.573,209.590]`（PA=1，图 `Player Profile Border` 256×286）· `Alliance` 组
`x[23.094,574.688] y[220.581,356.530]` · `NotInaAllianceText` `x[55.765,529.877] y[257.013,293.972]`
（α 0.7725，**原文 `This player is is still not part of an Alliance`（双 `is` = 原版笔误，勿改）**）。

**字**（字号 = 原版 `m_fontSize` × 该节点累计 `localScale`；全部 `H=1`(Left)）：
`Name:` 28.00(V=Capline) · 名字 32.00(Capline) · `Title:` 28.00(Capline) · 称号 28.00(Midline) ·
`Alliance:` 26.72(Capline) · 联盟名 28.00(Capline) · 无联盟 26.32(Midline)。

## 我们建了什么：文件 + 类 + 骨架照谁抄的 · 接线点

- **新建** `/d:/4/Unity/MyGame/Assets/CardPresentation/Shell/AlliancePanelWindow.cs`（369 行，LF）
  +\ 同名 `.cs.meta`（guid `e9ac0c9d94e44e0db5444f8f11dab7c7`，全库唯一）—— 类 `AlliancePanelWindow
  : MonoBehaviour`（**不是 `GameWindow`**）。
- **骨架照抄 `Shell/AllianceEventScorePanel.cs`**（同为「原版是 `MonoBehaviour` 面板、不是窗」那一族，
  且**同在本目录**）：`Create(Transform parent, …)` + `MenuDraw.ApplyPxRect` 摆根框 +
  `MenuDraw.{Node,Rect,Nine,Text,Hit,Absorb,ShadeHit}` 逐件建 + 每件显式写渲染队列。
  **没有**用 `MenuWindowBase` —— 原版它就不是 `WindowsManager` 的窗（没有 `type`/`placement`/`closeOnESC`
  可填），套那一套等于**给它编一个窗身份**。
- API：`Create(parent)` · `Open(playerName, title, allianceName)` · `Close()` · `Visible` ·
  `Root` / `NameLabel` / `TitleHolderNode` / `AllianceGroupNode` / `NotInAllianceNode` / `MissingArt`。
- 🔴 **接线点 = `Battle/BattleDriver.cs`（本笔白名单里没有 `Battle/**` ⇒ 没接）**，要做三件：
  ① HUD 补 `AlliancePanelOpenButton`（**它的 rect / 贴图判据本笔没查** —— 不在
  `2D层_battlearena1全树.md` 里，要另起一笔读 `BattleHud` 那棵树）；
  ② `_alliancePanel = AlliancePanelWindow.Create(hudRoot);`
  ③ 那颗钮 onClick ⇒ `Open(FoePlateText, "", "")`。

## 单机 bot 恒 `NotInAnAlliance` 那一档怎么显示的（如实）

`Open(name, "", "")` ⇒ `TitleHolder` 关（原版判据就是「称号空 ⇒ 关」）· `Alliance` 组关 ·
`NotInaAllianceText` **开** —— **这正是原版单机情境的形态**（`(inactive)` 只是出厂默认）。
⛔ **没有给 bot 编联盟名/称号/评级**（本工程红线：不静默失败、不说谎）。
名字那格传什么由调用方定；**单机时 `BattleDriver._enemyText` 是空串**（`NetMatchmaking.FoeName` 空 ⇒
「单机局留空不编」，`BattleDriver.cs:12390`）⇒ 建议传名牌上那一串（= 阵营名）或是空串，**接线那一批定**。
**两处评级不建**（`Main Icon`/`Individual rating value` 序列化 0×0，尺寸由 `RatingDisplay` 脚本运行时算，
节点上是 `HorizontalLayoutGroup` `childControlWidth/Height=1` ⇒ **静态取不到**；我们也没有对手评级数据）。

## 分层：RenderQueue 怎么排的 · 谁会遍历 `_layers`（grep 结果）

- 队列：`QShade=3980`（熄灭层）· `QBg=4000` · `QFrame=4010` · `QAvatar=4020` · `QText=4030`；
  `QContentMin=QBg` 交给 `MenuDraw.ShadeHit`/`Absorb`，由它们自己校验「压暗/吸收档严格低于内容档」。
  **为什么必须用队列**：全部件在透明队列 3000，而熄灭层中心在 x≈+650 px（**离相机比面板本体近**）
  ⇒ 靠 z 排会把熄灯层画到面板上面。
- **`grep -rn "_layers" --include=*.cs .` ⇒ 19 处、全部在 `Core/CardView.cs`**（那是**卡面**的「整卡着色」
  名单：`ApplyTint`/`SetAlpha`/刷新都会遍历它）；**`Shell/**` 0 命中、与本件无交集**
  ⇒ 铁律 10④ 那条「这一层的颜色是它自己说了算还是别人说了算」：**本件每一层都是自己说了算**，
  没有会被外部整卡着色刷掉的层（本件也根本不建 `CardView`）。
  唯一「别人给色」的是 `BG`：原版 `m_Color=(0.349,0.349,0.349,1)` 是**染色**，我们照抄成 `tint`。

## 断言：**没有**（需要哪个宿主）

同族（战斗侧面板）的断言全落在 **`Editor/BattleScene.cs`**（`Editor/BattleScene.Run` 是唯一会建
战斗 HUD 的宿主）—— **它在白名单黑名单里**。⇒ 本批只建窗。
**给转派时要写进简报的**：能分两态（`Open(n,"","")` ⇒ `NotInAllianceNode.activeSelf=true` +
`AllianceGroupNode=false`；`Open(n,"t","a")` ⇒ 反过来）+ 🧨 改坏法（把 `Apply` 里那条
`if (gameObject.activeInHierarchy)` 前置对齐删掉 ⇒ 名字左缘不再是 276.220）+ **灭自证**
（断「名字块左缘 == `NameTextR.x1`」而不是断「`AlignLeft` 被调过」；再断一条结构上不可能同时满足的：
`NotInaAllianceNode.activeSelf != AllianceGroupNode.activeSelf`）。

## 类型检查 · 行尾 · 没查清（含「查过哪些包、哪些词」）

- **类型检查**：`TMPDIR=/tmp/wf_ap bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0 ✅**
  （改完即跑，本笔只动 1 个新建 `.cs`）。**没跑 Unity**（白名单禁）。
- **行尾**：新文件 **LF**（先量过同目录 `Shell/AllianceEventScorePanel.cs` = 0 CRLF / 266 LF）✓；
  `.cs.meta` 也 LF。
- **查过哪些包 / 哪些词**：`bundle_scenes_scenes_battlearena1`（`GameObject/` · `RectTransform/` ·
  `MonoBehaviour/` 三种类型的**逐文件索引** + 父链递归）、`bundle_scenes_scenes_battlearena2`（同构核对）、
  `bundle_boosterpacks_assets_all/Sprite/`（图名解析）；词 = `Alliance Panel` / `Alliance` /
  `NotInAnAlliance` / `AlliancePanelOpenButton`（**0 命中**）/ `is still not part of`；
  反编译 = `d:/2/tools/decomp_full/BattleAlliancePanel__{Awake,Open,CloseButtonClick}.c` +
  `BattleHud__AlliancePanelOpenButton.c`；本仓 = `Assets/` 全量 grep `AlliancePanel`。
- **没查清（4 条，都不猜）**：
  ① `PlayerInfo` 只判出「有 `VerticalLayoutGroup`」—— 三个 holder 的**真位置**是按
     `UguiLayout.VerticalChild`（spacing 0 · UpperLeft · `childControlWidth=1`/`Height=0`）**推**的，
     **没有运行期读数** ⇒ 若真机上是别的排法，`Name:`/名字那一行会整体偏（x 不受影响）。
  ② **`Asar`/`Pragati` 那一列**：报告 §五 给 7 颗标了 `Asar`，但我现读 **7 颗的 `m_fontAsset` 与
     `m_sharedMaterial` 完全相同**（只有 `fontPreset` 不同）⇒ **我没能看到「哪颗用哪一份度量」的
     直接字段**。本件按 `MenuDraw.SetVAlign` 的**默认 `Pragati`** 建（= 差 ~2–3 px 的垂直墨心）。
  ③ `Close Background` 的贴图（`40K_shop_offer_bg_*`）**染色纯黑** ⇒ 我用**实底**画（等价，标注成「我们的做法」）。
  ④ `40K_shop_offer_bg_Sororitas_0`（`BG` 那张图）**本仓没有这张 PNG**（`Resources/Art/**` 里
     `*shop_offer_bg*` 0 命中；只在 `d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/` 与
     `assets_full/bundle_boosterpacks_assets_all/`）⇒ 跑起来**面板心是空的** + 一次 `Debug.LogWarning`。
     **要补进素材腿**（`工具/**`、`Resources/Art/**` 都不在本笔白名单）。
- **HUD 那颗钮**：本仓 `BattleDriver` 的 `HudButtonWorldPosForTest` 键表只有
  settings/cemetery/chat/offensive/cameraReset **五颗** ⇒ 联盟那颗**也还没有**（见「接线点」）。
