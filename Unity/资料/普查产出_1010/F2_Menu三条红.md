# F2 · `menu` 三条红（件 1 = 真缺陷 · 件 2 = 夹具回归）

> 写手 F2 · 2026-10-11 · 只碰白名单三个文件（`Shell/MainMenuRuntime.cs` · `Editor/MainMenuScene.cs` · `资料/主菜单_原版规格.md`）
> 基线 = `d:/4/_tmp_view/menu.log` 的 `[Menu] === 合计：1754 通过 / 3 失败 ===`（3 条：`:892` Rewards · `:16244` 匹配弹窗 · `:16401` 遭遇战窗，文末 `:30113/30124/30135` 是同一批红的重打）。

---

## ① 结论

**件 1（`:892`，分类 c · 真缺陷）**：`Rewards` 导航钮的**图标**确实比原版**低 10px** —— 而且**文档 / 实现 / 断言三处同源错**，另两颗（`Shop`/`Social`）的期望值也照抄了同一批错值 ⇒ 那两条是**假绿**。已按原版 RT 现推的值**三处一并订正**，并补了一条**相对**断言（相邻图标顶步进）防同类复发。

**件 2（`:16244` · `:16401`，分类 a · 回归）**：根因是上一批 **A217③ 照原版**让弹窗也写 `currentWindow` ⇒ `sk` 被 `ToBackground()` ⇒ `PointerReachable` 整批跳过它。**A217③ 不退、`PointerReachable` 不放宽**，改的是**自检夹具**：在模式校验那段之后**显式关掉那扇 `PromptPopup`**（走生产的 `Close → NotifyClosed → ShowPreviousWindow` 链把 `sk` 带回 `Open`）。

---

## ② 证据（全部亲读，不是转抄诊断）

### 件 1 —— 原版五颗图标的 RT 字段

`d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/RectTransform/`

| RT | 是谁 | `m_Father` | `anchorMin/Max` | `m_AnchoredPosition` | `m_SizeDelta` | `m_Pivot` |
|---|---|---|---|---|---|---|
| `1111` | Home 钮的 `Image` | `1123`(Home 钮) | (0,1)/(0,1) | (83.0949, −71.70) | **156.349 × 137.435** | (0.5, 0.5) |
| `1241` | Shop 钮的 `Image` | `1247`(Shop 钮) | 同上 | 同上 | **140 × 140** | 同上 |
| `1243` | Rewards 钮的 `Image` | `1245`(Rewards 钮) | 同上 | 同上 | **140 × 140** | 同上 |
| `1240` | Social 钮的 `Image` | `1244`(Social 钮) | 同上 | **同上** | **140 × 140** | 同上 |
| `1242` | Collection 钮的 `Image` | `1246`(Collection 钮) | 同上 | 同上 | **140 × 140** | 同上 |

⇒ 五颗**同一份写法**：图标顶 = 钮顶 + `71.70 − 高/2` = 钮顶 **+1.70**（四颗方的）/ **+2.9825**（Home）。

钮 RT（`1123`/`1244`/`1245`/`1246`/`1247`）全 `sizeDelta = (164.31, 169.68)`、`m_Father = 1112`（`Buttons Container`）。

父 VLG（实读工具：`python 工具/menu_dump.py bundle_scenes_scenes_mainmenuwarpforge "Navigation Panel" --depth 4 --no-sprite`）：

```
Buttons Container   VerticalLayoutGroup   spacing=-16.350000381469727 align=1 pad=0,0,-5,0 ctrlW=0 ctrlH=0 expandW=0 expandH=0
```
（`align=1` = **UpperCenter**；`pad` 打印序 = (left, right, top, bottom) ⇒ **padTop = −5**；`ctrlH=0` ⇒ 钮保持自己的 169.68 高。）

**推算**（钮顶 = 容器顶 148.41 + padTop −5，之后每颗步进 `169.68 − 16.35 = 153.33`）：

| 钮 | 钮顶 | 图标顶 = 钮顶 + 1.70（Home 2.9825） | 图标 y1..y2 |
|---|---|---|---|
| Home | 143.41 | +2.9825 | 146.39..283.83 |
| Collection | 296.74 | +1.70 | 298.44..438.44 |
| **Shop** | 450.07 | +1.70 | **451.77..591.77** |
| **Rewards** | 603.40 | +1.70 | **605.10..745.10** |
| **Social** | 756.73 | +1.70 | **758.43..898.43** |

⇒ 我们原来的 `461.8 / 615.1 / 768.4` 各**低 10px**；`Rewards` 之所以是唯一红的那条，是因为断言的 **`y1` 抄的是对的 605.1、`y2` 抄了错文档的 755.1** ⇒ 期望矩形成了 **150 高**，与文案里的「140×140」自相矛盾。

> ⚠️ **复核办法如实说明**：`menu_dump.py` 在这棵树上**给不出绝对矩形** —— 父链 `m_LocalScale` = 0（原版存下的姿态）⇒ 整棵树被压成 `0.00×0.00`（工具表尾自己标了那 41 处）。所以上表是**「RT 原字段 + uGUI VLG 算法手算」**得出的，并与两处**已知正确**的值互相吻合：文档 `Home`/`Collection` 两行、以及 A284 已核的「Home 顶距 2.98」。

