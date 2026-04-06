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

function emptyForm() {
  return {
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

    return null;
  }

  function buildRequestBody() {
    const fu = parseOptionalNonNegInt(form.followUpIndex);
    const cap = parseOptionalNonNegInt(form.captureIndex);
    const answerKeys = parseCommaList(form.answerKeys);
    return {
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

  async function handleCreate(e) {
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
      await BehaviorsApi.create(buildRequestBody());
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
          'Create failed';
      setCreateError(msg);
    } finally {
      setSaving(false);
    }
  }

  return (
    <div>
      <h1>Behaviors</h1>
      <form onSubmit={handleCreate}>
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
          <button type="submit" disabled={saving}>
            {saving ? 'Creating...' : 'Create'}
          </button>
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
              <th>Flags</th>
            </tr>
          </thead>
          <tbody>
            {behaviors.map((b) => (
              <tr key={b.id}>
                <td>{b.id}</td>
                <td>{b.judgeCommandPrefix ?? ''}</td>
                <td>
                  chat {b.enableChat ? 'on' : 'off'} / lead{' '}
                  {b.enableLead ? 'on' : 'off'} / judge{' '}
                  {b.enableJudge ? 'on' : 'off'}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </div>
  );
}
