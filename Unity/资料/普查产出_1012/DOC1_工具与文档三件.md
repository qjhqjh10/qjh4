# DOC1 · 工具与文档三件（A335 + A347 + A349）（写手 · 2026-10-12）

> **白名单三文件**：`Unity/工具/menu_dump.py`（**只加一列**）· `Unity/资料/待办判据_阶段二与联机.md`（**只动 A25 ⑥ 与 A25·补(一)**）·
> `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs`（**只改 `FindBuilt` 的注释**）。
> ⛔ 没跑 Unity（`-executeMethod` 一次都没有）· ⛔ 没跑那 11 条自检（本件按类型 = **零自检**：纯 python 工具 + 纯文档 + 纯注释）·
> ⛔ 没动 git 写命令 · ⛔ 没改正本（`项目任务.md` / `CLAUDE.md` 一个字没碰）· ⛔ 没越白名单。
> ⚠️ **秒级类型检查跑了，但没能验到本件自己的改动** —— 运行时程序集被**别的写手的半成品**挡住（详见 §五·1），如实记，**不当成「已验」**。

---

## 一、结论（三件各一句）

| 件 | 结论 |
|---|---|
| **A335** | ✅ **做完** —— `menu_dump.py` 的 TMP 那一格**新增 `基准=`**（= `m_fontSizeBase`），**与 `m_enableAutoSizing` 无关**（`auto=0` 的站也印）；`auto[…]` 的追加条件**一个字没动**。**已实跑验收**：`Campaign Tab`（文本模式）与 `Player Profile Window`（`--md` 模式）两种输出都能看到 base，且与 `V7_A305_A304_普查.md` §二·3 的逐站表**逐值吻合**（§二·1）。 |
| **A347** | ✅ **做完** —— A25·补(一) 那张派活表**逐行标了「✅ 已落地 + 现读档号」**（新增一列），并把 ⑥ 正文、裁断块那两句、表头「还剩 13 处」、表后那句「4 处现行档就是错的」**就地标成「当时状态」**（含 S5 §二·4 点名的**「全工程 7 处」**）。9 行现读逐处核过 = 全部走 `MenuDraw.ShadeHit`。⛔ **站点总数那几个数一个都没动**（理由见 §四·3）。 |
| **A349** | ✅ **做完** —— `FindBuilt` 的注释**就地订正**（原写「容差与 `SameObject`（0.05）一致」= **与实现不符**）⇒ 改成「**没有任何容差、同名取最近**」+ 写明那两处**用途本来就不同**。**实现一个字没动**。判据 = `FX1_Battle七条红修复.md` **§五·2** + 现读 `if (d < bd)`。⚠️ **我没有把它判成「该有容差」**，理由见 §三·2（不是「查不到」，是**判据本身就指向「无容差」**）。 |

---

## 二、判据（逐条 · 现值 vs 新值 · 出处）

### 2·1 A335 —— 加的那一列 + 验收实读

- **改法**：`describe()` 的 TMP 分支里，在 `字号=…` 之后插一格 `基准=`（`m_fontSizeBase`）。
- **为什么**（三条，都写进代码注释了）：① `base` 是**自适应重排的起点**（`TextMeshPro.cs:2149-2150` `if (m_enableAutoSizing) m_fontSize = Clamp(m_fontSizeBase, min, max)`）；
  ② 「`字号`」是**收敛后**的值、盖不到它，而 `fontSize` 的 setter **只在 `!m_enableAutoSizing` 时**才回写 base（`TMP_Text.cs:467`），默认值又是 **36**（`:473`）⇒ **开着 autoSizing 调字号时 base 一次都没被改过**；
  ③ 于是**想核 base 的人只能自己重写全库扫描**（V7 §六·1 就是这么被逼的）。
- **两处刻意的口径决定**（都写进注释，⛔ 别当 bug 改）：
  · **与 `auto[…]` 不同档**：这一格**不看 `m_enableAutoSizing`**（`auto=0` 的站也有 base —— 例：`Campaign Tab` 那颗 `Quantity` = 61.23，V7 §二·3 #4）；
  · **印在 `字号=` 之后**：文本模式最后那格「参数」有 `BODY_MAX` 截断、**砍的是尾巴**，越靠前越保得住（同 `reverse=1` 那条体例）。
