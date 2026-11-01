# B114_W11 · `B-242` 基础档 `Avatar Drawer` 在画原版看不见的阵营名

> 写手 `W-11` · 2026-11-01 · 第一百一十四会话 第 1 波
> 白名单内改动 3 处（1 个壳文件 + 1 个新断言件 + 1 处宿主接线），**未跑 Unity**（`typecheck` = 0/0）。

---

## 一、原版判据（逐字现读，不靠名字猜）

### 1. 方法体：`Draw` 只写 `.text`，**一次 `SetActive` 都没调**

`d:/2/tools/cpp2il/out_cpp2il_dev_ilspy/AvatarDrawer.cs:15-24`

```csharp
protected override void Draw(PlayerAvatar item, int quantity = 1)
{
    Image image = base.image;
    Sprite sprite = item.Image;
    image.sprite = sprite;
    if ((bool)armyText)
    {
        string localizedArmyName = ArmyUtilities.GetLocalizedArmyName(item.cardArmy);
        armyText.text = localizedArmyName;
    }
}
```

整个类只有 `Draw` 与 `GetItemName` 两个方法（全文 33 行），**没有**任何 `SetActive` / `enabled =` 之类
的显隐写入。

### 2. prefab：`Content/Army` 这颗 GameObject 是 `m_IsActive: 0`

判据目录 = `d:/2/tools/assetripper_out/ExportedProject/Assets/`（**按 GameObject 块读 `m_Name` + `m_IsActive`**，
⛔ 不是按文件名/节点路径猜）：

| prefab | `Army` GameObject | `m_IsActive` | `ItemDrawerComponents.armyText` 槽 |
|---|---|---|---|
| `Avatar Drawer.prefab` | `!u!1 &1618682377417806` | **0** | **非 null**（`{fileID: 114694121525821480}`，`:534`） |
| `Icon Avatar Drawer Variant.prefab` | `!u!1 &1724804207875695` | **0** | **非 null**（`{fileID: 114313026266067498}`，`:353`） |
| `Avatar Drawer Shop Variant.prefab` | `!u!1 &1618682377417806`（同 id） | **1** | 非 null（`:534`） |
| `Avatar Border Drawer.prefab` | 同结构 | **0** | ——（那一档的 `Draw` 不写阵营名） |
| `Alliance Badge Drawer.prefab` · `Title Drawer.prefab` | `!u!1 &1161607090078389` | **1** | ——（那两档原版**确实**显示阵营名） |

⇒ **结论**：槽非 null、`Draw` 也照样把文本写进去，但**节点在 prefab 里就是关着的** ⇒ 屏幕上**不出现**阵营名。
（这也解释了为什么「`Draw` 里写了 `text`」不等于「画出来」—— 只读方法体会把这条做反。）

---

## 二、我们原来是什么

`Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerAvatar.cs:47`（改动前）

```csharp
// ② `armyText.text = GetLocalizedArmyName(item.cardArmy)`（`Neutral` ⇒ 空串 ⇒ 原版也不写）
DrawerArmyName(content, fr, item.CardArmy, st);
```

`DrawerArmyName`（`Shell/ItemDrawerLayout.cs:146-154`）= `ClippedText(content, r, name, …)` ⇒ **建出一颗激活的
`Content/Army` 文字节点并画出来**。`ItemDrawerStyle.Default` 的 `NamePx = 26 > 0` ⇒ 这条路在生产调用下**必然会画**。

⚠️ 顺带核过：`AvatarBorderDrawer`（`ItemDrawerCosmetic.cs`）**没有**调 `DrawerArmyName`（那一档原版 `m_IsActive` 也是 0，
我们恰好没画，是对的）；`AllianceBadge` / `Title` / `RandomCard` / `Premium` / `Points` 那几档调了，而它们对应 prefab
的 `Army` 都是 `m_IsActive: 1` ⇒ **那几档不用改**。

---

## 三、改成什么

**照原版那三句做（写文本），但建完立刻 `SetActive(false)`** —— 与原版「节点在、`m_IsActive = 0`」结构逐字对应，
而不是「把整层删掉」：

