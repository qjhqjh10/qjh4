# DIAG 场上卡缺近战/远程/血量

## 一、结论（3–5 行）
1. **现象属实，且不是「场上全都没有」**：场上（`CardFace.Board`）四个数字**各占一张独立 quad**（`CardView.cs:5331-5384`），**会逐格丢**——同一张卡上有的格有、有的格没有；**跟督军/部队、阵营、稀有度无关**（实拍里同一种部队卡两张，丢的格都不同）。
2. **三个独立成因**：① 🔴 **「合法目标底光」把近战/远程数字盖掉**（**已钉死**，见 §三之3）② 🔴 **四个数字 quad 离 3D 卡体正面只有 0.012 卡单位**（= 卡高的 0.6%），是「平时也丢格」的头号嫌疑（**未最终证实**，见 §五·2、§六）③ ⚠️ **徽标底板 z=−0.095 比数字的 −0.09 更靠前**，7 个徽标时正好压住**护甲**那一格（静态可证，§五·3）。
3. **是缺陷**（原版场上四格都在，见 §四）。**自检一直没抓到**，因为那条断言（`Editor/BattleScene.cs:1006-1013` 的 `StatShown`）只读 `activeSelf`/数值，**读不到「屏幕上有没有」**——同一段日志 `_tmp_view/s22_BattleScene.log:4495` 报 `[3, 2, 1, 5]` 全绿，而同一次渲染出来的场卡（`_tmp_view/battle/28c_数值_掉血变色.png`，**确定性渲染、无战斗状态**）只画出了近战那一个数。

## 二、分类判据（谁会缺 / 谁不会）
**按代码**：`SyncStatLayers` 只有四个跳过分支（`CardView.cs:5370-5374`）——① 不是 Board 形态 ② `Data.isUnit == false` ③ **护甲格且 `armour <= 0`**（原版就这么做，`InfoTexture:5232` 同一条判据）④ `PragatiDigits.Available == false`（全局，不会单独发生）。
⇒ **只有「护甲 0」是「该没有就没有」；近战/远程/生命在任何单位卡上都该有。没有任何按卡分档的判据。**
**按实拍**（逐张放大亲读；放大图我这次生成在 `_tmp_view/diag_stats/`，原图见下）：
| 卡 | 近战 | 远程 | 生命 | 原图 |
|---|---|---|---|---|
| 敌方左部队（兽人） | ✗ | ✗ | ✅「2」 | `_tmp_view/s22_11_play.png`(1460,600) |
| 敌方中战将（兽人） | ✅「2」 | ✗ | ✗ | 同上 (1213,608) |
| 敌方右部队（兽人） | ✅「1」 | ✅「2」 | ✗ | 同上 (1415,609) |
| 我方战将（复仇者之刃） | ✗ | ✗ | ✗ | `_tmp_view/s22_00_battle.png`(1265,880) 与 `s22_11_play.png`(1250,1000) **两次都一样** |
| 灵族（自检渲染） | ✅「3」 | ✗ | ✗ | `_tmp_view/battle/07_触发效果_Rally.png`、`battle/28c_数值_掉血变色.png` |
⇒ 规律 = **上面两格（近战 y=−0.926 / 护甲 −0.894）比下面两格（远程 −1.260 / 生命 −1.309）活得久，但不严格**（左部队正好反过来，因为它的近战/远程被底光盖）。**给不出「卡池里会缺的卡清单」——它不取决于卡**，取决于这一帧（a）谁被点成合法目标（b）那几格的深度判定翻没翻。