- **实读（文本模式，`bundle_menus_assets_all "Campaign Tab" --depth 4`）**：

```
2      Title      … TextMeshProUGUI  … 'ULTRAMARINES' 字号=31.75 基准=36.0 auto[25.0~35.0] 对齐=Left/Bottom 折行=0 色=(1,1,1,1)
3      Quantity   … EverguildTextMeshPro … '200' 字号=61.22999954223633 基准=61.22999954223633 折行=1 色=(1,1,1,1) | 字段: …   ← auto=0 的站，本行【没有】auto[…]，但有基准=
2      Points     … TextMeshProUGUI  … 'Points: 69' 字号=34.79999923706055 基准=36.0 auto[18.0~40.0] 对齐=Left/Middle 折行=0 色=(1,1,1,1)
```

- **与 V7 逐站表打分（这是本件最硬的一条验收 —— 工具现在能**独立复现** V7 手扫出来的值）**：

| V7 §二·3 | V7 记的值 | 本件工具现读 | 判 |
|---|---|---|---|
| #1 `CampaignTab` Title | base **36.0** · auto `[25~35]` · fontSize 31.75 | `基准=36.0 auto[25.0~35.0] 字号=31.75` | ✅ 逐值同 |
| #2 `CampaignTab` Points | base **36.0** · auto `[18~40]` · 34.8 | `基准=36.0 auto[18.0~40.0] 字号=34.79999923706055` | ✅ |
| #4 `…Premium Panel/Points/Quantity` | base **61.23** · **`auto=0`** | `基准=61.22999954223633`、**无 `auto[…]`** | ✅ |
| #21 `PlayerProfileWindow` 六颗页签 | base **35.0** · auto `[10~35]` · 35/35/**33.65**/35/32.75/35 | `--md` 现读：`基准=35.0 auto[10.0~35.0]`、`字号=33.650001525878906 基准=35.0` | ✅ |
| #10 `BoosterInfoPopup` Slider counter | base **36.0** · nominal 26.35 | 本件未单独复跑（同上机制、无特例） | ⚪ 未跑 |

- `--md` 模式也已验证（同一个 `describe()`）：`bundle_menus_assets_all "Player Profile Window" --depth 6 --md` ⇒ **15 行带 `基准=`**。
- ⚠️ **代价（如实记）**：已经在 `BODY_MAX` 上限的行，尾巴会**多砍掉「这一格本身的宽度」** ——
  实测 `'Change Deck'` 那行 `……[+370]` → **`……[+378]`**（` 基准=12.0` 正好 8 字符），多砍的部分**全在行尾那串通用 `字段: …` 清单里**，没有实质信息被吃掉。

### 2·2 A347 —— 9 行逐行判决（现读，全部已落地）

> 判据 = `grep -rn "MenuDraw\.ShadeHit(" --include=*.cs` + 逐文件现读调用点（A/B 两个实参 = 表里的压暗档 / 内容最低档）。

| 表里那行 | 表里的「改前」编码 | **现读（2026-10-12）** | 判决 |
|---|---|---|---|
| `BattleLogPopup` | `QPanel`(3450) | `MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, …)` | ✅ 已落地（档未变） |
| `BoosterInfoPopup` | `QShadeHit=QShade`(3070) | `MenuDraw.ShadeHit(DarkNode, DarkR, QShadeHit, QHit, …)` | ✅ 已落地（档未变） |
| `CardDetailPopup` | `QCdHit−1`(3117) | `MenuDraw.ShadeHit(root, …, QShade, QCdHit, …)`；`QShade = 3105` | ✅ 已落地（**档已订正**，源码里有 2026-10-04（A47 接线批）订正注释） |
| `DeckSelectionPopup` | `QDsHit−1`(3127) | `MenuDraw.ShadeHit(root, …, QDs, QDsHit, …)`；`QDs = 3125` | ✅ 已落地（**档已订正**，同上） |
| `DuelPopupWindow` | `QPanel`(3400) | `MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, …)` | ✅ 已落地（档未变） |
| `LeaderboardWindow` | `QPanel`(3500) | `MenuDraw.ShadeHit(dark, DarkBgR, QPanel, QHit, …)` | ✅ 已落地（档未变） |
| `MissionRerollPopup` | `QShadeHit=QShade`(3080) | `MenuDraw.ShadeHit(dark, DarkR, QShadeHit, QHit, …)` | ✅ 已落地（档未变） |
| `PlayerProfileWindow` | `QShade+1`(3151) | `MenuDraw.ShadeHit(root, …, QShade, QHit, …)`；`QShade = 3150` | ✅ 已落地（**档已订正**，同上） |
| `BoosterPackOpenWindow` | `QCloseSurface=QBase−1`(3169) | `MenuDraw.ShadeHit(CloseNode, CloseSurfaceR, QShade, QCard, …)`；`QShade = 3170 < QCard = 3171` | ✅ 已落地（**档已订正**，`QCloseSurface` 常量已删） |

