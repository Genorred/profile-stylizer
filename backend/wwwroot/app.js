const { createApp } = Vue;

const TOKEN_KEY = "access_token";
const AUTH_TYPE_KEY = "auth_type";
const STYLIZED_KEY = "stylized_card";

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

      stylizedImageUrl: null,
      stylizedLoading: false,
      stylizedError: "",
    };
  },

  computed: {
    isAuthenticated() {
      return !!localStorage.getItem(TOKEN_KEY);
    },
  },

  mounted() {
    const token = localStorage.getItem(TOKEN_KEY);
    const authType = localStorage.getItem(AUTH_TYPE_KEY);

    // restore image from localStorage
    const cachedImage = localStorage.getItem(STYLIZED_KEY);
    if (cachedImage) {
      this.stylizedImageUrl = cachedImage;
    }

    if (token) {
      this.telegramStatus =
        authType === "telegram"
          ? "Telegram сесія активна"
          : "Сесія активна";

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
        localStorage.setItem(AUTH_TYPE_KEY, "password");

        this.loginStatus = "Успішний вхід";
        this.telegramStatus = "Сесія активна";

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
              localStorage.setItem(AUTH_TYPE_KEY, "telegram");

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

    // convert blob → base64
    blobToBase64(blob) {
      return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
      });
    },

    async loadStylizedCard() {
      this.stylizedLoading = true;
      this.stylizedError = "";

      try {
        const res = await axios.get("/stylized-card", {
          responseType: "blob",
        });

        const base64 = await this.blobToBase64(res.data);

        this.stylizedImageUrl = base64;
        localStorage.setItem(STYLIZED_KEY, base64);
      } catch (e) {
        const status = e.response?.status;

        if (status === 401 || status === 404) {
          this.stylizedError =
            "Користувач не авторизований або не зареєстрований";
        } else {
          this.stylizedError = "Помилка генерації картки";
        }
      } finally {
        this.stylizedLoading = false;
      }
    },
  },
}).mount("#app");
