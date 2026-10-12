# 丙 · 默认最高级 + 创建副本文案

> 写手 **丙**（2026-10-12）。触发 = 用户当天的两条面裁定（逐字引在 §一）。
> 所有行号 = **本笔改完之后现读**；判据全部来自 `D:/2/tools/decomp_full/` 的方法体（亲读）与本仓源码。
> **只改了 2 个文件**（`Shell/CardDetailPopup.cs` · `Shell/BoosterPackOpenWindow.cs`），⛔ 没跑 Unity、没动 git、没碰正本。

---

## 一、结论（两笔各一句话 + 用户能看到什么变化）

1. **「默认最高级」已落到取数口**：`CardProgress.Level(id)` 的**缺省值 `1` → `MaxLevel`**（`:141`）。
   用户能看到的变化：详情窗「升级」那块面板的标题由 **`Upgrade this card to level 2`** 变成
   **`Upgrade this card to level 4`**，三格数值由「2 张 / 500 金 / +100 点」变成**第 4 级那一行**
   （9 张 / 2000 金 / +500 点）；那颗钮照旧灰着、点不动。开包那枚「可升级」角标**由恒亮变永不亮**（见 §二·波及面）。
2. **「创建副本」的消费文案已带标识**：`Craft Explanation` 那行由
   `This will consume a wildcard` → **`This will consume a wildcard  Not implemented in this build*`**（`:822`）。
   记号 = 本工程**现成**的末尾 `*`（同族先例见 §三，⛔ 没发明新记号）。那颗钮的行为**一字未改**（照旧只出声）。

🔴 **一条必须让调度台知道的硬事实**：`No Upgrade Warning` 在本版**恒不露**，而且**不是漏做、是几何上的必然** ——
它与 `Content` **同 rect**，同时 active 就是两行字叠在一起。**这一条就是乙 §6·3·1 第 1 条那条待裁**，
它**不是版式偏好、是不能回避的约束**（详见 §二·4）。

---

## 二、(1) `Level` 的现状与判据 · 改了哪个取值口 · 波及面逐处

### 2·1 `CardProgress.Level` 现在怎么取值的（现读）

| 问题 | 答案 | 出处 |
|---|---|---|
| 定义在哪 | **`Shell/CardDetailPopup.cs` 里的 `static class CardProgress`**（⚠️ **没有** `Core/CardProgress.cs` 这个文件 —— 白名单写的那条路径**不存在**，`Level` 真身在这里） | `:73` · `:141` |
| 怎么取值 | 纯内存静态字典 `_lvl`：**取不到就返回缺省** | `:141` |
| 缺省是多少 | 改前 = **`1`**；改后 = **`MaxLevel`（= 4）** | `:141` · `:78`(`MaxLevel`) |
| 存档里存吗 | **不存**。`_lvl` 是 `static readonly Dictionary<string,int>`，`ResetForTest()` 只清它 | `:97` · `:157` |
| 唯一的写入点 | **`SetLevel` 全仓只被 `DoUpgrade()` 调一次**（`:1378-1383`），而那颗钮自 2026-10-12 起 `Interactable = false` ⇒ **本版实际永远取缺省值** | 现扫全仓 `SetLevel(` |
| 谁在读 | 只在 `CardDetailPopup.BuildUpgrade` / `DoUpgrade` / `CanUpgrade`，＋自检宿主 `Editor/CollectionScene.cs` | 见 §2·3 |

### 2·2 原版对应的语义（亲读判据）· 「满级」该由谁表达

🔴 **原版里「卡框档」和「升级面板」吃的是【同一个数】**，我们把它拆成了两条路，这才是病根：

```
d:/2/tools/decomp_full/UpgradePanel__CheckUpgradeRequirements.c
  :57   uVar3 = RawCardScript__get_CardTier(lVar7, 0);          // ← 卡自己的 tier
  :58   uVar4 = PlayerItem__get_Rarity(lVar7, 0);
  :60   local_58[0] = CardTierConfig__GetNextTierInfo(lVar8, uVar3, uVar4, 0);   // ← 下一档
  :113   SetActive(*(param_1 + 0x30) /*Content*/,          local_58[0] != 0);
  :116   SetActive(*(param_1 + 0x38) /*No Upgrade Warning*/, lVar7 == 0);
  :117   if (local_58[0] == 0) return;      // ← 满级：到此为止，连 interactable 都不设
  :121   Selectable__set_interactable(*(param_1 + 0x40) /*那颗钮*/, uVar2, 0);
```

