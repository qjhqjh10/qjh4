# B105 · 46 条 legacy `.anim` 的 `m_StopTime=1` —— 读数判据（探针已备、**未跑**）

> 代理 `B105_R2`（只读为主）· 2026-10-31 · 上游 = `判据/B104_anim曲线对账.tsv:5`（X2 项）·
> `判据/B104_A1_施工单.md:33`（X2 那一行）· `工具/gen_anim_clips.py:13-22,187`。
> ⛔ 本文件**没有任何 Unity 读数**：`clip.length` 那一列必须由探针回填后才算数。

---

## 〇 结论摘要

| # | 命题 | 状态 | 证据 |
|---|---|---|---|
| 1 | 47 条 `.anim` 的 **476 条曲线逐 key 0 差异** | 已证（B104） | `判据/B104_anim曲线对账.tsv:2` |
| 2 | 我们的 46 条 legacy clip **都写着 `m_AnimationClipSettings.m_StopTime: 1`** | 已证（现读） | 52 个 `.anim` 里 **51 条** `m_StopTime: 1`（余 1 条 `Card Explosion` = 0.81666，且 `m_Legacy: 0`） |
| 3 | **原版 bundle typetree 里 `m_AnimationClipSettings` 整个是 null**（46/46） | 已证（B104 + 本代理复扫 49/49） | 复扫脚本见 §五 |
| 4 | **原版 AssetRipper 导出（Unity 自己的写出器）里 66/66 条 legacy clip 也是 `m_StopTime: 1`** | 已证（本代理现读） | `D:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/*.anim`：legacy 66 条，`m_StopTime` 直方图 `{1: 66}`；含曲线到 **7.05 s** 的 `Godspear Warhead.anim:214` |
| 5 | 这些 clip 的**有效时长（末键）= 0.25 – 100.2 s** | 已证（本代理静态复算 + 与原版 JSON 抽样对账 5/5 吻合） | §一 表格 |
| 6 | **`clip.length` 到底取「末键」还是「StopTime」** | 🔴 **未判 —— 只差一次探针** | §二 |

**第 4 条是本次新查出来、之前没有记过的一条**：`m_StopTime: 1` **不是我们编的**，而是 Unity 自己
给 legacy clip 写出的默认值（AssetRipper 导出的原件也是 1）。所以「原版没有该字段」这句话**只对
bundle typetree JSON 成立**（那里 null 表示「字段缺省」），**不对 Unity 的序列化形态成立**。
⇒ 我们的生成器 `gen_anim_clips.py:187` 写 `1` 是**照 Unity 的默认形态照抄**，不是拍脑袋。

**已知旁证（不是判据，⛔ 不能代替探针）**：`B9_W37CLIP` 2026-10-27 记录 —— `m_StopTime` 在
`UnityEngine.CoreModule.dll` 里 **0 命中**、只在 `UnityEditor.CoreModule.dll` 里出现（`AnimationClipSettings` 7 处）；
原版**依赖** >1 s 的事件才能跑（`RankedEventDivisionChangeWindow__AnimationFinished` 3.9167 s、
`CardDisplayLevelUpAnimationController__AnimationFinished` 3.4833 s、`RemnantNecrons__AnimationResurrectionEnd` 1.8 s、
`SwarmVFXController__OnAnimationEndEvent` 1.2 s）⇒ 若真被截到 1 s，原版这四条事件**一次都不会响**。
这与 §四 的「结论偏 OK」一致，但**仍需探针实测**（铁律：判「有没有」靠读数不靠推断）。

---

## 一 清单

### 1·1 这 46 条 = 哪 46 条（集合怎么定的，可复现）

B104 的 tsv 只写「47 条合计」，没列名字。本代理用**曲线总数反推**把集合钉死：

- 现读 `Assets/WarpforgeVFX/Animations/*.anim` 共 **52** 个；
- 排除 3 个 10-27 新增（`Division Up` 11 曲线 / `Level Up Card` 10 / `Rank Up` 10）后 = **501 曲线**（B104 tsv 的 EXCL3 口径）；
- 再排除 **`Booster Open Sororitas`（12）** 与 **`Booster Open Standard`（13）** 后 = **476 曲线** ✅ 与 tsv 的「476 条（200/4/23/19/229/0/0/1）」**逐族全等**；
- 为什么排除这两条：它们的 `.anim` **和 `.meta` 的 mtime 都是 2026-10-31 01:08:2x–01:08:33**，而 B104 那份 tsv 写于 **01:02** ⇒ **它们是在对账之后才落盘的**（这不是猜：同目录其余 47 个的 `.meta` 都是 10-03/10-08/10-09）；
- `Card Explosion`（`m_Legacy: 0`、0 条 `m_*Curves`）也在那 47 里，但它**不是 legacy** ⇒ 46 = 47 − 1。

