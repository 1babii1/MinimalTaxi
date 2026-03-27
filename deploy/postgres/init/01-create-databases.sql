CREATE DATABASE minimal_taxi_service_db;

\connect minimal_taxi_auth;
CREATE EXTENSION IF NOT EXISTS postgis;

\connect minimal_taxi_service_db;
CREATE EXTENSION IF NOT EXISTS postgis;
