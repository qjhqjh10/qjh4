# W58 · 界面线 · `MenuDraw.FindOwnAbsorbPoint` 吸收点「设计帧 → 指针帧」修正

> 执行代理 **W58-ABSORB** · 2026-10-20 · 第五十八会话
> 判据来源：`资料/普查产出_1020/W58_自检线_设计帧到指针帧口.md` §五「其它宿主同类」
> 口径：**只做不验** —— ⛔ 没跑 Unity / `*.Run` / 截图；✅ 只跑了秒级 typecheck。
> 白名单内**只改** `Shell/MenuDraw.cs`、`Editor/MenuCheck.cs`（转调收口）、`Editor/ShellScene.cs`（本件断言）；
> ⛔ 没动 git / 正本 / 其它宿主。

---

## 结论

1. ✅ **修掉潜伏缺陷**：`FindOwnAbsorbPoint` 的候选点活在设计帧，而 `at`（生产路径 = `PointerLayer.ButtonAt`）
   吃指针帧 ⇒ 现在每个候选先过**唯一换算口** `MenuDraw.PointerFramePxFromDesign`，产出的 `px/py`
   也是指针帧（直接喂 `ClickAt`）。`k == 1` 快路径**逐位返回入参** ⇒ 今天约 32 扇窗的读数一字不变。
2. ✅ **唯一实现下沉到运行期件**：`MenuDraw.PointerFramePxFromDesign`（`Shell/MenuDraw.cs:114`）；
   `MenuCheck.PointerFramePxFromDesign`（`Editor/MenuCheck.cs:220`）改为**一行转调**，原 `ScaleAboutPx`
   副本已删（两份公式不再存在）。
3. ✅ **同批修掉同族守卫**：候选扫描里那条「在屏幕里」的约束也挪到**指针帧**上判
   （屏幕真的那一帧；`k == 1` 时逐位不变）。
4. ✅ 新增 **13 条断言**（含 3 条前提）的 ×2 夹具探针（`Editor/ShellScene.cs:3373`）：
   单位缩放逐位不变 · 换算值 = 画布中心 + 偏移×2 · 与 `PointerFramePx(quad)` 独立互证 ·
   `ButtonAt` 命中 · **灭自证**（设计点直接喂 `ButtonAt` 命不中）。
5. ⚠️ 断言**没真跑**（铁律 12：只做不验）—— 运行时通过与否未实测。

---

## 一、唯一实现放哪（理由）

| 位置 | 角色 | 为什么 |
|---|---|---|
| **`Shell/MenuDraw.cs:114` `PointerFramePxFromDesign`** | 🔴 **唯一实现** | `CheckAbsorbRule` / `FindOwnAbsorbPoint` 是**运行期**共用件（~32 扇窗），换算必须从这里调得到；`MenuDraw` 是运行期程序集 |
| `Editor/MenuCheck.cs:220` `PointerFramePxFromDesign` | **一行转调**（保留签名） | `CollectionScene` 那 6 个调用点一个字不用改；⛔ 不在 Editor 侧重写公式（两份迟早漂，CLAUDE.md §三） |
| `Shell/MenuDraw.cs:130` `ScaleAboutPx` | `private` 单分量仿射 | 与 `PointerLayer.ScaleAbout` 同形；只被上面那份实现读 |

恒等式 / 退化守卫 / 「⛔ 不是整值乘 k」的推导全部写在 `MenuDraw` 那份 doc 里（唯一一份）。

**换算式**（`C = (960,540)`、`k = owner.parent.lossyScale` 逐分量）：

```text
pointer = C + (design − C) ⊙ k
```

`k == 1` ⇒ 逐位返回入参；`owner == null` ⇒ `(-99999,-99999)`；`owner.parent == null` ⇒ `k = 1`；
`k` 非有限 / `|k| < 1e-6` ⇒ 该分量按 1。

---

## 二、候选点逐行表（`Shell/MenuDraw.cs`）

`FindOwnAbsorbPoint(winRoot, designOwner, x1, y1, x2, y2, at, out px, out py)`（`:2951`）
—— **输入矩形与候选点 = 设计帧；`at` 与产出 `px/py` = 指针帧**。

