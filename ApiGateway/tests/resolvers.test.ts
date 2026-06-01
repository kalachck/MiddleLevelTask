import { jest } from '@jest/globals';

const mockQuery = jest.fn<(args: any) => Promise<{ json: () => any }>>();

jest.unstable_mockModule('../src/db.js', () => ({
    clickhouse: { query: mockQuery },
}));

const { resolvers } = await import('../src/resolvers.js');

type Resolver = (parent: unknown, args: Record<string, unknown>) => Promise<unknown>;
const Query = resolvers.Query as Record<string, Resolver>;

const makeResultSet = (data: unknown) => ({ json: () => data });

describe('resolvers.Query', () => {
    beforeEach(() => {
        mockQuery.mockReset();
    });

    // -------------------- HISTORY QUERIES --------------------

    describe('getAirQualityHistory', () => {
        const sampleRows = [
            { id: '1', name: 'kitchen', co2: 600, pm25: 5, humidity: 40, timestamp: '2026-01-01T10:00:00Z' },
            { id: '2', name: 'kitchen', co2: 610, pm25: 6, humidity: 41, timestamp: '2026-01-01T10:01:00Z' },
        ];

        it('returns items and totalCount when location is provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet(sampleRows))
                .mockResolvedValueOnce(makeResultSet([{ total: 42 }]));

            const result = await Query.getAirQualityHistory!(null, {
                location: 'kitchen',
                limit: 25,
                offset: 5,
            });

            expect(result).toEqual({ items: sampleRows, totalCount: 42 });
            expect(mockQuery).toHaveBeenCalledTimes(2);

            const [dataCall, countCall] = mockQuery.mock.calls;
            expect(dataCall![0].query).toContain('FROM AirQualityReadings');
            expect(dataCall![0].query).toContain('WHERE Name = {loc:String}');
            expect(dataCall![0].query).toContain('ORDER BY Timestamp DESC');
            expect(dataCall![0].query).toContain('LIMIT {l:Int} OFFSET {o:Int}');
            expect(dataCall![0].query_params).toEqual({ loc: 'kitchen', l: 25, o: 5 });
            expect(dataCall![0].format).toBe('JSONEachRow');

            expect(countCall![0].query).toContain('SELECT count() as total FROM AirQualityReadings');
            expect(countCall![0].query).toContain('WHERE Name = {loc:String}');
            expect(countCall![0].query_params).toEqual({ loc: 'kitchen' });
        });

        it('omits WHERE clause when location is not provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet([]))
                .mockResolvedValueOnce(makeResultSet([{ total: 0 }]));

            await Query.getAirQualityHistory!(null, {});

            const [dataCall, countCall] = mockQuery.mock.calls;
            expect(dataCall![0].query).not.toContain('WHERE');
            expect(countCall![0].query).not.toContain('WHERE');
            expect(dataCall![0].query_params).toEqual({ loc: undefined, l: 10, o: 0 });
        });

        it('uses default limit 10 and offset 0', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet([]))
                .mockResolvedValueOnce(makeResultSet([{ total: 0 }]));

            await Query.getAirQualityHistory!(null, { location: 'bedroom' });

            expect(mockQuery.mock.calls[0]![0].query_params).toEqual({
                loc: 'bedroom',
                l: 10,
                o: 0,
            });
        });

        it('returns totalCount = 0 when count result is empty', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet(sampleRows))
                .mockResolvedValueOnce(makeResultSet([]));

            const result = (await Query.getAirQualityHistory!(null, {})) as { totalCount: number };

            expect(result.totalCount).toBe(0);
        });

        it('returns totalCount = 0 when count.total is missing', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet(sampleRows))
                .mockResolvedValueOnce(makeResultSet([{}]));

            const result = (await Query.getAirQualityHistory!(null, {})) as { totalCount: number };

            expect(result.totalCount).toBe(0);
        });
    });

    describe('getEnergyHistory', () => {
        const sampleRows = [
            { id: '1', name: 'meter-1', energy: 1.23, timestamp: '2026-01-01T10:00:00Z' },
        ];

        it('returns items and totalCount when location is provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet(sampleRows))
                .mockResolvedValueOnce(makeResultSet([{ total: 7 }]));

            const result = await Query.getEnergyHistory!(null, {
                location: 'meter-1',
                limit: 5,
                offset: 0,
            });

            expect(result).toEqual({ items: sampleRows, totalCount: 7 });
            expect(mockQuery.mock.calls[0]![0].query).toContain('FROM EnergyReadings');
            expect(mockQuery.mock.calls[0]![0].query).toContain('WHERE Name = {loc:String}');
            expect(mockQuery.mock.calls[0]![0].query_params).toEqual({
                loc: 'meter-1',
                l: 5,
                o: 0,
            });
            expect(mockQuery.mock.calls[1]![0].query).toContain(
                'SELECT count() as total FROM EnergyReadings',
            );
        });

        it('omits WHERE clause and applies defaults when no args provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet([]))
                .mockResolvedValueOnce(makeResultSet([{ total: 0 }]));

            await Query.getEnergyHistory!(null, {});

            expect(mockQuery.mock.calls[0]![0].query).not.toContain('WHERE');
            expect(mockQuery.mock.calls[0]![0].query_params).toEqual({
                loc: undefined,
                l: 10,
                o: 0,
            });
        });
    });

    describe('getMotionHistory', () => {
        const sampleRows = [
            { id: '1', name: 'door-1', motionDetected: true, timestamp: '2026-01-01T10:00:00Z' },
        ];

        it('returns items and totalCount when location is provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet(sampleRows))
                .mockResolvedValueOnce(makeResultSet([{ total: 3 }]));

            const result = await Query.getMotionHistory!(null, {
                location: 'door-1',
                limit: 100,
                offset: 20,
            });

            expect(result).toEqual({ items: sampleRows, totalCount: 3 });
            expect(mockQuery.mock.calls[0]![0].query).toContain('FROM MotionReadings');
            expect(mockQuery.mock.calls[0]![0].query).toContain(
                'MotionDetected as motionDetected',
            );
            expect(mockQuery.mock.calls[0]![0].query_params).toEqual({
                loc: 'door-1',
                l: 100,
                o: 20,
            });
            expect(mockQuery.mock.calls[1]![0].query).toContain(
                'SELECT count() as total FROM MotionReadings',
            );
        });

        it('omits WHERE clause when location is not provided', async () => {
            mockQuery
                .mockResolvedValueOnce(makeResultSet([]))
                .mockResolvedValueOnce(makeResultSet([{ total: 0 }]));

            await Query.getMotionHistory!(null, {});

            expect(mockQuery.mock.calls[0]![0].query).not.toContain('WHERE');
        });
    });

    // -------------------- AGGREGATION QUERIES --------------------

    describe('aggregateAirQuality', () => {
        const buckets = [
            { timeBucket: '2026-01-01T10:00:00Z', avgCo2: 600, avgPm25: 5, avgHumidity: 40, maxCo2: 700 },
        ];

        it('interpolates interval, forwards loc/from/to and returns JSON rows', async () => {
            mockQuery.mockResolvedValueOnce(makeResultSet(buckets));

            const result = await Query.aggregateAirQuality!(null, {
                location: 'kitchen',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
                interval: '1 hour',
            });

            expect(result).toEqual(buckets);
            expect(mockQuery).toHaveBeenCalledTimes(1);

            const callArg = mockQuery.mock.calls[0]![0];
            expect(callArg.query).toContain('FROM AirQualityReadings');
            expect(callArg.query).toContain('INTERVAL 1 hour');
            expect(callArg.query).toContain('avg(Co2) as avgCo2');
            expect(callArg.query).toContain('max(Co2) as maxCo2');
            expect(callArg.query).toContain('GROUP BY timeBucket');
            expect(callArg.query).toContain('ORDER BY timeBucket ASC');
            expect(callArg.format).toBe('JSONEachRow');
            expect(callArg.query_params).toEqual({
                loc: 'kitchen',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
            });
        });
    });

    describe('aggregateEnergy', () => {
        const buckets = [
            { timeBucket: '2026-01-01T10:00:00Z', totalEnergy: 12.5, avgPower: 1.5, peakPower: 3.2 },
        ];

        it('interpolates interval, forwards loc/from/to and returns JSON rows', async () => {
            mockQuery.mockResolvedValueOnce(makeResultSet(buckets));

            const result = await Query.aggregateEnergy!(null, {
                location: 'meter-1',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
                interval: '1 day',
            });

            expect(result).toEqual(buckets);

            const callArg = mockQuery.mock.calls[0]![0];
            expect(callArg.query).toContain('FROM EnergyReadings');
            expect(callArg.query).toContain('INTERVAL 1 day');
            expect(callArg.query).toContain('sum(Energy) as totalEnergy');
            expect(callArg.query).toContain('avg(Energy) as avgPower');
            expect(callArg.query).toContain('max(Energy) as peakPower');
            expect(callArg.query_params).toEqual({
                loc: 'meter-1',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
            });
        });
    });

    describe('aggregateMotion', () => {
        const buckets = [
            { timeBucket: '2026-01-01T10:00:00Z', eventCount: 7, isConstant: 1 },
        ];

        it('interpolates interval, forwards loc/from/to and returns JSON rows', async () => {
            mockQuery.mockResolvedValueOnce(makeResultSet(buckets));

            const result = await Query.aggregateMotion!(null, {
                location: 'door-1',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
                interval: '1 minute',
            });

            expect(result).toEqual(buckets);

            const callArg = mockQuery.mock.calls[0]![0];
            expect(callArg.query).toContain('FROM MotionReadings');
            expect(callArg.query).toContain('INTERVAL 1 minute');
            expect(callArg.query).toContain('countIf(MotionDetected = 1) as eventCount');
            expect(callArg.query).toContain('max(MotionDetected) as isConstant');
            expect(callArg.query_params).toEqual({
                loc: 'door-1',
                from: '2026-01-01T00:00:00Z',
                to: '2026-01-02T00:00:00Z',
            });
        });

        it('propagates errors thrown by ClickHouse client', async () => {
            mockQuery.mockRejectedValueOnce(new Error('CH offline'));

            await expect(
                Query.aggregateMotion!(null, {
                    location: 'door-1',
                    from: '2026-01-01T00:00:00Z',
                    to: '2026-01-02T00:00:00Z',
                    interval: '1 minute',
                }),
            ).rejects.toThrow('CH offline');
        });
    });
});
