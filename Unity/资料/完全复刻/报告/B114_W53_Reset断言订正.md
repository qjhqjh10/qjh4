# `B114_W53` · `Reset` 那条断言订正（改断言，⛔ 不改实现）

> 写手 `W-53` · 2026-11-01 · 工作目录 `d:/4`。
> 输入判据：`判据/B114_R10_Reset_ColorLookup诊断.md`（§四-1 给了可粘贴替换段）。
> ⛔ 未跑 Unity · ⛔ 未动 git · 只改了一条断言（+ 一个自检小工具）与两处**注释**。

---

## 〇、一句话

**主判 `(α) 断言写错`**，已按 `R10` 的口径重写，实现**一个字没动**：

- 旧断言断的是「`Reset` 之后 `ColorLookup.texture` == `OriginalLut`」——那是 **2026-10-26 之前我们自己那版实现**的口径；
- 原版 `ColorLookup.texture` **恒指 `combinedLUT`**（唯一写点是 `LUTBlender.Awake`），`ResetLUT` 只改**内容**；
- 而且 arena1 在 13 场重建前 `_original == null` ⇒ `ReferenceEquals(null, null)` **恒真**，这条断言**从来没真验过任何东西**。

---

## 一、改前 / 改后（`Editor/BattleScene.cs`）

### 改前（`:11988-11991`，4 行）

```csharp
                    // Reset：交还给**进场时那张静态 LUT**（原版 `PostFXController.ResetLUT`）
                    pfx.Handle(new WarpforgeVFX.PostFxRequest { op = WarpforgeVFX.PostFxOp.Reset, instant = true });
                    Check(ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.OriginalLut),
                          "★ `Reset` 把 `ColorLookup.texture` 交还给了进场时那张（= 该战场的静态 LUT）");
```

### 改后（`:11988-11991`，仍是 4 行）

```csharp
                    // Reset：`ColorLookup.texture` **仍指合并 RT**，变的是它的**内容** = 进场那张静态 LUT
                    //   （原版 `LUTBlender.Awake` 起恒指 `combinedLUT`，`ResetLUT` 只改内容；⚠️ 2026-11-01 `B114_W53` 订正：
                    //    这条以前写「交还给进场那张」= 10-26 之前的实现口径，且靠 `null==null` 空转过一个月 —— ⛔ 别改回去）
                    CheckResetLut(pfx, Check);   // 发 `Reset(instant)` 请求 + 断「引用 / `_Blend` / 像素」三件（判据见类尾）
```

⚠️ **调用点故意保持 4 行（净 0 行）**：`BattleScene.cs` 有 4 条 A 档落点在 `:11988` 之后（`13055` / `13439` / `15410` / `26444`），
往中间插行会让它们全部**漂移**。所以真正的断言体放在**类尾新增的 `CheckResetLut`**（在所有落点 `:47325` 之后 ⇒ 零漂移）。

### 新的三条断言（类尾 `CheckResetLut`，实做）

| # | 断言 | 为什么必须有 |
|---|---|---|
| 0 | `pfx.OriginalLut != null`（**硬前提**） | ⛔ 不许再 `null == null` 假绿；前提不成立就直接红并**返回**（不是静默跳过） |
| 1 | `ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.Combined)` | 原版常驻形态：引用**仍是**合并 RT |
| 2 | `blendBefore < 0.99f && Mathf.Approximately(pfx.BlendNow, 1f)` | 判别信号：Reset 前是 **0.25**、之后必须 **1** |
| 3 | 合并 RT 在 `(128,8)` 的像素 = 原 LUT 同点像素（**±0.02**） | **最强的一条**：只比引用是**恒真**的（Reset 前 `:11985-11987` 刚验过同一个引用），像素才排得掉「Reset 空转」 |

读口都是**真落到 Volume / 材质 / RT 上的值**（`ColorLookupRef.texture.value` · `BlendNow` = `_lastBlend` · `ReadRtPixel`），不读我们自己的 bool。

### 为什么这才是原版语义（判据出处，全部现读）

