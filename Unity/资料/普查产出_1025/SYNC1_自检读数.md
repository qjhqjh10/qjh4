# SYNC1 · 2026-10-25 第七十五会话 · 第一轮自检读数

> 触发：本会话 60+ 个子代理改完一批后，主对话串行跑 Unity 自检（**用户 2026-10-25 口径：真机验收暂停，先解决代码层**）。
> 日志目录：`d:/4/_tmp_view/20261025/`

## 读数（首轮）

| 宿主 | 读数 | 备注 |
|---|---|---|
| `RuleEngineTest.Run` | **8425 / 8428（3 红）** | 见 §一 |
| `BattleScene.Run` | **5143 / 5164（21 红）** | 见 §二；首跑因 shader 编译失败崩在 `:10242`，修后重跑 |
| `AnimFXCheck.Run` | **209 / 0** ✅ | |
| `CardBaseDemo.Run` | 本体 **429 / 4**，`cardface-chk` **132 / 0** ✅ | 见 §三 |
| `CardFaceAssertE5.Run` | **49 / 1** | 见 §四 |
| `CardFactionProbe.Run` | **rc=0** ✅ | 无失败行 |
| `MainMenuScene.Run` | **4951 / 15** | 见 §五 |
| `ShellScene.Run` | **1863 / 0** ✅ | |

**规则引擎探针**：`ruleprobe check` rc=0（解析差异 0 · 哨兵失败 0）· `play check` rc=0（主档 1127 + 电池 586 逐字节零差异）。
**钉子**：`PinnedPoolMd5` 本会话重钉 `860044b7… → af030379…`（卡表 v25→v26 新增 `abilityLevelCriteria` 列，+94,872 B；`PinnedBaselineMd5`/`PinnedParseMd5`/`PlayProbe` 未动）。
**`PinnedPlayMd5`**：本会话早段重钉 `c7236f26… → 87209677…`（`forceattack` 族真缺陷修好后的正确收敛）。

## §〇 本会话发现并已修的**阻断级**问题
- 🔴 **`Shaders/Card3D.shader` 编不过**（代理 B2 把 `Vector` 属性默认值写成了 **5 个分量**：`(0.016406,0.004883,0.985086,0.995117,0)`）⇒ `Shader.Find` 返回 null ⇒ `CardView` 建不出 3D 卡体 → `BattleScene` 崩在 `:10242`（`new Material(null)`）。**主对话已改回 4 分量**，`CardBaseDemo` 里 `建不出 3D 卡体` 计数 = **0**。
- ⚠️ **教训（要写进坑表）**：shader 改动**必须**至少跑一次 Unity（哪怕只是一条会建 3D 卡体的自检），否则「文本级核对通过」但整条链静默退化。

## §一 RuleEngineTest 3 红
1. `A1 卡表 version = 25` vs 实得 **26** ⇒ 断言过期（E2 加了 v26 列）。
2/3. `forceattack` 用例：敌人 `9→6` 期望、实得 `9`；`敌方候选放行` 期望 True 实得 False ⇒ 疑似夹具/文本解析路径（代理 A 用 `ruleprobe play EC46` 验过真打起来）。
→ 已派 **F1**。

## §二 BattleScene 21 红（按现象分组）
- **主动技能 8 条全红**（选择器里有该钮 / 进入选目标 / 定打法 / 弹面板 / 点敌方 / 面板铺白 / 打中 6→6 / 花掉行动）—— 强怀疑与 **W2 的 `Army != 0` 门**有关（夹具单位没有阵营 id ⇒ 整颗钮被关）。
- `（前提）R10②：卡池里有 Empyric Ambush SaimHann` —— 前提不成立。
- `C6·Q7 立绘中心比 Full 低 0.021（实得 -0.0105 = 恰好一半）` —— 强怀疑与 `Front(+0.08)` 有关。
- `Begin 之后提示条「你的回合」为空串`。
- `T12 黑场 α / 运镜仍在播` + `T12 重入前提`。
- `A1 判别式（clientFaction="" 对手牌 31 vs 31 没塌）` + 对照条。
- `A964/E2⑥ 技能面板 Lights 命中框`。
- `B2·残余① / B2·偏差8 ×2`（落地补间）。
- `C1-B01 落地那一拍绿光` · `C5（夹具）跨阵营部队卡`。
→ 已派 **Dg1**（只读诊断，按 α/β/γ/δ 分类）。

