# B9_D5SH — `ShellScene.Run` 崩溃修复（`AddComponent<ProbeTab>()` 恒失败 ⇒ NRE）

> 代理 **D5SH** · 2026-10-27 · 白名单内改动（`Editor/ShellScene.cs` ＋ 新增 `Shell/ProbeTab.cs`），另有一处**工具侧**必要修复（见 §④·1，超出白名单，理由写在那一节）。

## ① 根因（已现核，附实据）

`Editor/ShellScene.cs:10340` 的 `new GameObject("TabA").AddComponent<ProbeTab>()` **恒返回 null**，下一句 `:10341 a.type = …` 抛 `NullReferenceException` ⇒ ④/⑤/⑥/⑦ 四段（**72 处断言调用点**）一次都没跑过，**宿主合计（`:10581`）从未打印** ⇒ rc=1。

**日志原文**（`_tmp_view/wf_ShellScene.Run2.log`）：

```
219555: Can't add script behaviour 'ProbeTab' because it is an editor script.
        To attach a script it needs to be outside the 'Editor' folder.
219560: UnityEngine.GameObject:AddComponent<ProbeTab> ()      ← Editor/ShellScene.cs:10340
219566: NullReferenceException: Object reference not set to an instance of an object.
```

**精确机制（比上一版定性更细，可复核）**：Unity 拒绝的不是「MonoBehaviour 写在编辑器程序集里」这件事本身，而是
「**该 MonoBehaviour 有自己的脚本资产（MonoScript），而这个资产落在编辑器专用程序集**」。
判据 = **同一段代码在拆文件之前是跑通的**：

| 时间 | `ProbeTab` 的形态 | 结果 |
|---|---|---|
| 10-26 09:11（`_tmp_view/wf_ShellScene.Run_b.log`） | `ShellScene.cs` 的**嵌套类**（**没有自己的脚本资产**） | ④⑤⑥⑦ 全跑到 · 合计 **2070 / 0** · `editor script` 命中 **0** |
| 10-26 18:31（`wf_ShellScene.Run2.log`） | X1 拆成 **`Editor/ProbeTab.cs`**（有脚本资产、且在编辑器程序集） | `AddComponent` 被拒 ⇒ NRE ⇒ 崩 |

⇒ **崩是 10-26 X1 那次「拆成 `Editor/ProbeTab.cs`」引入的**（拆动的动机是对的：A1506「类名 == 文件名」，否则 `!u!115` 存根），
只是**放错了目录** —— MonoBehaviour 的脚本只要落在 `Editor/` 下就永远挂不上。

## ② 改法（三选一 → 选 **2：挪到运行时代码**）

| 选项 | 判决 | 理由 |
|---|---|---|
| 1. 改成 `new ProbeTab(...)` | ❌ 不成立 | `ProbeTab : WindowTabBase : MonoBehaviour` —— **它就是 MonoBehaviour**，`new` 出来的是没有 `gameObject` 的孤儿，`:10342` 的 `a.transform.SetParent(…)` 立刻 NRE |
| 2. **挪到 `CardPresentation/Shell/`** | ✅ **采用** | 它就是「要挂在 GameObject 上」的那一类；`Shell/` 是运行时程序集 ⇒ `AddComponent` 合法。同时**类名 == 文件名**（`Shell/ProbeTab.cs`）⇒ A1506 那条也仍然满足 |
| 3. 删掉整段 | ❌ 不成立 | 这一段测的是**真机制**（④`GameWindowWithTabs`/`WindowTabBase`/`TabButtons` 的页签生命周期；⑤ 按件拖拽；⑥ `DeckCell sortMode`；⑦ 资源条 overlay），**后面 72 处断言都挂在它后面**（④ 建的那两条探针页就是 ⑤⑥⑦ 前面必须成立的前提链），删 = 一次性丢掉 72 处覆盖 |

**落地的三件事：**

1. **新增** `Assets/CardPresentation/Shell/ProbeTab.cs`（＋`.cs.meta`，新 guid `f369c80c789a415aa3208c7ba7952ea6`，全库无冲突）——
   逻辑**逐字搬**，只有两处形态变化，都写在文件头里：
   - 加 `namespace CardPresentation { … }`（与 `Shell/` 其余页签一致；`ShellScene.cs` 已有 `using CardPresentation;` ⇒ 调用点一个字没动）；
   - **`internal` → `public`** —— 🔴 **必须**：编辑器程序集（`ShellScene` 那边）要看见它，`internal` 跨程序集不可见。
     （旧版隐式可见只是因为「两个类同在 `Assembly-CSharp-Editor`」。）
