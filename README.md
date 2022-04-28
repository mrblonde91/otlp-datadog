## Overview
This project was worked in conjunction with Bogdan Babiy, Michele Scandura and Andre Silva.

There's two major portions to this, the first is the usage of opentelemetry in conjunction with Datadog. The aim is to illustrate both dotnet metrics being fed to datadog and traces. We also include examples of custom metrics being fed over.

On top of that, we've also utilised Orleans grains utilising otlp. The main reason for that is that it's an in house technology that we like to play with. Hopefully this offers datadog users a better idea of how to utilise open telemetry. It's still somewhat of a work in progress. 

## Setup
Authenticate with aws if you want to use the dynamo portion

Add api key for data dog otel-collector-config.yaml.

    docker-compose build
    docker-compose up

Api can be hit via swagger on http://localhost:5009/swagger/index.html

## Using Dynamo
An optional 'OtlpTestTable' can be created in dynamo with a key of `Id` and enabling `UseDynamo` in appsettings. This provides a nice illustration of how outgoing calls are depicted as a span automatically. 


** .Client Console App still a work in progress ** 