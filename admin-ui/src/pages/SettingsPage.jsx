import { useCallback, useEffect, useState } from 'react';
import settingsService from '../api/services/settings.js';

/** @param {unknown} e */
function messageFromAxiosError(e) {
  const d = e?.response?.data;
  if (d && typeof d === 'object') {
    if (typeof d.message === 'string' && d.message) return d.message;
    if (typeof d.title === 'string' && d.title) return d.title;
    if (Array.isArray(d.errors) && d.errors.length) return d.errors.join('; ');
  }
  if (e && typeof e.message === 'string') return e.message;
  return 'Request failed';
}

function validatePublicBaseUrlInput(raw) {
  const t = raw.trim();
  if (t.length === 0) return { ok: true, normalized: '' };
  try {
    const u = new URL(t);
    if (!u.protocol || (u.protocol !== 'http:' && u.protocol !== 'https:')) {
      return { ok: false, error: 'URL must start with http:// or https://' };
    }
    return { ok: true, normalized: t };
  } catch {
    return { ok: false, error: 'Enter a valid absolute URL (e.g. https://api.example.com)' };
  }
}

export default function SettingsPage() {
  const [publicBaseUrl, setPublicBaseUrl] = useState('');
  const [loading, setLoading] = useState(true);
  const [loadError, setLoadError] = useState('');
  const [validationError, setValidationError] = useState('');
  const [actionError, setActionError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');
  const [saveBusy, setSaveBusy] = useState(false);
  const [clearBusy, setClearBusy] = useState(false);

  const load = useCallback(async () => {
    setLoadError('');
    setLoading(true);
    try {
      const data = await settingsService.getPublicBaseUrl();
      const v =
        data && typeof data === 'object' && 'publicBaseUrl' in data
          ? data.publicBaseUrl
          : '';
      setPublicBaseUrl(typeof v === 'string' ? v : v == null ? '' : String(v));
    } catch (e) {
      setPublicBaseUrl('');
      setLoadError(messageFromAxiosError(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    load();
  }, [load]);

  async function handleSave(e) {
    e.preventDefault();
    setValidationError('');
    setActionError('');
    setSuccessMessage('');

    const check = validatePublicBaseUrlInput(publicBaseUrl);
    if (!check.ok) {
      setValidationError(check.error ?? 'Invalid URL');
      return;
    }

    setSaveBusy(true);
    try {
      const normalized = check.normalized ?? publicBaseUrl.trim();
      const data = await settingsService.updatePublicBaseUrl(
        normalized.length === 0 ? null : normalized
      );
      const next =
        data && typeof data === 'object' && 'publicBaseUrl' in data
          ? data.publicBaseUrl
          : '';
      setPublicBaseUrl(
        typeof next === 'string' ? next : next == null ? '' : String(next)
      );
      setSuccessMessage('Public base URL saved.');
    } catch (e) {
      setActionError(messageFromAxiosError(e));
    } finally {
      setSaveBusy(false);
    }
  }

  async function handleClear() {
    if (
      !window.confirm(
        'Clear the database override for Public Base URL? The app will fall back to configuration (appsettings / environment).'
      )
    ) {
      return;
    }
    setActionError('');
    setSuccessMessage('');
    setValidationError('');
    setClearBusy(true);
    try {
      const data = await settingsService.deletePublicBaseUrl();
      const next =
        data && typeof data === 'object' && 'publicBaseUrl' in data
          ? data.publicBaseUrl
          : '';
      setPublicBaseUrl(
        typeof next === 'string' ? next : next == null ? '' : String(next)
      );
      setSuccessMessage('Database override cleared.');
    } catch (e) {
      setActionError(messageFromAxiosError(e));
    } finally {
      setClearBusy(false);
    }
  }

  const busy = saveBusy || clearBusy;

  return (
    <div>
      <h1>Settings</h1>
      <p style={{ color: '#64748b', fontSize: '0.9rem', maxWidth: '640px' }}>
        Public Base URL is stored in the database when set here. It overrides{' '}
        <code>App:PublicBaseUrl</code> from configuration until cleared. Used for
        Telegram webhook URLs and related diagnostics.
      </p>

      {loading && <p>Loading…</p>}
      {loadError && <p style={{ color: '#b91c1c' }}>{loadError}</p>}

      {!loading && !loadError && (
        <form onSubmit={handleSave} style={{ marginTop: '1rem' }}>
          <div style={{ marginBottom: '12px' }}>
            <label htmlFor="public-base-url">Public Base URL</label>
            <br />
            <input
              id="public-base-url"
              type="url"
              placeholder="https://api.example.com"
              value={publicBaseUrl}
              onChange={(e) => setPublicBaseUrl(e.target.value)}
              disabled={busy}
              style={{
                width: '100%',
                maxWidth: '480px',
                minWidth: '240px',
                marginTop: '6px',
                padding: '6px 8px',
                fontFamily: 'ui-monospace, monospace',
                fontSize: '0.9rem',
                boxSizing: 'border-box',
              }}
            />
          </div>
          {validationError && (
            <p style={{ color: '#b91c1c', marginTop: '4px' }}>{validationError}</p>
          )}
          {actionError && (
            <p style={{ color: '#b91c1c', marginTop: '4px' }}>{actionError}</p>
          )}
          {successMessage && (
            <p style={{ color: '#15803d', marginTop: '4px' }}>{successMessage}</p>
          )}
          <div style={{ marginTop: '12px' }}>
            <button type="submit" disabled={busy}>
              {saveBusy ? 'Saving…' : 'Save'}
            </button>{' '}
            <button type="button" disabled={busy} onClick={handleClear}>
              {clearBusy ? 'Clearing…' : 'Clear override'}
            </button>
          </div>
        </form>
      )}
    </div>
  );
}
