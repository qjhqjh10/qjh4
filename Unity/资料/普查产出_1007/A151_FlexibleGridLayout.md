# A151 · `FlexibleGridLayout` —— 「有子件的实例」全库巡库（波 1 · 零自检）

> 本件只有两个动作：**① 巡库找实例** ② **有能验的真数据才实现**。
> 结论是 **①找到 5 个、但没有一个能验布局体** ⇒ **②没实现**（一个字节都没改）。
> 判据文档：`资料/待办判据_1006.md` §A151 · 上一轮读全算法的报告 `资料/普查产出_1006/A150_A151_A60尾巴.md` §六。

---

## 一、结论

1. **全库（90 个目录 / 84 个 bundle / 246,680 个文件）共 5 个 `FlexibleGridLayout` 实例** ——
   此前只记了 `bundle_menus_assets_all` 里的 **3** 个；**另外 2 个在场景包里，是本件新查出来的**（见 §二）。
2. 🔴 **5 个实例的「直接 `RectTransform` 子件数」全是 0** ⇒ **布局体一次都不执行** ⇒ 按任务书
   「找不到」那一支收口：**不实现**，`工具/menu_dump.py` / `工具/menu_rect.py` **一个字节都没改**。
   现状仍落 `cust?`（**没排 + 出声**，不是静默错）。
   📌 **这一格是本件唯一需要调度台裁的地方**：按任务书括注的字面（「**子件数 ≠ 0**」），
   有 **1** 个实例命中（leviathan `Scenario`，`m_Children` 数 = 4）；
   但按 uGUI 的 `m_RectChildren` 口径它**等价于 0**（§一·3）—— **本件按后者收口**，
   理由是「拿它当验收」验的是**同一个恒等式的两边**（工具早就不排那 4 个子件 ⇒ 实现与否读数都一样 = 自证）。
   若调度台要按字面口径办，请裁；**能改判的材料全在本报告里**（实例清单 §2·3 + 空档锚 §4·1）。
3. 🔴 **「有 4 个子件」那一个是【不算的】**（本件最容易踩的一格）：`bundle_scenes_scenes_battlearenaleviathan`
   的 `Scenario` 有 4 个子件，但**全是纯 `Transform`**（3D：`Directional Light` / `Sun flare` /
   `Particle Effects` / `Battle Arena Leviathan Baked`）。
   判据 = uGUI `LayoutGroup.CalculateLayoutInputHorizontal`：**`var rect = rectTransform.GetChild(i) as RectTransform;`
   `if (rect == null || !rect.gameObject.activeInHierarchy) continue;`**
   （`LayoutGroup.cs:58-59`，本地官方源码 `MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/…/LayoutGroup.cs`）
   —— 纯 `Transform` 转型失败 ⇒ **`m_RectChildren` 为空** ⇒ `FindSpaceOnGrid`/`DoesItemFit`/`MarkCells`
   **一次都不调用**。⚠️ 这条与本工具既有行为**一致**（`menu_dump.walk` 本来就是
   `kids=[b.rt.get(c) for c in children]; kids=[k for k in kids if k]` ⇒ 纯 Transform 子件落 `stats['t_kids']`、
   不进表），实测那一个节点印的是「⚠️ 另有 **4** 个纯 `Transform` 子件……**不在本表里**」。
4. 🟢 **带回来一个真数据锚**（本件唯一的可用读数，留给将来真要做的人）：尾段回写
   `sizeDelta.x = padding.left + |(spacing.x + itemSize.x)·usedCols − spacing.x| + padding.right`
   在 **`usedCols = 0`（无占用格）** 时化简成 **`padding.left + spacing.x + padding.right`**，
   **5/5 实例与序列化 `sizeDelta.x` 逐位相等**（103 / 103 / 103 / 74 / 0，见 §四）。
   ⚠️ **它只覆盖尾段、且只覆盖空档**；**不覆盖** `FindSpaceOnGrid`/`DoesItemFit`/`MarkCells`/落位算式。

---

## 二、搜证过程（搜过哪些包、哪些词、命中什么）

### 2·1 三步定位法（`m_Script` 是跨文件 PPtr，直接用类名 grep **找不到实例**）

