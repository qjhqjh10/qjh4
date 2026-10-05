# D6 · Rewards 最后 1 条红诊断（只读 · 2026-10-11）

> 只读诊断件。**没跑 Unity / 没跑自检 / 没动 git / 没改任何既有文件**（本文件是唯一写出去的东西）。
> 现场：`d:/4/_tmp_view/rewards2.log`（**1289 通过 / 1 失败**，`:22160`）· 旧跑 `d:/4/_tmp_view/rewards.log`（**1271 / 11**，`:22150`）。

---

## 一、结论（(α|β|γ) + 一句话）

🔴 **⛔ 不是 (β)（实现没坏）· 红是【本批 WA2（A311 第 1 跳）带出来的】(γ)，修法在【断言那一侧】(α)。**

**一句话**：WA2 新接的 `ItemDrawer.SetPremium` 照原版 prefab 的 `sizeDelta (50,50)`（拉伸锚）把高级档的 `Highlight` **每边外扩 `50·k` = 13.889px**，而 §⑤ 这个夹具的第 2 条奖励正是 `TierPremium`（= 10）⇒ 那条装饰的左沿 **966.11** 探进遮罩收拢时那条 **20px 带 [950,970]** 里 **3.89px** ⇒「此刻物品**一个 quad 都画不出来**」这个**前提**不再成立 —— 它**只在「没有任何东西越出抽屉框」时才偶然为真**（旧跑之所以绿，是因为那 40px 缝 940→980 正好把 20px 带包在中间）。

⇒ 我们这一刻画出来的那一小块，**原版同样画**（`RectMask2D.m_Padding` 也是 950）⇒ **要改的是断言**（§四给了可执行文字），⛔ 别去动实现、也别动夹具。

---

## 二、这条是谁（子节 / 行号 / 属于哪件活 / 判据出处）

| 项 | 值 |
|---|---|
| 断言 | `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs:4283-4285` |
| 子节 | **§⑤ 开场揭示动画**（段头 `:4275`）—— 属 **W1_A309_A310**（揭示动画/`mask2D.padding`）那件活，**本批没人动它** |
| 断什么 | `CheckTrue(!RectOfUnion(rw.ListHolder, out _, out _, out _, out _), …)` = 「此刻 `ListHolder` 子树里**一个激活的 `ImageQuad` 都没有**」 |
| 「遮罩收到 20px」怎么来的 | `RewardWindow.MaskPad`（`Shell/RewardWindow.cs:408`）= `MaskPadAt(1 − _reveal)`；`_reveal = 0` ⇒ `(950,0,950,0)`（`Shell/RewardWindow.cs:677-684`，字面量出处 = 原版 `0x1834b32e0`）。谁在推：**`RewardWindow.Tick(dt)`**（`Shell/RewardWindow.cs:564-577`）—— 推进 `_reveal` 后**紧接着 `BuildItems()` 按新带口重切**（帧循环里由 `Update()` 调它，自检里直调） |
| 带子怎么算出来的 | `BuildItems()` 里 `Clip = ScrollView; ClipSoftness = ScrollSoft; ClipPad = MaskPad;`（`Shell/RewardWindow.cs:892-893`）→ `RenderClip = MenuDraw.PaddedClip(Clip, ClipPad)`（`Shell/WindowsManager.cs:250`）→ `PaddedRect` = `(x1+pad.x, y1+pad.w, x2−pad.z, y2−pad.y)`（`Shell/MenuDraw.cs:346-350`）⇒ **(0,300,1920,932.5) 内缩成 x ∈ [950, 970]**（左右各 950，正好 20px）|
| 「一个 quad 都画不出来」凭什么 | `MenuDraw.Rect` 的裁切语义：**整块在框外 ⇒ 不建**（`return null`，`Shell/MenuDraw.cs:1232`）；**部分在框里 ⇒ 建【求交后】的那一块**（`:1236-1238`） |
| 判据出处 | 原版 `DoRewardAnimation`（`mask2D.padding` 950 → 0 · 线性 · 0.8s）+ `RewardWindow__Open.c`；本仓既有订正注释在同段 `:4272-4275` |

---

## 三、证据（旧日志 vs 新日志逐条对照 + 因果链）

### 3·1 这条是「同一句」——行号只差 FX4 插进去的 20 行

