# B141-W33 · CardView 两条文字色（5-10）＋ 卡框超采样（5-21）执行报告

> 2026-11-04 · 第一百二十八会话 · 批 `B141` · 波次 `W129` · 执行写手 `B141-W33`
> 输入：`判据/B141_W129_对账_卡组色彩.md` §二 的 `5-10` / `5-21`（P5 包，`Core/CardView.cs` 独占）
> 判据复核：`报告/B131_审计_色彩材质与渲染.md` 的 `B3` / `B5` / `B14` ＋ 原版 prefab / 材质 / shader 字节码 **本次现读**
> 白名单：`Core/CardView.cs` · 新建 `Editor/B141_W33_Assert.cs` · 本报告。**未碰**其他文件 / 账 / 正本 / 所有权表；未跑 Unity；未动 git。

---

## 〇、结论

| 条目 | 判定 | 一句话 |
|---|---|---|
| **5-10** | ✅ **已做完** | 缺陷落点是**兵种行**（原版 253），不是阵营行（原版 255）—— 两行是两条独立判据链。新增类尾常量 `InkUnit = (253,154,88)`，兵种行的建层 + `ApplyTint` 两处都换过去；`InkArmy` 保持 255 并就地订正注释（铁律 5）。 |
| **5-21** | ✅ **已做完（续做 `B141-W33b`）** | 判据成立且已按原版 DXBC 落地：`CardSprite.shader` 加 `_AAScale` + `SUPERSAMPLING_ON` 的 4-tap RGSS；卡框 / 卡背材质 = 1.25、共享基材 = 0；全局关键字随画质档 3/4 ↔ 0/1/2 切换。详见 §五。 |

`typecheck`：运行时 **0** / 编辑器 **0**（`TMPDIR=/tmp/wf_b141_w33 bash 工具/typecheck.sh`）。

---

## 一、5-10 · 阵营行 / 兵种行文字色

### 1.1 判据（本次亲自现读，两份都是原版 bundle 的 JSON）

| 行 | 原版组件 | `m_fontColor` | 材质 `_FaceColor` | 乘积（屏上色） |
|---|---|---|---|---|
| **阵营行** `ArmyText*` | `bundle_battleprefabs_vfxandmisc_assets_all/MonoBehaviour/MonoBehaviour_-733742756674036800.json`（`m_text = "Goff klan"`） | **(1, 1, 1, 1)** | `Pragati-Regular Card Unit type`（pathID `3931739351530146963`）= **(1.0, 0.605498, 0.344340, 1)** | **(255, 154, 88) = #FF9A58** |
| **兵种行** `RaceText` | 同 bundle `MonoBehaviour_4216994895762529216.json`（`m_text = "Troop"`） | **(0.992157, 0.603922, 0.345098, 1)** | `Pragati-Regular Card Content`（pathID `-6250168224648873853`）= **(1, 1, 1, 1)** | **(253, 154, 88) = #FD9A58** |

两份材质 / TMP 都在 `assets_full` 里按 pathID 现读（不是转述）：
`m_SavedProperties.m_Colors` 里 `_FaceColor` 分别是 `(1, 0.60549784, 0.34433961)` 与 `(1,1,1,1)`。

### 1.2 审计原话的偏差（铁律 5：就地订正）

`B141_W129_对账_卡组色彩.md` 的 `5-10` 写「`InkArmy` 改 253」。**照字面改会修错一行**：

- `InkArmy` 在我们代码里**同时**供 `_army`（阵营行）与 `_race`（兵种行）两处；
- 原版阵营行 = **255**（白字 × `Card Unit type` 的 `_FaceColor`），只有兵种行是 **253**；
- 把 `InkArmy` 直接改 253 ⇒ **阵营行**从「✅与原版一致」变成新的偏差（B131色 `B3` 会翻红）。

⇒ 正确做法 = **拆成两条判据**，不是把一个常量改数。

