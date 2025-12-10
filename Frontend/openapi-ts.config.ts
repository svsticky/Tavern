import { defineConfig } from '@hey-api/openapi-ts';

export default defineConfig({
  input: 'http://localhost:8080/swagger/v1/swagger.json',
  output: {
    format: 'prettier',
    lint: 'eslint',
    path: './app/api',
  },
});
