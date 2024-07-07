#!/bin/bash

# start the database server without network or grant tables
/usr/sbin/mariadbd --datadir=/var/lib/mysql  --skip-grant-tables --skip-networking &

# wait for the server to start
sleep 2

# change the root password
mariadb -u root -e "FLUSH PRIVILEGES; ALTER USER 'root'@'localhost' IDENTIFIED BY '$MARIADB_ROOT_PASSWORD'; FLUSH PRIVILEGES;"

# stop the server
sleep 1
killall mariadbd

# start the server normally
/usr/sbin/mariadbd --datadir=/var/lib/mysql
