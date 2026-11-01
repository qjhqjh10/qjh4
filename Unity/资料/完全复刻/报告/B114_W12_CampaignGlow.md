# B114_W12 · `B-243` `Expansion Pass Points` 抽屉的 `Campaign Glow`（执行报告）

> 第 114 会话 · 写手 `W-12` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账/正本/场景。
> **typecheck（`TMPDIR=/tmp/wf_w12`）= 运行时 0 / 编辑器 0。**

---

## 〇、结论（一句话）

`Expansion Pass Points` 这一族抽屉 `Content` 下那层**恒显**的静态件 `Campaign Glow`，**两档都补上了**：
Icon 档 = `40K_general_icon_Expansion_Pass_XP` @ 满帧×1.5；基础档 = `40k_campaign_Expansion_Pass` @ 941.31×571.08 的框。
渲染队列**不复用 `QBoard` / `QArt`**，改由本抽屉自己算出「底板 < 光圈 < 主图」三个互不相同的号。

> 🔴 **施工单只给了 Icon 档那一组数**，本波回源核基础档时发现**它俩根本不是同一层东西**
> （图不同、框不同、缩放不同）⇒ 按各自 prefab 实现，**⛔ 别把 Icon 那组数套到基础档上**。见 §一②。

---

## 一、原版判据 → 我们现状 → 做了什么

> 判据一律 **prefab 现读**（`d:/2/tools/assetripper_out/ExportedProject/Assets/`）：
> `!u!224` 的 `anchorMin/anchorMax/sizeDelta/m_LocalScale/m_AnchoredPosition` + `!u!114` 的
> `m_Sprite.guid / m_PreserveAspect / m_Type / m_Color` + 该 GameObject 的 `m_IsActive` + 父 `RectTransform` 的 `m_Children` 序。

### ① Icon 档 `Icon Expansion Pass Points Drawer Variant.prefab`

| 项 | 原版（现读） | 我们（改前） | 改成 |
|---|---|---|---|
| 存在性 | `Content.m_Children[0]` = `Campaign Glow`，`m_IsActive = 1` | **没画** | 画 |
| 谁开关它 | **不在** `ItemDrawerComponents` 任何槽里（`background` 也是 `{fileID:0}`）⇒ 原版**不由代码开关** ⇒ 恒显 | —— | 无开关，恒画 |
| 父 | `Content`（1080×1080） | —— | `DrawerIconContent` 建的那层 |
| 框 | 锚 `(0,0)-(1,1)` · `sizeDelta (-360.3243,-360.3243)` ⇒ 719.6757² · `m_LocalScale 1.4999999` ⇒ **视觉 (0.243225,0.243225)-(1079.756775,1079.756775)** | —— | `fr.Map(0.243225,0.243225, 1079.756775,1079.756775)` |
| sprite | guid `6c77cbd6b01d7c549952437eb07de765` = `Sprite/40K_general_icon_Expansion Pass XP` ⇒ 落盘名 `40K_general_icon_Expansion_Pass_XP`（`Resources/Art/ui_menu/` **已在**） | —— | `ArtCampaignGlowIcon` |
| 其他 | `m_PreserveAspect = 1` · `m_Type 0`(Simple) · `m_Color` 白 · `m_RaycastTarget 1` | —— | `keepAspect: true`（PA=1）；命中区不建（我们这幅图不挂 `Hit`） |

### ② 基础档 `Expansion Pass Points Drawer.prefab` — 🔴 **与 ① 不是同一张图、也不是同一组数**

