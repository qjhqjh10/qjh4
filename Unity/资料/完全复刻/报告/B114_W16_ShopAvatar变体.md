# B114_W16 · `B-249` `Shop` 档 Avatar 变体（`Avatar Drawer Shop Variant`）

> 写手 `W-16` · 2026-11-01 · 第一百一十四会话。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改宿主 `Editor/ShellScene.cs`（接线片段见 §五，交调度台统一追加）。
> **typecheck（`TMPDIR=/tmp/wf_w16 bash 工具/typecheck.sh`）= 运行时 0 / 编辑器 0。**

---

## 〇、结论（一句话）

`Dispatch` 早把 `DrawerOverride ov` 收进来了，但 26 个 `case` 里只有 `GenericArmy` 往下传 ⇒ `Shop`(20) 这一档
一直按**基础档**几何画。本波在 `Dispatch` 的 `{` 行**同物理行**加了一句
`if (ov == DrawerOverride.Shop && ShopAvatarVariant(...)) return;`（`ItemDrawer.cs` **0 行数变化**），
`Shop` 档的 5 处差异（主图、阵营名显隐、阵营名版式、底板、名字行）全部落在新文件
`Shell/ItemDrawerPartial_B114W16.cs`，并配 `Editor/B114_W16_Assert.cs`（13 条断言，含灭自证）。

**⚠️ 三处与施工单/前置报告不同（每条都在下面写了反证）**：

1. 🔴 **施工单说的「`Content` 也分叉」不成立** —— 两份 prefab 的 `Content` 锚点/`sizeDelta` **逐值相同**；
   `avatar` 自己的锚点也**逐值相同**，它落点变了**只是因为父件 `avatarBG` 换了**（继承）。
   真分叉的是 **`Background`(激活态) / `avatarBG` / `Army` / `Label`** 四条（+ 字号/墨色/折行/对齐四项文本属性）。
2. 🔴 **`Army`/`Name` 的字号是 90，不是 75** —— `B114_W11` 那份 §五 与 AssetRipper 导出都写着 75。见 §一·②。
3. 🔴 **这一档的 `Background` 是关着的**（`m_IsActive: 0`）—— 施工单与前置报告**都没提**这一条，
   而它比「阵营名反方向」更容易看漏：它不是显隐层，是**底板整块不该画**。见 §一·③。

---

## 一、原版判据（逐字段现读，不靠名字猜）

### 判据来源与口径

**主源 = 真包当前 build**（与 `Shell/ItemDrawerLayout.cs` 里**全部**版面常量同一条读法）：

```bash
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Avatar Drawer Shop Variant" \
       --depth 4 --relative --no-sprite --no-ancestor-scale
# 出处 d:/2/新解包资源/assets_full/bundle_menus_assets_all
```

**对照源 = `d:/2/tools/assetripper_out/ExportedProject/Assets/Avatar Drawer Shop Variant.prefab`**（`!u!224` 锚点 + `!u!1` 的 `m_IsActive` + `!u!114` 的 TMP 字段）。

### ① 几何（两源**逐值一致**，可直接照做）

| 节点 | 锚点 / sizeDelta | 设计 px（根 712.8×1080 · 左上原点 · y 向下） | 基础档 `Avatar Drawer` 同格 | 差 |
|---|---|---|---|---|
| 根 | ARF 高控宽 **0.66** | 712.8×1080 | 同 | 无 |
| `Content` | (0.5,0.5)-(0.5,0.5) · size (712,1080) | (0.4,0)-(712.4,1080) | **逐值相同** | **无** |
| `avatarBG` | (0,0.38)-(1,0.95) · size 0 | **(0.4,54)-(712.4,669.6)**（712×615.6） | (139.8,233.3)-(571,712.3)（431.2×479） | 是 |
| `avatar`（`image` 槽） | (-0.475,-0.175)-(1.52,1.35)（相对 `avatarBG`） | **(-337.8,-161.46)-(1082.64,777.33)** = 1420.44×938.79 | (-65.0,65.6)-(795.2,796.1) = 860.21×730.48 | **最大一处** |
| `Army` | (0,0.26)-(1,0.35) · size 0 | **(0.4,702)-(712.4,799.2)**（712×97.2） | (-34.3,186.1)-(265.7,286.1)（300×100） | 是 |
| `Label` | (0,0.14)-(1,0.25) · size **(4,0)** | **(-1.6,810)-(714.4,928.8)**（716×118.8） | (36,810)-(676.8,918)（640.8×108） | 是 |
| `Label/Name` | `HorizontalLayoutGroup` pad L/R 25 | **(23.4,810)-(689.4,928.8)**（666×118.8） | (61,810)-(651.8,918)（590.8×108） | 是 |