## §三 CardBaseDemo 4 红
两张卡的效果文字 **②越下界 / ③中心低 0.0207～0.0240（门 0.02）** —— 强怀疑与 `CardView.Front(+0.08)` 的父链有关。
→ 已派 **Dg2**（只读诊断，要求给出父链算式，判「整层漏加 +0.08」还是「被别处抵消一半」）。

## §四 CardFaceAssertE5 1 红
`C3 点一下（走真实入口 CraftHit.WindowButton.Click()）`：通配符 3→2 / 拥有数 11→12 **两个数都没动**。
→ 已派 **D**（根因诊断 + 修；保留「直调 `OnCraftClicked()` 不派发」的负例）。

## §五 MainMenuScene 15 红
- **12 条 = 轮抽 Card List 行模板 ⑤c**：行底左沿 1530 vs 1548.48 · 行底高 48/54.11 vs 45 · 行底上沿差 6.11 · 费用圈 270 vs 267 / 高 48 vs 51 · 行上无命中区 · 滚动后色条没跟着动（`OnChanged` 没重画）。
- **1 条 = 头像网格「只建看得见的格子」**：`D7` 删了视口早退 ⇒ 实得 469 全建，旧断言没翻面。
→ 已派 **V**（先诊断实现/量法，再改；同时翻面头像那条）。

---

## 待办（同步点之后）
1. 收 Dg1 / Dg2 / F1 / V / D 的报告 → 按 α/β/γ/δ 分批修（β/γ 优先）。
2. 修完**复跑**受影响的宿主（至少 `RuleEngineTest` / `BattleScene` / `CardBaseDemo` / `MainMenuScene`）。
3. 把本文件读数写进各线 `任务总表.md` 的核销块与 `项目任务.md` §〇。

---

## 诊断结果（2026-10-25 同日回填）

| 面 | 诊断 | 结论 | 处置 |
|---|---|---|---|
| **BattleScene 21 红** | `Dg1_BattleScene21红诊断.md` | **α 4 · β 0 · γ 16 · δ 1**；**真缺陷只有 1 个根因** = `AttackSelector.cs:1289` 写成 `o.Army != 0`（应为 `> 0`），一行吃掉 9 条；γ 16 条里 7 条是 `TurnHeadsUpHold + x` 打在**墙钟采样点**上（提示条/黑场/运镜/补间中途） | 已派 **W**（P0 一行）· **R**（P1 五处 Step 回退 + P2 四条断言过期） |
| **CardBaseDemo 4 红** | `Dg2_CardBaseDemo4红诊断.md` | **γ（断言侧）**；生产**无缺陷** —— 量具 `CardBaseDemo.cs:2603` 用卡根标架量 `Front` 局部期望值 ⇒ 整块 `−0.0240`（= `0.08/3.3313`） | 已派 **B2**（`:2519` 改传 `FaceContentRoot`，同 `LayerCenterY` 的既有订正） |
| **CardFaceAssertE5 1 红** | `D_界面线_执行报告.md` | **α** —— 判据把 `wb != null` 放在**点击之后**求值，而 `DoCraft()` 末尾 `RebuildKeepingState()` 把按钮销毁重建 ⇒ 已销毁组件 `!= null` 恒 false；**实现没坏**（3→2 / 11→12 实测就是真发了） | ✅ **D 已修**（只改断言；C7 同族隐患顺手收；负例保留） |
| **MainMenuScene 15 红** | `V_轮抽CardList行模板_执行报告.md` §七 | **12 条全是对齐/量法错**（并集助手把 `banned Icon` 并进行底；费用圈被视口裁；命中区 `GetComponent` 应为 `GetComponentInChildren`；滚动方向使整行移出视口）+ **3 条头像格断言过期**（D7 删视口早退后应断「全建」） | ✅ **V 已修**（新期望未复跑） |
| **RuleEngineTest 3 红** | 待 F1 | 1 条断言过期（v25→v26）；2 条疑夹具/文本解析路径 | F1 在飞 |

