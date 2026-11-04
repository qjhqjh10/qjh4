# B141-W46 · W129 · 11-04 战斗 HUD 8 条 clip 的切线（斜率）恒为 0

> 执行写手 `W46` · 2026-11-04 · 批次 `B141` · 会话第一百二十八 · 域 = §11 动画「11-04」。
> 判据来源：`判据/B141_W129_对账_UI动画.md` W18 包 + 11-04 行 · `报告/B138_审计汇总_问题清单.md` §11 的 11-04（B135 动画 #51）。
> 结论：**已修**（生成器键名 `is`/`os` → `si`/`so`，重生成两份产物）。数值不变（今日斜率原版全 0），修的是**绑定**。

---

## 一、问题（原版值 vs 我们值）

| 项 | 原版 | 我们（修前） |
|---|---|---|
| 8 条 `BattleHud*` clip 的逐键 `inSlope` / `outSlope` | 8 条全 legacy，**119 条曲线 / 170 个键**，逐键都带 `inSlope`/`outSlope`（**今日数值恰好全 0**，`weightedMode=0` ⇒ 插值是 smoothstep） | 生成器把斜率写进 JSON 的 **`is`/`os`** 键，而运行期 `Battle/BattleHudAnim.cs:60` 的字段名是 **`si`/`so`** |
| 运行期读到的斜率 | 逐键写回 `new Keyframe(t, v, inTangent, outTangent)` | `JsonUtility` **按字段名绑定** ⇒ `si`/`so` **恒 null ⇒ 恒 0**（哑弹）；`BattleHudAnim.cs:318-319` 的 `k.si != null && …` 兜底成 0 |

**为什么今天看不出坏**：8 条 clip 的斜率现值**全是 0**（生成器 `self_check` 断言：`gen_battlehud_clips.py:586` `assert all(abs(x) < 1e-9 for x in k["si"] + k["so"])`），
所以「读到 0」与「绑不上而当 0」**结果相同**。⇒ 这是**静默**缺陷：一旦原版哪条键非零（或我们以后按正确 read 值重建），**滑出/滑入的形状会被丢掉**，且不报错、断言也抓不到（见 §五-3）。

**判据链**：
- 原版曲线：`d:/2/新解包资源/assets_full/bundle_battlesharedresources_assets_all/AnimationClip/*.json` 的 `m_Curve[].inSlope/outSlope`（13 场同一套，`clipPids` 逐一相同）。
- 我们的 `si`/`so` 字段：`MyGame/Assets/CardPresentation/Battle/BattleHudAnim.cs:60` `public class BattleHudKey { public float t; public float[] v; public float[] si; public float[] so; }`。
- 同仓**同一套键名的先例**：`camera_intro_clips.json` / `arena_scene_clips.json` 都用 `si`/`so`，消费端 `Battle/CameraIntro.cs:485-486` 读 `k.si` / `k.so` 且工作正常 ⇒ 惯例本来就是 `si`/`so`，`battlehud_clips.json` 是**唯一的例外**（`gen_arena_scene_clips.py:348` 的注释自陈过这件事）。

---

## 二、改了什么

### 1. `工具/gen_battlehud_clips.py`（唯一逻辑改动，+16/−7 行）

| 行（现读） | 改前 | 改后 |
|---|---|---|
| `:182-183` | `"is": _vec(k["inSlope"], dim)` / `"os": _vec(k["outSlope"], dim)` | `"si": …` / `"so": …`（并加 `# ✅ 已复刻（2026-11-04 · B141-W46 · …）` 标记） |
| `:586` | `assert all(abs(x) < 1e-9 for x in k["is"] + k["os"])` | `… k["si"] + k["so"]` |
| 文件头 `:37-39` / schema 样例 `:56`、`:83` / `:86` 的散文 | 写 `is`/`os` | 写 `si`/`so`，并在 `:41-46` 补一段 **2026-11-04 键名订正的来龙去脉**（为什么改、为什么今天数值差为 0、⛔ 别改回去） |

