//Check to see if the window is top if not then display button
$(window).scroll(function(){
	if ($(this).scrollTop() >200 ){
		$('#to_top').fadeIn();
	} else {
		$('#to_top').fadeOut();
	}
});

//Click event to scroll to top
$('.to_top').click(function(){
	$('html, body').animate({'scrollTop':0}, 1500, 'easeInOutExpo');
	return false;
});


//

$(".list_before.account li:empty").hide();







