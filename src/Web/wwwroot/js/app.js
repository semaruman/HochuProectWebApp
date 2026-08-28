window.HochuApp = (() => {
  const PROJECT_STATUS = {
    0: "Черновик",
    1: "Опубликован",
    2: "В работе",
    3: "Завершён",
    4: "Отменён",
    5: "Скрыт",
    Draft: "Черновик",
    Published: "Опубликован",
    InProgress: "В работе",
    Completed: "Завершён",
    Cancelled: "Отменён",
    Hidden: "Скрыт"
  };

  const DEAL_STATUS = {
    0: "Создана",
    1: "В работе",
    2: "Работа сдана",
    3: "Завершена",
    4: "Отменена",
    5: "Нужна доработка",
    Created: "Создана",
    InProgress: "В работе",
    Submitted: "Работа сдана",
    Completed: "Завершена",
    Cancelled: "Отменена",
    RevisionRequired: "Нужна доработка"
  };

  const BID_STATUS = {
    0: "Ожидает",
    1: "Принят",
    2: "Отклонён",
    3: "Отозван",
    Pending: "Ожидает",
    Accepted: "Принят",
    Rejected: "Отклонён",
    Withdrawn: "Отозван"
  };

  let currentUserId = null;

  function money(amount, currency = "RUB") {
    const n = Number(amount || 0);
    return `${n.toLocaleString("ru-RU")} ${currency}`;
  }

  function formatDate(value) {
    if (!value) return "—";
    return new Date(value).toLocaleString("ru-RU", {
      day: "2-digit",
      month: "short",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit"
    });
  }

  function qs(name) {
    return new URLSearchParams(location.search).get(name);
  }

  function toast(message, isError = false) {
    document.querySelectorAll(".toast").forEach((el) => el.remove());
    const el = document.createElement("div");
    el.className = `toast${isError ? " toast--error" : ""}`;
    el.textContent = message;
    document.body.appendChild(el);
    setTimeout(() => el.remove(), 3200);
  }

  function statusBadge(map, value, accent = false) {
    const label = map[value] ?? String(value ?? "—");
    const cls = accent ? "badge badge--accent" : "badge";
    return `<span class="${cls}">${escapeHtml(label)}</span>`;
  }

  function projectCard(p) {
    const desc = escapeHtml((p.description || "").slice(0, 140));
    return `
      <a class="item glass-card project-card" href="/project.html?id=${p.id}">
        <div class="row" style="justify-content:space-between;align-items:flex-start">
          <div class="item__title">${escapeHtml(p.title)}</div>
          ${statusBadge(PROJECT_STATUS, p.status, true)}
        </div>
        ${desc ? `<p class="item__desc">${desc}${(p.description || "").length > 140 ? "…" : ""}</p>` : ""}
        <div class="item__meta">
          <span class="item__budget">${money(p.budgetAmount, p.currency)}</span>
          <span>до ${escapeHtml(p.deadline)}</span>
          <span>${escapeHtml(p.category?.name || "")}</span>
        </div>
        <div class="item__footer">
          <span class="muted" style="font-size:var(--text-xs);font-family:var(--font-mono)">ID ${String(p.id).slice(0, 8)}…</span>
          <span class="btn btn-ghost" style="padding:0.4rem 0.9rem;font-size:var(--text-xs)">Открыть →</span>
        </div>
      </a>`;
  }

  function serviceCard(s) {
    return `
      <div class="item glass-card engineer-card">
        <div class="avatar">${escapeHtml((s.title || "У").charAt(0).toUpperCase())}</div>
        <div class="item__title">${escapeHtml(s.title)}</div>
        <p class="item__desc">${escapeHtml((s.description || "").slice(0, 100))}</p>
        <div class="item__meta" style="justify-content:center;margin-top:var(--space-3)">
          <span class="badge badge--accent">${escapeHtml(s.category?.name || "Услуга")}</span>
          <span class="item__budget">${money(s.price)}</span>
          <span>${s.deliveryDays} дн.</span>
        </div>
        <div style="margin-top:var(--space-4)">
          <span class="availability">Доступна</span>
        </div>
      </div>`;
  }

  function escapeHtml(str) {
    return String(str ?? "")
      .replaceAll("&", "&amp;")
      .replaceAll("<", "&lt;")
      .replaceAll(">", "&gt;")
      .replaceAll('"', "&quot;");
  }

  async function refreshAuth() {
    try {
      const me = await HochuApi.me();
      currentUserId = me.userId;
      return true;
    } catch {
      currentUserId = null;
      return false;
    }
  }

  function getUserId() {
    return currentUserId;
  }

  function requireAuthOrRedirect() {
    if (!currentUserId) {
      location.href = `/login.html?returnUrl=${encodeURIComponent(location.pathname + location.search)}`;
      return false;
    }
    return true;
  }

  async function mountShell({ active } = {}) {
    const header = document.getElementById("site-header");
    const footer = document.getElementById("site-footer");
    if (!header || !footer) return;

    const authed = await refreshAuth();
    const authLinks = authed
      ? `
        <a href="/deals.html" data-nav="deals">Сделки</a>
        <a href="/notifications.html" data-nav="notifications">Уведомления</a>
        <a href="/profile.html" data-nav="profile">Профиль</a>
        <button type="button" class="linkish" id="logout-btn">Выйти</button>`
      : `
        <a href="/login.html" data-nav="login">Войти</a>
        <a class="btn btn-primary" href="/register.html" style="padding:0.45rem 1rem">Регистрация</a>`;

    header.innerHTML = `
      <div class="site-header__inner">
        <a class="brand" href="/">
          <span class="brand__mark" aria-hidden="true"></span>
          <span>Хочу Проект</span>
        </a>
        <nav class="nav" aria-label="Основная навигация">
          <a href="/projects.html" data-nav="projects">Проекты</a>
          <a href="/services.html" data-nav="services">Услуги</a>
          ${authLinks}
        </nav>
      </div>`;

    footer.innerHTML = `
      <div class="wrap">
        Инженерная биржа компетенций ·
        <a href="/terms.html">Соглашение</a> ·
        <a href="/privacy.html">Конфиденциальность</a>
      </div>`;

    if (active) {
      header.querySelectorAll(`[data-nav="${active}"]`).forEach((el) => {
        el.classList.add("is-active");
      });
    }

    const logoutBtn = document.getElementById("logout-btn");
    if (logoutBtn) {
      logoutBtn.addEventListener("click", async () => {
        try {
          await HochuApi.logout();
          location.href = "/";
        } catch (err) {
          toast(err.message, true);
        }
      });
    }

    if (window.HochuEffects) HochuEffects.init();
  }

  function setBusy(btn, busy, label) {
    if (!btn) return;
    btn.disabled = !!busy;
    if (label) btn.dataset.label = btn.dataset.label || btn.textContent;
    btn.textContent = busy ? "…" : (btn.dataset.label || label || btn.textContent);
  }

  function dealNextAction(deal, userId) {
    const s = deal.status;
    const isBuyer = userId === deal.buyerId;
    const isSeller = userId === deal.sellerId;
    if (s === 1 || s === "InProgress") {
      if (isSeller) return "Загрузите результат и нажмите «Сдать работу».";
      return "Ожидайте сдачу работы от исполнителя.";
    }
    if (s === 5 || s === "RevisionRequired") {
      if (isSeller) return `Нужна доработка: ${deal.lastRevisionComment || "см. комментарий заказчика"}`;
      return "Ожидайте повторную сдачу от исполнителя.";
    }
    if (s === 2 || s === "Submitted") {
      const since = deal.submittedAt ? `Сдана ${formatDate(deal.submittedAt)}.` : "";
      if (isBuyer) return `${since} Проверьте файлы и примите работу или верните на доработку.`;
      return `${since} Ожидайте проверки заказчиком.`;
    }
    if (s === 3 || s === "Completed") return "Сделка завершена. Можно оставить отзыв.";
    return "";
  }

  function daysSince(dateStr) {
    if (!dateStr) return 0;
    return Math.floor((Date.now() - new Date(dateStr).getTime()) / 86400000);
  }

  return {
    PROJECT_STATUS,
    DEAL_STATUS,
    BID_STATUS,
    money,
    formatDate,
    qs,
    toast,
    statusBadge,
    escapeHtml,
    mountShell,
    refreshAuth,
    getUserId,
    requireAuthOrRedirect,
    setBusy,
    dealNextAction,
    daysSince,
    projectCard,
    serviceCard
  };
})();
