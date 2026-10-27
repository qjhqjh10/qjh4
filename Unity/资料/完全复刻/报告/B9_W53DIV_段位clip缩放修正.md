# B9_W53DIV —— 段位 clip 的 `Rank Icon` 缩放修正（「播完永久大 9.02%」）

> 代理 `B9_W53DIV`（2026-10-27）· **执行型**。
> 判据一手来自 `B9_W47SCALE` 的 **`资料/完全复刻/判据/升级动画scale曲线_节点映射.md`**（§三·2 ④ / §五·B）—— **本代理没有重查原版**，只做「按判据改 + 回读验证 + 两处注释订正」。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未越白名单（只碰 4 个文件：`工具/gen_division_clips.py` · 两份 `division_clips.json` 产物 · `工具/gen_anim_clips.py:29` · `Shell/CardDisplayEffects.cs:11`）。

---

## 一、结论

1. **真缺陷已修**：`工具/gen_division_clips.py` 现在对 **path == `Division Image/Rank Icon`** 的 scale 三条（x/y/z）统一 **÷ 1.0903291702270508**（= 原版该节点的**静止** `localScale`；我们已把那个因子烘进 `RankIconR` 的 73.51 px 视觉框、节点静止缩放是 **1**）。
   两条驱动（原生 `clip.SetCurve(...)` 与手动 `ApplyChannels` 的 `ScaleTrack`）**读同一份 JSON** ⇒ 一处改，两条同时正。
   **后果消除**：`Rank Up` 播完不再永久大 9.02%（末键 1.0903292 → **1.0**，与静止缩放一致），峰值也不再是 ×2.228 而是 **×2.043**，回弹底 **0.70832**；子件 `Rank Icon Glow` 跟着一起归位。
2. **生成物重跑两遍逐字节相同**（`cmp` 通过，`--check` 通过）；两份产物（运行期 `Resources/` 与「全量件」`数据/游戏数据/`）**彼此逐字节相同**，`sha256 = e4d925f3d01ba2ca210bc2eae03715995dde1bab2531c136d600e5b666d4347f`（24,039 字节）。
3. **回读证明「其它 path 零变化」**：整份 JSON 做深结构 diff ⇒ **差异恰好 12 条**，全部是那三条曲线的 4 个 `v`；**其余所有曲线（含 `Division Image` 的 ③⑤ 与全部 float 曲线）连同 `t`/`si`/`so` 逐位相同**，元数据面（`schema`/`note`/`source`/`owner`/`animation`/`key`/`name`/`length`/`events`/`src`）全部相同。
4. **两处注释已就地订正**（都**零行数变化**）：`工具/gen_anim_clips.py:29`（`Upgrade Panel` → **`CardUI`**、250 的量纲说清）· `Shell/CardDisplayEffects.cs:11`（`Lighting Rays` 是 **`CardUI` 的直接子件**、层级次序）。
5. **按判据刻意不动**：`Division Image`（③⑤）的三条曲线（本来就对，改它反而错）· `m_StopTime`（两处都是 1、都不是 length）· 死资产 `Resources/Animations/Rank Up.anim` / `Division Up.anim`（**本波不删**，删文件要用户点头 —— 见 §六·4）。

