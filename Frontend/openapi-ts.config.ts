import { defineConfig } from '@hey-api/openapi-ts';
import { getEnv } from '~/util/config.utils';

export default defineConfig({
  input: `${getEnv("ApiUrl")}/swagger/v1/swagger.json`,
  output: {
    format: 'biome',
    lint: 'biome',
    path: './app/api',
  },
  plugins: [
    '@hey-api/client-axios',
  ]
});
