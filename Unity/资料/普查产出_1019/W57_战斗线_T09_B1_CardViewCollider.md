# W57 战斗线 · T09 步骤 B 前半（B1）—— `CardView` 真 collider

> 执行代理：T09-B1 · 2026-10-19 · 白名单只有 `Core/CardView.cs`（实际只改了它一个文件）
> 口径 = **只做不验**：没跑 Unity / 没起 player / 没跑任何 `*.Run`；只跑了秒级 typecheck。
> 判据来源：`资料/普查产出_1019/W57_战斗线_T09迁移计划.md` §「步骤 B · 第 5 步」+ §五 第二批。

---

## 一、做了什么（落点 = `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs`）

| # | 内容 | 落点（当前行号） |
| --- | --- | --- |
| 1 | 新常量 `CardHitLayer = 5`（原版层判据，**只记录、不赋层**） | `:5516` |
| 2 | 新属性 `CardHitLayerMask`（`Physics2D.DefaultRaycastLayers`） | `:5526` |
| 3 | 新字段 `_hitCollider`（主面那颗 `BoxCollider2D`） | `:5531` |
| 4 | 查询缓冲 `_hitQueryBuf`（64 颗）· 过滤器 `_hitFilter`（`ContactFilter2D`，非触发器） | `:5536` / `:5542` / `:5544` |
| 5 | `EnsureHitCollider()` —— 在 `_tiltRoot`（原版 `2DCard` 那一级）上建/复用 `BoxCollider2D` 并刷档 | `:5555` |
| 6 | `SyncHitCollider()` —— 把 `HitRect()` 那一档写进 `size/offset`（**判据只有 `HitRect` 一处**） | `:5570` |
| 7 | **出生入口**：`Build` 里 `EnsureHitCollider()`（在 `_tiltRoot` 建好之后、不在 `if (_tiltRoot == null)` 里 ⇒ Build 重入也保证有） | `:2712` |
| 8 | **换形态入口**：`SetFace` 里 `SyncHitCollider()`（铁律 10 第 5 条：两个入口各显式刷一次） | `:1477` |
| 9 | `HitByCollider(Vector3 world)` —— `Physics2D.SyncTransforms()` → `Physics2D.OverlapPoint(point, filter, buffer)` → **逐颗比对是不是本卡那一颗** | `:5670` |
| 10 | Editor-only 只读探针（`#if UNITY_EDITOR`）：`ColliderForTest` / `ColliderSizeForTest` / `ColliderOffsetForTest` / `ColliderLayerForTest` | `:5704` / `:5708` / `:5711` / `:5715` |
| 11 | `_hitColliderWarned`（与 `_hitFrameWarned` 分开的「只出声一次」闩） | `:5609` |

**没做的（按本波边界）**：没动 `Hand/CardInteraction.cs` / `Battle/MulliganPanel.cs` / `Battle/BattleDriver.cs`（W2）；
没动 `Editor/**`；`Contains` / `ContainsNear` / `ContainsUiCollider` **一个字没改**（它们是一致性断言的对照物）。
**没建第二面**（`1.25 × 2.53 @ (0,−2.62)`，悬停放大那第三档）—— 与计划一致，留给后续。

---

## 二、collider 尺寸 / 偏移 / 层号来源

### 2·1 尺寸与偏移（逐值）

| 档 | `size` | `offset` | 判据 |
| --- | --- | --- |---|
| 常态（`Full` / `Board`） | **1.97 × 3.02** | **(0, 0)** | 原版 `2DCard` 主面 `BoxCollider2D_7520353667969031104.json` 的 `m_Size` / `m_Offset`（本次现读原件核对：`m_Size = (1.9700000286, 3.0199999809)`、`m_Offset = (0,0)`） |
| in-hand（`CardFace.Hand`） | **1.97 × 3.0502** | **(0, −0.0151)** | 原版 `CardCollider2D__SetColliderInHand.c`：`size.y ×= colliderHeightMultiplierInHand (1.01)`；`offset.y = origOffsetY − (newH − origH) × 0.5`。本次**重新核了两处**：① 常数 `0.5` = `GameAssembly.dll` 里 `DAT_1834b2bb4`，按 PE 节表换算 RVA `0x34b2bb4` → 文件偏移 `0x34b07b4` → `0000003f` = **0.5f**；② 公式与 `CardCollider2D__SetCollidersShowBig.c` 同一套 |

实现上**没有另写一份数字**：`SyncHitCollider()` 直接调 `HitRect(out halfH, out centerDy)`，
`size = (HitHalfW*2, halfH*2)`、`offset = (0, centerDy)` ⇒ 与 `Contains` 共用**同一个判据函数**，
不会出现两套值漂移。

