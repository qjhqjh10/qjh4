# B114_W43 · `B-284`（全族 `Content/Background` 的 PA）+ `B-285`（`Title` 的 `Label/Name` 垂直档）

> 第一百一十四会话 · 写手 `W-43` · 2026-11-01。
> ⛔ 未跑 Unity（`-executeMethod` 全禁）· ⛔ 未动 git · ⛔ 未改账/正本/`Scenes`/其它写手的文件。
> **typecheck（`TMPDIR=/tmp/wf_w43`）= 运行时 0 / 编辑器 0**。
> **账闸 `check_ledger_points.py` 现跑：A 5,841 条 · 通过 5,841 · 漂移 0 · 失败 0**（三个改动文件在账里 **0 命中** ⇒ 加行不会顶落点）。

---

## 〇、两件结论一句话

| # | 原版判据（真包现读） | 我们改之前 | 改成 |
|---|---|---|---|
| **`B-284`** | **全族各档** `Content/Background` 的 `m_PreserveAspect = 1`（逐份 dump 那行末都印 `preserveAspect`；**14 份一个例外都没有**，见 §一·1） | `DrawerBackground` 的 `keepAspect` 默认 `false` ⇒ 除 `Title`（`B114_W38` 显式传过）外**全族拉伸** | `bool keepAspect = true`（**默认值一行**）⇒ 11 份走这条路的 prefab 一起变对 |
| **`B-285`** | `Title Drawer ▸ Content/Label/Name` 对齐 = **`Center/Midline`**（对照 `Avatar Drawer` = `Center/Middle`） | 出厂 `Middle`（一颗都没设） | `Title(...)` 接住 `ClippedText` 的返回值后调 `MenuDraw.SetVAlign(nameLb, Label.VAlign.Midline, nr)` |

---

## 一、活 1 · `B-284`：全族 `Content/Background` 的 `m_PreserveAspect = 1`

### 1. 原版判据（一手 = 现行 build 的真包，逐份现读）

```bash
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "<prefab>" --depth 2 --relative --no-ancestor-scale
```

每一份都印出 `… Simple (1,1,1,1) preserveAspect`（本波逐份跑过，行原文见下表「那行末」列）。

### 2. 我们这边是怎么回事

`Shell/ItemDrawerLayout.cs` 的 `DrawerBackground(...)` 是全族画底板**唯一**的入口（9 个调用点），
它的 `keepAspect` 形参默认 `false` ⇒ 除了 `Title`（`B114_W38` 按施工单显式传了 `true`）之外**全族画成拉伸**。

### 3. 改成什么（**一行**）

```csharp
internal static bool DrawerBackground(Transform content, DrawerFrame fr, ItemDrawerStyle st,
                                      string sprite = DrBackgroundSprite, bool keepAspect = true)
```

`Title` 那句 `DrawerBackground(content, fr, st, TitleBgSprite, true)` 的显式 `true` **保留**
（自文档 + 让 `B114_W38_Assert` 的 C3 仍然盯「显式传」那一档）。

### 4. 落在哪一层：**11 份 prefab 由这一行覆盖，另 3 份各有各的路**

| prefab（真包现读） | `Background` 框 | `m_Sprite`（尺寸） | 我们是谁在画 | 本波之后 |
|---|---|---|---|---|
| `Avatar Drawer` | 712×1080 | `40k_Rewards_icon_base card` 328×498 | `DrawerBackground`（默认值） | ✅ PA（711.325×1080） |
| `Container Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（默认值） | ✅ PA |
| `Premium Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（默认值） | ✅ PA |
| `Expansion Pass Premium Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（`PremiumBody`） | ✅ PA |
| `Forge Points Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（默认值） | ✅ PA |
| `Expansion Pass Points Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（`PointsForArmy`） | ✅ PA |
| `Campaign Points Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（`DrawCampaignPointsBackground`） | ✅ PA（`B-266` 那条 doc 说的「≤0.7px 亚像素差」随之消失） |
| `Alliance Badge Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（默认值） | ✅ PA |
| `XSolla Offer Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（默认值） | ✅ PA |
| `Avatar Border Drawer` | 712×1080 | 同上 328×498 | `DrawerBackground`（`AvatarBorder`；`Shop` 档不画） | ✅ PA |
| `Title Drawer` | 712×1080 | `40k_Rewards_icon_title card` 329×499 | `DrawerBackground`（**显式** `true`，`B114_W38`） | ✅ PA（本来就对） |
| `Random Card Drawer` `Base Background` | 712×1080 | 328×498 | `DrawerImage`（**恒** `keepAspect: true`） | ✅ 本来就对，本波没动 |
| `Random Card Drawer` `Background`（稀有度档） | 712×**1134** | `…_common` 328×523 | `DrawerImage`（恒 `true`） | ✅ 本来就对，本波没动 |
| **`Currency Drawer`** | 712×1080 | 328×498 | 🔴 **`ItemDrawerCurrency.cs` 自己那句 `MenuDraw.Rect(…, false, …)`** | ❌ **仍是拉伸**（见 §五·① 补丁） |
| **`Title Drawer Horizontal Variant`** | 1028×370 | `40k_Profile_display_title` 322×116 | 🔴 **`ItemDrawerPartial_B114W32.cs` 自己那句 `MenuDraw.Rect(…, false, …)`** | ❌ **仍是拉伸**（见 §五·② 补丁） |

