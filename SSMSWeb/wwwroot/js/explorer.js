async function loadExplorer() {
    await Promise.all([loadDatabases(), loadLogins()]);
}

async function loadDatabases() {
    try {
        const response = await fetch('/Home/GetDatabases');
        if (response.ok) buildDatabasesTree(await response.json());
    } catch (e) {
        console.error('Erreur chargement bases de données:', e);
    }
}

async function loadLogins() {
    try {
        const response = await fetch('/Home/GetLogins');
        if (response.ok) buildLoginsTree(await response.json());
    } catch (e) {
        console.error('Erreur chargement logins:', e);
    }
}

function buildDatabasesTree(databases) {
    const container = document.getElementById('databasesTree');
    document.getElementById('dbCount').textContent = databases.length;
    _databasesList = databases;
    container.innerHTML = '';

    databases.forEach(name => {
        const wrapper = document.createElement('div');

        const dbRow = document.createElement('div');
        dbRow.className = 'tree-item db-item';
        dbRow.dataset.db = name;
        dbRow.dataset.loaded = 'false';
        dbRow.innerHTML =
            `<span class="db-toggle">&#9654;</span>` +
            `<span class="tree-icon">&#128452;</span>` +
            `<span class="db-name text-truncate">${escapeHtml(name)}</span>`;

        const tablesDiv = document.createElement('div');
        tablesDiv.className = 'db-tables-container';
        tablesDiv.style.display = 'none';

        dbRow.addEventListener('click', (e) => {
            if (e.target.classList.contains('db-toggle')) {
                toggleDatabaseTables(name, dbRow, tablesDiv);
            } else {
                selectDatabase(name, dbRow);
            }
        });

        wrapper.appendChild(dbRow);
        wrapper.appendChild(tablesDiv);
        container.appendChild(wrapper);
    });

    openTreeSection('databasesContent');
}

async function toggleDatabaseTables(dbName, dbRow, tablesDiv) {
    const toggle = dbRow.querySelector('.db-toggle');
    const isOpen = tablesDiv.style.display !== 'none';

    if (isOpen) {
        tablesDiv.style.display = 'none';
        toggle.classList.remove('open');
        return;
    }

    tablesDiv.style.display = 'block';
    toggle.classList.add('open');

    if (dbRow.dataset.loaded === 'false') {
        tablesDiv.innerHTML = '<div class="table-item text-muted fst-italic">Chargement...</div>';
        await loadTables(dbName, tablesDiv);
        dbRow.dataset.loaded = 'true';
    }
}

async function loadTables(dbName, container) {
    try {
        const response = await fetch(`/Home/GetTables?database=${encodeURIComponent(dbName)}`);
        if (!response.ok) { container.innerHTML = '<div class="table-item text-danger">Erreur de chargement</div>'; return; }

        const tables = await response.json();
        container.innerHTML = '';

        if (tables.length === 0) {
            container.innerHTML = '<div class="table-item text-muted fst-italic">Aucune table</div>';
            return;
        }

        _tableCache[dbName] = tables;
        tables.forEach(tableName => {
            const item = document.createElement('div');
            item.className = 'tree-item table-item';
            item.innerHTML = `<span class="tree-icon">&#9642;</span><span class="text-truncate">${escapeHtml(tableName)}</span>`;
            container.appendChild(item);
        });
    } catch {
        container.innerHTML = '<div class="table-item text-danger">Erreur réseau</div>';
    }
}

function buildLoginsTree(logins) {
    const container = document.getElementById('loginsTree');
    document.getElementById('loginCount').textContent = logins.length;
    container.innerHTML = '';

    logins.forEach(login => {
        const item = document.createElement('div');
        item.className = 'tree-item' + (login.isDisabled ? ' login-disabled' : '');

        const icon = login.typeDesc === 'SQL_LOGIN' ? '&#128273;' : '&#128100;';
        const typeLabel = login.typeDesc === 'SQL_LOGIN' ? 'SQL'
            : login.typeDesc === 'WINDOWS_LOGIN' ? 'Win' : 'Grp';
        const disabledBadge = login.isDisabled
            ? '<span class="login-type-badge text-danger">désactivé</span>' : '';

        item.innerHTML =
            `<span class="tree-icon">${icon}</span>` +
            `<span class="text-truncate flex-grow-1">${escapeHtml(login.name)}</span>` +
            `<span class="login-type-badge">${typeLabel}</span>` +
            disabledBadge;

        container.appendChild(item);
    });

    openTreeSection('loginsContent');
}

function selectDatabase(name, dbRow) {
    _activeDatabase = name;
    document.getElementById('activeDbDisplay').textContent = name;
    const editorDb = document.getElementById('editorActiveDb');
    if (editorDb) editorDb.textContent = name;

    document.querySelectorAll('#databasesTree .db-item').forEach(el => el.classList.remove('active-db'));
    dbRow?.classList.add('active-db');
}

function toggleTreeSection(contentId) {
    const content = document.getElementById(contentId);
    const icon = document.getElementById('toggle-' + contentId);
    const isOpen = content.style.display !== 'none';
    content.style.display = isOpen ? 'none' : 'block';
    icon.classList.toggle('open', !isOpen);
}

function openTreeSection(contentId) {
    document.getElementById(contentId).style.display = 'block';
    document.getElementById('toggle-' + contentId).classList.add('open');
}