- 旁证：`资料/普查产出_1011/WA_文档与注释四件.md` §2·2/§3·2/§六·3（它做过同一轮复核）· `资料/普查产出_1012/S5_文档工具挂起_开账现核.md` §开账表（A347 行：那 4 处代码全已改完）。
- **落地形态**（我改成什么样）：① 表**新增第 6 列「✅ 落地（2026-10-12 回填·A347）」**，9 行逐行 ✅（那 4 处另带**现读档号**）；
  ② 表**上方**加一条 ✅ 横幅（现读 9 行已落地 + 「站点总数别在本表记」）；③ ④**只在既有文本上就地标「当时状态」，⛔ 不复述任何数**。
- ⛔ **我刻意没做的**：**没有一个数**被我改动或新增（见 §四·3）· **没有碰行号**（按符号认 + 写明「行号早已漂 = A338 那一族」）· 没碰（二）`m_Padding` 表、没碰 A9/A27/A38/A47/A48 各行。

### 2·3 A349 —— 订正前后

- **改前**（`ArenaBuilder.cs` 的 `FindBuilt` doc，末两行）：「判据出处：… `ArenaBuilder.SameObject`（清单并表，**容差 0.05**）。这里**不写新口径**，**容差与 `SameObject` 一致**。」
- **实现现读**（`:2667` 起）：`if (Norm(t.name) != want) continue; … if (d < bd) { bd = d; best = t; }` ⇒ **没有任何阈值**，就是「同名里取最近」。
- **改后**：删掉「容差 0.05」那句，改成如实 + 说明**两处用途不同**（`SameObject` = 清单 ∥ 旁挂**并表**、要求逐轴 ≤ 0.05；本函数与 `EnvironmentApplier.FindNearest` = 同名会有多个 ⇒ 取最近，那边注释自称「同一族判据的**宽松版**」），并留更正痕迹（日期 + 原话 + 来件）。

---

## 三、待接线 / 待判据

### 3·1 待接线（⛔ 都不在我白名单里，只报）

| # | 位置（现读） | 是什么 | 为什么该接 |
|---|---|---|---|
| 1 | `Unity/MyGame/Assets/CardPresentation/Shell/BoosterPackOpenWindow.cs:312` | 注释仍写「（不经过 `MenuDraw.Hit`），档用的是 `QCloseSurface = QBase − 1`(3169)」 | 上下文是**复述改前**（下一行 `:313-314` 已写「按规矩收口到公共件 `MenuDraw.ShadeHit`，档改成 `QShade`(3170)」）⇒ **不算错，但同文件 `:98`/`:103` 已订正过同一件事 ⇒ 三处口径并存**，读的人容易只看到 `:312` 那句（本轮简报的同族附记也点了这一条）。⚠️ 顺带：那个**矩形变量名还叫 `CloseSurfaceR`**（`:317` 的实参），是同一个旧号的残影。 |
| 2 | `Unity/资料/待办判据_阶段二与联机.md`（**A25·补(一) 末尾那两块**）+ `Shell/MenuDraw.cs:1645` 的注释 | 两处都写 **21 处走 `MenuDraw.ShadeHit`** | **现读是 23 条真调用**（`Shell/` 22 + `Editor/ShellScene.cs:2314` 1），另有 **4 条注释里的散文提法**（`MenuDraw.ShadeHit(..., name)`，分别在各 Scene 的注释里）⇒ raw grep = **27**。**站点数的现行口径该由调度台一处定案**（两处一起改；`MenuDraw.cs` 不在我白名单，我只改一处就会造成「两份说法打架」）。 |

