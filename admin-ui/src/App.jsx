import { useState } from 'react';
import BehaviorsPage from './BehaviorsPage.jsx';
import BotsPage from './BotsPage.jsx';
import ConfigPage from './ConfigPage.jsx';
import IntegrationsPage from './IntegrationsPage.jsx';
import PersonasPage from './PersonasPage.jsx';
import TestPage from './TestPage.jsx';

export default function App() {
  const [page, setPage] = useState('test');

  return (
    <div>
      <div>
        <button type="button" onClick={() => setPage('test')}>
          Test
        </button>
        <button type="button" onClick={() => setPage('config')}>
          Config
        </button>
        <button type="button" onClick={() => setPage('bots')}>
          Bots
        </button>
        <button type="button" onClick={() => setPage('personas')}>
          Personas
        </button>
        <button type="button" onClick={() => setPage('behaviors')}>
          Behaviors
        </button>
        <button type="button" onClick={() => setPage('integrations')}>
          Integrations
        </button>
      </div>
      {page === 'test' && <TestPage />}
      {page === 'config' && <ConfigPage />}
      {page === 'bots' && <BotsPage />}
      {page === 'personas' && <PersonasPage />}
      {page === 'behaviors' && <BehaviorsPage />}
      {page === 'integrations' && <IntegrationsPage />}
    </div>
  );
}
