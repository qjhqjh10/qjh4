# DIAG_A983 — MainMenuScene 12 条红（A833）真根因

> 只读诊断（未跑 Unity、未动 git、未改任何生产/自检代码）。现场 = `d:/4/_tmp_view/menu_1020.log`。
> 🔴 **结论一句话**：12 条红**全部**是**夹具读错了 `TmpSpanPx` 的出参轴**（把 **X** 当 **Y** 用）——
> 是 (α) 断言写错，**不是实现缺陷**，也**不是**「选行选错」。
> 16 处受影响断言里 **12 红 + 4 假绿**（假绿那 4 条虽然没红，同样量错了轴）。

---

## 1 夹具原文（逐字，带行号）

`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`

**① 量法（A833 用的那个）—— `ShellScene.cs:192`**
```csharp
internal static bool TmpSpanPx(Label lb, out float minX, out float minY, out float maxX, out float maxY,
                               bool includeInactive = false)
```
参数序 = **(minX, minY, maxX, maxY)**（内层 `SpanOfTmp`（`ShellScene.cs:241-268`）就是往这四个里填
`p.x→minX/maxX`、`p.y→minY/maxY`）。⚠️ 另一个重载是 `(Transform root, …, out int verts)`（6 参），本处不会选到。

**② 夹具的调用与变量（`MainMenuScene.cs:5820-5822`，排行榜那一块）**
```csharp
float ex1 = 0f, ey1 = 0f, ex2 = 0f, ey2 = 0f, rx1 = 0f, ry1 = 0f, rx2 = 0f, ry2 = 0f;
bool eOk = eLb != null && ShellScene.TmpSpanPx(eLb, out ex1, out ey1, out ex2, out ey2);
bool rOk = rLb != null && ShellScene.TmpSpanPx(rLb, out rx1, out ry1, out rx2, out ry2);
```
⇒ 实际绑定：`ex1=minX · ey1=minY · ex2=**maxX** · ey2=**maxY**`（`r*` 同理）。
**然而下面四条断言把 `ex1`/`ex2` 当成「上沿 / 底」用了**（`eye*` 一次都没被读）：

| 行 | 代码 | 该用 |
|---|---|---|
| 5832 | `CheckTrue(ex2 <= VpBot + 0.5f, $"★ …顶点没有越过视口下沿（实得底 {ex2:F2} …")` | `ey2` |
| 5834 | `CheckNear(ex2, VpBot, 0.5f, $"★★ …TMP 渲染顶点被夹到视口下沿 …")` | `ey2` |
| 5844 | `CheckNear(ex1, ry1 + a833D, 0.5f, $"…上沿仍停在它自己的排版位（参照行上沿 {ry1:F2} + 行距差 {a833D:F2} = … vs 压边行 {ex1:F2}）")` | `ey1` |
| 5848 | `CheckTrue((ex2 - ex1) < (ry2 - ry1) - 5f, $"…真被截短过（参照行字盒高 {ry2-ry1:F2} vs 压边行 {ex2-ex1:F2}）")` | `(ey2 - ey1)` |

`MatchLogRow` 那一块（`MainMenuScene.cs:9252-9281`）**逐字同形**：`9253` 取 `ex1/ey1/ex2/ey2`，
`9265`/`9268`/`9276`/`9279` 同样把 `ex2`（=maxX）当底、`ex1`（=minX）当上沿。

**③ 夹具构建原文（`MainMenuScene.cs:5701-5724`，问 1 要的那几行）**
```csharp
const float A833Over = 55f;
const int A833K = 6;            // 0 起 = 让它当压边行（偏移 0 时建的是 0..5 ⇒ 它得滚一档才进来）
for (int i = 0; i <= A833K + 1; i++) a833Rows.Add(new LeaderboardRowData { … });   // 每行内容一模一样
LeaderboardData.InjectForTest(LeaderboardKind.Classic, LeaderboardTab.Player, a833Rows);
lb.RebuildForTest();
var a833Rs = lb.RowsScroll;
float a833Off = VpTop + A833K * pitch + OrigLbRowH - VpBot - A833Over;   // 反解偏移，⛔ 不写死
if (a833Rs != null) a833Rs.SetOffset(a833Off);
var a833Vp = FindChild(FindChild(lb.transform, "Scroll View"), "Viewport");
var a833C  = FindChild(a833Vp, "Content");
```
每个变量的来源：

