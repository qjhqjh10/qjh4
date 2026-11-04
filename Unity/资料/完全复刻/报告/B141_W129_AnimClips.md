# B141-W45 · 11-03 升级动画缺 `2DCard/Level Up Effect` scale 曲线

> 执行写手 `B141-W45` · 2026-11-04 · 会话第一百二十八 · 波次 `W129`（包 `W20`）。
> 改的两个文件：`工具/gen_anim_clips.py` · `Unity/MyGame/Assets/CardPresentation/Resources/Animations/Level Up Card.anim`（生成物）。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改 `d:/2` · ⛔ 未改账/活状态表/所有权表/`CLAUDE.md`。

---

## 0 · 一句话结果

`Level Up Card.anim` 现在有 `2DCard/Level Up Effect` 的 localScale 曲线（**1 → 1.1414**，@**1.433333–1.633333**，0.2 s，两端切线 0）；
根因（生成器硬写 `m_ScaleCurves: []`）已改在生成器里，`--check --only="Level Up Card"` **rc=0 逐字节一致**；其余 3 条 clip **零变化**。

---

## 1 · 判据（原版值，逐条现读）

判据源（两份都读了，数值一致）：
- `d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/Level Up Card.anim`（`m_ScaleCurves` 段）
- `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`（`m_ScaleCurves`）

原版 `Level Up Card` 一共 **2 条** scale 曲线：

| # | path | 键（time → value xyz） | 处置 |
|---|---|---|---|
| 0 | `''`（clip 自己的根） | 0 → 250,250,250 · 1.4333333 → 250 · **1.45 → 288.97699 / 288.98001 / 254.30411** · 1.5833334 → 250（si/so = 72.92988 / 68.22313 / 68.66892） · 1.8333334 → 250 | **不生成**（见 §2 理由） |
| 1 | `2DCard/Level Up Effect` | **1.4333333 → 0.0045454539358615875** · **1.6333333253860474 → 0.005188181065022945**（三轴同值，in/outSlope 全 0） | **生成（本条的活）** |

第 1 条的性质：**×1.141399987**（≈ ×1.1414）、时长 **0.2 s**、两端切线 **0**、共 **2 键**、`m_PreInfinity/m_PostInfinity = 2`（片头前推到首键 ⇒ 静止值 = 首键值）、`m_RotationOrder = 4`。
另注：这两颗 `Image` 的 RectTransform `m_LocalScale = 0.004545454`（= **1/220**），首键值就是它 —— 见 §2 的归一。

时间跨度核对：clip 的总长仍是 **3.4833 s**（float 曲线最晚键），两条事件 1.45 / 3.4833 都在跨度内 ⇒ 新增曲线**没有改变 length、没有改变事件时刻**。

---

## 2 · 🔴 关键判断：绝对值不能照抄，必须按「我们的静止 localScale」归一

**为什么不能照抄 0.004545454 / 0.005188181：**

- 原版那两颗 `Image` 的 `m_LocalScale` = **1/220**，原版节点的 rect 也是 1/220 量纲（`Level Up Effect` rect 616.1×837.97）。
- 我们的 `Shell/CardDisplayEffects.BuildImage` **把这 1/220 折进了节点尺寸**（`:296-298` 注释：「判据里已经是『本层最外层单位』（生成器把 `m_LocalScale` 逐级折进去了）」），
  节点自身**从不写 `localScale`**（全文件只有 `:67` 层根 `localScale = one` 与 `:148` 粒子节点），层根是 `localScale = one`、生产路径 `s = 1`。
- ⇒ 我们这颗 `Level Up Effect` 节点的**静止 localScale = 1**。动画曲线是**绝对写值**，照抄 `0.004545454` 会把它缩到原版的 **0.45%（220 倍）** —— 静默画面事故。

**做法** = 逐分量归一：`产出值 = 原值 × (我们的静止 ÷ 原版静止)`，**`value` / `inSlope` / `outSlope` 同乘**（切线是同一根曲线的导数），`inWeight/outWeight` 不变。
→ 本条的因子 = `1 ÷ 0.004545454 = 220.00003`，产出键值 **`1 → 1.1414`**。

