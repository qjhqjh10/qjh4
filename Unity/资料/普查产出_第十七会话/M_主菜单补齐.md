# M_主菜单补齐（执行代理报告 · 2026-10-11）

> 只改了一个文件：`Unity/MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`（LF 未翻）。
> 未跑 Unity · 未动 git · 未改正本 · 未碰 `Editor/`。

## 结论（一句话）
①「等级徽章」②「信箱钮」**都已经建好了**（都在共用件 `Shell/TopBar.cs`，不在我的白名单里 ⇒ 一个数没动）；
⑥「选中态高亮」**建了、但一个矩形都没画出来**（与导航底板同渲染队列被盖住）—— 修正落在白名单内的唯一那一处；
追加的 ⑦ 两条（`小冲突` 满高 + 不留洞）已做完。`typecheck` 两读数 0/0。

## 逐件对照表（表里的值 → 我们代码里的值）

### ① `Player Level`（等级徽章）—— **已存在**（`TopBar.cs`，非我文件）
| 项 | 权威表 §二·2 `:111/:112` | 我们（`Shell/TopBar.cs`） |
|---|---|---|
| 节点/rect | `Player Level` a(.5,.5) pos(-120.3,24.7) sz(53.12²) | `:269` `New(p,"Player Level",53.12,53.12)`；图格 `:270` = 117.5–170.6 × 54.3–107.4，与表值反推的 117.47–170.59 × 54.28–107.40 **逐值吻合** |
| 图 | `40k_topmarquee_currency_gold` · active=True | `:270` 同一张 ✅ |
| 子 `Player Level Text` | a(0,0)-(1,1) sz(-13.72,-13.72) · 无图 · 内容 `-` | `:272` `Text(lvl,"-",124.3,163.7,61.1,100.5)` = 39.4×39.4 ✅（53.12−13.72=39.4）· autosize `[18~37.2] base 36.89` ✅ |
| 等级号 | 原版出厂就是 `-`，运行时由**服务端**灌（参考图上是 `28`） | 我们恒 `-` ✅ **但没有出声** ⚠️ |
⇒ **这一件是对的、不需要补**：审计记的「缺等级徽章」= 参考图 `28` vs 我们 `-`，而 `-` 正是出厂值（服务端数据）⇒ **不是复刻缺口**。

### ② `InboxBtn`（信箱）—— **已存在**（`TopBar.cs`，非我文件）
| 项 | 权威表 `:92/:93/:94/:96` | 我们 |
|---|---|---|
| 容器 `TopBarButtons` | pos(581,-35.5) sz(311.4,71.33) · HLG sp 9.75 · align 3 | `:148` 311.4×71.33 ✅（HLG 已解算成绝对坐标，前导 = 425.3） |
| `InboxBtn` | 55×40 · `40K_notification` · True | `:149-151` = 425.3–480.3 × 15.5–55.5 ✅ |
| · `Badge Highlight` | a(.5,.5) pos(19,16) sz(35,35) · `40K_notification_number`+CG | `:155-156` = 454.3–489.3 × 2.0–37.0 ✅（= InboxBtn 中心 +(19,+16) 反推值，逐值吻合） |
| `Challenge button` | 47.46² · `40K_icon_duel` | `:157` 490.1–537.6 × 11.8–59.2 ✅（我们本来就对） |
| `Feedback Button` | 50×50 · `40K_icon_feedback` · **出厂 False** | ❌ **没建**（`:159` 只有一行「不建」的注释）⇒ **要建、保持关** |
⇒ 审计记的「少了信箱」= 它建了，但 **85% 压在名牌底板底下**（见「顺手发现 ②」）。

### ⑥ `Selected highlight`（选中态高亮）—— **已存在，在我这个文件里**
| 项 | 权威表 `:138-143` | 我们（改前） |
|---|---|---|
| 节点/rect | 子件第 1 颗 · a(.5,.5) pos≈0 sz(164.3,169.7)（满颗） | `MainMenuRuntime.cs:550`(旧) `-0.3..164.0 × y1..y2` ✅ 满颗 |
| 图 / 色 | `40k_main_bt_selected BW`（表未给色） | `40k_main_bt_selected_BW`（导入器把空格换下划线）✅ `Resources/Art/ui_deck/` 里 71²、灰 148、alpha 145–255 · tint `(1,0.08054,0,1)` ✅ |
| 显隐 | 只有当前页那一颗亮 | `if (idx == SelectedNav)` ✅ 语义已对 |
| 档位 | —— | ❌ `QPanel`(2910) = **与导航底板同一个数** ⇒ 谁盖谁不确定 ⇒ **实测没画出来** |

