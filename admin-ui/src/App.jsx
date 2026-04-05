import { useState } from 'react';
import ConfigPage from './ConfigPage.jsx';
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
      </div>
      {page === 'test' && <TestPage />}
      {page === 'config' && <ConfigPage />}
    </div>
  );
}
