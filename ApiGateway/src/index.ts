import './instrumentation.js';
import './loadEnv.js';

import Fastify from 'fastify';
import mercurius from 'mercurius';
import cors from '@fastify/cors';
import { typeDefs } from './schema.js';
import { resolvers } from './resolvers.js';
import { prometheusExporter } from './instrumentation.js';

const app = Fastify({
  logger: {
    transport: {
      target: 'pino-pretty',
      options: { colorize: true },
    },
  },
});

const start = async () => {
  try {
    await app.register(cors, {
      origin: true,
      methods: ['GET', 'POST'],
    });

    await app.register(mercurius, {
      schema: typeDefs,
      resolvers,
      graphiql: true,
      path: '/graphql',
    });

    app.get('/health', () => ({
      status: 'ok',
      timestamp: new Date().toISOString(),
    }));

    app.get('/metrics', (request, reply) => {
      reply.hijack();
      prometheusExporter.getMetricsRequestHandler(request.raw, reply.raw);
    });

    const port = Number(process.env.PORT) || 8080;
    const host = process.env.HOST ?? '0.0.0.0';

    await app.listen({ port, host });

    app.log.info(
      {
        graphql: `http://localhost:${port}/graphql`,
        graphiql: `http://localhost:${port}/graphiql`,
        health: `http://localhost:${port}/health`,
      },
      'API Gateway is ready',
    );
  } catch (error) {
    app.log.error(error);
    process.exit(1);
  }
};

void start();
