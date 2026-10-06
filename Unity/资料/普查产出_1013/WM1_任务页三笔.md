# WM1 · MissionsTab 三笔（A389 里程碑图 + A405 字号 + A493#9）

> 写手：**执行写手 WM1**（第四轮「清空 A 表」）· 时刻 **2026-10-13**（本机日期 2026-10-05）
> 白名单内改了 3 个文件：`Shell/MissionsTab.cs` · `工具/import_original_art.py` · `Editor/RewardsScene.cs`（只加自己那一段）
> ⛔ 没跑 Unity / `-executeMethod` / `_run_8_checks.sh` · ⛔ 没动 git（只跑过只读的 `git diff --numstat`）· ⛔ 没改两张正本 · ⛔ 没越白名单
> ✅ 跑过 **3 次** `TMPDIR=/tmp/wf_wm1 bash d:/4/Unity/工具/typecheck.sh`，最后一次 **运行时 0 / 编辑器 0**
> （第 1 次报 `Battle/ScenarioBlendables.cs(2140,33): error CS0103 … FindChildByName` —— **不是我的文件**，隔一轮再跑已消失）

---

## 一、结论（三句）

1. **A389 做完了**，而且**「第 N 格用哪一档宝箱」这一格不必照实拍猜 —— 判据是硬的**：
   原版 `MissionMilestonesDisplay.Setup` 逐格 `Instantiate(stepPrefab[Math.Min(下标, 长度−1)])`，
   而真包里**三份** `Mission Milestones Progress` 的 `stepPrefab` **同值 = `[T1,T2,T3,T4,T5]`**
   ⇒ **第 6 格被夹回 T5**。实拍（用户那张参考图）逐格吻合，是**复核**、不是判据。
2. **A405 两处改了**（`name` 12→**20** / `Refill Counter` 12→**10**），**A493#9 复核为真缺陷并改了**（周常 `name` 补 `AlignL`）。
3. 🔴 **顺带订正了块1 的一条判据（铁律 5）**：`holder/Image` 的 `m_Enabled` **是 1、不是 0** ——
   块1 把**同一个节点上 `Outline` 组件**的 `m_Enabled` 当成了 Image 的（`menu_dump.py` 的 `m_Enabled` 列
   在一行多组件时**只印一个组件的值**，正是它把人带偏的）。⇒ **原版会画那两个圆点**，我们照画。
   ⚠️ **那条错记录在 `资料/普查产出_1013/A表现核_块1.md`（不在我的白名单）⇒ 请调度台就地改掉。**

---

## 二、A389 逐格表

> 「今天画什么」= 本件动手**之前**的现读（`git show HEAD:…` 那一版）；
> 「改后画什么」= 本件落地后的实现。「判据出处」一律是**原版资产字段 / 反编译 / pid 索引**。

### 2·1 每日骷髅卡（`small = true`，5 格，阈值 `3/10/25/50/100`）

