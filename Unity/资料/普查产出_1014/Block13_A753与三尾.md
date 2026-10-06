# Block13 —— A753（A744「全删」补丁）+ A673 / A782 / A786 三尾

> 写手代理（一件活一个代理）· 2026-10-14 · 独占白名单：`Editor/MainMenuScene.cs` · `Editor/ShellScene.cs` ·
> `Shell/SocialWindow.cs` · `Editor/CollectionScene.cs`。
> 判据 = `资料/普查产出_1013/WA435丁_收尾.md` **§四**（A753）· `资料/普查产出_1013/批次计划_1013.md`
> **§六·B 的 A673/A782/A786 三行**。⛔ 没跑 Unity · ⛔ 没动 git · ⛔ 没改任何 `资料/*.md`（除本文件）。
>
> ⚠️ **行号一律是【改后】的**（WA435丁 那份清单里的行号是它当时的；本件落笔时三个文件的行号已漂：
> `MainMenuScene.cs` 探针段 +32 行、`ShellScene.cs` B13 段 +25 行）—— 逐处对的是**内容**，不是行号。

---

## 一、A753 —— 逐处表（清单 12 + 8 + 2 = 22 处，**实做 22 处**）

### 1·1 `Shell/SocialWindow.cs`（§4·1，清单 12 处 → 实做 12 处）

| 清单# | 改后行 | 改了什么 |
|---|---|---|
| **1** | `:221-231` | 删掉字段 doc（`/// <summary>**显式覆盖那一档的裁切边界**…`）+ **`protected PxRect? Clip;`** ⇒ 换成一段 **`//` 留档注释**（见下「判据去哪了」） |
| **2** | 同上 | 删掉 **`public PxRect? ClipNow { … }`** |
| **12** | 同上 | 字段 doc 里「今天的调用面只剩两处…」随 #1 一起删 |
| **3** | 同上 | 删掉 `SetClip` 的 doc（24 行）+ **`public void SetClip(PxRect? r) { Clip = r; }`** ⇒ ⚠️ 那段 doc 里的**三个视口原版真值**按 §4·4 #4 的第二个选项**就地留档**（挪进 #1 那段 `//` 注释） |
| **4** | `:246-248` | `SocialPage.Rect` 尾参 `Clip` 删掉 ⇒ `MenuDraw.Rect(parent, tex, r, name, Q + qOff, tint, keepAspect)`（缺省 `clip = null`） |
| **5** | `:251-261` | `Nine` 的 doc 重写（说明现在**不传**、由节点给）+ `Nine` 方法体末尾的 `clip: Clip);` 整行删掉、上一行尾逗号一并去掉 |
| **6** | `:262` | 同 #5（同一行） |
| **7** | `:365` | `SocialPage.Text`：`if (!MenuDraw.ClipRectAbove(parent, r, Clip, out _))` ⇒ `… , null, out _)`（⚠️ **`ClipRectAbove` 保留**，没退回裸 `ClipRect`） |
| **8** | `:385` | `SocialPage.Hit` 末句 `…pressedArt, Clip); }` ⇒ `…pressedArt); }` |
| **9** | `:267-268` · `:275-277` · `:376-385` | 三处 doc 提到 `Clip` 的地方全部改写（含 `:268` 那句历史描述加了一句「那句里的 `Clip` 就是本页那个**已于 A753 删掉的**显式覆盖字段」） |
| **10** | `:439-441` | `SocialView.SetClip` 的 doc + 方法体（含 `Page == null` 那条告警）**整块删掉**（连同 `:438` 的空行），换成两行 `//` 留档 |
| **11** | `:490-491` | `SocialView.Cosmetic` 尾参 `Page.ClipNow` 删掉 ⇒ 走缺省 `clip = null` |