> 改**生成器**而不是手改 JSON：手改会被下一次 `--write` 覆盖（W18 包判据明写）。
> 消费端（`BattleHudAnim.cs`）**逻辑零改动** —— 它本来就按 `si`/`so` 读，是生成器写错了名字。

### 2. 两份产物（`--write` 重生成）

- `d:/4/Unity/数据/游戏数据/battlehud_clips.json`（全量件）：**83,626 B → 83,626 B**（键名等长，字节数不变）
- `d:/4/Unity/MyGame/Assets/CardPresentation/Resources/battlehud_clips.json`（运行期副本）：**55,723 B → 55,723 B**

`git diff --numstat`：
```
341  341  Unity/MyGame/Assets/CardPresentation/Resources/battlehud_clips.json
  16    7  Unity/工具/gen_battlehud_clips.py
 354  354  Unity/数据/游戏数据/battlehud_clips.json
   1    1  Unity/MyGame/Assets/CardPresentation/Battle/BattleHudAnim.cs
```
两个 JSON 的行数变化为 **0/0**（只是同样的行里 `"is":`→`"si":`、`"os":`→`"so":`），行尾仍是 **纯 LF**（`CRLF=0 · bareCR=0`）。

### 3. `Battle/BattleHudAnim.cs`（仅注释，零行数变化）

`:310` 的 `/// <summary>` 那**一行就地改写**（不加行），挂上
`✅ 已复刻（2026-11-04 · B141-W46 · 判据 = gen_battlehud_clips.py 键名 si/so ↔ 本类 BattleHudKey.si/.so；⛔ 别把 JSON 键改回 is/os…）`。
行数 **624 → 624**（`git diff --numstat` = `1 1`），行尾 LF 未翻。

---

## 三、验证读数

| 验证 | 命令 | 结果 |
|---|---|---|
| 基线（改前） | `python -I -X utf8 工具/gen_battlehud_clips.py --check` | **rc=0**，两份产物与现生成一致（hash `9a4e484c3dba…`） |
| 改后未重生成 | 同上 | **rc=1**，两份产物都报「不一致」⇒ 证明改动确实进了生成结果（不是空转） |
| 重生成 | `--write`（显式动作） | 写出两份，`8 条 · 曲线数 {16,16,16,14,14,14,14,15}`（= **119 条**）· 长度集合 `[0.0, 1.0]` |
| 幂等复跑 | `--check` | **rc=0** ✅「两份产物都与现生成一致」（hash `ae1b8380c3b9…`） |
| 裸跑安全 | 不带参 | 只打印用法、零读零写（`gen_battlehud_clips.py:730-732`，账 `N55`）—— 本次**未裸跑** |
| 内容等价性（独立脚本比对 HEAD 版 vs 现版，逐层深比） | 把旧件里的 `is`/`os` 归一成 `si`/`so` 后与现件**全等** | ✅ 两份都「仅 `clipsHash` 一处不同」，其余**逐字节等值** |
| 斜率非零计数 | 同上脚本 | 运行期副本 **170 个键**：`si` 170 / `so` 170 / **`nonzero_si = 0` / `nonzero_so = 0`** ⇒ **数值差为 0**，与原版（今日全 0）一致 |
| 字段绑定 | `BattleHudAnim.cs:60` 字段 = `si`/`so`；JSON 键 = `si`/`so` | **同名 ⇒ JsonUtility 能绑上**（修前是不存在的键 `is`/`os`） |
| 类型检查（动了 `.cs`） | `TMPDIR=/tmp/wf_b141_w46 bash 工具/typecheck.sh` | **运行时错误 0 / 编辑器错误 0** |