⇒ **46 条 = 上表这 46 个**；另**现在还有 2 条同类**（`Booster Open Sororitas` / `Booster Open Standard`，
同属 legacy + `StopTime: 1`），它们**不在 B104 的账里**，但**同样要读**（探针会一起读）。

### 1·2 46 条（按末键升序；`末键` = 所有曲线键里最晚的 `time`，已与原版 JSON 抽样对账 5/5 吻合）

| # | clip 名 | 末键 (s) | 曲线数 |
|---|---|---|---|
| 1 | `Minion Death Icon` | 0.250000 | 3 |
| 2 | `OpenCardbacks` | 0.250000 | 6 |
| 3 | `SwarmIconOn` | 0.483333 | 3 |
| 4 | `Thunderstorm Flash` | 0.516667 | 2 |
| 5 | `Booster Window Close` | 0.566667 | 35 |
| 6 | `Booster Window Open` | 0.616667 | 36 |
| 7 | `Booster Opening - Card Open Normal` | 0.666667 | 4 |
| 8 | `Pray Enter` | 0.666667 | 1 |
| 9 | `Pray Exit` | 0.750000 | 5 |
| 10 | `Booster Window - Background Shake On Open` | 0.916667 | 2 |
| 11 | `Card Hand To Board` | 0.916667 | 17 |
| 12 | `Choppa Animation` | 1.000000 | 1 |
| 13 | `Booster Opening - Card Open Rare` | 1.166667 | 4 |
| 14 | `Swarm Triat Merge` | 1.200000 | 10 |
| 15 | `Axe Swing` | 1.333333 | 2 |
| 16 | `Booster Opening - Card Open Legendary` | 1.500000 | 4 |
| 17 | `ScytheAssault` | 1.500000 | 5 |
| 18 | `Stormsurge Cannon` | 1.716667 | 2 |
| 19 | `Vanguard Frame Animation` | 1.791667 | 7 |
| 20 | `InBattleDamageCounter Variation 1` | 1.833333 | 10 |
| 21 | `From Remnant Necrons` | 1.983333 | 21 |
| 22 | `SweepBackAndForth` | 2.000000 | 1 |
| 23 | `SweepBackAndForth_Adjacents` | 2.000000 | 1 |
| 24 | `SweepLeftToRight` | 2.000000 | 1 |
| 25 | `Blizzard` | 3.000000 | 3 |
| 26 | `Solar Pulse` | 3.000000 | 1 |
| 27 | `Particle Whip` | 3.700000 | 2 |
| 28 | `Will of Grok` | 3.700000 | 5 |
| 29 | `Resurrection Orb` | 3.916667 | 3 |
| 30 | `To Remnant Necrons` | 3.983333 | 24 |
| 31 | `Emperor's Children Environmental Condition 1` | 4.000000 | 2 |
| 32 | `Planetary Invasion Animation` | 4.000000 | 1 |
| 33 | `Sump Overspill` | 4.000000 | 1 |
| 34 | `SweepBackAndForth_loop` | 4.000000 | 1 |
| 35 | `To Remnant Aeldari` | 4.516667 | 8 |
| 36 | `Helfire` | 5.000000 | 1 |
| 37 | `Valkyire Drop Movement` | 5.000000 | 3 |
| 38 | `Hive Fleet Arrival PP` | 6.000000 | 11 |
| 39 | `HiveFleetTendrils` | 6.360000 | 76 |
| 40 | `Scene` | 6.360000 | 134 |
| 41 | `Solar Eclipse` | 6.700000 | 3 |
| 42 | `Godspear Warhead` | 7.050000 | 4 |
| 43 | `Booster Opening - Card Idle` | 8.000000 | 3 |
| 44 | `Spirit Stone Idle` | 8.000000 | 2 |
| 45 | `Firestorm Tornado` | 12.500000 | 1 |
| 46 | `Fleet Support Barges` | 100.200000 | 4 |