| | 旧跑 `rewards.log` | 新跑 `rewards2.log` |
|---|---|---|
| 这一条 | **`:14269` = ✓**（栈 `RewardsScene.cs:**4263**`） | **`:14285` = ✗**（栈 `RewardsScene.cs:**4283**`） |
| 合计 | `:22150` `=== 合计：1271 通过 / 11 失败 ===` | `:22160` `=== 合计：1289 通过 / 1 失败 ===` |
| 回顾 | （被 11 条淹着） | `:22171` 同一条（**失败回顾**，不是第二条红） |

栈行号 **+20** = FX4 在 `Build()` 里插的那 20 行订正注释（`Editor/RewardsScene.cs:710-725`，第 726 行才是 `var wm = WindowsManager.EnsureHost();`）⇒ **是同一条断言**，不是新加的。

🔴 **本件要回答的那一问**：旧日志里它 **是 ✓ 的**（不是「一直被 11 条淹了」）⇒ 它**确实是这一批改动带出来的**（(γ) 成立）。

### 3·2 整段逐行 diff：**只有 2 个读数变了**（⇒ 不是夹具、不是宿主改的）

把两跑 §⑤ 前后那 39 条 `[Rewards]` 行抽出来 diff（`rewards2.log:13900-14430` vs `rewards.log:13880-14410`），**唯一的两处差异**：

```
-[Rewards]    ✓ ★ …此刻遮罩收到只剩中间 20px …（没接动画的版本这一刻已经是全开的 ⇒ 红）
+[Rewards]    ✗ ★ …（同上） —— 期望 [True]，实得 [False]
-[Rewards]    ✓ …而且都落在视口里（实测 x 770.0 → 1142.8）
+[Rewards]    ✓ …而且都落在视口里（实测 x 770.0 → 1193.9）
```

其余全同：pad 四个分量 · 揭示进度 · 「两条奖励 ⇒ `listHolder` 底下**两个**抽屉节点」（`:14420`/旧 `:14404`）· 窗口 dump 行（新 `:13844` / 旧 `:13828`，逐字一致，含 `无图标物品 1`）· 截图亮度护栏。
⇒ **夹具、宿主（FX4 的 `EnsureHost`）、布局、抽屉数、pad 一个字都没变；变的只是「画出来的东西更宽了」。**

### 3·3 多出来的那 51.1px 是谁：**premium `Highlight`**（数字精确吻合）

- 布局（常量 `Shell/RewardWindow.cs:129/167/169/176`）：`ScrollView` 宽 1920 居中 · `PadL = PadR = 60` · `ItemW = 200` · `ItemSpacing = 40`。
  `contentW = 60+60+2×200+1×40 = **560**`（这句在日志 `:14421` 那条断言里也写着）⇒ 内容 = **680 → 1240**，
  **第 1 格框 = 740→940**、**第 2 格框 = 980→1180**、中缝 = **940→980**。
- 交叉验证（旧跑并集左沿 **770.0** = 740 + (200−140)/2 ⇒ 正是 `Square(box, 0.7)` 的 140 见方图标居中，`Shell/ItemDrawer.cs:1192-1196`）⇒ **框的位置是被独立证实过的**，不是我自己推的。
- 新跑并集右沿 **1193.9**，而 `1193.9 − 1180 = 13.9 = **50 × 300/1080**` —— 正是 `SetPremium` 那句
  `float k = box.H / DecorRefH; float pad = 50f * k;`（`Shell/ItemDrawer.cs:888-893`，`DecorRefH = 1080f` 在 `:306`，`box.H = ItemH = 300`）⇒ **Highlight = 966.111 → 1193.889**。
- ⛔ **反证「旧跑根本没用这个装饰」**：`Blackout` 是**整个抽屉框**（`Shell/ItemDrawer.cs:894-895`）⇒ 只要 `SetPremium` 被调过，并集右沿**必然 ≥ 1180**。旧跑是 **1142.8 < 1180** ⇒ 旧跑**一次都没调**。

### 3·4 因果链（每一环都有出处，数字全自洽）

