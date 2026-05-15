// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

let _isConnected = false;
let authenticationType = "windows";

async function connect() {
    const serverName = document.getElementById('serverName')?.value;
    const authType = authenticationType;

    if (!serverName) {
        alert("Veuillez saisir le nom du serveur.");
        return;
    }

    let requestPayload = {
        serverName: serverName,
        authType: authType,
        username: "",
        password: ""
    };

    if (authType === "sql") {
        requestPayload.username = document.getElementById('username')?.value || "";
        requestPayload.password = document.getElementById('password')?.value || "";

        if (!requestPayload.username || !requestPayload.password) {
            alert("Veuillez saisir le nom d'utilisateur et le mot de passe pour l'authentification SQL.");
            return;
        }
    }

    try {
        const response = await fetch('/Home/Connect', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(requestPayload)
        });

        _isConnected = response.ok;
        if (_isConnected) {
            alert("Connexion réussie !");
            updateUI(true);
        } else {
            const errorText = await response.text();
            alert("Échec de la connexion : " + errorText);
        }
        return _isConnected;
    }
    catch (error) {
        console.error("Erreur lors de la tentative de connexion :", error);
        alert("Erreur réseau");
        return false;
    }
}

function isConnected() {
    return _isConnected;
}

async function displayDatabases() {
    if (isConnected()) {
        try {
            const response = await fetch('/Home/GetDatabases');
            if (response.ok) {
                const databases = await response.json();

                // Afficher la liste en dessous du bouton
                const databasesList = document.getElementById('databasesList');
                databasesList.innerHTML = ''; // Nettoyer l'ancienne liste

                if (databases && databases.length > 0) {
                    databases.forEach(db => {
                        const li = document.createElement('li');
                        li.className = 'list-group-item';
                        li.textContent = db;
                        databasesList.appendChild(li);
                    });
                } else {
                    databasesList.innerHTML = '<li class="list-group-item text-muted">Aucune base de données trouvée.</li>';
                }
            }
            else {
                alert("Erreur lors de la récupération des bases de données.");
            }
        }
        catch (error) {
            alert("Erreur réseau lors de la récupération des bases de données.");
        }
    }
    else {
        alert("Veuillez vous connecter avant d'afficher les bases de données.");
    }
}

async function disconnect() {
    try {
        const response = await fetch('/Home/Disconnect', { method: 'POST' });
        if (response.ok) {
            _isConnected = false;
            updateUI(false);

            const databasesList = document.getElementById('databasesList');
            if (databasesList) {
                databasesList.innerHTML = '';
            }

            alert("Déconnecté avec succès.");
        } else {
            alert("Erreur lors de la déconnexion.");
        }
    } catch (error) {
        console.error("Erreur :", error);
    }
}

function updateUI(connected) {
    const connectionZone = document.getElementById('connectionZone');
    const disconnectZone = document.getElementById('disconnectZone');
    const databasesZone = document.getElementById('databasesZone');

    if (connected) {
        if (connectionZone) connectionZone.style.display = 'none';
        if (disconnectZone) disconnectZone.style.display = 'block';
        if (databasesZone) databasesZone.style.display = 'flex';
    } else {
        if (connectionZone) connectionZone.style.display = 'block';
        if (disconnectZone) disconnectZone.style.display = 'none';
        if (databasesZone) databasesZone.style.display = 'none';
    }
}

function authTypeChanged(newAuthType) {
    authenticationType = newAuthType;
    const sqlFields = document.getElementById('sqlAuthFields');

    if (sqlFields) {
        if (newAuthType === 'sql') {
            sqlFields.style.display = 'block';
        } else {
            sqlFields.style.display = 'none';
        }
    }
}