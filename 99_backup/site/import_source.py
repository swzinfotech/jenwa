"""Import the supplied official-site archive without third-party dependencies."""
from pathlib import Path
from urllib.parse import urlparse, unquote
from hashlib import sha256
import json, re, shutil
from source_parser import parse, root as archive

site=Path(__file__).resolve().parent
assets={}; missing=[]; pages=set()
def source_url(file):
    rel=file.relative_to(archive).as_posix()
    if rel=='index.html': return 'https://www.jenwa.com.tw/'
    folder,name=rel.split('/')
    if folder=='products':
        match=re.search(r'info_id_(\d+)',name)
        if match: return f'https://www.jenwa.com.tw/products/info.php?id={match[1]}'
    if folder=='workshow':
        match=re.search(r'info_id_(\d+)(?:_second_id_(\d+))?',name)
        if match: return f'https://www.jenwa.com.tw/workshow/info.php?id={match[1]}'+(f'&second_id={match[2]}' if match[2] else '')
    match=re.search(r'other_select_index_id_(\d+)',name)
    if match: return f'https://www.jenwa.com.tw/paper/other_select_index.php?id={match[1]}&title_id=1834&group_id=71'
    if name=='other_page_id_1857.html': return 'https://www.jenwa.com.tw/paper/other_page.php?id=1857'
    if name=='promotions_index_id_1986.html': return 'https://www.jenwa.com.tw/paper/promotions_index.php?id=1986'
    return 'https://www.jenwa.com.tw/'+rel
def source(file):
    pages.add(file.relative_to(archive).as_posix())
    return {'file':file.relative_to(archive).as_posix(),'url':source_url(file)}
def asset(src,file):
    if src.startswith(('http:','https:','//')):
        u=urlparse('https:'+src if src.startswith('//') else src)
        local=archive/'_external'/u.netloc/unquote(u.path.lstrip('/'))
        url='https://'+u.netloc+u.path
    else:
        local=(file.parent/unquote(src)).resolve()
        rel=local.relative_to(archive).as_posix()
        url='https://'+rel[len('_external/'):] if rel.startswith('_external/') else 'https://www.jenwa.com.tw/'+rel
    key=local.relative_to(archive).as_posix()
    dest='assets/official/'+sha256(key.encode()).hexdigest()[:10]+'-'+local.name
    if key not in assets:
        entry={'archive_path':key,'url':url,'local':dest,'used_by':[]}
        assets[key]=entry
        if local.is_file():
            (site/dest).parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(local,site/dest)
        else: missing.append(entry)
    assets[key]['used_by']=sorted(set(assets[key]['used_by']+[file.relative_to(archive).as_posix()]))
    return dest
def imgs(node,file): return list(dict.fromkeys(asset(n.attrs['src'],file) for n in node.all('img') if n.attrs.get('src')))
def edit(key):
    file=archive/f'paper/other_select_index_id_{key}_title_id_1834_group_id_71.html'
    return file,parse(file).all(cls='edit')[0]
