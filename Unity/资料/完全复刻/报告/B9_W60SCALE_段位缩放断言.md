# B9_W60SCALE —— 段位窗 `Rank Icon` 缩放断言（补自检空档）+ 一处过期注释

> 代理 `B9_W60SCALE`（2026-10-27）· **执行型** · 只做两件：① 补 `ShellScene` 断言 ② 订正 `gen_anim_clips.py:30` 注释。
> 判据一手来自 `报告/B9_W53DIV_段位clip缩放修正.md` §五·1（数据侧已修好，本代理**没有重查原版、也没有再动数据**）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未越白名单（只碰 2 个源文件 + 本报告）。

---

## 一、结论

1. **空档已补上**：`Editor/ShellScene.cs` 的段位窗那一节新增一条断言，断 **`Rank Up` 播完 `Rank Icon` 的 `localScale.x = 1.0`（容忍 0.005）**。
   走的是已有自检读口 `RankedDivisionChangeWindow.NodeScaleForTest("Division Image/Rank Icon")`（`Shell/RankedDivisionChangeWindow.cs:1032`）。
   这正是「播完永久大 9.02%」能长期全绿的**唯一空档**：那段原来只断 `Division Image` 的缩放（`:9551-9552`），**从没断过 `Rank Icon`**。
2. **是灭自证**：旧数据末键 = `1.0903291702270508` ⇒ 与 1.0 差 **0.0903**，远大于 0.005 ⇒ 红。
   **把 `工具/gen_division_clips.py` 那一步 `÷ 1.0903291702270508` 撤掉**（或把两份 `division_clips.json` 的末键改回 1.0903292）⇒ 本条立刻红。
   期望值取的是**原版判据**（原版该节点静止 `localScale = 1.0903292`，已按 73.51 px 视觉框烘进几何 ⇒ 我们节点静止 = 1），**不是**从我们自己的常量反算。
3. **订正了一处会误导的过期注释**：`工具/gen_anim_clips.py:30`「（我们树上没有这两个节点）」→「（升级窗 `CardDetailPopup` 那棵树里没有这两个节点；段位窗 `RankedDivisionChangeWindow` 有，但那两条走 `division_clips.json`、不经本生成器）」。
   **零行数变化**（293 → 293，已回读确认）。
4. **账：本波不写补丁**（理由见 §二·4）：这次是「补一条自检 + 一句注释」，**不产生新的「原版类 × 方法 → 我们实现」映射**；且机检闸 `check_ledger_points.py` **只扫 `我们的落点` 列**，而全部 `Editor/ShellScene.cs:NNNN` 的提及都在**备注列** ⇒ 本次插行**零账闸漂移**（已用脚本核过，见 §二·4）。

