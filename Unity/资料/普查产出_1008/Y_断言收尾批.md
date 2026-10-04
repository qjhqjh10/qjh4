# Y · 断言收尾批 —— A188 的 8 条旧断言 · A221② · W-C3 受限项 · A213 余下 17 处 · CollectionScene 3 红

> 写手报告（2026-10-08）。**没跑 Unity**（只有主对话能跑）· **没动 git** · **没改两张正本**。
> 全部改动走 **Edit 工具**（⛔ 全程没有 `sed -i`）。
> ✅ **跑了秒级类型检查**：`TMPDIR=/tmp/wf_y bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**
> （跑了**两遍**：全部 `.cs` 改完一遍、最后一处注释改完再一遍；用的是**独立 `TMPDIR`**，不与别的写手互盖）。
> ⚠️ 简报说「改完不用跑类型检查」—— 我跑了（`CLAUDE.md` §12 把它列为攒批规矩下**唯一不能省**的一步），**只为挡语法错**；
> ✅ **行尾**：8 个被改文件**逐个用 `rb` 读数过**（`b.count(b'\r\n')` vs `b.count(b'\n')`）⇒ **全是纯 LF、零翻转**；
> `git diff --numstat` 数字都远小于文件行数（没有整篇重写）。
> ✅ **白名单**：只动了 `Editor/{RewardsScene,ShellScene,MainMenuScene,CollectionScene,ShopScene}.cs` ·
> `Shell/{FriendsTab,AllianceMemberTab,CollectionWindow}.cs` · 本文件。**一行都没越界**。

---

## 〇 · 一句话总账

五个小组**全部落地**。(5) 那三条红的裁定：**两条是【实现】的层级错**（`Button Text` 挂在 `Close Button` 的兄弟位置 ⇒
按原版路径取不到），**一条是【旧断言】的期望值错**（拿「布局矩形」当「渲染矩形」量）——
**没有一处是「改断言迁就实现」或「改实现迁就断言」**，两条都有原版判据（见 §五）。

| 组 | 结论 | 改了几个文件 |
|---|---|---|
| (1) A188 的 8 条旧断言 | ✅ 落地（**8 条改期望值 + 3 条新加的反证**） | 2 |
| (2) A221② 奖杯详情弹窗 | ✅ 补上那 4 行 | 1 |
| (3) W-C3 受限项（页签渲染 + `WebTextR`） | ✅ 落地（**四窗全补**，简报只点了三个宿主） | 4 |
| (4) A213 余下 17 处 | ✅ 落地（**17 处逐条核过、7 处改**） | 2 |
| (5) `CollectionScene` 3 条红 | ✅ 3 条各有根因与判据、都已修 | 2 |

---

## 一、(1) A188 —— 那 8 条钉着旧模型的断言

**判据来源**：`资料/普查产出_1008/A188_修hitPad缩法.md`（§〇 那张表 + §六 ①② 的**建议改法**）。

### 1·1 结论

`MenuDraw.Hit` 现在把 pad 缩在 **`clip`**（原版 `RectMask2D` 自己那个框）上 ⇒ 判据 = **`R ∩ (V − pad)`**。
下面 **8 条**（`RewardsScene` 1 + `ShellScene` 7）原本钉的是旧模型 `(R − pad) ∩ V` ⇒ 期望值全部按**原版判据重算**。
⚠️ **`ShellScene` 那两幕没有「只改期望值」**：报告 §六·② 明写「**把 `clip` 收进 `r` 里面才能让 pad 真生效，别只删**」
—— 照它做了（否则旧模型与「pad 压根没接」在新剪裁框下**给同一个数**，断言会重新变成分不出两态的空断言）。

### 1·2 改动清单（文件:行 + 为什么）

| # | 文件:行 | 改前 → 改后 | 为什么 |
|---|---|---|---|
| 1 | `Editor/RewardsScene.cs:2485` | `CheckNear(qx1, trackR.x1, …)` → **`trackR.x1 + 10f`**（330.968 → **340.968**） | 旧模型给 `max(R.x1+10, V.x1)`；原版给 `max(R.x1, V.x1+padL)`（`RectMask2D.cs:178-184` ∧ `GraphicRaycaster.cs:327`） |
| 2 | `Editor/ShellScene.cs:837` | 端到端探针命中 quad 宽 **85 → 90** | `MenuDraw.Hit(…, r=(0,0,100,100), clip=(0,0,200,200), maskPad=(10,20,5,4))`：新判据的 `V − pad` = `(10,4,195,180)`，与 `r` 求交 ⇒ `(10,4,100,100)` ⇒ 宽 90 / 高 96 |
| 3 | `Editor/ShellScene.cs:840` | 同上 高 **76 → 96** | 同上 |
| 4 | `Editor/ShellScene.cs:1742`（新增 `clipPad2`） | ② 那一幕的 `clip` 从 `clipBig` 换成 **`(110,150,350,450)`** | 这**就是把 `clip` 收进 `r` 里面**：`V − pad` 左沿 = 120、不喂 pad 则 110 ⇒ 两态可分辨 |
| 5 | `Editor/ShellScene.cs:1746` | ② 左沿 **110 → 120** | 同上（这是**新**期望值，不是「旧值挪一挪」） |
| 6 | `Editor/ShellScene.cs:1756`（新增 `clipPad3`） | ③ 那一幕的 `clip` 从 `clipBig` 换成 **`(108,205,292,395)`** | 负 pad（`(−8,−5,−8,−5)`）扩大后 `V − pad` **正好 = `r`** ⇒ 命中 = `r` 本身；不喂 pad 则 = `clip` 自己 |
| 7 | `Editor/ShellScene.cs:1760-1763` | ③ 四条 **92/195/308/405 → 100/200/300/400** | 同上 |
| 8 | `Editor/ShellScene.cs:1771` | ④ 的文案改准（**值不变**，仍是 100） | ②③ 现在各喂自己的 `clip` ⇒ ④ 不再与它们同场景，原句「两态合起来…」会指向空处 |
| 9 | 🆕 `Editor/ShellScene.cs:1775-1778` | **新增 ④b**：与 ② **同一个 `clip`**、pad 清 0 ⇒ 左沿 **110** | ② 的 120 与它的 110 才构成**真两态**（缺了它，② 与「pad 被丢掉」同值） |
| 10 | 🆕 `Editor/ShellScene.cs:1780-1784` | **新增 ④c**：与 ③ 同一个 `clip`、pad 清 0 ⇒ 左沿 **108** / 下沿 **395** | 同上（证明负 pad **真的在扩大**） |

**没有改的**：`Editor/ShellScene.cs:809-821` 那三条纯函数断言（`PaddedHitRect` 本体语义未变）；
`:857-892` 渲染侧那一段（A188 报告 §四 已核「不受影响」）。
⚠️ A188 报告 §六·② 顺带建议的「把 `:809-821` 的文案从『命中区左边收进 10px』改成『按 `m_RaycastPadding` 缩一个矩形』」
**没做** —— 那三条的**文案**严格说仍是对的（它们断的的确就是 `PaddedHitRect`），改文案是措辞优化、不是判据，本批不动。

### 1·3 改坏法（逐条）

| 改坏法 | 会红在哪 |
|---|---|
| `MenuDraw.Hit` 改回 `ClipRect(PaddedHitRect(r, maskPad), clip, …)`（旧模型） | #1（差 10px）· #2/#3（宽 90→85 / 高 96→76）· #5（120→110）· #7（100→92 等四条） ⇒ **8 条全红** |
| `Hit` 的 pad 整个丢掉（`ClipRect(r, clip, …)`） | #5 → **110** · #7 → **108/205/292/395** ⇒ ②③ 那 5 条红；而 ④b 给 **110**（与 ② 同值）⇒ **② 与 ④b 一起红**（这正是 ④b 存在的理由） |
| 把 `clipPad2` / `clipPad3` 删掉（回到 `clipBig`） | **编不过**（`q3b`/`q3c` 还引着它们）⇒ 出声，不是静默 —— ⚠️ 真要退回得连 ④b/④c 一起删，那两幕的**反证能力**就没了 |

### 1·4 没查清的部分

- **一次都没跑**（归同步点）。上面所有数都是**静态算出来的**（几何 + uGUI 源码），不是量出来的。
- ④b/④c 是**我自己加的反证**（A188 报告只描述了「不喂 pad 则是 110 / `clip` 本身」，没写成断言）
  —— 期望值取自 `clip` 的**字面量**（110/108/395），不是我们任何常量 ⇒ 不构成自证。若调度台认为超范围，删掉不影响其余各条。

---

## 二、(2) A221② —— `TrophyInfoPopup` 补 `CheckShadeRule`

### 2·1 结论

✅ 补上。`MenuDraw.ShadeHit(` 的**生产站点 21 处**（`Shell/*.cs`，逐窗一处；另有 1 处在 `Editor/ShellScene.cs` 是自检探针）
vs `MenuDraw.CheckShadeRule(` 的调用 **20 → 21 处**，差额正是这一扇 —— 现在**逐窗都有**了。

### 2·2 改动清单

| 文件:行 | 改动 | 为什么 |
|---|---|---|
| `Editor/MainMenuScene.cs:5447-5462`（新增：12 行注释 + 那个调用） | `MenuDraw.CheckShadeRule(CheckTrue, "奖杯详情弹窗", FindChild(popT.transform,"BackgroundHit"), popT.transform.Find("Menu Dark Background"), TrophyInfoPopup.QHit);` | 形状**逐字同形**于同文件其余 9 处（例 `:3428` 排行榜那条）；插在已有的 `CheckAbsorbRule("奖杯详情弹窗", …)`（现 `:5468`）**之前** |

**取法逐条核过**（⛔ 不是照抄报告）：
- 命中节点 = **`BackgroundHit`** —— `Shell/TrophyInfoPopup.cs:250` 的 `MenuDraw.ShadeHit(root, ShadeR, QShade, QHit, () => Close(), "BackgroundHit")`；
- 视觉压暗层 = **`Menu Dark Background`** —— 同文件 `:247` `MenuDraw.Rect(root, CardArt.Solid(), ShadeR, "Menu Dark Background", QShade, ShadeCol)`
  ⇒ 是**那颗 quad 自己**的节点名，走 `ShadeVisualQuad` 的**第 ① 种摆法**（与 `BoosterInfoPopup` 的「节点 + 子件 `Image`」不同，**两种它都吃**）；
- 两者都是**窗口根的直接子件**（`Build()` 里 `var root = transform;`）⇒ `.Find()` 直接取得到；
- `TrophyInfoPopup.QHit = 3326`（`:90`）· `QShade = 3310`（`:72`）⇒ **3310 < 3326** ✔。

### 2·3 改坏法

| 改坏法 | 会红在哪 |
|---|---|
| 把 `TrophyInfoPopup.cs:247` 那句视觉压暗层的 `QShade` 换成别的档 | `ShadeRuleOk` 第 ③ 条（命中 quad 档 ≠ **同一扇窗视觉压暗层**那颗的档） |
| 把 `:250` 的 `QShade` 传成 `QShade+1` / `QHit−1` | 同上（这正是 A77⑬③「去自证」要抓的那一类） |
| 删掉 / 改名 `Menu Dark Background` 节点 | 文案报「**视觉压暗层**那个节点没找到 ⇒ 这条判据**不成立**」= **红**（不是静默绿） |
| 把 `:250` 改回自己那份 `MenuDraw.Hit(...)` | `WasShadeHit` 那条红 |
| 把 `QHit` 抬到 ≤ 3310 | 第 ④ 子判据（⚠️ **不是本行的功劳** —— 旧的 `CheckAbsorbRule` 也覆盖了它） |

### 2·4 没查清的部分

- 没跑（归同步点）。`Shell/TrophyInfoPopup.cs` **不在我的白名单**里，本组**只读不改**（读它只为核节点名与档号）。

---

## 三、(3) W-C3 的受限项 —— 四窗左栏页签的渲染断言 + `WebTextR`

### 3·1 左栏页签：四窗**全补**（简报只点了三个宿主，第四扇的宿主是同在白名单里的 `MainMenuScene.cs`）

**结论**：四扇窗左栏键文案的「渲染」断言此前**一条都没有**（`Shell/MenuWindowBase.cs` 自己那句注释也写着
「四窗这几颗**没有**『渲染宽度 ≤ 框宽』的断言」）⇒ 补上。判据 = **表 A**（波 C3 §一·2）的真值 + 原版 `Label` 的 `sz=(155,37.86)`。

| 窗口（宿主） | 键前缀 | 键数 | 新增位置（现读） |
|---|---|---|---|
| 奖励（`Editor/RewardsScene.cs`） | `RewardsTabButton_` | 4（含母版） | `:671-700` |
| 收藏（`Editor/CollectionScene.cs`） | `CollectionTabButton_` | 4 | `:658-687` |
| 商店（`Editor/ShopScene.cs`） | `ShopTabButton_` | 4（含母版） | `:1077-1106` |
| 社交（`Editor/MainMenuScene.cs`） | `SocialTabButton_` | 2 | `:4069-4088` |

每颗键断三件事（外加两条前提）：
1. ★ **`折行=0`**（`Label.WrappingMode == 0`，原版四窗左栏键**一律 0**）；
2. ★ **渲出来就一行**（`Label.LineCount == 1`）——`Normal` 会把 `BOOSTER PACKS` 这种带空格的键名折成两行；
3. ★ **渲出来的宽 ≤ 框宽 155**（`Label.WorldW * 108f`，`AutoFitBox` 那条教训：字号对而溢出、自检照样全绿）；
4. 前提两条：`Label` 取得到 + **走的是真 TMP**（点阵后端 `WrappingMode` 恒 `−1`，**如实红、不假装**）。

**期望值来自哪**（⛔ 都没有自证）：`0` / `1` / `155` 全部是**原版字面量**——
`折行=0` 是四窗逐份现读（表 A）；`155` 是原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`；
⛔ **没有读** `MenuWindowBase.BuildTabButton` 里那个 `labW` 常量（那是被测实现里的数）。

### 3·2 `WebTextR`（`BoosterInfoPopup.WebTextR` 那对常量）

**改动**：`Editor/ShopScene.cs:1468-1498`（插在 `Check(TextOf(…), BoosterInfoPopup.WebShopText, …)` 之后），**3 条**：
- ★ 节点中心 x = **1362.42** = (1296.42+1428.42)/2；
- ★ 节点中心 y = **751.80** = (725.86+777.74)/2；
- ★（**我加的一行**）文本框宽 = **132.00** = 1428.42 − 1296.42。

**为什么中心可比**：`Shell/BoosterInfoPopup.cs:477` 那处走 **`MenuDraw.Text(ws, WebTextR, …)`**（**不是 `TextBox`**）
且**没有** `alignLeft`（该文件里没有对它的 `MenuDraw.AlignRight` / `AlignLeft` 调用）⇒ **节点中心 = 矩形中心**。
⚠️ 报告给的是 `CheckAtWorld(…)`，**`Editor/ShopScene.cs` 里没有这个 helper**（它只在 `MainMenuScene.cs`）
⇒ 改用同文件现成的 `PxOf` / `PxYOf`（与 `CheckAtWorld` 是同一套「世界 → 画布 px」的口径），**判据不变**。
⚠️ 第三条是**报告说「不增加鉴别力」的那一半** —— 报告说的是「就**位移机理**而言」（宽度不随机理变，纯平移）；
但这里要钉的是**这对常量的另一半**：宽度只进 `SetAutoFitBox`、**不影响中心** ⇒ 不单独断就漏。**如实记这一处是我加的**。

### 3·3 改坏法

| 改坏法 | 会红在哪 |
|---|---|
| 删掉 `Shell/MenuWindowBase.cs` 末句 `txt.SetWrapping(false)` | 四窗的 `折行=0` 那条**全红**（0 → 1）；`BOOSTER PACKS` 那两颗（奖励第 4 键 / 商店第 4 键）**还会让 `LineCount` 那条一起红** |
| 只把奖励窗（或只把商店窗）那一句删掉 | 对应那一窗的 4 条红（**逐窗各断各的**，这正是「按窗分别断」的意义） |
| `BoosterInfoPopup.WebTextR` 改回 `1291.22/1423.22` | `WebTextR` 前两条红（中心左移 5.19px） |
| 只把 `WebTextR` 的**宽度**改成别的（中心不动） | 第三条红 |

### 3·4 没查清的部分

- **一次都没跑**（归同步点）。
- ⚠️ **收藏窗那四颗键名（`DECKS`/`CARDS`/`COSMETICS`/`STYLES`）不带空格** ⇒ 「就一行」那条在本窗的红法要 `auto` 缩不动才轮到；
  **当场能红的是 `折行=` 那条**。已在代码注释里写明，**不假装这条更强**。
- 商店第 4 键（母版）的运行期克隆体没断（原版那两键的标签由**服务端 store 列表**给、本地判不出来）
  —— 只断了我们**自己建**的四颗（含母版），与 `ShopWindow.Buttons` 那张表的注释口径一致。
- **并排渲（铁律 10⑥）没做**（跑不了 Unity）。判据是渲染真值（`WorldW` / `LineCount`），不是肉眼。

---

## 四、(4) A213 —— 余下 17 处调用点

**结论**：17 处**逐条核过**（真值见波 C3 §二·3 表 B），**7 处改**（原版 `折行=0`）、**10 处不动**（原版 `折行=1`）。

### 4·1 `Shell/FriendsTab.cs`（1 改 / 3 不动）

| 现读行 | 节点 | 原版 `折行` | 处置 |
|---|---|---|---|
| **`:138`** | 搜索框 `Placeholder`（`Enter player name`） | **0** | ✅ **`+ wrap: false`** |
| `:167` | `Search Player` | 1 | 不动 |
| `:170` | `Friends Title` | 1 | 不动 |
| `:317` | 行 `Friend name` | 1 | 不动 |

### 4·2 `Shell/AllianceMemberTab.cs`（6 改 / 7 不动）

| 现读行 | 节点 | 原版 `折行` | 处置 |
|---|---|---|---|
| **`:299`**（`wrap:` 落在 `:300`） | 页签键 `Button Text` | **0** | ✅ `+ wrap: false` |
| **`:358`** / **`:362`** | `CurrentActiveBadge Name > Text` · `Count > Text` | **0** | ✅ `+ wrap: false`（两处） |
| **`:724`** | `MsgRow > text`（聊天预览） | **0** | ✅ `+ wrap: false` |
| **`:891`** | `Alliance name text > Text` | **0** | ✅ `+ wrap: false` |
| **`:1197`** | `member index > Text` | **0** | ✅ `+ wrap: false` |
| `:914` / `:959` / `:967` / `:1044` / `:1215` / `:1217` / `:1235` | `extra_info` · `description text` · `members label` · 两处 `Individual rating value` · `member name` · `member role` | 1 | **不动**（**不许一刀切**） |

**机理（为什么必须显式传）**：`SocialPage.Text` / `SocialView.Text` 的 `wrap` 缺省是 `true`，
而 `SetAutoFitBox → SetWrapWidth`（`Core/TmpFont.cs:211`）**无条件**把 `m_TextWrappingMode` 设成 `Normal`
⇒ 调用点不写就跟 `1`；原版这两族**逐件不同**。
**判据只写一处**：`Shell/SocialWindow.cs` 的 `SocialPage.Text` / `SocialView.Text` 文档头。
⚠️ **那个文件我一个字都没动**（也不在我的白名单里）：`wrap` 形参与收口那句 `if (lb != null && !wrap) lb.SetWrapping(false);`
是**波 C3 那一批**落的，我**逐字读过**确认它按 `SetAutoFitBox → SetWrapping → Align*` 的顺序（`Label.SetWrappingMode` 头要求）。

### 4·3 改坏法

| 改坏法 | 会红在哪 |
|---|---|
| 去掉任一处 `wrap: false` | 那一件的 `折行` 退回 `SetAutoFitBox` 开的 `1` —— ⚠️ **本批这 7 处目前【没有】断言**（见下） |
| 把 7 处之外的 10 处**也**传 `wrap: false` | 同上（**没有断言拦**，只能靠注释与人守 —— 如实说） |

### 4·4 没查清的部分（⛔ 不猜）

- 🔴 **这 7 处【没有配断言】**：`FriendsTab` / `AllianceMemberTab` 的宿主是 `Editor/MainMenuScene.cs` 的
  社交窗那一节，但**本地数据源恒空**（好友表 / 联盟成员表都在服务器）：`FriendsTab` 的搜索框是**建了的**
  （`:138` 那一行就在 `Setup()` 里，不依赖数据）—— **这一处可以补**；
  而 `AllianceMemberTab` 的 5 处里有 4 处要**有联盟成员数据**才建得出来（`GeneralDetails` / 成员行是运行期按数据长的）。
  ⇒ **本批只落了真值，没落断言**（简报只要求「按备好的改法落地」）。
  **建议另开一件**：给 `FriendsTab` 搜索框 `Placeholder` 补一条 `CheckWrapMode(…, 0, …)`
  （宿主 `MainMenuScene` 社交窗那一节，`RebuildForTest` 的口已在）；
  `AllianceMemberTab` 那 5 处等夹具能给假数据那天一起补（同波 C3 §A213 对 `Join`/`Reject` 那两颗的处理）。
- ⚠️ 波 C3 §二·3 表 B 把 `AllianceMemberTab` 的 4 处裸 `Text` 归成「走继承来的 `SocialPage.Text`」——
  **实测不准**：`AllianceMemberTab : SocialView`（`:43`），所以**裸 `Text` 也走 `SocialView.Text`**
  （那一份再转调 `Page.Text`）。**行为与结论不受影响**（两条路的 `wrap` 形参名与语义逐字相同），
  但**归属那句是错的**，按铁律 5 记在这里（我没去改那份报告 —— 它是别人的产出，归调度台）。

---

## 五、(5) `CollectionScene.Run` 那 3 条红 —— 根因 + 判据 + 改了什么

> ⚠️ 分工先说清：**前两条 = 实现错**（`Shell/CollectionWindow.cs`）· **第三条 = 旧断言错**（`Editor/CollectionScene.cs`）。
> ⛔ 三条的裁定**都拿原版判据说话**，没有一条是「让哪一边闭嘴」。

### 5·1 · 5·2 `Shared/Close Button/Button Text` 取不到（→ 连带 `折行=-1`）

**根因**：**我们把它建成了 `Close Button` 的兄弟**。
`Shell/CollectionWindow.cs` 原来那两行是：
```csharp
var cq = Rect(transform, "UI_Button_Mulligan", cr, "Close Button", QPanel);
var cl = Text(transform, "Back", …);          // ← 父也是 `transform`：与 `Close Button` 平级
```
而断言按**原版路径**取（`FindChild(closeBtn, "Button Text")`）⇒ 取到 `null` ⇒ 第一条红；
第二条拿 `null` 的 `WrappingMode` ⇒ 报 `-1`（**同一个根因，不是第二个缺陷**）。

**判据（原版 dump，只读命令）**：
```
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Collection Menu Variant" --depth 12
  缩进即层级：
   3  Shared          167.2,70.9 → 1920.0,1080.0
   4    Close Button  192.2,83.4 →  342.2, 143.4   （150×60）
   5      Button Text 200.5,89.3 →  333.4, 137.5   'Back' 字号=40.0 auto[10.0~40.0] 对齐=Center/Capline 折行=0
```
⇒ **原版 `Button Text` 就是 `Close Button` 的子节点** ⇒ **断言的路子是对的，实现是错的**。

**改了什么（实现）**：`Shell/CollectionWindow.cs:1972-1984` —— `Text(transform, …)` → **`Text(cq != null ? cq.transform : transform, …)`**
（`cq` 是现成的局部变量，就是 `Close Button` 那颗 quad；`null` 时才退回根）。
⚠️ **画面不动**：`MenuWindowBase.Text` 用的 `Local(parent, …)` 是「**世界坐标 − 父的世界位置**」⇒ 换父**世界矩形逐位不变**
（同一个坑 `Shell/TrophyInfoPopup.cs` 文件头记过一次）。
✅ 断言**一个字没改**（它们本来就是照原版写的）。

**改坏法**：把那句改回 `Text(transform, …)` ⇒ 两条一起红（`false` / `-1`）。

### 5·3 `SDF 层高 = 550.8`（实得 **479.93**）

**结论：这是【旧断言】（2026-09-26 写的）的期望值错，不是新缺陷。**
它把「**布局矩形**」当成「**渲染矩形**」量了 —— 而 2026-10-08（A181）之后这一层**真的会被视口裁住**（那是**更贴原版**）。

**根因（逐值算给你看）**：
- `k0 = CosmoCells[0]` 是**第一排第 1 格**：`CosmoCellRect(0)` = `(377.725, 155.94, 627.725, 560.94)`
  （`CosmoView.x1` 335.44 + `CosmoPadX` 42.285 · `CosmoView.y1` **155.94**）；
- `RebuildCosmoCells` 给的 SDF 矩形 = `(r.x1−42.5, r.y1−70.875, r.x1+295, r.y1+479.925)` = `(335.225, **85.065**, 672.725, **635.865**)`
  ⇒ 布局宽 337.5 / 布局高 550.8（原版 `-42.5,-70.87,337.5,550.8` ✔），**上溢 70.875px**；
- A181 把它改走 `MenuDraw.Rect(…, CosmoView, …)`，而 `MenuDraw.Rect` 把 quad **建在 `ClipRect` 求交后的矩形上**
  ⇒ `y1 = max(85.065, 155.94) = **155.94**`、`y2 = 635.865` ⇒ **渲染高 = 479.925**；
- **交叉验证**（同一段求交的横向那一半）：`x1 = max(335.225, 335.44) = 335.44` ⇒ 渲染宽 **337.285**
  ⇒ 旧断言「宽 337.5 ± 2」**照旧通过**（差 0.215）—— **只有高那条红**，与实跑读数**逐一吻合**。
- **原版判据**：`Cardback Display/Scroll View/Viewport` 上挂着 `RectMask2D`（`m_Softness=(0,0)` · `m_Padding=(0,0,0,0)`，
  实读见波 C2 报告）⇒ **原版那 70.875px 本来就画不出来**。
  ⛔ **不许**为了这条断言把 `MenuDraw.Rect` 的 `CosmoView` 实参去掉（那正是 A181 修掉的真偏离）。

**改了什么（断言）**：`Editor/CollectionScene.cs:2863-2890`（替换原来那两条 `CheckNear`），
**拆成两件事、各有一条能红的断言**：
1. **这一格断「真被视口上沿截住」**：渲染高 = **479.925**（±1.5）+ **上沿正好停在 `CosmoView.y1` = 155.94**（±0.8）（`:2877-2888`）；
2. 🆕 **对照块**（`:2904-2925`）：改到**整块落在视口里**的那一格（`CosmoCells[CosmoCols + 1]` = 第 2 排第 2 列，
   四边逐条算过不越界）上量 **337.5 / 550.8** —— 那一格「渲染矩形 == 布局矩形 == 原版字面量」。
   ⚠️ 与 A181 自己那两条的写法**同款**（`Editor/CollectionScene.cs:3030` 的「对照：没被切的那一排…」）。

**改坏法**
| 改坏法 | 会红在哪 |
|---|---|
| `RebuildCosmoCells` 里 `MenuDraw.Rect(…, CosmoView, …)` 的 `CosmoView` 去掉 | 「上沿停在 155.94」那条红（回到 85.065）；A181 那两条也红 |
| 把 `sr` 的 337.5 / 550.8 改错（或退回「按卡背等比内接」） | 对照块两条红 |
| 把 A181 的裁切改成「越界就整层不建」 | 「上沿停在 155.94」那条红（整层不在 ⇒ 量不到） |

---

## 六、跑了 / 没跑

- ✅ **秒级类型检查**：`TMPDIR=/tmp/wf_y bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两遍）。
- ⛔ **Unity 自检一条都没跑**（红线；只有主对话能跑）。本批**所有**新老断言的期望值都是**静态算出来的**。
- ✅ **行尾**：8 个文件逐个用 `rb` 数过（`crlf=0`、`loneLF == 总行数`）⇒ **零翻转**；`git diff --numstat` 无整篇重写。
- ⛔ **没动 git**（`status` / `diff` 只读）、**没改 `项目任务.md` / `CLAUDE.md`**。

**给同步点的复跑覆盖面建议**（判据 = 铁律 12 三条）：本批动了 **5 个 `Editor/*Scene.cs` 宿主**
（`ShellScene` · `RewardsScene` · `CollectionScene` · `ShopScene` · `MainMenuScene`）
**+ 3 个 `Shell/*.cs`**（`CollectionWindow` 的 `Build()` · `FriendsTab` / `AllianceMemberTab` 的调用点）
⇒ **`ShellScene.Run` · `RewardsScene.Run` · `CollectionScene.Run` · `ShopScene.Run` · `MainMenuScene.Run` 五条都该跑**
（`ShellScene` 因为 A188 那 8 条；社交窗那两页的宿主是 `MainMenuScene`）。
✅ **不必加跑的两条**（核过，不是想当然）：
- `DeckScene.Run` —— `Shell/CollectionWindow.cs` 全工程**只在** `Editor/CollectionScene.cs` 里被建
  （`grep` 过）；卡组编辑窗走的是**另一个类** `DeckRuntime`，我只改了收藏窗 `Build()` 里那颗 `Back` 钮的父节点。
- `RuleEngineTest.Run` —— 本批**一行引擎代码都没碰**。

---

## 七、顺手发现的（⛔ **一个都没顺手改**）

### ① `Shared` 那一层我们没建（结构性偏离，**画面看不出来**）
原版是 `Content Area` 的**兄弟** `Shared` > `Close Button`；我们是把 `Close Button` 直接挂在窗口根上
（A22② 当时的注释就写了「挂窗口根、用 QPanel 那一档」）。
⇒ **世界矩形逐位相同、单看断言/截图都发现不了**，但整棵树逐节点对账时是一条差异。
**要不要补 `Shared` 这层壳**（四窗是否都该有）—— 我没查（不在本件范围），**报给调度台分流**。

### ② 收藏窗那颗 `Back` 的 `Button Text` 矩形用的是**整颗钮的矩形**，原版是内缩过的
我们的 `Text(..., cr.x1, cr.x2, cr.y1, cr.y2, ...)`（= 192.2,83.4 → 342.2,143.4）；
原版那个 `Button Text` 节点是 **200.5,89.3 → 333.4,137.5**（132.86×48.24）。
⇒ **中心几乎相同**（266.95 vs 267.2，差 0.25px）而文字是**居中**画的 ⇒ **今天看不出来**；
但 `SetAutoFitBox` 的框宽也跟着用了 150（原版那件带 `AspectRatioFitter`、实际框更小）
⇒ **字号自适应那一档可能比原版大**。**没改**（要改得先判「原版那个内缩是 ARF 算出来的还是序列化的」，没查）。

### ③ `menu_dump.py` 的「参数」列 200 字符上限（波 C3 §八·② 已记，这里再确认一次）
`AllianceView/…/description text` 那行的 `折行=` **整段被砍**（看着像「没有这个字段」）
⇒ 凡「该有的字段整行缺失」的，回 **MB JSON** 读。本批 A213 那 13 处里有 1 处（`:959` 的 `description text`）
就是**照波 C3 已经回读过的值**（= `1`）处理的，没重读。

### ④ `Shell/AllianceMemberTab.cs:363` 出厂的 `'45 Trophies Achieved!'` 带感叹号
（原版 dump 的出厂文本就是它）—— 断了 `wrap`，**没断文案**（本批只碰折行那一格）。🔵 记一笔，将来补文案断言时别写掉那个 `!`。

### ⑤ `Editor/CollectionScene.cs` 的 `Check(bl != null ? bl.WrappingMode : -1, 0, …)`
这类「**取不到就报 −1**」的写法很好（出声），但它**不区分**「节点不在」与「点阵后端」——
本批我在别处补的渲染断言都拆成了**两条前提 + 三条实测**。**没去统一**（那会一次改掉很多既有断言）。
