# `B114_R10` · `Reset` 把 `ColorLookup.texture` 交还进场那张 —— 只读诊断

> 只读诊断代理 `R-10` · 2026-11-01 · 工作目录 `d:/4`。
> ⛔ 未跑 Unity · ⛔ 未动 git · ⛔ 未改任何源码/资产（本文件是唯一产物）。
> 输入：`_tmp_view/b114_unity_battlescene2.log`（重建后，**8,846 ✓ / 1 ✗**）· `_tmp_view/b114_unity_battlescene.log`（重建前，8,816 ✓ / 13 ✗）·
> `CardPresentation/Battle/BattlePostFx.cs` · `CardPresentation/Editor/BattleScene.cs` · `d:/2/tools/decomp_full/LUTBlender__*.c`、`PostFXController__ResetLUT.c`。

---

## 〇、结论（四档判定）

**主判 `(α) 断言写错`。**

| 档 | 判 | 理由 |
|---|---|---|
| **(α) 断言写错** | ✅ **主因** | 断言写的是 **`Reset` 之后 `ColorLookup.texture` 引用等于进出场快照 `_original`**。这条期望值对应的是 **2026-10-26 之前我们自己那版实现**（`git show 4b315bbf` 里 `Reset` 只有一句 `_cl.texture.value = _original`）。原版**从来不是**这样：`ColorLookup.texture` 在 `LUTBlender.Awake` 里被一次性指到 `combinedLUT`，之后**永远不再改引用**，`ResetLUT` 只改 `combinedLUT` 的**内容**。 |
| (β) 实现缺陷 | ❌ 不成立 | 现实现（`ResetLutCore` 的 instant 支）的**净结果与原版一致**：`_LUT1 = _LUT2 = _original` + `_Blend = 1` ⇒ `combined = lerp(a,a,1) = 原 LUT`，且 `texture` 指 `combined`（正是原版的常驻形态）。见 §三。 |
| (γ) 本批回归 | ❌ 不是（但本批**暴露**了它） | 13 场重建没有改坏任何行为；它只是把「battlearena1 上 `_original` 是 `null`」这个**掩盖条件**去掉了。见 §二。 |
| (δ) 前提不成立 | ⚠️ **辅助（它为什么能潜伏一个月）** | 重建前这条断言是 **`null == null` 空转绿**：battlearena1 当时没有 `ColorLookup` 槽 ⇒ 运行时补出来的槽 `texture.value = null` ⇒ `_original = null`，Reset 后 `texture.value` 也是 `null` ⇒ `ReferenceEquals(null, null)` 恒真。**它从来没在 arena1 上真正验过任何东西**（与 CLAUDE.md「`if (x != null) Check(...)` = 前提不成立时静默跳过」同族，这次更隐蔽：不是跳过，是 null 对 null 判真）。 |

⚠️ **`B114_W41` 的预测是对的「推理形状」、错的「落地」**：`报告/B114_W41_A243与聚合行自检.md:182` 写「仍然成立（它比的是进场快照 `_original`，不看常量）」——
它只追到 `_original` 变成 `LUT Normal`，**没追 instant 支末尾那句 `BlendTo(1f)` 会把 `texture.value` 从 `_original` 覆盖成 `_combined`**（`BattlePostFx.cs:498` 与 `:505`）。⇒ **W41 那句预测要按本条订正。**

---

## 一、四问逐答

### 1. `Reset` 的实际行为是什么？它把 `texture` 交还给谁？

**代码** `CardPresentation/Battle/BattlePostFx.cs`：

```
363  void Reset(bool instant)              →  365  ResetBloomCore(instant); 366 ResetLutCore(instant);
486  void ResetLutCore(bool instant)
488      if (_cl == null) return;                       // 原版 LUTBlender 非空守卫
489      if (instant || _mat == null || _original == null)
497          _cl.texture.overrideState = true;
498          _cl.texture.value = _original;             // ← 先写快照
499          if (_mat != null && _original != null)
503              _mat.SetTexture("_LUT1", _original);
504              _mat.SetTexture("_LUT2", _original);
505              BlendTo(1f);                           // ← 这里把 texture.value **改写成 _combined**
342  void BlendTo(float v)
346      Graphics.Blit(null, _combined, _mat);
347      _cl.texture.overrideState = true;
348      _cl.texture.value = _combined;                 // ← 最终值
```

