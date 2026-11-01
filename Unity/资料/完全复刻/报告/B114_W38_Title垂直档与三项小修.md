# B114_W38 · `B-280`（Title Army 垂直档）+ `B-281`（死码）+ `B-282`（Title 底板 PA / Icon 档徽章节点名）

> 第一百一十四会话 · 写手 `W-38` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账/正本/`Scenes`/W-35 与 W-37 的文件。
> **typecheck（`TMPDIR=/tmp/wf_w38`）= 运行时 0 / 编辑器 0**（最后一版 0/0）。
> **账闸 `check_ledger_points.py` 现跑：A 5,841 条 · 通过 5,841 · 漂移 0 · 失败 0**（删行之后复跑，见 §二·3）。

---

## 〇、四条结论一句话

| # | 原版判据（真包现读） | 我们改之前 | 改成 |
|---|---|---|---|
| **`B-280`** | `Title Drawer` 的 `Content/Army` 对齐 = **`Center/Bottom`**（对照 `Avatar Drawer` = `Center/Middle`） | 出厂 `Middle`（一颗都没设） | `Title(...)` 拿到 `DrawerArmyName` 的返回值后调 `MenuDraw.SetVAlign(lb, Label.VAlign.Bottom, r)` |
| **`B-281`** | ——（死码，无原版对应） | `ItemDrawerPartial_B111R3W.cs:280-298` 的 `IconAllianceBadge` **0 调用点** | **删除**（连同 `IconVariant` 里那条 `case`）⇒ `IconVariant` 只服务其余 **6** 族 |
| **`B-282①`** | `Title Drawer` 的 `Content/Background.Image.m_PreserveAspect` = **1** | 拉伸（`keepAspect: false`） | `DrawerBackground(...)` 多一个 `bool keepAspect = false` 形参，`Title` 显式传 `true` |
| **`B-282②`** | `Alliance Badge Drawer Icon` 根的直接子件叫 **`Badge`**（`&224709867586078768`） | 沿用了基础档的 `Trophy Icon` | 新增 `NodeIconBadge = "Badge"`；`Icon` 档画完**当场改名**；`B114_C_Assert` 的查名同步 |

---

## 一、活 1 · `B-280`：`Title Drawer` 的 `Army` 垂直档 = `Center/Bottom`

### 1. 原版判据（一手 = 现行 build 的真包）

```
python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Title Drawer"  --depth 3 --relative
python -I -X utf8 工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer" --depth 2 --relative
```

| prefab / 节点 | 现读（原样） | 对齐 |
|---|---|---|
| `Title Drawer` ▸ `Content/Army` | `'Leviathan'` 字号=35.099998 基准=36 auto[12.0~75.0] 折行=1 色=(0.689,0.689,0.689,0.851) | **`Center/Bottom`** |
| `Title Drawer` ▸ `Content/Label/Name` | `'TITLE'` 字号=35.0998 auto[12~75] 折行=0 | `Center/Midline` |
| `Avatar Drawer` ▸ `Content/Army`（**对照**） | `'Goff'` 字号=**75.0** 基准=36 auto[12~75] | **`Center/Middle`** |

⇒ **`Middle` 是通则，`Title Drawer` 的 `Bottom` 是例外**（⛔ 别把通则改掉 —— 与 `B-276` 字号那条同一个形状）。

### 2. 改成什么

```csharp
// Shell/ItemDrawerLayout.cs —— 返回值由 void 改成 Label（源码兼容，见下）
internal static Label DrawerArmyName(Transform content, DrawerFrame fr, int army, ItemDrawerStyle st,
                                     float fontPx = DrArmyFontPx)
{
    if (st.NamePx <= 0f) return null;
    string name = ArmyIcons.LocalizedName(army);
    if (string.IsNullOrEmpty(name)) return null;
    var r = fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2);
    return ClippedText(content, r, name, DrArmyInk, NodeArmy, fontPx * fr.K, st.QText, r.W, st, 0,
                       12f * fr.K, 75f * fr.K, 36f * fr.K);
}

// Shell/ItemDrawerTitle.cs —— 拿到返回的 Label 再补垂直档
var armyLb = DrawerArmyName(content, fr, item.CardArmy, st, TitleArmyFontPx);
MenuDraw.SetVAlign(armyLb, Label.VAlign.Bottom, fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2));
```

