# WD3 · 领奖弹窗补建（A516–A520）

> 执行写手：**2026-10-13（批次 4）** · 独占文件：`Shell/DailyStreakPopup.cs` · `Shell/DailyRewardPopup.cs`（**只动了这两个 + 本报告**）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只用只读 `git status` / `git diff --numstat`）· ⛔ 没改两张正本 · ⛔ 没越白名单
> ✅ 类型检查**改完立刻**跑过两次：`TMPDIR=/tmp/wf_wd3 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（§十一）
> 📌 本报告的「实测」= `工具/menu_dump.py`（本件亲跑 4 次：两窗 × 带/不带 sprite）· `工具/menu_rect.py`（亲跑 2 次）· 解包 prefab 的 `RectTransform/*.json` / `MonoBehaviour/*.json` 亲读。
> 🔴 **起点纪律**：一切以【原版 dump + prefab 字段】为准；⛔ 没有拿我们自己的常量当过期望值。

---

## 一、结论

| 账 | 状态 | 一句话 |
|---|---|---|
| **A516**（奖励窗 `Timer` 少一颗） | ✅ **做完** | `DailyRewardPopup.BuildTimer` 补建 `EverguildTextMeshPro`（'More Rewards In'，**节点名照原版**），`Right/Capline` · `fs 50` · 框 `245.50 990.79 → 935.00 1054.15`（= 我们已有的 `TimerMore`，本件新增常量）。**画在时钟图之前**（原版兄弟序）。三方证据逐条对上（§二）。 |
| **A517**（字距 5，3 颗） | ✅ **做完 · 「为什么只有这 3 颗」也查出来了** | 3 颗各补 `SetCharSpacing(5f)`，**排在 `MenuDraw.AlignLeft` 之前**（A475 那个坑）。🔴 「为什么只有这 3 颗」**不是查不出** —— 全包 **4894** 颗 TMP 里只有 **12 颗**带字距 5，其中 **8 颗是 `fs 67.55` 的顶栏 `Window Title`（8/8 全带）**、3 颗是本窗那两个大号数（fs 70 / 80）+ 本窗 `Window Title`、另 2 颗是大号展示字 ⇒ **逐颗手填的静态值，不是预设/字体表**（同字体资产 540 颗里 537 颗是 0）。详见 §三。 |
| **A518**（`Fill Line` 的 `scl=1.2` + `Sliced`） | ✅ **做完（两笔一起）** | 走 `MenuDraw.Nine`：框 = **`S_FillLineVisual`**（绕 **左中** 放大 1.2 ⇒ `134.43,519.06 → 1919.99,598.71`）、九宫 border `(4,4,4,4)` / 贴图 `12×12`、**`borderOutPx=(2,2,2,2)`**（= 原版 `ppuMul=2`）。🔴 **绕的是左中、不是中心** —— prefab `m_Pivot=(0,0.5)` 亲读（绕中心会跑到 −14.37）。 |
| **A519**（顶栏少第二颗底图） | ✅ **做完 · 「是漏了还是有意省的」如实标为「判不出本窗意图」** | 补建 `Header Background`（`0,21.65 → 595.30,137.01`，九宫 `(335,0,395,0)` / 贴图 `740×167`），并把 `Window Title` **改挂到它底下**（原版父链）。**可见后果不小**：原来唯一那颗底图只盖 `x∈[−462.1, 87.9]` ⇒ 标题那一段后面**根本没有底板**。 |
| **A520**（父子关系） | ✅ **做完** | `Current Streak Value` 改挂 `Current Streak` **之下**（原版深 2 / 深 3）；落点不受影响（`MenuDraw.Text` 吃画布绝对矩形）。 |
| **断言** | ⏸ **没落地（只设计，§七 是可直接粘贴的成品）** | 🔴 宿主 `Editor/RewardsScene.cs` **在我动手这段时间里正被别的写手写**（见下），按简报「只在你确认那一刻没人动它时才加，否则只报不改」⇒ **一行没碰**。 |
| 类型检查 | ✅ **0 / 0** | 见 §十一。 |
| 行尾 | ✅ **没翻** | 二进制读：两文件改前改后**都是纯 LF**（`DailyStreakPopup` CRLF 0 / LF **591**；`DailyRewardPopup` CRLF 0 / LF **438**）。`git diff --numstat` = **140/7** 与 **42/2** —— 全是判据注释的增量。 |

### 🔴 为什么断言没落地（硬证据，不是偷懒）

`Editor/RewardsScene.cs` 的 mtime 在我这一轮里**动了**：

| 时刻 | 事件 |
|---|---|
| 10:10:18 | 我进场的第一次现读（W-E3 交件那一刻） |
| **10:24:52** | **它又被写了一次** —— 那一刻我正在改 `Shell/*.cs` |
| 10:25:31 | `Shell/CampaignRewardWindow.cs` 也被写（**距我现读只 20 秒**） |

而 `资料/普查产出_1013/批次计划_1013.md:224` 写着 **A537 的重做件**（`Shell/CampaignRewardWindow.cs` 三处居中/排布）**同一个文件里也有要一起翻的落点**（`:4489-4500` 的 `baseW/premW` 与 `:4522`）⇒ 那个写手**必然要写 `RewardsScene.cs`**。批次计划 `:245`（A558）已经记过一次同文件双写手的事故。
⇒ **判定：那一刻有别的写手在动它** ⇒ 按简报办：**只报不改**，断言规格（含期望值字面量、改坏法、落点与插入锚点）全写进 §七。
⚠️ 收工时 `git status` 里 `Editor/RewardsScene.cs` **仍是 `M`** —— 那是**别人**的在飞改动；**本件对它的 `Edit` 调用数 = 0**（可核对：本件只发过 6 个 `Edit`，全部落在 `Shell/DailyStreakPopup.cs` 与 `Shell/DailyRewardPopup.cs`）。

---

## 二、A516 那颗 `More Rewards In`（原版值 · 我们改成什么 · 三方证据对上没）

**原版**（`Daily Reward Popup/Timer` 底下**三件**，本件亲跑 dump 的兄弟序 + 逐字段）：

| # | 节点名 | 矩形（绝对 · 画布 px） | 字号 | 对齐 | 说明 |
|---|---|---|---|---|---|
| 1 | `EverguildTextMeshPro` | **245.50 990.79 → 935.00 1054.15**（689.50×63.36） | **50**（基准 50） | **`Right/Capline`** | `'Más Recompensas En'` ← **本件补的** |
| 2 | `Image` | 938.55 995.82 → 991.85 1049.12（53.29²） | — | — | `WF_icon_clock`（我们已有） |
| 3 | `EverguildTextMeshPro (1)` | 990.20 990.79 → 1453.98 1054.15 | 50 | `Left/Capline` | `'19h 23m'`（我们已有，A493 #8 补的对齐） |

**我们改成什么**（`Shell/DailyRewardPopup.cs`）：
- 新增常量 `TimerMore = new PxRect(245.50f, 990.79f, 935.00f, 1054.15f)`（注释标了出处）；
- `BuildTimer` 里**在时钟图之前**加：
  `MenuDraw.Text(t, TimerMore, DailyData.StreakNextRewardsText(), Color.white, "EverguildTextMeshPro", 50f, QText)` + `MenuDraw.AlignRight(more, TimerMore)`。

**三方证据逐条对上了没** —— ✅ **全对上**：

| 来源 | 说了什么 | 与实测一致？ |
|---|---|---|
| ① 本件亲跑 `menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12` | `245.5 990.8 → 935.0 1054.1 · 689.50×63.36 · 'Más Recompensas En' · 字号=50.0 · 基准=50.0 · 对齐=Right/Capline · 折行=1 · 色=(1,1,1,1)` | ✅（`menu_rect.py --depth 4 --relative` 另一个工具给 `245.50 990.79 935.00 1054.15`，**逐值相同**） |
| ② `资料/说明书/04_界面UI/菜单全树.md:1092` | `EverguildTextMeshPro [245,991 690x63] text:'Más Recompensas En',script script` | ✅ |
| ③ 正本 `资料/日常_原版规格.md:456` | `Timer` … → TMP **fs=50** + **`WF_icon_clock`**(53.292²) + 时间 **fs=50**（**三件**） | ✅ |
| ④ W-D2 报告 §七·1（主判据） | 同三条 + `245.5 990.8→935.0 1054.1` · `fs 50` | ✅ |

**两处如实标注**：
1. 文案走 **`DailyData.StreakNextRewardsText()`**（返回值 `"More Rewards In"`）—— 它是**连登窗那条口**，本窗**没有专门的口**，而 `DailyData.cs` **不在本件白名单**（且那一刻正在被别的写手写）⇒ **复用**。原版这两窗是**同一条本地化词条**，连登窗那份是英文 `'More Rewards In'`、奖励窗这份落到了 es 语系 `'Más Recompensas En'` ⇒ 复用**语义正确**，但**名字带 `Streak`**（见 §十·2，建议主对话另立一条把口名统一）。
2. **别一刀切**：这颗是 `Right/Capline`，同窗下面那颗 `(1)` 是 `Left/Capline` —— 已在代码注释里写死。

---

## 三、A517 字距 5（三颗 · 顺序怎么排 · 「为什么只有这 3 颗」查到没）

### 3·1 三颗是谁 · 改成什么

| 颗 | 位置 | 字号 | 字距（原版 `m_characterSpacing`） | 改法 |
|---|---|---|---|---|
| `Current Streak` | `Shell/DailyStreakPopup.cs` `BuildSuccessful`（`S_CurLabel`，左沿 **43.0**） | 70 | **5** | `curLbl.SetCharSpacing(5f)` **在 `MenuDraw.AlignLeft` 之前** |
| `Current Streak Value` | 同上（`S_CurValue`，左沿 **545.91**） | 80 | **5** | 同上 |
| `Window Title` | `BuildHeader`（`H_Title`，左沿 **155.0**） | 67.55 | **5** | 同上 |

**为什么必须排在 `AlignLeft` 之前**（A475 那个坑，本件复核过代码）：`MenuDraw.AlignLeft` → `Label.AlignLeftOn`（`Battle/Label.cs:862-869`）**头一句就是 `RefreshBounds()`** —— 它是「量**当时的** `WorldW` 再反推整块位置」；而 `SetCharSpacing` 会 `ForceMeshUpdate()` **改渲染宽** ⇒ 排在它后面 = 那一行按**旧宽**定位、字整体错 `Δ宽/2`，**而且一声不响**。原版那两颗是**静态序列化字段**（不存在「先对齐、后加字距」这种次序）⇒ 照原版就只能是「字距在前、对齐在后」。
⛔ 本件**不补** `ForceRelayout()` —— `AlignLeftOn` 自己就会按**含字距**的 `textBounds` 重量一次（同 `Shell/LiveOpsEventWindow.cs:741-747` 的口径，那里也是这么处理的）。

**本窗现读的对照组**：加之前 `grep SetCharSpacing Shell/DailyStreakPopup.cs` = **0 处**（W-D2 报告 §七·2 的观察）；加之后 = **3 处**（正好三颗）。

### 3·2 「为什么原版只有这 3 颗带 5」—— ✅ **查到了，不是「查不出」**

**做法**：把 `bundle_menus_assets_all` 的 **35014 个 `MonoBehaviour/*.json`** 全量扫一遍，挑出带 `m_text` 的（= TMP）**4894 颗**，按 `m_characterSpacing` 分组：

| 字距 | 颗数 |
|---|---|
| −4.0 / −3.5 / −3.0 / −2.6 / −2.0 / −1.8 | 4 / 27 / 29 / 12 / 6 / 23 |
| +1.0 / +1.2 / +2.0 / +3.24 | 2 / 16 / 1 / 1 |
| **+5.0** | **12** |

**那 12 颗是谁**（全部列出）：

| 文本 | 字号 | 颗数 |
|---|---|---|
| `'Game mode'` / `'Game Mode'` | 67.55 | **7** |
| **`'Daily Streak'`** | 67.55 | **1** ← 本窗 `Window Title` |
| **`'Current streak:'`** | **70** | **1** ← 本窗那一颗 |
| **`'7'`** | **80** | **1** ← 本窗那一颗 |
| `'BATTLE FOR WARPFORGE'` | 72 | 1 |
| `'<color=#E27E1B>Choose your side…'` | 48 | 1 |

⇒ **两条结论（都有数据库级证据）**：

1. 🔴 **不是预设表 / 不是字体资产带来的**：**与 `Current Streak` 共用同一个 `m_fontAsset`（pid `-8244042478085975641`）的 TMP 共 540 颗**，其中 **537 颗 `m_characterSpacing = 0`**（另 3 颗就是本窗那三颗）。若是字体/预设驱动，同字体的 540 颗不可能只有 3 颗带值。
   ⚠️ 旁证：那三颗的 MB 里 `m_characterSpacing` 是**组件上的字面量 `5.0`**（`MonoBehaviour_-2556798379861400917.json` 亲读：`m_text='Current streak:'` · `m_characterSpacing=5.0` · `m_fontSize=70.0` · `m_fontSizeBase=70.0`）。它们还带一个 `fontPreset` 引用（`m_FileID 5 · m_PathID -3091116848139127833`），🔴 **那个资产本地解不出来**（全 `assets_full` 按文件名搜零命中）⇒ 「预设里有没有字距这一栏」**读不到、如实标**；但上面那条 537/540 的证据**已经足够否定「预设表驱动」这个假设**。
2. **看得出一条规律：`字距=5` 跟着「大号展示字」** —— 全包 **`fs == 67.55` 的 TMP 共 8 颗，8 颗全带 5**（就是那一族顶栏 `Window Title`：7 颗 `Game mode` + 本窗 `Daily Streak`），另加本窗两个大号数（70 / 80）与 2 颗大号标语字。**本窗其余那几颗都是 fs 36**（`Info` · `Next Rewards text` · `Timer Text`）**且都不带** ⇒ 「只有这 3 颗」与规律自洽，**不是随手挑的**。
3. 交叉印证（同族先例）：`资料/阶段二_战斗入口_原版规格.md:191` 也把 `Window Title` 记成 **`fs67.55 · charSpacing 5`**；`Shell/LiveOpsEventWindow.cs:741` 已经按这个值实现过（A475）。

⇒ **做法正当**：**逐颗手填**（原版也是逐颗填），我们**逐颗补**，⛔ 没有给它整个窗加 5。

---

## 四、A518 `Fill Line`（`scl=1.2` 与 `Sliced + ppuMul` 各自的改法）

**原版实测（本件亲跑 + prefab 亲读）**：

```bash
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10
```
```
2  Fill Line  134.4 525.7 1622.4 592.1  1487.98 66.37  Image  ANC✗
   视觉框=×1.2 → 视觉 1785.57×79.65  40k_generial_bar_fill 12×12 九宫4,4,4,4
   | Sliced (0.434,0,0.0567,0.624) ppuMul=2.0
```

prefab `bundle_menus_assets_all/RectTransform/RectTransform_3187973920738910891.json` **亲读**：
`m_Pivot = (0, 0.5)` · `m_AnchorMin = m_AnchorMax = (0, 0.5)` · `m_AnchoredPosition = (134.42572021484375, 0)` ·
`m_SizeDelta = (1487.978515625, 66.37200164794922)` · **`m_LocalScale = (1.2000001668930054 ×3)`**。

### 4·1 `scl=1.2` 那一笔 —— 🔴 **绕左中，不是绕中心**

- 视觉框 = `(134.43, **519.063**) → (**1919.994**, 598.707)`（宽 1785.564 · 高 79.644 · 中心 y 558.885）。
- 换算式：左沿**不动**（pivot.x = 0）、右沿 = `134.4257 + 1487.9785 × 1.2` = **1920.0**（正好铺满整幅）；竖向绕中线各 `66.372 × 0.6` = 39.823。
- 🔴 **⛔ 别用本文件那个 `ScaleAbout`**（绕**中心**）—— 那是奖格 `scaleMultiplierFirstElement` 的口径（奖格 prefab 的 pivot 实测 `(.5,.5)`）；拿它算这一颗，左沿会跑到 **−14.37**（屏幕外），差 **148.8px**。
- 实现：新增 `static PxRect ScaleLeftAbout(PxRect r, float s)` + `S_FillLineVisual = ScaleLeftAbout(S_FillLine, 1.2f)`。

### 4·2 `Sliced + ppuMul` 那一笔 —— 走 `MenuDraw.Nine`，两个量分开传

| 量 | 原版值 | 怎么进 `MenuDraw.Nine` |
|---|---|---|
| UV 切分（图里的真边宽） | `m_Border = (4,4,4,4)`（贴图 **12×12**） | `border` 形参 = `(4,4,4,4)`、`texW=texH=12` |
| **画出来的角块** | `4 ÷ (100/100 × **2**)` = **2px** | `borderOutPx` 形参 = `(2,2,2,2)` |

（换算口径 = uGUI `Image.multipliedPixelsPerUnit`；`ImageQuad.CreateNineSlice` 的 `borderPx` / `borderOutPx` 两个形参就是为「`m_PixelsPerUnitMultiplier`」这一档开的，见那边的注释。）
⇒ 我们原来的 **`Simple` 把 12×12 整张拉伸到 1785×79.65**（4px 的边被拉成 ~595px）、原版是**角块恒 2px、只有中段拉**。

**一条明确不属于本账的**（见 §十·1）：原版那颗 `m_Color = (0.434, 0, 0.0567, 0.624)`，我们的 `FillLineTint = (0.43, 0, 0.06, 0.62)` —— 三个分量都差一点点（0.004 / 0.0033 / 0.004）。**本件没改它**（不在账里），代码注释里也写明了「另立账」。

---

## 五、A519 第二颗底图（建了没 · 怎么判的）

### 5·1 原版结构（本件亲跑 dump 实读，**兄弟序也照抄**）

```
Header With Back Button        0.00    21.65  550.00   131.20  (WindowHeaderWithBackButton)
├ Header Background            0.00    21.65  595.30   137.01  Image + CSFMinMax + HLG   ← 本件补
│  └ Window Title            155.00    38.00  534.30   120.66  TMP 67.55 Left/Capline 字距5  ← 本件改挂
├ Header Background (1)     −462.10    21.65   87.90   137.01  Image                    ← 我们原来只有这一颗
└ Header Back Button         −24.40    23.67  143.48   134.99  Image + EverguildButton
```
**两颗底图的 sprite pid 相同** = `6473405944757030420` = **`WF_Campaign_Info_Background`**（`740×167 · 九宫 335,0,395,0`）。

### 5·2 `595.30` 是怎么来的（⚠️ 别抄错工具的那一列）

- `工具/menu_rect.py … --relative` 给的是 **`0.00 → 0.00`（宽 0）** —— 那是**布局跑之前的模板位**（`RectTransform` 的 `m_SizeDelta.x = 0`，宽由布局撑）。
- `工具/menu_dump.py`（**跑过布局**）给 **595.30** ✓。等式（prefab 字段亲读）：
  `CSFMinMax(m_HorizontalFit=1 · clampWidth=1 · widthMin=550 · widthMax=1250)` + `HLG(pad L155/R61 · spacing 5.5 · align MiddleLeft)` + 唯一子件 `Window Title` 宽 379.30 ⇒ `155 + 379.30 + 61 = **595.30**`（550 ≤ 595.30 ≤ 1250，**没被钳到**）。
  ⇒ **另一个工具给 0 的那一列不是原版值**，⛔ 别拿它建节点。

### 5·3 「是漏了还是有意省的」—— **判不出【本窗】的意图，如实标**（按 X 处理）

- **本窗没被记过**：`grep "Header Background" 资料/日常_*.md` ⇒ **零命中**；`Shell/DailyStreakPopup.cs` 的建窗注释里也没有任何交代。
- 🔴 **但同族窗口有先例（这条是 W-D2 那份报告没提到的）**：`资料/阶段二_战斗入口_原版规格.md:191-193` **白纸黑字记着**
  「`Header Background` 下有两件：**`Window Title`**…**同级还有 `Header Background (1)`**（往左延伸的装饰）」
  —— 与原版同族窗口**同一份结构**；而且 `Shell/LiveOpsEventWindow.cs:711-717` **已经按这个结构实现过**（两颗都建）。
- ⇒ **按 X 处理**：**补上**（铁律 11「与原版不符/有缺漏 ⇒ 先记录、之后完全复刻」），样式照原版 `Sliced` 九宫（同族先例同参数），并把「**本窗**为什么少一颗判不出来」如实写在这里。

### 5·4 它不是什么无关紧要的一颗

我们原来唯一那颗底图只盖 **x∈[−462.1, 87.9]**，而 `Window Title` 在 **x∈[155, 534.3]** ⇒ **标题那一段后面原来根本没有底板**（x∈[87.9, 550] 整段是空的）。本窗自检此前对顶栏**零覆盖**（见 §八）。

### 5·5 ⚠️ 一处「状态 → 参数」（铁律 5·c）

原版这一颗的**宽是内容撑出来的**：`clamp(155 + 标题实测宽 + 61, 550, 1250)`（见 5·2）。我们**不跑 uGUI 布局** ⇒ 写的是**当前文案**（`DailyData.StreakWindowTitle()` = `"Daily Streak"`）对应的那个 `595.30`。
⇒ **今天逐值等价**；**文案一变（换语言 / 换串）原版会重撑、我们不会** —— 如实标（这是本工程「不跑 uGUI 布局」那一层的共性，不是本件新增的偏离）。

---

## 六、A520 父子关系（改法 + 断言别怎么写）

- **改法**：`MenuDraw.Text(curLbl != null ? curLbl.transform : p, S_CurValue, …)` —— 父从 `Streak Successful` 改成 **`Current Streak`**（原版：`Current Streak` 深 2、它的子件深 3）。
- **落点不受影响**（已在代码注释里写清）：`MenuDraw.Text` 吃的是**画布绝对矩形**（`Local()` = `RectCenter(…) − PosInDesignSpace(parent)`，对父做一次反算）⇒ 父件是谁**只改树形、不改落点**；`S_CurValue` 那四个数照旧。
- **`curLbl == null` 的兜底**：`MenuDraw.Text` 可能返回 `null` ⇒ 用三元退回 `p`（⛔ 不写 `curLbl.transform` 裸解引用）。
- 🔴 **断言别怎么写（照调度台口径）**：⛔ **别写** `FindPath(succ, "Current Streak/Current Streak Value")`；按名字取一律用**递归**的 `FindChild(succ, "Current Streak Value")`（`FindChild` 走 `GetComponentsInChildren<Transform>(true)`，**穿子树**）。要钉父子关系就断 `cv.IsChildOf(cl)` / `cv.parent == cl`（§七 A520 那两条）。
  ⚠️ 如实补一句：**改对之后**原版那条路径**也能用**了（`FindPath` 会命中）—— 但按调度台口径，本件仍然只用 `FindChild`。
- **零撞车证据**：`grep -rn "Current Streak" Editor/*.cs` ⇒ **0 命中**（本件亲跑）⇒ 改树形**不可能撞红任何既有断言**，也说明这一族今天**零覆盖**。

---

## 七、断言清单（断什么 · 期望值来源 · 改坏法 · 落点）

> 🔴 **状态：全部【未落地】** —— 宿主 `Editor/RewardsScene.cs` 那一刻有别的写手（§一末尾的 mtime 证据）。下面是**可直接粘贴的成品**，每段都自包含（不新增文件级 helper，用的都是文件里**现成**的 `FindChild` / `FindPath` / `RectOfUnion` / `TmpVertPx` / `CheckNear` / `CheckTrue` / `TextOf`）。
> 🔴 **期望值一律写【原版字面量】**（⛔ 不写 `DailyStreakPopup.S_*` / `H_*` / `TimerText` 这类**被测实现自己的常量**）。
> 🔴 **量法**：文字走 **`TmpVertPx`（= TMP 真 mesh 顶点，`Editor/RewardsScene.cs:333`）**；图像走 **`RectOfUnion`**（⚠️ 九宫根上**没有** `ImageQuad`，`Wpx` / `CheckRectPx` / `CheckArt` 会只量到**第一块子块** ⇒ 见 §八·2）。

### 7·1 先落这两行小工具（贴在 §四 之前、或两段各自的最前面）

```csharp
// 🆕 W-D3（A516–A520）：一段文字**真渲出来的**左/右缘（画布 px）—— 走 TMP 自己的 mesh 顶点
//   （`TmpVertPx` = `textInfo.meshInfo[0].vertices` 经 `TransformPoint` + `ToPixel`，本文件现成的）。
// ⛔ 别用 `TextLeftPx/TextRightPx` 当主判据：那两个读 `Label.WorldW`（`_tmpW` 缓存 = 回读我们自己写进去的数）。
// ⚠️ 取不到（`Label` 不在 / 没顶点）回 `NaN` ⇒ 与任何期望值比都是**红**，正好当「这件没建出来」用。
System.Func<Transform, bool, float> TmpEdgePx = (t, right) =>
{
    var lb = t != null ? t.GetComponentInChildren<Label>() : null;
    var vs = lb != null ? TmpVertPx(lb) : null;
    if (vs == null || vs.Length == 0) return float.NaN;
    float r = right ? float.MinValue : float.MaxValue;
    for (int i = 0; i < vs.Length; i++) r = right ? Mathf.Max(r, vs[i].x) : Mathf.Min(r, vs[i].x);
    return r;
};
```

### 7·2 A516（落点：§四 `Daily Reward Popup` 段内，`Check(dr.MissingArt.Count, 0, …)` **之前**）

```csharp
// ★ A516：原版 `Timer` 底下是**三件**（'More Rewards In' + 时钟图 + 倒计时），我们原来只建了两件。
//   判据 = 本件亲跑 `python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Reward Popup" --depth 12`
//   （`245.5 990.8 → 935.0 1054.1` · `字号=50.0` · **`对齐=Right/Capline`**）+
//   `资料/说明书/04_界面UI/菜单全树.md:1092` + 正本 `资料/日常_原版规格.md:456`。
//   改坏法：删掉 `Shell/DailyRewardPopup.cs` 的 `BuildTimer` 里那句 `MenuDraw.Text(t, TimerMore, …)` ⇒ ①③ 红；
//           把那句的 `AlignRight` 写成 `AlignLeft` ⇒ ② 红（右缘差一整个文字宽）。
{
    var timerN = FindChild(dr.transform, "Timer");
    var moreN  = FindChild(timerN, "EverguildTextMeshPro");     // ⚠️ 直接子件取不到才退到递归 —— 这里正好都是直接子件
    CheckTrue(moreN != null && timerN != null && moreN.parent == timerN,
              "★ A516：`Timer` 底下那颗 `EverguildTextMeshPro`（'More Rewards In'）在（原版三件里的第一件）");
    Check((timerN != null ? timerN.childCount : -1), 3,
          "★ A516：`Timer` 的直接子件**恰好三件**（原版 = 'More Rewards In' + `WF_icon_clock` + 倒计时；"
        + "改成两件那一版 ⇒ 红）");
    CheckNear(TmpEdgePx(moreN, true), 935.0f, 1.5f,
              "★ A516：那颗的**真渲染右缘** = 框右沿 **935.0**（原版 `对齐=Right/Capline`；"
            + "写成 `AlignLeft` ⇒ 右缘 = 245.5 + 文字宽 ⇒ 红）");
    var moreLb = moreN != null ? moreN.GetComponentInChildren<Label>() : null;
    CheckTrue(moreLb != null && Mathf.Abs(moreLb.FontPxNow - 50f) <= 0.5f,
              "★ A516：那颗的字号 = 原版 **50**（px 口径 `FontPxNow`；改坏法：写成 36 ⇒ 红）");
    CheckTrue(moreLb != null && Mathf.Abs(moreLb.CharSpacing) <= 0.01f,
              "★ A516（**对照条**）：这一颗原版**没有**字距（dump 的 `字距=` 列不出现）—— "
            + "防的是「把 A517 那个 5 一刀切到所有窗」（那样这条红）");
}
```

### 7·3 A517（落点：§五 段内，`var succ = FindChild(ds.transform, "Streak Successful");` **之后**）

```csharp
// ★ A517：`字距=5` 三颗 —— 原版 `m_characterSpacing = 5.0`（组件序列化字面量）。
//   判据 = 同一次 dump 的 `字距=` 列（`Current Streak` / `Current Streak Value` / `Window Title` 三颗有、其余没有）
//   + **全包普查**（本件亲跑）：4894 颗 TMP 里只有 12 颗带 5、其中 `fs 67.55` 那一族 **8/8 全带**、
//     同字体资产的 540 颗里 537 颗是 0 ⇒ **逐颗手填**（不是预设/字体表）⇒ 我们也逐颗补，⛔ 别给整窗加。
//   两条**一起**断（单断一条分不出两种状态）：
//     ① `CharSpacing == 5` —— 抓「有没有补」；
//     ② 真渲染**左缘 == 框左沿** —— 抓「次序对不对」（`SetCharSpacing` 排在 `AlignLeft` 之后 ⇒ 按旧宽定位 ⇒ 偏 Δ宽/2）。
//   改坏法：删掉任一句 `SetCharSpacing(5f)` ⇒ ① 红；把它挪到 `MenuDraw.AlignLeft` **之后** ⇒ ② 红。
{
    var csNodes = new (Transform node, float leftPx, string what)[]
    {
        (FindChild(succ, "Current Streak"),       43.00f, "`Current Streak`（fs 70）"),
        (FindChild(succ, "Current Streak Value"), 545.91f, "`Current Streak Value`（fs 80）"),
        (FindChild(ds.transform, "Window Title"), 155.00f, "`Window Title`（fs 67.55）"),
    };
    for (int i = 0; i < csNodes.Length; i++)
    {
        var lb = csNodes[i].node != null ? csNodes[i].node.GetComponentInChildren<Label>() : null;
        CheckTrue(lb != null && Mathf.Abs(lb.CharSpacing - 5f) <= 0.01f,
                  $"★ A517：{csNodes[i].what} 的字距 = 原版 **5**（实得 {(lb != null ? lb.CharSpacing : float.NaN)}）");
        CheckNear(TmpEdgePx(csNodes[i].node, false), csNodes[i].leftPx, 1.5f,
                  $"★ A517：{csNodes[i].what} 的真渲染**左缘** = 框左沿 **{csNodes[i].leftPx}**"
                + "（钉「字距在前、对齐在后」—— 次序反了会偏 Δ宽/2）");
    }
}
```
⚠️ **牙口预判（如实标）**：`Current Streak Value` 的框只有 **37.85** 宽、文案 `'7'` @ fs 80 ⇒ 第 ② 条**可能两态同形**（文字宽 ≈ 框宽 ⇒ 居中和贴左落在同一处）。但那一条的**第 ① 条**（`CharSpacing == 5`）**恒能分辨** ⇒ 这一颗不会「没牙口」。

### 7·4 A518（落点：同 §五 段内）

```csharp
// ★ A518：`Fill Line` 的**视觉框**（原版 `m_LocalScale = 1.2 × 绕 pivot (0,0.5)`）与 `Sliced`。
//   判据 = prefab `RectTransform_3187973920738910891.json` 亲读（`m_Pivot=(0,0.5)` · `m_AnchorMin=(0,0.5)` ·
//     `m_AnchoredPosition=(134.42572,0)` · `m_SizeDelta=(1487.97852,66.37200)` · `m_LocalScale=1.2000001668930054`）
//     + `menu_dump.py … "Daily Streak Popup" --depth 10` 的行末 `视觉框=×1.2 → 视觉 1785.57×79.65`。
//   🔴 三条各自钉一件：左沿钉「绕哪一点」（绕中心 ⇒ −14.37）、右沿/高钉「1.2 有没有接」。
//   改坏法：① 不接 1.2（画布局框）⇒ 右沿 1622.40、高 66.37 ⇒ 红；② 改成绕**中心**缩 ⇒ 左沿 −14.37 ⇒ 红。
float fx1, fy1, fx2, fy2;
bool okF = RectOfUnion(FindChild(succ, "Fill Line"), out fx1, out fy1, out fx2, out fy2);
CheckTrue(okF, "★ A518：`Fill Line` 量得到渲染矩形（九宫 ⇒ 走**并集**，⛔ 别用 `Wpx`/`CheckRectPx`，那会只量到第一块子块）");
if (okF)
{
    CheckNear(fx1, 134.43f, 0.5f,
              "★ A518：`Fill Line` 渲染**左沿 = 134.43**（原版 `m_Pivot=(0,0.5)` ⇒ 绕**左中**放大、左沿不动；"
            + "改成绕中心缩 ⇒ **−14.37** ⇒ 红）");
    CheckNear(fx2, 1919.99f, 0.5f,
              "★ A518：渲染**右沿 = 1920.0**（= 134.4257 + 1487.9785×1.2 ⇒ 正好铺满；**没接 `scl=1.2` ⇒ 1622.40** ⇒ 红）");
    CheckNear(fy1, 519.06f, 0.5f, "★ A518：渲染**上沿 = 519.06**（= 558.885 − 66.372×0.6）");
    CheckNear(fy2 - fy1, 79.64f, 0.5f, "★ A518：渲出来的**高 = 79.64**（= 66.372 × 1.2）");
}
```

### 7·5 A519（落点：同 §五 段内）

```csharp
// ★ A519：顶栏底下原版**两颗底图**，我们原来只建一颗（少了 `Header Background`），且 `Window Title` 挂在它底下。
//   判据 = `menu_dump.py bundle_menus_assets_all "Daily Streak Popup" --depth 10` 的树（深 1/2/3 + 矩形）。
//   ⚠️ 矩形要取 dump 那一份（**595.30**）—— `menu_rect.py --relative` 给 `0.00→0.00` 是**布局跑之前**的模板位。
//   改坏法：删掉 `Shell/DailyStreakPopup.cs` 的 `BuildHeader` 里那句 `MenuDraw.Node(h, "Header Background", H_Plate)`
//           ⇒ ①② 红；把 `Window Title` 改回挂 `h` ⇒ ③ 红。
{
    var plate = FindPath(ds.transform, "Header With Back Button/Header Background");
    CheckTrue(plate != null,
              "★ A519：顶栏底下有 `Header Background`（原版两颗底图里的**第一颗**；我们原来只有 `Header Background (1)`）");
    CheckTrue(plate != null && FindChild(plate, "Window Title") != null,
              "★ A519：`Window Title` 挂在 **`Header Background`** 之下（原版父链；挂在顶栏根 ⇒ 红）");
    float px1, py1, px2, py2;
    bool okP = RectOfUnion(plate, out px1, out py1, out px2, out py2);
    CheckTrue(okP, "★ A519：那颗底图量得到渲染矩形");
    if (okP)
    {
        CheckNear(px1, 0f, 1f,       "★ A519：`Header Background` 渲染**左沿 = 0**（原版 `m_AnchorMin=(0,0.5)` · dump 0.00）");
        CheckNear(px2, 595.30f, 1f,  "★ A519：渲染**右沿 = 595.30**（= CSFMinMax 撑出来的 155+379.30+61；⛔ 别用 0）");
        CheckNear(py1, 21.65f, 1f,   "★ A519：上沿 = 21.65（与 `Header Background (1)` **逐值相同**）");
        CheckNear(py2, 137.01f, 1f,  "★ A519：下沿 = 137.01");
    }
}
```
⚠️ **牙口如实标**：`RectOfUnion(plate)` 取的是**九宫那几块**的并集（`Window Title` 是 **Label**、不含 `ImageQuad` ⇒ 不会被算进来 —— 本件已核：`grep ImageQuad Battle/Label.cs` 只有注释命中）。原版那一颗两轴都会被挤（`335+395 = 730 > 595.30` ⇒ `scX = 0.8155`、中段宽 0 ⇒ **只建 2 块**），并集仍是整框 ⇒ 四条都分得出两种状态。
> ✅ **2026-10-16 就地订正（铁律 5 · A680）**：本行原文写「**只建 6 块**」—— **错**。判据 = `Battle/ImageQuad.cs:481-493` 的 `if (w <= 0f || h <= 0f) continue;` **逐块剔除** ⇒ 中段宽 0 时只剩 `(0,1)` / `(2,1)` 两块。原文「6」多半是把「九宫 9 块 − 中段 1 = 8」或别的口径串了。

### 7·6 A520（落点：同 §五 段内）

```csharp
// ★ A520：`Current Streak Value` 是 `Current Streak` 的**直接子件**（原版深 2 / 深 3）。
//   改坏法：把它建回 `Streak Successful` 的兄弟（`MenuDraw.Text(p, …)`）⇒ 两条一起红。
//   ⛔ 按调度台口径**不写** `FindPath(succ, "Current Streak/Current Streak Value")` —— 用**递归**的 `FindChild`。
{
    var cl = FindChild(succ, "Current Streak");
    var cv = FindChild(succ, "Current Streak Value");
    CheckTrue(cl != null && cv != null && cv.parent == cl,
              "★ A520：`Current Streak Value` 是 `Current Streak` 的**直接子件**（原版父链；兄弟那一版 ⇒ 红）");
    CheckTrue(cv != null && cl != null && cv.IsChildOf(cl),
              "★ A520：…而且**还在这棵子树里**（防止「挂到别处但恰好也叫这个名」）");
}
```

### 7·7 期望值来源汇总（⛔ 没有一条来自我们自己的常量）

| 条 | 期望值 | 来源 |
|---|---|---|
| A516 右沿 935.0 / 字号 50 / 子件数 3 | 原版 dump 实读 | `menu_dump … "Daily Reward Popup" --depth 12`（本件亲跑） |
| A517 字距 5 / 左沿 43.00 · 545.91 · 155.00 | 原版组件字段 + 原版矩形 | 全包 `m_characterSpacing` 扫描 + dump 实读 |
| A518 134.43 / 1919.99 / 519.06 / 79.64 | 原版 prefab 字段算出来的几何 | `RectTransform_3187973920738910891.json` 亲读 |
| A519 0 / 595.30 / 21.65 / 137.01 | 原版 dump（跑过布局） | `menu_dump … "Daily Streak Popup" --depth 10` |
| A520 父子关系 | 原版树层级 | 同上（深 2 / 深 3） |

---

## 八、那条「新加一层」的必查行 grep 结果

**问题**：本件**新增了绘制层**（① `Fill Line` 从 1 个 quad 变成**九宫 9 块**；② 顶栏多一颗底图；③ 奖励窗多一颗文字）⇒ 简报要求先查「凡 `!RectOfUnion` / 『一个 quad 都没有』式断言」会不会被打破。

**grep 结果（本件亲跑，只读）**：

| 查什么 | 命中 | 会不会被打破 |
|---|---|---|
| `!RectOfUnion` 在 `Editor/*.cs` | **1 处**：`Editor/ShopScene.cs:961`（helper `CheckRectPxUnion`）| ✅ **不会** —— 那是**肯定式**断言（`RectOfUnion == false` ⇒ 报「一整棵里没有 `ImageQuad`」），新加层只会让 quad 变多 |
| 「一个 quad 都没建」在 `Editor/RewardsScene.cs` | `:5498`（战役奖励窗那一族）、`:211` / `:478`（吸收层 / 命中区 helper）| ✅ **不会** —— 三条都不是本窗的节点（`:5498` 是 `CampaignRewardWindow` 的格子；`:211`/`:478` 断的是**指定节点**（`AbsorbHit` / 命中区）自己有没有 quad） |
| 🔴 `ScanSoftCuts(succ, …)`（§五 里那三条软边断言） | `:598-636` | ✅ **不会** —— 它**带名字闸**：`if (c == null \|\| c.name.IndexOf("_soft") < 0) continue;`（`:616`），而九宫的 9 块是 `Fill Line_00…` 这种名字 ⇒ 数不进来。**代码自己的注释里就写着**「`CreateNineSlice` 的 9 块是**兄弟**不是父子，但留一道名字闸更保险」 |
| `CheckShadeRule` / `CheckAbsorbRule` | `Shell/MenuDraw.cs:2077` · `Editor/RewardsScene.cs:191` | ✅ **不会** —— 都吃**指定节点**（`ds.ShadeHit` / `"AbsorbHit"`），不做「最上面那个 quad 是谁」这类推断 |
| window 级 quad 计数断言 | `RewardsScene.cs` **0 处**（`childCount` 的命中全在 Weekly Mission / ForgeTab / 一个合成测试窗） | ✅ **不会** |
| `Check(ds.MissingArt.Count, 0)` / `Check(dr.MissingArt.Count, 0)` | §五 / §四 各一条 | ✅ **不会** —— 本件**没引入任何新贴图**（`ArtHeaderBg` / `ArtFillBar` / `ArtClock` 三张原来就在用） |
| `CheckHoverSwap(dr.transform/ds.transform, …)` | §四 / §五 各一条 | ✅ **不会** —— 本件**一个 `WindowButton` 都没加/没删** |

**⚠️ 但有一条「新加层」带来的量法陷阱（写给后面写断言的人）**：
`MenuDraw.Nine` 建出来的是一棵小树（根**没有** `ImageQuad`，9 块是**子件**）
⇒ `Wpx` / `CheckRectPx` / `CheckArt` / `QueueOf` 这些走 **`GetComponentInChildren<ImageQuad>()`** 的 helper **只量到【第一块子块】**（比如 `Fill Line` 会读到宽 ~2px 而不是 1785.57）。
✅ **要量整块就用 `RectOfUnion`**（§七·4 就是这么写的）。

---

## 九、没查清 / 没做的

1. 🔴 **断言没落地**（§七 是成品但**没进代码**）—— 宿主 `Editor/RewardsScene.cs` 那一刻有别的写手（§一的 mtime 三条证据）。**这一步不能算「已验」**。
2. **本件没跑 Unity**（按批次口径）⇒ 「九宫画出来像不像 / 字距看起来对不对」**没验**；`RewardsScene.Run`（全套里 ≈ 4m20s）由主对话在同步点跑。
3. **`fontPreset` 那个资产本地解不出来**：三颗带字距的 TMP 都引用 `m_FileID 5 · m_PathID -3091116848139127833`，**全 `assets_full` 按文件名零命中** ⇒ 「预设里有没有字距这一栏」**读不到**（如实标）。但「不是预设表驱动」这个结论**另有独立证据**（同字体 540 颗里 537 颗是 0）⇒ 不影响 A517 的做法。
4. **A519「本窗为什么少一颗」判不出意图**（本窗文档零记录）；只查到**同族窗口有相反的先例**（§五·3）⇒ 按铁律 11 补上。
5. **A519 的「宽随内容」那一档没做**（§五·5）：我们写死 `595.30`、原版是 `clamp(155+标题宽+61, 550, 1250)`。今天文案固定 ⇒ 等价；**换文案会分家**。要不要做取决于「本工程要不要为这一类件实现 CSFMinMax」——**不由本件裁**。
6. **`H_Bg` 那颗（`Header Background (1)`）仍是 `Simple`**（原版 `Sliced`）—— 见 §十·3，⛔ 本件没改。
7. **WD2 §六 那 8 条「没查清」本件只碰了 2 条**（字距那一族、`Header Background`），其余（`ContentSizeFitter` 要不要建、H37 §四·3 那 6 组、点阵后端那一档）**一行没动**。

---

## 十、顺手发现（⛔ 一个都没在本件里改）

1. 🟡 **`Fill Line` 的色值差一点点**：原版 `m_Color = **(0.434, 0, 0.0567, 0.624)**`（dump 的 tint 格实读），我们的 `FillLineTint = (0.43f, 0f, 0.06f, 0.62f)` —— 三个分量各差 `0.004 / 0.0033 / 0.004`。**不在本账**（A518 是 `scl` 与 `Sliced`）⇒ 没改；代码注释里已写明「另立账」。
   **要做**，判据 = `menu_dump.py … "Daily Streak Popup" --depth 10` 那一格 `Sliced (0.434,0,0.0567,0.624)`。
2. 🟡 **`DailyData` 缺一个「奖励窗 + 连登窗共用」的 'More Rewards In' 口**：本件只能复用 `StreakNextRewardsText()`（名字带 `Streak`、返回值是连登窗 prefab 的**英文**字面量，而奖励窗 prefab 那份是 es 的 `'Más Recompensas En'`）。
   **要做**，判据 = 两窗原版是同一条本地化词条；⚠️ `Shell/DailyData.cs` **不在本件白名单**（且那一刻正在被别的写手写）⇒ 只报。
3. 🔴 **`Header Background (1)` 那一笔也没照原版**：`Shell/DailyStreakPopup.cs` 的 `BuildHeader` 里
   `MenuDraw.Rect(h, Art(ArtHeaderBg), H_Bg, "Header Background (1)", QPanel);`
   —— 原版那是 **`Sliced`**（同一个 sprite、同一套参数：`WF_Campaign_Info_Background` **740×167 · 九宫 (335,0,395,0)** · `m_PixelsPerUnitMultiplier = 1.0`；prefab `MonoBehaviour_-3814993926740998224.json` 亲读 `m_Type=1`）。
   **证据**：dump 那一行 `Header Background (1) … WF_Campaign_Info_Background 740×167 九宫335,0,395,0 | Sliced (1,1,1,1)`；**同族先例** = `Shell/LiveOpsEventWindow.cs:766-768` 对**同一个原版件**用的就是 `MenuDraw.Nine(…, HeaderBorder, HeaderTexW, HeaderTexH, …)`。
   **要做**（判据齐），改法 = 把那句换成 `MenuDraw.Nine(h, Art(ArtHeaderBg), H_Bg, HeaderBorder, HeaderTexW, HeaderTexH, QPanel)`（本件新增的那两个常量直接可用）。**⛔ 本件没改**（不在 A516–A520 任何一条里）。
   ⚠️ 如实标：**这一笔的视觉差很小** —— `335 + 395 = 730 > 550` ⇒ `scX = 0.7534`（`Simple` 是 `550/740 = 0.7433`），两者都是「两边端帽 + 中段被丢掉」，肉眼几乎不可分。**但按铁律 11 仍要做。**
4. ℹ️ **`ScaleAbout`（绕中心）与 `ScaleLeftAbout`（绕左中）现在同文件并存** —— 这是**有意的**（原版两颗件的 pivot 真的不同：奖格 `(.5,.5)`、`Fill Line` `(0,.5)`），两个函数各自的注释里都点明了「⛔ 别互相替换」。将来若有人把它们合并，两处几何会同时错。
5. ℹ️ **奖励窗 `Timer` 那颗新文字不吃裁切**（走 `MenuDraw.Text` 而不是 `GameWindow.Text`）—— 与同窗既有那两颗（时钟图 + 倒计时）**同一个口径**，本窗 `Timer` 本来就不在滚动视口里 ⇒ 不是缺陷，只是记一笔。

---

## 十一、类型检查结果

```
$ TMPDIR=/tmp/wf_wd3 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

✅ **运行时 0 · 编辑器 0**（**改完两个 `.cs` 立刻跑的**，独立 `TMPDIR` 不覆盖别人的产物；收工前一连跑了 **4 次**，最后一次仍 0/0）。

🔴 **如实记一次「假红」**（正是 `CLAUDE.md` 13·3 那条现象，**不是我的文件**）：
收工前倒数第二次跑，**运行时报告 12 个错、全部落在 `Assets/CardPresentation/Battle/BattleCameraSreenSize.cs`**（`error CS1061: 'Tween' 未包含 'Complete'/'Kill' 的定义`）。
- **判据**：① 逐文件归并 `grep -E "^Assets" | sed 's/(.*//' | sort | uniq -c` ⇒ **12/12 全在那一支**，我这两个文件**一条错都没有**；② 那个文件 `git status` = **`?? `（未跟踪的新文件）**、mtime = **10:27:44**（我跑检查前 23 秒）；③ **两次跑的错数还不一样（12 → 11）** ⇒ 那一刻有写手正在写它。
- **处置**：按简报 —— ⛔ **没碰别人的文件**，**等 75 秒重跑** ⇒ 运行时 **0** / 编辑器 **0**（那个文件写完了 / 被写对了）。
- ⇒ **我的两个文件在全部 4 次里从未出现过一条错**。

✅ **没有一条错（稳态下）落在别人的文件上**（= 不需要「重跑一次 + 如实记别人的半成品」那一步以外的事）。
✅ 行尾：两文件改前改后**都是纯 LF**（二进制读：`DailyStreakPopup` CRLF 0 / LF **591** · `DailyRewardPopup` CRLF 0 / LF **438**），
`git diff --numstat` = **140/7** 与 **42/2** —— 全是我加的行 + 我删掉的**过期的旧注释**，**没有整篇翻行尾**。
✅ 本报告 `WD3_领奖弹窗补建.md` 本身也是纯 LF（434 行）。
✅ ⛔ 没跑 Unity、没动 git、没改 `项目任务.md` / `CLAUDE.md`。
