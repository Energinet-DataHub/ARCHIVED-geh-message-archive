# Message Archive

[![codecov](https://codecov.io/gh/Energinet-DataHub/geh-message-archive/branch/main/graph/badge.svg?token=RYDD1WHQMO)](https://codecov.io/gh/Energinet-DataHub/geh-message-archive)

## Intro

Processing af saved request and response logs har in message-archive processed and saved.
The type of log er determined and parsed with a xml or json parser.

The processed logs should be searchable later and the specific log can be downloaded for storage.

Logs are saved by a middleware use en domains.
<https://github.com/Energinet-DataHub/geh-core/tree/main/source/Logging>

## Architecture

### Azure function trigger

Runs every X second and parses new logs saved by middleware.

Parsed with xml or json parser.

Saved to Cosmos Database.

### Web API

Search endpoint searches in saved logs.

Download endpoint downloads saved log content.

![Architecture](ARCHITECTURE.png)

## Structure

Code contains two main solutions.

One regarding processing and reading log files and one encapsulation a client for connection to the webapi with a factory pattern.

### Processing and reading

The overall structure and idea described here.

- Processing: Processing handler, log parsers, mappers and models.
- Reader: Search query against persistence. Models describing the search criteria and result. Validation and mappers. 
- Persistence: Write and read services for log storage.
- Persistence models: Contains shared models between reading and processing. 

### Client

- Nuget package with container extensions for setting up client factory for message archive http web api.
- Setup and calls the webapi from a input dto.
