function openModal({ title, body, confirmLabel = 'Confirmer', confirmClass = 'btn-primary', onConfirm }) {
    document.getElementById('modalTitle').textContent = title;
    document.getElementById('modalBody').innerHTML = body;
    const btn = document.getElementById('modalConfirmBtn');
    btn.textContent = confirmLabel;
    btn.className = `btn btn-sm ${confirmClass}`;
    btn.onclick = onConfirm ?? null;
    bootstrap.Modal.getOrCreateInstance(document.getElementById('appModal')).show();
}

function closeModal() {
    bootstrap.Modal.getInstance(document.getElementById('appModal'))?.hide();
}

function showModalError(msg) {
    let el = document.getElementById('modal-error');
    if (!el) {
        el = document.createElement('div');
        el.id = 'modal-error';
        el.className = 'alert alert-danger py-1 small mt-2';
        document.getElementById('modalBody').appendChild(el);
    }
    el.textContent = msg;
}

// ── Bases de données ──────────────────────────────────────────

function openCreateDbModal() {
    openModal({
        title: 'Nouvelle base de données',
        body: `<div class="mb-2">
                   <label class="form-label small">Nom</label>
                   <input id="modal-dbName" class="form-control form-control-sm" placeholder="MaBase">
               </div>`,
        confirmLabel: 'Créer',
        confirmClass: 'btn-success',
        onConfirm: async () => {
            const name = document.getElementById('modal-dbName').value.trim();
            if (!name) { showModalError('Le nom est requis.'); return; }

            const res = await fetch('/Home/CreateDatabase', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name })
            });

            if (res.ok) {
                closeModal();
                await loadDatabases();   // rafraîchit le tree
            } else {
                const err = await res.json().catch(() => ({}));
                showModalError(err.message ?? 'Erreur lors de la création.');
            }
        }
    });
}

function openDropDbModal(name) {
    openModal({
        title: 'Supprimer la base de données',
        body: `<p class="mb-1">Voulez-vous vraiment supprimer <strong>${escapeHtml(name)}</strong> ?</p>
               <p class="text-danger small mb-0">Cette action est irréversible et fermera toutes les connexions actives.</p>`,
        confirmLabel: 'Supprimer',
        confirmClass: 'btn-danger',
        onConfirm: async () => {
            const res = await fetch('/Home/DropDatabase', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name })
            });

            if (res.ok) {
                closeModal();
                if (_activeDatabase === name) {
                    _activeDatabase = '';
                    document.getElementById('activeDbDisplay').textContent = '—';
                    document.getElementById('editorActiveDb').textContent = '—';
                }
                await loadDatabases();
            } else {
                const err = await res.json().catch(() => ({}));
                showModalError(err.message ?? 'Erreur lors de la suppression.');
            }
        }
    });
}

// ── Logins ────────────────────────────────────────────────────

function openCreateLoginModal() {
    openModal({
        title: 'Nouveau login',
        body: `<div class="mb-2">
                   <label class="form-label small">Nom</label>
                   <input id="modal-loginName" class="form-control form-control-sm">
               </div>
               <div class="mb-2">
                   <label class="form-label small">Type</label>
                   <select id="modal-loginType" class="form-select form-select-sm"
                           onchange="document.getElementById('modal-pwdBlock').style.display = this.value === 'sql' ? 'block' : 'none'">
                       <option value="sql">SQL Server</option>
                       <option value="windows">Windows</option>
                   </select>
               </div>
               <div id="modal-pwdBlock" class="mb-2">
                   <label class="form-label small">Mot de passe</label>
                   <input type="password" id="modal-loginPwd" class="form-control form-control-sm">
               </div>`,
        confirmLabel: 'Créer',
        confirmClass: 'btn-success',
        onConfirm: async () => {
            const name = document.getElementById('modal-loginName').value.trim();
            const type = document.getElementById('modal-loginType').value;
            const password = document.getElementById('modal-loginPwd')?.value ?? '';

            if (!name) { showModalError('Le nom est requis.'); return; }
            if (type === 'sql' && !password) { showModalError('Le mot de passe est requis.'); return; }

            const res = await fetch('/Home/CreateLogin', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, type, password })
            });

            if (res.ok) {
                closeModal();
                await loadLogins();
            } else {
                const err = await res.json().catch(() => ({}));
                showModalError(err.message ?? 'Erreur lors de la création.');
            }
        }
    });
}