而卡框那一路（本仓 `Core/CardArt.cs:251-270` 已写明）是
`CardFramesSO.GetClanFrame(army, **tier**, cardType)` —— **入参里根本没有稀有度**。

⇒ **判清的结果**：
**「满级」在原版是【一个** `CardTier` **数】同时表达的** —— 不存在「卡框已经满级、而等级还是 1」这种状态。
我们这边 **`CardArt.TierOf` 早就恒返 `MaxTier`**（用户 2026-09-27 拍板「所有卡一律取最高档」= 原版的满级状态），
而 `CardProgress.Level` 缺省还是 `1` ⇒ **同一个数在两处打架**。
⇒ **正解 = `Level` 跟着 `TierOf` 走**（不是另立一套），也就是用户 2026-10-12 那句「**改成默认最高级**」。
本笔就是这么落的：**改的是 `Level` 的缺省（`Level` 是那个「满级」该被读到的口）**，
`TierOf` 一个字没动（它本来就对）。

⚠️ **`Level` 的语义在别处【没有】第二种含义**（这是我改之前专门核过的那一条）：全仓 `CardProgress.Level`
只被两个文件读到 —— `Shell/CardDetailPopup.cs` 与自检宿主 `Editor/CollectionScene.cs`；
开包/收藏/统计里**没有任何地方**把 `Level` 当别的意思用（收藏窗读的是 `Owned` / `DeckCap` / `Spares`，与 `Level` 无关）。

### 2·3 波及面逐处 —— 该不该跟着变（逐处现核）

| # | 位置（现读） | 改前读数 | 改后读数 | 判「该不该跟着变」 |
|---|---|---|---|---|
| 1 | `CardDetailPopup.cs:839` `Title` = `"…to level " + Min(Level+1, MaxLevel)` | `level 2` | **`level 4`** | ✅ **该变**（就是用户指出的那一处症状）。⚠️ `Mathf.Min` 那一下是**为满级写的钳位**，⛔ 别删（删了 `NeedCopies[Level+1]` 会越界）。⚠️ **本来含义有残留**：原版满级时**根本不写这颗 Title**（`:117` 就 `return` 了）⇒ 原版没有「满级 Title 印什么」的判据，我们印的是钳位后的数（读起来像「还要升到 4 级」）—— **如实标，没改**（要改 = §二·4 那条待裁） |
| 2 | `:845` `need` = `NeedCopies[Min(Level+1, MaxLevel)]` | 2 | **9** | ✅ 该变（同一族的钳位，同一个判据面） |
| 3 | `:862` `cost` = `NeedGold[…同上]` | 500 | **2000** | ✅ 该变 |
| 4 | `:870` `points` = `Points[…同上]` | +100 | **+500** | ✅ 该变 |
| 5 | `:844` `can` = `CanUpgrade(id, rarity)` | **恒真**（Level=1 < 4 且「给足」的 Spares 够） | **恒假**（`l < MaxLevel` 第一句就 false） | ✅ 该变（「满级 ⇒ 升不了」本来就是它该给的答案） |
| 6 | `:911→961` `No Upgrade Warning` 的 `SetActive` | `!can && Level>=MaxLevel` = **false** | **仍是 false**（本笔**加了显式的互斥条件**，理由见 §二·4） | ⚠️ **没有跟着变** —— 这是**本笔唯一的「该露但露不了」**，被 §二·4 那条待裁卡住；**状态与改前逐字相同**（所以不引入回归） |
| 7 | `:1378` `DoUpgrade()` 的三个守卫 + `SetLevel` | 不可达（钮灰着） | 不可达，且 `Level>=MaxLevel` 那一条**现在真的成立**（会打「已经满级」那句） | ✅ 该变，且更对 |
| 8 | `CardProgress.CanUpgrade` 的消费点 `Shell/BoosterPackOpenWindow.cs:624` → `:610` `UpBadges[i].SetActive(canUp)` | **恒亮**（文件头自己写着「恒真」） | **恒不亮** | ✅ **该变，而且是修对**：原版那枚角标 = `CardServices.CheckForCardUpgrade`（服务端），语义就是「**有没有下一级**」；「默认最高级」的答案本来就是「没有卡能升级」。⚠️ **层照建**（`UpBadges[i]` 不删、只 `SetActive(false)`）—— `Editor/ShopScene.cs:2520` 断的是 `UpBadges[0] != null`（**节点在不在**），⇒ **那条照样绿** |
| 9 | `Editor/CollectionScene.cs:6429-6437`（自检宿主，⛔ **不在我白名单**） | `lv0 = Level(...)`，点钮后 `Check(Level, lv0)` | `lv0` 由 1 变 **4**，等式**照样成立**（点了不变） | ✅ **不用改**（是等式断言、不是定值断言）。⚠️ 但**消息文案**里那个 `{lv0}` 会印 4 —— 只是数字变了，不是红 |
| 10 | `Editor/CollectionScene.cs:7617` `FindChild(…,"Upgrade Title") != null` | 绿 | **照样绿**（本笔**没删任何节点**） | ✅ 不用改 |