**几何差（都 <1 px，视觉等价 —— 改它是为了「换图/换框时不再露馅」，不是为了修一个看得见的偏差）**：

| 档 | PA 之后画出来 | 与框的差 |
|---|---|---|
| 基础档（328×498 进 712×1080） | **711.325 × 1080** | 宽 **−0.675 px** |
| `Title`（329×499 进 712×1080） | **712 × 1079.903** | 高 **−0.097 px** |
| `Random Card` 稀有度档（328×523 进 712×1134） | 711.189 × 1134 | 宽 −0.811 px |
| `Title Horizontal`（322×116 进 1028×370） | 1027.069 × 370 | 宽 −0.931 px |

> ⚠️ 上面第三、四行**本来就对**（走的是别的口，且已经是 PA）—— 列出来是为了让「全族」这张表是完整的。

---

## 二、活 2 · `B-285`：`Title Drawer` 的 `Label/Name` = `Center/Midline`

### 1. 原版判据（同一份 prefab，`--depth 3 --relative` 现读）

| prefab / 节点 | 现读 | 对齐 |
|---|---|---|
| `Title Drawer ▸ Content/Label/Name` | `'TITLE'` 字号=35.099998 基准=36 auto[12~75] 折行=0 色=(1,1,1,1) | **`Center/Midline`** |
| `Avatar Drawer ▸ Content/Label/Name`（**对照**） | `'Legendary Wildcard'` 字号=**75.0** auto[12~75] 折行=0 | **`Center/Middle`** |

⇒ **`Middle` 是通则，`Title Drawer` 的 `Midline` 是例外**（与 `B-280` 的 `Army = Bottom`、`B-276` 的 fs 35.1 同一个形状）。

### 2. 改成什么

```csharp
// Shell/ItemDrawerTitle.cs —— Title(...) 的 `Label/Name` 那一段
var nr = fr.Map(61f, 810f, 651.8f, 918f);
var nameLb = ClippedText(content, nr, item.Label, Color.white, NodeName, 35.1f * fr.K, st.QText, nr.W, st, 0,
                         12f * fr.K, 75f * fr.K, 36f * fr.K, true);
MenuDraw.SetVAlign(nameLb, Label.VAlign.Midline, nr);   // 拿 null 调是安全的（SetVAlign 自己判空）
```

- **框**：`(61,810)-(651.8,918)` 设计 px —— 与 `DrawerNameQuantity` 里「只有 `Name` 时铺满」那一路同一个框
  （`fr.Map(DrQtyX1, DrLbY1, DrNameX2, DrLbY2)` = `(61,810)-(651.8,918)`，逐值相同）。
- **为什么不改共用件**：`Middle` 是**通则**（`Avatar` / 其余档的 `Name` 都是 `Middle`）⇒ 只在 `Title(...)` 这一处按档补，
  ⛔ 别动 `DrawerNameQuantity`（那一改就是全族一刀切）。
- **`Midline` 的位移**：对「全大写 / 数字串」`OrigInkCenterPx` 恒 0（`Label.cs:2146`），
  带下伸部的串按墨水盒算 —— 这一档的语义边界 `Label.cs:2132` 已写明，本波没有越过它。

---

## 三、`B114_W38_Assert.cs` 的 **C5** 怎么改的

**改之前**（`B114_W38` 当时那份）：
```csharp
N(avatar.Aspect, boxA, 2e-5f, "C5 ★ 灭自证（本波的改动范围）：Avatar 的底板本波没动，仍是拉伸 …");
```
它断的是「**脏改动没越界**」—— 因为当时全族都拉伸，C5 只能这么写。

**改之后**（本波）：
```csharp
float baseA = JudgeBaseBgW / JudgeBaseBgH;          // 328/498 = 0.6586345（prefab 现读）
N(avatar.Aspect, baseA, 2e-5f,
  "C5 ★★ 全族 PA（B-284）：Avatar 的底板 —— 它不显式传 keepAspect、吃的是共用默认值 —— 也按自己那张 sprite 的比例 …");
```

