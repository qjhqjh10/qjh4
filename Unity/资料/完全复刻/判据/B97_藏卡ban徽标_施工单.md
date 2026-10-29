# B97 · 藏卡 ban 徽标几何

> 只读查判据代理 `B97_BAN` · 2026-10-29 · 判据源 = **原版 prefab YAML 逐字段现读**（`Collection Card.prefab`，带真 pathID），**⛔ 不是照截图量的**。
> 对象 = `Collection Card.prefab` 的 `CardUI/Ban Icon`（1.75×1.75 卡单位）+ 子件 `Banned Text`。
> 消费方：`Shell/CollectionWindow.cs`（卡池卡格，尚未建）；对照/已落地：`Deck/DeckRuntime.cs`（卡组编辑池，`B97_MENU` 今天已建）。

## 〇 结论（3 行）

1. **原版真值**：`Ban Icon` = **中心锚、`sizeDelta 1.75×1.75` 卡单位、`localScale 1`**，挂在 **`CardUI`（`localScale = 150`）** 下 ⇒ 实际画成 **262.5×262.5 px 的正方叉**（在 350×512 的卡根里居中）；`Image` = sprite **`40k_Cross_icon_cross_big Banned card`**（512²、`PreserveAspect=1`、白、不吃射线）；子件 `Banned Text` 框 = 方叉的 **0.8×0.2**（210×52.5 px，居中），TMP `m_fontSizeBase 36` / autosize / Center·Middle / Bold+UpperCase / 白 / 本地化键 `MenuDeck/Banned`（en `Banned` / zh `禁止`）。显隐 = `SetActive(banned)`（`BasicCardUI.ToggleBanned`），banned 源 = `PlayEventData.IsCardBanned(cardId)`。
2. **我们现状**：**卡组编辑卡池（`DeckRuntime`）已建**（`B97_MENU`·2026-10-29，`SetPoolBanIcon`，约 263.6 px）；**收藏窗卡池（`CollectionWindow.cs`）完全没建**（`RebuildCardsCells` 只建 `CardView` + `张数条`，全文件无 `ban` 相关层）。
3. **差多少**：收藏窗缺**整层**（图标 + 文字 + 显隐）；几何已可 100% 照抄 —— 我们格子 262.5×384 = 原版 350×512 **× 0.75**，故方叉 = **196.875 px**（= 262.5×0.75），中心 = 格中心，字框 157.5×39.375，字号 base 27。

## 一 原版真值表

判据文件：`d:/2/tools/assetripper_out/ExportedProject/Assets/GameObject/Collection Card.prefab`（行号 = 该 YAML 的 `--- !u!<类> &<fileID>` 行；真 pathID 见括号）。

