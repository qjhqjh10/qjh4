# F4 · 设置窗自检 7 条红（`settings.log` 344 / 7）—— **夹具回归**（分类 (a)）

> 写手：批次 1 · F4。工作目录 `d:/4/Unity`。**只改了一个白名单 `.cs`**（`Editor/SettingsScene.cs`）+ 本报告。
> ⛔ 没跑 Unity（自检由调度台在同步点跑）、没动 git、没改两张正本 / `资料/待办判据_*.md` / `资料/已知的坑.md`。
> 改动行尾 = **LF**（改后 `CRLF 0 / LF 2188`，与改动前一致）；`git diff --numstat` 里本文件是 `+…/−…`（**没有整篇重写**）。

---

## 一、结论

1. **分类 = (a) 夹具回归，实现是对的**：`SettingsScene` 把 `Tab Buttons` 的 `Transform` **存过了 `Build()` 重建**。
   A176 那一段的 `win.Close()` + 两次 `win.Manager.OpenWindow(win)` 会重跑 `SettingsWindow.Build()`
   （`Shell/SettingsWindow.cs:478-482`：第一句就把窗根的**全部子件销毁重建**；非 Play 下 `DestroySafe`
   直调 `DestroyImmediate` ⇒ 旧引用**当场**变假 null）。存下来的 `bar` 因此成了**假空**
   ⇒ `FindChild(bar, "Audio"/"Online")` 恒 `null` ⇒ **音频页 / 联机页此后一次都没切过去**
   ⇒ 5 条「没有 active 的 `ImageQuad`」是**同一个后果**。
2. **已修**：新增 `Area(root)` / `Bar(root)` 两个**现取**助手（`Editor/SettingsScene.cs:360-361`），
   文件里 `area` / `bar` 两个局部变量**整个删掉**（**16 处调用点**全改现取：`bar` 7 处 · `area` 9 处）。
   ✅ **`Run()` 里不再存在任何「在重建之前抓、之后还用」的 `Transform`**（全量扫过一遍，见 §三·3）。
3. **断言条数不减**：只动了夹具与两处失败文案，**一条断言都没删**；A176 那几条
   （③★ `:1247` / ④★★ `:1253` / ⑤★ `:1264` / ⑥★ `:1277`）**一字未动**（那一段没碰）。
4. **那条「未闭合的环」已查清 —— 两页确实没活过**，三个「矛盾」现象由一条此前没定死的 Unity 语义解释：
   **默认重载 `GetComponentInChildren<T>()` 不查祖先链**（祖先关着，照样能取到自己开着的子孙；
   `activeInHierarchy` 才查祖先 —— 而 `RectOf` 用的正是它）。判据见 §四，**不是猜测**
   （证死的是「祖先不活不影响」这一半；另一半按本仓既有观察接受，口径写在 §四·3 的 💬 里）。
5. **判断：修完 `bar` 之后那 5 条会一起转绿**（逐条理由 → §四·4）。若还有红的，那就**不再是夹具问题**，
   而是 A208 那半边的真缺陷（判据 → §五·1）。

---

## 二、证据（文件:行号）

**⑦ 条红的分布**（日志 = `d:/4/_tmp_view/settings.log`，`===== 通过 344 · 失败 7 =====` @`:6200`）：

| # | 日志行（`settings.log`） | 断言 | 调用点（**那一版**的 `SettingsScene.cs` 行号 = 日志 stack 里的） |
| --- | --- | --- | --- |
| 1 | `:4197` | ``✗ 点击区 `?` 不在（或没接 onClick）`` | `:1444`（音频页 `Click(FindChild(bar, "Audio"))`） |
| 2 | `:5000` | 同上 | `:1717`（联机页 `Click(FindChild(bar, "Online"))`） |
| 3 | `:5065` | `✗ ★（A208）IP 输入框**渲出来的矩形**…（没有 active 的 ImageQuad）` | `:1740` |
| 4 | `:5079` | `✗ ★（A208）密码输入框渲出来…` | `:1745` |
| 5 | `:5106` | `✗ ★（A208）输入框左沿 = 同列标签左沿（框 3.40e38 vs 标签 632.87）` | `:1762` |
| 6 | `:5411` | `✗ 【Test Public IP】落在 Save/Check 右边那片空位上（没有 active 的 ImageQuad）` | `:1824` |
| 7 | `:5503` | ``✗（A208）客机块那个 `IP Field`…（没有 active 的 ImageQuad）`` | `:1846` |

