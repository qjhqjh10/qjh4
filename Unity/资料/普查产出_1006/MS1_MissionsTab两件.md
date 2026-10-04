# MS-1 · `Shell/MissionsTab.cs` 上的两件（A106 残留 3~4 张档 + 最后一处 `CreateNineSlice` 直调收口）—— 执行报告

2026-10-06 · 路径基准 `d:/4/Unity/MyGame/Assets/CardPresentation/`
⛔ 未跑 Unity / `-executeMethod` / 批处理 · ⛔ 未动 git · ⛔ 未改两张正本 · ⛔ 未越白名单（只碰 `Shell/MissionsTab.cs`）
✅ `grep -n "ImageQuad.CreateNineSlice(" Shell/MissionsTab.cs` **零命中**（只剩注释里提到名字的两行）
　`git diff --numstat`：`268 / 33`（含别人先前的 A44 改动）· 行尾实测 **CRLF 0 / LF 1066** ⇒ **没翻行尾**

---

## 件 ① A106 残留 —— 「3~4 张卡那一档没算」

### 一、结论

1. **算了，两档都填进那张对照表**（`Shell/MissionsTab.cs:104-157`）。用的是**同一条算式**（`n` 代进去），
   **没有另起一套**、**没改任何代码常量**（本文件落的仍是【2 张】那一列，一字未动）。
2. 🔴 **n=4 那一档算出来是「放不下」—— 如实记，⛔ 没硬凑数**：
   `tot_pref 1637.1285 > 容器 1519` ⇒ uGUI 把 `if (surplusSpace > 0) { … }` **整段跳过** ⇒
   **`SM` 溢出容器 118.13px、`Daily Missions` 被压成 0 宽**（详见 §三·2）。
3. **算式被两档交叉验证过**：把 `n = 1 / 2` 代进去，算出的 `DM_Pos.x`（`409.0357` / `818.0746`）、
   `DM_Sz.x`（`965.186` / `609.500`）、`SM_Sz.x`（`347.64` / `706.29`）与**本文件已落地的常量 / 派活单给的已知数逐位对上**。
4. **我用两份实现独立复算过**（结论一致，见 §二·3）：① 代数化简版（float64）② 照 uGUI 源码逐句转写的
   `CalcAlongAxis → SetChildrenAlongAxis → SetChildAlongAxisWithScale` 模拟器（float32）。
5. **顺手订正了一句过期记录**（铁律 5）：`Shell/MissionsTab.cs` 里原来写「3~4 张那种实机态**本地算不了**」
   —— 算得了（就是本件算的），**算不了的只是「实机到底挂几张」**。已就地改成带日期的更正痕迹。

### 二、四档并排对照表 + 每一步中间量（可独立复算）

**输入（全部实读，出处见 §四）**：容器（`Normal Missions`）宽 **1519** · 每张卡 `minW **347.64**`（`prefW −1`、`flexW 0`）·
SM 自己那个 HLG 的 `spacing **11.01**` · 两个子件（SM / DM）各 `localScale **1.15**` ·
`DM` 的 `flexW **120**`（`minW/prefW` 都是 −1 ⇒ 当 0）· SM 的 `flexW 0`（**父组 `expandW = 1` 把它抬到 1**）。

**算式**（`n` = `Special Missions` 下挂几张卡；出处 = uGUI 源码，`HorizontalOrVerticalLayoutGroup.cs:87-142` /
`:144-221` · `LayoutUtility.cs:16/29/42` · `ContentSizeFitter.cs:87-104`）：