| 步 | 词 | 范围 | 命中 |
|---|---|---|---|
| ① 找 MonoScript | 字面量 `FlexibleGridLayout` | `assets_full/**`（246,680 文件） | **4** 个文件：`bundle_Waprforge_monoscripts/MonoScript/MonoScript_7184420427299685613.json`（`m_ClassName: FlexibleGridLayout`）· `globalgamemanagers/MonoScript/MonoScript_4449.json`（同一件在内置文件里的第二份）· 另 2 个是**另一个类** `AdaptFlexibleGridLayoutByAspectRatio`（名字里含子串，**不是**布局组） |
| ② 找引用 | `"m_PathID": 7184420427299685613`（+ 补搜 `4449`） | 同上 | **8** 个文件 = **5 个 MonoBehaviour 实例** + **3 个容器**（`AssetBundle_1.json` ×1 · `PreloadData_1.json` ×2，只是把脚本列进包清单）⇒ 认实例时剔掉容器；另 `4449` 的 **27 个命中全是假的**（见下） |
| ③ 子件数 | 逐实例解 `GameObject → RectTransform.m_Children` | 解包目录 + **真包**（UnityPy 复核） | 见 §2·3 表 |

⚠️ **`4449` 那 27 个命中全是假命中**（A152 的 pid 陷阱的形状）：`pid` 是**分包局部**的，
27 个文件里 `m_Script.m_PathID` **没有一个**等于 `4449`（那些是各自文件里别的 PPtr）。
⇒ 本件**没有**通过 `globalgamemanagers` 引用那一份 MonoScript 的实例。

### 2·2 「否定」有效吗 —— 量化的完整性验证（本件为「找不到」单独做的）

「按字面量扫 JSON」只有在**导出完整**时才算有效否定。实测：**84 个真包逐包**比对
「UnityPy 读出的对象数」vs「`assets_full` 目录里的文件数」（`d:/4/_tmp_view/a151_cov.py`，只读）：

- 合计 **real = 239,921 · export = 239,898**（差 **23**），**不等的包 = 6 个**；
- 那 23 个缺口**逐类型查清**：`Mesh` 15（battleprefabs 9 / battlesharedresources 6）·
  `Texture2D` 9（cosmeticavatars 2 / fonts 1 / necronssautekh 4）· `GameObject` 1（mainmenuwarpforge）。
- 🔴 **`MonoBehaviour` 的缺口 = 0**（每一个包都逐位相等，含 82,038 的 `menus_assets_all` 与 5,300+ 的 13 个场景包）
  ⇒ **没有任何一个 MonoBehaviour 被漏导或撞号覆盖** ⇒ 「全库只有 5 个 `FlexibleGridLayout`」是**有效否定**。
- ⚠️ 同时**排掉一个洞**：`dump.cs` 里**没有** `class X : FlexibleGridLayout`（全库只有
  `public class FlexibleGridLayout : LayoutGroup` 这一处声明，`dump.cs:104785`）⇒ **不存在**「子类的
  MonoScript 名字不同、因而 grep 不到」的漏网。
- 搜过的**全部 6 个非 bundle 目录**：`globalgamemanagers` · `level0` · `level1` · `resources` ·
  `sharedassets0` · `sharedassets1`（+ 84 个 `bundle_*`）。

### 2·3 五个实例（**这 5 行就是本件的全部实例**）

| # | 包 | GO（pid / 名） | 父链 | rows×columns · itemSize · spacing · pad(L/R) · align | **直接 RT 子件** | 纯 `Transform` 子件 |
|---|---|---|---|---|---|---|
| 1 | `bundle_menus_assets_all` | `-2927872650588077833` / `Content`（RT `-6154209878910385929`） | Viewport ← Packs Scroll View ← **Packs Tab** | 2×200 · 372.9×427.9 · 28/32 · 50/25 · 0 | **0** | 0 |
| 2 | 同上 | `-6537416839932815225` / `Content`（RT `-1738091081981175673`） | Viewport ← Packs Scroll View ← **Gold Tab** | 同 #1 | **0** | 0 |
| 3 | 同上 | `-8288250319848926468` / `Content`（RT `-2395934425507884292`） | Viewport ← Packs Scroll View ← **Generic Shop Tab** | 同 #1 | **0** | 0 |
| 4 | `bundle_scenes_scenes_mainmenuwarpforge` | `28` / `Content`（RT `1107`） | Viewport ← GameModes ← Safe area All ← MainMenu ← Main Canvas | 2×20 · 535×414.4 · 20/20 · 38/16 · **3 = MiddleLeft** | **0** | 0 |
| 5 | `bundle_scenes_scenes_battlearenaleviathan` | `4` / `Scenario`（RT `2747`） | （根，无父） | 2×10 · 100×100 · 0/0 · 0/0 · 0 | **0** | **4** ⚠️ 全部是纯 `Transform`（见 §一·3） |