## 改动清单（文件:行号 —— 改了什么 —— 判据出处）
1. `Shell/MainMenuRuntime.cs:238-267` —— **新增** `const int QNavHighlight = QPanel + 1;`（带长注释）—— 判据 = **原版兄弟序**（`Navigation Panel` 子件序 `Background→Panel Shadow→Separators→Buttons Container`，而高亮是**按钮的第 1 颗子件** ⇒ 必须在底板之上、在按钮自己的 `QContent` 之下）+ 同款先例 `Deck/DeckRuntime.cs:1223`（页签高亮 `QPanel 3004 > QSide 3000`）。
2. `Shell/MainMenuRuntime.cs:583` —— `Selected highlight` 的档位 `QPanel` → `QNavHighlight` —— 同上。
3. `Shell/MainMenuRuntime.cs:1381-1460`（`BuildGameModes`）—— ⑦ 两条 + 排布改 first-fit（判据见下）。
4. `Shell/MainMenuRuntime.cs:1375-1379` —— 就地订正那句已过期的话（原文「**只有 `Draft` 那一条有原版出处**」；现读 prefab 目录，Skirmish/Ranked 也有）。

## ⑥ 的显隐判据：读到了什么 / 没读到按什么做的
- 🔴 **反编译/组件字段那一条我【没读】**（简报说可以读 `toggle/toggleType=0` 与那颗脚本 MB）—— 因为现读代码发现**我们早就是「只有当前页那一颗画」**（`:581` 起 `if (idx == SelectedNav)`），与简报给的实拍结论一致 ⇒ 改它没有收益。**如实标：没读到、也没改。**
- **实测（本条的真发现）**：真机实拍 `_tmp_view/play17/S1_menu.png` vs 用户参考图 `主界面参考图.png`，逐像素量同一处条带：
  · 我们：选中那颗（PLAY）左缘 `(97,29,6)` vs 未选中四颗 `(95,25,8)` ⇒ **逐点几乎相同**；右缘 `(129,42,30)` vs `(119,13,3)`（那点差是**图标**：PLAY 图标 156.3 宽 vs 其余 140）。
  · 原版：`(116,14,2)` vs `(46,9,8)` ⇒ **2.5 倍，一眼可辨**。
  · 再拿「高亮本该有的颜色」≈ `(148,44,0)` 全图扫：PLAY 那一整块只命中 **47 px（0.1%）**、且都聚在图标刀身上 ⇒ **高亮一个矩形都没画出来**。
- **根因**：与 `Navigation Panel/Background` **同用 `QPanel`**，两件 z 也都是 0 ⇒ 本文件自己 2026-09-22 就记着「同一个队列 + z 都是 0 ⇒ 谁盖谁完全不确定」⇒ 底板赢了。**改法 = 单开一档**。
- ⚠️ **诚实标注**：这条根因是**由「同队列不确定」这条已知规律 + 实测现象推的，没有跑 Unity 验过**（执行代理不许跑）⇒ 记成「**已改、待实拍复核**」。**改坏法**：`QNavHighlight` 改回 `QPanel` ⇒ 症状就是这一轮的现象（**复跑自检仍会绿**，因为现有断言只量矩形、不量可见性 ⇒ 所以下面第 1 条断言必须加）。