**判据去掉了没有？没有。** §4·1 #3 点名的三个原版视口真值（`Open Alliances>Viewport` = 360.99,337.29→1874.90,1079.77 ·
`Friends Container>Viewport` = 332.15,314.80→1875.80,1080.06 · `MemberList>Scroll View>Viewport` = 369.67,493.63→1880.67,1080.05）
**原字面搬进了 `SocialWindow.cs:226-231` 那段留档注释**（并各标了它对应当前代码里的哪个常量/访问器），
外加奖杯格那条与三处软边那条 —— 用的是 §4·4 #4 允许的「**就地留档**」那一支（本件不许改 `资料/*.md`，所以没挪进 §4·4 的表）。

### 1·2 `Editor/MainMenuScene.cs`（§4·2 **档 1**，清单 8 处 → 实做 8 处）

| 清单# | 改后行 | 改了什么 |
|---|---|---|
| **1** | `:5744-5748` | 探针之上**新挂一颗载体节点**：`var probeVp = ViewportClip.Hang(sw.transform, "ClipProbeVp", fvp, Vector4.zero, Vector2Int.zero);` + `var probe = MenuDraw.Node(probeVp.transform, "ClipProbe", fvp);` |
| **2** | `:5751-5755` | 删掉 `pg.SetClip(fvp);` + 「`ClipNow` 就是那个视口」那条 ⇒ 换成清单建议的**等价且更强**的那条：`CheckTrue(ViewportClip.FindAbove(probe) == probeVp.GetComponent<ViewportClip>(), …)`（断的是「四处转发走的 `Resolve` 真的落到这颗节点上」） |
| **3** | 未动 | `pg.Rect` / `pg.Hit`×2 / `pg.Text` / `pg.Nine`×2 一行没改（本来就没传 `clip`） |
| **4** | `:5780-5782` | 删掉 `pg.SetClip(null);` + 「清掉了」那条 ⇒ 换两句说明「**A753 起没有『清』这一步**」（⛔ 没静默删） |
| **5** | 未动 | 7 条量值断言 + 命中区两条**一个字都没改** |
| **6** | `:5801-5807` | 只删 `view.SetClip(fvp);` / `view.SetClip(null);` 两行；`view.Rect(...)` 与那两条断言照旧（`:5760` 那句「子视图那一层」的注释改成新含义） |
| **7** | `:5818` | `SocialWindow.DestroySafe(probe.gameObject)` ⇒ **`DestroySafe(probeVp.gameObject)`**（删祖先带走 `probe`；⛔ 没两处都删） |
| **8** | `:5729-5741` | 段首那段注释按新语义重写（「现在补了写入口」→「那两个写入口已整体删掉 / 载体会话状态长在视口节点上」；「把 `SetClip` 或任一处转发拆掉」→「把 `ViewportClip.Hang` 或任一处转发拆掉」） |

✅ **7 条量值断言的期望值一位没动**（节点框 ≈ `fvp`，残差 ~6e-5 px ≪ 0.5px 容差）—— 与 §4·2 的预判一致。

### 1·3 `Editor/ShellScene.cs`（§4·3，清单 2 处 → 实做 2 处）

| 清单# | 改后行 | 改了什么 |
|---|---|---|
| **1** | `:3594-3609` | 那条读 `pf.ClipNow`/`pa.ClipNow` 的 `CS1061` 断言**整条换掉** ⇒ 新判据：`fVc = NodeOfFrame(pf.transform, 好友视口框)` → `fContent = FindChildIn(fVc.transform, "Content")` → `fAbove = ViewportClip.FindAbove(fContent)`，断 `fAbove == fVc` |
| **2** | `:3594-3599` | 断言文案整段重写（删掉「`Clip` 恒 `null`」「这个口留着（A25① 探针在用）」两句 —— 全删之后它们不再有指涉对象） |