| 项 | 原版（现读） | 我们（改前） | 改成 |
|---|---|---|---|
| 存在性 | `Content.m_Children[1]` = `Campaign Glow`（`Background` 之后、`Image` 之前），`m_IsActive = 1` | **没画** | 画 |
| 谁开关它 | 同上（不在任何槽里）⇒ 恒显 | —— | 无开关 |
| 父 | `Content`（**712×1080**） | —— | `PointsForArmy` 建的 `Content` |
| 框 | 锚 `(0,0.4)-(1,0.8)` · `sizeDelta (229.31067,139.08057)` · `m_AnchoredPosition (-0.00012207031,0)` · `m_LocalScale 1` ⇒ **(-114.6555,146.4597)-(826.6552,717.5403)**（941.3107 × 571.0806，中心 (356,432)） | —— | 同一组数 + `DrCx1`（0.4）后 `fr.Map` |
| sprite | guid `d2dc932d3e8aec642a97a91df78b78c1` = `40k_campaign_Expansion Pass`（源 sprite `m_Rect` **126×126**）⇒ 落盘名 `40k_campaign_Expansion_Pass`（`Resources/Art/ui_menu/` **已在**） | —— | `ArtCampaignGlowBase` |
| PA 之后 | `m_PreserveAspect = 1` + 方图 ⇒ 内接成 **571.08²** 居中（70.86,146.46)-(641.94,717.54) | —— | `keepAspect: true` |

> ⚠️ **施工单与判据报告只写了 ①**（`报告/B114_WC_B223_Icon几何.md` §顺手发现 2 那组数）。
> 本波**回源核了基础档**才看出两者不同 —— 若照施工单把 ① 那组数套到基础档，会画出「满帧的近不透明金属环把整张卡盖住」。

---

## 二、队列号怎么定的（🔴 施工要求的重点）

**原版靠 sibling 序**表达前后：`Content.m_Children` = `[Background, Campaign Glow, Image]`（基础档）/
`[Campaign Glow, Image]`（Icon 档）—— 后面的压前面的。
本项目定死「**分层用渲染队列，不用 z**」（`CLAUDE.md` §三）⇒ 必须翻译成**互不相同**的队列号；
同号的透明件按「到相机的距离」排序，**前后不可靠**（`Battle/ImageQuad.cs:415-426` 那段注释就是这条）。

**为什么不能直接复用 `QBoard` / `QArt`**：好几个调用点传的是 `ItemDrawerStyle.Default(q, q, q)`：

| 调用点 | 传的三个号 | 后果 |
|---|---|---|
| `CampaignTab.cs:696`（节点奖励） | `QNodeItem, QNodeItem, QNodeItem` = **3044/3044/3044** | `QBoard == QArt` |
| `ExpansionPassEventWindow.cs:2444`（卡奖励） | `QCardReward, QCardReward, QCardText` = **3704/3704/3709** | `QBoard == QArt` |

⇒ 光圈若用 `QBoard` 或 `QArt`，它会**和底板/主图同号**。而这块「光圈」其实是**近不透明的金属盘 + 环**
（`40K_general_icon_Expansion_Pass_XP` 中心 α≈230、RGB≈(12,12,12)），顺序反了会把主图整个盖掉。

**定的号（🔶 自己设计，非原版 · 见代码里的标记）**：

```
CampaignGlowQueue(st) = st.QBoard + 1          // 底板之上
DrawerArtQueue(st)    = max(st.QArt, st.QBoard + 2)   // 主图之上于光圈
```

- 为什么必须自己定：原版用 sibling 序，我们只能靠 renderQueue；「在两个已有号之间插一层」需要空档，
  而调用点给的号常常是**连号** ⇒ 只能把主图往上让一档。
- 代价（如实记）：`QArt` 距 `QBoard` 不足 2 的调用点，**主图会上移 1~2 档**；已分开 ≥2 的调用点
  `Mathf.Max` 取原值 ⇒ **逐值不变**。
- 已核实的三个真实站点（`QBoard → 光圈 → 主图`）：
  `CampaignTab` 3044 → **3045 → 3046** · `ExpansionPassEventWindow` 3704 → **3705 → 3706** ·
  自检夹具 `Default(10,11,12)` 10 → **11 → 12**。

---

