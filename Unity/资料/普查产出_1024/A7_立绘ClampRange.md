# A7 · 原版立绘硬矩形 `_Clamp_Range` —— 判据 / 差在哪 / 落地

**日期**：2026-10-24 · **范围**：卡牌线 §一「🆕 原版立绘的硬矩形 `_Clamp_Range`（UV 中间 64% 宽）未复刻」。
**一句话**：真值已从**反编译方法体 + 二进制常量 + shader 片元 + 材质**四路坐实；它是**贴图 UV** 的硬矩形，
两族 sprite rect 换算到我们的 PNG 后**数字不同**（单位卡小图档两端只各切 ~1.2%/1.1%，**不是**「中间 64%」）；
它只挂在一个地方 —— 原版 `CardImage`（材质 `Card Image Overflow`）⇒ 我们把它**只套 `_artFront`**（同一层语义），
小图态实测等价、只在大图档有可见效果。

---

## 一、判据（一手，值 + 出处）

### 1·1 真值：`CardImageController` 的两个静态 `Vector4`

`d:/2/tools/decomp_full/CardImageController__.cctor.c`（静态构造）把**两个 16 字节**写进自己的静态字段
（`+0x00 = SMALL`、`+0x10 = BIG`）：

```c
puVar1 = *(undefined8 **)(DAT_1842d47f8 + 0xb8);   // 静态区
*puVar1        = _DAT_1834b33d0;   puVar1[1] = _UNK_1834b33d8;      // → +0x00..0x0f
*(lVar2+0x10)  = _DAT_1834b33e0;   *(lVar2+0x18) = _UNK_1834b33e8; // → +0x10..0x1f
```

字节用 `工具/read_literal.py` 现读（`D:/2/unity_run_ref/GameAssembly.dll`，4 字节 float 档 —— 上下文就是 `Vector4`）：

| 常量 | 地址 | 值（x, y, z, w） |
|---|---|---|
| `CARD_IMAGE_CLAMP_RANGE_WHEN_SMALL` | `0x1834b33d0/d4/d8/dc` | **(0.81, 1, 0.18, 0)** |
| `CARD_IMAGE_CLAMP_RANGE_WHEN_BIG` | `0x1834b33e0/e4/e8/ec` | **(0.75, 0.89, 0.25, 0.1)** |

字段名见签名桩 `D:/2/Warpforge_code/Scripts/Assembly-CSharp/CardImageController.cs:13-15`。
`CardImageController__ChangeState.c` 的 small 分支读静态 `+0x0/4/8/c`、big 分支读 `+0x10/14/18/1c`
（`:71-74` / `:102-105`），与上表一一对应。

### 1·2 作用在哪一层、哪个属性

- `CardImageController__.ctor.c`：`materialOffsetAndScaleID = Shader.PropertyToID(<字符串>)`；
  那个字符串的 VA 是 `0x1842c5010`，`d:/2/tools/il2cpp_out/script.json` 的 `ScriptString` 里
  **`0x42c5010 = "_Clamp_Range"`**（RVA = VA − ImageBase，逐字节对上）。
- `CardImageController__DoChange.c`：instant 那一支 `Material.SetVector(cardImage.material, 该 ID, targetClamp)`
  （`:74-80`）；非 instant 走 `DOColor(material, 同一 ID, …)` 补间。
- **只作用于 `cardImage` 那颗 Image**（`CardImageController` 挂在 `CardImage` GameObject 上，
  `bundle_battleprefabs_vfxandmisc_assets_all/GameObject/CardImage.json` 的 4 个组件 =
  RectTransform / CanvasRenderer / Image / CardImageController）。
- 材质 = **`Card Image Overflow`**（`CardImage.json` 的 Image `m_Material` →
  `Material_-8301844802436097929.json`），shader = `Everguild/Card ImageUI`
  （`bundle_shaders_assets_all/Shader/Shader_-2318977921299023320.json`，`m_ParsedForm.m_Name`）。

### 1·3 它到底怎么裁：shader 片元（四变体逐字相同）

`D:/2/tools/ruri_out_1020/ExportedProject/Assets/b561210ac1934488e9c22d3d1c8e18d5/Sub0_Pass0_Fragment_b7.hlsl:41`：