| 行 | 内容 | 帧 | 说明 |
|---:|---|---|---|
| `:2957` | `cand` 容量 = `9 + AbsorbEdgeInset.Length * 8`（3 档 ⇒ 33） | 设计帧 | 定长数组（本文件只 `using UnityEngine`） |
| `:2959` | `(0,0)` —— **原版矩形中心**优先 | 设计帧 | 第一候选 |
| `:2960-2963` | 中心 ±80 的 **8 向一圈** | 设计帧 | 8 条 |
| `:2964-2971` | 贴边内缩 `{6,20,40}` × 8 条 = 24 | 设计帧 | `AbsorbEdgeInset`（本文件唯一一份） |
| `:2975` | 必须落在**原版矩形**里（内缩 3px） | 设计帧 | 不进 `at` |
| `:2977` | `pt = PointerFramePxFromDesign((tx,ty), designOwner)` | **设计 → 指针** | 🔴 本次新增的换算 |
| `:2979` | 「在屏幕里」（`2..1918` × `2..1078`） | **指针帧** | 本次从设计帧挪过来（屏幕真的那一帧） |
| `:2978` | `IsOwnAbsorbHit(at(pt.x, pt.y), winRoot)` | 指针帧 | 命中即返回 `pt` |
| `:2980-2986` | 找不到时 **40px 固定步长**扫矩形（从 `x1+4` 到 `x2−4`） | 设计 → 指针 | 确定性，不是随机 |

**`designOwner` 是谁**：`CheckAbsorbRule` 里取 `q.transform`（`MenuDraw.cs:2840`）——
`q` 就是 `node` 子树里第一颗 `ImageQuad`，也正是上面第 ③ 步量 `QuadRectPx(q)` 的那一颗
（乘的是它**父级**那一级 `lossyScale`，与 `PosInDesignSpace` 除的是同一级）。
取不到 `q` 时退 `node` / `winRoot`（那两档 `q == null` 上面已经报过红）。
`winRoot` 本身只用于 `IsOwnAbsorbHit`（`IsChildOf`）与诱饵灭自证，不参与换算。

**两处调用点**：`MenuDraw.cs:2846`（生产扫描）· `:2867`（`A2141` 别家窗诱饵——回调忽略坐标，换算无害）。

---

## 三、改法

### `Shell/MenuDraw.cs`

1. 新增 `public static Vector2 PointerFramePxFromDesign(Vector2, Transform)`（`:114`）+ `private ScaleAboutPx`（`:130`）。
2. `FindOwnAbsorbPoint` 增加 `Transform designOwner` 形参（`:2951`）；每个候选在喂 `at` **之前**换算，
   「在屏幕里」的守卫改判指针帧；产出 `px/py` 为指针帧。
3. `CheckAbsorbRule` 算出 `designOwner = q != null ? q.transform : (node ?? winRoot)`（`:2840`），
   两处调用都传它。
4. 新增自检只读口 `public static FindOwnAbsorbPointForTest(...)`（`:3000`）—— 函数体**只转发一跳**，
   不加工（`internal` 在 Editor 程序集看不见；本仓 0 个 `.asmdef`）。

### `Editor/MenuCheck.cs`

`PointerFramePxFromDesign`（`:220`）改为 `=> MenuDraw.PointerFramePxFromDesign(...)`；
删除原实现与 `ScaleAboutPx` 副本；doc 指向唯一实现。

### 行尾 / 规模

- 三文件均 **LF、CRLF 0、无 BOM**（改前改后一致；用 Edit，未用 `sed -i`）。
- `git diff --numstat`（对 HEAD）：`MenuDraw.cs` **89 / 9** · `MenuCheck.cs` **58 / 0**
  （含 `W58-PTRPORT` 已落盘的 +1 口，本次净减 36 行：实现搬走）· `ShellScene.cs` **398 / 74**
  （含此前既有 299/74 ⇒ 本次 +99 行）。

---

## 四、断言与灭自证（`Editor/ShellScene.cs:3360` ⑤·l 段）

夹具：`frameRoot.localScale = (2,2,1)`（世界原点）→ `MenuDraw.Absorb` 建设计矩形
**(1200,320)-(1400,520)** 的吸收层（档 3999 = 压过真界面最高档 ~3362）。
设计中心 `(1300,420)`；指针帧期望 `C + (design−C)·2 = (1640, 300)`。

| # | 断言 | 判别力 |
|---|---|---|
| 前提 | 指针层在 · `InputBlocked == false` | 锁着时 `HitButton` 直接 null ⇒ 不把红打在错的原因上 |
| 前提 | 吸收层由 `MenuDraw.Absorb` 建 · 下面有 `ImageQuad` | 没命中 quad 的话扫描天然找不到 |
| ★① | 单位缩放 ⇒ 换算口**逐位**返回入参（`==` 精确比较） | 抓「快路径不是逐位返回」 |
| ★② | `p2.x = 1640` / `p2.y = 300`（容差 0.01，期望值由恒等式字面量独立算） | 抓「整值乘 k」（会得 2600）与「不换算」（会得 1300） |
| ★③ | `MenuCheck.PointerFramePx(quad)` 与 `p2` 同帧（0.01） | 与**另一条独立读口**互证（裸世界坐标读口） |
| ★④ | `FindOwnAbsorbPointForTest` **找得到**；返回 `(1640,300)`；`ButtonAt(返回点)` 命中本夹具 | 走生产命中路，不是另算一遍 |
| ★⑤ | **设计帧点 (1300,420) 直接喂 `ButtonAt` 命不中**本夹具（指针帧命中区 x ∈ [1440,1840]） | 灭自证的另一半 |

