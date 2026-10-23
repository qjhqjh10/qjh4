# R1 `LutBlendProbe` 实现说明（2026-10-23 · W65 · A1 · R1）

> **本件是什么**：`WarpforgeArena1/Editor/LutBlendProbe.cs` 的实现说明 —— 一个 **A/B 专验宿主**，
> 用来验 R1（自建 `WFLUTBlender.shader` 补了 VS 的 `uv * _LUT1_ST.xy + _LUT1_ST.zw` 那一跳）。
>
> **判据（唯一来源，本件不另立口径）**：
> · `资料/普查产出_1023/E4_R29分因与R1判据.md` **§④-1**（骨架 + **A1–A7** 七条断言 + A/B 口径 + 色彩空间那条红线）
> · `资料/普查产出_1023b/特效线_执行报告.md` **§一 / §8·1**（本轮已修内容；「`cb0[2]` 查不到 ⇒ 取 `_LUT1_ST`」）
>
> **状态**：宿主已写完、**秒级类型检查 0/0**，但**一次都没跑过 Unity**（本轮红线：主对话串行跑）。

---

## 一、做了什么

新建 `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/LutBlendProbe.cs`（入口 `public static void Run()`）。
它把 E4 §④-1 的**第 0–4 步**逐条落成代码，**不建场景、不存盘、不改工程资产**（只 `Graphics.Blit` 到临时 RT + 往
`d:/4/_tmp_view/lutblend/` 写 PNG）。

| E4 步骤 | 落地 |
|---|---|
| 第 0 步 合成两张 256×16 LUT | `BuildLuts()`：`A(x,y)=(x/255, y/15, 0,1)` · `B(x,y)=(1-x/255, 1-y/15, 0.5,1)`；`new Texture2D(..., linear: true)`（**mip 关**、Bilinear、Clamp）。**期望值一律从同一份字节数组算**（不是拿理想公式算），避开 8 bit 量化差 |
| 第 1 步 建材质/RT | `Blit()`：`new Material(sh)` + `SetTexture("_LUT1"/"_LUT2")` + `SetFloat("_Blend")`；`RenderTexture.GetTemporary(256,16,0,ARGB32,**RenderTextureReadWrite.Linear**)` |
| 第 2 步 四档 `_Blend` | `_Blend ∈ {0, 0.25, 0.5, 1}` 各一次 `Graphics.Blit(null, rt, mat)`（**和 `BattlePostFx.BlendTo` 同一条真实路径**），`RenderTexture.active + ReadPixels` 读回 |
| 第 3 步 同参数跑原件 | `Hidden/LUTBlender` 从 `StreamingAssets/WarpforgeVFX/wf_arena_2.bundle` 取（`Shader.path_id = 42`） |
| 第 4 步 输出 + 存图 | 每档一行 `LP [case] maxAbs=… badPix0=… badPix2=… meanAbs=… magenta=…%`；PNG 原尺寸 + ×4 放大各一张 |

**取原件的路线**：`AssetBundle.LoadFromFile` → **`LoadAllAssets<Shader>()`** 按 `name == "Hidden/LUTBlender"` 匹配
（⚠️ **不用** `LoadAsset<Shader>(名字)` —— 本仓踩过「它认的是容器键、裸名恒 null」）。
兜底四层：名字后缀 → 包内唯一 shader → 容器键含 `lutblender` → 全部打日志说明走了哪一层。
`LoadFromFile` 返回 null 时再从 `AssetBundle.GetAllLoadedAssetBundles()` 里找同名包。

**色彩空间**（E4 §④-1 的硬要求）：合成的 LUT、输出 RT、**读回用的那张 Texture2D** 三者**全部 linear**，
文件头写了原因（工程 `m_ActiveColorSpace = 1`，14 张 LUT 都是 `sRGBTexture: 0`）。

**`_LUT1_ST`，不是 `_MainTex_ST`**（报告 §8·1 的订正）：`ApplyST()` 走**纹理 scale/offset**（Unity 上传材质时
把这两个值写进 `<tex>_ST`，内置管线与 URP 同一条），并**再直写一次**同名向量（`HasProperty` 为真才写）。
每次打 ST 都 **回读**一遍并打日志 —— 挡「量具自己没生效却报成 shader 不响应」这种假象。

