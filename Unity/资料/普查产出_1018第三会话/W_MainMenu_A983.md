# W_MainMenu_A983 —— 执行写手报告（`MainMenuScene` 族 · `A983` + `A833` 尾巴）

> 只改了一个文件：`Unity/MyGame/Assets/CardPresentation/Editor/MainMenuScene.cs`（`git diff --numstat` = **341 / 38**）。
> ⛔ 没跑 Unity、没动 git、没碰两张正本、没碰 `Shell/**` `BattleEngine/**` 等别人的文件。
> ⚠️ 行号 = **我这一笔落完之后**的现读，别人再动本文件都会漂 ⇒ 请按「节点名 / 语句」认。

---

## 一、`A983` —— 结论

### 1 改了哪 16 处轴（**逐处：符号 / 表达式**）

出参绑定：`TmpSpanPx(Label lb, out minX, out minY, out maxX, out maxY)`（`Editor/ShellScene.cs:192`）
⇒ `ex1=minX · ey1=minY · ex2=maxX · ey2=maxY`。**改的是「哪一对变量被读」，不是算式。**

| # | 族 | 语句（改后） | 改前 | 改后 |
|---|---|---|---|---|
| 1 | 排行榜 | `★ CheckTrue(eMaxY <= VpBot + 0.5f && eMaxY >= VpBot - 0.5f, …)` | `ex2` | **`eMaxY`**（并加下界） |
| 2 | 排行榜 | `★★ CheckNear(eMaxY, VpBot, 0.5f, …)` | `ex2` | **`eMaxY`** |
| 3 | 排行榜 | `CheckNear(eMinY, rMinY + a833D, 0.5f, …)`（「只截了下沿」） | `ex1` / `ry1` | **`eMinY` / `rMinY`** |
| 4 | 排行榜 | `CheckTrue((eMaxY - eMinY) < (rMaxY - rMinY) - 5f, …)`（「真被截短过」） | `(ex2-ex1)` / `(ry2-ry1)` | **`(eMaxY-eMinY)` / `(rMaxY-rMinY)`** |
| 5-6 | 排行榜 | 两条**前提**：`rMaxY + a833D > VpBot + 5f` · `rMaxY < VpBot - 5f` | `ry2` | **`rMaxY`** |
| 7 | MatchLog（弹窗） | `★ CheckTrue(eMaxY <= MBot + 0.5f && … >= MBot - 0.5f, …)` | `ex2` | **`eMaxY`** |
| 8 | MatchLog | `★★ CheckNear(eMaxY, MBot, 0.5f, …)` | `ex2` | **`eMaxY`** |
| 9 | MatchLog | `CheckNear(eMinY, rMinY + mD, 0.5f, …)` | `ex1` / `ry1` | **`eMinY` / `rMinY`** |
| 10 | MatchLog | `CheckTrue((eMaxY - eMinY) < (rMaxY - rMinY) - 5f, …)` | `(ex2-ex1)` / `(ry2-ry1)` | **`(eMaxY-eMinY)` / `(rMaxY-rMinY)`** |
| 11-12 | MatchLog | 两条前提 | `ry2` | **`rMaxY`** |

**改名结果**（诊断原话：`ex/ey` 一字之差在 4 条假绿下看不出来）：
`ex1/ey1/ex2/ey2 → eMinX/eMinY/eMaxX/eMaxY` · `rx1/ry1/rx2/ry2 → rMinX/rMinY/rMaxX/rMaxY` ——
**两族各一份**（排行榜 `foreach` 体内、`MatchLogRow` 的 `foreach` 体内），**新写的 (D) 与 `BattleLogTab` 两块一律用新名**。
`grep '\bex1\b|\bey2\b…'` 现在**只剩**：① 两处**本来是对的** `HitQuadRect` 块（`:5823-5838` 与 `:6920-6923`，那里 `ey2` 确实是「底」）
② 无关的 `TmpRenderedRect`/`RenderedRect` 块 ③ 说明性注释。**A833 那两族里一处都不剩。**

**消息串一并换掉**：原来把 X 印成「上沿 / 底」的 `{ex1}/{ex2}` 全部换成 `{eMinY}/{eMaxY}`（`r*` 同理）——
否则「轴修好了、消息还印错轴的值」，下一个人照样被骗。

### 2 四条假绿：逐条重判 + 处理