配套三处：

| 位置 | 改动 |
|---|---|
| 文件头 §灭自证 那一段 | 「C5 是改动范围哨兵、Avatar 本波没动」⇒ 改成「C3 = 显式传 `true` 那一档 / C5 = **吃默认值**那一档，两档都断 sprite 比例」 |
| 文件头同一段里的 `C4` | 就地订正成 `C1`（分辨力前提实际是 C1；`C4` 是「Avatar 前提」—— 铁律 5） |
| 常量区 | 新增 `JudgeBaseBgW = 328f, JudgeBaseBgH = 498f`（prefab 现读 + PNG IHDR 同值） |

**C3 / C5 现在是一对灭自证**：
- 🧨 把 `DrawerBackground` 的默认值改回 `false` ⇒ **C5 红、C3 仍绿**（`Title` 是显式 `true`）；
- 🧨 把 `Title` 那句显式 `true` 也删掉（只靠默认值）⇒ 两条一起绿 —— 所以两者**不是**同一条断言的复制，
  它们分别盯「显式那一路」和「默认值那一路」。
- 期望值一律是 **prefab 的 sprite 尺寸之比**（`329/499` / `328/498`），⛔ 不读生产常量。

---

## 四、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Assets/CardPresentation/Shell/ItemDrawerLayout.cs` | ① `DrawerBackground` 的 `keepAspect` 默认 `false` → **`true`**；② 那颗形参的 doc 重写（逐份判据 11 份 + 两档不走这里的例外 + 量化差） | **344 → 352（+8）**（账 0 命中） |
| `Assets/CardPresentation/Shell/ItemDrawerTitle.cs` | ① `Title(...)` 的 `Label/Name` 接住 `ClippedText` 返回值 + `MenuDraw.SetVAlign(…, Midline, nr)`；② 文件头那条「`Label/Name` 仍是 Middle、尚未做」就地订正成「已补（`B-285`）」 | **135 → 138（+3）**（账 0 命中） |
| `Assets/CardPresentation/Editor/B114_W38_Assert.cs` | **只动 ③ 的 C5**：头注 2 处订正 + 新常量 2 个 + C5 重写 | **418 → 427（+9）**（账 0 命中） |
| `Assets/CardPresentation/Editor/B114_W43_Assert.cs`（**新建**） | 两段共 **13 条**（① 7 条 `B-284` · ② 6 条 `B-285`） | **278 行** |
| `…B114_W43_Assert.cs.meta`（**新建**） | guid `39bc80dae0544ef19ddb302d2cc0dc52`（扫过 7,000+ 份 `.meta`，无重复） | 新文件 |

**行号纪律自检**（python 二进制数）：四个文件全 **纯 LF / 裸 CR 0 / UTF-8 可解**。

---

## 五、🔴 需要调度台转派的两条补丁（**本活白名单不含那两个文件，我没改**）

### ① `Shell/ItemDrawerCurrency.cs` —— 货币档的底板仍是拉伸（**同一个账 `B-284`**）

原版判据（本波现读）：`Currency Drawer ▸ Content/Background` = `40k_Rewards_icon_base card 328×498 … preserveAspect`，框 712×1080。
我们的实现在它自己的文件里，**不走** `DrawerBackground`：

```csharp
// 现读 :104-105
MenuDraw.Rect(content, bgTex, M(CurContentX1, 0f, CurContentX2, CurRootH),
              NodeCurrencyBackground, st.QBoard, null, false, st.Clip, st.ClipSoftness);
//                                                    ↑ 改成 true
```
**同时**该文件 `:13` 的文件头写的是「`40k_Rewards_icon_base card`（Simple · **非 preserveAspect**）」——**与真包相反**，按铁律 5 就地订正成 `preserveAspect`。

### ② `Shell/ItemDrawerPartial_B114W32.cs` —— 横条称号档的底板仍是拉伸（同一个账）

原版判据（本波现读）：`Title Drawer Horizontal Variant ▸ Content/Background` = `40k_Profile_display_title 322×116 … preserveAspect`，框 1028×370。

```csharp
// 现读 :99-100
MenuDraw.Rect(content, bg, fr.Map(0f, 0f, DrHTitleW, DrHTitleH), NodeBackground,
              st.QBoard, null, false, st.Clip, st.ClipSoftness);
//                            ↑ 改成 true
```