| 件 | 原版该画什么 | 我们今天画什么（改前） | 改后画什么 | 判据出处 |
|---|---|---|---|---|
| 格底 `holder/Image` | **无 sprite**（`m_Sprite = 0`）+ `m_Color = (0,0,0,1)` ⇒ UGUI 画**一块 40×40 纯黑** | 一张 `40k_missions_milestone_on/off` 圆点（67×66 等比）染绿/米黄 | 40×40 纯黑 quad | `GO/Mission Milestones Step (1).json` 的 MB `-5679983552473563956`（`actionOnActive = 5` ⇒ **不含 `ChangeSprite`** ⇒ 不换图）·Image MB `-3311031727186150196`（`m_Sprite=0` · `m_Color=(0,0,0,1)`） |
| 描边 `Outline` | `m_EffectDistance = (2,-2)` · `m_UseGraphicAlpha = 1` ⇒ **四条 2px 边、四角有缺口**（UGUI `Outline` 是四份**单轴**偏移副本）；颜色运行时写 = 达成 `activeColor` / 未达成 `disabledColor` | **一条都没画** | 四条 2px 条（上/下/左/右），色 = 绿 **(0.3312554,1,0)** / 米黄 **(0.9176471,0.7686275,0.4823530)** | Outline MB `-4591191426456847156`（`m_Enabled=1` · 两色值实读）· `DF:MissionMilestoneStep__Setup.c`（`&1` 支写 `outline.effectColor`） |
| 勾 `holder/CheckMark` | `activeCheckmark = 40K_settings_icon_checkmark` · `disabledCheckmark = 0` ⇒ **达成才 `SetActive`**；框 **40 × 42.3202**、中心在格心 **+21(右) / −27.3(上)**（`anchors (0,0)-(1,1)` · `sizeDelta (1.526e-05, 2.32018)` · `anchoredPosition (21, 27.3)`） | 没这一件 | 达成时叠那张勾，框与位置**照 RT 原文**（子树 `×1.15`） | 同 MB 的 `activeCheckmark = fid3:4411787853012002210` → pid 索引 `d:/4/_tmp_view/sprite_pids_ALL.json` 解出 `40K_settings_icon_checkmark` · `DF:MissionMilestoneStep__Setup.c` 尾段 `SetActive(op_Inequality(sprite,0))` |
| 数字 `holder/text` | 40×40（与格同框）· fs 42.2 · auto[10~50] · base 36 | 已经是这样（A371/A336③ 做的） | **不动** | — |

### 2·2 周常（`small = false`，6 格，阈值 `5/10/15/20/25/30`）

