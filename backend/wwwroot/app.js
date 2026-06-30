const loginForm = document.getElementById("login-form");
const loginStatus = document.getElementById("login-status");
const telegramLoginBtn = document.getElementById("telegram-login-btn");
const telegramLoginStatus = document.getElementById("telegram-login-status");
const loadBtn = document.getElementById("load-btn");
const dataList = document.getElementById("data-list");

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
    alert("Спочатку виконайте вхід");
    return;
  }

  try {
    const res = await fetch("/products", {
      headers: { Authorization: "Bearer " + token },
    });

    if (!res.ok) {
      alert("Не вдалося отримати дані");
      return;
    }

    const items = await res.json();
    dataList.innerHTML = "";

    items.forEach((item) => {
      const li = document.createElement("li");
      li.textContent = item.name;
      dataList.appendChild(li);
    });
  } catch (err) {
    alert("Помилка з'єднання з сервером");
  }
});
