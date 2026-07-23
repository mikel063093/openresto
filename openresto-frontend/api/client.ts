import { detectLocale, getBrowserLanguages } from "@/i18n/locale";
import { StorageService } from "@/services/storage";

const LANGUAGE_STORAGE_KEY = "openresto-language";

export function buildUrl(path: string): string {
  const API_URL = process.env.EXPO_PUBLIC_API_URL;
  const base = API_URL?.replace(/\/$/, "") ?? "";
  if (!base) return `/api${path}`;

  // Check if the URL path already contains /api as a segment
  const urlObj = (() => {
    try {
      return new URL(base);
    } catch {
      return null;
    }
  })();

  const hasApi = urlObj ? urlObj.pathname.split("/").includes("api") : base.includes("/api");

  return hasApi ? `${base}${path}` : `${base}/api${path}`;
}

type Method = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

interface RequestOptions {
  body?: unknown;
  headers?: Record<string, string>;
  credentials?: RequestCredentials;
}

export function getRequestLanguage(): string {
  return detectLocale({
    storedLocale: StorageService.getItem(LANGUAGE_STORAGE_KEY),
    browserLanguages: getBrowserLanguages(),
  });
}

export function withApiHeaders(headers: Record<string, string> = {}): Record<string, string> {
  return headers["Accept-Language"]
    ? { ...headers }
    : { ...headers, "Accept-Language": getRequestLanguage() };
}

export async function apiFetch(path: string, init: RequestInit = {}): Promise<Response> {
  return fetch(buildUrl(path), {
    ...init,
    headers: withApiHeaders((init.headers as Record<string, string> | undefined) ?? {}),
    credentials: init.credentials ?? "include",
  });
}

export async function api(
  method: Method,
  path: string,
  opts: RequestOptions = {}
): Promise<Response> {
  const headers = withApiHeaders({ ...opts.headers });

  let rawBody: string | undefined;
  if (opts.body !== undefined) {
    headers["Content-Type"] = "application/json";
    rawBody = JSON.stringify(opts.body);
  }

  const fetchOpts: RequestInit = {
    method,
    headers,
    body: rawBody,
    credentials: opts.credentials ?? "include",
  };

  const response = await fetch(buildUrl(path), fetchOpts);
  return response;
}

export const get = (path: string, opts?: RequestOptions) => api("GET", path, opts);

export const post = (path: string, body?: unknown, opts?: RequestOptions) =>
  api("POST", path, { ...opts, body });

export const put = (path: string, body?: unknown, opts?: RequestOptions) =>
  api("PUT", path, { ...opts, body });

export const patch = (path: string, body?: unknown, opts?: RequestOptions) =>
  api("PATCH", path, { ...opts, body });

export const del = (path: string, opts?: RequestOptions) => api("DELETE", path, opts);
