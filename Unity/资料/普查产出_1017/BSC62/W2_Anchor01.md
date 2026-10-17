# W2 · HudPiece.Anchor01() 读回丢 anchoredPosition 修复

## 一、改了什么（文件:行号 · 改前 → 改后）

文件（**唯一**）：`d:/4/Unity/MyGame/Assets/CardPresentation/Battle/HudPiece.cs`（LF，改前 346 行 → 改后 385 行）。

只动 `Anchor01()` 的 **uGUI 支**一处；quad 支、`NudgeWorld`、`SetAnchorPosition` **一行未动**。
（`git diff --numstat` = `41 + 2 -`，全部落在 doc + 这一个 return。）

- 改前（现读旧 `:253-257` 的方法体）：
  ```csharp
  public Vector2 Anchor01()
  {
      if (_quad != null) return LayoutSpace.ToNormalized(_quad.transform.localPosition);
      return _rt != null ? _rt.anchorMin : new Vector2(-1f, -1f);   // ← 丢掉 anchoredPosition
  }
  ```
- 改后（`HudPiece.cs:288-297`，doc 在 `:250-287`）：
  ```csharp
  public Vector2 Anchor01()
  {
      if (_quad != null) return LayoutSpace.ToNormalized(_quad.transform.localPosition);
      if (_rt == null) return new Vector2(-1f, -1f);
      return new Vector2(_rt.anchorMin.x + _rt.anchoredPosition.x / LayoutSpace.DesignPxW,
                         _rt.anchorMin.y + _rt.anchoredPosition.y / LayoutSpace.DesignPxH);
  }
  ```

## 二、单位契约推导（锚点参考矩形取哪一块 · 为什么）

逐跳（口径同时写进了方法 doc `:261-278`）：

1. `SetAnchorPosition(x01,y01)`（`:346`）= **归一化**：`anchorMin = anchorMax = (x01,y01)` 且 `ap = 0`。
2. `NudgeWorld(dxWorld,dyWorld)`（`:233`）= `ap += (dxWorld,dyWorld) · k`，`k = LayoutSpace.DesignPxH/DesignHeight = 108`
   （= `LayoutSpace.Px()` 的**逆**，`LayoutSpace.cs:211`）。
3. 三个调用点（`BattleDriver.cs:15899/15900/16453`）喂进来的 `dxWorld` **已经是 `Px(px) = px/108`**
   （`BattleDriver.cs:1378`）⇒ 乘回 108 之后 **`ap` 累加的就是【设计 px】**。
4. ⇒ 要把「设计 px 偏移」折回**归一化**，分母必须是**设计框** ⇒ 用 `LayoutSpace.DesignPxW`(1920) / `DesignPxH`(1080)。
5. quad 支的 `ToNormalized(localPosition)`（`LayoutSpace.cs:135`）是 `ToWorld` 的逆、**与分辨率无关** ⇒
   两支现在量的是**同一件事**（设计框归一化）。

🔴 **「锚点参考矩形」= 设计框 1920×1080，不是父件（canvas）的实 rect。**
- 根 canvas 挂 `CanvasScaler(1920×1080, **Expand**)`（`HudCanvas.cs:110-113`）⇒ canvas 实 rect = `屏px ÷ scaleFactor`，
  **只在 16:9 下 = 1920×1080**；比 16:9 **窄**时变高（批处理 640×480 → `scaleFactor = min(640/1920, 480/1080) = 1/3`
  ⇒ canvas = **1920×1440**，`LayoutSpace.cs:174` 有 640×480 的自证句）。
- 若拿父 rect 当分母：y 除以 1440 ⇒ 回合灯读回 `≈0.0925` ≠ 期望 `0.08778`（超 0.002 容差），
  且读数会**随宽高比变**，与「写侧写的是设计 px」及 quad 支**都对不上**。
- 接口选择：`LayoutSpace.DesignPxW` / `DesignPxH`（现成口），**没有硬编码 108 / 1920 / 1080**。

## 三、为什么这条修完「回合灯」那条断言会绿（代账）

断言：`Editor/BattleScene.cs:3588-3590`
`Check(|lp.x − 0.94451| < 0.002 && |lp.y − 0.08778| < 0.002)`，`lp = drv.MyDeckLightPos01`。

