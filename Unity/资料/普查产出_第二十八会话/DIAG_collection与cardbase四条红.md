# DIAG —— `CollectionScene` 2 条红 + `CardBaseDemo` 2 条红（只读诊断 · 第二十八会话）

> 只读；未跑 Unity、未动 git。判据一律现读（`git show` / 日志 / 源码 / 已存盘的场景）。
> ⚠️ `[诊断入口过期]`（242 vs 240）两条**不在本文范围**（生成物要重跑 `工具/gen_diag_runner.py`）。

## 逐条判定

### 红 1 · `✗ 格里的立绘贴图 = (一个 alt_* 都没有)`（`collection.log:33996`，失败重列 `:60049`）
**判定 = (β) 真实回归 —— 但不是本会话 A1808 打的；归因到【第二十六会话 · 提交 `601005b`（2026-10-14 00:09「卡牌 id 整族换原版真值」）】。从本会话看，它是一条「HEAD 上就已经红着」的红。**

1. 断言在 `Editor/CollectionScene.cs:7326-7337`：扫第 1 格（`StyleCells[0]`）**全部** `MeshRenderer.sharedMaterial.mainTexture.name`，要求出现 `alt_` 开头。
2. `Shell/CollectionWindow.cs:1508-1517` 的 `AltArtCards[0]` 现读 = **`AM2`**；`Core/CardArt.cs:719-721` 的 `AltArt` 拼的是 `Resources/Art/altarts/alt_<id 小写>` ⇒ 要 **`alt_am2`**。
3. **盘上那 7 张 PNG 还是旧 id 的名字**（`Resources/Art/altarts/`，mtime **2026-09-24 06:56**）：`alt_am5 / alt_bl1 / alt_da3 / alt_gof3 / alt_sau1 / alt_sw1 / alt_um_lieutenant_titus` —— **没有 `alt_am2`**。
4. `git show 601005b^:Shell/CollectionWindow.cs` 那张表 = `AM5 / BL1 / DA3 / SAU1 / GOF3 / SW1 / UM_Lieutenant_Titus` ⇒ **改前 `show[0]` = `AM5`、`alt_am5.png` 在 ⇒ 这条是绿的**。`601005b` 只换了 `CollectionWindow` 的 id，自述重跑的是「重出 1126 张卡图」那一路，**没重跑异画那一趟**。
5. 名字的**源头**也是旧 id：`git show HEAD:Unity/工具/import_original_art.py` 的 `ALT_ART` 现读仍是 `AM5/BL1/…/UM_Lieutenant_Titus`。本会话 **W16** 已把它改成真值 id（`资料/普查产出_第二十八会话/W16_对账口仪器与旧id残留.md:71,126`，自记「已修」）—— **只修了源头，产物（7 张 PNG + `alt_cutouts.json`）还没重生成** ⇒ 红照旧。
6. 现场读数：日志里 **`异画立绘取不到` 警告 53 条**，每次建页 6 条（`AM2/BL34/SAU35/GOF28/UM76/DA_Azrael`）⇒ **7 张里只有 `SW1` 命中**（新旧 id 恰好同名），其余 6 格画的是普通立绘。
7. 交叉核实（现算 `cards_engine.json`）：`AM2=Ursula Creed · BL34=Abaddon · SAU35=Imotekh · GOF28=Ghazghkull · SW1=Logan Grimnar · UM76=Lieutenant Titus · DA_Azrael=Azrael` —— 与 `import_original_art.py` 那 7 条源贴图**逐张同名同人**；而 **`AM5` 现在是 `Shock Trooper`（另一张卡）** ⇒ 盘上那 6 个旧文件名不只是「少一张图」，它是**把 Ursula Creed 的异画挂在了 `AM5` 这个 id 上**。

**该改成什么 / 谁改坏的**：谁改坏的 = `601005b`（换 id 未同步产物）。修法只有一步：重跑
`python -I d:/4/Unity/工具/import_original_art.py --only-altart`（它自带早退路径，专干这件事），
生成 7 张新名字 + `alt_cutouts.json`；**顺手删掉那 7 张旧名 PNG**。`alt_cutouts.json` 同源同病
（`CardArt.AltArtHasCutout` 对那 6 张现在恒假 ⇒ 卡面「越出卡框」那层也少），一次重跑一起修。