| 对象路径 | rect（`sizeDelta`） | 锚点 / pivot | 换算（相对卡根 350×512） | 贴图 / 字 | 显隐 | 判据出处 |
|---|---|---|---|---|---|---|
| `Collection Card`（根） | 350 × 512 | anchorMin/Max (0,1)·pivot (0,1) | — | — | 常显 | `RT_…-914581999851801256`（`DeckRuntime.cs:9502` 已核）· 卡根 |
| `Content`（GO 1730242686860875） | stretch `sizeDelta (0,0)` = 350×512 | 0..1 / pivot(.5,.5) | 铺满卡根；挂 `CanvasGroup`（入场淡入） | — | 常显 | RG 224477046003934300（`L1205`）· `m_Name: Content` L34 |
| ├ `Counter`（GO 1724804207875695，兄弟 **序 0**） | 底条（非本次对象） | — | 张数条在卡下缘 | — | 常显 | GO L55 `m_Name: Counter` |
| └ `CardUI`（GO 1703437598541787，兄弟 **序 1**，挂 `BasicCardUI`） | stretch `sizeDelta (-350,-512)` ⇒ 0×0 | 0..1 / pivot(.5,.5) | **`m_LocalScale = 150`** ⇒ 子件 1 单位 = 150 px；`2DCard`(2.0927×3.3313)=314×500 px | — | — | RT L1493（`m_LocalScale {150,150,150}` L1501）· `m_Name: CardUI` L96 · `BasicCardUI` MB L1516（`bannedIcon:{1801075752484240}`） |
| `CardUI/Ban Icon`（GO 1801075752484240） | **1.75 × 1.75**（卡单位） | **min=max (0.5,0.5)**，pivot (.5,.5)，anchoredPos ≈ **(0.0166, 0)** | **×150 = 262.5 × 262.5 px 正方**，居中于卡 | `Image`：`m_Sprite`=`40k_Cross_icon_cross_big Banned card`（512²·PPU100·pivot中）、`m_Type 0(Simple)`、**`m_PreserveAspect 1`**、`m_Color` 白(1,1,1,1)、**`m_RaycastTarget 0`** | `SetActive(banned)` | GO L1135 · **RT L6983** · **Image MB L7012**（`m_Sprite:{21300000, guid 9bf6f199201033447a1552538ac8cd95}` L7026） |
| `CardUI/Ban Icon/Banned Text`（GO 1000955326284023） | `sizeDelta (0,0)`（**stretch**） | **min (0.1,0.4) · max (0.9,0.6)**，pivot(.5,.5) | 框 = **0.8 × 0.2 × 262.5 = 210 × 52.5 px**，居中于方叉 | TMP：`m_text "Запрещено"`、**`m_fontSizeBase 36`**、`m_fontSize 0.25`、`enableAutoSizing 1`、`auto[0.25…72]`、**`m_fontStyle 17`(=Bold 1 + UpperCase 16)**、`HorizontalAlignment 2(Center)`、`VerticalAlignment 4096(Middle)`、白、font `Pragati-Regular SDF`、材质 `Pragati-Regular Atlas Material Thick Outline 2` | 跟父 `Ban Icon` 一起开关 | GO L1154 · **RT L7042** · **TMP L7070** · I2 Localize MB L7164（**`mTerm: MenuDeck/Banned`**） |

**贴图在工程里，可直接用**：
- 去重 Sprite JSON = `素材/Warpforge原版/UI图集/去重资源/Sprite/40k_Cross_icon_cross_big Banned card_-4138590900053668541.json`（`m_Rect 0,0,512,512`·PPU100·pivot中·atlasTag `0_GeneralUI Atlas`·纹理 `sactx-0-4096x2048-BC7-0_GeneralUI Atlas-7a9721b1_0.png`）。
- 运行时 PNG（已导入 `Resources`）= `CardPresentation/Resources/Art/ui_menu/40k_Cross_icon_cross_big_Banned_card.png`（`CardArt.MenuUi("40k_Cross_icon_cross_big_Banned_card")`，`Core/CardArt.cs:890`）。

**显隐条件（代码链，逐条现读）**：
- `CollectionCard.ToggleBanned(bool state)`（`decomp_full/CollectionCard__ToggleBanned.c`）：`this.isBanned(+0x78)=state` → `BasicCardUI.ToggleBanned(cardUI, state)` + `CollectionItem.ToggleDrag(this, !state)`。
- `BasicCardUI.ToggleBanned(bool)`（`decomp_full/BasicCardUI__ToggleBanned.c`）：`uVar1 = *(this+0x60)`（= 序列化字段 **`bannedIcon`**，本 prefab 里 = `{fileID: 1801075752484240}` = `Ban Icon` GO）⇒ 非空则 **`GameObject.SetActive(state)`**。**这就是整条显隐**（图标与子件 `Banned Text` 一起开关）。
- `banned` 的源：`PlayEventData.IsCardBanned(cardId)`（`out_cpp2il_dev_ilspy/DeckEditorCollectionDisplay.cs:295` 实算 `state = playEventData.IsCardBanned(card)` → `collectionCard.ToggleBanned(state)`；`CardCollectionDisplay.cs:288/364` 重放 `cardUI.ToggleBanned(isBanned)`;`:662` 有一支强制 `false`）。⇒ **= 当前活动（LiveOps）的禁卡表**；无活动 ⇒ `false`（原版 `IsCardBanned` 缓存 null 恒 false，我们 `Shell/PlayEvent.cs:377` 逐字照抄）。
- `prefab` 里 `Ban Icon` 的 `m_IsActive = 1`（序列化默认开），但生产显隐一律由 `ToggleBanned` 覆盖成 `banned` ⇒ **实现按「默认关、banned 才开」**（与 `DeckRuntime` 一致）。

