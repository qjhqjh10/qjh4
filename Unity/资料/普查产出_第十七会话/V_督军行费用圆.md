# V · 督军行费用圆（只改 `Deck/DeckRuntime.cs` 一处）

> 执行代理 `V-督军行费用圆`。**没跑 Unity**（红线）· **没动 git** · 只改一个文件。行尾复核 `crlf 6999 / lf 6999`（**纯 CRLF、lone LF = 0，没被翻**）。`git diff --numstat`（本文件）= **183 / 15** —— ⚠️ 含上一轮 `T` 尚未提交的 151/13 ⇒ **我这一笔 = +28 / −1**。类型检查 = **运行时 0 · 编辑器 0**。

## 结论（一句话）

督军行那颗费用圆**已藏**，而且**必须 `row_c*` 与 `row_cnt*` 两颗一起藏**；🔴 **但防御卡行【不藏】** —— 原判据里「原版督军与防御卡费用都是负的」**那半句不成立**：防御卡**卡面上真有费用六边形**（`0/1/2/3`）、原版 **Defensive 族预制体有** `Cost Image`、`Coin >= 0` 那条闸对它是**开**的 ⇒ 一起藏 = **造一条新偏离**。

## 状态 → 参数 表

| 卡种（`CardDef.Type`） | 原版预制体族 | 原版 `Coin` | 该族有没有 `Cost Image` | 费用圆（原版） | 改前 | 改后 |
| --- | --- | --- | --- | --- | --- | --- |
| `hero` 督军 | Hero | 卡面**没有费用这件东西**；表里 `0` = **我们填的** | **没有**（`:9321-9329`） | **不印**（结构上不存在，与值无关） | ❌ 开着、印 `0`（D2 第 0 行） | ✅ **藏**（两颗都关） |
| `defence` 防御卡 | Defensive | **0 / 1 / 2 / 3**（真值，卡面印的） | **有**（`:9337`，含子件 `Cost`） | `Coin >= 0` ⇒ **印** | 开着、印 `1`（D2 第 1 行 `支派圣殿`） | ✅ **保持开**（未改） |
| 普通 `unit`/`tactic` | Card Info | `0…14` | **有**（`:9348`） | `Coin >= 0` ⇒ **印** | 开着 | 不变 |

**为什么督军与防御卡要分开判** —— 两条走**不同机制**、覆盖面**不重叠**：① **督军**靠**预制体结构**（Hero 族**根本没有 `Cost Image` 这件子件** ⇒ 那颗**不存在**，与费用值无关）；② **防御卡**靠**运行期值闸**（Defensive 族**有** `Cost Image`，开不开由 `coin >= 0` 定；它费用 ≥ 0 ⇒ **开**）；③ ⇒ 原稿「判据 3 覆盖两支 ⇒ 两支恒藏」是**把前提当结论** —— 前提「防御卡费用为负」**我查不到任何出处**，且被下节三处证据否掉。

## 改动清单（`Unity/MyGame/Assets/CardPresentation/Deck/DeckRuntime.cs`）

| 行号 | 改了什么 | 判据 |
| --- | --- | --- |
| `3143-3172` | 新增整段判据 doc（含「督军 / 防御卡分开判」的理由） | 见下 |
| `3173` | 新增 `bool heroRow = on && shown[firstRow + i].Type == "hero";` —— **按卡种判、不按下标**（行会随滚动换卡） | ② ③ |
| `3176` | `SetOn(_deckRowCost[i], on)` → **`on && !heroRow`**（蓝六边形 = 原版 `Cost Image`） | ② |
| `3178` | `SetOn(_deckRowCount[i], on)` → **`on && !heroRow`**（圆里那个数 = 原版 `Cost`） | ② ③ |

🔴 **为什么必须两颗一起改**：`row_c*`（`:1413`）与 `row_cnt*`（`:1441`）在本工程是**两颗独立物体**（原版是**三套预制体**、我们一个模板复用）—— 只关前者会在督军行上留一颗**飘着的 `0`**。