**`isTrigger = false`**：原版主面 JSON 的 `m_IsTrigger: false`（同上文件）。

### 2·2 层号（`CardHitLayer` / `CardHitLayerMask`）

- **原版判据 = 层 5（Unity 内置 `UI`）**：`d:/2/新解包资源/assets_full/bundle_battleprefabs_vfxandmisc_assets_all/GameObject/2DCard.json`
  的 `m_Layer: 5`（本次现读；同一份 JSON 的组件表里挂着那两颗 `BoxCollider2D`）。
  ⇒ 已落成 `public const int CardHitLayer = 5;` **但只作记录**。
- **⛔ 没有把 GameObject 赋到 5 层**：本仓 `SetLayer` 把整张卡**递归**设成 0（手牌/卡池/换牌）或
  `ArenaSlots.ArenaLayer = 8`（场上），两台相机的 `cullingMask` 就按这两层分
  （`Editor/BattleScene.cs:28821` / `:29200`）。把 `2DCard` 改挂 5 会连带改掉
  `EffectAnchorHand`（= `FaceRoot` = `2DCard`）的层，破坏 `BattleDriver.cs:15510` 记的不变量
  「手牌 = 层 0 / 场上 = ArenaLayer」。**没有自造新层。**
- **查询掩码 = `Physics2D.DefaultRaycastLayers`**（除 `Ignore Raycast` 外全部）：卡的层是**运行时**改的、
  出牌途中还会切层 ⇒ 写死 `(1<<0)|(1<<8)` 在换层那一帧会漏掉自己（静默）。
  误判风险已被 `HitByCollider` **逐颗比对**（只认本卡那颗 collider）挡掉。

---

## 三、一致性判据（给 T 写断言用）

### 3·1 采样点（计划 §步骤 B 断言②那套，0° 倾摆）

标架 = `_tiltRoot`（= `HitFrame()`）；用现成探针 `HitLocalToWorldForTest(x, y)` 把卡局部点换成世界点。

| 档 | 点（卡局部） | 期望 |
| --- | --- | --- |
| 常态 | `(±0.96, 0)` · `(0, ±1.46)` | 内（`Contains` 与 `HitByCollider` **都 true**） |
| 常态 | `(±1.00, 0)` · `(0, ±1.52)` | 外（**都 false**） |
| in-hand | `(±0.96, −0.0151)` · `(0, −0.0151 ± 1.48)` | 内 |
| in-hand | `(±1.00, −0.0151)` · `(0, −0.0151 ± 1.56)` | 外 |

逐点判据：`cv.HitByCollider(p) == cv.Contains(p)`（**同一个世界点、两次独立判据**）。

⚠️ **计划里写的边界点 `x=±0.985` / `y=±1.51` 正好压在 collider 边上**：`Contains` 用的是 `≤`（含边界），
而 Physics2D 在「点正好落在形状边上」时是否判命中**本波没实测**（只做不验）⇒ 断言宜把边界档写成
`±0.985 ∓ 1e-3`（内）/ `±0.985 ± 1e-3`（外），或者直接用上面表里的 `0.96 / 1.00`、`1.46 / 1.52`。
（计划里那组 0.985/1.0/1.51/1.52 的原意就是「边界内一点点 / 外一点点」，按这个取即可。）

### 3·2 断言①②③ 分别怎么落

1. **存在 + 数值**（断言①）：`ColliderForTest != null` · `!isTrigger` · `ColliderSizeForTest` /
   `ColliderOffsetForTest` 逐值等于**原版字面量**（常态 `(1.97, 3.02)/(0,0)`；in-hand `(1.97, 3.0502)/(0,−0.0151)`）。
   ⛔ 期望值别从 `HitHalfW` 反推（那是自证）。
2. **一致性**（断言②）：用 3·1 的网格，逐点比 `HitByCollider` 与 `Contains`。
3. **灭自证**（断言③）：`ColliderForTest.size *= 1.2f` → 边界外的点 `(1.00, 0)` 必须**翻成 true**
   （证明断言量的是真 collider，不是复制的常量）→ 改回来再断一次 false。
   ⚠️ 改完**必须恢复原值**（同一张卡后面还要用），恢复后立即再断一次。
4. **换档**（换形态入口）：同一张卡 `SetFace(CardFace.Board)` 前后各量一次 `ColliderSizeForTest`
   （`3.0502 → 3.02`、`offset −0.0151 → 0`），证明 `SetFace` 那个入口刷了。
5. **层**：`ColliderLayerForTest == cv.gameObject.layer`（手牌 0 / 场上 `ArenaSlots.ArenaLayer`），
   证明层跟 `SetLayer` 走、不是被常量写死。