1. 夹具第 2 条奖励 = `new CampaignData.RewardSpec("Booster Pack Ultramarines", 1, CampaignData.TierPremium)`——`Editor/RewardsScene.cs:4167-4174`（**本批没改这一句**；它在 W1 那轮就长这样，旧跑同样有它）。
2. `TierPremium = 10` —— `Shell/CampaignData.cs:29`。
3. `st.Premium = (spec.Tier == CampaignData.TierPremium);` —— `Shell/RewardWindow.cs:944`（**本批新接的第 1 跳** `TogglePremiumHighlight`）。
4. Highlight 矩形 = **框 ± 50·k** —— `Shell/ItemDrawer.cs:888-893`（prefab 的 `sizeDelta (50,50)` 拉伸锚 ⇒ 每边外扩；WA2 报告 §二·② 有 prefab 实读）。
5. 裁切带 = **x ∈ [950, 970]**（§二 那两格）。
6. `966.111 < 970` ⇒ **部分在带里** ⇒ `MenuDraw.Rect` **建**（求交后 966.111→970，`activeSelf = true`，`MenuDraw.cs:1229-1238`）⇒ `RectOfUnion` 看的是 `activeSelf`（`Editor/RewardsScene.cs:562-577`）⇒ 返回 **true** ⇒ `!RectOfUnion` 假 ⇒ **红**。
   （另两件装饰都**不可能**是它：`Blackout` = 980→1180、`Badge` = 980→1040，**整块在带外 ⇒ 不建**。）

### 3·5 为什么旧跑是绿的 —— 这是「偶然」

旧跑里**没有任何 quad 越出抽屉框**（框 740→940 与 980→1180，全在带外），而 20px 带 [950,970] **正好坐在 40px 中缝**的中间（左右各余 10px）⇒ 「带里什么都没有」**成立但脆弱**：只要有**任何**一件越出框 ≥10px，这条就翻红。

### 3·6 FX4 的 `EnsureHost()` 有没有关系：**没有**

- 它改的是「管理器从哪来 / 窗归谁 / 锚点怎么挂」（`Editor/RewardsScene.cs:726`），**不产生任何绘制**；旧跑那 8 条级联红（`Instance` 恒 null）与这条的**判据类型完全不同**。
- 实证：该段**所有几何读数逐字未变**（含并集左沿 770.0、pad 950.0、抽屉数 2、窗口 dump 行）——若父链/缩放变了，这些数是**不可能**一个都不动的。

---

## 四、修法（(α)：可执行的正确判据文字）

### 4·1 推荐：把判据从「一个 quad 都没有」改成「**能画出来的东西全部落在带里**」

替换 `Editor/RewardsScene.cs:4283-4285`（保留 `★` 前缀与文件既有风格；`rwPad0` 在 `:4277` 已经读过，**别把 950 写死**——宽屏那一档 `MaskPadAt` 还要再加）：

```csharp
            // 🔴 **2026-10-11（批次2 · 诊断 D6）订正（铁律 5）**：这条原来断「物品一个 quad 都画不出来」——
            //    那个结论**只在「没有任何东西越出抽屉框」时才成立**，而我们**没有**这个性质：
            //    高级档那一格的 premium `Highlight` 照原版 prefab 的 `sizeDelta (50,50)`（拉伸锚）
            //    **每边外扩 50·k**（`Shell/ItemDrawer.cs:888-893`，`k = box.H/DecorRefH = 300/1080` ⇒ 13.889px）
            //    ⇒ 第 2 格（框 980→1180）那条 Highlight = **966.11 → 1193.89**，左沿**探进带里 3.89px**
            //    （带 = [950,970]）⇒ `MenuDraw.Rect` 把它**裁到带里那一条**建出来（`Shell/MenuDraw.cs:1229-1238`：
            //    整块在框外**不建**、部分在框里就建**求交后**的那块）⇒ 此刻**确实有一个 quad 画得出来**
            //    （宽 3.89px · alpha ≈0.08 —— 原版 `RectMask2D` 同义，**不是缺陷**）。
            //    ⇒ 判据改成它**本来那个意思**：「能画出来的东西**全部落在带里**」——
            //    揭示没接（或没推）时并集会跳到整条内容（实测 770.0 → 1193.9）⇒ **照样红**，判别力不减。
            bool anyQuad = RectOfUnion(rw.ListHolder, out var mq1, out _, out var mq3, out _);
            float bandL = rwPad0.x, bandR = 1920f - rwPad0.z;   // 950 / 970（宽屏那一档自动跟着涨）
            CheckTrue(!anyQuad || (mq1 >= bandL - 0.5f && mq3 <= bandR + 0.5f),
                      $"★ …此刻遮罩收到只剩中间 {bandR - bandL:F0}px（1920 − {bandL:F0} − {rwPad0.z:F0}）"
                      + $"⇒ 能画出来的东西**全部落在带里**（实测 x {(anyQuad ? mq1 : 0f):F1} → {(anyQuad ? mq3 : 0f):F1}）"
                      + "（没接动画的版本这一刻已经是全开的 ⇒ 并集 770.0 → 1193.9 ⇒ 红）");
```

