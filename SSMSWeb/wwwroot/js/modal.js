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

// Stubs à remplir lors de l'implémentation de chaque feature

function openCreateDbModal() {
    openModal({
        title: 'Nouvelle base de données',
        body: `<div class="mb-2">
                   <label class="form-label small">Nom</label>
                   <input id="modal-dbName" class="form-control form-control-sm" placeholder="MaBase">
               </div>`,
        confirmLabel: 'Créer',
        confirmClass: 'btn-success',
        onConfirm: () => closeModal()
    });
}

function openCreateLoginModal() {
    openModal({
        title: 'Nouveau login',
        body: `<div class="mb-2">
                   <label class="form-label small">Nom</label>
                   <input id="modal-loginName" class="form-control form-control-sm">
               </div>
               <div class="mb-2">
                   <label class="form-label small">Type</label>
                   <select id="modal-loginType" class="form-select form-select-sm">
                       <option value="sql">SQL Server</option>
                       <option value="windows">Windows</option>
                   </select>
               </div>
               <div class="mb-2">
                   <label class="form-label small">Mot de passe</label>
                   <input type="password" id="modal-loginPwd" class="form-control form-control-sm">
               </div>`,
        confirmLabel: 'Créer',
        confirmClass: 'btn-success',
        onConfirm: () => closeModal()
    });
}