data={}
home=archive/'index.html'; doc=parse(home)
data['banners']=[asset(n.attrs['src'],home) for n in doc.all('img')[1:4]]
data['history']=[{'title':n.all(cls='about-year-T')[0].text(),'text':n.text()[len(n.all(cls='about-year-T')[0].text()):].strip(),'source':source(home)} for n in doc.all(cls='about-year1')]
data['purchaseImages']=imgs(doc.all(cls='about-process')[0],home)
data['contact']={'phone':'07-783-0770','mobile':'0911-608-070','fax':'07-783-0780','taxId':'91445973','email':'container@jenwa.com.tw','address':'高雄市大寮區三隆路39巷61號','line':'https://line.me/ti/p/~@mop1712q','facebook':'https://www.facebook.com/jenwagroup/','map':'https://goo.gl/maps/WDjXHa98a2r','source':source(home)}
file,node=edit(206)
data['structure']={'items':[{'title':n.all(cls='P-A-name')[0].text(),'text':n.all(cls='P-A-int-2')[0].text()} for n in node.all(cls='P-A-int-1')],'images':imgs(node,file),'source':source(file)}
file,node=edit(209)
data['process']=[{'title':n.all(cls='P-D002')[0].text(),'text':n.text()[len(n.all(cls='P-D002')[0].text()):].strip(),'image':imgs(n,file)[0],'source':source(file)} for n in node.all(cls='P-D001')]
file,node=edit(522)
data['advantages']={'images':imgs(node,file),'source':source(file)}
file,node=edit(207)
data['differences']=[{'title':n.all(cls='P-B-03-T')[0].text(),'text':n.text()[len(n.all(cls='P-B-03-T')[0].text()):].strip(),'image':imgs(n,file)[0],'source':source(file)} for n in node.all(cls='P-B-03')]
file,node=edit(208)
data['patents']=[{'number':number,'image':img,'source':source(file)} for number,img in zip(['M408598','M431918','M552518'],imgs(node,file))]
file=archive/'paper/other_page_id_1857.html'; node=parse(file).all(cls='edit')[0]
data['faq']=[{'title':group.all(cls='QA-title-W')[0].text(),'items':[{'question':re.sub(r'^Q\d+\.\s*','',n.all(cls='QA-Q')[0].text()),'answer':n.all(cls='QA-A')[0].text()} for n in group.all(cls='QA-0')],'source':source(file)} for group in node.all(cls='type')]
data['cases']=[]; data['surveys']=[]
for file in sorted((archive/'products').glob('info*title_id_.html'),key=lambda f:int(re.search(r'info_id_(\d+)',f.name)[1]),reverse=True):
    doc=parse(file); title=doc.all(cls='mobile_product_name')[0].text()
    pictures=imgs(doc.all(cls='product_pic')[0],file)
    item={'id':re.search(r'info_id_(\d+)',file.name)[1],'title':title,'images':pictures,'specification':' '.join(n.text() for n in doc.all(cls='txt_box')).strip(),'source':source(file)}
    info=doc.all(cls='accordion-panel')
    item['description']=info[0].text() if info else ''
    if title.startswith('場勘'): data['surveys'].append(item)
    else:
        item['region']='north' if title.startswith(('新竹','苗栗','宜蘭')) else 'central' if title.startswith(('台中','彰化','南投','雲林')) else 'east' if title.startswith(('台東','花蓮')) else 'islands' if title.startswith(('金門','外島','小琉球')) else 'south'
        data['cases'].append(item)
data['models']=[]
for file in sorted((archive/'workshow').glob('info*second_id*.html')):
    doc=parse(file); node=doc.all(cls='show_content')[0]
    active=doc.all(cls='active'); title=next((n.text() for n in active if n.tag=='li'),'模型')
    data['models'].append({'id':re.search(r'second_id_(\d+)',file.name)[1],'budget':node.all('h2')[0].text(),'title':title,'images':imgs(node.all(cls='pic-list')[0],file),'source':source(file)})
file=archive/'paper/promotions_index_id_1986.html'; doc=parse(file)
data['comparisonArticle']={'title':doc.all('title')[0].text().split('-易立構SRC')[0],'video':'https://www.youtube.com/watch?v=LfrT94btz-g','embed':doc.all('iframe')[0].attrs['src'],'source':source(file)}
data['constructionNote']='工法、結構配置及用料依現場環境、結構安全、材料供應與相關法規，由專業技術團隊評估調整。實際規格、費用及施作範圍以個案確認為準。'
(site/'official-content.json').write_text(json.dumps(data,ensure_ascii=False,indent=2),encoding='utf-8')
(site/'source-manifest.json').write_text(json.dumps({'source':'使用者提供的全站下載html','pages':sorted(pages),'assets':list(assets.values()),'missing':missing},ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps({'cases':len(data['cases']),'faq':sum(len(g['items']) for g in data['faq']),'models':len(data['models']),'surveys':len(data['surveys']),'process':len(data['process']),'images':len(assets),'missing':missing},ensure_ascii=False))