### 1.3 改动（`Core/CardView.cs`）

| # | 位置 | 改前 | 改后 |
|---|---|---|---|
| ① | `:5145`（`_race = Fill(...)` 的基色实参） | `InkArmy` | `InkUnit` |
| ② | `:8178`（`ApplyTint` 里 `_race.color`） | `(Color)InkArmy` | `(Color)InkUnit` |
| ③ | `:12941-12943`（`InkArmy` 的 doc） | 「阵营行 / 兵种行：… `_FaceColor` ⇒ 255」 | 「**阵营行**…」＋ 明确写出兵种行走另一条链、落点 `InkUnit`、⛔ 别合并（**行数不变，3 行 → 3 行**） |
| ④ | **类尾**（`SetBadgesDirtyAware` 之后） | — | 新增 `InkUnit = new Color32(253, 154, 88, 255)` ＋ 判据 doc ＋ `// ✅ 已复刻（2026-11-04 · B141 · B131色 B5…）` |

- **行数变化**：`CardView.cs` **16,315 → 16,324**（+9）。其中 **3 行是就地改注释（净 0）**，
  **9 行全部追加在类尾**（`SetBadgesDirtyAware` 之后、类闭合 `}` 之前）—— 按铁律 20 / 18·b，**类尾之后没有账的落点**，不会造成 `check_ledger_points` 行号漂移。
- 行尾：改前改后都是 **纯 LF**（`b.count(b'\r\n') = 0`，无裸 CR）。
- `InkUnit` 与 `InkName` **数值相同**（都是 253,154,88）但**判据不同**（一个是卡名 `NameText*`，一个是兵种行 `RaceText`）—— 故意不合并：两条链将来任一变动互不牵连（注释已写死）。

### 1.4 断言（新建 `Editor/B141_W33_Assert.cs`）

- 夹具：`CardData.Placeholder(0)` 显式补 `type="unit"` / `subtype="Infantry"` / `faction="Ember"`，
  `CardView.Create(..., CardFace.Full)` ⇒ 阵营行与兵种行都会建出来。
- 逐条：阵营行 **255 / 154 / 88**（A5/A6/A6b）· 兵种行 **253 / 154 / 88**（A★/A7/A8）。
- **灭自证**（结构上不可能同时满足）：
  - A9 = 「阵营行 = 255 **且** 兵种行 ≠ 255」⇒ 把两行合并成一个常量（无论取 253 还是 255）必红一条；
  - A★ 若 `_race` 用回 `InkArmy` ⇒ 红；
  - A10/A11 走 `SetAlpha2D(0.5f)`（= `ApplyTint` 那条路）复验两行仍是 255/253 ⇒ **只改建层、漏改 `:8178`** 会红。
- 前提硬检查（⛔ 不静默跳过）：`TmpFont.Available` · 卡建出来了 · `army`/`race` 两个 TMP 层存在 · 字体材质 `_FaceColor` = 白。
- **断言条数：16**（`P0` · `A0`–`A4` 前提 5 条 · `A5/A6/A6b` 阵营行 3 条 · `A★/A7/A8` 兵种行 3 条 ·
  `A9` 灭自证 · `A10/11/12` `ApplyTint` 路径 3 条）。前提不成立时 `P0` 直接红并**早退**，不会拿 null 假绿。
- ⚠️ **接线待办**：本包白名单不允许改任何宿主（`BattleScene` / `ShellScene` / …）⇒ 断言文件**尚未挂进任何宿主**，
  由调度台另派（铁律 13·3「建了断言必须接线」）。本宿主执行后应看到合计行。

---

## 二、5-21 · 卡框超采样（`Custom/SuperSampled Texture` / `_AAScale 1.25`）

### 2.1 判定：**判据成立**（`B141-W33` 首轮因白名单停手 ⇒ 续做 `B141-W33b` 已落地，见 §五）

