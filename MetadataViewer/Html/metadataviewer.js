define(['dialogHelper', 'globalize', 'loading', 'detailtablecss', 'emby-button', 'emby-select', 'formDialogStyle'], function (dialogHelper, globalize, loading) {

    var currentItem;
    var currentItemType;
    var currentDeferred;
    var currentSearchResult;

    function showMetadataTable(page, item) {

        Dashboard.showLoadingMsg();

        var lang = page.querySelector('#selectLanguage').value;

        ApiClient.getJSON(ApiClient.getUrl('Items/' + item.Id + '/MetadataRaw', { language: lang })).then(function (table) {

            var htmlLookup = '<table class="detailTable">';

            for (var i = 0; i < table.LookupData.length; i++) {

                var row = table.LookupData[i];
                if (row.Key == 'MetadataLanguage') {

                    page.querySelector('#selectLanguage').value = row.Value || '';
                }
                else {

                    htmlLookup += '<tr style="vertical-align: top"><td style="width: 7em;">' + row.Key + '</td>';
                    htmlLookup += '<td>' + row.Value + '</td></tr>';
                }
            }

            htmlLookup += '</table>';
            page.querySelector('#searchCriteria').innerHTML = htmlLookup;

            var html = '<table class="detailTable" style="table-layout: fixed">';
            html += '<thead><th />';

            for (i = 0; i < table.Headers.length; i++) {
                html += '<th>' + table.Headers[i] + '</th>';
            }

            html += '</thead>';
            html += '<tbody style="vertical-align: top">';

            for (i = 0; i < table.Rows.length; i++) {

                row = table.Rows[i];
                html += '<tr><td style="overflow-x:hidden; text-overflow:ellipsis;">';
                html += row.Caption + '</td>';

                for (var n = 0; n < row.Values.length; n++) {
                    html += '<td>' + (row.Values[n] == null ? '' : row.Values[n]) + '</td>';
                }

                html += '</tr>';
            }

            html += '</tbody></table>';

            page.querySelector('.metadataRawTable').innerHTML = html;

            Dashboard.hideLoadingMsg();
        });
    }

    return {
        show: function (itemId) {
            return new Promise(function (resolve, reject) {

                var xhr = new XMLHttpRequest();
                xhr.open('GET', 'components/metadataviewer/metadataviewer.template.html', true);

                xhr.onload = function (e) {

                    var template = this.response;

                    ApiClient.getItem(Dashboard.getCurrentUserId(), itemId).then(function (item) {

                        var dialogOptions = {
                            removeOnClose: true,
                            size: 'fullscreen'
                        };

                        var dlg = dialogHelper.createDialog(dialogOptions);

                        dlg.classList.add('formDialog');

                        dlg.innerHTML = globalize.translateDocument(template);
                        document.body.appendChild(dlg);

                        dlg.querySelector('.formDialogHeaderTitle').innerHTML = "Raw Metadata for: " + item.Name;

                        dialogHelper.open(dlg);

                        dlg.addEventListener('close', function () {

                            Dashboard.hideLoadingMsg();

                            if (dlg.submitted) {
                                resolve();
                            } else {
                                reject();
                            }
                        });

                        dlg.querySelector('.btnCancel').addEventListener('click', function (e) {

                            dialogHelper.close(dlg);
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
                };

                xhr.send();
            });
        }
    };
});