⚠️ **全部 46 条**：`路徑 = Assets/WarpforgeVFX/Animations/<clip>.anim` · 我们 `m_StopTime = 1` ·
原版 bundle typetree `m_AnimationClipSettings = null` · 原版 AssetRipper 导出 `m_StopTime = 1`。

**判读时注意「没有区分力」的行**：`Choppa Animation` 的末键恰好 **1.000000** ⇒ 「按 StopTime 截断」与
「按末键取长」**两种假设给同一个数**，它**不能用来判**（照实写「不具区分力」）。
其余 45 条末键都离 1 秒 > 1e-3，**每一条都能单独判死**（最狠的是 `Fleet Support Barges` 100.2 s：差 100 倍）。

### 1·3 另外 2 条同类（不在 B104 账里，探针一并读）

| clip 名 | 路径 | 我们 StopTime | 原版 typetree | 原版导出 | 末键 (s) | 曲线数 | 备注 |
|---|---|---|---|---|---|---|---|
| `Booster Open Sororitas` | `Assets/WarpforgeVFX/Animations/Booster Open Sororitas.anim` | 1 | null | 1 | 2.000000 | 12 | 2026-10-31 01:08 落盘（B104 之后） |
| `Booster Open Standard` | `Assets/WarpforgeVFX/Animations/Booster Open Standard.anim` | 1 | null | 1 | 2.366667 | 13 | 同上 |

另 3 条 10-27 新增（`Division Up` 3.833333 / `Level Up Card` 3.483333 / `Rank Up` 3.916667）
**在 `Assets/CardPresentation/Resources/Animations/` 也各有一份同名副本**（`WarpforgeVFX/Animations/` 里的旧副本仍在）——
它们不走 B104 的账，但**也带 `m_StopTime: 1`**，探针会一起读（`Resources/` 那 3 份是运行期真正被 `Resources.Load` 取到的）。

---

## 二 探针

### 2·1 文件与入口

- 新增文件（本代理已写、`typecheck 0/0`）：`MyGame/Assets/CardPresentation/Editor/B105_ClipLengthProbe.cs`
- 类名 == 文件名、全局命名空间（本工程硬要求；否则存盘会烘成 `!u!115` 内联存根，player 加载即崩）
- 入口：**`B105ClipLengthProbe.Run`**（`public static void Run()`）
- **只读**：不写任何 `.anim`、不 SaveScene、不改 `.cs`；唯一写盘 = `D:/4/Unity/_tmp_view/b105_clip_length.tsv`

### 2·2 跑法（Unity 批处理 —— 只有调度台能跑，⛔ 子代理不跑）

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" \
  -executeMethod B105ClipLengthProbe.Run -logFile -
