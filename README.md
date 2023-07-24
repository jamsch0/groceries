# Groceries
An application for (manually) tracking your grocery shopping habits.

## Installation
A pre-built image is available at [ghcr.io/jamsch0/groceries](https://ghcr.io/jamsch0/groceries).
The default configuration can be found in `config.ini` once the volume has been created.

Example usage:
```bash
$ docker run -d -p 8080:80 -v ./groceries:/config ghcr.io/jamsch0/groceries
```

## Configuration
Groceries uses a PostgreSQL database. The PostgreSQL connection is configured by setting the `Database` key in the config file:

```ini
# config.ini
Database = "Host=127.0.0.1;Username=groceries;Password=password;Database=groceries"
```

The application will attempt to create the database if it doesn't already exist.
