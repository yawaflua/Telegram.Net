# Contributing to Telegram.Net

Thank you for your interest in contributing to Telegram.Net! We welcome contributions from anyone in the community, regardless of experience level.

## How to Contribute

### Getting Started

1. Fork the repository at [https://github.com/yawaflua/Telegram.Net](https://github.com/yawaflua/Telegram.Net)
2. Clone your fork to your local machine
3. Set up the development environment with the required .NET SDK

### Making Changes

1. Create a new branch for your feature or bug fix:
   ```
   git checkout -b feature/your-feature-name
   ```
2. Make your changes
3. Write or update tests for the changes you made using appropriate testing frameworks (e.g., MSTest, NUnit, or xUnit)
4. Ensure your code passes all tests and meets C# coding conventions

## Code Quality Requirements

### Test Coverage

- **All pull requests must include tests with at least 50% code coverage**
- Before submitting your PR, verify coverage using tools like:
  ```
  # Example using Coverlet and ReportGenerator
  dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
  dotnet tool run reportgenerator -reports:"**/coverage.opencover.xml" -targetdir:"coveragereport" -reporttypes:Html
  ```

### Coding Standards

- Follow C# coding conventions and the existing style in the project
- Use meaningful names for classes, methods, and variables
- Include XML documentation comments for public APIs
- Keep methods focused and reasonably sized

## Pull Request Process

1. Update documentation if you're changing or adding functionality
2. Ensure your code has adequate test coverage (minimum 50%)
3. Submit your pull request with a clear title and description
4. Reference any relevant issues in your PR description

## Review Process

- All submissions require review before being merged
- Maintainers may request changes or suggest improvements
- Be responsive to feedback on your pull request

## C# Specific Guidelines

- Target the same .NET version as the project
- Avoid excessive dependencies unless absolutely necessary
- Follow SOLID principles when applicable
- Use async/await patterns appropriately for asynchronous operations
- Be mindful of performance implications, especially for a networking library

## Questions?

If you have any questions about contributing to Telegram.Net, feel free to open an issue for discussion.

Thank you for helping improve Telegram.Net!