⇒ **`Reset(instant:true)` 的实际净结果（全链就绪时）**：
`ColorLookup.texture` = **`_combined`（合并 RT）**，其**内容** = `lerp(_LUT1,_LUT2,1)` = `_LUT2` = **进场那张静态 LUT**。
> 「交还给谁」的准确说法：**没有交还给 `_original` 这个对象**；交还的是**与 `_original` 内容相同的那张 `_combined`**。
> 只在**降级支**（`_mat == null || _original == null`，即老 prefab / 补出来的空槽）才真的把 `texture.value = _original`（此时通常是 `null`）。
> ⚠️ `:498` 那句注释「原版走到那一步的**净结果**也是这张」**具有误导性** —— 它 7 行后就被 `BlendTo(1f)` 覆盖掉了（见 §六-2）。

### 2. 「进场快照」什么时候、在哪里截的？6 场从无 `ColorLookup` 变成有 `LUT Normal` 之后，它变了吗？

- **截点**：`BattlePostFx.Attach` 里 **`BattlePostFx.cs:192`** `_original = _cl.texture.value;`
  位置在 `TryGet<ColorLookup>` / `Add<ColorLookup>`（`:161-169`）**之后**、建材质之前 —— 即「补槽/取槽都已就位」的那一刻。
- **变了，而且是 `null` → 非 null**：
  - 重建前：battlearena1 profile 没有 `ColorLookup` ⇒ 走 `:166 prof.Add<ColorLookup>(true)` ⇒ `MissingColorLookup++`（`:168`），新建组件的 `texture.value` 是 **null** ⇒ `_original = null`。
    **日志证据**：`b114_unity_battlescene.log:34639`「…`ColorLookup` 是运行时补出来的 **1** 次…」，紧接着 `:64800` 那条断言是 **✓**。
  - 重建后：`battlearena1_PostFx.asset:14-19` 现读有 `ColorLookup`（`active: 1` · `texture.m_OverrideState: 1` · `m_Value: {guid: aa44b50c…}` = **`LUT Normal`**）⇒ `TryGet` 命中 ⇒ `_original` = **`LUT Normal` Texture2D**（非 null）。
    **日志证据**：`b114_unity_battlescene2.log:34638`「…补出来的 **0** 次…」；`:248420`「★ 13 场的 `ColorLookup` 都跟原版那一档对得上（实际接上 LUT 的 **13** 场…）」**✓**。
    ⇒ 于是 `:499` 那个 `if (_mat != null && _original != null)` **第一次在 arena1 上成立** ⇒ 走到 `:505 BlendTo(1f)` ⇒ `texture.value = _combined ≠ _original` ⇒ 断言红。

### 3. 这条断言在 13 场重建前是不是绿的？

**是绿的（`✓`），而且是空转绿。**
- 重建前日志 `_tmp_view/b114_unity_battlescene.log:64800`：`BT    ✓ ★ \`Reset\` 把 \`ColorLookup.texture\` 交还给了进场时那张（= 该战场的静态 LUT）`
- 同一份日志 `:34639` 显示那场（`battlearena1`，见 `:34608` 附近「§27 战场：`battlearena1`」）是「补出来的 **1** 次」⇒ `_original = null`。
- ⇒ `ReferenceEquals(texture.value /*null*/, OriginalLut /*null*/)` = **true**。**不是行为对，是两个 null 相等。**

### 4. 若判定是断言错：新的期望值应该是什么？（判据出处）

原版判据（`d:/2/tools/decomp_full/` 现读）：