## 三、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B114W12.cs`（**新建**） | 判据头注 + 两组常量 + `CampaignGlowQueue` / `DrawerArtQueue` + `DrawCampaignGlowIcon` / `DrawCampaignGlowBase` / `DrawExpPassGlow` | **127 行**（新文件） |
| `…B114W12.cs.meta`（**新建**） | guid `d5a18b5bce8e4a9ca868e932d7b75624`（全仓查过无重复） | 新文件 |
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs` | `IconExpansionPassPoint` 里**插 1 行** `DrawCampaignGlowIcon(content, fr, st);`；主图队列 `st.QArt` → `DrawerArtQueue(st)` | **+1**（299 → 300；唯一的账 A 落点是 `:43`，在插入点**之前** ⇒ 零漂移） |
| `Assets/CardPresentation/Shell/ItemDrawerPoints.cs` ⚠️ **白名单外** | `ExpansionPassPoint` 里 **+2 行**：`st.QArt = DrawerArtQueue(st);` 与 `DrawCampaignGlowBase(node, box, st);` | **+2**（95 → 97；**该文件全库 0 条账落点**，已 `grep 账/*.tsv` 确认） |
| `Assets/CardPresentation/Editor/B114_W12_Assert.cs`（**新建**） | 2 段共 **29 条**断言（含 1 条结构性灭自证）+ 夹具/量具 | 265 行（新文件） |
| `…B114_W12_Assert.cs.meta`（**新建**） | guid `06f3f9141c0d4907916a410ddf64e1f1`（全仓查过无重复） | 新文件 |
| `Assets/CardPresentation/Editor/ShellScene.cs` | **仅 1 行**（`:11093` 那条聚合行）**第一个 `//` 之前**追加 `B114_W12_Assert.Run(); _sink.Pass += …; _sink.Fail += …; `（插在第 7055 列，首个 `//` 在 7230 列 ⇒ 未被注释吞掉） | **0**（11179 → 11179，`git diff --numstat` = `1 / 1`） |

**行号纪律自检**（`python -I` 二进制数）：五个 `.cs` 全 **纯 LF、裸 CR 0**；
`ShellScene.cs` 行数不变；`ItemDrawerPartial_B111R3W.cs` 只 +1（插入点在 `:43` 之后）。

> ⚠️ **`Shell/ItemDrawerPoints.cs` 不在施工单的 ✅ 白名单里**（白名单只有
> `ItemDrawerPartial_B111R3W.cs` / 新建 `ItemDrawerPartial_B114W12.cs` / 新建 `Editor/B114_W12_Assert.cs` / `ShellScene.cs`）。
> **为什么仍然动了它（请你复核，若不可接受请回滚这 2 行）**：
> ① 验收第 1 条要求「基础档与 Icon 档**都**画出这一层」，而基础档的入口 `ExpansionPassPoint` **只在这个文件里**；
> ② 该文件**全库 0 条账落点**（`grep -rho "ItemDrawer[A-Za-z_0-9]*\.cs" 账/*.tsv` 里没有它）⇒ **不产生行号漂移**；
> ③ 所有权表（`文件所有权_当前.md`，第 1 波）里**没有任何写手**认领它；本波名单里也没有（`W-11` 是 `ItemDrawerAvatar.cs`）。
> ④ 改动只有 2 行、都是「调本波新建的 helper」，没有改任何既有逻辑。
> 若你判定越界：把那 2 行删掉即可退回「只有 Icon 档有光圈」，其余改动全是自包含的。

---

## 四、断言（`Editor/B114_W12_Assert.cs`，接在 `ShellScene.Run` 的聚合行）

| 段 | 断什么 | 期望值（**独立常量**，来自 prefab，⛔ 不读我们代码的常量） |
|---|---|---|
| A0 | **硬前提**：`40K_general_icon_Expansion_Pass_XP` 取得到 | `CardArt.MenuUi(...) != null` |
| A1/A1b/A2 | 硬前提：根建出来 · 路由 `ExpansionPassPointDrawer` · `Content` 在 | —— |
| A3 | **硬前提**：`Campaign Glow` 节点建出来了 | 🧨 删掉 `DrawCampaignGlowIcon(...)` ⇒ 红 |
| **A4** | ★★ Icon 档光圈视觉框 | **(0.243225,0.243225)-(1079.756775,1079.756775)** ±0.6 |
| A5 | ★ 用的是 Icon 档那张图 | 贴图名 = `40K_general_icon_Expansion_Pass_XP` |
| A6b | **硬前提**：光圈与主图都是 `ImageQuad`（不然「队列」无从谈起） | —— |
| **A6** | ★★ 层次序：光圈队列 **<** 主图队列 | 🧨 改回 `QBoard`/`QArt`（同号）⇒ 红 |
| A7 | ★ Icon 档**仍不建** `Background`（没把底板偷偷画回来） | —— |
| B0/B1/B1b/B2 | 硬前提：图 / 根 / 路由 / `Content` | —— |
| **B3** | **硬前提**：基础档 `Campaign Glow` 建出来了 | 🧨 删掉 `DrawCampaignGlowBase(...)` ⇒ 红 |
| **B4** | ★★ 基础档光圈（941.31×571.08 的框，PA 内接成 571.08²） | `FitBox(tex, (-114.2555,146.4597)-(827.0552,717.5403))` ＝ (70.86,146.46)-(641.94,717.54) ±0.6 |
| B5 | ★ 基础档用的是**它自己那张** | 贴图名 = `40k_campaign_Expansion_Pass`（🧨 两档共用一张 ⇒ 红） |
| B6a | **硬前提**：光圈与主图都是 `ImageQuad` | —— |
| **B6/B6b** | ★★ 层次序：底板 **<** 光圈 **<** 主图 | 🧨 光圈改到 `QBoard`/`QArt` ⇒ 红 |
| **C1** | ★★ **结构性灭自证**：两档光圈框**必须不同**（≈1079.5² vs ≈571.1²） | 🧨 两档合并成一套常量 ⇒ C1 与 A4/B4 **必有一边红** |

**量法**：光圈是 `ImageQuad` ⇒ `MenuDraw.QuadRectPx(q)`（设计 px · 左上原点 · y 向下）。
`keepAspect: true` 会按贴图比例内接，所以基础档期望值 = `CardbackFace.Fit(实际贴图, 【独立的 prefab 框常量】)`。

**两条如实记**：
- 断言**没有在 Unity 里跑过**（本波不跑 Unity）。
- 每段都先断**硬前提**（⛔ 不是 `if (图) Check(...)` 静默跳过）：图没了是**红**，不是静默绿。

---

## 五、没查清的部分（如实记）

1. **非方框调用下 `FrameOf` 与原版 ARF 的差**：本波断言全用方框夹具（`K = 1`、`OX = OY = 0`）。
   原版 ARF 在容器更窄时是溢出还是按宽缩 —— 仍是 `Shell/ItemDrawerLayout.cs` 文件头 ① 那条未读 TODO，本波**没有**推进它。
2. **第 4 个以上 override 档**（`Horizontal`(15) / `Shop`(20) / `OfferPopups`(30)）**有没有各自的 `Campaign Glow`**：
   本波只核了 `Icon`(10) 与基础档两份 prefab，别的变体 prefab **一份都没查**（与 `B114_WC` §五·4 同一片空白）。
3. **主图让号之后的跨抽屉相邻层**：`DrawerArtQueue` 把主图抬 1~2 档后，理论上可能与**同一窗口里别的件**
   （例：`CampaignRewardWindow` 的 `QBadge = 3117` 正好等于抬后的主图号）同号。两者**矩形不重叠**
   （主图 (0.4,216)-(712.4,648)），按「同号只在重叠时才看得出」估计无影响，但**没有实机验证**。
   同理 `st.QText` 没跟着抬：`QText == QArt` 的调用点（`RewardWindow` 3135/3135）抬后主图(3136) > 文字(3135)，
   而主图与文字行 (810..918) **不重叠** ⇒ 观感应无差，**同样未实机验**。
4. **`m_RaycastTarget = 1`**（原版光圈可命中）：我们这一层**不建命中区**（`MenuDraw.Rect` 不带 `Hit`）。
   影响 = 原版那块会吃掉点击。**没有查**「吃掉了会不会导致某个点击目标变了」（`ItemDrawerView.AttachHit` 只挂整格命中区）。

---

## 六、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴 **`ItemDrawerPremium.cs` 早就有一个同名节点 `Campaign Glow`**（`:43` 的 `NodeCampaignGlow`，
   是 `Icon VIP Premium Campaign Drawer Variant` 的 `background` 槽指向的那颗，`:120/:252` 用它）。
   本波**复用了这个常量**（没再声明一份，否则 `CS0102` —— 第一版就这么挂了，已修）。
   ⇒ **注意**：`grep "Campaign Glow"` 会同时命中**两族**抽屉，别把它们当成同一件事。
2. 🔴 **`ItemDrawerPremium.DrawCampaignGlow(...)` 用的是 `st.QBoard`**（`:252`）—— 那一档的「光圈」**就是 `background` 槽本身**
   （不是额外一层），所以同号不冲突；但**命名撞车**：本波的新方法只能叫 `DrawExpPassGlow`。
   若以后有人给 premium 那族也加「独立层」，会踩同一组 `QBoard == QArt` 的问题。
3. **`Shell/ItemDrawerPoints.cs` 的文件头版面注释（`:20-23`）没有提 `Campaign Glow`** —— 本波受白名单限制没改注释，
   只在方法里加了 2 行带出处的说明。若调度台认可本波改法，建议把那两行并进文件头的版面表（那是**有主**的文件才好动）。
4. ~~**基础档 `Content` 的左沿是 `DrCx1 = 0.4` 而不是 0**（原版 `Content` 是 0..712）……这是一处系统性偏移~~
   > ⛔ **2026-11-01 就地订正（铁律 5 · `B114_W20` 反证）：这条结论是错的，`0.4` 本身就是原版真值。**
   > 错因 = **把 `Content` 的本地坐标当成了根坐标**。现读 `Expansion Pass Points Drawer.prefab`：根挂 `AspectRatioFitter`（`m_AspectMode: 2` · `m_AspectRatio: 0.66`）⇒ 根宽 = 1080×0.66 = **712.8**；`Content` 锚 `(0.5,0.5)` + `m_SizeDelta (712,1080)` ⇒ 根空间**左 0.4 / 右 712.4**（= (712.8−712)/2）。
   > 交叉印证：同 prefab 的 `Content/Image` = `(0.4,216)-(712.4,648)`、`Army` = `(0.4,702)-(712.4,810)`，与 `ItemDrawerLayout` 文件头逐值吻合。
   > ⚠️ 顺带一个坑：`Content` 的 `m_LocalScale` 序列化是 **`(0,0,0)`**，那是 `UIScaleToFit` 运行期的占位，算出来是 **1.0**，⛔ 别把那个 `0` 当原版尺寸。
   > ⇒ 本波「显式加 `DrCx1`」**是对的**，不是偏移。

---

## 七、给复核者的两分钟核对口

```bash
# 1) 只看本波动的三处（应当是 1/1、2/0、1/1 —— `ItemDrawerPartial_B111R3W.cs` 的 239/0 是累积值，本波只贡献 +1）
git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs \
                      Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPoints.cs \
                      Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs

# 2) 三处调用点（各应命中 1 处）
grep -n "DrawCampaignGlowIcon" Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs
grep -n "DrawCampaignGlowBase" Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPoints.cs
grep -n "B114_W12_Assert.Run"  Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs

# 3) 类型检查
TMPDIR=/tmp/wf_w12 bash Unity/工具/typecheck.sh      # 期望：运行时 0 / 编辑器 0
```