🔴 **我改之前专门核过、结论是「不会破坏别的语义」**：
没有存档、没有第二个写入点、`Level` 在别处没有第二种含义。
⇒ **没有触发简报里那条「停手报回」的口子**（那条口子举的例子是「开包/收藏页/统计里 `Level` 另有含义」，实测不存在）。

### 2·4 🔴 **唯一卡住的一处：`No Upgrade Warning` 与 `Content` 同 rect，二者不能并存**

**这不是版式偏好，是几何硬约束**（本笔亲读 + 本仓源码逐值核）：

- 原版靠**同一个布尔的两面**保证互斥：`SetActive(Content, next != null)` / `SetActive(Warning, next == null)`
  （`UpgradePanel__CheckUpgradeRequirements.c:113-119`，全文见 §2·2）。
- 我们这两件**同 rect**：`No Upgrade Title` @ `1314,430.62,1724,644.92`
  （出处 = `资料/阶段二_卡片详情窗_原版规格.md` §二 那张表的 Upgrade Panel 那一行，**我们摆的是逐值相同的位置**），
  而 `Upgrade Bg` / `Need` / `Cost` 就在 `524.96..585.31`；`Label` 出厂对齐 = **Center/Middle**
  ⇒ 那行 `Maximum card tier reached` 单行落在 **y≈537** ⇒ **正好压在 `Upgrade` 那颗钮上**。
- ⇒ 改前 warning 不露（`Level=1`）；**改缺省之后它会变成「该露」** —— 而我**没有**让它露，
  因为**一露就是两行字重叠**。本笔的落法是把互斥**显式写出来**（`bool contentShown = true;`），
  一行就能翻面，⛔ 不留两处各写一半。

🔴 **要它露，必须先落乙 §6·3·1 第 1 条那条待裁**（口径 = 「满级 ⇒ `Content` 整组关」），
判据是原版那三行；落地办法已写在 `:955-958`（把那 7 件一起 `SetActive(false)` + `w.SetActive(maxed)`，**一处改**）。
⚠️ **本笔没有自己裁它**，理由：调度台在 `Editor/CollectionScene.cs:7593-7600` 亲手写下的口径是
「**那块钮 = 显示但不可交互**（…**不是删面板**）」—— 「显示」与「`Content` 整组关」**直接冲突**，
所以这是**用户/调度台口径层**的事，不是我能在白名单文件里自己定的。
📌 **两个候选各自的代价**（留给调度台一句话就能裁）：
- **候选 A（保持现状 = 本笔）**：面板全显（标题 / 9 张 / 2000 金 / +500 / 灰钮），
  代价 = **`No Upgrade Warning` 永远不露**，且原版没有「满级 Title 印什么」的判据 ⇒ 标题读起来像「还要升到 4 级」。
- **候选 B（照原版满级支）**：`Content` 整组关 + warning 露，代价 = **面板的其余部分一并不显**
  （含那颗刚做好的灰钮 —— 灰化工作就成了「看不见的活」），且与 `CollectionScene.cs:7593-7600` 那句「显示」冲突。

---

## 三、(2) 创建副本：选了哪种标识形式 + 同族先例出处 + 改了哪一行

**形式 = 「改文案 + 末尾缀 `*`」**（不是「灰掉整块」，也不是给数字加记号）。