### 件 2 —— 回归链条（四段，全部实读）

1. `Shell/LiveOpsEventWindow.cs:805-809`：模式不合 ⇒ `Manager.ShowPopUp(whyMode, …)`。
2. `Shell/WindowsManager.cs:713-720`：`ShowPopUp → OpenWindow`，**弹窗支也写 `currentWindow`**（A217③，照 `WindowsManager__OpenWindowCO.c:50`）⇒ 上一句 `currentWindow.ToBackground()` 把 `sk` 压成 `Background`。
3. `Shell/PointerLayer.cs:1009-1013`：`PointerReachable` ⇒ `w.CurrentState != WindowState.Background` ⇒ **`sk` 整棵树被跳过** ⇒ `CheckAbsorbRule` 的 40px 网格扫遍全矩形也找不到命中吸收层的点。
4. 复原链本来就在：`WindowsManager.cs:811-822` `NotifyClosed` →（`wasTop`）→ `:848` `ShowPreviousWindow` → `:489` `ReopenFromBackground`（`SetActive(true)` + `CurrentState = Open`）。**自检里那扇弹窗从没被关过** ⇒ 底窗没人救。

---

## ③ 改动清单

### A. `MyGame/Assets/CardPresentation/Shell/MainMenuRuntime.cs`（实现，1 处值 + 2 段注释）
- `:291-293` 三个 `iy1`：`461.8 → 451.8` · `615.1 → 605.1` · `768.4 → 758.4`（`Home` / `Collection` **一字未动** —— ⛔ 没碰 A284 的 `137.435`）。
- `:273-283` 调用点上方补一段 **2026-10-11 更正**（判据、推算过程、出处 `D1_十二条红诊断.md §B-1`）。
- `:303-306`（原 `:289`）`<param name="iy1">` 订正：原来写「都来自 §五 C 表」⇒ 改成「照 RT 现推」，并写明那张表错在哪。

### B. `MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（断言 + 夹具）
1. `:412-414` 三条期望值改为 **`451.8..591.8` / `605.1..745.1` / `758.4..898.4`**（原 `:401-403`；⛔ 不读 `MainMenuRuntime` 的任何常量；期望值来源写进 `:401-411` 那段注释 = 原版 RT pid + VLG 字段）。
2. `:425-428` 新增 **4 条相对断言** `CheckNavIconStep`：`Home→Collection = 152.05`（= 153.33 − 1.2825）、`Collection→Shop` / `Shop→Rewards` / `Rewards→Social` = **153.33**（= 原版钮 RT 高 169.68 + VLG spacing −16.35）。新助手 `:6700-6711`，注释 `:416-424`。
   **为什么加**：绝对量那五条会被「抄错文档的期望值」**整批带偏**（本次 `Shop`/`Social` 就是假绿）——相对量拦的就是「整列挪 10」这个错法。
3. `:3273-3295`（原 `:3246` 那个 `}` 之后，现 `:3271`）**夹具**：关掉那扇 `PromptPopup` ——
   `sk.Manager.TopWindow as PromptPopup`（`:3288`）→ `Close()`，并配两条前提断言（弹窗真的弹了 / 关掉后 `sk.CurrentState == Open`）。
   **改坏法**：把这段删掉（= 把 `PromptPopup` 留在开着）⇒ `:16244` 与 `:16401` 两条 `found` 又红。

### C. `资料/主菜单_原版规格.md`（`:345-347` + 表下一段更正块）
- 三行的 `Image y` 改为 `451.8..591.8` / `605.1..745.1` / `758.4..898.4`。
- 表下新增 **2026-10-11 更正块**（原值 / 现值 / 错因 / 判据 / 「同表其余列不受影响」/「这个错被抄进过实现与断言」），保留更正痕迹（铁律 5）。
- ⚠️ 明确写了：**错因里「为什么会多出那 10」没查到**（见 ④），没有编一个说法。

> 只改了这三个文件（+ 本报告）。两条正本、`待办判据_*.md`、`已知的坑.md` **一个字没动**；别人的 `BattlePostFx.cs` / `Editor/{CollectionScene,SettingsScene}.cs` / `AnimFX_实现与接线.md` **没碰**。三个文件的**行尾都没翻**（改后复测 `CRLF=0`，纯 LF，与本批之前一致）。

---

## ④ 没查清的部分