**必跑结果**：`TMPDIR=/tmp/wf_w53 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 / 编辑器错误数 0**；两个 `.py` 都过 `py_compile`；生成器自检（含新加的 ⑥b）通过。

---

## 二、证据（文件:行号）

### 2.1 改前 / 改后：`Rank Up` / `Division Image/Rank Icon` 三条（x / y / z 三轴**逐位同值**）

| key `t` | 改前 `v` | 改后 `v` | 改后÷改前 | `si`/`so` |
|---|---|---|---|---|
| 0.38333332538604736 | `1.0903291702270508` | **`1.0`** | 0.917154220309 | 0/0 → 0/0 |
| 2.5999999046325684 | `2.2275424003601074` | **`2.042999913408024`** | 0.917154220309 | 0/0 → 0/0 |
| 2.6500000953674316 | `0.772301971912384` | **`0.7083200128925831`** | 0.917154220309 | 0/0 → 0/0 |
| 2.866666555404663 | `1.0903291702270508` | **`1.0`** | 0.917154220309 | 0/0 → 0/0 |

- 三轴（`localScale.x` / `.y` / `.z`）**12 个数逐一核对，全部等于上表**（比例恒 0.917154220309 = 1 / 1.0903292）。
- 第一条与末一条**恰好是 1.0**（不是 0.9999999…）—— 实现里用的是**除法**（`v / 1.0903292`）而不是乘倒数，所以 `x / x` 精确为 1。
- 原版这四个数（`1.0903292 / 2.2275424 / 0.77230197 / 1.0903292`）本代理**从解包 clip 现读核对过一遍**（`bundle_menus_assets_all/AnimationClip/AnimationClip_4418588262184220504.json` 的 `m_ScaleCurves`，四键 `si/so` 确实全 0），与判据文件 §一·2 ④ 逐位一致 —— 这一步只是**确认判据文件抄得对**，不是重新设计口径。

### 2.2 「其它 path 一个都没变」的证明（结构化 deep-diff，不是只看字节数）

对 `改前`（本波开始时的快照）/ `改后` 两份 JSON 递归比对（dict 键序、list 长度、叶子值全比）结果：

```
=== 结构差异条目数 = 12 ===
  /clips[2]/curves[3]/keys[0..3]/v     ← Rank Up / Division Image/Rank Icon / localScale.x
  /clips[2]/curves[4]/keys[0..3]/v     ← … / localScale.y
  /clips[2]/curves[5]/keys[0..3]/v     ← … / localScale.z
  其它全部曲线（含 Division Image 的 ③⑤ 与所有 float 曲线）逐位相同 = True
  schema / note / source / owner / animation 相同 = True
  key / name / length / events / src          相同 = True
