Authenticate with aws as this uses the default aws config to hit dynamo.

Add api key for data dog in secrets.env file and otel-collector-config.yaml.

    docker-compose build
    docker-compose up