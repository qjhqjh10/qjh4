# B118_W2 · 手牌悬停位姿（`B117-R64`）与 TMP 子网格队列（`B117-R73`）

> 红线：⛔ 未跑 Unity · ⛔ 未动 git · 未碰白名单外文件（`Editor/BattleScene.cs` / `Scenes/*` / 账主表 / 正本）。
> `TMPDIR=/tmp/wf_w2 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（两次；中途出现过一次 `LiveOpsHandler.cs` 的错，那是 W5 在飞）。
> `check_ledger_points`：**我这两个文件 0 漂移 / 0 失败**（总表 漂移 26 / 失败 5，全在别人在飞的文件上）。
> `check_aggregate_lines`：四个文件 **0 聚合行、违规 0**。

---

## 逐件一 · `B117-R64` 悬停那张的缩放与 y（✅ 已改）

**原版判据**（`判据/B117-G10-06_手牌.md` §四·1/§四·2；第一权威 `CardScript__ShowCardInHand`）：
`localScale = Vector3.one × cardInHandShownScale`（**绝对值 1.3**）· `position = (x, GetPlayerHand(isPlayer).transform.position.y + cardInHandShownYOffset, z − 0.01)`
—— `PlayerHand` 就挂在 `BottomAnchor/PlayerArea/HandArea`（纯 `Transform`、`localPosition = 0`）上 ⇒ 卡心 = **手牌区底边 + 30 px**，与槽位/弧高无关。
静止档 = `GetCardInHandScale()` = `one × m_scale(0.73)` ⇒ 悬停/静止 = **1.3 ÷ 0.73 = 1.7808**（改前是 0.73×1.3 = 0.949，**只有原版 53% 宽**）。

**改动清单**（`Hand/HandLayout.cs`，两条物理行就地改写，**行数零变化 1064**）
| 行 | 改前 → 改后 |
|---|---|
| `:798` | `pos.y += hoverLift;` → `pos.y = (areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + hoverLift;`（`(…−0.5)×VisibleHeight` = 可见区底边的世界 y；`areaOffsetY` 是换牌期整排下移的**动画位移**，原版 `PlayerHand` 读的就是 HandArea 那个节点 ⇒ 含它） |
| `:799` | `scale *= hoverScale;` → `scale = hoverScale * LayoutSpace.Scale;`（`LayoutSpace.Scale` = 静止档 `cardScale × Scale` 里那个**同一窄屏系数**；16:9 恒 1） |

**量级**：卡心由 161.35 px（131.35 + 30）降到 **30 px** ⇒ **低 131.35 px**（= 对账订正后的数；判据文件里那个「≈119 px」是错的）。悬停卡 1.3 档高 468 px、卡心在 30 px ⇒ 卡底出屏 204 px（原版代数如此）。
⚠️ **不引出 hover 抖动的证明**：`CardInteraction.HitTest → Frontmost` 走真 collider（y 敏感），但悬停那张的包围盒（x 更宽、y ∈ [−204, 264] px）**是静止档 [0, 262.7] 的超集** ⇒ 指针只要原来在卡上，抬起来之后仍在卡上。

**断言**：新增 `Editor/B118W2_Assert.cs` A 组 8 条（4 前提 + 4 结论）：
`A1`（绝对 1.3 = `hoverScale × LayoutSpace.Scale`）· `A1b`（与未悬停那张的**比值** = `1.3f/0.73f`，字面量来自原版两个常数，**不从实现读**）· `A2`（y = 底边 + 30 px）· `A2b`（**换到外端那一张**悬停仍是同一个绝对 y ⇒ 灭「槽位 y + 30」）。另：`Editor/B117F11_Assert.cs:127`（`want.y`）与 `:135`（`wantScale`）**已同改**。

## 逐件二 · `B117-R73` 收起再放开后 TMP 子网格掉回 3000（✅ 一处收口）

**判据链**（TMP 包源码，只有这一条）：`TextMeshPro.OnEnable`（`:630-668`）`SetAllDirty` → 延迟一拍的 `TMP_UpdateManager.DoRebuilds`（`TMP_UpdateManager.cs:52`，挂 `Canvas.willRenderCanvases`）→ `SetArraySizes`（`TextMeshPro.cs:1947`）**把每颗 `TMP_SubMesh` 的 `sharedMaterial` 换回它自己的参考表**（字体图集材质，队列 3000）。主网格由 `UpdateMaterial`（`:307-320`）按 `m_sharedMaterial` 拉平 —— 已由 `B117-FIXH` 的 `SyncTmpSharedMaterial` 同步 ⇒ **只剩子网格会掉**。

**① 路径普查**（`CardPresentation/` 全树，`SetActive(false)→(true)` 且树上有卡的文字层）

| 路径 | 位置 | 处理 |
|---|---|---|
| 换牌面板「眼睛」收起/放开 | `Battle/MulliganPanel.cs:1946/1950` | 已有（`B117-FIXH`） |
| 换牌面板 `Close()` 前还原被收起的卡 | `Battle/MulliganPanel.cs:1980` | **本次补** `RestampQueuesAfterEnable()` |
| 整张卡被关/开（`_logCard`、教程督军两拍、`createHandCard` 撤销、场上视图重建、卡池/收藏格复用…） | `BattleDriver.cs:12411/12422 · 1785 · 18132/18136 · 26296 · 37918 · 38793`、`Deck/DeckRuntime.cs` 卡池、`Editor/CollectionScene.cs` —— **都在白名单外** | **由 CardView 侧一处收口自动覆盖**（见下），⛔ 不需要逐个改 |
| `CardView` 内部文字层「从关到开」：`_title/_keywords/_race/_army`、`_createdBy`、四个数值容器、费用容器 | `Core/CardView.cs:2556`（`Show(TextMeshPro, bool)`）· `:4906`（`FillCreatedBy` 直写）· `:13458`（`SyncFaceStatTexts`）· `:13556`（`SyncFaceCostText`） | **本次补**（四处都挂起） |

**② 改动（`Core/CardView.cs`，行数守恒；新增 25 行**全在类尾 `:16033` 之后**，账最深落点 `:15640` ⇒ 零漂移）**
| 行 | 内容 |
|---|---|
| `:2556` | `Show(TextMeshPro, bool)` 由 `static` 改**实例**方法：**只在真从关到开**时挂起「下一帧重盖」（`static`→实例已 typecheck 验证：无静态调用方） |
| `:2809` | `Update` 末尾：`if (_enableRestampPending && Time.frameCount > _enableRestampFrame) RestampQueuesAfterEnable();` |
| `:4738` | `OnEnable`：`if (!_built) return;` **之后**挂起（`_built` 之前那次是 `Create` 的 `AddComponent`，不挂） |
| `:4906` · `:13458` · `:13556` | 直写 `SetActive(true)` 的三处补挂起（`_createdBy` / 数值容器 / 费用容器，都不走 `Show`） |
| `:16031` | `RestampQueuesAfterEnable` 末尾 `_enableRestampPending = false;`（盖完清位） |
| `:16033` 之后 +25 行 | `_enableRestampPending` · `_enableRestampFrame` · `EnableRestampPendingForTest`（读口）+ 判据 doc |

🔴 **为什么不在 `OnEnable` 当场盖**（这正是「一处收口」能成立的前提）：卡片激活时**父件 `OnEnable` 先于子件 TMP 的**（同族证据：`X17` 的 `_textNeedsRefit` 就是「激活那一刻量不到、要等下一帧」）⇒ 当场 `ForceMeshUpdate` 会被**子件之后那次** `SetAllDirty` 触发的重建覆盖。✅ **不会递归**：`SetQueue` 只写 `sharedMaterial` / `fontSharedMaterial`，都不触发 `SetActive`。✅ **不会覆盖别人**：`SetQueue` 重放的是**这张卡自己的 `_queue`**（「每张卡一段队列」的唯一实现）；卡外写队列的三处（`ImageQuad` / `Label` / `BoardLayout` 标记）都不写卡的渲染器。

**③ 断言**：`Editor/B118W2_Assert.cs` B 组 7 条（3 前提 + 4 结论）。做法 = 造一张 **CJK 卡名**的卡（拉丁字体走运行期 CJK fallback ⇒ TMP 多一颗子网格）→ 盖章 3007 → 收起→放开 →
`B1` 挂起位为真 · **`B2` 判别式：自己 `ForceMeshUpdate` 模拟「延迟一拍的重建」之后，必须有渲染器掉回别的队列**（若为 0 ⇒ 夹具没有判别力，如实红）· `B3` 重盖后**每一颗（含 `TMP SubMesh`）**都回到 3007 · `B4` 挂起位已清。

⚠️ **未覆盖**：`_healText` 那类浮字、以及将来「只重启单颗 TMP、又不经过 `Show(TextMeshPro, bool)`」的新路径（那种要在那一处补一次挂起）。

---

## 待调度台接线（`文件:行号` → 改成什么）

1. **`Editor/B118W2_Assert.cs`（新建，未接线）** —— `BattleScene.cs` 的聚合行追加：`Guard("B118W2_Assert", B118W2_Assert.Run);` + `_pass += B118W2_Assert.Pass; _fail += B118W2_Assert.Fail;`。**零前提**（自带临时根、`animateRelayout=false`、不用 `CardTween`）⇒ 可放任意位置。
2. **`Editor/BattleScene.cs:3745`** —— `Mathf.Abs(ratio - 1.3f) < 1e-3f` → `Mathf.Abs(ratio - 1.7808f) < 1e-3f`（实得比值 = 1.3 / 0.73005 = **1.78070**，差 1.3e-4 在容差内）。同段 `:3746-3749` 的文案「= 未悬停那张的 1.3 倍」要改「**1.7808 倍**（= 1.3 ÷ 0.73）」。
3. **`Editor/BattleScene.cs:24186` 与 `:24189`** —— `Mathf.Abs(hv.transform.position.y - (restY + hand.hoverLift))` → `Mathf.Abs(hv.transform.position.y - ((hand.areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + hand.hoverLift))`（两句一样；`LayoutSpace` 该文件已在用，152 处）。`restY` 仍用于 `:24194`「松开后回到原位」，⛔ 别删。
4. **`Editor/B117_G10_06_Assert.cs:118`** —— `wantHover.y += layout.hoverLift;` → `wantHover.y = (layout.areaOffsetY - 0.5f) * LayoutSpace.VisibleHeight + layout.hoverLift;`（该文件不在我白名单，且是同波另一个代理的产出 ⇒ 未动）。
5. **`判据/B117-G10-06_手牌.md:128`** —— 仍写着「比现在低 ≈ 119 px」，**现读 131.35 px**（161.35 → 30）；请就地订正（铁律 5）。
6. **`Editor/BattleScene.cs:24276` · `Editor/CardBaseDemo.cs:5791/5794`** —— 只断字段值（`hoverLift 30px` / `hoverScale 1.3`），**不受影响，不用改**（对账结论成立）。

## 账补丁

`账/_parts/B118_W2.tsv`（11 列表头，2 行）：
① **就地订正** `CardScript.ShowCardInHand(PointerEventData eventData)`（= `战斗逻辑驱动.tsv:922` 那一行，按 `(原版类,方法)` 键替换）—— 补上「两个数值原来就对了、**应用方式错了**」与本次改法；
② **新增** `TMP_Text.OnEnable()`（域 `卡牌面/渲染`，与既有 `TMP_Text.CalculatePreferredValues` 同域）—— 子网格材质被重建覆盖的机制 + 我们的收口落点。
⚠️ 未合并（主表只有调度台能写）。

## 顺手发现 / 没做的

- `Show(MeshRenderer, bool)` 仍是 `static`；`Show(TextMeshPro, bool)` 改实例是**必须的**（要挂起位）。已 typecheck 验证无静态调用方。
- `HandLayout` 里 `zOrderStep` / `frontZ` 那两条判据（§四·3/§四·4）**没动**（§四·3「−0.01 vs −0.5」要动 `B117F11_Assert.cs:128` 且不影响观感，属另一件）。
- 敌方手牌不会走到悬停分支（`isPlayer=0`），改这两行对它零影响。
- 批处理**没有帧循环** ⇒ `Update` 那一刀在自检里不会来；`MulliganPanel:1980` 的显式调用与 B 组断言都是为此保留的（真 Play 才有自动那条）。
