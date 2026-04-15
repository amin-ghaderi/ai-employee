import { useCallback, useEffect, useState } from 'react';
import { BehaviorsApi } from './api/services/behaviors.js';

const DEFAULT_ENGAGEMENT_RULES = {
  newUserWindowHours: 48,
  activeMessageThreshold: 10,
  inactiveHoursThreshold: 72,
  highEngagementScoreThreshold: 0.7,
  engagementNormalizationFactor: 1,
  stickyTags: [],
};

const INITIAL_FORM_STATE = {
  enableGatewayRouting: false,
  gatewayTriggerPhrases: '',
  gatewayMatchType: '0',
  gatewayCaseSensitive: false,
  judgeContextMessageCount: '5',
  judgePerMessageMaxChars: '2000',
  judgeCommandPrefix: '',
  excludeCommandsFromJudgeContext: false,
  followUpIndex: '',
  captureIndex: '',
  answerKeys: '',
  enableChat: true,
  enableLead: true,
  enableJudge: true,
  hotLeadPotentialValue: '',
  hotLeadTag: '',
};

function emptyForm() {
  return { ...INITIAL_FORM_STATE };
}

function parseCommaList(value) {
  return String(value)
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

/** @returns {number | null | NaN} null = empty, NaN = invalid */
function parseOptionalNonNegInt(raw) {
  const t = String(raw).trim();
  if (t === '') return null;
  const n = Number.parseInt(t, 10);
  if (Number.isNaN(n) || n < 0) return NaN;
  return n;
}

export default function BehaviorsPage() {
  const [behaviors, setBehaviors] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [form, setForm] = useState(() => emptyForm());
  const [validationError, setValidationError] = useState('');
  const [createError, setCreateError] = useState('');
  const [saving, setSaving] = useState(false);
  const [editingId, setEditingId] = useState(null);
  const [loadingEdit, setLoadingEdit] = useState(false);

  const loadList = useCallback(async () => {
    setError('');
    setLoading(true);
    try {
      const data = await BehaviorsApi.list();
      setBehaviors(Array.isArray(data) ? data : []);
    } catch (e) {
      setBehaviors([]);
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Failed to load behaviors'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadList();
  }, [loadList]);

  function validate() {
    const jcc = Number.parseInt(String(form.judgeContextMessageCount), 10);
    const jpm = Number.parseInt(String(form.judgePerMessageMaxChars), 10);
    if (!Number.isFinite(jcc) || jcc <= 0) {
      return 'judgeContextMessageCount must be greater than 0.';
    }
    if (!Number.isFinite(jpm) || jpm <= 0) {
      return 'judgePerMessageMaxChars must be greater than 0.';
    }

    const fu = parseOptionalNonNegInt(form.followUpIndex);
    const cap = parseOptionalNonNegInt(form.captureIndex);
    if (Number.isNaN(fu) || Number.isNaN(cap)) {
      return 'followUpIndex and captureIndex must be empty or non-negative integers.';
    }

    const answerKeys = parseCommaList(form.answerKeys);
    if (cap !== null && answerKeys.length === 0) {
      return 'When captureIndex is set, answerKeys must contain at least one value.';
    }

    if (fu !== null && cap !== null && cap < fu) {
      return 'When both indices are set, captureIndex must be >= followUpIndex.';
    }

    if (form.enableGatewayRouting) {
      const phrases = String(form.gatewayTriggerPhrases ?? '').trim();
      if (!phrases) {
        return 'When gateway routing is enabled, trigger phrases are required (comma-separated).';
      }
    }

    const gmt = Number.parseInt(String(form.gatewayMatchType), 10);
    if (!Number.isFinite(gmt) || gmt < 0 || gmt > 2) {
      return 'gatewayMatchType must be 0 (Contains), 1 (Exact), or 2 (Regex).';
    }

    return null;
  }

  function buildRequestBody() {
    const fu = parseOptionalNonNegInt(form.followUpIndex);
    const cap = parseOptionalNonNegInt(form.captureIndex);
    const answerKeys = parseCommaList(form.answerKeys);
    const gatewayMatchType = Number.parseInt(String(form.gatewayMatchType), 10);
    return {
      enableGatewayRouting: form.enableGatewayRouting,
      gatewayTriggerPhrases: String(form.gatewayTriggerPhrases ?? '').trim() || null,
      gatewayMatchType: Number.isFinite(gatewayMatchType) ? gatewayMatchType : 0,
      gatewayCaseSensitive: form.gatewayCaseSensitive,
      judgeContextMessageCount: Number.parseInt(
        String(form.judgeContextMessageCount),
        10
      ),
      judgePerMessageMaxChars: Number.parseInt(
        String(form.judgePerMessageMaxChars),
        10
      ),
      judgeCommandPrefix: String(form.judgeCommandPrefix).trim(),
      excludeCommandsFromJudgeContext: form.excludeCommandsFromJudgeContext,
      onboardingFirstMessageOnly: false,
      leadFlow: {
        followUpIndex: fu,
        captureIndex: cap,
        answerKeys,
      },
      automationRules: [],
      engagementRules: { ...DEFAULT_ENGAGEMENT_RULES },
      hotLeadPotentialValue: String(form.hotLeadPotentialValue).trim(),
      hotLeadTag: String(form.hotLeadTag).trim(),
      enableChat: form.enableChat,
      enableLead: form.enableLead,
      enableJudge: form.enableJudge,
    };
  }

  async function handleEdit(id) {
    setLoadingEdit(true);
    setCreateError('');
    try {
      const b = await BehaviorsApi.getById(id);
      setEditingId(id);
      setForm({
        enableGatewayRouting: Boolean(b.enableGatewayRouting),
        gatewayTriggerPhrases: b.gatewayTriggerPhrases ?? '',
        gatewayMatchType: String(
          b.gatewayMatchType !== undefined && b.gatewayMatchType !== null
            ? b.gatewayMatchType
            : '0'
        ),
        gatewayCaseSensitive: Boolean(b.gatewayCaseSensitive),
        judgeContextMessageCount: String(b.judgeContextMessageCount ?? '5'),
        judgePerMessageMaxChars: String(b.judgePerMessageMaxChars ?? '2000'),
        judgeCommandPrefix: b.judgeCommandPrefix ?? '',
        excludeCommandsFromJudgeContext: b.excludeCommandsFromJudgeContext ?? false,
        followUpIndex: b.leadFlow?.followUpIndex != null ? String(b.leadFlow.followUpIndex) : '',
        captureIndex: b.leadFlow?.captureIndex != null ? String(b.leadFlow.captureIndex) : '',
        answerKeys: (b.leadFlow?.answerKeys ?? []).join(', '),
        enableChat: b.enableChat ?? true,
        enableLead: b.enableLead ?? true,
        enableJudge: b.enableJudge ?? true,
        hotLeadPotentialValue: b.hotLeadPotentialValue ?? '',
        hotLeadTag: b.hotLeadTag ?? '',
      });
      setValidationError('');
    } catch (e) {
      setCreateError('Failed to load behavior: ' + (e.message || e));
    } finally {
      setLoadingEdit(false);
    }
  }

  function handleCancelEdit() {
    setEditingId(null);
    setForm(emptyForm());
    setValidationError('');
    setCreateError('');
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (saving) return;
    const v = validate();
    if (v) {
      setValidationError(v);
      return;
    }
    setValidationError('');
    setCreateError('');
    setSaving(true);
    try {
      if (editingId) {
        await BehaviorsApi.update(editingId, buildRequestBody());
        setEditingId(null);
      } else {
        await BehaviorsApi.create(buildRequestBody());
      }
      setForm(emptyForm());
      setSaving(false);
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          (editingId ? 'Update failed' : 'Create failed');
      setCreateError(msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h1>Behaviors</h1>
      <p style={{ maxWidth: 720 }}>
        Judge and lead <strong>prompts</strong> (templates, instructions, output schemas) are configured per bot under{' '}
        <strong>Prompt Configuration</strong>. This page covers runtime behavior: gateway routing triggers, judge context, lead flow indices, feature flags, and hot-lead rules.
      </p>
      {editingId && (
        <p><strong>Editing behavior:</strong> {editingId}{' '}
          <button type="button" onClick={handleCancelEdit}>Cancel</button>
        </p>
      )}
      <form onSubmit={handleSubmit}>
        <fieldset disabled={saving}>
          <legend>Gateway Routing (Chat)</legend>
          <div>
            <label>
              <input
                type="checkbox"
                checked={form.enableGatewayRouting}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    enableGatewayRouting: e.target.checked,
                  }))
                }
              />{' '}
              Enable gateway routing
            </label>
          </div>
          <div>
            <label htmlFor="bh-gw-phrases">Trigger phrases (comma-separated)</label>{' '}
            <input
              id="bh-gw-phrases"
              type="text"
              style={{ width: '100%', maxWidth: 560 }}
              value={form.gatewayTriggerPhrases}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  gatewayTriggerPhrases: e.target.value,
                }))
              }
              placeholder="e.g. help, speak to human"
            />
          </div>
          <div>
            <label htmlFor="bh-gw-match">Match type</label>{' '}
            <select
              id="bh-gw-match"
              value={form.gatewayMatchType}
              onChange={(e) =>
                setForm((f) => ({ ...f, gatewayMatchType: e.target.value }))
              }
            >
              <option value="0">Contains</option>
              <option value="1">Exact</option>
              <option value="2">Regex</option>
            </select>
          </div>
          {form.gatewayMatchType === '2' && (
            <p style={{ margin: '0.35rem 0 0', fontSize: '0.85rem', color: '#64748b', maxWidth: 560 }}>
              Regex patterns are validated at runtime; invalid regex may cause errors when users send messages.
            </p>
          )}
          <div style={{ marginTop: '0.35rem' }}>
            <label>
              <input
                type="checkbox"
                checked={form.gatewayCaseSensitive}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    gatewayCaseSensitive: e.target.checked,
                  }))
                }
              />{' '}
              Case sensitive
            </label>
          </div>
        </fieldset>
        <fieldset disabled={saving}>
          <legend>Judge</legend>
          <div>
            <label htmlFor="bh-jcc">Context message count</label>{' '}
            <input
              id="bh-jcc"
              type="number"
              min={1}
              step={1}
              value={form.judgeContextMessageCount}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  judgeContextMessageCount: e.target.value,
                }))
              }
            />
          </div>
          <div>
            <label htmlFor="bh-jpm">Per-message max chars</label>{' '}
            <input
              id="bh-jpm"
              type="number"
              min={1}
              step={1}
              value={form.judgePerMessageMaxChars}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  judgePerMessageMaxChars: e.target.value,
                }))
              }
            />
          </div>
          <div>
            <label htmlFor="bh-prefix">Command prefix</label>{' '}
            <input
              id="bh-prefix"
              type="text"
              value={form.judgeCommandPrefix}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  judgeCommandPrefix: e.target.value,
                }))
              }
            />
          </div>
          <div>
            <label>
              <input
                type="checkbox"
                checked={form.excludeCommandsFromJudgeContext}
                onChange={(e) =>
                  setForm((f) => ({
                    ...f,
                    excludeCommandsFromJudgeContext: e.target.checked,
                  }))
                }
              />{' '}
              Exclude commands from judge context
            </label>
          </div>
        </fieldset>
        <fieldset disabled={saving}>
          <legend>Lead (basic)</legend>
          <div>
            <label htmlFor="bh-fu">Follow-up index (optional)</label>{' '}
            <input
              id="bh-fu"
              type="text"
              inputMode="numeric"
              value={form.followUpIndex}
              onChange={(e) =>
                setForm((f) => ({ ...f, followUpIndex: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="bh-cap">Capture index (optional)</label>{' '}
            <input
              id="bh-cap"
              type="text"
              inputMode="numeric"
              value={form.captureIndex}
              onChange={(e) =>
                setForm((f) => ({ ...f, captureIndex: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="bh-keys">Answer keys (comma-separated)</label>{' '}
            <input
              id="bh-keys"
              type="text"
              value={form.answerKeys}
              onChange={(e) =>
                setForm((f) => ({ ...f, answerKeys: e.target.value }))
              }
            />
          </div>
        </fieldset>
        <fieldset disabled={saving}>
          <legend>Flags</legend>
          <div>
            <label>
              <input
                type="checkbox"
                checked={form.enableChat}
                onChange={(e) =>
                  setForm((f) => ({ ...f, enableChat: e.target.checked }))
                }
              />{' '}
              Enable chat
            </label>
          </div>
          <div>
            <label>
              <input
                type="checkbox"
                checked={form.enableLead}
                onChange={(e) =>
                  setForm((f) => ({ ...f, enableLead: e.target.checked }))
                }
              />{' '}
              Enable lead
            </label>
          </div>
          <div>
            <label>
              <input
                type="checkbox"
                checked={form.enableJudge}
                onChange={(e) =>
                  setForm((f) => ({ ...f, enableJudge: e.target.checked }))
                }
              />{' '}
              Enable judge
            </label>
          </div>
        </fieldset>
        <fieldset disabled={saving}>
          <legend>Hot lead</legend>
          <div>
            <label htmlFor="bh-hot-pot">Potential value</label>{' '}
            <input
              id="bh-hot-pot"
              type="text"
              value={form.hotLeadPotentialValue}
              onChange={(e) =>
                setForm((f) => ({
                  ...f,
                  hotLeadPotentialValue: e.target.value,
                }))
              }
            />
          </div>
          <div>
            <label htmlFor="bh-hot-tag">Tag</label>{' '}
            <input
              id="bh-hot-tag"
              type="text"
              value={form.hotLeadTag}
              onChange={(e) =>
                setForm((f) => ({ ...f, hotLeadTag: e.target.value }))
              }
            />
          </div>
        </fieldset>
        <p>
          <button type="submit" disabled={saving || loadingEdit}>
            {saving
              ? (editingId ? 'Saving...' : 'Creating...')
              : (editingId ? 'Save' : 'Create')}
          </button>
          {editingId && (
            <button type="button" onClick={handleCancelEdit} disabled={saving} style={{ marginLeft: 8 }}>
              Cancel
            </button>
          )}
        </p>
        {validationError && <p>{validationError}</p>}
        {createError && <p>{createError}</p>}
      </form>
      {loading && <p>Loading…</p>}
      {error && <p>{error}</p>}
      {!loading && !error && (
        <table>
          <thead>
            <tr>
              <th>Id</th>
              <th>Prefix</th>
              <th>Gateway</th>
              <th>Flags</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {behaviors.map((b) => (
              <tr key={b.id} style={editingId === b.id ? { background: '#fffde7' } : undefined}>
                <td>{b.id}</td>
                <td>{b.judgeCommandPrefix ?? ''}</td>
                <td>{b.enableGatewayRouting ? 'on' : 'off'}</td>
                <td>
                  chat {b.enableChat ? 'on' : 'off'} / lead{' '}
                  {b.enableLead ? 'on' : 'off'} / judge{' '}
                  {b.enableJudge ? 'on' : 'off'}
                </td>
                <td>
                  <button
                    type="button"
                    disabled={loadingEdit}
                    onClick={() => handleEdit(b.id)}
                  >
                    {loadingEdit && editingId === b.id ? 'Loading...' : 'Edit'}
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