**根因链**（每一环都有出处）：
1. `Editor/SettingsScene.cs` A176 段：`win.Close();` + `win.Manager.OpenWindow(win);`（**两次**）
   —— 改后的行号 `:1251` / `:1256` / `:1262` / `:1268`（那一版是 `:1188` / `:1193` / `:1199` / `:1205`；
   `git show HEAD:…SettingsScene.cs` 里这四行**都不存在** ⇒ 本批新加）。
2. `WindowsManager.OpenWindow` → `GameWindow.TryOpen` → `SettingsWindow.Open()`
   （`Shell/SettingsWindow.cs:452-458`：`Build()` + `OpenTab(Current)`）→ `Build()`：
   `for (int i = root.childCount - 1; i >= 0; i--) RewardsWindow.DestroySafe(root.GetChild(i).gameObject);`
   （`:482`）；`DestroySafe` 非 Play 下 = `DestroyImmediate`（同族两处实现：`Shell/MenuWindowBase.cs:155-161`、`Shell/MainMenuRuntime.cs:191-197`）。
3. 夹具在 `:524`（那一版）**只抓了一次** `var bar = FindChild(area, "Tab Buttons");`
   ⇒ 重建后 `bar` 假空 ⇒ `FindChild` 的 `parent == null` 守卫（`:341`）把它静静变成 `null`
   ⇒ `Click(null)` ⇒ **第 1、2 条红**。
4. **日志实证**：`[Settings] 切到 \`X\` 页` 只有 `OpenTab` 会打（`Shell/SettingsWindow.cs:597`），
   而 `OpenTab` 是 `Current`（`:581`）与三页 `activeSelf`（`:583`）的**唯一写者**。
   本轮 8 条切换中最后 3 条全是 `Graphics`（`:3498` / `:3630` = 两次重开；`:5978` = A94 收尾重开）
   ⇒ **重建之后音频页 / 联机页没再活过**。
5. `RectOf`（`Editor/SettingsScene.cs`）要求 `q.gameObject.activeInHierarchy`
   ⇒ 联机页整棵不活 ⇒ **第 3～7 条红**。
   （对照：同一份日志里 `Media Tab` 也不活，但音频页那一片量的是 `TrackWorldH` / `WorldPos`，
   走 `GetComponentsInChildren<ImageQuad>(true)` 与组件字段 ⇒ **不要求 active**，所以它们照绿。）

**绿色对照**（`d:/4/_tmp_view/final_SettingsScene.log` = 上一版全绿跑，`===== 通过 290 · 失败 0 =====` @`:5152`）：
- `:3353` `切到 Audio 页`（音频页那一段真的切过去了）· `:4156` / `:4970` `切到 Online 页`；
- `:4393` `✓ 【Test Public IP】落在 Save/Check 右边那片空位上 渲出来 = 270.0×72.0 @(1073.9,639.0)`（= 第 6 条红在那一版是**绿**的）。

---

## 三、改动清单（`Editor/SettingsScene.cs`，共 1 个文件）

1. **新增两个现取助手**（放在 `FindChild` 之后，`:347-361`）：
   ```csharp
   static Transform Area(Transform root) { return FindChild(root, "Menu Area"); }
   static Transform Bar(Transform root)  { return FindChild(Area(root), "Tab Buttons"); }
   ```
   带一段注释写明：**本窗的根子件会被 `Build()` 整棵销毁重建**、踩过的代价（7 条红）、
   **唯一该缓存的是 `root`**（窗根自身，`Build()` 不销毁它）。
