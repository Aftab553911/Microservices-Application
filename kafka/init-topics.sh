#!/bin/bash

kafka-topics \
  --bootstrap-server kafka:9092 \
  --create \
  --if-not-exists \
  --topic order.created \
  --partitions 3 \
  --replication-factor 1