> 🔴 **铁律 5 就地更正**（旧记录错，别照抄）：`B92_战斗判据_B4b.md:328-330` 等把运行时 dump 里那颗 `Ban Icon` 的 sprite `-4138590900053668541` 记成「**2x2 占位**」——**不成立**。`-4138590900053668541` **就是真图** `40k_Cross_icon_cross_big Banned card`（`素材/…/Sprite/40k_Cross_icon_cross_big Banned card_-4138590900053668541.json` 里 `m_Name` 逐字如此，512² 叉图）。dump 里写的 `2x2` 是 **`sizeDelta`(1.75 四舍五入=2) × 父级 150** 之前的**局部 rect 值**，不是「占位图」。

## 二 我们现状

| 窗口 | 文件 | ban 徽标 | 读数 / 出处 |
|---|---|---|---|
| 卡组编辑卡池 | `CardPresentation/Deck/DeckRuntime.cs` | ✅ **已建**（`B97_MENU`·2026-10-29） | `BanIconUnits = 1.75f`(`:9527`) · `SetPoolBanIcon`(`:9537`) · 调用 `:3133` · `ShowPoolBanIcon`(`:9568`) · `ClearPoolBanIcons`(`:9576`)；`side = 1.75 × PxPerUnit × P.CardScale`（桌面 ≈ **263.6 px**，原版 262.5）；字框 0.8×0.2、`Loc.T("MenuDeck/Banned")`、`SetAutoFitBox(…,8,basePx,basePx)`。**几何与原版一致** |
| **收藏窗卡池** | **`CardPresentation/Shell/CollectionWindow.cs`** | ❌ **没建** | `RebuildCardsCells`(`:1511`) 只 `CardView.Create/SetPose` + `BuildCardsCounter`(`:1676` 调 · `:353` 定义)；全文件无 `ban`/`IsCardBanned`/`PlayEvent`（除 `:4253` 一句注释）；格子 `CardsCellW=262.5 / CardsCellH=384`(`:211`) = **原版 350×512 × 0.75**；卡体缩放 `scale = CardsCellH*150/512/108`(`:1583`)，`CardsCardRect = cell`(`:290`，卡体居中于整格) |
| 数据写入口 | `CardPresentation/Shell/PlayEvent.cs` | ✅ 已有（`B96_MENU`·2026-10-29） | `IsCardBanned(string)`(`:377`) · `bannedCardIds`(`:374`) · **写入口 `PlayEventDataExtensions.SetBannedCards(evt, cardIds)`(`:1007`/`:1010`)**。⚠️ **生产路 0 调用**（源 = 服务器/活动 P3 远端）⇒ 恒 null ⇒ `IsCardBanned` 恒 false（= 原版「无活动」那一支，逐字等价） |
| 自设计标 | — | — | `Banned Text` 的字号映射（见 §四）是**按比例推**的，非逐字段直读；⛔ 别把「27 px」当原版绝对字段 |

## 三 施工单（收藏窗卡池）