> ⚠️ 语义前提（**核过**）：`RectOfUnion` 读的是 quad **自己的**世界位置/尺寸（`Editor/RewardsScene.cs:544-553`），
> 而 `MenuDraw.Rect` 建的就是**求交后**那块 ⇒ 「并集 ⊆ 带」是**正确的**断法，不是近似。
> ⚠️ 顺带把 `:4567-4568` 那句交叉引用改掉（**同一句话的第二份**，铁律 6）：
> 「…与第 ⑤ 段「收拢时一个 quad 都没有」互为对照」→「…与第 ⑤ 段「收拢时**带里只剩 premium 装饰那一条**」互为对照」。

### 4·2 ⛔ 不建议的两种改法（都是拿错的东西换红）

| 改法 | 为什么不行 |
|---|---|
| 把夹具那条 `TierPremium` 改成 `TierBasic` | 会连累同段 `:4266-4267` 与 §⑧·b「有高级档奖励」那条前提（那是**故意**用高级档奖励来判 `Premium Disclaimer` 的） |
| 改实现让装饰不外扩（或给它单独裁死） | **背离原版** —— prefab 就是 `sizeDelta (50,50)` 拉伸锚（每边外扩），原版那一刻同样会露出这一小条 |

---

## 五、判不了的（缺什么才能判）

1. 🔴 **「收拢那一刻并集恰好 = 966.1 → 970」是【算出来】的，不是【量出来】的** —— 本件只读、不许跑 Unity；而这条断言**只印 bool、不印数字**（§六·4）。
   便宜的闭合办法：按 §四 改完，下一次 `RewardsScene.Run` 的日志里就有实测值（**期望 `966.1 → 970.0`**）——**那一次日志就是本条的验收**。
2. 「**原版**那一刻也露出这一小条」= 由 `RectMask2D.m_Padding`（950）+ prefab `sizeDelta (50,50)` 推出，**没有实拍**（揭幕只有 0.8 秒，`SceneJumpShot` 那种静态 dump 拍不到那一拍）。
3. `k = box.H / DecorRefH`（±50 → ±13.889）这个**缩放比**本身我没核（是 `ItemDrawer` 的口径，判据在 WA2 报告里）。
   ⚠️ 但它**不影响本结论**：只要 `pad > 10px`，Highlight 左沿就必然 `< 970` ⇒ 这条断言**照样红**（`pad = 10` 恰好相切、`≤10` 才不红）。

---

## 六、顺手发现（⛔ 只报不改）

1. 🔴 **`Shell/RewardWindow.cs`（1090 行生产代码）在 git 里是【未跟踪】**（`git status` = `??`，同批还有 `Battle/AnimFXController.cs` 与 26 个新文档）。**这不是本轮造成的**，只要提交时 `git add -A` 就没事 —— 提出来是因为 `git clean -fd` / 选择性提交会**静默丢掉**这两份生产 `.cs`。⛔ 我不动 git。
2. 🔴 **WA2 的 §六·b「对既有断言的影响」自审有漏**（`资料/普查产出_1011/WA2_A311_A325.md:162-171`）：那张表逐条核了 7 族既有断言（`§⑧` 的 `CountByPrefix`/`childCount`、`§九(e)` 的 `ImageQuad` 长度、`§九(d)` punch…），结论写着「**A311 都不会带红**」—— **不成立**：`§⑤` 这条「带里有没有 quad」**根本没进那张表**。⇒ 建议把「**凡 `!RectOfUnion` / 「一个 quad 都没有」式的断言**」列成那张表的**必查行**（这类断言的成立**依赖几何**，而装饰层的存在会静默改几何）。
3. 🔴 **四跳（A311）自己的断言目前一条都没进宿主**（WA2 报告 `:115` / `:158` 自报：`Editor/RewardsScene.cs` 在黑名单 ⇒「现在**一条都还没跑**」）。本轮唯一的红**正是它带出来的副作用**，而它的验收断言**还是空缺** ⇒ 建议 §四 的订正与那批断言**一并落地**（派一段进 §⑤/§⑧）。
4. ⚠️ 这条失败消息**不印任何数字**（`CheckTrue` 只回 bool）—— 诊断成本全在「手算布局反推 13.889」上；§四 的改法顺手把并集印进消息（同类断言里 `{…:F1}` 的写法很常见）。
5. ⚠️ 同族脆性（今天**不红**，只是记一笔）：§⑤ 拐角那句「两条奖励 ⇒ **两个**抽屉节点」（`:4300`）依赖 `ExpandCells` 不展开 —— 夹具第 1 条 `Quantity = 3`，一旦 `ItemDrawerConfig` 判它「非堆叠」，格数会**静默**从 2 变 4；同理 `RewardsScene.cs:4048-4052`（高级列「渲染并集右沿 ≤ 1920」）与 `:4567` 那两条是同一族「拿并集当尺子」的断言。

