# B145-E1 TMProWarpText 订阅修复

执行代理 `B145-E1`（2026-11-05）。范围 = `Core/TMProWarpText.cs` + 新建 `Editor/B145_WarpText_Assert.cs` + 接线 `Editor/ShellScene.cs:11301`。
⛔ 未跑 Unity · ⛔ 未动 git / 账 / `活状态表.md`。

---

## 判据（原版 文件:行号 + 摘录）

**① 订阅哪个事件 —— `TMPro_EventManager.TEXT_CHANGED_EVENT`（`FastAction<Object>`），不是 `OnPreRenderText`**

`D:/2/tools/decomp_full/TMProWarpText__OnEnable.c:15-24`：

```
15  TMProWarpText__WarpText(param_1,0);                       // 先弯一次
19  lVar1 = *(longlong *)(*(longlong *)(DAT_1842b08a0 + 0xb8) + 0x58);   // TMPro_EventManager 的静态字段
20  uVar2 = thunk_FUN_18042f4a0(DAT_18429c8e0);
21  System_Action<object>___ctor(uVar2,param_1,DAT_1842aff60,0);         // 委托 = this.ReactToTextChanged
23  TMPro_FastAction<bool>__Add(lVar1,uVar2,DAT_1842db4f8);
```

`D:/2/tools/cpp2il/out_cpp2il_dev_ilspy/TMProWarpText.cs:66-79`（ILSpy 版同义、名字是明文）：

```csharp
private void OnEnable() {
    WarpText();
    Action<UnityEngine.Object> rhs = ReactToTextChanged;
    TMPro_EventManager.TEXT_CHANGED_EVENT.Add(rhs);
}
private void OnDisable() {
    Action<UnityEngine.Object> rhs = ReactToTextChanged;
    TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(rhs);
    text.ForceMeshUpdate();
}
```

**② 退订成对、且在 `ForceMeshUpdate()` 之前**

`D:/2/tools/decomp_full/TMProWarpText__OnDisable.c:19-28`：`TMPro_FastAction<bool>__Remove(...)`（`:23`）→ `(**(code **)(*plVar2 + 0x7d8))(plVar2, …)`（`:28`，= TMP 的 `ForceMeshUpdate`）。

**③ 回调签名 + 三道闸**：`D:/2/tools/decomp_full/TMProWarpText__ReactToTextChanged.c:22-30` —— `obj` 隐式 bool 为假 ⇒ 返回；`text` 为空 ⇒ 返回；`obj != text` ⇒ 返回；`isForceUpdatingMesh(+0x3c) == 0` 才 `WarpText()`。与现有 `TMProWarpText.cs:103-110` 逐条一致 ⇒ **本次不改**。

**④ 本工程 TMP 的实际成员名**（⛔ 没照抄网上旧名字，现读）：
`D:/4/Unity/MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/TMP/TMPro_EventManager.cs:30`
`public static readonly FastAction<Object> TEXT_CHANGED_EVENT = new FastAction<Object>();`
`FastAction<A>.Add/Remove(Action<A>)`、内部私有 `LinkedList<Action<A>> delegates` + `Dictionary lookup`（`FastAction.cs`）⇒ 元素类型 `UnityEngine.Object` 与原版那个 `System_Action<object>` 对齐。

---

## 改动（文件:行号 → 改前/改后）

全部改动**行数零变化**（`:198` → `:198` 行；账里 `菜单_外壳.tsv` L660–L665 引用的 `:82 / :88 / :95 / :103 / :113 / :135` 六个落点**逐一回位**，未漂移）。