> **落点 = `CardPresentation/Shell/CollectionWindow.cs`**（该文件当前归 `B97_MINI2` 在飞 ⇒ 动手前查 `资料/完全复刻/文件所有权_当前.md`）。
> 🔴 **零行数漂移（铁律 18·b）**：调用**追加在同一物理行**（`DeckRuntime.cs:3133` 同款），新方法**追加到类尾**（`:6599` 的 `    }` 之前）。⛔ 别在类体中间插成员 —— 该文件被账按 `文件:行号` 大量引用。

### 三·1 调用（改 `CollectionWindow.cs` 第 1676 行，行尾追加，**同行**）

```csharp
// 原：                                        BuildCardsCounter(cellT, i, r, list[i]);
// 改成（同一行追加 Ban 徽标）：
                BuildCardsCounter(cellT, i, r, list[i]); BuildCardsBanBadge(cellT, i, r, list[i]);   // 🆕 B97_BAN：藏卡 ban 徽标（原版 Collection Card/CardUI/Ban Icon，判据→判据/B97_藏卡ban徽标_施工单.md）
```
（徽标随 `Content <i>` 容器一起建/销毁：`RebuildCardsCells` 每趟先摘出池化的 `CardView`、再销毁 `Content <i>`（`:1529-1541`）⇒ ban 徽标作 `cellT` 的子件，滚动/换档重建时自然拆掉重建，**不会残留到别的卡**。）

### 三·2 新方法（追加到 `CollectionWindow.cs` 类尾 `:6599` 的 `}` 之前）

```csharp
// ============================================================ 藏卡 ban 徽标（B97_BAN · 2026-10-29）
// 判据：d:/2/tools/assetripper_out/ExportedProject/Assets/GameObject/Collection Card.prefab
//   · CardUI/Ban Icon：中心锚(0.5/0.5)、sizeDelta 1.75×1.75 卡单位、localScale 1；挂 CardUI(localScale 150)
//     ⇒ 物理 262.5×262.5 px（350×512 卡根居中）。Image：sprite `40k_Cross_icon_cross_big Banned card`
//     (512²/PPU100/pivot中)、Simple、PreserveAspect=1、白、RaycastTarget=0。
//   · CardUI/Ban Icon/Banned Text：anchor x.1..9 / y.4..6、sizeDelta 0 ⇒ 框 = 0.8×0.2 个方叉(210×52.5 px)居中；
//     TMP fontSizeBase 36 / autosize[0.25..72] / Center·Middle / style 17(Bold+UpperCase) / 白 / term `MenuDeck/Banned`。
//   · 显隐：BasicCardUI.ToggleBanned = banIcon.gameObject.SetActive(banned)；默认关、banned 才开。
// 我方格子 262.5×384 = 原版 350×512 × 0.75 ⇒ 方叉 = 262.5×0.75 = 196.875 px，中心 = 格中心。
void BuildCardsBanBadge(Transform parent, int i, PxRect cell, CardDef def)
{
    const string BanSprite = "40k_Cross_icon_cross_big_Banned_card";
    if (Art(BanSprite) == null) { Debug.LogWarning("[Collection] `" + BanSprite + "` 不在 Resources/Art ⇒ ban 徽标画不出来（缺口，不是被视口裁掉）"); return; }

    float pxPerUnit = CardsCellH * 150f / 512f;         // = 112.5（原版 1 卡单位 = 150 px，×0.75）
    float side      = 1.75f * pxPerUnit;                // = 196.875 px（原版 1.75 卡单位 × 150 × 0.75）
    float cx = cell.CX, cy = cell.CY;                   // 卡体居中于整格(CardsCardRect=cell) ⇒ 方叉中心 = 格中心
    var iconR = new PxRect(cx - side * 0.5f, cy - side * 0.5f, cx + side * 0.5f, cy + side * 0.5f);

    // keepAspect = 原版 m_PreserveAspect = 1（512² 方图 ⇒ 在正方框里正好铺满）；队列 > 卡(3000)，原版 CardUI 在 Counter 之后(画在条之上) ⇒ 取 QPageOverlay(3034)
    var icon = Rect(parent, BanSprite, iconR, "Ban Icon " + i, QPageOverlay, null, true);
    if (icon == null) return;                           // 整颗在视口外（正常，静默）

    // 子件「禁止」那行字：框 = 0.8 × 0.2 个方叉，居中（原版 anchor x.1..9 / y.4..6）
    float tw = side * 0.8f, th = side * 0.2f;
    var trR = new PxRect(cx - tw * 0.5f, cy - th * 0.5f, cx + tw * 0.5f, cy + th * 0.5f);
    float basePx = side * (36f / 262.5f);               // = 27（原版 fontSizeBase 36 是在 262.5 px 方叉里定的）
    var lb = Text(icon.transform, Loc.T("MenuDeck/Banned"), trR.x1, trR.x2, trR.y1, trR.y2, 5, Color.white,
                  "Banned Text", basePx,
                  wrapPx: tw, autoMinPx: 8f, autoMaxPx: 72f * (pxPerUnit / 150f), autoBasePx: basePx);
    if (lb != null)
    {
        lb.SetRenderQueue(QPageText);                   // = 3033，> 卡(3000)
        MenuDraw.SetVAlign(lb, Label.VAlign.Midline, trR);   // 原版 VerticalAlignment 4096；水平出厂即 Center(2)、⛔ 别调 Align*
    }

    bool banned = IsCardBanned(def);                    // 数据源见 三·3
    icon.gameObject.SetActive(banned);                  // 原版 BasicCardUI.ToggleBanned = SetActive(banned)；⛔ 默认关
    if (lb != null) lb.gameObject.SetActive(banned);
}
```

