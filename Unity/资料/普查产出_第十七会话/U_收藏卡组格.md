# U · 收藏窗卡组格 —— ⑦ 多一格「新卡组」· ⑤⑥ 两枚徽章 / 选中高光

> 执行代理 `U`。**只改了两个文件**：`Shell/MenuDraw.cs` · `Shell/CollectionWindow.cs`（都实测纯 LF，改后仍纯 LF）。
> 全部读数都是**现读**的（反编译机器码 / 解包 prefab / 存档文件 / 实拍像素）。**没改任何正本、没跑 Unity、没动 git。**

## 结论

- **⑦ = 假账，一个字都没删。** 实拍里第 2 格**不是「空位格」，是存档里一套真实的卡组**（名 `新卡组` · 0 张 · 无督军）。我们**根本没有**「多画一格」的代码（`RebuildDeckCells` 只按 `FilteredDeckIndices()` 逐套建，全文件没有 `+1`、没有 null 占位）。证据链三处互证，见下节。⇒ **该做的动作是把这条账划掉**，不是改代码。
- **⑤⑥ 判据挖到了，而且它们是【两处不同成因】**（原简报把 ⑤⑥ 当成同一处）：
  - **⑤ 两枚徽章「建了但看不见」** = 它们与 `Frame` **共用渲染队列 `q`**；同一队列内按「到相机距离」排序，
    大而居中的卡框盖住了边角的小徽章（CLAUDE.md §三 那条）。**未选中那一格也一样看不见** ⇒ 与金高光无关。
    已改：两枚徽章抬到 **`qOverlay`**（原版子件序里它们就排在 `Frame` / `Deck Name` **之后**）。
  - **⑥ 金高光糊满整格** = 收藏窗那一份 prefab 变体 **`useSelectedHighlight = 0`** ⇒ **原版这一页永远不会高亮**；
    我们传 `i == CurrentIndex()` 是**自加的**。已改：`CollectionWindow.BuildDeckCell` **恒传 `false`**。

## ⑦ 清点表 → 删了什么

| 问 | 答 |
|---|---|
| 那一格是谁建的 | `CollectionWindow.BuildDeckCell`（`:3258`）← `RebuildDeckCells`（`:3186`）← `FilteredDeckIndices()`（`:3134`，只遍历 `0..DeckCount()`）。**代码里不存在「空位格」这一态** |
| 有没有别处引用它的节点名 | 节点名 = `CollectionDeck_<原始下标>`，全仓只此一处（`grep CollectionDeck_` 1 命中） |
| 有没有断言指着它 | **没有**「格数 == 套数」这类断言（`CollectionScene.cs:1138` 只断 `>0`、`:1222` 断 `>0 && <=`）⇒ **没有断言会因为「少一格」变红** |

**三处互证（都在本地、都能复查）：**

1. **存档** `C:/Users/qjh36/AppData/LocalLow/Unity Technologies/com.unity.template.urp-blank/WarpforgeDecks.json`
   （mtime **10-11 07:08**，实拍 **13:43** 在其后 ⇒ **实拍那一刻盘上就是这份内容**）：
   `version 1 · current 0 · decks = 2` —— ① `复仇者之刃`（`WarlordId ASH1` · 30 张）② **`新卡组`（`WarlordId` 缺 · 0 张）**。
2. **运行时日志** `…/com.unity.template.urp-blank/Player-prev.log`（07:03 那一跑）自报 **「卡组数 2」**（`[DeckInfo] 接线层…` 那一行）。
3. **实拍** `_tmp_view/play17/C1_collection.png` = **正好两格**，顺序与存档逐格一致（复仇者之刃 / 新卡组）。

