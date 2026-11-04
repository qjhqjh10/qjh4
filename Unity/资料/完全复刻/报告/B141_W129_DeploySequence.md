# B141_W129 · W19 包 — 11-08 落位补间 `DOMove → DOLocalMove`（4 处）

> 执行写手 `B141-W41` · 2026-11-04 · 批次 `B141` · 会话 第一百二十八。
> 判据来源：`判据/B141_W129_对账_UI动画.md` §B 的 11-08 + §E-1；`报告/B138_审计汇总_问题清单.md` §11 的 11-08。
> ⛔ 未跑 Unity、未动 git、未改 `d:/2`、未改账主表/活状态表/所有权表。只改了白名单内的两个 `.cs`（+ 一个 `.meta`）。

## 一句话

把 `Board/DeploySequence.cs` 里 **4 处 `DOMove`（世界系）** 统一改成「世界点 → 父级局部系 → `DOLocalMove`」（新窄口 `LocalMoveToWorld`）；`typecheck` 运行时 0 错 / 编辑器 0 错；断言 23 条（未接线，需调度台挂宿主聚合行）。

---

## 一、坐标帧判定（R8 留的「世界 vs 竞技场局部」问号 —— 结论：**世界坐标**）

| 证据 | 位置 | 内容 |
|---|---|---|
| 生产者 doc + 实现 | `Board/BoardLayout.cs:515-525` `DropTargetWorld` | doc 明写「返回**世界坐标**（落在 HUD 平面 z=0 上，与 `SlotPosition` 同一空间）」；实现走 `LayoutSpace.ScreenToWorld(...)` |
| 同上（抬高那一档） | `Board/BoardLayout.cs:621-634` `DropTargetWorldAtHeight` | 「返回**世界坐标**，与 `DropTargetWorld` **同一个空间**」 |
| 调用点自证 | `Battle/BattleDriver.cs:25847-25849` | `aboveWorld = aboveLocal;`（注释：「`DropTargetWorld*` 返回值**已是世界点**」） |
| 生产路径实参 | `Hand/CardInteraction.cs:1481-1484` | 直接把 `which.DropTargetWorldAtHeight(...)` / `which.DropTargetWorld(land)` 传进 `Play` |

**反证**：`Board/BoardLayout.cs:527-528` 有 2026-11-03 `B124-W9` 的就地订正 —— 原实现里那句 `InverseTransformPoint` **已被删**，现在两档同空间（世界）。若它们是「竞技场局部」，那句 `InverseTransformPoint` 不会「两档现在同空间」，且 `BattleDriver.cs:25847` 的 `aboveWorld = aboveLocal`（不再 `boardRoot.TransformPoint`）也会落错点 —— 与实拍落位对不上。⇒ **世界坐标**，喂给 `DOLocalMove` 必须**先换算成父级局部系**。

> 关键实现：`LocalMoveToWorld(Transform tr, Vector3 worldTarget, float dur)` = `tr.parent.InverseTransformPoint(worldTarget)` 再 `DOLocalMove`。父级为 `null` 时直接用（指向世界）。

### 🔴 诚实记一条（铁律 5 · 供后来人别被「路径不同」误导）

对**静态仿射父级**（任意平移 / 旋转 / 非均匀缩放），`DOMove(world)` 与「世界点换算成父级局部后 `DOLocalMove`」的世界轨迹 **数学上逐点相同**（仿射映射与凸组合可交换；证：`A((1-f)·A⁻¹S + f·A⁻¹E) = (1-f)S + fE = Lerp(S,E,f)`）。
⇒ **本改动不改变今天的画面**（本仓父级当前恒等：手牌/场上卡的父级都是 `sceneRoot` = 恒等）。它是「与判据同一调用形状」的**保真修复**，不是可见 bug 修复。
⇒ 审计那句「**父有旋转/非均匀缩放时路径不同**」对**静态**父级**不成立**（数学等价）；唯一会拉开差别的形态 = **补间途中父级自己动了**（`DOLocalMove` 跟着父级走、`DOMove` 不跟）。本批的灭自证就是拿这一条做的。

> 原版判据（一手）：`decomp_full/MinionManager__MoveMinionToConversionPoint.c`（`param_4 == 0` 正常支是 `DG_Tweening_ShortcutExtensions__DOLocalMove`，目标 = `GetLeft/RightSlotPos` + `up × minionConversionHeight`，局部量）· `decomp_full/CardScript__UpdateMinionInPlayPosition.c:89`（同样 `DOLocalMove`）。

---

## 二、4 处改前 / 改后（行号现读，均**未漂移**：改后仍在 204 / 218 / 282 / 301）