| 环节 | 文件 | 事实 |
|---|---|---|
| 谁设置 `ColorLookup.texture` | `LUTBlender__Awake.c` | **只在这里设、且只设一次**：`TryGet<ColorLookup>`（`+0x38`）→ 取 `texture.value` 存为 `originalLUTTexture`（`+0x50`）→ 按它的宽高建 `combinedLUT`（`+0x40`）→ 把 **`ColorLookup.texture` 覆写成 `combinedLUT`**（对 `texture` 那个 VolumeParameter 的虚调用，实参 = `*(param_1+0x40)`） |
| Reset 干什么 | `PostFXController__ResetLUT.c` | `LUTBlender.SetTargetLUT(originalLUTTexture/*+0x50*/)` → `timeToBlend = instant ? 0 : TIME_TO_RESET` → `currentTime = 0` → `doAnimatedBlend = 1` → `enabled = 1`。**全程不碰 `ColorLookup.texture` 的引用** |
| SetTargetLUT | `LUTBlender__SetTargetLUT.c` | `Blit(combined → working)` + `SetTexture(mat, "_LUT2", 目标)` |
| DoBlend | `LUTBlender__DoBlend.c` | `SetFloat("_Blend", v)` + `Blit(null → combined)`。**不写 `ColorLookup.texture`** |
| Update | `LUTBlender__Update.c` | `currentTime += dt; DoBlend(currentTime / timeToBlend)`；到点自关。instant 时 `timeToBlend = 0` ⇒ 比值 → ∞ ⇒ 夹到 1 |
| 算式 | shader `WarpforgeVFX/LUTBlender` 文件头 | `o = lerp(_LUT1, _LUT2, _Blend)` |

⇒ **原版语义**：`ColorLookup.texture` **恒指 `combinedLUT`**；`Reset` 只把 `combinedLUT` 的**内容**变成原 LUT（`_LUT2 = originalLUTTexture` + `_Blend → 1`）。
⇒ **自检该断的期望值**（三件一起，缺一不可）：

1. `ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.Combined)` —— 引用仍是合并 RT（**原版常驻形态**）；
2. `pfx.BlendNow == 1f` —— 判别信号（Reset 前是 **0.25**，见 `BattleScene.cs:11976-11981`）；读口已存在：`BattlePostFx.cs:560 public float BlendNow`；
3. **（最强、建议加）** Reset 之后 `combined` 在取样点的像素 = **原 LUT 同一像素**（±2/255）——「画面 = 原 LUT」的净结果，直接盯画出来的东西。取法：`Graphics.Blit(pfx.OriginalLut, tmpRt)` 再读 `(128,8)`（`LUT Normal` 的 `isReadable: 0`，所以别用 `Texture2D.GetPixel`）。

理由（为什么不用 `OriginalLut` 直接比）：那是**旧实现的自设计口径**，不是原版口径；而且重建前它一直是 `null==null` 的假绿。

---

## 二、完整因果链（为什么是「重建后才红」，不是「重建改坏了」）

```
2026-10-01  断言写成（当时实现 = Reset 只做 `_cl.texture.value = _original`，与断言自洽）
            git show 4b315bbf:…/BattlePostFx.cs:224-237
            但 arena1 当时 `_original == null` ⇒ 断言 null==null 空转绿（假绿 · 从没真验过）
2026-10-26  B8_AF 把 Reset 重写成原版语义（commit b1d40fe5，`ResetLutCore`）
            instant 支新增 `_LUT1=_LUT2=_original; BlendTo(1f)` ⇒ texture 落成 _combined
            ⚠️ 断言文案与期望值**没跟着改**（仍写「交还给进场时那张」）
            仍然绿 —— 因为 arena1 的 `_original` 还是 null（降级支，不走 BlendTo）
2026-11-01  B114_W14 + 13 场重建：battlearena1 的 profile 补上 `ColorLookup`(`LUT Normal`)
            ⇒ `_original` 从 null 变成非 null ⇒ 首次走到 `:505 BlendTo(1f)`
            ⇒ texture = _combined ≠ _original ⇒ **红**
```

⇒ **根因 = 断言在 10-26 那次实现改写时没有同步更新**；重建只是撤掉了它的遮羞布。

---

## 三、重建前后这条断言的状态（对照）