⚠️ **与本会话 A1808 无关**：A1808 改的是 `Style` token（`AA_HB`/`v2` → `HammerAndBolter`）与格数 6→7；
`show[0]` 在 601005b 前后都是 `AM2`（改前 `AltStyles[0]` 也是 `AA_HB`）⇒ 它既不造成也不掩盖这条红。

### 红 2 · `✗ A1045⑥（A1726）… 九宫底 6 块全部换成…`（`collection.log:10571`）
**判定 = (α) 断言期望值不成立（错的是它的实现前提，不是读数）—— 不是回归、也不是老红：这条断言是【第一次跑】。**
（全 `_tmp_view/` 只有本轮日志含「看得出来被禁用」；断言由 `be31b7e`／第二十四会话 2026-10-13 加进来，其后没跑过 `CollectionScene.Run`。）

1. 失败读数 = `nNineOff = 6 · nGreyOff = 6` ⇒ **6 块全灰**，即「看得出来被禁用」这件事**做对了**；红的唯一原因是 `Editor/CollectionScene.cs:2909` 的 `nNineOff >= 9`。
2. **为什么只有 6**：`Shell/GenericOptionsPanel.cs:474-476` 建钮底 = `MenuDraw.Nine(..., border=(333,96,333,96), texW=410, texH=124, borderOutPx=(111,32,111,32))`，钮矩形 = `BtnRect`（`:102 TemplateH = 60`、`:104 BtnW = 357.30`）。角块**画出来的**高 = `borderOutPx.y/w = 32px` 各一 ⇒ **32+32 = 64 > 60** ⇒ `Battle/ImageQuad.cs:468-473, 485-486` 把该轴两条角块缩到 30、**中段高 = 0 ⇒ 中行 3 块 `continue` 跳过** ⇒ 9−3 = **6**。横向 111+111 (px) ≪ 357.3 ⇒ 中列**在**，所以不是 4。
3. 这是**原版 uGUI 的行为**（`Image.GetAdjustedBorders` 逐轴按 `rect.size < b1+b3` 缩、`GenerateSlicedSprite` 跳过零尺寸格 —— 同代码注释口径），**不是我们的缺陷**。
4. 佐证：日志里 `[ImageQuad] Image: border 比图还大（333+333 > 410 或 96+96 > 124）` 共 **50 条**（该钮每次重建都打）—— 那是**另一件**（既有、已如实出声）的事，不影响块数结论。

**该改成什么**：别写死 9。推荐照孪生断言 `Editor/ShellScene.cs:3855-3863` 的口径 —— **灰的那一组必须与亮的那一组同集合**；
至少也要 `nGreyOff == nNineOff && nNineOff >= 6`，并把注释 `Editor/CollectionScene.cs:2844` 那句「九宫底 **9 块**全灰」一并改掉。
⛔ 别把阈值放宽成 `> 1` 就交差（它仍挡「只灰 1 块」，但判别力不如「同集合」）。

### 红 3 / 4 · `✗ ④ 底板真的画出了像素` + `✗ ④·b 行均亮度比`（`cardbase.log:5535 / 5548`）
**判定 = (γ) 上一轮就有的老红 —— 且它抓的是真缺陷（断言在如实报，不是期望值过期、不是夹具问题）。**
- 读数 `66.8 → 66.8（掉 0.0%）· ratio 0.968（内 66.8 / 上 69.0）`；账号 = **`A1513`**（`资料/任务_卡组卡牌战斗.md:210` 仍开着），底层 = **`A1500③`**。
- 依据（读数与本轮**逐字相同**）：`资料/普查产出_第十八会话/D5_两条红的定性.md:41-60` · `资料/历史/交接_第二十一会话.md:50`（「两条都是既有的已知红 = `A1513`，不是本批回归」）· `资料/历史/交接_第十九会话.md:51,101`（「既有、从未绿过」）· `资料/历史/A表已收口_第十七会话.md:23`（同读数）。
- 夹具/仪器都是好的（同组 ①②③④·0 与「★ 前提」全绿，`cardbase.log:5360-5530`）：层在、`sharedMesh` 4 顶点、材质纯黑 α0.647、quad 1.7766×1.55、像素矩形 64×74 全在画面内。
- **本会话对它零影响**：`git diff` 里 `Editor/CardBaseDemo.cs` 只有 11 行（`TAU46` 卡名 `石之感知`→`石之感`）；`Core/CardView.cs` 的 +171 行全是伏击面朝下（`ApplyAmbush`/`SetAmbush`），不碰 `textBg`/`AddLayer`。