```hlsl
SV_Target.w = mad(COLOR.w, clamp( mad(
    ((_Clamp_Range.z >= TEXCOORD.x) || (!(_Clamp_Range.x >= TEXCOORD.x))) ? 0.0 : 1.0,
    ((!(_Clamp_Range.y >= TEXCOORD.y)) || (_Clamp_Range.w >= TEXCOORD.y)) ? 0.0 : 1.0,
    _65.w), 0,1), _TextureSampleAdd.w);
```

⇒ **保留当且仅当** `z < uv.x ≤ x` **且** `w < uv.y ≤ y`；其余整块 `α=0`（按轮廓/渐变都不是）。
`b8`（`UNITY_UI_ALPHACLIP`）/`b9`（`UNITY_UI_CLIP_RECT`）/`b10`（两者）**同一条**。
`uv` = 顶点阶段 `mad(TEXCOORD.x, _MainTex_ST.x, _MainTex_ST.z)`（`Sub0_Pass0_Vertex_b1.hlsl:88-89`），
而 `Card Image Overflow.mat` 的 `_MainTex` scale=(1,1) offset=(0,0) ⇒ **就是 sprite 的贴图 UV**。

### 1·4 两个「小图值」+ 状态（铁律 5·c：一个值 ≠ 全部情况）

| 场景 | `_Clamp_Range` | 依据 |
|---|---|---|
| 出厂 / 产品路径小图（`ShowBattleCard` → `ChangeSize(Small, instant)`） | **材质序列化默认 (0.82, 1, 0.18, 0)** | `Card Image Overflow.mat` 的 `_Clamp_Range`；`ChangeState` 第一句 `if (currentState == state) return;`（`ChangeState.c:20-23`）且 `currentState` 出厂 = Small(0) ⇒ **提前 return、不写材质** |
| Big（`ChangeState(1)`） | cctor BIG **(0.75, 0.89, 0.25, 0.1)** | `ChangeState.c:99-105` → `DoChange` |
| Big→Small 之后 | cctor SMALL **(0.81, 1, 0.18, 0)** | 同上 small 分支 |

两档小图值差 1% 图宽（≈10 纹素），落点都在框的金属/框外 ⇒ 画面零差别；实现取**产品实际值 0.82**。
⚠️ Big 在本 build **只有调试组件 `CardImageControllerTester`** 会调到（K1 §六·1）；产品可见的「大图档」是我们
`BigCardOverdraw` 那套（详情窗）。

### 1·5 贴图 UV ≠ sprite 局部 UV（本条最容易被读错的地方）

原版卡图贴图统一 **1024²**，但 sprite rect 有两族（`assets_full` 13 个 `*cardassets*` 包里
**1207 个** 卡图 Sprite 实测）：

| sprite rect（`m_RD.textureRect`） | 张数 | 是谁 |
|---|---|---|
| `x=176.5, y=0, w=670.5, h=1024` | **657** | 单位 / 督军（`*_inf_*` / `*_war_*`） |
| `x=0, y=0, w=1024, h=1024` | **548** | 战术 / 进攻 / 防御（`*_strat_*` / `*_offensive_*` / `*_defence_*`） |
| `x=176.66, y=0.25, w=670.17, h=1023.5` | 1 | 杂项 |
| `x=0, y=0, w=512, h=512` | 1 | 杂项 |

⇒ 贴图 UV 的小图档窗口（产品值 `(0.18,0.82]`，cctor 值 `(0.18,0.81]`）换到**单位卡**上 =
图宽两端各切 **1.2% / 1.1%**（cctor 的 0.81 则是 1.2% / 2.6%）；换到**战术卡**上才是真 `(0.18,0.82]`。
（我们的 `art_<id>.png` 就是按 `m_RD.textureRect` 裁的，见 `工具/import_original_art.py` 的 `write_portrait`；
现读 UM78 = **671×1024**、UM75 = **1024×1024**。）

**换算（实现里的 `CardView.ArtClampUv01`）**：`spanU = w/1024`、`uMin = (1 − spanU)/2`（rect 水平居中：
176.5 vs (1024−670.5)/2 = 176.75，差 0.25 px），`u_local = (u_tex − uMin)/spanU`；v 同理（rect 满高 ⇒ v 原样）。

