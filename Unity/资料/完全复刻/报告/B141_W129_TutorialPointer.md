# B141 · W129 · `4-03` 教学指针层序 —— 执行报告

> 执行代理 **B141-W4** · 2026-11-04 · 第一百二十八会话 · 波次 `W129`。
> 施工单 = `判据/B141_W129_对账_UI动画.md` 的 **W3 包** + `报告/B138_审计汇总_问题清单.md` §4 `4-03`。
> ⛔ 未跑 Unity / 未动 git / 未改 `d:/2` / 未改账主表 / 未动任何白名单外文件。

---

## 一、结论

| 项 | 改前 | 改后 |
|---|---|---|
| 教学指针整链渲染队列 | **4110 / 4112 / 4113 / 4114 / 4115 / 4116** | **4041 / 4042 / 4043 / 4044 / 4045 / 4046** |
| 相对 toast（4072–4076） | **压在上面**（反序） | **在下面** ✅ |
| 相对 tooltip（4077–4079） | **压在上面**（反序） | **在下面** ✅ |
| 相对加载黑场（4090） | **压在上面**（反序） | **在下面** ✅ |
| 相对窗带（≤4030）/ 错误横幅（4035） | 在**上**（正确） | 仍在**上** ✅（原版 `PopUps/2` > `PopUps/1` > `PopUps/0`） |
| 相对教学聚光（原 4040 → 现 **4047**） | 在**上**（反序） | **在下面** ✅（原版 `PopUps/2` < `PopUps/601`；**2026-11-04 续做**已重排梯子，见 §三） |

落点 = `Unity/MyGame/Assets/CardPresentation/Shell/TutorialPointerUi.cs:82-83`（**现读**；审计里的 `:83` 是改前口径，改后常量仍在 `:83`，只改了值）。

---

## 二、原版判据（全部本地现读）

1. **教学指针 = `PopUps` 层 / order 2**
   `d:/2/tools/assetripper_out/ExportedProject/Assets/TutorialPointerMenu.prefab:140-147`：
   ```
   Canvas:
     m_RenderMode: 2
     m_OverrideSorting: 1
     m_SortingLayerID: 1054366423
     m_SortingOrder: 2
   ```
   层 id → 名字：`d:/2/tools/assetripper_out/ExportedProject/ProjectSettings/TagManager.asset:69` ⇒ `PopUps`（该层表第 7 层，其后是 `Blocking`）。

2. **同层的相邻档**（`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/Canvas/`，两份都 `m_SortingLayerID = 1054366423`）：
   * `Canvas_1073.json` → `m_SortingOrder: 601`（教学**聚光** `Tutorial highlight`）
   * `Canvas_1074.json` → `m_SortingOrder: 900`（**toast** `Toast Notification Controller`）
   * 错误横幅 = `PopUps/1`、窗 = `PopUps/0`、tooltip = `Blocking` 层（判据链同 `资料/普查产出_1020/W58_界面线_Toast黑场次序判据.md` §2·3）

3. **同一排序层里 order 小的先画（在下）** ⇒ 原版次序：
   `PopUps/0` 窗 **<** `PopUps/1` 错误横幅 **<** **`PopUps/2` 教学指针** **<** `PopUps/601` 聚光 **<** `PopUps/900` toast **<** `Blocking`(tooltip)
   ⇒ **原版从来不是「指针压过 toast」**；旧值 4110–4116 的次序是反的。

4. **链内次序**（同一 Canvas 里后画的在上）——
   `TutorialPointerWildcards.prefab` 根的 `m_Children` 依次 = `Glow (1)` / `Pointer` / `Generic Popup Background`，
   `light` 是 `Pointer` 的子件，`Background fill` / `TutorialText` 在气泡下 ⇒
   `Glow (1) < Pointer < light < 气泡底 < 气泡填充 < 气泡文字`（= 改后的 4041→4046，逐级 +1）。

---

## 三、层序梯子重排（2026-11-04 续做：把 🔶 偏离收掉）

**首版**曾把本链放在聚光 **之上**（4041–4046），并记了一条 🔶 已知偏离 —— 原因是「横幅 4035」与「聚光 4040」之间只剩 4 格，而本链要 6 个互不相同的队列。
**续做已按调度台要求把这条偏离收掉**：不改本链的 6 个值，改**把聚光抬上去**，让它整体压在指针之上。

### 新号段表（终态）