| 件 | 原版该画什么 | 我们今天画什么（改前） | 改后画什么 | 判据出处 |
|---|---|---|---|---|
| 圆点 `holder/Image` | **`m_Enabled = 1`**、图 = `40k_missions_milestone_on/off`（`Simple` + `PreserveAspect`，67×67）—— **原版会画** | 画了圆点（**方向是对的**，但只有它） | 照画（压在宝箱**之下**） | Image MB `-5355200480893724929` / `-624403752006491195` 原文 **`m_Enabled: 1`** + `m_Sprite: fid10:-6034862274493247622`；🔴 **块1 的「`m_Enabled = 0`」是误读**（见 §七·1） |
| 宝箱 `holder/CheckMark` | 图 = `40k_Crate_Tier{1..5}_{Iron,Copper,Silver,Gold,Warp}`——**档位 = `stepPrefab[Min(i,4)]`**；框 **142.95 × 129.0763**、**以格心为中心** | **根本没有这一件**（画的是圆点） | 每格按档位画宝箱（框 = RT 原文） | `DF:MissionMilestonesDisplay__Setup.c`（`Math.Min(下标, 长度−1)`）+ 真包三份 MB 的 `stepPrefab` 数组（§九·1）+ 5 个宝箱 MB 的 `active/disabledCheckmark`（§九·2） |
| 开 / 闭 | `≤ lastReachedMilestone` → **`_open`**（开启）；`>` → **闭合** | — | 照做 | `DF:WeeklyMissionMilestone__DisplayCheckmark.c`（`value < / == / > lastReachedMilestone` 三支逐句） |
| 灰不灰 | `value < lastReachedMilestone` ⇒ **灰**（`SetInteractable(false)` + `colorTintGreyOnDisable=1` ⇒ 材质换 `Everguild/UI/Greyscale`，且 ColorTint 乘 `m_DisabledColor = 0.7843137`） | — | 照做（走 `WindowButton.GrayShaderName` 那份**现成**实现） | 同上 + `EverguildButton__DoStateTransition.c`（`m_Transition=1` 时那条**跳过自绘**的分支）+ 真包 `m_Colors.m_DisabledColor` |
| 数字 `holder/text` | 142.95×56 · 框底 = 格底 + 29.53 · fs 50 · auto[10~50]`（**页内那份**） | 已经是这样 | **不动**（⚠️ 但见 §七·2：**运行期那份 prefab 的 min 是 15**，是**另一笔**） | `menu_dump.py "Weekly Mission Milestones Step (3)"` 实读 |

### 2·3 层序（同一次改的，判据也是原版）

| 次序 | 原版依据 | 我们改后的队列 |
|---|---|---|
| 进度条 → 里程碑 | `progress` 的子节点序 `Mission Progress Bar`(N=3) → `Mission Milestones Progress`(N=4) ⇒ **宝箱把金条压住**（实拍上看得见） | 进度条 `QContent−3`(3007) / `Handle` `−2`(3008) |
| 圆点 → 宝箱 → 数字 | 每格 `holder` 的兄弟序 `Image → CheckMark → text`（`m_Children` 原文） | 圆点 `QContent−1`(3009) / 宝箱 `QContent`(3010) / 数字 `QText`(3011) |
| 描边 → 方框 → 勾 → 数字（每日） | 同上 | 描边 `−2` / 方框 `−1` / 勾 `QContent` / 数字 `QText` |

> 🔴 分层**只靠渲染队列**（透明队列按「到相机的距离」排 ⇒ 同档会静默盖错）—— 见 `ImageQuad.SetRenderQueue` 的注释。
> `BuildBar` / `BuildNine` 各加了一个尾参 `int q = RewardsWindow.QContent`（缺省 = 旧行为 ⇒ **每日行那条进度条一字未变**）。

---

## 三、A405 改动清单（2 处）

| 站（本件改后行号） | 原版 | 改前 | 改后 | 出处 |
|---|---|---|---|---|
| `Shell/MissionsTab.cs:1016` 页头 `name (Mission Header)`（`'Daily Missions'` / `'Daily Skulls'`） | `auto[20~36]` / `auto[20~30]` ⇒ **min = 20** | `12f` | **`20f`** | `资料/普查产出_1012/F1_字号线.md` §4·3 |
| `Shell/MissionsTab.cs:1027` `Refill Counter`（`'0 Disponible'`） | `auto[10~36]` ⇒ **min = 10** | `12f` | **`10f`** | 同上 |

⚠️ **只动第 2 个实参**（`autoMinPx`）：`autoMaxPx = 0f` ⇒ `Txt` 代标称 36（= 原版 max）· `autoBasePx = 46f` 已对。
⚠️ **两颗 min 不同（20 / 10）**是原版本来的样子（铁律 5·c：别取一个顶两个）—— 已写进代码注释。
⚠️ A143 当年只对齐了**卡头**那两处（`:1071` 那行 `20f` 就是它），**页头这两处漏了** —— 与 F1 §4·3 的记录一致。

---

## 四、A493#9 现读复核记录

### 4·1 现读（改前）原文 —— `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/MissionsTab.cs:896-899`

```csharp
            Txt(parent, new PxRect(head.x1, head.y1, head.x2, head.y1 + 50f),
                "Weekly Challenge", Color.white, "name", 36f);
```

- **没有** `Align*`；`MenuDraw.Text` / `MenuDraw.TextBox` 全程**不设 `alignment`**；
  而 `Battle/Label.cs` 的 `SetAlignLeft` 注释**逐字**写着「`TmpFont.NewText` 把所有 TMP 统一建成 **`Center`**，
  而原版这批文字**多数是 Left**（`ChatText` = 1 · `Mission Header` / 卡名 / 每日行全是 1）」
  ⇒ **缺省是 Center**，不是 Left。
- 所以 A493 那张表把这一行记成「原版 `Left/Capline`、我们没对齐」时，**这一行本身是对的**
  （与 H37 那张表**第 10 行**（`ChatPanel` 的 `Placeholder`）被上一位代理判「不可信」是**两回事** ——
   那一行是「有别的机制在对齐」，这一行**没有**）。

### 4·2 「H37 那张表这一行实际是什么」

| 问题 | 答案 |
|---|---|
| H37 §四·1 表里第 9 行记的是哪一行代码？ | `Shell/MissionsTab.cs:897` 的 `Txt(parent, new PxRect(head.x1, head.y1, head.x2, head.y1 + 50f), "Weekly Challenge", …)` —— 与现读**逐字一致**（本件现读的 `:897` 就是它） |
| 它「原版对齐」那一列写的是什么？ | `Left/Capline`（同族 `name` 4 颗全 Left） |
| 我复核的结论 | ✅ **这一行的判据成立**：现读代码确实**没有**任何 `Align*`，而 `Label` 的缺省是 Center ⇒ **是真缺陷** |
| ⚠️ 我**没有**读 H37 报告原件 | H37 报告不在我的白名单 ⇒ 上面「H37 记的是什么」是**转记**（A493 那张表 + 块4 的引文）；**我判的是现读代码 + `Label` 缺省**，不依赖 H37 |

### 4·3 改法（已落地，`Shell/MissionsTab.cs:897-904`）

```csharp
            var wkNameR = new PxRect(head.x1, head.y1, head.x2, head.y1 + 50f);
            AlignL(Txt(parent, wkNameR, "Weekly Challenge", Color.white, "name", 36f), wkNameR);
