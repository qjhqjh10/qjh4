# B141 · W129 · W17 包 —— `DeckRuntime` 三条参数 / 收口（4-20 · 4-10 · 4-12）

> 执行写手：`B141-W26`（第一百二十八会话）· 白名单：`CardPresentation/Deck/DeckRuntime.cs` + 新建 `CardPresentation/Editor/B141_W26_Assert.cs` + 本文件。
> 判据输入：`判据/B141_W129_对账_UI动画.md` §4（4-20 / 4-10 / 4-12）· `报告/B138_审计汇总_问题清单.md` §4 · `报告/B141_W129_LabelMenuDraw.md` §五·2（W14 的 4-12 收口分工）。
> 红线：不跑 Unity、不动 git、不改 `d:/2`、不改账主表、不改别的宿主。

---

## 一 · 结论速览

| # | 原版值（判据） | 我们改前 | 改后 | 动作 |
|---|---|---|---|---|
| **4-20** 卡组行名左沿 | **63.58**（行节点 8.4 + `Text fill` 左内缩 55.182） | 68 | **63.58** | ✅ 改了 `DeckRuntime.cs:433`（`RowNameX`） |
| **4-10** 行卡名垂直档 | 督军行 **512 = Middle** · 卡行 / 防御卡行 **8192 = Capline** | **已同值**（`ApplyRowStyles`） | **不变** | ⛔ **无需改代码**（判据行「未接」已过期，见 §三）· 补断言 |
| **4-12** `m_margin` 第三入口 | 原版 `(22.7,0,0,0)` = **22.7 设计 px**（`Army Filter/Title`；与同颗 `m_fontSize 32` 同档） | `SetTmpMargin` 直写 `TMP.margin`（世界档 ⇒ 实际 2451.6 px，**大 108 倍**） | 收口到 **`Label.SetMarginPx`**（薄转发，`MarginPx` 读回 **22.7**）· **输出矩形逐位不变** | ✅ 改了 `DeckRuntime.cs:2575` + `:10045-10054` |

- **`DeckRuntime.cs` 行数：10411 → 10411** · **`DeckScene.cs`（宿主）行数：10582 → 10582** —— 两个文件都是**零行数变化**，账**不需要 `--fix`**。
- 行尾：`DeckRuntime.cs` CRLF 10411 / 裸 CR 0；`DeckScene.cs` CRLF 10582 / 裸 CR 0。
- 秒级类型检查：**运行时 0 错 · 编辑器 0 错**（`TMPDIR=/tmp/wf_b141_w26 bash 工具/typecheck.sh`）。
- 账闸（只读）：`check_ledger_points.py` 报告里**没有任何 `DeckRuntime.cs` 行**（我的三处改动没造成漂移 / 失败；报告里那 399/38 是别人在飞的改动）。

---

## 二 · 4-20 卡组行名左沿：68 → 63.58

- **改了什么**：`DeckRuntime.cs:433`
  ```csharp
  const float RowNameX = 63.58f, RowNameW = 190f;      // 改前 68f
  ```
  `RowNameW = 190` 不动（原版 `Card Name` 的宽度是模板位，见那段 doc）。
- **判据**：原版 `Deck Editing Menu > … > Deck List drawer` 行节点 x = **8.4**，行内 `Text fill` = **55.182..301.083**
  ⇒ 卡名**框**左沿 = `8.4 + 55.182 = 63.582`（出处与推导已在 `RowNameX` 的 doc 里，`:424-426`）。
- **落点怎么用的**（两处，都吃这个常量，改一处即全改）：
  - `:4425` `_deckRowName[i].AlignLeftOn(LayoutSpace.FromPixel(RowNameX, 0f).x)`（每次刷新都落）
  - `:4533` `MoveLabel(_deckRowName[i], RowNameX + RowNameW * 0.5f, cy, QText)`（块心）
