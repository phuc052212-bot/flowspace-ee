/**
 * FlowSpace — Settings Module
 * Quản lý cài đặt cá nhân và cấu hình quản trị hệ thống (Categories, SLA, Notification Templates connected to APIs)
 */
(function (FS, $) {
  'use strict';

  FS.pages.settings = {
    _activeTab: 'personal',
    _categories: [],
    _workflowRules: [],
    _slaSettings: [],
    _notificationTemplates: [],

    _getAuthHeaders() {
      const session = FS.auth.getSession();
      return session && session.token ? { 'Authorization': 'Bearer ' + session.token } : {};
    },

    async init() {
      // 1. Cài đặt cá nhân mặc định
      this._renderProfile();
      this._renderNotifToggles();
      this._loadThemeState();
      
      // 2. Phân quyền hiển thị các Tab cấu hình Admin
      this._checkPermissions();

      // 3. Khởi tạo dữ liệu từ REST APIs
      await this._loadAdminData();

      // 4. Bind events
      this._bindEvents();
      this._bindAdminEvents();

      // Hiển thị tab mặc định
      this._switchTab(this._activeTab);
    },

    _checkPermissions() {
      const level = FS.auth.getRoleLevel();
      if (level >= 3) {
        $('.fs-admin-only').show();
      } else {
        $('.fs-admin-only').hide();
      }

      if (level >= 4) {
        $('.fs-director-only').show();
        this._renderSystemInfo();
      } else {
        $('.fs-director-only').hide();
      }
    },

    async _loadAdminData() {
      try {
        const [catRes, wfRes, slaRes, tplRes] = await Promise.all([
          $.ajax({ url: FS.API_BASE + '/api/v1/categories', type: 'GET', headers: this._getAuthHeaders() }).catch(() => null),
          FS.apiCall({ url: FS.API_BASE + '/api/v1/workflowrules', type: 'GET' }).catch(() => null),
          $.ajax({ url: FS.API_BASE + '/api/v1/slasettings', type: 'GET', headers: this._getAuthHeaders() }).catch(() => null),
          $.ajax({ url: FS.API_BASE + '/api/v1/notificationtemplates', type: 'GET', headers: this._getAuthHeaders() }).catch(() => null)
        ]);

        this._categories = (catRes && catRes.success && Array.isArray(catRes.data)) ? catRes.data : [];
        
        if (wfRes && wfRes.success && Array.isArray(wfRes.data)) {
          this._workflowRules = wfRes.data.map(r => ({
            id: r.id,
            name: r.name,
            reqType: r.requestType,
            operator: r.minAmount ? 'gt' : 'eq',
            value: r.minAmount || 0,
            maxRole: r.sequenceSteps || 'team_lead'
          }));
        } else {
          this._workflowRules = JSON.parse(localStorage.getItem('fs_workflow_rules') || '[]');
        }

        this._slaSettings = (slaRes && slaRes.success && Array.isArray(slaRes.data)) ? slaRes.data : [];
        this._notificationTemplates = (tplRes && tplRes.success && Array.isArray(tplRes.data)) ? tplRes.data : [];
      } catch (err) {
        console.error('Failed to load settings admin data:', err);
      }
    },

    _switchTab(tabName) {
      this._activeTab = tabName;
      
      $('#settings-tabs .fs-tab').removeClass('active');
      $(`#settings-tabs .fs-tab[data-tab="${tabName}"]`).addClass('active');

      $('.settings-tab-content').hide();
      $(`#tab-${tabName}`).show();

      if (tabName === 'categories') {
        this._renderCategories();
      } else if (tabName === 'workflows') {
        this._renderWorkflows();
      } else if (tabName === 'sla') {
        this._renderSla();
      } else if (tabName === 'templates') {
        this._renderTemplates();
      }
    },

    /* ── 1. Cài đặt cá nhân (Profile, Theme, Notifs) ───────── */
    _renderProfile() {
      const session = FS.auth.getSession();
      if (!session) return;
      
      const $avatar = $('#settings-avatar');
      $avatar.text(session.avatar).removeClass().addClass('fs-avatar');
      if (session.color && session.color.startsWith('#')) {
        $avatar.css('background-color', session.color);
      } else {
        $avatar.addClass(session.color || 'av-indigo');
      }
      
      $('#settings-name').text(session.name);
      $('#settings-email').text(session.email);
      $('#settings-display-name').val(session.name);
      $('#settings-user-email').val(session.email);
      $('#settings-role-badge').html(`<span class="fs-badge badge-accent">${FS.auth.getRoleLabel(session.role)}</span>`);
    },

    _renderNotifToggles() {
      const toggles = [
        { key: 'notif_task_assign',    label: 'Khi được giao task' },
        { key: 'notif_task_due',       label: 'Task sắp đến hạn' },
        { key: 'notif_task_done',      label: 'Task hoàn thành' },
        { key: 'notif_request',        label: 'Yêu cầu cần duyệt' },
        { key: 'notif_mention',        label: 'Được nhắc tới trong chat' },
        { key: 'notif_project_update', label: 'Cập nhật dự án' }
      ];

      const prefs = JSON.parse(localStorage.getItem('fs_notif_prefs') || '{}');

      $('#settings-notif-toggles').html(toggles.map(t => {
        const checked = prefs[t.key] !== false;
        return `
          <div class="d-flex align-items-center justify-content-between py-2" style="border-bottom:1px solid var(--fs-border)">
            <span style="font-size:13px">${t.label}</span>
            <label class="fs-toggle">
              <input type="checkbox" class="notif-toggle" data-key="${t.key}" ${checked ? 'checked' : ''}>
              <span class="fs-toggle-slider"></span>
            </label>
          </div>`;
      }).join(''));
    },

    _loadThemeState() {
      const theme  = localStorage.getItem('fs_theme') || 'light';
      const accent = localStorage.getItem('fs_accent') || '#6366f1';
      const fontSize = localStorage.getItem('fs_font_size') || '14';

      if (theme === 'dark') {
        $('html').addClass('dark-mode');
        $('#theme-dark-btn').addClass('active');
        $('#theme-light-btn').removeClass('active');
      } else {
        $('html').removeClass('dark-mode');
        $('#theme-light-btn').addClass('active');
        $('#theme-dark-btn').removeClass('active');
      }

      $('.color-swatch').css('border-color', 'transparent');
      $(`.color-swatch[data-color="${accent}"]`).css('border-color', '#1e293b');

      $('#settings-font-size').val(fontSize);
      document.documentElement.style.setProperty('--fs-font-size', fontSize + 'px');
    },

    _renderSystemInfo() {
      $('#settings-system-row').show();
      const items = [
        { label: 'Người dùng',    value: (FS.db.get('users') || []).length,        icon: 'bi-people' },
        { label: 'Dự án',         value: (FS.db.get('projects') || []).length,      icon: 'bi-folder2' },
        { label: 'Tasks',          value: (FS.db.get('tasks') || []).length,         icon: 'bi-check-square' },
        { label: 'Nhật ký',       value: (FS.db.get('system_logs') || []).length,   icon: 'bi-journal' },
        { label: 'Tài liệu',      value: (FS.db.get('documents') || []).length,     icon: 'bi-file-earmark' },
        { label: 'Giờ được log',  value: (FS.db.get('time_logs') || []).reduce((s,l)=>s+(l.hours||0),0) + 'h', icon: 'bi-clock' }
      ];
      $('#settings-sys-stats').html(items.map(i => `
        <div class="col-6 col-md-4 col-lg-2">
          <div class="fs-stat-card" style="padding:12px">
            <div class="fs-stat-icon" style="width:32px;height:32px"><i class="bi ${i.icon}"></i></div>
            <div class="fs-stat-value" style="font-size:20px">${i.value}</div>
            <div class="fs-stat-label" style="font-size:11px">${i.label}</div>
          </div>
        </div>`).join(''));
    },

    /* ── 2. Cấu hình Admin: Categories CRUD ───────────────── */
    _renderCategories() {
      const type = $('#cat-select-type').val();
      const list = this._categories;
      const titleMap = {
        project_types: 'Loại dự án',
        task_types: 'Loại công việc',
        request_types: 'Loại yêu cầu phê duyệt',
        priorities: 'Mức độ ưu tiên'
      };

      $('#cat-list-title').text('Danh sách ' + titleMap[type]);

      if (!list.length) {
        $('#cat-list-body').html('<tr><td colspan="3"><div class="fs-empty"><i class="bi bi-tag"></i><p>Chưa có mục nào trong danh mục này</p></div></td></tr>');
        return;
      }

      $('#cat-list-body').html(list.map(item => `
        <tr class="hover-row">
          <td style="font-family:monospace;font-size:12px;color:var(--fs-text-muted)">${item.id}</td>
          <td style="font-weight:500">${FS.str.escape(item.name)}</td>
          <td style="text-align:right">
            <button class="btn btn-ghost btn-icon btn-sm cat-edit-btn" data-id="${item.id}" title="Sửa">
              <i class="bi bi-pencil"></i>
            </button>
            <button class="btn btn-ghost btn-icon btn-sm cat-delete-btn" data-id="${item.id}" title="Xoá">
              <i class="bi bi-trash text-danger"></i>
            </button>
          </td>
        </tr>`).join(''));
    },

    async _saveCategory() {
      const id = $('#cat-modal-id').val();
      const name = $('#cat-modal-name').val().trim();

      if (!name) {
        FS.toast('Vui lòng nhập tên mục!', 'warning');
        return;
      }

      try {
        const url = id ? `${FS.API_BASE}/api/v1/categories/${id}` : `${FS.API_BASE}/api/v1/categories`;
        const method = id ? 'PUT' : 'POST';

        const res = await $.ajax({
          url: url,
          type: method,
          headers: this._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify({ name: name, description: '' })
        });

        if (res && res.success) {
          $('#cat-modal-overlay').hide();
          await this._loadAdminData();
          this._renderCategories();
          FS.toast('Cập nhật danh mục thành công!', 'success');
        } else {
          FS.toast(res?.message || 'Lỗi khi cập nhật danh mục', 'error');
        }
      } catch (err) {
        console.error('Failed to save category:', err);
        FS.toast('Lỗi khi lưu danh mục', 'error');
      }
    },

    _deleteCategory(id) {
      const self = this;
      FS.confirm('Bạn có chắc chắn muốn xoá mục này khỏi danh mục?', async () => {
        try {
          const res = await $.ajax({
            url: `${FS.API_BASE}/api/v1/categories/${id}`,
            type: 'DELETE',
            headers: self._getAuthHeaders()
          });
          if (res && res.success) {
            await self._loadAdminData();
            self._renderCategories();
            FS.toast('Đã xoá mục danh mục!', 'success');
          } else {
            FS.toast(res?.message || 'Lỗi khi xoá mục danh mục', 'error');
          }
        } catch (err) {
          console.error('Failed to delete category:', err);
          FS.toast('Lỗi khi xoá mục danh mục', 'error');
        }
      }, { danger: true, confirmText: 'Xoá' });
    },

    /* ── 3. Cấu hình Admin: Workflow Engine CRUD ──────────── */
    _renderWorkflows() {
      const reqLabels = { leave: 'Nghỉ phép', overtime: 'Tăng ca', purchase: 'Mua sắm', remote: 'Làm remote' };
      
      if (!this._workflowRules.length) {
        $('#wf-rules-body').html('<tr><td colspan="5"><div class="fs-empty"><i class="bi bi-diagram-3"></i><p>Chưa có quy tắc phê duyệt nào</p></div></td></tr>');
        return;
      }

      $('#wf-rules-body').html(this._workflowRules.map(rule => {
        const valueNum = parseInt(rule.value) || 0;
        const conditionText = rule.operator === 'gt' 
          ? `Lớn hơn (>) ${valueNum.toLocaleString()}` 
          : `Bằng (=) ${valueNum.toLocaleString()}`;
        
        return `
          <tr class="hover-row">
            <td style="font-weight:600;font-size:13px">${FS.str.escape(rule.name)}</td>
            <td><span class="fs-badge badge-neutral">${reqLabels[rule.reqType] || rule.reqType}</span></td>
            <td style="font-family:monospace;font-size:12px">${conditionText}</td>
            <td><span class="fs-badge badge-accent">${FS.str.escape(rule.maxRole)}</span></td>
            <td style="text-align:right">
              <button class="btn btn-ghost btn-icon btn-sm wf-edit-btn" data-id="${rule.id}" title="Sửa">
                <i class="bi bi-pencil"></i>
              </button>
              <button class="btn btn-ghost btn-icon btn-sm wf-delete-btn" data-id="${rule.id}" title="Xoá">
                <i class="bi bi-trash text-danger"></i>
              </button>
            </td>
          </tr>`;
      }).join(''));
    },

    async _saveWorkflowRule() {
      const id = $('#wf-modal-id').val();
      const name = $('#wf-modal-name').val().trim();
      const reqType = $('#wf-modal-req-type').val();
      const operator = $('#wf-modal-operator').val();
      const value = parseInt($('#wf-modal-value').val());
      const maxRole = $('#wf-modal-role').val();

      if (!name) {
        FS.toast('Vui lòng nhập tên quy tắc!', 'warning');
        return;
      }
      if (isNaN(value) || value < 0) {
        FS.toast('Giá trị so sánh không hợp lệ!', 'warning');
        return;
      }

      const payload = {
        requestType: reqType,
        name: name,
        minAmount: operator === 'gt' ? value : null,
        maxAmount: null,
        sequenceSteps: maxRole,
        isActive: true
      };

      try {
        let response;
        if (id && !id.startsWith('wf_rule_')) {
          response = await FS.apiCall({
            url: FS.API_BASE + '/api/v1/workflowrules/' + id,
            type: 'PUT',
            data: payload
          });
        } else {
          response = await FS.apiCall({
            url: FS.API_BASE + '/api/v1/workflowrules',
            type: 'POST',
            data: payload
          });
        }

        if (response && response.success) {
          FS.toast('Cập nhật quy tắc phê duyệt thành công!', 'success');
          $('#wf-modal-overlay').hide();
          await this._loadAdminData();
          this._renderWorkflows();
        } else {
          FS.toast('Máy chủ báo lỗi khi cập nhật quy tắc.', 'error');
        }
      } catch (err) {
        console.error('Save workflow rule error:', err);
        FS.toast('Không thể cập nhật quy tắc lên máy chủ. Vui lòng thử lại!', 'error');
      }
    },

    async _deleteWorkflowRule(id) {
      FS.confirm('Xoá quy tắc phê duyệt này? Yêu cầu mới sẽ áp dụng quy trình mặc định.', async () => {
        if (id && !id.startsWith('wf_rule_')) {
          try {
            const response = await FS.apiCall({
              url: FS.API_BASE + '/api/v1/workflowrules/' + id,
              type: 'DELETE'
            });
            if (response && response.success) {
              FS.toast('Đã xoá quy tắc phê duyệt!', 'success');
              await this._loadAdminData();
              this._renderWorkflows();
              return;
            }
          } catch (err) {
            console.error('Delete workflow rule failed:', err);
          }
          FS.toast('Không thể xoá quy tắc trên máy chủ.', 'error');
        }
      }, { danger: true, confirmText: 'Xoá' });
    },

    /* ── 4. Cấu hình Admin: SLA Settings ──────────────────── */
    _renderSla() {
      const list = this._slaSettings.length ? this._slaSettings : [
        { id: '1', name: 'Nghỉ phép', priority: 'Medium', responseTimeHours: 4, resolutionTimeHours: 24 },
        { id: '2', name: 'Tăng ca', priority: 'High', responseTimeHours: 2, resolutionTimeHours: 12 },
        { id: '3', name: 'Mua sắm', priority: 'High', responseTimeHours: 8, resolutionTimeHours: 48 },
        { id: '4', name: 'Remote', priority: 'Low', responseTimeHours: 4, resolutionTimeHours: 24 }
      ];

      $('#sla-settings-list').html(list.map(sla => `
        <div class="row g-2 align-items-center py-2" style="border-bottom:1px solid var(--fs-border)">
          <div class="col-12 col-md-5" style="font-weight:600;font-size:13px">
            ${FS.str.escape(sla.name)} (${sla.priority})
          </div>
          <div class="col-12 col-md-7 d-flex align-items-center gap-2">
            <input type="number" class="fs-input sla-hours-input" data-id="${sla.id}" data-name="${sla.name}" data-priority="${sla.priority}" value="${sla.resolutionTimeHours || 24}" min="1" style="max-width:100px">
            <span style="font-size:13px;color:var(--fs-text-secondary)">giờ xử lý</span>
          </div>
        </div>`).join(''));
    },

    async _saveSla() {
      const self = this;
      const promises = [];

      $('.sla-hours-input').each(function () {
        const id = $(this).data('id');
        const name = $(this).data('name');
        const priority = $(this).data('priority');
        const hours = parseInt($(this).val()) || 24;

        const payload = {
          name: name,
          priority: priority,
          responseTimeHours: Math.max(1, Math.floor(hours / 4)),
          resolutionTimeHours: hours,
          isActive: true
        };

        if (id && id.length > 10) {
          promises.push($.ajax({
            url: `${FS.API_BASE}/api/v1/slasettings/${id}`,
            type: 'PUT',
            headers: self._getAuthHeaders(),
            contentType: 'application/json',
            data: JSON.stringify(payload)
          }));
        } else {
          promises.push($.ajax({
            url: `${FS.API_BASE}/api/v1/slasettings`,
            type: 'POST',
            headers: self._getAuthHeaders(),
            contentType: 'application/json',
            data: JSON.stringify(payload)
          }));
        }
      });

      try {
        await Promise.all(promises);
        await this._loadAdminData();
        FS.toast('Lưu thiết lập SLA thành công!', 'success');
      } catch (err) {
        console.error('Failed to save SLA settings:', err);
        FS.toast('Lỗi khi lưu SLA settings', 'error');
      }
    },

    /* ── 5. Cấu hình Admin: Notification Templates ────────── */
    _renderTemplates(selectId = null) {
      const $select = $('#template-select');
      if (!this._notificationTemplates.length) {
        $select.html('<option value="">-- Chưa có mẫu nào --</option>');
        $('#template-edit-title').text('Nội dung mẫu');
        $('#template-subject').val('');
        $('#template-body').val('');
        $('#template-preview-subject').text('');
        $('#template-preview-body').text('');
        return;
      }

      $select.html(this._notificationTemplates.map(t => 
        `<option value="${t.id}">${FS.str.escape(t.name)} (${t.code})</option>`).join(''));

      const idToSelect = selectId || this._notificationTemplates[0].id;
      $select.val(idToSelect);
      this._loadTemplate(idToSelect);
    },

    _loadTemplate(id) {
      const template = this._notificationTemplates.find(t => t.id === id);
      if (!template) return;

      $('#template-edit-title').text('Nội dung mẫu: ' + template.name);
      $('#template-subject').val(template.subject || '');
      $('#template-body').val(template.body || '');
      this._updateTemplatePreview();
    },

    async _saveTemplate() {
      const id = $('#template-select').val();
      if (!id) return;

      const template = this._notificationTemplates.find(t => t.id === id);
      if (!template) return;

      const subject = $('#template-subject').val().trim();
      const body = $('#template-body').val().trim();

      if (!subject || !body) {
        FS.toast('Tiêu đề và Nội dung không được để trống!', 'warning');
        return;
      }

      const payload = {
        code: template.code,
        name: template.name,
        subject: subject,
        body: body,
        channel: template.channel || 'InApp',
        isActive: true
      };

      try {
        const res = await $.ajax({
          url: `${FS.API_BASE}/api/v1/notificationtemplates/${id}`,
          type: 'PUT',
          headers: this._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify(payload)
        });

        if (res && res.success) {
          await this._loadAdminData();
          this._updateTemplatePreview();
          FS.toast('Đã cập nhật mẫu thông báo thành công!', 'success');
        } else {
          FS.toast(res?.message || 'Lỗi khi cập nhật mẫu thông báo', 'error');
        }
      } catch (err) {
        console.error('Failed to save template:', err);
        FS.toast('Lỗi khi lưu mẫu thông báo', 'error');
      }
    },

    _deleteTemplate() {
      const id = $('#template-select').val();
      if (!id) {
        FS.toast('Không có mẫu nào để xoá!', 'warning');
        return;
      }

      const self = this;
      FS.confirm('Xoá mẫu thông báo này? Hệ thống sẽ mất mẫu gửi sự kiện tương ứng.', async () => {
        try {
          const res = await $.ajax({
            url: `${FS.API_BASE}/api/v1/notificationtemplates/${id}`,
            type: 'DELETE',
            headers: self._getAuthHeaders()
          });
          if (res && res.success) {
            await self._loadAdminData();
            self._renderTemplates();
            FS.toast('Đã xoá mẫu thông báo!', 'success');
          } else {
            FS.toast(res?.message || 'Không thể xoá mẫu thông báo', 'error');
          }
        } catch (err) {
          console.error('Failed to delete template:', err);
          FS.toast('Lỗi khi xoá mẫu thông báo', 'error');
        }
      }, { danger: true, confirmText: 'Xoá' });
    },

    async _saveNewTemplate() {
      const code = $('#template-modal-key').val().trim().toUpperCase();
      const name = $('#template-modal-name').val().trim();
      const subject = $('#template-modal-subject').val().trim();
      const body = $('#template-modal-body').val().trim();

      if (!code || !name || !subject || !body) {
        FS.toast('Vui lòng điền đầy đủ tất cả các trường!', 'warning');
        return;
      }

      const payload = {
        code: code,
        name: name,
        subject: subject,
        body: body,
        channel: 'InApp',
        isActive: true
      };

      try {
        const res = await $.ajax({
          url: `${FS.API_BASE}/api/v1/notificationtemplates`,
          type: 'POST',
          headers: this._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify(payload)
        });

        if (res && res.success) {
          $('#template-modal-overlay').hide();
          await this._loadAdminData();
          this._renderTemplates(res.data.id);
          FS.toast('Đã thêm mẫu thông báo mới thành công!', 'success');
        } else {
          FS.toast(res?.message || 'Lỗi khi tạo mẫu thông báo', 'error');
        }
      } catch (err) {
        console.error('Failed to create template:', err);
        FS.toast('Lỗi khi tạo mẫu thông báo mới', 'error');
      }
    },

    _updateTemplatePreview() {
      const subject = $('#template-subject').val() || '';
      const body = $('#template-body').val() || '';
      
      const replaceMap = {
        '{user_name}': 'Phạm Thanh Dung',
        '{task_title}': 'Thiết kế UI Dashboard mới',
        '{project_name}': 'FlowSpace Platform v2',
        '{due_date}': '15/07/2026',
        '{request_title}': 'Xin nghỉ phép 2 ngày',
        '{approver_name}': 'Lê Minh Cường',
        '{note}': 'Đồng ý phê duyệt.'
      };

      let renderedSubject = subject;
      let renderedBody = body;

      Object.entries(replaceMap).forEach(([placeholder, val]) => {
        renderedSubject = renderedSubject.split(placeholder).join(val);
        renderedBody = renderedBody.split(placeholder).join(val);
      });

      $('#template-preview-subject').text(renderedSubject);
      $('#template-preview-body').text(renderedBody);
    },

    /* ── Bind Event Listeners ────────────────────────────── */
    _bindEvents() {
      const self = this;

      $(document).off('click.settings-tab').on('click.settings-tab', '#settings-tabs .fs-tab', function (e) {
        e.preventDefault();
        self._switchTab($(this).data('tab'));
      });

      $('#settings-save-profile').off('click').on('click', function () {
        const name = $('#settings-display-name').val().trim();
        const email = $('#settings-user-email').val().trim();
        if (!name || !email) {
          FS.toast('Vui lòng điền đầy đủ tên và email.', 'error');
          return;
        }

        if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email)) {
          FS.toast('Email không hợp lệ.', 'error');
          return;
        }

        const session = FS.auth.getSession();
        if (session) {
          const avatar = name.split(' ').map(w => w[0]).join('').slice(0, 2).toUpperCase();
          const updated = FS.auth.updateUser({ name, email, avatar });
          
          if (updated) {
            $('#sidebar-user-name').text(name);
            $('#topbar-user-name').text(name.split(' ').pop());
            $('#topbar-user-avatar').text(avatar);
            FS.toast('Đã cập nhật hồ sơ thành công!', 'success');
            self._renderProfile();
          } else {
            FS.toast('Cập nhật hồ sơ thất bại.', 'error');
          }
        }
      });

      $(document).off('click.theme').on('click.theme', '.theme-btn', function () {
        const theme = $(this).data('theme');
        $('.theme-btn').removeClass('active');
        $(this).addClass('active');
        if (theme === 'dark') {
          $('html').addClass('dark-mode');
        } else {
          $('html').removeClass('dark-mode');
        }
        localStorage.setItem('fs_theme', theme);
        FS.toast(`Đã chuyển sang chế độ ${theme === 'dark' ? 'tối 🌙' : 'sáng ☀️'}`, 'success');
      });

      $(document).off('click.accent').on('click.accent', '.color-swatch', function () {
        const color = $(this).data('color');
        $('.color-swatch').css('border-color', 'transparent');
        $(this).css('border-color', '#1e293b');
        document.documentElement.style.setProperty('--fs-accent', color);
        document.documentElement.style.setProperty('--fs-accent-light', color + '20');
        localStorage.setItem('fs_accent', color);
        FS.toast('Đã thay đổi màu accent', 'success');
      });

      $('#settings-font-size').off('change').on('change', function () {
        const size = this.value;
        document.documentElement.style.setProperty('--fs-font-size', size + 'px');
        localStorage.setItem('fs_font_size', size);
        FS.toast(`Cỡ chữ: ${size}px`, 'success');
      });

      $(document).off('change.notif').on('change.notif', '.notif-toggle', function () {
        const prefs = JSON.parse(localStorage.getItem('fs_notif_prefs') || '{}');
        prefs[this.dataset.key] = this.checked;
        localStorage.setItem('fs_notif_prefs', JSON.stringify(prefs));
      });
    },

    _bindAdminEvents() {
      const self = this;

      $('#cat-select-type').off('change').on('change', () => this._renderCategories());

      $('#cat-add-btn').off('click').on('click', function () {
        $('#cat-modal-title').text('Thêm mục danh mục');
        $('#cat-modal-id').val('');
        $('#cat-modal-name').val('');
        $('#cat-modal-overlay').show();
      });

      $(document).off('click.cat-edit').on('click.cat-edit', '.cat-edit-btn', function () {
        const id = $(this).data('id');
        const item = self._categories.find(x => x.id === id);
        if (item) {
          $('#cat-modal-title').text('Sửa mục danh mục');
          $('#cat-modal-id').val(item.id);
          $('#cat-modal-name').val(item.name);
          $('#cat-modal-overlay').show();
        }
      });

      $(document).off('click.cat-delete').on('click.cat-delete', '.cat-delete-btn', function () {
        self._deleteCategory($(this).data('id'));
      });

      $('#cat-modal-close, #cat-modal-cancel').off('click').on('click', () => $('#cat-modal-overlay').hide());
      $('#cat-modal-overlay').off('click').on('click', function (e) {
        if (e.target === this) $(this).hide();
      });
      $('#cat-modal-save').off('click').on('click', () => this._saveCategory());

      $('#wf-add-btn').off('click').on('click', function () {
        $('#wf-modal-title').text('Thêm quy tắc phê duyệt');
        $('#wf-modal-id').val('');
        $('#wf-modal-name').val('');
        $('#wf-modal-req-type').val('leave');
        $('#wf-modal-operator').val('gt');
        $('#wf-modal-value').val('');
        $('#wf-modal-role').val('team_lead');
        $('#wf-modal-overlay').show();
      });

      $(document).off('click.wf-edit').on('click.wf-edit', '.wf-edit-btn', function () {
        const id = $(this).data('id');
        const rule = self._workflowRules.find(x => x.id === id);
        if (rule) {
          $('#wf-modal-title').text('Sửa quy tắc phê duyệt');
          $('#wf-modal-id').val(rule.id);
          $('#wf-modal-name').val(rule.name);
          $('#wf-modal-req-type').val(rule.reqType);
          $('#wf-modal-operator').val(rule.operator);
          $('#wf-modal-value').val(rule.value);
          $('#wf-modal-role').val(rule.maxRole);
          $('#wf-modal-overlay').show();
        }
      });

      $(document).off('click.wf-delete').on('click.wf-delete', '.wf-delete-btn', function () {
        self._deleteWorkflowRule($(this).data('id'));
      });

      $('#wf-modal-close, #wf-modal-cancel').off('click').on('click', () => $('#wf-modal-overlay').hide());
      $('#wf-modal-overlay').off('click').on('click', function (e) {
        if (e.target === this) $(this).hide();
      });
      $('#wf-modal-save').off('click').on('click', () => this._saveWorkflowRule());

      $('#sla-save-btn').off('click').on('click', () => this._saveSla());

      $('#template-select').off('change').on('change', function () {
        self._loadTemplate(this.value);
      });
      $('#template-save-btn').off('click').on('click', () => this._saveTemplate());

      $('#template-subject, #template-body').off('input').on('input', () => this._updateTemplatePreview());

      $('#template-add-btn').off('click').on('click', function () {
        $('#template-modal-key').val('');
        $('#template-modal-name').val('');
        $('#template-modal-subject').val('');
        $('#template-modal-body').val('');
        $('#template-modal-overlay').show();
      });

      $('#template-delete-btn').off('click').on('click', () => this._deleteTemplate());

      $('#template-modal-close, #template-modal-cancel').off('click').on('click', () => $('#template-modal-overlay').hide());
      $('#template-modal-overlay').off('click').on('click', function (e) {
        if (e.target === this) $(this).hide();
      });
      $('#template-modal-save').off('click').on('click', () => this._saveNewTemplate());
    }
  };

})(window.FS = window.FS || {}, jQuery);