| | 重建前 `b114_unity_battlescene.log` | 重建后 `b114_unity_battlescene2.log` |
|---|---|---|
| arena1 的 attach 日志 | `:34639` `ColorLookup` 补出来 **1** 次 | `:34638` 补出来 **0** 次 |
| `_original`（进场快照） | `null` | `LUT Normal`（`battlearena1_PostFx.asset:16-18` 的 `m_Value` guid = `aa44b50c…`） |
| Reset 是否走 `BlendTo(1f)` | 否（`_original == null`） | **是** |
| Reset 后 `texture.value` | `null` | `_combined` |
| 这条断言 | **✓（`null==null` 空转）** | **✗** |
| 相邻的 A243「13 场 `ColorLookup` 对得上」 | ✗（7 场接上 / 不对的 6 场 · `:248521`） | ✓（13 场 / 不对的 0 场 · `:248420`） |
| 合计 | 8,816 ✓ / 13 ✗（`:295400`） | 8,846 ✓ / 1 ✗（`:295587`） |

（`✗` 字面出现 4 次，其中 3 次在 ✓ 的文案里 ⇒ 真实失败 **1** 条。）

---

## 四、该怎么修（推荐：**改断言**，⛔ 不改实现）

### 4.1 推荐补丁（`Editor/BattleScene.cs:11988-11991` 整段替换）

```csharp
                    // Reset（原版 `PostFXController.ResetLUT(instant)`）：`texture` **仍指合并 RT**，
                    //   变的是它的**内容** —— `SetTargetLUT(originalLUTTexture)` + `_Blend → 1`
                    //   ⇒ `combined = lerp(a, 原 LUT, 1) = 原 LUT`（判据：`decomp_full/LUTBlender__Awake.c`
                    //   只在 Awake 把 `ColorLookup.texture` 指到 combined；`__DoBlend.c` / `__ResetLUT.c` 都不改引用）。
                    //   ⚠️ 2026-11-01 `B114_R10`：这条以前写「交还给进场那张」是 **10-26 之前的实现口径**，
                    //      而且重建前在 arena1 上是 `null == null` 空转绿 ⇒ 先钉死前提。
                    Check(pfx.OriginalLut != null,
                          "（前提）进场时 `ColorLookup.texture` 非空 ⇒ 下面那条才有意义（否则 null==null 假绿）");
                    float blendBefore = pfx.BlendNow;          // 上面刚 BlendTo(0.25) ⇒ 期望 0.25
                    pfx.Handle(new WarpforgeVFX.PostFxRequest { op = WarpforgeVFX.PostFxOp.Reset, instant = true });
                    Check(ReferenceEquals(pfx.ColorLookupRef.texture.value, pfx.Combined),
                          "★ `Reset` 之后 `ColorLookup.texture` **仍指**合并 RT（原版 `LUTBlender.Awake` 起的常驻形态）");
                    Check(blendBefore < 0.99f && Mathf.Approximately(pfx.BlendNow, 1f),
                          $"★ 而且 `_Blend` 落到了 1（Reset 前 {blendBefore:F2}）⇒ `lerp(_LUT1,_LUT2,1)` = 原 LUT —— "
                        + "「引用没变」还不够，这一条才排得掉「Reset 什么都没做」");
                    // 净结果：合并 RT 的内容 = 原 LUT（⛔ 别只比引用：`Combined` 是同一张 RT，比它恒真）
                    var O = ReadTexPixel(pfx.OriginalLut, 128, 8);
                    var R = ReadRtPixel(pfx.Combined, 128, 8);
                    Check(Mathf.Abs(R.r - O.r) + Mathf.Abs(R.g - O.g) + Mathf.Abs(R.b - O.b) <= 0.02f,
                          $"★ `Reset` 之后合并 RT 的内容 = 原 LUT（R={R.r:F3},{R.g:F3},{R.b:F3} "
                        + $"O={O.r:F3},{O.g:F3},{O.b:F3}）—— 这才是「画面回到原 LUT」的净结果");
```

配套小工具（`BattleScene.cs` 已有 `ReadRtPixel`，加一个同族的）：

