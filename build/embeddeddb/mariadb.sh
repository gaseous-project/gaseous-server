#!/bin/sh

# install the database
echo "Installing MariaDB"
/usr/bin/mariadb-install-db --datadir=/var/lib/mysql --user=gaseous

# start the database server without network or grant tables
echo "Starting MariaDB"
/usr/sbin/mariadbd --datadir=/var/lib/mysql  --skip-grant-tables --skip-networking &

# wait for the server to start
sleep 5

# change the root password
echo "Setting MariaDB root password"
mariadb -u root -e "FLUSH PRIVILEGES; ALTER USER 'root'@'localhost' IDENTIFIED BY '$MARIADB_ROOT_PASSWORD'; ALTER USER 'gaseous'@'localhost' IDENTIFIED BY '$MARIADB_ROOT_PASSWORD'; FLUSH PRIVILEGES; SHUTDOWN;"

# stop the server
sleep 5
echo "Stopping MariaDB"
killall mariadbd

# start the server normally
echo "Starting MariaDB"
/usr/sbin/mariadbd --datadir=/var/lib/mysql --user=gaseous