- **✅ 已同改（`B141-W26b`）**：`Editor/DeckScene.cs:5539` 的 `CheckNear(maxL, 68f, 1f, …)` → **`63.58f`**，口径 = **量框（`textBounds`）左沿**：
  - 那条量的量 = `PxOfWorld(node).x − WorldW/2`（`RowNameInkPx`），它量的是 **`textBounds` 左沿**，改后 = `RowNameX` = **63.58**（框左沿）；
  - 参考图那条 `68.0…74.1` 是**字墨**（没减 CJK 左留白 ≈1~2px）⇒ 与 63.58 差 ~2.4px，在它 6px 读数带内（`RowNameX` doc `:425-426` 早已记）；
  - 散文 `:579-583` 与 🧨 文案（原「改回 62」→「改回 68」）已同步；`:5554` 的「比 68 大」也改成 63.58。

---

## 三 · 4-10 行卡名的垂直档：**已实现，无需改代码**（判据行已过期）

判据行写「`Deck/DeckRuntime.cs:2080-2101`（行卡名，仅 `SetAlignLeft+SetAutoFitBox`）⇒ 未接」。
**现读不成立** —— 那一处只是**建标签**；垂直档在**每刷新落族**的那个方法里：

- `DeckRuntime.cs:10031`（`ApplyRowStyles`）：
  ```csharp
  nm.SetVAlign(heroRow ? Label.VAlign.Middle : Label.VAlign.Capline);
  ```
  调用点 = `:4401`（`RefreshDeckList` 的循环里，紧跟 `MoveRow`）⇒ 12 个槽每次刷新按族落一次（`_rowStyleKind` 变了才重落）。
- **与原版三族逐族吻合**（`判据/AUD02_serialized_params.tsv` 2026-11-04 现读）：

| 原版节点 | `m_VerticalAlignment` | 我们 |
|---|---|---|
| `…/Deck Selector Hero Card Info button/Content/Card Name` | **512 = Middle**（`:122`） | `heroRow → Middle` ✅ |
| `…/Deck Selector Card Info button/Content/Background/Text fill/Card Name` | **8192 = Capline**（`:226`） | `!heroRow → Capline` ✅ |
| `…/Deck Selector Defensive Card Slot/Content/Text fill/Card Name` | **8192 = Capline**（`:194`） | ✅ |

- ⇒ **不加第二处 `SetVAlign`**（同一规则只留一处；在 `:2080-2101` 再写一遍 = 铁律 6 那个坑，而且 hero 行会被写错成 Capline）。
- 本包只补了断言（见 §五 B 段）：**督军行必须 Middle、卡行/防御卡行必须 Capline**，且要求**两档同时出现**（灭自证：三族统一成任一档 ⇒ 红）。

---

## 四 · 4-12 `m_margin` 第三入口收口（只换写口，等价）

### 4·1 改了什么

1. `DeckRuntime.cs:10045-10054` —— `SetTmpMargin` 从「抓 `TMP_Text` 直写 `t.margin`」收口成 **`Label.SetMargin` 的薄转发**：
   ```csharp
   static void SetTmpMargin(Label lb, Vector4 m)
   {
       if (lb == null) return;
       lb.SetMargin(m);          // 唯一实现 = Battle/Label.cs:2091
       …
   }
   ```
   保留同名（**行号零变化**：账 `卡组编辑.tsv:340` 按 `DeckRuntime.cs:10047（SetTmpMargin）` 引用；删方法会让 10047 之后**9 行**账落点整体 −9）。
2. `DeckRuntime.cs:2575` —— 业务调用（卡背抽屉 `Army` 小标题，原版 `m_margin = (22.7,0,0,0)`，`判据/AUD02_裁定_卡组编辑.tsv:462`）**顺序调了**：
   ```csharp
   // 改前：MenuDraw.AlignLeft(...); SetTmpMargin(lb, new Vector4(22.7f,0,0,0));
   // 改后：SetTmpMargin(lb, new Vector4(22.7f,0,0,0)); MenuDraw.AlignLeft(...);
   ```