- 原版**确实**在卡框 / 卡背材质上跑 4-tap 旋转网格超采样，且**出厂默认是开的**（见 2.3）。
- `B141-W33` 首轮的白名单只有 `CardView.cs` / 断言 / 报告，改不了 `CardSprite.shader` ⇒ 当时按红线停手；
  `B141-W33b` 已扩白名单（shader + `SettingsWindow` 画质应用那一跳）并按 §2.5 落地。

### 2.2 原版算法（**本次从原版编译字节码反汇编**，⛔ 不是猜的）

`wf_shaders_extra.bundle` 的 `Custom/SuperSampled Texture`：`compressedBlob` LZ4 解开后 7,368 B，
内含 **4 个 DXBC 程序块**（2 个顶点 + 2 个片元；`D3DDisassemble` 逐块现读）：

- **顶点**（两份逐字相同）：`pos = MVP · obj`；`uv` 直通；`color` 直通。
- **片元 · 关键字 OFF**（`ps_4_0`，368 B）：
  ```
  sample r0, v1.xy, t0, s0
  mul    o0, r0, v2            // 贴图 × 顶点色
  ```
- **片元 · `SUPERSAMPLING_ON`**（`ps_4_0`，988 B）—— 4-tap **旋转网格超采样（RGSS）**：
  ```
  r0 = v1.xyxy * _AAScale                       // cb0[2].x = _AAScale
  dx = ddx(r0); dy = ddy(r0)
  s1 = tex(uv + 0.125·dx + 0.375·dy)
  s2 = tex(uv + 0.375·dx - 0.125·dy)
  s3 = tex(uv - 0.125·dx - 0.375·dy)
  s4 = tex(uv - 0.375·dx + 0.125·dy)
  o0 = (s1+s2+s3+s4) * v2 * 0.25                // 先平均、再乘顶点色
  ```
  （偏移系数就是标准 4×RGSS 的 `±(0.125,0.375)/(0.375,−0.125)`，整体乘 `_AAScale`；
  混合态 = `SrcAlpha → OneMinusSrcAlpha`、`ZWrite Off`、`Cull Off`。）
  ⚠️ 与我们 `CardSprite` 的**预乘**（`One OneMinusSrcAlpha` + `rgb *= a`）**结果等价**：
  `avg(rgb) × avg(a)` 两条路算出来相同 —— 实现时按我们既有的预乘写法即可。

### 2.3 门控链（为什么「材质里关键字没开」≠「原版没超采样」）

原版材质 `Card Frame and Cardback for combat`（`battleprefabs_vfxandmisc_assets_all`）现读：

```
m_ValidKeywords    : []
m_InvalidKeywords  : [ "_SUPERSAMPLING_2X2_RGSS", "_SUPERSAMPLING_ON" ]
m_Floats           : _AAScale = 1.25 · _Bias = -0.75 · _SuperSampling = 1
```

- 两个带**前缀下划线**的关键字是**旧 shader 版本遗留**（现 shader 声明的是 `SUPERSAMPLING_ON`，无下划线）⇒ 它们是
  invalid，不是「关了超采样」。
- 真正开关 = **全局关键字**：`QualitySettingsManager.ApplyGraphicsQuality`（ILSpy
  `cpp2il/out_cpp2il_dev_ilspy/QualitySettingsManager.cs:295-302` 实读）：
  ```
  if (!qualityDefinition.enableCardsAntiAliasing) Shader.DisableKeyword("SUPERSAMPLING_ON");
  else                                           Shader.EnableKeyword("SUPERSAMPLING_ON");
  ```
- 各画质档的 `enableCardsAntiAliasing`（原版 `Assets/Data/Scenes/Intro.unity` 的 `QualitySettingsManager`
  实例 `qualityDefinitions`，本次现读）：**quality 0/1/2 = 0** · **quality 3 = 1** · **quality 4 = 1**。
