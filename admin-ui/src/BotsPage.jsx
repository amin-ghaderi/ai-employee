import { useCallback, useEffect, useState } from 'react';
import { api } from './api/client.js';
import { BotsApi } from './api/services/bots.js';

function formatChannel(value) {
  if (value === undefined || value === null) return '—';
  if (typeof value === 'number') {
    if (value === 0) return 'Telegram';
    return String(value);
  }
  return String(value);
}

function normId(value) {
  if (value === undefined || value === null) return '';
  return String(value);
}

export default function BotsPage() {
  const [bots, setBots] = useState([]);
  const [personas, setPersonas] = useState([]);
  const [behaviors, setBehaviors] = useState([]);
  const [refsError, setRefsError] = useState('');
  const [newBotName, setNewBotName] = useState('');
  const [createError, setCreateError] = useState('');
  const [createBusy, setCreateBusy] = useState(false);
  const [assignSelections, setAssignSelections] = useState({});
  const [assignErrors, setAssignErrors] = useState({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [loadingIds, setLoadingIds] = useState(() => new Set());
  /** @type {Record<string, 'assign' | 'enable' | 'disable'>} */
  const [loadingKindByBotId, setLoadingKindByBotId] = useState({});
  const [actionErrors, setActionErrors] = useState({});

  const loadList = useCallback(async () => {
    setError('');
    setLoading(true);
    try {
      const data = await BotsApi.list();
      setBots(Array.isArray(data) ? data : []);
    } catch (e) {
      setBots([]);
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Failed to load bots'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadList();
  }, [loadList]);

  useEffect(() => {
    let cancelled = false;
    setRefsError('');
    (async () => {
      try {
        const [pRes, bRes] = await Promise.all([
          api.get('/personas'),
          api.get('/behaviors'),
        ]);
        if (cancelled) return;
        setPersonas(Array.isArray(pRes.data) ? pRes.data : []);
        setBehaviors(Array.isArray(bRes.data) ? bRes.data : []);
      } catch (e) {
        if (!cancelled) {
          setPersonas([]);
          setBehaviors([]);
          setRefsError(
            e.response?.data?.title ||
              e.response?.data?.message ||
              e.message ||
              'Failed to load prompt configurations or behaviors'
          );
        }
      }
    })();
    return () => {
      cancelled = true;
    };
  }, []);

  function setAssignSelection(botId, patch) {
    const key = String(botId);
    setAssignSelections((prev) => ({
      ...prev,
      [key]: { ...(prev[key] ?? {}), ...patch },
    }));
  }

  function personaSelectValue(bot) {
    const id = bot.id;
    const override = assignSelections[String(id)]?.personaId;
    if (override !== undefined) return override;
    return normId(bot.personaId);
  }

  function behaviorSelectValue(bot) {
    const id = bot.id;
    const override = assignSelections[String(id)]?.behaviorId;
    if (override !== undefined) return override;
    return normId(bot.behaviorId);
  }

  async function handleCreateBot(e) {
    e.preventDefault();
    const name = newBotName.trim();
    if (!name) return;
    setCreateError('');
    setCreateBusy(true);
    try {
      await BotsApi.create({ name });
      setNewBotName('');
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          'Create failed';
      setCreateError(msg);
    } finally {
      setCreateBusy(false);
    }
  }

  async function handleAssign(bot) {
    const id = bot.id;
    const personaId = personaSelectValue(bot);
    const behaviorId = behaviorSelectValue(bot);
    if (!personaId || !behaviorId) return;

    clearAssignError(id);
    addLoadingId(id, 'assign');
    try {
      await BotsApi.assign(id, {
        personaId,
        behaviorId,
        languageProfileId: bot.languageProfileId,
      });
      setAssignSelections((prev) => {
        const key = String(id);
        if (!(key in prev)) return prev;
        const next = { ...prev };
        delete next[key];
        return next;
      });
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          'Assign failed';
      setAssignErrors((prev) => ({ ...prev, [String(id)]: msg }));
    } finally {
      removeLoadingId(id);
    }
  }

  function clearAssignError(id) {
    setAssignErrors((prev) => {
      const key = String(id);
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  function addLoadingId(id, kind) {
    const key = String(id);
    setLoadingIds((prev) => {
      const next = new Set(prev);
      next.add(id);
      return next;
    });
    if (kind) {
      setLoadingKindByBotId((prev) => ({ ...prev, [key]: kind }));
    }
  }

  function removeLoadingId(id) {
    const key = String(id);
    setLoadingIds((prev) => {
      const next = new Set(prev);
      next.delete(id);
      return next;
    });
    setLoadingKindByBotId((prev) => {
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  function clearActionError(id) {
    setActionErrors((prev) => {
      const key = String(id);
      if (!(key in prev)) return prev;
      const next = { ...prev };
      delete next[key];
      return next;
    });
  }

  async function handleEnable(id) {
    clearActionError(id);
    addLoadingId(id, 'enable');
    try {
      await BotsApi.enable(id);
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
        e.response?.data?.message ||
        e.message ||
        'Enable failed';
      setActionErrors((prev) => ({ ...prev, [String(id)]: msg }));
    } finally {
      removeLoadingId(id);
    }
  }

  async function handleDisable(id) {
    clearActionError(id);
    addLoadingId(id, 'disable');
    try {
      await BotsApi.disable(id);
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
        e.response?.data?.message ||
        e.message ||
        'Disable failed';
      setActionErrors((prev) => ({ ...prev, [String(id)]: msg }));
    } finally {
      removeLoadingId(id);
    }
  }

  return (
    <div>
      <h1>Bots</h1>
      <form onSubmit={handleCreateBot}>
        <label htmlFor="new-bot-name">Name</label>{' '}
        <input
          id="new-bot-name"
          type="text"
          value={newBotName}
          onChange={(e) => setNewBotName(e.target.value)}
          disabled={createBusy}
        />{' '}
        <button type="submit" disabled={createBusy}>
          {createBusy ? 'Creating…' : 'Create Bot'}
        </button>
      </form>
      {createError && <p>{createError}</p>}
      {refsError && <p>{refsError}</p>}
      {loading && <p>Loading…</p>}
      {error && <p>{error}</p>}
      {!loading && !error && Object.keys(actionErrors).length > 0 && (
        <div>
          {Object.entries(actionErrors).map(([id, msg]) => (
            <p key={id}>
              Bot {id}: {msg}
            </p>
          ))}
        </div>
      )}
      {!loading && !error && (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Channel</th>
              <th>Enabled</th>
              <th>Prompt configuration</th>
              <th>Behavior</th>
              <th>Assign</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {bots.map((bot) => {
              const id = bot.id;
              const busy = loadingIds.has(id);
              const kind = loadingKindByBotId[String(id)];
              const personaId = personaSelectValue(bot);
              const behaviorId = behaviorSelectValue(bot);
              const matchesBot =
                normId(personaId) === normId(bot.personaId) &&
                normId(behaviorId) === normId(bot.behaviorId);
              const assignDisabled =
                busy ||
                !personaId ||
                !behaviorId ||
                matchesBot;
              return (
                <tr key={id}>
                  <td>{bot.name ?? ''}</td>
                  <td>{formatChannel(bot.channel)}</td>
                  <td>{bot.isEnabled ? 'Yes' : 'No'}</td>
                  <td>
                    <select
                      value={personaId}
                      disabled={busy}
                      onChange={(e) =>
                        setAssignSelection(id, { personaId: e.target.value })
                      }
                    >
                      <option value="">—</option>
                      {personas.map((p) => (
                        <option key={p.id} value={p.id}>
                          {p.displayName ?? p.id}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <select
                      value={behaviorId}
                      disabled={busy}
                      onChange={(e) =>
                        setAssignSelection(id, { behaviorId: e.target.value })
                      }
                    >
                      <option value="">—</option>
                      {behaviors.map((b) => (
                        <option key={b.id} value={b.id}>
                          {b.judgeCommandPrefix
                            ? `${b.judgeCommandPrefix} (${b.id})`
                            : String(b.id)}
                        </option>
                      ))}
                    </select>
                  </td>
                  <td>
                    <button
                      type="button"
                      disabled={assignDisabled}
                      onClick={() => handleAssign(bot)}
                    >
                      {busy && kind === 'assign'
                        ? 'Assigning...'
                        : 'Assign'}
                    </button>
                    {assignErrors[String(id)] && (
                      <p>{assignErrors[String(id)]}</p>
                    )}
                  </td>
                  <td>
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handleEnable(id)}
                    >
                      {busy ? 'Processing...' : 'Enable'}
                    </button>{' '}
                    <button
                      type="button"
                      disabled={busy}
                      onClick={() => handleDisable(id)}
                    >
                      {busy ? 'Processing...' : 'Disable'}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      )}
    </div>
  );
}