### 3·2 待判据 —— **本条我判「不成立」，不是「查不到」**

- 简报写：「若你判断『该有容差』是**原版语义** ⇒ 不自己做主，写进报告『待判据』」。
- **我的判定：没有这条判据、也不该有**，理由三条：
  ① `FindBuilt` 与 `ResolveGroupParent` 是**我们自己的建场工具链**（`WarpforgeArena1/Editor/`），**原版没有对应代码** ⇒ 「原版语义」这个源**根本不存在**，不能拿它当判据；
  ② 仓内**同一族的第三处**（`EnvironmentApplier.FindNearest`，运行时回头找）也是「同名取最近、无容差」，而且它**自己写明**这是「与 `ArenaBuilder.SameObject` 同一族判据的**宽松版**：那边是「并清单」要求逐字相同，这边是运行时回头找、同名会有多个」⇒ **三处的分工本来就是「并表要严、找对象要宽」**；
  ③ 真加 0.05 阈值**会改变行为且方向更糟**：候选再近也会被拒 ⇒ `ResolveGroupParent` 回 null + 出声（把「找到了」变成「找不到」），属于**新引入的缺陷**。
- ⇒ 我**只订正注释、实现一个字没动**；若调度台另有口径，`FindBuilt` 那 4 行就是唯一改动点（且要连 `FindNearest` 一起定）。

---

## 四、没查清 / 判不了的（⛔ 不猜）

1. 🔴 **本件的 `.cs` 改动没有被编译器验过** —— 类型检查跑了两遍都停在运行时程序集（别人的半成品，见 §五·1），**编辑器程序集根本没编到**（`error CS0006: 找不到元数据文件 …WFCheck.dll` 是上游失败的连带）。
   本件改的是 `///` 注释、且我逐字核过**没有引入裸 `<` / `&`**（原本那句 `d < bd` 我改写成了「取 `d` 最小的那一个」）⇒ 语法风险可忽略，但**结论按「未验」记**。
2. **那 4 处 `BackdropHit` 的「现读档号」我核了、但没逐个跑自检** —— 现读 `PracticeModePopup` `QPr`(3100) · `RankedEventWindow` `QBg`(3104) · `SkirmishEventWindow` `QBg`(3104) · `SearchingMatchPopup` `QSr`(3130)，与文件尾那块的 ✅ 一致；本件**零自检**（简报口径），没跑 `ShadeRuleOk`。
3. **站点总数我故意一个数都没写进文档**（见 §三·1 #2）：文件尾写 21、我实测 23 ⇒ **两个数已经不一致，我不再添第三个**。本件只在表里标 ✅，把数留给调度台一处定。

---

## 五、顺手发现（⛔ 只报不改）

1. 🔴 **类型检查现在被 `Battle/BattleDriver.cs` 挡死**（**不是本件引入的**）：两遍都报同一对错 ——
   `BattleDriver.cs(462,17)` 与 `(545,13)`：**`CS7036`：「未提供与 `BattleDriver.BeginFromPendingCore(NetPendingBattle, bool, string)` 的必需形参 `deckNote` 对应的参数」**
   ⇒ **运行时程序集编不过** ⇒ 编辑器程序集连带 `CS0006`。看着像**别人正在给该方法加 `deckNote` 形参、两个调用点还没跟上**（别的写手的活）。
   ⛔ 我没碰它（不在白名单）。**但这意味着：现在任何人跑类型检查都拿不到干净的编辑器侧结论。**
