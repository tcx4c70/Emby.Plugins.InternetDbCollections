define(['baseView', 'loading', 'emby-input', 'emby-button', 'emby-checkbox', 'emby-scroller', 'emby-select'], function (BaseView, loading) {
    'use strict';

    function View(view, params) {
        BaseView.apply(this, arguments);

        var instance = this;

        view.addEventListener('click', function (e) {
            var btnDeleteCollector = e.target.closest('.btnDeleteCollector');

            if (btnDeleteCollector) {
                instance.deleteCollector(btnDeleteCollector.getAttribute('collector-type'), btnDeleteCollector.getAttribute('collector-id'));
            }

            var btnAddCollector = e.target.closest('.btnAddCollector');

            if (btnAddCollector) {
                instance.addCollector();
            }
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.loadConfiguration = function () {

        var instance = this;

        ApiClient.getPluginConfiguration("1B55EFD5-6080-4207-BCF8-DC2723C7AC10").then(function (pluginConfig) {

            // Just in case it's empty
            pluginConfig.RootLocations = pluginConfig.RootLocations || [];

            instance.loadCollector(pluginConfig);
        });

        loading.hide();
    };

    View.prototype.loadCollector = function (pluginConfig) {

        var html = "";

        for (var i = 0, length = pluginConfig.Collectors.length; i < length; i++) {

            var collector = pluginConfig.Collectors[i];

            html += '<div class="listItem listItem-border">';

            html += '<i class="md-icon listItemIcon">movie_filter</i>';

            html += '<div class="listItemBody two-line">';
            html += "<h3 class='listItemBodyText'>" + collector.Type + "</h3>";
            html += "<div class='listItemBodyText secondary'>" + collector.Id + "</div>";
            html += '</div>';

            html += '<button type="button" is="paper-icon-button-light" collector-type="' + collector.Type + '" collector-id="' + collector.Id + '" class="btnDeleteCollector"><i class="md-icon">delete</i></button>';

            html += '</div>';
        }

        this.view.querySelector('.collectorList').innerHTML = html;
    };

    View.prototype.showCollectorEditor = function (dialogHelper) {

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
                        <option value="IMDb Chart">IMDb Chart</option>\
                        <option value="IMDb List">IMDb List</option>\
                    </select>\
                </div>';

        html += '<div class="inputContainer">\
                    <div class="flex align-items-center">\
                        <div class="flex-grow">\
                            <input is="emby-input" id="collectorId" name="collectorId" label="ID:" required="required" autocomplete="off" />\
                        </div>\
                    </div>\
                </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnabled" checked="checked" />\
                        <span>Enabled</span>\
                    </label>\
                 </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnableTags" checked="checked" />\
                        <span>Enable Tags</span>\
                    </label>\
                    <div class="fieldDescription">Add tags to media items</div>\
                 </div>';

        html += '<div class="checkboxContainer checkboxContainer-withDescription">\
                    <label>\
                        <input type="checkbox" is="emby-checkbox" id="collectorEnableCollections" checked="checked" />\
                        <span>Enable Collections</span>\
                    </label>\
                    <div class="fieldDescription">Add media items to collections</div>\
                </div>';

        html += '<div class="formDialogFooter">\
                    <button is="emby-button" type="submit" class="raised button-submit block formDialogFooterItem">\
                        <span>Save</span>\
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
            loading.show();

            ApiClient.getPluginConfiguration("1B55EFD5-6080-4207-BCF8-DC2723C7AC10").then(function (config) {
                var newEntry = true;

                var type = dlg.querySelector('#collectorType').value;
                var id = dlg.querySelector('#collectorId').value;
                var enabled = dlg.querySelector('#collectorEnabled').checked;
                var enableTags = dlg.querySelector('#collectorEnableTags').checked;
                var enableCollections = dlg.querySelector('#collectorEnableCollections').checked;

                // need to handle updating a collector in addition to creating a new one
                if (config.Collectors.length > 0) {
                    for (var i = 0, length = config.Collectors.length; i < length; i++) {
                        if (config.Collectors[i].Type === type && config.Collectors[i].Id === id) {
                            newEntry = false;
                            config.Collectors[i].Enabled = enabled;
                            config.Collectors[i].EnableTags = enableTags;
                            config.Collectors[i].EnableCollections = enableCollections;
                        }
                    }
                }

                if (newEntry) {
                    var collector = {};
                    collector.Type = type;
                    collector.Id = id;
                    collector.Enabled = enabled;
                    collector.EnableTags = enableTags;
                    collector.EnableCollections = enableCollections;
                    config.Collectors.push(collector);
                }

                ApiClient.updatePluginConfiguration("1B55EFD5-6080-4207-BCF8-DC2723C7AC10", config).then(function () {
                    loading.hide();

                    dialogHelper.close(dlg);
                    instance.loadConfiguration();
                });

                return true;
            });

            e.preventDefault();
        });

        dialogHelper.open(dlg);
    };

    View.prototype.addCollector = function () {

        var instance = this;

        require(['dialogHelper', 'formDialogStyle', 'emby-select', 'emby-input', 'emby-checkbox', 'paper-icon-button-light'], function (dialogHelper) {
            instance.showCollectorEditor(dialogHelper);
        });
    };

    View.prototype.deleteCollector = function (collectorType, collectorId) {

        var instance = this;

        require(['confirm'], function (confirm) {
            confirm({
                title: 'Delete Collector',
                text: 'Delete this collector?',
                confirmText: 'Delete',
                primary: 'cancel'
            }).then(function () {
                loading.show();

                ApiClient.getPluginConfiguration("1B55EFD5-6080-4207-BCF8-DC2723C7AC10").then(function (config) {
                    var index = 0;
                    for (var i = 0, length = config.Collectors.length; i < length; i++) {
                        if (config.Collectors[i].Type === collectorType && config.Collectors[i].Id === collectorId) {
                            index = i;
                            break;
                        }
                    }

                    config.Collectors.splice(index, 1);

                    ApiClient.updatePluginConfiguration("1B55EFD5-6080-4207-BCF8-DC2723C7AC10", config).then(function () {
                        loading.hide();
                        instance.loadConfiguration();
                    });
                });
            });
        });
    };

    View.prototype.onResume = function (options) {

        BaseView.prototype.onResume.apply(this, arguments);

        var instance = this;
        instance.loadConfiguration();
    };

    return View;
});