function openDropLoginModal(name) {
    openModal({
        title: 'Supprimer le login',
        body: `<p class="mb-0">Voulez-vous vraiment supprimer le login <strong>${escapeHtml(name)}</strong> ?</p>`,
        confirmLabel: 'Supprimer',
        confirmClass: 'btn-danger',
        onConfirm: async () => {
            const res = await fetch('/Home/DropLogin', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name })
            });

            if (res.ok) {
                closeModal();
                await loadLogins();
            } else {
                const err = await res.json().catch(() => ({}));
                showModalError(err.message ?? 'Erreur lors de la suppression.');
            }
        }
    });
}

function openBackupDbModal(name) {
    openModal({
        title: `Sauvegarder la base de données`,
        body: `<div class="mb-2">
                   <label class="form-label small">Base de données</label>
                   <input class="form-control form-control-sm" value="${escapeHtml(name)}" disabled>
               </div>
               <div class="mb-2">
                   <label class="form-label small">Type de sauvegarde</label>
                   <select id="modal-backupType" class="form-select form-select-sm"
                           onchange="document.getElementById('modal-logWarning').style.display = this.value === 'LOG' ? 'block' : 'none'">
                       <option value="FULL">FULL — Complète</option>
                       <option value="DIFFERENTIAL">DIFFERENTIAL — Différentielle</option>
                       <option value="LOG">LOG — Journal de transactions</option>
                   </select>
                   <div id="modal-logWarning" class="alert alert-warning py-1 px-2 small mt-2 mb-0" style="display:none">
                       ⚠️ La sauvegarde du journal nécessite que la base soit en mode de récupération
                       <strong>FULL</strong> ou <strong>BULK_LOGGED</strong>.
                       En mode <strong>SIMPLE</strong>, cette opération échouera.
                   </div>
               </div>
               <div class="mb-2">
                   <label class="form-label small">Chemin du fichier de destination</label>
                   <input id="modal-backupPath" class="form-control form-control-sm"
                          placeholder="C:\\Backups\\MaBase.bak">
                   <div class="form-text">Chemin complet sur le serveur SQL (ex&nbsp;: C:\\Backups\\MaBase_FULL.bak)</div>
               </div>
               <div id="modal-backupProgress" style="display:none" class="mt-2">
                   <div class="progress" style="height:6px">
                       <div class="progress-bar progress-bar-striped progress-bar-animated w-100"></div>
                   </div>
                   <p class="small text-muted mt-1 mb-0">Sauvegarde en cours, veuillez patienter…</p>
               </div>`,
        confirmLabel: 'Sauvegarder',
        confirmClass: 'btn-warning',
        onConfirm: async () => {
            const path = document.getElementById('modal-backupPath').value.trim();
            const type = document.getElementById('modal-backupType').value;

            if (!path) { showModalError('Le chemin de destination est requis.'); return; }

            // Affiche la progress bar et désactive le bouton
            document.getElementById('modal-backupProgress').style.display = 'block';
            document.getElementById('modalConfirmBtn').disabled = true;

            const res = await fetch('/Home/BackupDatabase', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ name, type, path })
            });

            document.getElementById('modalConfirmBtn').disabled = false;
            document.getElementById('modal-backupProgress').style.display = 'none';

            if (res.ok) {
                closeModal();
                // Affiche un message de succès dans le panneau Messages de l'éditeur
                displayMessage(`Sauvegarde ${type} de "${name}" terminée → ${path}`, false);
                switchTab('messages');
            } else {
                const err = await res.json().catch(() => ({}));
                showModalError(err.message ?? 'Erreur lors de la sauvegarde.');
            }
        }
    });
}