### 4·2 为什么必须调顺序（等价性论证）

`Label.SetMargin` 体内是 `_margin = m; _tmp.margin = m; ForceRelayout();`（`Battle/Label.cs:2091-2103`），
而 `ForceRelayout → RefreshBounds` **会重摆整块**（按 `_margin` 的可用区中心，`Label.cs:1677-1680`）。
- 旧写法直接写 `TMP.margin`、**不重排** ⇒ `AlignLeftOn`（绝对值落位）之后没有任何东西再动 x。
- 若仍保持「先 `AlignLeft`、后 `SetMargin`」：`SetMargin` 里那次 `RefreshBounds` 会把整块从「左沿 2.2」**重新居中**，
  且叠上 `(_margin.x−_margin.z)/2`（世界单位 11.35 = **1225.8 px**）⇒ 标题会飞出抽屉。
- 现顺序（先边距、后 `AlignLeft`）：`AlignLeftOn` 先 `RefreshBounds()` 再 `PlaceLeftNow()`（**绝对值** `ParentXInDesignSpace(worldLeft) + WorldW·0.5`）
  ⇒ **末位仍由 `AlignLeftOn` 决定**，边距只在它之前生效。
- **y 一个像素都不动**：原版这里 `(22.7, 0, 0, 0)` ⇒ `_margin.y = _margin.w = 0` ⇒ `VOffsetWorldNow` 那两项（`boxHLocal − y − w`、`(w−y)/2`）**逐位为 0**。
- **宽度不动**：TMP 建标签时 `textWrappingMode = NoWrap`（`Label.cs:1594`），且这颗走 `SetGlyphHeight`（固定字号、无 autosize）
  ⇒ margin 只影响「可用区」那一跳，`textBounds` 尺寸不变。
- ⇒ **输出矩形与改前逐位相同**；`Editor/DeckScene.cs:8837` 那条既有断言（`Army` 小标题左沿 = **2.2 ± 1**）就是这条等价的**独立腿**，改完照样绿。

### 4·3 量纲缺陷：**`B141-W26b` 已修**（`SetMargin(22.7)` → `SetMarginPx(22.7)`）

- 缺陷：`SetTmpMargin` 原样写 **22.7**；而这颗标签是**世界档**（`Label.Create`，`PxPerUnit = 108`）⇒ TMP margin 单位 = 世界单位 ⇒ 实际 **`22.7 × 108 = 2451.6 px`**，比原版大 108 倍。
  证据：`Label.cs:2034`（`PxPerWorld = 108`）· `:2040`（`PxPerUnit`）· `:2078`（`MarginPx = _margin × PxPerUnit`）· `:2083-2087`（`SetMarginPx` **除以** `PxPerUnit`）。同源先例：`DeckRuntime.cs:10031-10033` 自陈「`m_margin=(0,0,1.83,0)` 写进 `SetAutoFitBox` 后字号 4.63→1.67」。
- **「22.7 是设计 px 而不是 TMP 世界单位」的现读反证（原版 prefab 实读，`Deck Editing Menu.prefab:28549-28634`）**：
  · 那颗 TMP（`mTerm: MenuDeck/Filters/Army`，即 `Army Filter/Title`）= `m_text: Army` · **`m_fontSize: 32`** · `m_enableAutoSizing: 0` · `m_HorizontalAlignment: 1` · `m_VerticalAlignment: 512` · **`m_margin: {x: 22.7, y: 0, z: 0, w: 0}`**；
  · `m_margin` 与 `m_fontSize` 是**同一个 TMP 局部单位**（TMP 的 margin 就是 local unit，不是 em）；
  · 我们对**同一颗**的字号处理是 `SetGlyphHeight(LayoutSpace.Px(TitleFontPx = 32))`（`:2574`）⇒ **32 被当作 32 设计 px** ⇒ 22.7 同档 = **22.7 设计 px**。
  ⇒ 结论：**原版 22.7 = 设计 px**，`SetMargin(22.7)`（世界单位）确实大 108 倍。✔（没有第二条解释：若 22.7 是世界单位，则同一颗的 `m_fontSize 32` 也得按世界单位理解，那颗字就只剩 0.3 px。）
