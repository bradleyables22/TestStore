import assert from 'node:assert/strict';
import { startStore } from './run.mjs';
process.env.Store__EnablePreview = 'false';
const store = await startStore();
try {
    for (const port of [7072, 7073]) {
        const response = await fetch(`http://localhost:${port}/`);
        assert.equal(response.status, 404, 'Explicitly disabled preview must not be routed');
    }
    console.log('PASS: backend preview routes are absent when disabled.');
} finally { store.stop(); }
