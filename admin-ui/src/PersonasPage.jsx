import { useCallback, useEffect, useState } from 'react';
import { PersonasApi } from './api/services/personas.js';
import {
  hasAnyPromptExtensionValue,
  normalizeOptionalSchemaJson,
} from './promptConfigFormUtils.js';

const EMPTY_SCHEMA_TEXT = JSON.stringify({}, null, 2);

const emptyForm = {
  displayName: '',
  system: '',
  chatOutputSchemaText: '',
  judge: '',
  judgeInstruction: '',
  judgeSchemaText: EMPTY_SCHEMA_TEXT,
  lead: '',
  leadInstruction: '',
  leadSchemaText: EMPTY_SCHEMA_TEXT,
  userTypes: '',
  intents: '',
  potentials: '',
};

const FALLBACK_JUDGE_PROMPT =
  'Analyze the input and return a JSON result with winner and reason. Input: {{input}}';
const FALLBACK_LEAD_PROMPT =
  'Classify the user based on goal and experience. Return JSON. Goal: {{goal}}, Experience: {{experience}}';

function parseCommaList(value) {
  return value
    .split(',')
    .map((s) => s.trim())
    .filter((s) => s.length > 0);
}

function getJsonWarning(rawValue, label) {
  const value = String(rawValue ?? '').trim();
  if (!value) return '';
  try {
    JSON.parse(value);
    return '';
  } catch {
    return `${label} is not valid JSON.`;
  }
}

