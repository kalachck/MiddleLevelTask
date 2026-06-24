import { clickhouse } from './db.js';

interface HistoryQueryArgs {
  location?: string;
  limit?: number;
  offset?: number;
}

interface AggregateQueryArgs {
  location: string;
  from: string;
  to: string;
  interval: string;
}

interface TotalCountRow {
  total: number;
}

async function fetchPaginatedHistory<T>(
  table: string,
  selectClause: string,
  { location, limit = 10, offset = 0 }: HistoryQueryArgs,
): Promise<{ items: T[]; totalCount: number }> {
  const whereClause = location ? 'WHERE Name = {loc:String}' : '';
  const query = `
                SELECT ${selectClause}
                FROM ${table}
                ${whereClause}
                ORDER BY Timestamp DESC
                LIMIT {l:Int} OFFSET {o:Int}
            `;
  const countQuery = `SELECT count() as total FROM ${table} ${whereClause}`;

  const [rows, countResult] = await Promise.all([
    clickhouse
      .query({
        query,
        query_params: { loc: location, l: limit, o: offset },
        format: 'JSONEachRow',
      })
      .then((result) => result.json<T>()),
    clickhouse
      .query({
        query: countQuery,
        query_params: { loc: location },
        format: 'JSONEachRow',
      })
      .then((result) => result.json<TotalCountRow>()),
  ]);

  return {
    items: rows,
    totalCount: countResult[0]?.total ?? 0,
  };
}

export const resolvers = {
  Query: {
    getAirQualityHistory: (_parent: unknown, args: HistoryQueryArgs) =>
      fetchPaginatedHistory(
        'AirQualityReadings',
        `Id as id,
                Name as name,
                Co2 as co2,
                Pm25 as pm25,
                Humidity as humidity,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp`,
        args,
      ),

    getEnergyHistory: (_parent: unknown, args: HistoryQueryArgs) =>
      fetchPaginatedHistory(
        'EnergyReadings',
        `Id as id,
                Name as name,
                Energy as energy,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp`,
        args,
      ),

    getMotionHistory: (_parent: unknown, args: HistoryQueryArgs) =>
      fetchPaginatedHistory(
        'MotionReadings',
        `Id as id,
                Name as name,
                MotionDetected as motionDetected,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp`,
        args,
      ),

    aggregateAirQuality: async (_parent: unknown, { location, from, to, interval }: AggregateQueryArgs) => {
      const query = `
                SELECT 
                    formatDateTime(toStartOfInterval(Timestamp, INTERVAL ${interval}), '%Y-%m-%dT%H:%i:%sZ') as timeBucket,
                    avg(Co2) as avgCo2,
                    avg(Pm25) as avgPm25,
                    avg(Humidity) as avgHumidity,
                    max(Co2) as maxCo2
                FROM AirQualityReadings
                WHERE Name = {loc:String} 
                AND Timestamp >= parseDateTimeBestEffort({from:String}) 
                AND Timestamp <= parseDateTimeBestEffort({to:String})
                GROUP BY timeBucket
                ORDER BY timeBucket ASC
            `;
      const resultSet = await clickhouse.query({
        query,
        query_params: { loc: location, from, to },
        format: 'JSONEachRow',
      });
      return resultSet.json();
    },

    aggregateEnergy: async (_parent: unknown, { location, from, to, interval }: AggregateQueryArgs) => {
      const query = `
                SELECT 
                    formatDateTime(toStartOfInterval(Timestamp, INTERVAL ${interval}), '%Y-%m-%dT%H:%i:%sZ') as timeBucket,
                    sum(Energy) as totalEnergy,
                    avg(Energy) as avgPower,
                    max(Energy) as peakPower
                FROM EnergyReadings
                WHERE Name = {loc:String} 
                AND Timestamp >= parseDateTimeBestEffort({from:String}) 
                AND Timestamp <= parseDateTimeBestEffort({to:String})
                GROUP BY timeBucket
                ORDER BY timeBucket ASC
            `;
      const resultSet = await clickhouse.query({
        query,
        query_params: { loc: location, from, to },
        format: 'JSONEachRow',
      });
      return resultSet.json();
    },

    aggregateMotion: async (_parent: unknown, { location, from, to, interval }: AggregateQueryArgs) => {
      const query = `
                SELECT 
                    formatDateTime(toStartOfInterval(Timestamp, INTERVAL ${interval}), '%Y-%m-%dT%H:%i:%sZ') as timeBucket,
                    countIf(MotionDetected = 1) as eventCount,
                    max(MotionDetected) as isConstant
                FROM MotionReadings
                WHERE Name = {loc:String} 
                AND Timestamp >= parseDateTimeBestEffort({from:String}) 
                AND Timestamp <= parseDateTimeBestEffort({to:String})
                GROUP BY timeBucket
                ORDER BY timeBucket ASC
            `;
      const resultSet = await clickhouse.query({
        query,
        query_params: { loc: location, from, to },
        format: 'JSONEachRow',
      });
      return resultSet.json();
    },
  },
};
