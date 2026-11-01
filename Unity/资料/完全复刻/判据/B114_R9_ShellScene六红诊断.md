# B114_R9 · ShellScene 六红诊断（只读诊断代理 R-9 · 2026-11-01）

> 输入日志 = `d:/4/_tmp_view/b114_unity_shellscene.log`（5,935 通过 / 6 失败）。
> ⛔ 本报告**只诊断、未改任何源码/资产**。

## 一、结论表

| # | 断言（日志行） | 四档判定 | 一句话 | 证据 |
|---|---|---|---|---|
| 1 | 181385 `B110W5 ✗ 结论[旧] UI Border Highlight 编译无 error` | **(γ) 本批回归** | 属性块加了 `[Toggle(_AUTOROTATION)]`，而 CG 里仍声明同名 `float _AUTOROTATION` ⇒ **重定义、整份编不过** | 日志 1003 / 181400 / 181405；`WFUIBorderHighlight.shader:117` vs `:202`；引入者 = `W-7`（报告 `B114_W7_边框高亮shader.md:61`） |
| 2 | 190473 `B114W16 S5b Army 文本` | **(α) 断言写错** | 断言按 **uGUI** 组件读，而这条抽屉是**世界档**、挂的是 `TextMeshPro` ⇒ 恒读到 null | `B114_W16_Assert.cs:123`；`MenuDraw.cs:2052` → `Label.Create`（`ugui:false`）→ `TmpFont.cs:414 NewText` → `TextMeshPro` |
| 3 | 190507 `B114W16 S5d Army 墨色` | **(α) 断言写错**（同 2 的上游） | `tmp == null` ⇒ 兜底值 `RGBA(-1,-1,-1,-1)`，不是墨色错 | `B114_W16_Assert.cs:133`（`tmp != null ? tmp.color : new Color(-1,…)`） |
| 4 | 193492 `B114W32 B5b 主图左沿` | **(α) 断言写错** | 主图是 **PA=1**（原版 `Content/Image.m_PreserveAspect = 1`），实得 = 按高内接后**居中**；期望写成了「拉伸到框沿」 | `ItemDrawerLayout.cs:318`（`keepAspect: true`，**本批未改**）；`Title Drawer Horizontal Variant.prefab` → `Content/Image` `m_PreserveAspect: 1` |
| 5 | 193510 `B114W32 B5c 主图右沿` | **(α) 断言写错**（同 4 的另一半） | 同上；左右两侧偏差**等大反向**（+1.693 / −1.692）⇒ 窄了 3.385 px 且**同心** | 同上（460.856 / 584.300 ⇒ 宽 123.444 = 期望**高** 123.44） |
| 6 | 194997 `B114W38 D5 结构反证` | **(α) 断言函数写错** | 判据式写成 `iconHasBadge != baseHasTrophy`，而这两项**按设计都应为 true** ⇒ 恒 false | `B114_W38_Assert.cs:314`；日志 D2 ✓ / D2b ✓ / D4 ✓ / D4b ✓（194912 / 194929 / 194963 / 194980） |

**六条里 5 条是断言问题（α），只有 #1 是真回归（γ）；没有一条是 (β) 实现缺陷、(δ) 前提不成立。**

---

## 二、逐条详情

### #2 / #3 · `B114W16` 的 `Army` 文本与墨色

**现象**：`实得「<没有 TMP>」`（S5b）、`实得 RGBA(-1,-1,-1,-1)`（S5d），且**上一条 S5c 是绿的**（`Army 字号上限 = 90.00 px`）。

**这是自相矛盾的两条读数，而矛盾点正是判据**：

- S5c 走 `FontPxMax(army)`（`B114_W16_Assert.cs:129` → `:298-302`）：
  `t.GetComponent<Label>()` + `lb.FontSizeToPxNow(lb.FontSizeMax)`。
  它的 doc 写着「返回 **−1** = 这个节点上没有 `Label`（或点阵后端）」 ⇒ **拿到 90 就证明 `Label` 在、TMP 后端也在**。
- S5b/S5d 走 `army.GetComponentInChildren<TMPro.TextMeshProUGUI>(true)`（`B114_W16_Assert.cs:123`）⇒ null。

**生产侧建的是哪一档**（逐跳可核）：

