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