**这不算「自己设计」**：键数 / 时长 0.2 s / 比例 1.1414 / 两端切线 0 / 前后外推方式**全部照原版**，只是换了一把尺 —— 与 builder 那条「单位桥」同源。
已在生成器里写成 `normalize_scale_curve()` 的 docstring + `SCALE_PATHS` 的注释，判据行号写在代码里，供后来人复核。

**为什么根那一条（`path=''`）仍然不生成**：`250` 是「1 卡单位 = 250 画布 px」的**静止量纲**（不是放大倍数），我们的 `Animation` 挂在**主卡 `CardView` 根**
（`Shell/CardDetailPopup.cs:2959` 「宿主 = 主卡（槽 0）的 `CardView` 根 …… ⇒ clip 的 `2DCard/...` 相对这张卡解析」、`:3390` 同论），
`path=''` 会把这**整张卡**放大 250 倍；这条根脉冲**已由代码补间覆盖**（`Effects/CardDisplayLevelUp.cs` 的 `ApplyPulse` / `PulseCurves`，判据与常量都在那段注释里）⇒ 生成器里继续跳过。

**`Rank Up` / `Division Up` 仍不生成**：它们的两条 scale 绑 `Division Image` / `Division Image/Rank Icon`，我们这棵树里没有这两个节点（天生绑不上）；
本条只把 `SCALE_PATHS` 放开了一个 `(clip, path)`，那两条仍走「跳过」分支 ⇒ **产物零变化**（§4 有哈希证据）。

---

## 3 · 生成器改法（`工具/gen_anim_clips.py`）

| 改动 | 内容 |
|---|---|
| 新增常量 `SCALE_PATHS` | `{('Level Up Card','2DCard/Level Up Effect'): 1.0}` —— 值 = **我们这个节点的静止 localScale**；`path=''` 无条件跳过 |
| 新增 `normalize_scale_curve(c, rest)` | 按「静止值」归一（首键 = 原版静止），返回 `(归一后曲线, 三轴 factor)` |
| 新增 `scale_curve_yaml()` / `v3()` | 输出 Unity 的 Vector3 曲线 YAML；项级**没有** `serializedVersion`（那是 `m_FloatCurves` 才有），逐字照本工程 `WarpforgeVFX/Animations/Booster Open Standard.anim` 的格式 |
| `build()` | 遍历 `m_ScaleCurves`：`path=''` 跳过 → 不在白名单跳过 → 否则归一 + 输出；`m_ScaleCurves: []` 改为「有就展开、没有仍写 `[]`」⇒ 其它 clip **逐字节不变** |
| `main()` | 新增可选 `--only=<clip 名>`（只处理一条；名字带空格要引号） |
| 文件头 `:28-40` | **就地收窄**原来那条「`m_ScaleCurves` 有意不生成」：它的理由只对 `path=''` 成立，第二条 path 被误伤了；补上归一规则与两条反例 |
| 用法段 | 补 `--only` 与「全量 `--check` 必 rc=1 的既有原因」（见 §5） |

生成器仍**不跑 Unity、不改 `.cs`**；`--write` 依旧要求显式参数（裸跑 = dry-run）。

---

## 4 · 产物 diff

`git diff --numstat`：`26 1 Unity/MyGame/Assets/CardPresentation/Resources/Animations/Level Up Card.anim`（净 +25 行；520 → 545 行，12562 → 13390 B，CRLF 0 / LF 545 / 裸 CR 0）。
**整份 diff 只有这一块**（`m_PositionCurves: []` 之后、`m_FloatCurves:` 之前），没有任何其它行的改动：

