export const typeDefs = `
    type AirQualityReading {
        id: ID!
        name: String!
        co2: Int
        pm25: Int
        humidity: Int
        timestamp: String!
    }

    type MotionReading {
        id: ID!
        name: String!
        motionDetected: Boolean
        timestamp: String!
    }

    type EnergyReading {
        id: ID!
        name: String!
        energy: Float
        timestamp: String!
    }

    type AirQualityStats {
        timeBucket: String!
        avgCo2: Float
        avgPm25: Float
        avgHumidity: Float
        maxCo2: Int
    }

    type EnergyStats {
        timeBucket: String!
        totalEnergy: Float
        avgPower: Float
        peakPower: Float
    }

    type MotionStats {
        timeBucket: String!
        eventCount: Int
        isConstant: Boolean
    }

    type AirQualityResponse {
        items: [AirQualityReading!]!
        totalCount: Int!
    }

    type EnergyResponse {
        items: [EnergyReading!]
        totalCount: Int!
    }

    type MotionResponse {
        items: [MotionReading!]
        totalCount: Int!
    }

    type Query {
        getAirQualityHistory(location: String, limit: Int, offset: Int): AirQualityResponse!
        getEnergyHistory(location: String, limit: Int, offset: Int): [EnergyReading!]!
        getMotionHistory(location: String, limit: Int, offset: Int): [MotionReading!]!

        # interval: '1 minute', '1 hour', '1 day'
        aggregateAirQuality(location: String!, from: String!, to: String!, interval: String!): [AirQualityStats!]!
        aggregateEnergy(location: String!, from: String!, to: String!, interval: String!): [EnergyStats!]!
        aggregateMotion(location: String!, from: String!, to: String!, interval: String!): [MotionStats!]!
    }
`