| 条 | 轴修好前为什么绿 | 轴修好后还有没有牙 | 处理 |
|---|---|---|---|
| 排行榜 `Ranking` ★ | 拿 `maxX`=**434.19** 比视口底 `937.83` ⇒ 恒真 | **有**：删掉裁切 ⇒ `eMaxY` 回到不裁位 **960.18** > 938.33 ⇒ 红 | **改强**：加下界 `>= VpBot - 0.5f`（挡住「夹过头 / 整段塌掉」），消息改成「**正好停在视口下沿**」 |
| 弹窗 `Player Info` ★ | 拿 `maxX`=**808.61**（该字条**右缘**横坐标）比 `963.00` ⇒ 恒真 | **有**：不裁 ⇒ **977.51** ⇒ 红 | 同上（改成两界） |
| 排行榜「真被截短过」 | `(maxX−minX)`=24.44 恰好 < `35.2−5` | **有**：不裁 ⇒ 字盒高 35.2 ⇒ 不小于 30.2 ⇒ 红 | 保留算式，补**独立坏法**：写成「只改 alpha、不动顶点」⇒ 字盒高不变 ⇒ 红 |
| 弹窗「真被截短过」 | 同族（错轴的两个差值恰好一小一大） | **有**：同上 | 同上 |

⚠️ **如实记**：这四条与 ★★/「只截一侧」在**逻辑上互相蕴含**（夹取是精确的、`VpBot`/`MBot` 是原版常量），
所以我不做「结构上不可能同时满足」那种灭自证断言 —— 它们的价值在**各自不同的失败模式**（不裁 / 整段挪走 / 只改 alpha），
四条都已在源码注释里写清**改坏法**。★ 那两条现在是「两界」，比原来的一界强、但与 ★★ 数值上重合（**有意**，已在注释里说明）。

### 3 两处前科（`REV_SHELL F1` 那一改）—— 注释改成什么

**留公式、改注释**（诊断：公式本身在轴修好后是对的）。两处注释（排行榜 `:6031` 起 · MatchLog `:9571` 起）现在写的是：

> 期望值 `rMinY + 行距`（参照行上沿 + 一个行距），**公式本身是对的** —— 两行内容逐字相同、只差整数倍行距。
> ⚠️ **但旧注释的理由不成立**：它写「照旧写法（`rMinY`）**必红**」—— 红的真因**不是**「压边行比参照行低一个行距」
> （那件事本来就对），而是这 16 处**量错了轴**（`ex1` 恒是一串字的**左缘横坐标**、几百~上千 px）⇒ 拿 X 比 Y 当然红。
> 轴修好后，只截下沿**本来就该**让上沿停在 `rMinY + 行距` ⇒ 这条现在是真的在验语义。

---

## 二、`A833` 尾巴 —— 补了哪三档

| 档 | 落在哪 | 怎么分两态 | 改坏法 | 判别式 |
|---|---|---|---|---|
| ① **`LeaderboardRow.Name`** | **块 (D) 上沿档**（`LeaderboardRow.cs:221` 那句 `MenuDraw.Text(holder, …, "Name", …)`） | 压上沿行 vs **它下面那颗整颗在视口内的参照行**（内容逐字相同） | 把该句的 `clip` 改成 `MenuDraw.NoClip` ⇒ 顶点越过上沿 ⇒ 红 | `CheckNear(eMinY, VpTop, 0.5f)` + `CheckNear(eMaxY, rMaxY − tD, 0.5f)`（下沿不被碰） + `(eMaxY−eMinY) < (rMaxY−rMinY) − 5` |
| ② **上沿档**（同上一块） | (D) | 行 0 顶边 = `VpTop − 28` ⇒ 行内可见 `28..100`；参照行 = 行 1（顶 `VpTop + 87`） | 「整段挪走 / 按比例缩」⇒ 下沿也动 ⇒ 红；「只改 alpha」⇒ 字盒高不变 ⇒ 红 | 同上四条 + 两条前提（`rMinY − tD < VpTop − 5` · `rMinY > VpTop + 5`） |
| ③ **`BattleLogTab` 宿主** | **档案窗第 4 页段末尾**（`Battle Log Tab → Matches → Viewport → Content`；视口 `162.84..887.48`） | 同弹窗那一扇：喂 6 条**内容全同**、偏移反解到 **135.16** ⇒ 行 3 压出下沿 **28px**；参照行 = 行 2 | 九宫格：`Nine` 的 `clip: c.Clip → NoClip` 或删 `ClipNineChildren`；字：`Text(…)` 换成不沿父链解析的内层 / 喂 `NoClip` | 九宫格 `nLive>0 && nHide>0` + `CheckNear(bgBot, 887.48, 0.5)` + 参照行对照；两半边 `Alliance Name` 各跑 `CheckNear(eMaxY, 887.48, .5)` / `CheckNear(eMinY, rMinY+mD, .5)` / 截短 |