1. **那份文档后三行为什么统一 +10 —— 没查到。** 表是**推算值**（原版 `Image` 的 `pos` 全是 `(0,0)`），本仓没有它的中间稿/生成脚本；`Home`/`Collection` 两行用的是**对的**算式，所以「同一张表里一半对一半错」。文档更正块里如实写了「没查到」，**没有编错因**。
2. **`CheckAbsorbRule` 里那 12 条断言从没跑到过。** 该函数 `:156` 有 `if (!found) return;` ⇒ `found` 假时**它后面 6 条整批不跑**（匹配弹窗 + 遭遇战窗 = **12 条**）。它们上次真正执行是 **1007 批**（`s1007d_menu.log` 1512/0）。⇒ 本次修绿后这 12 条**第一次重新上电**，我预期全绿（几何/档位/告警那几条在 1010 批本来就 ✓，且 A217③ 只改了「谁写 `currentWindow`」、没碰命中排序），**但没跑过就没有实据**。
3. **`ShowPreviousWindow` 的 `lastMain.UnHide()`**（`Shell/WindowsManager.cs:883`）：那一刻 `openWindows` 里若还留着一扇**非弹窗**窗，它会被 `SetActive(true)` **重新显示出来**。我没法判断当时表里到底有没有（不能跑 Unity）⇒ **若有，症状是截图里多一扇窗**（不是断言红）。
4. **「顺手」那条没改**（见 ⑤·4）：唯一的同形写法在 `Editor/SettingsScene.cs:351-356`，**是别人的文件（黑名单）**；`MainMenuScene.cs` 里 `grep '"?"'` **零命中** ⇒ 无同形可改。

---

## ⑤ 顺手发现（**只报不改**）

1. **`资料/主菜单_原版规格.md:126` 那条原始字段本来就写对了**（`pos(83.09,-71.7) sz(PLAY 156.3×137.4；其余 4 个 140×140)`）—— 错的只是下面那张**推算表**。⇒ 可复用的教训：**同一份文档里「原始字段」与「推算值」打架时，以原始字段为准**。
2. **`D1_十二条红诊断.md` §B-1 的措辞有歧义**：它写「引 `RectTransform_1243.json`（Rewards 钮的 `Image`）」，而紧接着又写「**五颗钮 RT** 全 164.31×169.68」—— 这里的「钮 RT」是 `1123`/`1244`–`1247`，与 `1243`（图标）**不是同一批 pid**，连着读容易串。
3. **这一族节点没有脚本化的绝对矩形源**：`menu_dump.py` 对 `Navigation Panel` 整棵树给 `0×0`（父链 `m_LocalScale = 0`，工具自标）。`--no-ancestor-scale` 只救「乘以非零缩放」那一档、**救不了「等于 0」**。本次是靠「RT 原字段 + VLG 手算」，下次轮到别的子树可能同样卡住。
4. **`SettingsScene.Click(Transform)` 的失败文案（`:351-356`）确实分不出「节点没建」与「抓的是旧树」**（两种情况都打 `?`）—— 诊断说这正是耽误定位的地方。**该改，但不在我的白名单里**（建议派给 SettingsScene 那条线的写手/审查）。
5. **`CheckAbsorbRule` 的早退会静默吞掉 6 条断言**（`if (!found) return;`）—— 这是本次「只有 1 条红」假象的一半来源（实际有 12 条断言从未执行）。建议将来在失败文案里带上「**本次跳过了 N 条**」。

---

## ⑥ 跑过的检查

- **秒级类型检查**：`TMPDIR=/tmp/wf_f2 bash d:/4/Unity/工具/typecheck.sh`
  - 第 1 次：`运行时错误数: 0` / `编辑器错误数: 1` → `MainMenuScene.cs(6707,19): error CS0165: 使用了未赋值的局部变量"d1"` —— **我自己的错**（两次 `out` 写成 `&&` 短路 ⇒ 第二条没执行时 `c1..d2` 未赋值）。改成两句分开 ⇒
  - 第 2 次：**运行时 0 错 / 编辑器 0 错** ✅
- **没有跑任何自检**（红线：不跑 Unity）。上面每一处都只是**静态**核对过（原版 JSON / 反编译 / 源码）。
- 改完复测行尾：三个文件 `CRLF = 0`（纯 LF），`git diff --numstat` 数字与改动量相称、没有整篇重写。

### 修完后 `menu.log` 该是几条通过 / 几条失败 —— **1775 通过 / 0 失败**

| 项 | 条数 | 说明 |
|---|---|---|
| 基线 | **1754 通过 / 3 失败** | 实读 `_tmp_view/menu.log` 的 `=== 合计：1754 通过 / 3 失败 ===` |
| 三条回来的 | **+3** | Rewards 图标（件 1）· 匹配弹窗 + 遭遇战窗（件 2） |
| 我新增的 | **+6** | 4 条 `CheckNavIconStep` + 夹具那 2 条前提断言 |
| 本来被短路掉的 | **+12** | 两个 `CheckAbsorbRule` 各有一条 `if (!found) return;` ⇒ 修绿后各多跑 **6 条**（见 ④·2） |
| **合计** | **1775 / 0** | **旧断言一条没减** |

⚠️ 那 **12 条**是本次**没有实据**的部分（④·2）：若它们里有红的，读数会是 `1763+N / M` —— 只要不是 0，就是把 ④·2 那批唤醒了，**不是**件 1/件 2 没修好（后两条的证据链是独立的）。
