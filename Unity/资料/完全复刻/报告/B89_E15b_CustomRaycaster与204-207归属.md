# B89-E15b · 行 230 `CustomRaycaster` 审计 + 行 204–207 归属核实

会话：第八十九会话 · 波 1 执行代理 **E15**（续派）· 2026-10-28
账补丁：`资料/完全复刻/账/_parts/B89_E15b.tsv`（**5 行**：行 230 → **A** · 行 204/205/206/207 → **D**）
`typecheck`：**运行时 0 / 0 · 编辑器 0 / 0**（`TMPDIR=/tmp/wf_B89E15b`，本批跑；顺带把 E15 那批的 0/0 也补上了）
⛔ 没跑 Unity / ruleprobe · 没动 git · 没改正本 · 没碰任何人的白名单（**本批一行 `.cs` 都没改**，见下）

---

## §0 结论（两句话）

1. **行 230 的活已经做完了**（2026-10-25 / 10-27 就落地了，判据逐句照抄），账里那句「我们的教程层没有这条点击过滤」**已过期三天** ⇒ 本批**不建任何新件**，做的是**逐条审计 + 判 A + 把证据写进账**。
2. **行 204–207 确属 `Countdown`**（按类桩成员表核实，不是按账里的归属信）⇒ 与 208 同一结论：**该类在本 build 0 实例 / 0 引用点** ⇒ 四行**各自**复写三条证据判 D，并**订正一条旧落点错误**（它们原来指向 `TimerDisplay`，那是**另一个原版类**）。

---

## §1 逐条

### ① 行 230 `CustomRaycaster.Raycast` → **A**

| 项 | 内容 |
|---|---|
| 原版判据 | `CustomRaycaster.cs:7-11` = `base.Raycast(…)` 后 `RaycastFilter.FilterRaycastResults(resultAppendList)`；`RaycastFilter.cs:115-161` 谓词（**为真 ⇒ 剔除**）：① `allowedObjects.Count>0 && !Contains(r.gameObject)` ② `filteredType!=null && GetComponent(filteredType)==null`；`RaycastFilter.cs:163-220` 注册 = 目标 + 全子树；消费者 `TutorialManager.cs:1371-1397`（`filterRaycast` 真 ⇒ 先 `Clear` 再 `RegisterObjectAndChildrenGraphics`）；三个 `filterRaycast:true` 调用点 = `TutorialStageClaimForgeFirstLevel.cs:299` / `TutorialStageGetCampaignNode.cs:361` / `TutorialStageGetMission.cs:246` |
| 我们的实现（**现读逐处核过**） | ① `Shell/RaycastFilter.cs:53` 整类：2 个静态字段（`filteredType` / `allowedObjects`）+ 7 个方法，**方法名与顺序与原版一致**，谓词 `Allows` 逐句照抄；② 命中入口 `Shell/PointerLayer.cs:1580` —— 在 `CollectHits` 的候选表**进入排序/取赢家之前**剪（= 原版 `RemoveAll` 的等价位置，剪晚一步会出现「被禁的件先赢下 `best`、再被跳过 = 什么都不响」）；③ 注册 `Shell/MenuTutorialFlow.cs:765`，由 `:1037 FiltersRaycast(stage, kind)` 那张**按「档」不按「阶段」**的表控制（逐调用点实读，注释里把三个 stage 里哪一档带 `filterRaycast:true` 写全了）；④ 清空 `:684`（`RefreshPointer` **第一句** —— 于是「简报盖住 / 目标不在 / `def==null`」那些 early return 自动落回「表空 = 不过滤」）+ `:542`（换场景）+ `:474`（链停掉）+ `:479`（`_menu` 假 null）+ `Shell/TutorialMessagePopup.cs:330`（简报 Continue） |
| 状态 | **A** |
| 观测口 | `RaycastFilter.Active`（现在有没有在过滤）· `RaycastFilter.AllowedCount`（表里几条）· `RaycastFilter.FilteredTypeForTest`（应恒 `null`，那一半是死码）· 行为口 = 教程三档下点「允许表外的件」应**无反应**、点表里的件应正常 |