**那一格长这样，原版也会长这样（不是我们的画法错）：** 原版 `CollectionDeck.Config` 只在 `deck == null` 时开 `createObject`（空位态，
`CollectionDeck__Config.c:47-50`）；`新卡组` 是真卡组 ⇒ 进 `deck != null` 那一支；而它的 `deckHero == null` ⇒ `Config` 在
**设卡名之前**就 `return`（同文件 `:61-64`）⇒ 主图 / 阵营 / 模式三张 sprite **一个都不设** ⇒ 那一格**只剩卡框 + 名牌**，与实拍逐条吻合。

**它为什么会有这么一套空卡组**：我们「创建卡组」那颗钮**只建卡组、不开 `Deck Editing Menu`**（`CollectionWindow.CreateDeck()` `:3372`，已记的偏离 `A549①`）⇒ 点一下就落一套 `新卡组` 进存档。**那是另一笔账，不是「多画一格」。**

## ⑤⑥ 判据（子件序 / sprite / 色 / 队列 / 字段）

**子件序（机器级）** —— `工具/menu_dump.py bundle_menus_assets_all -2024334647938937812 --depth 4`（根 `Collection Deck`），
`Deck/` 下**底→顶**：

```
Highlight        -19.8,-10.9 -> 270.0,416.3   289.75x427.27   INACT  sprite Highlight Rounded Square 150x155 九宫73,75,73,75 Sliced 色(1,0.773,0,1)
Deck Image(mask) 11,26.1 -> 239,332.1         228x305.97      RectMask2D
  Deck Image     -49.4,6.2 -> 298.3,352.6     347.70x346.35   Image(无图)
Frame            2,17 -> 248,385              245.99x368.01   40K_bt_deck
Deck Name        20,344.2 -> 230,383.9        210.00x39.69    fs32 基准30 auto[8~32] 字距-2
Faction Icon     -10.5,273.7 -> 74,359.4       84.50x85.75    preserveAspect
Game Mode Icon   170.5,273.7 -> 255,359.4      84.50x85.75    preserveAspect
DificultyLevel   159.2,15.7 -> 244.1,99.7      84.93x84.01    INACT
```

⇒ **高光是 child[0]（最底、出厂 INACT）**；**两枚徽章排在卡框与名牌之后 = 画在它们之上**（我们两处都在 `q`）。
⇒ `Highlight` 的 sprite/色/九宫**我们是对的**（`Highlight_Rounded_Square` / (1,.773,0)），**错的只是层**。

**`useSelectedHighlight`（`A1503` 记的「判据不足」这次挖到底）：**

- (1) **唯一调用点**：`CollectionItem<object>$$Config` @ RVA **`0x15F1B10`**（`d:/2/tools/il2cpp_out/script.json` 的
  `ScriptMethod[].Address` → `GameAssembly.dll` 段表 → `capstone`）：`cmp byte ptr [rbx+0x30],0` → **非 0 才** `call [rax+0x228]`
  （= `SelectedHighlight(isSelected)`）。`+0x30` = 基类 `protected bool useSelectedHighlight`（`CollectionItem.cs:11`）
  ⇒ **字段为 0 时那一支根本进不去**。
- (2) **两个变体的字段值**（`bundle_menus_assets_all/MonoBehaviour/` 现读）：`Collection Deck`（GO `-2024334647938937812`）= **0**；
  `Collection Deck With Highlight`（GO `-7515975617742763446`）= **1**。
- (3) **旁证**：两张用户实拍**没有一格是金的**（`收藏—卡组的参考图.png` 4 格 · `更换卡组的参考.png` 12 格），
  而两张实拍里**每格都能同时看见**左下圆徽 + 右下模式徽（压在卡框/名牌**之上**）= ⑤ 的正面判据。
- 另注：`CollectionDeck.SelectedHighlight` 自己还有一道闸 `if (sortMode == 0)`（`CollectionDeck__SelectedHighlight.c`）
  ⇒ 就算 `useSelectedHighlight=1`，排序列模式下也不画。