```
SM_min      = 347.64·n + 11.01·(n−1)      # SM 自己 HLG 的 totalMin；prefW 全是 −1 ⇒ totalPreferred 被
                                          # 收尾那句 max(totalMin, totalPreferred) 抬回 totalMin
tot_min     = tot_pref = SM_min × 1.15    # 父组看到的 SM：min = max(组.minWidth) = SM_min
                                          #                     pref = max(minWidths, preferredWidths) = SM_min
tot_flex    = (1 + 120) × 1.15 = 139.15   # 与 n 无关：SM 那 1 是父组 expandW=1 抬的，DM 是 120
surplus     = 1519 − tot_pref             # surplus ≤ 0 ⇒ mult 保持 0、pos 保持 padding.left（那段 if 整段跳过）
mult        = surplus / 139.15            # minMaxLerp 恒 0（本例 tot_min == tot_pref）
SM 格宽     = SM_min + 1×mult             # 只用来【步进】——SM 自己的 CSF 随后把它的 sizeDelta 钉回 SM_min
SM_Sz.x     = SM_min                      # ← CSF 的结果（ContentSizeFitter: HandleSelfFittingAlongAxis）
DM_Pos.x    = 格宽 × 1.15                 # SetChildAlongAxisWithScale：pivot.x = 0 ⇒ anPos = pos
DM_Sz.x     = 120 × mult
```

**中间量**（float64 与 float32 两版一致到 4 位小数以内）：

| n | `SM_min` | `tot_pref` | `surplus` | `mult` | `SM 格宽` | `SM_Sz.x` | `DM_Pos.x` | `DM_Sz.x` |
|---|---|---|---|---|---|---|---|---|
| 1 | 347.64 | 399.786 | 1119.214 | 8.043220 | 355.6832 | 347.64 | 409.0357 | 965.186 |
| 2 | 706.29 | 812.234 | 706.766 | 5.079170 | 711.3692 | **706.29** | **818.074** | **609.500** |
| 3 | 1064.94 | 1224.681 | 294.319 | 2.115120 | 1067.0551 | 1064.94 | 1227.1134 | 253.814 |
| 4 | 1423.59 | 1637.1285 | **−118.1285** | **0** | 1423.59 | 1423.59 | 1637.1285 | **0** |

**落地进代码那张表的口径**（同一批数的另一组列 —— `Holder x1..x2` / `行左 / 行宽` 是「画布像素」那一层的）：

| 挂几张卡 | `SM_Sz.x` | `DM_Pos.x` | `DM_Sz.x` | `Holder x1..x2` | `行左 / 行宽` |
|---|---|---|---|---|---|
| 1 张（作者态） | 347.64 | 409.0357 | 965.186 | 781.41..1746.59 | 781.41 / 965.186 |
| **2 张（本文件用）** | **706.29** | **818.074** | **609.50** | **1190.44..1799.94** | **1190.44 / 609.50** |
| 3 张（本件新算） | 1064.94 | 1227.1134 | 253.814 | 1599.48..1853.30 | 1599.48 / 253.814 |
| 4 张（本件新算）⚠️ 放不下 | 1423.59 | 1637.1285 | **0** | **2009.50..2009.50** | **2009.50 / 0** |

> `Holder x1 = 372.37 + DM_Pos.x`（`372.37` = `Normal Missions` 左边缘）· `Holder x2 = x1 + DM_Sz.x`
> —— 与「2 张」那一行原来的写法一致（`372.37 + 818.0746 = 1190.44` ✔）。

**自证恒等式**：`DM 右边缘 = DM_Pos.x + DM_Sz.x × 1.15` ——
`n = 1 / 2 / 3` **恒等于 1519.00**（布局刚好填满容器；`flexible` 把 `surplus` 分完就必然如此），
`n = 4` 是 **1637.13**（= `SM` 自己那一格宽 × 1.15，**比容器右边缘超 118.13**）。

### 三、🔴 阶跃警告（派活单点名要的那条）

1. **`n = 3` 没有任何异常**：`surplus 294.319 > 0` ⇒ 正常走 flexible 分配；
   `DM` 拿 `253.814` 宽、`SM` 拿 `1064.94`（CSF），两者之间那条
   `(格宽 1067.0551 − 首选 1064.94) × 1.15 = 2.43px` 的缝**是原版就有的**（同「2 张」那档的 5.84px）。