2. **删掉两个局部变量、**16 处**调用点改现取**：
   - `bar` → `Bar(root)`：`:583`（`CheckAtS`）· `:588` · `:595`（`General`/`Account` 不建）·
     `:615`（切页循环）· `:633`（图像页）· `:1507`（音频页）· `:1780`（联机页）—— 共 7 处；
   - `area` → `Area(root)`：`:522` / `:524` / `:526` / `:528` / `:530` / `:532`（几何片）· `:571` / `:574`（A131 锚）· `:2064`（关窗钮）—— 共 9 处；
   - **顺带删掉** `:1878`（那一版）的 `area = FindChild(root, "Menu Area");`（A94 收尾后的「重抓一次」）
     —— 改现取之后它是多余的；那条**知识照旧留着**（注释改成：重开 = 整棵树换新，
     `FindChild(旧树, …)` 会静静拿到 `null`（那是假红），现在一律 `Area(root)` / `Bar(root)`）。
3. **顺手核过一遍（任务要求 ① 的后半句）—— 重建前抓、之后还用的 `Transform` 只有这两个**：
   把 `Run()` 里在第一次重建（`win.Close()`）之前声明的**全部**局部变量跑了一遍「之后还有没有用」，
   命中的只有 `bar` / `area`（已修）+ 这几类**不受影响**的：`win`（窗口组件，`Build()` 不换它）、
   `root`（窗根，只销毁它的子件）、`urp`（URP 资产对象）、`ssQuality`（`int`，每次用前重读）、
   `gsc3` / `ssOff` / `ssOn` / `ssBox` / `gTab3`（全部在重建**之前**用完）、
   `gTab4` / `ssOn2` / `ssAfter`（重建**之后**才抓，本来就是对的那半边）。
   ⇒ **本次之后，本文件没有任何跨重建的缓存**。
4. **`Click` 的失败文案拆细（任务的可选项 ③，做了）**：
   - `Click(Transform t)`（`:367-387`）：`t == null` 与「取到了但没挂 `WindowButton`」**分开报**；
     前者明写两种可能（**没建** / **手里是重建前的旧树**）并指向新重载。**行为一字未改**
     （仍是「取到就 `onClick()`、取不到记一条失败」）。
   - **新增** `Click(Transform parent, string name)`（`:389-410`）：父 `== null` ⇒ 报「**父节点是 null**（现取的父 ⇒ 父自己没建；存下来的旧引用 ⇒ 重建后的假 null）」；
     父是活的而名字找不到 ⇒ 报「**真的没建**，不是旧树」。**这一条才是能判出是哪一种的**。
   - 把 11 处调用点改成这个重载（父都是**恒活**的节点）：`Bar(root)` ×7 · `win.HostBlock`（Refresh ×3）·
     `win.ClientBlock`（Check Button）· `FindChild(root,"Online Tab")`（Role Client）·
     `FindChild(root,"Graphics Tab")`（QualityHit ×2）· `FindChild(…,"VSync")` · `FindChild(Area(root),"Generic Close Button")`（Hit）。
   - ⛔ 别的断言的文案**没动**（全仓 grep 过旧文案 `没接 onClick`：只有本文件与那份诊断/日志里有）。
5. **改坏法（一句话）**：把 `Bar(root)` 换回一个**在 `Run()` 开头抓一次的局部变量**
   （或把 `Click(Bar(root), "Audio")` 写回 `Click(旧引用, "Audio")`）⇒ 音频页不切 ⇒ 7 条红全回来。
   更小的一刀：只把 `:1507` 那一句退回缓存 ⇒ 2 条点击红 + 3 条 A208 红（联机页那 2 条要看有没有一起退）。

---

## 四、「未闭合的环」—— 正面回答（**不拿猜测填空**）

诊断自己写明的那一环是：**既然 Online 页不活，为什么同段里
`Click(FindChild(win.HostBlock,"Refresh"))` 与 `ipLb = …GetComponentInChildren<Label>()` 都成功？**
（默认重载「不含未激活」是与 `Editor/CollectionScene.cs:3970`、`Editor/MainMenuScene.cs:3313` / `:3830`
那几条本仓注释**打架**的。）