| 变量 | 来源 |
|---|---|
| `VpTop=288.59 / VpBot=937.83` | **常量**（`:5549`），值 = `LeaderboardWindow.ScrollR`（`LeaderboardWindow.cs:161`）= 原版数；上面那句 `CheckAtWorld(vp,…)` 已钉过节点在世界上的位置 |
| `pitch = OrigLbRowH + OrigLbRowGap` | `:5550` = `100 + 15`（`OrigLbRowH/Gap` 在 `:10725`） |
| `OrigLbRowH` | `:10725`，行高 100（原版值） |
| `a833Rs` | `lb.RowsScroll`（= `LeaderboardWindow._scroll`） |
| `a833C` | `lb → "Scroll View" → "Viewport" → "Content"`，**按名字找** |
| `a833Off` | 见上（本轮实得 **85.76**，可滚 0..255.76 ✓） |
| `MatchLogRow` 那份 | `MTop=130 / MBot=963`（`:9143`，= `BattleLogPopup` 的视口）· `MK=3` · `MOver=28`（`:9145-9151`）· `mOff` 实得 **26.80** ✓ |

## 2 参照行 / 压边行 是怎么挑的

**按名字 + 按几何极值**（不是按索引）：`:5728-5736`
```csharp
foreach (var rt in a833C.GetComponentsInChildren<Transform>(true))
{
    if (rt.name != "PlayerRankingRow") continue;
    float cy = LayoutSpace.PxY(rt.position.y);
    if (cy - OrigLbRowH * 0.5f >= VpBot) continue;   // 整行在视口之下 ⇒ 不可能是压边行
    if (cy > a833EdgeCy) { a833RefCy = a833EdgeCy; a833Ref = a833Edge; a833EdgeCy = cy; a833Edge = rt; }
    else if (cy > a833RefCy) { a833RefCy = cy; a833Ref = rt; }
}
```
（`MatchLogRow` 那份同名，`:9178-9186`，名字换成 `"Match Log"`。）

**「选行选错」这条**——**不成立**，三条独立证据：
1. 夹具自己算出的行距差 `a833D = a833EdgeCy − a833RefCy` = **115.00**，恰 = `pitch`（100+15）；
   `mD` = **228.20** = 203.20+25。⇒ 两颗是**相邻**的两行，不是两棵不同 Content。
2. 压边行行底 **992.83** = 反解式给的 `VpBot + A833Over`（937.83+55）**逐位吻合**；参照行底 877.83 = 992.83−115 ✓。
   （MatchLog：991.00 = 963+28 ✓，参照行 762.80 ✓）
3. 同一颗行的行底九宫格实测上沿 **889.61** = 行顶 892.83 − `BgR` 的 −3.22 ✓ ⇒ 行节点自身摆位与 `rr` 一致。
另：**没有别的行混进来** —— `RebuildRows` 每次都 `MenuDraw.ClearChildren(_listContent)`，
而它**批处理下走 `DestroyImmediate`**（`Shell/MenuDraw.cs:96-98`）⇒ 上一趟（偏移 0）的行不会留在树里。
⇒ 选出的 edge/ref **正确**。

## 3 坐标系与 115.00 / 228.20 的来源

- `y` 是**屏幕 y 向下**：`PxY(worldY) = 540 − worldY×108`（`Core/LayoutSpace.cs:179`）；`VpTop < VpBot`。
- `VpTop/VpBot` 是**常量**（`:5549`），不是从 `rect` 现读的；其值 = `ScrollR.y1/y2`。
- `115.00` = **行高 100 + 行距 15**（`pitch`），`228.20` = **203.20 + 25**；两者同时被**实测的节点行距差**印证（见 §2）。

## 4 根因链（谁把谁推走）—— 没有推手，是「轮子读错了」

**没有任何东西把压边行推走。那 4 个怪值全部是同一颗字的【X 区间】。** 逐个证：

