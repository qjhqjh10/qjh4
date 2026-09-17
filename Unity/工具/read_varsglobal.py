# 读 VarsGlobal 整表：字段名（来自签名桩的声明顺序）+ 值（来自 sharedassets0.assets 的原始字节）
#
# 为什么以前说「拿不到」：这个资源**不在解包整理里**，而且它所在的 .assets 没有 type tree，
# UnityPy 读不出字段名 ⇒ 抽取管线漏了它。但**声明的顺序在签名桩里**，字节在文件里，
# 两边一对就能还原整表（2026-09-17 实测：两份安装的副本逐字节一致）。
import io, re, struct, os

STUB = 'D:/2/Warpforge_code/Scripts/Assembly-CSharp/VarsGlobal.cs'
ASSET = 'D:/2/unity_run_ref/Warpforge_Data/sharedassets0.assets'
# deckSize(int=30) 1.7 2.5 30.0 25.0 18.0 maxSecToReconnect(int=30) —— 用它锚定字段数据起点
ANCHOR = struct.pack('<i', 30) + struct.pack('<5f', 1.7, 2.5, 30.0, 25.0, 18.0) + struct.pack('<i', 30)

src = io.open(STUB, encoding='utf-8').read()
fields = []
for m in re.finditer(r'^\s*public\s+([A-Za-z_][\w\.\[\]]*)\s+([A-Za-z_]\w*)\s*;', src, re.M):
    t, n = m.group(1), m.group(2)
    fields.append((t, n))

b = io.open(ASSET, 'rb').read()
off = b.find(ANCHOR)
print('锚点偏移 0x%X，签名桩里可解析的字段 %d 个' % (off, len(fields)))
print('-' * 58)

p = off
for t, n in fields:
    if t == 'int':
        v = struct.unpack_from('<i', b, p)[0]; p += 4
    elif t == 'float':
        v = struct.unpack_from('<f', b, p)[0]; p += 4
    elif t == 'bool':
        v = b[p]; p += 4          # Unity 序列化 bool 占 1 字节但按 4 对齐
    elif t == 'Vector2':
        v = struct.unpack_from('<2f', b, p); p += 8
    elif t == 'Vector3':
        v = struct.unpack_from('<3f', b, p); p += 12
    elif t == 'Vector4' or t == 'Color':
        v = struct.unpack_from('<4f', b, p); p += 16
    elif t == 'int[]':
        ln = struct.unpack_from('<i', b, p)[0]; p += 4
        v = list(struct.unpack_from('<%di' % ln, b, p)); p += ln * 4
    elif t == 'string':           # 4 字节长度 + 字节 + 对齐到 4
        ln = struct.unpack_from('<i', b, p)[0]; p += 4
        if ln < 0 or ln > 512:
            print('  (string 长度可疑 %d，后面按错位处理，就此打住)' % ln); break
        v = b[p:p + ln].decode('utf-8', 'replace'); p += (ln + 3) // 4 * 4
    else:
        print('  ⚠️ 遇到没处理的类型 %s %s —— 后面会错位，就此打住' % (t, n)); break
    print('  %-34s %-8s = %s' % (n, t, v))
