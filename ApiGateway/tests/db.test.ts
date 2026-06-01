import { jest } from '@jest/globals';

const mockCreateClient = jest.fn<(opts: Record<string, unknown>) => unknown>(
    () => ({ id: 'mock-client' }),
);

jest.unstable_mockModule('@clickhouse/client', () => ({
    createClient: mockCreateClient,
}));

const ENV_KEYS = [
    'CLICKHOUSE_HOST',
    'CLICKHOUSE_USER',
    'CLICKHOUSE_PASSWORD',
    'CLICKHOUSE_DB',
] as const;

const originalEnv: Record<string, string | undefined> = {};
for (const key of ENV_KEYS) {
    originalEnv[key] = process.env[key];
}

const reimportDb = async () => {
    jest.resetModules();
    mockCreateClient.mockClear();
    return import('../src/db.js');
};

describe('clickhouse client (db.ts)', () => {
    afterEach(() => {
        for (const key of ENV_KEYS) {
            if (originalEnv[key] === undefined) {
                delete process.env[key];
            } else {
                process.env[key] = originalEnv[key];
            }
        }
    });

    it('uses sensible defaults when no env vars are set', async () => {
        for (const key of ENV_KEYS) {
            delete process.env[key];
        }

        const mod = await reimportDb();

        expect(mockCreateClient).toHaveBeenCalledTimes(1);
        expect(mockCreateClient).toHaveBeenCalledWith({
            host: 'http://localhost:8123',
            username: 'admin',
            password: 'admin_pass',
            database: 'SensorsReadings',
        });
        expect(mod.clickhouse).toEqual({ id: 'mock-client' });
    });

    it('reads connection settings from environment variables', async () => {
        process.env.CLICKHOUSE_HOST = 'http://ch.example.com:8123';
        process.env.CLICKHOUSE_USER = 'reader';
        process.env.CLICKHOUSE_PASSWORD = 's3cret';
        process.env.CLICKHOUSE_DB = 'metrics';

        await reimportDb();

        expect(mockCreateClient).toHaveBeenCalledWith({
            host: 'http://ch.example.com:8123',
            username: 'reader',
            password: 's3cret',
            database: 'metrics',
        });
    });
});
