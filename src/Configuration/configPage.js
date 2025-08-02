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

            var btnAddCollector = e.target.closest('.btnAddCollector');

            if (btnAddCollector) {
                instance.addCollector();
            }
        });
    }

    Object.assign(View.prototype, BaseView.prototype);

    View.prototype.pluginId = "1B55EFD5-6080-4207-BCF8-DC2723C7AC10";

    View.prototype.config = null;

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

            html += '<button type="button" is="paper-icon-button-light" data-collector-idex="' + i + '" data-collector-type="' + collector.Type + '" data-collector-id="' + collector.Id + '" class="btnDeleteCollector"><i class="md-icon">delete</i></button>';

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

            var newEntry = true;

            var type = dlg.querySelector('#collectorType').value;
            var id = dlg.querySelector('#collectorId').value;
            var enabled = dlg.querySelector('#collectorEnabled').checked;
            var enableTags = dlg.querySelector('#collectorEnableTags').checked;
            var enableCollections = dlg.querySelector('#collectorEnableCollections').checked;

            // need to handle updating a collector in addition to creating a new one
            if (instance.config.Collectors.length > 0) {
                for (var i = 0, length = instance.config.Collectors.length; i < length; i++) {
                    if (instance.config.Collectors[i].Type === type && instance.config.Collectors[i].Id === id) {
                        newEntry = false;
                        instance.config.Collectors[i].Enabled = enabled;
                        instance.config.Collectors[i].EnableTags = enableTags;
                        instance.config.Collectors[i].EnableCollections = enableCollections;
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
                instance.config.Collectors.push(collector);
            }

            ApiClient.updatePluginConfiguration(instance.pluginId, instance.config).then(function () {
                loading.hide();

                dialogHelper.close(dlg);
                instance.loadCollectors(instance.config);
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

    View.prototype.deleteCollector = function (link) {
        var instance = this;
        var collectorType = link.getAttribute('data-collector-type');
        var collectorId = link.getAttribute('data-collector-id');
        var collectorIndex = link.getAttribute('data-collector-idex');

        require(['confirm'], function (confirm) {
            confirm({
                title: 'Delete Collector',
                text: 'Delete this collector (type: ' + collectorType + ', ID: ' + collectorId + ')?',
                confirmText: 'Delete',
                primary: 'cancel'
            }).then(function () {
                loading.show();

                instance.config.Collectors.splice(collectorIndex, 1);
                ApiClient.updatePluginConfiguration(instance.pluginId, instance.config).then(function () {
                    instance.loadCollectors(instance.config);
                    loading.hide();
                });
            });
        });
    };

    View.prototype.onResume = function (options) {
        BaseView.prototype.onResume.apply(this, arguments);

        loading.show();

        var instance = this;
        ApiClient.getPluginConfiguration(instance.pluginId).then(function (pluginConfig) {
            instance.config = pluginConfig;
            instance.loadCollectors(instance.config);
            loading.hide();
        });
    };

    return View;
});
