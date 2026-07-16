import '@testing-library/jest-dom';
import { afterAll } from 'vitest';

// Force exit after all tests complete — works around Node 24 open-handle hang
// with jsdom timers. Safe: only runs in test environment.
afterAll(() => {
  setTimeout(() => process.exit(0), 500);
});
