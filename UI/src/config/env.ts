const hubKeyDefault = 'c3VwZXItc2VjcmV0LWRhdGEtcHJvY2Vzc29yLWh1Yi1rZXk=';

export const env = {
  graphqlUrl: import.meta.env.VITE_GRAPHQL_URL ?? 'http://localhost:8080/graphql',
  signalrHubUrl: import.meta.env.VITE_SIGNALR_HUB_URL ?? 'http://localhost:5036/hubs/sensors',
  signalrHubKey: import.meta.env.VITE_SIGNALR_HUB_KEY ?? hubKeyDefault,
} as const;