**我没能钉死的一环（如实记）**：全包里**找不到指向这两份变体的引用点** —— `Deck Scroll View/Viewport/Content` 在这两扇窗的 prefab 里
**都没有子件**，`RecyclableScrollRect` 上也没有 cell 字段；`Collection Deck` 的 3 个实例**全在 `Deck Drawer`（`DeckAndCardbackDrawer` 那个
物品抽屉）底下、与卡组格无关**。⇒「收藏窗用哪一份」靠的是 **名字 + 两张实拍 + `A198③` 既有记录**，**不是实例级铁证**；要铁证得跑一次原版实况。

## 改了什么（三处，都在白名单内）

1. **`Shell/MenuDraw.cs:3074 / :3082`** —— `Faction` 与 `Game Mode Icon` 的队列 **`q` → `qOverlay`**，上面加 28 行判据注释。
2. **`Shell/MenuDraw.cs:2919` 起的 `selected` 文档** —— 把 (1)(2)(3) 三关判据写进去，并**就地记下还开着的那一半**（高光的层、`DcHiOff`）。
3. **`Shell/CollectionWindow.cs:3258`** —— `selected` 由 `i == CollectionData.CurrentIndex()` → **`false`**，加 13 行判据注释。

## 波及面（动了共用件 `MenuDraw.DeckCell`）

`DeckCell` 的调用点**只有两个**：`Shell/CollectionWindow.cs:3258`（收藏窗卡组页）· `Shell/DeckSelectionPopup.cs:731`（选卡组弹窗）。

- 队列梯子两边都成立：收藏窗 `QPageRow 3032 < QPageText 3033 < QPageOverlay 3034`；弹窗 `QDsRow 3126 < QDsText 3127 < QDsText+1 3128`
  ⇒ 两处徽章都**严格高于**卡框与名牌。
- 弹窗侧副作用两条，都**无可见差异**：徽章与 `DificultyLevel` 同档（区域不重叠：右上 vs 底部）；徽章与 `Hit` 的透明 quad 同档（不可见）。
- **既有断言不会红**：grep `"Faction"` / `"Game Mode Icon"` / `Highlight Rounded Square` 的断言只断「节点在不在 / 图对不对 / 矩形」，
  **没有一条盯队列**（`Editor/CollectionScene.cs:5327` · `Editor/MainMenuScene.cs:3205-3220` 都只看 `FindChild != null` + `Texture.name`）。
  反过来也意味着**没有断言盖住这次改动** ⇒ 见下节建议。
- 我**没有**碰 `DeckSelectionPopup.cs`（不在白名单），它的 `sel` 是**真值**，照旧。

## 没做到的

1. **`Highlight` 的层没修**（原版 = `Deck` 的 **child[0] 最底**；我们在 `qOverlay` **最顶**，金环糊在卡面上）。修它要给 `DeckCell` 加**第 4 档队列**
   （`q` 之下、页面板之上）**并由弹窗那个调用点传进来** —— `Shell/DeckSelectionPopup.cs` 不在本次白名单。**现在只影响弹窗那一格**（收藏窗已不画高亮）。
2. **`DcHiOff = -1.4`（x/y 同值）两轴都不对**：原版 `Highlight` 是 **x `-19.8` · y `-10.9`**（同 prefab，见上表）。同上，只影响弹窗，**建议与第 1 条一并改**
   （改动点两处：`MenuDraw.cs:2867-2868` 常量 + `:3062` 那个 `PxRect`；改完把 `DcHiOff` 拆成 `DcHiX/DcHiY` 并**统一乘 `K`**）。
3. **⑤ 改完没有实拍验收** —— 我这边不许跑 Unity、也不许截图（本轮全部视觉结论都出自既有实拍与用户参考图）。建议下一轮真机拍一格确认。
4. 「收藏窗用哪一份变体」缺实例级铁证（见上）；若主对话要铁证，需跑一次原版实况。

## 我建议加的断言（逐条；我不写，落点都在 `Editor/`）