> 两条都是**一个 `false` → `true`**，几何差分别是 −0.675 px / −0.931 px。改完这一族就真的**全族**对齐了。

### ③ 🔴 **本波落地后 `Editor/B114_W20_Assert.cs` 的 B3b 会红（算术已核）**

```csharp
// :164 现读
N(got.W, ContentWWant, 0.6f, "B3b ★ 画出来的宽 = 712（与 A3 同一数量）");
```
- 它量的是 `Expansion Pass Points Drawer` 的 `Content/Background` **quad 的实测矩形**（`QuadOf` → `MenuDraw.QuadRectPx`）；
- PA 之后画出来是 **711.325**（= `1080 × 328/498`），`|711.325 − 712| = 0.675 > 0.6` ⇒ **红**；
- 同一段的 B3 两条（左沿 0.4 / 右沿 712.4，容差 0.6）仍然是绿的（实测 0.7375 / 712.06，各偏 0.337）。

**建议改法**（二选一，都保持这条断言的**原意**＝「`Content` 那 0.4 的留白真的画出来了」）：
1. 容差 `0.6f` → **`1.0f`**（同族先例：`B114_W27_Assert` 的 F2/F2b 对**同一块底板**用的就是 `1.0f`）；
2. 或把期望改成 `1080f * 328f / 498f`（PA 的真值），并补一句「原版这颗 `m_PreserveAspect = 1`」。

> 我没有改那份文件（不在本活 ✅ 白名单里）。**这一条不修，同步点跑 `ShellScene.Run` 会多一条红，而它不是缺陷、是「原版判据变了」。**

---

## 六、断言清单（`B114_W43_Assert.cs`，13 条）

### ① `B-284`（7 条）—— 取的是**第三档** `Container Drawer`（与 `B114_W38` 的 C3/C5 不是同一份 prefab）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `F0` | **硬前提**：共享底图 `40k_Rewards_icon_base_card` 取得到 | 删图 ⇒ 红（⛔ 不静默跳过） |
| `F1` | **分辨力前提**：`|328/498 − 712/1080| = 6.25e-4 > 2e-5`（实测容差） | 换一张与框同比例的新图 ⇒ 这一段失去分辨力 ⇒ 红 |
| `F2` | **前提**：`Everguild.LiveOps.ShopContainer` 路由到 `ContainerDrawer` | 路由表被改 ⇒ 红 |
| `F2b` | **前提**：`Content/Background` 建出来且带贴图 | 整层没建 ⇒ 红 |
| `F3` | ★★ **结论**：画出来的比例 = **328/498**（实测 ≈0.6586345），框比例 0.6592593 | 🧨 默认值改回 `false` ⇒ 读到框比例 ⇒ 红 |
| `F4b` | ★ `Title`（**显式** `true`）也 = 329/499 | 🧨 删掉 `Title` 的显式实参且 `DrBackgroundSprite` 与它不同 ⇒ 红 |
| `F5` | ★★ **灭自证（不是一刀切常数）**：两档比例**不等**（0.6593186 vs 0.6586345） | 🧨 有人写死一个比例给全族 ⇒ 必有一档红 |

### ② `B-285`（6 条）

| # | 断什么 | 怎么才能红 |
|---|---|---|
| `N0`/`N0b`/`N0c` | **硬前提**：路由到 `TitleDrawer` · `Label/Name` 建出来 · 那件挂的是 `Label` | 任一不成立 ⇒ 红 |
| `N1` | ★★ **结论**：`Label.VAlignTier == Midline`（读**实际生效**的那一档） | 删掉 `SetVAlign` 那句 ⇒ 读回 `Middle` ⇒ 红 |
| `N3` | ★★ **灭自证（通则那一侧）**：`Avatar Drawer` 的 `Name` 仍是 **`Middle`** | 一刀切改成 Midline ⇒ 红 |
| `N4` | ★★ **结构反证**：两档**真正施加的垂直位移**（`VOffsetWorld`）不等 | 两处读到同一个数 ⇒ 红 |

> `N4` 的期望差有算式撑着：`OrigInkCenterPx(Middle) = (c/2 − (a+d)/2)/p × F`，Pragati `p95/a70/c60/d−20`
> ⇒ `5/95 × F`；`Title` 的 `F = 35.1` ⇒ ≈**1.85 px**（世界档 ÷108 ⇒ 0.017），`Midline` 那一侧 ≈0 ⇒ 差远大于 0.001。
> ⚠️ 与 `B114_W38` 的 B2 同一条纪律：`VOffsetWorld` **只判「不等」，⛔ 不当期望值**。

---

## 七、接线片段（可粘贴 · ⛔ 宿主 `Editor/ShellScene.cs` 不在我的白名单里）