| 处 | 行 | 段 | 改前 | 改后 | 路径 |
|---|---|---|---|---|---|
| ① | **204** | `PlayQuad` 起飞 0.3 | `seq.Append(tr.DOMove(abovePos, MoveTime).SetEase(Ease.OutQuad));` | `seq.Append(LocalMoveToWorld(tr, abovePos, MoveTime));` | `PlayQuad`（AI 卡背 / 探针） |
| ② | **218** | `PlayQuad` 落下 0.2（无交接档） | `seq.Append(tr.DOMove(landPos, LandTime).SetEase(Ease.OutQuad));` | `seq.Append(LocalMoveToWorld(tr, landPos, LandTime));` | 同上 |
| ③ | **282** | `Play` 起飞 0.3 | 同 ① 文本 | 同 ① 文本 | **生产路径**（`CardInteraction.cs:1481` 经 `Play`） |
| ④ | **301** | `Play` 落下 0.2（无交接档） | 同 ② 文本 | 同 ② 文本 | 生产路径 |

- 缓动不变：`LocalMoveToWorld` 内部仍 `SetEase(Ease.OutQuad)`（原版两条没有 `SetEase` ⇒ DOTween 默认 6 = OutQuad）。
- `:212` / `:293`（交接后的落体）本来就是 `FallTarget.DOLocalMove(handoff.LandLocal, ...)`，**未动**（已是局部）。
- `File` 顶注与 `Play` 的 XML doc 里「本补间所在空间」一句就地改成「**世界坐标**」（零行数变化，只改同一行文字）。

现读自检：`grep -c 'tr\.DOMove('` = **0** · `grep -c 'LocalMoveToWorld('` = **5**（1 定义 + 4 调用）。

## 三、行数变化

`Board/DeploySequence.cs`：**336 → 368 行（+32）**。
- 4 处调用点是**原位替换**（行号不变）。
- 新增的是类尾（`RotateEase` 之后、类闭合 `}` 之前）的 `✅ 已复刻` 标记块 + `LocalMoveToWorld` 定义。
- ⇒ **旧有 `文件:行号` 落点（≤ :334）全部稳定，零漂移**；`:335/:336` 是类/命名空间闭合括号（不作 A 档落点）。

## 四、断言（新建 `CardPresentation/Editor/B141_W41_Assert.cs`）

- 暴露 `public static int Pass/Fail` + `public static void Run()`，全在全局命名空间（同仓 `B120_W24_Assert` 的形状）。
- **23 条 `Check`**（绿态），分 4 组：

| 组 | 条数 | 盯什么 |
|---|---|---|
| A · 坐标帧 = 世界 | 12 | 4 处端点都落到**世界目标**（非恒等父级下）；每处再一条「**不是** `DOLocalMove(world)` 漏换算的错点」 |
| B · 灭自证 | 3 | 补间途中平移父级 `delta=(3,2,0)`：卡必须**跟着父级走**（`dRide<5e-2` 且 `dNoRide>1.5`）。数学上 `ride−noRide ≡ delta` ⇒ **DOMove 结构上不可能同时过这两条** |
| C · 生产路径 `:282` | 4 | 带交接的 `Play`：0.95 s 交接真跑、起飞端点对、交接后落体到局部落点（`:293`） |
| D · 源码防回退 | 4 | `tr.DOMove(` 计数 = 0；`LocalMoveToWorld(` ≥ 5 |

### ⚠️ 灭自证的诚实边界

- A1–A4 的「不是错点」只能抓「**漏换算**」这一种退化（`DOLocalMove(world)`）。
- **真正区分 `DOMove` vs 正确 `DOLocalMove` 的只有 B 组**（父级中途移动）—— 因为对静态父级两者数学等价（见 §一）。A 组端点断言**无法**区分这两种实现（它们端点相同），已如实标。
- 覆盖边界：编辑模式手动 `CardTween.Advance` 推补间（不跑 Unity 帧循环）；只验坐标系/端点/是否跟父级，**不验手感**（→ `资料/真Play待验清单.md`）；不验 `PlayQuad` 的 AI 分支时长耦合。

## 五、typecheck

`TMPDIR=/tmp/wf_b141_w41 bash 工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**（含新建的 Editor 断言文件 —— 脚本 `gen_csc_rsp.py` 自动扫 `Assets/`）。

## 六、❓ / 待调度台处理

1. **断言未接线**：白名单不含 `Editor/BattleScene.cs` ⇒ 本文件建好但**没挂进任何宿主聚合行**。调度台需在 `Editor/BattleScene.cs` 的聚合行加 `B141_W41_Assert.Run(); pass += B141_W41_Assert.Pass; fail += B141_W41_Assert.Fail;`（否则失败不会让宿主变红 = 静默失败）。
2. **给审计/账的建议**：11-08 的「父有旋转/非均匀缩放时路径不同」对**静态**父级不成立（数学等价，见 §一）。建议把该格描述就地改为「**与判据同形（`DOLocalMove`）的保真修复**；静态父级下与 `DOMove` 等价，差别只在补间途中父级移动」，避免后来人按「会看到路径变化」去验收。
3. **没有 ❓ 查不到的判据**：坐标帧、原版调用形状、4 处位置均已本机亲读落定。