2. 🟡 **`grep` 计数口径要留意**：`grep -rn "MenuDraw\.ShadeHit(" --include=*.cs`（`Assets/CardPresentation/` 下）现读 **27 条**，其中 **4 条是注释里的散文提法**（`Editor/{CollectionScene,MainMenuScene,RewardsScene,ShopScene}.cs`，写的是 `MenuDraw.ShadeHit(..., name)` 这个形参）⇒ **数「站点」时要么剔注释、要么换判据**（同 `Shell/BoosterPackOpenWindow.cs` 那个「`:95` 与 `:310` 只算一处」的坑是一族）。
3. ⚪ **A335 的截断代价已被量化**（见 §二·1 末）—— 建议调度台在派「核 base」的活时**优先用 `--md`**（那边本来就不截断）。
4. ⚪ **简报里两处出处指错**（我在两个文件里写的都是**正确的那一节**）：
   · A347 的判据在 `WA_文档与注释四件.md` **§六·3**（简报写 §六·2；§六·2 讲的是「本件改了 `BoosterInfoPopup.cs` ⇒ 别处的行号又漂了一格」）；
   · A349 在 `FX1_Battle七条红修复.md` **§五·2**（简报写 §四·2；§四·2 讲的是「`notBuilt`／节点数一个都没写死」）。
5. ⚪ **`待办判据_阶段二与联机.md` 的 markdown linter 噪音是既有的、不是我引入的**：全文件 11 条表分隔行**全是** `|---|---|` 紧凑体例（MD060 会对其中若干报「pipe is missing space」）· `MD028`（引语块之间夹空行）在 `:16`/`:517` 早有先例 ⇒ 我按本文件既有体例写，**没有引入新形态**。
6. ⚪ **`SameObject` 的 0.05 是「同一条目」的判定、不是「找对象」的容差** —— 这条区分我写进了 `FindBuilt` 的注释；同族若还有人把两者当一回事，照这条看（`EnvironmentApplier.cs` 那边**已经写对了**，不用改）。

---

## 六、改动清单（文件:行号 · 每处一句为什么）

| # | 文件 | 现读行号 | 改了什么 / 为什么 |
|---|---|---|---|
| 1 | `Unity/工具/menu_dump.py` | **`:7`** | docstring 的「第 0 层要哪些格子」那行补上 `m_fontSizeBase`（**一列**）⇒ 工具的自述与输出一致 |
| 2 | 同上 | **`:592-604`** | `describe()` TMP 分支加注释：**为什么非印 base 不可**（三条判据 + 两处刻意口径：与 `auto[…]` 不同档 · 印在 `字号=` 之后） |
| 3 | 同上 | **`:605-606`** | 新增两行：`if 'm_fontSizeBase' in mb: s += f' 基准={…}'`（**`auto[…]` 的追加条件一个字没动**） |
| 4 | `Unity/资料/待办判据_阶段二与联机.md` | **`:442`** | ⑥ 正文的「（全工程 7 处）」**就地标成已过期**（S5 §二·4 点名的那个数） |
| 5 | 同上 | **`:446-452`** | ⑥ 之后加 ✅ 回填块：**本条已落地** + 公共件 = `MenuDraw.ShadeHit`（**落点不是裁断里写的 `MenuWindowBase`**）+ 「数只在本节末尾那两块」+ 行号按符号认 |
| 6 | 同上 | **`:456-457`** | 裁断块里那两句「三种编码」的现读值：`QShade + 1` / `QBase − 1` ⇒ **划掉 + 标已订正成 `QShade`(3150) / `QShade`(3170)** |
| 7 | 同上 | **`:458-459`** | 追加一句：**三种编码已收敛**（三处现读都在 helper 上）+ 落点更正指向 ⑥ 的回填 |
| 8 | 同上 | **`:478-480`** | 表**上方**加 ✅ 横幅：本表是 2026-10-04 的派活表、9 行已落地、**总数别在本表记**（= S5 建议的「把横幅提到表上面」） |
| 9 | 同上 | **`:488-489`** | 那 4 处 `BackdropHit` 的「**档位是否合规矩还没逐处核**」⇒ 标成**已核完并收口**（不写新数，指向文件尾那块） |
| 10 | 同上 | **`:491-501`** | 表头 + 分隔 + 9 行**新增第 6 列「✅ 落地（2026-10-12 回填·A347）」**：逐行 ✅，那 4 处另给**现读档号** |
| 11 | 同上 | **`:503-505`** | 表下加「✅ 落地那一列的**出处**」+ 「站点/现编码两列的行号早已漂，按符号认」 |
| 12 | 同上 | **`:508-509`** | 表后那句「⇒ 4 处现行档就是错的…」**标成「当时」** + 4 处现读新档号（`3105`/`3125`/`3150`/`3170`） |
| 13 | `Unity/MyGame/Assets/WarpforgeArena1/Editor/ArenaBuilder.cs` | **`:2658-2667`** | `FindBuilt` 的 doc：**删掉「容差与 `SameObject`（0.05）一致」**（与实现不符），改成「**没有任何容差、同名取最近**」+ 写明两处用途不同（并表要严 / 找对象取最近）+ 更正痕迹 + 来件 §五·2。**实现一个字没动。** |