| # | 文件:行 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Core/TMProWarpText.cs:10` | `…__OnEnable.c —— 先 WarpText() 一次，再订阅 OnPreRenderText` | `…__OnEnable.c —— 先 WarpText() 一次，再 TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged)` |
| 2 | `Core/TMProWarpText.cs:28-29` | 「`OnPreRenderText` 的订阅：TMP 版本差异下该事件可能不存在 ⇒ 用 `Configure()` 显式接线 + `Update` 里『文本变了才重弯』的等价做法兜底（见 `Tick()`）」 | 「⚠️ 2026-11-05 订正（B145-E1 · 铁律 5）：**全是错的** —— 原版订阅的是 `TMPro_EventManager.TEXT_CHANGED_EVENT`…；本文件**没有** `Tick()` / `Update()`」 |
| 3 | `Core/TMProWarpText.cs:88-93` | `OnEnable` = null 回填 → `if (text == null) return;` → `WarpText();` | null 回填 → `WarpText();` → `TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ReactToTextChanged);`（**去掉了那个早退** —— 有它会在 `text` 为空时静默不订阅；原版没有这个早退） |
| 4 | `Core/TMProWarpText.cs:95-99` | `OnDisable` = `if (text == null) return;` → `text.ForceMeshUpdate();` | `TEXT_CHANGED_EVENT.Remove(ReactToTextChanged);`（**先退订**）→ `if (text != null) text.ForceMeshUpdate();` |
| 5 | `Core/TMProWarpText.cs:123`（doc） | `<para>② textInfo.characterCount == 0 \|\| lineCount == 0 ⇒ return（照抄两个空判据）` | 补上「且**不复位** `isForceUpdatingMesh`（原版 bug 照抄 …⛔ 别修）」+ `.c:120/123/126` vs `.c:352` |
| 6 | `Core/TMProWarpText.cs:134`（doc） | `<para>⑩ 循环结束 isForceUpdatingMesh = false。` | `<para>⑩ 循环结束 isForceUpdatingMesh = false（**唯一复位点**，判据 .c:352）。` |
| 7 | `Core/TMProWarpText.cs:144-145` | `if (ti == null) { _warping = false; return; }` / `if (ti.characterCount == 0 \|\| ti.lineCount == 0) { _warping = false; return; }` | 两条早退**都去掉 `_warping = false`**（照抄原版 bug），行尾写明 `.c:120/123/126` 早退不复位、`.c:352` 是唯一复位点、⛔ 别修 |

标记（铁律 18/20）：
- `:92` `// ✅ 已复刻（2026-11-05 · B145-E1 · decomp_full/TMProWarpText__OnEnable.c:15-24：先弯 → TEXT_CHANGED_EVENT.Add）`
- `:97` `// ✅ 已复刻（2026-11-05 · B145-E1 · decomp_full/TMProWarpText__OnDisable.c:19-23：先退订）`
- `:98` `// 🔶 自己设计（非原版）：text 空就跳过 —— 原版这里直接调、会 NRE（调度台 2026-11-05 判）`（原版 `OnDisable.c:24-28` 确实**不**判空，`text == null` 会走 `OP_IMPLICIT` 为假那条，最终落到 `FUN_1803f47a0()` = 空引用抛出）

---

## 调度台裁定补做：`isForceUpdatingMesh` 照抄原版 bug

**裁定**（2026-11-05，调度台）：铁律 18·0「默认复刻原版，**包括原版自己的 bug**」⇒ `WarpText()` 的三条早退分支**不复位**，并补断言钉住。

### ① 先核事实：`BoosterPackOpenWindow` 那条路的先后顺序（原文摘录）

```
BoosterPackOpenWindow.cs:1591   item.Bind(mr, EnsureArmyTexts(go.transform), WindowSounds.BoosterOpenCue(Army), BoosterTermOf(Army), …);
BoosterPackOpenWindow.cs:1928-1931
                                var w = t.GetComponent<TMProWarpText>();
                                if (w == null) w = t.gameObject.AddComponent<TMProWarpText>();
                                bool second = t.gameObject.name != "Army Text";
                                w.Configure(t, TMProWarpText.Axis.Z, ArmyWarpCurve(second), ArmyWarpYCurveScaling);
Core/TMProWarpText.cs:79        WarpText();                       // ← Configure 的最后一句
BoosterPackVisualItem.cs:113    public void Initialize(string localizedArmyText, BoosterPackOpenWindow boosterPackOpenWindow)
BoosterPackVisualItem.cs:145        t.text = localizedArmyText;   // 原版 `BoosterPackVisualItem__Initialize.c:31-42` 同
```