2. **`n = 4` 是「放不下」那一档**（**不是换行、也不是把卡缩窄**）：
   - `surplus = 1519 − 1637.1285 = −118.1285 ≤ 0` ⇒ uGUI 的
     `if (surplusSpace > 0) { … }`（`HorizontalOrVerticalLayoutGroup.cs:185-193`）**整段不执行** ⇒
     `itemFlexibleMultiplier` 保持 `0`、`pos` 保持 `padding.left`（= 0）；
   - ⇒ `SM` 拿它的**最小宽 1423.59**（画出来 `×1.15 = 1637.13`，**超出容器右边缘 118.13px**）；
   - ⇒ `DM` 拿 `0 + 120×0 = **0**` 宽、起点 `1637.13`（**已在容器之外**）⇒
     **每日任务那一整块（卡头 + 三行）在原版那一帧里 0 宽、根本画不出来。**
3. **这一档本地判不了会不会真出现**：`ContainerHolder.TryAdd<object>` 是「逐条加、加满 `maxAmount`（= 4）
   就返回 `false` 跳过」（反编译 `ContainerHolder__TryAdd_object_.c:22-27`；
   `MissionsTab__InstantiateNormalMissions.c:26-31,79-86` 是「服务端给几条就逐条 add」）
   ⇒ **实机挂几张由服务端任务数据定**。
   ⛔ 所以「4 张」那一行是**按同一条算式算出的预测**，**不是实机观测** —— 报告与代码注释里都写明了。
   （若实机真挂到 4 张，原版画面就是 §三·2 那一副：**右溢出 + 每日那列消失**。）
4. **建模的四条前提**（本件逐条查过，写进了代码注释；哪条不成立，整张表要重算）：
   ① 容器宽恒 **1519**（`Normal Missions` 的 RT 非拉伸 + 只有 RT/HLG 两个组件 + `MissionsTab` 全部方法里
   没有一处写 `sizeDelta`）；② 每张卡 `minW` 都是 **347.64**；③ 两个同名 `Normal Missions` 实例的 HLG 参数逐字段相同；
   ④ 卡最多 **4** 张（`maxAmount`）。

### 四、证据（`文件:行号` / 资产路径）

**① 原版参数（直接读 prefab JSON，`d:/2/新解包资源/assets_full/bundle_menus_assets_all`）**

| 对象 | 资产 | 实读值 |
|---|---|---|
| `Normal Missions` 的 RT | `RectTransform/RectTransform_527428140916741887.json` | `anchorMin = anchorMax = (0,1)`（**非拉伸**）· `sizeDelta (1519, 723.8)` · `pivot (0, 0.5)` · `scale 1` · 父 = `RT 6703300887156823807`（`Missions Tab`） |
| `Normal Missions` 的组件表 | `GameObject/Normal Missions.json` | **只有 2 个组件**：[RT 527428140916741887, MB 5862962067366549247] ⇒ **没有** CSF / LayoutElement / AspectRatioFitter |
| `Normal Missions` 的 HLG（树里那个实例） | `MonoBehaviour/MonoBehaviour_5862962067366549247.json` | `m_Spacing 0 · m_ChildAlignment 0 · m_Padding 0 · ctrlW 1 ctrlH 0 · expandW/H 1 · scaleW/H 1 · reverse 0` |
| `Normal Missions` 的 HLG（另一个同名实例） | `MonoBehaviour/MonoBehaviour_-8523192195473755463.json` | **逐字段相同**（⇒ 不存在「多实例不同值」） |
| `Special Missions` | `GameObject/Special Missions_8671611484050626303.json` | 组件 = [RT −3954402248558504193（`sizeDelta 779.21×556.223` · `scale 1.15` · `pivot (0,1)`）, Holder −7417265825195092225, CSF 3054603845766420223, HLG 5562888175203587839] —— **没有 `LayoutElement`**（⇒ SM 的灵活值只能来自 HLG = 0，父组 `expandW` 抬到 1） |
| SM 的 CSF / HLG / Holder | `MonoBehaviour_3054603845766420223.json` · `MonoBehaviour_5562888175203587839.json` · `MonoBehaviour_-7417265825195092225.json` | `hFit 2 (PreferredSize) vFit 0` · `spacing 11.01 align 3 pad 0 ctrlW/H 1 expandW/H 0 scaleW/H 1` · `maxAmount 4` |
| SM 的两个子件（卡） | `RectTransform_-8131297245380733185.json` · `RectTransform_3053782671748830975.json` | **`localScale 1.0`**（⇒ 卡的 `minW` 不用再乘缩放） |
| 登录卡 / 骷髅卡的 `LayoutElement` | `GameObject/Daily Login Container.json` → MB `-8734388465245769031` · `GameObject/Daily Skulls Mission Container_6434331890599441081.json` → MB `-761378385545022791` | `minW 347.64 · prefW −1 · flexW 0 · minH 555 · priority 1` |
| **其余**带 `LayoutElement` 的任务卡 prefab（本件新扫） | `… Small` · `… Variant  Expansion Pass` · `Mission Container Vertical` · `Weekly Mission Container`（+ 上面两张） | **一律** `minW 347.64 · prefW −1 · flexW 0 · minH 555` ⇒ 前提②对「挂的是哪几张」不敏感 |
| `Daily Missions` 的 `LayoutElement` | `MonoBehaviour/MonoBehaviour_3509833203841701631.json` | `minW −1 · minH −1 · prefW −1 · prefH −1 · flexW 120 · flexH −1 · priority 1` |

