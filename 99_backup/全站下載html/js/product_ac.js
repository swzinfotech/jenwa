$(function() {

	var ww = $(window).width();
    if (ww <768) {
        $(".accordion-item.is-active").removeClass("is-active");
    } else {
        $(".accordion > .accordion-item.is-active").children(".accordion-panel").slideDown();
    }

	$(".accordion > .accordion-item").click(function() {
		// Cancel the siblings
		$(this).siblings(".accordion-item").removeClass("is-active").children(".accordion-panel").slideUp();
		// Toggle the item
		$(this).toggleClass("is-active").children(".accordion-panel").slideToggle("ease-out");

	    $('input, textarea').click(function(event){
	        event.stopPropagation();
	    });
	});
});

$(function() {
	$('.bxslider').bxSlider({
		auto:'true',
		easing:'ease',
		controls: false,
		pagerCustom: '#bx-pager'
  	});
});