**为什么 `Name` 走「上沿档」而不是加进下沿档那两条**（**这是本轮的实质判断，不是省事**）：
- 下沿档可见区是行内 `0..45`；`Name` 的框是行内 `6..52`、字墨约 **14.6..51.8**（`Edge` 的 `g` 有降部；按实测标定的
  「墨底 ≈ 盒心 + 0.36·字号」推），落在沿位 `45` **附近 2px 内**。
- 要让 `Name` 被下沿夹住，得把沿压到行内 `<51.8`；可那样 `Ranking`（墨 `≈32.1..67.4`）/`Points`（`≈35.3..65.5`）
  的**上沿**也一起被夹 ⇒ 它们那两条「只截了下沿」就不成立 ⇒ **两颗字在同一个沿位上互相排斥**。
- 标定用的**实测值**（`_tmp_view/menu_1020.log` 里那几条绿前提）：
  `Ranking` 参照行墨底 **845.18**（= 行内 **67.35**）· `Points` **843.36**（行内 **65.53**）· 弹窗 `Alliance Name` **749.31**（行内 **189.71**）。
- 所以 `Name` 只能**换个方向**：把沿移到行的**上面**，夹它的**上沿**（`eMinY` 那一侧）—— 两个缺口一档销掉。

---

## 三、类型检查

```
$ TMPDIR=/tmp/wf_menu bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```
（连跑两次：写完 16 处轴 / 写完 (D) / 写完 `BattleLogTab` 之后各一次，**三次全 0**。
⚠️ 中途**没有**出现过「错在别人在写的文件上」那一档。）

**行尾 / 规模核对**：
```
$ python -I -c "…b.count(b'\r\n'), b.count(b'\n')"   →  CRLF 0 · LF 11526     # 改前也是 0 CRLF，没翻
$ git -C d:/4 diff --numstat -- …/Editor/MainMenuScene.cs   →  341  38        # 只增不删为主、没有整篇重写
```
改法：**全程 `Edit` 工具**（⛔ 没碰 `sed -i` / python 文本写）。`git status` 里除本文件外的改动**全是别的写手的**。

**⛔ 没跑 Unity**（同工程只能一个实例，只有调度台能跑）。本批的覆盖面 = **`MainMenuScene.Run` 这一条**。

---

## 四、没查清 / 停手的地方

1. **本批断言从没在 Unity 里跑过**（写手不许跑）。最需要复跑那一次盯的是**新加的两块**（(D) 上沿档 · `BattleLogTab`）——
   它们的前提类断言若红，**先按实测读数调 `A833TopOver`（=28，一处）/ `BltOver`（=28，一处）**，⛔ 别去改实现。
2. **`Name` 的字墨纵向位置只有推算**（`RefreshBounds` 把行盒居中 + 一项字墨校正）：
   两种模型（含 `g` 降部 / 不含）给出墨顶 **14.6 / 15.4**、墨底 **51.8 / 43.4**。上沿档取 `28` ⇒
   上沿余量 ≥12px、下沿余量 ≥15px ⇒ **两种模型下都稳**；下沿档则**两种模型下都盖不住**（见 §二）。
3. **`BattleLogTab` 那一扇的 `MaxOffset` 没现算**：我按「6 行 × 203.2 + 5 × 25 − 724.64 = 619.56」推，`bltOff = 135.16` 落在里面；
   这条已配**前提断言**（落得进 `0..MaxOffset`）⇒ 真不对会**大声红**，不会静默。
4. **顺手发现（都没改，交给调度台分流）**：
   a. **本文两族（排行榜 · MatchLog）的九宫格「active 子块并集」是就地内联的——这已经是第 3、第 4 份**（`Shell/MenuDraw` 里那份、
      `:5690-5730` 那份）。要收口得给 `Editor/` 加一个公共 API，超出「只改一个文件」的授权 ⇒ 只报不动。
   b. **`MenuDraw.ClipQuad` 的 x 往返非恒等**（诊断 §7·3 记的那条：写回走 `FromPixel` 用**实测** `VisibleWidth`、读回走 `ToPixel` 写死 **108 px/单位**）
      —— 只读核对、**没验**，也没查非 16:9 下谁在兜。**另立账**，不是本 12 条的成因。
   c. `Shell/BattleLogTab.cs:71-75` 的注释说 `RowCtx.Clip` 那对字段「留着、生产路径恒 null」——
      本轮**确认**该页的裁切确实全落在 `:89` 那颗 `ViewportClip` 节点上（`_scroll.ClipNode = vpVc`），注释与实际一致。