## 三、我们侧实现（两套脸型各自的数值链 + 每条跳过分支）
1. **场外（`CardFace.Full` / `Hand`）**：四个数**烘在一张 `_info` 贴图里** —— `InfoTexture`（`CardView.cs:5143`）的 `if (d.isUnit && !skipStats)`（`:5232`）逐个数调 `DrawStatAt`（`:5553`）。**这条路不会单独丢格**（同一张图，要么全在要么全无）；战术卡不画（`isUnit=false`）。
2. **场上（`CardFace.Board`）**：`SplitStats(d)`（`:953`）= `board && d.isUnit && PragatiDigits.Available`；为真时 `_info` 烘成**全透明**（`:5168`/`:5232` 两块都被跳过），改由四个独立层画：
   · 创建 `BuildStatLayers`（`:5331-5357`，64² 画布 `StatTexture` `:5306`，`CenteredQuad` `:1662`，当场 `StampQueue`）
   · 刷新 `SyncStatLayers`（`:5359-5384`）：每格 `SetActive(show)` / 位置 `StatAt01(s,true)` / 贴图 / 颜色 `_tint * _statFlash[s]`
   · 位置 = `BoardMeleeAt(0.1751,0.7779)`·`BoardRangedAt`·`BoardArmourAt`·`BoardHealthAt`（`:226-229`）
   · **z 全部 = `BoardInfoZ = -0.09f`**（`:1533`）；护甲盾 `BoardArmourIconZ=-0.088`；徽标底板 `-0.095`/图标 `-0.10`/角标 `-0.105`（`:1524-1535`）
   · 卡的**每一层共用一个渲染队列号**（`SetQueue` `:705` 扫整棵子树），号由 `BoardLayout.QueueOf(格)` 给（`Board/BoardLayout.cs:113-142`，段顶 2979）⇒ **卡内层序靠 z / 同 z 靠到相机的距离**。
3. 🔴 **成因①（已钉死）**：合法目标底光 `_glowMelee/_glowRanged` 由 `MakeGlow`（`:3971-3980`）建，用的是 `ImageQuad.Create`（`Battle/ImageQuad.cs:113-130`）——**它只 `new Material(Shader.Find("Sprites/Default"))`，一个字节都没写 `renderQueue`** ⇒ 生效队列是 SubShader 的 **`Transparent` = 3000**，而 `MakeGlow` **没有 `StampQueue`**（别的每一层都有）。底光是**懒建**的（`SetTargetGem`→`EnsureGlow`），调用点 `BattleDriver.cs:8116-8130 HighlightTargets`，**排在最后一次 `SyncBoard` 的 `SetQueue`(11368) 之后** ⇒ 底光整张画在卡的所有层**之上**；而这两张 sprite **中心是不透明的**（`Art/原版/battleatlasui/40K_melee_glow.png`、`40K_ranged_glow.png` 亲读：红盘/紫准星圈 + 不透明深色内芯）⇒ **把近战/远程那两个数字涂掉**。⇒ 实拍里「亮红盘 / 紫准星圈、里面没有数」的那两格就是它。（下一次 `SyncBoard` 会把它盖回本卡的号 ⇒ 症状只在「选目标」那一段持续——实拍那两张正是选目标期间。）

## 四、原版侧（场上显示哪几个数值、怎么排、出处）
- 原版场上**四格都在**（近战/远程/生命 + 护甲有值才画），**不是贴在网格的计数器面板上**，而是**四个独立的世界空间 TMP 文本节点**：`Melee AttackText` / `Range Attack Text` / `HealthText` / `Armour Text`，挂在 `3DBody` 下、与网格 `Card 3D` **平级**（所以**不乘 0.88586**）。位置（3DBody 空间，y=0 = 卡底）与 anchor 字段：
  `资料/3DBody_原版场上卡体规格.md:187-190` —— melee (−0.680, 0.740, **z −0.077**) · range (−0.434, 0.406, **−0.071**) · health (0.566, 0.357, **−0.080**) · armour (0.678, 0.772, **−0.063**)；anchor = `dump.cs` 的 `body3DMeleeAnchor:26179` / `body3DRangeAnchor:26178` / `body3DHealthAnchor:26180`（**护甲没有** anchor 字段，同资料 :190）。⇒ 我们的 `BoardMeleeAt…` 与这四个 x,y **逐位对上**（`:212-229` 的换算），**没有偏离**。
- 实拍里看到的「深色内嵌格 / 紫圈 / 绿框」**是 3D 卡体自带的计数器面板**（mesh 的 UV2.x==1 那 111 个顶点，shader `lerp(matcap着色, _BaseMap×UV2.x×_CountersIntensity, UV2.x)`，`_CountersIntensity=1.12` —— 同资料 `:37`/`:41`），**不是我们画的**；所以「底子是深色」不代表「我们画了数字」。
- 原版拿这四个数做的唯一动作 = `HighlightAttackType` 把那一格放大 **1.35×**（同资料 `:196`）。⇒ **原版场上没有「缺一格」这回事。**

