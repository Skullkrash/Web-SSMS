// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

let _isConnected = false;

async function connect(connectionString) {
    try {
        const response = await fetch('/Home/Connect', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ connectionString: connectionString })
        });
        
        _isConnected = response.ok;
        return _isConnected;
    }
    catch (error) {
        console.error("Erreur lors de la tentative de connexion :", error);
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
                console.log("Bases de données disponibles :");
                console.log(databases);
            }
            else {
                console.error("Erreur lors de la récupération des bases de données.");
            }
        }
        catch (error) {
            console.error("Erreur de communication avec le serveur :", error);
        }
    }
    else {
        console.warn("Veuillez vous connecter avant d'afficher les bases de données.");
    }
}