三条判据（独立）：

1. **参考图** `C:/Users/qjh36/Desktop/warpforge参考图/卡组编辑界面参考.png` 第 0 行 = 督军：**只有名字、字更大、无费用圆、无 `x2`**（本轮自己裁图复看 → `_tmp_view/vc/ref_col.png`）。
2. **prefab 三族、按卡种选**：`DeckListDrawer__SpawnCardInfo.c:29/:40/:51`（`card+0x94==10` ⇒ Hero · `==0x14 && card+0x90==0xd2` ⇒ Defensive · 否则 Card Info）；`Deck Selector Hero Card Info button/Content` **无** `Cost Image`（`资料/说明书/04_界面UI/菜单全树.md:9321-9329`），`Deck Selector Defensive Card Slot`（`:9337`）与 `Deck Selector Card Info button`（`:9348`）**都有**。
3. **值闸** `UICardInfoItem__Initialize.c:76`（同 `SetCost.c:26`）= `SetActive(obj, coin >= 0)` = `(uint)*(int*)(card + 0x78) >> 31 ^ 1`；`card + 0x78` = `RawCardScript.manaCost`（`资料/AI_原版反编译_0917.md:42`）。

## 防御卡那行的费用 —— 现读实测（**不是推的**）

**我们表**（`Unity/MyGame/Assets/RuleEngine/Resources/cards_engine.json`，当前卡池 **1126** 张）：

- `hero` = **56 张，`cost` 全 `0`** · `defence` = **39 张 = `0`×33 · `1`×4 · `2`×1 · `3`×1**
- 全池 **`min(cost) = 0`**、**没有任何一张是负的** ⇒ `cost >= 0` 在我们这份数据上**恒真**

**原版侧**（卡面 = 印刷品上的字，铁律 7；三张本轮亲读）：

| 卡图 | 卡面右上 | 表里 `cost` |
| --- | --- | --- |
| `d:/2/Warpforge部队卡片/Aeldari/1督军/Warpforge_1_Anvirr-Keltoc.png` | **只有阵营徽记、没有费用六边形** | 0（**填值**） |
| `d:/2/Warpforge部队卡片/Aeldari/5防御卡/Warpforge_65_Inspiring-Legacy.png` | **有**蓝色六边形 = **`0`** | 0 |
| `d:/2/Warpforge部队卡片/Aeldari/5防御卡/Warpforge_67_Aspect-Shrine.png` | **有**蓝色六边形 = **`1`** | 1 |

⇒ 正好是「**督军没有费用这件东西 · 防御卡有真费用**」。**为什么督军 `cost` 是 `0` 而不是负的**：那是**我们填的、不是原版值** —— `Unity/工具/gen_cards_engine.py:314-333`：13 阵营 **56 张督军卡图逐张**核过，**56/56 右上没有蓝色费用六边形**。🔴 **这正是不能照抄 `Coin >= 0` 的原因**：在我们这份数据上它**恒真、一颗都藏不住**。

**我们实拍对照**（`_tmp_view/play17/D2_deck.png`，裁图复看 → `_tmp_view/vc/our_col.png`）：第 0 行 = `安维尔·凯尔托克`（`hero`，印着 `0` ← **本件要修的**）；第 1 行 = `支派圣殿` = `Aspect Shrine`（`defence`，印着 `1`，与表里 `cost = 1` 一致）。

## 没做到的（为什么）

- **没实拍「改后」**（红线：不跑 Unity）⇒「藏住了」是**静态可推**的（`SetOn` → `SetActive`），**屏上没验**。
- **防御卡那行仍差最后一步硬证据**：参考图那副演示牌**没有防御卡**（第 0 行督军后**直接**是 `游侠 x2`）⇒ 参考图**证不了**防御卡行有没有圆。我用的是「prefab 有 `Cost Image` + 卡面有真费用 + `Coin >= 0`」三者合推。⚠️ 若另有判据说防御卡那颗也恒藏，改法**只有一处**（代码 `3167-3168` 已写明）。
- 原稿判据③那句「（原版督军费用为负 ⇒ 恒藏）」**我没找到出处** ⇒ 按「未查实」处理，⛔ **没照它改**。

