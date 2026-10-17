# W5 · PlaceMarkers 重复判据 + 卡背内接

> 执行写手 W5。两件活两个文件，均改完并过秒级类型检查。**本轮不跑 Unity 自检**（现行口径）。
> 行尾：改前先二进制核过 —— `BoardLayout.cs` = **LF**、`CardView.cs` = **CRLF**，改完 `git diff --numstat` 复核**未翻**。

## 一、① PlaceMarkers（改前 → 改后 · 删了哪一处 · 谁调用谁）

- 文件：`Unity/MyGame/Assets/CardPresentation/Board/BoardLayout.cs` · 函数 `PlaceMarkers`（现 `:271`）。
- **改前**（原 `:292-301`）—— `DropTargetWorld(i)` 之后**又有一段重复计算**把它整体覆盖掉：
  ```
  Vector3 markerPos = DropTargetWorld(i);
  if (use3D && boardCam != null)
  {
      var msp = boardCam.WorldToScreenPoint(
          ArenaSlots.CardCenter(i, mirror, ArenaSlots.CardScale(mirror)));   // ← 恒用 CardScale
      if (msp.z > 0f)
          markerPos = transform.InverseTransformPoint(                       // ← 覆盖掉上面那行
              LayoutSpace.ScreenToWorld(new Vector2(msp.x, msp.y)));
  }
  r.transform.localPosition = markerPos + new Vector3(0f, 0f, 0.05f);
  ```
- **删了哪一处**：删掉那个 `if` 块（原 `:293-300`，共 8 行）。**留下** `Vector3 markerPos = DropTargetWorld(i);`。
- **改后**（现 `BoardLayout.cs:292-300`）：只剩 `Vector3 markerPos = DropTargetWorld(i);` + 一句 `localPosition` = `markerPos + (0,0,0.05)`。
- **谁调用谁**：`PlaceMarkers` → `DropTargetWorld(i)`（`BoardLayout.cs:505-515`）→ `ProjectionScale(i)`（`:527-531`，督军走 `HeroScale` 0.40/0.77、其余 `CardScale` 0.36/0.69）。**这是唯一判据**；`PlaceMarkers` 不再自己算一份卡心投影。
- **为什么删这里、不动 `DropTargetWorld`**：`DropTargetWorld` 是**拖拽落点 / 底片 / 飘字 / 根特效**的公共判据（`:505` 注释明写「判据只有这一处」），**不能动**；错的是 `PlaceMarkers` 里那份**副本**（恒用 `CardScale`、丢 `HeroScale`）。⇒ **删副本、留正本**（⛔ 不制造第三种口径）。
- **影响面**：非督军格（0-3、5-8）取值**不变**（那份副本与 `DropTargetWorld` 逐字同式、同用 `CardScale`）；**只有督军格（槽 4）**从 `CardScale` 卡心变到 `HeroScale` 卡心（我方 ~9.6px / 敌方 ~9.1px，只在 y）。

## 二、② 卡背尺寸（改前 → 改后 · 为什么这样取）

- 文件：`Unity/MyGame/Assets/CardPresentation/Core/CardView.cs` · `SetBackVisible`（现 `:3524`；只动卡背尺寸这一处）。
- **改前**（原 `:3542`）：
  `_backLayer.AddComponent<MeshFilter>().sharedMesh = CenteredQuad("cardBack", Width, Height, back);`
  → `CenteredQuad`（`:1823-1848`）**按贴图自身比例内接进 (w × h)**：贴图比 **0.752** < 卡身比 `Width/Height` **0.628** ⇒ 走 `bh = w/a = 2.0927/0.752 = ` **2.782** ⇒ 网格 **2.093 × 2.782**（比卡**矮 16.5%**）。
- **改后**（现 `CardView.cs:3542-3550`）：`_backLayer.AddComponent<MeshFilter>().sharedMesh = Quad();`
  → `Quad()`（`:6534-6549`）= **卡大小的居中 quad**（顶点 `±Width/2, ±Height/2`，UV 拉满 0..1）⇒ 网格 **2.0927 × 3.3313**，贴图**铺满整张卡**。
- **为什么这样取**：卡背是**这张卡的另一面** —— 原版 = 同一条网格的另一面、**与卡面同尺寸**（文件内 `:3503-3504` 已写明）；`CenteredQuad` 的「按贴图比例内接」是给**徽标/图标**那类「别把图拉变形」的小图用的（`:1786`、`:1822-1827`），**不适用于卡背**。⛔ **未改贴图**（`CardArt.CardBack(...)` 原样）。
- **判据**：`Editor/BattleScene.cs:13056-13061`（断 `sharedMesh.bounds.size` = `CardView.Width × Height`，容差 0.02）。
- `CenteredQuad` 仍在别处使用（徽标 `:1795`/`:1815`、属性格 `:6130`），**未动它**；`Quad()` 本是共享静态网格（`:2013`/`:2219`/`:2774` 也在用），只读共享，安全。

