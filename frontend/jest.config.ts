/** @jest-config-loader ts-node */
/** @jest-config-loader-options {"transpileOnly": true} */
import type { Config } from 'jest';

const config: Config = {
  preset: 'ts-jest',
  testEnvironment: 'jsdom',
  moduleNameMapper: {
    '\\.(css|less|scss)$': 'identity-obj-proxy',
  },
  // Polyfills required before any test module loads (e.g. TextEncoder for React Router v7)
  setupFiles: ['<rootDir>/jest.setup.ts'],
  setupFilesAfterEnv: ['@testing-library/jest-dom'],
  transform: {
    '^.+\\.tsx?$': [
      'ts-jest',
      {
        tsconfig: {
          jsx: 'react-jsx',
          module: 'CommonJS',
          moduleResolution: 'node',
          esModuleInterop: true,
          allowSyntheticDefaultImports: true,
          strict: true,
          // es2020 lib adds Promise.finally; dom adds window/global/fetch types
          lib: ['es2020', 'dom', 'dom.iterable'],
          types: ['jest', '@testing-library/jest-dom', 'node'],
        },
      },
    ],
  },
};

export default config;