- 我们的 `GameStaticData.graphicsQuality` 出厂 = **Ultra(4)**（`Shell/GameStaticData.cs:106`）⇒
  **原版默认配置下卡框就是超采样的**（只有选到 VeryLow/Low/Medium 才关）。
- `_Bias` / `_SuperSampling`：shader 属性表只有 `_MainTex` / `_AAScale`（`普查产出_0917/shader属性表_块1.md` §2）
  ⇒ 这两个是**死值**（B131 B14 的判定成立）；`_AAScale` 只在关键字 ON 的变体里被读。
- 用到的 prefab 节点（`Assets/CardPrefab.prefab` 现读）：`CardFrame`（`:16137`）与 `Cardback`（`:19985`）
  的 `Image.m_Material` 都指这颗材质 ⇒ 卡框与卡背**同一支 shader**。

### 2.4 我们现状（⚠️ 下表是 **`B141-W33` 首轮的现读**，三行均已在 `B141-W33b` 修掉 —— 见 §五）

| 位置 | 首轮现状（改前） |
|---|---|
| `Resources/Shaders/CardSprite.shader` | 只有 `_MainTex` / `_Color` / `_GreyScale`，**无 `_AAScale`、无 `SUPERSAMPLING_ON` 变体** |
| `Core/CardView.cs` `BaseMaterial()`（`:7989`） | 所有卡面层的共享基材 ⇒ **不能直接给整支 shader 开超采样**（原版只有卡框/卡背那颗材质超采样） |
| `Shell/SettingsWindow.cs` `ApplyGraphicsQualityOnClose()`（`:7003`） | **自陈**「③ `SUPERSAMPLING_ON` 关键字 **未应用**（本工程没有对应物）」—— 这条缺口早被记过（`判据/B94_R-G_画质档位映射.md` §五①） |

### 2.5 施工单（`B141-W33b` 已按此实施；见 §五）

1. **shader**（`CardSprite.shader`）：加 `_AAScale`；`#pragma multi_compile __ SUPERSAMPLING_ON`；
   片元按 2.2 的 4-tap RGSS（偏移系数逐位照抄），`_AAScale = 0` / 关键字 OFF 时保持原路径（逐像素不变）。
   ⚠️ **属性默认值取 0（关）、不是原版的 1.25** —— 本 shader 是全卡面层共用的（`ImageQuad` 还直接
   `new Material(sh)`），默认 1.25 会把立绘/底板/数值层一起超采样；已按铁律 18 在 shader 里打
   `🔶 自己设计` 并写明理由（调度台 2026-11-04 裁定），卡框/卡背由材质显式写 1.25。
2. **不能污染其它卡面层**：`BaseMaterial()` 是全卡面层的共享模板 —— 给模板显式写 `_AAScale = 0`（关），
   只给 **卡框 / 卡背**那两份实例材质写回 **1.25**（`AddLayer("frame", …)` 与 `_backMr` 两处）。
   否则立绘 / 底板 / 数值层会被一起超采样，**原版没有**。
3. **渲染队列 / 预乘**：保持 `CardSprite` 现有 `Blend One OneMinusSrcAlpha` + `rgb *= a`（2.2 已证明等价）。
4. **画质门**：`Shader.EnableKeyword/DisableKeyword("SUPERSAMPLING_ON")`，条件 = 原版
   `qualityDefinition.enableCardsAntiAliasing`（`graphicsQuality >= High`，即 3 / 4）。
   落点应是 `SettingsWindow.ApplyGraphicsQualityOnClose()`（= 原版 `ApplyGraphicsQuality` 那一跳）
   ＋ 启动那一次（`SuperSampling` 类已有 `[RuntimeInitializeOnLoadMethod(AfterSceneLoad)]` 同款时机）。
5. **自检**：至少断 ① shader 有 `_AAScale`（默认 **0** = 关）；② 卡框/卡背材质 `_AAScale = 1.25`、其它层 = 0；
   ③ 关键字随 `graphicsQuality` 3/4 ↔ 0/1/2 切换；④（灭自证）把基材也设 1.25 ⇒ 必红。

