# 执行报告：`MainMenuScene.Run` 唯一红「（前提）首格有点击区」—— α 断言修

- 会话：2026-10-23 · 执行代理（`MainMenu 首格断言修复`）
- 判据：`资料/普查产出_1023b/诊断_MainMenu首格点击区.md`（只读诊断，结论 = **α 断言写错**）
- 红线遵守：**没跑 Unity** · **没动 git** · 没改正本 · 没碰生产代码

---

## 一、改动行

**文件：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`**（唯一改动的源码文件）

| 位置（改后） | 改动 |
|---|---|
| `:3078-3087` | **新增 10 行注释**：写明「点完必须重找」的根因链与 🧨 改坏法（判据出处逐条标） |
| `:3088` | **新增 1 行**：`own = FindChild(pw.transform, PracticeModePopup.YourDeckCellName);` |
| `:3089` | 原 `:3078` `var oh = FindChild(own, "Hit");` → 改成 `own != null ? … : null`（null 守卫） |
| `:3090` | 原 `:3079` `var owb = oh != null ? …` **原样保留** |
| `:3091` | 原 `:3080` 断言文案加后缀：`"（前提）首格有点击区"` → `"（前提）首格有点击区（重建后重找）"` |

- 净行数：**+11 行**（`18357` → `18368`），与「10 注释 + 1 重找」逐行对得上；删 3 行加 14 行 = 净 +11。✅
- **没有新增/删除任何断言**（`Check*` 条数不变，`owb` 那条只是文案加尾巴）。

### 为什么要重找（一句话）

`:3074` 的 `preWb.Click()` 走 `PickPair → PickPrebuilt → RebuildDeckRows`
（`PracticeModePopup.cs:2531 → :2574 → :1866-1870`），把整批格节点 `DestroyImmediate` 销毁重建
（编辑器非 playing 下 `Shell/MainMenuSubmenuWindow.cs:217-223`）；而 `own` 是 `:3049` **点击前**抓的
⇒ Unity 假 null ⇒ `FindChild(own, "Hit")` 恒 null（`Editor/MenuCheck.cs:141-147` 对 null 父直接返回、不抛异常）。

### 写法一致性

与同文件「点完就按名字重找」的四处同形：改后 `:3100`（`backCell`）· `:3172`（`r0d`）·
`:3191`（`r0e`）· `:3206`（`r0f`）—— 都是 `var x = FindChild(...); var h = x != null ? FindChild(x,"Hit") : null;`。
本次是本文件里**唯一**一处跨越 `Click()` 复用旧 `Transform` 的写法。

### 常量 / API 现核（不是照抄诊断）

- `PracticeModePopup.YourDeckCellName = "DeckRow_YourDeck"` —— `Shell/PracticeModePopup.cs:1970` ✅ 存在
- `MenuCheck.FindChild(Transform parent, string name)` —— `Editor/MenuCheck.cs:141` ✅ 签名一致（返回 `Transform`）
- `own` 是 `var` 局部（`:3049`），**重赋值合法**（同块作用域内）✅

---

## 二、恢复的断言块

`if (owb != null)` 里那一条 **`★ 点首格 ⇒ 选中我的卡组`**（`:3092-3098`）**恢复执行**：

```
owb.Click();
CheckTrue(pw.DeckIndex >= 0 && pw.PickedPrebuilt == null && pw.PairIndex < 0,
          "★ 点首格 ⇒ 选中**我的卡组**（`DeckIndex` = … 、预组与那一对作废）");
