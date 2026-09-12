# 卡面透明层：白边/黑边 + 一个 Unity 的坑（2026-09-13）

> 起因：用户问「**我说的黑边、白边是卡牌没有处理好透明层的时候出现的，现在你是否处理好了？**」
> 结论：**当时没处理好，是我们做出来的**；这一轮查清并修掉了（连着一个 Unity 的坑一起）。

## 一句话账

| | 事实 |
|---|---|
| **源图** | 插图的 alpha 是**软遮罩**（1024² 有 255 档）；**透明/半透明那片的 RGB 是背景残渣**（`SM_UM_inf_Heavy Intercessor` 的 alpha 1–64 档 RGB 均值 **(167,167,169)** = 天空/火光） |
| **源图有没有白边** | **没有**。原始 bundle 贴图里 alpha=255 的边界是 **(45,48,64)** ≈ 内部 (56,53,63) |
| **我们第 21 轮做了什么** | 把 alpha **二值化**（>127→255），为的是压掉「同一张图叠两次糊成马赛克」 |
| **后果** | 二值化把**带残渣色的半透明像素变成不透明** → 角色轮廓外多出一圈**亮边**：实测边界 **(98,101,112)** vs 内部 **(52,52,67)**，亮了近一倍 |
| **现在的做法** | **软 alpha 原样保留** + 把角色外一圈 8 px 的 RGB **渗成角色自己的颜色**（`工具/import_original_art.py` 的 `bleed_edge_rgb`）→ 残渣色被替换，马赛克和白边一起消失 |

## ⚠️ 第二层坑：Unity 的 `alphaIsTransparency`

修完 RGB 之后仍然是一片**彩色马赛克** —— 查下来是 **Unity 的 `alphaIsTransparency`**：
它开着时会把**透明区的 RGB 用「最近邻填充」补上**，而最近邻填充的划分边界**正好是多边形格子**，
放大看就是马赛克。**软 alpha 才触发，二值时不触发**（所以第 21 轮没撞上）。

⇒ 卡图的 `.meta` 必须 `alphaIsTransparency: 0`（Alpha 的语义由我们自己的两个 shader 控制）。
`工具/import_original_art.py` 里加了 `fix_art_meta()`；⚠️ `.meta` 是 Unity 生成的，
**第一次导入（还没开过 Unity）时它不存在**，所以要**开过一次 Unity 后再跑一遍脚本**（脚本幂等）。

## 图

| 文件 | 是什么 |
|---|---|
| `Unity坑_alphaIsTransparency.png` | **左**＝二值 alpha（Unity 加载到的贴图是干净插画）／**右**＝软 alpha + `alphaIsTransparency=1`（Unity 把透明区填成了多边形马赛克）。这张就是那个坑的直接证据 |
| `修复后的卡面.png` | 修完之后的单卡渲染（软 alpha ✓ 无白边 ✓ 无马赛克 ✓） |
| `我们vs原版成品卡.png` | 我们渲的基里曼 vs `D:/2/Warpforge部队卡片/…/UPDATED_Warpforge_01_Roboute-Guilliman.png`（原版 PnP 成品卡），用来验收卡面组装 |

## 全量验证（改完跑的）

- 1129 张有内容的插图里：**「贴边一圈 vs 往内 3 px」的色差 > 30 的 = 0 张**（没有硬边）。
- 剩下的「边界比内部亮」是**插画自带的柔和辉光**（渐变，不是边）—— 用「渐变 vs 硬边」的判据分开的。
- 自检四条全绿：`RuleEngineTest 841/841` · `BattleScene 283/0` · `DeckScene 61/61` · `CardBaseDemo` 全过。

## 复现

```python
# 量「有没有硬边」：贴边那一圈 vs 往内 3 px 的颜色差（> 30 就是硬边）
from PIL import Image; import numpy as np
a = np.asarray(Image.open('art_xxx.png').convert('RGBA')).astype(int)
al, rgb = a[:,:,3], a[:,:,:3]
solid, hollow = al >= 160, al < 10
ring0 = solid & reduce(np.logical_or, [np.roll(np.roll(hollow,dy,0),dx,1) for dy,dx in ((-1,0),(1,0),(0,-1),(0,1))])
s = solid.copy()
for _ in range(3):  # 腐蚀 3 次 → 往内 3px 的圈
    e = np.zeros_like(s); e[1:-1,1:-1] = s[:-2,1:-1]&s[2:,1:-1]&s[1:-1,:-2]&s[1:-1,2:]; s &= e
ring3 = np.zeros_like(solid); ring3[1:-1,1:-1] = s[:-2,1:-1]&s[2:,1:-1]&s[1:-1,:-2]&s[1:-1,2:]
ring3 &= ~s
print(abs(rgb[ring0].mean(0) - rgb[ring3].mean(0)).mean())   # > 30 = 硬边
```