**审计结论：与判据一致，无需改代码。** 两处「故意的差异」都有据、都写在文件里：
- `AllowsHit(button, hitGraphic)` 传**两个** GameObject 取 `||` —— 理由 = 我们的候选表里一颗 `WindowButton` 的命中区是**它子树里那颗 `ImageQuad`**，与按钮根常常不是同一个 GO；而 `RegisterObjectAndChildrenGraphics` 本来就登记「目标 + 全子树」⇒ 两颗一定都在表里 ⇒ **等价**（文件里有反证段落：「若只判 `button`，教程目标 = 按钮内某颗图时会判不中 ⇒ 软卡死」）。
- `SetRaycastFilter<T>()` 按**死码**保留（原版全库 0 调用点 ⇒ 出货 `filteredType` 恒 `null`）。

⚠️ **两条「拿不准时选漏」的判断是对的**（文件里写着）：`FiltersRaycast` 表故意只覆盖「某一档目标」而不是整个阶段 —— 漏过滤只是回到 #230 之前的行为，**多过滤会软卡死**（玩家点不动任何东西且无报错）。这条口径我复核后**同意**，账里的判据要点也照这个写。

### ② 行 204 / 205 / 206 / 207 → **D**（归属先核，核完才判）

**第一步：归属核实（⛔ 没信账里那列，按类桩现读）**
`d:/2/Warpforge_code/Scripts/Assembly-CSharp/Countdown.cs` 现读：
```
public class Countdown : MonoBehaviour
{
    private TextMesh timerText; private DateTime lastStop;
    private TimeSpan timeElapsedBeforeStop; private TimeSpan speedAdjustedTimeElapsed;
    private bool running; private float speedFactor; private TimeSpan timeAvailable;
    private bool soundPlayedFlag;
    public void Activate(float speed, int secondsDuration) { }
    public void ChangeSpeed(float newSpeed) { }
    public void Stop() { }
    public double GetSecondsLeft() { return 0.0; }
    private void Update() { }
}
```
⇒ 账里 204–207 那四个方法名（`Activate` / `ChangeSpeed` / `GetSecondsLeft` / `Stop`）**恰好 = 本类的全部公开成员** ⇒ **方法与类对得上**，不存在「记错类」的问题。

**真正错的是「落点」**：这 4 行原来写的落点 `CardPresentation/Shell/TimerDisplay.cs` 是**另一个原版类** `TimerDisplay` 的等价件。两个类**功能不同**：`Countdown` 有 `speedFactor` 变速累加 + `soundPlayedFlag`（剩余 <5s 播一声），`TimerDisplay` 只有「剩余 → 文案」。⇒ 那一列**不是等价物**，判 D 时把落点改成「无」，并在差异列写清这条。

**第二步：三条证据（每行都复写，⛔ 不写「同 208」）**

| # | 证据 | 读数 |
|---|---|---|
| ① 实例面 | `d:/2/tools/assetripper_out/ExportedProject` 里按 `Assets/Scripts/Assembly-CSharp/Countdown.cs.meta` 的 guid **`e9a9437d60f6224c42c38befbab6b787`** 全量 grep `*.prefab` + `*.unity` + `*.asset` | **0 命中** |
| ② 运行时面 | `d:/4/Unity/资料/说明书/**/*.md`（各战场的运行时全树 dump + 菜单全树）搜 `Countdown` | **0 命中** |
| ③ 引用面 | `d:/2/tools/cpp2il/out_cpp2il_dev_ilspy` 全树找**对本类的引用** | **0 处**。搜到的 `ChooseCardMenu.cs:797 CountdownTrigger` / `ClockManager.cs:52·171·259·297… clockCountdownFlag·clockCountdownSec` / `BattleManager._*FallbackCountdown` / `MulliganCountdown` 全是**同名不同物** —— 本类那 5 个成员在别处**一次也没被调用** |

⇒ 铁律 11 例外①「原版本身没有」⇒ **D**。
⚠️ **证据边界（如实写）**：AssetRipper 导出集若不全，① 要复核；但 ② 是**另一条独立源**（运行时实况 dump），两条都 0 命中 ⇒ 结论站得住。**回来条件** = 若将来在远端包 / 新解包资源里发现 `Countdown` 的实例或调用点，这 5 行（含 208）一并翻回来。

---

## §2 `typecheck` 读数（本批首次拿到 0/0）