| 跳 | 位置 |
|---|---|
| `DrawerArmyName` | `Shell/ItemDrawerLayout.cs:274-283` → `ClippedText(..., NodeArmy, …)` |
| `ClippedText` | `Shell/ItemDrawer.cs:1422-1442` → `MenuDraw.Text(...)` |
| `MenuDraw.Text` | `Shell/MenuDraw.cs:2052` `Label.Create(parent, …)`（**不是** `CreateUgui`） |
| `Label.Create` | `Battle/Label.cs:160-163` → `CreateCore(..., ugui: false)` |
| `CreateCore` → `BuildTmp` | `Battle/Label.cs:218` / `:1587-1593` → `TmpFont.NewText(...)` |
| `TmpFont.NewText` | `Core/TmpFont.cs:414/421` → 返回 **`TextMeshPro`**（世界空间）；`NewTextUgui` 在 `:465` 才是 `TextMeshProUGUI` |

⇒ 这条抽屉（以及整个 `ItemDrawer` 层）是**世界档**（`ImageQuad` + `MeshRenderer` + 裸 `Transform`，`B114_W16_Assert.cs:116-118` 的注释自己也这么写），
`GetComponentInChildren<TextMeshProUGUI>` **结构上不可能**取到东西。

**该怎么修**：把读口换成与 S5c 同一条（`Label`），或换成世界档组件类型：

```csharp
var lb  = army.GetComponent<Label>();              // Label.cs:45 public Color color · :236 public string Text
Tr(lb != null && lb.Text == armyName, "S5b …（实得「" + (lb != null ? lb.Text : "<没有 Label>") + "」）");
var ink = lb != null ? lb.color : new Color(-1f,-1f,-1f,-1f);
```

（若要保留 TMP 直读，则用 `GetComponentInChildren<TMPro.TextMeshPro>(true)` —— ⛔ 不是 `TextMeshProUGUI`。）

**判据出处**：`ItemDrawer` 全族的文字层都走 `Label.Create`（`MenuDraw.cs:2052`）；同批 `B114_W38` 的 A1c 正是用 `Label` 判同一颗节点（日志 194526「`Army` 那一件上挂的是 `Label`（TMP 后端）」✓）。

---

### #4 / #5 · `B114W32` 主图左/右沿

**读数与算术**（日志 193492 / 193510）：

| 量 | 实得 | 期望 |
|---|---|---|
| 左沿 | 460.856 | 459.163 |
| 右沿 | 584.300 | 585.992 |
| 宽 | **123.444** | 126.829（`217.33 × k`，`k = 0.5837`） |
| 中心 | 522.578 | 522.578（**相同**） |

`123.444` 恰好 = `(353.0 − 141.5) × k = 211.49 × 0.5837` = **期望的高**（B5d/B5e 因此是绿的）。
⇒ 主图被画成 **123.44 × 123.44 的方图、居中**，即 **PA=1 等比内接**（徽记图是方图，框 217.33×211.49）。

**实现侧**：`TitleHorizontal`（`Shell/ItemDrawerPartial_B114W32.cs:109-111`）走
`DrawerImage(content, imgR, tex, …)`，而 `DrawerImage`（`Shell/ItemDrawerLayout.cs:313-321`）第 318 行**恒传 `keepAspect: true`**。
⚠️ **本批没动这一句** —— `git diff ItemDrawerLayout.cs` 里的 `keepAspect` 改动只有 `DrawerBackground` 那颗**形参默认值**
（`:248` `bool keepAspect = true`，即 `B-284`），**`DrawerImage` 的函数体未变**。
⇒ 「`B-284` 改了默认值导致这两条红」的猜测**不成立**（那是底板 `Background` 那条路，不是 `NodeImage`）。

**原版判据（prefab 现读）**：`d:/2/tools/assetripper_out/ExportedProject/Assets/Title Drawer Horizontal Variant.prefab`
（按 `m_GameObject` + `m_Father` 父链还原路径后逐颗读 `!u!114`）：

```
1  Title Drawer Horizontal Variant/Content/highlight
1  Title Drawer Horizontal Variant/Content/Background
1  Title Drawer Horizontal Variant/Content/Image        ← 就是这一颗
1  Title Drawer Horizontal Variant/Content/Image/Image Top
0  Title Drawer Horizontal Variant/Content/Label
1  Title Drawer Horizontal Variant/Content/Lock
1  Title Drawer Horizontal Variant/Premium Highlight/Badge
```
基础档 `Title Drawer.prefab` 同格（`Content/Image`）也是 **1**。
旁证：`报告/B113X10_Icon变体几何15张.md:19` —— 「原版 **15/15 `m_PreserveAspect: 1`**，我们所有主图都走 `keepAspect:true`」。