**必跑结果**：`TMPDIR=/tmp/wf_w60 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**；`gen_anim_clips.py` 过 `py_compile`。

---

## 二、证据（文件:行号）

### 2.1 新增断言（落点 = 段位窗那一节，`winRank` 块的末尾）

| 行 | 内容 |
|---|---|
| `Editor/ShellScene.cs:9559` | 空行 |
| `:9560-9562` | 三行注释：为什么断 1.0（原版静止 1.0903292 / 已烘进 `RankIconR` 73.51 px / 数据侧 ÷1.0903292） |
| `:9563` | `var rankIconS = winRank.NodeScaleForTest("Division Image/Rank Icon");` |
| `:9564-9565` | **前提硬检查**：节点找得到 **且** `NodeScaleForTest` 返回的不是「找不到」时退回的 `(0,0,0)` |
| `:9566-9568` | **结论**：`CheckNear(rankIconS.x, 1.0f, 0.005f, …)` |

**断言三问**：

- **断什么**：`Rank Up` 播完（`TickForTest(AnimationLength + 1)` 之后）`Division Image/Rank Icon` 节点的 `localScale.x == 1.0 ± 0.005`。
- **改坏哪处会红**：
  ① 撤掉 `工具/gen_division_clips.py` 里对 `Rank Icon` path 的 `÷ 1.0903291702270508`（`_explode_scale()`）⇒ 末键回 1.0903292 ⇒ 差 0.0903 ⇒ **红**；
  ② 手动把 `Resources/division_clips.json`（或 `数据/游戏数据/division_clips.json`）`rankUp` 下 `Division Image/Rank Icon` 的末键改回 1.0903292 ⇒ **红**；
  ③ 把 `RankedDivisionChangeWindow.ApplyChannels` 的 `_rt.scales` 那条写 `localScale` 的循环删掉 ⇒ 节点停在出厂 1.0 反而会「碰巧绿」——⚠️ 这一档**本条抓不到**，由同段的 `Division Image = 1.0`（`:9551-9552`，起播 0.8、末帧 1.0）+ `Title` alpha 那几条兜（它们一起红）。
- **是灭自证吗**：**是**。旧数据的末键 1.0903292 与期望 1.0 差 0.0903 ≫ 0.005；期望值来自**原版静止缩放**（1.090329 已烘进几何），不是拿我们实现里的常量反算。

### 2.2 为什么这条会绿（数据 + 代码链，逐环核过）

1. 数据侧（**上一路已修，本代理只读核对、未改**）：`Resources/division_clips.json` 的 `rankUp` 下 `Division Image/Rank Icon` 三条 `Transform` 曲线键值 = `[1.0, 2.0429999, 0.70832, 1.0]`；`sha256 = e4d925f3d01ba2ca210bc2eae03715995dde1bab2531c136d600e5b666d4347f`，与 `数据/游戏数据/division_clips.json` 逐字节相同。
2. `RankedDivisionChangeWindow` 把它建成三条 `ScaleTrack`（`Shell/RankedDivisionChangeWindow.cs:672-675` 建 map、`:702-708` 组 `_rt.scales`；**不按节点存在性过滤**）。
3. `TickAnimation` 在 `_animT >= _animLen` 时把 `_animT` 夹到 `_animLen`（`:842-843`），先 `SampleAnimation`（`:846`）再 `ApplyChannels(_animT)`（`:847`）；
   `ApplyChannels` 的 ① 缩放段（`:863-874`）逐 path 写 `n.localScale = (x.Evaluate(t), y.Evaluate(t), z.Evaluate(t))`。
4. `_animLen = 3.9166667`（`Rank Up` length，`:9502-9503` 已断），曲线末键在 2.8667 s ⇒ `Evaluate(3.9167)` 夹到末键 **1.0**。
5. 节点确实挂得上：`BuildDivisionImage()` 把 `Division Image` 建在窗根下（`:1259-1275`），`BuildRankIcon()` 把 `Rank Icon` 建在 `_division` 下（`:1278-1293`）⇒ `NodeAt` 的 `transform.Find("Division Image").Find("Rank Icon")` 命中；`NodeForTest` 是公开读口（`:1031`），`NodeScaleForTest` 是 `:1032`。

### 2.3 前提硬检查（⛔ 不是 `if (前提) Check(结论)`）

按项目纪律「先写前提断言（硬检查），再写被测断言」写成**两条独立断言**：

- `CheckTrue(winRank.NodeForTest("Division Image/Rank Icon") != null && rankIconS != Vector3.zero, "（前提）…")`
  —— 节点找不到 / API 退化成 `(0,0,0)` 时**这条先红**（不会像 `if (x != null) Check(...)` 那样静默跳过）。
- 紧接着的 `CheckNear(...)` **无条件执行**，所以前提不成立时它也会一起红（双重兜底）。

（`Vector3` 来自文件头 `using UnityEngine;`，`:10`。）

### 2.4 账：为什么不写补丁 + 为什么插行没有闸漂移

- `工具/check_ledger_points.py:464` 只取两列：`i_status = hdr.index("状态")`、`i_land = hdr.index("我们的落点")` ⇒ **机检只看 `我们的落点` 列**。
- 用脚本扫了 `账/*.tsv` 的 **`我们的落点` 列**：**没有任何一行**引用 `ShellScene.cs`（0 命中）。
- 全部 `Editor/ShellScene.cs:NNNN` 提及都在**备注列**（共 15 处：`菜单_外壳.tsv` 13 处 + `其他_基础设施.tsv:7019` + `未分类_其他.tsv:7205`）。
  ⇒ **本次在 `ShellScene.cs` 插 10 行，机检闸零漂移。**
- ⚠️ 这 13 处备注列指针**在我动手之前就已经过期**（用 `git show HEAD:` 现读比对）：
  `:10109` 在 HEAD 上就是 `var obs = new List<string>();`，**不是**台账说的 `UIImageMaterialColorChanger` Awake 断言（后者今天在 `:10190` 那一节）。
  ⇒ **别拿「+10」去机械 `--fix` 这些备注列指针**（`--fix` 本来也不管备注列），要改只能人工重定位。
- 本次不新增「原版类 × 方法」映射 ⇒ 按 `B9_W53DIV` 的先例（`报告/B9_W53DIV_段位clip缩放修正.md` §2.4）**不写 `账/_parts/B9_W60SCALE.tsv`**：写一条没有类×方法语义的行只会制造补丁冲突/漂移。

### 2.5 `ShellScene.cs` 的漂移账（本次唯一改动 = +10 行，纯 LF 未翻）

- 文件行尾：改前 `CRLF 0 / LF 10655 / bareCR 0`；改后 `CRLF 0 / LF 10665 / bareCR 0`（**纯 LF，没被翻**）。
- 插入点在旧 `:9558` 之后 ⇒ **旧行号 > 9558 的全部 +10**。段位那一节 `:9479-9581` → **`:9479-9591`**。
- 受影响的备注列指针（旧 → 新，**都已是过期指针，仅作记录**）：

| 账文件 | 旧 | 新（+10） |
|---|---|---|
| 菜单_外壳.tsv | `:10109` | `:10119` |
| 菜单_外壳.tsv | `:10111-10115` | `:10121-10125` |
| 菜单_外壳.tsv | `:10113-10115` | `:10123-10125` |
| 菜单_外壳.tsv | `:10121-10130` | `:10131-10140` |
| 菜单_外壳.tsv | `:10131-10132` | `:10141-10142` |
| 菜单_外壳.tsv | `:10218` | `:10228` |
| 菜单_外壳.tsv | `:10220` | `:10230` |
| 菜单_外壳.tsv | `:10302` | `:10312` |
| 菜单_外壳.tsv | `:10303` | `:10313` |
| 菜单_外壳.tsv | `:10304-10306` | `:10314-10316` |
| 菜单_外壳.tsv | `:10332-10333` | `:10342-10343` |
| 菜单_外壳.tsv | `:10334-10337` | `:10344-10347` |
| 菜单_外壳.tsv | `:10338-10340` | `:10348-10350` |
| 其他_基础设施.tsv | `:7019` | `:7019`（未变，< 9558） |
| 未分类_其他.tsv | `:7205` | `:7205`（未变，< 9558） |

### 2.6 `gen_anim_clips.py:30` 注释订正（零行数变化）

- 改前：`（我们树上没有这两个节点）`
- 改后：`（升级窗 \`CardDetailPopup\` 那棵树里没有这两个节点；段位窗 \`RankedDivisionChangeWindow\` 有，但那两条走 \`division_clips.json\`、不经本生成器）`
- 事实核过（不是照搬结论）：
  · `CardDetailPopup.cs` 全文 **0 处** `"Division Image"` / `"Rank Icon"` 节点字面量（`grep` 现读）；数据侧 `Resources/*.json` 里唯一含 `Division Image` 的是 `division_clips.json`（段位窗那两份 clip），**不是**升级窗的节点表。
  · 建这两个节点的只有 `Shell/RankedDivisionChangeWindow.cs`（`:1261` / `:1280`）与 `Shell/RankedSeasonEndedWindow.cs`（`:185` / `:199`）。
- **零行数变化已复核**：改前改后都是 **293** 行（LF 293 / CRLF 0 / bareCR 0）；`:54` / `:60-62` / `:187` / `:228` / `:258-266` / `:275-283` 这些被别处报告引用的行号**没有漂移**。
- `py_compile` 通过。

---

## 三、没查清的部分（⛔ 不猜）

1. **没跑 Unity，所以没亲眼看见这条断言变绿**：`ShellScene.Run` 由调度台在同步点跑（本代理不许跑批处理）。上面的「会绿」是**读链推理**（数据末键 1.0 + `ApplyChannels` 逐帧写 `localScale` + 节点路径可解析），不是实测。
2. **`Animation.Stop()` 会不会把变换复位**：`TickAnimation` 在 `done` 时调 `_anim.Stop()`（`Shell/RankedDivisionChangeWindow.cs:851`）。若它在某个 Unity 版本下把 `localScale` 复位到 Play 前，本条读数会变成出厂 1.0 —— **恰好也是绿的**，那就失去了「抓到 9.02%」的能力。
   ⚠️ **不过这一点被同段的既有断言绑住**：`:9551-9552` 断 `Division Image` 末帧 = 1.0（**起播是 0.8**）。若 `Stop()` 真会复位，那条也会是 1.0（出厂也是 1.0 ⇒ **那条同样抓不到**）。
   ⇒ **同步点跑 `ShellScene.Run` 时，请顺带确认 `Division Image` 那条断言在当前 Unity 版本下确实能区分「起播 0.8 / 末帧 1.0」**（看合计是否如常）。若两条都属于「复位到 1.0 也能绿」的情形，需要另设计一条「播到半程读中段值」的断言 —— **本代理不擅改口径，交调度台裁**。
3. **`Rank Icon` 无图时的肉眼可观性**：`m_Sprite = 0`（服务端给）⇒ 真机只能靠子件 `Rank Icon Glow` 看缩放；本条断的是**节点缩放**，与有没有图无关（这一点与 `B9_W53DIV` §三·3 一致）。

---

## 四、改动清单

| # | 文件 | 改动 | 行数 |
|---|---|---|---|
| 1 | `d:/4/Unity/MyGame/Assets/CardPresentation/Editor/ShellScene.cs` | 段位窗那一节 `winRank` 块末尾新增 1 条断言（含 2 条前提硬检查、3 行判据注释） | 10,655 → **10,665**（+10，纯 LF） |
| 2 | `d:/4/Unity/工具/gen_anim_clips.py:30` | 注释订正（`我们树上没有` → `升级窗 CardDetailPopup 那棵树里没有` + 段位窗走 `division_clips.json` 的说明） | **293 → 293**（零变化） |
| 3 | `d:/4/Unity/资料/完全复刻/报告/B9_W60SCALE_段位缩放断言.md` | 本报告 | 新建 |

⛔ 没有动：`Resources/division_clips.json` / `数据/游戏数据/division_clips.json`（上一路已修，本代理只读核对）· `Shell/RankedDivisionChangeWindow.cs`（只读它的 API 与动画链）· 任何 `.unity` / `ProjectSettings` · 任何别的代理正在写的文件。

---

## 五、顺手发现（⛔ 本代理没改，交调度台分流）

1. **`Shell/CardDetailPopup.cs:2826` 也是一条过期注释**（不在本代理白名单 ⇒ 只报不改）：
   现文「🔴 **3 条 clip（`Level Up Card` / `Rank Up` / `Division Up`）还没导进工程**（导它要 Unity ⇒ 不在本代理范围）⇒ 现在 `myAnimation == null`」。
   **实际三者都已在工程里**：`Assets/CardPresentation/Resources/Animations/{Level Up Card,Rank Up,Division Up}.anim`（含 `.meta`，2026-10-27 18:57 生成，`B9_W37CLIP` 改的输出目录）。
   ⚠️ 但**「`myAnimation == null`」那半句要另行核实**：`B9_W53DIV` §六·4 说这三份 `.anim` 是**死资产**（段位窗走 `division_clips.json`；`Level Up Card` 才是被 `Resources.Load` 取的那一条），所以「加载点取不取得到」与「文件在不在」是两件事 —— 建议改注释时**两件一起写清**，别只把「还没导进工程」改成「已导入」。
2. **账备注列里的 13 个 `Editor/ShellScene.cs:NNNN` 指针早已过期**（见 §2.4）。它们不是闸扫的列，但会误导人；要修只能人工按**符号名**重定位，⛔ 别按「+10」机械平移。
3. **本段缺「中段值」断言**（见 §三·2）：现有 `Division Image` / 本次新增 `Rank Icon` 两条都只断**末帧**。若某天有人让 `Animation.Stop()` 复位变换，两条会同时变成「永远绿」。建议同步点确认后，考虑补一条「播到 2.6 s（峰值）读 2.043」的断言 —— 那是**结构上不可能与末帧同时满足**的位置（2.043 ≠ 1.0）。

---

### 交接给下一个代理（若本波被截断）

- 已完成：断言（`ShellScene.cs:9559-9568`）+ 注释（`gen_anim_clips.py:30`）+ typecheck 0/0 + py_compile + 本报告。
- 唯一遗留的「我自己该做完但做不了」：**没有**（跑 `ShellScene.Run` 是调度台同步点的动作；§五 三条都在别人的白名单里）。
- 复跑命令：`TMPDIR=/tmp/wf_w60 bash d:/4/Unity/工具/typecheck.sh`（应 0/0）· `python -I -X utf8 -m py_compile d:/4/Unity/工具/gen_anim_clips.py`。
