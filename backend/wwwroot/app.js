const loginForm = document.getElementById("login-form");
const loginStatus = document.getElementById("login-status");
const telegramLoginBtn = document.getElementById("telegram-login-btn");
const telegramLoginStatus = document.getElementById("telegram-login-status");
const loadBtn = document.getElementById("load-btn");
const profileStatus = document.getElementById("profile-status");
const profileCard = document.getElementById("profile-card");

let telegramPollTimer = null;

function setStatus(element, message, tone = "text-muted") {
  element.textContent = message;
  element.className = `status mb-0 small ${tone}`;
}

telegramLoginBtn.addEventListener("click", async () => {
  if (telegramPollTimer) {
    clearInterval(telegramPollTimer);
    telegramPollTimer = null;
  }

  setStatus(telegramLoginStatus, "Готуємо вхід через Telegram...", "text-muted");

  try {
    const res = await fetch("/auth/telegram/start", { method: "POST" });

    if (!res.ok) {
      setStatus(
        telegramLoginStatus,
        "Telegram-бот не налаштований на сервері",
        "text-danger"
      );
      return;
    }

    const data = await res.json();
    window.open(data.bot_url, "_blank", "noopener,noreferrer");

    setStatus(
      telegramLoginStatus,
      "Відкрийте Telegram, натисніть Start у боті — очікуємо підтвердження...",
      "text-muted"
    );

    telegramPollTimer = setInterval(async () => {
      try {
        const statusRes = await fetch(
          `/auth/telegram/status?token=${encodeURIComponent(data.token)}`
        );
        const statusData = await statusRes.json();

        if (statusData.status === "pending") {
          return;
        }

        clearInterval(telegramPollTimer);
        telegramPollTimer = null;

        if (statusData.status === "completed") {
          localStorage.setItem("access_token", statusData.access_token);
          setStatus(
            telegramLoginStatus,
            "Вхід через Telegram виконано успішно",
            "text-success"
          );
          return;
        }

        setStatus(
          telegramLoginStatus,
          "Час очікування минув. Спробуйте увійти через Telegram ще раз.",
          "text-danger"
        );
      } catch (err) {
        clearInterval(telegramPollTimer);
        telegramPollTimer = null;
        setStatus(
          telegramLoginStatus,
          "Не вдалося перевірити статус входу",
          "text-danger"
        );
      }
    }, 2000);
  } catch (err) {
    setStatus(telegramLoginStatus, "Не вдалося з'єднатися з сервером", "text-danger");
  }
});

loginForm.addEventListener("submit", async (e) => {
  e.preventDefault();

  const email = document.getElementById("email").value;
  const password = document.getElementById("password").value;

  try {
    const res = await fetch("/login", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ email, password }),
    });

    if (!res.ok) {
      setStatus(loginStatus, "Помилка входу", "text-danger");
      return;
    }

    const data = await res.json();
    localStorage.setItem("access_token", data.access_token);
    setStatus(loginStatus, "Вхід виконано успішно", "text-success");
  } catch (err) {
    setStatus(loginStatus, "Не вдалося з'єднатися з сервером", "text-danger");
  }
});

loadBtn.addEventListener("click", async () => {
  const token = localStorage.getItem("access_token");

  if (!token) {
    setStatus(profileStatus, "Спочатку виконайте вхід", "text-danger");
    profileCard.classList.add("hidden");
    profileCard.innerHTML = "";
    return;
  }

  setStatus(profileStatus, "Завантаження...", "text-muted");

  try {
    const res = await fetch("/auth/me", {
      headers: { Authorization: "Bearer " + token },
    });

    if (!res.ok) {
      setStatus(profileStatus, "Не вдалося отримати дані", "text-danger");
      profileCard.classList.add("hidden");
      profileCard.innerHTML = "";
      return;
    }

    const user = await res.json();
    renderProfile(user);
    setStatus(profileStatus, "", "text-muted");
  } catch (err) {
    setStatus(profileStatus, "Помилка з'єднання з сервером", "text-danger");
    profileCard.classList.add("hidden");
    profileCard.innerHTML = "";
  }
});

