/** @type {import('jest').Config} */
export default {
    preset: 'ts-jest/presets/default-esm',
    testEnvironment: 'node',
    extensionsToTreatAsEsm: ['.ts'],
    moduleNameMapper: {
        '^(\\.{1,2}/.*)\\.js$': '$1',
    },
    transform: {
        '^.+\\.tsx?$': [
            'ts-jest',
            {
                useESM: true,
                tsconfig: {
                    module: 'esnext',
                    target: 'esnext',
                    moduleResolution: 'bundler',
                    esModuleInterop: true,
                    allowSyntheticDefaultImports: true,
                    isolatedModules: true,
                    verbatimModuleSyntax: false,
                    noUncheckedIndexedAccess: false,
                    exactOptionalPropertyTypes: false,
                },
            },
        ],
    },
    testMatch: ['<rootDir>/tests/**/*.test.ts'],
    collectCoverageFrom: [
        'src/**/*.ts',
        '!src/index.ts',
        '!src/loadEnv.ts',
    ],
    coverageDirectory: 'coverage',
    clearMocks: true,
    coverageThreshold: {
        global: {
            statements: 80,
            branches: 90,
            functions: 85,
            lines: 80,
        },
    },
};
