#!/usr/bin/env python
# W58-K12 step 3: scan the 135 compiler-generated / nested methods that the naive
# file-name mapping missed (lambdas: <X>b__N, DisplayClass). Same +0x40 check.
import io, os, re, collections

DUMP = r"d:/2/tools/il2cpp_out/dump.cs"
DEC  = r"d:/2/tools/decomp_full"
def read(p): return io.open(p, "r", encoding="utf-8", errors="replace").read().splitlines()

lines = read(DUMP)
cls_re = re.compile(r'^(?:public |private |internal |protected )?(?:static |sealed |abstract )*(?:class|struct|interface|enum)\s+([A-Za-z0-9_\.<>`]+)')
cur=None; methods=[]
for i,ln in enumerate(lines,1):
    m=cls_re.match(ln)
    if m: cur=m.group(1); continue
    s=ln.strip()
    if cur and s.endswith("{ }") and "(" in s and not s.startswith("//"):
        nm=re.search(r'([A-Za-z0-9_<>\.`]+)\s*\(', s)
        if nm: methods.append((cur,nm.group(1),s,i))

sig=[m for m in methods if "RawCardScript" in m[2]]
files=set(os.listdir(DEC))

def mangle(x):
    return x.replace("<","_").replace(">","_").replace("+",".")

def cands(cls,name):
    c=mangle(cls); n=mangle(name)
    out=[c+"__"+n+".c", c.replace(".","__")+"__"+n+".c"]
    # generic suffix stripping for things like DraftEventBase<TS
    c2=re.sub(r'`.*','',c)
    out += [c2+"__"+n+".c"]
    return [x for x in out if x in files]

miss=[m for m in sig if not cands(m[0],m[1])]
print("signature methods:",len(sig),"still missing:",len(miss))
for m in miss[:20]: print("   MISS",m[0],"::",m[1])

pat=re.compile(r'\+\s*0x40\b')
hits=0
for cls,name,sigline,ln in sig:
    for fn in cands(cls,name):
        body=read(os.path.join(DEC,fn))
        for j,bl in enumerate(body,1):
            if pat.search(bl):
                hits+=1
                print("HIT %s:%d  %s\n    sig(dump.cs:%d): %s"%(fn,j,bl.strip(),ln,sigline))
print("total hits in lambda/nested methods:",hits)