function renderProfile(user) {
  const hasTelegram = user.telegramId != null;
  const username = user.telegramUsername ? `@${user.telegramUsername}` : "—";
  const photos = Array.isArray(user.telegramPictures) ? user.telegramPictures : [];

  const photosHtml = photos.length
    ? `<div class="d-flex flex-wrap gap-2 mt-3">${photos
        .map(
          (url) =>
            `<img src="${escapeHtml(url)}" alt="Фото профілю Telegram" class="profile-photo" />`
        )
        .join("")}</div>`
    : `<div class="alert alert-light border mt-3 mb-0">Фото відсутні</div>`;

  profileCard.innerHTML = `
    <div class="border rounded-4 p-3 bg-light-subtle">
      <div class="row g-3">
        <div class="col-12 col-md-6">
          <div class="border rounded-3 p-3 h-100 bg-white">
            <div class="text-uppercase small text-muted">ID</div>
            <div class="fw-semibold">${escapeHtml(String(user.id))}</div>
          </div>
        </div>
        <div class="col-12 col-md-6">
          <div class="border rounded-3 p-3 h-100 bg-white">
            <div class="text-uppercase small text-muted">Ім'я</div>
            <div class="fw-semibold">${escapeHtml(user.name || "—")}</div>
          </div>
        </div>
        <div class="col-12 col-md-6">
          <div class="border rounded-3 p-3 h-100 bg-white">
            <div class="text-uppercase small text-muted">Email</div>
            <div class="fw-semibold">${escapeHtml(user.email || "—")}</div>
          </div>
        </div>
        <div class="col-12 col-md-6">
          <div class="border rounded-3 p-3 h-100 bg-white">
            <div class="text-uppercase small text-muted">Роль</div>
            <div class="fw-semibold">${escapeHtml(user.role || "—")}</div>
          </div>
        </div>
        <div class="col-12">
          <div class="border rounded-3 p-3 bg-white">
            <div class="text-uppercase small text-muted">Bio</div>
            <div class="fw-semibold">${escapeHtml(user.bio || user.telegramBio || "—")}</div>
          </div>
        </div>
      </div>

      ${
        hasTelegram
          ? `
        <div class="mt-4">
          <h3 class="h6 text-uppercase text-muted mb-3">Telegram</h3>
          <div class="row g-3">
            <div class="col-12 col-md-6">
              <div class="border rounded-3 p-3 bg-white">
                <div class="text-uppercase small text-muted">Telegram ID</div>
                <div class="fw-semibold">${escapeHtml(String(user.telegramId))}</div>
              </div>
            </div>
            <div class="col-12 col-md-6">
              <div class="border rounded-3 p-3 bg-white">
                <div class="text-uppercase small text-muted">Ім'я в Telegram</div>
                <div class="fw-semibold">${escapeHtml(user.telegramName || "—")}</div>
              </div>
            </div>
            <div class="col-12">
              <div class="border rounded-3 p-3 bg-white">
                <div class="text-uppercase small text-muted">Username</div>
                <div class="fw-semibold">${escapeHtml(username)}</div>
              </div>
            </div>
            <div class="col-12">
              <div class="border rounded-3 p-3 bg-white">
                <div class="text-uppercase small text-muted">Біо</div>
                <div class="fw-semibold">${escapeHtml(user.telegramBio || "—")}</div>
              </div>
            </div>
          </div>
          ${photosHtml}
        </div>
      `
          : `<div class="alert alert-warning mt-3 mb-0">Telegram-профіль не прив'язано</div>`
      }
    </div>
  `;

  profileCard.classList.remove("hidden");
}

function escapeHtml(value) {
  return String(value)
    .replaceAll("&", "&amp;")
    .replaceAll("<", "&lt;")
    .replaceAll(">", "&gt;")
    .replaceAll('"', "&quot;")
    .replaceAll("'", "&#39;");
}