1. **⑤ 相对关系（灭自证式）**：收藏窗任一格，断 `Frame` 的 `ImageQuad.RenderQueue` **严格小于** `Faction` / `Game Mode Icon` 的；
   **同时断 `activeSelf`**（本仓最贵那条：合 inactive 的查找会骗人）。**把队列改回 `q` ⇒ 红。**
2. **⑤ 档位**：断两枚徽章的队列 == `CollectionWindow.QPageOverlay`（且 `> QPageText > QPageRow`）。**改回 `q` ⇒ 红。**
3. **⑥ 收藏窗不画高亮**：逐格断 `FindChild(cell, "Highlight Rounded Square") == null`，**并单独断「当前那一格也没有」**
   （`CollectionData.CurrentIndex()`）。**把 `false` 改回 `i == CurrentIndex()` ⇒ 红。**
4. **⑥ 的灭自证对**：选卡组弹窗里**选中那一格**断**有** `Highlight Rounded Square`（那一份变体 `useSelectedHighlight=1`）。
   **把 `DeckCell` 里 `if (selected)` 改成恒不画 ⇒ 这一条红**；**把收藏窗那处 `false` 改回真值 ⇒ 第 3 条红** —— 两条**结构上不可能同时满足**。
5. **⑦ 防复发**：断「画出的格数 == 当前筛选下的套数」（`DeckCellCount == FilteredDeckIndices().Count`，后者要加个只读出口）。
   **谁将来插一个 `+1` 空位 ⇒ 红。**

## 顺手发现（我没改）

1. **`A1525⑥` 记的成因不完整**：简报说「高光把两枚徽章盖掉」，但**未选中那一格照样看不见** ⇒ 主因是**队列同档 + 透明排序**，
   高光是**另一条**缺陷。硬证据：`_tmp_view/u_1/c2_full.png` 里经典模式那颗**红枪徽只剩左上一个月牙**，从卡框右下托架后面露出来。
2. **空卡组在实拍里是「原版行为」**：无督军的卡组走 `Config` 的**早退**分支，原版连卡名都**不设**（TMP 留 prefab 的 `'Deck name'`）
   ⇒ 我们显示真名是**多给了信息**（无害、但不逐字同原版）。成因是「创建卡组只建不开编辑器」（`A549①`）那笔账，**别为它改画法**。
3. **`DeckSelectionPopup.cs:731` 的 `sel` 是真值**（那份变体 `useSelectedHighlight=1`）⇒ **别顺手一起改成 `false`**。
4. **反汇编入口（本轮新踩实，可复用）**：`script.json` 的 `ScriptMethod[].Address` = **RVA**；再接 `GameAssembly.dll` 的 ImageBase `0x180000000`、`pefile` 段表 → 文件偏移 → `capstone`（`CS_ARCH_X86/CS_MODE_64`）。本轮脚本在 `/tmp/wf_disasm.py`（只读、没进工程）。
5. **卡组格那张高光图是「光环不是实心」**：sprite 150×155、九宫 `73,75,73,75` ⇒ 横竖边全被边框吃满 ⇒ 中间只有几像素；
   它现在糊在卡面上时表现为**一整条 65.7px 宽的金带**（不是整格实心）—— 与实拍一致，别按「实心方块」去推断。

## 类型检查读数

`TMPDIR=/tmp/wf_u bash d:/4/Unity/工具/typecheck.sh` → **运行时错误数 0 · 编辑器错误数 0**（每次改完 `.cs` 都立刻跑，跑了三次，三次都 0）。
`git diff --numstat`（相对 HEAD）：`MenuDraw.cs 34/2`（**全部是本件**）· `CollectionWindow.cs 53/7`（**其中本件只有 14/1** = 判据注释 13 行 + `false` 那一行）。
⚠️ 其余 39/6 **我进场前就在工作区里**（`A1498` 的左栏标签表 `Decks`→`Deck` + `term`，不是本件改的）—— 我**只动了 `BuildDeckCell` 那一段**。行尾：两文件都仍是**纯 LF**（0 个 CRLF）。
