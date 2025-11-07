define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading) {
    'use strict';

    function View(view, params) {
        BaseView.apply(this, arguments);

        var instance = this;

        view.addEventListener('click', function (e) {
            var btnDeleteCollector = e.target.closest('.btnDeleteCollector');
            if (btnDeleteCollector) {
                instance.deleteCollector(btnDeleteCollector);
            }

            var btnEditCollector = e.target.closest('.btnEditCollector');
            if (btnEditCollector) {
                instance.editCollector(btnEditCollector);
            }

            var btnAddCollector = e.target.closest('.btnAddCollector');
            if (btnAddCollector) {
                instance.addCollector();
            }
        });

        view.querySelector('form').addEventListener('submit', function (e) {
            e.preventDefault();
            loading.show();
            var mdbListApiKey = view.querySelector('.txtMdbListApiKey').value.trim();
            var traktClientId = view.querySelector('.txtTraktClientId').value.trim();
            if (instance.config.Collectors.some(collector => collector.Type === 'MDB List') && mdbListApiKey === '') {
                loading.hide();
                require(['confirm'], function (confirm) {
                    confirm({
                        title: 'MDB List API Key Required',
                        text: 'You have configured a MDB List collector, but have not provided an API key. Please provide a valid API key to use this collector.',
                        confirmText: 'OK',
                        primary: 'cancel'
                    });
                });
                return false;
            }
            if (instance.config.Collectors.some(collector => collector.Type === 'Trakt List') && traktClientId === '') {
                loading.hide();
                require(['confirm'], function (confirm) {
                    confirm({
                        title: 'Trakt Client ID Required',
                        text: 'You have configured a Trakt List collector, but have not provided a Client ID. Please provide a valid Client ID to use this collector.',
                        confirmText: 'OK',
                        primary: 'cancel'
                    });
                });
                return false;
            }

            instance.config.MdbListApiKey = mdbListApiKey;
            instance.config.TraktClientId = traktClientId;
            ApiClient.updatePluginConfiguration(instance.pluginId, instance.config).then(Dashboard.processServerConfigurationUpdateResult);
            return false;
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.pluginId = "1B55EFD5-6080-4207-BCF8-DC2723C7AC10";

    View.prototype.config = null;

    View.prototype.loadConfig = function (pluginConfig) {
        var instance = this;

        instance.view.querySelector('.txtMdbListApiKey').value = pluginConfig.MdbListApiKey || '';
        instance.view.querySelector('.txtTraktClientId').value = pluginConfig.TraktClientId || '';

        instance.loadCollectors(pluginConfig);
    }

    View.prototype.loadCollectors = function (pluginConfig) {
        var html = "";

        for (var i = 0, length = pluginConfig.Collectors.length; i < length; i++) {

            var collector = pluginConfig.Collectors[i];

            html += '<div class="listItem listItem-border">';

            html += '<i class="md-icon listItemIcon">movie_filter</i>';

            html += '<div class="listItemBody two-line">';
            html += "<h3 class='listItemBodyText'>" + collector.Type + "</h3>";
            html += "<div class='listItemBodyText secondary'>" + collector.Id + "</div>";
            html += '</div>';

            html += '<button type="button" is="paper-icon-button-light" data-collector-index="' + i + '" data-collector-type="' + collector.Type + '" data-collector-id="' + collector.Id + '" class="btnEditCollector"><i class="md-icon">edit</i></button>';
            html += '<button type="button" is="paper-icon-button-light" data-collector-index="' + i + '" data-collector-type="' + collector.Type + '" data-collector-id="' + collector.Id + '" class="btnDeleteCollector"><i class="md-icon">delete</i></button>';

            html += '</div>';
        }

        this.view.querySelector('.collectorList').innerHTML = html;
    };

    View.prototype.showCollectorEditor = function (dialogHelper, collectorIndex) {
        var instance = this;

        var dialogOptions = {
            removeOnClose: true,
            scrollY: false
        };

        dialogOptions.size = 'small';

        var dlg = dialogHelper.createDialog(dialogOptions);

        dlg.classList.add('formDialog');

        var html = '';
        var title = 'New Collector';
        var collectorType = 'IMDb Chart';
        var collectorId = '';
        var collectorName = '';
        var collectorSchedule = '0 0 1 * *';
        var collectorEnabled = true;
        var collectorEnableTags = true;
        var collectorEnableCollections = true;
        var submitBtnText = 'Add';
        if (collectorIndex !== null && collectorIndex !== undefined) {
            var collectorConfig = instance.config.Collectors[collectorIndex];
            title = 'Edit Collector';
            collectorType = collectorConfig.Type;
            collectorId = collectorConfig.Id;
            collectorName = collectorConfig.Name || '';
            collectorSchedule = collectorConfig.Schedule || '0 0 1 * *';
            collectorEnabled = collectorConfig.Enabled;
            collectorEnableTags = collectorConfig.EnableTags;
            collectorEnableCollections = collectorConfig.EnableCollections;
            submitBtnText = 'Save';
        }

        html += '<div class="formDialogHeader">';
        html += '<button is="paper-icon-button-light" class="btnCancel autoSize" tabindex="-1"><i class="md-icon">&#xE5C4;</i></button>';
        html += '<h3 class="formDialogHeaderTitle">';
        html += title;
        html += '</h3>';
        html += '</div>';

        html += '<div is="emby-scroller" data-horizontal="false" data-centerfocus="card" class="formDialogContent">';
        html += '<div class="scrollSlider">';
        html += '<form class="dialogContentInner dialog-content-centered padded-left padded-right">';

        html += '<div class="selectContainer">\
                    <select id="collectorType" name="collectorType" is="emby-select" label="Type:">\
                        <option value="IMDb Chart" ' + (collectorType === 'IMDb Chart' ? 'selected' : '') + '>IMDb Chart</option>\
                        <option value="IMDb List" ' + (collectorType === 'IMDb List' ? 'selected' : '') + '>IMDb List</option>\
                        <option value="Trakt List" ' + (collectorType === 'Trakt List' ? 'selected' : '') + '>Trakt List</option>\
                        <option value="MDB List" ' + (collectorType === 'MDB List' ? 'selected' : '') + '>MDB List</option>\
                        <option value="Letterboxd" ' + (collectorType === 'Letterboxd' ? 'selected' : '') + '>Letterboxd</option>\
                    </select>\
                </div>';

        html += '<div class="inputContainer">\
                    <div class="flex align-items-center">\
                        <div class="flex-grow">\
                            <input is="emby-input" id="collectorId" name="collectorId" label="ID:" required="required" autocomplete="off" ' + (collectorId !== '' ? 'value="' + collectorId + '"' : '') + '/>\
                        </div>\
                    </div>\
                </div>';

        html += '<div class="inputContainer">\
                    <div class="flex align-items-center">\
                        <div class="flex-grow">\
                            <input is="emby-input" id="collectorName" name="collectorName" label="Name:" autocomplete="off" ' + (collectorName !== '' ? 'value="' + collectorName + '"' : '') + '/>\
                            <div class="fieldDescription">\
                                Override the name scraped from the internet.\
                            </div>\
                        </div>\
                    </div>\
                </div>';

        html += '<div class="inputContainer">\
                    <div class="flex align-items-center">\
                        <div class="flex-grow">\
                            <input is="emby-input" id="collectorSchedule" name="collectorSchedule" label="Cron schedule:" autocomplete="off" ' + (collectorSchedule !== '' ? 'value="' + collectorSchedule + '"' : '') + '/>\
                            <div class="fieldDescription">\
                                Generate a cron expression from <a is="emby-linkbutton" class="button-link" href="https://crontab.cronhub.io/" target="_blank">Cronhub</a>.\
                            </div>\
                        </div>\
                    </div>\
                </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnabled" ' + (collectorEnabled ? 'checked' : '') + '/>\
                        <span>Enabled</span>\
                    </label>\
                 </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnableTags" ' + (collectorEnableTags ? 'checked' : '') + '/>\
                        <span>Enable Tags</span>\
                    </label>\
                    <div class="fieldDescription">Add tags to media items</div>\
                 </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnableCollections" ' + (collectorEnableCollections ? 'checked' : '') + '/>\
                        <span>Enable Collections</span>\
                    </label>\
                    <div class="fieldDescription">Add media items to collections</div>\
                </div>';

        html += '<div class="formDialogFooter">\
                    <button is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem">\
                        <span>' + submitBtnText + '</span>\
                    </button>\
                </div>';

        html += '</form>';
        html += '</div>';
        html += '</div>';

        dlg.innerHTML = html;

        dlg.querySelector('.btnCancel').addEventListener('click', function () {
            dialogHelper.close(dlg);
        });

        dlg.querySelector('form').addEventListener("submit", function (e) {
            e.preventDefault();

            loading.show();

            var newEntry = collectorIndex === null || collectorIndex === undefined;

            var type = dlg.querySelector('#collectorType').value;
            var id = dlg.querySelector('#collectorId').value;
            var name = dlg.querySelector('#collectorName').value;
            var schedule = dlg.querySelector('#collectorSchedule').value;
            var enabled = dlg.querySelector('#collectorEnabled').checked;
            var enableTags = dlg.querySelector('#collectorEnableTags').checked;
            var enableCollections = dlg.querySelector('#collectorEnableCollections').checked;

            var existing = instance.config.Collectors.some(function (collector, index) {
                if (collector.Type === type && collector.Id.trim().toLowerCase() === id.trim().toLowerCase()) {
                    if (newEntry || index !== collectorIndex) {
                        return true;
                    }
                }
                return false;
            });
            if (existing) {
                loading.hide();
                dialogHelper.close(dlg);
                require(['confirm'], function (confirm) {
                    confirm({
                        title: 'Duplicate Collector',
                        text: `A collector with the same type (${type}) and ID (${id}) already exists. Please edit the existing collector.`,
                        primary: 'OK'
                    });
                });
                return;
            }

            if (newEntry) {
                var collector = {};
                collector.Type = type;
                collector.Id = id.trim();
                collector.Name = name.trim();
                collector.Schedule = schedule.trim();
                collector.Enabled = enabled;
                collector.EnableTags = enableTags;
                collector.EnableCollections = enableCollections;
                instance.config.Collectors.push(collector);
            } else {
                instance.config.Collectors[collectorIndex].Type = type;
                instance.config.Collectors[collectorIndex].Id = id.trim();
                instance.config.Collectors[collectorIndex].Name = name.trim();
                instance.config.Collectors[collectorIndex].Schedule = schedule.trim();
                instance.config.Collectors[collectorIndex].Enabled = enabled;
                instance.config.Collectors[collectorIndex].EnableTags = enableTags;
                instance.config.Collectors[collectorIndex].EnableCollections = enableCollections;
            }

            loading.hide();
            dialogHelper.close(dlg);
            instance.loadCollectors(instance.config);
        });

        dialogHelper.open(dlg);
    };

    View.prototype.addCollector = function () {
        var instance = this;

        require(['dialogHelper', 'formDialogStyle', 'emby-select', 'emby-input', 'emby-checkbox', 'paper-icon-button-light'], function (dialogHelper) {
            instance.showCollectorEditor(dialogHelper, null);
        });
    };

    View.prototype.editCollector = function (link) {
        var instance = this;
        var collectorIndex = Number(link.getAttribute('data-collector-index'));

        require(['dialogHelper', 'formDialogStyle', 'emby-select', 'emby-input', 'emby-checkbox', 'paper-icon-button-light'], function (dialogHelper) {
            instance.showCollectorEditor(dialogHelper, collectorIndex);
        });
    }

    View.prototype.deleteCollector = function (link) {
        var instance = this;
        var collectorType = link.getAttribute('data-collector-type');
        var collectorId = link.getAttribute('data-collector-id');
        var collectorIndex = link.getAttribute('data-collector-index');

        require(['confirm'], function (confirm) {
            confirm({
                title: 'Delete Collector',
                text: 'Delete this collector (type: ' + collectorType + ', ID: ' + collectorId + ')?',
                confirmText: 'Delete',
                primary: 'cancel'
            }).then(function () {
                loading.show();
                instance.config.Collectors.splice(collectorIndex, 1);
                instance.loadCollectors(instance.config);
                loading.hide();
            });
        });
    };

    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);

        loading.show();

        var instance = this;
        ApiClient.getPluginConfiguration(instance.pluginId).then(function (pluginConfig) {
            instance.config = pluginConfig;
            instance.loadConfig(instance.config);
            loading.hide();
        });
    };

    return View;
});
