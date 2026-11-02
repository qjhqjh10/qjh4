# B117-WCARDTIER —— 开包/轮抽卡面档 + 开包翻牌后点击进详情

> 执行代理 `B117-WCARDTIER` · 2026-11-02 · 回报给调度台
> 简报里的两条活：`B117-52`（`CardUse.Booster` 没传）· `B117-55`（开包翻点卡进详情未接）
> ⚠️ 本文件是**判据 / 证据**，状态看 `活状态表`。

---

## 零、一句话结论

| 条 | 结果 |
|---|---|
| `B117-52` | ⚠️ **只改了 1 处，不是简报说的 5 处** —— 逐实例现读 prefab 后：**只有轮抽「选将格」该传 `CardUse.Booster`**；简报点名的另外 4 处**原版就是标准档 / 根本没卡面**，照着改会把正确的改错。 |
| `B117-55` | ✅ **早就是接好的**（2026-10-17 · T19 收尾）—— 只剩文件头那 4 行「没复刻（缺口）」是**过期说法**，已就地订正（铁律 5）。补了 7 条断言把它钉住。 |

---

## 一、`B117-52`：`CardUse.Booster` 只该给轮抽「选将格」

### 1·1 判据怎么取的（**逐实例 walk，不是按名字猜**）

`d:/2/新解包资源/assets_full/bundle_menus_assets_all`，从每棵树的 `CardUI` / `CardUI Reference` / `CardUI-Warlord`
RectTransform 往下 walk（**含「纯 Transform 转发节点」底下那两支** —— `menu_dump.py` 在纯 Transform 处不下钻，
会把 `Card Info`（19 个子件）与 `Textbackgrounds`（2 个）整支丢掉，所以那 4 支 `.json` 是绕开它直接读的）。

| 宿主（原版节点路径） | `NameTextUnit` | `DescTextUnit` | `RaceText`（兵种） | `ArmyTextUnit`（阵营） | 归哪档 |
|---|---|---|---|---|---|
| `Draft Mode Menu Demo / Draft Mode Select Warlord State / Generic Card Selector Menu / Viewport / Content / Generic Select Card Container / Content / CardUI` | `16.3 / **max 20**` | `19 / 19` | `17 / 17` | `17 / 17` | **Booster** |
| `Booster Pack Open Window / Booster Animation Parent / Cards / CardInBoosterPack UI n / Content / CardUI` | `16.3 / **max 20**` | `19 / 19` | `17 / 17` | `17 / 17` | **Booster**（已有自动识别） |
| `Draft Mode Menu Demo / Generic Multi Card Display / Content / CardUI Reference`（包预览） | `19 / 19` | `23.55 / 24` | `18 / 18` | `15 / 15` | **Standard** |
| `Draft Mode Menu Demo / Draft Mode Deck Info Panel / Energy View panel / content / CardUI-Warlord`（右栏） | `19 / 19` | `23.55 / 24` | `18 / 18` | `15 / 15` | **Standard** |
| `… / Catalog Item Shop Container / Content / Viewport / Packs Scroll View / Card Shop Tab / … / Card Drawer / Content / CardUI`（商店卡抽屉，5 份逐份同值） | `16.0 / max **19**` | `23.55 / 24` | `18 / 18` | `15 / 15` | **Standard**（有效上界与标准档同） |

> 🔴 **有效字号看 `m_fontSizeMax`，名义 `m_fontSize` 只是 autosize 起点**（TMP autosize 从 `clamp(base,min,max)`
> 一路涨到 max）。⇒ 商店那 5 份 `16.0 / max 19` 的**天花板就是 19 = 标准档**，不是开包族。
> 这一条 `B106_W9` 的 §二·3 第 2 点已经写过（审计表把 `16` 当有效值 ⇒ 判「商店比原版大 18.75%」**不成立**），
> 本轮**独立复核，结论一致**。

### 1·2 为什么简报说的 5 处里 4 处不能改