`ShellScene.Run` 那条聚合行，**同一物理行、第一个 `//` 之前**追加（与 `B114_W35_Assert` / `B114_W38_Assert` 同形）：

```
B114_W43_Assert.Run(); _sink.Pass += B114_W43_Assert.Pass; _sink.Fail += B114_W43_Assert.Fail;
```

（`Run()` 内部只建临时 GameObject、`DestroyImmediate` 收尾，**不写盘、不建场景**。）

---

## 八、没查清的部分（如实记）

1. **两件都没有跑过断言、也没有实机看过**（⛔ 不跑 Unity 是红线）⇒ 「C5 真的读回 328/498」「N1 真的读回 Midline」
   都是**按代码推的**，不是观测值。接上宿主后请把 `[B114W43]` 与 `[B114W38]` 两段逐条回读一次。
2. **`B-285` 的画面差值没实测**：`Midline` 在那个 **108px 高**的框里把字挪了多少，按 `Label` 的模型推是 ≈−1.85 px（fs 35.1），
   **没有实拍可比**（原版已关服）。`Midline` 对「带下伸部的串」按墨水盒算（`Label.cs:2132` 已界定的范围），
   本节**不断具体 px**，只断档位与「两档位移不同」。
3. **`Currency` 与 `Title Horizontal` 两档仍拉伸**（§五·①②）—— 不在本活白名单，**没改**；改完后族才算齐。
4. **`B114_W20_Assert` 的 B3b 会红**（§五·③）—— 算术已核，**没改**（同理由）。

---

## 九、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴 **`Shell/ItemDrawerPartial_B114W39.cs` 的 doc 已过期**（`:76-79` 那段）：
   「原版这颗 `Image` 的 `m_PreserveAspect = 1`，而 `DrawerBackground` 走 `keepAspect: false`（全库每一份抽屉都是这一条）…
   实画宽度差 ≤0.7px。要改就得动共用件 … ⛔ 不在本账范围内」——
   **本波已经把共用件改了**（`B-284`），这一段现在是过去式。不是我的文件，建议它的写手就地订正（铁律 5）。
   同理 `ItemDrawerCurrency.cs:13`（见 §五·①）。
2. **`Title Drawer` 的 `Content/Label` 下**其实**有** `Quantity` 子件（`INACT`，`'500'` · `对齐=Right/Middle` · fs 40）——
   我们这一档**不画数量**（`Title(...)` 里没有数量那一段）。这不影响本账（原版那颗 `m_IsActive = 0` ⇒ 视觉上同样不显示），
   但如果将来有人按「槽非 null ⇒ 要画」去改，请先核那颗的 `m_IsActive`。
3. **`Container Drawer` 的 `Content/Image` 是「铺满 + ×1.2 溢出」**（我们已按 prefab 做）——
   与底板那层无关，这里只记一笔：本波断言的 `Container` 夹具里 `Image` 也走 `DrawerImage`（恒 `keepAspect: true`），
   两者互不干扰（实测那一格比例不进 `F3`）。

---

## 十、给复核者的两分钟核对口

```bash
# 1) 判据（真包现读）：全族 Background 都是 preserveAspect
for p in "Avatar Drawer" "Container Drawer" "Premium Drawer" "Forge Points Drawer" "Alliance Badge Drawer" \
         "XSolla Offer Drawer" "Avatar Border Drawer" "Random Card Drawer" "Currency Drawer" "Title Drawer" \
         "Campaign Points Drawer" "Expansion Pass Points Drawer" "Expansion Pass Premium Drawer" \
         "Title Drawer Horizontal Variant"; do
  echo "== $p"
  python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "$p" --depth 2 --relative --no-ancestor-scale \
    | grep -i "background" | head -3
done

# 2) 判据：Title 的 Name = Center/Midline（Avatar = Center/Middle）
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Title Drawer"  --depth 3 --relative | grep -E "Name|Army"
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer" --depth 3 --relative | grep -E "Name|Army"

# 3) 代码
grep -n "keepAspect = true" Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerLayout.cs
grep -n "VAlign.Midline"   Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerTitle.cs
grep -n "JudgeBaseBgW\|baseA" Unity/MyGame/Assets/CardPresentation/Editor/B114_W38_Assert.cs

# 4) 两道闸
TMPDIR=/tmp/wf_w43 bash d:/4/Unity/工具/typecheck.sh        # 期望：运行时 0 / 编辑器 0
python -I -X utf8 d:/4/Unity/工具/check_ledger_points.py | tail -3   # 期望：漂移 0 / 失败 0
```
