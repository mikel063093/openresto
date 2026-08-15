import { buildUrl, api, getRequestLanguage } from "@/api/client";
import { Platform } from "react-native";

const mockFetch = jest.fn();
global.fetch = mockFetch;

const localStorageMock = (() => {
  let store: Record<string, string> = {};
  return {
    getItem: jest.fn((key: string) => store[key] ?? null),
    setItem: jest.fn((key: string, value: string) => {
      store[key] = value;
    }),
    removeItem: jest.fn((key: string) => {
      delete store[key];
    }),
    clear: jest.fn(() => {
      store = {};
    }),
  };
})();

Object.defineProperty(global, "localStorage", {
  value: localStorageMock,
  configurable: true,
});

beforeEach(() => {
  mockFetch.mockReset();
  localStorage.clear();
  Object.defineProperty(Platform, "OS", {
    value: "web",
    configurable: true,
  });
});

describe("buildUrl", () => {
  const originalEnv = process.env.EXPO_PUBLIC_API_URL;

  afterEach(() => {
    process.env.EXPO_PUBLIC_API_URL = originalEnv;
  });

  it("prepends /api when no EXPO_PUBLIC_API_URL is set", () => {
    process.env.EXPO_PUBLIC_API_URL = "";
    expect(buildUrl("/test")).toBe("/api/test");
  });

  it("handles base URL with trailing slash", () => {
    process.env.EXPO_PUBLIC_API_URL = "https://api.test.com/";
    expect(buildUrl("/foo")).toBe("https://api.test.com/api/foo");
  });

  it("handles base URL that already includes /api", () => {
    process.env.EXPO_PUBLIC_API_URL = "https://test.com/api";
    expect(buildUrl("/foo")).toBe("https://test.com/api/foo");
  });

  it("handles base URL without /api", () => {
    process.env.EXPO_PUBLIC_API_URL = "https://test.com";
    expect(buildUrl("/foo")).toBe("https://test.com/api/foo");
  });
});

describe("api", () => {
  it("sends GET with credentials: include by default", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    await api("GET", "/foo");
    const [url, opts] = mockFetch.mock.calls[0];
    expect(url).toContain("/api/foo");
    expect(opts.method).toBe("GET");
    expect(opts.credentials).toBe("include");
    expect(opts.headers["Accept-Language"]).toBe("en");
    expect(opts.body).toBeUndefined();
  });

  it("sends POST with JSON body and Content-Type header", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    await api("POST", "/bar", { body: { key: "value" } });
    const [, opts] = mockFetch.mock.calls[0];
    expect(opts.method).toBe("POST");
    expect(opts.headers["Accept-Language"]).toBe("en");
    expect(opts.headers["Content-Type"]).toBe("application/json");
    expect(JSON.parse(opts.body)).toEqual({ key: "value" });
  });

  it("allows passing custom headers", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    await api("GET", "/headers", { headers: { "X-Custom": "test" } });
    const [, opts] = mockFetch.mock.calls[0];
    expect(opts.headers["Accept-Language"]).toBe("en");
    expect(opts.headers["X-Custom"]).toBe("test");
  });

  it("does not set Content-Type when no body provided", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    await api("DELETE", "/baz");
    const [, opts] = mockFetch.mock.calls[0];
    expect(opts.headers["Accept-Language"]).toBe("en");
    expect(opts.headers["Content-Type"]).toBeUndefined();
  });

  it("allows overriding credentials", async () => {
    mockFetch.mockResolvedValueOnce({ ok: true });
    await api("GET", "/pub", { credentials: "omit" });
    expect(mockFetch.mock.calls[0][1].credentials).toBe("omit");
  });

  it("uses the stored es-CO locale for Accept-Language", async () => {
    localStorage.setItem("openresto-language", "es-CO");
    mockFetch.mockResolvedValueOnce({ ok: true });

    await api("GET", "/locale");

    expect(mockFetch.mock.calls[0][1].headers["Accept-Language"]).toBe("es-CO");
  });

  it("normalizes legacy stored es to es-CO", async () => {
    localStorage.setItem("openresto-language", "es");
    mockFetch.mockResolvedValueOnce({ ok: true });

    await api("GET", "/legacy-locale");

    expect(mockFetch.mock.calls[0][1].headers["Accept-Language"]).toBe("es-CO");
  });
});

describe("getRequestLanguage", () => {
  const originalNavigator = global.navigator;

  afterEach(() => {
    Object.defineProperty(global, "navigator", {
      value: originalNavigator,
      configurable: true,
    });
  });

  it("falls back to browser Spanish variants when no locale is stored", () => {
    Object.defineProperty(global, "navigator", {
      value: { languages: ["es-MX"] },
      configurable: true,
    });

    expect(getRequestLanguage()).toBe("es-CO");
  });
});