| 环节 | 文件 | 事实 |
|---|---|---|
| 谁写 `ColorLookup.texture` | `d:/2/tools/decomp_full/LUTBlender__Awake.c` | **只在这里、只写一次**：把 `ColorLookup.texture` 指到 `combinedLUT` |
| Reset 干什么 | `PostFXController__ResetLUT.c` | `SetTargetLUT(originalLUTTexture)` + `timeToBlend = instant ? 0 : 1.0` + `currentTime = 0` + `doAnimatedBlend = 1` —— **不碰引用** |
| SetTargetLUT / DoBlend | `LUTBlender__SetTargetLUT.c` / `LUTBlender__DoBlend.c` | 只改 `_LUT2` 与 `_Blend`，再 `Blit → combined` —— **不碰引用** |
| 算式 | `WarpforgeVFX/LUTBlender` shader 文件头 | `o = lerp(_LUT1, _LUT2, _Blend)` ⇒ `_Blend = 1` 时 = `_LUT2` = 原 LUT |

⇒ **净结果**：引用 = `_combined`（原版常驻形态）；内容 = `lerp(_LUT1,_LUT2,1) = _LUT2 = _original`。三条断言正好覆盖这两个面。

### 自检小工具 `ReadTexPixel`（类尾，新增）

LUT 资产 `isReadable: 0` ⇒ 不能 `GetPixels`；所以 `Graphics.Blit(t → 临时 RT)` 再读。
⚠️ **临时 RT 取 `Combined` 的尺寸**（不是 `t` 自己的）—— 原版 shader 是**同 UV 采样**，`64×4` 那种窄 LUT 会被拉伸填满合并 RT；
按 `t` 的尺寸拷会让两边量到**不同的样本**（静默比出假差异）。**没跑 Unity，这条是按 shader 语义推的**（见 §五）。

---

## 二、硬前提怎么加的

```csharp
bool hasOrigin = pfx != null && pfx.OriginalLut != null;
check(hasOrigin, "（前提）进场时 `ColorLookup.texture` 非空 ⇒ 下面那条才有意义（否则 `null == null` 假绿）");
if (!hasOrigin) return;
```

- **先断前提、再断结论**（CLAUDE.md 那条「`if (x != null) Check(...)` = 前提不成立时静默跳过」的修法）——
  这里**不是** `if (前提) Check(结论)`，前提本身就占一条断言：老 prefab 那 6 场的 `_original` 若又是 `null`，**会红出来**，而不是消失。
- `R10 §五-4` 点名要的正是这个：另外 12 个战场没有运行时 Reset 覆盖，靠这条前提才能在未来某场退化成 `null` 时报警。

---

## 三、两处注释订正（铁律 5，就地改）

| # | 位置 | 订正 |
|---|---|---|
| 1 | `CardPresentation/Battle/BattlePostFx.cs` **文件头第 ② 条（`:28-42`）** | 原文写「6 场 profile 没有 `ColorLookup` 槽 / 我们构建时不给它建 / 只在运行时副本上补一个」—— **已过期**。改成：`B114_W14` + 13 场重建起 `ArenaBuilder.ApplyPostFx` **构建时就建槽**（6 场回退贴共享 `LUT Normal`），现读 `arenas/*/Profiles/*_PostFx.asset` **13/13 都有槽**、`MissingColorLookup` **恒 0**，`Add<ColorLookup>` 支只剩老 prefab 会走到。⚠️ **原 `A293` 那次反方向的订正（「原版静态 LUT 也是空的」）保留**，两处都留着。⚠️ 并注明「运行时补槽那条支别当死代码删掉」。<br>**行数：15 → 15（净 0）** —— 该文件有 6 条 A 档落点（`:131/300/306/342/363/378`）全在这段之后。 |
| 2 | `BattlePostFx.cs:498`（单行注释） | `// 原版走到那一步的**净结果**也是这张` → `// ⚠️ 2026-11-01 订正：**只有降级支**（_mat/_original 为空）停在这张 —— 全链就绪时下面 BlendTo(1f) 会把它改回 _combined（内容 = _original）；原版 texture 恒指 combinedLUT（⛔ 别据此把 BlendTo(1f) 当冗余删掉）`。<br>**行数：1 → 1（净 0）**，且在最后一条落点 `:378` 之后，不影响任何落点。 |

> 为什么要改第 2 处：下个会话极容易照那句「净结果也是这张」把 `BlendTo(1f)` 当冗余删掉 ——
> 那会把 `B8_AF`（2026-10-26）修好的「之后任何一次 `BlendTo(0)` 又把复位结果掀回去」**请回来**。

---

## 四、净增净减