---

## 七、跑过的检查

| 检查 | 手段 | 结果 |
|---|---|---|
| A335 真的印出 base | 实跑 `python 工具/menu_dump.py bundle_menus_assets_all "Campaign Tab" --depth 4`（文本模式） | ✅ 10 行带 `基准=`；`ULTRAMARINES`/`Points: 69`/`Quantity` 三条与 V7 逐值吻合 |
| `--md` 也印 | `… "Player Profile Window" --depth 6 --md` | ✅ 15 行带 `基准=`（`35.0` / `33.65…→基准 35.0`） |
| A347 的 9 行真在 helper 上 | `grep -rn "MenuDraw\.ShadeHit(" --include=*.cs` + 逐文件现读 | ✅ 9/9 命中；全库 raw **27**（23 真调用 + 4 条注释） |
| 行尾没被翻 | 三个文件**二进制读**数 `\r\n` vs `\n` | `menu_dump.py` **纯 LF**（0 / 3373）· `待办判据_阶段二与联机.md` **纯 LF**（0 / 853）· `ArenaBuilder.cs` **纯 CRLF**（4514 / 0 lone_lf）✅ |
| 没整篇重写 | `git diff --numstat` 三行 | `menu_dump.py +16/−1` · `待办判据…md +37/−16` · `ArenaBuilder.cs +9/−1` ✅（都远小于文件行数） |
| 表没被改坏（10-03 那个坑） | 逐行数竖线 | 表头/分隔/9 行**全是 7 条竖线 = 6 列**，`\|` 转义 **0** ✅ |
| `.cs` 能编过吗 | `TMPDIR=/tmp/wf_doc1{,b} bash 工具/typecheck.sh` | ❌ **被别人的半成品挡住**（`Battle/BattleDriver.cs(462,17)`/`(545,13)` `CS7036`）⇒ 编辑器程序集连带 `CS0006`，**本件的 `.cs` 未被编译器验到**（见 §四·1 / §五·1） |
| ⛔ 没做的 | —— | 没跑 Unity · 没跑 11 条自检 · 没动 git 写命令 · 没改正本 · 没越白名单（3 个文件 + 本报告） |

---

## 八、本件读过的判据原文（可复查）

- `资料/普查产出_1011/V7_A305_A304_普查.md` §二·1/§二·3/§五·5/§六·1（A335 的成因与建议）
- `资料/普查产出_1011/WA_文档与注释四件.md` §2·2/§3·2/§六·3（A347 的旁证）
- `资料/普查产出_1011/FX1_Battle七条红修复.md` §五·2（A349 的来件）
- `资料/普查产出_1012/S5_文档工具挂起_开账现核.md`（A335/A347/A349 三行开账 + §二·4 的「7 处」）
- TMP 源码：`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMP_Text.cs:467/473` · `TextMeshPro.cs:2149-2150`
- 代码现读：`Shell/{BattleLogPopup,BoosterInfoPopup,CardDetailPopup,DeckSelectionPopup,DuelPopupWindow,LeaderboardWindow,MissionRerollPopup,PlayerProfileWindow,BoosterPackOpenWindow}.cs` 的 `MenuDraw.ShadeHit` 调用点 · `WarpforgeArena1/Editor/ArenaBuilder.cs` 的 `FindBuilt`/`SameObject` · `Battle/EnvironmentApplier.cs` 的 `FindNearest` 注释
