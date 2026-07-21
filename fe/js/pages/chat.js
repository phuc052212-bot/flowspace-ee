/**
 * FlowSpace — Chat Module
 * Connected 100% to REST API (/api/v1/chat/*) & SignalR Real-time Hubs
 */
(function (FS, $) {
  'use strict';

  FS.pages.chat = {
    _currentChannel: null,
    _searchQuery: '',
    _replyingTo: null,
    _mentionUsers: [],
    _mentionIndex: -1,
    _connection: null,

    // In-memory data caches (No primary localStorage usage)
    _channels: [],
    _users: [],
    _messagesMap: {},

    _getAuthHeaders() {
      const session = FS.auth.getSession();
      return session && session.token ? { 'Authorization': 'Bearer ' + session.token } : {};
    },

    async init() {
      await this._loadUsers();
      await this._loadChannels();
      this._bindEvents();

      if (this._channels.length) {
        await this._openChannel(this._channels[0].id);
      }

      await this._initSignalR();
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
        console.error('Failed to load users for chat:', err);
      }
    },

    async _loadChannels() {
      try {
        const res = await $.ajax({
          url: FS.API_BASE + '/api/v1/chat/channels',
          type: 'GET',
          headers: this._getAuthHeaders()
        });
        if (res && res.success && Array.isArray(res.data)) {
          this._channels = res.data;
        } else {
          this._channels = [];
        }
      } catch (err) {
        console.error('Failed to load channels:', err);
        this._channels = [];
      }
      this._renderChannelList();
    },

    async _initSignalR() {
      const session = FS.auth.getSession();
      if (!session || !session.token) return;

      const connectionUrl = FS.API_BASE + '/hubs/chat?access_token=' + encodeURIComponent(session.token);

      this._connection = new signalR.HubConnectionBuilder()
        .withUrl(connectionUrl)
        .withAutomaticReconnect()
        .build();

      this._connection.onreconnecting((error) => {
        console.warn('SignalR Reconnecting...', error);
        if (!$('#chat-reconnect-banner').length) {
          $('#page-content').prepend('<div id="chat-reconnect-banner" class="fs-login-alert show" style="display:flex; margin-bottom:16px; background:var(--fs-warning-light); border-color:var(--fs-warning)"><i class="bi bi-exclamation-triangle-fill" style="color:var(--fs-warning)"></i><span>Đang kết nối lại chat...</span></div>');
        }
      });

      this._connection.onreconnected((connectionId) => {
        console.log('SignalR Reconnected.', connectionId);
        $('#chat-reconnect-banner').remove();
        if (this._currentChannel) {
          this._connection.invoke("JoinChannel", this._currentChannel).catch(err => console.error(err));
        }
      });

      this._connection.onclose((error) => {
        console.error('SignalR connection closed.', error);
      });

      // SignalR Event Listeners
      this._connection.on("ReceiveMessage", (channelId, userJson, messageJson) => {
        const msg = typeof messageJson === 'string' ? JSON.parse(messageJson) : messageJson;
        if (!this._messagesMap[channelId]) this._messagesMap[channelId] = [];

        // Check if message is already in memory cache
        if (!this._messagesMap[channelId].some(m => m.id === msg.id)) {
          this._messagesMap[channelId].push(msg);
        }

        if (this._currentChannel === channelId) {
          this._renderMessages(channelId);
        }
      });

      this._connection.on("MessageRecalled", (channelId, msgId) => {
        const channelMsgs = this._messagesMap[channelId] || [];
        const msg = channelMsgs.find(m => m.id === msgId);
        if (msg) {
          msg.isRecalled = true;
          msg.content = "Tin nhắn này đã bị thu hồi.";
          if (this._currentChannel === channelId) {
            this._renderMessages(channelId);
          }
        }
      });

      this._connection.on("MessagePinned", (channelId, msgId, isPinned) => {
        const channelMsgs = this._messagesMap[channelId] || [];
        const msg = channelMsgs.find(m => m.id === msgId);
        if (msg) {
          msg.isPinned = isPinned;
          if (this._currentChannel === channelId) {
            this._renderMessages(channelId);
          }
        }
      });

      try {
        await this._connection.start();
        console.log('SignalR Chat Connected.');
        if (this._currentChannel) {
          await this._connection.invoke("JoinChannel", this._currentChannel);
        }
      } catch (err) {
        console.error('SignalR start failed:', err);
      }
    },

    _renderChannelList() {
      const publicCh = this._channels.filter(c => !c.isDirectMessage);
      const dms      = this._channels.filter(c => c.isDirectMessage);

      const $channelsList = document.getElementById('chat-channels-list');
      if ($channelsList) {
        $channelsList.innerHTML = publicCh.map(c => `
          <div class="chat-channel-item${this._currentChannel === c.id ? ' active' : ''}" data-channel-id="${c.id}">
            <span class="ch-hash">#</span>
            <span>${FS.str.escape(c.name)}</span>
          </div>`).join('');
      }

      const $dmList = document.getElementById('chat-dm-list');
      if ($dmList) {
        const session = FS.auth.getSession();
        $dmList.innerHTML = dms.map(c => {
          const partner = this._users.find(u => u.id !== session?.userId) || {};
          return `
            <div class="chat-channel-item${this._currentChannel === c.id ? ' active' : ''}" data-channel-id="${c.id}">
              <div class="fs-avatar fs-avatar-sm ${partner?.color || 'av-indigo'}" style="width:18px;height:18px;font-size:9px">${partner?.avatar || '?'}</div>
              <span>${FS.str.escape(c.name || partner?.name || 'DM')}</span>
            </div>`;
        }).join('');
      }
    },

    async _openChannel(channelId) {
      const oldChannel = this._currentChannel;
      this._currentChannel = channelId;
      const channel = this._channels.find(c => c.id === channelId);

      // Update header
      const $name = document.getElementById('chat-channel-name');
      const $desc = document.getElementById('chat-channel-desc');
      if ($name) $name.textContent = channel ? (!channel.isDirectMessage ? '#' + channel.name : channel.name) : 'Chat';
      if ($desc) $desc.textContent = channel ? (channel.description || '') : '';

      // Reset UI states
      this._searchQuery = '';
      const searchInput = document.getElementById('chat-search-input');
      if (searchInput) searchInput.value = '';
      this._cancelReply();
      this._hideMentionDropdown();

      const $inputArea = document.getElementById('chat-input-area');
      if ($inputArea) $inputArea.style.display = '';
      const $chatInput = document.getElementById('chat-input');
      if ($chatInput) $chatInput.focus();

      // Load messages from REST API
      await this._fetchChannelMessages(channelId);

      // Update channel list active state
      this._renderChannelList();

      // SignalR Group management
      if (this._connection && this._connection.state === signalR.HubConnectionState.Connected) {
        if (oldChannel) {
          this._connection.invoke("LeaveChannel", oldChannel).catch(err => console.error(err));
        }
        this._connection.invoke("JoinChannel", channelId).catch(err => console.error(err));
      }
    },

    async _fetchChannelMessages(channelId) {
      try {
        const res = await $.ajax({
          url: FS.API_BASE + `/api/v1/chat/channels/${channelId}/messages`,
          type: 'GET',
          headers: this._getAuthHeaders()
        });
        if (res && res.success && Array.isArray(res.data)) {
          this._messagesMap[channelId] = res.data;
        } else {
          this._messagesMap[channelId] = [];
        }
      } catch (err) {
        console.error('Failed to fetch messages for channel:', err);
        this._messagesMap[channelId] = [];
      }
      this._renderMessages(channelId);
    },

    _renderMessages(channelId) {
      let msgs = this._messagesMap[channelId] || [];
      const $container = document.getElementById('chat-messages');
      if (!$container) return;

      if (this._searchQuery) {
        const q = this._searchQuery.toLowerCase();
        msgs = msgs.filter(m => (m.content || m.text) && (m.content || m.text).toLowerCase().includes(q) && !m.isRecalled);
      }

      this._renderPinned(this._messagesMap[channelId] || []);

      if (!msgs.length) {
        $container.innerHTML = this._searchQuery 
          ? '<div class="fs-empty"><p>Không tìm thấy tin nhắn nào khớp với từ khóa.</p></div>' 
          : '<div class="fs-empty" style="padding-top:48px"><i class="bi bi-chat-square-dots"></i><p>Chưa có tin nhắn nào. Hãy bắt đầu cuộc trò chuyện!</p></div>';
        return;
      }

      let html = '<div class="chat-msg-divider">Hôm nay</div>';
      let lastUserId = null;
      const session = FS.auth.getSession();

      msgs.forEach(msg => {
        const senderId = msg.senderId || msg.userId;
        const user = this._users.find(u => u.id === senderId) || {
          name: msg.senderName || 'User',
          avatar: msg.senderAvatar || 'U',
          color: msg.senderColor || 'av-indigo'
        };

        const showHeader = senderId !== lastUserId || !!msg.replyToMessageId || !!msg.replyTo;
        lastUserId = senderId;
        const isMe = senderId === session?.userId;
        const isRecalled = msg.isRecalled || msg.recalled;

        const rawText = msg.content || msg.text || '';
        let displayText = isRecalled ? '<em style="color:var(--fs-text-muted)">Tin nhắn đã được thu hồi</em>' : FS.str.escape(rawText);

        if (!isRecalled) {
          const mentionRegex = /@([a-zA-ZÀ-ỹ\s_]+)/g;
          displayText = displayText.replace(mentionRegex, (match) => `<span class="chat-mention-highlight">${match}</span>`);

          if (this._searchQuery) {
            const q = this._searchQuery;
            const searchRegex = new RegExp(`(${FS.str.escape(q).replace(/[.*+?^${}()|[\]\\]/g, '\\$&')})`, 'gi');
            displayText = displayText.replace(searchRegex, '<span class="chat-search-highlight">$1</span>');
          }
        }

        // Reply quote
        let quoteHtml = '';
        const replyId = msg.replyToMessageId || msg.replyTo;
        if (replyId) {
          const origMsg = (this._messagesMap[channelId] || []).find(m => m.id === replyId);
          if (origMsg) {
            const origSenderId = origMsg.senderId || origMsg.userId;
            const origUser = this._users.find(u => u.id === origSenderId);
            const origText = (origMsg.isRecalled || origMsg.recalled) ? 'Tin nhắn đã được thu hồi' : (origMsg.content || origMsg.text || '');
            quoteHtml = `<div class="chat-quoted-msg">
              <strong>${FS.str.escape(origUser?.name || origMsg.senderName || 'Unknown')}</strong>: ${FS.str.escape(origText)}
            </div>`;
          }
        }

        const isPinned = msg.isPinned || msg.pinned;
        let actionsHtml = '';
        if (!isRecalled) {
          actionsHtml = `<div class="chat-msg-actions">
            <button class="chat-msg-action-btn action-reply" data-id="${msg.id}" title="Trả lời"><i class="bi bi-reply-fill"></i></button>
            <button class="chat-msg-action-btn action-pin" data-id="${msg.id}" title="${isPinned ? 'Bỏ ghim' : 'Ghim'}"><i class="bi ${isPinned ? 'bi-pin-fill' : 'bi-pin'}"></i></button>
            ${isMe ? `<button class="chat-msg-action-btn action-recall" data-id="${msg.id}" title="Thu hồi"><i class="bi bi-trash-fill"></i></button>` : ''}
          </div>`;
        }

        html += `<div class="chat-msg" data-msg-id="${msg.id}" style="position:relative">
          ${actionsHtml}
          <div style="width:32px;flex-shrink:0;padding-top:4px">
            ${showHeader ? `<div class="fs-avatar fs-avatar-sm ${user?.color || 'av-indigo'}">${user?.avatar || '?'}</div>` : ''}
          </div>
          <div class="chat-msg-body">
            ${showHeader ? `<div class="chat-msg-header">
              <span class="chat-msg-name">${FS.str.escape(user?.name || 'Unknown')}</span>
              <span class="chat-msg-time">${FS.date.chatTime(msg.createdAt)}</span>
            </div>` : ''}
            ${quoteHtml}
            <div class="chat-msg-text">${displayText}</div>
          </div>
        </div>`;
      });

      $container.innerHTML = html;

      if (!this._searchQuery) {
        $container.scrollTop = $container.scrollHeight;
      }
    },

    _renderPinned(msgs) {
      const pinnedMsgs = msgs.filter(m => (m.isPinned || m.pinned) && !(m.isRecalled || m.recalled)).slice(0, 3);
      const $panel = document.getElementById('chat-pinned-panel');
      const $list = document.getElementById('chat-pinned-list');
      if (!$panel || !$list) return;

      if (!pinnedMsgs.length) {
        $panel.style.display = 'none';
        return;
      }

      $panel.style.display = 'block';
      $list.innerHTML = pinnedMsgs.map(m => {
        const senderId = m.senderId || m.userId;
        const user = this._users.find(u => u.id === senderId);
        return `<div class="chat-pinned-item">
          <div style="font-weight:600;width:120px;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;flex-shrink:0">${FS.str.escape(user?.name || m.senderName || 'Unknown')}</div>
          <div style="flex:1;white-space:nowrap;overflow:hidden;text-overflow:ellipsis;color:var(--fs-text-secondary)">${FS.str.escape(m.content || m.text || '')}</div>
        </div>`;
      }).join('');
    },

    _cancelReply() {
      this._replyingTo = null;
      const $panel = document.getElementById('chat-reply-panel');
      if ($panel) $panel.style.display = 'none';
    },

    async _sendMessage(text) {
      if (!text.trim() || !this._currentChannel) return;
      const session = FS.auth.getSession();

      const payload = {
        channelId: this._currentChannel,
        content: text.trim(),
        replyToMessageId: this._replyingTo || null
      };

      try {
        const res = await $.ajax({
          url: FS.API_BASE + '/api/v1/chat/messages',
          type: 'POST',
          headers: this._getAuthHeaders(),
          contentType: 'application/json',
          data: JSON.stringify(payload)
        });

        if (res && res.success && res.data) {
          const sentMsg = res.data;
          if (!this._messagesMap[this._currentChannel]) this._messagesMap[this._currentChannel] = [];
          this._messagesMap[this._currentChannel].push(sentMsg);

          this._cancelReply();
          this._hideMentionDropdown();
          this._renderMessages(this._currentChannel);

          const $input = document.getElementById('chat-input');
          if ($input) {
            $input.value = '';
            $input.style.height = 'auto';
          }

          // Broadcast via SignalR if connected
          if (this._connection && this._connection.state === signalR.HubConnectionState.Connected) {
            this._connection.invoke("SendMessage", this._currentChannel, session?.username || session?.email || 'User', JSON.stringify(sentMsg))
              .catch(err => console.error('Send SignalR message failed:', err));
          }
        } else {
          FS.toast(res?.message || 'Không thể gửi tin nhắn', 'error');
        }
      } catch (err) {
        console.error('Failed to send message via API:', err);
        FS.toast('Lỗi kết nối khi gửi tin nhắn', 'error');
      }
    },

    _showMentionDropdown(query) {
      const users = this._users.filter(u => u.name && u.name.toLowerCase().includes(query.toLowerCase()));
      const $dd = document.getElementById('chat-mention-dropdown');
      if (!users.length || !$dd) {
        if ($dd) $dd.style.display = 'none';
        return;
      }
      this._mentionUsers = users.slice(0, 5);
      this._mentionIndex = 0;
      this._renderMentionDropdown();
      $dd.style.display = 'block';
    },

    _renderMentionDropdown() {
      const $dd = document.getElementById('chat-mention-dropdown');
      if (!$dd) return;
      $dd.innerHTML = this._mentionUsers.map((u, idx) => `
        <div class="chat-mention-item ${idx === this._mentionIndex ? 'active' : ''}" data-name="${u.name}">
          <div class="fs-avatar fs-avatar-sm ${u.color || 'av-indigo'}" style="width:20px;height:20px;font-size:10px">${u.avatar || '?'}</div>
          <div>${FS.str.escape(u.name)}</div>
        </div>
      `).join('');
    },

    _hideMentionDropdown() {
      this._mentionUsers = [];
      const $dd = document.getElementById('chat-mention-dropdown');
      if ($dd) $dd.style.display = 'none';
    },

    _insertMention(name) {
      const $input = document.getElementById('chat-input');
      if (!$input) return;
      const val = $input.value;
      const cursor = $input.selectionStart;
      const before = val.substring(0, cursor);
      const after = val.substring(cursor);
      const lastAt = before.lastIndexOf('@');

      if (lastAt >= 0) {
        const newVal = before.substring(0, lastAt) + '@' + name + ' ' + after;
        $input.value = newVal;
        $input.focus();
        $input.style.height = 'auto';
        $input.style.height = Math.min($input.scrollHeight, 120) + 'px';
        const newPos = lastAt + name.length + 2;
        $input.setSelectionRange(newPos, newPos);
      }
      this._hideMentionDropdown();
    },

    _bindEvents() {
      const self = this;

      // Channel item click
      $(document).off('click.channelItem').on('click.channelItem', '.chat-channel-item', function () {
        self._openChannel(this.dataset.channelId);
      });

      const $input = document.getElementById('chat-input');
      if ($input) {
        $input.addEventListener('keydown', function (e) {
          if (self._mentionUsers.length > 0) {
            if (e.key === 'ArrowDown') { e.preventDefault(); self._mentionIndex = (self._mentionIndex + 1) % self._mentionUsers.length; self._renderMentionDropdown(); return; }
            if (e.key === 'ArrowUp') { e.preventDefault(); self._mentionIndex = (self._mentionIndex - 1 + self._mentionUsers.length) % self._mentionUsers.length; self._renderMentionDropdown(); return; }
            if (e.key === 'Enter') { e.preventDefault(); self._insertMention(self._mentionUsers[self._mentionIndex].name); return; }
            if (e.key === 'Escape') { e.preventDefault(); self._hideMentionDropdown(); return; }
          }

          if (e.key === 'Enter' && !e.shiftKey) {
            e.preventDefault();
            self._sendMessage(this.value);
          }
        });

        $input.addEventListener('input', function () {
          this.style.height = 'auto';
          this.style.height = Math.min(this.scrollHeight, 120) + 'px';

          const val = this.value;
          const cursor = this.selectionStart;
          const before = val.substring(0, cursor);
          const match = before.match(/@([a-zA-ZÀ-ỹ\s_]*)$/);

          if (match) {
            self._showMentionDropdown(match[1]);
          } else {
            self._hideMentionDropdown();
          }
        });
      }

      $(document).off('click.mentionItem').on('click.mentionItem', '.chat-mention-item', function () {
        self._insertMention($(this).data('name'));
      });

      $('#chat-send-btn').off('click').on('click', function () {
        self._sendMessage($('#chat-input').val());
      });

      // Actions: Reply
      $(document).off('click.chatReply').on('click.chatReply', '.action-reply', function () {
        const msgId = $(this).data('id');
        const msgs = self._messagesMap[self._currentChannel] || [];
        const msg = msgs.find(m => m.id === msgId);
        if (msg) {
          self._replyingTo = msgId;
          const senderId = msg.senderId || msg.userId;
          const user = self._users.find(u => u.id === senderId);
          $('#chat-reply-name').text('Đang trả lời ' + (user?.name || msg.senderName || 'Unknown'));
          $('#chat-reply-text').text(msg.content || msg.text || '');
          $('#chat-reply-panel').css('display', 'flex');
          $('#chat-input').focus();
        }
      });

      $('#chat-reply-cancel').off('click').on('click', () => self._cancelReply());

      // Actions: Recall
      $(document).off('click.chatRecall').on('click.chatRecall', '.action-recall', function () {
        if (!confirm('Bạn có chắc muốn thu hồi tin nhắn này?')) return;
        const msgId = $(this).data('id');

        $.ajax({
          url: FS.API_BASE + `/api/v1/chat/messages/${msgId}`,
          type: 'DELETE',
          headers: self._getAuthHeaders()
        }).done(function (res) {
          if (res && res.success) {
            const msgs = self._messagesMap[self._currentChannel] || [];
            const msg = msgs.find(m => m.id === msgId);
            if (msg) {
              msg.isRecalled = true;
              msg.content = 'Tin nhắn này đã bị thu hồi.';
              self._renderMessages(self._currentChannel);

              if (self._connection && self._connection.state === signalR.HubConnectionState.Connected) {
                self._connection.invoke("RecallMessage", self._currentChannel, msgId)
                  .catch(err => console.error(err));
              }
            }
          } else {
            FS.toast(res?.message || 'Không thể thu hồi tin nhắn', 'error');
          }
        }).fail(function () {
          FS.toast('Lỗi khi thu hồi tin nhắn', 'error');
        });
      });

      // Actions: Pin
      $(document).off('click.chatPin').on('click.chatPin', '.action-pin', function () {
        const msgId = $(this).data('id');
        const msgs = self._messagesMap[self._currentChannel] || [];
        const msg = msgs.find(m => m.id === msgId);
        if (msg) {
          msg.isPinned = !msg.isPinned;
          self._renderMessages(self._currentChannel);

          if (self._connection && self._connection.state === signalR.HubConnectionState.Connected) {
            self._connection.invoke("PinMessage", self._currentChannel, msgId, msg.isPinned)
              .catch(err => console.error(err));
          }
        }
      });

      // Search input
      const $search = document.getElementById('chat-search-input');
      if ($search) {
        $search.addEventListener('input', function () {
          self._searchQuery = this.value;
          self._renderMessages(self._currentChannel);
        });
      }
    }
  };

})(window.FS = window.FS || {}, jQuery);