async function executeQuery() {
    const query = window.sqlEditor?.getValue()?.trim();
    if (!query) return;

    const btn = document.getElementById('executeBtn');
    btn.disabled = true;
    btn.textContent = 'Exécution...';

    try {
        const response = await fetch('/Home/ExecuteQuery', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ query, database: _activeDatabase })
        });

        const result = await response.json();

        if (result.type === 'results') {
            displayResults(result.columns, result.rows, result.truncated);
            switchTab('results');
        } else {
            displayMessage(result.text, result.type === 'error');
            switchTab('messages');
        }
    } catch {
        displayMessage('Erreur réseau lors de l\'exécution.', true);
        switchTab('messages');
    } finally {
        btn.disabled = false;
        btn.textContent = '▶ Exécuter';
    }
}

function displayResults(columns, rows, truncated) {
    const container = document.getElementById('resultsContent');

    if (rows.length === 0) {
        container.innerHTML = '<p class="text-muted small p-2">Requête exécutée — aucun résultat.</p>';
        return;
    }

    let html = '<table class="results-table"><thead><tr>';
    columns.forEach(col => { html += `<th>${escapeHtml(col)}</th>`; });
    html += '</tr></thead><tbody>';

    rows.forEach(row => {
        html += '<tr>';
        row.forEach(cell => {
            html += cell === null
                ? '<td><span class="null-value">NULL</span></td>'
                : `<td>${escapeHtml(String(cell))}</td>`;
        });
        html += '</tr>';
    });

    html += '</tbody></table>';
    html += `<div class="results-footer">${rows.length} ligne(s)${truncated ? ' — résultat tronqué à 1000 lignes' : ''}</div>`;

    container.innerHTML = html;
}

function displayMessage(text, isError = false) {
    document.getElementById('messagesContent').innerHTML =
        `<p class="${isError ? 'text-danger' : 'text-success'} small p-2 mb-0">${escapeHtml(text)}</p>`;
}

function switchTab(tab) {
    const isResults = tab === 'results';
    document.getElementById('tabResults').classList.toggle('active-tab', isResults);
    document.getElementById('tabMessages').classList.toggle('active-tab', !isResults);
    document.getElementById('resultsContent').style.display = isResults ? 'block' : 'none';
    document.getElementById('messagesContent').style.display = isResults ? 'none' : 'block';
}