| | 单位卡局部 UV | 战术卡局部 UV |
|---|---|---|
| SMALL (0.82,1,0.18,0) | **u (0.0117, 0.9883]** · v (0,1] | **u (0.18,0.82]** · v (0,1] |
| （cctor 的 0.81 那一档） | u (0.0117, 0.9731] | u (0.18,0.81] |
| BIG (0.75,0.89,0.25,0.1) | **u (0.1185, 0.8820]** · v (0.10,0.89] | **u (0.25,0.75]** · v (0.10,0.89] |

---

## 二、差在哪（原版硬矩形 vs 我们的「条带贴合」）

### 2·1 大图态（**唯一有可见效果**的一档）

`_artFront`（我们的「角色抠图压框」层，×1.27）原来只夹到**卡体矩形**：

| | 我们的（改前） | 原版硬矩形（BIG） | 差 |
|---|---|---|---|
| x | ±1.046（卡体半宽） | **±0.907** | 左右各 **0.14 卡单位** ≈ **42 px**（300 ppu）|
| y | −1.666 … +1.666 | **−1.496 … +1.372** | 下 **0.17** · 上 **0.29** 卡单位 ≈ 51 / 88 px |

改后 `_artFront` 的 UV 窗口 = `(0.1185,0.8820] × (0.10,0.89]`（与上表一致），几何按同一份板位反算
（**裁画面、不拉伸**）。

### 2·2 小图态：生产路径**零差**；opt-in 前景层会被切（= 原版的画法）

- 生产路径（`UseFrontLayer=false`）**根本不建** `_artFront` ⇒ 硬矩形对小卡渲染**一个像素都不动**（断言 ④a 实测）。
- 若临时开 `UseFrontLayer`（opt-in，出厂关），单位卡贴图的 alpha 会伸到局部 `u 0.006…1.049`
  （实测 UM78 的 alpha>0 范围），**超过**小图档窗口 `(0.0117,0.9883]` ⇒ 硬矩形**真的切掉一条**——
  这正是原版对 `CardImage` 做的事（④b 断「切了」且「只切在矩形外」）。
- ⚠️ **「小图态完全等价」只对生产路径成立**（不建那层）。按真 sprite rect 重算 104 张卡框：
  开口常规 `±0.75`、最宽（装饰缺槽）到 `±1.05`；单位卡小窗口边界 `±0.915` ⇒
  **25/104 张框**（Emp.Children 全档 · AstraMilitarum tier2-4 · GSC tier3/4 · Sautekh tier4 ·
  BlackLegion tier4 · DarkAngels tier4 …）的开口能伸到窗口外。⇒ 若把硬矩形套到「可见插图」那一层
  （我们的 `_art` 底层），这些卡的小图会**真的变**，而我们**没有**判据说原版那一层也被 clamp（见 §五·1）。

---

## 三、改动清单

### 3·1 `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs`（+173 / −10）

1. **常量块**（`ArtCoverMargin` 之后）：`ClampRangeSmall=(0.82,1,0.18,0)` · `ClampRangeBig=(0.75,0.89,0.25,0.1)` ·
   `ArtSourceTexPx=1024` · `public static bool DebugDisableArtClamp`（只给 A/B 断言）。
   doc 写全：shader 公式 · cctor 真值 + 地址 · PropertyToID 字符串出处 · 两族 sprite rect · 两个小图值 · 状态可达性。
2. **`static bool ArtClampUv01(Texture2D, bool big, out Vector4)`**：贴图 UV → 我们 PNG 的局部 UV。
   只覆盖实测两族（`height 1000..1032 && width 500..1032`）——占位图 128×179 与杂项 512² **不套**（不猜）。
3. **`ArtMeshInFrame(..., int clampKind = 0)`**（0 不套 / 1 小图 / 2 大图）：
   算出 `clampCard`（卡面坐标矩形）→ 与「卡体矩形 / 卡框开口」**取交集**（裁画面不拉伸）；
   缓存键加一档，乘数用**奇数 397**（键早已 `unchecked` 回绕，10 会丢低位 ⇒ 可能撞格）。
   `DebugDisableArtClamp` 在入口折算成 0 **再进键**（否则关掉时算的网格会污染开着的键）。
