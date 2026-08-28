window.HochuApi = {
  async request(url, options = {}) {
    const { body, headers, ...rest } = options;
    const isForm = body instanceof FormData;
    const opts = {
      credentials: "same-origin",
      headers: {
        Accept: "application/json",
        ...(body && !isForm ? { "Content-Type": "application/json" } : {}),
        ...(headers || {})
      },
      ...rest,
      body: body && !isForm && typeof body !== "string" ? JSON.stringify(body) : body
    };

    const res = await fetch(url, opts);
    const text = await res.text();
    let data = null;
    try {
      data = text ? JSON.parse(text) : null;
    } catch {
      data = { detail: text };
    }

    if (!res.ok) {
      const message = data?.detail || data?.title || res.statusText || "Ошибка запроса";
      const err = new Error(message);
      err.status = res.status;
      err.data = data;
      throw err;
    }
    return data;
  },

  get(url) {
    return this.request(url);
  },

  post(url, body) {
    return this.request(url, { method: "POST", body });
  },

  put(url, body) {
    return this.request(url, { method: "PUT", body });
  },

  me() {
    return this.get("/api/auth/me");
  },

  login(email, password) {
    return this.post("/api/auth/login", { email, password });
  },

  register(email, password, displayName, acceptTerms = true) {
    return this.post("/api/auth/register", { email, password, displayName, acceptTerms });
  },

  logout() {
    return this.post("/api/auth/logout");
  }
};