| 打印出来的「值」 | 真身 | 铁证 |
|---|---|---|
| `Ranking` 压边行「上沿 409.76 / 底 434.195」 | 那颗 `"3"` 的 **minX/maxX** | 行 x = `ListL..ListR` = **360..1560**（`LeaderboardWindow.cs:163`），`RankR` 行内 12..112 + `hAlign=Center` ⇒ 框 = **372..472**、中心 **422**；实测 [409.759, 434.195] 中心 = **421.977**，宽 24.436 ⇒ 「居中一颗数字」✓ |
| `Points` 压边行「上沿 1422.65 / 底 1442.93」 | `"1"` 的 **minX/maxX** | `PointsR` 行内 1060..1200 ⇒ 绝对 **1420..1560**，且 `AlignLeft` ⇒ 字从 1420 起；实测 minX = **1422.649**（= 1420 + 2.65px 左侧字距）✓ |
| `Player Info/Alliance Name`「底 808.610」/「上沿 669.712」 | 该串的 **X 区间** | 半行 `sideR` 右缘 = r.x1+W/2 ⇒ 我方（`AlignRight`）字条右缘 = 该值 −163；实测 ⇒ r.x1+W/2 ≈ **971.6** |
| `Enemy Info/Alliance Name`「上沿 1138.450」 | 同上（另一侧） | 敌方（`AlignLeft`）字条左缘 = r.x1+W/2 **+165**；由实测 minX ⇒ r.x1+W/2 ≈ **970.9** ⇒ **两侧独立算出同一个行心 971.2±0.4** ✓ |
| 「字盒高 27.63 → 138.90（变长）」 | **27.63 是高度、138.90 是宽度** | 同一串 `"Clan One"`（两侧同为 138.898 ✓）、`maxX−minX = 329.84` = `(971.2+165+b) − (971.2−163)` = 328 + 1.84 ✓ ⇒ 就是那串字的墨宽，不是「折了 5 行」 |

⇒ 断言把 **X** 跟 `VpBot/MBot`（**Y**）比 ⇒ 差值自然是几百上千 px（`Ranking`：434 vs 937.83；
`Points`：**1442.93 恰好 > 937.83** ⇒ 连 ★ 那条也红；`Player`：808.61 ≤ 963.5 ⇒ ★ 那条**假绿**）。

## 5 12 条红逐条分类（★ 全部 = (α) 断言写错）

| # | 行 | 断言 | 判 | 判据 |
|---|---|---|---|---|
| 1 | 28650 | `Ranking` ★★ `CheckNear(ex2, VpBot)` | **α** | LHS = `maxX` = 434.195（框 372..472 里那颗居中的 "3"） |
| 2 | 28664 | `Ranking` 只截下沿 `CheckNear(ex1, ry1+a833D)` | **α** | LHS = `minX` = 409.759，被拿去比 Y 值 920.03 |
| 3 | 28743 | `Points` ★ `CheckTrue(ex2 ≤ VpBot)` | **α** | LHS = `maxX` = 1442.932（>937.83 只是因为它靠右） |
| 4 | 28756 | `Points` ★★ | **α** | 同上 |
| 5 | 28770 | `Points` 只截下沿 | **α** | `minX` 1422.649 vs 922.39 |
| 6 | 39996 | `Player Info` ★★ | **α** | `maxX` = 808.610（该字条右缘） |
| 7 | 40010 | `Player Info` 只截下沿 | **α** | `minX` 669.712 vs 949.885 |
| 8 | 40024 | `Player Info` **真被截短过** | **α** | 138.898 是**墨宽**、27.63 是**行高** ⇒ 比的是「宽 vs 高」 |
| 9 | 40089 | `Enemy Info` ★ | **α** | `maxX` = 1277.348 > 963 |
| 10 | 40102 | `Enemy Info` ★★ | **α** | 同上 |
| 11 | 40116 | `Enemy Info` 只截下沿 | **α** | `minX` 1138.450 vs 949.885 |
| 12 | 40130 | `Enemy Info` 真被截短过 | **α** | 同 #8 |