export default function PersonasPage() {
  const [personas, setPersonas] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [form, setForm] = useState(() => ({ ...emptyForm }));
  const [editingId, setEditingId] = useState(null);
  const [validationError, setValidationError] = useState('');
  const [submitError, setSubmitError] = useState('');
  const [saving, setSaving] = useState(false);

  const loadList = useCallback(async () => {
    setError('');
    setLoading(true);
    try {
      const data = await PersonasApi.list();
      setPersonas(Array.isArray(data) ? data : []);
    } catch (e) {
      setPersonas([]);
      setError(
        e.response?.data?.title ||
          e.response?.data?.message ||
          e.message ||
          'Failed to load prompt configurations'
      );
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadList();
  }, [loadList]);

  function resetForm() {
    setForm({ ...emptyForm });
    setEditingId(null);
    setSaving(false);
    setValidationError('');
    setSubmitError('');
  }

  function validateForSubmit() {
    const name = form.displayName.trim();
    if (!name) return 'displayName is required.';
    const system = form.system.trim();
    if (!system) return 'prompts.system (chat instruction) is required.';

    const wChat = getJsonWarning(form.chatOutputSchemaText, 'Chat output schema');
    if (wChat) return wChat;
    const wJudge = getJsonWarning(form.judgeSchemaText, 'Judge output schema');
    if (wJudge) return wJudge;
    const wLead = getJsonWarning(form.leadSchemaText, 'Lead output schema');
    if (wLead) return wLead;

    return null;
  }

  function buildPromptExtensions() {
    const ext = {
      chatOutputSchemaJson: normalizeOptionalSchemaJson(form.chatOutputSchemaText),
      judgeInstruction: String(form.judgeInstruction ?? '').trim() || null,
      judgeSchemaJson: normalizeOptionalSchemaJson(form.judgeSchemaText),
      leadInstruction: String(form.leadInstruction ?? '').trim() || null,
      leadSchemaJson: normalizeOptionalSchemaJson(form.leadSchemaText),
    };
    return ext;
  }

  function buildRequestBody(existingPersona = null) {
    const existingJudge = existingPersona?.prompts?.judge ?? '';
    const existingLead = existingPersona?.prompts?.lead ?? '';
    const judgePrompt = form.judge?.trim() || existingJudge || FALLBACK_JUDGE_PROMPT;
    const leadPrompt = form.lead?.trim() || existingLead || FALLBACK_LEAD_PROMPT;

    if (!judgePrompt.includes('{{input}}')) {
      console.warn('Submit warning: judge template missing {{input}} placeholder.');
    }
    if (
      !leadPrompt.includes('{{goal}}') ||
      !leadPrompt.includes('{{experience}}')
    ) {
      console.warn(
        'Submit warning: lead template missing {{goal}} or {{experience}} placeholder.'
      );
    }

    const body = {
      displayName: form.displayName.trim(),
      prompts: {
        system: form.system.trim(),
        judge: judgePrompt,
        lead: leadPrompt,
      },
      classificationSchema: {
        userTypes: parseCommaList(form.userTypes),
        intents: parseCommaList(form.intents),
        potentials: parseCommaList(form.potentials),
      },
    };

    const ext = buildPromptExtensions();
    if (editingId) {
      body.promptExtensions = ext;
    } else if (hasAnyPromptExtensionValue(ext)) {
      body.promptExtensions = ext;
    }

    return body;
  }

  async function handleSubmit(e) {
    e.preventDefault();
    if (saving) return;
    const v = validateForSubmit();
    if (v) {
      setValidationError(v);
      return;
    }
    setValidationError('');
    setSubmitError('');
    setSaving(true);
    try {
      const existingPersona = editingId
        ? personas.find((p) => String(p.id) === String(editingId)) ?? null
        : null;
      const body = buildRequestBody(existingPersona);
      if (editingId) {
        await PersonasApi.update(editingId, body);
      } else {
        await PersonasApi.create(body);
      }
      resetForm();
      await loadList();
    } catch (e) {
      const msg =
        e.response?.data?.title ||
          e.response?.data?.message ||
          (Array.isArray(e.response?.data?.errors) &&
            e.response.data.errors.join('; ')) ||
          e.message ||
          (editingId ? 'Update failed' : 'Create failed');
      setSubmitError(msg);
    } finally {
      setSaving(false);
    }
  }

  function startEdit(persona) {
    setEditingId(persona.id);
    setValidationError('');
    setSubmitError('');
    const px = persona.promptExtensions ?? {};
    const judgeSchemaRaw = px.judgeSchemaJson
      ? (() => {
          try {
            return JSON.stringify(JSON.parse(px.judgeSchemaJson), null, 2);
          } catch {
            return String(px.judgeSchemaJson);
          }
        })()
      : EMPTY_SCHEMA_TEXT;
    const leadSchemaRaw = px.leadSchemaJson
      ? (() => {
          try {
            return JSON.stringify(JSON.parse(px.leadSchemaJson), null, 2);
          } catch {
            return String(px.leadSchemaJson);
          }
        })()
      : EMPTY_SCHEMA_TEXT;
    const chatOut = px.chatOutputSchemaJson
      ? (() => {
          try {
            return JSON.stringify(JSON.parse(px.chatOutputSchemaJson), null, 2);
          } catch {
            return String(px.chatOutputSchemaJson);
          }
        })()
      : '';

    setForm({
      displayName: persona.displayName ?? '',
      system: persona.prompts?.system ?? '',
      chatOutputSchemaText: chatOut,
      judge: persona.prompts?.judge ?? '',
      judgeInstruction: px.judgeInstruction ?? '',
      judgeSchemaText: judgeSchemaRaw,
      lead: persona.prompts?.lead ?? '',
      leadInstruction: px.leadInstruction ?? '',
      leadSchemaText: leadSchemaRaw,
      userTypes: (persona.classificationSchema?.userTypes ?? []).join(', '),
      intents: (persona.classificationSchema?.intents ?? []).join(', '),
      potentials: (persona.classificationSchema?.potentials ?? []).join(', '),
    });
  }

  return (
    <div>
      <h1>Prompt Configuration</h1>
      {editingId && (
        <p>
          Editing: {form.displayName.trim() || '(unnamed)'}
        </p>
      )}
      <form onSubmit={handleSubmit}>
        <fieldset disabled={saving}>
          <legend>General chat</legend>
          <div>
            <label htmlFor="persona-display-name">Display name</label>
            <input
              id="persona-display-name"
              type="text"
              value={form.displayName}
              onChange={(e) =>
                setForm((f) => ({ ...f, displayName: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="persona-prompt-system">Chat instruction</label>
            <textarea
              id="persona-prompt-system"
              rows={6}
              cols={80}
              value={form.system}
              onChange={(e) =>
                setForm((f) => ({ ...f, system: e.target.value }))
              }
            />
            <p className="hint">Maps to API field <code>prompts.system</code>.</p>
          </div>
          <div>
            <label htmlFor="persona-chat-output-schema">Chat output schema (JSON)</label>
            <textarea
              id="persona-chat-output-schema"
              rows={6}
              cols={80}
              placeholder="Optional. Leave empty or {} for free-form replies."
              value={form.chatOutputSchemaText}
              onChange={(e) =>
                setForm((f) => ({ ...f, chatOutputSchemaText: e.target.value }))
              }
            />
            {getJsonWarning(form.chatOutputSchemaText, 'Chat output schema') && (
              <p>{getJsonWarning(form.chatOutputSchemaText, 'Chat output schema')}</p>
            )}
          </div>
        </fieldset>

        <fieldset disabled={saving}>
          <legend>Judge</legend>
          <div>
            <label htmlFor="persona-prompt-judge-template">Judge template (full prompt)</label>
            <textarea
              id="persona-prompt-judge-template"
              rows={8}
              cols={80}
              value={form.judge}
              onChange={(e) =>
                setForm((f) => ({ ...f, judge: e.target.value }))
              }
            />
            <p className="hint">Fallback when instruction/schema below are empty. Include <code>{'{{input}}'}</code>.</p>
          </div>
          <div>
            <label htmlFor="persona-judge-instruction">Judge instruction</label>
            <textarea
              id="persona-judge-instruction"
              rows={5}
              cols={80}
              value={form.judgeInstruction}
              onChange={(e) =>
                setForm((f) => ({ ...f, judgeInstruction: e.target.value }))
              }
            />
            <p className="hint">Optional override; include <code>{'{{input}}'}</code> for transcript injection.</p>
          </div>
          <div>
            <label htmlFor="persona-judge-schema">Judge output schema (JSON)</label>
            <textarea
              id="persona-judge-schema"
              rows={6}
              cols={80}
              value={form.judgeSchemaText}
              onChange={(e) =>
                setForm((f) => ({ ...f, judgeSchemaText: e.target.value }))
              }
            />
            {getJsonWarning(form.judgeSchemaText, 'Judge output schema') && (
              <p>{getJsonWarning(form.judgeSchemaText, 'Judge output schema')}</p>
            )}
          </div>
        </fieldset>

        <fieldset disabled={saving}>
          <legend>Lead</legend>
          <div>
            <label htmlFor="persona-prompt-lead-template">Lead template (full prompt)</label>
            <textarea
              id="persona-prompt-lead-template"
              rows={8}
              cols={80}
              value={form.lead}
              onChange={(e) =>
                setForm((f) => ({ ...f, lead: e.target.value }))
              }
            />
            <p className="hint">Fallback when instruction/schema below are empty. Include <code>{'{{goal}}'}</code> and <code>{'{{experience}}'}</code>.</p>
          </div>
          <div>
            <label htmlFor="persona-lead-instruction">Lead instruction</label>
            <textarea
              id="persona-lead-instruction"
              rows={5}
              cols={80}
              value={form.leadInstruction}
              onChange={(e) =>
                setForm((f) => ({ ...f, leadInstruction: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="persona-lead-schema">Lead output schema (JSON)</label>
            <textarea
              id="persona-lead-schema"
              rows={6}
              cols={80}
              value={form.leadSchemaText}
              onChange={(e) =>
                setForm((f) => ({ ...f, leadSchemaText: e.target.value }))
              }
            />
            {getJsonWarning(form.leadSchemaText, 'Lead output schema') && (
              <p>{getJsonWarning(form.leadSchemaText, 'Lead output schema')}</p>
            )}
          </div>
        </fieldset>

        <fieldset disabled={saving}>
          <legend>Classification</legend>
          <div>
            <label htmlFor="persona-user-types">User types (comma-separated)</label>
            <input
              id="persona-user-types"
              type="text"
              value={form.userTypes}
              onChange={(e) =>
                setForm((f) => ({ ...f, userTypes: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="persona-intents">Intents (comma-separated)</label>
            <input
              id="persona-intents"
              type="text"
              value={form.intents}
              onChange={(e) =>
                setForm((f) => ({ ...f, intents: e.target.value }))
              }
            />
          </div>
          <div>
            <label htmlFor="persona-potentials">Potentials (comma-separated)</label>
            <input
              id="persona-potentials"
              type="text"
              value={form.potentials}
              onChange={(e) =>
                setForm((f) => ({ ...f, potentials: e.target.value }))
              }
            />
          </div>
        </fieldset>

        <div>
          <button type="submit" disabled={saving}>
            {saving
              ? editingId
                ? 'Updating...'
                : 'Creating...'
              : editingId
                ? 'Save'
                : 'Create'}
          </button>{' '}
          {editingId && (
            <button
              type="button"
              disabled={saving}
              onClick={resetForm}
            >
              Cancel
            </button>
          )}
        </div>
        {validationError && <p>{validationError}</p>}
        {submitError && <p>{submitError}</p>}
      </form>
      {loading && <p>Loading…</p>}
      {error && <p>{error}</p>}
      {!loading && !error && (
        <table>
          <thead>
            <tr>
              <th>Name</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {personas.map((p) => (
              <tr key={p.id}>
                <td>{p.displayName ?? ''}</td>
                <td>
                  <button
                    type="button"
                    disabled={saving}
                    onClick={() => startEdit(p)}
                  >
                    Edit
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