```csharp
    /// <summary>把任意 `Texture` 拷进临时 RT 再读一个像素（LUT 资产 `isReadable: 0` ⇒ 不能 `GetPixels`）。</summary>
    static Color ReadTexPixel(Texture t, int x, int y)
    {
        if (t == null) return default(Color);
        var rt = RenderTexture.GetTemporary(t.width, t.height, 0, RenderTextureFormat.ARGB32);
        Graphics.Blit(t, rt);
        var c = ReadRtPixel(rt, x, y);
        RenderTexture.ReleaseTemporary(rt);
        return c;
    }
```

⚠️ 容差说明：项目 `ProjectSettings.asset:50 m_ActiveColorSpace: 0` = **Gamma** ⇒ `ReadPixels` / `GetPixel` 无 sRGB 转换，理论可逐值相等；**0.02（≈5/255）是保守档**，实测若更紧可收。**未跑 Unity，容差没实测**（红线），请在同步点按实测收紧/放宽。

**灭自证检查**：上面三条里，去掉 `BlendNow == 1` 只留「`texture == Combined`」= **恒真**（Reset 前 `:11985-11987` 刚验过同一个引用，且 `BlendTo` 只写 `_combined`）—— 那等于什么都没验。像素那条是**结构上**排掉「Reset 空转」的唯一一条。

### 4.2 为什么不改实现（另一条路，**不推荐**）

若把 `:505 BlendTo(1f)` 删掉 / 改成把 `_cl.texture.value = _original` 挪到 `BlendTo` 之后，断言立刻变绿。但：

1. **它违背原版**：原版 `ColorLookup.texture` **恒指 `combinedLUT`**（`LUTBlender__Awake.c` 是唯一写点），Reset 不改引用 —— 改实现等于为了迁就一句错断言，把一个**已经复刻对了**的点改回自设计口径（铁律 18·0：默认复刻原版）。
2. **它会引入脆弱中间态**：模块空闲时每帧都会来一发 `BlendTo(0)`（`LUTBlend` 的降级入口），下一帧就会把 `texture` 重新指回 `_combined`；「Reset 后指静态 LUT」只能维持不到一帧 —— 断言会变成「验一个瞬时态」。
3. `:503-505` 那三句是 10-26 特意加的（注释写着「否则会把刚复位的结果又掀回去」），删掉会把那个真问题**请回来**。
4. `Detach`（`:382-386`）那条「交还静态 LUT 再销毁 RT」才是**必须**保留的（否则 `ColorLookup.texture` 会指一张已销毁的 RT）——别把两处混为一谈。

### 4.3 同时要订正的三处**文字**（铁律 5：就地改，⛔ 别另开一份说法）

| 位置 | 现文 | 该改成 |
|---|---|---|
| `Editor/BattleScene.cs:11988` | `// Reset：交还给**进场时那张静态 LUT**（原版 PostFXController.ResetLUT）` | 「Reset：`combined` 的**内容**变成进场那张静态 LUT（引用不变，原版 `LUTBlender.Awake` 起恒指 `combinedLUT`）」 |
| `Battle/BattlePostFx.cs:498` | `_cl.texture.value = _original;      // 原版走到那一步的**净结果**也是这张` | 「降级支才停在这张；下面 `BlendTo(1f)` 会把它改成 `_combined`（内容 = `_original`）—— 原版 `texture` 恒指 `combined`」 |
| `报告/B114_W41_A243与聚合行自检.md:182` | 「`Editor/BattleScene.cs:11988-11991` 那条…**仍然成立**」 | 「**不成立**（`B114_R10` 现核）：instant 支末尾 `BlendTo(1f)` 会把 `texture.value` 覆盖成 `_combined`」 |

---

## 五、没查清的部分（如实写）

1. **没跑 Unity**（红线）⇒ 上面「`texture.value == _combined`」是**代码走查**结论（三处代码行 + 原版反编译两边一致 + 日志方向一致），**没有现场打印 `pfx.OriginalLut.name` / `pfx.BlendNow`**。
   若同步点想先确认再做，最小改动是**只加两条 Log 打印**再跑一次 `BattleScene.Run`（但那属于写手活）。
