import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:8080/swagger/v1/swagger.json',
  output: {
    format: 'biome',
    lint: 'biome',
    path: './app/api',
  },
  plugins: [
    '@hey-api/client-axios',
  ]
});
