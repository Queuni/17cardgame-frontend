# Changelog


## 2025-12-10
- Simplify the config merge logic so overrides are predictable

## 2025-12-12
- Refactor the main entry point to make it easier to test

## 2025-12-14
- Fix the off-by-one error in the date range iterator

## 2025-12-15
- Add a note in the README about the breaking change in 2.0

## 2025-12-17
- Remove the temporary debug endpoint before the release

## 2025-12-17
- Implement proper backoff with jitter for the retry logic

## 2025-12-17
- Bump the dependency to fix the compatibility issue with Python 3.12

## 2025-12-19
- Improve error message when the required env var is not set

## 2025-12-19
- Clean up debug print statements before the release

## 2025-12-24
- Improve the default config so it works out of the box for dev

## 2025-12-25
- Clean up unused imports and fix formatting to match the project style guide

## 2026-01-06
- Clean up duplicate logic between the sync and async code paths

## 2026-01-06
- Handle timeout gracefully and return a clear error to the caller

## 2026-01-07
- Fix the test that was flaky due to reliance on system time

## 2026-01-09
- Add a note in the README about the breaking change in 2.0

## 2026-01-09
- Bump dependency to get the security fix for the reported CVE

## 2026-01-11
- Add a unit test for the edge case when the list is empty

## 2026-01-12
- Implement proper cleanup of resources when the process receives SIGTERM

## 2026-01-12
- Clean up debug print statements before the release

## 2026-01-14
- Remove redundant check that was already covered by the validator

## 2026-01-14
- Update the deployment docs with the new environment variables

## 2025-12-05
- Correct the default value for the feature flag in production
