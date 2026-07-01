const loginForm = document.getElementById("login-form");
const loginStatus = document.getElementById("login-status");
const telegramLoginBtn = document.getElementById("telegram-login-btn");
const telegramLoginStatus = document.getElementById("telegram-login-status");
const loadBtn = document.getElementById("load-btn");
const profileStatus = document.getElementById("profile-status");
const profileCard = document.getElementById("profile-card");

let telegramPollTimer = null;

telegramLoginBtn.addEventListener("click", async () => {
  if (telegramPollTimer) {
    clearInterval(telegramPollTimer);
    telegramPollTimer = null;
  }

  telegramLoginStatus.textContent = "Готуємо вхід через Telegram...";
  telegramLoginStatus.style.color = "#555";

  try {
    const res = await fetch("/auth/telegram/start", { method: "POST" });

    if (!res.ok) {
      telegramLoginStatus.textContent = "Telegram-бот не налаштований на сервері";
      telegramLoginStatus.style.color = "red";
      return;
    }

    const data = await res.json();
    window.open(data.bot_url, "_blank", "noopener,noreferrer");

    telegramLoginStatus.textContent =
      "Відкрийте Telegram, натисніть Start у боті — очікуємо підтвердження...";
    telegramLoginStatus.style.color = "#555";

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
          telegramLoginStatus.textContent = "Вхід через Telegram виконано успішно";
          telegramLoginStatus.style.color = "green";
          return;
        }

        telegramLoginStatus.textContent =
          "Час очікування минув. Спробуйте увійти через Telegram ще раз.";
        telegramLoginStatus.style.color = "red";
      } catch (err) {
        clearInterval(telegramPollTimer);
        telegramPollTimer = null;
        telegramLoginStatus.textContent = "Не вдалося перевірити статус входу";
        telegramLoginStatus.style.color = "red";
      }
    }, 2000);
  } catch (err) {
    telegramLoginStatus.textContent = "Не вдалося з'єднатися з сервером";
    telegramLoginStatus.style.color = "red";
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
      loginStatus.textContent = "Помилка входу";
      loginStatus.style.color = "red";
      return;
    }

    const data = await res.json();
    localStorage.setItem("access_token", data.access_token);
    loginStatus.textContent = "Вхід виконано успішно";
    loginStatus.style.color = "green";
  } catch (err) {
    loginStatus.textContent = "Не вдалося з'єднатися з сервером";
    loginStatus.style.color = "red";
  }
});

loadBtn.addEventListener("click", async () => {
  const token = localStorage.getItem("access_token");

  if (!token) {
    profileStatus.textContent = "Спочатку виконайте вхід";
    profileStatus.style.color = "red";
    profileCard.classList.add("hidden");
    profileCard.innerHTML = "";
    return;
  }

  profileStatus.textContent = "Завантаження...";
  profileStatus.style.color = "#555";

  try {
    const res = await fetch("/auth/me", {
      headers: { Authorization: "Bearer " + token },
    });

    if (!res.ok) {
      profileStatus.textContent = "Не вдалося отримати дані";
      profileStatus.style.color = "red";
      profileCard.classList.add("hidden");
      profileCard.innerHTML = "";
      return;
    }

    const user = await res.json();
    renderProfile(user);
    profileStatus.textContent = "";
  } catch (err) {
    profileStatus.textContent = "Помилка з'єднання з сервером";
    profileStatus.style.color = "red";
    profileCard.classList.add("hidden");
    profileCard.innerHTML = "";
  }
});

function renderProfile(user) {
  const hasTelegram = user.telegramId != null;
  const username = user.telegramUsername
    ? `@${user.telegramUsername}`
    : "—";
  const photos = Array.isArray(user.telegramPictures) ? user.telegramPictures : [];

  const photosHtml = photos.length
    ? `<div class="profile-photos">${photos
        .map(
          (url) =>
            `<img src="${escapeHtml(url)}" alt="Фото профілю Telegram" class="profile-photo" />`
        )
        .join("")}</div>`
    : `<p class="profile-empty">Фото відсутні</p>`;

  profileCard.innerHTML = `
    <dl class="profile-fields">
      <div class="profile-field">
        <dt>ID</dt>
        <dd>${escapeHtml(String(user.id))}</dd>
      </div>
      <div class="profile-field">
        <dt>Ім'я</dt>
        <dd>${escapeHtml(user.name || "—")}</dd>
      </div>
      <div class="profile-field">
        <dt>Email</dt>
        <dd>${escapeHtml(user.email || "—")}</dd>
      </div>
      <div class="profile-field">
        <dt>Роль</dt>
        <dd>${escapeHtml(user.role || "—")}</dd>
      </div>
      <div class="profile-field profile-field-wide">
        <dt>Bio</dt>
        <dd class="profile-bio">${escapeHtml(user.bio || user.telegramBio || "—")}</dd>
      </div>
    </dl>

    ${
      hasTelegram
        ? `
      <div class="profile-section">
        <h3>Telegram</h3>
        <dl class="profile-fields">
          <div class="profile-field">
            <dt>Telegram ID</dt>
            <dd>${escapeHtml(String(user.telegramId))}</dd>
          </div>
          <div class="profile-field">
            <dt>Ім'я в Telegram</dt>
            <dd>${escapeHtml(user.telegramName || "—")}</dd>
          </div>
          <div class="profile-field">
            <dt>Username</dt>
            <dd>${escapeHtml(username)}</dd>
          </div>
          <div class="profile-field profile-field-wide">
            <dt>Біо</dt>
            <dd>${escapeHtml(user.telegramBio || "—")}</dd>
          </div>
        </dl>
        ${photosHtml}
      </div>
    `
        : `<p class="profile-empty">Telegram-профіль не прив'язано</p>`
    }
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
