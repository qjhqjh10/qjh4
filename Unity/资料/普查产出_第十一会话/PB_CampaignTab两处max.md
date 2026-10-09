# P-B · `CampaignTab` 两处 autosize 上限（`A1206`）

**日期**：2026-10-10 · **执行代理**：P-B · **白名单**：`Shell/CampaignTab.cs`（唯一文件）

---

## 一、一句话结论

`Shell/CampaignTab.cs` 那两颗漏网的 autosize 上限已改回原版值
（`_title` `31.75 → 35`、`_points` `34.8 → 40`），现场复核原版 bundle 逐值吻合，
类型检查 `0` 错、行尾仍是纯 LF；**本件另发现：这两格目前【没有任何断言覆盖】，改了不会红也不会绿**（见 §五）。

---

## 二、逐处改动清单

| # | `文件:行号` | 改前 → 改后 | 判据出处 |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Shell/CampaignTab.cs:211` | `autoMaxPx: 31.75f` → **`autoMaxPx: 35f`** | 原版 `Campaign Tab ▸ Campaign Header ▸ Title` = `字号 31.75 · 基准 36.0 · auto[25.0~35.0] · 折行=0` |
| 2 | `…/CampaignTab.cs:244` | `autoMaxPx: 34.8f` → **`autoMaxPx: 40f`** | 原版 `… ▸ Campaign Header ▸ Points` = `字号 34.8 · 基准 36.0 · auto[18.0~40.0] · 折行=0` |

**两处的 `min` / `base` 未动**（已是原版值：`25/36` 与 `18/36`）。⛔ 没有顺手改别的格。

### 判据是怎么取的（**本件现场重量过，不是照抄报告**）

```
python -I d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Rewards Base Submenu Variant" --depth 8
```
实读原文（`/tmp/rb.txt` 本次 dump）：

```
4          Campaign Header   …  CampaignHeaderDisplay,Image   … 字段: alreadyPremium,armyIcon,pointArmyIcon,points,premiumButton,…
5            Title      …  TextMeshProUGUI  'ULTRAMARINES' 字号=31.75 基准=36.0 auto[25.0~35.0] 对齐=Left/Bottom 折行=0
5            Points     …  TextMeshProUGUI,ContentSizeFitter⚙CSF h:PreferredSize？  'Points: 69' 字号=34.79999923706055 基准=36.0 auto[18.0~40.0] 对齐=Left/Middle 折行=0
```

⇒ **两格上限 = 35 / 40，与 `R5_包装层菜单族.md` §3 注③ 给的值逐位一致**（`R5` 独立读了一遍，本件又读了一遍，两路同值）。
`R5` 的原话：「我们 `autoMaxPx: 31.75`，原版 `= fs31.75 · base36 · **auto[25~35]**` ⇒ 上限应为 **35**」
/「我们 `autoMaxPx: 34.8`，原版 `= fs34.8 · base36 · **auto[18~40]**` ⇒ 上限应为 **40**」。
口径坐实见同文件 §6 隐患 2「两处『接上了、值只对三分之二』」。

### 顺带写进代码的注释

两处调用点下方的 `A305①` 注释块里原来各有**一句「那是另一条（`A333`），本轮不动」**——
现改成 **`✅ 2026-10-11（A1206）已改`** + 新值 + 判据 + 为什么当初会漏
（「只判实参非 0」抓不到的「形状对、值不全对」，同族的 `panTitle` 在 `A274` 已修、这两颗漏网）。
⛔ 没删任何既有注释（`A303②` 折行那段、`A205` 顺序那段都原样留着）。

---

## 三、验证

| 项 | 读数 |
|---|---|
| 类型检查（`TMPDIR=/tmp/wf_pb bash d:/4/Unity/工具/typecheck.sh`） | **运行时程序集 0 错 · 编辑器程序集 0 错**（一次过，无「别人半成品」的假错） |
| `git diff --numstat -- …/CampaignTab.cs` | **`12  4`**（≈1127 行文件里的 12/4 ⇒ **没有翻行尾**） |
| 行尾实测（二进制读） | `CRLF 0 · LF 1135 · 92972 B` ⇒ **仍是纯 LF**，与派活简报给的一致 |
| 改动内容复核 | `git diff` 逐行看过：只有 **2 行实参 + 2 段注释**，无越界改动 |
| 未跑 Unity | ✅ 遵守红线（不 `-executeMethod`、不 `_run_8_checks.sh`）；本件宿主是 `RewardsScene.Run`，按铁律 12 由调度台在同步点跑 |

---

## 四、没查清 / 停手

**没有停手项。** 两处都改完、判据两路同值、类型检查过。

- ⚠️ 本件**没有跑** `RewardsScene.Run`（红线）⇒ 「改完画面还对不对」这一层**未验**。
  可预期的影响：`Title`/`Points` 在框宽不够时**可多长到 3.25px / 5.2px** 才缩字号；
  两者原版都带 `ContentSizeFitter`（`Points` 是 `CSF h:PreferredSize`）⇒ 框本来就由内容撑开，**不折行**（两处 `折行=0` 我们也已还原）。
- `A1206` 与既有账里的 **`A333`** 是同一类（上限抄成标称）：全库另有 `A333` 的落点
  （`Core/FilterPanelModel.cs` 多处 · `Battle/CardDisplayWindow.cs:792` · `Battle/Label.cs:810` · `Shell/MenuWindowBase.cs:332/358`），
  **不归本件**，只是登记别让下一轮把这颗当新账。