---

## 三、验证

| 项 | 结果 |
|---|---|
| `TMPDIR=/tmp/wf_b141_w33 bash 工具/typecheck.sh` | 运行时 **0** 错 · 编辑器 **0** 错 |
| `TMPDIR=/tmp/wf_b141_w33b bash 工具/typecheck.sh`（续做后） | 运行时 **0** 错 · 编辑器 **0** 错 |
| 断言文件 | `Editor/B141_W33_Assert.cs`（编译通过属 typecheck 覆盖）；**未接线**（宿主不在白名单）。A 组 16 条 + B 组 21 条 = **37 条** |
| 行尾 | 四个被改文件改后均纯 LF（`\r\n` = 0、裸 CR = 0） |
| 行数 | `CardView.cs` 16,315 → 16,366（W33 +9 / W33b +42，**全部在类尾或方法内新增行**）· `CardSprite.shader` 73 → 122 · `SettingsWindow.cs`（与 `B141-W13` 的未提交改动共用同一工作树） |
| shader 编译 | ⚠️ **只能 Unity 验**（本批记 `W132`）：本包只做静态自检（属性表 / 关键字 / 偏移常量 / 默认值）——下一次 Unity 同步点必须真编译 + 看卡框边缘 |

---

## 四、❓ 未闭合 / 需调度台决定

1. ~~**5-21 白名单**~~ ✅ **已解决**：`B141-W33b` 已获扩白名单并落地（见 §五）。
2. **断言接线**：`B141_W33_Assert.Run()` 目前没有任何宿主调用它 —— 需要一个宿主（`ShellScene.Run`
   或 `BattleScene.Run` 的断言聚合处）显式调用。不在本包白名单。
3. **`5-01` 复核**：`判据/B141_W129_对账_卡组色彩.md` 的 `5-01` 判 `InkName` 已 ✅，本次现读一致
   （`:12940` = 253,154,88），不需要再动。
4. ⚠️ **`SettingsWindow.cs` 是共用工作树**：本次改动与 `B141-W13` 的未提交改动并存（对方改的是
   设置窗其它节）。本包只动了 `SuperSampling` 类与 `ApplyGraphicsQualityOnClose` 的日志，未触碰其节；
   合并/提交时请注意两者都在这一份 diff 里。

---

## 五、续做 `B141-W33b` · 5-21 落地（2026-11-04）

### 5.1 shader（`CardPresentation/Resources/Shaders/CardSprite.shader`）

- 新增属性 `_AAScale ("AA Pixel Width", Range(0, 10)) = 0`，并声明
  `#pragma multi_compile __ SUPERSAMPLING_ON`（原版关键字名，⛔ 不改名）。
- 片元里按 §2.2 的 DXBC **逐句**实现：
  ```
  #ifdef SUPERSAMPLING_ON
  if (_AAScale > 1e-4) {
      float2 duv = i.uv * _AAScale;  float2 dx = ddx(duv);  float2 dy = ddy(duv);
      c  = tex2D(_MainTex, i.uv + 0.125*dx + 0.375*dy);
      c += tex2D(_MainTex, i.uv + 0.375*dx - 0.125*dy);
      c += tex2D(_MainTex, i.uv - 0.125*dx - 0.375*dy);
      c += tex2D(_MainTex, i.uv - 0.375*dx + 0.125*dy);
      c *= 0.25;  c *= i.color;
  } else
  #endif
  { c = tex2D(_MainTex, i.uv) * i.color; }
  ```
  之后仍走既有的 `_GreyScale` 去饱和 + 预乘（两条路共用，逐像素语义不变）。
- 🔶 **`_AAScale` 默认 0（而原版默认 1.25）**：本 shader 是卡面各层共用基材（含 `ImageQuad` 直接
  `new Material(sh)`），默认 1.25 会把立绘/底板/数值层一起超采样 ⇒ 用「默认关 + 卡框/卡背显式 1.25」
  保留原版的**着色结果**。已在 shader 里按铁律 18 打 `🔶 自己设计` + 理由 + 裁定日期。