**② 算法出处（本地 uGUI 源码，逐行读过）**
`MyGame/Library/PackageCache/com.unity.ugui@27635d171b1a/Runtime/UGUI/UI/Core/Layout/`：
- `HorizontalOrVerticalLayoutGroup.cs:87-142`（`CalcAlongAxis`：`totalMin += min + spacing` / 收尾 `max(totalMin, totalPreferred)`；
  ⚠️ `useScale` 那条把子件的 min/pref/flex **乘子件 `localScale[axis]`** —— 本件两个子件都是 1.15）
- 同文件 `:144-221`（`SetChildrenAlongAxis`：`surplus = size − totalPreferred` · **`:185-193` 那个 `if (surplusSpace > 0)`** ·
  `childSize = Lerp(min, pref, minMaxLerp) + flexible × mult` · `pos += childSize × scaleFactor + spacing`）
- `LayoutGroup.cs:249-267 / 291-317`（`SetChildAlongAxisWithScale`：**`sizeDelta` 就是 `size`**，只有位置那项乘 `scaleFactor`）
- `LayoutUtility.cs:16 / 29 / 42`（`GetMinSize` / `GetPreferredSize`（= `max(minWidths, preferredWidths)`）/ `GetFlexibleSize`）
- `ContentSizeFitter.cs:87-104`（`PreferredSize ⇒ SetSizeWithCurrentAnchors(axis, GetPreferredSize)`）

**③ 反编译（`d:/2/tools/decomp_full/`）**
- `ContainerHolder__TryAdd_object_.c:22-27`（`if (0 < maxAmount && maxAmount <= 已有数量) return 0`）
- `MissionsTab__InstantiateNormalMissions.c:26-31`（选哪个 holder）· `:79-86`（逐条 `TryAdd` + `SetChallenge`）
- `grep -l "SizeDelta\|SetSizeWithCurrentAnchors\|sizeDelta" MissionsTab__*.c` ⇒ **零命中**（9 个方法全查）⇒ 没有谁在运行期改容器宽

**④ 本件落点**
`Shell/MissionsTab.cs:104-157`（四档对照表 + 中间量 + 阶跃警告 + 四条前提）· `:166-169`（过期记录的就地更正）

### 五、改动清单（改前 → 改后）

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| 1 | `Shell/MissionsTab.cs` A106 卡片数表 | 三行（2 张 / 1 张 / **「3~4 张 = 没算（另开一件）」**） | **四行**（2 / 1 / **3** / **4**）+ 4 张那行标 ⚠️**放不下** |
| 2 | 同上，紧随其后 | —— | 新增：**中间量算式与四档逐项表**（`SM_min` / `tot_pref` / `surplus` / `mult` / `格宽` / 三个落点值）+ 精确值 + 「1/2 张与落地常量逐位对上」那句自证 |
| 3 | 同上 | —— | 新增：**n=4 阶跃段**（uGUI 跳过那段 `if`、SM 溢出 118.13px、DM 0 宽）+ 「本地判不了实机挂几张 ⇒ 这是预测不是观测」 |
| 4 | 同上 | —— | 新增：**四条前提**（容器宽恒 1519 / 每张卡 minW 347.64 / 两实例参数相同 / `maxAmount = 4`） |
| 5 | `Shell/MissionsTab.cs:166-169` | 「3~4 张那种实机态**本地算不了**（服务端任务数据）」 | 「**3 / 4 张那一档现在按同一条算式算出来了**（见上表 —— ⚠️ 那是算式预测…）」+ 🔴 日期更正痕迹（铁律 5） |

