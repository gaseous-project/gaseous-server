#!/bin/bash

# Wait for the service to start
while ! mysqladmin ping -h localhost --silent; do
    sleep 1
done

# Set the root password
mariadb -e "ALTER USER 'root'@'localhost' IDENTIFIED BY '$MARIADB_ROOT_PASSWORD';"