/**
 * FlowSpace — Documents Module
 * Connected 100% to REST API (/api/v1/documents/*)
 */
(function (FS, $) {
  'use strict';

  const FILE_ICONS = {
    folder: { icon: 'bi-folder-fill', color: '#f59e0b' },
    doc:    { icon: 'bi-file-earmark-text-fill', color: '#3b82f6' },
    sheet:  { icon: 'bi-file-earmark-spreadsheet-fill', color: '#10b981' },
    slide:  { icon: 'bi-file-earmark-slides-fill', color: '#f97316' },
    pdf:    { icon: 'bi-file-earmark-pdf-fill', color: '#ef4444' },
    image:  { icon: 'bi-file-earmark-image-fill', color: '#8b5cf6' }
  };

  FS.pages.documents = {
    _currentFolder: null,
    _search: '',
    _documents: [],
    _users: [],

    async init() {
      await this._loadUsers();
      await this._loadDocuments();
      this._bindEvents();
    },

    _getAuthHeaders() {
      const session = FS.auth.getSession();
      return session && session.token ? { 'Authorization': 'Bearer ' + session.token } : {};
    },

    async _loadUsers() {
      try {
        const res = await $.ajax({
          url: FS.API_BASE + '/api/v1/chat/users',
          type: 'GET',
          headers: this._getAuthHeaders()
        });
        if (res && res.success && Array.isArray(res.data)) {
          this._users = res.data;
        }
      } catch (err) {
        console.error('Failed to load users for documents:', err);
      }
    },

    async _loadDocuments() {
      try {
        const res = await $.ajax({
          url: FS.API_BASE + '/api/v1/documents',
          type: 'GET',
          headers: this._getAuthHeaders()
        });
        if (res && res.success && Array.isArray(res.data)) {
          this._documents = res.data;
        } else {
          this._documents = [];
        }
      } catch (err) {
        console.error('Failed to load documents:', err);
        this._documents = [];
      }
      this._renderTree();
      this._renderFiles();
    },

    _renderTree() {
      const folders = this._documents.filter(d => d.type === 'folder' && !d.parentId);
      const $tree = document.getElementById('doc-tree');
      if (!$tree) return;

      $tree.innerHTML =
        `<div class="doc-tree-item${!this._currentFolder ? ' active' : ''}" data-folder-id="">
          <i class="bi bi-house-door"></i> Tất cả
        </div>` +
        folders.map(f => `
          <div class="doc-tree-item${this._currentFolder === f.id ? ' active' : ''}" data-folder-id="${f.id}">
            <i class="bi bi-folder${this._currentFolder === f.id ? '-open' : ''}-fill"></i>
            <span class="truncate">${FS.str.escape(f.name)}</span>
          </div>`).join('');
    },

    _renderFiles() {
      let files;
      if (this._search) {
        const q = this._search.toLowerCase();
        files = this._documents.filter(d => (d.name || '').toLowerCase().includes(q) || (d.content || '').toLowerCase().includes(q));
      } else if (this._currentFolder) {
        files = this._documents.filter(d => d.parentId === this._currentFolder);
      } else {
        files = this._documents.filter(d => !d.parentId);
      }

      const session = FS.auth.getSession();
      const isManager = session?.role === 'manager' || session?.role === 'director';

      files = files.filter(d => {
        if (d.type === 'folder') return true;
        if (isManager) return true;
        if (d.createdBy === session?.userId) return true;
        if (d.sharedWith && d.sharedWith.includes(session?.userId)) return true;
        return true; // Fallback view for general users
      });

      const $bc = document.getElementById('doc-breadcrumb');
      if ($bc) {
        if (this._currentFolder) {
          const folder = this._documents.find(d => d.id === this._currentFolder);
          $bc.innerHTML = `
            <i class="bi bi-house cursor-pointer" id="doc-bc-root"></i>
            <i class="bi bi-chevron-right" style="font-size:10px"></i>
            <span style="color:var(--fs-text);font-weight:500">${FS.str.escape(folder?.name || 'Thư mục')}</span>`;
        } else {
          $bc.innerHTML = '<i class="bi bi-house"></i> Tất cả tài liệu';
        }
      }

      const $filesContainer = document.getElementById('doc-files');
      if (!$filesContainer) return;

      if (!files.length) {
        $filesContainer.innerHTML =
          '<div class="fs-empty"><i class="bi bi-folder2-open"></i><h5>Thư mục trống</h5><p>Tải lên hoặc tạo tài liệu mới</p></div>';
        return;
      }

      $filesContainer.innerHTML = `
        <div class="row g-2">
          ${files.map(f => {
            const meta = FILE_ICONS[f.type] || FILE_ICONS.doc;
            const creator = this._users.find(u => u.id === f.createdBy);
            const isFolder = f.type === 'folder';
            const subCount = isFolder ? this._documents.filter(d => d.parentId === f.id).length : 0;

            return `
              <div class="col-6 col-md-4 col-lg-3 col-xl-2">
                <div class="doc-file-card" data-doc-id="${f.id}" data-doc-type="${f.type}">
                  ${!isFolder ? `
                  <div class="doc-file-actions d-flex gap-1">
                    <div class="doc-action-btn doc-download-btn" data-id="${f.id}" title="Tải xuống"><i class="bi bi-download"></i></div>
                    <div class="doc-action-btn doc-delete-btn" data-id="${f.id}" title="Xóa"><i class="bi bi-trash"></i></div>
                  </div>` : `
                  <div class="doc-file-actions d-flex gap-1">
                    <div class="doc-action-btn doc-delete-btn" data-id="${f.id}" title="Xóa"><i class="bi bi-trash"></i></div>
                  </div>`}
                  <div class="doc-file-icon" style="color:${meta.color}">
                    <i class="bi ${meta.icon}"></i>
                  </div>
                  <div class="doc-file-name">${FS.str.escape(f.name)}</div>
                  <div class="doc-file-meta">${isFolder ? subCount + ' mục' : (f.size ? FS.str.fileSize(f.size) : '')} · ${creator?.name?.split(' ').pop() || 'Admin'}</div>
                </div>
              </div>`;
          }).join('')}
        </div>`;
    },

    _bindEvents() {
      const self = this;

      // Tree click
      $(document).off('click.docTree').on('click.docTree', '.doc-tree-item', function () {
        self._currentFolder = this.dataset.folderId || null;
        self._renderTree();
        self._renderFiles();
      });

      // File click
      $(document).off('click.docCard').on('click.docCard', '.doc-file-card', function (e) {
        if (e.target.closest('.doc-action-btn')) return;
        if (this.dataset.docType === 'folder') {
          self._currentFolder = this.dataset.docId;
          self._renderTree();
          self._renderFiles();
        } else {
          const doc = self._documents.find(d => d.id === this.dataset.docId);
          if (doc) self._previewDoc(doc);
        }
      });

      // Breadcrumb root
      $(document).off('click.docBcRoot').on('click.docBcRoot', '#doc-bc-root', function () {
        self._currentFolder = null;
        self._renderTree();
        self._renderFiles();
      });

      // Search
      const $search = document.getElementById('doc-search');
      if ($search) {
        $search.addEventListener('input', function () {
          self._search = this.value;
          self._renderFiles();
        });
      }

      // Upload file with API integration
      $('#doc-upload-btn').off('click').on('click', function () {
        const input = document.createElement('input');
        input.type = 'file';
        input.multiple = true;
        input.onchange = function () {
          const files = Array.from(this.files);
          let uploadedCount = 0;

          files.forEach(file => {
            const formData = new FormData();
            formData.append('file', file);
            if (self._currentFolder) {
              formData.append('parentId', self._currentFolder);
            }

            $.ajax({
              url: FS.API_BASE + '/api/v1/documents/upload',
              type: 'POST',
              data: formData,
              processData: false,
              contentType: false,
              headers: self._getAuthHeaders()
            }).done(function (res) {
              if (res && res.success) {
                uploadedCount++;
                if (uploadedCount === files.length) {
                  self._loadDocuments();
                  FS.toast(`Đã tải lên ${uploadedCount} tệp thành công!`, 'success');
                }
              }
            }).fail(function (err) {
              FS.toast('Lỗi khi tải lên tệp: ' + (err.responseJSON?.message || 'Error'), 'error');
            });
          });
        };
        input.click();
      });

      // New document
      $('#doc-new-doc-btn').off('click').on('click', function () {
        const name = prompt('Tên tài liệu mới:');
        if (!name) return;

        $.ajax({
          url: FS.API_BASE + '/api/v1/documents',
          type: 'POST',
          headers: self._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify({
            name: name,
            type: 'doc',
            parentId: self._currentFolder || null,
            content: 'Bắt đầu soạn thảo...'
          })
        }).done(function (res) {
          if (res && res.success) {
            self._loadDocuments();
            FS.toast('Đã tạo tài liệu mới', 'success');
          } else {
            FS.toast(res?.message || 'Lỗi khi tạo tài liệu', 'error');
          }
        }).fail(function () {
          FS.toast('Lỗi khi tạo tài liệu', 'error');
        });
      });

      // New folder
      $('#doc-new-folder-btn').off('click').on('click', function () {
        const name = prompt('Tên thư mục mới:');
        if (!name) return;

        $.ajax({
          url: FS.API_BASE + '/api/v1/documents',
          type: 'POST',
          headers: self._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify({
            name: name,
            type: 'folder',
            parentId: self._currentFolder || null
          })
        }).done(function (res) {
          if (res && res.success) {
            self._loadDocuments();
            FS.toast('Đã tạo thư mục mới', 'success');
          } else {
            FS.toast(res?.message || 'Lỗi khi tạo thư mục', 'error');
          }
        }).fail(function () {
          FS.toast('Lỗi khi tạo thư mục', 'error');
        });
      });

      // Download action
      $(document).off('click.docDownload').on('click.docDownload', '.doc-download-btn', function (e) {
        e.stopPropagation();
        const id = $(this).data('id');
        const doc = self._documents.find(d => d.id === id);
        if (!doc) return;

        if (doc.url) {
          window.open(FS.API_BASE + doc.url, '_blank');
          return;
        }

        const blob = new Blob([doc.content || 'Nội dung trống'], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = doc.name + (doc.type === 'doc' ? '.txt' : doc.type === 'sheet' ? '.csv' : '');
        a.click();
        URL.revokeObjectURL(url);
        FS.toast('Đang tải xuống: ' + doc.name, 'success');
      });

      // Delete action
      $(document).off('click.docDelete').on('click.docDelete', '.doc-delete-btn', function (e) {
        e.stopPropagation();
        if (!confirm('Bạn có chắc chắn muốn xóa tài liệu này?')) return;
        const id = $(this).data('id');

        $.ajax({
          url: FS.API_BASE + `/api/v1/documents/${id}`,
          type: 'DELETE',
          headers: self._getAuthHeaders()
        }).done(function (res) {
          if (res && res.success) {
            self._loadDocuments();
            FS.toast('Đã xóa tài liệu thành công', 'success');
          } else {
            FS.toast(res?.message || 'Không thể xóa tài liệu', 'error');
          }
        }).fail(function () {
          FS.toast('Lỗi khi xóa tài liệu', 'error');
        });
      });
    },

    _previewDoc(doc) {
      const creator = this._users.find(u => u.id === doc.createdBy);
      const meta = FILE_ICONS[doc.type] || FILE_ICONS.doc;
      const html = `
        <div class="fs-modal-overlay" id="doc-preview-overlay">
          <div class="fs-modal" style="max-width:680px;max-height:80vh">
            <div class="fs-modal-header">
              <div class="d-flex align-items-center gap-3">
                <i class="bi ${meta.icon}" style="font-size:24px;color:${meta.color}"></i>
                <div>
                  <h5 class="fs-h5 m-0">${FS.str.escape(doc.name)}</h5>
                  <div class="fs-small">${creator?.name || 'Admin'} · ${FS.date.format(doc.uploadedAt || doc.createdAt)} ${doc.size ? '· ' + FS.str.fileSize(doc.size) : ''}</div>
                </div>
              </div>
              <button class="btn btn-ghost btn-icon btn-sm" onclick="document.getElementById('doc-preview-overlay').remove()"><i class="bi bi-x-lg"></i></button>
            </div>
            <div class="fs-modal-body" style="min-height:200px">
              <div style="background:var(--fs-bg-secondary);border-radius:var(--fs-radius);padding:20px;font-size:13px;line-height:1.8;color:var(--fs-text-secondary)">
                ${FS.str.escape(doc.content || 'Tài liệu trống.')}
              </div>
            </div>
          </div>
        </div>`;
      document.body.insertAdjacentHTML('beforeend', html);
      document.getElementById('doc-preview-overlay').addEventListener('click', function (e) {
        if (e.target === this) this.remove();
      });
    }
  };

})(window.FS = window.FS || {}, jQuery);