**C# 求值顺序 ⇒ 事实**：`EnsureArmyTexts(...)` 是 `Bind(...)` 的**实参**，它先跑完（含 `Configure → WarpText()`），`Bind` 体才开始；而**写文字在更晚**的 `Initialize`（`BoosterPackVisualItem.cs:145`）。
所以 `Configure → WarpText()` 跑在**空文本**上 —— 与 `BoosterPackOpenWindow.cs:1918-1921` 那句现成注释一致：「两颗节点出厂是关着的 ⇒ `WarpText()` 此刻会因 `characterCount == 0` 提前返回」。

### ② 改法（见上表第 5–7 行）

`:144` / `:145` 两条早退去掉 `_warping = false`，行尾就地写「原版这里早退不复位 …（原版 bug，照抄，⛔ 别修）」；`:135` 那个 `if (tmp == null) return;` **在 `_warping = true` 之前**，保持不动（原版 `.c:110` 是先置 1 再判空、空则抛，我们这里改不出可观测差异）。

### ③ 新增断言 E（4 条，已进 `B145_WarpText_Assert`）

- **E1**：空文本 TMP + `Configure(...)` ⇒ `IsForceUpdatingMesh` **仍为 1**。
- **E2**：紧接着 `ReactToTextChanged(自己)` ⇒ 仍为 1（原版 `__ReactToTextChanged.c:29` 的闸恒假）。
- **E3 前提**：带字形夹具 `characterCount > 0`；**E3**：走完循环后 `IsForceUpdatingMesh == false`（灭自证：证明 E1 不是恒 true）。

### ④ 结论：**照抄后，卡包那条路在原版里也不靠事件重弯 —— 这是正确复刻，不是回归**

把原版那条链按顺序走一遍：
1. `Configure → WarpText()` 在**空文本**上跑 ⇒ `characterCount == 0` 早退 ⇒ 原版把 `isForceUpdatingMesh` **留在 1**（`.c:110` 置 1、`:123` 早退不复位）。
2. `Initialize` 里 `t.text = …` ⇒ TMP 的 `SetText` 会发 `ON_TEXT_CHANGED` ⇒ 进 `ReactToTextChanged` ⇒ 撞上 `+0x3c == 1` 那道闸 ⇒ **原版在这里就不重弯**。
3. 真正让包面弯起来的是**节点激活时的 `OnEnable → WarpText()`**（原版 `__OnEnable.c:15`）—— 它**没有**这道闸，而且那时文字已经写好，会走完循环并在 `.c:352` 把标记复位 0。

⇒ 玩家在原版里看到的「弯字」来自第 3 步，第 2 步本来就不生效；我们照抄后**可见行为与原版一致**。
⚠️ **唯一会露出来的边界**（如实写）：若我们壳里的**激活时序早于 `Initialize`**，那 `OnEnable` 的 `WarpText()` 也跑在空文本上 ⇒ 之后这个组件**永久不再重弯**（原版同样如此）。要改这个就得改成「原版没有的行为」，⛔ 本波不做；若真机上看到「卡包包面不弯」，请按这条链先查激活/写字的先后，**别先怀疑订阅**。

---

## 新增断言（类名 / 条数 / 接线到 宿主:行号）

**类名** `B145_WarpText_Assert` · 文件 `D:/4/Unity/MyGame/Assets/CardPresentation/Editor/B145_WarpText_Assert.cs`
**条数** = 全前提成立时 **30 条**（A 5 · B 10 · C 4 · E 4 · D 7）；A 前提挂则 B/C/D 早退（A 那几条已红）。