- **修法**：`SetTmpMargin` 体改成 `lb.SetMarginPx(m.x, m.y, m.z, m.w)`（`Label.cs:2083`，内部 `/108` 后走 `SetMargin`）。
  ⇒ `Label._margin = 22.7/108 = 0.2102` 世界单位，**`MarginPx` 读回 = 22.7 设计 px**（与原版同值）。
- **画面上为什么仍逐位相同**：`AlignLeftOn` 是绝对值落位（末位把整块横移到左沿），`_margin.y = _margin.w = 0` ⇒ 纵向两项为 0，`NoWrap` + 固定字号 ⇒ `textBounds` 尺寸不变。等价判别式仍由 `DeckScene.cs:8837`（左沿 2.2 ± 1）与 `B141_W26_Assert` C 段（左沿 2.2 + `MarginPx` = 22.7）双钉。

---

## 五 · 断言：`Editor/B141_W26_Assert.cs`（新，**已接线**）

`Run(DeckRuntime rt)` 的形状同 `B141_W14_Assert` / `B124_W44_Assert`（`Pass` / `Fail` 由宿主累加）。**共 17 条**（含 4 条前提）：

| 段 | 条数 | 盯什么（期望值来源） |
|---|---|---|
| **A · 4-20** | 5 | ① 前提：量到 ≥3 行；② 墨迹左缘 = **63.582**（字面量 `8.4 + 55.182`，⛔ 不读 `RowNameX`）；③ 各行左缘相等（<1px）；④ 🧨 与旧值 68 相差 >3px；⑤ 🧨 各行**宽度不等** >20px（否则③没鉴别力） |
| **B · 4-10** | 5 | ① 前提：第 0 行是督军；② `row_n0` = **Middle**（原版 512）；③ ≥2 行 = **Capline**（原版 8192）；④ 🧨 **两档必须同时出现**（统一档 ⇒ 红）；⑤ 没有别的档（Top/Bottom/Midline） |
| **C · 4-12** | 7 | ① 前提：`cosmoflt_title` 在；② `Label.MarginPx.x` = **22.7 设计 px**（🧨 旧直写 `TMP.margin` ⇒ `Label._margin` 恒 0 ⇒ 红；🧨 走 `SetMargin(22.7)` ⇒ 2451.6 ⇒ 红）；③④⑤ y/z/w = 0；⑥ 前提：量到真实宽度；⑦ 🧨 **等价判别式**：标题左沿仍 = **2.2 ± 1**（`SetMargin` 排到 `AlignLeft` 之后 ⇒ +1225.8px ⇒ 红） |

**接线（`B141-W26b` 已做）**：`Editor/DeckScene.cs:5555` 的 `}`（⑦ A1525③ 段收尾）**同一物理行追加**：

```csharp
} B141_W26_Assert.Run(_rt); _sink.Pass += B141_W26_Assert.Pass; _sink.Fail += B141_W26_Assert.Fail;
```
⛔ 别放在 ⑦ 之前（那时行名还没 `SetText` / `AlignLeftOn`）；⛔ 别在别的页签上调（`UiDeckRowAt` 会全 null ⇒ 前提红）。

⚠️ **本包的断言仍未执行**（红线：不跑 Unity）—— 首次执行由同步点负责；若 ⑦ 那一段的样例牌没有督军行，B 段第 ① 条会红（那是夹具前提，不是实现错）。

---

## 六 · 改动清单（供同步点核对）