- 复核口径：**解包目录 + 真包各读一遍**。真包那一遍用 UnityPy 走**主 CAB**（场景包是双 CAB 包，
  `pid` 会撞号 —— 例：leviathan 的 `pid 4` 在主 CAB 是 `GameObject Scenario`、
  在 `.sharedAssets` 里是 `Material Glow Rays Ring Add`；**按主 CAB 取才对**，A152 记的就是这个坑）。
- `mainmenuwarpforge` 的 `Content` 用真包复核过：主 CAB 里 `pid 28` = `GameObject Content`，
  它的 RT `1107` 的 `m_Children` = **`[]`**（不是导出丢了子件）。

---

## 三、做了什么（改动清单）

**空。** `工具/menu_dump.py` · `工具/menu_rect.py` **一行都没改**（也没改任何别的文件）：

```text
$ git diff --numstat -- Unity/工具/menu_dump.py Unity/工具/menu_rect.py
2363    97      Unity/工具/menu_dump.py      ← 与本件开工前**逐位相同**（是上一批遗留的未提交改动）
123     17      Unity/工具/menu_rect.py
```

只做了**只读**的事：巡库（`rg` / UnityPy）· 读反编译（`d:/2/tools/decomp_full/`）·
读 uGUI 官方源码（本地 PackageCache）· 读两个工具（**没改**）· 跑工具自检当基线。
临时探针写在 **`d:/4/_tmp_view/`**（`.gitignore:139` 已忽略，不进 git）：`a151_cov.py`（完整性比对）·
`a151_lit.py`（读常量）· `a151_probe3.py`（解 leviathan `Scenario` 子树）。

---

## 四、验收读数（真数据）

### 4·1 尾段回写 vs 序列化值（**5/5 逐位相等**）

| 实例 | `pad.left` | `spacing.x` | `pad.right` | 算式（`usedCols=0`）= `L+s+R` | 序列化 `sizeDelta.x` | 工具印的宽 |
|---|---|---|---|---|---|---|
| #1 Packs Tab `Content` | 50 | 28 | 25 | **103** | **103.0** | `103.00` ✔ |
| #2 Gold Tab `Content` | 50 | 28 | 25 | **103** | **103.0** | — |
| #3 Generic Shop Tab `Content` | 50 | 28 | 25 | **103** | **103.0** | — |
| #4 MainMenu `Content` | 38 | 20 | 16 | **74** | **74.0** | — |
| #5 Leviathan `Scenario` | 0 | 0 | 0 | **0** | **0.0** | `0.00` ✔ |

🔴 **这条读数怎么用、不能怎么用**（免得下一个人当成了「已验」）：
- 它是**一致性**证据：算式在**空档**上的输出 = 序列化值；`#1/#4` 这两个非平凡值
  （50+28+25、38+20+16）**不是巧合能解释的**（`padding.left+padding.right` 那种错读法会得 75/54）。
- 它**不是**「组件跑过」的证明：序列化值本身既可能是设计者手填的、也可能是 `LayoutGroup` 带了
  `[ExecuteAlways]`（`LayoutGroup.cs:9`）后在编辑器里被写回的 —— **本工具分不出来**，
  所以**只能当「没有反证」**，⛔ **不能当「布局体已验」**（布局体在 5 个实例上都是 0 次执行）。

### 4·2 工具现状（基线，未改动）

- `python 工具/menu_dump.py --verify-layout` ⇒ **EXIT = 0 · `❌` 0 条**（`✅` 共 **48** 行 = 47 条 fixture
  + `:23` 那条分区标题；任务书里写的「47 ✅」就是**只数 fixture**的口径）—— **未改动 ⇒ 与开工前逐行相同**。
- `python 工具/menu_dump.py bundle_scenes_scenes_battlearenaleviathan --rt 2747 --depth 2 --no-sprite`
  ⇒ 印 `0 Scenario … 0.00 100.00  FlexibleGridLayout,ScenarioMaterialFader …`
  \+ 「⚠️ 另有 **4** 个纯 `Transform` 子件……**不在本表里**」（= 与本件结论一致）。
- `python 工具/menu_dump.py bundle_menus_assets_all --rt -6154209878910385929 --depth 3 --no-sprite`
  ⇒ 印 `0 Content … 103.00 1060.00  FlexibleGridLayout …`（宽度就是 §4·1 那个 103）。