```
窗带最高 4030  (AlliancePanelWindow.QText)        ← 原版 PopUps/0（未动）
错误横幅 4035/4036 (ErrorMessageBanner.MenuQueue, 底条/文字) ← 原版 PopUps/1（未动）
指针链  4041–4046 (TutorialPointerUi.QGlow..QBubbleText)     ← 原版 PopUps/2
教学聚光 4047   (MenuTutorialHighlight.QShade)    ← 原版 PopUps/601（**4040 → 4047**）
toast    4072–4076 (ToastNotifications)           ← 原版 PopUps/900（未动）
tooltip  4077–4079 (Tooltip)                      ← 原版 Blocking（未动）
黑场     4090   (LoadingMenu.QPanel)              ← 我们的梯子最高档（未动）
```

⇒ 原版相对次序 `0 < 1 < 2 < 601 < 900 < Blocking` **逐段成立**，且不压任何既有窗带（≥4031 才动，窗带最高 4030）。

* 为什么不是「指针压到 4036–4039」：那只有 **4 格**，本链要 **6 个互不相同的渲染队列**（`Glow (1)` < `Pointer` < `light` < 气泡底 < 气泡填充 < 气泡文字，判据 = 原版 prefab 同一 Canvas 的子件序，见 §二·4）。
  同队列时透明队列按「到相机的 3D 距离」排序 ⇒ 谁压谁不可控（`CLAUDE.md` §三）。
* 为什么不是「把横幅挪到 4031」：4030 是**已知**最高窗队列，只留 1 格余量太脆；且横幅占 `Queue` + `Queue+1` 两格，会顶到 4032。

### 三处改动（一处改号，两处只改注释）

| 文件 | 改动 | 行数 |
|---|---|---|
| `Shell/MenuTutorialHighlight.cs` | `QShade` **4040 → 4047**（`🔶 自己设计` 标记行就地保留并补「2026-11-04 `B141_W4` 抬到 4047」的更正痕迹） | **212 → 212**（零变化） |
| `Battle/ErrorMessageBanner.cs` | `MenuQueue` **仍是 4035**，只把三条注释里「教学 4040」改成「指针 4041–4046 / 教学高亮 4047」 | **1234 → 1234**（零变化） |
| `Editor/ShellScene.cs` | W58 层序梯子：① `:8176-8178` 注释补 `PopUps/2`；② `:8187` 的 `CheckTrue` **把本链串进梯子**（`qBanner < QGlow && QBubbleText < qTut`）；③ `:8207-8208` 回归闸 `qTut == 4040 → 4047` | **11655 → 11655**（零变化） |
| `Shell/TutorialPointerUi.cs` | 6 个常量**不变**（4041–4046）；文件尾「层序」一节由「🔶 已知偏离」改写成「已落位 + 重排经过」 | 667（尾部注释块，账落点全在其之前） |

> ⚠️ 三处**行数全部守恒**（就地改写），⇒ 账在这些文件里的 `文件:行号` 落点**一个都没漂**。

### 断言同步（`Editor/B141_W4_Assert.cs`）

* **新增 B2b**：教学指针整链在教学聚光之下（原版 `PopUps/2` < `PopUps/601`）｜🧨 把本链抬到聚光之上、或把 `QShade` 降回 4040 ⇒ 本行红。
* **B6 由「🔶 已知偏离钉现状」改写为「本链 6 个队列互不相同」**（调度台要求的覆盖项）。
* `B2`（横幅 < 指针）、`B3`（< toast）、`B4`（< tooltip）、`B5`（< 黑场）保持。
* `A` 段 11 条（6 个绝对值 + 5 条链内次序）、`C` 段 17 条（实例级读回材质队列）不变。
  ⇒ 断言总数 **36 → 37**（`B2b` 新增；`B6` 是改写、不增条数）。

---

## 四、改动清单（两个文件，均未越白名单）

### 1. `CardPresentation/Shell/TutorialPointerUi.cs`

* `:82-83` **就地改写**（行数不变，仍 2 行）：
  * `:82` 注释改成 `// ✅ 已复刻（2026-11-04 · B141 · W4 · 4-03）：本链 = 原版 PopUps/order 2 那一档…`
  * `:83` 常量 `4110/4112/4113/4114/4115/4116` → **`4041/4042/4043/4044/4045/4046`**（`QGlow/QPointer/QLight/QBubble/QBubbleFill/QBubbleText`）
* **文件尾追加一节「层序」注释块**（原版 prefab 行号 · 梯子全图 · 重排经过 · ⛔ 别抬回 4110 / 别把 `QShade` 降回 4040）。
* **行数**：637 → **667**（+30，**全部在文件尾**；`:1-637` 除 `:82-83` 那两行的内容外一个字符没动 ⇒
  账在该文件的落点 `:167/198/206/231/274/284` **一个都没漂**）。

### 2. `CardPresentation/Editor/B141_W4_Assert.cs`（**新建**，类名 == 文件名、全局命名空间）

**37 条断言**（`B141_W4_Assert.Run()`，`Pass`/`Fail` 供宿主累加）：

