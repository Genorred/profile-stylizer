const { createApp } = Vue;

const TOKEN_KEY = "access_token";

/**
 * Axios interceptor (обязательное требование темы 11)
 */
axios.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) {
    config.headers.Authorization = "Bearer " + token;
  }
  return config;
});

createApp({
  data() {
    return {
      email: "",
      password: "",

      loginStatus: "",
      telegramStatus: "Telegram не підключено",

      profile: null,
      loadingProfile: false,

      telegramPollTimer: null,
    };
  },

  mounted() {
    if (localStorage.getItem(TOKEN_KEY)) {
      this.loadProfile();
    }
  },

  methods: {
    /**
     * LOGIN (email/password)
     */
    async login() {
      if (!this.email || !this.password) {
        this.loginStatus = "Заповніть всі поля";
        return;
      }

      try {
        const res = await axios.post("/login", {
          email: this.email,
          password: this.password,
        });

        localStorage.setItem(TOKEN_KEY, res.data.access_token);

        this.loginStatus = "Вхід виконано успішно";

        await this.loadProfile();
      } catch (err) {
        this.loginStatus = "Помилка входу";
      }
    },

    /**
     * TELEGRAM LOGIN (polling)
     */
    async telegramLogin() {
      if (this.telegramPollTimer) {
        clearInterval(this.telegramPollTimer);
      }

      this.telegramStatus = "Запуск Telegram логіну...";

      try {
        const res = await axios.post("/auth/telegram/start");

        const data = res.data;
        window.open(data.bot_url, "_blank");

        this.telegramStatus =
          "Очікуємо підтвердження в Telegram...";

        this.telegramPollTimer = setInterval(async () => {
          try {
            const statusRes = await axios.get(
              `/auth/telegram/status?token=${encodeURIComponent(data.token)}`
            );

            const status = statusRes.data;

            if (status.status === "pending") return;

            clearInterval(this.telegramPollTimer);
            this.telegramPollTimer = null;

            if (status.status === "completed") {
              localStorage.setItem(TOKEN_KEY, status.access_token);
              this.telegramStatus = "Успішний Telegram login";

              await this.loadProfile();
            } else {
              this.telegramStatus = "Таймаут входу";
            }
          } catch (e) {
            clearInterval(this.telegramPollTimer);
            this.telegramPollTimer = null;
            this.telegramStatus = "Помилка перевірки статусу";
          }
        }, 2000);
      } catch (err) {
        this.telegramStatus = "Помилка Telegram login";
      }
    },

    /**
     * LOAD PROFILE (READ API)
     */
    async loadProfile() {
      this.loadingProfile = true;

      try {
        const res = await axios.get("/auth/me");
        this.profile = res.data;
      } catch (err) {
        this.profile = null;
      } finally {
        this.loadingProfile = false;
      }
    },
  },
}).mount("#app");