### 1) 那两页到底活没活 / 什么时候活的
**没活过**（指重建之后）。时间线（全部有日志出处）：
- 开局 `Open()` → Graphics；**切页循环**把三页各切一次（`:1058` Graphics · `:1164` Audio · `:1270` Online）
  ⇒ 那一段里三页**都活过**（`OpenTab` 的 `SetActive` 是唯一的开关）。
- `:1388` 图像页那一段切回 Graphics。
- A176 段两次 `Close()` + `OpenWindow()` ⇒ `Build()` + `OpenTab(Current=Graphics)`（`:3498` / `:3630`）。
- 此后直到收工：**没有任何一次切换成功** ⇒ `Media Tab` / `Online Tab` 一直是 `activeSelf == false`。

### 2) 判据：怎么知道「Online Tab 不活」不是猜的
- `RectOf` 返回 false 的**唯一**条件 = 该节点子树里**没有一个是 `activeInHierarchy`** 的 `ImageQuad`。
- 而 IP 框那几颗 quad 造出来时**全是活的**：`MenuDraw.Node` = `new GameObject(...)`（`Shell/MenuDraw.cs:59-65`，出厂 `activeSelf = true`）、
  `MenuDraw.Rect` → `ImageQuad.Create`（`Shell/MenuDraw.cs:1049+`）、`Label.Create` 里**没有任何 `SetActive(false)`**；
  建树路径 `SettingsWindow.BuildRoleBlock:1456-1475` → `MenuInputField.Create:2035-2054` 全程只 `Node/Rect/Text/Hit`。
- 该节点到窗根这条链上**唯一的 `SetActive(false)` 只可能是页**（`OpenTab:583`；`Host Block` 那一条 `activeSelf == true` 当场有断言 `:1721` 绿；
  `Menu Area` 活着 —— 同一份日志里 `Screen()` 那一族几何断言（含 A131 锚）**全部绿**，而它们同样要求「有 active 的 quad」）。

### 3) 结论（这一环的解释）
⇒ **A = 「Online Tab 不活」（上一条判据）与 B = 「`GetComponentInChildren<WindowButton>()` / `<Label>()`
在那个不活的子树里取到了件」（两条点击 + `ipLb != null` + `win.Role` 真的切成 Client，`:5478` 绿）同时成立**。
两者只有在「默认重载会跳过**祖先不活**的对象」这一条上才互斥 ⇒ **那条假设是错的**：

> **`GetComponentInChildren<T>()`（不带 `true`）不查祖先链** —— 祖先 `activeSelf == false`
> **不影响**它找到（或被找到）自己开着的子孙；`activeInHierarchy`（`RectOf` 用的那个）才查祖先。
> ⚠️ **本件独立证死的是这一半（「祖先不活 ⇒ 照样返回」）**；另一半「**它自己** `activeSelf == false` ⇒ 跳过」
> 我**没有**在本件里独立证死，是按本仓既有观察（`Editor/CollectionScene.cs:3970` 那一族）**接受的**——
> 两半拼起来才是完整语义，落盘时请照这个口径写（别把另一半也记成「F4 实证」）。

**旁证（上一版全绿跑，`final_SettingsScene.log`）**：`:4970` = A94 收尾的 `win.TryOpen(null)`
（栈里写明 `SettingsWindow.OpenTab` ← `SettingsWindow.Open()` ← `GameWindow.TryOpen` ← `SettingsScene.cs:1487`）
⇒ 那一版此刻 `Current = Online`（`Graphics Tab` **不活**）；紧接着 A171 在 `:5009` / `:5022` 报
``✓ 页标题 `Tab Title` 在`` 与 `✓ ★ 页标题字号 = … 49.50`——而 `FindChild(root, "Tab Title")` 取的是
**建树次序里第一颗**（`SettingsWindow.cs:536-539` 先 `BuildTabs`、再 Graphics → Media → Online；
「Tab Buttons」下那三颗叫 `Tab Toggle Title`，不重名）⇒ 量到的正是**不活那一页**的标题，
且 `FontPxNow = 49.50`（`Label.FontPxNow` 是 `_tmp.fontSize` 的**纯读**，`Battle/Label.cs:388`）。
⇒ **全绿跑里就有一次「在不活的祖先下取到并量准」**。（这一条依赖「`GetComponentsInChildren` 兄弟序」这一条
Unity 列举语义；**主证是上一段那个矛盾**，不依赖它。）