| 简报点名 | 原版它是什么 | 结论 |
|---|---|---|
| `DraftModeWindow.cs:1391`（选将格） | `Generic Select Card Container/Content/CardUI` = **Booster** | ✅ **改** |
| `DraftModeWindow.cs:2076`（包预览） | `Generic Multi Card Display/Content/CardUI Reference` = **Standard** | ⛔ 不改 |
| `DraftModeWindow.cs:3093`（右栏战将） | `CardUI-Warlord` = **Standard** | ⛔ 不改 —— `B106_W9` §二·3 第 2 点**专门警告过**「⛔ 别把它套 Booster」 |
| `ItemDrawerCard.cs:124`（商店卡抽屉） | `Card Drawer/Content/CardUI` = `16.0/max **19**` ⇒ 与标准档**同界** | ⛔ 不改 |
| `DraftModeWindow.cs:2518`（轮抽「包内三张卡」） | 原版那里**没有卡面** —— `Prebuilt Pack UI` 的 `cardsContainer` 装的是 `cardInfoItem`（= `UICardInfoItem` 行：卡名 / 费用 / 稀有度小图标），`PrebuiltPackUI.Initialize` 逐张 `UICardInfoItem.Initialize(card, 0, 1, …)`（判据 = `S3_轮抽窗原版规格.md` §2.6 引的 `decomp_full/PrebuiltPackUI__*.c`） | ⛔ 不改 —— 我们那三张卡面**本身**就是自设计的替身，原版**没有**对应的档位判据；保持出厂 `Standard`，⛔ 不拿「轮抽 ∈ 开包族」这句话外推 |

**为什么选「显式传 `use:`」而不是「把宿主名加进自动识别白名单」**：
自动识别的判据是**原版宿主节点的名字**（现在是 `Booster Animation Parent`），而轮抽选将那一格挂在
`Generic Select Card Container / Content` 下 —— 这个名字在两扇窗里**都出现过**，
拿它当认档键会**误伤**（`Content` 是个到处都有的通用名）。⇒ 显式传档，改动面 1 行、判据最直接。
（`B106_W9` 的 §二·3 收尾给商店/轮抽写的就是这条补丁形状。）

### 1·3 改了什么

`Unity/MyGame/Assets/CardPresentation/Shell/DraftModeWindow.cs:1391`（**行数不变**）

```csharp
// 旧
var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI");
// 新
var cv = CardView.Create(content, BattleDriver.ToCardData(def, def.Faction), "CardUI", CardFace.Full, null, CardUse.Booster);
```

同文件 `:1385`（**已有注释行就地加写，行数不变**）补了判据：哪 4 份 prefab 是 Booster、哪两份**不是**。

---

## 二、`B117-55`：开包翻牌后点卡进详情 —— **已经接了**

### 2·1 现读（全在 `Shell/BoosterPackOpenWindow.cs`）

| 落点 | 内容 |
|---|---|
| `:756-761` `OnSlotClick(int i)` | `Ready(2)` → `Reveal(i)`；**`Open(5)` → `OpenCardDetail(i)`**；其余状态**什么都不做**（原版两个 handler 各自的状态守卫） |
| `:771-781` `OpenCardDetail(int i)` | `CardDetailPopup.Create(Manager)` + `Manager.OpenWindow(w)` + `w.ShowCard(Cards[i])`（= 原版 `CardInBoosterPack.CardClicked` → `CardDisplayWindow.ShowCard(card, …, true, …)`；菜单场景那扇 `CardDisplayWindow` 就是我们这扇 `CardDetailPopup`） |
| `:826-828` `Reveal` 里 | 「命中区**不再关掉**」—— 原来那句 `SlotHits[i].gameObject.SetActive(false)` **已删** |
| `:1037-1039` `QuickOpenAll` 里 | 同上（那句也删了） |
| `:707-720` `BuildSlot` | 每格一颗 `Hit`（带 `ImageQuad`，`PointerLayer.CollectHits` 才认得出），`onClick = () => OnSlotClick(i)`，`playClickSound = false`（那一下的声是 `clickSound`，在 `Reveal` 里播） |

⇒ **命中区是开着的、点击也分发到详情窗了**。缺的只是**文件头那一行说法**。

### 2·2 改了什么

`Shell/BoosterPackOpenWindow.cs:89-92`（**4 行 → 4 行，行数不变**）：把「**没接**（缺口）」那段换成
「✅ 已接（2026-10-17 · T19 收尾）」+ 现行机制 + 落点行号，并按铁律 5 保留「原写 … → 不成立」的痕迹。

---

## 三、断言

新建 `Assets/CardPresentation/Editor/B117_WCARDTIER_Assert.cs`（**未接线**，由调度台代接到宿主）。
**20 条 `Check` 调用**（A 段 12 · B 段 8）：

