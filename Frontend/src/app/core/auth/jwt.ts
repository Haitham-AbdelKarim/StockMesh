export interface JwtPayload {
  sub?: string;
  store_id?: string;
  email?: string;
  role?: string;
  vertical_category?: string;
  exp?: number;
}

export function decodeJwt(token: string): JwtPayload {
  const parts = token.split('.');
  if (parts.length !== 3) {
    return {};
  }

  const payload = parts[1];
  const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(normalized.length + ((4 - (normalized.length % 4)) % 4), '=');
  const json = decodeURIComponent(
    atob(padded)
      .split('')
      .map((char) => `%${char.charCodeAt(0).toString(16).padStart(2, '0')}`)
      .join(''),
  );

  try {
    return JSON.parse(json) as JwtPayload;
  } catch {
    return {};
  }
}