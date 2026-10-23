# D5 · `CardView.EndDissolve()` 早退条件核查（A4 报的疑似真缺陷）

> 2026-10-23。只改了 `Core/CardView.cs` + `Editor/CardBaseDemo.cs`（只加断言）+ 本报告。
> ⛔ 没碰 `Core/CardFeel.cs` · `CardPresentation/Battle/**` · `Shaders/Card3D.shader` · 本批一律不动 · 正本 · `d:/2` 写。
> ⛔ 没跑 Unity · 没动 git（只读 `git diff --numstat`）。
> ✅ `TMPDIR=/tmp/wf_D5 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**；两个文件 **CRLF 未翻**（10,622/10,622 · 3,305/3,305，bare LF 0）。

---

## ① 结论：**反了，已修**（但它是**潜伏缺陷** —— 全仓 0 个调用点）

- 原代码：
  ```csharp
  if (_body3D == null || _body3D.sharedMaterial == _dissolveMat) return;
  ```
  读法 = 「**正好在溶解材质上就不还原**」⇒ 任何 `BeginDissolve()` 之后接 `EndDissolve()` 的调用点都会**永远留在溶解材质上**（卡体一直溶着）。A4 的读法是对的。
- **原版判据（不是猜的）**：`BattleCardUI.RestoreOriginalMaterial` 整段**没有任何**「当前是不是溶解材质」的分支 —— 它就是
  `Renderer.SetMaterial(+0x2c8 里缓存的原件)` + `SetCardImageTo3DBase()`；
  `DelayedResetMaterial(delay)` 那条协程等完 `WaitForSeconds` 之后也是**无条件** `SetCardMaterial(defaultMaterial(+0x188), alsoSetCardImage: true)`。
  ⇒ 所以不是「`==` 改 `!=`」，而是**这个判断本身就不该有**（只留 null 守卫）。
- **修法**：只留 `if (_body3D == null) return;`，还原动作**转调 `RestoreOriginalMaterial()`**（它就是原版同名方法的对应物；判据只留一处，⛔ 不在这里再写一份「换回 `_bodyMat` + 喂卡图」），再复位 `_dissolveAmount = -1f`。
- **严重度**：`EndDissolve` 目前**没有生产调用点**（2026-10-23 全仓 `grep EndDissolve` 只命中定义本身 + 本次新加的断言）⇒ 这条是**潜伏缺陷**，修它是给下一个调用点铺路。
- **与 A4 那批改动的关系**：A4 的 `CardFeel` 改的是「阵亡兜底走溶解 / 回手兜底关体」，**不调用 `EndDissolve`** ⇒ 两边不冲突、也不改变它的行为（详见 §⑤·1 一条建议）。

---

## ② 证据

### 2.1 反编译（第一手）

`d:/2/tools/decomp_full/BattleCardUI__RestoreOriginalMaterial.c`：

```c
void BattleCardUI__RestoreOriginalMaterial(longlong param_1)
{
  if (*(longlong *)(param_1 + 0x180) != 0) {                 // minion3DRenderer != null
    UnityEngine_Renderer__SetMaterial(*(longlong *)(param_1 + 0x180),
                                      *(undefined8 *)(param_1 + 0x2c8), 0);   // +0x2c8 = 缓存的原件
    BattleCardUI__SetCardImageTo3DBase(param_1, 0);
    ...
```
—— **没有**「当前材质 == 溶解材质」这类比较；`+0x2c8` 是 `SetCardMaterial` 第一次调用时缓存进 `originalMaterial` 的那一份。

`d:/2/tools/decomp_full/BattleCardUI._DelayedResetMaterial_d__121__MoveNext.c`（协程收尾）：

```c
  if (*(int *)(param_1 + 0x10) == 1) {
    ...
    BattleCardUI__SetCardMaterial(lVar2, *(undefined8 *)(lVar2 + 0x188), 1, 0);   // +0x188 = defaultMaterial
  }
```
—— 同样是**无条件**换回（`1` = `alsoSetCardImage`）。

### 2.2 我们的改动前后

| | 改前 | 改后 |
|---|---|---|
| 守卫 | `_body3D == null \|\| sharedMaterial == _dissolveMat` | **只** `_body3D == null` |
| 还原 | 内联 `_body3D.sharedMaterial = _bodyMat;` | **转调 `RestoreOriginalMaterial()`**（= 换回 `_bodyMat` + `SetCardImageTo3DBase()`，与原版同名方法逐句对应） |
| 记账 | `_dissolveAmount = -1f`（只有没早退时才执行） | 同（**总会**执行到） |

行号（改后现读）：`Core/CardView.cs:5243-5265`（`EndDissolve` 本体 `:5259` · `RestoreOriginalMaterial` 在 `:5294`）。

### 2.3 新增断言（`CardBaseDemo.AssertBodyOpaque()` 的 ⑦ 段，`Editor/CardBaseDemo.cs:707-717`）

在同一次 probe 里，**紧接着 ⑥**（`BeginDissolve` 之后）：

| # | 断什么 | 灭自证（怎么才会红） |
|---|---|---|
| ★ 前提 | `BeginDissolve()` 后 `OnDissolveMaterial == true`（⑥ 段已有） | 证明「进来时**确实在**溶解材质上」—— 否则 ⑦ 是「本来就没换过去」也能绿 |
| ⑦ | `mr.sharedMaterial == m`（**引用相等**：换回的是原来那一份） | 早退条件写回 `== _dissolveMat` ⇒ 留在溶解材质上 ⇒ 红 |
| ⑦b | `!OnDissolveMaterial` 且 `renderQueue == 2000` | 同上；也抓「换回了别的材质 / 没换」 |
| ⑦c | `DissolveAmount < 0`（复位 −1） | 早退 ⇒ 仍是 0 ⇒ 红 |

`m` = 方法开头捕获的那份正常材质；卡体自 D4 起**不再被 `SetQueue` 盖章**，所以 `m` 就是 `_bodyMat` 本体（引用可比）。

---

## ③ 没查清

1. **原版有没有「已经在溶解材质上就保持」的别处语义** —— 我查了 `BattleCardUI` 里与材质相关的全部方法（`SetCardMaterial` / `RestoreOriginalMaterial` / `DelayedResetMaterial` / `ChooseDissolveMaterial` / `SetCardMaterialByCardTier` / `PlayInvokeAnimation` / `PlayBackToHandAnimation`），**没有一处**做「当前材质是不是溶解材质」的判断。⇒ 但这是**间接**证据（我对的是「这些方法里没有」，不是「整个代码库没有」）。
2. **`EndDissolve` 将来该由谁调** —— 现在 0 调用。A4 的 `ReturnToHand`（溶解支）**不还原材质**（见 §⑤·1），靠下一次 `HandToBoard` 里的 `RestoreOriginalMaterial()` 兜；「回手后一直停在溶解材质上」有没有副作用（例如 `ApplyTint` 跳过无 `_Color` 的材质、`SetCardImageTo3DBase` 写的是哪一份），**没验**。
3. **`_dissolveMat` 在 `EndDissolve` 之后仍然存在**（不清空）：`SetDissolveAmount` 的守卫是 `if (_dissolveMat == null) return;` ⇒ 之后若还有人调 `SetDissolveAmount`，会**写进那份没挂载的材质**（画面无变化，但 `_dissolveAmount` 会被改回 0..1）。**无害但会误导读数**（`DissolveAmount` 不再表示「当前是不是在溶解」）。**没改**（改它要动 `SetDissolveAmount` 的语义，超出本条范围）。
4. **A4 的阵亡兜底在溶解完成时是否该 `EndDissolve`** —— 那张卡随后被 `Kill`，材质状态不影响画面；若将来「阵亡后视图被复用」才有意义。没裁。

---

## ④ 改动清单

| 文件 | 改动 | `git diff --numstat` |
|---|---|---|
| `Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` | `EndDissolve()`：早退条件 `== _dissolveMat` → 只留 null 守卫；还原转调 `RestoreOriginalMaterial()`；整段判据注释（含更正痕） | **80 / 18**（累计，含 D2/D4 的改动；本批 +21/−4）· CRLF 10,622/10,622，bare LF **0** |
| `Unity/MyGame/Assets/CardPresentation/Editor/CardBaseDemo.cs` | `AssertBodyOpaque()` 的 ⑥ 之后新增 ⑦ / ⑦b / ⑦c 三条断言 + 改坏法注释 | **122 / 0**（累计；本批 +11）· CRLF 3,305/3,305，bare LF **0** |
| `Unity/资料/普查产出_1023/D5_EndDissolve.md` | 本报告 | 新建 |

**没动**：`Core/CardFeel.cs`（A4 的）· `CardPresentation/Battle/**` · `Shaders/Card3D.shader`（按要求本批不动）· 正本 · `d:/2`。

---

## ⑤ 顺手发现（只报不改）

1. 🟡 **`CardFeel.ReturnToHand()` 的溶解支不还原材质**（A4 的代码，`Core/CardFeel.cs:1296` 起）：
   `canDissolve` 那支只有 `BeginDissolve()` + `_DissolveAmount` 0→1，**没有** `EndDissolve()` / `RestoreOriginalMaterial()`。
   目前靠「下一次登场时 `HandToBoard` 在 0.55/0.7 调 `RestoreOriginalMaterial()`」兜住 ⇒ 画面不出问题；
   但那张卡**回手后一直挂着溶解材质**（`OnDissolveMaterial == true`），任何「按当前材质判断状态」的代码（含以后的断言）都会读到错的。
   **建议（归 A4）**：在 `ReturnToHand` 的 `OnComplete`/`OnKill` 里加一次 `card.EndDissolve()` —— 本次修好之后这条调用是安全的（无条件还原 + 复位 −1）。
2. 🟢 **本次修法与 D4 的 ⑥ 断言互补**：⑥ 断「溶解中 `SetQueue` 不换克隆」（卡体不被盖章），⑦ 断「`EndDissolve` 真的换回去」——两条合起来把「卡的材质身份」这条路的两端都钉住了。
3. ⚠️ **`DissolveAmount` 的语义边界**：`−1` = 没在溶解（`EndDissolve` 之后）/ `0..1` = 当前溶解量。但 §③·3 那条（`_dissolveMat` 不清空）意味着**别处仍可能把它写回 0..1** ⇒ 谁要拿它当「是否在溶解」的判据，请同时读 `OnDissolveMaterial`（材质身份才是硬判据）。
4. ⚠️ 本次没有改 `_dissolveMat` 的生命周期，也没有改 `SetDissolveAmount` 的守卫 —— 都属于「改语义」，留给出报告后另裁。
