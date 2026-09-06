from html.parser import HTMLParser
from pathlib import Path
import json, re, sys
sys.stdout.reconfigure(encoding='utf-8')
class Node:
    def __init__(self,tag='',attrs=()): self.tag=tag; self.attrs=dict(attrs); self.children=[]
    def all(self,tag=None,cls=None):
        out=[]
        for n in self.children:
            if isinstance(n,Node):
                if (not tag or n.tag==tag) and (not cls or cls in n.attrs.get('class','').split()): out.append(n)
                out.extend(n.all(tag,cls))
        return out
    def text(self):
        if self.tag in ('script','style','head'): return ''
        return re.sub(r'\s+',' ',' '.join(n.text() if isinstance(n,Node) else n for n in self.children)).strip()
class Parser(HTMLParser):
    def __init__(self,text):
        super().__init__(convert_charrefs=True); self.root=Node(); self.stack=[self.root]; self.feed(text)
    def handle_starttag(self,tag,attrs):
        n=Node(tag,attrs); self.stack[-1].children.append(n)
        if tag not in ('img','br','hr','input','meta','link','source','wbr','area','embed','param','col','base'): self.stack.append(n)
    def handle_endtag(self,tag):
        for i in range(len(self.stack)-1,0,-1):
            if self.stack[i].tag==tag: self.stack=self.stack[:i]; break
    def handle_data(self,s): self.stack[-1].children.append(s)
root=Path(__file__).resolve().parent.parent/'全站下載html'
def parse(file): return Parser(file.read_text(encoding='utf-8')).root
if __name__=='__main__':
    files=[root/p for p in sys.argv[1:]] if len(sys.argv)>1 else sorted(root.rglob('*.html'))
    for file in files:
        doc=parse(file); title=doc.all('title')[0].text()
        edits=doc.all(cls='edit')
        print(json.dumps({'file':file.relative_to(root).as_posix(),'title':title,'edits':[{'text':n.text(),'images':[i.attrs for i in n.all('img')],'links':[{'text':a.text(),'href':a.attrs.get('href')} for a in n.all('a')],'iframes':[i.attrs for i in n.all('iframe')]} for n in edits]},ensure_ascii=False))
