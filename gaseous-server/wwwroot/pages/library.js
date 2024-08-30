function SetupPage() {
    // setup view controls
    $('#games_library_pagesize_select').select2();
    $('#games_library_orderby_select').select2();
    $('#games_library_orderby_direction_select').select2();

    // setup scroll event
    $(window).scroll(IsInView);

    // setup filter panel
    ajaxCall('/api/v1.1/Filter', 'GET', function (result) {
        var scrollerElement = document.getElementById('games_filter_scroller');
        formatFilterPanel(scrollerElement, result);

        //executeFilter1_1();
    });
}

SetupPage();