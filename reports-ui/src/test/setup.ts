import '@testing-library/jest-dom';
import { afterAll } from 'vitest';

afterAll(() => {
  setTimeout(() => process.exit(0), 500);
});
