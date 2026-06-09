let _activeDatabase = '';
let _databasesList = [];
let _tableCache = {};

async function connect() {
    const serverName = document.getElementById('serverName')?.value?.trim();
    if (!serverName) {
        showConnectionError("Veuillez saisir le nom du serveur.");
        return;
    }

    const authType = document.getElementById('authType').value;
    const requestPayload = { serverName, authType, username: '', password: '' };

    if (authType === 'sql') {
        requestPayload.username = document.getElementById('username')?.value || '';
        requestPayload.password = document.getElementById('password')?.value || '';
        if (!requestPayload.username || !requestPayload.password) {
            showConnectionError("Veuillez saisir le nom d'utilisateur et le mot de passe.");
            return;
        }
    }

    const btn = document.getElementById('connectBtn');
    btn.disabled = true;
    btn.textContent = 'Connexion en cours...';
    hideConnectionError();

    try {
        const response = await fetch('/Home/Connect', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(requestPayload)
        });

        if (response.ok) {
            window.location.href = '/Home/Console';
        } else {
            const err = await response.json().catch(() => ({ message: 'Erreur inconnue.' }));
            showConnectionError(err.message || 'Echec de la connexion.');
            btn.disabled = false;
            btn.textContent = 'Se connecter';
        }
    } catch {
        showConnectionError("Erreur réseau. Vérifiez votre connexion.");
        btn.disabled = false;
        btn.textContent = 'Se connecter';
    }
}

async function disconnect() {
    try {
        await fetch('/Home/Disconnect', { method: 'POST' });
    } catch { /* ignore */ }
    window.location.href = '/Home/Login';
}

function showConnectionError(msg) {
    const el = document.getElementById('connectionError');
    el.textContent = msg;
    el.style.display = 'block';
}

function hideConnectionError() {
    document.getElementById('connectionError').style.display = 'none';
}

function authTypeChanged(value) {
    document.getElementById('sqlAuthFields').style.display = value === 'sql' ? 'block' : 'none';
}

function escapeHtml(str) {
    return str
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;');
}
