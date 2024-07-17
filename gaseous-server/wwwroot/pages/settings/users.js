function GetUsers() {
    let targetDiv = document.getElementById('settings_users_table_container');
    targetDiv.innerHTML = '';

    ajaxCall(
        '/api/v1.1/Account/Users',
        'GET',
        function (result) {
            let newTable = document.createElement('table');
            newTable.className = 'romtable';
            newTable.style.width = '100%';
            newTable.cellSpacing = 0;

            newTable.appendChild(
                createTableRow(
                    true,
                    [
                        '',
                        'Email',
                        'Role',
                        'Age Restriction',
                        ''
                    ],
                    '',
                    ''
                )
            );

            for (let i = 0; i < result.length; i++) {
                let userAvatar = new Avatar(result[i].profileId, 32, 32, true);
                userAvatar.classList.add("user_list_icon");

                let roleDiv = document.createElement('div');

                let roleItem = CreateBadge(result[i].highestRole);
                roleDiv.appendChild(roleItem);

                let ageRestrictionPolicyDescription = document.createElement('div');
                if (result[i].securityProfile != null) {
                    if (result[i].securityProfile.ageRestrictionPolicy != null) {
                        let IncludeUnratedText = '';
                        if (result[i].securityProfile.ageRestrictionPolicy.includeUnrated == true) {
                            IncludeUnratedText = " &#43; Unclassified titles";
                        }

                        let restrictionText = result[i].securityProfile.ageRestrictionPolicy.maximumAgeRestriction + IncludeUnratedText;

                        ageRestrictionPolicyDescription = CreateBadge(restrictionText);
                    }
                }

                let editButton = '';

                let deleteButton = '';

                if (userProfile.userId != result[i].id) {
                    editButton = '<a href="#" onclick="showDialog(\'settingsuseredit\', \'' + result[i].id + '\');" class="romlink"><img src="/images/edit.svg" class="banner_button_image" alt="Edit" title="Edit" /></a>';

                    deleteButton = '<a href="#" onclick="showSubDialog(\'settingsuserdelete\', \'' + result[i].id + '\');" class="romlink"><img src="/images/delete.svg" class="banner_button_image" alt="Delete" title="Delete" /></a>';
                }

                newTable.appendChild(
                    createTableRow(
                        false,
                        [
                            userAvatar,
                            result[i].emailAddress,
                            roleDiv,
                            ageRestrictionPolicyDescription,
                            '<div style="text-align: right;">' + editButton + deleteButton + '</div>'
                        ],
                        'romrow',
                        'romcell'
                    )
                );
            }

            targetDiv.appendChild(newTable);
        }
    );
}