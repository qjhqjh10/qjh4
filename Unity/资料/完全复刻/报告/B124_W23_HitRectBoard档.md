# B124_W23 · `HitRect()` Board 档（BT 簇 C 7 条）—— **停手：判据前提不成立，产品代码零改动**

> 2026-11-03 · 执行写手 W23 · 只读对账 + 数值复算，**未改任何源码**（`CardView.cs` 工作树现读 = 0 改动）。
> 输入 = `判据/B124_R6_剩余红分诊.md` §1 簇 C（#27–#33）· 日志 `d:/4/_tmp_view/battle_b124b.log:898441/898454/898506/898519/898545/898584/898597`。

## 〇、结论（一句）

`HitRect()` 的 Board 档**本来就返回 0.985 / 1.51 / 0**（不是 in-hand 值），而那 7 条红**根本不经过 `HitRect()`** ——
它们量的是**层 10 上那颗 3D `BoxCollider`**（原版 `Card 3D` 那一颗），生产路径自 `B120-W13/W27` 起就是原版射线链。
⇒ **β 归因错**（⛔ 判据本身错）；真正过期的是断言，账里 `B120-Q04e / Q04f / Q04g` 早已预告这三条。

## 一、取证：Board 档矩形从哪来 / 现在返回什么

1. **Board 档矩形源 = 原版 `2DCard` 主面**：`BoxCollider2D_7520353667969031104.json` → `m_Size 1.97 × 3.02` · `m_Offset (0,0)`；
   常量 = `Core/CardView.cs:9475 HitHalfW = 1.97*0.5` · `:9477 HitHalfH = 3.02*0.5` · `:9479 HitCenterDy = 0f`（in-hand 另两条 `:9482/:9484`）。
2. **`HitRect()` 现读（`Core/CardView.cs:9641-9646`）**：`bool inHand = _faceMode == CardFace.Hand && !_inHandBigGeometry;`
   ⇒ Board 时 `inHand == false` ⇒ `halfH = 1.51`、`centerDy = 0` ⇒ **就是 0.985 / 1.51 / 0，没有 in-hand**。
   `SetFace` 换形态那一刻还显式刷一次（`CardView.cs:2359 SyncHitCollider()`），2D 档不会停在旧几何。
3. **7 条红不走这条路**：`BattleDriver.HitUnitSlotForTest`（`:13627`）→ `HitSlot`（`:33318`）→ 3D 档
   `TryHitSlotByBoardRay`（`:41133-41156`）= `Physics.RaycastNonAlloc(ray, 200f, 1 << BoardCardLayer(10))` → **字面 `hits[0]`** →
   `GetComponentInParent<CardView>()`。层 10 上全工程**只有一颗 3D collider**：`CardView.cs:16077-16079`
   `BoxCollider` size **1.827034 × 3.01 × 0.5** · center **(0.013507843, 1.55, −0.016031265)**（原版 `CardPrefab.prefab:3389-3407`，`Card 3D` 层 10）。
   （全仓 `AddComponent<BoxCollider>` 只此一处 —— 没有第二颗可命中物。）
4. **原版同款**：`decomp_full/BattleManager__GetTargetInPos.c:238-265` = `ScreenPointToRay` → `RaycastNonAlloc(200f, 1<<NameToLayer("BoardElements")=10)`
   → `iVar7 < 1 ⇒ return null` → `hits[0].transform` → `GetComponentInParent<CardScript>()` ⇒ **原版棋盘命中就是这颗 3D 盒**，2D `BoxCollider2D`（层 5）不进 3D 射线。

## 二、数值复算：实测四个数**逐位**由 3D 盒解释

`body3D` 挂在卡根、`localPosition (0, −Height/2, 0)`、旋转 180°Y ⇒ x 取反、`localScale 0.88586`（`CardView.cs:7937-7941`；`_tiltRoot` 是 identity ⇒ 同一坐标系）
⇒ 盒子在卡局部：**x ∈ [−0.8212, +0.7973] · y ∈ [−1.6258, +1.0407]**。