⇒ **实现是对的，期望值（拉伸到框沿）是错的**。B5b/B5c 用的是「框沿」算式（`ox + HImgX1*k` / `ox + HImgX2*k`，`:222/:224`），
而正确的算式是 **`FitBox(tex, imgR)`**（同一份断言族的 `B114_W16_Assert.cs:145` S6 就是这么写的，`:304-312` 有现成 `FitBox` 实现）。

**该怎么修**（只改断言）：
```csharp
var imgR = new PxRect(ox + HImgX1*k, oy + HImgY1*k, ox + HImgX2*k, oy + HImgY2*k);
RectIs(QuadOf(img), FitBox(ArmyIcons.Texture(10), imgR), 1.5f, "B5b/B5c/B5d/B5e …（PA=1）");
```
⚠️ **B5d/B5e 现在是「碰巧绿」**（高是受限那一维，内接后上下沿正好等于框沿）—— 建议**一起**改成走同一个 `FitBox`，
否则换一张非方图就会只剩两条红、两条仍绿，判读会误导。

---

### #6 · `B114W38` D5 结构反证

代码（`Editor/B114_W38_Assert.cs:313-317`）：

```csharp
Tr(iconHasBadge != baseHasTrophy,
   "D5 ★★ 结构反证：Icon 档 = … / 基础档 = … ⇒ **两档的节点名不是同一个**｜🧨 两档读到同一个名字 ⇒ 红");
```

而 `iconHasBadge` = 「Icon 档根下**有** `Badge`」、`baseHasTrophy` = 「基础档根下**有** `Trophy Icon`」。
**这两项按设计都必须为 true**（否则 D2 / D4 先红）。日志证实：

| 断言 | 结果 | 日志行 |
|---|---|---|
| D2 Icon 档有 `Badge` | ✓ | 194912 |
| D2b Icon 档无 `Trophy Icon` | ✓ | 194929 |
| D4 基础档有 `Trophy Icon` | ✓ | 194963 |
| D4b 基础档无 `Badge` | ✓ | 194980 |
| **D5** | **✗** | 194997 |

`true != true` 恒为 `false` ⇒ **只要 D2 与 D4 是绿的，D5 就必然红**。这不是「改名被做成了一刀切」，
而是判据式把「**都成立**」写成了「**互斥**」。

**该怎么修**（只改断言）：把互斥写成**交叉缺失**那两半（这才是「不是同一个通则」的结构证据）：

```csharp
Tr(!iconHasTrophy && !baseHasBadge,
   "D5 ★★ 结构反证：Icon 档**没有** Trophy Icon、基础档**没有** Badge ⇒ 两档各自一套名字");
```
（D2/D2b/D4/D4b 保留不动；它们已经分别咬住「该有的有」与「不该有的没有」。）

---

## 三、#1 shader 编译失败专项

### 报错原文

日志 `:1003`（首次导入 AssetDatabase 时）与 `:181400 / :181405`（B110W5 跑的时候再次打印），三处同一句：

```
Shader error in 'WarpforgeVFX/UI/UI Border Highlight': redefinition of '_AUTOROTATION'
at Assets/WarpforgeVFX/Shaders/WFUIBorderHighlight.shader(202) (on d3d11)
```

上下文（日志 1004-1006）：

```
Compiling Subshader: 0, Pass: UI_BORDER_HIGHLIGHT, Vertex program with <no keywords>
Platform defines: SHADER_API_DESKTOP UNITY_COLORSPACE_GAMMA … UNITY_UI_CLIP_RECT UNITY_UI_ALPHACLIP
Disabled keywords: … UNITY_UI_ALPHACLIP UNITY_UI_CLIP_RECT … _AUTOROTATION _DEBUG _FLIPDIRECTION
```

### 定位到具体行

`WFUIBorderHighlight.shader:202`：

```hlsl
float _Rotation, _AUTOROTATION, _AutoRotationSpeed, _FLIPDIRECTION, _DEBUG, _QueueOffset, _QueueControl;
```

与属性块 `:117`：

```hlsl
[Toggle(_AUTOROTATION)] _AUTOROTATION ("AutoRotation", Float) = 0
```

**同名关键字 + 同名变量** ⇒ 关键字被打开时 Unity 会 `#define _AUTOROTATION`，与这句声明**重定义**、
**整份 shader 编不过**。

**本项目已有两条**同族**先例，错误文本逐字相同**：