## 三、类型检查结果

```
TMPDIR=/tmp/wf_w5 bash d:/4/Unity/工具/typecheck.sh
--- 运行时程序集 --- 运行时错误数: 0
--- 编辑器程序集 --- 编辑器错误数: 0
```
两程序集均 **0 错**。`git diff --numstat`：`BoardLayout.cs 7/8` · `CardView.cs 9/1`（`CardFeel.cs` **未改**，无 diff）—— 数字接近改动量、**行尾未翻**。

## 四、没能验的部分（⛔ 不猜）

1. **`20915` 的 0.2165 残差 —— 未坐实；且本单这一手【大概率解释不了它】。**
   - 🔴 **必须说清**：被删的那份副本，对**非督军格**（断言 `probeSlot` 只取 0/1）与 `DropTargetWorld` **逐字同式**（两处都用 `CardScale`）⇒ **删它不改变 0/1 格的 `markerPos`**。所以 0.2165 若真存在，**删副本这手【不是它的解】**。
   - 诊断代理列的三条未证候选仍在（更可能是 0.2165 的来源）：① `PlaceMarkers` 与断言**跑在不同分辨率**（`BoardLayout.cs:268-269` 自写「4:3 建、16:9 拍」）；② `Slots` 父链被额外平移；③ `ArenaSlots.CardCenter` 的相机状态。
   - 本单**跑不了 Unity**，我**没有**真机 / 带日志探针 ⇒ **不能证实或证伪**。残差原因如实列为**未决**，**不硬凑那个 0.2165**。
   - 删副本的价值在**它本身**（消除「两份判据」、督军格位置不再恒用 `CardScale`）——**不宣称它修好了 20915**。
2. **卡背贴图是否本就该是卡形 —— 未验。**
   - 由 `2.782 = 2.0927 / 0.752` 反推贴图比 ≈ **0.752**，与卡身 0.628 不符。**这张图究竟该不该是卡形**（=「比例本对、只是被内接压扁」还是「素材本身取错」）**需真机 / 看原版** —— 本单跑不了 Unity ⇒ **未验**。⛔ 不猜。
   - ⚠️ 我的改法**不依赖**这一条：无论贴图是不是卡形，「卡背网格取卡身尺寸」都成立（原版卡背与卡面同尺寸，见 `:3503-3504`）。若日后证实贴图取错，**另有账**，与本改动不冲突。

## 五、顺手发现（⛔ 没动手）

1. **同一函数 `PlaceMarkers` 的「尺寸」那一半仍在恒用 `CardScale`**（现 `BoardLayout.cs:309`：`float sc = ArenaSlots.CardScale(mirror);`）。
   - 位置那半已改走 `ProjectionScale(i)`（督军 `HeroScale`）⇒ 同一枚底片的**位置按 `HeroScale`、尺寸按 `CardScale`**，对**督军格**自相矛盾。
   - **未动手的理由**：① 不属于本单「重复判据」范围（诊断只点名位置那份副本）；② **督军格永无落子**（督军已占）⇒ 那格底片 α 恒 0、看不出来；③ 「底片尺寸该不该跟 `HeroScale`」**无明文判据**（只推得出「该跟」，未有出处）⇒ 按铁律 14「判据未清则先记账」。
   - 建议：若要收口，把它一并改成 `ProjectionScale(i)`（同一个口、只有一处判据）。**交主对话裁**。
2. **`BoardLayout.DropTargetWorldAtHeight:575` 同样恒用 `CardScale`**（`float sc = ArenaSlots.CardScale(mirror);`），与 `DropTargetWorld` 的 `ProjectionScale` 不一致。它是「手牌→棋盘」第一段终点的口（`:558-584`）。**不在本单范围**（简报只指定 `PlaceMarkers` 与 `DropTargetWorld`），且同样只影响督军格（无落子）⇒ **未动手**。⛔ 我没动 `ArenaSlots.cs`（不在白名单）。
3. `Core/CardFan.cs:151` 注释待订正 + `SwapToFront` 换位 tween 未同步 `CardView._baseScale` —— 简报已点名「已知潜伏、别动」，我**未碰**，照记确认。
