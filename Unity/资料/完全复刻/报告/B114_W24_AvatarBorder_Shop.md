# B114_W24 · `B-256` `Shop` 档头像边框变体（`Avatar Border Drawer Shop Variant`）

> 写手 `W-24` · 2026-11-01 · 第一百一十四会话 · 账 `B-256`（`B-249` 的同形兄弟）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改宿主 `Editor/ShellScene.cs`（接线片段见 §五，交调度台统一追加）。
> **typecheck（`TMPDIR=/tmp/wf_w24 bash 工具/typecheck.sh`）= 运行时 0 / 编辑器 0。**
> **账闸（只读 `python -I -X utf8 工具/check_ledger_points.py`）= A 5841 条 · 通过 5841 · 漂移 0 · 失败 0。**

---

## 〇、结论（一句话）

原版 `CosmeticItemAvatarBorder` 的 `Shop`(20) 档是**另一套 prefab**（`Avatar Border Drawer Shop Variant`，
同一个类 `AvatarBorderDrawer`），它与基础档的**唯一行为差异 = 不画那块 `Content/Background` 底板**；
我们的 `AvatarBorder(...)` 原先**无条件** `DrawerBackground` ⇒ 商店档多画一张底板。
已按 `B114_W16`（`B-249`）同一套做法修好：`Dispatch` 里**同物理行**加一句 `ov == Shop` 分流，
实现落在新文件 `Shell/ItemDrawerPartial_B114W24.cs`，基础档调用点**零改动**（新的 `drawBackground` 形参带默认值）。

**⚠️ 与施工单/前置报告一致，没有推翻项**（唯一补强：施工单只说「`m_IsActive` 1→0」，我把整份 `diff` 拉出来确认
**整份 prefab 只有 3 行不同**，因此几何一个字都不用动 —— 这一点与 `B-249` 那档相反，那一档还翻了 `Army` 显隐与字号）。

---

## 一、原版判据（两源，逐字段现读）

### ① 主源 = 真包当前 build（与本仓 `ItemDrawerLayout.cs` 全部版面常量同一条读法）

```bash
python -I -X utf8 d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Avatar Border Drawer Shop Variant" \
       --depth 4 --relative --no-sprite --no-ancestor-scale
# 出处 d:/2/新解包资源/assets_full/bundle_menus_assets_all
```

| 节点 | 真包读数 | 基础档 `Avatar Border Drawer` 同格 | 差 |
|---|---|---|---|
| 根 | ARF 高控宽 **0.66** ⇒ 712.8×1080 | 同 | 无 |
| `Content` | 712×1080（(0.4,0)-(712.4,1080)） | 同 | 无 |
| **`Background`** | **`INACT`（`m_IsActive = 0`）**，`Image` 组件 `m_Enabled=0` + `Simple (1,1,1,0)` | `m_IsActive = 1` · `(1,1,1,1)` | **是（唯一的行为差异）** |
| `avatarBG`（`image` 槽） | (139.8,233.3)-(571.0,712.3) | 同 | 无 |
| `avatar` | (-65.0,65.6)-(795.2,796.1) | 同 | 无 |
| `Army` | (-34.3,186.1)-(265.7,286.1)，`INACT` | 同（也 `INACT`） | 无 |
| `Label` | (36.0,810.0)-(676.8,918.0) | 同 | 无 |
| `Label/Name` | (61.0,810.0)-(651.8,918.0) | 同 | 无 |

### ② 交叉源 = 两份 AssetRipper 导出的 `diff`（**整份只有 3 行不同**）

```bash
cd "d:/2/tools/assetripper_out/ExportedProject/Assets"
diff "Avatar Border Drawer.prefab" "Avatar Border Drawer Shop Variant.prefab"
```