2. **像素比对的容差没实测**：项目是 Gamma（`m_ActiveColorSpace: 0`），理论上 `Graphics.Blit` + `ReadPixels` 与 shader 内 `tex2D` 走同一采样路径 ⇒ 可能逐值相等；但 `LUT Normal` 的 `mipMapMode: 0` / `enableMipMap: 1`、`filterMode: 1`（bilinear）我在**非整数 UV** 下没有实测。**(128,8) 在 256×16 上正好是网心外的半个 texel 位置，bilinear 会混两个纹素** —— 两侧都混同样的两个 ⇒ 应当一致；**建议改用整数边界 + 0.5 的采样点或直接加大容差**，⛔ 别用 `(0,0)`（边角可能受 wrap/border 影响）。
3. **`LUTBlender__Awake` 那次写 `ColorLookup.texture` 的虚调用**（vtable `+0x228`，实参 `combined`，方法信息 `+0x230`）我判定为「把 `texture.value` 设成 `combined`」。它也可能是 `VolumeParameter.Override(combined)`（两者都写 `value`）—— **结论不受影响**，但严格说这一层的**是否顺带置 `overrideState`** 我没坐实。
4. **另外 12 个战场没有运行时 Reset 覆盖**：`BattleScene.Run` 只打 `battlearena1`（日志 `b114_unity_battlescene.log:34626` / `…2.log:34625`「§27 战场：`battlearena1`」）。A243 那条只查**静态 profile 数据**（13/13 贴对名字），**不查运行时 Reset 行为** ⇒ 若将来某场 `_original` 又是 null（老 prefab），这条修好后的断言会靠前提那条 `Check(pfx.OriginalLut != null)` **红出来**（这正是我要的：⛔ 不许再 null==null 假绿）。

---

## 六、顺手发现（⛔ 一条都没改）

1. **`BattlePostFx.cs` 文件头第 ② 条（`:28-42`）已经过期**：它写「**6 个战场的 profile 没有 `ColorLookup` 槽**…我们构建时没给这 6 场建那个槽…在运行时那份 profile 副本上补一个」——
   重建后 `arenas/*/Profiles/*_PostFx.asset` **13/13 都有**（`battlearena1_PostFx.asset:14-19` 现读），`MissingColorLookup` 恒 0。
   ⇒ 该按「⚠️ 2026-11-01 更正：B114_W14 起构建时就建槽并回退贴 `LUT Normal`；`Add<ColorLookup>` 那条支只剩老 prefab 会走到」就地改写。**（`B114_W14` §七-1 与 `B114_W41` §五-3 都已记过这条，至今没改。）**
2. **`BattlePostFx.cs:498` 的注释与代码行为不符**（同上 §四-3）—— 下个会话很容易照着它把 `BlendTo(1f)` 当成冗余删掉（那会把 10-26 修的真问题请回来）。
3. **`Attach` 没有做原版 `Awake` 的两步**：原版 `Awake` 会 `Blit(originalLUTTexture → combinedLUT)` 并**立刻**把 `ColorLookup.texture` 指到 `combinedLUT`；我们 `Attach`（`:192` 之后到 `:200`）两件都没做 ⇒ **第一个 `BlendTo` 之前 `ColorLookup.texture` 还指着静态资产**。
   净视觉一致（`combined` 未初始化时用不用它看不出），但**引用形态与原版不同**，且这正是「Reset 到底该 `_original` 还是 `_combined`」这类口径混乱的温床。⇒ 记一条账：**要不要补 `Attach` 的初始 Blit + 指 `combined`**（补了之后 `Detach` 里那句「交还静态 LUT」仍然必要，别删）。
4. **`B114_W41:182` 的预测要订正**（见 §〇末）—— 它现在是一份「写着仍然成立、实际已经红了」的记录，正属于铁律 5 那种会误导下个会话的东西。
5. **这条断言的覆盖面很窄**：它只在 `battlearena1`（6 场「共享恒等」档之一）上跑。重建**之前**它测的是 **null 路径**，重建**之后**才第一次测到**非 null 路径** —— 也就是说：**「`_original` 非 null 时 Reset 会怎样」这件事，直到今天才第一次被自检碰到**，而 7 场专属 LUT 的战场**从来没走过这条断言**。修断言时最好顺手把「前提非空」写成硬断言，避免再次假绿。
