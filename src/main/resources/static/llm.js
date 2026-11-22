'use strict';
const form = document.getElementById('chat-form');
const input = document.getElementById('prompt');
const button = document.getElementById('submit-button');
const reset = document.getElementById('reset-button');
const status = document.getElementById('status');
const error = document.getElementById('error');
const log = document.getElementById('messages');
const model = document.getElementById('model');
const empty = document.getElementById('empty-state');
let history = [];
function showMessage(role, content) {
  const message = document.createElement('article');
  message.className = 'message ' + role;
  const label = document.createElement('strong');
  label.textContent = role === 'user' ? 'Bạn' : 'LLM';
  const text = document.createElement('div');
  text.textContent = content;
  message.append(label, text);
  log.append(message);
  return message;
}
reset.addEventListener('click', () => {
  history = [];
  log.replaceChildren();
  empty.hidden = false;
  model.hidden = true;
  error.hidden = true;
  input.value = '';
  status.textContent = 'Đã bắt đầu cuộc trò chuyện mới.';
  input.focus();
});
form.addEventListener('submit', async (event) => {
  event.preventDefault();
  if (button.disabled) return;
  const content = input.value.trim();
  if (!content) { input.focus(); return; }
  const messages = [...history, { role: 'user', content }];
  error.hidden = true;
  if (messages.length > 40 || messages.reduce((sum, m) => sum + m.content.length, 0) > 32000) {
    error.textContent = 'Cuộc trò chuyện đã đạt giới hạn. Hãy bắt đầu cuộc trò chuyện mới hoặc rút ngắn tin nhắn.';
    error.hidden = false;
    return;
  }
  const pending = showMessage('user', content);
  empty.hidden = true;
  button.disabled = reset.disabled = input.disabled = true;
  button.textContent = 'Đang trả lời…';
  status.textContent = 'Ollama đang xử lý. Lần đầu tải model có thể mất vài phút.';
  try {
    const response = await fetch('/api/chat', {
      method: 'POST',
      headers: { Accept: 'application/json', 'Content-Type': 'application/json' },
      body: JSON.stringify({ messages })
    });
    if (!response.ok) throw new Error(response.status === 503
      ? 'LLM chưa sẵn sàng. Kiểm tra Ollama đang chạy và model đã được tải, rồi gửi lại.'
      : 'Không thể gửi tin nhắn (HTTP ' + response.status + '). Kiểm tra giới hạn cuộc trò chuyện và thử lại.');
    const data = await response.json();
    if (typeof data.reply !== 'string' || !data.reply.trim()) throw new Error('Model trả về kết quả không hợp lệ. Hãy thử lại.');
    showMessage('assistant', data.reply);
    history = [...messages, { role: 'assistant', content: data.reply }];
    model.textContent = typeof data.model === 'string' ? data.model : 'Ollama';
    model.hidden = false;
    input.value = '';
    status.textContent = 'Đã trả lời. Bạn có thể hỏi tiếp hoặc bắt đầu cuộc trò chuyện mới.';
  } catch (failure) {
    pending.remove();
    empty.hidden = history.length > 0;
    error.textContent = failure instanceof TypeError
      ? 'Không kết nối được máy chủ. Kiểm tra ứng dụng đang chạy và gửi lại.' : failure.message;
    error.hidden = false;
    status.textContent = 'Chưa nhận được câu trả lời. Tin nhắn được giữ lại để gửi lại.';
  } finally {
    button.disabled = reset.disabled = input.disabled = false;
    button.textContent = 'Gửi tin nhắn';
    input.focus();
  }
});