### 5.2 材质档（`CardPresentation/Core/CardView.cs`）

| 位置 | 改动 |
|---|---|
| `BaseMaterial()` | 共享基材显式 `_AAScale = 0`（`HasProperty` 守；退回 `Sprites/Default` 时不出声乱写） |
| 卡框 `AddLayer("frame", …)` 之后 | `ApplyCardAaScale(_frame)` ⇒ **1.25** |
| 卡背 `_backMr.sharedMaterial` 建好之后 | `ApplyCardAaScale(_backMr)` ⇒ **1.25**（原版 `Cardback` 与 `CardFrame` 同一颗材质） |
| 类尾 | `public const float CardAaScale = 1.25f;` + `ApplyCardAaScale(MeshRenderer)`（认不出 `_AAScale` ⇒ 出声，不静默）；`// ✅ 已复刻（2026-11-04 · B141-W33b · …)` |

### 5.3 关键字门（`CardPresentation/Shell/SettingsWindow.cs` · 画质应用那一跳）

- `SuperSampling.CardsAntiAliasingAllowed` = `graphicsQuality >= High`（原版 `enableCardsAntiAliasing`
  五档 0/0/0/1/1，判据 = `Intro.unity` 的 `qualityDefinitions` 现读）。
- `SuperSampling.ApplyCardAntiAliasingKeyword()` = 原版 `ApplyGraphicsQuality` 里那两句
  `Shader.EnableKeyword / DisableKeyword("SUPERSAMPLING_ON")`。
- 调用点 = `SuperSampling.Apply()` **开头**（在两个早退之前）⇒ 启动那次（`ApplyAtStartup`）与
  关窗那次（`ApplyGraphicsQualityOnClose`）都覆盖；原版也是每次应用画质都无条件重设。
- 同方法里那条日志的「③ `SUPERSAMPLING_ON` 关键字 **未应用**」已就地订正为「已应用 + 当前档开/关」。

### 5.4 断言（`Editor/B141_W33_Assert.cs` 的 B 组，21 条）

- B0–B3b：shader 存在 · 有 `_AAScale` · **默认 0** · 源码含 `multi_compile __ SUPERSAMPLING_ON` /
  `ddx/ddy` / `0.125` `0.375` / `_AAScale`（**静态**自检；shader 编译只能 Unity 验 = `W132`）。
- B7–B14：卡框 `_AAScale = 1.25` · 共享基材（`info` 层）= 0 · 卡背 = 1.25 ·（灭自证）基材 ≠ 1.25。
- B15–B18：画质 VeryLow/Medium ⇒ 关键字**关**；High/Ultra ⇒ **开**（灭自证：阈值写错必红）。
- 断言会临时改 `GameStaticData.graphicsQuality` 与全局关键字，`finally` **原样还原**。

### 5.5 仍然欠的

- **shader 编译 + 画面**：只能下一次 Unity 同步点验（`W132`）；本包只静态断属性/关键字/取值。
- **断言接线**：仍无宿主调用（白名单不含宿主）。
- 卡框/卡背的 `_AAScale` 只在**建层那一刻**写入；材质被替换的路径（`_frameDissolveMat` 溶解材质、
  glitch 材质）不受影响 —— 原版那些路径本来也不用这颗超采样材质，属等价。
- **已存盘的旧场景**（如 `Scenes/CardBase.unity` 里那份上一轮生成的序列化材质）没有 `_AAScale` 字段
  ⇒ 读回来是 shader 默认 **0**；**下一次 `CardBaseDemo.Run` / 宿主重建卡视图时会由 `CardView` 写回 1.25**
  （不是缺陷，但下一次 Unity 同步点重生成这些场景后要顺带核一眼）。

