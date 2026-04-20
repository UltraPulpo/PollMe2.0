// Polyfill TextEncoder/TextDecoder for React Router v7 in jsdom environment.
// These are available in Node.js but not in older jsdom versions.
import { TextEncoder, TextDecoder } from 'util';

Object.assign(global, { TextEncoder, TextDecoder });