```

与同文件 `name (Mission Header)`（`:1013`）· 卡头 `name`（`:1071`）**同一处理**（`AlignL` → `Label.AlignLeftOn`）。
⚠️ 只做**横向**（原版那颗是 `Left`/`Capline`，纵向 `Capline` 我们这一族一律没做 —— 与另 3 颗 `name` 逐字同待遇，**不是本笔新开的口子**）。

### 4·4 覆盖现状

`grep -n '"name"' Editor/RewardsScene.cs` = **0 命中** ⇒ 这一颗**今天零断言**（与 A493 §已核一致）。
本件**没给它补断言**（它是 A493#9 的一行对齐，且同文件同节有别的写手在动 —— 见 §八·5）。

---

## 五、素材导入清单（5 张 `_open`；⚠️ 我**没跑** Unity）

| 导入后的名字 | 源（`ui_extract` 缓存） | 导入器里的位置 |
|---|---|---|
| `40k_Crate_Tier1_Iron_open` | `d:/2/Warpforge_tools/data/ui_extract/boosterpacks_assets_all/Sprite/40k_Crate_Tier1_Iron_open.png` | `工具/import_original_art.py:546` |
| `40k_Crate_Tier2_Copper_open` | 同目录 `…Tier2_Copper_open.png` | `:547` |
| `40k_Crate_Tier3_Silver_open` | 同目录 `…Tier3_Silver_open.png` | `:548` |
| `40k_Crate_Tier4_Gold_open` | 同目录 `…Tier4_Gold_open.png` | `:549` |
| `40k_Crate_Tier5_Warp_open` | 同目录 `…Tier5_Warp_open.png` | `:550` |

- 落点（`menu_jobs` 的规则，`工具/import_original_art.py:1344-1346`）= `Resources/Art/ui_menu/<名字>.png`（空格→下划线）。
- **照已有那 5 张闭合宝箱的写法加的**（`:537-541`），**只加了这 5 条**、没动别的行（`git diff --numstat` = **9 加 / 0 删**）。
- ⚠️ **现状**：`Resources/Art/ui_menu/` 里**还没有**这 5 张（只有一张不相干的 `40K_dropdown_field_opened.png`）
  ⇒ **在跑导入器之前，周常那 6 格的宝箱图取不到**；代码里配了一条 `Debug.LogWarning`（红线：不许静默失败），
  那时每一格只剩底下的圆点。
- ⚠️ **跑导入器 = `工具/import_original_art.py` 全量**（它没有「只导某几张」的开关）⇒ 归**主对话的 Unity 腿**统一跑。

---

## 六、断言清单（`Editor/RewardsScene.cs` 的 `Section("🆕 A389 里程碑格的图…")`，`+271` 行，改后 `:2259-2529`，共 **36 处**断言调用）

> 期望值**全部是原版字段 / 反编译 / 图片名的字面量**，⛔ 不从 `MissionsTab` 读常量
> （唯一一处「读实现」的是量 `WorldW/WorldH`，那是**渲染结果**、不是我们的常量）。
> 每条都断**两态或可分辨的量**；下面「改坏法」逐条写出**怎么改就红**。

| # | 断什么 | 期望值来源 | 🧨 改坏法（怎么改就红） |
|---|---|---|---|
| ① | 骷髅卡 5 格各有一块 `Box`（黑底方框）· 描边节点共 **20** 条（5×4） | 原版 daily prefab 的 `actionOnActive=5`（不换图 ⇒ 黑方块）+ `Outline` 四条副本 | 每日那一支退回「画圆点」⇒ 两条同时红（`Box` = 0） |
| ② | `Box` 的 tint = **纯黑** (0,0,0,1) | 原版 `m_Color = (0,0,0,1)` | 把 tint 改成别的颜色 |
| ③ | 未达成时 `Outline Top` 的 tint = **米黄** (0.9176471,0.7686275,0.4823530) | 原版 `disabledColor` 实读 | 把描边色换成 `col`（旧的绿/米黄是同一对值但用在圆点上）⇒ 命中不了 |
| ④ | 计数 2 ⇒ **0 个勾** | 原版 `disabledCheckmark = 0` | 把 `if (done)` 去掉（恒画）⇒ 红 |
| ⑤ | 计数 3 ⇒ **1 个勾** + 图 = `40K_settings_icon_checkmark` + 那一格描边变**绿** (0.3312554,1,0) | 原版 `activeCheckmark` / `activeColor` | 同上 + 把 `OutlineActive` 改成别的色 |
| ⑥ | 勾的中心比格心**右偏 24.15px**、**上偏 31.395px**（= 原版 `(21, 27.3)` × 子树 `1.15`） | RT 原文 `anchoredPosition (21, 27.3)` | 画成「与格心重合」⇒ 两条都变 0 ⇒ 红 |
| ⑦ | `Outline Top` 宽 = **46** / 高 = **2.3**；`Outline Left` 宽 = **2.3**；方框高 − 描边高 = **43.7** | 原版 `m_EffectDistance = (2,-2)` + 格 40×1.15 | 只画上下两条 / 把描边画成「往框里缩的 stroke」⇒ 分别红 |
| ⑧ | 层序：勾的队列 **>** 方框、**<** 数字 | 原版 `holder` 兄弟序 `Image → CheckMark → text` | 把三者写成同档 ⇒ 红（同档会按距离排 ⇒ 静默盖错） |
| ⑨ | 周常第 i 格的宝箱档位 = **`[T1,T2,T3,T4,T5,T5]`**（第 6 格夹回 T5） | `stepPrefab` 数组 + `Math.Min` | 把 `Mathf.Clamp` 改成 `index % 5`（第 6 格变 T1）或不夹（越界）⇒ 红 |
| ⑩ | 每格底下那颗 `Dot` 也在、图 = `40k_missions_milestone_off` | 原版 `holder/Image` `m_Enabled=1` | 把 `Dot` 删掉 ⇒ 红（**这条同时是「块1 那条误读」的防复发器**） |
| ⑪ | 宝箱框 **129.0763 × 129.0763**（`PreserveAspect` 内接 ⇒ 正方形） | RT `sizeDelta (72.9499, 59.0763)` + 512² 方图 | 去掉 `keepAspect` ⇒ 142.95×129.08 被拉宽 ⇒ 红 |
| ⑫ | 层序：宝箱 > 圆点；宝箱 **>** 进度条 | 原版 `progress` 子节点序（N=3 → N=4）+ 实拍 | 把进度条调回 `QContent` ⇒ 红 |
| ⑬ | 进度 **5** ⇒ 灰格 = **0** | `WeeklyMissionMilestone__DisplayCheckmark` 第一支 | `current` 恒 `false` ⇒ 这格变灰 ⇒ 红 |
| ⑭ | 进度 **29** ⇒ 灰格 = **4**；第 1..5 格图带 `_open`、第 6 格**闭合**；第 6 格**不灰** | 同上三支 | `current` 恒 `done` ⇒ 灰 0；恒 `false` ⇒ 灰 5；把 `≤` 写成 `<` ⇒ 第 5 格变闭合 |
| ⑮ | 灰格还乘了 `m_DisabledColor = 0.7843137` | 真包 `m_Colors.m_DisabledColor` 实读 | 只换材质不乘色 ⇒ 红 |
| ⑯ | 进度 **30** ⇒ 灰格 = **5**、**第 6 格不灰** | 最后一档 = 第 6 格 | 灰化条件写成「凡是达成就灰」⇒ 第 6 格也灰 ⇒ 红 |

- 判据里那条 shader 名（`"Everguild/UI/Greyscale"`）是**字面量**、⛔ 没读 `WindowButton.GrayShaderName` ——
  核的是**原版那张 shader 的名字**（不是我们自己的常量）⇒ 不是自证。
- 本节收工**还原**本节起点的骷髅计数与周常进度（各一条断言）。

---

## 七、没查清 / 没做的（如实）

1. 🔴 **块1 的 `m_Enabled` 误读（我已核死，但改不到那份文件）**
   - **块1 原文**（`资料/普查产出_1013/A表现核_块1.md` §A389）：「`holder/Image` 挂着 **`m_Enabled = 0`**（原版**根本不画**它）」
   - **实际**（现读两份 MB 原文）：
     `MonoBehaviour_-5355200480893724929.json`（= `WeeklyMissionMilestone` MB 的 `image` 字段指的那个）
     → **`m_Enabled: 1`** + `m_Sprite: {fid10, -6034862274493247622}`（= `40k_missions_milestone_off`）；
     `m_Enabled: 0` 的是**同一节点上的 `Outline` 组件**（`MonoBehaviour_7171112632542172927.json`）。
   - **错因**：`menu_dump.py` 在「一个节点挂多个组件」时，那一行的 `m_Enabled=` **只印其中一个组件**的值
     （周常那张表把 `字段: m_EffectColor,m_EffectDistance,m_UseGraphicAlpha` 与 `**m_Enabled=0**` 印在同一行 ⇒
     读的人自然把 0 归给 `Image`）。**独立反证**：每日那份的 `Outline` 是 `m_Enabled = 1`，同一列就没有 `m_Enabled=0` 标记。
   - **处置**：已就地写进 `Shell/MissionsTab.cs` 的注释（铁律 5 的更正痕迹）；
     ⛔ **块1 那份报告不在我的白名单 ⇒ 请调度台改**（连带 `资料/日常_原版规格.md` §3·7 的订正注若沿用了这句，一起改）。
2. 🟡 **周常 `Step Text` 的自适应下界：我们写 `10`，而「运行期那份 prefab」是 `15`**（**本笔没动**）
   - 页内那份 `Weekly Mission Milestones Step (3)/holder/text` = **auto[10.0~50.0]**；
     而 `MissionMilestonesDisplay.Setup` 会 `DestroyAllChildren` 再 **`Instantiate(stepPrefab[i])`** ⇒
     **运行期真正在用的是 `Weekly Mission Milestone T1..T5`**，那 5 份的 `holder/text` = **auto[15.0~50.0]**（`menu_dump` 实读）。
   - 影响：只有「数字长到装不下」时才有画面差别（我们的数字都是 1–3 字符、fs50 装得下）⇒ **今天零可观测差异**。
   - ⛔ **没动的原因**：改它要同时改 `MissionsTab.cs` 里 `Txt(..., 10f, 50f, 36f)` 那一个实参
     **和 A371 那节的断言**（`CheckFontWindow(wkR37(), "Step Text", 10f, 50f, …)`）——
     那条断言属**别人那一笔**（A371），且白名单只许我在 `Editor/RewardsScene.cs` 加「我这一段」。
   - ⇒ **要做**：判据 = 上面两处 `menu_dump` 实读；**先做**：等 A371 那节无人动时，一行实参 + 一条断言期望值 `10 → 15`。
3. 🟡 **周常灰化的「倍数」实拍核不了**：我按原版序列化字段写（`m_DisabledColor = 0.7843137` + 材质灰化）；
   用户那张参考图是**视频帧**、带终端调色 ⇒ 亮度**无法定标**（我试过按「最亮 10% 像素」对齐 cell 25 与纹理，
   两者比值 0.705 vs 0.590，噪声比信号大）。**如实标**：灰化本身有实拍佐证（铜箱确实是灰的，不是暗棕的
   —— 我用 `×0.784` 与「真灰化」两版跟实拍并排比过，**只有真灰化对得上**），**倍数没有**。
4. ⚠️ **本笔一行 Unity 都没跑** ⇒ §六 那 **36 处**断言**没实跑过**，只过了类型检查。
   归**主对话的同步点**：`RewardsScene.Run`（这一族**没有** `BuildAndSaveScene` 入口，只有 `Run`）。
5. ⚠️ **`Art()` 取不到图时那 6 格的观感**：在跑导入器之前，周常只剩圆点（有 `LogWarning`、不静默）。

---

## 八、顺手发现（⛔ 我一条都没顺手改）

1. 🔴 **`menu_dump.py` 的 `m_Enabled` 列会误导**（见 §七·1）—— 一行多组件时只印一个组件的值、
   且 `字段:` 列印的是**另一个**组件的字段。**这条已经害过一份报告**（块1）。
   ⛔ 我没动它（别的写手在改）⇒ 建议它**逐组件印**（或至少把 `m_Enabled=0` 标注到具体组件名上）。
2. 🟡 **`MissionMilestonesDisplay.stepPrefab` 这个数组在真包里是「按档位数配的」**：
   5 元那三份 = `[T1..T5]`；1 元那份挂在**每日行**的 `Mission Milestones Progress` 上（pid `7837716569381755339`）。
   ⇒ 谁再碰里程碑，**先读这个数组**，别按名字猜。
3. 🟡 **同一份 prefab 的两个来源（页内实例 vs 独立 prefab）在字号上不一致**（§七·2 就是它）
   —— 这正是本仓反复踩的「判据源分叉」（`MissionsTab.cs:877` 那条 A77-㉓⑨ 注释记过同一类事）。
   **本笔的取舍**：**几何/图**一律取**独立 prefab `Weekly Mission Milestone T1..T5`**（它们才是运行期被 `Instantiate` 的），
   而 `Weekly Mission Milestones Step (3)` 那份**只是作者预览**（会在 `DestroyAllChildren` 里被删）。
4. 🟡 **`40K_settings_icon_checkmark` 是一张多处复用的图**：对战内设置面板那个勾（A424）与每日里程碑的勾**同一张**
   （导入器 `:714` 早在列）。
5. ⚠️ **`Editor/RewardsScene.cs` 此刻有 3 个写手**：`git diff` 的三个 hunk =
   `@@ -1446,20 +1446,44 @@`（**不是我的**）· `@@ -2215,6 +2239,277 @@`（**我的**）· `@@ -8339,6 +8634,70 @@`（**不是我的**，看着像 W1 的 A239/A353，锚点 `:8341` 对得上）。
   ⇒ 三者**不重叠**；本件只往中间那段加，没碰另外两段。

---

## 九、判据原始读数（可复跑）

### 9·1 三份 `Mission Milestones Progress` 的 `stepPrefab`（现读）

```
MB -2694294769260735734   stepPrefab = [-860478294803506117, -1028855196971426013,
                                       -80993620748433015, 5443275201469238768, -3371964447567246684]