```
TMPDIR=/tmp/wf_B89E15b bash 工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
🔴 **这条读数的第二个用处**：它把 E15 那批（`Shell/LiveOpsHandler.cs` 改了 ~70 行）一并**正式确认**为 0/0 —— E15 交回时三次都被别人的半成品挡着（`BattleContext.cs` → `TMProWarpText.cs` + `BaseEventPopup.cs`），现在那些都修好了。
✅ 本批**没有改任何 `.cs`**（230 是审计，204–207 是判 D）⇒ 无行号漂移、无新落点。

---

## §3 需要 **Editor 宿主写手**落的 3 条断言（本代理白名单外，⛔ 没动）

`grep -rn "RaycastFilter" Editor/*.cs` ⇒ **0 命中** ⇒ 行 230 这条链**目前没有任何断言盯着**。三条建议（都只用现有只读口，逻辑可完全脱离 Unity 跑）：

1. **死码口**：`Check(RaycastFilter.FilteredTypeForTest == null, "RaycastFilter.filteredType 恒 null（原版 SetRaycastFilter 全库 0 调用点）")`
   —— 防「有人给它编一个调用点」把过滤偷偷改成按类型生效。
2. **空表 = 不过滤**：`RaycastFilter.ClearRaycastFilters(); Check(RaycastFilter.AllowsAnyForTest(任意GO), "空表 ⇒ 一律放行")`
   —— 这条是**防软卡死**的那条（空表实现成「全禁」会让整层菜单点不动、且零报错）。
   ⚠️ 需要一个新读口 `AllowsAnyForTest(GameObject)`（或直接用已有的 `Allows`：`Check(RaycastFilter.Allows(someGo))`）—— **用 `Allows` 就够，不必加新成员**。
3. **注册/清空成对**：走一次 `MenuTutorialFlow` 的 `ClaimForgeFirstLevel` 那一档 ⇒ `Check(RaycastFilter.AllowedCount > 0)`；然后 `FinishAll()` ⇒ `Check(RaycastFilter.AllowedCount == 0 && !RaycastFilter.Active)`。
   —— 这条盯的是「教程结束后允许表还留着 ⇒ 玩家从此点不动别处」那个**静默软锁**。

---

## §4 没做的 / 需要别人接的

| 项 | 归属 |
|---|---|
| 上面 3 条断言 | `Editor/` 宿主写手（`Editor/MainMenuScene.cs` 教程段或 `Editor/ShellScene.cs` 任一处能建 `MenuTutorialFlow` 的宿主） |
| `Countdown` 五行的最终翻案条件 | 若将来在远端包 / 新解包资源里出现该类实例 ⇒ 5 行一并翻回 |

---

## §5 交接

1. **本域（`未分类_其他`）本轮的账面变化**：E15 做掉 4 条 A（行 3/4/5/6）· 判 D 1 条（208）· 本批判 A 1 条（230，**零代码改动，纯账订正**）· 判 D 4 条（204–207，原本是 B）⇒ **C 行 37 → 31**（−4 A −1 D −1 A）· **B 行 93 → 89**（−4 D）· 待办总数 **130 → 120**。
2. 🔴 **给调度台的方法论提醒**：行 230 是「**账说没做、实际三天前就做完了**」的典型 —— 这类**账过期**会**低估**真复刻率。建议在重同步点让某个只读代理做一次「**落点文件里 grep 一遍账里说『缺』的关键词**」的抽检（例如对判词含「没有 / 未建 / 缺」的行，去它自己的落点文件里搜同名符号），成本低、能捞出这一整类。
3. **下一步在本域能做的**（E15 报告 §4 已逐条列过）：真正的「判据齐 + 无写手」只剩 **706/708**（需配一次 Unity 截图）· **301**（等远端口径）· **41**（等轮抽联盟页那棵树）。其余 6 条等写手 / 6 条等远端 / 1 条等图导入 / 4 条归战场线。

**踩过的坑（给下一个代理）**
- ⛔ **别看到账里写「缺」就直接开工** —— 行 230 的落点文件（`RaycastFilter.cs` / `PointerLayer.cs` / `MenuTutorialFlow.cs`）都已经写好了，照账开工等于**重写一遍别人的实现**。
- 判「某类在本 build 有没有实例」用 `.cs.meta` 的 **guid** 去 grep 导出集（类名搜不到，因为序列化里只有 guid）；再配一条**运行时全树 dump** 做独立第二源。
- 判 D 之前**先核类归属**（按类桩成员表现读），⛔ 别信账里「原版类」那一列；**「方法对得上」不等于「落点对得上」** —— 行 204–207 就是方法与类都对、但落点指到了另一个原版类。