## ⑦ 尺寸档与排布（改前档位 → 改后档位）
| 卡 | 改前 | 改后 | 判据 |
|---|---|---|---|
| Tutorial | 1×1 · col0/row0 `(205,182.8)` | **不变** | 简报：保持 1×1 |
| Draft | 1×2 · col1 `(760,182.8)` | **不变** | 简报：已对、别动 |
| Practice | 1×1 · col2/row0 `(1315,182.8)` | 1×1 · **col0/row1 `(205,617.2)`** —— 填掉 Tutorial 下面那个洞 | 不留洞 |
| **Skirmish（小冲突）** | 1×1 · col2/row1 `(1315,617.2)` | **1×2 · col2 `(1315,182.8)`**（h 414.4→**848.8**） | prefab 名 **`Skirmish Ranked Game Mode Container 1x2`**（`bundle_menus_assets_all/GameObject/` 里逐字如此，我现读了目录）+ 参考图（左起第 1 张「小冲突」是满高） |
| **Ranked** | 1×1 · col3/row0 `(1870,182.8)` | **1×2 · col3 `(1870,182.8)`**（h 414.4→**848.8**） | prefab 名 **`Ranked Game Mode Container 1x2`**（同上逐字）+ **结构理由**：5 张里若只剩 1 个 `1×1`，总格数 = 1+2+1+2+1 = **7（奇数）** ⇒ 两行网格**必然留洞**，与「不留洞」直接打架 |
**排布规则（我按什么排的）**：`BuildGameModes` 换成 **first-fit 分配器**（`:1399-1418`）—— 按**卡片顺序**逐张找**第一个放得下的位置**：`1×2` 找**整列都空**的列；`1×1` 找**还有空行**的列（同列内先 row0 后 row1）；**已有列遍历完才开新列** ⇒ 排不下就 `Debug.LogWarning` 出声。结果 **4 列、零洞**：col0 = Tutorial(上)/Practice(下) · col1 Draft · col2 Skirmish · col3 Ranked（4 列 × 2 行 = 8 格正好放满 8 格）。
🔴 **原版真正的换列规则【没读到】**（`Content` 出厂 0 子、卡由 liveop 事件数据运行时实例化；`FlexibleGridLayout` 只有 `rows=2 columns=20 itemSize 535×414.4 spacing(20,20)` 这一组字段）⇒ **这条规则是照用户参考图排的**（那上面看不出洞），**⛔ 不是「原版就是这样」**。
**「尺寸 = 这次活动的配置、不是模式的固有属性」** —— 已写进 `:1396-1398` 的注释（旁证：`Practice` 的 `Base Game Mode Container 1x1` 与 `1x2` **两个 prefab 都在**）。

## 没做到的（为什么）
1. ① ② 的**唯一真实缺口在 `Shell/TopBar.cs`**（不在白名单 ⇒ 按纪律只报不改）：
   · **`Feedback Button` 没建**（`TopBar.cs:159` 只有注释）—— 表 `:96` 要「50×50 · `40K_icon_feedback` · **出厂 False** ⇒ 建但保持关」。
   · **① 的 `-` 没出声**（简报明确要求「如实出声」）：`TopBar.cs:272` 直接 `Text(lvl,"-",…)`，没有任何 `Debug.Log`/登记。
2. ⑥ 的**可见性没有实拍验收**（不许跑 Unity）⇒ 待复核（见上「诚实标注」）。
3. ⑦ 的**节点名没改**：`…1x1 - Skirmish` / `…1x1 - Ranked` 现在名不副实（尺寸已 1×2），但改名要动 `Editor/MainMenuScene.cs` 三处（`:1419`/`:1420` 名字表 · `:1541`/`:1542` `CheckCard` · `:1572` 的 `"Base Game Mode Container 1x1 - " + modeCases[mi]`）⇒ **不碰 `Editor/`**，留给调度台（⛔ 三处一起改）。
4. **必须由调度台改的既有断言**（⑦ 会红三条，其余保持绿）：
   · `Editor/MainMenuScene.cs:1540` Practice：`x 205+2*555 → 205` · `y 182.8 → 617.2`
   · `:1541` Skirmish：`y 617.2 → 182.8` · `h 414.4 → 848.8`
   · `:1542` Ranked：`h 414.4 → 848.8`
   （`:1397` Tutorial / `:1398` Draft **原样通过**；`:1525` Practice 的 `Hit` 仍 535×414.4 ✅）

## 我建议加的断言（逐条，含「改回什么 ⇒ 红」；我自己不写）
1. 🔴 **灭自证那条（本轮最该加的）**：不用像素，断**结构上不可能同时成立**的一条 ——
   `hl.RenderQueue > bg.RenderQueue && hl.RenderQueue < icon.RenderQueue`（`hl` = `Selected highlight` 的 quad、`bg` = `Navigation Panel/Background` 的 quad、`icon` = 该钮 `Image` 的 quad）。
   **改回什么 ⇒ 红**：① 把 `QNavHighlight` 改回 `QPanel` ⇒ `hl == bg` **必红**（这一轮的症状）；② 把它抬到 `QContent` ⇒ `hl == icon` 也红（高亮会把图标压掉）。