# 筛输出：grep "^B105 "
# 结果表：D:/4/Unity/_tmp_view/b105_clip_length.tsv
```

### 2·3 它逐条读什么

| 列 | 来源 | 说明 |
|---|---|---|
| `assetPath` / `clipName` | `AssetDatabase` | 扫描 `Assets/WarpforgeVFX/Animations` + `Assets/CardPresentation/Resources/Animations` |
| **`length`** | **`AnimationClip.length`** | 🔴 **本探针的核心读数**（Unity 实际认为的时长） |
| `lastKey` | 遍历曲线键取最大 `time` | `AnimationUtility.GetCurveBindings` + `GetEditorCurve`；**拿不到曲线时退回解析 `.anim` 文本**（`curveSource` 列写 `api` / `yaml` / `none`） |
| `delta_len_minus_lastKey` | `length − lastKey` | 一眼看截没截 |
| `curveCount` / `curveSource` | 同上 | `curveCount=0` ⇒ 那条不判（muscle/空壳） |
| `frameRate` / `legacy` / `wrapMode` | `AnimationClip` | 排除「sampleRate 差异导致取整」这类干扰 |
| `settings_startTime` / **`settings_stopTime`** / `settings_loopTime` | `AnimationUtility.GetAnimationClipSettings(clip)` | 编辑器侧看到的 StopTime（预期 = 1） |
| `yaml_stopTime` | 直接读 `.anim` 文本的 `m_StopTime` | 与 `settings_stopTime` 交叉核对（不一致 ⇒ 资产没被重导入） |
| **`verdict`** | 见 §三 | `OK_LASTKEY` / `TRUNCATED` / `MISMATCH` / `NO_CURVES` |

汇总行（日志尾部）：`B105 合计 读=N OK_LASTKEY=a TRUNCATED=b MISMATCH=c NO_CURVES=d`。

⚠️ **它可能遇到的 API 口径**：legacy clip 的曲线**不一定**能从 `GetCurveBindings` 取到（若取不到，
`curveSource=yaml`、`curveCount` 来自文本解析 —— 那仍然是对的，因为 `.anim` 就是工程里的真身）。
**只要 `length` 是 Unity 读的，本件的判据就成立**。

---

## 三 判定规则

容差 **`Eps = 1e-3` 秒**（sampleRate 25/60 的最短帧 = 0.04 / 0.0167 s，1e-3 足以区分「末键」和「StopTime」，又不会被浮点噪声骗）。

| verdict | 条件 | 含义 | 该做什么 |
|---|---|---|---|
| `OK_LASTKEY` | `|length − lastKey| ≤ 1e-3` | **Unity 忽略 StopTime，length = 末键** | **不改生成器**；只订正台账（B104 tsv 第 5 行的「若 Unity 按 StopTime 取长则…」改成已判）|
| `TRUNCATED` | `|length − stopTime| ≤ 1e-3` **且** `stopTime < lastKey − 1e-3` | **Unity 确实按 StopTime 截断** | **必须改生成器 + 改写现有 46+ 条**（§四 分支 B） |
| `MISMATCH` | 两者都不等 | 第三种口径（可能是 sampleRate 取整 / 曲线被裁剪） | 人看：拿该条的放大读数回原版 JSON 复核，再定 |
| `NO_CURVES` | `curveCount == 0` | muscle clip / 空壳（`Card Explosion`） | 不适用本件（另走 `WarpforgeAnimatorBridge` 那条） |

**怎么读这一批**（建议顺序）：
1. 先看 **`Fleet Support Barges`**（末键 100.2 s）：`length ≈ 100.2` ⇒ 直接定案 `OK_LASTKEY`；`length = 1` ⇒ 直接定案 `TRUNCATED`。
2. 再看 `Firestorm Tornado`（12.5）/ `Godspear Warhead`（7.05）/ `Scene`（6.36）—— 三条大跨度独立复核。
3. 最后统计 46 条里两类各多少。**只要有一条 `TRUNCATED`，整批就必须修**（因为机制是全局的）；
   **只要有一条 `OK_LASTKEY` 且末键远大于 1，就已经证明机制是「忽略 StopTime」**（同一引擎不可能对这一条例外）。
4. `Choppa Animation` 那一行照实写「不具区分力」。

---

## 四 `工具/gen_anim_clips.py` 的 patch（**按探针结果二选一；本代理未改任何文件**）

### 分支 A —— 探针判 `OK_LASTKEY`（预期；`B9_W37CLIP` 的旁证指向这一支）

**代码：一个字都不改。**

只订正文档/台账（两处，都不是代码逻辑）：

- `工具/gen_anim_clips.py:13` —— 现在写「`m_StopTime: 1`（原版也是 1，⛔ 别改）」。这句话**是对的**，
  但容易让人误读成「原版 bundle 里有这个字段」。建议就地补一句（**同一条注释行内改写、⛔ 别新增行**）：
  ```
  **`m_StopTime: 1`**（原版 bundle typetree 里 `m_AnimationClipSettings` 是 null；但 AssetRipper 导出的 66/66 条 legacy 全是 1，且 2026-10-31 `B105` 探针实测 clip.length = 末键 ⇒ 别改）· ...
  ```
- `资料/完全复刻/判据/B104_anim曲线对账.tsv:5` 与 `B104_A1_施工单.md:33`（X2 行）：把「若 Unity 按 StopTime 取长则 46 条被截到 1 s（未跑 Unity，判不了）」改成
  「**2026-10-31 `B105` 探针实测 `clip.length` = 末键，Unity 不按 StopTime 取长**」并补探针产物路径。
  ⚠️ `账/*` 与两张正本**归调度台写**，本代理只给文字。

### 分支 B —— 探针判 `TRUNCATED`（要真改）

**(B-1) 生成器：`工具/gen_anim_clips.py:187`**

原文：
```python
        '    m_StopTime: 1',            # 原版 66/66 条 legacy clip 都是 1；**不是 length**（length = 曲线最晚键），⛔ 别改
```
改成（`max_curve_time(d)` 在本文件 `:211-223` 已有，`build()` 里 `d` 就是该 clip 的源 JSON，可直接调用）：
```python
        '    m_StopTime: ' + f(max_curve_time(d)),   # B105 探针实测 StopTime 会截长度 ⇒ 写成曲线末键（0 曲线时 = 0，届时改手写兜底）
```
⚠️ 注意：`max_curve_time` 对**没有任何曲线**的 clip 返回 `0.0` ⇒ 那样会写出 `m_StopTime: 0`（= 0 长的 clip）。
本脚本现管 4 条（`Rank Up` / `Division Up` / `Level Up Card` / `Booster Window - Background Shake On Open`）
**都有曲线**，所以眼下不会触发；但要把兜底写进注释：`max_curve_time(d) or 1`（退化时保守取 1）。

**(B-2) 现有 46 + 2 条 `.anim` 本身**（⚠️ 它们**不是这个脚本生成的** —— 全仓只有 `gen_anim_clips.py`
会写 `.anim` 文本，这 48 条是历史产物，没有现成生成器能重跑）⇒ 只能就地改那一行。
**改法（二进制读写，⛔ 别用文本模式 / `sed -i`，会把 CRLF 翻成 LF）**：

```python
# 一次只改这一行：'    m_StopTime: 1'  ->  '    m_StopTime: <该条末键>'
import io, re
p = r'D:/4/Unity/MyGame/Assets/WarpforgeVFX/Animations/<clip>.anim'
b = io.open(p, 'rb').read()
new = re.sub(rb'(?m)^    m_StopTime: [0-9.eE+-]+$',
             ('    m_StopTime: %s' % ('%.7g' % LAST_KEY)).encode('ascii'), b, count=1)
assert new != b, '没改动：m_StopTime 那行没匹配上'
io.open(p, 'wb').write(new)   # 先把 new 算好再写（⛔ 别写成 io.open(p,'wb').write(<表达式>)）
```
改完必须：① `b.count(b'\r\n')` 与改前一致（行尾没翻）② 让 Unity 重新导入（打开工程 / 批处理即可）
③ 再跑一次 `B105ClipLengthProbe.Run`，应全部 `OK_LASTKEY`（这就是本件的**灭自证**验收：两边一起改回旧值不会变绿）。
**⛔ 本代理没有改这些 `.anim`（红线）。**

**(B-3) 别忘的地方**：`Assets/CardPresentation/Resources/Animations/` 那 3 条同名副本（`Division Up` / `Level Up Card` /
`Rank Up`）—— 运行期 `Resources.Load` 取的是**它们**，`WarpforgeVFX/Animations/` 里那 3 份是**旧副本**。
真修的时候两处都要改（或删掉旧副本，别留两份不一致）。

---

## 五 还差什么（本代理没做 / 做不了的）

1. 🔴 **跑一次探针**（Unity 批处理，只有调度台能跑）—— 这是本件唯一未完成项，命令见 §二。
2. **`clip.length` 的 46 个读数回填本表**：跑完把 `_tmp_view/b105_clip_length.tsv` 的 `length` 列并进 §一 表格，
   并把 §〇 第 6 条改成已判。**⛔ 在回填之前，任何「StopTime 不影响 length」的结论都只是旁证，不是判据。**
3. 台账订正（`B104_anim曲线对账.tsv:5` · `B104_A1_施工单.md:33` · `任务线/特效线/任务总表.md` 的 W65/X2 行）——
   按分支 A/B 写结论；本代理只给文字，**账归调度台**。
4. **`Booster Open Sororitas` / `Booster Open Standard` 这两条要不要补进 B104 的账**：它们是 01:08 新增的，
   B104 的 476 曲线口径里**没有**它们。是否补一次「逐曲线对账」由调度台定（本代理只报事实）。
5. **B104 tsv 里「原版 legacy 无该字段」这句表述偏窄**（见 §〇 第 4 条）：只对 bundle typetree 成立。
   tsv 与施工单里的同一句话要一并订正（铁律 5：同一句话别留两处打架）。

### 复现本文件里静态数字的命令（只读）

```bash
# 曲线总数反推（确认 46 条集合）：临时脚本读 .anim 文本，按 m_*Curves 段计数
# → WarpforgeVFX/Animations 全量 532；减 3 条新增（31）= 501；减 Booster Open 两条（12+13）= 476 ✅
# 原版 typetree 的 m_AnimationClipSettings=null：扫 d:/2/新解包资源/assets_full/*/AnimationClip/*.json 的 m_Name
# 原版 AssetRipper 导出的 StopTime：扫 d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/*.anim
#   的 m_Legacy / m_StopTime（legacy 66/66 = 1）
```