---

## 七、我读过的文件与命令

**日志**（只读）
- `d:/4/_tmp_view/rewards2.log`（`:14285` ✗ · `:14381` 并集 770.0→1193.9 · `:22160` 合计 1289/1 · `:22171` 回顾 · `:13844` 窗口 dump）
- `d:/4/_tmp_view/rewards.log`（`:14269` ✓ 栈 4263 · `:14365` 并集 770.0→1142.8 · `:22150` 合计 1271/11 · `:13828` 窗口 dump）
- 命令：`grep -n`（两次定位同一句 + 计数 `✗`）· `sed -n`（取上下文）· **python 抽 `[Rewards]` 行 + `diff -u`**（`13900-14430` vs `13880-14410`，得到 §3·2 那张「只差 2 个读数」表）

**源码**（只读）
- `Unity/MyGame/Assets/CardPresentation/Editor/RewardsScene.cs`：`:4167-4174`（夹具两条奖励）· `:4275-4297`（§⑤ 全段）· `:519-577`（`RectOf` / `QuadRectOf` / `RectOfUnion`）· `:4560-4572`（§⑩ 对照那条）· `:4048-4052`（§ 高级列并集）· `:710-726`（FX4 的 `EnsureHost`）
- `Unity/MyGame/Assets/CardPresentation/Shell/RewardWindow.cs`：`129/132/167/169/176/178/186/202/241`（常量）· `408`（`MaskPad`）· `564-577`（`Tick`）· `677-684`（`MaskPadAt`）· `863-909`（`BuildItems` 的裁切三件套）· `934-965`（`BuildItem` 四跳）· `978-995`（`ExpandCells`）
- `Unity/MyGame/Assets/CardPresentation/Shell/ItemDrawer.cs`：`306`（`DecorRefH=1080`）· `885-899`（`SetPremium`）· `918-924` / `946-960`（另两跳）· `1078-1108`（`IsStackable`/`ExpandsByQuantity`）· `1192-1196`（`Square`）
- `Unity/MyGame/Assets/CardPresentation/Shell/MenuDraw.cs`：`346-350`（`PaddedRect`）· `371-385`（`PaddedClip`）· `616-706`（`ApplySoftEdges`，含「整块在裁切框外 ⇒ 四角 alpha 清 0、**不关节点**」那一支 `:634-643`）· `1212-1253`（`Rect` 的裁切/建模）
- `Unity/MyGame/Assets/CardPresentation/Shell/WindowsManager.cs:250`（`RenderClip`）· `Shell/CampaignData.cs:29`（`TierPremium = 10`）
- `Unity/资料/普查产出_1011/FX4_Rewards十一条红修复.md`（全文）· `WA2_A311_A325.md`（§一 / §二·①② / §六 / §六·b）· `DIAG-B_Rewards十一条红.md`（grep：**没提过这条**）· `Unity/资料/待办判据_1011.md`（grep：**没有 A311 / 奖励窗的条目**）

**git**（只读）
- `git status --short`（**`?? Shell/RewardWindow.cs`**，见 §六·1）· `git diff --stat -- Shell/ItemDrawer.cs`（+399/−20）· `git diff -- Shell/ItemDrawer.cs | grep`（**`SetPremium` 与 `float pad = 50f * k;` 都是新增行** ⇒ 本批新加的）· `git diff -- Editor/RewardsScene.cs | grep`（夹具那两句是 `+`，但属 W1 那轮、旧跑已在跑它）