```yaml
  m_ScaleCurves:
  - curve:
      serializedVersion: 2
      m_Curve:
      - serializedVersion: 3
        time: 1.433333
        value: {x: 1, y: 1, z: 1}
        inSlope: {x: 0, y: 0, z: 0}
        outSlope: {x: 0, y: 0, z: 0}
        tangentMode: 0
        weightedMode: 0
        inWeight: {x: 0.3333333, y: 0.3333333, z: 0.3333333}
        outWeight: {x: 0.3333333, y: 0.3333333, z: 0.3333333}
      - serializedVersion: 3
        time: 1.633333
        value: {x: 1.1414, y: 1.1414, z: 1.1414}
        inSlope: {x: 0, y: 0, z: 0}
        outSlope: {x: 0, y: 0, z: 0}
        tangentMode: 0
        weightedMode: 0
        inWeight: {x: 0.3333333, y: 0.3333333, z: 0.3333333}
        outWeight: {x: 0.3333333, y: 0.3333333, z: 0.3333333}
      m_PreInfinity: 2
      m_PostInfinity: 2
      m_RotationOrder: 4
    path: 2DCard/Level Up Effect
```

**结构对照（不是自证）**：把数值全部替换成 `#` 后，我们这一段与 assetripper 原版 `m_ScaleCurves[1]` 的**行数与逐行形状完全一致**（各 26 行，`difflib` 零差异）。

### 其它 clip 是否受影响 —— **是 0**

改前 / 改后 sha256（前 16 位）与字节数：

| 文件 | 改前 | 改后 | 结论 |
|---|---|---|---|
| `Level Up Card.anim` | `8525885007d00668` · 12562 B | `c97d9251cedfbca3` · 13390 B | 本条的产物 |
| `Rank Up.anim` | `7408647e46dcb938` · 9671 B | **同左** | 未动 |
| `Division Up.anim` | `743dade06f6c9f5f` · 11152 B | **同左** | 未动 |
| `Booster Window - Background Shake On Open.anim` | `c599280b80ea8f79` · 3483 B | **同左** | 未动 |

重生成只跑了 `--write --only="Level Up Card"`（不是全量 `--write`）⇒ 另外三条**连写盘都没发生**。

---

## 5 · 验证（逐条，可复跑）

```bash
python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --check --only="Level Up Card"   # rc=0 ✅ 逐字节一致
python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --check --only="Rank Up"          # rc=0 ✅（未动）
python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --check --only="Division Up"      # rc=0 ✅（未动）
python -I -X utf8 d:/4/Unity/工具/gen_anim_clips.py --check                            # rc=1（见下，**改动前就是 1**）
```

🔴 **全量 `--check` rc=1 是既有现象、与本次改动无关**：`Booster Window - Background Shake On Open.anim` 盘上那份是 **Unity 重新序列化过**的版本
（`inWeight: 0.33333334` / `m_Events: []` / 原版 `script` PPtr），生成器产出的是旧格式 —— 本次改动在动手**之前**跑基线 `--check` 就已经是 rc=1。
该文件不在本条白名单内，**没有动它**（要收口得单独判断：全量 `--write` 会把 Unity 的重序列化版本覆盖回旧格式）。

其它证据：
- 生成器自带的曲线报告：`2DCard/Level Up Effect` 的 `localScale` 现在出现在列表首位（`classID=4`，「已按我们的静止值 1 归一」）。
- 事件/时长自检：`length(曲线最晚键) = 3.4833333 s · 最晚事件 = 3.4833333 s` ⇒ **未变**。
- 行尾：两个文件都是 **纯 LF**（anim CRLF 0 · py CRLF 0 · 裸 CR 0）。
- **C# 自检：零条**（纯工具 + 纯资产，不用 typecheck、不用跑宿主）。

---

## 6 · 没查清 / 需要上位判断的

1. 🔴 **归一这个决定本身**：我按「节点静止 localScale」归一（产出 `1 → 1.1414`），理由是 builder 把 1/220 折进尺寸了（§2，有代码行号）。
   如果将来有人把那颗节点改回「原版做法」（节点自身 scale = 1/220、尺寸不折），**这条曲线要同步改回绝对值**（`SCALE_PATHS` 的 `rest` 改成 `0.004545454` 即可）。
   ⚠️ 反过来，**照抄绝对值的版本一定不能上线**（会把光效缩到 0.45%）。