- **同族先例（全仓只有这一族，就是本工程唯一的「本版未实现」记号）**：
  · `Battle/BattleDriver.cs:12064-12097` `UnimplementedNotes(CardDef)` ——
    `list.Add("效果本版结算不了*")` / `list.Add("效果本版没作用*")`；
    纯名字那一形 = `CardText.Keyword(kw) + "*"`（同文件 `:12040` / `:12046`）。
  · 形状 = **「文案自己把状态说清楚」+ 末尾一个 `*`**；`*` 就是「本版未实现 / 没作用」。
  · 拼接形状（原句 + **两个空格** + 缀注）照 `BattleDriver.FaceTextFull`（`:11974`）
    那句 `string.IsNullOrEmpty(notes) ? body : body + "  " + notes`。
  ⛔ **没有发明新记号**；⛔ 也**没有**去用 `ErrUnimplemented`（那是引擎的返回码，
  本仓**没有任何地方**把 `RuleCodes.Message(...)` 渲染到画面上，现扫全仓 `RuleCodes.Message` **零命中**）。

- **改了哪一行**：`CardPresentation/Shell/CardDetailPopup.cs:821-824`
  `Craft Explanation` 那一行的字符串：
  `"This will consume a wildcard"` → **`"This will consume a wildcard  Not implemented in this build*"`**
  （原版原句**保留成前缀**，只加缀注 —— 与卡面「原文保留 + 附注」同形）。
  那一行上面 `:799-820` 是新写的注释块，写清记号出处 / 为什么选英文 / 为什么数字不加记号 / 字号是估算。

- **为什么不是「灰掉整块」**：灰 = `WindowButton.Interactable = false`，而 `Click()` 会在派发前就 `return`
  ⇒ **那颗钮就不再走 `OnCraftClicked`、不再出声** —— 与用户「**只出声、不做机制**」的裁定**直接冲突**
  （「只出声」的前提是它还能被点）。而且「灰」只读得出「禁用」，读不出「**本版没实现**」。

- **为什么 `"1 〔万能卡图标〕"` 那两件没加记号**：`Craft Text` 就是一格**数字**（原版 `Button Text` = `"1"`），
  它是「价格」槽、不是「承诺」句 —— 往数字后缀 `*` 会被读成「价格是 1\*」（另一件事）。
  面板上那句**承诺**是 `Craft Explanation`，记号落在这里。

- **⛔ 行为一字未改**：`BuildCrafting` 末尾那句 `Hit(p, "CraftHit", b, () => { OnCraftClicked(); }, …)`
  **没动**（`:832`）；`OnCraftClicked` 照旧只 `Debug.Log`（`:1319-1344`，只补了文字让它指回这句文案）。

- ⚠️ **语言选英文**（不是中文）：本窗**每一句都是原版 prefab 的英文原文**（我方**没有** I2 词条表），
  往英文面板里插一句中文会更难读。中文先例 `效果本版没作用*` 是卡面那一路、它走 `CardText`、本来就按语档切。

---

## 四、要调度台做的事

### 4·1 跑哪几条自检（按覆盖面）

- ✅ **`ShellScene.Run`** —— 本笔动了 `Shell/`。⚠️ 它有一条**读本文件源码文本**的静态断言
  （`Editor/ShellScene.cs:6943-6955`，找的是 `"1726f - 1307.5f, 10f, 40f, 36f);"`）
  —— 我**只改了同一条语句第一行的字符串字面量**，那个子串**原位保留**（现读在 `:824`）⇒ **预期绿**。
- ✅ **`CollectionScene.Run`** —— 详情窗是它开的；**本笔没删任何节点**，两条等式断言（`:6429-6437`）与
  存在性断言（`:7617-7624`）**预期全绿**（理由逐条在 §2·3 第 9/10 行）。
- ✅ **`ShopScene.Run`** —— 本笔改了 `BoosterPackOpenWindow` 的注释（含 `ReadyForLevelUp` 的 doc）。
  ⚠️ **`ReadyForLevelUp` 的返回值由恒真变恒假**，但 `Editor/ShopScene.cs:2520` 断的是
  `bp.UpBadges[0] != null`（**节点在不在**，不是 `activeSelf`）⇒ **预期绿**。
  🔴 **这一条是我唯一不 100% 确定的**：如果 `ShopScene` 底下还有别处断了角标的 `activeSelf`，
  它会红 —— **红了请按「新口径」改那条断言（恒假才是对的），⛔ 别回退 `Level` 的缺省**。
