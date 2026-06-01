/** @type {import('jest').Config} */
export default {
    preset: 'ts-jest/presets/default-esm',
    testEnvironment: 'jsdom',
    extensionsToTreatAsEsm: ['.ts', '.tsx'],
    setupFilesAfterEnv: ['<rootDir>/jest.setup.ts'],
    moduleNameMapper: {
        '^(\\.{1,2}/.*)\\.js$': '$1',
        '\\.(css|less|sass|scss)$': '<rootDir>/tests/__mocks__/styleMock.cjs',
    },
    transform: {
        '^.+\\.tsx?$': [
            'ts-jest',
            {
                useESM: true,
                tsconfig: {
                    jsx: 'react-jsx',
                    module: 'esnext',
                    target: 'es2023',
                    moduleResolution: 'bundler',
                    esModuleInterop: true,
                    allowSyntheticDefaultImports: true,
                    isolatedModules: true,
                    verbatimModuleSyntax: false,
                    noUnusedLocals: false,
                    noUnusedParameters: false,
                    erasableSyntaxOnly: false,
                    skipLibCheck: true,
                    types: ['node', 'jest', '@testing-library/jest-dom'],
                },
            },
        ],
    },
    testMatch: ['<rootDir>/tests/**/*.test.{ts,tsx}'],
    collectCoverageFrom: [
        'src/components/dashboard/LiveReadingBanner.tsx',
        'src/components/ui/ConnectionStatus.tsx',
        'src/hooks/**/*.ts',
        'src/signalr/selectors.ts',
        'src/signalr/sensorNotificationsContextValue.ts',
        'src/signalr/useSensorNotificationsContext.ts',
    ],
    coverageDirectory: 'coverage',
    clearMocks: true,
};