2. **运行时未验（本波禁 Unity）**：路径 `2DCard/Level Up Effect` 能不能绑上，我只能给静态证据 —— ① 我们的 `2DCard` 是 `CardView` 建的子件（`Core/CardView.cs:4273`）、`Level Up Effect` 由 `Shell/CardDisplayEffects` **按原版 `parent` 字段建在 `2DCard` 下**（`Shell/CardDisplayEffects.cs:72-80`）；② `Animation` 宿主 = **主卡 `CardView` 根**（`Shell/CardDetailPopup.cs:2959`/`:3389`）；③ 同一 clip 的 `floatProperty1.value` 早就绑在这条 path 上。**实机是否可见（1.4333 s 起放大 14%）未测。**
3. **浮点精度**：生成器统一 `%.7g` ⇒ `time: 1.433333`（原版 `1.4333333`，差 3e-7 s）。这是本文件所有曲线一贯的写法（同文件既有 float 曲线也是 `1.833333`），**不是本轮新引入**；要提精度得整份统一改，那会动到另外 3 条 clip ⇒ 没动。
4. 📌 **顺手发现（没改）**：生成器对**没有事件**的 clip 写的是 `m_Events:` + 下一行 `  []`，而 Unity 自己序列化成 `m_Events: []`（见 Booster 那份的 diff）。
   现在只有 Booster 一条无事件（且它本来就不一致）⇒ 没动；将来若要让 `--write` 产出 Unity 风格，这一处要一起改。
5. 📌 **顺手发现（口径过期，未改账）**：账/报告里有「`Animation` 挂**窗根**」的说法（`账/_parts/_merged_B9_W37CLIP/B9_W37CLIP.tsv` 第 5 行等，`B9_W25UPGR` 时期），
   现读代码是**主卡 `CardView` 根**（`Shell/CardDetailPopup.cs:2959`/`:3389`，后者注释直接写「宿主 = 主卡根」）。这条对路径解析是决定性的（挂窗根时 `2DCard/...` 一条都不命中），
   **建议调度台按铁律 5 就地订正账里那两句**（本代理不改账）。
6. ⚠️ **`Effects/CardDisplayLevelUp.cs:457` 仍写「我们的 `Animation` 挂在**窗根**（`Shell/CardDetailPopup.cs:3298`）」**，与现读代码不符（真值是 `:2959`/`:3389`，且是**主卡根**）。
   本条的归一论证不依赖它（依赖的是 `CardDisplayEffects` 的尺寸折算），但它是同一族的过期口径，**建议调度台一并订正**（该文件不在本条白名单）。

---

## 7 · 出处（复核用）

- 原版判据：`d:/2/tools/assetripper_out/ExportedProject/Assets/AnimationClip/Level Up Card.anim`（`m_ScaleCurves` 第 2 项）
  · `d:/2/新解包资源/assets_full/bundle_scenes_scenes_mainmenuwarpforge/AnimationClip/AnimationClip_10.json`（`m_ScaleCurves[1]`）
- 单位桥：`CardPresentation/Shell/CardDisplayEffects.cs:57-67`（层根 `localScale = one`、生产 `s = 1`）· `:286-300`（`BuildImage` 只写位置/尺寸，尺寸已折 `m_LocalScale`）· `:148`（只有粒子节点写 `localScale`）
- 绑定前提：`CardPresentation/Core/CardView.cs:4273`（`2DCard`）· `CardPresentation/Shell/CardDisplayEffects.cs:72-80`（`Level Up Effect` 建在 `2DCard` 下）
- `Animation` 宿主：`CardPresentation/Shell/CardDetailPopup.cs:2959`、`:3389-3390`
- 根脉冲（代码侧等价物）：`CardPresentation/Effects/CardDisplayLevelUp.cs` 的 `ApplyPulse` / `PulseCurves`（同段注释给了原版 5 键）
- 生成器：`工具/gen_anim_clips.py` 的 `SCALE_PATHS` / `normalize_scale_curve` / `scale_curve_yaml` / `build` / `main`