- **矩形从哪来**：`fr.Map(DrCx1, DrArmyY1, DrCx2, DrArmyY2)` = `(0.4,702)-(712.4,810)` 设计 px，
  与真包 `Content/Army` 的 `(0.1,171.5)-(174.1,197.9)`（视觉帧）÷ `0.244367` **逐值相同** ⇒ 与 `DrawerArmyName` 内部那一格是同一个框。
- **为什么改返回值不会破坏别的调用点**：C# 里「调一个有返回值的方法但不要返回值」与「调 void 方法」是同一件事
  ⇒ 其余 **9 个**既有调用点（`ItemDrawerAvatar:59` · `ItemDrawerAlliance:69` · `ItemDrawerPoints:92` ·
  `ItemDrawerPremium:86` · `ItemDrawerRandomCard:114` …）**一个字节都没改**（⛔ 也没给它们加垂直档）。
- **`face` 档**：`MenuDraw.SetVAlign` 走缺省 `Label.OrigFace.Pragati` —— 与全仓其余 `SetVAlign` 调用点同一档
  （`WA712 §2·4`：Pragati 是主力）。⛔ 没有证据表明这颗用 Asar。

### 3. 断言（`Editor/B114_W38_Assert.cs` §①，6 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `A0` | **硬前提**：阵营 `10` 有徽记图 + 本地化名（否则 `DrawerArmyName` 早退、`Army` 不建） | 缺图 ⇒ 红（⛔ 不静默跳过） |
| `A1`/`A1b`/`A1c` | **硬前提**：路由到 `TitleDrawer` · `Army` 建出来 · 那件挂的是 `Label` | 任一不成立 ⇒ 红 |
| `A2` | ★★ **结论**：`Title` 的 `Army` `Label.VAlignTier == Bottom`（读**实际生效的那一档**） | 删掉 `SetVAlign` 那句 ⇒ 读回 `Middle` ⇒ 红 |
| `B1` | ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Army` 仍是 **`Middle`** | 一刀切改成 Bottom ⇒ 红 |
| `B2` | ★★ **结构反证**：两档**真正施加的垂直位移**（`Label.VOffsetWorld`）必须不等 | 两处读到同一个数 ⇒ 红 |

> ⚠️ `B2` **只判「不等」、⛔ 不拿 `VOffsetWorld` 当期望值**（那是拿我们的算式证明我们 —— W35 报告 §二·4 的纪律）。

---

## 二、活 2 · `B-281`：删掉死码 `IconAllianceBadge`

### 1. 为什么它是死码

`B114_W35`（`B-277`）在 `Dispatch` 的**第一句**插了 `IconAllianceBadgeFixed` 分流（命中即 `return`），
而 `IconVariant` **只从那一行被调** ⇒ `IconVariant` 里的 `case DrawerAllianceBadge` 永远走不到 ⇒
它转调的 `IconAllianceBadge`（写的是 `frameR = box`，跟框画）**一个调用点都没有**。

### 2. 我选了「删」还是「留空壳」—— **删**，理由有三条（都是现读，不是推断）

1. **该文件在 `账/*.tsv` 里只有 1 条落点**：`日常_奖励.tsv:788` / `_merged_B111/B111_R3WB.tsv:2`，
   落点 = `CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs:43（DrawIconFill）`。
2. **那条落点在 `43` 行，位于删除区（≥108 行）之前** ⇒ 删下面 9 行**不会**让它漂移。
3. **实跑账闸验过**：`python -I -X utf8 工具/check_ledger_points.py` ⇒
   `A 5841 条 · 通过 5841 · 漂移 0 · 失败 0`（删完之后跑的）。

⇒ **没有走「类尾留空壳」那条路**（那条只在「删了会顶漂落点」时才需要）。

### 3. 改了什么

| 位置 | 改动 |
|---|---|
| `IconVariant` 的 `switch` | 删 `case DrawerAllianceBadge: IconAllianceBadge(…); return true;`（换成 2 行说明注释） |
| 类尾（原 `:280-298` 的那个方法） | **整段删除**（含 doc），换成 6 行「已删除 + 现在的实现在这两份文件」的说明 |
| 文件头 `B114_WC · B-223：Icon 变体几何 **7** 档` | **就地订正成 6 档**（铁律 5），并写明第 7 档已于同日移交 W35 |
| 文件头那张 prefab 表第 14 行 | 加 `← 已移交 W35/W38`，并注明「本节的 `frameR = box` 模型不适用」 |
| `IconVariant` 的 doc | 「命中这 **6** 族」+ 写明它对 `AllianceBadgeDrawer` 恒返回 `false`（保留 `false` 是正确语义） |

### 4. 断言

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `B0`（§②） | ★★ **反射**：`typeof(ItemDrawer).GetMethod("IconAllianceBadge", NonPublic\|Static) == null` | 有人把它加回来 ⇒ 红 |
| `B0b`/`B0c` | **前提**：替换件 `IconAllianceBadgeFixed` / 分流口 `IconAllianceBadgeFixedNamed` 都在 | 连替换件都没了 ⇒ 红 |
| `B2`（§②） | ★★ **灭自证**（`IconVariant` 仍服务其余 6 族）：画一次 **`Icon` 档的 `Avatar Drawer`**，主图必须仍是 **> 1080² 的过扫**（基础档会画成 712×432 的横带） | 把 `IconVariant` 整个删掉 ⇒ 红 |

### 5. 「全仓 0 命中」的实际结果（如实报）

`grep -rnE "IconAllianceBadge([^A-Za-z]|$)" --include=*.cs Assets` 的剩余命中**全是**：

- **注释**：`ItemDrawerPartial_B114W35.cs:30/39/43` · `Editor/B114_W35_Assert.cs:14` · `ItemDrawer.cs:1918`（行尾注释）· 本波自己写的说明；
- **字符串/反射名**：`Editor/B114_W38_Assert.cs`（**故意**的：`GetMethod("IconAllianceBadge")` 就是用来断「它不存在」的）。

⇒ **可执行代码里 0 命中**。`ItemDrawerPartial_B114W35.cs` 与 `B114_W35_Assert.cs` 里的那 4 处注释**不是我的文件**（⛔ 没改），
建议它们的下一个写手顺手把那两句「它该由那份文件的所有者删掉」改成「已删（`B114_W38`）」。

---

## 三、活 3 · 两处小项

### ① `Title` 的 `Background` 按原版的 `m_PreserveAspect = 1` 画

**原版判据**（真包）：`Title Drawer` ▸ `Content/Background` 那一行末 = `… Simple (1,1,1,1) preserveAspect`（= PA 1），
sprite = `40k_Rewards_icon_title card` **329×499**，框 = `Content` 那一格 **712×1080**。

**改成**：`DrawerBackground(Transform, DrawerFrame, ItemDrawerStyle, string sprite = DrBackgroundSprite, bool keepAspect = false)`
—— 新形参默认 `false` ⇒ **既有 9 个调用点逐像素不变**；`ItemDrawerTitle.Title(...)` 显式传 `true`。

**⚠️ 必须核的那个 0.009%**（做过了，数字如下）：

| 对 | 比例 | 与框（712/1080 = 0.6592593）之差 |
|---|---|---|
| `40k_Rewards_icon_title_card.png`（PNG IHDR 现读 **329×499**） | 0.6593186 | **+5.93e-5 = 0.0090%** ⇒ 高差 **≈0.10 px** |
| `40k_Rewards_icon_base_card.png`（现读 **328×498**） | 0.6586345 | +6.25e-4 = 0.095% ⇒ 高差 ≈1.0 px |

⇒ **等价**（与 `B114_W35` 报告 §七·2 的数字一致；我复核的是 **PNG 的真实 IHDR**，不是说的）。
断言不用「看起来一样」这种话：直接比**画出来的宽高比**必须等于 **sprite 比例 329/499**（容差 **2e-5**），
而这个容差**比两种候选比例的差（5.9e-5）更紧** ⇒ 拉伸与 PA 在几何上**可区分**（C1 那条单独把这件事断出来，防「测试没有分辨力」）。

> 🔴 **顺带查出来的大事（本波⛔ 没做，见 §六·①）**：**全族各档的 `Content/Background` 都是 `m_PreserveAspect = 1`**，
> 我们**全族都是拉伸**。本波按施工单只改了 `Title` 这一档，`C5` 那条断言断的是「本波没越界」，
> **不是**「拉伸是对的」—— 报告与断言的文案里都写死了这一点。

### ② `Icon` 档徽章的节点名改回原版的 `Badge`

**原版判据**：`menu_dump … "Alliance Badge Drawer Icon" --depth 2 --relative` ⇒ 根下**只有两颗**：

| 深度 | 名字 | 尺寸 | 组件 |
|---|---|---|---|
| 1 | **`Frame`** | 165.95×155.74 | `Image,EverguildButtonMaterialModifier`（`preserveAspect`） |
| 1 | **`Badge`** | 165.95×155.74 | `Image,EverguildButtonMaterialModifier`（`preserveAspect`） |

而**基础档** `Alliance Badge Drawer` 同一位置那颗叫 `Content/Frame/Trophy Icon` ⇒ **两套 prefab 各自一个名字，不是一个通则**。

**改成**（`Shell/ItemDrawerPartial_B114W38.cs`，新文件）：

```csharp
static bool IconAllianceBadgeFixedNamed(string drawer, Transform node, PxRect box, ItemSpec item, int quantity,
                                        ItemDrawerStyle style, ref ItemDrawResult res)
{
    if (!IconAllianceBadgeFixed(drawer, node, box, item, quantity, style, ref res)) return false;
    var badge = node != null ? node.Find(NodeTrophyIcon) : null;   // 直接子件（`AllianceBadgeArt.Draw(node, …)`）
    if (badge != null) badge.name = NodeIconBadge;                 // "Badge"
    return true;
}
```

- **为什么是「建完再改名」而不是「在创建处就写对」**：创建处是 `AllianceBadgeArt.Draw(node, r, id, NodeTrophyIcon, q)`，
  调用点在 `ItemDrawerPartial_B114W35.cs`（**W-35 的产物，本波⛔ 不归我写**）⇒ 我只能在它外面包一层。
  **几何仍然只有一份**（转调 `IconAllianceBadgeFixedDraw`，⛔ 没复制那套常量/算式）。
  那份文件的下一个写手把 `NodeTrophyIcon` 换成 `NodeIconBadge` 之后，本文件这一跳可以整段删掉（断言仍绿 —— 它断的是**结果名**）。
- **`NodeIconBadge = "Badge"` 放在哪**：`Shell/ItemDrawerLayout.cs` 里 `NodeTrophyIcon` 旁边（那个文件 `账/*.tsv` **0 命中** ⇒ 可自由加行）。
- **接线**：`ItemDrawer.cs` 的 `Dispatch` 那一行把 `IconAllianceBadgeFixed` 换成 `IconAllianceBadgeFixedNamed`
  （**同一物理行**，行数 2497 → 2497 不变）。
- **同步「按名查它」的地方**：`Editor/B114_C_Assert.cs` 的 G3 原写 `FindDeep(res.Node, ItemDrawer.NodeTrophyIcon)` ⇒ 改成 `NodeIconBadge`。
  ⚠️ 顺带发现那条断言**在 `B-277` 之后已经红了**（它的期望还是「`Frame`/`Badge` = 整个调用框」），见 §六·③。

#### 断言（§④，6 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `D0`/`D1` | **硬前提**：徽章图取得到 · `Icon` 档抽屉根建出来 | 缺图/没建 ⇒ 红 |
| `D2` | ★★ **结论**：Icon 档根下**有 `Badge`** | 沿用 `Trophy Icon` ⇒ 红 |
| `D2b` | ★★ Icon 档根下**没有 `Trophy Icon`** | 改名改出一份副本 / 压根没改 ⇒ 红 |
| `D4` | ★★ **灭自证（通则那一侧）**：基础档那颗仍是 `Trophy Icon` | 把 `NodeTrophyIcon` 常量直接改名 ⇒ 红 |
| `D4b` | ★ 基础档根下没有 `Badge` | —— |
| `D5` | ★★ **结构反证**：同一批调用里**两套 prefab 的节点名必须不同** | 两档读到同一个名字 ⇒ 红 |

---

## 四、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/ItemDrawerLayout.cs` | ① 新增 `NodeIconBadge = "Badge"`（+ doc 订正 `NodeTrophyIcon` 只属基础档）；② `DrawerBackground` 加 `bool keepAspect = false` 形参（转给 `MenuDraw.Rect`）；③ `DrawerArmyName` 由 `void` 改 **`Label`**（3 个 `return null` + 末句 `return ClippedText(...)`）；④ 文件头 ③ 补一段「纵向档已补完」 | **322 → 344（+22）**（账 `0` 命中） |
| `Assets/CardPresentation/Shell/ItemDrawerTitle.cs` | ① 文件头版面段补 `Background` 的 PA / `Army` 的 Bottom / `Label/Name` 的 Midline 现状；② `Title(...)`：`DrawerBackground(..., TitleBgSprite, **true**)` · `var armyLb = DrawerArmyName(...)` + `MenuDraw.SetVAlign(armyLb, Label.VAlign.Bottom, r)` | **128 → 135（+7）**（账 `0` 命中） |
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B111R3W.cs` | 删 `IconAllianceBadge`（方法 + doc）· 删 `case DrawerAllianceBadge` · 文件头/表/doc 三处「7 档」就地订正成 6 档 | **300 → 291（−9）**（账落点只有 `:43`，**在其之前** ⇒ 不漂；已由账闸实测） |
| `Assets/CardPresentation/Shell/ItemDrawer.cs` | `Dispatch` 第一句：`IconAllianceBadgeFixed` → `IconAllianceBadgeFixedNamed`；行尾注释补「已收尾」 | **2497 → 2497（0）** |
| `Assets/CardPresentation/Shell/ItemDrawerPartial_B114W38.cs`（**新建**） | `IconAllianceBadgeFixedNamed`（唯一新增件） | **52 行** |
| `…ItemDrawerPartial_B114W38.cs.meta`（**新建**） | guid `f67a73f91da3a124174d72e2bc80841b`（扫过 7,294 份 `.meta`，无重复） | 新文件 |
| `Assets/CardPresentation/Editor/B114_W38_Assert.cs`（**新建**） | 四段共 **28 条**（① 9 · ② 5 · ③ 6 · ④ 8） | **418 行** |
| `…B114_W38_Assert.cs.meta`（**新建**） | guid `0b1c6a96b1f5dab20f4aa46764e29b77` | 新文件 |
| `Assets/CardPresentation/Editor/B114_C_Assert.cs` | ① 新增 `BoxAllianceIconRoot`（= prefab 常量手算的固定根矩形）；② G2/G3 期望改成固定根 + 节点名改 `Badge` + 新增 `G3b`；③ 文件头「Alliance = 调用框」就地订正 | **+约 15**（该文件本会话**新建未提交**，无 HEAD 基线） |

**行号纪律自检**（python 二进制数）：七个文件全 **纯 LF / 裸 CR 0**。
`ItemDrawer.cs` 两处改动都在**第 1918 行这同一条物理行内**（代码区在第一个 `//` 之前、另一处在注释里）⇒ 行数不变、账不漂。

---

## 五、接线片段（可粘贴 · ⛔ 宿主 `Editor/ShellScene.cs` 是 W-37 的，我没改）

`ShellScene.Run` 那条聚合行，**同一物理行、第一个 `//` 之前**追加（与 `B114_W35_Assert` 同形）：

```
B114_W38_Assert.Run(); _sink.Pass += B114_W38_Assert.Pass; _sink.Fail += B114_W38_Assert.Fail;
```

（`B114_W38_Assert.Run()` 内部只建临时 GameObject、`DestroyImmediate` 收尾，**不写盘、不建场景**。）

---

## 六、没查清的部分（如实记）

1. **四条都**没有跑过断言、也没有实机看过（⛔ 不跑 Unity 是红线）⇒ 「`A2` 真的读回 `Bottom`」「`C3` 真的落在 329/499」
   都是**按代码推的**，不是观测值。接上宿主后请把 `[B114W38]` 那一段逐条回读一次。
2. **`B-280` 的画面差值没实测**：`Bottom` 档在那个 **108 设计 px 高**的框里把字往下挪多少，
   是按 `Label.SetVAlign` 的模型推的（`WA712` 那张表的档位目标），**没有实拍可比**（原版已关服）。
3. **`Title Drawer` 的 `Label/Name` 的 `Center/Midline` 没做**（见 §七·②）—— 判据已有、做法与活 1 完全相同，本波**只做了施工单点名的 `Army`**。
4. **全族 `Background` 的 PA 没做**（见 §七·①）—— 真包逐份核过 9 份全是 PA=1，我们全族拉伸；本波按施工单只改 `Title`。
5. **`Icon` 档徽章节点是「建完再改名」**，不是创建处就写对（原因 = `ItemDrawerPartial_B114W35.cs` 不归我写）。见 §三·②。

---

## 七、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴🔴 **全族各档的 `Content/Background` 都是 `m_PreserveAspect = 1`**，我们**全族都是拉伸**（本波只改了 `Title`）。
   逐份现读（`menu_dump … --depth 2 --relative`，每份都印 `… Simple (1,1,1,1) preserveAspect`）：
   `Avatar` · `Container` · `Premium` · `Forge Points` · `Alliance Badge` · `XSolla Offer` · `Avatar Border` · `Random Card` · `Currency`（+ 已改的 `Title`）。
   **改法 = 一行**：把 `DrawerBackground` 的 `bool keepAspect = false` 改成 `= true`（`Title` 那句显式 `true` 可留作自文档）。
   **量化影响**（都是 < 1.5 px）：基础档 `328×498` → 框 `712×1080` ⇒ 高差 **0.68 px**；`Random Card` `328×523` → 框 `712×1134` ⇒ 宽差 **0.83 px**。
   ⚠️ 落地时**请把 `B114_W38_Assert` 的 `C5` 改成「两档都 = sprite 比例」**（它现在断的是「本波没越界」）。
2. 🔴 **`Title Drawer` 的 `Content/Label/Name` 是 `Center/Midline`，我们画 `Middle`**（真包现读；同一份 prefab，是 `B-280` 的姊妹条）。
   做法与活 1 逐字相同，可粘贴：
   ```csharp
   var nameLb = ClippedText(content, nr, item.Label, Color.white, NodeName, 35.1f * fr.K, st.QText, nr.W, st, 0,
                            12f * fr.K, 75f * fr.K, 36f * fr.K, true);
   MenuDraw.SetVAlign(nameLb, Label.VAlign.Midline, nr);
   ```
   （现在那一句是 `ClippedText(...)` 的裸调用、返回值被丢掉 —— 改起来就是接住 `nr` 那一个框。）
3. 🔴🔴 **`Editor/B114_C_Assert.cs` 的 Alliance 段在 `B-277` 之后已经红了**：它的 `G2`/`G3` 期望写的是
   「`Frame`/`Badge` = **整个调用框** `(100,50)-(266,205.74)`」，而 `B114_W35` 之后实际是**固定根** `(100.02,5.37)-(265.98,161.11)`
   （差 **44.63 px** = 那个上移量）⇒ `RectIs` 容差 0.6 判红。
   **我已就地改成固定根的期望值**（`BoxAllianceIconRoot`，三个数是 prefab 字段原文、**不是**读生产常量）+ 节点名改 `Badge` + 补 `G3b`。
   ⚠️ **这是白名单外的改动**（`B114_C_Assert.cs` 既不在我的 ✅ 也不在 ⛔ 清单里）—— 理由是「同步所有按名查它的地方」这条指令，
   且**不改就会留一条已知必红的断言**。请审查这一处；若判断该由 `W-C` 自己改，直接回退我这几行即可（其余改动自洽）。
4. `ItemDrawerPartial_B114W35.cs:43-44` 与 `Editor/B114_W35_Assert.cs:14` 里还写着「`IconAllianceBadge` … 由那份文件的所有者删掉」
   —— **已经是过去式**（本波删了）。不是我的文件，⛔ 没改，建议各自的下一个写手顺手订正（铁律 5）。
5. `Editor/B114_C_Assert.cs` 顶部还按「Icon 变体几何 **7** 档」表述（`把 IconVariant 那一支删掉 ⇒ 这 7 组同时红`）。
   我只改了其中与 Alliance 几何有关的那几行，**档数表述没动**（那是那份文件的主体叙述，应由它的所有者统一订正）。
6. 🔴 **并发提醒**：`Unity/资料/完全复刻/文件所有权_当前.md`（第 1 波那份）把 `ItemDrawerPartial_B111R3W.cs` 记在
   `W-C` 与 `W-27` 名下。我按本波施工单（✅ 白名单）删了那 9 行并动了 `ItemDrawer.cs` 的**同一物理行**；
   现读 `W-27` 的产物是 `ItemDrawerPartial_B114W27.cs`（17:31）与 `Editor/B114_W27_Assert.cs`（18:07 仍在写）——
   它**没有**再动 `ItemDrawerPartial_B111R3W.cs`。若它接下来要动，请注意我把该文件从 300 行改成了 291 行
   （账落点 `:43` 未动，已被账闸证实）。
   ⚠️ 另：18:07 那次 typecheck 曾报 `B114_W27_Assert.cs` 两条 `CS0103`（`SectionMainImage`/`SectionBaseBackground`），
   **不是我改的文件、也不是我的错**（那是 W-27 写到一半）；等它落定后复跑 = **0 / 0**。

---

## 八、给复核者的两分钟核对口

```bash
# 1) 活 1 / 活 3① 的判据（真包现读）
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Title Drawer"  --depth 3 --relative | grep -E "Background|Army |Name "
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer" --depth 2 --relative | grep -E "Background|Army "
# 判据：Title Army = Center/Bottom（Avatar = Center/Middle）· Title Background.sprite = 40k_Rewards_icon_title card 329×499 + preserveAspect

# 2) 活 3② 的判据（Icon 档根下两颗叫 Frame / Badge）
python -I -X utf8 Unity/工具/menu_dump.py bundle_menus_assets_all "Alliance Badge Drawer Icon" --depth 2 --relative

# 3) 活 2：可执行代码里 0 命中
grep -rnE "IconAllianceBadge([^A-Za-z]|$)" --include=*.cs Unity/MyGame/Assets   # 剩下的应当全是注释/断言里的字符串

# 4) 行号守恒 + 账不漂
wc -l Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs                                   # 2497
python -I -X utf8 Unity/工具/check_ledger_points.py | tail -3                                     # 漂移 0 / 失败 0

# 5) 两个新文件 + 双 meta
ls -l Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B114W38.cs* \
      Unity/MyGame/Assets/CardPresentation/Editor/B114_W38_Assert.cs*

# 6) 类型检查
TMPDIR=/tmp/wf_w38 bash Unity/工具/typecheck.sh     # 期望：运行时 0 / 编辑器 0
```
