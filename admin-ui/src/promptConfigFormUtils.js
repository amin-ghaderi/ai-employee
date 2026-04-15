/**
 * Pure helpers for Prompt Configuration (Persona) forms — no React deps.
 * @param {string | null | undefined} text
 * @returns {string | null} null when empty or invalid JSON
 */
export function normalizeOptionalSchemaJson(text) {
  const t = String(text ?? '').trim();
  if (!t) return null;
  try {
    return JSON.stringify(JSON.parse(t));
  } catch {
    return null;
  }
}

/**
 * @param {Record<string, unknown>} ext
 * @returns {boolean}
 */
export function hasAnyPromptExtensionValue(ext) {
  if (!ext || typeof ext !== 'object') return false;
  return Object.values(ext).some(
    (v) => v != null && String(v).trim().length > 0
  );
}
