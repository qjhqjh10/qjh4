# B89 · E17d —— `CurrencyDrawer` 整件（主图 + 名条行 + 小图标 + 数量）

> 执行代理 `E17d` · 2026-10-28 · 第八十九会话（最后一件）
> ⛔ 未跑 Unity / 未跑 `ruleprobe` / 未动 git / 未改账主表（只写 `_parts/B89_E17d.tsv`）/ 未改正本 / 未碰 `d:/2`（只读）。
> 白名单内改动：**新建** `Shell/ItemDrawerCurrency.cs`（`ItemDrawer` 的第三个 partial 文件）· `Shell/ItemDrawer.cs`（**2 行就地改写**）· `Shell/CurrencyNames.cs`（新增 `Small`）。

---

## 〇 一句话

按 `menu_dump` 的整棵子树**逐项**实现了原版 `CurrencyDrawer`（含 `HorizontalLayoutGroup` 的**真居中**，不是按静态位贴），
并把 `Icon` 抽屉改成「认出是货币就转交」。**`ItemDrawer.cs` 仍是 2152 行、`check_ledger_points 商店` 漂移 0**。
玩家可见的变化只有一处：**战役奖励窗里 `C2`（水晶）那 8 个格子**从「居中方块主图 + 数量」变成原版的四件套。

---

## §1 几何：逐项，一个数都不是目测的

判据 = `python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Currency Drawer"`
（**prefab 局部坐标 · 1920×1080 设计基准 · 左上原点 · y 向下**）：

| 节点 | 矩形 | 属性 |
|---|---|---|
| `Currency Drawer` | `603.6,0 → 1316.4,1080`（712.8×1080） | `AspectRatioFitter` **高控宽 0.66** |
| `Content` | `604,0 → 1316,1080` | |
| `Content/Background` | 同 `Content` | `40k_Rewards_icon_base card` · Simple · **非** preserveAspect |
| `Content/Image`（主图槽） | `604,0 → 1316,864` | **`×0.9` 绕中心 ⇒ 视觉 640.8×777.6** · preserveAspect |
| `Content/Label`（名条行） | `639.6,810 → 1280.4,918`（640.8×108） | `40k_main_bt_nametag` 拉伸铺满 ＋ `HorizontalLayoutGroup`：**spacing 10 · padding (25,25,0,0) · align 4 = MiddleCenter** |
| `Content/Label/Icon`（**小图标槽**） | `901,810 → 1009,918`（**108²**） | preserveAspect |
| `Content/Label/Quantity` | `1019,810 → （宽由布局给）` | `fs 100` · 基准 `36` · auto `12~100` · **Left/Midline** |

同一份 dump 里的 `Converted Drawer` / `Ephemeral Drawer` / `Collected Badge` / `Premium Highlight`
**出厂 `m_IsActive = 0`**，且**不是 `Draw` 这一跳的件** ⇒ **本件不建**（⛔ 没顺手补），它们归 `ItemDrawer` 那三跳（A311）。

---

## §2 🔴 坐标口径怎么换的（你点名要先说清）

分两层，别混：

1. **dump 的数 ↔ 我们的 `PxRect`**：**同一口径，不需要换算** ——
   `menu_dump` 打的就是「1920×1080 设计基准 · 左上原点 · y 向下」的绝对矩形，
   而我们 `PxRect(x1,y1,x2,y2)` 也是这个口径（佐证：`ShopTabPage.ScrollView = (329.76,127.62,1920,1080)` 与
   `menu_dump` 打出来的 `Packs Scroll View` 逐字相同）。
   ⚠️ **没有**从 `runtime_ui_dump_*.tsv` 取任何数（那张表**表头列名错位**：写的 `rect/pos/pivot`
   实际是 `anchoredPosition/sizeDelta/anchorMin`，本项目踩过）。
2. **prefab 局部 ↔ 每个格子的 `box`**（这层**必须**换算，因为同一个抽屉会被塞进 200×300 之类的格子）：
   - 尺度 `k = min(box.H, box.W / 0.66) / 1080`。`0.66` = `AspectRatioFitter` 的「高控宽」比（`712.8/1080`）。
   - 原点 `ox = box.CX − 712.8k/2`、`oy = box.CY − 1080k/2`（水平垂直都居中）。
   - 每个矩形 = `(ox + (x1 − 603.6)·k, oy + y1·k, ox + (x2 − 603.6)·k, oy + y2·k)`。
   > 🔶 **自己设计（非原版）**：`min(box.H, box.W/0.66)` 里的那个「容器更窄时按宽缩」是**兜底** ——
   > 原版 ARF 在容器窄于抽屉时怎么缩，**没有判据**（没读）。容器够宽时与原版逐像素相同（常态）。
   > 代码里已在那一行按铁律 18 打了 `🔶`。

---