🔴 **偏离清单字面的一处（如实报，⛔ 不是自作主张）**：§4·3 #1 建议的原文是
`CheckTrue(ViewportClip.FindAbove(pf != null ? pf.transform : null) != null, …)` —— **照抄会当场假红**：
`ViewportClip.FindAbove`（`Shell/ViewportClip.cs:263`）是**沿父链向上**找，而那颗 `Friends Container/Viewport`
是**页根的后代**（`FriendsTab.cs:217-219`：`Root → Friends List → Friends Container → Viewport`，
本文件 `:3576-3578` 的 A30 `NodeCheck` 正是拿 `pf.transform` 当根**往下**搜才找到它的）
⇒ 从 `pf.transform` 向上走**永远 null**。
⇒ 按该条的**意图**（「好友页的切载体是节点」）写成**方向正确、且更强**的版本 —— 起点取视口**里面**的真实内容件
（`…/Viewport/Content`，`FriendsTab.cs:229` 那句话**空表也会建**），落到的必须是那颗 `Viewport`。
✅ 与清单意图一致（「载体已经是节点、不再是页字段」），并额外钉住了「接在**正确那一颗**上」。

---

## 二、A673 / A782 / A786

| 账 | 改了什么 | 判据 | 做完没有 |
|---|---|---|---|
| **A673** | `Editor/ShellScene.cs:3198-3215`：`lw1.Close()` 之后**现场重抓**那颗入口钮（`FindChildIn(profA.transform, …)` 三行）+ 一条前提断言，再 `logBtn2.onClick()`；⛔ **没有**只用「加个 null 守卫」 | §六·B 的 A673 行（「连 null 守卫都没有」）+ **同一族的尺子 = `Editor/MainMenuScene.cs:4590-4607` 的 A516 改法**（那边就是「重抓 + 前提出声」） | ✅ **做完**。判据「半齐」的那半也**读掉了**（见下） |
| **A782** | `Editor/MainMenuScene.cs:4783-4792`：把 `ScanSoftCuts` 那条断言的**真红法**就地订正（改「逐件传的实参」已经不灵 ⇒ 写成还灵的两手：改 `ArmyClipSoft` 常量 / 改 `Hang` 的 softness 实参），并把不灵的旧写法**留着当反例** + 复述 A782① 的脆性警告 | §六·B 的 A782 行（① 真红法保住的原因是「节点的 softness 从 `ArmyClipSoft` 取」② 那段榜单断言没跑没改、要现读） | ✅ **改完 + 现读完了**（见下「A782 现读结论」）；⚠️ **断言本身一条未改、未弱化** |
| **A786** | `Editor/CollectionScene.cs:2396-2404`（前提那句）+ `:2408-2415`（理由那句）：两处都加**带日期的更正痕迹**（「原来写 X，实际是 Y，错因 Z」） | §六·B 的 A786 行（理由过期、结论仍成立）+ 铁律 5 | ✅ **做完**，结论与断言一个字没动 |

### A673 那半条「没读」的判据 —— 读掉了

`WindowButton.onClick` 是**裸字段**（`Shell/PromptPopup.cs:326`）。
**「旧委托体碰到已销毁对象会怎样」的答案（读出来的，不是猜的）**：**照旧成功、不抛** ——
委托体 `BattleLogTab.OpenPopup`（`Shell/BattleLogTab.cs:172-177`）**一句实例状态都没读**：
只有**静态**的 `WindowsManager.OpenByRef`（`Shell/WindowsManager.cs:1206`，`PrefabRefBattleLog` 是 `const`，`:1138`）
+ 一句**常量串** `Debug.Log` ⇒ 即便宿主 `BattleLogTab` 已销毁，这条委托也照样把窗开出来。
⇒ **今天这一处是「巧合安全」、不是「结构安全」**：哪天 `OpenPopup` 读一个实例字段就抛 `MissingReferenceException`。
⇒ 所以本件**不止加守卫**、而是按 A516 的尺子**重抓**（守卫判假会「一下都不点」⇒ 下面 `lw2 != null && lw2 != lw1`
变**假红**，那正是 A516 踩过的坑）。

### A782 现读结论（`MainMenuScene.cs:4716-4815` 那一段榜单断言）

逐条核过 **两条结构性影响**（A782② 说的那两条），**静态上全部成立、期望值应逐位不变**：