**账目闭合**：这类用法共 **4 组 × 4 条 = 16 条**，其中 **12 红 + 4 假绿**：
假绿 = 28637（`Ranking` ★，434.19 ≤ 937.83）· `Ranking` 真被截短过（24.44 < 40.15−5）·
`Points` 真被截短过（20.28 < 30.97）· 39983（`Player` ★，808.61 ≤ 963.5）。
⇒ **没有 (β)/(γ)/(δ)**：该块是本轮**新增**夹具（commit `41fed28` 引入，首次运行；提交信息自己已记这 12 条红），
夹具的**前提类**断言全绿（选行、偏移可滚、九宫格两态都对）。

## 6 最小改法

**改夹具（只改这一族，不动实现、不动 `A833Over`/`MOver`）**——4 处 × 4 组 = 16 行：

1. `:5832` `ex2` → **`ey2`**；`:5834` `ex2` → **`ey2`**；`:5844` `ex1` → **`ey1`**；`:5848` ⇒ `(ey2 - ey1)`。
2. `:9265` `ex2` → **`ey2`**；`:9268` `ex2` → **`ey2`**；`:9276` `ex1` → **`ey1`**；`:9279` ⇒ `(ey2 - ey1)`。
3. 同批把**消息串**里的 `{ex1}/{ex2}` 一起换成 `{ey1}/{ey2}`（现在它们把 X 印成「上沿 / 底」，下一个人还会被带错）。
4. 顺手把 `ex1/ey1/ex2/ey2` 改名成 `eMinX/eMinY/eMaxX/eMaxY`（`r*` 同理）—— 这 16 处里有 12 处红、
   4 处**假绿**，说明「ex/ey 一字之差」在这段代码里是**看不出来的**。
**理由**：值本身就是 X（§4 三处独立坐实），把 LHS 换成 Y 出参后，期望值（`VpBot` / `MBot` /
`ry1+a833D` / `ry2−ry1`）**才第一次真的在说同一件事**。

⚠️ **口径「（前提）红了就调 `A833Over`/`MOver`、⛔ 别改实现」——本条不适用**：那是为「前提断言红」写的，
而这次红的是**主断言**；且失败量的量级是 **500～2600 px 的「X 与 Y 之差」**，**任何偏移量都改不动它**
（要让 `maxX≈1442` 等于视口底，得把 `VpBot` 挪到 1442 ——那会连带打红一整片别的断言）。⇒ **常量不用动**。

⚠️ **改完必须复跑一次才知道还有没有 β**：这 12 条红**不能证明**实现是对的——
夹具**从没打印过压边行文字的 `minY/maxY`**，所以「TMP 的 y 到底有没有被夹到 `VpBot`」**这一轮没被验到**（见 §7）。

## 7 没查清的部分

1. **生产侧「TMP 文字的 y 真被夹到视口下沿」**：本日志**量不到**（夹具没读 `ey*`）。
   修完夹具复跑才知 ★★/「真被截短过」是绿是红。旁证（倾向能过，但**不是判据**）：
   `ClipQuad`（`Shell/MenuDraw.cs:1302-1327`）的 y 往返 `ToPixel∘FromPixel` 是**恒等式**；
   同一把刀在**图**那一路（`ClipNineChildren` 的 y 截，A833 九宫格两条）**全绿**；
   x 那一路（A781/A822）也绿。
2. **`Ranking` 那条 ★ 断言在修好后仍是「弱」的**：`ey2 ≤ VpBot+0.5` 对「没裁」也会真
   （只要它底本来就在框内）——真正的判别式是 ★★（`CheckNear(ey2, VpBot)`）与「真被截短过」。
3. 顺带（**另立账，不是本 12 条的成因**）：`MenuDraw.ClipQuad` 的写回走
   `LayoutSpace.FromPixel`（x 用**实测** `VisibleWidth`），而读回走 `ToPixel`（写死 **108 px/单位**）
   ⇒ **非 16:9 下 x 往返不闭合**（`Core/LayoutSpace.cs:39/66/78-90/168-179`；y 恒闭合）。
   本轮自检是 16:9，所以看不出来。**只读核对，未验**（没跑 Unity），也没查非 16:9 下谁在兜。
