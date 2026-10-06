# W1 · `Shell/MenuDraw.cs`（A796 + A798）· 写手报告（「清空 A 表」第六轮 · 2026-10-15）

## 〇、摘要（≤ 6 行）

1. 🔴 **简报前提被现读推翻：这两笔账【都已经在 HEAD 里了】** —— A798 的两行闸与 A796 的 `CheckShadeClickRule` 都由 **`71bad57`（第五轮收口提交）** 落地，开工时 `git status` 对该文件**干净**（零 diff）。⇒ **本件没有功能代码可写**。
2. 我做的 = **逐条核过两份落地物**（判据 / 签名 / 读口 / 判别力 / 残留风险）+ 补 **1 段 11 行注释**（新口的**时机硬约束**，见 §2 —— 它会真把窗关掉，同族已在 `RewardsScene` 踩过一次红）。
3. 类型检查：`TMPDIR=/tmp/wf_w1 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**（改动前后各跑一次，两次都 0/0）。
4. 行尾：改后 `git diff --numstat` = **11 / 0**（纯插入）· `MenuDraw.cs` 仍**纯 LF**（`b.count(b'\r\n') == 0`）✔
5. **还欠**：A796 的 **24 个宿主调用点**（全仓 `CheckShadeClickRule` **零调用点**）—— 按简报**不在我这一件**；A798 **零剩余**。
6. 另报（简报 §④ 点名要写的那条）：账上「压暗层点击在 11 条自检里**零站立点**」**不成立** —— `CheckAbsorbRule` 早在真点击（证据 → §5·1）。

---

## 一、前提更正（现读 · 铁律 5 口径：保留更正痕迹）

| 账 | 简报怎么说 | **HEAD 实际** | 证据 |
|---|---|---|---|
| **A798** | 「`Text` / `TextBox` 不做那道闸，**要各加一行闸**」 | ✅ **两行都在**：`Text` 在 **`Shell/MenuDraw.cs:1614`**、`TextBox` 在 **`:1673`**，都是 `if (!Visible(r, _st.RenderClip)) return null;`；两处 doc 也已就地订正（`:1580-1595` · `:1657`） | `git log -S "A798" -- Shell/MenuDraw.cs` ⇒ 只有 **`71bad57`** |
| **A796** | 「那 6 条断言一条都不问点了会不会关，**要新增一个口** `CheckShadeClickRule`」 | ✅ **口已在**：**`Shell/MenuDraw.cs:2248`** `public static void CheckShadeClickRule(MenuCheck chk, string what, Transform winRoot, Transform darkHit, Func<WindowState> state)`（我插注释前是 `:2237`）；`CheckShadeRule` 签名未动（`:2173`） | `git log -S "CheckShadeClickRule" -- Shell/MenuDraw.cs` ⇒ 只有 **`71bad57`** |

🔴 **正本 `d:/4/项目任务.md:397` 记的就是这个状态**：「**A796**（`CheckShadeClickRule` **新口已加**、**24 个宿主调用点未接**）」——A798 已不在「还开着」清单里。
⇒ 简报 ② 指的判据文件（`普查产出_1014/RO_文字半边与压暗层.md` · `普查产出_1013/批次计划_1013.md` §六·B）是**动手前**的普查口径，**动手那一轮已经把两句都做掉了**。**建议调度台按 §5「还欠」重切这一件**。

---

## 二、改动清单（本件**只此一处**）

| 文件:锚点 | 改前 | 改后 | 依据（判据出处） |
|---|---|---|---|
| `Shell/MenuDraw.cs:2228`（`CheckShadeClickRule` 的 doc，插在「⚠️ 它【不】答什么」段与「调用点怎么接」段**之间**） | 无此段 | **新增 11 行注释**：🔴 **时机硬约束 —— 本口会把窗真的关掉，要在本窗其它断言都跑完之后再调**；含三条实据：① `Close()` **同步**（`Shell/WindowsManager.cs:590-625`，同一次调用里就写 `CurrentState = Closed` + 末句 `SetActive(false)`）② 打在**顶窗**上时 `NotifyClosed` 会 `ShowPreviousWindow()` 把底窗带回前台（`Shell/WindowsManager.cs:1000`）③ ⛔ 别试「就地重开」——`TryOpen(null)` 会**重建内容**、清掉刚灌的假数据（同族实测留档 → `Editor/RewardsScene.cs:8715-8725`，原话「⛔ 不许再把本块挪回 `CheckAbsorbRule` 之后」）· 并写明「这不是新坑，`CheckAbsorbRule` 第 ⑥ 步早就在真点击」 | 本仓同族**已踩过一次红**：`Editor/RewardsScene.cs:8716`「本块原来跑在 `CheckAbsorbRule(收件箱窗)` **之后**…而那一段的第 ⑥ 步就是「点面板外 ⇒ 窗 `Closed`」⇒ 探针跑的时候窗已经关了」· `Shell/WindowsManager.cs:990-1000` `NotifyClosed` 体 · `Shell/PointerLayer.cs:628`「关掉弹窗后底窗要回 `Open`」 |
| （**无其它改动**） | —— | —— | A798 的两行闸、A796 的口**都已存在**，我一个字没动 ⇒ 逐位保留第五轮已验证的字节 |

> 复现：`git diff --numstat -- Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs` ⇒ **`11 0`**（纯插入、零删除）。

---

## 三、核过什么（A796 / A798 两份落地物逐条对判据）

### 3·1 A796 新口 = 判据 §三 的落点 (a) 逐条对上

| 判据（RO §三②）要求 | 落地物 | 结论 |
|---|---|---|
| ① 点**前** `state() == Open`（**互为对照**，防「两边一起改回去」） | `:2265` `chk(before == WindowState.Open, …)` + **不成立就 `return`**（`:2268`，不硬点） | ✅ |
| ② 对 `darkHit` 取 `WindowButton` 调 `Click()` ⇒ 断 `state() == Closed` | `:2254` `GetComponent<WindowButton>()` → `:2269` `wb.Click()` → `:2271` `chk(after == Closed)` | ✅ |
| 「新开一口、`CheckShadeRule` 签名不动」 | `CheckShadeRule` 仍是 `(MenuCheck, string, Transform, Transform, int)`（`:2173`） | ✅ |
| 读口全 public、无需新增 | `Hit` 把 `WindowButton` 挂**返回节点自己身上**（`MenuDraw.cs:1780`）⇒ `darkHit.GetComponent<WindowButton>()` 不是空判；`PointerLayer.ClickAt` 也是 `b.Click()`（`Shell/PointerLayer.cs:926-932`）⇒「`Click()` = 派发口」成立 | ✅ |
| 改坏法有鉴别力 | 换成 `Absorb` ⇒ `absorbOnly` ⇒ `Click()` 首句早退（`Shell/PromptPopup.cs:1100`）⇒ ② 必红；另有 `:2261` `chk(!wb.absorbOnly)` 这条**结构**断言兜「两边一起改」 | ✅ |
| ⚠️ 它**不**答什么 | doc 明写「几何/档」归 `CheckShadeRule`、「屏幕坐标点得到吗」归 `CheckAbsorbRule`，本口**不看坐标、不看遮挡**、⛔ 别在本口里扫点 | ✅ 与我这次补的「时机硬约束」互补 |

**实多出来的一条**（RO 设计只要 2 条，落地是 3 条结构/行为断言）：`:2252` 「命中区**属于这一扇窗**」（`darkHit.IsChildOf(winRoot)`，传错节点/被别家窗顶掉就红）—— 方向一致，**不是偏离**。

### 3·2 A798 两行闸逐条对上

| 检查 | 结果 |
|---|---|
| 位置在 `TextCore` **之前**（建完再 `return null` = 树上留个没人管的节点） | ✅ `Text` `:1614` / `TextBox` `:1673` 都在各自的内层调用之前 |
| 用 `Visible` 还是 `ClipRect` | ✅ 用 `Visible`（`MenuDraw.cs:290-295`）—— 与 `Rect`/`Nine`/`Hit` 那条路**故意**不同：`ClipRect` 多「退化矩形（w 或 h ≤ 0.01）有裁切即不可见」那条守卫，那是给**建不出 quad 的图**用的（`ClipRect:210`） |
| 无裁切时行为逐位不变 | ✅ `_st.RenderClip` 为 `null` ⇒ `Visible` 首句 `return true`；`NoClip` 哨兵也走真（`r.x2 <= −∞` 等四条恒假）⇒ **不建节点这条路一个字节都没动** |
| 残留风险（**不是 NRE**）是否如实记 | ✅ 复核了被点名的那处：`Shell/DailyStreakPopup.cs:351` `MenuDraw.Text(curLbl != null ? curLbl.transform : p, …)` —— 守卫**在**，闸返回 `null` 时落 `p`（层级变、不炸），与 doc 写的一致 |
| 调用点要不要改 | ✅ 0 处（RO §一② 的 199 调用点口径，我现数同量级：`MenuDraw.Text(` 152 行 / `MenuDraw.TextBox(` 54 行，含注释与字面量） |

### 3·3 顺带现数的两个公开数字（都有行号）

- `MenuDraw.CheckShadeRule` **代码调用点 = 24**：`CollectionScene` 4（`:2230/2813/4833/5424`）· `MainMenuScene` 11（`:1287/2709/2777/3484/3960/4343/4440/4668/7212/7756/8509`）· `RewardsScene` 5 · `SettingsScene` 1 · `ShopScene` 3 ⇒ **与 RO §三① 逐格吻合**。
- `MenuDraw.WasShadeHit` / `ShadeHitTierWarned` / `WasAbsorb` / `AbsorbTierWarned` **全 public** ⇒ 自检侧无需新读口。

---

## 四、新口签名 + 用法示例（**给下一批补调用点的人直接抄**）

```csharp
// d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs:2248
public static void CheckShadeClickRule(MenuCheck chk, string what,
                                       Transform winRoot,        // 这一扇窗的根（核「这颗命中区属于它」）
                                       Transform darkHit,        // 压暗层命中区（ShadeHit 的返回值 / FindChild(win, "<名字>")）
                                       System.Func<WindowState> state)   // 喂 () => win.CurrentState
```

**它断三条（互为对照，缺一条就瘦一圈）**：
① 那颗命中区**挂在这一扇窗的树里** ② 它**不是吸收层**（`!absorbOnly` —— 结构断言，挡「两边一起改」）③ **点之前是 `Open`**（前提，不成立就不点）④ **`Click()` 之后是 `Closed`**。

**抄法（形状 A：窗对象自己记着那颗命中区 —— 多数窗）**
```csharp
MenuDraw.CheckShadeClickRule(CheckTrue, "卡包详情窗", t, darkHitN, () => win.CurrentState);
```
**抄法（形状 B：宿主按名字取 —— 名字照 `CheckShadeRule` 那一行现成的实参抄）**
```csharp
MenuDraw.CheckShadeClickRule(CheckTrue, "战斗日志弹窗",
    pop.transform, FindChild(pop.transform, "CloseHit"), () => pop.CurrentState);
```

**四条须知**：
1. 🔴 **时机硬约束（这次补进 doc 的那段）**：本口**真把窗关掉**（`Close()` 同步；顶窗还会 `ShowPreviousWindow()` 把底窗带回前台）⇒ **放在本窗其它断言之后**；⛔ 别试「就地重开」（`TryOpen(null)` 会重建内容）。
2. ⚠️ **节点名逐窗不同**（`CloseHit` / `BackgroundHit` / `BackdropHit` …）—— **照 `CheckShadeRule` 那一行已经写着的名字抄**，⛔ 别统一按 `BackgroundHit` 写（`Editor/MainMenuScene.cs:172-174` 记着「会把聊天窗那条误判成红」）。
3. ⚠️ **本口不看坐标/遮挡**（走派发口直调是**有意的**）；要覆盖「真鼠标点得到」就在宿主侧另配 `PointerLayer.ClickAt` **钉死的点**，⛔ 别在本口里扫点。
4. ⚠️ `winRoot` 别传 `null`、`state` 别传常量闭包 —— 传 `null` 会被 ⓪ 那条**如实报红**（不静默早退），传常量则 ① 的前提那条**失去意义**。

---

## 五、没做完 / 做不了的（如实记）

1. **A796 的 24 个宿主调用点 —— 未接**（简报划走：`Editor/*.cs` 归另一批写手）。现读：全仓 `CheckShadeClickRule` **0 个调用点**（只有定义 + doc 示例）⇒ **这笔账今天还没真正闭上**：口有了，站立点仍是 0。
   ⇒ 下一批若只补 `MainMenuScene` 11 + `RewardsScene` 5（RO §三④ 的建议），**别忘掉 ① 的时机**（§四·1）。
2. **未跑 Unity**（本轮规矩：A 表清零前不中途跑自检）⇒「**加闸后有没有既有断言会红**」仍然**没实跑过**。RO §一③ 的**只读**判断是「A781 那段的夹具矩形与 clip 有交集 ⇒ 不会被闸影响」—— 我只复核了它的口径，**没有实跑**，如实标注。
3. **A799**（VC 父链 ~145 处）不是我这件，一个字没碰。
4. **「点了不会关」那个方向**：RO §三①(b) 明说**今天全仓没有一扇窗需要**它 ⇒ ⛔ 没做（不发明口径）。
5. **A798 的 `Text` doc 措辞不精确（未改，留给出账人裁）**：`:1583` 写「闸就加在**本方法第一句**」，代码里第一句是 `ViewportClip.Resolve`（`:1604`），闸是 `:1614`。**语义没错**（闸确实在 `TextCore` 之前、且成立时直接返回），只是「第一句」这三个字不严 —— 我**没有动它**（避免改无关的、第五轮已核过的文本）。

## 六、顺手发现（⛔ 我一个字没改别的文件）

1. 🔴 **「压暗层点击在 11 条自检里零站立点」这条不成立**（简报 §④ 点名要报）：`CheckAbsorbRule` **5 份宿主副本**（`Editor/{CollectionScene:78, MainMenuScene:73, RewardsScene:191, SettingsScene:100, ShopScene:869}`）**已经在真点击**——它走 `PointerLayer.Instance` + **固定的**候选点，第 ⑥ 步就是「点面板外 ⇒ 窗 `Closed`」，且最后一跳是真 `pl.ClickAt`。
   ⚠️ **数字对不上，如实记**：RO §三① 记「**26 个调用点**」，我**现数 = 23 处代码调用点**（Collection 4 · MainMenu 12 · Rewards 5 · Settings 1 · Shop 1；已排除 `static void CheckAbsorbRule(` 定义行与注释行）—— **差 3 条我没对出差在哪**（可能 RO 把注释行/多行调用算进去了）。**结论方向不受影响**（≥23 处真点击）。
2. 🔴 **同族已有一次真实翻车，判据完整可引**：`Editor/RewardsScene.cs:8715-8725`（收件箱那一块）——「本块原来跑在 `CheckAbsorbRule` **之后** ⇒ 探针跑在**关着的窗**上 ⇒ `HoverAt` 返 `<null>`、连带两条红」；那里还记着「⚠️ **别再试「就地重开窗」**（`inbox.TryOpen(null)`）：实测它**重建内容** ⇒ 上面灌的 8 条假消息没了」。**这正是新口会遇到的同一族**，我已把它写进 `CheckShadeClickRule` 的 doc（§二），**建议下一批简报的「已知什么」那一格也带上这两条**。
3. **`CheckAbsorbRule` 的 5 份宿主副本仍未收口**（RO §三② 记为「另一笔账」）—— 现读 5 份签名逐字相同（`(string what, Transform winRoot, string nodeName, float x1,y1,x2,y2, int qShade, int qContentMin, Func<WindowState> state)`），是「两处写同一条规则」的形状。**我只记不做**。
4. **A798 的「调用点零改动」今天仍然成立**：`Shell/DailyStreakPopup.cs:351` 是那族里唯一「拿 label 的 `transform` 当父件」的形状（其余 140 个赋值点都在守卫内），它**本来就有兜底**（`curLbl != null ? … : p`）⇒ 闸上线后**层级会随滚动状态变**，但**不炸**。若将来有人给这条配断言，⛔ 别断「父子关系在所有滚动位置都恒定」。
