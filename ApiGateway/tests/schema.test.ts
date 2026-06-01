import { buildSchema, type GraphQLObjectType } from 'graphql';
import { typeDefs } from '../src/schema.js';

describe('typeDefs', () => {
    const schema = buildSchema(typeDefs);

    it('parses into a valid GraphQL schema', () => {
        expect(schema).toBeDefined();
        expect(schema.getQueryType()).toBeDefined();
    });

    describe('object types', () => {
        const expectedObjectTypes = [
            'AirQualityReading',
            'MotionReading',
            'EnergyReading',
            'AirQualityStats',
            'EnergyStats',
            'MotionStats',
            'AirQualityResponse',
            'EnergyResponse',
            'MotionResponse',
        ];

        it.each(expectedObjectTypes)('defines %s', (typeName) => {
            expect(schema.getType(typeName)).toBeDefined();
        });

        it('AirQualityReading exposes the expected fields', () => {
            const type = schema.getType('AirQualityReading') as GraphQLObjectType;
            const fields = type.getFields();
            expect(Object.keys(fields).sort()).toEqual(
                ['co2', 'humidity', 'id', 'name', 'pm25', 'timestamp'].sort(),
            );
            expect(String(fields.id!.type)).toBe('ID!');
            expect(String(fields.name!.type)).toBe('String!');
            expect(String(fields.timestamp!.type)).toBe('String!');
        });

        it('EnergyReading exposes the expected fields', () => {
            const type = schema.getType('EnergyReading') as GraphQLObjectType;
            const fields = type.getFields();
            expect(Object.keys(fields).sort()).toEqual(
                ['energy', 'id', 'name', 'timestamp'].sort(),
            );
            expect(String(fields.energy!.type)).toBe('Float');
        });

        it('MotionReading exposes the expected fields', () => {
            const type = schema.getType('MotionReading') as GraphQLObjectType;
            const fields = type.getFields();
            expect(Object.keys(fields).sort()).toEqual(
                ['id', 'motionDetected', 'name', 'timestamp'].sort(),
            );
            expect(String(fields.motionDetected!.type)).toBe('Boolean');
        });

        it('AirQualityResponse wraps items and totalCount as non-null', () => {
            const type = schema.getType('AirQualityResponse') as GraphQLObjectType;
            const fields = type.getFields();
            expect(String(fields.items!.type)).toBe('[AirQualityReading!]!');
            expect(String(fields.totalCount!.type)).toBe('Int!');
        });
    });

    describe('Query', () => {
        const queryType = () => schema.getQueryType()!;

        const expectedQueries = [
            'getAirQualityHistory',
            'getEnergyHistory',
            'getMotionHistory',
            'aggregateAirQuality',
            'aggregateEnergy',
            'aggregateMotion',
        ];

        it.each(expectedQueries)('defines %s', (fieldName) => {
            expect(queryType().getFields()[fieldName]).toBeDefined();
        });

        it('getAirQualityHistory has optional location/limit/offset and returns AirQualityResponse!', () => {
            const field = queryType().getFields().getAirQualityHistory!;
            expect(String(field.type)).toBe('AirQualityResponse!');

            const args = Object.fromEntries(field.args.map((a) => [a.name, String(a.type)]));
            expect(args).toEqual({
                location: 'String',
                limit: 'Int',
                offset: 'Int',
            });
        });

        it('getEnergyHistory returns a non-null list of EnergyReading', () => {
            const field = queryType().getFields().getEnergyHistory!;
            expect(String(field.type)).toBe('[EnergyReading!]!');
        });

        it('getMotionHistory returns a non-null list of MotionReading', () => {
            const field = queryType().getFields().getMotionHistory!;
            expect(String(field.type)).toBe('[MotionReading!]!');
        });

        it.each([
            ['aggregateAirQuality', '[AirQualityStats!]!'],
            ['aggregateEnergy', '[EnergyStats!]!'],
            ['aggregateMotion', '[MotionStats!]!'],
        ])('%s requires location/from/to/interval and returns %s', (fieldName, returnType) => {
            const field = queryType().getFields()[fieldName]!;
            expect(String(field.type)).toBe(returnType);

            const args = Object.fromEntries(field.args.map((a) => [a.name, String(a.type)]));
            expect(args).toEqual({
                location: 'String!',
                from: 'String!',
                to: 'String!',
                interval: 'String!',
            });
        });
    });
});