4. **接线**：
   - `_artFront`（×1.27，大图档）→ `clampKind: 2`；
   - 小卡 opt-in 前景层 → `clampKind: 1`（懒建，只在真要建那一层时才算）；
   - 底层 `_art`（`ArtOpaque`）→ **不套**（理由见 §五·1 / 代码注释）。
5. **自检钩子**：`public static bool ArtClampWindowForTest(art, big, out Vector4)`、
   `public static Mesh ArtMeshForTest(art, artScale, frameTex, clipWindow, clampKind)`（与 `Build` 同一缓存键）。
6. **顺手订正（调度台派发 · 铁律 5）**：`TweenPose` 那段的 4 处 `CardTween.ToPose` 注释 ——
   原文「菜单版详情窗 `Shell/CardDetailPopup` 仍是 `CardTween.ToPose`」**已不成立**
   （`A11` 已把那两处换成 `CardView.TweenPose`，且 `CardTween.ToPose` 已整段删除、全仓零调用方，
   退役注在 `Core/CardTween.cs:62`）。已逐处加「2026-10-24 `A11` 已删 / 已不成立」更正痕；
   ⛔ 没动 `Hand/*` 里拿 `ToPose` 当「Sequence 只能传 Linear」知识对照的注释（那几条仍成立）。

### 3·2 `Unity/MyGame/Assets/CardPresentation/Editor/CardFaceProbe.cs`（+230 / −0）

`CheckLayersBody` 末尾新增 `CheckArtClampRange(pool)`，共 **17 条**断言：

| # | 断言 | 怎么才会红 |
|---|---|---|
| 前提 ×2 | 样本立绘 = 原版两族尺寸（UM78 671×1024 · UM75 1024²） | 美术/清单不在 |
| ② ×6 | `ArtClampWindowForTest` 的窗口 = **常数**（单位卡小 0.0117/0.9883、大 0.1185/0.8820/0.10/0.89；战术卡小 0.18/0.82、大 0.25/0.75/0.10/0.89） | 把贴图 UV 当局部 UV（⇒ 单位卡变 0.18/0.81）· 两档用同一个值 |
| ③ ×5 | **真网格**（`ArtMeshForTest`，与 `Build` 同键）的 UV = 原版窗口；关档窗口**严格更大**（灭自证）；关档反解板位能**预测**开档顶点（裁画面≠拉伸） | 常量写了但没接到网格 / clamp 被删 / 只改 UV 不裁几何 |
| ④a ×2 | 生产小卡（单位 / 战术）硬矩形开/关 **逐像素 = 0 差** | 把 `clampKind` 接到 `_art`（战术卡必红） |
| ④b ×2 | opt-in 前景层：开/关差集 **> 20 px**，且**全部落在硬矩形之外**（矩内 0 点） | clamp 没接上（差集 0）/ 窗口算错（矩内有改动）|

④ 用现成的 `RenderPixels()`（与 `CheckOutlineGlow` 同一套相机口径）；每次建卡都在 `finally` 里
`DestroyImmediate` + 还原 `UseFrontLayer` / `DebugDisableArtClamp`。

---

## 四、验收怎么跑

- **宿主**：`CardBaseDemo.Run`（在 `工具/_run_8_checks.sh` 里，第 41 行；它 `:322` 会调
  `CardFaceProbe.CheckLayers()`）⇒ 新增的 17 条**真会被跑到**。
- **期望合计**：`CardFaceProbe` 的 `=== 合计：N 通过 / M 失败 ===` = **93 + 17 = 110 / 0**
  （93 是简报给的基线，⛔ 未实跑）。`CardBaseDemo` 自己那套 `_pass/_fail` 是**另一个类的静态字段**
  ⇒ 它的合计**不变**（≈326）。
