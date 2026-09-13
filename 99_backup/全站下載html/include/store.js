
//線上簡訊發送
function SendMsg(){
	if(!document.send_msg.name.value){
		alert("聯絡人未填寫");
		return false;
	}
	if(!document.send_msg.tel.value){
		alert("電話未填寫");
		return false;
	}
	if(!document.send_msg.checknum_2.value){
		alert("驗證碼未填寫");
		return false;
	}
}
//首頁估價單
function SendAllStore(){
	
	if(!document.send_all_form.name.value){
		alert("客戶名稱必填");
		return false;
	}
	if(!document.send_all_form.phone.value){
		alert("手機必填");
		return false;
	}
	if(!document.send_all_form.tel.value){
		alert("電話必填");
		return false;
	}
	if(!document.send_all_form.city.value){
		alert("招標區域必填");
		return false;
	}
	if(!document.send_all_form.area.value){
		alert("招標鄉鎮必填");
		return false;
	}
	if(!document.send_all_form.address.value){
		alert("招標地址必填");
		return false;
	}
	if(!document.send_all_form.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
	
}
//產品問與答
function SendProAsk(){
	if(!document.send_ask.name.value){
		alert("提問者稱呼未填寫!");
		return false;
	}
	if(!document.send_ask.qus.value || document.send_ask.qus.value=='提出問題，不得超過100字'){
		alert("問題未填寫!");
		return false;
	}
	if(!document.send_ask.mail.value){
		alert("mail未填寫!");
		return false;
	}
	if(!document.send_ask.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
}
function SavePayMethod(key){//購物車選取付款方式
	var xmlHttp;
	var url='pay_method_ajax.php?act=show_data&key='+key;
	if (window.ActiveXObject) { 
  		xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
	}else if(window.XMLHttpRequest){ 
   		xmlHttp = new XMLHttpRequest();
 	} 
	xmlHttp.open("GET",url,true);
	xmlHttp.send(null);
}
function total_ajax(id,pro_id,key,transport_price,transport_limit_price,size){//產品總計
	var xmlHttp;
	var url='total_ajax.php?act=show_total&id='+id+'&pro_id='+pro_id+'&useno='+key+'&transport_price='+transport_price+'&transport_limit_price='+transport_limit_price+'&size='+size;
	if (window.ActiveXObject) { 
  		xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
	}else if(window.XMLHttpRequest){ 
   		xmlHttp = new XMLHttpRequest();
 	} 
 	xmlHttp.onreadystatechange = function(){
  		if(xmlHttp.readyState == 4){
			data_array=xmlHttp.responseText;
            var arrayStr=data_array.split(",");  
   			document.getElementById('total_value').innerHTML = arrayStr[0];
			document.getElementById('transport_value').innerHTML = arrayStr[1];
			document.getElementById('total_price_'+pro_id+size).innerHTML = arrayStr[2];
			document.getElementById('all_total_price').innerHTML = arrayStr[3];
  		}
 	}
	xmlHttp.open("GET",url,true);
	xmlHttp.send(null);
}

function area_ajax(id,key){//地區
	var xmlHttp;
	var url='area_ajax.php?act=show_city&id='+id+'&city='+key;
	if (window.ActiveXObject) { 
  		xmlHttp = new ActiveXObject("Microsoft.XMLHTTP");
	}else if(window.XMLHttpRequest){ 
   		xmlHttp = new XMLHttpRequest();
 	} 
 	xmlHttp.onreadystatechange = function(){
  		if(xmlHttp.readyState == 4){
   			document.getElementById('area_value').innerHTML = xmlHttp.responseText;
  		}
 	}
	xmlHttp.open("GET",url,true);
	xmlHttp.send(null);
}
//產品訂購
function StoreOrderPro(){
	if(!document.store_form.order_name.value){
		alert("訂購人未填寫!");
		return false;
	}
	if(!document.store_form.order_tel.value){
		alert("聯絡電話未填寫!");
		return false;
	}
	if(!document.store_form.order_email.value){
		alert("Email未填寫!");
		return false;
	}
	RegExpPtn = /\w[\w.-]+@[\w-]+(\.\w{2,})+/gi;
	if(!RegExpPtn.test(document.store_form.order_email.value)){
		alert("Email格式不正確"); 
		return false;
	}
	if(!document.store_form.to_name.value){
		alert("收件人未填寫!");
		return false;
	}
	if(!document.store_form.to_tel.value){
		alert("聯絡電話未填寫!");
		return false;
	}
	if(!document.store_form.to_address.value){
		alert("地址未填寫!");
		return false;
	}
	if(!document.store_form.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
	if(!document.store_form.check_data.checked){
		alert("確認訂單資訊必需勾選");
		return false;
	}
}
//產品詢價
function StoreInquiryPro(){
	if(!document.store_form.keynote.value){
		alert("主旨必需填寫");
		return false;
	}
	if(!document.store_form.askinfo.value){
		alert("詢問內容必需填寫");
		return false;
	}
	if(!document.store_form.name.value){
		alert("聯絡人必需填寫");
		return false;
	}
	if(!document.store_form.tel.value){
		alert("聯絡電話必需填寫");
		return false;
	}
	if(!document.store_form.company.value){
		alert("公司名稱必需填寫");
		return false;
	}
	if(!document.store_form.mail.value){
		alert("電子信箱必需填寫");
		return false;
	}
	if(!document.store_form.city.value){
		alert("國家地區必需填寫");
		return false;
	}
	if(!document.store_form.checknum.value){
		alert("驗證碼必需填寫");
		return false;
	}
	if(!document.store_form.check_data.checked){
		alert("確認詢價單資訊必需勾選");
		return false;
	}
}
//產品訂購如同訂購人
function SameOrder(){
	document.store_form.to_name.value=document.store_form.order_name.value;
	document.store_form.to_tel.value=document.store_form.order_tel.value;
}

//聯絡我們
function SendPriceStore(){
	if(document.sendsp_form.contact_is.value==0){
		if(!document.sendsp_form.name.value){
			alert("姓名未填寫!");
			return false;
		}
		if(!document.sendsp_form.tel.value){
			alert("電話未填寫!");
			return false;
		}
		if(!document.sendsp_form.phone.value){
			alert("手機未填寫!");
			return false;
		}
		if(!document.sendsp_form.msg.value){
			alert("留言內容未填寫!");
			return false;
		}	
	}
	if(!document.sendsp_form.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
}
//作品寄信給店家
function SendStore(){
	if(!document.send_form.name.value){
		alert("姓名未填寫!");
		return false;
	}
	if(!document.send_form.mail.value){
		alert("mail未填寫!");
		return false;
	}
	if(!document.send_form.address.value){
		alert("地址未填寫!");
		return false;
	}
	if(!document.send_form.tel.value){
		alert("電話未填寫!");
		return false;
	}
	if(!document.send_form.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
	if(!document.send_form.message.value){
		alert("訊息未填寫!");
		return false;
	}
}
//營業項目更多
function MoreItem(a,b){
	 if(a=='yes'){
		 document.getElementById('show_item'+b).style.display="";
		 document.getElementById('NoButton'+b).style.display="none";
		 document.getElementById('YesButton'+b).style.display="";
	 }else if(a=='no'){
	 	document.getElementById('show_item'+b).style.display="none";
		document.getElementById('NoButton'+b).style.display="";
		document.getElementById('YesButton'+b).style.display="none";
	 }
}
//匯款回報
function SendPrice(){
	if(!document.send_form.order_num.value){
		alert("訂單編號未填寫!");
		return false;
	}
	if(!document.send_form.account_id.value){
		alert("帳號末五碼未填寫!");
		return false;
	}
	if(!document.send_form.price.value){
		alert("匯款金額未填寫!");
		return false;
	}
	if(!document.send_form.pay_date.value){
		alert("匯款日期未填寫!");
		return false;
	}
	if(!document.send_form.name.value){
		alert("匯款者姓名未填寫!");
		return false;
	}
	if(!document.send_form.tel.value){
		alert("匯款者電話未填寫!");
		return false;
	}
	if(!document.send_form.checknum.value){
		alert("驗證碼未填寫!");
		return false;
	}
}
//會員註冊
function MemRegist(){
	if(!document.store_form.useno.value){
		alert("帳號未填寫!");
		return false;
	}
	RegExpPtn = /\w[\w.-]+@[\w-]+(\.\w{2,})+/gi;
	if(!RegExpPtn.test(document.store_form.useno.value)){
		alert("帳號Email格式不正確"); 
		return false;
	}
	if(!document.store_form.pwd.value){
		alert("密碼未填寫!");
		return false;
	}
	if(document.store_form.pwd2.value != document.store_form.pwd.value){
		alert("密碼兩次未相同!");
		return false;
	}
	if(!document.store_form.name.value){
		alert("中文姓名未填寫!");
		return false;
	}
	var CheckFlag;
	CheckFlag = false
	for(var i=0;i<store_form.sex.length;i++){
		CheckFlag = (CheckFlag || store_form.sex[i].checked);
	}
	if(!CheckFlag){
		alert("性別必需選取");
		return false;
	}	
	if(!document.store_form.tel.value){
		alert("電話未填寫!");
		return false;
	}
	if(!document.store_form.phone.value){
		alert("手機未填寫!");
		return false;
	}
	if(!document.store_form.address.value){
		alert("地址未填寫!");
		return false;
	}
	if(!document.store_form.is_agree.checked){
		alert("確認同意未勾選!");
		return false;
	}
}
//會員登入
function MemLogin(){
	if(!document.store_form.useno.value){
		alert("帳號未填寫!");
		return false;
	}
	if(!document.store_form.pwd.value){
		alert("密碼未填寫!");
		return false;
	}
}
//會員資料修改
function MemEdit(){
	if(!document.store_form.name.value){
		alert("中文姓名未填寫!");
		return false;
	}
	if(!document.store_form.tel.value){
		alert("電話未填寫!");
		return false;
	}
	if(!document.store_form.phone.value){
		alert("手機未填寫!");
		return false;
	}
	if(!document.store_form.address.value){
		alert("地址未填寫!");
		return false;
	}
}
//密碼修改
function ChangePwd(){
	if(!document.store_form.pwd.value){
		alert("舊密碼未填寫!");
		return false;
	}
	if(!document.store_form.new_pwd.value){
		alert("新密碼未填寫!");
		return false;
	}
	if(document.store_form.new_pwd.value != document.store_form.new_pwd2.value){
		alert("密碼兩次未相同!");
		return false;
	}
}