**为什么是灭自证（结构上不可能同时满足）**：

- 本夹具设计中心与画布中心的 x 偏移 **340 > 半宽 100 × 2** ⇒ **所有设计候选点（含 ±80 与贴边）
  都落在指针帧命中区之外**；换算后的中心落在正中。
- 把实现退回「设计点直接喂 `at`」⇒ ④ 的 `found` 变假（中心候选 1300 落空，其它候选 x ≤ 1397 同样落空）
  ⇒ ④ 第一条红。
- 若想「把 ④ 的期望值也改成 1300」圆回去 ⇒ ⑤ 证明那一点 `ButtonAt` 命不中本夹具 ⇒ ⑤ 红。
  **两边不可能同时绿。**

---

## 五、其它宿主（**只报告，未改** —— 白名单外）

判据同普查件 §五：**参数是设计 px、而调用的是指针帧 API**。本次只复核了站点仍在（行号会漂，⛔ 别当坐标用）：

| 宿主 | 站点（现读） | 形态 | 状态 |
|---|---|---|---|
| `Shell/MenuDraw.cs` | `FindOwnAbsorbPoint` → `ButtonAt` | 共享件（~32 扇窗 `CheckAbsorbRule`） | ✅ **本件已修** |
| `RewardsScene.cs` | `ClickButtonByQuad`（`:566`）→ `ButtonAt/ClickAt` | `QuadRectOf`（设计帧）中心 | ⛔ 未改（被大量调用点共用） |
| `RewardsScene.cs` | `:3929`（5,5）· `:4856`（5,5）· `:3979` · `:4541/:4543` · `:11001` | 设计 px 字面量 / 设计矩形中心 | ⛔ 未改（今天父链单位缩放 ⇒ 无可观测差） |
| `MainMenuScene.cs` | `:1155`（`gearX=1847, gearY=35.5`）· `:1162/:1167/:1176/:1181` · `:1281` · `:3888` · `:7051…:7060` · `:10330` · `:11534…:11539` · `:11857` | 设计 px 字面量 / `QuadPxRect` 中心 | ⛔ 未改 |
| `ShopScene.cs` | `:2346`（`RectOf` 中心 → `ButtonAt`）· `:2360`（360,1010） | 设计帧 | ⛔ 未改 |
| `ShellScene.cs` | `:1238…:1262`（400,400）· `:1625…:1676`（5,5）· `:3265-3267` · `:3973-3988` | 探针设计 px 字面量 | ⚠️ **不是缺陷那一档**：探针窗都在**单位缩放**夹具下（设计帧 == 指针帧）；若将来给夹具加 ×M 会异帧 |
| `SettingsScene.cs` | `A167` 两态夹具 | **已手工乘 M** | ✅ 不是缺陷（将来可改用本口） |
| `CollectionScene.cs` | 6 处 | —— | ✅ `W58-PTRPORT` 已收口 |

---

## 六、风险 / 没查清

1. **断言没真跑**（铁律 12）。逻辑前提：`PointerLayer.Instance != null`、`InputBlocked == false`
   （本宿主前面几十条 `ButtonAt` 同前提）；已显式写成前提断言，红在错原因时能一眼看出。
2. **×2 夹具按 16:9 帧书写**：期望值 `(1640,300)` 由 `ToDesignPixel` 的 x 斜率推；ShellScene 的历史探针
   也在这一帧上（A1350 那批同做法）。若将来宿主改钉非 16:9，`PxPerWorldX` 会让 1640 差一个比例
   —— 那时应改成「用 `PointerFramePx(quad)` 当期望」或显式钉 aspect。
3. **`designOwner` 是逐站点判的**：本件取 `q.transform`（与 `QuadRectPx(q)` 同一帧）。
   若将来某扇窗的**吸收层内部**多出一级自带 `localScale` 的中间节点，`q.parent` 就是那一级
   —— 公式仍与 `PosInDesignSpace` 对称，但 `QuadRectPx` 的语义会先分家（该账属 `A1350` §二·3，不属本件）。
4. **同族欠账仍开着**：上表 `RewardsScene` / `MainMenuScene` / `ShopScene` / `ShellScene` 探针站点
   今天父链单位缩放 ⇒ **无可观测差**；按铁律 11 要全部做，但**另开一批**（白名单外）。

---

## 七、typecheck

```text
TMPDIR=/tmp/wf_absorb bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