1. **`MainMenuScene.FindChild` 是「按名递归」**（`:8922-8927`：`parent.GetComponentsInChildren<Transform>(true)` 逐个比 `name`）
   ⇒ `FindChild(armSel, "Army Content")`（`:4723`）**不受换父影响** —— A435 庚 把 `Army Content` 从
   `Army Selector` 的**兄弟**改成了 `Viewport` 的**子件**（`Shell/LeaderboardWindow.cs:488-489`）✅。
2. **`CheckAtWorld` 量的是节点的世界坐标**（`:265-271`：`Vector3.Distance(t.position, Center(x1,x2,y1,y2)) ≤ 0.01`），
   而 `MenuDraw.Node` 的 `r` 是**绝对设计 px**（`MenuDraw.Local` = `RectCenter(r) − PosInDesignSpace(parent)`，
   父的世界位再加回来 ⇒ 与父是哪一颗无关；`Viewport` 自己 `localScale = 1`）⇒ **换父不动位姿** ✅ ——
   `Army Selector` / 第 1 颗军种项（`:4748`，框 248.99,385.35,142.32,263.91）那几条的期望值照旧。
3. **`ScanSoftCuts` 那两条**（`:4793-4798`：竖切线恰好 `{290.99, 1629.01}`、横切线 `0`）：
   带的内沿 = 视口边 ± `softness`，节点给的 `softness` 同一份 `ArmyClipSoft = (42,0)`
   ⇒ 248.99 + 42 = **290.99** / 1671.01 − 42 = **1629.01**，容差 0.6px（节点框残差 ~6e-5）⇒ **照旧成立**。
4. ⚠️ **只有一处行为真的变了、而且没有任何断言量它**（如实报，非缺陷）：A435 庚 之后
   `RebuildArmyButtons` 里那颗 `MenuDraw.Hit(node, "Hit", r, QHit, …)` 开始吃视口的裁切
   （条目框 y = 142.32→263.91 比视口 147.6→258.6 **高**）⇒ 命中 quad 的**竖向**被截掉两头。
   `:4757-4770` 那两条只断「有 `WindowButton`」+ 直接 `ClickForTest()`（不走射线）⇒ 不受影响；
   ⚠️ 但**将来谁要拿射线去点军种条的上下边缘**，得先知道这一条。

⚠️ **本件没跑 Unity** ⇒ 上面四条是**静态判据**；「那一段断言真的全绿」只有 `MainMenuScene.Run` 能证。
**建议同步点跑 `MainMenuScene.Run`**（该段所在宿主）。

---

## 三、顺手发现（⛔ 一个都没自己改）

1. 🔴 **A744 全删之后的同族悬空文档指针（2 处，别人的文件）**：`Shell/AllianceMemberTab.cs:1362` ·
   `Shell/FriendsTab.cs:258` 都写着「（先例 → `SocialPage.SetClip` 那段注释）」—— **那段注释刚被本件删掉**
   （这正是 WA435丁 §八·5 早就点名的那两处）。⇒ 要把那句话改成指向 `SocialPage` 顶上那段新留档，或直接删掉。
2. ⚠️ **`Editor/MainMenuScene.cs:5921` / `:6518` 两条断言的【文案】**仍写着「拆掉 `SetClip` 这一条立刻红」
   （WA435丁 §八·4 点名的那两处）—— 那两条本身**不调 `SetClip`**（量的是 `QuadsOutside`），现在那句**失去指涉对象**。
   ⛔ 本件按简报「顺手发现只报不改」**没动**；建议归到 A753 的收尾（**纯文案**，断言仍有效）。
3. ⚠️ **`Editor/MainMenuScene.cs:5760` 那句注释里的旧判断已过期**（**先于本件就过期**，A25④ 那天起）：
   写的是「`SocialPage.Text` 那一处**只判横轴**（判据就是它自己那一句）」—— A25④ 已收口成
   `MenuDraw.ClipRectAbove`（**两轴**）⇒ 这句要改。**断言不受影响**（「整块在右缘以外」两轴下照样返回 null）。
4. ⚠️ **`MainMenuScene.cs:5788` 的断言文案**「（没设 `Clip` 时这里会是 1150）」用的是旧说法
   （今天没有「设 `Clip`」这个动作了）—— 纯文案，**断言义不变**。本件没改（不在 §四 清单内）。