| 探针（`BattleScene.cs:39198-39252`） | 盒内？ | 二分落点 | 日志 |
| --- | --- | --- | --- |
| x 左 −0.935 → −1.065 / 右 +0.935 → +1.065 | 全在盒外（盒仅 ±0.82/0.80） | 停内端 **±0.9350** | `898506` |
| y 上 1.4600 → 1.5900 | 全在盒外（盒顶 1.0407） | 停内端 **1.4600** | `898519` |
| y 下 −1.4600 → −1.5900 | **全在盒内**（盒底 −1.6258） | 停外端 **−1.5900** | `898519` |
| 中心 (0,0) / 0.45H=1.4991 / 0.35H=1.1660 | 中心内、另两点高于盒顶 | 5 / −1 / −1 | `898545` |
| 2D collider 放大 1.2× 后 (1.05,0) | 盒半宽 0.809×1.2=0.971 < 1.05 | 仍 **False** | `898597` |

**分诊里「半高 1.525 = in-hand、中心 −0.065」是二分端点的巧合**：(1.46+(−1.59))/2 = −0.065、(1.46−(−1.59))/2 = 1.525，
而 1.46 / −1.59 正是断言自己的探测端点（`1.51∓0.05` / `−1.51∓0.08`）⇒ **上沿根本没探到、下沿被探针截断**，那两个数不代表任何真实边界。

## 三、灭自证（三条，说明「改 `HitRect()` 一定修不好」）

- **B2b③（`BattleScene.cs:39347-39360`）**：放大的是 **2D** `ColliderForTest`，生产口读数**一点没动**（`898597` 放大后 False/False）。
  若棋盘走 2D，`1.05` 处在 1.2× 后必翻成命中 ⇒ 反证棋盘**不在 2D 路径上**。
- **B2b①（`898545`）**：同一时刻 `HitSlot` 判 −1、手算 `Contains` 判 5 —— 2D 档下两者是同一个矩形、必然同判 ⇒ 反证当前是 3D 档。
- 全工程棋盘命中只有一条几何：`PointerOverReplayInspectable`（`BattleDriver.cs:8659` 一带）棋盘走 `HitSlot`、手牌才走 `Contains`。
- ⇒ 把 in-hand 值写进 Board 档：**这 7 条读数一个都不会变**（仍红），而手牌档 / 2D 兜底档的断言会红 —— 那是纯破坏。

## 四、改动 / 行数 / 类型检查

- **产品代码改动：无逻辑改动**。唯一一笔 = **同一行的注释订正**（铁律 5）：`CardView.cs:16082`
  `body.layer = gameObject.layer;` 行尾原写「场上 = `ArenaLayer`(8) ⇒ 落 `CollisionMask` 的 bit8」——**已过期**
  （`B120-W27` 已把场上卡迁层 **10 `BoardCardLayer`**、`TapParticleController.CollisionMask` 显式含 bit10）⇒ 就地改成层 10 并留订正痕迹。
- **行数变化：0**（改前 16308 行 / 改后 16308 行，LF 无裸 CR；`CardView.cs:16080-16099` 无任何账行落点）。
- `TMPDIR=/tmp/wf_b124w23 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（改注释前后各跑一次，均 0/0）。

## 五、真正该改的（不是我的文件，交调度台分流）

7 条全在 `Editor/BattleScene.cs:39157-39360`，**口径按 3D 盒重定**（期望值一律从 `CardView.TapBlockColliderForTest`（`:16090`）经同一 `HitLocalToWorldForTest` 换出来，⛔ 别抄 0.985/1.51）：
① **T03（#29/#30）**：边界改 `x ∈ [−0.8212, 0.7973]`、`y ∈ [−1.6258, 1.0407]`（或改断「= 3D 盒的四个面」）；
② **A964/E2②（#27/#28）**：`0.45H(1.499)/0.35H(1.166)` 都在盒顶外 ⇒ 换盒内探针（如 `0.25H = 0.833`）+ 保留中心点；
③ **B2b①（#31）**：「与改前手算 `Contains` 逐点同判」的前提**已随 `B120-W13` 失效**，改为「与 3D 盒手算同判」；
④ **B2b②（#32）**：`hits[0]` 不按 z 排序（原版也没有排序），实测两只都取到槽 4 ⇒ 按 `B120-Q04g` 回源重判，别硬改成 z-min；
⑤ **B2b③（#33）**：放大对象改 `TapBlockColliderForTest`。
