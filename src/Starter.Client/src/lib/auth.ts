export interface User {
  id: string;
  username: string;
  color: string;
}

const STORAGE_KEY = 'tabletop_user';

export function getUser(): User | null {
  if (typeof window === 'undefined') return null;
  const stored = localStorage.getItem(STORAGE_KEY);
  if (!stored) return null;
  try {
    return JSON.parse(stored);
  } catch {
    return null;
  }
}

export function saveUser(user: User): void {
  if (typeof window === 'undefined') return;
  localStorage.setItem(STORAGE_KEY, JSON.stringify(user));
}

export function clearUser(): void {
  if (typeof window === 'undefined') return;
  localStorage.removeItem(STORAGE_KEY);
}

export function createUser(username: string): User {
  const colors = [
    '#ef4444', '#f59e0b', '#eab308', '#22c55e',
    '#3b82f6', '#6366f1', '#8b5cf6', '#ec4899'
  ];
  const user: User = {
    id: crypto.randomUUID(),
    username,
    color: colors[Math.floor(Math.random() * colors.length)]
  };
  saveUser(user);
  return user;
}