---

## 二、断言清单（A1–A7，逐条都有落点）

| # | 断言（E4 原话） | 代码里怎么算 | 判读口径 |
|---|---|---|---|
| **A1** | `_Blend=0` ⇒ 输出与 LUT1 **逐字节相同** | `Compare(ours/b0, A)`，`badPix0` = 任一通道差 > 0 的像素数 | `badPix0 == 0` ⇒ ✓；仅差 1 LSB（`badPix2 == 0`）⇒ **≈**（近似，日志会写清） |
| **A2** | `_Blend=1` ⇒ 输出与 LUT2 逐字节相同（**灭自证**：挡「恒返回 LUT1」） | `Compare(ours/b1, B)` | 同 A1 |
| **A3** | `_Blend=0.5` ⇒ 与 `(A+B)/2` 每通道 **≤ 2/255** | `Compare(ours/b05, Fade(A,B,0.5))` | `badPix2 == 0` ⇒ ✓；`maxAbs ≤ 4` ⇒ ≈ |
| **A4** | `_Blend=0.25` ⇒ 与 `lerp(A,B,0.25)` ≤ 2/255（挡「把 `_Blend` 当 0/1 开关」） | 同上，t=0.25 | 同 A3 |
| **A5** | `_LUT1 = _LUT2 = X` ⇒ 输出 == X（任意 `_Blend`；挡「没采纹理、自己发光」） | 三档 `b0 / b0.5 / b1` 各比一次 `A` | 三档都 `badPix2 == 0` ⇒ ✓ |
| **A6** | 原件那一路：`maxAbs ≤ 2` ⇒ **可进 `UseOriginal`**；**整片 (255,0,255) ⇒ 记「原版批处理下不可渲染」**（替换理由的正向证据，**不算失败**，但必须打出来） | 原件四档各自与期望比，取 `maxAbs` 最差 + `magenta%` | 两条**可接受结局**都记 ✓（日志写清是哪一条）；其它 ⇒ ✗ 并指向翻转诊断 |
| **A7** | VS 的 ST 那一跳：`_LUT1_ST=(1,-1,0,1)` ⇒ 输出应为 **A 的上下翻转版** | `Compare(ours/stflip, FlipY(A))` | `badPix2 == 0` **且** 与未翻转版 `maxAbs > 8`（后半句是**灭自证**：挡「压根没读 ST」）。改前这条只有原件会翻 ⇒ 现在是「两侧都翻」才算过 |

🔴 **「没取到像素」本身也要判红**：`Cmp.ok == false`（case 抛异常 / 尺寸不符）时
`badPix0`/`badPix2` 一律置 `int.MaxValue` 哨兵 ⇒ **A1–A7 全部必红**。
（不这么做的话，默认的 `0` 会让「图都没渲出来」的 A1 **静默变绿** —— 这是本仓反复强调的自证类陷阱。）

**判绿红**：`LP === RESULT R1 PASS|FAIL ===`（只看**自建那份**的 A1–A5 + A7；A6 是第二判据，
不参与 R1 的绿红，但一定会打出来）。

---

## 三、附加诊断（**不改判据**，只把「查不到」的问号关掉）

1. **反推原件的 `cb0[2]` 是哪一张 `_ST`**（报告 §1·3 那条「查不到」）：
   对原件逐张打翻转 `_LUT1_ST` / `_LUT2_ST` / （声明了的话）`_MainTex_ST`，看哪一张能让输出**真的翻**。
   日志前缀 `LP   A7 诊断 · 原件 + …`，结论会直接写「原件真正读的是 **`_X_ST`**」。
   ⇒ 若结论不是 `_LUT1_ST`，**自建那份要改用同一张**（这属于下一轮改动，本件只量）。
2. **原件 ↔ 我们 同参数逐像素比**（`CrossReport`）：平台层面的 Blit 翻转 / 精度差会在这里现形。
3. **翻转提示**：每一条 `Report` 都顺带比一次「上下翻转版」，若翻转版更像会在行尾打
   `⚠ 更像「上下翻转版」（flipY maxAbs=…）`。