回合灯件：`BattleDriver.cs:14437-14438` 建在 `MyDeckX01/MyDeckY01 = 0.90104 / 0.10648`（锚），
`PlaceDeckLights`（`:15899`）nudge `NudgeWorld(Px(83.46), Px(−20.2))` ⇒ `ap = (83.46, −20.2)`。

- 修前（只回 `anchorMin`）：`lp = (0.90104, 0.10648)` ⇒ 两轴各差 `0.04347 / −0.0187` ⇒ **红**。
- 修后（本件）：
  - `x = 0.90104 + 83.46/1920 = 0.90104 + 0.04346875 = 0.94450875` → 期望 `0.94451`，差 `1.3e−6` ✅
  - `y = 0.10648 + (−20.2)/1080 = 0.10648 − 0.01870370 = 0.08777630` → 期望 `0.08778`，差 `3.7e−6` ✅
- 那两个差 `+0.04347 / −0.0187` px 折回 = `(83.46, −20.2)` px —— 正是 `PlaceDeckLights` 喂进去的那个偏移，**一分不差**。

## 四、类型检查结果

`TMPDIR=/tmp/wf_w2 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时错误数 0 · 编辑器错误数 0**（两个程序集都过）。

## 五、没能验的部分（⛔ 不猜）

1. 🔴 **没跑 Unity 自检**（现行口径 = 只做不验）。⇒ 「回合灯那条断言真的会绿」是**算术推导**，**没有实跑证据**；
   要证实必须跑 `BattleScene.Run`（本件不跑）。
2. 🔴 **本仓没有 62 条红之前的基线 ⇒ 不声称任何一条是「回归」**（简报已点名）。
3. ⚠️ **「CanvasScaler Expand 在 4:3 ⇒ canvas 实 rect = 1920×1440」是推导、未在批处理实测**
   （依据 = `LayoutSpace.cs:174` 自证 640×480 + CanvasScaler Expand 语义）。⇒
   `设计框` 与 `父 rect` 谁对，**决定于这条推导**。**但即便推导反了**（父 rect 恰是 1920×1080），
   本件用设计框仍然给出 `0.94450875/0.08777630` ⇒ 结论不变（设计框在 16:9 与父 rect 同值）。
4. ⚠️ **未验**：`HandPlatePos01`（`BattleDriver.cs:16030`）现在会**变值**（见下），其断言
   `Editor/BattleScene.cs:2193` 我只做了静态推断（更宽松、仍绿），**没跑**。

## 六、顺手发现（⛔ 没动手）

1. `EnemyPlatePos01`（`BattleDriver.cs:16045`）**没成对**改：它仍走 `LayoutSpace.ToNormalized(_enemyPlate.transform.localPosition)`，
   而 `MyPlatePos01`（`:16041`）走 `Anchor01()` —— 简报已点名那是 **W3** 的活，**未动**。
2. **其它 `Anchor01()` 消费者的静态复核（只读）** —— 只有 ap≠0 的件读数会变：
   - ap=0（**本件后读数不变**）：`SkullIconPos01`（`:16014`）· `MyPlatePos01`（`:16041`）·
     `HudExtraPosPx`（`:6648`）· `HudExtraReport`（`:14788`）· `PosOf(HudPiece)`（`:16180`，任务点/能量座）。
   - ap≠0（**读数会变**）：`MyDeckLightPos01`（`_myDeckLight`，`PlaceDeckLights`）· **`HandPlatePos01`**（`_handPlate`，
     `PlaceHandPlate` `:15924` 写 `ap = (WorldW/2, WorldH/2)`）⇒ 由「= 锚点」变为「= 板心**（真值）**」。
     其断言 `Editor/BattleScene.cs:2191-2194`（`hp.x − halfW01 ≥ −0.002`）在 hp.x **变大**后只会**更宽松** ⇒ 仍绿（**未实跑**）。
3. `FitDeckPile`（`BattleDriver.cs:16442-16454`）对 `_myPile` 也写非零 `ap`（`SetAnchorPosition` 后 `NudgeWorld(Px(dx),Px(dyUp))`），
   但**没有消费者读 `_myPile.Anchor01()`** ⇒ 无影响（仅登记，供后续对账时留意）。