> `autoMaxPx: 72f * (pxPerUnit/150f)` 展开 = 72×0.75 = **54**（原版 autosize 上限 72，按同一 0.75 缩）。

### 三·3 数据接线（`IsCardBanned`）

- 判据口 = `PlayEventData.IsCardBanned(string)`（`Shell/PlayEvent.cs:377`）。
- 事件数据源**复用 `DeckRuntime.CurrentEventData`**（`DeckRuntime.cs` 类内 `public static PlayEventData CurrentEventData;`，`DeckRuntime` 已用它喂卡组编辑池）⇒ 只需一个极薄的静态转调：

```csharp
// 追加到类尾（与 BuildCardsBanBadge 同批）；语义逐字 = 原版（无活动 ⇒ false = 放行）
static bool IsCardBanned(CardDef def) => def != null && DeckRuntime.CurrentEventData != null && DeckRuntime.CurrentEventData.IsCardBanned(def.Id);
```
（若嫌跨窗耦合，也可在 `CollectionWindow` 里另起 `public static PlayEventData CurrentEventData;` 一句 —— 但**判据只留一处**：`IsCardBanned` 的实现只有 `PlayEventData` 那一份，⛔ 别在这里再写禁卡表比对。）
- ⚠️ **生产路目前恒 false**（`CurrentEventData` 没人写；写入口 `SetBannedCards`(`PlayEvent.cs:1010`) 生产 0 调用，源在远端）—— 这是**原版「没绑活动」那一支，行为逐字等价**，不是缺陷。活动线拿到禁卡表后调 `SetBannedCards` 即可点亮。

### 三·4 验收 / 断言要点（宿主 = `Editor/CollectionScene.cs`）