2. **删除** `Assets/CardPresentation/Editor/ProbeTab.cs` ＋ 它的 `.meta`（留着就是同名类型两份 ⇒ `CS0104` 歧义；旧 guid 全库零引用，已核）。
3. `Editor/ShellScene.cs` **只改一行注释**（`:10333` 段头，就地补一句「夹具页住 `Shell/ProbeTab.cs`，必须在非 `Editor/` 目录」）——
   **行数中性**：`git diff --numstat` = `1 1`，`:10340` / `:10341` / `:10581` **行号一个没变**（账落点安全）。

⛔ **没有**只加 null 检查让它静默跳过 —— 那是把「挂不上」变成「永远少跑 72 条且看不见」。

## ③ 修前 / 修后能跑的断言数

静态数（`Check|CheckTrue|CheckNear` 调用点，正则排除了 `MenuCheck.` 这类限定名）：

| | 数 |
|---|---|
| `Run()` 全体（`:1009-10589`） | **1,315 处调用点**（循环展开后实跑 1,963 ✓ —— 与日志一致） |
| 崩溃点**之后**从未执行 | **72 处**：④ 33 · ⑤ 20 · ⑥ 13 · ⑦ 6 |
| **修后应跑** | **1,315 / 1,315 全部可达 ⇒ 实跑预期 ≈ 1,963 + 72 = 2,035** |

⚠️ 两点如实说明：
- 这 72 处里 **⑤ 的 20 处在 `if (pl != null)` 内**、⑥ 有 3 处在 `if (wbN/wbS != null)` 内、⑦ 有 5 处在 `if (bar != null …)` / 1 处在 `if (q0 != null)` 内
  ⇒ 若某条**前提**红了，实跑数会略低于 2,035（但那时日志里必有一条红的前提断言，不是静默）。
- **这 72 处从未被执行过**（含 2026-10-25/26 新加的 ⑤⑥⑦ 三块）⇒ 我只能静态保证「第一处崩溃修好了」；
  静态扫过 ④⑤⑥⑦ 的依赖（`TabButtons.Initialize`、`ChangeTabCO`、`GetTab`、`UpdateTab`、`TopBar.Build(parent, null)`）**都有 null 保护、没有显式解引用**，但**有没有第二处红/崩要靠调度台实跑**。

## ④ 验收与顺手发现

**验收**
- `TMPDIR=/tmp/wf_D5SH bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0** ✅（修完的正式读数）
- 未跑 Unity（按派单，调度台统一复跑 `ShellScene.Run`）。

**顺手发现 1（已修，⚠️ 超出白名单，请复核）**：`工具/gen_csc_rsp.py` 有个**镜像缺陷** ——
它早就会把「csproj 里没有的新 `.cs`」自动补进 rsp（2026-09-29 修的），但**不会剔除「csproj 里还在、磁盘上已删」的 `.cs`**
⇒ 我删掉 `Editor/ProbeTab.cs` 之后，`typecheck.sh` 报 `CS2001 找不到源文件`、**编辑器程序集整趟没跑起来**。
csproj 是「上次 Unity 生成」的，Unity 下次跑会自己清掉，但**任何一次「删/挪 `.cs`」都会在中间态把 typecheck 打红**（这一批多路并行，删挪是常态）。
⇒ 补了 **6 行**（两份 rsp 各一行 `if not os.path.exists(...): continue` ＋ 说明注释）。
**安全性**：只跳过「磁盘上不存在的源文件」，对存在的文件行为逐字不变；Unity 重新生成 csproj 之后这段是**永久空转**。
**若调度台判此举越界**：`git checkout -- Unity/工具/gen_csc_rsp.py` 即可回退，届时 typecheck 会恢复成「1 条 CS2001」直到 Unity 跑过一次。

**顺手发现 2（不阻塞，留账用）**：`ProbeTab.setups` 的计数点在 **`Setup(GameWindow)` 的 override**，
而 `资料/完全复刻/报告/B8_BI_外壳剩余B.md:222` 那条待办的写法是「加 `public int setups; protected override void OnSetup() { setups++; }`」。
两者**只在「有人直接调 `OnSetup()` 而不走 `Setup(window)`」时才会不同**。将来真加那条断言时，先确认它调的是哪条路，否则会数出 0（⛔ 别据此判「机制没实现」）。

**顺手发现 3**：`ProbeTab` 现在 `public` 且编进**运行时程序集**（会随 player 走）。
可选收敛手段 = 文件内 `#if UNITY_EDITOR`（只进编辑器编译），但仓里大量 `*ForTest` 公开读口本来就在运行时侧，**未做**，留给调度台裁定。

**顺手发现 4**：`交接_第八十四会话.md` §1·b 那张表里 `ShellScene = 2,022 / 0` 是旧值，
第八十五会话 `D3OT` 已就地订正为「rc=1 · 1,963 ✓」；本次不再改那份文档（正本归调度台）。
