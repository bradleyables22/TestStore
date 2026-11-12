# TestStore

## Run on another computer

Install the .NET 10 SDK, Node.js with npm, and Azure Functions Core Tools v4.

The three projects include tracked `local.settings.json` files with localhost URLs and Azurite development storage settings. A clone or pull includes these files; no settings copy is required. Keep committed settings limited to development defaults; use user secrets or environment variables for credentials.

1. Run `npm ci` in the solution directory.
2. Run `npm run storage` in one terminal and leave it running.
3. Run `npm start` in another terminal to build and start all three apps.
4. Open http://localhost:7071.

`npm run setup` can restore missing settings from the example files without overwriting existing files.