### 3·3 断言放哪

- 宿主 = **`Editor/BattleScene.cs`**（T 独占）。
- **手牌 / 换牌档**：紧挨现有 `T03` 那组（`Editor/BattleScene.cs:18460-18510`，那里已经在用
  `IsHandFaceForTest` + `HitLocalToWorldForTest` 做 in-hand `3.0502 @ −0.0151` 的边界二分）——
  新断言的 in-hand 期望值可以直接复用那一段的 `halfH03m / dy03m` 字面量口径。
- **常态档**：紧挨 `Editor/BattleScene.cs:18051` 那组（「原版 `BoxCollider2D` 主面 1.97 宽 ⇒ 半宽 0.985」），
  以及 `:24974 / :25080 / :25108` 那几组 `Contains` 边界断言。
- **展开/展示窗**不动（`CardDisplayWindow` 走的是 `UI Collider` 那第二套，`ContainsUiCollider`，与本批无关）。

---

## 四、没查清的部分（⛔ 不是猜，是还没验）

1. **`Physics2D` 在批处理（无帧循环）下能否直接查询** —— `SyncTransforms()` + `OverlapPoint` 理论上
   不依赖模拟步进（本波只允许 typecheck，没实跑）。⇒ `BattleScene.Run` 第一次跑这批断言时**先看这一条**；
   不过就说明需要先让物理世界初始化（例如显式 `Physics2D.simulationMode` / 一帧 `await`），届时补记。
2. **正好压线的点（±0.985 / ±1.51）Physics2D 判不判命中** —— 见 3·1 的 ⚠️。断言先避开精确边界。
3. **X/Y ±10° 倾摆时的实际差异** —— 已知 `BoxCollider2D` 不跟（只认 Z 旋转），计划里写「最大 ~2.6% 卡高」，
   本波没有新的实测值。⇒ 断言在 0° / ±10° 两档各测一次时**把实测差记下来**（原版同构，不是缺陷）。
4. **零缩放卡上的 2D collider 行为**（卡被 `localScale = 0` 藏起来的情况）—— 全仓没找到这种写法
   （`Core/CardView.cs` / `Hand/` / `Board/` 都没有），但**没实测 Box2D 对退化缩放的告警**。
5. **第二面 collider**（悬停放大 = 主面 ∪ 第二面 `1.25 × 2.53 @ (0,−2.62)`）—— 仍未落，与文件头原记录一致。
6. **`CardHitLayerMask` 取全层是否引入别的 collider** —— 本仓现读**没有任何其它 `Collider2D`**
   （全仓 grep `Collider2D` 只有注释与本文件新增），但将来加 2D 物理件时要注意；
   `HitByCollider` 的逐颗比对保证本方法的正确性，只在「W2 的全局列表」那一侧可能需要再收窄。
7. **GameObject `SetActive(false)` 的卡**：`Physics2D` 不把未激活 GO 上的 collider 放进查询结果，
   而 `Contains`（手算）仍会返回 true ⇒ 两者在「卡被整体隐藏」时**语义不同**（这是 Unity 的标准行为，
   也是真 collider 路线的预期：藏起来的卡本来就不该可点）。本仓现读**没有**对 `CardView` 根
   `SetActive(false)` 之后还做命中判定的路径（`Show()` 只关层、不关根）⇒ 先记着，不另做兼容。

---

## 五、typecheck 结果

```text
TMPDIR=/tmp/wf_t09b1 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 ---
运行时错误数: 0
--- 编辑器程序集 ---
编辑器错误数: 0
```

- 行尾：`CardView.cs` 改后 **CRLF 8055 / LF 8055**（一致，没翻）。
- `git diff --numstat`：**176 行新增 / 0 行删除**（只改了这一个文件）。
- 确认 `UNITY_EDITOR` 在运行时程序集的 `-define:` 里（`/tmp/wf_t09b1/wf_csc.rsp`），
  所以 Editor 探针在编辑器编译下**存在**，T 的断言能直接引用。

---

## 六、给 W2 / 主对话的一句话交接

- W2 接 `CardInteraction.Frontmost` / `MulliganPanel.CardAt` / `BattleDriver.HitSlot` 时：
  用 `CardHitLayerMask` + `Physics2D.SyncTransforms()`（**每帧一次**，别每卡一次）+
  按 `cv.transform.position.z` 取最小者；单卡判定直接用 `cv.HitByCollider(world)`。
- **`Physics2D` 没有 z**：重叠卡一定都要自己排 z，别依赖查询顺序。
- 本批**没跑任何自检**（按「只做不验」口径）。
