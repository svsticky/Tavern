import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: `${import.meta.env.ApiUrl}/swagger/v1/swagger.json`,
  output: {
    format: 'biome',
    lint: 'biome',
    path: './app/api',
  },
  plugins: [
    '@hey-api/client-axios',
  ]
});
