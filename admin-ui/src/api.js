// NOTE: Restart dev server after changing .env files
import axios from 'axios';

const baseURL = `${import.meta.env.VITE_API_URL ?? 'http://localhost:5155'}/admin`;

export const api = axios.create({
  baseURL,
  headers: {
    'X-Admin-Key': 'your-secret-key',
    'Content-Type': 'application/json',
  },
});