| 位置 | 原话 |
|---|---|
| `WarpforgeVFX/Shaders/WFEclipseTau.shader:96-101` | 「⚠️ **这里【不能】声明 `float _APPLYAMBIENTCOLOR;`** —— 它是 `[Toggle(_APPLYAMBIENTCOLOR)]` 的关键字名，又出现在 `#pragma multi_compile_local` 里；材质一开 Unity 就 `#define … 1` ⇒ 与同名变量**重定义、整份编不过**。实测：`Shader error … redefinition of '_APPLYAMBIENTCOLOR'`」 |
| `WarpforgeVFX/Shaders/WFParticlePremultiplyGreyscale.shader:109-111` | 「⚠️ **同样不能声明 `float _SOFTPARTICLES;`** —— 它是 `[Toggle(_SOFTPARTICLES)]` 的关键字名」 |

### 是 `W-13` 还是 `W-36` 引入？—— **都不是，是 `W-7`**

- `W-13`（`报告/B114_W13_边框shader其余5处.md`）只做了 §② `shader_feature_local` **核对**（结论「与原版一致、不改」）、
  `Cull Back`、删 `_RendererColor`。
- `W-36`（`报告/B114_W36_clip软边与对象空间.md:86`）明确写「`_DEBUG` / `_AUTOROTATION` / `_FLIPDIRECTION` 三条 `shader_feature_local` **原样**」——
  它只加了 `_ClipRect` / `_UIMaskSoftness*` 与裁切算式。
- **`W-7`（`报告/B114_W7_边框高亮shader.md:61`）** 的差异表里写着：
  「`_DEBUG` / `_AUTOROTATION` / `_FLIPDIRECTION` ｜ 原版：带 `[Toggle(关键字)]`；默认 1/0/0 ｜ 我们：**无 `Toggle`**、默认 0/0/0 ｜ **照抄**（`:58/61/67`）」
  ⇒ 就是这一波把三颗 `[Toggle(...)]` 加进属性块，而 `:202` 那句 `float … _AUTOROTATION …` 是**更早就存在的**（HEAD 版本已有同一句）。
  `git diff` 也证实：HEAD 的 Properties **完全没有** `[Toggle]`，工作树才有。

**为什么只报 `_AUTOROTATION` 一个**：`:202` 那一行里，`_AUTOROTATION` 是**第一个**同名标识符
（顺序 `_Rotation`(无冲突) → `_AUTOROTATION` → `_AutoRotationSpeed` → `_FLIPDIRECTION` → `_DEBUG`），
Unity 每份 pass 只报第一条错。**`_FLIPDIRECTION` / `_DEBUG` 有同样的病，只是没轮到它们报**。

### 该怎么修

把这三颗从 `:202` 的变量声明里删掉（代码里**只**用 `#if defined(...)`，**从不读它们的值**：

| 用法 | 行 |
|---|---|
| `#if defined(_DEBUG)` | `:242` |
| `#if defined(_AUTOROTATION)` | `:250` |
| `#if defined(_FLIPDIRECTION)` | `:288` |

```hlsl
float _Rotation, _AutoRotationSpeed, _QueueOffset, _QueueControl;   // ← 去掉 _AUTOROTATION / _FLIPDIRECTION / _DEBUG
```
Properties 里那三条 `[Toggle(...)]` **保留**（那是原版属性表的一部分）。这就是 `WFEclipseTau.shader:101-102` 的「同族做法」。

⚠️ **顺手提醒**：B110W5 同一组里 `isSupported == true`（181370）与 `SetPass(0) == true`（181426）都是**绿**的 ——
**不要**因为那两条绿就认为这颗 shader 没事；`ShaderHasError` 才是这条的真判据，它红就是真编不过。

---

## 四、哪些是「本批回归」

| 写手 | 报告 | 引入的东西 | 对应红 |
|---|---|---|---|
| **`W-7`** | `资料/完全复刻/报告/B114_W7_边框高亮shader.md`（`:61` 那一行） | 三颗 `[Toggle(关键字)]` 加进 `WFUIBorderHighlight.shader` 属性块，而 `:202` 的同名 `float` 声明没删 | **#1** |

其余五条**不是生产回归**：`B114_W16`（#2/#3）、`B114_W32`（#4/#5）、`B114_W38`（#6）的红都是
**本批新写的断言自己算错/读错**（α），生产代码在这三处**与判据一致**。

**时间线旁证**：`grep -rl "redefinition of '_AUTOROTATION'" d:/4/_tmp_view` 全树只命中
`b114_unity_shellscene.log` 一份（批内另一份 `b114_unity_battlescene.log` 没有）；
shader 文件 mtime = `2026-11-01 17:57:32`，本轮日志 mtime = `19:03:58` ⇒ 先改后跑，与「本批引入」一致。