| 文件 | 行数（前→后） | diff（含本批前的既有改动） |
|---|---|---|
| `Editor/BattleScene.cs` | **50,660 → 50,712（+52）** | `78 +/27 −`（基线 `22 +/23 −` ⇒ 本轮 = **+56 / −4**） |
| `Battle/BattlePostFx.cs` | **562 → 562（±0）** | `13 +/13 −`（纯注释改写） |

- `BattleScene.cs` 的 **+52 全部落在类尾**（最后一条账落点 `:47325` 之后）⇒ 落点**零漂移**；
- 调用点那块是 **4 → 4 行（净 0）**；
- 行尾：两文件分别仍是 **CRLF（50,712/50,712）** 与 **LF（562/562）**，**裸 CR = 0**（二进制数的）。

---

## 五、容差待实测（⚠️ 红线：本代理跑不了 Unity）

- 用的是 `R10` 给的 **±0.02**（三通道 |Δ| 之和，`≈ 5/255`）。项目 `ProjectSettings.asset` 的 `m_ActiveColorSpace: 0` = **Gamma**
  ⇒ `Graphics.Blit` + `ReadPixels` 无 sRGB 转换，**理论上可逐值相等**；
- 取样点 `(128,8)` 在 `256×16` 上正好是 **texel (128,8) 的网心**（像素中心 `(128.5,8.5)` ⇒ uv = 8.5/16 = 网心）⇒ bilinear **不该混两个纹素**；
  但 `LUT Normal` 的 `filterMode: 1` / `enableMipMap: 1` / `mipMapMode: 0` 我**没有实测**；
- **建议同步点先跑一次 `BattleScene.Run`、把日志里 `R=` / `O=` 那 6 个数字抄下来**，再决定收紧到多少或放宽。
  ⛔ **在那之前别把 0.02 改成更紧的值**（会变成「没实测就收紧 ⇒ 假红/假绿」）。

---

## 六、待复跑 / 没查清的部分

1. **待复跑 `BattleScene.Run`**（本代理不能跑 Unity —— 红线）。预期：原来 8,846 ✓ / 1 ✗ 那一批里，那条 ★ 转 ✓，
   并多出 **3 条**新断言（前提 / 引用 / `_Blend`）+ 像素那条的读数会打进日志。**断言合计会变**（+3 或 +4，取决于像素那条是否计入同批）。
2. **像素断言是否真的逐值相等：没实测**（§五）。
3. **`_original` 非 null 这条路只在 `battlearena1` 跑得到**（`BattleScene.Run` 只打这一场）——
   修好之后另外 12 场仍然**没有被这条断言覆盖**，靠第 0 条硬前提在未来兜底（`R10 §五-4`）。
4. **顺手发现、⛔ 我没改的三条**（原样保留在 `R10 §六`，转给调度台分流）：
   ① `Attach` 没做原版 `Awake` 的「初始 Blit + 立刻把 `ColorLookup.texture` 指到 `combinedLUT`」两步；
   ② `报告/B114_W41_A243与聚合行自检.md:182` 那句「仍然成立」按 `R10` 应订正为**不成立**（那是**别人**的产物文件，不在我的白名单）；
   ③ 本条断言的覆盖面很窄（见上一条）。
   ⚠️ 关于 ②：`R10 §四-3` 点名要改，但**该文件不在我 `✅ 可写` 白名单里**（只给了 `BattleScene.cs` / `BattlePostFx.cs` / 本报告）
   ⇒ **按红线「卡住停手」处理，不改，交给调度台**。
5. **实现一个字没动**：`ResetLutCore` / `BlendTo` / `Detach` / `_503-505` 三句全部原样（`R10 §4.2` 的四条理由）。

---

## 七、验收（本代理能做到的部分）

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_w53 bash Unity/工具/typecheck.sh` | ✅ **运行时 0 / 编辑器 0** |
| `python -I -X utf8 Unity/工具/check_ledger_points.py`（只读） | ✅ **A 5,841 · 通过 5,841 · 漂移 0 · 失败 0**；B 4 条 / 落点问题 2 条 —— **与改前基线逐项相同** |
| 行尾未被翻 | ✅ CRLF/LF 各自保持，裸 CR = 0 |
| `BattleScene.Run` | ⏳ **待同步点复跑**（本代理不能跑 Unity） |
