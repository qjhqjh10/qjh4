# F · 断言 · DeckScene（A1497④ 卡池 + 通配符计数条的两页判据）

**只改了一个文件**：`Unity/MyGame/Assets/CardPresentation/Editor/DeckScene.cs`（`+191 / −0`）。
⛔ 没碰其它任何文件（`Deck/DeckRuntime.cs` 一个字没动）· ⛔ 没跑 Unity · ⛔ 没动 git。

⚠️ **简报里写「LF · 6900 行」是错的** —— 现核（二进制读）本体是 **CRLF**、改前 **6697 行**。
改完再核：`crlf 6887 == lf 6887`（无孤立 LF），`git diff --numstat` = **191 / 0**（没有整篇翻行尾）。

---

## 加了哪几条（在本文件哪一行 + 一句话）

**A. 5 个只读助手（`:191`–`:270`，放在既有的 `FindDeep` 后面）**

| 行 | 名字 | 一句话 |
|---|---|---|
| `:210` | `PoolCellGo(i)` | 卡池第 i 格的视图根（`Root` 下的 `pool_<i>`），doc 里写明**为什么不能用 `UiQuadActive`** |
| `:224` | `PoolCellsShown()` | 数 `Root` 下 `pool_*` **直接子件**里 `activeInHierarchy` 的格数（`poolbar_*` / `poolcnt_*` 天然被排除） |
| `:241` | `WcBarNodes` | 通配符计数条 **10 件**的节点名（底板 1 + 图标 4 + 数字 4 + 阵营徽记 1） |
| `:249` | `WcBarQuadNodes` | 上面那 6 件属于 `ImageQuad` 的（`UiQuadActive` 量得到的） |
| `:254` / `:265` | `NodeShown(name)` / `WcBarShownCount()` | 按名字取 GO 读 `activeInHierarchy`（`Label` 也认），再数 10 件里现在真显示着几件 |

**B. 断言一整节（`:2842`–`:2949`，在 `TestLayout` 里、紧接既有的 `UiInfoOnlyActive` / `UiCosmOnlyActive` 那两条之后）**

| 格 | 行 | 断什么 |
|---|---|---|
| ① Cards 页（基线） | `:2859`–`:2871` | `pool_0` 视图在且 `activeInHierarchy` · `UiPoolCardAt(0) != null` · `PoolCellsShown() > 0` · `UiPoolCellRect(0)` 量得出来（记下 `bx/by/bw/bh`，② 要拿它比） |
| ② **Deck info 页（本活正主）** | `:2874`–`:2900` | `pool_0` 也 `activeInHierarchy` · `UiPoolCardAt(0) != null` · **格数 == Cards 页那个数** · `UiPoolCellRect(0)` 量得出 · **与 Cards 页矩形同值（同一处）** |
| ③ Cosmetics 页 | `:2903`–`:2926` | 卡池格数 **== 0** · `UiPoolCardAt(0) == null` · `pool_0` 关着 · 通配符条 **0/10 件** · 6 件 quad 逐个 `!UiQuadActive` · 4 件 Label 逐个 `!NodeShown` |
| ④ 回 Deck info 页 | `:2931`–`:2941` | 通配符条 **10/10 件全显示** · 6 件 quad 逐个 `UiQuadActive` · 4 件 `hdr_wct*` 逐个 `NodeShown` |
| ⑤ 切回 Cards 收尾 | `:2944`–`:2948` | 格数回基线 · 通配符条 10/10 · `ActiveTab == 0`（下一节按 Cards 页跑） |

共 **38 条**断言。每格开头都 `UiSetTab(0)` 摆回基线（铁律：上一格切到 Cosmetics 的残留会漏到下一格）。

---

## 每条「改回什么 ⇒ 这条红」（逐条验收判据）

| 把什么改回去 | 哪几条会红（其余格**故意保持绿**，我不虚报） |
|---|---|
| **`DeckRuntime.cs:1958` 把 `CardDisplayPage` 改回 `_tab == 0`**（= 本次修的那一处） | **② 全 5 条红**（`pool_0` 不 active、`UiPoolCardAt(0) == null`、格数 0≠nPool、`UiPoolCellRect` 恒 false、同矩形比较红）。①③⑤ 绿 —— 旧写法在 Cards/Cosmetics 两页本来就是对的那一侧（这正是「只断 Cosmetics 会假绿」的原因） |
| **`:4473` 把 `CardDisplayPage` 改成恒 `true`** | **③ 4 条红**（格数 ≠0 · `UiPoolCardAt(0) != null` · `pool_0` 还亮着 · 通配符条 10≠0） |
| **`:1958` 把 `on` 写成恒 `false`** | **① 4 条红**（active / `UiPoolCardAt` / `nPool > 0` / 矩形）＋ **② 全 5 条红** |
| **`:4485` 把 `_cardsInfoOnly` 那一句改成 `_tab == 1`**（Info-only，最像「顺手写错」的那一种） | **⑤ 的 `WcBarShownCount() == 10` 红**。③④ 绿（Cosmetics 上它确实关着、Info 上确实开着）⇒ **只有 ⑤ 这一格抓得住它**，这就是 ⑤ 存在的理由 |
| **`:4485` 改成 `_tab == 0`**（Cards-only） | **④ 全 11 条红**（1 个计数 + 6 quad + 4 Label）。③⑤ 绿 |
| **`:4485` 那一句整个删掉**（= 通配符条永不关） | **③ 全 11 条红**（1 个计数 + 6 quad + 4 Label） |
| **逐件漏登记**（少登记任一件进 `_cardsInfoOnly`，如删掉 `:1127` 那句 `_wcTxt[i]`） | **③ 对应那一件红**（`!NodeShown("hdr_wct<i>")`）＋ **③ 的 `WcBarShownCount() == 0` 红** |
| **把 10 件里的某一件改成不建**（如 `hdr_army` 喂一张不存在的图 ⇒ `Img` 回 null） | **④ 的 `WcBarShownCount() == 10` 红** ＋ ④ 对应那件红（**不会静默**） |

