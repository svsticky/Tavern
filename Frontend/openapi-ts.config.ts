import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:8080/swagger/v1/swagger.json',
  output: {
    // app/api is excluded from biome.json's includes (generated code doesn't conform to the
    // project's lint rules), so a biome post-processor here would fail with "no files processed".
    path: './app/api',
  },
  plugins: [
    '@hey-api/client-axios',
  ]
});