`avatar` 的落点算式（父件换了的继承结果，不是它自己改了）：
```text
avatarBG   = x 0.4..712.4 · y(向下) 54..669.6   （高 615.6）
avatar.x1  = 0.4  + 712 × (-0.475) = -337.8
avatar.x2  = 0.4  + 712 × ( 1.52 ) = 1082.64
avatar.y1  = 669.6 - 615.6 × 1.35  = -161.46
avatar.y2  = 669.6 - 615.6 × (-0.175) = 777.33
```

### ② 🔴 两源打架的那一项：`Army` / `Name` 的字号 = **90**（不是 75）

| 源 | `Army` fontSize / max / min / base | 墨色 | 对齐 | 折行 |
|---|---|---|---|---|
| **真包（现行 build）** | **90 / 90 / 12 / 36** | **(1,1,1,1) 白** | Center/**Middle** | **0（不折）** |
| AssetRipper 导出 | 75 / 75 / 12 / 36 | (1,1,1,1) 白 | Center/Middle | 0 |
| 基础档 `Avatar Drawer`（两源一致） | 75 / 75 / 12 / 36 | 白 | Center/Middle | 0 |
| 通用可见档（`Alliance Badge` / `Random Card` / `Premium`） | 75 | **(0.689,0.689,0.689,0.851) 灰** | Center/**Bottom** | **1（折）** |

**取 90，三条反证**：

1. 真包 `bundle_menus_assets_all/MonoBehaviour/` 全扫：`m_fontSize == 90` 的 `'Goff'` / `'Legendary Wildcard'`
   **各只有一颗**（全库共 2 条），而且 `'Goff'` 那颗的 RectTransform 锚点**逐值等于本 prefab 的 `Army`**
   （`(0,0.25999999)-(1,0.34999999)`，`RectTransform_6534585956179331196.json`）⇒ 它**就是**这一档的 Army，
   不是别处另一份。`Name` 那颗同理（`RectTransform_-2551090149645727620.json`，锚 (0,0)-(0,0) + piv 0.5 = 布局组子件）。
2. 本工程 `ItemDrawerLayout.cs` 里**全部**版面常量都是从这条读法（真包 + `menu_dump`）出来的，
   拿另一条源去改一个字段 = 两套口径并存。
3. AssetRipper 那份导出里两份 prefab 的 TMP **共用同一个 pathID**（`114694121525821480`），
   而它们的 RectTransform 又确实不同 ⇒ 那张表是「prefab variant 的 override 只合进去了一部分」
   （矩形合了、TMP 覆盖没合），**不能当这两个字段的判据**。

### ③ 🔴 施工单没写、但确实存在的一条：`Background` 是关着的

| 项 | `Avatar Drawer`（基础） | `Avatar Drawer Shop Variant` |
|---|---|---|
| `Background` GameObject `m_IsActive` | **1** | **0** |

判据链（**只看 prefab 字段会漏掉「谁会把它打开」这一步**，所以两头都读了）：
- `ItemDrawer<T>.Draw`（`d:/2/tools/decomp_generic_trial/out/ItemDrawer_object___Draw.c`，全文 175 行）
  只对 **`quantity`**（`+0x38`）与 **`label`**（`+0x30`）两格调过 `GameObject.SetActive`，
  **`background` 那格一次都没碰** ⇒ 序列化态说了算 ⇒ `Shop` 档**没有**那张 `40k_Rewards_icon_base card` 底板。
- `AvatarDrawer.Draw`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarDrawer.cs:15-24`）只有
  `image.sprite = item.Image;` 与 `armyText.text = GetLocalizedArmyName(...)` 两句，也不碰底板。

### ④ 其余（两源一致，逐条核过）

- `quantity` 槽 = `null`，`Label` 下**没有** `Quantity` 子件 ⇒ 这一档**永不画数量**（与基础档同）。
- `Premium Highlight` / `Converted Drawer` / `Ephemeral Drawer` / `Collected Badge` 出厂 **inactive**（与基础档同）。
- `Label` / `Avatar` 的 `Image` 组件 `m_Material` 两边都是 `{fileID: 0}`（默认材质）——
  施工单里那句「两处材质 guid 分叉」**在 `m_Material` 上不成立**；两份 prefab 的 `Image` 组件字段逐值相同，
  **唯一的材质类差异是 `Background` 那颗在新档里 `m_Color.a = 0` + `m_Enabled = 0`**（它本来就不画，合起来同义）。

---

## 二、我们原来是什么（改前）

`Dispatch`（`Shell/ItemDrawer.cs:1918` 那条 `{` 行）只分了一档：

```csharp
{ if (ov == DrawerOverride.Icon && IconVariant(drawer, node, box, item, quantity, style, ref res)) return;   // 🔴 B114_WC（B-223）…
```

`Shop` 档因此落进 `case DrawerAvatar: Avatar(node, box, item, quantity, style, ref res); break;`（基础档那套）：

| 项 | 改前（基础档那套） | 原版 `Shop` 档 |
|---|---|---|
| 主图 | `AvatarImageFrame(fr)` = (-65,65.6)-(795.2,796.1) | (-337.8,-161.46)-(1082.64,777.33) |
| 底板 | 画 `Content/Background` | **不画** |
| 阵营名 | 建出来又 `SetActive(false)`（`B-242` 的结论） | **建出来且显示** |
| 阵营名版式 | (0.4,702)-(712.4,810) · 灰 · 75 · 折行开 | (0.4,702)-(712.4,799.2) · 白 · 90 · 不折行 · Middle |
| 名字行 | `Label` (36,810)-(676.8,918) · 75 | `Label` (-1.6,810)-(714.4,928.8) · 90 |

---

## 三、改成什么

新增 `Shell/ItemDrawerPartial_B114W16.cs`（`partial class ItemDrawer` 类内新成员，⛔ 不推主文件任何账落点）：

| 成员 | 干什么 |
|---|---|
| `ShopAvatarVariant(drawer, …)` | `Dispatch` 的 Shop 档分流；只认 `DrawerAvatar`，其余返回 `false` |
| `ShopAvatar(node, box, item, qty, st, ref res)` | 这一档的 `AvatarDrawer.Draw`：`FrameOf(box)` + `DrawerContent`（与基础档同），**跳过 `DrawerBackground`**，主图/阵营名/名字行按新常量 |
| `DrawShopArmyName(...)` | `Content/Army`：白字 · 字号 90 · Center/Middle · **不折行** · **保持激活** |
| `DrawShopName(...)` | `Content/Label` + `Label/Name`：新两格 + 字号 90 + 白 + 不折行 |

**为什么不改 `Shell/ItemDrawerAvatar.cs`**：那个文件不在本波白名单（W-11 已交回，但调度台只给了我
`ItemDrawer.cs` + 新建 partial + 新建断言件）。走 `Dispatch` 分流**也与 `B114_WC` 的 Icon 档同一套做法**，
两条变体将来可以并到一处。

**闸与基础档对齐**：`st.NamePx <= 0f` ⇒ 不建文字层（与 `DrawerArmyName` / `DrawerNameQuantity` 同一条闸）；
`item.CardArmy` 的 `LocalizedName` 空串 ⇒ 不建 `Army` 节点。

---

## 四、改动清单 + 净增净减

| 文件 | 改动 | 净行数 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs` | **仅 1 行**：`Dispatch` 的 `{` 行（`:1918`）**同物理行**追加 `if (ov == DrawerOverride.Shop && ShopAvatarVariant(drawer, node, box, item, quantity, style, ref res)) return;`（落在该行**第一个 `//` 之前**） | **0**（2497 → 2497；`git diff --numstat` = `1 / 1`） |
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B114W16.cs`（**新建**） | Shop 档分流 + 实现 + 判据头注 | 新文件 **148** 行 |
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B114W16.cs.meta`（**新建**） | guid `d5a17e6c4b93f2087c1e3a94f6b02d58`（全仓 grep 0 命中） | 新文件 |
| `Unity/MyGame/Assets/CardPresentation/Editor/B114_W16_Assert.cs`（**新建**） | 13 条断言（Shop 10 条 + 灭自证 4 条，其中 D1/D1b/D0 为前提） | 新文件 **322** 行 |
| `Unity/MyGame/Assets/CardPresentation/Editor/B114_W16_Assert.cs.meta`（**新建**） | guid `9e4b7c2a15f83d60ab27e5c93f60d184`（全仓 grep 0 命中） | 新文件 |

**行号纪律自检**（`io.open(p,'rb')` 现读）：

| 文件 | 行数 | CRLF | 裸 CR |
|---|---|---|---|
| `Shell/ItemDrawer.cs` | **2497（= 开工基线 2497）** | 0 | 0 |
| `Shell/ItemDrawerPartial_B114W16.cs` | 148 | 0 | 0 |
| `Editor/B114_W16_Assert.cs` | 322 | 0 | 0 |

`ItemDrawer.cs:1918` 现读（第 228 个字符处才是第一个 `//`，两句代码都在它**之前**）：

```text
{ if (ov == DrawerOverride.Icon && IconVariant(...)) return; if (ov == DrawerOverride.Shop && ShopAvatarVariant(...)) return;   // 🔴 B114_WC（B-223）…
```

---

## 五、🔴 接线（可粘贴 · **由调度台统一追加**，写手没碰宿主）

> 调度台 2026-11-01 改派：`W-12` 也要往**同一物理行**追加，两人会互相覆盖 ⇒ 本波**不动** `ShellScene.cs`。
> ✅ 我**没有**加过任何接线（`ShellScene.cs` 的改动全部是别人的；我只读过它）。

**宿主 = `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs`，第 11093 行**（那条 8078 字符的聚合行）。

- 该行**第一个 `//` 在第 7216 个字符位**（0 基）。
- **插在第 7150 个字符位**（0 基）= 紧跟 `B114_W12_Assert.Fail; ` 之后、`Debug.Log(P + $"=== 合计` 之前 ⇒ **在第一个 `//` 之前**，行数零变化。

**要插入的那一段（一字不改地粘）**：

```csharp
B114_W16_Assert.Run(); _sink.Pass += B114_W16_Assert.Pass; _sink.Fail += B114_W16_Assert.Fail; 
```

**落点前后文（当前现读，供核对锚点唯一性）**：

```text
… B114_W11_Assert.Run(); _sink.Pass += B114_W11_Assert.Pass; _sink.Fail += B114_W11_Assert.Fail; B114_W12_Assert.Run(); _sink.Pass += B114_W12_Assert.Pass; _sink.Fail += B114_W12_Assert.Fail; ⟨此处插入⟩ Debug.Log(P + $"=== 合计：{_sink.Pass} 通过 / {_sink.Fail} 失败 ===");   // 🆕 2026-10-29（调度台代接线 …
```

**调用链自证**：
`ShellScene.Run()`（唯一宿主）→ 那条聚合行 → `B114_W16_Assert.Run()`（`public static`，全局命名空间，与 `B114_W11_Assert` 同形）
→ `SectionShop()` + `SectionDefaultControl()` → `ItemDrawer.Draw(..., DrawerOverride.Shop, st)`
→ `ItemDrawer.Draw` → `Dispatch(..., ov: Shop)` → 本波新加的那一句 `ShopAvatarVariant(...)` → `ShopAvatar(...)`。
`Pass` / `Fail` 两个静态属性直接累加进 `_sink`（与同一行上其它 60 多个断言件同一写法）。

---

## 六、断言（`Editor/B114_W16_Assert.cs`）

夹具：`BoxBase = (0,0)-(712.8,1080)`（`K = 1`，设计 px 原值）· `ArtAvatar = "Avatar_AM_Attilan Rough Rider"`
（与 `B114_C` / `B114_W11` 同一张，`Resources/Art/avatars/` 现成可取）· `CardArmy = 10`（Ultramarines）
· `ItemDrawerStyle.Default(10,11,12)`（`QuantityPx 34 / NamePx 26` **都非 0** ⇒ 文字层是真开着的，不是把宿主设成 0 换来的绿）。

| 段 | 断什么 | 期望值（**独立常量**，来自 prefab，⛔ 不读我们代码的常量） |
|---|---|---|
| **S0 / S0b** | **硬前提**：头像图取得到 · `ArmyIcons.LocalizedName(10)` 非空 | ——（⛔ 不是 `if (图) Check(...)` 静默跳过：这两条自己就是断言） |
| S1 / S1b / S2 | 前提：根建出来 · 路由到 `AvatarDrawer` · `Content` 建出来 | —— |
| S3 | 前提：`Army` 节点**建了** | —— |
| **S4** | **结论**：`Army.activeSelf == true` | 原版 `m_IsActive: 1` |
| **S5** | `Army` **框心** = (356.4, 750.6) | prefab 锚 (0,0.26)-(1,0.35) 的矩形中心 |
| S5b / S5c / S5d | `Army` 文本 = `LocalizedName(10)` · 字号上限 **90 px** · 墨色 **白** | 90 / (1,1,1,1) |
| **S6** | 主图矩形 = `Fit(tex, 过扫框)` | (-337.8,-161.46)-(1082.64,777.33) |
| **S7** | **不建** `Background` | 原版 `Background` `m_IsActive: 0` |
| S8a / S8b | `Label` 在 · 框 = (-1.6,810)-(714.4,928.8) | prefab 锚 + sizeDelta.x 4 |
| S8c / S9 | `Label/Name` 在 · 字号上限 **90 px** | 90（基础档 75） |
| S10 | 不建 `Quantity` | `quantity` 槽 = null |
| **D2 / D3** | 基础档：`Army` 在但 **inactive** · **有** `Background` | `Avatar Drawer` 的 0 / 1 |
| **D4** | **灭自证**：两档 (底板在否, 阵营名开否) = 基础 **(有,关)** vs `Shop` **(无,开)**，**互为反相** | —— |

**每条怎么才能红（灭自证）**

| 断言 | 怎么红 |
|---|---|
| S4 | **删掉 `Dispatch` 那条 `ov == Shop` 分流**（落回基础档）⇒ `ItemDrawerAvatar.cs` 的 `SetActive(false)` 把节点关掉 ⇒ 红 |
| S5 | 用回通用可见档那格 `DrArmyY1/Y2`（中心 756）或基础档那格（中心 236.1）⇒ 红 |
| S5c / S9 | 照基础档给 75（或用回 `DrawerNameQuantity`）⇒ 红 |
| S6 | 用回 `AvatarImageFrame`（(-65,65.6)-(795.2,796.1)）⇒ 红 |
| S7 | 让 Shop 支仍调 `DrawerBackground` ⇒ 红 |
| S8b | 用回基础档那格 (36,810)-(676.8,918) ⇒ 红 |
| **D4** | **谁把两档画成同一套（Shop 也画底板 / 或 Shop 的阵营名也关掉）⇒ D4 与 S4/S7 一起红** |

**两条量具口径**（都转调全仓唯一实现，⛔ 没写第二份公式）：
- 图：`MenuDraw.QuadRectPx(ImageQuad)`（= `B114_C` / `C5_Assert` 那一份）。
- 容器/框心：`PixelOfDesign(PosInDesignSpace(t))` + `rect.rect`（= `Editor/C5_Assert.cs` 的 `NodeRect` 同一套）。

**⚠️ 一处「没能断死」的地方（如实记）**：`Army` / `Name` 这两个**世界档 `Label`** 挂在**纯 `Transform`** 上
（`Label.CreateCore` 建的是 `new GameObject(goName)`，TMP 在它的子节点）⇒ **没有 `RectTransform` 可量宽度**。
所以：`Army` 只断**框心**（(356.4,750.6) 与基础档的 (115.7,236.1)、通用档的 (356.4,756) 都可区分）；
`Name` 与 `Label` **同心**（两者中心都是 356.4,869.4），框心断不出差别 ⇒ 这一格改用**字号 90** 当判别量，
宽度那半由 `Label` 那一格（S8b，`Label` 是 `MenuDraw.Node` 建的、有 `RectTransform`）+ 生产代码里
「`Name` = `Label` 左右各收 pad 25」这一句共同锁住。`NameRect` 常量仍写在断言件文件头，供复核。

---

## 七、没查清 / 未接的部分（如实记）

1. 🔴 **生产调用点的 `NamePx = 0`** —— `Shell/ShopTabPage.cs:193-204` 的 `DrawerStyle` 里
   `st.QuantityPx = 0f; st.NamePx = 0f;`（那套版式本身标着 `🔶 自己设计（非原版）`：商店格自己画类型带/名字带）。
   ⇒ **真跑起来这一档只会画主图**（阵营名与名字行会被 `st.NamePx <= 0f` 那道闸挡掉）。
   本活只保证「`ov = Shop` 分流时几何与显隐**按原版**」，**上游要不要给文字层是商店格自己的事**，不在本活范围。
   （`ShopData.cs` 目前也没有 `PlayerAvatar` 商品 ⇒ 生产暂未触发；断言直接按 `Shop` 调 `Draw`，与 `ShopTabPage.cs:565` 同形。）
2. **非方框调用下的 ARF 行为仍未读** —— 本支沿用 `FrameOf(box)`（`min(box.H/1080, box.W/712.8)` 等比内接），
   与 `ItemDrawerLayout.cs` 文件头 ① 那条 TODO 是同一条（原版 ARF 在窄容器里是溢出还是按宽缩**没实测**）。
3. **`Horizontal`(15) / `OfferPopups`(30) 两档没接** —— `ItemTypeSets` 里本族这两列都是 `null`（= 回落主档），
   真包也没有对应的 Avatar 变体 prefab ⇒ 现在不接是对的；但**没逐份核过那两档有没有别的变体**。
4. **断言没在 Unity 里跑过**（本波不跑 Unity）。风险最高的一条是 `S5c/S9` 的
   「字号上限折算回 px = 90」—— 它走 `Label.FontSizeToPxNow(FontSizeMax)`（`Label.FontSizeMax` 的 doc
   明写这条口就是「自检拿它跟原版的 `m_fontSize` 比」），但我**没有实跑验证**它在世界档 `Label` 上读出来的就是 90.0。
   若首次跑出来不是 90（例如差一个比例），判据在 `Battle/Label.cs:1376-1379`（`fontSizeMax = cur * (maxPx / nomPx)`），
   按那条算式复核即可，⛔ 别把断言改成「随便一个容差」。

---

## 八、顺手发现（⛔ 都没改，交调度台分流）

1. 🔴 **`B-249` 的同形兄弟：`CosmeticItemAvatarBorder` 的 Shop 档也没做。**
   `ItemTypeSets`（`Shell/ItemDrawer.cs:421`）里 `CosmeticItemAvatarBorder` 的 `Shop` 列指向 **`AvatarBorderDrawer`**，
   真包里有 **`Avatar Border Drawer Shop Variant.prefab`**，它和基础档的唯一差别是
   **`Content/Background` 的 `m_IsActive`：基础档 `1`、Shop 变体 `0`**（其余 `Army` / `avatar` / `Label` 逐值相同）。
   我们的 `ItemDrawerCosmetic.cs:69` 的 `AvatarBorder(...)` **无条件**调 `DrawerBackground` ⇒ 商店档会多画一张底板。
   **改法很小**（`ShopAvatarVariant` 这个口加一个 `case DrawerAvatarBorder`，实现里只跳过 `DrawerBackground`），
   但**本波白名单不含 `ItemDrawerCosmetic.cs`**（而且它不在任何人的黑名单里，需要调度台先分配所有权）⇒ 只记不做。
2. **`Avatar Drawer`（基础档）的 `Army` 几何我们画的是「通用可见档」那一格，不是它自己那一格。**
   prefab 现读：基础档 `Army` = **(-34.3,186.1)-(265.7,286.1)（300×100）· 白 · 75 · Middle · 不折行**；
   我们（`DrawerArmyName`）画的是 **(0.4,702)-(712.4,810) · 灰 · 75 · Bottom · 折行** —— 那是
   `Alliance Badge` / `Random Card` / `Premium` 三份**真有阵营名**的 prefab 的尺。
   因为基础档这颗节点是 `m_IsActive: 0`（`B-242` 已让它不可见），**屏幕上零差异**，只有「节点框」不同。
   若要严格照原版，得给 `Avatar` 档单独一档几何 —— 属 `ItemDrawerAvatar.cs` 的活，本波不在白名单。
3. **`Icon Avatar Drawer Variant` 与基础档的 `Army` 几何也是同一条**（(-34.3,186.1)-(265.7,286.1)），
   而 `B114_WC` 那一档是**干脆不建** `Army` 节点 ⇒ 与 §八·2 是同一族「节点框不同」，不影响画面。
4. **`Background` 那颗在两份 prefab 里的 `Image` 组件字段其实也不同**：Shop 变体 `m_Color.a = 0` + `m_Enabled = 0`，
   基础档 `(1,1,1,1)` + 启用。因为整颗 GameObject 本来就关着，这一层**叠加同义**；记在这里只为
   「别人读那份 diff 时别再把它当成一条独立差异」。
