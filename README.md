Create a network

    docker create network mikes-net

Add api key to docker-compose and otel config. 


Link the otlp collector to the network

    docker run -d --name opentelemetry-collector \
            --network mikes-net \
            -v $(pwd)/otel_collector_config.yaml:/etc/otel/config.yaml \
            otel/opentelemetry-collector-contrib

Start container

    docker-compose up 

Check datadog to see activity.