---

## 五、没查清的部分（「还差什么」）

1. 🔴 **布局体（`FindSpaceOnGrid` / `DoesItemFit` / `MarkCells` / 落位算式 / `usedCols`）在真数据上
   零覆盖** —— 差一个「**直接 `RectTransform` 子件数 > 0**」的实例。全库没有（§二已量化证明），
   能再变出实例的路只剩两条，**都不在本件（也都不在本波）**：
   - **真 Play 时 dump 一次**运行期实例化之后的 `Content`（原版已关服、货架数据在远端
     ⇒ 就算跑起来也**大概率是空的**，与「手牌注入 `inj FAIL: empty Data`」同一族的墙；真要试就是一次真 Play）；
   - 让运行期实例化用的那个 prefab（`ShopTab.flexibleGridLayoutContent` 指的那件 / 商城货架条目）
     **静态化**进场景 —— 判据在**远端数据**（Title Data / CCD），本地没有。
2. 🔴 **同一件的「状态 → 参数」**：`#4` 的 `Content` 上还挂着
   **`AdaptFlexibleGridLayoutByAspectRatio`**（`dump.cs:104727`，字段 `flexibleGridLayout` @0x20）——
   它在 `OnEnable`/分辨率变化时**改写 grid 的 `itemSize`**：
   `itemSize.x = this.baseX(0x28)`；`itemSize.y = this.baseY(0x2c) × max(1.0, 1.7777778 ÷ (Screen.width/Screen.height))`
   （`AdaptFlexibleGridLayoutByAspectRatio__ModifyFlexibleLayout.c`；常量见 §6·4）。
   ⇒ **序列化的 `itemSize` 不是运行期值**（16:9 之外会变）⇒ 将来真要做这一档，**先得裁「取哪一帧/哪一档」**，
   ⛔ 不能直接拿序列化字段顶替（铁律 5·c）。另有 `DAT_18453b740` 那个「先取序列化值当基准」的懒初始化。
3. **`usedCols` 的非空档**（占用格 > 0）没有真数据 —— 它才是 `sizeDelta.x` 的真正行为；空档只是化简形。

---

## 六、顺手发现（**只报不改**）

1. 🔴 **上一条记录里「全库只有 3 个实例」的口径偏小**：真值是 **5 个**（多出的 2 个在
   `scenes_scenes_mainmenuwarpforge` 与 `scenes_scenes_battlearenaleviathan`，
   **都不在 `bundle_menus_assets_all`**）⇒ `A150_A151_A60尾巴.md` 与 fixture ⑱b 的「本包 3/3/1」
   **本身没错**（那是「本包」口径），但**别再把它当成「全库」**。
2. 🔴 **`Scenario` 上还挂着 `ScenarioMaterialFader`**（`dump.cs` 那族字段：`fadeOnEnable` /
   `isInDefaultScenario` / `renderers` / `disableOnFadeOut`）⇒ 它是战场**材质淡入**的根。
   ⚠️ 那个 `FlexibleGridLayout` 的 **6 个序列化字段全在 ctor 默认值上**（`rows=2` · `columns=10` ·
   `itemSize=100×100` · `spacing=0` · `m_Padding=0` · `m_ChildAlignment=0`）——「**没人调过它**」是**描述**，
   ⛔ 不是判「原版没用它」：只能说明**它此刻改不了任何东西**（4 个子件也都不参与布局）。
3. **`FlexibleLayoutSizeOption` 全库 67 个，都在 `bundle_menus_assets_all`**（40 个 1×2 · 27 个 1×1），
   **没有一个是上述 5 个 grid 的直接子件**：51 个的 `m_Father` pid = 0（各自 prefab 的根、没有静态父）、
   16 个挂在别的容器下（`Content` ×6 / `Special Missions` ×4 / `Weekly Mission Holder` ×2 /
   `Gold Tab` 1 / `Generic Shop Tab` 1 / `Expansion Pass Missions Tab` 1 / `Packs Tab` 1）——
   那 6 个 `Content` **不是**有 grid 的那三个（父链分别通到 `Item Shop Tab` / `Daily Shop Tab` /
   `Card Shop VIP Tab Variant` / `Item Shop Tab No Automatic Ordering` / `Shop Tab/…/Shop Menu Variant`；
   逐个解过父链、**没有一个穿过那 3 个 grid 的 RT pid**）。
   旁证：`ShopTab` 上**两个**序列化字段并存 —— `gridLayoutContent`(`dump.cs:91674`, `GridLayoutGroup`) 与
   `flexibleGridLayoutContent`(`dump.cs:91676`, `FlexibleGridLayout`)，类在 `dump.cs:91668`
   ⇒ 商城的 tab **分两型**，只有 3 个走 FlexibleGridLayout。