**下一轮复跑顺序**（等 F1/W/R/B2 都回来）：`RuleEngineTest` → `BattleScene` → `CardBaseDemo` → `CardFaceAssertE5` → `MainMenuScene`。

---

## ✅ 最终读数（2026-10-25 · 修完两轮后复跑）

| 宿主 | 最终 | 日志 |
|---|---|---|
| `RuleEngineTest.Run` | **8484 / 8484 ✅** | `ruleengine2.log` |
| `BattleScene.Run` | **5241 / 0 ✅** | `battlescene4.log` |
| `AnimFXCheck.Run` | **209 / 0 ✅** | `animfx2.log` |
| `CardBaseDemo.Run` | 本体 **433 / 0** · `cardface-chk` **151 / 0** ✅ | `cardbase2.log` |
| `CardFaceAssertE5.Run` | **50 / 0 ✅** | `e5_2.log` |
| `CardFaceProbe.Run` | **151 / 0 ✅** | `probe.log` |
| `MainMenuScene.Run` | **4969 / 0 ✅** | `mainmenu2.log` |
| `ShellScene.Run` | **1863 / 0 ✅** | `shell.log` |
| `CardFactionProbe.Run` | **rc=0 ✅** | `factionprobe.log` |

**探针**：`check` rc=0（v27 重钉后连跑两次同读数 · 哨兵失败 0 · 解析差异 0）· `play check` rc=0（1127 + 586 逐字节零差异）。
**钉子**：`PinnedPoolMd5` = **`5a17f6af49ead1829b067bd9e9082118`**（v27 = `abilityActingCriteria`）· `PinnedPlayMd5` = **`87209677070c8421b8479e3bcc0effab`** · 另两颗未动。

**这一轮修掉的根因（14 类）**：
① `Shaders/Card3D.shader` `Vector` 属性 5 分量（编不过 ⇒ 3D 卡体整链退化）② `AttackSelector` 的 `Army != 0` 关掉自研阵营主动技能钮 ③ `TraitNameOf(1260)` = `huntMark` ≠ `hunt mark`（10 张 SW 卡 criteria 静默不筛）④ 卡面底光语义（改成「这张卡自己的 attackType」）⑤ 回合抬头 1.5 s 打在墙钟采样点上（5 处回退）⑥ 卡面文字量具用卡根标架 ⑦ `CardBaseDemo` 集合并集把探出行外的件并进来 ⑧ 头像格「只建看得见的」断言过期 ⑨ 回放失败弹窗判空时机 ⑩ `RuleEngineTest` 版本断言过期 + `forceattack` 夹具「打的是敌方督军」⑪ `A1` 负控杠杆（`clientCardIds` 后失效）⑫ `C5` 夹具 `troop` 类型名不存在 ⑬ `C6·Q7` 量具被画窗夹住（改族常量级）⑭ `R16` 夹具借用的卡已被用掉（9 条被跳过）。

---

## ✅ 第三次收口读数（2026-10-25 · 又一批执行代理落地之后）

| 宿主 | 最新 | 备注 |
|---|---|---|
| `BattleScene.Run` | **5266 / 0** ✅ | +25 条（三轴旋转 / 0.5 偏移 / 易伤两拍 / 回牌库 0.95） |
| `CardBaseDemo.Run` | **433 / 0** · `cardface-chk` **172 / 0** ✅ | +21 条（2D 四个数改真 TMP / 简化视图与 0 值口径） |
| `ShellScene.Run` | **1920 / 0** ✅ | +57 条（`A1520` 六条状态不变量） |
| `MainMenuScene.Run` | **4969 / 0** ✅ | |
| `AnimFXCheck.Run` | **209 / 0** ✅ | |
| `RuleEngineTest.Run` | **8532 / 8534（2 红）** | 两条都断「`Hit{Amount = 4}` 存在」，易伤拆两拍后序列变了 ⇒ 已派 **F2** 诊断 |