⛔ **代码常量一个都没动**（`SM_Sz` / `DM_Pos` / `DM_Sz` / `SM_Scale` / `DM_Scale` 全部原样）·
⛔ 没动 `Editor/RewardsScene.cs` 的断言（本文件仍落 2 张那一列，断言无需变）。

---

## 件 ② `Shell/MissionsTab.cs` 里最后一处 `ImageQuad.CreateNineSlice` 直调收口

### 一、结论

**做了。** `MissionsTab.BuildNine(...)`（包装，`Shell/MissionsTab.cs:925-948`）体内那一句直调
`ImageQuad.CreateNineSlice` 已改调公共件 `MenuDraw.Nine`（`:946-947`），
**签名一字未改**（8 个形参原样）⇒ **调用点零改动**（`BuildBar` 里那 2 处，`:917` / `:921`）。
收口后本文件 `grep -n "ImageQuad.CreateNineSlice("` **零命中**（只剩注释里提到名字的两行）。

**逐项等价（四样，都核过）**：

| 项 | 旧代码 | 新代码（`MenuDraw.Nine` 内部） | 等价判据 |
|---|---|---|---|
| **矩形** | `Local(parent, r.x1…r.y2)` + `LayoutSpace.Px(r.W)` / `Px(r.H)` | **同一句**（`Shell/MenuDraw.cs:781-782`） | 逐字相同 |
| **落位** | `RewardsWindow.Local(…)` | `MenuDraw.Local(…)` | **两份算式逐字同源**：都是 `LayoutSpace.RectCenter(…) − parent.position`（`Shell/MenuWindowBase.cs:231-232` vs `Shell/MenuDraw.cs:25-26`）⇒ **不需要**「`parent.position == 0`」那条前提（与卡组编辑那处不同：那边旧代码用的是**绝对世界点**） |
| **队列 / tint** | 建完 `foreach(GetComponentsInChildren<ImageQuad>()) { SetTint(tint); SetRenderQueue(QContent); }` | `foreach(GetComponentsInChildren<ImageQuad>()) { if(tint) SetTint; SetRenderQueue(q); }` | 同一个 `q = RewardsWindow.QContent`（= 3010，`Shell/MenuWindowBase.cs:72`）· 本包装 `tint` 是**非空**形参 ⇒ 两边都设、**先后也同**（先 tint 后队列）· 两边都用**不含未激活件**的那个重载 ⇒ 子块集合相同 |
| **九宫切边 / 块数** | `border` 同时喂第 3 与第 10 个实参（`borderOutPx`） | `border` + `borderOutPx = null ⇒ ?? border` | 同值；块数 / uv 切分 / 每块 `SetAspect` 都是**同一句 `CreateNineSlice`**（`Battle/ImageQuad.cs:330`）⇒ 全同 |

**一条语义差（`tex == null` 返回什么）在本处踩不到**：`ImageQuad.CreateNineSlice` 会**照样返回一个空根节点**
（`Battle/ImageQuad.cs:335-338`，只多一条告警），`MenuDraw.Nine` **返回 `null`**（`Shell/MenuDraw.cs:776`）
—— 判据见 `资料/已知的坑.md` 2026-10-06 那条；本包装**头一句就是 `if (tex == null) return;`**，
且它是 `void`、又不读返回值 ⇒ **两种语义都碰不到调用方**。这句早退我保留了，并把这条理由写进了注释。

### 二、改动清单（改前 → 改后）

