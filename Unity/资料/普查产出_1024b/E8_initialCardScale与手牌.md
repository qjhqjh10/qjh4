# E8 · `initialCardScale` 口径复核 ＋ 手牌三组断言

> 执行代理 **E8** · 2026-10-24
> 白名单：`Hand/HandLayout.cs` · `Hand/Hand*.cs` · `Hand/CardInteraction.cs`（回弹那几段）· **新建** `Editor/HandAssertE8.cs`。
> ⛔ 本批**一个生产文件都没改**（理由见 §一、§二）；⛔ 未跑 Unity、未动 git。
> `TMPDIR=/tmp/wf_e8 bash 工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后一次改动之后复跑）。

---

## 〇、三件的结论速览

| # | 任务 | 结论 |
| --- | --- | --- |
| 1 | `initialCardScale 150 / initialHeight 512` 改 `SetPose` 口径 | ✅ **判据查清**；🔴 **结论是「⛔ 不改」** —— 它给的是「**每卡单位多少 px**」的绝对缩放，**不是**我们的 `SetPose(scale)`（相对原生大小的世界缩放）。直译会让卡**压进张数条 29.1 px**，与用户的原版实拍矛盾（详见 §一）。断言已写成「口径钉」（`D_InitialCardScale`，3 条）。 |
| 2 | `HandLayout.Place` 换 `CardView.TweenPose` | ✅ **已经在盘上**（`A3` 落的，`HandLayout.cs:874-882`；`CardTween.ToPose` 已被 `A11` 整段删除）。本批**只补了它没覆盖的那半条**：`_baseScale` **补间进行中**就跟着走（`A3` 在 `CardBaseDemo` 断的是**终值**）。 |
| 3 | 手牌三组断言（旋转 / 重排闸 / 回弹） | ✅ 写进**新建**的 `Editor/HandAssertE8.cs`，A/B/C 三组 31 条（含 8 条前提 + 2 条对照）＋ D 组 3 条 = **共 34 条**。 |

---

## 一、`initialCardScale / initialHeight` —— **用法坐实，但结论是「别直译」**

### 1.1 原版怎么用（现读，铁律 3）

`d:/2/tools/decomp_full/CollectionCard__OnRectTransformDimensionsChange.c:16-22`：

```
UnityEngine_RectTransform__get_sizeDelta(lVar1,0);
local_20 = (extraout_var * *(float *)(param_1 + 0x70)) / *(float *)(param_1 + 0x74);
UnityEngine_Component__get_transform(*(longlong *)(param_1 + 0x48),0);   // cardUI
UnityEngine_Transform__set_localScale(lVar1, &local_28, 0);              // (s,s,s)
```

- `+0x70` / `+0x74` = `initialCardScale` / `initialHeight`；序列化实读
  `bundle_menus_assets_all/MonoBehaviour/MonoBehaviour_40971030049256792.json` = **150.0 / 512.0**；
- `lVar1` = `GetComponent<RectTransform>(this)` = **`CollectionCard` 自己的 rect**；被写的是 **`cardUI`**；
- ⚠️ **`localScale` 在这里是「每卡单位多少 px」的绝对缩放**（`A4_装饰页与驱动链.md:149`：卡内单位是作者的米
  `2.093×3.331`，×150 才成像素）—— **不是**我们的 `CardView.SetPose(scale)`（「相对卡原生大小的世界缩放」，
  `HandLayout` 那一套是 `m_scale 0.73 × 2Dcard`）。
- ✅ 顺带把 A14 的更正核死了：这个法体**一次都没读 `AnimateIn` 的东西**，两个字段也不在入场动画那条路上。

### 1.2 为什么**反推不出**原版那个 `sizeDelta.y`（铁律 2「查不到就说查不到」）

`cardUI` 的 `RectTransform`（`RectTransform_6631878794057741656.json`）：
stretch 锚 `min(0,0)/max(1,1)` · `m_SizeDelta = (−350, −512)` · 父件 `Content` 已 stretch 到
`Collection Card` 的 **350×512** ⇒ 算出 **0×0**。`localScale` 出厂值恰好就是 **150**。
⇒ 卡的真尺寸/位置是**运行时**设的，静态数据里没有（同族结论：`资料/普查产出_0923/A4_装饰页与驱动链.md:149,223`
已经写过「查不到」）。

### 1.3 最终判据 = 用户的原版实拍（这一条把结论定死了）

`C:/Users/qjh36/Desktop/warpforge参考图/收藏—张牌的界面参考图（打开过滤器按钮后）.png`（1578×888，原版收藏 Cards 页）
放大 5 倍量卡底那一带（`_tmp_view/e8/ref_counter_zoom.png`）：
**卡框的银色下边框完整可见，暗红张数条（`x2`/`x1`）在它【下面】**，两者几乎相接、**没有重叠**
+ 与 `W11`（`资料/普查产出_1018/W11_卡组编辑计数重叠.md` §二·1）在原版卡组编辑页量到的
「卡框可见底 507.6–510.1 / 条顶 511.3 ⇒ 间隙 ≈1.2 px」**同一结论**。

⇒ **现行口径（`CardsCardRect` = 格高 − 张数条高 = 345.728）与原版实拍一致**。
原版公式直译成我们的口径（`scale = 格高 × 150/512 ÷ 108`）会让卡渲成
`3.3313 × 384 × 150/512` = **374.76 px** 高、压进条里 **29.03 px** —— **与实拍矛盾** ⇒ **不改**
（这一笔也不需要动 `Shell/CollectionWindow.cs`，本代理本来也在黑名单内）。

---

## 二、第 2 件：`HandLayout.Place` → `CardView.TweenPose`（已在盘上，只补了缺口）

- 现状：`Hand/HandLayout.cs:845`（不补间那一支 `KillPoseTween + SetPose`）/ `:880-882`
  （`InverseTransformPoint` 后 `c.TweenPose(local, angle, scale, RelayoutDuration, Ease.OutQuad)`）；
  `J3` 那份 `HandPoseTween` 已删；`Core/CardTween.ToPose` 已由 `A11` 整段删除（退役注 `Core/CardTween.cs`）。
- `A3` 的 9 条在 `Editor/CardBaseDemo.cs:3760 AssertHandPoseTween`（断终值、世界位、`BaseScale`、两个掐补间口）。
- 🔴 **本批补的那一条（A 组）**：`BaseScale` 在**补间半程**就已经是 `start + 0.75 × (want − start)` ——
  `DOScale` 直写 `localScale` 时它**整段停在起点**，所以这一格**结构上不可能靠巧合成立**；
  顺带把「局部角」也放在**半程**上断（世界欧拉版差 `0.75 × 30° = 22.5°`，终值差 30°）。

---

## 三、第 3 件：三组断言（`Editor/HandAssertE8.cs`）

| 组 | 断言 | 灭自证（结构上不可能两边一起绿） |
| --- | --- | --- |
| **A 旋转 + 缩放所有权** | 前提（起止角差 >1°）· ①半程局部角 = `start + 0.75Δ` · ★②半程 `BaseScale` = OutQuad 半程值 · ③终值局部角 · ④世界位仍落在槽位 · ⑤终点 `BaseScale` · ⑥`localScale = BaseScale × HighlightMul` | ①③ 与「世界欧拉版」差 22.5°/30°；②⑤ 与「直写 localScale」差整段缩放；④ 与「不 `InverseTransformPoint`」差父件 2× + 30° |
| **B 重排闸** | 前提 3 条 · ★①开闸 · ★②闸长 0.2 s（±15%）· ★③闸内无新 hover · ★④闸内抓不起来 · ⑤半程仍开 · ★⑥过 0.2 s 自松 · ★⑦⑧（对照）闸放开后同样两步必须成立 · 收尾 | ③④ 与 ⑦⑧ **互斥**：闸写成「永远禁止」⇒ ③④ 绿、⑦⑧ 红；闸写成「不存在」⇒ 反过来 |
| **C 回弹 = 普通重排** | 前提 6 条（含「角度真的变了」>1.5°）· ★①从当前位置起补（起点离终点 >1 世界单位）· ★②半程位移 **0.75** · ★③**正好 0.2 s** 到位 · ★④带角度 | ②钉**形状**（Sequence 层再弯一次 ⇒ 0.9375；Linear ⇒ 0.5）、③钉**时长**（旧的 0.22 s 序列 ⇒ 0.2 s 只走 91%）、④钉**角度**（旧序列没有角度 ⇒ 卡停在拖拽前的角） |
| **D `initialCardScale` 口径** | ★①卡区底 == 张数条框顶 · ★②卡区高 = 格高 − 张数条高 · ★③卡区必须比 `150/512` 直译值（374.76 px）**矮 ≥20 px** | ①③：把 `CardsCardRect` 改成整格（384）⇒ 压条 38.27 px ⇒ 红；②：少减/多减一格 ⇒ 红 |

**判据出处**（全部一手，写在每组方法头）：
`CardScript__UpdateMinionInHandPosition.c:33/43/52`（`DOLocalRotate` 局部 / `DOScale` / `DOMove` 世界，无 `SetEase`）·
`PlayerHand._MoveCardsInHandToPosition_d__76__MoveNext.c:33`（只在 `selectedCard == -1` 置 `isDoingAnim`）`/44-47`（`Toggle(false)`）
`/104-106`（`WaitForSeconds(timeToPositionCard)`）`/126-127`（`SetColliderInHand()` ⇒ `Toggle(true)`）·
`BattleManager__Update.c:210/220-223/246`（回弹 = `MoveBackToHand` 只改状态 + 两条 `MoveCardsInHandToPosition(-1,null)`）。

**宿主要求（给调度台挂 `Editor/BattleScene.cs` 时）**：`HandAssertE8.Run()` **零前提** —— 不读 `BattleDriver`、
不读场景/存档；自己在 `Run()` 里把 `CardTween.Mode` 拨成 `Manual`、`CardTween.LinkEnabled` 拨成 `false`，
`finally` 原样还原（可以插在别的段中间）。收尾全部 `DestroyImmediate`，不留补间/对象。

---

## 四、没查清 / 没做（如实）

1. 🔴 **一条断言都没跑过**（红线：本代理不许 `-executeMethod`）。覆盖它的宿主只有它自己
   （`-executeMethod HandAssertE8.Run`），或挂进 `BattleScene.Run` / `CardBaseDemo.Run` 的末尾。
2. ⚠️ **D 组断的是「口径几何」不是「运行时渲出来的像素」** —— 后者只有 `CollectionScene.Run` 的版面断言
   （量渲染图）能断；本批没碰 `Shell/**`、也没跑场景。
3. ⚠️ **`CardInteraction.PickUp` 的「保持局部角」没有专门断言**（`CardInteraction.cs:615` 传的是
   `localEulerAngles.z`；C 组只推进到松手之后，拿起那条 0.1 s 补间在断言里**从不被推进**）。
4. ⚠️ B 组的闸只在 `hand.animateRelayout == true` 时存在（批处理出厂 false）⇒ 断言里手工打开；
   真机上「每次出牌/抽牌后 0.2 s 手牌不可点」的手感仍需真 Play（`J3` §五·6 已记）。

---

## 五、顺手发现（本批**没改**，供调度台分流）

1. 📌 `Editor/CardBaseDemo.cs:234-248` 与 `Editor/BattleScene.cs:5026` 那两条「回弹到第 0 位 / 回弹补间走完」
   **只断终态** ⇒ 对「0.22 s 自造序列」与「普通重排」两版**都成立**（`J3` §3.4 已指出）。C③ 补上了这一格。
2. 📌 原版的 `initialCardScale` 语义（px/卡单位）与我们的 world-scale 口径**是两套量纲** ——
   `资料/任务线/界面线/任务总表.md` 那一行仍写「`✗` 未做」，建议改成
   「✅ 判据查清：`150/512` 是 px/卡单位，**直译会压条 29 px、与实拍矛盾 ⇒ 不改**；断言见 `HandAssertE8.D`」。
   （⛔ 本代理不许写任务总表，留给主对话。）
3. 📌 `CollectionCard` 那张 prefab 的 `cardUI` rect 是 **0×0**（stretch + `sd(−350,−512)`）——
   以后再遇到「拿 prefab 尺寸反推渲染尺寸」的地方，先问一句「它是不是 0×0 的模板位」。

---

## 六、改动清单

```text
新增  Unity/MyGame/Assets/CardPresentation/Editor/HandAssertE8.cs       (+ 约 400 行)
新增  Unity/MyGame/Assets/CardPresentation/Editor/HandAssertE8.cs.meta
新增  资料/普查产出_1024b/E8_initialCardScale与手牌.md
未改  任何生产文件（Hand/** · CardInteraction.cs · Shell/** · Core/** 全部零改动）
```

`TMPDIR=/tmp/wf_e8 bash d:/4/Unity/工具/typecheck.sh` ⇒ **运行时 0 / 编辑器 0**（最后一次改动之后）。