> 📌 与本仓旧注释的关系（**不是**推翻它们，是**收窄**）：`Editor/CollectionScene.cs:3970` / `Editor/MainMenuScene.cs:3313` / `:3830`
> 那三处说的「不含未激活」，实测只覆盖**被搜的节点自己**是关着的那种（`hl.SetActive(false)`、
> `ChooseNameWindow`、`pop.transform` 那几处正是这一档）。**本报告的这一条是新的**：
> **祖先关着、自己开着 ⇒ 默认重载照样返回。** 两说可以并存 ⇒ 已按「只报不改」处理（§六·1）。

### 4) 修完 `bar` 之后，那 5 条会不会一起转绿？——**会**，逐条给理由
- 它们**共同的前提**（联机页活着）修好后成立：`Click(Bar(root), "Online")` ⇒ `OpenTab(Online)` ⇒ `RectOf` 能量。
- 第 3～5、7 条（A208 那 4 条）：期望值 = **设计矩形 × `Screen()`** 手算的字面量
  （`[632.868,414]–[1082.868,468]`，算式写在 `:1716-1719` 那段注释里），而 A208 已经让
  `MenuInputField.Create` **进门第一行就过 `Screen()`**（`Shell/SettingsWindow.cs:2039`）
  ⇒ 渲出来的矩形就是同一个数（`MenuDraw.Rect(...,"bg",...)` 按 `r` 画，无 `clip`/`keepAspect`）。
  **最有力的一环**：第 5 条是「框左沿 vs **同列标签**左沿」的**跨路径一致性**，
  而日志里**标签那一侧实测已经是 632.87**（`:5106`）＝ 期望的 `632.868` ⇒ 框一旦能量到，左沿必然对得上。
- 第 6 条（【Test Public IP】）：**上一版全绿跑里量出来就是绿的**（`final_SettingsScene.log:4393`，
  `270.0×72.0 @(1073.9,639.0)` 与期望逐位吻合）。
- ⚠️ **保留意见（如实写）**：A208 那 4 条**在任何一份日志里都还没绿过**（它们是本批新加的，
  `grep -c A208` 在 `final_SettingsScene.log` / `settings_1006*.log` / `s1007*_settings.log` 里全是 **0**）。
  上面第 3 点的理由是我能给的**构造性判据**（期望值与实现同源于一条已经验证过的换算 + 标签侧实测吻合），
  但「第一次绿」要等同步点那次跑。**若还有红的 ⇒ 按 §五·1 判，别再怀疑夹具。**

---

## 五、没查清的部分（如实）

1. **A208 那 4 条的「首次绿」没观察到**（见 §四·4 的保留意见）。判据齐、构造正确，但**没有历史绿**。
   同步点若见红，请按「实现缺陷」处理（检查点：`MenuInputField.Create:2039` 的 `Screen()`、
   `RectOf` 的并集量法、`MenuDraw.Rect` 的 `clip` 参数），**不要再回头动夹具**。
2. **本报告 §四·3 的一条支链依赖「`GetComponentsInChildren` 按兄弟序列举」**（用来断定
   「上一版全绿跑里 A171 量的是 Graphics 页的标题」）。主证（矛盾式）不依赖它，但我没能在本地
   （不跑 Unity）把它**单独**证死 —— ⛔ 没有拿它当结论的唯一支撑。
3. **`SettingsWindow.Build()` 的重建语义我只读到"销毁子件"这一层**（`:482`），
   没去核 `GameWindowWithTabs`/`GameWindow` 那一侧在 `Close()` 时还会动什么
   （本件不需要 —— 但若以后有人在**关窗期间**量节点，那一层要另查）。