## §3 逐件实现（`Shell/ItemDrawerCurrency.cs`）

| 件 | 怎么画的 | 判据 |
|---|---|---|
| 背景 | `40k_Rewards_icon_base card` 铺满 `Content` | **图不在 `Resources/`** ⇒ **不画 + 出声一次**（源图在 `Art/原版/0_mainmenu/40k_Rewards_icon_base_card.png`，要跑 `工具/import_original_art.py --only-menu`）。⛔ 没拿别的图冒充 |
| 主图 | `item.Art` 铺 `Content/Image` 槽再 `×0.9`（绕中心）、preserveAspect | dump 的 `×0.9 → 视觉 640.80×777.60` |
| 名条行 | `Content/Label` ＋ `40k_main_bt_nametag`（**拉伸**，非 preserveAspect） | dump 那一行的 `Simple (1,1,1,1)` |
| 小图标 | `CurrencyNames.Small(code)`：`C2→40k_topmarquee_currency_crystal` · `C3→…_blackstone` · `C5→…_ticket`（**guid 链**，见 `CurrencyNames.cs`） | 108²、竖向铺满名条行、preserveAspect |
| 数量 | `fs 100 / 基准 36 / auto 12~100 / Left·Midline / 白` | dump 那三格 |
| **HLG 居中** | 先建数量**量渲染宽** ⇒ 组宽 = `108k + 10k + qtyW` ⇒ 起点 = `名条左边 + 25k + max(0,(内宽−组宽)/2)` ⇒ 建小图标、再把数量 `AlignLeft` 到「图标右边 + 10k」 | `spacing 10 · padding 25,25,0,0 · align 4 (MiddleCenter)` —— **所以数量串变长时小图标会跟着往左移**（与 dump 里 `Quantity` 宽 0 时图标在 901 的静态位一致） |
| 主图取不到 | 退 `Placeholder` + `res.MissingArt` | 与通用 `Icon` 抽屉同一条退路 |

**接线（零行数变化）**：`Shell/ItemDrawer.cs:1297` 那一行改成
`var tex = Tex(item.Art); if (CurrencyNames.Of(item.Id) != null) { DrawCurrency(...); return; }`
—— 货币的 `Kind` 是 `Generic` ⇒ 落到 `Icon`，这里转交；`Refresh()` 重放走 `Dispatch → Icon` ⇒ 同样转交（不会漏）。
`ItemDrawer` 本来就是 `public static partial class` ⇒ 新件放进**新文件**即可，⛔ 不需要在 `ItemDrawer.cs` 里加行。

---

## §4 🔴 哪些格会**多出内容**（玩家看得见的变化）

| 位置 | 改前 | 改后 |
|---|---|---|
| **战役奖励窗**（`CampaignRewardWindow`）里 id = `C2` 的格 —— 现读共 **8 处**：`UM5` / `UM6` / `UM12` / `UM22` / `UM28` / `UM29`（两个档各一）/ `UM39` | 居中方块主图（`IconFill 0.7`）＋ 右下数量 | **四件套**：名条行（`40k_main_bt_nametag` 底）＋ **小图标（水晶）**＋ 数量（挪到名条行右侧、`fs100` 档）＋ 主图缩到上方 80% 且 ×0.9 |
| `C3` / `C5` | 同「改前」 | 同上（但**现读这两档没有出现在任何奖励表里**） |
| 商店格 | 不走抽屉（`ShopOffer.Art` + `MenuDraw.Rect`） | **不变** |
| 其余物品（`Wildcard` / 卡包 / 图名 id 的 `Grants`） | —— | **不变**（`CurrencyNames.Of` 只认 `C2/C3/C5`） |

⇒ **唯一受影响的位面 = 战役奖励窗那 8 个 `C2` 格**；这一点必须让真机验收时看一眼（背景层不画 ⇒ 那 8 格后面是空的，
原版那里有一张 `40k_Rewards_icon_base card` 底板 —— **这是本批唯一「看着会不一样」的地方**，原因见 §3 第一行）。

---

## §5 观测口（**已落进宿主**）

**读数口**：`ItemDrawer.LastCurrency`（`ItemDrawerCurrency.cs`）——
```csharp
public struct CurrencyParts {
    public bool Background, MainImage, Nametag, SmallIcon, Quantity;
    public PxRect LabelRow, SmallIconRect, QuantityRect;   // ← 后两个是 HLG 排完的**实测**值，不是常量
}
public static CurrencyParts LastCurrency;
```

**断言（已落 `Editor/ShopScene.cs:1917` 起，新 `Section("货币抽屉…")`，共 8 条）**：
探针挂在**整屏矩形**的节点上（`MenuDraw.Node(root, "CurrencyDrawerProbe", (0,0)-(1920,1080))`）⇒ 下面的 px 就是设计基准绝对坐标；
格子取 `200×300 @ (860,390)-(1060,690)` ⇒ `k = 300/1080 = 0.277778`、原点 `(861,390)`：