---

## 四、没查清的部分（**不许当结论**）

1. 🔴 **宿主一次都没跑过**（本轮红线：不跑 Unity）。所以「原件能不能取到」「原件是否整片品红」
   「A1 是不是真的逐字节」——**全部是未测**，要主对话跑一次才有读数。
2. **`cb0[2]` 的真身**：要等 `LP   A7 诊断` 那几行出来才关闭。三种结局都可能：
   命中唯一一张 / 三张全不响应（可能由 `Graphics.Blit` 内部绑死）/ 多张同格。
3. ⚠️ **`_MainTex_ST` 那条诊断多半打不出来**：原件属性表只有 `_LUT1`/`_LUT2`/`_Blend`，
   `_MainTex_ST` 大概率不是声明属性 ⇒ `SetVec` 会打一句「跳过直写」。这条**不影响 A1–A7**。
4. ⚠️ **覆盖范围 = `Hidden/LUTBlender` 这一个全屏 blit**，**不进**战场后处理栈
   （`BattlePostFx` → `ColorLookup` / URP Volume 那一整条）。E4 §2·4 已说明 sweep 从来没进过后处理栈，
   本件同样只量 shader 本身，**不量它在战场里的最终观感**。
5. **精度档**：A1/A2 的「逐字节」在 GPU 采样下可能差 1 个 LSB（半精度 `lerp` / 8 bit 量化）。
   探针把「差 0」与「只差 1–2 个字节级」**分开报**（`badPix0` vs `badPix2`），不混为一谈。
6. **非单位 ST 在真实链路里的后果**（256×16 的 LUT 被上下颠倒）**没有在这里造真实场景**：
   A7 只证明「ST 生效且方向与原件一致」，不证明「战场上真会出现非单位 ST」。

---

## 五、改动文件

| # | 文件 | 说明 |
|---|---|---|
| 1 | `d:/4/Unity/MyGame/Assets/WarpforgeArena1/Editor/LutBlendProbe.cs` | **新建**（本件唯一代码改动） |
| 2 | `d:/4/Unity/资料/普查产出_1023b/R1_LutBlendProbe_实现.md` | **新建**：本说明 |

（`.meta` 由 Unity 生成，属正常。）
⛔ **没碰**：`WFLUTBlender.shader` · `WarpforgeShaderMap.cs` · 任何 `Core/**` `Battle/**` ·
`CLAUDE.md` / `项目任务.md` / 台账 · git。

---

## 六、怎么跑 + 判读

```bash
unset ELECTRON_RUN_AS_NODE && "D:/Unity/Hub/Editor/6000.3.23f1/Editor/Unity.exe" \
  -batchmode -quit -projectPath "D:\4\Unity\MyGame" -executeMethod LutBlendProbe.Run -logFile -
# 筛日志：grep "^LP " <log>
# 图在   ：d:/4/_tmp_view/lutblend/（每档 PNG 原尺寸 + ×4）
```

**判读**：

| 读数 | 结论 |
|---|---|
| `LP === RESULT R1 PASS ===` | 自建那份 A1–A5 + A7 全绿 ⇒ **本轮补的 `_LUT1_ST` 那一跳成立** |
| `A6 … 原件可进 UseOriginal` | 原件在批处理下**能渲且与期望一致** ⇒ `WarpforgeShaderMap` 那条「品红」理由**不成立**（改不改由主对话定） |
| `A6 … 整片品红` | 原件**批处理下不可渲染** ⇒ 替换理由**成立**，保持自建（**不是失败**） |
| `A7 诊断：原件真正读的是 **`_X_ST`**` | 报告 §1·3 的「查不到」关闭；若 `X ≠ _LUT1` ⇒ **下一轮把自建那份改成 `X`** |
| `A1/A2` 是 `≈` 而不是 `✓` | 只差 1 LSB（`badPix2 = 0`）⇒ 行为等价，但**「逐字节」这条按字面没过**，如实记 |