2. **⑦ 档位（逐张）**：`Skirmish` / `Ranked` 的 rect = `535 × 848.8` —— **改回 414.4 ⇒ 红**；反面对照 `Tutorial` / `Practice` 仍 `535 × 414.4` —— **改大 ⇒ 红**（挡「一律 1×2」）。
3. **⑦「零洞」（结构性）**：遍历 `Content` 的 5 张卡，按列号 `round((left-205)/555)` / 行号 `0|1` 打格，断「**每一个 `1×1` 都能被放进已有列的空行里、没有单格列**」。
   **改坏法**：把 Practice 手写回 col2 ⇒ 那个洞回来 ⇒ **必红**。
4. **`Feedback Button`（加在 `TopBar.cs` 那一批的宿主里）**：`FindChild(TopBarButtons,"Feedback Button") != null` **且** `activeSelf == false` **且** 图 = `40K_icon_feedback`、rect 50×50。
   **改回什么 ⇒ 红**：① 不建 ⇒ 第 1 句红；② 建了却 `SetActive(true)` ⇒ 第 2 句红。
   🔴 **必须同时断 `activeSelf`** —— `FindChild` / `GetComponentsInChildren(true)` **含 inactive** ⇒ 只断「找得到」时，「不建」与「建了但关着」**两种状态测不出区别**。
5. **① 的出声**：断一个**结构**（例如 `TopBar` 暴露 `public static bool LevelTextIsPlaceholder` / 或 `MainMenuRuntime.MissingArt` 那条路里有一条说明「等级号是服务端数据」）—— **删掉 ⇒ 红**。
   ⚠️ 别用「抓日志字符串」那种写法（本仓踩过「被测代码自己会打印的东西别当断言过滤词」）。

## 顺手发现（都没自己改）
① `TopBar.cs:159` 的 `Feedback Button` 没建（上已列）。
② 🔴 **信箱「看不见」的根因不在我这一层**：`InboxBtn` 的 x = **425.3–480.3**，而名牌底 `Player Profile/Planer Name Background` = **25.6–472.1**，档位 `QBarNamePlate(2990)` **高于** `InboxBtn` 的 `QBarContent(2987)` ⇒ **信封 46.8/55 px（85%）压在名牌底下**；实拍上名牌右端可见一条灰白窄条 = 露出来的那 15%。
   两条判据：① 原版兄弟序（`Upper bar` 子件序里 `Player Profile` 在 `TopBarButtons` **之后**）；② 参考图上**信封是完整可见的**（在名牌右端之外）⇒ 原版这**两件不重叠**，我们重叠。**要动名牌的 x/宽、还是 `InboxBtn` 的位置，我没有独立判据 ⇒ 请调度台裁（不猜）。**
③ **导航底板比原版亮 ~2 倍**（在我文件里，但**不属于这三件** ⇒ 只报）：`MainMenuRuntime.cs:477-482` 的 `White Square` 四角顶点色 → 实测（我们可见区）左缘 `(97,29,6)`、右缘 `(118,12,3)`；参考图同一处 `(46,9,8)`/`(52,5,3)` ⇒ 原版可见（右）半边是**暗**的、我们的是**亮**的 ⇒ 怀疑 `SetCornerColors` 的**四角对应**与原版 `MB1941` 的字段序不一致（横向镜像）。**判据没读到，只报。**
④ **`SelectNav` 只在自检里被调**（`OnNavClick` 不调它）⇒ 运行时高亮**恒在 PLAY**。原版那颗是 `Toggle`（点第 2 颗会不会把高亮挪过去）**我没读到**、简报也没要求 ⇒ 未动。
⑤ **⑦ 的同列上下次序与参考图相反**：参考图那一列是「**练习在上、教学在下**」，我们的 first-fit 排出来是「**教学在上、练习在下**」（卡片顺序 Tutorial 在最前）。卡片顺序 = liveop 服务端数据、本地没有 ⇒ 未动，只报。

## 类型检查读数
`TMPDIR=/tmp/wf_m bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数: 0 · 编辑器错误数: 0**（两次改动后各跑一次，都是 0/0）。
未跑任何 Unity 自检（简报禁止）· 未动 git · 只改 `Shell/MainMenuRuntime.cs`（行尾 LF，`\r\n` = 0）。