5. 📌 **`SocialWindow.cs` 里 `MenuWindowBase.Clip` 这个词还在 `:279` 出现**（「这条缺口在 `MenuWindowBase.Clip`
   的注释里记着」）—— 那是**另一个类**的字段（`GameWindow` 那一套，没被本次全删波及），**不是悬空引用**，别误删。

---

## 四、没做完的（+ 为什么）

| 项 | 为什么没做 |
|---|---|
| `资料/普查产出_1013/WA435丁_收尾.md` §4·4 #4 的**第一个**选项（把三个视口真值挪进 §4·4 那张表） | 那张表在 `资料/*.md` 里，⛔ 本件简报不许改 `资料/*.md` ⇒ 用了 §4·4 #4 明写的**第二个**选项「就地留档」（`SocialWindow.cs:226-231`） |
| 同步点自检（`ShellScene.Run` + `MainMenuScene.Run` 两条） | ⛔ 简报红线「不跑 Unity」；本件只跑了**秒级类型检查** |
| 上面「顺手发现」5 条 | 简报红线「顺手发现只报不改」 |

---

## 五、类型检查 / 行尾自检

```
$ TMPDIR=/tmp/wf_b13 bash d:/4/Unity/工具/typecheck.sh      # 跑了 5 次（前 4 次：每改完一个文件就立刻跑；第 5 次：最后一次落盘之后再跑一遍）
--- 运行时程序集 ---  运行时错误数: 0
--- 编辑器程序集 ---  编辑器错误数: 0
```

**4 次全绿、一次都没出现「别人的半成品」那一类假错**（简报那条纪律没用上）。

| 文件 | `git diff --numstat` | 行尾（`io.open(p,'rb')` 二进制数） |
|---|---|---|
| `Editor/MainMenuScene.cs` | `108 / 38` | `CRLF=0` / `LF=9842` ✅ 纯 LF，没翻行尾 |
| `Editor/ShellScene.cs` | `116 / 18` | `CRLF=0` / `LF=4964` ✅ |
| `Shell/SocialWindow.cs` | `43 / 82` | `CRLF=0` / `LF=497` ✅ |
| `Editor/CollectionScene.cs` | `62 / 6` | `CRLF=0` / `LF=6386` ✅ |

⚠️ **`--numstat` 的数字里含【别的批次留在这四个文件上的未提交改动】** —— 开局 `git status` 里这四个文件**就已经是 ` M`**。
🔑 **本件自己的改动 = 上面那三张逐处表**（`git diff -U0` 的 hunk 头已逐条对上：
`SocialWindow.cs` **12 个 hunk = 本件全部**（与 §4·1 的 12 项一一对应，那个文件本件之外没人动过）；
`MainMenuScene.cs` **23 个 hunk 里本件的 12 个**（`4760`/`5697`/`5700`/`5703`/`5708`/`5711`/`5738`/`5759`/`5764`/`5766`/`5771`/`5774`，
余 11 个在 `785`/`868`/`907`/`910`/`948`/`8287`/`8375`/`8424`/`8507`/`8634`/`8636` 附近 = 别人早先的未提交改动）；
`ShellScene.cs` **12 个 hunk 里本件的 2 个**（`3199`/`3569`，余 10 个在 `3436`/`3793`/`3831`/`3833`/`3839`/`3846`/`3850`/`4362`/`4377`/`4543`）；
`CollectionScene.cs` **5 个 hunk 里本件的 2 个**（`2396`/`2402`，余 3 个在 `3175`/`5062`/`5097`））。
行尾一律**照抄原文件（LF）**，一律用 Edit 工具改（⛔ 没用 `sed -i` / python 写）。

**没碰**：`CLAUDE.md` · `项目任务.md` · 任何 `资料/*.md`（除本文件）· 别的任何 `.cs` · **git 一个命令都没动**
（只读的 `git diff/status/show`）· **Unity 一次没跑**。