🧨 **灭自证**：所有「露着没有」都读**画面**（`activeInHierarchy`），⛔ 没有一条去读 `_cardsInfoOnly`
那张表 —— 读它就是把被测实现的账本当检测器（两边一起改回去照样绿）。
「格数 == Cards 页那个数」这条也是为此而设：只断「`pool_0` 活着」的话，一个「只补开第 0 格」的实现照样绿。

---

## 没做到的（为什么）

1. **简报第 3 条（可选）「Info 页 `UiClickPx(某格中心)` ⇒ `State.Deck.CountOf(def)` 变了」—— 做不到，且不是我偷懒**：
   卡池的点击**只从 `HandlePointer` 进**（`DeckRuntime.cs:3637`），而 `UiClickPx`（`:4880-4888`）走的是
   `HandleButtons` / `HandleFilterClick` / `HandleCosmoFltClick`，**没有卡池那一支**；`UiPressAt` 走 `UpdateButtonPress`、也不是那条路。
   `DeckRuntime.cs:4150` / `:4989` 两处自己写着「**批处理里驱动不了 `HandlePointer`（没有鼠标）**」。
   ⇒ 要真点一下，**得在 `DeckRuntime.cs` 开一个自检口**（= 简报的 ⛔ 清单点名不许碰的文件）。
   **替代**（做了）：把命中这一格的**三个前提**钉住 —— 视图 `activeInHierarchy`（②）✚ `_poolIndex[0] >= 0`（②）
   ✚ 矩形与 Cards 页**同值**（② 第 5 条，`HandlePoolClick` 用的就是**同一颗** `pool_0` transform 做 `Contains`）。
   ⚠️ **诚实标注**：这**不等于**「真点到了」—— 它是前提齐了，不是那一下。
2. **没跑 Unity**（简报红线）⇒ 这 38 条**没有实跑过**，只过了类型检查。真正的验收要等同步点那次 `DeckScene.Run`。

---

## 顺手发现（尤其别的「恒绿 / 恒红」读数）

1. 🔴🔴 **简报（和写手 E 的报告 `E_信息页卡池.md:143,145,146`）建议的 `UiQuadActive("pool_0")` 是【恒假】的** ——
   卡面的每一层都是 **`MeshRenderer`**（`Core/CardView.cs:2290-2307` 的 `AddLayer` 建的是 `MeshFilter + MeshRenderer`），
   一棵 `CardView` 里**唯一**的 `ImageQuad` 是 `SetTargetGem` 那条路建的 `glow_melee/glow_ranged`
   （`Core/CardView.cs:3634,3658-3676,3678-3681`：出厂 `SetActive(false)`，而且卡池的卡**永远不**调 `SetTargetGem`）
   ⇒ `UiQuadActive("pool_N")` 恒答 false。
   **若照抄那份建议**：Cards / Info 两页的「露着」断言会**恒红**，而 Cosmetics 页那三条 `!UiQuadActive` 会**恒绿**
   —— 正是本活要防的那一族，而且是「一半恒红一半恒绿」的双份。
   （我用 `activeInHierarchy` 替代，并在 `:207-209` 的 doc 里写死理由，防下一个人再抄回去。）
2. 🔴 **简报里那句「或既有的 `UiWcCounterRect`」同样是恒绿口**：`DeckRuntime.cs:4797-4804` 只读
   `_wcTxt[i]`（一个序列化字段引用）的 `localPosition / WorldW / WorldH`，**完全不管它 `activeSelf`**
   ⇒ 拿它断「Cosmetics 页上数字不显示」**恒真**。我改用 `FindDeep` + `activeInHierarchy`（= `NodeShown`）。
3. 🔴 **既有的「该藏的藏住了吗」通用守卫盖不住这一批**：`UiInfoOnlyActive` / `UiCosmOnlyActive`
   （`DeckRuntime.cs:5140-5151`）读的是 `_infoOnly` / `_cosmOnly`，而本批新加的 `_cardsInfoOnly` **不在任一张表里**
   ⇒ `DeckScene.cs:2839-2840` 那两条（Cards 页零泄露）**对通配符计数条恒绿**。
   这正是我把 ③/④/⑤ 三格都写全的原因（Cards ✚ Info 都开、Cosmetics 关，三页缺一页就是假绿）。
4. ✅ **`PoolCellsShown()` 的过滤前缀是安全的**：张数条那两族是 `poolbar_*`（`DeckRuntime.cs:2067` 的 `Img`）
   与 `poolcnt_*`（`:2070` 的 `TxtPx`），都以 `poolb` / `poolc` 开头，`StartsWith("pool_")` **不会**误收它们。
5. ℹ️ 与 `UiQuadActive` 同族的「找不到就静默答 false」风险，本宿主里**老账已收过**（`:1980` / `:2449` / `:4611` 那几段
   注释就是 A57① / A190 / A77⑭ 三次补 `FindDeep` 兜底的记录）—— 我这两件新读数**从第一版就带兜底**（`Root.Find` → `FindDeep`）。

---

## 类型检查读数

`TMPDIR=/tmp/wf_f bash d:/4/Unity/工具/typecheck.sh`（两次，改助手块后 + 改断言块后，**两次都 0**）：

```
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

`git diff --numstat -- …/Editor/DeckScene.cs` = **191 / 0**；二进制核：`crlf 6887 == lf 6887`（CRLF 未被翻）。