- **本代理没跑 Unity**（红线）：只跑了秒级类型检查 ——
  `TMPDIR=/tmp/wf_a7 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 错 / 编辑器 0 错**。
  （中途一次「编辑器 1 错」出在 `Assets/RuleEngine/Editor/RuleEngineTest_EngineB3.cs` —— 别的代理正在写的
  文件，**非本代理**；隔 45 s 重跑已是 0。）
- 行尾：`git diff --numstat` = CardView **+173/−10**、CardFaceProbe **+230/−0** —— 没有整篇翻行尾。

---

## 五、没查清 / 没做（如实）

1. **原版「背景那半幅插图」由谁画、受不受同一硬矩形 —— 判不了。** 本地**只有一颗** art 材质
   （`Card Image Overflow`，用它的是唯一一颗 `Everguild/Card ImageUI`）；而单位卡贴图的 alpha 是
   「角色/溢出抠图」（UM78：97% α=0，alpha>0 只占 1%）。我们的 `ArtOpaque` 底层是**自造的近似**
   （忽略 alpha 画整幅），原版**没有**与之对应的、被 clamp 的层 ⇒ **没套**硬矩形上去。
   需要：原版实拍/远端 bundle 里的另一张卡面 art，才能判这一层。
2. **BIG 那档在本 build 的产品路径不可达**（只有 `CardImageControllerTester`）—— 已按铁律 11 照做
   （接在 `_artFront` 上，即详情窗大图那档），但「原版大图态到底长什么样」仍欠**真机并排**（清单已有此账）。
3. **战术/防御立绘的缩放/偏移**：原版 `cardScales` 对 Tactic=20 → **0.61**、防御 → **0.60**，
   并加 `tacticsOffsetPosition (0, 0.5)`（`CardScalesByCardType.json` + `SetImage.c:127-150`）；
   我们是**全尺寸 + 裁到卡框开口**。这是**既有**偏离（W61 那套条带方案的取舍），与本条无关，未动 —— 记在 §六。
4. **本次没跑 Unity**（红线）⇒ 上面「期望 110/0」是**预计**，不是实测。

---

## 六、顺手发现（⚠️ 未改，只记账）

1. **战术/防御卡的大图档我们多乘了 1.27**：原版 `SetImage` 的 `fVar12 = bigSizeMultiplier` **只在
   `cardTypeOptions == 0(Minion) || == 10(Hero)`** 时取（~~`SetImage.c:100-104`~~ **`:110-111`**（D3 现核，原行号偏了 10 行）的
   `if (currentState==1 && (param_3==10 || param_3==0)) fVar12 = *(this+0x20)`），
   而我们的 `artScale = (_bigImage && BigCardOverdraw) ? 1.27 : 1` **对所有类型**都乘
   （Tactic=20 / Defence=5 也放大）⇒ 战术/防御卡的大图档比原版**大 1.27 倍**。
   🔴 **2026-10-24 补注（铁律 5 · D3 现核）**：`ChangeState.c:90-95`（Small→Big 正常路径）**并不判卡类型**、无条件乘 `bigSizeMultiplier`；「只有 Minion/Hero 得 ×1.27」这条是**两条事实合起来**才成立的 —— ① 只有 `useOverDraw==1` 的卡进 Big 态；② 卡池现读 `useOverDraw=1` 的正好只有 **unit 585/586 + hero 56/56**（tactic 446 + defence 39 全 0）。已按此门控（`CardView.BigArtScaleApplies` = `d.isUnit`，即原版 `IsUnit` 镜像），并配 4 条断言（`CardFaceProbe` 110→**114**）。
   改它会动观感、要并排核，**本笔未动**。
2. **原版战术/防御立绘另走 `cardScales` 0.61/0.60 + y+0.5**（同 §五·3）—— 与我们的条带方案不同构图。
3. **K1 §五·2 那句「UV 中间 64% 宽」是把贴图 UV 当 sprite 局部 UV 的读法**：按真 sprite rect 换算，
   单位卡小图档两端只各切 **1.2%/1.1%**（cctor 的 0.81 是 1.2%/2.6%），大图档才切 ~24%。本文档已按此订正（铁律 5）。
4. `Card Image Overflow.mat` 里的 `_AlphaCutout 0.21` / `_Clamp_range (0.2,0.8,0,0)` / `_ShadowColor` /
   `_SmoothStep` 都是**换 shader 前的残留死值**（CLAUDE.md §三「原版材质上带着旧 shader 残留值」同族）——
   `Everguild/Card ImageUI` 的属性表里没有它们，⛔ 别当活值读。