```text
16c16   m_Name: Avatar Border Drawer            → Avatar Border Drawer Shop Variant
57c57   m_IsActive: 1                           → m_IsActive: 0                 ← `Background` 那颗 GameObject
625c625 m_Enabled: 1                            → m_Enabled: 0                  ← 同一个 `Background` 的 Image
631c631 m_Color: {r:1,g:1,b:1,a:1}              → {r:1,g:1,b:1,a:0}
```

`m_IsActive: 0` 那一行属于 `Background`（`!u!1 &1724804207875695`，`m_Name: Background`，父 = `Content` 的 `224969106005120503`）。
后两行（`m_Enabled=0` / `m_Color.a=0`）与「整颗 GO 关着」**叠加同义**（`B114_W16` 报告 §八·4 对
`Avatar Drawer Shop Variant` 记过同一条）⇒ 按「不显示」处理即可，⛔ 不要为它再建一个透明节点。

### ③ 判据链的另一半：**谁在运行时把底板打开？没人**

- `ItemDrawer<T>.Draw`（`d:/2/tools/decomp_generic_trial/out/ItemDrawer_object___Draw.c`，全文 175 行）只对
  `quantity`(＋0x38) 与 `label`(＋0x30) 两格调过 `GameObject.SetActive`，**`background` 那格一次都没碰**；
- `AvatarBorderDrawer.Draw`（`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarBorderDrawer.cs:15-24`）全篇一句
  （`base.image.sprite = item.Image;`），也不碰底板。

⇒ **序列化态说了算** ⇒ 商店档屏幕上**没有**那张 `40k_Rewards_icon_base card`。

---

## 二、我们原来是什么（改前）

| 位置 | 改前 |
|---|---|
| `Shell/ItemDrawer.cs:421`（`ItemTypeSets`） | `CosmeticItemAvatarBorder` 的 Shop(20) 列 = `"AvatarBorderDrawer"` —— **表是对的**（同一个类、两套 prefab） |
| `Shell/ItemDrawer.cs:1918`（`Dispatch`） | 只有 `ov == Icon`（`B114_WC`）与 `ov == Shop → ShopAvatarVariant`（`B114_W16`，只认 `DrawerAvatar`）两次分流 ⇒ `AvatarBorderDrawer` **一次也没消费 `ov`** |
| `Shell/ItemDrawerCosmetic.cs:69`（`AvatarBorder`） | **无条件** `DrawerBackground(content, fr, st)` ⇒ 商店档多画一张底板 |

---

## 三、改成什么

| 文件 | 改动 |
|---|---|
| `Shell/ItemDrawerCosmetic.cs` | `AvatarBorder(...)` 多一个 **`bool drawBackground = true`** 形参（默认值 ⇒ 基础档调用点逐字不变）；体内改成 `if (drawBackground) DrawerBackground(...)`。文件头新增「③ `Shop` 档」一节，把两源判据与判据链落盘 |
| `Shell/ItemDrawerPartial_B114W24.cs`（新建） | `ShopAvatarBorderVariant(drawer, …)` = `Dispatch` 的 Shop 档分流，**只认 `DrawerAvatarBorder`**，命中 → 调 `AvatarBorder(..., drawBackground: false)` 并返回 `true`；其余返回 `false`（照旧走 `switch`） |
| `Shell/ItemDrawer.cs` | **仅 1 行**：`Dispatch` 的 `{` 行（`:1918`）在 `W-16` 那句之后**同物理行**追加 `if (ov == DrawerOverride.Shop && ShopAvatarBorderVariant(...)) return;` |
| `Editor/B114_W24_Assert.cs`（新建） | 14 条断言（Shop 10 条含 6 条硬前提 + 灭自证 4 条） |

**为什么不用 `W-16` 那个 `ShopAvatarVariant` 加一个 `case`**：`Shell/ItemDrawerPartial_B114W16.cs` 是 `W-16` 的产物、**已定稿且在本波黑名单**（同一文件同一时刻只能一个写手）⇒ 按铁律 13·3 另开本文件、**不碰它**。

