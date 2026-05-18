import dotenv from 'dotenv';
import { dirname, resolve } from 'node:path';
import { fileURLToPath } from 'node:url';

const packageRoot = resolve(dirname(fileURLToPath(import.meta.url)), '..');

dotenv.config({ path: resolve(packageRoot, '.env') });
dotenv.config({ path: resolve(packageRoot, '..', '.env'), override: false });