| 段 | 条 | 盯什么 | 怎么才会红（灭自证） |
|---|---|---|---|
| 接线前置 | A0/A0-b/A0-c/A0-d、A1-b、B0、B0-b、B5前提 | 前提硬检查（⛔ 不写成 `if (前提) Check(结论)` 那样静默跳过） | —— |
| A | **A1** | 选将格 `CardView.Use == CardUse.Booster` | 去掉 `use:` 实参 ⇒ 回 Standard |
| A | **A2–A5** | 卡名 / 效果 / 阵营 / 兵种四行折回**原版 pt** = `20 / 19 / 17 / 17`（用标准档那一格当尺子，非自证） | 同 A1；A4/A5 **方向相反**（17>15、17<18）⇒ 靠整体缩放满足不了 |
| A | **A6 / A7** | 包预览宿主（`Generic Multi Card Display/…/CardUI Reference`）与右栏（`CardUI-Warlord`）**必须 Standard** | 把 `IsBoosterCardView` 扩成按这两个名字认档 ⇒ 红 |
| B | **B1** | `Ready` 点卡 ⇒ 进翻牌 | `OnSlotClick` 的 Ready 分支被删 |
| B | **B2** | 推时间后 = `Open(5)` | 翻牌链断 |
| B | **B3** ★★ | 翻完之后 `SlotHits[0].gameObject.activeSelf` **仍为真** | 在 `Reveal`/`QuickOpenAll` 里加回 `SetActive(false)` ⇒ 红（= 本条缺口原状） |
| B | **B4** | `Back(1)` 状态点卡什么都不做 | 去掉状态守卫（无脑 `Reveal`） |
| B | **B5** ★★ | `Open(5)` 再点 ⇒ 场景里真的多出一扇 `CardDetailPopup` | 删掉 `OnSlotClick` 的 Open 分支 |

⚠️ **A6/A7 用的是「合成宿主」不是真窗节点**：真窗里那两格要先 `ChooseWarlord` → `ContinueAfterWarlord` →
`ShowPackPreview` 才建得出来，链路长、易受别的断言影响（`DraftPacks.SetPacksForTest`）。
用原版宿主**节点名**建卡能起到同一道护栏（`IsBoosterCardView` 一旦被扩名字就红），而且稳定。

---

## 四、验了什么 / 没验什么

- ✅ `TMPDIR=/tmp/wf_wcardtier bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**。
- ✅ `git diff --numstat`：`DraftModeWindow.cs` **2/2**、`BoosterPackOpenWindow.cs` **4/4**（**行数守恒**，
  两文件行数与 `HEAD` 逐字节相同：4849 / 1814）。
- ⛔ **没跑 Unity 批处理**（本代理禁跑）⇒ 断言**没执行过**，只是编译通过；接线由调度台做。
- ⛔ **没做真机**：`CardDetailPopup` 那一扇真开出来没有、字号肉眼对不对，留真机验收。

---

## 五、没做完 / 要调度台定

1. 🔴 **`DraftModeWindow.cs:2518`（轮抽「包内三张卡」）目前画的是卡面，原版画的是 `UICardInfoItem` 行。**
   这是一条**结构级的自设计**（不是档位问题），本代理没动：它不在 `B117-52` 的范围里，而且改它等于重做那一层。
   **建议另立账**：`Prebuilt Pack UI / Cards container` 的三张 = `UICardInfoItem`（`cardName` / `cardCost` / `cardRarityHolderController` …），
   `PrebuiltPackUI.Initialize` 按 `pack.GetCardLibrary()` 的卡数实例化。判据在 `S3_轮抽窗原版规格.md` §2.6。
2. ⚠️ **`判据/B117_G11_卡牌展示场景矩阵.md` §二·1 那条描述偏粗**（写成「轮抽 / 商店……都没传 ⇒ 都错」）。
   按铁律 5 的惯例应由调度台**就地订正**那一行（⛔ 本代理不在白名单里改它）——建议改成：
   「**只有轮抽选将格**缺 `Booster`；包预览 / 右栏战将 / 商店卡抽屉**本来就是标准档**（逐实例见本文 §1·1）」。
3. ℹ️ `CardView.Create` 签名上方那段 `<param name="use">` 的文档仍只提 `ZoomWindow` / `MenuDetail`，
   没提 `Booster` 与自动识别 —— `B106_W9` §④·4 已记过，本轮**没碰**（改它会动行号）。