**C# 自检**：本次只改注释 + 数据键名，**未跑 Unity 宿主**（红线）。受影响的自检是 `Editor/BattleScene.cs:28131-28146` 那 4 条（8 条 clip 表 / 时长 1.000 s 与 0.000 s / rig 搬家 / 端点 delta）——
它们断的是**时长、曲线数、位置**，本次三者**一个数没变**（见上表「内容等价性」），预期全绿；**建议下个同步点顺带跑一次 `BattleScene.Run` 复核**。

---

## 四、副作用（须知）

- **`clipsHash` 变了**：`9a4e484c3dba918b4c513da4e55fe125` → **`ae1b8380c3b9bd2543939a3c23868e0b`**。
  原因：`clips_hash()`（`gen_battlehud_clips.py:284-286`）算的是**我们这份 clips 载荷**的 md5，键名换了名字就跟着变。
  语义上它是「13 场共用同一套」的**自证指纹**（`:591` `assert hashes == {doc["clipsHash"]}` 仍成立），**不是**原版数据的指纹 ⇒ 不影响正确性。
  ⚠️ 但它让两份**历史报告里记的旧 hash 过期**（见 §五）。

---

## 五、❓ / 顺手发现（本代理白名单外，未改，交调度台）

1. **`工具/gen_arena_scene_clips.py:348` 的注释现在过期**：
   原文「⚠️ 键名用 si/so（与 camera_intro_clips.json 一致；**battlehud_clips.json 用的是 is/os，同义**）。」
   ⇒ 后半句已不成立（本次起 battlehud 也是 `si`/`so`）。建议把那半句删掉或改成「三份现已统一为 si/so」。
2. **历史报告里的旧 hash（铁律 5 建议就地加订正注）**：
   - `资料/完全复刻/报告/B120_C_动画与时间轴.md:67`（`hash 9a4e484c3dba`）
   - `资料/普查产出_1022/E-C_BattleHud8条.md:7`（`clipsHash 9a4e484c3dba918b4c513da4e55fe125` 与全量件 md5 `39d8f7cf…`）、`:97-106`（逐场 `9a4e484c…` 列）
   两份都是**当时**的读数（历史留痕可保留），但为避免下个会话把它当现状 ⇒ 建议标注「hash 已于 2026-11-04 键名订正后变为 `ae1b8380…`；**曲线数值未变**」。
3. **没有一条断言覆盖「斜率真的写进了 Keyframe」**：`Editor/BattleScene.cs` 的 E-C 四组只断时长 / 曲线数 / 位置；`grep "\.si"` 在 `Editor/` 下 0 命中。
   这就是这条哑弹能活到今天的原因（断言测的都不是它）。
   **建议**（需 `Editor/*.cs` 写手，本代理无权限）：在 E-C 那节补一条**结构上不可能自证**的断言 —— 例如拿 `BattleHudAnim` 重建出的 `AnimationCurve` 逐键比 `inTangent/outTangent`（今日应恒 0），并配一条「把 JSON 键改回 `is`/`os` ⇒ 期望值不再相等」的负例说明。
   ⚠️ 注意：现在斜率全 0，**这条断言即使接通了也恒绿**（0 == 0）⇒ 真正的守卫是「键名与字段名一致」这件结构事实，断言只能断「读出来不为 null」，或在数据侧放一条**非零斜率**的合成用例才有效。**这点我不下结论**（属于测试设计，请调度台裁）。
4. **`gen_arena_scene_clips.py` 与 `gen_misc_clips.py` 是否也有同族例外**：本次只核实了 battlehud；`arena_scene_clips` 自身已是 `si`/`so`，`camera_intro_clips` 也是。**未普查其余 *_clips.json 生成器**（超出 11-04 范围），列为待查。

---

## 六、一句话

11-04 **已修**：生成器键名 `is`/`os` → `si`/`so`（与 `BattleHudKey.si/.so` 对齐）、两份产物已重生成、`--check` **rc=0**、内容逐值等价（170 个键的斜率今日全 0 ⇒ 数值差 0）、typecheck **0/0**。