**为什么保留 `DrawerAvatarBorder` 的同一条实现（不另抄几何）**：判据就是「两档几何逐值相同、只差底板」，
另抄一份 = 把一个开关放大成两次维护；而 `drawBackground` 带默认值 ⇒ `ItemDrawer.cs:1925` 那条 `case` 调用**一个字都不用改**。

---

## 四、改动清单 + 净增净减

| 文件 | 行数（改前 → 改后） | 净增 | `git diff --numstat` |
|---|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs` | **2497 → 2497** | **0** | `1 / 1`（同一物理行的行内追加；该行另有 `W-16` 未提交的追加，故与 HEAD 比是 1 行） |
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerCosmetic.cs` | 125 → **143** | **+18**（文件头判据节） | `20 / 2`（另 2 处是**同一行内替换**：形参行、`DrawerBackground` 行） |
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerPartial_B114W24.cs`（新建） | — → **67** | 新文件 | untracked |
| `…/Shell/ItemDrawerPartial_B114W24.cs.meta`（新建） | — | 新文件 | guid `b7c24e93d8a04f1e86a5c3d0e4b9f712`（全仓 grep 0 命中） |
| `Unity/MyGame/Assets/CardPresentation/Editor/B114_W24_Assert.cs`（新建） | — → **279** | 新文件 | untracked |
| `…/Editor/B114_W24_Assert.cs.meta`（新建） | — | 新文件 | guid `c4f18a2b6d7e0398455bf1b2d3c6a8e9`（全仓 grep 0 命中） |

无新增 `.cs` 之外的产物；⛔ 没有改任何 `账/*.tsv` · `Scenes/*.unity` · 正本 · 别人的文件。

**行号纪律自检**（`io.open(p,'rb')` 现读，不是文本模式）：

| 文件 | 行数 | CRLF | 裸 CR |
|---|---|---|---|
| `Shell/ItemDrawer.cs` | **2497（= 开工基线 2497）** | 0 | 0 |
| `Shell/ItemDrawerCosmetic.cs` | 143 | 0 | 0 |
| `Shell/ItemDrawerPartial_B114W24.cs` | 67 | 0 | 0 |
| `Editor/B114_W24_Assert.cs` | 279 | 0 | 0 |

`ItemDrawer.cs:1918` 现读：**该行第一个 `//` 在字符位 345（0 基），我这段代码起于字符位 259** ⇒
在注释**之前**（同族教训：把代码追加到行末 `//` 之后 = 静默不执行）。

```text
{ if (ov == …Icon… ) return; if (ov == …Shop… && ShopAvatarVariant(…)) return; if (ov == DrawerOverride.Shop && ShopAvatarBorderVariant(drawer, node, box, item, quantity, style, ref res)) return;   // 🔴 B114_WC（B-223）…
```

---

## 五、🔴 接线（可粘贴 · **由调度台统一追加**，写手没碰宿主）

> 现状现读（2026-11-01）：`Editor/ShellScene.cs` 那条聚合行（第 **11093** 行，长 8078 字符，第一个 `//` 在字符位 **7216**）
> **还没有** `B114_W16_Assert` 的接线（`grep -c "B114_W16_Assert.Run()"` = **0**）⇒ 我与 `W-16` 的接线都要由调度台补，
> 两段**并排**插在同一处即可（顺序无所谓）。

**锚点**：紧跟 `B114_W12_Assert.Fail; ` 之后、`Debug.Log(P + $"=== 合计` 之前 ——
即 `…B114_W12_Assert.Run(); _sink.Pass += B114_W12_Assert.Pass; _sink.Fail += B114_W12_Assert.Fail; ⟨此处插入⟩ Debug.Log(P + $"=== 合计：…`

**要插入的那一段（一字不改地粘）**：

```csharp
B114_W24_Assert.Run(); _sink.Pass += B114_W24_Assert.Pass; _sink.Fail += B114_W24_Assert.Fail; 
```

**调用链自证**：
`ShellScene.Run()`（唯一宿主）→ 那条聚合行 → `B114_W24_Assert.Run()`（`public static`，全局命名空间，与 `B114_W11/W12/W16_Assert` 同形）
→ `ItemDrawer.Draw(..., DrawerOverride.Shop, st)` → `PickDrawer`（`ItemTypeSets:421` 的 Shop 列 = `AvatarBorderDrawer`）
→ `Draw` 的 ④ `Dispatch(..., ov: Shop)` → 本波新加的那一句 `ShopAvatarBorderVariant(...)` → `AvatarBorder(..., drawBackground: false)`。
`Pass` / `Fail` 两个静态属性直接累加进 `_sink`（与同一行上其它 60 多个断言件同一写法）。

---

## 六、断言（`Editor/B114_W24_Assert.cs`，14 条）

夹具：`BoxBase = (0,0)-(712.8,1080)`（`K = 1`，设计 px 原值）· `Type = "CosmeticItemAvatarBorder"` ·
`Art = "Player_Profile_Border"`（`Resources/Art/ui/` 现成可取）· `ItemDrawerStyle.Default(10,11,12)`
（`QuantityPx 34 / NamePx 26` **都非 0** ⇒ 文字层真开着，不是把宿主设成 0 换来的绿）。

| 段 | 断什么 | 期望（**独立 prefab 常量**，⛔ 不读 `ItemDrawer` 的常量） |
|---|---|---|
| **P0** | **硬前提**：共享底图 `40k_Rewards_icon_base_card` 取得到（`CardArt.MenuUi`） | —— |
| **P0b** | **硬前提**：边框图 `Player_Profile_Border` 取得到 | —— |
| P1 / P1b / P2 | 前提：根建出来 · 路由到 `AvatarBorderDrawer` · `Content` 建出来 | `ItemTypeSets:421` |
| **P3** | 前提：主图 `Image` **建出来了** —— C1 的反向守卫（防「整格没画」骗过「没有底板」） | —— |
| **C1** | **结论**：`Shop` 档 `Background` **不存在或未激活** | prefab `m_IsActive: 0` |
| **C2** | 主图框 = `Fit((139.8,233.3)-(571.0,712.3))` | 两档逐值相同 |
| C3a / **C3b** | `Label` 建了 · 框 = (36.0,810.0)-(676.8,918.0) | prefab 实读 |
| C4 / C5 | `Label/Name` 建了 · **不建** `Quantity` | `quantity` 槽 null |
| D1 / D2 / **D3** | 前提：两档根都建了 · 同一抽屉类 · **基础档有 `Background` 且 `activeSelf == true`** | `m_IsActive: 1` |
| **D4** | **灭自证**：`基础(有底板·激活) ∧ Shop(无底板) ∧ 两档主图矩形与 `Label` 矩形逐值相同` | 判据 = 两份 prefab 只有那一处差异 |

**每条怎么才能红（灭自证）**

| 断言 | 怎么红 |
|---|---|
| **C1** | 让 Shop 支仍调 `DrawerBackground`（或删掉 `Dispatch` 那条 `ov == Shop` 分流） |
| **D3** | 把 `AvatarBorder` 的 `drawBackground` 默认值改成 `false`（基础档也不画） |
| **D4** | 谁把两档画成同一套（Shop 也画底板）⇒ D4 与 C1 一起红；谁把 Shop 支换成另一套几何 ⇒ `sameImg`/`sameLabel` 红 |
| P3 | 谁靠「Shop 整格没画」把 C1 骗绿 ⇒ P3 红（**前提断言本身计失败**，不是 `if (前提) Check(...)` 静默跳过） |
| C2 / C3b | 照 `B-249` 的 `ShopAvatar` 那批常量（或任何别的档）给 |

**两条量具口径**（都转调全仓唯一实现，⛔ 没写第二份公式）：
图 → `MenuDraw.QuadRectPx(ImageQuad)` · 容器/框 → `PixelOfDesign(PosInDesignSpace(t))` + `rect.rect`（与 `B114_W16_Assert.cs` 同一条）。

---

## 七、没查清 / 未接的部分（如实记）

1. **断言没在 Unity 里跑过**（本波不跑 Unity）。风险最高的两条：① `C3b` 的 `Label` 矩形走 `NodeRectOf`
   （`rect.rect` + `PixelOfDesign`），与 `B114_W16_Assert.cs` 的 `S8b` 同一套量法 —— 那条也**尚未实跑**过；
   ② `C2` 的期望值走 `CardbackFace.Fit`，同一套量法在 `B114_W16` 的 `S6` 里也没实跑过。
   若首次跑出来差一点点，**先回 prefab 复核框值**（(139.8,233.3)-(571.0,712.3)），⛔ 别直接放宽容差。
2. **生产暂未触发**：`ShopData.cs` 目前没有 `CosmeticItemAvatarBorder` 商品 ⇒ 这一档只有断言在走
   （与 `B-249` 同一现状）。上游什么时候上架头像边框，这一档才会真出现在商店里。
3. **`Horizontal`(15) / `OfferPopups`(30) 两档没接** —— `ItemTypeSets:421` 里本族这两列都是 `null`（= 照原版回落主档），
   真包 `bundle_menus_assets_all` 里本族只有 `Avatar Border Drawer` / `Avatar Border Drawer Shop Variant` 两份 prefab
   （另有一份 `Icon Avatar Border Drawer Variant`，但**本族 Icon 列写的是 `null`** ⇒ 原版这一族不出 Icon 档）。
   ⚠️ 那条「本族没有别档变体」是**按名字扫**得出的 —— 若将来发现某档走了别的命名，要回来重核（铁律：按名字搜不到 ≠ 不存在）。
4. **`DrawerOverride.Shop` 在别处是否也该分流**：本次只按 `ItemTypeSets` 的 Shop 列逐个核了本族；
   其它族（`CosmeticItemCardback` / `Wildcard` …）Shop 列都是 `null` ⇒ 不进本活。

---

## 八、顺手发现（⛔ 都没改，交调度台分流）

1. **`Icon Avatar Border Drawer Variant.prefab` 在真包里存在，但 `ItemTypeSets:421` 的 Icon 列是 `null`。**
   我按「表优先」处理（不接 Icon 档），但这条**值得有人回原版核一次**：要么那张 prefab 属于别的物品类型
   （例如 `CosmeticItemAvatarBorder` 的 is-a 子类），要么配置表那条记录我们抄漏了一格。
   ⇒ 与 `B114_WC`（Icon 变体 7 档）是同一族问题，建议并入那边的判据复核。
2. **`ItemDrawerCosmetic.cs` 现在有两个 `Background` 相关口径要对齐**：`CardbackDrawer` / `CosmeticDrawer` 是
   **原版根本没有那颗槽**（`background = {fileID: 0}`，见该文件 `DrawCosmeticOneImage` 的注释），
   而 `AvatarBorderDrawer` 的 Shop 档是**有槽但关着**。两者结果都是「不画」，但**判据不同**（一个判据为空、一个是 `m_IsActive: 0`）
   ⇒ 账里若合并成一条会丢掉这个区别，建议 `B-256` 的判据要点里保留这句。
3. **`ItemDrawer.cs:1918` 这条 `Dispatch` 行已经有 3 句 `ov` 分流的行内追加**（Icon / Shop-Avatar / Shop-AvatarBorder）。
   行本身仍合法、行数零变化，但**可读性在下降**；若后面还有第 4 个变体，建议调度台起一次「把这些分流收进一个
   `VariantDispatch(ov, drawer, …)`」的重构（那会动账落点，必须在没有 `.cs` 写手的同步点上做）。
4. **`B114_W16_Assert` 目前还没接进宿主**（`grep -c "B114_W16_Assert.Run()" Editor/ShellScene.cs` = **0**）——
   与本件同处一个接线点，调度台补的时候可以一次补齐两条。