```

**它仍然有鉴别力**（不是自证）：
- 重找拿到的是**新格**，它的命中回调 = `PracticeModePopup.cs:1917` `() => PickDeck(yiForHit)`，
  而 `yiForHit` = **建格那一刻**的 `YourDeckIndex`（`ShowYourDeckCell` 那段里的局部量，不在闭包里读字段）。
- 断的是 `PickDeck`（`PracticeModePopup.cs:2469-2481`）真正写入的三个字段
  ⇒ 回调写成空 / 写成 `PickPair` 那一档改坏法照样红。
- **前置态已离开首格**：`:3075` 那条夹具断言（先点第 1 对 ⇒ `PairIndex=0`、`DeckIndex<0`）保证点之前选中的不是首格，
  ⇒ 「点首格之后变成它」不是恒真。

**复位块结构没动**（诊断 §六 明确要求别挪）：`:3099-3106` 的「点回第 1 对」仍在 `if (owb != null)` **之外**
（`if` 在 `:3098` 结束）—— 这一档即使将来又红，后面几段的夹具态也不会被带歪。

### 额外做的静态风险核验（对应诊断 §五 的「未验证项」）

诊断说「修完若仍红，下一个怀疑点 = `ShowYourDeckCell()` 点完第 1 对后返回 `false`」。**静态核过，不会**：

| 环节 | 事实 | 出处 |
|---|---|---|
| `PickPrebuilt` **不改** `ArmyIndex` | 函数体内无 `ArmyIndex =` 赋值；唯一的 `ArmyIndex = i` 在 `PickArmy` | `PracticeModePopup.cs:2558-2590`（函数起点 `:2558`）· 赋值点只有 `:1236` / `:2455` / `:2658` |
| `ShowYourDeckCell()` 的判据 | `YourDeckIndex = DeckIndex >= 0 ? DeckIndex : CollectionData.CurrentIndex()`，点完 `DeckIndex=-1` ⇒ **回落到 `CurrentIndex()`**，仍 ≥ 0 | `:1980-2004` · 写入点 `:2568`（`DeckIndex = -1;`） |
| 阵营那半 | `ArmyIndex` 不变 ⇒ 与点前同值，点前 `:3050` 已通过 ⇒ 点后仍通过 | 同上 |
| 重建确实会重画首格 | `RebuildDeckRows` 开头先 `if (ShowYourDeckCell()) { … BuildDeckCell(… YourDeckCellName …) }` | `:1907-1922`（`BuildDeckCell` 调用在 `:1912-1920`） |
| 首格 `Hit` 建得出来 | `BuildDeckCell` 尾部**无条件** `HitOn(... "Hit" ...)`；`HitOn` 只在 `ClipRectAbove` 失败（整块在视口外）时返 null | `:2135` · `:3215` |
| 首格矩形全在视口内 | 格 `(270.08,262.64)-(450.08,442.64)` ⊂ Viewport `(261.28,262.64)-(634.88,803.43)` | 诊断 §三·4（已核） |
| 点完**不会**把首格滚出视口 | `RebuildDeckRows` 末尾只做 `Offset = Mathf.Clamp(Offset, ClampLo, ClampHi)`，**不重置**、不 `SetOffset`（避开回调套回调）⇒ offset 只会被夹小 | `:1956-1963` |
| 已有旁证 | 同批重建后，兄弟格用**新引用**取到了 `Hit`+`WindowButton`：`menu.log:15890`（`:3092→` 改后 `:3103` 复位用）；首格本身用新引用也通过过：`menu.log:17174`（改后 `:3539-3540`「（前提）`DeckRow_YourDeck`（格位 0）在」） | 诊断 §三·4 |

⇒ 结论：**预计本条转绿，且后面 `★ 点首格…` 补跑并通过**。

---

## 三、typecheck

```
TMPDIR=/tmp/wf_menufix bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

**0 / 0** ✅（独立 `TMPDIR`，未与其它代理共用输出目录）

行尾：改前后都是**纯 LF**（`CRLF 0 / LF 18357` → `CRLF 0 / LF 18368`），未翻行尾；
`git diff --numstat` 该文件 `404 57`（含**本轮之前**就已存在的他人改动，非本次引入）。

---

## 四、台账

`资料/任务线/界面线/任务总表.md` —— 在 W66 那段之后**新增一段**（没改任何旧行、没删任何旧结论）：

> ✅ **2026-10-23 修：测试点后重找首格（α）** ……（根因 / 修法 / 生产零改动 / 正测恢复 / typecheck 0/0 / 判据与报告指针）

`W65` 主行、以及 §54 那行「首格缺失 ✅ 已修」**都没有动**（它们说的是**功能**已修，与本条「断言」是两件事；
本条已在新增段落里写清「α = 断言写错，不是生产缺陷」）。

---

## 五、没查清 / 留给主对话

1. **没跑 Unity**（红线）⇒ 结论是**静态 + 日志**推的。实际结果请以 `MainMenuScene.Run` 为准。
2. **预计读数**（照诊断 §四 的推算）：
   - 若只算本轮改动：`3966 通过 / 1 失败` ⇒ **`3968 通过 / 0 失败`**
     （红的那条转绿 `+1`，被跳过的 `★ 点首格…` 补跑 `+1`；**断言总数不变**，因为它本来就在代码里、只是被跳过）。
   - ⚠️ 注意：`MainMenuScene` 在这之后又被 W65-1c 收尾 / W66 两批改过，**实际总数不会等于 3968** ——
     **看「失败数 = 0」即可**，别拿 3968 当硬指标。
3. **诊断 §四 的可选项**「再加一条『重建之后首格还在』的前提断言」**没有加** —— 现有的
   `CheckTrue(owb != null, "（前提）首格有点击区（重建后重找）")` 已经同时覆盖了「首格在」+「有点击区」
   两件事（`own` 与 `Hit` 任一为 null 都会红）。若主对话想要更细的定位，再拆不迟。
4. 未动 `:3826` 那处复用旧 `awb` 的写法 —— 诊断 §六 已判**安全**（`PickArmy` 同阵营早返回）。

## 六、改动清单（全部）

| 文件 | 性质 |
|---|---|
| `Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs` | +11 行（10 注释 + 1 重找）、1 行文案加尾巴 |
| `Unity/资料/任务线/界面线/任务总表.md` | +1 段（W66 之后），LF 未翻 |
| `Unity/资料/普查产出_1023b/MainMenu首格修复_执行报告.md` | 本报告（新建） |