## 共性
- 四条**没有一条**是本会话改动打出来的：红 1 = `601005b`（上一轮之前）；红 2 = 断言自带的实现前提错、且是首跑；红 3/4 = 10-11 起从未绿过。
- 红 1/2 的错都长在**断言侧写死了会漂的数**（`>= 9`／`alt_<id>` 当常量用）；红 3/4 反过来是**断言对、实现错**。
- 红 2/3/4 同一形状：**只测「层/组件在不在、开没开」，没测「画出来了吗」**（红 2 用「块数」代理「看得见」；红 3/4 已经把这条补上了 —— 说明该补）。

## 分不清的
- 红 3/4 的**根因**（`A1500③`「为什么不出像素」）**没查清**：静态能查的全对（网格/材质/z/队列/tint），B 报告已排除五条、我又排掉两条。**不给猜测**。
- 红 2 的结论**只覆盖「这一颗钮这一档尺寸」**（铁律 5·c）：同一套 `MenuDraw.Nine` 在别的尺寸上是 9 块（`Editor/ShellScene.cs:3810-3863` 那条孪生断言就是 9）⇒ 修时⛔ 别把「≥9」推广回去。

## 顺手发现
1. **我把 B 报告没列的两条候选现核掉了（都不是根因）**：① 网格拓扑 —— 从本轮自检存盘的 `Scenes/CardBase.unity` 里 `SpriteQuad_textBg` 那颗 Mesh 现读 `m_IndexBuffer = 000002000100000003000200`（6×u16 = 2 三角）、`topology = 0`、`indexCount = 6`、`localAABB.center = (−0.0010, −0.6500)` ⇒ 网格是好的；② 层 z —— 场景里 `textBg.m_LocalPosition.z = +0.015`，与 `Core/CardView.cs:433 TextBgZ = 0.015f` 一致（⚠️ B 报告表格里写的 `−0.012` 与现读不符，是那份报告的笔误）。
2. 🔴 **可查的「可见后果」线索（B 报告没有）**：`Core/CardView.cs:5429-5435` 记着 **2026-09-15 撤掉了 `InfoTexture` 里那层自造的「文字区柔和压暗」`DrawTextScrim`**，理由写的正是「原版有一层真的文字底板，我们现在按原版加上了（`textBg` 图层）」。现核 `grep -rn DrawTextScrim` ⇒ **只有定义（`:5797`）、零调用点**。⇒ 那个「替代品」就是红 3/4 的对象（**一个像素都不画**）⇒ **卡面效果文字现在很可能整体缺一层暗底**（原版有）。建议并进 `A1513`/`A1500③`。⚠️ 我没有真机/渲图证据断言「肉眼已可见」，**如实标「还没查清」**。
3. **现成探针从没跑过**：`Editor/CardBaseDemo.cs:1037-1114` 的 `ProbeTextBg`（门 = 环境变量 `WF_TEXTBG_PROBE=1`，不设则零输出、不影响 stdout 回归判据）一次就能打全 B 报告「还差什么」那五样（渲染器 `enabled/activeSelf/forceInHierarchy/forceRenderingOff/isVisible/layer+cullingMask` · 网格 · 材质 · 位置 · 只渲这一层存图 · 开/关两帧亮度差）。全库 grep「探针·文字底板」**0 命中** ⇒ 下次跑 `CardBaseDemo.Run` 时顺手 `set WF_TEXTBG_PROBE=1` 是最省的一条路。
4. W16 那份报告把 `import_original_art.py` 的异画命名记为「已修」（`:71,126`）——**只修了源头、产物没重生成**，红 1 照旧 ⇒ 措辞建议补「⏳ 待重跑 `--only-altart` 才生效」。
5. 本会话 A1808 的四条断言读数（异画 7 / 风格 1 / `StyleCells` 7 / 箭钮死键 `:33670,33683,33939,35289,35431`）**全绿** ⇒ A1808 自身没打出红。
