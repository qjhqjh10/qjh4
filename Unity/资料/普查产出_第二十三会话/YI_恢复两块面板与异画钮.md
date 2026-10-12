# 乙 · 恢复「创建副本 / 升级」+ 异画左右钮

> 写手 **乙**（2026-10-12）。**只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs`。
> 所有行号 = 本笔**现读**（改完之后的状态）；原版判据全部来自 `D:/2/tools/decomp_full/` 的方法体与解包/实拍资源。
> 触发 = 用户 2026-10-12 的裁定（原话逐字引在下面）+ 账 `A1699`（→ `资料/任务_卡组卡牌战斗.md:270`）。

---

## 一、结论（三件事各一句话）

1. **两块面板已恢复**：`Build()` 里那条 `Debug.Log` 换成 `if (Craftable(Card.Rarity)) BuildCrafting(vlg); BuildUpgrade(vlg);`
   —— UI 照原版版式，**一个字都没改**那两个方法。
2. **「升级」那颗钮 = 显示、但不可交互**：用的是**本仓现成的那一个口** `WindowButton.Interactable = false`
   （= 原版 `Selectable.set_interactable` 的等价物），**没有发明新机制**；「创建副本」那颗钮**点下去只出声**
   （`本版未实现：创建副本`），**本版不实现真机制**。
3. **异画那两颗左右 `Select Art Button` 已建**（三层贴图 + 左钮镜像 + 命中区 + 悬停/按下换图），点它**在 `0..N` 档之间回绕**
   换**前台那张**的立绘；`CardData.artOverride` 已喂进大卡（**与另外 4 处逐字同一个口径**），
   风格表**没有新写第二份**（按 `CollectionWindow.AltArtCards` 筛）。

---

## 二、逐块：**原版面板长什么样** → 改成什么 → 并排关键一行

### 2·1 「创建副本」（`Crafting Panel`）

| | 原版（判据） | 我们（改后） |
|---|---|---|
| 版式 | `1294,146.71 → 1744,410.44`；`Title` 1307.5,165.17→1726,244.29 `"Create a copy of this card"` fs42 · `Craft` 1384,244.29→1654,304.95(`UI_Button_Mulligan`) · `Explanation` 1307.5,315.5→1726,378.79 `"This will consume a wildcard"` fs40 · `Info Icon` `40K_generic_bt_info` | **逐值相同**（上一轮照正本 §二 建的，本笔**一字未改** —— `:752-792`） |
| 那格**建不建** | `CraftingPanel__CheckButtons.c`：`CardServices.CheckForCardCrafting(card, out cost)` ⇒ **`cost == null` 时 `gameObject.SetActive(0)` 整块关**；否则 `Selectable.set_interactable(+0x30, canCraft)` | `if (Craftable(Card.Rarity)) BuildCrafting(vlg); else` **整块不建** + 出声（`:500-505`）。`Craftable` = 稀有度是不是那四档（`special` = 防御/药剂/秘仪 50 张 ⇒ 假） |
| 那颗钮点下去 | `CraftingPanel__TryCrafting.c` / `TryCraftingInternal.c` → **服务端**发卡 | 🔴 **本版不实现真机制**：走 `OnCraftClicked()`，**只出声**（见 §五）。`DoCraft()`（加副本那半支）**保留但没接线** |

### 2·2 「升级」（`Upgrade Panel`）

| | 原版（判据） | 我们（改后） |
|---|---|---|
| 版式 | `1294,411.12 → 1744,672.74`；`Title` `"Upgrade this card\nto level {0}"` fs41.4 · `Upgrade` 钮 1384,524.96→1654.38,585.31 · `cards`/`cost`/`Explanation`/`Info Icon` · `No Upgrade Warning`（与 `Content` 同级、同 rect） | **逐值相同**（本笔**一字未改** —— `:797-826`、`:872-880`） |
| 满级那一支 | 🔴 `UpgradePanel__CheckUpgradeRequirements.c`：`GetNextTierInfo` 给空 ⇒ **`Content` 整组 `SetActive(0)` + `No Upgrade Warning` 露出**，**`return`（连 `interactable` 都不设）**；有下一级才 `Selectable.set_interactable(+0x40, 够不够)` | ⚠️ **本笔按【用户口径】做**（面板在、钮不可交互），**不是**照这一支的「整组关」。两处如实标注写在 `:848-855`，**交调度台裁**（见 §七·2） |
| 「不可交互」怎么落地 | `UnityEngine_UI_Selectable__set_interactable(UpgradePanel + 0x40, …)` —— `+0x40` 就是那颗钮 | `upWb.Interactable = false;`（`:860`），**排在 `Hit(...)` 之后**（理由见 §三） |

### 2·3 异画（`Alternate Art Panel`）——**本笔改动最大的一块**

**原版长什么样**（`1294,673.42 → 1744,903.01`）：`Title`（印的是升级文案 = 原版复制粘贴 bug，不抄）·
`Current Style` · `Lock Background` + `WF Lock Icon Simple` ·
**左右两颗 `Select Art Button`（74.39×75.61）** · `Buy Original Card Button`(PriceDisplayButton) 328.71×73.38。

⚠️ **两颗钮的几何量不到场景 JSON**：`Alternate Art Panel` 整条父链 `m_LocalScale = 0,0`（动画/隐藏态，
`工具/menu_rect.py --rt 1289/1384` 实测全零；正本 §六·3 记过同一条）⇒ 走**实况 dump**
（`资料/原版实拍/menu_forge_1016/runtime_ui_dump_menu_forge.tsv:154-159`，铁律 4「以实况为准」）：

| 件 | 原版实读 | 我们摆的绝对屏坐标（1920×1080） |
|---|---|---|
| `Select Art Button Right` | `m_AnchoredPosition = (171.9, 65.0)` · `sd (74.4, 75.6)` | 中心 `(1690.9, 723.215)` ⇒ **1653.705, 685.410 → 1728.095, 761.020** |
| `Select Art Button Left` | `(−177.1, 65.0)` · `sd (74.4, 75.6)` | 中心 `(1341.9, 723.215)` ⇒ **1304.705, 685.410 → 1379.095, 761.020** |
| 子件 `Background` / `Icon` | stretch 锚点 `(0.114,0.124)-(0.873,0.888)` + `sd (0.4,0.4)`、`pos (−0.1,0.3)` ⇒ **56.86×58.13**、居中 | 左 1313.470,694.150→1369.330,752.280 · 右 1662.470,694.150→1718.330,752.280 |

⚠️ **两个 x 不对称**（−177.1 / +171.9，差 4.2px）—— **原版就是这样，本笔没有「顺手对称化」**。
贴图三层（同两行实读）：根节点自己 = `UI_Button_Round_background` · 子件 `Background` = `40k_general_bt_yellow` ·
子件 `Icon` = `40k_general_bt_arrow`（**左钮镜像** = 原版 `UIFlippable(m_Horizontal=1)`，我们翻 uv 的 u）。

**改成什么**（`:1009-1040 BuildAltArrow`，左右各调一次）：
1. 圆底盘画在**节点自己**身上（节点名 = `Select Art Button Left/Right`，同原版；同 `CollectionWindow.BuildStyleArrow` 引的 A1149）；
2. 黄底 + 箭头两层**逐级高一层队列**（`QCdRow(3116) < QCdText(3117) < QCdIcon(3119)`，新常量）；
3. 命中区 `ArrowHit Left/Right`（**名字带左右**）+ 悬停/按下换图（`target` = 黄底那层、图名 `40k_general_bt_yellow`）。

**点它做什么**：`ChangeAltStyle(±1)`（`:1216`）—— **左 = −1 / 右 = +1**（按位置接线，同收藏窗那条注释），
在 **`0..Count`** 上回绕（**0 = 原画**，1..N = 该卡第 N 种异画），然后 `RebuildKeepingState()` 重画。
**换的是前台那张的立绘**：`BuildCardStack` 里 `i == 0` 时 `cd.artOverride = AltArtTexture()`，**在 `CardView.Create` 之前**（`:555-560`）。

**关键一行并排**：

```csharp
// 原版（d:/2/tools/decomp_full/AlternateArtPanel__SwapStyle.c:25）
*piVar1 = *piVar1 + (param_2 & 0xff ^ 1) * 2 + -1;     // isLeft ⇒ −1 · 否则 +1
// :30  index < 0      ⇒ index = list.Count        // 回绕
// :36  Count < index  ⇒ index = 0
// :47  if (index == 0) 取 +0x58（原画卡）· 否则取 list[index − 1]（★ 1 基）
// 我们（CardDetailPopup.cs:1216-1235）
int n = styles.Count + 1;
AltStyleIndex = ((AltStyleIndex + dir) % n + n) % n;
```

---

## 三、「不可交互」用的哪一个同族先例

**先例 = `Shell/WindowButton.cs` 的 `Interactable`**（**不是**新机制）。两半都在：

| 半 | 出处（`文件:行号`） | 做什么 |
|---|---|---|
| 变灰 | `Shell/WindowButton.cs:684-750` `RefreshGray()`（`Interactable` 的 setter 在 `:644-648` 调它；`SoftDisabled` 走同一份） | 把「`target` + 子树」的图形件换成**原版**灰化 shader `Everguild/UI/Greyscale`；取不到 shader ⇒ 出声 + 不变灰 |
| 挡派发 | `Shell/WindowButton.cs:786-798` `Click()` 头一句 `if (!_interactable) { Debug.LogWarning(…); return; }` | = 原版 `Selectable.OnPointerClick` 的第一句 |

**同族生产先例（逐字照它的两条纪律）**：`Shell/CampaignRewardWindow.cs:820-849`（`wb.Interactable = clickable;`）：

1. **存量实现只有一处**（`SoftDisabled` 与 `Interactable` 共用 `RefreshGray`）⇒ 我们**转调**，不另写；
2. 🔴 **必须排在 `Bind` 【之后】** —— `GrayTargets()` = `target` + 子树，而 `target` 是 `Bind` 才设成那颗
   **画出来的**底图的；排在 `Bind` 之前 ⇒ 头一次 `RefreshGray()` 只灰到那颗**透明的命中区 quad**、
   `GrayedForTest` 随即为真 ⇒ 后面再设都早退 ⇒ **画面上一像素都不变**（静默）。
   本笔的做法：`Hit(...)` **内部**就 `Bind`，所以 `upWb.Interactable = false;` **放在 `Hit(...)` 那一句之后**（`:856-860`），
   并且把**画出来那颗 `Upgrade Bg`** 当 `target` 传了进去 —— ⛔ **这两件事缺一不可**（原来 `Hit` 只传 5 个实参 ⇒ `target` 是空的）。

**原版同一位的判据**：`UpgradePanel__CheckUpgradeRequirements.c` 末尾
`UnityEngine_UI_Selectable__set_interactable(*(longlong *)(param_1 + 0x40), uVar2)`（字段序 `0x20` currentCardUI ·
`0x28` options · `0x30` Content · `0x38` No Upgrade Warning · **`0x40` 那颗钮**）。

---

## 四、异画：**判据怎么与收藏窗 Styles 页共用一份**；`artOverride` 用的是哪个口径

**① 表只有一份**：`AltStylesOf(cardId)`（`:908`）**只是按卡 id 筛** `CollectionWindow.AltArtCards`
（`Shell/CollectionWindow.cs:1478`，7 张 · 2 种风格，出处 = `工具/import_original_art.py` 的 `ALT_ART`）
—— **没有新写第二张表**。顺带把 `HasAltArtStyle` 改成 `AltStylesOf(id).Count > 0`（**同一份**，原来是各写一遍循环）。
风格显示名走 `CollectionWindow.StyleLabel`（同一份，不另写映射）。
⛔ 明确没做、也不该做：自己枚举 `Resources/Art/altarts/`、或把判据改成 `CardArt.AltArt(id) != null`
（那是「贴图加载成功没有」，理由写在 `HasAltArtStyle` 的注释里）。

**② 回绕口径也是同一套形状**：`((i + dir) % n + n) % n`（收藏窗 `ChangeStyle` 那一句同形）。
差别只有一处、且是**原版本来就有的差别**：收藏窗按**全局风格表**（2 种）转，详情窗按**这张卡的风格表**（1 种）转
—— 正因如此，**详情的档数是「原画 + 该卡的 N 种」= 原版的 1 基那一段**，而收藏窗那张表是另一种视图。

**③ `artOverride` 喂进去用的是哪个口径** —— **4 处消费点口径【逐字相同】，没有分歧**（这点本笔逐个现核过）：

| 消费点 | 怎么用 |
|---|---|
| `Core/CardView.cs:74` | 字段声明（`artOverride` 非空 ⇒ 立绘用它、不再按 `artId` 取） |
| `Core/CardView.cs:1911-1912` | 抠图清单跟着换：`d.artOverride != null ? CardArt.AltArtHasCutout(d.artId) : CardArt.HasCutout(d.artId)` |
| `Core/CardView.cs:5574` | `var real = d.artOverride != null ? d.artOverride : CardArt.Portrait(d.artId);` |
| `Battle/BattleDriver.cs:4766` | 进攻卡：构造 `CardData` 时就 `artOverride = CardArt.OffensiveFace(...)` |

⇒ 共同口径 = **`CardData.artOverride` 非空就覆盖立绘**，且**必须在 `CardView.Create` 之前设好**
（`Create` 那一趟就按它建了立绘层；同 `CollectionWindow.cs:1779` 那条注释「`d.artOverride` 是在 `Create` 之前设好的」）。
本笔照这一口径做（`BuildCardStack` 的 slot 0）。

---

## 五、创建副本那颗钮：今天的行为（出声原文）+ 两条待裁候选

**今天**（`:1240-1252 OnCraftClicked`）——**什么都不改**，只打一条：

```
[CardDetail] **本版未实现：创建副本** —— 那颗钮点了**什么都不做**（「<卡名>」的拥有数仍是 N、万能卡一张没扣）。
用户 2026-10-12 裁定「先恢复 UI」，真机制待裁；引擎里那份半成品实现是 `DoCraft()`（加副本、**不扣**万能卡），本版**没有**接它。
```

**两条待裁候选**：

| | 做什么 | 现成到哪一步 | 还差什么 |
|---|---|---|---|
| **①** | **真扣通配符并加副本** | 加副本那一半**已有**（`DoCraft()`，`CardProgress.AddOwned(+1)`，保留未删）；要接回去 = `BuildCrafting` 末尾那句 lambda 换回 `DoCraft` | 🔴 **扣通配符那一半本地没有判据**：原版走 `MenuDeck/Craft/*` + 服务端，**万能卡没有发放源**（正本 §六·7 那几条老账；本窗的通配符条现在**恒印 99**，见 `BuildWildcards` 的注释）。⇒ 得先裁「万能卡是不是也走『给足』那套口径」 |
| **②** | **永久只出声** | 就是今天这版 | 无（但面板上那行 `"1 〔图标〕"` + `"This will consume a wildcard"` 会**永远对不上实际行为** ⇒ 要么接受、要么把那两处也改成「本版未实现」的口径） |

---

## 六、要调度台做的事

### 6·1 跑哪几条自检（候选）

**结论：`CollectionScene.Run` 是必跑的一条，而且它【现在会红】**（见 6·2）。
`ShellScene.Run` **也要跑**（它有一条读本文件**源码文本**的静态断言，`Editor/ShellScene.cs:6949` ——
`"1726f - 1307.5f, 10f, 40f, 36f);"` **仍在原位**（本文件 `:776`），本笔没动那一行 ⇒ 预期**绿**）。
`DeckScene.Run` / `BattleScene.Run`：本笔**没有**碰战斗那扇窗（`Battle/CardDisplayWindow.cs`）、也没有碰卡组编辑，
`Editor/BattleScene.cs:7325-7331` 那条「本窗没有 options 族」钉的是**战斗版**（不读本文件）⇒ **按覆盖面不必跑**；
但 `甲` 若把卡组编辑改成开本窗，**那一条要重估**。

### 6·2 🔴 **已知会红的断言（本笔【不能改】那些文件 —— 自检宿主不在白名单里）**

| 文件:行号 | 断言 | 为什么红 |
|---|---|---|
| `Editor/CollectionScene.cs:7592` | `CheckTrue(FindChild(dw.transform, "Craft Icon") == null, …没有「创建副本」那一格…)` | 面板恢复了 ⇒ `common`/`rare`/`epic`/`legendary` **4 次**红（`special` 那一档仍绿 = `Craftable` 假） |
| `Editor/CollectionScene.cs:7594` | `CheckTrue(FindChild(dw.transform, "Upgrade Title") == null, …没有「升级」那一格…)` | 面板恢复了 ⇒ **5 次全红** |
| `Editor/CollectionScene.cs:6409-6414` | 点 `CraftHit` ⇒ `Check(Owned, owned0 + 1)` | 那颗钮现在**只出声、不改数** ⇒ 实得 `owned0` |
| `Editor/CollectionScene.cs:6417-6421` | 点 `UpgradeHit` ⇒ `Check(Level, lv0 + 1)` | 那颗钮 `Interactable = false` ⇒ `Click()` 早退 ⇒ 等级不变 |
| `Editor/CollectionScene.cs:7579`（Section 标题） | `Section("卡片详情窗 · 「创建副本」/「升级」都不建")` | **标题本身**也过期了（口径已由用户 2026-10-12 推翻） |

**建议口径（⛔ 由调度台裁、由调度台改）**：
- 7592/7594 两条**反向**（改成「这两格**在**」），并**保留 `special` 那一档的「不在」**（那一档判据是 `Craftable`，没变）；
- 6409-6414：改成断「**点了什么都没发生** + **出声**」（`Owned` 不变）—— 这才是本版口径；
- 6417-6421：改成断「**那颗钮 `Interactable == false`** ⇒ `Click()` 后等级不变」，并顺手断
  `GrayedForTest` / `WindowButton.GrayShaderAvailable`（判据 = `CampaignRewardWindow` 那一族的写法）；
- 7579 标题改成「两块面板恢复（用户 2026-10-12 裁定）」。

### 6·3 另外两件要给用户的裁定（本笔**没做**，都有判据、都不在本人白名单）

1. **满级那一支的版式**：原版满级时是 **`Content` 整组关 + `No Upgrade Warning` 露出**（判据见 §2·2），
   而本笔按用户口径做成「面板在、钮灰着」。⇒ **要按用户口径、还是要照原版那一支**？
2. **`CardProgress.Level` 的缺省**：现在是 **1**（不是 `MaxLevel`）⇒ 那颗 `Title` 会印
   `"Upgrade this card to level **2**"`、`No Upgrade Warning` 也不露 —— 与用户那句「**默认最高级**」**不完全自洽**。
   要改成「缺省 = 最高级」的话，影响面**在本文件之外**（吃到 `CardProgress.CanUpgrade` 的还有
   `Shell/BoosterPackOpenWindow.cs:615/622`、以及 `Editor/CollectionScene.cs:6313/6407-6421` 那几节）
   ⇒ **本笔不动**，请裁。

---

## 七、没查清的部分（如实）

1. **两颗圆钮的高亮图/按下图，名字是我按命名规则推的（`40k_general_bt_yellow_hover` / `_pressed`）**，
   **不是从原版读出来的**：原版那两个 sprite 是 `m_FileID = 6` 的**外部引用**（pptr PathID
   `556997698038149155` / `-5560139829673007091`），我在这个 bundle 的 `MonoBehaviour/` 里解不出它们的**名字**。
   ⚠️ **旁证**：`Shell/CollectionWindow.cs` 引**同一件预制体**的那两颗圆钮（`/Header/Select Art Button {L,R}`，
   普查 `资料/普查产出_0923/A4_装饰页与驱动链.md:129`）**也是这么接的**（`AddHit(..., target, "40k_general_bt_yellow")`
   ⇒ 同一个 `Bind` 派生规则），而且本地两张图都在 `Resources/Art/ui_menu/`。
   ⇒ **判据是「同一件预制体 + 同一条既有接法」，不是「我读到了原版那两个 sprite 名」**。
2. **原版 `NavigationButtonClick` 里那两下 `set_interactable(0)/(1)` 没有复刻** —— 它挡的是「切图是异步加载、
   切到一半再点会串档」；我们这侧是**同步取 `Resources` 缓存**，那个窗口不存在。**如实标成偏离**（注释在 `:1229-1232`），
   ⛔ 没有为了「像」而加一个没有对象的闸。
3. **`Current Style` 那一格的文案是我挑的**（原版它是**风格图标**，`AlternateArtPanel.SetStyle` 里
   `set_sprite(GetStyleIcon(style))`；**风格图标 SO 在服务端**，本地没有）⇒ 现在印「风格名 + 第几档」。
4. **`Buy Original Card` 那颗钮画出来的框比原版低 42px**（我们 `y = 820..893.38`；原版
   `m_AnchoredPosition (225, 88)` + anchor/pivot 在面板左下 ⇒ **778.32..851.70**）—— 本笔**没动它**
   （动它会与上面那行 `Current Style` 文字（762..800）撞上 ⇒ 是把一个偏离换成一个更差的重叠），
   命中区照**现在画出来的那一块**摆（同矩形）。**这是一条新账，请调度台记**。

---

## 八、顺手发现（只报不改）

1. 🔴 **`CardProgress.Level` 缺省 1 ≠ 用户那句「默认最高级」** —— 见 §6·3·2（有影响面清单）。
2. 🔴 **`Lock Icon` 的显隐与原版相反**：`AlternateArtPanel__SetStyle.c` 里
   `GameObject.SetActive(*(param_1 + 0x38) /* = lockIcon */, **!InventoryManager.HasItem(当前那张)**)` ⇒
   **拥有那张（异）画时锁是【关】的**；本作是**全解锁**（7 张异画都在收藏里）⇒ 原版这一格**恒关**，
   而我们**恒画**（`MenuDraw.Rect(… "WF_Lock_Icon_Simple")`，`:977`）。
   ⚠️ 附带：**它的位置/尺寸也与原版不符** —— 我们 `1310,690 → 1360,740`（50×50）；原版是
   `Lock Background`（`UI_Deck_Information_Tab`）65.8×124.9 @ 面板局部 `(230.5, 3.7)` ⇒ 屏 **1716.6,722.05 → 1782.4,846.95**，
   子件 `Lock icon` 28.1×42.2。⇒ **两条都没做**（判据齐、只是不在本笔口径里），**请记新账**。
3. 📌 **`R4` §六·3 那条「原版 `Select Art Button` 到底改不改『玩家实际用哪张立绘』」—— 本笔顺手查清了，可以销**：
   `AlternateArtPanel__SetStyle.c` 末尾那两跳（`+0x70 onArtSelectedMethod` / `+0x80 OnAlternateArtSelected`）
   都以「**调用方传了回调**」为前提，而这条链的调用方是
   `CardDisplayWindow.ShowCard(card, tier, showOptions, **onChangeArt: 0**, flag)`（正本 §四·3 记的实参）
   ⇒ **`+0x70` 恒空**（`SetStyle` 里 `cVar2 == '\0'` 那一支根本不进）⇒ **纯预览、不写回、不装备**。
   我们这侧照这个做（只看不改），**没有引入任何写回**。
4. 📌 **`R4` §六·4 那条「`AlternateArtPanel__BuyOriginalCard.c` 没读」—— 本笔读了**：
   `WindowsManager.ShowPopUp(<确认弹窗>)` + `PlayfabWrapper.GenericCloudScriptHandler(0x385, …)`（**服务端 CloudScript**），
   价格来自 `ItemsConfig.GetOriginalArtPrice`（LiveOps 配置，本地取不到）⇒ **本地那条链根本走不通**，
   所以我们的处理是「接上命中区 + 出声」（见 §2·3 与 `OnBuyOriginalCardClicked`）。
   另外 `TogglePurchaseBasicCard.c` 也一同读了：`param_2 = 0`（不需要买原画）⇒ 关 `+0x30`（风格图标）+ 藏 `+0x40`（那颗钮）；
   `param_2 = 1` ⇒ 去 `ItemsConfig.GetOriginalArtPrice` 取价、取不到连钮都不建 ⇒ **我们那颗价签是【恒显示】**（未改，如实标）。

---

## 九、改动清单

**文件（唯一）**：`Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs`
**行尾**：**LF**（改前 LF、改后 LF；`crlf = 0 / lf = 1387`，二进制现数）
**`git diff --numstat`**：`351  39  Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs`（不是整篇重写）

| 现读行号 | 改了什么 |
|---|---|
| `:169` | 新常量 `QCdIcon = 3119`（异画圆钮的箭头那一层，理由/先例写在注释里） |
| `:186-207` | 新常量族 `AltBtnW/H` · `AltArrowW/H` · `AltBtnCxL/CxR/Cy`（实况 dump 出处逐条写在注释里） |
| `:324-326` | 新自检口 `AltArrowHit(side)` / `BuyHit` |
| `:344` | 新字段 `AltStyleIndex`（原版 `currentStateIndex`，0 = 原画） |
| `:411-414` | `ShowCard` 里归零（原版每次重跑 `Initialize`） |
| `:496-505` | **`Build()` 恢复两块面板**（原来那条 `Debug.Log` 顶掉一切）+ `Craftable` 那一支出声 |
| `:555-560` | `BuildCardStack`：slot 0 吃 `cd.artOverride`（**在 `CardView.Create` 之前**） |
| `:749` | `Craft Bg` 的 quad 存进变量（给命中区当 `target`） |
| `:768-776` | 旧注释里「本处没有生产路径 / `BuildCrafting` 全库零调用点」**就地销账** |
| `:784` | `CraftHit` 换成「传 `target` + 走 `OnCraftClicked`」 |
| `:794` | `Upgrade Bg` 的 quad 存进变量 |
| `:827-866` | **`Interactable = false` 那一整段**（含判据、同族先例、两处如实标注） |
| `:892-911` | `HasAltArtStyle` 改为转调 `AltStylesOf`；新增 `AltStylesOf` |
| `:913-926` | 新增 `AltArtTexture()`（0 档返回 null；取不到图出声） |
| `:928-1002` | **`BuildAltArt` 改写**（两颗圆钮 + `Current Style` 跟档 + 价签接命中区） |
| `:1009-1040` | 新增 `BuildAltArrow` |
| `:1216-1238` | 新增 `ChangeAltStyle` |
| `:1240-1252` | 新增 `OnCraftClicked` |
| `:1254-1263` | 新增 `OnBuyOriginalCardClicked` |
| `:1262-1274` | `DoCraft` 的文档补「本版没接它 / 为什么保留」 |
| `:1277-1283` | `DoUpgrade` 的文档补「从那颗钮上到不了」 |
| `:9-16`（文件头） | **铁律 5 就地订正**：「菜单版 ≠ 战斗版 … 战斗 743px」那一段（判据 → 正本 §十·4） |
| `:38-45`（文件头） | **铁律 5 就地订正**：「升级/创建副本只改本地拥有数与等级」那句（已被本笔取代）+ 异画左右钮那一条改成「已建」 |

**没碰**（逐个确认过）：`CardPresentation/Core/**` · `Battle/**` · `Deck/**` · `Editor/**`（含两个自检宿主）·
`RuleEngine/**` · `工具/**` · `资料/**`（除本报告）· `Shell/CollectionWindow.cs`（只读它）·
**`ShowCard` / `Create` 的签名一个字没动**。

**验过**：`TMPDIR=/tmp/wf_yi bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改完跑了两遍）。
⛔ **没跑 Unity 批处理**（红线）。