| 段 | 条数 | 内容 |
|---|---|---|
| A · 绝对值回归闸 | **11** | `A1a-f` 六个常量逐值（4041…4046）；`A2a-e` 链内次序（Glow<Pointer<light<气泡底<填充<文字，判据 = 原版 prefab 子件序） |
| B · 梯子（钉外部件常量） | **9** | `B1/B1b/B1c` 三条**前提**（窗<横幅<聚光 · toast<tooltip<黑场）；`B2` 指针 > 错误横幅（原版 2>1）；**`B2b` ★ 整链 < 教学聚光（原版 2<601）**；`B3/B4/B5` 整链 < toast / tooltip / 黑场；`B6` 六个队列**互不相同** |
| C · 实例级（值真写进材质） | **17** | `C0-C3` 四条前提；`C4a-f` 六个件与 `Label` 都在；`C5a-f` **逐件读回 `ImageQuad.RenderQueue` / `Label.RenderQueue`** 与常量比；`C6` 实物复核「六件全部低于 toast」 |

* **灭自证**：
  * 把常量改回 `4110–4116` ⇒ `A1a-f` 6 条红 **且** `B3/B4/B5`/`C6` 红（4116 不再 < 4072/4078/4090）。
  * 把本链抬到聚光之上 / 把 `QShade` 降回 4040 ⇒ **`B2b` 红**。
  * 让任意两层共用同一个队列 ⇒ **`B6` 红**。
  * 只改常量、不改 `MenuDraw.Rect/Nine/Tiled/TextBox` 的调用点 ⇒ `C5a-f`/`C6` 红。
  * 前提不成立（图缺 / 气泡没建）⇒ **前提断言先红**，不留 `if (x != null) Check(...)` 那种静默跳过。
* ⚠️ **本代理不动宿主接线**：文件头已写清接法（`B141_W4_Assert.Run(); _pass += …; _fail += …;`，推荐宿主 `Editor/ShellScene.cs`）；本次已在 `ShellScene.cs:8187` 的 W58 梯子里串入「横幅 < 指针 < 聚光」这一跳（**那条会随 `ShellScene.Run` 真的执行**）。

---

## 五、验证

* `cd /d/4/Unity && TMPDIR=/tmp/wf_b141_w4 bash 工具/typecheck.sh`
  ⇒ **运行时 0 错 / 编辑器 0 错**（首轮 7 条 `CS0103: toast` 是本代理自己漏了局部变量，已当场修掉后复跑 0/0）。
* 行尾：两个文件都是**纯 LF**（`CRLF 0 / 裸 CR 0`），未翻行尾。
* ⛔ 按红线**未跑 Unity** ⇒ 断言**尚未在任何宿主里执行过**（`Editor/ShellScene.cs` 不在白名单，需调度台接线后再跑）。

---

## 六、没做的事 / 需要调度台决定的

1. **`Editor/B141_W4_Assert.cs` 仍未接线**（白名单只允许改 `Editor/ShellScene.cs` 的**层序梯子那几行**，不含加调用）⇒ 需要调度台在宿主里加两行。
   ✅ 但**关键次序已经有执行口**：本次把 `TutorialPointerUi.QGlow / QBubbleText` 串进了 ShellScene 那条 W58 层序梯子（`:8187`），跑 `ShellScene.Run` 就会验「横幅 < 指针 < 聚光 < toast < tooltip < 黑场」。
2. **「指针 < 聚光」已按原版次序落位**（§三续做）：`MenuTutorialHighlight.QShade` 4040 → **4047**，`Editor/ShellScene.cs` 的 W58 梯子已把本链串进去（`:8187`）。
3. **顺手发现（⛔ 未改，白名单外）**：
   * `Shell/DraftModeWindow.cs:205` 的注释「相邻带（…最高一条 = ToastNotifications 4072–4076 · `QGlow` 4110 / 4112–4116）」**已过期** —— 本链现在是 4041–4046，不再是全仓最高档。
   * `Editor/B108_A13_R3_Assert.cs:17` 的注释引用「`Shell/TutorialPointerUi.cs:589-641`」—— 本次是**追尾追加**（`:1-637` 不变），该区间的实现行未动，但**区间末尾**（`:636-641`）已顺延 31 行，引用可一并订正。
   * 账里 `未分类/其他 · CursorByArmySO.GetCursor` 的落点写 `CardPresentation/Shell/TutorialPointerUi.cs:82`，而 `:82` 现读是**队列注释行**（`GetCursor` 在 `:493`）—— 属**既有的**落点漂移，与本批无关（本批保持行数守恒，未加剧）。
4. **未做的事**：没有把 4110 段在其它文档/账里的叙述一并改掉（只读范围外）；没有跑任何宿主。