- ① `LastCurrency.MainImage` / 前提 `Nametag` / ② `SmallIcon` / ③ `Quantity`（四条 bool）
- `CheckAt(Content/Label, 871.0, 1049.0, 615.0, 645.0)` —— 名条行矩形
- `CheckNear(SmallIconRect.W/H, 30.0, 0.5)` —— 小图标边长 = `108 × 0.277778`
- `SmallIconRect.x2 ≤ QuantityRect.x1` —— HLG 的 `spacing 10` 排布（顺序写反就红）
- 两件都在名条行内部（不溢出到主图上）

上面那些**绝对数全是手算的**（`861 + (639.6−603.6)×0.277778 = 871.0` · `390 + 810×0.277778 = 615.0` · `108×0.277778 = 30`），
**不是拿同一套公式再算一遍**（那就成自证了）。
🧨 改坏法：删 `CurrencyNames.Small` 的 `C2` 那支 ⇒ ②红；删 `ItemDrawer.cs:1297` 那句转交 ⇒ ②③ 红（退回通用 `Icon`：没有名条行/小图标）；
把 HLG 的 `spacing` 写成 0 ⇒ 顺序那条红。⚠️ 主图 `×0.9` 去掉**不会**让现有断言红（名条行仍对）—— 那条只能靠并排看，已写进 §7。

---

## §6 读数

| 项 | 读数 |
|---|---|
| `TMPDIR=/tmp/wf_B89E17d bash 工具/typecheck.sh`（最后一跑） | **编辑器程序集 0** ✅（⇒ §5 那 8 条断言**真编过了**，不是「塞进去没验」）· **运行时 2 条，全在 `Assets/CardPresentation/Shell/MainMenuRuntime.cs`**（`ModeCardOwnerEvent` / `EventNavTitleOf` —— `Shell/**` 其它 = E16c/E22/R6/E27/R3e 的活，**不是我的**）。⚠️ 这一轮运行时的错**一直在换文件**（先 `BattleDriver.cs` 十来条 → 1 条 → 清 0 → 现在 `MainMenuRuntime.cs` 2 条）⇒ 是别人在写，我改的 4 个文件**一次都没上过错误清单** |
| 编辑器能编的那次 | 第一次跑（加断言**之前**）运行时 0/0 **双绿**；加断言那次编辑器 0 ⇒ 断言通过了类型检查 |
| `check_ledger_points.py 商店` | A 124 · 通过 124 · **漂移 0** · 告警 1 · 失败 0 |
| `Shell/ItemDrawer.cs` | **仍 2152 行**（2 处同行改写：`:1297` 转交 + 4 处数量闸在上一批）· `ItemDrawerCurrency.cs` **187 行**（新）· `CurrencyNames.cs` **109 行** · `Editor/ShopScene.cs` 5961→**6010**（+49，无 A 档落点 ⇒ 不产生漂移） |
| 行尾 | 四个文件全 **LF**、`bareCR = 0`（二进制核） |

---

## §7 还差（如实记，⛔ 没有「影响小不做」）

1. **背景图**：`40k_Rewards_icon_base card` 需跑一次导入腿（`工具/import_original_art.py --only-menu`）才会进 `Resources/`。
   在那之前那 8 个 `C2` 格**没有底板**（其余四件都对）。这不是判据缺口，是**资源导入腿**。
2. **`Converted Drawer` / `Ephemeral Drawer` / `Collected Badge`** 三块（出厂 INACT）本件**没建** —— 它们归
   `ItemDrawer` 那三跳（`SetConvertedItem` / `SetEphemeralDisplay` / `ToggleClaimedWarning`，A311）。
   ⚠️ **一个待核项**（本批没查）：那三跳现在挂在**调用方传进来的 `res.Node`** 上，而原版它们是 `Content` 的子节点；
   本件多了一层 `Content` ⇒ 若那三跳真的在战役奖励链上被用到，节点层级会与原版差一层（**不影响画面，影响取件路径**）。
3. **`Icon Currency Drawer Variant`**（商店那档，override 10）的 `label`/`quantity`/`icon` **全是 null** ⇒
   那一档原版**只有主图**。我们商店格不走抽屉 ⇒ 今天无偏差；若将来切到抽屉体系，**要按「只有主图」实现**（本批未做）。
4. **主图 `×0.9` 那条没有断言**（删掉它断言仍绿，只有并排看才看得出来）—— 按铁律 10 第 6 条，**真机/并排比**是这一条的验收手段：
   挑一个 `C2` 格看主图有没有压到名条行上。
5. **`ItemDrawer.LastCurrency` 是静态读口**：批处理里连画多件时读的是**最后一次** ⇒ 断言必须「画完立刻读」（§5 就是这么写的）。