- ⛔ **不必跑** `BattleScene.Run` / `DeckScene.Run` —— 本笔没碰战斗那扇窗、没碰卡组编辑、没碰引擎。
- ⛔ **纯文档部分 0 条**。

### 4·2 会被碰到的断言（⛔ 都不在我白名单，**要改由调度台改**）

**本笔预判：一条都不用改**（理由见 §2·3 第 9/10 行 + §4·1）。唯一需要盯的是 `ShopScene.Run` 那一条。

---

## 五、没查清的部分（如实）

1. **改后那行文案的字号是【估算】，不是量出来的**（本笔红线：不跑 Unity）。
   框 `1307.5,315.5 → 1726,378.79`（418.5 × 63.29）、autosize `[10~40] base 36`、折行开
   ⇒ 63 个字符估下来落在 **fs≈25 / 2 行**（改前 29 个字符约 fs≈26~35）。
   **要复核就量渲染后的 `fontSize` 与块高**（`Label.DumpSizes()`），⛔ 别只比字号。
   ⚠️ 框宽 `418.5` 本身是**我们自己摆的**（原版那颗的 `m_SizeDelta = (0,0)` + 祖先链 `m_LocalScale = (0,0)`，
   **框宽量不到**）⇒ 本笔**没有**动它（铁律 3：查不到就说查不到）。
2. **改缺省之前有没有别的地方落过 `SetLevel`**：现扫全仓 `SetLevel(` 只有 `DoUpgrade` 一处，
   且那颗钮不可交互 ⇒ 我判「永远取缺省」。⚠️ **但这依赖「钮一直灰着」这个前提** ——
   若将来口径改成可升级，`Level` 就会开始有真值，**那时 `MaxLevel` 这个缺省就不再是「一律满级」的保证**。
3. **原版满级时那颗 `Title` 印什么 —— 判据是空的**（原版在写 Title 之前就 `return`）。
   我们印的是钳位后的数；**这一处没有原版值可对**，如实标，⛔ 没编一个（铁律 3）。
4. **`No Upgrade Warning` 露出来之后与 `Content` 到底重叠多少** —— 我用几何算的（§二·4），
   **没进真机/批测量过像素**（红线）。判据链是硬的（同 rect + Label 居中），
   但「重叠成什么样」这一步是推导，**要真机看一眼**。

---

## 六、顺手发现（⚠️ 只报不改 —— 都不在我白名单）

1. 🔴 **`Core/CardArt.cs:266-267` 那条注释现在【会误导人】**：
   原文写「要退回『按升级档画』，把这里换成 `Mathf.Clamp(CardProgress.Level(id), 1, MaxTier)` 那条路即可
   （升级档本来就存在）」—— **那条路现在走不通了**：`Level` 缺省已是 `MaxLevel`，
   `Clamp(MaxLevel,1,4)` 还是 `MaxTier` ⇒ **换了等于没换**。
   ⇒ 要退回「按升级档画」得**同时**把 `Level` 的缺省改回去（`Core/CardArt.cs` 不在我白名单，**没动**）。
2. 🔴 **`Editor/ShellScene.cs:6954` 那句注释已过期**（乙上一轮就记过，本笔**复现**）：
   「⚠️ **静态断言**：证明不了运行时生效（**那处没有生产路径**，理由见上）」——
   **`BuildCrafting` 已经有生产路径了**（`Build()` 里按 `Craftable(稀有度)` 调它）。
   原文与乙在同一句注释里的「就地订正」**互相打架**（铁律 5 说的「两份说法打架比没有更糟」）。
   ⇒ 建议把那一句删掉（`Editor/` 不在我白名单）。
3. 📌 **`CardProgress.Level` 的物理位置**：白名单写的 `Core/CardProgress.cs` **不存在** ——
   它在 `Shell/CardDetailPopup.cs` 里（`static class CardProgress`，`:73`）。
   本笔按白名单的「或你现读出来的、`Level` 真正的落点文件」那一句落的手。
4. 📌 **`CardDetailPopup.cs:844` 的 `can` 与 `:911` 的 warning 判据里都含 `Level >= MaxLevel`**：
   改之后 `can` 恒假、`Level >= MaxLevel` 恒真 ⇒ 那句 `!can && maxed` 的**两个操作数各自恒定了**。
   本笔**没有**把它简化掉（简化会丢掉「原版那两件的对应关系」这条信息），只加了互斥项并写了注释。
