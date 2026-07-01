const { createApp } = Vue;

const TOKEN_KEY = "access_token";

axios.interceptors.request.use((config) => {
  const token = localStorage.getItem(TOKEN_KEY);
  if (token) config.headers.Authorization = "Bearer " + token;
  return config;
});

createApp({
  data() {
    return {
      authTab: "login",

      email: "",
      password: "",

      loginStatus: "",
      registerStatus: "",
      telegramStatus: "Telegram не підключено",

      registerForm: {
        name: "",
        email: "",
        password: "",
      },

      profile: null,
      loadingProfile: false,

      telegramTimer: null,
    };
  },

  mounted() {
    if (localStorage.getItem(TOKEN_KEY)) {
      this.loadProfile();
    }
  },

  methods: {
    async login() {
      try {
        const res = await axios.post("/login", {
          email: this.email,
          password: this.password,
        });

        localStorage.setItem(TOKEN_KEY, res.data.access_token);
        this.loginStatus = "Успішний вхід";

        await this.loadProfile();
      } catch {
        this.loginStatus = "Помилка входу";
      }
    },

    async register() {
      try {
        await axios.post("/auth/register", this.registerForm);

        this.registerStatus = "Реєстрація успішна";
        this.authTab = "login";

        this.email = this.registerForm.email;

        this.registerForm = {
          name: "",
          email: "",
          password: "",
        };
      } catch (e) {
        if (e.response?.status === 409) {
          this.registerStatus = "Користувач вже існує";
        } else {
          this.registerStatus = "Помилка реєстрації";
        }
      }
    },

    async telegramLogin() {
      try {
        const res = await axios.post("/auth/telegram/start");
        const data = res.data;

        window.open(data.bot_url, "_blank");

        this.telegramStatus = "Очікуємо Telegram...";

        this.telegramTimer = setInterval(async () => {
          try {
            const r = await axios.get(
              `/auth/telegram/status?token=${encodeURIComponent(data.token)}`
            );

            if (r.data.status === "pending") return;

            clearInterval(this.telegramTimer);

            if (r.data.status === "completed") {
              localStorage.setItem(TOKEN_KEY, r.data.access_token);
              this.telegramStatus = "Успішний Telegram login";

              await this.loadProfile();
            } else {
              this.telegramStatus = "Таймаут";
            }
          } catch {
            clearInterval(this.telegramTimer);
            this.telegramStatus = "Помилка Telegram";
          }
        }, 2000);
      } catch {
        this.telegramStatus = "Telegram error";
      }
    },

    async loadProfile() {
      this.loadingProfile = true;

      try {
        const res = await axios.get("/auth/me");
        this.profile = res.data;
      } catch {
        this.profile = null;
      } finally {
        this.loadingProfile = false;
      }
    },
  },
}).mount("#app");
