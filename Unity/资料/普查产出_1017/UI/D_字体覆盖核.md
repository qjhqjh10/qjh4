# 字体字形覆盖核（2026-10-17 · 主对话现核）

> 用途：回答 `N42/N44` 的「日语/韩语/俄语字体覆盖未验」。**本文件只证「字体源里有没有这些字形」**，
> ⛔ **不等于**「真机渲染已经验过」（渲染还受 TMP 图集、fallback 链、材质影响）。

## 一、工具（已装）

```bash
python -m pip install --quiet fonttools      # 2026-10-17 装, 版本 4.67.0
```
> 按 `CLAUDE.md` 铁律 8：缺工具可直接装；装在用户 Python 环境（不是工程里）。

复核脚本（原样可跑）：
```python
from fontTools.ttLib import TTFont
f = TTFont(r'd:/4/Unity/MyGame/Assets/CardPresentation/Fonts/NotoSerifCJK-Regular.ttf', fontNumber=0)
cmap = set()
for t in f['cmap'].tables:
    if t.isUnicode(): cmap |= set(t.cmap.keys())
def cov(r): return sum(1 for c in r if c in cmap)
print('hiragana', cov(range(0x3041,0x3097)), '/', 0x3097-0x3041)
print('katakana', cov(range(0x30A1,0x30FB)), '/', 0x30FB-0x30A1)
print('hangul',   cov(range(0xAC00,0xD7A4)), '/', 0xD7A4-0xAC00)
print('latin',    cov(range(0x20,0x7F)), '/', 0x7F-0x20)
print('cyrillic', cov(range(0x400,0x500)), '/', 0x100)
print('cjk',      cov(range(0x4E00,0xA000)), '/', 0xA000-0x4E00)
```

## 二、结果（`NotoSerifCJK-Regular.ttf`，43,029 个码位）

| 区块 | 覆盖 |
|---|---|
| 平假名 | **86 / 86** |
| 片假名 | **90 / 90** |
| 韩文音节 | **11,172 / 11,172** |
| 拉丁基本区 | **95 / 95** |
| 西里尔区（U+0400–04FF） | 66 / 256（**但俄语 33 个大小写字母 + Ё/ё 全部在内 = 0 缺**） |
| CJK 统一表意 | **20,971 / 20,992** |
| **波兰变音符** | `ĄĆĘŁŃÓŚŹŻ` 等 **CJK 字体里没有** |
| **捷克变音符** | `ÁČĎÉĚÍŇÓŘŠŤÚŮÝŽ` 等 **CJK 字体里没有** |

⇒ **波兰/捷克走拉丁主字体**：`Resources/Fonts/Pragati-Regular SDF.asset` 的 `m_CharacterTable` 现读 **250 字**，
含 `m_Unicode: 261 (Ą)` · `263 (Ć)` · `280 (Ę)` 等 ⇒ 这一档由 Pragati 覆盖。
⇒ **日/韩/俄走 CJK 兜底**：`TmpFont.EnsureCjkFallback()` 运行期把 CJK 挂进拉丁面的 `fallbackFontAssetTable`
（纯内存、不落盘，见 `A1024③` 那半）。

## 三、结论与边界

- ✅ **判据层**：日/韩/俄所需的字形**在源字体里都有**（见上表）。
- ⚠️ **仍未验**：真机渲染（TMP 动态图集按需生成、fallback 顺序、材质/描边）—— 归属真机验收清单。
- ⚠️ 土耳其语（`tr`）不在 `AvailableLanguages` 枚举里（只有数据列）⇒ 不涉 UI 字体选择。
