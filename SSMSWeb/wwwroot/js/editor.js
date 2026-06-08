const _HISTORY_KEY = 'ssmsQueryHistory';
const _HISTORY_MAX = 50;

async function executeQuery() {
    const query = window.sqlEditor?.getValue()?.trim();
    if (!query) return;

    const danger = _dangerousCommand(query);
    if (danger) {
        openModal({
            title: '&#9888; Commande potentiellement dangereuse',
            body: `<p class="mb-1">La requête contient : <strong>${escapeHtml(danger)}</strong></p>
                   <p class="text-danger small mb-0">Cette opération peut modifier ou supprimer des données de manière irréversible.</p>`,
            confirmLabel: 'Exécuter quand même',
            confirmClass: 'btn-danger',
            onConfirm: () => { closeModal(); _doExecuteQuery(query); }
        });
        return;
    }

    await _doExecuteQuery(query);
}

async function _doExecuteQuery(query) {
    const btn = document.getElementById('executeBtn');
    btn.disabled = true;
    btn.textContent = 'Exécution...';

    try {
        const response = await fetch('/Query/ExecuteQuery', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ query, database: _activeDatabase })
        });

        const result = await response.json();
        _addToHistory(query, _activeDatabase);

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

function _dangerousCommand(query) {
    const q = query.toUpperCase();
    if (/\bDELETE\b/.test(q) && !/\bWHERE\b/.test(q)) return 'DELETE sans clause WHERE';
    if (/\bUPDATE\b/.test(q) && !/\bWHERE\b/.test(q)) return 'UPDATE sans clause WHERE';
    if (/\bDROP\s+(TABLE|DATABASE|LOGIN|VIEW|PROCEDURE|FUNCTION)\b/.test(q)) return 'DROP sur un objet';
    if (/\bTRUNCATE\s+TABLE\b/.test(q)) return 'TRUNCATE TABLE';
    return null;
}

// === Historique ===

const _PLACEHOLDER = '-- Tapez votre requête SQL ici';

function _addToHistory(query, database) {
    const clean = query.startsWith(_PLACEHOLDER)
        ? query.slice(_PLACEHOLDER.length).trim()
        : query;
    if (!clean) return;
    const history = _getHistory();
    if (history.length > 0 && history[0].query === clean) return;
    history.unshift({ query: clean, database, time: new Date().toISOString() });
    if (history.length > _HISTORY_MAX) history.pop();
    localStorage.setItem(_HISTORY_KEY, JSON.stringify(history));
}

function _getHistory() {
    try { return JSON.parse(localStorage.getItem(_HISTORY_KEY) || '[]'); }
    catch { return []; }
}

function _renderHistory() {
    const container = document.getElementById('historyContent');
    const history = _getHistory();

    if (history.length === 0) {
        container.innerHTML = '<p class="text-muted small p-2 mb-0">Aucune requête dans l\'historique.</p>';
        return;
    }

    const clearBtn = `<div class="history-toolbar">
        <button class="btn btn-sm btn-link text-danger p-0" onclick="clearHistory()">Vider l'historique</button>
    </div>`;

    const items = history.map((item, i) => {
        const time = new Date(item.time).toLocaleTimeString('fr-FR', { hour: '2-digit', minute: '2-digit' });
        const preview = item.query.replace(/\s+/g, ' ').trim();
        const truncated = preview.length > 90 ? preview.substring(0, 90) + '…' : preview;
        const db = item.database ? `<span class="history-db">${escapeHtml(item.database)}</span>` : '';
        return `<div class="history-item" onclick="loadQueryFromHistory(${i})">
            <div class="history-meta"><span class="history-time">${time}</span>${db}</div>
            <div class="history-query">${escapeHtml(truncated)}</div>
        </div>`;
    }).join('');

    container.innerHTML = clearBtn + items;
}

function loadQueryFromHistory(index) {
    const history = _getHistory();
    if (index < 0 || index >= history.length) return;
    window.sqlEditor?.setValue(history[index].query);
    switchTab('results');
}

function clearHistory() {
    localStorage.removeItem(_HISTORY_KEY);
    _renderHistory();
}

// === Résultats ===

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
    ['results', 'messages', 'history'].forEach(t => {
        const id = 'tab' + t.charAt(0).toUpperCase() + t.slice(1);
        document.getElementById(id)?.classList.toggle('active-tab', t === tab);
        document.getElementById(t + 'Content').style.display = t === tab ? 'block' : 'none';
    });
    if (tab === 'history') _renderHistory();
}

function exportResultsAsCsv() {
    const table = document.querySelector('#resultsContent .results-table');
    if (!table) return;

    const rows = [];
    table.querySelectorAll('tr').forEach(tr => {
        const cells = [...tr.querySelectorAll('th, td')].map(cell => {
            const val = cell.querySelector('.null-value') ? '' : cell.textContent.trim();
            return '"' + val.replace(/"/g, '""') + '"';
        });
        rows.push(cells.join(','));
    });

    const blob = new Blob(['﻿' + rows.join('\r\n')], { type: 'text/csv;charset=utf-8;' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = 'resultats.csv';
    a.click();
    URL.revokeObjectURL(url);
}