| 组 | 断的是什么 | 办法 |
|---|---|---|
| A（5） | `OnEnable`/`OnDisable`/`ReactToTextChanged(UnityEngine.Object)` 三个方法都在 · `TEXT_CHANGED_EVENT` 是 `FastAction<UnityEngine.Object>` · 能读到订阅表 | 反射 |
| B（10） | `OnEnable` ⇒ 订阅数 **+1**、且新委托**指向本组件**（B1/B1'）· 重复 `OnEnable` 不重复订（B2）· `OnDisable` ⇒ **回落**且委托消失（B3/B3'）· `text` 为空时照样订阅/照样退订不抛（B4/B5）· 带真 TMP（`Resources/Fonts/RobotoCondensed-Regular SDF`）整条路径不抛且 +1/−1（B6 前提 + B6 + B6'） | 反射数 `FastAction<Object>.delegates` 链表的**差值** |
| C（4） | `ReactToTextChanged(null)` / 非 `TMP_Text` / **别家的** `TMP_Text` 三种入参都不抛、且**不改订阅数** | 直接调 + try/catch |
| E（4） | 原版 bug 照抄（调度台 2026-11-05 裁定）：空文本跑过一次 `WarpText()` ⇒ `isForceUpdatingMesh` **仍为 1**（E1）· 紧接着 `ReactToTextChanged(自己)` ⇒ **不会**重弯（E2）· 灭自证：**有字形**的文本走完循环 ⇒ 复位为 0（E3 前提 `characterCount > 0` + E3） | 读 `TMProWarpText.IsForceUpdatingMesh` 只读口 |
| D（7） | 源码结构：`WarpText()` 在 `.Add(...)` **之前**（D1）· `.Remove(...)` 在 `ForceMeshUpdate()` **之前**（D2）· **不许**再出现 `if (text == null) return;`（D3 灭自证）· 文件里**没有** `void Update()` / `void Tick()`（D4，杀回旧的假自陈） | 读 `Assets/CardPresentation/Core/TMProWarpText.cs` 文本 |

灭自证（怎么才能红）已写在文件头：删 `.Add` ⇒ B1/B1'/D1 红；删 `.Remove` ⇒ B3/B3'/D2 红；恢复 `if (text == null) return;` ⇒ B4+D3 红；无条件 `ForceMeshUpdate()` ⇒ B5 红。
⚠️ 「先弯再订」这条**行为测不出来**（弯两次结果相同），只能靠 D1 结构守卫 —— 文件头里写明了。

**接线**：`Editor/ShellScene.cs:11301`（ShellScene 的聚合行，**合计 `Debug.Log` 之前**、**同一物理行追加**）：
`…B144_C_UI_Assert.Fail; /* ✅ 已复刻（2026-11-05 · B145-E1 · 断言宿主接线）：… */ B145_WarpText_Assert.Run(); _sink.Pass += B145_WarpText_Assert.Pass; _sink.Fail += B145_WarpText_Assert.Fail; Debug.Log(P + $"=== 合计…`
- 机器核过：该 `Run()` 落在**块注释之后、行内第一个 `//` 之前**（`//` 在列 17112，调用在列 16578）⇒ **不会被注释吞掉**；`ShellScene.cs` 行数不变（11780 个 LF / 11781 行）、行尾仍是 LF。
- `B145_WarpText_Assert` 全仓 4 次出现：3 次在 ShellScene 那一行（块注释 + 接线 3 处），1 次在本文件自身。

---

## typecheck 结果

`cd D:/4/Unity/MyGame && bash D:/4/Unity/工具/typecheck.sh`

- 改动前后各跑多次（**最终读数 = 运行时 0 · 编辑器 0**）：
  - `TMPDIR=/tmp/wf_e1 bash D:/4/Unity/工具/typecheck.sh` → **运行时错误数: 0 · 编辑器错误数: 0** ✅（连跑两次一致）
  - 另用一个**全新空目录** `TMPDIR=/tmp/wf_e1v` 再跑一次 → **0 / 0** ✅
    （为什么加这一次：`/tmp/wf_e1/wfcheck/WFCheck.dll` 原是 21:29 的旧产物，按本仓已知坑「运行时编不过时脚本不清旧 DLL ⇒ 编辑器那半边拿旧 DLL 编、报一串级联假错」；换全新目录能排掉这一层，结果同样是 0/0 ⇒ 我的三个文件确实干净。）
  - 中途出现过两类**不是我负责的**错，均已随写手修好而消失：
    · `RuleEngine/Core/RuleCore.cs(18873) CS0111 AbilityTargetListFromCriteriaForTest 重复定义`（`B145-E2` 正在写，同名方法一度三处）
    · `CardPresentation/Battle/MultiCardDisplay.cs(530) CS0103 BuildHeaderHitRect`（`B145-E6` 正在写，文件 mtime 每秒在变）+ 由它引起的 `B142_E17` / `B142_E62` 两条级联假错（旧 `WFCheck.dll`）
    · 按纪律 **⛔ 未去改别人的文件**，只是等它们写完重跑。

