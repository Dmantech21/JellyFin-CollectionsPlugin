// Smart Collections plugin config page controller.
// Loaded by Jellyfin via data-controller="SmartCollectionsController" using importModule().
// The default export receives the page view element as its first argument.

const PLUGIN_ID = '7c78ef5d-3741-4b63-a22e-5a1f6c12f0db';

function esc(s) {
    return (s || '').replace(/&/g, '&amp;').replace(/"/g, '&quot;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function renderRulesTable(view, themeRules) {
    const tbody = view.querySelector('#ThemeRulesBody');
    if (!tbody) return;
    tbody.innerHTML = '';
    themeRules.forEach(function (rule, idx) {
        const tr = document.createElement('tr');
        tr.innerHTML =
            '<td style="padding:4px 8px;"><input type="text" class="emby-input rule-name" value="' + esc(rule.Name) + '" style="width:100%;" /></td>' +
            '<td style="padding:4px 8px;"><input type="text" class="emby-input rule-title" value="' + esc(rule.TitleKeywords) + '" placeholder="christmas,xmas" style="width:100%;" /></td>' +
            '<td style="padding:4px 8px;"><input type="text" class="emby-input rule-genre" value="' + esc(rule.GenreKeywords) + '" placeholder="Holiday" style="width:100%;" /></td>' +
            '<td style="padding:4px 8px;"><input type="text" class="emby-input rule-tag" value="' + esc(rule.TagKeywords) + '" placeholder="christmas" style="width:100%;" /></td>' +
            '<td style="padding:4px 8px;"><input type="number" class="emby-input rule-min" value="' + (rule.MinMovieCount || 2) + '" min="1" max="99" style="width:4em;" /></td>' +
            '<td style="padding:4px 8px;">' +
              '<select class="rule-mode" style="padding:4px; background:var(--color-backdrop,#333); color:var(--color-text,#fff); border:1px solid #555; border-radius:2px;">' +
              '<option value="Any"' + (rule.MatchMode !== 'All' ? ' selected' : '') + '>Any</option>' +
              '<option value="All"' + (rule.MatchMode === 'All' ? ' selected' : '') + '>All</option>' +
              '</select></td>' +
            '<td style="padding:4px 8px; text-align:center;"><input type="checkbox" class="rule-enabled"' + (rule.Enabled ? ' checked' : '') + ' /></td>' +
            '<td style="padding:4px 8px;"><button type="button" class="rule-delete" data-idx="' + idx + '" style="cursor:pointer; background:none; border:none; color:#e87c7c; font-size:1em; padding:2px 6px;">✕</button></td>';
        tbody.appendChild(tr);
    });
}

function collectRules(view) {
    return Array.from(view.querySelectorAll('#ThemeRulesBody tr')).map(function (tr) {
        return {
            Name:          tr.querySelector('.rule-name').value.trim(),
            TitleKeywords: tr.querySelector('.rule-title').value.trim(),
            GenreKeywords: tr.querySelector('.rule-genre').value.trim(),
            TagKeywords:   tr.querySelector('.rule-tag').value.trim(),
            MinMovieCount: parseInt(tr.querySelector('.rule-min').value, 10) || 2,
            MatchMode:     tr.querySelector('.rule-mode').value,
            Enabled:       tr.querySelector('.rule-enabled').checked
        };
    });
}

function renderUserDeleted(view, userDeletedKeys) {
    const div = view.querySelector('#UserDeletedSection');
    if (!div) return;
    if (!userDeletedKeys.length) {
        div.innerHTML = '<p><em>None — all missing collections will be recreated.</em></p>';
        return;
    }
    let html = '<ul style="list-style:none; padding:0;">';
    userDeletedKeys.forEach(function (key, idx) {
        html += '<li style="margin:4px 0;">' +
            '<code style="margin-right:8px;">' + esc(key) + '</code>' +
            '<button type="button" class="btn-undel" data-idx="' + idx + '" ' +
            'style="cursor:pointer; padding:2px 8px; background:#444; border:1px solid #666; color:#fff;">Allow Recreate</button></li>';
    });
    html += '</ul>';
    div.innerHTML = html;
}

export default function SmartCollectionsController(view) {
    console.log('SmartCollections: controller loaded');

    let themeRules = [];
    let userDeletedKeys = [];

    function refreshDeletedSection() {
        renderUserDeleted(view, userDeletedKeys);
        view.querySelectorAll('.btn-undel').forEach(function (btn) {
            btn.addEventListener('click', function () {
                userDeletedKeys.splice(parseInt(this.dataset.idx, 10), 1);
                refreshDeletedSection();
            });
        });
    }

    function refreshRulesTable() {
        renderRulesTable(view, themeRules);
        view.querySelectorAll('.rule-delete').forEach(function (btn) {
            btn.addEventListener('click', function () {
                themeRules.splice(parseInt(this.dataset.idx, 10), 1);
                refreshRulesTable();
            });
        });
    }

    function loadConfig() {
        console.log('SmartCollections: loadConfig');
        ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (config) {
            view.querySelector('#chkEnableSeries').checked = config.EnableSeriesCollections !== false;
            view.querySelector('#txtSeriesMin').value      = config.SeriesMinMovieCount || 2;
            view.querySelector('#chkEnableTheme').checked  = config.EnableThemeCollections !== false;
            view.querySelector('#txtSyncInterval').value   = config.SyncIntervalHours || 24;

            try { themeRules = JSON.parse(config.ThemeCollectionRulesJson || '[]'); } catch (e) { themeRules = []; }
            refreshRulesTable();

            try { userDeletedKeys = JSON.parse(config.UserDeletedCollectionKeysJson || '[]'); } catch (e) { userDeletedKeys = []; }
            refreshDeletedSection();
        }).catch(function (err) {
            console.error('SmartCollections: loadConfig error', err);
        });
    }

    function saveConfig(e) {
        e.preventDefault();
        themeRules = collectRules(view);

        ApiClient.getPluginConfiguration(PLUGIN_ID).then(function (config) {
            config.EnableSeriesCollections       = view.querySelector('#chkEnableSeries').checked;
            config.SeriesMinMovieCount           = parseInt(view.querySelector('#txtSeriesMin').value, 10) || 2;
            config.EnableThemeCollections        = view.querySelector('#chkEnableTheme').checked;
            config.SyncIntervalHours             = parseInt(view.querySelector('#txtSyncInterval').value, 10) || 24;
            config.ThemeCollectionRulesJson      = JSON.stringify(themeRules);
            config.UserDeletedCollectionKeysJson = JSON.stringify(userDeletedKeys);
            return ApiClient.updatePluginConfiguration(PLUGIN_ID, config);
        }).then(function () {
            Dashboard.processPluginConfigurationUpdateResult();
        }).catch(function (err) {
            console.error('SmartCollections: saveConfig error', err);
            Dashboard.alert('Failed to save configuration.');
        });
    }

    // Wire up buttons
    const addBtn = view.querySelector('#btnAddRule');
    if (addBtn) {
        addBtn.addEventListener('click', function () {
            themeRules = collectRules(view);
            themeRules.push({ Name: 'New Collection', TitleKeywords: '', GenreKeywords: '', TagKeywords: '', MinMovieCount: 2, MatchMode: 'Any', Enabled: true });
            refreshRulesTable();
        });
    }

    const form = view.querySelector('#SmartCollectionsConfigForm');
    if (form) {
        form.addEventListener('submit', saveConfig);
    }

    // viewshow fires when Jellyfin's view manager shows the page
    view.addEventListener('viewshow', loadConfig);
}