4. 音频页那一段现在**真的会切到 Audio**（修前它是「没切也照样绿」）⇒ 那一片断言回到
   「Media Tab 活着」的状态下跑。上一版全绿跑证明过那一片在该状态下是绿的，但**中间批次改过它**
   （A169 / A125 / A96 都是新加的），所以它同样属于「回到设计态、首次在本批代码上跑」。

---

## 六、顺手发现（⛔ 只报不改）

1. **本仓对 `GetComponentInChildren` 默认重载的语义记录只覆盖了一半，而且是两说并存的状态**：
   `Editor/CollectionScene.cs:3970`、`Editor/MainMenuScene.cs:3313`、`:3830`、`:5109`（后一条自己就
   标着「语义没写死 · X5 审查 §断言 #26 也标了定不了」）+ `资料/普查产出_1004/X5审查_A55.md:65`（#26）
   一直在等一次实跑判决。**本件给出的判决是「祖先不活不影响、自己关着才跳过」**（§四·3）——
   但那几处**不在我的白名单** ⇒ 请调度台决定要不要把这条判据落进 `资料/已知的坑.md` 或那几处注释
   （⚠️ 落的时候要写清「**它自己 `activeSelf` 关着**才算不含」这一档仍然成立）。
2. **诊断文件里的一处计数与日志不符（结论不受影响）**：`资料/普查产出_1010/D1_十二条红诊断.md:110`
   写「`切到 X 页` 本轮只剩 5 条（最后一条 = `:1388`）」——实测**本轮也是 8 条**
   （`:448` / `:1058` / `:1164` / `:1270` / `:1388` / `:3498` / `:3630` / `:5978`），
   变的是**构成**：重建之后那三次全落在 `Graphics`（本轮 Graphics×6 / Audio×1 / Online×1；
   上一版 Graphics×3 / Audio×2 / Online×3）。诊断的结论（两页没切过去）**完全正确**，只是条数写错了。
   （该文件不在我的白名单 ⇒ 只报不改。）
3. **同族形状在别的宿主要不要普查**：`grep -n "OpenWindow(\|TryOpen(\|\.Close()" Editor/*.cs`
   ⇒ `CollectionScene.cs` 有 20+ 处、`MainMenuScene.cs` 数处。我**只看了一处**形状最像的
   （`CollectionScene.cs:4278-4283`：A94 收尾重开聊天窗 —— 重开之后没有再用重开前抓的节点 ⇒ **安全**），
   **没做全量普查**。建议调度台按本件的判据（「重建前抓的 `Transform` 之后还用吗」）派一条只读普查。
4. **A171 的「页标题」取法是隐式的**：`FindChild(root, "Tab Title")` 靠建树次序拿第一颗
   （= Graphics 页的）。它现在**受当前页影响**（不活也不影响读取，但断言的「断的是哪一页」不明显）
   ⇒ 建议改成 `FindChild(FindChild(root, "Graphics Tab"), "Tab Title")`，与 `:620` 切页那一段同形。
   （⛔ 我没改 —— 会动到 A171 的断言形状，超出「夹具回归」这一件。）
5. **`Label` 的 `WorldW` 在不活页上也是有效值**（第 5 条红里「标签 632.87」就是从不活的联机页量出来的）
   —— 这条与 §四·3 同源，对**将来写「不活页也要量」**的自检有用（不用先切页）。

---

## 七、跑过的检查

- **秒级类型检查**：`TMPDIR=/tmp/wf_f4 bash d:/4/Unity/工具/typecheck.sh`
  ⇒ `--- 运行时程序集 --- 运行时错误数: 0` · `--- 编辑器程序集 --- 编辑器错误数: 0`（**0 错**，且无「别人的半成品」报错）。
- **行尾 / 规模**：`CRLF 0 / LF 2188`（改动前 `CRLF 0 / LF 2122`）—— 没翻行尾。
- ⛔ **没跑 Unity / 没跑 `SettingsScene.Run`**（自检由调度台在同步点按覆盖面跑）。
  ⇒ 那 7 条**是否转绿以同步点那次为准**；本件的判断与理由在 §四·4。