**探针**：`check` rc=0（v28 重钉后连跑两次）· `play check` rc=0（1127 + 586 逐字节零差异）。
**钉子**：`PinnedPoolMd5` = **`0397005df1c7de826f56e651c4ca7b01`**（卡表 **v28** = `activeAbilityPickCriteria`；本会话从 v24 → **四连跳 v25→v26→v27→v28**）· `PinnedPlayMd5` = `87209677070c8421b8479e3bcc0effab`。

**本批新落地（要点）**：卡本体位移**三轴旋转**（`SlerpUnclamped`，轴 right/up/forward，连乘 `qX*qY*qZ*startRot`）· 战术卡 `play` 桶 **+0.5**（敌方打出；回放 ÷3）· **易伤两拍**（1.0 + 1.4 = 2.4，第二拍不后坐不震屏）· **trigger 事件带目标**（`TL37` 位移能播）· **`activeAbilityPickCriteria` v28**（可达 0 张，如实标注）· **2D 卡四个数改真 TMP**（原版 `fontSize 0.34` → 我们 `3.4`，因资产 em = fontSize×0.1）· **`SmallScreenUI` 只切费用两档**（判据推翻旧说法）· **远程 0 照印**、只有护甲 `!= 0` 才藏 · `A1520` 六条状态不变量。

---

## ✅ 第四轮（最终）收口读数（2026-10-25 · 全部宿主）

| 宿主 | 最终 | 备注 |
|---|---|---|
| `RuleEngineTest.Run` | **8823 / 8823 ✅** | |
| `BattleScene.Run` | **5292 / 0 ✅** | 删掉自拟 HUD 提示后 |
| `AnimFXCheck.Run` | **209 / 0 ✅** | |
| `CardBaseDemo.Run` | **433 / 0** · `cardface-chk` **207 / 0 ✅** | |
| `CardFaceAssertE5.Run` | **61 / 0 ✅** | 已含 `SoundCueAssertT` 的 11 条（原来无宿主、永远跑不到） |
| `CardFaceProbe.Run` | **207 / 0 ✅** | |
| `MainMenuScene.Run` | **4969 / 0 ✅** | |
| `ShellScene.Run` | **1920 / 0 ✅** | |
| `NetBattleTest.Run` | **239 / 0 ✅** | 修前 233/7（见 `Dg4_NetBattle7红诊断.md`） |
| `NetSelfTest.Run` | **347 / 0 ✅** | |
| `SettingsScene.Run` | **1558 / 0 ✅** | |
| `CardFactionProbe.Run` | **rc=0 ✅** | |
| `ruleprobe check` / `play check` | **rc=0 / rc=0** | 卡池 v28 · 结算指纹 `fc591fb3…` |

**三项收口动作（2026-10-25）**
1. **CJK 字体换源并重烘（界面线 `T07` 那条）**：`TmpSetup.SrcTtf` 改为原版 TTC 的 **face 0**（`Unity/素材/Fonts/NotoSerifCJK-Regular-JP.ttf`，23,624,316 B），跑 `TmpSetup.CopyCjkFont → BuildCjkFontAsset → Verify`（**全部通过**，①②③④⑤⑥⑦⑧⑨）。⚠️ 语料里 **4 个字该字体没有**（`Č š ✅ ️`）⇒ 那些字符会渲成空白（emoji/带音符拉丁；CJK 正文不受影响）。
2. **联机自检 7 红修好（一行）**：`EffectResolver.NoteAbilityWithoutAnimRow` 删掉 `ctx.Log(msg)` —— 进程级静态去重表让**第二个 context** 少一条诊断事件，而 `NetProtocol.Fingerprint` 计 `Events.Count` ⇒ 指纹差 **961 = 31²**。修后 `NetBattleTest 239/0`、`RuleEngineTest 8823/8823`。
3. **`SoundCueAssertT` 接宿主**：`CardFaceAssertE5.Run` 末尾调用（原来**全仓无宿主** ⇒ 11 条断言永远跑不到）。

