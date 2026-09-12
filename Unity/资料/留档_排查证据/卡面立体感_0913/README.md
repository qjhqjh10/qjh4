# 卡面立体感（「3DLit」）排查留档 —— 2026-09-13

> 起因：用户问「原版的卡牌有一种 3D 立体感，角色的部分（身体/武器）**超出了卡框**但仍在方形画布里，
> 这是怎么做到的？我们做出来了没有？」—— 并要求拿**部队卡片文件夹**里的成品卡图对比。

## 结论（一句话）

**立绘贴图的 alpha 通道就是「角色的抠图轮廓」**：单位卡 91–97% 全透明、亮的那块形状正是角色；
战术卡 0% 透明（整幅矩形插画 → 战术卡没有越界效果）。
做法是**同一张立绘用两次**：底层忽略 alpha（完整插图，垫在卡框下、补上拱窗里的背景），
前景层用真 alpha（角色，盖在卡框上面）。⚠️ 卡框的拱窗**自己也是全透明的**，所以必须有底层。

⚠️ **但游戏内不是这个结构**：游戏里 `Front` 只有一层立绘 + 一层卡框（卡框渲染在上）；
「立绘压卡框」是**官方 PnP 印刷卡**（`D:/2/Warpforge部队卡片/`，900×1200）的合成顺序。
**用户要的是 PnP 那个观感**，我们照它做。

⚠️ **我们以前把 alpha 当「解码残渣」写成 255 了** —— 所以这个效果一直没做出来。
`工具/import_original_art.py` 已改成**保留 + 二值化**（>127→255）。
**二值化是关键**：不二值化的话，那片 25% 的半透明渐变会让卡面**糊成彩色马赛克**。

## 图

| 文件 | 是什么 |
|---|---|
| `1_Guilliman卡图_alpha通道_角色抠图.png` | 从 bundle 直接读出的 alpha —— **一眼看出是角色轮廓**（光环/头/金鹰肩甲/剑/旗帜） |
| `2_Guilliman卡图_RGB_完整插图.png` | 同一张贴图的 RGB —— 完整插图（角色 + 天空 + 火 + 石墙） |
| `3_我们的督军卡_两层效果.png` | 我们做出来的效果（`CardFaceProbe.Run` 放大渲的单卡；透明区的压缩残渣在这种放大下才看得见） |
| `4_我们的单位卡_两层效果.png` | 同上，单位卡 |

对照原版：`D:/2/Warpforge部队卡片/Ultramarines/1督军/UPDATED_Warpforge_01_Roboute-Guilliman.png`

## 怎么复现上面两张证据图

```python
import UnityPy, os
AA = "d:/2/Warhammer 40k Warpforge/Warpforge_Data/StreamingAssets/aa/StandaloneWindows64"
env = UnityPy.load(os.path.join(AA, "spacemarinesultramarinescardassets_assets_all.bundle"))
for o in env.objects:
    if o.type.name != "Texture2D": continue
    d = o.read_typetree()
    if "Guilliman" not in d.get("m_Name", ""): continue
    img = o.read().image.convert("RGBA")
    img.save("tex_rgba.png")                    # RGB = 完整插图
    img.getchannel("A").save("tex_alpha.png")   # alpha = 角色抠图
```

## 还没做的（留给后续会话）

1. **`AutoCardRotation` 的整卡刚性倾摆**（±10°，**跟位移**不跟指针）—— 游戏里「3D 感」的主要来源之一
2. **SDF 光影层** `Card Highlight And Shadow`（4.4281²，sprite 运行时生成）
3. 战场单位的**真 3D 网格** + MatCap（`Card 3D WH40k.obj`、材质 `Card 3d Stealth`）
4. ⚠️ 另一条思路 `Shaders/FrameCutout.shader`（卡框按遮罩挖洞、立绘只画一次，**没有重影**）
   没调通：遮罩采样错 → 插图碎成马赛克。`CardView.UseFrontLayer = false` 可切过去接着调
5. 透明区（alpha=0）的 RGB 是**压缩残渣**，放大 3 倍以上看得出来（游戏内卡只有 137–165 px，看不见）