| 文件 | 行 | 改动 | 行数变化 |
|---|---|---|---|
| `CardPresentation/Deck/DeckRuntime.cs` | `:427-429` | doc 就地改写（「本轮没动 68」→「已改 63.58 + ✅」） | **0** |
| 同上 | `:433` | `RowNameX = 68f` → **`63.58f`** | **0** |
| 同上 | `:2575` | 调用顺序：`SetTmpMargin` 提前、`AlignLeft` 后移；行尾追加 `✅ 已复刻（…）` 与顺序理由 | **0** |
| 同上 | `:10031` | 行尾追加 `✅ 已复刻`（4-10 三族逐族核） | **0** |
| 同上 | `:10045-10054` | `SetTmpMargin` 收口成 `Label.SetMarginPx` 薄转发（**量纲修复**，10 行 → 10 行） | **0** |
| `CardPresentation/Editor/DeckScene.cs` | `:579-583` · `:5539-5544` · `:5554` | 4-20 期望值 68 → **63.58**（口径 = 框/`textBounds` 左沿）+ 散文/🧨 同步 | **0**（10582 → 10582） |
| 同上 | `:5555` | 接线：`B141_W26_Assert.Run(_rt)` + Pass/Fail 累加 | **0** |
| `CardPresentation/Editor/B141_W26_Assert.cs` | 新文件 | 17 条断言 | +新文件 |

**下游行号位移：0**（三处改动全是「同一物理行内改 / 原地改写 / 等行数替换」）⇒ 账不需要 `--fix`。
⚠️ 唯一要动**别的文件**的两处（都在白名单外，⛔ 我没碰）：
① `Editor/DeckScene.cs:5539` 的期望值 `68f`（**会红**，见 §二）；② 上面 §五 的接线一行。

---

## 七 · ❓ / 留给调度台

1. ~~**`DeckScene.cs:5539` 期望值重定**~~ ✅ **已做（`B141-W26b`）**：口径 = **框/`textBounds` 左沿 = 63.58**（原版 prefab 8.4 + 55.182），散文与 🧨 同步。
2. **4-10 判据行过期**：`判据/B141_W129_对账_UI动画.md:32` 与 `报告/B138_审计汇总_问题清单.md:191` 都把 `DeckRuntime` 记成「未接」；
   现读 `:10031` + `:4401` 已按三族落了（且与原版 512/8192 逐族吻合）。请按铁律 5 就地订正那两行（⛔ 我没改判据/报告主文件）。
3. ~~**4-12 量纲 108×**~~ ✅ **已修（`B141-W26b`）**：`SetTmpMargin` → `Label.SetMarginPx`（22.7 设计 px；原版 prefab `m_fontSize 32 / m_margin 22.7` 同档，反证见 §4·3）。
4. **`MarginPx` 的读数口径**：`Label.MarginPx` 返回的是**画布 px**（`_margin × 108`）—— 但 `SetMarginPx(22.7)` 之后它读回**就是 22.7**（`SetMarginPx` 先除了 108）。将来写站点断言时按「原版 px 原值」写。
5. **顺手发现（未处理，判据待核）**：原版那颗 `Army Filter/Title` 的 `m_TextWrappingMode = 1`（折行开），而我们的 `Label.Create` 默认 `NoWrap`（`Label.cs:1594`，这颗标题也没显式设）—— 4-13/4-15 一类账之外的单点差异，建议另核。

---

## 八 · 续做记录（`B141-W26b`，2026-11-04）

上表的 §二 / §4·3 / §五 / §六 / §七 已按本轮结果就地订正（不另抄第二份）。本轮共三件：

### 8·1 `DeckScene.cs:5539` 期望值：68 → **63.58**（口径 = 框/`textBounds` 左沿）

- **口径裁定（读代码定的，不是挑一个数）**：`RowNameInkPx` 量的是 `PxOfWorld(node).x − WorldW/2`，而 `WorldW` = TMP `textBounds` 的**尺寸**、`AlignLeftOn` 把**块左沿**钉到 `RowNameX`
  ⇒ 这个量**数学上就等于 `RowNameX`**（今天断言 68 能过，正是因为它恒等于那天的 `RowNameX`）。它是**框/文字块**左沿，不是逐字墨迹。