## 我建议加的断言（逐条含「改回什么 ⇒ 红」；我不写 —— `Editor/` 主对话独占）

1. 🔴 **主条（现状：这一处【一条断言都没覆盖】）**：先断前提 `_rt.UiDeckRowAt(0).Type == "hero"`，再断 `_rt.UiQuadActive("row_c0") == false` **且** `FindDeep(_rt.Root, "row_cnt0").activeSelf == false`。⇒ **红法**：把 `RefreshDeckList` 的 `on && !heroRow` 改回 `on` ⇒ 立刻红。🧨 **必须断 `activeSelf`，⛔ 不能只断文本** —— 既有的 `DeckScene.cs:2987-2990`（`UiLabelText("row_cnt"+i) == d.Cost`）读的是 `Label.Text`、**不看 `activeSelf`**（`DeckRuntime.UiLabelText`，`:4801-4812`）⇒ 它对这条缺陷**完全无感、改前改后都绿**。
2. **配一条反向的（鉴别力）**：挑 `defence` 行（D2 第 1 行，`cost = 1`），断 `_rt.UiQuadActive("row_c1") == true`。⇒ **红法**：把防御卡也一起藏（改成 `!(heroRow || rowDefence)`）⇒ 红。（① 与 ② 是一对：**只留 ① 的话，「整块全关」也蒙得过去**。）
3. **期望值钉在原版、⛔ 别读我们的常量**：①② 注释各带 `菜单全树.md:9321-9329` / `:9337` 出处。
4. ⚠️ 建议顺手给 `DeckScene.cs:2987` 那条既有断言补一句「`row_cnt` 的可见性」—— 它现在只验「文本对不对」，**「该藏的行藏没藏」整个是空白**。

## 顺手发现（⛔ 没改，按红线只报）

1. ⚠️ **原版那句 `SetActive` 打的是 `Cost`（数字 TMP）自己那颗 GameObject、不是 `Cost Image`**（`UICardInfoItem__Initialize.c:73-76`：`get_gameObject(param_1 + 0x28)`，而 `+0x28` 是 `set_text` 的受体）⇒ 严格讲原版只关**数字**、六边形还留着。参考图第 0 行两样都看不见 ⇒ 说明**督军那边根本不靠这一句**（Hero 族压根没这件子件）。🔴 **建议记账**（不在本件范围）：原版「负费用」的卡在**普通 / 防御族**上到底关掉哪一颗，我**没查到** —— 原版数据在远端，本地造不出一张负费用的卡来实跑。
2. 🔴 **`DeckRuntime.UiLabelText`（`:4801-4812`）不判 `activeSelf`** —— 正对上本仓那条「凡『该显示 / 该藏』都要断 `activeSelf`」：**它是那类断言最容易漏的口子**。
3. **同一模板上「只跟 `on` 走」的件都值得重看**：`_deckRowCount[i]` 一直跟 `on` 走，是靠「恰好和圆同步」蒙对的；本件把它绑上 `!heroRow` 后两者才真正同源。本轮未发现第二处。
4. `Deck/DeckRuntime.cs` 在盘上**未提交**（含上一轮 `T` 的 151/13）⇒ 看 `git diff --numstat` 时，只有 **+28 / −1** 是这一笔。

## 类型检查读数

`TMPDIR=/tmp/wf_v bash d:/4/Unity/工具/typecheck.sh` ⇒ `--- 运行时程序集 --- 运行时错误数: 0` · `--- 编辑器程序集 --- 编辑器错误数: 0`（改完立刻跑；两次都是 **0 / 0**。**无**「错误全在别人文件上」的情形。）