MB -533312439237404929   ＝ 同上（逐个 pid 相同）
MB -7530505938951294279  ＝ 同上
```
复现：`python - <<…` 扫 `d:/2/新解包资源/assets_full/bundle_menus_assets_all/MonoBehaviour/*.json` 里含
`stepPrefab` 的 18 份（其中 5 元的 3 份、1 元的 15 份）。

### 9·2 那 5 个 pid 的 `activeCheckmark` / `disabledCheckmark`（现读 + pid 索引）

| stepPrefab[i] | MB | `activeCheckmark` | `disabledCheckmark` |
|---|---|---|---|
| `-860478294803506117` | （与 `-106036988674173185` 同值） | `40k_Crate_Tier1_Iron_open` | `40k_Crate_Tier1_Iron` |
| `-1028855196971426013` | T2 | `40k_Crate_Tier2_Copper_open` | `40k_Crate_Tier2_Copper` |
| `-80993620748433015` | T3 | `40k_Crate_Tier3_Silver_open` | `40k_Crate_Tier3_Silver` |
| `5443275201469238768` | T4 | `40k_Crate_Tier4_Gold_open` | `40k_Crate_Tier4_Gold` |
| `-3371964447567246684` | T5 | `40k_Crate_Tier5_Warp_open` | `40k_Crate_Tier5_Warp` |

名字来源 = `d:/4/_tmp_view/sprite_pids_ALL.json`（`工具/menu_dump.py:sprite_pid_map()` 扫**真包**建的索引）。
⚠️ 这 10 个 pid 在**解包 JSON 的 `Sprite/` 目录里查不到**（`fid7` 跨 SerializedFile）⇒ **只有走真包 pid 索引才解得开**
（这正是 R1 当年写「解析不出名字」的原因）。

### 9·3 实拍复核（用户那张参考图）

`C:/Users/qjh36/Desktop/奖励—布道所（每日任务）参考图.png` 的「每周挑战」一行（进度 `25/30`）：
- 第 1 格 = Tier1 Iron（深灰 + 盖上翼形圆徽）· 第 3 格 = Tier3 Silver（**盖上 X 形交叉扣**，一眼可辨）·
  第 5、6 格 = **同款紫色的 Tier5 Warp**（一格开、一格闭）⇒ 与 `[T1,T2,T3,T4,T5,T5]` 逐格吻合。
- 第 1..4 格的箱子**明显去色**（铜箱看起来是灰的、不是棕的）⇒ 与「已达成但非最后一档要灰」吻合
  （我另做过并排对照：`×0.784` 的铜箱仍是**棕的**、只有**真灰化**对得上 ⇒ 灰化这条不是猜的）。
- 第 5 格（`25`）是全彩 + 紫光 ⇒ 它是 `lastReachedMilestone` ⇒ **不灰**。
- 金色进度条**被那 6 个箱子压住** ⇒ 层序那条的实拍依据。

### 9·4 复跑命令（全只读）

```
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Daily Skulls Mission Container Small" --depth 7 --relative --no-layout
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Weekly Mission Milestone T1" --depth 4 --relative --no-layout
python d:/4/Unity/工具/menu_dump.py bundle_menus_assets_all "Weekly Mission Milestones Step (3)" --depth 3 --relative --no-layout
```

---

## 十、类型检查结果

```
$ TMPDIR=/tmp/wf_wm1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 第 1 次（改完 `MissionsTab.cs`、还没改断言时）报过一条 **`Battle/ScenarioBlendables.cs(2140,33): error CS0103: 当前上下文中不存在名称"FindChildByName"`**
  —— **不是我的文件**（别的写手当时正在改它）；隔一轮再跑已消失。
- 倒数第 2 次又报过 **3 条 `Shell/DailyData.cs(1479/1566/1640,20): error CS0246: 未能找到类型或命名空间名"Exception"`**
  —— **也不是我的文件**（另一个写手正在往里加 `throw new Exception(...)` 而漏了 `using System;`）；
  **隔 75 秒再跑**已消失（`运行时错误数: 0`）。
- ⇒ 全程**两次**都落在「错误集中在**别人的**文件」那一档，**我一次都没去改别人的文件**（铁律 13·3）。
- 行尾：三个文件改完**按二进制数过** —— `MissionsTab.cs` `CRLF=0 / LF=1499` · `RewardsScene.cs` `CRLF=0 / LF=8877`
  （两个都是纯 LF、**没被翻**）· `import_original_art.py` `CRLF=1520 / LF=1520`（纯 CRLF、**没被翻**）。
- `git diff --numstat`：`Shell/MissionsTab.cs` **250 加 / 31 删** · `Editor/RewardsScene.cs` **388 加 / 10 删**
  （其中 **271 加是我的**（改后 `:2259-2529`），另两段是别的写手的）· `工具/import_original_art.py` **9 加 / 0 删**。