- 📌 **日期口径的一处不一致（留痕，供调度台裁定）**：本机系统日期是 **2026-10-10**，
  而**同一文件里邻近的注释块**（`A305①` / `A303②`，`Shell/CampaignTab.cs:215/243/246`）与本批的
  `资料/普查产出_1011/`（**64 处**）都写作 **`2026-10-11`**（该目录名也是 `1011`）。
  ⇒ 我写注释时**跟着同文件的既有口径用 `2026-10-11`**（同一批人、同一轮），
  ⛔ 没有去改别人的日期；**若这一轮的真日期是 10-10、10-11 是笔误，请调度台统一订正**（本件只标不改）。

---

## 五、🔑 顺手发现（⛔ 只报不改，由调度台分流）

### 1. 🔴 **这两格改了：一条断言都不会红、也不会绿** —— 没有任何断言覆盖它们的 autosize 窗口

`Editor/RewardsScene.cs` 里与这两个节点有关的断言只有两条，**都不是窗口**：

| 断言 | 断的是什么 | 覆盖 `min/max/base` 吗 |
|---|---|---|
| `RewardsScene.cs:10219-10235`（`A303②`） | `Campaign Header/Title` 与 `/Points` 的**折行 = 0** | ❌ 只读 `WrappingMode` |
| `RewardsScene.cs:6813` | `Campaign Header/Title` 的**左边缘 = 480.69** | ❌ 只读 `TextLeftPx` |

⇒ 也就是说：**`A305①` 补 `autoBasePx`、本件补 `autoMaxPx`，四格（min/max/base/折行）里有三格至今没断言兜着**。
对照先例 —— 同页的 `Premium Panel/Title` 在 `A274` 已经拿了**一条专属的** `CheckFontWindow`：

```csharp
// Editor/RewardsScene.cs:6806
CheckFontWindow(cpan2, "Title", 10f, 40f, "★ `Premium Panel/Title`（`Premium Campaign daily bonus`）的自适应窗口 …");
```
（那条的改坏法都写好了：「把那行的第 4 个实参改回 `33.3f` ⇒ 这条红」）

**建议**（不属本件白名单，`Editor/RewardsScene.cs` 有人占着）：
给 `Campaign Header/Title`（`25 / 35 / 36`）与 `Campaign Header/Points`（`18 / 40 / 36`）各补一条
`CheckFontWindow`（该助手在 `RewardsScene.cs:10859`，读的是 `Label.FontSizeMin/Max` **真字段**，不是我们的实参
—— 正是「灭自证」要的那一口）。⚠️ 注意 `CheckFontWindow` 内部是 `FindChild` + `GetComponentInChildren<Label>()`
（**单参**、**递归**），树里同名 `Title` 不止一颗（`Campaign Header/Title` · `Premium Panel/Title`）
⇒ 得先确认从哪个父节点起找（`cpan2` 那条之所以没串味，是因为它从 `Premium Panel` 起找 —— 别照抄成从 `Campaign Tab` 起找）。

### 2. `CampaignTab.cs` 全文件的 autosize 调用点**只有三处**，现在三处全对

```
:211  _title   autoMinPx: 25f · autoMaxPx: 35f · autoBasePx: 36f   ← 本件改（原 31.75）
:244  _points  autoMinPx: 18f · autoMaxPx: 40f · autoBasePx: 36f   ← 本件改（原 34.8）
:942  panTitle autoMinPx: 10f · autoMaxPx: 40f · autoBasePx: 12f   ← A274 已修，本次复核仍是原版值
       （原版 `Title`（Premium Panel 底下那一颗）…= `m_fontSizeMin 10` · `m_fontSizeMax 40` · `base 12`，见 `:947` 注释）
```
⇒ **本文件内「上限抄成标称」这一族已经清零**，没有第四颗漏网。

### 3. 本页原版还有一颗 autosize 的 TMP，我们**有意不建**（不是缺陷，登记免得下轮当缺口核）

`Campaign Tab ▸ ChooseArmyText`（节点挂在 `ToggleChooseArmyText` 上）原版实读：
`字号=35.0 · 基准=36.0 · auto[25.0~35.0] · 折行=1`（⚠️ `⇲ls=0.5,1`、视觉框 ×2 ⇒ 618.15×102.71）。
它的窗口**恰好与 `Campaign Header/Title` 同值（25/35/36）**，但**折行相反**（1 vs 0）。
我们**不建**它是**已登记的主动决定**（`Shell/CampaignTab.cs:11` + `Editor/RewardsScene.cs:4938`：
「`ToggleChooseArmyText` **全库 0 调用者** ⇒ 一律不建，改由教程线做」）⇒ **不列为缺口**；
但**将来若做教程线要补这一颗**，四格照上面这组取（别顺手抄 `Title` 的 `折行=0`）。

### 4. 同一棵 `Campaign Header` 底下还有两颗我们**有意不建**的（原版也 `INACT`）

`Debug Point Button`（及其 `Button Text` `'Change Deck'`，`字号=36.0 · 基准=12.0 · auto[10.0~36.0]`）
与 `Premium Button Container` —— 原版节点带 `INACT`，我们**不建**，
且 `Editor/RewardsScene.cs:4933/4935` 已有「不建」的断言（`FindChild(...) == null`）⇒ 与非缺口一致，无账。

### 5. `A333` 是个「一类」不是「一颗」

除本件外，`A333` 还落在 `Core/FilterPanelModel.cs`（多处，2026-10-12 已按族修）、
`Battle/CardDisplayWindow.cs:792`（`min` 那格**如实记着没动**）、`Battle/Label.cs:810`、
`Shell/MenuWindowBase.cs:332/358`（兜底 `autoMaxPx <= 0 ⇒ fontPx` 的口径）。
⇒ 调度台若要盘「这一类还剩几处」，**别按 `A1206` 单号找**，得按「`m_fontSizeMax` ≠ 我们传的实参」这个形状搜。
