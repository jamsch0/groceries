# Groceries
An application for (manually) tracking your grocery shopping habits.

## Installation
A pre-built image is available at [git.jamsch0.dev/jamsch0/groceries](https://git.jamsch0.dev/jamsch0/-/packages/container/groceries).
The default configuration can be found in `config.ini` once the volume has been created.

Example usage:
```bash
$ docker run -d -p 8080:80 -e LANG=en_GB TZ=Europe/London -v ./groceries:/config git.jamsch0.dev/jamsch0/groceries
```

## Configuration
Groceries uses a PostgreSQL database. The PostgreSQL connection is configured by setting the `Database` key in the config file:

```ini
# config.ini
Database = "Host=127.0.0.1;Username=groceries;Password=password;Database=groceries"
```

The application will attempt to create the database if it doesn't already exist.
