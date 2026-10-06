# WE1g · 同族收口（A718 + A720 + A721）

> 写手代理 · 2026-10-13 · **动两个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`
> （A718 + A721）与 `Shell/MenuWindowBase.cs`（A720，**只改注释、代码零动**）+ 本报告。
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只跑了只读的 `git diff` / `git status`）· ⛔ 没改正本
> ⛔ 没越白名单（`Battle/Label.cs` · `Shell/` 其余 · `Editor/` 其余 **全部只读**，一个字没动）
> 📌 **本报告的行号 = 收工那一刻的现读**（`MainMenuScene.cs` **9201 → 9286** 行 · `MenuWindowBase.cs` **540 → 558** 行）。
> 🔴 **两处「本件动手前」的旧坐标**（简报给的就是它们）：A718 的判据句原在 `MainMenuScene.cs:1831` · A720 那句过期话原在 `MenuWindowBase.cs:505-506`。

---

## 一、结论

| 账 | 结果 | 一句话 |
|---|---|---|
| **A718**（`:1831` 同族第三处「高」那半读 `_tmpH` 缓存） | ✅ **换口完成** | 量法 `Label.WorldH`（缓存 `_tmpH`）→ **`TmpRenderedRect` 的网格高**；字面常量 `31.6f` 与文案全文**逐字未动**（§二） |
| **A720**（`Shell/MenuWindowBase.cs:505-506` 的过期话） | ✅ **订正完成** | 原句「四窗这几颗**没有**渲染宽断言」**不成立**（四扇窗各自都有）—— 按铁律 5 留更正痕迹（§三） |
| **A721**（「不刷缓存」名单里 `SetAutoFitBox` 那项**是反的**） | ✅ **8 处全订正** | 简报说 **7 处**，**实为 8 处**（多一处 `:4835`，见 §四·0）；7 处换成 `SetFontSize` + 全 8 处留更正痕迹 |
| 类型检查 | ✅ **0 / 0** | `TMPDIR=/tmp/wf_we1g bash d:/4/Unity/工具/typecheck.sh` |
| 行尾 | ✅ **没翻** | 二进制读：`MainMenuScene.cs` **0 CRLF / 9286 LF**（改前 `0 / 9201`）· `MenuWindowBase.cs` **0 CRLF / 558 LF**（改前 `0 / 540`）；全程只用 **Edit**，⛔ 无 `sed -i` |
| 改动量 | `MainMenuScene.cs` **+88 / −3（净 +85）** · `MenuWindowBase.cs` **+20 / −2（净 +18）** | 见 §八（两边账都对得上） |

**本件动的落点（现读）**：A718 = `:1836–:1903`（真红法 `:1836-1837` 原样保留 + 换口注释 `:1838-1886` + 断言 `:1887-1903`）·
A721 = 文件头 `:310–:326` + 7 处短痕迹（`:2950` / `:2960` / `:4835` / `:6847` / `:7309` / `:8612` / `:8752`）·
A720 = `Shell/MenuWindowBase.cs:505–524`（**代码在 `:525`，未动**）。

---

## 二、A718 换口

### 2·1 换口前后（现读 `:1887–:1903`）

```csharp
{
    var longName = FindChild(FindChild(deckHolder, "DeckRow_0"), "Deck Name");
    CheckTrue(longName != null, "（前提）第 1 套是那个**超长名字**的卡组、`Deck Name` 在");
    if (longName != null)
    {
        var lbL = longName.GetComponentInChildren<Label>();
        CheckTrue(lbL != null, "（前提）那一段是真 `Label`");
        if (lbL != null)
        {
            CheckFits(longName, 172f, "★ ㉒③ 超长卡组名**渲出来不超过 172px 的条**");   // ← 未动（A709 已换口）
            float hx1, hy1, hx2, hy2;                                                // ← 新
            if (!TmpRenderedRect(lbL.transform, out hx1, out hy1, out hx2, out hy2)) // ← 新（换口 + 量不到就红）
            { CheckTrue(false, "★ ㉒③ 超长卡组名（前提）…「渲出来的高」量不到…"); }       // ← 新（显式红）
            else
            {
                float hPx = hy2 - hy1;                                               // ← 新
                CheckTrue(hPx <= 31.6f,                                              // ← 判据句（见 2·3）
                          $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 {hPx:F2}px）"
                          + " —— 只接折行、没接自适应的那一档在这里是 ~210px");
            }
            CheckTrue(lbL.FontPxNow < 28.5f, …);                                     // ← 未动（相邻那条自适应该断的）
        }
    }
}
```

**旧码那 3 行**（`git diff -U0` 的 `-` 行，逐字）：

```
-                                    CheckTrue(lbL.WorldH * 108f <= 31.6f,
-                                              $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 {lbL.WorldH * 108f:F2}px）"
-                                              + " —— 只接折行、没接自适应的那一档在这里是 ~210px");
```

### 2·2 简报点名「照 A709/A714/A715 那一套」的两个细节 —— 逐条交代

| # | 细节 | 本件 | 说明 |
|---|---|---|---|
| **①** | **量不到 ⇒ 显式红（⛔ 不许省）** | ✅ 补了 | `TmpRenderedRect` 失败时四个 out **全 0**；若让 `hPx` 拿到 0，`0 ≤ 31.6f` 会**假绿**。 |
| **②** | **量 `lbL.transform` 而不是 `longName`** | ✅ 照抄 | 取 `Label` 的写法 `longName.GetComponentInChildren<Label>()` **一字未动**；测量口用 **`lbL.transform`**（TMP 是它的子件 —— `Battle/Label.cs:784` `TmpFont.NewText(transform, …)`，**已实读**）。⛔ 没有改成 `TmpRenderedRect(longName, …)`：那会捞 `longName` 子树里**第一颗 TMP（含 inactive）**，与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。 |

🔴 **③ 本半条的判据句【没有】`> 0f` 那一半** ⇒ ⛔ **不能**照 A714 用 `-1f` 哨兵
（`-1 ≤ 31.6` **恒真** = 假绿的另一种写法）—— 只能像 A715 那样**把原句包进 `else { }` + 补一条显式红**。
理由已写进代码注释（`:1881-1884`）。

### 2·3 「期望值与容差一字未动」的证据 —— **⚠️ 本件**做不到**「判据句是上下文行」，如实标**

简报验收标准 1 要的是「`git diff -U0` 自证：判绿红那句是**上下文行**」。**本件做不到，原因是结构性的、与实现无关**：

```
$ git diff -U0 -- …/Editor/MainMenuScene.cs | grep -n "31.6f\|30.6px 的条\|只接折行"
402:+                        //    🔴 **期望值 `31.6f`（30.6px 的条）/ 文案全文一位未动** …（我新写的注释）
422:-                                    CheckTrue(lbL.WorldH * 108f <= 31.6f,
423:-                                              $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 {lbL.WorldH * 108f:F2}px）"
424:-                                              + " —— 只接折行、没接自适应的那一档在这里是 ~210px");
440:+                                        CheckTrue(hPx <= 31.6f,
441:+                                                  $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 {hPx:F2}px）"
442:+                                                  + " —— 只接折行、没接自适应的那一档在这里是 ~210px");
```

⇒ **判据句必然进 diff**：旧码把 `lbL.WorldH * 108f` **直接写在算式与插值里**，**没有中间变量可换**
（`CheckFits` 能「上下文行不变」是因为它本来就有一个 `float w = …` 中转 —— 见 WE1f 报告 §3·3 的同一段如实标）。
**等价的、更硬的证据**（逐字对照上面 6 行）：

| 项 | 旧 | 新 | 判 |
|---|---|---|---|
| 字面常量 | `31.6f` | `31.6f` | ✅ 逐字同 |
| 文案正文 | `★ ㉒③ …而且**高度也没溢出** 30.6px 的条（实得 ` | 同 | ✅ 逐字同 |
| 文案尾句 | `+ " —— 只接折行、没接自适应的那一档在这里是 ~210px");` | 同 | ✅ 逐字同 |
| **被比较的表达式** | `lbL.WorldH * 108f` | `hPx` | 变了 —— **这正是「换口」本身** |
| 插值项 | `{lbL.WorldH * 108f:F2}` | `{hPx:F2}` | 同上 |
| 缩进 | 36 空格 | 40 空格 | 变了（包进 `else` 的代价，同 A715） |

⇒ **`31.6f`（原版 30.6px 的条）/ 容差（这里没有第二条容差，判据是裸 `≤`）/ 文案全文一位未动**；
变的是「测量的口」与它外层的括号。⛔ **不是「改判据」。**

### 2·4 该段自带的真红法 —— ✅ **没弄丢**（简报点名）

现读 `:1836-1837`（**逐字未动**，已用字符串计数核过 `count == 1`）：

```
                        // 🔴 **真红法**：把 `RebuildDeckRows` 里那句 `nm.SetAutoFitBox(…, 8, 29)` 删掉 ⇒
                        //    字号回到名义 29（只折行）⇒ **高那条红**；再连 `SetWrapping` 一起去掉 ⇒ 两条都红。