## 五、改动清单
1. 🔴 **`Core/CardView.cs:3971 MakeGlow`**：建完紧跟一句把这张卡当前的队列号盖上（`q.SetRenderQueue(Queue)`，或直接复用 `StampQueue` 那条口）——⛔ **不要另发明一个号**（`SetQueue` 是「每张卡一段队列」的唯一实现）。这样底光与数字同号，靠 z 定序（数字 −0.09 比底光 −0.01 **更靠近相机** ⇒ 数字在上，和原版 `SortingOrder 比数字小` 同义）。风险：低（只是把该层拉回它本该在的那一段）。**改完必须并排渲一张带底光的场卡核**。
2. 🔴 **`BoardInfoZ`（`:1533`）与整叠覆盖层的间隙**：现在数字离卡体正面只有 **0.012**（卡体正面 = mesh z +0.088 × 0.88586 = **−0.078**，出处 `资料/3DBody…:32` 的 mesh 实读 + 我们导入那份 `.asset` 的 `m_LocalAABB` 同值）。建议把 `BoardInfoZ / BoardArmourIconZ / Badge{Plate,Icon,Counter}Z` **一起**往相机方向挪到留出明确间隙（如 −0.13…−0.16），**保持现有相对次序**。⚠️ 原版判据**只给相对关系**（数字在卡体正面之前），绝对值要多少**是我们挑的** ⇒ 代码里照铁律 3 写明「这是我们挑的」。⚠️ 改前先留场景副本，改后跑 `BattleScene.Run` + `CardBaseDemo.Run`（**串行**）+ **并排渲图**。
3. ⚠️ **徽标底板压住护甲格**：底板 `BadgePlateZ=-0.095` **比数字 −0.09 更靠前**，右列第 4 行的底板 rect（x 0.236…0.788 · y −0.978…−0.277，`Core/Badges.cs:100-131,179-191`）**正好罩住护甲格 (0.678, −0.894)** ⇒ 7 个关键词徽标的单位护甲数会不见。等 §五·2 一起挪 z 时把次序理顺（数字仍在底板之后）。
4. **自检**：`Editor/BattleScene.cs:1006-1013` 只读 `activeSelf`，**永远绿** ⇒ 补一条**渲图数像素**的断言（同族先例：`CardFaceProbe` 的 ink 量法「断渲染宽度 ≤ 框宽，别只比字号」）。⛔ 不改判据本身（`StatShown` 那几条测的是行为，仍要留）。

## 六、没查清的部分（⛔ 不猜）
- **「平时也丢格」的最终机制**我没钉死。我算过的两条：数字 quad 平面在卡体正面**之前** 0.012；卡体**滚圆底边**只占 3DBody y 0…0.17（= 卡 y < −1.515），而最低那格（生命）**字形**底边在 −1.423 ⇒ **按几何字形不该被卡体挡住**；24 位深度缓冲在 14.6 单位处精度 ~4e-5，也解释不了 0.012。**我没做**：没在真机读回这四张 quad 的 `activeSelf`/`localScale`/`renderQueue`/材质色，也没渲过「把 z 拉开之后」的对照图。⇒ §五·2 是「把间隙拉开」的处置，**不等于「已证明是间隙造成」**。
- 已排除的几条（都读过，别再查）：字形表不缺字（`Core/PragatiDigits.Data.cs` 20 条，`0-9 thin` + `0-9 thick` 全在）· `isUnit` 在真实对局的场上恒真（`BattleDriver.cs:11809-11838 ToCardData(UnitState)` 恒 `isUnit = true`）· 卡本身的自检日志零异常（`s22_BattleScene.log` 里 `Exception` 0 条）· `_info` 在场上烘的是全透明（`:5168`/`:5232` 两条都跳过）· 软光影/软阴影 z 在卡后（`ShadowZ=0.06`）。
- `_statFlash` 会不会在某条路径上停在非白（把数字涂深）——静态看不出（`ClearStatFlashes` 每轮先刷白），**没在真机核**。
- 远端限制：卡 SO 本体（`RawCardScript`）在远端 Addressables 包、本地没有；本次**不需要**它（判据全取自本地：`CardView.cs` / `Badges.cs` / `BoardLayout.cs` / `Card 3D WH40k.obj` 与导入的 `.asset` / `40K_*_glow.png` / `资料/3DBody_原版场上卡体规格.md`）。