```csharp
DrawerArmyName(content, fr, item.CardArmy, st);
var armyNode = content.Find(NodeArmy);      // `DrawerArmyName` 建的正是 `Content/Army`（直接子件）
if (armyNode != null) armyNode.gameObject.SetActive(false);
```

顺序说明：**先建后关**（`ClippedText` → `MenuDraw.Text` → `Label.Create` 会把 `Army` 直接建在 `content` 下，
且 TMP 的量尺寸要在**激活态**下做 —— 先关再量会拿到垃圾尺寸）。`Neutral`（空串）时 `DrawerArmyName` 本来就不建节点
⇒ `Find` 返回 `null` ⇒ 不多做任何事（与原版「空串也照样写、但节点反正关着」等价）。

---

## 四、改动清单（净增净减）

| 文件 | 改动 | 行数 |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawerAvatar.cs` | ① 文件头补「`Army` 是 `m_IsActive: 0`」那一条；② 把 `DrawerArmyName(...)` 后面加 `Find` + `SetActive(false)`，并把「为什么」连判据一起写进注释 | **+16 / −2（净 +14）** |
| `Unity/MyGame/Assets/CardPresentation/Editor/B114_W11_Assert.cs`（**新建**）+ `.cs.meta` | `B-242` 的断言 11 条（A0/A0b/A1/A1b/A2/A3/A4/A5/A5b/A5c/A5d + A6/A6a/A6b） | 新文件 162 行 · guid `b7f4d2c1e35a4c0e9a6f1d8b2c4e7f31` |
| `Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | 只在那条聚合行**同一物理行**、第一个 `//` **之前**追加接线：`B114_W11_Assert.Run(); _sink.Pass += …; _sink.Fail += …;` | **1 行改 / 净 0 行**（`git diff --numstat` = `1 1`） |

- 行尾：三个文件都是 **纯 LF**（`b.count(b'\r\n') == 0`、裸 CR = 0），没翻行尾。
- `Shell/ItemDrawerAvatar.cs` **不背任何账落点**（`grep -rc ItemDrawerAvatar 账/*.tsv` = 0 命中）⇒ 净 +14 行不会顶漂任何 A 行。
- `typecheck`（`TMPDIR=/tmp/wf_w11 bash d:/4/Unity/工具/typecheck.sh`）= **运行时 0 / 编辑器 0**。

### 断言怎么才能红（灭自证）

| 断言 | 内容 | 怎么红 |
|---|---|---|
| A0 / A0b | 前提：头像图取得到 · `ArmyIcons.LocalizedName(10)` **非空**（实得 Ultramarines 那一串） | 图被删 ⇒ 红；词条丢了 ⇒ 红（否则「不可见」是空转） |
| A1 / A1b / A2 | 前提：根建出来 · 路由到 `AvatarDrawer` · `Content` 建出来 | 路由表改坏 ⇒ 红 |
| **A3** | **前提**：`Army` 节点**建了** | 把整层删掉（连节点都不建）⇒ 红 |
| **A4** | **结论**：`Army.activeSelf == false` | **删掉 `SetActive(false)` 那一句（= 改动前的老行为）⇒ 红** |
| A5 组 | 底板 `Background` / 主图 `Image` / `Label` / `Name` **都还在** | 顺手把别的层关掉 ⇒ 红（锁住「本活只动阵营名这一层」） |
| **A6** | **灭自证**：同夹具 / 同 `cardArmy` / 同 `ItemDrawerStyle`，`TitleDrawer` 那档的 `Army` 竟然**建出来且 `activeSelf == true`** | `DrawerArmyName` 整条弄空 / `NamePx = 0` ⇒ 红 |

A4 与 A6 是**结构上不可能同时满足**的一对：只要有人把 `DrawerArmyName` 的产物重新打开，A4 立刻红；
而 A6 证明「这个版式 + 这个阵营」本来**建得出**激活节点 ⇒ A3/A4 不是坐标系/可见性造成的假绿。
（A6 用 `TitleDrawer` 是有意的：那份 prefab 的 `Army` 是 `m_IsActive: 1`，原版**确实**显示阵营名，
所以拿它当见证不会与「照原版」冲突。）

---

## 五、没查清 / 未接的部分（如实记）