---

## 五、没查清的部分

1. **`#define _AUTOROTATION` 到底是哪条路给出来的**：日志那段上下文写的是
   `Vertex program with <no keywords>`、并把 `_AUTOROTATION` 列进「Disabled keywords」——
   按 `WFEclipseTau` 的说法，宏只在「材质把关键字打开」时出现；而 `W-7` 报告 `:147` 又说「`_AUTOROTATION` / `_FLIPDIRECTION` / `_DEBUG` 目前**没有任何材质在开**」。
   - 已排除：`D:/Unity/Hub/Editor/6000.3.23f1/Editor/Data/CGIncludes/` 全目录 `grep -rl "_AUTOROTATION"` = **0 命中**。
   - **结论**：宏的**来源机制未查实**（可能出自 Unity 对 `[Toggle]` 属性 + `shader_feature_local` 同名组合的内部处理），
     但**故障点与修法是确定的**（两条同族先例 + 逐字相同的错误文本）。
     ⛔ 修完务必复跑 B110W5；若还红，说明不止这一处，再回来看这条。
2. **修掉 `_AUTOROTATION` 后 `_DEBUG` / `_FLIPDIRECTION` 会不会接着报**：按 `:202` 的声明顺序与「每 pass 只报首条」的推断**会**，
   但没有第二份日志可证（本轮日志只报了一条）。
3. **#2 的文本本身对不对**：我只证明了「读口用错组件」，**没有**在运行时读到 `Army` 的真实文本
   （本轮只读、不跑 Unity）。证据链上判「文本是对的」的依据是 `B114_W38` A1c（同批日志 194526 ✓：该节点挂的是 `Label`）。
4. **#4/#5 的 `m_PreserveAspect = 1` 取自 AssetRipper 导出工程**（`Title Drawer Horizontal Variant.prefab`，且基础档 `Title Drawer.prefab` 同格也是 1）。
   ⚠️ `B114_W32` 自己的报告 `:42-43` 提醒：**这份导出的 TMP override 没合进 variant**（`m_fontSize 90` vs 真包 `120`）。
   `m_PreserveAspect` 在 Image 组件上、且 variant 与基础档**两边都是 1**，我判它可信；但**没有再打开真包 `menu_dump` 逐格复核这一格**。
   （真包侧我只确认了 `bundle_menus_assets_all/GameObject/` 下确实有 `Title Drawer Horizontal Variant*.json` 一族，未逐 pathID 解到 Image 组件。）

---

## 六、顺手发现（⛔ 未动手改）

1. **`B114_W32` 的 B5d/B5e 是「碰巧绿」**：它们的期望（上下沿 = 框沿）在「高是受限那一维」时与 PA=1 的实画结果**同值**。
   一旦换非方图 / 换比例，这两条会给出**误导性的绿**。建议与 B5b/B5c 一起改成走 `FitBox`（见 §二 #4/#5）。
2. **`B114_W16` 的 S5c 与 S5b/S5d 用了两套读口**（`Label` vs `TextMeshProUGUI`）——这是同一段里唯一的不一致，
   很可能就是抄了别的宿主（那些宿主跑在 uGUI 画布下）留下的。**同族写法**值得顺手全仓扫一遍：
   `grep -rn "GetComponentInChildren<TMPro.TextMeshProUGUI>" Assets/CardPresentation/Editor/`，
   凡是断言对象在 `ItemDrawer` / 世界档下的，都是同一个坑。
   （⛔ 本轮只报不改；未统计全仓命中数。）
3. **`B114_W38` 的 D5 形态**（`A != B` 当「结构反证」）在同批其它断言里也可能出现：这种写法只在
   「A、B 恰好互斥」时成立，而「两档各自都有自己那份」恰恰**不互斥**。建议同族审查一次。
4. **`WFUIBorderHighlight.shader` 的 `_DEBUG` 属性默认值是 1**（`:108`，照原版 `m_DefValue`）。
   按 `WFMatcap.shader:66-77` 记过的教训，`[Toggle]` 会让 Unity **在建材质时按属性默认值同步关键字**
   ⇒ 任何**新建**的材质都会把 `_DEBUG` 打开（走 VS-B、极坐标原点取网格 uv 中心）。
   现网那份 `.mat` 显式写全了这几个值，所以今天不出问题；但「新建材质」这条路的行为**取决于属性默认值**，
   将来加材质时容易踩。⛔ 本报告不建议改（那是照原版抄的），只是提请注意。
