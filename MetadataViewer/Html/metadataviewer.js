define(['paperdialoghelper', 'paper-dialog', 'paper-fab', 'paper-input', 'paper-checkbox', 'detailtablecss'], function (paperDialogHelper) {

    var currentItem;
    var currentItemType;
    var currentDeferred;
    var currentSearchResult;

    function showMetadataTable(page, item) {

        Dashboard.showLoadingMsg();

        var lang = page.querySelector('#selectLanguage').value;

        ApiClient.getJSON(ApiClient.getUrl('Items/' + item.Id + '/MetadataRaw', { language: lang })).then(function (table) {

            var htmlLookup = '<table data-role="table" class="stripedTable ui-responsive table-stroke detailTable">';

            for (var i = 0; i < table.LookupData.length; i++) {

                var row = table.LookupData[i];
                if (row.Key == 'MetadataLanguage') {

                    page.querySelector('#selectLanguage').value = row.Value || '';
                }
                else {

                    htmlLookup += '<tr style="vertical-align: top"><td>' + row.Key + '</td>';
                    htmlLookup += '<td>' + row.Value + '</td></tr>';
                }
            }

            htmlLookup += '</table>';
            page.querySelector('#searchCriteria').innerHTML = htmlLookup;

            var html = '<table data-role="table" data-mode="reflow" class="stripedTable ui-responsive table-stroke detailTable" style="table-layout: fixed">';
            html += '<thead><th />';

            for (var i = 0; i < table.Headers.length; i++) {
                html += '<th>' + table.Headers[i] + '</th>';
            }

            html += '</thead>';
            html += '<tbody>';

            for (var i = 0; i < table.Rows.length; i++) {

                var row = table.Rows[i];
                html += '<tr style="vertical-align: top"><td>' + row.Caption + '</td>';

                for (var n = 0; n < row.Values.length; n++) {
                    html += '<td>' + (row.Values[n] == null ? '' : row.Values[n]) + '</td>';
                }

                html += '</tr>';
            }

            html += '</tbody></table>';

            page.querySelector('.dialogHeaderTitle').innerHTML = 'Metadata Viewer'; // Globalize.translate('HeaderMetadataRaw');
            page.querySelector('.metadataRawTable').innerHTML = html;

            Dashboard.hideLoadingMsg();
        });
    }

    function onDialogClosed() {

        Dashboard.hideLoadingMsg();
        currentDeferred.resolveWith(null, [hasChanges]);
    }

    function showEditor(itemId) {

        var xhr = new XMLHttpRequest();
        xhr.open('GET', 'components/metadataviewer/metadataviewer.template.html', true);

        xhr.onload = function (e) {

            var template = this.response;

            ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                var dlg = paperDialogHelper.createDialog({
                    size: 'large'
                });

                dlg.classList.add('ui-body-b');
                dlg.classList.add('background-theme-b');
                
                var html = '';
                html += Globalize.translateDocument(template);

                dlg.innerHTML = html;
                document.body.appendChild(dlg);

                paperDialogHelper.open(dlg);

                dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                    paperDialogHelper.close(dlg);
                });

                dlg.querySelector('#selectLanguage').addEventListener('change', function (e) {

                    showMetadataTable(dlg, item);
                });

                dlg.addEventListener('iron-overlay-closed', function () {

                    Dashboard.hideLoadingMsg();
                });

                dlg.classList.add('metadataViewer');

                showMetadataTable(dlg, item);
            });
        }

        xhr.send();
    }

    return {
        show: function (itemId) {
            return new Promise(function (resolve, reject) {

                showEditor(itemId);
            });
        }
    };
});