1. 🔴 **`Avatar Drawer Shop Variant` 是反方向的例外，本波没接。**
   那份 prefab 的 `Army` 是 **`m_IsActive: 1`**（现读），且 `armyText` 槽非 null ⇒ **原版在商店那一档会显示阵营名**。
   要照做必须按 `DrawerOverride.Shop` 分流，但 `Avatar(...)` 现在**拿不到 `ov`**：
   - `Dispatch` 收了 `ov`（`ItemDrawer.cs:1916-1918`），但 switch 那一巨行（`:1925`）调
     `Avatar(node, box, item, quantity, style, ref res)` **不传 `ov`**；
   - `IconVariant` 的分流靠 `Dispatch` 开头那句 `ov == DrawerOverride.Icon`（`:1918`）—— 同一个位置才能加 `Shop` 支。
   - 两条都在 `ItemDrawer.cs`（⛔ 本波白名单外，且有 124 行 / 219 处账落点）⇒ **未动**。
   - 现实影响：`ShopData.cs` 目前**没有任何 `PlayerAvatar` 商品**（只有一条 `Avatar Border: Servo` 的 `Cosmetic`），
     `DrawerOverride.Shop` 的 `PlayerAvatar → Avatar Drawer Shop Variant` 只在映射表 / 断言里存在
     （`Editor/ShopScene.cs:326/360/361/4165`）⇒ **潜在差异、生产暂未触发**。建议单列一条账：
     「`ShopAvatar` 分流（几何 + 阵营名显隐）」，判据 = 两份 prefab 的锚点/`m_IsActive` 逐字段 diff。
2. **`Avatar Drawer Shop Variant` 的几何也整体不同**（不止阵营名）：`diff Avatar\ Drawer.prefab` 与
   `Shop Variant` 只在 `Content`/`avatar`/`Army`/`Label` 那几颗的 `m_AnchorMin/Max`、以及两处材质 guid 上分叉；
   我们现在一律按基础档几何画 ⇒ 同一族的两套 prefab 我们只做了一套（与 `B-223` 的 Icon 档是同一形状的欠账）。
3. **同一族两半的实现风格不一致（不是缺陷，登记一句）**：`Icon Avatar Drawer Variant` 那一档由 `B114_WC` 做的是
   **干脆不建 `Army` 节点**（`ItemDrawerPartial_B111R3W.cs` 的 `IconAvatar`），基础档这边是**建了再关**。
   屏幕结果同为「看不见」，但**节点计数口径**不同 ⇒ 将来若有断言按名字数节点，要记得这两档不一样。

---

## 六、顺手发现（不属本活，供调度台分流）

1. `Shell/ItemDrawerLayout.cs` 的 `DrawerArmyName` 注释写「`Neutral` / 认不出 ⇒ 原版那串是空、这一层不画」
   —— 语义对，但没写「**这一档的节点本来就是关着的**」。现在这条知识已经写进
   `ItemDrawerAvatar.cs` 的 ② 注释，`ItemDrawerLayout.cs` 那处**没改**（⛔ 不在白名单）。
2. **两份 prefab 里 `m_IsActive: 0` 的节点全清单**（顺带逐颗点名，免下一个会话再数一遍）：
   - `Avatar Drawer.prefab`：`Army` · `Converted Drawer` · `Ephemeral Drawer` · `Collected Badge` · `Premium Highlight`
   - `Icon Avatar Drawer Variant.prefab`：`Army` · `Converted Drawer` · `Ephemeral Drawer`
   ⇒ 除 `Army` 外那几颗**都是装饰层**（出厂关、由调用方按状态打开），我们这一侧分别由
   `ItemDrawerStyle.Converted/Ephemeral/Premium`（默认 false）与 `B107_W20_Assert`（`N131` `Collected Badge`）覆盖
   ⇒ **无新增缺口**（本波只把 `Army` 这颗补上）。
3. `Shell/ItemDrawer.cs:2453` 的注释写着「`DrawerAvatar`：`Avatar Drawer`（`Avatar Drawer Shop Variant` 同）」——
   `HasQuantitySlot` 那一格「同」得对（两份 `quantity` 都 null），但**显隐层面两份并不同**（`Army` 一关一开）。
   该注释容易让人以为两份可以一套画法 ⇒ 建议后续改那句注释时补一句（本波 ⛔ 不动 `ItemDrawer.cs`）。
