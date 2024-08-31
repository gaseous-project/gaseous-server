let selectedTab = '';

function SetupPage() {
    if (userProfile.roles.includes("Admin")) {
        document.getElementById('properties_toc_settings').style.display = '';
        document.getElementById('properties_toc_libraries').style.display = '';
        document.getElementById('properties_toc_users').style.display = '';
        document.getElementById('properties_toc_services').style.display = '';
        document.getElementById('properties_toc_mapping').style.display = '';
        document.getElementById('properties_toc_logs').style.display = '';
    }
    if (userProfile.roles.includes("Gamer")) {
        document.getElementById('properties_toc_mapping').style.display = '';
    }

    let myParam = getQueryString('sub', 'string');

    if (myParam) {
        selectedTab = myParam;
    } else {
        selectedTab = 'system';
    }

    SelectTab(selectedTab);
}

function SelectTab(TabName) {
    if (selectedTab != TabName) {
        window.location.href = '/index.html?page=settings&sub=' + TabName;
    }

    let tocs = document.getElementsByName('properties_toc_item');
    for (let i = 0; i < tocs.length; i++) {
        if ((tocs[i].id) == ("properties_toc_" + TabName)) {
            tocs[i].className = "properties_toc_item_selected";
        } else {
            tocs[i].className = '';
        }
    }

    let subScriptObject = document.createElement('script');
    subScriptObject.src = '/pages/settings/' + TabName + '.js?v=' + AppVersion;
    subScriptObject.setAttribute('type', 'text/javascript');
    subScriptObject.setAttribute('async', 'false');
    subScriptObject.addEventListener('load', function () {
        $('#properties_bodypanel').load('/pages/settings/' + TabName + '.html?v=' + AppVersion);
    });
    document.head.appendChild(subScriptObject);
}

SetupPage();