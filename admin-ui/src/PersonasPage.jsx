import { useCallback, useEffect, useState } from 'react';
import { PersonasApi } from './api/services/personas.js';

const emptyForm = {
  displayName: '',
  system: '',
  judge: '',
  lead: '',
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
          'Failed to load personas'
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
    if (!system) return 'prompts.system is required.';
    return null;
  }

  function buildRequestBody(existingPersona = null) {
    const existingJudge = existingPersona?.prompts?.judge ?? '';
    const existingLead = existingPersona?.prompts?.lead ?? '';
    const judgePrompt = form.judge?.trim() || existingJudge || FALLBACK_JUDGE_PROMPT;
    const leadPrompt = form.lead?.trim() || existingLead || FALLBACK_LEAD_PROMPT;

    if (!judgePrompt.includes('{{input}}')) {
      console.warn('Persona submit warning: judge prompt missing {{input}} placeholder.');
    }
    if (
      !leadPrompt.includes('{{goal}}') ||
      !leadPrompt.includes('{{experience}}')
    ) {
      console.warn(
        'Persona submit warning: lead prompt missing {{goal}} or {{experience}} placeholder.'
      );
    }

    return {
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
    setForm({
      displayName: persona.displayName ?? '',
      system: persona.prompts?.system ?? '',
      judge: persona.prompts?.judge ?? '',
      lead: persona.prompts?.lead ?? '',
      userTypes: (persona.classificationSchema?.userTypes ?? []).join(', '),
      intents: (persona.classificationSchema?.intents ?? []).join(', '),
      potentials: (persona.classificationSchema?.potentials ?? []).join(', '),
    });
  }

  return (
    <div>
      <h1>Personas</h1>
      {editingId && (
        <p>
          Editing: {form.displayName.trim() || '(unnamed)'}
        </p>
      )}
      <form onSubmit={handleSubmit}>
        <div>
          <label htmlFor="persona-display-name">Display name</label>
          <input
            id="persona-display-name"
            type="text"
            value={form.displayName}
            onChange={(e) =>
              setForm((f) => ({ ...f, displayName: e.target.value }))
            }
            disabled={saving}
          />
        </div>
        <div>
          <label htmlFor="persona-prompt-system">System prompt</label>
          <textarea
            id="persona-prompt-system"
            rows={6}
            cols={80}
            value={form.system}
            onChange={(e) =>
              setForm((f) => ({ ...f, system: e.target.value }))
            }
            disabled={saving}
          />
        </div>
        <div>
          <label htmlFor="persona-user-types">User types (comma-separated)</label>
          <input
            id="persona-user-types"
            type="text"
            value={form.userTypes}
            onChange={(e) =>
              setForm((f) => ({ ...f, userTypes: e.target.value }))
            }
            disabled={saving}
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
            disabled={saving}
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
            disabled={saving}
          />
        </div>
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