```

⚠️ **如实标**：这条真红法**不区分两个口**（删了 `SetAutoFitBox`，两种量法读到的都是 ~210px、都红）。
⇒ 本件**另补了一条只咬旧口**的改坏法（§五）。

---

## 三、A720 注释订正（`Shell/MenuWindowBase.cs`）

### 3·1 原来写的 vs 实际（🔴 实况比简报说的还严重）

**原句**（现读 `:505-507`，作为「原来这两行写的是」的引文保留在文件里）：

> 「（四窗这几颗**没有**「渲染宽度 ≤ 框宽」的断言 —— 已 `grep` 核过；同形的两条在
>  `Editor/MainMenuScene.cs` 的**档案窗**键循环里，那是 `PlayerProfileWindow` 自己那一族、不走本函数。）」

🔴 **实际**：2026-10-08 波 C3（A212 主表 #31）已经给**四扇窗各自**加了该断言 —— **每扇 4 颗键逐颗断**，
而且断的**正是本函数（`BuildShell` → `BuildTabBar:414` → `BuildTabButton`）建出来的那几颗**：

| 窗 | 宿主与行号 | 键的节点名 | 本件亲读 |
|---|---|---|---|
| 奖励窗 | `Editor/RewardsScene.cs:980` | `RewardsTabButton_*` | ✅ 读过断言全文 |
| 商店窗 | `Editor/ShopScene.cs:1278` | `ShopTabButton_*` | ✅ 读过断言全文 |
| 收藏窗 | `Editor/CollectionScene.cs:892` | `CollectionTabButton_*`（前缀扫的） | ✅ 读过断言全文 |
| 社交窗 | `Editor/MainMenuScene.cs:5360`（简报给的 `:5287` 是**本件动手前**的位置；本件 A718+A721 把它推了 +73 行） | `SocialTabButton_*` | ✅ 读过断言全文 |

四条都是 `渲出来的宽 ≤ 155 + 0.5`；框宽 **155** = 原版 `Tab Buttons/*/Label` 的 `sz=(155,37.86)`。
**四窗各自的块注释也自陈**这件事（例：`RewardsScene.cs:957` / `ShopScene.cs:1252` / `CollectionScene.cs:867` /
`MainMenuScene.cs:5314`，都写着「（四窗这一族原来一条都没有）」——**过去时**）。
四窗确实都走本函数：`Shell/{RewardsWindow.cs:249, ShopWindow.cs:132, CollectionWindow.cs:2064, SocialWindow.cs:129}`
各一句 `BuildShell(transform, Buttons, "<X>TabButton_", "Tabs")` —— **本件亲读**。
⚠️ **四条行号 = 2026-10-13 现读**（写进 `MenuWindowBase.cs` 的注释里也是这四个 + 一句「会漂、按句子现读找」）。

### 3·2 改法（⛔ 只改注释、代码零动）+ 更正痕迹

`Shell/MenuWindowBase.cs:505-524` 换成：「原来这两行写的是 …（原文照录）」+「**实际是** …（四窗 × 文件:行号）」
+「**错因** = 原句写作时（波 C3 之前）确实成立，它是**当时的实况**；波 C3 给四个宿主加断言时**没回头改这一句**
⇒ 典型的「当时对、现在不对」」。
**代码行 `:525` `if (txt != null) txt.SetWrapping(false);` 一字未动**（`git diff` 里它是**上下文行**）。
⇒ `Shell/` 是共用件，本件**没动任何代码**，所以**不触发**共用件那一族的复跑要求。

> ⚠️ **顺带记两条（只写进注释、⛔ 没改别处）**：① 那四条里有**三条仍读缓存**
> （`RewardsScene`/`ShopScene`/`CollectionScene` 的 `klb.WorldW * 108f` = `_tmpW` 缓存口；只有
> `MainMenuScene` 那条 A715 已换口）—— 即**同族第四/五/六处**，⛔ 不在本件白名单；
> ② 原句后半「同形的两条在**档案窗**键循环里」也过期了（那两条现读 `MainMenuScene.cs:2769-2772`，
> **早已换成 `TmpRenderedRect`**，不是 `RenderedRect` 了）。

---

## 四、A721 八处逐处（现读行号 · 改成了什么 · 更正痕迹）

### 4·0 🔴 简报说 **7 处**，**实为 8 处** —— 第 8 处在 `:4835`

简报按「`grep` 那句话的关键词」列出了 7 处（`:310` / `:2881-2882` / `:2890` / `:6770` / `:7229` / `:8528-8529` / `:8669`），
**漏了一处**：`MainMenuScene.cs` 里 `LeaderboardRow` 那条 A617 断言的**失败文案**里也有一份
（现读 `:4838` 那句字符串 `+ "（SetCharSpacing/SetWrapWidth/SetAutoFitBox）⇒ 旧量法照旧报 1420、这一条红）"`）。
它比另外 7 处**藏得更深**（在**运行期字符串**里、不在注释里）⇒ `grep` 注释关键词会漏。
**本件一并订正**（判据 = 铁律 5「顺手 `grep` 那句话的关键词，把同一句话被复制到别处的地方一起改」——
这句话的「关键词」不止 `SetAutoFitBox`，还有「旧量法照旧报」）。

**全库复核**（⛔ 只读，本件亲跑）：`grep "不刷新 \`_tmpW\`" / "只重排 mesh"` 全 `CardPresentation/**.cs`
—— 命中的**只有 `Editor/MainMenuScene.cs`**，**别的文件一份都没有**（所以本件没漏掉跨文件的副本）。
另有 `MainMenuScene.cs:4084` 一处**只点 `SetCharSpacing`**（不写 `_tmpW` 那半），**它是对的** ⇒ **未动**（与简报一致）。

### 4·1 逐处对照表

| # | 现读落点 | 原来的说法 | 改成 | 更正痕迹 |
|---|---|---|---|---|
| **1** | `:310` = `TmpRenderedRect` **文件头**（**原委只写这一处**） | `SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`** 这一族**只重排 mesh、不刷新 `_tmpW`** | `… / `**`SetFontSize`**` 这一族…` | ✅ **全文更正段**（`:313–:326`）：原写 X → 实际 Y → 错因 Z + 8 处清单 + 关键词「A721 订正」 |
| **2** | `:2948`（A666 · `:2946` 那句「那一族」的简写版） | `SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`** 那一族 | 同上一族 = `SetFontSize` | ✅ 3 行短痕迹（`:2950-2952`）指向文件头 |
| **3** | `:2958`（同段「改坏法」那一句） | 改坏法 = 插一句重排调用（`SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`**） | 同上一族 = `SetFontSize` | ✅ 2 行短痕迹（`:2960-2961`） |
| **4** | `:4835`（**简报漏的第 8 处**；在 A617 断言的**文案**里，判据句在 `:4838`） | `（SetCharSpacing/SetWrapWidth/`**`SetAutoFitBox`**`）` | `（…/SetFontSize）` | ✅ 3 行短痕迹（`:4835-4837`，插在字符串拼接中间） |
| **5** | `:6846`（A617 · 「`SetCharSpacing` **那族**」的笼统写法） | 「`SetCharSpacing` 那族只重排 mesh、不刷新 `_tmpW`」 | 「`SetCharSpacing` / `SetFontSize` 那族…」 | ✅ 4 行短痕迹（`:6847-6850`）—— 这一处**原文里没有 `SetAutoFitBox` 四个字**，「那族」指向的却是含它的名单 ⇒ 一并澄清 |
| **6** | `:7309`（A666 · 改坏法） | 改坏法 = 插一句重排调用（`SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`**） | 同上一族 = `SetFontSize` | ✅ 3 行短痕迹（`:7309-7311`） |
| **7** | `:8611`（A684 · `CheckText` 那段的改坏法） | 「…（`SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`** —— 这一族只 `ForceMeshUpdate`、**不刷新 `_tmpW`**…」 | 同上一族 = `SetFontSize` | ✅ 4 行短痕迹（`:8610-8614`） |
| **8** | `:8751`（**A709 `CheckFits` 的文件头**） | `SetCharSpacing` / `SetWrapWidth` / **`SetAutoFitBox`** 这一族只 `ForceMeshUpdate()`、**不刷新 `_tmpW`** | 同上一族 = `SetFontSize` | ✅ 4 行短痕迹（`:8750-8754`） |

**全 8 处统一带 `A721` 关键词**（现读 `grep -c A721` = **10**：8 处痕迹 + 文件头里的引用行 + A718 注释里的指向行）。

### 4·2 「实际是 Y」的判据 —— 本件**逐条实读 `Battle/Label.cs`**（⛔ 没照抄 WE1f 的结论）

| 方法 | 刷不刷 `_tmpW/_tmpH` | 出处（**本件亲读**） |
|---|---|---|
| **`SetAutoFitBox`** | ✅ **刷** —— **末句就是 `RefreshBounds()`** | `:640-695` 的 **`:694`** |
| **`SetFontSize`** | ❌ **不刷**（`_tmp.fontSize = worldSize; _tmp.ForceMeshUpdate();`，无 `RefreshBounds`） | `:503-514` |
| `SetCharSpacing` | ❌ 不刷（`_tmp.characterSpacing = v; _tmp.ForceMeshUpdate();`） | `:524-533` |
| `SetWrapWidth` | 激活路 ❌ 不刷（`:258-262` `GenerateLayout` 后就 `return`）；**未激活路 ✅ 刷**（`:295` / `:306` 各一次 `RefreshBounds()`） | `:249-265` / `:267+` |
| `ForceRelayout` | ✅ 刷 | `:454-464` 的 `:463` |
| **唯一的写点** | `_tmpW = …` / `_tmpH = …` 全库只在 **`:803-804`**（`RefreshBounds` 内）+ 字段初值 `:37` | `grep "_tmpW\s*=\|_tmpH\s*="` = 3 命中 |

⇒ **结论不变**（换口只需要「**存在**一条不刷的路」）：`SetCharSpacing` 就是。**错因**（写进文件头）
= 这句话是照 A490/A617 那份名单抄下来的、**没有回 `Battle/Label.cs` 逐条实读**。

---

## 五、灭自证与改坏法

### 5·1 A718 的「灭自证」—— 两个口各读什么、谁写它

| | 旧口 | 新口 |
|---|---|---|
| 读什么 | `Label.WorldH` = **字段缓存** `_tmpH`（`Battle/Label.cs:37` 初始化 · `:55` 只读口） | **TMP 自己的 `textBounds`**（mesh 的活值，过 `localToWorldMatrix`；`TmpRenderedRect` `:324-342`，A490/A617 收口的那一份） |
| **谁写它** | **只有 `RefreshBounds()`**（`:799-804`，实读全库 `_tmpW = ` / `_tmpH = ` 仅 3 命中）—— 也就是**被测实现自己** | TMP 每次重排都重算，**不在实现那条链上** |
| 自称 | 断「**高度**没溢出」 | 同一条自**称** |

⇒ 旧口读的是「**实现最后一次记下来的数**」，新口读的是「**画面上真的有多高**」。
断言的**自称**是「渲出来的高」，**只有新口配得上这个自称**。

### 5·2 两条改坏法（A718）—— 一条「不区分口」、一条「只咬旧口」，**触发与否如实标**

| 改坏法 | 动哪里（具体到行） | 机制 | 预期 |
|---|---|---|---|
| **① 该段自带的（原有、保留）** | 删 `Shell/PracticeModePopup.cs` 的 `RebuildDeckRows` 里 `nm.SetAutoFitBox(…, 8, 29)`（现读 `:1063-1065`） | 字号回到名义 29、只折行 ⇒ 高 ~210px | **新旧都红** ⇒ **不区分两个口**（同 A709② 的如实标） |
| **② 本件新补的（只咬旧口）** | 同函数 `RelayoutNow(nm);`（现读 **`:1073`** —— 那是 `_tmpH` 的**最后一次**写入：`RelayoutNow` → `ForceRelayout`（`Battle/Label.cs:454`）→ `RefreshBounds()`（`:463`））**之后**插一句 `nm.SetCharSpacing(5f);` | 网格重排了、**缓存不动** ⇒ 两个口读到的数**必然不同** | 旧口报**旧高**、新口报**活值**（是不是翻红见下） |

⚠️ **如实标（照抄并重申 WE1f §4·3 那条，⛔ 别当成「已验证」）**：

1. 🔴 **`SetAutoFitBox(…, 8, 29)` 开着 autosize** ⇒ TMP 在重排时**可能把字缩回去**、让渲出高**仍 ≤ 31.6**
   ⇒ 改坏法② **【不一定】把绿翻红**。**能确定的只有**「改坏之后两个口读到的数不一样」= 换口的全部意义；
   「新口红 / 旧口绿」三件套里**第三件（红）本地证不出来**，⛔ 不假装证过。
2. **幅度与容差谁大 —— 本地量不出来**（同 WE1e §五 / A715 那条口径）。已知先例只在【位置】那一族
   （`Shell/LiveOpsEventWindow.cs:734-746` 的 A475 / 同族 `SetAutoFitBox` 次序错实测偏 40.75px）；
   **【宽度/高度】这一族没有本地实测值**。
3. ⚠️ 改坏法② 的**落点定位依赖**「`RelayoutNow(nm)` 是该 `Label` 末次刷缓存」这条链（`SetAutoFitBox`（`:1063-1065`）→
   `SetWrapping(true)`（`:1068`）→ `RelayoutNow`（`:1073`），现读 `Shell/PracticeModePopup.cs`）—— **本件静态读出来的，没实跑证实**。

### 5·3 换口当天「两条量法逐位同值」的链条（静态推的）

- 两条读的是**同一份** `_tmp.textBounds`（旧口 = `RefreshBounds` 里的 `Max(|b.size.y|, 1e-4)`；新口 = 同 `b` 四角）；
- `LayoutSpace.PxX(x) = x×108 + 960` · `PxY(y) = 540 − y×108` ⇒ **差值**里常数项抵消，`py2 − py1 = ΔworldY × 108`；
- 父链**单位缩放 + 无旋转**时 `Δworld = b.size` ⇒ 新 = `|b.size.y| × 108` = 旧（差只可能来自浮点舍入，容差够）。
- ⚠️ **父链缩放本件静态核过（新证据）**：`Shell/PracticeModePopup.cs:686-688` 亲写「今天父链是**单位缩放**」
  （窗根 = `WindowsManager.AttachToAnchor` 写死 `localScale = one`，`:965`；本窗的小屏缩放器只在 `LateUpdate`，
  批处理**没有帧循环**；`extraScaleSmallScreen = 1.07` 只写字段、不挂 `TransformScalerBySmallScreenUI`）。
  **旋转**这一条**没查**（若哪颗标签被转过，新口报的是外接矩形、更大）。

---

## 六、没查清 / 没做的（⛔ 如实标）

1. ⚠️ **A718 那条断言从没在运行时跑过**（按简报**一次 Unity 都没跑**）⇒ **首跑就是它的第一次**。
   静态侧本件核到了「两条读同一份 `textBounds`、父链单位缩放」（§5·3），**没有实跑复核**。
   **红了先量实得值**：⛔ **别直接改期望值**。四种典型长相 —— ① 报「量不到」（= 我补的那条「前提」红了 ⇒ 网格不在）；
   ② 报**天文数字**（≈`4.29e9` = 空串/未生成时 TMP 的哨兵，见 `TmpRenderedRect` 文件头）；
   ③ 报一个**正常但比旧值大/小**的数（那才是真的版面差异）；④ 报 0.00（本件不可能 —— 旧写法下 `_tmpH` 最小 1e-4×108 = 0.0108）。
2. ⚠️ **本件没跑任何自检**（简报禁 Unity）—— 所以「A718 换口后那条仍绿」**不是本件证出来的**，
   要等同步点跑 `MainMenuScene.Run`。**本件能保证的只有「编得过」（类型检查 0/0）+ 静态推理**。
3. ⚠️ **`Shell/` 只改了那一句注释，但 `Shell/MenuWindowBase.cs` 是共用件** —— 本件**没动代码**
   （`git diff` 里 `:523` 是上下文行），因此**不新增**共用件那族的覆盖要求；同步点按简报判即可。
4. ⚠️ **A720 的「原来这两行写的是」是照录的**（含原来的错话）—— 这是**故意的**（铁律 5 要留更正痕迹），
   ⛔ 别当成「文件里还有一句错话」再改一遍。
5. ⚠️ **A721 的 8 处短痕迹里凡写「行号」的只有文件头那一处**（`:2950 / :2960 / :4835 / :6847 / :7309 / :8612 / :8752`），
   并**已写明「行号 = 收工那一刻的现读、会漂、按句子现读找（关键词 `A721 订正`）」**——本件自己就撞过一次
   （先写 7 个号、加完注释后发现全漂了 2 行，改过一次）。
6. ⚠️ **本件没有去核对 `Editor/RewardsScene.cs` / `ShopScene.cs` / `CollectionScene.cs` 里那三条**
   「渲出来的宽 ≤ 155」断言的**其它毛病**（例如它们读的还是缓存口）—— 只在 A720 的注释里**如实点了一句**，
   ⛔ **没改**（不在白名单、不在账上）。

---

## 七、顺手发现（⛔ **一个都没改**）

### 7·1 🔴 社交窗那条 C3 块注释里的「末句」说法可能**不准**（本件亲读、**没核完**）

`MainMenuScene.cs:5320` 的 block 注释写着「**改坏法**：删掉 `BuildTabButton` **末句** `txt.SetWrapping(false)`」。
**本件亲读** `Shell/MenuWindowBase.cs`：那一句现读 **`:525`**，而它**后面还有 ~30 行**
（`Badge Highlight` 子树 + 点击区那一整块）⇒ **它不是 `BuildTabButton` 的末句**。
⛔ **本件没改**（不是我的账；且它把语句**逐字引了**出来 ⇒ 读者仍找得到那句话，只是「末句」这个词不准）。
**要不要订正由调度台定**。

### 7·2 🔴 同族**第四/五/六处**：另外三扇窗的那条断言仍读缓存口

`RewardsScene.cs:981` · `ShopScene.cs:1279` · `CollectionScene.cs:893` —— 三处都还是
`CheckTrue(klb.WorldW * 108f <= 155f + 0.5f, …)`（= `_tmpW` 缓存口），与 A709/A714/A715/A718 换口前的
`MainMenuScene` 那两条**同族**。⛔ **不在本件账上、没碰**（简报白名单只有两个文件）。

### 7·3 🔴 `MainMenuScene.cs:4084` 那处**只点 `SetCharSpacing`**（**它是对的**）

简报已说明；本件亲读确认它的原文是「`SetCharSpacing` 又只 `ForceMeshUpdate`、**不刷新 `_tmpW`**」——
**没有** `SetAutoFitBox` ⇒ **没动**。⚠️ 但它引的行号是 `Battle/Label.cs:524-532`，本件实读 `SetCharSpacing` 是 `:524-533`
（尾行差 1）—— 属**极小**的引用偏移，⛔ 没改（不在账上、且同一句被复制到多处时会一起漂）。

### 7·4 ℹ️ `MainMenuScene.cs:2769-2772`（档案窗六键那两条）**已经是新口**且**带 `> 0f` 那一半**

A720 的原句说「同形的两条在**档案窗**键循环里」—— 那两条现读用的是 `TmpRenderedRect`（A714 换的），
**且判据自带 `wPx > 0f` / `hPx > 0f`**（所以 A714 能用 `-1f` 哨兵）—— 这是 A718 **不能**照抄那个形状的**直接原因**（§2·2 ③）。

---

## 八、类型检查结果 + 改动量

```
$ TMPDIR=/tmp/wf_we1g bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
⇒ **一趟通过、0 / 0**（本件没遇到 WE1f 那种「别人的半成品被编进来」的情形）。

```
$ python: CRLF 计数（二进制读）
Editor/MainMenuScene.cs   CRLF= 0  LF= 9286      （改前：CRLF= 0  LF= 9201）
Shell/MenuWindowBase.cs   CRLF= 0  LF= 558       （改前：CRLF= 0  LF= 540）

$ git diff --numstat  （相对 HEAD；⚠️ 里面含 W-E1 / WE1b–WE1f 的未提交改动，⛔ 别当本件的改动量）
1099  70  Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs
  20   2  Unity/MyGame/Assets/CardPresentation/Shell/MenuWindowBase.cs
```

- **`MainMenuScene.cs` 本件真正动的是 +88 / −3（净 +85）** —— 基线 = WE1f 收工的 `1011 / 67`
  ⇒ 1099−1011 = **88** · 70−67 = **3** · 88−3 = **85** = 9286−9201 ✅ 两边对得上。
  **那 3 条删除**逐条点名（`git diff -U0` 的 `-` 行，就是 §2·1 那三行）：`CheckTrue(lbL.WorldH * 108f <= 31.6f,` ·
  `$"★ ㉒③ …（实得 {lbL.WorldH * 108f:F2}px）"` · `+ " —— 只接折行、没接自适应的那一档在这里是 ~210px");`
  （**后两条只是「表达式/缩进变了」，文案字面一位未改** —— §2·3 那张表）。
  ⚠️ 这 **+88** 的构成（**实测量出来的、不是估的**）：**A718 那一段 = 15 行 → 68 行（净 +53）** ——
  本件亲测：现读 `:1836–:1903` 那段（真红法那行到它的闭括号）**= 68 行**，改前 `:1819–:1833` **= 15 行**；
  ⇒ 它在 `-U0` diff 里记成 **+56 / −3**（其中 12 行逐字保留 ⇒ 只算 3 条删除 ✅）。
  余下 **+32（且一行未删）** = A721 那 8 处 —— git 在那些注释/字符串块里**全部报成纯插入**，
  所以 `-` 数没有增加。**账核对**：56+32 = 88 ✅ · 删除恒 3 ✅ · 88−3 = 85 = 9286−9201 ✅。
- **`MenuWindowBase.cs` 本件动的是 +20 / −2（净 +18）** —— 540+18 = 558 ✅；
  `−2` = 原来那两行注释（作为引文原样出现在新注释里），代码行 `:525` 是**上下文行**、不进 diff。
- ⛔ **没跑 Unity** · ⛔ **没动 git**（只跑只读的 `git diff` / `git status` / `git diff --numstat`）· ⛔ **没改正本** ·
  只碰白名单里那两个 `.cs` + 本报告（`Battle/Label.cs` / `Shell/` 其余 / `Editor/` 其余**一律只读**）。

---

*（报告完 · 写手代理 WE1g · 只写了本文件 + `Editor/MainMenuScene.cs` + `Shell/MenuWindowBase.cs` 的注释）*