```

**字节账自洽**：`24,129 → 24,039` = **−90 字节**，与 12 处数字的字符差逐项相等
（每条曲线：首键 `1.0903291702270508`(18) → `1.0`(3) 少 15；次键 18 → 17 少 1；三键 17 → 18 多 1；末键少 15 ⇒ 每条 −30 × 3 条 = **−90**）。

**幂等**：连跑两遍，两份产物各自 `cmp` 逐字节相同；第三遍 `--check` 打印「✅ 两份产物与现生成一致」。
（另：改前/改后的 `sha256` = `3674915…` / `e4d925f3…`，可直接用于下次复跑对账。）

### 2.3 代码改动落点

| 文件:行 | 内容 |
|---|---|
| `工具/gen_division_clips.py:42-44` | 文件头新增一条判据指针（映射判据 → `资料/完全复刻/判据/升级动画scale曲线_节点映射.md` §三·2 ④ / §五·B）＋「本文件唯一一处换算」的口径 |
| `工具/gen_division_clips.py:129-137` | 新增常量 `RANK_ICON_PATH = "Division Image/Rank Icon"` · `RANK_ICON_REST_SCALE = 1.0903291702270508`（带出处：prefab tran `224743393880019028` 的序列化 `localScale`；`RankIconR` 73.51 px = 67.4182 × 1.0903292） |
| `工具/gen_division_clips.py:175-201` | `_explode_scale()`：`div = RANK_ICON_REST_SCALE if path == RANK_ICON_PATH else 1.0`，对 `v` / `si` / `so` **一起除**（切线是「值 / 秒」，值的量纲换了切线必须同因子；该 path 现读 `si/so` 全 0 ⇒ 数值上无观察差异，但按量纲一并除才对） |
| `工具/gen_division_clips.py:441-446` | 断言 ⑥ 的期望值 `("Division Image/Rank Icon", 1.09033)` → **`1.0`**，注释改成「**映射后的值**（原版 1.09033 ÷ 静止缩放 1.0903292）」并加一行「⛔ 别把原版值抄回来当期望」 |
| `工具/gen_division_clips.py:460-471` | **新增** ⑥b：`Rank Icon` 三条 x/y/z 的**四个键全部断言**（`1.0 / 2.043 / 0.70832 / 1.0`）；期望值取**判据文件那张表的四舍五入值**，⛔ 不拿 `RANK_ICON_REST_SCALE` 反算（避免拿实现自证） |
| `工具/gen_anim_clips.py:29` | 注释订正（**零行数变化**，见下） |
| `Shell/CardDisplayEffects.cs:11` | 注释订正（**零行数变化**，见下） |

**两处注释订正的具体内容**

- `工具/gen_anim_clips.py:29`：原来写「（原版 `Upgrade Panel` 根的真实缩放）」。改成
  「⚠️ 2026-10-27 现核：那个根是 **`CardUI`**（`MainMenu Warpforge.unity` tran `&1516`），**不是 `Upgrade Panel`** —— 后者只是控制器 `CardDisplayLevelUpAnimationController` 的宿主；`250` 是「**1 卡单位 = 250 画布 px**」的静止量纲、**不是放大倍数**」。
  **结论「别照搬（会放大 250 倍）」一字未改**；只修「根是谁 / 250 是什么」这两处会误导下一个人的事实。
- `Shell/CardDisplayEffects.cs:11`：原来写 `… ▸ CardUI ▸ Cards ▸ Card Display ▸ 2DCard ▸ {Glow Explosion, Lighting Rays, …}`。改成
  「（2026-10-27 现核）：`Card Displayer Menu For Menu ▸ Card Display ▸ Cards ▸ CardUI ▸ {2DCard ▸ {Glow Explosion, Level Up Effect, …}, Lighting Rays}` —— ⚠️ `Lighting Rays`（tran `&1574`，`m_Father = &1516`）是 **`CardUI` 的直接子件、不在 `2DCard` 下**；原注释把 `Cards`/`CardUI` 的次序写反了、并多写了一段 `Card Display`」。
  **本代理自己现读核过一遍**（`d:/2/tools/assetripper_out/ExportedProject/Assets/Data/Scenes/MainMenu Warpforge.unity`）：
  `tran 1574 = Lighting Rays`，`m_Father = 1516`；`tran 1516 = CardUI`，`m_Father = 1589 (Cards)`，`m_LocalScale = {250,250,250}`；
  `CardUI.m_Children` 序 = `CreatedByText · 2DCard · Card Ready for level up · New Card Badge · Ban Icon · **Lighting Rays**`；
  父链 = `CardUI ▸ Cards ▸ Card Display ▸ Card Displayer Menu For Menu ▸ 3 - PopUp Holder ▸ Safe area Only Horizontal ▸ MainMenu ▸ Main  Canvas ▸ Game UI`（除 `CardUI` 外每一级 `localScale` 都是 1）。
  ⇒ 与 `B9_W47SCALE` 判据文件 §二·1 逐条吻合。
  **零行数变化已复核**：`CardDisplayEffects.cs` 改前改后都是 **478** 个 `\n`、`CRLF = 0`、无裸 `CR`；`gen_anim_clips.py` 都是 **293**（⇒ 该文件里 `:54` / `:60-62` / `:187` / `:228` / `:258-266` / `:275-283` 那些被别的报告引用的行号**没有漂移**）。

### 2.4 没有新增/改判账行（说明）

本次是**生成物数值修正 + 生成器逻辑 + 注释**，不产生新的「类 × 方法 → 原版实现」映射。相关既有账行（`账/个人资料_成就_排行.tsv:223` `DoDivisionChangeAnimations` · `:224` `DoPromoteAnimations` · `:225` `DoRankChangeAnimations` · `:228` `Open`）**状态与本次无关**（它们停在 B/类级命中，是因为方法体级对应未核，不是因为这条缩放）。
⇒ **本波不写 `账/_parts/B9_W53DIV.tsv`**：写一条没有类×方法语义的行只会制造冲突/漂移。若调度台要求补账，建议的形式是给上面 `:225` 那行加一句备注（「曲线数值已按静止缩放映射，见 `报告/B9_W53DIV_…md`」），但那属于调度台合并时的动作。

---

## 三、没查清的部分（⛔ 不猜）

1. **肉眼效果没验**（本波不许跑 Unity）：「播完归位」「峰值 ×2.043」「回弹底 0.70832」都是**数据层**结论。观感要在真机/Unity 里看（§五 给了具体验法）。
2. **原生 `Animation` 与手动 `ApplyChannels` 的写入先后**：我只按判据「两条驱动读同一份 JSON、都会写」改值；**没有**去读这两条谁最后写、会不会互相抖动的实现细节（那份代码在 `Shell/RankedDivisionChangeWindow.cs` 的动画段，属别的波次范围）。若两条在**同一帧**写不同值（原生按 clip 时间、手动按自己的计数器），可能出现一帧级抖动 —— **本代理没跑，不断言**。
3. **`Rank Icon` 的视觉可观性**：那颗 `m_Sprite = 0`（运行期由服务端 `RankedDivisions.GetRankData` 给）⇒ 在拿到图之前，**能看见这次修正确实生效的只有子件 `Rank Icon Glow`**（黄加色块）。要肉眼确认「缩回静止大小」，最好看 Glow 的尺寸变化，或临时给 `Rank Icon` 一个替身图。
4. **`Rank Icon Glow` 的父级继承**：文本推理上，原版 Glow 是 `Rank Icon` 的子件、父静止 1.0903292 ⇒ 视觉被放大 9.02%；我们父静止 1 ⇒ 现在两边一致。但**没有在 Unity 里量过两个三角形的实际像素尺寸**。

---

## 四、改动清单

| # | 文件 | 改动 |
|---|---|---|
| 1 | `d:/4/Unity/工具/gen_division_clips.py` | 新增 2 个常量 + 4 行判据注释；`_explode_scale()` 对 `Rank Icon` path 的 `v/si/so` ÷ 1.0903292；断言 ⑥ 期望 1.09033→1.0；新增断言 ⑥b（四个键全断）。476 → 513 行（`.py`，无账行引用） |
| 2 | `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/division_clips.json` | 生成物重跑：12 个数字变化，24,129 → 24,039 B，LF，无 BOM（生成器 `newline="\n"`） |
| 3 | `d:/4/Unity/数据/游戏数据/division_clips.json` | 同上（与 #2 逐字节相同） |
| 4 | `d:/4/Unity/工具/gen_anim_clips.py:29` | 注释订正（**零行数变化**：293 → 293） |
| 5 | `d:/4/Unity/MyGame/Assets/CardPresentation/Shell/CardDisplayEffects.cs:11` | 注释订正（**零行数变化**：478 → 478） |

⛔ 没有动：`Division Image` 的 5 键曲线（③⑤）· `m_StopTime` · 任何 `.unity` / `ProjectSettings` · 任何别的代理正在写的文件。

---

## 五、同步点要验什么（这段动画的肉眼效果仍要 Unity 验）

1. **Unity 自检（必跑，主对话统一跑）**：`ShellScene.Run`（本段在 `Editor/ShellScene.cs:9479-9581`）。
   - 现有断言**不会因本次改值变红**：那段只断 `Division Image` 播完 = 1.0（`:9551-9552`）与曲线条数/长度/事件，**没有任何一条断言 `Rank Icon` 的缩放值** —— 这正是本缺陷能长期存活的空档。
   - 🔴 **建议在同步点补一条（本代理无权改 `ShellScene.cs`）**：
     `CheckNear(winRank.NodeScaleForTest("Division Image/Rank Icon").x, 1.0f, 0.005f, "★ 播完 `Rank Icon` 缩放回到静止 1.0（我们静止缩放是 1、1.090329 已烘进矩形；旧数据这里会得 1.0903 ⇒ 红）")`
     —— 走的是**已有 API**：`RankedDivisionChangeWindow.NodeScaleForTest(string relPath)`（`Shell/RankedDivisionChangeWindow.cs:1032`，`relPath` 支持 `Division Image/Rank Icon`）。
     **灭自证性**：旧数据（末键 1.0903292）会差 0.09 > 0.005 ⇒ 红；把生成器改回原值也红。
2. **真机/肉眼**（进段位窗，`IsDivisionChange=false` = `Rank Up` 一档）看三处：
   - **播完（3.9167 s 之后）**：`Rank Icon` / `Rank Icon Glow` 应**停在与开窗前一样大**（旧版会大 9.03%，≈ 73.51 → 80.15 px 的等效，肉眼是「图标静置后比进场时胖一圈」）。
   - **峰值（2.6 s）**：放大幅度应为静止的 **2.043 倍**（旧版 2.228 倍）。
   - **回弹底（2.65 s）**：应为静止的 **0.7083 倍**（旧版 0.7723 倍）。
   - ⚠️ 可观测性见 §三·3（`Rank Icon` 无图时只有 Glow 能看出来）。
3. **复跑口径**：`python -I -X utf8 d:/4/Unity/工具/gen_division_clips.py --check` 应打印「✅ 两份产物与现生成一致」；产物 `sha256` 应为 `e4d925f3d01ba2ca210bc2eae03715995dde1bab2531c136d600e5b666d4347f`。
4. **跨波冲突提醒**：本波改了 `Resources/division_clips.json` 的**12 个数字**；若同一同步点上还有代理在改这个 JSON 或它的生成器，**以本波为准**（别让后写的把 1.0903292 写回）。

---

## 六、顺手发现（⛔ 本代理没改，交调度台分流）

1. **`工具/gen_anim_clips.py:30` 也是过期口径**（不在本波授权范围内 ⇒ 只报不改）：
   现文「`Rank Up` / `Division Up` 的两条绑 `Division Image` / `Rank Icon`（**我们树上没有这两个节点**）⇒ 天生绑不上」。
   实际自 2026-10-27 起 **`RankedDivisionChangeWindow` 已经建了这两个节点**（`Shell/RankedDivisionChangeWindow.cs:91` `DivisionR` / `:96` `RankIconR`、`:1259-1293` 建件）—— 见 `B9_W47SCALE` 判据文件 §三·1。⇒ 该句「我们树上没有」应改成「升级窗那棵树里没有（本窗是 `CardDetailPopup`）/ 段位窗里有，但那两条曲线走 `division_clips.json` 而不是本生成器的 `.anim`」。
   ⚠️ 后果很轻（该段只在解释「为什么本生成器不产出 `m_ScaleCurves`」），但会误导下一个人以为段位窗也缺节点。
2. **`报告/B9_W47SCALE.md:29` 的行号引用了本波改动过的文件，已漂移**：
   它写 `工具/gen_division_clips.py:161-170`（`_explode_scale`）· `:419-434`（断言首键 `1.09033`）。改后实际是 **`:175-202`** · **`:441-471`**（且断言期望已变 `1.0`）。⛔ 别人的报告我不改，请调度台合并时订正（或由原代理回改）。
3. **`ShellScene` 在这条链上缺「`Rank Icon` 缩放」断言**（见 §五·1）。这是本缺陷能在自检全绿下活下来的原因 —— 建议同步点补上；同一族的「灭自证」写法可参考 `:9551-9552`（`Division Image` 那条）。
4. **`Resources/Animations/Rank Up.anim` / `Division Up.anim` 是死资产**（判据见下，本波**按要求没删**）：
   全仓唯一 `Resources.Load<AnimationClip>("Animations/…")` 的加载点只取 `Level Up Card`（⇒ 段位窗那两份 `.anim` 从来没人加载；段位窗走的是 `Resources/division_clips.json` 的**运行期 `SetCurve` 重建**）。**删文件要用户点头** ⇒ 建议进「待用户裁定」清单，别在本批顺手删。
   （另：它们里面的 `m_ScaleCurves` 一直是「有意不生成」，与本波无关。）
5. **`Rank Icon` 的 3 条 scale 曲线在 `Division Up` 里也有**（本波**未动**、也不该动）：`Division Up` 的 `Rank Icon` 曲线只有 `m_IsActive`（开/关），scale 仍只属于 `Rank Up`；我在 diff 里确认过 `Division Up` 的缩放曲线（`Division Image` 那条）**逐位未变**。记一笔防止后人以为「两条 clip 要一起除」。

---

### 交接给下一个代理（若本波被截断）

- 已完成：生成器改 + 重跑（两遍幂等）+ 回读验证 + 两处注释 + typecheck 0/0 + 本报告。
- 唯一遗留的「我自己该做完但做不了」：无（§五 的点都是**同步点**由调度台跑 Unity / 真机，或需要改 `ShellScene.cs` 而它不在我的白名单）。
- 复跑命令：`python -I -X utf8 d:/4/Unity/工具/gen_division_clips.py`（写盘）/ `--check`（只比）。