4. 🟢 **`DAT_1834b32f8` 的「按 8 字节读才是 `double 0.0001`」得到二次独立确认**：
   同一个常量在**另一个方法体**里也出现（`AdaptFlexibleGridLayoutByAspectRatio.ModifyFlexibleLayout`
   的「尺寸差 > 阈值才 `MarkLayoutForRebuild`」守卫），按 8 字节读 = **0.0001**、按 4 字节读 = `-1.889e26`（垃圾）。
   顺带实读到的常量（`d:/4/_tmp_view/a151_lit.py`，RVA→节表→偏移，只读 `GameAssembly.dll`）：
   `0x1834b32d0 = float 1.7777778`（16:9）· `0x1834b2bb8 = float 1.0` ·
   `0x1834b2ba8 = float 1e-10` · `0x1834b32f8 = double 0.0001` ·
   `0x1834b2bb4 = float 0.5`（`MiddleLeft` 的 ×0.5）· `0x1834b2e60 = 0x7FFFFFFF`（绝对值掩码）。
   ⛔ 本件**没改** `工具/read_literal.py`。
5. **解包目录的 6 个包有小缺口**（§2·2）：`Mesh` 15 · `Texture2D` 9 · `GameObject` 1 件没导出。
   **与 `MonoBehaviour` 无关**（MB 零缺口），但 `GameObject` 那 1 件在
   `scenes_scenes_mainmenuwarpforge`（恰是 A152 记的双 CAB / 128 个 GameObject 撞号的那一个包）
   ⇒ **值得另立一条查**（本件只报不改、也没查它是什么）。
6. **`DoesItemFit` 的「怪形状」可以放心照「任一出界即不合适」实现**：反汇编里 `bVar3` 在**界内那一支被重置**
   （看着像「只有最后一格算数」），但**两种读法在本算法里逐点同值** —— 出界只可能发生在
   `i = sizeX−1` 或 `j = sizeY−1`（更大的 `i`/`j` 才越界），而循环**最后检查的那一格恰好就是
   `(sizeX−1, sizeY−1)`** ⇒ 「最后一格出界」⟺「存在出界的格」。⚠️ 另有一个**没验过**的边角：
   `sizeX`/`sizeY` **为 0** 时循环不执行 ⇒ `DoesItemFit` 返回 **true**（任何位置都「放得下」）；
   本库 67 个 option 里**没有 0**（40 个 1×2、27 个 1×1），**没真数据可验**。

---

## 七、跑了什么

| 跑了什么 | 结果 |
|---|---|
| `rg` 全库字面量扫描（①类名 ②`m_PathID` 两种） | 见 §2·1（都是**只读**） |
| `d:/4/_tmp_view/a151_cov.py`（UnityPy 84 包逐包对象数 vs 解包目录文件数） | 239,921 / 239,898 · 不等的 6 包 · **MB 零缺口** |
| UnityPy 复核 5 个实例的子件（含**主 CAB** 口径） | 5×0 个 RT 子件；leviathan 4 个纯 `Transform` |
| `python 工具/menu_dump.py --verify-layout` | **EXIT = 0 · `❌` 0 条 · `✅` 48 行**（基线，未改动） |
| `python 工具/menu_dump.py … --rt …`（3 处） | 见 §4·2（Scenario 4 个纯 Transform / Content 宽 103.00） |
| `python d:/4/_tmp_view/a151_lit.py`（读 `GameAssembly.dll` 常量） | 见 §6·4 |
| **没跑**：Unity 批处理 · 自检 · 任何写盘生成器 · `git` 写操作 · `/d/2` 任何写操作 | 铁律 12（纯工具/纯文档零自检）· 本件红线 |

**一句话交接**：**「有参与布局的子件」的实例在本地不存在**（5 个实例的 RT 子件数全 0，已量化证明）；
A151 保持**未实现**（`cust?` = 没排 + 出声）；将来要动手，**先解决「怎么拿到一个 RT 子件数 > 0 的实例」**，
本件带回的 **尾段空档真数据锚（5/5）** 只能当**一部分**验收、**不能**当布局体的验收。
