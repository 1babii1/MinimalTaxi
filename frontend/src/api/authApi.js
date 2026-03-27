// Базовый helper для запросов к API авторизации.
async function request(path, { method = 'GET', body, token, baseUrl }) {
  const headers = {
    'Content-Type': 'application/json',
  }

  if (token) {
    headers.Authorization = `Bearer ${token}`
  }

  const response = await fetch(`${baseUrl}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  })

  const text = await response.text()
  let data = text

  try {
    data = text ? JSON.parse(text) : null
  } catch {
    data = text
  }

  if (!response.ok) {
    const message = typeof data === 'string' ? data : JSON.stringify(data)
    throw new Error(message || `Request failed with status ${response.status}`)
  }

  return data
}

export const authApi = {
  register: (payload, baseUrl) => request('/register', { method: 'POST', body: payload, baseUrl }),
  confirmEmail: ({ userId, token }, baseUrl) =>
    request(`/confirm-email?userId=${encodeURIComponent(userId)}&token=${encodeURIComponent(token)}`, {
      method: 'GET',
      baseUrl,
    }),
  login: (payload, baseUrl) => request('/login', { method: 'POST', body: payload, baseUrl }),
  refresh: (payload, baseUrl) => request('/refresh', { method: 'POST', body: payload, baseUrl }),
  logout: (payload, baseUrl) => request('/logout', { method: 'POST', body: payload, baseUrl }),
  forgotPassword: (payload, baseUrl) =>
    request('/forgot-password', { method: 'POST', body: payload, baseUrl }),
  resetPassword: (payload, baseUrl) => request('/reset-password', { method: 'POST', body: payload, baseUrl }),
  requestChangePassword: (payload, token, baseUrl) =>
    request('/change-password/request', { method: 'POST', body: payload, token, baseUrl }),
  confirmChangePassword: (payload, token, baseUrl) =>
    request('/change-password/confirm', { method: 'POST', body: payload, token, baseUrl }),
}
