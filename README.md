# Project


- Support optional config file path via env var for easier deployment

- Simplify the config validation by using a declarative schema

- Update README with installation steps and environment variable documentation

- Implement proper cleanup of resources when the process receives SIGTERM

- Update the API docs with the new query parameters and examples

- Update the deployment docs with the new environment variables

- Correct the logic that determined whether to use cache or not

- Handle timeout gracefully and return a clear error to the caller

- Implement proper cleanup of resources when the process receives SIGTERM

- Clean up the test fixtures and move shared data to a single file

- Implement a simple metrics endpoint for Prometheus scraping

- Handle timeout gracefully and return a clear error to the caller

- Clean up unused imports and fix formatting to match the project style guide

- Improve the error recovery when the database connection is lost

- Adjust the default concurrency limit based on load test results

- Refactor utils to use a single source of truth for default values

- Adjust default timeout value to prevent premature connection drops

- Fix the ordering of middleware so auth runs before the handler

- Bump the library version and pin the dependency in requirements

- Clean up leftover code from the previous implementation

- Remove deprecated CLI flag and update docs to use the new option

- Update the example config with all available options and comments

- Simplify the main loop by extracting request handling into a dedicated function

- Implement proper cleanup of resources when the process receives SIGTERM

- Implement request ID propagation for better tracing across services

- Update the deployment docs with the new environment variables

- Add proper error handling for invalid config so the app doesn't crash on startup

- Refactor error handling to use a custom exception hierarchy

- Simplify the main loop by extracting request handling into a dedicated function

- Add a comment explaining why we disable the linter on this line

- Fix the off-by-one error in the date range iterator

- Simplify error messages so they are actionable for the end user

- Support custom headers in the client for API key or auth tokens

- Adjust the threshold so we only log when it's actually an issue

- Adjust buffer size for the stream reader to reduce memory usage

- Refactor the main entry point to make it easier to test

- Implement fallback to default value when config key is missing

- Update the example config with all available options and comments

- Implement a simple metrics endpoint for Prometheus scraping