5. 📌 **`NoUpgradeWarn` 这个自检口（`:377`）现在是「永远拿得到一个 invactive 的节点」** ——
   全仓**没有任何断言**用它（现扫）。若 §二·4 那条待裁落成候选 B，它才重新有意义。

---

## 七、改动清单（文件 + 现读行号 + 行尾 + `git diff --numstat`）

**行尾（二进制现数）**：两个文件**都是 LF**（`crlf = 0`），改前改后一致 —— 没翻行尾。

| 文件 | `git diff --numstat` | 说明 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/CardDetailPopup.cs` | `448  42` | ⚠️ **这 448/42 含乙上一轮的 351/39** ⇒ **本笔净增 ≈ 97 / 3**（不是整篇重写） |
| `Unity/MyGame/Assets/CardPresentation/Shell/BoosterPackOpenWindow.cs` | `11  2` | 全是注释（两处「恒真 → 恒假」的就地订正）；**没有一行代码改动** |

### 7·1 `Shell/CardDetailPopup.cs`（现读行号）

| 行 | 改了什么 |
|---|---|
| `:141` | 🔴 **`Level` 的缺省 `1` → `MaxLevel`**（**本笔的核心一行**） |
| `:119-140` | 新增 `Level` 的 doc：改动前后 / 原版判据（`CheckUpgradeRequirements.c:57-60` 两句同源）/ 「没有存档、没有第二个写入点」/ 连带后果 |
| `:43-45` | 文件头补「第二笔」那一行：默认最高级已落到取数口 + 文案已带标识 |
| `:799-820` | `Craft Explanation` 那一行的**新注释块**：记号出处（`BattleDriver.UnimplementedNotes`）、拼接形状（`FaceTextFull:11974`）、为什么英文、为什么数字不加记号、字号是估算 |
| `:822` | 🔴 **文案加 `Not implemented in this build*`**（**本笔的第二处核心改动**） |
| `:896-901` | 原「**两处**如实标注」改成「**一处仍开着**（= §二·4 那条待裁）+ **一处已销**」；①里补上「同 rect 是硬约束」 |
| `:903-914` | ② 就地订正（铁律 5）：把「缺省是 1、本笔不动、已报回」逐句改成「已改成 `MaxLevel`」，并列出改后三格的读数；如实标「满级 Title 无原版判据」 |
| `:926-928` | 那颗钮的 `Debug.Log` 补上「缺省 = `MaxLevel` ⇒ 三格钳在第 4 级那一行」 |
| `:940-961` | 🔴 新增 `No Upgrade Warning` 的**互斥说明**（原版判据三行 + 我们的几何 + 待裁落点 + 一行翻面办法）；把 `SetActive` 的判据改成 `!contentShown && !can && maxed` |
| `:1319-1344` | `OnCraftClicked` 的 doc 改成「用户裁定 = **只出声、不做机制**（候选②已定案）」+ 点名那句文案与它是一件事；`Debug.Log` 末尾补一句指向文案标识 |
| `:1354-1361` | `DoCraft` 的 doc 补「候选②已定案 ⇒ 本方法本版不会被执行」 |

### 7·2 `Shell/BoosterPackOpenWindow.cs`（现读行号）

| 行 | 改了什么 |
|---|---|
| `:76-85` | 文件头「`Card Ready for level up` 的判据」那一格：**就地订正**「恒真」→「**恒假**」，并写明「这是同一裁定的必然、不是新缺陷」+「层别删，将来口径回头它自己就活」 |
| `:616-621` | `ReadyForLevelUp` 的 doc 同上订正（`l < MaxLevel` 第一句就 false） |

**⛔ 没碰**（逐个确认过）：`CardProgress` 的 `Owned`/`AddOwned`/`Spares`/`CanUpgrade`/`DeckCap` 的**实现**（只改了 `Level` 一行）·
`BuildCrafting` 的 `Hit(...)` 那一句 · `BuildAltArt` / `ChangeAltStyle` / 异画那一片 · `Hit(...)` / `Title(...)` / `PanelBox(...)` 的定义 ·
`Editor/**` 四个自检宿主 · `Deck/**` · `Battle/**` · `RuleEngine/**` · `工具/**` · `资料/**`（除本报告）· `Core/CardArt.cs`（只读它）。

**验过**：`TMPDIR=/tmp/wf_bing bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
⛔ **没跑 Unity 批处理**（红线）· ⛔ 没动 git 写操作。