---

## 没做成 / 与判据冲突的地方

1. **`isForceUpdatingMesh` 的复位语义** —— 已按调度台 2026-11-05 裁定**照抄原版 bug**（见上面那一整节）；原「担心会让主场景功能失效」已核清：卡包那条路在原版里**本来也不靠事件重弯**，可见行为不变。
2. **「先弯再订」的顺序无法用纯行为断言证明**（`WarpText` 幂等）⇒ 用 D1 源码结构守卫代替，文件头已声明这是**结构证据不是行为证据**。
3. **本件没跑 Unity**（执行代理纪律）⇒ 断言是**待跑**状态；跑绿之前不得据此收账。

---

## 顺手发现

① 🔴 **原版 `WarpText` 的 `isForceUpdatingMesh` 只在「走完整个循环」那条路复位**（`decomp_full/TMProWarpText__WarpText.c:110` 置 1，`:352` 置 0）；而**所有早退分支都把它留在 1**：
   `:120` `textInfo == null ⇒ return` · `:123` `characterCount == 0 ⇒ return` · `:126` `lineCount == 0 ⇒ return`（`:130` 之后的 null 支走 `goto LAB_180811750` = 抛）。
   ⇒ 原版一旦在**空文本**上跑过一次 `WarpText()`，`ReactToTextChanged` 的那道闸（`ReactToTextChanged.c:29` `*(char *)(param_1 + 0x3c) == '\0'`）**永远为假 ⇒ 再也不重弯**。
   我们 `TMProWarpText.cs:144/145` 原来每一条早退都 `_warping = false` ⇒ **行为与原版不同**（我们会在文本从空变成非空后重弯，原版不会）。
   影响面：`Shell/BoosterPackOpenWindow.cs:1931` 的 `Configure(...)` 在**设置文字之前**就调了 `WarpText()` —— 若那一刻 `characterCount == 0`，照原版就会永久不弯。
   ✅ **本波已按调度台 2026-11-05 裁定改成照抄原版**（早退不复位），并补 E1/E2/E3 三条断言钉住；卡包那条路的可见行为已在上面那节核清「与原版一致」。

② 🟡 **账/判据文件里仍写着错的订阅对象**（⛔ 本代理不动账，仅报）：
   - `资料/完全复刻/账/菜单_外壳.tsv` L661 / L662 的判据列写「先退订 `TMP_Text.OnPreRenderText`」「再订阅 `OnPreRenderText`」—— 实际是 `TMPro_EventManager.TEXT_CHANGED_EVENT`（`__OnEnable.c:19-23` / ILSpy `:66-79`）。
   - `判据/B91_Q2_无落点110条.tsv:104` 与 `判据/B93_Q3_无落点220.tsv:215` 的备注写「退订那条已挪到 `Configure()`，因为 TMP 版本差异下 `OnPreRenderText` 可能不存在」—— **前提本身是错的**（本工程 `TEXT_CHANGED_EVENT` 一直都在），本次已把它挪回 `OnDisable`。

③ 🟡 `Core/TMProWarpText.cs:39-40` 的类注释仍写着「编辑模式下 `AddComponent` **不会**触发 `Awake/OnEnable/Update` ⇒ 必须先 `Configure(...)`」—— 对**编辑器自检**成立，但**运行时** `AddComponent` 会立刻走 `Awake`+`OnEnable`（本次订阅就靠这条路生效）。不算错，只是容易误读；本波未改（不在本单范围）。

④ 🟡 `B142_E55_Assert.cs`（卡包 34 颗 `Army Text*` 的曲线/参数断言）**没有覆盖订阅**：它只断「组件挂了 + 参数逐值」。本次的 `B145_WarpText_Assert` 补上了订阅面，两者互补、无重复。