1. **几何**：可见格子里 `Find("Ban Icon <i>")` 存在；**渲染宽高 = 196.875 px（both，±0.5）**；中心 = `CardsCellRect(i).CX/CY`（±0.5）。
2. **默认隐藏**：`CurrentEventData == null` ⇒ 每格 `Ban Icon <i>.activeSelf == false`（含字）。
3. **banned 点亮**：造活动事件 `evt.SetBannedCards(new[]{某卡Id})` → 指向 `DeckRuntime.CurrentEventData` → `RebuildCardsCells` ⇒ 该卡那格 `activeSelf == true`，文字 = `Loc.T("MenuDeck/Banned")`（zh = `禁止`）。
4. **灭自证**（结构上不可能一起变绿）：
   - 把 `SetActive(banned)` 改成 `SetActive(true)` ⇒ 断言②红（默认该关却开着）。
   - 把 `side = 1.75f * pxPerUnit` 改成 `side = 1.75f`（忘乘）⇒ 断言①红（约 1.99 px，不是 196.875）。
   - `banned` 恒 false 时断言③红（该卡那格仍关）。
5. **重建不残留**：滚出再滚回同一卡 ⇒ 徽标跟着该格重建成对应卡的 banned 态，**不出现在别的卡上**（徽标是 `cellT` 子件，随 `Content` 销毁）。
6. **队列**：`Ban Icon` 的 `RenderQueue > 3000`（卡）；字 `> 3000`。原版 `CardUI`(含 Ban) 在 `Counter` 之后（画在张数条之上），但我们方叉(93.6..290.4 px)与张数条(345.7..383 px)**纵向不重叠** ⇒ 只要 >3000 即看不出差别。

## 四 没查清 / 需真机核

1. **`Banned Text` 的实际渲染字号（TMP autosize 结果）静态读不出**：prefab 序列化 `m_fontSize 0.25`（= 最小值 clamp / 编辑态残留）+ `m_fontSizeBase 36` + `enableAutoSizing`。上面取 `basePx = side×36/262.5 = 27`，是**照 `DeckRuntime` 已落地那套的比例推**（二者同为 `36/262.5`），**不是逐字段直读的绝对数**。若真机与原版并排看字偏小/偏大，可改试 `side×(0.25×150)/262.5 = 28`（差 ~4%）。**要判只能真机拍一张**：收藏窗里造一张 `banned` 卡，截图与「原版同位置 Banned 卡」并排比字高（原版没有可跑的离线界面 ⇒ 退一步只能与原版 prefab 的 210×52.5 框 + 自适应规则对逻辑，或与已落地的 `DeckRuntime` 卡组编辑池那颗并排）。
2. **收藏窗卡池里 `isBanned` 是否真有「置真」的活入口（原版侧）**：反编译只看到 `DeckEditorCollectionDisplay.DrawCell` 调 `CollectionCard.ToggleBanned(state)`（`out_cpp2il_dev_ilspy/DeckEditorCollectionDisplay.cs:295`）；`CardCollectionDisplay` 只**重放** `isBanned`(`:288/364`) 和一支强制 `false`(`:662`)。⇒ **卡池网格里 `isBanned` 可能结构上一直 false**（除非另有写入口没扫到）。所以建这一层**不算死代码（原版有 `SetCell/UpdateCardVisuals` 重放路径 + 字段在）**，但「收藏窗里它到底会不会亮」取决于是否存在置真入口 —— **这一格没查透**，若后续发现收藏侧确实从不置真，本层与 `DeckRuntime` 一样属于「数据通了才亮」。
3. **队列值**：`QPageOverlay(3034)` 是我的选择（>卡3000、>张数条3032，且原版 CardUI 在 Counter 之后）；因**纵向不重叠**，取 `QPageText(3033)` 或 3032 也看不出差别 ⇒ 非严格判据，按页内梯子避让即可。
4. **字体族**：原版 `Banned Text` 用 `Pragati-Regular SDF`（拉丁）。我们 `MenuDeck/Banned` 在 zh 语档 = `禁止`，需 CJK 字形 ⇒ 用 `Text` 的默认 `TmpFont.Face.Cjk`（能同时渲 `Banned` / `禁止`）。原版在 zh 客户端怎么换字无本地判据（客户端已关服）—— 按「能显示各语档」处理，**这是我们的取法**。
