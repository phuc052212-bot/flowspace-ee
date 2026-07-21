(function (FS, $) {
  'use strict';

  FS.pages.users = {
    _filter: { search: '', role: '' },
    _usersCache: null,
    async init() {
      if (!FS.auth.isDirector()) {
        document.getElementById('users-table-body').innerHTML =
          '<tr><td colspan="7"><div class="fs-empty"><i class="bi bi-shield-lock"></i><h5>Không có quyền truy cập</h5></div></td></tr>';
        return;
      }
      await this._loadUsers();
      this._render();
      this._bindEvents();
    },
    async _loadUsers() {
      try {
        const response = await FS.apiCall({
          url: FS.API_BASE + '/api/v1/chat/users',
          type: 'GET',
          headers: this._getAuthHeaders()
        });
        if (response && response.success && response.data) {
          this._usersCache = response.data;
          $('#users-offline-banner').remove();
        } else {
          this._usersCache = null;
          this._showOfflineBanner('Không nhận được dữ liệu người dùng hợp lệ. Đang hiển thị dữ liệu offline.');
        }
      } catch (err) {
        console.warn('Users API failed:', err);
        this._usersCache = null;
        this._showOfflineBanner('Không thể kết nối máy chủ. Đang hiển thị dữ liệu offline (Demo).');
      }
    },
    _getAuthHeaders() {
      const session = FS.auth.getSession();
      return session && session.token ? { 'Authorization': 'Bearer ' + session.token } : {};
    },
    _showOfflineBanner(message) {
      if (!$('#users-offline-banner').length) {
        $('#page-content').prepend(`
          <div id="users-offline-banner" class="fs-login-alert show" style="display:flex; margin-bottom:16px; background:#fff3cd; border:1px solid #ffeeba; color:#856404">
            <i class="bi bi-exclamation-triangle-fill" style="margin-right:8px"></i>
            <span>${message}</span>
          </div>
        `);
      }
    },
    _getData() {
      const users = this._usersCache || [];
      const { search, role } = this._filter;
      let filtered = users;
      if (search) {
        const q = search.toLowerCase();
        filtered = filtered.filter(u => (u.name + u.email).toLowerCase().includes(q));
      }
      if (role) filtered = filtered.filter(u => u.role === role);
      return filtered;
    },
    _render() {
      const users = this._getData();
      $('#users-count-label').text(`${users.length} người dùng`);

      // No fallback data; rely on API
      const tasks = [];
      const logs = [];
      const projects = [];

      $('#users-table-body').html(users.map(u => {
        const userTasks = tasks.filter(t => t.assigneeId === u.id);
        const userLogs = logs.filter(l => l.userId === u.id);
        const userProjects = [...new Set(userTasks.map(t => t.projectId))];
        const totalHours = userLogs.reduce((s, l) => s + (l.hours || 0), 0);
        const roleLabels = {
          employee: '<span class="fs-badge badge-neutral">Nhân viên</span>',
          team_lead: '<span class="fs-badge badge-accent">Trưởng nhóm</span>',
          manager: '<span class="fs-badge badge-warning">Quản lý</span>',
          director: '<span class="fs-badge badge-success">Ban GĐ</span>'
        };
        return `
          <tr class="hover-row">
            <td>
              <div class="d-flex align-items-center gap-3">
                <div class="fs-avatar ${u.color}">${u.avatar}</div>
                <div>
                  <div style="font-size:13px;font-weight:600">${FS.str.escape(u.name)}</div>
                  <div class="fs-small">${u.department || '—'}</div>
                </div>
              </div>
            </td>
            <td style="font-size:12px">${u.email}</td>
            <td>${roleLabels[u.role] || u.role}</td>
            <td style="font-size:13px">${userProjects.length}</td>
            <td>
              <div class="d-flex align-items-center gap-2">
                <span style="font-size:13px">${userTasks.filter(t=>t.status==='done').length}/${userTasks.length}</span>
              </div>
            </td>
            <td style="font-size:13px">${totalHours}h</td>
            <td>
              <button class="btn btn-ghost btn-icon btn-sm" title="Xem chi tiết" onclick="FS.toast('Xem hồ sơ ${FS.str.escape(u.name)}', 'info')">
                <i class="bi bi-eye"></i>
              </button>
            </td>
          </tr>`;
      }).join(''));
    },
    _bindEvents() {
      const self = this;
      $('#users-search').off('input').on('input', function () { self._filter.search = this.value; self._render(); });
      $('#users-filter-role').off('change').on('change', function () { self._filter.role = this.value; self._render(); });
    }
  };
})(window.FS = window.FS || {}, jQuery);