- 所以取 **prefab 口径**（权威度：prefab > 参考图截图）：`8.4 + 55.182 = 63.582`。
- 参考图那条 `68.0…74.1` 不作期望值：它是**字墨**左缘（没减 CJK 左留白 ≈1~2px），与框左沿差 ~2.4px，落在它自己 6px 的读数带里（`RowNameX` doc `:425-426` 早就记过这条）。
- 一并改了：`:579-583` 散文（「左对齐到 68」→「63.58（框左沿）」＋ 两条口径的关系）· `:5544` 🧨「改回 62」→「改回 68」· `:5554`「比 68 大」→「比 63.58 大」。**全部同行数改写。**

### 8·2 接线

`DeckScene.cs:5555`（⑦ A1525③ 段收尾的 `}`）**同一物理行**追加：

```csharp
} B141_W26_Assert.Run(_rt); _sink.Pass += B141_W26_Assert.Pass; _sink.Fail += B141_W26_Assert.Fail;
```

⇒ 17 条断言随 `DeckScene.Run` 一起跑（⚠️ 本轮仍未执行，红线不跑 Unity）。

### 8·3 4-12 量纲：`SetMargin(22.7)` → **`SetMarginPx(22.7)`**

- **先做了「22.7 到底是哪个单位」的反证**（⛔ 不猜）：原版 prefab `Deck Editing Menu.prefab:28549-28634` 现读 —— 那颗 TMP（`mTerm: MenuDeck/Filters/Army`）`m_fontSize = 32`、`m_margin = (22.7,0,0,0)`、`m_enableAutoSizing = 0`。
  margin 与 fontSize 是**同一个 TMP 局部单位**；我们对**同一颗**的字号走 `SetGlyphHeight(LayoutSpace.Px(32))`（`:2574`）= 按**设计 px** 理解
  ⇒ 22.7 同档 = **22.7 设计 px**。若硬说 22.7 是世界单位，那颗字号就只剩 0.3 px —— 不成立。
- 改法：`SetTmpMargin` 体 `lb.SetMargin(m)` → `lb.SetMarginPx(m.x, m.y, m.z, m.w)`（`Label.cs:2083`，内部 `/108`）。
  ⇒ `Label._margin = 0.2102` 世界单位、`MarginPx` 读回 **22.7**（与原版同值）；画面因 `AlignLeftOn` 绝对值落位 + `_margin.y=w=0` + `NoWrap` ⇒ **逐位不变**（`DeckScene.cs:8837` 的左沿 2.2 断言仍是独立腿）。
- 断言 C 段期望值同步：**22.7**（不是 22.7 × 108），并加了一条「走 `SetMargin(22.7)` ⇒ `MarginPx` = 2451.6 ⇒ 红」的灭自证。

### 8·4 本轮验证

- `TMPDIR=/tmp/wf_b141_w26 bash 工具/typecheck.sh` → **运行时 0 错 · 编辑器 0 错**。
- 行数：`DeckRuntime.cs` **10411 → 10411** · `DeckScene.cs` **10582 → 10582** ⇒ **下游行号位移 = 0**，账不需要 `--fix`。
- 行尾：两文件 CRLF 全额 / 裸 CR 0（`DeckRuntime` 10411/10411、`DeckScene` 10582/10582）。
- 账闸（只读）：`check_ledger_points.py` 全文**0 条 `DeckRuntime.cs` / `DeckScene.cs`**。
- ⚠️ **没有跑 Unity**（红线）⇒ 三条改后值都是**代码/判据层**的，真机/宿主读数留待同步点：① 行名文字块左沿 63.58；② `row_n0` = Middle / 其余 Capline；③ `MarginPx.x` = 22.7 且标题左沿 2.2。