```csharp
// 改前（Shell/MissionsTab.cs 的 BuildNine 体内）
if (tex == null) return;
var go = ImageQuad.CreateNineSlice(parent, tex, new Vector4(border, border, border, border), texW, texH,
                                   RewardsWindow.Local(parent, r.x1, r.y1, r.x2, r.y2),
                                   LayoutSpace.Px(r.W), LayoutSpace.Px(r.H), "Nine",
                                   new Vector4(border, border, border, border), fillCenter);
if (go == null) return;
foreach (var q in go.GetComponentsInChildren<ImageQuad>())
{
    q.SetTint(tint);
    q.SetRenderQueue(RewardsWindow.QContent);
}

// 改后（签名一字未改；`foreach` 删掉 —— 它设的两个值与公共件内部设的同值）
if (tex == null) return;                       // ← 保留了（理由见注释：挡住 `null` vs 空节点那条语义差）
MenuDraw.Nine(parent, tex, r, new Vector4(border, border, border, border), texW, texH,
              RewardsWindow.QContent, tint, fillCenter, "Nine");
```

### 三、没查清 + 顺手发现（两件共用）

1. **没查清（件①）**：**实机到底会挂几张卡** = 服务端任务数据，**本地判不了**（同 A106 那份报告的口径）。
   ⇒ 3 / 4 张两行是**算式预测**；真正能验的只有「实机跑一次看是不是这样」，我们跑不到（原版已关服）。
2. **没查清（件①）**：**「第 3 / 第 4 张是什么任务卡」本地判不了**。我扫了 bundle 里**全部**带 `LayoutElement`
   的任务卡 prefab，**一律 `minW 347.64`** ⇒ 前提②对「挂哪几张」不敏感；
   ⚠️ **但有一型没有 `LayoutElement`**：`Daily Mission Container`（就是每日任务那三行的行体，组件表里只有
   RT + Graphic + `MissionContainer` 脚本 + `displayRule`）—— 它的 `min/pref/flex` 全被 uGUI 当 0。
   若实机往 SM 里挂的是**这一型**，上表整张要重算。我**没查**「服务端会给 SM 分配哪一型」（查不到）。
3. **顺手发现（只报，未动）**：`Shell/MenuWindowBase.cs` 这个**文件名**与它里面的**类名**不一致 ——
   那个文件里声明的类是 **`MainMenuSubmenuWindow : GameWindowWithTabs`**，而全仓 `.cs` 里
   **`class MenuWindowBase` 根本不存在**（`MenuWindowBase` 只出现在注释里）。本件的等价性论证要靠
   `RewardsWindow.Local` 的出身，所以顺手核了这条链：`RewardsWindow : MainMenuSubmenuWindow : GameWindowWithTabs :
   GameWindow : MonoBehaviour`（`Shell/RewardsWindow.cs:181/134`、`Shell/WindowsManager.cs:60`）。
   ⛔ 属别人的文件、且只是命名漂移，**没动**。
4. **顺手发现（件②，只报）**：`MissionsTab` 自己**不是** `MainMenuSubmenuWindow` 系
   （`MissionsTab : WindowTabBase`），却调 `RewardsWindow.Local(...)` —— 那是**继承来的静态方法**
   （C# 允许从派生类型名调）⇒ 等价性成立，但读代码时容易误以为 `RewardsWindow` 里有一份自己的 `Local`。
   本件把判据（两份 `Local` 逐字同源）写进了注释，免得下一个人重新推一遍。
5. **既有的未修项（不是本件范围，别误当成我漏的）**：`DM_Scale = 1.15f` 声明了却**从未被引用**
   （`Daily Missions` 的 `localScale` 我们没作用到子树 ⇒ 那一块渲染小 15%）—— A106 报告 §四·2 已上报，本件不碰。

---

**类型检查：运行时 0 / 编辑器 0**（`TMPDIR=/tmp/wf_ms1 bash d:/4/Unity/工具/typecheck.sh`，两件落地后各跑一次，末次 0/0）。
未跑任何 Unity 批处理（主对话在同步点统一跑；本件只动了一个宿主文件 `Shell/MissionsTab.cs` ⇒ 按铁律 12 对应的是 `RewardsScene.Run`）。
