# WE1f · 同族两处（A714 + A715）

> 写手代理 · 2026-10-13 · **只动一个文件**：`d:/4/Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（+ 本报告）
> ⛔ 没跑 Unity（一次 `-executeMethod` 都没调）· ⛔ 没动 git（只跑了只读的 `git diff` / `git diff --numstat`）· ⛔ 没改正本
> ⛔ 没越白名单（`Battle/Label.cs` · `Shell/*` · `Core/*` **全部只读**，一个字没动）
> 📌 **本报告的行号 = 收工那一刻的现读**（文件 **9143 → 9201** 行）。**两件都办到**，没有一件写「只报不改」。

---

## 一、结论

| 账 | 结果 | 一句话 |
|---|---|---|
| **A714**（`:2740` 那条与 A709 同病灶、且**同时踩 `_tmpH`**） | ✅ **换口完成** | 量法 `Label.WorldW/WorldH`（缓存 `_tmpW/_tmpH`）→ **`TmpRenderedRect` 的网格宽/高**；**两条判据句（含 `> 0f` 那半与容差 `0.5f`）是纯上下文行**（diff 证据见 §二·3） |
| **A715**（`:5231` 同族「渲出来的宽 ≤ 框宽 155」） | ✅ **换口完成** | 同上；⚠️ **如实标**：这一条的**条件行结构上不可能留在上下文里**（旧码把 `clb.WorldW * 108f` 直接写在算式里、没有中间变量可换）⇒ 改的是那一行，**期望值 `155f` / 容差 `0.5f` / 文案尾句逐字未动**（§三·3） |
| 类型检查 | ✅ **0 / 0**（跑 3 趟，第 2 趟是**别人的半成品**、第 3 趟复原 —— §八） | `TMPDIR=/tmp/wf_we1f bash d:/4/Unity/工具/typecheck.sh` |
| 行尾 | ✅ **没翻** | 二进制读：改前 `0 CRLF / 9143 LF` ⇒ 改后 `0 CRLF / **9201** LF`；全程只用 **Edit**，⛔ 无 `sed -i` |
| 净增行 | **+63 / −5（净 +58）** | `git diff --numstat` 收工那一刻 `1011  67`（基线 = WE1e 收工的 `948 / 62`）⇒ 1011−948 = **63** · 67−62 = **5** · 63−5 = **58** = 9201 − 9143 ✅ 两边对得上 |

**本件动的两处落点（现读）**：`:2738–:2773`（A714）· `:5257–:5291`（A715）。

---

## 二、A714 改动（换口 · 那两个细节照抄了没有 · 期望值/容差动没动）

### 2·1 换口前后（现读 `:2738–:2773`）

```csharp
// 🔴 2026-10-13（A714）换口：量法从 Label.WorldW/WorldH（= 缓存 _tmpW/_tmpH）
//    换成 TmpRenderedRect 量出来的那块网格（px2 − px1 / py2 − py1）……（下略，全 17 行注释）
{
    var lb = lab != null ? lab.GetComponent<Label>() : null;
    float wPx = -1f, hPx = -1f;                                                  // ← 新（哨兵）
    float rx1, ry1, rx2, ry2;                                                    // ← 新
    if (lb == null || !TmpRenderedRect(lb.transform, out rx1, out ry1, out rx2, out ry2))  // ← 新（换口 + 量不到就红）
        CheckTrue(false, $"键 {i} 的「{spec.Label}」（前提）量不到「渲出来的字」—— …");       // ← 新（显式红）
    else { wPx = rx2 - rx1; hPx = ry2 - ry1; }                                   // ← 新
    float bw = PlayerProfileWindow.LabR - PlayerProfileWindow.LabL, bh = spec.LabB - spec.LabT;  // ← 未动
    CheckTrue(wPx > 0f && wPx <= bw + 0.5f, …);                                  // ← 未动（上下文行）
    CheckTrue(hPx > 0f && hPx <= bh + 0.5f, …);                                  // ← 未动（上下文行）
}
```

**旧码那两行**（`git diff -U0` 的 `-` 行，逐字）：

```
-                            float wPx = lb != null ? lb.WorldW * 108f : -1f;
-                            float hPx = lb != null ? lb.WorldH * 108f : -1f;
```

### 2·2 简报点名「A709 踩过、你必须照抄」的两个细节 —— 逐条交代

| # | 细节 | 本件 | 说明 |
|---|---|---|---|
| **①** | **量不到 ⇒ 显式红（⛔ 不许省）** | ✅ **补了，而且两道** | `TmpRenderedRect` 失败时四个 out **全 0**；若让 `wPx/hPx` 拿到 0，`0 ≤ bw + 0.5f` 会**假绿**。本件**两条都留着**：`-1f` **哨兵**（`wPx > 0f` 那一半照旧会红）+ 一条**说清原因的显式红**（文案写「量不到「渲出来的字」」+「⛔ 不是实现溢出」）。⚠️ 与 A709 的 `CheckFits` **形状不同**：那边判据没有 `> 0f` 那一半（所以必须 `CheckTrue(false)+return` 顶住），这边**本来就有** ⇒ 保哨兵 + 补具名红，**判据句可以一个字符都不动**（§2·3） |
| **②** | **量 `lb.transform` 而不是 `lab`** | ✅ **照抄** | 取 `Label` 的写法 `lab.GetComponent<Label>()` **一字未动**（它只认**这一颗自己**身上的 `Label`）；测量口用 **`lb.transform`**（TMP 是它的子件 —— `Battle/Label.cs:784` `TmpFont.NewText(transform, …)`）。⛔ 没有改成 `TmpRenderedRect(lab, …)`：那会捞 `lab` 子树里**第一颗 TMP（含 inactive）**，与「只找激活」的 `Label` **未必是同一颗** ⇒ 会把「节点不在（红）」与「量到了别一颗（绿）」混成一档。理由已写进代码注释 |

### 2·3 「期望值与容差一位未动」的证据（不是嘴说）

`git diff -U0`（**无上下文行** ⇒ 判据句只要出现就说明被改过）：

```
$ git diff -U0 -- …/Editor/MainMenuScene.cs | grep -n "wPx > 0f\|hPx > 0f\|bw + 0.5f\|bh + 0.5f"
(空)
```

⇒ **`CheckTrue(wPx > 0f && wPx <= bw + 0.5f, …)` / `CheckTrue(hPx > 0f && hPx <= bh + 0.5f, …)`（现读 `:2769` / `:2771`）整个 hunk 都没进 diff**，是**上下文行**。
本件动过的**只有**：两条 `float wPx/hPx = …WorldW/WorldH…` 的**取数那一句**（2 行删除）+ 新增的注释与前置。
`bw` / `bh` 的算式（`PlayerProfileWindow.LabR − LabL` / `spec.LabB − spec.LabT`）、文案全文，**一位未动**。

### 2·4 顺带把 `_tmpH` 那条也一并换掉了

简报说这条「比 A709 还多一层：`WorldH` 就是 **A617 #13** 那条 `_tmpH` 风险」——
本件**同一次调用**同时取 `px2−px1` 与 `py2−py1` ⇒ 宽、高**两条一起**换口，⛔ 没有只换一半。

---

## 三、A715 改动

### 3·1 换口前后（现读 `:5257–:5291`）

```csharp
if (clb != null)
{
    Check(clb.LineCount, 1, …);                                   // ← 未动
    // 🔴 2026-10-13（A715）换口：…（14 行注释）
    {
        float rx1, ry1, rx2, ry2;
        if (!TmpRenderedRect(clb.transform, out rx1, out ry1, out rx2, out ry2))
        {
            CheckTrue(false, $"★ 左栏第 {i + 1} 键 `{…}`（前提）这一颗 `Label` 底下没有 TMP 网格 ⇒ 「渲出来的宽」量不到…");
        }
        else
        {
            float w = rx2 - rx1;
            CheckTrue(w <= 155f + 0.5f,                            // ← 期望值/容差逐字同旧
                      $"★ …而且**渲出来的宽 {w:F1} ≤ 框宽 155**"
                    + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
        }
    }
}
```

**旧码**（`git diff -U0` 的 3 条 `-` 行，逐字）：

```
-                                CheckTrue(clb.WorldW * 108f <= 155f + 0.5f,
-                                          $"★ …而且**渲出来的宽 {clb.WorldW * 108f:F1} ≤ 框宽 155**"
-                                        + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
```

### 3·2 那两个细节

| # | 细节 | 本件 |
|---|---|---|
| **①** | **量不到 ⇒ 显式红** | ✅ 补了（`if (!TmpRenderedRect(…)) { CheckTrue(false, …量不到…); }`） |
| **②** | **量 `clb.transform`** | ✅ 照抄 —— 被测对象与旧写法**逐字相同**（同是 `br/Text` 那一颗 `Label`），只换测量口 |

🔴 **为什么这一条不能像 A714 那样用 `-1f` 哨兵**（已写进代码注释，是**必须**那么写的）：A714 的判据句自带 `wPx > 0f &&`，哨兵会被那一半挡住；**A715 的判据句没有正数那一半** ⇒ 哨兵 `-1f` 会让 `-1 ≤ 155.5` **恒真 = 假绿**（正是简报点名的那种弱断言）。所以这里只能把原句包进 `else { }`：**判据句的字面（`w <= 155f + 0.5f`）保持逐字不变**，代价是缩进进了 diff。

### 3·3 「期望值与容差」的证据 + ⚠️ 如实标

```
$ git diff -U0 … | grep -n "155f + 0.5f\|sz=(155,37.86)"
-  CheckTrue(clb.WorldW * 108f <= 155f + 0.5f,
-  $"★ …而且**渲出来的宽 {clb.WorldW * 108f:F1} ≤ 框宽 155**"
-  + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
+  CheckTrue(w <= 155f + 0.5f,
+  $"★ …而且**渲出来的宽 {w:F1} ≤ 框宽 155**"
+  + "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");
```

- ✅ **期望值 `155f`、容差 `0.5f`、文案全文（含 `sz=(155,37.86)`）逐字未动** —— 变的只有「`155f` 左边那个被比较的表达式」与缩进。
- ⚠️ **如实标（⛔ 别当成没做到）**：这一条的**条件行本身结构上必然进 diff**，因为旧码把 `clb.WorldW * 108f` **直接写在算式里**、**没有中间变量可换**（A709 的 `CheckFits` 之所以能「上下文行不变」，是因为它本来就有一个 `float w = …` 中转）。⇒ 简报验收标准 1 里那条「判绿红那句是上下文行」**只对 A714 成立**；A715 的等价证据 = **字面常量与文案逐字相同**（上面那四行对照）。

---

## 四、灭自证与改坏法

### 4·1 为什么「换口」本身就是灭自证

| | 旧口 | 新口 |
|---|---|---|
| 读什么 | `Label.WorldW` / `WorldH` = **字段缓存** `_tmpW/_tmpH`（`Battle/Label.cs:37` 初始化 · `:53-55` 只读口） | **TMP 自己的 `textBounds`**（mesh 的活值，过 `localToWorldMatrix`，`TmpRenderedRect` `:324-342`，A490/A617 收口的那一份） |
| 谁写它 | **只有 `RefreshBounds()`**（`Battle/Label.cs:799-804`）—— 也就是**被测实现自己** | TMP 每次重排都重算，**不在实现那条链上** |

⇒ 旧口读的是「**实现最后一次记下来的数**」，新口读的是「**画面上真的有多大**」。两条断言的自称都是「**渲出来的**字/宽」，**只有新口配得上这个自称**。

### 4·2 缓存真会滞后吗 —— 逐条**实读**源码（这一节是本件自己核的，与 A709 注释里那份名单**有一处不同**，见 §七·5）

| 路径 | 刷不刷 `_tmpW/_tmpH` | 出处（实读） |
|---|---|---|
| `RefreshBounds()` | **它就是唯一写点** | `Battle/Label.cs:799-804`（`_tmpW = Mathf.Max(Mathf.Abs(b.size.x), 1e-4f)`） |
| `SetCharSpacing(v)` | ❌ **不刷**（`characterSpacing = v; ForceMeshUpdate();`） | `:524-533` ✅ 与简报一致 |
| `SetFontSize(w)` | ❌ **不刷**（`fontSize = w; ForceMeshUpdate();`） —— **A709/WE1e 那份名单里没有它** | `:503-514` |
| `SetWrapWidth(w)` | 激活那条路 ❌ 不刷（`SetWrapWidthRect + GenerateLayout` 就返回） | `:249-265`（未激活那条路走待办，兑现时**会**刷：`:288-296` / `:301-307`） |
| `SetAutoFitBox(...)` | ✅ **刷**（**末句就是 `RefreshBounds()`**） | `:640-695` 的 `:694` —— ⚠️ **与 A709 注释里那份名单不符**（§七·5） |
| `ForceRelayout()` | ✅ 刷 | `:454-464` 的 `:463` |

⇒ **结论不变**（换口的理由只需要「有一条不刷的路」），而且**要多一条**：`SetFontSize`。
设问「那这两条断言到底会不会红」——**在换口当天不会**（见 4·4），意义在**将来**：谁在末次 `RefreshBounds` 之后插一句 `SetCharSpacing`/`SetFontSize`，旧口就报旧值、**照旧全绿**。

### 4·3 改坏法（两处各一个**具体落点**；机制确定，触发与否如实标）

| 条 | 动哪里（具体到行） | 机制 | 预期 |
|---|---|---|---|
| **A714-①** | `Shell/PlayerProfileWindow.cs:338`（`if (lbl != null) lbl.SetWrapping(false);` —— 这六颗页签标签的**最后一次**刷缓存：`SetWrapping`→`ForceRelayout`→末句 `RefreshBounds`）**之后**插一句 `if (lbl != null) lbl.SetCharSpacing(5f);` | 字距改了 mesh（每字多 5 个 font unit）、**缓存不动** ⇒ 两个口读到的数**必然不同**：旧口报**旧宽**、新口报**活值** | **新口读到的数会变、旧口不变**（是不是翻红见下面的「如实标」） |
| **A715-①** | `Shell/MenuWindowBase.cs:507`（`BuildTabButton` 末句 `if (txt != null) txt.SetWrapping(false);` —— **四窗**（Rewards/Shop/Social/Collection）左栏键共用这一句）之后插 `if (txt != null) txt.SetCharSpacing(5f);` | 同上 | 同上 |

**⚠️ 如实标（两条，⛔ 别当成「已验证」）**：

1. **幅度与容差谁大 —— 本地量不出来**（同 WE1e §五 / WE1d §六·2 那条口径）。已知的**先例是【位置】那一族**：
   `Shell/LiveOpsEventWindow.cs:734-746`（A475）实测「`SetCharSpacing(5f)` 排在 `AlignLeft` 之后 ⇒ 按**旧宽**定位、字整体往左溢出」，那次记的是「9 个字 ⇒ 左缘偏 `Δ宽/2`」、同族的 `SetAutoFitBox` 次序错实测偏 **40.75px**。
   **宽度**这一族**没有本地实测值**。
2. 🔴 **更实质的一条**：这两颗标签都开着 **autosize**（`SetAutoFitBox`，`enableAutoSizing = true` + `fontSizeMin/Max`），TMP 在重排时**可能把字缩回去**让渲出宽仍在 155 以内 ⇒ 那这个改坏法**不一定**把绿翻红。
   **能确定的只有**：改坏之后**两个口读到的数不一样**（旧口报旧值、新口报活值）——这就是换口的全部意义；「新口红 / 旧口绿」三件套里**第三件（红）本地证不出来**，⛔ 不假装证过。
   （旁证：`MainMenuScene.cs:1830-1835` 那条「同块断言」的真红法是真删 `SetAutoFitBox` —— `:1819-1820` 写着，那种删法**新旧都红**，**不区分两个口**，同 A709-② 的如实标。）

### 4·4 换口当天「两条量法逐位同值」的链条（静态推的）

- 两条读的是**同一份** `_tmp.textBounds`（旧口：`RefreshBounds` 里 `Max(|b.size.x|,1e-4)`；新口：同 `b` 的四个角）；
- `LayoutSpace.PxX(x) = x×108 + 960` · `PxY(y) = 540 − y×108`（`Core/LayoutSpace.cs:149,153`）⇒ **差值**里那两个常数项抵消，`px2 − px1 = ΔworldX × 108`；
- 父链**单位缩放 + 无旋转**时 `Δworld = b.size` ⇒ 新 = `|b.size.x| × 108` = 旧（差只可能来自 `(a+C) − (b+C)` 的浮点舍入，判据容差是半个像素，够）。
- ⚠️ 反过来说：**父链一旦带缩放或旋转，只有新口对**（缩放时新口跟着缩放、旋转时新口是外接矩形）—— 所以「同值」不是新口的优点条件，只是**换口当天的安全垫**。

---

## 五、`:931` 那条「不是缺陷」的如实记（⛔ 别按形状批量修）

现读 `:931-934`（**本件一个字没碰**）：

```csharp
float tw = tlb.WorldW * 108f;
CheckTrue(tw >= 103.51f - 1.5f && tw <= 153f + 1.5f,
          $"★ 文字框宽落在原版 `ContentSizeFitterMinMax`（`MB_544`）那两条 clamp 之间"
        + $"（`widthMin 103.51` / `widthMax 153` · 实得 {tw:F2}px）");
```

- 它断的是「**文字框宽**落进原版 `ContentSizeFitterMinMax` 的 clamp（103.51 / 153）」——**语义本来就是「框」**（原版那两条 clamp 管的就是布局框宽）⇒ 读 `Label.WorldW`（同一颗 `Label` 上「框」的量化值）**反而是对的**。
- ⛔ **把它一起换成 `TmpRenderedRect` 就是【改判据】**（会变成「字画出来多宽」，与原版那条 clamp 不是同一件事）。
- ⇒ 如实记成「**同形不同义**」：**形状像 A714/A715（都在'框宽'那一段），但不是同一族**。⛔ 别按形状批量化改。

---

## 六、没查清 / 没做的（⛔ 如实标）

1. ⚠️ **这两条断言从没在运行时跑过**（按简报**一次 Unity 都没跑**）⇒ **首跑就是它的第一次**。静态侧我核到了「两条读同一份 `textBounds`、父链单位缩放」（§4·4），**没有实跑复核**。
   **红了先量实得值**：⛔ **别直接改期望值**。三种典型长相 —— ① 报 `-1.0px`（= 我补的那条「前提」也一起红了 ⇒ 网格不在）；② 报**天文数字**（`≈4.29e9` = 空串/未生成时 TMP 的哨兵，见 `TmpRenderedRect` 文件头）；③ 报一个**正常但比旧值大/小**的数（那才是真的版面差异）。
2. ⚠️ **父链缩放我只静态核了这三条**（没实跑）：`Shell/PlayerProfileWindow.cs:214` 只写字段 `extraScaleSmallScreen = 1.075`、**不挂** `TransformScalerBySmallScreenUI`；`Shell/WindowsManager.cs:965` 把窗口根 `localScale = Vector3.one`；`Shell/TransformScalerBySmallScreenUI.cs:67-72` 那个开关**出厂关**（`PlayerPrefs` 缺省 0）+ `Tick()` 只在 `LateUpdate`（批处理**没有帧循环**）⇒ 缩放恒 1。
   ⚠️ **旋转**这一条我**没查**：若哪一颗标签被转过，新口报的是旋转后四角的**外接矩形**（更大）。
3. ⚠️ **点阵后端（`_tmp == null`）口径差**：旧口报 `_texW / PixelsPerUnit`、**新口如实红**（同 A684/A490/A617/A709 的既定口径，已写进两处注释）。
   这六颗档案窗页签 + 社交窗左栏两键**实际走哪条后端**：相邻的 `CheckWrapMode`（认 `Label.CanRenderChinese`）是既有绿断言 ⇒ **期望**是真 TMP；**没实跑证实**。
4. ⚠️ **`lb == null` 那一路的红数变了**：旧码是 `-1f` ⇒ 两条判据红（每键 2 条）；现在是**具名红 + 两条判据红**（每键 3 条）。绿的路上**计数不变**（那条 `if` 进不去）。如实记，免得上位下位看到「红条数 +6」时误判。
5. ⚠️ **没有扫「别的形状」的缓存口**：本件只处理简报点名的两条（§七 只列不改）。
6. ⚠️ **`MenuDraw.Text` 里有没有额外的对齐后置步骤**我没逐行读（A714/A715 两颗标签的 `alignLeft` 都是 `false` = **不挪**）⇒ 「末次刷缓存是哪一句」我按 `SetAutoFitBox(:694)` → `SetWrapping(false)→ForceRelayout(:463)` 的链推的（§4·3 的改坏法落点依赖它）。**没实跑证实**。

---

## 七、顺手发现（⛔ **一个都没改**）

### 7·1 🔴 同族**第三处**（高度那条）：`:1831`

```
CheckTrue(lbL.WorldH * 108f <= 31.6f, $"★ ㉒③ …而且**高度也没溢出** 30.6px 的条…");
```

同一个 `{}` 里，**宽**那半（`:1830` `CheckFits(longName, 172f, …)`）**A709 已经换过口**了，**高**这半还在读 `_tmpH` **缓存** —— 正是 A617 #13 点名的那条风险、与 A714 **同一个病灶**。
⚠️ 这一段自带「**真红法**」（`:1819-1820`：删 `RebuildDeckRows` 里那句 `SetAutoFitBox(…, 8, 29)` ⇒ 高那条红）⇒ 谁要换口，⛔ 别把那句改坏法连着弄丢。
⛔ **不在本件账上、没碰。**

### 7·2 🔴 `:1958`（A685 那处 `nl.WorldW * 108f`）—— 仍没动

WE1e §7·3 已记「同族、不在账上」；本件**也没动**（不在 A714/A715 账上）。如实记：**它与 A714/A715 是同一族**（缓存 ≠ 活值），别当两件事。

### 7·3 ℹ️ 另外两处读数（**不同义**，只是列出来免得误判「漏了」）

- `:7982`（`RenderedRect(FindChild(mcard, "Event Title"), …)` 那条量高）：量的是「暗带顶 → 标题上边缘」的距离，注释自己写着「量的是 `Label.WorldH`（**字形盒**）」——走的是 `RenderedRect` 助手，**语义是「盒」**。
- `:9141-9142`（`cq.WorldW/H … : cl.WorldW/H` 那个兜底三元）：**命中/尺寸**那一族。
- ⚠️ 这两处我**没有逐条判**「该不该换口」——⛔ 没改。

### 7·4 🔴 一句**过期话**：`Shell/MenuWindowBase.cs:505-506`

> 「（四窗这几颗**没有**「渲染宽度 ≤ 框宽」的断言 —— 已 `grep` 核过；同形的两条在 `Editor/MainMenuScene.cs` 的**档案窗**键循环里……）」

**已经不成立了**：本文件 `:5287`（**就是 A715 换口的那一条**）就是**社交窗**左栏键文案的「渲出来的宽 ≤ 框宽 155」，而且它上面 `:5240-5246` 那段块注释（同一批 · 2026-10-08 波 C3 · A212）**正是在给这四窗加【渲染】断言**。
⇒ 一句「当时对、现在不对」的话（铁律 5 那一类）。
⛔ **`Shell/` 不在我白名单 ⇒ 没改**，只报。

### 7·5 🔴 A709/WE1e 那份「不刷缓存」的名单里 **`SetAutoFitBox` 那项不成立**（结论不受影响）

本文件里这句话（或它的「那族」版）**出现 7 处**（现读 `:310`（`TmpRenderedRect` 文件头）· `:2881-2882` · `:2890` · `:6770`（只说「`SetCharSpacing` 那族」）· `:7229` · `:8528-8529` · `:8669`（`CheckFits` 头注释）；⚠️ `:4012` 那处**没有**这个毛病，它只点了 `SetCharSpacing`，**是对的**）；原文形如：

> 「`SetCharSpacing` / `SetWrapWidth` / `SetAutoFitBox` 这一族只 `ForceMeshUpdate()`、**不刷新 `_tmpW`**」

**实测**：`SetAutoFitBox` **末句就是 `RefreshBounds()`**（`Battle/Label.cs:640-695` 的 `:694`）⇒ 它**刷**。
反过来，真正常驻这份名单的应当是 **`SetFontSize`**（`:503-514`，只 `fontSize = w; ForceMeshUpdate();`，**不刷**）—— ✅ 本件**在新写的两段注释里点名了它**（A714 `:2741` / A715 `:5261-5262`），⛔ **但没有去改那 6 处旧话**（那是 A490/A617/A709 的注释，**由调度台裁**；也避免与审查代理撞车）。
**结论不变**：换口只需要「**存在**一条不刷的路」（`SetCharSpacing` 已由 `Battle/Label.cs:524-533` 直接实读证实）。

---

## 八、类型检查结果

```
# ① 两处改完（第一趟）
$ TMPDIR=/tmp/wf_we1f bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0

# ② 注释再收紧之后（第二趟）—— 🔴 **错误全部【不在我的文件上】**
运行时程序集: 4 个错 · 全部在
  Shell/CampaignTab.cs(652,33) / (653,25)  CS0103 找不到 prevSoft / prevClip
  Shell/AllianceMemberTab.cs(445,94)       CS1503 Vector2 → Vector2Int
编辑器程序集: 4 个错 · 全部在
  Editor/ShopScene.cs(4288..4295)          CS0103 找不到 ColNear
（本文件 `Editor/MainMenuScene.cs` **一条都没有** ⇒ 那是**别的写手当时正在写的半成品**，
 处置 = 隔一会儿重跑一次 + 如实记，⛔ **没去改别人的文件** —— 见 `CLAUDE.md` §13·3 第 2 条）

# ③ 等 90 秒重跑（第三趟）
--- 运行时程序集 ---   运行时错误数: 0
--- 编辑器程序集 ---   编辑器错误数: 0
```

- ⇒ **我的改动 = 0 / 0**（第三趟复原即证第二趟那 8 条是别人的瞬时状态）。
  ⚠️ 但**本批有别的写手在飞** ⇒ **同步点那一刻要重跑一次**才算数。
- **行尾**：二进制读 `0 CRLF / **9201** LF`（改前 `0 / 9143`）⇒ **纯 LF 未翻**；全程只用 **Edit** 工具，⛔ 没有 `sed -i`。
- **`git diff --numstat`**（收工那一刻）：`1011  67  Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`
  （**相对 HEAD** —— 里面含 W-E1 / WE1b / WE1c / WE1d / WE1e 的未提交改动，⛔ **别当本件的改动量**；基线 = WE1e 收工的 `948 / 62`）。
  **本件真正加的是 +63 行 / 删 5 行（净 +58，9143 → 9201）** —— 两边账对得上（§一 最后一行）。
  **那 5 条删除**逐条点名：`float wPx = lb != null ? lb.WorldW * 108f : -1f;` · `float hPx = lb != null ? lb.WorldH * 108f : -1f;`（A714）·
  `CheckTrue(clb.WorldW * 108f <= 155f + 0.5f,` · `$"★ …而且**渲出来的宽 {clb.WorldW * 108f:F1} ≤ 框宽 155**"` · `+ "（原版 `Label` 的 `sz=(155,37.86)`；超了就是 auto 没缩够、字冲出去了）");`（A715；最后这条**只是缩进变了**，字面一字未改）。
- ⛔ **没跑 Unity**（简报禁）· ⛔ **没动 git**（只跑了只读的 `git diff` / `git diff --numstat`）· ⛔ **没改正本** ·
  只碰白名单里那一个 `.cs` + 本报告（**`Battle/Label.cs` / `Shell/*` 一律只读**）。

---

*（报告完 · 写手代理 WE1f · 只写了本文件 + `Editor/MainMenuScene.cs`）*
