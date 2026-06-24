import { createClient } from '@clickhouse/client'

export const clickhouse = createClient({
    host: process.env.CLICKHOUSE_HOST ?? 'http://localhost:8123',
    username: process.env.CLICKHOUSE_USER ?? 'admin',
    password: process.env.CLICKHOUSE_PASSWORD ?? 'admin_pass',
    database: process.env.CLICKHOUSE_DB ?? 'SensorsReadings',
});