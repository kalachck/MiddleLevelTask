import { clickhouse } from "./db.js";

export const resolvers = {
    Query: {
        // --- HISTORY QUERIES (Filtration and pagination) ---
        getAirQualityHistory: async (_: any, { location, limit = 10, offset = 0}: any) => {
            const whereClause = location ? 'WHERE Name = {loc:String}' : '';

            const query = `
                SELECT Id as id,
                Name as name,
                Co2 as co2,
                Pm25 as pm25,
                Humidity as humidity,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp
                FROM AirQualityReadings
                ${whereClause}
                ORDER BY Timestamp DESC
                LIMIT {l:Int} OFFSET {o:Int}
            `;

            const countQuery = `SELECT count() as total FROM AirQualityReadings ${whereClause}`;

            const [rows, countResult]: any = await Promise.all([
                clickhouse.query({ query: query, query_params: { loc: location, l: limit, o: offset }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json()),
                clickhouse.query({ query: countQuery, query_params: { loc: location }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json())
            ]);

            return {
                items: rows,
                totalCount: countResult[0]?.total || 0
            };
        },

        getEnergyHistory: async (_: any, { location, limit = 10, offset = 0}: any) => {
            const whereClause = location ? 'WHERE Name = {loc:String}' : '';

            const query = `
                SELECT Id as id,
                Name as name,
                Energy as energy,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp
                FROM EnergyReadings
                ${whereClause}
                ORDER BY Timestamp DESC
                LIMIT {l:Int} OFFSET {o:Int}
            `;

            const countQuery = `SELECT count() as total FROM EnergyReadings ${whereClause}`;

            const [rows, countResult]: any = await Promise.all([
                clickhouse.query({ query: query, query_params: { loc: location, l: limit, o: offset }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json()),
                clickhouse.query({ query: countQuery, query_params: { loc: location }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json())
            ]);

            return {
                items: rows,
                totalCount: countResult[0]?.total || 0
            };
        },

        getMotionHistory: async (_: any, { location, limit = 10, offset = 0}: any) => {
            const whereClause = location ? 'WHERE Name = {loc:String}' : '';

            const query = `
                SELECT Id as id,
                Name as name,
                MotionDetected as motionDetected,
                formatDateTime(Timestamp, '%Y-%m-%dT%H:%i:%sZ') as timestamp
                FROM MotionReadings
                ${whereClause}
                ORDER BY Timestamp DESC
                LIMIT {l:Int} OFFSET {o:Int}
            `;

            const countQuery = `SELECT count() as total FROM MotionReadings ${whereClause}`;

            const [rows, countResult]: any = await Promise.all([
                clickhouse.query({ query: query, query_params: { loc: location, l: limit, o: offset }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json()),
                clickhouse.query({ query: countQuery, query_params: { loc: location }, format: 'JSONEachRow' }).then((r: { json: () => any; }) => r.json())
            ]);

            return {
                items: rows,
                totalCount: countResult[0]?.total || 0
            };
        },

        // --- AGGREGATION QUERIES (Graphics) ---

        aggregateAirQuality: async (_: any, { location, from, to, interval }: any) => {
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
            const resultSet = await clickhouse.query({ query, query_params: { loc: location, from, to }, format: 'JSONEachRow' });
            return resultSet.json();
        },

        aggregateEnergy: async (_: any, { location, from, to, interval }: any) => {
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
            const resultSet = await clickhouse.query({ query, query_params: { loc: location, from, to }, format: 'JSONEachRow' });
            return resultSet.json();
        },

        aggregateMotion: async (_: any, { location, from, to, interval }: any) => {
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
            const resultSet = await clickhouse.query({ query, query_params: { loc: location, from, to }, format: 'JSONEachRow' });
            return resultSet.json();
        }
    }
}